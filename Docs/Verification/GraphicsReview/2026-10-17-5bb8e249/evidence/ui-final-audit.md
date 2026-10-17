# Final UI/manual caption and manifest audit

Audited `captions.md` and `manifest.json` against the previously opened originals and source records for all **34** entries owned by this review: 24 initial manual/baseline images, seven Gantry/journal images and three selected skills-route images. This audit did not operate Unity or change repository files.

## Verified

- All 34 IDs are present exactly once in the final manifest.
- Every final image path exists. PNG dimensions and SHA-256 match the manifest. Each selected skills copy is byte-identical to its preserved route original.
- For all 31 per-frame manual/journal/baseline records, session, timestamp, input state, player state, input devices, graphics settings, active cameras, renderer flags, time band, tick, ambient level and tint match the raw JSON exactly.
- Baseline menu frames are correctly separated as supplements and disclose inherited full reveal. Actual world frames disclose runtime full-reveal off and retained source-default FOV radius 999. Full-screen inventory/journal renderer suppression is appropriately distinguished from gameplay rendering failure.
- The map correctly distinguishes raw player (47,8), logical destination (17,5), and Look cursor (46,8), with descent based on player position.
- Stump and Overwrit formation names are the explicitly verified specialized `CascadeGorge` and `PilgrimageRim`; generic raw `None` is preserved by reference rather than misread as no generation.
- UI-04 uses the recaptured settled long dialogue, not the excluded typewriter frame. The 720p pair uses the same tick and branch. Travel-note coordinates and text match the selected report.
- The skills images disclose staged SP, synthetic input, incomplete route result and lack of per-frame world-camera measurements. The missing discipline rail on keyboard reopen is preserved as a visible defect, and connected-controller hints are not mistaken for keyboard-only presentation.
- Nothing in the owned captions claims physical Steam Deck validation, motion/performance validation, natural progression to the staged ranks, or unexplored destination-state verification.

## Corrections requested from assembler

1. **Frame versus route state in SKL-03.** Its per-image `alterations`/`overrides` and resulting caption say Long Blades, Deflect 3, Riposte 3 and En Garde have been purchased. Those purchases happen later in the route. The actual Hearthwarm image shows eight SP remaining after only Acrobatics and Vault, with Vault assigned to slot ten. Use: “10 SP staged at route start. By this frame Acrobatics and Vault were purchased, leaving 8 SP; Vault assigned to slot ten. Original starter gear/HP unchanged.” The complete purchase list can remain in session-level route history. SKL-01/02 correctly represent the later zero-SP state.
2. **Baseline review link.** `reviewEvidence` for BASE-01 through BASE-04 currently names `evidence/manual-first-review.md`, but their actual image review is `evidence/base-review.md`. Point those four entries to the latter.

## Plain-English polish suggested

- Caption Location lines currently render Python dictionary syntax, for example `world {'x': 17, 'y': 5, 'depth': 0}`. Format as “world (17,5), depth 0; player tile (39,11).” Preserve the raw-world-map qualifier on MAP-01.
- Insert spaces in generated boilerplate such as `seed64`, `radius999`, `resolutions1280x800` and `baseline0.82`.
- Remove duplicate sentence periods in skills input and alteration lines.
- The generic “Next: Waiting for ordinary game input” is accurate but less useful than the specific available action: Enter closes Examine; Esc closes dialogue; Q/Esc closes Journal; Tab switches quests/notes; map travel uses player location. This is optional caption refinement, not evidence failure.
- The journal captions accurately interpret zero-of-three, but a short adjacent note about the visibly absent separator/current marker would help readers who do not open the separate findings file. Those defects already exist in manifest `visibleFindings` and review evidence.

These observations are about package accuracy and clarity. No production fix is requested within this capture-only assignment.


## Resolution verified after regeneration

Reopened the regenerated manifest and captions. Both substantive corrections are resolved: SKL-03 now states that only Acrobatics and Vault have been purchased at that frame, with eight of the staged ten SP remaining and Vault assigned to slot ten; later Long Blades purchases are explicitly separated. All four BASE entries now point to `evidence/base-review.md`.

All 52 caption Location lines use readable coordinate prose rather than printed dictionaries; MAP-01 preserves its raw world-map tile qualification. No further factual correction is needed for the owned entries. A few optional spacing/double-period polish nits remain, without affecting meaning. No production files or Unity state were changed by this verification.
