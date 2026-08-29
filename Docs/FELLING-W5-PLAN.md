# W5 — The vertical world: sinkholes, descents, catacombs

> Plan-to-disk per CLAUDE.md. Design source:
> `Docs/FELLING-WORLD-DESIGN.md` §4 (the sinkhole stack), §6 (light as
> terrain), §7.5 (Wall-Catching). Canon: `catacomb_village_design.md`
> (514 lines — "if a question is answered there, it is answered"),
> `Lore/Factions/06_CatacombVillagers.md`. Sibling precedent:
> `Docs/FELLING-W4-PLAN.md` — the arc this mirrors, including its
> review cadence, its cuts, and its close-out shape.

## 0. Scope

Canon's spatial grammar is vertical and the engine already speaks
part of it. W5 makes a hole in the world you can climb into: a
**Sinkhole POI** whose z-levels route to bespoke pipelines — the
Mouth you find, the Descent you survive, the Floor that is a
different place each time. Plus the two things the dark needs to be
worth entering: an authored light ladder, and one archetype built
end-to-end rather than eight built halfway.

## 1. Verification sweep — corrections table

Every row read at source before planning, per house rule.

| Plan/design claim | Verified reality | Evidence |
|---|---|---|
| "The multi-zone vertical recipe is proven" | **HALF TRUE — and the half that is false is W5's core work.** `GetPipelineForZone` returns `CreateUndergroundPipeline(wz)` for ANY `wz > 0` **before it ever reads the POI**. POIs are consulted only at z=0. A sinkhole's Descent and Floor would silently generate as generic caves. W5.1's first job is making POI identity reach every z-level. | OverworldZoneManager.cs:42-49 |
| "`Overworld.X.Y.Z` z-levels + stairs registry exist" | TRUE. `ZoneConnection {Source/Target ZoneID+X+Y, Type}`, `ZoneManager.RegisterConnection`, StairsUp/Down builders, `StairConnectorBuilder`, and `WorldMap.GetZoneBelow`. `CaveEntranceBuilder` is the working surface→z1 precedent: place StairsDown, register the connection. | ZoneManager.cs:10-17, CaveEntranceBuilder.cs:41-60 |
| "Sinkhole POIType" | Enum has Village/Lair/MerchantCamp/RiverChunk. Append-only add, same shape as the Formation enum's appends. Note `PointOfInterest` now also carries `Profile` (W4.6a) — the floor archetype can ride that field rather than inventing a parallel one. | PointOfInterest.cs:3-15 |
| "Authored AmbientLevels (C14)" | **The hook is already built and waiting.** `GetDepthAmbient(depth)` exists, returns the flat historical value, and its docstring says outright: "W5 (catacombs) replaces the body with the authored ladder … and that phase must not also be inventing where the number comes from." W0.2 also recorded the two constraints W5 inherits. | OverworldZoneManager.cs:663-685 |
| "RememberedColor guard (C15)" | Real and load-bearing: remembered cells render at a flat 0.2 grey, unmodulated. An ambient BELOW 0.2 makes visible cells darker than remembered ones — so the design's 0.12 catacomb and 0.02 dead-zone values **invert the fog-of-war** unless the remembered floor moves with them. The design doc does not mention this; it is a real constraint, recorded here. | OverworldZoneManager.cs:677-681 |
| "Plaque-walls (KnowledgePart at scale)" | `KnowledgePart` exists (FactBag-backed, ISaveSerializable, per-entity topics). It is a KNOWING tracker, not a readable-text part — plaque *text* wants the Examinable/readable path W3's Bog-Taken bodies used; KnowledgePart is what records that you read one. Both, not either. | KnowledgePart.cs:11-17 |
| "CatacombFolk faction" | TRUE — registered (W0.4), unused by content so far. W5 is its first employer. | Factions.json:113 |
| Sinkhole cells are free | TRUE and already reserved in prose: Olderdeep (4,6), the Deepest Cathedral (5,4), Lampwell (12,3), Spivenor (16,4) "are reserved but NOT placed here — they need POI types and zone stacks that W5/W6/W7 build. Their cells are already the right biome." W4.7 also added `AuthoredWildernessZoneIDs`, the reservation list these should join so an opportunistic POI cannot take a mouth cell. | WorldMapAuthoring.cs:189-196 |
| "Bio-light sources are LightSourcePart" | TRUE — that is how light already works (MycelialColumn in W4.1 is the shipped precedent, radius 3). No new light substrate needed; the work is content + the ambient ladder. | W4.1 log |
| Underground content to build on | `CreateUndergroundPipeline` ships SolidEarth/Strata/Connectivity/Stairs/StairConnector + `StampCatalog.Underground(depth)`, `PopulationTable.UndergroundTier(depth)`, hazard terrain, containers. A Floor pipeline can reuse most of this rather than starting bare. | OverworldZoneManager.cs:202-225 |

### 1a. Scope-prune with rationale

The milestone map's W5 row lists eight floor archetypes, five
catacomb-village archetypes, dead zones, eyeless apex predators, two
displacement mechanics, and two named places. That is three phases of
work. Cuts, with reasons:

- **CUT: five of eight floor archetypes** (Time-Locked Forest,
  Boneyard, Sealed Library, Pure Catacomb, Door). Ship **three**
  (Drowned Sima, Stranded Settlement, Choir Cathedral) built
  end-to-end. The archetype-roll machinery is the reusable part; a
  fourth archetype after that is content, not architecture. The Door
  (a wormhole into another sinkhole's floor) is *especially*
  deferred — it needs cross-map destination selection and its own
  save contract.
- **CUT: the five catacomb-village archetypes** → one village, built
  properly, as the Stranded Settlement floor. The canon doc is 514
  lines; five variants of a place nobody has visited yet is
  content-before-feedback.
- **CUT: dead zones + eyeless apex predators.** Dead zones are the
  0.02 ambient tier, which the C15 finding says needs the
  remembered-colour floor solved first. Both defer to W5's successor
  or a follow-on.
- **CUT: Being-Preserved** (Catchers' displacement). Wall-Catching
  alone proves the shared "status accumulates → you wake elsewhere"
  shape, and Wall-Catching is the one W4.5 already promised.
- **CUT: Lampwell + Spivenor as full places.** They become *placed
  sinkholes* (mouth + descent + floor) in W5.6 — named holes, not
  authored settlements. Their settlement content is W8's business.
- **KEPT deliberately: the ambient ladder.** Small in code, large in
  feel, and W0.2 built the hook specifically for this phase.

## 2. Content-readiness

- 🟢 z-level routing + stairs registry + connection persistence;
  `CaveEntranceBuilder` as the down-link precedent; underground
  builders (SolidEarth/Strata/Connectivity/Stairs); LightSourcePart;
  KnowledgePart; CatacombFolk faction; `GetDepthAmbient` hook;
  `FormationReachability` (fresh — W5's formations are its first new
  consumers); reserved mouth cells.
- 🟡 Floor archetypes, Mouth/Descent formations, catacomb village kit,
  plaque-wall readables, Wall-Catching effect — all designed in prose,
  none authored.
- 🔴 **The POI-at-depth routing gap** (sweep row 1) — the one true
  blocker, and W5.1's first commit.

## 3. Sub-milestones (smallest blast radius first)

**W5.1 — The stack.** `POIType.Sinkhole`; routing so POI identity
reaches every z (the sweep's 🔴); `CreateSinkholeMouthPipeline` (z=0:
surface biome + the lip — a void ringed by spray-fed green, with the
down-link registered), `CreateSinkholeDescentPipeline` (z=1: terrace
ledges, rope anchors, a cache ledge and the expedition that left it),
and a placeholder Floor that is still the generic underground
pipeline. Reserved mouth cells join `AuthoredWildernessZoneIDs`.
Tests: routing pins per z-level, the mouth's down-link resolves, a
non-sinkhole cave still routes generically (counter), reachability on
both new formations.

**W5.2 — The dark.** Fill `GetDepthAmbient` with the authored ladder
(surface 0.40 → floor 0.22 → catacomb 0.12). **Resolve C15 first**:
either move the remembered-cell floor with the ambient or clamp the
ladder above it — decided in-phase with a one-paragraph note, pinned
by a test that visible-cell brightness never falls below remembered.
Honesty bound: the *feel* of 0.12 is a look-pass question this phase
cannot answer headless.

**W5.3 — The Drowned Sima.** The first real floor: standing water,
semi-drowned green, its own population (gin frogs), reusing the
liquid + gas substrate W3 built. Proves archetype-roll-then-fix (the
archetype is chosen at worldgen from the sinkhole's own id — a pure
function like `IsBloomFront`, so it needs no save plumbing).

**W5.4 — The catacomb village** (Stranded Settlement floor). The kit
from canon: glow before people, the hearth-patch at the centre
(threatening it is war), the plaque-wall with its chiselled-out niche,
the Pebble-Sundew doormat that announces you. CatacombFolk's first
employment. Plaque readables + KnowledgePart recording what you read.

**W5.5 — The Choir Cathedral + Wall-Catching.** The substrate-grown
vault, and the displacement that closes W4.5's loop: falling into a
Cathedral sinkhole (or voluntary use) wakes you at another Cathedral
node, with a Wall-Bound counter that rises permanently. **This is
where the encasement offer stops being a spoken no.**

**W5.6 — Lampwell + Spivenor placed**, plus the mouths at Olderdeep
and the Deepest Cathedral if their floors fall out of the three
shipped archetypes.

**W5.7 — Close-out.** Both audit angles, hypothesis-driven RED pass,
adversarial sweep (the taxonomy will apply: save/load reach,
cross-zone state, anti-exploit on displacement), design-gate sweep,
honesty bounds, exit statement.

## 4. Performance / observability

Per-frame: nothing new expected — bio-light is LightSourcePart, which
the Wx review priced at ~0.1ms for a grove ring. **Watch item:** a
catacomb village is denser in light sources than a grove, and the
lightmap recompute is per-full-redraw; if a village floor carries
20+ patches, re-measure `COO.Zone.ComputeLightMap` before shipping
W5.4. Gen-time: two new formations use the fresh
`FormationReachability`; the Floor pipelines reuse the underground
stack. Diag: `worldmap/SinkholeRouted` (which pipeline a z-level
got), `worldgen/FloorArchetype`, `effect/WallCaught` with source and
destination node — every gate that rejects names its reason.

## 5. Risks

**R1 — The C15 inversion is a real blocker, not a nit.** Remembered
cells render at a flat 0.2; the design's 0.12 and 0.02 tiers put
visible ground *darker* than fog-of-war memory. W5.2 must resolve it
before any tier below 0.2 ships, or the dark reads as a bug.

**R2 — Falling must not be a save-scum lottery.** "Falling is cheap
and often lethal" is canon, but this is an RPG with recoverable
death, not a roguelike. The Descent's fall should cost — HP, gear
position, turns climbing back — and never delete a run. Decide the
fall's damage shape in W5.1 with the RPG framing explicit.

**R3 — Archetype-roll must be a pure function of the sinkhole's id,**
like `BloomFrontBuilder.IsBloomFront` — never the builder rng
(pipeline-retry reseeding) and never a saved field. The design says
"rolled at worldgen, then FIXED"; a pure function is fixed for free
and survives a load without a format bump (the W4.7 profile lesson).

**R4 — Wall-Catching is cross-zone teleportation, the most
save/load-hostile thing in the arc.** Its destination set, its
Wall-Bound counter, and its interaction with zone caching all need
pinning before it ships. Budget the adversarial sweep for W5.5
specifically.

**R5 — Depth tier vs. surface tier.** Design says "sinkhole floors =
surface tier +1" while `CreateUndergroundPipeline` computes
`depth/3 + 1`. Two formulas that will disagree. Pick one at W5.1 and
record which.

**R6 — Don't let the village become a second starting town.** The
catacomb village is a *place with a culture*, not a shop hub; the
Concord/trade reflexes from W2-W4 should stay out of it unless canon
puts them there.

## 6. Implementation log

_(filled per sub-milestone)_

### W5.1 — The stack (SHIPPED)

`POIType.Sinkhole` appended; `SinkholeSites` places canon's four
reserved mouths (Olderdeep 4,6 · the Deepest Cathedral 5,4 · Lampwell
12,3 · Spivenor 16,4) and reserves their cells from opportunistic
rolls before the lair loop — the W4.7 lesson applied ahead of the bug
rather than after it.

**The sweep's 🔴 fixed:** POI identity now resolves BEFORE the generic
depth branch, so a sinkhole owns all of its z-levels. Everything else
is unchanged — an ordinary cave under Sill still routes generically
(counter-pinned).

**R5 decided:** a sinkhole floor's tier is surface tier + 1 (canon),
not the anonymous stack's `depth/3 + 1`. Recorded in the pipeline's
docstring so the two formulas cannot drift back together.

**Two design errors caught by the tests, both worth keeping:**
1. *The rim ate itself.* The mouth builder ran at priority 2500 —
   BEFORE `ConnectivityBuilder` (3000) — so it asked "is this zone
   still crossable?" of a raw jungle chunk that was not crossable
   yet, and the repair loop dutifully removed all 40-odd lip cells.
   Fixed twice over: the builder moved to 3100 (cut the hole into an
   already-carved zone) AND the repair now uses the **delta
   contract** — if the zone could not be crossed before we touched
   it, the rim is not the reason. This is the W4.1
   `PlaceSolidIfHarmless` lesson recurring in a new builder; the
   pattern is now explicit in a second place.
2. *The descent had no way out* — because the test generated it in
   isolation, and `StairsUpBuilder` derives the exit from the
   connection the MOUTH registers. The test now generates the mouth
   first, which is the only way a player is ever in a descent.
   Faithful, not weakened.

Tests: 7062 → 7069 (+7). All green.

### W5.2 — The dark (SHIPPED)

`GetDepthAmbient` filled with the authored ladder, into the hook W0.2
built for exactly this: day 0.40 → descent 0.28 (the shaft still
catches light from its own mouth) → floor 0.22 → catacombs 0.12 and
below. Nothing reaches zero — the introspection doc's UX warning
honored, so it is the CONTENT that demands a lamp, never the floor.

**R1 resolved, and it was a real bug in waiting.** Remembered cells
drew at a hard-coded flat 0.2 grey. That was invisible while every
zone sat at ambient 0.40 and would have become a fog-of-war inversion
the instant the 0.12 rung shipped: ground you were LOOKING AT drawn
darker than ground you merely remembered. The remembered grey now
derives from the zone's own ambient (half of it, capped at the
historical 0.2), so memory is dimmer than sight at every rung — pinned
across the whole ladder — and the surface look is bit-identical.
Cached per zone rather than recomputed per remembered cell, per the
Wx review's render standard.

Honesty bound: whether 0.12 *feels* right is a look-pass question this
phase cannot answer headless. The invariants are pinned; the feel is
not.

Tests: 7069 → 7074 (+5). All green.

### W5.3 — The Drowned Sima (SHIPPED)

The floor is a different place per sinkhole. `SinkholeArchetypes.For`
is a **pure function of the hole's name** (R3) — never the builder rng,
never a saved field — so it is fixed forever for free and identical
across a load, which is the W4.6a/W4.7 profile lesson applied before
it could bite a second time.

**Where canon named a place, canon wins:** the Deepest Cathedral is a
Choir Cathedral because it is called that; Lampwell is a well and so
holds water; Olderdeep has somebody in it. The stable hash is the
fallback for holes nobody named, not the primary mechanism — the
design doc's "rolled at worldgen" reads as *fixed*, and an authored
table is fixed too, with better taste.

Drowned Sima content: a basin of standing water (reusing W3's
MirePool), the gin frogs canon's survey came for, and banks that stay
walkable — a floor that is entirely water is a screenshot, not a room
(pinned).

**Methodology note, honestly:** the tests were written before the
code, but I implemented before RUNNING them, so W5.3 has no confirmed
RED. Steps compressed; recorded rather than glossed. The counter-check
(a Cathedral floor is not a sima) does prove the branch actually
branches.

Tests: 7074 → 7080 (+6). All green.

### W5 foundation — cold-eye adversarial pass (after W5.1–W5.3)

Run BEFORE W5.4/W5.5 layer content on the vertical foundation, on the
principle the arc keeps re-proving: a foundation bug is cheapest at
the moment nothing is standing on it yet. Hypothesis-driven rather
than re-reading — asking what the SHAPE implies, not whether the code
looks right.

**🔴 H9 — a Drowned Sima had a Drowned Sima under it, forever.** The
routing sent every level below the Descent through the archetype
branch, and each floor also gets a `StairsDownBuilder` — so z=3 was
another sima, z=4 another, without end. Canon puts "Z=3+ CATACOMBS /
ROOTWAYS — below the floors, where placed": different content, not a
copy. The Floor is now exactly one level; below it routes to the
generic underground stack.

**🔴 H10 — half of all worlds gave a sinkhole two front doors.** The
mouth pipeline reuses the biome's wilderness recipe, which carries
`CaveEntranceBuilder` — and that fires on a **50% roll**, dropping a
second random staircase into the same descent. Worse, the original
"exactly one way down" pin passed only because the coin landed right
at seed 42: a seed-lucky test, which is a test that is not yet doing
its job. The pin now sweeps twelve seeds, and the mouth pipeline
drops `CaveEntranceBuilder` outright — a hole in the world does not
need a cave door beside it.

**Refuted, and worth recording:** zone connections DO survive save/load
(`GetConnectionSnapshot` → `SaveZoneConnection`), so a player who saves
in a descent is not stranded when the registry rebuilds — R2's
guarantee holds across a reload, which was the scariest open question.

Tests: 7080 → 7082 (+2, both RED→GREEN).

### W5.4 — The catacomb village (SHIPPED)

**The sweep corrected the plan's own citation.** W5.4's line cites
`catacomb_village_design.md` as the authority, and that file — read in
full — is an ANTHROPOLOGY doc: roles, customs, cosmology, dialect,
commerce. It carries no floor plan, no glyph legend, no entry sequence,
and none of the quotes attributed to it. The real spatial authority is
`FELLING-WORLD-DESIGN.md:1099-1113`, which ships an actual ASCII
hearth-chamber sketch WITH a legend; the real quote authority is
`Lore/Codex/05_PlaqueWall.md`, which is a finished in-world artifact.
Both were used directly: the layout preserves the sketch's topology
scaled to 80×25, and all six plaques ship **verbatim**.

**Canon drift, mine, corrected.** W5.3 assigned Lampwell and Spivenor
to DrownedSima on a rationale I invented ("a well holds water"). Canon
says Spivenor is the NAMED instance of the Spore-Wedded village
archetype and Lampwell is the bioluminescent catacomb hub — both are
catacomb villages. Reassigned. Consequence handled honestly: all four
authored sinkholes are now villages or the Cathedral, so the Drowned
Sima has no named instance in the shipped map. It remains a real
archetype reached by the hash for unnamed holes, and its content is
now tested on its own builder rather than through a named place.

**What shipped:** the hearth-patch (glowing radius 4 — canon's number;
"warmth, food, politics and the god's dream in one object"), a broken
Drosera ring, niche columns in three tiers, the plaque-wall with its
six authored plaques including the chiselled-smooth niche that names
no one, beetle-jars, two walkable Pebble-Sundew mats flanking the way
out, and two people: the Warden who meets strangers (canon's first
contact) and the Plaque-Tender who speaks the bracketed notes the
codex already wrote.

**Two mechanics, both canon-load-bearing:** the threshold ANNOUNCES
you (`dewstep` — the village recognising you by the feel of your step,
their own word for welcome), and threatening the patch **is war** — a
single 40-point reputation break, not a graded tick, on the prop-damage
seam so a torch counts too.

**HAZARD the sweep caught and the plan would have missed:** the
sinkhole floor pipeline still runs `PopulationBuilder(UndergroundTier)`
AFTER this builder, and at that depth the table rolls snapjaws. A
village square full of snapjaws is not a village — the chamber is
claimed in `GenReservedCells`, which is how the surface town keeps its
own square clear. Pinned.

**R6 honored, and partly free:** no TraderPart, no stock table — these
are a people with a culture, not a shop. `TradeStockBuilder` also gates
on `Faction == "Villagers"`, so CatacombFolk NPCs can never be
auto-stuffed with surface trade goods.

Tests: 7082 → 7091 (+9). All green.

### W5.5 — The Choir Cathedral (SHIPPED, with Wall-Catching deferred)

**Scope decided from canon, not from the plan line.** Canon puts two
different things at this address. The Cathedral ARCHETYPE is generic —
"every sinkhole of the Choir Cathedral archetype is a Choir node …
some are large and old, some small and recent"
(Lore/Factions/01_RotChoir.md:126). The DEEPEST Cathedral is Tier 5,
holds the Wedded's own body, and is "the most-difficult-to-earn
audience with any of the Six gods" (:122, :218) — god-room content the
milestone map puts in **W8**. W5.5 builds the archetype; the god lands
on top later, exactly as W5.4 left Olderdeep's founding-village
identity to W6.

Shipped: a substrate-grown vault (a nave with grown walls, warm to the
hand), the node where every thread gathers and goes on to the next
vault, two-to-four encased elders held in the wall with a voice of
their own, reused ChoirTendrils, a claimed footprint, and three new
sprites.

**WALL-CATCHING DEFERRED — three blockers, all verified in code:**

1. **The destination set has cardinality ONE.** Wall-Catching is
   Cathedral→Cathedral displacement "among Cathedral nodes", and
   exactly one sinkhole in the shipped world maps to ChoirCathedral.
   Canon's own mechanic has nowhere to route. Adding nodes is a
   world-authoring decision (canon reserved four mouths and named
   three of them as villages), not something to smuggle in here.
2. **There is no fall.** The accidental trigger is "falling into a
   Cathedral sinkhole" — but `SinkholeMouthBuilder` rings the hole
   with solid lip precisely so you cannot stroll in, and the interior
   is plain walkable floor. Codebase-wide there is no fall damage or
   pit mechanic at all.
3. **Teleporting into an ungenerated floor produces a zone with NO
   STAIRS UP — a hard soft-lock.** `StairsUpBuilder` does not create
   stairs, it READS the connection that the Descent's
   `StairsDownBuilder` wrote. Arrive by teleport and that connection
   never existed, so the exit is not there. A safe arbitrary-destination
   primitive must guarantee an exit before any teleport ships.

Also recorded from the sweep, for whoever builds it: the permanent
Wall-Bound counter belongs in `NarrativeStatePart`'s FactBag, NOT on an
Effect (effects are stripped on death and by the cure-all tonic, so the
count would evaporate); a step-trigger must NOT transition inline
(InputHandler dereferences the old zone immediately after the move);
and the save format is strict-equality versioned, so nothing may be
added to SaveZone/SaveZoneConnection/SavePointOfInterest.

Tests: 7101 → 7110 (+9). All green.

---

### W5 cold-eye + adversarial fix wave — 2026-08-23

The post-W5.5 review (workflow, both angles + player-flow hypotheses)
returned 3 🔴 and ~12 🟡. The war-latch fix shipped separately
(`18074649` — RouteDamage emits TakeDamage per blow AND per burning
tick, so rep drained per-hit instead of once; `WarDeclared` latch).
The rest ship in this wave:

| # | Sev | Finding | Fix |
|---|-----|---------|-----|
| A | 🔴 | Stairs down land dead-centre in the hearth-patch — arrival = instant war | `StrandedSettlementBuilder` takes the ZoneManager, reads the real arrival cell from the StairsDown connection, offsets the patch `PatchOffsetFromArrival = 14` cells away; mats + villagers placed relative to the chamber |
| B | 🔴 | Cathedral's four full-width solid bands had no reachability contract — a bad roll seals the floor | `IsDoorway` gaps (x 20-22, 38-40, 56-58) + delta-contract repair. Verify-pass correction: the first repair asked FloodFromWest's CROSSED question — the wrong axis (bands run east-west; west→east crossing survives via the margins while the nave is sealed north-south). Contract is now `FullyReached` whole-zone delta (falling back to crossed when the pipeline itself delivered less), and the repair is TARGETED — remove a vault cell bordering the sealed region — not tail-popping, since the sealing cell may have been placed first |
| C | 🔴 | Stair-travel state survives a load — the player auto-walks a stale path through the RESTORED zone | `CancelStairTravel` made public; `WirePresentationForLoadedGame` clears it |
| D | 🟡 | Stair travel refused whenever ANY hostile existed anywhere in the zone | `StairTravel.NoticeRadius`, Chebyshev — only a hostile that can plausibly notice you vetoes the walk. Verify-pass correction: first cut was 9, one cell UNDER `BrainPart.SightRadius` (10); raised to 10 so the gate is at least as wide as the eyes it models |
| E | 🟡 | "Any keypress stops it" sampled `anyKeyDown` on ~1 frame in 7 (below the 120ms rate gate) | interrupt latched every frame above the gate, consumed by the stepper |
| F | 🟡 | Tint regime violations: `PlaqueWall &w`, five named plaques `&W`, `DroseraRing &r` | all → `&y` / `&R` (fixture sprites multiply by glyph colour) |
| G | 🔴/🟡 | Voice/canon: the encased elder CONCEDED loss (the one thing the Choir never concedes — "everything here is somebody" is inventory, not elegy); elder promised a return-conversation that doesn't exist; "Small tier" design-register leak; Plaque-Tender's "She's" had no antecedent | `NotSame` rewritten to refuse the ledger, not concede it; `Travel` promise replaced with "ask the grove, not me"; register leak cut; the pronoun now points at the plaque of Riane — whose examine text already says "Says the patch sang once" |
| H | 🔵 | `SubstrateVault` = one bordered tile stamped up to 272 times per Cathedral floor — wallpaper | fixture-variant mechanism: `FixtureVariantCounts` + `FixtureVariantIndex(x,y,n)` position hash (distinct constants from `FloorVariantIndex`); 3 new sprites `substrate_vault_v1..v3` (same palette, moved lilac nodes + weave knots), missing-file fallback to base |

Verify-pass additions beyond the recovered findings:

| # | Sev | Finding | Fix |
|---|-----|---------|-----|
| I | 🟡 | "War" cost nothing for one act and everything for four: RepLoss=40 from rep 0 lands at −40 = NEUTRAL (threshold −50) — text not backed by mechanics. AND the ellipse places ~51 separate HearthPatch entities, so the per-Part `WarDeclared` latch was 51 independent latches (one Pyroclasm = 9 wars = −360) | The LEDGER is the latch: `WarRep = HATED_THRESHOLD` (−150); on first player blow, `Modify(WarRep − current)` — one war, village-wide, save-safe, attitude-tier real (Hated → hostile). `WarDeclared` deleted. New tests: two-tiles-one-war, falling-rock counter-check; the old two tests re-pinned to Hated / −150 |
| J | 🔴 deferred | Player MELEE never bills the war at all — bump-attack (`InputHandler.cs:748`) and Break (`:2702`) call `DestructionSystem.Damage` directly, which never fires TakeDamage; only `RouteDamage` does. The most obvious way to destroy the patch is the one path that is free | DEFERRED to its own RED-first commit: rerouting melee through `RouteDamage` widens the TakeDamage seam to EVERY breakable prop (BurnOffGasPart et al.), which the verify pass itself says "deserves its own RED test pass before merging" |
| K | 🟡 | Mats: a 5×7 band across a 23-row interior is not "the only way to the fire" — the comment over-claimed | Comment now says what the dew is: a doorbell on the arrival→glow axis, not a moat; circling wide is a choice |
| L | 🟡 | RopeAnchor `&w` — the 7th tint-regime instance, unnamed in the recovered findings | → `&y` |
| M | 🔵 | The elder rewrite dropped the old closer's permission beat ("permitted to think otherwise…"), which was good voice-card material — tolerance without concession | Appended to the new `NotSame` text |

**RED honesty:** A–G were fixes to reviewed findings with the RED
demonstrated by the review's own failing scenarios or by prior
committed tests (war-latch). H's tests (`FixtureVariantTests`, 5
tests incl. two counter-checks) were written before the mechanism
existed — RED as compile error, not run in isolation before
implementing; noted per §2.1.


---

### W5.6 — Ginmere: the drowned sima gets a place in the world (SHIPPED)

**What the plan said:** "Lampwell + Spivenor placed, plus the mouths at
Olderdeep and the Deepest Cathedral if their floors fall out of the
three shipped archetypes." All four were placed in W5.1 and all four
floors are in-scope — so the literal W5.6 was vacuous.

**What the sweep found instead:** the W5.4 canon correction (Lampwell,
Spivenor, Olderdeep → StrandedSettlement) left the **Drowned Sima
orphaned**. Its honesty note said the archetype "remains reachable by
the hash for unnamed holes" — but `SinkholeSites.All` is the only
source of `POIType.Sinkhole` (WorldGenerator.cs:82) and the shipped
world contains **no unnamed holes**. All of W5.3's floor — the standing
water, the gin frogs — was stored, not shipped.

**The fix:** a fifth authored mouth, **Ginmere** (2,7) → DrownedSima.
- Siting verified before writing code: Grovelands at the tepui's foot
  (canon puts simas in tepui country — Lore/History/00_Canon.md:63),
  tier 3, two cells west of Olderdeep; no place, road or river on the
  cell (each pinned by test).
- The name is COINED, and says so in the source docstring — canon
  names no drowned sima. In the register of Lampwell/Wellmeet: a mere
  is a standing pool; the gin frogs live in it. (Distinct from the
  W5.4 lesson: that was inventing a rationale to override an EXISTING
  canon identity; this is naming a new thing that has none.)
- **The structural pin that would have caught the orphaning:**
  `EveryShippedArchetype_HasAPlaceInTheWorld` — every enum member must
  be reachable from some authored mouth. RED before the fix (proof the
  gap was real), GREEN after, and permanent: a future archetype cannot
  ship floor-only.
- End-to-end test goes POI → mouth → descent → floor through the real
  routing, not just the builder in isolation; counter-check pins that
  Ginmere is not a village and the other four assignments moved not at
  all.

**Sprites (the standing rule, applied):** Ginmere makes the gin frog
headline fauna of a reachable floor — it was a bare 'f'. Shipped
`gin_frog.png` (pale silver-green, the "gin" is the translucency).
Sweep also caught W5.4 debt: Warden Nossik and the Plaque-Tender were
bare '@'s — shipped `catacomb_warden.png` / `plaque_tender.png`
(olm-pale, per canon's catacomb-villager morphology). Roster pins
bumped (44→45 creatures, 12→14 named actors) and a new loads-audit
covers BOTH actor tables (no such guard existed — a typo'd filename
degraded silently to the glyph).

**RED honesty:** proper RED this time — all four W5.6 tests were run
and confirmed failing (assertion-RED, 7124 total / 4 failed) before
the implementation commit. One environmental note: a mid-run
MCP-for-Unity WebSocket error poisoned 14 unrelated tests via
LogAssert in one GREEN run; server relaunched, clean rerun required
before commit.

Tests: 7120 → 7125 (+5: 4 Ginmere + 1 actor-sprite loads-audit; the
roster pins are edits to existing tests). All green — including, this
run, the documented-flaky contagion test.

---

### W5.7 — Close-out (audit + fix wave)

**The audit:** a 57-agent workflow — Angle A (taxonomy) over both
recent commits, Angle B (canon-parity-first) over the whole W5
surface, a save/load-reach lane, and a player-flow hypothesis
generator — every finding surviving only on a 2-of-2 (or red 1-of-2)
adversarial refutation vote. 20 confirmed findings (with cross-agent
duplicates; ~14 distinct) + 13 hypotheses.

**Fixed in this wave:**

| Sev | Finding / hypothesis | Fix |
|-----|----------------------|-----|
| 🔴 | Deleting `RepLoss`/`WarDeclared` breaks every v6 save mid-load — `ReadFieldValue`'s SkipValue path never consumes the value bytes, so an unknown field name misaligns the stream | `FormatVersion` 6 → 7: the strict gate turns a corrupt load into an honest rejection. The durable fix (self-describing field format that CAN skip) is recorded as a v8 project |
| 🔴 | All five mouths rendered as white `?` markers AND leaked their names on examine — canon: "overgrown mouths are found, not shown", and the mouth builder's own docstring promised no marker | POI override gated on the `Visited` bitmap (exists since v4, zero render consumers until now): plain biome face until entered, then a dark `o`. Villages stay marked undiscovered (civilization is known of) — pinned both ways |
| 🟡 | Pre-W5.6 saves never grow Ginmere — POIs come only from the save stream; the sima stays orphaned in every existing world | `WorldMap.RehydrateAuthoredSinkholes()` on load — the sinkhole twin of the W4.7 profile heal; never displaces an existing POI (pinned) |
| 🟡 | The interrupt latch sat below the save/pause/skills/wait handlers, which all early-return: '.', X, M, Tab mid-walk consumed the key and the walk resumed unasked | Latch moved above every consuming handler in the Normal-state chain |
| 🟡 | Mouth void unreserved — the biome's LandmarkBuilder (3800) accepted the cleared ellipse; a Choir shrine could generate standing INSIDE the hole | Ellipse joins `GenReservedCells` while cutting (the village-square fence) |
| 🟡 | War guard's protective direction untested: deleting the early-out would make patch damage RAISE rep for a player below −150 | Counter-check pinned (rep −175 stays −175) |
| 🟡 H1 | A floor reached LATERALLY generated before its descent — born with no way up, a soft-lock | The stack generates top-down: `PrepareZoneForAccess` recursively generates the level above first; pinned by a cold `GetZone(z=2)` |
| 🟡 H2 | A fleeing gin frog (faction-hostile, Passive) vetoed stair travel across its own floor | Passive creatures veto only when personally in a fight with you (mirrors `BrainPart`'s own canInitiate rule); counter-pinned |
| 🟡 H4 | One walk across the mats printed the dewstep line up to 5×, and NPCs narrated second-person text at the player | Recognition is permanent: player-only, once-ever (`DewstepKnown` property, rides the save); diag still fires per crossing |
| 🟡 H6 | One grove arson (−40, threshold −10) locked every Choir conversation forever — elder's mercy and the Bloom's cure included | Canon resolves it: the Choir does not do enmity. `SpeaksToHostiles` tag on all 7 Choir speakers; war-factions keep the refusal (warden counter-pinned) |
| 🟡 H7 | Burning the Deepest Cathedral's own substrate billed nothing (`z != 0` gate) | `GroveLaw.IsChoirGround` = grove surface OR ChoirCathedral floor; descent/sima counter-pinned |
| 🟡 H8 | Murdering a named villager cost −10 (base-Creature GivesRep) vs −150 for scratching the fungus | `GivesRep = 150` on Warden Nossik + the Plaque-Tender |
| 🟡 H11 | The walk cancelled dead at an immobile encased elder (planning ignores creatures) | Blocked step re-plans with `ignoreCreatures: false` before giving up; pure-half pinned |
| 🟡 H12 | The drowned sima shipped pitch-dark — the one floor with no glow was the one under an OPEN HOLE | A sima is a light well: `ShaftLightAmbient = 0.34` on DrownedSima floors via the persisted `Zone.AmbientLevel`; village keeps its authored dark (the glow-before-people reveal) — counter-pinned |
| 🔵 | Stale `RepLoss=40` param in the HearthPatch blueprint — a silent dead tuning knob | Deleted |
| 🔵 | `NoticeRadius` const vs blueprint-settable `SightRadius` | Gate widens to the hostile's own eyes (`Max(NoticeRadius, brain.SightRadius)`); pinned |
| 🔵 | The oldest plaque stranded 17 rows from its wall | Rejoins the wall band (y=4, western floor-level end); pinned |
| 🔵 | Connection registry never deduped: one duplicate per unload/regen cycle, saved forever, one StairsUp entity per duplicate | `RegisterConnection` idempotent by value; `UnloadZone` drops the connections the zone owns (both pinned) |
| 🔵 | The descent's promised cache ledge was never built (docstring + plan + canon all claim it) | Built: a sack (torch, dried meat, tonic) and the bones of whoever packed it, on the lowest terrace |
| 🔵 | The reservation sweep generated each world five times | One world per seed, all mouths checked per world |
| 🔵 | Only-way-down proven only for `All[0]` | Sweeps every mouth (each sits in a different biome with different ambient builders) |

**Pinned-as-correct (hypotheses that found no bug):** H9 — the warden
DOES refuse conversation at war (gate existed; now pinned). H10 — the
war's −150 survives save/load via the existing
`Gap_PlayerReputation_StaticState_PreservedAcrossRoundTrip` mechanism.
H13 — became the all-mouths sweep above.

**Deferred, recorded:**
- 🔴 melee-bypass (player melee never fires TakeDamage) — task chip
  filed; widens the seam to every breakable prop, needs its own
  RED-first pass (verify pass's own advice).
- H3/LOS — a hostile sealed BEHIND the vault wall still vetoes travel
  inside the nave (Chebyshev has no line-of-sight term). Wants a LOS
  check in `ShouldInterrupt`; deferred with the same shape as the
  BloomGoal LOS debt.
- H5 — war laundering via follower kills (source ≠ Player). Sibling
  of the melee chip; fold into that branch.
- Descent cost asymmetry ("falling is cheap; climbing back up costs
  turns and gear") — a multi-turn transition mechanic, not a
  close-out rider. Scope-pruned HERE, explicitly, which the sweep
  flagged the docstring for claiming silently.
- v8 save format: self-describing fields so unknown names can be
  skipped — every future public-field rename on a reflected Part is
  otherwise a save-breaker.

**Honesty bounds:** headless EditMode cannot verify — how the hidden
mouths read on the actual world-map screen; the shaft-light feel at
0.34 vs the village's 0.22; whether the re-path around an elder feels
like walking or like pathing. All three are look-pass items for a live
session. The adversarial-vote pattern is bounded by what 2 refuters
can trace; a finding both refuters miss stays missed.

**Verification:** 7125 → 7151 (+26). Two RED moments en route, both
honest: the FormatVersion tripwire fired on the 6→7 bump exactly as
designed (pin updated with the migration note), and the new
discovered-mouth test had a fixture bug (empty blueprint set vs the
builder's border walls) — fixed in place. One full-suite run was
poisoned by the MCP-for-Unity plugin's WebSocket error (its server had
died); relaunched + keepalive, clean rerun: 7151/7151, including the
documented-flaky contagion test.

---

### W5 — Exit statement

**Shipped:** the vertical world is real. Five authored mouths (four
named by canon, Ginmere coined to give the Drowned Sima a place);
three floor archetypes end-to-end (sima, catacomb village, Choir
Cathedral); the descent with its ledges, anchors, and the expedition
that never climbed out; depth-keyed ambient with the sima as a light
well; the catacomb village with its hearth-war, plaque-wall, dewstep
and two named villagers; the Cathedral with its node, encased elders,
and the law that now reaches it; Qud-parity stair travel with an
honest interrupt model; sprites for every new thing the player sees
(17 fixtures + vault variants + gin frog + both villagers).

**Structural guarantees now pinned:** every shipped archetype is
reachable from the world map; the stack generates top-down (no
stranded floors); the mouth's void is fenced; connections neither
duplicate nor leak; authored mouths rehydrate into old saves; the
save format rejects what it cannot read instead of corrupting.

**The audit trail:** three review waves (post-W5.5 20-agent, W5.6
inline, close-out 57-agent), every confirmed finding fixed or
explicitly deferred with rationale, 10 of 13 player-flow hypotheses
confirmed as real defects and fixed, 3 pinned-as-correct. The
dedicated adversarial gate file (`W5AdversarialTests`) probes the
four applicable taxonomy surfaces. Suite: 6965 at W5's start →
7164 at close, green throughout (final gate run: 7164/7164).

**Deferred with recorded blockers:** Wall-Catching (3 blockers),
melee TakeDamage seam (chip), ShouldInterrupt LOS, follower
war-laundering, descent cost asymmetry, v8 self-describing save
fields, and the live look-passes (hidden mouths on screen, shaft
light vs authored dark, re-path feel).

W5 is complete.
