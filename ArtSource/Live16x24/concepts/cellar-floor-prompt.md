# Cellar floor material study — 2026-10-25

Status: concept exploration only. The actual fresh seed64 entry capture from03:02UTC is the geometry/owner baseline. Nella d313eac64 does not change this graph or rendering. No new floor runtime or acceptance is implied.

Built-in imagegen. Input: `Docs/Verification/Live16x24/fresh-opening-cellar-entry.png`, the actual1280×800 game screenshot. Existing reference-library R01 supplies only the previously documented quiet-ground/readability principle; no external image is imported. Do not copy another game's assets.

## Exact generation prompt

Use case: precise-object-edit.
Asset type: production concept paintover of an actual flat 16x24 pixel roguelike screenshot, NOT final game evidence.
Input image1 is the exact current game capture. Change ONLY the visible walkable stone-floor pixels inside the existing cellar rooms and corridors. Keep the image size, camera, UI text and panels, hotbar, black margins, fog boundary, walls, player, enemy, sign, stair and brown cultivated soil exactly where and as they are. Do not reveal any fog-covered space.

Design a quiet worn green-gray stone floor for an abandoned working cellar. The floor should read as a subtly different material from outdoor dark green grass, while remaining visually subordinate to the yellow player and useful objects. Use a very dark desaturated teal base close to #182C29, with sparse short broken horizontal slab seams and a few restrained worn-corner clusters, only three flat dark colors. Think coarse original low-resolution pixel art built at16x24 cells with square pixels. Most of each floor cell stays unmarked. Leave ample negative space and vary the short marks so the route does not become a regular tiled checkerboard or brick carpet. No continuous bright outlines around tiles. Keep existing wall masonry unchanged and much stronger than the floor. Existing actors and props remain entirely untouched and readable above it.

Strictly flat top-down2D. No perspective, isometric edges, raised floor slabs, gradients, glow, shadows, volumetric lighting, painterly noise, new debris, cracks that resemble hazards, arrows, path highlighting, extra props, doors, crops or characters. Do not change walkability or replace brown crop beds. This is one restrained floor-material pass, not a redesigned dungeon or HUD.

## Result and critique

Output: `cellar-floor-v1.png` from built-in imagegen, exec3a4ba45d-ef36-4d92-90d0-3a5d04b3b15d,10:37UTC. Reference only. Select the sparse short broken slab seams and large dark unmarked areas. Reject its changed output size, modified HUD lettering (including Rain renamed Torment), softened lighting, darker/mottled fog, wall alterations, actor redraws and overly regular T-shaped seam repetition. None of the generated screenshot is imported as gameplay art. Native authoring must use the actual screenshot's existing wall/actor/UI pixels and only a deterministic sparse three-color floor family on real eligible Floor owners.
