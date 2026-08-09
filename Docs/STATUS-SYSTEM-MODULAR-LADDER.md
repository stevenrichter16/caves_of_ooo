# The Modular Status Ladder — extracted from an external design conversation

**Source:** a ChatGPT conversation the user (steven) had about the
status-effect / skill / grimoire system, pasted into the Claude Code
session on 2026-08-09.
**Status:** REFERENCE. Nothing here is scheduled. This document extracts
the ideas, records exactly what the user did and did not commit to, and
maps every proposal onto what Caves of Ooo already has.

**Source completeness — verified.** The shared link
(`chatgpt.com/share/6a78e33e-…`) was fetched and contains *exactly* the
text that was pasted, no more. The conversation **begins mid-stream**:
its first line is a reply ("Exactly. Spectacle's nice…"), so an earlier
portion exists that the share does not include. It was a voice
conversation, which is why it reads as continuous prose.

**Consequence: several systems are referenced as already-established but
are never defined in the available text.** Do not treat the descriptions
below as their specifications — they are glimpses:

| Term | All that the available text tells us |
|---|---|
| **Affinity halo** | A hover-time indicator on the cursor/spell that picks up visual riders from nearby state — "little yellow sparks" over water for lightning, "a soft white wisp" near steam |
| **Arcane Sense** | A toggle held for ~two seconds; tiles glow by type; release and it is gone. Explicitly designed so you still have to *remember* |
| **Reaction compass** | Named once, in the list of Awareness-UI pieces. Never described |
| **Ink** | Referenced as the currency Reversion refunds. Its source and other sinks are not in this text |
| **Reversion** | The harvest verb — removes temporary layers from a tile and refunds Ink. Mechanics beyond that are absent |
| **Codex** | Named once as an Awareness-UI piece. Never described |

If any of these get picked up, the missing definitions have to come from
the user, not from this document.

> **Read the approval section carefully.** Most of this document is an
> *external assistant's proposal*. The user asked for it and did not
> reject it, but "not rejected" is not "approved." §1 draws that line
> explicitly so a future reader does not mistake a brainstorm for a
> decision.

---

## 1. What the user actually committed to

These are the user's own words in that conversation, and they are the
only load-bearing approvals in it:

> *"I want to build this system as simple modular components that can be
> composed on top of one another. This makes a proof of concept for each
> depth level of the system testable. And proof of concepts can be
> developed quicker."*

**Approved — three principles:**

| # | Principle | Consequence |
|---|---|---|
| A1 | **Modular, composable components** | Each layer is independently useful; deeper layers add rules to things that already exist rather than replacing them |
| A2 | **Every depth level independently testable as a POC** | You can stop at any layer and still have a playable system |
| A3 | **POCs developed quickly** | Prefer small provable slices over big-bang features |

The user's only other message was *"do more expansion, flesh it out with
more examples"* — a request for detail, not an endorsement of any
specific mechanic.

**Not approved, merely proposed:** the twelve-module ladder, the Ink
harvest economy, the `Material / Coating / Energy / Structure` grammar,
the affinity halo, Arcane Sense, the "Nearby potentials" widget, and
every specific reaction. Treat all of it as a menu, not a plan.

---

## 2. The one architectural principle worth adopting outright

From the transcript, and the single most valuable line in it:

> **Spells should emit simple events, not contain combo logic.**

Instead of Fireball asking *"is the target wet? oily? frozen?"*, Fireball
says: *apply Heat, apply Burning, maybe create Embers.* A separate
reaction system decides what that means.

**Why this matters right now:** it is a direct constraint on **SM7's
`ResonanceSystem`**, which is the next thing to be built in
`Docs/SPELLCRAFT-STATUS-SYNERGY.md`. The plan already says resonance
must be a data table rather than hard-coded per rite; this transcript
independently arrives at the same conclusion from the other direction.
That agreement is worth taking seriously.

**Where the codebase already violates it:** `PyroIgnition` (SM4) has the
new Pyromancy powers ask "is the target wet?" before igniting. That is
combo logic living in the caster. It was the right call for SM4's scope
— the alternative was rewriting four shipped mutations — but under this
principle it is a known deviation, not a pattern to copy.

---

## 3. The proposed ladder, mapped against what already exists

This is the part that turns a brainstorm into something actionable. Most
of the early ladder is **already built**.

| # | Proposed module | Status in Caves of Ooo | Evidence |
|---|---|---|---|
| 1 | **Status Core** — actors have states | ✅ **Done, far past the proposal** | 34 `Effect` subclasses: Wet, Burning, Frozen, Electrified, Acidic, Charred, Smoldering, Hobbled, Rooted, Confused, Paralyzed… |
| 2 | **Tile State** — floors hold states too | 🟡 **Partial** | `LiquidPoolPart`, `GasPoolPart` exist; there is no general "this tile is Wet/Burning" layer that shares code with creature effects |
| 3 | **Reaction Table** — two states produce a result, data-driven | ✅ **Done, and it is exactly the proposed design** | **13 JSON reactions** in `Assets/Resources/Content/Data/MaterialReactions/`, resolved by `MaterialReactionResolver`. Includes the transcript's own examples: `water_plus_fire`, `lightning_plus_conductor`, `oil_plus_fire`, `fire_plus_ice` |
| 4 | **Residue** — actions leave traces | 🟡 **Partial** | `LifespanPart` drives steam clouds, ash drifts, scorch marks and conjured puddles; SM6's `IceWall` uses it. No general "spell writes a residue" contract |
| 5 | **Propagation** — states move | 🟡 **Partial** | `ElectrifiedEffect.OnTurnEnd` chains through conductors; the gas system drifts; fire spreads thermally. Not unified |
| 6 | **Terrain Objects** — tiles contain nouns with tiny property sets | ✅ **Done, and the property set matches** | `MaterialPart` carries `Combustibility`, `Conductivity`, `Porosity`, `Brittleness` — the transcript proposes almost exactly this list |
| 7 | **Terrain Creation** — the player adds nouns | 🟡 **Partial, and growing** | `ConjureWaterMutation`, `ConjureRainMutation`, and **SM6's `Cryomancy_GlacialWall`**, which raises temporary ice. No Grow Tree / Raise Stone |
| 8 | **Structure / Movement** — push, pull, cut, fall | 🟡 **Partial** | SM1's `TryPush` and SM5's `TryPull` give push/pull on a shared stepper. No cut / fall / collapse |
| 9 | **Clean Slate / Harvest** — erase writing, refund Ink | 🔴 **Not built** | Genuinely new. Interacts with the approved ink economy (see §4) |
| 10 | **Underlying Grammar** — `Material / Coating / Energy / Structure` | 🔴 **Not built** | The transcript itself says do not ship this unless it creates decisions the simpler table cannot |
| 11 | **Reaction Awareness UI** | 🔴 **Not built — and it is already planned** | This is **SM12** in `SPELLCRAFT-STATUS-SYNERGY.md` (primed marker, resonance preview, callouts). Two independent designs converged on it |
| 12 | **Progression unlocks capability, not numbers** | 🟡 **Partial** | Skill trees exist and grant powers; nothing yet toggles a *capability* onto an existing spell ("Fireball gains Leaves Embers") |

**The headline:** modules 1, 3 and 6 — the foundation the transcript
spends most of its time proposing — are **already shipped**, and module
3 is already the data-driven lookup table it recommends. The genuinely
unbuilt work is 9 (harvest), 10 (deep grammar), 11 (awareness UI, =SM12)
and the capability half of 12.

---

## 4. Where this converges with the already-approved plan

`Docs/SPELLCRAFT-STATUS-SYNERGY.md` was written and approved *before*
this transcript was shared. Three things line up, which is meaningful
precisely because the two were arrived at independently:

1. **Ink as a resource.** The transcript's module 9 refunds *Ink* for
   erasing environmental layers. The approved plan's third "oomph"
   pillar is that **grimoire rites cost ink charges**, re-inked with
   `InkVial`. Same currency, two different sinks — a harvest module
   would slot into an economy that already exists.
2. **A reaction/resonance table, not per-spell logic.** Both designs
   independently insist the combination rules live in data.
3. **An awareness layer as its own module.** Transcript module 11 and
   plan SM12 are the same feature, including the same instinct that it
   should *observe* the simulation rather than drive it.

**And one real tension.** The transcript's module 1 says *"skills apply
them reliably; grimoires consume or transform them"* — which is the
prime/detonate grammar the user affirmed in session. But it also
proposes reactions that consume statuses **automatically** (`Wet +
Charged → Electrified`). If the world consumes a status on its own, the
player's rite has nothing left to spend. Any future reaction work has to
decide which statuses are *world-consumable* and which are reserved as
**player currency**. That question does not exist yet, but it will the
moment modules 3 and 9 meet the rites.

---

## 5. Examples worth keeping (they are good, and they are cheap)

The transcript's vignettes are useful as design targets rather than
specs:

- **The ruined greenhouse.** Puddles on the floor, copper pipes
  overhead. Cast lightning into the water, the arc leaps into the pipe,
  and a gate unlatches. *No tutorial text — the room explains itself.*
  This is the strongest single idea in the transcript: **environmental
  puzzles authored out of reaction rules that already exist.**
- **Steam as a carrier.** Water across hot ash makes steam; a gust spell
  through steam picks up a burn rider. Caves of Ooo already spawns
  `SteamEffect` from `water_plus_fire`, so half of this exists.
- **Progressive disclosure of information.** Three tiers, player's
  choice: nothing → a two-second "Arcane Sense" tile-type overlay → a
  corner widget listing *"Nearby potentials: wet, conductive,
  flammable."* Crucially: **ingredients, never solutions.** That framing
  is a good constraint on SM12.

---

## 6. If this ladder is ever picked up, the honest starting point

Given how much already exists, a first slice would **not** start at
module 1. The smallest genuinely-new, independently-testable POC is:

> **Tile state that shares code with creature state (module 2), plus one
> residue writer (module 4).** A water spell writes `Wet` to a tile; a
> creature standing on a Wet tile counts as Wet for conduction. Nothing
> else changes.

That is testable in isolation, it reuses the existing reaction table
rather than growing it, and it would immediately make the already-built
`lightning_plus_conductor` reaction reachable from terrain instead of
only from creature effects — which is the transcript's greenhouse
vignette, at the smallest possible scope.

**This is a suggestion, not a plan.** The approved roadmap is
`SPELLCRAFT-STATUS-SYNERGY.md` SM7–SM13, and SM7 (resonance + rites) is
the next thing to build.
