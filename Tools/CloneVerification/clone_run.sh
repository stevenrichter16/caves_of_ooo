#!/bin/bash
# usage: clone_run.sh <clone> <test filter regex> <result-name>
# Sync the working tree into the clone and run the matching EditMode tests there. Results: $COO_RESULTS/<result-name>.xml/.log
# Use a NEW result-name every time (an old name is overwritten and a stale file reads as a fresh result).
# Examples:  clone_run.sh clone3 'CavesOfOoo.Tests.WorldToolRowsTests' tools1
#            clone_run.sh clone2 'CavesOfOoo' fullrun_h       # the whole suite: 2.8 to 3.4 hours
. "$(dirname "$0")/env.sh"
[ $# -eq 3 ] || { sed -n 2,6p "$0"; exit 2; }
N=$(coo_need_clone "$1") || exit 4
coo_ensure_mcp; coo_sync "$N"; coo_unity_run "$N" "$2" "$3"
