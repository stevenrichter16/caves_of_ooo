# Native gleaners’ cellar art

Status: source art rendered and locally verified, 2026-10-23. This folder contains editable original Blender pixel geometry, nine static 16×24 frames and the source/verification receipts. Runtime integration, owner routing and ordinary in-game acceptance are separate gates owned by the parent task; this source receipt does not claim them.

## Rebuild and inspect

From the repository root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_cellar.py
python3 ArtSource/Live16x24/cellar/verify.py
```

- `opening-cellar.blend`: named editable mesh collections for all nine frames, plus a separate contact scene. One mesh unit is one native pixel. Flat emission materials use the previously accepted palette.
- `opening-cellar.png`: 144×24 transparent atlas, left-to-right indices below. Objects are centered within their cells, touch row22 and leave row23 transparent. Frame8 is the deliberate full-cell terrain exception.
- `opening-cellar-contact.png`: 1280×1024 direct eightfold geometry render, with an additional three-cell masonry repetition. It is not a resized PNG.
- `source-cells.json`: exact named-part/color grid at native resolution, top-left origin. It is generated from original geometry instructions, not sampled from the concept.
- `manifest.json`: identities, bounds, palettes, known functions, limitations, frame rectangles and current PNG SHA256.
- `verify.py` / `verification.json`: read-only raster/source/contact and prior-art preservation checks. The validator writes only its JSON receipt.

The intended import is16PPU, centered tile pivot, Point filtering, Clamp, no mipmaps, no compression. These are static identity/state frames, not animation frames. The three crop stages are distinct existing gameplay states.

The final frozen PNG SHA256 is `94a9dd85bee742f03543138e877b39adec2e7058b9c732911025ef976ec7cf77`. Its decoded RGBA SHA256 is `5b28a167bb7579f2382e371c55960cc2ba6bdcba6fea41e160ddea3f07bb52de`. A repeated Blender build matched every source pixel but changed the whole-file checksum: Cycles writes variable `cycles.ViewLayer.total_time`/`render_time` PNG text metadata even when image stamping is disabled. Reproduction is exact at the native pixel level; a rebuilt artifact must be imported with its newly recorded manifest hash. No PNG metadata stripping or raster rewrite is used.

| Index | Native identity | Readable visual distinction |
| --- | --- | --- |
| 0 | `StairsUp` | Three widening pale treads between short stone cheeks; ascending passage, unlike the earlier dark descending pit. |
| 1 | `Crate` | Squat plank box with corner braces and recessed boards; no chest latch, arched lid or painted contents. |
| 2 | `GleanersTimberPallet` | Three broad low slats across two runners, with open spaces between boards; not the smaller loose timber item. |
| 3 | `SootrootCrop`, stage0 | Small planted notch with soil footing. |
| 4 | `SootrootCrop`, stage1 | Two angular leaves and a short stem; no exposed harvest root. |
| 5 | `SootrootCrop`, stage2 | Three-leaf crown over dark root shoulders, with a restrained pale seam. |
| 6 | `SootrootPulp` | Compact torn root portion, dark skin and pale interior. |
| 7 | `SootrootSeed` | One tiny tapered kernel with a pale crease; no soil or packet. |
| 8 | `StoneWall` | Solid quiet stone courses with staggered joints, continuous to all four cell boundaries. |

## Source sweep and honest state constraints

The source contract is documented in `Docs/LIVE-16X24-CELLAR.md`. The authoring sweep checked `GleanersCellarBuilder`, `CropPart`, `CropYieldService`, `CureTonicPart`, `StairsUpPart` and the actual blueprints in `Objects.json`.

- The crate has no persistent open-lid state. Looting it does not make a second art state. The finite stock remains actual container contents.
- The pallet is a solid haulable owner with an unspent timber harvest. Dismantling removes it and creates the actual timber items. There is no depleted pallet, decorative supply stack or painted strength/readiness indicator.
- Sootroot has exactly three growth stages. Mature harvest removes the plant and produces pulp and seed; the cultivated floor remains. Moisture is already a ground-layer responsibility. No wooden planter, wilted stage or harvested standing crop was added.
- Growth timing is mutable and uses elapsed-world conversion; old prose about a fixed number of “moist rounds” is not a visual contract. The atlas makes no timer promise.
- Pulp is a topical burning cure with a non-drinkable tonic contract, not a healing drink. Seed plants sootroot on cultivated soil. Their native sizes do not promise item counts.
- Plain masonry must route through actual owned `StoneWall`/Wall/solid/non-takeable semantics. No glade VisualID or new collision is assigned by art. Current world occupancy, fog, carried/examine authorization and state invalidation remain runtime responsibilities.

## Greybox → concept → native critique

The parent first walked the real seed64 route through the glade stairs, cellar landing, service beam detour and store, using ordinary controls with40HP intact. Actual inputs are `Docs/Verification/Live16x24/cellar-greybox-entry.png`, `cellar-greybox-service.png` and `cellar-greybox-store.png`. The images establish an existing layout and owners; the authoring does not move them or add stock.

The selected concept is `ArtSource/Live16x24/concepts/cellar-parts-v2.png`, extracted after `cellar-paintover-v1.png`. Its rising treads, braced box, slatted pallet, growth silhouettes and dark-root products were retained. The first extracted parts reference was rejected by the parent for halos. The selected second reference still contained gradients, irregular pixel grain and overly large objects; these were not copied into the native art. The scene paintover’s planters, noise and atmospheric lighting were also excluded because they did not correspond to accepted owners or flat rendering.

Pass1 reconstructed the eight object/state/product silhouettes using original integer-cell quads and3–5 flat colors each. The native atlas and direct8× contact were inspected together. The three treads, closed crate, slat gaps, ordered growth heights, restrained uncut-root seam and compact loose products were retained. All source/native/contact pixels matched exactly.

Pass2 corrected the masonry coverage after the parent inspected the accepted surface ruin pillar against actual cellar corridors. Repeating that narrow stepped pillar would visually imply gaps in solid walls. This is a deliberate post-concept scope correction: one ninth terrain frame, preserving the first eight indices and pixels. Its entire16×24 area is opaque, including row23, and its joint pattern repeats without holes or intrusion into neighboring cells. The three-cell contact preview uses the same collection instances. There is no perspective cap, halo or decorative damage that suggests a walkable break.

The atlas generator never reads a raster, traces an image, downsamples a concept or modifies any approved PNG. Geometry is rendered directly at144×24; the contact is separately rendered at8×. All materials come from the accepted opening palette. The original user sprites and base/cast/anchors/states/motion/items atlases remain untouched.

## Verification and limits

The verifier was authored first and failed because `source-cells.json` did not exist. After the initial eight-frame render it passed. Before adding masonry, its updated144×24/nine-frame expectation failed against the old128×24 artifact. The final render passes all checks:

- Exact native dimensions, binary alpha,3–5 colors per frame, frame identity/order and manifest hash.
- Zero source/native mismatches and zero contact/native mismatches, including every8×8 cell and repeated wall preview.
- Objects touch row22 and leave row23 clear; terrain has384 opaque pixels with no fake wall gaps.
- All nine silhouettes are connected. Growth heights increase4→11→18pixels. Pulp is6pixels high/33opaque pixels; seed is5pixels high/15opaque pixels.
- Three ascending tread rows, open pallet board gaps and separate bottom runners.
- Prior approved raster bytes match committed or accepted-manifest hashes.

These checks verify native source integrity and the stated static shape constraints. They do not prove real Unity resource binding, exact owner/state selection, visibility, harvesting, transfers, input flow or performance. The parent’s native runtime tests and ordinary cellar revisit are required before calling the integrated slice accepted. There is no motion, lighting, directional sheet or release-wide biome coverage claim in this packet.
