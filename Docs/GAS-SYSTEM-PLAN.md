# Gas System — Qud-parity port plan

> **Status:** G.1 (this commit). Plan-only.
> **Branch:** `feat/gas-system`
> **Source survey:** `Docs/QUD-GAS-INVESTIGATION.md` (the post-LA-followup
> investigation report — read that first).

## 0. The question, answered

Caves of Qud's gas system is a parallel to its liquid system: per-cell
entities (`Gas` Part) plus behavior Parts (`IGasBehavior` subtypes) that
dispatch effects on creatures/objects in the same cell. CoO already has
the matching half for liquids (`LiquidPoolPart` + `LiquidCoveredEffect`).
Gas is the missing half. This plan ports it with full Qud parity where
the engine supports it.

## 1. Reference — how Qud actually does it (verification sweep)

| Premise | Confirmed at | Detail |
|---|---|---|
| Gas is a Part on a GameObject that lives in a Cell | `qud/XRL.World.Parts/Gas.cs:1-422` | One GameObject per gas cloud, freely created/destroyed |
| Density + Level + Seeping + Stable + GasType + ColorString + Creator are the universal state | `Gas.cs:12-27` | Five ints/bools + two strings + one entity ref |
| ProcessGasBehavior runs each turn-tick — decay + wind-biased random walk + dissipation | `Gas.cs:162-318` | The dispersal heart |
| Merge is GasType + ColorString matched | `Gas.cs:354-410` | Merge or partial merge (MergeToGas) |
| `IGasBehavior` accessor caches the sibling `Gas` Part | `IGasBehavior.cs:5-21` | Abstract base; provides BaseGas + GasDensityStepped |
| `IObjectGasBehavior` iterates cell.Objects and calls ApplyGas(obj) on each | `IObjectGasBehavior.cs:34-79` | Concrete dispatch loop; OnTurnTick + ObjectEnteredCellEvent |
| `CheckGasCanAffectEvent.Check` is the blanket veto layer | `CheckGasCanAffectEvent.cs:34-46` | Listened by `GasImmunity` |
| `GasMask.HandleEvent(GetRespiratoryAgentPerformanceEvent)` reduces gas intake by Power*5 | `GasMask.cs:29-36` | Defensive equipment |
| Wind = `Zone.CurrentWindSpeed/Direction` gated by `GlobalConfig.GetBoolSetting("WindAffectsGasDispersal")` | `Gas.cs:214-218` | Wind biases dispersal direction + frequency |
| `Object.Respires` is a hard gate for breathe-gas dispatch | `GasPoison.cs:100` | Robots/undead skip the entire pipeline |
| GasCryo bypasses Respires and hits all matter via TemperatureChange + cold damage | `GasCryo.cs:127-145` | Different parent class (IGasBehavior, not IObjectGasBehavior) |
| GasPlasma applies `CoatedInPlasma` Effect for `Random(density*2/5, density*3/5)` ticks | `GasPlasma.cs:104-123` | Gas-as-coat hybrid (Effect outlasts the cloud) |
| `BurnOffGas` spawns gas blueprint on Heat/Fire damage threshold | `BurnOffGas.cs:74-117` | Outgassing-on-fire (burn fungi → spores) |
| `EmitGasOnHit` rolls density on impact cell + adjacent on weapon hit | `EmitGasOnHit.cs:132-188` | Gas-emitting weapons |
| `GasGrenade.DoDetonate` spawns 9 gas clouds in a 3×3 square | `GasGrenade.cs:56-86` | Throwable gas item |
| AI nav weight: `Smart` AI sees `density/2 + level*10` capped at 80; non-Smart flat 8 | `GasPoison.cs:38-55` | Pathfinding integration |
| Per-creature filter chain: Respires → Creature tag → CheckGasCanAffectEvent → CanApply{Specific}Gas → CanApplyEffectEvent → PhaseMatches → GetRespiratoryAgentPerformanceEvent | `GasPoison.cs:77-124` | 7-layer veto/modifier pipeline |

## 2. What CoO already has (the substrate we build on)

| CoO piece | What it gives us | File |
|---|---|---|
| `LiquidPoolPart` | Pattern: per-cell entity with `RenderPart` + `PhysicsPart{Solid=false}` + payload Part | `Materials/LiquidPoolPart.cs:35-163` |
| `LiquidRegistry` (Initialize / InitializeFromJsonSources / ResetForTests / Get / IsInitialized / Count) | Pattern: JSON-driven content registry with bootstrap wiring | `Materials/LiquidRegistry.cs:19` |
| `LiquidDefinition` | Pattern: flyweight data class with serializable JSON fields | `Materials/LiquidDefinition.cs:21` |
| `TurnManager.TickEnd` event on `World` singleton entity | The per-turn dispatch hook (gas dispersal listens here) | `Turns/TurnManager.cs:371-373` |
| `Effect.OnTurnStart(Entity, GameEvent)` | The status-effect-side tick (gas-applied effects like PoisonedByGas tick here) | `Effects/Effect.cs:185-188` |
| `Entity` + `Part` + event dispatch | The Qud-parity Part architecture | `Core/Entity.cs`, `Core/Part.cs` |
| `Zone.AddEntity / MoveEntity / GetCell / InBounds` | Cell placement + movement for gas spread | `World/Map/Zone.cs:85-234` |
| `Damage.HasAttribute("Gas")` + `Damage` typed object | Gas-damage attribute (Qud uses the same string) | `Combat/Damage.cs:99` |
| `Diag.Record(category, kind, actor, target, payload)` | Per-gate diag observability | `Shared/Utilities/Diag.cs` |
| `SettlementRuntime.ActiveZone` | Static zone accessor for systems that don't get one passed | `Settlements/SettlementRuntime.cs:7` |
| `GameBootstrap` Step 1b' pattern | Where to add `GasRegistry` initialization | `Bootstrap/GameBootstrap.cs:116-134` |

**What CoO is missing** (out of scope for this ship; deferred):
- Wind concept (`Zone.CurrentWindSpeed/Direction`) — defer to G.10
- Phase matching (`GameObject.PhaseMatches`) — defer or skip
- `GetMatterPhaseEvent` — skip (no caller in CoO needs it yet)
- `Object.Respires` predicate — add as a Tag check (`Tags["Respires"]`) instead of a property
- AI pathfinding nav-weight events — CoO has `AI*Part` system; the integration is its own milestone (G.11)

## 3. Architecture decisions (Qud → CoO mapping)

### 3.1 Class hierarchy (mirrors Qud)

```
Part (CoO base)
├── GasPoolPart                  // universal: Density/Level/Seeping/Stable/GasType/Color/Creator
│                                // dispersal+merge+dissipation engine (G.3-G.4)
└── IGasBehaviorPart (abstract)  // BaseGas accessor + GasDensityStepped()
    └── IObjectGasBehaviorPart (abstract)
        │                        // iterates cell entities, calls ApplyGas(entity)
        │                        // listens to EntityEnteredCell + TickEnd
        ├── GasPoisonPart        // G.5 — Respires-gated, applies PoisonedByGasEffect + tick damage
        ├── GasStunPart          // G.8 — applies StunnedEffect (existing)
        ├── GasSleepPart         // G.8 — applies AsleepEffect (new) or SleepingEffect (existing?)
        ├── GasConfusionPart     // G.8 — applies ConfusedEffect (existing — verify)
        ├── GasFungalSporesPart  // G.8 — applies SporeInfectionEffect (new)
        └── GasPlasmaPart        // G.8 — applies CoatedInPlasmaEffect (new, gas-as-coat hybrid)
    └── GasCryoPart              // G.8 — direct IGasBehavior child; hits all matter via cold damage
                                 // (mirrors Qud's "no Respires gate" design)
```

### 3.2 JSON content (mirrors LiquidDefinition / LiquidRegistry)

`Resources/Content/Data/GasDefinitions/<id>.json`:

```json
{
  "Gases": [
    {
      "Id": "poison-vapor",
      "DisplayName": "poison vapor",
      "Adjective": "poison-fumed",
      "GasType": "Poison",
      "Glyph": "°",
      "Color": "&g",
      "DefaultDensity": 100,
      "DefaultLevel": 1,
      "Seeping": false,
      "Stable": false,
      "BehaviorKind": "Poison"
    }
  ]
}
```

`GasRegistry` mirrors `LiquidRegistry` exactly (Initialize / InitializeFromJsonSources / ResetForTests / Get / IsInitialized / Count). Bootstrap pulls every `.json` in `Resources/Content/Data/GasDefinitions/` at startup.

### 3.3 Spawning a gas cloud

A gas cloud is an `Entity` with three Parts:
- `RenderPart { RenderString=def.Glyph, ColorString=def.Color, DisplayName=def.DisplayName }`
- `PhysicsPart { Solid=false }` (creatures walk through gas)
- `GasPoolPart { GasId, Density, Level, Seeping, Stable, GasType, ColorString, Creator }`
- Optional sibling behavior Part (e.g. `GasPoisonPart`) — added in G.5+

Authoring: a static helper `GasFactory.SpawnGas(Zone, x, y, gasId, density, level, creator)` creates the entity, applies the def's render, attaches the behavior Part for the def's `BehaviorKind`, places at (x, y). (Equivalent of `GameObject.Create("PoisonGas")` in Qud.)

### 3.4 Dispersal loop dispatch (G.3)

Two options for "who calls ProcessGasBehavior each turn":

- **(A) Each gas pool subscribes to TickEnd** — gas pool's `Initialize()` registers a `TickEnd` listener on `TurnManager.World`. Each gas listens for itself.
- **(B) A `GasSystem` static class iterates all gas pools each turn** — TurnManager fires TickEnd → GasSystem.OnTickEnd walks `ActiveZone.GetAllEntities()`, finds GasPoolParts, runs dispersal.

**Recommendation: (B)**, mirrors how Qud's `XRLCore` walks `WantTurnTick()` objects. Cleaner; one place to add diag, profiling, ordering rules. CoO doesn't have `WantTurnTick` but `SettlementRuntime.ActiveZone.GetAllEntities()` is the analog.

### 3.5 Merge semantics (G.4)

Two gases share a cell only if they don't merge (incompatible GasType). When a gas spreads into a cell containing a compatible gas:
- **Full merge** (when a spreader of equal/greater density meets a sibling): receiver += donor.Density, MaxLevel wins, OR-merge Seeping, inherit Creator if missing. Donor dies.
- **Partial merge** (when spreading a chunk): receiver += chunk, donor -= chunk.

`GasPoolPart.OnStack`-equivalent: there's no native CoO `OnStack` for entities (that's an Effect concept). The merge happens **inside the dispersal loop** when looking at the destination cell's existing gases.

### 3.6 Per-creature filter pipeline (G.5)

The CoO equivalent of Qud's 7-layer pipeline:

| Qud layer | CoO equivalent (G.5) |
|---|---|
| `Object != self` | reference check |
| `Object.Respires` | `Tags.ContainsKey("Respires")` — added to creature blueprints |
| `Object.HasTag("Creature")` | `Tags.ContainsKey("Creature")` — existing |
| `CheckGasCanAffectEvent.Check` | New `CheckGasCanAffect` event fired on the target; `GasImmunityPart` handles + vetoes |
| `Object.FireEvent("CanApply{Specific}GasGas")` | per-effect veto event — fire on target before ApplyEffect |
| `CanApplyEffectEvent.Check<T>` | CoO has `target.CanApplyEffect<T>()` if a check exists; else inline |
| `PhaseMatches` | skip (out of scope) |
| `GetRespiratoryAgentPerformanceEvent` | New `GetRespiratoryPerformance` event; `GasMaskPart` reduces intake |
| Apply effect + immediate damage | `target.ApplyEffect(...)` + `CombatSystem.ApplyDamage(...)` |

### 3.7 Events (new)

| Event | Direction | When |
|---|---|---|
| `TickEnd` | listened by `GasSystem` | once per turn |
| `EntityEnteredCell` | listened by `GasPoolPart` | a mover stepped into the gas's cell |
| `CheckGasCanAffect` | fired by gas behavior, vetoed by `GasImmunityPart` | before applying any gas effect |
| `GetRespiratoryPerformance` | fired by gas behavior, reduced by `GasMaskPart` | after CheckGasCanAffect, before apply |
| `CreatorModifyGas` | fired by gas behavior, listened by source's equipment | spawning a gas (lets GasTumbler amplify density) |
| `CreatorModifyGasDispersal` | fired by gas-pool dispersal, listened by source's equipment | per dispersal tick (lets GasTumbler slow decay) |
| `DensityChange` | fired by `GasPoolPart` when Density changes | every density mutation |
| `GasSpawned` | fired on new gas | on dispersal-spread |

## 4. Sub-milestones (smallest blast radius first)

### G.1 — This plan + branch (this commit)

Doc-only. `Docs/GAS-SYSTEM-PLAN.md`. Branch `feat/gas-system` from main.

### G.2 — Foundation (the next commit — this PR's "Phase 1")

The smallest viable slice: a gas pool entity that EXISTS in a cell with
correct render/state, but doesn't yet disperse, merge, or apply effects.

**New files:**
- `Assets/Scripts/Gameplay/Materials/GasDefinition.cs` — `[Serializable]` data class. Fields: Id, DisplayName, Adjective, GasType, Glyph, Color, DefaultDensity, DefaultLevel, Seeping, Stable, BehaviorKind.
- `Assets/Scripts/Gameplay/Materials/GasRegistry.cs` — mirror of LiquidRegistry (Initialize / InitializeFromJsonSources / ResetForTests / Get / IsInitialized / Count).
- `Assets/Scripts/Gameplay/Materials/GasPoolPart.cs` — universal Gas Part. Fields: GasId, Density, Level, Seeping, Stable, GasType, ColorString, Creator. `Initialize()` applies def.Glyph/Color to the entity's RenderPart (mirror of LiquidPoolPart.ApplyDefinitionRender). Density setter fires "DensityChange" event. NO dispersal yet (G.3). NO behavior yet (G.5+).
- `Assets/Scripts/Gameplay/Materials/GasFactory.cs` — static helper `SpawnGas(Zone, x, y, gasId, density, level, creator)` — creates the entity with the standard 3 Parts + def-driven render. Returns the spawned entity (null on bad inputs).
- `Assets/Resources/Content/Data/GasDefinitions/poison-vapor.json` — first content row (no behavior wired yet — visual + state placeholder).
- `Assets/Tests/EditMode/Gameplay/Materials/GasPoolPartTests.cs` — content + behavior + counter + diag tests.

**Modified files:**
- `Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs` — add Step 1b'' (GasRegistry init) mirroring 1b' (LiquidRegistry).

**Test coverage (RED → GREEN):**
- Content: `poison-vapor.json` parses + registers with GasType="Poison", default density 100
- Behavior: `GasFactory.SpawnGas` creates an entity with all 3 Parts, def.Glyph/Color applied to RenderPart
- Counter: SpawnGas with unknown gasId returns null (no crash, no half-built entity)
- State: GasPoolPart.Density round-trips through save (public field, mirrors LiquidPoolPart.Volume)
- Diag: every spawn emits `gas/Created` with payload (gasId, density, level, x, y)
- Density setter: setting `pool.Density = 50` fires "DensityChange" event with OldValue/NewValue params

**Observability (CoO §Observability rule):**
- `gas/Created` — new gas entity spawned in a cell
- `gas/DensityChange` — density mutated (already gated to fire only when actually changes)

**No dispersal, merge, behavior, defense, or production yet.**

### G.3 — Dispersal mechanic (one commit)

`GasSystem` static class with `OnTickEnd(World, GameEvent)` listener. On each tick, iterate `SettlementRuntime.ActiveZone.GetAllEntities()`, find GasPoolParts, call `ProcessGasBehavior(zone)`. The `ProcessGasBehavior` body is a port of `Gas.cs:204-319` — decay if unstable, random-walk spread, dissipation. No wind yet (defer to G.10). New diag: `gas/Dispersed`, `gas/Dissipated`, `gas/Spread`.

### G.4 — Merge semantics (one commit)

`GasSystem.SpreadToCell` checks the destination for compatible gas (same GasType + ColorString); if present, calls `GasPoolPart.MergeFrom(other, chunk)`; if absent, spawns a new gas via GasFactory. The Creator inheritance + Seeping OR + Level max rules from Qud. Counter: incompatible gases don't merge (different GasType in same cell coexists). Diag: `gas/Merged`.

### G.5 — First behaving gas: poison (one commit)

`IGasBehaviorPart` abstract + `IObjectGasBehaviorPart` + `GasPoisonPart` concrete. Per-creature filter pipeline: Respires tag, Creature tag, CheckGasCanAffect event, GetRespiratoryPerformance event, ApplyEffect. New `PoisonedByGasEffect` (Effect class). The poison-vapor.json gets `BehaviorKind="Poison"` and a `GasPoisonPart` attached on spawn. Diag: `gas/Applied`, `gas/Affected`, `gas/ApplyVetoed` (with reason field). Tests: damage tick lands on Creature+Respires; doesn't land on non-Creature; doesn't land on non-Respires; emits diag per gate.

### G.6 — Defenses (one commit)

- `GasMaskPart` (equipment): reduces `GetRespiratoryPerformance.Intake` by `Power * 5`
- `GasImmunityPart` (creature): vetoes `CheckGasCanAffect` when `GasType` matches
- Counter tests: gas-mask-wearing creature takes less damage; gas-immune creature takes none

### G.7 — Production: throwable + on-hit (one commit)

- `GasGrenadePart`: `DoDetonate(Cell)` spawns 9 gases in 3×3
- `EmitGasOnHitPart`: on weapon-hit event, rolls density on impact + adjacent
- One gas-grenade blueprint (`PoisonGasGrenade`) + one EmitGas weapon (`PoisonFangs` snapjaw variant?)

### G.8 — More gas types (one commit per type, or batched)

- `GasCryoPart` (cold damage to all matter via TemperatureChange — needs CoO thermal coupling)
- `GasStunPart` (applies StunnedEffect — exists)
- `GasSleepPart` (applies AsleepEffect — new or existing?)
- `GasConfusionPart` (applies ConfusedEffect — verify exists)
- `GasFungalSporesPart` (applies SporeInfectionEffect — new)
- `GasPlasmaPart` (applies CoatedInPlasmaEffect — new)

### G.9 — Outgassing-on-fire (one commit)

`BurnOffGasPart`: on `BeforeTakeDamage` with Heat/Fire attribute, accumulate damage; on threshold, spawn N copies of `Blueprint` gas at the current cell. Tests: damage threshold + Number roll + Heat-only triggering.

### G.10 — Wind coupling (one commit)

`Zone.CurrentWindSpeed` + `CurrentWindDirection` (new fields, default 0/"."). Plumb into `GasSystem.ProcessGasBehavior` dispersal — bias direction + frequency. New `BlowAwayGasPart` (creature/equipment with Radius that pushes nearby gas away each turn).

### G.11 — AI navigation integration (one commit)

Hook gas avoidance into existing CoO `AIBehaviorPart` / `Goals.MoveToGoal` pathfinding. Density-stepped, gas-immunity-aware. May need a new "nav weight" event analog.

### G.12 — Bench + cold-eye + adversarial sweep + merge (one commit)

`GasDispersalTestBench` scenario — synthetic matrix:
- Spawn 1 gas of each type at known cells
- Tick N turns
- Audit: density decreased? Did it spread? Did it merge? Did it dissipate?
- Spawn a creature with each defense (gas-mask, immunity, none); fire ApplyGas; audit each
- Live diag query with runId scoping (Rule 8)
- Cold-eye Q1–Q4
- Adversarial sweep file (taxonomy: state atomicity, save/load reach, cross-actor flows, boundary inputs, mid-tick death, probability boundaries, etc.)
- Roadmap update; merge to main

## 5. Performance section

Gas pools are per-cell entities. Per-tick cost = O(N_gas) where N_gas is
the count of active gas entities in `ActiveZone`. For dispersal, each
gas does up to 4 cell-spawn attempts → O(4N_gas) worst case per turn.

Pre-flag:
- **Use `Zone.GetAllEntities()` returning a List** — already allocates (Zone.cs:239) but it's a one-shot per-tick, not per-frame
- **GasSystem iterates the gas-pool list ONCE per tick** — mutations during iteration (new gas spawned, old gas dissipated) handled via snapshot-then-iterate pattern
- **DensityChange event** — fires per density mutation; LiquidPoolPart-style listeners (AI nav cache flush) attach here. Skip the event if Density doesn't actually change (event-fired on setter only when oldValue != newValue, mirroring Qud's `_Density != value` gate)
- **Profiler check**: spawn 50 gases, tick 60 turns, measure with `ProfilerRecorder` (mirroring CLAUDE.md §Performance rule 1)

No per-frame or per-render allocations expected. No new MonoBehaviour with Update/LateUpdate. The system runs only on `TickEnd`.

## 6. Observability plan

Every gate emits a `gas/...` record:

| Kind | Fires on | Payload |
|---|---|---|
| `gas/Created` | new gas spawned (factory) | gasId, density, level, x, y, creator |
| `gas/DensityChange` | density mutated | gasId, oldDensity, newDensity, delta |
| `gas/Dispersed` | dispersal tick decremented density | gasId, dropped, remaining |
| `gas/Spread` | dispersal spawned/merged in new cell | gasId, fromCell, toCell, chunk |
| `gas/Merged` | two gases combined | gasId, donorDensity, receiverDensityBefore, receiverDensityAfter |
| `gas/Dissipated` | gas removed from cell | gasId, cause |
| `gas/Applied` | effect applied to a target | gasId, target, effectName, level |
| `gas/Affected` | target took damage from gas | gasId, target, damageType, amount |
| `gas/ApplyVetoed` | filter pipeline rejected | gasId, target, reason (one of NullActor / NotACreature / NotRespires / GasImmunity / CanApplyVetoed / CanApplyEffectVetoed / ZeroIntake) |
| `gas/MaskReduced` | GetRespiratoryPerformance reduced intake | gasId, target, baseIntake, reducedIntake, mask |

## 7. Pre-flagged self-review (fix or defer pre-commit)

- **🟡 Wind coupling deferred (G.10).** Qud's `Zone.CurrentWindSpeed/Direction` doesn't exist in CoO. The dispersal in G.3 uses no-wind defaults. Document the deferral; the dispersal API takes optional wind params so G.10 is a JSON-tuning-only change.
- **🟡 Phase matching skipped.** Qud's `PhaseMatches` is the cross-phase visibility gate; CoO doesn't have phase yet and no caller needs it. Document as out-of-scope; if a future "phased gas grenade" item ships, add then.
- **🟡 Respires tag — new tag.** Creatures need `Tags["Respires"]` for the breathe pipeline to fire. The first non-Respires creature in CoO (a robot or undead) doesn't exist yet, so G.5 ships with all creatures defaulting to respiring (gate is `if (Tags.ContainsKey("Respires"))` but a future opt-out is just removing the tag from a blueprint).
- **🔵 GasSystem singleton.** `GasSystem` is static (mirrors `LiquidRegistry` shape). State lives in the gas pool entities, not the system — system is just dispatch.
- **🔵 No Phased/Omniphase effects yet.** Qud's effects that make gas cross phase boundaries are skipped.
- **🔵 AI navigation deferred (G.11).** Creatures don't avoid gas in their pathing until G.11. Early gas content (G.5-G.10) means dumb-creature charges through poison clouds — acceptable for content-bringup.
- **⚪ Outgassing-on-fire (G.9) requires existing damage attribute system to route Heat correctly** — verified during G.9 sweep; same path the existing acid + element coats use.
- **🧪 RED discipline** — content RED via on-disk file load (Initialize), behavior RED before each engine extension is wired. Same pattern as LA/LB.

## 8. Implementation log

> Filled in per phase as commits land.

### G.1 (this commit)
Plan to disk. Branch `feat/gas-system` from main.
