# Wayside cook — native 16×24 sprite

Created 2026-10-18 using Seed Keeper 3 as the style and scale reference.

The cook has a flour-pale cloth cap, olive tunic, long cream apron with a rust
repair patch, brown boots and a long wooden cooking spoon. The pale cap and
patched apron follow the existing `SpreadWaysideCook` examine description.
The olive colors match the keeper's palette. The spoon/hand and feet/body form
one continuous, hard-edged silhouette.

## Files

- `cook-16x24.png`: native transparent production sprite.
- `cook-16x24.piskel`: editable single-frame Piskel document with the exact PNG.
- `cook-16x24.svg`: editable native pixel rectangles.
- `sprite-grid.txt`, `palette.json`, `cook-pixels.json`: exact editable cells and colors.
- `cook-16x24-preview.png`: exact 32× nearest-neighbor preview on dark green.
- `verification.json`, `unity-import.json`: source and import checks.
- `imagegen-prompt.txt`, `imagegen-concept.png`: built-in imagegen concept/provenance.

The first generated concept introduced glow and excess detail. The final native
sprite was authored directly on a 16×24 grid and exported through Unity, following
the useful clothing/silhouette ideas while removing those rendering effects.
The concept is a reference only; the production PNG has no glow or antialiasing.

Edit the Piskel file and export its PNG when iterating. Update the Unity PNG while
retaining its `.meta` file. Keep the grid/JSON/SVG exports synchronized when making
changes; the current verification checks every cell against the native PNG.

Unity asset: `Assets/Art/Cook16x24/cook-16x24.png`, Point, uncompressed, no mipmaps,
no NPOT resizing, full rectangle, 16 PPU, bottom-center pivot. The prefab
`Assets/Art/Cook16x24/Cook16x24.prefab` shares the existing flat study's unlit
material, with white tint and no shadows. The source is one static frame.

This delivers art and a reusable prefab. The prefab is now placed beside the
keeper in the flat study scene. Live cook NPC model routing remains separate.
