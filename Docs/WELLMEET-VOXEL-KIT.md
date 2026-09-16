# Wellmeet voxel kit

Status: complete and installed, 76 combined-mesh model variants. The two kits total 116 models. FC21b rebuild preserved all 470 asset and metadata files byte for byte; final art tests and owner-coverage gates pass. CoO-original art for native owners, not a new gameplay system or Qud port. Final results and visual limits are recorded in [the area plan](OLDERDEEP-WELLMEET-COMPOSITION-PLAN.md) and [verification receipt](Verification/VoxelWorld/FC23-closeout/README.md).

## Verified native contracts

| Source inspected | Actual behavior | Consequence for this kit |
|---|---|---|
| `WorldMapAuthoring`; TentRight profile stamp | Wellmeet is `Overworld.8.16.0`, TentCamp. First Tent is a separate site and countercontrol. | Exact scope belongs to native renderer routing; this kit does not convert all villages or First Tent. |
| `Objects.json`: TentWall | Solid cloth wall, HP8, flammable and thermally native. | Thin joined cloth panels with a low cutaway top. No extra roof collision, canopy owner or indestructibility. |
| GuestClothPole / TentRightHost | Pole is non-solid and examinable; the host conversation supplies the actual three-day oath. The oath protects from hostile people, not beasts. | Dark readable cloth above the roofs and a distinct host pose. Art adds no safety aura, god icon or oath interaction. |
| SaltMaster | Real PaleSalt exchange grants native TentRight reputation; white to the elbows. | Pale sleeves and salt-dusted arms separate this actor from the host. No decorative altar replaces the service. |
| VillagePopulation / `SettlementSiteVisuals` | This phase enables the existing native repair systems at Wellmeet; main well, oven and watch lantern use their real Parts, colors and effects. A second ordinary camp well has no site Part. | Each service family has four state models. The recipe uses the actual owner-local repair stage; ordinary camp well uses stable variant 2. |
| Bed / Chair / Shrine / WeaponRack | Native bed, chair, Sanctuary and storage Parts already exist. | Distinct compact furniture models retain actual owners. First Tent's no-god imagery is not a reason to delete Wellmeet's functional shrine. |
| FC01 three-seed census | Background residents coexist with traders, scribe, tinker, elder, innkeeper, quartermaster and children. Villager may use the legitimate quest-beacon `p` glyph. | Stable adult role variants plus shorter children; canonical glyph and quest-state guards remain in integration, not the mesh. |
| Old village well asset | The older decorative well spans about 3.75 cells and has thousands of triangles. | New one-cell well preserves the native cell/owner contract and the two-color style. |

## Public library and family contract

`WellmeetVoxelLibrary.ResourcePath` is `WellmeetVoxel3D/Library`.
`ModelId(family, variant)` accepts exactly the ordered families below and 0–3;
invalid input throws. `Family` uses exact blueprint aliases and otherwise returns
null. `Load`, `Find`, and `Validate` use the established native-kit interface.

| Ordered family | Exact native aliases | Art identity |
|---|---|---|
| ground | Sand | Full-cell warm quiet sand, slot 32; buried variation only. |
| floor | StoneFloor | Full-cell dark interior floor, slot 12; not a roof or indoor flag. |
| tent | TentWall | Broad cloth panel and simple pole, 7 / 10; 1.10 high. |
| cloth | GuestClothPole | Dark 10 banner on pale 19 support; 1.92 high. |
| well | Well | Open stone ring and inset water; visible broken-to-repaired course. |
| host | TentRightHost | Warm 8 / 32, cap and open receiving palm. |
| salt | SaltMaster | Muted 64 / pale 19, salt-white sleeves. |
| bed | Bed | Long low straw bed, 8 / 19. |
| chair | Chair | Legs, seat and rear back, 8 / 9. |
| oven | Oven | Open-front stone oven and chimney; repair-specific core. |
| shrine | Shrine | Low broad Sanctuary fixture, 64 / 19; no invented deity figure. |
| lantern | WatchLantern | Raised hooded lamp with repair-specific dim/bright core. |
| rack | WeaponRack | Wood supports and two clear iron weapon forms, 8 / 13. |
| adult | Elder, Innkeeper, Merchant, Quartermaster, Scribe, Tinker, Villager, Warden, Farmer, WellKeeper | Four stable role silhouettes, muted 48 / warm 32. |
| child | VillageChild | Clearly shorter person, 21 / 32, under one cell high. |
| corner | None; TentWall selected by actual cardinal topology | Local +X/+Z half-panels and corner support, 7 / 10. |
| path | RoadStone | Quiet packed earth, slot 7; separate from interior floor. |
| shelf | AlchemyShelf | Open wood shelving and two coarse teal vessels, 8 / 22. |
| marker | WellGroundMarker, OvenGroundMarker, LanternGroundMarker, CampfireGroundMarker | Three very low overlapping worn pads, 12 / 64; .032 maximum height. |

There are 76 assets, named `wellmeet-{family}-{0…3}`. Ground, floor and path have metadata
kind `ground`; all other models describe actual entities. Unsupported content
keeps other exact native kit/catalog aliases rather than being broadly claimed.

### Stable variants with semantic meaning

Adult variants are **0 goods carrier, 1 ledger carrier, 2 tool carrier, 3 plain
traveler**. Integration assigns service roles deterministically. Variant choice
must not change when a person moves to another cell. The plain traveler provides
the no-accessory control; new carried mesh shapes do not invent inventory items.

Well, oven and lantern variants are **0 Fouled, 1 TemporarilyPurified, 2 StableRepair,
3 ImprovedWithCaretaker**. Integration maps actual enum members explicitly; it
must not assume enum numeric order. The ordinary camp well uses variant 2. The
fouled well retains a visibly lower broken front course while repaired stages
close it. Well water remains inside the fixture; no native liquid tile or swim
mechanic is added. Oven/lantern core colors change with the actual repaired owner.
Native Parts remain responsible for light, effects, interaction and description.

## Offline generation and budgets

Call `CavesOfOoo.Editor.WellmeetVoxelKitBuilder.Run()` in the isolated editor.
It rebuilds combined meshes/prefabs under `Assets/Resources/WellmeetVoxel3D` in
place, preserving asset GUIDs and writing no scene. Each prefab has exactly one
mesh renderer referencing the existing ring material, no collider, script or
Unity light, and identity transform. No palette texture or shared source changes.

Every mesh stays inside a single cell horizontally, uses no more than two
swatches and has at most ten boxes / 240 vertices. Runtime presentation borrows
the coarse mesh directly. Quiet ground tops remain constant between variants.
Each roofless panel is a single cell; native wall adjacency determines rotation.

## Verification and self-review

- FC01 census of all three seeds established the required native service owners.
- FC02 confirmed both missing kit types before production.
- Tests cover all 76 entries, exact aliases, invalid inputs, source references,
  metadata, shape distinctness, ground continuity and malformed copy controls.
  Additional visual invariants probe actual well openings and broken/repaired
  ring faces, tent cutaway clearance, chair/oven openings, rack blades, native
  adult accessories, child scale and repair-dependent lantern cores.
- Final source-only audit covers 116 models across both kits: at most 240 vertices
  and two colors, one-cell horizontal bounds, six copied source metas with unique GUIDs.
  This source audit is distinct from the final exported-asset and Unity test
  totals maintained in the parent plan.
- FC17 final WellmeetCamp-64 was inspected independently after the earlier
  three-seed FC13 review. Flat service scuffs replace the oversized charcoal
  marker stars; no remaining blocking material defect was identified.
- 🧪 Images can establish composition and silhouette; they cannot establish live
  input feel, sustained frame rate, or native light animation quality.
- ⚪ The kit changes presentation only. Native repair, shade, sleep, Sanctuary,
  stock, oath and mineral exchange behavior remains independently verified by
  the area's gameplay/presenter tests.

## Implementation log

- 2026-09-15: Verified blueprints, repair visual source, current three-seed census
  and the unsuitable older large decorative well. Scoped 15 families and agreed
  explicit role/state indices with parent before production.
- 2026-09-15: Initial art tests failed with actual missing types in FC02. Wrote
  geometry identity assertions, then the 60-model library and offline generator.
  Source and metadata audits passed; isolated export and native look review followed.

Files: new `WellmeetVoxelLibrary.cs`, `WellmeetVoxelKitBuilder.cs`,
`WellmeetVoxelKitTests.cs`, their metas and this document. Parent owns asset export
and shared native rendering integration.

### FC06 native look review — initial defects and RED proposals

The first exported native WellmeetCamp-64 showed isolated wall panels failing to
turn cleanly around tents, and dark StoneFloor public paths reading as asphalt.
A true corner mesh is preferable to replacing cloth with opaque solid cubes.
The proposal appended `corner` (topology-selected, no fictional blueprint alias), `path`
(RoadStone packed earth) and `shelf` (the actual AlchemyShelf container found by
the postgeneration census), bringing the planned kit to 72 models. Existing IDs
stay stable. Corner local half-panels meet +X and +Z at the native cell center;
rotation comes from current cardinal wall neighbors. The interior quadrant
remains visibly open. A single muted earth slot 7 distinguishes native paths
from dark interior floor and warm sand. AlchemyShelf is a real wood container,
not a decorative bookshelf: the new silhouette exposes two coarse teal vessels.

FC07 passed all current art assertions; new corner/path/shelf and plume tests
were saved for FC08's actual RED before their production geometry changed.
Static review establishes these composition/silhouette defects, not live feel.

- 2026-09-15: FC08 captured actual missing corner/path/shelf and plume refinement
  failures with zero C# errors. Appended three families without changing existing
  IDs, implemented a real L corner and packed-road floor, and added the native
  alchemy container's coarse shelf silhouette. Source-only audit at that stage covered 108
  combined meshes, each within 240 vertices/two swatches/one native cell. Refined
  exported-asset assertions and final native camera review were still pending then.

### FC13 independent three-seed camera review

Reviewed WellmeetCamp64/1729/729490642. The true cloth corners now join the
rectangular tents, and lighter outdoor paths separate public ground from dark
shaded interiors without the earlier asphalt appearance. Clusters of existing
dry brush and rock frame the settlement. Calm surfaces, visible doors, separated
host/salt service positions and accessible open yards remain readable.

A remaining defect was the CampfireGroundMarker→AshBed fallback: native markers
appeared as tall charcoal stars around a fire. Repair enablement also exposed
Well/Oven/LanternGroundMarker. Their exact native blueprint descriptions
are damp flagstone, soot-stained stone and lamplit stone, with no collision or
new interaction. The proposal used one quiet, very low `marker` quartet for all four,
appended at offset 72; maximum height .045, broad broken perimeter, at most three
boxes, palette 12/64, metadata kind `entity`. Per-owner variants are stable, while
main service fixtures communicate repair state. Main-markers do not become wells.
This raised the planned kit to 76 assets; removal of the inappropriate ash
fallback was held for actual RED. No new family is needed for Farmer and WellKeeper:
both are verified native Villager-derived `@` actors and use exact adult aliases.
Tests included spelling/unrelated-owner countercontrols. Production additions were
held for FC14/FC15 RED. No further blocking visual issue was identified in these
three static images; animation feel and sustained performance remain unverified.

### Final source handoff after FC15

FC15 captured actual marker/wall/alias RED with zero C# errors before production.
Appended the marker quartet at offset 72 and exact Farmer/WellKeeper adult aliases;
all existing model IDs remain stable. Each marker is a .032-high, three-box worn
pad with a broken outer perimeter and quiet stone/soot colors. Parent integration
removes the old ash fallback and preserves the current native marker entity.
The main well, oven and lantern remain the visible repair-state indicators.

Final source audit: 116 total meshes across both kits, at most 240 vertices and two
swatches per mesh, one-cell horizontal bounds and six unique copied source metas.
No further visual refinement is proposed after the scoped material/marker fix.
At the FC15 handoff, final asset rebuild, targeted tests and the full suite were
still pending. Those verification totals remain parent-owned; no Unity launch,
user scene edit or shared integration edit was performed by the art task.

### FC17 final visual acceptance

Independently viewed [WellmeetCamp-64](Verification/VoxelWorld/FC17-final-preview/WellmeetCamp-64.png).
The repair and fire markers now read as small flat worn patches around their
actual service fixtures. The tall charcoal stars are gone, and the wells, oven,
lantern and fires remain distinct focal objects. Joined cloth corners, lighter
public paths, dark shaded interiors and grouped shoulder vegetation remain
readable. No remaining blocking material defect was identified in this final view.

This final pass covers this image only; the three-seed composition review is the
separate FC13 record above. It does not establish live input feel, light animation
quality or sustained frame rate. Parent verification owns final asset/test totals.
