# September 2026 design session — roadmap and recommendations

> **Status: SYNTHESIS DOCUMENT.** This is not a new design — it is a
> map of twelve design docs produced over the past week
> (`Docs/Design/BUILD-EXPRESSIVITY.md` through
> `Docs/Design/STILL-CREWED-VEHICLES.md`), showing how they connect,
> what small pieces of infrastructure unlock several of them at once,
> and what order to actually build things in. Each idea below is
> summarized in a paragraph; the full design — verification sweeps,
> risk tables, exact code citations — lives in its own file, linked.
>
> Scope note: this covers the twelve docs from this conversation
> (2026-09-09 through 2026-09-16). It does not re-cover the earlier
> August design docs (`ALCHEMY_SPELL_SYSTEM.md`, `PLAYER-SHOP.md`,
> `HOUSE_DRAMA_SCHEMA_V2.md`, etc.), which predate this session and are
> a separate body of work.

---

## 1. The twelve ideas, briefly

### Character progression & itemization

- **[`BUILD-EXPRESSIVITY.md`](BUILD-EXPRESSIVITY.md)** — the
  foundational one. Reframes progression around seven Practice roots
  (one per inheritance-function: Keeping/Memory, Wedding/Substrate,
  Salting/Preservation, Posy/Beauty, Counting/Exchange, Deep/Roots,
  Naming) plus three Pacts (the Spirits), so every archetype from
  detective to necromancer to pacifist refuser has a mechanical home.
  Reconciled against the unmerged `feat/fun-p0-spine` branch's "feed
  existing systems before building new ones" rule. Also catalogues
  ~110 lore-native world objects with concrete exploit hooks. This doc
  is referenced by nearly everything below — several other designs
  gate an ability on a root this doc defines (Root-Sense, The No,
  Persuasion).
- **[`BARNACLE-SYMBIOTES.md`](BARNACLE-SYMBIOTES.md)** — small
  creatures that survive defeat and clamp onto a chosen limb as living,
  tradeable attachments granting a mutation while fed. Mostly buildable
  on shipped anatomy/mutation machinery (the Manager-pattern runtime
  body-part attachment already exists). The hive-mind colony layer is
  the standout idea: enough symbiotes start "singing," which the Choir
  can hear.

### Combat (one interlocking cluster)

- **[`MOMENTUM-COMBAT.md`](MOMENTUM-COMBAT.md)** — a manual, free-cursor
  multi-target pick list (not a line or ring — the player selects
  arbitrary cells, in order) feeding a persistent Momentum counter that
  raises damage per landed hit and decays on a miss or hard CC. The pick
  cap can itself scale with Momentum.
- **[`TELLS-AND-WINDUPS.md`](TELLS-AND-WINDUPS.md)** — telegraphed
  attacks built on the turn scheduler's existing energy-tick
  granularity (a normal actor already acts once every ten ticks — that
  *is* the micro-tick the pitch asked for). A **Read** skill line grants
  graduated information about an incoming attack; a **feint** layer
  (capable enemies can lie) is what makes it "poker, not reflexes." The
  richest part: countering is *configurable* — players mark specific
  abilities as prepared, and partial completion is measured in the same
  energy currency the scheduler already uses, so "cast it partially" and
  "pay for a reaction" turn out to be the same number.
- **[`COUNTER-CHAINS.md`](COUNTER-CHAINS.md)** — built directly on the
  Tells system. Very few NPCs (named duelists, the Declined, senior
  body-readers) are counter-capable, so meeting one is an event: an
  automated exchange (swing → counter → counter-to-the-counter) that
  terminates on the same energy ledger, configurable via a small
  condition→response chain reusing the game's existing conversation-
  predicate shape. The player-facing chain *editor* is flagged as the
  expensive part and explicitly deferred past v1.
- **[`TRAPDOOR-TACTICS.md`](TRAPDOOR-TACTICS.md)** — collapsible weak
  floor tiles that drop combatants to the level below (which already
  exists and is already generated). Three-tier gate: collapsing under
  load is free physics for anyone, deliberately breaking one needs
  Strength + a heavy weapon, and *seeing* one at all is a perception
  skill (Root-Sense from `BUILD-EXPRESSIVITY.md`). Explicitly designed
  to fuse with Tells (a failing floor is a tell with a resolve tick).
- **[`BODY-LATTICE.md`](BODY-LATTICE.md)** — the one that touches the
  most existing code. Keeps the HP pool (life) but adds per-limb
  integrity (capability) — full/impaired/numb — on top of a
  hit-location and per-node-armor system that turns out to **already
  ship** (most players don't know a struck leg reads a leg's own armor
  today). Numbness, not loss, is the headline idea: an attached, useless
  limb creates demand for prosthetics, salt-curing, and symbiotes that
  three other docs already designed with no strong use case until now.

### World & death (one interlocking cluster)

- **[`GLACIER-CLOCK.md`](GLACIER-CLOCK.md)** — discovered, rather than
  invented: the Hush is already canon as a slow, localized, currently-
  advancing consuming phenomenon, and an old lore document already
  flagged "correlate the infection with the player's incomplete-act
  ratio" as the single best unbuilt mechanical hook in the game. This
  doc is the implementation plan for that hook: three independently-
  ticking Fronts (Hush/Naming, the Cure/Preservation, the Arrears/
  Exchange), each fed by a local tally of unclosed acts in a place, each
  transforming a consumed region into new biome/enemies/loot/ruins.
- **[`NPC-SCHEDULES.md`](NPC-SCHEDULES.md)** — turns out to need almost
  no new architecture. The idle-behavior substrate (a `AIBoredEvent`
  hook, five existing "go do a thing when idle" NPC parts, furniture
  that already knows how to be walked to and used) is fully built; the
  only missing ingredient is a clock. Adding a coarse day-period
  Calendar and gating the five existing behaviors by period is a
  near-free first ship.
- **[`COMPOST-ECOLOGY.md`](COMPOST-ECOLOGY.md)** — corpses already never
  decay (a latent fact, not new), so this gives that permanence a
  consequence: a locality accumulates a death "profile" from the alchemy
  property-atoms of what died there, and matures on re-entry into a
  mushroom grove, a predator's nest, etc. The best idea: unburied dead
  are canonically an abandonment, so a productive corpse farm
  *accelerates a Glacier Clock front* near it — trading regional
  stability for personal yield, in the open.
- **[`STILL-CREWED-VEHICLES.md`](STILL-CREWED-VEHICLES.md)** — answers
  its own lead question ("haunted" is the wrong word) by reframing
  derelict vehicles through the closure-ledger and the Unsaying: the
  craft is still executing its last standing order because nobody ever
  told it the voyage ended. Four resolution paths (close the account,
  countermand it, inherit the crew, scrape it) map onto four different
  factions' vocabulary for the same phenomenon. Routes are patterned
  (a vehicle runs its *last* route, so its pattern is researchable
  history, not randomness). Explicitly scoped down to a **stationary**
  derelict-as-dungeon for v1 — a moving world-map location is real new
  engineering and isn't the fastest way to the fantasy.

### Environmental / hazard

- **[`DISEASE.md`](DISEASE.md)** — the closest thing to already-shipped
  on this whole list. A complete, tested, contagious, five-stage disease
  (`FungalInfectionEffect`) already exists; the work is generalizing it
  into a data-driven registry and using two things that already exist
  and are unused (a `TYPE_DISEASE` flag, and a full transmission-vector
  taxonomy as type flags). The standout addition: a second, lore-native
  disease family where the symptom is *forgetting your own name*
  (the Unsaying, made communicable), cured only by being remembered —
  a hermit cannot be cured of it.

---

## 2. The shared infrastructure — build these regardless of which idea you greenlight

Reading all twelve together surfaces four small pieces of
infrastructure that each unlock **multiple** designs. None of them is
large. All of them are higher leverage than any single feature above,
because building one pays off two or three docs at once.

| Primitive | What it is | Unlocks |
|---|---|---|
| **A Calendar** (day-period enum off the existing tick counter) | The *only* new thing `NPC-SCHEDULES.md` needs; everything else it needs already exists | `NPC-SCHEDULES.md` (directly), `COMPOST-ECOLOGY.md` ("weeks later" maturation), `STILL-CREWED-VEHICLES.md` (timetabled routes) — **three docs asking for the same primitive independently** |
| **The closure-ledger** (open/closed/refused/abandoned acts, per place) | Already canon-designed (`11_SecondSpine.md` C4) but never implemented | `GLACIER-CLOCK.md` (the whole causal engine), `COMPOST-ECOLOGY.md` (the farming cost), conceptually `STILL-CREWED-VEHICLES.md`'s "unclosed account" framing |
| **Body-part integrity** (the lattice's core field) | New, but small — one int + thresholds on `BodyPart` | `BODY-LATTICE.md` (directly), `DISEASE.md`'s contact vector (armor-per-node blocking infection), `BARNACLE-SYMBIOTES.md` (a numb limb is where a prosthetic/symbiote goes) |
| **A `Requires`/reachability content-test convention** | Already exists (`GrimoireDistribution`'s totality pin) | Every content-heavy doc above cites this as the pattern its own reachability tests should follow — not a build item, just a discipline to keep applying |

**Recommendation: build the Calendar first, this week, regardless of
what else gets greenlit.** It is the cheapest item on this entire
roadmap (a pure function of an existing tick counter) and it is the one
piece three independent designs converged on without being asked to.

## 3. Recommended build order across the whole portfolio

Not "build every doc's own internal order in sequence" — those exist
already, in each file. This is the cross-cutting order: what to build
*first*, given how the twelve designs lean on each other.

### Wave 0 — infrastructure (small, unlocks everything downstream)
1. The Calendar primitive (§2).
2. Body-part integrity field on `BodyPart` (the lattice's core, gated to
   player + named NPCs only per `BODY-LATTICE.md`'s risk note).

### Wave 1 — the cheapest wins with the highest payoff-to-cost ratio
3. **`NPC-SCHEDULES.md` Tier 1** — gate the five existing idle-behavior
   parts by Calendar period. One field, one line, per part. Immediate
   "the world feels alive" improvement for almost no work.
4. **`DISEASE.md` step 1** — generalize `FungalInfectionEffect` into a
   registry-driven definition and start using the already-existing
   `TYPE_DISEASE` flag. The disease *already works*; this is a
   refactor, not new mechanics.
5. **`BODY-LATTICE.md` step 1** — resolve the legacy vs. per-part armor
   fork in `CombatSystem` and add hit-location messages. Pure clarity
   work on a mechanic that already ships; makes the existing per-node
   armor system visible to players for the first time.

### Wave 2 — the combat cluster (build together; they share the energy-window model)
6. **`TELLS-AND-WINDUPS.md`** through its free-verb reaction tier
   (declare/resolve split on the goal AI, the Read line's first two
   ranks). This is the spine `COUNTER-CHAINS.md` and part of
   `TRAPDOOR-TACTICS.md` need.
7. **`MOMENTUM-COMBAT.md`** step 1 (the Momentum counter alone, no
   targeting UI yet) — independent of Tells, can run in parallel with 6.
8. **`TRAPDOOR-TACTICS.md`** Tier 1 (ungated load-triggered collapse) —
   independent of Tells but designed to fuse with it once both exist.
9. **`BODY-LATTICE.md`** step 2–3 (integrity effects + dismemberment
   keyed to integrity) — now that Wave 0's field exists and Wave 1's
   clarity work has landed.
10. Then, once 6 is proven fun: `TELLS-AND-WINDUPS.md`'s prepared-loadout
    tier, `MOMENTUM-COMBAT.md`'s manual multi-pick targeting, and
    `COUNTER-CHAINS.md`'s two-link exchanges — roughly in that order,
    each gated on the previous one actually feeling good in play.

### Wave 3 — the world/death cluster (build together; they share the ledger and calendar)
11. **`GLACIER-CLOCK.md`**'s data model (Front state per world-map cell)
    — cheap, pure data, no gameplay yet.
12. **The closure-ledger substrate** (§2) — this is the point where it
    actually needs to exist, since both 11 and the next item read it.
13. **`COMPOST-ECOLOGY.md`** steps 1–3 (death profile, maturation on
    re-entry, three outcomes) — now wired to both the Calendar (Wave 0)
    and the ledger (12), so the "farming accelerates a Front nearby"
    payoff lands immediately instead of needing a second pass later.
14. **`STILL-CREWED-VEHICLES.md`**'s stationary derelict — independent
    of the ledger/calendar work but reuses the same "unclosed act"
    framing conceptually; can slot in anywhere in this wave.

### Wave 4 — itemization and the expensive/deferred pieces
15. **`BARNACLE-SYMBIOTES.md`** attach/detach + upkeep — best done after
    Wave 0's integrity field exists, since numb limbs are where
    symbiotes and prosthetics both want to go.
16. **`BUILD-EXPRESSIVITY.md`**'s own build order (Names + the
    closure-ledger, which Wave 3 will have already built; then Naming's
    Host/The No, then Keeping's testimony items) — this doc is large
    enough to run as its own long-running track alongside the waves
    above rather than waiting for all of them.
17. Deferred on purpose, in every doc that mentions them: player-facing
    editors (the Counter-Chain chain editor), full HP-pool replacement,
    NPC-to-NPC disease spread, moving world-map vehicles, feints, and
    the hive-mind symbiote colony layer. Each is flagged in its own doc
    as "prove the simple version is fun first."

## 4. If you can only greenlight one cluster

**The world/death cluster (Wave 3).** Three separate designs
independently converged on wanting the same closure-ledger and calendar
primitives, and all three are majority-content work once those two
pieces exist — no combat rebalancing, no new UI screens, no risk to the
214 test files that touch the HP pool. It is also the cluster most
directly grounded in canon that was *already written and never
implemented* (the Hush, the incomplete-act-ratio hook), so it is the
lowest-risk way to make the world feel different without touching
anything players already rely on.

The combat cluster (Wave 2) is the more exciting one, but it is also the
one with a real risk flagged in three different docs: pacing collapse if
reactions/counters/tells become mandatory rather than a texture layer.
It should follow, once the ledger/calendar work has proven the "small
shared primitive unlocks several designs" pattern once already.

## 5. Where everything lives

All twelve docs are on branch `claude/game-lore-analysis-jqa7ur`, in
`Docs/Design/`. Every doc is self-contained (verification sweep, risk
table, its own scope-prune and build order) — this roadmap is the index
and the cross-cutting recommendation, not a replacement for reading the
one you're about to build.
