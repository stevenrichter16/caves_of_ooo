# Caves of Qud: Combat & Physics Systems -- Deep Dive Analysis

> **Source:** Decompiled from `Assembly-CSharp.dll` via ILSpy/dnSpy. All file references are relative to the `qud_decompiled_project/` directory (namespace-organized).

---

## Table of Contents

1. [Ranged Combat](#1-ranged-combat)
   - [1.1 MissileWeapon Part](#11-missileweapon-part)
   - [1.2 Fire Initiation Pipeline](#12-fire-initiation-pipeline)
   - [1.3 Accuracy & Aim Variance](#13-accuracy--aim-variance)
   - [1.4 Projectile Path Calculation](#14-projectile-path-calculation)
   - [1.5 Path Traversal & Hit Detection](#15-path-traversal--hit-detection)
   - [1.6 To-Hit Roll](#16-to-hit-roll)
   - [1.7 Penetration System](#17-penetration-system)
   - [1.8 Damage Calculation](#18-damage-calculation)
   - [1.9 Critical Hits](#19-critical-hits)
   - [1.10 Cover & Obstacles](#110-cover--obstacles)
   - [1.11 Thrown Weapons](#111-thrown-weapons)
   - [1.12 Ammunition Systems](#112-ammunition-systems)
   - [1.13 Weapon Skill Effects](#113-weapon-skill-effects)
   - [1.14 Energy Costs](#114-energy-costs)
   - [1.15 Ranged Combat Formulas Summary](#115-ranged-combat-formulas-summary)
2. [Temperature System](#2-temperature-system)
   - [2.1 Core Temperature Tracking](#21-core-temperature-tracking)
   - [2.2 Temperature Change Pipeline](#22-temperature-change-pipeline)
   - [2.3 Temperature Decay Toward Ambient](#23-temperature-decay-toward-ambient)
   - [2.4 Burning System](#24-burning-system)
   - [2.5 Freezing System](#25-freezing-system)
   - [2.6 Vaporization](#26-vaporization)
   - [2.7 Liquid Temperature System](#27-liquid-temperature-system)
   - [2.8 Resistance & Insulation](#28-resistance--insulation)
   - [2.9 Temperature Delivery Mechanisms](#29-temperature-delivery-mechanisms)
   - [2.10 Firefighting](#210-firefighting)
3. [Gas Propagation](#3-gas-propagation)
   - [3.1 Architecture Overview](#31-architecture-overview)
   - [3.2 The Gas Part: Core Properties](#32-the-gas-part-core-properties)
   - [3.3 Gas Propagation Algorithm](#33-gas-propagation-algorithm)
   - [3.4 Gas Merging System](#34-gas-merging-system)
   - [3.5 Gas Behavior Hierarchy](#35-gas-behavior-hierarchy)
   - [3.6 Individual Gas Types](#36-individual-gas-types)
   - [3.7 Respiratory Agent Performance System](#37-respiratory-agent-performance-system)
   - [3.8 Gas Immunity & Resistance](#38-gas-immunity--resistance)
   - [3.9 Gas Generation Sources](#39-gas-generation-sources)
   - [3.10 Environmental Interaction](#310-environmental-interaction)
   - [3.11 AI Navigation Weights](#311-ai-navigation-weights)
4. [Explosives](#4-explosives)
   - [4.1 IGrenade Base Class](#41-igrenade-base-class)
   - [4.2 Core Explosion Algorithm](#42-core-explosion-algorithm)
   - [4.3 Kinetic Resistance](#43-kinetic-resistance)
   - [4.4 Chain Detonation](#44-chain-detonation)
   - [4.5 Grenade Types](#45-grenade-types)
   - [4.6 Trigger Parts](#46-trigger-parts)
   - [4.7 Mine System](#47-mine-system)
   - [4.8 Special Explosion Types](#48-special-explosion-types)
   - [4.9 Concussion System](#49-concussion-system)
   - [4.10 Terrain Interaction](#410-terrain-interaction)
   - [4.11 Explosives Formula Summary](#411-explosives-formula-summary)

---

# 1. Ranged Combat

Ranged combat flows through: **initiation** (player/AI command) -> **projectile path calculation** (Bresenham line with angular variance) -> **path traversal** (cell-by-cell with cover/obstruction checks) -> **to-hit roll** (d20 + modifiers vs DV) -> **penetration roll** (3 dice vs AV) -> **damage** (dice per penetration).

## 1.1 MissileWeapon Part

**File:** `XRL.World.Parts/MissileWeapon.cs` (~3500 lines)

The `MissileWeapon` Part is the ranged combat equivalent of `MeleeWeapon`. Key fields:

```csharp
public class MissileWeapon : IPart
{
    public string Skill;                // Required skill tree ("Rifle", "Pistol", "HeavyWeapons", "Bow")
    public string Modifier = "Agility"; // Governing attribute for to-hit / penetration
    public int WeaponAccuracy;          // Per-shot angular scatter (0 = best, 25+ = worst)
    public int HitBonus;                // Flat to-hit bonus
    public int MaxRange = -1;           // Maximum firing range (-1 = unlimited)
    public int ShotsPerAction = 1;      // Shots fired per trigger pull
    public int AmmoPerAction = 1;       // Ammo consumed per shot
    public int EnergyCost = 1000;       // Energy cost to fire (1000 = one full action)
    public int NoWildfire;              // If 1, no wildfire (stray hits) risk
    public int WeaponBonusCap;          // Max stat bonus cap for penetration
    public string SlotType = "Hand";    // Valid equipment slot types
    public string ProjectilePenetrationStat; // Stat used for PV (e.g., "Strength" for bows)
    public string AimVarianceBonus;     // Extra aim variance reduction
    public string FireSound;            // Sound effect on fire
}
```

The weapon equips into `Hand` slots by default. `GiantHands` mutation can reduce hand requirements so a two-handed rifle can be wielded in one hand.

## 1.2 Fire Initiation Pipeline

**File:** `XRL.World.Parts/Combat.cs`, line 333

`Combat.FireMissileWeapon()` is the static entry point for all missile weapon firing:

```csharp
public static bool FireMissileWeapon(GameObject Attacker, GameObject AimedAt = null,
    Cell TargetCell = null, FireType FireType = FireType.Normal, string Skill = null,
    int Rapid = 0, int SweepShots = 0, int SweepWidth = 90,
    float? EnergyMultiplier = null, bool SkipPastMaxRange = false)
```

**Process:**
1. Checks `CanMoveExtremities` -- cannot fire if immobilized
2. Fires `CanFireMissileWeapon` event (can be cancelled by effects/parts)
3. Gets all equipped missile weapons via `GetMissileWeapons()`
4. If multiple weapons and `CanFireAllMissileWeaponsEvent` fails (Akimbo not active), picks one, alternating via `LastFired`
5. If the target has a **RifleMark** from this attacker, adds +2 to `AimLevel`
6. For each weapon, sends `CommandFireMissile` event to the weapon
7. For **sweep fire**: iterates `SweepShots` times with angular offset stepping across `SweepWidth` degrees
8. For **rapid fire**: fires `Rapid` extra shots with `EnergyMultiplier = 0` (free actions)

Energy cost is split across multiple weapons: `energyMult /= weaponCount`.

## 1.3 Accuracy & Aim Variance

**File:** `XRL.World.Parts/MissileWeapon.cs`, lines 2688-2760

The aim variance system determines angular scatter of each shot. Lower is better.

### Variance Accumulation

```
base = -StatMod(Agility)                         // Higher Agility = less variance
     - 2             (if SteadyHands skill)       // Shared Pistol/Rifle skill
     - AimLevel      (+2 if target is RifleMark'd)
     - AimVarianceBonus                           // Weapon property
     - MissileWeaponAccuracyBonus                 // Attacker + weapon properties
     + ModifyAimVariance events                   // Equipment/effect modifications
     + ModifyIncomingAimVariance events            // Defender-side modifications
```

### Variance Die Roll

```csharp
int varianceRoll = "2d20".Roll();           // Range 2-40, average 21
int finalVariance = |varianceRoll - 21| + accumulatedBase;  // 0-19 base, + modifiers
```

The roll is centered at 21. `|roll - 21|` gives a 0-19 range. The accumulated modifiers reduce this. A skilled shooter with high Agility easily gets variance = 0.

### Special Cases

- **Sure Fire** / **Beacon Fire** / **OneShot with SureFire**: Variance forced to 0
- **Running**: Adds `Random(-23, 23)` degrees unless Pistol + Sling and Run
- **Wildfire** (2+ hostile enemies adjacent): 50% chance adds `Random(-23, 23)` degrees
- **Per-shot scatter**: Each shot gets `Random(-WeaponAccuracy, WeaponAccuracy)` additional offset

### Wildfire Check (lines 2435-2460)

```csharp
if (!NoWildfire) {
    int hostileCount = 0;
    foreach (Cell adjacentCell in Attacker.CurrentCell.GetAdjacentCells()) {
        foreach (GameObject item in adjacentCell.LoopObjectsWithPart("Combat")) {
            if (item.IsHostileTowards(Attacker) && item.PhaseAndFlightMatches(Attacker)
                && item.CanMoveExtremities()) {
                hostileCount++;
                if (hostileCount > 1) {
                    if (50.in100()) wildShot = true;
                    break;
                }
            }
        }
    }
}
```

## 1.4 Projectile Path Calculation

**File:** `XRL.World.Parts/MissileWeapon.cs`, line 1372 (`CalculateBulletTrajectory`)

The trajectory is calculated in a **3x magnified coordinate space** for sub-cell precision:

```csharp
Path.X0 = X0 * 3;      // Origin (3x scale)
Path.Y0 = Y0 * 3;
Path.X1 = X1 * 3 + 1;  // Target center
Path.Y1 = Y1 * 3 + 1;

double angle = Math.Atan2(Path.X1 - Path.X0, Path.Y1 - Path.Y0).normalizeRadians();
int totalVariance = WeaponVariance + FlatVariance + AimVariance.RollCached();
angle += totalVariance * 0.0174532925; // Degrees to radians

// Ray march through 3x space
double dx = Math.Sin(angle);
double dy = Math.Cos(angle);
while (inBounds) { x += dx; y += dy; }
```

Visited cells are determined via `ListOfVisitedSquares()` (Bresenham-like in 3x space), then **deflection/refraction** is checked cell-by-cell for `RefractLight` and `ReflectProjectile` events.

## 1.5 Path Traversal & Hit Detection

**File:** `XRL.World.Parts/MissileWeapon.cs`, lines 2889-3460

For each cell along the trajectory:

### Solid Object Detection

```csharp
cell.FindSolidObjectForMissile(Attacker, Projectile, Launcher,
    out SolidObject, out IsSolid, out IsCover, out RecheckHit, out RecheckPhase, ...);
```

**In `Cell.cs` (line 5590):** iterates all objects in the cell:
1. Checks `TreatAsSolid` part
2. For each object: `ConsiderSolidForProjectile()` -- walls block unless `PenetrateWalls`, creatures block unless `PenetrateCreatures`
3. **Cover check**: `GetMissileCoverPercentageEvent` returns a percentage. If `percentage.in100()` succeeds, that object is solid/cover

### Combat Target Acquisition

```csharp
target = cell.GetCombatTarget(Attacker, IgnoreFlight: true,
    IgnoreAttackable: false, IgnorePhase: false, Phase, ...);
```

Gets the first valid combat target in the cell that phase-matches the projectile.

## 1.6 To-Hit Roll

**File:** `XRL.World.Parts/MissileWeapon.cs`, line 3311

```csharp
int naturalRoll = Stat.Random(1, 20);
int toHitMod = GetToHitModifierEvent.GetFor(Attacker, Target, Weapon, 0, Projectile,
    null, Skill, null, Prospective: false, Missile: true);
int totalHit = naturalRoll + toHitMod;
int combatDV = Stats.GetCombatDV(Target);
```

### To-Hit Modifier Breakdown (`GetToHitModifierEvent.cs`, line 73):

```
toHit = Bonus
      + Attacker.StatMod("Agility")
      + Attacker.IntProperty("HitBonus")
      + Weapon.IntProperty("HitBonus")
      + Target.IntProperty("IncomingHitBonus")
      + Attacker.IntProperty("MissileHitBonus")       // Missile-specific
      + Weapon.IntProperty("MissileHitBonus")          // Missile-specific
      + Projectile.IntProperty("HitBonus")             // Ammo-specific
      + Projectile.IntProperty("MissileHitBonus")      // Ammo-specific
      + Target.IntProperty("IncomingMissileHitBonus")  // Defender-specific
```

### DV Calculation (`Stats.cs`, line 20):

```csharp
public static int GetCombatDV(GameObject GO) {
    int result = 6 + GO.Stat("DV") + GO.StatMod("Agility");
    if (!GO.IsMobile()) result = -10;
    return result;
}
```

### Critical Missile DV Penalty (lines 3326-3333):

```csharp
if (!target.HasSkill("Acrobatics_SwiftReflexes")) {
    combatDV -= 5;  // Most creatures get -5 DV against missiles!
}
if (!target.IsMobile()) {
    combatDV = -100;  // Immobile = guaranteed hit
}
```

**Hit succeeds if:** `totalHit > combatDV`

## 1.7 Penetration System

**File:** `XRL.Rules/Stat.cs`, line 160 (`RollDamagePenetrations`)

The same penetration formula is used for both melee and ranged:

```csharp
public static int RollDamagePenetrations(int AV, int PV, int PVCap)
{
    int penetrations = 0;
    int successes = 3;  // Start at 3 to enter loop
    while (successes == 3) {
        successes = 0;
        for (int i = 0; i < 3; i++) {
            int roll = Random(1, 10) - 2;  // Base: -1 to 8
            int exploded = 0;
            while (roll == 8) {             // Exploding 10s!
                exploded += 8;
                roll = Random(1, 10) - 2;
            }
            exploded += roll;
            int total = exploded + Math.Min(PV, PVCap);
            if (total > AV) successes++;
        }
        if (successes >= 1) penetrations++;
        PV -= 2;  // PV drops by 2 for subsequent rounds
    }
    return penetrations;
}
```

**Algorithm:**
1. Roll 3 dice: each = `(1d10 - 2)` with exploding 10s, + `min(PV, PVCap)`
2. If die > AV: success
3. If 1+ of 3 succeed: count 1 penetration
4. If ALL 3 succeed: PV drops by 2, roll again
5. Repeat until fewer than 3 dice succeed

### Missile PV Calculation (in `MissileHit()`, line 1701):

```csharp
int pv = Projectile.BasePenetration;
int pvCap = Projectile.BasePenetration + Projectile.StrengthPenetration;
if (!ProjectilePenetrationStat.IsNullOrEmpty() && Attacker != null) {
    pv += Attacker.StatMod(ProjectilePenetrationStat); // e.g., Strength for bows
}
```

## 1.8 Damage Calculation

After penetration count is determined:

```csharp
DieRoll damageRoll = getMissileWeaponPerformanceEvent.GetPossiblyCachedDamageRoll();
int totalDamage = 0;
for (int i = 0; i < penetrationCount; i++) {
    totalDamage += damageRoll.Resolve();
}
damage.Amount = totalDamage;
```

Damage comes from `Projectile.BaseDamage` (e.g., "1d4", "1d6"), modified by `GetMissileWeaponPerformanceEvent` (weapon mods, power load, etc.).

## 1.9 Critical Hits

**File:** `XRL.World.Parts/MissileWeapon.cs`, line 1690

```csharp
int critThreshold = GetCriticalThresholdEvent.GetFor(Attacker, Defender, Weapon, Projectile, Skill);
int specialChance = GetSpecialEffectChanceEvent.GetFor(Attacker, Weapon, "Missile Critical", 5, ...);
if (specialChance != 5) {
    critThreshold -= (specialChance - 5) / 5;
}
if (naturalHitResult >= critThreshold) { isCrit = true; }
```

On critical hit, the skill's `GetWeaponCriticalModifier` adds bonus penetration:
- **Rifle** base: +2 PV and +2 PV cap
- **Pistol** base: +2, **Dead Shot** adds +2 more (total +4)
- **Heavy Weapons** base: +2
- **Beacon Fire**: automatic crit if target is marked

## 1.10 Cover & Obstacles

### MissileCover Part (`XRL.World.Parts/MissileCover.cs`)

```csharp
public int Percentage;  // 0-100 cover percentage (random intercept chance)
public bool DisabledByPenetrateCreatures = true;
public bool DisabledByPenetrateWalls = true;
```

### Cover Accumulation

`CalculateMissilePath` accumulates cover percentages along the path:
```csharp
if (IncludeCover) {
    float cumulative = 0f;
    for (int j = 0; j < Path.Cover.Count; j++) {
        cumulative += Path.Cover[j];
        Path.Cover[j] = cumulative;
    }
}
```

The UI color-codes cover: Green (<20%) -> dark green (20%) -> brown (50%) -> dark red (80%) -> red (>=100%).

### `FindSolidObjectForMissile` (Cell.cs, line 5590)

For each cell the projectile traverses:
1. Check `TreatAsSolid` parts
2. Check each object's `ConsiderSolidForProjectile()` -- walls, solid objects
3. Check cover: `GetMissileCoverPercentageEvent` returns a percentage, then `percentage.in100()` randomly determines if cover blocks

## 1.11 Thrown Weapons

**File:** `XRL.World.Parts/ThrownWeapon.cs` and `XRL.World/GameObject.cs` line 14793

Thrown weapons are separate from missile weapons:

- `ThrownWeapon` part stores `Damage`, `Penetration`, `PenetrationBonus`, `Attributes`
- Range: determined by `GetThrownWeaponRangeEvent` (usually Strength-based)
- Accuracy: `GetThrowProfileEvent` for `AimVariance`
- PV: `min(Strength, Penetration) + PenetrationBonus`
- The weapon is **unequipped** and physically travels to the target
- Uses `CloseThrowRangeAccuracyBonus` for short-range accuracy boost
- Hit detection: `Stat.RollPenetratingSuccesses("1d" + Stat("Agility"), 3)`

Every creature with standard anatomy gets a `Thrown Weapon` body part slot. Equipping into this slot costs 0 energy (instant swap).

### Throw Path Calculation (GameObject.cs, line 14846)

```csharp
MissileWeapon.CalculateMissilePath(MPath, zone, startX, startY, targetX, targetY,
    IncludeStart: false, IncludeCover: false, MapCalculated: false, this);
// Uses same Bresenham trajectory as missile weapons
```

### Throw Flight (line 14922-14930)

```csharp
for (int i = 0; i < list.Count; i++) {
    int dx = startX - list[i].X;
    int dy = startY - list[i].Y;
    int distance = (int)Math.Sqrt(dx * dx + dy * dy);
    if (distance >= Range || distance >= targetDist) break;
    // Process cell: check for solid objects, combat targets
    // ...
}
```

The thrown object stops at Range or target distance, whichever is less.

## 1.12 Ammunition Systems

### MagazineAmmoLoader (`XRL.World.Parts/MagazineAmmoLoader.cs`)

Standard physical ammo for rifles, pistols, bows:

```csharp
public int MaxAmmo;            // Magazine capacity
public string AmmoPart;        // Type accepted (e.g., "AmmoSlug", "AmmoArrow")
public GameObject Ammo;        // Currently loaded ammo stack
```

On fire: removes one ammo, gets projectile via `GetProjectileObjectEvent`. Reload costs `ReloadEnergy` (default 1000 = 1 turn).

### EnergyAmmoLoader (`XRL.World.Parts/EnergyAmmoLoader.cs`)

For energy weapons (laser rifles, phase cannons):
- Uses charge from energy cells via `EnergyCellSocket`
- Creates projectile from `ProjectileObject` blueprint each shot
- Power Load: if overloaded (400+ load), adds bonus damage

### LiquidAmmoLoader

For flamethrowers, acid sprayers -- consumes liquid volume per shot.

### Projectile Part (`XRL.World.Parts/Projectile.cs`)

```csharp
public int BasePenetration = 1;       // PV floor
public int StrengthPenetration;       // Additional PV cap above base
public bool PenetrateCreatures;       // Pass through creatures?
public bool PenetrateWalls;           // Pass through walls?
public string BaseDamage = "1d4";     // Damage per penetration
public string Attributes = "";        // "Vorpal", "Mental", "Psionic", etc.
public string PassByVerb = "whiz";    // "An arrow whizzes past you"
```

## 1.13 Weapon Skill Effects

### Pistol Skills

| Skill | Effect |
|-------|--------|
| **Pistol (base)** | Critical modifier +2 PV/cap |
| **SteadyHands** | -2 aim variance (shared with Rifle) |
| **DeadShot** | Critical modifier becomes +4 PV/cap total |
| **Akimbo** | Fire all equipped pistols simultaneously |
| **FastestGun** | Energy multiplier *= 0.75 (25% faster) |
| **EmptyTheClips** | Pistol energy cost *= 0.5 for 20 rounds (200-turn cooldown) |
| **SlingAndRun** | No running accuracy penalty for pistols |
| **DisarmingShot** | On hit, `Agility.in100()` chance to disarm |
| **WeakSpotter** | Bonus to-hit modifiers |

### Rifle/Bow Skills

| Skill | Effect |
|-------|--------|
| **Rifle (base)** | Critical modifier +2 PV/cap |
| **SteadyHands** | -2 aim variance (shared with Pistol) |
| **DrawABead** | Mark target with `RifleMark` (-1 variance, +2 AimLevel) |
| **SuppressiveFire** | Applies `Suppressed` effect (3-5 turns, locks in place) |
| **FlatteningFire** | Also knocks prone + disarms |
| **WoundingFire** | Applies `Bleeding` effect |
| **DisorientingFire** | Applies `Disoriented` (5-7 turns, -4 to rolls) |
| **SureFire** | Aim variance = 0 (perfect accuracy) |
| **BeaconFire** | Perfect accuracy + automatic critical hit |
| **OneShot** | Fires all special fire types simultaneously |

### Heavy Weapon Skills

| Skill | Effect |
|-------|--------|
| **HeavyWeapons (base)** | Critical modifier +2 PV/cap, -25 move speed |
| **Sweep** | 5 shots in 90-degree cone (250-turn cooldown) |
| **Tank** | Defensive bonuses |
| **StrappingShoulders** | Halves heavy weapon weight penalty |

## 1.14 Energy Costs

```csharp
int cost = (int)((float)EnergyCost * energyMultiplier);

// Pistol-specific reductions (cumulative):
if (Skill == "Pistol" && energyMultiplier > 0f) {
    if (Attacker.HasEffect<EmptyTheClips>()) energyMultiplier *= 0.5f;
    if (Attacker.HasSkill("Pistol_FastestGun")) energyMultiplier *= 0.75f;
    int mod = Attacker.GetIntProperty("PistolEnergyModifier");
    if (mod != 0) energyMultiplier *= (100f - mod) / 100f;
}
```

## 1.15 Ranged Combat Formulas Summary

| Formula | Expression |
|---------|------------|
| **Aim Variance** | `abs(2d20 - 21) + base_modifiers` (min 0). SureFire = 0. |
| **Per-shot scatter** | `Random(-WeaponAccuracy, WeaponAccuracy)` degrees |
| **To-Hit** | `1d20 + AgilityMod + HitBonuses` vs `6 + DV + AgilityMod - 5` |
| **Missile DV penalty** | -5 DV for targets without Swift Reflexes |
| **Penetration** | 3 dice: `(1d10-2)[exploding] + min(PV, PVCap)` vs AV. If all 3 succeed: PV-=2, roll again. |
| **Damage** | Per penetration: roll `Projectile.BaseDamage`. Total = sum. |
| **Stat Modifier** | `floor((Score - 16) / 2)` |

---

# 2. Temperature System

## 2.1 Core Temperature Tracking

**File:** `XRL.World.Parts/Physics.cs`

Every game object has a `Physics` part storing temperature state:

```csharp
public const int BASE_TEMPERATURE = 25;        // Universal baseline (room temp)
public int _Temperature = 25;                   // Current temperature
public int FlameTemperature = 350;              // Ignition threshold
public int VaporTemperature = 10000;            // Vaporization threshold
public int FreezeTemperature = 0;               // Freezing threshold
public int BrittleTemperature = -100;           // Frozen-solid / brittle threshold
public float SpecificHeat = 1f;                 // Thermal mass (0 = immune to temp changes)
public GameObject InflamedBy;                   // Kill attribution for fire
```

### State Checking Methods (lines 614-631)

| Method | Condition | Note |
|--------|-----------|------|
| `IsFrozen()` | `Temperature <= BrittleTemperature` | The severe frozen state |
| `IsFreezing()` | `Temperature <= FreezeTemperature` | Mild cold state |
| `IsAflame()` | `Temperature >= FlameTemperature` | On fire |
| `IsVaporizing()` | `Temperature >= VaporTemperature` | Being vaporized |

**Temperature Setter Guard (line 268):**
```csharp
set { if (SpecificHeat != 0f || Temperature == 25) { _Temperature = value; } }
```
Objects with `SpecificHeat = 0` are permanently at 25 degrees -- immune to all temperature changes.

### Ambient Temperature (lines 227-257)

Each zone has a `BaseTemperature`. Per-object ambient is:
- If zone temp > 25 and object has HeatResistance: `Ambient = Max(25, zoneTemp - 4 * HeatResistance)`
- If zone temp < 25 and object has ColdResistance: `Ambient = Min(25, zoneTemp + ColdResistance)`
- Otherwise: `Ambient = zoneTemp`

Heat resistance reduces perceived ambient by **4 per point**. Cold resistance is **1:1**.

## 2.2 Temperature Change Pipeline

**File:** `XRL.World.Parts/Physics.cs`, lines 4023-4167 (`ProcessTemperatureChange`)

This is the central method for ALL temperature changes. Called via `GameObject.TemperatureChange()`.

### Phase Matching
Temperature changes respect phasing. Different-phase actors/targets are rejected (lines 4040-4042).

### Event Interception
1. `BeforeTemperatureChangeEvent` -- any handler can modify or cancel
2. `AttackerBeforeTemperatureChangeEvent` -- allows amplification (used by ThermalAmp)

### Two Modes: Radiant vs Direct

**RADIANT mode** (`Radiant = true`):
```csharp
Amount = Amount * (100 - Resistance) / 100;                    // Resistance always applies
Temperature += (Amount - Temperature) * (0.035 / SpecificHeat); // Asymptotic approach
```
- Gradual approach to source temperature, never overshoots
- If object is already aflame, cooling toward the radiant source is blocked
- Used for environmental/spreading heat

**DIRECT mode** (`Radiant = false`):
```csharp
Amount = Amount / SpecificHeat;  // Higher mass = less change
// FattyHump mutation halves the amount
// Resistance applies only when crossing thresholds (>50 for heat, <25 for cold)
Temperature += Amount;           // Simply additive
```
- Used for deliberate attacks (weapon hits, mutations)

### State Transition Detection (lines 4138-4165)

After applying the change:
- **Ignition**: If newly aflame -- plays ignition sound, records `InflamedBy`, interrupts autoact
- **Extinguished**: If was aflame, now not -- plays extinguish sound
- **Frozen**: If newly frozen -- plays frozen sound
- **Thawed**: If was frozen, now not -- plays thaw sound

## 2.3 Temperature Decay Toward Ambient

**File:** `XRL.World.Parts/Physics.cs`, lines 2932-3051 (`UpdateTemperature`)

Called every turn on `EndTurnEvent` (only if active zone, player proximity, or aflame/frozen).

### Thermal Insulation Dead Zone

```csharp
int insulation = ParentObject.GetIntProperty("ThermalInsulation", 5); // Default 5
```
Temperature only decays if it differs from ambient by more than this threshold. Prevents trivial oscillation.

### Decay Formula

**Cooling** (Temperature > Ambient):
```csharp
int decay = Math.Max(5, (int)((Temperature - AmbientTemperature) * 0.02));
```
**Heating** (Temperature < Ambient): Same formula in reverse.

- **2% of difference from ambient, minimum 5 degrees per turn**
- Modified by ColdResistance (when cooling) or HeatResistance (when warming)
- Blocked if `CanTemperatureReturnToAmbientEvent` returns false (used by TemperatureController)

### Heat Radiation to Adjacent Cells (lines 2972-2980)

When an object's temperature differs from ambient, it radiates to all 8 adjacent cells:
```csharp
int amount = AmbientTemperature + tempOffset;
foreach (Cell adj in CurrentCell.GetLocalAdjacentCells()) {
    adj.TemperatureChange(amount, InflamedBy, Radiant: true, ...);
}
```
**This is how fire spreads** -- burning objects radiate heat each turn using **radiant** mode.

## 2.4 Burning System

### Burn Damage

The actual burn damage comes from `Physics.UpdateTemperature()` (lines 3006-3021):

```csharp
if (IsAflame()) {
    WasAflame = true;
    if (ParentObject.FireEvent("Burn") && IsAflame()) {
        ParentObject.TakeDamage(
            Burning.GetBurningAmount(ParentObject).RollCached(),
            "from the fire%S!", "Fire", ...);
    }
}
```

### Burn Damage Table (`Burning.GetBurningAmount`, lines 47-79)

Based on overshoot above `FlameTemperature`:

| Temp - FlameTemp | Damage/Turn |
|------------------|-------------|
| 0-100 | 1 |
| 101-300 | 1-2 |
| 301-500 | 2-3 |
| 501-700 | 3-4 |
| 701-900 | 4-5 |
| 900+ | 5-6 |

### HotBurn Feedback Loop (line 3016-3018)

Objects with `"HotBurn"` tag gain additional temperature while burning:
```csharp
if (ParentObject.HasTag("HotBurn")) {
    Temperature += hotBurnValue;
}
```
Creates a positive feedback loop -- the hotter it gets, the more temperature it gains.

### Ignition Sources

**Campfire** (`XRL.World.Parts/Campfire.cs`, lines 145-189):
- 10% chance per turn to raise own temperature by 150 (if under 600)
- Heats all objects in cell by 150 per turn
- Implements `RadiatesHeatEvent`

**PyroZone** (`XRL.World.Parts/PyroZone.cs`):
- Created by Pyrokinesis mutation
- Temperature change: `(310 + 30 * Level) / 2` per turn

## 2.5 Freezing System

### State Transitions (Physics.cs lines 3030-3050)

```csharp
if (IsFrozen()) {
    if (!WasFrozen && FrozeEvent.Check(ParentObject, InflamedBy)) {
        WasFrozen = true;
        // Play frozen sound, apply Frozen effect
    }
} else if (WasFrozen) {
    WasFrozen = false;
    // Remove Frozen effect, fire ThawedEvent
}
```

### Frozen Effect (`XRL.World.Effects/Frozen.cs`)

Prevents physical actions: `"Can't take physical actions."`

### Frozen Rendering (Physics.cs lines 2087-2105)

- **Brittle** (at BrittleTemperature): cyan foreground + bright cyan background
- **Freezing** (at FreezeTemperature): frozen "ø" symbol on certain animation frames

### HeatSelfOnFreeze (`XRL.World.Parts/HeatSelfOnFreeze.cs`)

Biological cold-resistance (shivering):
```csharp
int amount = HeatAmount.RollCached() * (BrittleTemperature - Temperature) / 100;
ParentObject.TemperatureChange(amount);
```
Default: recovers 60% of the difference between BrittleTemp and current temp.

### CryoZone (`XRL.World.Parts/CryoZone.cs`)

Created by Cryokinesis mutation. Temperature change: `(-20 - 60 * Level) / 2` per turn.

## 2.6 Vaporization

**File:** `XRL.World.Parts/Physics.cs`, lines 2983-3005

When `Temperature >= VaporTemperature` (default 10000):
1. `VaporizedEvent.Check()` fires -- can be cancelled
2. Object dies: "You were vaporized"
3. If object has `VaporObject` tag, that object (typically gas) is created at the cell
4. Gas `Creator` = `InflamedBy` for kill attribution

## 2.7 Liquid Temperature System

### BaseLiquid Properties (`XRL.Liquids/BaseLiquid.cs`)

Every liquid type defines thermal properties:

```csharp
public int FlameTemperature = 99999;
public int VaporTemperature = 100;
public int FreezeTemperature = 0;
public int BrittleTemperature = -100;
public int Temperature = 25;           // Intrinsic temperature
public int ThermalConductivity = 50;   // Heat transfer rate (0-100)
public int Combustibility = 0;         // Flammability (-100 to 100, negative = suppression)
```

### Liquid Temperature Properties

| Liquid | FlameTemp | VaporTemp | Combustibility | ThermalCond | IntrinsicTemp |
|--------|-----------|-----------|----------------|-------------|---------------|
| Water | 99999 | 100 | **-50** | 50 | 25 |
| Oil | 250 | 2000 | **90** | 40 | 25 |
| Asphalt | 240 | 1240 | **75** | 35 | 25 |
| Sap | 250 | 1250 | **70** | 40 | 25 |
| Wax | 300 | 2000 | **65** | 40 | 25 |
| Honey | 300 | 1300 | **60** | 40 | 25 |
| Ink | 350 | 1350 | 30 | 40 | 25 |
| Blood | 400 | 1200 | 2 | 35 | 25 |
| Wine | 620 | 1620 | 15 | 45 | 25 |
| Lava | 99999 | 10000 | 0 | 50 | **1000** |
| Neutron Flux | 99999 | 10000 | 0 | **0** | 25 |

### Liquid-Object Temperature Transfer (`LiquidVolume.cs`, lines ~2680-2737)

```csharp
double volume = Math.Min(Volume, GO.GetMaximumLiquidExposureAsDouble());
int thermalConductivity = GetLiquidThermalConductivity();
int liquidTemp = GetLiquidTemperature();
int diff = liquidTemp - objTemp;
double change = diff * volume.DiminishingReturns(8.0) / 4.0;
```

Key interactions:
- **Combustible liquid + aflame object**: If combustibility >= 50, liquid FEEDS the fire
- **Water (combustibility -50)**: Actively suppresses fire
- ThermalConductivity scales final change: `change = change * thermalConductivity / 100`

### Liquid Freezing (Phase Change) (`LiquidVolume.cs`, lines ~3886-3946)

Each liquid defines freeze objects at volume thresholds:

| Liquid | Small Freeze | Medium Freeze | Large Freeze | Verb |
|--------|-------------|---------------|--------------|------|
| Lava | Small boulder (1+) | Medium boulder (100+) | Shale wall (400+) | solidify |
| Wax | Wax Nodule (1+) | Wax Block (500+) | -- | congeal |
| Salt | -- | -- | Halite (500+) | solidify |
| Convalescence | CryoGas (1+) | -- | -- | sublimate |

When freezing:
```csharp
ParentObject.Die(E.By, null, "You froze.", ...);  // Liquid pool dies
GameObject frozen = GameObject.Create(liquidFreezeObject);
cell.AddObject(frozen);                             // Frozen form replaces it
```

Creatures in the liquid get `Stuck` effect:
- Large freeze: `Stuck(30 duration, 25 difficulty, "Frozen Stuck Restraint")`
- Small freeze: `Stuck(5 duration, 15 difficulty, DependsOnMustBeFrozen: true)` -- breaks when ice thaws

### Mixed Liquid Temperature

For mixed liquids, all properties are **weighted averages** (per-mille component ratios).

## 2.8 Resistance & Insulation

### Heat/Cold Resistance

Applied during `ProcessTemperatureChange`:
- **Radiant**: `Amount = Amount * (100 - Resistance) / 100` -- always
- **Direct**: Only applied crossing thresholds (>50 heat, <25 cold)
- **FattyHump mutation**: Halves all direct temperature changes

### Insulating Part (`XRL.World.Parts/Insulating.cs`)

Worn equipment reduces cold changes:
```csharp
if (E.Amount < 0) {
    E.Amount = (int)((float)E.Amount * Amount);  // Default 0.9 = 10% reduction
}
```

### ThermalAmp (`XRL.World.Parts/ThermalAmp.cs`)

Amplifies outgoing heat/cold:
```csharp
Amount *= (100 + GetPercentage(ModifyHeat, powerLoad)) / 100;
```

## 2.9 Temperature Delivery Mechanisms

| Part | Trigger | Effect |
|------|---------|--------|
| `TemperatureOnHit` | WeaponHit, WeaponDealDamage, ProjectileHit | Dice-rolled temp change on hit |
| `TemperatureOnEntering` | ProjectileEntering | Temp change to all objects in cell |
| `TemperatureOnEat` | Eating | +100 temperature to eater |
| `ModFlaming` | Weapon mod | `2*(Tier+PowerLoadBonus)d8` heat |
| `ModFreezing` | Weapon mod | `-(Tier+PowerLoadBonus)d4` cold |
| `TemperatureAdjuster` | Powered, per turn | Fixed temp adjustment toward threshold |
| `TemperatureController` | Powered, per turn | Smart thermostat toward configurable target |
| `TemperatureVenting` | Over ceiling/below floor | Vents % of differential, spawns steam/cryo gas |

## 2.10 Firefighting

**File:** `XRL.World.Capabilities/Firefighting.cs`

Requirements: hands (to beat) OR can go prone (to roll), HP < 50%, Int >= 7, Will >= 7.

Methods:
- **Beat with hands**: Reduces temperature via `GetFirefightingPerformanceEvent`
- **Roll on ground**: Applies `Prone`, reduces temperature by 200 (clamped at ambient)

---

# 3. Gas Propagation

## 3.1 Architecture Overview

The gas system has three layers:

1. **`Gas` (base part)** -- `XRL.World.Parts/Gas.cs` -- Handles density, propagation, merging, dissipation
2. **`IGasBehavior` / `IObjectGasBehavior`** -- Abstract classes defining how gas affects creatures
3. **Concrete gas types** (e.g., `GasPoison`, `GasStun`, `GasSleep`) -- Specific damage/effect logic

A gas in-world is a **full GameObject** with a `Gas` part + behavior parts. Each cell can contain a gas object. Same-type gases in the same cell **merge** (densities combine).

## 3.2 The Gas Part: Core Properties

**File:** `XRL.World.Parts/Gas.cs`

```csharp
public int _Density = 100;           // Concentration in this cell
public int Level = 1;                 // Power level (affects damage, save DCs)
public bool Seeping;                  // Can pass through solid walls
public bool Stable;                   // Does not naturally lose density
public string GasType = "BaseGas";    // Type identifier for merging
public string ColorString;            // Visual color
public GameObject _Creator;           // Who created this gas
```

The `Density` property fires a `DensityChange` event on change, flushing AI navigation caches.

## 3.3 Gas Propagation Algorithm

**File:** `XRL.World.Parts/Gas.cs`, lines 204-319 (`ProcessGasBehavior`)

Called every turn via `TurnTick`. The algorithm:

### Step 1: Natural Density Decay
```csharp
if (Density > 10 && !Stable) {
    Density -= GetDispersalRate();  // Random(1, 3) by default
}
```

The `GetDispersalRate()` method fires `CreatorModifyGasDispersal` event -- the `GasTumbler` part can multiply dispersal rate by `DispersalMultiplier/100` (default 25% = 4x slower).

### Step 2: Wind Check
```csharp
int windSpeed = parentZone.CurrentWindSpeed;
string windDirection = parentZone.CurrentWindDirection;
```

### Step 3: Spread Chance
```csharp
if ((25 + windSpeed).in100()) {   // Base 25% chance + wind speed
    int spreadAttempts = Stat.Random(1 + windSpeed/30, 4 + windSpeed/20);
```
Base **25% chance per turn** to spread. Number of attempts: **1-4** in calm conditions.

### Step 4: Direction Selection (Wind Bias)
```csharp
string direction = ((windSpeed.in100() && 90.in100()) ? windDirection : null)
    ?? Directions.GetRandomDirection();
```
For each attempt: if wind is strong, bias toward wind direction (90% of the time).

### Step 5: Solid/Wall Check
```csharp
if (Seeping || !targetCell.IsSolidFor(ParentObject))
```
Gas enters if: `Seeping = true` (passes through everything) OR cell is not solid. Walls and closed doors block. Open doors allow passage.

### Step 6: Stable Gas Handling
Stable gas only spreads to cells **empty of other gas**. Non-stable gas always tries.

### Step 7: Density Transfer
```csharp
int transferAmount = Stat.Random(1, Math.Min(Density, 30));
```
Each attempt transfers **1 to min(density, 30)** to target cell.

### Step 8: Merge or Create
If target cell has same-type gas: merge densities. Otherwise: create new gas object from blueprint, transfer density, copy Level/Seeping/Creator.

### Step 9: Dissipation Check
```csharp
if (Density <= 0 || (Density <= 10 && (50 + windSpeed).in100())) {
    Dissipate();  // Obliterate the gas object
}
```
Gas fully dissipates when density reaches 0, or at density <= 10 with a 50%+ roll.

## 3.4 Gas Merging System

**File:** `Gas.cs`, lines 354-421

**Merge conditions**: Same `GasType` AND same `ColorString` AND same Phase.

```csharp
private void MergeGas(Gas gas) {
    Density += gas.Density;                         // Add all density
    if (gas.Level > Level) Level = gas.Level;       // Keep highest level
    if (gas.Seeping && !Seeping) Seeping = true;    // Inherit seeping
    if (gas.Creator != null && Creator == null) Creator = gas.Creator;
}
```

## 3.5 Gas Behavior Hierarchy

### IGasBehavior (`XRL.World.Parts/IGasBehavior.cs`)

Base class providing:
- `BaseGas` -- cached reference to the `Gas` part
- `GasDensity()` -- current density
- `GasDensityStepped(Step=5)` -- rounds density to nearest 5 (using `StepValue`)

### IObjectGasBehavior (`XRL.World.Parts/IObjectGasBehavior.cs`)

Adds creature-affecting logic:
- **`ObjectEnteredCellEvent`**: calls `ApplyGas(E.Object)` when something enters
- **`TurnTick`**: calls `ApplyGas(ParentObject.CurrentCell)` to affect all objects each turn
- **`DensityChange`**: flushes navigation caches

## 3.6 Individual Gas Types

### Poison Gas (`GasPoison.cs`)

**Requirements:** `Creature` tag, must `Respires`, passes `CheckGasCanAffectEvent`.

**Immediate damage:**
```csharp
int perf = GetRespiratoryAgentPerformanceEvent.GetFor(Object, ParentObject, gasPart);
int damage = (int)Math.Max(Math.Floor((double)(perf + 1) / 20.0), 1.0);
// Density 100 -> floor(101/20) = 5 damage/turn
```

**Lingering effect:** `PoisonGasPoison(Duration=Random(1,10), Damage=GasLevel*2)` -- deals damage per turn after leaving gas.

### Stun Gas (`GasStun.cs`)

**No saving throw.** Three tiers based on density:

| Density | Effect |
|---------|--------|
| > 60 | Full stun -- forfeits entire turn |
| > 40 | -60% Quickness (Speed * 6/10) |
| <= 40 | -30% Quickness (Speed * 3/10) |

### Sleep Gas (`GasSleep.cs`)

**Save:** Toughness vs DC `5 + gasLevel + respiratoryPerformance/10`
**Effect:** Asleep for `4d6 + gasLevel` turns. Does NOT apply if already asleep.

### Confusion Gas (`GasConfusion.cs`)

**Save:** Toughness vs DC `5 + gasLevel + respiratoryPerformance/10`
**Effect:** `Confused(duration=4d6+gasLevel, level=gasLevel, maxLevel=gasLevel+2, type="ToxicConfusion")`

### Shame Gas (`GasShame.cs`)

**Save:** **Willpower** (not Toughness!) vs DC `5 + gasLevel + respiratoryPerformance/10`
**Effect:** `Shamed(duration=2d6 + gasLevel*2)`

### Disease Gas (`GasDisease.cs`)

**Save:** Toughness vs DC `5 + gasLevel + respiratoryPerformance/10`
**Disease type:** Odd zone depth = Glotrot, even = Ironshank.

### Steam Gas (`GasSteam.cs`)

**No saving throw. Density-based:**
```csharp
damage = (int)Math.Max(Math.Ceiling(0.18f * (float)part.Density), 1.0);
// Density 100 = 18 damage/turn
```
Only affects organic creatures. Reduced by HeatResistance.

### Cryo Gas (`GasCryo.cs`)

**Temperature + flat damage:**
```csharp
int tempDrop = (int)Math.Ceiling(2.5f * (float)part.Density);
// Density 100 = -250 degrees/turn
GO.TemperatureChange(-tempDrop, ...);
GO.TakeDamage(1, "Cold", ...);  // Plus 1 flat cold damage
```

### Plasma Gas (`GasPlasma.cs`)

Applies `CoatedInPlasma` effect with duration = 40-60% of density. Very high navigation weight (70-90).

### Ash Gas (`GasAsh.cs`)

```csharp
damage = (int)Math.Max(Math.Floor((double)(perf + 1) / 10.0), 1.0);
// Density 100 = 10 damage/turn
```
**Unique: Occluding** when density >= 40, blocks line of sight.

### Fungal Spores (`GasFungalSpores.cs`)

Complex infection mechanic:
1. Applies `SporeCloudPoison` (damage = `min(1, GasLevel)`, duration = `Random(2,5)`)
2. Toughness save vs DC `10 + GasLevel/3` to resist `FungalSporeInfection`
3. If save fails: infection duration `Random(20,30) * 120` turns
4. Creator is immune (spore-producers don't self-infect)

### Generic Damaging Gas (`GasDamaging.cs`)

Configurable system used for acid gas and normality gas:

**Creature damage:** `ceil(respiratoryPerformance * gasLevel / CreatureDamageDivisor)` (default divisor = 200)
**Object damage:** `ceil((0.75 * level + 0.25) * density)` -- MUCH higher than creature damage

Supports: `AffectEquipment`, `AffectCybernetics`, `TargetPart`, `TargetTag`, `TargetBodyPartCategory`, `DamageAttributes` (Acid, Heat, Cold, Electric).

## 3.7 Respiratory Agent Performance System

**File:** `XRL.World/GetRespiratoryAgentPerformanceEvent.cs`

Central mechanism translating gas density to effect strength:

```csharp
public static int GetFor(GameObject Object, GameObject GasObject, Gas Gas, ...)
{
    int result = Gas?.Density ?? 0;  // Start with gas density
    // Fire events for equipment/effect modification
    result += LinearAdjustment;
    result = result * (100 + PercentageAdjustment) / 100;
    return result;
}
```

**Gas Mask** hooks into this:
```csharp
E.LinearAdjustment -= Power * 5;  // Default Power=10 -> -50 from density
```
A gas mask with Power 10 subtracts **50 from respiratory performance**, adds **+Power to saves**, and reduces Gas-attributed damage by **Power%**.

## 3.8 Gas Immunity & Resistance

### GasImmunity Part (`XRL.World.Parts/GasImmunity.cs`)

```csharp
public override bool HandleEvent(CheckGasCanAffectEvent E) {
    if (E.Gas.GasType == GasType) return false;  // Immune
}
```

### Self-Immunity

Gas generation mutations grant immunity to own gas type via same `CheckGasCanAffectEvent`.

### Respiration Gating

Most gases require `Object.Respires` to return true (`RespiresEvent`). Robots and non-breathing creatures are generally immune to inhaled gases.

### Gas Mask Save Bonus

```csharp
E.Roll += Power;         // +10 to save roll with default mask
E.IgnoreNatural1 = true; // Cannot critically fail saves
```

### Elemental Resistances

For `GasDamaging` with elemental damage (Heat, Cold, Acid, Electric), creature resistance reduces both navigation weight and damage: `weight * (100 - resistance) / 100`.

## 3.9 Gas Generation Sources

### Gas Grenades (`XRL.World.Parts/GasGrenade.cs`)

Creates gas in center + all adjacent cells (up to 9 cells):
```csharp
List<Cell> adjacentCells = C.GetAdjacentCells();
adjacentCells.Add(C);
foreach (Cell item in adjacentCells) {
    Gas part = gameObject.GetPart<Gas>();
    part.Creator = Actor;
    part.Density = Density;  // Default 20
    part.Level = Level;      // Default 1
    item.AddObject(gameObject);
}
```

### Gas Generation Mutations (`XRL.World.Parts.Mutation/GasGeneration.cs`)

Activated toggle. When active, pumps gas every turn for `GetReleaseDuration(Level)` rounds:

```csharp
// Total density = 800, split among valid adjacent cells
part.Density = GetGasDensityForLevel(Level) / validCells.Count;
// With 8 adjacent cells: 100 density per cell per turn
```

| Mutation | Gas Object | Duration | Cooldown |
|----------|-----------|----------|----------|
| PoisonGasGeneration | PoisonGas | Level + 2 | 40 |
| CorrosiveGasGeneration | AcidGas | Level + 2 | 40 |
| SleepGasGeneration | SleepGas | Level + 2 | 40 |
| ConfusionGasGeneration | ConfusionGas | (Level+2)*3/2 | 40 |
| NormalityGasGeneration | NormalityGas | (Level+2)*3/2 | 40 |

### Wall Traps (`XRL.World.Parts/WalltrapGas.cs`)

Gas jet along all 4 cardinal directions, up to `JetLength` (default 6) cells:
```csharp
for (int i = 0; i < JetLength; i++) {
    C = C.GetCellFromDirection(D);
    if (C == null || C.IsSolid(ForFluid: true)) break;
    part.Density = Density.RollCached();  // "1d80+20" = 21-100
}
```

### Weapon Hit Sources

| Part | Trigger | Density | Notes |
|------|---------|---------|-------|
| `GasOnHit` | Weapon hit | 3d10 (3-30) | Single cell |
| `GasOnEntering` | Projectile enters cell | 3d10 | Single cell |
| `EmitGasOnHit` | Weapon hit | Center: 4d10, Adjacent: 2d10 | Multi-cell |
| `BurnOffGas` | Accumulated damage threshold | Per `DamagePer` (10) | Heat/Fire damage driven |

## 3.10 Environmental Interaction

### Walls and Doors

- Standard walls and closed doors: **solid** -- gas blocked
- Open doors: **not solid** -- gas flows through
- `Seeping` flag bypasses ALL solid checks

### Gas Repulsion (`PartsGas.cs`)

Powered part that repels gas objects away from equipped creature:
1. Gets cells within `Radius` (default 1)
2. For each gas, if `Chance%` succeeds: moves gas object outward
3. Supports various charge modes

### BlowAwayGas (`BlowAwayGas.cs`)

Fan-like effect. Spirals through cells within `Radius` (default 4), moves all non-self gas objects outward.

### GasTumbler (`GasTumbler.cs`)

Equipment that modifies gas from the wearer:
- **Density**: `DensityMultiplier/100` (default 200% = double density)
- **Dispersal**: `DispersalMultiplier/100` (default 25% = 4x slower dispersal)

### Phase Matching

Gas interacts only with phase-matching objects: `obj.PhaseMatches(ParentObject)`. Phase carries over from creators.

## 3.11 AI Navigation Weights

Every gas behavior implements `GetNavigationWeightEvent`:

Example from GasPoison:
```csharp
// In-cell: density/2 + level*10, capped at min(50+level*10, 80)
E.MinWeight(GasDensityStepped() / 2 + GasLevel * 10, Math.Min(50 + GasLevel * 10, 80));
// Adjacent: density/10 + level*2, capped at min(10+level*2, 16)
```

Plasma gas has highest weights (70-90 in-cell). Non-breathing creatures ignore inhaled gases. `E.IgnoreGases` flag allows AI to ignore gas entirely.

### Key Gas Constants

| Constant | Value | Context |
|----------|-------|---------|
| Base spread chance | 25% per turn | ProcessGasBehavior |
| Spread attempts | 1-4 (calm) | Per turn |
| Max density transfer | min(density, 30) | Per attempt |
| Natural decay | Random(1,3) per turn | Non-stable gas |
| Dissipation threshold | <=10 density, 50% chance | Per turn |
| Grenade default density | 20 per cell (9 cells) | GasGrenade |
| Mutation total density | 800 split across cells | GasGeneration |
| Wall trap density | 1d80+20 per cell | WalltrapGas |
| Gas mask linear reduction | -50 respiratory perf | Power=10 |
| Gas mask save bonus | +10 | Power=10 |
| Steam damage | ceil(0.18 * density) | GasSteam |
| Cryo temp drop | ceil(2.5 * density) | GasCryo |
| Poison damage | floor((perf+1)/20) | GasPoison |
| Ash damage | floor((perf+1)/10) | GasAsh |
| GasDamaging creature divisor | 200 | GasDamaging |
| Save DC formula | 5 + level + perf/10 | Most gas types |
| Fungal spore DC | 10 + level/3 | GasFungalSpores |

---

# 4. Explosives

## 4.1 IGrenade Base Class

**File:** `XRL.World.Parts/IGrenade.cs`

All grenades inherit from `IGrenade`, which handles the **lifecycle** of a grenade:

### Detonation Triggers

| Trigger | Event | Details |
|---------|-------|---------|
| Projectile impact | `ProjectileHit` | When fired from grenade launcher |
| Thrown landing | `AfterThrown` | When thrown and landing at target |
| Before death | `BeforeDeathRemovalEvent` | If `IntProperty("Primed")`, detonates when destroyed |
| Took damage | `TookDamageEvent` | **Any damage = immediate detonation (chain detonation!)** |
| Manual | `InventoryActionEvent("Detonate")` | Player activates from inventory (1000 energy) |
| Examine failure | `ExamineFailureEvent` (25%) / `ExamineCriticalFailureEvent` (50%) | Examining unknown grenades |

### The Detonate() Method (lines 123-145)

```csharp
public bool Detonate(Cell InCell = null, GameObject Actor = null,
                     GameObject ApparentTarget = null, bool Indirect = false)
{
    Cell cell = InCell ?? ParentObject.GetCurrentCell();
    if (cell == null) return false;
    if (ParentObject.CurrentCell != cell) {
        ParentObject.RemoveFromContext();
        cell.AddObject(ParentObject, Forced: true, IgnoreGravity: true);
    }
    if (!BeforeDetonateEvent.Check(ParentObject, Actor, ApparentTarget, Indirect))
        return false;
    return DoDetonate(cell, Actor, ApparentTarget, Indirect); // Subclass implements
}
```

`BeforeDetonateEvent` can cancel detonation (dampening fields, etc.).

### The "Primed" Property

When a grenade is fired as a projectile via `MissileWeapon.SetupProjectile()`, it sets `IntProperty("Primed")` to 1. If the grenade is destroyed without detonating, it still explodes via `BeforeDeathRemovalEvent`.

## 4.2 Core Explosion Algorithm

**File:** `XRL.World.Parts/Physics.cs`, lines 1442-1623 (`ApplyExplosion`)

Uses **breadth-first search (BFS)** to propagate a force wave outward from the epicenter.

### Parameters

```csharp
public static void ApplyExplosion(
    Cell C,                    // Epicenter cell
    int Force,                 // Initial force (e.g., 10000 for standard HE)
    List<Cell> UsedCells,      // Prevents revisiting
    List<GameObject> Hit,      // Prevents double-damage
    bool Local,                // Stay within current zone
    bool Show,                 // Display VFX
    GameObject Owner,          // Kill attribution
    string BonusDamage,        // Additional dice (e.g., "2d6")
    int Phase,                 // Phase of explosion
    bool Neutron,              // Neutron explosion flag
    bool Indirect,             // Indirect/accidental explosion
    float DamageModifier,      // Multiplier on base damage
    GameObject WhatExploded    // Source object for death messages
)
```

### Algorithm

1. **Initialize three queues**: cells to process, force at each cell, propagation direction
2. **Enqueue epicenter** with full Force and direction "."
3. **Play VFX**: `ImpactVFXExplosion` or `ImpactVFXNeutronImpact`
4. **Animation delay**: `20 - Force/1000` ms per step (bigger = faster animation)

5. **BFS Loop for each dequeued cell:**
   a. Wake creatures in area
   b. Get adjacent cells (excluding used ones)
   c. **Deal damage** to all objects in current cell via `ExplosionDamage()`
   d. Track total kinetic resistance absorbed
   e. **Shuffle** adjacent cells randomly (adds chaos to explosion shape)
   f. **Process adjacent cells:**
      - Deal damage to objects
      - If object's kinetic resistance > remaining force: remove cell from propagation
      - **Knockback**: If object is moveable, push in propagation direction (random at epicenter)
      - If cell is solid: remove from propagation
   g. **Force distribution**: `remaining = (currentForce - totalResistance) / numPaths`
   h. **Cutoff**: Only propagate if remaining force > 100

### Damage Formula (`ExplosionDamage`, lines 1625-1711)

```csharp
// BASE DAMAGE = DamageModifier * CurrentForce / 250
int damage = (int)(DamageModifier * (float)CurrentForce / 250f);

// ADD BONUS DAMAGE (e.g., "2d6" for HE grenades)
if (!BonusDamage.IsNullOrEmpty())
    damage += BonusDamage.RollCached();

// Apply with "Explosion" or "Neutron Explosion" attribute
GO.TakeDamage(damage, "Explosion", accidental: notEpicenter, ...);

return GO.GetKineticResistance(); // Returns resistance for force absorption
```

**For a standard HE grenade (Force=10000, Damage="2d6"):**
- Epicenter: 10000/250 + 2d6 = **40 + 7 avg = 47 damage**
- Force attenuates outward: `(force - resistance) / numPaths`
- Propagation stops at force <= 100

## 4.3 Kinetic Resistance

**File:** `XRL.World/GetKineticResistanceEvent.cs`

```csharp
int resistance = Object.Weight + Object.GetIntProperty("Anchoring");
// Modified by LinearIncrease, PercentageIncrease, LinearReduction, PercentageReduction
```

- **Heavy objects** (walls) absorb more force, blocking the wave
- **Light objects** get knocked away
- Solid cells block propagation entirely
- Objects pushed if `IsMoveable()` returns true (excludes scenery, terrain features)

## 4.4 Chain Detonation

**The mechanism:** `TookDamageEvent` handler in `IGrenade`:

```csharp
public override bool HandleEvent(TookDamageEvent E) {
    if (E.Object == ParentObject) {
        ParentObject.SplitFromStack();
        if (!Detonate(null, E.Actor, null, Indirect: true))
            return false;
        ParentObject.CheckStack();
    }
    return base.HandleEvent(E);
}
```

**Chain reaction sequence:**
1. Explosion A damages objects in blast radius via `TakeDamage`
2. Grenade B in blast radius receives damage
3. B's `TookDamageEvent` fires -> calls `Detonate(Indirect: true)`
4. B creates a **new** explosion
5. Can chain to C, D, etc.

**Recursion prevention:** `detonating` flag in `HEGrenade`:
```csharp
protected override bool DoDetonate(Cell C, ...) {
    if (detonating) return true; // Prevents re-entrance
    detonating = true;
    ...
}
```

Similarly, `Tinkering_Mine.HandleEvent(TookDamageEvent)` triggers `Boom()` on any positive damage while armed.

## 4.5 Grenade Types

### HEGrenade (High Explosive)
**File:** `XRL.World.Parts/HEGrenade.cs`
- Force: **10000**, Damage: **"2d6"**
- Standard Physics.ApplyExplosion BFS
- Navigation weight: 10

### EMPGrenade
**File:** `XRL.World.Parts/EMPGrenade.cs`
- Radius: **4**, Duration: **"1d2+4"**
- Calls `ElectromagneticPulse.EMP()` -- **fixed radius, not force-based**
- Does NOT use Physics.ApplyExplosion
- Navigation weight: 3

### ThermalGrenade
**File:** `XRL.World.Parts/ThermalGrenade.cs`
- Radius: **1**, TemperatureDelta: **1000** (can be negative for cryo)
- `item.TemperatureChange(TemperatureDelta)` on every cell in radius
- Phase-aware. **Fixed radius, not force-based.**
- Navigation weight: 8

### GasGrenade
**File:** `XRL.World.Parts/GasGrenade.cs`
- Default: PoisonGas, Density 20, Level 1
- Creates gas in center + adjacent cells (9 total)
- Fires `CreatorModifyGas` event for GasTumbler interaction
- Navigation weight: 5

### GravityGrenade
**File:** `XRL.World.Parts/GravityGrenade.cs`
- Force: **1000**, Radius: **2**, ForceDropoff: **500/ring**
- Reality-distortion-based (can be blocked)
- Pulls objects toward center via `Accelerate()`
- Navigation weight: 5

### FlashbangGrenade
**File:** `XRL.World.Parts/FlashbangGrenade.cs`
- Radius: **4**, Duration: **"1d2+4"**
- Applies `Confused(duration, Radius*5, 7, "SensoryConfusion")` to all creatures with Brain
- Phase mismatch: duration reduced to 2/3
- Navigation weight: 2

### PhaseGrenade
**File:** `XRL.World.Parts/PhaseGrenade.cs`
- Toggles phase: removes `Phased` from phased objects, applies to unphased
- Reality-distortion-based
- Navigation weight: 3

### SunderGrenade
**File:** `XRL.World.Parts/SunderGrenade.cs`
- Radius: **1**, Level: **1**
- Calls `Disintegration.Disintegrate()` -- uses Disintegration mutation logic
- Navigation weight: 8

### TimeDilationGrenade
**File:** `XRL.World.Parts/TimeDilationGrenade.cs`
- Range: **9**, Level: **1**
- Creates time dilation field (slows affected creatures)
- Navigation weight: 3

### DeploymentGrenade
**File:** `XRL.World.Parts/DeploymentGrenade.cs`
- Spawns objects from blueprint (default "Forcefield") in radius
- Configurable: Chance, Count, Duration, BlockedBySolid, Seeping
- Can create creatures loyal to thrower
- Navigation weight: 2

## 4.6 Trigger Parts

### ExplodeOnHit (`XRL.World.Parts/ExplodeOnHit.cs`)
- Force: **10000**, Damage: **"0"**
- Triggers on `ProjectileHit` or `AfterThrown`
- Standard Physics.ApplyExplosion

### ExplodeAfterTurns (`XRL.World.Parts/ExplodeAfterTurns.cs`)
- Countdown timer, detonates when Turns <= 0
- Visual warning: expanding red "!" rings
- Force: **10000** default

### DetonateOnHit (`XRL.World.Parts/DetonateOnHit.cs`)
- Makes the **defender** explode on hit
- `SuppressDestroy: true` -- target isn't auto-destroyed
- Percentage chance per hit

### BlastOnHit (`XRL.World.Parts/BlastOnHit.cs`)
- Uses `StunningForce.Concussion()` instead of ApplyExplosion
- Push/stun blast, not damage-dealing

### BoomOnHit (`XRL.World.Parts/BoomOnHit.cs`)
- Chance per hit to make **defender** neutron-explode

## 4.7 Mine System

**File:** `XRL.World.Parts/Tinkering_Mine.cs`

A mine is a container (MineShell) holding any grenade as its `Explosive`.

### Key Properties

```csharp
public int Timer;           // -1 = proximity mine, >0 = timed bomb
public bool Armed;          // Active?
public GameObject Owner;    // Who placed it
public string OwnerAllegiance; // Faction snapshot at placement
public bool PlayerMine;     // Placed by player?
```

### Triggers

| Trigger | Event | Condition |
|---------|-------|-----------|
| Proximity | `ObjectEnteredCellEvent` | `Armed && Timer <= 0 && WillTrigger(obj)` |
| Timer | `EndTurnEvent` | `Armed && Timer > 0`, decrements each turn (blocked by Stasis) |
| Damage | `TookDamageEvent` | `Armed && damage > 0` |
| Broken/Rusted | `EffectAppliedEvent` | Detonates. EMP = disarm instead. |

### WillTrigger() Logic

```csharp
return Actor.IsCombatObject()
    && Actor.PhaseAndFlightMatches(ParentObject)
    && ConsiderHostile(Actor); // Not owner, not allies
```

### ConsiderHostile()

```csharp
if (PlayerMine && Actor.IsPlayerControlled()) return false;
if (Owner valid && Actor.IsHostileTowards(Owner)) return true;
if (OwnerAllegiance matches hostile) return true;
return false;
```

### Boom() Method

```csharp
public bool Boom() {
    GameObject explosive = Explosive;
    SetExplosive(null);
    Cell C = ParentObject.GetCurrentCell();
    ParentObject.RemoveFromContext();
    C.AddObject(explosive, Forced: true, IgnoreGravity: true);
    // Carry over Temporary, Hidden, Phase
    explosive.ForeachPartDescendedFrom((IGrenade p) => !p.Detonate(C, Owner, null, Indirect: true));
    ParentObject.Destroy(null, Silent: true);
}
```

### Disarming

- **Sifrah system** (mini-game): difficulty = explosive tier, rating = Intelligence + bonuses
- **Classic system**: Intelligence save vs DC (9 + tier + mark)
- **Critical failure**: electrical discharge (`3d8` damage) THEN detonation

### Mine Creation (`Tinkering_LayMine.cs`)

```csharp
public static GameObject CreateBomb(GameObject obj, GameObject Actor, int Countdown) {
    gameObject = GameObject.Create("MineShell");
    part = gameObject.GetPart<Tinkering_Mine>();
    part.SetExplosive(obj);
    part.Timer = Countdown; // -1 = proximity, >0 = timed
    part.Arm(Actor);
    // Rename: "HE grenade" -> "HE mine" or "HE bomb"
}
```

### Miner NPC Part (`XRL.World.Parts/Miner.cs`)

NPCs with Miner:
- Generate mine types from `"Explosives {Mark}"` population table
- Hidden difficulty: `12 + Mark * 3`
- Max 15 mines per zone
- Auto-name: "HE miner mk III"

## 4.8 Special Explosion Types

### Neutron Flux (`XRL.Liquids/LiquidNeutronFlux.cs`)

```csharp
WhatExplodes.Explode(15000, Owner, "10d10+250", 1f, Neutron: true);
```
- Force **15000** (50% > standard HE)
- Bonus damage: **10d10+250** (305 avg!)
- Death message: "crushed under the weight of a thousand suns"
- Triggers on: contact, drinking, pouring without containment

### Capacitor (`XRL.World.Parts/Capacitor.cs`)

On death: `ParentObject.Explode(Charge)` -- explosion force equals stored charge.

### FusionReactor (`XRL.World.Parts/FusionReactor.cs`)

50% chance on death, Force 10000. CatastrophicDisable also triggers.

### Dystechnia Mutation (`XRL.World.Parts.Mutation/Dystechnia.cs`)

Critical failure examining/repairing:
```csharp
Object.Explode((int)(complexity * 3000f * Stat.Random(0.8f, 1.2f)), Actor,
               null, 1f, complexity >= 8); // Neutron if complexity >= 8!
```

### Baetyl Hostility (`XRL.World.Parts/BaetylHostility.cs`)

Angered baetyl (HP < 20%):
- Force: **25000**, Damage: **"10d10+75"**, Neutron
- Leaves behind Space-Time Vortex
- One of the most powerful explosions in the game

### QuantumRippler (`XRL.World.Parts/QuantumRippler.cs`)

When reality-stabilized:
```csharp
ParentObject.Explode(20000, E.Effect.Owner, "12d10+300", 1f, Neutron: true);
```
Force 20000, bonus 12d10+300 = 366 avg. Neutron.

## 4.9 Concussion System

**File:** `XRL.World.Parts.Mutation/StunningForce.cs`

Different from Physics.ApplyExplosion -- used by `BlastOnHit` and Stunning Force mutation:

```csharp
public static void Concussion(Cell StartCell, GameObject ParentObject,
    int Level, int Distance, int Phase, GameObject Target, bool Stun, bool Damage)
```

1. BFS from center with distance counter
2. Push objects: `item.Push(direction, Level * 1000, 4)`
3. Damage = up to 3 increments of `"1d3"` or `"1d3 + (Level-1)/2"`
   - 3 increments at center, 2 at distance 1, 1 at distance 2, 0 at 3+
4. Stun: Toughness save vs DC (15 + Level), failure = `Stun(3, 15+Level)`

## 4.10 Terrain Interaction

Explosions interact with terrain through BFS:

1. **Solid cells block propagation**: wave stops at solid walls
2. **Objects in blocked cells still take damage** -- walls can be destroyed
3. **Heavy walls** absorb force via kinetic resistance, reducing propagation
4. **Zone edges**: `if (Local && (x == 0 || x == 79 || y == 0 || y == 24))` -- explosions don't cross zone boundaries when Local

### IsExplosiveEvent

```csharp
public static bool Check(GameObject Object) {
    // HEGrenade, ExplodeOnHit return false (= "yes, explosive")
    return !flag; // Inverted: event blocked = is explosive
}
```
Used by PointDefense to identify incoming explosives for interception.

### Point Defense (`XRL.World.Parts/PointDefense.cs`)

Intercepts explosive projectiles:
- Explosives: 100% base detection
- Thrown weapons: 100%
- Arrows: 100%
- Slugs/Energy: 0%
- Fires counter-projectile via `ProjectileMovingEvent`

## 4.11 Explosives Formula Summary

| Parameter | Formula/Value |
|-----------|---------------|
| **Base damage** | `DamageModifier * CurrentForce / 250` |
| **Bonus damage** | Additional dice (e.g., "2d6" for HE) |
| **Force propagation** | `(currentForce - totalKineticResistance) / numPaths` |
| **Propagation cutoff** | Force <= 100 |
| **Kinetic resistance** | `Object.Weight + Anchoring` |
| **Knockback direction** | Propagation direction (random at epicenter) |
| **Animation speed** | `20 - Force/1000` ms/step |
| **HE Grenade** | Force 10000, "2d6" bonus |
| **Neutron Flux** | Force 15000, "10d10+250" bonus |
| **Baetyl** | Force 25000, "10d10+75", Neutron |
| **Quantum Rippler** | Force 20000, "12d10+300", Neutron |
| **Dystechnia** | Force = `complexity * 3000 * Random(0.8, 1.2)` |
| **Mine hidden DC** | `12 + Mark * 3` |
| **Mine disarm DC** | `9 + tier + mark` |
