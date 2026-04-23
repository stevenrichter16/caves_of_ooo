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
| MoveToExteriorGoal | 🟢 Shipped | — (M4) |
| MoveToInteriorGoal | 🟢 Shipped | — (M4) |

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
| **M2** — Dialogue/status triggers | ✅ Done | B | 2–3d | NoFightGoal, WanderDurationGoal |
| **M3** — Ambient behavior parts | ✅ Done | B | 2–3d | PetGoal, GoFetchGoal, FleeLocationGoal |
| **M4** — Interior/Exterior (Gap B) | ✅ Done (1665/1665) | C | 3–4d | MoveToInterior/ExteriorGoal, weather foundation |
| **M5** — Corpse system (Gap C) | ✅ Done (1702/1702, PlayMode-verified) | C | 3–5d | CorpsePart, DisposeOfCorpseGoal, AIUndertakerPart, Graveyard blueprint |
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

##### M1 Post-audit findings (2026-04-23)

Pass 5 of the Comprehensive Audit. Read: `AISelfPreservationPart.cs`,
`RetreatGoal.cs`, `AIAmbushPart.cs`, `DormantGoal.cs`, `BrainPart.cs`
(Passive field semantics), blueprint grep for `AISelfPreservation` /
`AIAmbush` / `Passive=true`. Verified `AIAmbushPart.Rearm` is tested
(`AIAmbushPartTests.cs:161`). Checked M1.R-3 fix (RetreatGoal
BaseValue vs Value) at `RetreatGoal.cs:109-125`.

**4 findings** — 0 🔴, 0 🟡, 1 🔵, 2 🧪, 1 ⚪.

Smaller finding count than other milestones because M1 shipped with
14 pre-ship review findings fixed (see §M1 Code Review — Findings
above); the patterns that would surface new audit-tier issues were
largely closed during initial development.

| # | Sev | Cat | Title | File:line |
|---|-----|-----|-------|-----------|
| M1.A1 | 🔵 | logic | AISelfPreservation entry uses Stat.Value; RetreatGoal exit uses BaseValue — Penalty-without-damage can thrash | `AISelfPreservationPart.cs:86` vs `RetreatGoal.cs:109-125` |
| M1.A2 | 🧪 | test-playmode | No PlayMode sanity sweep for M1 | n/a |
| M1.A3 | 🧪 | test-manual | M1 scenarios (CorneredWarden, IgnoredScribe, SleepingTroll, MimicSurprise) manual observation pending | all menu-wired |
| M1.A4 | ⚪ | doc | Value/BaseValue asymmetry design decision documented in RetreatGoal but not AISelfPreservationPart | cross-ref requires reading both files |

###### 🔵 M1.A1 (logic) — Value/BaseValue asymmetry can thrash under HP Penalty without damage

**Files:**
- **Entry gate:** `AISelfPreservationPart.cs:86`
  ```csharp
  int hp = ParentEntity.GetStatValue("Hitpoints", 0);  // returns Stat.Value (BaseValue - Penalty)
  ```
- **Exit gate:** `RetreatGoal.cs:123-131`
  ```csharp
  int baseHp = hpStat.BaseValue;  // deliberately BaseValue, docstring lines 116-125 explains
  ```

Walkthrough of the thrash:
1. NPC has `Hitpoints.BaseValue=100, Max=100, Penalty=100 → Value=0`.
   (Penalty-without-damage scenario; future `Wounded` / `Exhausted`
   effects could produce this shape.)
2. BoredGoal fires `AIBoredEvent`. `AISelfPreservationPart.HandleBored`:
   `fraction = 0 / 100 = 0`, `<= RetreatThreshold` → push `RetreatGoal`.
3. BrainPart's next-tick cleanup: `RetreatGoal.Finished` evaluates
   `baseHp / Max = 100/100 = 1.0`, `>= SafeThreshold (0.75)` → true.
   Goal pops on first Finished-check before any TakeAction.
4. Next tick's BoredGoal fires `AIBoredEvent`. `AISelfPreservation`
   idempotency gate `brain.HasGoal("RetreatGoal")=false`.
   `Value=0` still → push RetreatGoal again → pop immediately again.
5. Thrash: push-pop every tick for as long as the Penalty is held.

No current content produces HP Penalty without damage. `StatusEffectsPart`
hooks in the codebase apply HP reductions via actual damage (damage
reduces BaseValue directly, not Penalty). So the bug is **latent** —
possible but not currently triggered.

**Why it matters:** if a future status effect (e.g., `Wounded`,
`Fatigued`, a debuff gauntlet applied by a boss) applies HP Penalty
mechanically, every affected NPC starts thrashing silently. Perf
impact is one-push-one-pop per tick per NPC, small but cumulative.
More concerning: the `MessageLog` emits nothing, so the bug is only
visible via goal-stack inspector.

**Proposed fix:** three options:
- (a) Align the gates: `AISelfPreservationPart.HandleBored` uses
  `hpStat.BaseValue` too. Reads as "retreat when I've objectively
  taken damage." Consistent; loses the "Penalty fraction" signal
  if that ever matters.
- (b) Align the other way: `RetreatGoal.Finished` uses Value. Reads
  as "recover fully (penalty-free)." Risk: a Penalty-stuck NPC can't
  satisfy the exit and stays in RetreatGoal forever — deadlock
  instead of thrash.
- (c) Explicit logic: AISelfPreservation checks both Value AND
  BaseValue gates; only fires when BaseValue is also below threshold.
  Reads as "retreat when the damage is real, not just debuffs."
  More code, most semantically honest.

**Severity rationale:** 🔵 because no current content triggers.
Promote to 🟡 if a non-damage HP Penalty effect ships.

---

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

##### M2 Post-audit findings (2026-04-23)

Pass 4 of the Comprehensive Audit. Read: `NoFightGoal.cs`,
`WanderDurationGoal.cs`, `ConversationActions.cs` (PushNoFightGoal
registration), `CalmMutation.cs`, `WitnessedEffect.cs`, `CombatSystem.cs`
(BroadcastDeathWitnessed), `PacifiedWarden.cs` scenario,
`NoFightConversationTests.cs`, `CalmMutationTests.cs`,
`WitnessedEffectTests.cs`. Grepped `Assets/Resources/**/*.json` for
`PushNoFightGoal` consumers.

**5 findings** — 0 🔴, 1 🟡, 1 🔵, 2 🧪, 1 ⚪.

| # | Sev | Cat | Title | File:line |
|---|-----|-----|-------|-----------|
| M2.A1 | 🟡 | wiring | `PushNoFightGoal` dialogue action has zero content consumers | `ConversationActions.cs:295` (registered), grep `Assets/Resources` for `PushNoFightGoal` → 0 |
| M2.A2 | 🔵 | design | NoFightGoal suppresses ALL AIBehaviorPart responses while active; pacified low-HP NPCs can't self-retreat | `NoFightGoal.cs:22-30` (documented) |
| M2.A3 | 🧪 | test-playmode | No PlayMode sanity sweep for M2 | n/a |
| M2.A4 | 🧪 | test-manual | 7 M2 scenarios' manual observation pending | `ScenarioMenuItems.cs` entries all wired; no user reports |
| M2.A5 | ⚪ | parity | WitnessedEffect classifies as Mental but lacks Qud's `-Level DV` stat-shift | `WitnessedEffect.cs:36-41` (documented) |

###### 🟡 M2.A1 (wiring) — PushNoFightGoal dialogue action has zero consumers

**Files:**
- **Registered:** `Assets/Scripts/Gameplay/Conversations/ConversationActions.cs:295-327`
- **Tested:** `Assets/Tests/EditMode/Gameplay/Conversations/NoFightConversationTests.cs` (8 tests exercise the action via direct `ConversationActions.Execute` calls).
- **Consumed by dialogue content:** grep `-rn "PushNoFightGoal" Assets/Resources` returns **zero** matches.

The M2.1 shipping narrative (QUD-PARITY.md:1571) documents
`PushNoFightGoal` as a "CoO-original hook" intended to make
conversation choices like "Stand down, friend" actually pacify the
speaker. The C# action is fully wired: idempotency guard, TryParse
trap-fix, MessageLog echo. But no conversation tree in
`Assets/Resources/...` references the action name.

The existing M2 scenarios that exercise pacification
(`PacifiedWarden.cs`, `CalmTestSetup.cs`, etc.) bypass dialogue by
calling `AsPersonalEnemyOf` then casting `CalmMutation`, not by
triggering the dialogue action.

**Why it matters:** M2.1's shipped claim is "dialogue-pacify." Tests
confirm the action fires correctly. But a player in the actual game
cannot trigger the action because no dialogue offers it. The feature
is functionally inert in the shipped game — same class as M5.A3
(Graveyard without world-gen placement). The M2 parity audit table
correctly labels the action "CoO-original hook," but doesn't flag
that the hook has no caller.

**Proposed fix:** add a dialogue choice to at least one Villager /
Merchant / Scribe conversation tree (wherever `Conversation_1.json`
or similar lives) that calls `PushNoFightGoal` with an appropriate
duration. E.g., a "calm down" option on the Warden's dialogue when
the player has some high-Ego stat. Needs dialogue-content authoring
+ a scenario that demonstrates it. ~2 hours.

**Severity rationale:** 🟡 because the M2.1 shipping claim is
partially unverified in normal play. Not 🔴 because the tests pin
the action's behavior — the gap is content, not correctness.

###### 🔵 M2.A2 (design) — NoFightGoal suppresses self-preservation

**File:** `Assets/Scripts/Gameplay/AI/Goals/NoFightGoal.cs:22-30`

Docstring explicitly warns:

> *⚠️ Side-effect: while NoFightGoal is on top of the stack,
> AIBoredEvent does not fire, which means all AIBehaviorPart
> subclasses stop responding — including AISelfPreservationPart. A
> pacified creature at critical HP will not be retreated by
> self-preservation until the NoFightGoal expires or is removed.*

This is a documented design trade-off — pacification is "complete"
by intent. But it creates a concrete gameplay surface: Calm a hostile
Warden → Warden stops attacking → also stops self-preservation →
any ongoing damage (bleed, poison, environmental) kills the Warden
uninterrupted.

**Why it matters:** calm-then-kill-with-damage-over-time is an
exploit. The player's counter-strategy ("calm + wait for DoT")
bypasses the "non-lethal pacification" intent.

**Proposed fix:** two options:
- (a) Add a NoFightGoal-bypass in AISelfPreservationPart: if HP
  fraction < critical threshold (e.g., 0.15), remove NoFightGoal
  and push RetreatGoal. Documented as "pacification doesn't override
  flight instinct when dying."
- (b) Accept the current semantics and document the exploit as
  intentional (calm-then-kill is a valid strategy by design).

**Severity rationale:** 🔵 because it's documented existing behavior,
not new drift. Elevate to 🟡 if playtest shows it's a common exploit.

---

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

##### M3 Plan Verification Sweep (applying Methodology Template §1.2)

Pre-implementation audit of every API-shape and path claim across §§M3.1–M3.3.
Applies the Methodology Template's Part 1.2 discipline: never trust a plan's
signature claims — verify each against the live code before writing any
implementation, then log corrections here so the implementer sees them before
touching the keyboard.

Status: **plan is solid overall**, with one hard path correction and several
implementation-time notes the plan didn't surface. All three Phase 6 goal
primitives (`PetGoal`, `GoFetchGoal`, `FleeLocationGoal`) are confirmed present
and non-stub.

**Plan corrections (MUST be applied before implementation):**

| # | Plan claim | Actual state | Action |
|---|------------|--------------|--------|
| 1 | `ThrowItemCommand.cs` lives at `.../Commands/Item/ThrowItemCommand.cs` | Path is `.../Commands/Disposition/ThrowItemCommand.cs` (confirmed at file:1) | M3.2 file list must be updated |
| 2 | `FleeLocationGoal(x, y, maxTurns)` — 3 params | Ctor is `FleeLocationGoal(int x, int y, int maxTurns = 30, bool endWhenNotFleeing = true)` — 4 params with defaults | None (plan's 3-arg call compatible); note second optional param exists |
| 3 | `PetGoal()` — only no-arg ctor | Two ctors: `PetGoal()` and `PetGoal(Entity target)`. Targeted variant pre-sets `_phase = Approach`, skipping FindAlly. | None for M3.1; noted for future targeted-pet callers |

**Primitive goal readiness (all ✅):**

- `PetGoal.cs` — fully implemented, not a stub. Two-phase (FindAlly →
  Approach), emits magenta `*` particle on success, capped at
  `MaxApproachAttempts=3` pushes of `MoveToGoal` to prevent infinite chase.
  Uses `GetReadOnlyEntities()` to scan — see implementation-time note #2 below.
- `GoFetchGoal(Entity item, bool returnHome = false)` — present, matches
  M3.2's `new GoFetchGoal(item, returnHome: true)` call.
- `FleeLocationGoal(int x, int y, int maxTurns = 30, bool endWhenNotFleeing = true)`
  — present, matches M3.3's `new FleeLocationGoal(cell.X, cell.Y, maxTurns: 50)`.

**Pattern parity (AIWellVisitorPart reference):**

M3.1's class shape cites "mirrors AIWellVisitorPart." Verified against
`Assets/Scripts/Gameplay/AI/AIWellVisitorPart.cs:15–24`:
- `class : AIBehaviorPart` ✓
- `public override string Name => ...` ✓
- `public int Chance = ...` ✓
- `HandleEvent` on `AIBoredEvent.ID` with `brain.Rng.Next(100) >= Chance` gate ✓
- `e.Handled = true; return false;` consume pattern ✓

AISelfPreservationPart, AIGuardPart, and AIWellVisitorPart all use identical
event-consumption shape. M3.1/M3.2/M3.3 are safe to copy it.

**ThrowItemCommand insertion point (M3.2 BroadcastItemLanded):**

Concrete line anchor — both `if (!consumedOnImpact)` blocks at lines 194 and 201
can be merged. The broadcast belongs AFTER `zone.AddEntity` succeeds (so
`landingCell` is valid and the item is placed) and BEFORE `transaction.Do`
(so a roll-back of the throw also rolls back any AIRetriever response).
Insertion shape:

```csharp
if (!consumedOnImpact && !zone.AddEntity(itemToThrow, landingCell.X, landingCell.Y))
{
    return InventoryCommandResult.Fail(
        InventoryCommandErrorCode.ExecutionFailed,
        "The thrown item could not land.");
}

if (!consumedOnImpact)
{
    BroadcastItemLanded(zone, actor, itemToThrow, landingCell);  // ← M3.2 addition

    transaction.Do(
        apply: null,
        undo: () => zone.RemoveEntity(itemToThrow));
}
```

The `consumedOnImpact` branch (thrown tonic applied on hit, line 160–164)
is correctly excluded — no landed item exists to fetch.

**Implementation-time notes (NOT plan drift — forward reminders):**

1. **Snapshot discipline on GetReadOnlyEntities** (Methodology Template §7.2):
   `PetGoal.FindNearestAlly` iterates `GetReadOnlyEntities()` and nothing it
   does mutates `_entityCells`, so it's fine. BUT M3.2's `AIHoarderPart` (scans
   items, pushes GoFetchGoal) and `BroadcastItemLanded` (fires events on every
   Creature, some of which may call `ApplyEffect`) should use
   `zone.GetAllEntities()` or take a `new List<Entity>(...)` snapshot before
   iterating. Same pitfall that caught us in M2.3 BroadcastDeathWitnessed.

2. **M3.2's AIHoarderPart scan is O(all-zone-entities) per bored tick**:
   if a zone has thousands of entities (live sweep confirmed 2181 in the
   starting zone), scanning for Shiny items every AIBored tick on every
   Magpie will be O(magpies × entities) per tick. For a village with one
   Magpie this is fine; if M3 grows into "flock of magpies" scenarios
   the scan needs a spatial index. Acceptable for shipping; flag for
   Phase 7 polish.

3. **M3.3's "declare AIFleeToShrine FIRST" blueprint ordering**: relies on
   `Dictionary<string, Dictionary<...>>` insertion-order preservation for
   `HandleEvent` dispatch — the same ⚪ architectural dependency
   documented in M1 review finding #14 (`BlueprintLoader.Bake`). Works on
   Unity 6 / .NET Standard 2.1. If an ordering regression ever surfaces
   (AISelfPreservation fires before AIFleeToShrine despite the blueprint
   listing order), look first at `BlueprintLoader.Bake`'s dictionary
   iteration.

4. **Tests from Methodology Template §3 should be planned per sub-milestone**:
   - M3.1: EditMode unit tests with `Chance=100` (deterministic) + counter-
     check with `Chance=0`. Integration test via `ScenarioTestHarness` that
     exercises the full AIBoredEvent → HandleEvent → PushGoal path on a real
     blueprint.
   - M3.2: unit tests for `AIHoarderPart.FindNearestTagged` (scan correctness),
     `AIRetrieverPart.HandleItemLanded` (ally-filter, radius gate, idempotent
     push). Integration test for `ThrowItemCommand → BroadcastItemLanded`.
     Counter-checks: enemy throw filtered, out-of-radius throw filtered.
   - M3.3: unit tests for `AIFleeToShrinePart` (HP gate, zone scan for
     SanctuaryPart). Integration test for "no shrine in zone → falls through
     to AISelfPreservation." Counter-check: shrine destroyed mid-scenario
     reverts the NPC to AISelfPreservation.
   - All three sub-milestones: regression tests per Methodology Template
     §3.3 for any bug caught during implementation.

5. **Manual playtest scenarios** (Methodology Template §3.6) to write during
   or after each sub-milestone:
   - M3.1: `VillageChildrenPetting` — enter a village, observe `*` particles
     periodically near the Innkeeper's chair. Counter-case: combat scenario
     where children retreat/stop petting (Passive flag interaction).
   - M3.2: `MagpieFetchesGold` — drop gold near Magpie, watch it fetch.
     `ThrownBoneForDog` — throw bone past a PetDog, watch it fetch.
     Counter-case: enemy throwing a bone — PetDog ignores (ally-only filter).
   - M3.3: `WoundedScribeFleesToShrine` — wound Scribe, observe flight toward
     shrine instead of home. Counter-case: destroy the shrine mid-flight,
     observe fallback to AISelfPreservation (home).

6. **Starting-zone hazards** (from M2 scenario implementation, same trap
   likely hits M3 scenarios): east-axis scenarios must call
   `ctx.World.ClearCell(p.x + 1..5, p.y)` to remove the starting zone's
   West compass stone (player+2,0), grimoire chest (player+4,0), and East
   compass stone (player+6,0). See M2 scenarios `PacifiedWarden.cs` and
   `ScribeWitnessesSnapjawKill.cs` for the pattern.

**Post-M3 Qud parity audit plan (Methodology Template §4):**

After M3 ships, produce a parity table entry in this doc analogous to the
M2 one. Expected findings based on code survey so far:

| Artifact | Reference (Qud decompiled) | Predicted classification |
|---|---|---|
| PetGoal (primitive) | `XRL.World.AI.GoalHandlers/Pet.cs` | Verify exists; CoO impl likely simplified |
| GoFetchGoal (primitive) | Not yet surveyed | TBD |
| FleeLocationGoal (primitive) | Not yet surveyed | TBD |
| AIPetterPart | Likely no direct Qud analog | Probably CoO-original pattern |
| AIHoarderPart | `XRL.World.Parts/Hoarder.cs` (if exists) | Verify |
| AIRetrieverPart | Dogged-fetch behavior in Qud? | Verify |
| AIFleeToShrinePart | CoO-original — no Qud shrine concept | **CoO-original** |
| SanctuaryPart | Marker-part pattern is CoO convention | **CoO-original** |
| Shrine blueprint | CoO-original content | **CoO-original** |

The parity audit is a **post-implementation** step — not a blocker for
starting M3 work.


##### M3 Verification checklist
- [ ] AIPetterPart + VillageChild; 1-2 children near Innkeeper
- [ ] AIHoarderPart + Magpie; Shiny tag on gold/gems
- [ ] AIRetrieverPart + PetDog; ThrowItemCommand fires ItemLanded
- [ ] SanctuaryPart + Shrine; AIFleeToShrine on Scribe/Elder
- [ ] Shrine placed in villages during generation
- [ ] All M3 tests green; full suite still passes

##### M3 Post-audit findings (2026-04-23)

Pass 3 of the Comprehensive Audit. Read: `AIPetterPart.cs`,
`AIHoarderPart.cs`, `AIRetrieverPart.cs`, `AIFleeToShrinePart.cs`,
`SanctuaryPart.cs`, `GoFetchGoal.cs`, `FleeLocationGoal.cs`,
`ScenarioMenuItems.cs`, `AIBehaviorPartTests.cs` (M3.1/M3.3 test
sections). Qud references: `AIPetter.cs`, `AIHoarder.cs`,
`AIRetriever.cs`, `AIFleeToShrine.cs`, `GoFetch.cs`,
`FleeLocation.cs`, `Pet.cs`.

**9 findings** — 0 🔴, 2 🟡, 3 🔵, 4 🧪, 0 ⚪.

| # | Sev | Cat | Title | File:line |
|---|-----|-----|-------|-----------|
| M3.A1 | 🟡 | func | SanctuaryPart is pure marker — heal-over-time mechanic never shipped | `SanctuaryPart.cs:9-13`, `Shrine.Physics.Solid=false` in `Objects.json:280-296` |
| M3.A2 | 🟡 | func | AIRetrieverPart doesn't return bone to thrower; "fetch" loop is half-complete | `AIRetrieverPart.cs:115-128` (TODO comment) |
| M3.A3 | 🔵 | bug | FleeLocationGoal's "running for safety" thought sticks after OnPop (no clear) | `FleeLocationGoal.cs:59` (set), no OnPop override |
| M3.A4 | 🔵 | logic | No reservation on fetch targets — two Magpies race, waste motion | `AIHoarderPart.cs` + `AIRetrieverPart.cs` + `GoFetchGoal.cs` (grep for "reserv" → 0 matches) |
| M3.A5 | 🔵 | logic | FleeLocationGoal targets SafeX,SafeY directly; future Solid sanctuary would reproduce HaulPhase-style fail | `FleeLocationGoal.cs:63` |
| M3.A6 | 🧪 | test-integration | No test for two-Magpie race on same GoldCoin | n/a |
| M3.A7 | 🧪 | test-integration | No test for intended "dog fetches, returns bone to owner" loop (pins M3.A2) | n/a |
| M3.A8 | 🧪 | test-playmode | No PlayMode sanity sweep for M3 | n/a |
| M3.A9 | 🧪 | test-manual | M3 scenarios (VillageChildrenPetting, MagpieFetchesGold, PetDogFetchesBone, WoundedScribeFleesToShrine) manual observation pending | scenarios exist + menu-wired |

###### 🟡 M3.A1 (func) — SanctuaryPart has no sanctuary mechanic

**File:** `Assets/Scripts/Gameplay/Settlements/SanctuaryPart.cs:9-13`

SanctuaryPart class body is literally just `public override string Name
=> "Sanctuary";`. The class docstring explicitly states:

> *Pure marker — no behavior of its own. The "HealOverTime" polish
> feature in the M3.3 plan is deferred; when implemented it'll add a
> field here plus a tick handler that regenerates HP for adjacent
> allied creatures. Today a shrine just provides a destination; it
> doesn't heal on arrival.*

The M3.3 plan (visible at `QUD-PARITY.md:1659+`) mentioned sanctuary
mechanics implicitly via the scenario goal. What shipped: shrine is a
flee destination, and an NPC reaching the shrine just stands there.

**Why it matters:** player plays the scenario, sees the wounded
Scribe run to the shrine, expects something to happen (heal, visual
cue, faction cue, timer). Nothing happens. The scenario reads as
"feature is broken" when it's actually "feature was descoped but
not flagged in scenario docs."

**Proposed fix:** one of
- (a) Ship a minimal heal-over-time: at-shrine NPC regenerates 1 HP
  per turn until Max. Add field `HealPerTurn=1` + an `OnTurnEnd`
  handler that scans adjacent cells for allied wounded + heals one
  tick. ~30 min.
- (b) Accept current state, update M3.3 docstring + scenario log to
  say "shrine reached" is the terminal state and no heal yet.

**Severity rationale:** 🟡 because it's an observable design gap in
a shipped feature — the shrine exists in the world and nothing uses
it. Not 🔴 because the sanctuary flee destination works mechanically.

###### 🟡 M3.A2 (func) — AIRetriever doesn't complete the fetch loop

**File:** `Assets/Scripts/Gameplay/AI/AIRetrieverPart.cs:115-128`

AIRetrieverPart pushes `new GoFetchGoal(item, returnHome: false)`
at line 128. `returnHome: false` means GoFetchGoal terminates the
moment the dog picks up the bone — no phase WalkHome, no drop at
thrower. Bone permanently lives in dog's inventory.

The TODO at line 115-127 acknowledges the gap:
> *Real "dog fetches bone to owner" UX wants a third mode — walk to
> an empty cell ADJACENT to the thrower, then DropCommand the fetched
> item there.*

As shipped, "PetDog fetches bone" plays as "Player throws bone, dog
walks to it, bone disappears into dog" — one-shot, no repeat possible
because bone is now in dog's inventory.

**Why it matters:** `PetDogFetchesBone` scenario is misleading —
reads as "this works" in the menu, plays as incomplete-UX in practice.
User would reasonably expect fetch-and-return.

**Proposed fix:** implement the TODO. Extend GoFetchGoal with a
`ReturnToEntity` mode that walks to a passable cell adjacent to a
target entity (tracking the target's current position each tick in
case they move), then calls DropCommand. 2-4 hours + regression tests.

**Severity rationale:** 🟡 because the scenario has the same
"misleading shipped status" property as M3.A1. Not 🔴 because the
mechanical pipeline (AIBehaviorPart consumes ItemLandedEvent, pushes
goal, walks, picks up) works.

###### 🔵 M3.A3 (bug) — FleeLocationGoal leaves "running for safety" sticky

**File:** `Assets/Scripts/Gameplay/AI/Goals/FleeLocationGoal.cs:59`

```csharp
public override void TakeAction()
{
    var pos = CurrentZone.GetEntityPosition(ParentEntity);
    if (pos.x < 0) { FailToParent(); return; }

    Think("running for safety");

    PushChildGoal(new MoveToGoal(SafeX, SafeY, MaxTurns));
}
```

Each TakeAction re-asserts the thought. Good while goal is active.
But no `OnPop` clears it after the goal terminates (reached safe,
MaxTurns exceeded, or HP recovered). After pop, BoredGoal runs;
BoredGoal doesn't Think. Result: `"running for safety"` sticks in
LastThought indefinitely even after the NPC is back home and healed.

Same exact class as the user-reported sticky-`"buried"` bug M5.2
fixed. FleeLocationGoal has no `OnPop` override today.

**Proposed fix:** add:
```csharp
public override void OnPop() { Think(null); }
```
One line. Plus a regression test in the existing M3 flee-to-shrine
test fixture asserting LastThought is null after the goal pops.

**Severity rationale:** 🔵 same reasoning as M4.A2 — mechanical
behavior correct, only inspector readout stale. Elevate to 🟡 if user
reports confusion during playtest.

###### 🔵 M3.A4 (logic) — No fetch-target reservation; concurrent fetches waste motion

**Files:** `AIHoarderPart.cs`, `AIRetrieverPart.cs`, `GoFetchGoal.cs`
— grep for `Reserve|reserv|Claim|claim` returns 0 matches across
all three.

Compare with M5's DisposeOfCorpseGoal which explicitly reserves the
corpse via `SetIntProperty("DepositCorpsesReserve", 50)` to prevent
two Undertakers racing.

For fetch behaviors: two Magpies in the same zone both see the same
Shiny GoldCoin, both push `GoFetchGoal(goldcoin)`, both walk toward
it. First arrives, calls `InventorySystem.Pickup` — succeeds, coin
enters Magpie-A's inventory. Magpie-B's next tick:
`GoFetchGoal.WalkToItem` line 80 `if (itemCell == null) { Pop(); return; }`
— pops gracefully because the coin is no longer in zone (it's in
Magpie-A's inventory).

Race resolves, no crash, no data loss. But Magpie-B's wasted motion
is real — they walked halfway to a coin that was already claimed
by the time they arrived. For a flock scenario this compounds.

**Why it matters:** efficiency + scenario feel. A "flock of magpies
converging on a coin-drop" plays as jittery — multiple fly in, first
grabs, others turn and walk away silently. Not wrong, just flavorless.

**Proposed fix:** add a shared `FetchClaim` IntProperty that
AIHoarder/AIRetriever set at claim time and `GoFetchGoal.OnPop`
clears. Mirrors the M5 reservation pattern. ~30 min + counter-test.

###### 🔵 M3.A5 (logic) — FleeLocationGoal targets cell directly; Solid-sanctuary future trap

**File:** `FleeLocationGoal.cs:63`
```csharp
PushChildGoal(new MoveToGoal(SafeX, SafeY, MaxTurns));
```

The SafeX/SafeY target is passed straight to MoveToGoal. If the
safe-target cell contains a Solid object, MoveToGoal fails.

Current `Shrine` blueprint has `Physics.Solid=false` (`Objects.json:289`),
so MoveToGoal can reach the shrine cell directly. Not a bug today.

A future shrine variant (e.g., "Altar" with `Solid=true` so it can't
be walked on top of) would reproduce the pre-fix HaulPhase bug:
MoveToGoal can't step onto Solid, A* + greedy fail, FleeLocationGoal's
`Failed` propagates via `FailToParent` — NPC stops wherever they
are. And FleeLocationGoal has no corpse-in-inventory concern like
M5.A2 does, so the consequence is just "NPC doesn't reach shrine."

**Proposed fix:** identical to the HaulPhase fix —
`FindPassableCellNearTarget` helper, MoveToGoal targets a neighbor.
Or document `SanctuaryPart` as an invariant: "sanctuary-bearing
entities MUST be non-Solid." Current Shrine already complies; lint
could enforce.

**Severity rationale:** 🔵 because no current content triggers the
bug. Promote to 🟡 if a future sanctuary blueprint needs Solid.

---

#### Milestone M4 — Interior/Exterior cell tagging (Tier C, 3–4 days)

**Status: ✅ Shipped** — 1665/1665 EditMode tests passing (1 pre-existing
unrelated flake: `AsciiFxRendererTests.InputHandler_WaitsForBlockingFx_
UntilRendererFinishes`, passes in isolation).

**Manual playtest:** ⏳ scenario shipped, awaiting user observation.

M4 adds a per-cell `IsInterior` flag, tagged at zone-generation time, plus
two goal handlers (`MoveToInteriorGoal`, `MoveToExteriorGoal`) that BFS to
the nearest reachable cell matching the predicate and push `MoveToGoal` as
a child. Completes Gap B from the Phase 6 audit.

**Commit history** (chronological on `main`):

| SHA | Role | Description |
|---|---|---|
| `7d991ee` | M4.1 | `Cell.IsInterior` + VillageBuilder/dungeon tagging + CellVerifier assertions |
| `5e1571d` | M4.2 | `AIHelpers.FindNearestCellWhere` + MoveToInterior/ExteriorGoal + 13 tests |
| `04e09d5` | docs | Plan-doc update; Qud-parity audit captured in this section |
| `ac9c5cc` | fix-pass | Post-review: 🟡×2 (dungeon-tag coupling, BFS start-cell passability) + 🧪×2 (MaxTurns pin, Think-signal pins) |
| `3e6e3b7` | scenario | `ScribeSeeksShelter.cs` manual-playtest + menu wiring |
| `9781450` | scenario fix | Spawn silently failed on a CompassStone at player+2; switched to `NearPlayer(3,5)` with null-check |
| `fe55380` | scenario fix | Disable Scribe's `Staying=true` so BoredGoal step-5 doesn't round-trip her back to her exterior spawn |
| `f7b9ec3` | **thought UX** | `OnPop` writes terminal thought (`"sheltered"` / `"outside"`) to unstick `LastThought` — user-reported after playtest |

##### M4 Design decisions (with Qud-parity evidence)

Before implementation, we consulted the Qud decompiled source. Findings:

- **Qud uses a zone-level flag, not per-cell**: `Zone.IsInside()` returns
  true based on a zone property or dungeon depth (`Z > 10`). Buildings are
  **pocket-dimension `InteriorZone` objects**, not walls-on-a-grid.
- **Qud's MoveToInterior/MoveToExterior navigate to specific `GameObject`
  targets** (containers with `Interior` part, or `InteriorPortal` for
  exits), not to predicate-matched cells.
- **Qud's callers are vehicle-related** (`AIBarathrumShuttle`,
  `AIVehiclePilot`, `AIPassenger`) — no weather-driven shelter AI.
- **No MaxTurns** on Qud's versions — they loop until Finished() or the
  target becomes null (unreachable / TryEnter fails).

Implementation choices for CoO:

1. **Per-cell `IsInterior` bool** — fits our flat-zone architecture where
   buildings are walls+floor in the same 80×25 grid. Documented as a CoO
   adaptation of Qud's zone-level concept.
2. **BFS cell-predicate search** — no Qud analogue; needed because we
   don't have InteriorPortal GameObjects to target. `MaxSearchRadius=40`.
3. **`MaxTurns=50` safety net** — CoO-native addition. Qud can afford no
   timeout because its pathing target is concrete; our predicate search
   could spin on a broken path without the cap.
4. **Dungeon cells tagged as interior** — mirrors Qud's
   `Zone.IsInside() → Z > 10`. Done in `OverworldZoneManager.OnZoneGenerated`
   for every cell of a `wz > 0` zone.
5. **Weather/curfew triggers deferred** — Qud doesn't wire
   MoveToInterior to weather either; M4 ships only the primitive. Future
   triggers belong to Phase 12 (Calendar) or Phase 17 (Weather).

##### M4 Verification checklist
- [x] `Cell.IsInterior` field on `Cell.cs`
- [x] `VillageBuilder.BuildRoom` tags interior floor cells
- [x] `OverworldZoneManager.MarkDungeonInterior` tags all cells of `wz > 0` zones (extracted from OnZoneGenerated for save/load reusability — fix-pass 🟡 Bug 1)
- [x] `AIHelpers.FindNearestCellWhere` (BFS primitive) — rejects non-passable start cells (fix-pass 🟡 Bug 2)
- [x] `MoveToInteriorGoal` + `MoveToExteriorGoal` — `OnPop` writes terminal thought to unstick LastThought
- [x] `CellVerifier.IsInterior()` / `.IsExterior()` test helpers
- [x] Tests: 2 VillageBuilder interior tagging + 13 goal/BFS + 3 regression pins (MaxTurns mid-journey, Think signals) + 4 OnPop terminal-thought pins = **22 new M4 tests**
- [x] Full suite green (1665/1665, +8 since initial M4 ship)
- [x] Playtest scenario `ScribeSeeksShelter` wired into `Caves Of Ooo > Scenarios > AI Behavior > Scribe Seeks Shelter (M4 MoveToInterior)`
- [ ] Manual playtest observation (awaiting user run)

##### M4 Post-review findings (applied 5.1–5.3 severity-scaled review)

| # | Sev | Title | Status |
|---|---|---|---|
| 1 | 🟡 | Dungeon interior tag never re-applied after `OnZoneGenerated` (future save/load trap) | ✅ Fixed in `ac9c5cc` — extracted `MarkDungeonInterior` helper |
| 2 | 🟡 | `FindNearestCellWhere` traverses non-passable start cell | ✅ Fixed in `ac9c5cc` — early-return null |
| 3 | 🧪 | MaxTurns mid-journey unpinned | ✅ Test added in `ac9c5cc` |
| 4 | 🧪 | `Think("seeking shelter")` / `Think("heading outside")` unpinned | ✅ 3 tests added in `ac9c5cc` |
| 5 | ⚪ | Qud's `Interior.TryEnter` status enum not mirrored | 📝 Noted, deferred — only relevant if we ever port pocket-dim buildings |
| 6 | 🟡 | **Post-playtest finding** — sticky LastThought lingers after goal pops (`"seeking shelter"` after Scribe already arrived) | ✅ Fixed in `f7b9ec3` — OnPop writes terminal thought |

##### Methodology Template compliance

Applied retroactively after initial M4 ship per user request (see the
commit chain `ac9c5cc → fe55380 → f7b9ec3`):

| Template part | Status |
|---|---|
| 1.2 Pre-impl verification sweep | ✅ Every API read before use |
| 1.4 Risk-ordered sub-milestones | ✅ M4.1 (infra) → M4.2 (consumers) → docs → review → scenario |
| 2.1 Hallucination-avoidance | ✅ No unverified API calls |
| 2.2 Commit-message template | ✅ Scoped prefixes + structured bodies |
| 3.1 EditMode unit tests | ✅ 22 tests |
| 3.3 Regression pins | ✅ All findings have accompanying tests |
| 3.4 Counter-check pattern | ✅ Positive + negative tests paired |
| 3.5 PlayMode sanity sweep | ✅ 4 scenarios with Observed/Expected tables (see commit series) |
| 3.6 Manual playtest scenario | ✅ Committed; ⏳ user observation pending |
| 4 Parity audit | ✅ Read Qud's `MoveToInterior.cs`, `MoveToExterior.cs`, `Interior.cs`, `InteriorPortal.cs`, `InteriorZone.cs`, `Zone.IsInside` |
| 5.1–5.3 Post-impl review | ✅ 6 findings logged + fixed |
| 6.1–6.4 Honesty protocols | ✅ Raw Observed/Expected tables, can/cannot-verify bounds stated |
| 7 Unity MCP tooling | ✅ Turn-by-turn live reflection traces |
| 8.4 Post-milestone | ✅ This section

##### M4 Post-audit findings (2026-04-23)

Pass 1 of the Comprehensive Audit. Read: `Cell.cs`, `AIHelpers.cs`,
`MoveToInteriorGoal.cs`, `MoveToExteriorGoal.cs`,
`VillageBuilder.cs`, `OverworldZoneManager.cs`,
`MoveToInteriorExteriorGoalTests.cs`, `VillageBuilderInteriorTests.cs`,
`ScribeSeeksShelter.cs`, `ScenarioMenuItems.cs`,
Qud `MoveToInterior.cs`. Grepped for M4 consumers + test coverage.

**9 findings** — 0 🔴, 1 🟡, 3 🔵, 3 🧪, 2 ⚪.

| # | Sev | Cat | Title | File:line |
|---|-----|-----|-------|-----------|
| M4.A1 | 🟡 | logic | FindNearestCellWhere uses 4-directional BFS; MoveToGoal's A* uses 8-directional | `AIHelpers.cs:443,462` vs `FindPath.cs:19,119` |
| M4.A2 | 🔵 | bug | MoveToInterior/ExteriorGoal OnPop writes sticky terminal thought ("sheltered" / "outside") | `MoveToInteriorGoal.cs:103`, `MoveToExteriorGoal.cs:84` |
| M4.A3 | 🔵 | doc | Doorway cells tagged IsInterior=false by design; tests don't pin this | `VillageBuilder.cs:170-177` |
| M4.A4 | 🧪 | test-unit | MarkDungeonInterior helper has no regression test | `OverworldZoneManager.cs:243` |
| M4.A5 | 🧪 | test-playmode | No PlayMode sanity sweep for the interior-tagging + BFS + MoveTo chain | n/a |
| M4.A6 | 🧪 | test-manual | ScribeSeeksShelter manual observation still ⏳ pending | scenario file exists + menu-wired |
| M4.A7 | ⚪ | parity | Flat-zone IsInterior vs Qud's pocket-dim InteriorZone | documented in §M4 design table |
| M4.A8 | ⚪ | parity | No `Interior.TryEnter` status enum equivalent | scope-pruned in §M4 |
| M4.A9 | 🔵 | test-unit | No test explicitly checks Door cell's IsInterior value | `VillageBuilderInteriorTests.cs` |

###### 🟡 M4.A1 (logic) — FindNearestCellWhere's 4-directional BFS diverges from MoveToGoal's 8-directional A*

**Files:** `Assets/Scripts/Gameplay/AI/AIHelpers.cs:443,462` and `Assets/Scripts/Gameplay/AI/FindPath.cs:19,119`

`FindNearestCellWhere` expands only 4 cardinal neighbors (line 443
iterates `CardinalOffsets`, defined line 462). `FindPath.Search` —
the A* used by every `MoveToGoal` — expands 8 directions (line 19
comment: *"8-directional neighbor offsets (N, NE, E, SE, S, SW, W,
NW)"*, line 119 `for (int dir = 0; dir < 8; dir++)`).

Concrete failure mode: a room whose ONLY entrance is a diagonal
passthrough (unusual but possible in a winding village layout).
`FindNearestCellWhere` can't find the interior cell (its BFS stops
at the diagonal boundary), so `MoveToInteriorGoal.TakeAction`
`FailToParent`s. `MoveToGoal` itself could have pathed there.

**Why it matters:** "NPC stands outside, can't find shelter" even
when the room is actually reachable. Current starting-village layout
uses straight-walled buildings so this rarely fires in practice, but
a future zone with a diagonal-access building would hit it silently.

**Proposed fix:** swap `CardinalOffsets` for an 8-direction array in
the BFS loop. One-line change + a regression test that constructs a
diagonal-access layout. Alternative (documentary): add a docstring
note that `FindNearestCellWhere` can undercount reachable cells
compared to A* and accept the mismatch.

**Severity rationale:** 🟡 not 🔵 because it's a reachability
mismatch between two systems that claim to answer the same question
("is this cell reachable from here?"). Not 🔴 because the current
content layout doesn't expose the bug.

###### 🔵 M4.A2 (bug) — MoveToInterior/ExteriorGoal write sticky terminal thoughts

**Files:** `Assets/Scripts/Gameplay/AI/Goals/MoveToInteriorGoal.cs:103` and `Assets/Scripts/Gameplay/AI/Goals/MoveToExteriorGoal.cs:84`

`OnPop` calls `Think(cell != null && cell.IsInterior ? "sheltered" : null);` and
the Exterior counterpart writes `"outside"`. On success, the thought
sticks indefinitely in `LastThought` because subsequent goals
(`BoredGoal`, `WaitGoal`, `MoveToGoal`, `WanderRandomlyGoal`) never
call `Think()`. Same class as the user-reported sticky-`"buried"`
bug in M5.2 (fixed in commit `941ce1e` by switching DisposeOfCorpseGoal.OnPop to `Think(null)`
unconditionally).

**Why it matters:** Phase 10 inspector shows stale `"sheltered"` for
an NPC who may have since moved, been displaced, or is about to
re-leave the building. The self-documenting comment in
MoveToInteriorGoal.cs:80-99 (the extended OnPop rationale) predates
the M5.2 UX lesson. DisposeOfCorpseGoal's current OnPop docstring
at `DisposeOfCorpseGoal.cs:320-324` explicitly flags this as the
M4 carry-over concern.

**Proposed fix:** change both OnPops to `Think(null);` unconditionally.
Update the 3 existing regression pins in
`MoveToInteriorExteriorGoalTests.cs` that assert `"sheltered"` /
`"outside"` after OnPop to instead assert `null`. Same shape as
commit `941ce1e`.

**Severity rationale:** 🔵 not 🟡 because the mechanical behavior is
correct (NPC sheltered correctly, just the inspector readout is
stale). Promote to 🟡 if user confirms via playtest that the stale
thought is actively confusing (matches the M5.2 escalation path).

###### 🔵 M4.A3 (doc) — Doorway cells tagged IsInterior=false by design, rationale debatable

**File:** `Assets/Scripts/Gameplay/World/Generation/Builders/VillageBuilder.cs:170-177`

Edge cells of a room are always `IsInterior=false` (walls AND door
cells — the BuildRoom loop treats `isEdge` the same regardless of
whether a door is later placed there). Comment at line 170: *"Walls
(the edge branch) stay IsInterior=false so doors at edge positions
naturally read as exterior, matching the intuition that the roof is
above the floor, not the doorway."*

Design argument is defensible but contestable. In weather/shelter
rules, a doorway IS partially under the roof. If a future system
wants "N is under roof" as a gate, door cells will be misclassified.

**Why it matters:** future weather triggers (rain shelter, dusk
curfew) that key off `IsInterior` may behave weirdly at doorways.
Specifically: NPC walking through a door would register as
"transitioned exterior" for one tick, then "interior" the next.

**Proposed fix:** either (a) accept the current design and document
it in Cell.cs's IsInterior xml-doc more prominently; or (b) change
VillageBuilder to tag door cells as IsInterior=true. Needs a design
call — flagging as 🔵 not ⚪ because it has real downstream
consequences.

###### 🧪 M4.A4 (test-unit) — MarkDungeonInterior helper has no regression test

**File:** `Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs:243`

The M4 post-review extracted `MarkDungeonInterior` from inline
code into a named helper specifically so a future save/load path
could re-invoke it (commit `ac9c5cc`, M4 fix-pass 🟡 #1). The
extraction wasn't accompanied by a test. Grep of
`Assets/Tests/EditMode` for `MarkDungeonInterior` returns **zero**
matches.

**Why it matters:** the helper is private-static so refactors don't
get compile-error guardrails. If its signature or behavior changes,
dungeon zones silently stop tagging cells IsInterior=true.

**Proposed fix:** add a test that constructs a `Zone` with `wz > 0`
(via `OverworldZoneManager.OnZoneGenerated`), asserts every cell has
`IsInterior=true` post-call. ~15 lines.

###### 🧪 M4.A5 (test-playmode) — No PlayMode sanity sweep for M4

**Scope:** M4 declared ✅ Shipped without the Template §3.5 sweep.
Unlike M5 (where the sweep caught the 🔴 HaulPhase bug that EditMode
missed), M4 has no live-scene verification that the interior-tagging
+ BFS + MoveToGoal end-to-end chain actually works in a real bootstrap.
M4's regression tests all use hand-constructed zones, not the
real-Objects.json village layout.

**Why it matters:** the M5 precedent is concerning — the live-scene
flow had bugs the unit tests couldn't catch. M4's flow has the same
shape (goal → MoveToGoal → real-village geometry). Risk is analogous.

**Proposed fix:** a 3-phase PlayMode sweep (spawn Passive NPC
outdoor → push MoveToInteriorGoal → advance turns → confirm
`cell.IsInterior=true` at final position + thought transitioned
correctly). Same `execute_code` shape as the M5 sweep. ~30 min.

###### 🧪 M4.A6 (test-manual) — ScribeSeeksShelter manual observation pending

**Scope:** `Assets/Scripts/Scenarios/Custom/ScribeSeeksShelter.cs`

Scenario exists, `[Scenario]`-attributed, menu-wired at
`ScenarioMenuItems.cs:87-89`. Confirmed via grep. But M4 §Status
still carries `⏳ scenario shipped, awaiting user observation` (line
1864 in QUD-PARITY.md). No user-observed playtest report has
landed since M4 shipped.

**Why it matters:** §M4's ✅ Shipped status is partially unverified.
The M5.2 sticky-thought bug surfaced only via playtest (not EditMode
tests). Unplaytested M4 could have analogous-but-undiscovered bugs.

**Proposed fix:** user runs the scenario once and reports Observed
vs Expected (the scenario's ctx.Log documents the expected flow).
OR: convert to a PlayMode sweep per §M4.A5 to mechanically verify.

###### 🔵 M4.A9 (test-unit) — No test explicitly checks Door cell's IsInterior

**File:** `Assets/Tests/EditMode/Gameplay/World/Generation/VillageBuilderInteriorTests.cs`

Existing tests cover StoneFloor (interior) and wall (exterior) cells.
No test targets a Door entity specifically. The design (per §M4.A3)
treats door cells as exterior. If a future VillageBuilder refactor
starts tagging doors as interior (or changes the edge-branch logic),
no test catches the drift.

**Why it matters:** couples to §M4.A3. If A3 is resolved by keeping
doors exterior, this test pins that decision. If A3 is resolved by
flipping doors to interior, this test still gets written but with
the opposite assertion. Either way, the Door case is unpinned today.

**Proposed fix:** add `BuildZone_DoorCell_HasIsInteriorFalse` (or
`_True`, pending A3) to `VillageBuilderInteriorTests.cs`.

---

##### M4 Follow-up opportunities (out of M4 scope)

- **`BrainPart.Passive`-style trigger parts** — e.g. `AISheltererPart` that
  pushes `MoveToInteriorGoal` on a GameEvent (rain, night, fire).
- **`AISheltererPart` equivalent on vehicles** — closer to Qud's actual
  usage pattern (`AIPassenger`-style).
- **Weather system** — rain event would iterate outdoor NPCs and push
  `MoveToInteriorGoal`. Needs Phase 17.
- **Curfew system** — dusk/dawn pressure on NPCs. Needs Phase 12.
- **`Zone.IsInside()` mirror** — for full Qud parity, add a zone-level
  `IsInside` flag that defaults from dungeon depth + a builder override.
  Our current `wz > 0` tag-all-cells approach approximates this but could
  be cleaner as a true zone-level flag with cell-level implying true.

#### Milestone M5 — Corpse system (Tier C, 3–5 days)

**Status: ✅ Shipped + PlayMode-verified** — 1702/1702 EditMode tests,
PlayMode sanity sweep all 11 observations pass. Closes Gap C from the
Phase 6 audit.

**Manual playtest:** ⏳ `SnapjawBurial` scenario shipped, awaiting user
observation (the PlayMode sweep covers the mechanical chain via
`execute_code` but not visual feel).

**Actual outcome vs plan:**
- **Tests:** planned ~23, delivered **37** (7 M5.1 + 10 M5.2 + 10 M5.3 + 1 initial-review death-drop pin + 2 HaulPhase-fix regression pins + 7 CreatureCorpse blueprint pins).
- **Suite growth:** 1665 → 1702 (+37).
- **Sub-milestones:** all three shipped in sequence with zero regressions at each step.
- **PlayMode sweep caught 🔴 production bug missed by EditMode** — HaulPhase targeting the Solid container cell. Fixed in `daba022`. Details under §M5 Post-review findings #10.
- **User playtest caught 🟡 content gap** — only Snapjaw had CorpsePart; all other NPCs lacked corpse drops. Fixed in `87c8400`. Details under #11.
- **Follow-up opportunity surfaced during review:** ClearGoals-on-NPC-death (see §M5 follow-ups); cross-cutting so deferred.

**Commit history** (chronological on `main`):

| SHA | Role | Description |
|---|---|---|
| `33c4be7` | docs | M5 plan section authored (pre-impl) |
| `4eed32a` | M5.1 | `CorpsePart` + `SnapjawCorpse` blueprint + "Died" event wiring + 7 tests |
| `ab28254` | M5.2 | `DisposeOfCorpseGoal` 2-phase state machine + 10 tests |
| `89d2070` | M5.3 | `AIUndertakerPart` + `Graveyard` / `Undertaker` blueprints + `SnapjawBurial` scenario + 10 tests |
| `44182c9` | fix-pass | Initial post-review: 🔴×1 (factory-null log) + 🟡×1 (rename) + 🧪×1 (undertaker-death drop pin) |
| `42ea48d` | docs | M5 post-ship commit-chain + findings table |
| `daba022` | fix-pass | **PlayMode sweep caught** 🔴 HaulPhase targets Solid container cell → corpse stuck in inventory; fix + 2 regression pins |
| `87c8400` | fix-pass | **User playtest caught** 🟡 spawn-village NPCs never dropped corpses; CreatureCorpse blueprint + Creature.CorpsePart + SuppressCorpseDrops on Player/MimicChest + 7 regression pins |

##### M5 Post-review findings (Methodology Template §5.1–5.3)

Independent review pass after the three feature commits. 9 findings
triaged — 3 actionable, 6 dismissed with documented rationale.

| # | Sev | Title | Status |
|---|---|---|---|
| 1 | 🔴 | `CorpsePart` silently drops a corpse on null `factory.CreateEntity` (misconfigured blueprint symptom) | ✅ Fixed in `44182c9` — added `Debug.LogWarning` with parent + target names |
| 3 | 🟡 | `AIUndertakerPart.FindNearestUnclaimedReachableCorpse` name misleading (no actual reachability check) | ✅ Fixed in `44182c9` — renamed + documented; pathfinding left to `DisposeOfCorpseGoal.MaxMoveTries` cap |
| 8 | 🧪 | Test gap: undertaker killed mid-haul | ✅ Added in `44182c9` — pins `DropInventoryOnDeath` contract |
| 2 | ⚪ | Reservation leak from entity pooling | 📝 Dismissed — CoO has no entity pool; IDs are monotonic |
| 4 | ⚪ | Validation order causing NPE | 📝 Dismissed — false alarm; zone null-check is already first |
| 5 | ⚪ | Container-full atomicity race | 📝 Dismissed — CoO is single-threaded; no race window |
| 6 | ⚪ | Shared `TEST_SEED` constant | 📝 Dismissed — cosmetic; `System.Random(0)` is a stable API |
| 7 | ⚪ | `SnapjawBurial` Part mutation | 📝 Dismissed — verified safe; EntityFactory creates fresh Part instances per entity |
| 9 | ⚪ | `GoalHandler.ParentHandler` cycle | 📝 Dismissed — not a leak in .NET GC |
| 10 | 🔴 | **PlayMode-sweep** caught: `HaulPhase` targeted the Solid container cell; MoveToGoal FailsToParent → corpse stuck in inventory forever | ✅ Fixed in `daba022` — `FindPassableCellNearContainer` targets nearest steppable neighbor; steppability predicate tightened to match `PhysicsPart.HandleBeforeMove` (both `"Solid"` tag AND `PhysicsPart.Solid=true`); 2 regression pins |
| 11 | 🟡 | **User-playtest** caught: spawn-village NPCs never dropped corpses — only Snapjaw had `CorpsePart`, the 40+ other creature blueprints had nothing | ✅ Fixed in `87c8400` — `CreatureCorpse` blueprint + `Corpse` part on `Creature` parent + `SuppressCorpseDrops` tag on `Player` / `MimicChest`; 7 regression pins |

##### M5 PlayMode sanity sweep results

Executed against a live `GameBootstrap` + real `Objects.json`, with the
HaulPhase fix (`daba022`) and NPC-corpse fix (`87c8400`) in place.

**Scenario 1 — Happy-path full chain** (all 8 phases pass):

| Phase | Observed | Expected | Verdict |
|---|---|---|---|
| Preflight (8 gates) | `PLAY_ACTIVE=True CORPSE_FACTORY=WIRED Creature_has_Corpse_part_entry=True Villager.CorpseChance=100 .CorpseBlueprint=CreatureCorpse Snapjaw.CorpseChance=70 .CorpseBlueprint=SnapjawCorpse Player.HasSuppressCorpseDrops=True` | All wiring reachable at runtime | ✅ |
| 1a Setup | SJ=(41,7) UT=(44,7) GY=(47,7) | 3 entities on open strip | ✅ |
| 1b Kill → corpse spawn | `SJ_IN_ZONE=False CORPSE_AT=(41,7) BP=SnapjawCorpse` | Corpse at SJ's cell | ✅ |
| 1c AIBored → goal push | `CONSUMED=True TOP=DisposeOfCorpseGoal Reserve=50` | Goal pushed, corpse reserved | ✅ |
| 1d–1e Fetch phase | T1–T3 walk west, `PICKUP_AT_T=4 thought=fetching corpse` | Walks to corpse, picks up | ✅ |
| 1f–1g Haul phase | T5–T9 walk east, `DEPOSIT_AT_T=10 thought=hauling corpse` UT ended (46,6) adjacent to GY(47,7) | Targets neighbor cell, reaches it, deposits | ✅ |
| 1h Terminal | `FINAL_Reserve=0 FINAL_THOUGHT=buried HasDisposeGoal_final=False` | Reservation cleared, "buried" thought, goal popped | ✅ |

**Scenario 2 — Counter-checks** (all 3 rule out vacuous pass):

| Variant | Observed | Expected | Verdict |
|---|---|---|---|
| 2a No graveyard in zone | `UT.GoalCount=0 consumed=False HasDispose=False` | No push | ✅ |
| 2b Undertaker has NoHauling tag | `UT.GoalCount=0 consumed=False HasDispose=False` | No push | ✅ |
| 2c Corpse pre-reserved by another | `UT.GoalCount=0 consumed=False HasDispose=False Reserve=50` (unchanged) | No push, reservation intact | ✅ |

**Honesty bounds:**

*Can script-verify:* Bootstrap wiring, blueprint inheritance resolution,
entity position, corpse-at-cell, goal-stack membership at each tick,
inventory contents, container contents, reservation int property,
LastThought progression through the state machine.

*Cannot script-verify:* `DeathSplatterFx` particle emission, A* visual
smoothness, glyph rendering (`%` in red), "feel" of the sequence,
player-swings-sword kill path (the sweep uses scripted
`HandleDeath` to isolate the pipeline from combat-resolution variables).

##### Methodology Template compliance

Applied end-to-end (not retroactively, unlike M4):

| Template part | Status |
|---|---|
| 1.2 Pre-impl verification sweep | ✅ Every CoO API read before use — see §M5.1–M5.3 design tables |
| 1.4 Risk-ordered sub-milestones | ✅ M5.1 (spawner, no AI) → M5.2 (goal, testable alone) → M5.3 (behavior + blueprints + scenario) |
| 2.1 Hallucination avoidance | ✅ No unverified APIs |
| 2.2 Commit-message template | ✅ Scoped prefixes (`feat(entities)`, `feat(ai)`, `fix-pass(ai)`) + structured bodies with test-count deltas |
| 3.1 EditMode unit tests | ✅ 28 tests across 3 new test files |
| 3.3 Regression pins | ✅ Hook-ordering, try-counter caps, reservation lifecycle, death-drop contract all pinned |
| 3.4 Counter-check pattern | ✅ Positive paths paired with negatives (no-corpse, no-graveyard, reserved, overburden, NoHauling, idempotency) |
| 3.5 PlayMode sanity sweep | ✅ Executed against live Bootstrap + real Objects.json; 8-phase happy-path + 3 counter-checks all pass with raw Observed/Expected tables above. **Sweep caught 🔴 HaulPhase-Solid-target bug** that EditMode missed. |
| 3.6 Manual playtest scenario | ✅ `SnapjawBurial` committed; ⏳ user observation pending |
| 4 Parity audit | ✅ Read Qud's `Corpse.cs`, `DisposeOfCorpse.cs`, `DepositCorpses.cs` before design |
| 5.1–5.3 Post-impl review | ✅ 11 findings logged across initial-review + PlayMode-sweep + user-playtest passes, 5 fixed, 6 dismissed with rationale |
| 6.1–6.4 Honesty protocols | ✅ Raw test-count deltas, documented-known-limitations (reservation-on-death leak); can-verify/cannot-verify bounds in PlayMode sweep report |
| 7 Unity MCP tooling | ✅ `run_tests` + `read_console` + `manage_editor play/stop` + `execute_code` used; PlayMode sweep exercised the full loop including live entity-inspection |
| 8.4 Post-milestone | ✅ This section |

M5 adds:
- A **`CorpsePart`** spawner (a Part on living creatures) that listens to
  the `"Died"` event in `CombatSystem.HandleDeath` and drops a corpse
  entity at the deceased's cell.
- A **`DisposeOfCorpseGoal`** — a 4-case carry-and-deposit state machine
  ported from Qud's `XRL.World.AI.GoalHandlers.DisposeOfCorpse`.
- An **`AIUndertakerPart`** — a new `AIBehaviorPart` subclass that
  responds to `AIBoredEvent`, finds a corpse + graveyard, reserves the
  corpse, and pushes `DisposeOfCorpseGoal`.
- Two new blueprints — **`SnapjawCorpse`** (the corpse entity) and
  **`Graveyard`** (unlimited-capacity container). One new NPC blueprint,
  **`Undertaker`**.
- A manual-playtest scenario **`SnapjawBurial`** — player + snapjaw +
  undertaker + graveyard; kill the snapjaw, watch the undertaker haul
  the corpse.

Downstream M5 unlocks:
- Necromancy (reanimate Corpse-tagged entities) — Phase 16
- Butchering recipes (SnapjawCorpse → RawMeat) — Phase 14
- Corpse decay → bones via existing `LifespanPart` — trivial follow-up

##### Sub-milestone breakdown

**M5.1 — CorpsePart + `SnapjawCorpse` blueprint + death hook** (~1.5 days, ~6 tests)

New files:
- `Assets/Scripts/Gameplay/Entities/CorpsePart.cs` — ports Qud's `Corpse` Part
- `Assets/Tests/EditMode/Gameplay/Entities/CorpsePartTests.cs`
- New blueprint entry in `Assets/Resources/Content/Blueprints/Objects.json`:
  `SnapjawCorpse`

Behavior:
- `CorpsePart` is a Part on **living creatures**, not an entity type — it's
  a **spawner**, mirroring Qud's architecture (`XRL.World.Parts.Corpse`
  lines 9–202).
- Fields: `CorpseChance` (int 0–100), `CorpseBlueprint` (string),
  `BuildCorpseChance` (int, default 100), `SuppressCorpseDrops` tag check.
- Burnt/Vaporized variants **deferred** — CoO has no `LastDamagedByType` on
  `PhysicsPart` (grep confirmed no matches). Adding damage-type tracking is
  M9's scope; single-variant ships in M5.
- Hook: listens for `"Died"` fired in
  `Assets/Scripts/Gameplay/Combat/CombatSystem.cs:451`. The hook point is
  valid — Died fires after equipment drop (lines 435–445) and before
  `zone.RemoveEntity(target)` (line 465), so the deceased's cell is still
  resolvable via `zone.GetEntityCell(target)`.
- On Died handler:
  1. Skip if parent has `SuppressCorpseDrops` tag.
  2. Skip if `Rng.Next(100) >= CorpseChance`.
  3. Spawn `CorpseBlueprint` entity at the deceased's cell via `EntityFactory`.
  4. Copy properties mirroring Qud's ProcessCorpseDrop (lines 138–163):
     - `CreatureName` = parent's DisplayName
     - `SourceBlueprint` = parent's BlueprintName
     - `SourceID` = parent's ID (if HasID)
     - `KillerID` = killer's ID (if killer != null && killer != parent)
     - `KillerBlueprint` = killer's BlueprintName (if applicable)

Blueprint `SnapjawCorpse`:
- Render: `"%"` (standard roguelike corpse glyph), color `&r` (red)
- Physics: `Solid=false` (walk-over), `Weight=10` (carry-friendly for Str-8+ NPCs)
- Tags: `Corpse` (for the `AIUndertakerPart` finder), `Organic` material
- `Examinable`: formatted "the corpse of a {SourceBlueprint}"

`Snapjaw` blueprint gets a new Part entry:
```json
{
  "Name": "Corpse",
  "Params": [
    { "Key": "CorpseChance", "Value": "70" },
    { "Key": "CorpseBlueprint", "Value": "SnapjawCorpse" }
  ]
}
```

Tests (6):
- Snapjaw dies → CorpseBlueprint spawns at its cell (deterministic RNG)
- CorpseChance=0 → no spawn
- SuppressCorpseDrops tag → no spawn
- Spawned corpse has CreatureName, SourceBlueprint
- Killer's ID propagated to corpse when Killer has HasID
- Spawn cell resolvable before `zone.RemoveEntity` (pins the hook-ordering)

**M5.2 — DisposeOfCorpseGoal (carry-and-deposit state machine)** (~1 day, ~9 tests)

New files:
- `Assets/Scripts/Gameplay/AI/Goals/DisposeOfCorpseGoal.cs` — ports Qud's
  `XRL.World.AI.GoalHandlers.DisposeOfCorpse` (lines 6–90)
- `Assets/Tests/EditMode/Gameplay/AI/DisposeOfCorpseGoalTests.cs`

Constructor: `DisposeOfCorpseGoal(Entity corpse, Entity container)` — both
injected. Internal state: `Done` bool, `GoToCorpseTries` int,
`GoToContainerTries` int.

4-case state machine in `TakeAction()` (ports Qud DisposeOfCorpse.cs
lines 41–89):

| Case | Condition | Action |
|---|---|---|
| A | `corpse` or `container` null/different zone | `FailToParent()` |
| B | Carrying corpse && adjacent to container | Try `container.ContainerPart.AddItem(corpse)`; on failure drop at feet (`PerformDrop` event); `Done = true` |
| C | Carrying corpse && !adjacent | `PushChildGoal(new MoveToGoal(containerX, containerY, remainingTurns))`; `GoToContainerTries++`; if > 10 → drop at feet, `Done = true` |
| D | !Carrying && adjacent to corpse | `new PickupCommand(corpse).Execute(...)`; on failure `FailToParent()` |
| E | !Carrying && !adjacent | `PushChildGoal(new MoveToGoal(corpseX, corpseY, remainingTurns))`; `GoToCorpseTries++`; if > 10 → `Done = true` |

Each tick re-validates:
- `corpse` still exists in NPC's inventory OR in a cell in the same zone
- `container` still exists in the same zone
- NPC in same zone as both

`Think()` narrative signals (mirrors M4 pattern):
- Phase B/D → `"arrived"` (transitional, next tick handles transfer)
- Phase C → `"hauling corpse"`
- Phase E → `"fetching corpse"`
- `OnPop` writes terminal thought: `"buried"` on success, clears on failure

Tests (9):
- All 5 cases reached independently (A failure, B transfer, C hauling, D pickup, E fetch)
- Corpse destroyed mid-haul → FailToParent (case A)
- Container full → corpse dropped at NPC's feet (fallback in case B)
- 11 tries to reach container → dropped at feet (case C cap)
- 11 tries to reach corpse → Done without failure (case E cap, matches Qud line 87–88)
- OnPop writes `"buried"` on success

**M5.3 — AIUndertakerPart + Graveyard blueprint + Undertaker NPC** (~1.5 days, ~8 tests)

New files:
- `Assets/Scripts/Gameplay/AI/AIUndertakerPart.cs` — `AIBehaviorPart` subclass
- `Assets/Tests/EditMode/Gameplay/AI/AIUndertakerPartTests.cs`
- `Assets/Scripts/Scenarios/Custom/SnapjawBurial.cs` — manual-playtest showcase

Blueprint additions in `Objects.json`:
- `Graveyard` — ContainerPart(`MaxItems=-1`, unlimited), `Solid=true`,
  `Render='†'`, tag `Graveyard`. Single-cell entity; scenario spawns one
  for now (world-generation hook is out of M5 scope).
- `Undertaker` — inherits `Villager`. Adds `AIUndertakerPart`. Render
  `'U'` color `&k` (black), DisplayName "undertaker".

`AIUndertakerPart` fields (Qud-parity naming; see Qud's `DepositCorpses`
lines 12–16):
- `Chance = 100` — always try when bored, matching Qud's deterministic
  idle hijack (`AIPetterPart` uses 3% for cosmetic behavior; undertaking
  is a job, not a whim)
- `MaxNavigationWeight = 30` — Qud-parity; rejects unreachable corpses
- `OwnersOnlyIfOwned = true` — respect graveyard ownership (can be turned
  off on public graveyards)

`HandleEvent(AIBoredEvent)` flow (mirrors `AIPetterPart.HandleBored`
structure):
1. Idempotency: if brain already has `DisposeOfCorpseGoal` → `return true`
2. Find nearest `Graveyard`-tagged container in same zone
3. Find all `Corpse`-tagged entities in same zone; filter out those with
   active `DepositCorpsesReserve` property
4. Pick nearest unclaimed corpse (Chebyshev distance)
5. Check NPC's `InventoryPart.WouldBeOverburdened(corpse)` via
   `GetCarriedWeight() + corpseWeight <= GetMaxCarryWeight()` guard
6. Claim: `corpse.SetIntProperty("DepositCorpsesReserve", 50)` — mirrors
   Qud's `Corpse.cs` ProcessCorpseDrop line 115
7. `brain.PushGoal(new DisposeOfCorpseGoal(corpse, graveyard))`;
   `e.Handled = true`; `return false`

**Reservation lifecycle:** `DisposeOfCorpseGoal.OnPop` clears the property.
Qud decrements each frame — simpler to clear on goal-pop since CoO goals
have clean Pop lifecycle. Test explicitly.

Tests (8):
- AIUndertaker with graveyard + corpse + Bored → pushes DisposeOfCorpseGoal
- No graveyard → no push (no-op)
- No corpse → no push (no-op)
- Two Undertakers + one corpse → only one claims (reservation works)
- Overburdened NPC → skipped (no push)
- NoHauling tag → skipped (parity with Qud line 75)
- Blueprint wiring: `Undertaker` blueprint produces entity with AIUndertakerPart
- Blueprint wiring: `Graveyard` blueprint produces entity with ContainerPart(MaxItems=-1) + Graveyard tag

##### M5 Design decisions (with Qud-parity evidence)

Read:
- `XRL.World.Parts.Corpse` (Corpse spawner Part)
- `XRL.World.AI.GoalHandlers.DisposeOfCorpse` (goal state machine)
- `XRL.World.Parts.DepositCorpses` (Undertaker behavior dispatcher)

| Question | Qud answer | CoO choice | Rationale |
|---|---|---|---|
| Is Corpse a Part or an entity? | **Part** on living creatures (spawner) | Same | Preserves Qud's architecture; mirrors CoO's existing Part-as-behavior-attachment convention. |
| Death hook point | `BeforeDeathRemovalEvent` (typed event) | `GameEvent.New("Died")` in `CombatSystem.HandleDeath:451` | No typed event infra in CoO yet — the string-ID event fires at the equivalent point (post equipment-drop, pre zone-removal). |
| Burnt / Vaporized variants | Via `Physics.LastDamagedByType` (Corpse.cs lines 115–128) | **Deferred** to M9 (damage-type system) | `grep` confirmed no `LastDamagedByType` in CoO. Shipping base variant avoids guessing damage-type API. |
| Goal architecture | 4-case state machine with `GoToCorpseTries` / `GoToContainerTries` capped at 10 | Same, ported directly | No architectural friction; CoO has `MoveToGoal` with a turn-budget analog. |
| Corpse reservation (anti-race) | `SetIntProperty("DepositeCorpsesReserve", 50)`, `ModIntProperty(..., -1)` each frame | `SetIntProperty("DepositCorpsesReserve", 50)`, cleared on `DisposeOfCorpseGoal.OnPop` | CoO's goal lifecycle gives a clean clear point; avoids per-frame decrement overhead. |
| Undertaker dispatch: container-side or NPC-side? | **Container-side** — `DepositCorpses` IActivePart on the Graveyard handles `IdleQueryEvent` and pushes the goal onto the bored NPC (`DepositCorpses.cs:50–57`) | **NPC-side** — `AIUndertakerPart : AIBehaviorPart` on the NPC handles `AIBoredEvent` | CoO's `IdleQueryEvent` is structured for furniture-offer (TargetX/Y/Action/Cleanup), not NPC-hijacking. NPC-side dispatch mirrors the existing `AIPetterPart` / `AIGuardPart` / `AIFleeToShrinePart` convention. Documented adaptation (same category as M4's per-cell vs zone-level IsInterior). |
| Graveyard capacity | Unlimited (zone-level container in Qud) | `ContainerPart(MaxItems=-1)` | CoO's ContainerPart supports -1 as unlimited; matches Qud's semantics. |
| Container-full fallback | `PerformDrop` event at NPC's feet | Same — fire `PerformDrop` event; Done=true | Ports directly. |
| `NoHauling` tag | Parity check on actor (`DepositCorpses.cs:75–78`) | Same — skip NPCs with `NoHauling` tag | Ports directly; tag gets documented in new blueprint comment. |
| Cybernetics extraction | Collected into `CyberneticsButcherableCybernetic` | **Deferred** — no cybernetics in CoO yet (Phase 15) | Noted in follow-ups. |

##### M5 Verification checklist (targets)

- [ ] `CorpsePart.cs` — listens to "Died", gates on CorpseChance,
  writes CreatureName/SourceBlueprint/KillerID properties
- [ ] `SnapjawCorpse` blueprint — renderable, walkable, `Corpse` tag, `Organic` material
- [ ] `DisposeOfCorpseGoal.cs` — 4-case state machine, try-counter caps,
  OnPop clears reservation + writes terminal thought
- [ ] `AIUndertakerPart.cs` — AIBehaviorPart pattern, reservation,
  overburden check, NoHauling skip
- [ ] `Graveyard` blueprint — ContainerPart(MaxItems=-1), Solid, `Graveyard` tag
- [ ] `Undertaker` NPC blueprint — inherits Villager + AIUndertakerPart
- [ ] `Snapjaw` blueprint — CorpsePart with CorpseChance=70
- [ ] Tests: 6 (M5.1) + 9 (M5.2) + 8 (M5.3) = **~23 new M5 tests**
- [ ] Full suite green (1665 → ~1688)
- [x] Playtest scenario `SnapjawBurial` wired into `Caves Of Ooo > Scenarios > AI Behavior > Snapjaw Burial (M5 Corpse system)` (menu entry added retroactively in `3d7e298` — the initial M5.3 commit shipped the `IScenario` class but forgot the `[MenuItem]` entry in `ScenarioMenuItems.cs`)
- [ ] Manual playtest observation (awaiting user run)

##### M5 Risks & mitigations

| # | Risk | Likelihood | Mitigation |
|---|---|---|---|
| 1 | `"Died"` event fires after `zone.RemoveEntity` → cell lookup fails | Low (verified line 451 < line 465) | Pin with a regression test: assert `zone.GetEntityCell(target)` is non-null at the moment `CorpsePart` reads it |
| 2 | Corpse weight > NPC max carry → Undertaker can't haul | Medium | Weight=10 keeps Str-8+ NPCs OK; pin with overburden-skip test (`Str=4` Undertaker + Weight=100 corpse → no push) |
| 3 | Two Undertakers race same corpse | Medium | Reservation via `DepositCorpsesReserve` int property; test explicitly with 2-NPC zone |
| 4 | Container full → silent corpse loss | Medium | `ContainerPart.AddItem` returning false triggers `PerformDrop` at feet (Qud-parity); test explicitly |
| 5 | Goal loop: dispose → arrive → pop → bored → push again → infinite | Low | `AIUndertakerPart.HandleEvent` checks `brain.HasGoal<DisposeOfCorpseGoal>()` before pushing (mirrors `AIPetterPart.HandleBored` line 49–50) |
| 6 | Circular blueprint dependency (Graveyard references AIUndertakerPart references Graveyard) | None | AIUndertakerPart searches by tag/ContainerPart presence, not by blueprint name |
| 7 | CombatSystem hot-path slow-down from entity spawn | Low | CoO already drops equipment + emits splatter fx in HandleDeath; one more spawn is symmetric |
| 8 | Reservation never cleared (NPC dies mid-haul, corpse stays claimed forever) | Medium | `DisposeOfCorpseGoal.OnPop` clears the property unconditionally; also add `CorpsePart.OnZoneActivate` sweep to clear stale reservations (Qud-parity-plus safety) |

##### M5 Post-audit findings (2026-04-23)

Pass 2 of the Comprehensive Audit. Read: `CorpsePart.cs`,
`DisposeOfCorpseGoal.cs`, `AIUndertakerPart.cs`, `SnapjawBurial.cs`,
`CorpsePartTests.cs`, `DisposeOfCorpseGoalTests.cs`,
`AIUndertakerPartTests.cs`, `ScenarioMenuItems.cs`, Qud `Corpse.cs` /
`DisposeOfCorpse.cs` / `DepositCorpses.cs`. Cross-checked
StackerPart.CanStackWith semantics against inventory / container
flows.

**12 findings** — 0 🔴, 3 🟡, 3 🔵, 4 🧪, 2 ⚪.

| # | Sev | Cat | Title | File:line |
|---|-----|-----|-------|-----------|
| M5.A1 | 🟡 | bug | CreatureCorpse entities stack ignoring CreatureName — defeats DisplayName interpolation | `StackerPart` + `InventoryPart.AddObject` + `ContainerPart.AddItem` |
| M5.A2 | 🟡 | bug | DisposeOfCorpseGoal.Failed doesn't drop carried corpse; orphaned in inventory forever | `DisposeOfCorpseGoal.cs:290-298` |
| M5.A3 | 🟡 | wiring | No world-gen places Graveyard in villages — Undertaker inert outside scenarios | `VillageBuilder.cs` (absence grep), `Objects.json` (Graveyard placed only by scenario) |
| M5.A4 | 🔵 | logic | Interpolation gate `render.DisplayName == "corpse"` is case-sensitive | `CorpsePart.cs:207` |
| M5.A5 | 🔵 | logic | FetchPhase targets corpse cell directly; a future Solid-corpse would reproduce the HaulPhase bug pre-fix | `DisposeOfCorpseGoal.cs:143` |
| M5.A6 | 🔵 | logic | Fallback `GetDisplayName()` returns BlueprintName with capital-S → `"Snapjaw corpse"` for creatures without a Render.DisplayName | `Entity.cs:391-405` + `CorpsePart.cs:171,209` |
| M5.A7 | 🧪 | test-unit | Factory-null `Debug.LogWarning` (fix-pass #1) has no regression test | `CorpsePart.cs:160-163` |
| M5.A8 | 🧪 | test-integration | No test for the sit → AIUndertaker stand → haul integration path (from `04dca03` sit-fix) | n/a |
| M5.A9 | 🧪 | test-unit | No test covers `DisposeOfCorpseGoal.Failed` while carrying — pins M5.A2 | n/a |
| M5.A10 | 🧪 | test-manual | SnapjawBurial visual-feel manual observation pending (PlayMode sweep covered mechanics only) | scenario file |
| M5.A11 | ⚪ | logic | FindGraveyard returns first match, not nearest | `AIUndertakerPart.cs:107-118` — documented |
| M5.A12 | ⚪ | logic | FindNearestUnclaimedCorpse uses Chebyshev, not reachability | `AIUndertakerPart.cs:120-164` — documented |

**Cross-milestone pointer**: the M5 commit body documents a known
ClearGoals-on-NPC-death leak affecting DisposeOfCorpseGoal's
`DepositCorpsesReserve` cleanup. That finding lives in the
cross-milestone section — see Cross-milestone concern CM-1.

###### 🟡 M5.A1 (bug) — StackerPart merges different-CreatureName corpses

**Files:** `Assets/Scripts/Gameplay/Items/StackerPart.cs:30-40` (CanStackWith),
`Assets/Scripts/Gameplay/Inventory/InventoryPart.cs:51-89` (AddObject
stack-merge), `Assets/Scripts/Gameplay/Items/ContainerPart.cs:30-79`
(AddItem stack-merge).

`StackerPart.CanStackWith` returns true when both entities have a
StackerPart and share the same `BlueprintName`. `Villager` kill
produces a CreatureCorpse with `CreatureName="villager"` and
interpolated DisplayName `"villager corpse"`; `Scribe` kill produces
another CreatureCorpse with `CreatureName="scribe"` and DisplayName
`"scribe corpse"`. Both have `BlueprintName="CreatureCorpse"` → they
stack. Player picks them up and the inventory shows
`"scribe corpse (x2)"` (or whichever the stack-leader was) — one of
the corpses loses its identity.

This directly contradicts the M5 `59e1674` commit which shipped
DisplayName interpolation specifically to distinguish corpse types
in the UI.

**Why it matters:** two villagers killed in the same cell → looks
like one corpse in the UI with count=2. Butchering / burial flavor
(future Phase 14) reads the wrong `CreatureName`. Reversible but
quietly destructive — the minority entity's `SourceID`, `KillerID`,
and any other Properties get discarded by MergeFrom.

**Proposed fix:** extend `StackerPart.CanStackWith` to check
equality of a declarable set of Properties (e.g., a new field
`StackByProperties: string[]` defaulting to `[]`). `CreatureCorpse`
blueprint sets `StackByProperties="CreatureName,SourceBlueprint"` so
only identical-creature corpses stack. Alternative (simpler but
loses stacking entirely for corpses): add a `NoStack` tag to
`CreatureCorpse` and short-circuit `CanStackWith` on either party's
`NoStack`.

**Severity rationale:** 🟡 because it's observable player-facing
incorrectness (inventory shows wrong data), not just an edge case.
Not 🔴 because reproduction requires two corpses to land in the
same cell, which is uncommon in normal play.

###### 🟡 M5.A2 (bug) — DisposeOfCorpseGoal.Failed orphans carried corpse

**File:** `Assets/Scripts/Gameplay/AI/Goals/DisposeOfCorpseGoal.cs:290-298`

```csharp
public override void Failed(GoalHandler child)
{
    FailToParent();
}
```

When a child MoveToGoal hits its explicit failure path (A* AND
greedy fallback both fail), it calls `FailToParent()`. Our
`DisposeOfCorpseGoal.Failed(child)` cascades that up by calling
`FailToParent()` on itself. That pops the goal and runs `OnPop`
(lines 300+) which clears the corpse's `DepositCorpsesReserve`
property and `LastThought` — but **does not drop the carried corpse
back to the zone**.

Downstream consequence traced end-to-end:
1. Goal pops, `_done=false`, NPC still has corpse in `InventoryPart.Objects`.
2. Corpse is removed from `zone._entityCells` (happened at pickup
   via `PickupCommand.Execute`) → no longer visible in
   `zone.GetReadOnlyEntities()`.
3. AIUndertakerPart's next bored tick calls
   `FindNearestUnclaimedCorpse(brain.CurrentZone, ...)` which scans
   zone entities. The corpse isn't in the zone, so the scan misses
   it. `FindNearestUnclaimedCorpse` returns null. No new
   `DisposeOfCorpseGoal` pushed.
4. NPC walks around with corpse in inventory forever. No
   mechanism re-engages.

**Why it matters:** the Undertaker pipeline silently loses a corpse
to NPC inventory if pathing fails mid-haul. The MaxMoveTries cap in
`HaulPhase` only fires on counter overflow (10 tries before
drop-at-feet). The `Failed` cascade fires on any A*-and-greedy-both-
fail tick, which for a broken/walled-in path happens immediately.

**Proposed fix:** before calling `FailToParent()` in
`Failed(GoalHandler child)`, drop the corpse at NPC's feet via
`InventorySystem.Drop(ParentEntity, Corpse, CurrentZone)` if the
NPC is carrying it:

```csharp
public override void Failed(GoalHandler child)
{
    var inv = ParentEntity?.GetPart<InventoryPart>();
    if (inv != null && Corpse != null && inv.Contains(Corpse))
        InventorySystem.Drop(ParentEntity, Corpse, CurrentZone);
    FailToParent();
}
```

Plus a regression test that forces MoveToGoal failure mid-haul
(walls every neighbor of a Graveyard mid-carry) and asserts the
corpse ends up in zone at NPC's feet, not stuck in inventory.

**Severity rationale:** 🟡 because it's a quiet data-loss path the
user would see as "Undertaker never finishes a burial despite
trying." Not 🔴 because the trigger (A* and greedy both failing) is
rare in the starting village layout. Elevate to 🔴 if a user reports
the symptom in play.

###### 🟡 M5.A3 (wiring) — No world-gen places Graveyard → Undertaker inert outside scenarios

**Files:** `Assets/Scripts/Gameplay/World/Generation/Builders/VillageBuilder.cs`
(grep for `"Graveyard"` → 0 matches inside the builder),
`Assets/Resources/Content/Blueprints/Objects.json` (Graveyard
blueprint exists but no world-gen references it).

The `AIUndertakerPart` filter at `AIUndertakerPart.cs:87-89` requires
a Graveyard-tagged entity in the zone:
```csharp
Entity graveyard = FindGraveyard(brain.CurrentZone);
if (graveyard == null) return true;
```

In the shipped starting village, no Graveyard is placed. The
Undertaker blueprint itself isn't placed either (no
`PlaceNPCInInterior("Undertaker", ...)` call in VillagePopulationBuilder
— grep confirms). So the Undertaker behavior only runs via the
`SnapjawBurial` scenario or a future world-gen addition.

**Why it matters:** the M5 section (1975+) reads as if Undertakers
exist in villages. They don't. Player playing the game — not a
scenario — will never encounter an Undertaker. The entire M5.3
gameplay surface is scenario-only content for now.

**Proposed fix:** extend `VillagePopulationBuilder` to
(a) place one Graveyard at a plausible cell (village edge, near a
path) and (b) place one Undertaker NPC associated with it. Or scope
as intentional follow-up and update the M5 doc's status to
"shipped; world-gen placement deferred" for honesty.

**Severity rationale:** 🟡 because the gameplay-visible feature
doesn't work in normal play. Not 🔴 because the mechanics all work;
only the population hook is missing. The M5 docs don't claim
world-gen placement was in scope (the follow-ups list mentions it
explicitly), so this is scope-honesty not functional regression.

###### 🔵 M5.A4 (logic) — DisplayName interpolation case-sensitive

**File:** `CorpsePart.cs:205-209`

```csharp
if (render != null
    && !string.IsNullOrEmpty(creatureName)
    && render.DisplayName == "corpse")
{
    render.DisplayName = $"{creatureName} corpse";
}
```

The gate compares exactly to lowercase `"corpse"`. If a future
corpse blueprint accidentally uses `"Corpse"` or `" corpse"` as its
authored DisplayName, the interpolation won't fire and the corpse
displays as-authored. No bug today (CreatureCorpse is lowercase) but
brittle.

**Proposed fix:** use `string.Equals(render.DisplayName, "corpse",
StringComparison.OrdinalIgnoreCase)` plus `.Trim()` on the
blueprint-loaded string. Or accept the brittleness and
document it in a code comment (already partially documented).

###### 🔵 M5.A5 (logic) — FetchPhase targets corpse cell directly

**File:** `DisposeOfCorpseGoal.cs:143`

```csharp
PushChildGoal(new MoveToGoal(corpseCell.X, corpseCell.Y, ChildMoveMaxTurns));
```

Symmetric to the HaulPhase bug that PlayMode-sweep caught. Currently
safe because `SnapjawCorpse`/`CreatureCorpse` both have
`Physics.Solid=false` → MoveToGoal reaches the corpse cell fine. A
future corpse blueprint with `Solid=true` (e.g., a "frozen bandit
corpse" acting as a temporary barrier) would reproduce the
"MoveToGoal fails because target is solid" bug.

**Proposed fix:** mirror HaulPhase's `FindPassableCellNearContainer`
with a symmetric `FindPassableCellNearCorpse` — OR document the
invariant ("corpse blueprints must be non-Solid") in
`CorpsePart.CorpseBlueprint` docstring and a blueprint-validation
lint.

**Severity rationale:** 🔵 because no current content violates the
invariant. Promote to 🟡 if a future corpse blueprint needs Solid.

###### 🔵 M5.A6 (logic) — Empty-DisplayName fallback produces capital-letter interpolation

**Files:** `Entity.cs:391-405` (GetDisplayName fallback),
`CorpsePart.cs:171` (creatureName = GetDisplayName),
`CorpsePart.cs:209` (interpolation)

`Entity.GetDisplayName()` returns `RenderPart.DisplayName` when set,
else falls back to `BlueprintName ?? ID ?? "unknown"`. A creature
blueprint that has a RenderPart but leaves `DisplayName` empty
(common mistake) would produce `GetDisplayName() = "Snapjaw"` (the
BlueprintName, capital S). The interpolation then writes
`"Snapjaw corpse"` — capital letter leaks into the player-visible
string.

No current blueprint triggers this (every Creature-derived has
non-empty DisplayName per `VillageBuilderInteriorTests` setup and
`AsciiWorldRenderPolicy` validation), but a new blueprint without
DisplayName would silently look wrong.

**Proposed fix:** lowercase the string at interpolation time:
`render.DisplayName = $"{creatureName.ToLowerInvariant()} corpse";`.
Keep the property (`CreatureName`) in its original case so consumers
that want proper capitalization ("the corpse of a {CreatureName}")
can do their own formatting.

###### 🧪 M5.A7 (test-unit) — Factory-null log warning not tested

**File:** `CorpsePart.cs:160-163`

Fix-pass #1 in the M5 post-review added a `Debug.LogWarning` when
`factory.CreateEntity(CorpseBlueprint)` returns null (misconfigured
blueprint). No test asserts the warning actually fires. If the log
call is accidentally deleted or changed to the wrong level, no test
catches it.

**Proposed fix:** use `LogAssert.Expect(LogType.Warning, "...")` in
a new test `CorpsePart_LogsWarning_WhenCorpseBlueprintUnknown` that
sets `CorpseBlueprint="NonexistentBlueprint"` and fires Died.

###### 🧪 M5.A8 (test-integration) — Sit → stand → haul integration path

**Scope:** `04dca03` sit-fix makes `BoredGoal.Step 1` fire
AIBoredEvent while sitting, and an `AIBoredEvent` consumer
(including `AIUndertakerPart`) can stand the NPC up. No test exercises
the full path: seated Undertaker + corpse in zone → TakeTurn →
stands up → DisposeOfCorpseGoal pushed.

The existing BoredGoal sit-regression (IdleQueryTests) uses a generic
`TestAIBoredConsumer`. Not an Undertaker-specific test.

**Proposed fix:** add an `AIUndertakerPartTests` test that applies
SittingEffect, places corpse+graveyard, fires TakeTurn on the
Undertaker, and asserts: `SittingEffect` removed, `DisposeOfCorpseGoal`
on stack, corpse `DepositCorpsesReserve=50`.

###### 🧪 M5.A9 (test-unit) — No test covers Failed-while-carrying

**Scope:** pins M5.A2. Current `DisposeOfCorpseGoalTests` has
`HaulPhase_ExhaustedTries_DropsAtFeetAndSetsDone` for the MaxMoveTries
cap path, but nothing for the
`MoveToGoal.FailToParent → DisposeOfCorpseGoal.Failed → orphaned`
path.

**Proposed fix:** fix-pass commit for M5.A2 MUST include a regression
test: construct a scenario where the MoveToGoal will fail its A* AND
greedy step (e.g., wall-in every neighbor so the greedy step fails),
assert after one tick the corpse is in zone at NPC's feet and the
goal is popped.

###### 🧪 M5.A10 (test-manual) — SnapjawBurial visual-feel observation pending

**Scope:** `SnapjawBurial.cs` is menu-wired and has passed the
PlayMode mechanical sweep (Scenario 1 + Scenario 2 counter-checks
in §M5 PlayMode sweep results). But no user-confirmed visual-feel
pass ("the corpse `%` glyph looks right," "the haul animation reads
clearly," "the graveyard `†` is visible"). Without that pass, the
visual-rendering-layer assumptions in §"Cannot script-verify" of the
PlayMode sweep remain unverified.

**Proposed fix:** user runs the scenario once, confirms Observed vs
Expected on the "can NOT script-verify" list (particle emission on
death, glyph rendering, pathing smoothness), reports back.

---

##### M5 Follow-up opportunities (out of M5 scope)

- **BurntCorpse / VaporizedCorpse variants** — needs `LastDamagedByType` on
  PhysicsPart; ships with M9 (damage types).
- **Butchering recipes** — "butcher SnapjawCorpse → RawMeat" crafting
  action; needs Phase 14 (crafting).
- **Necromancy mutation** — `RaiseDeadMutation` targeting `Corpse`-tagged
  entities; Phase 16.
- **Corpse decay → Bones** — `LifespanPart` (already exists) with a 30-day
  timer that replaces SnapjawCorpse with Bones entity. Trivial post-M5 add.
- **Boneyard / Ossuary zones** — world-gen hook that auto-places a
  Graveyard in every village edge; needs Phase 12.
- **Cybernetics extraction** — Qud's ProcessCorpseDrop collects Cybernetics
  into a butcherable list; needs Phase 15.
- **Player butchering UI** — "examine corpse → butcher" action menu entry;
  plugs into the existing world action menu infrastructure.
- **Zone-level DepositCorpses dispatcher** — for full Qud parity, add an
  `IActivePart`-equivalent on the Graveyard that also offers work to bored
  NPCs via a new `ZoneTickIdleOffer` path; the per-NPC AIUndertakerPart
  would then be complementary rather than the sole dispatcher.
- **`ClearGoals` on NPC death (cross-cutting)** — surfaced during M5
  post-review: `CombatSystem.HandleDeath` doesn't clear the dying NPC's
  goal stack, so any in-flight `DisposeOfCorpseGoal` never fires its
  `OnPop` cleanup. In M5 specifically this leaks the
  `DepositCorpsesReserve` property on the carried corpse (another
  undertaker can still find + claim it next tick in 99% of cases because
  the reservation's meaning is "in-flight for someone," and the in-flight
  undertaker is dead — but the property stays stuck). Fixing cleanly
  means deciding a policy for every Phase 6 goal's death-cleanup;
  probably right before `zone.RemoveEntity(target)` in HandleDeath,
  `brain?.ClearGoals()`. Deferred as cross-cutting work.
- **Corpse examine pronoun** — Qud uses `NameMaker.MakeName` to generate
  unnamed corpse descriptors ("the corpse of a gnarled snapjaw"); CoO's
  `ExaminablePart` could gain a templated description.

##### Methodology Template planned application (M5)

Following the standard established by M1–M4:

| Template part | Plan |
|---|---|
| 1.2 Pre-impl verification sweep | ✅ This plan — Qud source cross-referenced + CoO APIs grepped with line citations |
| 1.4 Risk-ordered sub-milestones | M5.1 (spawner, testable in isolation) → M5.2 (goal, depends on M5.1 blueprint) → M5.3 (behavior + Undertaker/Graveyard, depends on M5.1+M5.2) |
| 2.1 Hallucination avoidance | Every CoO API cited here was grepped and line-cited during reconnaissance (see "Design decisions" table evidence column) |
| 2.2 Commit-message template | Scoped prefixes: `feat(entities)`, `feat(ai)`, `feat(blueprints)`, `docs(qud-parity)`, `fix-pass(ai)`, `test(scenarios)` |
| 3.1 EditMode unit tests | ~23 new tests across 3 new test files (CorpsePartTests, DisposeOfCorpseGoalTests, AIUndertakerPartTests) |
| 3.3 Regression pins | Every risk in the table above has an explicit regression test |
| 3.4 Counter-check pattern | Positive path (spawn, dispose, deposit) paired with negative (no-chance, overburden, NoHauling, claimed) for each |
| 3.5 PlayMode sanity sweep | Scenarios `SnapjawBurial` + on-demand `InspectAIGoals` during playtest |
| 3.6 Manual playtest scenario | `SnapjawBurial.cs` — player + snapjaw + undertaker + graveyard in starter village; kill snapjaw, observe corpse spawn, observe undertaker haul, observe graveyard deposit |
| 4 Parity audit | Pre-impl done here; post-impl re-audit before docs update |
| 5.1–5.3 Post-impl review | Mandatory severity-scaled pass (🔴🟡⚪🧪) before M5 section's Status flips to ✅ |
| 6.1–6.4 Honesty protocols | Observed/Expected tables in playtest scenarios; "can/cannot verify" bounds explicit |
| 7 Unity MCP tooling | Turn-by-turn reflection traces on brain.Goals for debugging corpse-carry transitions |
| 8.4 Post-milestone | This section gets a **Commit history** table and **Post-review findings** table after ship, mirroring M4's final form |

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

### Comprehensive Audit Plan — M1 through M5

> **Status:** planning document; execution pending. Once executed, findings
> land in §"Post-audit findings" per milestone (new subsections appended
> to each M1–M5 section), plus a cross-milestone findings appendix.
> Baseline HEAD for this audit: `caad57d` (main), 1712/1712 EditMode tests.

#### Audit goal

Produce a rigorous, evidence-backed inventory of every functional gap,
Qud-parity divergence, test hole, and logic error across M1–M5 —
including the production code, blueprint content, test code, and
manual playtest scenarios each milestone shipped. Output: a prioritised
fix-pass queue that the next several commits can burn down.

#### Scope

**In scope:**
- All files produced or modified by M1–M5 commit chains (Phase 6 Tier A/B/C work).
- The Qud decompiled reference files each milestone's docstring cites.
- All `[Test]` fixtures whose names reference M1–M5 features.
- All scenarios under `Assets/Scripts/Scenarios/Custom/` added for M1–M5.
- `Assets/Editor/Scenarios/ScenarioMenuItems.cs` wiring entries.
- All `Passive`, `AllowIdleBehavior`, `Corpse`, `Graveyard`, and M1.3-ambush-related blueprint keys in `Objects.json`.

**Explicitly out of scope:**
- Phase 6 Tier D (M8 zone transitions, M9 damage types) — not shipped.
- Unrelated systems (crafting, weather, calendar) unless a direct dependency surfaces.
- Performance profiling / GC allocation analysis.
- UI/renderer code except where it reads M1–M5-written state (e.g., sidebar reading `LastThought`).
- Save/load serialization drift — tracked separately.

#### Anti-hallucination discipline

Every finding in the executed audit must carry **source evidence**.
Non-evidence findings are discarded before reporting. Rules:

1. **File:line citation required.** A finding has the form
   `<path>:<line-range> — <observation>`. No exceptions for "I think it's…"
   or "probably…" claims.

2. **Quote the relevant code** inline with the finding when it aids reading.
   Prefer ≤ 8-line quotes; if more context is needed, cite the broader
   range in text and quote the tightest relevant slice.

3. **Claims about missing functionality require a negative-evidence
   grep.** Example: "X is missing" → run `grep -rn "X" Assets/Scripts`
   and include the empty/no-match result in the finding body. Absence
   of the tool's output is not absence of the feature.

4. **Claims about Qud parity require the Qud source file:line too.**
   A "parity divergence" without a cited Qud reference is just an
   opinion. If Qud has no equivalent, state that explicitly as
   "CoO-original" (per Methodology Template §4.2).

5. **Claims about test coverage require grepping the test assembly.**
   "No test pins X" → run
   `grep -rn "<keyword>" Assets/Tests/EditMode | wc -l` and report the
   count. Paired with a positive search of *what* the tests cover so
   the claim is falsifiable.

6. **Severity must be justified.** Every 🔴 / 🟡 finding includes a
   one-sentence "why this severity" explanation tying to concrete
   gameplay or correctness impact. No borderline-severity hedging.

7. **Wishy-washy language is banned from findings.** Replace "might,"
   "could potentially," "probably," "sometimes" with specific observed
   conditions or, if unverifiable, promote to a 🧪 test-gap finding
   ("behavior is unverified under condition X").

8. **Findings the audit cannot ground are rejected**, not deferred.
   If a claim needs playmode verification and the auditor can't run
   playmode, the finding becomes "🧪 test gap: behavior under condition
   X is not pinned; write a test." It does not become "🟡 bug: behavior
   under condition X is probably broken."

#### Finding taxonomy

Per Methodology Template §5.1, findings use the shared severity scale:

| Marker | Meaning |
|---|---|
| 🔴 | Critical — ships a bug, corrupts state, or blocks a claim in docs |
| 🟡 | Moderate — real defect or parity drift, workable for one iteration |
| 🔵 | Minor — polish, UX feedback, docstring drift |
| 🧪 | Test gap — behavior is correct (or presumed so) but unpinned |
| ⚪ | Architectural note for future work, not actionable now |

Each finding is also tagged with one or more **category tags**:

- `func` — functional completeness gap (claim vs shipped)
- `parity` — Qud divergence, intentional or not
- `test-unit` — EditMode unit test gap
- `test-integration` — scenario-harness / real-blueprint integration test gap
- `test-playmode` — live PlayMode sanity sweep gap
- `test-manual` — manual-playtest scenario missing / broken / unwired
- `bug` — observable incorrect behavior with repro steps
- `logic` — code correctness error without playtest repro but provable via read
- `wiring` — Part/Blueprint/Menu hookup missing
- `doc` — QUD-PARITY.md drift vs shipped code
- `perf` — unnecessary work, leak, or allocation in a hot path

Finding format (per Methodology Template §5.2):

```
##### <sev> <cat-tag> — <one-line title>

**File:** <path>:<line-range>

<1-paragraph description: what's wrong, what's observable, what fires
or doesn't fire. Quote the relevant code inline.>

**Why it matters:** <concrete in-game consequence or correctness
property it breaks>

**Proposed fix:** <1-3 sentences, sketch only — no code yet>

**Severity rationale:** <why this marker, not the next one up or down>
```

#### Audit dimensions (cross-cutting)

Each milestone must be examined along these seven dimensions. Findings
may apply to multiple dimensions — tag all that fit.

1. **Functional completeness.** Does the shipped code actually deliver
   the player-visible outcome the milestone plan promised? Cross-check
   the milestone's "M_x adds:" bullet list against shipped file diff.
   Any promised deliverable without corresponding code → 🟡 func gap.

2. **Qud parity.** For each ported artifact (Part, Goal, Effect), read
   the Qud reference file cited in the class docstring and confirm:
   mechanical behavior match, event-name match, field-type match,
   divergence documentation match. Undocumented divergences → 🟡 parity.

3. **EditMode unit test coverage.** For every public method, every
   event-handler branch, every non-trivial state transition: is there a
   test that would fail if the branch were broken? Count branches vs
   test-assertions; missing branch → 🧪 test-unit.

4. **Integration test coverage.** Does a `ScenarioTestHarness`-style
   test (or manual inline factory test) exercise the real blueprint
   wiring end-to-end? Missing integration pin → 🧪 test-integration.

5. **PlayMode sanity sweep.** Was the milestone verified with a
   live-bootstrap `execute_code` sweep per Template §3.5? Missing →
   🧪 test-playmode (for milestones claiming completeness; M5 did this,
   M1–M4 did not).

6. **Manual playtest scenario.** Does a scenario file exist under
   `Assets/Scripts/Scenarios/Custom/`? Is it wired into
   `ScenarioMenuItems.cs`? Does its current spawn geometry actually
   work in the starting village (no CompassStone-at-+2 pitfalls)? Any
   "exists but doesn't run" → 🟡 test-manual.

7. **Wiring/integration gaps.** Is the Part attached to the blueprints
   the milestone's doc claims it should be? Do the blueprints carry
   the tags the Part's filter requires? Does `GameBootstrap` wire any
   static factory references the Part uses? Missing → 🟡 wiring.

#### Per-milestone audit workbooks

Each workbook enumerates the files/blueprints/tests/scenarios/Qud-refs
in scope, plus the specific audit questions to answer. The auditor's
deliverable per milestone is a filled-in findings subsection.

##### M1 — Blueprint wiring (Tier A)

**Production files (confirm existence + read):**
- `Assets/Scripts/Gameplay/AI/AISelfPreservationPart.cs` (M1.1)
- `Assets/Scripts/Gameplay/AI/Goals/RetreatGoal.cs` (M1.1)
- `Assets/Scripts/Gameplay/AI/BrainPart.cs` — `Passive` field semantics (M1.2)
- `Assets/Scripts/Gameplay/AI/AIAmbushPart.cs` (M1.3)
- `Assets/Scripts/Gameplay/AI/Goals/DormantGoal.cs` (M1.3)
- `Assets/Scripts/Gameplay/AI/Goals/BoredGoal.cs` — Passive gate at hostile-initiate (consumer of M1.2)

**Blueprints to audit (in `Objects.json`):**
- Which NPCs carry `AISelfPreservation` part? Grep `"AISelfPreservation"`.
- Which NPCs set `Brain.Passive=true`? Grep `"Key": "Passive"`.
- Which NPCs carry `AIAmbush` part? Grep `"AIAmbush"`.
- MimicChest, SleepingTroll variants — are they correctly flagged with
  `WakeOnDamage` / `WakeOnHostileInSight`?

**Tests to verify exist:**
- `AIBehaviorPartTests` — specifically Warden AISelfPreservation + Ambush behaviors.
- `Phase6GoalsTests` or similar — RetreatGoal pathing.
- `AIAmbushPartTests` / `AISelfPreservationBlueprintTests` — blueprint wiring integration.

**Scenarios to verify (exist + menu-wired + actually run):**
- `CorneredWarden.cs`
- `IgnoredScribe.cs`
- `SleepingTroll.cs`
- `MimicSurprise.cs` (M1.3 dormant-creature content)

**Qud references to cross-check:**
- `qud_decompiled_project/XRL.World.Parts/AISelfPreservation.cs`
- `qud_decompiled_project/XRL.World.Parts/Brain.cs` (Passive field)
- `qud_decompiled_project/XRL.World.Parts/AIAmbush.cs`
- `qud_decompiled_project/XRL.World.AI.GoalHandlers/Retreat.cs` and `Dormant.cs`

**Specific audit questions:**
1. Does `BrainPart.HandleTakeTurn` actually gate hostile-initiate on
   `Passive`, or does the Passive gate live only in a subset of code
   paths? Read `BoredGoal.TakeAction` Step 2.
2. Does `RetreatGoal` use `Stat.BaseValue` (not `.Value`) for the
   HP-recovery exit gate? (M1 post-review M1.R-3.) Verify the current
   code still avoids Penalty-stuck deadlock.
3. Does `AIAmbushPart` handle entity removal (zone.RemoveEntity) while
   dormant without leaving stale references? Grep for any event hooks
   it registers and confirm cleanup.
4. Do all M1.3 dormant-creature blueprints actually inherit from a
   consistent parent that wires `DormantGoal` on initial spawn?
5. Is the Passive-flag gate on `BroadcastDeathWitnessed` (line 501)
   consistent with who "should" react to death? (Opens a M1.2 design
   re-evaluation for Villager/Merchant inclusion.)

##### M2 — Social + Consequence Layer (Tier B)

**Production files:**
- `Assets/Scripts/Gameplay/AI/Goals/NoFightGoal.cs` (M2.1)
- `Assets/Scripts/Gameplay/AI/Goals/WanderDurationGoal.cs` (M2.1 + M2.3 consumer)
- `Assets/Scripts/Gameplay/Conversations/ConversationActions.cs` — `PushNoFightGoal` action
- `Assets/Scripts/Gameplay/Mutations/CalmMutation.cs` (M2.2)
- `Assets/Scripts/Gameplay/Effects/Concrete/WitnessedEffect.cs` (M2.3)
- `Assets/Scripts/Gameplay/Combat/CombatSystem.cs` — `BroadcastDeathWitnessed` (M2.3)

**Blueprints to audit:**
- Which conversations reference `PushNoFightGoal`? Grep in `Conversations/*.json` if present, or in `ConversationActions.cs` usages.
- Is `CalmMutation` accessible to the player (grimoire / starting mutations)?
- What's the Qud-parity effect-type bitmask on `WitnessedEffect` (currently `MENTAL|MINOR|NEGATIVE|REMOVABLE`)?

**Tests to verify exist:**
- `NoFightConversationTests`
- `CalmMutationTests`
- `WitnessedEffectTests`
- `WitnessLineOfSightWall`, `WitnessRadiusBoundary`, `WitnessStacksOnSecondDeath` scenarios

**Scenarios to verify:**
- `PacifiedWarden.cs`
- `CalmTestSetup.cs`
- `CalmThenWitness.cs`
- `ScribeWitnessesSnapjawKill.cs`
- `WitnessLineOfSightWall.cs` / `WitnessRadiusBoundary.cs` / `WitnessStacksOnSecondDeath.cs`

**Qud references:**
- `XRL.World.AI.GoalHandlers/NoFightGoal.cs`
- `XRL.World.Effects/Shaken.cs` (the M2.3 divergent-mechanic reference)
- Any Qud equivalent to `CalmMutation` (M2 audit table says CoO-original; re-verify).

**Specific audit questions:**
1. Does `PushNoFightGoal` still idempotency-guard against duplicate
   pushes? Re-read the conversation-action implementation.
2. Does `WanderDurationGoal`'s new `Thought` field interact correctly
   with non-WitnessedEffect callers (if any exist)? `grep -n
   "new WanderDurationGoal" Assets/Scripts` → audit every call site.
3. `BroadcastDeathWitnessed` filter at `CombatSystem.cs:501` — is the
   Passive-only gate actually correct? Or should Villagers/Merchants/
   Farmers also react? (Design question, not necessarily a bug.)
4. Does `WitnessedEffect.OnStack` correctly extend on longer incoming
   duration but not on shorter? Re-verify against Qud's Shaken.
5. Does `CalmMutation` respect Passive flag correctly when targeting?
   Can it calm a non-Passive Warden into a pacified-but-still-Staying
   state, or does it flip Passive to true during the duration?
6. Is the `int.TryParse` 0-on-failure trap documented in the M2 review
   actually fixed in `PushNoFightGoal`'s duration parse? (M2.R precedent
   — should still hold, confirm.)

##### M3 — Ambient behavior parts (Tier B)

**Production files:**
- `Assets/Scripts/Gameplay/AI/AIPetterPart.cs` + `Goals/PetGoal.cs` (M3.1)
- `Assets/Scripts/Gameplay/AI/AIHoarderPart.cs` + `AIRetrieverPart.cs` + `Goals/GoFetchGoal.cs` (M3.2)
- `Assets/Scripts/Gameplay/AI/AIFleeToShrinePart.cs` + `Goals/FleeLocationGoal.cs` (M3.3)
- `Assets/Scripts/Gameplay/Settlements/SanctuaryPart.cs` (M3.3)

**Blueprints:**
- Which NPCs carry `AIPetter`? (Default target: VillageChild.)
- Which entities carry `AIHoarder` / `AIRetriever`? (Magpie, PetDog.)
- Which NPCs carry `AIFleeToShrine`? (Wounded Scribe, etc.)
- Which entities carry `Sanctuary` part? (Shrine.)

**Tests + scenarios:**
- `AIPetterPartTests` / `AIBehaviorPartTests` subset.
- `VillageChildrenPetting.cs`
- `MagpieFetchesGold.cs`
- `PetDogFetchesBone.cs`
- `WoundedScribeFleesToShrine.cs`

**Qud references:**
- `XRL.World.Parts/AIPetter.cs`, `AIHoarder.cs`, `AIRetriever.cs`, `AIFleeToShrine.cs`
- `XRL.World.AI.GoalHandlers/GoFetch.cs`, `FleeLocation.cs`, `Pet.cs`

**Specific audit questions:**
1. `AIPetterPart` — does it correctly idempotency-check `HasGoal("PetGoal")`?
   Does it still work after the 2026-04 sit-forever fix (BoredGoal
   fires AIBored while sitting)?
2. `AIHoarder`/`AIRetriever` interaction — can a Magpie and PetDog
   fight over the same GoldCoin, or does the reservation system prevent
   it? `GoFetchGoal` has a target-entity — is there a claim/reservation?
3. `AIFleeToShrine` filter — triggers only on wounded (low HP)? On
   shaken (WitnessedEffect)? Both? Read the `HandleEvent` gate.
4. `SanctuaryPart` — does it actually protect fleeing NPCs (e.g., apply
   a heal-over-time, reduce aggro)? Or is the shrine just a flee destination
   with no sanctuary mechanic? Compare to Qud's Sanctuary semantics.
5. Are all M3 scenarios wired into `ScenarioMenuItems.cs`? Same gap
   that bit SnapjawBurial — grep `[MenuItem]` entries.

##### M4 — Interior/Exterior (Tier C)

**Production files:**
- `Assets/Scripts/Gameplay/World/Map/Cell.cs` — `IsInterior` field
- `Assets/Scripts/Gameplay/World/Generation/Builders/VillageBuilder.cs` — interior tagging in `BuildRoom`
- `Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs` — `MarkDungeonInterior` helper
- `Assets/Scripts/Gameplay/AI/AIHelpers.cs` — `FindNearestCellWhere` BFS
- `Assets/Scripts/Gameplay/AI/Goals/MoveToInteriorGoal.cs` + `MoveToExteriorGoal.cs`
- `Assets/Tests/EditMode/TestSupport/CellVerifier.cs` — `IsInterior()/IsExterior()` helpers

**Scenarios:**
- `ScribeSeeksShelter.cs` (user has NOT yet confirmed manual playtest)

**Qud references:**
- `XRL.World/Zone.cs` — `IsInside` flag
- `XRL.World.AI.GoalHandlers/MoveToInterior.cs` + `MoveToExterior.cs`
- `XRL.World.Parts/Interior.cs` + `InteriorPortal.cs`
- `XRL.World/InteriorZone.cs`

**Specific audit questions:**
1. Does `VillageBuilder.BuildRoom` tag every interior floor cell
   correctly, or are doorway cells / thresholds missed?
2. `OverworldZoneManager.MarkDungeonInterior` — does it tag every
   dungeon cell (all of `wz > 0`), or only passable ones? What about
   walls inside dungeons?
3. `FindNearestCellWhere` — does it correctly reject non-passable
   START cells (fix-pass 🟡 #2), or can it still loop when the NPC
   themselves is on a solid cell?
4. `MoveToInteriorGoal`/`MoveToExteriorGoal` — do their `OnPop`
   overrides match the M5.2 lesson (clear `LastThought` rather than
   write a terminal `"sheltered"` that sticks)? This is the
   **known unfixed M4 follow-up** from the M5.2 commit body.
5. Has `ScribeSeeksShelter` actually been playtest-verified by the
   user? (Marked ⏳ pending — confirm or follow up.)
6. Is `ScribeSeeksShelter` wired into `ScenarioMenuItems.cs`? (Verify
   — it was the pattern-fix precedent for the M5 menu gap.)
7. Is there a `MoveToExteriorGoal`-driving scenario anywhere? (M4
   plan says it was out of scope — confirm.)

##### M5 — Corpse system (Tier C)

**Production files:**
- `Assets/Scripts/Gameplay/Entities/CorpsePart.cs`
- `Assets/Scripts/Gameplay/AI/Goals/DisposeOfCorpseGoal.cs`
- `Assets/Scripts/Gameplay/AI/AIUndertakerPart.cs`
- `Assets/Scripts/Scenarios/Custom/SnapjawBurial.cs`
- `Assets/Scripts/Gameplay/Combat/CombatSystem.cs` — `"Died"` event Zone-param (M5.1)

**Blueprints:**
- `SnapjawCorpse`, `CreatureCorpse`, `Graveyard`, `Undertaker`
- `Creature` parent (gained `Corpse` part in fix-pass `87c8400`)
- `Player`, `MimicChest` (gained `SuppressCorpseDrops` tag)
- `Snapjaw` (override for `Corpse` part)

**Tests:**
- `CorpsePartTests`, `CreatureCorpseBlueprintTests`, `DisposeOfCorpseGoalTests`, `AIUndertakerPartTests`, `SnapjawBurial_Applies_WithoutThrowing` smoke

**Scenarios:**
- `SnapjawBurial.cs` (user has NOT yet confirmed manual playtest; the
  mechanics are verified via PlayMode sweep but the visual/feel pass
  is ⏳)

**Qud references:**
- `XRL.World.Parts/Corpse.cs`
- `XRL.World.AI.GoalHandlers/DisposeOfCorpse.cs`
- `XRL.World.Parts/DepositCorpses.cs`
- `XRL.Names/NameMaker.cs` (corpse naming flavor — current CoO "{name} corpse" is simpler than Qud's NameMaker-driven descriptors)

**Specific audit questions:**
1. `CorpsePart.HandleDied` — does the spawned corpse's `DisplayName`
   interpolation fire for every Creature-derived NPC, or are there
   edge cases where `CreatureName` is the literal `"corpse"` string
   and the interpolation short-circuits? Audit the conditional.
2. `StackerPart.CanStackWith` stacks by `BlueprintName` — two
   `CreatureCorpse` entities with DIFFERENT `CreatureName` properties
   (villager + scribe) still stack. Confirm with a grep of stack logic
   + one new test. Currently noted as "accepted" in the M5 commit body
   — re-evaluate severity.
3. `DisposeOfCorpseGoal.OnPop` clears `DepositCorpsesReserve` — does
   it handle the case where the corpse was destroyed between claim
   and pop (e.g., burned to ash)? Grep for `RemoveIntProperty` and
   verify null-safety.
4. `AIUndertakerPart.FindNearestUnclaimedCorpse` — verify the rename
   in commit `44182c9` actually landed in both the method definition
   and all call sites.
5. `Undertaker` blueprint inherits `Villager`. Villager has
   `AllowIdleBehavior` tag → Undertaker inherits sitting behaviour.
   After the 2026-04 sit-fix (`04dca03`), AIUndertaker can now stand
   the NPC up when bored and a corpse exists — verify by integration
   test if not already covered.
6. `SnapjawBurial` scenario's `TryPlaceGraveyard` — the fallback ring
   of offsets avoids known starting-village blockers. Does it work in
   other village layouts (if zones are re-generated with different
   RNG)? Out of M5 scope but flag as ⚪ architectural note.
7. NPC-dies-mid-haul reservation leak — the documented follow-up
   (corpse's `DepositCorpsesReserve` stays set forever because
   `OnPop` doesn't fire on NPC death, which doesn't call
   `brain.ClearGoals()`). This is the **cross-milestone ClearGoals
   concern**; promote to a cross-cutting finding.
8. `CreatureCorpse` has `Weight=10`. A Str=14 Undertaker can carry
   multiple corpses simultaneously (MaxCarry ≈ 210). Is there any
   check preventing the Undertaker from over-claiming? Re-read
   `AIUndertakerPart`'s claim logic.

#### Cross-milestone concerns

These issues span multiple milestones; they are tracked at the audit
level rather than per-milestone to avoid double-logging.

1. **`ClearGoals` on NPC death** — `CombatSystem.HandleDeath` never
   calls `brain?.ClearGoals()`. Every goal on the dying NPC's stack
   keeps its state, including any `OnPop`-based cleanup. Known effects:
   - M5.2: `DisposeOfCorpseGoal.OnPop` doesn't fire → corpse's
     `DepositCorpsesReserve` property leaks.
   - Potentially M3.2: `GoFetchGoal`'s target-reservation leaks.
   - M1.3: `DormantGoal` state leaks (harmless since the ambusher is
     dead, but noisy).
   
   **Proposed audit action:** enumerate every goal with a meaningful
   `OnPop` / pop-time cleanup, verify cleanup-on-entity-death semantics,
   propose a single-site `brain?.ClearGoals()` call in `HandleDeath`
   with regression tests per affected goal.

2. **`Passive` flag coverage for civilian NPCs** — Villager, Farmer,
   Merchant, Warden, Undertaker are all `Passive=false`, meaning they
   do not react to nearby deaths via M2.3's `WitnessedEffect`. Design
   question: should the Passive filter be narrowed to "combat NPCs
   only," or is the current gap intentional (guards/farmers are too
   seasoned to be shaken)? Outcome: either widen the `Passive=true`
   roster in `Objects.json`, or document the design decision in the
   M2.3 section.

3. **`SittingEffect` zombie state** — `SittingEffect` persists even
   when the NPC moves off the chair (e.g., driven by `WanderDurationGoal`
   from M2.3's witness pacing). Post-pace, the NPC still has
   `SittingEffect`; `BoredGoal.Step 1` short-circuits to WaitGoal. The
   NPC stands in place forever with a stale "sitting" effect attached.
   **Proposed audit action:** either (a) auto-clear `SittingEffect`
   when the carrier's cell ≠ `SittingEffect.Furniture`'s cell, or
   (b) have `WanderDurationGoal.OnPush` remove `SittingEffect`.

4. **Scenario menu-wiring pattern gap** — Every new scenario requires
   BOTH the `[Scenario]`-attributed class AND a hand-added
   `[MenuItem]` in `ScenarioMenuItems.cs`. M5's `SnapjawBurial` shipped
   without the menu entry (fixed retroactively in `3d7e298`). Audit
   every scenario under `Assets/Scripts/Scenarios/Custom/` and verify
   menu wiring exists. Tooling opportunity: consider a build-time
   validator that fails compilation if a `[Scenario]` class has no
   corresponding `[MenuItem]`.

5. **Scenario spawn-geometry pitfall** — The starting village has
   solid CompassStones at player+2 east and player+6 east. Scenarios
   using `AtPlayerOffset(2, 0)` or `AtPlayerOffset(6, 0)` silently
   fail the spawn. Pattern caught in M4's `ScribeSeeksShelter` (fix
   `9781450`) and repeated in M5's `SnapjawBurial` (fix `e24a595`).
   Audit every scenario for hardcoded offsets against known blockers.
   Longer-term: `NearPlayer(min, max)` is the safe default.

6. **Terminal-thought stickiness pattern** — `LastThought` is a
   single-slot field with no auto-clear. Any goal that writes a
   "completion" thought in `OnPop` (e.g., M4's `"sheltered"` /
   `"outside"`) will stick indefinitely until the next goal's `Think()`
   call — which for idle villagers is never. M5.2 fixed this for
   `DisposeOfCorpseGoal` (OnPop writes null). **Audit M4's OnPop
   overrides:** `MoveToInteriorGoal` and `MoveToExteriorGoal` still
   write terminal thoughts. Either align with M5 by writing null, or
   verify in playtest that the M4 stickiness is desired.

7. **Manual-playtest validation gap** — M4 (`ScribeSeeksShelter`) and
   M5 (`SnapjawBurial`) both ship with manual playtest scenarios
   marked ⏳ "awaiting user observation." User has observed M5
   (multiple findings surfaced and were fixed); M4 remains unobserved.
   Audit outcome: either the user runs M4 and we log findings, or
   M4's status is honestly re-labeled "unverified" instead of
   "shipped."

8. **`AllowIdleBehavior` tag coverage** — Villager, Merchant, and
   Undertaker all carry `AllowIdleBehavior`. Does every NPC blueprint
   that SHOULD sit have it, and every one that shouldn't sit lack it?
   Grep-verify against the intended design for each.

9. **Event-ordering invariants** — Several M1-M5 features depend on
   precise event ordering in `CombatSystem.HandleDeath`:
   - M2.3: `BroadcastDeathWitnessed` must fire AFTER equipment-drop
     and BEFORE `zone.RemoveEntity(target)`.
   - M5.1: `CorpsePart.HandleDied` via `"Died"` event must fire
     BEFORE `zone.RemoveEntity(target)` so the death cell is
     resolvable.
   Read `CombatSystem.HandleDeath` once and confirm the current
   ordering. Any drift would silently break M2.3 AND M5.1.

10. **`BrainPart.CurrentZone` auto-population** — BrainPart's
    `CurrentZone` is set externally (by `GameBootstrap` for the
    starting player, by spawn-handlers for NPCs). Scenarios that
    spawn NPCs via `ctx.Spawn` — is `CurrentZone` set, or do some
    paths rely on first-TakeTurn to populate it (which means goals
    pushed pre-TakeTurn with brain-accessing logic can NRE)? Grep
    all `EntityBuilder.SpawnAt` paths for `brain.CurrentZone =`.

#### Pre-known findings (to confirm or dismiss during audit)

These were surfaced in prior reviews or user playtests and tracked
informally. The audit should confirm their current status and either
resolve or formalise as numbered findings.

| # | Sev | Source | Status |
|---|---|---|---|
| 1 | 🟡 | M5 post-review | ClearGoals-on-NPC-death leaks DisposeOfCorpseGoal's reservation. Cross-milestone. |
| 2 | 🟡 | M5 post-playtest | SittingEffect zombie state (pace without clearing). Just flagged; not fixed. |
| 3 | 🟡 | Investigation | Passive flag missing on Villager/Merchant/Farmer/Warden; death-witness broadcast excludes them. Design-question-shaped. |
| 4 | 🔵 | M4 follow-up | MoveToInterior/ExteriorGoal OnPop still writes terminal `"sheltered"` / `"outside"`, potential sticky-thought bug same as M5.2 pattern. Unverified in playtest. |
| 5 | 🧪 | M4 | Manual playtest of `ScribeSeeksShelter` ⏳ pending. |
| 6 | ⚪ | M5 | Corpse stacker bug: CreatureCorpse stacks by BlueprintName ignoring CreatureName. Documented as accepted; re-evaluate. |
| 7 | ⚪ | M5 follow-up | No world-gen hook places Graveyard in villages — scenario-only. |
| 8 | 🔵 | Scenario review | Scenario spawn-geometry pattern (CompassStone pitfall) — enumerate all scenarios for repeat offenders. |
| 9 | 🧪 | Scenario review | Menu-wiring pattern gap — enumerate all scenarios for missing `[MenuItem]` entries. |

#### Execution sequence

Audit is organised as **five per-milestone passes** followed by **two
cross-cutting passes**. Each pass produces a subsection appended to
QUD-PARITY.md.

**Recommended order (smallest-review-surface first):**

1. **M4 pass** — smallest code footprint; lets the auditor warm up
   on pattern recognition (OnPop stickiness, scenario wiring) before
   tackling the larger milestones.
2. **M5 pass** — most recently touched; patterns are familiar.
3. **M3 pass** — three AIBehaviorParts + FleeLocationGoal + SanctuaryPart.
4. **M2 pass** — dialogue-hook + mutation + effect. Moderate surface.
5. **M1 pass** — blueprint wiring + RetreatGoal state machine. Largest
   historical scope; save for when audit skills are calibrated.
6. **Cross-milestone pass 1** — confirm the 10 cross-cutting concerns
   above; promote to formal findings or dismiss.
7. **Cross-milestone pass 2** — review the pre-known findings table;
   confirm or dismiss each.

**Checkpointing:** at the end of each pass, commit a doc update with
the findings subsection. This keeps the review log in git history
rather than one monolithic unreviewable PR.

**Estimated effort (wall-clock):**
- Per-milestone pass: 45-90 min (read, grep, find, write)
- Cross-milestone passes: 30-45 min each
- Total: ~6-8 hours of focused audit work
- Fix-pass burn-down: another 4-8 hours depending on finding count

#### Reporting format

Findings land in QUD-PARITY.md as new subsections per milestone:

```
##### M<X> Post-audit findings (<date>)

<Intro: audit pass N of N; # findings by severity>

| # | Sev | Cat | Title | File:line |
|---|-----|-----|-------|-----------|
| …detailed finding blocks follow the table… |

##### <sev> <cat-tag> — <Finding title>

**File:** <path>:<line-range>

<body>
```

Cross-milestone findings go into a new top-level subsection:

```
#### Cross-milestone audit findings (<date>)

<Intro>

<Findings, same format>
```

After all findings land, a fix-pass queue is assembled at the top of
the audit section:

```
##### Fix-pass queue (ordered by severity × effort)

1. <🔴 finding> — est. <N hours>
2. …
```

#### Success criteria

The audit is complete when:

- [ ] Each milestone (M1-M5) has a Post-audit findings subsection with
      at least the categories (functional, parity, test, bug, wiring)
      explicitly addressed — either with findings or with an evidence-
      backed "no findings" statement.
- [ ] Every finding has a file:line citation and a severity rationale.
- [ ] Every finding in the Pre-known table is either confirmed (with
      a numbered audit finding) or dismissed (with evidence).
- [ ] Every cross-milestone concern is either promoted to a formal
      finding or dismissed with evidence.
- [ ] A prioritised fix-pass queue exists at the top of the audit
      section, with effort estimates per finding.
- [ ] The audit section committed to main with a clear rollback path
      (each pass = one commit).

The audit is NOT considered complete if:

- Any finding uses wishy-washy language ("might," "probably").
- Any finding lacks a file:line or severity rationale.
- The fix-pass queue mixes severity tiers without effort weights.
- Cross-cutting concerns are left unpromoted/undismissed.

---

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
Phase 5 — Goal composition primitives (InsertGoalAfter, CommandGoal)
Phase 6 — Missing goal handlers (WanderDuration, FleeLocation, Retreat, Dormant, Reequip, MoveToZone, etc.)
Phase 7 — More AIBehaviorPart subclasses (AIShopper, AIPilgrim, AIShoreLounging)
Phase 8 — Party / follower system (PartyLeader, CanAIDoIndependentBehavior)
Phase 9 — Opinion system (OpinionMap replacing PersonalEnemies)
Phase 10 — Debug / introspection (Brain.Think, goal descriptions)
Phase 11 — TurnTick system (part-level ticks independent of TakeTurn)
Phase 12 — Calendar / world time (IsDay/IsNight, schedules)
Phase 13 — Zone lifecycle (suspend/thaw, elapsed-time catch-up)
Phase 14 — AI combat intelligence (weapon evaluation, Reequip, FindProspectiveTarget)
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

## Major Feature Development Standard — Methodology & Testing Template

> **Executive Summary**
>
> Phase 6 (M1 + M2) shipped two milestones with 1564 passing EditMode tests,
> zero shipping-blocking bugs, and a documented audit trail of every
> assumption, divergence, and post-review fix. It also caught several bugs
> _pre-commit_ that would have shipped silently under a lower-discipline
> workflow: the `int.TryParse` 0-on-failure trap in PushNoFightGoal, the
> `Penalty`-stuck RetreatGoal deadlock, the `GatherRoomCells` zone re-scan
> that stacked ambushers on guards, and the look-mode "hostile" label on
> pacified NPCs.
>
> Those catches did not happen by accident. They happened because we
> verified every plan assumption against live code before writing any,
> paired every positive assertion with a counter-check, and reviewed our
> own output critically against the decompiled reference. When we cut
> corners — implicit trust in a plan's claimed API shape, vacuous tests
> that passed without exercising the path — we regressed.
>
> This section codifies the workflow that produced that outcome so it can
> be reused for every future Major Plan in Caves of Ooo, and for Unity
> projects built with the Unity MCP tooling beyond this one. It is
> deliberately specific — concrete checklists, not principles — because
> the failure modes we saw were all "I forgot to check X" failures, not
> "I didn't know X was important" failures.
>
> Non-goals: this template is not a style guide, not a code-review rubric,
> and not a replacement for judgment. It's a protocol for _which order to
> do things in_ and _what evidence to produce at each step_.

### How to use this template

For any feature that meets two or more of:

- Touches multiple systems (AI + combat + UI, etc.)
- Requires new blueprint/JSON content to be visible
- Claims parity with a reference codebase (Qud, etc.)
- Has a non-obvious failure mode (state-machine, timing, RNG-dependent)
- Will be wired into gameplay the player can exercise

Follow Parts 1–7 in order. Reach for Part 8 checklists mid-milestone to
stay honest about what you've verified.

Small features (one-file, one-test bugfixes) don't need the full protocol —
but the commit-message discipline (Part 2.2) and the honesty protocols
(Part 6) still apply.

---

### Part 1 — Plan Lifecycle

A plan passes through three states before any code is written:
**drafted** → **verified** → **scope-pruned and tiered**. Skipping any
state is how hallucinations ship.

#### 1.1 — Initial plan draft

**Output:** a plan document (in-repo — `Docs/...` — not in a chat
buffer) with:

| Section | Content |
|---|---|
| Goal | 1-paragraph description of the player-visible outcome |
| Scope | What's in, what's explicitly out |
| Content-readiness analysis | Each deliverable tagged 🟢 ready / 🟡 partial / 🔴 blocked / ⚪ deferred-until-content |
| Cross-cutting infrastructure gaps | Any adjacent system that must change to unblock this |
| Effort-to-impact ordering | Sub-deliverables ranked by player-visible value per day of work |
| Implementation tiers | A (blueprint wiring, hours), B (small systems, days), C (medium infra, days each), D (large infra, weeks) |
| Verification checklist | Empty checkboxes for "what proves this shipped correctly" |

**Evidence from Phase 6:** commits `e56e674`, `519ee3f` established the
Per-Goal Verdict table, Cross-Cutting Infrastructure Gaps section, and
Summary Matrix. That framework made it possible to say M1 ships Tier A
(blueprint wiring) and defer Tiers C/D without losing track.

#### 1.2 — Pre-implementation verification sweep

**The single highest-leverage step in this whole protocol.** Before
writing code, take every API-shape claim in the plan and verify it
against the actual codebase.

**Check each of:**

- Class/interface signatures referenced by the plan
- Abstract vs virtual members (plans often get this wrong)
- Constructor signatures (is there a `: base(duration)` overload? Often not.)
- Field vs property access (can you really override that?)
- Event names + argument shapes
- Enum values referenced (e.g. does `AsciiFxTheme.Mental` exist?)
- Line numbers the plan cites (they drift over time)

**Output:** a correction table in the plan, showing every drift you
found.

**Evidence from Phase 6 M2**: the pre-M2 verification sweep produced 14
concrete corrections, every one of which would have caused a compile
error or silent no-op if the plan had been followed verbatim. Examples:

- Plan said `Effect` had `public int Type`; reality was
  `virtual int GetEffectType()`
- Plan said `OnApply()` / `OnRemove()` were zero-arg; reality takes
  `Entity target`
- Plan said `DirectionalProjectileMutationBase` had 4 abstracts; reality
  has 7
- Plan said `Effect` took `: base(duration)`; no such constructor exists

See `Docs/QUD-PARITY.md` M2 section "Plan corrections vs the prior M2
drafts" for the full list.

#### 1.3 — Scope pruning with documented rationale

Sometimes the verification sweep reveals that a sub-feature the plan
called for is **redundant in the current code**, or **actively harmful**.
Prune it, _in writing_, with the rationale.

**Evidence from Phase 6 M2**: during M2.1 implementation, reading
`BrainPart.HandleTakeTurn` revealed `if (InConversation) return true;`
at line 231 — which meant the plan's "auto-pacify on StartConversation"
feature was functionally redundant, and would actively _break_ the
PushNoFightGoal action via the idempotency guard. Shipped only the
dialogue action; documented the cut in the commit message and the test
class xml-doc. See commit `a34faf6`.

**Protocol for pruning:**

1. Quote the plan's original claim
2. Cite the line numbers in current code that make it redundant or
   harmful
3. Describe the concrete regression the prune prevents
4. Record what to revisit if conditions change ("if a future scenario
   needs mid-dialogue combat protection beyond what InConversation
   provides, auto-pacify can be revisited with semantics that don't
   collide with this action")

#### 1.4 — Risk-ordered sub-milestone breakdown

**Rule:** smallest blast radius first. Each sub-milestone must:

- Commit as one reviewable change
- Be independently revertable
- Ship one complete player-visible or testable behavior

**Evidence from Phase 6**: M2 shipped as three focused commits in risk
order — M2.2 (CalmMutation — 1 new class + 1 JSON edit + 1 Player
loadout one-liner) before M2.1 (ConversationManager static state)
before M2.3 (CombatSystem.HandleDeath — highest integration cost).
Each could have been reverted alone.

---

### Part 2 — Implementation Discipline

#### 2.1 — Hallucination-avoidance checklist (apply per code change)

Before writing each new symbol:

- [ ] Open the file you're extending; confirm the abstract/virtual
  surface matches your plan
- [ ] If the plan shows code, spot-check at least one signature in the
  file before copying the shape
- [ ] Follow an existing sibling (e.g. another `DirectionalProjectileMutation`)
  as the template for your new class — not your memory of the pattern
- [ ] If a method takes `out T`, read its failure semantics before
  trusting it. `int.TryParse` writes 0 on failure AND returns false
  — either guard is sufficient if you know about it, together they
  close the trap

Before calling an API you haven't used recently:

- [ ] Read its implementation (not just the signature) to understand
  side effects
- [ ] Check whether it mutates state that your calling context also
  touches (the `GetReadOnlyEntities` live-collection trap)

#### 2.2 — Commit message template

Phase 6 commits converged on a consistent body structure. Reuse it:

```
<type>(<scope>): <tight present-tense summary>

<2-3 sentence problem statement: what was broken or missing, in
user-observable terms>

<section headers in CAPS for each kind of content>

IMPLEMENTATION NOTES (risks verified before writing code)
  1. <Plan correction #1>: what was wrong in the plan, what's actually
     true (with file:line citation), how the code reflects the truth
  2. <Plan correction #2>: ...

SCOPE DIVERGENCE FROM THE PLAN (if any)
  <Feature that was dropped>: why, backed by cited evidence; what to
  revisit if conditions change.

BUG CAUGHT BY A TEST MID-IMPLEMENTATION (if any)
  <Raw pre-commit test failure>, followed by: root cause + fix.

Files:
- MOD/NEW <path>: <one-line purpose>
- ...

Tests: <N> -> <M> (+D). All green.

<Footer: co-author line>
```

**Evidence**: every Phase 6 M1/M2 commit follows this pattern. Examples:
`a34faf6` (scope divergence called out), `9c8522c` (5 implementation
notes), `ecca5c9` (2 post-review findings, each cited against
file:line).

#### 2.3 — Pre-commit verification gates

Before `git commit`:

- [ ] `mcp__unity__refresh_unity` with `compile: request` → no errors
- [ ] `mcp__unity__read_console` with `types: ["error"]` → empty
- [ ] `mcp__unity__run_tests` EditMode → all green
- [ ] If new test files were added and `total` count didn't change after
  the test run, retry with `mode: force` on the refresh (Unity's test
  runner can miss newly-created files on a soft refresh)
- [ ] Diff review: did any symbol name, method arity, or signature
  contradict the verified API from Part 1.2?

---

### Part 3 — Testing Pyramid

Caves of Ooo has six distinct test layers. Each has a different failure
mode it's good at catching; none replaces the others. The goal is not to
use every layer for every feature but to know _when_ each is the right
tool.

#### 3.1 — EditMode unit tests (fastest, most isolated)

**Location:** `Assets/Tests/EditMode/**/*.cs`, NUnit `[Test]` methods.

**What they're good for:** single-method behavior, boundary conditions,
null-guard coverage, algorithmic correctness. Fast — whole 1564-test
suite runs in ~15s.

**Style:**
- Manual entity construction (`new Entity()` + `AddPart(...)`) when you
  need to exercise pre-wiring code paths
- `ctx.Spawn("Warden")` via `ScenarioTestHarness` when you want the real
  blueprint
- Setup via `[SetUp]`, teardown via `[TearDown]` (Faction/ConversationActions
  reset commonly needed)

#### 3.2 — EditMode integration tests via Scenario harness

**Location:** same test folder, but using `ScenarioTestHarness`,
`ctx.AdvanceTurns(n)`, and `ctx.Verify()`.

**What they're good for:** end-to-end behavior against real blueprints
— "does the Warden blueprint's AISelfPreservation actually trigger
after a tick cycle" kinds of questions. The harness loads Objects.json
once at `[OneTimeSetUp]` and hands out fresh `ScenarioContext`
instances per test.

**Why it matters:** unit tests against hand-constructed entities can pass
even when a blueprint JSON edit silently dropped a required part. The
harness closes that gap.

**Evidence**: `AIBehaviorPartTests` was ported to the harness during
scenario Phase 3d (commit `4404df7`) with measurable line reduction.

#### 3.3 — Regression tests (pin every fix)

**Rule:** every bug fix ships with a test whose name describes the bug
and whose assertion would fail if the fix were reverted.

**Examples from Phase 6:**

- `RetreatGoal_Recovery_Exit_UsesBaseValue_NotPenalizedValue` — comment
  cites M1.R-3, the test deliberately sets Penalty=18 so the old
  `Value`-based gate would deadlock forever
- `AmbushCreatures_NeverShareCellWithGuardsOrLoot` — 200-seed iteration
  across both biomes, builds an occupancy dictionary, fails if any
  cell has >1 entity
- `HandleDeath_Broadcast_IncludesWitnessAtExactRadius` — places a
  witness at Chebyshev distance exactly 8, pins the inclusive boundary
  against a future `>=` flip

**Every test file** containing regression tests should link the xml-doc
back to the commit or review finding that motivated it ("Regression
for M2 post-review finding M1.R-2").

#### 3.4 — Counter-check pattern (avoids vacuous passes)

**The core discipline for tests:** every positive assertion is paired
with a case that would FAIL if the wiring were wrong.

**Examples:**

| Positive test | Counter-check that rules out vacuous pass |
|---|---|
| Passive Scribe doesn't push KillGoal against hostile Snapjaw | Non-Passive Warden in identical setup DOES push KillGoal |
| MimicChest stays dormant with faction-hostile in sight | SleepingTroll in identical setup DOES wake (`WakeOnHostileInSight=true`) |
| PushNoFightGoal pacifies speaker | Listener (player) in the same call is NOT pacified |
| Scribe within 8 cells gets WitnessedEffect | Scribe at distance 10 does NOT |
| Wall between witness and death blocks effect | No-wall control case: effect fires |

**Without counter-checks**, "no KillGoal pushed" can mean "the Passive
gate works" _or_ "the Snapjaw couldn't see the Scribe" _or_ "they
weren't actually faction-hostile". The counter-check distinguishes.

**Every precondition** (hostility, line-of-sight, sight radius, distance)
is explicitly asserted via `Assume.That(...)` or an inline check before
the positive assertion. See `CalmMutationTests` and `WitnessedEffectTests`.

#### 3.5 — PlayMode sanity sweep via `mcp__unity__execute_code`

**When to use:** after a milestone ships, before declaring it production-
ready — verify the state-machine transitions hold against a live
bootstrap with real blueprints, live FactionManager, and the full
turn-manager loop.

**Protocol (follow exactly, or a silent sweep will convince you
everything works when it doesn't):**

1. `mcp__unity__manage_editor play` — warn the user this resets the
   scene
2. **Preflight**: one `execute_code` call confirming Play mode is
   active, PlayerEntity resolves, EntityFactory is wired, the specific
   blueprints and registry entries you're about to exercise are reachable
3. **Per-scenario**:
   a. Print preconditions before acting (hostility, LOS, distances, stat
      values). If any precondition is not what you expected, STOP and
      report; do not paper over
   b. Perform the action
   c. Print raw post-state — not paraphrased. Raw field values, raw
      goal stack contents, raw effect list
   d. Include a counter-case in the same scenario (or as a sibling
      scenario) that would FAIL if the wiring were broken
4. `mcp__unity__manage_editor stop`
5. **Summary**: one table per scenario, every row a fact from
   execute_code output, each row labeled "Observed / Expected". No
   "mostly works" — either the table passes or it doesn't.

**Honesty bounds — always stated explicitly in the summary:**

- **Can script-verify**: goal-stack membership, effect-list contents,
  `Stat.BaseValue`, `zone.GetEntityPosition`, `brain.HasGoal<T>()`,
  `FactionManager.IsHostile`, message-log last-entry
- **Cannot script-verify**: particle emission, smooth visual motion,
  camera transitions, FX renderer state, input-driven dialogue flows

**Evidence**: the M1 state-portion sweep (see "Option A — State-portion
results" earlier in this conversation) caught a vacuous-pass risk in
Scenario 4a — Snapjaw is not faction-hostile to MimicChest, so the
"sight-wake disabled" test was trivially true. Re-ran with a Warden
(confirmed faction-hostile) to make the counter-check meaningful. M2
sweep confirmed the live look-mode label "pacified" bugfix end-to-end.

#### 3.6 — Manual playtest via Scenario Scripting

**Location:** `Assets/Scripts/Scenarios/Custom/*.cs` + one menu entry in
`Assets/Editor/Scenarios/ScenarioMenuItems.cs`.

**When to use:** for behaviors that are easy for a human to see but hard
to script-verify — particle emission, path-smoothness, UI rendering,
"does this _feel_ right."

**Pattern:**

```csharp
[Scenario(name: "…", category: "…", description: "…")]
public class MyScenario : IScenario
{
    public void Apply(ScenarioContext ctx)
    {
        ctx.Player.AddMutation("...", level: 3);
        ctx.Spawn("Scribe").AtPlayerOffset(3, 0);
        ctx.Spawn("Snapjaw").WithHpAbsolute(1).AsPersonalEnemyOf(ctx.PlayerEntity).AtPlayerOffset(5, 0);
        ctx.Log("Kill the Snapjaw, watch the Scribe shake.");
    }
}
```

+ one menu entry line in `ScenarioMenuItems.cs`.

**Evidence**: M1 shipped three playtest scenarios (`CorneredWarden`,
`IgnoredScribe`, `SleepingTroll`, commits `0ebaa9a` and
`e3d7d76`). M2 mapping is documented in the conversation above but not
yet committed.

**Do not** use this layer for things that _could_ be script-verified.
Manual playtest time is expensive; pyramid discipline says push as
low as possible.

#### 3.7 — MCP `manage_input` keyboard-driven PlayMode testing

**Location**: documented in `Docs/MCP_PlayMode_Testing_Strategy.md`.

**When to use**: flows that involve UI state transitions (Inventory →
ActionMenu → Read → Announcement → Dismiss) where `execute_code`
cannot legally substitute (it would leave popups orphaned per the
strategy doc's Rule 1: "NEVER fire game events directly via execute_code").

**Protocol** (excerpted from the strategy doc):

1. Use `manage_input` `move_to` / `key_press` / `send_sequence` to mimic
   player actions
2. Use `execute_code` only for **read-only observation** (InputState,
   AnnouncementUI.IsOpen, stat values)
3. Always dismiss announcement popups after actions that may queue
   them — they stack

#### 3.8 — Testing-layer selection matrix

| Behavior under test | First choice | Second choice |
|---|---|---|
| Pure algorithm (distance, LOS) | EditMode unit | — |
| Part with minimal env (Effect lifecycle) | EditMode unit | — |
| Behavior through blueprint wiring | EditMode integration (Scenario harness) | EditMode unit if harness overkill |
| Bug you just fixed | Regression test (Parts 3.1 or 3.2) | Playtest scenario if visual |
| Multi-system live integration | PlayMode sanity sweep (3.5) | Manual scenario (3.6) |
| Particle / animation / "feel" | Manual scenario (3.6) | Screenshot by user |
| UI-driven state flow | `manage_input` MCP (3.7) | — |

---

### Part 4 — Parity / Reference-Code Audit Protocol

When adapting from a reference codebase (Qud decompiled, or any upstream
project), document parity explicitly. Overclaimed parity is a bug.

#### 4.1 — Survey the reference

For each artifact you're about to implement:

1. `find qud_decompiled_project -name "*KeywordA*" -o -name "*KeywordB*"`
   (or ripgrep equivalent) — identify candidate files
2. Read each candidate. Compare:
   - Signatures (ctor, method args, field types)
   - Mechanical behavior (what events fire, what state changes)
   - Identifiers (effect bitmasks — `TYPE_MENTAL | TYPE_MINOR` etc.)
   - Subtle merge rules (OnStack vs Apply overrides)

3. Classify each artifact as:
   - **Match** — same shape and mechanic
   - **Extension** — same shape, CoO adds fields/params the reference lacks
   - **Divergent mechanics** — shares name/spirit, implements differently
   - **CoO-original** — no reference equivalent

**Evidence from Phase 6 M2**: the post-implementation parity audit
produced a classification table for M2:

| Artifact | Reference | Classification |
|---|---|---|
| NoFightGoal (primitive) | `XRL.World.AI.GoalHandlers/NoFightGoal.cs` | Extension (Duration + Wander added) |
| CalmMutation | None | CoO-original |
| PushNoFightGoal dialogue action | None | CoO-original hook |
| WitnessedEffect | `XRL.World.Effects/Shaken.cs` | Divergent mechanics, bitmask aligned |
| BroadcastDeathWitnessed | None | CoO-original mechanic |

#### 4.2 — Handle classification honestly

- **Match**: cite the reference file:line in the class xml-doc.
- **Extension**: document which fields are CoO-specific and why they
  don't collide with future strict-parity work.
- **Divergent mechanics**: call out what DIFFERS in a "Parity notes:"
  block in the class docstring. Do not pretend the mechanics are
  identical.
- **CoO-original**: flag explicitly in both the class docstring AND the
  project's parity doc (e.g. `QUD-PARITY.md`) so a future parity audit
  doesn't mistake it for a port.

#### 4.3 — Doc drift prevention

After shipping, add a post-implementation parity table to the plan doc
— not to the class docstrings alone. A reader of `QUD-PARITY.md`
should be able to see at a glance which artifacts are matched, extended,
divergent, or CoO-original _without reading the source_.

**Evidence**: M2 shipped this as the "Post-implementation Qud parity
audit (M2)" subsection in `Docs/QUD-PARITY.md`.

---

### Part 5 — Post-Implementation Review

After a milestone's code lands and tests pass, review your own output
critically. This is not a second round of implementation — it's a
code-review pass you perform on yourself (or delegate to a sub-agent)
with the goal of catching what the implementation missed.

#### 5.1 — Severity scale

Use the same scale consistently across the project. Introduced for M1
review:

| Marker | Meaning |
|---|---|
| 🔴 | Critical — ships a bug, corrupts state, or blocks a claim in docs |
| 🟡 | Moderate — real defect or parity drift, workable for one iteration |
| 🔵 | Minor — polish, UX feedback, docstring drift |
| 🧪 | Test gap — behavior is correct but unpinned |
| ⚪ | Architectural note for future work, not actionable now |

#### 5.2 — Finding template

For each finding, record:

```
##### 🟡 Bug N — <one-line title>

**File:** <exact path>:<line-range>

<1-paragraph description: what's wrong, what's observable, what fires
or doesn't fire>

**Why it matters**: <concrete in-game consequence or correctness
property it breaks>

**Proposed fix**: <1-3 sentences, sketch only — no code yet>
```

**Evidence**: the M1 review section of `QUD-PARITY.md` documents 14
such findings. The post-M2 self-review in this conversation produced 11
more using the same format.

#### 5.3 — Fix pass structure

After review:

1. Rank findings by severity + effort
2. Pick the top N (usually 3–5) for an immediate follow-up commit
3. Document the rest in the plan for later
4. Each fixed finding gets a regression test per Part 3.3

**Rule**: the follow-up commit's message body lists every finding by
severity marker ("🔴 Bug 1: ...", "🟡 Bug 2: ..."). See commit
`3167614` for the M1 fix-pass precedent, `585b73b` for the M2 analog.

#### 5.4 — What review misses

Self-review is systematically weaker at:

- **Behavioral feel** (does this animate right, does the timing feel
  good). Manual playtest scenarios are the only reliable check.
- **Integration timing** (event ordering across systems). PlayMode
  sanity sweeps with counter-checks are the only reliable check.
- **Hallucination in the review itself**. If you delegate to a sub-
  agent, cross-check 3–4 of the agent's claims against source files
  before accepting the full report. See the M1 audit, where the audit
  agent's claim that a specific fix was incomplete turned out to be
  correct (M2.R-3 RetreatGoal Penalty).

---

### Part 6 — Honesty & Reporting Protocols

This is where discipline most often breaks down under time pressure.

#### 6.1 — Raw output rule

When reporting on a sanity sweep or live check: **paste the raw
`execute_code` output**, not a paraphrase. Paraphrasing is how you
convince yourself "mostly works" is "works."

**Good**: "Observed: `HasKill=False HasFlee=False HasNoFight=True
top=NoFightGoal`. Expected: HasKill=False (Passive suppresses combat
push), NoFight on stack."

**Bad**: "Counter-check passed."

#### 6.2 — Stop-on-unexpected rule

If any precondition is not what you expected, or a script throws, or a
counter-check returns "n/a":

1. Stop
2. Report the raw observation
3. Decide: is this a meaningful failure (the feature is broken), a
   precondition issue (fix the setup and re-run), or a hallucinated
   expectation (update your understanding)?
4. Do not continue to subsequent steps as if nothing happened

**Evidence**: M1 Option A Scenario 4a showed `hostile-to-Mimic=False`
as a precondition — the test was trivially true with no real hostile
in sight. Stopped, surveyed faction relationships, re-ran with a
Warden (actually faction-hostile to Mimic) as the witness source. The
second run then exercised the WakeOnHostileInSight gate for real.

#### 6.3 — Can-verify vs cannot-verify bounds

Every live-verification report includes an explicit honesty section:

```
Can verify (script-observable):
  - goal-stack membership, effect list, stat values, entity positions

Cannot verify (require screenshot or human eyes):
  - particle emission, smooth motion, UI rendering, animation feel
```

Claim nothing outside the "can verify" bounds. If the user needs the
"cannot verify" checks, _say so_ — don't bluff.

#### 6.4 — Scope divergence transparency

When a plan calls for X and you ship Y, the commit message body says
SO, with the rationale. Do not ship the divergence silently assuming
the plan will be updated later.

**Evidence**: commit `a34faf6` body has a full "SCOPE DIVERGENCE FROM
THE CONSOLIDATED M2 PLAN" section explaining why the auto-pacify was
cut. Future readers can see the reasoning without reading conversation
history.

---

### Part 7 — Unity MCP Tooling Workflow

Specific tool-call discipline for the Unity MCP environment.

#### 7.1 — Tool usage rules

| Tool | Use for | Do NOT use for |
|---|---|---|
| `mcp__unity__refresh_unity` | Compile requests after script changes | Dismissing stale play state (use `manage_editor stop`) |
| `mcp__unity__read_console` | Errors, warnings after compile | Performance profiling (use `manage_profiler`) |
| `mcp__unity__run_tests` | EditMode & PlayMode suites | Running a single test (filter is unreliable; run all and read the summary) |
| `mcp__unity__manage_editor play/stop` | Entering/exiting Play mode | Dismissing UI (use `manage_input`) |
| `mcp__unity__execute_code` | **Read-only** state observation | Firing gameplay events (corrupts state — see `Docs/MCP_PlayMode_Testing_Strategy.md` Rule 1) |
| `mcp__unity__manage_input` | Keyboard-driven playtest flows | Logic not backed by an actual key binding |

#### 7.2 — Common pitfalls

**New test files not picked up by `run_tests`**: the test runner misses
newly-created files on a soft refresh. Retry with `mcp__unity__refresh_unity`
`{mode: force, compile: request, scope: all}`. Observed twice during Phase
6 M2 (at M2.3 WitnessedEffectTests and at post-review tests).

**Test job "failed to initialize (tests did not start within timeout)"**: 
the editor is in Play mode. Call `mcp__unity__manage_editor stop` first.
Observed after ~6 distinct `run_tests` calls during Phase 6.

**`Int.TryParse(arg, out duration)` writes 0 on failure**: always
combine with explicit sentinel (use a separate `out int parsed`
variable and guard `parsed > 0`). This trap caught us pre-commit once
at M2.1.

**`Zone.GetReadOnlyEntities()` returns live `Dictionary.KeyCollection`**:
do not iterate it while calling `ApplyEffect`, `AddEntity`,
`RemoveEntity`, or anything that could touch `_entityCells`. Use
`zone.GetAllEntities()` which allocates a fresh list. Comment at
`Zone.cs:141` explicitly warns: "Callers must not mutate the zone
during iteration."

**Play mode is a reset**: entering Play mode resets the scene to its
initial state. Warn the user before doing it. If they had in-progress
state (carried items, explored zones), those are gone.

#### 7.3 — Verification loop

Standard post-edit cycle:

```
1. refresh_unity { compile: request }
2. refresh_unity { compile: none, wait_for_ready: true }   # wait for compile to finish
3. read_console { types: [error] }                          # must be empty
4. run_tests { mode: EditMode }                             # must pass
   - If timeout: manage_editor stop, re-run
   - If new test count == old: refresh with mode: force, re-run
5. Commit
```

---

### Part 8 — Copy-Paste Checklists

Pin these in the milestone's workspace. Check items off as you go.

#### 8.1 — Pre-plan checklist

- [ ] Feature goal is one sentence, player-visible
- [ ] Scope document lists what's in AND what's out
- [ ] Every deliverable tagged 🟢/🟡/🔴/⚪
- [ ] Cross-cutting gaps identified
- [ ] Verification checklist exists with empty boxes
- [ ] Plan committed to the repo before any code

#### 8.2 — Pre-implementation checklist (per sub-milestone)

- [ ] Verification sweep complete — every API shape the plan claims is
  corroborated or corrected
- [ ] Corrections logged in the plan as a table
- [ ] Scope pruned with rationale if the sweep revealed redundancy
- [ ] Sub-milestone order set by blast radius (smallest first)
- [ ] Each sub-milestone will commit standalone

#### 8.3 — Pre-commit checklist (per sub-milestone)

- [ ] `refresh_unity` compile clean, `read_console` errors empty
- [ ] `run_tests` EditMode: all green
- [ ] New test count increased as expected (force-refresh if not)
- [ ] Regression test present for every fix
- [ ] Counter-check present for every positive assertion
- [ ] Commit message body follows the template in Part 2.2
- [ ] Scope divergences documented (if any)

#### 8.4 — Post-milestone checklist (after final sub-milestone)

- [ ] Self-review pass completed, findings logged with severity markers
- [ ] High-priority findings fixed in a follow-up commit
- [ ] Parity audit table populated in the plan doc
- [ ] PlayMode sanity sweep executed with raw-output reporting
- [ ] Honesty bounds (can-verify / cannot-verify) stated
- [ ] Manual playtest scenarios written for visual/feel aspects
- [ ] Plan doc's verification checklist boxes all checked
- [ ] Plan doc's claims still match the shipped code — any drift fixed

---

### Appendix A — Worked example: Phase 6 M1 + M2

Phase 6 M1 + M2 is the canonical application of this template. For a
new feature, read the corresponding section here alongside the
checklists above.

| Step | M1 precedent | M2 precedent |
|---|---|---|
| 1.1 Plan draft | `Docs/QUD-PARITY.md` Phase 6 sections (commits `e56e674`, `519ee3f`) | M2 consolidated plan in QUD-PARITY.md (commit `9cedaec`) |
| 1.2 Verification sweep | Pre-coding audit against `BlueprintLoader`, `Entity`, `BrainPart` | 14 plan corrections documented in QUD-PARITY.md M2 section |
| 1.3 Scope pruning | N/A | M2.1 auto-pacify pruned (commit `a34faf6`, cited `BrainPart.HandleTakeTurn:231`) |
| 1.4 Risk-ordered breakdown | M1.1 (blueprints) → M1.2 (Passive) → M1.3 (AIAmbush + lair) | M2.2 → M2.1 → M2.3 (by blast radius) |
| 2.2 Commit messages | `bf06376`, `3167614` | `9c8522c`, `a34faf6`, `a16c35c` |
| 2.3 Verification gates | 1317 → 1534 → 1536 tests | 1536 → 1539 → 1547 → 1559 → 1564 tests |
| 3.1–3.3 Unit/integration/regression | `AIAmbushPartTests`, `AISelfPreservationBlueprintTests`, `LairPopulationBuilderAmbushTests` | `CalmMutationTests`, `NoFightConversationTests`, `WitnessedEffectTests`, `LookQueryServiceTests` |
| 3.4 Counter-checks | Warden-not-retreating-in-combat; SleepingTroll wakes, Mimic doesn't | Speaker-not-listener, Passive-vs-combatant witness, wall-blocks-LOS |
| 3.5 PlayMode sanity sweep | "Option A" four-scenario sweep earlier in this conversation | M2 four-scenario sweep immediately above |
| 3.6 Manual playtest | `CorneredWarden`, `IgnoredScribe`, `SleepingTroll`, `MimicSurprise` | S1–S6 mapped in conversation; `CalmTestSetup` shipped pre-M2 |
| 4 Parity audit | Per-Goal Verdict table in QUD-PARITY.md | "Post-implementation Qud parity audit (M2)" subsection |
| 5 Post-review | 14-finding review → 14 fixes (commit `3167614`) | 11-finding self-review → 4 fixes (commit `585b73b`) |
| 6 Honesty | Option A results labeled "cannot verify particles" | M2 sweep same bounds stated |
| 7 MCP tooling | refresh/compile/tests/force-refresh discipline | same, + `manage_editor stop` before `run_tests` after Play mode |

**Net outcome:** Phase 6 shipped with 1564 passing tests, 5 of 7 goal
primitives wired to real gameplay triggers, one user-reported bug
(pacified-hostile look label) fixed within the same session, and every
deviation from the original plan documented with cited rationale. The
workflow above is what produced that outcome.

---

## Implementation Priority (Recommended)

1. **Tier 4 polish** (small wins in already-shipped systems): SittingEffect visual indicator, tunable scan frequency, force-move auto-cleanup
2. **Phase 7 — more AIBehaviorPart subclasses**: pulls `HasGoal(string)` into production
3. **Phase 12 — Calendar**: unlocks day/night schedules (huge "lived-in" impact)
4. **Phase 6 — missing goals**: gradually add as content demands them
5. **Phase 10 — debug introspection**: low-cost developer QoL
6. **Phase 9 — opinion system**: refines combat/conversation feel
7. **Phase 14 — combat intelligence**: last big feature
