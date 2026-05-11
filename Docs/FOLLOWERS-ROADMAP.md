# Followers System — Roadmap & Progress

> Top-level reference doc for the multi-phase Followers feature.
> Tracks what's shipped, what's planned, and where each piece lives.
> Phase-specific plans live in their own docs (linked below); this file
> is the entry point.

---

## Status banner

| Field | Value |
|---|---|
| **Current phase** | F.1 ✅ / F.2 ✅ / F.3 ✅ / F.4 not started |
| **Phases planned** | F.1 → F.4+ (5 phases total, plus optional follow-ups) |
| **Last updated** | 2026-05-11 |
| **Cumulative tests** | 171 (F.1: 61 + F.2: 55 + F.3: 55) |
| **Cumulative contracts pinned** | 136 (F.1: 13 + F.2: 55 + F.3: 68) |
| **Real bugs found** | 2 (F.2.2 `Entity.RemoveEffect(Effect)` overload mismatch; F.3 `ApplyBonus` partial-apply atomicity) |
| **Audit passes run** | 2 (post-F.2.7 cold-eye → 3 findings shipped; post-F.3 cold-eye → 5 findings shipped) |
| **Reference codebase** | Qud (`/Users/steven/qud-decompiled-project/`) |

---

## Why we're building this

The game wants creatures that can follow the player (and each other)
as companions. The Qud-decompile (see [IDEAS.md "Investigate Qud
Followers mechanic" chapter](IDEAS.md)) shows a clean, layered
architecture for this:

1. **Data substrate** — bidirectional leader/follower link on `Brain`.
2. **Allegiance reasons** — typed `IAllyReason` allowing multiple
   recruitment paths to coexist.
3. **Recruitment skills** — Proselytize / Beguile / Rebuke each
   produce a same-shaped Effect that sets the link.
4. **Companion-limit (slot) system** — query event lets any item /
   skill / mutation contribute slots.
5. **Faction integration** — `GrantsRepAsFollower` Part on creatures
   so recruitment moves player rep.
6. **AI behavior** — the leader/follower link feeds into existing AI
   goals (Wander/Move) to keep followers near their leader.

CoO mirrors this layering. We ship one layer per phase, smallest blast
radius first (per `CLAUDE.md` §1.4), so each phase is independently
shippable + testable.

---

## Phases overview

| Phase | What ships | Status | Plan doc |
|---|---|---|---|
| **F.1** | Leader/follower data substrate + AI follow goal + hostility guard | ✅ **Shipped** | [`FOLLOWERS.md`](FOLLOWERS.md) (F.1-specific) |
| **F.2** | Recruitment skill + Recruited effect + Dismiss skill | ✅ **Shipped** | [`FOLLOWERS-F2.md`](FOLLOWERS-F2.md) |
| **F.2.6** (follow-up) | Showcase scenario + persistent FollowLeaderGoal fix | ✅ **Shipped** | (commits `ad153ae`/`304c84f`) |
| **F.2.7** (follow-up) | Cross-zone follower transit | ✅ **Shipped** | (commit `72b8290`) |
| **F.2 audit** (3 findings) | 1 🔴 latent + 2 🟡 correctness | ✅ **Shipped** | (commit `b7c3605`) |
| **F.3** | Companion-slot system + `GrantsRepAsFollower` Part + faction content | ✅ **Shipped** | [`FOLLOWERS-F3.md`](FOLLOWERS-F3.md) |
| **F.3 audit** (5 findings) | 1 🟡 atomicity + 1 🟡 Qud-parity restore + 3 🟡 defense-in-depth | ✅ **Shipped** | (commit `21a61a3`) |
| **F.4** | Cross-zone pursuit + mutual defense + LeftBehindByPlayer | ⏳ Planned | TBD — `FOLLOWERS-F4.md` |
| **F.5+** | Per-member flags (independent ally, etc.), opinion map, pack-spawner Part | ⏳ Deferred | — |

---

## Phase F.1 — Leader/Follower Scaffolding ✅ COMPLETE

**Goal:** Pin the bidirectional leader/follower link on `BrainPart`
mirroring Qud's `Brain.SetPartyLeader` semantics. NO recruitment yet
— just the data layer + AI follow behavior + hostility guard.

**Shipped in branch `feat/followers-f1-leader-scaffold`**, merged to
`main` at `b77d223`. See [`FOLLOWERS.md`](FOLLOWERS.md) for the full
phase plan + corrections table + sub-milestone breakdown.

### What landed

| Sub-milestone | Commit | Tests | What |
|---|---|---|---|
| F.1.1 Plan + sweep | `4c7fa67` | — | Plan to disk, verification sweep (corrections table) |
| F.1.2 Data substrate | `acccdaf` | 20 | `PartyLeader`, `PartyMembers`, `SetPartyLeader`, `IsLedBy`, `GetFinalLeader`, cycle detection, Forgive |
| F.1.3 Save/load | `afc7cb3` | 6 | Round-trip with SL.8 identity contract |
| F.1.4 Hostility guard | `db2168f` | 12 | `ArePartyAligned` + `FactionManager.GetFeeling` exception + `ALLIED_FEELING` constant |
| F.1.5 FollowLeaderGoal | `db60f05` | 13 | AI goal: greedy step-toward leader, 6 termination cases |
| F.1.6 Adversarial + cold-eye + merge | `1e6552a` → `b77d223` | 10 | 4+ taxonomy surface sweep, Q1-Q4 review |

**Total: 61 tests, 13 contracts, 0 bugs.**

### Qud parity mapping (current state)

| Qud Brain.cs symbol | CoO equivalent | Notes |
|---|---|---|
| `LeaderReference` (line 217) | `BrainPart.PartyLeader` | Public `Entity` field, save-loaded via token system |
| `PartyMembers` (line 229) | `BrainPart.PartyMembers` | `HashSet<Entity>` — flags-per-member deferred to F.2 |
| `SetPartyLeader` (line 895-948) | `BrainPart.SetPartyLeader(Entity)` | Returns `bool` (false = rejected by self/cycle) |
| `IsLedBy` (line 1438) | `BrainPart.IsLedBy(Entity)` | 64-depth safety cap |
| `GetFinalLeader` (line 1623) | `BrainPart.GetFinalLeader()` | 64-depth safety cap |
| `Forgive` (Brain.cs:924-932) | `PersonalEnemies.Remove(leader)` in `SetPartyLeader` | Recruit doesn't re-aggro |
| Brain hostility via PartyMembers | `BrainPart.ArePartyAligned` + `FactionManager.GetFeeling` guard | PersonalEnemies vendetta still beats party tie |
| (no dedicated Goal) | `FollowLeaderGoal` | CoO-original; Qud composes from Wander/Move |
| Typed allegiance (`IAllyReason`) | **Deferred to F.2** | Will be needed when recruitment ships |

---

## Phase F.2 — Recruitment

**Goal:** A player skill that recruits an NPC, producing the same
shape as Qud's Proselytize → Proselytized flow.

**Why this fits cleanly on top of F.1:** The data substrate is in
place. F.2 just adds the verb that populates it.

### Planned scope

| Piece | Qud reference | CoO mapping |
|---|---|---|
| `IAllyReason` interface + `AllyDefault` impl | XRL.World.AI, `SetAlliedLeader<T>` | New file `AllyReasons.cs`. Initially: flat (one reason). Type-parameter slot reserved for F.5+ when a second recruitment path lands. |
| `RecruitedEffect : Effect` | `Proselytized` (Effects/Proselytized.cs) | Carries `Recruiter` entity ref. `Apply()` calls `target.GetPart<BrainPart>().SetPartyLeader(Recruiter)`. `Remove()` clears link if recruiter still leads. Auto-saves via SL.6-pinned Effect contract. |
| `Persuasion_Recruit` skill | `Persuasion_Proselytize` (Parts.Skill/) | Cooldown-gated activated ability. Picks target, validates conversation-possible, rolls mental attack with attacker's Ego vs. target's Level. On success, ApplyEffect(new RecruitedEffect). |
| Dismiss action | `GetInventoryActionsEvent` "Dismiss" | Inventory action on the follower (visible to the leader) that removes the RecruitedEffect. |
| Goal stack integration | — | On recruit, push `FollowLeaderGoal` onto the new follower's brain. On dismiss, pop it. |

### Sub-milestone outline (TBD; refine when starting F.2)

- F.2.1 Plan + verification sweep
- F.2.2 RecruitedEffect (round-trip, Apply, Remove)
- F.2.3 Persuasion_Recruit skill (cooldown ability, target picker)
- F.2.4 Mental-attack roll (or copy CoO's existing skill-roll pattern)
- F.2.5 Dismiss inventory action
- F.2.6 Goal-stack auto-wire (push FollowLeaderGoal on Apply)
- F.2.7 Adversarial sweep + cold-eye + merge

**Estimated:** ~50-80 tests, ~7-10 commits.

---

## Phase F.3 — Slot system + faction-rep content ✅ COMPLETE

**Goal:** Cap how many followers the player can have at once, and
make followers grant faction reputation while they're with the player.

**Shipped in branch `feat/followers-f3-slot-system`**, merged to
`main` at `5849c8d`. Post-audit fixes merged at `2dec554`. See
[`FOLLOWERS-F3.md`](FOLLOWERS-F3.md) for the full phase plan +
corrections table + sub-milestone breakdown + cold-eye review +
adversarial sweep + audit findings.

### What landed

| Sub-milestone | Commit | Tests | What |
|---|---|---|---|
| F.3.1 Plan + sweep | `b53c850` | — | Plan, 2 🔴 blockers resolved (GameEvent pool ≡ Qud PooledEvent; Part.HandleEvent + Entity.FireEvent is the dispatch surface) |
| F.3.2 GetCompanionLimitEvent | `ab7f39a` | 8 | Static `GetFor(actor, means, baseLimit)` query, `Persuasion_Recruit` HandleEvent override bumps "Recruit" by +1 |
| F.3.3 Slot enforcement | `f81a8c1` | 5 | Veto #8 `at_companion_limit` + `CountRecruitedFollowers` filter (per-Recruiter, per-RecruitedEffect) |
| F.3.4 GrantsRepAsFollowerPart | `2d62559` | 17 | Apply/unapply + Qud-parity comma-delimited Faction syntax with `:N` per-entry override |
| F.3.5 Save/load round-trip | `44ff7de` | 4 | Reflection-based `AppliedBonus` round-trip pinned |
| F.3.6 Adversarial + cold-eye + merge | `3464221` → `5849c8d` | 14 | 7+ taxonomy surfaces, 0 bugs found |
| Post-audit fix pass | `21a61a3` → `2dec554` | 3 | 5 findings shipped (1 latent atomicity bug + 1 Qud-parity `*allvisiblefactions` restore + 3 defense-in-depth) |

**Total: 55 tests, 68 contracts, 1 real bug surfaced + fixed.**

### Qud-parity bonus (beyond F.3.1 plan)

User emphasized Qud parity, so two features the plan deferred to F.5+
were shipped now:

- ✅ Comma-delimited `Faction` with `:N` per-entry override
- ✅ `*allvisiblefactions:N` wildcard syntax

### Qud parity mapping (current state)

| Qud feature | F.3 status |
|---|---|
| `GetCompanionLimitEvent.GetFor` | ✅ ported (CoO `GameEvent`-backed) |
| Per-means slot bump | ✅ ported |
| Comma-delimited `Faction` + `:N` per-entry override | ✅ ported (user-emphasis bonus) |
| `*allvisiblefactions:N` wildcard | ✅ ported (post-audit, user-emphasis bonus) |
| `SyncTarget` auto-dismiss-oldest | ⚪ deferred F.5+ (CoO uses veto — divergence documented) |
| `OnDestroyObjectEvent`/`SuspendingEvent` unapply | ⚪ deferred (CoO lacks event surfaces) |
| `DeepCopy` reset of `AppliedBonus` | ⚪ deferred (no CoO cloning) |
| `Persuasion_Proselytize` Sifrah minigame | ⚪ deferred (subsystem missing) |

---

## Phase F.3 (original planned scope — preserved for reference)

### Planned scope

| Piece | Qud reference | CoO mapping |
|---|---|---|
| `GetCompanionLimitEvent` | XRL.World/GetCompanionLimitEvent.cs | New event class. `static GetFor(Actor, Means, BaseLimit)` query. Any Part can listen and `E.Limit++`. |
| Slot-source: skill | `Persuasion_Proselytize` adds +1 | `Persuasion_Recruit` adds +1 to "Recruit" Means. |
| Slot-source: CompanionCapacity Part | `CompanionCapacity` (Parts/) | A wearable item or accessory that adds +N slots. |
| Slot enforcement | `SyncTarget` trims oldest if over | When `SetPartyLeader` succeeds AND total followers > limit, oldest follower is auto-dismissed. |
| `GrantsRepAsFollower` Part | Parts/GrantsRepAsFollower.cs | EndTurn-tick checks `parent.PartyLeader == player && same zone`; applies/unapplies `PlayerReputation.Modify(faction, +N)` via existing rep system. |
| Faction content | — | New content: which creatures grant rep with which factions. Pure JSON / blueprint additions. |

### Sub-milestone outline (TBD)

- F.3.1 Plan + sweep
- F.3.2 `GetCompanionLimitEvent` + `Persuasion_Recruit` slot bump
- F.3.3 Slot enforcement (auto-dismiss oldest)
- F.3.4 `GrantsRepAsFollower` Part + apply/unapply lifecycle
- F.3.5 Faction-rep round-trip tests (rep stays positive while follower is in zone)
- F.3.6 Adversarial + cold-eye + merge

**Estimated:** ~30-50 tests, ~5-7 commits.

---

## Phase F.4 — Cross-zone pursuit + mutual defense

**Goal:** Followers don't stay behind when the player travels; the
party defends each other in combat.

### Planned scope

| Piece | Qud reference | CoO mapping |
|---|---|---|
| Cross-zone follow | `LeftBehindByPlayer` flag + zone-transition hooks | `FollowLeaderGoal` currently fails on zone mismatch (F.1.5 deferred this). F.4 adds: detect leader-zone-change, queue a zone-transition step, despawn-and-respawn on arrival, OR teleport-with-leader. |
| Mutual defense | Brain.cs `OnDamaged` event handlers | When a party member is damaged, OTHER party members in the zone push a KillGoal targeting the attacker. |
| Leader-attacks-follower handling | `Forgive` clears damage tracking | Already handled in F.1's Forgive. F.4 confirms: if leader attacks follower mid-recruitment, follower can break free (existing PersonalEnemies path). |

### Sub-milestone outline (TBD)

Likely 3-4 sub-milestones; depends on how cross-zone is implemented
in the existing CoO zone system (need a fresh verification sweep when
starting F.4).

---

## Phase F.5+ — Polish (deferred)

These are nice-to-have features that don't have a clear immediate
gameplay need. Each can land as a single-commit addition when its
content shows up.

| Piece | Notes |
|---|---|
| Per-member flags map | Qud uses `Dictionary<BaseID, flags>` for things like "is independent ally?". CoO's HashSet would upgrade to `Dictionary<Entity, int>` — purely additive (saves get `flag=0` default). |
| OpinionMap | Per-target sentiment, separate from allegiance. "This villager remembers you saved them" without making them a literal companion. |
| Pack-spawner Part | `Followers.cs` in Qud — entity arrives with auto-spawned allies. Useful for boss encounters. |
| Multiple recruitment paths | Beguile, Rebuke (robot), Cooking-charm. Each is a parallel skill + Effect copying the Persuasion_Recruit shape. Demonstrates the value of typed `IAllyReason` (F.2 should add this even if only one path ships then). |

---

## Where to look

### Plan docs

- **This file** — multi-phase roadmap (you are here)
- [`FOLLOWERS.md`](FOLLOWERS.md) — Phase F.1 detailed plan + post-mortem
- `FOLLOWERS-F2.md` — TBD when F.2 starts
- `FOLLOWERS-F3.md` — TBD when F.3 starts

### Code (current state)

| File | Phase | Purpose |
|---|---|---|
| `Assets/Scripts/Gameplay/AI/BrainPart.cs` | F.1.2 / F.1.4 | `PartyLeader`, `PartyMembers`, `SetPartyLeader`, `IsLedBy`, `GetFinalLeader`, `ArePartyAligned` |
| `Assets/Scripts/Gameplay/AI/FactionManager.cs` | F.1.4 | `ArePartyAligned`-driven hostility exception in `GetFeeling`; `ALLIED_FEELING = 100` constant |
| `Assets/Scripts/Gameplay/Save/SaveSystem.cs` | F.1.3 | `SaveBrainPart` / `LoadBrainPart` extended with PartyLeader + PartyMembers |
| `Assets/Scripts/Gameplay/AI/Goals/FollowLeaderGoal.cs` | F.1.5 | AI goal — greedy step-toward leader with 6 termination cases |

### Tests (current state)

| File | Tests | Phase |
|---|---|---|
| `Assets/Tests/EditMode/Gameplay/AI/FollowerSystemTests.cs` | 32 | F.1.2 + F.1.4 |
| `Assets/Tests/EditMode/Gameplay/AI/FollowLeaderGoalTests.cs` | 13 | F.1.5 |
| `Assets/Tests/EditMode/Gameplay/AI/FollowerSystemAdversarialTests.cs` | 10 | F.1.6 sweep |
| `Assets/Tests/EditMode/Gameplay/Save/Tier1BrainPartTests.cs` | +6 (now 21 total) | F.1.3 — appended to SL.7.5 fixture |

### Reference (Qud decompile)

| File | Symbols |
|---|---|
| `/Users/steven/qud-decompiled-project/XRL.World.Parts/Brain.cs` | `SetPartyLeader` (line 895-948), `IsLedBy` (1438), `GetFinalLeader` (1623), `Forgive` (924-932) |
| `XRL.World.Parts/Followers.cs` | Pack-spawner Part (deferred to F.5+) |
| `XRL.World.Parts/GrantsRepAsFollower.cs` | Faction-rep mechanic (F.3) |
| `XRL.World.Effects/Proselytized.cs` | Recruitment Effect shape (F.2) |
| `XRL.World.Effects/Beguiled.cs` | Charm recruitment variant (F.5+) |
| `XRL.World.Effects/Rebuked.cs` | Robot rebuke recruitment variant (F.5+) |
| `XRL.World.Parts.Skill/Persuasion_Proselytize.cs` | Recruitment skill shape (F.2) |
| `XRL.World.Parts/CompanionCapacity.cs` | Slot-bump Part (F.3) |
| `XRL.World/GetCompanionLimitEvent.cs` | Slot-query event shape (F.3) |

### IDEAS.md cross-ref

The IDEAS.md "Investigate Qud Followers mechanic" chapter contains
the full architectural analysis from the decompile read — useful as
a reference when starting F.2 / F.3 / F.4.

---

## Working principles (mirrored from CLAUDE.md)

These are the constraints every phase must follow:

1. **TDD per CLAUDE.md §2.1** — write the failing test first, confirm
   RED, implement minimum to pass, confirm GREEN. Documented in each
   sub-milestone's commit body.
2. **Smallest blast radius first** (§1.4) — order sub-milestones so
   each commits as one reviewable, independently revertable change.
3. **Verification sweep before code** (§1.2) — every phase opens with
   a corrections table that reads every reference the plan cites.
4. **Cold-eye review after multi-commit features** (§Q1-Q4) —
   symmetry, cross-feature consistency, counter-check completeness,
   doc-vs-impl drift.
5. **Adversarial sweep when 2+ taxonomy surfaces hit** — bug-class
   probes covering scale, boundary, state atomicity, cross-actor,
   save/load reach, etc.
6. **Qud parity over CoO-originals** — when Qud has a pattern that
   addresses the problem, use it. CoO-originals get a 🟡 or ⚪
   marker explaining the divergence.

---

## Open design questions (carry forward to F.2+)

These are flagged in IDEAS.md / FOLLOWERS.md but worth surfacing
again here so they don't get lost:

- **Single recruitment path or several?** F.2 starts with one. F.5+
  adds Beguile / Rebuke if content needs them.
- **Companion limit default?** Probably 1 in F.3 v1. Skill +1, item
  +1 — so player starts at 1, can grow to 3-4 with content.
- **Recruitment energy cost / cooldown?** Qud uses 25 turns. CoO
  probably matches — long enough that recruiting is a commitment.
- **Cross-zone behavior?** F.1.5 gives up gracefully. F.4 needs to
  decide: teleport with leader / despawn-and-respawn / leave-behind-
  with-flag.
- **Mutual defense scope?** Auto-attack all hostiles to leader, OR
  only attack the exact attacker? Qud does the latter.

---

*Updated at the end of each phase.*
