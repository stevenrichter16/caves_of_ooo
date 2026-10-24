# G2 boundary parts extraction

Built-in imagegen; reference only. Inputs: `g2-boundary-paintover-v1.png`
(material motifs only; its invented road walls are rejected) and
`../base/opening-base-contact.png` (approved existing palette/style).

## Exact prompt

Use case: background-extraction
Asset type: isolated environmental parts design reference, to be manually reconstructed as native16x24 game tiles.
Input image1: a concept paintover of our game. Image2: our existing native art palette and simplicity.
Extract and simplify ONLY material motifs, not layout. Produce a clean sheet of twelve isolated flat pixel-art references arranged4columns by3rows on transparent background, each separated by generous empty space. Every motif is a single-cell tile design viewed flat top-down, square source pixels and severe 16x24-equivalent simplification. No text, labels or UI.
Row1: low olive-grey masonry horizontal connector; low masonry vertical connector; Lcorner; brokenend. These must read as continuous low stone courses, not fence posts.
Row2: darker blue-green ruin horizontal connector; dark ruin vertical connector; Lcorner; brokenend. Two or three large stone masses, sparse dark mortar, little moss, no perspective sides.
Row3: restrained teal pond-bank edge with olive mud; matching outer bank corner; passable flat worn stone path segment; cultivated dark-ochre earth with2sparse furrow marks.
Do not put raised blocks on the passable path. Do not include water-source vessels, crops, actors, doors or new props. Draw no background scenery, floor carpet, shadows or glow. Keep shapes simple enough to reconstruct on an exact16x24 pixel grid, using muted3-value material palettes from image2. The sprite footprint must stay inside its cell. This is reference art only, not an implemented scene; the native version will connect to actual wall and ground entities.

## Extraction critique

Output: `g2-boundary-parts-v1.png`. The broad stones, dark mortar and broken
ends provide useful shape references. This is not a native atlas: generated
cells have irregular pixel scales, soft shading and oversized shapes. Rebuild
the connections on the exact16×24 grid. Native palette remains the existing
base palette, not sampled generated colors. The bottom-right design resembles
standing wheat rather than two soil furrows; reject those yellow stems. Keep
soil detail lower contrast than actual crop sprites. The path has excessive
rubble texture; simplify it and never turn it into solid stone wall art.

Ownership mapping: low/dark/lit masonry belongs only to exact `Wall` owners
with their opening VisualIDs; quartz remains its separate harvest resource.
Banks follow current actual water-ground representation and disclosed edges;
paths require `RoadStone`; furrows require real cultivated/crop-row ground.
Full source/state design will live in `Docs/GRAPHICS-G2-BOUNDARIES.md` before
production. No generated image is directly imported as a game sprite.
