# Followers System — Phase F.1: Leader/Follower Scaffolding

> **Living plan + findings document.** Phase F.1 ships the data substrate +
> AI behavior for one entity to follow another. NO recruitment skill, NO
> dismiss action, NO faction-rep-as-follower content yet — those land in
> Phases F.2 / F.3 / F.4 respectively. See `IDEAS.md` for the larger
> Followers design plus the Qud-decompile analysis (chapter "Investigate
> Qud Followers mechanic") for the reference architecture.

---

## Status banner

| Field | Value |
|---|---|
| **Phase** | F.1 — Leader/follower scaffolding |
| **Last updated** | 2026-05-10 |
| **Branch** | `feat/followers-f1-leader-scaffold` |
| **Sub-milestones complete** | 0 / 6 |
| **Real bugs found** | 0 |
| **Contracts pinned** | 0 |
| **Tests added** | 0 |

---

## Goal

Pin the **bidirectional leader/follower link** on `BrainPart` so a future
recruitment skill (F.2) can populate it without touching the data
substrate. Mirror **Qud's `Brain.SetPartyLeader` semantics as closely as
the existing CoO architecture supports**, since the Qud version has been
field-tested for years and its invariants are well-understood:

- Reference identity preserved on save/load (SL.8 contract)
- Cycle detection on every set
- `Forgive` on leadership change (clear leader from follower's
  `PersonalEnemies`)
- Hostility guard: party members don't attack each other
- Chain-walking helpers (`IsLedBy`, `GetFinalLeader`) for nested followers

---

## Reference — Qud's pattern

See `/Users/steven/qud-decompiled-project/XRL.World.Parts/Brain.cs`:

| Symbol | Line | What it does |
|---|---|---|
| `LeaderReference` | 217 | `GameObjectReference` — the upward pointer |
| `PartyMembers` | 229 | `PartyCollection` (Dictionary<BaseID, flags>) — the downward roster |
| `PartyLeader` property | 450 | Thin wrapper around `LeaderReference.Object` |
| `SetPartyLeader(Object, Flags, Transient, Silent)` | 895 | The load-bearing primitive. Bidirectional mirror, cycle check, Forgive, sound, event fire. |
| `SetAlliedLeader<T>(Object)` | 889 | Composite: TakeAllegiance<T> + SetPartyLeader. **Deferred to F.2 (recruitment).** |
| `BecomeCompanionOf(Object)` | 1432 | Convenience wrapper. **Deferred to F.2.** |
| `IsLedBy(Object)` | 1438 | Walks `LeaderReference` chain looking for Object. |
| `IsPlayerLed()` | 1478 | Walks chain looking for IsPlayer or LeftBehindByPlayer. |
| `GetFinalLeader()` | 1623 | Top of the chain. |

F.1 mirrors the parts of this surface that don't require Allegiance
(typed `IAllyReason`) infrastructure. The typed-reason layer is built
on top of the data substrate; ship the data substrate first.

---

## Verification sweep — Pre-impl corrections table

Per CLAUDE.md §1.2. All file:line citations into the current CoO tree.

| What I assumed | What's there | Status |
|---|---|---|
| `BrainPart` has no party fields | Confirmed via Explore-agent read of `BrainPart.cs:1-457`. Only Entity-ref fields are `Target` (54) and `PersonalEnemies` HashSet (61). | ⚪ confirmed |
| Goal stack API: `PushGoal` / `PeekGoalAt` / `GoalCount` / `Pop` exists | `BrainPart.cs:236-367`. PeekGoalAt(0) = bottom; GoalCount-1 = top. (SL.7.5 already pinned this.) | ⚪ confirmed |
| `SaveBrainPart` field order is stable; goals serialized AFTER scalar fields | `SaveSystem.cs:1413-1431` writes scalars, then `1433+` writes goals. **Safe insertion point: after `ThinkOutLoud` (1431), before goal-count (1433)**. Mirror in LoadBrainPart at 1473→1476. | ⚪ confirmed |
| Hostility decisions flow through one centralized gate | `FactionManager.IsHostile(source, target)` (`FactionManager.cs:197-200`) calls `GetFeeling()` (166-192). Three call sites in `AIHelpers.cs`: `FindNearestHostile` (217), `IsValidHostileTarget` (180), `FindHostilesInRadius` (260). **Injection point: `GetFeeling()` lines 171-177**, alongside the existing `IsPersonallyHostileTo` check. | ⚪ confirmed |
| `MoveToGoal` is the right template for `FollowLeaderGoal` | `Goals/MoveToGoal.cs:1-97` — stores TargetX/Y, A*-paths via `TryApproachWithPathfinding`, Finished() checks arrival or `Age > MaxTurns`. | ⚪ confirmed |
| `GoalHandler` base has Age, ParentBrain, ParentEntity, CurrentZone, Think() | `Goals/GoalHandler.cs:1-136`. All present. | ⚪ confirmed |
| Zone awareness: a follower's leader might be in a different zone | `Entity` has NO `GetCurrentZone()` API. Must read `leader.GetPart<BrainPart>()?.CurrentZone`. Cross-zone is **out of scope F.1** — `FollowLeaderGoal` will fail/give-up if zones mismatch. | 🟡 partial |
| `PartyMembers` collection shape | Qud uses `PartyCollection : Dictionary<BaseID, flags>`. CoO has no BaseID-keyed map type; will use `HashSet<Entity>` for O(1) membership (matches `PersonalEnemies` convention). Per-member flags (Qud uses flags for "is independent ally?" etc) **deferred** — not needed for F.1. | 🟡 simplification |

**No false premises detected.** One deliberate simplification (flags-per-member deferred); one out-of-scope item (cross-zone pursuit).

---

## Scope-prune

What Qud's Followers has that F.1 **doesn't ship**:

| Qud feature | Why deferred |
|---|---|
| Typed allegiance reasons (`AllyProselytize`, `AllyBeguile`, etc.) | Built on top of AllegianceSet. F.1 is the data substrate ONLY; typed reasons land in F.2 when recruitment ships. |
| `SetAlliedLeader<T>` | Same — composite wrapper on Allegiance + SetPartyLeader. |
| `BecomeCompanionOf` | Same — combines TakeAllegiance with StopFight. |
| Recruitment skills (Proselytize, Beguile, Rebuke) | F.2. |
| `Dismiss` inventory action | F.2 (paired with recruitment). |
| Slot system (`GetCompanionLimitEvent`) | F.3 — only matters when recruitment can over-fill. |
| `GrantsRepAsFollower` Part | F.3 — content layer. |
| Per-member flags (independent ally, etc.) | F.2+ — needed only when typed reasons land. |
| Cross-zone pursuit | F.4+ — bigger zone-system work. |
| Mutual defense (follower attacks leader's attackers) | F.4+ — combat-rules surface. |
| `LeftBehindByPlayer` semantic | F.4+. |
| `IsPlayerLed()` helper that walks for player OR LeftBehindByPlayer | F.4+. Keep `IsLedBy(Entity)` for now; trivial extension. |

---

## Sub-milestones — smallest blast radius first

Per CLAUDE.md §1.4. Each commits as one reviewable change, independently
revertable, ships one complete testable behavior.

### F.1.1 — Plan to disk + verification sweep (this commit)
Doc only. No code changes.

### F.1.2 — `PartyLeader` + `PartyMembers` + `SetPartyLeader` + cycle detection
**The data substrate.** Bidirectional integrity is the load-bearing
invariant — must ship together.

Additions to `BrainPart`:
```csharp
public Entity PartyLeader;                          // upward pointer
public HashSet<Entity> PartyMembers = new HashSet<Entity>();  // downward roster

public bool SetPartyLeader(Entity newLeader);       // returns false if rejected (cycle, self-ref)
public bool IsLedBy(Entity candidate);              // chain-walks LeaderReference
public Entity GetFinalLeader();                     // top of the chain
```

**`SetPartyLeader` semantics (mirroring Qud Brain.cs:895-948):**
1. `newLeader == ParentEntity` → reject + `Think("can't follow self")` + return false
2. `newLeader != null && IsLedBy(newLeader chain points at us)` → cycle, reject + `Think("leader cycle blocked")` + return false
3. Idempotent: setting same leader twice is a no-op + return true
4. **Remove** `ParentEntity` from old leader's `PartyMembers` (if old leader exists)
5. **Assign** `PartyLeader = newLeader`
6. **Add** `ParentEntity` to new leader's `PartyMembers` (if newLeader != null)
7. **Forgive**: `PersonalEnemies.Remove(newLeader)` on `ParentEntity` (so the follower stops being personally hostile to its new leader)
8. Return true

**TDD test surface** (~12-15 tests in new `FollowerSystemTests.cs`):
- Empty → SetPartyLeader(A): leader=A, A.PartyMembers contains self
- A → B → C chain: C.IsLedBy(A) true; A.IsLedBy(C) false; C.GetFinalLeader() == A
- Bidirectional remove: A.PartyLeader=B, then A.PartyLeader=C; B.PartyMembers no longer has A
- SetPartyLeader(self): rejected, returns false, leader unchanged
- Cycle: A→B exists; B.SetPartyLeader(A) rejected, returns false
- Null leader: A.SetPartyLeader(null) clears leader + removes from old leader's PartyMembers
- Forgive: A.PersonalEnemies.Add(B); A.SetPartyLeader(B); B no longer in A.PersonalEnemies
- Idempotence: A.SetPartyLeader(B) twice — no duplicate in B.PartyMembers
- Counter-check: GetFinalLeader on unleadered brain returns null
- Counter-check: IsLedBy(null) returns false
- Counter-check: deep chain (A→B→C→D) — D.IsLedBy(A) true; A.IsLedBy(D) false

### F.1.3 — Save/load round-trip for the new fields
**Extends SL.7.5's BrainPart contract.** Pin that `PartyLeader` and
`PartyMembers` survive the save graph, AND that reference identity is
preserved (SL.8 contract) — if Actor has Leader L and L is in the save
graph, after round-trip Actor.PartyLeader and L are the SAME loaded
instance.

Modifications to `SaveSystem.SaveBrainPart` / `LoadBrainPart`:
- Insert AFTER `writer.Write(brain.ThinkOutLoud)` line 1431 and BEFORE
  `goals` block at 1433:
  - `writer.WriteEntityReference(brain.PartyLeader)`
  - `writer.Write(brain.PartyMembers.Count)`
  - foreach → `writer.WriteEntityReference(member)`
- Mirror reads in LoadBrainPart at line 1473→1476.

**TDD test surface** (~5-6 tests added to `Tier1BrainPartTests.cs`):
- Empty PartyLeader → loads as null
- Set PartyLeader to a peer entity → leader survives via token graph
- Empty PartyMembers → loads as count=0
- Multiple PartyMembers → all survive (count + each entity ref)
- **Identity check (SL.8 contract)**: if leader X and member M (with M.PartyLeader=X) are both in the save graph, after round-trip, `loaded(M).PartyLeader == loaded(X)` (AreSame, not AreEqual)
- Cycle-attempt round-trip: pin that a follow-attempt that was rejected pre-save remains null post-load

### F.1.4 — Hostility guard
Inject a party-aware exception into `FactionManager.GetFeeling`. If
`source.PartyLeader == target` OR `target.PartyLeader == source` OR
`source.GetFinalLeader() == target.GetFinalLeader()` (same party root),
return a non-hostile feeling (HIGHER than the hostile threshold).

Implementation guard:
```csharp
public static int GetFeeling(Entity source, Entity target) {
    // ... existing IsPersonallyHostileTo check at line 171-177 ...

    // F.1.4 — party-leader exception
    if (ArePartyAligned(source, target)) {
        return ALLIED_FEELING;  // e.g. +50, well above HOSTILE_THRESHOLD (-10)
    }

    // ... existing faction/rep logic ...
}
```

`ArePartyAligned(a, b)`:
- a == null or b == null → false
- a.PartyLeader == b → true
- b.PartyLeader == a → true
- a and b share GetFinalLeader() (and that leader is not null) → true
- else false

**TDD test surface** (~8 tests in `FollowerSystemTests.cs`):
- Without leader/follower: FactionManager.GetFeeling unchanged (counter-check existing behavior)
- Player as leader: NPC.PartyLeader=Player → FactionManager.GetFeeling(NPC, Player) returns non-hostile even if faction-rep would normally be hostile
- Bidirectional: FactionManager.GetFeeling(Player, NPC) also non-hostile
- Sibling party members: A and B both have leader L → FactionManager.GetFeeling(A, B) non-hostile (shared root)
- A→B→C chain: A and C share root B → non-hostile
- Counter-check: A and unrelated D → faction-rep determines normally
- Counter-check: dead leader (null reference) → no NRE, returns normal hostility
- Adversarial: A.PartyLeader = A (self-ref attempt blocked by F.1.2; but if somehow set, guard doesn't NRE)

### F.1.5 — `FollowLeaderGoal`
New AI Goal. Pushed onto a party member's goal stack when it should
follow. Uses `MoveToGoal`-like pathfinding to move toward leader's
current cell when distance > threshold; stays put when close.

```csharp
public class FollowLeaderGoal : GoalHandler {
    public Entity Leader;
    public int CloseEnoughDistance = 2;  // Chebyshev
    public int MaxAgeBeforeGiveUp = 20;

    public override bool Finished() { ... }
    public override void TakeAction() { ... }
}
```

Behavior:
- If `Leader == null`, `Leader` destroyed, or leader's `BrainPart.CurrentZone` != ours → Finished() returns true, pops
- If distance ≤ CloseEnoughDistance → wait (don't move)
- If distance > CloseEnoughDistance → move one step toward leader's cell via existing `TryApproachWithPathfinding`
- Cross-zone leader: gracefully give up (set internal flag, Finished returns true)

**TDD test surface** (~8 tests in `FollowerSystemTests.cs` and/or `Goals/FollowLeaderGoalTests.cs`):
- Leader at distance > threshold → goal pushes a step toward leader
- Leader at distance ≤ threshold → goal idles, doesn't move
- Leader null → goal immediately Finished()
- Leader has no BrainPart → goal Finished()
- Leader in different zone → goal Finished()
- Leader moves → next TakeAction targets new cell
- MaxAge → goal Finished even if still distant (defensive)
- Counter-check: actor with no FollowLeaderGoal continues normal AI

### F.1.6 — Cold-eye review (CLAUDE.md §Q1-Q4) + doc backfill + merge

---

## Reusable utilities (don't reinvent)

| Utility | Path | Used for |
|---|---|---|
| `BrainPart.PersonalEnemies` add/remove | `BrainPart.cs:61` | `Forgive` step in SetPartyLeader |
| `BrainPart.Think(string)` | `BrainPart.cs:219` | Cycle-rejection notice + cross-zone give-up notice |
| `FactionManager.GetFeeling` | `FactionManager.cs:166-192` | Hostility-guard injection point |
| `AIHelpers.TryApproachWithPathfinding` | `AIHelpers.cs` | FollowLeaderGoal locomotion |
| `MoveToGoal` template | `Goals/MoveToGoal.cs:1-97` | FollowLeaderGoal shape |
| `PartRoundTripHelper.RoundTripEntityViaTokenGraph` | `Tests/EditMode/TestSupport/PartRoundTripHelper.cs` | Save/load tests |
| `SaveSystem.WriteEntityReference` + token system | `SaveSystem.cs:77-92, 174-187` | Entity-ref serialization (SL.8 pinned identity) |

---

## Self-review pre-flagged findings

These are designed-in tradeoffs to surface BEFORE committing (per
CLAUDE.md §5):

- **🟡 PartyMembers as HashSet<Entity>** — Qud uses a flags-per-member
  map. F.1 simplifies to a flag-less set. If F.2 needs per-member
  flags (e.g. "this follower is independent"), upgrade to
  `Dictionary<Entity, int>`. Migration is purely additive (saves get
  a "flag = 0" default).
- **🟡 Cross-zone follower behavior** — F.1's `FollowLeaderGoal` gives
  up if leader leaves zone. Qud's design supports cross-zone via the
  `LeftBehindByPlayer` flag. Deferred to F.4+. Worth verifying the
  give-up doesn't leave orphan party-member entries; if it does,
  add an OnZoneTransition hook in F.4.
- **🔵 Hostility-guard one-way vs. two-way** — Qud's `Forgive` is
  one-way (follower forgets leader was hostile, but the GUARD in
  `FactionManager.GetFeeling` checks BOTH directions). F.1 mirrors
  this: the Forgive step is one-way (only the recruit forgives), but
  the hostility-decision query is bidirectional (neither attacks the
  other). Confirmed this matches Qud at F.1.4 injection time.
- **⚪ Per-member flags** — out of scope.

---

## Sub-milestone progress

| Sub-milestone | Status | Tests | Commit |
|---|---|---|---|
| F.1.1 Plan + verification sweep | ⏳ | — | — |
| F.1.2 PartyLeader + PartyMembers + SetPartyLeader | ⏳ | — | — |
| F.1.3 Save/load round-trip | ⏳ | — | — |
| F.1.4 Hostility guard | ⏳ | — | — |
| F.1.5 FollowLeaderGoal | ⏳ | — | — |
| F.1.6 Cold-eye + merge | ⏳ | — | — |
| **TOTAL** | **0 / 6** | — | — |

---

*Updated as each sub-milestone ships.*
