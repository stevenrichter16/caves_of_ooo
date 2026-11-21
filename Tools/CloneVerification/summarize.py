#!/usr/bin/env python3
"""Print the totals of a Unity NUnit result XML and one line per failed test (name | first 150 characters of the message)."""
import sys
import xml.etree.ElementTree as ET

try:
    root = ET.parse(sys.argv[1]).getroot()
except Exception as e:  # missing or truncated file: the run did not finish
    print("no results", e)
    sys.exit(0)
print({k: root.get(k) for k in ("total", "passed", "failed", "inconclusive")})
for tc in root.iter("test-case"):
    if tc.get("result") == "Failed":
        m = tc.find("failure/message")
        text = ((m.text or "") if m is not None else "").strip().replace("\n", " ")[:150]
        print(" FAIL", tc.get("fullname").split("Tests.")[-1][:100], "|", text)
