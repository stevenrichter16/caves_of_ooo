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

### 7.3 Shattered Rime (Cold) — ✅ shipped SM9

- **Rite of the Shattered Rime** — cone 3. Consumes Frozen for massive physical shatter damage; consumes Wet to Freeze first, *then* shatter — a one-cast two-step if you primed with water.
- **Rite of the Still Heart** — single. Consumes Frozen to inflict Hibernating (long, breaks on damage). Removes an elite from a fight instead of killing it.

### 7.4 Verdigris Bloom (Acid) — ✅ shipped SM9

- **Rite of the Verdigris Bloom** — radius 2. Consumes Acidic for ShatterArmor on all; consumes Wet to *spread* Acidic to everything in radius instead. Armour-stripping setup for the party's physical damage.
- **Rite of the Hollow Coin** — single. Consumes any three statuses of *any* kind for a flat execute-style burst. The universal cash-out for players who stacked without planning — deliberately less efficient than a matched rite, so matching still matters.

### 7.5 Sundering Word / Bloodletter's Ledger — ✅ shipped SM9 (CoO-original)

Not planned here; added in SM9 because the six-rite slate was otherwise
all damage. See the SM9 implementation log for both.

- **Rite of the Sundering Word** — radius 2, wildcard table. Spends marks on Broken + Weakened across everything caught. Barely hurts. Turns a dangerous pack into a survivable one.
- **Rite of the Bloodletter's Ledger** — single, wildcard table. Deep Bleeding on the target and a heal for the caster, per mark spent. The only rite that gives something back — and it gives nothing when cast cold, so it can never be a rest button.

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
| **SM1** | P1 push helper extracted + pinned | `Cudgel_GroundPound` uses the helper — **charter corrected, see §14: this is no longer behaviour-neutral** |
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

### Later decision — skills stay cooldown-based (2026-08-09)

Prompted by the Palimpsest v0.1 spec, which proposes a Stamina economy
for skills. **Rejected.** Skills remain gated only by cooldowns; ink
remains the only additional cost, and only on rites.

The consequence to carry into SM13: with cooldowns of 20–45 (Ember
Spit's 8 being the sole outlier), a player casts two or three powers in
a fight. Resonance's 2-slot default is right; the 3-status ×5.50 tier is
aspirational rather than routine. The damage bench should measure
whether that peak is reachable often enough to be worth having, or
whether each tree needs its own cheap filler the way Pyromancy has Ember
Spit. Full reasoning in `PALIMPSEST-SPEC-VS-SHIPPED.md` §2.1.

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

---

## 14. Implementation log

### SM1 — push becomes a primitive ✅
### ⚠️ SM1 charter correction (SCOPE DIVERGENCE)

The roadmap chartered SM1 as *"no behaviour change"*. **That is no
longer true**, and the change was made deliberately during the SM3
audit rather than accidentally.

`TryPush` originally moved a creature with a raw `Zone.MoveEntity`,
copying what `Cudgel_GroundPound` had always done. Adversarial review
flagged that as a defect: a raw `MoveEntity` teleports. It fires no
`AfterMove`, runs no cell-entry, and marks no cells dirty — so a
creature shoved into a pool never gets wet, which directly undercuts
the soak-then-shock premise this entire feature is built on, and the
renderer is never told to repaint (CLAUDE.md perf rule 4).

`TryPush` now calls the new `MovementSystem.ForceMoveTo`. **Every
`TryPush` caller is affected, including `Cudgel_GroundPound`**, whose
knockback now runs the post-move pipeline it never did before.

`ForceMoveTo` exists rather than reusing `TryMoveTo` because the first
attempt at this fix *did* use `TryMoveTo` and broke
`GroundPound_KnocksbackAdjacentCreature`. `TryMoveTo` fires the
vetoable `BeforeMove`, which `StatusEffectsPart.HandleBeforeMove`
answers from `AllowMovement` — cleared by Stunned. Ground Pound stuns
*then* pushes, so routing a shove through the voluntary-movement gate
let the attack's own stun cancel the attack's own knockback. A forced
move is not a voluntary move, and the two now have separate entry
points.

**Deliberately left undecided:** whether a Rooted creature should
resist a shove. It arguably should — but answering it by reusing the
voluntary-movement veto would have silently roped in Stunned too. That
belongs in its own change.


`SkillCombatHelpers.TryPush(actor, target, zone, cells = 1)`, extracted
from `Cudgel_GroundPound` (the codebase's only knockback), which now
routes through it. Multi-cell shoves stop at the first obstacle and keep
the ground gained; `false` means nothing moved at all, which is what
lets a caller tell "shoved into the open" from "slammed into a wall".
Items on the floor never block — after the loot overhaul the ground is
covered in them. 12 tests.

### SM2 — the cone ✅

`SpellTargeting.GetCreaturesInCone`. Step *n* is a band `2n-1` wide, so
length 3 covers 9 cells. A wall occludes **its own ray only** — one
pillar must not cancel a flamethrower. A wall at the one-cell **apex**
does stop everything, which is intended, teachable positioning. 14 tests.

### SM3 — the Ground Surge family ✅

Three Galvanism actives, all primers, sharing `GalvanismLine.Collect`
for the walk and differing entirely in what they do with the result.

| Power | Shape | Damage | Status | Push |
|---|---|---|---|---|
| **Ground Surge** | line 4 | 6 Electric | 40% Electrified (1.0 charge) | 1 cell, all targets |
| **Backlash Coil** | self ring | 4 Electric | **none, deliberately** | 2 cells, all adjacent |
| **Rail Spike** | line 6, pierces | 5 Electric | Electrified 1.5 on **last target only** | none |

**Why each exists.** Ground Surge is the request's shockwave and the
family's default. Backlash Coil is the surrounded-with-no-time-to-aim
button — it needs no direction, and it deliberately does **not** prime,
because a power that both broke a surround *and* charged everyone would
strictly dominate Ground Surge and collapse the family into one power.
Rail Spike exists for one problem: the target worth priming is the
caster at the back and there is a rank of bodies in the way. Its charge
grounds in the last body reached, which makes it a positioning decision
rather than a longer Ground Surge.

**Collect-then-act.** Every power snapshots its targets before touching
any of them. Acting during the walk would let a shoved creature land in
a cell the walk had not reached yet and be hit twice from one cast —
`GalvanismLine` returning a list makes that bug unrepresentable in the
callers rather than merely absent from them.

**Grammar pin.** `GroundSurge_DoesNotConsumeExistingStatuses` asserts a
Wet target is still Wet after the cast. Skills prime; only rites spend.
If a future edit lets a skill consume a status, the prime/detonate
division collapses and grimoires lose the one thing that makes them
distinct — so the contract is a test, not a comment.

**A trap worth recording.** The resistance stat that
`CombatSystem.ApplyResistances` reads for Electric/Lightning damage is
**`ElectricResistance`** (CombatSystem.cs:1164). `LightningResistance`
— which the pre-existing `GalvanismOverloadTests` fixture sets — is read
by no production code at all. A test that sets it looks like it is
establishing immunity and establishes nothing. Caught here because
`AllThree_DealElectricDamage_SoGalvanismsBonusApplies` asserted a
fully-resistant target takes zero and got 6.

**Live verification.** All three resolve through the real
`SkillsPart.AddSkill(string)` reflection path and register activated
abilities alongside Overload:

```
Ground Surge  [CommandGroundSurge]  mode=DirectionLine  range=4
Backlash Coil [CommandBacklashCoil] mode=SelfCentered   range=1
Rail Spike    [CommandRailSpike]    mode=DirectionLine  range=6
```

**Honesty bounds.** *Can verify (script-observable):* shapes, damage
typing, status application and its counter-check, push distances, wall
and edge behaviour, diag reasons on every reject branch, JSON
registration, live ability registration. *Cannot verify without a human
at the keyboard:* whether 30/40/45-turn cooldowns feel right, whether
the three read as distinct in play, and whether 40% Electrified is a
satisfying rate.

Tests: 6047 → 6078 (+31).

### SM3 audit — what the review gates caught

Both CLAUDE.md audit angles ran as an adversarial workflow (5 finder
dimensions → 24 raw findings → 23 unique → skeptic-verified). Tests
were green at 6078 before any of this; **green was not enough.**

**🔴 CRITICAL — Ground Surge shoved only the furthest target.**
Independently found by three of the five dimensions. `GalvanismLine`
returns targets nearest-first and `TryPush` refuses a step into an
occupied cell, so in a packed rank every target except the last shoved
into the body behind it and did not move. In the canonical use of a
line AoE — a corridor with enemies stacked up — the shockwave moved
exactly one enemy while the JSON promised "knocks back 1". Fixed by
splitting into two passes: damage and prime nearest-first, then shove
**furthest-first** so each destination is vacated before the target
behind it steps into it. The ordering is load-bearing and commented as
such.

**🔴 CRITICAL — a shove was a teleport.** See the SM1 charter
correction above.

**🟡 Rail Spike told the player it grounded a charge in a corpse.**
The effect branch was right; the message was unconditional. A player
who lined the shot up specifically to prime the back rank would read
"grounds in the snapjaw!", plan the next turn around a charge that did
not exist, and find nothing. The message now branches with the effect.

**🟡 Backlash Coil reported movers, not victims.** Cornered in a
corridor — the exact situation the power exists for — it printed
"hurls 0 attackers clear!" after landing damage on two enemies. Now
reports who was hit, and says plainly when nothing moved.

**🟡 `push_blocked` blamed geometry when everyone had died.** Both
push powers now track survivors and emit `all_targets_died` instead,
so a future `diag_query` does not send a debugger hunting a wall that
was never there.

**🟡 Descriptions were invisible at the point of purchase.**
`SkillsScreenUI` renders one row and hard-truncates at 55 chars
(`POPUP_W - 4`). The originals ran 65–72 and lost their mechanic to
the cut. Rewritten to 49–51 so the mechanic survives, and the limit is
now asserted by a test. *(Noted, not fixed: the three pre-existing
Galvanism entries run 89–185 chars and are equally truncated. Not SM3's
to fix — its own change.)*

**🔵 "2 bodys".** The siblings dodge this by stem luck ("targets",
"conductors").

**🧪 Five test-quality defects in my own tests**, all fixed:
`GroundSurge_NeverHitsTheCaster` passed on an empty line and would
have passed against a power that damaged its own caster;
`AllThree_DealElectricDamage` cast only one of the three;
`RailSpike_Spec_IsALongerLine` compared two constants and never read
`spec.Range`; the 200-HP fixture made every lethality branch
unreachable; and no test read `ElectrifiedEffect.Charge`, so
`charge: 0f` would have satisfied the entire suite.

### Deferred with reason

- **Galvanism's +25% conductor bonus reaches none of its own actives.**
  `GetSpellDamageModifier` is only consulted by
  `MutationDamageHelpers.ApplySpellDamage`, and every Galvanism active
  calls `CombatSystem.ApplyDamage` directly. **`Galvanism_Overload`
  does the same**, so SM3 matched the established tree pattern rather
  than deviating from it. This is a pre-existing tree-wide gap and
  deserves its own fix covering all six powers — bolting a one-off onto
  SM3 would leave the tree inconsistent.
- **`SkillTreeShowcase` hotbar overflow.** `SlotCount` is 10 and the
  scenario pre-buys 22 ability-declaring skills, so everything after
  the tenth — including `Galvanism_Overload`, which shipped long before
  SM3 — lands unbound. Pre-existing and scenario-only. The showcase now
  says so explicitly instead of telling the player to press keys that
  do nothing; properly fixing it means binding or raising the cap.

### SM4 — the Flame Jet family ✅

Three Pyromancy actives, and the first consumers of the SM2 cone.

| Power | Shape | Damage | Burn | Push | Cooldown |
|---|---|---|---|---|---|
| **Flame Jet** | cone 3 | 5 Heat | 1.5 intensity | — | 35 |
| **Ember Spit** | single, range 4 | 3 Heat | 0.6 intensity | — | **8** |
| **Backdraft** | cone 2 | 4 Heat | 1.0 intensity | 1 cell | 25 |

**Why each exists.** Flame Jet is the heavy primer — the cone is what
lets one cast prime a *clump* instead of a file, which no other shape in
the game could do. Ember Spit's identity is not power but
**availability**: an 8-turn cooldown against Flame Jet's 35, so it is
the cast you make while the jet cools. A prime-then-detonate loop needs
cheap filler or the rhythm collapses into waiting. Backdraft is the only
fire power that *moves* anything, trading reach and heat for a shove.

**Water beats fire — a deliberate anti-synergy, and a real find.**
`WetEffect`'s own docstring promises it "suppresses ignition when
Moisture > 0.35", and `ThermalPart.TryIgnite` (ThermalPart.cs:110-122)
honours that. But **`BurningEffect` has no moisture check whatsoever**,
and every existing fire spell — `FireBoltMutation`,
`ConflagrationMutation`, the FlamingSword on-hit — applies it directly,
bypassing `ThermalPart` entirely. So today a drenched target catches
fire from any spell, which contradicts both the effect's own contract
and what `EffectDescriber` tells the player ("douses flame, conducts
shock").

That matters because the grammar this feature teaches depends on water
being fire's counter: soaking sets a target up for lightning *and*
shields it from flame. A combo that deliberately does NOT work teaches
the element rules as sharply as one that does. The new powers route
ignition through `PyroIgnition`, which mirrors `ThermalPart`'s rule —
too wet to catch, and some moisture boils off, so repeated fire
eventually dries a target out. Water is a delay, not an immunity.

**Known divergence, deliberately deferred:** only the SM4 powers use
`PyroIgnition`. The older direct-appliers still ignite through water.
Unifying them means touching several shipped mutations and their tests;
it is recorded here rather than smuggled into this milestone.

**Renamed:** `GalvanismLine` → `SkillLine`. It started inside the
Galvanism tree, but Ember Spit needs the identical walk, and a
tree-specific name would make every future caller look like it was
borrowing someone else's code.

**SCOPE DIVERGENCE:** the plan specified Backdraft as a cone *"behind
the caster"*. Shipped as an ordinary aimed cone. Firing opposite the
aimed direction would mean the player points one way and the flame goes
the other — a UI trap rather than a tactic. The intended *use*
(rear-guard while falling back) is preserved by aiming at the pursuer,
which is what a player does naturally.

**A grammar problem worth naming.** `Pyromancy_Pyroclasm` — which
shipped long before this plan — **consumes** `BurningEffect`, and its own
docstring calls it "the only ability that CONSUMES A STATUS EFFECT FOR
DAMAGE". Under the agreed grammar (skills may *read* a status but only
rites may *spend* one) that is a skill doing a rite's job. Left
untouched: it is pre-existing, it is load-bearing for the Pyromancy
tree's identity, and the right time to decide its fate is SM7, when
rites exist and it can either become one or be justified as a
deliberate exception.

**Honesty bounds.** *Can verify:* cone coverage including off-axis
targets, directionality, wall occlusion, damage typing via a
fully-resistant target, burn intensities and their gradient, the
wet-suppression gate (mutation-tested — disabling the guard kills
exactly one test), single-target non-piercing, furthest-first shove
ordering, diag reasons on every reject path, registration through the
real `SkillRegistry`, and live ability registration. *Cannot verify
without a human:* whether the 8/25/35 cooldown spread feels right, and
whether the cone's width reads clearly on screen.

Tests: 6090 → 6113 (+23).

### SM5 — Hydromancy, the fifth tree ✅

The milestone where the soak-then-shock loop becomes self-serve.

| Power | Shape | Damage | Moisture | Movement |
|---|---|---|---|---|
| **Jet Blast** | cone 2 | 2 (untyped) | 0.8 | push 1 |
| **Drench Lob** | flies to 6, bursts radius 2 | **none** | 0.6 | none |
| **Undertow** | line 3 | 2 (untyped) | 0.5 | **pull 1** |

**Root passive: your water sticks.** Rather than a fourth
element-damage modifier (the Pyromancy / Cryomancy / Galvanism pattern),
`HydromancySkill` deepens every soaking its owner applies by 0.25. That
is the mechanically honest bonus for a tree whose job is priming: it
keeps a target above the thresholds that matter — **0.2** for
`ElectrifiedEffect`'s charge doubling and **0.35** for fire suppression
— for more turns, since moisture only evaporates a little each turn.
Every water power calls `ApplyMoistureBonus`, so a future power cannot
forget the bonus.

**Why water is its own tree.** Galvanism's identity is a *conditional*
bonus against Wet or Electrified targets. If a lightning mage could soak
for free, that condition would always hold and the tree would collapse
into a flat damage bonus. Keeping water separate means soak-then-shock
costs a real investment — two trees, or two characters.

**Damage is deliberately negligible, and deliberately untyped.**
`"Water"` maps to `DamageAttributeFlags.None` (Damage.cs:144-173), so no
elemental resistance touches it. That is pinned by a test rather than
left to chance: if `"Water"` ever becomes a real flag, the decision has
to be made on purpose.

**Undertow is the only pull in the game.** Every other movement power in
every tree shoves things away. Pulling is not a reversed push: you end
the turn closer to what you dragged, and to everything near it, so it is
a build decision rather than a strictly better shove. `TryPull` shares
its step loop with `TryPush` via `DragAlong`, because the destination
guards are exactly the thing that must not drift apart.

**Pull ordering is the mirror of shove ordering.** Undertow drags
NEAREST-first so each target vacates the cell the one behind it needs;
Jet Blast shoves FURTHEST-first for the same reason in reverse. Both are
pinned.

**Mutation-testing found dead code — in my own new code.** Removing the
pull's stop-adjacent guard killed *no* tests: for a Creature puller the
occupancy check already refuses that step. The guard was kept (a future
whirlpool fixture or tentacle prop would otherwise drag its victim
inside itself) and is now pinned by a test using a **non-Creature**
puller, which is the only case it is reachable in. Untestable dead code
became tested code rather than being quietly deleted or quietly kept.

**SCOPE DIVERGENCE.** The plan specified Drench Lob as "lobbed, radius 2
at range 6" with a freely chosen impact point. `AbilityTargetingMode` has
exactly three values — AdjacentCell, DirectionLine, SelfCentered — and
none lets a player pick an arbitrary cell at range. Rather than grow the
input/UI layer inside a content milestone, the lob flies along an aimed
line and bursts where it stops: on the first creature it meets, or at
maximum range. Same radius, same reach, aimed the way every other ranged
power is aimed. A true free-cell targeting mode is worth doing, but as
its own change.

**Honesty bounds.** *Can verify:* cone and line coverage, moisture
values and that they clear BOTH interaction thresholds, the tree
passive reaching the powers end-to-end, soak-then-shock doubling the
charge across three systems, soak-protects-from-fire, pull stop-adjacent
(incl. the non-Creature case), pull/push ordering in a packed rank,
water damage being unresisted, diag reasons on every reject path, and
live discovery of an entirely new JSON tree file. *Cannot verify without
a human:* whether 0.25 is the right passive, whether the lob's
fly-and-burst reads as intended on screen, and whether pulling feels as
good as it sounds.

Tests: 6113 → 6150 (+37).

### SM6 — the Rime Grip family ✅ (skill half complete)

| Power | Shape | Damage | Effect |
|---|---|---|---|
| **Rime Grip** | single, range 5 | 4 Cold | Frozen 0.5 — **deeper on a wet target** |
| **Glacial Wall** | line 3 | **none** | raises `IceWall`, melts after 8 turns |
| **Cold Snap** | nova 2, self-centred | **none** | Hobbled 6 turns on everything but you |

**The symmetry fix, and it is the important part.**
`ElectrifiedEffect.OnApply` has always doubled its charge on a target
with `Moisture > 0.2`. `CryomancySkill`'s own bonus already keys on Wet.
But `FrozenEffect` did **not** amplify — so soaking made a target better
to shock and no better to freeze, for no reason a player could infer.
`FrozenEffect` now amplifies on the *same* 0.2 threshold, so the player
learns one rule ("wet enough to matter") instead of two.

The multiplier is ×1.5, not Electrified's ×2, because `Cold` is capped
at 1.0 and *any* positive value already blocks action — the gain is
**duration** (it thaws from higher), not a stronger state. Three tests
cover it: amplified on a soaked target, **not** amplified on a barely
damp one (the counter-check that makes the threshold real), and still
clamped at 1.0. It broke zero existing tests despite touching an effect
four shipped powers already use.

**Glacial Wall is the only power in any tree that edits the map.**
Everything else damages, primes or moves a creature; this changes where
creatures can walk. Hence no damage at all. The ice melts because a
permanent wall would let a player brick a corridor and farm it — the
power buys a few turns of tempo, not a solution. It never buries
anyone: occupied cells are skipped so the wall forms *around* whatever
is standing in the line, since raising solid rock onto a creature would
either delete it from play or stack a solid on an occupied cell.

**Cold Snap deals no damage on purpose.** What it buys is turns. A
prime-then-detonate plan needs the pack to still be where you left it
when the payoff lands.

**Bootstrap cleanup.** The factory-wiring block had duplicated,
mis-indented `ContainerPlacementService` / `TraderPart` assignments left
from an earlier session, and the post-load re-attach site was missing
several entries the boot site had. Both sites are now consistent and
carry the new `Cryomancy_GlacialWall.Factory`.

**Honesty bounds.** *Can verify:* freeze depth and the wet
amplification with its counter-check and clamp, single-target
non-piercing, Cold typing via a fully-resistant target, wall solidity /
melting / not-burying / not-stacking / graceful null-factory, Cold Snap
radius and caster exclusion, no-damage on both utility powers, diag
reasons on every reject path. Live: all three register with correct
modes and ranges, the factory is wired at boot, and `IceWall` spawns
solid with an 8-turn lifespan. **Known consequence, not fixed here.** Six more actives lands on the
already-noted hotbar overflow (22+ actives, 10 slots) and makes it
sharper rather than causing it. These are rites, so they arrive on a
found grimoire rather than a skill tree, and a player will rarely hold
more than two or three at once — but the ceiling is real and still
deferred.

*Cannot verify without a human:* whether
8 turns of ice is the right number, and whether Cold Snap's 6-turn
hobble feels like tempo or like tedium.

Tests: 6150 → 6176 (+26). **SM1–SM6 complete — the skill-primer half of
the plan is done.** SM7 (resonance + rites) is next.

### SM7 — resonance: the payoff verb ✅ (the keystone)

The milestone the whole feature was built toward. Statuses can now be
**spent**.

**`ResonanceSystem`** — `Preview` (read-only) and `Spend` (mutating).
A rite says only *"I am Electric, I have 2 slots"*; the **data** decides
what that is worth. No combination logic lives in any spell, which is
the architectural principle both this plan and the external design
conversation (`STATUS-SYSTEM-MODULAR-LADDER.md` §2) independently
arrived at.

`Preview` exists so SM12's targeting overlay can show the payoff
*before* the player commits a charge — and a test pins that previewing
consumes nothing.

**The scaling, live-verified:**

| Statuses spent | Multiplier | Storm Anvil damage |
|---|---|---|
| 0 (cast cold) | ×1.00 | **4** |
| 1 (Wet) | ×2.00 | 8 |
| 2 (Wet + Electrified) | ×3.50 | **14** |
| 3 (channelled, SM9) | ×5.50 | 22 |

**A rite cast cold is deliberately weak** — this is the answer to the
question raised in session ("doesn't that make rites much more powerful
than skills?"). Yes, *when fed*. Storm Anvil's base 4 damage on an
unprimed target is worse than a skill of the same cooldown, so a rite is
a bad opener and only becomes worth casting once something is stacked.
That is what creates the prime→prime→detonate rhythm instead of "always
rite". The damage lives in what you spend, not in the spell.

**Dead pairs are content.** `Burning` has no entry in the Electric table
— fire does not conduct. A declined status is not eaten and not paid
for, and it emits `ResonanceDeclined` with a reason that distinguishes
*"not resonant with this element"* from *"resonant, but you were out of
slots"*. "Why didn't my combo work?" is now a `diag_query`, not a
debugging session.

**Ink (pillar 3).** `GrimoireChargePart` puts ~10 charges on the book.
`GrimoirePart` never destroyed a grimoire on reading, which turns out to
be exactly right: reading **teaches** the rite, carrying the inked book
lets you **cast** it. A grimoire becomes a physical thing you maintain,
and a depleted one is a real inventory problem. Storm Anvil refuses —
and says why — with no ink, and critically **checks for targets BEFORE
spending a charge**, because burning a charge on empty air would be the
most infuriating possible bug in a resource-costed spell.

**Obtainable:** `StormAnvilGrimoire` is stocked by the Arcanist, Scribe,
Pale Curator and Palimpsest Echo.

**SCOPE DIVERGENCE (milestone boundary moved).** The roadmap put
resonance in SM7 and the first rites in SM8. One rite was pulled forward
so SM7 ships a system with a consumer: a `ResonanceSystem` that nothing
calls is precisely the unreachable-mechanic trap this project keeps
hitting (dead grimoires, unplaceable containers, empty shops). SM8 now
adds the remaining rites to a proven system.

**A silent-failure caught by its own test.** The new `spell` diag
category was not in `Diag.DefaultOnCategories`, so every resonance
record was being **dropped on the floor**. The observability tests
failed and surfaced it. Any future new category needs that registration
— the failure mode is total silence, not an error.

**Honesty bounds.** *Can verify:* the multiplier curve and its
super-linearity, that spending removes and previewing does not, dead
pairs declining without being eaten, both decline reasons, slot limits,
element tables valuing the same status differently, Wet resonating with
every element, unknown elements eating nothing, null safety. Live: the
table loads at boot, and the curve reads ×1.00 / ×2.00 / ×3.50 for
0/1/2 statuses with the right riders. *Cannot verify without a human:*
whether 4 base damage is the right floor, whether ×3.50 feels like a
payoff worth two setup turns, and whether 10 charges is rhythmic or
stingy in practice.

Tests: 6176 → 6189 (+13).

### SM8 — three more rites ✅

Now that resonance exists, rites can differ from each other in kind
rather than in numbers.

| Rite | Shape | Element | What it does with the marks |
|---|---|---|---|
| **Hanging Bolt** | line 6, single | Electric | converts each mark into **2 turns of no-save Paralysis** instead of damage |
| **Rendered Steam** | radius 2 | Heat | wants **Wet AND Burning on the same body**; the pair adds ×1.5 and blinds |
| **Scalding Veil** | self | Heat | spends **your own** Wet for a retaliation aura |

**Hanging Bolt is the argument for building resonance as a shared
system.** It reads the *identical* Electric table Storm Anvil does and
spends the same statuses — then converts them into control rather than
damage. Storm Anvil removes a pack; Hanging Bolt removes one elite from
the fight. Two rites, one table, opposite purposes, and neither knows
anything about the other.

**Rendered Steam is the clearest "two beats one" lesson in the game.**
Water and fire cancel everywhere else — moisture suppresses ignition
(`PyroIgnition`), burning boils moisture off — so a target carrying both
at once is a deliberate, awkward, short-lived arrangement. This is the
reward for arranging it. Either status alone still resonates and still
hurts; only the **pair** detonates and blinds. Live: `Wet + Burning` on
the Heat table reads ×3.50 spending both, before the pair bonus.

**Scalding Veil is the only rite that reads the caster.** Being soaked
is normally a liability — it is exactly what sets you up to be shocked
and frozen — and this is the one way to cash your own debuff in. It
fires on `OnTakeDamage`, so it answers attackers rather than ticking on
bystanders: you have to actually be hit for the steam to bite.

**Two refusal invariants, both tested.** A rite with no target and a
Scalding Veil cast while dry both refuse **without spending ink**.
Burning a charge to accomplish nothing would be the most infuriating
possible bug in a resource-costed spell, so it is pinned rather than
assumed.

**Obtainable:** all four rite grimoires spawn inked (10 charges) and are
stocked across the Arcanist, Scribe, Pale Curator and Palimpsest Echo. A
test asserts each names a mutation class that actually resolves, and
another asserts each appears somewhere in the loot tables — a rite
nobody can obtain does not exist.

**Honesty bounds.** *Can verify:* ink spent per cast, refusal without
spending on no-target and on dry-caster, marks converting to paralysis
and scaling with count, cast-cold paralysing nothing, the pair
detonating while either half alone does not, the pair out-damaging a
single, the veil consuming the caster's own water, the veil scalding and
confusing an attacker, the veil NOT firing on sourceless damage, and all
four grimoires spawning inked with resolvable mutations. Live: every
grimoire spawns with 10 ink and teaches a real class; `Wet + Burning`
reads ×3.50 on Heat while the same Wet reads ×2.00 on Electric — the
tables genuinely disagree. *Cannot verify without a human:* whether
paralysis-instead-of-damage is a trade players will actually want, and
whether arranging Wet+Burning is achievable often enough in practice to
be worth a whole rite.

Tests: 6189 → 6205 (+16).

### SM9 — six world-found rites ✅

**Request:** *"add a slew of new grimoires/rites to the game that are
available randomly throughout the map in chests, containers, barrels,
etc. these grimoires consume status effects and are very powerful."*

Two halves, and the second half is the one with teeth. The first five
rites (SM7–SM8) were **bought** — stocked on the Arcanist, Scribe, Pale
Curator and Palimpsest Echo. These six are **found**, which changes what
they are allowed to be. A rite you buy has to be priced; a rite you dig
out of a sealed vault only has to be worth the trip.

| Rite | Shape | Table | Slots | What the marks buy |
|---|---|---|---|---|
| **Shattered Rime** | cone 3 | Cold | 2 | untyped shatter + ShatterArmor |
| **Still Heart** | single, 5 | Cold | 2 | Hibernating, 8 turns **per mark** |
| **Verdigris Bloom** | radius 2 | Acid | 2 | ShatterArmor + Acidic on everything |
| **Hollow Coin** | single, 4 | **Any** | 3 | flat untyped burst; Broken at 3 marks |
| **Sundering Word** | radius 2 | **Any** | 2 | Broken + Weakened on everything |
| **Bloodletter's Ledger** | single, 5 | **Any** | 2 | deep Bleeding **and heals the caster** |

**"Very powerful" had to be paid for somewhere, so it is paid for in
setup.** Every one of these carries a small base number — 2 to 6 — and
gets its power entirely from the resonance multiplier. Cast cold into an
unprepared enemy, the best of them does about as much as a dagger. Cast
into a target you spent two turns priming, they are the hardest hits in
the game. That is the whole design, and it is pinned by a matched pair
of tests: `EveryRite_CastCold_IsNearlyWorthless` caps every rite at 10
damage with nothing consumed, and `EveryRite_HitsHarderWhenFed` is its
counter-check — without the second, "weak" would be indistinguishable
from "broken."

**A shared spine, because five longhand rites had already drifted.**
`ConsumingRiteBase` owns targeting, the ink check, the resonance spend,
the damage roll and the diag trail; a concrete rite declares eight
properties and overrides `ApplyPayoff` and `CastMessage`. It enforces two invariants for
everyone rather than five times each:

- **Ink is checked AFTER targets.** A rite that can do nothing must
  never take a charge for it. This was already true in four of the five
  earlier rites and is now structurally impossible to get wrong.
- **Riders never land on the dead.** `ApplyPayoff` runs only for
  survivors, so nothing walks away wearing a debuff it did not live
  through.

**The `Any` table is new content, not a special case in code.** §7.4
promised Hollow Coin would consume "any three statuses of *any* kind",
but the four existing tables are elemental — Burning appears on none of
Electric/Cold/Acid (it IS on Heat at 0.75, which Rendered Steam and
Scalding Veil read), so a wildcard rite reading Electric would silently
refuse to spend a burning target. Rather than branch in the rite, the
fifth table went in `Resonance.json` listing all five resonant statuses.
It pays **0.4 per mark against a matched table's 0.5–0.75**, so building
around an element strictly beats stacking at random — pinned by
`WildcardTable_PaysLessPerMarkThanAMatchedRite`, and the reason three
rites can share it without obsoleting the elemental four.

**Two rites answer problems the catalog didn't have an answer for.**
Sundering Word and Bloodletter's Ledger are not in §7 — they were added
because the six-rite slate was otherwise all damage. Sundering Word
spends marks on Broken + Weakened across a radius and barely hurts;
turning a dangerous pack into a survivable one is a different kind of
power from killing it. Bloodletter's Ledger is the only rite that gives
something back, healing the caster per mark spent — and
`BloodletterLedger_CastCold_HealsNothing` exists specifically so it can
never become a rest button between fights.

**Scattered, not stocked.** 32 entries across 22 world container types —
bookshelves, reliquaries, strongboxes, urns, crates, tomb vaults, cult
caches, the ziggurat vault, the sealed vault, the alchemy shelf. Weighted by
tier, so a T1 bookshelf might yield Shattered Rime and only a T3 sealed
vault is likely to hold Hollow Coin. Each rite ended up with 5-6
independent sources.

`VillageGrimoireChest` was deliberately left OUT after the first run:
`BiomeLootTableTests.GrimoireChest_StockedFromTable_SameThreeRites`
asserts that chest holds exactly the three utility rites, and it is a
designed village fixture rather than a random world container — never
in scope for "randomly throughout the map". The three displaced entries
were re-homed to CrateT2, UrnT2 and AlchemyShelfT2. Every rite has **at least two independent
container sources**, asserted through the game's own `LootTableRegistry`
rather than by grepping the JSON — with
`ContainerNamesUsedByThoseAssertions_AreRealTables` as the counter-check,
because a misspelled table name would make the coverage assertion pass
while pointing at nothing.

#### Verification-sweep corrections (logged before writing code)

| Premise | Reality | Fix |
|---|---|---|
| `BleedingEffect(duration:)` | **No duration parameter.** Bleeding is indefinite; recovery is a Toughness save that eases by 1/turn | Marks scale `saveTarget` (14 + 4/mark) and `damageDice` (1d4 → 1d6) — the two knobs it actually has |
| Shattered Rime deals Cold | `CombatSystem.cs:1181` — resistance 100 **zeroes** the hit, so a cold-immune target would absorb its own shattering | Untyped physical. Matches the fiction ("frozen flesh is brittle") and is the only version that works on an ice creature |
| Hollow Coin can read Electric for "any status" | Electric table is {Wet, Electrified, Frozen} — Burning unreachable | New `Any` table in `Resonance.json` |

The Bleeding one is the same trap the on-hit adversarial sweep caught
(`OnHitEffectFactory.cs`, Magnitude-vs-DurationTurns). It compiled fine
in my head and would not have compiled at all in Unity; the sweep caught
it first, which is the entire argument for the sweep.

#### Adversarial sweep (CLAUDE.md §Adversarial test sweep)

Five taxonomy surfaces apply — state atomicity, cross-actor flows,
stacking semantics, anti-exploit gates, boundary inputs — well past the
two-surface trigger. `ConsumingRiteAdversarialTests.cs`, 21 tests:

| Surface | Probed with |
|---|---|
| Boundary inputs | null zone; zero direction on a directional rite (and the counter-check that a radius rite must ACCEPT zero direction); caster with no inventory; empty book |
| State atomicity | no-target cast leaves ink and bystander statuses untouched; an ink-less cast must not have eaten the target's setup first; one cast spends exactly one charge across six targets |
| Anti-exploit | the Ledger farmed five times on a status-free target heals nothing; the Ledger cannot heal past maximum; a second cast on the same target is measurably weaker |
| Cross-actor | two casters do not share a book; a radius rite never hits its own caster, riders included |
| Stacking / mid-execution death | re-cast onto an already-debuffed target; a target killed by the damage gets no riders; one dying victim does not stop the rest of the radius resolving |
| Diag contracts | a success emits `RiteCast` carrying `statusesConsumed` and `inkLeft`; a refusal emits no `RiteCast` at all |
| Content integrity | six distinct mutation classes, six distinct command strings, and every declared element resolving to a table that actually resonates |

The last three content-integrity tests exist because six near-identical
blueprints and six near-identical classes are exactly where a
copy-paste slip hides: a duplicated command string would make one rite
permanently uncastable, and an element naming no table would make a rite
permanently cold — powerful on paper, useless in play, and silent about
it.

**Honesty bound:** the sweep found 0 bugs beyond the two the per-feature
run caught. That does not prove the rites are bug-free — adversarial
tests are bounded by the bug classes the author imagined. Surfaces NOT
probed: save/load round-tripping of a mid-cooldown rite, and multi-zone
casting; neither has a code path distinct from the five earlier rites.

#### Scope divergence from §7

- **§7.3 Shattered Rime** planned "consumes Wet to Freeze first, *then*
  shatter — a one-cast two-step." Shipped: Wet resonates on the Cold
  table and multiplies directly. The two-step would need resonance to
  apply an intermediate status mid-spend, which `ResonanceSystem` has no
  path for; adding one for a single rite was not worth the coupling.
  The player-visible result — soak, then shatter harder — is the same.
- **§7.4 Verdigris Bloom** planned "consumes Wet to *spread* Acidic to
  everything in radius instead." Shipped: the radius shape already hits
  everything, and every hit target that spent a mark gets Acidic, so the
  spread happens without a special Wet branch.
- **Sundering Word** and **Bloodletter's Ledger** are CoO-original, not
  from the §7 catalog. Classified per §4.2 as neither Qud-parity nor
  plan-derived.

#### Honesty bounds

*Can verify (script-observable):* every rite capped at ≤10 damage cast
cold; four rites measurably harder when fed a single Wet; ink spent on a
cast and **not** spent on a miss, for all six; Still Heart producing
Hibernating on a living target; Sundering Word applying Broken +
Weakened across a radius; Verdigris Bloom applying ShatterArmor;
Shattered Rime hitting both on-axis and off-axis targets and damaging a
cold-immune creature; Verdigris Bloom being fully absorbed by acid
immunity (the counter-check that untyped is an exception, not the house
style); Hollow Coin damaging a target immune to all four elements and
consuming Burning, which no Cold or Acid rite can reach; the wildcard table
paying strictly less per mark than the matched one; Bloodletter's Ledger
healing when fed and healing nothing when cold; all six grimoires
spawning inked with a mutation class that resolves; each having ≥2 real
container sources; ≥15 container types carrying at least one rite.

*Cannot verify without a human:* whether the multiplier ceiling is
actually reachable in a real fight without the enemy killing you during
setup; whether finding a rite in a vault reads as a discovery or as
clutter alongside the 25 grimoires that already exist; whether Hollow
Coin's untyped burst is too safe an answer to hard enemies; whether the
0.5-vs-0.75 wildcard penalty is enough to keep the elemental rites
relevant. **None of these six have been exercised in Play mode** — the
EditMode suite proves the mechanics, not the feel.

Tests: 6291 → 6332 (+41: 20 feature, 21 adversarial). All green.

### SM9 cold-eye audit — the pass that caught what green tests could not

Run AFTER `0ca70a2e` merged with 6332/6332 green and a 21-test
adversarial sweep reporting 0 bugs. Six independent lenses over the
whole diff at once; every finding put through three DISTINCT refutation
angles (is the claim factually accurate / is the state actually
reachable / does an existing test already cover it) and kept only on a
majority survival. **29 findings survived, 5 were refuted.**

The headline: **tests-green-feels-clean is exactly the state where the
worst bug was hiding.**

#### 🔴 Still Heart ran the mechanic backwards

`HibernatingEffect` is a **self-buff**. Its only other caller is
`Cryomancy_Hibernate` applying it to the *caster*. It heals 5% of max HP
every turn, forces Heat AND Cold resistance to 100, and has no
wake-on-damage hook at all. Still Heart handed it to an **enemy**.

Cast at a primed elite, the anti-elite rite therefore: healed the elite
up to ~80% of its max HP over the sleep, made it immune to the rite's
own Cold damage and to every Cryomancy and Pyromancy spell the player
owned, and could not be woken by hitting it. The rite's own docstring
promised "a long sleep that breaks on damage." Every word of that was
false.

`AsleepByGasEffect` is the hostile sleep — blocks action, wakes on
damage, buffs nothing — and **its docstring explicitly warns against
this exact substitution**: *"Distinct from HibernatingEffect (which is a
self-buff: heals + max resistances)"*. The warning was sitting in the
file the whole time.

Why the shipped test missed it: `StillHeart_RemovesAnEliteInsteadOfKilling
It` asserted only `HasEffect<HibernatingEffect>()` and `HP > 0`. Both
were true. It never ticked a turn, never read a resistance, never struck
the sleeper. A test that asserts the presence of an effect proves
nothing about what the effect *does*.

#### 🔴 The baker was eating rite books

All eleven rite grimoires declared `"Inherits": "GrimoireCopy"` — and
tags merge parent-first with no removal syntax
(`BlueprintLoader.cs:201-204`), so all eleven silently carried the
`GrimoireCopy` **tag**. That tag means "a disposable copy the scribe
made," and it is consumed destructively: the Farmer's oven dialogue
fires `TakeItemWithTag: GrimoireCopy`, which removes the first matching
item in the pack. Finish the oven quest holding a rite you just pulled
out of a sealed vault and the book is gone — no confirmation, no undo,
and the rite becomes permanently uncastable because `GrimoireInk` reads
charges off the carried book.

The same tag ran the other way too: `CopyGrimoire` skips anything
carrying it, so the scribe *offered* to copy rite grimoires and then
reported "You don't have a grimoire to copy" while the dialogue narrated
handing over an item that was never created.

The tell was there in the content: **all 20 non-rite grimoires inherit
`Item` directly and carry no such tag.** Only the rites inherited the
*copy* blueprint. Fixed with a `RiteGrimoire` base — same shape, without
the tag — and all eleven repointed.

#### 🟡 The rest

| Finding | Fix |
|---|---|
| Bloodletter's heal lived in `ApplyPayoff`, which the base skips when the target dies — so the better your setup, the more likely the sustain silently vanished | new `OnCastResolved` hook that always runs; riders stay survivor-gated, earnings do not |
| All eleven rites missing from `GrimoireTooltipData` — the picker's own docstring says a missing row makes a mutation **invisible** there | eleven rows added; a completeness test now enumerates every `ConsumingRiteBase` subclass so the next rite cannot forget |
| The three new single-target rites filed their `RiteCast` diag under the *caster*, unlike the two older single-target rites which file under the victim | single-target now files under the victim, with a counter-check that radius still files under the caster |
| `AlchemyShelfT2` is unreachable — villages hardcode tier 1 — so one re-homed entry was dead content | moved to `AlchemyShelfT1` |
| The wildcard table shipped at 0.5, which **tied** three matched entries (Electric/Frozen, Heat/Frozen, Acid/Wet), making "matching still beats stacking at random" false for them | wildcard lowered to 0.4, strictly worse than every matched entry |
| Six mutations survived paper mutation-testing — Shattered Rime's ShatterArmor, Verdigris' acid re-seed, Hollow Coin's third slot and `>=3` branch, the entire Bleeding payoff, and every rider guard on a cold cast | six payoff-pin tests |
| Doc/commit claimed "Burning is on no elemental table" — **false**, Heat carries it at 0.75 and two shipped rites read it | corrected to "no Cold or Acid rite can reach"; the narrower claim was the one that actually justified the `Any` table |

#### What this says about the earlier gates

The per-feature suite, the counter-checks, and the 21-test adversarial
sweep were all green and all honest — and all six lenses of this pass
still found real defects. The adversarial sweep in particular probed
atomicity, cross-actor flows and boundary inputs thoroughly and found
nothing, because **none of those categories asks "is this the right
effect?"** A bug-class taxonomy cannot see a semantic substitution; only
reading the effect's own source next to the rite's intent does.

That is the empirical case for running the audit angles as separate
passes rather than one merged checklist — and for treating "0 bugs
found" as the trigger for the next pass rather than the end of the work.

Tests: 6332 → 6346 (+14 cold-eye regressions and payoff pins).
