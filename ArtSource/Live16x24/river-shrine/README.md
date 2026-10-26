# River shrine — native source packet

Status: source geometry and contact accepted for integration; runtime and actual
gameplay acceptance remain root-owned. `contact.png` is a direct Blender geometry
render, **not a screenshot from the game**.

The real before capture is
`Docs/Verification/Live16x24/current-scene-11-shrine-before.png`. The built-in
imagegen reference is `concepts/shrine-scene-reference.png`, with its original
prompt in `concepts/prompt.txt`. Both the actual game image and concept were
viewed before authoring. The concept's stone mass, recessed panel and small ochre
inlay were reconstructed as original named integer cells; no concept raster was
sampled, copied, resized or traced into the asset.

## Owner and visual contract

One neutral frame represents the real, non-solid, nonportable `RiverShrine` with
`SanctuaryPart`. The shrine uses a single world cell. Donations affect the actor,
so there is no donated/empty/full shrine variant. The fixed ochre detail is a
carved token/inlay, not a loose coin, inventory count, item pickup, glow or active
effect. No water contents or animation are depicted.

The native frame is 16×24 with 132 opaque pixels. Its connected stone silhouette
spans 14×13 pixels, ending at row 22 with transparent cell margins. The recess and
one 2×2 ochre token give a readable interior. Worn edges are short and uneven;
there is no all-around beveled lighting. The exact five-color palette is:

| Material | Hex |
| --- | --- |
| Recess | `233C32` |
| Dark stone | `3C5148` |
| Mid stone | `526657` |
| Worn edge | `8D9B78` |
| Fixed token | `C99538` |

## Editable production and review

`Tools/Live16x24/build_river_shrine.py` uses the established Blender integer-quad,
orthographic emission pipeline. `source-cells.json` preserves every editable
part/color cell. `editable.blend` contains named planar mesh groups and both the
native transparent scene and direct 8× contact scene on approved `032F23` ground.
The contact is rendered from the geometry rather than resized from a bitmap.

Native-size and enlarged inspection found a compact stone/recess identity with
one restrained warm detail. Root independently reviewed the contact and accepted
the low silhouette for integration. Geometry is frozen pending actual gameplay;
the source review does not certify world selection, interaction, lighting or
screen readability.

`verify.py` was run before outputs existed and failed as expected; the receipt is
`source-red.txt`. After Blender generation it passes exact native/source and
direct-contact pixel comparisons, palette, binary alpha, connected silhouette,
footprint, baseline and one fixed token. It reads PNG pixels only; it does not
author or edit raster images. `deterministic-rebuild.json` records the second
direct Blender build. Native and contact RGBA pixels are identical across builds;
the native PNG container bytes differ, while the contact PNG bytes match. Use the
current `manifest.json` hash for the runtime transfer. No previous approved source
or runtime asset was edited.

Run from the repository root:

```
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_river_shrine.py
python3 ArtSource/Live16x24/river-shrine/verify.py
```

Transfer `ArtSource/Live16x24/river-shrine/native.png` unchanged to the root-owned
`Assets/Resources/Sprites/Live16x24/river-shrine.png`. Import at 16 PPU, centered
pivot, Point filtering, Clamp wrapping, no mipmaps or compression, with binary
alpha. Root owns that PNG and its Unity metadata, runtime resolver, tests, actual
game captures and final acceptance. The generator intentionally does not write
anything under `Assets`.
