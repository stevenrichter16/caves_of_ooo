# Seed Keeper — supplied 16×24 sprite

Status: imported, then corrected against the user's higher-resolution reference
(2026-10-18).

The user supplied `seed-keeper-16x24-source.zip` after requesting a recreation of
the simplified Seed Keeper. The archive already contains that artwork. Its
native PNG, previews, SVG, literal grid, palette metadata and generator are
preserved unchanged in Git commit `121c780f2`. The working files under
`ArtSource/SeedKeeper16x24/` now contain the requested reference corrections.
`reference-higher-resolution.jpg` preserves the newer user-supplied comparison.

## Import

`Assets/Art/SeedKeeper16x24/seed-keeper-16x24.png` is a byte-identical copy of the
current source PNG. Unity imports it as a single sprite with point filtering,
uncompressed texture data, no mipmaps, no non-power-of-two resizing, clamp wrap,
full-rect mesh and 16 pixels per unit. Its pivot is `(8, 1)` native pixels, at the
bottom of the boots above the transparent bottom margin.

This is a standalone art asset available in the Project browser. It is not wired
into the live Seed Keeper presentation; the previous flat Blender artwork remains
the gameplay asset described in `SEED-KEEPER-FLAT-ART.md`.

## Original import verification

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

## Higher-resolution reference corrections

The later reference is a more detailed rendition, not an exact integer upscale.
It has photographic texture and a different silhouette. Its structural details
take priority; its background noise and tiny rounded corners do not belong in
the native sprite. No automatic downsampling was used.

| Inconsistency | Correction at 16×24 |
| --- | --- |
| Broad rectangular upper cloak | Narrower collar and shoulders; coat widens lower down. Occupied bounds change from 14×22 to 13×22. |
| Broad rectangular right brim | Crown and brim descend to a lower right tip. |
| Red, cream and blue read as painted patches | Each pouch has a narrow neck and wider body; dark separation makes them read as hanging objects. |
| Missing olive divider between lower pouches | Restore a one-pixel olive strap between cream and blue. |
| Short, crowded cloak creases | Separate upper and lower vertical fold runs. |
| Flat hem and equal boot heights | Hem ends lower on the right; left boot begins one row earlier. |
| Boots much darker than the reference | Brighten boots toward the hat's warm ochre; use representative flat colors for the remaining palette. |

### Verification and self-review

- First check before regenerating: old PNG differed from the corrected source
  at 150 cells. Regenerated in Unity from the editable grid, then verified every
  pixel against the generator, grid, palette metadata and SVG.
- Final native asset: 16×24, eight opaque colors, 134 opaque pixels and 250
  transparent pixels. Alpha remains binary; no smoothing, mipmaps or compression.
- The two source previews remain exact 8× and 32× enlargements. Inspected the
  native-sized design enlarged with point sampling and the before/after sheet.
- Independent art review found the missing olive divider after the first pass;
  corrected column 10 on rows 15–17. No other material issues remained within
  the native resolution. Pouch corner rounding and fractional boot proportions
  remain deliberate low-resolution simplifications.
- Unity still reports RGBA32, Point, Uncompressed, 16 pixels per unit and pivot
  `(8, 1)`. The asset GUID is retained, preserving existing asset references.
- Gameplay code and scene routing are unchanged. This verifies the source and
  sprite import, not an in-game replacement or a new Steam Deck build.

Comparison: `Verification/SeedKeeper16x24/before-after.png` (old left, corrected
right). Review scope: art fidelity, source/export consistency and import settings.

## Editing

Edit the SVG in a vector editor, or change the supplied generator's `GRID` and
`PALETTE` and regenerate the source PNG using Pillow. Copy the updated native PNG
over the Unity asset, retaining its `.meta` file. Keep previews outside `Assets`
so they cannot accidentally replace the transparent native sprite.

Files: eight editable source/export files, the new reference, native Unity PNG,
before/after comparison and this document. No scene or gameplay character routing
was modified.
