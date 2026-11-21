#!/bin/bash
# usage: new_clone.sh <new-name> [from-clone]
# Make another warm verification clone with an APFS copy-on-write copy (about 20 s, almost no extra disk until files
# change; each clone is ~13 GB nominal). The copy inherits the source clone's Library cache, so the first Unity run in
# it does not have to import the project from scratch. Do not copy a clone that has a Unity run in progress.
. "$(dirname "$0")/env.sh"
[ $# -ge 1 ] || { sed -n 2,5p "$0"; exit 2; }
FROM="$COO_CLONES/${2:-clone}"
[ -d "$FROM/Assets" ] || { echo "no such clone: $FROM" >&2; exit 4; }
coo_busy "$FROM" && { echo "source clone is busy; copy an idle one" >&2; exit 4; }
[ -e "$COO_CLONES/$1" ] && { echo "$COO_CLONES/$1 already exists" >&2; exit 4; }
cp -cR "$FROM" "$COO_CLONES/$1" && echo "made $COO_CLONES/$1"
