# Stump material kit: preflight and coverage contract

Status: **bounded material integration verified in actual native Editor gameplay,
2026-10-27 13:11 UTC; source pushed and fresh Mac/Linux packages verified within the bounds below**. The original
preflight below was saved before production. Five formation views, real tank
service, vein harvest/product readers, wet-to-dry traversal and fresh Continue
are verified. Regional polish/fullness and complete Stump coverage remain open.
Root owns Unity and Git.
This slice follows the worldwide missing-sprite priority in
[the completion plan](GRAPHICS-COMPLETION-PLAN.md) and the required
[capture → concept → native art → integration pipeline](LIVE-16X24-ART-DIRECTION.md).
It preserves accepted art and defers optional animation.

## 1. Goal and bounded scope

Make the existing Stump formations read as the petrified anatomy of the Tree:
pink-grey worn ground, consistent stone-wood grain, low rock shelves, quiet spray
basins and sheltered summit bromeliads. Preserve their real routes, collision,
water, harvesting, ecology and ordinary services. This is an eight-owner material
kit with two exact, unchanged vegetation reuses, not a new terrain generator.

`StumpCompositionPlan.IsWildernessZone` is the source authority. The prior
[source audit](Verification/Live16x24/world-next-static-kit-audit.md) identifies
**18 canonical ordinary addresses** out of 22 authored Stump surface cells.
A supported current graph must additionally have the correct live map biome,
null POI, current manager/cache identity and no saved lair column. A biome label
or null POI alone is insufficient. Explicitly exclude Stillleaf/sinkhole 2,4,
Root 3,3, Felling-Site 3,5, multi-cell pilot 3,7, underground and other special
sites. Do not expand opening or Grovelands admission to obtain shared art.

The ordinary pipeline realizes 2,000 `TepuiStone` floor owners, then the actual
formation owners. Its shared surface spine still adds entrances, landmarks,
hazards, population, containers, trade stock and haulable props; optional crop
placement follows. All of those other families remain separate coverage work.

## 2. Source candidates and actual before evidence

| Source candidate | Band / formation | Existing composition to preserve | Root capture now available |
| --- | --- | --- | --- |
| `Overworld.2.5.0` | Foothills / CascadeGorge | Three unequal spray basins, stone shoulders, low ledges and protected dry approaches around water. | `stump-before-01-cascade-gorge.png` |
| `Overworld.2.2.0` | Slopes / Grainfield | East–west solid grain bands, deliberate gaps and sheltered living vegetation. | `stump-before-02-grainfield.png` |
| `Overworld.4.5.0` | Slopes / ButtressRidge | Tapered solid fingers with open approaches and pockets. | `stump-before-03-buttress-ridge.png` |
| `Overworld.3.4.0` | Summit / SummitScrub | Unequal stone-dome masses with sheltered trees and water-bearing plants. | `stump-before-04-summit-scrub.png` |
| `Overworld.2.3.0` | Summit / RimForest | Unequal living tree/bush islands, an open crack and small dome clusters. | `stump-before-05-rim-forest.png` |

The formation/address mapping above is source-derived, as recorded in the prior
audit. All five listed PNGs and `stump-before-06-tank-reader.png` were opened at
original 1280×800 resolution for this preflight. They are actual before views,
not generated concepts. Root confirms ordinary map travel through all five exact candidates and tank
examination at local (36,11) from player (37,11). This is attributed root gameplay
evidence, not a route executed by the planning reviewer. The saved
`Verification/Live16x24/stump-before-owner-census.json` and
`stump-before-gameplay-receipt.json` bind current-manager identity, serialized
addresses, owner counts, six capture hashes and the earned turn-6810 checkpoint.
Six screenshots are not six distinct zones or proof that every
owner/state was exercised.

Visible defects are concrete: repeated punctuation dominates the floor; spray
basins read as long rows of blue tildes; wall, ridge and ledge glyphs are difficult
to distinguish as materials; summit domes repeat small arch glyphs; tank plants
remain green `u` shapes. The tank reader does disclose the existing rain-barrel
bromeliad and drinking lore. It does not prove a drink action happened.
The large dark areas outside the Gorge disclosure are fog/visibility context,
not permission to invent cliffs or fill unobserved terrain with scenery.

## 3. Pre-implementation verification sweep

| Premise to reject | Verified source correction / consequence |
| --- | --- |
| All stone-looking owners require Material, Thermal and Destructible. | `TepuiStone`, `GrainRidge`, `DescentLedge` and `StoneDome` lack these parts. Only `TepuiWall` inherits the wall material/damage/thermal contract. Validate each exact identity rather than a universal stone predicate. |
| Any glyph variant marks a customized owner. | Ordinary `TepuiStone` declares `GlyphVariants` as `...,.'` plus a final backtick. An unconditional nonempty-variant refusal would reject every normal floor. Accept the exact authored value; refuse unfamiliar variants. |
| Stump bands represent simulated elevation. | `StumpBands` is authored ecology. `DescentLedge` is passable and has no stairs, falling or climbing part. Art must not advertise a drop, jump or descent action. |
| A depleted SprayPool becomes dry. | `Zone.ProjectPool` writes permanent water regardless of `Volume`; the natural owner remains after vessel withdrawal. Zero volume means no remaining draw stock, not dry ground. Preserve a wet surface. |
| All water sources share the Fen adapter. | The approved Grovelands adapter explicitly refuses physical pools/wells. `SprayPool` is a real `LiquidPool(water,40)` with its own owner and source meaning. Do not weaken the ownerless-water predicate. |
| The blueprint's initial pool color is the complete runtime signature. | `LiquidPoolPart` initializes its render from the liquid definition: water is `~`, `&c`, while the blueprint starts with `&C`. Use actual factory-created owners in tests. |
| TankBrocchinia needs full/empty pool frames. | It has `WellPart` but no LiquidPool or projected water. Existing drinking uses the well service without finite stock and does not soak the cell. One working plant identity is sufficient; no invented depleted state. |
| The pale vein is a portable item or mining workstation. | `TepuiboneVein` is a solid Harvestable source yielding 1–2 `Tepuibone`. Successful harvest removes the source. There is no persistent depleted node or mining-service part. The portable product already has its own accepted sprite. |
| Living trees on a petrified mountain should become dead stone trees. | The exact `Tree` and `Bush` owners retain ordinary Wood/Plant, thermal and destruction semantics. Reuse their accepted living art unchanged. |
| Every TepuiStone cell is neutral bare floor. | `BiomeCropPlacement` can add `CultivatedSoilPart` and `Plantable` to an existing TepuiStone owner. Such a cell must preserve cultivation/soil feedback rather than be covered by neutral material art. |
| One center screenshot proves a finished region. | The five formations retain fauna, hazards, crops, landmarks and service/prop glyphs. Judge whole scenes and disclose these omissions; do not certify fullness from a material atlas. |

## 4. Exact owner/state contract

All source checks include owned Render/Physics parts, exact blueprint, current
spatial registration and the normal renderer's selected disclosed owner.
No portable, carried, equipped, stale, hidden, forged or expanded-footprint
instance is admitted merely because it shares a display name or glyph.

| Owner | Real body and essential state | Native visual direction |
| --- | --- | --- |
| `TepuiStone` | Terrain, layer 0, non-solid; no material/damage part. Neutral ground only; preserve cultivated/stateful alternatives. | Quiet opaque pink-grey plane with rare, faint stone-wood grain fragments. No repeated grass glyph or gravel carpet. |
| `TepuiWall` | Solid Wall, layer 2; inherited Stone, 40/40 Destructible, hardness 3, Rubble wreckage and Thermal. Positive partial HP retains its identity; changed thermal/status meaning must remain legible. | Compact stone shoulder with subdued grain. No tall cliff, brick wall, new aperture or passable-looking gap. |
| `GrainRidge` | Solid Terrain, layer 2; no Material, Destructible or Thermal. | Continuous petrified grain mass, distinct from floor and passable ledge. Quiet majority; do not join bright horizontal stripes across every cell. |
| `DescentLedge` | Non-solid PhysicalObject, layer 1; no stair/drop/interaction part. | Low flat shelf with a restrained edge, visibly walkable. Preserve one-cell footprint. |
| `SprayPool` | Non-solid Terrain, layer 1, Material Water, real LiquidPool water initially 40. Positive stock versus zero stock; both remain wet while the source survives. | Continuous quiet cool basin surface and a subtly shallower still-wet state. Preserve ice, mixed coatings, charge, fire/gas and unknown states through existing feedback or refusal. |
| `StoneDome` | Solid Terrain, layer 2; no damage/material part. | Smooth low rounded pink-grey mass, distinguishable from ridges and angular shoulders. Repeated adjacent cells must read as a larger landform. |
| `TankBrocchinia` | Non-solid Terrain + Vegetation, layer 2; Well with no finite pool. No Crop or Harvestable part. | Broad upright cupped leaves around contained dark water. Distinct from crops, generic bushes and sundew; no invented fruit, depletion gauge or wet floor. |
| `TepuiboneVein` | Solid mineral source, layer 4; Harvestable yield Tepuibone 1–2. Reject harvested/removed/stale source. | Pale seam through angular stone-wood, distinct from a loose chip. No remaining corpse, ore pile or stump after successful removal. |

Vessel withdrawal from an actual SprayPool uses existing `LiquidVesselService`
and phase guards. Frozen source/ice cannot be presented as freely drawable
water. Pool contact does not itself consume stock. Tank drinking instead uses
`WellDrinkingService`; no new water economy or state simulation belongs here.

### Immutable exact identity reuse

| Real owner | Existing resource and frame | Retained semantics |
| --- | --- | --- |
| `Tree` | `Sprites/Live16x24/north-vegetation`, frame **0** of 4 | Solid Vegetation, layer 1; Wood, Destructible 30/30 and Thermal. |
| `Bush` | `Sprites/Live16x24/opening-base`, frame **5** of 10 | Non-solid Terrain + Vegetation, layer 0; Plant, Destructible 6/6 and Thermal. |

These are **two borrowed cells from two existing atlases**, not new art. Preserve
all input PNG/source/import bytes. Cache derived sprites/tiles once and dispose
only what the new catalog owns. Missing/malformed groups fail independently;
injected null textures must not silently load production resources. Existing
player, equipment and harvested `Tepuibone` product art remain unchanged.

## 5. Proposed native production packet

The following is a provisional extraction budget, to be finalized after the
actual-view imagegen concept and critique. It is not an asset-count completion
target: **26 original 16×24 cells in a 416×24 atlas**, plus the two unchanged
borrowed cells. No optional animation or duplicate idle sheets.

| Proposed frames | Owner/state |
| --- | --- |
| 0–3 | TepuiStone quiet base and three sparse variants |
| 4–5 | TepuiWall compact shoulders |
| 6–9 | GrainRidge quiet mass and rare grain variants |
| 10–11 | DescentLedge low shelves |
| 12–15 | SprayPool with positive volume: quiet base and sparse variations |
| 16–19 | SprayPool with zero volume: visibly wet base and sparse variations |
| 20–21 | StoneDome smooth masses |
| 22–23 | TankBrocchinia intact plant silhouette variants, not growth/stock stages |
| 24–25 | TepuiboneVein intact source variants, not depleted states |

Use a 75% quiet base / 25% sparse alternative coordinate selection for large
four-frame fields, as in accepted regional kits. Choose variation deterministically
without consuming game RNG. Do not bake high-contrast borders around each cell.
The floor must remain calmer than solid masses; the pool should read as one
basin, not hundreds of separate bowls. Ridges must preserve the authored grain
direction while their silhouette follows each existing formation. Tanks and
ore can carry local detail because they are sparse functional destinations.

Capture-backed concepts are references only. Produce original native integer-cell
geometry or an equivalently editable native source; retain source cells, palette,
editable source, atlas, import specification, direct native and enlarged contacts,
manifest, prompts and critique. Follow the existing source verifier pattern:
missing-output RED before generation, exact dimensions/binary alpha/cell decoding,
protected-input hashes, meaningful corrupt-output counterchecks and deterministic
native rebuild. Inspect both 1× and enlarged contacts, then freeze for integration.
Do not crop or shrink generated concepts into production sprites.

## 6. Runtime integration and test plan

Implement one bounded Stump catalog patterned on the existing regional catalogs.
It should expose current foreground, neutral ground and exact-subject reader
resolution; if water uses a dedicated ground path, bind it to the actual validated
SprayPool source and coating, not a global wet-cell shortcut. Reuse the existing
`FlatPixelFallbackCatalog` selection and `WorldLocationContext` current graph.
Cache the finite address set once. Rendering may not call GetZone, generation,
inspection commands, whole-world scans, game RNG, liquid withdrawal or gameplay
mutation. Keep the legacy/non-flat rendering branch intact.

Write and run native RED before production integration, then GREEN and existing
related controls. Pair each positive case with a nearby countercase:

1. **Scope:** valid ordinary addresses versus other biome, special site, stale
   same-ID graph, changed POI/map, saved lair, underground and malformed ID/map.
2. **Identity:** every actual factory-created owner; foreign/duplicated parts,
   unexpected glyph/VisualID, wrong solidity/layer, detached or moved owner,
   carried/takeable state, footprint mutation and unfamiliar added semantics.
3. **Disclosure:** visible/explored selection versus hidden, remembered,
   unexplored and another covering owner; no new atlas leak through portraits.
4. **Ground:** normal floor beneath actors/items and the admitted passable
   vegetation/ledge versus crop/Plantable/soil, competing unknown Terrain,
   additional state sources and mixed/expired/foreign liquid projections.
5. **Water:** positive and zero volume with permanent actual water, invalid
   liquid/negative stock/foreign pool, real stock decrement and dirty refresh,
   removal of the last source versus another source retaining water. Check
   overlays/ice/charge and a covering actor; suppress only represented water
   feedback, never unrelated state. Keep the ownerless Fen rule unchanged.
6. **Lifetime and actions:** partial wall/tree/bush damage, actual removal and
   wall wreckage; actual vein harvest/removal/product reuse; well availability
   without a pool. Never require invented depleted plant or vein frames.
7. **Resources/performance:** exact imported dimensions and frame rectangles,
   independent optional-group failure, cached frame reuse/disposal, unchanged
   borrowed textures and repeated-query zero-allocation where the existing
   counter is supported. A zero unsupported counter is not performance proof.
8. **Shared presentation:** combined ground/foreground and exact reader texture
   identity, ordinary current-graph travel and Continue; retain existing grading
   lifecycle controls. A fallback sprite being non-null is not native parity.

Existing `BiomeAffordanceTests` already cover tank drinking, spray contact,
last-pool removal and saved source parts. Reuse these domain controls; add only
missing presentation/state boundaries. Do not alter their mechanics to make
art predicates pass.

## 7. Whole-scene and ordinary-action acceptance

Root must bind each baseline to its actual saved graph, seed/address, pose,
selected owners, screenshot hash and intervention limits. Revisit the same five
formations after integration at the same zoom/pose/disclosure where feasible.
Compare actual whole 1280×800 views, not only a contact sheet or a cropped prop.

Required visible checks: continuous Gorge water versus dry bypass and walkable
ledges; Grainfield gaps and petrified direction; Buttress tapered masses and
pockets; Summit dome/tank distinction; RimForest vegetation islands and open
crack. Check material grade through ordinary border travel and fresh Continue.
Use normal controls to walk a dry route and enter water, approach a tank/read/use
its existing service, and reach/read/harvest an actual vein when available.
Record earned partial damage/removal where feasible. Do not inject stock, actors,
teleport or claim unobserved depletion simply to fill an evidence table; defer
an unavailable ordinary state explicitly while retaining its test coverage.

The material pass is complete only within its proven owners/states. Judge
whether each scene feels coherent and inhabited without texture carpeting.
If a formation remains too bare or repetitive, name the actual defect and
separately plan/test a source-level composition change. Do not hide it with
unowned props or visual density that obstructs routes. Known further categories
include habitat fauna (CascadeFather, GlasspaneFrog, Wardline, Sari/Summit actors),
crops/soil, hazards, stairs, landmarks, containers, residents/services and special
sites. Registry candidates are not an actual encounter census.

## 8. Review, implementation log and evidence limits

- **Source sweep complete:** ordinary generation, blueprint inheritance, water,
  well, harvest, crop mutation, borrowed frame mapping and regional rendering
  integration were inspected. No current source blocker to the bounded kit.
- **Before-view inspection complete:** five formation images and one tank reader
  were viewed. Root confirms all five exact candidates were reached by ordinary
  map travel and the tank examined; durable census/header binding is saved in
  the receipt above. No after or production-art acceptance yet.
- **Yellow premises corrected before implementation:** absent stone parts,
  authored floor glyph variants, persistent water at zero volume, non-finite
  tank, passable ledge, removed harvested vein and cultivated floor mutation.
- **Open visual gates:** new concept, native source/contact review, actual material
  repetition/fullness, draw/drink/harvest feedback, travel and restart.
- **No tests or builds run for this preflight.** It cannot certify renderer
  performance, Linux execution, physical Steam Deck behavior or worldwide art
  completion. No native art/runtime/test/Unity/Git mutation occurred.
- **Files changed by this planning task:** only `Docs/GRAPHICS-STUMP-MATERIALS.md`.

Primary source trail: `Assets/Resources/Content/Blueprints/Objects.json`;
`World/Generation/StumpCompositionPlan.cs` and
`Builders/StumpCompositionBuilder.cs`; `World/Map/StumpBands.cs`,
`OverworldZoneManager.cs` and `Zone.cs` pool projection;
`World/BiomeCropPlacement.cs`; `Materials/LiquidPoolPart.cs`;
`Items/LiquidVesselService.cs`, `LiquidSourcePhase.cs`, `HarvestablePart.cs`;
`Settlements/WellPart.cs`, `WellDrinkingService.cs`;
`Presentation/Rendering/FlatPixelGrovelandsMaterialsCatalog.cs`,
`FlatPixelOverwritMaterialsCatalog.cs`, `FlatPixelOpeningArtCatalog.cs`,
`FlatPixelGreyboxCatalog.cs`, `ZoneRenderer.FlatPixelPaint.cs`;
`Assets/Tests/EditMode/Gameplay/Biomes/BiomeAffordanceTests.cs`.
Gameplay paths in this list are relative to `Assets/Scripts/Gameplay/`;
Presentation paths are relative to `Assets/Scripts/`.

## 9. Native production and implementation log — 2026-10-27

The actual before survey is preserved in six PNGs, five owner censuses and the
earned turn-6810 fixture. Two built-in imagegen passes are retained under
`ArtSource/Live16x24/stump-materials/concepts/` with exact prompts. Version 1's
extrusion/cactus/noise and version 2's changed layout/HUD are rejected; only
palette and restrained material direction inform native authoring. No concept
pixels or invented scene placements enter the game.

The native packet contains 26 original 16×24 cells, editable Blender and cell
sources, palette, import specification, direct 1×/8× contacts and frame mapping.
The builder reads no raster input. Source verification checks 9,984 pixels,
eight corrupt-output controls and 350 unchanged protected inputs. Rebuilds are
byte-identical after removing only Blender's volatile non-pixel render-timing
text from PNG outputs; the initial metadata-only mismatch remains recorded.
Candidate atlas SHA-256: `caa932d3b2f8fcd0602e9a6bde905309414a412b57ba06b52f36e332789e49c1`.
The contact review flags dome-versus-floor contrast for actual gameplay testing.

`FlatPixelStumpMaterialsCatalog` and bounded `FlatPixelGreyboxCatalog` hooks now
connect ground, exact owner bodies, real source water and shared readers. They
reuse approved Tree/Bush cells without editing them; ordinary scope remains 18
addresses, guarded by current graph/map/POI/lair identity. No generation, stock,
loot, service, collision, biome grading or optional animation behavior changed.

- **Native RED:** 92/92 failed before the catalog existed, 11:42:51–11:42:59 UTC;
  `Verification/Live16x24/stump-materials-native-red.xml`.
- **Counter-check review:** strengthened submitted missing-atlas pool waves and
  actual earned Tepuibone inventory/examine identity; still 92 cases. See
  `Verification/Live16x24/stump-test-contract-review.md`.
- **Native candidate GREEN:** 92/92 passed, zero failed/skipped/inconclusive,
  11:51:00–11:51:09 UTC; `stump-materials-candidate.xml`, SHA-256
  `a5dc538edbac2b6518e02a60d4738e6d8e0c18d51ec387008680666f389810fe`.
  The MCP bridge reported an initialization timeout even though Unity ran and
  saved that result. Native XML and inactive native-run state were verified;
  the bridge failure is retained and is not reported as a successful tool job.
- **Independent source review:** no unresolved runtime blocker; current graph,
  source/state, optional resource, reader and disposal contracts inspected.
- **Native related GREEN:** 644/644 passed, zero failed/skipped/inconclusive,
  11:54:11–11:55:10 UTC; `stump-materials-related.xml`. The bridge again
  timed out at initialization; native XML and inactive-run state were checked.
  This includes 92 new Stump cases, prior 533 presentation/grading controls
  and 19 existing biome affordance cases.
- **First actual trial:** all five formations revisited with ordinary controls;
  Tank examination and drinking verified. `stump-trial-gameplay-receipt.json`
  binds eight actual captures and the earned turn-7130 checkpoint.
- **🟡 Visual RED:** under the existing Cave grade, domes almost merge with
  floor and ridges/walls read as floor stains. Correct only new stone colors;
  preserve water/tank/seam colors and all generation/geometry. Reassess
  SummitScrub fullness after contrast is readable. No trial acceptance.
- **Still pending:** corrected actual views/actions, source repetition/fullness,
  profiling, fresh Continue and delivery.

Files: new Stump catalog/meta, Stump tests/meta, builder and editable source
packet/atlas/meta; modified bounded Greybox hooks; new before/concept/verification
records. Historical `STUMP-COMPOSITION.md` water statements are explicitly
corrected to the current LiquidPool/Well contracts. This log is not visual or
all-world acceptance.

## 10. Connected solid perimeter correction — pre-implementation, 2026-10-27

**Status: named visual defect; contract and failing tests first.** The actual
palette-corrected trial still presents solid TepuiWall, GrainRidge and StoneDome
masses as dark floor patches. A color-only correction is insufficient. Add a
thin, flat native perimeter around the exposed edges of these existing solid
owners. Do not change generation, collision, object counts, grading, water,
plants, harvest, services or animations. This is an original presentation fix;
no Qud assets or copied implementation are involved.

### Verified premises and bounded design

| Premise checked against current source | Correction or binding |
| --- | --- |
| Zone/render orientation | Zone north is `(x, y - 1)`. The renderer maps it toward the top of the screen with `height - 1 - zoneY`; native north must remain the sprite's top edge. |
| Existing wall mask helpers | Their bits mean connected neighbors. This new mask means **exposed edges**, so those helpers cannot be reused unmodified. |
| A nearby solid owner is enough to join | False. Only the currently selected, explored, visible, currently placed, exact same-family owner with the same accepted body/state contract can hide an edge. Hidden, remembered, covered, malformed and changed-meaning neighbors behave exactly like an absent neighbor. |
| Neighbor validity comes from ground approval | False. Foreground solids retain their existing semantic gate. An independent tile coating or heat/charge overlay must not invent or remove solid topology; an invalid owner effect/material still refuses joining. |
| A perimeter requires a new scene renderer | False. The existing foreground tile and shared subject reader can select cached frames. The flat renderer already expands incremental dirty cells to eight neighbors. |
| Every cell needs an outline | False. An owner joined on all four sides uses its unchanged original interior variant. Cross-family contact remains a boundary; diagonals do not join. |

Preserve all original 26 palette-corrected cells byte-for-byte. Append 45 cells
for a **71-frame, 1136×24** atlas. Each added cell is a single native 16×24
sprite. Four exposed-edge bits are **N=1, E=2, S=4, W=8**. A bit is set when
that side lacks a disclosed, selected and validated exact same-family neighbor.
Out-of-zone cells count as absent; do not load an adjacent zone. The 15 nonzero
masks per family are ordered numerically:

| Family | Mask 0: all four sides join | Masks 1–15: exposed perimeter |
| --- | --- | --- |
| TepuiWall | Existing frames 4–5 with the existing coordinate variant | `26 + mask - 1` (26–40) |
| GrainRidge | Existing frames 6–9 with the existing coordinate variant | `41 + mask - 1` (41–55) |
| StoneDome | Existing frames 20–21 with the existing coordinate variant | `56 + mask - 1` (56–70) |

The added frames use the corresponding original base mass and a restrained
one-pixel edge cue. They must remain flat top-down sprites: no extrusion, tall
side faces, added geometry outside the owner footprint or decorative objects.
Mask 15 describes an isolated owner; mask 0 remains a quiet interior. Existing
water/tank/vein/floor/ledge and borrowed Tree/Bush frames are unchanged.

The catalog first validates the center owner through the existing current
manager/cache, disclosure, selection, placement and exact semantic gates. Only
then may it inspect four bounded current cells. Resolve their selected owner
without recursively resolving presentation; apply the same nonrecursive body
validator and exact blueprint-family equality. No LINQ, allocation per query,
RNG, cached mutable topology, zone generation or world scan is required. Source
queries cannot mutate owner state or materialize a lair ledger.

Retain the existing shared world/reader frame parity after current-document and
selected-owner authorization. The reader may use the same disclosed-neighbor
mask as the world and must not consult hidden neighbors. This preserves the
current public subject-sprite contract rather than introducing a separate reader
representation during a perimeter fix.

### RED → GREEN and actual-view acceptance

Before production changes, extend the existing fixture with exact all-16-mask
checks for each family, asymmetric north/east orientation checks, same-family
versus other-family/diagonal/covered/hidden/remembered/invalid-owner controls,
zone-edge behavior, current-graph revocation, read-only queries and actual
submitted dirty-neighbor repaint. Preserve the prior resource-fallback, wet-zero,
Well, harvest and earned-product portrait controls. Update the atlas dimensions
and family-frame expectations without weakening unrelated owner assertions.
Root runs and retains native RED before runtime or final expanded atlas changes.

After RED, implement the minimal cached frame choice, append native source art,
then rerun the focused and related native suites. Actual ordinary views must
show continuous solid masses with clear traversable borders, quiet interiors,
unchanged water/routes and no visible per-cell checkerboard. Recheck all five
formations, especially large SummitScrub domes and Grainfield gaps, then ordinary
reader/collision and fresh Continue. Test counts or contact sheets alone cannot
close this visual defect or the broader whole-chunk fullness requirement.

Source references: `FlatPixelStumpMaterialsCatalog.cs` current `ReadyCell`,
`Selected`, `Placed`, `DryFrame` and shared reader;
`FlatPixelFallbackCatalog.cs` current selected-owner/disclosure policy;
`EnvironmentSpriteRenderer.cs` `PostRender` eight-neighbor expansion and
`ResolveCell` zone-to-tile projection; `MovementSystem.cs` direction offsets;
`DestructionSystem.cs` ordinary dirty-cell notification. Independent preflight
by `world_coverage_ledger` confirms the orientation/disclosure approach and calls
out the inverted mask meaning and the existing repaint path. No new production
behavior or visual acceptance is claimed by this addendum.


### Perimeter native RED — 2026-10-27 12:22 UTC

`stump-perimeter-native-red.xml` records111 cases:94passed,17failed,
zero skipped/inconclusive. Sixteen new behavior cases and the71-frame import
contract fail on the unchanged26-frame catalog; the three independent-overlay
controls already pass. Start12:22:25Z, end12:22:34Z. SHA-256
`3a23e22f974514043e071db9f92ac202adc49a93327120a494f2df14168644af`.
The separate independent fixture review found no concrete API or dirty-hook
problem. Runtime/art changes follow this retained failure boundary.


### Actual hourly report — 2026-10-27 12:27 UTC

Root sent the requested itemized report at12:27UTC; next due13:27UTC.
Completed: Overwrit delivery/evidence587837b verified on origin/main; Stump
five-formation before survey, eight material-owner integration, initial644
native passes and actual tank examination/drinking plus dome collision.
In progress: palette correction and71-frame connected-solid perimeter, with
revised related native tests running after111-case RED (17fail/94pass).
No final Stump visual, whole-region, package or push acceptance yet.
Next: corrected five views, ordinary harvest and fresh Continue, reviewed
exact Stump main push, then missing fauna identities and all-world breadth.
Creature/crop/hazard/stair/prop and settlement/underground/special-site gaps
remain open; nuanced animation remains deferred.

### Perimeter native GREEN and frozen source — 2026-10-27 12:30 UTC

**Current candidate: 71 frames, not 26.** The 26-frame counts and atlas hash in
§9 describe the preserved first trial. The current 1136×24 atlas retains all
26 palette-corrected base cells exactly and appends 45 perimeter cells. The
palette-only trial and first trial remain preserved in the editable source
packet; their historical evidence is not the current artifact.

Root's retained `Verification/Live16x24/stump-perimeter-related-green.xml`
records **663 distinct cases, all passed**, with zero failed, skipped or
inconclusive cases, from **12:29:02Z to 12:30:00Z**. The result contains all
**111 Stump cases plus 552 related cases**; those groups must not be added a
second time to the total. XML SHA-256:
`ee0a5cca8aa3fa3ec40e52a1d263f68e69ab97d016af917b087410b4a5538ba3`.
The XML counts, unique full names and source hashes were re-read for this log.
The MCP bridge's initialization-timeout response is preserved separately; this
is evidence of native execution, not a claim that the bridge job succeeded.

The minimal runtime change selects a cached perimeter frame only after the
center and four neighboring cells satisfy the documented disclosure, selection,
placement and same-family semantic guards. Mask 0 keeps the old interior;
mask 1–15 uses the appended family range. No Greybox, generation, collision,
world-state, animation or dirty-render infrastructure change was needed.
Current frozen SHA-256 values are:

| Artifact | SHA-256 |
| --- | --- |
| `FlatPixelStumpMaterialsCatalog.cs` | `854659dee1c9c659fbdc78ae4080a125288bd526b951b37ee95a96ebf113a3a8` |
| Unchanged `FlatPixelGreyboxCatalog.cs` | `62357e1530445eafd80af1cebc261fe21a823b107f8a8dd853d014b5e8058498` |
| `FlatPixelStumpMaterialsTests.cs` | `4069c3b8e7bbfcbad0156043449630623315a1f9cd1da9ceefd7b730a794aa57` |
| Imported `stump-materials.png` | `fd33497368615b60f09ccc2d16efe25dd0033370034c95ce93fe0445bb2af7f1` |

The separate `world_coverage_ledger` reviewer found no unresolved significant
source/API/fixture defect in the perimeter change. Its independent addendum in
`Verification/Live16x24/stump-runtime-independent-review.md` binds the exact
runtime hash and checks orientation, disclosure, frame bounds, reader parity,
resource independence and real dirty-neighbor repaint. The earlier portion of
that record is explicitly an author review; it is not relabeled independent.

The source-art handoff at
`ArtSource/Live16x24/stump-materials/verification/frozen-handoff.json` records
71 frames, 350 preserved inputs, 27,264 native pixel checks, 11 corrupt-output
controls and byte-identical native/contact rebuilds. These source checks are
separate from the 663 native gameplay/rendering test cases. All 45 new frames
change only their selected outside edges; original palette-corrected base-frame
pixels and definitions remain exact.

**Still pending at this boundary:** actual corrected five-formation views,
solid-versus-floor readability under the unchanged live grade, interior-grid
and repetition inspection, whole-chunk fullness, ordinary action/reader and
fresh Continue checks, bounded performance observations and release delivery.
Native GREEN and source contacts do not close the named visual defect. This
log does not claim a new package, Linux execution, physical Deck testing or
complete Stump/world graphics coverage.


### User-play overlap preserved — 2026-10-27 12:35 UTC

The user confirmed they were playing during the first corrected-perimeter
session and explicitly asked root to proceed. Input/route attribution is mixed,
so `stump-interrupted-*` images and profile are retained but do not establish
agent action or final route/performance acceptance. F5 saved their current
Overworld.15.4.0 progress at turn7640,HP38/40. All six isolated-root files were
byte-verified into the persistent local backup
`~/Library/Application Support/DefaultCompany/caves-of-ooo/SaveBackups/UserPlay-20261027T1235Z`;
normal saves were not overwritten. The temporary root was additionally moved to
`/private/tmp/coo-stump-user-play-archived-6cb9319e` before finishing isolation.
Restart the agent comparison separately from the retained turn6810 fixture.

### Final material integration checkpoint — 2026-10-27 12:52 UTC

The clean replay starts from earned `Fixtures/stump-before66` at turn6810,
separately from preserved human progress. Normal map controls visited RimForest
2,3, SummitScrub3,4, ButtressRidge4,5, Grainfield2,2 and CascadeGorge2,5.
Current71-frame atlas is observed in `stump-final-01`,05,07,08,18 actual1280×800
captures. These are five formation viewports, not observation of all18 eligible
addresses or all80×25cells. No teleport, reveal, injected stock or added loot.

- Tank: normal C/Left/X examine (`03`), then C/Left/W drink (`04`); real Well
  text reports cool water. No finite stock or invented wet floor.
- Solid boundary: walked to Summit44,10; Up into dome44,9 retained44,10/HP38.
- Harvest: Grain35,18 vein yielded one carried Tepuibone ID69509 and left only
  TepuiStone. Source reader10, inventory16 and portable reader17 display the
  appropriate distinct source/product identities. Ordinary travel also caused
  a real sari-snake encounter; the snake died and XP rose to28, HP stayed38.
- Gorge: traversed the actual wet basin39,11→43,11 then dry/passable
  DescentLedge43,12, HP38. Retained soaked status is distinct from dry ground.
- F5 saved earned `Fixtures/stump-final68`, turn7360. Fresh isolated C Continue
  restored43,12/HP38, carried ID69509/count1, and the removed Grain35,18 vein.
  Decoded RGB world viewport [0,40,940,640] is identical in final19/final20.

The consolidated `Verification/Live16x24/stump-final-gameplay-receipt.json`
records actions/censuses/honesty bounds. Final profile spans56.10377s with11607
samples in each of four valid recorders, no16384-capacity overflow. Renderer
mean0.101144ms/p950.158792ms/max5.00575ms; input mean0.008069ms; main-thread
max1089.42825ms. Editor calls/capture overhead are included, no matched baseline
or isolated attribution; this is not a build-FPS, optimization, GPU, Linux or
physical Deck claim. Raw streams and `stump-final-profile.json` are retained.

Independent source/test review and the extended actual-image review accept a
**bounded material integration checkpoint**. No new material blocker was found
across the five views. Explicit open visual work: subtle dome blocking semantics,
SummitScrub's broad sparse center, missing glyph creatures, crops/props/hazards,
remembered fallback presentation, special sites and underground. Fullness and
regional/world completeness are not accepted. Continue the saved Stump fauna
plan and other missing regional families before nuanced motion polish.

🟡 Initial material/solid separation defects were corrected by palette-v2 and
45 exposed-perimeter variants after genuine RED111 (94pass/17fail).
🔵 Final663 distinct native cases pass (111Stump+552related); no code changes
followed that run. Raw MCP init-timeout is retained separately from native XML.
🧪 Whole-chunk polish, all18-address observation and current Stump packages remain
open. Existing Overwrit Mac/Linux archive remains the current delivered build.
⚪ Console retains two Metal memoryless depth load/store messages; no clean-log
claim. Normal Editor/standalone saves and foreign38 paths remain byte-identical;
Unity finishes in clean Edit mode with no save-root override. Storage inspection
reports about428GB free; no cleanup/deletion performed.


### Packaged delivery checkpoint — 2026-10-27 13:11 UTC

Source commit `98e3e167b8789608f783701490822a024ffa9963` is verified on
`origin/main`. Fresh source-specific Mac and Linux builds succeeded with zero
errors and four warnings each. The current Mac app is
`Builds/Verification/2026-10-27-stump-materials-98e3e167b-current/CavesOfOoo.app`.
The Linux archive is
`Builds/SteamDeck/2026-10-27-stump-materials-98e3e167b-current/CavesOfOoo-SteamDeck-flat16x24-current.tar.gz`,
128,791,153 bytes, SHA-256
`429a9b9c30ba6ae47b4671c3d670b0a4933c9c98a8a3401acefe874e695956ef`.
Independent archive inspection passed all 189 files/13 directories, internal
checksums, permissions and safe paths. Linux and physical Deck execution remain
unverified.

Actual Mac gameplay used the exact new executable: Continue in CascadeGorge,
Tepuibone inventory/examine, normal map travel to Grainfield, F5, full quit and
fresh-process Continue. Earned `Fixtures/packaged-stump69` retains turn7460,
Grainfield40,12 and HP38/40. The six `packaged-stump-01..06` captures and
`packaged-stump-materials-mac-play-summary.json` record the bounded evidence.
Independent image review found no new package visual regression. Saved/restarted
world pixels match in decoded JPEG rectangle [0,96,940,696]; this is not a
lossless renderer-buffer or Editor-to-package identity claim. Travel/restart
images04/06 are byte-identical; their separate process provenance depends on
root's verified executable/PID observations, not image bytes alone.

An ambiguous name-only app lookup initially opened an older October25 app. It
was closed, the exact new path was selected and Continue repeated before
accepted evidence. Its retained `packaged-stump-interrupted-old-launch.jpg`
is excluded. The first shared Player log also has mixed-launch provenance.
The fresh restart log has zero Exception lines but retains six premature-thread
finalization notices and one ComputeBuffer-disposal notice; no clean-log claim.

The restoration receipt verifies all50,712 tracked source-snapshot files,
including38 unrelated edits; normal Editor/standalone save state and preferences
were restored. Earned package saves were archived separately. Prior builds and
archives remain intact. Unity returned to clean Edit mode. Storage inspection
found about426GB free; no cache or storage deletion occurred. The five formation
Editor views and two packaged area views do not certify all18 eligible addresses,
whole80×25chunks, remaining families or regional polish.

Current next action: the saved Stump fauna preflight, starting with the five
existing crisp16×24 species and preserving essential damage/status cues. Continue
other missing world families. The user's newly required later area-by-area
polish phase is separately tracked; subtle dome collision cues, sparse Summit
center, remaining glyph owners and remembered presentation stay open. No
runtime/art changes occurred after663 native passes; delivery records add no new
runtime behavior. Current delivery-record commit is prepared for root's exact
review and push. Last actual hourly report remains12:27UTC, next13:27UTC.
