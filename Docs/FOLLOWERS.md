# Followers System

> **Single source of truth.** This file consolidates the multi-phase
> Followers feature development — plans, progress, cold-eye reviews,
> adversarial sweeps, audit findings, and Qud-parity tracking.
> Updated as each sub-milestone ships.
>
> Replaces the earlier per-phase docs (`FOLLOWERS-ROADMAP.md`,
> `FOLLOWERS-F2.md`, `FOLLOWERS-F3.md`). Git history preserves those.

---

## Table of contents

- [Status banner (cumulative)](#status-banner-cumulative)
- [Why we're building this](#why-were-building-this)
- [Phases overview](#phases-overview)
- [Working principles](#working-principles)
- [Qud-parity status (cumulative)](#qud-parity-status-cumulative)
- [Phase F.1 — Leader/Follower scaffolding ✅](#phase-f1--leaderfollower-scaffolding-)
- [Phase F.2 — Recruitment ✅](#phase-f2--recruitment-)
- [Phase F.3 — Slot system + faction-rep ✅](#phase-f3--slot-system--faction-rep-)
- [Phase F.4 — Cross-zone polish + mutual defense ⏳](#phase-f4--cross-zone-polish--mutual-defense-)
- [Phase F.5+ — Deferred polish ⏳](#phase-f5--deferred-polish-)
- [Cross-references](#cross-references)
- [Open design questions](#open-design-questions)

---

## Status banner (cumulative)

| Field | Value |
|---|---|
| **Current phase** | F.1 ✅ / F.2 ✅ / F.3 ✅ / F.4 not started |
| **Phases planned** | F.1 → F.4+ (5 phases total + F.5+ polish queue) |
| **Last updated** | 2026-05-11 |
| **Cumulative tests** | 171 (F.1: 61 + F.2: 55 + F.3: 55) |
| **Cumulative contracts pinned** | 136 (F.1: 13 + F.2: 55 + F.3: 68) |
| **Real bugs surfaced + fixed** | 2 (F.2.2 `Entity.RemoveEffect(Effect)` overload mismatch; F.3 `ApplyBonus` partial-apply atomicity) |
| **Audit passes run** | 2 (post-F.2.7 → 3 findings shipped; post-F.3 → 5 findings shipped) |
| **Reference codebase** | Qud (`/Users/steven/qud-decompiled-project/`) |

---

## Why we're building this

The game wants creatures that can follow the player (and each other)
as companions. The Qud-decompile (see `IDEAS.md` "Investigate Qud
Followers mechanic" chapter) shows a clean, layered architecture:

1. **Data substrate** — bidirectional leader/follower link on `Brain`.
2. **Allegiance reasons** — typed `IAllyReason` allowing multiple
   recruitment paths to coexist.
3. **Recruitment skills** — Proselytize / Beguile / Rebuke each
   produce a same-shaped Effect that sets the link.
4. **Companion-limit (slot) system** — query event lets any item /
   skill / mutation contribute slots.
5. **Faction integration** — `GrantsRepAsFollower` Part on creatures
   so recruitment moves player rep.
6. **AI behavior** — the leader/follower link feeds existing AI
   goals (Wander/Move) to keep followers near their leader.

CoO mirrors this layering. One layer per phase, smallest blast
radius first (CLAUDE.md §1.4), so each phase is independently
shippable + testable.

---

## Phases overview

| Phase | What ships | Status | Tests | Branch |
|---|---|---|---|---|
| **F.1** | Leader/follower data substrate + AI follow goal + hostility guard | ✅ Shipped | 61 | `feat/followers-f1-leader-scaffold` (merged `b77d223`) |
| **F.2** | Persuasion_Recruit + RecruitedEffect + Persuasion_Dismiss | ✅ Shipped | 55 | `feat/followers-f2-recruitment` (merged `3aeab76`) |
| **F.2.6** follow-up | Showcase scenario + persistent FollowLeaderGoal fix | ✅ Shipped | +6 regression | `feat/recruit-showcase` (merged `3eb9b1f`), `fix/follow-leader-goal-persistent` (merged `93fa0c9`) |
| **F.2.7** follow-up | Cross-zone follower transit | ✅ Shipped | +3 | `feat/followers-f2.7-cross-zone-transit` (merged `1e6d31d`) |
| **F.2 audit** | 3 findings (1 🔴 latent + 2 🟡 correctness) | ✅ Shipped | +3 | `fix/follower-system-audit-pass` (merged `1b1cb8b`) |
| **F.3** | Slot system + GrantsRepAsFollower + full Qud comma-delim syntax | ✅ Shipped | 55 | `feat/followers-f3-slot-system` (merged `5849c8d`) |
| **F.3 audit** | 5 findings (1 🟡 atomicity + 1 🟡 Qud-parity restore + 3 🟡 defense-in-depth) | ✅ Shipped | +3 | `fix/follower-f3-audit-pass` (merged `2dec554`) |
| **F.4** | Cross-zone pursuit + mutual defense + LeftBehindByPlayer | ⏳ Not started | — | TBD |
| **F.5+** | Polish queue (Beguile/Rebuke paths, auto-dismiss-oldest, clones, Sifrah, etc.) | ⏳ Deferred | — | TBD |

---

## Working principles

Mirrored from CLAUDE.md. Constraints every phase follows:

1. **TDD per §2.1** — write the failing test first, confirm RED,
   implement minimum, confirm GREEN.
2. **Smallest blast radius first** (§1.4) — each sub-milestone ships
   as one reviewable, independently revertable change.
3. **Verification sweep before code** (§1.2) — every phase opens with
   a corrections table reading every reference the plan cites.
4. **Cold-eye review after multi-commit features** (§Q1-Q4) —
   symmetry, cross-feature consistency, counter-check completeness,
   doc-vs-impl drift.
5. **Adversarial sweep when 2+ taxonomy surfaces hit** — bug-class
   probes covering scale, boundary, state atomicity, cross-actor,
   save/load reach, etc.
6. **Qud parity over CoO-originals** — when Qud has a pattern that
   addresses the problem, use it. CoO-originals get a 🟡 or ⚪
   marker explaining the divergence.
7. **Post-feature audit gate** — after a multi-commit phase merges,
   delegate a fresh Explore-agent audit, triage findings, ship fixes
   on a `fix/<phase>-audit-pass` branch. Both F.2 and F.3 ran this gate
   and surfaced real latent bugs.

---

## Qud-parity status (cumulative)

Aggregated across all shipped phases. Ported = full parity. Defer =
documented divergence with rationale.

| Qud feature | CoO status |
|---|---|
| `BrainPart.LeaderReference` / `PartyLeader` | ✅ ported (F.1.2 as `Entity PartyLeader` field) |
| `BrainPart.PartyMembers` | ✅ ported (F.1.2 as `HashSet<Entity>`; per-member flags deferred) |
| `SetPartyLeader` bidirectional mirror + cycle check + Forgive | ✅ ported (F.1.2) |
| `IsLedBy` / `GetFinalLeader` chain-walkers | ✅ ported (F.1.2, 64-depth safety cap) |
| Hostility guard (party members non-hostile to each other) | ✅ ported (F.1.4 via `FactionManager.GetFeeling`) |
| `Proselytized` effect shape (Apply / Remove / Dismiss) | ✅ ported (F.2.2 as `RecruitedEffect`) |
| `Persuasion_Proselytize` skill (Ego vs Level roll + vetos) | ✅ ported (F.2.3 as `Persuasion_Recruit`, d20+mod vs DC) |
| Dismiss as effect-as-dispatcher | ✅ ported (F.2.4 — symmetric skill `Persuasion_Dismiss` + `RecruitedEffect.Dismiss`) |
| `GetCompanionLimitEvent.GetFor` | ✅ ported (F.3.2 using CoO `GameEvent` pool) |
| Per-means slot bump (skill grants +1 "Recruit") | ✅ ported (F.3.2) |
| `GrantsRepAsFollower` comma-delim factions + `:N` per-entry override | ✅ ported (F.3.4 — Qud-parity bonus beyond plan) |
| `*allvisiblefactions:N` wildcard | ✅ ported (F.3 audit fix — Qud-parity bonus) |
| EndTurn hook for rep apply/unapply | ✅ ported (F.3.4) |
| Negative deltas honored | ✅ ported |
| `LeftBehindByPlayer` flag | ⚪ deferred F.4 |
| Cross-zone pursuit | partial: F.2.7 ports same-zone-with-leader transit, F.4 will add stay-behind opt-out |
| Mutual defense (follower attacks leader's attackers) | ⚪ deferred F.4 |
| Typed `IAllyReason` (Beguile/Rebuke/Proselytize coexistence) | ⚪ deferred F.5+ |
| `SyncTarget` auto-dismiss-oldest (Qud's slot-overflow path) | ⚪ deferred F.5+ (CoO uses veto — HashSet enumeration isn't deterministic for "oldest") |
| `OnDestroyObjectEvent`/`SuspendingEvent` unapply hooks | ⚪ deferred F.5+ (CoO lacks these event surfaces) |
| `DeepCopy` reset of `AppliedBonus` | ⚪ deferred F.5+ (no CoO cloning) |
| `Persuasion_Proselytize` Sifrah minigame option | ⚪ deferred F.5+ (entire subsystem missing in CoO) |
| Pack-spawner Part (`Followers.cs`) | ⚪ deferred F.5+ |
| Per-member flags (independent ally) | ⚪ deferred F.5+ |

**Cumulative: 14 Qud features ported in full. 9 deferred with
documented gameplay-impact rationale.** Plus 2 Qud-parity bonuses
that exceeded the original plan scope thanks to user emphasis (full
comma-delim + `*allvisiblefactions` wildcard).

---

# Phase F.1 — Leader/Follower scaffolding ✅

## Goal

Pin the **bidirectional leader/follower link** on `BrainPart` so a
future recruitment skill (F.2) can populate it without touching the
data substrate. Mirror **Qud's `Brain.SetPartyLeader` semantics**:

- Reference identity preserved on save/load (SL.8 contract)
- Cycle detection on every set
- `Forgive` on leadership change (clear leader from follower's `PersonalEnemies`)
- Hostility guard: party members don't attack each other
- Chain-walking helpers (`IsLedBy`, `GetFinalLeader`)

## Qud reference

`/Users/steven/qud-decompiled-project/XRL.World.Parts/Brain.cs`:

| Symbol | Line | What it does |
|---|---|---|
| `LeaderReference` | 217 | `GameObjectReference` — upward pointer |
| `PartyMembers` | 229 | `PartyCollection` (Dict\<BaseID, flags\>) — downward roster |
| `PartyLeader` property | 450 | Thin wrapper around `LeaderReference.Object` |
| `SetPartyLeader` | 895 | Bidirectional mirror, cycle check, Forgive, sound, event fire |
| `IsLedBy(Object)` | 1438 | Walks chain looking for Object |
| `GetFinalLeader` | 1623 | Top of chain |

## Sub-milestones

| # | Status | Tests | Commit | What |
|---|---|---|---|---|
| F.1.1 Plan + verification sweep | ✅ | — | `4c7fa67` | Plan doc + corrections table |
| F.1.2 Data substrate | ✅ | 20 | `acccdaf` | `PartyLeader`, `PartyMembers`, `SetPartyLeader`, `IsLedBy`, `GetFinalLeader` |
| F.1.3 Save/load | ✅ | 6 | `afc7cb3` | Round-trip via SL.8 identity contract |
| F.1.4 Hostility guard | ✅ | 12 | `db2168f` | `ArePartyAligned` + `FactionManager.GetFeeling` exception + `ALLIED_FEELING` constant |
| F.1.5 FollowLeaderGoal | ✅ | 13 | `db60f05` | AI goal: greedy step-toward leader, 6 termination cases |
| F.1.6 Adversarial + cold-eye + merge | ✅ | 10 | `1e6552a` → `b77d223` | 4+ taxonomy surface sweep, Q1-Q4 review |

**Total: 61 tests, 13 contracts, 0 bugs found in F.1.**

## Cold-eye review (Q1-Q4)

- **Q1 Symmetry:** ✅ `SetPartyLeader` maintains both sides of the link; save format mirrors load format; hostility guard placed after PersonalEnemies override (vendetta beats loyalty).
- **Q2 Consistency:** ✅ Qud-style verb-naming; new `FollowLeaderGoal` matches `MoveToGoal` shape; `ALLIED_FEELING` constant alongside `HOSTILE_THRESHOLD`.
- **Q3 Counter-checks:** ✅ Every positive contract paired with counter-form (leader-rejected, cycle blocked, etc.).
- **Q4 Drift:** ✅ Minor (renamed `IsPartyAlignedWith` → static `ArePartyAligned` for null-safety) — backfilled into docstring.

## Adversarial sweep (F.1.6)

`FollowerSystemAdversarialTests.cs` — 10 tests across 6 surfaces (scale, deep chain, boundary, rapid mutation, mid-state save, stale refs). **0 bugs found.**

## Pre-flagged self-review findings

- 🟡 PartyMembers as `HashSet<Entity>` (Qud uses flags-map) — flag-less is fine for F.1 substrate, upgrade if F.2+ needs flags.
- 🟡 Cross-zone follower behavior — give-up if zones mismatch (F.4+ to revisit).
- 🔵 Hostility guard one-way Forgive, two-way query — confirmed matches Qud.

---

# Phase F.2 — Recruitment ✅

## Goal

Ship the **recruitment verb**: player-owned activated ability that
mental-attacks an adjacent NPC; on success, installs `RecruitedEffect`
which (via F.1.2's `SetPartyLeader`) makes the target follow the
player. Plus symmetric **dismiss** verb.

## Qud reference

| File | Symbol | What it does |
|---|---|---|
| `XRL.World.Effects/Proselytized.cs:86-112` | `Apply(GameObject)` | Validates, fires `ApplyProselytize`, plays sound, adds opinion, **`SetAlliedLeader<AllyProselytize>(Proselytizer)`** (the link itself), calls `SyncTarget` |
| `XRL.World.Effects/Proselytized.cs:114-124` | `Remove(GameObject)` | If still under proselytizer's leadership: clears `PartyLeader`, clears goal stack, removes `AllyProselytize` allegiance |
| `XRL.World.Effects/Proselytized.cs:63-84` | `HandleEvent(GetInventoryActionsEvent)` + `HandleEvent(InventoryActionEvent)` | Surfaces "Dismiss" action when proselytizer looks at proselyte |
| `XRL.World.Parts.Skill/Persuasion_Proselytize.cs:142-226` | `AttemptProselytization()` | Veto chain → pick target → roll mental attack → apply `Proselytized` → consume 1000 energy + 25-turn cooldown |

## Design lockdowns (pinned in F.2.1 before code)

### 1. Roll formula

```
roll = d20 + StatUtils.GetModifier(attacker, "Ego")
DC   = RECRUIT_BASE_DC + max(defender.Level - attacker.Level, 0)
hit  = roll ≥ DC
```

Constants: `RECRUIT_BASE_DC = 10`, `RECRUIT_COOLDOWN_TURNS = 25`. 🟡 Divergent from Qud's MentalAttack penetration (no MA stat in CoO).

### 2. Veto chain (8 → 7 after audit removed dead #8 → 8 again after F.3.3 added at_companion_limit)

In evaluation order:

| # | Veto | reason= |
|---|---|---|
| 1 | null context | `null_context` |
| 2 | no adjacent target | `no_target` |
| 3 | self target (defense-in-depth) | `self_target` |
| 4 | target has no Brain | `target_no_brain` |
| 5 | already recruited | `already_recruited` |
| 6 | follows someone else | `follows_another` |
| 7 | target hostile (faction or grudge — bidirectional via `GetFeeling`) | `target_hostile` |
| 8 | at companion limit (F.3.3 addition) | `at_companion_limit` |
| — | roll failed | `roll_failed` with `d20`/`egoMod`/`roll`/`dc` payload |

### 3. Forgive contract

Reuse F.1.2's `SetPartyLeader` Forgive step. Don't duplicate. Veto #7 fires BEFORE recruit so an actively-grudging target can't be coerce-converted by a single skill cast.

### 4. Dismiss surface

Effect-as-dispatcher. `RecruitedEffect.Dismiss(dismisser)` is the public API; F.2.4 ships `Persuasion_Dismiss` as the v1 trigger surface (activated ability, adjacent-cell). Future UI surfaces (right-click, conversation) call the same `Dismiss()`.

🟡 Divergent from Qud (Qud uses inventory-actions surface; CoO lacks that for NPCs).

### 5. Diag records

| Category | Kind | When | Payload |
|---|---|---|---|
| `skill` | `CommandRouted` | OnCommand invoked | framework auto-emits |
| `skill` | `SkillRejected` | any veto fires | `skillClass, displayName, reason` (+ `d20/egoMod/roll/dc` for `roll_failed`) |
| `skill` | `Recruited` | roll succeeded + effect applied | `actor, target, roll, dc` |
| `skill` | `Dismissed` | RecruitedEffect.Dismiss completes | `actor, target` |

## Sub-milestones

| # | Status | Tests | Commit | What |
|---|---|---|---|---|
| F.2.1 Plan + lockdowns | ✅ | — | `dc34e09` | 5 design lockdowns pinned before code |
| F.2.2 RecruitedEffect | ✅ | 12 | `6dbb6c7` | Apply/Remove/Dismiss + round-trip with SL.8 identity |
| F.2.3 Persuasion_Recruit | ✅ | 15 | `245eec3` | 8-veto chain + d20 roll + diag emission |
| F.2.4 Persuasion_Dismiss | ✅ | 9 | `08dc26b` | Symmetric release verb |
| F.2.5 Adversarial + cold-eye + merge | ✅ | 19 | `8474b5f` → `3aeab76` | 7+ taxonomy surfaces |

**Total F.2 ship: 55 tests, 0 bugs found in adversarial sweep.**

## F.2.6 / F.2.7 follow-ups

### Showcase scenario + persistent-follow fix

- **F.2.6 RecruitShowcase** (`ad153ae` → `3eb9b1f`) — manual playtest scenario (Caves Of Ooo → Scenarios → AI Behavior → Recruit Showcase) exercising the full F.1+F.2 chain end-to-end.
- **F.2.6 persistent-follow bug fix** (`304c84f` → `93fa0c9`) — user playtest reported "Scribe didn't follow after recruit." Probe: scribe's `PartyLeader=player` but no `FollowLeaderGoal` on goal stack. Root cause: F.1.5's `Finished()` returned `true` when within `CloseEnoughDistance`; at recruit time the recruiter is adjacent by definition, so the goal popped tick 1. Fix: persistent semantics — Finished() doesn't pop on close-enough; `TakeAction` idles and resets `Age` when close. +3 regression tests.

### Cross-zone follower transit (F.2.7)

`72b8290` → `1e6d31d`. User playtest: "follower no longer with me after I go to a different chunk." Cause: `ZoneTransitionSystem.TransitionPlayer` moved only the player; `InputHandler.HandleZoneTransition` removed all old-zone creatures from TurnManager → follower stranded.

Fix: `TransitPartyMembers` helper called from both horizontal + vertical transition paths. Moves each same-zone follower to a passable cell adjacent to the leader's arrival. 8-direction deterministic search + 3-ring spiral fallback. Followers in OTHER zones not teleport-yanked. +3 regression tests.

## F.2 post-audit pass — 3 findings shipped

Branch `fix/follower-system-audit-pass` (merged `1b1cb8b`).

| # | Severity | Finding | Fix |
|---|---|---|---|
| 1 | 🔴 | `RecruitedEffect.OnApply` ignored `SetPartyLeader` return → goal pushed even when leader-link wasn't installed (cycle/self-ref) | Gate `PushGoal` behind `if (!brain.SetPartyLeader(Recruiter)) return;` + regression test |
| 2 | 🟡 | `Persuasion_Recruit` Veto #8 (`personal_grudge`) unreachable dead code (FactionManager.GetFeeling already checks bidirectional PersonalEnemies) | Removed Veto #8; renamed test to pin consolidated behavior |
| 3 | 🟡 | `FollowLeaderGoal` popped on cross-zone, stranding followers when F.2.7 transit fails to place them | Persistent across cross-zone (mirrors F.2.6 close-enough fix); TakeAction idles + resets Age when cross-zone |

## Cold-eye review (F.2.5)

- **Q1 Symmetry:** ✅ Recruit/Dismiss `OnCommand` share skeleton; `RecruitedEffect.OnApply`/`OnRemove` inverses
- **Q2 Consistency:** ✅ Diag payload shapes consistent; existing `EmitSkillRejectedDiag` re-used
- **Q3 Counter-checks:** ✅ Every veto has positive + counter; roll math both extremes pinned
- **Q4 Drift:** 🟡 Lockdown #5 spec'd `RecruitRejected`/`DismissRejected` kinds; impl uses framework's `SkillRejected` umbrella with `reason` field. Behavioral parity preserved, naming differs.

## Adversarial sweep (F.2.5)

`RecruitedEffectAdversarialTests.cs` — 19 tests across 10 surfaces (state atomicity, cross-actor chains, save/load identity, double-apply/dismiss, anti-exploit, probability boundaries, diag dispatch, goal stack, null-safety, scale). **0 bugs found.**

---

# Phase F.3 — Slot system + faction-rep ✅

## Goal

Cap how many followers the player can have at once (slot system) and
make followers grant the player faction reputation while they're in
the player's party (faction-rep mechanic). Two orthogonal substrate
layers.

## Qud reference

| File | Symbol | What it does |
|---|---|---|
| `XRL.World/GetCompanionLimitEvent.cs:35-60` | `GetFor(Actor, Means, BaseLimit)` | Fires a pooled event; listeners do `E.Limit += N` to bump |
| `XRL.World.Parts.Skill/Persuasion_Proselytize.cs:47-54` | `HandleEvent(GetCompanionLimitEvent)` | If `E.Means == "Proselytize"`, bumps `E.Limit++` |
| `XRL.World.Parts/CompanionCapacity.cs:45-59` | `HandleEvent(GetCompanionLimitEvent)` | Item-wearer-side; separate `Proselytized`/`Beguiled` fields |
| `XRL.World.Parts.Skill/Persuasion_Proselytize.cs:93-123` | `SyncTarget` | Slot enforcement: trim oldest if over limit |
| `XRL.World.Parts/GrantsRepAsFollower.cs:128-141` | `CheckApplyBonus(who)` | Each turn: if conditions hold → ApplyBonus; if applied + conditions break → UnapplyBonus |
| `XRL.World.Parts/GrantsRepAsFollower.cs:62-92` | `ApplyBonus` | Parses comma-delim faction list (`"FactionA:5,FactionB"`); calls `PlayerReputation.Modify` for each |

## F.3.1 verification sweep — blockers cleared

Two 🔴 blockers identified in the initial plan, both resolved before F.3.2:

| Blocker | Resolution |
|---|---|
| PooledEvent equivalent in CoO | ✅ `GameEvent` uses static `Pool` stack with `Rent()`/`Release()` (GameEvent.cs:18, 40-60). Same intent as Qud's `PooledEvent<T>`, different shape (one class + string ID + dynamic param dicts). |
| How skills hook events | ✅ Every `Part` has virtual `HandleEvent(GameEvent e)` (Part.cs:49-52). `Entity.FireEvent` iterates ALL Parts. `SkillsPart.HandleEvent` (SkillsPart.cs:379-419) is the canonical consumer. |

## Sub-milestones

| # | Status | Tests | Commit | What |
|---|---|---|---|---|
| F.3.1 Plan + sweep | ✅ | — | `4c7fa67` initial → `b53c850` blockers cleared | Both 🔴 blockers resolved |
| F.3.2 GetCompanionLimitEvent + slot bump | ✅ | 8 | `ab7f39a` | Static `GetFor(actor, means, baseLimit)`, `Persuasion_Recruit` HandleEvent bumps "Recruit" by +1 |
| F.3.3 Slot enforcement | ✅ | 5 | `f81a8c1` | Veto #8 `at_companion_limit` + `CountRecruitedFollowers` filter (per-Recruiter) |
| F.3.4 GrantsRepAsFollowerPart | ✅ | 17 | `2d62559` | Apply/unapply + full Qud comma-delim Faction syntax with `:N` per-entry override |
| F.3.5 Save/load round-trip | ✅ | 4 | `44ff7de` | Reflection-based `AppliedBonus` round-trip pinned |
| F.3.6 Adversarial + cold-eye + merge | ✅ | 14 | `3464221` → `5849c8d` | 7+ taxonomy surfaces, 0 bugs found |

**Total F.3 ship: 55 tests, 68 contracts, 0 bugs found in adversarial sweep.**

## F.3 post-audit pass — 5 findings shipped

Branch `fix/follower-f3-audit-pass` (merged `2dec554`). 19 audit findings triaged to 5 actionable fixes:

| # | Severity | Finding | Fix |
|---|---|---|---|
| 1 | 🟡 | `GetCompanionLimitEvent.GetFor` leaked event on exception | try-finally around FireEvent + Release |
| 2 | 🟡 | Qud-parity restore: `*allvisiblefactions:N` wildcard | Ported (3 regression tests) — applies to every faction in `PlayerReputation.GetAll().Keys` |
| 6 | 🟡 | `CountRecruitedFollowers` foreach without snapshot | Snapshot pattern (defense-in-depth, matches F.2.7's TransitPartyMembers) |
| 7 | 🟡 | Veto #8 didn't clamp negative limit values | `Math.Max(0, ...)` clamp |
| 8 | 🟡 | `ApplyBonus` partial-apply atomicity (real correctness gap) | Eager AppliedBonus flag + `HasAnyApplicableEntry` pre-flight — prevents re-entry double-apply on exception paths |

10 findings deferred with documented rationale (Qud's `SyncTarget`/`DeepCopy`/`SuspendingEvent`/Sifrah subsystems not in CoO yet). 4 findings confirmed not-bugs or false alarms.

## Cold-eye review (F.3.6)

- **Q1 Symmetry:** ✅ `GetCompanionLimitEvent.GetFor` mirrors Qud's; `Persuasion_Recruit.HandleEvent` mirrors `Persuasion_Proselytize`'s; `ApplyBonus`/`UnapplyBonus` are inverses; veto numbering sequential.
- **Q2 Consistency:** ✅ Same idiom as `TurnManager.EndTurn` for event dispatch; `HandleEvent` shape matches `SkillsPart.HandleEvent`; `EmitSkillRejectedDiag` consistent.
- **Q3 Counter-checks:** ✅ Every veto + every parser path has positive + counter pair.
- **Q4 Drift:** 🟡 POSITIVE — plan deferred comma-delim Faction syntax to F.5+; user emphasized Qud parity → shipped full Qud-parity syntax. CoO is CLOSER to Qud than plan promised. Post-audit pass added `*allvisiblefactions:N` for same reason.

## Adversarial sweep (F.3.6)

`F3SlotSystemAdversarialTests.cs` — 14 tests across 8 surfaces:

| Surface | Probe |
|---|---|
| State atomicity | Apply→unapply→apply correct final state; zone-transit oscillation × 5 → no drift |
| Cross-actor | Slot limits per-actor (Alice ≠ Bob count); NPC leader → no player-rep flow |
| Anti-exploit | 100 apply/unapply cycles → net zero (no rep-pump); empty PartyMembers handled cleanly |
| Cross-system aggregation | Two followers same faction → linear stack; different factions → independent |
| Diag dispatch | At-limit veto emits exactly 1 SkillRejected record |
| Parser malformed | `:` (no value) / `:abc` (non-numeric) / `::` (colon-only) / `:-N` (negative) — all handled |
| Scale | 10 over-limit pre-existing recruits → veto fires |

**0 bugs found in adversarial sweep.**

---

# Phase F.4 — Cross-zone polish + mutual defense ⏳

**Not started.** F.2.7 already ships the default-follow path (followers come along through zone transitions). F.4 covers the OPT-OUT case + combat-rules surface.

## Planned scope

| Piece | Qud reference | CoO mapping |
|---|---|---|
| `LeftBehindByPlayer` flag | Brain.cs | Boolean field on BrainPart; explicit "stay here" command sets it; FollowLeaderGoal honors |
| Cross-zone graceful stay | Zone transition flow | F.2.7 already covers default-follow; F.4 adds explicit opt-out |
| Mutual defense | Brain.cs `OnDamaged` event handlers | When a party member is damaged, OTHER party members in zone push a KillGoal targeting the attacker |
| Leader-attacks-follower handling | Qud's `Forgive` clears damage tracking | F.1's Forgive covers recruit-time clearing; F.4 confirms ongoing case (leader attacks follower → follower can break free via PersonalEnemies path) |

## Sub-milestone outline (TBD)

3-4 sub-milestones likely:
- F.4.1 Plan + sweep
- F.4.2 `LeftBehindByPlayer` flag + opt-out command
- F.4.3 Mutual defense (party-member-attacked → other party-members push KillGoal)
- F.4.4 Adversarial + cold-eye + merge

Estimated 4-6 commits, ~25-40 tests.

---

# Phase F.5+ — Deferred polish ⏳

These are nice-to-have features without immediate gameplay need.
Each can land as a single-commit addition when its content shows up.

| Piece | Notes |
|---|---|
| Per-member flags map | Qud uses `Dictionary<BaseID, flags>` for "is independent ally?" etc. CoO's HashSet upgrades to `Dictionary<Entity, int>` (purely additive — saves get `flag=0` default). |
| OpinionMap | Per-target sentiment, separate from allegiance. "This villager remembers you saved them" without making them a literal companion. |
| Pack-spawner Part | `Followers.cs` in Qud — entity arrives with auto-spawned allies. Useful for boss encounters. |
| Multiple recruitment paths | Beguile (mutation), Rebuke (robot), Cooking-charm. Each parallel skill + Effect copying `Persuasion_Recruit` shape. Demonstrates value of typed `IAllyReason`. |
| `SyncTarget` auto-dismiss-oldest | Qud's slot-overflow path. CoO ships veto-mode in F.3; auto-dismiss needs recruit-order tracking (HashSet enumeration isn't deterministic). |
| `OnDestroyObjectEvent` / `SuspendingEvent` unapply | Qud's explicit cleanup hooks for `GrantsRepAsFollower`. CoO lacks these event surfaces; leader-null branch covers most cases. |
| `DeepCopy` reset of `AppliedBonus` | For when CoO adds in-game cloning. |
| Sifrah minigame for recruitment | Qud's `ProselytizationSifrah`. Entire minigame subsystem missing in CoO. |

---

## Cross-references

### Code (current state)

| File | Phase | Purpose |
|---|---|---|
| `Assets/Scripts/Gameplay/AI/BrainPart.cs` | F.1.2, F.1.4 | `PartyLeader`, `PartyMembers`, `SetPartyLeader`, `IsLedBy`, `GetFinalLeader`, `ArePartyAligned` |
| `Assets/Scripts/Gameplay/AI/FactionManager.cs` | F.1.4 | `ArePartyAligned`-driven hostility exception, `ALLIED_FEELING = 100` |
| `Assets/Scripts/Gameplay/Save/SaveSystem.cs` | F.1.3 | `SaveBrainPart`/`LoadBrainPart` with PartyLeader + PartyMembers |
| `Assets/Scripts/Gameplay/AI/Goals/FollowLeaderGoal.cs` | F.1.5, F.2.6, F.2 audit | Persistent across close-enough AND cross-zone; idle + Age reset |
| `Assets/Scripts/Gameplay/Effects/Concrete/RecruitedEffect.cs` | F.2.2, F.2 audit | OnApply/OnRemove/Dismiss + SetPartyLeader return check |
| `Assets/Scripts/Gameplay/Skills/Persuasion_Recruit.cs` | F.2.3, F.3.2, F.3.3, F.3 audit | 8-veto chain + d20 roll + HandleEvent slot bump + at_companion_limit veto |
| `Assets/Scripts/Gameplay/Skills/Persuasion_Dismiss.cs` | F.2.4 | Symmetric release verb |
| `Assets/Scripts/Gameplay/World/ZoneTransitionSystem.cs` | F.2.7 | `TransitPartyMembers` + `FindAdjacentPassableCell` |
| `Assets/Scripts/Gameplay/AI/GetCompanionLimitEvent.cs` | F.3.2, F.3 audit | Static `GetFor(actor, means, baseLimit)` query with try-finally |
| `Assets/Scripts/Gameplay/AI/GrantsRepAsFollowerPart.cs` | F.3.4, F.3 audit | Full Qud-parity Faction syntax + `*allvisiblefactions` wildcard |
| `Assets/Scripts/Scenarios/Custom/RecruitShowcase.cs` | F.2.6 | Manual playtest scenario |

### Tests (current state)

| File | Tests | Phase |
|---|---|---|
| `Assets/Tests/EditMode/Gameplay/AI/FollowerSystemTests.cs` | 32 | F.1.2 + F.1.4 |
| `Assets/Tests/EditMode/Gameplay/AI/FollowLeaderGoalTests.cs` | 15+ | F.1.5 + F.2.6 + F.2/F.3 audit |
| `Assets/Tests/EditMode/Gameplay/AI/FollowerSystemAdversarialTests.cs` | 10 | F.1.6 |
| `Assets/Tests/EditMode/Gameplay/Save/Tier1BrainPartTests.cs` | 21 | F.1.3 |
| `Assets/Tests/EditMode/Gameplay/Effects/RecruitedEffectTests.cs` | 13 | F.2.2 + audit |
| `Assets/Tests/EditMode/Gameplay/Effects/RecruitedEffectAdversarialTests.cs` | 21 | F.2.5 |
| `Assets/Tests/EditMode/Gameplay/Skills/PersuasionRecruitTests.cs` | 20 | F.2.3 + F.3.3 |
| `Assets/Tests/EditMode/Gameplay/Skills/PersuasionDismissTests.cs` | 9 | F.2.4 |
| `Assets/Tests/EditMode/Gameplay/World/Map/WorldMapTests.cs` (+3) | 65 (+3 F.2.7) | F.2.7 cross-zone transit |
| `Assets/Tests/EditMode/Gameplay/Scenarios/ScenarioCustomSmokeTests.cs` (+1) | 44 (+1 F.2.6) | F.2.6 showcase |
| `Assets/Tests/EditMode/Gameplay/AI/GetCompanionLimitEventTests.cs` | 8 | F.3.2 |
| `Assets/Tests/EditMode/Gameplay/AI/GrantsRepAsFollowerPartTests.cs` | 24 | F.3.4 + F.3.5 + F.3 audit |
| `Assets/Tests/EditMode/Gameplay/AI/F3SlotSystemAdversarialTests.cs` | 14 | F.3.6 |

### Qud reference files

| File | Symbols |
|---|---|
| `/Users/steven/qud-decompiled-project/XRL.World.Parts/Brain.cs` | `SetPartyLeader`, `IsLedBy`, `GetFinalLeader`, `Forgive` |
| `XRL.World.Effects/Proselytized.cs` | Apply/Remove/Dismiss shape (F.2 mirror) |
| `XRL.World.Parts.Skill/Persuasion_Proselytize.cs` | Recruit skill shape (F.2.3) |
| `XRL.World/GetCompanionLimitEvent.cs` | Slot-query event shape (F.3.2) |
| `XRL.World.Parts/GrantsRepAsFollower.cs` | Faction-rep mechanic (F.3.4) |
| `XRL.World.Parts/CompanionCapacity.cs` | Slot-bump Part (F.5+) |
| `XRL.World.Effects/Beguiled.cs`, `Rebuked.cs` | Alternative recruitment paths (F.5+) |
| `XRL.World.Parts/Followers.cs` | Pack-spawner (F.5+) |

---

## Open design questions

Carry forward to F.4+:

- **Cross-zone behavior for explicit opt-out** — F.4's `LeftBehindByPlayer` flag UX: player-issued command, conversation action, or context menu?
- **Mutual defense scope** — auto-attack all hostiles to leader, OR only attack the exact attacker? Qud does the latter.
- **Companion limit default value** — should there be a base limit > 0 (always 1 follower available) or strict opt-in via skill?
- **`SyncTarget` revisit** — recruit-order tracking field needed for auto-dismiss-oldest. Worth the cost if playtest shows veto-mode UX is frustrating.
- **Cross-zone rep flow** — F.3.4 says "same zone only" matches Qud. Confirm via playtest that the rep-toggle on zone transit is observable + not confusing.

---

*This file is updated as each sub-milestone ships. Cumulative status
+ phase post-mortems + cross-refs all live here.*
