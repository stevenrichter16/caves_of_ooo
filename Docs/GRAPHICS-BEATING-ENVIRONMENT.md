# Beating environmental polish: sand, road and ruined masonry

Planned after the Grovelands floor pass; no implementation or acceptance yet.

Root and a separate reviewer inspected actual `Verification/Live16x24/beating-final-04-ruin-field.png`. Regular diagonal marks cover broad sand and sandstone planes; solid brick remnants have weak depth beside walkable paving. The caravan-road capture also shows broad repetition. Compared with Overwrit's lore-sensitive empty center and Sill's repeated lawn, this offers a cohesive next improvement to ground and blocking architecture. The older green-road defect is already fixed; do not reopen it.

Start pixel-only: existing34-frame `Sprites/Live16x24/beating-materials`, change Sand0–3, SandstoneFloor12–15, SandstoneWall16–17 and RoadStone32–33 (12frames); preserve the22otherframes exactly, including dunes, salt and brine. Quiet irregular sand, worn flatter road/paving and layered shaded masonry should preserve straight cell boundaries, owner footprints, collision and destruction semantics. No new objects or machinery. Confirm frame mapping from the current catalog/source before authoring; a fresh actual baseline must justify any expanded scope.

Reuse `ArtSource/Live16x24/beating-materials/{source-cells.json,beating-materials.blend}` and `Tools/Live16x24/build_beating_materials.py` helpers without overwriting approved sources. Root owns Unity/Git. Healthy earned `Fixtures/beating-baseline-dune65`:19.14,turn2930,HP38/40; ordinary world-map travel two north reaches RuinField19.12. Reconfirm fresh state and use ordinary controls, no teleport/reveal. Freeze matched real views, generate one labeled imagegen material reference, critique parts, author original editable native cells, compare actual sand/ruin/road and blocker/reader behavior.

Relevant existing test class: `CavesOfOoo.Tests.FlatPixelBeatingMaterialsTests`. Pixel-only work needs source identity and current regression checks; changed selection/behavior requires focused RED first. Record known sparse composition separately. Full Beating, other sites, packaged execution and physical Deck stay open until actually checked.
