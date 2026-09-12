# Procedural voxel settlement toolkit

Status: Milestone1 complete and extended through all10 role profiles, on codex/voxel-town-generator. The user
replaced the non-voxel art direction and explicitly authorized autonomous
iterations. Earlier art and mixed uncommitted work remain preserved; the new
pipeline is isolated in ArtSource/VoxelTown. No existing Unity world is replaced.

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
No save migration work. Native integration and runtime performance gates follow
only when that work is actually introduced.

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
No district-only regeneration or native Unity voxel runtime is claimed. These
are future toolkit milestones, not hidden manual steps required to regenerate
the delivered examples. No Qud-code parity claim applies.

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

### Files and remaining milestones

New implementation lives entirely in `ArtSource/VoxelTown`: semantic model and
planners,53 procedural recipes,26-color palette, greedy mesher, semantic shell
builder, Blender adapter, camera/lighting, CLI/presets and pure/native tests.
`README.md` describes usage, parameters and JSON/FBX boundaries. Source reference
image, demonstration scenes and generation receipts are preserved.

Next bounded phases: richer multi-room floor plans and architectural silhouettes;
a second environmental anchor family such as a river crossing; district-level
regeneration with durable identities; then a Unity voxel-rendering/destruction
pilot with native pathfinding/displacement/damage tests. Existing save migrations
remain out of scope per the user's instruction.


### Final gate and output-publication review

Final suite:209 pure tests pass. Seven native Blender scripts pass for framing,
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
