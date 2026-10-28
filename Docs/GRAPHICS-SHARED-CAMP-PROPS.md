# Next surface props: shared camp storage and water fixtures

Recommend **Sack + Urn + ordinary Well**, using three unchanged legacy16×16 bodies. This closes visible camp functions and reuses existing resources across settlements/regions without a new art packet. Initial selection below is retained; see the current implementation log for verified progress.

**No duplicate scope:** `Docs/GRAPHICS-SHARED-STORAGE.md` explicitly limits the shipped catalog to Chest/LockedChest/StrongBox/Crate/OreCache plus mimic disguise. Sacks and urns remain excluded. No current FlatPixel catalog match for Sack/Urn/Well was found.

## Exact contracts and reusable art

| Owner | Source correction | Existing immutable resource |
|---|---|---|
| Sack | `Objects.json:30388`: PhysicalObject, `u/&w`, layer1, **passable**, Container capacity6. No authored Material/Thermal/Destructible; do not invent cloth-health, empty/open sprites or Solid=true. | `Assets/Resources/Sprites/Environment/cont_sack.png`,16×16, SHA`d1a4c73c85a9aa5d115456cbb37fa106746d9a2cfcb6298fc6911d912488c9b4` |
| Urn | `Objects.json:30442`: `8/&y`, layer1, solid Container capacity5. No authored Material/Thermal/Destructible despite “clay” description; contents remain mechanic-owned. | `.../cont_urn.png`,16×16, SHA`02c6e0906e1789fbd070edb6267a6303585cc045d86f4e70220befe559d15690` |
| Well | `Objects.json:24332`: `O/&c`, layer1, solid, WellPart; **no LiquidPool** in ordinary blueprint. No invented finite reservoir or depletion state. | `.../well.png`,16×16, SHA`4d8a33eced838078f84360ecd8d04a9d4921b06ee8ca1c4ec4a22c72406940c4` |

All three viewed directly. `EnvironmentSpriteRenderer.cs:210–211,756` confirms established mappings. Cache centered FullRect16PPU views; preserve raster/meta originals. Exact editable raster provenance remains unverified; `ArtSource/Clearing16x24/kit/stone-well.png` and 3D well/sack models are distinct assets, not demonstrated source files for these PNGs.

`ContainerPlacementService.cs:59–106,123–129` places Sack across Beating, Spread, Sodden, Grovelands and settlement pools; Urn also occurs in desert/jungle pools. `VillagePopulationBuilder.cs:545–574` and `FirstTentCompositionPlan.cs:54–55` place wells.

**Well caveat:** `WellDrinkingService.cs:7–14,35–38` supports unlimited ordinary wells and finite pools only when actually present. `WellPart.cs:18` respects repair faults. `SettlementSiteVisuals.cs:45–71` deliberately changes Well base color/name through four repair stages. First scope should exclude WellSite/custom-state wells or explicitly preserve their existing stage cues; never replace them with an unconditional white native “working well.” No query may drink, repair or alter stock.

## Practical actual baseline

Current `beating-actors-after-06-brine-lens.png` still visibly contains camp glyphs. Historical current-paint receipt `beating-final-03-brine-lens.json` identifies Sack35227 at41,2, Well35212 at51,6, GuestClothPole35213 at54,6 and nearby TentWalls. Reacquire via ordinary controls from earned `beating-actors-drifter136` (17,13,turn3290,HP38). CaravanRoad final05 receipt identifies Urn39386 at47,7; earned road135 is19,13,turn3550,HP33. Historical disclosure is not current access proof; obtain exact readers before RED.

TentWall/GuestClothPole remain important next visual gaps, but no exact existing flat PNG was identified. TentWall is solid Cloth8HP terrain; pole is passable Furniture, not a service (`Objects.json:33942,33992`). Existing Wellmeet/FirstTent/Sodden libraries are3D. Do not borrow stone walls or signposts deceptively. After this reusable batch, commission a coherent tent/cloth kit if actual views still prioritize it. Whole-area fullness remains separately open.

## Pre-implementation sweep — 2026-10-28 03:11UTC

Root reread all three blueprints and PhysicalObject inheritance, ContainerPart, WellPart/WellDrinkingService, SettlementSiteVisuals, placement sources and existing storage/render seams; viewed all three PNGs. Source corrections saved before production changes:

| Premise | Corrected scope |
|---|---|
| Well water heals | Ordinary draw clears Parched and its STR/AGI penalties; HP unchanged. No authored finite pool. |
| Well name/color suffice | Exclude WellSitePart, RepairablePart and LiquidPoolPart even if repaired/stable. These have separate service/state semantics. |
| Container stock defines sprite | Contents, capacity and locks remain mutable; body is invariant, access remains mechanic-owned. |
| Every Player owns status effects | Factory Player creates the container lazily via Entity.ForceApplyEffect; fixture corrected before RED. |

Actual unchanged GameBootstrap baselines captured from earned drifter136 and road65, seed65: Brine17.13 player45,9 HP38, Sack35227 at41,2 and Well35212 at51,6; Road19.13 player59,9 HP38, Urn39386 at47,7. Normal Look/examine verified Sack and Well. Road Urn was current-visible in the world receipt, but the intended reader input landed on sand at49,6; preserved as `before-05-road-sand-reader-miss`, not Urn evidence. Five before PNGs show world gaps and two exact readers. No movement, spawn, stock or save mutation was scripted. Both isolated roots were copied and SHA-verified under `/private/tmp/coo-camp-before-preserved` and `coo-camp-road-before-preserved` before Finish; normal saves/prefs restored. Scope reuses unchanged legacy16×16 bodies in the flat16×24 presentation; no new imagegen, pixel reauthoring or exact editable provenance claim. This is integration of existing art; new tent/cloth artwork still requires the full reference pipeline.

## Implementation log

- RED: native TestResults.xml03:14:02–06UTC,26failed/0passed, retained `shared-camp-props-red.xml`. MCP job27b2f3ca2e0f4e29959fff273914a8f5 reported initialization timeout despite the later genuine native run; XML is the assertion evidence, not that envelope.
- Minimum implementation: exact three roles, independent optional16square inputs, cached borrowed-texture views, authoritative refusal across four world/reader seams, one existing Render cue dispatch. No blueprint/mechanics/save-format edits; no query actions. Relevant GREEN and actual after acceptance pending.
- Next cloth reference uses built-in imagegen on before01: `ArtSource/Live16x24/shared-camp-cloth/scene-reference-01.png`; exact prompt and manifest/critique alongside. This concept is NOT gameplay and does not preserve footprints/UI exactly. Original native cloth sprites/integration remain pending; approved shared-prop rasters stay unchanged.

- Cold independent source review (portable_equipment_final_review) found no P1/P2 in99-line adapter and four routes. Tests first ran against the old assembly because script-only refresh had not imported the new source; retained102-case stale-import XML (26camp failures/76existing passes). Full AssetDatabase refresh then confirmed compiling=false, adapter loaded=true and zero console errors before the final native run. This tooling correction is not a gameplay defect.

- First loaded-source native run101/102 passes: duplicate footprint setup threw the existing Entity single-footprint invariant before reaching the renderer. Corrected the counter-check to assert that rejection and unchanged body; no runtime change. Retained `shared-camp-props-fixture-correction.xml`. MCP job51957d71093c48239e7e366552e98bb5 again timed out before native callbacks; native XML03:19:19–34 is authoritative.

Hourly report sent 2026-10-28T03:28:05.799544+00:00; next due 2026-10-28T04:28:05.799544+00:00. Prior surface package/main completed. Camp props102native passes and actual Well draw/Sack transfer+traversal locally complete; Urn/restart/publication in progress. Next original cloth kit, sparse-region and55+5polish scopes remain open; animation deferred.

## Bounded implementation acceptance — 2026-10-28 03:33UTC

Qud reference: existing owner-authorized container and well mechanics, not a new port. Scope divergence: reuse unchanged16×16 raster bodies on the flat16×24 world grid; no new art/animation, blueprint or save-format change. Exact native source PNG provenance remains unidentified; all three hashes above unchanged.

Final native job881e6ab59b8c4878afe8f595c8211bea: **102/102 distinct passes**,26camp+76existingstorage/furniture,13.48s. XML includes positive owner paths and counter-checks for foreign/missing/duplicate parts, custom identity, custody, resource refusal, current graph/fog/selected-owner and cache lifetime. Real native well cure removes Parched without healing; actual player was not Parched, so gameplay proves draw-water action/log only.

Actual seed65 GameBootstrap: before01→after01 at45,9HP38 replaces Sack/Well glyphs without changing terrain/shoreline geometry. Exact Well02 and Sack04 Examine portraits match the world. Walk45,9→51,7, ordinary draw water, unchanged38HP; walk→41,3, open Sack35227 containing mushroom +6gold, take mushroom, stock2→1. Step onto41,2 selects player over the passable Sack; step back41,3 restores Sack body. F5fixture138,turn3540; fresh isolated Continue138 restores41,3HP38 and stock1. World crop comparison564000pixels differs210pixels confined to player region458,110–481,140; do not claim identical frame or derive a performance change. Road fixture65 verifies current Urn39386 at47,7 and exact after10 portrait; no actual Urn transfer or new cross-zone route claimed.

Editor profile220.13s/77248unwrapped samples: renderer mean0.126ms,p99.243ms; drawcalls mean39.19,max43. Includes tool/UI/idle; no controlled speedup, sustained FPS, Linux or physical Deck claim. Console retains two memoryless depth notices. Current Mac/Linux packages remain source8c0c72e11; this prop slice is Editor verified and will be batched with cloth before rebuilding.

Independent source+actual visual review found no P1/P2 within this scope (`shared-camp-props-independent-review.md`). **Can verify:** source guards, native gameplay operations, actual world/reader appearance, ordinary inputs, saved contents, restart and bounded profile. **Cannot claim:** every regional instance observed, actual Parched cure/Urn transfer, finite/repair-well acceptance, full scene polish or newly packaged execution. Tiny16square bodies, glyph cloth, broad bare patches and all55+5polish scopes remain explicit debt.

All five isolated roots (before,road-before,after,restart,road-after) copied and SHA-verified under `/private/tmp/coo-camp-*-preserved` before Finish. Pending view restoration after the test import needed the existing Restore method once while idle; normal scene clean/Playfalse/rootnull verified.49foreignpaths retain their hashes. Three owned cave metadata whitespace changes from Unity import are preserved outside the project then restored toHEAD. No storage data removed; read-only disk check370GiBfree.

Files: NEW adapter+meta, tests+meta; MOD Greyboxfourroutes; phase/coverage/continuation records; actualcaptures, XML, JSON receipts/profile, fixture138. No existing raster edit. Next: original native TentWall/GuestClothPole kit; source/exact readers and imagegen critique retained in GRAPHICS-SHARED-CAMP-CLOTH.md. Animation deferred.

## Packaged delivery — 2026-10-28 04:10UTC

Source9a2371852 is verified on origin/main and included in fresh Mac/Linux builds. See [batched delivery](Verification/Live16x24/packaged-camp-props-cloth-delivery.md):10actual Mac captures across2processes, world/readers, pole traversal, well drinking, Sack Open2items and exact564000-pixel save/restart comparison. Real Scorpion damage is preserved in fixture141. Linux archive verified, Linux/physicalDeck not executed. Earlier pending-package statements are superseded within these bounded observations; no full-area polish claim. Normal state, prior builds and49foreign paths preserved.
