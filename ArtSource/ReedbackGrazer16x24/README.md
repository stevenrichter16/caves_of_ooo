# Reedback Grazer — selected 03-ridge sprite

Added to the flat study on 2026-10-18 from Downloads
`grazer-editable-source/03-ridge.png`. The PNG, literal grid, SVG and palette are
preserved unchanged. This is the user's selected ridge iteration of the grazer.

Native art: 16×24 RGBA, 111 opaque pixels, six opaque colors, binary alpha.
Every grid cell and SVG rectangle matches the PNG. The low, broad 16×12 occupied
silhouette retains its original transparent margins; no stretching or trimming.

Unity: `Assets/Art/ReedbackGrazer16x24/03-ridge.png`, byte-identical to the source.
Single sprite, Point, Uncompressed, no mipmaps/NPOT resizing, full rectangle,
16 PPU and bottom-center pivot. The reusable `ReedbackGrazer16x24.prefab` shares
the existing unlit material with white tint and no shadows.

The study now shows keeper, cook, Tatterjay and grazer in four adjacent 1×1.5
cells. The Grid shifts to center the group; all sprites retain scale 1 and the
same canvas baseline. This is static art placement, without gameplay NPC simulation.

Edit the native SVG/grid and export a 16×24 PNG when iterating; retain the Unity
PNG's `.meta` file. Keep the source PNG, grid, SVG and palette synchronized.

Source and import checks: `verification.json` and `unity-import.json`.
Actual Game frame: `Docs/Verification/SeedKeeper16x24/keeper-cook-tatterjay-grazer-game.png`.
Exact 8× source-composite proof: `Docs/Verification/SeedKeeper16x24/grazer-capture.json`.
