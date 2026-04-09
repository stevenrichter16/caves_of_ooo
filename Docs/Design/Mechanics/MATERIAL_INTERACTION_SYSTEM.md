# Material Interaction System: Architecture and Fire + Organic Implementation

> Foundation for emergent environmental gameplay in Caves of Ooo.
> Spells, liquids, temperature, and material properties interact through shared simulation rules.
> Based on patterns from Caves of Qud decompiled source analysis.

## Core Thesis

Material interactions are not authored combos. They are consequences of continuous properties crossing thresholds while independent systems read shared state. "Fire + organic = bigger fire" is not a special case. It is what happens when an object with high Combustibility receives enough heat to cross its FlameTemperature.

This system is the bridge between grimoire spells and the environment. Shaping spells modify cell and object properties. The material interaction system reads those properties and produces consequences. The spell never knows what consequence it caused.

---

## Qud Patterns Adopted

These patterns come directly from the decompiled source analysis (see sibling docs in this directory).

### 1. Continuous properties, not binary tags

Qud's `Physics.cs` tracks temperature as a continuous scalar, not a "burning" boolean. Objects warm gradually, cross thresholds at different times based on `SpecificHeat`, and radiate heat proportional to their temperature excess. We adopt this for temperature, moisture, fuel mass, and integrity.

**Source:** `Physics.cs` — `_Temperature`, `FlameTemperature`, `VaporTemperature`, `SpecificHeat`

### 2. Two heat transfer modes

Qud distinguishes radiant heat (asymptotic approach toward source temperature) from direct heat (additive, scaled by SpecificHeat). This creates a tactical difference between standing near danger, stepping into danger, and being hit by danger. We adopt both modes.

**Source:** `Physics.cs` lines 4050-4167 — radiant formula `(Amount - Temperature) * (0.035f / SpecificHeat)`, direct formula divides by SpecificHeat then applies

### 3. Entity + Part + Event composition

Caves of Ooo already uses this pattern. `Entity` has a `List<Part>`, parts register for events via `WantEvent`/`HandleEvent`, and `GameEvent` carries typed parameters. We extend this with new parts and events for material state, not a parallel system.

**Source:** `GameObject.cs`, `IPart.cs`, `MinEvent.cs`

### 4. Effects as temporary state, Parts as permanent capability

Qud separates permanent behavior (`IPart`) from transient conditions (`Effect`). Caves of Ooo already has this split (`Part.cs` vs `Effect.cs` with `StatusEffectsPart`). BurningEffect is an Effect. MaterialPart is a Part. They interact through events.

**Source:** `IPart.cs` vs `Effect` subclasses; `LiquidCovered.cs` as Effect example

### 5. Data-driven reactions

Qud's per-liquid hooks (`MixingWith`, `SmearOn`, `Drank`) let specific materials add behavior without rewriting the core system. We formalize this into `MaterialReactionBlueprint` assets so designers can add new material interactions as data, not code.

### 6. Spatial propagation through cells

Qud's heat radiation iterates adjacent cells. Gas disperses through adjacent cells. Liquids spread through adjacent cells. We use the same `Zone.GetCellInDirection()` pattern for heat spread, fire propagation, and moisture seepage.

**Source:** `Physics.cs` lines 2972-2980 — `CurrentCell.GetLocalAdjacentCells()` for heat radiation

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│                   Entity                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │
│  │ MaterialPart│  │ ThermalPart │  │  FuelPart   │ │
│  │ (what it's  │  │ (temperature│  │ (combustible│ │
│  │  made of)   │  │  state)     │  │  mass)      │ │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘ │
│         │                │                │         │
│  ┌──────┴────────────────┴────────────────┴──────┐  │
│  │          StatusEffectsPart (existing)          │  │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────────┐   │  │
│  │  │ Burning  │ │   Wet    │ │  Smoldering  │   │  │
│  │  │  Effect  │ │  Effect  │ │   Effect     │   │  │
│  │  └──────────┘ └──────────┘ └──────────────┘   │  │
│  └───────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
         │ events │              │ events │
         ▼        ▼              ▼        ▼
┌─────────────────────────────────────────────────────┐
│              MaterialReactionResolver                │
│  Reads: MaterialReactionBlueprint assets (data)      │
│  Listens: ApplyHeatEvent, MaterialContactEvent       │
│  Emits: TryIgniteEvent, MaterialReactedEvent         │
└─────────────────────────────────────────────────────┘
         │                               │
         ▼                               ▼
┌──────────────────┐          ┌──────────────────────┐
│   Cell / Zone    │          │   Grimoire Spells    │
│  (spatial prop-  │          │  (Shaping spells     │
│   agation)       │          │   emit ApplyHeat,    │
│                  │          │   MaterialContact)   │
└──────────────────┘          └──────────────────────┘
```

---

## Part Definitions

### MaterialPart

Declares what an object is made of. Read-only after creation (material doesn't change mid-game; effects change how material behaves).

```csharp
public class MaterialPart : Part
{
    // Primary material identity
    public string MaterialID;           // "Wood", "Cloth", "Iron", "Stone", "Bone", "Flesh"

    // Derived from MaterialID via data table, but overridable per-object
    public float Combustibility;        // 0-100. How readily this ignites.
    public float Conductivity;          // 0-100. Electrical conductivity.
    public float Porosity;              // 0-100. How readily liquids absorb into this.
    public float Volatility;            // 0-100. How readily this vaporizes.
    public float Brittleness;           // 0-100. How easily this shatters when frozen.

    // Tags for reaction matching (an object can have multiple)
    public HashSet<string> MaterialTags; // {"Organic", "Fibrous"} or {"Metal", "Ferrous"}
}
```

**Material tag examples:**

| MaterialID | MaterialTags | Combustibility | Notes |
|-----------|-------------|---------------|-------|
| Wood | Organic, Fibrous | 70 | Burns well, chars |
| Cloth | Organic, Fibrous | 85 | Burns fast, little fuel |
| Flesh | Organic | 30 | Burns poorly when wet |
| Bone | Organic | 10 | Barely burns, brittles when frozen |
| Iron | Metal, Ferrous | 0 | Doesn't burn, conducts electricity and heat |
| Stone | Mineral | 0 | Doesn't burn, high heat capacity |
| Crystal | Mineral, Crystalline | 0 | Doesn't burn, shatters when frozen |

### ThermalPart

Tracks continuous temperature state on an entity. This is the core of fire, freezing, and vaporization.

```csharp
public class ThermalPart : Part
{
    public float Temperature = 25f;         // Current temperature (continuous)
    public float FlameTemperature = 400f;   // Ignition threshold
    public float VaporTemperature = 10000f; // Vaporization threshold
    public float FreezeTemperature = 0f;    // Freeze threshold
    public float BrittleTemperature = -100f;// Shatter threshold
    public float HeatCapacity = 1.0f;       // Thermal inertia (higher = slower to change)
    public float AmbientDecayRate = 0.02f;  // Rate of return to ambient per turn

    // State flags (derived from Temperature vs thresholds)
    public bool IsAflame => Temperature >= FlameTemperature;
    public bool IsFrozen => Temperature <= FreezeTemperature;
    public bool IsBrittle => Temperature <= BrittleTemperature;

    // Event registration
    public override bool WantEvent(string eventID)
        => eventID == "EndTurn"
        || eventID == "ApplyHeat"
        || eventID == "QueryTemperature";
}
```

**EndTurn behavior:**
1. Decay toward ambient: `Temperature += (Ambient - Temperature) * AmbientDecayRate / HeatCapacity`
2. If `IsAflame`: radiate heat to adjacent cells via `ApplyHeat` events (Radiant mode)
3. Check threshold crossings and fire state-change events (`IgnitedEvent`, `FrozeEvent`, etc.)

### FuelPart

How much combustible mass an object has. Without fuel, fire dies even if temperature remains high.

```csharp
public class FuelPart : Part
{
    public float FuelMass = 100f;       // Remaining combustible material (0 = exhausted)
    public float MaxFuel = 100f;        // Starting fuel
    public float BurnRate = 1.0f;       // Fuel consumed per turn while burning
    public float HeatOutput = 1.0f;     // Heat emitted per unit of fuel consumed
    public string ExhaustProduct;       // What remains: "Ash", "Charcoal", "Slag", null

    public bool HasFuel => FuelMass > 0f;
}
```

**Fuel depletion flow:**
1. BurningEffect calls `ConsumeFuel()` each turn
2. Fuel consumed = `BurnRate * BurningEffect.Intensity`
3. Heat emitted = fuel consumed * `HeatOutput` * `MaterialPart.Combustibility / 50f`
4. When `FuelMass <= 0`: BurningEffect ends, SmolderingEffect may begin, ExhaustProduct spawned

---

## Effect Definitions

### BurningEffect

The central fire effect. Not a binary toggle — intensity is continuous.

```csharp
public class BurningEffect : Effect
{
    public float Intensity = 1.0f;      // How fierce the fire is (scales damage + heat output)
    public Entity IgnitionSource;       // Who/what started this fire (attribution)

    public override int EffectType => TYPE_THERMAL;

    public override void OnTurnStart()
    {
        // 1. Consume fuel
        var fuel = Owner.GetPart<FuelPart>();
        if (fuel == null || !fuel.HasFuel)
        {
            Remove(); // No fuel = fire dies
            Owner.ApplyEffect(new SmolderingEffect { Duration = 3 });
            return;
        }
        float consumed = fuel.BurnRate * Intensity;
        fuel.FuelMass -= consumed;

        // 2. Deal burn damage to owner (scales with intensity, 6-tier formula from Qud)
        int damage = GetBurnDamage(Intensity);
        Owner.TakeDamage(damage, "Fire", IgnitionSource);

        // 3. Emit heat to self (reinforces temperature) and adjacent cells
        var thermal = Owner.GetPart<ThermalPart>();
        float heatEmitted = consumed * fuel.HeatOutput * GetCombustibilityMultiplier();
        EmitHeatToSelf(thermal, heatEmitted);
        EmitHeatToAdjacent(heatEmitted);

        // 4. Check for propagation to neighbors
        CheckFireSpread();
    }

    public override bool OnStack(Effect incoming)
    {
        // Stacking increases intensity, doesn't add duplicate effects
        if (incoming is BurningEffect b)
        {
            Intensity = Math.Min(Intensity + b.Intensity * 0.5f, 5.0f);
            return true; // Consumed the incoming effect
        }
        return false;
    }
}
```

**Burn damage tiers** (adapted from Qud's `Burning.cs`):

| Temperature Excess | Damage |
|-------------------|--------|
| 0-100 over FlameTemp | 1 |
| 101-300 | 1-2 |
| 301-500 | 2-3 |
| 501-700 | 3-4 |
| 701-900 | 4-5 |
| 900+ | 5-6 |

### WetEffect

Suppresses fire, increases conductivity, evaporates over time.

```csharp
public class WetEffect : Effect
{
    public float Moisture = 1.0f;       // 0.0 to 1.0
    public override int EffectType => TYPE_CHEMICAL;

    public override void OnTurnStart()
    {
        // Evaporate based on owner's temperature
        var thermal = Owner.GetPart<ThermalPart>();
        if (thermal != null)
            Moisture -= (thermal.Temperature - 25f) * 0.005f;

        if (Moisture <= 0f)
            Remove();
    }

    // Veto or weaken ignition attempts
    // (handled via TryIgniteEvent — see Event Pipeline)
}
```

### SmolderingEffect

Post-fire state. Low heat emission, can re-ignite.

```csharp
public class SmolderingEffect : Effect
{
    public override int EffectType => TYPE_THERMAL;
    public int Duration = 5;

    public override void OnTurnStart()
    {
        // Emit small amount of heat (can re-ignite if new fuel arrives)
        EmitHeatToAdjacent(0.3f);
        // Produce smoke (spawn light gas in cell)
        SpawnSmoke();
    }
}
```

### CharredEffect

Permanent material degradation after burning. Reduces integrity, changes appearance.

```csharp
public class CharredEffect : Effect
{
    public override int EffectType => TYPE_STRUCTURAL;
    public int Duration = -1; // Permanent until repaired

    public override void Apply()
    {
        // Reduce material properties
        var mat = Owner.GetPart<MaterialPart>();
        if (mat != null)
            mat.Combustibility *= 0.3f; // Charred material is less combustible
    }
}
```

---

## Event Pipeline

All material interactions flow through typed events with a veto/modify/notify lifecycle. This follows Qud's pattern where parts negotiate outcomes through shared events rather than calling each other directly.

### Event Definitions

```csharp
// CHECK: Can this object ignite? Parts can veto.
public class TryIgniteEvent : GameEvent
{
    public static readonly string ID = "TryIgnite";
    public Entity Source;           // What's trying to ignite this
    public float IncomingHeat;      // How much heat is being applied
    public bool Cancelled;          // Set true by any part to veto ignition
    public float IgnitionModifier = 1.0f; // Parts can weaken/strengthen
}

// RESOLVE: Apply temperature change to an object.
public class ApplyHeatEvent : GameEvent
{
    public static readonly string ID = "ApplyHeat";
    public Entity Source;
    public float Joules;            // Amount of heat
    public HeatMode Mode;           // Radiant or Direct
    public bool IsSpellOrigin;      // True if caused by a grimoire spell
    public string SpellSchool;      // "Shaping", "Martial", etc.
}

public enum HeatMode { Radiant, Direct }

// RESOLVE: Two materials are in contact.
public class MaterialContactEvent : GameEvent
{
    public static readonly string ID = "MaterialContact";
    public Entity Source;
    public Entity Target;
    public ContactType Contact;     // Adjacent, Smeared, Submerged, Projectile
    public float Intensity;
}

public enum ContactType { Adjacent, Smeared, Submerged, Projectile }

// NOTIFY: A material reaction occurred (for logging, VFX, sound, AI awareness).
public class MaterialReactedEvent : GameEvent
{
    public static readonly string ID = "MaterialReacted";
    public Entity Primary;
    public Entity Secondary;
    public string ReactionID;       // Blueprint ID that fired
    public string VisualEffect;     // VFX key
    public string SoundEffect;      // SFX key
}
```

### Event Flow: Ignition Attempt

```
ApplyHeatEvent arrives at target entity
    │
    ├─► ThermalPart.HandleEvent: applies heat scaled by HeatCapacity
    │   Temperature increases. Check: did we cross FlameTemperature?
    │
    ├─► If threshold crossed: fire TryIgniteEvent
    │       │
    │       ├─► WetEffect.HandleEvent: if Moisture > 0.35, set Cancelled = true
    │       │   OR reduce IgnitionModifier by Moisture amount
    │       │
    │       ├─► MaterialPart.HandleEvent: if Combustibility == 0, set Cancelled = true
    │       │
    │       ├─► Any "Fireproof" effect: set Cancelled = true
    │       │
    │       └─► If not Cancelled:
    │               StatusEffectsPart.ApplyEffect(new BurningEffect {
    │                   Intensity = IgnitionModifier * (Combustibility / 50f),
    │                   IgnitionSource = Source
    │               })
    │
    └─► Fire MaterialReactedEvent for VFX/SFX/AI
```

### Event Flow: Per-Turn Burn Tick

```
EndTurn event on burning entity
    │
    ├─► BurningEffect.OnTurnStart()
    │   ├─ Consume fuel from FuelPart
    │   ├─ Calculate heat output (fuel consumed * HeatOutput * Combustibility factor)
    │   ├─ Deal burn damage to owner
    │   ├─ Emit heat to self (ThermalPart)
    │   └─ Emit heat to adjacent cells
    │       │
    │       └─► For each adjacent cell (Zone.GetCellInDirection, 8 directions):
    │           For each entity in cell with ThermalPart:
    │               Fire ApplyHeatEvent(Mode=Radiant, Joules=heatOutput/8)
    │               This may trigger THEIR ignition via the same pipeline
    │
    ├─► If FuelPart exhausted:
    │   ├─ Remove BurningEffect
    │   ├─ Apply SmolderingEffect (residual heat, smoke)
    │   └─ Spawn ExhaustProduct entity (Ash, Charcoal) if defined
    │
    └─► ThermalPart.HandleEvent(EndTurn)
        ├─ Decay toward ambient temperature
        └─ If no longer IsAflame and was aflame: fire ExtinguishedEvent
```

---

## Data-Driven Reaction Layer

Material-specific interactions are defined as data assets, not hardcoded branches. The `MaterialReactionResolver` reads these at runtime.

### MaterialReactionBlueprint

```json
{
    "id": "fire_plus_organic",
    "description": "Organic material burns hotter and consumes fuel faster",
    "conditions": {
        "sourceState": "Burning",
        "targetMaterialTag": "Organic",
        "minTemperature": 180,
        "maxMoisture": 0.35
    },
    "effects": [
        {
            "type": "ModifyBurnIntensity",
            "add": 0.5
        },
        {
            "type": "ModifyFuelConsumption",
            "multiply": 1.5
        },
        {
            "type": "EmitBonusHeat",
            "joules": 15
        },
        {
            "type": "SpawnParticle",
            "particle": "EmberBurst"
        }
    ],
    "priority": 40
}
```

### MaterialReactionResolver

A singleton Part attached to the Zone entity (not individual objects). Listens for MaterialContactEvent and ApplyHeatEvent, checks blueprints, and applies matching reactions.

```csharp
public class MaterialReactionResolver : Part
{
    private List<MaterialReactionBlueprint> _blueprints; // Loaded from data

    public override bool WantEvent(string eventID)
        => eventID == MaterialContactEvent.ID
        || eventID == ApplyHeatEvent.ID;

    public override bool HandleEvent(GameEvent e)
    {
        if (e.ID == MaterialContactEvent.ID)
            ResolveContact((MaterialContactEvent)e);
        else if (e.ID == ApplyHeatEvent.ID)
            ResolveHeat((ApplyHeatEvent)e);
        return true;
    }

    private void ResolveContact(MaterialContactEvent e)
    {
        foreach (var bp in _blueprints)
        {
            if (bp.Matches(e.Source, e.Target))
            {
                bp.Apply(e.Source, e.Target, e.Intensity);
                FireMaterialReactedEvent(e.Source, e.Target, bp);
            }
        }
    }
}
```

### Why data-driven

- Designers add `acid_plus_metal.json`, `frost_plus_crystal.json` without touching C#
- Debug traces report which blueprint fired and why
- Blueprints can be hot-reloaded during playtesting
- Conditions and effects are composable from a small set of primitives

---

## Simulation Loop

Per-turn processing phases, run during the EndTurn event cascade. This prevents mid-iteration mutation bugs.

```
Phase 1: GATHER
    Collect all pending heat transfers and material contacts from this turn.
    (BurningEffect emits, spells emit, liquid contact emits, radiant sources emit)

Phase 2: CHECK
    For each pending interaction, fire veto events (TryIgniteEvent, etc.).
    WetEffect, Fireproof parts, material immunity can cancel.

Phase 3: RESOLVE
    Apply surviving interactions:
    - Temperature changes (ThermalPart)
    - Effect applications (BurningEffect, WetEffect)
    - Fuel consumption (FuelPart)
    - Material reactions (from blueprints)

Phase 4: PROPAGATE
    Enqueue neighbor checks for next turn:
    - Burning objects radiate heat to adjacent cells
    - Liquids spread based on fluidity
    - Gas disperses based on density and wind

Phase 5: NOTIFY
    Fire MaterialReactedEvent for each reaction that occurred.
    Renderer picks up VFX. Sound system picks up SFX. AI picks up threat.
```

**Why phased:** If burning object A heats object B during A's turn, and B immediately ignites and heats C during the same turn, you get order-dependent cascades. Phased processing ensures all objects gather inputs first, then all resolve simultaneously, then all propagate. Deterministic regardless of entity iteration order.

---

## Fire + Organic: The First Interaction (Complete Walkthrough)

### Setup

A wooden barrel (Organic, Fibrous) sits in a corridor. A torch entity burns in an adjacent cell.

**Barrel properties:**
- MaterialPart: MaterialID="Wood", MaterialTags={"Organic","Fibrous"}, Combustibility=70
- ThermalPart: Temperature=25, FlameTemperature=200, HeatCapacity=0.8
- FuelPart: FuelMass=80, BurnRate=1.0, HeatOutput=1.2, ExhaustProduct="Ash"

**Torch properties:**
- ThermalPart: Temperature=500 (above its own FlameTemperature)
- BurningEffect: Intensity=1.5 (steady burn)

### Turn-by-Turn Cascade

**Turn 1:** Torch's BurningEffect emits heat to adjacent cells. Barrel receives `ApplyHeatEvent(Joules=12, Mode=Radiant)`. ThermalPart calculates: `(12 - 25) * (0.035 / 0.8)` — small warming. Barrel temperature: 25 → 31. No threshold crossed.

**Turn 2-6:** Same radiant heating each turn. Barrel temperature climbs: 31 → 38 → 46 → 55 → 65. HeatCapacity 0.8 means it warms 25% faster than baseline.

**Turn 10:** Barrel temperature reaches ~130. Still below FlameTemperature (200). But warming accelerates as temperature approaches source temperature via the radiant formula.

**Turn 14:** Barrel temperature crosses 200 (FlameTemperature). ThermalPart fires `TryIgniteEvent`. No WetEffect present. MaterialPart checks Combustibility=70 (nonzero). No fireproof parts. Ignition succeeds.

`BurningEffect` applied with `Intensity = 1.0 * (70/50) = 1.4`. The barrel burns fiercely because organic material has high Combustibility.

**Turn 15:** `fire_plus_organic` blueprint triggers:
- Condition check: source is Burning, target has "Organic" tag, temperature > 180, no moisture → match
- Effects: burn intensity +0.5 (now 1.9), fuel consumption x1.5, bonus heat +15 joules
- Barrel now emits MORE heat than the torch that lit it

**Turn 15-25:** Barrel burns hot. FuelMass depletes at `1.0 * 1.5 * 1.9 = 2.85` per turn. Heat radiates to ALL 8 adjacent cells. Anything organic nearby starts warming. If another wooden object is adjacent, it follows the same 14-turn warmup cycle — but faster, because the barrel burns hotter than the torch did.

**Turn 43:** Barrel's FuelMass reaches 0. BurningEffect removed. SmolderingEffect applied (3 turns of low heat + smoke). ExhaustProduct "Ash" entity spawned in the cell. CharredEffect applied to barrel — Combustibility drops to 21 (0.3 * 70). If the barrel survives structurally, it's now a charred husk that's hard to re-ignite.

### What Emergence Looks Like

None of this was scripted as "barrel catches fire from torch." The simulation produced:
- Gradual warming from radiant heat (not instant ignition)
- HeatCapacity made wood warm faster than stone would
- Combustibility made the fire intense once it caught
- The organic material reaction blueprint amplified the burn
- Heat radiated outward, potentially igniting neighbors
- Fuel depletion meant the fire eventually died
- Residual charring changed the barrel's properties permanently

A player who casts **Dry Wind** (removes moisture) before the torch is lit removes the moisture buffer and potentially lowers effective FlameTemperature. A player who casts **Create Spring** (adds water/moisture) nearby applies WetEffect to the barrel, which vetoes ignition. A player who casts **Field of Flowers** fills adjacent cells with high-Combustibility organic matter — the barrel fire spreads into the flowers.

No special case for any of these. The spell modifies properties. The simulation reads properties. Emergence falls out.

---

## Grimoire and Sigil Integration

### Shaping Spells as Event Emitters

Shaping grimoire spells do not bypass the material system. They feed into it by emitting the same events that environmental sources emit.

```csharp
public class FireboltSpellEffect : Part
{
    public override bool HandleEvent(GameEvent e)
    {
        if (e.ID == "SpellImpact")
        {
            var target = e.GetParameter<Entity>("Target");
            var heat = new ApplyHeatEvent
            {
                Source = Owner,             // The caster
                Joules = 80f,               // Strong direct heat
                Mode = HeatMode.Direct,     // Not radiant — spell impact is forceful
                IsSpellOrigin = true,
                SpellSchool = "Shaping"
            };
            target.FireEvent(heat);
            return true;
        }
        return true;
    }
}
```

The target's ThermalPart handles this identically to heat from a torch or a burning barrel. Direct mode means it heats faster than radiant (Qud pattern: deliberate action > passive environment).

### Sigil Modifications

Sigils modify the spell's output before it enters the material pipeline. They never touch the material system directly.

| Sigil | What It Modifies | Material System Effect |
|-------|-----------------|----------------------|
| Sigil of Permanence | Spell's heat source duration | Fire persists longer → more fuel consumed → more spread |
| Sigil of Expansion | Spell's target area | ApplyHeatEvent hits more cells → wider ignition zone |
| Sigil of Echoes | Spell casts twice at half power | Two ApplyHeatEvents at 40 Joules each → different thermal profile |

### Everyday Magic Integration

Everyday spells from EMERGENT_EVERYDAY_MAGIC.md modify cell environmental properties. The material system reads those properties.

| Everyday Spell | Property Modified | Material System Consequence |
|---------------|------------------|---------------------------|
| Field of Flowers | +Organic Matter, +Moisture | New fuel source (Organic) BUT moisture suppresses ignition |
| Dry Wind | -Moisture, +Airflow | Removes WetEffect from objects, fire spreads faster downwind |
| Gentle Breeze | +Airflow direction | Heat propagation biased in wind direction |
| Warm Hearth | +Temperature in radius | Objects warm toward ignition without a direct fire source |
| Clean Surface | -Organic Matter, -Moisture | Removes fuel and moisture — fire prevention |
| Create Spring | +Liquid (water), +Moisture | WetEffect applied to nearby objects — fire suppression |

---

## AI Integration

Creatures respond to material hazards through the existing goal stack and navigation weight patterns (see AI_ENVIRONMENTAL_RESPONSES_EMERGENCE.md).

### Navigation Weights

Burning objects and cells publish navigation weights so AI avoids fire.

```csharp
// In BurningEffect or ThermalPart:
public override bool HandleEvent(GameEvent e)
{
    if (e.ID == "GetNavigationWeight")
    {
        if (IsAflame)
            e.SetIntParameter("MinWeight", 30); // Strong avoidance
        else if (Temperature > FlameTemperature * 0.7f)
            e.SetIntParameter("MinWeight", 10); // Moderate avoidance of hot areas
        return true;
    }
    return true;
}
```

### Goal Responses

| Condition | AI Goal Pushed | Intelligence Gate |
|-----------|---------------|------------------|
| Self on fire | ExtinguishSelf (seek water) | Int >= 7: seeks pools. Lower: flails. |
| Adjacent fire | Flee (move away 1-3 cells) | Int >= 5: routes around. Lower: random. |
| Low HP + fire nearby | Retreat (strategic withdrawal) | Scales with Int |

### AI Using Fire Tactically

Intelligent enemies (Int >= 12) with fire spells can recognize Organic MaterialTags in the environment and target them deliberately. This isn't hardcoded — it falls out of the AI evaluating "which target produces the most damage" and fire + organic = more heat = more damage to adjacent creatures.

---

## Save/Load Considerations

Since this is an open-world RPG with saves (not a roguelike), all material state must serialize cleanly.

| State | Serialization |
|-------|--------------|
| MaterialPart properties | Saved with entity (Combustibility, etc. may have been modified by CharredEffect) |
| ThermalPart.Temperature | Saved as float — restores exact thermal state |
| FuelPart.FuelMass | Saved as float — partially burned objects stay partially burned |
| BurningEffect | Saved with Intensity, Duration, IgnitionSource ref |
| WetEffect | Saved with Moisture level |
| CharredEffect | Saved (permanent until repaired) |
| Pending heat transfers | Discarded on save — recalculated next turn from current state |

**Key invariant:** Save/load must not cause fires to extinguish or re-ignite. Loading a save with a burning barrel at Temperature=350 and BurningEffect.Intensity=1.9 must produce identical behavior to never having saved.

---

## Future Material Interactions (Sketched)

These use the same architecture. Only new blueprints and potentially new Effects are needed.

| Interaction | Blueprint Conditions | Effects |
|------------|---------------------|---------|
| Acid + Metal | ContactType=Smeared, targetTag="Metal" | Apply CorrodedEffect, reduce Integrity over time |
| Electricity + Liquid | Conductivity > 50, liquid present in cell | Chain ApplyShockEvent to all entities in conductive liquid |
| Frost + Crystal | Temperature < FreezeTemperature, targetTag="Crystalline" | Brittleness check, shatter if Brittleness > 60 |
| Water + Fire | WetEffect applied to burning entity | Reduce Intensity, produce SteamGas entity in cell |
| Oil + Fire | ContactType=Adjacent, sourceState="Burning", targetTag="Oil" | Massive Combustibility bonus, rapid spread |

Each requires: one blueprint JSON, possibly one new Effect class, zero changes to the core simulation.

---

## Implementation Order

### Phase 1: Core Parts (no interactions yet)
1. `MaterialPart` — material identity and properties
2. `ThermalPart` — temperature tracking, ambient decay, threshold detection
3. `FuelPart` — fuel mass tracking

### Phase 2: Fire Effects
4. `BurningEffect` — fuel consumption, damage, heat emission
5. `WetEffect` — moisture tracking, ignition suppression, evaporation
6. `SmolderingEffect` — post-fire residual heat and smoke
7. `CharredEffect` — permanent material degradation

### Phase 3: Event Pipeline
8. `ApplyHeatEvent`, `TryIgniteEvent`, `MaterialContactEvent`, `MaterialReactedEvent`
9. Hook ThermalPart into EndTurn for ambient decay and radiant emission
10. Hook BurningEffect into TryIgniteEvent veto chain

### Phase 4: Reaction System
11. `MaterialReactionBlueprint` data format
12. `MaterialReactionResolver` — loads blueprints, matches conditions, applies effects
13. First blueprint: `fire_plus_organic.json`

### Phase 5: Spatial Propagation
14. Heat radiation to adjacent cells (8-directional via Zone.GetCellInDirection)
15. Fire spread check (adjacent entities with MaterialPart + ThermalPart)
16. Airflow bias for directional spread (connect to cell environmental properties)

### Phase 6: Integration
17. Connect Shaping spells to emit ApplyHeatEvent
18. Connect everyday magic property layer (moisture, organic matter) to MaterialPart/WetEffect
19. Add navigation weights for AI fire avoidance
20. Add debug trace logging for reaction chains

### Phase 7: Validation
21. Test scenario: torch ignites barrel, barrel ignites neighboring barrel
22. Test scenario: wet barrel resists ignition from torch
23. Test scenario: Field of Flowers + Dry Wind + fire source = corridor fire
24. Test scenario: save/load mid-fire preserves exact state
25. Test scenario: AI flees from fire, seeks water if burning

---

## Acceptance Criteria for Fire + Organic MVP

- [ ] Fire next to organic object can ignite it through the event pipeline (not hardcoded)
- [ ] Wet organic object resists ignition via WetEffect veto
- [ ] Burning organic produces more heat than non-organic baseline (blueprint-driven)
- [ ] Fire spreads to adjacent organic objects through radiant heat
- [ ] Fuel depletion extinguishes fire and produces exhaust product
- [ ] Temperature is continuous — objects warm gradually, not instantly
- [ ] AI avoids burning cells via navigation weight
- [ ] Full reaction chain visible in debug trace
- [ ] Save/load preserves all thermal and effect state
- [ ] A Shaping spell can ignite organic material through the same pipeline as a torch

Once these pass, subsequent material interactions (acid+metal, frost+crystal, electricity+liquid) use the same Part + Effect + Event + Blueprint architecture with zero changes to the core simulation.

---

## Source References

- `TEMPERATURE_GAS_PHYSICS_EMERGENCE.md` — temperature model and gas dispersal patterns
- `AI_ENVIRONMENTAL_RESPONSES_EMERGENCE.md` — goal stack and navigation weight patterns
- `QUD_ENTITY_PART_EVENT_EMERGENT_SYSTEMS.md` — entity composition and event architecture
- `LIQUIDS.md` — liquid property system and contact mechanics
- `EMERGENT_EVERYDAY_MAGIC.md` — environmental property layer and spell-to-property mapping
- `GRIMOIRES_AND_SIGILS.md` — grimoire schools, sigil modifications, spell tiers
- Qud decompiled: `Physics.cs`, `Gas.cs`, `BaseLiquid.cs`, `LiquidVolume.cs`, `Brain.cs`, `Burning.cs`
```
