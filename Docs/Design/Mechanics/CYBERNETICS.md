# Cybernetics System - Deep Dive

> Source analysis of Caves of Qud's cybernetics, implant slots, license points, becoming nooks, credit wedges, skillsofts, and compute power systems from decompiled C# source code.

---

## Table of Contents

1. [System Overview](#system-overview)
2. [Implant Slots & Body Part Mapping](#implant-slots--body-part-mapping)
3. [License Points & Tiers](#license-points--tiers)
4. [Credit Wedges](#credit-wedges)
5. [Becoming Nooks (Cybernetics Terminals)](#becoming-nooks-cybernetics-terminals)
6. [Installation & Uninstallation Process](#installation--uninstallation-process)
7. [Skillsofts](#skillsofts)
8. [Compute Power System](#compute-power-system)
9. [Biodynamic Power Plant](#biodynamic-power-plant)
10. [Complete Implant Catalog](#complete-implant-catalog)
11. [Character Creation (True Kin)](#character-creation-true-kin)
12. [NPC Cybernetics](#npc-cybernetics)
13. [Cybernetic Rejection Syndrome](#cybernetic-rejection-syndrome)
14. [Cursed Cybernetics](#cursed-cybernetics)
15. [Hacking Terminals](#hacking-terminals)
16. [Key Events & Extension Points](#key-events--extension-points)
17. [Terminal UI Flow](#terminal-ui-flow)

---

## System Overview

Cybernetics is the augmentation system exclusive to **True Kin** characters. It allows installing bionic implants into body part slots, powered by an internal license point budget and an economy of credit wedges. The system is deeply integrated with the body part anatomy system, the power/energy system, and a "compute power" scaling mechanic.

### Core Loop

```
1. EARN LICENSE POINTS (chargen genotype + subtype, credit wedge upgrades, hacking)
2. FIND IMPLANTS (loot, purchase, character creation)
3. VISIT BECOMING NOOK (cybernetics terminal in ruins)
4. INSTALL IMPLANTS (spend license points, select body part slot)
5. BENEFIT FROM COMPUTE POWER (implants scale with local lattice compute power)
```

### Core Source Files

| System | Primary Source |
|--------|---------------|
| Base implant class | `XRL.World.Parts/CyberneticsBaseItem.cs` |
| Body part integration | `XRL.World.Anatomy/BodyPart.cs` |
| Terminal controller | `XRL.UI/CyberneticsTerminal.cs` |
| Terminal part | `XRL.World.Parts/CyberneticsTerminal2.cs` |
| Credit wedges | `XRL.World.Parts/CyberneticsCreditWedge.cs` |
| Skillsofts | `CyberneticsSingleSkillsoft.cs`, `CyberneticsTreeSkillsoft.cs`, `CyberneticsSchemasoft.cs` |
| Compute power | `XRL.World/GetAvailableComputePowerEvent.cs`, `XRL.World.Parts/ComputeNode.cs` |
| Chargen module | `XRL.CharacterBuilds.Qud/QudCyberneticsModule.cs` |
| NPC implants | `CyberneticsHasImplants.cs`, `CyberneticsHasRandomImplants.cs` |
| Rejection syndrome | `XRL.World.Effects/CyberneticRejectionSyndrome.cs` |

---

## Implant Slots & Body Part Mapping

Implants are installed into **body part slots**. Each implant declares which body part types it's compatible with via the `Slots` field (comma-separated list). The body part system then manages the physical attachment.

### Body Part Implant Fields

```csharp
// In BodyPart class:
public GameObject _Cybernetics;        // The installed implant (property: Cybernetics)
public bool FirstSlotForCybernetics;   // Flag: FLAG_FIRST_CYBERNETICS = 32768
```

### Implantability Rules

```csharp
public bool CanReceiveCyberneticImplant()
{
    if (Extrinsic) return false;     // Extrinsic (added) parts cannot receive implants
    if (Category != 1) return false;  // Only Category 1 (primary) parts are implantable
    return true;
}
```

**Category 1** = primary anatomical parts (head, body, arms, hands, feet, back, face). Temporary, summoned, or equipment-granted body parts cannot receive implants.

### Standard Implantable Slot Types

| Slot Type | Description | Typical Count |
|-----------|-------------|---------------|
| `Head` | Cranium | 1 |
| `Face` | Facial features | 1 |
| `Body` | Torso | 1 |
| `Back` | Upper back | 1 |
| `Arm` | Upper limbs | 2 (left/right) |
| `Hand` / `Hands` | Lower limbs / grip | 2 (left/right) |
| `Feet` | Lower extremities | 1 |

Implants declare compatibility like: `Slots = "Head,Face"` meaning the implant can go in either a Head or Face slot.

### Equipment Slot Interaction

Some implants have the `CyberneticsUsesEqSlot` tag. When installed, they occupy the equipment slot for that body part — meaning the player cannot wear armor/equipment in that slot while the implant is active. The system automatically unequips any existing equipment when such an implant is installed.

### Multi-Slot Implants

An implant can span multiple body parts by listing multiple slot types. The system finds the first compatible, unoccupied body part of any listed type. If a body part already has an implant, the new one replaces it (the old one is uninstalled).

---

## License Points & Tiers

License points are the primary budget for cybernetics. Every implant has a `Cost` in license points, and the total installed cost cannot exceed total available points.

### Properties

| Property | Description |
|----------|-------------|
| `CyberneticsLicenses` | Total license tier (base + earned + free) |
| `FreeCyberneticsLicenses` | Bonus licenses from hacking (don't count toward upgrade cost tiers) |
| `LicensesUsed` | Sum of all installed implant costs (calculated at runtime) |
| `LicensesRemaining` | `Licenses - LicensesUsed` |

### How License Points Are Acquired

1. **Character Creation** — Genotype provides base points, subtype adds to them:
   ```csharp
   // Genotype contribution:
   gameObject.ModIntProperty("CyberneticsLicenses", genotypeEntry.CyberneticsLicensePoints);
   // Subtype contribution:
   gameObject.ModIntProperty("CyberneticsLicenses", subtypeEntry.CyberneticsLicensePoints);
   ```

2. **Credit Wedge Upgrades** — Spend credits at a becoming nook to increase license tier by 1

3. **Hacking** — Successfully hacking a becoming nook can grant bonus license points:
   - Normal hack success: +1 point (30% chance for each additional, up to ~4)
   - Exceptional hack success: +2 base (50% chance for each additional, up to ~5)
   - These are added as `FreeCyberneticsLicenses`

### License Upgrade Cost Scaling

The credit cost to upgrade license tier increases based on how many base (non-free) licenses you have:

| Base License Tier | Credit Cost Per Upgrade |
|-------------------|------------------------|
| 0-7 | 1 credit |
| 8-15 | 2 credits |
| 16-23 | 3 credits |
| 24+ | 4 credits |

```csharp
int baseLicenses = Licenses - FreeLicenses;
if (baseLicenses < 8)  return 1;
if (baseLicenses < 16) return 2;
if (baseLicenses < 24) return 3;
return 4;
```

---

## Credit Wedges

Credit wedges (`CyberneticsCreditWedge`) are physical currency items used to upgrade license tiers at becoming nooks.

### Implementation

```csharp
public class CyberneticsCreditWedge : IPart
{
    public int Credits;  // How many credits this wedge is worth
}
```

- **Stackable**: Multiple wedges can stack; total value = `Credits × Count`
- **Partial Consumption**: When upgrading costs less than a wedge's full value, the wedge is split and the remainder stays
- **Display**: Shows credit count in cyan: `"{{C|N\u009b}}"`

### Consumption Algorithm

When upgrading license tier at a terminal:

```
1. Iterate through all credit wedges in inventory
2. For each wedge:
   a. If wedge value × stack count > remaining cost:
      - Destroy individual wedge items until cost paid
      - If fractional cost remains, split stack of 1, reduce Credits by remainder
   b. If wedge value × stack count <= remaining cost:
      - Obliterate entire stack, subtract from remaining cost
3. Increment CyberneticsLicenses by 1
```

---

## Becoming Nooks (Cybernetics Terminals)

Becoming nooks are the physical terminals where True Kin interact with the cybernetics system. They are found in technological ruins throughout the world.

### Terminal Part (`CyberneticsTerminal2`)

```csharp
public class CyberneticsTerminal2 : IPoweredPart, IHackingSifrahHandler
{
    public static readonly string COMMAND_NAME = "InterfaceWithBecomingNook";

    // Power requirement
    ChargeUse = 100;
    WorksOnSelf = true;
    NameForStatus = "BecomingNookInterface";
}
```

- Requires **power** (ChargeUse = 100) to function
- Interaction command: `"InterfaceWithBecomingNook"`

### Authorization

```csharp
public bool IsAuthorized(GameObject Object)
{
    if (HackActive) return true;       // Hacked terminal allows anyone
    return Object?.IsTrueKin() ?? false; // Only True Kin by default
}
```

Only **True Kin** can use becoming nooks normally. Mutants must **hack** the terminal first.

### Terminal State

The terminal controller tracks:

```csharp
public class CyberneticsTerminal
{
    public List<CyberneticsCreditWedge> Wedges;  // Credit items in inventory
    public List<GameObject> Implants;             // Available uninstalled implants
    public int Licenses;                          // Total license points
    public int FreeLicenses;                      // Bonus (hacking) points
    public int LicensesUsed;                      // Currently spent points
    public int Credits;                           // Total credit wedge value
    public int LicensesRemaining => Licenses - LicensesUsed;

    public GameObject Terminal;    // The terminal object
    public GameObject Subject;     // Creature being modified
    public GameObject Actor;       // Who is using it
    public int HackLevel;          // Hacking progress
    public int SecurityAlertLevel; // Failed hack accumulation
    public bool HackActive => HackLevel > SecurityAlertLevel;
    public bool LowLevelHack;     // Stealth hack mode
}
```

### Implant Discovery

The terminal scans for available implants from:
1. Adjacent cells — checks containers for implant objects
2. Subject's inventory — checks for carried implants
3. Only shows **understood** implants to the player

### Lore & Flavor

The terminal addresses the player as "Aristocrat" and frames cybernetics as a path toward the "Grand Unification":

> *"Welcome, Aristocrat, to a becoming nook. You are one step closer to the Grand Unification. Please choose from the following options."*

> *"Cybernetics are bionic augmentations implanted in your body to assist in your self-actualization. You can have implants installed at becoming nooks such as this one. Either load them in the rack or carry them on your person."*

**Terminal variants found in the world:**
- Standard becoming nooks (in ruins)
- "Recoming nook at Gyl" (advanced/endgame variant)
- "Reshaping Nook" (Court of the Sultans variant)

---

## Installation & Uninstallation Process

### Installation Flow

```
1. Player selects implant from CyberneticsScreenInstall
   → Condition check: not broken, rusted, EMPed, or temporary
   → License cost check: Cost <= LicensesRemaining
   → OneOnly check: CyberneticsOneOnly tag blocks duplicates

2. Player selects target body part from CyberneticsScreenInstallLocation
   → Must be in implant's Slots list
   → Must pass CanReceiveCyberneticImplant() (Category 1, not Extrinsic)
   → Shows replacement warning if slot already occupied

3. CyberneticsInstallResult animation plays
   → "Interfacing with nervous system..."

4. On TextComplete():
   a. If CyberneticsUsesEqSlot: unequip current equipment
   b. If replacing existing implant: unimplant/destroy old one
   c. Remove implant from context (inventory/container)
   d. Mark as understood
   e. Call bodyPart.Implant(implant)

5. bodyPart.Implant(implant):
   a. Set _Cybernetics = implant
   b. If CyberneticsUsesEqSlot: Equip(implant) into body part
   c. Fire ImplantedEvent
   d. Fire ImplantAddedEvent
   e. RegenerateDefaultEquipment()
   f. RecalculateFirstCybernetics()
   g. Log achievement if player

6. CyberneticsBaseItem.HandleEvent(ImplantedEvent):
   a. Set ImplantedOn reference
   b. Set CannotEquip, CannotDrop, NoRemoveOptionInInventory properties
   c. Check cybernetic rejection syndrome (mutants only)
```

### Uninstallation Flow

```
1. Player selects installed implant from CyberneticsScreenRemove
   → Check: not CyberneticsNoRemove tag/property

2. If CyberneticsDestroyOnRemoval tag:
   → Unimplant(MoveToInventory: false)
   → Destroy the implant object
   → "[destroyed on uninstall]" warning shown

3. Otherwise:
   → Unimplant(MoveToInventory: true)
   → Implant moved to subject's inventory

4. bodyPart.Unimplant():
   a. Fire UnimplantedEvent
   b. Fire ImplantRemovedEvent
   c. If was also equipped: clear _Equipped
   d. If MoveToInventory: add to inventory
   e. Set _Cybernetics = null
   f. Flush transient caches
```

### Implant Tags

| Tag | Effect |
|-----|--------|
| `CyberneticsOneOnly` | Can only install one of this blueprint on a creature |
| `CyberneticsDestroyOnRemoval` | Implant destroyed when uninstalled (not moved to inventory) |
| `CyberneticsUsesEqSlot` | Occupies equipment slot (must unequip worn item) |
| `CyberneticsNoRemove` | Cannot be uninstalled at all |
| `CyberneticsNoDisplay` | Hidden from terminal display |
| `CyberneticsModifiesAnatomy` | Implant changes body structure |
| `CyberneticsPreferredSlots` | Preferred installation locations (comma-separated) |

### Properties Set on Installation

```csharp
ParentObject.SetIntProperty("CannotEquip", 1);          // Can't equip as gear
ParentObject.SetIntProperty("CannotDrop", 1);            // Can't drop
ParentObject.SetIntProperty("NoRemoveOptionInInventory", 1); // No remove UI option
```

---

## Skillsofts

Skillsofts are cybernetic implants that grant skills, powers, and schematics. There are three types.

### Single Skillsoft (`CyberneticsSingleSkillsoft`)

Grants **one skill or power** to the implantee.

**Cost calculation:**
```csharp
// Randomly selects from available powers within cost range
MinCost..MaxCost (default max 50)
// Cost set on parent CyberneticsBaseItem
SkillFactory.GetPowerPool(MinCost, MaxCost)
```

**Mechanics:**
- On implant: Adds skill if implantee doesn't already have it
- Sets a dependency property to track that this skillsoft provides the skill
- On unimplant: Removes skill **only if** no other skillsofts are also providing it
- Tracked via `TrackingKey()` and `DependencyKey()` for safe multi-source skill management

**Example:** A single skillsoft might grant "Long Blade Proficiency" or "Cooking and Gathering" — one specific power entry from the skill factory.

### Tree Skillsoft (`CyberneticsTreeSkillsoft`)

Grants access to an **entire skill tree** (the base skill plus all powers in that tree).

**Cost calculation:**
```csharp
// Base skill cost + sum of all power costs in tree, divided by 100
int totalCost = skillCost;
foreach (power in tree)
    totalCost += power.Cost;
implantCost = totalCost / 100 + AddCost;
```

**Mechanics:**
- On implant: Adds ALL skills in the selected skill tree
- Tracks each power individually via unique tracking/dependency keys
- On unimplant: Removes powers only provided by this specific skillsoft
- Supports `AddOn` mode to append to description
- Random skill selection via `SkillFactory.GetSkillPool()`

**Example:** A tree skillsoft for "Axes" would grant Axe Proficiency, Cleave, Dismember, Berserk, and all other powers in the Axes tree.

### Schemasoft (`CyberneticsSchemasoft`)

Grants access to **crafting schematics** for a specific tinkering category.

**Categories available:**
- Ammo, Pistols, Rifles, Melee Weapons, Grenades, Tonics, Utility, Armor, Heavy Weapons

**Mechanics:**
- Randomly selects a category on creation
- On implant: Unlocks all schematics up to `MaxTier` (default 3) for that category
- Stores learned recipe blueprints in `RecipesAdded` list for clean removal
- On unimplant: Unlearns all previously added recipes

**Tier display:**
| MaxTier | Display |
|---------|---------|
| 0-3 | Low |
| 4-5 | Mid |
| 6+ | High |

---

## Compute Power System

Compute power is a scaling mechanic that enhances cybernetic implant effectiveness. It operates on a "local lattice" — the character's personal technology network.

### How Compute Power Works

The `GetAvailableComputePowerEvent` aggregates compute power from all sources on an actor. Implants then use this value to scale their effects.

### Scaling Formulas

**AdjustUp (increase values with compute power):**
```
AdjustUp(Amount) = Amount × (100 + ComputePower) / 100
AdjustUp(Amount, Factor) = Amount × (100 + ComputePower × Factor) / 100
```

Example: Base range 10, compute power 20 → `10 × 120 / 100 = 12`

**AdjustDown (reduce cooldowns/costs with compute power):**
```
AdjustDown(Amount) = max(Amount × (100 - ComputePower) / 100, Amount / FloorDivisor)
```

Default `FloorDivisor = 2`, so cooldowns can never drop below 50% of base value.

Example: Base cooldown 200, compute power 20 → `max(200 × 80 / 100, 200 / 2) = max(160, 100) = 160`

### Compute Power Sources

| Source | Base Power | Scaling |
|--------|-----------|---------|
| **ComputeNode** (equippable part) | 20 | Power load sensitive: `+(powerLoad - 100) / 10`% at overload |
| **ModCoProcessor** (head armor mod) | `Tier × 2.5` | Power load bonus × Factor |
| **Palladium Electrodeposits** (implant) | 20 (fixed) | None |

**ComputeNode at overload:**
Standard overload (400% power load) → `20 × 1.30 = 26` compute power

**ModCoProcessor example:**
Tier 3 at 400% load with +3 power load bonus → `(3 + 3) × 2.5 = 15` compute power. Also grants Intelligence bonus via `AttributeBonusFactor = 0.25`.

### Implants That Scale With Compute Power

| Implant | What Scales | Base → With 20 CP |
|---------|------------|-------------------|
| Electromagnetic Sensor | Detection radius | 9 → 10 |
| Penetrating Radar | Radar radius | 10 → 12 |
| Stasis Entangler | Cooldown | 200 → 160 turns |
| Stasis Projector | Coverage, duration, cooldown | All scale |
| Automated Defibrillator | Activation chance | 50% → 60% |
| Micromanipulator Array | Tinkering cost reduction | 40% → 50% |
| Nocturnal Apex | Agility, speed, duration | All scale up |
| Matter Recompositer | Cooldown | 100 → 80 turns |
| High-Fidelity Matter Recompositer | Cooldown | 50 → 40 turns |
| Onboard Recoiler Teleporter | Cooldown | 50 → 40 turns |
| Communications Interlock | Rebuke bonus | 5 → 6 |
| Social Coprocessor | Reputation bonus, cost reduction, proselytize limit | All scale |
| Anomaly Fumigator | Gas density | 10-120 → scaled up |
| Inflatable Axons | Quickness bonus, duration | 40 → 48 quickness |

---

## Biodynamic Power Plant

The `CyberneticsBiodynamicPowerPlant` is a special implant that generates internal power for other cybernetic implants.

### Mechanics

```csharp
public int ChargeRate = 5000;  // Charge generated per turn
```

- Generates **5000 charge units per turn** from biological energy
- Charge resets at the start of each turn, then refills: `Charge = 0; AddCharge(ChargeRate);`
- Does not require external power source
- Can be disabled by EMP, breakage, rust, or reality distortion
- Shows in equipment descriptions: *"When equipped, [device] can be powered by [your Biodynamic Power Plant]."*

### Power Event Integration

Registers for the full power query chain:
- `QueryCharge` — Reports available charge
- `TestCharge` — Tests if sufficient charge exists
- `UseCharge` — Allows consumption of generated charge
- `QueryChargeProduction` — Reports 5000 as production rate
- `HasPowerConnectors` — Declares itself as a power source

---

## Complete Implant Catalog

### Stat Modifiers & Passive Effects

| Implant | Effect | Details |
|---------|--------|---------|
| **CyberneticsStatModifier** | Modifies base stats | Format: `"Stat:amount;Stat2:amount2"` |
| **CyberneticsPropertyModifier** | Modifies object properties | Applies integer property changes |
| **CyberneticsPalladiumElectrodeposits** | +2 Intelligence, +20 compute power | Head slot only |
| **CyberneticsTonicDurationModifier** | Extends tonic durations | Powered, percentage-based |

### Vision & Sensing

| Implant | Effect | Power | Details |
|---------|--------|-------|---------|
| **CyberneticsNightVision** | Darkvision (40 radius) | None | Toggleable |
| **CyberneticsPenetratingRadar** | Radar vision | Powered | Radius 10, scales with CP |
| **CyberneticsOpticalMultiscanner** | Reveals HP, armor, DV values | None | Passive |
| **CyberneticsElectromagneticSensor** | Detects robots | Powered | Range 9, scales with CP |
| **CyberneticsAirCurrentMicrosensor** | Reveals stairs on zone entry | None | Auto-triggers |
| **CyberneticsBiologicalIndexer** | Bio-scanner equipped | None | Sets `BioScannerEquipped` |
| **CyberneticsTechIndexer** | Tech-scanner equipped | None | Sets `TechScannerEquipped` |

### Movement & Mobility

| Implant | Effect | Details |
|---------|--------|---------|
| **CyberneticsMotorizedTreads** | Replaces legs with treads | Anatomy modification, +100 carry capacity |
| **CyberneticsTibularHydrojets** | +200 swim speed | Removes water movement penalty |
| **CyberneticsCathedra** | Flight system | Toggleable, level 10, 6% fall chance/turn, +100 carry |
| **CyberneticsCathedraBlackOpal** | Wormhole teleportation flight | Variant activation |
| **CyberneticsCathedraRuby** | Flight variant | Specialized cathedra |
| **CyberneticsCathdraSapphire** | Flight variant | Specialized cathedra |
| **CyberneticsCathedraWhiteOpal** | Flight variant | Specialized cathedra |
| **CyberneticsSyncJump** | Syncs jump with acrobatics | Passive |

### Combat & Weapons

| Implant | Effect | Details |
|---------|--------|---------|
| **CyberneticsFistReplacement** | Replaces hand weapon | Anatomy mod, custom `FistObject` blueprint |
| **CyberneticsPrecisionForceLathe** | Creates Force Knife | Powered, reality distortion required |
| **CyberneticsGunRack** | Adds 2 hardpoint body parts | Anatomy modification |
| **CyberneticsEquipmentRack** | Adds equipment slot to back | Anatomy modification |
| **CyberneticsGraftedMirrorArm** | Adds thrown weapon body part | Anatomy modification |
| **CyberneticsInflatableAxons** | +40 quickness for 10 turns | Powered, cooldown 100, penalty after |

### Defense

| Implant | Effect | Details |
|---------|--------|---------|
| **CyberneticsReactiveCranialPlating** | Immunity to daze and stun | Head, passive |
| **CyberneticsForcefieldNullifier** | Projectiles pass through forcefields | Passive |
| **CyberneticsAnchorSpikes** | Prevents involuntary movement | Toggleable, blocks knockback/push |
| **CyberneticsEffectSuppressor** | Prevents specific status effects | Powered, configurable effect list |
| **CyberneticsFireSuppressionSystem** | Sprays cooling gel when on fire | Toggleable |
| **CyberneticsBionicLiver** | Poison and disease immunity | Passive, blocks all poison/disease |

### Teleportation & Phase

| Implant | Effect | Details |
|---------|--------|---------|
| **CyberneticsHighFidelityMatterRecompositer** | Guided teleportation | Powered, cooldown 50, AI usable |
| **CyberneticsMatterRecompositer** | Emergency random teleport | Powered, cooldown 100, explored cells only |
| **CyberneticsOnboardRecoilerImprinting** | Imprint current location | Powered, cooldown 100 per imprint |
| **CyberneticsOnboardRecoilerTeleporter** | Teleport to imprinted location | Powered, cooldown 50 |
| **CyberneticsPhaseHarmonicModulator** | Omniphase toggle | Grants `Omniphase` effect |
| **CyberneticsPhaseAdaptiveScope** | Phase-shifts projectiles | Powered, syncs to target phase |
| **CyberneticsStasisProjector** | Creates stasis fields around target | Powered, coverage 6, duration 6-8 |
| **CyberneticsStasisEntangler** | Stasis on all creatures except target | Powered, cooldown 200, duration 15 |

### Medical

| Implant | Effect | Details |
|---------|--------|---------|
| **CyberneticsMedassistModule** | Auto-injects tonics on damage | Powered, capacity 8 tonics, utility scoring |
| **CyberneticsAutomatedInternalDefibrillator** | Removes cardiac arrest | Powered, 50% chance (scales with CP) |

### Social

| Implant | Effect | Details |
|---------|--------|---------|
| **CyberneticsCommunicationsInterlock** | Rebuke Robot skill (+5 bonus) | Scales with CP |
| **CyberneticsSocialCoprocessor** | Water ritual & proselytize bonuses | Powered, reputation +half base unit |
| **CyberneticsCustomVisage** | +300 reputation with chosen faction | One-time faction selection |
| **CyberneticsHolographicVisage** | +200 reputation, changeable | Activated, 1000 energy per use |

### Power Generation

| Implant | Effect | Details |
|---------|--------|---------|
| **CyberneticsBiodynamicPowerPlant** | Generates 5000 charge/turn | Internal power for other implants |

### Skillsofts

| Implant | Effect | Details |
|---------|--------|---------|
| **CyberneticsSingleSkillsoft** | Grants one skill/power | Variable cost based on power |
| **CyberneticsTreeSkillsoft** | Grants entire skill tree | Cost = `(skillCost + Σ powerCosts) / 100` |
| **CyberneticsSchemasoft** | Grants crafting schematics | Category-specific, up to MaxTier |

### Miscellaneous

| Implant | Effect | Details |
|---------|--------|---------|
| **CyberneticsMicromanipulatorArray** | Combat tinkering, energy cost reduction | Powered, 40-60% reduction |
| **CyberneticsNocturnalApex** | Night prowl mode (+6 AGI, +10 speed) | Duration 100, once per night |
| **CyberneticsAnomalyFumigator** | Generates normality gas | Powered, toggleable |
| **CyberneticsRefractLight** | Reflects light-based attacks | Configurable chance and angle |
| **CyberneticsMagneticCore** | Adds floating nearby body part | Anatomy modification |

---

## Character Creation (True Kin)

True Kin characters select their starting cybernetic during character creation via the `QudCyberneticsModule`.

### Module Activation

The cybernetics selection module is only enabled when:
1. Genotype has `CyberneticsLicensePoints > 0`
2. A subtype has been selected

### Available Starting Implants

Starting implants are discovered from blueprints tagged with:
- `"StartingCybernetic:General"` — Available to all True Kin
- `"StartingCybernetic:[SubtypeName]"` — Subtype-specific options (e.g., `"StartingCybernetic:Praetorian"`)
- `"StartingCybernetic:[GenotypeName]"` — Genotype-specific options

Each implant is offered with all its compatible body part slots, so the player also chooses **where** to install it.

### The "No Implant" Option

Players can reject the starting implant:
- **Penalty**: -2 License Tier (reduces `CyberneticsLicenses` by 2, down to minimum 0)
- **Benefit**: +1 Toughness stat
- Description: `"{{C|-2 License Tier\n+1 Toughness}}"`

### Boot Event (Installation)

```csharp
// On game start:
if (selectedCybernetic == null)
{
    // No implant chosen
    gameObject.SetIntProperty("CyberneticsLicenses", 0);
    gameObject.Statistics["Toughness"].BaseValue++;
}
else
{
    // Install selected implant
    List<BodyPart> parts = gameObject.Body.GetPart(selectedSlotType);
    if (parts.Count > 0)
    {
        GameObject implant = GameObject.Create(selectedBlueprint);
        implant.MakeUnderstood();
        parts.GetRandomElement().Implant(implant);
    }
}
```

### License Point Sources in Chargen

```
Total Starting Licenses = GenotypeEntry.CyberneticsLicensePoints
                        + SubtypeEntry.CyberneticsLicensePoints
                        - 2 (if no implant chosen)
```

---

## NPC Cybernetics

NPCs can receive cybernetics through two systems.

### Predefined Implants (`CyberneticsHasImplants`)

Installs specific implants on object creation:

```csharp
// Format: "BlueprintName@BodyPartType,BlueprintName@BodyPartType"
public string Implants = "";

// On ObjectCreatedEvent:
string[] implants = Implants.Split(',');
foreach (string implant in implants)
{
    string[] parts = implant.Split('@');
    GameObject cyber = GameObject.Create(parts[0]);   // Blueprint
    BodyPart part = body.GetPartByNameWithoutCybernetics(parts[1]); // Slot
    if (part != null)
        part.Implant(cyber);
    else
        cyber.Obliterate(); // Failed
}
```

### Random Implants (`CyberneticsHasRandomImplants`)

Generates random implants from a population table:

```csharp
public string ImplantTable = "Implants_1and2Pointers";  // Population table
public string ImplantChance = "100";                      // Percentage
public string LicensesAtLeast = "1";                      // Minimum licenses
public string Adjective = "{{implanted|implanted}}";      // Display adjective
public bool ChangeColor = true;                           // Change to cyan
```

**Algorithm:**
1. Roll `ImplantChance` — skip if failed
2. Ensure minimum `CyberneticsLicenses`
3. Try up to 30 times to install implants:
   a. Roll random implant from `ImplantTable` population
   b. Get compatible body part slots
   c. Shuffle slots, prioritize preferred slots
   d. Find first unoccupied compatible part
   e. Check license budget: `remaining >= implant.Cost`
   f. Install if affordable, skip if not
4. If any implants installed and `ChangeColor`: change NPC display to cyan

### Programmatic Installation (`GameObjectCyberneticsUnit`)

Used by character templates and construction systems:

```csharp
public class GameObjectCyberneticsUnit : GameObjectUnit
{
    public string Blueprint;          // Implant blueprint
    public string Slot;               // Preferred slot
    public bool Removable = true;     // Can be uninstalled

    public GameObject Implant(GameObject Object)
    {
        // 1. Try specific slot if provided
        // 2. Get compatible slots from CyberneticsBaseItem.Slots
        // 3. Shuffle, prioritize CyberneticsPreferredSlots
        // 4. Find first unoccupied compatible part
        // 5. Create implant, set CyberneticsNoRemove if !Removable
        // 6. MakeUnderstood for player
        // 7. bodyPart.Implant(implant)
    }
}
```

---

## Cybernetic Rejection Syndrome

When **mutants** install cybernetics, they risk developing rejection syndrome — a debilitating status effect.

> **Note**: The rejection system exists in code but may not be active in the base game. The `CyberneticRejectionSyndrome` file is commented as "not used in the base game."

### Chance Calculation

```csharp
int chance = 5 + implant.Cost + modifier;
// modifier from CyberneticsRejectionSyndromeModifier property

// Additional per-mutation modifiers:
foreach (mutation in implantee.Mutations)
{
    if (mutation.IsNonPhysical)
        chance += 1;  // Per level
    else if (mutation.IsPhysical)
        chance += mutation.Level;
}
```

### Effects by Level

| Effect | Formula | Cap |
|--------|---------|-----|
| Move speed penalty | `-min(Level, 10)` | -10 |
| Healing reduction | `min(Level × 5, 50)%` | 50% |
| Natural regen reduction | `min(Level × 7, 60)%` | 60% |

### Behavior

- On application: *"You feel feverish."* (red text)
- On level increase: *"Your feverish feeling is getting worse."*
- On removal: *"You feel less feverish."* (green text)
- Can be reduced via `Reduce(amount)` when implants are removed
- Level equals the `Cost` of the offending implant

---

## Cursed Cybernetics

The `CursedCybernetics` part makes an implant permanently irremovable.

```csharp
public class CursedCybernetics : IPart
{
    // Blocks CanBeUnequippedEvent unless Forced
    // Blocks BeginBeingUnequippedEvent unless Forced
    // Shows: "You can't remove [item]."
    // NOT considered an affliction (healing won't remove it)
}
```

**Behavior:**
- Normal removal at becoming nooks is blocked
- Only forced unequipping can remove it
- Not flagged as an affliction, so cure effects won't help
- Used for story/quest implants that should be permanent

---

## Hacking Terminals

Non-True Kin characters (mutants) can hack becoming nooks to gain access.

### Hack Mechanics (`CyberneticsTerminal2 : IHackingSifrahHandler`)

Uses the **Sifrah minigame** system for hacking attempts.

**Success outcomes:**

| Result | Hack Level Increase | Bonus |
|--------|-------------------|-------|
| Normal success | 1 (30% chance for each additional) | — |
| Exceptional success | 2 (50% chance for each additional) | +1-4 free license points |
| Partial success | 0 | No security alert |

**Failure outcomes:**

| Result | Security Alert Increase | Consequence |
|--------|------------------------|-------------|
| Failure | +1-4 | Alert rises |
| Critical failure | +2-6 | Extreme alert increase |

### Security System

```csharp
public bool HackActive => HackLevel > SecurityAlertLevel;

public bool CheckSecurity(int AlertChance, TerminalScreen Screen, int Times = 1)
{
    if (HackActive)
    {
        if (LowLevelHack) AlertChance -= 5;  // Stealth bonus

        for (int i = 0; i < Times; i++)
        {
            if (AlertChance.in100())
            {
                Terminal.ModIntProperty("SecurityAlertLevel", 1);
                break;
            }
        }
    }

    if (!Authorized) // Alert exceeded hack level
    {
        CurrentScreen = new CyberneticsScreenGoodbye();
        return false;  // Kicked out
    }
    return true;
}
```

Every terminal action has a chance to trigger a security alert. If the security alert level rises above the hack level, the player is locked out. Low-level hacking reduces this chance by 5%.

---

## Key Events & Extension Points

### Implant Lifecycle Events

| Event | When Fired | Key Data |
|-------|-----------|----------|
| `ImplantedEvent` | After implant installed | Implantee, Implant, BodyPart, ForDeepCopy, Silent |
| `UnimplantedEvent` | After implant removed | Implantee, Implant, BodyPart, Silent |
| `ImplantAddedEvent` | After ImplantedEvent | Same as ImplantedEvent |
| `ImplantRemovedEvent` | After UnimplantedEvent (cascade 17) | Same as UnimplantedEvent |
| `CanBeUnequippedEvent` | Before unequip attempt | Can block removal |
| `BeginBeingUnequippedEvent` | Before unequip process | Can block with failure message |

### Compute Power Events

| Event | Purpose |
|-------|---------|
| `GetAvailableComputePowerEvent` | Aggregate compute power from all sources |

### Body Part Queries

```csharp
// On GameObject:
bool HasInstalledCybernetics(string Blueprint, Predicate<GameObject> Filter)
BodyPart FindCybernetics(GameObject GO)
int GetInstalledCyberneticsCount()
List<GameObject> GetInstalledCybernetics()
void ForeachInstalledCybernetics(Action<GameObject> proc)
```

---

## Terminal UI Flow

### Screen Hierarchy

```
CyberneticsScreenMainMenu
├── "Learn about cybernetics"
│   └── CyberneticsScreenLearn
│       └── CyberneticsScreenLearnHowMany
│           └── CyberneticsScreenLearnUninstall
├── "Install a cybernetic implant"
│   └── CyberneticsScreenInstall (lists available implants)
│       └── CyberneticsScreenInstallLocation (pick body part)
│           └── CyberneticsInstallResult (animation + execution)
├── "Uninstall a cybernetic implant"
│   └── CyberneticsScreenRemove (lists installed implants)
│       └── CyberneticsRemoveResult (animation + execution)
├── "Upgrade your license tier" (if credits available)
│   └── CyberneticsScreenUpgrade
├── "Select a subject" (if hacked)
│   └── CyberneticsScreenSelectSubject
├── "Attempt to hack this terminal" (if not authorized)
│   └── HackingSifrah minigame
└── "Goodbye"
    └── CyberneticsScreenGoodbye
```

### Install Screen Display

Implants are sorted by:
1. Unavailable items last (already installed OneOnly, or too expensive)
2. Higher cost first among available
3. Alphabetical within same cost

Each entry shows:
- Implant name
- License cost in color (red if can't afford, cyan if can)
- `[already installed]` for OneOnly duplicates
- Condition warnings for broken/rusted/EMPed items

### Uninstall Screen Display

Shows installed implants with:
- Implant name + body part location
- `[cannot be uninstalled]` for CyberneticsNoRemove
- `[destroyed on uninstall]` for CyberneticsDestroyOnRemoval

### Upgrade Screen Display

Shows:
- Current license tier (and base tier without free licenses)
- Credit cost for next upgrade
- Available credits
- Button to confirm upgrade

---

## Butcherable Cybernetics — Harvesting Implants from Corpses

When True Kin NPCs die, their corpses may contain extractable cybernetic implants. The `CyberneticsButcherableCybernetic` part enables this recovery mechanic.

### Source: `XRL.World.Parts/CyberneticsButcherableCybernetic.cs`

```csharp
public class CyberneticsButcherableCybernetic : IPart
{
    public int BaseSuccessChance = 80;  // 80% base success rate
    public List<GameObject> Cybernetics;  // Stored implant objects
}
```

### Requirements
- Player must have the `CookingAndGathering_Butchery` skill
- Corpse must be visible and not a hologram
- Not in combat (auto-butcher checks for nearby hostiles)

### Butchering Algorithm

```
1. For each cybernetic implant stored on corpse:
   a. Roll BaseSuccessChance (80%)
   b. SUCCESS: Extract implant, add to player inventory or drop on ground
      → "You butcher [implant] from [corpse]."
   c. FAILURE: Implant is destroyed in the extraction process
      → "You rip [implant] out of [corpse], but destroy it in the process."
2. Destroy the corpse object after extraction
3. Costs 1000 energy (one turn)
```

### Sound Effect
Extraction plays `"Sounds/Interact/sfx_interact_cyberneticImplant_butcher"`.

### Auto-Butcher Integration
When the Butchery toggle is enabled, auto-butchering occurs via `ObjectEnteringCellEvent` — the player automatically attempts extraction when walking over eligible corpses (same combat-check rules as regular auto-butchering).

---

## Medassist Module — Detailed Implementation

The Medassist Module (`CyberneticsMedassistModule`) is one of the most complex cybernetic implants, implementing an autonomous medical injection system.

### Source: `XRL.World.Parts/CyberneticsMedassistModule.cs`

```csharp
public class CyberneticsMedassistModule : IPoweredPart
{
    public int TonicCapacity = 8;  // Maximum loaded tonics
    // Toggleable activated ability
    // Responds to BeforeApplyDamage on implantee
}
```

### Tonic Loading System

The module has its own **internal Inventory** that stores up to 8 injectable tonics:

| Action | Command | Description |
|--------|---------|-------------|
| Load single tonic | `LoadTonic` | Load from context menu on a tonic in player inventory |
| Load via picker | `LoadTonics` | Opens item picker showing all compatible tonics |
| Eject all tonics | `EjectTonics` | Returns all loaded tonics to player inventory |

**Loading Rules:**
- Only accepts items with the `Tonic` part
- Rejects tonics with `Eat = true` (oral tonics)
- Cannot exceed `TonicCapacity` (8)

### Auto-Injection AI

On each turn tick AND on `BeforeApplyDamage`, the module evaluates:

```
1. Check: module enabled (toggled on), not broken/rusted/EMPed
2. Check: implantee has room for another tonic effect (under tonic capacity)
3. Score each loaded tonic via GetUtilityScoreEvent
4. Select highest-scoring tonic (must be > 0)
5. Inject: fire ApplyingTonic → ApplyTonic events
6. Destroy consumed tonic
7. Message: "Your [module] injects you with [tonic]."
```

The **utility scoring** system considers the implantee's current state and incoming damage to decide which tonic is most needed. This allows it to, for example, inject a healing tonic when taking heavy damage or a salve tonic when poisoned.

### Phase-Awareness

The Medassist Module is **phase-aware**: if an injection shifts the implantee's phase (e.g., via a phase tonic), and the incoming damage no longer matches the new phase, the damage is negated entirely.

---

## Anatomy-Modifying Implants — Deep Dive

Several implants physically alter the creature's body part tree. These are among the most complex cybernetics implementations.

### Motorized Treads (`CyberneticsMotorizedTreads`)

**Source:** `XRL.World.Parts/CyberneticsMotorizedTreads.cs`

On implant:
1. **Saves** all original body part properties (Name, Description, DependsOn, Laterality, Mobility, Integral, Plural, Mass, Category)
2. **Converts** the target body part to `"lower body"` (Category 6, integral, non-mobile)
3. **Adds** two new `"Tread"` body parts as children (both integral, Category 6)
4. Triggers `WantToReequip()` to handle equipment changes
5. **Blocks dismemberment** of the implanted body part via `BeforeDismemberEvent`

On unimplant:
1. **Removes** added Tread parts by their manager ID
2. **Restores** all saved original properties
3. Triggers `WantToReequip()`

Uses a dual manager ID system:
- `AdditionsManagerID` = `"{objectID}::MotorizedTreads::Add"` — tracks added parts
- `ChangesManagerID` = `"{objectID}::MotorizedTreads::Change"` — tracks modified parts

### Gun Rack (`CyberneticsGunRack`)

**Source:** `XRL.World.Parts/CyberneticsGunRack.cs`

On implant:
1. Adds two `"Hardpoint"` body parts to the body root (Category 6, integral)
2. Hardpoints accept `"Missile Weapon"` equipment type
3. Positioned before Hands/Feet/Roots/Thrown Weapon in body order
4. Triggers `WantToReequip()`

### Grafted Mirror Arm (`CyberneticsGraftedMirrorArm`)

**Source:** `XRL.World.Parts/CyberneticsGraftedMirrorArm.cs`

On implant:
1. Adds one `"Thrown Weapon"` body part to the body root
2. Positioned after existing "Thrown Weapon" parts
3. Enables carrying an additional thrown weapon

All anatomy-modifying implants use **manager IDs** for clean removal — when unimplanted, parts are removed by manager ID string, ensuring only the parts added by that specific implant are affected (even if dismembered).

---

## Compute Power Event — Full API Reference

The `GetAvailableComputePowerEvent` is the central scaling system for cybernetics.

### Source: `XRL.World/GetAvailableComputePowerEvent.cs`

```csharp
[GameEvent(Cascade = 3, Cache = Cache.Singleton)]
public class GetAvailableComputePowerEvent : SingletonEvent<GetAvailableComputePowerEvent>
{
    public GameObject Actor;
    public int Amount;  // Aggregated compute power
}
```

### Query Methods

```csharp
// Get total compute power for an actor
static int GetFor(GameObject Actor)

// Get total compute power for an active part (sums across all subjects)
static int GetFor(IActivePart Part)
```

### Scaling Overloads (Complete Set)

**AdjustUp — Increase values with compute power:**
```csharp
// Integer scaling
static int AdjustUp(GameObject Actor, int Amount)
// → Amount × (100 + CP) / 100

static int AdjustUp(GameObject Actor, int Amount, float Factor)
// → Amount × (100 + CP × Factor) / 100

// Float scaling
static float AdjustUp(GameObject Actor, float Amount)
static float AdjustUp(GameObject Actor, float Amount, float Factor)

// IActivePart variants (same math, resolves actor from part)
static int AdjustUp(IActivePart Part, int Amount)
static int AdjustUp(IActivePart Part, int Amount, float Factor)
static float AdjustUp(IActivePart Part, float Amount)
static float AdjustUp(IActivePart Part, float Amount, float Factor)
```

**AdjustDown — Decrease cooldowns/costs with compute power:**
```csharp
// Integer scaling with floor
static int AdjustDown(GameObject Actor, int Amount, int FloorDivisor = 2)
// → max(Amount × (100 - CP) / 100, Amount / FloorDivisor)

static int AdjustDown(GameObject Actor, int Amount, float Factor, int FloorDivisor = 2)
// → max(Amount × (100 - CP × Factor) / 100, Amount / FloorDivisor)

// Float scaling with floor
static float AdjustDown(GameObject Actor, float Amount, int FloorDivisor = 2)
static float AdjustDown(GameObject Actor, float Amount, float Factor, int FloorDivisor = 2)

// IActivePart variants
static int AdjustDown(IActivePart Part, int Amount, int FloorDivisor = 2)
static int AdjustDown(IActivePart Part, int Amount, float Factor, int FloorDivisor = 2)
static float AdjustDown(IActivePart Part, float Amount, float FloorDivisor = 2)
static float AdjustDown(IActivePart Part, float Amount, float Factor, float FloorDivisor = 2)
```

The `Factor` parameter allows implants to scale at different rates with compute power. For example, `Factor = 0.5` means the implant only benefits from half the compute power.

### Palladium Electrodeposits — Bonus Intelligence

```csharp
// CyberneticsPalladiumElectrodeposits.HandleEvent(ImplantedEvent)
if (E.Part?.Type == "Head")
{
    StatShifter.SetStatShift(E.Implantee, "Intelligence", 2, baseValue: true);
}
```

Palladium Electrodeposits provides +2 Intelligence **only when installed in the Head slot**. It provides +20 compute power regardless of slot.

---

## Reactive Cranial Plating — Event Blocking Pattern

The `CyberneticsReactiveCranialPlating` demonstrates a key cybernetics design pattern: **event registration on the implantee**.

```csharp
// On implant: register to intercept events on the creature, not the implant object
E.Implantee.RegisterPartEvent(this, "CanApplyDazed");
E.Implantee.RegisterPartEvent(this, "ApplyDazed");
E.Implantee.RegisterPartEvent(this, "CanApplyStun");
E.Implantee.RegisterPartEvent(this, "ApplyStun");

// On unimplant: clean up by unregistering
E.Implantee.UnregisterPartEvent(this, "CanApplyDazed");
// ... etc

// Event handler: return false to block the event entirely
public override bool FireEvent(Event E)
{
    if (E.ID == "CanApplyDazed" || E.ID == "ApplyDazed" ||
        E.ID == "CanApplyStun" || E.ID == "ApplyStun")
        return false;  // Block stun and daze completely
    return base.FireEvent(E);
}
```

This pattern is used by many cybernetics that need to intercept events on their host creature rather than on themselves. The `RegisterPartEvent` / `UnregisterPartEvent` pair ensures clean lifecycle management.

---

## Inflatable Axons — Compute Power Scaling Example

A detailed example of how compute power scales a cybernetic ability:

```csharp
// Base values
public int Bonus = 40;    // +40 quickness
public int Duration = 10;  // 10 turns

// On activation:
int num3 = GetAvailableComputePowerEvent.GetFor(E.Actor);
if (num3 != 0)
{
    Duration = Duration * (100 + num3) / 100;   // Scale duration up
    Bonus = Bonus * (100 + num3) / 100;          // Scale bonus up
}
E.Actor.ApplyEffect(new AxonsInflated(Duration, Bonus, ParentObject));
E.Actor.CooldownActivatedAbility(ActivatedAbilityID, 100);  // Fixed 100-turn cooldown
```

With 20 compute power: `Bonus = 40 × 120/100 = 48`, `Duration = 10 × 120/100 = 12`
With 40 compute power: `Bonus = 40 × 140/100 = 56`, `Duration = 10 × 140/100 = 14`

After the boost expires, the implantee suffers -10 quickness for 10 turns (sluggish penalty, not shown in the compute power scaling — fixed penalty regardless of CP).

---

## NoteReducedLicensePoints — Discount Implants

Some implants have reduced license point costs compared to normal. The `NoteReducedLicensePoints` part adds a descriptive note:

```csharp
public class NoteReducedLicensePoints : IPart
{
    public int Amount;  // How many fewer license points
    // Appends to description:
    // "[This implant] costs [Amount] fewer license points than usual to install."
}
```

---

## Summary: Complete Cybernetics Flow

```
CHARACTER CREATION (True Kin only)
├── Genotype sets base CyberneticsLicensePoints
├── Subtype adds additional points
├── Player chooses starting implant OR rejects (+1 Toughness, -2 license)
└── Selected implant installed on game start

FINDING IMPLANTS
├── Loot from enemies/containers
├── Purchase from merchants
├── Hacking becoming nooks for bonus license points
└── Found pre-installed on defeated True Kin NPCs

AT A BECOMING NOOK
├── Authorization: True Kin auto-authorized, Mutants must hack
├── Install: Select implant → Select body part → Spend license points
├── Uninstall: Select implant → Moves to inventory (or destroyed)
├── Upgrade: Spend credit wedges → +1 license tier
└── Learn: Tutorial about the system

IMPLANT MECHANICS
├── Each implant costs License Points (1-6+)
├── Installed in body part slots (Head, Body, Arm, Hand, Feet, Back, Face)
├── Some use equipment slots (can't wear armor there)
├── Some modify anatomy (add/replace body parts)
├── Scale with Compute Power (from ComputeNodes, CoProcessors, Palladium)
├── Powered implants need charge (from Biodynamic Power Plant or cells)
├── Skillsofts grant skills/schematics while installed
└── Cursed implants cannot be removed

COMPUTE POWER
├── Sources: ComputeNode (20), Palladium Electrodeposits (20), ModCoProcessor (Tier×2.5)
├── AdjustUp: Amount × (100 + CP) / 100  (buffs scale up)
├── AdjustDown: max(Amount × (100 - CP) / 100, Amount/2)  (cooldowns scale down)
└── Overloading ComputeNodes increases output by ~30% at 400% load
```
