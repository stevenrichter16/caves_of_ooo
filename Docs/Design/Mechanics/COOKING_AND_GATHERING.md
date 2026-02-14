# Caves of Qud: Cooking & Gathering System -- Complete Technical Deep Dive

> Compiled from decompiled C# source analysis of the Caves of Qud codebase.
> All class names, method signatures, formulas, and constants are extracted directly from source code.

---

## Table of Contents

1. [System Architecture Overview](#1-system-architecture-overview)
2. [Harvestable Plants](#2-harvestable-plants)
3. [Butchering System](#3-butchering-system)
4. [Food & Eating Mechanics](#4-food--eating-mechanics)
5. [Hunger & Thirst (Stomach System)](#5-hunger--thirst-stomach-system)
6. [Ingredients & Prepared Cooking Ingredients](#6-ingredients--prepared-cooking-ingredients)
7. [Preserving System](#7-preserving-system)
8. [Campfires & Ovens](#8-campfires--ovens)
9. [The Cooking Pipeline](#9-the-cooking-pipeline)
10. [Recipe System](#10-recipe-system)
11. [Cookbooks & Chef NPCs](#11-cookbooks--chef-npcs)
12. [Ingredient Domains (Categories)](#12-ingredient-domains-categories)
13. [Procedural Cooking Effects Architecture](#13-procedural-cooking-effects-architecture)
14. [Complete Domain Effect Catalogue](#14-complete-domain-effect-catalogue)
15. [Basic "Tasty Meal" Effects](#15-basic-tasty-meal-effects)
16. [Named (Preset) Recipes](#16-named-preset-recipes)
17. [Nostrums (Medical Cooking)](#17-nostrums-medical-cooking)
18. [Cooking Skill Tree](#18-cooking-skill-tree)
19. [Automated Food Processing](#19-automated-food-processing)
20. [Summary Tables](#20-summary-tables)

---

## 1. System Architecture Overview

The cooking system is a multi-layered, blueprint-driven architecture composed of:

```
Gathering Layer:
  Harvestable (IPart) ──> raw plant items
  Butcherable (IPart) ──> raw meat/bone items

Conversion Layer:
  PreservableItem (IPart) ──> preserved cooking ingredients
  PreparedCookingIngredient (IPart) ──> domain-typed cooking ingredients

Cooking Layer:
  Campfire (IActivePart) ──> orchestrates all cooking operations
  CookingRecipe ──> stored recipe definitions
  CookingGameState ──> global recipe storage singleton

Effect Layer:
  ProceduralCookingEffect (Effect) ──> container for cooking buffs
    ProceduralCookingEffectUnit ──> passive stat bonuses
    ProceduralCookingEffectWithTrigger ──> conditional triggered actions
      ProceduralCookingTriggeredAction ──> actions fired by triggers
  BasicCookingEffect (Effect) ──> "tasty meal" bonuses
```

### Class Hierarchy

```
Effect (base)
  +-- ProceduralCookingEffect                    [Duration=1, event-driven expiry]
  |     +-- ProceduralCookingEffectWithTrigger   [adds trigger + triggered actions]
  +-- BasicCookingEffect                         [Duration=1, event-driven expiry]
  |     +-- BasicCookingEffect_Hitpoints
  |     +-- BasicCookingEffect_Quickness
  |     +-- BasicCookingEffect_RandomStat
  |     +-- BasicCookingEffect_Regeneration
  |     +-- BasicCookingEffect_ToHit
  |     +-- BasicCookingEffect_MA
  |     +-- BasicCookingEffect_MS
  |     +-- BasicCookingEffect_XP
  +-- BasicTriggeredCookingEffect                [duration-based, decrements each EndTurn]
        +-- BasicTriggeredCookingStatEffect       [stat shift sub-effect]

ProceduralCookingEffectUnit (abstract base for passive buffs)
  +-- ProceduralCookingEffectUnitMutation<T>     [generic mutation granter]
  +-- ProceduralCookingEffectUnitSkill<T>        [generic skill granter]
  +-- ~40+ concrete unit subclasses

ProceduralCookingTriggeredAction (abstract base for triggered actions)
  +-- ~25+ concrete triggered action subclasses
```

### Key Design Finding: No Spoilage System

**Caves of Qud has NO food spoilage mechanic.** There is no expiry timer, freshness counter, or decay system. The distinction between "fresh" and "preserved" is purely about form:
- **Fresh food** (e.g., raw starapple): Has `PreservableItem` part, may lack `PreparedCookingIngredient`
- **Preserved food** (e.g., Starapple Preserves): Has `PreparedCookingIngredient` with cooking domains and charges

Preservation is a **conversion** system, not a decay prevention system.

---

## 2. Harvestable Plants

### 2.1 Core Class: `Harvestable` (IPart)

**File:** `XRL.World.Parts/Harvestable.cs`

This is the primary component that makes any game object harvestable.

#### Fields

```csharp
public bool DestroyOnHarvest;              // If true, parent object destroyed after harvest
public string OnSuccess = "Vinewafer";     // Blueprint name of item produced (or "@PopTable")
public string OnSuccessAmount = "1";       // Dice string for quantity (e.g. "1", "1d3")
public string RipeTiles = "";              // Comma-separated tile paths when ripe
public string RipeRenderString = "";       // ASCII character when ripe
public string RipeColor = "";              // Color string when ripe
public string RipeTileColor = "";
public string RipeDetailColor = "";
public string UnripeTiles = "";            // Tiles when unripe
public string UnripeRenderString = "";
public string UnripeColor = "";
public string UnripeTileColor = "";
public string UnripeDetailColor = "";
public int TileIndex = -1;                 // Which tile variant to use (randomized once)
public bool Ripe;                          // Current ripeness state
public string StartRipeChance = "1:1";     // Chance to start ripe (e.g. "1:1" = 100%)
public int RegenTimer = int.MaxValue;      // Turns until re-ripening
public string RegenTime = "";              // Dice string for regen time (e.g. "200-400")
public string RipeTimerChance = "1:1";     // Chance to ripen when timer expires
public string HarvestVerb;                 // Custom verb (e.g. "shuck", "pluck")
```

#### Key Methods

**`UpdateRipeStatus(bool newRipeStatus)`** -- Sets the plant ripe or unripe. When transitioning to unripe (and `RegenTime` is set), rolls the `RegenTime` dice to set `RegenTimer`. Updates the visual appearance (tile, color, render string) from the appropriate tile set. `TileIndex` is randomly assigned once and persists.

**`Ripen()`** -- Called every turn via `EndTurnEvent`. Decrements `RegenTimer`. When it reaches 0, rolls `RipeTimerChance` -- on success, plant becomes ripe; on failure, re-rolls `RegenTimer`.

**`IsHarvestable()`** -- Returns `true` if `Ripe && Render.Visible` and not a hologram.

**`AttemptHarvest(GameObject who, ...)`** -- The core harvest logic:
1. Checks `IsHarvestable()` and that `who` has `CookingAndGathering_Harvestry` skill
2. If automatic: skips if hostiles nearby, if toggled off, if object is important
3. Checks frozen status, confirms if important object
4. Rolls `OnSuccessAmount` -- if result is 0 or plant is temporary, nothing harvested
5. If `OnSuccess` starts with `@`, uses `PopulationManager.RollOneFrom()` for random blueprint
6. Otherwise creates `OnSuccess` object directly
7. Gives items to actor via `TakeObject`
8. Sets plant to unripe via `UpdateRipeStatus(false)`
9. If `DestroyOnHarvest`, destroys the parent object
10. Costs **1000 energy** ("Skill")

#### Events Handled

| Event | Action |
|-------|--------|
| `AfterObjectCreatedEvent` | Rolls `StartRipeChance` for initial ripe status |
| `EndTurnEvent` | Calls `Ripen()` |
| `CanSmartUseEvent` / `CommandSmartUseEvent` | Smart-use harvesting |
| `GetInventoryActionsEvent` | Adds "Harvest" action (key 'h', priority 15) |
| `InventoryActionEvent` | Processes "Harvest" command |
| `ObjectEnteringCellEvent` | Auto-harvest when walking into a plant |
| `AccelerateRipening` | Legacy event, calls `Ripen()` |

### 2.2 Plant Properties

**`PlantProperties` (IPart)** -- `XRL.World.Parts/PlantProperties.cs`

Marks an entity as a plant with properties:

```csharp
public bool Rooted = true;
```

- **Rooted plants:** Immune to Prone, Wading, Swimming, CardiacArrest effects
- **Kinetic resistance:** +300 linear, +200% when rooted
- **Movement:** `IsRootedInPlaceEvent` returns false when rooted (blocks movement)
- **Unrooting:** Triggered by animation, flying effects, leaving cell, equipping "Roots" armor

**`RipePlant` (IPart)** -- `XRL.World.Parts/RipePlant.cs` -- Simple tracker. On `BeforeDeathRemovalEvent`, increments the game state counter `"ripe plants killed"`.

### 2.3 Special Plant Behaviors

**`Yonderbrush` (IPart)** -- `XRL.World.Parts/Yonderbrush.cs` -- A hostile plant that teleports creatures entering its cell. If the creature has `CookingAndGathering_Harvestry` toggled on, it is harvested instead. Navigation weight: 95.

**`SurroundOnStep` (IPart)** -- `XRL.World.Parts/SurroundOnStep.cs` -- Creates surrounding objects when stepped on. Can be harvested if `AllowHarvest` is true and the creature has the harvest skill toggled on, preventing the surround effect.

### 2.4 Zone Builders for Farms

**`StarappleFarm`** (`XRL.World.ZoneBuilders/StarappleFarm.cs`):
- Generates 1-3 fenced box areas
- Plants "Starapple Farm Tree" at 45% chance per valid cell
- Uses brinestalk fences with gates

**`CarbonFarm`** (`XRL.World.ZoneBuilders/CarbonFarm.cs`):
- Generates maze-like farm layout using `RecursiveBacktrackerMaze`
- 10x3 grid of 8x8 cells with limestone walls

**`FractiPlanter`** (`XRL.World.ZoneBuilders/FractiPlanter.cs`):
- Plants 3-4 Fracti organisms, each grown to size 10-20 on SaltPath floors

---

## 3. Butchering System

### `Butcherable` (IPart)

**File:** `XRL.World.Parts/Butcherable.cs`

Allows corpses/items to be butchered into ingredients.

```csharp
public string OnSuccessAmount = "1";  // Dice string
public string OnSuccess = "";          // Blueprint or @PopTable
```

**Process:**
- Requires `CookingAndGathering_Butchery` skill
- Energy cost: `1000 / max(ButcheryToolEquipped + 1, 1)` -- butchery tools reduce energy cost
- If `OnSuccess[0] == '@'`, uses population table
- Auto-butchers during autoexplore when toggled on
- Destroys source corpse, creates product items

---

## 4. Food & Eating Mechanics

### `Food` (IPart)

**File:** `XRL.World.Parts/Food.cs`

The basic food component for edible items.

```csharp
public static readonly int SMALL_HUNGER_AMOUNT = 200;  // Snack reduces CookingCounter by 200
public int Thirst;                                       // Water added when eaten
public string Satiation = "None";                        // "None", "Snack", "Meal"
public bool Gross;                                       // Requires Famished to eat
public bool IllOnEat;                                    // 100% illness chance
public string Healing = "0";                             // Dice string for HP healed
public string Message = "That hits the spot!";           // Displayed on eat
```

**Eating Process:**
1. Check `CanMoveExtremities` and `Stomach` part exists
2. If `Gross` and not famished (and not carnivore eating meat), refuse
3. Fire `BeforeConsumeEvent` check
4. Fire `OnEat` event on the food item (triggers special effects)
5. Fire `Eating` event on the actor
6. Add water: `FireEvent("AddWater", Thirst)`
7. Heal: `Actor.Heal(Healing.RollCached())`
8. Apply satiation:
   - `"Snack"`: `CookingCounter -= 200`
   - `"Meal"`: `CookingCounter = 0`
9. Carnivorous check: non-meat food has 50% illness chance
10. Fire `AfterConsumeEvent`, `Eaten`, `AfterEat` events
11. Destroy the food item
12. Costs **1000 energy**

**Illness Formula:**
```csharp
public virtual int IllnessChance(GameObject Actor) {
    if (Actor.HasPart<Carnivorous>()) {
        if (ParentObject.HasTag("Meat")) return 0;
        if (!IllOnEat) return 50;
        return 100;
    }
    if (IllOnEat) return 100;
    return 0;
}
```

### OnEat Effect Parts

Many items have special effects triggered when eaten via separate IPart components:

| Part Class | Effect |
|---|---|
| `EffectOnEat` | Applies arbitrary Effect class by name, with optional Duration |
| `HealOnEat` | Heals to full HP + fires "Recuperating" event |
| `FearOnEat` | Mental attack: Terrified (Strength dice, Duration dice) |
| `Yuckwheat` | Removes Confused and Poisoned effects |
| `PoisonOnEat` | Poisons the eater |
| `ConfuseOnEat` | Confuses the eater |
| `TeleportOnEat` | Teleports the eater |
| `TemperatureOnEat` | Changes body temperature |
| `StatOnEat` | Modifies stats |
| `BreatheOnEat` | Grants breath weapon |
| `JumpOnEat` | Grants jumping |
| `MutationPointsOnEat` | Grants mutation points |
| `MutationInfectionOnEat` | Grants/infects mutations |
| `SecretsOnEat` | Reveals secrets |
| `RefreshCooldownsOnEat` | Refreshes cooldowns |
| `GeometricHealOnEat` | Percentage-based healing |

All trigger on the `"OnEat"` event fired by `Food.HandleEvent(InventoryActionEvent)`.

---

## 5. Hunger & Thirst (Stomach System)

### `Stomach` (IPart)

**File:** `XRL.World.Parts/Stomach.cs`

```csharp
public const int COOKING_INCREMENT = 1200;  // Base turns between hunger levels
public int CookCount;                         // Meals eaten this hunger period
public int Water = 30000;                     // Current water level
public int HungerLevel;                       // 0=Sated, 1=Hungry, 2=Famished
public int _CookingCounter;                   // Current hunger counter (turns)
```

### Hunger Thresholds

```csharp
int increment = CalculateCookingIncrement();
// CookingCounter < increment         -> HungerLevel 0 (Sated, green)
// CookingCounter >= increment         -> HungerLevel 1 (Hungry, white)
// CookingCounter >= increment * 2     -> HungerLevel 2 (Famished, red)
```

### `CalculateCookingIncrement()`

| Condition | Increment |
|---|---|
| Base | 1200 ticks |
| `Discipline_FastingWay` | x2 (2400) |
| `Discipline_MindOverBody` | x6 (7200) |
| Both skills | x12 (14400) |

### Thirst Formula (per turn)

```csharp
int value = ParentObject.Speed / (Amphibious ? 3 : 5) / num;
// num = 1 (base), x2 with FastingWay, x6 with MindOverBody
// FattyHump: only -1 water per turn instead of speed-based
```

### CookCount

Tracks meals eaten while sated. Allows up to **3 meals while sated** (`CookCount < 3`). Resets to 0 on hunger level change.

---

## 6. Ingredients & Prepared Cooking Ingredients

### `PreparedCookingIngredient` (IPart)

**File:** `XRL.World.Parts/PreparedCookingIngredient.cs`

This component marks an item as a valid **cooking ingredient** (as opposed to just "food").

```csharp
public string type;                // Cooking domain type(s), comma-separated
public string descriptionPostfix;  // Auto-generated description of cooking effects
public int charges;                // Number of cooking servings
```

### Type Resolution

**`GetTypeInstance()`** -- Returns a single resolved type:

| Input | Behavior |
|---|---|
| `"random"` | Picks from weighted `IngredientMapping` blueprints using `RandomWeight` tag |
| `"randomHighTier"` | Picks from `RandomHighTierWeight` weighted blueprints |
| `"type1,type2"` | Picks randomly from comma-separated options |
| `"specificType"` | Returns as-is |

**`GetRandomTypeList()`** -- Builds weighted list from all blueprints inheriting `"IngredientMapping"`, using their `RandomWeight` tag. Type name extracted by splitting blueprint name on `_` (e.g., `"ProceduralCookingIngredient_heatMinor"` -> `"heatMinor"`).

### On ObjectCreated

For each type in the comma list, looks up `ProceduralCookingIngredient_{type}` blueprint, gets the `Description` tag, sets `descriptionPostfix` = "Adds {descriptions} effects to cooked meals."

### Display

Shows `[X cooking servings]` in item display name for items tagged `SpecialCookingIngredient`.

### Valid Cooking Ingredient Check

```csharp
public static bool IsValidCookingIngredient(GameObject obj) {
    // Not temporary (unless CanCookTemporary tag)
    // Not important
    // Has PreparedCookingIngredient part, OR
    // Has LiquidVolume that is not sealed, has cooking ingredient, and is identified
}
```

### Liquid Cooking Ingredients

Each liquid type in `XRL.Liquids` overrides `GetPreparedCookingIngredient()`:

| Liquid | Cooking Domain |
|---|---|
| `LiquidAcid` | `"acidMinor"` |
| `LiquidAsphalt` (Tar) | `"stabilityMinor"` |
| `LiquidSlime` | `"slimeSpitting"` |
| `LiquidGoo` | `"selfPoison"` |
| `LiquidSludge` | `"selfPoison"` |
| `LiquidOoze` | `"selfGlotrot"` |
| `LiquidHoney` | `"medicinalMinor"` |
| `LiquidLava` | `"heatMinor"` |
| `LiquidAlgae` | `"plantMinor"` |
| `LiquidSalt` | `"tastyMinor"` |
| `LiquidCider` | `"quicknessMinor"` |
| `LiquidCloning` | `"cloningMinor"` |
| `LiquidConvalessence` | `"coldMinor,regenLowtierMinor"` |
| `LiquidNeutronFlux` | `"density"` |

**Non-cookable liquids** (return `""` from base class): Blood, BrainBrine, Gel, Ink, Oil, ProteanGunk, Putrescence, Sap, SunSlag, WarmStatic, Water, Wax, Wine.

---

## 7. Preserving System

### `PreservableItem` (IPart)

**File:** `XRL.World.Parts/PreservableItem.cs`

Marks an item as preservable. Pure data holder:

```csharp
public string Result;   // Blueprint name of the preserved result
public int Number;       // Number of preserved items produced per source item
```

### Standard Preservation Flow (`Campfire.Preserve()`)

1. Requires `CookingAndGathering` skill
2. Gets all inventory items with `PreservableItem` part, NOT tagged `ChooseToPreserve`, not temporary, not important
3. For each item, calls `PerformPreserve()`
4. Plays sound `"Sounds/Interact/sfx_interact_preserve_foods"`

### Exotic Preservation (`Campfire.PreserveExotic()`)

1. Requires `CookingAndGathering` skill
2. Gets items with `PreservableItem` + `ChooseToPreserve` tag + not temporary + `Understood()`
3. Player picks from a list, chooses quantity
4. Calls `PerformPreserve()` on the selected split stack
5. Plays sound `"Sounds/Interact/sfx_interact_preserve_exotic"`

### `PerformPreserve()` -- Core Logic

```csharp
public static bool PerformPreserve(GameObject go, StringBuilder sb,
                                    GameObject who, bool Capitalize, bool Single) {
    int count = go.Count;
    if (Single && count > 1) { go = go.RemoveOne(); count = 1; }

    PreservableItem part = go.GetPart<PreservableItem>();
    PreparedCookingIngredient part2 = go.GetPart<PreparedCookingIngredient>();

    int outputNum = 1;
    if (part2 != null) outputNum = part2.charges;
    if (part != null) outputNum = part.Number;  // PreservableItem.Number takes precedence
    outputNum *= go.Count;  // Multiply by stack size

    go.Obliterate();  // Destroy source
    who.TakeObject(part.Result, outputNum, ...);  // Give preserved result
}
```

**Formula:** Each source item produces `PreservableItem.Number * stack_count` units of `PreservableItem.Result`.

The preserved result object uses tags:
- `ServingType` (default: "serving") -- e.g., "serving", "jar"
- `ServingName` (default: display name) -- e.g., "pickled mushrooms"

---

## 8. Campfires & Ovens

### `Campfire` (IActivePart)

**File:** `XRL.World.Parts/Campfire.cs` (~1948 lines)

The central cooking interface. Key constants:

```csharp
public const string COOKING_PREFIX = "ProceduralCookingIngredient_";
public string ExtinguishBlueprint;       // Blueprint to replace with when extinguished
public string PresetMeals = "";          // Comma-separated preset meal class names
public List<CookingRecipe> specificProcgenMeals;  // Procgen meals for Chef ovens
```

### Command Constants

```csharp
public static readonly string COOK_COMMAND_RECIPE = "CookFromRecipe";
public static readonly string COOK_COMMAND_WHIP_UP = "CookWhipUp";
public static readonly string COOK_COMMAND_CHOOSE = "CookChooseIngredients";
public static readonly string COOK_COMMAND_PRESERVE = "CookPreserve";
public static readonly string COOK_COMMAND_PRESERVE_EXOTIC = "CookPreserveExotic";
public static readonly string COOK_COMMAND_PRESET_MEAL = "CookPresetMeal:";
public static readonly string COOK_COMMAND_STOP_BLEEDING = "CookStopBleeding";
public static readonly string COOK_COMMAND_TREAT_POISON = "CookTreatPoison";
public static readonly string COOK_COMMAND_TREAT_ILLNESS = "CookTreatIllness";
public static readonly string COOK_COMMAND_TREAT_DISEASE_ONSET = "CookTreatDiseaseOnset";
```

### Heat Mechanics (EndTurnEvent)

- Each turn if ready: if temperature below 600 and 10% chance, heats self by 150 degrees
- Heats all objects in same cell. Combat objects get 10% chance; non-combat always heated. Max temp = 600
- If liquid pool of volume >= 10 drams is in same cell, campfire extinguishes
- If campfire is frozen, it extinguishes

### `Cook()` Entry Point

1. Checks if player is frozen, if hostiles are nearby (blocks cooking), if campfire is operational
2. Checks `CanChangeMovementMode("Cooking")`
3. Opens campfire ambient sounds (`CampfireSounds.Open()`)
4. Enters loop: gathers cooking actions via `GetCookingActionsEvent`, shows menu, processes selection
5. Menu header: `"The fire breathes its warmth on your bones."`

### `IsHungry()` Check

```csharp
public static bool IsHungry(GameObject Object) {
    if (Object.TryGetPart<Stomach>(out var Part)) {
        if (Part.HungerLevel <= 0) return Part.CookCount < 3;
        return true;
    }
    return false;
}
```

Up to **3 meals while sated**. Once hungry or famished, always eligible.

### Cooking Menu (Priority Order)

| Action | Key | Priority | Conditions |
|--------|-----|----------|------------|
| Preset meals (from oven) | a-e | 100+ | `IsHungry` |
| "Whip up a meal" | m | 90 | `IsHungry` |
| "Choose ingredients to cook with" | i | 80 | `CookingAndGathering` skill + `IsHungry` |
| "Cook from a recipe" | r | 70 | Skill + known recipes + `IsHungry` |
| "Preserve your fresh foods" | f | 60 | Skill + has preservable items |
| "Preserve your exotic foods" | x | 50 | Skill + has exotic preservables |
| "Stop bleeding" | b | 40 | `Physic_Nostrums` skill |
| "Treat poison" | p | 30 | `Physic_Nostrums` skill |
| "Treat illness" | l | 20 | `Physic_Nostrums` skill |
| "Treat disease onset" | d | 10 | `Physic_Nostrums` skill |

### `CampfireRemains` (IPart)

**File:** `XRL.World.Parts/CampfireRemains.cs`

Represents an extinguished campfire. Has `Blueprint` field (default "Campfire"):
- Smart-use or "Light" inventory action triggers `AttemptLight()`
- Cannot light if in a liquid pool (volume >= 10, open)
- On receiving `Burn` event, automatically lights
- Lighting replaces the object with the `Blueprint` object

### `CampfireSounds` (MonoBehaviour)

**File:** `CampfireSounds.cs`

- On `Open()`: mutes game music, starts fire ambient sounds, sets flourish/harmonica timers
- Flourish sounds: `Cooking_Chopping_*`, `Cooking_Pan_*` -- plays randomly every ~1s with 20% chance
- Harmonica: 10% chance to start every 0.25s
- On `Close()`: plays extinguish sound, restores music after 2s fadeout

---

## 9. The Cooking Pipeline

### 9.1 "Whip Up a Meal" (`CookFromIngredients(random: true)`)

The simplest cooking path, **no skill required**:

1. Check `IsHungry()` -- if not hungry: "You aren't hungry. Instead, you relax by the warmth of the fire."
2. Get valid cooking ingredients from inventory + equipment + adjacent cells
3. Shuffle ingredients, pick up to 3 with distinct types (duplicates skipped)
4. No type selection UI -- types randomly resolved
5. Generate `ProceduralCookingEffect` from selected types
6. Show procedurally-generated meal description (from `DescribeMeal()`)
7. Roll for "tasty" bonus (10% base chance)
8. Apply effect: `effect.Init(player)`, duration=1
9. Message: "You start to metabolize the meal, gaining the following effect for the rest of the day"
10. Consume 1 charge from each used ingredient (or 1 dram of liquid)
11. Increment `CookCount`, clear hunger

### 9.2 "Choose Ingredients" (`CookFromIngredients(random: false)`)

Requires `CookingAndGathering` skill:

1. Same hunger check
2. Ingredient limit: **base 2, +1 with `CookingAndGathering_Spicer`** (total max 3)
3. Shows multi-select UI: `"Choose ingredients to cook with."`
4. For each selected ingredient, resolves type (random types get resolved, checking for duplicates)
5. **If player has `Inspired` effect:**
   - Generates 3 different effects via `GenerateEffectsFromTypeList(types, 3)`
   - Player picks one from 3 options
   - Creates a new recipe: `CookingRecipe.FromIngredients()`
   - Learns it: `CookingGameState.LearnRecipe()`
   - Shows: "You create a new recipe for {name}!"
   - Logs journal accomplishment, increments `Achievement.RECIPES_100`
   - Removes `Inspired` effect
6. **Without Inspired:** generates single effect, no recipe creation
7. Apply effect, consume ingredients

### 9.3 "Cook From Recipe" (`CookFromRecipe()`)

Requires `CookingAndGathering` skill:

1. Resets inventory snapshot
2. Lists all known non-hidden recipes (filtered for Carnivorous)
3. Missing-ingredient recipes hidden; count shown: `"< N hidden for missing ingredients >"`
4. Shows recipe list sorted: favorites first, then available ingredients, then alphabetical
5. On selecting a recipe, sub-menu: **Cook, Add/Remove Favorite, Forget, Back**
6. "Cook": `CheckIngredients()` -> `UseIngredients()` -> clear effects -> roll tasty -> apply effects
7. "Forget": sets `recipe.Hidden = true`

### 9.4 Preset Meals (`CookPresetMeal(index)`)

Used by Chef ovens with pre-defined meals:

1. Hunger check
2. Plays eating sound, shows random eating message
3. Fires `ClearFoodEffects`
4. **Carnivorous check**: if recipe has plants/fungi, 50% chance of `Ill(100)`
5. Otherwise: `recipe.ApplyEffectsTo(player)`, clear hunger
6. Increment CookCount

### 9.5 Effect Generation Algorithm

**`Campfire.GenerateEffectFromTypeList(List<string> types)`**

1. Generate all permutations of the ingredient type list
2. Add `null` as a fallback option
3. Shuffle all permutations randomly
4. For each permutation:
   - **null or 1 element**: `CreateJustUnits(allTypes)` -- passive-only effect
   - **2 elements** and first has Triggers, second has Actions: `CreateTriggeredAction(first, second)`
   - **3 elements** and first has Units, second has Triggers, third has Actions: `CreateBaseAndTriggeredAction(first, second, third)`
5. Returns the first successfully created effect

The `null` fallback ensures an effect is always generated even if no trigger/action permutation works.

### 9.6 "Tasty" Bonus Roll

```csharp
public static bool RollTasty(int Bonus = 0, bool bCarnivore = false, bool bAlwaysSucceed = false)
```

- Base chance: `(10 + Bonus)` percent
- Forced success if any ingredient has `tastyMinor` type (salt)
- On success: applies one of 7 random `BasicCookingEffect` types (see Section 15)

### 9.7 Meal Description Generation

**`RollIngredients(amount, objects)`** generates 3-4 ingredient description strings:
- For actual ingredients: uses their display name or liquid name
- For filler: expands `<spice.cooking.ingredient.!random>` with context variables:
  - `$terrain` -- current zone terrain
  - `$creaturePossessive` -- possessive form of a random dismemberable creature
  - `$creatureBodyPart` -- a dismemberable body part
  - `$bookName`, `$gasName`, `$tonicName` -- random objects

**`DescribeMeal()`** makes an and-list and plugs into `<spice.cooking.cookTemplate.!random>`, replacing `*ingredients*`.

**`ProcessEffectDescription()`** substitutes pronoun templates:
- `@thisCreature` -> "you" / "this creature"
- `@s` -> "" / "s", `@es` -> "" / "es"
- `@is` -> "are" / creature's Is
- `@they`/`@their`/`@them` -> appropriate pronouns

---

## 10. Recipe System

### `CookingRecipe` (IComposite)

**File:** `XRL.World.Skills.Cooking/CookingRecipe.cs`

```csharp
public bool Hidden;
public bool Favorite;
public string DisplayName;
public string ChefName;
public List<ICookingRecipeComponent> Components;  // Ingredient requirements
public List<ICookingRecipeResult> Effects;          // Applied effects
public Renderable Tile;                             // Visual icon
```

### Recipe Name Generation

Uses `HistoricStringExpander` with spice templates:
- 1 ingredient: `<spice.cooking.recipeNames.oneIngredientMeal.!random>`
- 2 ingredients: `<spice.cooking.recipeNames.twoIngredientMeal.!random>`
- 3 ingredients: `<spice.cooking.recipeNames.threeIngredientMeal.!random>`

### Recipe Tile Generation

`GenerateRecipeTile()` matches recipe name against 38 food tile types:
```
cake, bread, loaf, slaw, stew, soup, brisket, borscht, dip, baklava,
compote, hash, porridge, matz, cookies, yogurt, goulash, rice, hummus, knish,
broth, kugel, latkes, schnitzel, pancake, roast, shawarma, flatbread,
meatballs, pastry, casserole, dumpling, doughnut, tajine, couscous, dolma, kebab, fillet
```
Plus 12 extra numbered tiles (`sw_food_0.png` through `sw_food_11.png`). Colors random from bright/dark Crayons palettes.

### `RollOvenSafeIngredient(string table)`

Rolls from population table, excluding dangerous items:
- `"Psychal Gland Paste"`, `"FluxPhial"`, `"CloningDraughtWaterskin_Ingredient"`, `"Drop of Nectar"`, `"Wild Rice"`
- Fallback after 40 rerolls: `"Starapple Preserves"`

### Recipe Component Types (`ICookingRecipeComponent`)

```csharp
interface ICookingRecipeComponent : IComposite {
    bool doesPlayerHaveEnough();
    string createPlayerDoesNotHaveEnoughMessage();
    void use(List<GameObject> used);
    string getDisplayName();
    bool HasPlants();
    bool HasFungi();
    int PlayerHolding();
    string getIngredientId();
}
```

| Class | ID Format | Match Type |
|---|---|---|
| `PreparedCookingRecipieComponentBlueprint` | `blueprint-{name}` | Specific blueprints (pipe-separated alternatives) |
| `PreparedCookingRecipieComponentLiquid` | `liquid-{id}` | Specific pure liquids by dram |
| `PreparedCookingRecipieComponentDomain` | `prepared-{type}` | `PreparedCookingIngredient` by type string |
| `PreparedCookingRecipeUnusualComponentBlueprint` | `blueprint-{name}` | Whole objects (not charges-based) |

**Blueprint consumption:** Deducts `charges` from `PreparedCookingIngredient`. If charges reach 0, destroys item.

**Liquid consumption:** Uses `LiquidVolume.UseDrams(amount)` or empties volume.

### Recipe Results (`ICookingRecipeResult`)

```csharp
interface ICookingRecipeResult : IComposite {
    string GetCampfireDescription();
    string apply(GameObject eater);
}
```

- **`CookingRecipeResultProceduralEffect`** -- Deep-copies a `ProceduralCookingEffect`, calls `Init()`, then `ApplyEffect()`. Standard result type.
- **`CookingRecipeResultEffect`** -- Instantiates a named Effect class directly.

### `CookingGameState` (IGameStateSingleton)

**File:** `XRL.World.Skills.Cooking/CookingGameState.cs`

```csharp
public List<CookingRecipe> knownRecipies;
public static List<GameObject> inventorySnapshot;  // Cached inventory
public static Dictionary<string, int> ingredientQuantity;  // Cached quantities
```

- `LearnRecipe()` -- Adds to journal via `JournalAPI.AddRecipeNote()`
- `KnowsRecipe()` -- Checks by display name or class name
- `GetInventorySnapshot()` -- Cached filtered inventory (excludes plant/fungus for Carnivorous)

### Recipe Learning Paths

1. **Cookbook reading** -- Reading each page teaches that page's recipe
2. **Water Ritual** (`WaterRitualCookingRecipe`) -- Costs reputation (default 50); can come from NPC's `SharesRecipe`, `TeachesDish`, faction's `WaterRitualRecipe`, chef signature dishes
3. **Carbide Chef Inspiration** -- During `CookFromIngredients(random: false)` with `Inspired` effect, player creates and learns a new recipe
4. **`CookedAchievement`** part -- Triggers achievements when specific ingredients are used

### Journal Integration

**`JournalRecipeNote`** (`Qud.API/JournalRecipeNote.cs`):

```csharp
public override void Reveal(string LearnedFrom = null, bool Silent = false) {
    base.Reveal(LearnedFrom, Silent);
    CookingGameState.instance.knownRecipies.Add(Recipe);
    DisplayMessage("You learn to cook " + Recipe.GetDisplayName() + "!",
                   "sfx_cookingRecipe_learn");
}
```

---

## 11. Cookbooks & Chef NPCs

### `Cookbook` (IPart)

**File:** `XRL.World.Parts/Cookbook.cs`

```csharp
public int Tier = 1;
public string NumberOfIngredients = "2-4";
public string Style = "Generic";        // "Generic", "Generic_LegendaryChef", "Focal"
public string ChefName;
public List<CookingRecipe> recipes;
public List<bool> readPage;
```

### Procedural Cookbook Generation

**Naming by Style:**
- `Generic`: `<spice.cooking.cookbooks.genericName.!random>`
- `Generic_LegendaryChef`: `"Chef {ChefName}'s " + genericName`
- `Focal`: `<spice.cooking.cookbooks.focalName.!random>` with `$focus` replaced by focus ingredient

**Ingredient count by tier:**

| Tier | 1-ingredient | 2-ingredient | 3-ingredient |
|------|-------------|-------------|-------------|
| 0-1 | 25% | 60% | 15% |
| 2-3 | 15% | 50% | 35% |
| 4-5 | 5% | 47% | 48% |
| 6-7 | 0% | 35% | 65% |
| 8+ | 0% | 25% | 75% |

**Commerce value:** `50 + 10 * Tier`

**Focal style:** One ingredient is fixed (the "focus"), rest are random. Title uses the focus name.

### `Chef` (IPart)

**File:** `XRL.World.Parts/Chef.cs`

NPCs with the `Chef` part generate a cookbook and oven on `EnteredCell`:
1. Creates `BaseCookbook` with tier from current zone
2. Legendary chefs (`GivesRep`) get `Generic_LegendaryChef` style with 3 recipes
3. Creates `ChefOven` object with `Campfire` part containing `specificProcgenMeals`
4. Legendary chefs: all recipes become signature dishes
5. Regular chefs: one random recipe becomes signature
6. NPCs tagged `StiltChef` also get `HotandSpiny` recipe added
7. Places oven in a connected spawn location

### `TeachesDish` (IPart)

**File:** `XRL.World.Parts/TeachesDish.cs`

Simple data holder for NPCs that teach specific recipes:
```csharp
public string Text;
public CookingRecipe Recipe;
```

---

## 12. Ingredient Domains (Categories)

Caves of Qud does **NOT** use traditional flavor categories (spicy, sweet, salty, etc.). Instead, the system uses **domain-based** architecture where ingredients are categorized by technical type strings that map to XML blueprint objects.

### Blueprint Structure

Each ingredient domain is defined as a blueprint named `ProceduralCookingIngredient_{domain}` with these tags:

| Tag | Purpose |
|-----|---------|
| `Units` | Comma-separated `ProceduralCookingEffectUnit` class names (passive bonuses) |
| `Triggers` | Comma-separated `ProceduralCookingEffectWithTrigger` class names |
| `Actions` | Comma-separated `ProceduralCookingTriggeredAction` class names |
| `Description` | Human-readable description of the domain's effects |
| `RandomWeight` | Weight for random ingredient selection |
| `RandomHighTierWeight` | Weight for high-tier random selection |

Values starting with `@` are treated as population table references.

### Domain-to-Effect Mapping

The `Campfire` class uses three helper methods to check what a domain provides:

```csharp
public static bool HasTriggers(string type) =>
    !Blueprints["ProceduralCookingIngredient_" + type].GetTag("Triggers").IsNullOrEmpty();

public static bool HasActions(string type) =>
    !Blueprints["ProceduralCookingIngredient_" + type].GetTag("Actions").IsNullOrEmpty();

public static bool HasUnits(string type) =>
    !Blueprints["ProceduralCookingIngredient_" + type].GetTag("Units").IsNullOrEmpty();

public static bool IsHalfIngredient(string type) =>
    !HasTriggers(type) && !HasActions(type);  // Units only
```

### Complete Domain List (from source code analysis)

Organized by theme, each domain is identified by its type string:

| Domain Type String | Theme | Has Units | Has Triggers | Has Actions |
|---|---|---|---|---|
| `heatMinor` / `heat` | Fire/Heat | Yes | Yes | Yes |
| `coldMinor` / `cold` | Cold/Ice | Yes | Yes | Yes |
| `electricMinor` / `electric` | Electricity | Yes | Yes | Yes |
| `acidMinor` / `acid` | Acid | Yes | - | - |
| `strengthMinor` / `strength` | Strength | Yes | - | Yes |
| `agilityMinor` / `agility` | Agility | Yes | Yes | Yes |
| `armorMinor` / `armor` | Armor | Yes | Yes | Yes |
| `hpMinor` / `hp` | Hitpoints | Yes | Yes | Yes |
| `fearMinor` / `fear` | Fear/Intimidate | Yes | Yes | Yes |
| `loveMinor` / `love` | Love/Beguiling | Yes | Yes | Yes |
| `teleportMinor` / `teleport` | Teleportation | Yes | Yes | Yes |
| `phaseMinor` / `phase` | Phasing | Yes | Yes | Yes |
| `plantMinor` / `plant` | Plants | Yes | Yes | Yes |
| `fungusMinor` / `fungus` | Fungi | Yes | Yes | Yes |
| `medicinalMinor` / `medicinal` | Healing/Disease | Yes | Yes | Yes |
| `regenLowtierMinor` / `regenLowtier` | Regeneration (low) | Yes | Yes | Yes |
| `regenHightier` | Regeneration (high) | Yes | Yes | Yes |
| `rubberMinor` / `rubber` | Rubber/Jumping | Yes | Yes | Yes |
| `breathersMinor` / `breathers` | Breath weapons | Yes | - | - |
| `burrowingMinor` / `burrowing` | Burrowing | Yes | - | - |
| `cloningMinor` / `cloning` | Cloning | Yes | - | - |
| `tongueMinor` / `tongue` | Sticky Tongue | Yes | Yes | Yes |
| `quicknessMinor` / `quickness` | Quickness | Yes | - | - |
| `willpowerMinor` / `willpower` | Willpower | Yes | - | - |
| `darknessMinor` / `darkness` | DV/Darkness | Yes | - | - |
| `density` | Density (neutron flux) | Yes | - | - |
| `dissociation` | Body Dissociation | Yes | - | - |
| `egoMinor` / `ego` | Ego Projection | Yes | - | - |
| `slimeSpitting` | Slime Glands | Yes | - | - |
| `photosyntheticSkin` | Photosynthesis | Yes | - | - |
| `reflectMinor` / `reflect` | Damage Reflection | Yes | Yes | Yes |
| `stabilityMinor` / `stability` | Move Resistance | Yes | - | - |
| `secretsMinor` / `secrets` | Secret Revelation | Yes | - | - |
| `selfPoison` | Self-Harm (poison) | Yes | - | - |
| `selfGlotrot` | Self-Harm (glotrot) | Yes | - | - |
| `specialCrystal` | Crystal Transform | Yes | - | - |
| `specialSlog` | Slug Transform | Yes | - | - |
| `tastyMinor` | Taste (forces tasty) | Yes | - | - |
| `artifactMinor` / `artifact` | Artifact Identification | Yes | Yes | Yes |
| `attributesMinor` / `attributes` | All Stats Boost | Yes | - | - |

---

## 13. Procedural Cooking Effects Architecture

### `ProceduralCookingEffect` (Effect)

**File:** `XRL.World.Effects/ProceduralCookingEffect.cs`

```csharp
public long StartTick;
public List<ProceduralCookingEffectUnit> units;
public bool bApplied;
public bool init;
```

**Effect type:** `67108868`
**Display:** "metabolizing"
**Duration:** 1 (special handling -- persists until hunger events remove it)

### Removal Triggers

Effect removed on: `BecameHungry`, `BecameFamished`, `ApplyWellFed`, `RemoveProceduralCookingEffects`, `ClearFoodEffects`

### Non-Player Expiry

```csharp
public int GetNonPlayerDuration() {
    return Object?.GetPart<Stomach>()?.CalculateCookingIncrement() ?? 1200;
}
// Expires when Calendar.TotalTimeTicks >= StartTick + GetNonPlayerDuration()
```

Checked on `AIBoredEvent`, `AfterGameLoadedEvent`, `ZoneThawedEvent`, `JoinedPartyLeader`.

### Factory Methods

| Method | Inputs | Result |
|---|---|---|
| `CreateJustUnits(types)` | List of type strings | Plain effect with units from all types |
| `CreateTriggeredAction(trigger, action)` | Two type strings | Effect with trigger + action |
| `CreateBaseAndTriggeredAction(base, trigger, action)` | Three type strings | Effect with base unit + trigger + action |
| `CreateSpecific(units, triggers, actions)` | Optional lists of class names | Specific named units/triggers/actions |

### Blueprint Lookup and Instantiation

```csharp
private static void ApplyUnitFromTypeToEffect(ProceduralCookingEffect effect, string type) {
    GameObjectBlueprint bp = Blueprints["ProceduralCookingIngredient_" + type];
    string unitClass = bp.GetTag("Units").Split(',').GetRandomElement();
    if (unitClass.StartsWith("@"))
        unitClass = PopulationManager.RollOneFrom(unitClass.Substring(1)).Blueprint;
    effect.AddUnit(Activator.CreateInstance(
        ModManager.ResolveType("XRL.World.Effects." + unitClass)
    ) as ProceduralCookingEffectUnit);
}
```

Same pattern for Triggers and Actions.

### `ProceduralCookingEffectWithTrigger`

**File:** `XRL.World.Effects/ProceduralCookingEffectWithTrigger.cs`

Extends `ProceduralCookingEffect`. Adds `List<ProceduralCookingTriggeredAction> triggeredActions`.

**`Trigger()` method:** Called by trigger subclasses when condition met. Calls `Apply(Object)` on each triggered action and shows notification messages.

**Description format:** `"{unit descriptions}\n{trigger description} {action1}, {action2}, ..."`

### Deep Copy Pattern

```csharp
ProceduralCookingEffect copy = effect.DeepCopy(Parent);
copy.bApplied = false;
// Each unit deep-copied with parent reference updated
```

### Stacking

`SameAs()` always returns `false` -- cooking effects never stack as the "same effect."

---

## 14. Complete Domain Effect Catalogue

### 14.1 Passive Units (ProceduralCookingEffectUnit subclasses)

#### Stat Boost Units

| Class | Domain | Effect | Formula |
|-------|--------|--------|---------|
| `CookingDomainStrength_UnitStrength` | strength | +4 Strength | flat 4 |
| `CookingDomainAgility_UnitAgility` | agility | +4 Agility | flat 4 |
| `CookingDomainWillpower_UnitWillpower` | willpower | +4 Willpower | flat 4 |
| `CookingDomainQuickness_UnitQuickness` | quickness | +4-5 Quickness | `Stat.Random(4, 5)` |
| `CookingDomainLove_UnitEgo` | love | +4 Ego | flat 4 |
| `CookingDomainDarkness_UnitDV` | darkness | +4 DV | flat 4 |
| `CookingDomainArmor_UnitAV` | armor | +2 AV | flat 2 |
| `CookingDomainFear_UnitBonusMA` | fear | +2 MA | flat 2 |
| `CookingDomainHP_UnitHP` | HP | +10-15% max HP | `ceil(Tier * 0.01 * BaseHP)`, Tier=`Random(10,15)` |

#### Elemental Resistance Units

| Class | Domain | Effect | Formula |
|-------|--------|--------|---------|
| `CookingDomainHeat_UnitResist` | heat | +10-15 Heat Resistance | `Stat.Random(10, 15)` |
| `CookingDomainCold_UnitResist` | cold | +10-15 Cold Resistance | `Stat.Random(10, 15)` |
| `CookingDomainElectric_ResistUnit` | electric | +10-15 Electric Resistance | `Stat.Random(10, 15)` |
| `CookingDomainAcid_UnitResist` | acid | +10-15 Acid Resistance | `Stat.Random(10, 15)` |
| `CookingDomainElectric_ExtraResistUnit` | electric (high) | +50-75 Electric Resistance | `Stat.Random(50, 75)` |

#### Mutation Grant Units (via `ProceduralCookingEffectUnitMutation<T>`)

If target lacks the mutation: grants at `AddedTier`. If already has it: adds `BonusTier` bonus levels. Uses `Mutations.AddMutationMod()` with `SourceType.Cooking`.

| Class | Mutation | AddedTier | BonusTier |
|-------|----------|-----------|-----------|
| `CookingDomainHeat_UnitFlamingHands` | FlamingRay | 1-2 | 2-3 |
| `CookingDomainHeat_UnitBreatheFire` | FireBreather | 1-2 | 2-3 |
| `CookingDomainHeat_UnitPyrokinesis` | Pyrokinesis | 1-2 | 2-3 |
| `CookingDomainCold_UnitFreezingHands` | FreezingRay | 1-2 | 2-3 |
| `CookingDomainCold_UnitCryokinesis` | Cryokinesis | 1-2 | 2-3 |
| `CookingDomainCold_UnitBreatheIce` | IceBreather | 1-2 | 2-3 |
| `CookingDomainElectric_ElectricalGenerationUnit` | ElectricalGeneration | 1-2 | 2-3 |
| `CookingDomainElectric_EMPUnit` | ElectromagneticPulse | **2-3** | **3-4** |
| `CookingDomainAcid_UnitCorrosiveGas` | CorrosiveGasGeneration | 1-2 | 2-3 |
| `CookingDomainAcid_UnitBreatheCorrosiveGas` | CorrosiveBreather | 1-2 | 2-3 |
| `CookingDomainLove_UnitBeguiling` | Beguiling | 1-2 | 2-3 |
| `CookingDomainBurrowing_UnitBurrowingClaws` | BurrowingClaws | **5-6** | **3-4** |
| `CookingDomainEgo_UnitEgoProjection` | WillForce | **4-5** | **4-5** |
| `CookingDomainLiquidSpitting_UnitSlimeGlands` | SlimeGlands | **1** | **0** |
| `CookingDomainReflect_UnitQuills` | Quills | **5-6** | **3-4** |
| `CookingDomainTeleport_UnitTeleportOther` | TeleportOther | 1-2 | 2-3 |
| `CookingDomainTongue_UnitStickyTongue` | StickyTongue | **4-5** | **4-5** |
| `CookingDomainPhase_UnitPhasing` | Phasing | 1-2 | 2-3 |
| `CookingDomainPlant_UnitBurgeoningLowTier` | Burgeoning | 1-2 | 2-3 |
| `CookingDomainPlant_UnitBurgeoningHighTier` | Burgeoning | **7-8** | **5-6** |
| `CookingDomainArtifact_UnitPsychometry` | Psychometry | 1-2 | **3-4** |

**Breather variants** at fixed high tiers (all `ProceduralCookingEffectUnitMutation<T>`):

| Class | Mutation | Tier |
|-------|----------|------|
| `CookingDomainBreathers_UnitBreatheFire5/10` | FireBreather | 5/10 |
| `CookingDomainBreathers_UnitBreatheIce5/10` | IceBreather | 5/10 |
| `CookingDomainBreathers_UnitBreatheNormalityGas5/10` | NormalityBreather | 5/10 |
| `CookingDomainBreathers_UnitBreathePoisonGas5/10` | PoisonBreather | 5/10 |
| `CookingDomainBreathers_UnitBreatheShameGas5/10` | ShameBreather | 5/10 |
| `CookingDomainBreathers_UnitBreatheSleepGas5/10` | SleepBreather | 5/10 |
| `CookingDomainBreathers_UnitBreatheStunGas5/10` | StunBreather | 5/10 |

#### Skill Grant Units (via `ProceduralCookingEffectUnitSkill<T>`)

| Class | Skill | Bonus If Already Known |
|-------|-------|------------------------|
| `CookingDomainFear_UnitIntimidate` | Persuasion_Intimidate | `+(BonusTier * 2)` on Ego roll |

#### Regeneration Units

| Class | Effect | Formula |
|-------|--------|---------|
| `CookingDomainRegenLowtier_RegenerationUnit` | +10-15% healing rate | `1 + Tier*0.01` multiplier on `Regenerating2` |
| `CookingDomainRegenHightier_RegenerationUnit` | +100% healing rate | `Tier = 100`, doubles healing |
| `CookingDomainPhotosyntheticSkin_RegenerationUnit` | +20% + (level*10)% | `1 + (20 + Tier*10) * 0.01` |

#### Save Bonus Units

| Class | Effect | Formula |
|-------|--------|---------|
| `CookingDomainMedicinal_DiseaseResistUnit` | +3 vs disease | Adds 3 to Roll if Vs contains "Disease" |
| `CookingDomainRegenLowtier_BleedResistUnit` | +8-12 vs bleeding | `Stat.Random(8, 12)` |
| `CookingDomainStability_MoveResistUnit` | +6 vs move/knockdown/restraint | Flat 6 |

#### Reputation Units

| Class | Effect |
|-------|--------|
| `CookingDomainFungus_FungusReputationUnit` | +300 reputation with Fungi |
| `CookingDomainPlant_UnitPlantReputationLowTier` | +100 rep with 6 plant factions |
| `CookingDomainPlant_UnitPlantReputationHighTier` | +200 rep with 6 plant factions |

#### Damage Reflection Units

| Class | Effect | Formula |
|-------|--------|---------|
| `CookingDomainReflect_UnitReflectDamage` | Reflect 3-4% damage | `Max(1, ceil(Amount * Tier * 0.01))` |
| `CookingDomainReflect_UnitReflectDamageHighTier` | Reflect 15-18% damage | `Stat.Random(15, 18)` |

#### Special Mechanic Units

| Class | Effect |
|-------|--------|
| `CookingDomainTeleport_UnitBlink` | 20-25% chance to teleport and negate avoidable damage |
| `CookingDomainPhase_UnitDoublePhaseDuration` | Phase effects last 2x as long |
| `CookingDomainPhase_UnitPhaseOnDamage` | 15-20% chance to phase for 8-10 turns on damage |
| `CookingDomainDissociation_UnitOtherBodyBonus` | +40-70 Quickness in non-original body |
| `CookingDomainPhotosyntheticSkin_SatedUnit` | Keeps player fed, resets cooking counter |
| `CookingDomainPhotosyntheticSkin_UnitQuickness` | +13 + (PhotosyntheticSkin level * 2) Quickness |
| `CookingDomainFungus_FungusResistUnit` | 75% chance fungal infection doesn't develop |
| `CookingDomainMedicinal_IllResistUnit` | Ill lasts 1/10th duration |
| `CookingDomainRubber_ExtraJump` | Jump again after jumping |
| `CookingDomainRubber_Extra2Jumps` | Jump two more times after jumping |
| `CookingDomainRubber_ReducedFallDamage` | 50% fall damage reduction |
| `CookingDomainRubber_ExtraReducedFallDamage` | 90% fall damage reduction |
| `CookingDomainTaste_UnitDoNothing` | No effect (guarantees "tasty") |

#### Self-Harm Units

| Class | Effect |
|-------|--------|
| `CookingDomainSelfHarm_UnitSelfPoison` | Poisons the eater (1d4+4 duration) |
| `CookingDomainSelfHarm_UnitSelfGlotrot` | Applies GlotrotOnset |

#### Permanent Effect Units

| Class | Effect | Details |
|-------|--------|---------|
| `CookingDomainDensity_UnitPermanentAV` | +1 AV permanently | 10% chance gravitational collapse: `Explode(15000, "10d10+250", Neutron: true)` |
| `CookingDomainAttributes_UnitPermanentAllStats_25Percent` | 25% chance: +1 all 6 stats | Rolls 25% chance per application |
| `CookingDomainSpecial_UnitCrystalTransform` | Crystal transformation | Rebuilds body to "Crystal", adds Crystallinity mutation |
| `CookingDomainSpecial_UnitSlogTransform` | Slug transformation | Rebuilds body to "SlugWithHands", adds SlogGlands, achievement |
| `CookingDomainSecrets_RevealSecrets` | Reveals random journal secret | `JournalAPI.RevealRandomSecret()` |
| `CookingDomainCloning_UnitMultipleClones` | Creates 1-3 clones | `Stat.Random(1, 3)` clones at 60-80% player level |

### 14.2 Triggers (ProceduralCookingEffectWithTrigger subclasses)

| Class | Trigger Condition | Probability |
|-------|-------------------|-------------|
| `CookingDomainHP_OnDamaged` | Taking damage | 8-10% |
| `CookingDomainHP_OnDamagedMidTier` | Taking damage | 12-15% |
| `CookingDomainHP_OnLowHealth` | Below 20% HP | 100% (one-shot) |
| `CookingDomainHP_OnLowHealthMidTier` | Below 30% HP | 100% |
| `CookingDomainAgility_OnPerformCriticalHit` | Critical hit (melee/missile) | 100% |
| `CookingDomainArmor_OnPenetration` | Suffering 2X+ penetration | 100% |
| `CookingDomainHeat_OnEnflamed` | Temp >= FlameTemperature | 100% (one-shot) |
| `CookingDomainHeat_OnDealingHeatDamage` | Dealing fire damage | 10% |
| `CookingDomainCold_OnSlowedByCold` | Temp <= FreezeTemperature | 100% (one-shot) |
| `CookingDomainCold_OnDealingColdDamage` | Dealing cold damage | 25% |
| `CookingDomainElectric_OnElectricDamaged` | Taking electric damage | 50% |
| `CookingDomainElectric_OnDealingElectricDamage` | Dealing electric damage | 25% |
| `CookingDomainFear_OnFeared` | Becoming afraid | 100% |
| `CookingDomainFear_OnEatLah` | Eating dreadroot/lah petal | 100% |
| `CookingDomainFungus_OnItchy` | Getting itchy skin | 100% |
| `CookingDomainFungus_OnEatFungus` | Eating a mushroom | 100% |
| `CookingDomainMedicinal_OnEatYuckwheat` | Eating yuckwheat stem | 100% |
| `CookingDomainMedicinal_OnDrinkHoney` | Drinking honey | 100% (magnified) |
| `CookingDomainRegenLowtier_OnDamaged` | Taking damage | 8-10% |
| `CookingDomainRegenHightier_OnDamaged` | Taking damage | 30-40% |
| `CookingDomainRegenLowtier_OnSalve` | Using salve/ubernostrum | 100% |
| `CookingDomainLove_OnGainFollower` | Gaining non-trifling follower | 100% |
| `CookingDomainPhase_OnPhaseOut` | Phasing out | 100% |
| `CookingDomainPhase_OnPhaseIn` | Phasing in | 100% |
| `CookingDomainTeleport_OnTeleport` | Teleporting | 100% |
| `CookingDomainReflect_OnDamaged` | Taking damage | 8-10% |
| `CookingDomainReflect_OnDamagedHighTier` | Taking damage | 16-20% |
| `CookingDomainReflect_OnReflectedDamage` | Reflecting damage | 50% |
| `CookingDomainReflect_OnReflectedDamageHighTier` | Reflecting damage | 100% |
| `CookingDomainRubber_OnProne` | Going prone | 100% |
| `CookingDomainRubber_OnJump` | Jumping | 100% |
| `CookingDomainTongue_OnStuck` | Getting stuck | 50% |
| `CookingDomainPlant_OnDamageByPlant` | Damage from a plant | 100% |
| `CookingDomainArtifact_OnIdentify` | Identifying artifact | 100% |
| `OnKillToughProceduralCookingTrigger` | Killing creature (Con >= 5) | 100% |
| `OnHealProceduralCookingTrigger` | Healing to full from half | 100% |
| `OnDrinkWaterProceduralCookingTrigger` | Drinking freshwater | 25% (magnified) |

### 14.3 Triggered Actions (ProceduralCookingTriggeredAction subclasses)

#### Resistance Burst Actions

| Class | Effect | Duration |
|-------|--------|----------|
| `CookingDomainHeat_HeatResist_*` | +40-50 Heat Resistance | 300 turns (6 hrs) |
| `CookingDomainHeat_LargeHeatResist_*` | +125-175 Heat Resistance | 50 turns |
| `CookingDomainCold_ColdResist_*` | +40-50 Cold Resistance | 300 turns |
| `CookingDomainCold_LargeColdResist_*` | +125-175 Cold Resistance | 50 turns |
| `CookingDomainElectric_SmallElectricResist_*` | +40-50 Electric Resistance | 300 turns |
| `CookingDomainElectric_LargeElectricResist_*` | +125-175 Electric Resistance | 50 turns |

#### Stat Burst Actions

| Class | Effect | Duration |
|-------|--------|----------|
| `CookingDomainStrength_LargeStrengthBuff_*` | +8 Strength | 50 turns |
| `CookingDomainAgility_LargeAgilityBuff_*` | +8 Agility | 50 turns |
| `CookingDomainArmor_LargeAVBuff_*` | +6 AV | 50 turns |
| `CookingDomainHP_IncreaseHP_*` | +30-40% max HP | 50 turns |
| `CookingDomainHP_HealToFull_*` | Heal to full HP | 15% chance, instant |

#### Immunity Actions

| Class | Effect | Duration |
|-------|--------|----------|
| `CookingDomainFear_FearImmunity_*` | Immune to fear | 300 turns |
| `CookingDomainFungus_SporeImmunity_*` | Immune to spores | 300 turns |
| `CookingDomainMedicinal_DiseaseImmunity_*` | Immune to disease onset | 300 turns |
| `CookingDomainPhase_NoPhase_*` | Can't be phased | 100 turns |

#### Ability Cast Actions

| Class | Effect |
|-------|--------|
| `CookingDomainHeat_FlamingHandsCharge_*` | Cast FlamingRay at level 5-6 |
| `CookingDomainHeat_PyrokinesisCharge_*` | Cast Pyrokinesis at level 5-6 |
| `CookingDomainCold_FreezingHandsCharge_*` | Cast FreezingRay at level 5-6 |
| `CookingDomainCold_CryokinesisCharge_*` | Cast Cryokinesis at level 5-6 |
| `CookingDomainElectric_EMP_*` | EMP at level 8-9, damage: `Random(4+Tier*2, 13+Tier*2)` |
| `CookingDomainElectric_Discharge_*` | Electrical discharge at level 5-6 |
| `CookingDomainFear_Intimidate_*` | Free Intimidate action |
| `CookingDomainAgility_Shank_*` | Perform Shank attack |
| `CookingDomainStrength_DismemberOrSlam_*` | 50% Dismember or 50% Slam |
| `CookingDomainLove_BeguilingCharge_*` | Beguile at rank 7-8 |
| `CookingDomainTeleport_Teleport_*` | Teleport at level 5-6 |
| `CookingDomainTeleport_MassTeleportOther_*` | Teleport all adjacent creatures |
| `CookingDomainPhase_Phase_*` | Phase out for 20 turns |
| `CookingDomainReflect_QuillBurst_*` | Expel quills at level 8-9 (1d4+1 per cell) |
| `CookingDomainReflect_Reflect100_*` | 100% reflect next hit, 50 turns |
| `CookingDomainReflect_Reflect100HighTier_*` | 100% reflect next 3 hits, 50 turns |
| `CookingDomainTongue_ThreeTongues_*` | Shoot 3 sticky tongues at rank 10 |
| `CookingDomainPlant_BurgeoningHighTier_*` | Friendly plants at level 10 |
| `CookingDomainArtifact_IdentifyAllInZone_*` | Identify all artifacts on map |

#### Cleansing Actions

| Class | Effect |
|-------|--------|
| `CookingDomainLowtierRegen_StopBleeding_*` | Remove all Bleeding (up to 10) |
| `CookingDomainLowtierRegen_RemoveDebuff_*` | Remove 1 random minor debuff |
| `CookingDomainHightierRegen_RemoveDebuff_*` | Remove 1 random major debuff (or minor) |
| `CookingDomainMedicinal_RemoveIll_*` | Cure Ill status |
| `CookingDomainMedicinal_RemoveDiseaseOnset_*` | 25% cure GlotrotOnset/IronshankOnset |

#### Jump Actions

| Class | Effect |
|-------|--------|
| `CookingDomainRubber_JumpTwice_*` | Jump twice at +1 range |
| `CookingDomainRubber_JumpThrice_*` | Jump three times at +2 range |

---

## 15. Basic "Tasty Meal" Effects

Granted by `RollTasty()` -- 10% base chance (or forced by `tastyMinor`/salt). One randomly selected:

| Index | Class | Effect | Formula |
|-------|-------|--------|---------|
| 0 | `BasicCookingEffect_Hitpoints` | +10% base HP | `Max(Round(BaseStat("Hitpoints") * 0.1), 1)` |
| 1 | `BasicCookingEffect_MA` | +1 MA | Via StatShifter |
| 2 | `BasicCookingEffect_MS` | +6% Move Speed | `Max(Round((200 - BaseStat("MoveSpeed")) * 0.06), 1)` |
| 3 | `BasicCookingEffect_Quickness` | +3% Quickness | `Max(Round(BaseStat("Speed") * 0.03), 1)` |
| 4 | `BasicCookingEffect_RandomStat` | +1 random stat | Random from {Str, Int, Will, Agi, Tou, Ego} |
| 5 | `BasicCookingEffect_Regeneration` | +10% healing rate | Multiplies `Regenerating2` Amount by 1.1 |
| 6 | `BasicCookingEffect_ToHit` | +1 to hit | Adds to `GetToHitModifierEvent` |
| 7 | `BasicCookingEffect_XP` | +5% XP gained | Intercepts `AwardingXPEvent`, adds `Amount/20` |

All share the same event-driven expiry mechanism (removed on hunger state changes).

---

## 16. Named (Preset) Recipes

All in namespace `XRL.World.Skills.Cooking`, extending `CookingRecipe`:

| Recipe | Ingredients | Effect Summary |
|--------|-------------|----------------|
| **Apple Matz** | Vinewafer Sheaf + Starapple Preserves | +10-15% healing rate, half thirst rate |
| **Mah Lah Soup** | Dried Lah Petals + Vinewafer Sheaf | On drink water: 25% chance fear immunity 6hrs |
| **The Porridge** | Fermented Yuckwheat Stem + Spark Tick Plasma + honey | +3 disease save; on eat yuckwheat: electrical discharge |
| **Goat in Sweet Leaf** | Goat Jerky + Sun-Dried Banana + Fermented Yondercane | Psychometry 4-5; 12-15% mass teleport on damage |
| **Mulled Mushroom Cider** | Pickled Mushrooms + Starapple Preserves + cider | +4-5 Quickness, +8-12 bleed save, 75% fungus resist |
| **Bone Babka** | Compacted Bone Meal + Sun-Dried Banana + Congealed Skulk | Burrowing Claws 5-6; on identify: +6 AV 50 turns |
| **Tongue and Cheek** | Fermented Tongue + Sliced Bop Cheek + Congealed Love | Sticky Tongue 4-5; on jump: beguile at 7-8 |
| **Hot and Spiny** | Cured Dawnglider Tail + Spine Fruit Jam | +10-15 Heat Resist, 3-4% damage reflect |
| **Crystal Delight** | Crystal of Eve + warmstatic + GlitterGrenade3 + Gentling Collar | **PERMANENT**: Crystal body transformation |
| **Cloaca Surprise** | goo + sludge + ooze (liquids) | **PERMANENT**: Slug body transformation |

**Crystal Delight** and **Cloaca Surprise** have custom `ApplyEffectsTo()` overrides that suppress the normal metabolizing message -- they cause permanent body transformations.

### Cloaca Surprise Transformation Detail

```csharp
public static void ApplyTo(GameObject Object) {
    if (Object.GetPropertyOrTag("AteCloacaSurprise") == "true") {
        Object.ShowFailure("Your genome has already undergone this transformation.");
        return;
    }
    // Flavor text popups...
    Object.Body.Rebuild("SlugWithHands");
    Object.RequirePart<Mutations>().AddMutation(new SlogGlands());
    Object.Render.RenderString = "Q";
    Object.Render.Tile = "Creatures/sw_slog.bmp";
    Object.SetStringProperty("AteCloacaSurprise", "true");
    Object.SetStringProperty("Species", "slug");
}
```

---

## 17. Nostrums (Medical Cooking)

The campfire serves as a medical station when the player has `Physic_Nostrums` skill:

### Stop Bleeding
- Finds player + adjacent companions with `Bleeding` effects
- Removes all non-internal bleeding
- Internal bleeding: "wounds are too deep to treat"
- Phase/stasis checks applied

### Treat Poison
- Finds companions with poison effects (type 67174400)
- Requires "Medicinal" tagged ingredient or `regenLowtier` `PreparedCookingIngredient`
- Consumes one ingredient per treatment
- Removes all poison effects

### Treat Illness
- Removes all `Ill` effects
- Same medicinal ingredient consumption pattern

### Treat Disease Onset
- Gets disease onset effects via `GetDiseaseOnsetEvent`
- **20% chance** to cure the disease outright
- Otherwise: applies `BoostedImmunity` effect (if not already present)
- Can only boost immunity once per creature

---

## 18. Cooking Skill Tree

All in `XRL.World.Parts.Skill`:

| Skill | Effect |
|-------|--------|
| `CookingAndGathering` | Base skill. Gates: choose ingredients, cook from recipe, preserve. Priority = `int.MinValue` |
| `CookingAndGathering_Harvestry` | Enables plant harvesting. Toggleable ability "Harvest Plants" (key 'h'). Auto-harvest during autoexplore |
| `CookingAndGathering_Butchery` | Enables corpse butchering. Toggleable ability "Butcher Corpses". Auto-butcher during autoexplore |
| `CookingAndGathering_Spicer` | Allows **3 ingredients** instead of 2 when choosing ingredients |
| `CookingAndGathering_MealPreparation` | Passive. Adds 3 "salt" elements for water ritual trades |
| `CookingAndGathering_CarbideChef` | Grants `Inspired` effect. On level-up: always. On new zone: 5% chance. Duration: 2400 |

### Carbide Chef & Inspired

```csharp
public void Inspire() {
    if (!ParentObject.HasEffect<Inspired>())
        ParentObject.ApplyEffect(new Inspired(2400));
}
// Triggers: AfterLevelGainedEvent (100%), "VisitingNewZone" (5%)
```

When `Inspired` is active during "Choose Ingredients" cooking:
1. Generates 3 possible effects
2. Player chooses one
3. Creates permanent recipe, learned automatically
4. Journal accomplishment logged
5. `Inspired` effect consumed

---

## 19. Automated Food Processing

### `FoodProcessor` (IPoweredPart)

**File:** `XRL.World.Parts/FoodProcessor.cs`

```csharp
ChargeUse = 500;
WorksOnInventory = true;
```

Every turn, processes items in its inventory:
1. First tries to butcher via `Butcherable.AttemptButcher()` (SkipSkill: true)
2. Then tries to preserve via `Campfire.PerformPreserve()` for items with `PreservableItem`
3. Consumes 500 charge per processed item

### `Mill` (IPoweredPart)

**File:** `XRL.World.Parts/Mill.cs`

```csharp
public string Transformations;     // "Blueprint:ResultBlueprint" pairs
public string TagTransformations;  // "Tag:ResultBlueprint" pairs
ChargeUse = 1;
WorksOnInventory = true;
```

Every turn, processes items:
1. Checks `Transformations` dictionary by exact blueprint name
2. If no match, checks `TagTransformations` by tag
3. If result is empty string: tries butchering, then preserving
4. If result is a blueprint: replaces item with new blueprint
5. Consumes charge per transformation

---

## 20. Summary Tables

### Power Tiers

| Tier | Passive (Units) | Triggered Actions |
|------|-----------------|-------------------|
| **Low** | +1 stat, +10% healing, +2 AV/MA | +40-50 resist for 300 turns |
| **Medium** | +4 stat, +10-15 resist, +10-15% HP | +8 stat for 50 turns, immunity 300 turns |
| **High** | +50-75 resist, +100% healing, mutations tier 5-10 | +125-175 resist for 50 turns, cast abilities level 5-9 |
| **Permanent** | +1 AV (10% death), +1 all stats (25%), body transforms | N/A |

### Trigger Probability Ranges

| Range | Probability | Examples |
|-------|-------------|---------|
| Rare | 8-10% | On damage (HP, Reflect, RegenLowtier) |
| Uncommon | 12-25% | On damage mid-tier, on dealing elemental damage |
| Common | 30-50% | On electric damage, on reflected damage, on stuck |
| Guaranteed | 100% | On feared, phased, killed, critical hit, specific eating |

### Key Constants

| Constant | Value | Context |
|----------|-------|---------|
| `COOKING_INCREMENT` | 1200 turns | Base hunger cycle |
| `SMALL_HUNGER_AMOUNT` | 200 | Snack satiation reduction |
| Max meals while sated | 3 | `CookCount < 3` |
| Default ingredient slots | 2 | Campfire cooking |
| Spicer extra slot | +1 (total 3) | `CookingAndGathering_Spicer` |
| Tasty base chance | 10% | `RollTasty()` |
| Campfire max temperature | 600 | Heat mechanics |
| Campfire heat per tick | 150 | 10% chance/turn |
| Cookbook commerce value | `50 + 10 * Tier` | Commerce |
| Harvest energy cost | 1000 | Skill-type energy |
| Butchery energy cost | `1000 / max(ToolBonus + 1, 1)` | Tool-dependent |
| Eating energy cost | 1000 | Standard |
| Inspired duration | 2400 | Carbide Chef effect |
| Small buff duration | 300 turns (6 hours) | Resistance/immunity actions |
| Large buff duration | 50 turns (1 hour) | Stat/resist burst actions |

### Ingredient Source Mapping

| Source | Gathering Method | Result | Cooking Domain |
|--------|-----------------|--------|----------------|
| Starapple Tree | Harvest | Starapple -> Preserves | Via blueprint tags |
| Vinewafer Plant | Harvest | Vinewafer Sheaf | Via blueprint tags |
| Lah Bush | Harvest | Lah Petals -> Dried | Via blueprint tags |
| Yondercane | Harvest | Yondercane -> Fermented | Via blueprint tags |
| Corpses | Butcher | Meat/Bone products | Via blueprint tags |
| Mushrooms | Harvest | Mushrooms -> Pickled | Via blueprint tags |
| Acid pools | Container | Liquid acid | `"acidMinor"` |
| Honey (liquid) | Container | Liquid honey | `"medicinalMinor"` |
| Salt water | Container | Liquid salt | `"tastyMinor"` |
| Lava | Container | Liquid lava | `"heatMinor"` |
| Cider | Container | Liquid cider | `"quicknessMinor"` |
| Cloning draught | Container | Liquid cloning | `"cloningMinor"` |
| Convalessence | Container | Liquid convalessence | `"coldMinor,regenLowtierMinor"` |
| Neutron flux | Container | Liquid neutron flux | `"density"` |
| Slime | Container | Liquid slime | `"slimeSpitting"` |
| Goo | Container | Liquid goo | `"selfPoison"` |
| Sludge | Container | Liquid sludge | `"selfPoison"` |
| Ooze | Container | Liquid ooze | `"selfGlotrot"` |
| Algae | Container | Liquid algae | `"plantMinor"` |
| Tar (asphalt) | Container | Liquid asphalt | `"stabilityMinor"` |
