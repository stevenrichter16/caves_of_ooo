# Tells & Windups — telegraphed attacks, graduated reading, and the feint layer

> **Status: BRAINSTORM / DESIGN PROPOSAL.** Nothing here is implemented.
> User pitch (2026-09-15): every enemy attack telegraphs first; a
> Perception skill line called **Read** turns tells into information
> (low rank flags that something's coming, high rank names the attack
> and opens a window to interrupt/dodge/counter); "poker, not just
> reflexes." Plus: use **micro-ticks** to make this a minimal formal
> system, with Perception detecting windup phases and **Reflexes**
> governing what you can do inside them.

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

- **Interrupt** — attack into a windup; enough damage (or a stagger
  threshold) cancels the attack outright and the attacker loses the
  action it already paid for.
- **Slip** — a repositioning move that leaves the telegraphed cell or
  area. Pairs directly with the manual multi-cell targeting work in
  `Docs/Design/MOMENTUM-COMBAT.md`: *where* an attack was aimed becomes
  load-bearing.
- **Brace** — commit to eating it at reduced damage in exchange for
  something concrete (no knockback, or a banked Momentum stack).
- **Counter** (capstone) — correctly identify *and* answer in the right
  window to strike into the windup at bonus effect. The riposte.

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

## 5. What reacting costs

A reaction must cost something or "always react" is trivially correct.

**Recommended:** a reaction is drawn against your *next* action — you
are spending your upcoming turn early and out of order. This reuses the
energy ledger exactly as it already works, and it produces the right
tension: a player who answers every telegraph never advances their own
offense. Defense becomes a genuine tempo trade instead of free value.

(Alternative considered and not recommended: N free reactions per
windup gated by Reflexes rank. Simpler to tune, but it makes reacting
strictly free value at high rank, which flattens the decision.)

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
4. **Reflexes** family: Interrupt and Slip first; Brace and Counter
   after the tempo cost from §5 is tuned in play.
5. **Then** feints, then symmetry, then the knowledge economy.
