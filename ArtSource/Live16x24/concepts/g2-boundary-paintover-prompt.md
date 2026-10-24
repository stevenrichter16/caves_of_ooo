# G2 boundary paintover — focused design input

Status: concept requested, not integrated art or acceptance. Built-in imagegen.
Inputs: `Docs/Verification/Live16x24/native-states-start.png` (actual Unity
capture; geometry remains the opening's verified live geometry) and
`ArtSource/Live16x24/base/opening-base-contact.png` (existing palette/style only).
R01 in `Docs/ART-REFERENCE-LIBRARY.md` informs quiet ground and clusters;
no external reference pixels are imported. Current delivery priority remains
G1 normal-launch continuity. This study supports the next bounded G2 family.

## Exact prompt

Use case: precise-object-edit
Asset type: art-direction paintover reference for an existing playable 2D pixel game, not a final screenshot or new game layout.
Input image 1 is the actual Unity Game capture. Preserve its entire layout, view scale, 940-pixel left world panel, right HUD and bottom skill bar. Input image 2 is an existing native pixel-art contact sheet; it is style and palette reference only.
Primary request: Improve ONLY the environmental material connections within the world panel. Make the existing low ruined masonry read as connected low stone walls with proper horizontal/vertical joins, corners and broken ends. Make the existing pond shoreline read through a restrained dark teal inset and sparse moss/bank pixels at its current perimeter. Make the existing cultivated soil strips read as worked earth through sparse few-pixel furrows and irregular edge ends.
Invariants: exact positions and footprints of every existing wall, gap, door, actor, prop, pond, crop and road remain. All existing open paths and ruins' entrances stay open. Keep all existing characters, their silhouettes, sizes and colors unchanged. Keep the right HUD, text, bottom skill bar, fog, resolution and composition unchanged. Do not invent any new actors, objects, structures, fire, flowers or services. The golden crop stems remain crops; rubble remains rubble.
Medium: very restrained flat orthographic 2D pixel art, 16x24 native cells, deliberate square pixels, no antialiasing, no isometric sides, no gradients, no bloom, no perspective, no painterly texture. Keep at least 80 percent of the quiet dark-green ground entirely plain. Use the existing subdued olive and blue-grey stone palette with at most three material values. Wall connections should be unmistakable even at this small scale; decorative texture must not compete with interactables.
Deliver one full screenshot paintover. This is only a reference that will be translated into editable native tiles, so favor coherent repeatable wall segments and small reusable shore/soil shapes over photographic detail.

## Result and extraction decision

Output: `g2-boundary-paintover-v1.png`. The generated scene is **rejected as
a layout or gameplay screenshot**. It promotes passable road bands around
the pond and garden into masonry, alters pixels/text/scale and introduces
soft shading. None of those changes should be imported.

Useful isolated motifs: broad connected stone courses with dark mortar;
one-pixel inset shoreline with intermittent bank clusters; sparse furrow
flecks kept within actual cultivated soil. The vertical ruin should connect
only where real wall owners exist. The east low wall's unsupported segments
must never bridge empty space or hide existing traversal gaps.

Next: extract only wall segments/corners/end caps, authored bank and furrow
references, then rebuild native 16×24 cells. Bind masonry to real `Wall`
owners with the exact opening VisualIDs after a fresh source sweep. Road
remains passable ground. Check neighbor topology against disclosed current
owners, never hidden state. Keep approved actor and prop pixels unchanged.
Do not count this concept as integrated graphics or G2 acceptance.
