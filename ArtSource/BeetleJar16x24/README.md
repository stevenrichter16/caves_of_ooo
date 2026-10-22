# Beetle jar — user-supplied 16×24 sprite

Imported 2026-10-21 from Downloads `beetle-jar.png` and
`beetle-jar-20261021-204220.piskel`. Both originals are preserved unchanged here.
The second PNG export has identical pixels and is not duplicated in this folder.

Native art: 16×24 RGBA, 176 opaque pixels, ten opaque colors, binary alpha.
The Piskel frame matches every PNG pixel. No resizing, recoloring, trimming or
image generation was applied. `verification.json` records the source checks.

## Game and study assets

- `Assets/Resources/Sprites/Environment/beetle_jar.png`: exact source PNG at the
  existing BeetleJar fixture resource path. Existing GUID and metadata retained.
  Single, Point, Uncompressed, no mipmaps/NPOT resizing, 16 PPU, centered pivot.
- `Assets/Art/BeetleJar16x24/beetle-jar.png`: exact source PNG for the flat study,
  same native import settings with a bottom-center pivot like the other figures.
- `Assets/Art/BeetleJar16x24/BeetleJar16x24.prefab`: shared unlit material,
  white tint, scale 1, no shadows. Placed as the fifth adjacent study cell.

The existing game's light/examine parts are reused. No gameplay blueprint, item
behavior or placement tables changed. The original native 3D model and its
Examine portrait remain available through their separate presentation path.
Normal 2D world lighting/glyph tint still applies; the flat study shows exact
source colors.

`previous-fixture-16x16.png` preserves the prior resource image. To restore that
2D art, copy it over the resource PNG while retaining its `.meta`. The complete
earlier 3D style is also preserved by the graphics checkpoint documented in
`Docs/PIXEL-16X24-STUDY.md`.

Edit the Piskel and export a native PNG to iterate. Update this source and both
Unity PNGs together, retaining Unity metadata. Keep all 16×24 cells; do not
stretch the sprite to its occupied bounds. `unity-import.json` records both imports.
