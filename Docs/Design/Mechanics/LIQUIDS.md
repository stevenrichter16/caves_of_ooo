# Caves of Qud: Liquid System Deep Dive

> Comprehensive analysis of the liquid system from decompiled Caves of Qud source code.
> Source files referenced from `/Users/steven/qud-decompiled-project/`

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [BaseLiquid Class](#2-baseliquid-class)
3. [LiquidVolume Part](#3-liquidvolume-part)
4. [ComponentLiquids & Proportions](#4-componentliquids--proportions)
5. [Container vs Pool (MaxVolume)](#5-container-vs-pool-maxvolume)
6. [Sealed/Unsealed Mechanics](#6-sealedunsealed-mechanics)
7. [Liquid Mixing (MixWith)](#7-liquid-mixing-mixwith)
8. [Proportion Normalization](#8-proportion-normalization)
9. [Primary/Secondary Tracking](#9-primarysecondary-tracking)
10. [Physical Property Mixing](#10-physical-property-mixing)
11. [Drinking Mechanics](#11-drinking-mechanics)
12. [Pouring Mechanics](#12-pouring-mechanics)
13. [Contact & Smearing](#13-contact--smearing)
14. [Liquid Pools & Zone Generation](#14-liquid-pools--zone-generation)
15. [Liquid Spreading & Flowing](#15-liquid-spreading--flowing)
16. [Evaporation](#16-evaporation)
17. [Temperature Effects (Freeze/Vaporize)](#17-temperature-effects-freezevaporize)
18. [Slippery & Sticky Mechanics](#18-slippery--sticky-mechanics)
19. [Event System (Dram Events)](#19-event-system-dram-events)
20. [Liquid Producer](#20-liquid-producer)
21. [Navigation Weights](#21-navigation-weights)
22. [Value & Economy](#22-value--economy)
23. [Cooking Ingredients](#23-cooking-ingredients)
24. [Rendering & Visuals](#24-rendering--visuals)
25. [All 27 Liquid Types](#25-all-27-liquid-types)
26. [Special Reactions](#26-special-reactions)
27. [Key Source Files](#27-key-source-files)

---

## 1. Architecture Overview

Qud's liquid system is built on three pillars:

| Layer | Class | Location | Purpose |
|-------|-------|----------|---------|
| **Type definitions** | `BaseLiquid` + 27 subclasses | `XRL.Liquids/` | Properties & behavior per liquid type |
| **Container/volume** | `LiquidVolume` (Part) | `XRL.World.Parts/LiquidVolume.cs` | 6,752-line Part that tracks volume, composition, and handles all actions |
| **Registry** | `LiquidVolume.Liquids` | Static `StringMap<BaseLiquid>` | Lazy-initialized via `[IsLiquid]` attribute discovery |

**Key insight**: Liquids are NOT entities. A liquid type is a singleton `BaseLiquid` instance (registered at startup). The `LiquidVolume` Part attached to a container/puddle entity holds the actual volume and composition.

### Registration

```csharp
// LiquidVolume.cs:1112
public static void Init()
{
    _Liquids = new StringMap<BaseLiquid>();
    foreach (Type item in ModManager.GetTypesWithAttribute(typeof(IsLiquid)))
    {
        BaseLiquid baseLiquid = Activator.CreateInstance(item) as BaseLiquid;
        _Liquids[baseLiquid.ID] = baseLiquid;
    }
}
```

Each liquid type is marked with `[IsLiquid]` attribute and discovered via reflection.

---

## 2. BaseLiquid Class

**File**: `XRL.Liquids/BaseLiquid.cs` (711 lines)

### Temperature Properties

| Property | Default | Description |
|----------|---------|-------------|
| `Temperature` | 25 | Ambient temperature |
| `FlameTemperature` | 99999 | Ignition point |
| `VaporTemperature` | 100 | Boiling point |
| `FreezeTemperature` | 0 | Freezing point |
| `BrittleTemperature` | -100 | Shatters when frozen |

### Physical Properties

| Property | Default | Description |
|----------|---------|-------------|
| `Weight` | 0.25 | Density per dram |
| `Fluidity` | 50 | Flow rate (0=viscous, 100=watery). `Viscosity = 100 - Fluidity` |
| `Combustibility` | 0 | Flammability (-50 water to 90 oil) |
| `Adsorbence` | 100 | Ability to absorb into materials |
| `Evaporativity` | 0 | Rate of vapor loss |
| `ThermalConductivity` | 50 | Heat transfer rate |

### Electrical Properties

| Property | Default | Description |
|----------|---------|-------------|
| `PureElectricalConductivity` | 80 | Conductivity when pure |
| `MixedElectricalConductivity` | 80 | Conductivity when mixed |

**Design note**: Pure water has conductivity 0, but mixed water has 100 (saltwater conducts).

### Interaction Properties

| Property | Default | Description |
|----------|---------|-------------|
| `Staining` | 0 | Staining intensity (0-3) |
| `Cleansing` | 0 | Cleaning power (5=water, 30=acid, 20=lava) |
| `EnableCleaning` | false | Can clean objects? |
| `StainOnlyWhenPure` | false | Only stains as pure liquid |
| `InterruptAutowalk` | false | Stops auto-movement |
| `ConsiderDangerousToContact` | false | Dangerous to touch |
| `ConsiderDangerousToDrink` | false | Dangerous to drink |
| `Glows` | false | Emits light |

### Contact Effect Properties

| Property | Default | Description |
|----------|---------|-------------|
| `SlipperyWhenWet` | false | Causes slipping when liquid |
| `SlipperyWhenFrozen` | false | Causes slipping when frozen (ice) |
| `StickyWhenWet` | false | Causes sticking when liquid |
| `StickyWhenFrozen` | false | Causes sticking when frozen |
| `SlipperySaveTargetBase` | 5 | Base DC for slip save |
| `SlipperySaveTargetScale` | 0.3 | Scales with liquid amount |
| `SlipperySaveStat` | "Agility" | Stat to resist |
| `StickySaveTargetBase` | 1 | Base DC for stick save |
| `StickySaveTargetScale` | 0.1 | Scales with liquid amount |
| `StickySaveStat` | "Strength,Agility" | Stats to resist |
| `StickyDuration` | 12 | Turns stuck |

### Freeze Object System

| Property | Description |
|----------|-------------|
| `FreezeObject1/2/3` | Object created when frozen at volume threshold |
| `FreezeObjectThreshold1/2/3` | Volume needed for each freeze object |
| `FreezeObjectVerb1/2/3` | Verb description ("solidify", etc.) |
| `VaporObject` | Gas object created on vaporization ("SteamGas", "AcidGas") |
| `ConversionProduct` | Alternate liquid form (slime -> gel) |

### Virtual Methods

```csharp
// Identity & Display
GetValuePerDram() -> float
GetPureLiquidValueMultiplier() -> float (default 1.0)
GetName(LiquidVolume) -> string
GetColor() / GetColors() -> color codes
GetAdjective(LiquidVolume) -> string
GetSmearedAdjective/Name() -> string
GetStainedName() -> string

// Behavior
Drank(LiquidVolume, int Volume, GameObject Target, StringBuilder, ref bool ExitInterface) -> bool
SmearOn(LiquidVolume, GameObject) -> void
SmearOnTick(LiquidVolume, GameObject) -> void
Froze(LiquidVolume, GameObject By) -> bool
Thawed(LiquidVolume, GameObject By) -> bool
Vaporized(LiquidVolume, GameObject By) -> bool
FillingContainer(GameObject, LiquidVolume) -> void
MixingWith(LiquidVolume, LiquidVolume, ...) -> bool  // Return false to veto mixing
MixedWith(LiquidVolume, LiquidVolume, ...) -> void    // Post-mix effects
ObjectEnteredCell(LiquidVolume, IObjectCellInteractionEvent) -> void

// Rendering
RenderPrimary/Secondary(LiquidVolume, RenderEvent) -> void
BaseRenderPrimary/Secondary(LiquidVolume) -> void
RenderSmearPrimary/Secondary(LiquidVolume, RenderEvent, GameObject) -> void
BeforeRender(LiquidVolume) -> void

// Navigation & Safety
GetNavigationWeight(LiquidVolume, GameObject, bool Smart, ...) -> int
SafeContainer(GameObject) -> bool
```

---

## 3. LiquidVolume Part

**File**: `XRL.World.Parts/LiquidVolume.cs` (6,752 lines)

### Core Fields

```csharp
public int MaxVolume = -1;        // -1 = unlimited (open volume/pool)
public int Volume;                 // Current liquid volume in drams
public string StartVolume = "";    // Blueprint initialization

// Composition
public Dictionary<string, int> ComponentLiquids = new Dictionary<string, int>();
public string Primary;             // Most abundant liquid ID
public string Secondary;           // Second most abundant

// Container behavior
public int Flags = 32;             // Bitfield (see below)
public bool Sealed;                // Cannot pour/drink
```

### Flag Bits

| Flag | Value | Description |
|------|-------|-------------|
| `LIQUID_FLOWING` | 1 | Liquid spreads |
| `LIQUID_COLLECTOR` | 2 | Container collects liquids |
| `LIQUID_SEALED` | 4 | Sealed container |
| `LIQUID_MANUAL_SEAL` | 8 | Can be manually sealed/unsealed |
| `LIQUID_VISIBLE_WHEN_SEALED` | 16 | Shows contents when sealed |
| `LIQUID_SHOW_SEAL` | 32 | Displays seal status (default) |
| `LIQUID_HAS_DRAIN` | 64 | Has drain |

---

## 4. ComponentLiquids & Proportions

**The critical data structure**: `Dictionary<string, int> ComponentLiquids`

- Keys = liquid type IDs (e.g., "water", "acid", "blood")
- Values = **proportion out of 1000** (permille, NOT percentage)
- 1000 = 100% pure, 500 = 50% of mixture

### Proportion System

```csharp
// Example: 60% water, 30% oil, 10% blood
ComponentLiquids = {
    { "water", 600 },   // 60%
    { "oil", 300 },     // 30%
    { "blood", 100 }    // 10%
};
// Sum always == 1000

Volume = 1000; // 1000 drams total
```

### Amount Calculations

```csharp
// Real drams of a specific liquid
public int Amount(string Liquid)
    => Volume * Proportion(Liquid) / 1000;
// Example: 1000 drams at 600/1000 water = 600 drams of water

// Rounded up (minimum 1 drop)
public int UpperAmount(string Liquid)
    => (int)Math.Ceiling((double)(Volume * Proportion(Liquid)) / 1000.0);

// Raw scale (no division by 1000)
public int MilliAmount(string Liquid)
    => Volume * Proportion(Liquid);
```

### Purity Checks

```csharp
public bool IsPureLiquid() => ComponentLiquids.Count == 1 && Volume > 0;
public bool IsPureLiquid(string LiquidType) => /* only that type exists */;
public bool IsMixed() => ComponentLiquids.Count > 1;
public bool IsEmpty() => ComponentLiquids.Count == 0 || Volume == 0;
```

---

## 5. Container vs Pool (MaxVolume)

| Type | MaxVolume | Example | Behavior |
|------|-----------|---------|----------|
| **Container** | > 0 | Waterskin (1000), Vial (100) | Fixed capacity, can be sealed |
| **Open volume** | -1 | Puddle, lake, pool | Unlimited capacity, always open |

```csharp
public bool IsOpenVolume() => MaxVolume == -1;
```

### Depth Thresholds (Open Volumes Only)

| Threshold | Volume | Method | Effect |
|-----------|--------|--------|--------|
| Puddle | 1-199 | — | Visual only |
| Wading | >= 200 | `IsWadingDepth()` | Movement effects, slipping/sticking |
| Swimming | >= 2000 | `IsSwimmingDepth()` | Full submersion, swimming effect |

---

## 6. Sealed/Unsealed Mechanics

```csharp
public bool EffectivelySealed()
{
    if (Sealed) return !IsBroken();
    return false;
}
```

**When sealed**:
- `GetFreeDramsEvent` returns 0 (can't drink)
- `GetStorableDramsEvent` returns 0 (can't refill)
- `UseDramsEvent` rejected (can't use liquid)
- `GiveDramsEvent` rejected (can't pour into)
- `Pour()` returns false
- `Drink` action blocked

---

## 7. Liquid Mixing (MixWith)

**File**: `LiquidVolume.cs:1362-1466`

### Algorithm

```
1. Fire LiquidMixing event on ParentObject (can veto)
2. Call MixingWith() on each BaseLiquid in both volumes (can veto)
3. Optional: Split incoming amount if partial pour
4. Recalculate proportions:
   For each liquid type in combined set:
     new_prop = (incoming_vol * incoming_prop + current_vol * current_prop)
                / (incoming_vol + current_vol)
   Remove components that drop to 0
5. Volume += incoming amount (cap to MaxVolume)
6. Call FillingContainer() on new liquid types
7. Call MixedWith() on each BaseLiquid (post-mix effects)
8. Fire LiquidMixedEvent
9. Recalculate Primary/Secondary, update rendering
```

### Example: Mix 500 drams of lava into 1000 drams of water

```
Before: water=1000/1000, vol=1000 + lava=1000/1000, vol=500
After proportions:
  water = (1000*1000 + 500*0) / 1500 = 667
  lava  = (1000*0 + 500*1000) / 1500 = 333
After volume: 1500
Result: water-667,lava-333 at 1500 drams
```

---

## 8. Proportion Normalization

**File**: `LiquidVolume.cs:1468-1560`

Ensures ComponentLiquids values always sum to exactly 1000.

```
1. Sum all proportions
2. If sum == 1000, done (fast path)
3. Calculate deficit = 1000 - sum
4. Distribute deficit proportionally across all components
5. Handle ±1 rounding: give remainder to highest-proportion liquid
6. For larger discrepancies: round-robin distribute to components by descending proportion
```

---

## 9. Primary/Secondary Tracking

**File**: `LiquidVolume.cs:1060-1110`

```csharp
public bool RecalculatePrimary()
{
    // 0 liquids: Primary=null, Secondary=null
    // 1 liquid: Primary=that liquid, Secondary=null
    // 2+ liquids: Sort by proportion descending
    //   Primary = highest proportion
    //   Secondary = second highest
    //   EXCEPTION: Blood always becomes Secondary if present (unless it IS Primary)
    //   EXCEPTION: If Primary="warmstatic" and Secondary="water", skip water
}
```

**Returns true** if Primary/Secondary changed, signaling render update needed.

---

## 10. Physical Property Mixing

**File**: `LiquidVolume.cs:5138-5196`

For mixed liquids, physical properties are **weighted averages** based on proportions:

```csharp
// For each property:
MixedValue = SUM(ComponentValue * ComponentProportion) / 1000
```

Properties mixed this way:
- FlameTemperature
- VaporTemperature
- FreezeTemperature
- BrittleTemperature
- ElectricalConductivity (uses `MixedElectricalConductivity` per component)
- ThermalConductivity (`GetLiquidThermalConductivity()`)
- Combustibility (`GetLiquidCombustibility()`)

**Example**: 500 parts water (conductivity 0) + 500 parts salt (conductivity 100) = 50 conductivity.

---

## 11. Drinking Mechanics

**File**: `LiquidVolume.cs:3118-3254`

### Flow

```
1. Check not sealed, not in stasis
2. Fire CanDrinkEvent (parts can allow/deny)
3. If ConsiderDangerousToDrink: show confirmation popup
4. Fire "BeforeDrink" event (can prevent)
5. Fire "DrinkingFrom" event on actor
6. For each liquid component: call BaseLiquid.Drank()
7. Consume exactly 1 dram via UseDram()
8. Fire "Drank" event on actor {Object, WasFreshWater}
9. Update water/hunger UI
```

**Always consumes 1 dram per drink action.**

### Per-Liquid Drinking Effects

| Liquid | Effect |
|--------|--------|
| Water | Adds 10x water amount to Stomach via "AddWater" event. "Ahh, refreshing!" |
| Acid | `(proportion/100 + 1)d10` damage. "IT BURNS!" |
| Lava | TemperatureChange(1000). `(proportion/100 + 1)d100` damage. Achievement. "IT BURNS!" |
| Honey | "AddFood" Snack satiation. "Delicious!" |
| Slime | Confused effect (3d6 turns, 5d7 power). "It's disgustingly slimy!" |
| Wine | Adds 2x volume water. Intoxication tracking: if amount > max(1, Toughness_mod*2), Confused (5d5 turns). Resets after 80 turns. |
| Cider | Adds 2x volume water. Same intoxication as wine but separate tracker. |
| Blood | "It has a metallic taste." (no effect) |
| Oil | "Disgusting!" (no effect) |
| Salt | Removes 100 water per unit. Dehydrates. "Blech, it's salty!" |
| Goo | Poisoned effect (1d4+4 duration, 1d2+2 d2 damage). "It's awful!" |
| Sludge | Poisoned effect (same as goo). "It's horrifying!" |
| Ooze | Disease (GlotrotOnset or IronshankOnset). "It's repulsive!" |
| Putrescence | Induces vomiting. "It's disgusting!" |
| Brain Brine | Confused (20-30 turns, 1d3+1 power). BrainBrineCurse. "Your mind starts to swim." |
| Cloning | Budding effect (cloning). "You feel unsettlingly ambivalent." |
| Neutron Flux | Immediate explosion at drinker's location |
| Warm Static | Pure: randomly scrambles skills OR mutations (50/50). Mixed: random effect. |
| Sunslag | Permanently increases Speed stat (capped at 10 total). Achievement. |
| Convalessence | "It's effervescent!" |
| Algae | "Snack" satiation, removes 50 water. "The brine stings your mouth." |
| Wax | TemperatureChange(100). "It's hot and disgusting." |
| Asphalt | `(proportion/100 + 1)d6` damage, heats by 500. "It burns!" |

---

## 12. Pouring Mechanics

**File**: `LiquidVolume.cs:6149-6338`

### Three Pour Targets

1. **Into another container** (`PerformFill`): Select target from inventory, ask "How many drams?", call `target.MixWith(this, Amount)`
2. **Into a cell** (`PourIntoCell`): Pick direction, liquid pours on ground
3. **On self** (Douse): Pour on actor's current cell, calls `ProcessContact(Poured=true)`

### PerformFill Flow

```
1. Check if sealed
2. Get actor's inventory and equipment
3. Partition containers: same liquid vs different liquid
4. Show picker to select target
5. If different liquid: ask "Empty first?"
6. Calculate pour amount
7. Call target.MixWith(this, Amount)
```

### PourIntoCell Priority

```
1. Contact creatures in the cell (combat targets)
2. Merge with GroundLiquid if same type (performance optimization)
3. Mix into existing OpenLiquidVolume in cell
4. Fill containers with LIQUID_COLLECTOR flag
5. Create new puddle object if no receptor
```

---

## 13. Contact & Smearing

**File**: `LiquidVolume.cs:2184-2600+`

### ProcessContact Flow

```
1. Pre-checks: not frozen/stasis, target has physics
2. Check existing LiquidCovered effect to prevent duplicates
3. If swimming depth (>= 2000):
   - Apply Swimming effect
   - 50% of volume as exposure
   - Distribute to body parts, equipment, inventory
4. If wading depth (200-1999):
   - Apply Wading effect
   - Exposure scaled by body mobility (0.1-0.4)
5. Body Part Exposure:
   - Prioritize mobility-providing parts (feet/legs first)
   - Distribute randomly across body parts
   - Cap at BodyPartCapacity per part
   - Non-mobile parts get secondary exposure
6. For each exposed body part:
   - Call SmearOn() on equipped item
   - Call SmearOnTick() on ongoing turns
```

### SmearOn Examples

| Liquid | Contact Effect |
|--------|---------------|
| Acid | `ApplyAcid()`: damage = millidrams/20000 + random rolls |
| Water | Amphibious creatures: pour water directly on them |
| Neutron Flux | Immediate explosion (15000 damage, 10d10+250) |
| Warm Static (pure) | `GlitchObject()` on target (transmutation, skill scrambling) |
| Cloning (pure) | Apply Budding effect for cloning |

---

## 14. Liquid Pools & Zone Generation

### LiquidPools Zone Builder

**File**: `XRL.World.ZoneBuilders/LiquidPools.cs`

```csharp
public class LiquidPools {
    public bool BuildZone(Zone Z, string PuddleObject, int Density,
                         string FlamingPools = "0", string PlantReplacements = null)
}
```

Process:
1. Generate NoiseMap with specified density
2. Create puddle objects in noisy cells
3. Optional: set some pools on fire (raise temperature)
4. Optional: replace nearby plants in boundary zones

### Puddle Rendering by Depth

| Depth | Volume | Glyph | Behavior |
|-------|--------|-------|----------|
| Puddle | 1-199 | `,` `` ` `` `'` `˚` (random) | RenderIfDark=false |
| Wading | 200-1999 | `~` (animated) | Frame offset animation |
| Swimming | 2000+ | `~` (animated) | RenderIfDark=true, liquid-specific colors |

---

## 15. Liquid Spreading & Flowing

### FlowIntoCell vs PourIntoCell

| Method | Performance Path | Merges with Ground | Use Case |
|--------|-----------------|-------------------|----------|
| `FlowIntoCell()` | Yes | Yes | Natural spreading |
| `PourIntoCell()` | No | Conditional | Player actions |

### MingleAdjacent (Adjacent Pool Mixing)

**File**: `LiquidVolume.cs:5960-6017`

- Open volumes with same liquid: equilibrate to 50/50 split
- Mixed liquids slowly exchange between adjacent pools
- Sealed containers: no exchange

### Ground Liquid Merging

**File**: `LiquidVolume.cs:6460-6494`

```csharp
public bool CheckGroundLiquidMerge()
{
    if (!IsOpenVolume() || IsWadingDepth()) return false;
    // Pure shallow puddles merge into cell's GroundLiquid string
    // Puddle object destroyed, GroundLiquid persists
    ParentObject.Obliterate();
}
```

---

## 16. Evaporation

**File**: `LiquidVolume.cs:6443-6458`

### Conditions (ALL must be true)

1. Must be open volume (`IsOpenVolume()`)
2. Must be shallow (< 200 drams, NOT wading depth)
3. Must be mixed (ComponentLiquids.Count > 1)
4. Pure liquids do NOT evaporate

### Evaporativity-Based Removal

```csharp
// UseDramsByEvaporativity(int Num)
// Removes liquids by evaporation rate, NOT proportionally
// Higher evaporativity = removed first
// Example: oil-water mix -> water (evaporativity 2) goes first
```

| Liquid | Evaporativity |
|--------|--------------|
| Water | 2 |
| Acid | 1 |
| Lava | 0 (never) |
| Cloning | 100 (extremely volatile) |
| Wine | 3 |
| Convalessence | 15 |

---

## 17. Temperature Effects (Freeze/Vaporize)

### Vaporization

**File**: `LiquidVolume.cs:3838-3862`

When liquid reaches `VaporTemperature`:
1. Call each liquid's `Vaporized()` callback
2. Create gas object from `VaporObject` template
3. Set gas density: `part.Density = Liquid.Amount(ID) / 20`
4. Add gas to cell

| Liquid | VaporObject | VaporTemp |
|--------|------------|-----------|
| Water | SteamGas | 100 |
| Acid | AcidGas | 100 |
| Goo | PoisonGas | 110 |
| Ooze | Miasma | 150 |
| Brain Brine | ConfusionGas | 2000 |
| Warm Static | GlitterGas | 2000 |
| Lava | (doesn't vaporize) | 10000 |

### Freezing

**File**: `LiquidVolume.cs:3864-3949`

When temperature drops to `FreezeTemperature`:
1. Call each liquid's `Froze()` callback
2. Query `GetLiquidFreezeObject(volume)` for object to create
3. If freeze object exists: kill puddle, create solid object, apply Stuck to creatures
4. If no freeze object: apply mild Stuck (5 turns, DC 15)

| Liquid | Freeze Objects (by volume threshold) |
|--------|--------------------------------------|
| Lava | SmallBoulder (1), MediumBoulder (100), Shale (400) |
| Salt | Halite (500) |
| Wax | Wax Nodule (1), Wax Block (500) |
| Convalessence | CryoGas (1) |
| Water | (default ice behavior) |

---

## 18. Slippery & Sticky Mechanics

### Slippery Save

```csharp
int DC = Math.Min(24,
    SlipperySaveTargetBase +
    Liquid.Amount(ID).DiminishingReturns(Scale) -
    (Subject?.GetIntProperty("Stable") ?? 0)
);
// Save: Agility vs "Slip Move"
// Failure: forced random-direction movement
```

**Slippery liquids**: Gel, Ink, Oil, Slime, Water (frozen only)

### Sticky Save

```csharp
// Save: Strength/Agility vs "Stuck Restraint"
// Failure: Stuck effect for StickyDuration (12) turns
```

**Sticky liquids**: Asphalt, Honey, Sap, Wax

---

## 19. Event System (Dram Events)

### Multi-Pass Priority System

Both `GiveDramsEvent` and `UseDramsEvent` use a **pass system** for priority routing:

#### GiveDrams (Store liquid in container)

| Pass | Priority | Condition |
|------|----------|-----------|
| 1 | Highest | `AutoCollectLiquidType` matches (e.g., waterskin autocollects water) |
| 2 | | Container explicitly wants this liquid type |
| 3 | | Container has pure liquid AND doesn't produce it |
| 4 | | Container is empty AND doesn't produce it |
| 5 | Lowest | Accept anything |

#### UseDrams (Extract liquid from container)

| Pass | Priority | Condition |
|------|----------|-----------|
| 1 | Highest | Liquid-producing object (e.g., acid gland) |
| 2 | | Container that doesn't collect this liquid |
| 3+ | Lowest | Any container |

#### GetFreeDrams ("How much can I drink?")

```csharp
// Pure: E.Drams += Volume
// Mixed (ImpureOkay): E.Drams += Math.Max(Volume * ComponentLiquids[Liquid] / 1000, 1)
// Sealed: returns 0
```

#### GetStorableDrams ("How much can I store?")

```csharp
// E.Drams += MaxVolume - Volume (available capacity)
// Only accepts pure liquids OR empty container with matching AutoCollectLiquidType
// Sealed: returns 0
```

---

## 20. Liquid Producer

**File**: `XRL.World.Parts/LiquidProducer.cs`

Generates liquid on a tick-based schedule:

```csharp
public int Rate = 1;              // Ticks between productions
public string Liquid = "water";   // What to produce
public bool FillSelfOnly;         // Only fill self, not distribute
public bool PreferCollectors;     // Prefer collector containers
public bool PureOnFloor;          // Only pour pure on floor
```

Process: Each turn, if `Tick >= Rate`, produces 1 dram and calls `DistributeLiquid()`:
1. Try filling self (ParentObject's LiquidVolume)
2. Try filling same-cell containers
3. Try filling same-cell open volumes
4. Create new puddle if needed

---

## 21. Navigation Weights

Each liquid type returns a navigation weight for AI pathfinding:

| Liquid | Weight (Normal) | Weight (Smart) | Notes |
|--------|----------------|----------------|-------|
| Most liquids | 0 | 0 | Passable freely |
| Goo | 2 | 3 | Unless FilthAffinity |
| Sludge | 2 | 3 | Unless FilthAffinity |
| Ooze | 2 | 3 | Unless FilthAffinity |
| Putrescence | 2 | 0 (smart) | Smart creatures avoid |
| Acid | 30 (no resistance) | Scales with AcidResistance | |
| Lava | 99 (no resistance) | Scales with HeatResistance | |
| Neutron Flux | 99 | 99 | Always dangerous |

Weight >= 2 also triggers `InterruptAutowalk`.

---

## 22. Value & Economy

### Value Calculation

```csharp
public float GetExtrinsicValuePerDram(bool Pure)
{
    return GetValuePerDram() * (Pure ? GetPureLiquidValueMultiplier() : 1f);
}
```

### Value Table (Sorted by Value)

| Liquid | Base Value | Pure Multiplier | Effective Pure Value |
|--------|-----------|----------------|---------------------|
| Cloning | 1250.0 | 1.0 | 1250.0 |
| Brain Brine | 1233.0 | 1.0 | 1233.0 |
| Neutron Flux | 1000.0 | 1.0 | 1000.0 |
| Sunslag | 1000.0 | 1.0 | 1000.0 |
| Lava | 50.0 | 1.0 | 50.0 |
| Wine | 4.0 | 1.0 | 4.0 |
| Protean Gunk | 4.0 | 1.0 | 4.0 |
| Cider | 3.8 | 1.0 | 3.8 |
| Oil | 3.0 | 1.0 | 3.0 |
| Sap | 2.0 | 1.0 | 2.0 |
| Honey | 2.0 | 1.0 | 2.0 |
| Acid | 1.5 | 1.0 | 1.5 |
| Ink | 1.5 | 1.0 | 1.5 |
| **Water** | **0.01** | **100.0** | **1.0** |
| Gel | 0.5 | 1.0 | 0.5 |
| Blood | 0.25 | 1.0 | 0.25 |
| Slime | 0.1 | 1.0 | 0.1 |
| Salt/Sludge/Ooze/etc. | 0.0 | 1.0 | 0.0 |

**Key**: Pure fresh water has 100x multiplier, making it the universal currency at 1.0/dram.

---

## 23. Cooking Ingredients

Each liquid can return a cooking ingredient category:

| Liquid | Ingredient |
|--------|-----------|
| Acid | "acidMinor" |
| Lava | "heatMinor" |
| Honey | "medicinalMinor" |
| Slime | "slimeSpitting" |
| Algae | "plantMinor" |
| Asphalt | "stabilityMinor" |
| Brain Brine | (none) |
| Cider | "quicknessMinor" |
| Cloning | "cloningMinor" |
| Convalessence | "coldMinor,regenLowtierMinor" |
| Goo | "selfPoison" |
| Neutron Flux | "density" |
| Ooze | "selfGlotrot" |
| Salt | "tastyMinor" |
| Sludge | "selfPoison" |
| Water | (none) |

---

## 24. Rendering & Visuals

### Color System

Qud uses CP437 color codes:
- Lowercase (k,r,y,g,c,b,m,w) = dark variants
- Uppercase (K,R,Y,G,C,B,M,W) = bright variants

### Glowing Liquids

| Liquid | Glows | Notes |
|--------|-------|-------|
| Lava | Yes | White/Red light |
| Cloning | Yes | Yellow glow |
| Convalessence | Yes | Cyan glow, zone light based on volume |
| Neutron Flux | Yes | Yellow glow |
| Sunslag | Yes | Yellow glow |

### Animated Rendering

Liquids at wading depth use frame-based animation:
```csharp
int num = (XRLCore.CurrentFrame + Liquid.FrameOffset) % 60;
// Cycles through glyphs: "÷" -> "~" -> "\t" -> "~" per 15-frame intervals
// Random cosmetic sparkles (1/600 chance per frame)
```

### Paint Atlases by Liquid

| Liquid | Paint Atlas |
|--------|------------|
| Algae | "Liquids/Paisley/" |
| Asphalt | "Liquids/Speckle/" |
| Brain Brine | "Liquids/Dot/" |
| Cloning | "Liquids/Paisley/" |
| Goo/Sludge/Ooze/Slime | "Liquids/Splotchy/" |
| Honey/Protean Gunk | "Liquids/Gunk/" |
| Salt | "Liquids/Speckle/" |
| Wax | "Liquids/Splotchy/" |

---

## 25. All 27 Liquid Types

### Complete Property Table

| ID | Name | Color | Weight | Fluidity | Combustibility | FlameTemp | VaporTemp | Evaporativity |
|----|------|-------|--------|----------|----------------|-----------|-----------|--------------|
| water | fresh water | B | 0.25 | 30 | -50 | 99999 | 100 | 2 |
| acid | acid | G | 0.25 | 30 | 3 | 99999 | 100 | 1 |
| lava | lava | R | 0.50 | 15 | 0 | 99999 | 10000 | 0 |
| blood | blood | r | 0.25 | 5 | 2 | 400 | 1200 | 1 |
| oil | oil | K | 0.25 | 25 | 90 | 250 | 2000 | 0 |
| slime | slime | g | 0.25 | 10 | 8 | 550 | 1550 | 1 |
| honey | honey | w | 0.50 | 10 | 60 | 300 | 1300 | 1 |
| salt | salt | y | 0.25 | 35 | 0 | 99999 | 200 | 0 |
| gel | gel | Y | 0.25 | 5 | 0 | 99999 | 100 | 1 |
| goo | green goo | G | 0.25 | 10 | 20 | 400 | 110 | 1 |
| sludge | brown sludge | w | 0.25 | 10 | 7 | 575 | 1575 | 1 |
| ooze | black ooze | K | 0.25 | 10 | 15 | 500 | 150 | 1 |
| sap | sap | W | 0.50 | 3 | 70 | 250 | 1250 | 1 |
| ink | ink | K | 0.25 | 10 | 30 | 350 | 1350 | 1 |
| algae | algae | g | 0.25 | 35 | 0 | 99999 | 200 | 1 |
| asphalt | asphalt | K | 0.50 | 1 | 75 | 240 | 1240 | 0 |
| brainbrine | brain brine | g | 0.25 | 10 | 30 | 99999 | 2000 | 1 |
| cider | cider | w | 0.25 | 30 | 5 | 500 | 800 | 5 |
| cloning | cloning draught | Y | 0.25 | 10 | 0 | 99999 | 100 | 100 |
| convalessence | convalessence | C | 0.25 | 20 | 5 | 99999 | 100 | 15 |
| neutronflux | neutron flux | y | 2.50 | 100 | 0 | 99999 | 10000 | 100 |
| proteangunk | primordial soup | c | 0.25 | 25 | 40 | 99999 | 2000 | 1 |
| putrid | putrescence | K | 0.25 | 15 | 5 | 600 | 1600 | 1 |
| sunslag | sunslag | Y | 0.25 | 10 | 30 | 99999 | 2000 | 1 |
| warmstatic | warm static | Y | 0.25 | 10 | 30 | 99999 | 2000 | 1 |
| wax | molten wax | Y | 0.50 | 7 | 65 | 300 | 2000 | 0 |
| wine | wine | m | 0.25 | 30 | 15 | 620 | 1620 | 3 |

### Contact Properties Table

| ID | Slippery Wet | Slippery Frozen | Sticky Wet | Sticky Frozen | Dangerous Contact | Dangerous Drink | Staining | Cleansing |
|----|-------------|----------------|-----------|--------------|-------------------|-----------------|----------|-----------|
| water | - | Yes | - | - | - | - | 0 | 5 |
| acid | - | - | - | - | Yes | Yes | 1 | 30 |
| lava | - | - | - | - | Yes | Yes | 0 | 20 |
| oil | Yes | Yes | - | - | - | - | 1 | 1 |
| slime | Yes | Yes | - | - | - | - | 1 | 0 |
| honey | - | - | Yes | - | - | - | 0 | 0 |
| sap | - | - | Yes | - | - | - | 2 | 0 |
| gel | Yes | Yes | - | - | - | - | 0 | 1 |
| ink | Yes | Yes | - | - | - | - | 3 | 0 |
| asphalt | - | - | Yes | - | - | - | 1 | 0 |
| wax | - | - | Yes | - | - | - | 0 | 3 |
| neutronflux | - | - | - | - | Yes | Yes | 0 | 100 |

### Electrical Conductivity Table

| ID | Pure | Mixed |
|----|------|-------|
| water | 0 | 100 |
| salt | 0 | 100 |
| oil | 0 | 0 |
| wax | 0 | 0 |
| neutronflux | 0 | 0 |
| gel | 100 | 100 |
| sunslag | 100 | 100 |
| warmstatic | 100 | 100 |
| lava | 90 | 90 |
| Most others | 80 | 80 |
| convalessence | 10 | 10 |
| asphalt | 10 | 10 |
| honey | 40 | 40 |
| ink | 40 | 40 |

---

## 26. Special Reactions

### Neutron Flux - Explosions

**File**: `XRL.Liquids/LiquidNeutronFlux.cs`

- `MixingWith()`: Can block mix if explosion interrupted
- `MixedWith()`: Triggers explosion (15d10+250, neutron type)
- `SmearOn()`/`SmearOnTick()`: Immediate explosion
- `ObjectEnteredCell()`: Explosion on entry if open volume
- `Drank()`: Explosion at drinker's location

### Warm Static - Glitching

**File**: `XRL.Liquids/LiquidWarmStatic.cs`

- `MixedWith()`: In sealed containers, applies `ContainedStaticGlitching` effect
- `PourIntoCell()` (pure): `GlitchObject()` on targets (transmutation, skill/mutation scrambling)
- `SmearOn()` (pure): Glitches target object
- `Drank()` (pure): 50/50 chance to scramble skills or mutations

### Acid - Container Damage

**File**: `XRL.Liquids/LiquidAcid.cs`

- `FillingContainer()`: Applies `ContainedAcidEating` effect to organic containers
- `SafeContainer()`: Returns `!GO.IsOrganic` (acid destroys organic containers)

### Protean Gunk - Spawning

- `ProcessTurns()`: Can spawn up to 30 SoupSludge entities per zone when in contact with secondary liquids

### Lava - Solidification

- When frozen: creates SmallBoulder (< 100), MediumBoulder (100-399), Shale (400+)
- `SafeContainer()`: Only containers with FlameTemperature > 1000

---

## 27. Key Source Files

| File | Lines | Description |
|------|-------|-------------|
| `XRL.Liquids/BaseLiquid.cs` | 711 | Abstract base class for all liquid types |
| `XRL.World.Parts/LiquidVolume.cs` | 6,752 | Container/pool Part with all actions |
| `XRL.World.Parts/LiquidProducer.cs` | ~200 | Tick-based liquid generation |
| `XRL.World.Parts/MovespeedInLiquid.cs` | ~50 | Movement speed modifier in liquids |
| `XRL.World.Parts/AISeekHealingPool.cs` | ~50 | AI behavior to seek healing pools |
| `XRL.World.ZoneBuilders/LiquidPools.cs` | ~90 | Zone builder for liquid pool placement |
| `XRL.Liquids/LiquidWater.cs` | ~280 | Water implementation |
| `XRL.Liquids/LiquidAcid.cs` | ~200 | Acid implementation |
| `XRL.Liquids/LiquidLava.cs` | ~200 | Lava implementation |
| `XRL.Liquids/LiquidNeutronFlux.cs` | ~170 | Neutron flux (explosive) |
| `XRL.Liquids/LiquidWarmStatic.cs` | ~610 | Warm static (glitching) |
| + 16 more liquid type files | | One per liquid type |

---

## Design Summary

1. **Proportional mixing** via 1000-point scale avoids floating-point errors
2. **Multi-pass event system** routes liquids to appropriate containers by priority
3. **Dual electrical conductivity** models real physics (pure water vs saltwater)
4. **Value multiplier pattern** makes pure water 100x more valuable (currency)
5. **Hook-based reactions** (MixingWith/MixedWith) enable special chemistry
6. **Contact exposure** distributes across body parts weighted by mobility
7. **Temperature cascade** (freeze/vaporize) creates phase-change objects
8. **All 27 types** share a common property+virtual method pattern for extensibility
