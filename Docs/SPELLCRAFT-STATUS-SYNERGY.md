# Spellcraft & Status Synergy — the Prime → Detonate loop

**Status:** APPROVED 2026-08-09 — all four open questions answered
(§13). Building SM1 onward.
**Date:** 2026-08-09
**Goal (user's words):** *"put more grimoires and skills in the game that
directly deal damage and status effects… I want skills and grimoires
with status effects be a no brainer and a thing a player would easily
think to do, which is to use multiple status effects on one enemy to
deal tons of damage and open up yourself for emergent gameplay. the
environment also should have some more objects or environmental features
that assist using multiple status effect type spells. first determine
what should be a spell from a skill vs what should be a spell from a
grimoire. grimoires should provide an additional oomph… you should also
generate a couple variations of each skill spell/grimoire spell."*

---

## 1. Verification sweep — read before believing the plan

Per CLAUDE.md §1.2, every premise below was checked against source
**before** any design was written. Three load-bearing assumptions were
wrong, and they reshape the whole feature.

| # | Assumption going in | What the source says | Consequence for the plan |
|---|---|---|---|
| 1 | 🔴 "Wet → electrocute needs building" | **Already fully built.** `ElectrifiedEffect.OnApply` doubles `Charge` and adds +1 duration on a target with `WetEffect.Moisture > 0.2`. `GalvanismSkill.OnGetSpellDamageModifier` adds `baseDamage / 4` against Wet **or** Electrified targets. `Galvanism_Overload` chains a line *only* through conductive (Wet/Electrified) creatures and breaks at the first dry one. | The feature is **not** "add a synergy." It is "make the synergy that already exists visible, rewarding, and reachable." Scope shifts from mechanics to payoff + legibility. |
| 2 | 🔴 "There are few damage spells" | **20 grimoires exist**, 13 offensive: ArcBolt, Thunderclap, IceLance, RimeNova, AcidSpray, Conflagration, EmberVein, FireBolt, FlamingHands, FrostNova, IceShard, ChainLightning, PrismaticBeam, PoisonSpit. All are in loot tables. Four elemental skill trees ship today: Pyromancy, Cryomancy, Galvanism, Corrosion — plus Spellcraft. | Do **not** add a parallel spell list. Add the *missing shapes and the payoff verb*, and slot new spells into the existing trees. |
| 3 | 🔴 "Grimoires differ from skills" | They do **not**. `GrimoirePart` calls `mutations.AddMutation(className, level)` and stops. A grimoire spell and a skill spell are mechanically indistinguishable once learned — both are cooldown-gated activated abilities with no resource cost. | The user's question ("what should be a skill vs a grimoire, and what is grimoire oomph?") has **no answer in the current code**. This plan must invent the distinction, not document it. |
| 4 | 🟡 "Knockback exists as a primitive" | It exists **inline only**, in `Cudgel_GroundPound.cs:124-131`, via `Zone.MoveEntity` with a destination-solid check. `SkillCombatHelpers` has no push helper. | The user's shockwave-that-pushes needs the push **extracted** to a shared helper first — one small refactor, not new tech. |
| 5 | 🟡 "Cone/spray targeting exists" | `SpellTargeting` offers `TraceBeam` (line), `GetCreaturesInRadius` (nova) and `FindChainTargets` (chain). **No cone.** | Flamethrower and jet-blast need a genuinely new shape primitive. |
| 6 | 🟢 "Casting costs a resource" | It does not — cooldown only. No mana/essence system anywhere. But `InkVialPart` exists, ink is craftable, and ink is already stocked by shops (SM9). | A resource cost is available to invent for grimoires without building an economy from scratch. |

**The honest summary:** roughly 70% of what the request describes is
already in the codebase and unreachable *as an experience*, not as code.
The player has no reason to notice it and no dramatic payoff when they
do. That reframing is the single most important output of this sweep.

---

## 2. What actually exists today

**Status effects (34):** Wet, Electrified, Burning, Frozen, Charred,
Smoldering, Acidic, Poisoned, Bleeding, Stunned, Paralyzed, Confused,
Rooted, Hobbled, Weakened, Broken, ShatterArmor, Steam, CoatedInPlasma,
FungalInfection, Hibernating, Berserk, Stoneskin, PaperSkin, Hooked,
LiquidCovered, and others.

**Existing pairwise interactions (already coded):**

| Pair | What happens today | Where |
|---|---|---|
| Wet + Electric | Charge ×2, duration +1 | `ElectrifiedEffect.OnApply` |
| Wet + Fire | Ignition suppressed above 0.35 moisture; `water_plus_fire` reaction spawns Steam | `WetEffect`, material reactions |
| Conductive liquid coat + Electric | Same amplification as Wet | `ElectrifiedEffect.OnApply` (LQ.5) |
| Electrified + adjacency | Chains along conductors each turn | `ElectrifiedEffect.OnTurnEnd` |
| Heat + Wet | Evaporates moisture (temperature-scaled) | `WetEffect.OnTurnEnd` |

**The gap this table reveals:** every interaction is a *modifier* — a
bit more damage, a bit longer, a slow chain. None of them is a **moment**.
Nothing in the game says "your setup paid off" with a bang.

---

## 3. The five real gaps

1. **No payoff verb.** Statuses accumulate but are never *spent*. The
   reward for stacking three statuses is three small modifiers, not one
   large event. This is why stacking is not a no-brainer.
2. **No skill/grimoire identity.** Both are cooldown-gated abilities.
   A grimoire feels like a skill you found instead of bought.
3. **No cone shape.** Every spell is bolt, line, nova or chain. The
   flamethrower and jet-blast the user asked for are unbuildable today.
4. **No shared push.** Knockback is trapped inside one cudgel power.
5. **No legibility.** Nothing tells the player a target is primed, and
   nothing previews what a spell would do to a primed target. The combo
   is invisible until you already know it exists.

---

## 4. The design: skills **prime**, grimoires **detonate**

This is the answer to *"what should be a spell from a skill vs a spell
from a grimoire."*

> **A skill spell is a trained reflex. It APPLIES a status.**
> **A grimoire rite is a channelled rite. It CONSUMES statuses.**

That one sentence is the whole grammar, and it is learnable in a single
fight. Skills are your verbs every turn; rites are the sentence's
punchline.

| | **Skill spell** | **Grimoire rite** |
|---|---|---|
| Acquired by | skill points in an element tree | finding/buying a book |
| Cost | cooldown only | **ink charge** + cooldown |
| Cast time | instant | instant **or** channel 1 turn |
| Core job | **apply** one status cleanly | **consume** statuses for payoff |
| Damage profile | steady, predictable | scales with statuses consumed |
| Availability | unlimited once bought | limited charges, must re-ink |
| Failure mode | wastes a turn | wastes a turn **and** a charge |

### 4.1 What "oomph" means — three pillars

Grimoire rites get all three. Skill spells get none. This is the
mechanical answer to the user's question.

**Pillar 1 — RESONANCE (the important one).**
A rite reads every status on its target and *consumes* the ones it
resonates with, each for a distinct bonus. One status consumed is
decent. Three is enormous and non-linear.

This is the payoff verb the game is missing, and it is structurally
impossible for a skill to do — skills only add. It converts "stack
statuses" from a diffuse modifier into a deliberate, explosive plan.

Damage scaling is deliberately **super-linear** so the third status is
worth chasing: consuming *n* resonant statuses multiplies the rite's
base damage by roughly `1 + 0.75n + 0.25n²` (1 → ×2, 2 → ×3.5, 3 → ×5.25).
Exact numbers to be tuned on a self-auditing bench, not guessed.

**Pillar 2 — CHANNEL (shape escalation).**
A rite may be *held*. Release immediately for the base shape; hold one
turn to escalate (bolt → cone → nova) and unlock an extra resonance
slot. Channelling is real tactical tension — you are stationary and
interruptible — and skills, being instant, never have it.

**Pillar 3 — INK COST.**
Rites spend a charge from the book. A depleted grimoire is re-inked with
an `InkVial` — an item that already exists, is already craftable, and is
already stocked by scribes and arcanists after SM9. This makes rites
feel precious, gives the ink economy a real sink, and gives the
Scribe/Arcanist shops a reason to exist.

### 4.2 The resonance table

Each rite has an element. What it does with a status depends on whether
that status *conducts*, *feeds*, or *opposes* that element. This teaches
the elemental grammar through play rather than through a manual.

Illustrative, for a **Lightning** rite:

| Status on target | Resonance | Reads as |
|---|---|---|
| Wet | consumed → ×2 damage, arc to 2 extra targets | water carries the charge |
| Electrified | consumed → 3-turn stun, no save | the charge doubles back |
| Frozen | consumed → shatter, bonus physical | brittle ice explodes |
| Burning | **not** consumed, no bonus | fire does not conduct |

The last row matters as much as the others: a combo that *doesn't* work
teaches the grammar as sharply as one that does. Every rite will have at
least one deliberate dead pair.

---

## 5. New primitives required (build before content)

| # | Primitive | Why | Risk |
|---|---|---|---|
| P1 | `SkillCombatHelpers.TryPush(actor, target, cells, zone)` | extracted from `Cudgel_GroundPound.cs:124-131`; shockwave/jet-blast need it | Low — refactor with the existing test as the pin |
| P2 | `SpellTargeting.GetCreaturesInCone(zone, ox, oy, dx, dy, length, halfWidth)` | flamethrower/jet-blast shapes | Medium — needs its own targeting tests + a facing convention |
| P3 | `ResonanceSystem` — reads statuses, computes consumed set + multiplier, emits one diag record per resonance | the payoff verb; the heart of the feature | **Highest.** Must be pure and table-driven, not hard-coded per rite |
| P4 | `GrimoireChargePart` — ink charges, re-ink action | Pillar 3 | Low-medium; save/load reach |
| P5 | `ChannelPart` — hold-a-rite state, interrupted on damage/move | Pillar 2 | Medium — a state machine, so an adversarial sweep is mandatory |

**P3 is the load-bearing one.** If resonance is hard-coded per rite it
will drift immediately. It must be a data table (`Resonance.json`)
mapping `element × status → {consume?, damageMult, rider}`, so new rites
are content, not code.

---

## 6. Skill-spell catalog — the primers

Three variations each, as requested; each variation exists for a
*different reason*, not as a numeric reskin.

### 6.1 Galvanism — *Ground Surge* family (the user's shockwave)

| Variant | Shape | Effect | Why you'd pick it |
|---|---|---|---|
| **Ground Surge** | line 4 | dmg; 40% Electrified; **push 1 away** | the ask, verbatim: knock them off you and prime them |
| **Backlash Coil** | self-centred ring 1 | dmg; push **all** adjacent 1; no Electrified | pure panic button — escape a surround, no prime |
| **Rail Spike** | line 6, pierces | dmg; Electrified only on the **last** target hit | reach past the front rank to prime the caster in the back |

### 6.2 Pyromancy — *Flame Jet* family (the user's flamethrower)

| Variant | Shape | Effect | Why you'd pick it |
|---|---|---|---|
| **Flame Jet** | cone 3 | sustained Burning on all | the ask: mass-prime a clump with fire |
| **Ember Spit** | single, range 4, short cooldown | light Burning | cheap spammable primer between big turns |
| **Backdraft** | cone 2 **behind** the caster | Burning + push away | fight a corridor from the front; leave a wall of fire behind you |

### 6.3 Hydromancy (NEW tree) — *Jet Blast* family (the user's jet blast)

| Variant | Shape | Effect | Why you'd pick it |
|---|---|---|---|
| **Jet Blast** | cone 2 | Wet 0.8; **push 1** | the ask: soak a group and set up every lightning follow-up |
| **Drench Lob** | lobbed, radius 2 at range 6 | Wet 0.6, no push, no damage | prime a distant clump before they close |
| **Undertow** | line 3 | Wet 0.5 + **pull 1 toward** the caster | drag a caster out of their back line into your melee |

### 6.4 Cryomancy — *Rime Grip* family

| Variant | Shape | Effect | Why you'd pick it |
|---|---|---|---|
| **Rime Grip** | single, range 5 | Frozen; Wet targets freeze **harder** | the wet→freeze branch, parallel to wet→shock |
| **Glacial Wall** | line 3, terrain | brief impassable ice | zoning, not damage |
| **Cold Snap** | nova 2 | Hobbled on all, no damage | slow a pack so your channels land |

**Where water lives.** Hydromancy is proposed as a new fifth elemental
tree rather than folding water into Galvanism. If Galvanism could
self-apply Wet for free, its own conditional bonus becomes unconditional
and the tree's identity collapses. Galvanism keeps exactly one weak
self-prime (Rail Spike's tail-Electrified) so a pure-lightning build
still functions solo. *Open question #2 revisits this.*

---

## 7. Grimoire-rite catalog — the detonators

Two variants each. Every rite is an **ink-costing, channellable,
resonance-consuming** rite. Names are placeholders.

### 7.1 Storm Anvil (Lightning)

- **Rite of the Storm Anvil** — nova 2. Consumes Wet (×2 + arc), Electrified (hard stun), Frozen (shatter). Channel → nova 3 + one extra resonance slot.
- **Rite of the Hanging Bolt** — single target, huge. Consumes the **same** statuses but converts every consumed status into +1 turn of *no-save* Paralysis instead of damage. The control variant of the same idea.

### 7.2 Rendered Steam (Fire ∩ Water)

- **Rite of Rendered Steam** — the only rite that *wants* two opposed statuses. Consumes Wet **and** Burning together for a steam burst: heavy damage in radius 2, Confused on all. This is the game's clearest "two statuses beat one" teaching moment.
- **Rite of the Scalding Veil** — self-centred. Consumes Wet **on yourself** to become wreathed in steam: attackers take damage and are Confused. Turns a debuff you're suffering into an asset.

### 7.3 Shattered Rime (Cold)

- **Rite of the Shattered Rime** — cone 3. Consumes Frozen for massive physical shatter damage; consumes Wet to Freeze first, *then* shatter — a one-cast two-step if you primed with water.
- **Rite of the Still Heart** — single. Consumes Frozen to inflict Hibernating (long, breaks on damage). Removes an elite from a fight instead of killing it.

### 7.4 Verdigris Bloom (Acid)

- **Rite of the Verdigris Bloom** — radius 2. Consumes Acidic for ShatterArmor on all; consumes Wet to *spread* Acidic to everything in radius instead. Armour-stripping setup for the party's physical damage.
- **Rite of the Hollow Coin** — single. Consumes any three statuses of *any* kind for a flat execute-style burst. The universal cash-out for players who stacked without planning — deliberately less efficient than a matched rite, so matching still matters.

---

## 8. Environmental features that assist status play

Terrain the player *seeks out* — this is where the emergent gameplay
the user asked for actually comes from.

| Feature | Biome | Effect | The play it creates |
|---|---|---|---|
| **Rain Basin / Cistern** | village, jungle | shoot/break it → Wet everything in radius 3 | free mass-prime; fight *here*, not there |
| **Static Spire** | ruins | electric spells cast within 5 chain +2 further | a lightning build's home turf |
| **Brine Flat** | desert | standing on it counts as Wet for conduction | conduction without spending a turn priming |
| **Tar Seep** | cave, strata | flammable pool; ignites into a durable fire field; slows | area denial you set up in advance |
| **Frost Vent** | strata, cave | periodic chill; Frozen lands easier nearby | cryo build's home turf |
| **Ember Vent** | cave, desert | periodic heat; dries Wet, ignites the Burning-prone | *anti*-synergy terrain — punishes water builds, rewards fire |
| **Ley Node** | ruins | casting a rite while standing on it refunds the ink charge | a reason to hold ground during a channel |

Ember Vent is deliberately hostile to the water plan. Terrain that only
ever helps is not tactics.

**Teaching placement.** One early ruins zone gets a Rain Basin within
sight of a Static Spire and a couple of dry enemies. The combo is
stumbled into, not read about. This is the concrete mechanism for
"a thing a player would easily think to do."

---

## 9. Legibility — the actual "no-brainer" requirement

Mechanics alone will not make this a no-brainer. Four surfaces:

1. **Primed marker.** Any creature carrying a status that something on
   your hotbar resonates with gets a pulsing halo in its status colour.
   Reuses the round-15 sprite-tint and glint infrastructure, so this is
   cheap.
2. **Resonance preview.** While aiming a rite, the targeting overlay
   shows what it would consume and the resulting multiplier —
   `⚡ RESONATE — Wet, Electrified → ×3.5` — **before** committing the
   charge. This is what converts the combo from lore into a decision.
3. **Message-log callouts.** Distinct, punchy lines per resonance
   ("The water carries the charge!"), so the payoff is *heard* as well
   as seen.
4. **Effect tooltips already do their part** — `EffectDescriber` says
   "douses flame, conducts shock" today. Extend the same voice to every
   status so the grammar is readable from the status panel.

---

## 10. Sub-milestones (smallest blast radius first)

| SM | Content | Ships |
|---|---|---|
| **SM1** | P1 push helper extracted + pinned | no behaviour change; `Cudgel_GroundPound` uses the helper |
| **SM2** | P2 cone targeting + tests | shape primitive, no spells yet |
| **SM3** | Galvanism *Ground Surge* family (3) | the user's shockwave, playable |
| **SM4** | Pyromancy *Flame Jet* family (3) | the flamethrower, playable |
| **SM5** | Hydromancy tree + *Jet Blast* family (3) | the jet blast; wet→shock now self-serve |
| **SM6** | Cryomancy *Rime Grip* family (3) | fourth primer family |
| **SM7** | P3 `ResonanceSystem` + `Resonance.json` + P4 ink charges | **the payoff verb** — the keystone |
| **SM8** | Storm Anvil + Rendered Steam rites (4) | first detonators; combo loop closes |
| **SM9** | P5 channel/hold + shape escalation | Pillar 2 |
| **SM10** | Shattered Rime + Verdigris Bloom rites (4) | rite catalogue complete |
| **SM11** | Environmental features (7) + teaching placement | emergent terrain |
| **SM12** | Legibility layer (primed marker, resonance preview, callouts) | the "no brainer" |
| **SM13** | Adversarial sweep + self-auditing damage bench + cold-eye ×2 | the gates |

SM1–SM6 are playable-value-first and low-risk. **SM7 is the keystone** —
everything after it depends on resonance being right, so it gets the
verification sweep and the deterministic bench treatment.

---

## 11. Performance section (CLAUDE.md requires one)

Four of the five triggers apply, so this is mandatory reading before
SM7.

- **Cone targeting (P2) allocates a `List<Entity>` per cast.** Cast is
  turn-scale, not frame-scale, so a per-call list is acceptable — but it
  must use the scratch-list pattern (`Docs/PERF-FOUNDATION.md §Pattern 1`)
  because `Galvanism_Overload` already showed line-scans get called in
  AI evaluation loops, which are per-turn per-NPC.
- **Primed marker (SM12) must not scan every entity every frame.** It
  hooks status apply/remove and marks cells dirty via
  `ZoneRenderHooks.MarkCellDirty`, never full-zone `MarkDirty`.
- **Resonance preview (SM12) recomputes only when the aim cell or the
  target's status set changes** — fingerprint-gated exactly like
  `SidebarRenderer.ComputeSnapshotFingerprint`.
- **`ResonanceSystem` must not allocate per query.** Table lookups into
  a pre-populated dictionary covering the full `element × status`
  domain, so a miss is cheap (CLAUDE.md perf rule 2).

---

## 12. Observability (CLAUDE.md requires this decided up front)

New `spell` diag category. Every gate emits on **both** branches:

| Kind | When | Payload |
|---|---|---|
| `SpellCast` | any spell resolves | spell, element, shape, targets hit |
| `ResonanceFired` | a status is consumed | status, element, multiplier before/after, rider applied |
| `ResonanceDeclined` | a status present but **not** resonant | status, element, why (the dead-pair teaching moment) |
| `ChannelStarted` / `ChannelBroken` | hold begins / interrupted | rite, turns held, breaking cause |
| `RiteRejected` | ink empty, no target, out of range | reason naming the gate |

`ResonanceDeclined` matters: when a player reports "my combo didn't
work," the first query answers it instantly.

---

## 13. Open questions — ANSWERED 2026-08-09

The user accepted all four recommendations. Decisions are binding on
the sub-milestones above:

1. **Ink cost: rhythmic**, ~10 charges, cheap re-ink. The combo should
   be a habit, not a ration.
2. **Hydromancy ships as a fifth elemental tree.** Galvanism's
   conditional wet-bonus stays conditional.
3. **Strict prime/detonate separation.** No skill detonates. Revisit a
   capstone per tree only after the grammar has landed.
4. **Primers first (SM1–SM6)**, then resonance + rites.

### Original wording of the questions

1. **Ink cost harshness.** Should a rite be *precious* (3–5 charges per
   book, re-ink costs a full InkVial) or *rhythmic* (10–15 charges,
   cheap re-ink)? Precious makes each rite a decision; rhythmic makes
   rites a normal part of every fight. *Recommendation: rhythmic to
   start (~10 charges).* The combo should be a habit before it is a
   ration, and the user's stated goal is "a no brainer," not "a
   treasure."
2. **Hydromancy as a fifth tree, or water folded into Galvanism?**
   *Recommendation: new tree*, so Galvanism's conditional bonus stays
   conditional and water gets room for its own douse/steam/plant
   identity.
3. **Should skills ever detonate?** Strict separation is cleanest to
   teach. *Recommendation: keep it strict for now*, and consider one
   capstone skill per tree that consumes exactly one status much later —
   only after the grammar has landed.
4. **Scope order.** SM1–SM6 (primers: shockwave, flamethrower, jet
   blast — everything named in the request, playable) before SM7–SM10
   (resonance + rites)? Or resonance first so the very first new spell
   already has a payoff to feed? *Recommendation: primers first.* They
   are independently fun, they de-risk the two new primitives, and the
   named asks land in the player's hands soonest.
