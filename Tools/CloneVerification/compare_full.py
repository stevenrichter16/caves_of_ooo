#!/usr/bin/env python3
"""usage: compare_full.py <new.xml> [baseline.xml] — compare a full EditMode run with the baseline.

The baseline defaults to $COO_BASELINE (/Users/steven/coo-verification/baselines/full_s9.xml: 39,500 tests, 91 known
failures, taken at 2026-11-19). A NEW failure is one that is not in the baseline. Timeouts under CPU contention (two Unity
runs at once) are the usual false alarms: the table marks them, and the rule is to re-run every new failure ALONE
(clone_run.sh with its exact class filter) before believing it. A failure that also fails alone is a regression.
"""
import os
import sys
import xml.etree.ElementTree as ET


def load(path):
    root = ET.parse(path).getroot()
    out = {}
    for tc in root.iter("test-case"):
        if tc.get("result") == "Failed":
            m = tc.find("failure/message")
            out[tc.get("fullname")] = ((m.text or "") if m is not None else "")[:200]
    return root, out


new_path = sys.argv[1]
base_path = sys.argv[2] if len(sys.argv) > 2 else os.environ.get("COO_BASELINE", "/Users/steven/coo-verification/baselines/full_s9.xml")
rn, new = load(new_path)
rb, base = load(base_path)
print("new:  total", rn.get("total"), "passed", rn.get("passed"), "failed", rn.get("failed"))
print("base: total", rb.get("total"), "passed", rb.get("passed"), "failed", rb.get("failed"))
only_new = sorted(set(new) - set(base))
fixed = sorted(set(base) - set(new))
print("NEW FAILURES (not in the baseline):", len(only_new))
for n in only_new:
    msg = new[n].replace("\n", " ")
    tag = "  [TIMEOUT: re-run alone]" if "increase the timeout value" in msg else ""
    print("  ", n, "|", msg[:140], tag)
print("baseline failures that now pass:", len(fixed))
for n in fixed[:30]:
    print("  ", n)
