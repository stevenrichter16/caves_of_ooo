# East / kitchen — original native 16×24 source kit

Status: original source production, native/contact verification, resource-byte preparation and independent visual review complete, 2026-10-24. Unity import, owner/state routing, ordinary gameplay and scene-level acceptance remain root-owned gates. This source packet does not claim service completion or a complete kitchen state machine.

## Contract and preserved art

One 288×24 atlas contains 18 exact 16×24 cells in the agreed order:

| Frame | Blueprint / state | Native treatment |
|---|---|---|
| 0 | TinkersForge | Tall square masonry hearth, dark unlit mouth and plain work lip |
| 1 | Oven | Squat pale masonry, clay arch, short capped neck |
| 2 | Bed | Vertical woven cot contained within one cell |
| 3 | Chair | Open-backed wooden chair with separated legs |
| 4 | ConnectedBatchPan, damaged | Broad clay pan with dark front crack |
| 5 | ConnectedBatchPan, repaired | Same footprint, pale clay repair seam |
| 6 | ConnectedKitchenEscrow | Neutral tied woven input tray |
| 7 | ConnectedKitchenPickup | Neutral handled pickup board |
| 8 | ClaspbeanCrop, stage 0 | Small cotyledons above a quiet planted crown |
| 9 | ClaspbeanCrop, stage 1 | Forked broad leaves, no pods |
| 10 | ClaspbeanCrop, stage 2 | Mature stem with paired pale hanging pods |
| 11 | ClaspbeanPulp | Small loose soft pale pulp |
| 12 | ClaspbeanSeed | One small kidney-shaped seed, no drawn quantity |
| 13 | FieldMeal | Folded linen parcel with a plain tie |
| 14 | CharmFlowers | Two sparse muted pink flowers at uneven heights |
| 15 | CopperPipe | Small copper elbow with dull green union |
| 16 | SteamVent | Neutral inset grille, no plume |
| 17 | MillStone | Pale grinding ring with an axle hole and radial groove |

`Assets/Resources/Sprites/Live16x24/east-kitchen-cook.png` is a byte copy of the unchanged approved `Assets/Art/Cook16x24/cook-16x24.png`. Its separate metadata is new. Cook was not reinterpreted, transformed, resampled or rendered. Cook SHA256: `d534b0e3cab143d8a1523829fc4bd101b76d776d307a152fd18cfe9c5681dbdd`.

The 18 new frames use 3–6 flat colors each, binary alpha and an occupied baseline at top-origin row 22; row 23 is transparent. Every new pixel stays within x=1..14 and y=0..22. The original Cook remains exempt from new-cell baseline/palette edits because preserving its exact pixels takes priority. The resource atlas is byte-identical to its native source export. Metadata requests Point, Clamp, 16 PPU, centered pivot, no mipmaps, no compression and no NPOT resizing. Root must verify actual Unity importer state.

## Pipeline and authoring

Actual baseline captures precede this art: forge reader, kitchen layout and pan menu under `Docs/Verification/Live16x24/east-kitchen-baseline-*`. `concepts/east-kitchen-v1.png` and `v2.png` with their adjacent exact prompts supply reference direction. The approved opening-anchor contact supplies palette and scale. See the parent `Docs/LIVE-16X24-EAST-KITCHEN.md` for the actual route and source semantics.

No generated pixels were opened, sampled, traced, sliced or downscaled by the generator. The new sprites are original integer-cell designs. `source-cells.json` stores each named part/color cell; `east-kitchen.blend` stores the same cells as editable flat emission meshes in named frame collections. The atlas and 8× contact are separate direct Blender geometry renders. The contact is **static source art**, not an actual game capture. Existing Blender helpers are imported read-only.

V1's invented side tool table, active flame, altered Cook and mislabeled millstone are rejected. V2's bevels, microtexture and wide cot are reduced into flat native shapes. Forge, oven, pan, trays and vent depict no flame, steam, ready food, ingredients, heat or active-job cue. The repaired pan changes only its actual integrity cue. Neutral containers deliberately cannot prove a working or ready state. The recipe's output quantity remains real state, never painted into a sprite.

## Source RED, correction and GREEN

`verify.py` was written and executed before the generator. `source-red.txt` records the expected assertion for the five missing source/native/contact/manifest/Blender outputs. The first source pass then caught disconnected crop-tip pixels; `source-first-pass-findings.txt` preserves that failure. Their geometry was connected without adding effects or detail. Visual polish shortened the oven from 18 to 15 occupied rows, making it squatter than the 18-row forge.

The final `verification.json` and `source-green.txt` establish:

- Exact 288×24 atlas and 1536×1024 direct 8× labeled contact.
- Exact 18-frame order; every source cell matches native and contact pixels with zero mismatches.
- Binary alpha, 3–6 colors, quiet shared palette, clean cell margins and consistent new-frame baseline.
- 17 distinct normalized outlines; only the real crack/seal pan state pair intentionally shares one outline.
- Connected geometry per frame except the intended separate two-flower cluster.
- Cook byte identity, resource-byte identity, metadata contract and fresh nonduplicated GUIDs.
- Ten prior accepted atlas hashes unchanged.

Final independent art review found no actionable art or semantic defect: cot stays vertical, pan stays clay, services stay neutral, sparse flowers stay legible, and the palette fits the approved anchors. This is source/contact review only. Real ground contrast, selection, service custody/state repaint, actual importer behavior and frame cost require the ordinary root-owned integration pass.

Native PNG SHA256: `594079f4dbfe9d17f0b58f8fcb6adc647d63dae9cfd385ce32fd8b6e31413c56`.

## Rebuild and verify

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/Live16x24/build_east_kitchen.py
python3 ArtSource/Live16x24/east-kitchen/verify.py
```

Run from the repository root. The generator only writes this source folder and the two owned resource PNG/meta pairs. Existing resource metas retain their GUIDs on rebuild. Cycles can change PNG timing metadata on a repeat render even if decoded pixels match; exact source/native/contact RGBA agreement is the reproducibility invariant. Keep the accepted artifact/hash until an intentional revision.

## Root integration addendum

The later root integration is now recorded in `Docs/LIVE-16X24-EAST-KITCHEN.md`:
495 distinct native Unity cases pass, actual ordinary repair/harvest/save/revisit
and exact native portraits are captured, repaired-pan readout P2 is closed.
This supersedes the pending root gates in the historical source-stage status;
full cooking/crop/forging lifecycle, scene composition and device gates remain
open. Source/contact proof and gameplay evidence remain separate records.
