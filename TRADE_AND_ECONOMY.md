# Caves of Qud: Trade & Economy System

> Deep dive into how Qud implements merchants, barter, water currency, item valuation, haggling, and inventory restocking. All findings sourced from the decompiled source code at `qud-decompiled-project/`.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Water Drams as Currency](#2-water-drams-as-currency)
3. [Dram Event System](#3-dram-event-system)
4. [Container Capacity & Storage](#4-container-capacity--storage)
5. [Item Value Pipeline](#5-item-value-pipeline)
6. [Commerce Part (Base Value)](#6-commerce-part-base-value)
7. [Value Events (Intrinsic, Adjust, Extrinsic)](#7-value-events-intrinsic-adjust-extrinsic)
8. [Condition Penalties on Value](#8-condition-penalties-on-value)
9. [Mod/Enchantment Value Effects](#9-modenchantment-value-effects)
10. [Trade Performance Formula (Ego)](#10-trade-performance-formula-ego)
11. [Buy vs Sell Price Asymmetry](#11-buy-vs-sell-price-asymmetry)
12. [The Haggling Sifrah Game](#12-the-haggling-sifrah-game)
13. [Reputation Effects on Trading](#13-reputation-effects-on-trading)
14. [Merchant Identification](#14-merchant-identification)
15. [GenericInventoryRestocker Part](#15-genericinventoryrestocker-part)
16. [Inventory Generation (Population Tables)](#16-inventory-generation-population-tables)
17. [Merchant Types (General, Apothecary, Tinker)](#17-merchant-types-general-apothecary-tinker)
18. [Restocking Mechanics](#18-restocking-mechanics)
19. [The Stock Marking System](#19-the-stock-marking-system)
20. [Trade Initiation Flow](#20-trade-initiation-flow)
21. [Item Validation for Trade](#21-item-validation-for-trade)
22. [The Trade UI](#22-the-trade-ui)
23. [Transaction Execution (PerformOffer)](#23-transaction-execution-performoffer)
24. [Vendor Services (Identify, Repair, Recharge)](#24-vendor-services-identify-repair-recharge)
25. [Debt & Credit System](#25-debt--credit-system)
26. [NPC Shopping AI](#26-npc-shopping-ai)
27. [Trade Events Reference](#27-trade-events-reference)
28. [Key Source Files](#28-key-source-files)

---

## 1. Overview

Qud's economy is built on **fresh water drams** as universal currency. Every item has a value in drams (via the Commerce part), and all trades settle the difference in water. The player's **Ego stat** determines trade performance — a single multiplier that affects all prices. Merchants restock periodically from tier-based population tables, and an optional Sifrah-based haggling minigame can shift prices further.

**Core formula**: `Performance = clamp(0.35 + 0.07 * EgoMod, 0.05, 0.95)`

---

## 2. Water Drams as Currency

**Source**: `XRL.Liquids/LiquidWater.cs`, `XRL.World.Parts/LiquidVolume.cs`

Fresh water is the sole accepted medium of exchange. The unit is the **dram** — a volumetric measure of liquid.

### Water Value Per Dram
```csharp
// LiquidWater.cs
public override float GetValuePerDram() => 0.01f;        // Base value
public override float GetPureLiquidValueMultipler() => 100f; // Pure multiplier
```

- **Pure fresh water**: 0.01 × 100 = **1.0 value per dram** (the currency unit)
- Mixed/impure water: Reduced value
- Saltwater, contaminated: Not accepted as currency

### Purity Check
```csharp
// LiquidVolume.cs
public bool IsFreshWater(bool AllowEmpty = false)
{
    return IsPureLiquid("water", AllowEmpty);
}
```

Only pure, uncontaminated water passes the currency check. The trade UI explicitly prevents trading fresh water in containers:
```csharp
// TradeUI.cs line 116
if (liquidVolume != null && liquidVolume.IsFreshWater() && !Object.HasPart<TinkerItem>())
    return false; // Can't trade water containers with fresh water
```

### The AssumeTradersHaveWater Flag
```csharp
// TradeUI.cs line 42
public static bool AssumeTradersHaveWater = true;
```
When true (default), merchants don't need physical water containers — they're assumed to have unlimited dram access. This prevents NPC bankruptcy and simplifies the economy.

---

## 3. Dram Event System

**Source**: `XRL.World/GetFreeDramsEvent.cs`, `UseDramsEvent.cs`, `GiveDramsEvent.cs`, `GetStorableDramsEvent.cs`

All water currency operations use an event-based cascade system:

### GetFreeDrams — Query Available Currency
```csharp
// GameObject.cs
public int GetFreeDrams(string liquidType = "water", GameObject skip = null,
                         List<GameObject> skipList = null, Predicate<GameObject> filter = null,
                         bool impureOkay = false)
{
    return GetFreeDramsEvent.GetFor(this, liquidType, skip, skipList, filter, impureOkay);
}
```
Cascades through all inventory items with LiquidVolume parts, summing available pure water drams.

### UseDrams — Deduct Currency (3-Pass Cascade)
```csharp
public bool UseDrams(int drams, string liquidType = "water", ...)
{
    return !UseDramsEvent.Check(this, liquidType, drams, ...);
}
```
Uses a **3-pass system** allowing handlers multiple opportunities to refuse or contribute drams. Supports `ImpureOkay` flag for non-trade contexts and `drinking` flag to distinguish trade deductions from consumption.

### GiveDrams — Add Currency (5-Pass Cascade)
```csharp
public bool GiveDrams(int drams, string liquidType = "water", bool auto = false, ...)
{
    return !GiveDramsEvent.Check(this, liquidType, drams, ...);
}
```
Uses a **5-pass system** for complex distribution across multiple containers. The `SafeOnly` parameter prevents storing in unsafe/contaminated containers.

### GetStorableDrams — Query Container Capacity
```csharp
public int GetStorableDrams(string liquidType = "water", ...)
{
    return GetStorableDramsEvent.GetFor(this, liquidType, ...);
}
```

LiquidVolume handler:
```csharp
// Returns empty space in containers
E.Drams += MaxVolume - Volume;  // If container has room and is compatible
```

---

## 4. Container Capacity & Storage

**Source**: `XRL.World.Parts/LiquidVolume.cs`

Water is physically stored in containers:

| Container | Typical Capacity | Notes |
|-----------|-----------------|-------|
| Waterskin | 16 drams | Common starter |
| Canteen | 32 drams | Standard capacity |
| Vial | 1 dram | Small |

**LiquidVolume Key Properties**:
- `ComponentLiquids`: Dictionary mapping liquid ID → dram count
- `Volume`: Total current liquid volume
- `MaxVolume`: Container capacity (-1 = unlimited for puddles)
- `EffectivelySealed()`: Whether container is sealed/inaccessible

**Weight**: Water has weight ~0.25 per dram (from `BaseLiquid.Weight`). The trade UI factors water weight into carry capacity:
```csharp
int waterWeight = (int)(LiquidVolume.GetLiquid("water").Weight * CalculateTrade(Totals[0], Totals[1]));
```

---

## 5. Item Value Pipeline

**Source**: `XRL.World/GameObject.cs` (lines 571-598)

Every item's value is computed through a **3-stage event pipeline**:

```
ValueEach = Commerce.Value (base)
    → GetIntrinsicValueEvent  (parts modify base value)
    → AdjustValueEvent        (multiplicative penalties)
    → GetExtrinsicValueEvent  (add contained item values)

Total Value = ValueEach × Count
```

```csharp
public double ValueEach {
    get {
        double num = GetPart<Commerce>()?.Value ?? 0.01;

        // Stage 1: Intrinsic modifications
        if (WantEvent(GetIntrinsicValueEvent.ID, ...)) {
            var evt = GetIntrinsicValueEvent.FromPool(this, num);
            HandleEvent(evt);
            num = evt.Value;
        }
        // Stage 2: Multiplicative adjustments (conditions, knowledge)
        if (WantEvent(AdjustValueEvent.ID, ...)) {
            var evt = AdjustValueEvent.FromPool(this, num);
            HandleEvent(evt);
            num = evt.Value;
        }
        // Stage 3: Add contained/equipped values
        if (WantEvent(GetExtrinsicValueEvent.ID, ...)) {
            var evt = GetExtrinsicValueEvent.FromPool(this, num);
            HandleEvent(evt);
            num = evt.Value;
        }
        return num;
    }
}
```

---

## 6. Commerce Part (Base Value)

**Source**: `XRL.World.Parts/Commerce.cs`

```csharp
public class Commerce : IPart
{
    public double Value = 1.0;  // Base dram value
}
```

- Default value when no Commerce part: **0.01** (nearly worthless)
- Set in blueprints or by generation code
- `CommerceRangeValue` part can roll a random range and create Commerce dynamically:
  ```csharp
  // At ObjectCreated:
  ParentObject.GetPart<Commerce>().Value = Stat.Roll(Range);
  ```

---

## 7. Value Events (Intrinsic, Adjust, Extrinsic)

**Source**: `XRL.World/IValueEvent.cs`, `GetIntrinsicValueEvent.cs`, `AdjustValueEvent.cs`, `GetExtrinsicValueEvent.cs`

### Base Interface
```csharp
public abstract class IValueEvent : MinEvent {
    public GameObject Object;
    public double Value = 0.01;
}
```

### GetIntrinsicValueEvent
Parts can modify the base value. Example — ModGigantic gives 3.33x to currency items:
```csharp
// ModGigantic.cs
public override bool HandleEvent(GetIntrinsicValueEvent E) {
    if (E.Object.GetIntProperty("Currency") > 0)
        E.Value *= 3.333333;
    return base.HandleEvent(E);
}
```

### AdjustValueEvent
**Multiplicative** adjustments. Used for conditions and knowledge state:
```csharp
public void AdjustValue(double Factor) {
    Value *= Factor;  // Multiplicative
}
```

### GetExtrinsicValueEvent
**Additive** — adds value of contained items:
```csharp
// Inventory.cs
public override bool HandleEvent(GetExtrinsicValueEvent E) {
    for (int i = 0; i < Objects.Count; i++)
        E.Value += Objects[i].Value;  // Adds ALL contents' values
    return base.HandleEvent(E);
}
```

This means containers recursively include their contents' total value.

---

## 8. Condition Penalties on Value

**Source**: Various effect/part files

| Condition | Multiplier | Source |
|-----------|-----------|--------|
| Broken | ×0.01 (1%) | `XRL.World.Effects/Broken.cs` |
| Rusted | ×0.01 (1%) | `XRL.World.Effects/Rusted.cs` |
| Shattered Armor | ×(1/amount) | `XRL.World.Effects/ShatteredArmor.cs` |
| Unknown item | ×0.10 (10%) | `XRL.World.Parts/Examiner.cs` |
| Partially identified | ×0.20 (20%) | `XRL.World.Parts/Examiner.cs` |
| Fully identified | ×1.00 (100%) | `XRL.World.Parts/Examiner.cs` |

### Identification Logic (Examiner Part)
```csharp
public override bool HandleEvent(AdjustValueEvent E) {
    if (GetEpistemicStatus() != 2) {  // Not fully identified
        GameObject owner = ParentObject.Equipped ?? ParentObject.InInventory;
        if (owner != null && owner.IsPlayer()) {
            E.AdjustValue((GetEpistemicStatus() == 1) ? 0.2 : 0.1);
        }
    }
}
```

**Key insight**: Unidentified items are worth only 10-20% to the player. Identifying items before selling dramatically increases their trade value.

---

## 9. Mod/Enchantment Value Effects

**Source**: `XRL.World/ModEntry.cs`, `XRL.World/ModificationFactory.cs`

### ModEntry Structure
```csharp
public class ModEntry {
    public double Value = 1.0;  // Value multiplier for this mod
    public int Rarity;          // C, U, R, R2, R3
    public int Tier;            // Item tier (1-8)
}
```

### Rarity Weights
| Rarity | Weight | Description |
|--------|--------|-------------|
| Common (C) | 100,000 | Most frequent |
| Uncommon (U) | 40,000 | — |
| Rare (R) | 10,500 | — |
| Rare2 (R2) | 1,500 | Very rare |
| Rare3 (R3) | 150 | Extremely rare |

Higher rarity mods generally have higher Value multipliers, making rare items worth more. Mod values are loaded from XML data (Mods.xml) and applied during item generation.

---

## 10. Trade Performance Formula (Ego)

**Source**: `XRL.World/GetTradePerformanceEvent.cs` (lines 40-91)

The **Performance** value is the core price multiplier, derived from the player's Ego stat:

```csharp
public static double GetFor(GameObject Actor, GameObject Trader)
{
    if (!Actor.HasStat("Ego"))
        return 0.25;  // Non-egoic beings trade poorly

    int egoMod = Actor.StatMod("Ego");
    double linearAdj = 0.0;
    double factorAdj = 1.0;

    // Fire event — skills/parts can modify linearAdj and factorAdj
    var evt = FromPool(Actor, Trader, egoMod, linearAdj, factorAdj);
    Actor.HandleEvent(evt);

    // Final formula, clamped to [0.05, 0.95]
    return Math.Min(Math.Max(
        (0.35 + 0.07 * ((double)evt.EgoModifier + evt.LinearAdjustment)) * evt.FactorAdjustment,
        0.05), 0.95);
}
```

### Performance by Ego

| Ego Modifier | Performance | Effect |
|-------------|-------------|--------|
| -4 | 0.07 | Terrible prices |
| -2 | 0.21 | Poor prices |
| 0 | 0.35 | Baseline (35%) |
| +2 | 0.49 | Decent prices |
| +4 | 0.63 | Good prices |
| +6 | 0.77 | Great prices |
| +8 | 0.91 | Excellent prices |
| +9+ | 0.95 | Maximum (capped) |

### Modifiable Parameters
| Parameter | Default | Modified By |
|-----------|---------|-------------|
| `EgoModifier` | Actor.StatMod("Ego") | Mutations (HeightenedEgo adds 2+(Level-1)/2) |
| `LinearAdjustment` | 0.0 | Skills, parts via event handler |
| `FactorAdjustment` | 1.0 | Skills, parts via event handler |

### Rating (For Haggling)
```csharp
public static int GetRatingFor(GameObject Actor, GameObject Trader)
{
    return (int)((EgoMod + LinearAdj) * FactorAdj);
}
```
This integer rating determines which haggling tokens are available and the Sifrah difficulty.

---

## 11. Buy vs Sell Price Asymmetry

**Source**: `XRL.UI/TradeUI.cs` (lines 236-240)

Performance applies **differently** depending on direction:

```csharp
public static double GetValue(GameObject obj, bool? TraderInventory = null)
{
    if (obj is in trader's inventory)
        return ItemValueEach(obj) / Performance;   // Player BUYS: divide by perf
    else
        return ItemValueEach(obj) * Performance;   // Player SELLS: multiply by perf
}
```

### Price Examples (Performance = 0.50)

| Item Base Value | Buy From Trader | Sell To Trader |
|----------------|-----------------|----------------|
| 10 drams | 10 / 0.50 = **20 drams** | 10 × 0.50 = **5 drams** |
| 50 drams | 50 / 0.50 = **100 drams** | 50 × 0.50 = **25 drams** |
| 100 drams | 100 / 0.50 = **200 drams** | 100 × 0.50 = **50 drams** |

**Key insight**: At Performance = 0.50, items cost **2x their value** to buy and sell for **half their value**. Higher Ego narrows this gap (at 0.95, buy cost ≈ 1.05x, sell value ≈ 0.95x).

### Currency Exception
```csharp
public static double GetMultiplier(GameObject GO)
{
    if (GO == null || !GO.IsCurrency)
        return Performance;  // Normal items use Performance
    return 1.0;              // Currency trades at 1:1
}
```

Water drams used as currency always trade at face value (1:1), regardless of Performance.

### Minimum Sell Value
Traders can enforce a floor price:
```csharp
int minSell = _Trader.GetIntProperty("MinimumSellValue");
if (minSell > 0 && itemValue < minSell)
    itemValue = minSell;  // Floor the price
```

---

## 12. The Haggling Sifrah Game

**Source**: `XRL.World/HagglingSifrah.cs`

Haggling is implemented as a **Sifrah puzzle game** — a match-token challenge where the player uses social tokens to improve their trade deal.

### Setup Parameters
- **Difficulty**: Trader's tier (1-8), adjusted by reputation
- **Rating**: Based on player's Ego stat (via `GetRatingFor`)
- **MaxTurns**: Base 3, modified by reputation and difficulty
- **Slots**: 4-6 puzzle slots

### Difficulty Scaling
```csharp
int Difficulty = ContextObject.GetTier();
if (Difficulty >= 3) slots++;      // +1 slot
if (Difficulty >= 7) {
    slots++;                        // +1 more slot
    additionalTokens += 4;         // +4 tokens available
}
```

### Available Haggling Tokens (15+ Types)

| Token | Requirement |
|-------|-------------|
| The Power of Love | Specific skill learned |
| Scanning | Scanning capability for trader type |
| Sociable Chat | Rating >= Difficulty + 3 |
| Listen Sympathetically | Rating >= Difficulty + 2 |
| Crack a Joke | Rating >= Difficulty + 1 |
| Pay a Compliment | Rating >= Difficulty |
| Leverage Being Loved | Faction rep +2 (Beloved) |
| Leverage Being Favored | Faction rep +1 (Favored) |
| Leverage Being True Kin | True Kin + Robot trader |
| Flatter Insincerely | Rating >= Difficulty - 1 |
| Spin a Tale of Woe | Rating >= Difficulty - 2 |
| Posture Intimidatingly | Constitution 10+ higher than trader |
| Telepathy | Telepathic contact with trader |
| Empathy | Empathic contact with trader |
| Tenfold Path of Sed | Specific skill learned |
| Items/Gifts/Liquids/Bits/Charges | Various resource tokens |

### Outcome Impact on Performance
```csharp
// TradeUI.cs lines 1045-1052
if (hagglingSifrah.Performance > 0)
    Performance += (1.0 - Performance) * hagglingSifrah.Performance / 100.0;
else
    Performance -= Performance * (-hagglingSifrah.Performance) / 100.0;
```

| Outcome | Performance Change | Example (base 0.35) |
|---------|-------------------|---------------------|
| Critical Failure | -80 to -90 | 0.35 → ~0.04 |
| Failure | -40 to -60 | 0.35 → ~0.14 |
| Partial Success | 0 to +10 | 0.35 → ~0.42 |
| Success | +20 to +40 | 0.35 → ~0.61 |
| Critical Success | +60 to +80 | 0.35 → ~0.87 |

**Trigger**: "SifraHaggle" command when `Options.SifrahHaggling` is enabled. Must have items selected and at least half the required drams.

---

## 13. Reputation Effects on Trading

**Source**: `XRL.World/HagglingSifrah.cs` (lines 34-68)

Faction reputation directly affects haggling difficulty:

```csharp
string primaryFaction = ContextObject.GetPrimaryFaction();
int reputationLevel = The.Game.PlayerReputation.GetLevel(primaryFaction);
```

| Reputation | Level | Effect |
|-----------|-------|--------|
| Beloved | +2 | +1 extra turn, "Leverage Being Loved" token |
| Favored | +1 | "Leverage Being Favored" token |
| Neutral | 0 | No special effects |
| Hated | -1 | +1 difficulty |
| Despised | -2 | +2 difficulty, +1 slot, -1 turn |

**Summary**: Good reputation makes haggling easier (more tokens, more turns). Bad reputation makes it harder (higher difficulty, fewer turns, more slots to fill).

---

## 14. Merchant Identification

**Source**: `XRL.World/GameObject.cs` (line 18946)

```csharp
public bool IsMerchant()
{
    if (!HasTagOrProperty("Merchant"))
        return HasPart<GenericInventoryRestocker>();
    return true;
}
```

A GameObject is a merchant if:
1. It has the `"Merchant"` tag/property, **OR**
2. It has the `GenericInventoryRestocker` part

### Visual Indicator
`MerchantIconColor` part colors merchants **white** (`&W`) with priority 100.

### Merchant Revealer
`MerchantRevealer` part creates in-world books/advertisements that reveal merchant locations via journal map notes.

---

## 15. GenericInventoryRestocker Part

**Source**: `XRL.World.Parts/GenericInventoryRestocker.cs`

The core component managing merchant inventory generation and restocking.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `LastRestockTick` | long | 0 | Last restock timestamp |
| `RestockFrequency` | long | 6000 | Ticks between restocks (5 game days at 1200 ticks/day) |
| `Chance` | int | 100 | Percentage chance to restock when due |
| `Tables` | List\<string\> | — | Population tables for normal stock |
| `HeroTables` | List\<string\> | — | Additional tables for hero merchants |

### Key Methods

| Method | Description |
|--------|-------------|
| `PerformStock(Restock, Silent)` | Generate inventory from population tables |
| `PerformRestock(Silent)` | Remove old `_stock` items, then call PerformStock |
| `AddTable(string)` | Add a population table to the Tables list |
| `Clear()` | Reset all tables |

---

## 16. Inventory Generation (Population Tables)

**Source**: `XRL.World/GameObject.cs` (EquipFromPopulationTable, lines 11213-11267)

### Method
```csharp
public void EquipFromPopulationTable(
    string Table,                    // e.g., "Tier3Wares"
    int ZoneTier = 1,               // Zone difficulty tier
    Action<GameObject> Process,     // Post-processing (craftmarks)
    string Context,                 // "Stock" or "Restock"
    bool NoStack, bool Silent)
```

### Process
1. Create variable dictionary:
   - `ownertier`: Merchant's tier
   - `ownertechtier`: Merchant's tech tier
   - `zonetier`: Current zone tier
2. Call `PopulationManager.Generate(Table, vars)` to roll items
3. Handle special `*relic:` blueprint prefix via `RelicGenerator.GenerateRelic()`
4. Apply optional craftmarks (if merchant has `HasMakersMark` part)
5. Add items to merchant inventory via `ReceiveObject()`

### Craftmark System
If a merchant has `HasMakersMark`, generated items get:
- Crafter name and color attribution
- Random basic bestowals (30-value each, up to N attempts based on `HeroGenericInventoryBasicBestowalChances`)

---

## 17. Merchant Types (General, Apothecary, Tinker)

**Source**: `XRL.World.ZoneBuilders/VillageCoda.cs`

### General Merchant
```csharp
GenericInventoryRestocker restocker = merchant.RequirePart<GenericInventoryRestocker>();
restocker.Clear();
for (int i = 0; i <= 2 && villageTier > i; i++)
    restocker.AddTable("Tier" + (villageTier - i) + "Wares");
```

| Village Tier | Tables Used |
|-------------|-------------|
| 1 | Tier1Wares |
| 3 | Tier3Wares, Tier2Wares, Tier1Wares |
| 5 | Tier5Wares, Tier4Wares, Tier3Wares |
| 8 | Tier8Wares, Tier7Wares, Tier6Wares |

**Creates graduated inventory** — items from current tier and 2 tiers below.

### Apothecary
- Template: `HumanApothecaryN` (N = clamped village tier 1-8)
- Table: `"Village Apothecary 1"` (or inherited from template)
- Chance: 100% (always restocks)
- Role: `"Apothecary"`, property `VillageApothecary = 1`

### Tinker
- Template: `HumanTinkerN` (N = village tier 1-8)
- Table: `"Village Tinker 1"` (or inherited from template)
- Chance: 100% (always restocks)
- Role: `"Tinker"`, property `VillageTinker = 1`
- Gets workbenches and furniture in their building

### Dromad Traders
- Created from `DromadTrader` + tier blueprints (e.g., `DromadTrader5`)
- `DromadCaravan` part removed when settling in villages
- Same restocking system with alternative base templates

### Comparison Table

| Aspect | General | Apothecary | Tinker |
|--------|---------|-----------|--------|
| Tables | 1-3 tier-based | Single template-based | Single template-based |
| Restock Chance | 100% | 100% | 100% |
| Role | "Merchant" | "Apothecary" | "Tinker" |
| Blueprint | Dynamic/Dromad | HumanApothecaryN | HumanTinkerN |
| Special | Multiple tiers | Healing items | Repair/Recharge services |

---

## 18. Restocking Mechanics

**Source**: `XRL.World.Parts/GenericInventoryRestocker.cs`

### First Trade Trigger
```csharp
public override bool HandleEvent(StartTradeEvent E)
{
    if (LastRestockTick == 0L) {
        PerformRestock(Silent: true);
        LastRestockTick = The.Game.TimeTicks;
    }
    return base.HandleEvent(E);
}
```

### Ongoing Restock (TurnTick)
```csharp
// Every turn:
long elapsed = CurrentTick - LastRestockTick;
if (elapsed >= RestockFrequency && playerInSameZone) {
    LastRestockTick = CurrentTick;
    int accumulatedChance = Chance * (int)(elapsed / RestockFrequency);
    if (accumulatedChance.in100() && !isPlayerControlled) {
        PerformRestock();
    }
}
```

**Key behaviors**:
- Only restocks while player is in the same zone
- Chance scales with time elapsed (accumulates if player hasn't visited)
- Player-controlled/former-player merchants do NOT restock
- Default: 6000 ticks (5 days) between restocks

### Hero Promotion Restock
```csharp
// On "MadeHero" event:
PerformRestock(Silent: true);  // Immediate restock with hero items
```

---

## 19. The Stock Marking System

Items are tracked with properties to manage restocking:

| Property | Meaning |
|----------|---------|
| `_stock = 1` | Current merchant stock (eligible for removal on restock) |
| `norestock = 1` | Permanent inventory (never removed) |
| `IsImportant()` | Quest/unique items (never removed) |

### Restock Cleanup Flow
```csharp
public void PerformRestock(bool Silent = false)
{
    // 1. Remove old stock
    foreach (GameObject item in inventory) {
        if (item.HasProperty("_stock") && !item.HasPropertyOrTag("norestock")
            && !item.IsImportant()) {
            inventory.RemoveObject(item);
            item.Obliterate();
        }
    }
    // 2. Generate fresh stock
    PerformStock(Restock: true, Silent: false);
}
```

### During Trade
- Items bought FROM merchant: `_stock` property removed (becomes player's item)
- Items sold TO merchant: Gets `_stock = 1` (becomes restockable merchant item)

---

## 20. Trade Initiation Flow

**Source**: `XRL.World.Conversations.Parts/Trade.cs`

### Conversation Integration
The Trade conversation part provides the `{{g|[begin trade]}}` option in dialog:

```csharp
public static bool CanTradeWith(GameObject Object)
{
    return Object != null &&
           The.Player.PhaseMatches(Object) &&
           !Object.HasTagOrProperty("NoTrade") &&
           Object.InSameOrAdjacentCellTo(The.Player);
}
```

### Requirements
- Both parties in same or adjacent cells
- Both must phase-match (same reality phase)
- Neither can have `NoTrade` tag
- Both must have an Inventory part

### StartTradeEvent
```csharp
// Broadcast when trade begins
StartTradeEvent.Send(Player, Trader, IdentifyLevel, Companion,
                     Identify, Repair, Recharge, Read);
```

Feature flags indicate trader capabilities:
- `Identify`: Can identify items (tinkering skill)
- `Repair`: Can repair broken items
- `Recharge`: Can recharge energy cells
- `Read`: Can read books (librarian)

---

## 21. Item Validation for Trade

**Source**: `XRL.UI/TradeUI.cs` (ValidForTrade)

### Items Excluded from Trade
1. **Natural items**: `Object.IsNatural()` — body parts, natural weapons
2. **Fresh water in containers**: Pure water containers (except tinkerables)
3. **Player won't sell**: `PlayerWontSell` property
4. **Merchant won't sell**: `WontSell` property (unless player-led)
5. **Tagged exclusions**: Items matching trader's `WontSellTag`
6. **Containment loops**: `MovingIntoWouldCreateContainmentLoop()`
7. **Event vetoed**: `CanBeTradedEvent.Check()` returns false

### Player-Led Companions
When the trader is a companion:
- `_costMultiple = 0` (no money changes hands)
- Items simply transfer between inventories
- Container mode activated (fewer restrictions)

---

## 22. The Trade UI

**Source**: `XRL.UI/TradeUI.cs` (1630 lines), `Qud.UI/TradeScreen.cs` (1001 lines)

Qud has two parallel UI implementations:

### Legacy Console UI
- **Left panel** (columns 0-39): Trader's inventory
- **Right panel** (columns 42-79): Player's inventory
- **Status bar** (rows 22-24): Totals, weight, commands
- Text-based with color coding

### Modern Unity UI (TradeScreen)
- Dual-panel layout with search/filtering
- Drag-and-drop support
- Category collapsing
- Async number input (type quantity while item selected)
- Sorting: A-Z or by Category

### Item Display
Items grouped by inventory category with headers. Each shows:
- Name with color codes
- Quantity (if stacked)
- Price in drams
- Weight

### Trade Totals
```csharp
// Real-time balance display
sReadout = " {{C|" + Totals[0] + "}} drams <-> {{C|" + Totals[1] + "}} drams — {{W|$" + freeDrams + "}} ";
```

### Trade Balance Calculation
```csharp
public static int CalculateTrade(double Bought, double Sold)
{
    double num = Bought - Sold;  // What player must pay
    if (num > 0.0)  num += 0.0001;  // Round up for player cost
    if (num < 0.0)  num -= 0.0001;  // Round down for player gain

    if (Math.Abs(num) < 10.0)
        return (int)Math.Ceiling(num);
    return (int)Math.Round(num, MidpointRounding.AwayFromZero);
}
```

---

## 23. Transaction Execution (PerformOffer)

**Source**: `XRL.UI/TradeUI.cs` (PerformOffer, lines 1296-1537)

### Phase 1: Pre-Trade Validation
```csharp
if (Difference > 0) {  // Player must pay
    int freeDrams = The.Player.GetFreeDrams();
    if (freeDrams < Difference)
        // Check force completion or show insufficient funds
}
int storableDrams = The.Player.GetStorableDrams("water");
if (storableDrams < returnDrams)
    // Ask about partial trade
```

### Phase 2: Deduct Payment
```csharp
if (Difference > 0)
    The.Player.UseDrams(Difference);  // Player pays
```

### Phase 3: Item Transfer
```csharp
// From trader to player
foreach (selected item in trader inventory) {
    item.SplitStack(selectedQuantity, Trader);
    TryRemove(item, Trader);  // CommandRemoveObject event
    item.RemoveIntProperty("_stock");  // No longer merchant stock
    toPlayer.Add(item);
}

// From player to trader
foreach (selected item in player inventory) {
    item.SplitStack(selectedQuantity, Player);
    TryRemove(item, Player);
    toTrader.Add(item);
}
```

### Phase 4: Receive Items
```csharp
// Player receives
foreach (var item in toPlayer) {
    if (!The.Player.TakeObject(item))
        Trader.ReceiveObject(item);  // Fallback: put back with trader
}
// Trader receives
foreach (var item in toTrader)
    Trader.TakeObject(item);
```

### Phase 5: Currency Exchange
```csharp
if (Difference < 0) {       // Trader pays player
    The.Player.GiveDrams(-Difference);
    Trader.UseDrams(-Difference);
} else if (Difference > 0) { // Already paid in Phase 2
    Trader.GiveDrams(Difference);
}
```

### Return Statuses
| Status | Meaning |
|--------|---------|
| `TOP` | Trade complete, refresh inventory |
| `REFRESH` | Something changed, refetch |
| `NEXT` | User declined, stay in trade |
| `CLOSE` | Exit trade UI |

---

## 24. Vendor Services (Identify, Repair, Recharge)

**Source**: `XRL.UI/TradeUI.cs` (ShowVendorActions)

### Identify Service
```csharp
int complexity = GO.GetComplexity();
int difficulty = GO.GetExamineDifficulty();
float cost = Math.Max(2.0, -0.0667 + 1.24 * (complexity + difficulty) + ...);

if (The.Player.UseDrams((int)cost)) {
    Trader.GiveDrams((int)cost);
    GO.MakeUnderstood();
}
```
Requires trader with `Identify` capability (tinkering skill).

### Repair Service
```csharp
int cost = Math.Max(5 + (int)(GetValue(GO) / 25.0), 5) * GO.Count;
if (confirmed) {
    The.Player.UseDrams(cost);
    Trader.GiveDrams(cost);
    RepairedEvent.Send(Trader, GO, null, damagedPart);
}
```
Cost scales with item value. Minimum 5 drams.

### Recharge Service
```csharp
int rechargeAmount = energyPart.GetRechargeAmount();
int cost = Math.Max(rechargeAmount / 500, 1);

if (The.Player.UseDrams(cost)) {
    Trader.GiveDrams(cost);
    energyPart.AddCharge(rechargeAmount);
}
```
Cost = max(chargeNeeded / 500, 1). Very cheap for small recharges.

### Available Services

| Service | Requirement | Cost |
|---------|------------|------|
| Look | Always | Free |
| Identify | Tinkering_Identify | Complexity-based |
| Repair | Tinkering_Repair | Value/25 + 5 |
| Recharge | Tinkering_Tinker1 | Charge/500 |
| Read | Librarian property | Free |

---

## 25. Debt & Credit System

**Source**: `XRL.UI/TradeUI.cs` (lines 381-408)

If a player can't pay the full difference:

### Creating Debt
```csharp
int owed = Difference - freeDrams;
Trader.ModIntProperty("TraderCreditExtended", owed);
// "You now owe [Trader] [owed] drams of fresh water."
```

### Debt Collection on Next Trade
```csharp
int debt = _Trader.GetIntProperty("TraderCreditExtended");
if (debt > 0) {
    int freeDrams = The.Player.GetFreeDrams();
    if (freeDrams <= 0) {
        // "[Trader] will not trade with you until you pay [debt] drams."
        return;  // Block trade
    }
    if (freeDrams < debt && playerConfirmsPartialPayment) {
        debt -= freeDrams;
        The.Player.UseDrams(freeDrams);
        _Trader.SetIntProperty("TraderCreditExtended", debt);
    }
}
```

Debt blocks all future trades until paid. Partial payment reduces the debt.

---

## 26. NPC Shopping AI

**Source**: `XRL.World.Parts/AIShopper.cs`

NPCs can shop at merchants via the brain goal system:

```csharp
public override bool HandleEvent(AIBoredEvent E)
{
    if (ShouldGoShopping() && 25.in100())  // 25% chance when bored
    {
        ParentObject.Brain.PushGoal(new GoOnAShoppingSpree());
        return false;
    }
    return base.HandleEvent(E);
}
```

This creates a living economy where NPCs visit merchants and make purchases, depleting/changing merchant stock independently of the player.

---

## 27. Trade Events Reference

| Event | File | Purpose |
|-------|------|---------|
| `CanTradeEvent` | `XRL.World/CanTradeEvent.cs` | Check if trade is allowed between two entities |
| `CanBeTradedEvent` | `XRL.World/CanBeTradedEvent.cs` | Validate individual item for trading |
| `StartTradeEvent` | `XRL.World/StartTradeEvent.cs` | Broadcast trade initiation; triggers first restock |
| `GetTradePerformanceEvent` | `XRL.World/GetTradePerformanceEvent.cs` | Compute price multiplier from Ego |
| `AllowTradeWithNoInventoryEvent` | `XRL.World/AllowTradeWithNoInventoryEvent.cs` | Permit empty-inventory merchant trading |
| `GetIntrinsicValueEvent` | `XRL.World/GetIntrinsicValueEvent.cs` | Stage 1: Base value modifications |
| `AdjustValueEvent` | `XRL.World/AdjustValueEvent.cs` | Stage 2: Multiplicative penalties |
| `GetExtrinsicValueEvent` | `XRL.World/GetExtrinsicValueEvent.cs` | Stage 3: Add contained values |
| `GetFreeDramsEvent` | `XRL.World/GetFreeDramsEvent.cs` | Query available water currency |
| `UseDramsEvent` | `XRL.World/UseDramsEvent.cs` | Deduct water (3-pass cascade) |
| `GiveDramsEvent` | `XRL.World/GiveDramsEvent.cs` | Add water (5-pass cascade) |
| `GetStorableDramsEvent` | `XRL.World/GetStorableDramsEvent.cs` | Query container capacity |
| `StockedEvent` | (referenced) | Fired after merchant inventory populated |
| `RepairedEvent` | (referenced) | Fired after item repaired |
| `CommandRemoveObject` | (referenced) | Attempt to remove item from inventory |
| `BeforeContentsTaken` | (referenced) | Container pre-removal hook |
| `AfterContentsTaken` | (referenced) | Container post-removal hook |

---

## 28. Key Source Files

### Trade UI
| File | Description |
|------|-------------|
| `XRL.UI/TradeUI.cs` (1630 lines) | Main trade logic, price calculation, transaction execution |
| `Qud.UI/TradeScreen.cs` (1001 lines) | Modern Unity trade UI |
| `Qud.UI/TradeLine.cs` (631 lines) | Individual item/category line in trade UI |
| `XRL.UI/TradeEntry.cs` | Item/category data structure for trade lists |

### Merchant Parts
| File | Description |
|------|-------------|
| `XRL.World.Parts/GenericInventoryRestocker.cs` | Inventory generation and restocking |
| `XRL.World.Parts/Commerce.cs` | Base item value storage |
| `XRL.World.Parts/MerchantIconColor.cs` | Merchant visual indicator |
| `XRL.World.Parts/MerchantRevealer.cs` | Merchant location books |
| `XRL.World.Parts/AIShopper.cs` | NPC shopping behavior |
| `XRL.World.Parts/Examiner.cs` | Identification state affecting value |

### Value System
| File | Description |
|------|-------------|
| `XRL.World/GetIntrinsicValueEvent.cs` | Stage 1 value event |
| `XRL.World/AdjustValueEvent.cs` | Stage 2 multiplicative adjustment |
| `XRL.World/GetExtrinsicValueEvent.cs` | Stage 3 additive (contents) |
| `XRL.World/IValueEvent.cs` | Base value event interface |
| `XRL.World/ModEntry.cs` | Mod value definitions |
| `XRL.World/ModificationFactory.cs` | Mod rarity weights |

### Currency
| File | Description |
|------|-------------|
| `XRL.Liquids/LiquidWater.cs` | Water definition, value per dram |
| `XRL.World.Parts/LiquidVolume.cs` | Liquid container part |
| `XRL.World/GetFreeDramsEvent.cs` | Query available drams |
| `XRL.World/UseDramsEvent.cs` | Deduct drams (3-pass) |
| `XRL.World/GiveDramsEvent.cs` | Add drams (5-pass) |
| `XRL.World/GetStorableDramsEvent.cs` | Query storage capacity |

### Haggling
| File | Description |
|------|-------------|
| `XRL.World/HagglingSifrah.cs` | Sifrah haggling minigame |
| `XRL.World/GetTradePerformanceEvent.cs` | Ego-based performance formula |

### Conversations
| File | Description |
|------|-------------|
| `XRL.World.Conversations.Parts/Trade.cs` | Trade dialog option |
| `XRL.World.Conversations.Parts/WaterRitual.cs` | Water ritual integration |

### Village Generation
| File | Description |
|------|-------------|
| `XRL.World.ZoneBuilders/VillageCoda.cs` | Merchant creation in villages |
