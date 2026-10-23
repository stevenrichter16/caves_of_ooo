# Opening base frames — first production group

Status: authored and visually reviewed in Blender; native import/gameplay review belongs to the integrating agent. These are **ten static base subjects**, not animation frames and not complete opening coverage.

## Files and recreation

- `opening-base.png`: transparent 160×24 atlas, ten 16×24 cells left-to-right.
- `opening-base-contact.png`: direct Blender render of the same pixel geometry at exactly 8× magnification, with labels on dark green. No atlas raster was enlarged or edited to create it.
- `opening-base.blend`: editable flat front-facing meshes grouped by frame and named sub-part; includes native-atlas and contact-sheet scenes.
- `source-cells.json`: authored cell/part/palette data, image coordinates with Y downward. Each nonempty cell becomes a flat mesh quad; no source raster sampling.
- `manifest.json`: frame rectangles, intended owners, one-cell footprints, bounds, palette and native PNG hash.
- `verification.json`: read-only pixel/dimension checks and approved-input preservation hashes.
- Generator: [build_opening_base.py](../../../Tools/Live16x24/build_opening_base.py).

Rebuild from the project root:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_opening_base.py
```

The script writes only this directory. It uses Blender 5.2.1 LTS, an orthographic camera, flat emission materials, Standard color transform, no dithering, no lights/shadows, and direct native rendering. It reads no images. The earlier clearing kit's color/render approach was consulted but its assets and generator were not modified.

## Exact index contract

| Index | Frame | Owner/state intent |
| --- | --- | --- |
| 0 | player | Player tag; ochre traveler with open face, cream front and separated boots. No baked weapon. |
| 1 | dark-wall | Wall with `reference-glade-dark-ruin` only; three tall stone masses. Lit-wall/quartz treatment retains its fallback in this slice. |
| 2 | low-wall | Wall with `reference-glade-low-wall`, including the actual low crossing whose Wall tag is removed but whose VisualID is the same. |
| 3 | tall-reeds | Existing pale Reeds; taller, narrow straw-grey blades. |
| 4 | short-reeds | Same existing owner, lower asymmetric variant. Select stably; no new population. |
| 5 | low-bush | Existing glade green-grass Bush VisualID; small olive forked sprig. |
| 6 | rubble | Rubble with `reference-glade-gravel` or BrokenColumn with `reference-glade-dark-ruin`; low separated flat stone fragments, visually passable. |
| 7 | ripe-grain | RipeCropRow while `FieldHarvestPart.Harvested == false`. |
| 8 | stubble | Plain CropRow or harvested RipeCropRow. No growth-clock promise. |
| 9 | warden | Warden; forest coat, flat cap, cream front and rust shoulder cloth. No baked weapon. |

Drawgourd does **not** use frame7. Likewise, generic creatures must not become wardens or players. A stock container, basin or well must retain its truthful fallback until its specific art is authored.

Import at 16 PPU, centered tile pivot `(0.5,0.5)`, Point filtering, no mipmaps and no lossy compression. Frame rectangles are `(index*16,0,16,24)`. One-cell world geometry is 1×1.5. A future actor-view path using a bottom-center foot anchor needs its own deliberate pivot/offset integration; do not silently reuse a centered-tile assumption.

## Design source and visual review

The viewed [parts concept](../concepts/base-parts-v1.png) supplied broad silhouettes and material contrast. The original approved native sprites and [clearing study](../../../Docs/Verification/Clearing16x24/clearing-game-final.png) supplied pixel weight and color-mass restraint. This work is original authored geometry, not crops, traces or downsampled pieces of either image.

Two render/review passes were completed:

1. Native frames and 8× contact sheet established a warm traveler versus forest/rust warden, tall versus half-height stone, straw-grey reeds versus warm grain, and low passable debris.
2. Grain changed from an uninterrupted head to alternating kernel notches so its silhouette differs from reeds independently of color. Contact labels were enlarged. Reviewed the final contact sheet directly and retained the native atlas.

The five approved PNGs remain byte-identical to repository HEAD: SeedKeeper3, Cook16x24, Tatterjay16x24, ReedbackGrazer16x24 and BeetleJar16x24. They were not imported into Blender, altered, relit or replaced.

## Verification and limits

- Atlas is exactly 160×24; each native frame is 16×24 and nonempty.
- Alpha contains only 0/255. Per-frame opaque palette sizes range 3–9 colors.
- Read-only Pillow comparison found **0 mismatches** between all 3,840 native PNG pixels and authored geometry/color cells.
- All ten contact-sheet sprite areas are exact 8× replicas of the native pixel values over `#032F23`: **0 mismatches**. The labeled sheet is a geometry render, not image processing.
- Final native PNG SHA256: `71ce34419f0e5b1a9a494dd4abd2b29efa8c5866dab97e2aaa57b1e2fb42df2e`.
- Blender source and palette are editable. No material gradients, volumetric shading, fake animation frames or extra fictional props were introduced.

Independent native/contact review found no material blocker for this static group. It confirmed actor distinction, low-versus-tall stone, reed-versus-grain silhouettes and readable stubble. Its metadata finding was fixed: the manifest now describes only the loader's exact VisualID routes, and the PNG/hash is unchanged. A low wall remains a movement obstacle where its native owner is solid; its short silhouette communicates height and visible landing, not unconditional passability.

These checks prove the source raster contract, not in-game acceptance. Root owns Resources import, exact owner/state routing, native Unity tests, actual Game capture, fog/selection/UI review and performance evidence. The group must be judged on repeated tiles and moving real owners in that capture before expanding it. Remaining characters, props, services, crops and action animation are still separate work.
