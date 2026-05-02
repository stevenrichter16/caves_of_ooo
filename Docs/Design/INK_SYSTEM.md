# Ink — Class A Pillar Design

> The Palimpsest's elemental medium. The cosmological counter to the
> Choir's mycelial memory-consumption. A pillar mechanic with its own
> progression, verbs, and cross-system integration — comparable in
> scope to combat, mutations, and faction politics.

---

## Thesis

**Ink is what memory looks like when it is preserved instead of digested.**

The Choir digests the world into understanding through mycelium and
spore. The Palimpsest preserves the world into permanence through
inscription and ink. These are the two answers the world gives to *what
do you do with the broken old world?* — and they are mutually hostile
methodologies. Existing Palimpsest dialogue already establishes this:
*"We are the text written over text, preserving every layer."*

This document promotes ink from scattered system-flavor (faction
reaction in repair, reagent in grimoires, dialogue color in Palimpsest
conversations) into a **Class A pillar mechanic** with the same
authoring weight as combat, mutations, House Drama, and the narrative
state layer. The Palimpsest is the faction that teaches it; ink is the
material; **Inkwork** is the player-facing skill tree.

This doc unifies and extends three existing systems:
- `Mechanics/GRIMOIRES_AND_SIGILS.md` — magical inscription as equipment
- `INKBOUND_LORE_REPAIR_SYSTEMS.md` — restoration of inscribed infrastructure
- `Conversations/Palimpsest.json` — the faction's cosmological identity

It also **resolves the open BlackmailV2 question** of what Palimpsest-side
political verb mirrors the Choir's *Held Breath* — the answer is
*Inscription Oaths* (see §"Cross-system integration" below).

---

## The cosmological frame

Caves of Ooo's central memory-cosmology is a pair, not a singleton:

| Faction | Substrate | Verb | Result |
|---|---|---|---|
| Rot Choir | Mycelium / spore | **Digest** | Memory becomes understanding (lossy, communal, biological) |
| Palimpsest | Ink / inscription | **Inscribe** | Memory becomes permanence (durable, layered, archival) |

Both are responses to the same cataclysm — the Choir calls it the *First
Feeding*; the Palimpsest calls it the *Temporal Unraveling*. The two
factions are already established as politically opposed (-25 in
`Factions.json`). This design makes that opposition *mechanical*: every
ink action interferes with mycelial action and vice versa, with explicit
counter-mechanics on both sides.

The implication: choosing an alignment is not just a flavor choice. It
gates skill access, content surface, and the verbs available to the
player for shaping the world.

---

## What ink does — the seven verbs

All seven verbs operate on **inscriptions** — durable marks placed by
ink on a surface (object, person, place, even the world's narrative
state record).

### 1. Inscribe (Tier 1 — Apprentice)
Write a mark onto a surface. Most basic verb. Effects depend on ink
type and surface type. Includes mundane uses (signs, labels, journal
entries) and minor magical uses (a Mark of Recognition that lets you
re-find a place, a Mark of Owning that proves an item's provenance).

### 2. Read (Tier 1 — Apprentice)
Interpret existing inscriptions, including faded, layered, or partially
erased ones. Reveals provenance, age, sometimes the writer's identity.
At higher ranks, reads *ghost-layers* — earlier inscriptions beneath
the current one. The investigator's verb.

### 3. Bind (Tier 2 — Scribe)
Write a sigil that anchors a person, place, or object to a condition.
The combat/utility verb, where this design folds in
`Mechanics/GRIMOIRES_AND_SIGILS.md`:
- Defensive wards (sigil on a doorway: hostile entry triggers an alarm
  or damage)
- Bind-runes (sigil on a target: target cannot leave a zone, or cannot
  perform a specific action, for N turns)
- Object-bindings (sigil on a weapon: weapon's owner is recorded;
  theft becomes detectable)
- Identity-bindings (sigil on a person, with consent: their true name
  is anchored against soul-altering effects)

### 4. Layer (Tier 2 — Scribe)
Write *over* an existing inscription, partially preserving the original
as a ghost-layer. The verb that gives the faction its name. Lets a
player overwrite a previous truth with a new one — but the original
isn't fully gone; a high-Read scribe can find it.

This is the central authoring move of the Palimpsest's worldview: *every
new truth is layered on previous truths; nothing is fully replaced, only
overwritten.*

### 5. Anchor (Tier 3 — Palimpsest)
Make a fact **permanent against memory consumption**. The direct counter
to the Choir's `MemoryConsumption` mechanic. An anchored fact cannot
be eaten by the Choir without paying apex-tier costs (and even then
leaves a violent scar in the spore network that everyone notices).

This is the high-cost defensive verb. It requires rare ink (Deep Ink —
see typology) and a Palimpsest-recognized rite. Players use it
sparingly: anchor the truths that *must* survive.

### 6. Erase (Tier 3 — Palimpsest)
Destroy an existing inscription, with traces. Cannot fully un-write —
even the most thorough Erase leaves a *scrape-mark* readable by a
high-Read scribe. The Palimpsest's worldview here: *we do not pretend
nothing was ever written; we make the writing harder to read.*

### 7. Witness-Bind (Tier 4 — True Scribe, long-horizon)
Formalize a witnessed event into an irrefutable record. The apex
defensive verb against Choir memory-eating. A Witness-Bound event is
written not just into one inscription but into the temporal substrate
itself — multiple corroborating ghost-layers, distributed across
locations, none individually destroyable. Requires multiple witnesses,
rare ink, and Palimpsest-aligned standing of *Trusted* or higher.

---

## Ink typology

Inks are **resources** the player gathers and crafts, like alchemical
reagents but for inscription. Each ink has: source, rarity, properties,
faction implications, counter-effects.

| Ink | Source | Rarity | Properties | Faction implications |
|---|---|---|---|---|
| **Iron-gall ink** | Common; oak galls + iron filings | Common | Standard medium. Cheap, durable. Inscriptions last decades. | Universal. |
| **Spore-blood ink** | Choir-byproduct or hostile harvest | Uncommon | Inscriptions interfere with mycelial signatures (counter-leverage when used against blackmail traces). Choir hates seeing it used. | Choir-hostile. Palimpsest-permitted but uneasy. |
| **Ash ink** | Ash from a meaningful burning (a heretic, a contract, a relic) | Uncommon | Carries mourning weight. Inscriptions about the burned subject are unusually potent. | Palimpsest accepts; Pale Curation regulates. |
| **Saltwater ink** | Brine + bone-black, gathered at the Bright-Water Coast | Uncommon | Resistant to fire and time. Inscriptions outlast most surfaces. | Glassblown Remnant aligned. |
| **Dream-fruit ink** | Pulped dream-fruit (also a Choir-side memory-eating reagent — direct competition) | Rare | Inscribes onto things normally uninscribable: memory, identity, intent. | Both Choir and Palimpsest covet it; Palimpsest pays more. |
| **Bone-black ink** | Bones of a specific named person (with consent or theft) | Rare | Inscriptions referencing the bone's owner are irrefutable to other inscriptions. The dead become witnesses. | Pale Curation tightly regulates. |
| **Sun-fade ink** | Pigment that bleaches in light | Common | Temporary by design — inscriptions fade in days or hours. For one-time bindings, passwords, transient signs. | Universal. |
| **Witness-blood ink** | Blood freely given by a witness to a specific event | Rare | Inscriptions about *that specific event* are irrefutable. Required for Witness-Bind. | Palimpsest-coveted; Choir hostile. |
| **Deep ink (true ink)** | Drawn from the Hollow Cathedral's deepest archive-pool; quest-gated | Apex | Inscriptions cannot be erased, layered over, or palimpsested. The Anchor tier. | Palimpsest-only access. |

Crafting and acquisition loops are real adventure work — none of these
are buyable in bulk. Iron-gall is cheap because it's a player-craftable
recipe; the rare inks each have their own quest seam.

---

## The Inkwork skill tree

Four tiers, paralleling The Held Breath's structure but with its own
verbs. Faction lock-in: **Inkwork is Palimpsest-aligned.** Players
deeply tied to the Choir cannot advance past Tier 1 without losing
Choir standing; Palimpsest reputation gates higher tiers.

### Tier 1 — Apprentice (v1.0 shippable)
Verbs: **Inscribe**, **Read**.
Gating: any character can take this; no faction requirement.
Player-facing: literacy, basic sign-making, journal entries, examining
old inscriptions. The "everyone can read and write" baseline.

### Tier 2 — Scribe (v1.0 shippable)
Verbs: **Bind**, **Layer**.
Gating: Palimpsest reputation *Acquainted* or higher.
Player-facing: defensive wards, bind-runes, the layering verb. This is
where Inkwork becomes a *playstyle* — you can shape the world by
inscribing on it. Folds in the existing Grimoires & Sigils content as
the equipment-side expression of Bind.

### Tier 3 — Palimpsest (v1.0 shippable, gated by content)
Verbs: **Anchor**, **Erase**.
Gating: Palimpsest reputation *Trusted* or higher; rite of induction
(quest-gated). Cannot be Choir-aligned past *Indifferent*.
Player-facing: counter-memory-consumption defenses, deliberate erasure
of inscriptions, full participation in the cosmic memory-axis war.

### Tier 4 — True Scribe (long-horizon)
Verbs: **Witness-Bind**, **Forge a Soul** (rewrite an identity's true
name; identity-altering, very rare).
Gating: deep main-quest content, multiple Tier-3 witnesses,
Palimpsest-internal politics. Reserved for the late-game.

---

## Cross-system integration

The ink system isn't a sidecar — it cuts across most of the existing
gameplay systems and provides the Palimpsest-aligned counterpart to
mechanics currently authored from the Choir side.

### Combat — sigils and wards
- Bind-runes inscribed on the ground, doorways, weapons.
- Folds in `Mechanics/GRIMOIRES_AND_SIGILS.md` directly: grimoires are
  *bound spell-inscriptions*. Sigil-slot upgrades on grimoires are
  Inkwork-verb actions, not generic crafting.
- Existing `LayRuneGoal` content (in the codebase) is now an Inkwork
  expression.

### Repair — restoration as inscription
- Folds in `INKBOUND_LORE_REPAIR_SYSTEMS.md` directly: restoring an
  archive stone is **Read** + **Layer** (you read the faded inscription,
  you layer over it preserving the ghost). Restoring a dried well is
  **Bind** (you inscribe the water-seeking sigil back into the rim).
- Faction reputation effects already authored there now fit the larger
  Inkwork frame.

### Social — Inscription Oaths (the Held Breath counter)
**Resolves the BlackmailV2 open question.** The Palimpsest-side political
verb is the **Inscribed Oath**:

- A formal contract inscribed on an object both parties touch (the
  Palimpsest's preferred technology — formal, witnessed, archived).
- An Oath's terms are mechanical: violations trigger a Bind effect on
  the violator (the inscription enforces itself).
- Witnessed publicly. Preserves the Palimpsest's cosmology of
  *transparency through layered record*, in deliberate contrast to The
  Held Breath's *power through hidden leverage*.
- Politically: where The Held Breath produces "compliance without
  displacement," the Inscribed Oath produces "compliance through
  visible commitment." Both extract behavioral change from a target;
  one is private blackmail, one is public covenant.

This is the **Class A symmetry** the cosmology has been promising:
two factions, two political verbs, mutually exclusive playstyles.

### Narrative state — Anchoring against memory consumption
- The `NarrativeStatePart` event log can be **Anchored** (Tier 3) to
  protect facts from Choir memory-eating.
- An Anchored fact survives even apex-tier consumption attempts; the
  Choir leaves a violent scar in the spore network when forced to eat
  past an Anchor.
- Direct mechanical counter to the existing `MemoryConsumption` design
  in IDEAS.md.

### Identity and Lineage — the true name
- Inscribing a person's true name in the Palimpsest's archive anchors
  their identity against soul-altering effects (Memory Consumption of
  *self*, mutations that wipe identity, etc.).
- Folds into the existing `LINEAGE-DESIGN.md` system: legitimacy claims
  can be Inkbound via Witness-Bind. Forged lineage claims are detectable
  by a high-Read scribe (the ghost-layer betrays the forgery).

### Witness — formalization of evidence
- The Witness system already produces events. Inkwork **formalizes**
  them into permanent record via Witness-Bind.
- This is what makes the Held Breath's blackmail-counter mechanic
  tractable: a Witness-Bound event cannot be eaten by the Choir, which
  means a player can preemptively protect themselves from
  retroactive-truth-erasure.

---

## The Choir-Palimpsest balance

Explicit table of mutual constraints — both factions get verbs that
counter the other, so neither is unconditionally dominant:

| Choir verb | Palimpsest counter | Tradeoff |
|---|---|---|
| Memory Consumption (Tier 1: one fact / NPC) | Inscribe (Tier 1: a mark of provenance) | Symmetric base verbs |
| Memory Consumption (Tier 2: a face from a faction) | Layer (Tier 2: re-author over previous record) | Both rewrite history; ink leaves ghost-traces, mycelium leaves none |
| Memory Consumption (Apex: world-event erasure) | Anchor (Tier 3: world-event protection) | An Anchored fact is the only thing the Choir cannot fully eat |
| Mycelial Signature (passive: blackmail traces in spore network) | Spore-blood ink Erase (active: scrub specific signatures) | Player can lay or scrub traces depending on alignment |
| Choir voice-vote on requests | Inscribed Oath (formal contract instead of leverage) | Two political modalities: hidden persuasion vs. public covenant |
| Spore-borne reputation | Sigil of Recognition (faction-level rep marker, immune to spore propagation) | The Palimpsest doesn't trust spore-rep; they want it inscribed |

**Neither faction is unconditionally stronger.** A pure-Choir player has
amazing manipulation tools but cannot defend their own truths. A
pure-Palimpsest player has perfect defenses but cannot edit reality
freely. Mixed-alignment players have access to less of each, with
neither faction trusting them at the apex tiers.

---

## The Palimpsest-aligned playthrough

What does it look like to commit to this path?

- Early game (Tier 1): you can read inscriptions other characters can't.
  You sign your work; you find archive stones in the wild and recover
  lost recipes.
- Mid game (Tier 2): you ward your camps. You inscribe contracts. You
  layer your way through a House's archives, preserving the old records
  as ghost-layers under your own commentary.
- Late game (Tier 3): you anchor critical truths against the Choir's
  hunger. You erase what your enemies tried to write into the world.
  You become a player in the cosmic memory-axis war.
- Endgame (Tier 4, long-horizon): you Witness-Bind the war's final
  events into the temporal substrate itself; the Palimpsest's archive
  carries your inscriptions long after your character is gone.

---

## Faction lock-in and player choice

Inkwork is Palimpsest-aligned. This is intentional, RPG-appropriate
class-design. But the lower tiers aren't *exclusively* Palimpsest:

- **Tier 1** (Inscribe, Read) is universal — anyone can learn basic
  literacy. Everyone has a journal.
- **Tier 2** (Bind, Layer) requires Palimpsest reputation *Acquainted*
  but doesn't lock out other factions. A Choir-friendly player can
  still ward their camps.
- **Tier 3+** locks out Choir-hostile alignment. You cannot be both a
  Palimpsest Anchor-bearer and a Choir Composted Mind. The cosmologies
  refuse cohabitation.

Players who want neither alignment retain access to Tiers 1–2 of both
systems' base verbs (Inscribe + minor Memory Consumption) — they're
generalists who never reach the deep tools, but the political-cosmic
content is open to them at the surface level.

---

## Implementation phasing

### v1.0 (smallest playable slice)
- Tier 1 verbs: Inscribe, Read.
- Iron-gall ink + Sun-fade ink (the two cheap inks).
- Integrate the existing Grimoires & Sigils equipment system as the
  Tier 2 expression of Bind (no new architecture required; relabel +
  faction-gate the existing system).
- Integrate the existing Inkbound Repair system as a Read+Layer
  expression (no new content required; relabel + add ink-typology
  hooks to the existing repair flow).
- Estimated 2-3 milestones on top of existing systems.

### v1.1 (after playtest)
- Tier 2 verbs: Bind (full sigil-creation flow, not just grimoire
  equipment), Layer.
- Spore-blood ink, Ash ink, Saltwater ink.
- Inscribed Oath as the Palimpsest-side political verb (resolves
  BlackmailV2's open question).
- Estimated 3-4 milestones.

### v2.0 (gated on Memory Consumption being implemented first)
- Tier 3 verbs: Anchor, Erase.
- Dream-fruit ink, Bone-black ink, Witness-blood ink, Deep ink.
- Full Choir-Palimpsest balance table active.
- Estimated 3-4 milestones, depends on Memory Consumption shipping.

### Long-horizon
- Tier 4: Witness-Bind, Forge a Soul.
- Apex-tier content (Hollow Cathedral archive-pool, true-name
  inscription, identity-altering rituals).

---

## Open design questions

1. **Sigil grammar.** Do sigils have a compositional grammar (a
   *language* the player learns piece by piece), or are they discrete
   recipes (memorize this exact pattern → this exact effect)? Compositional
   is more interesting but vastly more authoring; discrete is faster
   to ship.

2. **Ink as inventory item vs. ink as resource pool.** Every ink an
   item in the inventory (limited, lootable, weighted) vs. a player
   stat (current/max iron-gall ink, restored over time). Item-based
   fits the world better; pool-based is friendlier to play. Probably
   item-based for rare inks, pool-based for common (Iron-gall is a stat
   you maintain; Deep Ink is an item you carry).

3. **How visible should ghost-layers be by default?** A high-Read
   scribe finds them, but should *every* inscription show "this has
   been layered before" by default? Affects how much the Layer verb
   feels like cheating.

4. **Faction-side parallel.** What's the Pale Curation's elemental
   medium? The Glassblown Remnant's? If ink is the Palimpsest's pillar,
   each major faction probably wants one — but that's a much larger
   design conversation. Out of scope here, flagged as a future thread.

5. **Witness-blood and consent.** Witness-blood ink requires *freely
   given* witness blood. Should there be a coercive variant (witness-
   blood-stolen) that produces an inscription with detectable
   illegitimacy? Aligns well with the Palimpsest's transparency ethos
   if yes.

6. **Save format implications.** Anchored facts need a flag in the
   `NarrativeStatePart` event log. Likely a single bool per event
   entry. Cheap.

---

## Why this is Class A

A pillar mechanic, by the criteria implicit in the project's existing
design choices:

- ✅ Has its own progression system (Inkwork tree, four tiers)
- ✅ Has its own resource economy (ink typology, crafting/sourcing loops)
- ✅ Cuts across multiple gameplay loops (combat via sigils, social via
  oaths, narrative via anchoring, identity via true-name, repair via
  restoration)
- ✅ Has a moral/cosmological frame (Choir-Palimpsest axis)
- ✅ Has an explicit counter (Memory Consumption / mycelium)
- ✅ Has player-facing UI (inkwell, sigil-binding interface, archive lookup)
- ✅ Faction-aligned without being faction-exclusive at low tiers
- ✅ Generates content rather than consuming it (every inscription is
  authorable; every ghost-layer is a story-seam)

It's the Palimpsest's pillar in the same way mutations are the player's
pillar and House Drama is the world's pillar. Building it elevates the
Palimpsest from "faction with strong dialogue" to "faction with a
playable cosmology."
