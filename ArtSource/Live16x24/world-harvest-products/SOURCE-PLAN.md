Historical pre-authoring plan, retained. Implemented source status and deviations are recorded in README.md. The concept wait below is historical; both concepts were inspected before final native authoring.

# Eight original portable harvest sprites — native source plan

2026-10-27. Status: source and approved references inspected; **awaiting the root's actual-inventory concept before final native authoring**. This document is a scratch design handoff, not art or gameplay acceptance. No production image, runtime, Unity import or new test execution has been created by this planning pass.

## Frozen packet and ownership

Proposed author-owned files, subject to root confirming the packet name: `Tools/Live16x24/build_world_harvest_products.py` and `ArtSource/Live16x24/world-harvest-products/**`. Root retains concept/prompt ownership if placed there and owns all runtime, Assets, Unity, Git and ordinary gameplay actions.

Exactly eight original frames in `world-harvest-products.png`: horizontal128×24, each16×24, transparent background, binary alpha,16PPU, centered pivot, Point/Clamp, no mipmaps or compression. Native collections sit on the same lower baseline as approved loose goods: last occupied row22 and row23 empty. Proposed silhouettes fit x2–13 with variable negative space above them; they are carried products rather than miniaturized terrain owners. One static identity per product; no stock, price, effect, cooking, quantity or animation state is painted into it.

The nine borrowed identities remain byte-exact in their original resources: opening-items6 GlowQuartz,4 Mushroom,9 FireClay,10 SalvagedTimber; north-works-products0 StoneburrSeed,1 FrostLichen,2 MendleafSprig,3 WildBerries,4 GoldCoin. The new atlas does not copy, recolor, reslice or replace them. In particular, opening-items11 is a specific Marlback corpse and is not Bone art.

## Preimplementation source corrections

Inspected actual `Objects.json` definitions alongside `Docs/GRAPHICS-HARVEST-PRODUCTS.md`, both approved item contacts and their generators, Beating's native generator/verifier, and `Docs/LIVE-16X24-ART-DIRECTION.md` required pipeline.

- PaleSalt and ChoirIron own MineralPreparation, but no MaterialPart is authored on PaleSalt. Tepuibone owns `shape_head`, not MineralPreparation. These distinctions remain runtime contracts; no gameplay part is added to earn a sprite.
- Bone is one worn animal-bone length. Tepuibone is dense sandstone cut from the Root retaining wood-like grain. Their pale colors must not collapse their silhouettes into one identity.
- GroveRed is a raw reagent with vital3 and toxic1, and a detoxification recipe. Its source RenderLayer is3. It must differ from healthy WildBerries and the separate CleansedGrovePulp; do not advertise a safe food or tonic.
- Honeycomb and SaltbriarSprig have Food.Cooking=`Meal`; that label alone is not a new Cookable recipe or cooked product.
- SaltbriarSprig is harvested produce, not a planted Saltbriar owner. KnotflaxCord is already stripped/twisted cord, not a crop or a finished bandage.
- Stack counts are state, not artwork. A cluster of material facets or a coil indicates shape only. Neither the image nor the verifier promises a number of inventory units.
- This source packet verifies geometry/pixels and preservation only. It cannot establish portable admission, inventory authority, consumability, disclosure, import or actual player journeys.

## Proposed original silhouettes

Final cell layout follows the forthcoming concept critique, not a raster trace. Occupancy boxes are design targets, not yet measured results.

| Frame | Exact product | Native part design | Palette and separation |
|---|---|---|---|
|0|PaleSalt|Low irregular broken crystal cluster, about8×7 occupied pixels, two or three short unequal angular faces with a quiet broken base. One or two attached pale crumbs at most. No tall upright vein, aura or landscape base.|Borrow Beating mineral `C6BDA0`, edge `E0D6B5`, shadow `8D826C`; perhaps approved pale salt `C3B99A` only if needed. Pale compact cluster differs from tall green oblique GlowQuartz.|
|1|Bone|One slightly diagonal worn shaft, about9×12, narrowed middle, one gently bulbous end and one broken porous end. One dark pore in the cut face, no skull, ribs, crossed bones or weapon fitting.|Beating bone `AFA688`, light `D2C7A6`, mineral shadow `8D826C`. Long organic shaft distinguishes it from squat Tepuibone.|
|2|SaltbriarSprig|Short detached twig, about8×11, with sparse small gray/tan pointed leaves and two quiet salt-rimed tips; trimmed stem ends at row22. Open asymmetric branching, no rooted crown.|Beating briar `6C705E`, leaf `8E9480`, salt `B9B79C`, stem/shadow `535449`. Thin gray leaves distinguish it from broad green Mendleaf.|
|3|ChoirIron|One compact fractured dark ore wedge, about9×7, with an uneven metallic face and two restrained reddish fragments attached to the broken edge. No ingot, generic pebbles or silver weapon blade.|Opening stone-dark `3C5148`, stone `526657`, rust `9B442B`, optional stone-light `7D8E71`. Matte muted highlights, no glow; darker/angular than FireClay and pale minerals.|
|4|Honeycomb|A small broken amber wedge, about9×8, with three readable sparse dark wax-cell hollows inside a connected wax rim and one short sticky torn corner. No jar, hive or hexagonal full-tile texture.|Opening ochre `C99538`, ochre-dark `9C6B32`, grain `D8A446`, optional cream `E4D0A0`. Broken wax shape differs from solid GoldCoin disk.|
|5|GroveRed|One dense asymmetric raw growth/fruit, about9×9, with a deep inward crease and a short torn stalk or folded growth seam; single continuous mass rather than a berry bunch. No face, eye, brain illustration, clean cutlery or safety icon.|Prefer approved rust `9B442B`, rust-light `B85834`, dark leather/ink family. If the concept requires a deeper dried-red face, nominate one restrained explicit hex in the manifest; do not recolor any existing product. Toxicity remains disclosed in text, not an invented poison status glyph.|
|6|Tepuibone|Squat dense chipped stone block, about10×8, flat cut flank and one closed compressed curved grain band. Pale wood-memory stratum remains enclosed in the rock. No porous animal end, freestanding log or hammer head.|Beating sandstone top `B29B73`, pale mineral `C6BDA0`, bone-light `D2C7A6`, shadow `8D826C`. Wide closed mass and grain differ from Bone's long shaft.|
|7|KnotflaxCord|A compact oval coil, about10×9, one clear central negative-space opening, short loose tail and a small offset crossing/knot. Sparse alternating braid strokes, no dangling snare installation or rectangular dressing.|Opening linen `B5A47E`, cream `E4D0A0`, leather `5F4830`; Beating bone-light if needed. An open loop must read as cord rather than a stone disk.|

Use3–4 local colors per product, with deliberate quiet value shapes; never add bevels, outlines or microdetail merely to increase complexity. Final contrast must be checked on approved dark green ground, warm sand and the actual black inventory panel. The source contact is a direct geometry render, not an edited screenshot.

## Minimal production workflow

1. Root supplies actual inventory/drop/F1 reference(s), imagegen output and exact prompt. Inspect all supplied images. Save a short accept/reject critique and the resulting part map, including source-to-product distinctions and which families still lack ordinary captures.
2. Snapshot hashes of approved source images, editable blends, source-cells, manifests and their generators. Include both borrowed atlases and source families used as palette anchors. Exclude new packet files and root's actively written living records/concepts from immutable-input claims. No existing approved source is rewritten.
3. Author the packet's decode-only `verify.py` before any native outputs and run it: it must fail explicitly for missing production assets. Retain this RED receipt. This is source verification, distinct from root's later native Unity RED.
4. Author original named integer-cell geometry. Reuse existing `build_cindercaller.model`, `build_opening_base.camera_for/emission_material/add_label` and `build_north_plot.instance/plane` read-only. Use a local small Frame class and local explicit palette; avoid mutating imported global palettes. The generator reads no raster input and writes only the new packet. Disable `.blend1` backup generation.
5. Render native128×24 directly. Render a native contact and an8× contact directly from the same scene geometry. Keep a neutral overview plus small dark-green, sand and inventory-dark background studies using instances; no resize or post-render pixel writes. Save editable `.blend`, named source-cells, manifest, contacts, source import metadata and verifier result.
6. Inspect native and enlarged contact for palette, silhouette distinction and source fidelity. Ask root/peer to review the exact original contact; no Assets copy before root approval for trial. If a concrete defect warrants revision, retain the initial source and receipt before modifying it.
7. One bounded rebuild checks the same semantic cells and decoded RGBA plus contacts. PNG file metadata may vary with render time; record both encoded artifact SHA and decoded-pixel SHA, and claim only the reproducibility actually observed. Keep the frozen approved artifact and final handoff hashes.
8. Root copies/imports approved source, performs portable17 runtime tests and real earned-item journeys. Mark source acceptance as trial-only until those captures establish actual inventory/world/examine behavior. Do not claim all17 observed from eight-frame source completion.

## Source verifier and counterchecks

Independent decode-only checks: exact8-frame owner/order/map;128×24 dimensions;384 cells per frame; named integer cell colors from the explicit palette; nonempty connected compact silhouette; transparent margins/bottom; binary alpha; all eight normalized outlines distinct; plausible3–4 color budget; exact source-to-native pixel equality; direct contact integer-scale equality; every manifest rect and claimed artifact hash; source import settings; all frozen approved inputs unchanged.

Material safeguards remain bounded and honest: Bone requires shaft plus broken-end/pore geometry, Tepuibone a closed stone mass plus enclosed grain, Honeycomb a connected wax outline with bounded internal hollows, cord a bounded clear loop and knot, Saltbriar a cut stem and gray leaf tips, GroveRed an unpurified raw growth seam. These are source-design invariants, not mechanical validation.

Pair positive checks with mutations in memory only: missing frame, reordered/wrong owner, exact duplicate normalized silhouette, opaque background/bottom row, unknown palette token, out-of-bounds cell grid, missing bone cut, cord loop filled, Honeycomb hollow budget removed, Tepuibone changed to bone shaft, excessive color/bright highlight, and imported metadata changed. Every mutation must be rejected by its intended check. Do not edit a production PNG to manufacture a test.

## Ready / waiting

Ready to author after concept review. No Unity/Git/Assets action is needed from this agent. Root currently owns the actual source/action captures and imagegen step; exact concept acceptance remains outstanding. The required eight frames and nine immutable reuses are understood, and the existing native Blender pipeline is sufficient without a new framework.
