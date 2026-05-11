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
| **Phase** | F.3 — Slot system + faction-rep ✅ COMPLETE |
| **Last updated** | 2026-05-11 |
| **Branch** | `feat/followers-f3-slot-system` (merged) + `fix/follower-f3-audit-pass` (merged) |
| **Sub-milestones complete** | 6 / 6 + post-audit fix pass |
| **Real bugs found** | 1 latent (Finding #8 — ApplyBonus partial-apply atomicity) |
| **Contracts pinned** | 68 (8 query + 5 slot-enforcement + 24 GrantsRep + 4 round-trip + 14 adversarial + 3 wildcard) |
| **Tests added** | 55 EditMode tests across 3 new fixtures + 5 in existing |
| **Qud-parity bonus** | Full comma-delimited `Faction` syntax + `*allvisiblefactions:N` wildcard shipped (plan deferred both to F.5+; user emphasis on Qud parity earned the upgrades) |

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
| CoO has a `PooledEvent` infrastructure equivalent to Qud's `GetCompanionLimitEvent` | ✅ CONFIRMED. `GameEvent` uses a static `Pool` stack (`GameEvent.cs:18, 40-60`) with `Rent()`/`Release()` — same intent as Qud's `PooledEvent<T>`, different shape (one class with string ID + dynamic param dicts vs Qud's per-event-type generic). For F.3.2: `GetCompanionLimitEvent.GetFor(actor, means, baseLimit)` does `GameEvent.New("GetCompanionLimit", ...)` + `SetParameter("Limit", baseLimit)` + `actor.FireEvent(e)` + read back via `e.GetIntParameter("Limit", baseLimit)` + `e.Release()`. | ⚪ confirmed |
| `BrainPart.CurrentZone` is queryable for the "same zone" check | Confirmed — F.1.5 already reads `leader.GetPart<BrainPart>()?.CurrentZone` (see `FollowLeaderGoal.Finished()`). | ⚪ confirmed |
| `BrainPart.PartyMembers` is HashSet enumeration; deterministic? | HashSet enumeration order is NOT deterministic in .NET (implementation-defined). For F.3.3 slot-enforcement, can't rely on enumeration order for "oldest". **Decision**: F.3 v1 ships VETO mode (reject when at-limit), not auto-dismiss. Auto-dismiss deferred to F.5+ which would need a recruit-order tracking field. | 🟡 simplification |
| Items can have Parts that listen for events | Confirmed — existing skill registration pattern (`SkillsPart`, `ActivatedAbilitiesPart`). CompanionCapacity equivalent would be an equippable item. **Defer the equippable item to F.5+ content**; F.3 ships the EVENT + SKILL-side bump only. | 🟡 scope-prune |
| F.2.3's `Persuasion_Recruit` has a `HandleEvent` extension point | ✅ CONFIRMED. Every `Part` has a virtual `HandleEvent(GameEvent e)` (`Part.cs:49-52`). `Entity.FireEvent(e)` (`Entity.cs:255-265`) iterates ALL Parts and calls each one's `HandleEvent`. `SkillsPart.HandleEvent` is the canonical consumer (`SkillsPart.cs:379-419`) — filters by `e.ID.StartsWith("Command")` for skill-dispatch. For F.3.2: `Persuasion_Recruit` (which is itself a `BaseSkillPart`, attached to the actor) overrides `HandleEvent` to look for `e.ID == "GetCompanionLimit"` and bumps `Limit` by 1 when `Means == "Recruit"`. | ⚪ confirmed |

**Both blockers cleared.** F.3.2 design locked: CoO-idiomatic `GameEvent`-fired query (Option A from the sweep) — same shape as how `SkillsPart.HandleEvent` already dispatches commands, just inverse direction (skills listen + bump rather than dispatch + execute).

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

## Sub-milestone progress

| Sub-milestone | Status | Tests | Commit |
|---|---|---|---|
| F.3.1 Plan + verification sweep | ✅ | — | `4c7fa67` (initial) → `b53c850` (blockers cleared) |
| F.3.2 GetCompanionLimitEvent + skill bump | ✅ | 8 | `ab7f39a` |
| F.3.3 Slot enforcement (Veto #8 at_companion_limit) | ✅ | 5 (in PersuasionRecruitTests) | `f81a8c1` |
| F.3.4 GrantsRepAsFollowerPart + Qud comma-delim syntax | ✅ | 17 | `2d62559` |
| F.3.5 Save/load round-trip | ✅ | 4 | `44ff7de` |
| F.3.6 Adversarial sweep + cold-eye + merge | ✅ | 14 | `3464221` → `5849c8d` (merge) |
| Post-F.3 audit fixes (5 findings) | ✅ | 3 (wildcard tests) | `21a61a3` → `2dec554` (merge) |
| **TOTAL** | **6 / 6 + audit** | **55** | — |

---

## F.3.6 — Cold-eye review (CLAUDE.md §Q1-Q4)

Ran after all 5 production sub-milestones landed; before adversarial
sweep + merge.

### Q1 — Symmetry

- `GetCompanionLimitEvent.GetFor` mirrors Qud's `GetFor` — same (actor,
  means, baseLimit) signature → int return ✓
- `Persuasion_Recruit.HandleEvent` mirrors `Persuasion_Proselytize`'s
  HandleEvent — per-means filter + bump ✓
- `GrantsRepAsFollowerPart.ApplyBonus` and `UnapplyBonus` are inverses
  (parse same Faction string, apply ± delta, idempotent guards) ✓
- Veto numbering: F.2.3 was 1-7 after audit removed dead #8; F.3.3
  added new #8 (`at_companion_limit`) — sequential preserved ✓

✅ Symmetry passes.

### Q2 — Cross-feature consistency

- `GetCompanionLimitEvent.GetFor` uses `GameEvent.New + FireEvent +
  Release` — same idiom as `TurnManager.EndTurn` (TurnManager.cs:315-318)
- `Persuasion_Recruit.HandleEvent` follows the shape of
  `SkillsPart.HandleEvent` (SkillsPart.cs:379-419) — check `e.ID`, do
  work, return `base.HandleEvent(e)`
- `EmitSkillRejectedDiag(ctx, reason)` consistent across all 8 vetos
- `GrantsRepAsFollowerPart` extends `Part` with public fields; hits
  SaveSystem's generic `WritePublicFields` fall-through (SaveSystem.cs:
  1126-1127) — same pattern as other reflective Parts

✅ Consistency passes.

### Q3 — Counter-check completeness

| Positive | Counter |
|---|---|
| `GetFor_WithPersuasionRecruit_BumpsLimitBy1` | `GetFor_WrongMeans_NoBump` + `GetFor_AfterSkillRemoval_LimitDropsBack` + `GetFor_NullActor_NoCrash` |
| `AtLimit_VetoFires_NoEffectApplied` | `BelowLimit_RecruitProceeds` + `AfterDismissingFollower_NewRecruitProceeds` |
| `Apply_PlayerLeader_SameZone_AppliesRep` | `Apply_LeaderNotPlayer_DoesNotApply` + `Apply_DifferentZone_DoesNotApply` |
| `Parser_PerFactionOverride_UsesColonValue` | `Parser_EmptyFactionString_NoOp` + `Parser_WhitespaceOnlyFaction_NoOp` |
| `ApplyThenUnapply_NetZeroChange` | `Apply_Idempotent_DoesNotStackOnRepeatCalls` |

✅ Counter-checks complete.

### Q4 — Doc-vs-impl drift

**🟡 POSITIVE drift documented:** F.3.1 plan deferred Qud's
comma-delimited Faction syntax to F.5+, but user emphasized Qud
parity. Shipped full Qud-parity syntax (single + comma + per-entry
colon override + mixed + whitespace tolerance). Net: F.3 is CLOSER
to Qud than the plan promised. Documented in updated status banner.

Post-audit pass: backported `*allvisiblefactions:N` wildcard for the
same reason. Status banner updated to reflect both Qud-parity
bonuses.

Ran. **1 positive drift documented, 0 negative drift, 0 latent bugs.**

---

## F.3.6 — Adversarial sweep results

Per CLAUDE.md "Adversarial test sweep" gate. F.3 hits 7 taxonomy
surfaces (state atomicity, cross-actor flows, save/load reach,
anti-exploit gates, diag dispatch invariants, cross-system
aggregation, parser malformed inputs).

**`F3SlotSystemAdversarialTests.cs` — 14 tests across 8 surfaces:**

| Surface | Probe | Result |
|---|---|---|
| State atomicity | Apply→unapply→apply → correct final state | ✅ |
| State atomicity | Zone-transit oscillation × 5 → no rep drift | ✅ |
| Cross-actor | Slot limits per-actor (Alice ≠ Bob count) | ✅ |
| Cross-actor | NPC leader → no player-rep flow | ✅ |
| Anti-exploit | 100 apply/unapply cycles → net zero (no rep-pump) | ✅ |
| Anti-exploit | Empty PartyMembers handled cleanly | ✅ |
| Cross-system aggregation | Two followers same faction → linear stack | ✅ |
| Cross-system aggregation | Two followers different factions → independent | ✅ |
| Diag dispatch | At-limit veto emits exactly 1 SkillRejected record | ✅ |
| Parser malformed | "Snapjaws:" (colon, no value) → fallback to Value | ✅ |
| Parser malformed | "Snapjaws:abc" (non-numeric) → fallback to Value | ✅ |
| Parser malformed | ":,::," (colon-only entries) → graceful skip | ✅ |
| Parser malformed | "Snapjaws:-10" (negative) → applied as -10 | ✅ |
| Scale | 10 over-limit pre-existing recruits → veto fires | ✅ |

**0 bugs found in adversarial sweep.** Per CLAUDE.md honesty bound:
this doesn't prove F.3 is bug-free — the bug classes are bounded by
what the author imagined. The sweep's value is regression
infrastructure: future changes break visibly with a named test.

---

## Post-F.3 audit pass — findings table

Per CLAUDE.md cold-eye self-directive: ran a fresh holistic audit
after F.3 shipped. Explore agent surfaced 19 findings; cross-check
triaged to 5 actionable fixes.

| # | Severity | Finding | Fix shipped? |
|---|---|---|---|
| 1 | 🟡 | `GetCompanionLimitEvent.GetFor` leaked event on exception | ✅ try-finally |
| 2 | 🟡 | Qud-parity restore: `*allvisiblefactions:N` wildcard | ✅ ported (3 regression tests) |
| 3 | 🟡 | `OnDestroyObjectEvent`/`SuspendingEvent` unapply | ⚪ defer (CoO lacks events) |
| 4 | 🟡 | `SyncTarget` auto-dismiss-oldest | ⚪ defer (deliberate F.3.1 divergence) |
| 5 | 🟡 | Sifrah minigame option | ⚪ defer (entire subsystem missing) |
| 6 | 🟡 | `CountRecruitedFollowers` foreach without snapshot | ✅ snapshot pattern |
| 7 | 🟡 | Veto #8 didn't clamp negative limit | ✅ `Math.Max(0, ...)` |
| 8 | 🟡 | `ApplyBonus` partial-apply atomicity gap | ✅ eager flag + `HasAnyApplicableEntry` |
| 9 | 🟡 | `DeepCopy` reset of `AppliedBonus` | ⚪ defer (no CoO cloning) |
| 10-12 | confirmed not-bugs | various | — |
| 13-14 | 🧪 | (false alarm — tests already exist) | — |
| 15-19 | ⚪ informational | various | — |

**Net actionable: 5 fixes (1 real latent bug, 1 Qud-parity restore,
3 defense-in-depth).** All shipped in commit `21a61a3 → 2dec554`.

---

## Qud-parity status (final)

| Qud feature | F.3 status |
|---|---|
| `GetCompanionLimitEvent.GetFor` | ✅ ported (CoO `GameEvent`-backed) |
| Per-means slot bump (skill grants +1 "Recruit") | ✅ ported |
| Comma-delimited factions in `GrantsRepAsFollower` | ✅ ported (F.3.4, user-emphasis bonus) |
| Per-faction `:N` override | ✅ ported |
| `*allvisiblefactions:N` wildcard | ✅ ported (post-audit, user-emphasis bonus) |
| Apply on player-led + same-zone, unapply on either break | ✅ ported |
| Negative deltas honored | ✅ ported |
| EndTurn hook | ✅ ported |
| `SyncTarget` auto-dismiss-oldest | ⚪ deferred to F.5+ (CoO uses veto — divergence documented) |
| `OnDestroyObjectEvent`/`SuspendingEvent` explicit unapply | ⚪ deferred (CoO lacks event surfaces) |
| `DeepCopy` reset of `AppliedBonus` | ⚪ deferred (no CoO cloning) |
| `Persuasion_Proselytize` Sifrah minigame option | ⚪ deferred (entire subsystem missing) |

**Ported: 8 features in full. Deferred: 4 features with documented
gameplay-impact rationale.** F.3 represents the highest Qud-parity
coverage of any Followers phase so far.

---

*Updated as each sub-milestone ships + post-mortem on close.*
