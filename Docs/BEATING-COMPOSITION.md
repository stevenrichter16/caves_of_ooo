# The Beating: exposure and passage

Status: implemented, verified and installed, 2026-09-12. CoO-original spatial grammar and
voxel art; no Qud source parity claim. Authoritative lore: Lore/History/02_Geography,
03_History and Lore/Factions/04_SaccharineConcord. The lost leaf-light leaves raw
sun, the dry country exposes pre-Felling structures, and Tent-Right salt extraction
feeds the Concord's routes. Ruins show what was built without resolving its meaning.

## Spatial grammar

Keep the six shipped formations and their authored selection weights. Define
large surfaces, passages and activity areas before objects. Meaningful seed
variation changes wind orientation, offsets, ruin lots, water lenses and route
orientation, while neighbors share dry three-cell approach portals.

- Salt pan: unequal pale plates with quiet interiors, sparse crust seams and
  2–4 accessible native PaleSaltVein outcrops. Exposure is the dominant shape.
- Ruin field: unequal roofless rooms aligned around a broken older street;
  broad entrances, surviving corners and open courtyards. Each wall is a native
  independently destructible SandstoneWall; its Rubble also receives voxel art.
- Dune belt: three offset curved ridges with broad saddles and staggered breaks.
  Travelers can read a pass from the gameplay camera; no repeated fence texture.
- Caravan road: one well-to-well main line, east/west or north/south, widened at
  a passing place. Signs and occasional bones sit beside the route, never on it.
- Wind barrens: directional groups of rock with dry brush concentrated downwind;
  broad empty crossings between groups, rather than evenly sprinkled debris.
- Brine lens: unequal pools with pale crust margins and dry necks between them.
  Surface appearance belongs to the native BrinePool; no permanent replacement.

Ordinary wilderness only. Named places, all sinkhole mouths, the Tenth Fire
(2,19), and the two abandoned counters (19,18 and 19,19) keep existing pipelines.
Existing camps, hazard tables, populations, commerce and guest-rights continue.

## Verification sweep / corrected premises

| Assumption | Verified current contract / decision |
|---|---|
| Shadow grants shade | BeatingGlareSystem checks surface biome, Height band, player-only exposure, actual Cell.IsInterior or head equipment. Roofless ruin rooms are never flagged interior merely for appearance. |
| The arid pools stamp permanent water | BrinePool has renewable four-turn water source. Seed the native source and preserve its lifetime; existing water coating cures Parched even when source liquid is brine. Do not advertise drinking it. |
| Salt resources are destructible props | PaleSaltVein is solid Harvestable yielding PaleSalt1–2, consumed on harvest. Preserve the 2–4 outcrop supply and native trade loop. |
| All environmental objects break | SandstoneWall HP30/Hardness2 becomes Rubble, DryBrush HP4 and Saltbriar HP5 break. Dunes, crust, rocks, bones, signs and brine lack explicit Destructible; retain and document current semantics. |
| All empty biome addresses are ordinary | TenthFire has a deliberate bare pipeline and no veins. Abandoned counters require the zoneID argument for their forced stamps. Explicitly exclude all three from composition and its visual scope. |
| A crossing test proves exploration | Test every open cell, semantic room entries, all four boundary approaches after the full pipeline, and resource access after final solid placement. |

Read sources: BeatingFormationBuilder, FormationSelector, OverworldZoneManager,
WorldMapAuthoring, SinkholeSites, Objects.json's exact terrain/resource blueprints,
BeatingGlareSystem, TileStateSourcePart, native harvesting/destruction, and the
Sodden composition/art integration and tests. Existing W2 debt is not reopened.

## Readiness, art and milestones

Native content 🟢; native basic art mapping 🟡; extra actors/camp fixtures ⚪ keep
existing fallback. Author twelve families × four coarse variants: sand, pale pan
floor, packed road/floor, crust, dune, broken wall, salt vein, brine, bones, sign,
rubble, saltbriar. Reuse existing voxel rock/dry brush. At most two palette swatches
per object, one combined mesh, single-cell horizontal extents. Quiet ground and
water use constant visible tops; variation must not create checkerboards.

Steps: RED plan/art tests → native realization → integration RED/wiring →
adversarial actual mechanics and renderer checks → eighteen full native previews
and generator-level visual fixes → independent cold-eye → full regression against
SD11's 32 known failures → living docs and exact mixed-source delta commit.

## Performance and honesty bounds

Generation-only bounded masks and data structures. Prebuilt coarse meshes and
existing native dirty-patch batching; no runtime voxel authoring or per-voxel
GameObjects. Finite cached address membership and cached model IDs. Reserve
approaches/pools while leaving usable spaces for existing 8×6 camp stamps.
Measure zone generation separately from render/frame time. Preview pitch matches
gameplay, but whole-chunk overviews omit live player lightmaps and HUD.
No camera, spawn, reveal, saved-chunk rebuild or migration changes.

## Implementation log

- BT00 captures current mixed-file baselines after completed Sodden a2579879.
  The user continues playing; tests/renders use /tmp/coo-spread-validation and its
  healthy existing MCP server. Baseline SD11: 11,994 total, 11,962 pass, 32 recorded
  failures. No user session is restarted.

- BT01 confirms missing plan/builder types RED before native production. BT02
  confirms missing art library RED before authoring the48-model kit. The isolated
  offline kit build completed with zero C# compile errors.
- BT03:99 tests,86 pass/13 fail, zero compile errors. Twelve failures confirm
  absent manager/renderer integration; the remaining failure catches a real
  disconnected north/south painted road at seed38. Native camp-space checks,
  harvest/overflow, roofless glare, source lifetime, all-cell terrain reachability
  and art contracts passed. Exact receipts are under Verification/VoxelWorld.
- Installed ordinary-wilderness routing and read-only native-owner recipes in the
  six pre-existing mixed files; named places, sinkhole mouths and authored mystery
  sites stay on their existing pipelines. Pale pan appearance uses the canonical
  formation address, never a runtime reconstruction of generated terrain.
- Independent cold-eye review checked all six integration deltas. Added final
  pipeline salt count/access checks and actual presenter owner-removal / borrowed
  mesh controls. Road passing-place widening receives a separate RED gate. Visual
  review will explicitly inspect vertical room-wall readability.

- BT04:105 tests,103 pass/two intended road failures. Implemented connected
  polyline rasterization and an actual paved passing bay; BT05 passes all105 new
  feature cases. One older WorldMap routing fixture lacks newly required native
  keys; added six minimal fixture blueprints without weakening its routing check.
- BT06:18 actual native-pipeline renders, zero unmapped voxel source meshes.
  Camera review found clipped brine pools, narrow repeated dune crests and absent
  paving between ruin rooms. Nine intended failures in BT07 (180 tests,171 pass)
  now capture these weaknesses plus dune height/palette and low wall foundations.
- Generator refinement: brine basins are formed before three-cell-clear dry routes
  are searched around them; broad dunes taper at their ends and retain saddles;
  ruin rooms share native paved paths. Dune height is read from current visible
  cardinal neighbors so object removal also changes the surviving silhouette.
  The four art variants become flank/shoulder/crest/peak, sharing sand and muted
  warm stone. Low ruin foundations span both horizontal axes. No extra palette
  noise or independent voxel objects are introduced.

- BT08 refinement and neighboring systems:332/332 green, zero C# errors. This
  includes all116 Beating feature cases,96 intact-basin cases,144 real camp
  footprint checks, both caravan orientations and actual cross-patch dune removal.
  The WorldMap routing fixture now passes without changing its assertions.
- Independent cold-eye reviewed the refinement: bounded brine predecessor walk,
  native room paving, current-neighbor height calculation and renderer
  invalidation/fingerprints. No remaining material finding. Dry approaches may
  retain walkable SaltCrust; dry does not claim object-empty. A GUID audit found
  no collisions among107 new source/model/library metadata files (plus four existing Beating script metas also scanned).

- BT10 full regression before the final art seam pass:12,110 total,12,078 passed,
 32 failed, zero C# compile errors. All32 names exactly match SD11's recorded
 baseline; zero new failures. Source/assets matched the isolated project across
 214 checked files. The final dune seam simplification has its own RED and
 subsequent final verification; BT10 is not relabeled as the later-art result.

- BT12 last art-only seam gate:60 cases,56 pass/four expected failures. Two
  full-width slabs replace four tapered slabs per dune cell (48 rather than96
  vertices), retaining height grades and two swatches. BT13 rebuilt the kit and
  rendered18 native scenes; zero C# errors and zero unmapped voxel source meshes.
- Final independent cold-eye inspected dunes, pools, ruin streets and the exact
  six-file integration patch. No remaining material findings in those static
  views. Source/assets/metas match the isolated validation checkout across220
  scoped files;107 new GUIDs are collision-free.

## Self-review and limits

- 🟡 Fixed before close-out: north/south painted-road gaps, missing paved passing
  bay, sliced brine basins, disconnected ruin paving, fence-shaped dune bodies,
  bright repeated dune courses and narrow foundations. Every rule/art correction
  has a confirmed RED receipt followed by verification.
- 🔵 Native compatibility preserved: salt is consumed by harvesting and produces
  trade goods; ruined walls produce owned rubble; brine dries after its source
  disappears; roofless ruins do not grant invented shelter. Legacy camps still
  have real unreserved placement opportunities. Recorded W2 debts remain scoped
  out, and no historical world/special-site meaning is invented.
- 🧪 Can verify: generated content, traversal, resource access, liquid leases,
  ownership/removal, current-neighbor height changes, explicit model references,
  numeric shade controls and static camera composition. Cannot verify here:
  live player lightmaps, controller feel, HUD/fallback integration appearance or
  real frame-time profiling. No new measurable combat system was introduced.
- ⚪ Named places and authored mystery sites retain legacy layouts. Unsupported
  existing camp fixtures and actors retain their existing presentation fallback;
  zero unmapped voxel sources is not a claim that every native blueprint has new
  3D art. Existing saved chunks are not rebuilt and no migration work was added.

## Installed files and reproduction

New production: `BeatingCompositionPlan`, `BeatingCompositionBuilder`,
`BeatingVoxelLibrary`, offline `BeatingVoxelKitBuilder` and disposable
`BeatingCompositionPreviewBatch`. Four test fixtures include core, adversarial,
look-regression and model/renderer contracts. The existing WorldMap test fixture
adds six native blueprint names so missing fixture content cannot defeat routing.

Run `BeatingVoxelKitBuilder.Run` in an isolated Unity project to regenerate art.
Run `BeatingCompositionPreviewBatch.BuildAndRun` with `BEATING_PREVIEW_NATIVE=1`
and `BEATING_PREVIEW_OUT=<directory>` to reproduce all18 native views. Generation
rules produce each scene; no individual demonstration object is hand-positioned.

The six shared runtime files already contained substantial earlier work at BT00.
Their Beating changes are installed on disk, with the exact additive delta in
`Verification/VoxelWorld/BT11-final/implementation.patch`, before/installed hashes
and a reverse-apply check. The feature commit stages owned new sources/assets,
the six-line clean fixture addition, docs and receipts; it does not absorb the
unrelated mixed-file contents. Reapplying that patch to this workspace is not
needed. It records the changes relative to the captured BT00 state.

An interactive review gallery at `Verification/VoxelWorld/BT11-final/gallery.html`
provides36 views across the completed Sodden and Beating, six formations and three
seeds per country. The active user game, camera, spawn and reveal settings were
not reset during this work.

## Final verification

BT14 on the final source and final two-slab art: **12,114 total,12,082 passed,32
failed**, zero C# errors. All32 failed names exactly match SD11; zero new failures.
All120 Beating cases pass. The completed Sodden adds82 other passing cases in
this work sequence; together the two countries add72 models and202 checks.
The latest18 native Beating previews have zero unmapped voxel source meshes;
measured native generation is19.0–60.3ms (median22.7ms), not a frame-rate claim.
The commit includes living docs, confirmed RED receipts, compressed test XML,
final rendered views, exact shared-source delta and ownership/GUID audits.
