# Momentum combat — chained multi-target strikes with rolling damage

> **Status: BRAINSTORM / DESIGN PROPOSAL.** Nothing here is implemented.
> User pitch (2026-09-14): a punch that hits one guy, then hits the next
> guy harder because of carried momentum — with the explicit note that
> "target more than one cell for one hit" doesn't have to be tied to
> "momentum," they're separable ideas.

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

### A. The targeting primitive — "a swing that can reach more than one cell"

Distinct from both existing multi-target skills:
- **Not Whirlwind** (omnidirectional, unordered, every adjacent target
  hit once, no scaling between them).
- **Not Cleave** (chance-gated, one bonus target, fixed half-damage,
  no sequence).

New shape: **a short ordered line.** Reuses `Cudgel_ChargingStrike`'s
already-shipped line-walk (`ctx.DirectionX/DirectionY`,
`AbilityTargetingMode.DirectionLine`) but instead of moving the actor
to the first creature and stopping, the actor **stays put and strikes
every creature found along up to N cells in the chosen direction**,
in order — Whirlwind's "collect targets, then loop
`PerformSingleAttack`" applied to a line instead of an 8-direction ring.

This targeting primitive, alone, with **no damage scaling at all**, is
already a complete, shippable skill (call it **Flowing Strike**): hit
everyone standing in a row. That's the pitch's "target more than one
cell for one hit," fully decoupled from momentum, exactly as the user
suggested it could be.

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

Combine A and B and you get exactly what was pitched: **Flowing Strike
reads and writes the same Momentum counter as every other attack.**
Hitting the first guy in the line increments Momentum before the second
step resolves, so the second guy in the *same swing* is hit at a higher
damage multiplier than the first — carried speed, mechanically real.

The elegant part: because Momentum is a persistent counter, not a
per-skill local variable, **"hit two guys in one swing" and "hit one
guy last turn, then another guy this turn" are the same currency.**
A player doesn't need Flowing Strike to build or spend Momentum; they
need it to spend several stacks in a single action instead of trickling
them out one swing at a time. That is the cleanest version of the
user's own decoupling: one shared resource, two different ways to
interact with it (accumulate steadily across turns, or burn a big chain
of stacks at once against a row of enemies).

Worked example (5 stacks max, +8%/stack, base damage 10):
| Step | Momentum before | Damage this hit |
|---|---|---|
| Bump attack, turn 1 | 0 | 10 (+0%) |
| Bump attack, turn 2 | 1 | 10.8 (+8%) |
| Flowing Strike, target 1 | 2 | 11.6 (+16%) |
| Flowing Strike, target 2 (same swing) | 3 | 12.4 (+24%) |
| Flowing Strike, target 3 (same swing) | 4 | 13.2 (+32%) |

A pure single-target build racks up the same +8%-per-landed-hit climb
over several turns; Flowing Strike just lets a player cash in a whole
staircase of it against a lined-up group in one action, which is the
"reward for setting up the shot" the pitch is describing.

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
- **Flowing Strike needs living targets in the line to be worth using**
  — against a single enemy it's strictly worse than a normal attack
  (why walk a line-trace for one hit), so it's a build-around-crowds
  tool, which gives it a clean niche instead of replacing the bump
  attack.
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
magic). Momentum plus Flowing Strike is a strong seed for exactly that
gap: an unarmed tree's whole identity could be "you don't hit as hard
per swing as an axe, but every connected hit compounds," which gives
unarmed a reason to exist mechanically instead of being a worse weapon.
Not required to ship the mechanic — it works fine as a Cudgel or
Short Blades active first — but worth flagging as the natural eventual
home.

## 5. Suggested build order (agent-pace)

1. **Momentum primitive alone** (B): a small `MomentumPart` (or an
   int property, mirrors the `StartingLoadout.KitAppliedProperty`
   pattern) incrementing/decaying on the existing
   `OnAttackerAfterAttack` hook (same hook `Axe_Cleave` already uses)
   plus a miss/CC listener. Ship with a flat damage-bonus modifier
   feeding into the existing damage pipeline. No targeting work at all
   — this alone is testable and useful on every weapon class today.
2. **Flowing Strike alone** (A), with **no** momentum interaction
   (flat damage per target) — a straight recombination of
   `Cudgel_ChargingStrike`'s line-walk and `Axe_Whirlwind`'s
   collect-and-loop, so it's low-risk and reuses two already-reviewed
   patterns.
3. **Wire Flowing Strike's per-step hits through the same Momentum
   counter from step 1.** By this point both halves are independently
   tested; the combination is "make sure the existing hook fires N
   times in the existing order," not new logic.
4. **AI-side counterplay** (target the highest-Momentum actor) as a
   small, separate follow-up once the player-facing half is confirmed
   fun.
