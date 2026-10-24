# Sparse working-court stones

Three original static 16×24 ground cells for the kitchen working court, packed left to right in one 48×24 opaque atlas. Each has two or three broad worn stone pieces separated by conspicuous green joints. All accepted character, object and ground art remains unchanged.

The viewed `../concepts/working-court-v1.png` informed the court's grouping only. Its full paving, soft shading and HUD alterations were rejected. No image was sampled, traced, cropped or resized to produce this art: every source pixel is a flat integer-coordinate Blender quad with a named part and palette material.

## Source and rebuild

- `working-court.blend`: editable native atlas, direct 8× contact and direct 4× repetition scenes.
- `source-cells.json`: top-left-origin part/material assignment for every native pixel.
- `manifest.json`: palette, frame rectangles, coverage and resource hash.
- `working-court-contact.png`: direct 8× geometry render, 768×320 including labels outside the frames.
- `working-court-repeat.png`: direct 4× geometry render of twelve coordinate variants, 256×288.
- Runtime resource: `Assets/Resources/Sprites/Live16x24/working-court.png` and its stable-GUID `.meta`.

From the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_working_court.py
python3 ArtSource/Live16x24/working-court/verify.py
```

The generator writes only this source directory and the owned resource. It preserves existing normalized resource metadata. Blender scene geometry is the image source; the verification script uses Pillow only for read-only pixel comparisons.

## Runtime contract

All three frames belong to actual `StoneFloor` owners with exact role `court-floor`, displayed as “worn stepping stones.” Select a stable coordinate variant; these are not animation frames or different gameplay states. They do not promise full paving, a road, a special action, heat or a maintained safe zone. No `Barren` behavior is introduced: visible grass joints reflect the real underlying plantable Grass retained by the court generation pass.

| Index | ID | Rectangle x,y,w,h | Stone pixels | Green pixels | Part grouping |
| --- | --- | --- | --- | --- | --- |
| 0 | broad-pair | 0,0,16,24 | 148 | 236 | Upper broad stone, smaller lower stone, mineral/wear patches |
| 1 | offset-pair | 16,0,16,24 | 131 | 253 | Longer left stone, offset lower stone, mineral/wear patches |
| 2 | broken-three | 32,0,16,24 | 134 | 250 | Broad upper stone, two lower pieces, mineral/wear patches |

Palette: grass `#032F23`, stone olive `#344A40`, stone grey `#425548`, worn surface `#4A5B4D`. Alpha is 255 at every native pixel. There are no gradients, cast shadows, outlines or directional lighting. Import uses Point filtering, Clamp wrapping, 16 pixels per unit, no mipmaps, no compression and no NPOT resize. Unity normalized `alphaIsTransparency` to 1 during import; this is harmless for this fully opaque image and the normalized metadata is preserved.

## Bounded review and evidence

`source-red.txt` records the verifier failing before creation. `source-green.txt` and `verification.json` record the completed source checks: exact native dimensions and four-color palette; every source pixel matches native and direct 8× geometry; direct 4× repetition matches the atlas; disconnected broad stones and green margins; byte-identical source/resource PNG; metadata contract; and SHA-256 preservation of 18 pre-existing art PNGs listed in `preserved-art-hashes.json`.

The direct contact and tiled geometry previews were visually inspected. The broad pieces read as quiet worn stones; open green areas occupy 61–66% of each tile. Repetition is visible in the deliberately dense contact grid, while three offset shapes avoid identical pairs in every tile. Actual court spacing and coordinate selection must be assessed in game; these previews do not prove ground priority, crop behavior, visibility, input, traversal or Unity runtime appearance. Root owns catalog integration, gameplay tests and actual screenshots. This is a bounded source-art acceptance, not a release-quality claim.
