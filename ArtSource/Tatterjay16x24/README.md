# Tatterjay — user-supplied native sprite

Added to the flat study scene on 2026-10-18 from Downloads `tatterjay.png` and
`tatterjay-20261018-221815.piskel`. Both originals are preserved unchanged here.
The matching duplicate `tatterjay-1.png.png` has identical pixels. The older
`tatterjay-matched` export differs, so the explicitly named latest PNG is used.

Native art: 16×24 RGBA, 103 opaque pixels, six opaque colors, binary transparency.
The editable Piskel frame matches every PNG pixel. No resizing, recoloring or
image generation was applied.

Unity PNG: `Assets/Art/Tatterjay16x24/tatterjay.png`, byte-identical to the source.
Sprite Single, Point, Uncompressed, no mipmaps/NPOT resizing, full rectangle,
16 PPU, bottom-center pivot. `Tatterjay16x24.prefab` uses the existing flat-study
unlit material with white tint and no shadows.

The saved study places keeper, cook and Tatterjay in adjacent cells centered at
world X -1, 0 and 1, with shared cell baseline Y -0.75. All remain scale 1.
This is static art placement; gameplay Tatterjay behavior/model routing is separate.

Edit the Piskel and export its native PNG to iterate. Copy it over the Unity PNG
while retaining `.meta`, and update this source PNG and verification together.

`verification.json` records the source; `unity-import.json` records the import.
Actual Game capture and exact 8× source-composite proof are under
`Docs/Verification/SeedKeeper16x24/tatterjay-*` and `keeper-cook-tatterjay-game.png`.
