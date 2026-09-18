# Caves of Ooo — release-candidate checkpoint, 2026-09-18T03:28:18Z

**Branch:** `release-candidate` (new; branched from local `main` at `41820270`). Two commits are in the local repository; **the push to `origin` could not be completed from the unattended run** (no GitHub credential is available to the isolated VM, and the cloud proxy refuses this repository). Push it from a normal terminal: `git push -u origin release-candidate`.
**Base runtime:** `26fbe544` (accepted main integration). `main` itself was not modified by this run.
**Run type:** unattended scheduled run from a Linux VM on the user's machine. Unity was not launched; the user's open Editor was not touched.

## Purpose

This document records what the scheduled release-candidate run found already implemented, what it applied, what it independently verified, and which release gates remain. It is the dated companion to `RELEASE-STABILIZATION.md` and `RELEASE-AGENT-PROMPT.md`. Read those first for the full R1 context; this file only adds the delta from 17 September 22:05 CDT to 18 September 03:28 UTC.

## 1. What was already implemented (do not redo)

The 17 September assessment listed three R1 repair groups covering all 32 inherited RS28 failures. All three were already fully authored in the isolated clone and preserved as `Docs/ReleaseHandoff/R1-candidate.zip` with a 99-file before/candidate hash manifest:

| Group | Already done in the candidate | Evidence already on file |
|---|---|---|
| 29 equipment ground-sprite cases | Seven 16×16 sprites + GUID-only metas, preload registration, ten exact blueprint routes, narrow missing-resource guard in `EnvironmentSpriteRenderer` | R101 (79 cases, 72/7, guard absent), R103 green, contact sheet, static verification JSON |
| Two stale world fixtures | `PlaceProfileTests` now expects explicit profiles for Morrowfast/Gantry/Tine/Quillhold/Tally with ordinary-village controls; `WorldMapAuthoringTests` finds Sill by coordinate via `PlaceAt(10,10)` instead of `Places[0]` | R103 green |
| One building-kit variant assertion | 72 FBXs, `catalog.json`, `BlockHouse.prefab`, authoring material regenerated from source; `validate_variant_fingerprints` added to `contract.py` with 8 new tests | R102 import (exit 0, 0 CS, metas unchanged), 15 offline tests |

Also already present in the candidate: two new observational checks in the native journey (`native_opening_starts_vulnerable`, `native_opening_completed_without_debug`) plus their registration in the Editor batch's `RequiredChecks`. They compiled in R103 but have never executed natively.

Not already done (and still not done — see section 4): the dedicated ground-sprite idle/walk/pickup-drop A/B bench, execution of the two opening checks, imported-block visual review, final cold-eye review, and any fresh full-suite run on a live checkout.

## 2. What this run did

The candidate had not been applied anywhere in Git: all 83 pre-existing files in the live tree matched their manifest `before` hashes byte-for-byte and the 16 new files were absent. Zero conflicts. Since the archive was explicitly preserved as a reviewable draft and the user asked for a `release-candidate` branch, the run:

1. Created `release-candidate` from `main` (`41820270`).
2. Extracted the 99 manifest paths from the zip, verifying each extracted file's SHA-256 against the manifest `candidate` hash and each live file against `before` before overwriting. Result: 99/99 now match `candidate`.
3. Reviewed the source diffs by hand. The renderer change is three hunks (preload list, guard, route switch); the fixture changes replace only the obsolete `Places[0]`/plain-Morrowfast premises and add presence/uniqueness assertions; the scenario change adds two `Check`/`Require` pairs; the Python change adds one validator and its tests. Nothing else in `Assets/Scripts` changed.
4. Updated living docs: `RELEASE-STABILIZATION.md` (new checkpoint), `EQUIPMENT-GROUND-SPRITES-PLAN.md` and `BUILDING-BLOCKS-3D.md` (R1 status sections so future agents do not rediscover this wave), `ReleaseHandoff/README.md` and `RELEASE-AGENT-PROMPT.md` (branch note).
5. Committed only the 99 candidate paths, the doc edits and this file by explicit path. The two modified UnityMCP log files and roughly 4,300 untracked report/cache/`__pycache__` files were deliberately left uncommitted.
6. Attempted to push `release-candidate` to `origin`. The isolated VM has no GitHub credential (`could not read Username for 'https://github.com'`), and the cloud sandbox's git proxy refused to inject one for `stevenrichter16/caves_of_ooo` (403, repository not in the session's authorized set). A 1.06 GB bundle of `origin/main..release-candidate` was produced but exceeds the 400 MB device-to-cloud transfer limit and would not have helped without push credentials anyway; it was left at `_to_delete/release-candidate.bundle` for manual deletion. **The push must be done by hand:** `git push -u origin release-candidate`. Note that `origin/main` is 368 commits behind local `main`; pushing this branch uploads those ancestor commits (about 1 GB) but does not move `origin/main`.

Housekeeping caused by the unattended run: git could not delete its own lock and temporary object files inside the connected folder, so `.git/HEAD.lock`, `.git/objects/maintenance.lock` and 64 `tmp_obj_*` files were **renamed** into `.git/_to_delete/` instead of removed. Both are safe to delete. The working tree still shows the two modified `Assets/UnityMCP/Log/*.log` files and the pre-existing untracked reports, exactly as before the run.

## 3. Independent checks performed here

These were run on the Linux VM against the `release-candidate` tree. They are cheap countercheck evidence, not Unity acceptance.

- **GUID collision audit:** each of the seven new sprite `.meta` GUIDs appears exactly once across every `Assets/**/*.meta`. The authoring material's new `_MainTex` GUID resolves to `Assets/Art3D/BuildingBlocks/Textures/BuildingPalette.png.meta`.
- **Sprite contract:** all seven PNGs are 16×16, 8-bit RGBA, alpha ∈ {0, 255}, outline ink `(30,32,28)` present. Contact sheet viewed: dagger, sword, spear, boots, gloves, helmet and mace are distinct, readable silhouettes.
- **Sprite reproduction:** `ArtSource/EquipmentGroundSprites/generate.py` was run in an isolated copy with Pillow 12.3. The seven outputs are pixel-identical (RGBA bytes) to the committed assets; PNG file bytes differ only because of the encoder version. The metadata copy path was not exercised (existing metas were provided), so no GUIDs were regenerated.
- **Building-kit contract:** `python3 -m unittest test_contract` in `ArtSource/BuildingBlocks3D`: 15/15 pass.
- **Compile-surface sanity:** `WorldMapAuthoring.PlaceAt(int,int)` and `DebugInvincibility.IsEnabled(Entity)` exist in the live scripts, so the corrected fixtures and opening checks reference real APIs. This is not a compile; R103 is the compile evidence.

## 4. Outstanding before `release-candidate` can be promoted to `main`

Unchanged from the handoff checkpoint. None of these can be done from a headless Linux VM; they need the macOS clone (or a fresh isolated copy at this branch) with Unity.

1. **Fresh full EditMode suite on this exact branch checkout.** R103's 14,901/14,901 ran in `/tmp/coo-regional-verification-20260917`; the branch is byte-identical for the 99 files but has never been compiled as a checkout.
2. **Native journey with the two new opening checks executing** (`Tools/ChunkGameplay/run_native.py … --execute` in the clone). The opening must start vulnerable and finish Farra's expedition plus five interiors alive without F12 or synthetic healing; the later regional F12 use stays labelled separately.
3. **Dedicated ground-sprite A/B bench** per `EQUIPMENT-GROUND-SPRITES-PLAN.md`: preserved original renderer as baseline, same harness/workload for the candidate, private saves, idle/walk/pickup-drop, real counters, honest synthetic-vs-native input labelling. No such bench exists yet.
4. **Imported block variant visual review** in useful Editor views, plus a GUID/reproducibility audit of the 72 reimported FBXs.
5. **Cold-eye / adversarial review** (Q1–Q4) of the applied slice and any resulting fixes, then re-run of the affected fixtures.
6. **Merge into `main`** with the evidence receipts in the same reviewable change, keeping the user's Editor open (defer import/reload if a Play session is active).

## 5. Things this run explicitly did not do

- Did not run Unity, tests, or the native harness on the user's machine.
- Did not modify `main`, the configured spawn (`Overworld.2.6.0`), camera, reveal, or any gameplay setting.
- Did not apply the clone-only helper adaptations (`Tools/Village3D/run_common.py`, `Tools/VoxelWorld/run_editmode.py`) or private ProjectSettings; both are excluded from the manifest.
- Did not start any post-R1 work (middle-game chain, ending spine, construction, Vein Pressure). Per the handoff, those wait until R1 closes.
- Did not re-inspect the R103 XML beyond its receipt summary.

## 6. Suggested next scheduled or interactive step

Run gates 1–2 from section 4 in the clone against `release-candidate`, attach receipts under `Docs/Verification/`, and record the result in `RELEASE-STABILIZATION.md`. If both are green, the sprite bench (gate 3) is the last substantive blocker before merging to `main` and starting the middle-game chain milestone.
