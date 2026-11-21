#!/bin/bash
# usage: red_run.sh <clone> <test filter regex> <result-name> <repo-relative file>...
# RED proof for a change that is already written: sync the working tree into the clone, then restore the listed files
# from git HEAD there, and run the tests. The new tests must FAIL against the old production files (and the run must
# compile; a compile error is not a RED). Only the clone is touched.
. "$(dirname "$0")/env.sh"
[ $# -ge 4 ] || { sed -n 2,6p "$0"; exit 2; }
N=$(coo_need_clone "$1") || exit 4
FILTER="$2"; NAME="$3"; shift 3
coo_ensure_mcp; coo_sync "$N"
for f in "$@"; do git -C "$COO_MAIN" show "HEAD:$f" > "$N/$f" || { echo "cannot restore $f from HEAD (new file? delete it in the clone instead)" >&2; exit 3; }; done
coo_unity_run "$N" "$FILTER" "$NAME"
