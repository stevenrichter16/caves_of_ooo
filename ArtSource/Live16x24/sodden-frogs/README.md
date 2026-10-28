# Ordinary Sodden frogs — two original native bodies

Source art ready for root-owned integration. This packet creates only Reedfrog
and Bandfrog. It does not replace the existing GinFrog or publish anything into
`Assets/`. Gameplay, fog, ownership, damage cues and release acceptance remain
separate gates.

| Export | Owner | Canvas | Occupied bounds, exclusive maximum | Baseline |
|---|---|---|---|---|
| `reedfrog.png` | `Reedfrog` | 16×24 | x1..15, y12..23 | y22 |
| `bandfrog.png` | `Bandfrog` | 16×24 | x1..15, y12..23 | y22 |

`sodden-frogs.png` is the same two frames in a32×24 atlas, ordered Reedfrog,
Bandfrog. Separate exports are pixel-identical to their atlas rectangles.
Use16PPU, centered pivot, FullRect, Point/Clamp, binary transparency, no mipmaps
or compression. No optional animation, fake shadow, poison cloud, wet coating,
held item or attack pose is baked into either frame.

The Reedfrog uses114 opaque pixels and the Bandfrog113. Each uses six clustered
colors; the combined palette has ten. Both have14×11 occupied silhouettes,
folded hind legs and four separated toe clusters. The first source review
identified merged horizontal feet that resembled boots; the final cells separate
those clusters and round the Reedfrog throat. This is a source-art refinement,
not evidence that game motion or combat was tested.

`source-cells.json` stores the exact named pixel cells; `palette.json` stores
their colors. `editable.blend` contains227 editable unit quads grouped by real
part/color, plus separate native export scenes and the comparison study. The
Blender geometry is planar and uses unlit emission colors. It is not a3D game
model or a raster image mapped onto a plane.

`contact-native.png` and `contact.png` are direct1×/8× Blender geometry studies,
not gameplay screenshots. The right-hand comparison is the existing protected
`Assets/Resources/Sprites/Environment/gin_frog.png`, exactly16×16 and centered
unscaled in a16×24 comparison cell. Its original palette is pale olive/cream;
it is not recolored to match the yellow glyph. Its visual baseline is higher
than the two new frames because its original square framing is preserved.

Reproduce from the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_sodden_frogs.py -- --build
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/sodden-frogs/editable.blend --threads 4 --python Tools/Live16x24/build_sodden_frogs.py -- --inspect-blend
python3 Tools/Live16x24/build_sodden_frogs.py --verify
```

Do not rerun `--snapshot`: that one-time operation pinned the actual baseline,
concept/prompt, approved GinFrog PNG/meta and four imported authoring helpers.
The initial source absence is recorded in `source-red.json`; it is not a native
Unity RED test. Verification checks every atlas/export pixel,74,880 direct-study
pixels, bounds, connected anatomy, baseline, distinct palettes and four deliberate
pixel corruptions. Saved-Blend inspection checks named quads, coordinates and
actual material emissions. No gameplay, performance or whole-region completion
claim follows from these checks.

See `PARTS.md` for concept interpretation and actual-play watch items.
