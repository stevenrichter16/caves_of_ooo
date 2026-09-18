# Main integration — 17 September 2026

The user explicitly requested all completed changes on `main`, followed by work on the release plan. Before integration, `main` was an ancestor of `codex/voxel-town-generator` by 365 commits. The feature checkout also contained accumulated uncommitted game code, art, source generators and tests from earlier work; advancing the old commit alone would omit required playable dependencies.

## Integration scope

Preserve the complete current gameplay source, runtime art and matching metadata; authoring sources, generators and tests; project quality settings; living documents; compact verification receipts and the current final native screenshots. This is a preservation commit of accumulated work, not a claim that every included file was newly authored during the regional-situation phase.

Do not include active MCP logs, Python caches, generated temporary Unity test scenes, local ProBuilder editor preferences, Blender automatic backups, or bulk historical raw verification logs/media. They remain untouched on disk. Existing tracked historical evidence remains tracked. A local scope manifest is `/tmp/coo-main-integration-scope.json`; source publication hashes are `Verification/RegionalSituations/publication.json`.

The original approximately 35 pre-existing modified files cannot be reliably identified from the present larger checkout. No unrelated contents are rewritten or discarded. The explicit current request to put all changes on main is satisfied by preserving the accumulated playable state, with all authored files reviewable in Git.

## Verification

- RS28: 14,901 cases, 14,869 passed, exactly 32 inherited failures, no added failures, zero C# errors. +141 cases compared with the current phase's baseline.
- RS26: 100/100 focused regional cases.
- RS27: 56/56 native checks, 17 gameplay images, 877 input steps, nine borders and save/load; all five Morrowfast interiors inspected. Explicit F12 protection on the long regional trip means this is not a combat-balance test.
- All 6,760 metadata files audited without GUID collision; 117 new coarse bindings checked.
- Published source/art hashes match the verification clone. Main editor remains open; forced refresh loaded `RegionalCargoPart`, returned idle and reported zero `error CS` console entries.
- Before committing, compare the candidate index against the validated clone for Assets, Packages and runtime ProjectSettings (excluding only private clone identity and non-runtime logs/test scenes). Do not treat an untracked-dependent checkout as proof of a complete commit.

Advance `main` only by fast-forward, preserving its full history. No remote push is implied. The next phase is release stabilization: classify and resolve the 32 inherited failures and then verify the normal opening without debug protection. The full design direction is in `RELEASE-VISION.md`.
