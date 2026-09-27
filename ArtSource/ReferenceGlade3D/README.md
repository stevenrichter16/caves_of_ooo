# Reference glade source kit

The authoritative original art recipe is `build_kit.py` → `kit.json`. Native
`ReferenceGladeVoxelKitBuilder.Run` reads this source and builds40 persistent
combined meshes/prefabs with a private24-color fog-aware palette. Run the 30
source checks with `python3 -m unittest discover -s ArtSource/ReferenceGlade3D`.

`kit-review.blend` matches the current seventh-pass JSON:40 models and2,391
cuboids. The reviewed gallery is archived under the seventh-pass verification
folder. Blender uses separate review lighting; Unity remains the gameplay authority.
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

The importer creates eight persistent scoped actor meshes. Five Marlbacks remain
strict UV-only copies of the adopted native geometry, with rust/ochre paint.
Exactly three glade humanoids (player and Sien/Nam) use original compact cuboid
body forms with a small pale head, shorter coat and separated legs. They retain
the exact source bindposes, bone hierarchy, clips, equipment sockets and rigid
bone ownership. The player remains green/pale and local people gray/pale. No
global source mesh, atlas, skeleton or ordinary-biome color is edited. The actor
cache swaps persistent mesh references after ordinary voxel adoption; it never
copies vertices per frame. The imported animation bounds are preserved and
expanded if needed around the authored bind-pose body.

The glade profile supplies a constant soft ambient probe to owned renderers at
bind, preserving their native visibility and other property-block fields. Real
sun/shadows remain enabled; low-detail shadows-off and ordinary scene probes are
unchanged. Third-pass native tests passed; the third image was rejected for reed/grass silhouette and actor scale. Fourth native tests passed; its image still needed broader ground variation and rectangular reed fingers. Fifth source tests pass; native pixel and full-scene visual acceptance remain required.

The fifth palette parameter defaults to zero globally. The active glade enables
static low-contrast ground variation only on its owned material clone, excluding
raised/vertical faces and transient owners. Native visibility and remembered-cell
brightness still determine what can be drawn. This is spatial albedo variation,
not a baked reference image, lighting bypass or occlusion claim.

The seventh source candidate changes only four green-grass recipes to broader,
lower tufts; the other36 models and palette are exact. Its native grass tests
first failed against the previous imported kit. Native builder adoption and all four grass geometry cases passed. The seventh
Blender review file/gallery were rebuilt and inspected; a new actual gameplay
screenshot is still required.
