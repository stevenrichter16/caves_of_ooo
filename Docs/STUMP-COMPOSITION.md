# The Stump: grain, spray and petrified canopy

Status: complete and verified, 2026-09-14. All 18 ordinary
Stump chunks, five formations and 56 voxel models are integrated. ST25 passes
363/363 targeted checks, including 148 new cases. ST26 finishes 12,230/12,262 passing, with the same 32 baseline failures and zero C# errors.
CoO-original composition and art; no Qud parity claim. Vein Pressure is saved
for later in VEIN-PRESSURE-DESIGN.md.

## Scope and spatial grammar

Extend the biome composition system to the 18 ordinary surface Stump addresses:
11 foothills, three slopes and four summit. Preserve authored band selection and
the five native formation weights. Exclude Root (3,3), Felling-Site (3,5), Stillleaf
mouth (2,4), the multi-cell pilot (3,7), every named place/mouth and runtime POI.
Do not rebuild loaded/saved chunks or change camera, spawn or full reveal.

- CascadeGorge: three intact unequal spray basins aligned along a drainage cut,
  separated by dry necks and low ledges. Broad dry shoulders and approaches
  skirt the basins. Outer stone stays quiet; vegetation concentrates near the
  drainage. This describes scenery, not continuous simulated flow or bridges.
- Grainfield: broad east–west fossil ribs with unequal interruptions and an open
  recess. Seed changes do not rotate the global grain direction.
- ButtressRidge: a few thick root fingers fan downhill and taper into walkable
  pockets. Both slope formations attempt three short east–west tepuibone seams;
  bounded placement and connectivity repair retain the native harvestable owners.
- SummitScrub: knuckle domes enclose airy clearings; tanks cluster at their edges.
  Small subordinate tree pockets provide native Singer habitat.
- RimForest: one humid winding green crack through exposed stone, with grouped
  dwarf trees and openings rather than two vegetation fences. Small tank pockets
  provide the other summit endemic's native habitat.

Large landforms and connected routes precede local vegetation. Three-cell dry
portals match adjacent Stump chunks for a given seed. Each mass consists of
independent native cell owners; no cosmetic terrain replay resurrects removed
objects. Ordinary Stump ambient structures now receive a one-cell open apron
checked and reserved by LandmarkBuilder, protecting their entrances from later
stamps and props. This uses cloned local stamps, not a modified shared catalog.

## Verification sweep / corrections before implementation

| Premise | Verified contract and decision |
|---|---|
| Stump has 21 authored cells | Current map has 22; the southern root spur is an existing authored pilot, not new wilderness. Final eligible count is 18. |
| Pools imply drinkable/permanent water | SprayPool has Water material and examine text only. Preserve this native identity; no invented LiquidPool or wetness lease. |
| Tanks are usable water sources | TankBrocchinia is walkable vegetation and real Sentinel cover; drinking/study/inoculation are W6 debt. |
| Reserving habitats protects ecology | PopulationBuilder excludes all reserved cells before habitat filtering. Temporarily expose only protected walkable habitat cells to the existing band population pass, then restore reservations before later dressing. Never bypass flag rolls or synthesize fauna. |
| Existing summit terrain supports both endemics | Scrub lacks Tree; RimForest lacks tanks. Every new summit plan must supply both, with native eligible open cells. |
| All rock scenery is destructible | TepuiWall has HP40/hardness3 and yields Rubble. GrainRidge and StoneDome have no Destructible. Retain current behavior and do not claim universal destruction. |
| Native ledges change elevation | DescentLedge is walkable scenery. Bands select content and tint, not actual per-cell height. Keep ledges low. |
| Mineral appearance implies an aura | Veins yield real heavy Tepuibone (weight12); neither ordinary item nor Wardline has an anti-Urqu field. Preserve harvesting and trade only. |
| Manifestation and silence already run | Requires UrquActive/Forbids EcologyDamaged are real spawn gates; their clock and Singer alarm are recorded W6 debt. Preserve gates without claiming new AI/audio mechanics. |
| A passable doorway guarantees access to its room | Later barrels or neighboring camp walls can close its exterior cell. Clone the native ambient catalog with an open one-cell apron; use it only in the composed Stump route. Preserve stamp chances, tiers, legends and shared source objects. |
| A preview with zero missing meshes proves every owner has a recipe | The missing-mesh counter covers resolved visual sources, not every visible native owner. Add an explicit recipe/ownership gate across all 18 addresses and three seeds, plus a locked-chest countercontrol. |

Read references: Lore/10_Bible.md (authority), aligned Lore/History/02_Geography.md,
FELLING-WORLD-DESIGN §3.6, FELLING-W6-PLAN implementation log bottom-up,
StumpBands, FormationSelector, StumpFormationBuilder, StumpFaunaHabitat,
PopulationBuilder and PopulationTable, OverworldZoneManager, WorldMapAuthoring,
SinkholeSites, MultiCellPilotRuntime, HazardTerrainBuilder.IsOpenFloor,
LandmarkBuilder.FootprintClear, exact Objects.json blueprints, existing Stump
harvest/ecology tests, and Beating's native-owner rendering integration.

## Readiness and milestones

🟢 Native composition, protected routes, habitats, mineral access and camp aprons.
🟢 The 56-model kit, native-owner presentation and all 148 new targeted cases.
🟢 ST26 full regression: all 148 new cases pass; no new failures above the 32-case baseline.
⚪ Existing W6 mechanics debts and live feel/performance remain outside this pass.

The implementation followed plan → verification sweep → RED → implementation →
visual/adversarial review → repair cycles. The dedicated adversarial fixture has
50 cases; tests include counterchecks for eligibility, reservation restoration,
real native ownership, missing content, destruction, harvest and rendering.
Fifteen seeded native previews expose all five formations. Revisions improve
generator and asset rules rather than manually moving demonstration objects.

## Performance and honesty bounds

Bounded generation-only masks; prebuilt combined voxel meshes, existing linked
mesh batching and dirty patches. No per-voxel GameObjects or runtime voxel build.
Static overviews verify composition, not live input feel/lightmaps or frame rate.
The disposable preview is the reproducible visual scenario. Tests verify actual
native consumers, not a separate demo model. No migration work is needed.

ST11 renders exercise all five formations at seeds 64, 1729 and 729490642.
All 15 use the actual native pipeline and report zero unmapped voxel source
meshes. These images establish the final art and corrected basin/tank layouts,
but precede the later camp-apron change. ST19 repeats all 15 native previews
after that repair, with zero C# errors or missing meshes; independent inspection
finds no material visual regression. ST20 subsequently proves that this
missing-mesh metric does not establish explicit recipes for every visible
native owner: LockedChest can be refused before reaching mesh lookup. The new
ST21 gate enumerates every owner across all 18 native addresses at three seeds,
requires explicit models for visible owners, and checks owner identity and
unchanged native entity versions. ST22 exposes IronKey after the chest refusal
is repaired. ST25 and ST26 verify both fixes and the expanded recipe gate. Static previews cannot establish live input,
small-creature recognition during motion, animation feel, gameplay lightmaps or frame rate.
Recorded cold-generation timings are not a warm gameplay performance profile.

## Current implementation and scope decisions

The manager keeps authored POI routing ahead of ordinary biome routing. Only
eligible ordinary Stump addresses select StumpCompositionBuilder, the cloned
ambient catalog and StumpHabitatPopulationBuilder. Other Stump routes retain
their existing formation and population pipeline. The population wrapper owns
the actual generated Zone reference, opens only still-valid habitat cells,
excludes stairs, calls the unchanged native band table/filter, and restores
reservations in a finally block.

```csharp
var terrain = new StumpCompositionBuilder(WorldSeed);
var composedPipeline = CreateSurfacePipeline(BiomeType.Stump, tier, terrain);
composedPipeline.RemoveBuilders<LandmarkBuilder>();
composedPipeline.AddBuilder(new LandmarkBuilder(BiomeType.Stump, tier,
    StumpCompositionBuilder.CreateLandmarkCatalog()));
composedPipeline.RemoveBuilders<PopulationBuilder>();
composedPipeline.AddBuilder(new StumpHabitatPopulationBuilder(terrain,
    PopulationTable.GetStumpTable(band, tier)));
```

CreateLandmarkCatalog allocates new StructureStamp instances, row arrays and
legend dictionaries. It pads each source footprint with open `.` cells and
copies Chance, MinTier and ClearsVegetation. Existing LandmarkBuilder checks
the enlarged footprint and reserves its apron. No global catalog, legacy
Stump route or foreign-biome stamp is edited by this operation. The larger
footprint may change which ambient stamps fit and where they appear; preserving
usable entrances takes priority over reproducing a formerly blocked placement.

The renderer resolves current native owners only. Grain and dome height grades
come from current visible cardinal neighbors; their two full-cell slabs use
lower swatch 56 and upper swatch 79, distinct from walkable ground 76. Singer
and Sentinel are static transient actor views. Destroyed TepuiWall owners leave
native Rubble using the four existing Beating voxel variants; no new rubble
identity or repair-on-render mechanism is introduced.

An exact `LockedChest` alias borrows the existing `Chest` visual binding only
in eligible ordinary Stump zones. It retains the real container owner, LockPart
and membership. Locked and ordinary chests share appearance; the native name,
examine text and lock mechanics remain authoritative. Four new IronKey variants
append to the original 52 models. They have an open ring, connected shaft and
separate teeth in two gray iron colors; native Takeable keeps them transient.
ST23 confirms the missing-key RED; ST24 builds the art; ST25 verifies every
visible owner's recipe across all 18 addresses and three seeds. All 209 earlier
art/metadata files remain byte-identical apart from the expanded library asset.

| Design boundary | Current scope |
|---|---|
| Original connected spray ribbon | Replaced after visual RED by three intact unequal basins with dry necks. SprayPool still has no LiquidPool or TileStateSource. |
| Individually capped domes and crosswise grain rails | Replaced after art RED by full-cell slabs whose native footprint and neighbor heights define the larger form. |
| Uniform summit tanks | Exactly eight tanks cluster within two cells of scrub domes or three cells of rim trees in the tested plans; both formations retain Singer trees and Sentinel cover. |
| Universal object destruction | Not added. Native destructibility, harvestability and non-destructible terrain remain explicit. |
| Broad camp/stamp repair | Scoped to cloned ordinary Stump ambient stamps. HaulablePropBuilder's shared reservation guard is the separately verified cross-biome safety correction. |
| Locked camp chest presentation | Exact Stump-only LockedChest→Chest visual alias; native lock, container owner and membership remain unchanged. ST21 proves RED before the alias; ST22 passes the chest repair and exposes missing IronKey mapping next. |
| Lore expansion | No new quests, archive access, Root room, endings, flight, anti-Urqu aura, tank verbs, Singer alarm or manifestation clock. Mystery Ledger questions remain untouched. |

## In-phase self-review

- 🟡 Resolved by earlier targeted gates: explicit Felling-Site exclusion;
  missing renderer/actor wiring; clipped spray basins; tanks detached from
  shelter; dome waffle and grain rails; ground-colored solid tops; missing
  post-destruction voxel rubble; habitat windows accepting a different Zone or
  stair cell; haulables ignoring reservations.
- 🟡 Resolved and verified in ST18: camp interiors isolated by
  an exterior barrel or adjacent camp wall. The local apron prevents these
  placements rather than deleting obstacles or rebuilding player-modified zones.
- 🟡 Resolved in ST25: locked chests and iron keys previously lacked recipes.
  Zero missing-mesh counters could not detect an owner rejected before mesh
  lookup. The new all-owner sweep now covers all 18 addresses and three seeds;
  exact lock/membership and portable-key controls guard against cosmetic fixes
  replacing native entities or making keys stationary.
- 🔵 Corrected documentation/fixture assumptions: current 22-cell band census,
  18 eligible addresses, Singer is a frog, and the reduced WorldMap factory
  needs the new native terrain keys. The wall identity test remains strict at
  a foothill address instead of requiring arbitrary outcrops on a grain slope.
  Two older VoxelWorldIntegrationTests negatives referred to newly supported
  ordinary addresses; their inputs now use protected Stillleaf and Root sites.
- 🧪 Static model anatomy, all 15 ST11 images and all 15 final ST19 images were
  inspected. Small creature recognition during live movement, animation feel
  and performance require their own live evidence and are not inferred from
  the previews.
- ⚪ Canon review preserved the Root, Felling-Site, Stillleaf and multi-cell
  pilot exclusions. Existing W6 silence/cloud/tank/flight debts and the user's
  Vein Pressure deferral remain recorded; this feature does not close them.

The first cold-eye pass found real visual issues. The subsequent player-flow
checks also found the two blocked camp entrances after broad traversal and
presentation checks had passed. Therefore ST11's clean visual review is not
reported as proof that every native interaction was complete.

## Files changed

- New StumpCompositionPlan and StumpCompositionBuilder, including the scoped
  population wrapper and cloned ambient catalog.
- OverworldZoneManager: ordinary Stump composition/landmark/population routing.
- HaulablePropBuilder: honor existing generation reservations.
- StumpVoxelLibrary, StumpVoxelKitBuilder, StumpCompositionPreviewBatch and
  Assets/Resources/StumpVoxel3D: strict library, reproducible 56-model kit and
  disposable native preview.
- SpawnRing3DRecipes/Catalog/Library/Presenter and VoxelWorldPresentation:
  finite coverage, native aliases, current-neighbor heights and shared rubble.
- StumpCompositionTests, StumpCompositionAdversarialTests,
  StumpCompositionRefinementTests, StumpVoxelKitTests and
  StumpCompositionRenderingTests; surgical StumpFormationTests/WorldMapTests
  fixture corrections and two VoxelWorldIntegrationTests negative-address
  corrections; associated metadata and verification artifacts.
- This document and STUMP-VOXEL-KIT.md. Mixed pre-existing integration changes
  remain protected by the captured baseline and scoped feature deltas.

Seven mixed files have captured baselines: six runtime integration files
(OverworldZoneManager, SpawnRing3DRecipes, SpawnRing3DCatalog,
SpawnRing3DLibrary, SpawnRing3DPresenter and VoxelWorldPresentation), plus the
pre-existing untracked `Assets/Tests/EditMode/VoxelWorldIntegrationTests.cs`.
The seventh was captured before changing only the negative inputs from
Overworld.2.5.0 to protected Stillleaf mouth Overworld.2.4.0, and from
Overworld.4.5.0 to protected Root Overworld.3.3.0. Their exclusion assertions
remain strict; ordinary supported addresses are exercised positively elsewhere.
These seven mixed files are not staged wholesale. The final source audit
compares 257 source/art files exactly; all 124 new metadata GUIDs are unique.

## Implementation log

- ST00 baseline: 12,114 total; 12,082 pass; same 32 recorded failures; zero C#
  errors. Isolated project /tmp/coo-spread-validation, healthy MCP throughout.
  Captured six pre-existing mixed integration files before new changes.
- Independent lore survey confirms whole ordinary Stump is bounded and records
  the two summit habitat gaps and reservation/population conflict above.

- ST01 confirms missing native plan/builder RED. ST02 confirms missing art
  library RED. ST03:53 cases,40 pass,13 fail; two expose the Felling-Site's lack
  of a named-place record (now explicitly excluded),11 confirm missing native
  integration. ST04 confirms all five renderer/actor/height checks RED before
  wiring. The wrapper retains normal PopulationBuilder configuration, native
  habitat predicates and flag rolls; no direct guaranteed-fauna stamp was added.
- ST05 builds52 native voxel models and renders15 full-pipeline chunks with zero
  C# errors and zero unmapped voxel source meshes. Both summit species appear in
  each preview; foothills retain CascadeFather. Visual review finds fragmented
  spray basins, individually capped dome checker patterns, crosswise rib stripes,
  and tanks scattered away from shelter. These need generator/art fixes before
  close-out; baseline preview is not the final result.
- Art verification corrected an early mistaken bird interpretation: SummitSinger
  is canonically a small brown frog with a dark W. New static frog/lizard models
  preserve the native anatomy and do not imply a new call/alarm/animation system.

- ST06:183 tests,165 pass,18 failures; confirmed look regressions plus native
  stair/reservation and same-address/different-Zone ownership failures. A late
  haulable placement could also occupy reserved arrival space. ST07 confirms
  that exact placement bug and missing post-destruction voxel rubble RED.
- Fixes: intact unequal basins precede dry-route search; tanks cluster beside
  scrub domes or rim trees; grain/dome cells join as two broad slabs. The habitat
  window now requires the original native Zone reference and excludes stairs.
  HaulablePropBuilder respects GenReservedCells. Broken walls reuse the existing
  four coarse Rubble variants while preserving each native owner.
- The older wall identity check moved from slope(2,2) to foothill(2,1): slopes
  deliberately express their solid terrain as GrainRidge, so adding arbitrary
  outcrop scatter just to preserve the old density assertion would contradict
  this composition. The assertion still proves native TepuiWall and excludes
  desert SandstoneWall; this is an explicit formation-specific test correction.
- ST08 rebuilds52 models and rerenders15 native scenes, zero C# errors/missing
  voxel source meshes. ST09:316 tests,314 pass, two WorldMap fixture failures;
  every Stump feature and reviewed existing Stump/Beating art case passes.
  The reduced WorldMap fixture lacks six newly required native identities;
  add those keys without weakening its routing or follower assertions.
- ST08 visual finding, since corrected: raised grain/dome tops shared ground
  swatch 76, hiding solid footprints except for shadows. ST10 confirmed the two
  new contrast assertions RED before changing only their upper slabs to 79;
  lower 56 and ground 76 remain unchanged. The reduced WorldMap fixture repair
  also passes its targeted check. No geometry or palette-noise expansion was
  required for this readability correction.
- ST11 final-art native preview: all 15 images independently inspected. Gorge
  basin sizes are [72,84,125], [74,84,136] and [74,84,127] for the three seeds;
  every summit example has eight tanks at the required shelter distance and
  both endemic species. Grain/dome masses are continuous and distinguishable
  from ground; no missing voxel meshes are reported. This art review precedes
  the later camp-apron implementation and is not a post-apron layout capture.
- ST12 targeted sweep: 331 tests, 329 pass and two failures, zero C# errors.
  Stronger final-pipeline reachability finds stranded environmental cells inside
  WarbandCamp at Overworld.5.3.0 seed64 and Overworld.2.5.0 seed729490642.
  ST15 access diagnostics identify the actual blockers: a barrel at (26,5) in
  the former and an adjacent camp wall at (69,5) in the latter.
- ST16 confirms missing CreateLandmarkCatalog compile RED before production.
  The implemented repair clones native ambient stamps with a one-cell open
  apron and wires them only into the composed Stump manager branch. Source
  catalog objects, original layouts and other routes remain untouched.
- ST17: 287 tests, 285 pass, two repeated access failures, zero C# errors. Hash
  comparison proved that the validation harness had missed the nested captured
  OverworldZoneManager path and retained the old integration. This was a stale
  integration run, not a regression in the apron implementation. Syncing all
  six exact manifest keys and asserting source equality corrected the harness;
  no production source change was required.
- ST18: 287/287 targeted cases pass, including all 135 new Stump cases, with zero
  C# errors. The refreshed ST14 source/art audit matches all 240 manifest files between
  the original and isolated project; all 116 new metadata GUIDs are collision
  free. The scoped integration patch was regenerated and reverse-checked.
- ST19 final native preview: all 15 post-apron images rendered with zero C# errors
  and zero missing source meshes. Independent inspection of all 15 finds no
  material visual regression; the final basin, shelter and coarse stone-art
  rules remain readable. The root also rechecked three representative images.
  ST20's later full-suite findings are recorded below.
- ST14's source/GUID evidence after the apron repair recorded:
  240 exact source/art matches and 116 new metadata files with zero collisions.
  The then-current scoped patch was reverse-checked. The untracked-path scan
  reports 431 entries, all within scope, with all pre-existing dirty entries
  still present. A further audit update for the seventh mixed file and final
  recipe correction was still pending at ST20; the later 257-file audit proves the final source match.
- ST20 full suite: 12,249 total, 12,212 pass, 37 failures and zero C# errors.
  Exactly five failures exceed the 32 recorded baseline cases. Three existing
  native-ground-graph checks report LockedChest without a recipe: seed1729 at
  Overworld.2.5.0, and seed729490642 at Overworld.2.5.0 and Overworld.4.5.0.
  Two older voxel-authority negatives use ordinary 2.5 and 4.5, now intentionally
  supported. Capture the pre-existing untracked test file before replacing only
  those two inputs with protected 2.4 and 3.3; retain the assertions.
- ST21 RED: 30 total, 26 pass, four LockedChest failures and zero C# errors.
  The three new all-18-address cases use seeds64,1729,729490642 and require a
  recipe for each visible owner without mutating the native graph. The fourth
  checks a real locked chest's owner, lock, removal and out-of-scope control.
  All four fail before production; the feature now contains 139 new tests.
- The production correction aliases only exact LockedChest to existing Chest
  art within ordinary Stump eligibility, preserving native LockPart and
  membership. The existing chest voxel chain was checked; no new art is needed
  and the alias leaves the kit at 52 models.
- ST22: 354 total, 348 pass, six failures and zero C# errors. LockedChest now
  passes. The remaining failures are IronKey mapping in the three existing
  native-ground-graph cases and the three new all-address coverage cases,
  previously hidden behind the first chest failure. Four coarse key variants
  are planned as a fourteenth family; RED tests precede their production build.
  At ST22 the key art had not been built; later gates below record its RED,
  completed build and final 257-file source/art audit.

## Installed files and reproduction

The active workspace contains the complete native integration. Seven files
already contained earlier work: six runtime integration files plus the existing
VoxelWorldIntegrationTests file. Their Stump-only additions/corrections are
recorded in `Docs/Verification/VoxelWorld/ST14-final/implementation.patch`, with
pre-edit and installed SHA256 values in `integration.json`. Their earlier
contents are not absorbed into this feature commit. The three tracked files
that were clean before this phase (HaulablePropBuilder, StumpFormationTests and
WorldMapTests) are committed normally alongside the owned new files/assets.

The patch is relative to those captured working versions, not a promise that a
bare checkout of this feature commit includes every earlier uncommitted voxel
dependency. Verify the before hashes before applying it to that matching
baseline; do not apply it again to the already-integrated workspace. The reverse
check and the original/isolate source hashes establish the installed delta.

Rebuild art with the editor method `StumpVoxelKitBuilder.Run`. The disposable
`StumpCompositionPreviewBatch.BuildAndRun` rebuilds and renders, while `.Run`
uses installed art. Set `STUMP_PREVIEW_NATIVE=1` and `STUMP_PREVIEW_OUT` to an
empty evidence directory in an isolated Unity process. Neither preview saves a
scene. The gallery compares the first native pass with the final verified pass.
Tests exercise the native manager, population, interactions and renderer; the
preview is not a separately authored demonstration world. Existing saved/native
chunks retain their state; the new composition applies when wilderness is
generated.

## Final key and owner-coverage gates

- ST23: 78 cases, 65 pass, 13 confirmed missing-key failures, zero C# errors.
  Asset checks pin an actual open ring, joined shaft and separated teeth,
  one-cell bounds, two colors, exact native alias and stable appended IDs.
- ST24 builds 56 models and renders 15 native chunks with zero C# errors or
  missing source meshes. Existing 52 models and metadata are unchanged; only
  four key variants and the library entries are added. Independent source and
  representative native-view review finds no material issue.
- ST25: 363/363 targeted checks pass, including all 148 new cases: 50 adversarial,
  12 composition, eight refinement, 11 rendering and 67 art-library cases.
- ST14 audit: 257 original/isolate file matches, 124 collision-free new GUIDs,
  current seven-file patch reverse-check and a 30-image comparison gallery.
- ST26 full regression: 12,262 total, 12,230 pass, 32 baseline failures, zero C# errors. All 148 new cases pass; the exact failure names and messages match ST00.

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
