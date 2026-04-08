# Material Interaction Status-Effect Architecture (Qud-Style)

## Goal

Design a Qud-inspired architecture for **emergent material interactions** where environmental state, spell effects, and object composition interact through a shared event pipeline.

This document defines the *general* system, but uses **Fire + Organic => Stronger Fire** as the concrete first implementation.

---

## 1) Qud Patterns We Intentionally Reuse

From Qud's decompiled design patterns, we mirror these principles:

1. **GameObject + IPart composition** for capabilities and state.
2. **Effect objects in parallel with Parts** (temporary state distinct from permanent capabilities).
3. **Event-first resolution** (`WantEvent`/`HandleEvent`) with typed event classes.
4. **Veto/modify/notify lifecycle** for actions.
5. **Cascade-aware dispatch** across equipment/inventory/body/cell/zone contexts.
6. **Multi-pass processing** for complicated resource or interaction resolution.

This gives us scalability: adding a new material should usually be new Parts + new Event handlers + data-driven reaction rules, not central rewrites.

---

## 2) Core Architecture

## 2.1 Entity model

Every simulation object is a `GameObject` with a `PartRack` and `EffectRack`.

### Suggested Parts

- `MaterialPart`
  - Canonical material tags (`Organic`, `Wood`, `Cloth`, `Oil`, `Stone`, etc.)
  - Material properties (porosity, volatility, wetness affinity, heat capacity)
- `ThermalPart`
  - Current temperature, ignition threshold, cooling rate
- `FuelPart`
  - Available combustible mass/energy, burn curve
- `PropagationPart`
  - How this object spreads effects to nearby objects/cells
- `IntegrityPart`
  - Structural HP and deformation thresholds from heat
- `InventoryPart` / `BodyPart` / `PhysicsPart`
  - Existing game behavior components that naturally participate in events

### Suggested Effects

- `BurningEffect` (temporary, stackable or intensity-based)
- `SmolderingEffect`
- `WetEffect` (suppresses ignition efficiency)
- `AshenEffect` / `CharredEffect` (post-burn state)

Qud-like split: Parts define intrinsic behavior; Effects define transient state.

---

## 2.2 Event model

Use typed `MinEvent`-style classes with IDs and cascade levels.

### Base event lifecycle categories

- **Check events** (gate/veto): `CanIgniteEvent`, `CanApplyEffectEvent`
- **Resolve events** (state mutation): `ApplyHeatEvent`, `ConsumeFuelEvent`
- **Notify events** (side-effects): `IgnitedEvent`, `MaterialReactedEvent`

### Required event contracts (minimum)

- `MaterialContactEvent(Source, Target, ContactType, Intensity)`
- `HeatTransferEvent(Source, Target, Joules, Mode)`
- `TryIgniteEvent(Target, IgnitionSource, Heat, Reason)`
- `BurnTickEvent(Target, DeltaTime)`
- `MaterialReactionEvent(Primary, Secondary, Context)`
- `EffectIntensityQueryEvent(Target, EffectType)`

All events should include:

- `Phase` (simulation phase)
- `CascadeLevel`
- `Flags` (`Preview`, `Forced`, `Environmental`, `SpellOrigin`)
- `TraceId` for debugging chain-of-events

---

## 2.3 Priority and dispatch order

Adopt Qud-like deterministic ordering:

1. **Intrinsic safety/veto parts** (phase immunity, indestructible, scripted exceptions)
2. **Material/tuning parts** (organic multiplier, wetness dampening)
3. **Active effects** (Burning/Wet/Frozen modifications)
4. **Inventory/equipment/body routing**
5. **Cell/zone/environment hooks**
6. **Notifications/UI/logging**

This order avoids non-deterministic outcomes and keeps "can this happen?" separate from "what happened?"

---

## 3) Data-Driven Reaction Layer

## 3.1 Reaction definition asset

Create a data asset (JSON/ScriptableObject): `MaterialReactionBlueprint`.

```json
{
  "id": "fire_plus_organic",
  "primary": "Fire",
  "secondaryTag": "Organic",
  "conditions": {
    "minTemperature": 180,
    "maxWetness": 0.35,
    "requiresOxygen": true
  },
  "effects": [
    { "type": "ApplyEffect", "effect": "BurningEffect", "intensityAdd": 2 },
    { "type": "ModifyFuel", "multiplier": 1.5 },
    { "type": "EmitHeat", "bonusJoules": 20 }
  ],
  "priority": 40,
  "stackPolicy": "refresh_and_increase"
}
```

### Why data-driven first

- Designers can tune reactions without code changes.
- New spell-material interactions become content authoring.
- Debug traces can report "which blueprint fired".

---

## 3.2 Reaction resolver part

`MaterialReactionResolverPart : IPart`

Responsibilities:

- listens for `MaterialContactEvent` / `HeatTransferEvent`
- queries reaction blueprints matching source+target+context
- runs check events (`CanIgniteEvent`, wetness veto, oxygen check)
- applies resolve events in deterministic order
- emits `MaterialReactedEvent`

This is the **single orchestration point** but not a god-system: actual behavior is still distributed across parts/effects responding to the emitted events.

---

## 4) Fire + Organic (First Concrete Interaction)

## 4.1 Target behavior

When Fire interacts with Organic material:

1. Organic object is easier to ignite.
2. If already burning, burn intensity increases.
3. Additional fuel is consumed, producing stronger local heat.
4. Stronger heat increases probability of propagation to neighboring organics.

---

## 4.2 Event flow (single tick)

1. `BurningEffect` on source emits `HeatTransferEvent` to nearby objects.
2. Target with `MaterialPart(Organic)` receives `TryIgniteEvent`.
3. `CanIgniteEvent` pipeline runs:
   - `WetEffect` can reduce/deny.
   - `ThermalPart` checks threshold.
   - `SpecialProtectionPart` may veto.
4. If pass:
   - apply/boost `BurningEffect` on target.
   - `FuelPart` applies organic multiplier (`x1.5` baseline).
   - emit bonus `HeatTransferEvent` back into cell.
5. `Zone/Cell` handler enqueues neighboring contact checks for next simulation slice.
6. `MaterialReactedEvent` logs trace for debugging/UI.

---

## 4.3 Suggested formulas (starting point)

```text
igniteChance = baseChance
             + (temperature - ignitionThreshold) * tempFactor
             + organicAffinityBonus
             - wetnessPenalty
             - oxygenPenalty
```

```text
burnIntensityNext = clamp(
  burnIntensityCurrent
  + reactionIntensityAdd
  + fuelQualityBonus
  - dampening,
  0,
  maxBurnIntensity
)
```

For the first pass, keep formulas simple and tunable by data assets.

---

## 5) Grimoire Spell Integration

Spells should use the same event contracts, not bypass them.

## 5.1 Spell-to-material bridge

`SpellMaterialEmitterPart`

- Converts spell impact into `MaterialContactEvent` + `HeatTransferEvent`.
- Sets context flags: `SpellOrigin = true`, `SpellSchool = Pyromancy`, etc.
- Supports spell modifiers that alter cascade radius/intensity.

This ensures spell/environment interactions are emergent by default.

## 5.2 Example

`Flame Sigil` triggers on a tile:

- emits heat pulses each turn
- each pulse runs standard ignition pipeline
- organic clutter catches, boosts fire, chain spreads
- downstream systems (smoke, visibility, panic AI) react through existing events

No special-case "if spell then do organic fire logic" branch needed.

---

## 6) Simulation Phases (Recommended)

Per turn/tick:

1. **Gather phase**: collect pending material/heat contacts.
2. **Check phase**: run all veto/check events.
3. **Resolve phase**: mutate state (apply effects, consume fuel, transfer heat).
4. **Propagate phase**: enqueue neighbors and secondary reactions.
5. **Notify phase**: logs, VFX, SFX, UI.

This phase model prevents mid-iteration mutation bugs and keeps behavior deterministic.

---

## 7) Minimal API Sketch (C#-style)

```csharp
public abstract class IMaterialEvent : MinEvent
{
    public GameObject Source;
    public GameObject Target;
    public MaterialContext Context;
}

public class TryIgniteEvent : IMaterialEvent
{
    public float IncomingHeat;
    public bool Result;
}

public class MaterialReactionResolverPart : IPart
{
    public override bool WantEvent(int id, int cascade)
        => id == MaterialContactEvent.ID || id == HeatTransferEvent.ID;

    public override bool HandleEvent(MaterialContactEvent e)
    {
        // find blueprint(s)
        // run checks
        // apply effect/intensity/fuel changes
        // emit MaterialReactedEvent
        return true;
    }
}
```

---

## 8) Debuggability Requirements (Do This Early)

Add an `EventTraceService` to record:

- TraceId
- source/target
- event order + handler name
- veto reasons
- chosen reaction blueprint
- final intensity/fuel deltas

Emergent systems become impossible to tune without this visibility.

---

## 9) Implementation Plan (Fire + Organic MVP)

1. Implement `MaterialPart`, `ThermalPart`, `FuelPart`, `BurningEffect`.
2. Add typed events: `HeatTransferEvent`, `TryIgniteEvent`, `MaterialReactionEvent`.
3. Implement `MaterialReactionResolverPart` with one blueprint: `fire_plus_organic`.
4. Add cell-level propagation queue.
5. Add event trace logging and a debug overlay command.
6. Tune constants with small scenario tests (single tile, corridor spread, wet organics).

---

## 10) Acceptance Criteria for First Interaction

- Fire next to Organic can ignite it through event pipeline.
- Wet Organic sometimes resists ignition via veto check.
- Burning Organic produces stronger heat than non-organic baseline.
- Neighbor propagation occurs without hardcoded object-type checks.
- Full reaction chain is visible in debug trace.

Once these pass, the same architecture can scale to:

- acid + metal (corrosion)
- electricity + liquid (conductive arcs)
- frost + brittle stone (fracture)
- grimoire sigils + atmosphere/material state coupling

All with the same Part + Effect + Event grammar.
