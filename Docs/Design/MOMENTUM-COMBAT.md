# Momentum combat — chained multi-target strikes with rolling damage

> **Status: BRAINSTORM / DESIGN PROPOSAL.** Nothing here is implemented.
> User pitch (2026-09-14): a punch that hits one guy, then hits the next
> guy harder because of carried momentum — with the explicit note that
> "target more than one cell for one hit" doesn't have to be tied to
> "momentum," they're separable ideas. **Corrected same day:** the
> multi-cell targeting is a *manual* pick — the player selects each
> target themselves, and the picked cells need not be adjacent to each
> other or to the attacker. §1.A below reflects the correction; an
> earlier auto-resolved line-trace draft is superseded.

## 0. Grounding: the codebase already has both halves, separately

Two skills already ship the exact primitives this pitch needs, just
never combined:

- **`Axe_Whirlwind`** (`Assets/Scripts/Gameplay/Skills/Axe_Whirlwind.cs`)
  is the multi-target-in-one-action pattern: snapshot every adjacent
  creature, then loop `CombatSystem.PerformSingleAttack` across the
  whole list, each hit running the attacker's full combat pipeline
  (Cleave, Dismember, on-hit procs — everything a normal swing gets).
- **`Cudgel_ChargingStrike`** already uses the word **"momentum"** in
  its own message-log line: `"'s charge adds +N momentum damage!"` —
  a single-target bonus computed as a percent of the damage just dealt.
  The vocabulary is already half-established in the game's own text.

So this design is a recombination of two shipped patterns, not new
engineering from zero. Per `CLAUDE.md` "feed before you build," that's
exactly the right shape for a first ship.

## 1. Two decoupled primitives (per the pitch's own framing)

### A. The targeting primitive — manual, arbitrary, not-necessarily-adjacent

**Correction (2026-09-14):** the pitch is a *manually picked* set of
cells, not an auto-resolved line or ring, and the picked cells need not
be adjacent to each other or to the attacker. That rules out the first
draft of this section (a line-trace) — it also rules out both existing
multi-target skills as the model:
- **Not Whirlwind** (omnidirectional, unordered, auto-collected —
  the game chooses the targets, the player chooses nothing).
- **Not a line-trace** (still auto-resolved once aimed; the player
  picks a direction, not a set of specific cells).

The right model already exists in the input layer, just for a single
cell: **`ThrowTargeting`** (`InputHandler.BeginThrowTargeting`). It
activates a free-roam world cursor (`WorldCursorMode.ThrowTarget`),
clamped to range and the visible zone, moved freely with no adjacency
constraint at all, confirmed with one key and cancelled with another.
Throwing a bottle at a cell across the room already proves the engine
can let a player point at *any* cell in range, not just a neighbor —
this is exactly the primitive a manual multi-target picker needs, run
several times before the swing resolves instead of once.

**New input flow — call it a Called Strike:**
1. Activate the same free-roam cursor as throwing (own
   `WorldCursorMode`, e.g. `MultiStrikeTarget`), range-limited to the
   weapon/skill's reach.
2. **Confirm** on a cell holding a valid creature *adds it to an
   ordered pick list* instead of immediately resolving the action.
   Confirming an already-picked cell again removes it (undo a pick).
   Empty cells can't be added — no wasted picks on nothing.
3. Repeat freely — moving the cursor and adding/removing picks costs
   no turn, same as aiming any other ability today. Cells can be
   anywhere in range: two enemies on opposite sides of the room,
   skipping everything in between, is a legal pick list.
4. A distinct **finalize** input (or reaching the pick cap) ends
   targeting and resolves the whole list in *pick order* — each
   picked cell's creature takes a full `PerformSingleAttack`, in the
   sequence the player chose, spending the turn once at the end.
5. **Cancel** at any point aborts with no turn spent, same as
   `ThrowTargeting`'s cancel path today.

This primitive, alone, with **no damage scaling**, is already a
complete, shippable skill: pick up to N creatures anywhere in range,
hit all of them once, turn ends. That's the pitch's "target more than
one cell for one hit," fully decoupled from momentum.

**Why manual beats auto-resolved here:** an auto-line or auto-ring
never asks the player anything. A manual pick list turns "who do I hit
last" into a real decision — and once this is wired to the momentum
primitive below, that decision has teeth: pick off a weak, easy target
first to bank a cheap stack, then spend the accumulated bonus on the
priority target you picked last. That's an execute-order puzzle an
auto-resolved shape can't produce.

### B. The momentum primitive — a rolling resource, independent of targeting

A **Momentum** counter on the attacker, separate from any specific
skill:
- Increments by 1 on every landed hit — any weapon, any target, any
  action, including a plain bump attack.
- Caps at a small number (proposed: 5).
- Decays by 1 on any turn the attacker lands no hit, and **resets to 0**
  on a miss, or on taking a hit from a hard-CC effect (`StunnedEffect`,
  `ParalyzedEffect`) — you can be knocked out of your rhythm.
- Feeds a flat **+8% damage per stack** to the *next* attack (cap
  +40% at 5 stacks) — small enough per-stack to feel earned, large
  enough at cap to change a fight.

This primitive needs zero targeting logic. It works for a Short Blades
flurry build, a plain Axe swing, or a boss fight where the player is
just landing hits turn after turn. It is the general case the pitch's
"you don't have to tie these together" comment is describing.

## 2. Where they combine — the pitch's actual image

Combine A and B and you get exactly what was pitched: **a Called
Strike reads and writes the same Momentum counter as every other
attack.** Hitting the first picked creature increments Momentum before
the second pick resolves, so the second target in the *same swing* is
hit at a higher damage multiplier than the first — carried speed,
mechanically real, and the player chose the order.

The elegant part: because Momentum is a persistent counter, not a
per-skill local variable, **"hit two guys in one swing" and "hit one
guy last turn, then another guy this turn" are the same currency.**
A player doesn't need a Called Strike to build or spend Momentum; they
need it to spend several stacks in a single action instead of trickling
them out one swing at a time. That is the cleanest version of the
user's own decoupling: one shared resource, two different ways to
interact with it (accumulate steadily across turns, or burn a big chain
of stacks at once against however many scattered targets are in range).

**The pick cap can itself scale with Momentum** — a nice second-order
tie-in that isn't required for the core design but is cheap to add:
instead of a flat pick limit, cap = `1 + (Momentum / 2)`. A player who
walks into a room cold can only call one target; a player who's already
been landing hits this fight can flag three or four scattered enemies
in one swing. Momentum then governs both the size and the strength of
the chain, which makes banking it across a fight feel like it's
building toward something concrete rather than just a numeric buff.

Worked example (5 stacks max, +8%/stack, base damage 10, pick cap =
1 + Momentum/2):
| Step | Momentum before | Pick cap | Damage this hit |
|---|---|---|---|
| Bump attack, turn 1 | 0 | — | 10 (+0%) |
| Bump attack, turn 2 | 1 | — | 10.8 (+8%) |
| Called Strike, pick 1 (any cell in range) | 2 | 2 | 11.6 (+16%) |
| Called Strike, pick 2 (a different, non-adjacent cell) | 3 | 2 | 12.4 (+24%) |

A pure single-target build racks up the same +8%-per-landed-hit climb
over several turns; a Called Strike just lets a player cash in a whole
staircase of it against however many scattered targets they deliberately
chose, in the order they chose, in one action — the "reward for setting
up the shot" the pitch is describing, now with an actual choice behind
"setting up."

## 3. Counterplay and build implications

- **Losing Momentum has to hurt**, or stacking it is free and the
  system is just a damage-up timer. A miss or a hard-CC hit zeroing the
  counter means an aggressive momentum build is genuinely riskier
  against anything that can stun or has high evasion — a real
  trade-off, not a strict upgrade.
- **AI counterplay for free**: an enemy AI that specifically targets
  the highest-Momentum actor (easy to read — it's a public counter)
  gives fights a natural "peel the puncher" dynamic without new AI
  code beyond a priority-scoring tweak.
- **A Called Strike needs multiple valid targets in range to be worth
  the extra input** — against a single enemy it's strictly slower than
  a normal attack (why open a picker for one target), so it's a
  build-around-crowds/build-around-priority-targets tool, which gives
  it a clean niche instead of replacing the bump attack.
- **The free-aim, no-turn-cost picking phase needs a hard ceiling
  somewhere other than the pick cap**, or a patient player could scan
  an entire visible zone risk-free before ever committing. The
  existing `ThrowTargeting` range clamp already solves this — reuse it
  verbatim rather than inventing a new limit.
- **Cross-skill synergy for free**: Momentum stacking makes any
  "several attacks per turn" tool (multiple natural weapons after the
  FUN-P0 two-swing pass, an extra-arm mutation, a flurry skill) already
  a Momentum engine without extra work — the primitive rewards a build
  archetype (dual-wield, extra limbs) that doesn't currently have a
  mechanical throughline tying it together.

## 4. A note on where this could live thematically (optional, not required)

The pitch's own example is a **punch** — and the skill-family survey in
`Docs/Design/BUILD-EXPRESSIVITY.md` §0 notes the game has no dedicated
unarmed/brawling skill tree today (11 families, all weapon-class or
magic). Momentum plus a Called Strike is a strong seed for exactly that gap: an
unarmed tree's whole identity could be "you don't hit as hard per swing
as an axe, but every connected hit compounds, and you get to choose
who eats the compound," which gives unarmed a reason to exist
mechanically instead of being a worse weapon. Not required to ship the
mechanic — it works fine as a Cudgel or Short Blades active first —
but worth flagging as the natural eventual home.

## 5. Suggested build order (agent-pace)

1. **Momentum primitive alone** (B): a small `MomentumPart` (or an
   int property, mirrors the `StartingLoadout.KitAppliedProperty`
   pattern) incrementing/decaying on the existing
   `OnAttackerAfterAttack` hook (same hook `Axe_Cleave` already uses)
   plus a miss/CC listener. Ship with a flat damage-bonus modifier
   feeding into the existing damage pipeline. No targeting work at all
   — this alone is testable and useful on every weapon class today.
2. **The manual multi-pick input flow alone** (A), with a **flat pick
   cap and no momentum interaction** — a direct extension of
   `InputHandler.BeginThrowTargeting`'s free-cursor state machine
   (add a pick-list array and an add/remove/finalize step instead of
   resolving on the first confirm) plus `Axe_Whirlwind`'s
   collect-then-loop-`PerformSingleAttack` pattern for resolution.
   Low-risk: it reuses two already-reviewed subsystems and needs no
   new range/LOS/cursor-rendering code.
3. **Wire the pick-list resolution through the same Momentum counter
   from step 1**, and make the pick cap Momentum-scaled. By this point
   both halves are independently tested; the combination is "make sure
   the existing hook fires once per pick, in pick order," not new logic.
4. **AI-side counterplay** (target the highest-Momentum actor) as a
   small, separate follow-up once the player-facing half is confirmed
   fun.
