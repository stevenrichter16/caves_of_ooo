# Grovelands environmental polish: forest floor

**Current environment checkpoint — 2026-10-28 16:19UTC:** Grovelands now has four original native forest-floor frames with flatter low moss/litter, replacing shared lawn repetition. V1 object-like stamps were rejected and corrected in V2.159/159native pass; actual Grove/Fen/CompostingField, Grass reader, dry/water/dry crossing and ordinary return/freshContinue173 verified (0/564000worldpixel differences). Independent final review0P1/P2. Exact publication is in progress; current43a151ddb packages exclude this pass. Sparse composition, repeated soft fragments and all full-area polish remain open. Next [Beating sand/paving/ruined-wall materials](GRAPHICS-BEATING-ENVIRONMENT.md). Crops/subtleanimation deferred. Last actual hourly report15:42:40UTC; next16:42:40UTC. Storage inspection only. See [Grovelands record](GRAPHICS-GROVELANDS-ENVIRONMENT.md).


**In progress,2026-10-28.** Next after the bounded Sodden/Stump passes under
the user's all-biome environmental priority. No Assets, runtime, tests, Unity
or Git changes accompany this document. No existing plan with this name was
present. Root owns actual capture, imagegen, integration and acceptance.

## Named actual defect and first design

Viewed `Verification/Live16x24/grovelands-actors-after-01-grove.png`: disclosed
forest ground looks like bright settlement lawn, with the same regular tiny
sprinkle repeated across a broad clearing. Its enclosing columns, trees and
vine walls give the scene an identity that its dominant floor does not support.
Dark glyph-covered regions are remembered terrain, not a demonstrated missing
native floor. The prior Fen capture
`grovelands-water-after-01-matched-fen.png` is a second material-transition
control; its ownerless permanent water is already intentionally native.

**Replace only the ordinary Grovelands Grass presentation with a restrained
forest-floor material.** Quiet cool dark earth/moss base, sparse irregular
leaf fragments and low decomposed-wood/filament detail should distinguish the
forest from the Sodden's olive fen and Sill lawn. Keep substantial blank areas;
no recognizable motif in every base cell, repeated diagonal lattice, hard
rectangular patch borders, fake harvestable growth or obstructing roots. Preserve
all actual trees, walls, columns, items, hazards, routes and collision. Do not
add props to fill the clearing in this first pass.

Proposed new `Sprites/Live16x24/grovelands-ground`,64×24: four opaque16×24
frames0–3. Frame0 is genuinely quiet;1–3 supply distinct subdued litter forms
with identical boundary base colors. First judge them tiled at normal zoom;
keep the existing stable coordinate selector if it succeeds. If real captures
still demonstrate stamp repetition, add one focused RED before changing only
the new floor selector. Do not inherit Sodden's former identical35-cell detail
rectangles. Neighbor-sensitive dressing is deferred unless a named actual
composition problem needs it; do not derive cover from unseen owners.

## Exact sources and preimplementation corrections

- `FlatPixelGrovelandsMaterialsCatalog.cs:96`: floor is exact current disclosed
  Grass; live independent tile state, competing owners and cultivation outrank
  it. It currently borrows **Sill frames20–23**, not the14-frame Grovelands
  material atlas. Editing Sill would repaint other regions and is out of scope.
- `VariantAt` uses local x/y with75% frame0; a strong frame0 mark repeats widely.
  Keep the existing material-body variations unchanged if floor selection changes.
- Preserve `grovelands-materials` frames0–13 (columns, seep full/film, sign,
  red growth, fruiting body, compost row/cache, vein, sundew), Sill0–15 vines,
  north-vegetation0 Tree and opening-base5 Bush. Their source is
  `ArtSource/Live16x24/grovelands-materials/{source-cells.json,manifest.json}`
  plus editable Blender source and `Tools/Live16x24/build_grovelands_materials.py`.
- The catalog's5-/6-input constructors already promise independent optional
  groups. Append a seventh floor texture after the borrowed selector; old
  constructors explicitly pass null and retain approved old floor behavior.
  Default loads the new resource; missing/malformed new floor safely retains
  the existing accepted material. No implicit reload for injected null.
- `TryResolveWaterGround` represents exact ownerless **permanent water** using
  opening-ground art. It is distinct from finite GroveSeep and from Grass.
  Keep wet/dry transitions, actor cover, mixed/temporary/owned coatings and
  energy/gas markers unchanged. Seep stock0 is still wet, not dry forest floor.
- `GrovelandsCompositionPlan` supplies real central/side openings and connected
  three-cell approaches for Grove/FruitingWall/TendrilFen/CompostingField.
  Do not change generation or POI/lair/sinkhole exclusions for this art pass.

Create a small editable unit-cell source under
`ArtSource/Live16x24/grovelands-environment`, reusing the established Blender
quad workflow (`build_sodden_environment.py` is the latest example). No new
asset-generation framework. Actual screenshot→imagegen→parts critique→native
cells→tiled study→actual comparison. Prompt must preserve HUD/camera/owners/
routes and revise only disclosed floor; reject invented giant mushrooms,
canopies, roots, lights, new services or planted crops.

## Earned route and focused acceptance

Use healthy `Verification/Live16x24/Fixtures/grovelands-grove-baseline65`:
Grove4.7, turn3720, HP38/40; saved gzip SHA256
`ce2c19fd97df8611685766c7597a05bb29acce89f8654b527e08a9ca53289ecb`.
`grovelands-play-summary.json` records the ordinary route: nine Right, Up,
C/Right/X for seep Examine, close and C/Right/W to drink; then approach61,12,
C/Up/X for the real sign61,11. Root must reconfirm current owner/position rather
than blindly repeat keys. Capture the initial whole view before moving.
Normal map travel4.7→5.7→4.8→0.8 has already reached all four formations.
For a short second-view control use the existing healthy
`Fixtures/grovelands-after-fen65`, Fen0.8, turn4430, HP38/40. The separate water
receipt records ordinary40,16 dry→40,17 water→40,19 opposite dry bank; inspect
current state on launch. Later actor checkpoints79/81 are only23/40 and are
less suitable for a material audit. No teleport, health reset or hidden reveal.

Write roughly4–6 focused RED cases before optional-resource integration:
actual world/reader floor routing, exact old constructor fallback, independent
water/owner priority, current graph/fog refusal, cached borrowed lifetime and
submitted floor recovery. Reuse existing
`CavesOfOoo.Tests.FlatPixelGrovelandsMaterialsTests`, including its `FenWater_*`
cases, then `FlatPixelSillEnvelopeTests` to guard shared-art preservation; add
GlobalPresentation only if its routing changes. Preserve old-resource
compatibility pins explicitly rather than blindly changing every Sill assertion.

Acceptance needs noticeable whole-view forest identity without louder clutter,
clear actual hazards/items, ordinary approach and wet/dry travel, reader parity,
return/save/fresh Continue, and byte-identical non-target art. Native counts do
not prove polish. The other formations/addresses, special sites, complete
Grovelands composition, packages and physical Deck performance remain open.

## Preimplementation corrections,15:49UTC

Root captured actual Grove4.7(40,12HP38) from earned65 and Fen0.8 from earned4430 before changes. Existing test file is Assets/Tests/EditMode/Presentation/Rendering/FlatPixelGrovelandsMaterialsTests.cs, not Tests/EditMode root. New optional seventh forest texture follows borrowed selector; old5/6 constructors explicitly retain legacy floor. Only default construction loads new resource. Existing Grass resource assertions must migrate after focused RED; old injected compatibility assertions stay. Retain VariantAt and all14body/16vine/water21 groups. New five focused tests cover world/reader routing, independent fallback, actor/water priority, current owner/fog/graph refusal and cached lifetime. Source art/reference and runtime implementation remain pending.

Pin correction: root first hashed the test file while the delegated RED edit was already arriving. Baseline test hash was corrected from exact HEAD a2dafcad7 (runtime/art pins unchanged); the RED file is separately preserved in native evidence. No claim that the test pin predates the write.

Native filter correction: test_names does not expand the ForestFloor_ prefix. That initial run selected0cases and is not RED/verification. Root reruns the five exact full method names; final regression uses the exact class name.

## Actual first trial,15:59UTC

159/159 GrovelandsMaterials/SillEnvelope cases passed after five intended RED failures. Candidate1 e2eb82a improves woodland palette but actualGrove v1-01 shows repeated brightmoss and darksoil stamps reading as tiny objects. Root rejects that presentation for final acceptance; v2 reduces local contrast/removes parallelbar/inner-rim shapes while retaining basepalette and native4frame/runtimecontracts. Native code unchanged, so source/art and actualcomparison repeat after this pixel-only revision; no redundant fulltest requirement. CUA needed freshApp/AX Raise after Unityreload; no alternateinput/simulationmutation used.

## Performance and final evidence

Four additional cached tiles/sprites are constructed once, no new frame/turn allocation or generation work. Ownership/disclosure/selector remain unchanged, borrowed sources are never disposed; new cached views are destroyed with catalog. The five new native methods cover default world/reader routing, optional compatibility, water/actor priority, stale/hidden graph refusal and lifetime/state stability. Full focused159 passed. Pixel-only V2 does not invalidate runtimeGREEN. See `Verification/Live16x24/grovelands-environment-final-receipt.json` for actual controls, profile bounds and remaining defects. All original body/vine/tree/bush/water art stays byte-identical. This establishes the material revision in three observed views, not all Grovelands areas or packaged execution.
