#!/bin/bash
# usage: mutation_run.sh <clone> <test filter regex> <result-name> <spec.json>
# Mutation check: sync the working tree into the clone, apply every mutation in the spec to the CLONE's files (never
# the working tree), run the filter once and list the failing tests. A mutation is {"id", "file", "old", "new"}: "old"
# must occur in the file (the first occurrence is replaced). A test suite that stays green under a mutation that
# changes behaviour has a gap. Several mutations in one spec must not be covered by the same tests or touch the same
# line; for one clean verdict per mutation use each_mutant.sh. Never edit the working tree's production code while a
# batch is running: every run re-syncs it.
. "$(dirname "$0")/env.sh"
[ $# -eq 4 ] || { sed -n 2,9p "$0"; exit 2; }
N=$(coo_need_clone "$1") || exit 4
coo_ensure_mcp; coo_sync "$N"
python3 - "$N" "$4" <<'PY'
import json, sys
n, spec = sys.argv[1:3]
muts = json.load(open(spec))
for m in muts:
    p = n + "/" + m["file"]
    s = open(p).read()
    if m["old"] not in s:
        print("MUTATION TARGET NOT FOUND:", m["id"]); sys.exit(3)
    open(p, "w").write(s.replace(m["old"], m["new"], 1))
print("applied", len(muts))
PY
[ $? -ne 0 ] && exit 3
coo_unity_run "$N" "$2" "$3"
