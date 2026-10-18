# Flat Seed Keeper source

Recreates the right-hand character in the user-supplied Seed Keeper reference as original planar pixel geometry: a straw hat, shaded face, olive coat, four seed pouches and brown boots.

- `seed-keeper.blend`: editable Blender source; every character vertex is coplanar. Orthographic camera and emission colors preserve the authored 2D shading.
- `seed-keeper-preview.png`: transparent render from Blender, not an in-game screenshot.
- `../../Tools/SeedKeeperFlat/build_blender.py`: reproducible authoring script. Run with Blender's `--background --python` arguments from the repository root.
- `../../Tools/SeedKeeperFlat/seed-keeper.json`: palette and occupied-pixel geometry exported for the Unity importer. Empty pixels have no faces.

The Blender file stays outside `Assets` so Unity does not invoke Blender implicitly during ordinary imports. In an idle Unity Editor, call `CavesOfOoo.Editor.SeedKeeperFlatBuilder.Build("Tools/SeedKeeperFlat/seed-keeper.json", "Docs/Verification/SeedKeeperFlat/import.json")` to regenerate the persistent runtime prefab, mesh and point-filtered palette.

This is a static flat pose. Native movement, visibility, picking, trading and Examine still belong to the existing NPC and rendering systems. See `Docs/SEED-KEEPER-FLAT-ART.md` for validation and actual game captures.
