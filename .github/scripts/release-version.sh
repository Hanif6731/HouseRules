#!/usr/bin/env bash
# Prints the package version of a release tag, or fails when the tag must not be released: the tagged commit must
# be on origin/main, and the top version in its CHANGELOG.md must be the tag's.
#
#   .github/scripts/release-version.sh v1.2.0
set -euo pipefail

tag=$1
version=${tag#v}

if ! git merge-base --is-ancestor "$tag^{commit}" origin/main; then
  echo "$tag is not on main. Tag a commit that is on origin/main." >&2
  exit 1
fi

top=$(git show "$tag:CHANGELOG.md" | sed -n 's/^## \[\([^]]*\)\].*/\1/p' | grep -vix unreleased | head -n 1 || true)
if [ "$top" != "$version" ]; then
  echo "$tag does not match the top version in CHANGELOG.md (${top:-none}). Add a '## [$version]' section first." >&2
  exit 1
fi

echo "$version"
