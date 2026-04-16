# Qud Parity Tracker

Tracks the goal-stack AI architecture port from Caves of Qud's decompiled source. For each phase, lists: what exists in Qud, what's implemented in Caves of Ooo, and what content is still needed.

**Shorthand:**
- ✅ Implemented and actively used in-game
- ⚠️ Implemented but no production caller yet (needs more content to be useful)
- ⏸️ Deferred — implement when a real caller appears
- ❌ Not implemented

---

## Phase 0 — Goal Stack Foundation

**Status:** ✅ Complete

Core goal stack in `BrainPart._goals` (LIFO list). Every creature has one via Creature blueprint inheritance.

| System | Status |
|--------|--------|
| `GoalHandler` abstract base | ✅ |
| `BrainPart` owns stack, handles TakeTurn | ✅ |
| Child-chain execution loop (pushed child runs in same tick) | ✅ |
| Age tracking on goals | ✅ |
| ParentHandler wiring for FailToParent | ✅ |

**Concrete goals shipped:** BoredGoal, KillGoal, FleeGoal, MoveToGoal, WanderGoal, WanderRandomlyGoal, WaitGoal, StepGoal, GuardGoal, DelegateGoal, CommandGoal.

---

## Phase 1 — A* Pathfinding

**Status:** ✅ Complete

Pool-based A* over 80x25 grid with Chebyshev heuristic. Used by MoveToGoal (with greedy fallback) and TryApproachWithPathfinding (combat chase).

---

## Phase 2 — Brain State + Staying

**Status:** ✅ Complete

`StartingCell`, `Staying` flag, `WhenBoredReturnToOnce` property. All village NPC blueprints opted in (Villager, Elder, Merchant, Tinker, Warden, Farmer, Scribe, WellKeeper, Innkeeper).

**Not yet shipped** (brain flags defined in Qud but unused here):
- `Passive` — won't initiate combat
- `Hibernating` — dormant until triggered
- `Mobile` — explicit immobility flag
- `Calm` / `Aquatic` / `LivesOnWalls` / etc.

---

## Phase 3 — AIBoredEvent

**Status:** ✅ Complete

`AIBoredEvent` fires on bored NPCs. `AIBehaviorPart` abstract base. Two concrete subclasses:
- `AIGuardPart` (Warden) — pushes GuardGoal
- `AIWellVisitorPart` (Farmer, 5%) — walks to village well

---

## Phase 4 — IdleQueryEvent + Furniture

**Status:** ✅ Complete

`IdleQueryEvent`, `DelegateGoal`, `SittingEffect`, `ChairPart`, `BedPart`. Both Chair and Bed blueprints exist. Innkeeper owns her chair (ChairPart.Owner filter).

---

## Phase 5 — Goal Composition Primitives

**Status:** ✅ Lookup subset complete. ⏸️ Insertion methods deferred.

### Qud's API Surface

Qud defines ~17 stack-insertion method overloads on `GoalHandler`:
- `InsertGoalAfter`, `ForceInsertGoalAfter`, `InsertChildGoalAfter` (6 overloads each)
- `InsertGoalAsParent`, `ForceInsertGoalAsParent` (various overloads)
- Plus `Brain.FindGoal`, `HasGoal`, `HasGoalOtherThan`

### What Qud Actually Uses — Verified by Grep

| Qud Method | Real callers | Dead code? |
|------------|:---:|:---:|
| `PushGoal` | 139 | |
| `PushChildGoal` | 82 | |
| `FailToParent` | 112 | |
| `Pop` | many | |
| `HasGoal(string)` | 17 | |
| `Goals.Clear()` (~ClearGoals) | 15 | |
| `Goals.Peek()` (~PeekGoal) | 8 | |
| `FindGoal(string)` | 1 (ModPsionic) | |
| `InsertGoalAsParent` | 1 (same ModPsionic) | |
| `InsertGoalAfter` + all overloads | 0 | Yes |
| `ForceInsertGoalAfter` + overloads | 0 | Yes |
| `InsertChildGoalAfter` + overloads | 0 | Yes |
| `ForceInsertGoalAsParent` + overloads | 0 | Yes |

### Method-by-Method Analysis

#### `PushGoal(GoalHandler)` — ✅ Used
**Qud pattern:** Behavior parts and goal handlers push new goals onto the top of the stack. Most common stack operation in Qud.

**Qud examples:**
- `Bored.cs:240` — `ParentBrain.PushGoal(new MoveTo(cell2))` — return to WhenBoredReturnToOnce cell
- `DropOffStolenGoods.cs:46` — `ParentBrain.PushGoal(new MoveTo(...))` — move stolen loot to drop cell
- `PlaceTurretGoal.cs:85` — turret tinker pushes PlaceTurretGoal
- `GoOnAPilgrimage.cs:64` — pilgrimage pushes MoveTo → MoveToGlobal chain
- `Wishing.cs:2222` — debug command pushes PaxKlanqMadness

**Caves of Ooo usage:**
- `AIGuardPart.HandleBored()` pushes GuardGoal
- `AIWellVisitorPart.HandleBored()` pushes MoveToGoal toward well
- `BoredGoal.TakeAction()` pushes KillGoal, FleeGoal, WanderRandomlyGoal, WaitGoal, DelegateGoal+MoveToGoal

**Content readiness:** ✅ Actively used. No gaps.

---

#### `PushChildGoal(GoalHandler)` — ✅ Used
**Qud pattern:** Goal handlers decompose into sub-tasks by pushing child goals with ParentHandler wired.

**Qud examples:**
- `MoveTo.cs:175` — `PushGoal(new Step(...))` — decompose path into steps
- `Wander.cs` — pushes MoveTo child to reach random cell
- `Kill.cs` — pushes Step children to approach target

**Caves of Ooo usage:**
- `BoredGoal` pushes KillGoal/FleeGoal/WanderRandomlyGoal as children
- `GuardGoal` pushes KillGoal/MoveToGoal
- `MoveToGoal` pushes StepGoal (via A* path follow)
- `WanderGoal` pushes MoveToGoal child

**Content readiness:** ✅ Actively used.

---

#### `Pop()` — ✅ Used
**Qud pattern:** A goal removes itself from the stack when done (alternative to `Finished()` returning true).

**Qud examples:**
- `Command.cs:45` — pops after firing CommandEvent
- `Step.cs` — pops after attempting move
- `FleeLocation.cs` — pops when at target cell

**Caves of Ooo usage:**
- `CommandGoal.TakeAction()` — pops after firing event
- `WanderGoal.TakeAction()` — pops on failure to find cell
- `MoveToGoal` — pops via `Finished()` returning true

**Content readiness:** ✅ Actively used.

---

#### `RemoveGoal(GoalHandler)` — ⚠️ Internal only
**Qud pattern:** Not exposed to gameplay code. Only called internally by Pop() via Goals.Pop() on CleanStack.

**Qud usage:** Zero external callers.

**Caves of Ooo usage:** Called internally by `Pop()` and `ClearGoals()`. No external callers.

**Content readiness:** ✅ Internal infrastructure. Not intended for gameplay use.

---

#### `ClearGoals()` — ⚠️ No production callers yet (content gap)
**Qud pattern:** Wipe the entire goal stack when an NPC's state is catastrophically disrupted — mind control, transformation, teleportation, death handling, quest triggers.

**Qud examples:**
- `Transmutation.cs:233` — when creature is transformed (mutation polymorph)
- `TemporalFugue.cs:309` — temporal duplicate creation
- `Domination.cs:203` — mind control takes effect
- `AIVehiclePilot.cs:89` — vehicle destroyed, pilot's goals invalidated
- `TurretTinker.cs:205` — debug/reset command
- `Vehicle.cs:428` — vehicle damaged severely
- `Wishing.cs:4177` — wish command
- `ITombAnchorSystem.cs:150` — undead reanimation
- `IfThenElseQuestWidget.cs` — quest-triggered NPC state reset

**Caves of Ooo usage:** No production callers.

**What content would enable this:**
| Missing content | Unlocks ClearGoals usage |
|-----------------|--------------------------|
| Polymorph / transformation effects | "Turn snake → human" clears old goals |
| Mind-control mechanic (Phase 9 opinion system) | Mind-control clears victim's goals |
| Quest system with state resets | "Reset NPC behavior" quest actions |
| Debug/admin commands | `/resetai <entity>` |

**Recommendation:** ⏸️ Keep the method; no content to use it yet. No harm in it sitting idle.

---

#### `FailToParent()` — ✅ Used
**Qud pattern:** Child goal discovers it cannot complete; fails back to the parent so parent can try an alternative.

**Qud examples:**
- `MoveToExterior.cs:51` — can't find an exterior cell → fail
- `DropOffStolenGoods.cs:54` — no valid drop location → fail
- `MindroneGoal.cs` — 6+ places where drone can't heal target
- `ClonelingGoal.cs` — 5+ places when clone conditions fail
- `DustAnUrnGoal` — when urn is destroyed mid-journey

**Pattern:** "I'm stuck, parent goal please try something else."

**Caves of Ooo usage:**
- `StepGoal` fails when move is blocked
- `MoveToGoal` fails when A* returns no path AND greedy fails

**Content readiness:** ✅ Pattern established. Could be used more broadly as more complex goals are added (e.g., MoveToZone with no stairs, DustAnUrn when urn is gone).

---

#### `HasGoal<T>()` — ⚠️ Generic variant, tests only
**Qud pattern:** Not in Qud (Qud only has the string variant).

**Qud examples:** N/A

**Caves of Ooo usage:** Tests only. `brain.HasGoal<KillGoal>()` style.

**Content readiness:** ⚠️ Convenience wrapper. Adds readability when used in type-safe contexts. Not blocking anything.

---

#### `HasGoal(string typeName)` — ⚠️ No production callers yet (content gap)
**Qud pattern:** Behavior parts gate on "am I already doing X?" to avoid spawning duplicate goals. This is Qud's most common inspection pattern.

**Qud examples:**
- `TurretTinker.cs:178,182` — only place turret if `!HasGoal("PlaceTurretGoal")`
- `Miner.cs:102` — `!HasGoal("LayMineGoal") && !HasGoal("WanderRandomly") && !HasGoal("Flee")`
- `AIUrnDuster.cs:48` — `if (HasGoal("DustAnUrnGoal"))` return
- `AIShootAndScoot.cs:39` — `if (Target != null && !HasGoal("Flee"))`
- `ModPsionic.cs:62,68` — check if `ChangeEquipment` / `Reequip` already queued
- `AISelfPreservation.cs:23` — `!HasGoal("Retreat")` before initiating retreat
- `Mindrone.cs:23` — skip if `HasGoal("MindroneGoal")`
- `EngulfingWanders.cs:29,35` — coordinate with `FleeLocation`
- `Engulfing.cs:350` — prevent engulf-while-fleeing
- `ForceWall.cs:113` — skip force wall mutation if already fleeing

**Pattern:** "Don't push goal X if I'm already doing it or if something more important is happening."

**Caves of Ooo usage:**
Current `AIGuardPart` and `AIWellVisitorPart` don't need it because they're simple (push-once behaviors where the goal handles its own lifecycle). But multi-step behavior parts WILL need it.

**What content would enable this:**
| Missing content | Unlocks HasGoal(string) usage |
|-----------------|-------------------------------|
| `AIShopper` part | `!HasGoal("GoOnAShoppingSpree")` before pushing one |
| `AIShootAndScoot` part | `!HasGoal("Flee")` before shooting |
| `AILayMine` part (if we add mines) | `!HasGoal("LayMineGoal")` |
| `RetreatGoal` | `AISelfPreservation` checks `!HasGoal("Retreat")` |
| `FleeLocationGoal` | `EngulfingWanders`-style coordination |
| More mutations that push goals | Any mutation that spawns AI behavior |

**Recommendation:** ⚠️ Available for when we add Phase 7 (AIBehaviorPart subclasses) and Phase 6 (more goals). The hook is there; content will fill it.

---

#### `FindGoal<T>()` — ⚠️ Generic variant, tests only
**Qud pattern:** Not in Qud. Ours adds type-safety.

**Caves of Ooo usage:** Tests only. Would become useful for debugging UI or goal introspection.

**Content readiness:** ⚠️ Infrastructure. Minor QoL.

---

#### `FindGoal(string typeName)` — ⚠️ No production callers yet (content gap)
**Qud pattern:** Find a specific goal on the stack to operate on it. Qud uses this exactly once.

**Qud example:**
- `ModPsionic.cs:64` — psionic weapon hits immune target:
  ```
  GoalHandler kill = E.Actor.Brain.FindGoal("Kill");
  if (kill != null) {
      kill.PushChildGoal(new ChangeEquipment(E.Weapon));
      if (!HasGoal("Reequip"))
          kill.InsertGoalAsParent(new Reequip());
  }
  ```

**Caves of Ooo usage:** Tests only.

**What content would enable this:**
| Missing content | Unlocks FindGoal(string) usage |
|-----------------|--------------------------------|
| `ReequipGoal` + `ChangeEquipmentGoal` | Weapon-swap on immunity (the ONE real Qud caller) |
| `AIBrainStateInspector` debug part | "What's the NPC currently doing?" UI |

**Recommendation:** ⏸️ Wait for Phase 14 (Reequip pattern) before this gets a real caller. Available for debug/UI.

---

#### `HasGoalOtherThan(string typeName)` — ⚠️ No production callers yet (needs content)
**Qud pattern:** Passive NPCs only accept new Kill targets if they have no other meaningful goals on the stack.

**Qud example:**
- `Brain.cs:3023`:
  ```
  bool flag = ParentObject.DistanceTo(E.Target) <= MaxKillRadius
      && Target == null && CanFight()
      && (!Passive || !HasGoal());
  ```
  (Note: uses `HasGoal()` no-arg variant here — same pattern, different entry point.)

**Caves of Ooo usage:** Tests only.

**What content would enable this:**
| Missing content | Unlocks HasGoalOtherThan usage |
|-----------------|--------------------------------|
| `BrainPart.Passive` flag | Passive creatures that only fight when idle |
| Passive NPC blueprints (Scribe, Elder with Passive=true) | Real Passive behavior |

**Recommendation:** ⚠️ Needs Phase 2b (additional Brain flags). Then Passive NPCs gate combat acquisition with this.

---

#### `PeekGoal()` — ⚠️ No production callers yet (needs mutations/effects)
**Qud pattern:** Inspect the top goal's type to decide conditional behavior. Used primarily by mutations and status effects that interact with what an NPC is "currently doing."

**Qud examples:**
- `IrisdualBeam.cs:694` — `if (Goals.Peek() is Wait wait)` — treat waiting creatures as affected targets
- `IrisdualBeam.cs:729` — `if (!(Goals.Peek() is FleeLocation))` — don't hit fleeing creatures
- `AIJuker.cs:25` — `Brain.Goals.Peek().TakeAction()` — force top goal to run twice (double-speed juking)
- `Triner.cs:136` — `if (Goals.Peek() is Step)` — trinity effect triggers on stepping
- `BoneWorm.cs:83` — skip behavior unless top is MoveTo or Step
- `AISeekHealingPool.cs:38` — `Brain.Goals.Peek().TakeAction()` — double-action for healing pool approach
- `AISelfPreservation.cs:29` — force top goal TakeAction (instant retreat)
- `DeepDream.cs:117` — `if (!(Goals.Peek() is Dormant))` — dream-state gates on dormant
- `Dominating.cs:78` — same

**Pattern:** "Inspect what the NPC is doing to alter behavior" — mutations, effects, two-action-per-turn tricks.

**Caves of Ooo usage:** Tests only.

**What content would enable this:**
| Missing content | Unlocks PeekGoal usage |
|-----------------|------------------------|
| Speed mutations (extra-action-per-turn) | AIJuker / self-preservation double-tap |
| `DormantGoal` | DeepDream/Dominating effects that check for dormancy |
| `DormantEffect` / sleep mechanics | Effects that alter dormant NPCs differently |
| Targeting spells that filter by NPC state | IrisdualBeam-style conditional effects |

**Recommendation:** ⚠️ Infrastructure exposed; useful for debug (`dump_entity` command showing top goal). Real game usage needs mutations/effects.

---

#### `GoalCount` — ✅ Used internally
**Qud pattern:** Check if stack is empty before peeking. Used in base GoalHandler and in several "is the NPC idle?" checks.

**Caves of Ooo usage:**
- `BrainPart.HandleTakeTurn` — child-chain loop termination
- `BrainPart.PeekGoal` — null guard
- Tests

**Content readiness:** ✅ Used internally by the goal stack itself.

---

### Content Gaps Summary for Phase 5

| Method | Current state | Needs (to be used) |
|--------|---------------|-------------------|
| PushGoal / PushChildGoal / Pop / FailToParent | ✅ Active | Nothing |
| GoalCount / RemoveGoal | ✅ Internal | Nothing |
| ClearGoals | ⚠️ No callers | Polymorph/dominate/quest-reset mechanics |
| HasGoal<T>() / FindGoal<T>() | ⚠️ Tests only | More behavior parts; debug UI |
| HasGoal(string) | ⚠️ Tests only | More AIBehaviorParts (Phase 7); RetreatGoal, LayMineGoal, etc. (Phase 6) |
| FindGoal(string) | ⚠️ Tests only | Reequip pattern (Phase 14) |
| HasGoalOtherThan(string) | ⚠️ Tests only | Brain.Passive flag + passive NPC blueprints |
| PeekGoal | ⚠️ Tests only | Mutations, effects, debug UI |

### What Caves of Ooo Needs Now

The lookup API is **ready for when Phase 6, 7, and 14 content arrives**. No changes needed to the API itself. Instead, each subsequent phase will pull these methods into production use as it ships content:

- **Phase 6** (missing goals — RetreatGoal, LayMineGoal, DormantGoal, etc.) → enables HasGoal(string) gating in behavior parts
- **Phase 7** (AIBehaviorPart subclasses — AIShopper, AIPilgrim, etc.) → heaviest consumer of HasGoal(string)
- **Phase 9** (Opinion system) → Domination mechanic → first real caller of ClearGoals
- **Phase 14** (combat intelligence) → Reequip → first caller of FindGoal(string) + InsertGoalAsParent

### Deferred: Stack-Insertion Methods

`InsertGoalAfter`, `InsertGoalAsParent`, `ForceInsertGoalAfter`, `ForceInsertGoalAsParent`, `InsertChildGoalAfter` — all 17 overloads.

**Rationale:** Grep across Qud's entire codebase shows only ONE real caller (`ModPsionic.InsertGoalAsParent(new Reequip())`). Adding ~200 lines of overloads speculatively with zero callers is noise. When Phase 14 adds the Reequip pattern, we'll add the single method `InsertGoalAsParent(GoalHandler newParent)` (~10 lines) as part of that phase.

---

## Phase 6 — Missing Goal Handlers

**Status:** ❌ Not started

Qud has ~40 goal handlers. Caves of Ooo ships 11. Missing goals (non-exhaustive):

- `RetreatGoal` — structured retreat with waypoints
- `FleeLocationGoal` — flee to a specific cell (vs our FleeGoal which is entity-based)
- `WanderDurationGoal` — wander for N turns (vs one-shot)
- `MoveToZoneGoal` — cross-zone navigation
- `MoveToGlobalGoal` — world-map-scale travel
- `MoveToExteriorGoal` — find outside of building
- `MoveToInteriorGoal` — find inside of building
- `DormantGoal` — hibernate until triggered
- `ReequipGoal` / `ChangeEquipmentGoal` — AI weapon management
- `GoFetchGoal` — walk to object, pick it up, return
- `DisposeOfCorpseGoal` — clean up dead bodies
- `LayMineGoal` — place a mine
- `PlaceTurretGoal` — place a turret
- `PetGoal` — companion pets nearby creatures
- `NoFightGoal` — pacifist override

---

## Phase 7 — Concrete AIBehaviorPart Subclasses

**Status:** 🟡 Partial (2/many)

**Shipped:**
- `AIGuardPart` (Warden)
- `AIWellVisitorPart` (Farmer)

**Missing:**
- `AIShopper` — visit merchant area
- `AIShoreLounging` — aquatic shore behavior
- `AIPilgrim` — journey to holy site
- `AIPatrol` — patrol between zones (TombPatrolBehavior)
- `AISelfPreservation` — retreat on low HP
- `AISitting` — auto-sit on cell entry
- `AIUrnDuster` — specific quest-tied behaviors

---

## Phase 8 — Party / Follower System

**Status:** ❌ Not started

No PartyLeader/PartyMembers fields on Brain. No `CanAIDoIndependentBehavior` event. No follower mechanics.

---

## Phase 9 — Opinion System

**Status:** ❌ Not started (basic PersonalEnemies exists)

Replace `PersonalEnemies` HashSet with full `OpinionMap`. Opinion types: OpinionAttack, OpinionKilledAlly, OpinionFriendlyFire, OpinionThief, etc.

---

## Phase 10 — Debug / Introspection

**Status:** ❌ Not started

- `Brain.Think(string)` — debug thought logging
- `GoalHandler.GetDescription()` / `GetDetails()` — UI-friendly goal descriptions
- Goal stack inspector UI

---

## Phase 11 — TurnTick System

**Status:** ❌ Not started

Part-level ticks independent of TakeTurn. Used by AIUrnDuster and similar "check every N turns" patterns.

---

## Phase 12 — Calendar / World Time

**Status:** ❌ Not started

`Calendar` static class with TurnsPerHour / TurnsPerDay / CurrentDaySegment / IsDay / IsNight. No NPC currently schedule-gates on time of day.

---

## Phase 13 — Zone Lifecycle Integration

**Status:** ❌ Not started

Zone suspend/thaw, elapsed-time catch-up, `GetZoneSuspendabilityEvent`.

---

## Phase 14 — AI Combat Intelligence

**Status:** ❌ Not started

Weapon evaluation (`CompareWeapons`), `PerformReequip`, `WantToKill(entity, reason)`, Reequip/ChangeEquipment goals, `AICommandList` priority system.

---

## Implementation Priority (Recommended)

1. **Tier 4 polish** (small wins in already-shipped systems): SittingEffect visual indicator, tunable scan frequency, force-move auto-cleanup
2. **Phase 7 — more AIBehaviorPart subclasses**: pulls `HasGoal(string)` into production
3. **Phase 12 — Calendar**: unlocks day/night schedules (huge "lived-in" impact)
4. **Phase 6 — missing goals**: gradually add as content demands them
5. **Phase 10 — debug introspection**: low-cost developer QoL
6. **Phase 9 — opinion system**: refines combat/conversation feel
7. **Phase 14 — combat intelligence**: last big feature
