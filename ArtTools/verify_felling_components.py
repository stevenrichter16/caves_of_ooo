#!/usr/bin/env python3
"""Verify the current Felling component build without regenerating any artwork.

Runs Python/Node tests, strict PNG checks, live-source provenance, independent RGBA
reassembly, and exact atlas checks. Writes raw logs and aggregate JSON under
ArtSource/FellingSite/Components/reports. A failed or skipped required check exits1.
"""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess
import sys
import time
from urllib.parse import unquote

try:
    from .felling_components_contract import validate_manifest
except ImportError:
    from felling_components_contract import validate_manifest


REPO = Path(__file__).resolve().parents[1]
PACKAGE = REPO / "ArtSource/FellingSite/Components"


def sha256(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def utc_now():
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


def load_json(path):
    def pairs(items):
        result = {}
        for key, value in items:
            if key in result:
                raise ValueError(f"Duplicate JSON key: {key}")
            result[key] = value
        return result

    def nonfinite(value):
        raise ValueError("Nonfinite JSON number: " + value)

    return json.loads(Path(path).read_text(), object_pairs_hook=pairs, parse_constant=nonfinite)


def confined(build, name):
    if (not isinstance(name, str) or not name or name != name.strip()
            or any(char in name for char in ("\\", "\x00", ":"))
            or PurePosixPath(name).is_absolute() or unquote(name) != name
            or any(part in ("", ".", "..") for part in name.split("/"))):
        raise ValueError(f"Unsafe build-relative path: {name!r}")
    path = (build / name).resolve()
    path.relative_to(build.resolve())
    return path


def require(condition, message):
    if not condition:
        raise ValueError(message)


def strict_assets(build):
    manifest = load_json(build / "manifest.json")
    report = validate_manifest(manifest, build, require_assets=True)
    require(report["ok"], "; ".join(report["errors"]))
    return report


def current_provenance(build, sources):
    """Compare current inputs, their archived copies, and the actor-patch recipe."""
    import numpy as np
    from PIL import Image

    manifest = load_json(build / "manifest.json")
    structural = validate_manifest(manifest, build)
    require(structural["ok"], "; ".join(structural["errors"]))
    verification = load_json(confined(build, manifest["report"]["path"]))
    declared = verification["sources"]
    checks = []
    for key, path in sources.items():
        actual = sha256(path)
        expected = declared.get(key)
        require(expected == actual, f"Stale build: {key} differs from current input {path}")
        checks.append({"field": key, "path": str(path), "sha256": actual})
    require(manifest["reference"]["sha256"] == declared["referenceSha256"], "Manifest/reference provenance mismatch")
    require(sha256(confined(build, manifest["reference"]["path"])) == declared["referenceSha256"], "Archived reference differs from live reference")
    backing = manifest.get("backingSource")
    require(isinstance(backing, dict) and backing.get("id") == "generated-backing", "Missing archived generated-backing provenance")
    require(backing.get("sha256") == declared["backingSha256"], "Backing record does not describe the current input")
    require(sha256(confined(build, backing.get("path"))) == declared["backingSha256"], "Archived backing differs from actual backing input")
    rect = declared.get("actorRemovalPatchRect")
    require(rect == [736, 800, 80, 96] and all(type(v) is int for v in rect), "Actor-removal patch recipe changed or is missing")
    with Image.open(sources["referenceSha256"]) as image:
        expected_baseline = np.array(image.convert("RGB"))
    with Image.open(sources["actorRemovalPatchSourceSha256"]) as image:
        patch = np.array(image.convert("RGB"))
    require(patch.shape == expected_baseline.shape == (1024, 1536, 3), "Reference/actor patch dimensions changed")
    x, y, width, height = rect
    expected_baseline[y:y + height, x:x + width] = patch[y:y + height, x:x + width]
    with Image.open(confined(build, manifest["baseline"]["path"])) as image:
        baseline = np.array(image.convert("RGB"))
    require(np.array_equal(expected_baseline, baseline), "Baseline does not equal the documented original-plus-actor-patch recipe")
    return {"ok": True, "sources": checks, "actorRemovalPatchRect": rect, "baselineRecipeExact": True}


def reassembly(build):
    """Render exported files independently of the extraction tool's in-memory masks."""
    import numpy as np
    from PIL import Image

    manifest = load_json(build / "manifest.json")
    structural = validate_manifest(manifest, build)
    require(structural["ok"], "; ".join(structural["errors"]))
    verification = load_json(confined(build, manifest["report"]["path"]))
    with Image.open(confined(build, manifest["base"]["path"])) as image:
        intact = image.convert("RGBA")
        removed = intact.copy()
    components = sorted(manifest["components"], key=lambda c: (c["depth"], c["id"]))
    for component in components:
        if not component["contributesToComposite"] or not component["defaultVisible"]:
            continue
        x, y, width, height = component["bounds"]
        pivot = component["pivot"]
        calculated_foot = [x + pivot[0] * width, y + (1 - pivot[1]) * height]
        require(all(abs(a - b) <= 1e-8 for a, b in zip(calculated_foot, component["foot"])),
                "Foot/pivot placement mismatch: " + component["id"])
        for asset in (component["contact"], component["sprite"]):
            if asset is None:
                continue
            with Image.open(confined(build, asset["path"])) as image:
                rgba = image.convert("RGBA")
                intact.alpha_composite(rgba, (x, y))
                if not component["mutable"]:
                    removed.alpha_composite(rgba, (x, y))
    with Image.open(confined(build, manifest["baseline"]["path"])) as image:
        target = np.array(image.convert("RGBA"))
    intact_array = np.array(intact)
    require(intact_array.shape == target.shape, "Reassembly canvas size mismatch")
    changed = int(np.any(intact_array != target, axis=2).sum())
    require(changed == 0, f"Exported RGBA reconstruction differs at {changed} pixels")
    with Image.open(build / "reconstructed.png") as image:
        require(np.array_equal(intact_array, np.array(image.convert("RGBA"))), "Saved reconstructed.png is stale")
    clean_path = confined(build, verification["allMutableRemoved"]["path"])
    with Image.open(clean_path) as image:
        clean = np.array(image.convert("RGBA"))
    removed_array = np.array(removed)
    require(np.array_equal(removed_array, clean), "Removing all mutable exports does not reproduce cleaned-environment.png")
    require(verification["intactReassembly"]["passed"] is True
            and verification["intactReassembly"]["changedPixels"] == changed,
            "Saved reassembly report contradicts current exported pixels")
    return {"ok": True, "changedPixels": changed, "intactRGBAExact": True,
            "allMutableRemovedExact": True, "pivotFootConsistency": True}


def atlas_check(build):
    import numpy as np
    from PIL import Image

    manifest = load_json(build / "manifest.json")
    structural = validate_manifest(manifest, build)
    require(structural["ok"], "; ".join(structural["errors"]))
    data = load_json(build / "prop-atlas.json")
    require(type(data.get("schemaVersion")) is int and data["schemaVersion"] == 1, "Unsupported atlas schema")
    require(type(data.get("pixelsPerUnit")) is int and data["pixelsPerUnit"] == 32, "Atlas must use32 PPU")
    atlas_path = confined(build, data.get("path"))
    require(sha256(atlas_path) == data.get("sha256"), "Atlas PNG hash mismatch")
    with Image.open(atlas_path) as image:
        require(image.format == "PNG" and image.mode == "RGBA", "Atlas must be an RGBA PNG")
        atlas = image.copy()
    require(data.get("size") == list(atlas.size), "Atlas dimensions differ from its metadata")
    expected = {c["id"]: c for c in manifest["components"] if c["mutable"]}
    records = data.get("entries")
    require(isinstance(records, list), "Atlas entries must be a list")
    seen = set()
    occupied = np.zeros((atlas.height, atlas.width), dtype=bool)
    for entry in records:
        require(isinstance(entry, dict), "Atlas entry must be an object")
        component_id = entry.get("id")
        require(isinstance(component_id, str) and component_id in expected and component_id not in seen,
                f"Duplicate/unknown atlas ID: {component_id!r}")
        seen.add(component_id)
        component = expected[component_id]
        rect = entry.get("sourceRectTopLeft")
        require(isinstance(rect, list) and len(rect) == 4 and all(type(v) is int for v in rect), "Invalid atlas rectangle: " + component_id)
        x, y, width, height = rect
        require(x >= 0 and y >= 0 and width > 0 and height > 0 and x + width <= atlas.width and y + height <= atlas.height,
                "Out-of-bounds atlas rectangle: " + component_id)
        require(not occupied[y:y + height, x:x + width].any(), "Overlapping atlas rectangles: " + component_id)
        occupied[y:y + height, x:x + width] = True
        require([width, height] == component["bounds"][2:], "Atlas crop resized the component: " + component_id)
        require(entry.get("unityRectBottomLeft") == [x, atlas.height - y - height, width, height], "Incorrect Unity atlas Y: " + component_id)
        require(entry.get("pivot") == component["pivot"], "Atlas pivot drift: " + component_id)
        sprite_path = confined(build, component["sprite"]["path"])
        require(entry.get("sourceSpriteSha256") == component["sprite"]["sha256"] == sha256(sprite_path),
                "Atlas source sprite hash mismatch: " + component_id)
        with Image.open(sprite_path) as image:
            source = np.array(image.convert("RGBA"))
        require(np.array_equal(np.array(atlas.crop((x, y, x + width, y + height))), source),
                "Atlas changed source sprite pixels: " + component_id)
    require(seen == set(expected), "Atlas omitted mutable component IDs: " + ", ".join(sorted(set(expected) - seen)))
    require(not np.array(atlas)[~occupied].any(), "Atlas contains undeclared pixels outside its sprite rectangles")
    return {"ok": True, "spriteCount": len(seen), "size": list(atlas.size),
            "exactRGBA": True, "uniqueCompleteIds": True, "bottomLeftRectanglesCorrect": True}


def counts_for(name, output):
    if name == "python-tests":
        found = re.search(r"Ran (\d+) tests?", output)
        return {"total": int(found.group(1))} if found else None
    if name == "node-tests":
        counts = {key: int(value) for key, value in re.findall(
            r"^# (tests|pass|fail|skipped) (\d+)\s*$", output, re.MULTILINE)}
        return counts or None
    return None


def run_command(name, command, reports, timeout):
    started = time.monotonic()
    result = {"name": name, "command": command, "log": name + ".log", "startedAt": utc_now()}
    try:
        process = subprocess.run(command, cwd=REPO, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                                 text=True, errors="replace", timeout=timeout, check=False)
        output = process.stdout
        result.update(status="passed" if process.returncode == 0 else "failed", exitCode=process.returncode)
        counts = counts_for(name, output)
        if counts:
            result["tests"] = counts
        if result["status"] == "passed" and (not counts or counts.get("total", counts.get("tests", 0)) <= 0):
            result.update(status="failed", error="Test command returned no nonzero test summary")
        if counts and (counts.get("skipped", 0) or counts.get("fail", 0)):
            result.update(status="failed", error="A required test failed or was skipped")
        if name == "python-tests" and re.search(r"\bskipped=\d+", output):
            result.update(status="failed", error="Python suite skipped a required test")
    except (OSError, subprocess.TimeoutExpired) as exc:
        output = getattr(exc, "stdout", "") or ""
        if isinstance(output, bytes):
            output = output.decode("utf-8", errors="replace")
        output += "\nRUNNER ERROR: " + str(exc) + "\n"
        result.update(status="failed", exitCode=None, error=str(exc))
    result["durationSeconds"] = round(time.monotonic() - started, 3)
    (reports / result["log"]).write_text(output)
    print(f"{name}: {result['status'].upper()}", flush=True)
    return result


def run_check(name, function, reports):
    started = time.monotonic()
    result = {"name": name, "log": name + ".log", "startedAt": utc_now()}
    try:
        findings = function()
        result.update(status="passed", findings=findings)
        output = json.dumps(findings, indent=2, allow_nan=False) + "\n"
    except Exception as exc:
        result.update(status="failed", error=f"{type(exc).__name__}: {exc}")
        output = "CHECK FAILED: " + result["error"] + "\n"
    result["durationSeconds"] = round(time.monotonic() - started, 3)
    (reports / result["log"]).write_text(output)
    print(f"{name}: {result['status'].upper()}", flush=True)
    return result


def fingerprint(paths):
    return {str(path): sha256(path) if path.is_file() else None for path in sorted(set(paths))}


def watched_paths(build, sources):
    paths = list(sources.values()) + [build / "manifest.json", build / "prop-atlas.json",
        build / "prop-atlas.png", build / "reconstructed.png", build / "cleaned-environment.png",
        REPO / "ArtTools/felling_components.py", REPO / "ArtTools/felling_components_contract.py",
        REPO / "ArtTools/test_felling_components.py", REPO / "ArtTools/test_felling_components_contract.py",
        REPO / "ArtTools/test_felling_components_adversarial.py", REPO / "ArtTools/test_felling_components_viewer.cjs",
        PACKAGE / "viewer.js", Path(__file__).resolve()]
    try:
        manifest = load_json(build / "manifest.json")
        for key in ("reference", "baseline", "base", "report", "backingSource"):
            paths.append(confined(build, manifest[key]["path"]))
        paths.append(confined(build, manifest["base"]["inferenceMaskPath"]))
        for component in manifest["components"]:
            paths.append(confined(build, component["sprite"]["path"]))
            paths.append(confined(build, component["repair"]["maskPath"]))
            if component["contact"] is not None:
                paths.append(confined(build, component["contact"]["path"]))
    except (KeyError, TypeError, ValueError, OSError):
        # Required checks will fail; retain a partial snapshot for useful diagnostics.
        pass
    return paths


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--reports-dir", type=Path, default=PACKAGE / "reports")
    parser.add_argument("--authoring", type=Path, default=PACKAGE / "authoring.json")
    parser.add_argument("--backing", type=Path, default=PACKAGE / "sources/empty-environment-v1.png")
    parser.add_argument("--reference", type=Path, default=PACKAGE.parent / "reference.png")
    parser.add_argument("--actor-patch", type=Path, default=PACKAGE.parent / "generated/clean-plate.png")
    parser.add_argument("--timeout-seconds", type=int, default=180)
    args = parser.parse_args(argv)
    if args.timeout_seconds <= 0:
        parser.error("--timeout-seconds must be positive")
    # The Node suite's actual-asset test targets this authored package path. Do not
    # offer an alternate build argument while silently testing a different build.
    build, reports = (PACKAGE / "build").resolve(), args.reports_dir.resolve()
    reports.mkdir(parents=True, exist_ok=True)
    sources = {"referenceSha256": args.reference.resolve(), "backingSha256": args.backing.resolve(),
               "authoringSha256": args.authoring.resolve(), "actorRemovalPatchSourceSha256": args.actor_patch.resolve(),
               "extractorSourceSha256": REPO / "ArtTools/felling_components.py"}
    started = time.monotonic()
    paths = watched_paths(build, sources)
    before = fingerprint(paths)
    report = {"schemaVersion": 1, "id": "felling-components-offline-verification", "startedAt": utc_now(),
              "build": str(build), "unityExecuted": False,
              "scope": "Current component metadata, source provenance, exported pixels, atlas, and offline contracts. Visual silhouette/repair quality and Unity integration remain separate gates.",
              "toolchain": {"python": sys.executable, "pythonVersion": sys.version.split()[0], "node": shutil.which("node")},
              "steps": []}
    report["steps"].append(run_command("python-tests", [sys.executable, "-m", "unittest",
        "ArtTools.test_felling_components", "ArtTools.test_felling_components_contract",
        "ArtTools.test_felling_components_adversarial"], reports, args.timeout_seconds))
    report["steps"].append(run_command("node-tests", ["node", "--test", "--test-reporter=tap",
        str(REPO / "ArtTools/test_felling_components_viewer.cjs")], reports, args.timeout_seconds))
    for name, function in (("strict-assets", lambda: strict_assets(build)),
                           ("current-provenance", lambda: current_provenance(build, sources)),
                           ("exported-reassembly", lambda: reassembly(build)),
                           ("exact-atlas", lambda: atlas_check(build))):
        report["steps"].append(run_check(name, function, reports))
    after = fingerprint(paths)
    changed = [path for path in before if before[path] != after[path]]
    report["sourceAndOutputSnapshot"] = after
    report["changedDuringRun"] = changed
    report.update(finishedAt=utc_now(), durationSeconds=round(time.monotonic() - started, 3),
                  status="passed" if not changed and all(step["status"] == "passed" for step in report["steps"]) else "failed")
    if changed:
        report["error"] = "Inputs or outputs changed during verification; rerun after the build is stable."
    path = reports / "verification-report.json"
    path.write_text(json.dumps(report, indent=2, allow_nan=False) + "\n")
    print(f"Verification: {report['status'].upper()} — {path}", flush=True)
    return 0 if report["status"] == "passed" else 1


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError) as exc:
        print("ERROR: component verifier failed: " + str(exc), file=sys.stderr)
        sys.exit(1)
