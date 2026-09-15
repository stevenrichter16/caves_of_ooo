# Tells & Windups — telegraphed attacks, graduated reading, and the feint layer

> **Status: BRAINSTORM / DESIGN PROPOSAL.** Nothing here is implemented.
> User pitch (2026-09-15): every enemy attack telegraphs first; a
> Perception skill line called **Read** turns tells into information
> (low rank flags that something's coming, high rank names the attack
> and opens a window to interrupt/dodge/counter); "poker, not just
> reflexes." Plus: use **micro-ticks** to make this a minimal formal
> system, with Perception detecting windup phases and **Reflexes**
> governing what you can do inside them.
>
> **Refined same day:** countering mid-attack should be *configurable* —
> the player chooses which skills and spells can be cast **partially**,
> rather than every character sharing one fixed set of reaction verbs.
> §5 is rewritten around that, and it also subsumes the earlier
> reaction-cost model (partial completion and energy cost turn out to be
> the same number).

## 0. The decisive finding: the micro-tick clock already exists

The pitch proposes micro-ticks as new infrastructure. They are already
the architecture, and nothing currently uses them for anything.

`TurnManager` is energy-based, Qud-faithful
(`Assets/Scripts/Gameplay/Turns/TurnManager.cs`):

- Every **tick**, each entity gains energy equal to its `Speed` stat.
- An entity acts when energy reaches `ActionThreshold = 1000`.
- `DefaultSpeed = 100`.

So a normal actor acts **once every ten ticks**. The tick *is* the
micro-tick; a "turn" is already subdivided into ten sub-units of
temporal resolution. Today that granularity is used for exactly one
thing — deciding who acts next — and is otherwise invisible. A windup
system is the natural consumer of a resolution the engine already has.

Two consequences worth stating plainly:

1. **Windup length is expressible in the currency the game already
   speaks.** "This attack resolves six ticks from now" is a statement
   the scheduler can already make and already schedules around.
2. **Resolution is a tunable constant, not an architecture change.**
   If ten sub-units per action is ever too coarse for a three-phase
   windup, raise `ActionThreshold` (or lower baseline speeds) — the
   ratio is what matters. No new clock, no second time system.

## 1. Verification sweep — what exists, what doesn't

| Assumption in the pitch | Actual state | Impact |
|---|---|---|
| Micro-ticks need building | Already the scheduler's model: energy per tick, act at 1000 (10 ticks/action at Speed 100) | Build on it; do not add a parallel clock |
| Attacks could telegraph | **Zero** windup/telegraph/cast-time concept exists (grepped `Windup\|Telegraph\|ChargingUp\|CastTime\|Preparing` — no matches) | Genuinely new mechanics |
| Attacks have a declare/resolve split to hook | They do not. `KillGoal.TakeAction()` calls `CombatSystem.PerformMeleeAttack(...)` **in the same instant** the AI decides to attack | This split is the one real architectural change the feature needs |
| A Perception stat exists | It does not. Stats present: Strength, Agility, Toughness, Intelligence, Willpower, Ego, Speed. Adding Perception is a Qud divergence *and* needs blueprint backfill across ~31 armed creatures | Recommend skill lines gated on existing stats instead — see §3 |
| Defender-side skill hooks exist to hang "Read" on | Thin: `BaseSkillPart` has `OnAttackerAfterAttack`, `OnAttackerMeleeMiss`, `OnDefenderAfterAttackMissed`, `OnWeaponMadeCriticalHit`. There is **no before-the-attack-lands defender hook at all** | One new additive virtual, following the shape of the four that exist |
| Windup state must survive save/load | `SaveSystem` reflects public fields on Parts (confirmed earlier this session for `ContainerPart`) | A `WindupPart` with public int fields round-trips free |
| Abilities have a cost to scale a partial cast against | They do **not**. `ActivatedAbilitySpec` carries only DisplayName / Command / Class / TargetingMode / Range / **Cooldown** — no mana or energy cost field. MP exists as a stat but was added for raising mutation ranks, not for casting | Partial casting must scale against the **energy** the window affords (§5), which is the currency the scheduler already runs on — not a new per-ability cost field |
| A surface exists to configure abilities | **Yes**: `AbilityManagerUI`, opened with **M** (WSP8.0), with its own modal input state and activation callback | The prepared-reaction loadout extends an existing screen instead of adding one |

## 2. The core move: split declare from resolve

Today: decide → attack, one instant, no gap for anyone to act in.

Proposed: when an actor commits to a **telegraphed** attack, it does not
resolve. It enters a **winding** state that publishes:

- the attacking entity and its intended target (or target cell),
- an attack **identity** (name + category),
- a **resolve tick**,
- a **truth flag** the observer never sees directly (see §4).

The attacker's energy is spent at declaration, so it does not act again
until resolution. Everyone else keeps reaching their own action
thresholds during those ticks — **those turns are the reaction window,
and they are ordinary turns, not a special interrupt mode.**

Because the window is measured in ticks and every actor's cadence is
`Speed`-derived, **`Speed` silently becomes the most interesting stat in
the game**: it is now literally "how many chances you get to respond to
what you can see coming." Relative speed between two combatants becomes
a live tactical fact rather than a scheduling detail. No change to the
Speed stat itself is required to get this.

## 3. Two skill lines, not two new stats

The pitch names Perception and Reflexes. Recommendation: build them as
**skill families** (the existing pattern — 11 families today, JSON in
`Content/Data/Skills/`, powers with `Requires` chains that the FUN-P0
spine just shipped), each **gated on an existing stat**:

- **Read** — gated on **Intelligence**, which currently does almost
  nothing (only 8 blueprints even carry it). This gives a dead stat a
  real job instead of adding a twelfth.
- **Reflexes** — gated on **Agility**, which already exists broadly and
  already feeds defense.

### Read — graduated information (the perception half)

Rank determines *how much of the tell resolves into fact*:

| Rank | What you see |
|---|---|
| 0 (untrained) | *That* something is happening. "The snapjaw shifts its weight." No name, no timing. |
| 1 | The **category**: heavy / quick / grapple / ranged / spell. Enough to know whether to dodge or interrupt, not when. |
| 2 | The **attack's name and resolve tick** — a real countdown: "Gutting Lunge — resolves in 4." |
| 3 | The **counter-window**: which class of response actually beats this attack, and this creature's habitual opening. |
| 4 (capstone) | **Feint detection** — see §4. |

### Reflexes — what you may do inside the window (the action half)

A small set of built-in responses comes free with the line, so an
untrained-but-observant player always has *something* to do with a tell:

- **Interrupt** — attack into a windup; enough damage (or a stagger
  threshold) cancels the attack outright and the attacker loses the
  action it already paid for.
- **Slip** — a repositioning move that leaves the telegraphed cell or
  area. Pairs directly with the manual multi-cell targeting work in
  `Docs/Design/MOMENTUM-COMBAT.md`: *where* an attack was aimed becomes
  load-bearing.
- **Brace** — commit to eating it at reduced damage in exchange for
  something concrete (no knockback, or a banked Momentum stack).

But a fixed verb list means every character reacts identically, which
is the weakest possible version of this system. The real content of the
Reflexes line is **§5: how many of your own abilities you can hold
ready, and how well they survive being compressed into a window.**

## 4. The feint layer — why this is poker and not Simon says

**Without this section the system solves itself.** If every tell is
honest, maximum Read rank reduces combat to: read tell, press the
listed counter, win. The pitch's own framing ("poker, not just
reflexes") only holds if **some enemies lie.**

So: capable enemies — humanoids, veterans, bosses; not vermin — can
**feint**. A feint publishes a tell for attack A and either resolves
attack B instead, or cancels outright to bait a reaction that the
player has now spent.

The consequence is the best property in this design: **a little Read is
worse than none against a liar.** Rank 0 sees "something's coming" and
stays cautious. Rank 2 sees a confident, specific, *false* countdown and
commits to the wrong answer. Only rank 4 sees the inconsistency that
marks the lie. A difficulty curve that dips in the middle is unusual,
true to poker, and makes investing in Read feel like reaching for real
mastery rather than buying a solution.

Two supporting details:
- Ranks 2–3 should surface a **confidence**, not just a fact, once the
  player has met any feinting creature — the information that your
  information might be wrong is itself a rank.
- A read that turns out false is a **teaching moment worth logging**:
  the message when a feint lands on you should name what it was, so the
  player learns the liar's repertoire the honest way.

## 5. The prepared loadout and partial casting (user refinement, 2026-09-15)

> *"I'd also potentially make countering mid-attack configurable, so the
> player can choose skills and spells to be able to cast partially."*

This replaces a fixed reaction verb list with a **build decision made
before the fight**, which is both more expressive and more in keeping
with the poker framing: you commit to a strategy, then read the hand.

### The currency is already in the engine, and it is exact

A full action costs `ActionThreshold = 1000` energy. A windup window of
*N* ticks gives an observer *N × Speed* energy of room — at Speed 100, a
four-tick window is 400 energy, or **40% of an action**.

So "partially cast" is not a metaphor needing a new resource. It is
literally: *you spend the energy the window actually affords, and the
ability delivers in proportion.* Two things fall out of that for free:

1. **The cost model and the partial model are the same number.** The
   earlier draft of this section proposed that reactions be drawn
   against your next action. Partial casting *is* that rule, made
   precise: dump 400 energy into a counter and you arrive at your next
   turn 400 later. No separate accounting.
2. **`Speed` compounds again.** A fast character doesn't only get more
   reaction windows (§2) — their partial casts come out *more complete*
   in the same window. One stat, two escalating consequences, still no
   change to the stat itself.

### Per-ability degradation profiles (what "partially" means)

Not every ability degrades sensibly. A half-strength firebolt is
coherent; half a stun is not. So each ability carries a **profile**,
and this is the axis the player configures:

| Profile | Behavior | Fits |
|---|---|---|
| **Scaled** | effect scales with completion — a 40% cast does 40% damage/duration | damage and duration spells |
| **Thresholded** | needs at least X% completion to fire at all, then fires at full effect; below the bar it fizzles and the energy is spent anyway | binary effects (stun, disarm, knockback) — a genuine gamble |
| **Degraded form** | an authored lesser version that fits a window (a full Whirlwind becomes a single parrying strike; a full Pyroclasm becomes a spark that only interrupts) | the most interesting, the most authoring-expensive |

**Authoring cost guard:** profiles must have sensible *defaults by
category* — damage abilities default to Scaled, control abilities to
Thresholded — so authoring a profile is an opt-in override, not a
mandatory per-ability content pass across ~30 mutations and every skill
active. Otherwise this feature blocks on a content sweep before it can
ship at all.

### Preparation is the cost, and Reflexes rank is the budget

Flagging an ability as reaction-ready must be limited or everything gets
flagged and the decision evaporates. **Reflexes rank buys prepared
slots** — rank 1 holds one ability ready, rank 3 holds three. That
turns the Reflexes line into "how much of your kit you can hold at the
ready," which is a far better identity than a fixed list of four verbs,
and it means two Reflexes characters with different prepared loadouts
play differently.

A pyromancer counters with a compressed firebolt. A cudgel bruiser
counters with a shove. A scribe counters by stepping out of the way,
because they prepared nothing and the free verbs are all they have.

### The configuration UI already exists

`AbilityManagerUI` (opened with **M**, WSP8.0) is already the
player-facing surface for managing abilities, with its own modal input
state and activation callback. Marking abilities as prepared and
choosing their profile belongs there — no new screen, no new input
state, one existing modal extended.

### Where spells specialize

`Spellcraft` is already a family of powers that modify *every* spell
through hooks (`Spellcraft_Empower` adds flat damage via
`OnGetSpellDamageModifier`, and its own docstring names planned siblings
for cooldown reduction and repeat-cast bonuses). A partial-cast power
belongs in exactly that pattern — e.g. **Quickening:** your prepared
spells need meaningfully less completion to clear their threshold.

That splits the two lines cleanly, with no overlap:
- **Reflexes** — how many abilities you can hold ready, and whether you
  can act in a window at all.
- **Spellcraft** — how well *spells specifically* survive compression.

### Symmetry

Capable enemies get prepared reactions too. This is what makes feints
(§4) cut both ways: baiting out an enemy's prepared counter and then
cancelling is the same play they can run on you.

## 6. The honest risk: pacing collapse

The standard failure mode of reaction systems in turn-based games is
that a fight with five enemies becomes five prompts per round and the
pace dies. Mitigations, in priority order:

1. **Only significant attacks telegraph.** Basic swings stay instant.
   A windup then *means* something: it marks the attacks that matter.
   This is also the cheapest possible v1 scope.
2. **No modal prompts, ever.** A windup is **world state** — visible on
   the tile and in the sidebar — not an interrupt that stops the game to
   ask a question. If the player ignores it and takes an ordinary turn,
   nothing blocks. Reacting is choosing to spend your normal turn on a
   response. This is what keeps the feature turn-based rather than
   real-time-with-pause, and it preserves the existing input model
   wholesale.
3. **Cap concurrent telegraphs** in a single zone if playtesting shows
   visual noise; the rest degrade to instant attacks.

## 7. Symmetry — enemies read you

The player's own heavy attacks (and a big Momentum dump from
`MOMENTUM-COMBAT.md`'s Called Strike) should telegraph too, and capable
enemies with a Read-equivalent should slip or interrupt them. This turns
the player's biggest commitments into genuine risks and keeps the system
from being a one-way power fantasy. It also gives the AI a use for the
same data structures with no second implementation.

## 8. Tells as learnable knowledge (the content hook)

The strongest content opportunity: **a tell can be *studied*, not only
rolled for.** Read rank sets your ceiling; specific creature knowledge
is content you acquire.

- **Study a creature** (observe it fighting, or read a corpse — the
  Keeping root in `Docs/Design/BUILD-EXPRESSIVITY.md` already proposes
  body-reading) to permanently learn *that species'* tells, independent
  of rank.
- **The Concord sells field-guides**: "a Concord catalogue of the
  snapjaw's four openings," half-accurate, caveat emptor — exactly the
  faction's established comic register.
- **The Recension files them properly** and will trade a correct tell
  for a testimony you bring back.

This makes Read partly a knowledge economy rather than a pure stat
check, and it means a scholar build genuinely reads a fight better than
a brawler — which is a fantasy the game currently cannot deliver.

**Thematic note (free, and unusually well-earned):** this setting is
already *about* reading — a world written over an older one, an order
whose whole theology is collation, underreading as the recovery of what
was scraped away. A combat line literally called **Read**, which sees
the intention underneath a motion, and **feints** as bodies that lie
about what they are about to be, sit inside that vocabulary without any
forcing. Share the lexicon with the Keeping root deliberately.

## 9. Scope-prune (v1) and build order

**Cut from v1, with rationale:** feints (ship honest tells first and
confirm the base loop is fun before adding the layer that makes it
poker); player-side telegraphs (§7 — symmetric but doubles the surface);
tell-study content and field-guides (§8 — content work, needs the
mechanic proven); any new stat.

1. **`WindupPart` + declare/resolve split.** The one real architectural
   change: `KillGoal`'s adjacent branch routes *significant* attacks
   through a declaration instead of `PerformMeleeAttack` directly.
   Public int fields only, so save/load is free. Diag records on
   declare / resolve / cancel, per the observability rule.
2. **Presentation:** the winding state rendered on the tile and in the
   sidebar, plus message-log lines. Ship the tell as *visible world
   state* before any skill can interpret it — at this point an
   observant player can already react manually with zero skill
   investment, which is the correct baseline.
3. **New defender-side hook** on `BaseSkillPart` (the before-it-lands
   virtual that does not exist yet) + the **Read** family, ranks 0–2.
4. **Reflexes** family, free verbs only: Interrupt and Slip first,
   Brace after the tempo cost is tuned in play. No prepared loadout
   yet — confirm that reacting at all is fun before building
   configuration on top of it.
5. **The prepared loadout + partial casting** (§5): energy-proportional
   completion, the two cheap profiles (Scaled, Thresholded) with
   category defaults, prepared slots gated on Reflexes rank, and the
   `AbilityManagerUI` extension to configure them. Authored **degraded
   forms** are deferred content, added per-ability where they earn it.
6. **Then** feints, then symmetry (including enemy prepared
   reactions), then the knowledge economy, then a Spellcraft
   partial-cast power.
