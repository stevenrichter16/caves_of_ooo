# Shared storage — immutable part extraction

Status: source-review checkpoint, 2026-10-27. Five genuine before bodies are
observed through root's ordinary controls. Native RED/runtime/actual after
acceptance remain pending here. This record extracts the semantic parts and
reuse boundaries from the concept; it does not extract or repaint its pixels.

## Concept critique

The built-in imagegen result in [concept.png](concept.png) is a direction study.
Its exact prompt, actual reference list and hashes are in [prompt.txt](prompt.txt)
and [reuse-manifest.json](reuse-manifest.json). The copied image matches the
original generated file byte for byte.

Useful direction: distinguish a reinforced wooden chest from a braced cargo
crate, retain a compact iron strongbox, and keep mineral storage a separate
identity. Reject invented warm lighting/gradients, uneven resampled pixels,
changed body positions, removed or altered UI, and the redesigned chest. These
changes would invalidate the actual gameplay comparison and approved source.
The bottom label “sixth frame” does not select a source: the production chest
is **zero-based frame6**, x96, the seventh slot.

The successful request contained five references: the actual StrongBox and
Stump Crate views, opening-anchors, opening-cellar and cont_strongbox. The
OreCache texture was omitted because the tool accepts five reference inputs.
Consequently the generated mineral shape has no approved-source status. Use
the existing exact OreCache raster and its real Container semantics instead.
No concept pixels, palette samples or geometry are copied into production.

## Four bodies, five genuine families and one disguise exception

| Body source | Exact owners | Cached view | State and service meaning |
| --- | --- | --- | --- |
| opening-anchors frame6 | Chest, LockedChest, living MimicChest | Rect(96,0,16,24), centered16PPU FullRect | Same approved chest body for unlocked/locked and living dormant/waking/awake mimic. Static hardware does not promise a lock state. Preserve existing event/intent cues; no render-time goal scan. |
| opening-cellar frame1 | Crate | Rect(16,0,16,24), centered16PPU FullRect | Braced plank cargo box. No lid/contents/damage/empty derivative. |
| Environment/cont_strongbox | StrongBox | Rect(0,0,16,16), centered16PPU FullRect | Compact iron box; real access stays in existing lock/menu mechanics. No invented structural part or lock overlay. |
| Environment/cont_ore_cache | OreCache | Rect(0,0,16,16), centered16PPU FullRect | Mineral storage, not a mineable vein. Container capacity6; no harvest/depleted sprite state. |

All views use binary source alpha, Point filtering, no mipmaps, uncompressed
textures and Clamp wrapping. Rectangular sources occupy1×1.5 world units;
square sources occupy their original1×1 units centered in the16×24 grid. Do not
stretch, crop, resample or add padding. Tile colliders remain None; actual
Physics owns passability. Borrow textures and dispose only new cached views.

World selection/current graph/disclosure/placement/custody and authorized
reader identity remain independent gates. A surviving damaged or unlocked
body is still the same body. A dead, removed, foreign, custom or unsupported
owner must not gain native authority. A known-role refusal must not fall
through weaker local art rules. No ground or hazard representation is granted.

## Exact immutable source pins

| PNG | SHA256 |
| --- | --- |
| Assets/Resources/Sprites/Live16x24/opening-anchors.png | `aacb70020e54fe9322d507c1f1fadbcf9004bb4f7f8715ad4cb7105cea8ff98f` |
| Assets/Resources/Sprites/Live16x24/opening-cellar.png | `94a9dd85bee742f03543138e877b39adec2e7058b9c732911025ef976ec7cf77` |
| Assets/Resources/Sprites/Environment/cont_strongbox.png | `eeafc5515ded0031254e45963acd5ecbeca586aaf8b15d2051803d7a81b539e0` |
| Assets/Resources/Sprites/Environment/cont_ore_cache.png | `673011057ee2b01346b8dd0339b5a816243e3e9ca5fa1f8dcd098011c4f902a8` |

The manifest separately records exact current and HEAD518 metadata hashes.
Root verified anchors/cellar differ only in trailing whitespace; StrongBox
and OreCache raw metadata match HEAD. This worker independently hashes current
bytes and parsed import fields, and attributes HEAD comparison to root. No
metadata is rewritten, normalized or re-pinned to conceal a difference.

## Editable source and read-only verification

- Anchors: `ArtSource/Live16x24/anchors/opening-anchors.blend`,
  `source-cells.json`, original source PNG/manifest/verifier and
  `Tools/Live16x24/build_opening_anchors.py`. Frame id is `chest`.
- Cellar: `ArtSource/Live16x24/cellar/opening-cellar.blend`,
  `source-cells.json`, original source PNG/manifest/verifier and
  `Tools/Live16x24/build_cellar.py`. Frame id is `crate`.
- StrongBox/OreCache: exact existing native rasters plus the editable
  `strongbox()`/`ore_cache()` definitions in `ArtTools/coo_items.py` and exact
  entries in `ArtTools/coo_outline_pass.py`'s OUTLINE_4 list. No dedicated
  Blender source was found in the bounded search. These legacy scripts were
  read and hashed, not executed or freshly proven reproducible.

Read-only decoding compares every source-cell pixel of both rectangular
atlases: anchors4,608 and cellar3,456, zero mismatches. Source/runtime PNGs and
stored manifest hashes agree. Selected frame statistics are respectively
128opaque/6colors,154opaque/4colors,114opaque/5colors and82opaque/5colors;
all alpha values are0 or255. Existing Blender files were hashed, not reopened.
Their stored historical verification reports are explicitly separate from
this fresh pixel/hash check. All26 protected input hashes remain unchanged.

## Actual evidence and remaining gates

Root's `Docs/Verification/Live16x24/shared-storage-before-play.json` records
five genuine identities across Sill, Stump, shallow cave, limestone and Beating.
OreCache and LockedChest were examined through remote Look; neither proves
opening or contents transfer. The LockedChest approach lost HP38 to7 through
ordinary combat and poison, preserved in fixture103. A living mimic has not
been observed during this phase. This is not a six-family witness count.

The proposed companion uses four resources and four unchanged body frames,
zero new production pixels and zero new sprite states. Native RED/GREEN,
real after world/readers, combined lock/access behavior, destruction/contents
custody, living mimic disguise/event cues, persistence and package checks
remain their own gates. Whole-chunk composition, all-biome coverage and the
required later area-by-area polish are not accepted by this extraction.
