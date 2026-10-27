# Next coverage slice after the Grovelands actors

Source preflight, 2026-10-27. This document recommends the next slice; it does not
report an underground playtest, approved new art, native test result or release.
The ongoing Grovelands actor trial remains a separate acceptance gate.

## Recommendation

Cover the **ordinary shallow sandstone cave travel/material family**, at depths
1–2. Complete six exact owners together: `SandstoneFloor`, `SandstoneWall`,
`Rubble`, `Stalagmite`, `StairsDown` and `StairsUp`. This introduces a coherent
visual foundation in an area type that still has no ordinary native-scene
acceptance, rather than adding another small animation pass to the opening.

The coverage ledger has all six ordinary underground material bands marked
`not_yet_audited` for native region kit, actual scene/state, fullness and package
acceptance ([world-sprite-coverage.json:1312](../../../ArtSource/Live16x24/world-sprite-coverage.json)).
All six surface biomes now have bounded material integration records. Neither
fact means that surface graphics are finished. This is a priority choice for
broader visible coverage, not a claim that cave scenery is the only remaining
gap. Keep the later serious area-polish phase separate.

## Corrected source premises

| Possible assumption | Verified current source and consequence |
| --- | --- |
| `Cave` is another fresh surface country. | The authored map uses six surface biome letters; ordinary caves are reached through depth routing. Start with a real surface stair, not a fabricated Cave surface zone. See [WorldMapAuthoring.cs:51](../../../Assets/Scripts/Gameplay/World/Map/WorldMapAuthoring.cs). |
| The native opening cellar proves cave coverage. | Lairs, sinkholes, the Root and the Gleaners cellar are dispatched before ordinary underground. Their acceptance cannot stand in for this slice. See [OverworldZoneManager.cs:95](../../../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs). |
| A shallow sandstone cave may have a roomed layout. | Sandstone is depth1–2; room selection starts at depth3, and only ordinary Spread/Sodden/Beating columns are eligible. This slice requires the natural layout, not an invented roomed sandstone fixture. See [SolidEarthBuilder.cs:42](../../../Assets/Scripts/Gameplay/World/Generation/Builders/SolidEarthBuilder.cs) and [UndergroundLayoutPlan.cs:35](../../../Assets/Scripts/Gameplay/World/Generation/UndergroundLayoutPlan.cs). |
| The cave is a floor sheet beneath everything. | `StrataBuilder` places sandstone floor on 85% of carved cells, rubble on the next10%, and leaves the last5% without either; later builders can alter this. Do not infer or paint a sandstone owner into empty cells. See [StrataBuilder.cs:99](../../../Assets/Scripts/Gameplay/World/Generation/Builders/StrataBuilder.cs). |
| CaveBat/CaveSlime are the ordinary shallow encounter. | `UndergroundTier` selects a MarlbackScrabbler/Gleaner/Tunnelguard group, adds Stalagmite2–6 and optional Glowmaw at tier1. CaveBat/CaveSlime belong to other tables. See [PopulationTable.cs:1102](../../../Assets/Scripts/Data/Tables/PopulationTable.cs). |
| Every surface chunk provides an entrance. | `CaveEntranceBuilder` has a50% attempt roll and can fail placement. Preserve registered connections and actual saved stairs; do not generate a convenient replacement. See [CaveEntranceBuilder.cs:27](../../../Assets/Scripts/Gameplay/World/Generation/Builders/CaveEntranceBuilder.cs). |
| Depth1–2 contains only the six proposed owners. | The ordinary pipeline also adds landmarks, cave hazards, population and containers. Those remain explicit coverage gaps; the six-owner kit is a material/travel foundation. See [OverworldZoneManager.cs:580](../../../Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs). |

## Finite owner and art contract

| Exact owner | Real meaning / essential outcome | Existing immutable art candidate |
| --- | --- | --- |
| `SandstoneFloor` | Passable `Terrain`, `.` and `...,.'\`` variants; granular stone with shallow worn hollows. It is not a tiled room floor by definition. | `Sprites/Live16x24/beating-materials`, frames12–15, four16×24 cells. Candidate reuse; compare their muted joins to the actual natural cave before accepting. |
| `SandstoneWall` | Solid Wall, `#`; inherited Stone/Mineral material, HP30/30, hardness2, wreckage `Rubble`. Damage/destruction must preserve truthful state and reveal the actual remaining cell. | Same atlas frames16–17 are visibly **masonry** in the inspected contact. They are an inventory candidate, not approved natural cave geology. Prefer original quiet continuous rock faces/edge cues if the real cave/concept establishes that mismatch; do not repaint the accepted Beating cells. |
| `Rubble` | Passable Terrain with `,`; both naturally generated loose scatter and wall wreckage use this blueprint. No harvest or solidity is implied. | Existing `Sprites/Environment/rubble.png`,16×16 with binary alpha. Direct centered16PPU square reuse is possible, with4px implicit vertical space; existing native opening-base frame6 is another candidate, but its current policy is deliberately bound to `reference-glade-gravel`. Do not widen that old policy by accident. |
| `Stalagmite` | Solid physical object at layer1, `^`; no authored destructible/harvest state. It must read as a compact obstacle, not floor debris. | Existing `Sprites/Environment/stalagmite.png`,16×16. Inspected native source is a small pale pointed stone; preserve pixels and trial the centered square geometry before commissioning new art. |
| `StairsDown` | Passable Terrain carrying owned `StairsDownPart`; its physical owner and registered destination determine travel. | `Sprites/Live16x24/opening-anchors`, frame4,16×24; inspected contact shows a recessed stair opening. |
| `StairsUp` | Passable Terrain carrying owned `StairsUpPart`; actual return link must survive ordinary use and revisit. | `Sprites/Live16x24/opening-cellar`, frame0,16×24. Borrow the cell, not the cellar's address policy. |

Blueprint definitions: [Objects.json:3489](../../../Assets/Resources/Content/Blueprints/Objects.json),
[3523](../../../Assets/Resources/Content/Blueprints/Objects.json),
[7895](../../../Assets/Resources/Content/Blueprints/Objects.json),
[8761](../../../Assets/Resources/Content/Blueprints/Objects.json),
[8989](../../../Assets/Resources/Content/Blueprints/Objects.json),
[9032](../../../Assets/Resources/Content/Blueprints/Objects.json).
Current reuse policies are in [FlatPixelOpeningArtCatalog.cs:497](../../../Assets/Scripts/Presentation/Rendering/FlatPixelOpeningArtCatalog.cs)
and [FlatPixelBeatingMaterialsCatalog.cs:54](../../../Assets/Scripts/Presentation/Rendering/FlatPixelBeatingMaterialsCatalog.cs).
The Beating catalog currently admits eligible **surface** Beating addresses; its
matching owner names do not make generic caves native today.

Decode-only inspection confirmed existing atlas sizes192×24 for anchors,
144×24 for cellar and544×24 for Beating; rubble/stalagmite are16×16. This is
source-shape evidence, not an instruction to crop, resize or copy an atlas.
Use cached Sprite/Tile views of unchanged borrowed textures where appropriate.
No new poses, particles, creatures or loot are prerequisites for this kit.

## Ordinary route candidates

Two entrances were actually retained in earlier seed65 surface receipts. They
are **historical observed entrances**, not proof that the current play graph has
them or that their underground destination has been visited.

| Priority | Surface entrance | Ordinary first destination and evidence |
| --- | --- | --- |
| A, closest to current western work | `Overworld.0.9.0`, cell56,7 | `Overworld.0.9.1`. The actual GreenRim census records one visible StairsDown at56,7 with `savedLair:false`; [overwrit-before-owner-census.json:5](overwrit-before-owner-census.json) and [206](overwrit-before-owner-census.json). The historical ordinary route reached this area directly from Fen0,8; [overwrit-before-play-summary.json:13](overwrit-before-play-summary.json). From a current Fen visit, use the real world-map route one cell south and then ordinary local approach. |
| B, independent surface column | `Overworld.16.13.0`, cell45,9 | `Overworld.16.13.1`. Actual WindBarrens receipt records StairsDown30567 rendered as a glyph; [beating-baseline-01-wind-barrens-census.json:30008](beating-baseline-01-wind-barrens-census.json). Reach16,13 using ordinary map travel, re-observe the owner and take the registered stair. |

Before using either, verify the current saved map/POI/lair authority and literal
stair still match. Do not promise an exact safe walking path through current
actors. `>` (Shift+Period) invokes ordinary descent, `<` (Shift+Comma) ascent;
the existing travel helper can walk to a reachable disclosed stair and interrupts
for danger ([InputHandler.cs:998](../../../Assets/Scripts/Presentation/Input/InputHandler.cs),
[StairTravel.cs:15](../../../Assets/Scripts/Gameplay/World/StairTravel.cs)).
Capture depth1 entry/return, a chamber edge, an interior route, a depth2 descent
and return, then a fresh Continue. Depth3 is a scope boundary/countercheck, not
another promised material band. No teleport, inventory grant or save mutation.

## Named composition debt and minimum acceptance

1. **Unknown ordinary cave whole-view composition.** No underground baseline was
   taken by this audit. The85/10/5 placement and large solid masses create a
   source-predicted risk of empty black patches, repeated debris carpeting and
   indistinct wall/floor borders. Capture first; do not label every black cell
   unfinished ground—visibility and genuinely ownerless cells are different.
2. **Natural rock versus masonry.** The inspected Beating wall cells have obvious
   block joints. An unmodified atlas reuse must not quietly turn an entire
   geological cave into a constructed ruin. Set the cave material direction
   against the actual capture; use original editable native art only where a
   named mismatch remains.
3. **Return-route and obstruction readability.** Stairs must remain distinct in
   dim cave grading, and stalagmites/solid walls must not merge with loose
   rubble. Judge current visibility, remembered fallback, targeting, actual
   collision and reader identity. If connected edges are necessary, reuse the
   existing disclosed-neighbor pattern without exposing unseen geometry.
4. **Incomplete encounters and tactical surfaces.** Marlbacks/Glowmaw, containers,
   landmark owners and BrinePool/TarSeep/AshBed/SteamVent/PeatBog remain separately
   inventoried. The latter five are the current cave hazard table
   ([HazardTerrainBuilder.cs:81](../../../Assets/Scripts/Gameplay/World/Generation/Builders/HazardTerrainBuilder.cs)).
   A native floor must not conceal those real states. This six-owner delivery
   cannot advance an entire underground band to polished or complete.

Keep implementation finite: actual before capture and owner census; reuse
decision plus imagegen/native production for any **new** wall/floor art; native
RED for exact current graph/depth/owner guards, real wall destruction and stair
travel state, optional resource isolation, hidden/remembered/foreign/custom-owner
refusal, ground-under-subject parity and existing hazard preservation; minimal
catalog integration; meaningful GREEN/cold review; matched ordinary views and
save/revisit; current-source Mac/Linux delivery with existing package guards.
Use existing render/profile receipts to compare the same settled view; no new
benchmark framework or inferred physical Deck performance claim.

This audit read source, retained receipts and existing art only. It created this
preflight file and changed no Unity state, assets, runtime, tests or Git state.
