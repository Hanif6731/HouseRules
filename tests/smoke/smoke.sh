#!/usr/bin/env bash
# Builds small consumer projects against a packed HouseRules.Analyzers and checks what each level reports.
#
#   tests/smoke/smoke.sh <folder that holds HouseRules.Analyzers.<version>.nupkg>
#
# DOTNET     the dotnet executable to use (default: dotnet on PATH)
# SMOKE_SDK  pin the consumer projects to this SDK band with latestPatch, e.g. 8.0.100 (default: newest installed)
set -euo pipefail

here=$(cd "$(dirname "$0")" && pwd)
feed=$(cd "$1" && pwd)
dotnet_exe=${DOTNET:-dotnet}
package=$(find "$feed" -maxdepth 1 -name 'HouseRules.Analyzers.*.nupkg' | head -n 1)
version=$(basename "$package" .nupkg)
version=${version#HouseRules.Analyzers.}
work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT
failures=0

# Paths handed to a Windows dotnet from Git Bash must be native paths.
native() { if command -v cygpath > /dev/null; then cygpath -w "$1"; else echo "$1"; fi; }

# Commands below read exactly like the README: `dotnet build`, `dotnet format`, `dotnet add package`.
dotnet() { command "$dotnet_exe" "$@"; }

export NUGET_PACKAGES
NUGET_PACKAGES=$(native "$work/packages")
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

# new_project <name> <source file> <level or ""> [csproj properties]
new_project() {
  local dir="$work/$1"
  mkdir -p "$dir"
  cp "$here/sample/$2" "$dir/"
  if [ -n "${SMOKE_SDK:-}" ]; then
    printf '{ "sdk": { "version": "%s", "rollForward": "latestPatch" } }\n' "$SMOKE_SDK" > "$dir/global.json"
  fi

  cat > "$dir/nuget.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$(native "$feed")" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="local">
      <package pattern="HouseRules.*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
EOF

  local template
  template=$(< "$here/sample/Sample.csproj.template")
  printf '%s\n' "${template/<!-- smoke: project properties -->/${4:-}}" > "$dir/Sample.csproj"
  if [ -n "$3" ]; then
    cat > "$dir/Directory.Build.props" <<'EOF'
<Project>
  <PropertyGroup>
    <HouseRulesLevel>Strict</HouseRulesLevel>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="HouseRules.Analyzers" Version="1.0.0" PrivateAssets="all" />
  </ItemGroup>
</Project>
EOF
    sed -i.bak -e "s|<HouseRulesLevel>Strict<|<HouseRulesLevel>$3<|" -e "s|Version=\"1.0.0\"|Version=\"$version\"|" \
      "$dir/Directory.Build.props"
    rm "$dir/Directory.Build.props.bak"
  fi

  echo "$dir"
}

# build <dir>: sets $output and $code
build() {
  code=0
  output=$(cd "$1" && dotnet build 2>&1) || code=$?
}

check() {
  local name=$1 expectation=$2 before=$failures
  shift 2
  case "$expectation" in
    fails) [ "$code" -ne 0 ] || { echo "FAIL $name: build succeeded, expected it to fail"; failures=$((failures + 1)); } ;;
    passes) [ "$code" -eq 0 ] || { echo "FAIL $name: build failed"; echo "$output"; failures=$((failures + 1)); } ;;
  esac

  local pattern
  for pattern in "$@"; do
    if [ "${pattern:0:1}" = "!" ]; then
      if grep -qE -- "${pattern:1}" <<< "$output"; then
        echo "FAIL $name: output matches '${pattern:1}'"
        failures=$((failures + 1))
      fi
    elif ! grep -qE -- "$pattern" <<< "$output"; then
      echo "FAIL $name: output lacks '$pattern'"
      failures=$((failures + 1))
    fi
  done

  if [ "$failures" -eq "$before" ]; then
    echo "ok   $name"
  fi
}

reported() { echo "(error|warning) $1:"; }

dir=$(new_project baseline Violations.cs Baseline)
echo "HouseRules.Analyzers $version on SDK $(cd "$dir" && dotnet --version)"
build "$dir"
check baseline fails "$(reported HR0001)" "$(reported RCS0056)" "$(reported CA5351)" "$(reported IDE0161)" \
  "!$(reported IDE1006)" "!$(reported CS8602)"

dir=$(new_project recommended Violations.cs Recommended)
build "$dir"
check recommended fails "$(reported HR0001)" "$(reported IDE1006)" "'DateTime.UtcNow' is banned" \
  "!$(reported CS8602)"

dir=$(new_project strict Violations.cs Strict)
build "$dir"
check strict fails "$(reported HR0001)" "$(reported IDE1006)" "$(reported CS8602)"

dir=$(new_project clean-strict Clean.cs Strict)
build "$dir"
check clean-strict passes "!warning [A-Z]+[0-9]+"

dir=$(new_project editorconfig-opt-out Violations.cs Baseline)
printf 'root = true\n\n[*.cs]\ndotnet_diagnostic.HR0001.severity = none\n' > "$dir/.editorconfig"
build "$dir"
check editorconfig-opt-out fails "!$(reported HR0001)" "$(reported RCS0056)"

dir=$(new_project warnings-not-errors Violations.cs Baseline '<TreatWarningsAsErrors>false</TreatWarningsAsErrors>')
build "$dir"
check warnings-not-errors passes "warning HR0001:" "!error [A-Z]+[0-9]+"

dir=$(new_project wall-clock-ban-off Violations.cs Recommended '<HouseRulesBanWallClock>false</HouseRulesBanWallClock>')
build "$dir"
check wall-clock-ban-off fails "!'DateTime.UtcNow' is banned" "'MD5' is banned"

dir=$(new_project format-fixes-layout Violations.cs Baseline)
(cd "$dir" && dotnet format > /dev/null 2>&1) || true
build "$dir"
check format-fixes-layout fails "!$(reported HR0001)" "!$(reported RCS0056)" "!$(reported IDE0161)" \
  "!$(reported IDE0055)" "$(reported CA5351)"

dir=$(new_project level-in-project-file Clean.cs Baseline '<HouseRulesLevel>Strict</HouseRulesLevel>')
build "$dir"
check level-in-project-file fails "Set HouseRulesLevel in Directory.Build.props"

dir=$(new_project invalid-level Clean.cs Strictest)
build "$dir"
check invalid-level fails "Set it to Baseline, Recommended or Strict"

dir=$(new_project add-package Clean.cs "")
(cd "$dir" && dotnet add package HouseRules.Analyzers > /dev/null)
build "$dir"
check add-package passes "!warning [A-Z]+[0-9]+"

if [ "$failures" -ne 0 ]; then
  echo "$failures smoke check(s) failed"
  exit 1
fi

echo "All smoke checks passed"
