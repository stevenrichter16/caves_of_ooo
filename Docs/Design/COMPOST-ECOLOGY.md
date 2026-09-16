# Compost Ecology — the dead terraform the world

> **Status: BRAINSTORM / DESIGN PROPOSAL.** Nothing here is implemented.
> User pitch (2026-09-16): corpses left in the world aren't cleaned up,
> they terraform it. A battlefield full of the dead becomes, weeks
> later, a mutation-rich mushroom grove or a new predator's nest, seeded
> by what died there and how. Players can deliberately farm terrain by
> choosing what to kill and where to leave it — death as long-term
> gardening.

## 0. Verification sweep

**The reframing fact: corpses already never decay.** There is no decay,
rot, expiry, or despawn logic on `CorpsePart` or on items anywhere. A
corpse dropped in a cached zone stays there indefinitely. So the pitch
is not fighting a cleanup system — **the world already accumulates the
dead, and that accumulation currently means nothing.** This feature
gives existing debris a consequence.

| What the pitch needs | Actual state |
|---|---|
| Corpses that persist | **Already do**, permanently. `CorpsePart` spawns a corpse entity at the death cell carrying `CreatureName`, `SourceBlueprint`, `SourceID`, `KillerID`, `KillerBlueprint` — the "what died here and how" record already exists per corpse |
| Something to grow in the aftermath | **Already built**: `GatherNodePart` (regrowing harvest points with `MaxUses`/`RegrowTurns`) + `LootTableRegistry`. A grove is a cluster of gather nodes; the payoff needs no new systems |
| A new predator's nest | **Already built**: `PopulationTable` seeds creatures per zone at generation |
| A cleanup force to compete with | **Exactly one, and it's local and rare**: `AIUndertakerPart` hauls corpses to a graveyard, opt-in per NPC blueprint, intended for an Undertaker NPC. See §4 — this is a feature, not an obstacle |
| Elapsed time ("weeks later") | **Missing.** `TurnManager.TickCount` is the only clock; there is no calendar. This is the *same* gap `Docs/Design/NPC-SCHEDULES.md` identified — both features want one small shared primitive |
| Zones that survive absence | `ZoneManager.CachedZones` persists zones by ID and restores them from saves | Deferred resolution on re-entry is viable |

## 1. The loop

1. Things die and are left. Each corpse already records what it was.
2. The site accumulates a **death profile** (§2) — not per corpse, but
   per locality, so a battlefield reads differently from a single kill.
3. The player leaves.
4. On re-entry, elapsed ticks are resolved against the profile and the
   site has **become** something (§3).

Deferred resolution on re-entry is the right shape because zones already
generate lazily and deterministically. Nothing needs to simulate while
the player is away — the transformation is computed from *time elapsed*
and *what was recorded*, once, when someone arrives to see it.

## 2. What determines the outcome — reuse the atom vocabulary

The pitch's "seeded by what died there and how" needs an input
vocabulary. **Use the one the alchemy system already speaks:** the
`ReagentPart` property atoms — heat, cold, combustible, corrosive,
conductive, toxic, binding, vital, sweet, viscous, volatile.

Every creature contributes its atoms to the site when it rots there. The
accumulated profile, plus density and biome, selects the outcome:

| Site profile | Becomes |
|---|---|
| `vital` + `viscous`, dense | the classic: a fat mushroom grove, mutation-rich |
| `toxic` heavy | a poisonous fen — good reagents, bad to linger in |
| `heat` + `combustible` | a smoldering ashbed; fire-flora, fire-fauna |
| `binding` heavy | something that holds its shape too well — salt-adjacent, and unsettling |
| thin profile, scattered kills | scrub and scavengers, not a grove — **farming requires commitment** |

Three consequences worth having:
- **It is learnable and deterministic**, so farming is a plan rather than
  a lottery.
- **Cause of death should modify it** — burned corpses skew the profile
  toward heat, dissolved ones toward corrosive. The player's *method*
  becomes an input, which is exactly the pitch's "and how."
- **Zero new vocabulary.** Crafting, alchemy, and ecology all speak one
  language, so a player who understands reagents already understands
  compost.

## 3. Outcomes are existing content, placed

A matured site spawns **gather nodes** (shipped) from a compost-specific
`LootTable` (shipped), and optionally seeds a **population entry** for a
predator drawn to the site. Nothing here is new engineering — it is a
new *reason* for the M4 gather/loot slice to exist, which currently has
one biome's worth of content and no narrative source.

## 4. The undertakers are the balance, and they already exist

`AIUndertakerPart` is the only force that removes corpses, it is opt-in
per NPC, and it hauls to a graveyard. That produces a rule the design
gets for free and should lean on hard:

**You can only farm where nobody tends the dead.**

Villages with undertakers stay clean. Wilderness, lairs, and abandoned
places compost. So the map self-organizes into tended and untended
ground without a single new system, and "is anyone still burying people
here?" becomes a readable signal about a place's health — which dovetails
precisely with the schedule-breakdown tell in
`Docs/Design/NPC-SCHEDULES.md` §3.

## 5. Lore — this is the Rot Choir's doctrine as a simulation

Canon: the substrate is *"flesh, rot, root, the warm dark that everything
lies down into at the end of standing up,"* and the Choir *"gathers
everything the falling world set down."* Compost ecology is not
Choir-adjacent flavor — **it is the Choir's theology running as a world
system**, and every faction already has a committed position on it:

- **The Rot Choir approves**, warmly and sincerely. Farming corpses
  raises Choir standing, which is funny and horrifying in the right
  proportion: the kindest faction in the game loves your body farm.
- **The Recension is appalled.** Bodies are records; letting one rot
  into mushrooms destroys a record that could have been read. This is
  the canonical hold-versus-eat axis, expressed as terrain.
- **Pale Curation files the site.** The Catchers, who preserve the
  about-to-die, regard a compost field as a moral catastrophe.

## 6. The cost that makes it a decision (and the best idea here)

Farming needs a real price or it is strictly free value. Canon supplies
one exactly:

**The unburied dead are an abandonment.** The closure-ledger
(`11_SecondSpine.md` C4) holds that what feeds the Thinning is *acts
left dangling without a spoken end* — and a body left to rot, unmourned
and un-entered, is the most literal dangling act the setting has.

So a productive compost farm **raises its region's local incomplete-act
tally**, accelerating a front in `Docs/Design/GLACIER-CLOCK.md`. The
player is trading regional stability for personal yield, deliberately,
with full information.

That single link does a lot of work:
- Farming becomes a genuine moral and strategic decision rather than a
  chore with upside.
- It explains *why* the Choir approves — the Choir has never minded the
  world thinning; inclusion is their answer to it.
- It gives the Glacier Clock a **player-caused** accelerant, which makes
  that system's "your behavior shapes where the infection spreads" rule
  concrete instead of abstract.
- And it means a player can **deliberately rot a region** — which is a
  legitimate, horrifying strategy the game should allow.

## 7. Adjacent hooks (cheap, optional)

- **Severed limbs compost too** — they are already item entities
  (`SeveredLimbFactory`), so a field of arms is a thinner, stranger
  profile than a field of bodies.
- **Symbiotes feed at compost sites** (`BARNACLE-SYMBIOTES.md` §4), so a
  farm doubles as an upkeep station.
- **Mourning is the counter-play**: holding a funeral, entering a death
  with a Recension scribe, or burying the dead *closes* the act — which
  removes the ledger cost and also removes the compost. You cannot both
  honor them and farm them, which is the correct tension.

## 8. Risks

- 🟡 **Save and state volume.** A per-site death profile for every zone
  the player has ever fought in accumulates forever. Needs a cap, a
  decay on stale sites, or aggregation to a coarse per-zone summary
  rather than per-cell records.
- 🟡 **Fast-travel exploits.** If maturation keys purely off elapsed
  ticks, a player can burn time cheaply to force harvests. Tie
  maturation to a *minimum real elapsed span* and cap yields per site.
- 🔵 **Legibility.** The player must be able to tell why a site became
  what it became, or farming is superstition. The Recension is the
  natural in-fiction reader: a scribe can tell you what a site is
  turning into and why.
- 🔵 **Corpse clutter performance** — already a latent condition since
  nothing decays, but this feature encourages piles. Worth measuring
  before shipping.

## 9. Scope-prune (v1) and build order

**Cut from v1:** predator nests (groves only); cause-of-death profile
modifiers; severed-limb profiles; the closure-ledger cost (it depends on
a ledger that isn't built yet — but design the hook now so it slots in).

1. **The shared calendar primitive** — the same one
   `NPC-SCHEDULES.md` needs. Build once, use twice.
2. **Per-site death profile**: accumulate atoms from corpses in a
   locality, stored coarsely per zone, with a cap.
3. **Maturation on re-entry**: elapsed ticks + profile → one of three
   outcomes, spawning existing gather nodes from a compost loot table.
   Three outcomes, not ten — prove the loop.
4. **Faction reactions** (Choir up, Recension down) and a scribe who can
   read a site.
5. **Then** the ledger cost and Glacier Clock link, predator nests,
   cause-of-death inputs, and mourning as counter-play.
