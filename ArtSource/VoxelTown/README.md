# Caves of Ooo — procedural voxel towns

A seed-driven Blender toolkit that builds semantic town data first and realizes
that data as voxel geometry. The supplied oasis image is an art-direction
reference; there are no seed-specific building coordinates or manually repaired
demo scenes. This is original CoO generation, not a recreation of Qud code.

The default milestone has ten buildings and four roles. The `oasis.json` preset
uses all ten supported roles in a denser 64m settlement. Change the seed to
regenerate buildings, routes, dressing and inhabitants together.

## Generate a town

Run from `/Users/steven/caves-of-ooo`:

```sh
/Applications/Blender.app/Contents/MacOS/Blender -b --threads 2 \
  --python-exit-code 1 \
  --python ArtSource/VoxelTown/town_generator/generate.py -- \
  --config ArtSource/VoxelTown/presets/oasis.json \
  --seed 41 --render --export-fbx \
  --output ArtSource/VoxelTown/Output/my-town
```

Blender 5.2.1 LTS is installed and includes `bpy`; no separate pip installation
is needed. `--python-exit-code 1` is important: Blender otherwise may report
success after a Python exception. `--samples 32` produces a quicker preview;
48 is the default. The script creates a separate generated scene and opens its
saved file in the orthographic gameplay camera. It does not modify Unity.

For abstract generation and inspection without Blender:

```sh
PYTHONPATH=ArtSource/VoxelTown python3 -m town_generator.generate \
  --config ArtSource/VoxelTown/presets/oasis.json --seed 73 \
  --data-only --output /tmp/coo-town-data
```

`--data-only` cannot be combined with `--render` or `--export-fbx`. You can also
supply `--town-size`, `--building-count`, `--population`, or `--all-archetypes`.
CLI values override the corresponding preset values.

## Controls

All amount/irregularity controls range from 0 to 1. A valid numeric configuration
can still be spatially infeasible; generation raises a specific error rather
than emitting intersecting buildings or silently dropping mandatory content.

| Control | Effect |
|---|---|
| `seed` | Reproducible layout and independent dressing/population streams |
| `town_size` | Square world extent, 32–512m in .5m increments |
| `building_count` | Requested lots, subject to available space |
| `population` | Human residents; enclosures additionally receive livestock |
| `density` | Lot separation and clustering pressure |
| `water_amount` | Oasis extent; zero removes the surface water body |
| `vegetation_amount` | Shore reeds, understory and activity-area canopy density |
| `agriculture_amount` | Attached farm extent; crops remain a deliberate farm pattern |
| `ruin_amount`, `town_age` | Remnant frequency around the ancient-site influence |
| `wealth` | Building height within usable entrance constraints |
| `clutter_amount` | Optional activity clutter; required furniture remains at zero |
| `building_irregularity` | Lot size, scatter and alignment variation |
| `path_irregularity` | Route variation while retaining clear connected destinations |
| `archetypes` | Supported building roles to cycle through |
| `voxel_size` | Integrated town scale is fixed at .25m for this milestone |

Archetypes: `residence`, `workshop`, `trader`, `meeting_house`, `shrine`,
`storehouse`, `farmhouse`, `bathhouse`, `animal_enclosure`, `guard_watch`.

## Semantic and geometry layers

`config.py` and `model.py` contain no Blender dependency. `layout.py`,
`districts.py`, `buildings.py` and `paths.py` produce Town, District, Building,
Room, Path, WaterBody and FarmPlot records. Streets connect real entrances,
markets, storage, water, farms and communal anchors. Priority placement reserves
farms and scarce waterfront lots before less constrained uses.

`terrain.py` supplies gradual environmental influence fields. `spatial.py`
provides width-aware path checks and exact voxel reservations. `props.py`,
`population.py` and `vegetation.py` independently derive compatible placements;
calling them in a different order does not consume shared randomness or rely on
mutable caller-populated lists. Crops respect irrigation and farm gates.

`assets.py` contains 53 reusable recipes, including all requested categories,
and three variants for repeated nature and character families. `materials.py`
defines 26 flat colors. `architecture.py` produces role-specific open shells,
real entrances/windows, voxel masonry and floors. `mesh.py` preserves occupied
cells while merging internal/coplanar render surfaces. A cube is not a Blender
object: repeated assets share meshes, and terrain is batched into patches.

`scene.py` realizes these records into `GENERATED_TERRAIN`,
`GENERATED_BUILDINGS`, `GENERATED_PROPS`, `GENERATED_VEGETATION`,
`GENERATED_WATER`, `GENERATED_NPCS`, and `GENERATED_LIGHTING`. The unlinked,
fake-user asset library survives save/reopen without appearing in the render.

## Regeneration and manual work

In Blender's Python console, add `ArtSource/VoxelTown` to `sys.path`, then:

```python
from town_generator.config import Config
from town_generator.layout import generate_town
from town_generator.scene import realize
scene, voxels, stats = realize(generate_town(Config(seed=73)))
```

To target an existing scene, pass it as `realize(town, scene)`. Exact ownership
properties, not collection names, determine what may be rebuilt. Manual objects,
manual nested collections, borrowed meshes/materials, parented manual children,
and manually authored collection instances survive cleanup. Manual camera/world
assignments remain intact. Pure planning validates before clearing the previous
generation. Scene realization is not a general transaction against an unexpected
Blender allocation failure; save important manual work normally.

## Outputs and game-engine boundary

- `town.blend`: editable scene, linked asset library, camera and lighting.
- `gameplay.png`: actual Blender render, when requested.
- `town.json`: abstract layout, config, districts, rooms, paths and home IDs.
- `town.voxels.json.gz`: lossless voxel assets, terrain, water, building structures
  and individual prop/vegetation/population placements.
- `town.fbx`: optional meshes with semantic custom properties; standard Y-up FBX.
- `stats.json`: counts, timings, current artifact flags and voxel fingerprint.
- `bundle.json`: current output file hashes, written after the complete bundle.
- `fbx-export.log`: full optional exporter output, including known duplicate warnings.

Generation writes into an owned staging directory. Planning, export, render or
save failure preserves the previous output bundle. Ordinary publication errors
roll back replaced files; unrelated destination files remain intact. Optional
outputs from earlier runs may remain, but `bundle.json` explicitly excludes them
from the current generation and lists their names. Consumers should verify its
hashes. This does not promise a concurrent-writer or power-loss transaction.

The JSON manifest uses metres, Blender X east / Y north / Z up, and **radians**
for rotations. Each voxel run is `[x,y,z,length,material_index]` with integer
voxel coordinates; it expands along +X. Structure runs are local to their
building transform. Terrain runs are in world coordinates. Asset runs are local
to individually identified placements. Scale is .25m, and palette indices are
provided in the same manifest. Air gaps and material assignments are preserved.

FBX was round-tripped through Blender and compared per world-space polygon,
material color, dimension and identity. Blender 5.2's exporter emits repeated
shared-mesh material registration warnings; the checked output retained all
colors and geometry. Preserve instancing rather than making every mesh unique
to silence that exporter behavior.

These exports are a foundation for Unity integration. They do **not** install a
voxel renderer, damage model, pathfinding system or physics simulation in the
game. A building's batched mesh is not the sole logical entity: the manifest
retains its cells and all placed objects for a later destructible implementation.

## Verification

```sh
PYTHONPATH=ArtSource/VoxelTown python3 -m unittest discover \
  -s ArtSource/VoxelTown/tests -p 'test_*.py'
```

Run all available native gates and keep per-gate receipts:

```sh
python3 ArtSource/VoxelTown/verify.py \
  --blender /Applications/Blender.app/Contents/MacOS/Blender \
  --fbx-example oasis-seed41
```

The final pure suite has209 tests. Actual Blender gates also verify ownership,
manual-scene state, repeated realization, framed lots, saved exports and FBX
roundtripping. The dedicated full-planner sweep checks54 varied towns.

Run the `blender_*.py` scripts with Blender and `--python-exit-code 1` for actual
ownership, manual-scene, framing and integration gates. The FBX roundtrip script
accepts an output folder name after `--`; that folder needs both a `.blend` and
FBX export. Receipts and visual comparisons live in
`Docs/Verification/VoxelTown`; implementation decisions and review findings are
in `Docs/VOXEL-TOWN-GENERATOR.md`.

## Current scope and next work

The toolkit has repeatable oasis settlements and all ten role profiles. Current
buildings use one rectangular room; the environment family has a predominantly
western oasis and eastern dry ground. Multi-room/irregular floor plans, alternate
river/cliff layouts, district-only regeneration and live voxel gameplay remain
future milestones. The reference's richer architectural wear, silhouettes and
activity storytelling remain art-direction targets. The generator, rather than
individual example scenes, should continue to acquire those rules.
