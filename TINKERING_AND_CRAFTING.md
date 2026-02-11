# Tinkering & Crafting System - Deep Dive

> Source analysis of Caves of Qud's tinkering, crafting, power cell, and modification systems from decompiled C# source code.

---

## Table of Contents

1. [System Overview](#system-overview)
2. [Tinkering Skill Tree](#tinkering-skill-tree)
3. [Bits - Crafting Materials](#bits---crafting-materials)
4. [Data Disks & Schematics](#data-disks--schematics)
5. [Building Items](#building-items)
6. [Item Modifications (Mods)](#item-modifications-mods)
7. [Disassembly](#disassembly)
8. [Reverse Engineering](#reverse-engineering)
9. [Power Cells & Energy System](#power-cells--energy-system)
10. [Recharging](#recharging)
11. [Overloading](#overloading)
12. [Repair System](#repair-system)
13. [Sifrah Minigames](#sifrah-minigames)
14. [Tinkering UI](#tinkering-ui)
15. [Maker's Mark System](#makers-mark-system)
16. [Key Events & Extension Points](#key-events--extension-points)

---

## System Overview

Tinkering is Caves of Qud's crafting system. It revolves around a closed-loop economy:

1. **Acquire schematics** (data disks) that teach recipes
2. **Collect bits** (tiered crafting materials) by disassembling items
3. **Build** new items from known recipes + bits
4. **Mod** existing items with learned modifications + bits
5. **Power** crafted items with energy cells
6. **Recharge** depleted cells using bits
7. **Overload** items for boosted performance at higher power draw

### Core Source Files

| System | Primary Source |
|--------|---------------|
| Skill tree | `XRL.World.Parts.Skill/Tinkering.cs`, `Tinkering_Tinker1.cs`, `Tinkering_Tinker2.cs`, `Tinkering_Tinker3.cs` |
| Bits | `XRL.World.Tinkering/BitType.cs`, `XRL.World.Parts/BitLocker.cs`, `XRL.World.Tinkering/BitCost.cs` |
| Data disks | `XRL.World.Parts/DataDisk.cs` |
| Recipes | `XRL.World.Tinkering/TinkerData.cs` |
| Items | `XRL.World.Parts/TinkerItem.cs` |
| Disassembly | `XRL.World.Tinkering/Disassembly.cs` |
| Modifications | `XRL.World.Parts/IModification.cs`, `XRL.World/ModificationFactory.cs`, `XRL.World.Tinkering/ItemModding.cs` |
| Power cells | `XRL.World.Parts/IEnergyCell.cs`, `EnergyCell.cs`, `EnergyCellSocket.cs` |
| UI | `Qud.UI/TinkeringStatusScreen.cs`, `XRL.UI/TinkeringScreen.cs` |
| Helpers | `XRL.World.Tinkering/TinkeringHelpers.cs` |

---

## Tinkering Skill Tree

Tinkering is a 3-tier skill progression with several support skills. Each tier unlocks access to higher-tier recipes and provides bonus abilities.

### Tier 1: Tinker I (`Tinkering_Tinker1`)

**On skill acquisition:**
- Grants one random recipe from tiers 0-3 (if `ReceivedTinker1Recipe` property not set)
- Adds **Recharge** activated ability (hotkey `0x9B`)
- Awards Sifrah insight XP for players

**Abilities:**
- **Recharge** — Spend bits to recharge energy cells (see [Recharging](#recharging))
- Can build/learn recipes of tier 1-3

### Tier 2: Tinker II (`Tinkering_Tinker2`)

**On skill acquisition:**
- Grants one random recipe from tiers 4-6 (if `ReceivedTinker2Recipe` property not set)
- Awards Sifrah insight XP

**Abilities:**
- Can build/learn recipes of tier 4-6
- +2 levels to gadget inspection (identification)

### Tier 3: Tinker III (`Tinkering_Tinker3`)

**On skill acquisition:**
- Grants recipe from tier 7+
- Awards Sifrah insight XP

**Abilities:**
- Can build/learn recipes of tier 7+
- +1 level to gadget inspection

### Support Skills

| Skill | Effect |
|-------|--------|
| **Gadget Inspector** (`Tinkering_GadgetInspector`) | +5 bonus to "Inspect" type tinkering checks. Base 3 identification levels (requires no Dystechnia) |
| **Disassemble** (`Tinkering_Disassemble`) | Enables breaking down items into bits. Unlocks auto-disassembly and scrap toggle |
| **Repair** (`Tinkering_Repair`) | Repair damaged/rusted items using bits |
| **Reverse Engineer** (`Tinkering_ReverseEngineer`) | Learn recipes by disassembling items. +2 scholarship. Enables mod recipe learning from disassembly |

### Identification Formula

The inspection/identification level is calculated as:

```
base = 3 (if has GadgetInspector AND not Dystechnia)
     + 2 (if has Tinker2)
     + 1 (if has Tinker3)

bonus = base * (Intelligence_modifier * 3) / 100 + GetTinkeringBonusEvent
```

### Skill-to-Tier Requirements

| Recipe Tier | Required Skill | Display Name |
|-------------|---------------|--------------|
| 1-3 | `Tinkering_Tinker1` | Tinker I |
| 4-6 | `Tinkering_Tinker2` | Tinker II |
| 7+ | `Tinkering_Tinker3` | Tinker III |

---

## Bits - Crafting Materials

Bits are the universal crafting currency. There are **12 bit types** organized across **9 tiers** (0-8). Tier 0 contains four types of scrap; tiers 1-8 each have one progressively rarer material.

### Complete Bit Table

| Char | Tier | Color Code | Description | Category |
|------|------|-----------|-------------|----------|
| `R` | 0 | Red | scrap power systems | Scrap |
| `G` | 0 | Green | scrap crystal | Scrap |
| `B` | 0 | Blue | scrap metal | Scrap (tier priority) |
| `C` | 0 | Cyan | scrap electronics | Scrap |
| `r` | 1 | dark red | phasic power systems | Advanced |
| `g` | 2 | dark green | flawless crystal | Advanced |
| `b` | 3 | dark blue | pure alloy | Advanced |
| `c` | 4 | dark cyan | pristine electronics | Advanced |
| `K` | 5 | black | nanomaterials | Exotic |
| `W` | 6 | white | photonics | Exotic |
| `Y` | 7 | yellow | AI microcontrollers | Exotic |
| `M` | 8 | magenta | metacrystal | Exotic |

> `B` (scrap metal) is the **tier priority** bit for tier 0 — it serves as the default/representative bit for that tier.

### Bit Display

Bits can display as either symbols or alphanumeric characters (controlled by `Options.AlphanumericBits`). The translation mapping:

| Bit Char | Alpha Display |
|----------|--------------|
| R | A |
| r | 1 |
| g | 2 |
| b | 3 |
| c | 4 |
| K | 5 |
| W | 6 |
| Y | 7 |
| M | 8 |

### BitLocker (Storage)

Every creature/container that holds bits has a `BitLocker` part (`XRL.World.Parts/BitLocker.cs`).

**Storage structure:**
```csharp
public Dictionary<char, int> BitStorage;  // bit character -> count
```

**Key operations:**
- `AddBits(string bits)` — Parse string, add one bit per character
- `UseBits(string bits)` — Consume bits; returns false if insufficient
- `HasBits(string bits)` — Check availability without consuming
- `GetBitCount(char bit)` — Count of a specific type
- `GetTotalBitCount()` — Sum of all bits
- `AddAllBits(int num)` — Add `num` of every bit type (debug/wish)

### BitCost (Cost Representation)

`BitCost` extends `Dictionary<char, int>` to represent bit costs for recipes and operations.

**Key methods:**
- `Import(string bits)` — Parse a bit string like `"RRGBBr"` into `{R:2, G:1, B:2, r:1}`
- `ToBits()` — Convert back to sorted bit string
- `GetHighestTier()` — Returns the maximum tier present in the cost
- `ToString()` — Color-coded display string

### Bit Cost Templates

Item bit costs in blueprints use a **template system** with numeric placeholders:

```
Template: "01234"
Conversion: BitType.ToRealBits(template, blueprint)
```

Each digit N maps to a random bit from tier N, with seeded randomness based on blueprint name for consistency. The system has small chances to "level down" (select a lower tier) or "level up" (select a higher tier) during conversion.

### Data Disk Currency Values

Data disks have commerce value based on their highest-tier bit:

| Highest Bit | Currency Value |
|-------------|---------------|
| M (tier 8) | 450 |
| Y (tier 7) | 400 |
| W (tier 6) | 350 |
| K (tier 5) | 300 |
| c (tier 4) | 250 |
| b (tier 3) | 200 |
| g (tier 2) | 150 |
| r (tier 1) | 100 |
| Tier 0 | 50 |

---

## Data Disks & Schematics

Data disks (`XRL.World.Parts/DataDisk.cs`) are the primary way players learn tinkering recipes. Each disk contains a single `TinkerData` recipe.

### TinkerData Structure

```csharp
public class TinkerData : IComposite
{
    public string DisplayName;      // Human-readable name
    public string Blueprint;        // Item blueprint ID or "[mod]ModName"
    public string Category;         // UI category (weapon, armor, utility, etc.)
    public string Type;             // "Build" or "Mod"
    public int Tier;                // 1-9
    public string Cost;             // Bit cost string (e.g., "bcY")
    public string Ingredient;       // Comma-separated optional ingredient blueprints
}
```

### Recipe Types

- **Build** (`Type = "Build"`) — Creates a new item from scratch
- **Mod** (`Type = "Mod"`) — Applies a modification to an existing item. Blueprint stored as `"[mod]PartName"`

### Learning a Recipe

When a player uses the "Learn" action on a data disk:

1. Check if recipe already known via `TinkerData.RecipeKnown(Data)`
2. Verify player has required skill tier (`DataDisk.GetRequiredSkill(Data.Tier)`)
3. Add to `TinkerData.KnownRecipes` list
4. Create a sample object, mark as understood, show popup
5. Play sound `"Sounds/Interact/sfx_interact_dataDisk_learn"`
6. **Destroy the disk**

### Recipe Discovery

Recipes are loaded from all game blueprints at startup:

```csharp
// Build recipes: from blueprints with TinkerItem part
TinkerItem.LoadBlueprint(blueprint)
// - Skips BaseObject, NoDataDisk tags, items without TinkerItem
// - Requires CanBuild = true
// - Skips items with SubstituteBlueprint set

// Mod recipes: from ModificationFactory
ModificationFactory._ModList entries where TinkerAllowed = true
```

### Data Disk Generation (Targeting)

Data disks can be generated with target parameters for intelligent recipe selection:

```csharp
public int TargetTier;              // Desired item tier
public int TargetTechTier;          // Desired tech tier
public int TargetTinkerTier;        // Desired tinkering skill tier
public string TargetBlueprint;      // Specific blueprint
public string TargetTinkerCategory; // Category filter
public string TargetType;           // "Build" or "Mod"
```

**Scoring algorithm** (higher score = better match):
- Tier match: `+(8 - abs(difference))`
- TechTier match: `+(8 - abs(difference))`
- TinkerTier match: `+(8 - abs(difference))`
- Category match: `+16`
- Type match: `+32`
- Base: `+1`

The system attempts up to `maxScore × 10` random selections, then iterates the full shuffled list if no perfect match found.

### Recipe Learning on Skill Acquisition

When a player acquires a Tinker skill tier, they get to choose from **3 random data disks** of the appropriate tier range. NPCs get 1 option.

### Serialization

Known recipes persist across save/load:
- `TinkerItem.SaveGlobals()` serializes `BitCostMap` and `KnownRecipes`
- `TinkerItem.LoadGlobals()` restores them via `ReadComposite<TinkerData>()`

---

## Building Items

Building is the core crafting action — constructing new items from known recipes and bits.

### Build Process (`TinkeringScreen.PerformUITinkerBuild`)

1. **Validate skill requirement** — Check player has required Tinker tier
2. **Check ingredients** — If recipe has `Ingredient` field (comma-separated blueprints), verify player has them in inventory
3. **Calculate bit cost** — Get base cost, apply `ModifyBitCostEvent` modifiers
4. **Check hostiles** — If enemies nearby, warn and optionally exit
5. **Consume ingredients** — Remove from inventory
6. **Consume bits** — Via `BitLocker.UseBits()`
7. **Create items** — `GameObject.Create(blueprint)` × `TinkerItem.NumberMade`
8. **Process each item** — `TinkeringHelpers.ProcessTinkeredItem()`:
   - `StripForTinkering()` — Remove energy cell, clear ammo, empty liquids
   - Mark as understood
   - Set `"TinkeredItem"` property to 1
   - `CheckMakersMark()` — Optionally add crafter's mark
9. **Fire events** — `TakenEvent` and `TookEvent`
10. **Consume energy** — 1000 energy units (`"Skill Tinkering Build"`)
11. **Play sound** — `"sfx_ability_buildRecipeItem"`

### TinkerItem Part

Every buildable/disassemblable item has a `TinkerItem` part:

```csharp
public class TinkerItem : IPart
{
    public bool CanDisassemble = true;    // Can be broken down for bits
    public bool CanBuild;                 // Can be built via data disk
    public int BuildTier = 1;             // Required Tinker tier
    public int NumberMade = 1;            // Items created per build
    public string Ingredient = "";        // Optional ingredient blueprints
    public string SubstituteBlueprint;    // Alternate item basis
}
```

### Build From Data Disk (Direct)

Players can also build directly from an unlearned data disk (the "Build" inventory action):
- Same process as above
- Does NOT consume the disk (disk is reusable for building)
- Still requires the appropriate Tinker skill tier

---

## Item Modifications (Mods)

The modification system allows adding special properties to existing items. There are **100+ different modifications** organized by item type and rarity.

### Modification Architecture

```
IModification (abstract base)
├── IMeleeModification (melee weapons only - rejects missile weapons)
├── ModFlaming, ModFreezing, ModElectrified...  (damage mods)
├── ModOverloaded, ModMassivelyOverloaded...     (power mods)
├── ModPadded, ModReinforced, ModHardened...     (defense mods)
├── ModDisplacer, ModMorphogenetic...            (special effect mods)
├── ModExtradimensional...                       (random bonus mods)
└── ModImprovedMutationBase                      (mutation enhancement mods)
```

### IModification Base Properties

```csharp
public abstract class IModification : IActivePart
{
    public int Tier;  // Scales effect based on item quality

    void ApplyTier(int Tier);
    void Configure();              // Subclass override for setup
    void TierConfigure();          // Tier-specific configuration
    int GetModificationSlotUsage(); // Default: 1 slot

    bool ModificationApplicable(GameObject obj);  // Can this mod go on this item?
    bool BeingAppliedBy(GameObject obj, GameObject who);
    void ApplyModification(GameObject obj);       // Apply the effect

    void IncreaseComplexity(int amount);
    void IncreaseDifficulty(int amount);
    void IncreaseDifficultyAndComplexity(int d, int c);
}
```

### Modification Tables

Mods are organized into tables that determine which item types they can appear on:

| Table | Item Types |
|-------|-----------|
| `MeleeWeapon` | Swords, axes, daggers, etc. |
| `MissileWeapon` | Guns, bows, launchers |
| `Armor` | Body armor, helmets, boots, etc. |
| `Shield` | Shields and bucklers |
| `Misc` | Grenades, tools, other items |

### ModEntry Structure

Each mod is registered in `ModificationFactory` with metadata:

```csharp
public class ModEntry
{
    public string Part;              // Class name (e.g., "ModFlaming")
    public string Tables;            // Comma-separated mod groups
    public int Rarity;               // 0=C, 1=U, 2=R, 3=R2, 4=R3
    public int MinTier = 1;          // Minimum item tier
    public int MaxTier = 99;         // Maximum item tier
    public int NativeTier;           // Tier where mod is most common
    public int TinkerTier = 1;       // Tinkering cost tier
    public double Value = 1.0;       // Commerce value multiplier
    public string TinkerDisplayName; // Name in tinkering UI
    public string Description;       // Tooltip text
    public string TinkerIngredient;  // Required crafting ingredient
    public string TinkerCategory;    // UI category (default: "utility")
    public bool TinkerAllowed = true;   // Can be applied via tinkering
    public bool BonusAllowed = true;    // Can appear as random bonus
    public bool CanAutoTinker = true;   // Auto-generation eligible
}
```

### Rarity Weights

| Code | Name | Base Weight |
|------|------|------------|
| C | Common | 100,000 |
| U | Uncommon | 40,000 |
| R | Rare | 10,500 |
| R2 | Very Rare | 1,500 |
| R3 | Legendary | 150 |

**Tier adjustment factor:**
```
weight = base_weight  (if itemTier >= modNativeTier)
weight = base_weight / ((modNativeTier - itemTier) * 5)  (if itemTier < modNativeTier)
```

### Random Mod Application (Item Generation)

When items are generated with random mods (`ModificationFactory.ApplyModifications`):

1. Base mod chance = 3 (from item properties)
2. For each eligible mod table on the item:
   - Collect eligible mods: tier in `[MinTier, MaxTier]`, `CanAutoTinker = true`
   - Calculate weighted selection based on rarity × tier adjustment
   - Roll against cumulative weight
3. Up to **3 mod passes** per item
4. Selected mods applied via `ItemModding.ApplyModification()`
5. Commerce value multiplied by mod's `Value` field

### Mod Application via Tinkering (`TinkeringScreen.PerformUITinkerMod`)

1. Validate mod object and recipe data
2. Check bit cost (with optional Sifrah minigame)
3. Unequip item if currently equipped
4. Consume ingredient if required
5. May trigger `ItemModdingSifrah` minigame
6. Apply modification via `ItemModding.ApplyModification()`
7. Can apply bestowal (special abilities) on success
8. Re-equip if was equipped
9. Cost: 1000 energy units
10. Sound: `"Sounds/Abilities/sfx_ability_tinkerModItem"`

### Maximum Mod Slots

Items have a maximum of **6 modification slots**. Each mod typically uses 1 slot. Some special mods (like `ModGigantic`) use 0 slots.

### Notable Mod Categories

#### Damage Mods (Weapons)
| Mod | Effect | Charge | Notes |
|-----|--------|--------|-------|
| ModFlaming | Heat damage (8-12 base) | 10/turn | Power-load sensitive |
| ModFreezing | Cold damage (8-12 base) | 10/turn | Incompatible with ModRelicFreezing |
| ModElectrified | Electrical damage (Tier to Tier×1.5) | 10/turn | Power-load sensitive |
| ModSerrated | Dismemberment chance | None | LongBlades/Axes only |
| ModSharp | +1 penetration | None | Foundation for ModKeen |
| ModKeen | +1 penetration (stacks on Sharp) | None | Requires ModSharp |
| ModMorphogenetic | Daze/shock save | 200/use | Power-load sensitive |
| ModHeartstopper | Special damage on hit | — | — |
| ModNulling | Reality stabilization | 150/use | Creates astral burden |

#### Performance Mods
| Mod | Effect | Notes |
|-----|--------|-------|
| ModGigantic | +3 damage, 2× capacity/radius/dosage | 0 slot cost, doubles weight |
| ModHypervelocity | Missile speed boost | — |
| ModScoped | Missile accuracy bonus | — |
| ModHighCapacity | Magazine capacity increase | — |
| ModDrumLoaded | Drum magazine | — |
| ModLiquidCooled | 30-60% fire rate boost | Consumes 1 dram pure water |

#### Armor/Defense Mods
| Mod | Effect |
|-----|--------|
| ModPadded | +1 AV |
| ModReinforced | Structural strengthening |
| ModHardened | Defense improvement |
| ModSturdy | Durability enhancement |
| ModFlexiweaved | Flexible armor |
| ModWooly | Insulation/cold resistance |
| ModGlassArmor | Glass-based armor enhancement |

#### Power Mods
| Mod | Power Load Increase | Description |
|-----|-------------------|-------------|
| ModOverloaded | +300 | Increased performance, extra charge draw, heat generation |
| ModMassivelyOverloaded | +800 (additional) | Extreme performance boost, requires ModOverloaded |

#### Special Effect Mods
| Mod | Effect |
|-----|--------|
| ModDisplacer | Teleports target 1-4 tiles (5-6 overloaded), 250 charge |
| ModBlinkEscape | Escape teleportation (armor) |
| ModPhaseHarmonic | Phase manipulation |
| ModExtradimensional | Random bonus from 18+ categories |
| ModDisguise | Complete appearance overhaul (player picks creature species) |
| ModCoProcessor | Grants compute power (head armor only) |

---

## Disassembly

Disassembly breaks items into their constituent bits. It's an `OngoingAction` that takes time.

### Disassembly Requirements

- Item must have `TinkerItem` part with `CanDisassemble = true`
- Player must have `Tinkering_Disassemble` skill
- Item cannot be: a creature, an armed mine, a BaseObject, in stasis, broken
- Cannot disassemble combat items (items with `Combat` part)

### Bit Extraction Algorithm

The core extraction logic from `Disassembly.cs`:

```
BitChance = 50 + DisassembleBonus + GetTinkeringBonusEvent("Disassemble")

IF item has single-bit cost:
    BitChance = 0 (guaranteed)
    IF NumberMade <= 1 OR random(1, NumberMade+1) == 1:
        Extract the bit

ELSE (multi-bit cost):
    last_index = cost.Length - 1
    FOR each bit in cost string:
        IF (this is last_index) OR (BitChance.in100()):
            IF NumberMade <= 1 OR random(1, NumberMade+1) == 1:
                Extract this bit
```

**Key rules:**
- **Last bit is always guaranteed** (the highest-tier bit in the cost)
- All other bits have `BitChance%` chance of extraction (base 50%)
- `NumberMade` acts as a dilution factor — if an item recipe creates multiple copies, each bit has only `1/(NumberMade+1)` chance
- Temporary items yield **no bits**

### Bit Cost of Modded Items

When `GlobalConfig.IncludeModBitsInItemBits` is true, disassembling modded items yields additional bits:

```
For each IModification on the item:
    Get mod's TinkerTier from ModificationFactory
    Add bit at TierBits[Constrain(modTier)]
    Add bit at TierBits[Constrain(slotsUsed - NoCostMods + TechTier)]
```

These mod bits are also subject to `ModifyBitCostEvent.Process(player, cost, "DisassembleMod")`.

### Auto-Disassembly

The `Tinkering_Disassemble` skill enables automatic disassembly on pickup:

**Trigger:** `TookEvent` (when player picks up an item)

**Conditions for auto-disassembly:**
1. `Options.AutoDisassembleScrap` is enabled
2. Item passes `WantToDisassemble()` check:
   - Player is controlling character
   - Item is valid and has TinkerItem
   - Item is marked as scrap (via toggle or `"Scrap"` tag)
   - Item is not owned by someone else
   - Item is not important
   - Item is not in stasis
   - Item has no liquid contents

### Scrap Toggle System

Players can manually mark items as "scrap" for auto-disassembly:

- **Toggle key**: `'S'` in inventory
- State stored in game state: `SetBooleanGameState(toggleKey)` where toggleKey includes blueprint + mod profile + mine/bomb state
- Persists across sessions
- UI shows "Toggle Scrap" inventory action

---

## Reverse Engineering

Reverse engineering is the process of learning recipes by disassembling items. Requires `Tinkering_ReverseEngineer` skill.

### Reverse Engineering Process

1. During disassembly, if item has a learnable recipe:
   - Base chance: **15%** + `GetTinkeringBonusEvent("ReverseEngineer")` bonus
2. If `Options.SifrahReverseEngineer` enabled:
   - Triggers `ReverseEngineeringSifrah` minigame
   - Rating = `Intelligence stat + bonus`
   - Complexity/Difficulty from item's `Examiner` part
3. If no Sifrah, simple percentage roll against `chance`

### What Can Be Learned

- **Build recipes**: Learn the item's construction blueprint
- **Mod recipes**: If item has IModification parts and player has ReverseEngineer skill, can learn those mod recipes
- Partial learning possible — fewer mods learned on partial success

### Complexity/Difficulty

Items have an `Examiner` part tracking:
- `Complexity` — How complicated the item is (affects Sifrah token count)
- `Difficulty` — How hard to examine/reverse engineer
- `EpistemicStatus` — Knowledge level (-1=uninitialized, 0=unknown, 1=partial, 2=known)

Modifications increase complexity and difficulty of their host item via `IncreaseComplexity()` and `IncreaseDifficulty()`.

---

## Power Cells & Energy System

Caves of Qud has a sophisticated energy system that powers technological items. The system is built on an abstract `IEnergyCell` interface with multiple concrete implementations.

### Architecture

```
IEnergyCell (abstract) : IRechargeable
├── EnergyCell              — Standard rechargeable cell
├── LiquidFueledEnergyCell  — Liquid-powered (non-rechargeable)
└── (other implementations)

EnergyCellSocket : IPoweredPart   — Holds a cell in a device
EnergyCellRack : IPoweredPart     — Holds multiple cells

IPoweredPart : IActivePart        — Base for all powered devices
```

### IEnergyCell Interface

```csharp
public abstract class IEnergyCell : IRechargeable
{
    public string SlotType;        // "EnergyCell" or "PowerCore"
    public GameObject SlottedIn;   // Device this cell is installed in

    abstract bool HasAnyCharge();
    abstract bool HasCharge(int Amount);
    abstract int GetCharge();
    abstract int GetChargePercentage();
    abstract string ChargeStatus();
    abstract void TinkerInitialize();
    abstract void UseCharge(int Amount);
    abstract void SetChargePercentage(int Percentage);
    abstract void RandomizeCharge();
    abstract void MaximizeCharge();
}
```

### Standard Energy Cell (`EnergyCell`)

The most common cell type — discrete charge with recharging support.

```csharp
public class EnergyCell : IEnergyCell
{
    public int Charge = 100;          // Current charge
    public int MaxCharge = 100;       // Maximum capacity
    public int ChargeRate = 10;       // Recharge acceptance rate per event
    public int RechargeValue = 3000;  // Recharge difficulty value
    public char RechargeBit = 'R';    // Bit type for recharging display
    public string StartCharge = "";   // Initial charge expression (e.g., "1d100")
    public string ChargeDisplayStyle = "electrical";
    public bool ConsiderLive = false; // Reports electrical conductivity (95)
    public bool IsRechargeable = true;
}
```

**Charge events handled:**
- `QueryChargeEvent` — Reports available charge (non-destructive)
- `TestChargeEvent` — Tests if sufficient charge exists
- `UseChargeEvent` — 2-pass consumption system
- `RechargeAvailableEvent` — Accepts charge, limited by `ChargeRate`
- `CellDepletedEvent` — Fired when charge reaches 0

### Charge Levels & Display

Charge is displayed as a descriptive status with color coding:

| Level | Percentage | Electrical | Clockwork | Bio | Color |
|-------|-----------|------------|-----------|-----|-------|
| 0 | 0% | Drained | Run Down | Exhausted | K (dark) |
| 1 | 1-10% | Very Low | Very Run Down | Flagging | r (dim red) |
| 2 | 11-25% | Low | Fairly Run Down | Enervated | R (red) |
| 3 | 26-50% | Used | Somewhat Run Down | Fatigued | W (white) |
| 4 | 51-75% | Fresh | Well-Wound | Lively | g (green) |
| 5 | 76-100% | Full | Fully Wound | Vigorous | G (bright green) |

Additional display styles: `kinetic`, `tension`, `glow`, `dark`, `roughpercentage`, `percentage`, `amount`.

### Liquid-Fueled Energy Cell (`LiquidFueledEnergyCell`)

A non-rechargeable cell that converts liquid into charge.

```csharp
public class LiquidFueledEnergyCell : IEnergyCell
{
    public string Liquid = "water";       // Required liquid type
    public int ChargePerDram = 10000;     // Charge per dram consumed
    public int ChargeCounter = 0;         // Fractional charge accumulator
    public bool ConsiderLive = false;
}
```

**Consumption mechanics:**
- `GetCharge()` = `Volume × ChargePerDram - ChargeCounter`
- `UseCharge(Amount)` accumulates in `ChargeCounter`
- When `ChargeCounter >= ChargePerDram`, consumes 1 dram
- **Cannot be recharged** — `CanBeRecharged() = false`

### Energy Cell Socket (`EnergyCellSocket`)

Devices hold cells via sockets:

```csharp
public class EnergyCellSocket : IPoweredPart
{
    public string SlotType = "EnergyCell";
    public GameObject Cell;                          // Currently installed cell
    public int ChanceSlotted;                        // % chance to have cell on creation
    public string SlottedType;                       // Blueprint for auto-generated cell
    public int ChanceFullCell;                       // % chance to spawn fully charged
    public string CellStartChargePercentage;         // Override initial charge %
    public int ChanceDestroyCellOnForcedUnequip;     // % to lose cell if forced unequip
    public bool VisibleInDisplayName = true;
    public bool VisibleInDescription = true;
}
```

**Cell replacement UI:**
- Shows available cells from inventory sorted by charge (descending)
- Option to remove current cell
- Option to disassemble current cell (if has TinkerItem)

### Slot Types

| Slot Type | Display Name | Short Name |
|-----------|-------------|------------|
| `EnergyCell` | energy cell | cell |
| `PowerCore` | power core | core |

### Power Generators

The game includes several power generation systems:

#### Solar Array
```csharp
public class SolarArray : IPoweredPart
{
    public int ChargeRate = 10;
    // Only produces when: not blackout, outside, daytime
    // Production: ChargeRate - ChargeUse per turn
}
```

#### Liquid-Fueled Power Plant
```csharp
public class LiquidFueledPowerPlant : IPoweredPart
{
    public string Liquid;              // Primary fuel type
    public string Liquids;             // Multi-fuel map "type1:rate1,type2:rate2"
    public int ChargePerDram = 10000;
    public int ChargeRate;
    // Supports multiple liquid fuel types with different conversion rates
}
```

#### Zero-Point Energy Collector
```csharp
public class ZeroPointEnergyCollector : IPoweredPart
{
    public int ChargeRate = 10;
    public string World = "JoppaWorld";
    // IsPowerLoadSensitive = true
    // Rate scales with power load: EffectiveRate = ChargeRate * PowerLoad% / 100
    // With standard overload (+400 load): 5x charge production
}
```

#### Broadcast Power System
```csharp
// Transmitter: broadcasts power wirelessly
public class BroadcastPowerTransmitter : IPoweredPart
{
    public int TransmitRate;  // 0 = unlimited
}

// Receiver: receives from transmitters or satellites
public class BroadcastPowerReceiver : IPoweredPart
{
    public int ChargeRate = 10;
    public bool CanReceiveSatellitePower;
    public int MaxSatellitePowerDepth = 12;
    // Satellite occlusion: 1/1000 chance to occlude per turn, 5/1000 to de-occlude
}
```

#### Free Power
```csharp
public class FreePower : IPoweredPart
{
    public int ChargeFulfilled = 10;
    // Infinite charge, never depletes
    // Used for special items, testing, narrative purposes
}
```

### Charge Consumption Patterns

1. **Passive (turn-based)** — Items with `WantTurnTick()` consume each turn (always-active effects)
2. **Active (on-use)** — Consumed when ability activated, checked via `TestChargeEvent`
3. **System (generators)** — Power plants produce, receivers accumulate, transmitters distribute

### 2-Pass Charge System

Charge consumption uses a 2-pass event architecture:
- **Pass 1:** Early checks — verify charge availability
- **Pass 2:** Actual consumption — deduct charge, fire `ChargeUsedEvent`

Parameters:
```csharp
int Amount;           // Charge needed
int Multiple;         // Multiplier for turns/repeats
long GridMask;        // Grid filter mask
bool Forced;          // Force consumption
bool LiveOnly;        // Only conducting cells
bool IncludeTransient, IncludeBiological;  // Type filters
```

---

## Recharging

The Recharge ability (granted by Tinker I) lets players spend bits to refill energy cells.

### Recharge Algorithm (`Tinkering_Tinker1.Recharge`)

```
1. Check player can move extremities ("Recharge")
2. For each IRechargeable part on the target:
   a. Check CanBeRecharged()
   b. Get rechargeAmount = MaxCharge - Charge
   c. Get rechargeBit (the bit type required, e.g., 'r')
   d. Get rechargeValue (energy per bit)
   e. Calculate bits needed: bitsForFull = rechargeAmount / rechargeValue (minimum 1)
   f. Check player has at least 1 of the required bit type
   g. Ask player how many bits to spend (0 to min(owned, needed))
   h. If partial: chargeAdded = bitsSpent × rechargeValue
      If full:    chargeAdded = rechargeAmount
   i. Consume bits via BitLocker.UseBits()
   j. Add charge to cell
3. Cost: 1000 energy units
4. Sound: "Sounds/Abilities/sfx_ability_energyCell_recharge"
```

### Recharge UI

The player is prompted:
> "It would take **N** [bit type] bits to fully recharge [item]. You have **M**. How many do you want to use?"

With a number input defaulting to `min(owned, needed)`.

### IRechargeable Interface

```csharp
public interface IRechargeable
{
    bool CanBeRecharged();      // Is recharging possible?
    int GetRechargeAmount();    // Charge needed for full
    int GetRechargeValue();     // Energy units per bit (difficulty)
    char GetRechargeBit();      // Required bit type character
    void AddCharge(int Amount); // Add charge to cell
}
```

---

## Overloading

Overloading increases item performance by adding power load, at the cost of higher charge consumption, heat generation, and breakdown risk.

### Power Load System

Power load is a percentage multiplier on charge consumption and effects:

| State | Power Load | Effective Multiplier |
|-------|-----------|---------------------|
| Normal | +0 | 1× |
| Overloaded | +300 | ~4× |
| Massively Overloaded | +300 + 800 = +1100 | ~12× |

### ModOverloaded

```csharp
public class ModOverloaded : IModification
{
    // WorksOnSelf = true, IsEMPSensitive = true
    // NameForStatus = "PowerRegulator"

    // GetPowerLoadLevelEvent: +300 to power load
    // Display: "{{overloaded|overloaded}}" adjective
    // Difficulty +2, Complexity +1
}
```

> "Overloaded: This item has increased performance but consumes extra charge, generates heat when used, and has a chance to break relative to its charge draw."

### ModMassivelyOverloaded

```csharp
public class ModMassivelyOverloaded : IModification
{
    // Requires ModOverloaded already present
    // NameForStatus = "AuxiliaryPowerRegulator"

    // GetPowerLoadLevelEvent: +800 additional power load
    // Display: "{{overloaded|massively overloaded}}" adjective
    // Difficulty +1, Complexity +1
}
```

> "Massively overloaded: This item has significantly increased performance but consumes a great deal of extra charge, generates considerable heat when used, and has a chance to break relative to its charge draw."

### Power Load Sensitivity

Items with `IsPowerLoadSensitive = true` scale their effects with power load. For example:
- **ZeroPointEnergyCollector**: `EffectiveRate = ChargeRate × PowerLoad% / 100`
- **Damage mods** (Flaming, Freezing, Electrified): Tier bonus from `PowerLoadBonus(PowerLoad)`
- Standard overload (+400 load) typically grants +2 tier bonus to damage calculations

### Overload Events

- `IsOverloadableEvent` — Queries if an object can accept overload (cascade level 17)
- `GetOverloadChargeEvent` — Queries extra charge drawn by overloaded item
- `GetPowerLoadLevelEvent` — Collects total power load from all modifications

---

## Repair System

The Repair skill (`Tinkering_Repair`) allows fixing damaged and rusted items.

### Repair Cost Calculation

```csharp
GetRepairCost(GameObject obj):
    If item has RepairCost/RustedRepairCost property on TinkerItem:
        If rusted: uses ALL bits from item's bit cost
        If not rusted: probabilistic selection (50% per bit, seeded by world seed)
        Fallback: "BC" (basic scrap bits)
```

### Repair Tier Requirement

```
RepairTier = highest tier bit in repair cost
Requires: Tinker skill >= GetRequiredSkill(RepairTier)
Also: max repairable tier = Difficulty / 2 (clamped 0-8) from Repair part
```

### Repair Outcomes

| Result | Effect |
|--------|--------|
| **Success** | Item repaired |
| **Exceptional Success** | Item repaired + receive `3d6` random bits at item complexity tier |
| **Partial Success** | Progress made, not fully repaired |
| **Failure** | "Can't figure it out" |
| **Critical Failure** | May apply `Broken` effect to item |

---

## Sifrah Minigames

Sifrah is a match-grid puzzle minigame integrated into tinkering operations. Two variants exist for crafting.

### Base Architecture

```csharp
public abstract class SifrahGame
{
    public string Description;          // Task description
    public List<SifrahSlot> Slots;      // Grid squares to fill (4-6)
    public List<SifrahToken> Tokens;    // Available actions/items
    public int MaxTurns;                // Time limit
    public int Turn;                    // Current turn
    public bool Solved;                 // All slots filled correctly
    public int PercentSolved;           // Progress percentage
}
```

### Item Modding Sifrah (`ItemModdingSifrah`)

Triggered when applying mods via tinkering (if `Options.SifrahItemModding` enabled).

**Parameters:**
- Complexity = item complexity level
- Difficulty = item difficulty
- Rating = Intelligence + tinkering bonuses

**Slot count:** 4-6 (based on complexity)

**Slot names:**
1. "upgrading gearworks"
2. "retooling spark vines"
3. "optimizing humour flows"
4. "enhancing glow modules"
5. "befriending spirits"
6. "invoking the beyond"

**Token types (selected based on rating and complexity):**
- Physical Manipulation
- Tenfold Path skills
- Telekinesis (if has mutation)
- Advanced Toolkit
- Compute Power
- Charge
- Bits (each tier up to Complexity+Difficulty)
- Liquids (oil, gel, acid, lava, brainbrine, neutronflux, sunslag)
- Copper Wire

**Outcome calculation:**
```
SuccessFactor = (Rating + PercentSolved) × 0.01
CriticalChance = SuccessFactor × 0.02 × (1 + bonus from turns)

Results:
  Success:         SuccessFactor × 100%
  Failure:         (1 - SuccessFactor) × 50%
  PartialSuccess:  (1 - SuccessFactor) × 50%
  CriticalSuccess: SuccessFactor × CriticalChance
  CriticalFailure: (1 - SuccessFactor) × 10%
```

**Result effects:**
| Outcome | Effect |
|---------|--------|
| Critical Failure | Item becomes Broken |
| Failure | No mod applied |
| Partial Success | Mod applied, Performance = 0 |
| Success | Mod applied, Performance = 1-2 (25% chance of 2) |
| Critical Success | Mod applied, Performance = 3-5, grants Insight |

**XP:**
- Success: `(Tokens² - Slots) × (Complexity + Difficulty) × 2`
- Critical Success: 10× the above

### Reverse Engineering Sifrah (`ReverseEngineeringSifrah`)

Triggered during disassembly when reverse engineering (if `Options.SifrahReverseEngineer` enabled).

**Slot names:**
1. "sketching gearworks"
2. "diagramming spark vines"
3. "measuring humour flows"
4. "mapping glow modules"
5. "summoning spirits"
6. "communing with the beyond"

**Additional tokens available:**
- Visual Inspection (requires no Myopia)
- Scanning (if has scan ability)
- Psychometry (if has psychometry)
- Ink, wax (as liquids)

**Results:**
| Outcome | Effect |
|---------|--------|
| Critical Failure | Abort, learn nothing |
| Failure | Learn nothing |
| Partial Success | Learn 0 mods |
| Success | Learn build blueprint and/or some mods |
| Critical Success | Learn everything, don't destroy item |

---

## Tinkering UI

The tinkering interface has both a modern and legacy implementation.

### Modern UI (`TinkeringStatusScreen`)

**Layout:**
- Two tabs: **Build** (mode 0) and **Mod** (mode 1)
- Left panel: recipe list with category grouping and collapse/expand
- Right panel: bit locker display showing current vs. required bits
- Search bar with fuzzy matching (via FuzzySharp)
- Filter by category or "\*All"

**Build mode:**
- Lists known build recipes grouped by `UICategory` (weapon, armor, tool, utility, consumable, etc.)
- Shows bit cost next to each recipe
- Selected recipe shows full description

**Mod mode:**
- Lists items in inventory/equipped that have applicable mods
- Expandable per-item to show available modifications
- Shows which mods apply via mod tags

**Bit display:**
- Each bit type shown with: current count, required count for selected recipe
- Visual indicators: `✓` (have enough), `X` (need more), `-` (not required)

### Legacy UI (`TinkeringScreen`)

```
[[ Tinkering ]]            [hostiles nearby] [ Bit Locker ]
  > Build    Mod           [bit type] xxx [count/requirement]
                           [bit type] xxx [count/requirement]
  [Recipe 1]        <bits>
  [Recipe 2]        <bits>  [Recipe details
  [Recipe 3]  >     <bits>   in bottom area]
  [Recipe 4]        <bits>

< 7 Journal | Skills 9 >
```

**Controls:**
- Numpad 8/2: Up/Down
- Numpad 4/6: Switch Build/Mod tabs
- Numpad +/-: Scroll description
- Page Up/Down: Page through items
- Space/Enter: Select recipe

---

## Maker's Mark System

Crafted items can bear the crafter's signature.

### How It Works

`TinkeringHelpers.CheckMakersMark()` is called after building or modding items:

1. Fire `TriggersMakersMarkCreationEvent.Check()`
2. **Player:** Shows maker's mark selection UI (if enabled in options)
3. **NPC:** Generates random mark name and color
4. Adds `MakersMark` part to item via `RequirePart<MakersMark>()`
5. Records crafter name, color, and usage via `MakersMark.RecordUsage()`

### Eligibility

Items are eligible for a maker's mark if:
- They have a `Description` part
- They do NOT have the `AlwaysStack` tag
- They are NOT standard scrap

---

## Key Events & Extension Points

The tinkering system is highly extensible through events:

### GetTinkeringBonusEvent

The primary hook for modifying tinkering skill checks.

```csharp
public class GetTinkeringBonusEvent
{
    public string Type;              // "Inspect", "Build", "Disassemble", "ReverseEngineer", etc.
    public int BaseRating;           // Base difficulty rating
    public int Bonus;                // Output: bonus value
    public int SecondaryBonus;       // Additional bonus
    public int ToolboxBonus;         // Toolbox-specific bonus
    public bool PsychometryApplied;  // Whether psychometry was used
    public bool Interruptable;       // Can be interrupted
    public bool ForSifrah;           // For Sifrah minigame context
}
```

Listeners can intercept this to modify tinkering outcomes. Example: `GadgetInspector` adds +5 to "Inspect" type checks.

### ModifyBitCostEvent

Allows modification of bit costs before consumption.

```csharp
public class ModifyBitCostEvent
{
    public GameObject Actor;
    public BitCost Bits;        // In-out: the cost being modified
    public string Context;      // "Disassemble", "DisassembleMod", "Build", "Mod"
}
```

### Other Key Events

| Event | Purpose |
|-------|---------|
| `CellDepletedEvent` | Fired when energy cell reaches 0 charge |
| `ChargeUsedEvent` | Fired after charge consumed |
| `ModificationAppliedEvent` | Fired after mod successfully applied |
| `CanBeModdedEvent` | Gate check for mod applicability |
| `IsOverloadableEvent` | Query if object accepts overloading |
| `GetPowerLoadLevelEvent` | Collects total power load |
| `IsRepairableEvent` | Gate check for repair eligibility |

---

## Summary: Complete Tinkering Flow

```
1. ACQUIRE SKILLS
   Tinker I → Tinker II → Tinker III
   + Support skills: Gadget Inspector, Disassemble, Repair, Reverse Engineer

2. LEARN RECIPES
   Find data disks in the world → Use "Learn" action
   OR: Level up Tinker skill → Choose from 3 random recipes
   OR: Reverse engineer items during disassembly

3. GATHER BITS
   Disassemble items → Extract bits (guaranteed last bit + 50% per other bit)
   12 bit types across 9 tiers (scrap → metacrystal)

4. BUILD ITEMS
   Open Tinkering screen → Select known recipe → Spend bits (+ingredients)
   → New item created with TinkeredItem property and optional Maker's Mark

5. MOD ITEMS
   Open Tinkering screen → Mod tab → Select item → Select applicable mod
   → Spend bits (+ingredients) → Optional Sifrah minigame
   → Mod applied (up to 6 slots per item)

6. POWER ITEMS
   Install energy cells in sockets → Cells provide charge for powered parts
   Cell types: Standard (rechargeable), Liquid-fueled, Free
   Generators: Solar, Liquid-fueled, Zero-point, Broadcast, Free

7. RECHARGE
   Use Recharge ability → Spend bits to refill energy cells
   Cost: rechargeAmount / rechargeValue bits of specified type

8. OVERLOAD
   Apply ModOverloaded → +300 power load → ~4x performance/consumption
   Stack ModMassivelyOverloaded → +800 more → extreme performance

9. REPAIR
   Use Repair skill → Spend repair-cost bits → Roll for outcome
   Success/Exceptional/Partial/Failure/Critical Failure

10. DISASSEMBLE
    Break down items → Receive bits → Optionally reverse engineer recipes
    Auto-disassembly for items marked as scrap
```
