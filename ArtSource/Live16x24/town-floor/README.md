# Town floor — packed settlement earth

Four original opaque 16×24 cells in a new 64×24 `town-floor.png`, for the plain `Floor` owner of ordinary authored settlements. That owner was about 8,400 seed-64 cells across Tine, Sumphold, the Drowned Ledger, the Last Counter, Marrowstye, Quillhold and Tally. It is a separate resource: no approved atlas was copied or repainted.

## Reference and direction

The direction came from three sources:
- the actual offline baselines (`settlement-water-walls-tine-offline.png`, `-last-counter-`), where Floor drew as a black field of dark-grey `.` glyphs;
- the approved neighbouring materials: Sill lawn `344639`, Sill room floor `605E53`, Grovelands soil `3A3C2E` and Beating sand `735735`;
- the house rule: quiet, flat floors.

The material is packed town earth, `48433A`. It is darker and warmer than the room floor, so interiors stay distinct, and it is never green, so it does not read as lawn.
- Frame 0 is the exact quiet base.
- Frame 1 is one broad, flat, interrupted foot-worn field: no rim, raised centre or outline.
- Frame 2 has four single grains and two one-pixel grain shadows, never a cluster that could read as an item.
- Frame 3 has two short, low-contrast lateral scuffs.

All border pixels are the base, and each frame keeps at least 330 of 384 base pixels. The variant selector is the shared quiet-majority rule (about 75% frame 0).

**No imagegen concept.** None was produced for this material: no image-generation tool was available in this session. The cells were authored directly as named unit quads.

## Editable source and checks

`Tools/Live16x24/build_town_floor.py` follows the approved Grovelands builder. It reuses the existing named unit-quad model, camera, instance and timing-canonicalisation helpers, and writes only this folder.
- `editable.blend` holds the original named pixel geometry and emission colours.
- `--verify` checks the following:
  - all 1,536 native pixels;
  - opacity, the exact quiet frame 0 and common borders;
  - every contact and pattern pixel;
  - that four corruptions are rejected.
- `--inspect-blend` reopens the saved blend: 1,536 exact unit quads, 9 named groups, no animation.
- Protected inputs (approved atlases, helpers and the two baselines) are pinned in `protected-inputs.json`, and every build re-checks them.
- `source-red.txt` records the genuine missing-source failure before the first build.
- Only one build was made, so rebuild byte-identity is not claimed.

## Reproduce

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_town_floor.py -- --build
python3 Tools/Live16x24/build_town_floor.py --verify
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/town-floor/editable.blend --threads 4 --python Tools/Live16x24/build_town_floor.py -- --inspect-blend
```

Import: centred 16 PPU, Point/Clamp, no mipmaps or compression; the `.meta` follows `grovelands-ground.png.meta` with its own GUID.
