#!/usr/bin/env python3
"""Run all Felling scene offline checks and refresh the prepared JSON handoff.

Usage from any directory:
    python3 /path/to/caves-of-ooo/ArtTools/verify_felling_scene.py

Requires the existing Python/Pillow, Node.js and .NET toolchains on PATH.
Writes raw text logs, a JSON report and prepared JSON only; never calls Unity
or generates/edits raster assets. A failed step always yields a nonzero exit.
"""

from __future__ import annotations

import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import sys
import time


REPO = Path(__file__).resolve().parents[1]
SCENE = REPO / "ArtSource" / "FellingSite"


def utc_now():
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


def test_counts(name, output):
    if name == "python-tests":
        match = re.search(r"Ran (\d+) tests?", output)
        return {"total": int(match.group(1))} if match else None
    if name == "node-tests":
        counts = {key: int(value) for key, value in
                  re.findall(r"^# (tests|pass|fail|skipped) (\d+)\s*$", output, flags=re.MULTILINE)}
        return counts or None
    if name == "dotnet-tests":
        match = re.search(r"RESULT: (\d+) passed; (\d+) failed\.", output)
        if match:
            passed, failed = map(int, match.groups())
            return {"total": passed + failed, "passed": passed, "failed": failed}
    return None


def run_step(name, command, reports_dir, timeout):
    started = time.monotonic()
    result = {"name": name, "command": command, "cwd": str(REPO), "startedAt": utc_now(),
              "log": f"{name}.log"}
    try:
        completed = subprocess.run(command, cwd=REPO, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                                   text=True, errors="replace", timeout=timeout, check=False)
        output = completed.stdout
        result.update(exitCode=completed.returncode, status="passed" if completed.returncode == 0 else "failed")
    except subprocess.TimeoutExpired as error:
        output = error.stdout or ""
        if isinstance(output, bytes):
            output = output.decode("utf-8", errors="replace")
        output += f"\nRUNNER ERROR: step exceeded {timeout} seconds.\n"
        result.update(exitCode=None, status="failed", error="timeout")
    except OSError as error:
        output = f"RUNNER ERROR: could not start command: {error}\n"
        result.update(exitCode=None, status="failed", error=str(error))
    counts = test_counts(name, output)
    if counts:
        result["tests"] = counts
        if name == "node-tests" and counts.get("skipped", 0):
            result.update(status="failed", error="Node suite skipped a required comparison test")
    if name.endswith("-tests") and result["status"] == "passed" and not counts:
        result.update(status="failed", error="Suite returned success without its expected test summary")
    if name in ("validate-assets", "export-prepared") and result["status"] == "passed":
        try:
            result["findings"] = json.loads(output)
        except ValueError:
            result.update(status="failed", error="Validator returned success without a JSON summary")
    result["durationSeconds"] = round(time.monotonic() - started, 3)
    (reports_dir / result["log"]).write_text(output, encoding="utf-8")
    print(f"{name}: {result['status'].upper()}", flush=True)
    return result


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--reports-dir", type=Path, default=SCENE / "reports",
                        help="raw-log/report directory (default: ArtSource/FellingSite/reports)")
    parser.add_argument("--timeout-seconds", type=int, default=180,
                        help="maximum elapsed seconds per command (default: 180)")
    args = parser.parse_args(argv)
    if args.timeout_seconds <= 0:
        parser.error("--timeout-seconds must be positive")
    reports_dir = args.reports_dir.resolve()
    reports_dir.mkdir(parents=True, exist_ok=True)
    start = time.monotonic()
    report = {"schemaVersion": 1, "id": "felling-scene-offline-verification", "startedAt": utc_now(),
              "repository": str(REPO), "unityExecuted": False,
              "toolchain": {"python": sys.executable, "pythonVersion": sys.version.split()[0],
                            "node": shutil.which("node"), "dotnet": shutil.which("dotnet")},
              "steps": [],
              "scope": "Offline geometry, asset metadata and pure-code contracts; Unity runtime and visual acceptance remain pending."}
    layout_path = SCENE / "layout.json"
    if layout_path.is_file():
        report["sourceLayoutFileSha256"] = hashlib.sha256(layout_path.read_bytes()).hexdigest()

    def run(name, command):
        result = run_step(name, command, reports_dir, args.timeout_seconds)
        report["steps"].append(result)
        return result["status"] == "passed"

    validator = str(REPO / "ArtTools" / "felling_scene.py")
    # Export current data before Node's independent cell-by-cell occupancy check.
    assets_passed = run("validate-assets", [sys.executable, validator, "validate", str(layout_path), "--require-assets"])
    if assets_passed:
        export_passed = run("export-prepared", [sys.executable, validator, "export", str(layout_path),
                                              "--out", str(SCENE / "Prepared"), "--require-assets"])
    else:
        export_passed = False
        explanation = "Skipped because strict asset validation failed. Existing Prepared files were not refreshed.\n"
        (reports_dir / "export-prepared.log").write_text(explanation, encoding="utf-8")
        report["steps"].append({"name": "export-prepared", "status": "skipped", "exitCode": None,
                                "log": "export-prepared.log", "reason": explanation.strip()})
        print("export-prepared: SKIPPED (asset validation failed)", flush=True)
    # Continue independent suites after failures so one run captures all useful diagnostics.
    run("python-tests", [sys.executable, "-m", "unittest", "ArtTools.test_felling_scene"])
    run("node-tests", ["node", "--test", "--test-reporter=tap", str(REPO / "ArtTools" / "test_felling_preview.cjs")])
    run("dotnet-tests", ["dotnet", "run", "--project",
                         str(SCENE / "Integration" / "Tests" / "FellingScene.Core.Tests.csproj")])
    report.update(preparedRefreshed=export_passed, finishedAt=utc_now(),
                  durationSeconds=round(time.monotonic() - start, 3),
                  status="passed" if all(step["status"] == "passed" for step in report["steps"]) else "failed")
    if not export_passed:
        report["preparedComparisonNote"] = "Node may have compared an older export; this run cannot certify fresh Prepared output."
    report_path = reports_dir / "report.json"
    report_path.write_text(json.dumps(report, indent=2, allow_nan=False) + "\n", encoding="utf-8")
    print(f"Verification: {report['status'].upper()} — {report_path}", flush=True)
    return 0 if report["status"] == "passed" else 1


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError) as error:
        print(f"ERROR: verification runner failed: {error}", file=sys.stderr)
        sys.exit(1)
