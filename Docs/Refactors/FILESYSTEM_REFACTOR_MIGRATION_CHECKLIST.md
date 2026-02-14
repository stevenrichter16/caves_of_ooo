# Filesystem Refactor Migration Checklist

This checklist is the concrete migration plan for the folder structure refactor. It is ordered to minimize breakage and make rollback straightforward.

## Phase 0 - Preflight

- [ ] Commit or stash current changes.
- [ ] Close Unity before bulk `git mv` operations.
- [ ] Create a safety branch: `git checkout -b codex/filesystem-refactor-migration`.

## Phase 1 - Create Target Folders (in this order)

```bash
mkdir -p Docs/Design/Mechanics
mkdir -p Docs/Plans
mkdir -p Docs/Refactors
mkdir -p Docs/Status

mkdir -p Assets/Content/Narrative
mkdir -p Assets/Resources/Content/Blueprints
mkdir -p Assets/Scenes/Main

mkdir -p Assets/Scripts/Gameplay/Abilities
mkdir -p Assets/Scripts/Gameplay/AI
mkdir -p Assets/Scripts/Gameplay/Anatomy
mkdir -p Assets/Scripts/Gameplay/Combat
mkdir -p Assets/Scripts/Gameplay/Economy
mkdir -p Assets/Scripts/Gameplay/Entities
mkdir -p Assets/Scripts/Gameplay/Events
mkdir -p Assets/Scripts/Gameplay/Inventory/Commands/Acquisition
mkdir -p Assets/Scripts/Gameplay/Inventory/Commands/Actions
mkdir -p Assets/Scripts/Gameplay/Inventory/Commands/Disposition
mkdir -p Assets/Scripts/Gameplay/Inventory/Commands/Equipment
mkdir -p Assets/Scripts/Gameplay/Inventory/Planning
mkdir -p Assets/Scripts/Gameplay/Inventory/Rules
mkdir -p Assets/Scripts/Gameplay/Items
mkdir -p Assets/Scripts/Gameplay/Mutations
mkdir -p Assets/Scripts/Gameplay/Stats
mkdir -p Assets/Scripts/Gameplay/Turns
mkdir -p Assets/Scripts/Gameplay/World/Generation/Builders
mkdir -p Assets/Scripts/Gameplay/World/Map

mkdir -p Assets/Scripts/Presentation/Bootstrap
mkdir -p Assets/Scripts/Presentation/Cameras
mkdir -p Assets/Scripts/Presentation/Input
mkdir -p Assets/Scripts/Presentation/Rendering
mkdir -p Assets/Scripts/Presentation/UI

mkdir -p Assets/Scripts/Data/Blueprints
mkdir -p Assets/Scripts/Data/Factories
mkdir -p Assets/Scripts/Data/Tables
mkdir -p Assets/Scripts/Shared/Utilities

mkdir -p Assets/Tests/EditMode/Gameplay/AI
mkdir -p Assets/Tests/EditMode/Gameplay/Anatomy
mkdir -p Assets/Tests/EditMode/Gameplay/Combat
mkdir -p Assets/Tests/EditMode/Gameplay/Entities
mkdir -p Assets/Tests/EditMode/Gameplay/Inventory
mkdir -p Assets/Tests/EditMode/Gameplay/Mutations
mkdir -p Assets/Tests/EditMode/Gameplay/World/Generation
mkdir -p Assets/Tests/EditMode/Gameplay/World/Map
mkdir -p Assets/Tests/EditMode/Gameplay/World/Movement
```

## Phase 2 - Move Docs and Content Assets

### 2.1 Root docs -> `Docs/*`

```bash
git mv BODY_AND_ANATOMY.md Docs/Design/Mechanics/BODY_AND_ANATOMY.md
git mv BODY_PART_SYSTEM.md Docs/Design/Mechanics/BODY_PART_SYSTEM.md
git mv COMBAT_AND_PHYSICS.md Docs/Design/Mechanics/COMBAT_AND_PHYSICS.md
git mv COOKING_AND_GATHERING.md Docs/Design/Mechanics/COOKING_AND_GATHERING.md
git mv CYBERNETICS.md Docs/Design/Mechanics/CYBERNETICS.md
git mv FLOORS.md Docs/Design/Mechanics/FLOORS.md
git mv INVENTORY.md Docs/Design/Mechanics/INVENTORY.md
git mv LIQUIDS.md Docs/Design/Mechanics/LIQUIDS.md
git mv NPCs.md Docs/Design/Mechanics/NPCs.md
git mv QUESTS_AND_JOURNAL.md Docs/Design/Mechanics/QUESTS_AND_JOURNAL.md
git mv STATUS_EFFECTS.md Docs/Design/Mechanics/STATUS_EFFECTS.md
git mv TINKERING_AND_CRAFTING.md Docs/Design/Mechanics/TINKERING_AND_CRAFTING.md
git mv TRADE_AND_ECONOMY.md Docs/Design/Mechanics/TRADE_AND_ECONOMY.md
git mv XP_LEVELLING.md Docs/Design/Mechanics/XP_LEVELLING.md

git mv COO_MUTATION_SYSTEM_IMPLEMENTATION_PLAN.md Docs/Plans/COO_MUTATION_SYSTEM_IMPLEMENTATION_PLAN.md
git mv COO_XP_LEVELLING_DEPENDENCY_IMPLEMENTATION_PLAN.md Docs/Plans/COO_XP_LEVELLING_DEPENDENCY_IMPLEMENTATION_PLAN.md

git mv DESIGN_REFACTOR.md Docs/Refactors/DESIGN_REFACTOR.md
git mv INVENTORY_REFACTOR_DEFINITION_OF_DONE.md Docs/Refactors/INVENTORY_REFACTOR_DEFINITION_OF_DONE.md

git mv IMPLEMENTED.md Docs/Status/IMPLEMENTED.md
```

### 2.2 Content assets

```bash
git mv Assets/Narrative/mycelium.txt Assets/Content/Narrative/mycelium.txt
git mv Assets/Narrative/mycelium.txt.meta Assets/Content/Narrative/mycelium.txt.meta

git mv Assets/Resources/Blueprints/Objects.json Assets/Resources/Content/Blueprints/Objects.json
git mv Assets/Resources/Blueprints/Objects.json.meta Assets/Resources/Content/Blueprints/Objects.json.meta
git mv Assets/Resources/Blueprints/Mutations.json Assets/Resources/Content/Blueprints/Mutations.json
git mv Assets/Resources/Blueprints/Mutations.json.meta Assets/Resources/Content/Blueprints/Mutations.json.meta

git mv Assets/Scenes/SampleScene.unity Assets/Scenes/Main/SampleScene.unity
git mv Assets/Scenes/SampleScene.unity.meta Assets/Scenes/Main/SampleScene.unity.meta
```

## Phase 3 - Move Script Files

Note: `Assets/Scripts/CavesOfOoo.asmdef` stays at `Assets/Scripts/CavesOfOoo.asmdef`.

### 3.1 Core gameplay files

```bash
git mv Assets/Scripts/Core/AIHelpers.cs Assets/Scripts/Gameplay/AI/AIHelpers.cs
git mv Assets/Scripts/Core/ActivatedAbilitiesPart.cs Assets/Scripts/Gameplay/Abilities/ActivatedAbilitiesPart.cs
git mv Assets/Scripts/Core/ActivatedAbility.cs Assets/Scripts/Gameplay/Abilities/ActivatedAbility.cs
git mv Assets/Scripts/Core/ArmorPart.cs Assets/Scripts/Gameplay/Items/ArmorPart.cs
git mv Assets/Scripts/Core/BaseMutation.cs Assets/Scripts/Gameplay/Mutations/BaseMutation.cs
git mv Assets/Scripts/Core/Body.cs Assets/Scripts/Gameplay/Anatomy/Body.cs
git mv Assets/Scripts/Core/BrainPart.cs Assets/Scripts/Gameplay/AI/BrainPart.cs
git mv Assets/Scripts/Core/Cell.cs Assets/Scripts/Gameplay/World/Map/Cell.cs
git mv Assets/Scripts/Core/CellularAutomata.cs Assets/Scripts/Gameplay/World/Generation/CellularAutomata.cs
git mv Assets/Scripts/Core/CombatSystem.cs Assets/Scripts/Gameplay/Combat/CombatSystem.cs
git mv Assets/Scripts/Core/CommercePart.cs Assets/Scripts/Gameplay/Economy/CommercePart.cs
git mv Assets/Scripts/Core/ContainerPart.cs Assets/Scripts/Gameplay/Items/ContainerPart.cs
git mv Assets/Scripts/Core/DiceRoller.cs Assets/Scripts/Shared/Utilities/DiceRoller.cs
git mv Assets/Scripts/Core/Entity.cs Assets/Scripts/Gameplay/Entities/Entity.cs
git mv Assets/Scripts/Core/EquippablePart.cs Assets/Scripts/Gameplay/Items/EquippablePart.cs
git mv Assets/Scripts/Core/FactionManager.cs Assets/Scripts/Gameplay/AI/FactionManager.cs
git mv Assets/Scripts/Core/FoodPart.cs Assets/Scripts/Gameplay/Items/FoodPart.cs
git mv Assets/Scripts/Core/GameEvent.cs Assets/Scripts/Gameplay/Events/GameEvent.cs
git mv Assets/Scripts/Core/InventoryAction.cs Assets/Scripts/Gameplay/Inventory/InventoryAction.cs
git mv Assets/Scripts/Core/InventoryPart.cs Assets/Scripts/Gameplay/Inventory/InventoryPart.cs
git mv Assets/Scripts/Core/InventoryScreenData.cs Assets/Scripts/Gameplay/Inventory/InventoryScreenData.cs
git mv Assets/Scripts/Core/InventorySystem.cs Assets/Scripts/Gameplay/Inventory/InventorySystem.cs
git mv Assets/Scripts/Core/ItemCategory.cs Assets/Scripts/Gameplay/Items/ItemCategory.cs
git mv Assets/Scripts/Core/MeleeWeaponPart.cs Assets/Scripts/Gameplay/Items/MeleeWeaponPart.cs
git mv Assets/Scripts/Core/MessageLog.cs Assets/Scripts/Gameplay/Events/MessageLog.cs
git mv Assets/Scripts/Core/MovementSystem.cs Assets/Scripts/Gameplay/Turns/MovementSystem.cs
git mv Assets/Scripts/Core/MutationsPart.cs Assets/Scripts/Gameplay/Mutations/MutationsPart.cs
git mv Assets/Scripts/Core/OverworldZoneManager.cs Assets/Scripts/Gameplay/World/Map/OverworldZoneManager.cs
git mv Assets/Scripts/Core/Part.cs Assets/Scripts/Gameplay/Entities/Part.cs
git mv Assets/Scripts/Core/PhysicsPart.cs Assets/Scripts/Gameplay/Combat/PhysicsPart.cs
git mv Assets/Scripts/Core/RenderPart.cs Assets/Scripts/Gameplay/Entities/RenderPart.cs
git mv Assets/Scripts/Core/SimpleNoise.cs Assets/Scripts/Gameplay/World/Generation/SimpleNoise.cs
git mv Assets/Scripts/Core/StackerPart.cs Assets/Scripts/Gameplay/Items/StackerPart.cs
git mv Assets/Scripts/Core/Stat.cs Assets/Scripts/Gameplay/Stats/Stat.cs
git mv Assets/Scripts/Core/StatUtils.cs Assets/Scripts/Gameplay/Stats/StatUtils.cs
git mv Assets/Scripts/Core/TonicPart.cs Assets/Scripts/Gameplay/Items/TonicPart.cs
git mv Assets/Scripts/Core/TradeSystem.cs Assets/Scripts/Gameplay/Economy/TradeSystem.cs
git mv Assets/Scripts/Core/TurnManager.cs Assets/Scripts/Gameplay/Turns/TurnManager.cs
git mv Assets/Scripts/Core/WorldGenerator.cs Assets/Scripts/Gameplay/World/Generation/WorldGenerator.cs
git mv Assets/Scripts/Core/WorldMap.cs Assets/Scripts/Gameplay/World/Map/WorldMap.cs
git mv Assets/Scripts/Core/Zone.cs Assets/Scripts/Gameplay/World/Map/Zone.cs
git mv Assets/Scripts/Core/ZoneGenerationPipeline.cs Assets/Scripts/Gameplay/World/Generation/ZoneGenerationPipeline.cs
git mv Assets/Scripts/Core/ZoneManager.cs Assets/Scripts/Gameplay/World/Map/ZoneManager.cs
git mv Assets/Scripts/Core/ZoneTransitionSystem.cs Assets/Scripts/Gameplay/World/ZoneTransitionSystem.cs
```

### 3.2 Anatomy folder

```bash
git mv Assets/Scripts/Core/Anatomy/AnatomyFactory.cs Assets/Scripts/Gameplay/Anatomy/AnatomyFactory.cs
git mv Assets/Scripts/Core/Anatomy/BodyPart.cs Assets/Scripts/Gameplay/Anatomy/BodyPart.cs
git mv Assets/Scripts/Core/Anatomy/BodyPartCategory.cs Assets/Scripts/Gameplay/Anatomy/BodyPartCategory.cs
git mv Assets/Scripts/Core/Anatomy/BodyPartType.cs Assets/Scripts/Gameplay/Anatomy/BodyPartType.cs
git mv Assets/Scripts/Core/Anatomy/Laterality.cs Assets/Scripts/Gameplay/Anatomy/Laterality.cs
git mv Assets/Scripts/Core/Anatomy/NaturalWeaponFactory.cs Assets/Scripts/Gameplay/Anatomy/NaturalWeaponFactory.cs
git mv Assets/Scripts/Core/Anatomy/SeveredLimbFactory.cs Assets/Scripts/Gameplay/Anatomy/SeveredLimbFactory.cs
git mv Assets/Scripts/Core/Anatomy/SeveredLimbPart.cs Assets/Scripts/Gameplay/Anatomy/SeveredLimbPart.cs
```

### 3.3 World builders

```bash
git mv Assets/Scripts/Core/IZoneBuilder.cs Assets/Scripts/Gameplay/World/Generation/Builders/IZoneBuilder.cs
git mv Assets/Scripts/Core/Builders/BorderBuilder.cs Assets/Scripts/Gameplay/World/Generation/Builders/BorderBuilder.cs
git mv Assets/Scripts/Core/Builders/CaveBuilder.cs Assets/Scripts/Gameplay/World/Generation/Builders/CaveBuilder.cs
git mv Assets/Scripts/Core/Builders/ConnectivityBuilder.cs Assets/Scripts/Gameplay/World/Generation/Builders/ConnectivityBuilder.cs
git mv Assets/Scripts/Core/Builders/DesertBuilder.cs Assets/Scripts/Gameplay/World/Generation/Builders/DesertBuilder.cs
git mv Assets/Scripts/Core/Builders/JungleBuilder.cs Assets/Scripts/Gameplay/World/Generation/Builders/JungleBuilder.cs
git mv Assets/Scripts/Core/Builders/PopulationBuilder.cs Assets/Scripts/Gameplay/World/Generation/Builders/PopulationBuilder.cs
git mv Assets/Scripts/Core/Builders/RuinsBuilder.cs Assets/Scripts/Gameplay/World/Generation/Builders/RuinsBuilder.cs
```

### 3.4 Inventory command/runtime files

```bash
git mv Assets/Scripts/Core/Inventory/IInventoryCommand.cs Assets/Scripts/Gameplay/Inventory/IInventoryCommand.cs
git mv Assets/Scripts/Core/Inventory/InventoryCommandExecutor.cs Assets/Scripts/Gameplay/Inventory/InventoryCommandExecutor.cs
git mv Assets/Scripts/Core/Inventory/InventoryCommandResult.cs Assets/Scripts/Gameplay/Inventory/InventoryCommandResult.cs
git mv Assets/Scripts/Core/Inventory/InventoryContext.cs Assets/Scripts/Gameplay/Inventory/InventoryContext.cs
git mv Assets/Scripts/Core/Inventory/InventoryTransaction.cs Assets/Scripts/Gameplay/Inventory/InventoryTransaction.cs
git mv Assets/Scripts/Core/Inventory/InventoryValidationResult.cs Assets/Scripts/Gameplay/Inventory/InventoryValidationResult.cs

git mv Assets/Scripts/Core/Inventory/Commands/Acquisition/PickupCommand.cs Assets/Scripts/Gameplay/Inventory/Commands/Acquisition/PickupCommand.cs
git mv Assets/Scripts/Core/Inventory/Commands/Acquisition/TakeFromContainerCommand.cs Assets/Scripts/Gameplay/Inventory/Commands/Acquisition/TakeFromContainerCommand.cs
git mv Assets/Scripts/Core/Inventory/Commands/Actions/PerformInventoryActionCommand.cs Assets/Scripts/Gameplay/Inventory/Commands/Actions/PerformInventoryActionCommand.cs
git mv Assets/Scripts/Core/Inventory/Commands/Disposition/DropCommand.cs Assets/Scripts/Gameplay/Inventory/Commands/Disposition/DropCommand.cs
git mv Assets/Scripts/Core/Inventory/Commands/Disposition/DropPartialCommand.cs Assets/Scripts/Gameplay/Inventory/Commands/Disposition/DropPartialCommand.cs
git mv Assets/Scripts/Core/Inventory/Commands/Disposition/PutInContainerCommand.cs Assets/Scripts/Gameplay/Inventory/Commands/Disposition/PutInContainerCommand.cs
git mv Assets/Scripts/Core/Inventory/Commands/Equipment/AutoEquipCommand.cs Assets/Scripts/Gameplay/Inventory/Commands/Equipment/AutoEquipCommand.cs
git mv Assets/Scripts/Core/Inventory/Commands/Equipment/EquipBonusUtility.cs Assets/Scripts/Gameplay/Inventory/Commands/Equipment/EquipBonusUtility.cs
git mv Assets/Scripts/Core/Inventory/Commands/Equipment/EquipCommand.cs Assets/Scripts/Gameplay/Inventory/Commands/Equipment/EquipCommand.cs
git mv Assets/Scripts/Core/Inventory/Commands/Equipment/UnequipCommand.cs Assets/Scripts/Gameplay/Inventory/Commands/Equipment/UnequipCommand.cs

git mv Assets/Scripts/Core/Inventory/Planning/EquipPlan.cs Assets/Scripts/Gameplay/Inventory/Planning/EquipPlan.cs
git mv Assets/Scripts/Core/Inventory/Planning/EquipPlanBuilder.cs Assets/Scripts/Gameplay/Inventory/Planning/EquipPlanBuilder.cs
git mv Assets/Scripts/Core/Inventory/Planning/EquipPlanner.cs Assets/Scripts/Gameplay/Inventory/Planning/EquipPlanner.cs
git mv Assets/Scripts/Core/Inventory/Planning/InventoryDisplacement.cs Assets/Scripts/Gameplay/Inventory/Planning/InventoryDisplacement.cs

git mv Assets/Scripts/Core/Inventory/Rules/DisplacementRule.cs Assets/Scripts/Gameplay/Inventory/Rules/DisplacementRule.cs
git mv Assets/Scripts/Core/Inventory/Rules/EquipRuleResult.cs Assets/Scripts/Gameplay/Inventory/Rules/EquipRuleResult.cs
git mv Assets/Scripts/Core/Inventory/Rules/IEquipRule.cs Assets/Scripts/Gameplay/Inventory/Rules/IEquipRule.cs
git mv Assets/Scripts/Core/Inventory/Rules/SlotAvailabilityRule.cs Assets/Scripts/Gameplay/Inventory/Rules/SlotAvailabilityRule.cs
git mv Assets/Scripts/Core/Inventory/Rules/SlotCountRule.cs Assets/Scripts/Gameplay/Inventory/Rules/SlotCountRule.cs
git mv Assets/Scripts/Core/Inventory/Rules/TargetPartCompatibilityRule.cs Assets/Scripts/Gameplay/Inventory/Rules/TargetPartCompatibilityRule.cs
```

### 3.5 Mutations folder

```bash
git mv Assets/Scripts/Core/Mutations/ChimeraMutation.cs Assets/Scripts/Gameplay/Mutations/ChimeraMutation.cs
git mv Assets/Scripts/Core/Mutations/EsperMutation.cs Assets/Scripts/Gameplay/Mutations/EsperMutation.cs
git mv Assets/Scripts/Core/Mutations/ExtraArmPrototypeMutation.cs Assets/Scripts/Gameplay/Mutations/ExtraArmPrototypeMutation.cs
git mv Assets/Scripts/Core/Mutations/FlamingHandsMutation.cs Assets/Scripts/Gameplay/Mutations/FlamingHandsMutation.cs
git mv Assets/Scripts/Core/Mutations/IRankedMutation.cs Assets/Scripts/Gameplay/Mutations/IRankedMutation.cs
git mv Assets/Scripts/Core/Mutations/IrritableGenomeMutation.cs Assets/Scripts/Gameplay/Mutations/IrritableGenomeMutation.cs
git mv Assets/Scripts/Core/Mutations/MutationCategoryDefinition.cs Assets/Scripts/Gameplay/Mutations/MutationCategoryDefinition.cs
git mv Assets/Scripts/Core/Mutations/MutationDefinition.cs Assets/Scripts/Gameplay/Mutations/MutationDefinition.cs
git mv Assets/Scripts/Core/Mutations/MutationGeneratedEquipmentTracker.cs Assets/Scripts/Gameplay/Mutations/MutationGeneratedEquipmentTracker.cs
git mv Assets/Scripts/Core/Mutations/MutationModifierTracker.cs Assets/Scripts/Gameplay/Mutations/MutationModifierTracker.cs
git mv Assets/Scripts/Core/Mutations/MutationRegistry.cs Assets/Scripts/Gameplay/Mutations/MutationRegistry.cs
git mv Assets/Scripts/Core/Mutations/MutationSourceType.cs Assets/Scripts/Gameplay/Mutations/MutationSourceType.cs
git mv Assets/Scripts/Core/Mutations/RegenerationMutation.cs Assets/Scripts/Gameplay/Mutations/RegenerationMutation.cs
git mv Assets/Scripts/Core/Mutations/TelepathyMutation.cs Assets/Scripts/Gameplay/Mutations/TelepathyMutation.cs
git mv Assets/Scripts/Core/Mutations/UnstableGenomeMutation.cs Assets/Scripts/Gameplay/Mutations/UnstableGenomeMutation.cs
```

### 3.6 Data + Presentation files

```bash
git mv Assets/Scripts/Data/Blueprint.cs Assets/Scripts/Data/Blueprints/Blueprint.cs
git mv Assets/Scripts/Data/BlueprintLoader.cs Assets/Scripts/Data/Blueprints/BlueprintLoader.cs
git mv Assets/Scripts/Data/EntityFactory.cs Assets/Scripts/Data/Factories/EntityFactory.cs
git mv Assets/Scripts/Data/PopulationTable.cs Assets/Scripts/Data/Tables/PopulationTable.cs

git mv Assets/Scripts/Rendering/CP437TilesetGenerator.cs Assets/Scripts/Presentation/Rendering/CP437TilesetGenerator.cs
git mv Assets/Scripts/Rendering/CameraFollow.cs Assets/Scripts/Presentation/Cameras/CameraFollow.cs
git mv Assets/Scripts/Rendering/ContainerPickerUI.cs Assets/Scripts/Presentation/UI/ContainerPickerUI.cs
git mv Assets/Scripts/Rendering/GameBootstrap.cs Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs
git mv Assets/Scripts/Rendering/InputHandler.cs Assets/Scripts/Presentation/Input/InputHandler.cs
git mv Assets/Scripts/Rendering/InventoryUI.cs Assets/Scripts/Presentation/UI/InventoryUI.cs
git mv Assets/Scripts/Rendering/PickupUI.cs Assets/Scripts/Presentation/UI/PickupUI.cs
git mv Assets/Scripts/Rendering/QudColorParser.cs Assets/Scripts/Presentation/Rendering/QudColorParser.cs
git mv Assets/Scripts/Rendering/ZoneRenderer.cs Assets/Scripts/Presentation/Rendering/ZoneRenderer.cs
```

## Phase 4 - Move Tests

```bash
git mv Assets/Tests/EditMode/BodyPartSystemTests.cs Assets/Tests/EditMode/Gameplay/Anatomy/BodyPartSystemTests.cs
git mv Assets/Tests/EditMode/CombatSystemTests.cs Assets/Tests/EditMode/Gameplay/Combat/CombatSystemTests.cs
git mv Assets/Tests/EditMode/EntitySystemTests.cs Assets/Tests/EditMode/Gameplay/Entities/EntitySystemTests.cs
git mv Assets/Tests/EditMode/FactionAITests.cs Assets/Tests/EditMode/Gameplay/AI/FactionAITests.cs
git mv Assets/Tests/EditMode/GridSystemTests.cs Assets/Tests/EditMode/Gameplay/World/Map/GridSystemTests.cs
git mv Assets/Tests/EditMode/InventorySystemTests.cs Assets/Tests/EditMode/Gameplay/Inventory/InventorySystemTests.cs
git mv Assets/Tests/EditMode/MutationSystemTests.cs Assets/Tests/EditMode/Gameplay/Mutations/MutationSystemTests.cs
git mv Assets/Tests/EditMode/TurnMovementTests.cs Assets/Tests/EditMode/Gameplay/World/Movement/TurnMovementTests.cs
git mv Assets/Tests/EditMode/WorldMapTests.cs Assets/Tests/EditMode/Gameplay/World/Map/WorldMapTests.cs
git mv Assets/Tests/EditMode/ZoneGenerationTests.cs Assets/Tests/EditMode/Gameplay/World/Generation/ZoneGenerationTests.cs
```

## Phase 5 - Immediate Compile-Fix Pass (required)

- [ ] Update namespaces to match new folders (or keep namespace stable and only fix broken `using` statements).
- [ ] Update any hard-coded `Resources.Load(...)` blueprint paths in `BlueprintLoader`.
- [ ] Update scene references (if `SampleScene` path is baked into build settings/scripts).
- [ ] Remove now-empty old folders under `Assets/Scripts/Core`, `Assets/Scripts/Rendering`, and `Assets/Scripts/Data`.

## Phase 6 - Validation and Closure

- [ ] Open Unity and let it reimport.
- [ ] Resolve compile errors.
- [ ] Run edit mode tests.
- [ ] Verify live gameplay flows:
  - [ ] Pickup flow.
  - [ ] Drop and partial drop.
  - [ ] Container put/take.
  - [ ] Equip, auto-equip, unequip.
  - [ ] Mutation gain and mutation-driven equipment/anatomy behavior.
- [ ] Commit migration in one commit:
  - [ ] `git add -A`
  - [ ] `git commit -m "Refactor: reorganize project filesystem into gameplay/presentation/data layout"`

