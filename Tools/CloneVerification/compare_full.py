#!/usr/bin/env python3
"""usage: compare_full.py <new.xml> [baseline.xml] — compare an EditMode result with the baseline.

The baseline defaults to $COO_BASELINE (/Users/steven/coo-verification/baselines/full_s9.xml: 39,500 tests, 91 known
failures, taken at 2026-11-19). A NEW failure is one that is not in the baseline. Timeouts under CPU contention (two Unity
runs at once) are the usual false alarms: the table marks them, and the rule is to re-run every new failure ALONE
(clone_run.sh with its exact class filter) before believing it. A failure that also fails alone is a regression.
Missing, skipped or inconclusive tests are not reported as fixes.
"""
import os
import sys
import xml.etree.ElementTree as ET


def load(path):
    root = ET.parse(path).getroot()
    out = {}
    for tc in root.iter("test-case"):
        name = tc.get("fullname")
        if not name or name in out:
            raise ValueError(f"Missing or duplicate test identity in {path}: {name!r}")
        out[name] = (tc.get("result"), tc.findtext("failure/message", default="")[:200])
    return root, out


new_path = sys.argv[1]
base_path = sys.argv[2] if len(sys.argv) > 2 else os.environ.get("COO_BASELINE", "/Users/steven/coo-verification/baselines/full_s9.xml")
rn, new = load(new_path)
rb, base = load(base_path)
print("new:  total", rn.get("total"), "passed", rn.get("passed"), "failed", rn.get("failed"))
print("base: total", rb.get("total"), "passed", rb.get("passed"), "failed", rb.get("failed"))
baseline_failures = {name for name, (result, _) in base.items() if result == "Failed"}
current_failures = {name for name, (result, _) in new.items() if result == "Failed"}
only_new = sorted(current_failures - baseline_failures)
fixed = sorted(name for name in baseline_failures & new.keys() if new[name][0] == "Passed")
absent = sorted(baseline_failures - new.keys())
unresolved = sorted(name for name in baseline_failures & new.keys() if new[name][0] not in ("Passed", "Failed"))
print("NEW FAILURES (not in the baseline):", len(only_new))
for n in only_new:
    msg = new[n][1].replace("\n", " ")
    tag = "  [TIMEOUT: re-run alone]" if "increase the timeout value" in msg else ""
    print("  ", n, "|", msg[:140], tag)
print("baseline failures that now pass:", len(fixed))
for n in fixed[:30]:
    print("  ", n)
print("baseline failures absent from this result:", len(absent))
print("baseline failures with another non-passing result:", len(unresolved))
for n in unresolved:
    print("  ", n, "|", new[n][0])
