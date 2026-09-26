# Reference glade source kit

The authoritative original art recipe is `build_kit.py` → `kit.json`. Native
`ReferenceGladeVoxelKitBuilder.Run` reads this source and builds40 persistent
combined meshes/prefabs with a private24-color fog-aware palette. Run the16
source checks with `python3 -m unittest discover -s ArtSource/ReferenceGlade3D`.

`kit-review.blend` is an editable Blender review of exactly these2,467 terrain/prop cuboids;
`preview_kit.py --output <new-directory>` reproduces it and the gallery when run
inside Blender. It is not a substitute for the native gameplay render and does
not use the supplied reference image as geometry or a screen overlay. Model rows
are low textured ground, pale branching reeds, connected green tufts, fractured
dark ruins, gray low masonry, emerald wall edges, raised gravel, detailed chests
banded barrels and small mushroom rings, with four deterministic variants per family.

Native collision, light, loot, harvestable quartz and save mutations belong to
the actual zone entities. Meshes contain no colliders, light emitters or behaviours.
Terrain requires its saved VisualID/native-blueprint match and active glade
authority. The two plain native container types and MushroomRing have exact glade-only aliases.
Other world assets are unchanged. Existing original humanoid rigs are reused for
the local people; the presenter scales the actual adopted voxel rig, preserving
its animations, equipment children and picking bounds. Native gameplay bodies
and the original imported assets retain their dimensions.

The scene uses a profile on its own render-surface clones and sun. It does not
change global pipeline settings, gameplay light/FOV, borrowed camera/materials,
or other biomes. Low-detail still disables shadows. Source/contract tests cannot
prove visual similarity, readable lighting, native animation or performance.

See `Docs/DENSITY-REFERENCE-GLADE-ART.md` for source tests, measured import evidence,
third-material batching, counterchecks and the native visual acceptance status.

The importer also creates exactly eight persistent UV-only actor meshes from the
actual adopted native player, Sien/Nam and five original Marlback rigs. The local
palette paints the player green/pale, local people gray/pale and Marlbacks
rust/ochre. Bones, weights, bindposes, geometry and submeshes are copied unchanged;
head/hand bone ownership controls the humanoid accent. No global mesh, atlas or
ordinary-biome color is edited. The actor paint cache swaps immutable mesh
references after ordinary voxel adoption; it never copies vertices per frame.

The glade profile supplies a constant soft ambient probe to owned renderers at
bind, preserving their native visibility and other property-block fields. Real
sun/shadows remain enabled; low-detail shadows-off and ordinary scene probes are
unchanged. Third-pass native tests and visual acceptance are still required.
