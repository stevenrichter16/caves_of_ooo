# R1 candidate handoff

These files preserve unfinished release-stabilization work separately from the playable main runtime.

- `R1-candidate.zip`: relative-path candidate files, including the seven sprite assets/routes, two corrected world fixtures, regenerated/imported building kit and two observational opening checks.
- `R1-candidate-manifest.json`: exact base/candidate hashes. Apply only against matching base bytes in an isolated verification copy; inspect conflicts.
- `equipment-sprite-contact-sheet.png`: readable nearest-neighbor review, not a native gameplay screenshot.
- `equipment-sprite-static-verification.json`: dimensions, alpha, metadata, GUID and repeat-generation checks.
- `missing-resource-guard.json`: exact already-restored guard and the honest R101 staging sequence; do not apply twice.

The complete prompt is [RELEASE-AGENT-PROMPT](../RELEASE-AGENT-PROMPT.md), and the authoritative final checkpoint is at its end. The original clone is `/tmp/coo-regional-verification-20260917`. Clone-only runner helpers and private settings are excluded from this archive.

All completed runtime changes are on main; this candidate awaits its remaining native/visual/review gates. Keeping a draft archive in Git is not runtime integration or release acceptance.

**2026-09-18 update:** the archive was applied to Git branch `release-candidate` (branched from `main` at `41820270`). All 83 pre-existing base files matched their `before` hashes and the 16 new files were absent, so the application was conflict-free; all 99 paths now match their `candidate` hashes on that branch. `main` is unchanged. See `Docs/RELEASE-CANDIDATE-2026-09-18T032818Z.md` for the remaining gates.
