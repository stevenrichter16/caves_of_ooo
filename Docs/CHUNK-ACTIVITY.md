# Chunk activity — waysites: things to do in every chunk

**Status:** WA1–WA4 and WA6 implemented 2026-10-31; WA5 (Overwrit) cut by canon (§3.2). See the
implementation log and §8 for results.

**User direction (2026-10-31):**
- Move back into filling out the chunks with environment and more significant tiles or
  things to do.
- Fill out chunk object density.
- Plan and implement it all.

This continues the standing chunk-population direction (`GRAPHICS-CHUNK-POPULATION.md`):
- purposeful groups, real mechanics, and no fake services or loot;
- deliberately empty places kept;
- cached chunks untouched.

## 1. Baseline (measured)

`ChunkActivityCensusBenchTests` (explicit) builds seed 64 as a fresh game does, with Spread
exploration enabled. It stands a player beside every visible owner of every surface zone and
asks for that owner's world actions, through the interact menu's own query (zone and actor
supplied; parts that need nearness answer honestly).

A **purposeful** verb is any verb other than look, examine, pick up and the structural verbs.
Break, haul, throw and door are structural: almost every solid or heavy thing offers them.

| Group | Zones | Purposeful owners per zone | Distinct purposeful verbs | Stations | Readables | Talkers | Zero-purposeful zones |
|---|---:|---:|---:|---:|---:|---:|---:|
| Opening (benchmark) | 5 | 9.0 | 6.2 | 1.2 | 1.4 | 1.0 | 0 |
| Spread wild | 124 | 4.0 | 2.8 | 0.2 | 0.3 | 0.2 | 0 |
| Sodden wild | 59 | 4.2 | 1.9 | 0.0 | 0.0 | 0.1 | 0 |
| Beating wild | 101 | 7.5 | 3.5 | 0.3 | 0.4 | 1.0 | 0 |
| Stump wild | 19 | 7.9 | 2.8 | 0.0 | 0.0 | 0.2 | 0 |
| Grovelands wild | 38 | 10.1 | 3.3 | 0.4 | 0.0 | 0.8 | 0 |
| Overwrit wild | 23 | 0.6 | 0.7 | 0.0 | 0.0 | 0.0 | 17 |

**What a wild chunk offers today.** Open, Harvest, Unlock and Chat.
- **Draw water:** only at Beating wells and Grovelands seeps.
- **Rest, Sleep, Read and Cook:** essentially nowhere in the wild.
- **The Spread**, a third of the world: berry bushes, crates, sacks and strongboxes, with a
  farmstead or river shrine in some zones.
- **The Overwrit:** 17 of 23 zones offer nothing, and several have no owners at all.

**The census undercounted twice.** Both errors are now fixed:
- a detached manager defaults to the legacy layout (no Spread exploration);
- wells refuse an actor who is not beside them.

## 2. Verification sweep — the mechanics a waysite can rely on

| Verb | Owner and part | What it really does | Confirmed |
|---|---|---|---|
| Rest, rest until the next band | `Campfire` (`CampfirePart`) | passes time and recovers (`RestSystem`); cooking through `CampfireWorkActions` | `CampfirePart.cs:49-53` |
| Sleep, sleep until the next band | `Bed` (`BedPart`) | player bed rest (`PlayerBedService`); an Owner restricts it | `BedPart.cs:25-26,48` |
| Draw water | `Well` (`WellPart`, an optional finite pool) | drinking cures Parched (the Beating's exposure), with a finite pool decrement; elsewhere it is flavour | `WellDrinkingService.cs` |
| Read | any blueprint with `ReadableDocumentPart` | catalogued text shown; sets `document_read.<id>`, which concerns can ask for; one blueprint per document | `ReadableDocumentPart.cs`, `ReadableDocumentCatalog.cs` |
| Harvest | `HarvestablePart` | finite yield, then the source is removed | `HarvestablePart.cs` |
| Open, Unlock | `ContainerPart`, `LockPart` | stocked by `LootStocker` tables | `LandmarkBuilder.Apply` |
| Donate | `RiverShrine` (`SanctuaryPart`) | 5 drams for 150 turns of damage reduction | chunk-gameplay audit |
| Chat | NPCs with `ConversationPart` | | |
| (signpost) | `Signpost` (`RegionalSignpostPart`) | carved directions to up to four villages on examine | `RegionalSignpostPart.cs` |

**Placement substrate.** `LandmarkBuilder`:
- a stamp's legend places blueprints, stocked chests and locked chests, spawns and
  interior cells;
- `FootprintClear` refuses reserved cells, stairs and liquid, and solid cells unless the
  stamp clears vegetation;
- at most two ambient stamps per zone, each rolling its own chance (the Spread's river shrine
  is 20%).

Cell reservation (`GenReservedCells`) is how composition builders protect roads and portals.

## 3. Design — waysites

A **waysite** is a small authored situation stamped into a fresh wild chunk. Each one ties a
local need to an action and a payoff, using only the mechanics above, and leaves a readable
trace that says why it is there.
- It is not loot confetti. Every waysite answers why someone would stop here.
- It is biome-true. Its material and text come from canon (`Lore/History/08_MaterialCulture.md`,
  `02_Geography.md`).

### 3.1 Selection, budget, variety

`WaysiteBuilder(biome, tier, worldSeed)` runs at priority 3850:
- after the ambient landmarks (3800), so authored set pieces keep first claim;
- before hazards (3900), population (4000) and containers (4100), so creatures and caches
  arrange around the site.

**Budget.** Each biome has a target number of sites per wild chunk:

| Biome | Sites per chunk |
|---|---|
| Spread | 2 |
| Sodden | 2 |
| Beating | 1 (it already has wells and camps) |
| Stump | 1 |
| Grovelands | 1 |
| Overwrit | 0: canon makes its emptiness the point (`Lore/Design/THE-OVERWRIT.md` §II) |

**Selection.** It is deterministic per world seed and zone (FNV with an avalanche, as
`SpreadWildernessSituationPlan` does), a weighted draw without repetition inside a zone.
- **Neighbour variety:** each palette has one world-wide assignment of first kinds
  (`WaysitePlan.FirstKinds`), chosen in raster order so that a zone's first kind is never its
  west neighbour's and, with three or more kinds, never its north neighbour's either. The rest
  of a zone's order is a weighted draw.
- The assignment is a pure function of the seed and the palette, computed once and cached, and
  it never reads a generated neighbour, so generation order cannot change it.
- **Superseded:** the first design penalised a kind drawn by the neighbour. That was measured
  and failed: the penalty targeted the neighbour's unadjusted draw, but the neighbour's real
  first kind was itself adjusted, so 51 of 342 pairs still repeated at /4 and 60 at /16.

**Atomic placement.** A site places all of its owners or none.
- The footprint must pass `FootprintClear`.
- Reachability, the cells reachable from the zone's edge-crossing cells, must not drop by
  more than the site's own solid cells, and the reachable area must not split into more
  pieces. The piece count catches a road cut into two halves that each still touch an edge,
  which the cell count alone accepts.
- A waysite never clears vegetation: a recorded placement cannot fell trees and put them back,
  so it takes open ground or none.
- **Open ground:** every footprint cell holds its ground and nothing else. Passability alone let
  a trough stand in reeds, a berry bush on a crop row and a signpost in a hedge gap (found by
  `WaysitePresentationTests`).
- On failure the site's owners are removed and its reservations released, and the
  builder tries another anchor and then another kind.

**Scope.**
- Wild zones only (no POI). Places, sites, lairs and the opening are untouched.
- Fresh generation only: cached zones are never retrofitted.

**Diag.** `worldgen/WaysitePlaced` {zone, kind, ax, ay, owners} and
`worldgen/WaysiteRefused` {zone, kind, reason}.

### 3.2 Waysite kinds (first palette)

All placed owners are real.

**Spread** (farm country, the recovered world; ordinary people, ordinary reasons):

| Kind | Owners | Verbs | Need → payoff |
|---|---|---|---|
| Hedge camp | Campfire, Bedroll (Bed), camp sack (Sack with travel food), a drover's note | Rest, Sleep, Open, Read | hurt far from town → recover; supplies |
| Drovers' trough | stone trough (Well, spring-fed and endless), hitching post | Draw water | fill vessels for crops and fire; cures Parched for travellers heading to the Beating |
| Bee orchard | Beehive, 2–3 BerryBush | Harvest | honey and berries (food, reagents) |
| Fallen cart | overturned cart (haulable), spilled sacks, a locked strongbox, a carriage slip | Open, Unlock, Haul, Read | goods; the slip names the carter and the cargo |
| Boundary stone | Signpost, a token-dish of carved river-tokens | Read (directions), Open | find the way; a small offering |

**Sodden** (bog country; peat-cutters, boats, the Bog-Taken):

| Kind | Owners | Verbs |
|---|---|---|
| Peat-cutter's bothy | Bed, Chair, a cutter's basket (WovenBasket), a tally note | Sleep, Sit, Open, Read |
| Dry hummock camp | Campfire, Bedroll, a camp basket | Rest, Sleep, Open |
| Duckboard landing | a boat-hook post, a FrogOil cache (WovenBasket), a ferryman's slate | Open, Read |

**Beating:**
- **Shade cistern:** a short sandstone wall over a cistern (Well), interior cells beneath it
  (`BeatingGlareSystem` exempts interior cells) and a note. Draw water cures Parched, but a
  cistern holds only `WaysiteCatalog.CisternDraws` (3) units of rain water (`WellPart.Stock`).
  Endless water in about half the wild Beating would undo the exposure that is the region's
  point.
- **Salt pan:** three PaleSaltVein (Harvest).

**Stump:**
- **Watcher's camp:** Campfire, Bedroll and an observation note.

**Grovelands:**
- **Gatherer's rest:** Bedroll, a basket and a gatherer's note. No fire: under the Choir's law
  fire costs standing.

**Overwrit:** none. Canon makes the region's emptiness the point ("no ruins where ruins should
be"), and what surfaces there does so only at thresholds, which a static placement would
contradict. Its 17 empty zones stay empty.

**Cut from the first design:**
- peat stack and salt rake: no harvest mechanic fits them that the bog and salt veins do not
  already provide;
- Unsaid stones and salvage drift: the Overwrit cut above.

### 3.3 Wayside documents

New catalogued documents, one blueprint each (the codex pattern), written in canon voice:
- three drovers' notes, two carriage slips, two peat-cutter's tallies, two ferryman's slates;
- two cistern notes, two observation notes, two gatherer's warnings.

Fifteen documents in all (`CodexDocuments.json`, 39 documents total), each with its own
blueprint and Sources line.

Their text is short (300–700 characters). It is about the place and ordinary life, and it
sometimes points at a mechanic: for example, "the trough holds clean water when the ditches
foul". Reading sets the fact, so later concerns can ask whether the player has read it.

### 3.4 Art and presentation

New fixed props (Bedroll, DroversTrough, Cistern, HitchingPost, OverturnedCart, TokenDish,
BoatHookPost) get flat 16×24 frames from the prop-icons generator, which
indexes any new non-takeable prop automatically. New notes are items and get item icons the
same way.

The previous (3D) presentation has no models for them. They join the documented
`PreviousPresentationTierGap` excuse, as P4 and P5 did.

## 4. Success criteria (census, seed 64, fresh game)

| Biome (wild) | Purposeful owners | Purposeful verbs | Stations | Readables |
|---|---|---|---|---|
| Spread | 4.0 → ≥ 7 | 2.8 → ≥ 4.5 | 0.2 → ≥ 1.0 | 0.3 → ≥ 0.8 |
| Sodden | 4.2 → ≥ 7 | 1.9 → ≥ 4 | 0 → ≥ 1 | 0 → ≥ 0.6 |
| Beating, Stump, Grovelands | | each gains ≥ 1 | each gains Rest or Read where it had none | |
| Overwrit | | | | |

For the Overwrit: unchanged by canon (§3.2). The first draft's target (17 empty zones down to
12) is withdrawn.

**Integrity.**
- Every placed waysite leaves every zone edge crossing reachable.
- No waysite overlaps a road, portal, stairs, liquid or reserved cell.
- Cached zones are byte-unchanged.
- Generation-time cost stays within 5% of the current pipeline.

## 5. Sub-milestones (smallest blast radius first)

1. **WA1 — framework.** `WaysiteBuilder`:
   - deterministic selection, budget and neighbour variety;
   - atomic placement with reachability rollback, using `LandmarkBuilder`'s footprint and
     apply;
   - diag;
   - tests with existing blueprints only.
2. **WA2 — Spread.** Bedroll, trough, hitching post, cart and token-dish blueprints; five
   Spread kinds; Spread wayside documents; flat art; census delta.
3. **WA3 — Sodden.** Three kinds, their documents and art.
4. **WA4 — Beating, Stump, Grovelands.** One kind each where it adds a missing verb.
5. **WA5 — Overwrit.** Cut by canon (§3.2).
6. **WA6 — Close.** Census, an independent review of the content and placement,
   adversarial sweep, living doc.

Each runs RED → GREEN → counter-check → adversarial → review → doc → commit.

## 6. Performance

Generation-time only: one deterministic selection and at most a few footprint attempts,
each followed by one breadth-first reachability pass over 80×25 cells. No per-frame or
per-turn work and no hot-path allocation.

One cache: `WaysitePlan.FirstKinds`, keyed by (seed, palette instance, tier). A miss computes
one world-sized table (a few hundred weighted draws), and the shipped palettes are static
instances, so a session holds at most one table per palette and tier for each world seed it
generates.

## 7. Honesty bounds

- The census measures what is offered, not whether a player enjoys it. Pacing and feel
  need play.
- Waysites add situations, not systems. Rest, sleep and drink are the existing services.
  Their value depends on existing balance (healing rates, Parched), which this work does
  not retune.

## 8. Results (census, seed 64, fresh game with exploration)

**Census filter fix.** A campfire inherits `Terrain`, and the first census dropped every
Terrain owner, so it never counted a campfire (or a Rest verb) anywhere. Terrain owners that
are stations now count.

The comparison below uses the corrected census on the same code, once with waysites switched
off (one mutation of the wiring) and once with them on.

| Biome (wild) | Purposeful owners | Purposeful verbs | Stations | Readables | Owners | Criterion |
|---|---|---|---|---|---|---|
| Spread (124) | 4.0 → 8.3 | 2.8 → 6.3 | 0.7 → 2.2 | 0.3 → 1.4 | 111 → 117 | met |
| Sodden (59) | 4.3 → 8.6 | 2.1 → 6.7 | 0.6 → 2.6 | 0 → 1.1 | 397 → 403 | met |
| Beating (101) | 8.0 → 9.9 | 4.4 → 5.4 | 1.3 → 1.8 | 0.4 → 0.9 | 88 → 91 | met |
| Stump (19) | 9.7 → 11.5 | 3.3 → 7.6 | 2.4 → 4.3 | 0 → 0.9 | 320 → 322 | met |
| Grovelands (38) | 10.3 → 11.9 | 3.6 → 6.3 | 0.9 → 1.8 | 0 → 0.9 | 296 → 298 | met |
| Overwrit (23) | 0.6 → 0.6 | 0.7 → 0.7 | 0.3 → 0.3 | 0 → 0 | 5 → 5 | unchanged by canon |
| Opening (5, control) | 9.2 → 9.2 | 6.6 → 6.6 | 2.2 → 2.2 | 1.4 → 1.4 | 91 → 91 | untouched |

**What a wild chunk now offers.**
- **Spread:** Sleep in 62 of 124 zones, Rest in 61, Draw water in 55, Read in 89, plus the
  existing Harvest and Open.
- **Sodden:** Sleep 56/59, Read 55, Rest 45, Sit 33.
- **Beating:** Draw water at 63 of 101 (49 new cisterns, three draws each).
- **Grovelands:** Sleep and Read in 35 of 38.
- **Stump:** Rest, Sleep and Read in 18 of 19.

**Object density did not move** (owners per Spread zone 111 → 117). Waysites add purpose, not
mass. The owner distribution is bimodal: 70 of 124 Spread wild zones and 52 of 101 Beating
ones have fewer than 60 visible owners in 2,000 cells, against the opening's 90. That is the
next milestone (§9).

### 8.1 Integration findings and fixes (2026-10-31)

Whole-chunk renders (offline, through the real `FlatPixelGreyboxCatalog`) and a new test,
`WaysitePresentationTests`, found the following. The test resolves every owner of every real
waysite in 24 zones per biome and requires native art on the real ground (`LocalGround`).

| Finding | Fix |
|---|---|
| Spread signposts (boundary stone) drew as an `I` glyph. The prop atlas skips blueprints a catalog already names, and only the opening and the Beating drew signposts. | `FlatPixelSpreadEnvironmentCatalog` admits the canonical signpost to the opening's approved frame (opening-anchors 8), the same frame the Beating already borrows (rank 2b). `FlatPixelSpreadSignpostTests`: 1 positive, 9 mutation counter-checks, 1 consistency pin. |
| Stump watcher's-camp fires burned on black. The Stump ground resolver was the one biome resolver without the ordinary-hearth exemption. | The exemption, mirroring the other four. `FlatPixelWildFaunaTests` gains the Stump case. |
| Owners stacked on passable objects: trough in reeds, berry bush on a crop row, signpost in a hedge gap. | The open-ground rule (§3.1). |
| Wild cisterns gave endless water. | `WellPart.Stock`; drinking and waterskin filling both spend it, both undoable through the inventory transaction. |
| First test check `GroundTile != null` passed a black box: a black-tinted glyph backing is a ground tile too. | The test requires `LocalGround`. |

**Verification-sweep correction:** the Beating does not have its own signpost art; it borrows
the opening's frame. The planned "the Beating keeps its own signpost" test was a false premise
and became a consistency pin.

**Pre-existing gaps seen in the same renders (not waysite owners; queued):** Spread `CropRow` /
`RipeCropRow` outside the opening are glyphs (213 cells in one FieldStrips chunk). In the
Sodden, `DryBrush`, `CopperPipe`, `PeatBog` and `Sack` are glyphs. Elsewhere: `BrinePool` in the
Spread and Stump, `StairsDown` outside the opening, `AshBed`, `WoodenBarrel`, `FallenBeam`,
`MirrorMucilagePool` and `SpreadSeedKeeper`. Wild Beating crops draw on a dark-olive backing.

### 8.2 Adversarial sweep and mutations

`WaysiteAdversarialTests`: 41 tests across selection (malformed zone IDs, null palettes, tier
and weight boundaries, world-wide neighbour variety, determinism across generation order),
builder boundaries, rollback fidelity, notes, diag, shipped content and canon, and real
generated chunks.

Two confirmed bugs, RED → GREEN:
- a kind with a null legend or null notes threw;
- a vegetation-clearing stamp built its owners into trees.

The third failure was a test error: `Diag.ResetAll` restores the default channels.

Mutations, each run in the verification clone; every one turned its probe RED:

| Mutation | Caught by |
|---|---|
| Drop the piece-count route check | `SplittingARoadIntoTwoEdgeTouchingHalves` |
| Drop the whole route check | `EnclosingAPocket`, `Splitting…`, `ARefusedKind_FallsThroughToTheNext` |
| Undo leaves interior set | `AnUndoneSite_RestoresInteriorAndReservationsExactly` |
| First kinds ignore neighbours | `FirstKinds_NeverRepeatTheWestNeighbour` ×3 (89 / 131 / 195 repeats) |
| Wiring ignores eligibility | `TheOpeningAndPlacesGenerateWithoutSites` |
| Resolve keeps `ClearsVegetation` | `AVegetationClearingStamp_NeverBuildsIntoATree` |
| Drop the incomplete-site undo | `ASpawnTheTierFloorRefuses_UndoesTheWholeSite` |

## 9. Next milestone — chunk density (planned, not started)

**Evidence.** The census owner counts are bimodal:

| Biome (wild) | Zones under 60 owners | Median owners | Opening |
|---|---:|---:|---:|
| Spread | 70 / 124 | 56 | 90 |
| Beating | 52 / 101 | 58 | — |
| Sodden | 7 / 59 | 455 | — |
| Stump | 7 / 19 | 479 | — |

Whole-chunk renders of the sparse cases:
- **Spread FieldStrips, FlowerMeadow and OldRoad:** wide uniform grass. The only structure is
  two shelter masses (trees and bushes) in opposite corners. The opening reads rich because of
  structure (hedge lines, tree groves, tuft skirts, water, a ruin), not scatter.
- **Beating CaravanRoad, SaltPan and RuinField:** open sand. RuinField's ruins are single wall
  tiles that do not read as ruins.
- **Sodden open mire and bare Stump ledges:** read as their formations intend; not targets.

**Direction.** Structure, not confetti. A post-composition builder for the sparse formations
only, before connectivity repair (3000) so routes are re-proven:
- **Spread:** field-margin hedge fragments on parcel edges; lone field trees with bush skirts
  at margin corners; tuft drifts along lanes. Approved art only: Tree, Bush, Hedge,
  CharmFlowers.
- **Beating:** low rock outcrops with dry brush in their lee, the WindBarrens grammar applied
  sparsely; RuinField fragments completed into readable corner ruins.

**Constraints.**
- Never in the opening, places or sites, or where an exploration family or situation owns
  the chunk's cover (captured hedges and trees).
- Determinism and no pipeline-RNG use, as for waysites.
- Pins checked deliberately.

**Success:** sparse-formation median owners at least 80, with no empty quarter-chunk; routes
proven; whole-chunk before/after renders reviewed.

## Implementation log

- 2026-10-31 — census bench (`ChunkActivityCensusBenchTests`); baseline above; design
  written.
