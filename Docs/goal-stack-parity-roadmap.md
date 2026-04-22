# Goal Stack Parity Roadmap: Caves of Ooo ↔ Caves of Qud

This document tracks every gap between our goal stack implementation and the Qud decompiled source. Items are ordered by implementation priority — dependencies first, then impact.

**Current state:** BrainPart has a goal stack with 10 goal handlers (BoredGoal, KillGoal, FleeGoal, WanderRandomlyGoal, WaitGoal, StepGoal, MoveToGoal, WanderGoal, GuardGoal). All 1109 tests pass. No pathfinding, no bored event, no furniture interaction, no party system, no time system.

---

## Phase 1: Pathfinding Foundation

Without A*, MoveToGoal/GuardGoal/WanderGoal silently fail on non-trivial geometry. Every subsequent feature depends on NPCs being able to navigate reliably.

### 1.1 — FindPath (A* Pathfinding)
- [ ] **`FindPath.cs`** — A* over zone grid (80×25 = 2000 cells)
  - Qud ref: `XRL.World.AI.Pathfinding.FindPath`
  - Heuristic: Chebyshev distance (matches 8-directional movement)
  - Returns `List<string> Directions` and `List<Cell> Steps`
  - `Usable` property: `Found && Steps.Count > 1`
  - Constructor takes start/end zone IDs + coordinates, `PathGlobal`, `PathUnlimited`, `Looker` entity, `MaxWeight`
- [ ] **`CellNavigationValue.cs`** — A* node with Cost, Estimate, Parent, Direction
  - Qud ref: `XRL.World.AI.Pathfinding.CellNavigationValue`
  - Pre-allocated pool of 4000 nodes (`CellNavs[]` static array)
- [ ] **`NavigationWeight`** — per-cell traversal cost
  - Qud ref: `XRL.World.AI.Pathfinding.NavigationWeight`
  - `Cell.NavigationWeight(entity, ref int Nav)` — walls infinite, liquids/gases have variable weight
  - `MaxWeight` parameter on FindPath to control what cells are traversable (default 95, walls 100+)

### 1.2 — MoveToGoal A* Upgrade
- [ ] Rewrite `MoveToGoal.TakeAction()` to use `FindPath` instead of greedy `TryStepToward`
  - Compute path once on first `TakeAction()`, cache as direction queue
  - Push `StepGoal` for each direction in the path
  - Recompute path if stuck for > N turns
  - `careful` flag: avoid cells with hostile creatures (Qud's `MoveTo.careful`)
  - `overridesCombat` flag: `CanFight()` returns `!overridesCombat`
  - `wandering` flag: movement is casual, can be interrupted
  - `shortBy` int: stop N cells short of destination (approach but don't step on)
  - `AbortIfMoreSteps` int: give up if path is too long
  - `MaxWeight` int: navigation weight limit (default 95)
  - `global` flag: cross-zone pathfinding (see Phase 6)

---

## Phase 2: Brain State and Starting Cell

### 2.1 — Brain.StartingCell + Staying
- [ ] Add `BrainPart.StartingCell` field — `(int X, int Y, string ZoneID)` tuple or equivalent
  - Qud ref: `Brain.StartingCell` (type `GlobalLocation`)
  - Set when entity is first placed in a zone (in entity spawning / zone population code)
- [ ] Add `BrainPart.Staying` flag (default false)
  - Qud ref: `Brain.Staying` (bitmask flag `BRAIN_FLAG_STAYING`)
  - When true + not Wanders + not at StartingCell → `BoredGoal` pushes `MoveToGoal(StartingCell)`
- [ ] Add `BrainPart.Stay(Cell C)` method
  - Sets `Staying = true`, updates `StartingCell` to cell position
  - If `!Wanders && !WandersRandomly` → saves cell as return point
- [ ] Add `WhenBoredReturnToOnce` string property on Entity
  - Qud ref: `Bored.TakeAction()` checks `GetStringProperty("WhenBoredReturnToOnce")`
  - Format: `"X,Y"` — walk to those coordinates once, then clear the property
  - `BoredGoal` checks this before scanning for hostiles

### 2.2 — Brain Flags
- [ ] Add `BrainPart.Mobile` flag (default true)
  - Qud ref: `BRAIN_FLAG_MOBILE`
  - Immobile NPCs skip all movement goals; `MoveTo` calls `FailToParent()` if `!IsMobile()`
- [ ] Add `BrainPart.Hibernating` flag (default false)
  - Qud ref: `BRAIN_FLAG_HIBERNATING`
  - Hibernating entities skip TakeTurn entirely until woken by `AIWakeupBroadcast` event
- [ ] Add `BrainPart.Passive` flag (default false)
  - Qud ref: `BRAIN_FLAG_PASSIVE`
  - Passive NPCs don't scan for hostiles; `BoredGoal` skips `FindNearestHostile`
- [ ] Add `BrainPart.Calm` property (part of allegiance)
  - Qud ref: `Brain.Calm` / `BRAIN_FLAG_CALM`
  - Suppresses hostility regardless of faction feelings
- [ ] Add `BrainPart.Aquatic` flag
  - Qud ref: `BRAIN_FLAG_AQUATIC`
  - Movement goals filter for cells with aquatic support
- [ ] Add `BrainPart.LivesOnWalls` flag
  - Qud ref: `BRAIN_FLAG_LIVES_ON_WALLS`
- [ ] Add `BrainPart.WallWalker` flag
  - Qud ref: `BRAIN_FLAG_WALL_WALKER`
- [ ] Add `BrainPart.PointBlankRange` flag
  - Qud ref: `BRAIN_FLAG_POINT_BLANK_RANGE`
  - Ranged AI prefers melee distance
- [ ] Add `BrainPart.DoReequip` flag
  - Qud ref: `BRAIN_FLAG_DO_REEQUIP`
  - Triggers gear evaluation on next TakeTurn
- [ ] Add `BrainPart.NeedToReload` flag
  - Qud ref: `BRAIN_FLAG_NEED_TO_RELOAD`

### 2.3 — BoredGoal Updates
- [ ] Check `WhenBoredReturnToOnce` before scanning hostiles
- [ ] Check `Staying && !Wanders && !WandersRandomly && not at StartingCell` → push `MoveToGoal(StartingCell)`
- [ ] Check `Passive` → skip hostile scan
- [ ] Add `AllowIdleBehavior` tag check (placeholder for Phase 4)
- [ ] Set `ParentBrain.CurrentState = AIState.Idle` when falling through to default

---

## Phase 3: AIBoredEvent System

### 3.1 — AIBoredEvent
- [ ] **`AIBoredEvent.cs`** — event class with `Check(Entity actor)` static method
  - Qud ref: `XRL.World.AIBoredEvent`
  - `Check()` fires both the legacy string event `"AIBored"` and the typed `AIBoredEvent`
  - Returns false if any handler consumed the event (NPC took an action)
  - Fields: `Actor` (the bored entity)
- [ ] **`CanAIDoIndependentBehavior`** — string event fired before independent behaviors
  - Qud ref: used by `AIShopper`, `AIPilgrim`, `TombPatrolBehavior`, `Bored` to check if NPC can act independently
  - Returns false if NPC is a follower or otherwise constrained

### 3.2 — AIBehaviorPart Base
- [ ] **`AIBehaviorPart.cs`** — abstract base extending `Part`
  - Qud ref: `XRL.World.AIBehaviorPart` (empty marker class)
  - Parts that inherit from this register for `AIBoredEvent` and push goals when bored

### 3.3 — BoredGoal Integration
- [ ] After hostile scan fails, fire `AIBoredEvent.Check(ParentEntity)` on the entity
- [ ] If event returns false (consumed), return — a behavior part took over
- [ ] Only proceed to wander/idle if no behavior part handled it

---

## Phase 4: IdleQueryEvent + Furniture Interaction

### 4.1 — IdleQueryEvent
- [ ] **`IdleQueryEvent.cs`** — event class with `Actor` field
  - Qud ref: `XRL.World.IdleQueryEvent`
  - Fired on furniture/interactable objects in the zone
  - Object returns false to indicate it accepted the idle NPC
- [ ] Add `AllowIdleBehavior` entity tag
  - NPCs with this tag participate in idle queries
- [ ] `BoredGoal` idle scan: iterate zone cells, find objects that handle `IdleQueryEvent`, shuffle, query each
  - Qud ref: `Bored.TakeAction()` lines 299-347
  - Zone-wide scan, checks `WantEvent(IdleQueryEvent.ID)`

### 4.2 — DelegateGoal
- [ ] **`DelegateGoal.cs`** — goal that executes a delegate/lambda
  - Qud ref: `XRL.World.AI.GoalHandlers.DelegateGoal`
  - Constructor takes `Action<GoalHandler>` callback
  - `TakeAction()` invokes callback with `this` as parameter
  - Used by `Chair.HandleEvent(IdleQueryEvent)` for "move to chair, then sit"

### 4.3 — Chair Part (IdleQueryEvent handler)
- [ ] **`ChairPart.cs`** — or extend existing furniture part
  - Qud ref: `XRL.World.Parts.Chair`
  - `HandleEvent(IdleQueryEvent)`:
    - Check `Owner` property (faction/name match)
    - 1% chance per idle tick
    - If adjacent: `SitDown(actor)` directly
    - If distant and `Random(1,40) > distance`: push `DelegateGoal` (sit on arrive) + `MoveToGoal` (walk to chair)
    - Rate-limit via `LastIdleUsed` (50 turn cooldown)
  - `SitDown(actor)` — apply `Sitting` effect, move actor to chair cell
  - `StandUp(actor)` — remove `Sitting` effect

### 4.4 — Sitting Effect
- [ ] **`SittingEffect.cs`** — status effect applied when seated
  - Qud ref: `XRL.World.Effects.Sitting`
  - Fields: `SittingOn` (Entity reference to the chair), `Level`, `DamageAttributes`
  - Blocks movement while sitting (handle `BeforeMove` → return false)
  - Removed when standing up or when chair moves/is destroyed

### 4.5 — Bed Part (IdleQueryEvent handler)
- [ ] **`BedPart.cs`** — or extend existing furniture part
  - Qud ref: `XRL.World.Parts.Bed`
  - Similar to Chair but applies sleeping/resting effect
  - `PollForHealingLocationEvent` — NPCs seek beds when injured
  - `UseHealingLocationEvent` — NPC uses bed to heal

### 4.6 — Owner Property on Furniture
- [ ] Furniture entities carry `Owner` string property
  - Values: faction name (e.g., "Villagers"), NPC name (e.g., "Tam"), or empty (public)
  - Chair/Bed check: `owner.IsNullOrEmpty() || actor.HasTagOrProperty(owner) || actor.DisplayName == owner || actor.BelongsToFaction(owner)`

---

## Phase 5: Goal Composition Primitives

### 5.1 — Stack Manipulation Methods on GoalHandler
- [ ] `InsertGoalAfter(GoalHandler after, GoalHandler goal)` — insert goal below another in stack
  - Qud ref: `GoalHandler.InsertGoalAfter`
- [ ] `InsertGoalAsParent(GoalHandler becomesChild, GoalHandler goal)` — insert above a specific goal
  - Qud ref: `GoalHandler.InsertGoalAsParent`
- [ ] `ForceInsertGoalAfter` / `ForceInsertGoalAsParent` — skip `OnPush()` call
- [ ] `InsertChildGoalAfter` — combined child + insert
- [ ] `FindGoal(string typeName)` on BrainPart — find goal by type name in stack
  - Qud ref: `Brain.FindGoal(string)`

### 5.2 — BrainPart Stack Manipulation
- [ ] `BrainPart.InsertGoalAfter(GoalHandler after, GoalHandler goal)` — modify stack in-place
- [ ] `BrainPart.InsertGoalUnder(GoalHandler above, GoalHandler goal)` — insert below
- [ ] Support indexed access to goal stack for debugging
- [ ] `BrainPart.IsBusy()` — check if top goal reports busy
  - Qud ref: `Brain.IsBusy()` — delegates to `Goals.Peek().IsBusy()`

### 5.3 — Command Goal
- [ ] **`CommandGoal.cs`** — fire a named command event on the entity
  - Qud ref: `XRL.World.AI.GoalHandlers.Command`
  - Constructor takes command string
  - `TakeAction()` fires `GameEvent.New(command)` on `ParentEntity`
  - One-shot, finishes immediately

---

## Phase 6: Missing Goal Handlers

### 6.1 — Core Missing Goals
- [ ] **`WanderDurationGoal.cs`** — wander for N turns (vs our one-shot)
  - Qud ref: `XRL.World.AI.GoalHandlers.WanderDuration`
  - Pushes `Wander` child goals repeatedly until duration expires
- [ ] **`FleeLocationGoal.cs`** — flee to a specific cell
  - Qud ref: `XRL.World.AI.GoalHandlers.FleeLocation`
  - Combines flee movement with a destination
- [ ] **`RetreatGoal.cs`** — structured retreat with fallback positions
  - Qud ref: `XRL.World.AI.GoalHandlers.Retreat`
- [ ] **`DormantGoal.cs`** — hibernate until triggered
  - Qud ref: `XRL.World.AI.GoalHandlers.Dormant`
  - For ambush creatures; replaces our GlowmawAmbushPart pattern

### 6.2 — Equipment / Item Goals
- [ ] **`ReequipGoal.cs`** — evaluate and re-equip gear
  - Qud ref: `XRL.World.AI.GoalHandlers.Reequip`
  - `Brain.PerformReequip()` evaluates weapons, armor, shields
  - `CompareWeapons` / `CompareGear` / `CompareMissileWeapons` sorting
- [ ] **`ChangeEquipmentGoal.cs`** — swap specific equipment
  - Qud ref: `XRL.World.AI.GoalHandlers.ChangeEquipment`
- [ ] **`GoFetchGoal.cs`** — walk to object, pick it up, optionally return
  - Qud ref: `XRL.World.AI.GoalHandlers.GoFetch` + `GoFetchGet`
- [ ] **`DisposeOfCorpseGoal.cs`** — clean up dead bodies
  - Qud ref: `XRL.World.AI.GoalHandlers.DisposeOfCorpse`
- [ ] **`DropOffStolenGoodsGoal.cs`** — return stolen items
  - Qud ref: `XRL.World.AI.GoalHandlers.DropOffStolenGoods`

### 6.3 — Movement Goals
- [ ] **`MoveToZoneGoal.cs`** — navigate to a different zone
  - Qud ref: `XRL.World.AI.GoalHandlers.MoveToZone`
  - Find zone exit/entrance, walk to it, transition
- [ ] **`MoveToGlobalGoal.cs`** — world-map-scale travel
  - Qud ref: `XRL.World.AI.GoalHandlers.MoveToGlobal`
  - Requires world map pathfinding
- [ ] **`MoveToExteriorGoal.cs`** — move to outside of a zone
  - Qud ref: `XRL.World.AI.GoalHandlers.MoveToExterior`
- [ ] **`MoveToInteriorGoal.cs`** — move to inside of a building
  - Qud ref: `XRL.World.AI.GoalHandlers.MoveToInterior`
- [ ] **`LandGoal.cs`** — flying creature lands
  - Qud ref: `XRL.World.AI.GoalHandlers.Land`

### 6.4 — Social / Special Goals
- [ ] **`NoFightGoal.cs`** — pacifist override (CanFight → false)
  - Qud ref: `XRL.World.AI.GoalHandlers.NoFightGoal`
- [ ] **`ConfusedGoal.cs`** — random movement while confused
  - Qud ref: `XRL.World.AI.GoalHandlers.Confused`
- [ ] **`ExtinguishSelfGoal.cs`** — NPC on fire tries to put themselves out
  - Qud ref: `XRL.World.AI.GoalHandlers.ExtinguishSelf`
- [ ] **`PetGoal.cs`** — NPC pets nearby creatures
  - Qud ref: `XRL.World.AI.GoalHandlers.Pet`
- [ ] **`GiveATreatToPartyLeaderGoal.cs`** — companion gives item to leader
  - Qud ref: `XRL.World.AI.GoalHandlers.GiveATreatToPartyLeader`

### 6.5 — Quest-Specific Goals
- [ ] **`GoOnAPilgrimageGoal.cs`** — cross-zone journey to holy site
  - Qud ref: `XRL.World.AI.GoalHandlers.GoOnAPilgrimage`
- [ ] **`GoOnAShoppingSpreeGoal.cs`** — walk to merchant area, browse
  - Qud ref: `XRL.World.AI.GoalHandlers.GoOnAShoppingSpree`
  - `TargetZones` list, `LookForMerchant()`, random zone travel
- [ ] **`DustAnUrnGoal.cs`** — specific to Qud's urn-dusting mechanic
  - Qud ref: `XRL.World.AI.GoalHandlers.DustAnUrnGoal`
  - Only implement if equivalent mechanic exists
- [ ] **`TombPatrolGoal.cs`** — cyclic zone patrol
  - Qud ref: `XRL.World.AI.GoalHandlers.TombPatrolGoal`
  - Walk to next zone in a ring pattern (zone coordinate cycling)

### 6.6 — Creature-Specific Goals
- [ ] **`ClonelingGoal.cs`** — behavior for cloned creatures
  - Qud ref: `XRL.World.AI.GoalHandlers.ClonelingGoal`
- [ ] **`GraftekGoal.cs`** / **`GraftekGraftGoal.cs`** — cybernetic grafting AI
  - Qud ref: `XRL.World.AI.GoalHandlers.GraftekGoal`
- [ ] **`MindroneGoal.cs`** — psychic drone behavior
  - Qud ref: `XRL.World.AI.GoalHandlers.MindroneGoal`
- [ ] **`PlaceTurretGoal.cs`** — place a turret
  - Qud ref: `XRL.World.AI.GoalHandlers.PlaceTurretGoal`
- [ ] **`LayMineGoal.cs`** — place a mine
  - Qud ref: `XRL.World.AI.GoalHandlers.LayMineGoal`
- [ ] **`PaxKlanqMadnessGoal.cs`** — specific boss behavior
  - Qud ref: `XRL.World.AI.GoalHandlers.PaxKlanqMadness`
  - Only implement if equivalent boss exists

---

## Phase 7: Concrete AIBehaviorPart Implementations

These are Parts (not goals) that register for `AIBoredEvent` and push goals.

- [ ] **`AIShopper.cs`** — NPC walks to merchant area, browses
  - Qud ref: `XRL.World.Parts.AIShopper`
  - 25% chance on bored, pushes `GoOnAShoppingSpreeGoal`
  - Guards: not a follower, in valid zone, can do independent behavior
- [ ] **`AIShoreLounging.cs`** — aquatic creature goes to shore / returns to water
  - Qud ref: `XRL.World.Parts.AIShoreLounging`
  - `GoToShoreChance` (4%), `GoBackToPoolChance` (10%), `Range` (20)
- [ ] **`AIPilgrim.cs`** — NPC journeys to holy site
  - Qud ref: `XRL.World.Parts.AIPilgrim`
  - One-shot pilgrimage to target zone (configurable destination)
- [ ] **`AIUrnDuster.cs`** — NPC periodically dusts urns
  - Qud ref: `XRL.World.Parts.AIUrnDuster`
  - Uses `WantTurnTick` (not bored event) — checks every turn
  - Only implement if equivalent mechanic exists
- [ ] **`TombPatrolBehavior.cs`** — NPC patrols tomb zones in a ring
  - Qud ref: `XRL.World.Parts.TombPatrolBehavior`
  - On bored: compute next zone in ring, push `TombPatrolGoal`
- [ ] **`AIBarathrumShuttle.cs`** — NPC travels between specific zones
  - Qud ref: `XRL.World.Parts.AIBarathrumShuttle`
  - Only implement if equivalent mechanic exists
- [ ] **`AIWorldMapTravel.cs`** — NPC travels on world map
  - Qud ref: `XRL.World.Parts.AIWorldMapTravel`
  - Requires world map navigation support
- [ ] **`AISitting.cs`** — Apply Sitting effect on first cell entry
  - Qud ref: `XRL.World.Parts.AISitting`
  - One-shot: on `EnteredCell` event, apply `Sitting` effect

---

## Phase 8: Party / Follower System

### 8.1 — Brain Party Fields
- [ ] `BrainPart.PartyLeader` — entity reference (who this NPC follows)
  - Qud ref: `Brain.PartyLeader` (via `LeaderReference`)
- [ ] `BrainPart.PartyMembers` — collection of follower entities
  - Qud ref: `Brain.PartyMembers` (type `PartyCollection`)
- [ ] `BrainPart.IsPlayerLed()` — check if leader chain reaches player
  - Qud ref: `Brain.IsPlayerLed()`

### 8.2 — Follower Behavior
- [ ] `BoredGoal.TakeActionWithPartyLeader()` — full decision tree for companions
  - Qud ref: `Bored.TakeActionWithPartyLeader()` (lines 67-193)
  - Follow leader within distance, pathfind, scan for leader's targets
- [ ] `GetPartyLeaderFollowDistanceEvent` — configurable follow distance
  - Qud ref: default 5 for NPCs, 1 for player-led
- [ ] `BrainPart.GoToPartyLeader()` — pathfind to party leader
  - Qud ref: `Brain.GoToPartyLeader()`
  - Uses A* pathfinding with retry backoff (`NextLeaderPathFind`, `NextLeaderAltPathFind`)

### 8.3 — Ally System
- [ ] **`AllegianceSet.cs`** — flags for allegiance state
  - Qud ref: `XRL.World.AI.AllegianceSet`
  - Contains Hostile, Calm flags as bitmask
- [ ] Ally reason types: Beguile, Proselytize, Bond, Summon, etc.
  - Qud ref: `IAllyReason`, `AllyBeguile`, `AllyBond`, `AllySummon`, etc.
  - Only implement ally types relevant to our game's mechanics

---

## Phase 9: Opinion System

### 9.1 — Full Opinion Map
- [ ] **`OpinionMap.cs`** — dictionary of entity → opinion entries
  - Qud ref: `XRL.World.AI.OpinionMap`
- [ ] **`IOpinion.cs`** — interface for opinion entries
  - Qud ref: `XRL.World.AI.IOpinion`
  - Fields: value (int), source, decay rate
- [ ] Concrete opinion types:
  - [ ] `OpinionAttack` — NPC was attacked by entity
  - [ ] `OpinionKilledAlly` — entity killed NPC's ally
  - [ ] `OpinionFriendlyFire` — entity hit NPC with friendly fire
  - [ ] `OpinionThief` — entity stole from NPC
  - [ ] `OpinionTrespass` — entity trespassed
  - [ ] `OpinionMollify` — positive opinion from dialogue
  - Other opinion types as needed: `OpinionGoad`, `OpinionCoquetry`, `OpinionInscrutable`, `OpinionPoorReasoning`

### 9.2 — Brain Integration
- [ ] Replace `HashSet<Entity> PersonalEnemies` with `OpinionMap Opinions`
  - Keep `PersonalEnemies` as a derived query: entities with opinion <= hostile threshold
- [ ] `Brain.GetFeeling(Entity target)` — compute aggregate feeling from opinions + faction
  - Qud ref: `Brain.GetFeeling()` is more nuanced than our `FactionManager.GetFeeling()`

---

## Phase 10: Debug / Introspection

### 10.1 — AI Thought Logging
- [ ] `BrainPart.Think(string thought)` — log AI decision for debugging
  - Qud ref: `Brain.Think(string)` — stores in `LastThought`, optionally prints
- [ ] `BrainPart.LastThought` — string field, last debug thought
- [ ] Add `Think()` calls throughout goals: `"I'm going to move towards my target."`, `"I'm bored."`, `"I'm guarding this place."`

### 10.2 — Goal Description
- [ ] `GoalHandler.GetDescription()` — human-readable description
  - Qud ref: `GoalHandler.GetDescription()` — returns `"TypeName: details"`
- [ ] `GoalHandler.GetDetails()` — override in subclasses for specifics
  - Qud ref: `GoalHandler.GetDetails()` — e.g., `"target=snapjaw at (5,3)"`
- [ ] Debug command / UI to display NPC goal stack

---

## Phase 11: TurnTick System

### 11.1 — Part-Level Tick
- [ ] `Part.WantTurnTick()` — return true to receive turn ticks independent of TakeTurn
  - Qud ref: `IComponent.WantTurnTick()` (replaces deprecated `WantTenTurnTick`, `WantHundredTurnTick`)
- [ ] `Part.TurnTick(long timeTick, int amount)` — called every turn with elapsed count
  - Qud ref: `IComponent.TurnTick(long, int)` — amount is interval since last call
  - Used by `AIUrnDuster` (checks urns every tick regardless of boredom state)
- [ ] Wire into `TurnManager` — after processing all entity turns, call `TurnTick` on parts that want it

---

## Phase 12: Calendar / World Time

### 12.1 — Calendar System
- [ ] **`Calendar.cs`** — static time abstraction over `TurnManager.TickCount`
  - Qud ref: `XRL.World.Calendar`
  - Constants: `TurnsPerHour = 50`, `TurnsPerDay = 1200`, `TurnsPerYear = 438000`
  - `CurrentDaySegment` — `(TimeTicks % TurnsPerDay) * 10` (0–11990 range)
  - `StartOfDay = 3250`, `StartOfNight = 10000` (segment values)
  - `IsDay()` — segment in [2500, 9124)
  - `GetTime()` — named time periods ("Beetle Moon Zenith", "Harvest Dawn", "High Salt Sun", "Jeweled Dusk", etc.)
  - `GetDay()`, `GetMonth()`, `GetYear()` — calendar date
  - `TotalTimeTicks` — raw tick count from game core

### 12.2 — Daylight Part
- [ ] **`DaylightPart.cs`** — adjusts light level based on time of day
  - Qud ref: `XRL.World.Parts.Daylight`
  - Uses `Calendar.CurrentDaySegment` to compute light intensity (0–80 range)
  - Only affects surface zones (check zone depth)

---

## Phase 13: Zone Lifecycle Integration

### 13.1 — Zone Suspendability
- [ ] **`GetZoneSuspendabilityEvent.cs`** — event that lets goals prevent zone suspension
  - Qud ref: `XRL.World.GetZoneSuspendabilityEvent`
  - `Suspendability` enum: `Active`, `Pinned`, `TooRecentlyActive`, `Suspendable`
  - Goals/parts that need the zone active (e.g., quest scripts) set `Pinned`
- [ ] Wire into `ZoneManager` — query all entities before suspending a zone

### 13.2 — Zone Freeze/Thaw
- [ ] `Zone.Thawed(long frozenTicks)` — called when a frozen zone is restored
  - Qud ref: `Zone.Thawed(long)` → fires `ZoneThawedEvent`
  - Elapsed ticks allow NPCs to "catch up" to current time
- [ ] `ZoneThawedEvent` — parts can handle elapsed time (e.g., advance schedules)

### 13.3 — Zone TurnTick
- [ ] `Zone.TurnTick(long timeTick, int amount)` — per-zone tick for zone-level parts
  - Qud ref: `Zone.TurnTick(long, int)` iterates `IZonePart` list
  - Separate from entity TurnTick — these are zone-wide effects (weather, ambient, etc.)

---

## Phase 14: AI Combat Intelligence

### 14.1 — Weapon Evaluation
- [ ] `BrainPart.CompareWeapons(obj1, obj2, pov)` — which weapon is better
  - Qud ref: `Brain.CompareWeapons()` / `WeaponSorter`
- [ ] `BrainPart.CompareGear(obj1, obj2, pov)` — which armor/gear is better
  - Qud ref: `Brain.CompareGear()` / `GearSorter`
- [ ] `BrainPart.CompareMissileWeapons(obj1, obj2, pov)` — ranged weapon comparison
  - Qud ref: `Brain.CompareMissileWeapons()` / `MissileWeaponSorter`
- [ ] `BrainPart.PerformReequip()` — evaluate all gear and re-equip best
  - Qud ref: `Brain.PerformReequip()` — sorts equipment by quality, equips best

### 14.2 — AI Ability Lists
- [ ] **`AICommandList.cs`** — prioritized list of AI actions
  - Qud ref: `XRL.World.AI.GoalHandlers.AICommandList`
  - `HandleCommandList()` — iterate abilities, try each, return first success
- [ ] `AIGetMovementAbilityListEvent` — query movement abilities (teleport, phase, etc.)
- [ ] `AIGetPassiveAbilityListEvent` — query passive abilities
- [ ] `AIGetPassiveItemListEvent` — query passive item uses

### 14.3 — Target Acquisition
- [ ] `BrainPart.FindProspectiveTarget()` — more nuanced than `FindNearestHostile`
  - Qud ref: `Brain.FindProspectiveTarget()` — considers kill radius, hostile walk radius, leader's target
- [ ] `BrainPart.WantToKill(entity, reason)` — formalized target acquisition
  - Qud ref: `Brain.WantToKill()` — pushes Kill goal with reason string, checks CanFight
- [ ] `BrainPart.MinKillRadius` / `MaxKillRadius` / `HostileWalkRadius` / `MaxWanderRadius`
  - Qud ref: configurable per-NPC behavior radii

---

## Implementation Priority Summary

| Phase | Effort | Impact | Dependency |
|-------|--------|--------|------------|
| 1. A* Pathfinding | Medium | Critical | None |
| 2. Brain State / StartingCell | Small | High | None |
| 3. AIBoredEvent | Small | High | None |
| 4. IdleQueryEvent + Furniture | Medium | High | Phase 3 |
| 5. Goal Composition | Small | Medium | None |
| 6. Missing Goals | Large | Medium | Phase 1 |
| 7. AIBehaviorParts | Medium | High | Phase 3 |
| 8. Party System | Medium | Medium | Phase 1 |
| 9. Opinion System | Medium | Low | None |
| 10. Debug / Introspection | Small | Low | None |
| 11. TurnTick System | Small | Low | None |
| 12. Calendar / World Time | Small | Low | None |
| 13. Zone Lifecycle | Medium | Low | Phase 12 |
| 14. AI Combat Intelligence | Large | Medium | Phase 1 |

---

## Notes

- Phases 1–4 deliver the "lived-in" feel described in the original design proposal.
- Phases 5–8 make the system flexible enough for diverse NPC behaviors.
- Phases 9–14 are completionism — implement as needed by specific game content.
- Quest-specific goals (DustAnUrn, PaxKlanqMadness, etc.) should only be implemented if our game has equivalent mechanics. The pattern is more important than the specific implementations.
- Cross-zone movement (MoveToZone, MoveToGlobal) requires zone transition infrastructure that may not exist yet.
