#!/usr/bin/env python3
"""usage: mutant_report.py <prefix> <spec.json> — verdict per mutation from each_mutant.sh outputs.

KILLED   at least one test failed and the build compiled (the suite notices the change)
SURVIVED nothing failed (a test gap, or a mutation that changes no behaviour)
INVALID  the build did not compile (not a real kill: rewrite the mutation)
PENDING  no output yet (the run is still going)
"""
import json
import os
import re
import sys

prefix, spec = sys.argv[1], sys.argv[2]
results = os.environ.get("COO_RESULTS", "/Users/steven/coo-verification/results")
for m in json.load(open(spec)):
    path = os.path.join(results, "%s_%s.out" % (prefix, m["id"]))
    if not os.path.exists(path):
        print("%-10s %s" % ("PENDING", m["id"])); continue
    text = open(path).read()
    compile_errors = re.search(r"compile errors: (\d+)", text)
    if "MUTATION TARGET NOT FOUND" in text:
        verdict, detail = "INVALID", "target text not found (the code changed since the spec was written)"
    elif not compile_errors:
        verdict, detail = "PENDING", "no summary yet"
    elif int(compile_errors.group(1)) > 0:
        verdict, detail = "INVALID", "%s compile errors" % compile_errors.group(1)
    else:
        failed = re.search(r"'failed': '(\d+)'", text)
        n = int(failed.group(1)) if failed else -1
        verdict = "KILLED" if n > 0 else "SURVIVED"
        names = [l.strip() for l in text.splitlines() if l.strip().startswith("FAIL")][:2]
        detail = "%s failing: %s" % (n, "; ".join(names)[:160])
    print("%-10s %-14s %s" % (verdict, m["id"], detail))
