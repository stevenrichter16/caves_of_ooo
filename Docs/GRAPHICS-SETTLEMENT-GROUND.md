# Settlement ground

**Status (2026-10-29):** implemented and regression-tested; verified through offline renders of real generated settlements drawn by the real presentation catalog. Not yet captured in live play (see Limits).

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
