# Opening masonry — optional native source

Status, 2026-10-24 UTC: **integrated and accepted within the exact opening
masonry family through native tests and bounded ordinary keyboard Play**.
The earlier offline-only approval is historical; the G2 scene and final
graphics release remain incomplete.
The governing owner, visibility and acceptance plan is
[GRAPHICS-G2-BOUNDARIES.md](../../../Docs/GRAPHICS-G2-BOUNDARIES.md).

The native atlas is256×72: three rows of16 frames, each16×24. Rows are low,
dark ruin and mineral-seamed wall. Column is the cardinal mask
`N=1, E=2, S=4, W=8`; index is `material*16+mask`. Unity bottom-left rect is
`(mask*16, (2-material)*24, 16, 24)`. Intended import is Texture2D, Point,
Clamp, no mipmaps, no compression; cached sprites use16PPU and center pivot.

Only exact Wall owners with `reference-glade-low-wall`,
`reference-glade-dark-ruin` or `reference-glade-lit-wall` are the scoped runtime users in the current ReferenceGlade.
GlowQuartzVein shares the last VisualID but **must remain a resource**, never
a masonry connector. RoadStone, rubble, beam, wicket and ordinary StoneWall
are excluded. The low worn crossing blocks walking: its Wall tag was removed
for sight, while its Solid identity remains. Art changes no collision, FOV,
harvest, lighting, damage or route layout.

Files:

- `opening-boundaries.blend`: editable flat integer-cell mesh geometry and
  three scenes, with named palette/material parts. No input raster tracing.
- `opening-boundaries.png`: final native atlas.
- `opening-boundaries-contact.png`: all48 frames, rendered directly at4×.
- `opening-boundaries-context.png`: source-layout illustration using the
  actual opening wall coordinates plus unchanged quartz, beam and open wicket
  source geometry. Other content/fog are omitted. **Not an in-game screenshot.**
- `source-cells.json`: full authored cell map and palette.
- `manifest.json`: frame identities, rects, masks, palettes and source layout.
- `source-red.json`: genuine missing-artifact failure before generation.
- `source-green.json`: final exact-pixel, alpha, palette, connectivity, port
  and contact checks. It is an offline validator, not a Unity test receipt.
- `*-pass1.*`: retained first-pass atlas/contact/check receipt; the visual
  review rejected its repeated wider centers despite numerical correctness.
- `visual-review.md`: historical offline passes, subsequent runtime review and
  remaining acceptance limits.

Reproduce from the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_opening_boundaries.py
python3 Tools/Live16x24/verify_opening_boundaries.py --receipt ArtSource/Live16x24/boundaries/source-green.json
```

The builder writes only this source directory; it does not import assets or
open/edit the game. The validator reads images without modifying them.
It checks the native output against the authored cells and the4× contact
against those same cells, including exact alpha/color at matching ports.
Current atlas SHA-256:
`7ad89c28fa6d9dcc3be83ed9935aa257f5d10b1d3ba41100f0dc18051a89dff2`.

Existing atlases and approved actor sprites were not modified. Runtime tests
must precede integration; native source checks cannot establish world
ownership, hidden-neighbor safety, actual movement, readable collision,
renderer restoration, frame cost or Deck feel.


## Runtime and ordinary Play checkpoint — 2026-10-24 UTC

The unchanged atlas is imported and selected by the optional boundary catalog.
Native [focused tests](../../../Docs/Verification/Live16x24/g2-boundaries-green.xml)
passed **60/60**, followed by a [bounded regression](../../../Docs/Verification/Live16x24/g2-boundaries-regression.xml)
with **552/552** passed, zero skipped. The 552 includes the 60 focused cases;
these counts are not additive. Tests cover exact owner and frame selection,
conservative fog, semantic fallback, submitted-neighbor repaint, destruction,
resource lifetime and the affected existing presentation controls.

Actual normal launch used seed 64 and isolated saves, with no forced
presentation override. Ordinary keyboard movement reached the northern wall
and western ruin. Native-size 1280×800 captures and matching JSON receipts are
in `Docs/Verification/Live16x24/`:

- [Normal start](../../../Docs/Verification/Live16x24/g2-normal-start.png):
  connected courses replace isolated repeated posts; existing actors are unchanged.
- [Wall examine](../../../Docs/Verification/Live16x24/g2-wall-examine.png):
  isolated stone portrait matches the wall; Stone and structure 39/40 are readable.
- [Wall destroyed](../../../Docs/Verification/Live16x24/g2-wall-destroyed.png):
  ordinary attacks remove the wall at (44,6), create factory Rubble and leave
  clean surviving end caps at (43,6) and (45,6), without a stone arm over the gap.
- [Ruin passage](../../../Docs/Verification/Live16x24/g2-ruin-passage.png) and
  [western ruin](../../../Docs/Verification/Live16x24/g2-western-ruin.png):
  y16 remains a clear traversable passage; visible dark masonry remains legible,
  and the beam/quartz retain separate identities. Fogged masonry is deliberately
  dim and conservatively capped, not a claim about remembered live topology.
- [Low crossing](../../../Docs/Verification/Live16x24/g2-low-crossing-confirmed.png):
  the worn crossing at (28,17) blocks the player at (28,16), and ordinary input
  logs a strike against the crossing wall. The small cap is an obstacle, not floor.

Independent visual review found no significant readability defect in these
matched opening and route captures. The source atlas remains byte-identical
to the hash above; approved actor and earlier atlas files were not changed.

**Can verify:** the recorded native assertions, real keyboard traversal,
wall examine/destruction and the crossing's blocking behavior, plus readability
at the captured scale. **Cannot verify from these checks:** Vault landing or
targeting (no Vault skill was granted), physical Steam Deck controls/performance,
all lighting situations or world regions, completed scene art, or a finished
packaged graphics release. This family is accepted within those bounds; the
remaining G2 scene work and release gates remain open.
