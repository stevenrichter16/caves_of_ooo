# Sinkhole lip — original rim rock

Two original opaque 16×24 frames in `sinkhole-lip.png` (32×24) for the solid `SinkholeLip` ring around ordinary sinkhole mouths. The blueprint describes it as "undercut rock at the rim, wet with spray from below", where the green "grows thicker … and leans in".

**Design.** The block follows the approved straight, layered wall style:
- a moss cap (rows 0–2) with uneven drips;
- a grey-brown rock face with flat strata and a crack;
- a darker undercut band (rows 17–19);
- a deep undercut base (rows 20–23).

The two frames vary the drips, strata and cracks and alternate by (x+y)&1. The ring study (`scene-pattern.png`) shows them placed on the mouth's ellipse.

**Reference.** The offline sinkhole renders (`special-site-*-offline.png`), the approved wall and floor palettes, and the blueprint description. No image-generation concept was available in this session.

**Checks.** `--verify` checks every native pixel, the moss cap, the undercut base, distinct frames and four rejected corruptions. `--inspect-blend` reopens 768 exact unit quads. Protected inputs are pinned.

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_sinkhole_lip.py -- --build
python3 Tools/Live16x24/build_sinkhole_lip.py --verify
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/sinkhole-lip/editable.blend --threads 4 --python Tools/Live16x24/build_sinkhole_lip.py -- --inspect-blend
```
