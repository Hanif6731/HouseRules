#!/usr/bin/env bash
# Builds small consumer projects against a packed HouseRules.Analyzers and checks what each level reports.
#
#   tests/smoke/smoke.sh <folder that holds HouseRules.Analyzers.<version>.nupkg>
#
# DOTNET     the dotnet executable to use (default: dotnet on PATH)
# SMOKE_SDK  pin the consumer projects to this SDK band with latestPatch, e.g. 10.0.100 (default: newest installed)
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

dir=$(new_project lowercase-level Violations.cs strict)
build "$dir"
check lowercase-level fails "$(reported HR0001)" "$(reported IDE1006)" "$(reported CS8602)"

dir=$(new_project clean-strict Clean.cs Strict)
build "$dir"
check clean-strict passes "!warning [A-Z]+[0-9]+"

# The SDK floor is 10, but a project may target any framework that SDK builds.
for level in Baseline Recommended Strict; do
  dir=$(new_project "net8.0-$level" Clean.cs "$level" '<TargetFramework>net8.0</TargetFramework>')
  build "$dir"
  check "net8.0-$level-clean" passes "!warning [A-Z]+[0-9]+"

  cp "$here/sample/Violations.cs" "$dir/"
  build "$dir"
  check "net8.0-$level-violations" fails "$(reported HR0001)"
done

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

dir=$(new_project ratchet Violations.cs Baseline)
code=0
output=$(cd "$dir" && dotnet build -p:HouseRulesBaseline=init 2>&1) || code=$?
check ratchet-init passes "records [0-9]+ finding"

build "$dir"
check ratchet-baselined-findings-pass passes "!error [A-Z]+[0-9]+"

sed -e 's/class Violations/class MoreViolations/' "$here/sample/Violations.cs" > "$dir/More.cs"
build "$dir"
check ratchet-new-findings-fail fails "More\.cs\([0-9,]+\): error HR0001" "!Violations\.cs\([0-9,]+\): error"

rm "$dir/More.cs"
sed -i.bak -e 's/^            return count;$/\n            return count;/' "$dir/Violations.cs"
rm "$dir/Violations.cs.bak"
code=0
output=$(cd "$dir" && dotnet build -p:HouseRulesBaseline=prune 2>&1) || code=$?
check ratchet-prune passes "records [0-9]+ finding"

code=0
output=$(< "$dir/houserules.baseline")
check ratchet-prune-drops-fixed-entries passes "!\|HR0001\|" "\|RCS0056\|"

code=0
output=$(cd "$dir" && dotnet build -p:HouseRulesBaseline=init 2>&1) || code=$?
check ratchet-init-refuses-to-grow fails "already exists, and a baseline never grows"

dir=$(new_project ratchet-mode-casing Violations.cs Baseline)
code=0
output=$(cd "$dir" && dotnet build -p:HouseRulesBaseline=Init 2>&1) || code=$?
check ratchet-mode-casing passes "records [0-9]+ finding"

dir=$(new_project ratchet-unrecordable Legacy.cs Baseline)
code=0
output=$(cd "$dir" && dotnet build -p:HouseRulesBaseline=init 2>&1) || code=$?
check ratchet-init-names-what-it-cannot-record fails "LEGACY001 cannot be recorded"
[ ! -e "$dir/houserules.baseline" ] || { echo "FAIL ratchet-unrecordable: a baseline was written"; failures=$((failures + 1)); }

build "$dir"
check ratchet-failed-init-leaves-no-false-green fails "error LEGACY001:"

dir=$(new_project ratchet-multi-target Clean.cs Baseline '<TargetFrameworks>net8.0;netstandard2.0</TargetFrameworks><LangVersion>latest</LangVersion>')
sed -i.bak -e '/<TargetFramework>/d' "$dir/Sample.csproj"
rm "$dir/Sample.csproj.bak"
build "$dir"
check multi-target-builds passes "!warning [A-Z]+[0-9]+"

code=0
output=$(cd "$dir" && dotnet build -p:HouseRulesBaseline=init 2>&1) || code=$?
check ratchet-multi-target-is-refused fails "does not support projects with several target frameworks"

dir=$(new_project nested-level Violations.cs Strict)
mkdir "$dir/app"
mv "$dir/Sample.csproj" "$dir/Violations.cs" "$dir/app/"
cat > "$dir/app/Directory.Build.props" <<'EOF'
<Project>
  <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />
  <PropertyGroup>
    <HouseRulesLevel>Baseline</HouseRulesLevel>
  </PropertyGroup>
</Project>
EOF
build "$dir/app"
check nested-level fails "$(reported HR0001)" "!$(reported IDE1006)"

if [ "$failures" -ne 0 ]; then
  echo "$failures smoke check(s) failed"
  exit 1
fi

echo "All smoke checks passed"
