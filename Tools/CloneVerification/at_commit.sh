#!/bin/bash
# usage: at_commit.sh <clone> <commit> <test filter regex> <result-name>
# Run tests against what a given commit contained (bisecting a failure, or checking whether it already failed before
# your change): export the commit's Assets/Packages/ProjectSettings/Docs, sync them into the clone, run the filter.
# The working tree is not touched; the next clone_run.sh re-syncs it into the clone.
. "$(dirname "$0")/env.sh"
[ $# -eq 4 ] || { sed -n 2,6p "$0"; exit 2; }
N=$(coo_need_clone "$1") || exit 4
EXPORT="$COO_RESULTS/export_$$"; rm -rf "$EXPORT"; mkdir -p "$EXPORT"
git -C "$COO_MAIN" archive "$2" Assets Packages ProjectSettings Docs | tar -x -C "$EXPORT"
coo_ensure_mcp
for d in Assets Packages ProjectSettings Docs; do rsync -a --delete --exclude 'InitTestScene*' "$EXPORT/$d/" "$N/$d/"; done
rm -rf "$EXPORT"
coo_unity_run "$N" "$3" "$4"
