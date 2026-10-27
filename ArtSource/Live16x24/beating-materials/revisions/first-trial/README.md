# Beating materials — original native static source

Status, 2026-10-27: **first source candidate rendered and source-verified; root approved the
contact for an actual gameplay trial. Source art is frozen for that trial.** This packet is not whole-biome
acceptance. Source author wrote only this packet (excluding root-owned concepts)
and `Tools/Live16x24/build_beating_materials.py`. No Unity, Assets import, runtime,
world generation, gameplay, save or Git mutation was performed by this author.

## Inputs and extraction

The root captured six actual Beating formation baselines under
`Docs/Verification/Live16x24/beating-baseline-01..06*`: wind barrens, salt pan,
brine lens, ruin field, caravan road and dune belt, plus a closer dune view.
All seven original gameplay PNGs were inspected. The root's terrain and parts
studies and exact imagegen prompts are retained unchanged in `concepts/`.
`references.json` records the actual filenames and content hashes.

Retain the concept's warm arid ground, pale mineral-pan contrast, calm dark teal
brine and natural low ridges. Reject its granular carpeting, added rock sprinkles,
tall cliff rendering, exaggerated masonry height, invented basin edges and
bare leafless interpretation of Saltbriar. The native art is newly authored
integer-cell geometry, not a trace, crop, resize or recolouring of raster inputs.
The root produced imagegen references; this author used Blender 5.2.1 LTS for the
native atlas, both contact magnifications and an editable `.blend`.

Actual whole scenes expose large repeated fields: the retained dune census has
505 selected DuneCrest owners. The common variant is intentionally quiet. The
contact's repeated fields use 75% base and 25% other variants, with no animated
water or texture scrolling. They are **source studies, not gameplay captures**.
They reveal repetition risks; only actual integration can resolve those risks.

## Frame map and real ownership

One `512×24` atlas, 32 distinct `16×24` frames. Rectangles are
`[index*16,0,16,24]`, centered tile pivot, 16PPU, Point filtering, Clamp, no
mipmaps, no compression. The root owns Unity metadata and import.

| Frames | Canonical owner / essential state | Native treatment |
| --- | --- | --- |
| 0–3 | Sand | Opaque warm walking ground; over 95% common base per frame, sparse low-contrast wind fragments, shared quiet boundaries. |
| 4–7 | SaltCrust | Opaque pale flat pan, a few fine irregular cracks and small thin plates; no blockwall shading. |
| 8–11 | DuneCrest | Opaque solid sand mass with broad low-contrast wind strata and a small muted lee edge; no isolated mountains, gaps or tall cliff. |
| 12–15 | SandstoneFloor | Opaque warm flat paving with short broken shallow joins. |
| 16–17 | SandstoneWall | Opaque low worn sandstone masonry; static variants, not separate damage stages. |
| 18–19 | Rock | Small compact rounded grey-brown solid rock; transparent surrounding cell. |
| 20–23 | BrinePool, positive stock | Calm opaque dark teal, tiny subdued level glints and depth marks. |
| 24–27 | BrinePool, depleted but still wet | Opaque slightly fainter grey-teal film; preserves a wet cue and makes no dry/safe-ground promise. |
| 28 | PaleSaltVein, unharvested | Attached pale angular deposit; distinct from loose salt, crust or ordinary rock. |
| 29 | Bones, unharvested | Low bleached ribs, a long bone and small skull; no standing corpse. |
| 30–31 | Saltbriar, unharvested | Grey branching stems with small salt-rimed leaves; different from bare DryBrush. |

Transparent props end at native row22 with row23 empty. Other terrain/solid mass
and liquid frames fill all384 pixels. Frames0–15 and20–27 use static variants;
requested runtime weights are9:1:1:1. No source consumes simulation RNG or adds
animation. Parent-owned admission must still use real current owners, visibility,
state, materials, foreign effects and surviving/disclosed geometry.

Corrected source assumptions:

- `DuneCrest` is the actual solid owner, not a `Dune` blueprint. Sand remains passable.
- `SandstoneWall` has HP30/Hardness2 and becomes Rubble. These intact variants do
  not claim destruction or wreckage art. Saltbriar has HP5; Rock and DuneCrest
  do not acquire damage stages merely because the art looks solid.
- BrinePool carries brine stock90 and a renewable water coating. Stock and coating
  are independent: depleted art remains wet. Neutral art must never erase live
  heat, cold, charge, mixed liquids or other meaningful state markers.
- PaleSaltVein yields PaleSalt1–2, Bones yields Bone1–2 and Saltbriar yields
  SaltbriarSprig1–2. Native harvesting removes those owners, so no fictitious
  retained empty-source frame is supplied.
- Actual CaravanRoad19,13 contains217 selected `RoadStone` owners with the packed
  road display, not SandstoneFloor. These frames are authored for SandstoneFloor;
  any explicit RoadStone reuse belongs to the root's documented/tested mapping.
  This packet does not silently rename or replace that functional owner.

## Palette and source editing

Full exact swatches are in `source-cells.json` and `PALETTE` in the generator.
The main material values are:

| Family | Base | Restrained supporting values |
| --- | --- | --- |
| Sand | `#735735` | `#7D5F3D`, `#6B5032` |
| Salt crust | `#C3B99A` | `#AEA489`, `#D8CEB1` |
| Solid dune | `#9C784C` | `#8C6943`, `#AC8857`, `#947047` |
| Sandstone floor | `#806A4C` | `#736047`, `#8D7656` |
| Sandstone masonry | `#765C40` | `#9E8260`, `#B29B73`, `#594A37` |
| Rounded rock | `#494640` | `#625D50`, `#7D7765` |
| Full brine | `#153D38` | `#133530`, `#31534A` |
| Depleted wet film | `#1B3F37` | `#19382F`, `#2D4C40` |
| Vein / bone | `#C6BDA0` / `#AFA688` | `#8D826C`, `#E0D6B5`, `#D2C7A6` |
| Saltbriar | `#6C705E` | `#535449`, `#8E9480`, `#B9B79C` |

Named mesh groups correspond to semantic cells and material parts. Each quad is
an exact square native cell. The Blender scenes use a head-on orthographic
camera and unlit colour materials; there is no pixel filter applied to a 3D
render. Both contact resolutions render the same geometry directly. A saved
`.blend` remains editable; `.blend1` backups are disabled for this owned export.

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --threads 4 --python Tools/Live16x24/build_beating_materials.py
python3 ArtSource/Live16x24/beating-materials/verify.py
```

Reusing `model`, camera, collection-instance and plane helpers is read-only.
The generator writes only this source packet; it never copies to Assets. The
source snapshot checks310 prior PNG/editable-source/helper inputs before and
after rendering. Mutable world ledgers and Unity-normalized metadata are not
part of that preserved-input set.

## Source verification and remaining gates

`source-red.txt` records the verifier failing before production because the
atlas, blend, cells, manifest and contacts did not exist. `source-green.txt` and
`verification.json` record the first successful source verification:

- Exact512×24 native atlas,32 distinct frames and binary alpha.
- Every native and contact pixel agrees with the semantic geometry/palette;
  no decoded pixel mismatch at1× or directly rendered4×.
- All310 preserved inputs unchanged.
- Ten negative mutations rejected: frame loss, duplicate art, opaque dune hole,
  false dry/stock liquid state, bright liquid carpet, dense sand carpet, bad prop
  baseline, wrong owner and unknown palette.

`reproducibility.json` compares a second Blender generation: semantic cells,
native PNG bytes and decoded atlas/both contacts were identical in this run.
Editable Blend binary reproducibility is not asserted. Decoder checks use Pillow
only to read pixels; no raster writing/editing/cropping/resizing occurs.

**Can verify:** original editable source, exact native dimensions, alpha,
source/contact correspondence, source determinism and preserved inputs.
**Cannot verify:** renderer admission, gameplay state changes, normal travel,
revisit/save behavior, large-field repetition, scene fullness, native Unity tests,
Linux execution or physical Steam Deck performance. Those remain root gates. The root accepted the contact while explicitly retaining
dune stratum repetition as a scene-scale question; no further source revision was
authorized before matched gameplay views.

Self-review: no source-contract blocker after the first build. Dune stratum
repetition, low-volume film readability, bright-pan scale and the player's
contrast against warm terrain remain explicit actual-view checks. Do not call
the Beating biome finished or imported based on this packet.
