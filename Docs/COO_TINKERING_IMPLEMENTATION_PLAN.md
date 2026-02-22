# Caves of Ooo Tinkering/Crafting Implementation Plan

Source basis: `/Users/steven/caves-of-ooo/Docs/qud-tinkering-analysis.md`

## Goal
Implement a Qud-like tinkering system that fits your existing entity/part architecture, inventory command pipeline, and blueprint-driven content.

This plan has two tracks:
1. `Full Similar System` (close to Qud behavior)
2. `Simplified Functional V1` (smaller scope, production-usable, foundation-safe)

---

## Current Architecture Fit (important)

Your codebase already has strong seams for this:
- Entity/Part model: `Assets/Scripts/Gameplay/Entities/Entity.cs`, `Assets/Scripts/Gameplay/Entities/Part.cs`
- Inventory + command execution: `Assets/Scripts/Gameplay/Inventory/*`
- Data loading patterns: `Assets/Scripts/Data/*` + `Assets/Resources/Content/*`
- Blueprint-driven item setup: `Assets/Resources/Content/Blueprints/Objects.json`
- Value/economy part already exists: `Assets/Scripts/Gameplay/Economy/CommercePart.cs`

This means tinkering should be implemented as:
- new `Part` types on entities/items
- data tables under `Resources/Content/Data/`
- inventory commands/services, not monolithic UI logic

## Qud Design Patterns To Mirror

- **Part-centric behavior**: put item/crafter behavior in `Part` classes (`BitLockerPart`, `TinkerItemPart`) rather than in UI.
- **Data-driven rules**: load recipe definitions from JSON registries and keep logic generic.
- **Static content registries**: cache recipe data in a central registry with explicit init/reset hooks for tests.
- **Service orchestration**: use a small domain service (`TinkeringService`) that validates + executes while UI/input just forwards intent.
- **Command pipeline integration**: expose craft/disassemble through inventory commands so all action paths share the same validation seams.

---

## A) Full Similar System (Qud-like)

## Phase 1: Core Data Model (Bits + Recipes)

### Add
- `Assets/Scripts/Gameplay/Tinkering/BitType.cs`
  - char-based bit definitions and tier mapping.
- `Assets/Scripts/Gameplay/Tinkering/BitCost.cs`
  - parse/serialize `"BBCr"` style costs.
- `Assets/Scripts/Gameplay/Tinkering/TinkerRecipe.cs`
  - build/mod recipe DTO.
- `Assets/Scripts/Gameplay/Tinkering/TinkerRecipeRegistry.cs`
  - runtime cache of all recipes + known recipes.

### Data files
- `Assets/Resources/Content/Data/Tinkering/Bits.json`
- `Assets/Resources/Content/Data/Tinkering/Recipes.json`

### Notes
- Keep costs string-based for parity and easy save/load.
- Add deterministic cost-template resolution helper (`"1BC"` -> resolved bits by tier with stable seed).

---

## Phase 2: Player Currency Storage + Spend API

### Add
- `Assets/Scripts/Gameplay/Tinkering/BitLockerPart.cs`
  - attached to Player entity.
  - APIs:
    - `AddBits(string bits)`
    - `HasBits(string bits)`
    - `UseBits(string bits)`
    - `GetSnapshot()`

### Integrate
- Ensure Player blueprint includes `BitLocker` part in `Objects.json`.
- Hook message logging for earn/spend events.

---

## Phase 3: Item Metadata for Build/Disassembly

### Add
- `Assets/Scripts/Gameplay/Tinkering/TinkerItemPart.cs`
  - fields mirroring Qud model:
    - `CanDisassemble`
    - `CanBuild`
    - `BuildTier`
    - `NumberMade`
    - `Ingredient`
    - `SubstituteBlueprint`
    - `RepairCost`
    - `BuildCost` (explicit resolved cost optional override)

### Integrate content
- Add `TinkerItem` entries to relevant blueprints in `Objects.json`.

---

## Phase 4: Skill Gating + Access

### Add
- `Assets/Scripts/Gameplay/Tinkering/TinkeringSkillPolicy.cs`
  - maps recipe tier to required skill tags:
    - tiers 0-3 -> `TinkerI`
    - 4-6 -> `TinkerII`
    - 7-8 -> `TinkerIII`

### Skill storage strategy
- Use entity tags/properties first (`HasTag("TinkerI")`) to avoid blocking on a full skill-tree refactor.
- Later: replace with formal skill system if introduced.

---

## Phase 5: Build Command Flow

### Add
- `Assets/Scripts/Gameplay/Tinkering/TinkeringService.cs`
  - orchestration layer for validation and execution.
- `Assets/Scripts/Gameplay/Inventory/Commands/Actions/CraftFromRecipeCommand.cs`
  - executes via `InventoryCommandExecutor`.

### Validation sequence
1. recipe known
2. skill requirement met
3. ingredient present
4. bits affordable
5. inventory capacity check

### Execute sequence
1. spend bits
2. consume ingredient
3. create item(s) via `EntityFactory`
4. add to inventory

---

## Phase 6: Disassembly + Reverse Engineering

### Add
- `Assets/Scripts/Gameplay/Inventory/Commands/Actions/DisassembleCommand.cs`
- `Assets/Scripts/Gameplay/Tinkering/DisassemblyResolver.cs`

### Behavior
- consume item
- derive yielded bits from item tinker cost
- last bit guaranteed, others chance-based
- if player has reverse-engineer tag, chance to learn recipe

---

## Phase 7: Modification System

### Add
- `Assets/Scripts/Gameplay/Tinkering/Mods/IModificationPart.cs`
- concrete mods in `Assets/Scripts/Gameplay/Tinkering/Mods/*`
- `Assets/Scripts/Gameplay/Tinkering/ModRegistry.cs`
- `Assets/Scripts/Gameplay/Inventory/Commands/Actions/ApplyModificationCommand.cs`

### Data
- `Assets/Resources/Content/Data/Tinkering/Mods.json`

### Compatibility
- use existing item tag tables (e.g. `Mods="MeleeWeapons,Armor"`).
- enforce slot usage and max mod slots.

---

## Phase 8: Data Disks + Learn Flow

### Add
- `Assets/Scripts/Gameplay/Tinkering/DataDiskPart.cs`
- `Assets/Scripts/Gameplay/Inventory/Commands/Actions/LearnRecipeFromDiskCommand.cs`

### Behavior
- disk teaches recipe and is consumed.
- optional direct-build action from disk.

---

## Phase 9: UI

### Add
- `Assets/Scripts/Presentation/UI/TinkeringUI.cs`

### MVP UI requirements
- recipe list
- affordability + skill lock display
- ingredient requirement display
- build/disassemble actions

---

## B) Simplified Functional V1 (recommended first)

This version is intentionally smaller but fully playable.

## Scope
- Build + Disassemble only
- No item mods yet
- No data disks yet
- No skill gate in V1
- 4 base bits only: `R,G,B,C`

## What players can do
- salvage qualifying items into bits
- craft a small recipe set from bits
- fail crafting when lacking bits/ingredients
- unlock new recipes via simple progression hooks (quests/events/manual grant)

---

## V1 Implementation Plan (do this first)

## Step 1: Add minimum data and parts
- Add `BitLockerPart`, `BitCost`, `TinkerRecipe`, `TinkerRecipeRegistry`, `TinkerItemPart`.
- Add data file:
  - `Assets/Resources/Content/Data/Tinkering/Recipes_V1.json`

Starter recipe examples:
- `ThornDagger` cost `BC`
- `LeatherArmor` cost `BBC`
- `Torch` cost `RC`

---

## Step 2: Add service and commands
- `TinkeringService` with methods:
  - `TryCraft(Entity crafter, EntityFactory factory, string recipeId, out List<Entity> crafted, out string reason)`
  - `TryDisassemble(Entity crafter, Entity item, out string yieldedBits, out string reason)`
- Add commands:
  - `CraftFromRecipeCommand`
  - `DisassembleCommand`

Route through existing inventory command executor.

---

## Step 3: Wire minimal UI entry
- Add a small `Tinkering` panel or debug action:
  - list known recipes
  - craft selected
  - disassemble selected inventory item

Keep UI simple; logic stays in service/commands.

---

## Step 4: Seed content
- Update `Objects.json`:
  - Player gets `BitLocker` part.
  - select items get `TinkerItem` part and costs.
- Seed known recipes on player start (hardcoded or from a starter profile in data).

---

## Step 5: Tests (must-have)

Create:
- `Assets/Tests/EditMode/Gameplay/Tinkering/TinkeringServiceTests.cs`
- `Assets/Tests/EditMode/Gameplay/Tinkering/BitLockerTests.cs`

Test cases:
1. `AddBits/UseBits/HasBits` correctness
2. craft succeeds with exact bits
3. craft fails without bits
4. disassemble yields bits and destroys item
5. ingredient-gated craft succeeds/fails correctly

---

## Definition of Done for V1

V1 is complete when:
1. Player has a persistent bit inventory during play session.
2. At least 5 recipes can be crafted from bits.
3. At least 8 existing items are disassemblable.
4. All V1 tests pass.
5. No crafting logic lives directly in UI scripts.

---

## Suggested V1 -> Full migration order

After V1 ships:
1. add tiered bits and Tinker I/II/III gating
2. add reverse engineering chance
3. add data disks
4. add modification system
5. expand recipe catalog + balancing

---

## Guardrails

- Do not couple tinkering logic to rendering/UI classes.
- Keep recipe and bit definitions data-driven from day one.
- Keep command execution idempotent and validation-first.
- Log clear failure reasons for every craft/disassemble failure.
