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
    <PackageReference Include="HouseRules.Analyzers" Version="1.0.0-preview.5" PrivateAssets="all" />
  </ItemGroup>
</Project>
EOF
    sed -i.bak -e "s|<HouseRulesLevel>Strict<|<HouseRulesLevel>$3<|" -e "s|Version=\"[^\"]*\"|Version=\"$version\"|" \
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
  "$(reported CA2264)" "!$(reported CA1874)" "!$(reported IDE1006)" "!$(reported CS8602)" "!$(reported RS0030)" \
  "!$(reported HR0002)"

dir=$(new_project recommended Violations.cs Recommended)
build "$dir"
check recommended fails "$(reported HR0001)" "$(reported IDE1006)" "!'DateTime.UtcNow' is banned" \
  "$(reported CA1874)" "$(reported IDE0051)" "!$(reported CS8602)" "!$(reported RS0030)" "$(reported HR0002)"

dir=$(new_project strict Violations.cs Strict)
build "$dir"
check strict fails "$(reported HR0001)" "$(reported IDE1006)" "$(reported CS8602)" "'DateTime.UtcNow' is banned" \
  "$(reported CA5351)" "!'MD5' is banned" "$(reported HR0002)"

dir=$(new_project lowercase-level Violations.cs strict)
build "$dir"
check lowercase-level fails "$(reported HR0001)" "$(reported IDE1006)" "$(reported CS8602)"

# IDE0005 needs the documentation file; a project that turns it off loses IDE0005 instead of failing every build.
dir=$(new_project doc-file-off UnusedUsing.cs Recommended '<GenerateDocumentationFile>false</GenerateDocumentationFile>')
build "$dir"
check doc-file-off passes "!warning [A-Z]+[0-9]+" "!EnableGenerateDocumentationFile"

dir=$(new_project unused-using UnusedUsing.cs Recommended)
build "$dir"
check unused-using fails "$(reported IDE0005)"

dir=$(new_project null-check-in-query Query.cs Recommended)
build "$dir"
check null-check-in-query passes "!$(reported IDE0031)"

dir=$(new_project brand-namespace Brand.cs Recommended)
build "$dir"
check brand-namespace passes "!warning [A-Z]+[0-9]+"

sed -e 's/class Brand/class lowercaseType/' "$here/sample/Brand.cs" > "$dir/Brand.cs"
build "$dir"
check lowercase-type-name fails "$(reported IDE1006)"

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

# Directory.Build.props is read before the package's defaults, so this opt-out depends on them being conditional.
dir=$(new_project warnings-not-errors-from-props Violations.cs Baseline)
sed -i.bak -e 's|^    <HouseRulesLevel>Baseline</HouseRulesLevel>$|&\n    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>|' \
  "$dir/Directory.Build.props"
rm "$dir/Directory.Build.props.bak"
build "$dir"
check warnings-not-errors-from-props passes "warning HR0001:" "!error [A-Z]+[0-9]+"

dir=$(new_project wall-clock-ban-off Violations.cs Strict '<HouseRulesBanWallClock>false</HouseRulesBanWallClock>')
build "$dir"
check wall-clock-ban-off fails "!'DateTime.UtcNow' is banned" "$(reported CA5351)"

dir=$(new_project format-fixes-layout Violations.cs Baseline)
(cd "$dir" && dotnet format > /dev/null 2>&1) || true
build "$dir"
check format-fixes-layout fails "!$(reported HR0001)" "!$(reported RCS0056)" "!$(reported IDE0161)" \
  "!$(reported IDE0055)" "$(reported CA5351)"

# dotnet format fixes only what it found before its first fix, so a statement it wraps gets its HR0001 blank line on
# the next run; two runs must leave nothing for a third.
dir=$(new_project format-twice-converges Wrap.cs Baseline)
(cd "$dir" && dotnet format > /dev/null 2>&1) || true
(cd "$dir" && dotnet format > /dev/null 2>&1) || true
code=0
output=$(cd "$dir" && dotnet format --verify-no-changes 2>&1) || code=$?
check format-twice-converges passes

build "$dir"
check format-twice-fixes-wrapped-statements passes "!$(reported HR0001)" "!$(reported RCS0056)"

# HR0002 splits a packed list in one dotnet format run and leaves nothing for RCS0053, HR0001 or a second run.
dir=$(new_project hr0002-format-converges Packed.cs Recommended)
(cd "$dir" && dotnet format > /dev/null 2>&1) || true
code=0
output=$(cd "$dir" && dotnet format --verify-no-changes 2>&1) || code=$?
check hr0002-format-converges passes

build "$dir"
check hr0002-format-fixes-packed-lists passes "!$(reported HR0002)" "!$(reported RCS0053)" "!$(reported HR0001)"

# RCS0053 measures indentation in 4-space steps without any .editorconfig (a Docker build context, say), as with
# indent_size = 4; a consumer's own .editorconfig still decides.
dir=$(new_project indent-without-editorconfig BaseList.cs Baseline)
build "$dir"
check indent-without-editorconfig passes "!warning [A-Z]+[0-9]+"

printf 'root = true\n\n[*.cs]\nindent_size = 2\n' > "$dir/.editorconfig"
build "$dir"
check indent-from-editorconfig-wins fails "$(reported RCS0053)"

dir=$(new_project level-in-project-file Clean.cs Baseline '<HouseRulesLevel>Strict</HouseRulesLevel>')
build "$dir"
check level-in-project-file fails "Set HouseRulesLevel in Directory.Build.props"

dir=$(new_project invalid-level Clean.cs Strictest)
build "$dir"
check invalid-level fails "Set it to Baseline, Recommended or Strict"

dir=$(new_project add-package Clean.cs "")
(cd "$dir" && dotnet add package HouseRules.Analyzers --prerelease > /dev/null)
build "$dir"
check add-package passes "!warning [A-Z]+[0-9]+"

code=0
output=$(< "$dir/Sample.csproj")
check add-package-takes-the-packed-version passes "Include=\"HouseRules.Analyzers\" Version=\"${version//./\\.}\""

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

build "$dir"
check ratchet-pruned-baseline-passes passes "!error [A-Z]+[0-9]+"

code=0
output=$(cd "$dir" && dotnet build -p:HouseRulesBaseline=init 2>&1) || code=$?
check ratchet-init-refuses-to-grow fails "already exists, and a baseline never grows"

# A finding suppressed in source after init no longer needs its entry: prune drops it.
dir=$(new_project ratchet-prune-source-suppressed Violations.cs Baseline)
code=0
output=$(cd "$dir" && dotnet build -p:HouseRulesBaseline=init 2>&1) || code=$?
check ratchet-source-suppressed-init passes "records [0-9]+ finding"

{ echo '#pragma warning disable HR0001'; cat "$here/sample/Violations.cs"; } > "$dir/Violations.cs"
code=0
output=$(cd "$dir" && dotnet build -p:HouseRulesBaseline=prune 2>&1) || code=$?
check ratchet-prune-source-suppressed passes "records [0-9]+ finding"

code=0
output=$(< "$dir/houserules.baseline")
check ratchet-prune-drops-source-suppressed-entries passes "!\|HR0001\|" "\|RCS0056\|"

# -p:HouseRulesBaseline=prune reaches every project of a solution and every project reference; one without a
# baseline has nothing to prune and builds as usual.
dir=$(new_project ratchet-prune-solution Violations.cs Baseline)
mkdir "$dir/app" "$dir/lib"
mv "$dir/Sample.csproj" "$dir/Violations.cs" "$dir/app/"
cp "$dir/app/Sample.csproj" "$dir/lib/Lib.csproj"
cp "$here/sample/Clean.cs" "$dir/lib/"
sed -i.bak -e 's|^</Project>$|  <ItemGroup>\n    <ProjectReference Include="../lib/Lib.csproj" />\n  </ItemGroup>\n</Project>|' \
  "$dir/app/Sample.csproj"
rm "$dir/app/Sample.csproj.bak"
(cd "$dir" && dotnet new sln -n Sample > /dev/null && dotnet sln add app/Sample.csproj lib/Lib.csproj > /dev/null)
code=0
output=$(cd "$dir/app" && dotnet build -p:HouseRulesBaseline=init 2>&1) || code=$?
check ratchet-solution-init passes "records [0-9]+ finding"

code=0
output=$(cd "$dir" && dotnet build -p:HouseRulesBaseline=prune 2>&1) || code=$?
check ratchet-prune-solution passes "records [0-9]+ finding" "nothing to prune"

code=0
output=$(cd "$dir/app" && dotnet build -p:HouseRulesBaseline=prune 2>&1) || code=$?
check ratchet-prune-project-with-reference passes "records [0-9]+ finding" "nothing to prune"

dir=$(new_project ratchet-mode-casing Violations.cs Baseline)
code=0
output=$(cd "$dir" && dotnet build -p:HouseRulesBaseline=Init 2>&1) || code=$?
check ratchet-mode-casing passes "records [0-9]+ finding"

dir=$(new_project ratchet-info-level InfoFinding.cs Baseline)
code=0
output=$(cd "$dir" && dotnet build -p:HouseRulesBaseline=init 2>&1) || code=$?
check ratchet-init-skips-info-findings passes "records 1 finding"

code=0
output=$(grep -v '^#' "$dir/houserules.baseline" | cut -d'|' -f2 | sort -u | tr '\n' ' ') || true
check ratchet-info-baseline-lists-hr0001-only passes "^HR0001 $"

build "$dir"
check ratchet-info-baselined-passes passes "!error [A-Z]+[0-9]+"

dir=$(new_project ratchet-linked-file Clean.cs Baseline)
mkdir "$dir/app" "$dir/shared"
mv "$dir/Sample.csproj" "$dir/Clean.cs" "$dir/app/"
cp "$here/sample/Violations.cs" "$dir/shared/"
sed -i.bak -e 's|^</Project>$|  <ItemGroup>\n    <Compile Include="../shared/Violations.cs" />\n  </ItemGroup>\n</Project>|' \
  "$dir/app/Sample.csproj"
rm "$dir/app/Sample.csproj.bak"
code=0
output=$(cd "$dir/app" && dotnet build -p:HouseRulesBaseline=init 2>&1) || code=$?
check ratchet-linked-file-init passes "records [0-9]+ finding"

code=0
output=$(grep -v '^#' "$dir/app/houserules.baseline" | cut -d'|' -f1 | sort -u | tr '\n' ' ') || true
check ratchet-linked-file-path-is-relative passes "^\.\./shared/Violations\.cs $"

build "$dir/app"
check ratchet-linked-file-baselined-passes passes "!error [A-Z]+[0-9]+"

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

dir=$(new_project both-target-properties Clean.cs Baseline '<TargetFrameworks>net8.0;net10.0</TargetFrameworks>')
build "$dir"
check both-target-properties fails "sets both TargetFramework \(net10.0\) and TargetFrameworks \(net8.0;net10.0\)" \
  "!HouseRulesLevel changes"

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
