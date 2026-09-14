# Stump voxel art kit

Status: 56 models built and integrated, 2026-09-14. ST24 renders 15 native
previews. ST25 passes 363/363 targeted checks, including all 148 new feature
cases. ST26 finishes 12,230/12,262 passing, with the same 32 baseline failures and zero C# errors. CoO-original art, not a Qud source port.

The Stump reads as one petrified body: broad pink-gray stone, continuous east–west grain, knuckle domes, cool spray, squat bromeliad cups and wind-shortened vegetation. The generator supplies fourteen reusable single-cell families with four variants each. Shapes carry variation; each object uses at most two constant palette swatches. No generated demonstration layout is manually edited.

| Family | Exact native alias | Art rule |
|---|---|---|
| ground | TepuiStone | Quiet pink-gray floor; one swatch and level exposed top across variants. |
| wall | TepuiWall | Broad stone shoulder with low continuous foundation and two coarse strata. |
| grain | GrainRidge | Full-cell strata assemble into broad east–west ribs through native footprint and height grades; lower56/upper79 distinguish them from ground76. No per-cell rails. |
| dome | StoneDome | Broad full-cell strata combine into one knuckle through the native outline and height grades; lower56/upper79, without individually narrowed caps. |
| ledge | DescentLedge | Broad low shelf, clearly distinct from a solid ridge. |
| spray | SprayPool | Contiguous cold teal surface, one swatch; native scenery identity only. |
| tank | TankBrocchinia | Squat open cup with raised green leaves and a low central water-colored surface. |
| vein | TepuiboneVein | Pale horizontal seam held within dense pink stone, not a floating aura. |
| bone | Tepuibone | A small pale cut stone chip, not anatomical remains. |
| tree | Tree | Short, broad dwarf tree; restrained wind-shaped canopy. |
| bush | Bush | Low scrub masses in two muted greens. |
| singer | SummitSinger | Small brown frog with dark W back marking, hind legs, throat/head and eyes. |
| sentinel | BrocchiniaSentinel | Flattened lichen-colored lizard with tapering tail, small legs and yellow eyes. |
| key | IronKey | Flat open-ring iron key with shaft and two separated teeth; seven boxes, 168 vertices, gray palette slots 12/13. |

Only ground metadata uses `kind=ground`. All other entries are static `entity` meshes. Singer/Sentinel remain native actors in the parent recipe and use transient views; these unrigged meshes do not add animation, sound or AI behavior. Existing unsupported creature species retain their usual presentation. The library does not infer aliases from names or anatomy.

Current grain grades are 0.62/0.82/1.08/1.32 cells; dome grades are
0.72/0.90/1.08/1.26. Both families use exactly two full1×1 slabs and48vertices.
Current visible cardinal neighbors select the grade, so removing a native
neighbor changes the surviving mass rather than replaying a seed. Stone upper
swatch79=(197,171,148) differs from ground76=(157,131,124); lower56 provides the
second stone hue. All other families retain their earlier geometry and colors.
Native Rubble after wall destruction shares four existing Beating kit variants;
it does not increase this kit's 56-entry count.
Native LockedChest now shares the existing Chest visual binding only in
eligible ordinary Stump zones. Its native LockPart, container owner and
membership remain unchanged. The chest-to-voxel chain already exists; the alias
adds no model, material or palette noise and does not increase the kit count.
ST23 pins the missing IronKey art before production. ST24 appends four coarse
key variants at indices 52–55 without changing the earlier geometry or metadata.
ST25 verifies the complete kit and native owner mapping; the kit now has 56 models.

## Verification sweep and corrections

| Premise / reference | Verified fact and decision |
|---|---|
| Initial parent brief called Singer a bird | **Corrected before tests/art:** Objects.json says a small brown frog with a dark W, Anatomy=Frog, glyph f. The bestiary agrees. Author a frog. |
| Tepuibone sounds like animal bone | Shipped examine text describes sandstone cut from the Root, pale grain and weight12. Art is a dense stone chip. |
| SprayPool is a functional pool | It has Water material and Examinable, without LiquidPool or TileStateSource. Its art never creates liquid contact or permanent wetness. |
| TankBrocchinia offers water verbs | It is walkable vegetation and native Sentinel habitat; drinking/study/inoculation remain W6 debt. A visible cup does not claim implemented water storage. |
| Singer silence drives alarms | Current actor is passive/wandering; native spawning requires nearby Tree habitat. Alarm/audio/manifestation-clock behavior is outside this kit. |
| All stone can already be destroyed | TepuiWall inherits destructibility and wreckage; GrainRidge/StoneDome have no Destructible part. Native authority remains unchanged. |
| DescentLedge changes elevation | It is non-solid scenery, so art stays below0.26cells and adds no collision. |
| Beating dune lesson | Full-X span at every grain height level prevents repeated per-cell grooves; use two coarse strata rather than many tiny voxel steps. |
| Ring material and strict library | Beating library/builder/tests supply the established combined-mesh and reference-validation pattern. Shared assets and palettes are borrowed without edits. |
| Zero missing meshes proves all native owners are modeled | A native owner can lack a recipe before mesh lookup. ST20 exposes LockedChest despite clean preview counters; ST21 adds direct all-owner coverage and native lock/removal/scope controls. |

References read: CLAUDE.md, Docs/STUMP-COMPOSITION.md, all fourteen exact Objects.json blueprints, sarisarinama_bestiary_design.md's Singer and Sentinel entries, Stump-plan corrections, BeatingVoxelLibrary/KitBuilder/KitTests, SpawnRing3DCatalog.Model and existing ring palette samples.

## Test and performance contracts

Tests cover exact56-entry coverage, four distinct meshes per family, one-cell X/Z bounds, matching metadata/prefab/material references, no prefab colliders or scripts, and at most240vertices. Singer has a bounded288-vertex allowance for frog anatomy plus W mark and eyes; no other model receives the exception. Numerical readability gates cover tall stone versus low shelves, small fauna, short trees, domes assembled from broad cells, open tank cups, quiet flat surfaces and continuous east–west grain. Twelve source-preserving corruption controls and exact-name counterchecks make failure paths explicit.

All geometry is authored offline into shared mesh assets; one renderer per prefab, no cube GameObjects in final assets. Runtime IDs are precomputed, validated lookups are finite, and valid lookup introduces no allocation. The parent integration owns native dirty-patch batching, actor membership/visibility, movement/removal tests and full pipeline verification.

Four additional integration cases now require explicit recipes for every visible
native owner across all 18 Stump addresses at three seeds, plus a real
LockedChest control for owner identity, locked state, membership removal and
out-of-scope refusal. All four fail in ST21 before the exact visual alias;
ST22 passes the chest control but the three all-address cases now fail on
IronKey. The four tests raise the feature's new case count from 135 to 139.
ST23 adds the key-art RED controls; the final feature count is 148, all passing in ST26.

## Reproduction and honesty

Build entry point: `CavesOfOoo.Editor.StumpVoxelKitBuilder.Run` inside the disposable Unity validation project. Generated resources belong under `Assets/Resources/StumpVoxel3D`; rebuilding preserves existing GUIDs and never saves a scene.

ST11 rendered all five formations at seeds64,1729,729490642 through the native
pipeline and presenter at gameplay pitch. All15 images were independently
inspected, and all report zero missing voxel source meshes. Grain/dome masses
are continuous, solid tops remain distinct from walkable ground, spray basins
are intact, and summit tanks sit beside the intended native shelter. These are
the final art assets; ST11's native layouts precede the later camp-apron repair.
ST19's final native preview repeats the same formations/seeds after that repair;
all15 images were independently inspected, with no material visual regression,
zero C# errors and zero missing meshes.

ST20's full suite establishes a separate limit: zero missing meshes does not
mean every visible native owner obtained an explicit recipe. LockedChest was
refused before mesh lookup. The new owner-enumeration gate tests that earlier
stage directly; clean ST19 images are not reported as proof of post-alias
coverage. ST22 then exposes IronKey behind the chest's first failure. The
expanded gate caught both missing recipes and passes in ST25 after the exact
chest alias and key art are installed. ST24 supplies the final 15 native views.

Tests prove numeric structure and reference integrity; previews add direct
composition evidence. Neither proves live animation feel, small-animal
recognition during movement, gameplay lightmaps or frame rate. Singer/Sentinel
models remain static, with native actor ownership and transient movement views.
No original Unity scene is launched or mutated by this work.

## Self-review and integration boundary

- 🟡 Corrected after RED: repeated narrowed dome caps, crosswise grain rails and
  solid tops that shared the exact ground color. Current full-cell slabs and
  upper79 solve those art-generation defects without new detail noise.
- 🟡 Corrected integration: wall destruction now keeps native Rubble visible
  through shared coarse variants. Ground/fixture ownership, visibility and
  removal stay controlled by the live native entity, not by procedural replay.
- 🟡 Chest and key repairs verified in ST25: ordinary Stump LockedChest
  shares the existing chest model while retaining its real lock and owner.
  The extra recipe gate addresses a blind spot in the earlier preview metric
  and verifies the IronKey mapping that the chest failure previously masked.
- 🔵 Corrected before authoring: Singer is a brown frog with a dark W, and
  Tepuibone is a heavy mineral chip. Palette and anatomy choices follow the
  actual blueprint/bestiary, not a name-based guess.
- ⚪ No new liquid contact, water storage, drinking, alarm audio, flight,
  anti-Urqu field or universal destruction is implied by the art. Recorded W6
  affordance debts and the user's Vein Pressure deferral remain separate.
- 🧪 Static previews and asset checks do not replace live feel/performance
  verification. Headless integration is verified in ST26; live feel remains unverified.

The final native camp access fix belongs to StumpCompositionBuilder and its
composed manager route: cloned ambient stamps gain one open reserved cell
around their footprint. No shared catalog is mutated and no model geometry,
palette, sprite or library count changes. It follows two diagnosed blocked
WarbandCamp entrances and ST16 compile RED. ST18 verifies the repair after a
stale ST17 harness copy was corrected. ST19's post-apron native previews pass
visual review. ST20's later full run and ST21 recipe RED are recorded below;
ST25 verifies both chest and key repairs; ST26 confirms no new failures above the recorded baseline.
See STUMP-COMPOSITION.md
for exact coordinates and evidence.

## Implementation log

- ST02 missing-type RED confirmed by parent:81 C# error lines, all solely missing StumpVoxelLibrary. Production began only afterward.
- Initial ST05 build:52 models in13 families through one reusable offline builder. Static prefabs have a single combined mesh and shared material, no scripts or colliders. Singer uses12 boxes/288vertices for brown frog body, dark W and eyes; Sentinel uses10/240; tanks9/216; all remaining families five boxes or fewer.
- Initial geometry, superseded below: grain used full-X strata; dome used broad crossed tiers and a smaller cap. Height grades were already0.62/0.82/1.08/1.32 and0.72/0.90/1.08/1.26. The implemented parent recipe derives grades from current visible same-blueprint cardinal neighbors via clamp(count−1,0,3), preserving native removal.
- Initial palette, with the grain/dome top correction recorded below: stone76/56, spray28, bromeliad16/28, vein/chip76/19, dwarf tree8/48, scrub48/16, frog9/10, lizard31/21. Exposed ground and spray surfaces retain one fixed swatch and height.
- Source review corrected an initial draft tree-tier separation before handoff: canopy intervals overlap at every dwarf-tree height. Frog anatomy, mineral chip identity and descriptive pool/tank boundaries remain explicit. Later build/preview and final ST26 integration evidence are recorded below.

## ST05 mass-composition correction

Viewed native SummitScrub-64 and ButtressRidge-64. An individually rounded/stepped dome is the wrong unit when many native cells together describe one large knuckle: repeated narrowed caps create a waffle texture. Grain's 0.7-depth upper stratum similarly repeats rails across north–south rows. This is a composability defect in the art-generation rule, not a reason to reposition the rendered demonstration.

The correction kept native footprints and current-neighbor height grades responsible for the overall dome/ridge shape. Each grain/dome cell became two full1×1 slabs with two existing stone hues and unchanged height grades. No individual cell narrows or overhangs. Eight new continuity cases require every horizontal level to reach exactly ±0.5 on both X and Z, with a48-vertex budget. The earlier narrow-dome-cap assertion was explicitly replaced by broad composable-cell coverage; low walkable ledge checks remain. Production was held unchanged until ST06 RED confirmation. Other eleven families were unchanged.

ST06 confirmed9 intended art failures: the corrected broad-dome assertion and all eight slab continuity cases. The other art cases remained green. Only Grain and Dome generator methods changed: both emitted two full1×1 slabs, with the original height grades unchanged. Each has48vertices (dome down from120); all other model families were untouched. ST08 rebuilt and rendered these corrected masses before the subsequent contrast correction.

## ST08 quiet collision-readability correction

Viewed refined SummitScrub-64 and ButtressRidge-1729. Full-cell slabs formed continuous masses, but their top used the exact ground swatch76, making blocked terrain depend almost entirely on shadows for visibility. The correction changed only grain/dome upper slabs to pale stone79=(197,171,148), retaining lower56 and ground76=(157,131,124). Two colors per object and all geometry/height grades remained unchanged; no texture noise was added.

Two new pre-production cases require grain and dome tops to use one shared swatch across variants that differs from walkable ground, while retaining exactly two total swatches. The builder remained unchanged until ST10 RED after the ST09 run. Other eleven families remained outside this change.

ST10 confirmed exactly the two new contrast cases RED, with122 other targeted cases passing and zero C# errors. Grain and dome upper-stratum calls now use swatch79; lower56, ground76, all52 geometries and the other eleven model families are unchanged.

## ST11 final-art inspection and later integration gate

All15 ST11 native images were inspected after the final rebuild. The earlier
waffle/rail patterns and shadow-only footprint readability were resolved. All
three gorge previews contain exactly three unequal basins, and all six summit
previews contain eight tanks in their required native shelter neighborhoods,
with both endemic species present. Receipts report zero missing voxel meshes.

ST12 then exposed two native camp-access failures in a stronger traversal gate;
ST15 diagnosed the exterior barrel/neighboring wall blockers. ST16 confirmed
missing-method RED for the local catalog/apron repair. The camp change touches
generation placement only, so the art remains as inspected in ST11; a final
camp-layout or full-regression result cannot be inferred from those images.
ST17 returned 285/287 with the same access failures and zero C# errors, but source
hashes proved the isolated harness still held the old manager integration. The
six exact manifest keys were synchronized and checked without a production
source change. ST18 then passed 287/287, including all 135 new cases, with zero
C# errors. The refreshed ST14 source/art audit matches 240 files exactly between
projects; all 116 new metadata GUIDs are collision free, and the scoped integration
patch was regenerated and reverse-checked. ST19 then rendered all 15 post-apron native
views with zero C# errors and zero missing meshes. Independent inspection of
all 15 found no material visual regression, with three also rechecked by the root.

## ST20 full-suite findings and explicit native-owner recipes

ST20 reports 12,249 total, 12,212 pass and 37 failures, with zero C# errors.
Five exceed the 32 recorded baseline failures: three native-ground-graph cases
lack a LockedChest recipe, and two older voxel-authority negatives point at
ordinary Stump addresses that this feature intentionally supports. Only those
two negative inputs change: 2.5→protected Stillleaf mouth2.4, and
4.5→protected Root3.3; the negative assertions remain intact.

ST21 confirms all four new cases RED: 30 total, 26 pass, four LockedChest
failures and zero C# errors. Three seeds enumerate all visible owners in all
18 native zones; the fourth keeps the real locked chest and tests membership,
removal and a foreign-zone countercontrol. The exact Stump-only LockedChest→Chest
alias follows RED and preserves native LockPart and owner membership. The
existing chest voxel chain was verified, so the alias leaves the kit at 52
models and the feature has 139 new tests.

ST22 returns 354 total, 348 pass and six failures, with zero C# errors. The
chest repair passes; IronKey now fails in the three old native-ground-graph
cases and the three new all-address cases. The earlier chest failure masked
this second unmapped native owner. Four coarse key models are planned as a
fourteenth family, with dedicated RED tests before production. The intended
56-model kit had not yet been built at ST22. The later ST23 RED, ST24 build
and ST26 final regression are recorded below.

The seventh mixed-file baseline captures the pre-existing untracked
VoxelWorldIntegrationTests before its two negative-input edits. Together with
the six runtime integration baselines, it supports a scoped delta rather than
wholesale staging of mixed files. The source audit is being updated from 240
to 241 files before accounting for any additional key-art files; the earlier
240-file match is historical evidence for its earlier source state. See
STUMP-COMPOSITION.md for the exact seven-file boundary.

## Final IronKey extension and verification

ST23 confirms 13 RED failures across key art/alias and recipe coverage before
production. The four appended keys use seven boxes/168 vertices, palette slots
12/13, a real open bow, a shaft and two separated teeth. Bounds are within one
cell: X −0.45…0.4525, Z −0.19…0.22, height 0.08…0.11. Variants change thickness
and tooth depth, with no texture noise or individual voxel objects.

ST24 builds all 56 model/prefab pairs and renders 15 native scenes. All 209
previous art/metadata files remain byte-identical; Library.asset gains the four
entries. Native IronKey ownership, KeyPart and Takeable behavior remain intact.
ST25 passes all 67 art-library cases and all 148 new feature cases within the
363-case targeted run. Independent review finds no material source or static
presentation issue. The final kit has 124 new metadata GUIDs without collisions;
the source/art audit covers 257 exact original/isolate file matches.

The gallery now uses ST24 as its final view. ST26 finishes 12,230/12,262 passing, with the same 32 baseline failures and zero C# errors.
Live pickup/animation feel and small-object readability are not claimed from
these static overviews. Existing chests and locked chests share their model;
native names/examination and the lock component communicate their behavior.

## Verified close-out

ST26 finishes 12,262 cases: 12,230 pass and the same 32 failures recorded before
this feature, with zero C# errors. All 148 new cases pass. The existing failures
concern earlier building-art variants, equipment ground sprites and the
Morrowfast/Sill authoring expectations; this pass does not claim a green global
baseline or silently fix unrelated work. `ST14-final/regression-comparison.json`
records the exact name/message comparison against ST00.

The final kit has 56 models, the native preview has 15 scenes, the source audit
has 257 matching original/isolate files, and all 124 new metadata GUIDs are
collision-free. Independent cold-eye and root visual review found no remaining
material issue in the reviewed scope. The 50-case adversarial gate plus targeted
counterchecks cover the generator and native consumers. Original saves, scene,
spawn, camera and reveal settings were not reset. Live player/HUD/lightmap feel
and runtime frame profiling remain explicitly unverified.
