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
| `AILayRune` part (Caves of Ooo uses runes, not mines) | `!HasGoal("LayRuneGoal")` |
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
| HasGoal(string) | ⚠️ Tests only | More AIBehaviorParts (Phase 7); RetreatGoal, LayRuneGoal, etc. (Phase 6) |
| FindGoal(string) | ⚠️ Tests only | Reequip pattern (Phase 14) |
| HasGoalOtherThan(string) | ⚠️ Tests only | Brain.Passive flag + passive NPC blueprints |
| PeekGoal | ⚠️ Tests only | Mutations, effects, debug UI |

### What Caves of Ooo Needs Now

The lookup API is **ready for when Phase 6, 7, and 14 content arrives**. No changes needed to the API itself. Instead, each subsequent phase will pull these methods into production use as it ships content:

- **Phase 6** (missing goals — RetreatGoal, LayRuneGoal, DormantGoal, etc.) → enables HasGoal(string) gating in behavior parts
- **Phase 7** (AIBehaviorPart subclasses — AIShopper, AIPilgrim, etc.) → heaviest consumer of HasGoal(string)
- **Phase 9** (Opinion system) → Domination mechanic → first real caller of ClearGoals
- **Phase 14** (combat intelligence) → Reequip → first caller of FindGoal(string) + InsertGoalAsParent

### Deferred: Stack-Insertion Methods

`InsertGoalAfter`, `InsertGoalAsParent`, `ForceInsertGoalAfter`, `ForceInsertGoalAsParent`, `InsertChildGoalAfter` — all 17 overloads.

**Rationale:** Grep across Qud's entire codebase shows only ONE real caller (`ModPsionic.InsertGoalAsParent(new Reequip())`). Adding ~200 lines of overloads speculatively with zero callers is noise. When Phase 14 adds the Reequip pattern, we'll add the single method `InsertGoalAsParent(GoalHandler newParent)` (~10 lines) as part of that phase.

---

## Phase 6 — Missing Goal Handlers

**Status:** 🟡 In progress — ready subset shipped, infrastructure-blocked goals deferred.

Qud has ~40 goal handlers. Caves of Ooo ships 11 as of Phase 0. This phase audits the remaining ~15 and categorizes each by content-readiness.

### Legend
- 🟢 **Ready** — Can ship today, no new systems needed
- 🟡 **Partial** — One small prerequisite missing (in-scope for the goal)
- 🔴 **Blocked** — Requires a separate content system first
- ⚪ **User-acknowledged** — Runes/turrets (intentionally deferred until content exists)

### Per-Goal Verdict

#### 🟢 Ready — Can ship immediately

| Goal | What's needed | What we have |
|------|--------------|--------------|
| **FleeLocationGoal** | Target cell + pathfinding + StepGoal | `MoveToGoal` already routes to cells. FleeLocation wraps it with "step away from danger, toward safe cell." |
| **WanderDurationGoal** | Tick budget + existing WanderRandomly | `GoalHandler.Age` already tracks ticks. Trivial wrapper with a duration counter. |
| **GoFetchGoal** | Walk → pickup → return | `InventorySystem.Pickup(actor, item, zone)` is AI-friendly (no player-gate). `MoveToGoal` exists. |
| **PetGoal** | Find ally + approach + emit fluff | `FactionManager` provides ally detection. Goal is basically `MoveTo` + particle emit. Pure flavor. |

#### 🟡 Partial — Small gap, single-PR fix

| Goal | What's needed | What was missing |
|------|--------------|------------------|
| **RetreatGoal** | Safe waypoint to retreat to | `BrainPart.StartingCellX/Y` already exists → used as retreat point. Added `AISelfPreservationPart` (HP-threshold trigger). |
| **DormantGoal** | Wake trigger | Goal itself is trivial (don't pop, do nothing). Wakes on: damage taken, hostile entity in sight radius, or explicit `WakeUp` event. Can be pushed by BedPart or as initial AI state for ambush creatures. |
| **NoFightGoal** | `BrainPart.Passive` flag | Added. `KillGoal` acquisition now gates on `!Passive || !HasGoal()` semantics via `HasGoalOtherThan("BoredGoal")`. `NoFightGoal` is a hard override on top of the flag. |

#### 🔴 Blocked — Needs a separate content system

| Goal | Dominant blocker |
|------|------------------|
| **ReequipGoal / ChangeEquipmentGoal** | 1. **No weapon scoring** — `MeleeWeaponPart` exposes raw stats; no `CompareWeapons` method exists. 2. **AutoEquipCommand refuses to displace** (AutoEquipCommand.cs:100-105) — NPCs can't swap a held weapon. 3. **No damage-type / resistance / immunity system** — the Reequip trigger in Qud fires when a weapon is ineffective vs. immunities; we have zero such infrastructure. **Prerequisite: Phase 14 (combat intelligence).** |
| **DisposeOfCorpseGoal** | **No corpse entities exist.** `CombatSystem.HandleDeath` drops items onto the cell, then `zone.RemoveEntity(target)` — the dead entity is gone. `ItemCategory.Corpses` is a stub with no producer. **Prerequisite: Corpse entity system** (spawn corpse blueprint on death, retain for N turns). |
| **MoveToZoneGoal** | 1. **Player-only zone transitions** — `ZoneTransitionSystem.TransitionPlayer` is invoked only from `InputHandler` keypresses; stairs have no `OnStep` hook. 2. **No `CurrentZoneID` on entities** — zone membership is implicit. 3. **No zone-graph pathing** — `WorldMap.GetAdjacentZoneID` + `ZoneManager.ZoneConnection` provide data but nothing searches it. **Prerequisite: Phase 13 (zone lifecycle) with NPC-capable transitions.** |
| **MoveToGlobalGoal** | Same blockers as MoveToZoneGoal + world-scale BFS/A*. **Prerequisite: Phase 13 + global path solver.** |
| **MoveToExteriorGoal** | **Zero interior/exterior tagging.** Only trace is a code comment in `VillageBuilder.cs:165`. **Prerequisite: cell-level indoors/outdoors tagging.** |
| **MoveToInteriorGoal** | Same as above. |

#### ⚪ User-acknowledged (deferred until content ships)

| Goal | Status |
|------|--------|
| **LayRuneGoal** (Caves of Ooo's analogue to Qud's LayMineGoal) | No rune system yet. Needs: Rune blueprint/part, step-on-trigger mechanism, rune inventory on rune-laying NPCs. |
| **PlaceTurretGoal** | No turret system yet. Needs: Turret blueprint with auto-fire, tinker materials, placement mechanics. |

### Cross-Cutting Infrastructure Gaps

Three gaps surfaced during the audit that block multiple Phase 6 goals:

**Gap A — NPC-capable zone transition** (blocks `MoveToZoneGoal`, `MoveToGlobalGoal`)
- `Entity.CurrentZoneID` property
- `ZoneTransitionSystem.TransitionNPC(entity, fromZone, toZone)` variant that doesn't assume player camera/input
- `StairsDownPart.OnStep` hook for non-player entities
- Zone-graph path solver over `WorldMap.GetAdjacentZoneID` + `ZoneManager.ZoneConnection`
- Live-simulate traversed zones OR teleport-with-catch-up (Qud uses catch-up)

**Gap B — Interior/Exterior cell tagging** (blocks `MoveToExteriorGoal`, `MoveToInteriorGoal`)
- Per-cell `IsInterior` flag set at generation time, or a `BuildingPart` on rooms, or a `RoofPart` on covered cells
- Easiest approach: `VillageBuilder` already knows when it's painting interior floors — tag the cell then.
- Bonus: unlocks weather effects, "indoor safe from rain," bed-only-indoors preferences.

**Gap C — Corpse entity system** (blocks `DisposeOfCorpseGoal`)
- `Corpse` blueprint in `Objects.json` with `ItemCategory = "Corpses"` tag
- `CombatSystem.HandleDeath` spawns `Corpse(entityBlueprintName)` at death cell
- Decay timer (removes corpse after N turns)
- Bonus: unlocks necromancy, undead reanimation, butchering for food.

**Gap D — Damage types + resistances + weapon scoring** (blocks `ReequipGoal`, `ChangeEquipmentGoal`)
- `DamageType` enum matching the mutation catalog (Bludgeon/Pierce/Slash/Fire/Cold/Acid/Mental/Electric)
- `ResistancesPart` on entities (per-type resistance values)
- `MeleeWeaponPart.DamageType` field + tagging existing weapons
- `CompareWeapons(actor, a, b)` scoring method
- `AutoEquipCommand` displacement path OR new `SwapEquipCommand`
- This is really Phase 14 foundation work.

### Effort-to-Impact Ordering

Cheapest → most expensive: **Gap B (interior/exterior) → Gap C (corpses) → Gap A (cross-zone AI) → Gap D (damage types)**.

### Summary Matrix

| Goal | Status | Blocker |
|------|:------:|---------|
| FleeLocationGoal | 🟢 Shipped | — |
| WanderDurationGoal | 🟢 Shipped | — |
| GoFetchGoal | 🟢 Shipped | — |
| PetGoal | 🟢 Shipped | — |
| RetreatGoal + AISelfPreservationPart | 🟡 Shipped | — |
| NoFightGoal + `BrainPart.Passive` | 🟡 Shipped | — |
| DormantGoal + wake triggers | 🟡 Shipped | — |
| LayRuneGoal | ⚪ | Rune system |
| PlaceTurretGoal | ⚪ | Turret system |
| DisposeOfCorpseGoal | 🔴 | **Gap C** |
| ReequipGoal / ChangeEquipmentGoal | 🔴 | **Gap D (Phase 14)** |
| MoveToZoneGoal | 🔴 | **Gap A (Phase 13)** |
| MoveToGlobalGoal | 🔴 | **Gap A + global solver** |
| MoveToExteriorGoal | 🔴 | **Gap B** |
| MoveToInteriorGoal | 🔴 | **Gap B** |

### Known Style / Polish Items (Non-Blocking)

These surfaced during Phase 6 code review. All are cosmetic or structural concerns
that don't affect correctness and don't justify separate commits. Captured here so
they surface naturally when the files are next touched (or when patterns are
applied elsewhere).

1. **RetreatGoal shares its `MaxTurns` budget with the inner `MoveToGoal`.**
   `RetreatGoal(maxTurns: 200)` passes the same 200 to its child `MoveToGoal`, so
   a 195-tick journey leaves only 5 ticks for recovery before the whole goal
   expires. Consider splitting into a dedicated `TravelMaxTurns` (or halving the
   value for the child) if recovery time matters for balance.

2. **FleeLocationGoal relies on lockstep `Age` expiration with its `MoveToGoal` child.**
   The parent's `Finished()` check (`Age > MaxTurns`) and the child's own check
   expire on the same tick by accident of sharing the same `MaxTurns`. If
   `MoveToGoal`'s pop semantics ever change, FleeLocationGoal could regress to
   the same infinite-re-push bug that was fixed in PetGoal / GoFetchGoal via
   attempt counters. Either add a pinning test for the timeout case, or adopt
   the attempt-counter pattern here too.

3. **`WanderDurationGoal._ticksTaken` increments even when child movement fails.**
   Intentional — `Duration` is "turns elapsed," not "successful steps." The XML
   doc could be clearer about this. A cornered NPC still counts down its duration.

4. **Passive-flag `Target` semantics change.**
   Before the Phase 6 patch, `BoredGoal` set `Brain.Target = hostile` on any
   sighted hostile. After, a Passive NPC that ignores a hostile leaves `Target`
   null. This is intentional (Target == "creature I'm acting on") but is a
   behavior change that future consumers should know about. No current code
   depends on the old semantics.

5. **`PetGoal` had redundant `Phase.PetAndFinish` transitions.**
   The enum value was set and immediately overwritten with `Phase.Done`.
   Cleaned up in the infinite-loop fix; the enum now only contains states that
   `TakeAction`'s `switch` actually dispatches on.

6. **`GoFetchGoal.WalkToItem` previously set `_phase = Pickup` in both branches.**
   Duplicate assignment; hoisted to a single line above the same-cell check in
   the infinite-loop fix.

### Full Implementation Plan — from "defined" to "visibly used in play"

Phase 6 goals ship in two states:
1. **API-complete** — type, constructor, tests all green (where we are today).
2. **Gameplay-live** — at least one blueprint or player-accessible system invokes the goal during normal play.

This section describes what it takes to move every goal from state 1 → state 2, with concrete in-game behavior examples so the visible payoff is clear.

#### Tier A — Zero-infrastructure wiring (1–2 days total)

##### A1. `RetreatGoal` + `AISelfPreservationPart`

**What you see in game:**
- Warden's HP drops below 30% → breaks from combat → retreats to guard post → regenerates → re-engages if you pursue.
- Innkeeper at 70% HP → abandons counter → retreats to private quarters → won't emerge until full HP.
- Farmer at 40% HP → abandons well trip → returns home.

**Work:** Add `AISelfPreservation` entries to Warden, Innkeeper, Farmer, Tinker, Scribe blueprints. Tune thresholds per NPC role. Blueprint JSON only; no new code.

##### A2. `BrainPart.Passive` flag

**What you see in game:**
- Scribe keeps writing when snapjaws walk into his study; only defends if attacked first.
- Elder ignores combat entirely unless directly aggro'd.
- WellKeeper chats with you peacefully even as raiders loom.

**Work:** Add `Passive=true` to Scribe, Elder, WellKeeper, Innkeeper blueprints. Verify `BlueprintLoader` sets the field (one-line test if missing).

##### A3. `DormantGoal` + new `AIAmbushPart`

**What you see in game:**
- Sleeping Troll in a cave emits `z` particles; enter sight radius → `!` alert → troll wakes and pushes `KillGoal`.
- Mimic Chest disguised as treasure; attack or open it → wakes and bites.
- Ambushing bandits in tall grass — first footstep in LOS wakes them all.

**Work:**
1. Create `AIAmbushPart` that pushes `DormantGoal` on first TakeTurn.
2. New blueprints: `SleepingTroll`, `MimicChest`, `Ambush_Bandit`.
3. Populate 1–2 dungeon zones during generation.

#### Tier B — Small gameplay systems (~1 week total)

##### B1. `NoFightGoal`

**What you see in game:**
- Persuasion dialogue branch (Charisma check) pushes `NoFightGoal(100)` on a hostile NPC — they sheathe weapons and wander passively.
- `Calm` mutation (new) pacifies one target for 50 turns.
- Two NPCs in dialogue cannot attack each other or you until conversation ends.
- Quest-driven truce: "Broker peace between Villagers and Rustling Camp" — all faction members pacified indefinitely.

**Work:**
1. New `ConversationAction.PushNoFightGoal(duration)` for dialogue trees.
2. New `CalmMutation` applying `NoFightGoal` via `MutationsPart`.
3. `ConversationManager` auto-pushes/pops NoFight on participants during dialogue.
4. Attach persuasion branch to at least one existing hostile NPC as proof.

##### B2. `WanderDurationGoal`

**What you see in game:**
- `Witnessed` status effect applied to nearby peaceful NPCs after a violent death — they pace for 20 turns looking rattled.
- "Come back later" dialogue branch pushes `WanderDurationGoal(30)`.
- `AIFidgetPart` on nervous NPCs — 5% per bored tick pushes short WanderDuration.

**Work:**
1. `AIFidgetPart` (ambient flavor).
2. `WitnessedEffect` that pushes WanderDurationGoal.
3. `CombatSystem.HandleDeath` broadcasts witness event to nearby Passive NPCs.
4. Dialogue action for scripted usage.

##### B3. `PetGoal` + `AIPetterPart`

**What you see in game:**
- Village children periodically walk to Innkeeper and emit `*` magenta particle (affection).
- Companion pet dog nuzzles player when bored.
- Allied NPCs greet each other when adjacent.

**Work:**
1. `AIPetterPart` — probabilistic push of PetGoal on AIBored (gated with `!HasGoal("PetGoal")`).
2. Child NPC blueprint with AIPetterPart.
3. Spawn children near Innkeeper during village generation.

##### B4. `GoFetchGoal`

**What you see in game:**
- Throw a bone near your pet dog → dog fetches it → returns to your feet.
- Drop a hammer in front of Tinker → Tinker picks it up, carries to workbench.
- Magpie creature in zones periodically scans for `Shiny` items and grabs them to its den.

**Work:**
1. `AIHoarderPart` — scans zone for tagged items, pushes GoFetchGoal.
2. Modify `ThrowItemCommand` to fire `ItemLanded` event on nearby allies.
3. `AIRetrieverPart` listens for ItemLanded, pushes GoFetchGoal if owner-thrown.
4. Add `Shiny` tag to gold/gems. Create Magpie creature.

##### B5. `FleeLocationGoal`

**What you see in game:**
- Scribe at low HP flees to the Shrine (specific cell) instead of randomly.
- Refugee quest event: survivors flee to a designated evacuation point.
- New `Panic(targetCell)` spell — target flees TO a cell, not AWAY from caster.

**Work:**
1. `SanctuaryPart` marker (with optional heal-over-time aura).
2. `AIFleeToShrinePart` — pushes FleeLocationGoal to nearest sanctuary when HP low.
3. Shrine blueprint + placement in village.
4. Attach AIFleeToShrinePart to Scribe, Elder, priestly NPCs.

#### Tier C — Medium infrastructure (2–4 days each)

##### C1. Interior/Exterior tagging (Gap B) → `MoveToInteriorGoal`, `MoveToExteriorGoal`

**What you see in game:**
- Weather system drops rain → all outdoor NPCs push MoveToInterior → streets empty.
- At dawn (Phase 12 Calendar) → NPCs push MoveToExterior and go to work.
- Fire erupts in a building → occupants push MoveToExterior, flee through doors.
- Poison cloud indoors → NPCs evacuate.

**Work:**
1. Add `Cell.IsInterior` bool.
2. Modify `VillageBuilder.PaintInteriorFloors` to tag cells.
3. Implement MoveToInterior/ExteriorGoal as BFS for nearest matching cell + MoveToGoal child.

**Collateral unlock:** foundation for weather effects, indoor/outdoor combat rules, sheltering behaviors.

##### C2. Corpse entity system (Gap C) → `DisposeOfCorpseGoal`

**What you see in game:**
- Killed creatures leave `%` corpse sprites for ~50 turns before decaying.
- Undertaker NPC walks to corpses, carries them to graveyard — busy after a raid.
- Vulture creature eats corpses (primary idle behavior).
- Foundation for necromancy (raise corpse) and butchering (yield raw meat).

**Work:**
1. `Corpse` blueprint in Objects.json + `CorpsePart` storing source blueprint.
2. `CorpseDecayPart` — per-tick countdown.
3. Modify `CombatSystem.HandleDeath` to spawn corpse at death cell.
4. Implement `DisposeOfCorpseGoal`: walk to nearest corpse → pick up → dispose.
5. Create `Undertaker` NPC + `Vulture` creature blueprints.

**Collateral unlock:** necromancy, butchering for food, corpse-based quest hooks.

##### C3. Rune system → `LayRuneGoal`

**What you see in game:**
- Rune Cultist retreats 2 steps, places `‡ Rune of Flame` on cell, retreats further. Step on rune → fire damage.
- Boss fight: arena peppered with delayed-trigger runes during Phase 1.
- Druid lays protection runes around sacred grove before combat.

**Work:**
1. `RunePart` tagged item + `TriggerOnStepPart` that fires on cell-entered event.
2. Rune blueprints: `RuneOfFlame`, `RuneOfFrost`, `RuneOfPoison`.
3. Implement `LayRuneGoal` (pull rune from inventory, drop at current cell).
4. `AILayRunePart` behavior part mirroring Qud's `Miner.cs` pattern.
5. Create `RuneCultist` creature with rune inventory + AILayRunePart.

##### C4. Turret system → `PlaceTurretGoal`

**What you see in game:**
- TurretTinker NPC deploys small autonomous turret during bandit raid; turret stays put shooting hostiles.
- Bandit-engineer deploys turret then retreats behind cover.
- Player's future companion engineer can place turrets for battlefield control.

**Work:**
1. `TurretPart` — auto-fires at nearest hostile on its own TakeTurn.
2. Turret blueprint (solid, non-movable, limited ammo).
3. `TurretKit` item blueprint (Takeable, becomes turret on drop).
4. Implement `PlaceTurretGoal`.
5. `AITurretTinkerPart` with HP/proximity deploy logic.
6. Create `TurretTinker` creature blueprint.

#### Tier D — Large infrastructure (multi-week phases)

##### D1. Gap A: NPC-capable zone transitions → `MoveToZoneGoal`, `MoveToGlobalGoal`

**What you see in game:**
- Warden patrols between village and adjacent cave every 50 turns — when you return from a dungeon, the Warden may not be in the village.
- Trade caravan merchants travel between villages on a schedule.
- Wanted bandit boss migrates between zones; player hunts them across the world map.
- Refugees visibly walk across the world map from besieged village to safe village.

**Work (Phase 13 scope):**
1. Add `Entity.CurrentZoneID` tracking.
2. Create `ZoneTransitionSystem.TransitionNPC(entity, fromZone, toZone, cell)` API.
3. Hook `StairsDownPart` / `StairsUpPart` `OnEntityEnter` for non-player entities.
4. Build zone graph from `WorldMap.GetAdjacentZoneID` + `ZoneManager.ZoneConnection`.
5. Implement `MoveToZoneGoal`: path via zone graph, push MoveToGoal to each stair, transition on arrival.
6. Choose live-simulate vs teleport-with-catch-up strategy.
7. `MoveToGlobalGoal` = chained MoveToZoneGoal across world-map A*.

##### D2. Gap D: Damage types + resistances + weapon scoring → `ReequipGoal`, `ChangeEquipmentGoal`

**What you see in game:**
- Snapjaw with sword attacks you in plate armor (Slash immunity). After 3 turns of no damage → unsheathes mace from pack and swaps (Bludgeon damage).
- Ice elemental immune to cold → NPC ally swaps to fire torch.
- Companion ally learns boss's weakness and exploits it mid-fight.
- Weapon variety becomes tactically meaningful.

**Work (Phase 14 scope):**
1. `DamageType` enum (Bludgeon/Pierce/Slash/Fire/Cold/Acid/Mental/Electric).
2. `MeleeWeaponPart.DamageType` field + tag existing weapons.
3. `ResistancesPart` on creatures (per-type resistance %).
4. Modify `CombatSystem.ApplyDamage` to apply resistance.
5. `WeaponEvaluator.CompareWeapons(actor, a, b, target)` scoring.
6. Implement `ChangeEquipmentGoal` (unequip current, equip new).
7. Implement `ReequipGoal` (find best weapon, push ChangeEquipment).
8. Integrate trigger in `KillGoal` (after N ineffective attacks, inject ReequipGoal).

#### Final NPC Roster After Full Phase 6

| Blueprint | Phase 6 parts/flags |
|-----------|---------------------|
| Warden | `AISelfPreservation(0.3/0.7)`, optional ambush at night |
| Farmer | `AISelfPreservation(0.4)`, existing `AIWellVisitor` |
| Innkeeper | `AISelfPreservation(0.7)`, target of children's `PetGoal` |
| Scribe | `Passive=true`, `AISelfPreservation(0.8)`, `AIFleeToShrinePart` |
| Elder, WellKeeper | `Passive=true` |
| Tinker | `AISelfPreservation(0.5)`, `AIHoarderPart` |
| Merchant | `AIShopper` (Phase 7) + `MoveToGlobalGoal` |
| *(NEW)* Village Children | `AIPetterPart` |
| *(NEW)* Pet Dog | `GoFetchGoal` + `PetGoal` |
| *(NEW)* Sleeping Troll, Mimic, Ambush_Bandit | `AIAmbushPart` → `DormantGoal` |
| *(NEW)* Rune Cultist | `AILayRunePart` → `LayRuneGoal` |
| *(NEW)* TurretTinker | `AITurretTinkerPart` → `PlaceTurretGoal` |
| *(NEW)* Undertaker, Vulture | `DisposeOfCorpseGoal` |
| *(NEW)* Shrine | `SanctuaryPart` marker |
| Snapjaw Warrior | `ReequipGoal` via `KillGoal` integration |

#### Execution Milestones

| Milestone | Status | Tier | Effort | What goes live |
|-----------|:------:|:----:|:------:|----------------|
| **M1** — Blueprint wiring | ✅ Done (1317/1317) | A | 1–2d | RetreatGoal, Passive, DormantGoal |
| **M2** — Dialogue/status triggers | ⏳ Next | B | 2–3d | NoFightGoal, WanderDurationGoal |
| **M3** — Ambient behavior parts | | B | 2–3d | PetGoal, GoFetchGoal, FleeLocationGoal |
| **M4** — Interior/Exterior (Gap B) | | C | 3–4d | MoveToInterior/ExteriorGoal, weather foundation |
| **M5** — Corpse system (Gap C) | | C | 3–5d | DisposeOfCorpseGoal, necromancy/butcher foundation |
| **M6** — Rune system | | C | 3–4d | LayRuneGoal |
| **M7** — Turret system | | C | 3–4d | PlaceTurretGoal |
| **M8** — Gap A (zone transitions) | | D | 1–2w | MoveToZone/GlobalGoal, Phase 13 foundation |
| **M9** — Gap D (damage types) | | D | 2–3w | ReequipGoal/ChangeEquipmentGoal, Phase 14 foundation |

**Recommended sequencing:** M1 → M2 → M3 → (M4 ∥ M5) → M6 → M7 → (M8 ∥ M9).

**Phase 6 is "done"** when all 14 goals are used by at least one blueprint or player-accessible system, every tier's tests are green, and a blind player walking through the game would encounter ≥ 8 of the 14 goals in a typical play session.

### Detailed Plans: M1, M2, M3

#### Milestone M1 — Blueprint wiring (Tier A, 1–2 days)

Goal: after M1, three Phase 6 additions are visibly active during play:
`RetreatGoal` (via `AISelfPreservationPart`), `BrainPart.Passive`, and
`DormantGoal` (via new `AIAmbushPart`). Prerequisites: Phase 6 API (shipped).

##### M1.1 — Wire `AISelfPreservationPart` into NPC blueprints

**Files to modify:** `Assets/Resources/Content/Blueprints/Objects.json`

**Tuning table:**

| NPC | RetreatThreshold | SafeThreshold | Rationale |
|-----|:----------------:|:-------------:|-----------|
| Warden | 0.3 | 0.7 | Die-hard guard |
| Tinker | 0.5 | 0.75 | Moderate combatant |
| Farmer | 0.4 | 0.75 | Minor combatant |
| Innkeeper | 0.7 | 0.9 | Non-combatant |
| Scribe | 0.8 | 0.95 | Extreme non-combatant |

**JSON per NPC (append to the `Parts` array):**
```json
{ "Name": "AISelfPreservation", "Params": [
    { "Key": "RetreatThreshold", "Value": "0.3" },
    { "Key": "SafeThreshold", "Value": "0.7" }
]}
```

The reflection-based loader (EntityFactory.cs:255–296) auto-sets the public
float fields. Part-name lookup accepts `"AISelfPreservation"` (class name
without `Part` suffix) just like existing `AIGuard` / `AIWellVisitor`.

**Tests** (new `AISelfPreservationBlueprintTests.cs`):
- `Warden_HasAISelfPreservation_LoadedFromBlueprint` — asserts thresholds parse correctly
- `Innkeeper_AISelfPreservation_TriggersRetreatAtLowHp` — end-to-end integration

**Acceptance:** 5 blueprints parse cleanly, tests pass, playtest confirms
Warden breaks combat at 30% HP and retreats to her post.

##### M1.2 — Wire `BrainPart.Passive` on non-combat NPCs

**Files to modify:** `Assets/Resources/Content/Blueprints/Objects.json`

**Targets:** Scribe, Elder, WellKeeper, Innkeeper.

**JSON modification (extend existing Brain part Params):**
```json
{ "Name": "Brain", "Params": [
    { "Key": "SightRadius", "Value": "8" },
    { "Key": "Wanders", "Value": "false" },
    { "Key": "WandersRandomly", "Value": "false" },
    { "Key": "Staying", "Value": "true" },
    { "Key": "Passive", "Value": "true" }    // NEW
]}
```

`BrainPart.Passive` is already a public field; the loader sets it via reflection. No code changes.

**Tests:**
- `Scribe_IsPassive_FromBlueprint`
- `Scribe_DoesNotInitiateCombat_InProximityToHostile`
- (Existing Phase6GoalsTests already cover flee-when-low-HP and fight-back-on-personal-hostility)

**Acceptance:** Passive NPCs ignore hostile sight; still flee at low HP; still retaliate when attacked.

##### M1.3 — Create `AIAmbushPart` + dormant-creature blueprints

**Files to create:**
- `Assets/Scripts/Gameplay/AI/AIAmbushPart.cs`
- `Assets/Tests/EditMode/Gameplay/AI/AIAmbushPartTests.cs`

**Files to modify:**
- `Assets/Resources/Content/Blueprints/Objects.json` (add 3 blueprints)
- `Assets/Scripts/Gameplay/World/Generation/Builders/LairPopulationBuilder.cs`

**`AIAmbushPart` shape:**
```csharp
public class AIAmbushPart : AIBehaviorPart {
    public override string Name => "AIAmbush";
    public bool WakeOnDamage = true;
    public bool WakeOnHostileInSight = true;
    public int SleepParticleInterval = 8;
    private bool _dormantPushed;

    public override bool HandleEvent(GameEvent e) {
        if (!_dormantPushed && e.ID == "TakeTurn") {
            var brain = ParentEntity.GetPart<BrainPart>();
            if (brain != null) {
                brain.PushGoal(new DormantGoal(
                    wakeOnDamage: WakeOnDamage,
                    wakeOnHostileInSight: WakeOnHostileInSight,
                    sleepParticleInterval: SleepParticleInterval));
                _dormantPushed = true;
            }
        }
        return true;
    }
}
```

**Pattern rationale:** use `TakeTurn` with a `_dormantPushed` flag so this is
robust to blueprint part-ordering. By the time `TakeTurn` fires, all parts
(including BrainPart) exist on the entity.

**Blueprints:** `SleepingTroll`, `MimicChest`, `AmbushBandit` (see the
implementation plan output for full JSON bodies).

**Tests:**
- `AIAmbush_PushesDormantGoalOnFirstTakeTurn`
- `AIAmbush_OnlyPushesOnce_AcrossMultipleTurns`
- `SleepingTroll_Blueprint_LoadsCorrectly`
- `MimicChest_WakesOnDamageOnly_NotOnSight`

**Integration:** `LairPopulationBuilder` spawns ambushers — e.g., 30% chance of SleepingTroll per lair zone, 0–2 MimicChests in room corners.

**Acceptance:** Step into lair → visible `z` particles on sleeping troll → entering sight radius triggers `!` particle → combat starts.

##### M1 Verification checklist
- [x] 5 NPCs have AISelfPreservation; 4 NPCs have Passive=true
- [x] AIAmbushPart.cs created and compiles
- [x] 3 new blueprints (SleepingTroll, MimicChest, AmbushBandit) load without errors
- [x] Lair generation spawns dormant creatures
- [x] All M1 tests green; full EditMode suite still passes (1301/1301 tests, was 1275 before M1)

##### M1 Status: ✅ Complete — all review findings addressed and verified, subject to in-game validation

Initial M1 implementation: 1301/1301 EditMode tests passing (MCP-verified).
Post-review fix pass: all 14 findings (1 🔴 + 3 🟡 + 5 🔵 + 4 🧪 + 1 ⚪) addressed.
Post-fix test run: **1317/1317 EditMode tests passing** (MCP-verified, 12.5s).

Net new tests from the fix pass: 16 (covering Initialize-based ambush push,
stack-contents regression, Rearm behavior, fallback ordering, MimicChest
same-cell wake, WellKeeper/Elder AISelfPreservation, Warden-no-retreat-in-combat
vs Warden-retreats-after, RetreatGoal heal-to-safe + clamp-to-max + MaxTurns
fallback, and 6 statistical LairPopulationBuilderAmbushTests).

**In-game playtest still deferred** — behaviors should be visually verified during
live play per Option A below.

##### In-Game Verification (Option A) — TODO

When available, run an MCP-assisted in-editor verification pass to visually confirm
M1 behaviors. Plan:

1. Enter Play mode in Unity Editor.
2. Use `mcp__unity__execute_code` to spawn controlled scenarios adjacent to the player:
   - **Passive check:** spawn a Scribe + Snapjaw side-by-side; verify no KillGoal, no red `!` particle, Scribe stays put.
   - **AISelfPreservation check:** spawn a Warden, break LOS, drop her HP to 20%, advance several turns; verify she walks to her starting cell and idles.
   - **Ambush check:** spawn a SleepingTroll 10 cells from the player; verify `z` particle appears every 8 turns; walk within 8 cells; verify yellow `!` wake particle and immediate aggression.
   - **Mimic check:** spawn a MimicChest visible but not adjacent; verify NO wake; attack it; verify wake + combat.
3. Capture console logs and (optionally) screenshots via `mcp__unity__read_console`.
4. Update the status below to "✅ Complete — verified in-game" once all four scenarios pass.

This approach takes ~5 minutes of tool calls and produces confidence equivalent to a
dedicated manual QA playthrough without requiring world exploration to reach lairs
or aggro targets.

##### M1 Code Review — Findings

Post-implementation review of M1 files:
- `Assets/Scripts/Gameplay/AI/AIAmbushPart.cs` (new)
- `Assets/Scripts/Gameplay/World/Generation/Builders/LairPopulationBuilder.cs` (modified)
- `Assets/Resources/Content/Blueprints/Objects.json` (3 NPC edits for AISelfPreservation, 4 for Passive, 3 new ambush blueprints)
- `Assets/Tests/EditMode/Gameplay/AI/AISelfPreservationBlueprintTests.cs` (new, 17 tests)
- `Assets/Tests/EditMode/Gameplay/AI/AIAmbushPartTests.cs` (new, 9 tests)

Severity legend: 🔴 critical, 🟡 moderate, 🔵 minor/polish, 🧪 test gap.

###### 🔴 Bug 1 — Turn-1 ordering: `AIAmbushPart` pushes `DormantGoal` AFTER `BrainPart` handles `TakeTurn`

**File:** `AIAmbushPart.cs:48-63`

Blueprint part order is determined by `BlueprintLoader.Bake` (BlueprintLoader.cs:136-155):
parent-blueprint parts first, then child-blueprint *new* parts appended. `Brain` lives in
the `Creature` parent blueprint, so it occupies index 5 in the merged part dictionary.
`AIAmbush` is a new child-blueprint part, appended later (index 10+).

`Entity.FireEvent` dispatches `HandleEvent` to every part in `Parts.Add` order
(Entity.cs:257-263). Result: on the **first** `TakeTurn`, `BrainPart.HandleTakeTurn`
runs FIRST — it pushes `BoredGoal` onto an empty stack, executes `BoredGoal.TakeAction`
(which scans for hostiles and may push `KillGoal`/`FleeGoal`/`WanderRandomlyGoal`),
runs the child-chain loop, and returns. **Then** `AIAmbushPart.HandleEvent` runs and
pushes `DormantGoal` on top.

Net effect: the ambush creature is briefly awake on turn 1 before "falling asleep" on
turn 2. For SleepingTroll/MimicChest (both `Staying=true`, `StartingCell` just set),
`BoredGoal` would likely idle in place — no visible movement. But `AmbushBandit`
(no `Staying` flag) could take a random step or chase the player before the ambush
takes effect. And any child-chain side effects (particle emission on first aggro,
`Target` assignment, etc.) fire prematurely.

**Why tests don't catch this:** `SleepingTroll_FromBlueprint_PushesDormantOnFirstTurn`
only asserts that `DormantGoal` is on the stack after `TakeTurn` — it doesn't assert
that the stack is exactly `[DormantGoal]`. A stack of `[BoredGoal, DormantGoal]` also
passes the test.

**Proposed fix:** move the push from `HandleEvent` to `Part.Initialize()`:
```csharp
public override void Initialize()
{
    var brain = ParentEntity?.GetPart<BrainPart>();
    if (brain != null)
    {
        brain.PushGoal(new DormantGoal(WakeOnDamage, WakeOnHostileInSight, SleepParticleInterval));
    }
}
```
Initialize runs inside `Entity.AddPart` (Entity.cs) right after the part is attached,
well before any `TakeTurn` fires. Part add order follows the same dictionary iteration,
so `BrainPart` is attached before `AIAmbushPart` → `ParentEntity.GetPart<BrainPart>()`
returns the live instance. `PushGoal` doesn't need `CurrentZone`, so pre-placement is fine.

Requires tests update: replace `AIAmbush_PushesDormantGoalOnFirstTakeTurn` with
`AIAmbush_PushesDormantGoalAtConstructionTime`, and add a "stack has ONLY DormantGoal
after turn 1" regression test.

###### 🟡 Bug 2 — `RetreatGoal.Recover` has no passive HP regeneration source

**Impact:** AISelfPreservation pushes RetreatGoal, which transitions to `Phase.Recover`
at the waypoint. `RecoverAtWaypoint()` polls `ParentEntity.GetStat("Hitpoints")` and
only finishes when `hp / maxHp >= SafeHpFraction`. But nothing regenerates an NPC's HP
unless they have `RegenerationMutation` — and none of the NPCs in M1.1 blueprints do.

Result: a wounded Warden retreats to her post, then sits idle for exactly `MaxTurns=200`
turns (the safety cap in `RetreatGoal.Finished`), then pops and goes back to idling at
low HP. The next AIBored tick pushes RetreatGoal again. She's effectively stuck at low
HP, cycling retreat → timeout → retreat → timeout indefinitely.

**Proposed fix (choose one):**
- Add `RegenerationMutation(level: 1)` to the village NPCs we attached AISelfPreservation
  to. Heals 1 HP per turn. Simple, scoped fix.
- Create a passive `VitalRegenPart` on Creature base that heals a small fraction per N
  turns. Broader fix; unlocks natural recovery for all creatures.
- Make `RetreatGoal` time-based instead of HP-based: "hide at waypoint for N turns,
  then resume duties regardless of HP." Simpler, more predictable.

Recommend option 1 for M1 scope; option 2 or 3 for a later polish pass.

###### 🟡 Bug 3 — MimicChest is `Solid=true` but real Chest is `Solid=false`

**Files:** `Objects.json` (MimicChest blueprint, inherits `Physics.Solid=true` from
Creature base) vs. Chest blueprint (explicitly `Physics.Solid=false`).

The whole point of a mimic is that players treat it as a real chest and then get
surprised. With `Solid=true`, a player who tries to walk onto a mimic gets blocked —
they immediately learn it's not a real chest without attacking it. The disguise fails.

**Proposed fix:** override `Physics.Solid=false` on MimicChest, and add an
interaction-wake hook so the mimic wakes when the player attempts to "open" it
(walks onto it, tries to use it as container, etc.). Or accept "walk-into" as the
wake condition by teaching `MimicChest.AIAmbush` to listen for collision events.

Current mitigation (insufficient): `WakeOnDamage=true` means attacking it still wakes
it, which is the primary gameplay loop. But the "disguise broken by walk attempt"
failure mode breaks on turn 1 for any observant player.

###### 🟡 Bug 4 — WellKeeper is `Passive` but has no `AISelfPreservation`

**Files:** `Objects.json` (WellKeeper).

WellKeeper got the Passive flag (won't initiate combat) but not an AISelfPreservation
part (won't retreat when wounded). Asymmetric: if a Snapjaw attacks a WellKeeper, the
Passive path routes through `PersonalEnemies` and the WellKeeper fights back — but
has no retreat behavior, so he fights until dead. The other three passive NPCs
(Innkeeper, Scribe, Elder) have both Passive AND AISelfPreservation. Elder doesn't
have AISelfPreservation either, same issue.

**Proposed fix:** add AISelfPreservation to WellKeeper (threshold 0.7/0.9, matching
Innkeeper) and Elder (threshold 0.7/0.9). Keeps non-combatants consistent.

###### 🔵 Polish 5 — `AIAmbushPart` doesn't reset `_dormantPushed` on wake

**File:** `AIAmbushPart.cs:46, 59`.

Once `_dormantPushed=true` is set, the flag never clears. If gameplay later introduces
a sleep spell or a "return to ambush" effect that pushes a fresh DormantGoal, the flag
won't re-arm — no effect. For M1 scope this is fine (no such effect exists), but worth
noting for future reference.

**Proposed fix (when relevant):** expose a `Rearm()` method that resets the flag; or
detect DormantGoal pop via an OnPop handler.

###### 🔵 Polish 6 — `AIAmbushPart` xml doc is now inaccurate (rationale paragraph)

**File:** `AIAmbushPart.cs:20-24`.

The doc block says:
> Pattern rationale: the push happens on the first TakeTurn event rather than in
> Initialize(). This makes the part robust to blueprint part-declaration order —
> by the time the first TakeTurn fires, all parts (including BrainPart) are
> guaranteed to exist on the entity, and the zone context is fully wired.

The claim that TakeTurn-based push is "robust to part-declaration order" is the exact
assumption that Bug 1 invalidates. Part-declaration order decides which HandleEvent
fires first, and that choice causes the ordering bug. If the code moves to Initialize()
per Bug 1's fix, this doc needs rewriting.

###### 🔵 Polish 7 — Ambush creatures may spawn in hallways/corridors

**File:** `LairPopulationBuilder.cs:86-99` (PlaceEntity uses `GatherOpenCells` which
accepts any passable cell).

A SleepingTroll in a corridor looks weird (trolls sleep in dens/rooms, not thin
passages). A MimicChest in a corridor has no reason to be disguised as a chest
(chests belong in rooms). This is a surprise-preservation polish issue.

**Proposed fix (when relevant):** add a `GatherRoomCells` helper that restricts
placement to cells with ≥ 3 adjacent passable cells (i.e., inside a room, not in a
1-cell-wide corridor).

###### 🔵 Polish 8 — `AmbushBandit` lacks `Staying=true`

**File:** `Objects.json` (AmbushBandit blueprint).

SleepingTroll and MimicChest have `Staying=true` in their Brain blocks. AmbushBandit
doesn't. Once woken, AmbushBandit will chase the player freely (no home cell magnet).
Is this intentional? Arguably yes — an active bandit is supposed to pursue.

But the inconsistency also means that on turn 1 (before the ambush push), a bandit
with Wanders=false and WandersRandomly=false falls into BoredGoal's final `WaitGoal(1)`
branch — they idle. Nothing visibly wrong, but the design intent across the three
ambushers isn't uniform.

**Proposed fix:** document the distinction in the blueprint, or add `Staying=true`
to AmbushBandit if the design is "woken bandits defend their ambush site."

###### 🔵 Polish 9 — Passive + FleeThreshold interaction needs explicit doc

**File:** `BoredGoal.cs` (post-patch Passive gate), `AISelfPreservationPart.cs`.

Innkeeper config: `RetreatThreshold=0.7, SafeThreshold=0.9, FleeThreshold=0.25`
(inherited from Creature).

Meaning:
- At 70% HP, AIBored fires → AISelfPreservation pushes RetreatGoal → walks home.
- At 25% HP, BoredGoal's `ShouldFlee()` fires → pushes FleeGoal → runs from enemy.

These two thresholds are complementary — retreat is the "graceful" fall-back, flee is
"panic mode" — but the interaction isn't documented in either file. A blueprint author
setting RetreatThreshold = 0.2 (below FleeThreshold) would create a dead zone where
flee triggers first and retreat never fires.

**Proposed fix:** add a one-paragraph cross-reference in `AISelfPreservationPart.cs`
xml explaining the FleeThreshold relationship, and ideally validate that
RetreatThreshold > FleeThreshold at Initialize time.

###### 🧪 Test gap 10 — No test asserts stack contents after turn 1

See Bug 1. Current tests only verify presence of DormantGoal, not absence of BoredGoal.
A regression test asserting `brain.GoalCount == 1 && brain.PeekGoal() is DormantGoal`
would have caught this.

###### 🧪 Test gap 11 — No test for Warden-doesn't-retreat-while-in-combat

The semantic "AISelfPreservation only fires via AIBored, so NPCs don't mid-combat
retreat" is a design choice with gameplay consequences. It should be pinned with a
test: spawn hostile in sight, drop Warden HP below threshold, fire TakeTurn, verify
RetreatGoal is NOT on stack but KillGoal IS.

###### 🧪 Test gap 12 — No integration test for LairPopulationBuilder ambush spawns

`PlaceAmbushers` has per-biome RNG logic that's only exercised via full zone
generation. Worth a targeted test: build a Cave lair with a fixed seed, count
SleepingTroll instances across N runs, verify ~25% rate.

###### 🧪 Test gap 13 — No test for `RetreatGoal.Finished_WhenStuckWithoutRegen`

Ties to Bug 2. A test that confirms RetreatGoal pops after MaxTurns when HP never
recovers would document the current (buggy) behavior until Bug 2 is fixed.

###### ⚪ Architectural note — Dictionary iteration order as load-bearing semantic

Entire M1's event-dispatch correctness depends on `Dictionary<string, Dictionary<...>>`
iterating in insertion order, which is a .NET Core 2.0+ guarantee that was formerly
undefined behavior. The project uses Unity 6 / .NET Standard 2.1, where insertion order
is preserved — but anyone migrating to an older runtime would silently break part
ordering. Worth a comment in `BlueprintLoader.Bake` noting this dependency.

###### Summary and priority

| # | Severity | Issue | Fix complexity | Status |
|---|:--------:|-------|:--------------:|:------:|
| 1 | 🔴 | Turn-1 ordering: push in Initialize, not HandleEvent | Small | ✅ Fixed |
| 2 | 🟡 | No HP regen → RetreatGoal stuck | Small–medium | ✅ Fixed |
| 3 | 🟡 | MimicChest Solid=true breaks disguise | Small | ✅ Fixed |
| 4 | 🟡 | WellKeeper/Elder missing AISelfPreservation | Trivial (JSON) | ✅ Fixed |
| 5 | 🔵 | AIAmbush `_dormantPushed` never resets | Small | ✅ Fixed |
| 6 | 🔵 | AIAmbush XML doc inaccurate | Trivial | ✅ Fixed |
| 7 | 🔵 | Ambushers spawn in hallways | Small–medium | ✅ Fixed |
| 8 | 🔵 | AmbushBandit lacks Staying | Trivial | ✅ Fixed |
| 9 | 🔵 | Passive/Flee threshold docs | Trivial | ✅ Fixed |
| 10 | 🧪 | Stack-contents-after-turn-1 regression test | Small | ✅ Fixed |
| 11 | 🧪 | Warden-does-not-retreat-while-in-combat test | Small | ✅ Fixed |
| 12 | 🧪 | LairPopulationBuilder ambush spawn rate test | Small | ✅ Fixed |
| 13 | 🧪 | RetreatGoal recovery-heals test | Small | ✅ Fixed |
| 14 | ⚪ | BlueprintLoader dictionary-insertion-order comment | Trivial | ✅ Fixed |

**All 14 findings addressed.** Highlights:
- Bug 1 fix splits into Initialize-push (primary path) + HandleEvent fallback, making
  AIAmbushPart robust to both normal blueprint loading AND edge-case manual part
  ordering (test-only scenarios where AIAmbush is attached before Brain).
- Bug 2 adds `HealPerTick` parameter (default 1) to RetreatGoal so NPCs without
  RegenerationMutation can still recover during retreat. Scoped to Recover phase
  only — does not affect general combat balance.
- Bug 3 makes MimicChest non-solid with `SightRadius=0` so the disguise holds when
  the player walks adjacent, but wakes the moment they step ONTO the mimic
  (distance-0 hostile in sight).
- New tests added: `AIAmbush_DormantGoalOnTop_NotBoredGoal_AfterFirstTakeTurn`,
  `AIAmbush_Rearm_AllowsReAmbushAfterWake`, `AIAmbush_FallbackPushOnTakeTurn_*`,
  `MimicChest_StaysDormantWhenHostileAdjacent_ButWakesOnSameCell`,
  `WellKeeper_HasAISelfPreservation_*`, `Elder_HasAISelfPreservation_*`,
  `Warden_DoesNotRetreat_WhileHostileInSight`,
  `Warden_Retreats_AfterHostileLeavesSight`,
  `RetreatGoal_Recovery_HealsHpPerTick_WithoutExternalRegen`,
  `RetreatGoal_Recovery_ClampsHealToMaxHp`,
  `RetreatGoal_Recovery_HealPerTickZero_FallsBackToMaxTurnsExit`,
  new `LairPopulationBuilderAmbushTests` fixture (6 statistical spawn-rate tests).

✅ **Verified via MCP test run:** 1317/1317 EditMode tests passing (12.5s). All 14
finding-fixes confirmed green, including the ones flagged as highest-risk during
implementation (LairPopulationBuilderAmbushTests statistical bounds, RetreatGoal
`HealPerTickZero` MaxTurns-fallback boundary, and AIAmbush Initialize-vs-fallback
ordering paths).

#### Milestone M2 — Social + Consequence Layer (Tier B, 2–3 days)

Goal: wire the remaining Phase 6 goals (`NoFightGoal`, `WanderDurationGoal`) to
real gameplay triggers. M2 adds **non-violent player tools** (persuasion,
pacification) and **world reactivity to violence** (witness effect). It consumes
M1's `Passive` flag directly as the witness filter.

This section replaces the two earlier M2 drafts (previously at this location and
under "Milestone M2 — Dialogue/Status triggers"). It was rewritten after an
audit of the actual codebase surfaced ~14 concrete drifts between the prior
plans' claimed API shapes and the real code. Corrections below.

#### Coverage after M2

| Phase 6 goal          | Wired | Triggered by                                           |
|-----------------------|:-----:|--------------------------------------------------------|
| RetreatGoal           |  M1   | AISelfPreservation at low HP                           |
| DormantGoal           |  M1   | AIAmbushPart at spawn                                  |
| Passive flag          |  M1   | Blueprint config                                       |
| NoFightGoal           |  M2   | Dialogue action / CalmMutation / conversation auto-pacify |
| WanderDurationGoal    |  M2   | WitnessedEffect (nearby violent death)                 |
| PetGoal               |  M3   | —                                                      |
| GoFetchGoal           |  M3   | —                                                      |
| FleeLocationGoal      |  M3   | —                                                      |

Post-M2: **5 of 7 shipped Phase 6 goals have real gameplay triggers.**

#### Plan corrections vs the prior M2 drafts

Each item below would have caused a compile failure, silent no-op, or spec
mismatch if followed verbatim. All are fixed in the per-sub-milestone spec
below.

| # | Prior plan claim | Reality in current code | Location |
|---|------------------|-------------------------|----------|
| 1 | `public override void OnApply()` (no args) | `virtual void OnApply(Entity target)` | Effect.cs:106 |
| 2 | `public override void OnRemove()` (no args) | `virtual void OnRemove(Entity target)` | Effect.cs:112 |
| 3 | `WitnessedEffect(int duration) : base(duration)` | `Effect` has no ctor taking duration; must assign `Duration` in body | Effect.cs:9–47 |
| 4 | `public override string ClassName => "Witnessed";` | `ClassName => GetType().Name;` is non-virtual; do not override | Effect.cs:57 |
| 5 | `public override int Type => ...;` | Property is `virtual int GetEffectType()`, returns bitmask | Effect.cs:62 |
| 6 | `DirectionalProjectileMutationBase` has 4 abstracts | Has 7: CommandName, FxTheme, CooldownTurns, AbilityRange, DamageDice, AbilityClass, ImpactVerb (plus Name/MutationType/DisplayName inherited) | DirectionalProjectileMutationBase.cs:12–18 |
| 7 | Mutations.json entry lacks Defect/Exclusions | Existing entries include both; required for schema consistency | Mutations.json:25–35 |
| 8 | `target = speaker ?? listener` in PushNoFightGoal | Speaker = NPC, listener = player; `??` would pacify player | ConversationManager.cs:69–70 |
| 9 | NoFightGoal pacification is harmless | `NoFightGoal` suppresses `AIBoredEvent`; AISelfPreservation stops firing while pacified | NoFightGoal.cs:23–30 |
| 10 | `OnRemove` can always call `RemoveGoal` on tracked reference | Goal may have popped naturally via `Finished()`; `BrainPart.RemoveGoal` on absent = no-op (verify), but guard with null-check | BrainPart.cs:131; NoFightGoal.cs:49 |
| 11 | `NoFightGoal(0)` = infinite | Confirmed: `Finished()` returns `Duration > 0 && Age >= Duration` | NoFightGoal.cs:49–52 |
| 12 | `brain.HasGoal("NoFightGoal")` | Both `HasGoal<T>()` and `HasGoal(string)` work | BrainPart.cs:146, 160 |
| 13 | `Effect.Owner` access in OnApply | Owner is set by StatusEffectsPart before OnApply fires | Effect.cs:41 |
| 14 | `NoFightGoal` ctor | `NoFightGoal(int duration = 0, bool wander = false)` | NoFightGoal.cs:40 |

#### M2.1 — NoFightGoal via dialogue + conversation auto-pacify

**Files:**

| Path | Change |
|------|--------|
| `Assets/Scripts/Gameplay/Conversations/ConversationActions.cs` | Add `PushNoFightGoal` action in `RegisterDefaults()` |
| `Assets/Scripts/Gameplay/Conversations/ConversationManager.cs` | Track per-conversation NoFightGoals; push both speaker + listener on Start; remove both on End |
| `Assets/Tests/EditMode/Gameplay/Conversations/NoFightConversationTests.cs` | **new** — 6 tests |
| `Assets/Resources/Content/Blueprints/Conversations/*.json` | Add ONE Charisma-gated persuasion branch to a hostile NPC (choice of tree deferred to implementation) |

**ConversationActions — new action (added in `RegisterDefaults()` alongside SetTag/SetProperty):**
```csharp
Register("PushNoFightGoal", (speaker, listener, arg) =>
{
    // Speaker is the NPC in this codebase — that's the entity to pacify.
    if (speaker == null) return;
    var brain = speaker.GetPart<BrainPart>();
    if (brain == null) return;
    if (brain.HasGoal<NoFightGoal>()) return; // idempotent per correction #12

    int duration = 100;
    int.TryParse(arg, out duration);
    brain.PushGoal(new NoFightGoal(duration, wander: false));
});
```

**ConversationManager — static per-entity goal tracking + Start/End hooks:**
```csharp
private static readonly Dictionary<Entity, NoFightGoal> _conversationNoFight
    = new Dictionary<Entity, NoFightGoal>();

private static void PushConversationNoFight(Entity e)
{
    if (e == null) return;
    var brain = e.GetPart<BrainPart>();
    if (brain == null) return;
    if (_conversationNoFight.ContainsKey(e)) return;
    var goal = new NoFightGoal(duration: 0, wander: false); // 0 = infinite per correction #11
    brain.PushGoal(goal);
    _conversationNoFight[e] = goal;
}

private static void RemoveConversationNoFight(Entity e)
{
    if (e == null) return;
    if (!_conversationNoFight.TryGetValue(e, out var goal)) return;
    var brain = e.GetPart<BrainPart>();
    brain?.RemoveGoal(goal);
    _conversationNoFight.Remove(e);
}
```

- In `StartConversation`, after line 75 (the existing `brain.InConversation = true`):
  `PushConversationNoFight(speaker); PushConversationNoFight(listener);`
- In `EndConversation`, before line 134 (while `Speaker`/`Listener` references
  still point at the entities): `RemoveConversationNoFight(Speaker); RemoveConversationNoFight(Listener);`

**Hallucination risks to watch while implementing:**
- **AISelfPreservation suppression** (correction #9). Document on the new helpers
  that conversation-pacified NPCs won't retreat at low HP until dialogue ends.
- **Listener = Player edge case.** If player lacks a `BrainPart` in live
  gameplay, the null-guard in `PushConversationNoFight` makes it a no-op.
  Verify in a test that player Brain presence is consistent.
- **BrainPart.RemoveGoal's OnPop semantics.** Before writing the remove helper,
  read BrainPart.cs:131 — if RemoveGoal does NOT call OnPop, document the
  asymmetry with the natural Finished()→pop path. Currently NoFightGoal has no
  OnPop behavior, so this is latent.

**Tests (6):**
1. `PushNoFightGoal_DialogueAction_PushesWithParsedDuration` — arg `"200"` → Duration=200.
2. `PushNoFightGoal_Idempotent_DoesNotStackIfAlreadyPresent`.
3. `PushNoFightGoal_EmptyOrInvalidArg_DefaultsTo100`.
4. `ConversationStart_PacifiesBothParticipants`.
5. `ConversationEnd_RemovesPacification`.
6. `ConversationStart_SpeakerAlreadyHasNoFight_DoesNotStack` — pre-push, then Start.

**Acceptance:** CHA-gated "Stand down" branch makes a hostile Warden non-aggressive for 200 turns; neither party attacks mid-conversation.

#### M2.2 — CalmMutation

**Files:**

| Path | Change |
|------|--------|
| `Assets/Scripts/Gameplay/Mutations/CalmMutation.cs` | **new** — extends DirectionalProjectileMutationBase |
| `Assets/Resources/Content/Blueprints/Mutations.json` | Append Calm entry |
| `Assets/Resources/Content/Blueprints/Objects.json:144` | Change Player `StartingMutations` from `"FlamingHandsMutation:1"` to `"FlamingHandsMutation:1,CalmMutation:1"` |
| `Assets/Tests/EditMode/Gameplay/Mutations/CalmMutationTests.cs` | **new** — 3 tests |

**Class shape (all 10 required overrides, correction #6):**
```csharp
public class CalmMutation : DirectionalProjectileMutationBase
{
    public const string COMMAND = "CommandCalm";

    // DirectionalProjectileMutationBase abstracts (7):
    protected override string CommandName   => COMMAND;
    protected override AsciiFxTheme FxTheme => AsciiFxTheme.Mental; // verify enum exists
    protected override int CooldownTurns    => 20;
    protected override int AbilityRange     => 6;
    protected override string DamageDice    => "0";                  // verify DiceRoller accepts
    protected override string AbilityClass  => "Mental Mutations";
    protected override string ImpactVerb    => "calms";

    // Part + BaseMutation abstracts (3):
    public override string Name         => "Calm";
    public override string MutationType => "Mental";
    public override string DisplayName  => "Calm";

    public int BaseDuration = 40;

    protected override void ApplyOnHitEffect(Entity target, Zone zone, System.Random rng)
    {
        var brain = target?.GetPart<BrainPart>();
        if (brain == null || brain.HasGoal<NoFightGoal>()) return;
        int duration = BaseDuration + (Level * 10);
        brain.PushGoal(new NoFightGoal(duration, wander: false));
        MessageLog.Add($"{target.GetDisplayName()} becomes peaceful.");
    }
}
```

**Mutations.json entry (correction #7 — includes Defect, Exclusions):**
```json
{
  "Name": "Calm",
  "ClassName": "CalmMutation",
  "DisplayName": "Calm",
  "Category": "Mental",
  "Cost": 4,
  "MaxLevel": 10,
  "Defect": false,
  "Ranked": true,
  "Exclusions": []
}
```

**Hallucination risks to watch while implementing:**
- **`AsciiFxTheme.Mental` may not exist.** Open `AsciiFxTheme.cs` BEFORE writing
  the override. If Mental isn't a value, either substitute (Ice? Lightning?) or
  add the enum value. Existing themes: Ice, Fire, Acid, Lightning.
- **`DiceRoller.Roll("0")`.** Verify `DiceRoller.Roll("0", rng)` returns `0`
  cleanly. If it doesn't, use `"0d1"` or override `Cast` to skip damage. Cheaper
  to fix DiceRoller if it's broken for the zero case.
- **Re-cast behavior.** Idempotency guard (`HasGoal<NoFightGoal>`) means a
  second Calm cast on the same target won't extend duration. Documented
  trade-off, not a bug.
- **"Splashes against obstacle" message on zero-damage cast.** Cosmetic; defer.

**Tests (3):**
1. `CalmMutation_AppliesNoFightGoalOnHit_WithBaseDuration` — Level=1 → Duration=50.
2. `CalmMutation_LevelScalesDuration` — Level=3 → Duration=70.
3. `CalmMutation_Idempotent_DoesNotStackOrExtendIfAlreadyPacified`.

**Acceptance:** Player casts Calm → target gains NoFightGoal(`BaseDuration + Level*10`) turns; existing pacification not replaced.

#### M2.3 — WitnessedEffect + death broadcast

**Files:**

| Path | Change |
|------|--------|
| `Assets/Scripts/Gameplay/Effects/Concrete/WitnessedEffect.cs` | **new** |
| `Assets/Scripts/Gameplay/Combat/CombatSystem.cs` | Add private static `BroadcastDeathWitnessed`; call between lines 454 and 457 |
| `Assets/Tests/EditMode/Gameplay/Effects/WitnessedEffectTests.cs` | **new** — 7 tests |

**WitnessedEffect class shape (corrections #1–5 and #13):**
```csharp
public class WitnessedEffect : Effect
{
    public override string DisplayName => "shaken";

    public override int GetEffectType()                        // NOT `Type` (correction #5)
        => TYPE_GENERAL | TYPE_NEGATIVE | TYPE_REMOVABLE;

    private WanderDurationGoal _pushedGoal;

    public WitnessedEffect(int duration = 20)                  // no `: base(duration)` (correction #3)
    {
        Duration = duration;
    }

    public override void OnApply(Entity target)                // takes Entity (correction #1)
    {
        var brain = target?.GetPart<BrainPart>();
        if (brain == null) return;
        if (brain.HasGoal<WanderDurationGoal>()) return;
        _pushedGoal = new WanderDurationGoal(Duration);
        brain.PushGoal(_pushedGoal);
        MessageLog.Add($"{target.GetDisplayName()} looks shaken.");
    }

    public override void OnRemove(Entity target)               // takes Entity (correction #2)
    {
        if (_pushedGoal == null) return;
        target?.GetPart<BrainPart>()?.RemoveGoal(_pushedGoal);
        _pushedGoal = null;
    }
}
```

**CombatSystem.HandleDeath — insert point at line 454–457, new helper in same file:**
```csharp
// New helper:
private static void BroadcastDeathWitnessed(Entity deceased, Entity killer, Zone zone, int radius)
{
    if (zone == null) return;
    var deathCell = zone.GetEntityCell(deceased);
    if (deathCell == null) return;

    // Snapshot first — the Died event fired just before this may have added
    // or removed entities, and we'll be calling ApplyEffect inside the loop.
    var snapshot = new List<Entity>(zone.GetReadOnlyEntities());

    for (int i = 0; i < snapshot.Count; i++)
    {
        var witness = snapshot[i];
        if (witness == deceased || witness == killer) continue;
        if (!witness.HasTag("Creature")) continue;
        var brain = witness.GetPart<BrainPart>();
        if (brain == null || !brain.Passive) continue;                 // M1.2 Passive consumed here

        var wCell = zone.GetEntityCell(witness);
        if (wCell == null) continue;
        if (AIHelpers.ChebyshevDistance(deathCell.X, deathCell.Y, wCell.X, wCell.Y) > radius) continue;
        if (!AIHelpers.HasLineOfSight(zone, wCell.X, wCell.Y, deathCell.X, deathCell.Y)) continue;

        witness.ApplyEffect(new WitnessedEffect(duration: 20));
    }
}

// In HandleDeath, insert between line 454 (target.FireEvent(died);) and line 457 (zone.RemoveEntity(target);):
BroadcastDeathWitnessed(target, killer, zone, radius: 8);
```

**Hallucination risks to watch while implementing:**
- **Snapshot the entity list.** `zone.GetReadOnlyEntities()` likely returns the
  live collection. Iterating while `ApplyEffect` runs could invalidate the
  enumerator. Take `new List<Entity>(...)` snapshot before iterating.
- **Killer may be null.** Environmental death, poison DoT. `witness == killer`
  with null killer is safe (null != witness), confirm by test.
- **HasLineOfSight symmetry.** Confirm `AIHelpers.HasLineOfSight` is symmetric
  across arg order before shipping. If not, "witness sees death" ≠ "death sees
  witness" and we'd pick the wrong orientation.
- **WanderDurationGoal vs NoFightGoal interaction.** If a Passive NPC already
  has NoFightGoal (e.g., mid-conversation) and witnesses a death, WanderDurationGoal
  pushes on top — the NPC wanders despite being pacified. Design-acceptable
  (shock overrides calm); document.

**Tests (7):**
1. `WitnessedEffect_PushesWanderDurationOnApply`.
2. `WitnessedEffect_OnRemove_ClearsGoal`.
3. `WitnessedEffect_OnRemove_SafeIfGoalAlreadyPoppedNaturally` — short Duration, tick past, then manual remove.
4. `CombatDeath_BroadcastsWitness_ToNearbyPassiveNpcs`.
5. `CombatDeath_DoesNotShakeActiveCombatants` — Warden/Snapjaw (Passive=false).
6. `CombatDeath_Broadcast_RespectsLineOfSight` — wall between blocks effect.
7. `CombatDeath_Broadcast_SkipsDeceasedAndKiller` — contrived Passive killer.

**Acceptance:** Kill a snapjaw near a Scribe → Scribe paces for 20 turns. Kill through a wall → no effect.

#### Sequence + rollback

Implement in order — each sub-milestone stands alone and commits separately so
any can be reverted without affecting the others.

1. **M2.2 first** — smallest blast radius (new class + JSON + Player one-liner).
2. **M2.1 second** — ConversationManager static state requires care.
3. **M2.3 last** — touches CombatSystem.HandleDeath; highest integration cost.
   Save for last so M2.1/2.2 can ship if M2.3 regresses.

Rollback: `git revert <sha>` on any single sub-milestone commit.

#### M2 verification checklist

**Per-sub-milestone gates (each must pass before moving on):**

M2.1:
- [ ] `PushNoFightGoal` registered; int/empty/invalid args handled
- [ ] Both speaker + listener pacified on StartConversation
- [ ] Both un-pacified on EndConversation
- [ ] Player (listener) pacification doesn't break player input
- [ ] 6 tests green

M2.2:
- [ ] `CalmMutation.cs` compiles with all 10 overrides
- [ ] `AsciiFxTheme.Mental` exists (or substituted)
- [ ] `DiceRoller.Roll("0")` returns 0 cleanly (or spec updated)
- [ ] Mutations.json entry round-trips through MutationRegistry
- [ ] Player starts with Calm castable
- [ ] 3 tests green

M2.3:
- [ ] `WitnessedEffect` compiles with corrected OnApply/OnRemove signatures
- [ ] BroadcastDeathWitnessed iterates a snapshot, not live enumerator
- [ ] Passive NPCs within 8 cells + LOS get effect; active combatants do not
- [ ] Wall blocks effect; killer and deceased are skipped
- [ ] 7 tests green

**Full suite target:** 1536 → ~1552 (+16 total: 6+3+7).

**Post-M2 sanity:** re-run Option A (M1 state-portion) + one M2 scenario (cast
Calm on a hostile; tick; assert no engagement). Script-verifiable.

#### Post-implementation Qud parity audit (M2)

Added after M2 shipped — survey of `qud_decompiled_project/` for each M2 feature.
M2 was planned and built without a parity pre-check; the findings below make the
actual parity status explicit so future parity work on Qud's equivalents isn't
blocked by ambiguous provenance claims.

| M2 feature | Qud equivalent | Parity status |
|------------|----------------|---------------|
| `NoFightGoal` (Phase 6 primitive) | `XRL.World.AI.GoalHandlers/NoFightGoal.cs` | **Extension.** Qud's version is 13 lines — only `CanFight() => false`. CoO's adds `Duration` + `Wander` fields that the M2.1 dialogue action and M2.2 CalmMutation both depend on. Divergence is deliberate and forward-compatible: a future strict parity run could drop our fields into a subtype without rewriting upstream uses. |
| `CalmMutation` (M2.2) | **None.** Qud's mental mutations (MentalMirror, PsionicMigraines, Telepathy, Beguile, CollectiveUnconscious) do not pacify via NoFightGoal. | **CoO-original.** Builds on Qud's `DirectionalProjectileMutationBase` shape but the pacify-on-hit mechanic is ours. |
| `PushNoFightGoal` dialogue action (M2.1) | **None** in Qud's `ConversationActions` registry. | **CoO-original hook.** |
| `WitnessedEffect` (M2.3) | `XRL.World.Effects/Shaken.cs` (partial) | **Divergent mechanics, same classification.** Qud's `Shaken` carries a `Level` field and applies `-Level DV` via `StatShifter`; ours pushes `WanderDurationGoal` for a pacing animation. Both share the `"shaken"` display name and the max-on-stack merge rule. Our `GetEffectType()` now matches Qud's bitmask (`117440514` = `TYPE_MENTAL \| TYPE_MINOR \| TYPE_NEGATIVE \| TYPE_REMOVABLE`) so future mental-effect category queries classify both correctly. |
| `BroadcastDeathWitnessed` (M2.3) | **None.** Qud's `Shaken` is fired from `ApplyShaken` events in combat contexts (`CryptFerretBehavior.cs` etc.), never from a death handler. | **CoO-original mechanic.** Uses M1.2's `Passive` flag as the filter — an M1-to-M2 hook that has no Qud precedent. |
| `Effect.OnStack` merge override | Qud uses `Shaken.Apply` override + `Object.GetEffect<Shaken>()` lookup | **Different override point, equivalent behavior.** Both take `max(existing, incoming)` on Duration. If future Qud parity work on `Shaken` grows complex merge rules (level-capping, resistance), switching our merge to `Apply` is the natural refactor. |

**Net:** M2 delivers Qud-primitive consumers (`NoFightGoal`, `WanderDurationGoal`) via
CoO-original triggers. The Phase 6 coverage claim ("5 of 7 shipped goals now have
real gameplay triggers") stands; the claim that M2 is "Qud parity work" does NOT —
M2 is closer to "Qud-inspired extensions that consume Qud-parity primitives."

#### Milestone M3 — Ambient behavior parts (Tier B, 2–3 days)

Goal: after M3, `PetGoal`, `GoFetchGoal`, and `FleeLocationGoal` are all
triggered via new NPC blueprints and gameplay events — the village feels
more alive.

##### M3.1 — AIPetterPart + VillageChild

**Files to create:** `Assets/Scripts/Gameplay/AI/AIPetterPart.cs`

**Class shape (mirrors AIWellVisitorPart):**
```csharp
public class AIPetterPart : AIBehaviorPart {
    public override string Name => "AIPetter";
    public int Chance = 3; // % per bored tick

    public override bool HandleEvent(GameEvent e) {
        if (e.ID == AIBoredEvent.ID) {
            var brain = ParentEntity.GetPart<BrainPart>();
            if (brain?.Rng == null || brain.CurrentZone == null) return true;
            if (brain.HasGoal("PetGoal")) return true;
            if (brain.Rng.Next(100) >= Chance) return true;
            brain.PushGoal(new PetGoal());
            e.Handled = true;
            return false;
        }
        return true;
    }
}
```

**VillageChild blueprint** — new entry in Objects.json:
- Render: `c`, `&Y`, "child"
- Brain: Wanders, Passive=true
- AIPetter: Chance=5
- Conversation: VillageChild_1
- Stats: HP 10, Str 6, Agi 12, Tou 8
- Faction: Villagers

**Placement in VillagePopulationBuilder.cs** — after Innkeeper placement, spawn 1–2 `VillageChild` near her cell via expand-ring search for passable interior cells (max radius 5).

**Tests:**
- `AIPetter_PushesPetGoal_AtChanceRate` (Chance=100 → deterministic)
- `AIPetter_DoesNotDoublePush`
- `VillageChild_BlueprintLoads_WithAIPetter`

**Acceptance:** Enter village → 1-2 children near Innkeeper emit magenta `*` particles periodically.

##### M3.2 — AIHoarderPart + AIRetrieverPart (GoFetchGoal)

**Files to create:**
- `Assets/Scripts/Gameplay/AI/AIHoarderPart.cs`
- `Assets/Scripts/Gameplay/AI/AIRetrieverPart.cs`

**Files to modify:**
- `Assets/Scripts/Gameplay/Inventory/Commands/Item/ThrowItemCommand.cs`
- `Assets/Resources/Content/Blueprints/Objects.json` (Magpie, PetDog, Shiny tag on gold)

**AIHoarderPart** — scans zone via `GetReadOnlyEntities()` for items with a configurable tag (default "Shiny"), picks nearest by Chebyshev, pushes `GoFetchGoal(item, returnHome: true)`. Gated with `!HasGoal("GoFetchGoal")`.

**AIRetrieverPart** — listens for `"ItemLanded"` events (fired from ThrowItemCommand). If the throw came from an ally (configurable) and the item is within `NoticeRadius`, pushes `GoFetchGoal(item, returnHome: false)`.

**ThrowItemCommand modification** — after the item lands, fire `ItemLanded` event on every Creature in the zone with `Item`, `Thrower`, `LandingCell` parameters.

**Blueprints:**
- `Magpie` — flying-creature template, Inventory(MaxWeight=20), AIHoarder(TargetTag="Shiny", Chance=15)
- `PetDog` stub — Inventory(10), Brain(Passive), AIRetriever(AlliesOnly=true), AIPetter(4%)
- Add `{"Key": "Shiny", "Value": ""}` tag to `GoldPile`, gem blueprints

**Tests:**
- `AIHoarder_FindsAndPushesGoFetch_ForTaggedItem`
- `AIHoarder_IgnoresUntaggedItems`
- `AIRetriever_PushesGoFetch_OnAllyThrow`
- `AIRetriever_IgnoresEnemyThrow`
- `Throw_FiresItemLandedEvent_ToZoneCreatures`

**Acceptance:** Drop gold near Magpie → Magpie fetches it. Throw bone near PetDog → dog fetches and returns.

##### M3.3 — AIFleeToShrinePart + SanctuaryPart + Shrine

**Files to create:**
- `Assets/Scripts/Gameplay/Settlements/SanctuaryPart.cs`
- `Assets/Scripts/Gameplay/AI/AIFleeToShrinePart.cs`

**Files to modify:**
- `Assets/Resources/Content/Blueprints/Objects.json` (Shrine blueprint; add AIFleeToShrine to Scribe/Elder)
- `Assets/Scripts/Gameplay/World/Generation/Builders/VillageBuilder.cs` (place Shrine)

**SanctuaryPart** — marker part on shrines/altars. Optional `HealOverTime` flag (deferred to polish).

**AIFleeToShrinePart** — on AIBored when HP < FleeThreshold, scans zone for nearest SanctuaryPart-bearing entity, pushes `FleeLocationGoal(cell.X, cell.Y, maxTurns: 50)`. Gated with `!HasGoal("FleeLocationGoal")`.

**Priority note:** If both AIFleeToShrine and AISelfPreservation are attached, declare AIFleeToShrine FIRST in the blueprint so it processes the bored event first and wins (HasGoal gate prevents AISelfPreservation from overriding).

**Shrine blueprint:**
- Render: `†`, `&Y`, "shrine"
- Physics: non-solid, non-takeable
- Sanctuary: HealOverTime=false (polish later)
- Tag: Furniture

**Integration in VillagePopulationBuilder.cs** — before NPC placement, call `PlaceShrine(zone, factory, rng)` to place one shrine in a central passable cell.

**Blueprint modification** — Scribe and Elder get an `AIFleeToShrine` part added before their AISelfPreservation entry.

**Tests:**
- `AIFleeToShrine_PushesFleeLocationGoal_WhenHpLow`
- `AIFleeToShrine_IgnoresFullHp`
- `AIFleeToShrine_NoShrine_DoesNothing` (no shrine in zone → falls through to AISelfPreservation)
- `Shrine_Blueprint_Loads`

**Acceptance:** Wound the Scribe → she flees to the shrine instead of home. Destroy the shrine → she falls back to AISelfPreservation (home).

##### M3 Verification checklist
- [ ] AIPetterPart + VillageChild; 1-2 children near Innkeeper
- [ ] AIHoarderPart + Magpie; Shiny tag on gold/gems
- [ ] AIRetrieverPart + PetDog; ThrowItemCommand fires ItemLanded
- [ ] SanctuaryPart + Shrine; AIFleeToShrine on Scribe/Elder
- [ ] Shrine placed in villages during generation
- [ ] All M3 tests green; full suite still passes

#### Cross-milestone dependencies

```
          ┌──> M1.1 AISelfPreservation ─┐
M1 ──────>├──> M1.2 Passive ────────────┼──> Foundation for witness (M2.3)
          └──> M1.3 AIAmbush ───────────┘

          ┌──> M2.1 NoFight dialogue ──> Dialogue-pacify foundation
M2 ──────>├──> M2.2 CalmMutation
          └──> M2.3 WitnessedEffect ───> Needs M1.2's Passive flag

          ┌──> M3.1 AIPetter
M3 ──────>├──> M3.2 AIHoarder/Retriever
          └──> M3.3 AIFleeToShrine ────> Optional: combos with M1.1
```

**Strict ordering:** M1 before M2.3 (WitnessedEffect filters by Passive).
Otherwise M2 and M3 can proceed in parallel.

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

## Scenario Library — Phase 3: Scenario-as-Test Infrastructure

**Status:** ✅ SHIPPED (2026-04-18). All five sub-phases (3a–3e) landed with
review follow-ups for each. Canonical details live in
`Docs/SCENARIO_SCRIPTING.md`; this section retained for Qud-parity work
tracking since Phase 3 directly affects how parity features get validated.

**What shipped:**
- 3a — `ScenarioTestHarness` (fixture-scope factory)
- 3b — `ctx.AdvanceTurns(n)` + runtime `TurnManager.Entities` accessor
- 3c — `ctx.Verify()` fluent assertion DSL (4 verifier types, ~25 methods)
- 3d — `AIBehaviorPartTests` ported end-to-end (13 tests, 385 → 310 lines)
- 3e — Docs updated in `Assets/Scripts/Scenarios/README.md` and
  `Docs/SCENARIO_SCRIPTING.md`

**Honest numbers:**
- Test suite: 1445 / 1445 passing (0 regressions across full suite)
- New tests added by Phase 3 infrastructure: 58 (harness 10 + AdvanceTurns 11 + Verify 48, minus overlaps)
- Line reduction on the one deep port: 19% (below the 30-40% plan target;
  raw delta undersells the semantic cleanup — per-test bodies halved)

**Next parity work that benefits:** every Phase 7 (`AIBehaviorPart` subclasses
like `AIPilgrim`, `AIShopper`) test from here on can use the scenario-as-test
stack. No more hand-rolled creature helpers per fixture.

---

**Original plan (retained for reference):**

**Design thesis:** Phase 3 isn't "scenarios can now run in CI." It's *"the
scenario IS the test fixture."* The scenario's `Apply()` is the only place
the setup exists — tests call the same `Apply()` users click-to-launch.
Drift goes to zero.

**Why it matters for parity work:** every Qud-parity behavior we port
(RetreatGoal, AIGuard, AIWellVisitor, future goals) has a "canonical
situation" that proves it works. Today each test re-declares the setup
inline; Phase 3 lets tests + manual playtests share one fixture.

### Sub-phase breakdown

#### 3a — `ScenarioTestHarness` (foundation)

- `Assets/Tests/EditMode/TestSupport/ScenarioTestHarness.cs` — fixture-scope
  factory + context builder. Encapsulates `FactionManager.Initialize`,
  `EntityFactory` with blueprint JSON load, stub player creation,
  `ScenarioContext.CreateContext(rngSeed)` per-test.
- **Key design call:** test assembly only, zero runtime deps. Keeps
  `Application.dataPath` out of runtime builds.
- **Impact:** replaces the `BuildContext()` helpers in `PlayerBuilderTests`,
  `ZoneBuilderTests`, `EntityBuilderModifierTests`, `AIBehaviorPartTests`.
- **Scope:** ~150 lines, 1–2 hours.

#### 3b — Turn advancement API

- `ScenarioContextExtensions.AdvanceTurns(this ctx, int count)` — fires
  `TakeTurn` on every registered entity N times. Simple tick; speed-independent.
- **Runtime touchpoint:** expose `TurnManager.Entries` as read-only view
  (currently private). One-line addition.
- **Alternative rejected:** energy-accurate advancement — matches game loop
  exactly but requires deeper wiring; tests rarely need it. Defer until a
  concrete test demands it.
- **Scope:** ~50 lines + 1 runtime accessor, 1 hour.

#### 3c — Fluent `Verify()` API

Extension method on `ScenarioContext` (test assembly) returning a chainable
verifier. NUnit-native failures with readable messages.

```csharp
ctx.Verify()
    .Entity(warden).IsAt(10, 10).HasHpFraction(0.20f).HasGoalOnStack<GuardGoal>()
    .Back()
    .Player().HasMutation("FireBoltMutation").HasEquipped("ShortSword")
    .Back()
    .EntityCount(withTag: "Creature", expected: 3);
```

Initial surface (~20 methods across 4 verifier types):

| Verifier | Methods |
|----------|---------|
| `EntityVerifier` | `IsAt`, `HasHpFraction`, `HasStat`, `HasStatAtLeast`, `HasPart<T>`, `HasGoalOnStack<T>`, `HasTag`, `IsAlive` |
| `PlayerVerifier` | `HasMutation`, `HasItemInInventory`, `HasEquipped`, `HasFactionRep`, `IsAt` |
| `CellVerifier` | `ContainsBlueprint`, `IsEmpty`, `IsPassable`, `IsSolid` |
| `ScenarioVerifier` (root) | `EntityCount(withTag, expected)`, `PlayerIsAlive`, `TurnCount` |

**Design calls:** extension-only (no runtime pollution), `.Back()` to
escape sub-verifiers, each assertion generates readable fail messages.

**Scope:** ~500 lines, 3 hours.

#### 3d — Port existing tests as proof

Port 4 test files to prove the harness + Verify() combination is
measurably cleaner:

| Test file | Why |
|-----------|-----|
| `AIBehaviorPartTests.cs` | 13 tests with manual creature setup — biggest line-count savings |
| `EntityBuilderModifierTests.cs` | 9 tests, repeated fixture setup |
| `PlayerBuilderTests.cs` | 20 tests — harness migration baseline |
| `ZoneBuilderTests.cs` | 16 tests — easy port |

Rule: only port tests with non-trivial scenario-like setup. Don't port
`Stat_Clamps_ToMax()`-style pure algorithm tests.

Target: 30–40% line reduction across ported files.

#### 3e — Docs

- `Assets/Scripts/Scenarios/README.md` — new "Reusing scenarios as tests"
  section with before/after walkthrough
- `Docs/SCENARIO_SCRIPTING.md` — Phase 3 marked complete, acceptance criteria
  updated

### Phase 3 non-goals (defer)

- `RunAsTest` attribute magic (e.g. `[TestScenario(typeof(WoundedWarden))]`)
  — punt; cleverness exceeds payoff
- Mock/fake parts for unit-test isolation — unit-test territory, out of scope
- PlayMode test harness — separate runner, separate concerns
- Perf benchmark API — Phase 5 if requested
- Parameterized scenarios — Phase 5 per roadmap

### Risks

| Risk | Mitigation |
|------|------------|
| Blueprint load expensive (~100ms) | Fixture-scope sharing via `OneTimeSetUp` (established pattern) |
| `AdvanceTurns` semantics drift from real loop | Simple TakeTurn-per-entity matches current tests exactly; add energy-accurate variant separately if demanded |
| `Verify()` surface balloons | Start with ~20 methods; add only per-concrete-test-demand |
| Runtime assembly pollution | Strict rule: Phase 3 code in `Assets/Tests/EditMode/TestSupport/` only. One runtime exception: `TurnManager.Entries` accessor |
| Ported tests fail for subtle reasons | Port incrementally, full-suite after each. No big-bang migration |

### Implementation sequence

```
1. 3a — ScenarioTestHarness + migrate PlayerBuilderTests + ZoneBuilderTests
2. 3b — TurnManager.Entries accessor + AdvanceTurns extension
3. 3c — Verify() root + EntityVerifier (highest-demand first)
4. 3c — PlayerVerifier + CellVerifier + ScenarioVerifier
5. 3d — Port AIBehaviorPartTests + EntityBuilderModifierTests
6. 3e — Docs
```

Three commit boundaries: `3a`, `3b+3c`, `3d+3e`.

### Acceptance criteria

- [ ] All existing tests still pass (zero regressions)
- [ ] `ScenarioTestHarness` in test assembly, zero runtime deps except `TurnManager.Entries`
- [ ] `ctx.Verify()` chain produces NUnit-native failure messages
- [ ] At least 2 test files ported with measurable line reduction (target: 30–40%)
- [ ] README has a "Reusing scenarios as tests" section with full before/after example
- [ ] `SCENARIO_SCRIPTING.md` marks Phase 3 complete

---

## Implementation Priority (Recommended)

1. **Tier 4 polish** (small wins in already-shipped systems): SittingEffect visual indicator, tunable scan frequency, force-move auto-cleanup
2. **Phase 7 — more AIBehaviorPart subclasses**: pulls `HasGoal(string)` into production
3. **Phase 12 — Calendar**: unlocks day/night schedules (huge "lived-in" impact)
4. **Phase 6 — missing goals**: gradually add as content demands them
5. **Phase 10 — debug introspection**: low-cost developer QoL
6. **Phase 9 — opinion system**: refines combat/conversation feel
7. **Phase 14 — combat intelligence**: last big feature
