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
