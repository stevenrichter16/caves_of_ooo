# Settlement ground

**Status (2026-10-29):** four slices, all implemented:
1. ground;
2. water and sandstone walls;
3. original town-floor art;
4. plants and boardwalks.

All four and regression-tested; verified through offline renders of real generated settlements drawn by the real presentation catalog. Not yet captured in live play (see Limits).

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

## Slice 3: original town floor (2026-10-29)

**Problem.** After slice 2, the plain `Floor` owner was the dominant settlement gap: 8,378 cells across seven towns drew as black fields of dark-grey `.` glyphs. No approved material fit it:
- the cellar floor reads as a cave void;
- the Sill grey already marks room interiors;
- lawn and sand would misstate the owner.

**Art.** A new original material, packed town earth: `Sprites/Live16x24/town-floor` (64×24, four frames). It is built from editable Blender quads by `Tools/Live16x24/build_town_floor.py`. Source, checks and limits: `ArtSource/Live16x24/town-floor/README.md`. No imagegen concept was available in this session.

**Runtime.** `FlatPixelSettlementGroundCatalog` admits the canonical `Floor` as a ground owner: `.` `&K`, no background, the standard glyph variants, terrain without material, thermal or destructible state. It draws the four frames through the shared variant selector.
- Its ground kind is `StoneFloor`, the kind the greybox already gives a plain Floor, so tints and other receivers are unchanged.
- The 5-texture constructor became 6-texture. Its only caller is the Resources constructor.

**Tests.** Six new biome-art cases: Tine, Sumphold, the Drowned Ledger, the Last Counter, Quillhold and Tally.
- RED: 6 failures, each 0 of N native.
- GREEN and regression: all `FlatPixel*` classes 3,736 passed, 0 failed, 3 inconclusive (existing allocation probes).

**Result.** Native cells per town:

| Town | Before | After |
|---|---|---|
| Tine | 822 | 1,844 |
| Sumphold | 608 | 1,878 |
| the Drowned Ledger | 588 | 1,870 |
| the Last Counter | 412 | 1,958 |
| Marrowstye | 972 | 1,940 |
| Quillhold | 557 | 1,804 |
| Tally | 943 | 1,986 |

The 15 settlements now total about 28,879 of 30,000 native cells (96%), up from 1,111 before slice 1.

**Evidence.** `settlement-town-floor-{tine,last-counter,quillhold,drowned-ledger,tally}-offline.png`: before (top), after (bottom). They are offline renders through the real catalog, not gameplay captures.

**New visible defect.** Remaining glyph owners on the new floor now show as black boxes, which were invisible against the old black field: DryBrush in the Last Counter, Reeds and Duckboard in Tine, campfire markers, and a magenta `+`. This is the next slice. DryBrush and Duckboard have approved Beating and Sodden art to reuse.

## Slice 4: plants and boardwalks on settlement ground (2026-10-29)

**Problem.** After the town floor, some owners still drew as glyphs on black boxes over native ground. Seed-64 census:

| Owner | Cells |
|---|---|
| Duckboard | 183 |
| Bush | 83 |
| Reeds | 71 |
| DryBrush | 68 |
| PeatBank | 65 |
| Tree | 31 (glyph only) |

**Change.** Each owner reuses approved art and the exact eligibility rule of the biome that already draws it. Each rule is exposed as a small internal static, so admission cannot drift.

| Owner | Rule | Art |
|---|---|---|
| Bush, Tree | `FlatPixelSpreadEnvironmentCatalog.IsEligiblePlant` (Spread) | `opening-base` frame 5; `north-vegetation` frame 0 |
| DryBrush | `FlatPixelBeatingMaterialsCatalog.IsEligibleDryBrush` (Beating) | `north-vegetation` frame 3 |
| Duckboard, Reeds, PeatBank | A Sodden travel instance with settlement admission, via a new internal constructor | Sodden's own connectivity masks and frames |

`FlatPixelSoddenTravelCatalog.IsSettlementFixture` lets the ground resolver recognise eligible boards and reeds.

**Ground beneath.** Eligible Bush, DryBrush, Duckboard and Reeds no longer compete with the settlement ground, so their see-through art shows the real ground beneath instead of a black box. Ineligible ones still compete, as before. The public Sodden, Spread and Beating behaviour is unchanged; their suites are in the regression run.

**Tests.**
- Seven new cases: Cinderhold Bush and Tree; DryBrush in Wellmeet and the Last Counter; Tine Duckboard; Drowned Ledger Reeds; Sumphold PeatBank. Each requires at least 90% native with the expected atlas, real ground beneath for see-through owners, and a matching reader texture.
- One counter-check: a destroyed Bush (HP 0) and a frozen Duckboard keep their fallback.
- **RED:** 7 real failures plus 2 bridge-noise failures.
- **GREEN and regression:** all `FlatPixel*` classes 3,744 passed, 0 failed, 3 inconclusive.

**Result.** Per-settlement native cells, e.g. Tine 1,844 → 1,978, Sumphold 1,878 → 1,979, the Drowned Ledger 1,870 → 1,976, Cinderhold 1,929 → 1,984. Census file: `settlement-plants-boards-census.txt`.

**Evidence.** `settlement-plants-boards-{tine,cinderhold,sumphold,last-counter,wellmeet}-offline.png`. These are offline renders, not gameplay captures.

**Remaining in settlements.**
- Campfire and campfire-marker cells (red cross on black).
- Authored Quillhold archive and Gantry timber walls.
- Settlement NPC actors (Innkeeper, Quartermaster, Scribe, Warden, Villager), which are still smiley glyphs in every settlement.
- Shrine, oven, lantern, door and stair glyphs.

## Slice 5: original settlement keepers (2026-10-29)

**Problem.** In every settlement, the Innkeeper (`&B`), Quartermaster (`&Y`) and Scribe (`&M`) were still coloured `@` glyphs, standing beside Villagers, Merchants, Tinkers and Elders who already had native bodies.

**Art.** Three original 16×24 bodies in the approved right-facing cast style, built from editable Blender quads by `Tools/Live16x24/build_settlement_keepers.py`, which is modelled on the shared-specialists builder:
- the Innkeeper has a blue vest and a white apron;
- the Quartermaster has an ochre coat, brass buttons and a crossbody satchel;
- the Scribe has a plum robe, a quill and a scroll.

Source and contact sheet are in `ArtSource/Live16x24/settlement-keepers/`. `--verify` checks every native pixel and rejects six corruptions. `--inspect-blend` reopens the saved blend: 412 faces, exact rerender. The atlas is published as `Sprites/Live16x24/settlement-keepers` (48×24), with its own GUID.

**Runtime.** `FlatPixelSharedResidentsCatalog` adds the three roles under its existing exact rule: canonical `@` and colour, Trader and Conversation, body, brain, no foreign meaning. They are portable residents like the others.

The Warden is deliberately excluded. Claiming it in the shared catalog would reroute the opening's own Warden art.

**Tests.**
- Six keeper cases across Posy, Tine, Wellmeet and Tally.
- Counter-checks: a recoloured Innkeeper, and a Scribe without trade, keep the glyph.
- **Revised pin:** `OtherResidentsAndSourceBlueprintAliasesDoNotAcquireTheseBodiesAndFarmerStaysUnchanged` used the Innkeeper as its example of a resident with no role. It now uses the Warden, and adds a check that an Innkeeper aliased to Villager never borrows the villager body.
- **RED:** 5 real failures plus 2 bridge-noise failures.
- **GREEN:** the residents, settlement and civic classes, 146 of 146.
- **Broader run** (FlatPixel, CivicQuartet and Resident classes): 3,976 passed. Of the two failures, one was the pin revised above. The other, `SpreadEverydayResidentTests` purse 8 vs 10, fails identically on committed sources.

**Evidence.** `settlement-keepers-{posy,tine}-offline.png`: before (top), after (bottom), enlarged crops. These are offline renders.

## Live play verification and slice 6: settlement memory (2026-10-30)

**Live route.** The immutable East-kitchen fixture `current-scene-earned-kitchen224401440` (Overworld.12.11.0, turn 3630, HP 40) was continued in the Editor's isolated launcher with a real C key. Real arrow keys then walked south across the zone edge into Marrowstye (12.12.0): east 4, south 6, west 2, south 19. In town: east 9, south 11 to the crossroads at 46,11; south 1, east 14, south 6 into the southeast house at 60,18. This needs no `>`, which computer-use cannot deliver. The same route was replayed after the fix and reached the same positions and turns.

**Seen in actual play** (`settlement-live-01…03`):
- the sandstone brick walls;
- the original town floor, the opening road and the stone room floors;
- the Scribe, Quartermaster and Innkeeper bodies, standing beside the shared Villager, Merchant, Elder and Children bodies.

No `@` resident glyph remained in the house.

**Live-found defect: remembered settlement ground.** Explored settlement ground and walls fell back to dark glyph speckle once out of sight (`settlement-live-04…`, top).

**Fix.** `FlatPixelSettlementGroundCatalog.TryResolveRemembered` follows Sill's static-memory policy:
- It applies only to the fallback-selected remembered owner, judged by static identity: a plain ground owner, or a SandstoneWall with its `#`, Wall tag, destructible part and Stone material.
- It draws the same frame shown when the cell is seen, under the single 35% memory dim.
- It reads no tile state, wall condition or neighbour. Water is live state, so it is never kept.
- It grants no reader or collision authority.

The greybox consults it only where Sill's own memory does not apply.

**Tests.**
- New: four memory cases (town floor, lawn, road and wall: same frame, dimmed, no glyph over ground, no native reader portrait) and one counter-check (unexplored ground stays undisclosed; remembered water is not kept).
- Two deliberate revisions:
  - The settlement "hidden" check now requires the memory dim instead of a fallback glyph.
  - `FlatPixelSillMemoryTests.MemoryRequiresExploredCurrentExactSillGraph` used Gantry as a village with no memory. It now expects Gantry's own settlement memory (lawn frames 0–3, memory tint) and checks that Sill's adapter never admits Gantry. The renamed-Sill case still falls back.
- The first GREEN run also showed that "no reader authority" was too strong: a remembered cell keeps the existing fallback portrait. The assertion now forbids only a native portrait from memory.
- **RED:** 4 failures.
- **GREEN:** all `FlatPixel*` classes 3,756 passed, 0 failed, 3 inconclusive (existing probes).

**Live after.** On the same route, remembered floors, roads and walls stayed as dim native material instead of speckle (`settlement-live-04…`, bottom). A few remembered bushes still show glyphs; plant memory is not in this slice.

**Honesty bounds.**
- Verified by script: positions, turns and HP from the probe.
- Verified visually: the four captures, at the Editor's native 1× 1280×800.
- Not verified: F5 save and fresh Continue (no state-changing claim was made), packaged builds and Deck.

## Slice 7: the settlement hearth (2026-10-30)

**Problem.** Every settlement centre showed a red `*` campfire and four red `.` scorched-earth markers on black boxes.

**Semantics.** An ordinary settlement campfire (`CampfirePart.FiniteCooking == false`) is a permanent lit hearth whose glyph flickers between red and yellow (`CampfirePart.HandleRender`). Its truthful approved image is therefore the opening's burning campfire, `opening-states` frame 5. Finite-cooking fires have real cold, hot and burning states, which this adapter does not read, so they keep their fallback.

**Art.** The markers use a new original scorched-earth stain (`ArtSource/Live16x24/campfire-scorch`, two frames, transparent so the real ground shows). The first draft read as a small object and was flattened.

**Runtime.**
- The hearth requires the canonical `*` `&R` terrain with its own CampfirePart, heat and fuel.
- The marker requires the canonical red `.` terrain with nothing else.
- Both stand on the real settlement ground instead of competing with it.

**Tests.**
- Three hearth cases (Tally, Wellmeet, Marrowstye): burning frame, ground beneath, reader, and at least 80% of the ring scorched on ground.
- One counter-check: a finite-cooking fire and a recoloured marker keep their fallback.
- **RED:** 4 failures.
- **GREEN:** all `FlatPixel*` classes 3,760 passed, 0 failed, 3 inconclusive.

**Live.** The same fixture route was taken to the Marrowstye hearth at 78,2, with the player at 75,2, turn 4340, HP 40 (`settlement-live-05/06`). The hearth burns on its stone ring, with four stains on the town ground.

## Slice 8: the settlement Warden (2026-10-30)

**Problem.** Wardens in nine settlements were still red `@` glyphs. The opening's Warden is the same `Warden` blueprint and has an approved body: `opening-base` frame 9.

**Routing decision.** The body is drawn by `FlatPixelSettlementGroundCatalog`, which admits only settlement zones. It is not added to the portable shared-residents catalog, because claiming the role there would reroute the opening's own Warden path.

**Rule.**
- canonical red `@` on the actor layer, with no visual override;
- its own body, brain, `AIGuardPart` and conversation;
- alive, untinted and single-cell.

A damage-flash tint therefore briefly keeps the glyph, as the other actor adapters do.

**Tests.**
- Three Warden cases: Tine, Tally and Wellmeet.
- One counter-check: a recoloured or dead Warden keeps the glyph.
- **RED:** 3 failures.
- **GREEN:** all `FlatPixel*` classes 3,764 passed, 0 failed, 3 inconclusive.

**Evidence.** `settlement-warden-tally-tine-offline.png`: before (left) and after (right), enlarged crops of the offline render. It is not a live capture, because the live route town Marrowstye has no Warden.

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
