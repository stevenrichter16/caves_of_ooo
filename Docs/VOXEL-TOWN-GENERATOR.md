# Procedural voxel settlement toolkit

Status: Milestone1/all10 role profiles and the native rectangular adapter are
complete on codex/voxel-town-generator; the current pure suite passes280/280.
The separate four-chunk voxel renderer is installed and native-audited; its
implementation and receipts are in `Docs/VOXEL-WORLD-INTEGRATION.md`. This toolkit
produces reusable candidates and assets, preserving canonical native owners
rather than automatically replacing their layouts. Earlier art and unrelated
uncommitted work remain preserved.

## Goal and reference

Build the generator, not a fixed screenshot. The supplied reference shows an
inhabited oasis, teal wetland/reeds, warm stone open interiors, red/cream cloth,
weathered wood, functional clutter and sparse desert/ruins. Its arrangement and
labels are visual reference, not a literal Joppa map or imported game content.
This is CoO-original generation with no Qud-code parity claim.

## Milestones

1. Deterministic abstract town plus Blender realization: terrain, one body of
   water,10 buildings/4 archetypes, semantic districts/anchors, obstacle-aware
   paths, vegetation, functional props, population, orthographic camera/light.
2. Inspect gameplay camera. Record composition/scale/readability/repetition
   weaknesses; change generator rules and regenerate. Never move demo objects
   manually to conceal rule failures.
3. Verify alternative seeds, empty/extreme controls, larger settlements and
   safe regeneration. Export JSON semantic/voxel data and Blender source for
   later district editing and engine integration.
4. Extend roles/assets incrementally after the first actual visual gate. Full
   requested role set remains explicit; unsupported behavior fails clearly.

## Architecture / verified interfaces

Pure Python config/model/layout/districts/buildings/paths produce Town,
Building,Room,District,Path,WaterBody,FarmPlot records. Material/mesh/assets
modules use bpy only at realization. Root-owned terrain/vegetation/props/
population modules plan context-aware placements. A scene adapter realizes
these in namespaced GENERATED_* collections and writes source/geometry receipts.

The voxel grid is.25m. Integer voxel occupancy is separate from greedy merged
render surfaces; repeated asset meshes are linked. Building placement is organic
and semantic but remains quantized with quarter-turn orientation for later game
cell compatibility. A whole building never becomes the only logical object:
semantic rooms/entrances and asset/voxel records remain in the export.

Read before implementation: CLAUDE.md; user attached prompt and reference;
existing Blender5.2.1 executable/export pipeline; mesh-generation conventions.
Corrections: prior smooth/icosphere asset kits are not voxel art; do not reuse
those meshes. The Unity56-degree camera is untouched. This standalone demo
receives the near-overhead camera requested here. No engine integration is
implied by a Blender render or prefab existing.

## Verification and performance

TDD: first run missing modules/behavior RED; implement; counter-checks and
20–60-case adversarial sweeps for configuration, layout/paths and safe scene
ownership. Preserve failures. Seed controls must alter layout materially;
repeat seed must reproduce data and geometric fingerprints. Config controls
must affect their claimed output without consuming unrelated random streams.

Measure object/mesh/voxel counts and generation time; avoid cube-per-object.
Regeneration owns only its tagged objects/data, not collections merely sharing
names. Preserve manual objects, materials, camera/world state and unrelated
collections; audit idempotence and a manual-child counterexample in real bpy.

Blender viewport/camera renders verify appearance; abstract path tests verify
layout reachability. Neither proves native Unity combat/destruction/pathfinding.
No save migration work. Native integration and runtime performance are verified
separately in the four-chunk renderer phase, not inferred from these toolkit tests.

## Implementation log

- Created requested branch from current HEAD, preserving mixed worktree and
  recording baseline status in Docs/Verification/VoxelTown/prechange.json.
- Delegated bounded pure-layout and voxel-asset modules; root implements fields,
  dressing, population, scene realization, exports and integrated review.

## M1 implementation and visual iteration — 2026-09-11/12

The modular toolkit now generates the complete M1 scene. A first actual Blender
render was inspected before expanding the demonstration to all ten building
roles. The original four-role M1 and subsequent renders are retained in
`ArtSource/VoxelTown/Output`; the denser reusable oasis preset is the primary
example. Seeds41 and73 are generated independently from the same preset.

### Verification sweep corrections

| Assumption examined | Verified correction |
|---|---|
| External `bpy` may be needed | Installed Blender5.2.1 already supplies bpy and FBX exporter. |
| Building rotations might be degrees | Pure transforms use radians; scene/export preserve radians. |
| Center-only prop placement is adequate | Exact voxel reservations are required across props/NPCs/plants. |
| Any voxel size can be used with fixed room dimensions | Integrated milestone explicitly accepts .25m only; independent asset mesher supports other scales. |
| Any finite map size fits the common grid | Centered maps use .5m increments; independent terrain tiling still clips fractional final cells. |
| Collection names identify disposable content | Exact ownership and scene scope determine cleanup; names alone never grant ownership. |
| Shared FBX material warnings imply corruption | Actual face-by-face roundtrip retained every material; installed exporter warns on repeated shared-mesh registration. |
| Orthographic scale always means horizontal width | Framing must account for Blender's aperture convention and render aspect ratio. |

### Visual review and generator corrections

1. First M1: flat walls, large repeated ground patches, overly aligned district
   lots and sparse settlement canopies. Changed whole-stone coloring, caps and
   local relief; route/shore terrain now samples the quarter-metre grid.
2. Early masonry refinement: continuous deep course recesses read as stacked
   pallets. Replaced these with small staggered notches and intact stone courses.
   Kept the whole-block material variation and role-specific cloth/wood accents.
3. Layout: lots now seek distinct positions within their semantic districts,
   with an irregularity-controlled alignment penalty. No seed-specific offsets.
   Scarce farm/waterfront sites reserve space before flexible uses.
4. Environment: removed a repetitive diagonal water-highlight pattern; replaced
   broad dark central patches with restrained worn/sand/oasis/field materials.
   Added priority canopy around courtyard corners, farm edges and inhabited bank.
5. Agriculture and activity: short individual fence pieces describe farm
   boundaries, preserve a two-metre access strip, and reserve space before crops.
   Outdoor trader yards search usable apron/market space. One third of human
   residents prefer unobstructed route-side activity sites; homes and counts stay
   intact, with indoor fallback. Livestock remains in its enclosure.
6. Camera:63-degree elevation keeps interiors and characters readable. Projected
   functional bounds determine the frame for landscape or portrait output.

Every listed change modifies generator rules. No demonstration object was moved
by hand. These are real Blender renders; no image-generation substitute was used.

### Confirmed bugs fixed

- Furniture/furniture, NPC/NPC, NPC/furniture and crop/irrigation intersections.
- Rotated crowd overflow entering other buildings; occupants now use reserved
  usable interiors or clear outdoor sites.
- Radius clearance missing water and large path-bin queries.
- Fractional terrain tiles extending past configured bounds.
- Coarse-scale assets exceeding rooms; explicit integrated-scale rejection.
- Optional clutter claiming the primary well before its reservation.
- Low-wealth2.25m walls unable to support the required doorway and lintel.
- Waterfront bathhouse scheduled after flexible uses, exhausting a feasible site.
- Traders silently lacking outdoor activity space in dense layouts.
- Civic/shrine corner piers overlapping off-center doorways or interior props.
- Overlapping material repaint rejected by the voxel cell budget.
- Large finite coordinates overflowing the grid conversion.
- Livestock horn attached by only an edge; now face-connected.
- Manual children shifting after generated-parent deletion.
- Manual collection instances losing their borrowed source during regeneration.
- A pure planning rejection erasing the previous generated scene.
- Manual world/camera assignments being replaced during realization.
- Camera framing cutting off authored lots at alternate aspect ratios.
- Contradictory CLI artifact flags silently succeeding.

### Review and methodology classification

🟡 All confirmed notable findings above were reproduced and fixed. Regression
receipts preserve their RED→GREEN history. The initial environment/dressing draft
preceded its dedicated acceptance tests; this is explicitly methodology debt,
not a claim of pristine TDD. The independent hypothesis pass confirmed its faults
before fixes. Layout, asset/mesher, ownership, architecture, camera and later
refinements used failing tests before implementation/fix.

🔵 Source organization remains intentionally lightweight; some pure planner
helpers use compact formatting. This does not change the external data contract.

⚪ Current room plans are rectangular and single-room. Oasis geography remains a
western-water/eastern-desert family, with seed-dependent sites/routes/content.
At the M1 closeout, no native Unity voxel runtime was shipped by that wave.
The later four-chunk renderer is installed separately; district-only regeneration
remains future toolkit work, not a hidden manual step required to regenerate the
delivered examples. No Qud-code parity claim applies.

🧪 Render review verifies composition and model appearance at the provided camera.
It cannot establish native Unity collision, destructibility, combat, audio,
performance or play feel. No Unity scripts or scenes were changed by this wave;
the inherited game worktree and prior art remain preserved on the new branch.

### Performance and export evidence

The refined80m example has600 mesh instances using85 unique meshes,45,917 quads
and10 buildings. The independent FBX reimport preserves every object's geometry,
per-face base color, dimensions and semantic properties. Exactly13,390 warnings
match `(600-85)*26` repeated shared-mesh material registrations; they are accounted
for rather than treated as an engine-material failure.

The64m oasis examples generated in approximately1.3 seconds before rendering on
this machine; concurrent CPU previews took about56–79 seconds. Timings describe
Blender generation/rendering, not game frame time. The model keeps integer cell
occupancy separate from greedy batched render meshes and links repeated assets.
Individual fences/props/creatures remain separate placements in the interchange.

The larger pure gate exercises54 full towns, including50 buildings and200 human
residents, checking actual occupied-voxel intersections across shell, furniture,
population and vegetation. Separate layout/property gates exercised hundreds of
seed/configuration combinations. Full receipts, not extrapolated guarantees,
are retained under `Docs/Verification/VoxelTown`.

### M1 files and follow-up plan (historical)

New implementation lives entirely in `ArtSource/VoxelTown`: semantic model and
planners,53 procedural recipes,26-color palette, greedy mesher, semantic shell
builder, Blender adapter, camera/lighting, CLI/presets and pure/native tests.
`README.md` describes usage, parameters and JSON/FBX boundaries. Source reference
image, demonstration scenes and generation receipts are preserved.

The M1 follow-up plan identified richer multi-room floor plans and architectural
silhouettes, a second environmental anchor family such as a river crossing,
district-level regeneration, and a native voxel-rendering pilot. The native
rectangular adapter and separate four-chunk renderer have since shipped with
native movement/destruction/revisit checks. The richer generator features remain
future work. Existing save migrations remain out of scope per the user's instruction.


### M1 final gate and output-publication review (historical)

At M1 closeout,209 pure tests passed; the later adapter gate below brings the
current total to280. Seven native Blender scripts passed for framing,
integration/repeatability, manual camera/world preservation, saved exports,
independent regeneration hypotheses,20-case ownership taxonomy and baseline
ownership. The final oasis FBX additionally receives a separate real roundtrip.
The latest activity pass retains24 humans (8 outdoors) and2 enclosed livestock.
These are authored initial activity placements, not an implemented AI schedule.

The CLI now stages complete output bundles, then publishes their files and
writes `bundle.json` last with hashes. Planning/export/render/save rejection
preserves the previous bundle; ordinary partial-publication errors roll back.
Unrelated destination files and symlink targets survive. Retained old optional
artifacts are explicitly excluded from the new bundle. This is not a concurrent
writer or power-loss transaction.21 CLI tests cover these cases; the RED receipts
precede the fix. Raw FBX logs remain available, with duplicate shared-slot
warnings aggregated only after checking slot compatibility.

Actual larger Blender generation passed:120m,30 buildings,84 residents,
6 livestock,315 props,984 vegetation placements;1,502 visible objects share135
meshes and total229,162 triangles. Generation took9.06 seconds under concurrent
render/test load. No game-runtime performance claim follows from these figures.

Final example bundles: `Output/oasis-seed41`, `Output/oasis-seed73`, and
`Output/large-oasis-seed73`. Earlier M1/refinement images remain as review receipts.
Source and generated output are committed only within the new voxel scope;
unrelated pre-existing modifications are not staged.

### Native rectangular adapter — complete bounded milestone (12 September 2026)

This extension preserves the square toolkit and exports reusable asset geometry
plus deterministic 80×25 gameplay-cell candidate chunks. The four connected
roles are Morrowfast (3,6), western fields (2,6), Stump foothills (3,7), and
grove (4,7). Candidate layouts do not replace canonical native owners; Unity
integration selects compatible assets and content explicitly.

Pre-implementation verification corrected four assumptions: native chunks are
80×25, not square; local Y increases south whereas Blender Y points north;
current bootstrap starts Morrowfast despite the older Sill constant; and many
existing fixtures lack DestructiblePart. The adapter therefore declares axes,
native cell footprints and destruction intent, without pretending visual voxel
occupancy creates gameplay behavior. Game integration is documented separately.

Milestones: (1) neutral mesh/voxel/palette export with explicit uniform single-cell
fit scales; (2) semantic rectangular candidates with reciprocal portals and
cell-aligned accessible buildings; (3) adversarial multi-seed gates and native
Blender previews. Asset export is prioritized so runtime integration can reuse
the actual toolkit recipes. TDD receipts will live in Verification/VoxelTown.

Original CoO generation; no Qud implementation-parity claim is made. Native
source checks: Zone.cs, Village3DProjection.cs, ZoneTransitionSystem.cs,
SpatialFootprintPart.cs, BuilderSpawn.cs, Objects.json, and GameBootstrap.cs.

Implementation now separates `native_assets.py` (neutral real meshes/voxel runs),
`native_region.py` (integer-cell semantic candidates), `native_scene.py` (Blender
boundary), and `native_generate.py` (exports/previews). The old square APIs and
209 original tests remain intact. Crate/barrel families now have four actual
geometry variants each; the full library is59 recipes. Native mesh export
declares floor pivot, Blender/native axes, palette, bounds, full-size footprints
and honest uniform single-cell scales, including their reduced effective voxel
size. It does not infer Unity Parts from visual geometry.

Preliminary verification passed95 regions (80 normal seeds plus40×20,64×24,
96×32,160×50,256×128 envelopes). A greedy-lot dead end and undersized-envelope
overbooking were confirmed and fixed through deterministic retries and explicit
area-based lot counts. A slow sweep exposed unnecessary growing-set copies in
native flood-fill; those were removed before repeating the complete sweep.

Cold-eye hypotheses found six additional gaps: unknown mesh identities, claimed
footprints smaller than geometry, unchecked out-of-bounds route records,
incorrect external-world neighbors, missing walls removed from both semantic
and placement lists, and clutter scattered outside activity areas. Dedicated
RED receipts precede their fixes. Fractional anchors were already rejected and
are now explicitly type-checked. Trees/palms/rocks have solid candidate bodies;
reeds, scrub and crops remain passable. Every solid candidate reserves its whole
declared footprint and every architectural wall has a separate one-cell owner.

The first real Blender preview passed actual transformed mesh versus native
footprint bounds, complete camera framing, linked-mesh reuse, and preservation
of manually owned objects during repeated regeneration for all four chunks.
Visual review then identified checkerboard masonry, too-wide secondary roads,
repeated rectangular silhouettes, and sparse/uniform dressing. The general
rules now use restrained masonry recesses, narrow secondary footpaths,
role-specific building dimensions, an outdoor trader stall, contextual exterior
clutter, coherent vegetation patches and denser green shoreline bands. Final
post-refinement validation and refreshed previews passed.

Honesty bounds: pure and Blender scripts can verify deterministic geometry,
native-cell ownership plans, clear entrances, connectivity, model dimensions and
regeneration safety. They cannot prove Unity interaction/destruction, runtime
frame time, or aesthetic equivalence to the image reference. Those native
integration tests and profile receipts belong to the separate world-integration
phase. Candidate layouts are not silently installed over canonical owners.

Independent read-only review added four actionable findings, all resolved with
RED tests before fixes:

| Severity | Finding | Resolution |
|---|---|---|
| 🟡 | Bad preview data could erase the prior generated scene before failing | Pure asset, terrain, placement and real-bounds preflight precedes every Blender mutation; native Blender rejection tests preserve the prior scene |
| 🟡 | Imported material IDs could be displayed against a different palette | Preview explicitly rejects incompatible palette names/colors/order before mutation |
| 🟡 | A wall could claim ownership while its solid/opaque flags were false | Validator requires one-cell, solid, opaque, destructible wall owners |
| 🟡 | Flood-fill could start from a blocked or outside plaza | Plaza must be an integer, in-bounds, dry and unblocked cell |

The accompanying CLI failure probe found that requesting a render without bpy
could overwrite prior JSON exports first. Blender availability is now checked
before publication. Other failure, repeated-export, symlink-target and unrelated
file preservation hypotheses passed and remain regression pins. The native
command still promises per-file atomic writes, not a full multi-file transaction.

Final receipts (under `Docs/Verification/VoxelTown`):

- `native-23-final-suite.log`: **280/280 pure tests**; all209 original tests
  retained, plus71 new tests including35 dedicated adversarial cases.
- `native-region-24-final-multiseed.log`: **95/95 four-chunk regions**, comprising
  80 default-size seeds and15 varied-size cases, after the final composition rules.
- `native-blender-25-final-probe.log`: all four chunks pass actual mesh versus
  native footprint bounds, framing, linked geometry and regeneration preservation;
  malformed model/palette rejection leaves the previous preview intact.
- `native-blender-27-final-renders.log`: four final gameplay-camera images plus
  `native-region.blend`, rendered using two threads and16 samples in about37s.

Final candidate output contains10 buildings,20 initial human placements and662
total prop/wall/NPC/vegetation owners over8000 terrain cells. The source59-model
library exports independently in `Output/native-region/assets.json` with SHA256
`a7f350b3f54e4402e8500104aaa0f44732b79456515ea0077ab2a426e08ec7b2`.
The final refinement changed only the unused `voxel-wall` recipe relative to the
native audit's asset file: all19 unique recipes used by38 native bindings and the
palette stayed identical (`native-assets-26-final-provenance.log`).

Visual review confirms the specific composition improvements; these small native
candidates remain simpler than the square oasis showcase/reference, particularly
in architectural variation and ground microdetail. This is a documented visual
limit, not a claim that the reference aesthetic has been fully reproduced.
Further candidate embellishment must remain rule-based and preserve the native
cell/navigation gates. No Unity source was modified in this sub-milestone.

## Native coarse recipe profile (2026-09-12, toolkit complete)

The user requested fewer actual voxels per object after a paint-only reduction.
This opt-in native mesh profile retains the original fine toolkit and square/
rectangular candidate geometry. It targets the 19 unique recipes currently used
by 38 native bindings, with the same asset IDs and palette. Native import applies
one uniform scale to the existing owner bounds; it never stretches individual axes.

Verification sweep before implementation:

| Assumption | Verified correction |
|---|---|
| Increasing `build_recipes` voxel size reduces detail | False: recipes are authored in integer cells, so this preserves every cell and only enlarges metres; native fitting then cancels the change |
| Blind factor-two union preserves small silhouettes | False: it fills the open crate/tub, merges stool legs and can inflate negative-coordinate bounds by 0.5 m |
| Current native voxels are all 0.25 m | Source recipes are 0.25 m, but native uniform fitting produces approximately 0.073–0.231 m visible cells |
| Mixed recipe pitch can use the old global pitch claim | False: coarse export needs an explicit per-asset pitch policy; the original candidate preview must reject that import-only profile |

Implementation plan: independently resample source occupancy in physical space,
using a coarser lattice anchored at each recipe's lower bounds and deterministic
material votes. Preserve authored hollow container centres and essential furniture
features; keep the already minimal 15-cell stool as an explicit exception. Export
to `Output/native-region/assets-coarse.json` using an optional `native_coarse`
profile, recomputing all bounds, floor pivots, runs and uniform-fitting metadata.
The fine `assets.json`, original recipe API defaults and candidate plans remain
unchanged. RED tests precede implementation; cell-count reduction, silhouette
features, variants, deterministic exports and profile isolation are the gates.
This is CoO-original art tooling, not a Qud implementation-parity claim.

Implemented API:

```python
native_recipes(profile='native_coarse')
asset_manifest(profile='native_coarse')
export_assets('ArtSource/VoxelTown/Output/native-region/assets-coarse.json',
              profile='native_coarse')
```

The default `fine` profile is unchanged, including the exact previous JSON hash.
The new profile resamples physical cell centres with deterministic palette votes;
this actually reduces occupied cells. Containers use 1/3 m cells to retain a
three-cell rim within a one-metre horizontal envelope. Nature and beds generally
use 0.375 m cells. The low rock uses a single 0.5 m layer, and the already minimal stool
retains its 15 original 0.25 m cells and separate legs. All 59 original IDs survive;
only the 19 bound recipes participate. The remaining 40 recipes retain their fine
geometry. The coarse file explicitly declares `voxelSizePolicy:per-asset`, a null
global pitch and import-only usage; every mesh row has its real pitch. Existing
fine candidate preflight rejects it before any Blender mutation.

The implemented profile contains **584 versus 1428 cells (59.1% fewer)** across
those 19 recipes. Greedy render geometry contains 1260 versus 1748 triangles
(27.9% fewer); cell reduction and triangle reduction are different measurements.
Every changed recipe also produces larger visible cells *after* fitting uniformly
into the original owner envelope. The stool is the explicit unchanged exception.
No anisotropic transform, native owner change or candidate-layout change is used.

In-phase and cold-eye findings:

| Severity | Finding | Resolution |
|---|---|---|
| 🟡 | Frame voxels outvoted the bed's red blanket | First implementation failed its existing RED silhouette gate; authored visible mattress-top repair retains blanket and pillow |
| 🟡 | Resampling the 0.5 m-high rock into two 0.375 m layers caused native uniform fitting to cancel its pitch gain | Stronger after-fit countercheck went RED; a real single 0.5 m layer preserves a low silhouette and larger effective cells |
| 🔵 | Full-size coarse bounds differ, so silently applying the profile to candidate owners would invalidate footprints | Separate output, explicit import-only policy, recomputed bounds and an existing-preview rejection gate |
| 🧪 | Pure geometry gates cannot establish gameplay readability at the exact live camera | Actual Blender paired visual review completed; native in-game verification remains in the parent world-integration phase |

Verification receipts under `Docs/Verification/VoxelTown`:

- `coarse-01-red.log`: new profile unsupported before implementation.
- `coarse-02-blanket-red.log`: initial implementation still RED on lost blanket.
- `coarse-03-green.log`: initial 30 tests pass after blanket repair.
- `coarse-04-adversarial.log`: after-fit low-rock countercheck RED; one diagnostic
  test also corrected a mistaken preview helper name before its final run.
- `coarse-05-adversarial-green.log`: 35 coarse gates pass, including 19 individual
  recipe gates, source isolation, cavities, variants, materials, negative-grid
  translations, deterministic votes, output preservation and after-fit pitch.
- `coarse-06-full-suite.log`: **315/315 pure tests**, preserving all 280 prior cases.
- `coarse-07-export.log`: per-recipe counts, actual pitch, export/provenance hashes.
- `coarse-08-blender-comparison.log`: actual Blender validates 38 meshes and renders
  19 paired models, two threads/eight samples, in 32 s; process then exits.
- `coarse-09-reviewed-counters.log`: all 35 coarse cases still pass after the
  independent reviewer requested an explicit air-gap countercheck under the bed.

The paired image `Output/native-region/coarse-comparison.png` shows fine on the
left, coarse on the right at the same uniform owner envelopes. Visual inspection
confirms larger canopy blocks, fewer broader scrub branches, open container
centres, recognizable furniture and differing storage variants. The low rock is
intentionally one broader layer; the stool is unchanged. This is an asset-level
comparison, not a claim that live-camera gameplay or the reference town has been
fully revalidated here.

Final coarse export: `Output/native-region/assets-coarse.json`, SHA256
`36cc6b5db46cae463ddbe2bd54b9237bb7406fa6594daaeeb76ed1321909b49b`.
Fine export remains
`a7f350b3f54e4402e8500104aaa0f44732b79456515ea0077ab2a426e08ec7b2`.
Files changed: new `native_coarse.py`, optional-profile changes in
`native_assets.py`, new `test_native_coarse.py` and
`blender_native_coarse_preview.py`, new coarse JSON/image, README and this log.

Independent read-only review found no remaining production blocker. It identified
a test weakness: counting at least four bed ground cells would also accept a solid
plinth. The countercheck now explicitly requires air under the mattress and between
the two front supports. Current geometry already satisfied those gaps; no further
production or export change was necessary. The coarse export hash remains final.
