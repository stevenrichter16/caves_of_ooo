# Eight portable harvest products — original native source

2026-10-27. **Status: source verified, root reviewed and approved for an integration trial.** The atlas and import metadata are frozen. Actual portable17 runtime tests and ordinary inventory/world/examine acceptance belong to the parent task; this source packet does not establish those gates. No optional animation, gameplay content, item properties or UI redesign is added here.

## Real source and concept extraction

The actual earned PaleSalt item appears as a large yellow glyph in the packaged inventory capture `Docs/Verification/Live16x24/packaged-beating-materials-07-resource-inventory.jpg`. Root then captured the real current inventory, F1 details and ground drop in `harvest-products-before-02-inventory.png`, `-03-details.png` and `-05-ground.png` under the same verification directory. These show the actual product/custody surfaces to fix, not a generated stocked inventory. Other seven new identities are grounded in the loaded Objects definitions; this packet does not claim ordinary observation of every one.

Root's built-in imagegen produced `concepts/inventory-study.png` and `concepts/parts-study.png`. Their exact prompts are retained alongside them; `references.json` records the input/output hashes. Both generated images are design references, not gameplay evidence or production textures. The source author inspected them and the approved opening-items and north-works-products contacts before final native authoring.

Retained: short pale broken crystals, a worn bone shaft, a detached gray sprig, fractured dark ore, broken amber wax, a distinct raw red growth, pale grained stone and an open pale cord coil. Rejected: the concept's oversize detail, smooth shading, many scattered crumbs, a large living mineral deposit, ordinary clustered berries for GroveRed, and a literal wood log for Tepuibone. The UI interpretation did not authorize UI/text changes. All native cells are newly authored geometry; no generated raster was traced, cropped, sampled, resized or edited into a sprite.

## Frozen frame map

`world-harvest-products.png`:128×24, eight horizontal16×24 frames; resource `Sprites/Live16x24/world-harvest-products`. All silhouettes end at row22, with transparent row23 and clear side margins. The manifest records centered pivot,16PPU, Point/Clamp, no mipmaps and no compression.

|Index|Exact blueprint|Native identity and limits|Occupied bounds|
|---|---|---|---|
|0|PaleSalt|Short unequal pale crystal faces joined by a broken base. A carried mineral measure, not the live vein or glowing quartz; no MaterialPart is invented.|9×7|
|1|Bone|One slender diagonal worn animal shaft, with a visible dark pore in its broken end. Not Bones terrain, a skull, corpse or stone.|10×12|
|2|SaltbriarSprig|Detached gray/tan twig, sparse narrow leaves and two quiet salt tips. No rooted crown, berries or healing sparkle.|8×11|
|3|ChoirIron|Compact dark ore wedge, muted metallic face and short rust fractures. Neither an ingot nor a new IronOre identity.|9×7|
|4|Honeycomb|Broken amber wax wedge with bounded dark cell hollows. No honey bottle, bee, hive or invented cooking result.|9×8|
|5|GroveRed|One asymmetric creased raw red growth with a torn stalk. It remains the vital3/toxic1 reagent; not ordinary berries or CleansedGrovePulp.|9×11|
|6|Tepuibone|Squat chipped pale sandstone with enclosed wood-memory grain. Stone from the Root, not animal Bone, a log or a prepared hammer head.|10×8|
|7|KnotflaxCord|Open oval coil, a small crossing knot and short loose tail. Cord rather than planted fiber, a bandage or installed trap.|9×9|

Bounds describe occupied pixels, not physical dimensions or inventory quantities. The single static identity does not encode prices, current stack count, recipes, charged effects, safe consumption or condition. Exact gameplay semantics, changing overlays and authorized disclosure remain runtime responsibilities.

The nine reused frames remain in their original resources, unchanged: opening-items6 GlowQuartz,4 Mushroom,9 FireClay,10 SalvagedTimber; north-works-products0 StoneburrSeed,1 FrostLichen,2 MendleafSprig,3 WildBerries,4 GoldCoin. This generator neither copies those frames into a new atlas nor recolors them. Opening-items11 remains the specific Marlback corpse, never a Bone substitute.

## Palette and editable production

Each product uses three or four colors from approved item/Beating families. Pale mineral uses `C6BDA0/E0D6B5/8D826C`; bone uses `AFA688/D2C7A6`; the gray sprig uses `6C705E/8E9480/B9B79C/535449`; ore uses `3C5148/526657/7D8E71` with rust `9B442B`; wax uses `C99538/9C6B32/D8A446/E4D0A0`; raw growth uses `9B442B/B85834/342E25/5F4830`; sandstone adds `B29B73`; cord uses `B5A47E/E4D0A0/5F4830`. The complete named palette is in `source-cells.json` and the generator. No prior palette or image is edited.

`Tools/Live16x24/build_world_harvest_products.py` creates named integer quads in an editable Blender file. Existing mesh, material, instance and camera helpers are imported read-only. It writes only this packet, does not read raster inputs, and disables Blender backup generation. The native atlas, native contact and8× contact are separate direct geometry renders. Contacts repeat the same exact sprites on approved dark green, warm sand and near-black inventory backgrounds; these are source-only contrast studies, not game reconstructions. Every source cell and every contact placement is checked by an independent decode-only verifier.

Reproduce from the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_world_harvest_products.py
python3 ArtSource/Live16x24/world-harvest-products/verify.py
```

The accepted output is frozen; rebuild deliberately and update receipts if work resumes. Do not run the generator merely to inspect the package.

## Verification and preservation

`verify.py` was written and executed before native production. `source-red.txt` retains the expected missing-art failure. First production and the reproducibility rebuild pass: exact eight-owner order, transparent128×24 grid, binary alpha, connected compact silhouettes, distinct translation-normalized outlines, three/four colors, correct lower baseline, exact source/native/contact pixels and manifest regions. All317 snapshotted approved inputs remain unchanged, including the nine borrowed sprites' original sources. No Assets import or Unity action was performed by this source author.

Thirteen in-memory counters are rejected: missing frame, wrong owner, duplicate outline, opaque bottom row, unknown palette, wrong grid width, missing bone cut, filled cord loop, missing wax hollows, missing stone grain, neon salt palette, missing raw-growth fold and changed import filtering. These establish bounded source invariants, not gameplay semantics. Source import metadata has fresh GUIDs and uses the existing semantic metadata comparator; optional `--check-unity` compares the runtime PNG bytes and every authored metadata key while tolerating only additional Unity defaults. That optional import check has not been run by this source author at handoff.

One rebuild reproduced `source-cells.json` and metadata byte-for-byte and all three images' decoded RGBA exactly. The PNG file hash changed because the renderer's metadata can vary. `rebuild-before.json` and `reproducibility.json` record both artifacts honestly; this is decoded-pixel reproducibility, not encoded-PNG or cross-platform determinism. No `.blend1` exists.

Frozen native PNG SHA256: `18fad5c1d0a20c1ba8aac2790f2fa4aee2f05e49154b3e945c4a68f0f8f71683`.

Decoded RGBA SHA256: `c607476ce27c1eb71c79b181dfa8caca1d3e31759757ed1cc35f319e46388f9c`.

Authored metadata SHA256: `1fb7b31e9a07d4e64fabf89bfd3e7c759b6704b17e3a6879bf5e39c461c3d0da`.

## Review and remaining acceptance

The source author viewed the original atlas and both contacts. Root independently viewed the contact and accepted all eight as distinct and consistent on green ground, sand and the inventory background for integration trial. The quiet native scale follows the existing items; extra detail is not added solely to increase iteration count.

Root must still prove current semantic owners, strict refusal/status behavior, borrowed frame integrity, public inventory/examine custody, genuine drop/pickup and harvest output, save/reload and ordinary transport. Do not infer those from the PNG, source counters or this static review. This packet alone does not complete all17 carried identities, all game items, any whole biome, graphical fullness, controls or physical Steam Deck performance. Preserve the final root-owned receipts and captures in the parent living document.
