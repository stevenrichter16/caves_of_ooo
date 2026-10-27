# Original native Shambler source

Status: source reconstruction verified and approved by root for an integration trial on 2026-10-27. This is not runtime, package, biome or polish acceptance. The existing README, prompts, imagegen references and Mosshulk/ChoirTendril artwork are unchanged.

The scene and transparent parts references guided only the small drooped head, crooked narrow mauve torso, uneven feet and one attached tapering tendril. The native drawing is original manually authored cell geometry. Concept pixels are not sampled, traced, downsampled or used as render input. The cane-like hook, gradients, halo, soft alpha and altered scene/HUD geometry were rejected. There is no new attack pose, tongue, corpse, cloud or animation.

`Tools/Live16x24/build_shambler.py` creates named integer unit quads in Blender using the established opening-cast helpers. The 16×24 transparent PNG is rendered directly from that geometry. The contact renders the same geometry at exactly eight pixels per cell over stone, green and reader backings; it is not a resized raster. The saved `.blend` retains the native render scene and 38 editable named mesh clusters.

The silhouette contains 95 occupied cells within top-left bounds `[2,5,13,23]` (11×18), with baseline row 22 and an empty row 23. All occupied cells are four-connected. Seven opaque flat colors and binary alpha are used. The 12-cell attached tendril ends at row 19, above the feet; the leg gap remains open. The source JSON is the exact part/color cell specification.

Palette: `#3F3343`, `#665164`, `#92768A`, `#B39AA9`, `#6B775F`, `#92A283`, `#282731`.

## Verification before runtime import

- `source-verification.json`: all 384 native RGBA cells exactly match the authored source; all 73,728 sampled direct 8× contact pixels match their cells/backings; binary alpha, shape, palette and connectivity constraints pass.
- Five deliberate corruptions are rejected: partial alpha, incorrect tendril color, wrong canvas dimensions, a missing baseline foot and source-cell color drift. The verifier only decodes/checks images; it never edits or saves image data.
- `blend-geometry-verification.json`: reopening the saved source recovers all 95 integer quads with exact cell, part and palette agreement. The native scene remains 16×24 at 100%, transparent. Inspection did not save the blend again.
- All 1,656 pre-existing protected inputs matched their recorded SHA-256 values before runtime import. `protected-inputs.json` retains those original pins; there is no automatic repinning. Mutable release/world ledgers and this task's new outputs are outside the protection set. If Unity later normalizes a pinned older `.meta`, record and reconcile that separate change explicitly rather than treating a failed protection check as artwork approval.

## Output pins

| File | SHA-256 |
|---|---|
| `shambler.png` | `e0d8be101e58460fb1a2e45503a2f7b1a729be838466a7468ec8d488a95e3287` |
| `shambler.blend` | `502a8a2da0f873d473b403ff58c414026ed1bc020eb93793360f3dcc0962f247` |
| `shambler-contact.png` | `cd10c4c200720cccf040df3223e3ae4af1e882d9fc119a437e81103d22235105` |
| `source-cells.json` | `84811c1e063ca516c1f0fc3954c7a9597729d90dcbb1662c450cc09339d461b9` |
| `Tools/Live16x24/build_shambler.py` | `7ee4fbd4ef5d967c26ed63abcb34859bc6388e7e2b18a350adaa10f3d7ce3084` |

## Review boundary

The contact supports a readable former-person silhouette against green and reader backings. The mauve colors are intentionally subdued against pale stone; actual biome grading, normal zoom, commitment and status/damage cues still require root's real gameplay review. Accepted native pixels are frozen unless a named gameplay defect warrants a source correction. The original reference files remain intact. Runtime resource copying/import, RED/GREEN tests, normal gameplay, save/restart and packaged delivery are root-owned subsequent gates.

Reproduce with installed Blender:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_shambler.py -- --build
python3 Tools/Live16x24/build_shambler.py --verify
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Live16x24/grovelands-actors/shambler.blend --python Tools/Live16x24/build_shambler.py -- --inspect-blend
```

Rebuilding writes only the builder's named source outputs. The supplied verification receipts describe the already-completed pre-import checks; they do not claim an unperformed later recheck.
