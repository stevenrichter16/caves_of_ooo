# Followers System — Phase F.3: Slot System + Faction-Rep-as-Follower

> **Living plan + findings document.** Phase F.3 caps how many followers
> the player can have at once (slot system) and makes followers grant
> the player faction reputation while they're in the player's party
> (faction-rep mechanic). NO new recruitment paths, NO cross-zone
> behavior — those land in F.4+. See
> [`FOLLOWERS-ROADMAP.md`](FOLLOWERS-ROADMAP.md) for the multi-phase
> overview and [`FOLLOWERS-F2.md`](FOLLOWERS-F2.md) for the F.2 recruitment ship.

---

## Status banner

| Field | Value |
|---|---|
| **Phase** | F.3 — Slot system + faction-rep ⏳ IN PROGRESS |
| **Last updated** | 2026-05-10 |
| **Branch** | `feat/followers-f3-slot-system` |
| **Sub-milestones complete** | 0 / 6 |
| **Real bugs found** | — |
| **Contracts pinned** | — |
| **Tests added** | — |

---

## Goal

Two orthogonal mechanics, both substrate-layer:

1. **Slot system** — cap recruitment via a query event. Skills and
   items can each contribute slots. Veto recruit attempts when the
   actor is at-limit. (F.5+ may upgrade to auto-dismiss-oldest.)

2. **Faction-rep-as-follower** — a `GrantsRepAsFollower` Part on a
   creature blueprint grants the player faction rep while that
   creature is in the player's party AND in the player's current
   zone. Reverses when either condition fails.

Mirror Qud's `GetCompanionLimitEvent`, `CompanionCapacity` Part, and
`GrantsRepAsFollower` Part as the reference architecture.

---

## Reference — Qud's pattern

| File | Symbol | What it does |
|---|---|---|
| `XRL.World/GetCompanionLimitEvent.cs:35-60` | `GetFor(Actor, Means, BaseLimit)` | Fires a pooled event; listeners do `E.Limit += N` to bump |
| `XRL.World.Parts.Skill/Persuasion_Proselytize.cs:47-54` | `HandleEvent(GetCompanionLimitEvent E)` | If E.Means == "Proselytize", bumps `E.Limit++` (skill grants +1) |
| `XRL.World.Parts/CompanionCapacity.cs:45-59` | `HandleEvent(GetCompanionLimitEvent E)` | Item-wearer-side; separate `Proselytized`/`Beguiled` counter fields, bumps `E.Limit += <counter>` per means |
| `XRL.World.Parts.Skill/Persuasion_Proselytize.cs:93-123` | `SyncTarget` | Slot enforcement: trim oldest followers if over limit (Qud's auto-dismiss path) |
| `XRL.World.Parts/GrantsRepAsFollower.cs:128-141` | `CheckApplyBonus(who)` | Each turn: if `who == player && CurrentZone == player.CurrentZone && !AppliedBonus` → `ApplyBonus`; else if `AppliedBonus && condition broken` → `UnapplyBonus` |
| `XRL.World.Parts/GrantsRepAsFollower.cs:62-92` | `ApplyBonus` | Parses comma-delimited faction list (`"FactionA:5,FactionB"`); calls `PlayerReputation.Modify(faction, value)` for each |

---

## Verification sweep — Pre-impl corrections table

Per CLAUDE.md §1.2. All file:line citations into the current CoO tree.

| What I assumed | What's there | Status |
|---|---|---|
| CoO has a `PlayerReputation` static API with `Modify(faction, delta)` | Confirmed via `TradeSystem.cs:61`, `ConversationActions.cs:187, 192`. Static manager pattern. Signature: `PlayerReputation.Modify(string faction, int delta)`. | ⚪ confirmed |
| CoO has a `PooledEvent` infrastructure equivalent to Qud's `GetCompanionLimitEvent` | **🔴 NOT CONFIRMED** — Qud uses `PooledEvent<T>` with cascade levels + handler dispatch. CoO's `GameEvent` shape may or may not match. Need to read `GameEvent.cs` before designing `GetCompanionLimitEvent`. **F.3.1 sweep step 1**. | 🔴 BLOCKER |
| `BrainPart.CurrentZone` is queryable for the "same zone" check | Confirmed — F.1.5 already reads `leader.GetPart<BrainPart>()?.CurrentZone` (see `FollowLeaderGoal.Finished()`). | ⚪ confirmed |
| `BrainPart.PartyMembers` is HashSet enumeration; deterministic? | HashSet enumeration order is NOT deterministic in .NET (implementation-defined). For F.3.3 slot-enforcement, can't rely on enumeration order for "oldest". **Decision**: F.3 v1 ships VETO mode (reject when at-limit), not auto-dismiss. Auto-dismiss deferred to F.5+ which would need a recruit-order tracking field. | 🟡 simplification |
| Items can have Parts that listen for events | Confirmed — existing skill registration pattern (`SkillsPart`, `ActivatedAbilitiesPart`). CompanionCapacity equivalent would be an equippable item. **Defer the equippable item to F.5+ content**; F.3 ships the EVENT + SKILL-side bump only. | 🟡 scope-prune |
| F.2.3's `Persuasion_Recruit` has a `HandleEvent` extension point | **🔴 NOT CONFIRMED** — `BaseSkillPart` may or may not auto-register `HandleEvent` for arbitrary event types. Need to read how `Cudgel_Conk` or another skill hooks events. **F.3.1 sweep step 2**. | 🔴 BLOCKER |

**🔴 2 blockers** for the sweep step. Both need to clear before F.3.2 implementation lands.

---

## Scope-prune

What Qud's F.3 equivalent has that F.3 v1 **doesn't ship**:

| Qud feature | Why deferred |
|---|---|
| `CompanionCapacity` Part (equippable item that grants slots) | F.5+ content layer; F.3 v1 ships the EVENT infrastructure + SKILL-side bump only |
| Auto-dismiss-oldest on over-limit (Qud's `SyncTarget`) | F.5+ — needs recruit-order tracking (HashSet order isn't deterministic). F.3 v1 vetoes instead |
| `Beguiled` slot category | F.5+ — only relevant when Beguile is a second recruitment path |
| `*allvisiblefactions:N` syntax (apply same rep to every visible faction) | F.5+ — content syntax sugar; basic single-faction works for F.3 |
| Per-target-leader rep checks (Qud's `who.CurrentZone == ParentObject.CurrentZone`) | F.3 v1 — yes, this is in scope. F.3 already needs zone-equality |
| `GrantsRepAsFollower.AppliedBonus` field that survives save/load | F.3 v1 — yes, in scope (SL.6 pinned Part contract makes it free) |
| `DeepCopy` override that resets `AppliedBonus` | F.5+ — CoO has no in-game cloning yet |

---

## 6 Sub-milestones

Per CLAUDE.md §1.4. Each commits as one reviewable change.

### F.3.1 — Plan + verification sweep + design lockdowns (this commit)

Doc only. No code changes. **Clears the two 🔴 blockers** above
before F.3.2 starts:

- Read `GameEvent.cs` / `PooledEvent`-equivalent — choose CoO's event-fire shape for `GetCompanionLimitEvent`.
- Read `BaseSkillPart` or `Cudgel_Conk` for the `HandleEvent` registration pattern, OR design an alternative (e.g., direct method call from Persuasion_Recruit).

If the sweep reveals CoO has no PooledEvent-equivalent or the skill-event-handler pattern is too heavyweight, **scope-prune** to a simpler interface: a static method `GetCompanionLimitEvent.GetFor(actor, means, baseLimit)` that walks the actor's Parts + Skills directly (no event dispatch). Same observable behavior, less ceremony.

### F.3.2 — `GetCompanionLimitEvent` (static query) + skill slot bump

**New file:** `Assets/Scripts/Gameplay/AI/GetCompanionLimitEvent.cs`

```csharp
public static class GetCompanionLimitEvent
{
    public const int BASE_LIMIT_DEFAULT = 0;
    public const string MEANS_RECRUIT = "Recruit";

    public static int GetFor(Entity actor, string means, int baseLimit = BASE_LIMIT_DEFAULT)
    {
        // Walks actor's SkillsPart + Parts asking each for slot
        // contributions for this means. Returns base + sum of bumps.
    }
}
```

**Modification to** `Persuasion_Recruit.cs`:
- New method `int GetSlotBumpFor(string means)` → returns 1 if means=="Recruit", 0 otherwise. Called by `GetCompanionLimitEvent.GetFor`.

**TDD test surface** (~6-8 tests):
- `GetFor_NoListeners_ReturnsBaseLimit` — actor with no skill returns 0
- `GetFor_WithRecruitSkill_Returns1` — skill grants +1
- `GetFor_WrongMeans_ReturnsBaseLimit` — `means != "Recruit"` doesn't trigger bump
- `GetFor_NullActor_ReturnsBaseLimit_NoCrash`
- `GetFor_MultipleListeners_Sums` — two skill instances grant +2 (defense-in-depth; CoO doesn't normally allow duplicate skills but verify summation)
- Counter-check: `GetFor` doesn't double-count
- Counter-check: removing the skill drops the bump back

### F.3.3 — Slot enforcement in Persuasion_Recruit

**Modification to** `Persuasion_Recruit.cs`:
- Add Veto #9 `at_companion_limit` BEFORE the roll: check `currentRecruitedFollowers >= GetCompanionLimitEvent.GetFor(actor, "Recruit", 0)`
- "currentRecruitedFollowers" = count of `actor.GetPart<BrainPart>().PartyMembers` where each has a `RecruitedEffect` whose Recruiter == actor

**TDD test surface** (~5-6 tests):
- `AtLimit_VetoFires_NoEffectApplied` — actor with 1 follower + 1 slot → veto
- `BelowLimit_RecruitSucceeds` — actor with 0 followers + 1 slot → success
- `AboveLimit_AfterSkillRemove_NextAttemptFails` — counter-check
- `Veto9_EmitsRejected_AtCompanionLimit_Diag`
- `LimitCheck_OnlyCountsRecruitedFollowers` — other PartyMembers (e.g. added via direct SetPartyLeader without RecruitedEffect) don't count toward the limit

### F.3.4 — `GrantsRepAsFollower` Part + apply/unapply lifecycle

**New file:** `Assets/Scripts/Gameplay/AI/GrantsRepAsFollowerPart.cs`

```csharp
public class GrantsRepAsFollowerPart : Part
{
    public string Faction = "";
    public int Value;
    public bool AppliedBonus;

    public void CheckApplyBonus(Entity leader, Zone leaderZone)
    {
        bool shouldApply = leader != null
            && leader.Tags.ContainsKey("Player")
            && ParentEntity.GetPart<BrainPart>()?.CurrentZone == leaderZone
            && !AppliedBonus;
        bool shouldUnapply = AppliedBonus && (
            leader == null
            || !leader.Tags.ContainsKey("Player")
            || ParentEntity.GetPart<BrainPart>()?.CurrentZone != leaderZone
        );

        if (shouldApply) { PlayerReputation.Modify(Faction, Value); AppliedBonus = true; }
        if (shouldUnapply) { PlayerReputation.Modify(Faction, -Value); AppliedBonus = false; }
    }
}
```

Hook: invoked from a per-turn hook (probably `BrainPart.OnEndTurn` or
a TurnManager subscription). F.3.4's sweep needs to identify the right
hook point.

**TDD test surface** (~7-9 tests):
- `ApplyBonus_LeaderIsPlayer_SameZone_AppliesRep`
- `ApplyBonus_LeaderIsPlayer_DifferentZone_DoesNotApply`
- `ApplyBonus_LeaderIsNotPlayer_DoesNotApply` (recruited by NPC, not player)
- `UnapplyBonus_LeaderLeavesZone_Unapplies`
- `UnapplyBonus_FollowerDismissed_Unapplies` (covered by RecruitedEffect.OnRemove → goal pop → re-check)
- `Idempotence_DoubleApply_DoesNotStack`
- `Idempotence_DoubleUnapply_DoesNotStack`
- Counter-check: actor without GrantsRepAsFollowerPart → no rep change
- Counter-check: empty Faction string → no-op

### F.3.5 — Save/load round-trip for `AppliedBonus`

**Modification to** `SaveSystem.cs` (or none, if `Part` auto-reflects):
- Verify `GrantsRepAsFollowerPart` round-trips with `Faction`, `Value`, `AppliedBonus` preserved.
- If the Part follows the SL.6-pinned contract (public fields, reflection-saved), no manual save dispatch needed.

**TDD test surface** (~3-4 tests):
- `GrantsRepAsFollowerPart_RoundTrips_FactionAndValueAndAppliedBonus`
- `GrantsRepAsFollowerPart_AppliedBonusTrue_RoundTrips`
- `GrantsRepAsFollowerPart_AppliedBonusFalse_RoundTrips`
- `GrantsRepAsFollowerPart_OnSave_NoRepChangeFires` (the part's state-shadow doesn't fire Modify during save)

### F.3.6 — Adversarial sweep + cold-eye review + merge

Surfaces F.3 hits (CLAUDE.md "Adversarial test sweep" gate):
- State atomicity (apply/unapply pairs must be balanced)
- Cross-actor flows (player as leader, NPC as recruit, NPC as recruiter)
- Save/load reach (Part survives serialization)
- Anti-exploit gates (Veto #9 holds; no rep-pump via add+remove)
- Probability boundaries — N/A (no random rolls in F.3)
- Diag dispatch invariants (Veto #9 emits a record)
- Cross-system aggregation (multiple GrantsRepAsFollowerPart instances on different followers all apply correctly to the same player faction)

**Adversarial fixture:** `F3SlotSystemAdversarialTests.cs` (~15-25 tests).

Cold-eye review per CLAUDE.md §Q1-Q4 as usual.

**Merge to main** after 0 substantive findings + 0 bugs.

---

## Reusable utilities (don't reinvent)

| Utility | Path | Used for |
|---|---|---|
| `PlayerReputation.Modify(faction, delta)` | (file path TBD in F.3.4 sweep) | Apply/unapply rep delta |
| `BrainPart.PartyLeader` / `PartyMembers` | F.1.2 | Slot-count + apply/unapply triggers |
| `RecruitedEffect.Recruiter` | F.2.2 | Filter "my followers" for slot-count |
| `Persuasion_Recruit` veto-emit pattern | F.2.3 | Veto #9 follows same shape |
| `SaveSystem.WritePublicFields` reflection | (existing) | Part auto-saves if pure-public-fields |
| `BrainPart.CurrentZone` | F.1.5 (FollowLeaderGoal usage) | Zone equality check |

---

## Self-review pre-flagged findings

Designed-in tradeoffs to surface BEFORE committing (per CLAUDE.md §5):

- **🟡 Veto mode instead of auto-dismiss-oldest** — Qud auto-dismisses
  the oldest when at-limit. CoO F.3 v1 vetoes instead. Player UX: "you
  can't recruit until you dismiss one of your existing followers."
  Simpler, no recruit-order tracking field needed. Auto-dismiss is
  F.5+ when content makes it valuable.
- **🟡 No `CompanionCapacity` item Part** — Qud's "item that grants
  slots" pattern deferred to F.5+. F.3 v1 only ships the skill-side
  bump (+1 from `Persuasion_Recruit`).
- **🟡 Single faction per Part** — Qud's comma-delimited
  `"FactionA:5,FactionB:-3"` syntax is more flexible but more complex.
  F.3 v1 ships single-faction; comma syntax is a 🔵 future
  enhancement.
- **🔵 Per-turn hook for `GrantsRepAsFollowerPart.CheckApplyBonus`** —
  Qud uses `EndTurn` event. CoO has a TurnManager; need to identify
  the right subscription point in F.3.4's sweep. The hook design will
  influence test design.
- **⚪ No `*allvisiblefactions` syntax** — Qud has a "apply to all
  visible factions" wildcard. Out of scope.

---

## Open design questions (carry forward)

- **Default `BASE_LIMIT_DEFAULT`?** 0 means player starts with 0 slots,
  must purchase the Recruit skill to have any. Alternative: 1 (always
  one slot, skill bumps to 2). Lockdown in F.3.2 after playtest feel.
- **Veto-vs-auto-dismiss UX choice** — confirm via short playtest
  before F.5+ revisits.
- **Cross-zone rep behavior** — F.3 says "same zone only". When the
  follower is in a different zone but still your follower, no rep
  flows. Is this the right design? Or should it flow as long as
  they're in your party regardless of zone? Pin in F.3.4.

---

*Updated as each sub-milestone ships.*
