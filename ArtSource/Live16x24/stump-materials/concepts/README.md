# Stump concept pass

These generated images are **concept references only**, not actual gameplay or
production sprites. Actual before captures live under
`Docs/Verification/Live16x24/stump-before-*.png`.

Both prompts were run with the built-in imagegen tool on 2026-10-27. Version 1
used all five actual formation captures; version 2 used version 1 plus the actual
RimForest capture. Exact prompts are adjacent. External morphology references
and their limited use are recorded in `../references.json`.

Version 1 is rejected for extrusion, masonry-like ridges, cactus-like vegetation,
noise and changed layout. Version 2 helps establish quieter ground and contained
tank water, but its changed positions, plant counts, HUD numbers/text and player
are rejected. The HUD health in the concept is **not a gameplay observation**;
the real baseline player remained at 38/40 HP.

Extract only the palette and restrained material distinctions described in
`../parts-and-decisions.json`. Native sprites are separately authored as editable
16×24 cells. No crops or downscaled concept pixels may become the atlas.
