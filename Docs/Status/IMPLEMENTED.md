# Caves of Ooo - Implementation Record

A faithful remake of Caves of Qud's core systems in Unity 6 (6000.3.4f1).
271 passing NUnit tests. Core simulation has zero Unity dependencies.

---

## Phase 1: Entity System

The foundational ECS-like architecture, faithful to Qud's `GameObject`/`IPart`/`GameEvent` pattern.

### What was implemented

- **Entity** (`Core/Entity.cs`) - Bag of Parts + Stats + Tags + Properties. No behavior of its own. Mirrors Qud's `GameObject`.
- **Part** (`Core/Part.cs`) - Abstract component base. All behavior lives in Parts via `HandleEvent`/`WantEvent`. Mirrors Qud's `IPart`.
- **GameEvent** (`Core/GameEvent.cs`) - String-ID event with typed parameter dictionaries (object, string, int). Pool-friendly `New()` factory. Mirrors Qud's `Event`.
- **Stat** (`Core/Stat.cs`) - Named statistic: `Value = BaseValue + Bonus - Penalty + Boost`, clamped to `[Min, Max]`. Mirrors Qud's `Statistic`.
- **Blueprint** (`Data/Blueprint.cs`) - JSON template with inheritance chain. Holds part configs, stats, tags, properties.
- **BlueprintLoader** (`Data/BlueprintLoader.cs`) - Loads JSON, resolves inheritance via `Bake()`. Parent parts/stats/tags merge into children.
- **EntityFactory** (`Data/EntityFactory.cs`) - Creates entities from blueprints. Uses reflection to instantiate Parts and set properties. `RegisterPartsFromAssembly()` auto-discovers Part subclasses.
- **RenderPart** (`Core/RenderPart.cs`) - Data-only Part holding DisplayName, RenderString, ColorString, RenderLayer, Tile.

### Blueprints defined

Base templates: `PhysicalObject`, `Terrain`, `Wall`, `Item`, `MeleeWeapon`, `ArmorItem`, `Creature`

### Tests

18 tests in `EntitySystemTests.cs` covering entity creation, part add/remove/get, stat arithmetic, tag operations, event firing and handling, blueprint loading, inheritance resolution, and factory creation.

---

## Phase 2: Grid and Rendering

The 80x25 zone grid and Unity tilemap rendering bridge.

### What was implemented

- **Cell** (`Core/Cell.cs`) - Single tile position holding a stack of entities. Methods: `IsSolid()`, `IsWall()`, `IsPassable()`, `GetTopVisibleObject()` (highest RenderLayer).
- **Zone** (`Core/Zone.cs`) - 80x25 grid of Cells. Entity tracking with position lookups. Constants: `Width=80`, `Height=25`. Methods: `AddEntity()`, `RemoveEntity()`, `MoveEntity()`, `GetEntityCell()`, `GetEntitiesWithTag()`.
- **ZoneRenderer** (`Rendering/ZoneRenderer.cs`) - MonoBehaviour that renders Zone onto Unity Tilemap. Reads each cell's top entity's RenderPart, draws glyph in color. Y-axis inverted (cell y=0 is top, Unity y=0 is bottom). Only component bridging simulation to Unity.
- **QudColorParser** (`Rendering/QudColorParser.cs`) - Parses Qud's `&r`, `&G`, `&Y` etc. color codes to Unity Colors. Full 16-color CGA palette.
- **CP437TilesetGenerator** (`Rendering/CP437TilesetGenerator.cs`) - Generates CP437 character tiles at runtime for the tilemap.
- **GameBootstrap** (`Rendering/GameBootstrap.cs`) - MonoBehaviour entry point. Loads blueprints from `Resources/Blueprints/Objects.json`, generates zone, wires renderer/input/turns.

### Scene setup

- `SampleScene.unity`: GameManager (GameBootstrap) -> ZoneGrid (Grid) -> ZoneTilemap (Tilemap + TilemapRenderer + ZoneRenderer)
- Main Camera: orthographic, auto-sized to fit full 80x25 zone
- Global Light 2D for URP

### Tests

20 tests in `GridSystemTests.cs` covering cell creation, entity stacking, solid/wall checks, zone bounds, entity movement, position lookups, tag queries, top visible object selection.

---

## Phase 3: Turn System and Movement

Energy-based turn order and movement with event validation.

### What was implemented

- **TurnManager** (`Core/TurnManager.cs`) - Energy-based turn scheduling faithful to Qud. Each tick, entities gain `Speed` energy. At 1000 threshold, entity gets a turn. Player turn pauses for input (`WaitingForInput` flag). `ProcessUntilPlayerTurn()` runs all NPC turns deterministically.
- **MovementSystem** (`Core/MovementSystem.cs`) - Static class. `TryMove()`/`TryMoveEx()` fire `BeforeMove` event (Parts can block), perform move, fire `AfterMove`. `TryMoveEx` returns `(moved, blockedBy)` for bump-to-attack detection.
- **PhysicsPart** (`Core/PhysicsPart.cs`) - Handles `BeforeMove` event. Checks target cell for Solid entities and blocks movement. Sets `BlockedBy` parameter on the event. Also holds `Solid`, `Weight`, `Takeable`, `InInventory`, `Equipped` flags.
- **InputHandler** (`Rendering/InputHandler.cs`) - MonoBehaviour converting key presses to game commands. Supports WASD, arrow keys, numpad 8-directional, and vi keys (hjklyubn). Rate-limited with `MoveRepeatDelay`. Only place Unity input touches simulation.

### Tests

11 tests in `TurnMovementTests.cs` covering energy accumulation, turn order by speed, movement success/failure, solid blocking, multi-entity turns, wait/skip turn.

---

## Phase 4: Zone Generation

Modular builder pipeline with cellular automata, noise, and population.

### What was implemented

- **IZoneBuilder** (`Core/IZoneBuilder.cs`) - Interface with `BuildZone()`, `Name`, `Priority`. Builders run in priority order.
- **ZoneGenerationPipeline** (`Core/ZoneGenerationPipeline.cs`) - Sorts builders by priority, runs sequentially, retries on failure (up to 5 attempts with new seeds). `CreateCavePipeline()` factory method.
- **CellularAutomata** (`Core/CellularAutomata.cs`) - Qud-faithful CA: `SeedChance=55`, `BornList=[6,7,8]`, `SurviveList=[5,6,7,8]`, `SeedBorders=true`, `BorderDepth=2`. Creates natural cave walls near edges.
- **SimpleNoise** (`Core/SimpleNoise.cs`) - 2D value noise with cosine interpolation and octave layering. Used for terrain variation overlay.
- **CaveBuilder** (`Core/Builders/CaveBuilder.cs`) - Priority 2000. Fills with walls, runs CA + noise, carves open spaces. Floor/Rubble placement with 80/15/5 distribution.
- **BorderBuilder** (`Core/Builders/BorderBuilder.cs`) - Priority 1000. Places wall entities on zone perimeter. (Removed from overworld pipelines in Phase 9 to match Qud's seamless edges.)
- **ConnectivityBuilder** (`Core/Builders/ConnectivityBuilder.cs`) - Priority 3000. Flood-fill to find reachable area, carve corridors to connect islands. Path widening at 75% chance. Ensures passable cells on all 4 zone edges for zone transitions (like Qud's ForceConnections + CaveMouth builders).
- **PopulationBuilder** (`Core/Builders/PopulationBuilder.cs`) - Priority 4000. Spawns entities from a PopulationTable into random passable cells.
- **PopulationTable** (`Data/PopulationTable.cs`) - Weighted encounter table. `Roll()` produces blueprint name list. Guaranteed MinCount + weight-based chance for extras. Factory methods: `CaveTier1()`, `DesertTier1()`, `JungleTier1()`, `RuinsTier1()`.
- **ZoneManager** (`Core/ZoneManager.cs`) - Zone caching and generation. `GetZone()` generates on first request, returns cached thereafter. `SetActiveZone()`, `UnloadZone()`. `GetPipelineForZone()` is `protected virtual` for subclass routing.

### Blueprints defined

Terrain: `Floor`, `Rubble`, `Stalagmite`
Walls: `Wall`

### Tests

18 tests in `ZoneGenerationTests.cs` covering CA generation, noise field properties, cave builder output, pipeline retry, connectivity, population spawning, zone manager caching.

---

## Phase 5: Combat

Melee combat with Qud-faithful hit/penetration/damage formulas.

### What was implemented

- **CombatSystem** (`Core/CombatSystem.cs`) - Static class. Full Qud melee combat:
  - **Hit roll**: `1d20 + AgilityMod + HitBonus >= DV` (defender's dodge value)
  - **Penetration**: Up to 3 rolls of `1d8 + PV vs AV` with diminishing returns (each subsequent roll is harder)
  - **Damage**: Per penetration, roll weapon's `BaseDamage` dice
  - **Death**: When HP <= 0, fires `Died` event, removes entity from zone
  - Events fired: `BeforeMeleeAttack`, `TakeDamage`, `Died`
- **MeleeWeaponPart** (`Core/MeleeWeaponPart.cs`) - Holds `BaseDamage` (dice string), `PenBonus`, `HitBonus`, `MaxStrengthBonus`, `Stat` (governing attribute).
- **ArmorPart** (`Core/ArmorPart.cs`) - Holds `AV` (armor value), `DV` (dodge value), `SpeedPenalty`.
- **DiceRoller** (`Core/DiceRoller.cs`) - Parses and rolls `"NdS+M"` expressions. Supports `1d4`, `2d6+3`, `1d8-1`, etc.
- **StatUtils** (`Core/StatUtils.cs`) - `GetModifier()`: Qud formula `(score - 16) / 2`.
- **MessageLog** (`Core/MessageLog.cs`) - Combat message sink. `OnMessage` callback wired to `Debug.Log` by GameBootstrap.
- **Bump-to-attack** in InputHandler: When movement is blocked by a Creature, automatically performs melee attack.

### Blueprints defined

Weapons: `Dagger` (1d4, 4 weight, Hand slot), `LongSword` (1d8, 8 weight, Hand slot)
Armor: `LeatherArmor` (AV:3, DV:-1, Body slot), `ChainMail` (AV:5, DV:-2, Body slot)

### Tests

32 tests in `CombatSystemTests.cs` covering hit/miss, DV calculation, AV calculation, penetration rolls, damage application, death handling, weapon stat bonuses, unarmed combat, message logging.

---

## Phase 6: Inventory and Items

Full inventory with pickup, drop, equip, unequip, and stat bonuses.

### What was implemented

- **InventoryPart** (`Core/InventoryPart.cs`) - Item container on entities. `MaxWeight` limit, `Objects` list, `EquippedItems` dict (slot -> entity). Methods: `AddObject()`, `RemoveObject()`, `Equip()`, `Unequip()`, `GetEquipped()`, `GetCarriedWeight()`.
- **EquippablePart** (`Core/EquippablePart.cs`) - Marks items as equippable. `Slot` (Hand, Body, Head, etc.), `EquipBonuses` dict for stat modifications on equip.
- **InventorySystem** (`Core/InventorySystem.cs`) - Static operations:
  - `Pickup()` - Remove from zone, add to inventory (weight check)
  - `Drop()` - Remove from inventory, add to zone at entity's feet
  - `Equip()` - Move from inventory to equipment slot, apply stat bonuses
  - `Unequip()` - Move from slot to inventory, remove stat bonuses
  - `GetTakeableItemsAtFeet()` - Query items at entity's cell
  - Full event chain: `BeforePickup`/`AfterPickup`, `BeforeDrop`/`AfterDrop`, `BeforeEquip`/`AfterEquip`, `BeforeUnequip`/`AfterUnequip`
- **G key pickup** in InputHandler: Picks up first takeable item at feet, auto-equips if slot is empty.

### Tests

46 tests in `InventorySystemTests.cs` covering pickup, drop, equip, unequip, weight limits, slot management, stat bonuses on equip/unequip, auto-equip, stacking, event cancellation.

---

## Phase 7: Mutations

Activated abilities with cooldowns, plus passive mutations.

### What was implemented

- **ActivatedAbility** (`Core/ActivatedAbility.cs`) - Data class: GUID ID, DisplayName, Command string, CooldownRemaining, MaxCooldown. `IsUsable` when cooldown is 0.
- **ActivatedAbilitiesPart** (`Core/ActivatedAbilitiesPart.cs`) - Manages ability list on an entity. `AddAbility()`, `RemoveAbility()`, `GetAbilityBySlot()`, `TickCooldowns()` (called each turn).
- **BaseMutation** (`Core/Mutations/BaseMutation.cs`) - Abstract Part base. `Mutate()`/`Unmutate()` lifecycle. Helper methods for registering activated abilities and managing cooldowns.
- **MutationsPart** (`Core/MutationsPart.cs`) - Container Part. `AddMutation()`, `RemoveMutation()`, `GetMutation<T>()`. Parses `StartingMutations` string (`"ClassName:Level,..."`) using reflection.
- **FlamingHandsMutation** (`Core/Mutations/FlamingHandsMutation.cs`) - Physical mutation. Activated ability with 10-turn cooldown. Deals `Level * 1d4` fire damage to all creatures in target adjacent cell. Fires `CommandFlamingHands` event, handles via `TakeDamage`.
- **TelepathyMutation** (`Core/Mutations/TelepathyMutation.cs`) - Mental mutation. Passive: grants `+Level/2` (min 1) Ego stat bonus on mutate.
- **RegenerationMutation** (`Core/Mutations/RegenerationMutation.cs`) - Physical mutation. Passive: heals `Level` HP per turn on `TakeTurn` event.
- **Ability input** (keys 1-5) in InputHandler: Press number key, then direction to target. Escape cancels. Fires ability's Command event with TargetCell, Zone, RNG parameters.

### Blueprints defined

Player blueprint has `ActivatedAbilities` and `Mutations` parts with `StartingMutations: "FlamingHandsMutation:1"`.

### Tests

44 tests in `MutationSystemTests.cs` covering ability creation, cooldown ticking, slot assignment, mutation add/remove, flaming hands damage, telepathy stat bonus, regeneration healing, mutations part lifecycle, starting mutations parsing.

---

## Phase 8: Factions and AI

Faction relationship system and creature AI with line-of-sight.

### What was implemented

- **FactionManager** (`Core/FactionManager.cs`) - Static global registry. Factions: Humanoids, Snapjaws, Apes, Robots, Fungi, etc. Faction feelings: `SetFactionFeeling(A, B, value)`. Thresholds: hostile at -10, allied at +50. `GetFeeling()` resolves entity→faction→target feeling chain.
- **AIHelpers** (`Core/AIHelpers.cs`) - Static spatial utilities:
  - `ChebyshevDistance()` - 8-directional distance
  - `StepToward()`/`StepAway()` - Greedy 1-step movement
  - `HasLineOfSight()` - Bresenham's line algorithm checking for solid/wall blockers
  - `FindNearestHostile()` - Scans zone for hostile creatures within radius using faction feelings
  - `FindHostilesInRadius()` - Returns all hostiles in range
  - `RandomPassableDirection()` - For wandering
- **BrainPart** (`Core/BrainPart.cs`) - AI state machine on `TakeTurn` event:
  - **Idle**: Check for hostiles in sight range. If found, switch to Chase.
  - **Wander**: Random cardinal movement. Periodically check for hostiles.
  - **Chase**: Step toward target each turn. Attack if adjacent (bump-to-attack via MovementSystem). Lose target if out of sight range.
  - Properties: `SightRadius`, `Wanders`, `WandersRandomly`, `CurrentZone`, `Rng`

### Blueprints modified

`Creature` base blueprint: added `Brain` part (SightRadius=10, Wanders=true, WandersRandomly=true).
`SnapjawHunter`: Brain SightRadius override to 15.
All Snapjaw variants: faction tag `Snapjaws`.

### Tests

41 tests in `FactionAITests.cs` covering faction registration, feeling queries, hostility/alliance thresholds, line-of-sight, distance calculations, step-toward/away, hostile finding, brain state transitions, chase behavior, wander behavior, combat engagement.

---

## Phase 9: World Map and Zone Transitions

10x10 overworld with 4 biomes and seamless zone edge transitions.

### What was implemented

- **WorldMap** (`Core/WorldMap.cs`) - `BiomeType` enum: Cave, Desert, Jungle, Ruins. 10x10 grid of tiles. Utilities: `ToZoneID(x,y)` → `"Overworld.5.3"`, `FromZoneID()`, `IsOverworldZoneID()`, `GetAdjacentZoneID()` (null at world edges), `GetBiome()`, `InBounds()`.
- **WorldGenerator** (`Core/WorldGenerator.cs`) - Static `Generate(seed)`. Uses `SimpleNoise.GenerateField(10, 10, rng, octaves: 2)` for biome assignment. Noise thresholds: [0, 0.25)=Cave, [0.25, 0.50)=Desert, [0.50, 0.75)=Jungle, [0.75, 1.0]=Ruins. Center (5,5) forced to Cave. Post-process ensures all 4 biomes present.
- **ZoneTransitionSystem** (`Core/ZoneTransitionSystem.cs`) - Static class, faithful to Qud:
  - `GetTransitionDirection()` - Detects which edge the player exits
  - `GetArrivalPosition()` - Wraps to exact opposite edge: East→(0,y), West→(79,y), South→(x,0), North→(x,24)
  - `TransitionPlayer()` - Full transition: compute adjacent zone ID, get/generate zone, spiral search for passable arrival cell, transfer player between zones
  - No border walls. Transitions trigger when player moves out of bounds (like Qud's `Cell.GetCellFromDirectionGlobal`).
- **OverworldZoneManager** (`Core/OverworldZoneManager.cs`) - Extends ZoneManager. Generates WorldMap from seed. Routes zone IDs through biome lookup for pipeline selection:
  - Cave: CaveBuilder → ConnectivityBuilder → PopulationBuilder(CaveTier1)
  - Desert: DesertBuilder → ConnectivityBuilder → PopulationBuilder(DesertTier1)
  - Jungle: JungleBuilder → ConnectivityBuilder → PopulationBuilder(JungleTier1)
  - Ruins: RuinsBuilder → ConnectivityBuilder → PopulationBuilder(RuinsTier1)
- **DesertBuilder** (`Core/Builders/DesertBuilder.cs`) - Priority 2000. Mostly open Sand with noise-based SandstoneWall clusters (threshold 0.85) and scattered Rock (5%).
- **JungleBuilder** (`Core/Builders/JungleBuilder.cs`) - Priority 2000. CA-based (SeedChance=48, more open than caves). Grass floors, VineWall walls, 10% Tree scatter.
- **RuinsBuilder** (`Core/Builders/RuinsBuilder.cs`) - Priority 2000. Fills with StoneWall, carves 4-8 rectangular rooms (size 4-12), connects with L-shaped corridors. 15% Rubble in corridors.
- **CameraFollow** (`Rendering/CameraFollow.cs`) - Positions camera to show entire 80x25 zone at once (Qud-style fixed screen). Auto-sizes orthographic camera. Snaps on zone transition.
- **Zone transition wiring** in InputHandler: When `TryMoveEx` returns `(false, null)` (out of bounds), detect direction, call `TransitionPlayer`, rewire TurnManager/BrainParts/ZoneRenderer/CameraFollow for new zone.
- **Edge connectivity** added to ConnectivityBuilder: Carves passable paths to at least one cell on each zone edge, like Qud's ForceConnections + CaveMouth builders.

### Blueprints defined

Terrain: `Sand` (. &W), `Grass` (. &g), `StoneFloor` (. &y)
Walls: `SandstoneWall` (# &W), `VineWall` (# &G), `StoneWall` (# &w)
Obstacles: `Rock` (o &y, solid), `Tree` (T &G, solid)

### Tests

41 tests in `WorldMapTests.cs` covering zone ID parsing, adjacency, world generation determinism, biome coverage, transition detection, arrival positions, player transfer, biome builders, overworld routing, full integration transitions.

---

## Architecture Summary

### Design rules

- **Core simulation has zero Unity dependencies.** Entity, Part, Event, Stat, Zone, Combat, AI, WorldMap — all pure C#. Only `Rendering/` touches MonoBehaviours.
- **Event-driven behavior.** Parts communicate exclusively through GameEvent. No direct Part-to-Part coupling.
- **Reflection-based factory.** EntityFactory discovers Part types at runtime, instantiates from JSON blueprints.
- **Modular zone generation.** IZoneBuilder pipeline with priority ordering and retry. Subclass ZoneManager to route custom pipelines.

### File counts

| Directory | Files | Purpose |
|-----------|-------|---------|
| `Scripts/Core/` | 20 | Simulation engine |
| `Scripts/Core/Builders/` | 7 | Zone generation builders |
| `Scripts/Core/Mutations/` | 3 | Mutation implementations |
| `Scripts/Data/` | 4 | Blueprint loading, entity factory |
| `Scripts/Rendering/` | 6 | Unity bridge (rendering, input, bootstrap) |
| `Tests/EditMode/` | 9 | NUnit test suites |
| `Resources/Blueprints/` | 1 | Blueprint JSON data |

### Test coverage

271 passing tests across 9 test files. All core systems have dedicated test suites running in Unity's EditMode test runner.

### All blueprint names

**Templates:** PhysicalObject, Terrain, Wall, Item, MeleeWeapon, ArmorItem, Creature
**Creatures:** Player, Snapjaw, SnapjawScavenger, SnapjawHunter
**Weapons:** Dagger, LongSword
**Armor:** LeatherArmor, ChainMail
**Terrain:** Floor, Rubble, Sand, Grass, StoneFloor
**Walls:** Wall, SandstoneWall, VineWall, StoneWall
**Obstacles:** Stalagmite, Rock, Tree
