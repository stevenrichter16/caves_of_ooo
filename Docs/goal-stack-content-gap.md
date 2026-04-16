# Goal Stack Feature: Content & Wiring Gap Analysis

The goal stack feature (Phases 0-4) is **code-complete but content-empty**. The C# classes, event flows, and tests all work, but **none of the Phase 2-4 features are reachable in a running game** because no content/data triggers them.

This document catalogs every gap between "code exists" and "feature actually fires in-game."

---

## The Reachability Problem

Each Phase 2-4 feature has an implicit chain of preconditions. Follow each chain end-to-end:

### Staying (NPCs return home when idle)
```
BoredGoal.TakeAction() sees brain.Staying == true
  ↑
brain.Staying was set somewhere
  ↑
Blueprint param { "Staying": "true" }  OR  code calls brain.Stay(x,y)
  ↑
No blueprint uses this param. No code calls Stay().
  ↑
❌ DEAD CODE
```

### IdleQueryEvent (NPCs sit in chairs)
```
NPC sits in a chair
  ↑
DelegateGoal executes at chair cell
  ↑
MoveToGoal walked NPC to the chair
  ↑
BoredGoal.ScanForIdleOffer found a furniture entity
  ↑
Zone contains entity with ChairPart
  ↑
Zone builder placed a chair entity
  ↑
Chair blueprint exists
  ↑
❌ NO CHAIR BLUEPRINT. NO VILLAGE BUILDER CODE TO PLACE IT.
  AND: NPC must have AllowIdleBehavior tag. NO NPC BLUEPRINT HAS THIS TAG.
```

### A* pathfinding (NPCs navigate around walls)
```
MoveToGoal uses FindPath.Search() for multi-step navigation
  ↑
MoveToGoal was pushed onto the goal stack
  ↑
Something pushed it — WanderGoal, GuardGoal, BoredGoal Staying branch,
BoredGoal WhenBoredReturnToOnce branch, BoredGoal furniture scan
  ↑
NONE of these fire today because their upstream conditions are dead.
BoredGoal only pushes WanderRandomlyGoal, which uses greedy single-step.
  ↑
⚠️ FindPath exists but never runs in production.
```

### AIBoredEvent (custom idle behavior per NPC type)
```
AIBehaviorPart subclass handles "AIBored" event
  ↑
Entity has such a part attached
  ↑
Blueprint declares it  OR  code adds it
  ↑
❌ ZERO concrete AIBehaviorPart subclasses exist.
```

---

## Gap Catalog

Organized by severity.

### 🔴 Blockers — without these, feature is 100% dead

#### B1. No `Chair` or `Bed` entity blueprints
**File:** `Assets/Resources/Content/Blueprints/Objects.json`

`ChairPart` and `BedPart` classes are registered via `EntityFactory.RegisterPartsFromAssembly`, but no blueprint declares them. Calling `EntityFactory.CreateEntity("Chair")` returns null. There is literally no way to create a chair in the game.

**What to add:**
```json
{
  "Name": "Chair",
  "Inherits": "PhysicalObject",
  "Parts": [
    { "Name": "Render", "Params": [
      { "Key": "DisplayName", "Value": "wooden chair" },
      { "Key": "RenderString", "Value": "h" },
      { "Key": "ColorString", "Value": "&w" },
      { "Key": "RenderLayer", "Value": "5" }
    ]},
    { "Name": "Physics", "Params": [
      { "Key": "Solid", "Value": "false" }
    ]},
    { "Name": "Chair" }
  ]
}
```

Same pattern for `Bed` (`RenderString = "="`, `RenderLayer = 5`).

#### B2. No friendly NPC blueprints with `AllowIdleBehavior` tag
**File:** `Assets/Resources/Content/Blueprints/Objects.json`

The only creatures in Objects.json today are `Snapjaws` and `SnapjawHunter` — hostile monsters that would never sit in chairs. The idle furniture system cannot fire.

**What to add:**
```json
{
  "Name": "Villager",
  "Inherits": "Creature",
  "Parts": [
    { "Name": "Render", "Params": [
      { "Key": "DisplayName", "Value": "villager" },
      { "Key": "RenderString", "Value": "v" },
      { "Key": "ColorString", "Value": "&Y" }
    ]},
    { "Name": "Brain", "Params": [
      { "Key": "SightRadius", "Value": "8" },
      { "Key": "Wanders", "Value": "false" },
      { "Key": "WandersRandomly", "Value": "false" },
      { "Key": "Staying", "Value": "true" }
    ]}
  ],
  "Tags": [
    { "Key": "Faction", "Value": "Villagers" },
    { "Key": "AllowIdleBehavior", "Value": "" }
  ]
}
```

#### B3. `VillageBuilder` doesn't spawn NPCs or furniture
**File:** `Assets/Scripts/Gameplay/World/Generation/Builders/VillageBuilder.cs`

Current `BuildRoom()` places walls and floor tiles only. Rooms are empty shells. Even if blueprints existed, nothing would place them.

**What to add:**
- `BuildRoom()` should place 1-3 chairs per building interior (and maybe a bed)
- A `PopulateVillage()` phase that spawns 1 villager per building, calls `brain.Stay(centerX, centerY)` with their assigned home cell
- Turn-manager registration for spawned entities (or verify it happens automatically in bootstrap)

#### B4. Factions.json needs verification
**File:** `Assets/Resources/Content/Data/Factions.json` (or wherever it is)

The hardcoded test default in `FactionManager.Initialize()` includes "Villagers" with positive player reputation. But the production-loaded JSON may not. Villagers must be:
- Registered as a faction
- Not hostile to Player (so the NPCs don't attack the player on sight)
- Hostile to Snapjaws (so NPCs flee/fight raiders)

### 🟡 Critical but less obvious

#### C1. Zone-builder-spawned entities must reach TurnManager
**File:** `Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs:760-774`

`RegisterCreaturesForTurns()` scans `_zone.GetEntitiesWithTag("Creature")` **after** the zone is generated. So if VillageBuilder spawns villagers during zone generation (before `RegisterCreaturesForTurns()` runs), they'll be picked up correctly.

But: if new zones are generated mid-game (player descends into a dungeon), the new zone's creatures need to be registered too. The current GameBootstrap pattern only runs once at startup. Zone transitions need their own registration step.

**Verify:** What happens in `ZoneTransitionSystem` when the player enters a new zone? Are creatures registered? If not, this is a separate bug that blocks any multi-zone game with NPCs.

#### C2. `StartingCell` eager init only happens in bootstrap
**File:** `Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs:771-779`

Eager init only runs in `RegisterCreaturesForTurns()` at game start. Entities spawned mid-game rely on lazy init in `BrainPart.HandleTakeTurn()`. That works, but means `HasStartingCell == false` during the one-tick window before their first TakeTurn.

**Fix:** Have `VillageBuilder` or `PopulateVillage` explicitly call `brain.Stay(x, y)` when placing NPCs, setting both StartingCell and Staying atomically.

#### C3. Owner filtering requires name-to-chair mapping
**File:** `Assets/Scripts/Gameplay/AI/ChairPart.cs`

`ChairPart.Owner` can hold a tag or entity ID to restrict who sits there. For "the innkeeper's chair behind the counter," the chair needs `Owner = "<innkeeper entity ID>"` and the innkeeper needs that ID. This mapping doesn't exist in blueprints — it's a runtime decision during village population.

**Fix:** `PopulateVillage` spawns both the NPC and their owned furniture, then explicitly sets chair's Owner after creation:
```csharp
var innkeeper = factory.CreateEntity("Innkeeper");
var chair = factory.CreateEntity("Chair");
chair.GetPart<ChairPart>().Owner = innkeeper.ID;
zone.AddEntity(innkeeper, counterX, counterY);
zone.AddEntity(chair, chairX, chairY);
innkeeper.GetPart<BrainPart>().Stay(counterX, counterY);
```

#### C4. Persistence across zone unload
**File:** `Assets/Scripts/Gameplay/World/Map/ZoneManager.cs`

`BrainPart._goals` is a private runtime field — it's preserved as long as the zone stays in memory (`ZoneManager.CachedZones`). If the cache evicts the zone, NPCs regenerate from blueprints and lose their goal state, `StartingCell`, and `Occupied` chair references.

**Impact:** A villager who was walking to the inn during your last visit will respawn at their blueprint position next visit. The inn's chair, which they had reserved, will be freshly un-Occupied. Not a crash, just a "long-term memory loss" effect.

**Scope:** Full persistence is a huge undertaking. For MVP, accept session-only persistence. Document it.

### 🟢 Quality of life

#### Q1. Idle scan is O(zone entities) per tick per idle NPC
**File:** `Assets/Scripts/Gameplay/AI/Goals/BoredGoal.cs:140-160`

Every BoredGoal tick scans `zone.GetReadOnlyEntities()` for ChairPart/BedPart matches. With 20 villagers and 200 entities, that's 4000 part checks per tick. Fine for small zones, wasteful if we scale.

**Fix:** Cache furniture entities in `Zone._idleFurniture` list, updated on `AddEntity`/`RemoveEntity`.

#### Q2. 1% per-chair chance is unreliable
Villagers with one chair in the zone take ~100 ticks on average to sit. With the reactivity fix (no WaitGoal blocking), this means 100 TakeTurn calls, which is ~10-20 seconds of game time. That might be too slow or too fast depending on desired pacing.

**Fix:** Make `IdleChance` configurable per-chair AND per-NPC (via a `Restless`/`Social` tag).

#### Q3. No per-chair rate limiting (50-turn cooldown in Qud)
An NPC can sit, stand (due to hostile), then immediately re-query the same chair. Qud's `LastIdleUsed` cooldown prevents this thrashing.

**Fix:** Add `int LastIdleUsed` to ChairPart/BedPart, check `currentTurn - LastIdleUsed >= 50` before offering.

#### Q4. SittingEffect doesn't auto-remove on forced movement
If an NPC is knocked off a chair (pushed, teleported), the SittingEffect persists even though they're no longer on the chair.

**Fix:** SittingEffect handles `AfterMove` event, self-removes when position != chair cell. Or register `BeforeMove` handler to block voluntary movement until standing up.

#### Q5. No visual indicator of sitting state
Players can't tell that an NPC is seated. Compare to BurningEffect's `GetRenderColorOverride() => "&R"`.

**Fix:** `SittingEffect` override `GetRenderColorOverride` or emit a small particle/sprite overlay when rendering a seated creature.

#### Q6. `WanderGoal` does up to 10 A* searches per pick
Expensive if every idle NPC uses WanderGoal.

**Fix:** Cache reachable cells from NPC's position via flood-fill on first wander call, re-use across multiple wanders until zone changes.

---

## Tiered Shopping List: "Lived-In Feel" Roadmap

What's the minimum content work to actually **observe** the goal stack in-game?

### Tier 1: "One villager sits in one chair" (~2 hours)
1. Add `Chair` blueprint to `Objects.json`
2. Add `Villager` blueprint with `AllowIdleBehavior` tag, `Staying = true`
3. Modify `VillageBuilder.BuildRoom()` to place 1 chair per building interior
4. Modify `VillageBuilder.BuildZone()` to spawn 1 villager per building and call `brain.Stay()`
5. Verify `TurnManager.AddEntity` picks them up via `RegisterCreaturesForTurns`

**Observable effect:** Walk into a village, see villagers standing at their assigned cells. Occasionally they walk to a chair, sit for a while, and stand up when you approach.

### Tier 2: "Shop has a shopkeeper who stays at the counter" (~1 day)
1. Add `Bed` blueprint
2. Add `Innkeeper`, `Shopkeeper` variant blueprints with unique entity IDs
3. Refactor VillageBuilder with building-type awareness (Inn, Smithy, Farm)
4. Each building type has a specialized spawn: counter position, bed position, chair with Owner
5. `PopulateVillage()` dispatches to `InnBuilder`, `ShopBuilder`, etc.

**Observable effect:** Each building has a themed inhabitant who stays at their workstation and occasionally uses their owned furniture.

### Tier 3: "Guards patrol the village" (~0.5 days)
1. Add `Guard` blueprint (Faction=Villagers, Tags="Guard")
2. Add an `AIGuard` behavior part that pushes `GuardGoal` on bored
3. Or: configure Guard blueprint to auto-push a GuardGoal at spawn via a one-time initialization
4. VillageBuilder places 1-2 guards at village entrance

**Observable effect:** Guards pace around their assigned post, return to it after combat.

### Tier 4: "NPCs have schedules" (much later)
Requires WorldClock (Phase 12), time-of-day gating, AIShopper/AIPilgrim patterns. Out of scope for making Phase 2-4 work.

---

## One-Line Summary

**The goal stack feature is code-complete but content-empty.** The code will gladly simulate an innkeeper sitting in their chair, returning home at night, and running for help when attacked — but today there are no innkeepers, no chairs, no homes, and no populated villages. The minimum bridge to observable in-game behavior is:

1. **2-3 new blueprints** (Chair, Villager, maybe Bed)
2. **~30 lines in `VillageBuilder`** to place them
3. **Verify turn-manager wiring** for zone-builder-spawned entities
4. **One optional `AIBehaviorPart` subclass** (e.g., `AITavernSitter`) to prove the AIBoredEvent chain end-to-end

Everything else is polish on top of that minimum.
