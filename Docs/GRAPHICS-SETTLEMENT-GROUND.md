# Settlement ground

**Status (2026-10-29):** two slices, ground then water and sandstone walls (below); both implemented and regression-tested; verified through offline renders of real generated settlements drawn by the real presentation catalog. Not yet captured in live play (see Limits).

## Problem (seed-64 census, flat 16×24 presentation)

The 17 authored settlements are the places players spend most time. Morrowfast has its own 3D scene and Sill its own adapter. The other 15 drew almost entirely glyph ground, because every biome adapter admits wilderness zones only. Across those 15, native cells went from 1,111 of 30,000 (4%) to 17,294 (58%) with this change:

| Settlement | Biome | Native before | Native after |
|---|---|---|---|
| the First Tent | Beating | 166 | 1,960 |
| Wellmeet | Beating | 161 | 1,925 |
| Gantry | Spread | 69 | 1,863 |
| Cinderhold | Grovelands | 25 | 1,787 |
| Posy | Spread | 89 | 1,638 |
| Slip | Spread | 91 | 1,636 |
| the Quiet's Door | Sodden | 80 | 1,631 |
| the Salt-Vault | Beating | 21 | 1,556 |
| Tally | Spread | 229 | 943 |
| Marrowstye | Spread | 18 | 729 |
| Quillhold | Spread | 23 | 557 |
| Tine | Spread | 21 | 415 |
| the Last Counter | Beating | 29 | 265 |
| the Drowned Ledger | Sodden | 67 | 231 |
| Sumphold | Spread | 22 | 158 |

Raw census text: `Docs/Verification/Live16x24/settlement-ground-census-{before,after}.txt`.

## Change

`FlatPixelSettlementGroundCatalog` serves the current cached surface graph of an authored Place other than Morrowfast and Sill, while its map POI is still that Village. It reuses art already approved for the same identities:

| Owner | Art |
|---|---|
| Grass | Settlement lawn, frames 0–3 of `sill-environment` (the same frames ordinary Spread turf uses); in Grovelands, the forest floor `grovelands-ground` |
| StoneFloor | Settlement room floors, frames 4–7 of `sill-environment` |
| Sand | Beating sand, `beating-materials` 0–3 |
| RoadStone | Opening road, `opening-ground` 16–17; in Beating, the Beating road, `beating-materials` 32–33 |

It draws ground only, with one plain dry owner per cell. Wet, mixed, stacked, hidden, remembered or foreign cells keep their fallback. No geometry, collision, state or other presentation changes.

## Tests

- `FlatPixelSettlementGroundTests`, 15 cases:
  - nine biome/owner cases across six settlements: at least 95% native, correct atlas, matching reader portrait;
  - frame families (lawn 0–3, floors 4–7);
  - Sill, Morrowfast and wilderness not admitted;
  - wet, hidden and stacked cells keep their fallback;
  - the surrounding wilderness unchanged.
- **Results.** RED: 10 failures. GREEN: 15/15.
- **Regression** (verification copy): Sill, settlement, opening and light/shadow classes 379/379; all `FlatPixel*` classes 3,748 passed. The 3 failures are bridge-log noise, the revised pin below, and the pre-existing terrain glyph audit, which also fails on committed sources.

**Deliberately revised pin.** `FlatPixelSillEnvelopeTests.ManagerlessZoneCannotAdoptSillAndOtherSettlementOnlySharesVines` asserted that Gantry's grass never uses the Sill atlas. The lawn frames 0–3 are the shared Spread turf, already used by every ordinary Spread wilderness zone. The test now requires those frames only. Sill's envelope, stone, room-floor and masonry art is still not adopted.

## Evidence

`Docs/Verification/Live16x24/settlement-ground-{posy,wellmeet,gantry,cinderhold,tally,first-tent}-offline.png` stack before (top) over after (bottom). Each is an offline render, in the verification copy, of every cell's ground and foreground sprite exactly as `FlatPixelGreyboxCatalog` resolves them for the real generated seed-64 zone. They are not gameplay captures.

## Slice 2: settlement water and sandstone walls (2026-10-29)

A census of the 15 settlements found 2,394 `WaterPuddle` and 935 `SandstoneWall` cells, all still glyphs:

| Settlement | Puddles lie on | Walls stand on |
|---|---|---|
| Posy, Slip, the Quiet's Door | Grass | — |
| Tine, Sumphold, the Drowned Ledger | Floor | Floor |
| The Salt-Vault | Sand | Sand |
| Cinderhold | — | Grass |
| Marrowstye, the Last Counter | — | Floor or StoneFloor |

Every puddle cell carries the pool's permanent water projection, so each is a real pool. Every wall is the ordinary SandstoneWall blueprint.

**Water.** Pools reuse the Sill river's water views and pool rules: `opening-ground` frames 0–15, with bank edges where disclosed neighbours are dry.
- `FlatPixelSillRiverCatalog` gained an internal constructor that takes an admission rule and a plain-substrate rule.
- The public constructor keeps Sill admission with grass, unchanged.
- Settlements admit plain Grass, Sand, StoneFloor or the canonical town `Floor` beneath a pool.
- Any second coating, or any other terrain, keeps the fallback.
- Markers still refuse, so a campfire marker over water keeps its glyph.

**Walls.** SandstoneWall reuses the approved Beating wall frames 16–17 through `FlatPixelBeatingMaterialsCatalog.SandstoneWallFrame`, which is exactly the Beating's own admission rule. The frames are fully opaque. Collision and the wall entity are unchanged.

**Result.** Native cells rose from 17,294 to 20,501 of 30,000 (68%). Per-settlement counts: `settlement-water-walls-census-after.txt`.

**Tests**
- New cases:
  - five settlement-water cases, covering Grass, Floor and Sand: at least 95% native water, no collider, reader matches;
  - four sandstone-wall cases: at least 95% native, still blocking, reader matches;
  - one counter-check: oil on a pool, and a frozen wall, keep their fallback.
- **RED:** 10 failures, 0 of N native in each.
- **GREEN:** 24 of 25. The remaining failure was the bridge-log SetUp noise.
- **Regression:** with the bridge server running, all `FlatPixel*` classes gave 3,730 passed, 0 failed and 3 inconclusive (existing allocation probes). The Sill river and Beating wall suites are included and unchanged.

**Evidence.** `settlement-water-walls-{tine,salt-vault,posy,cinderhold,last-counter}-offline.png`: before (top), after (bottom). They are rendered by the same offline method as above, not gameplay captures.

**Corrected premise.** The first counter-check assumed a damaged wall falls back. The Beating rule only requires HP > 0, so the check now freezes the wall, which the rule does refuse.

**Remaining.** The town `Floor` is now the dominant gap: about 8,400 cells across Tine, Sumphold, the Drowned Ledger, the Last Counter, Marrowstye, Quillhold and Tally. It needs its own paving art. After that come Duckboard, Reeds, PeatBank, the authored Gantry and Quillhold walls, campfire, oven and well markers, and small props.

## Limits and next

- **Live play.** Not yet captured. Reaching a settlement from a new game needs world-map travel and `>`. Computer-use key events reach Unity without a held Shift, so `>` was never received, and typing it started the run command. A Shift+Period injection made while Unity was frontmost was not received either. Next session: inject `>` with the Input System while Unity is frontmost and the run is idle, or earn a settlement checkpoint.
- **Remaining glyphs in settlements:**
  - `Floor` (seven towns, about 8,400 cells), which needs its own town-paving art;
  - `WaterPuddle` (about 2,300);
  - `SandstoneWall` (about 900);
  - DryBrush, Rock, Bush and Tree;
  - hearth and well ground markers;
  - Duckboard, Reeds and PeatBank;
  - authored walls such as `GantryTimberWall` and `QuillholdArchiveWall`.

  The foreground owners can reuse approved Beating, Spread and Fen art next.
- No packaged build includes this change.
