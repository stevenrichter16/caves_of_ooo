#!/usr/bin/env python3
"""Audit authored examine prose; this is source coverage, not a spawn simulation.

Matches BlueprintLoader's parent-first part/parameter merge and the final
Text/Description alias assignment. A separate receipt can compare parsed objects
and reject every change outside Examinable.Text/Description on the manifest.
"""
import argparse
import collections
import copy
import hashlib
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_OBJECTS = ROOT / "Assets/Resources/Content/Blueprints/Objects.json"
DEFAULT_MANIFEST = ROOT / "Docs/Verification/ExamineDescriptions/proposed-descriptions.json"


def objects(path):
    data = json.loads(Path(path).read_text())["Objects"]
    result = {entry["Name"]: entry for entry in data}
    if len(result) != len(data):
        raise ValueError("Duplicate blueprint names")
    return result


def resolve(data):
    cache, pending = {}, set()

    def visit(name):
        if name in cache:
            return cache[name]
        if name in pending:
            raise ValueError("Inheritance cycle at " + name)
        pending.add(name)
        entry = data[name]
        parent = entry.get("Inherits")
        parts = copy.deepcopy(visit(parent)) if parent else {}
        for part in entry.get("Parts", []):
            parts.setdefault(part["Name"], {}).update(
                (pair["Key"], pair["Value"]) for pair in part.get("Params", []))
        cache[name] = parts
        pending.remove(name)
        return parts

    for name in data:
        visit(name)
    return cache


def text_of(parts):
    # Both keys bind the same runtime field. Last insertion-order assignment wins.
    result = ""
    for key, value in parts.get("Examinable", {}).items():
        if key in ("Text", "Description"):
            result = value or ""
    return result.strip()


def references(data):
    result = collections.defaultdict(list)
    roots = (ROOT / "Assets/Scripts/Gameplay/World", ROOT / "Assets/Resources/Content/Data")
    for root in roots:
        for path in sorted(root.rglob("*")):
            if path.suffix not in (".cs", ".json"):
                continue
            # Visual registrations and definition registries are not source evidence.
            if path.name in ("EntityVisuals.json", "Factions.json"):
                continue
            for number, line in enumerate(path.read_text().splitlines(), 1):
                for name in re.findall(r'"([^"\n]+)"', line):
                    if name in data:
                        value = str(path.relative_to(ROOT)) + ":" + str(number)
                        if value not in result[name]:
                            result[name].append(value)
    return result


def without_prose(entry):
    result = copy.deepcopy(entry)
    parts = []
    for part in result.get("Parts", []):
        if part["Name"] == "Examinable":
            part["Params"] = [p for p in part.get("Params", [])
                              if p["Key"] not in ("Text", "Description")]
            if not part["Params"]:
                continue
        parts.append(part)
    result["Parts"] = parts
    return result


def compare(before, after, expected):
    if list(before) != list(after):
        raise AssertionError("Blueprint identities/order changed")
    changed = [name for name in before if before[name] != after[name]]
    if set(changed) != set(expected):
        raise AssertionError("Changed blueprint set differs from manifest")
    for name in changed:
        if without_prose(before[name]) != without_prose(after[name]):
            raise AssertionError("Non-description field changed: " + name)
    return {"changed_count": len(changed), "unchanged_count": len(before) - len(changed),
            "changed_blueprints": changed, "allowed_fields_only": True}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--objects", type=Path, default=DEFAULT_OBJECTS)
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--expect-authored", action="store_true")
    parser.add_argument("--compare-before", type=Path)
    args = parser.parse_args()
    data = objects(args.objects)
    baked = resolve(data)
    manifest = json.loads(args.manifest.read_text())
    refs = references(data)
    children = collections.Counter(x.get("Inherits") for x in data.values())
    rows, failures = [], []
    for name, parts in baked.items():
        display = parts.get("Render", {}).get("DisplayName", "")
        prose = text_of(parts)
        if name in manifest:
            expected = manifest[name]["text"]
            if children[name]:
                failures.append(name + ": target has children")
            if not expected.isascii() or not 25 <= len(expected) <= 240 or "\n" in expected:
                failures.append(name + ": invalid proposed text")
            if not refs[name]:
                failures.append(name + ": no production source reference")
            if args.expect_authored and prose != expected:
                failures.append(name + ": expected authored text is missing/different")
        rows.append({"name": name, "display_name": display, "parent": data[name].get("Inherits"),
                     "authored": bool(prose), "selected": name in manifest,
                     "source_references": refs[name], "resolved_text": prose})
    missing_targets = set(manifest) - set(data)
    failures.extend(name + ": missing blueprint" for name in sorted(missing_targets))
    report = {
        "scope": "Exact-token world/data source references; not live spawn or acquisition coverage.",
        "objects_sha256": hashlib.sha256(args.objects.read_bytes()).hexdigest(),
        "counts": {"blueprints": len(data), "named": sum(bool(r["display_name"]) for r in rows),
                   "named_missing_prose": sum(bool(r["display_name"]) and not r["authored"] for r in rows),
                   "referenced_named_missing_prose": sum(bool(r["display_name"]) and not r["authored"] and bool(r["source_references"]) for r in rows),
                   "selected": len(manifest),
                   "selected_authored": sum(r["selected"] and r["authored"] for r in rows)},
        "failures": failures, "blueprints": rows}
    if args.compare_before:
        report["parsed_diff"] = compare(objects(args.compare_before), data, manifest)
        report["baseline_sha256"] = hashlib.sha256(args.compare_before.read_bytes()).hexdigest()
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({"counts": report["counts"], "failures": failures,
                      "parsed_diff": report.get("parsed_diff")}, indent=2))
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
