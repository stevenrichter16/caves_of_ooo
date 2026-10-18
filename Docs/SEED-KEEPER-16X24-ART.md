# Seed Keeper — supplied 16×24 sprite

Status: source preserved and imported into Unity (2026-10-18).

The user supplied `seed-keeper-16x24-source.zip` after requesting a recreation of
the simplified Seed Keeper. The archive already contains that artwork. Its
native PNG, previews, SVG, literal grid, palette metadata and generator are
preserved unchanged under `ArtSource/SeedKeeper16x24/`.

## Import

`Assets/Art/SeedKeeper16x24/seed-keeper-16x24.png` is a byte-identical copy of the
supplied native PNG. Unity imports it as a single sprite with point filtering,
uncompressed texture data, no mipmaps, no non-power-of-two resizing, clamp wrap,
full-rect mesh and 16 pixels per unit. Its pivot is `(8, 1)` native pixels, at the
bottom of the boots above the transparent bottom margin.

This is a standalone art asset available in the Project browser. It is not wired
into the live Seed Keeper presentation; the previous flat Blender artwork remains
the gameplay asset described in `SEED-KEEPER-FLAT-ART.md`.

## Verification and review

- Compared every grid cell against the supplied photographic reference: same
  silhouette and color regions. Source colors differ from JPEG interiors by at
  most one value per RGB channel; the lossless source palette is authoritative.
- Native PNG: 16×24 RGBA, eight opaque colors, 151 opaque and 233 transparent
  pixels, alpha values only 0 and 255. Grid and metadata match every pixel.
- Preview PNGs: exact 8× and 32× nearest-neighbor enlargements on opaque teal.
  Visually inspected the supplied 32× preview. The native PNG supplies transparency.
- Initial Unity import used bilinear filtering; inspection caught this before
  completion. Reimport confirmed Point, Uncompressed, no mipmaps, full rectangle,
  16×24 dimensions, 16 pixels per unit and the expected pivot.
- No runtime code changed. No gameplay test suite was required for this asset
  import. These checks verify the source and import, not an in-game replacement.
- Self-review: no outstanding notable findings. Scope changed from drawing a
  recreation to using the exact source at the user's direction. No Qud source
  material or parity claim is involved.

## Editing

Edit the SVG in a vector editor, or change the supplied generator's `GRID` and
`PALETTE` and regenerate the source PNG using Pillow. Copy the updated native PNG
over the Unity asset, retaining its `.meta` file. Keep previews outside `Assets`
so they cannot accidentally replace the transparent native sprite.

Files: eight original source files; native Unity PNG and importer/folder metadata;
this document. No scene or existing character asset was modified.
