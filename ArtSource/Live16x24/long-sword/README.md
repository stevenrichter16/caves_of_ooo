# LongSword native source packet

Original static art for the **exact `LongSword`** owner. Source art is ready for
runtime review; this packet alone does not establish selection, reader,
custody, fog, gameplay or hardware acceptance. Runtime integration and ordinary
Play evidence are tracked in `../../../Docs/GRAPHICS-G3-LONG-SWORD.md`.

## Source and reduction

Root supplied an imagegen parts concept derived from the actual baseline and
accepted item contact. Its preserved copy is `concept.png` (generated file
`exec-7e071fbb-eaa8-4e0d-a63c-ada13c27dd1c.png`). The concept's printed
"actual size 16x24" and "8x" labels are not pixel measurements and were not
accepted as source geometry. See `critique.md` for the deliberate reduction.

The production image is a fresh Blender render of explicit integer cells:
one square mesh quad per authored native pixel, grouped into editable blade,
guard, leather grip and pommel objects. No concept raster was sampled, traced,
resized, cleaned, recolored or otherwise used to generate pixels. The parts
sheet is a direct render of subsets of the same geometry, not a raster cutout.

| Property | Source contract |
|---|---|
| Exact owner | `LongSword`, neutral static item |
| Resource | `Sprites/Live16x24/long-sword` |
| Native frame / rectangle | 16×24 / `[0, 0, 16, 24]` |
| Nontransparent bounds | `[2, 1, 14, 23]`, top-left coordinates; 12×22 |
| Palette / alpha | Six approved-kit colors; binary 0/255 |
| Occupied pixels | 66 |
| Import | 16 PPU, center pivot, Point, Clamp, no mipmaps, uncompressed |
| Meaning | Plain steel blade, simple guard, leather grip, metal pommel |
| Not represented | Magic, temper state, durability, held weapon, animation, other sword identities |

The six colors are copied as explicit color constants from the accepted item
palette: `#342E25`, `#5F4830`, `#3C5148`, `#526657`, `#7D8E71`, `#B5A47E`.
The green contact background is presentation-only and is transparent in the
runtime frame. The approved clay and tonic comparison cells are re-rendered
from their existing source geometry without modification.

## Editable files and checks

- `long-sword.blend`: native, direct 8× contact and parts scenes; named meshes.
- `source-cells.json`: all 384 cells and palette, with transparent cells explicit.
- `long-sword.png`: native transparent production source.
- `long-sword-contact.png`: direct 8× geometry beside accepted item cells.
- `long-sword-parts.png`: separated named geometry, using the same coordinates.
- `manifest.json`: exact resource, shape, palette and SHA256.
- `preserved-art-hashes.json`: 88 pre-existing PNG and PNG metadata files.
- `verify.py`, `source-red.txt`, `source-green.txt`, `verification.json`: source
  validation and genuine missing-artifact RED → rendered-artifact GREEN.
- `preservation-followup.json`: later independent detection of concurrent
  metadata-only drift; see the review below before treating the earlier
  preservation receipt as current.

Rebuild from the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_long_sword.py
python3 ArtSource/Live16x24/long-sword/verify.py
```

The verifier reads pixels with Pillow; it never edits images. It compares every
native cell, every relevant 8× contact/parts block, unchanged comparison art,
runtime/source byte equality, palette/alpha/bounds, fresh import metadata and
all 88 canonical committed file hashes. Blender backups are disabled for this generator.

## Review status

Source self-review and independent art review: no material blocker in native,
8× contact or parts images.
Blade/guard/grip are distinct, the blade remains the longest shape, and the
palette matches the approved kit. The independent check found 22 existing
PNG metadata files changed after the initial successful preservation check;
no preserved PNG changed. The exact follow-up and root's reconciliation status
are recorded in `critique.md`. These checks are source-art checks, not a claim
that a real game owner or inventory reader has displayed the sprite.

## Final reconciliation and integration

`preservation-reconciled.json` resolves the 22 metadata differences: the original pre-authoring snapshot already included Unity-added trailing whitespace. Root compared every line with committed metadata before restoring canonical bytes. All PNGs are unchanged. The original snapshot and historical GREEN/follow-up receipts remain; `preserved-art-git-hashes.json` is the final committed baseline used by `verify.py`.

Root inspected the native/contact and actual world, ground reader, equipped reader, reload and cellar-return reader. These bounded Play receipts and 391 native regression results are documented in `Docs/GRAPHICS-G3-LONG-SWORD.md`; no ordinary temper or full journey claim.
