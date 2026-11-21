#!/bin/bash
# usage: each_mutant.sh <clone> <spec.json> <test filter regex> <prefix>
# One clean Unity run per mutation in the spec (the clone is re-synced before each, so mutations never combine), then a
# verdict table. Outputs: $COO_RESULTS/<prefix>_<id>.out (+ .xml/.log). Run it in the background (a run is 2-4 minutes);
# the table is printed when it ends and is rebuilt any time with: mutant_report.py <prefix> <spec.json>
# A mutant whose run had compile errors is INVALID, not killed (rewrite it). A survivor is a test gap or an equivalent
# mutant: decide which, and close real gaps with a new test.
. "$(dirname "$0")/env.sh"
[ $# -eq 4 ] || { sed -n 2,8p "$0"; exit 2; }
CLONE="$1"; SPEC="$2"; FILTER="$3"; PREFIX="$4"
for id in $(python3 -c "import json,sys;print(' '.join(m['id'] for m in json.load(open(sys.argv[1]))))" "$SPEC"); do
  python3 - "$SPEC" "$id" "$COO_RESULTS/one_$PREFIX.json" <<'PY'
import json, sys
spec = json.load(open(sys.argv[1]))
json.dump([m for m in spec if m["id"] == sys.argv[2]], open(sys.argv[3], "w"))
PY
  "$COO_TOOLS/mutation_run.sh" "$CLONE" "$FILTER" "${PREFIX}_$id" "$COO_RESULTS/one_$PREFIX.json" > "$COO_RESULTS/${PREFIX}_$id.out" 2>&1
done
python3 "$COO_TOOLS/mutant_report.py" "$PREFIX" "$SPEC"
