# Atmosphere delivery preparation

Status: new packager prepared and offline-tested, 2026-10-11. No build, package, archive, Unity action or commit was executed by this agent. The coordinator supplies final native receipts, commits unchanged final production source, builds Linux and executes delivery. The earlier performance/place packagers and the current delivery README remain unchanged.

## Scope and observed checks

The pre-implementation plan/sweep is recorded in AUDIO-ATMOSPHERE-ASSETS.md. New files: `Tools/SteamDeck/package-atmosphere.py`, `Tools/SteamDeck/test_package_atmosphere.py`, this document and focused packaging test receipts under `Docs/Verification/AtmosphericAudio/`.

Observed RED: the absent new module produced three expected fixture setup errors before implementation (`package-red.txt`); this was an import-failure gate, not 27 executed assertions. GREEN: `package-green.txt` records27 tests, all passing. They retain10 previous native/NUnit summary-versus-individual-outcome checks,4 real asset-union/credits/receipt tests, and13 four-iteration evidence checks. Asset mutation probes use temporary copies and restore positive controls; no shared Assets writes occur. `--help` was inspected; this does not execute packaging.

- Exact disjoint22 +31 +20 source-manifest union:73 source clips, no extra/missing Ogg files. All three independent signal receipts, current output/provenance SHA256s, full attribution/license texts, catalog/resource mappings and appropriate short/streaming importers are checked.
- Complete original, place and atmospheric credits combine into `AUDIO-CREDITS.txt`; separate expansion credits, all three source specifications/provenances and asset reports remain readable alongside it. Original Content ID/streamer instructions remain intact.
- Four fresh, distinct successful live reports are bound by path, hash, run ID, mode and explicit evidence time. Native/NUnit cases are kept separately; they are never added into one inflated total. Iteration4 also serves as the final world-audio report.
- Raw four-run reports and the original manifest are included in `Audio-Iteration-Receipts/`; its generated `index.json` supplies portable `packaged_report` names and retains original source paths/hashes. The raw source manifest intentionally retains its original paths as historical evidence; use the index for packaged report lookup. Screenshots are checked for local existence but are not copied by this tool; their source paths remain in raw reports. Screenshot existence is not visual review.
- Clean committed production source/current HEAD, fresh successful Linux build, exact output path/ELF identity, output-alias checks, no archive overwrite and full archive readback/file digests retain the prior tool's safeguards.

The existing world-audio pool remains16 sources; the tool adds no runtime sources. Native Mac/editor sample windows cannot establish pleasant listening, globally unclipped output, Linux behavior or physical Deck latency/frame rate/battery/thermal performance. No numeric performance improvement is claimed. Earlier iteration receipts describe their incremental source states; only the coordinator's final test/commit/build workflow relates final tests to the delivery HEAD.

## Explicit four-iteration manifest

Pass `--iterations-manifest` a JSON file with this structure. Paths may be absolute or relative to the manifest. Supply the actual task-start lower bound; placeholders below are schema examples, not evidence:

```json
{
  "schemaVersion": 1,
  "scope": "atmosphere-four-iterations",
  "notBeforeUtc": "<coordinator-recorded task-start ISO timestamp with timezone>",
  "iterations": [
    {
      "iteration": 1,
      "mode": "world-audio",
      "report": "<actual first live report path>",
      "sha256": "<SHA256 of those exact raw bytes>",
      "runId": "<raw runId>",
      "timeBasis": "report-file-mtime-estimate",
      "reportModifiedUtc": "<actual file mtime in timezone-aware ISO form>"
    }
  ]
}
```

Exactly four ordered entries are required. Modes are `world-audio`, `world-audio`, `native-work-audio`, `world-audio`. Iteration3 uses the explicitly bounded real repair/harvest/checkpoint route. Neither `world-audio` nor the full `native-repair-and-cultivation` mode is accepted in that slot. Each selected report must be complete/error-finalized, contain positive matching raw PASS counts, have zero failures/unexpected errors, retain can/cannot-verify bounds and name existing screenshots.

Native reports contain `seconds` but no native UTC timestamp. `reportModifiedUtc` therefore records filesystem evidence only, compared against the selected file within0.01s. An **estimated** interval subtracts the unchanged raw duration; these intervals must follow the task lower bound and one another. Do not invent precise starts or describe estimates as independently logged execution timestamps. Prefer original native report files, or copy with timestamp preservation (`shutil.copy2`); a fresh copy's creation time is not the original evidence time. Neither these estimates nor explicit iteration numbering independently proves feature coverage or revision identity.

A coordinator can derive each row without guessing times:

```python
from datetime import datetime, timezone
from hashlib import sha256
import json
from pathlib import Path
p = Path("<actual native report>").resolve()
raw = json.loads(p.read_text())
row = dict(iteration=1, mode=raw["mode"], report=str(p),
           sha256=sha256(p.read_bytes()).hexdigest(), runId=raw["runId"],
           timeBasis="report-file-mtime-estimate",
           reportModifiedUtc=datetime.fromtimestamp(p.stat().st_mtime, timezone.utc).isoformat())
```

## Final invocation contract

After the coordinator has all four live gates and the final all-green unit/build receipts, use the following command with actual paths and the full final40-character HEAD. This has **not** been executed:

```text
python3 Tools/SteamDeck/package-atmosphere.py <iteration4-report.json> \
  --source-commit <full-final-HEAD> \
  --build-report <successful-fresh-linux-build.json> \
  --editmode-report <final-native-editmode.xml> \
  --asset-report Docs/Verification/AtmosphericAudio/original22-green.json \
  --place-asset-report Docs/Verification/AtmosphericAudio/prior31-green.json \
  --atmosphere-asset-report Docs/Verification/AtmosphericAudio/asset-contract-green.json \
  --iterations-manifest <four-live-iterations.json>
```

Expected already-built executable: `Builds/SteamDeck/2026-10-11-atmosphere-current/CavesOfOoo-current/CavesOfOoo.x86_64`. New archive: `Builds/SteamDeck/2026-10-11-atmosphere-current/CavesOfOoo-SteamDeck-atmosphere-current.tar.gz`. Existing archive is a hard refusal. `--folder` accepts only that new folder name beneath `Builds/SteamDeck`; no old delivery is overwritten.

🟡 Self-review: final-receipt revision language distinguishes earlier incremental gates from final source; per-run raw evidence is retained and counted once; time estimates remain explicitly limited. 🧪 Build/archive execution, final real iteration manifest, final native regression/visual review and physical-device listening remain coordinator gates. The27 passing tests prove offline packaging checks, not an actual completed delivery.


## Iteration3 route contract correction

The coordinator's full repair-and-cultivation live attempt encountered an actual Rotling during the later growing wait. Its three repair/audio checks and original crop/audio actions had passed; the full route and failed report remain preserved. The coordinator authorized a separate `native-work-audio` route that ends after real three repairs, original harvest/pickup and F5/F6 checkpoint, with no enemy/world edits. This delivery gate now requires that exact mode for iteration3; it does not relabel the failed full route or claim completed long-growth coverage. The four-report hash/identity/timing/count/failure guards are unchanged.

Tests were changed first: `package-work-mode-red.txt` records27 cases with two expected new-mode positive-path errors and one old-mode countercheck failure. After the single production mode pin changed, `package-work-mode-green.txt` records27/27 passing. The initial package-red/package-green receipts remain historical evidence of the earlier contract. No Assets, old packagers, README, builds, packaging or commits were touched in this correction. The combat observer remains only an outside-Assets draft until the iteration3 live release.
