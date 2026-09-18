# Release stabilization — R1

Status: started 17 September 2026, after all accumulated playable changes reached local `main` at `26fbe544`. The user authorized starting the release plan without intervention. Unity stays open; implementation and tests use the independent verification clone. No gameplay reset or debug-state change in the live editor.

## Goal

Resolve the 32 inherited RS28 failures by fixing actual missing content and correcting only demonstrably stale assumptions. Preserve meaningful assertions. Then verify the ordinary opening without debug invincibility; separate mechanical completion from feel/balance. This is release phase one, not a promise that the campaign is ready to ship.

## Verification sweep and existing RED evidence

RS28: 14,901 tests, 14,869 pass, 32 fail, zero compiler errors. Exact failures are already recorded before any R1 production edits.

| Failure family | Count | Verified premise / intended response |
|---|---:|---|
| Equipment ground sprites | 29 | Existing adversarial fixture expects seven distinct 16×16 sprites and exact ten-blueprint routes. PNGs/routes are missing; fulfill the art/mapping contract, retaining glyph fallback, tint, visibility, dirty-cell and claim controls. |
| Building-block variants | 1 | Six family/variant pairs still share geometry/paint. Compare authoring output to installed FBX, then repair the generator/import publication; do not hand-edit one baked mesh or weaken uniqueness. |
| Place profile table | 1 | Morrowfast is now an explicit authored profile. Replace the obsolete plain-village expectation with the actual authored-profile contract and neighboring controls. |
| Sill/start position | 1 | SampleScene explicitly starts at Overworld.2.6.0; bootstrap reads its configured field, not Places[0]. Sill's address/biome/tier contract stays; test those without imposing the obsolete table-order spawn premise. |

## Gates

1. Preserve source/art before editing. Work from the exact committed state and record corrections above before fixes.
2. Reuse the existing failing assertions as the RED baseline; add paired tests for any newly identified branch. Fix each group independently.
3. Keep pixel sprites binary-alpha, shared outline (30,32,28), full 16×16 frames and copied importer metadata with unique GUIDs. Check silhouettes and inspect the resulting art.
4. Rebuild block models from their generator and repeat-build/hash-check where relevant. Validate real imported meshes, not filenames or FBX metadata.
5. Run focused fixtures, independent Q1–Q4/countercheck review and a fresh full suite. No blanket exemptions.
6. Publish exact verified files, commit on main, keep the current Unity session intact. Record actual normal-player native acceptance and remaining limitations.

## Scope and performance

Art is produced offline; no extra per-frame scans or runtime cube generation. Existing renderer logic is changed only if tests establish a real routing defect. No new lore, campaign, world simulation or save migration in this first repair wave. The following roadmap remains `RELEASE-VISION.md`.

## Implementation log

- Main integration preserves all accumulated runtime dependencies; 12,979 Assets/Packages files match the validated clone. Main remains local; no remote push requested.
- R1 opened against exact RS28 red cases; source audits run before edits. The original full-suite failures remain visible until genuinely resolved.
