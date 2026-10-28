# Quartzite9–10 — scene reference and bounded parts critique

**Current pass: AI material reference only; root review pending. No native art, atlas, runtime, tests or gameplay acceptance is supplied by this packet.** The built-in imagegen tool created `scene-reference-01.png` from two actual captures, after both were viewed. The exact prompt is `scene-prompt-01.txt`; input/output hashes and the original generated-image path are in `scene-reference-01.json`. The output was copied without pixel edits. It is not an actual game screenshot.

## Observed inputs and source corrections

`quartzite-caves-before-07-entry10.png` is the primary composition target. It visibly shows a disclosed branching area with the player at35,17,HP23/40,Charred and an Up stair in Focus. `quartzite-caves-before-06-down9-save.png` is supporting context: a more horizontal disclosed corridor and Down stair at the same focus coordinates/health. These descriptions concern the visible captures only; they do not certify generation provenance, a complete zone census, safe routes or area fullness. The preflight's earlier “unobserved” status is historical relative to these new root captures; this packet does not edit that living phase file.

Read current `Objects.json` definitions: QuartziteWall at8643 inherits Wall, uses# / &C / ^c and has55/55 structure, hardness5 and Rubble wreckage. Its authored description calls for small bright grains in a hard uneven face. QuartziteFloor at8913 inherits Terrain, uses. / &C and exact glyph variants `...,.'` plus a backtick; its description combines hard pale grains and worn patches. The floor does not gain collision, an ore resource, a glow or structural damage states from this art work. Generation files live under `Assets/Scripts/Gameplay/World/Generation/Builders/`; this pass does not infer which layout generated either capture.

Current `ZoneRenderer.FlatPixelPaint.cs` paints native foreground and owned local/ambient ground using authored colors before the game's broader presentation grading. Do not assume the old glyph's depth dimming will multiply native material RGB. Retain existing fog/disclosure and grade; actual after views must judge the chosen contrast.

## Material direction worth carrying into native design

The useful contrast is a cool, pale-gray hard mineral wall against a flatter, quieter gray floor. Small pale flecks embedded in compact uneven faces can distinguish Quartzite from Slate's repeated paired slabs and from stacked masonry. Use restrained internal2D shading to express the wall material while its outer rectangle remains fully opaque and straight. Mineral grains are texture, not crystals, stars, emissive light or harvest promises.

The legacy macro-floor base RGB104,116,120 (#687478) is a **candidate**, not an approved frozen palette. Native floor trials should keep most pixels identical to the base with only a few short low-contrast worn grains. The concept's floor is substantially too busy. Its walls offer a direction for hardness and brightness, not exact shapes to copy.

## Rejected concept differences

- Output1586×992 is not the1280×800 actual capture and is not an exact16×24 production grid. Sidebar/hotbar text, player pixels, geometry scale and lighting drift must not be adopted or used for image-parity claims.
- The generated floor repeats pale pebble clusters across nearly every cell. Reject this carpet. Quiet negative space and readable gameplay marks take priority.
- The wall rendering has repeated rounded boulders, raised-looking side faces and heavy small shadows. Reject detached piles, extrusions, beveled borders, black tile frames and a “stone ring” around every visible/fog frontier. Only real disclosed wall owners may receive a wall body.
- Keep the actual traversable shape, placed owner footprints, stair/player coordinates, fog, remembered/hidden areas and all unrelated props. The reference cannot add new boundary architecture, collectibles, pools or scenery.
- No pixels from imagegen will be cropped, downsampled, traced or installed as game assets. Native production, if approved, will author original integer-cell geometry and retain editable sources.

## Proposed minimal parts — not yet produced or frozen

| Part / actual owner | Proposed cells | Shape, palette and essential state |
| --- | --- | --- |
| QuartziteFloor |0–3, four16×24 opaque cells | Quiet flat gray base, a sparse handful of short low-contrast worn grains; favor a near-blank base variant. Preserve full straight cell coverage; no border, large crack, rubble mound, resource cue or glow. Candidate base104/116/120 remains subject to root review and actual grade. |
| QuartziteWall |4–5, two16×24 opaque cells | Continuous mineral mass with irregular compact internal facets and a few embedded bright grains. All boundaries share one material base, with no transparent recess or adjacency mask. Avoid mirrored pairs, masonry, repeated circles or detached pebble outlines. A surviving1–55HP wall retains its truthful solid body; destruction removes it and exposes existing Rubble. |
| Rubble |Existing unchanged square source | Borrow the approved body as a separate owner. Do not repaint it to harmonize a concept or invent a damaged QuartziteWall sprite. |
| Stalagmite |Existing unchanged square source | Preserve its compact identity and footprint; no new decorative population. |
| StairsDown / StairsUp |Existing opening-anchors frame4 / opening-cellar frame0 | Reuse unchanged approved cells, real endpoint authority and reader identity. No new stair placement. |

The six new cells would form96×24 with16PPU centered geometry, Point/Clamp and no mipmaps/compression, matching the current finite cave companion contract. These are construction constraints, not a new runtime specification or an implementation authorization. No animation or health-stage atlas is needed for this material pass.

## Next gate and limits

Root should approve or reject the **material direction and parts contract**, not the generated composition. After approval, original editable Blender integer-cell planes can produce exact native sprites plus directly rendered contacts; source tests should reject wrong dimensions, alpha, opaque-edge drift, unintended palette changes and borrowed-input edits. Native runtime RED is root-owned and must precede production code. Then ordinary actual9/10 before/after views, readers, collision/destruction, travel and fresh Continue must establish bounded integration. Fullness, coherent whole-chunk composition and later biome/area polish remain separate.

## Root direction review and native source authorization

Root reviewed the generated reference and authorized original native source production only. The accepted direction is compact granular mass/internal shading above a quieter floor; bright cyan endcaps, uniform relighting and all UI/geometry/fog drift are explicitly rejected. This is not actual visual parity approval. The floor palette is now frozen for the first source trial: base104/116/120, pale114/126/130 and dark92/104/110, with4–8 accent pixels in each384-pixel cell. Two wall variants will use3–5 irregular interlocking internal mineral planes and a few embedded pale grains, within straight fully opaque borders. No isolated boulders, paired Slate slabs, heavy grid or outside shadows.

Read-only peer source review confirms Stone/Mineral55/5, the same surviving body at1–55HP, unchanged olive Rubble on destruction, and no floor Material/Thermal/Destructible. Current Pillar, BrokenColumn and SteamVent owners must not be aliased as Quartzite. Source art production is authorized; runtime/tests/Assets and actual integration remain root-owned separate gates.
