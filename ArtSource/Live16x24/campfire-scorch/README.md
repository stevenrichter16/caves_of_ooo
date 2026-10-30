# Campfire scorch — original scorched-earth stain

Two original 16×24 frames in `campfire-scorch.png` (32×24) for the plain `CampfireGroundMarker` ("scorched earth") ring around ordinary settlement hearths. The frames are drawn with binary transparency over the real settlement ground.

- **Reference.** The actual Marrowstye captures, where the ring showed red `.` glyphs on black boxes, plus the approved town floor, lawn and sand that the stain lies on. No image-generation concept was available in this session, so no concept was used.
- **First draft rejected.** In the first contact sheet the compact dark oval read as a small object.
- **Final.** A wide stain only 6 rows tall (rows 14–19), ragged on every edge and mostly soft char, with three small deeper burns, a two-pixel subdued heat stain and four ash flecks. Frame 1 moves the flecks (8 pixels change). There is no rim, dome or flame; the campfire owns the fire.
- **Checks.** `--verify` checks every native pixel, binary alpha, one contiguous footprint and the bounds. `--inspect-blend` reopens 136 exact unit quads. Protected inputs are pinned.

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_campfire_scorch.py -- --build
python3 Tools/Live16x24/build_campfire_scorch.py --verify
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/campfire-scorch/editable.blend --threads 4 --python Tools/Live16x24/build_campfire_scorch.py -- --inspect-blend
```
