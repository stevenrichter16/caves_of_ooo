# Caves of Qud — Status Effects System: Deep Dive

> Compiled from decompiled source code at `/Users/steven/qud-decompiled-project/`
> 391 effect files in `XRL.World.Effects/`, core infrastructure in `XRL.World/`

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Effect Base Class](#2-effect-base-class)
3. [Effect Type Bitmask System](#3-effect-type-bitmask-system)
4. [Effect Storage: EffectRack](#4-effect-storage-effectrack)
5. [StatShifter: Temporary Stat Modifications](#5-statshifter-temporary-stat-modifications)
6. [Effect Application Pipeline](#6-effect-application-pipeline)
7. [Effect Removal Pipeline](#7-effect-removal-pipeline)
8. [Duration and Turn Tracking](#8-duration-and-turn-tracking)
9. [Saving Throw System](#9-saving-throw-system)
10. [Resistance and Immunity Layers](#10-resistance-and-immunity-layers)
11. [Physical Status Effects (10 Core)](#11-physical-status-effects)
12. [Elemental and Environmental Effects](#12-elemental-and-environmental-effects)
13. [Phase System](#13-phase-system)
14. [Disease System](#14-disease-system)
15. [Gas System](#15-gas-system)
16. [Combat Stance Effects](#16-combat-stance-effects)
17. [Tonic System](#17-tonic-system)
18. [Cooking System](#18-cooking-system)
19. [Mind Control Effects](#19-mind-control-effects)
20. [Movement Effects](#20-movement-effects)
21. [Rendering and Display](#21-rendering-and-display)
22. [Serialization](#22-serialization)
23. [Complete Effect Catalog (391 files)](#23-complete-effect-catalog)

---

## 1. Architecture Overview

Effects in Qud are **parallel to Parts** — both inherit from `IComponent<GameObject>` and participate identically in the event system via `WantEvent`/`HandleEvent`. The key difference:

- **Parts** live in `_PartsList` on a GameObject — permanent components defining what an entity *is*
- **Effects** live in `_Effects` (an `EffectRack`) — temporary states defining what is *happening to* an entity

Effects are dispatched **after** Parts in the event handling chain (see `HandleEventInner` in GameObject.cs). This means Parts can intercept and modify events before Effects see them.

### Core Files

| File | Purpose |
|------|---------|
| `XRL.World/Effect.cs` (678 lines) | Base class for all effects |
| `XRL.World/EffectRack.cs` (35 lines) | Typed collection extending `Rack<Effect>` |
| `XRL.World/StatShifter.cs` (257 lines) | Temporary stat modification manager |
| `XRL.World/GameObject.cs` | ApplyEffect, RemoveEffect, CleanEffects, HasEffect, GetEffect |
| `XRL.Rules/Stat.cs` | RollSave, MakeSave — saving throw mechanics |
| `XRL.World/ApplyEffectEvent.cs` | Gatekeeper event for effect application |
| `XRL.World/EffectAppliedEvent.cs` | Notification event after application |
| `XRL.World/EffectRemovedEvent.cs` | Notification event after removal |

---

## 2. Effect Base Class

**File**: `XRL.World/Effect.cs` — 678 lines

```csharp
[Serializable]
[HasWishCommand]
public class Effect : IComponent<GameObject>
```

### Fields

```csharp
[NonSerialized] public GameObject _Object;        // owning game object
[NonSerialized] public StatShifter _StatShifter;   // lazy-initialized stat modifier
[NonSerialized] public Guid ID;                    // unique instance identifier
[NonSerialized] public string DisplayName;         // human-readable name
[NonSerialized] public int Duration;               // remaining turns (9999 = indefinite)
[NonSerialized] private string _ClassName;         // cached Type.Name
```

All fields are `[NonSerialized]` — the class uses custom `Save`/`Load` static methods with block-framing for error recovery.

### Constructor

```csharp
public Effect()
{
    ID = Guid.NewGuid();
    DisplayName = "";
}
```

### Core Lifecycle Methods

| Method | Signature | Purpose |
|--------|-----------|---------|
| `Apply` | `virtual bool Apply(GameObject Object)` | Called when effect is applied. Return false to reject. |
| `Applied` | `virtual void Applied(GameObject Object)` | Called after successful application. |
| `Remove` | `virtual void Remove(GameObject Object)` | Called when effect is removed. |
| `Expired` | `virtual void Expired()` | Called when duration naturally reaches 0. |

### Event Registration

Effects register for events identically to Parts:

```csharp
// Modern typed event system
public override bool WantEvent(int ID, int cascade) { ... }
public override bool HandleEvent(SomeEvent E) { ... }

// Legacy string-based event system
public override void Register(GameObject Object, IEventRegistrar Registrar) { ... }
public override bool FireEvent(Event E) { ... }
```

### Key Properties

- **`StatShifter`**: Lazy-initialized on first access. Provides clean stat modification with automatic cleanup.
- **`ClassName`**: Cached `GetType().Name` to avoid GC allocation.
- **`ApplySound`/`RemoveSound`**: Virtual properties for SFX. Negative psionic effects get `"sfx_statusEffect_spacetimeWeirdness"`, negative mental get `"sfx_statusEffect_mentalImpairment"`.

---

## 3. Effect Type Bitmask System

Every effect returns a bitmask from `GetEffectType()` combining mechanism types (lower 24 bits) and classification types (upper bits).

### Mechanism Types (how the effect works)

| Constant | Value | Meaning | Applicability Rule |
|----------|-------|---------|-------------------|
| `TYPE_GENERAL` | 1 | Generic/catch-all | Always applies |
| `TYPE_MENTAL` | 2 | Affects the mind | Requires Brain |
| `TYPE_METABOLIC` | 4 | Metabolic process | Requires Stomach |
| `TYPE_RESPIRATORY` | 8 | Breathing-based | Requires Stomach |
| `TYPE_CIRCULATORY` | 16 | Blood-based | Requires bleeding capability |
| `TYPE_CONTACT` | 32 | Surface contact | Fails on liquid/gas/plasma |
| `TYPE_FIELD` | 64 | Field effect | Always applies |
| `TYPE_ACTIVITY` | 128 | Bodily activity | Requires Body |
| `TYPE_DIMENSIONAL` | 256 | Dimensional | Always applies |
| `TYPE_CHEMICAL` | 512 | Chemical reaction | Fails on plasma |
| `TYPE_STRUCTURAL` | 1024 | Structural change | Fails on liquid/gas/plasma |
| `TYPE_SONIC` | 2048 | Sound-based | Fails on plasma |
| `TYPE_TEMPORAL` | 4096 | Time-based | Always applies |
| `TYPE_NEUROLOGICAL` | 8192 | Nerve-based | Always applies |
| `TYPE_DISEASE` | 16384 | Disease | Always applies |
| `TYPE_PSIONIC` | 32768 | Psionic power | Always applies |
| `TYPE_POISON` | 65536 | Poison | Always applies |
| `TYPE_EQUIPMENT` | 131072 | Equipment-sourced | Always applies |

### Classification Types (what kind of effect)

| Constant | Value | Meaning |
|----------|-------|---------|
| `TYPE_MINOR` | 16777216 (0x01000000) | Minor effect |
| `TYPE_NEGATIVE` | 33554432 (0x02000000) | Negative/debuff |
| `TYPE_REMOVABLE` | 67108864 (0x04000000) | Can be explicitly removed |
| `TYPE_VOLUNTARY` | 134217728 (0x08000000) | Voluntarily assumed |

### Bitmask Constants

```csharp
public const int TYPES_MECHANISM = 16777215;    // lower 24 bits
public const int TYPES_CLASS = 251658240;        // upper classification bits (0x0F000000)
public const int DURATION_INDEFINITE = 9999;     // never decrements
```

### Query Methods

```csharp
bool IsOfType(int Mask)   // true if ANY bit in Mask matches (partial match)
bool IsOfTypes(int Mask)  // true if ALL bits in Mask match (full match)
```

### Applicability Checks by Matter Phase

```csharp
static bool CanEffectTypeBeAppliedTo(int Type, GameObject Object)
```

This master check validates:
- Mental (bit 2) → requires Brain part
- Metabolic/Respiratory (bits 4, 8) → requires Stomach
- Circulatory (bit 16) → requires bleeding capability
- Activity (bit 128) → requires Body
- Then checks matter phase: solid always OK, liquid rejects Contact+Structural, gas rejects Contact+Metabolic+Structural, plasma rejects Contact+Metabolic+Chemical+Structural+Sonic

---

## 4. Effect Storage: EffectRack

### Inheritance Chain

```
Container<T>       (abstract: Items[], Size, Length, Variant; zero-alloc struct enumerator)
  └── Rack<T>      (concrete: Add, Remove, RemoveAt, Insert, Clear, Sort, Shuffle)
       └── EffectRack  (thin subclass: FinalizeRead for deserialization)
```

**`Container<T>`** provides the backing array with automatic growth (doubles capacity), a mutation counter (`Variant`) for enumerator safety, and a zero-allocation struct enumerator.

**`Rack<T>`** adds the full mutable list API with `TakeAt` (remove-and-return), span access, and Fisher-Yates shuffle.

**`EffectRack`** adds only `FinalizeRead` for deserialization — iterates all effects calling `ApplyRegistrar` and `FinalizeRead`, handling collection mutation during iteration.

---

## 5. StatShifter: Temporary Stat Modifications

**File**: `XRL.World/StatShifter.cs` — 257 lines

The universal mechanism for effects to temporarily modify stats. Nearly all effects that alter stats use this.

### Data Structure

```csharp
public Dictionary<string, Dictionary<string, Guid>> ActiveShifts;
// Outer key: target GameObject.ID
// Inner key: "statName" or "statName:base"
// Value: Guid returned by Statistic.AddShift()
```

### Core API

```csharp
// Set or update a stat shift (amount=0 removes it)
bool SetStatShift(string statName, int amount, bool baseValue = false)
bool SetStatShift(GameObject target, string statName, int amount, bool baseValue = false)

// Get current shift amount
int GetStatShift(string statName, bool baseValue = false)
int GetStatShift(GameObject target, string statName, bool baseValue = false)

// Remove all shifts
void RemoveStatShifts()                    // all targets
void RemoveStatShifts(GameObject target)   // specific target
void RemoveStatShift(GameObject target, string stat, bool baseValue = false)  // single stat
```

### Key Behaviors

- **Idempotent**: Calling `SetStatShift` with the same stat updates the existing shift via `Statistic.UpdateShift()`. Setting amount to 0 removes it.
- **Cross-object shifts**: Can modify stats on objects other than the Owner (display name uses `Grammar.MakePossessive`).
- **Base value support**: The `baseValue` flag appends `:base` to the key and modifies the stat's base value instead of bonus.
- **Automatic cleanup**: Effects call `RemoveStatShifts()` in their `Remove()` method — guaranteed cleanup on effect removal.

### Common Usage Pattern

```csharp
// In Apply():
base.StatShifter.SetStatShift("DV", -5);
base.StatShifter.SetStatShift("Speed", -70);

// In Remove():
base.StatShifter.RemoveStatShifts();
```

---

## 6. Effect Application Pipeline

**File**: `XRL.World/GameObject.cs`, `ApplyEffect` at line 5726

### Normal Apply: `ApplyEffect(Effect E, GameObject Owner = null)`

```
1. Check "NoEffects" tag (bail unless "ForceEffects" property set)
2. ApplyEffectEvent.Check(this, E.ClassName, E, Owner)
   a. Effect.CanBeAppliedTo(obj)  →  type/matter-phase check
   b. Fire MinEvent (ApplyEffectEvent)  →  any handler can veto
   c. Fire legacy "ApplyEffect" string event  →  any handler can veto
3. If effect can't apply to stacks, split stack first
4. Set E.Object = this
5. Play E.ApplySound
6. Call E.Apply(this)  →  effect's own logic, return false to reject
7. Effects.Add(E)  →  add to EffectRack
8. E.ApplyRegistrar(this)  →  register for events
9. E.Applied(this)  →  post-apply callback
10. EffectAppliedEvent.Send(this, E.ClassName, E)  →  notification (not vetoed)
11. CheckStack()
```

### Force Apply: `ForceApplyEffect(Effect E, GameObject Owner = null)`

```
1. ForceApplyEffectEvent.Check(this, E.ClassName, E)
   - Only checks CanBeAppliedTo + ForceApplyEffect handlers
   - If this fails, falls back to normal ApplyEffect(E)
2. ApplyEffectEvent.Check() is called but its return value is IGNORED
3. Same steps 3-11 as normal apply
4. Additionally fires EffectForceAppliedEvent.Send()
```

### Event Hierarchy

```
MinEvent
  └── IEffectCheckEvent (abstract: Name, Duration; CascadeLevel=0)
       └── IActualEffectCheckEvent (abstract: Effect, Actor)
            ├── ApplyEffectEvent      — gatekeeper, can veto
            ├── ForceApplyEffectEvent  — gatekeeper, only CanBeAppliedTo
            ├── EffectAppliedEvent     — notification after apply
            ├── EffectForceAppliedEvent — notification after force-apply
            └── EffectRemovedEvent     — notification after removal
```

---

## 7. Effect Removal Pipeline

### Core Primitive: `RemoveEffectAt(int Index)`

```csharp
public Effect RemoveEffectAt(int Index, bool NeedStackCheck = true)
{
    Effect effect = _Effects.TakeAt(Index);     // remove from rack
    PlayWorldSound(effect.RemoveSound);          // play sound
    effect.Remove(this);                         // effect's cleanup logic
    effect.ApplyUnregistrar(this);               // unregister from events
    effect.Object = null;                        // sever reference
    EffectRemovedEvent.Send(this, effect.ClassName, effect);  // notify
    if (NeedStackCheck) CheckStack();
    return effect;
}
```

### Removal Overloads (11 total)

| Method | Behavior |
|--------|----------|
| `RemoveEffect(Effect E)` | Find by reference, remove |
| `RemoveEffect(Type)` | Find first by exact type |
| `RemoveEffect<T>()` | Find first by generic type |
| `RemoveEffect(Predicate<Effect>)` | Find first matching predicate |
| `RemoveEffect(Type, Predicate)` | Find first matching type + predicate |
| `RemoveEffectDescendedFrom<T>()` | Find first assignable to T |
| `RemoveAllEffects()` | Remove all effects (reverse iteration) |
| `RemoveAllEffects<T>()` | Remove all of exact type |
| `RemoveEffectsOfType(int Mask)` | Remove all matching ALL bits in mask |
| `RemoveEffectsOfPartialType(int Mask)` | Remove all matching ANY bit in mask |

### CleanEffects — Automatic Expiration

Called at the end of `EndTurnEvent`:

```csharp
public bool CleanEffects()
{
    for (int i = 0; i < effects.Count; i++)
    {
        if (effect.Duration <= 0)
        {
            effect.Expired();           // notify the effect
            RemoveEffect(effect);       // full removal pipeline
            i = -1;                     // restart from beginning
            count = effects.Count;      // re-read length
        }
    }
}
```

The loop restarts from the beginning each time an expired effect is removed, ensuring all expired effects are cleaned even if removal triggers side effects that modify the list.

---

## 8. Duration and Turn Tracking

### Two Duration Strategies

**Strategy 1: Standard Duration Countdown** (`UseStandardDurationCountdown() → true`)

The Effect base class has built-in `WantEvent`/`HandleEvent` for:
- **Objects WITH a Brain**: Decrement on `BeforeBeginTakeActionEvent` (per-action countdown)
- **Objects WITHOUT a Brain**: Decrement on `EndTurnEvent` (per-turn countdown)
- **Duration 9999**: Never decremented (indefinite)

**Strategy 2: Manual Management** (`UseStandardDurationCountdown() → false`, the default)

The effect handles its own duration in its event handlers. Used by effects with complex duration logic (Poisoned, Bleeding, Stun — which check saves each turn).

### Zone Thaw Support

```csharp
virtual bool UseThawEventToUpdateDuration() → false (default)
```

If true, duration is reduced by frozen ticks when a zone thaws (handles the case where time passes while a zone is frozen/unloaded).

### Common Duration Patterns

| Effect | Duration | Strategy |
|--------|----------|----------|
| Poisoned | 4-10 | Manual (BeforeBeginTakeAction) |
| Bleeding | Indefinite (save-based) | Manual (EndTurn, Toughness save) |
| Stun | 1-4 | Manual (BeginTakeAction, Toughness save) |
| Confused | 15-60 | Manual (BeginTakeAction) |
| Paralyzed | 2-5 | Standard countdown |
| Blind | 20-30 | Standard countdown |
| Flying | 9999 | Standard countdown (never decrements) |
| Longblade Stances | 9999 | Standard countdown (never decrements) |

---

## 9. Saving Throw System

**File**: `XRL.Rules/Stat.cs`

### Stat Modifier Formula

```csharp
public static int GetScoreModifier(int Score)
{
    return (int)Math.Floor((Score - 16) * 0.5);
}
```

This is Qud's equivalent of D&D's ability modifier, but centered on 16 instead of 10.

### RollSave

```
1. Roll 1d20 (NaturalRoll)
2. Roll = NaturalRoll + Defender.StatMod(Stat)
3. Difficulty = BaseDifficulty + Attacker.StatMod(AttackerStat ?? Stat)
4. Fire ModifyAttackingSaveEvent on Attacker (can modify Roll and Difficulty)
5. Fire ModifyOriginatingSaveEvent on Source (can modify Roll and Difficulty)
6. Fire ModifyDefendingSaveEvent on Defender (can modify Roll and Difficulty)
7. Play save sound effect (mental or physical based on stat)
```

### MakeSave (Pass/Fail Determination)

```
Success if ANY of:
  - Player has godmode (IDKFA) and not IgnoreGodmode
  - NaturalRoll == 20 and not IgnoreNatural20
  - NaturalRoll != 1 (or IgnoreNatural1) AND Roll >= Difficulty

Failure if:
  - NaturalRoll == 1 and not IgnoreNatural1 (automatic failure)
  - Roll < Difficulty
```

Returns `SuccessMargin` (Roll - Difficulty) or `FailureMargin` (Difficulty - Roll).

### SaveChance (Percentage Calculation)

Computes the theoretical percentage chance of making a save:
```
chances = 21 - Difficulty + Roll
clamped to [0 if nat1 matters, 19 if nat20 matters]
percentage = chances * 5
```

### Common Save Examples

| Effect | Save Stat | Base Difficulty | Notes |
|--------|-----------|----------------|-------|
| Stun | Toughness | 11 + Tier*4 | Each turn while stunned |
| Bleeding | Toughness | 20 (decreasing by 1/turn) | Each turn |
| Confused | — | — | No save (pure duration) |
| Paralyzed | — | — | No save (pure duration) |
| Sleep Gas | Toughness | 5 + Level + performance/10 | On exposure |
| Poison Gas | Toughness | (via performance) | On exposure |

---

## 10. Resistance and Immunity Layers

Effects must pass through 5 layers before being applied:

### Layer 1: Effect Type Compatibility

`Effect.CanBeAppliedTo(GameObject)` checks the bitmask against the target's composition:
- Mental effects need a Brain
- Metabolic/Respiratory need a Stomach
- Circulatory needs bleeding
- Activity needs a Body
- Matter phase checks (solid/liquid/gas/plasma)

### Layer 2: CanApply Named Events

Individual effects fire named events that parts can intercept:
```csharp
Object.FireEvent("ApplyPoison")     // Poisoned
Object.FireEvent("ApplyStun")       // Stun
Object.FireEvent("CanApplyFear")    // Terrified
Object.FireEvent("ApplyPhased")     // Phased
Object.FireEvent("ApplyRusted")     // Rusted
Object.FireEvent("ApplyBlind")      // Blind
Object.FireEvent("ApplyDomination") // Dominated
```

Any handler returning false blocks the effect.

### Layer 3: ApplyEffectEvent (Typed Event)

`ApplyEffectEvent.Check()` fires the typed MinEvent, allowing parts/effects to veto:
```csharp
if (obj.WantEvent(ID, CascadeLevel) && !obj.HandleEvent(FromPool(Name, Effect, Actor)))
    return false;  // vetoed
```

Then fires legacy string event `"ApplyEffect"` with the Effect as a parameter.

### Layer 4: Saving Throws

Many effects include saves in their `Apply()` or per-turn handlers:
```csharp
if (Stat.MakeSave(Object, "Toughness", SaveTarget, Attacker, null, "Stun"))
{
    Duration = 0;  // broke free
}
```

### Layer 5: Gas Immunity (for gas-applied effects)

`GasImmunity` part blocks `CheckGasCanAffectEvent` for specific gas types:
```csharp
// GasImmunity.cs — simply blocks the event for matching GasType
```

### Gas Application Pipeline (special path)

```
1. Check Object.Respires (has respiratory system)
2. CheckGasCanAffectEvent  →  GasImmunity can block
3. CanApplyEffectEvent (for some gas types)
4. GetRespiratoryAgentPerformanceEvent  →  base = gas density
   - GasMask reduces by Power * 5 (LinearAdjustment)
   - Final = (BaseRating + LinearAdjust) * (100 + PercentAdjust) / 100
5. Saving throw (Toughness vs 5 + level + performance/10)
6. Apply effect if save fails
```

---

## 11. Physical Status Effects

### Poisoned (`Poisoned.cs`, 175 lines)

- **Type**: `117506052` (metabolic + poison + negative + removable)
- **Duration**: 4-10 turns (tier-dependent)
- **Damage**: `ceil(DamageIncrement.Roll / 2)` per turn, DamageIncrement = `Level+"d2"` where Level = Tier*1.5
- **Stacking**: Takes max of Duration/DamageIncrement/Level from existing poison
- **Healing interaction**: Healing halved, Regeneration fully blocked, Recuperating clears poison
- **Expiry**: Applies `Ill` effect with duration based on Level
- **Render**: Green heart (`♥` in `&G`) on frames 36-44 of 60-frame cycle
- **Special**: Poison damage suppressed while standing in GasPoison

### Bleeding (`Bleeding.cs`, 392 lines — most complex)

- **Type**: `117440528` (circulatory + negative + removable)
- **Duration**: Indefinite (save-based)
- **Damage**: Dice expression (e.g. "1d2-1" to "3-4") per turn
- **Save**: Toughness vs SaveTarget (starts ~25, **decreases by 1 each turn** making recovery progressively easier)
- **Stacking**: Upgrades SaveTarget/Damage to higher values
- **Requires**: `Bleeds` property > 0
- **Blood system**: 50% chance to bloody objects in cell, 5% chance to create liquid blood pool matching creature's circulatory liquid type
- **Render**: Red heart (`♥` in `&r`) on frames 46-54
- **Bandaging**: Has `Bandaged` flag for wound treatment

### Stun (`Stun.cs`, 219 lines)

- **Type**: `117440514` (mental + negative + removable)
- **Duration**: 1-4 turns
- **Effect**: DV set to 0 (stat shift), turn forfeited
- **Save**: Toughness vs 11 + Tier*4 each turn (success clears immediately, negative SaveTarget = auto-fail)
- **Apply**: Requires Brain. Stacks by adding durations, upgrading SaveTarget.
- **Movement**: `IsMobile` returns false
- **Render**: `!` in cyan on frames 11-24

### Confused (`Confused.cs`, 231 lines)

- **Type**: `117440514` (mental + negative + removable)
- **Duration**: 15-60 turns
- **Stat penalties**: -Level to DV, -Level to MA, -MentalPenalty to Willpower/Intelligence/Ego
- **NPC behavior**: 50% chance random movement, 10% chance random target
- **Stacking**: Cannot stack (returns false if already confused)
- **Remove**: Clears Brain goals and targets
- **Render**: `?` in red on frames 36-24

### Paralyzed (`Paralyzed.cs`, 235 lines)

- **Type**: `117440516` (metabolic + negative + removable)
- **Duration**: "1d3+1" to "1d3+scaled" (2-6 turns typically)
- **Effect**: DV set to 0, `PreventAction = true` (cannot act at all)
- **Save**: **NO per-turn save** — pure duration, unlike Stun
- **Movement**: IsMobile, CanChangeBodyPosition, CanMoveExtremities all return false
- **Stacking**: Extends to higher duration
- **Render**: `X` in cyan on frames 16-29

### Asleep (`Asleep.cs`, 531 lines — second most complex)

- **Type**: `117440514` (mental), OR'd with `0x8000000` if Voluntary
- **Duration**: 40-200 turns (tier-dependent), or 9999 for indefinite
- **Stat penalties**: -12 DV
- **Attacker bonus**: +4 penetration against sleeping targets
- **Wake mechanics**: Damage wakes creature in a `Dazed` state (3-4 turns). Same-turn damage doesn't wake.
- **Applies**: `Prone` effect (unless robot or explicitly disabled)
- **Forced vs Voluntary**: Forced sleep applies `Wakeful` effect (3-5 turns) on wake, preventing immediate re-sleep
- **Indefinite sleep**: 0.1% per-turn chance for player to wake spontaneously
- **Robot support**: Displays as "sleep mode", requires Tinkering to wake
- **Render**: `z` in cyan on frames 11-24
- **Dream text**: Markov chain dream generation with species-specific filters

### Prone (`Prone.cs`, 327 lines)

- **Type**: `117440640` (activity + negative + removable), OR'd with `0x8000000` if Voluntary
- **Duration**: Defaults to 1
- **Stat penalties**: -6 Agility, -5 DV, +80 MoveSpeed
- **Standing up**: Costs a full turn (1000 energy), involuntary prone auto-stands on next action
- **Voluntary**: Standing up is automatic when leaving a cell
- **Requires**: "Feet" or "Roots" body part with Mobility > 0
- **Interactions**: Removes Sitting effect on apply, ends linked Immobilized effects on remove
- **Render**: `_` in red (involuntary) or cyan (voluntary) on frames 36-44

### Blind (`Blind.cs`, 68 lines — simplest)

- **Type**: `117448704`
- **Duration**: 20-30 turns
- **Uses standard countdown**: Yes
- **Effect**: Clears the zone's entire light map for the player (everything goes dark)
- **Application**: Fires "ApplyBlind" event — return value determines success
- **Details**: "Can't see."

### Burning (`Burning.cs`, 85 lines — lightweight marker)

- **Type**: `33554944`
- **Duration**: Managed by Physics temperature system
- **Damage scaling** (based on temperature above FlameTemperature):
  - ≤100 above: "1"
  - 101-300: "1-2"
  - 301-500: "2-3"
  - 501-700: "3-4"
  - 701-900: "4-5"
  - 900+: "5-6"
- **Note**: The Burning effect is a **status indicator only**. Actual fire damage is computed by `Physics.UpdateTemperature()`.

### Frozen (`Frozen.cs`, 39 lines — simplest of all)

- **Type**: `33555456`
- **Duration**: Managed by Physics temperature system
- **Effect**: Triggers `MovementModeChanged("Frozen", Involuntary: true)`
- **CanApplyToStack**: Returns true (items in stacks can freeze)
- **Note**: Like Burning, this is a **marker effect**. Actual freezing/brittleness/shattering logic lives in `Physics.UpdateTemperature()`.

---

## 12. Elemental and Environmental Effects

### Temperature System (`Physics.cs`)

The core thermodynamics engine that drives Burning and Frozen:

**Constants:**
| Property | Default | Meaning |
|----------|---------|---------|
| `_Temperature` | 25 | Ambient/current temp |
| `FlameTemperature` | 350 | Ignition point |
| `VaporTemperature` | 10000 | Vaporization point |
| `FreezeTemperature` | 0 | Freezing point |
| `BrittleTemperature` | -100 | Deep freeze/brittle |
| `SpecificHeat` | 1.0 | Thermal capacity |

**`UpdateTemperature()` pipeline (runs on EndTurnEvent):**
1. Temperature drifts toward ambient at 2% rate per turn, with `ThermalInsulation` dead zone (default 5)
2. Heat/Cold resistance slows ambient return
3. Radiates temperature to adjacent cells
4. **If vaporizing** (≥10000): fires VaporizedEvent, kills object, optionally creates VaporObject gas
5. **If aflame** (≥350): applies Burning effect, deals fire damage
6. **If frozen** (≤-100): fires FrozeEvent, applies Frozen effect; on thaw fires ThawedEvent

**Temperature change modes:**
- **Radiant**: `Temperature += (Amount - Temperature) * 0.035 / SpecificHeat` (asymptotic approach)
- **Direct**: `Temperature += Amount / SpecificHeat`, with resistance applied for threshold-crossing amounts
- `FattyHump` mutation halves direct temperature changes

### Other Elemental Effects

| Effect | Key Behavior |
|--------|-------------|
| `CoatedInPlasma` | -100 to ALL elemental resistances |
| `ContainedAcidEating` | Acid damage to containers, volume-based formula |
| `Rusted` | Requires Metal part. Creatures: -70 Quickness. Items: can't equip, second rust = destroyed ("reduced to dust") |
| `ElectromagneticPulsed` | Cascading EMP to inventory, Duration 50-300 |
| `LiquidCovered` (535 lines) | Tracks liquid volume, handles dripping (fluidity), evaporation, staining, cleansing. Affects conductivity and weight. |

---

## 13. Phase System

Three phase states, each an Effect:

### Phased (`Phased.cs`)
- Can't physically interact with non-phased entities
- Can pass through solid objects
- On removal: if overlapping a solid, attempts push to adjacent cell. If no cell available → **death by Pauli exclusion principle** (achievement unlockable)
- `RealityStabilizeEvent` forces phase-in with 2d6 damage
- Astral creatures get special flickering render (400-frame cycle)
- Removed when entering world map

### Nullphased (`Nullphased.cs`)
- Counter-phased state (can only interact with other nullphased)

### Omniphase (`Omniphase.cs`)
- Can interact with objects in ALL phases simultaneously

---

## 14. Disease System

Diseases follow an **onset → full disease** progression pattern.

### Glotrot (`Glotrot.cs`, `GlotrotOnset.cs`)

**Onset**: Applied by `GasDisease` (even zone Z levels). Toughness saves every 1200 turns. 3 failures or 5 days → full disease.

**Full disease — 3 stages** (progression every 1200 turns via `Count` field):
- **Stage 1**: Tongue bleeds when eating/drinking
- **Stage 2**: More bleeding
- **Stage 3**: Tongue lost, can't speak (adds `GlotrotFilter` to conversations)

**Cure**: Drink "flaming ick" — a 3-component liquid where the container is on fire. On cure: tongue regrows. Regeneration level 5+ also regrows tongue.

**Trade penalty**: -3 to trade performance.

### Ironshank (`Ironshank.cs`, `IronshankOnset.cs`)

**Onset**: Applied by `GasDisease` (odd zone Z levels). Same onset pattern.

**Full disease**: Progressive leg bone fusion.
- MoveSpeed penalty starts 6-10, increases by 6-10 every 4800 turns (max 80)
- AV bonus = penalty/15 (compensation for immobility)
- Requires infectable Feet body part with mobility > 0

**Cure**: Drink liquid matching game state "IronshankCure" + gel. Penalty reduces by 6-10 per 1200-turn cycle. Cured when penalty reaches 0.

### Monochrome (`Monochrome.cs`, `MonochromeOnset.cs`)
- Greyscale shader effect
- Visual/aesthetic disease

### Fungal Spore Infection (`FungalSporeInfection.cs`)
- Timed infection: after `TurnsLeft` expires (20-30 * 120 turns), equips a fungal infection item on a body part
- Body part selection prefers "Fungal Outcrop" type, then suitable contact parts
- `PaxInfection` is special variant (duration=3, sets `HasPax` property)
- `ImmuneToFungus` tag blocks

---

## 15. Gas System

### Core Architecture

**`Gas.cs`** — The core gas component (part, not effect):
- Fields: `_Density` (default 100), `Level`, `Seeping`, `Stable`, `GasType`, `ColorString`, `_Creator`
- Density-based dispersal in `ProcessGasBehavior()`: spreads to adjacent cells, merges with same-type gas
- Dissipates when density drops to 0 or below 10 with 50% chance
- Wind affects dispersal via zone's `CurrentWindSpeed`/`CurrentWindDirection`
- Animated rendering: 4-frame cycle using CP437 glyphs

**`IGasBehavior.cs`** — Abstract base for gas behavior parts. Provides `BaseGas` reference, `GasDensity()`, `GasDensityStepped()` helpers.

**`IObjectGasBehavior.cs`** — Extends IGasBehavior. Default `TurnTick` and `ObjectEnteredCellEvent` handling that calls `ApplyGas(Cell)` / `ApplyGas(GameObject)`.

### Gas Types (16 total)

| Gas Part | GasType | Effect Applied | Save | Notes |
|----------|---------|---------------|------|-------|
| `GasPoison` | Poison | PoisonGasPoison | Toughness | Dmg = Level*2, direct inhalation damage |
| `GasSleep` | Sleep | Asleep | Toughness (5+lvl+perf/10) | Duration 4d6+level, requires Brain |
| `GasConfusion` | Confusion | Confused | Toughness (5+lvl+perf/10) | Duration 4d6+level, requires Brain |
| `GasStun` | Stun | StunGasStun | None | Duration = performance |
| `GasDamaging` | (configurable) | Direct damage | — | Most configurable: TargetPart/Tag/BodyPart |
| `GasCryo` | Cryo | Temperature change | — | -ceil(2.5*density), +1 cold dmg/turn |
| `GasSteam` | Steam | Heat damage | — | Dmg = max(ceil(0.18*density), 1) |
| `GasDisease` | Disease | Ironshank/Glotrot onset | Toughness | Even Z→Ironshank, Odd Z→Glotrot |
| `GasFungalSpores` | FungalSpores | SporeCloudPoison + FungalSporeInfection | Toughness (10+lvl/3) | Checks ImmuneToFungus |
| `GasPlasma` | Plasma | CoatedInPlasma | — | Duration density*2/5 to density*3/5 |
| `GasGlitter` | Glitter | Light refraction | — | Density-based probability |
| `GasShame` | Shame | Shamed | Willpower (not Toughness) | Duration 2d6+level*2 |
| `GasAsh` | Ash | AshPoison | — | Occluding at density≥40 |

### Gas Equipment

| Part | Effect |
|------|--------|
| `GasMask` | Reduces respiratory agent performance by Power*5, adds Power to inhaled gas saves, reduces gas damage by Power% |
| `GasTumbler` | Modifies gas density (2x) and dispersal rate (0.25x) for gases created by wearer |
| `GasImmunity` | Blocks CheckGasCanAffectEvent for matching GasType |
| `GasGrenade` | Spawns gas in adjacent cells on detonation |
| `GasOnHit` | Creates gas on weapon hit |
| `GasOnEntering` | Creates gas on projectile entering cell |

---

## 16. Combat Stance Effects

Three longblade stances — all mutually exclusive (each `Apply()` removes all three):

| Stance | DisplayName | Effect Type | Base Bonus | Improved Bonus |
|--------|------------|-------------|------------|----------------|
| `LongbladeStance_Aggressive` | `{{R|aggressive stance}}` | 128 (activity) | +1 pen, -2 hit | +2 pen, -3 hit |
| `LongbladeStance_Defensive` | `{{G|defensive stance}}` | 128 | +2 DV | +3 DV |
| `LongbladeStance_Dueling` | `{{W|dueling stance}}` | 128 | +2 to hit | +3 to hit |

All have Duration 9999 (permanent while active). The stances hold no stat-shifting logic — the actual combat modifiers are applied by corresponding `LongBlades*` skill parts that check for the effect's presence.

Hidden while asleep (`GetDescription()` returns null if creature has Asleep effect).

### Other Combat Effects

| Effect | Behavior |
|--------|----------|
| `Running` | 2x move speed (3x with Springing, further with Wings). -5 DV (unless Hurdle), -10 melee hit. Ends on melee attack or non-movement actions. |
| `ShieldWall` | Defensive shield stance |
| `Cudgel_SmashingUp` | Cudgel skill effect |
| `Dashing` | Movement ability effect |
| `RifleMark` | Rifle skill targeting |
| `EmptyTheClips` | Rapid fire effect |

---

## 17. Tonic System

### Base Class: `ITonicEffect`

```csharp
public abstract class ITonicEffect : Effect
{
    public override int GetEffectType() => 4;  // TYPE_METABOLIC
    public override bool IsTonic() => true;
    public abstract void ApplyAllergy(GameObject target);
}
```

All tonics are metabolic effects. Each must implement `ApplyAllergy()` for allergy/overdose reactions.

### Tonic Catalog

| Tonic | Effect | Allergy/Overdose |
|-------|--------|-----------------|
| `HulkHoney_Tonic` | +9/6 Str, +HP, -25 MoveSpeed | `HulkHoney_Tonic_Allergy` |
| `Blaze_Tonic` | +20/10 Quickness, +100 Heat Resist | Extreme overheating |
| `Salve_Tonic` | Healing over time | Minor side effects |
| `Ubernostrum_Tonic` | Powerful healing, limb regrowth | Strong side effects |
| `SphynxSalt_Tonic` | Anti-confusion immunity | Confusion vulnerability |
| `ShadeOil_Tonic` | +8 DV, phasing | Phase instability |
| `Skulk_Tonic` | Night vision bonuses | Light sensitivity |
| `Hoarshroom_Tonic` | Cold resistance | Cold vulnerability |
| `Rubbergum_Tonic` | +100 Electric Resist | `Rubbergum_Tonic_Allergy` |
| `LoveTonic` | Beguiling effect | Lovesickness |

### True Kin vs Mutant Variants

Tonics often have different effects for True Kin (cybernetic humans) vs Mutants, typically with True Kin getting stronger base effects but Mutants getting mutation-synergy bonuses.

---

## 18. Cooking System

### Class Hierarchy

```
Effect
  └── BasicCookingEffect           — base: "well fed", listens for hunger events to self-remove
       └── BasicCookingStatEffect  — stat bonus variant
  └── ProceduralCookingEffect      — procedurally generated from ingredients
       └── ProceduralCookingEffectWithTrigger  — triggered variant
```

### BasicCookingEffect (`BasicCookingEffect.cs`)

```csharp
public class BasicCookingEffect : Effect
{
    public string wellFedMessage = "You eat the meal. It's tastier than usual.";
    DisplayName = "{{W|well fed}}";
    Duration = 1;
    GetEffectType() => 67108868;  // TYPE_REMOVABLE + metabolic
}
```

Listens for `BecameHungry`, `BecameFamished`, `ApplyWellFed`, `ClearFoodEffects` — self-removes on any.

### Simple Cooking Effects (8 basic types)

| Effect | Bonus |
|--------|-------|
| `BasicCookingEffect_Hitpoints` | +HP |
| `BasicCookingEffect_MA` | +Mental Armor |
| `BasicCookingEffect_MS` | +Move Speed |
| `BasicCookingEffect_Quickness` | +Quickness |
| `BasicCookingEffect_RandomStat` | Random stat bonus |
| `BasicCookingEffect_Regeneration` | +Regeneration |
| `BasicCookingEffect_ToHit` | +To Hit |
| `BasicCookingEffect_XP` | +XP gain |

### Procedural Cooking: CookingDomain System

~130 CookingDomain unit files organized by domain:

| Domain | Examples |
|--------|----------|
| `CookingDomainAcid` | Corrosive gas breath, acid resistance |
| `CookingDomainAgility` | Agility buffs, shank attacks, crit triggers |
| `CookingDomainArmor` | AV buffs, penetration triggers |
| `CookingDomainCold` | Cold resistance, cryokinesis, ice breath |
| `CookingDomainHeat` | Heat resistance, pyrokinesis, fire breath |
| `CookingDomainElectric` | Electric resistance, discharge, EMP |
| `CookingDomainHP` | Healing, HP increase, damage triggers |
| `CookingDomainPhase` | Phasing, phase duration, phase triggers |
| `CookingDomainReflect` | Damage reflection, quill bursts |
| `CookingDomainStrength` | Strength buffs, dismember/slam |
| `CookingDomainTeleport` | Blink, teleport other, mass teleport |
| `CookingDomainFungus` | Fungus reputation, spore immunity |
| `CookingDomainFear` | Fear immunity, intimidation |
| `CookingDomainMedicinal` | Disease immunity/resistance |
| `CookingDomainPlant` | Plant reputation, burgeoning |

Each domain has Unit files (passive bonuses) and ProceduralCookingTriggeredAction files (activated on conditions like "on damage", "on kill", "on low health").

---

## 19. Mind Control Effects

### Dominated / Dominating (paired effects)

**`Dominated.cs`** — placed on the **target** being controlled:
- Links to `Dominator` object reference
- Adds "End Domination" activated ability
- Ceases combat between dominated creature and dominator's allies
- Supports both mental (`ApplyDomination`) and robotic (`ApplyRoboDomination`) variants
- On removal: fires `DominationEnded`, handles metempsychosis (permanent body transfer)
- Duration managed per-action (BeginTakeActionEvent)
- Prevents joining party leaders

**`Dominating.cs`** — placed on the **dominator's original body**:
- Sets DV to 0 (body is unresponsive)
- Pushes `Dormant` AI goal onto brain
- Handles metempsychosis on death (BeforeDeathRemovalEvent)
- Body's zone can't be frozen
- If a negative effect is applied to the dominator's body, interrupts domination

### Beguiled (`Beguiled.cs`)
- Sets follower relationship
- +5 HP per level bonus
- Lighter than domination — target retains AI

### Proselytized (`Proselytized.cs`)
- Converts target to follower permanently

---

## 20. Movement Effects

| Effect | Speed | DV | Other | Duration |
|--------|-------|----|-------|----------|
| `Running` | 2x (3x w/ Springing) | -5 | -10 melee hit, ends on attack | Timed |
| `Flying` | Normal | Normal | Immune to non-flying melee, ignores terrain, reduces electrical conductivity | 9999 |
| `Burrowed` | Varies | Varies | Underground movement | While active |
| `Swimming` | Varies | Varies | Water movement | While in liquid |
| `Wading` | Reduced | Normal | Shallow water | While in liquid |
| `Submerged` | Reduced | Normal | Deep underwater | While in liquid |
| `Springing` | 3x (with Running) | Normal | Jump boost | Short |

---

## 21. Rendering and Display

### Render Pipeline

Effects participate in multiple render passes:

```csharp
virtual void OnPaint(ScreenBuffer Buffer)     // screen buffer paint
virtual bool Render(RenderEvent E)            // standard render pass
virtual bool OverlayRender(RenderEvent E)     // overlay pass
virtual bool FinalRender(RenderEvent E, bool bAlt)  // final pass
virtual bool RenderTile(ConsoleChar E)        // tile mode
virtual bool RenderSound(ConsoleChar C)       // sound rendering
```

All return `true` by default (continue rendering chain).

### Flashing Indicator Pattern

Most effects flash an indicator character during specific frames of a 60-frame animation cycle:

| Effect | Char | Color | Frames |
|--------|------|-------|--------|
| Poisoned | `♥` (`\u0003`) | `&G^k` (green) | 36-44 |
| Bleeding | `♥` (`\u0003`) | `&r` (red) | 46-54 |
| Stun | `!` | `&C^c` (cyan) | 11-24 |
| Confused | `?` | `&R` (red) | 36-24 |
| Paralyzed | `X` | `&C^c` (cyan) | 16-29 |
| Asleep | `z` | `&C^c` (cyan) | 11-24 |
| Prone (involuntary) | `_` | `&R` (red) | 36-44 |
| Prone (voluntary) | `_` | `&c` (cyan) | 36-44 |
| Running | `→` (`\u001a`) | `&W` (white) | Always |
| Flying | `↑` (`\u0018`) | `&y` (yellow) | Always |

### Display Name Tags

Effects add tags to entity display names:
```csharp
// Rusted
E.AddTag("[{{r|rusted}}]", 20);

// Flying
E.AddTag("[{{B|flying}}]");

// Prone
E.AddTag("[{{C|prone}}]");  // or "[{{C|lying on [object]}}]"
```

### Suppression

```csharp
virtual bool SuppressInLookDisplay()   // hide from look/inspect UI
virtual bool SuppressInStageDisplay()  // hide from stage display
```

---

## 22. Serialization

### Effect Save/Load

```csharp
static void Save(Effect E, SerializationWriter Writer)
{
    // Write block frame for error recovery
    // Write type token (class type)
    // Write Guid ID
    // Write DisplayName
    // Write Duration
    // StatShifter.Save(_StatShifter, Writer)
    // Call E.SaveData(Writer)  →  effect-specific data
}

static Effect Load(GameObject Basis, SerializationReader Reader)
{
    // Read block frame
    // Read type token → Activator.CreateInstance(type)
    // Read Guid, DisplayName, Duration
    // StatShifter.Load(Reader, instance)
    // Call instance.LoadData(Reader)  →  effect-specific data
    // Error recovery via ReadError or SkipBlock
}
```

### StatShifter Serialization

Serializes the `ActiveShifts` dictionary: count → DefaultDisplayName → for each target: ID → shift count → for each shift: key → Guid.

### EffectRack Finalization

After deserialization, `FinalizeRead` iterates all effects calling `ApplyRegistrar` (re-register for events) and `FinalizeRead` (effect-specific post-load), with mutation-safe iteration.

---

## 23. Complete Effect Catalog

**391 total effect files** in `XRL.World.Effects/`

### By Category

**Incapacitation (10)**: Asleep, Blind, Confused, Dazed, Paralyzed, Prone, Sitting, Stun, StunGasStun, Stuck

**Damage Over Time (8)**: Bleeding, Burning, Frozen, Poisoned, PoisonGasPoison, SporeCloudPoison, AshPoison, BasiliskPoison

**Mental/Psionic (12)**: Beguiled, Dominated, Dominating, Terrified, Confused, Lovesick, Proselytized, MemberOfPsychicBattle, DeepDream, WakingDream, SensePsychicEffect, Rebuked

**Disease (7)**: Glotrot, GlotrotOnset, Ironshank, IronshankOnset, Monochrome, MonochromeOnset, FungalSporeInfection

**Elemental (8)**: Burning, Frozen, CoatedInPlasma, ElectromagneticPulsed, Rusted, ContainedAcidEating, ContainedStaticGlitching, Crackling

**Phase (5)**: Phased, Nullphased, Omniphase, PhasedWhileStuck, PhasePoisoned

**Movement (10)**: Running, Flying, Burrowed, Swimming, Wading, Submerged, Dashing, Springing, Hobbled, Hampered

**Combat Stances (4)**: LongbladeStance_Aggressive, LongbladeStance_Defensive, LongbladeStance_Dueling, LongbladeEffect_EnGarde

**Combat Misc (8)**: ShieldWall, Cudgel_SmashingUp, RifleMark, EmptyTheClips, Cripple, ShatterArmor, ShatteredArmor, ShatterMentalArmor

**Tonics (12)**: Blaze_Tonic, Hoarshroom_Tonic, HulkHoney_Tonic, HulkHoney_Tonic_Allergy, LoveTonic, Rubbergum_Tonic, Rubbergum_Tonic_Allergy, Salve_Tonic, ShadeOil_Tonic, Skulk_Tonic, SphynxSalt_Tonic, Ubernostrum_Tonic

**Cooking — Basic (11)**: BasicCookingEffect, BasicCookingStatEffect, BasicCookingEffect_Hitpoints, BasicCookingEffect_MA, BasicCookingEffect_MS, BasicCookingEffect_Quickness, BasicCookingEffect_RandomStat, BasicCookingEffect_Regeneration, BasicCookingEffect_ToHit, BasicCookingEffect_XP, BasicTriggeredCookingEffect, BasicTriggeredCookingStatEffect

**Cooking — Procedural (~130)**: CookingDomain* files across 30+ domains (Acid, Agility, Armor, Artifact, Attributes, Breathers, Burrowing, Cloning, Cold, Darkness, Density, Dissociation, Ego, Electric, Fear, Fungus, Heat, HighTierRegen, HP, LiquidSpitting, Love, LowTierRegen, Medicinal, Phase, PhotosyntheticSkin, Plant, Quickness, Reflect, RegenHighTier, RegenLowTier, Rubber, Secrets, SelfHarm, Special, Stability, Strength, Taste, Teleport, Tongue, Willpower)

**Buffs/Utility (20+)**: BoostStatistic, BoostedImmunity, DistractionImmunity, Emboldened, Ecstatic, Frenzied, FoliageCamouflaged, UrbanCamouflaged, Gleaming, Grounded, Healing, GeometricHeal, Inspired, Invulnerable, Luminous, Meditating, NocturnalApexed, QuantumStabilized, RealityStabilized, Refresh, Wakeful, WarTrance, Trance

**Debuffs (15+)**: Berserk, BlinkingTicSickness, Broken, BrainBrineCurse, CardiacArrest, CyberneticRejectionSyndrome, Disoriented, Distracted, Exhausted, Famished, Flagging, Ill, Lost, MobilityImpaired, Overburdened, Shaken, Shamed, Stressed, Suppressed

**Mutation-Related (8)**: AxonsInflated, AxonsDeflated, Budding, FungalVisionary, FuriouslyConfused, MutationInfection, Mutating, ItemEffectBonusMutation

**Liquid (3)**: LiquidCovered, LiquidStained, Greased

**Vehicle (3)**: Piloting, Unpiloted, VehicleUnpowered

**Temporal (4)**: TimeDilated, TimeDilatedIndependent, TimeCubed, ITimeDilated

**Misc (15+)**: AmbientRealityStabilized, AnemoneEffect, ArtifactDetectionEffect, CorpseTethered, Disguised, Enclosed, Engulfed, Hooked, Incommunicado, Interdicted, IrisdualCallow, IrisdualMolting, LatchedOnto, LifeDrain, Nosebleed, Scintillating, SynapseSnap, WarmingUp

**Abstract/Interface (5)**: IBusted, ICamouflageEffect, IEffectWithPrefabImposter, IShatterEffect, ITonicEffect

### Stacking Patterns

| Pattern | Examples |
|---------|---------|
| **Takes max**: Poisoned (max of Duration/Damage/Level) |
| **Upgrades fields**: Bleeding (higher SaveTarget/Damage) |
| **Adds durations**: Stun (cumulative duration, upgrades SaveTarget) |
| **Extends to max**: Paralyzed (takes longer duration) |
| **Cannot stack**: Confused, Asleep, Prone, Blind (returns false) |
| **Mutually exclusive**: Longblade stances (removes others on apply) |

### Effect Type ID Commonalities

Several effects share the same type bitmask, indicating shared category:
- `117440514`: Stun, Confused, Asleep, Terrified (incapacitating mental effects)
- `128`: All longblade stances (activity effects)
- `33554944`: Burning (negative + field)
- `33555456`: Frozen (negative + dimensional)
- `4`: All tonic effects (metabolic)

---

## Key Architectural Takeaways for Implementation

1. **Effects are parallel to Parts** — same event system, separate storage. Effects fire AFTER parts in the event chain.

2. **Bitmask type system** enables both applicability checks (can this effect apply to gas?) and category queries (is this negative? removable?).

3. **StatShifter is the universal stat modification tool** — lazy-initialized, idempotent, cross-object capable, auto-cleaned on removal.

4. **Duration has two tick modes**: per-action (Brain entities on BeforeBeginTakeAction) and per-turn (non-Brain on EndTurn). 9999 = indefinite.

5. **5-layer resistance system**: type compatibility → named CanApply events → ApplyEffectEvent → saving throws → gas immunity.

6. **Burning and Frozen are lightweight markers** — real thermal logic lives in Physics.UpdateTemperature().

7. **Stacking varies per effect** — no universal stacking rule. Each effect decides in its own Apply().

8. **CleanEffects restarts iteration** from the beginning each time it removes an expired effect, ensuring complete cleanup.

9. **ForceApplyEffect ignores normal vetoes** but still checks CanBeAppliedTo (matter-phase compatibility).

10. **The cooking system alone accounts for ~130 of 391 effect files** through its procedural domain system.
