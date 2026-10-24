# Independent incidental-actor production review — 2026-10-24

Reviewed frozen new FlatPixelIncidentalActorCatalog, Greybox diff,39-case test source, two Resource PNGs/metas and incidental-art README/verification. Also followed fallback world authority and actual flat-examine authorization. Read-only; no Unity, test execution, repository edits or cleanup. Farmer contact sheet inspected visually; it is source art, not a gameplay screenshot.

## Result

**No actionable P1/P2 runtime finding. No production fix proposed.** Root owns native GREEN/regression and ordinary acceptance; this source review does not claim those ran.

## Source reasoning

- **Exact neutral identity:** catalog lines30–58 accepts only Farmer/ReedbackGrazer with Creature, owned visible Render/Physics/Brain/Body, no Player/Item/custody/takeability/explicit reskin, canonical single glyph and single-cell footprint. Grazer also needs its own SpreadGrazerPart. Stock, health, passive mood and forage values never select appearance. No inherited Villager/Cindercaller alias or motion policy change.
- **World authority retained:** Greybox TryResolvePresentation obtains the existing current selected fallback owner before calling the new local companion (aroundlines688–737). The adapter has no path around current graph/cell/fog/custody selection and loads only for its exact roles. Existing boundary, kitchen and incidental roles are disjoint, so the shared `out art` variable does not overwrite an already-selected other-role image in the current table. Outside admitted local graphs these actors keep truthful glyph fallback.
- **Reader authority retained:** Greybox subject lookup is deliberately semantic. Actual `ZoneRenderer.FlatPixel.cs:50–56` first calls `ExaminePortraitRenderer.IsCurrentPresentationSubject`; the latter checks current renderer graph and `ExamineDocumentBuilder.IsCurrentSubject`. Builder lines34–61 recheck canonical disclosed cell, real subject/viewer placement, owned Render/Physics/Examinable, custody and stack state. Adding this semantic native selector does not broaden that authority.
- **Resource independence/lifetime:** catalog lines24–27 build groups independently. Each must be exactly16×24; Sprite.Create atline77 explicitly uses the full rect,16PPU, centered pivot and FullRect. No borrowed texture setters. Tile/Sprite allocations occur only at construction; Dispose lines84–98 is idempotent and destroys owned frames only. New parent disposal follows existing optional groups.
- **Performance:** warm semantic query has no LINQ, collection construction, string formatting, listener or world scan. Existing getters and fixed part checks are used. New test lines178–185 measures1000 warmed semantic calls; this is an appropriate narrow allocation seam, not a total frame-time claim.

## Tests and bounds

The fixture covers actual imported resources, exact texture identity, full rect/pivot/PPU, distinct cached frames, semantic rejection after positive controls, real valid-resource disposal/peer survival, missing/malformed group independence, mutable state invariance, current-world hidden/unexplored/stale/outside/foreign/custody cases and unchanged motion admission.

`RealFarmerMovementRestoresTheCoveredBed...` lines203–218 uses MovementSystem.TryMove and normal old/new dirty hooks, checks already-submitted overlay tiles, unchanged full-pass counter, actual Bed frame and passability/occupancy. It does not merely re-query or manually force a redraw after movement.

`WorldAndAuthorizedReader...` lines187–201 exercises actual document creation, portrait renderer, submitted world tile and post-fog retirement. **Coverage limit:** this new actual-reader test specifically covers fog. Other stale graph/cell/custody tests here are world/direct semantic routes; do not report them as new complete reader-lifetime coverage. Existing shared authority tests remain the broader gate. This is a reporting bound, not a found runtime defect.

No native keyboard delivery, save/reload, visual legibility over the real cot or physical Deck acceptance follows from these fixtures. Ordinary root-owned captures must establish actual Farmer and Grazer placement; no spawned substitute is needed. Static images must remain described as static.

## Independent asset checks

- Approved `Assets/Art/ReedbackGrazer16x24/03-ridge.png` and new Resource copy both277 bytes,16×24, SHA256 `29db64b90637fdd305a355907c06547c5f46d82f912ec95a88307d92f28b4dad`. Bytes remain exact.
- Farmer source and Resource both834 bytes,16×24, SHA256 `4981b3cb861d30a3c8db4b426237f2a751dd48eca584034dac43d78acb63bfc0`.
- New texture metas use independent fresh GUIDs, no cropped sprite rect, center pivot,16PPU, Point/Clamp/no mipmaps/no compression/NPOT resize disabled. Four new source/test/resource GUIDs each appear exactly once under Assets. The old Grazer importer was not copied.
- Farmer contact shows exposed head, short moss/ochre torso, empty lowered arms and separated dark legs. README rejects the misleading generated-context inset and makes no sleep/tool/stock promise. Contact inspection is not final game art acceptance.

## Reviewed snapshot hashes

- `Assets/Scripts/Presentation/Rendering/FlatPixelIncidentalActorCatalog.cs`: `642bb9bdacaa24f1b19f4b1957c81ee59eccd7c3733c09ade47bea4b8e0c9c03`
- `Assets/Scripts/Presentation/Rendering/FlatPixelGreyboxCatalog.cs`: `2121548ec15c79e3266c10cb63231fd252e2c8911ab271e92093020516e976d8`
- `Assets/Tests/EditMode/Presentation/Rendering/FlatPixelIncidentalActorTests.cs`: `1d3f95ea1949d822b3f3e53d0f97134fd627d2346af5d6d2fb27b01d75368e9e`
- `Assets/Resources/Sprites/Live16x24/farmer.png.meta`: `a5ae1a95ca0f7b72be11acfccae95dfa25e2c50febd868a2fe6d3cee983654a6`
- `Assets/Resources/Sprites/Live16x24/reedback-grazer.png.meta`: `ddf5b92792452295a3ed6bc280da51a80dbbafb70440be4ae1ecdf294e8c38ab`

Root pre-commit formatting note: trailing whitespace emitted by Unity was stripped from the two new PNG metas after this review; importer values/GUIDs are unchanged. The hashes above identify the reviewed pre-format bytes, not the final whitespace-normalized metadata. Source verification was rerun.
