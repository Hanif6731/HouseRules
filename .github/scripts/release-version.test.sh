#!/usr/bin/env bash
# Runs release-version.sh against a throwaway repository.
set -euo pipefail

script=$(cd "$(dirname "$0")" && pwd)/release-version.sh
work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT
failures=0

cd "$work"
git init -q -b main
git config user.name test
git config user.email test@example.invalid
git config core.autocrlf false

# release <version>: commits a CHANGELOG whose top version is <version>, under an empty Unreleased section.
release() {
  printf '# Changelog\n\n## [Unreleased]\n\n## [%s] - 2026-10-24\n\n## [0.9.0] - 2026-01-01\n' "$1" > CHANGELOG.md
  git add CHANGELOG.md
  git commit -qm "$1"
}

# check <name> <tag> <expected exit code> <expected output pattern>
check() {
  local code=0 output
  output=$("$script" "$2" 2>&1) || code=$?
  if [ "$code" -ne "$3" ] || ! grep -qE -- "$4" <<< "$output"; then
    echo "FAIL $1: exit $code, output: $output"
    failures=$((failures + 1))
  else
    echo "ok   $1"
  fi
}

release 1.2.0
git tag -a v1.2.0 -m v1.2.0
release 1.3.0
git tag v1.4.0
git update-ref refs/remotes/origin/main HEAD
git checkout -qb feature
release 2.0.0
git tag v2.0.0
git checkout -q main

check tag-on-main-matching-changelog v1.2.0 0 '^1\.2\.0$'
check tag-not-matching-changelog v1.4.0 1 "v1\.4\.0 does not match the top version in CHANGELOG\.md \(1\.3\.0\)"
check tag-not-on-main v2.0.0 1 'v2\.0\.0 is not on main'

if [ "$failures" -ne 0 ]; then
  echo "$failures release-version check(s) failed"
  exit 1
fi
