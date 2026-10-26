# East surface-water continuity: concept extraction

The built-in imagegen concept uses the actual `east-water-before-bank.png`
capture, with the existing `opening-ground.png` atlas as the material reference.
It is a paintover, **not gameplay evidence**. The input was ordinary Continue,
walking from the earned kitchen save to East (50,20), 30 HP, seed200893250.

Accept the quiet teal interior and muted broken bank pixels inside the existing
four-cell channel. Reject the output's resized viewport, softened ground,
altered foliage/text, brighter saturated bank and uniform all-side edging.
Actual visible neighbor state must determine which edges receive bank pixels.
The source graph and original water footprint must remain unchanged.

| Production part | Actual owner/state | Editable source and mapping |
| --- | --- | --- |
| Water interior and 16 bank combinations | Current admitted East cell with a sole live water coating; only visible disclosed dry cardinal neighbors establish banks | Reuse `Assets/Resources/Sprites/Live16x24/opening-ground.png` frames0–15 and `ArtSource/Live16x24/Ground/source-cells.json`, `opening-ground.blend`, `build_ground.py`. Each cell16×24, centered pivot, point sampled. No repaint or extra atlas. |
| Covered water | Same TileState underneath a real actor/item | Existing lower-ground selection and environment Tilemap, keeping the selected foreground owner intact. |
| Ice, mixed/unknown coatings, hidden and remembered cells | Existing world state | Existing presentation remains authoritative; no concept pixels override it. |

Production is an editable C# owner/state admission change, not a crop of the
concept. Do not apply water permission to East soil, roads or fields. Do not
change liquid stock, drink interactions, zone generation, collision or FOV.
