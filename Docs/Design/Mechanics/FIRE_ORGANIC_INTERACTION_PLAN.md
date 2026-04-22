# Plan: Fire + Organic Material Interaction System

## Context

Implementing the emergent material interaction system designed in `Docs/Design/Mechanics/MATERIAL_INTERACTION_SYSTEM.md`. Scope: fire and organic material only, but the full framework for future expansion (acid+metal, frost+crystal, electricity+liquid). This is the foundation for grimoire spells interacting with the environment.

The game already has: Entity+Part+Effect architecture, GameEvent pooling, StatusEffectsPart, a simple BurningEffect (duration+damageDice), CombatSystem.ApplyDamage, Zone/Cell spatial grid, JSON blueprint system.

---

## Key Decisions

1. **Replace existing BurningEffect** -- the current one (`duration + damageDice`) has no intensity, fuel, heat emission, or propagation. Rewrite in-place; update 2 callsites (FireBoltMutation, StatusEffectTests).

2. **Use existing GameEvent pattern** -- string IDs + typed parameter dicts, not event subclasses. Matches every other system in the codebase.

3. **Inline heat propagation** -- BurningEffect emits heat to adjacent cells during its own OnTurnStart (like Qud's Burning.cs). No deferred queue for MVP. Order-dependent but produces reasonable results.

4. **JSON for reaction blueprints** -- consistent with existing `Factions.json` and `Recipes_V1.json`. Placed in `Assets/Resources/Content/Data/MaterialReactions/`.

5. **Generalize aura system** -- replace hardcoded `BurningEffect`/`PoisonedEffect` checks in `StatusEffectsPart.TryStartAura` (lines 356-384) with an `IAuraProvider` interface.

---

## Phase 1: Core Material Parts

No interactions yet. Entities declare material identity and track temperature.

### New File: `Assets/Scripts/Gameplay/Materials/MaterialPart.cs`
- `MaterialPart : Part`
- Fields: `string MaterialID`, `float Combustibility`, `float Conductivity`, `float Porosity`, `float Volatility`, `float Brittleness`, `string MaterialTagsRaw` (comma-separated for blueprints)
- `HashSet<string> MaterialTags` parsed from `MaterialTagsRaw` in `Initialize()`
- `Initialize()` also adds material tags to `ParentEntity.SetTag()` for zone queries
- Handles `"TryIgnite"`: if `Combustibility == 0`, sets `Cancelled = true`
- Handles `"QueryMaterial"`: exposes material info on event

### New File: `Assets/Scripts/Gameplay/Materials/ThermalPart.cs`
- `ThermalPart : Part`
- Fields: `float Temperature = 25f`, `float FlameTemperature = 400f`, `float VaporTemperature = 10000f`, `float FreezeTemperature = 0f`, `float BrittleTemperature = -100f`, `float HeatCapacity = 1.0f`, `float AmbientDecayRate = 0.02f`
- Derived: `bool IsAflame => Temperature >= FlameTemperature`
- Handles `"ApplyHeat"`: two modes from Qud:
  - Radiant: `Temperature += (Joules - Temperature) * (0.035f / HeatCapacity)`
  - Direct: `Temperature += Joules / HeatCapacity`
  - If crosses FlameTemperature: fires `"TryIgnite"` on self, checks WetEffect moisture, applies BurningEffect if not cancelled
- Handles `"EndTurn"`: decay toward ambient, check if no longer aflame -> fire `"Extinguished"`

### New File: `Assets/Scripts/Gameplay/Materials/FuelPart.cs`
- `FuelPart : Part`
- Fields: `float FuelMass = 100f`, `float MaxFuel = 100f`, `float BurnRate = 1.0f`, `float HeatOutput = 1.0f`, `string ExhaustProduct = ""`
- Handles `"ConsumeFuel"`: subtracts amount, reports HeatProduced and Exhausted on event

---

## Phase 2: Enhanced Effects

### Replace: `Assets/Scripts/Gameplay/Effects/Concrete/BurningEffect.cs`
- New fields: `float Intensity = 1.0f`, `Entity IgnitionSource`
- `Duration = -1` (fuel-controlled) when FuelPart present; fallback `ceil(Intensity*3)` for creatures without fuel
- `OnTurnStart`:
  1. Fire `"ConsumeFuel"` on owner -> read HeatProduced, Exhausted
  2. If exhausted: remove self, apply SmolderingEffect, spawn ExhaustProduct
  3. Deal damage via `CombatSystem.ApplyDamage` (6-tier intensity table)
  4. Emit heat to self via `"ApplyHeat"` (Direct, small reinforcement)
  5. Call `MaterialSimSystem.EmitHeatToAdjacent()` for spatial propagation
- `OnStack`: `Intensity = Min(Intensity + incoming.Intensity * 0.5f, 5.0f)`, return true
- Implements `IAuraProvider` -> `AsciiFxTheme.Fire`

### New File: `Assets/Scripts/Gameplay/Effects/Concrete/WetEffect.cs`
- `float Moisture = 1.0f`, evaporates based on owner temperature each turn
- Ignition suppression handled in ThermalPart's TryIgnite flow (checks WetEffect)
- `OnStack`: adds moisture, capped at 1.0
- `GetRenderColorOverride`: `"&B"`

### New File: `Assets/Scripts/Gameplay/Effects/Concrete/SmolderingEffect.cs`
- `Duration = 5`, emits low heat to adjacent cells, spawns smoke particles
- `GetRenderColorOverride`: `"&W"`

### New File: `Assets/Scripts/Gameplay/Effects/Concrete/CharredEffect.cs`
- `Duration = -1` (permanent), reduces `MaterialPart.Combustibility` by 70%
- Stores original value, restores on Remove

---

## Phase 3: Heat Propagation Utility

### New File: `Assets/Scripts/Gameplay/Materials/MaterialSimSystem.cs`
- Static class (consistent with `CombatSystem`, `MovementSystem`)
- `static void EmitHeatToAdjacent(Entity source, Zone zone, float totalJoules)`:
  - Gets source cell, iterates 8 directions via `zone.GetCellInDirection()`
  - For each entity with ThermalPart: fires `"ApplyHeat"` (Radiant, joules/8)
- Called from BurningEffect.OnTurnStart and SmolderingEffect.OnTurnStart

---

## Phase 4: Data-Driven Reactions

### New File: `Assets/Scripts/Gameplay/Materials/MaterialReactionBlueprint.cs`
- `[Serializable]` data class: ID, Priority, Conditions (SourceState, TargetMaterialTag, MinTemperature, MaxMoisture), Effects list (Type, FloatValue, StringValue)

### New File: `Assets/Scripts/Gameplay/Materials/MaterialReactionResolver.cs`
- Static class, loads blueprints from JSON
- `EvaluateReactions(Entity, Zone)`: matches conditions against entity state, applies effects (ModifyBurnIntensity, ModifyFuelConsumption, EmitBonusHeat, SpawnParticle)
- Called from BurningEffect.OnTurnStart after fuel consumption

### New File: `Assets/Resources/Content/Data/MaterialReactions/fire_plus_organic.json`
```json
{
  "Reactions": [{
    "ID": "fire_plus_organic",
    "Priority": 40,
    "Conditions": {
      "SourceState": "Burning",
      "TargetMaterialTag": "Organic",
      "MinTemperature": 180,
      "MaxMoisture": 0.35
    },
    "Effects": [
      { "Type": "ModifyBurnIntensity", "FloatValue": 0.5 },
      { "Type": "ModifyFuelConsumption", "FloatValue": 1.5 },
      { "Type": "EmitBonusHeat", "FloatValue": 15 }
    ]
  }]
}
```

---

## Phase 5: Integration & Cleanup

### New File: `Assets/Scripts/Gameplay/Effects/IAuraProvider.cs`
```csharp
public interface IAuraProvider { AsciiFxTheme GetAuraTheme(); }
```

### Modify: `Assets/Scripts/Gameplay/Effects/StatusEffectsPart.cs` (lines 356-384)
- Replace hardcoded BurningEffect/PoisonedEffect checks with `if (effect is IAuraProvider aura)`

### Modify: `Assets/Scripts/Gameplay/Effects/Concrete/PoisonedEffect.cs`
- Add `IAuraProvider` implementation -> `AsciiFxTheme.Poison`

### Modify: `Assets/Scripts/Gameplay/Mutations/FireBoltMutation.cs` (line 21)
- `new BurningEffect(duration: 3, damageDice: "1d3")` -> `new BurningEffect(intensity: 1.0f, source: ParentEntity)`

### Modify: `Assets/Scripts/Gameplay/Mutations/FlamingHandsMutation.cs` (after line 99)
- Add `"ApplyHeat"` event emission (Direct, Joules = totalDamage * 5f) so Flaming Hands participates in material system

### Modify: `Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs` (after line 75)
- Load material reaction JSON: `MaterialReactionResolver.Initialize(reactionAsset.text)`

### Modify: `Assets/Resources/Content/Blueprints/Objects.json`
- Add Material/Thermal/Fuel parts to existing blueprints (Campfire, wooden objects, creatures)
- Add new blueprints: WoodenBarrel, AshPile, Torch

---

## Phase 6: Tests

### New File: `Assets/Tests/EditMode/Gameplay/Materials/MaterialSystemTests.cs`

Key test cases:
- MaterialPart parses comma-separated tags
- ThermalPart: direct heat increases temperature scaled by HeatCapacity
- ThermalPart: radiant heat converges asymptotically
- ThermalPart: ambient decay cools over EndTurn
- FuelPart: consumption reduces mass, reports exhaustion
- Ignition: crossing FlameTemperature fires TryIgnite
- Ignition: Combustibility=0 cancels ignition
- Ignition: WetEffect with Moisture>0.35 cancels ignition
- BurningEffect: consumes fuel per turn, removes self on exhaustion
- BurningEffect: stacking increases intensity
- BurningEffect: no FuelPart uses fallback duration
- Heat propagation: burning entity heats neighbors
- Chain ignition: torch -> barrel over several turns
- Reaction: fire+organic increases intensity (blueprint-driven)
- WetEffect: evaporates when hot, removed at 0 moisture
- CharredEffect: reduces Combustibility, permanent

### Modify: `Assets/Tests/EditMode/Gameplay/Effects/StatusEffectTests.cs`
- Update BurningEffect constructor in existing tests

---

## File Summary

| New Files (12) | Purpose |
|---|---|
| `Assets/Scripts/Gameplay/Materials/MaterialPart.cs` | Material identity & properties |
| `Assets/Scripts/Gameplay/Materials/ThermalPart.cs` | Continuous temperature |
| `Assets/Scripts/Gameplay/Materials/FuelPart.cs` | Combustible mass |
| `Assets/Scripts/Gameplay/Materials/MaterialSimSystem.cs` | Heat propagation utility |
| `Assets/Scripts/Gameplay/Materials/MaterialReactionBlueprint.cs` | Reaction data class |
| `Assets/Scripts/Gameplay/Materials/MaterialReactionResolver.cs` | Reaction matching |
| `Assets/Scripts/Gameplay/Effects/Concrete/WetEffect.cs` | Moisture/fire suppression |
| `Assets/Scripts/Gameplay/Effects/Concrete/SmolderingEffect.cs` | Post-fire heat/smoke |
| `Assets/Scripts/Gameplay/Effects/Concrete/CharredEffect.cs` | Permanent degradation |
| `Assets/Scripts/Gameplay/Effects/IAuraProvider.cs` | Aura interface |
| `Assets/Resources/Content/Data/MaterialReactions/fire_plus_organic.json` | First reaction |
| `Assets/Tests/EditMode/Gameplay/Materials/MaterialSystemTests.cs` | Tests |

| Modified Files (7) | Change |
|---|---|
| `Assets/Scripts/Gameplay/Effects/Concrete/BurningEffect.cs` | Full rewrite: intensity, fuel, heat emission |
| `Assets/Scripts/Gameplay/Effects/StatusEffectsPart.cs` | IAuraProvider replaces hardcoded checks |
| `Assets/Scripts/Gameplay/Effects/Concrete/PoisonedEffect.cs` | Implement IAuraProvider |
| `Assets/Scripts/Gameplay/Mutations/FireBoltMutation.cs` | New BurningEffect constructor |
| `Assets/Scripts/Gameplay/Mutations/FlamingHandsMutation.cs` | Add ApplyHeat emission |
| `Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs` | Load reaction blueprints |
| `Assets/Resources/Content/Blueprints/Objects.json` | Add material parts to blueprints |

---

## Verification

1. **Unit tests**: Run `MaterialSystemTests` -- all pass
2. **Existing tests**: Run `StatusEffectTests` -- updated constructor, still pass
3. **Manual: Torch ignition**: Place WoodenBarrel next to Campfire, observe gradual warming -> ignition -> fuel depletion -> ash
4. **Manual: Wet suppression**: Apply WetEffect to barrel, confirm torch fails to ignite it
5. **Manual: Fire spread**: Line of 3 WoodenBarrels, ignite first, observe chain spread
6. **Manual: FireBolt spell**: Cast FireBolt at creature, confirm BurningEffect applies with intensity
7. **Manual: Organic bonus**: Observe burning WoodenBarrel (Organic) burns hotter than a hypothetical non-organic object
8. **Manual: Save/load**: Save mid-fire, reload, confirm temperature and BurningEffect state preserved (when save system exists)
