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

`assets.py` contains 59 reusable recipes, including all requested categories,
three variants for repeated nature/character families, and four geometrically
different crates and barrels. `materials.py`
defines 26 flat colors. `architecture.py` produces role-specific open shells,
real entrances/windows, voxel masonry and floors. `mesh.py` preserves occupied
cells while merging internal/coplanar render surfaces. A cube is not a Blender
object: repeated assets share meshes, and terrain is batched into patches.

`scene.py` realizes these records into `GENERATED_TERRAIN`,
`GENERATED_BUILDINGS`, `GENERATED_PROPS`, `GENERATED_VEGETATION`,
`GENERATED_WATER`, `GENERATED_NPCS`, and `GENERATED_LIGHTING`. The unlinked,
fake-user asset library survives save/reopen without appearing in the render.

## Rectangular native candidates and shared engine assets

The separate native adapter keeps the existing square planner intact:

```sh
PYTHONPATH=ArtSource/VoxelTown python3 -m town_generator.native_generate \
  --seed 41 --output ArtSource/VoxelTown/Output/native-region
```

Add `--render` when running the same script through Blender to create four
gameplay-camera previews and `native-region.blend`:

```sh
/Applications/Blender.app/Contents/MacOS/Blender -b --threads 2 \
  --python-exit-code 1 \
  --python ArtSource/VoxelTown/town_generator/native_generate.py -- \
  --seed 41 --render --samples 16 \
  --output ArtSource/VoxelTown/Output/native-region
```

`native_region.generate_region(NativeConfig(...))` produces 80×25 cell candidates
for Morrowfast (3,6), western fields (2,6), Stump foothills (3,7), and grove (4,7).
Every cell is one metre. `--width` and `--height` explore rectangular envelopes
from40×20 through256×128; the real Unity game remains80×25. Small envelopes
request fewer complete buildings, while all asset dimensions remain unchanged.

The region contains explicit terrain/interior cells, semantic buildings,
individually destructible wall-cell owners, furniture/NPC footprints, fields,
contextual clutter and reciprocal edge portals. Portal identity derives from
the shared world-zone pair and seed, so neighboring candidates agree without
independent edge-carving. Circulation and door openings are checked on the final
native integer grid. A deterministic retry stream handles greedy lot dead ends
without moving one demonstration by hand or silently omitting a requested role.

These are **candidate layouts**, marked `candidateOnly:true`. Running the
adapter does not replace authored Morrowfast, the existing Stump pilot, or any
Unity gameplay owner. A separate voxel renderer is now installed in those four
native chunks and binds compatible toolkit models to existing content, preserving
its authored owners and mechanics. See `Docs/VOXEL-WORLD-INTEGRATION.md` for the
native implementation and audit receipts.
Initial NPC role/home records are not an implemented AI work schedule.

`assets.json` exports the actual toolkit recipes as centred floor-pivot mesh
vertices, quads, triangles, per-face material indices, palette hex colors, and
lossless voxel runs. `voxelOrigin` converts the source integer run grid to the
centred mesh; odd-width recipes can require a half-voxel pivot offset. Axes are
Blender X east/Y north/Z up; Unity uses X east/Y height/Z north. Native logical
cell Y points south. Quarter turns in `region.json` rotate east toward south.

Each asset declares native bounds, full geometry footprints, and whether it fits
one cell at its original .25m voxel scale. Large assets expose one scalar
`singleCellScale` plus `[s,s,s]` as a uniform fit option; no axis is stretched.
That option reduces height and effective voxel size too, so a full-size well
should use multi-cell ownership when its stature matters. Full-size even-cell
footprints use `nativePlacementOffset`; single-cell fitted meshes do not. These
footprints include tree canopies and do not automatically imply trunk-only
collision. Native gameplay chooses the appropriate physical ownership policy.

The optional `native_coarse` export profile reduces actual voxel occupancy for
the 19 recipes used by 38 native bindings, while keeping all fine town assets and
candidate layouts unchanged:

```sh
PYTHONPATH=ArtSource/VoxelTown python3 - <<'PY'
from town_generator.native_assets import export_assets
export_assets('ArtSource/VoxelTown/Output/native-region/assets-coarse.json',
              profile='native_coarse')
PY
```

The 19 recipes contain 584 cells instead of 1428 (**59.1% fewer**) and 1260 triangles
instead of 1748. This changes geometry, not just paint or the scale of unchanged
cells. Containers use real 1/3 m cells; nature and beds generally use 0.375 m cells,
with a 0.5 m single-layer low rock. The minimal 15-cell stool retains its three
separate legs. Hollow crate/tub rims, blanket/pillow, tree canopy and distinct
storage silhouettes survive the reduction. Native import uniformly fits these
new meshes into existing owner bounds and keeps gameplay occupancy unchanged.

Coarse export declares `voxelSizePolicy:"per-asset"` and a null global pitch;
each mesh declares its actual pitch, runs, bounds and uniform scale. It is an
**import-only profile**: the fine candidate preview rejects it because coarse
full-size footprints can differ. The original `assets.json` remains byte-for-byte
unchanged. `Output/native-region/coarse-comparison.png` compares all 19 fine/coarse
pairs at the same uniform owner envelopes, with fine on the left. That image
records the geometry milestone before the following color reduction.

The coarse export now uses at most **two existing palette colors per model**.
Trees keep wood and foliage; beds keep wood and red fabric; containers retain
wood and metal where metal already existed, otherwise two wood tones. Fire and
water remain distinct accents. All 59 exported models meet the cap (58 use two
colors; the floor uses one). The rule changes only exported material IDs, after
geometry generation, so vertices, faces, voxel-run spans, pivots and dimensions
remain exact. Construction recipes retain their original semantic materials,
and the fine export remains unchanged. This limits base palette colors; ordinary
lighting still shades the surfaces.

`assets.json` and `region.json` each publish through an atomic file replacement.
Unrelated files survive. This native preview command does not claim the complete
multi-file transaction guarantees of the square toolkit's `bundle.json` CLI.

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

Running these export commands does not alter the Unity game. The separately
installed four-chunk renderer uses selected toolkit recipes and baked voxel
versions of existing models while retaining native destruction, pathfinding,
collision and interactions. Candidate layouts remain authoring data; a whole
building mesh does not become one blocking gameplay owner. This integration
does not introduce a new per-voxel damage or physics simulation.

## Verification

```sh
PYTHONPATH=ArtSource/VoxelTown python3 -m unittest discover \
  -s ArtSource/VoxelTown/tests -p 'test_*.py'
```

Run the square toolkit's Blender gates and keep per-gate receipts:

```sh
python3 ArtSource/VoxelTown/verify.py \
  --blender /Applications/Blender.app/Contents/MacOS/Blender \
  --fbx-example oasis-seed41
```

The current pure suite passes **383/383 tests**, retaining the original 209,
71 native-adapter/export cases, 35 coarse-profile gates and 68 palette gates.
Actual Blender gates verify ownership,
manual-scene state, repeated realization, framed lots, saved exports and FBX
roundtripping. The square full-planner sweep checks54 varied towns; the native
adapter sweep passes95 four-chunk regions across seeds and rectangular sizes.

Run the rectangular preview's actual Blender geometry, footprint, framing and
safe-regeneration gate separately:

```sh
/Applications/Blender.app/Contents/MacOS/Blender -b --threads 2 \
  --python-exit-code 1 \
  --python ArtSource/VoxelTown/tests/blender_native_region_probe.py
```

Run the `blender_*.py` scripts with Blender and `--python-exit-code 1` for actual
ownership, manual-scene, framing and integration gates. The FBX roundtrip script
accepts an output folder name after `--`; that folder needs both a `.blend` and
FBX export. Receipts and visual comparisons live in
`Docs/Verification/VoxelTown`; implementation decisions and review findings are
in `Docs/VOXEL-TOWN-GENERATOR.md`.

## Current scope and next work

The toolkit has repeatable oasis settlements, all ten role profiles, rectangular
native candidates and a reusable mesh export consumed by the installed
four-chunk renderer. Native gameplay retains its existing simulation owners;
the generated candidate towns are not automatically installed over them.
Current buildings use one rectangular room; the square environment family has a
predominantly western oasis and eastern dry ground. Multi-room/irregular floor
plans, alternate river/cliff layouts and district-only regeneration remain
future generator work. The reference's richer architectural wear, silhouettes
and activity storytelling remain art-direction targets. The generator, rather
than individual example scenes, should continue to acquire those rules.
