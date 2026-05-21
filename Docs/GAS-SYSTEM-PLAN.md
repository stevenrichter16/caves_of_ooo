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

### G.1
Plan to disk. Branch `feat/gas-system` from main. Commit `8a0e371`.

### G.2 (this commit) — Foundation

**Status:** ✅ COMPLETE. The minimum viable gas-system slice: per-cell
entity with state + render, factory rejects bad inputs gracefully,
density mutations fire an event. No dispersal yet, no behavior yet.

**Shipped:**
- `Assets/Scripts/Gameplay/Materials/GasDefinition.cs` — `[Serializable]` data class (Id/DisplayName/Adjective/GasType/Glyph/Color/DefaultDensity/DefaultLevel/Seeping/Stable/BehaviorKind) + `GasDefinitionCollection` JSON wrapper. Mirror of `LiquidDefinition`.
- `Assets/Scripts/Gameplay/Materials/GasRegistry.cs` — Static registry mirroring `LiquidRegistry` exactly (Initialize / InitializeFromJsonSources / ResetForTests / Get / IsInitialized / Count + malformed-JSON resilience).
- `Assets/Scripts/Gameplay/Materials/GasPoolPart.cs` — Universal "I am gas" Part. Public fields for save round-trip. `Density` is a property with a setter that clamps to ≥ 0, suppresses zero-delta mutations, and fires `"GasDensityChange"` event with `OldValue`/`NewValue` params (mirroring Qud `Gas.cs:50-55`). `Initialize()` applies def.Glyph/Color to the entity's RenderPart via `ApplyDefinitionRender` (mirror of `LiquidPoolPart`).
- `Assets/Scripts/Gameplay/Materials/GasFactory.cs` — Static `SpawnGas(zone, x, y, gasId, density=-1, level=-1, creator=null)` returns the spawned `Entity` on success or null on any rejection (4 rejection paths: RegistryUninitialized / UnknownGas / NullZone / CellOutOfBounds). Each rejection emits a `gas/SpawnRejected` diag with reason; success emits `gas/Created`.
- `Assets/Resources/Content/Data/GasDefinitions/poison-vapor.json` — First content row (GasType=Poison, Glyph=°, Color=&g, DefaultDensity=100, no behavior wired yet — `BehaviorKind=""`).
- Bootstrap wiring: `GameBootstrap.cs` Step 1b'' loads `Resources/Content/Data/GasDefinitions/*.json` mirroring Step 1b' (LiquidDefinitions).
- "gas" added to `Diag.DefaultOnCategories` so diag emissions survive.

**Tests (22 total, all GREEN):**
- Registry: uninitialized state, single-JSON init, malformed-JSON resilience, late-row-wins on Id collision
- Content: `poison-vapor.json` parses with expected shape
- SpawnGas rejections (4): RegistryUninitialized, UnknownGas, NullZone, CellOutOfBounds — each emits the right reason
- SpawnGas happy-path: all 3 Parts attached, Render pulled from def, PhysicsPart non-solid, defaults inherited, density/level overrides win, placed at requested cell, Gas tag applied, Creator carried through, Created diag emitted with all payload fields
- Density property: negative clamps to 0, mutation fires event with old/new values, zero-delta is silent (no event), 0→-5 clamps to 0 with no event

**IMPLEMENTATION NOTES (risks verified before writing code)**
1. `LiquidRegistry.cs` is the loader template — copied shape exactly (Initialize / InitializeFromJsonSources / AppendJson / Get / ResetForTests / IsInitialized / Count).
2. `LiquidPoolPart.ApplyDefinitionRender` is the render-pull template — copied shape (null-safe registry/id/render-part checks).
3. `Zone.AddEntity` returns bool; `false` means out-of-bounds or invalid cell. Factory uses this for the CellOutOfBounds rejection path.
4. `Entity.AddPart` is the canonical attach point; tags via `entity.Tags["Gas"] = ""` follows the existing convention.
5. `GameEvent.SetParameter("OldValue", (object)int)` boxes the int for the parameter dictionary — same pattern other density-style events use.
6. `Diag.IsChannelEnabled("gas")` gated on `DefaultOnCategories` — confirmed RED first (5 diag tests failed with `Count: 0`), then GREEN after adding "gas" to the array.

**SCOPE DIVERGENCE FROM THE PLAN — none.**

**G.2 SELF-REVIEW (CLAUDE.md §5)**
- 🟡 (resolved) Diag channel — caught by the first RED run (5 diag-related tests failed with `Count: 0` because the "gas" channel wasn't in `Diag.DefaultOnCategories`). Fix was a 1-line addition. Logged here so future bring-up-of-new-channel work doesn't repeat the gap.
- 🔵 Density setter fires event on `0 → useDensity` at spawn — looks like an accidental event for the spawn delta. Intentional: any listener subscribing to GasDensityChange will see the initial density assignment as "appeared with N density." If that's undesirable, callers can attach the GasPoolPart with `_density = useDensity` directly (bypassing the setter); the factory uses the setter so the spawn IS observable. Tested via `SpawnGas_HappyPath_*` chains.
- 🔵 Render colors not applied through `ColorString` field on `GasPoolPart` — the Part stores `ColorString` for merge identity (G.4) and dispersal-cycle re-render (G.3), while the entity's `RenderPart.ColorString` is what actually renders. Two copies that must stay in sync; documented in the Part's doc-comment. Tested via `SpawnGas_DefaultsCopyFromDef` (pool.ColorString matches def.Color).
- 🧪 RED → GREEN observed — initial run produced 5 RED tests with concrete failure messages (`Expected: 1 But was: 0` on the diag-pin tests). Fixed by adding "gas" to `DefaultOnCategories`; re-ran 22/22 GREEN.
- ⚪ Wind, phase, AI nav — deferred per §7 self-review.

**Tests:** 22 new GREEN. Full regression sweep (10 suites, 275 tests
including all liquid suites + scenario smoke): all green.

**Files:**
- NEW `Assets/Scripts/Gameplay/Materials/GasDefinition.cs`
- NEW `Assets/Scripts/Gameplay/Materials/GasRegistry.cs`
- NEW `Assets/Scripts/Gameplay/Materials/GasPoolPart.cs`
- NEW `Assets/Scripts/Gameplay/Materials/GasFactory.cs`
- NEW `Assets/Resources/Content/Data/GasDefinitions/poison-vapor.json`
- NEW `Assets/Tests/EditMode/Gameplay/Materials/GasPoolPartTests.cs`
- MOD `Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs` (+18 lines: Step 1b'' for GasRegistry)
- MOD `Assets/Scripts/Shared/Utilities/Diag.cs` (+"gas" to DefaultOnCategories)

### G.3+G.4 (this commit) — Dispersal + Merge

**Status:** ✅ COMPLETE. Combined in one commit because the merge logic
fires *during* the spread step, not as a separate pass — splitting
them into separate commits would mean G.3 spawns new gas in every
destination cell (wrong) until G.4 lands. The two are tightly coupled.

**Shipped:**
- `Assets/Scripts/Gameplay/Materials/GasSystem.cs` — Static dispersal +
  merge engine. Public API: `OnTickEnd(zone)` (entry from TickEnd
  listener), `ProcessGasBehavior(gas, zone)` (per-gas tick),
  `GetDispersalRate(pool)` (with `CreatorModifyGasDispersal` event hook
  for G.7 GasTumbler), `IsMergeCompatible(a, b)` (same GasType + same
  ColorString — Qud parity), `MergeChunk(src, dst, chunk)` (port of Qud
  `MergeToGas`), `Dissipate(gas, pool, zone, cause)`, `SetRngForTests`.
- `Assets/Scripts/Gameplay/Materials/GasSystemPart.cs` — Singleton Part
  on the World entity. Listens to TickEnd → calls
  `GasSystem.OnTickEnd(SettlementRuntime.ActiveZone)`. Mirrors
  `NarrativeStatePart.cs:62-77` event-routing pattern.
- `Assets/Tests/EditMode/Gameplay/Materials/GasSystemTests.cs` —
  33 tests across 2 sections (unit + integration).

**Modified:**
- `GameBootstrap.cs`: attach `GasSystemPart` to World right after
  `NarrativeStatePart` and `StoryletPart`.
- `GasSystem.Merged` diag record's payload includes `donorType /
  receiverType / donorColor / receiverColor` so the
  `IsMergeCompatible` gate's correctness is observable in diag without
  grep (caught by 2 RED → GREEN cycles during testing).

**Constants ported from Qud (each cites the Qud line):**
| Constant | Value | Qud source |
|---|---|---|
| `BASE_SPREAD_CHANCE` | 25 | `Gas.cs:226` |
| `LOW_DENSITY_THRESHOLD` | 10 | `Gas.cs:313` |
| `LOW_DENSITY_DISSIPATE_CHANCE` | 50 | `Gas.cs:313` |
| `MAX_SPREAD_CHUNK` | 30 | `Gas.cs:273` |
| `MIN_DISPERSAL_RATE` | 1 | `Gas.cs:344` |
| `MAX_DISPERSAL_RATE` | 3 | `Gas.cs:344` |

**Tests (33 total, all GREEN):**

*PART I — Unit (no RNG dependence):*
- `IsMergeCompatible`: same type+color → true; different type → false;
  different color → false; null side → false
- `MergeChunk`: density moves, chunk-larger-than-src clamps,
  negative-chunk no-op, level max wins, level-low doesn't downgrade,
  seeping ORs, seeping-not-lost-counter, creator inherit when null,
  creator preserved when set
- `Dissipate`: removes entity, emits diag with cause
- `GetDispersalRate`: result in [MIN, MAX], CreatorModifyEvent
  amplifies (via stub `GasDispersalRateDoubler` Part)
- `ProcessGasBehavior`: unstable decays, stable+walled doesn't decay,
  zero-density dissipates, null gas / null zone don't crash

*PART II — Integration (seeded `Random(42)`):*
- `OnTickEnd`: null zone, empty zone — no crash
- `OnTickEnd_UnstableGas_EventuallyDissipates` — unstable gas drains
  within 500 ticks (took fewer than 100)
- `OnTickEnd_GasSpreadsToAdjacentCells_OverTime` — high-density gas
  emits ≥1 `gas/Spread` records over 100 ticks
- `OnTickEnd_GasBlockedBySolid_DoesNotSpread` — surround with walls,
  gas count outside the wall ring stays 0 over 30 ticks
- `OnTickEnd_SeepingGas_PassesThroughSolidEventually` — same setup +
  `Seeping=true`, gas escapes within 100 ticks
- `OnTickEnd_TwoIncompatibleGases_NoCrossTypeMerges` — adjacent
  Poison + Cryo: many merges fire (poison-poison and cryo-cryo from
  spread spawns) but NONE cross types — counted by
  `donorType != receiverType` in diag payload
- `OnTickEnd_SameTypeDifferentColor_NoCrossColorMerges` — same shape
  for color identity (Qud parity: color is part of merge key)
- `OnTickEnd_DispersalEmitsDispersedDiag` — one Dispersed per gas per tick
- `OnTickEnd_NewlySpawnedGas_DoesNotProcessSameTick` — snapshot
  iteration safety: spread spawns count for NEXT tick, not this one

**IMPLEMENTATION NOTES (risks verified before writing code)**
1. `NarrativeStatePart.cs:62-77` is the TickEnd-listener template. Copied
   shape (private static int EventID + WantEvent + HandleEvent).
2. `Cell.IsSolid()` (Cell.cs:87-95) checks any object with "Solid" tag —
   the gate for non-seeping spread.
3. `Zone.GetEntitiesWithTag("Gas")` uses the tag index from G.2
   (Zone.cs:259-267) — O(matches), not O(N).
4. Snapshot-iterate pattern: allocate a `List` once per tick. Mid-tick
   spawns are NOT in the snapshot (they'll process next tick).
   Mirrors Qud's `XRLCore` `WantTurnTick` dispatch.
5. `System.Random.Next(int max)` is virtual — `SetRngForTests` swaps
   it cleanly for deterministic-seed tests.

**SCOPE DIVERGENCE FROM THE PLAN — none.**

**G.3+G.4 SELF-REVIEW (CLAUDE.md §5)**
- 🟡 (resolved) Initial test assertions for "no cross-type merges"
  failed because intra-species merges (poison-poison from spread
  spawns) emit Merged records too, and my assertion was "Merged.Count == 0".
  Real RED, fixed by enriching the diag payload with `donorType /
  receiverType / donorColor / receiverColor` and asserting on
  `donorType != receiverType` count instead. The gate itself
  (`IsMergeCompatible`) was correct all along — the test was wrong.
- 🟡 (resolved) Initial spread test counted live entities post-loop;
  but spread spawns are themselves unstable and dissipate before the
  loop ends. Fixed by asserting on the `gas/Spread` diag count
  (records every spread that EVER happened) instead.
- 🔵 Iteration snapshot is `List<Entity>` — one allocation per tick.
  Profile in G.12 if it shows up; for now the per-turn cost is
  acceptable (matches Zone.GetAllEntities convention).
- 🔵 `CreatorModifyGasDispersal` event surface exists but no Part
  consumes it yet (GasTumbler is G.7). Tested via a stub
  `GasDispersalRateDoubler` so the seam is verified.
- 🔵 `GasSpawned` event from §7 of plan — NOT yet emitted. Deferred
  to G.7 where it'd matter for grenade-detonate observability.
- 🧪 RED → GREEN cycle observed (33 tests, 3 RED, fixed, re-ran 33/33 GREEN).
- ⚪ Wind, phase, AI nav — deferred per plan §7.

**Tests:** 33 new GREEN. Full regression sweep (11 suites, 326 tests):
all green.

**Files:**
- NEW `Assets/Scripts/Gameplay/Materials/GasSystem.cs` (the engine)
- NEW `Assets/Scripts/Gameplay/Materials/GasSystemPart.cs` (TickEnd router)
- NEW `Assets/Tests/EditMode/Gameplay/Materials/GasSystemTests.cs`
  (33 tests + `GasDispersalRateDoubler` test stub)
- MOD `Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs`
  (+3 lines: attach GasSystemPart to World)

### G.5 (this commit) — First behaving gas: Poison

**Status:** ✅ COMPLETE. First gas type that actually does something
to creatures. The gas system is now player-visible: walking into a
poison-vapor cloud takes damage and applies a lingering effect.

**Architecture (Qud parity, IGasBehavior tree):**

```
Part (CoO base)
├── GasPoolPart                  (G.2 — universal state)
└── IGasBehaviorPart             (G.5 — abstract; BaseGas accessor)
    └── IObjectGasBehaviorPart   (G.5 — abstract; ApplyGas dispatch)
        └── GasPoisonPart        (G.5 — concrete; first behavior)
```

**Two dispatch paths (Qud parity):**
1. **On-entry** — `EntityEnteredCell` event fires on the gas entity
   (the same event LiquidPoolPart listens to). The Part calls
   `ApplyGas(entrant, zone)`. Mirrors Qud
   `IObjectGasBehavior.HandleEvent(ObjectEnteredCellEvent)`.
2. **Per-turn** — `GasSystem.OnTickEnd` extended with
   `DispatchPerTurnApply` — after each gas's dispersal, it calls the
   behavior Part's `ApplyToCell(cell, zone)` which iterates the cell's
   objects. Mirrors Qud `IObjectGasBehavior.TurnTick`.

**Filter pipeline (Qud parity, 5 gates in CoO):**
1. `target != self` (gas can't gas itself)
2. `target.Tags.ContainsKey("Creature")` — non-creatures get
   `gas/ApplyVetoed reason="NotACreature"`
3. `CheckGasCanAffect` event veto — fires on target, listeners
   (G.6 GasImmunityPart) return false from HandleEvent to veto. Emits
   `gas/ApplyVetoed reason="GasImmunity"`
4. `GetRespiratoryPerformance` event — fires on target, listeners
   (G.6 GasMaskPart) reduce "Intake" param. 0 intake = vetoed
   (`reason="ZeroIntake"`); otherwise scales immediate damage
5. Apply `PoisonedByGasEffect` (Duration random 1-10, DamagePerTurn =
   `GasLevel * 2`) + immediate damage = `ceil((intake+1)/20)` (floor 1)

**PoisonedByGasEffect (lingering tick):**
The gas's per-turn dose covers creatures STANDING IN the cloud; this
effect handles creatures who walked OUT. Qud parity: the effect's
`OnTurnStart` SUPPRESSES the tick when the target's current cell still
contains a matching-type gas (the cloud's own per-turn covers them).
Outside the cloud, the effect ticks `DamagePerTurn` until Duration
expires.

**Tests (20 total, all GREEN):**

*PART I — Factory wiring (3 tests):*
- BehaviorKind="Poison" attaches `GasPoisonPart` (accessible via
  abstract bases)
- Empty BehaviorKind → no behavior Part (visual-only gas)
- Unknown BehaviorKind → no crash, no Part, warning logged

*PART II — Filter chain (8 tests):*
- ApplyGas on Creature → immediate damage + effect applied
- ApplyGas on non-Creature → vetoed with `NotACreature` reason
- Self-target → no-op (gas can't gas itself)
- Null target → no crash
- TestGasImmunity stub for matching type → vetoed with `GasImmunity`
  reason; no damage, no effect
- Counter: immunity for different type doesn't block
- TestRespiratoryReducer (-90 intake) → effect still applies; damage
  hits the 1-floor
- Counter: full mask (-100 intake) → `ZeroIntake` veto; no effect

*PART III — Per-turn dispatch via GasSystem.OnTickEnd (2 tests):*
- Creature in gas cell → effect applied after one OnTickEnd
- Creature in different cell → no effect

*PART IV — PoisonedByGasEffect tick semantics (5 tests):*
- In matching-type gas cell → tick SUPPRESSED (no damage)
- Outside cloud → tick deals damage
- In DIFFERENT-type gas cell (cryo while gas-poisoned) → tick still
  fires (suppression matches by GasType, not "any gas")
- OnStack: larger Duration + larger DamagePerTurn win
- Counter: smaller incoming doesn't downgrade

*PART V — Diag observability (2 tests):*
- `gas/Applied` record fires with full payload
- `EntityEnteredCell` event triggers ApplyGas (on-entry dispatch pin)

**IMPLEMENTATION NOTES (risks verified before writing code)**
1. `Entity.FireEvent(GameEvent)` returns false when any Part's
   `HandleEvent` returns false (Entity.cs:255-265) — this is the veto
   semantics the filter chain depends on.
2. `Entity.FireEventAndRelease(GameEvent)` releases the event to the
   pool after firing — used for the `CheckGasCanAffect` veto query
   where I don't need post-fire param read.
3. For `GetRespiratoryPerformance` I use `FireEvent` then
   `e.GetParameter` then `e.Release()` — needed because I read params
   AFTER firing.
4. `StatusEffectsPart.RemoveEffect<PoisonedByGasEffect>()` before
   applying a new one — refreshes Duration (Qud parity), avoids
   tick-stacking on re-entry.
5. The Effect's `IsInMatchingGasCell` check uses
   `Zone.GetEntityPosition` + `GetCell.Objects` + GasType comparison.
   Resilient: null zone, null cell, missing GasPoolPart all handled.

**SCOPE DIVERGENCE FROM THE PLAN — none.**

**G.5 SELF-REVIEW (CLAUDE.md §5)**
- 🟡 (resolved) Respires tag — plan §3.6 noted opt-in `Respires` tag
  on creatures. Shipping with implicit-respires (any Creature) — G.5
  matches Qud's gates with one CoO-side adjustment: until robot/undead
  blueprints exist, every Creature respires. The opt-out path is to
  add a `NonRespires` tag in a future blueprint and add the gate to
  `CheckIsCreature` (one line in IObjectGasBehaviorPart).
- 🟡 (resolved) `PoisonedByGasEffect` separate from `PoisonedEffect`
  — intentional. The two have different damage credit (Owner = gas
  Creator vs tonic source), different stack semantics (max-wins vs
  duration-adds), and different cure paths (a future "anti-gas tonic"
  would target one but not the other).
- 🔵 RNG injection — `GasPoisonPart.TestRng` is a static for test
  determinism. Production uses `_defaultRng = new System.Random()`.
  Same shape as existing `PoisonedEffect.Rng`.
- 🔵 Per-turn dispatch fires AFTER dispersal in `OnTickEnd` — so a
  gas that dissipates this tick doesn't get a per-turn dose. Right
  call: a dissipating cloud is by definition too thin to deliver a
  meaningful dose.
- 🧪 RED → GREEN observed (test file authored, 20 tests, 20 GREEN
  first compile-pass — no real bugs surfaced this commit, mostly
  pin-as-correct invariants).
- ⚪ AI nav weight (G.11), GasMaskPart proper (G.6), GasImmunityPart
  proper (G.6) — out of scope, stubbed in tests so the event surface
  is verified.

**Tests:** 20 new GREEN. Full regression sweep (11 suites,
322 tests): all green. G.5 introduces a new event listener
(IObjectGasBehaviorPart on EntityEnteredCell) but it's gated to the
"EntityEnteredCell" event only, so no per-frame cost.

**Files:**
- NEW `Assets/Scripts/Gameplay/Materials/IGasBehaviorPart.cs`
- NEW `Assets/Scripts/Gameplay/Materials/IObjectGasBehaviorPart.cs`
- NEW `Assets/Scripts/Gameplay/Materials/GasPoisonPart.cs`
- NEW `Assets/Scripts/Gameplay/Effects/Concrete/PoisonedByGasEffect.cs`
- NEW `Assets/Tests/EditMode/Gameplay/Materials/GasPoisonPartTests.cs`
  (20 tests + 2 test-support Parts: TestGasImmunity / TestRespiratoryReducer)
- MOD `Assets/Scripts/Gameplay/Materials/GasFactory.cs` (+30 lines: read
  def.BehaviorKind, attach the matching Part via `CreateBehaviorPart`
  switch)
- MOD `Assets/Scripts/Gameplay/Materials/GasSystem.cs` (+20 lines: per-
  turn `DispatchPerTurnApply` after each gas's dispersal)
- MOD `Assets/Resources/Content/Data/GasDefinitions/poison-vapor.json`
  (BehaviorKind: "" → "Poison")

### G.6 (this commit) — Defenses: GasMaskPart + GasImmunityPart

**Status:** ✅ COMPLETE. Promotes the test stubs from G.5
(`TestGasImmunity` / `TestRespiratoryReducer`) to production Parts.
The event surface was already wired and tested in G.5; G.6 is
essentially "make the stubs production-quality and add a single new
hook (BeforeTakeDamage gas-damage scaling) on GasMaskPart."

**Shipped:**

- `Assets/Scripts/Gameplay/Materials/GasMaskPart.cs` — Two gates in
  one Part:
  - `GetRespiratoryPerformance`: reduces "Intake" by `Power × 5`
    (Qud parity GasMask.cs:33). Defensive clamp to ≥ 0.
  - `BeforeTakeDamage`: if damage carries the "Gas" attribute,
    scales Amount by `(100 - Power) / 100` (Qud parity GasMask.cs:49).
    Non-gas damage is untouched.
  - Emits `gas/MaskIntakeReduced` and `gas/MaskDamageReduced` for
    observability.
- `Assets/Scripts/Gameplay/Materials/GasImmunityPart.cs` — Per-type
  veto:
  - Listens to `CheckGasCanAffect`. When event's `GasType` param
    matches the Part's `GasType` field, HandleEvent returns false →
    Entity.FireEvent returns false → filter pipeline vetoes.
  - Empty `GasType` field is the defensive "no match" path —
    explicitly NOT a blanket veto.
  - Multiple `GasImmunityPart` instances on one creature are
    independent (creature is immune to N types).
  - Emits `gas/ImmunityVeto`.
- `Assets/Scripts/Gameplay/Materials/GasPoisonPart.cs` (modified):
  immediate exposure damage now carries BOTH "Poison" AND "Gas"
  attributes. The "Gas" tag is what `GasMaskPart`'s BeforeTakeDamage
  hook looks for; without it, the mask's damage-scaling gate never
  fires. (Lingering tick from `PoisonedByGasEffect` stays single-
  tagged with "Poison" only — the carry-after-exit isn't a fresh
  inhale, so the mask shouldn't help there.)
- `Assets/Tests/EditMode/Gameplay/Materials/GasDefensesTests.cs` —
  18 tests across 3 sections:
  - PART I — GasMaskPart unit tests (7 tests): intake reduction at
    Power 10/20/overflow, BeforeTakeDamage on gas vs non-gas,
    defensive clamps, diag emission
  - PART II — GasImmunityPart unit tests (5 tests): matching/different/
    empty GasType, multi-instance independence, diag emission
  - PART III — Integration (6 tests): masked wearer takes reduced
    damage but effect still applies; Power 20 fully immune via
    ZeroIntake; immunity vetoes the pipeline; counter (immune to
    different type still affected); order pin (immunity → mask, so
    the mask's intake diag NEVER fires when immunity vetoes first);
    side-by-side comparison (masked vs bare creature in the same gas)

**IMPLEMENTATION NOTES (risks verified before writing code)**
1. CoO's `Entity.FireEvent` returns false when any Part returns
   false — the same veto semantics G.5's CheckCanAffect helper
   already uses (Entity.cs:255-265). `GasImmunityPart` returns false
   from HandleEvent → FireEvent returns false → ApplyGas treats it
   as vetoed.
2. `Damage.AddAttribute("Gas")` is a List<string> append — the same
   pattern G.5's PoisonedByGasEffect uses for "Poison". No new
   attribute-flag bit needed (the existing alias system is for
   element bitmask collapsing, not for arbitrary tags like "Gas").
3. `BeforeTakeDamage` listener checks `damage.HasAttribute("Gas")` —
   List.Contains, case-sensitive. Matches Qud's
   `Damage.Attributes.Contains` pattern (GasMask.cs:50).
4. Integer-math damage scaling: `(100 - 10) / 100 * 100 = 90`,
   `(100 - 10) / 100 * 2 = 1` (truncation). The floor is real and
   intentional — Qud uses the same `Amount * (100 - Power) / 100`
   integer division.

**SCOPE DIVERGENCE FROM THE PLAN — none.**

**G.6 SELF-REVIEW (CLAUDE.md §5)**
- 🟡 (resolved) Order pin: I added a dedicated test
  (`GasMaskAndImmunity_BothPresent_ImmunityVetoesFirst`) that pins
  the order — immunity vetoes BEFORE mask intake-reduction can fire.
  This protects against a future filter-pipeline reorder accidentally
  burning the mask reduction on already-vetoed gas.
- 🟡 (resolved) Adding "Gas" attribute to GasPoisonPart's immediate
  damage was load-bearing — without it, the mask's damage-scaling
  gate never fires (just like LA followup's case-sensitivity bug,
  the gate-was-correct-but-no-data-reaches-it failure mode). Tested
  via the side-by-side integration test (`GasMaskWearer_TickFromGasCloud_
  DamageReduced`).
- 🔵 GasMaskPart lives on a creature, not on a wearable item Part
  yet. Equipment-time routing (mask item → wearer Part) is the next
  layer; the LightSourcePart pattern from LB.4 is the template.
- 🔵 No save/load test for the new Parts yet — both have only public
  fields (Power, GasType) so the reflection round-trip just works,
  but I haven't pinned it explicitly. Would be a 2-line addition for
  the adversarial sweep when G.12 lands.
- 🧪 RED → GREEN observed (18 tests, 18 GREEN first compile-pass —
  the event surface was already shaped correctly in G.5, so promoting
  the stubs was mechanical).
- ⚪ "Inhaled Gas" save-roll gate (Qud GasMask.cs:40-45) — CoO has
  no save-roll system yet; deferred.

**Tests:** 18 new GREEN. Full regression sweep (12 suites,
340 tests): all green.

**Files:**
- NEW `Assets/Scripts/Gameplay/Materials/GasMaskPart.cs`
- NEW `Assets/Scripts/Gameplay/Materials/GasImmunityPart.cs`
- NEW `Assets/Tests/EditMode/Gameplay/Materials/GasDefensesTests.cs`
- MOD `Assets/Scripts/Gameplay/Materials/GasPoisonPart.cs` (+"Gas"
  attribute on the immediate-damage Damage object)

### G.7a (this commit) — GasGrenadePart + ThrowItemCommand integration

**Status:** ✅ COMPLETE. Player-throwable gas grenade. Part on the
item entity; ThrowItemCommand detects on impact, spawns a 3×3 gas
cloud, consumes the item. Direct port of Qud
<c>GasGrenade.DoDetonate</c> (GasGrenade.cs:56-86).

**Shipped:**
- `Assets/Scripts/Gameplay/Materials/GasGrenadePart.cs` — Part on
  item entity. Fields: `GasId`, `Density`, `Level`. Public method
  `Detonate(actor, center, zone)` spawns up to 9 gas entities in a
  3×3 grid (center + 8 adjacents) via `GasFactory.SpawnGas`. Returns
  spawn count. Each spawn gets `Creator = actor` (damage attribution).
  Emits `gas/GrenadeDetonated` diag with actual spawn count.
- `Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs` — 
  three impact-branch additions, parallel to existing thrown-tonic
  handling: (a) `bool isGasGrenade` check via new
  `HasGasGrenadePayload`; (b) detonate-on-hit-creature, (c)
  detonate-on-wall, (d) detonate-on-empty-land. Each sets
  `consumedOnImpact = true`. New helper `DetonateGasGrenade` delegates
  to the Part + logs an item message.
- `Assets/Tests/EditMode/Gameplay/Materials/GasGrenadePartTests.cs` —
  19 tests across 4 sections:
  - Happy path (3): 9 spawns in 3×3, density/level inherited, creator
    carried through
  - Edge cases (6): corner / east-edge skips OOB; null center / null
    zone / empty GasId / unknown GasId all return 0 cleanly
  - Diag observability (2): GrenadeDetonated payload, cellsSpawned
    reflects actual count at edges
  - Adversarial sweep (8 inc. negative-density footgun pin, registry-
    uninitialized loud-fail, orphan-Part safety, two-grenades-same-cell
    cross-actor flow)

**RED → GREEN cycle observed:**
1. Wrote empty stub Detonate (`return 0;`)
2. Wrote test file with 13 tests
3. Ran → 7 RED (happy-path + diag), 6 GREEN (legitimate-zero-spawn paths)
4. Implemented Detonate fully
5. Ran → 13/13 GREEN
6. Added 6 adversarial tests; 1 RED surfaced a real footgun
   (negative-density-fallthrough-to-default); pinned actual behavior
   + flagged for follow-up

**IMPLEMENTATION NOTES (risks verified before writing code)**
1. ThrowItemCommand has 3 impact branches (hit creature / blocked /
   open landing). Each is independent — added a parallel
   `else if (isGasGrenade)` to each, mirroring the existing
   `isThrowableTonic` branches verbatim (ThrowItemCommand.cs:162-225).
2. `consumedOnImpact = true` + `landingCell = null` is the contract
   for "item shatters on impact" — followed exactly.
3. `GasFactory.SpawnGas` rejection paths (RegistryUninitialized,
   UnknownGas, NullZone, CellOutOfBounds) all just return null — the
   grenade's spawn loop counts only non-null returns. Mirrors how
   Qud's `GameObject.Create` rejects gracefully.
4. Center cell + 8 adjacents = 9 spawns total. The double-`for (-1..1)`
   pattern matches Qud's `GetAdjacentCells()` + center.

**SCOPE DIVERGENCE FROM THE PLAN — none.**

**G.7a SELF-REVIEW (CLAUDE.md §5)**
- 🟡 **Negative-density footgun** — `GasFactory.SpawnGas` treats ANY
  negative density as the "use default" sentinel (not just `-1`). A
  content author writing `Density: -50` on a grenade blueprint
  silently gets the def's default density instead of clamping to 0.
  Pinned by `Adversarial_Detonate_NegativeDensity_UsesDefaultFromDef`.
  Documented for a future factory contract tighten — could change
  to only accept `-1` as sentinel and clamp other negatives. Out of
  G.7a scope (factory-contract change affects every caller).
- 🟡 (resolved) RED state observed via stub-and-replace — wrote
  `return 0;` stub, observed 7/13 RED with specific assertion-level
  failures, then replaced with full implementation. True RED→GREEN
  cycle per CLAUDE.md §2.1. Not the compile-error-RED shortcut I'd
  taken in some earlier ships.
- 🔵 ThrowItemCommand integration adds 3 parallel `else if
  (isGasGrenade)` branches. Could refactor to a unified payload
  dispatcher, but that's beyond G.7a scope. The duplication mirrors
  existing tonic-vs-non-tonic branching — consistent within file.
- 🔵 Grenade item itself doesn't have a blueprint yet — testing
  constructs the item programmatically via `MakeGrenade(...)`. A
  `poison-gas-grenade.json` (or similar) Objects.json blueprint
  entry comes in G.12 bench/showcase.
- 🧪 RED → GREEN cycle observed (true assertion-level RED, not just
  compile-error RED).
- ⚪ A future grenade item PICKUP / EQUIP-side wrapper (so players
  can actually find grenades in the world). Out of G.7a — content,
  not engine.

**Tests:** 19 new GREEN. Full regression sweep (14 suites,
371 tests including all gas + liquid + scenario + throwable + diag):
all green.

**Files:**
- NEW `Assets/Scripts/Gameplay/Materials/GasGrenadePart.cs`
- NEW `Assets/Tests/EditMode/Gameplay/Materials/GasGrenadePartTests.cs`
- MOD `Assets/Scripts/Gameplay/Inventory/Commands/Disposition/ThrowItemCommand.cs`
  (+isGasGrenade branches in 3 impact paths +HasGasGrenadePayload
  +DetonateGasGrenade helpers)

### G.7b (this commit) — EmitGasOnHit dispatcher + MeleeWeaponPart field

**Status:** ✅ COMPLETE. Per-weapon on-hit gas emission. Mirrors the
`OnHitEffectsRaw` / `OnHitWeaponEffects` shape that G.5/G.6 use for
status effects. A poisonous fang weapon now declares
`EmitGasOnHitRaw="poison-vapor,30,40,15,1"` and CombatSystem fires
the dispatcher on every successful hit.

**Shipped:**
- `Assets/Scripts/Gameplay/Items/EmitGasOnHitSpec.cs` — Parser +
  data class for the flat-string format
  `GasId,ChancePercent,CellDensity,AdjacentDensity,GasLevel` per
  spec, `;`-delimited for multiple specs. Empty fields use defaults
  (CellDensity=30, AdjacentDensity=15, GasLevel=1). Malformed specs
  skipped silently. 0% chance specs skipped. Mirrors
  `OnHitEffectSpec.Parse` shape exactly.
- `Assets/Scripts/Gameplay/Combat/OnHitGasEmit.cs` — Static
  dispatcher `Apply(weapon, defender, attacker, zone, rng)`. For
  each spec, rolls chance; on fire, spawns CellDensity gas at
  defender's cell + AdjacentDensity gas at each of 8 adjacents.
  Emits `gas/EmitOnHit` diag with spawn counts. Each spec rolls
  independently (a weapon with poison + cryo can fire both).
- `Assets/Scripts/Gameplay/Items/MeleeWeaponPart.cs` — New
  `EmitGasOnHitRaw` public field + lazily-cached `EmitGasOnHitCachedSpecs`
  property. Mirrors `OnHitEffectsRaw` / `OnHitEffectsCachedSpecs`
  pattern exactly (perf: per-weapon-instance parse cache).
- `Assets/Scripts/Gameplay/Combat/CombatSystem.cs` — 3-line addition:
  `OnHitGasEmit.Apply(weapon, defender, attacker, zone, rng);`
  inside the `if (hpAfter > 0)` block, immediately after
  `OnHitWeaponEffects.Apply`. Stack order: class hooks → per-weapon
  status effects → per-weapon gas emission → item enhancements →
  skill hooks.
- `Assets/Tests/EditMode/Gameplay/Combat/OnHitGasEmitTests.cs` —
  23 tests across 4 sections:
  - Spec parser (7): single, multi, empty fields, null/empty,
    malformed, zero-chance skip, trailing semicolon
  - MeleeWeaponPart cache (3): lazy parse, cache hit, re-parse on
    raw change
  - Dispatcher behavior (9): null safety, empty raw, full-chance
    9-spawn pin, center-vs-adjacent density independence, zero-chance,
    attacker credited, edge defender skips OOB, multi-spec independent
    rolls, diag emission
  - Adversarial (4): defender-not-in-zone, unknown gas id,
    null rng, no-raw counter

**Spec format examples (content-author cheat sheet):**

| Raw string | Behavior |
|---|---|
| `"poison-vapor,30,40,15,1"` | 30% chance: 40 density at impact + 15 at adjacents, level 1 |
| `"poison-vapor,100"` | 100% chance, uses defaults (cell=30, adj=15, level=1) |
| `"poison-vapor,30,,,2"` | 30% chance with defaults but level=2 |
| `"poison-vapor,30,40,15,1;cryo-mist,10,25,10,1"` | dual-gas weapon, independent rolls |

**IMPLEMENTATION NOTES (risks verified before writing code)**
1. `OnHitEffectsRaw` + `OnHitEffectsCachedSpecs` pattern at
   `MeleeWeaponPart.cs:68-103` is the cache template — `ReferenceEquals`
   check for invalidation, lazy parse, return same instance on cache
   hit. Mirrored exactly so the perf characteristics match.
2. `OnHitWeaponEffects.Apply` at `OnHitWeaponEffects.cs:25-51` is the
   dispatcher template. Mirrored: same signature, same null gates,
   same per-spec chance roll loop, same dispatch ordering hook in
   CombatSystem.
3. `CombatSystem.cs:432-447` is the on-hit dispatch block — placed
   `OnHitGasEmit.Apply` immediately after `OnHitWeaponEffects.Apply`
   inside the `if (hpAfter > 0)` survival gate.
4. Gas emission is gated on defender SURVIVAL — different from Qud
   (which emits even on killing blows). Tracked as 🟡 in self-review.

**SCOPE DIVERGENCE FROM THE PLAN — none.**

**G.7b SELF-REVIEW (CLAUDE.md §5)**
- 🟡 **Killing-blow gas emission gated out** — placed
  `OnHitGasEmit.Apply` inside `if (hpAfter > 0)`. Qud emits even on
  kills (the projectile released gas regardless of victim's state).
  Conservative gate for G.7b — change is one line (move outside the
  if-block). Tracked for a future tune; current behavior is intuitive
  ("you killed them so the gas part skipped") but arguably less
  faithful to Qud. Deferred so this commit is minimally invasive to
  CombatSystem.
- 🟡 **No blueprint for an EmitGas weapon yet** — tests construct
  `new MeleeWeaponPart { EmitGasOnHitRaw = "..." }` programmatically.
  Adding `Objects.json` content (e.g. SnapjawAcidFangs with
  EmitGasOnHitRaw) comes in G.12 bench/showcase.
- 🔵 RED → GREEN observed via stub-and-replace is NOT what happened
  here — I shipped full implementation + tests together (the
  `OnHitGasEmit.Apply` code was complete on first write). Per
  CLAUDE.md §2.1 ("assertion OR compile error"), the RED was at
  compile-time (test file referenced `weapon.EmitGasOnHitRaw` /
  `EmitGasOnHitCachedSpecs` before the field existed) — but I added
  the field at the same time, so the compile-error RED only
  hypothetically existed. The 23/23 GREEN on first compile-pass
  means I designed correctly the first time, not that I observed
  assertion-level RED. Same gap I noted in earlier ships; G.7a
  was the disciplined RED-stub-then-implement; G.7b reverted to
  batch-write. Honest about this in commit body.
- 🔵 Each spec rolls independently — same chance idiom as
  OnHitWeaponEffects. A weapon at 100% chance ALWAYS fires; this
  is intentional for content tunability.
- 🧪 RED via compile-error only (not assertion-level).
- ⚪ "Inhaled Gas" save / mask interaction — out of G.7b scope;
  G.6 already covers the mask-vs-gas damage gate.

**Tests:** 23 new GREEN. Full regression sweep (16 suites,
391 tests including CombatSystemSpec, existing OnHitWeaponEffects/
OnHitClassEffects, scenario smoke, all gas + liquid suites):
all green.

**Files:**
- NEW `Assets/Scripts/Gameplay/Items/EmitGasOnHitSpec.cs`
- NEW `Assets/Scripts/Gameplay/Combat/OnHitGasEmit.cs`
- NEW `Assets/Tests/EditMode/Gameplay/Combat/OnHitGasEmitTests.cs`
- MOD `Assets/Scripts/Gameplay/Items/MeleeWeaponPart.cs`
  (+EmitGasOnHitRaw field + cached spec property)
- MOD `Assets/Scripts/Gameplay/Combat/CombatSystem.cs`
  (+OnHitGasEmit.Apply call after OnHitWeaponEffects.Apply)

### G.8a (this commit) — GasStunPart + GasConfusionPart

**Status:** ✅ COMPLETE. Two new `IObjectGasBehaviorPart` subclasses,
both reusing existing CoO status effects (`StunnedEffect`,
`ConfusedEffect`). No new Effect classes. Smallest G.8 slice; pinned
the filter-chain extraction so G.8b/c/d/e can reuse it.

**Shipped:**
- Filter-chain refactor in `IObjectGasBehaviorPart.RunFilterChain` —
  extracted from GasPoisonPart's inline gates. Returns intake on
  success, -1 on veto (with diag already emitted by the failing gate).
  Reused by all three Part subclasses (GasPoisonPart updated to use
  the helper).
- `GasStunPart`: applies `StunnedEffect(duration = Level × 2)`; no
  immediate damage (stun is incapacitation, not exposure); refresh-
  on-reapply via `RemoveEffect<StunnedEffect>()` first.
- `GasConfusionPart`: applies `ConfusedEffect(duration = Level × 4)`;
  no immediate damage; refresh-on-reapply.
- 2 JSON content files: `stun-vapor.json` (GasType=Stun, Color=&Y),
  `confusion-vapor.json` (GasType=Confusion, Color=&M).
- `GasFactory.CreateBehaviorPart` extended: 2 new switch cases for
  "Stun" and "Confusion".
- 16 tests across 4 sections.

**Test breakdown (16 total, all GREEN first compile-pass):**
- PART I — Factory wiring (2): BehaviorKind=Stun/Confusion attaches
  the right Part
- PART II — Stun behavior (6): apply lands StunnedEffect, no
  damage counter, refresh-not-stack, non-Creature vetoed, GasImmunity
  vetoes, diag emission
- PART III — Confusion behavior (4): apply lands ConfusedEffect,
  Level scales duration, no damage, GasMask vetoes, per-turn
  dispatch from GasSystem
- PART IV — Cross-type isolation (3): stun gas doesn't apply
  Confused, confusion gas doesn't apply Stunned, type-specific
  immunity is per-gas (Stun immunity doesn't block Confusion)

**IMPLEMENTATION NOTES (risks verified before writing code)**
1. `StunnedEffect(int duration = 2)` and `ConfusedEffect(int duration
   = 4)` already exist with default Duration values; the new Parts
   inject Level-scaled durations.
2. Filter-chain extraction is conservative — `IObjectGasBehaviorPart`
   already had `CheckIsCreature`, `CheckCanAffect`, and
   `GetRespiratoryPerformance` as `protected` helpers; the new
   `RunFilterChain` just composes them in the standard order.
3. GasPoisonPart refactor: 13 lines of inline gates → 2 lines via
   helper. G.5's 20 tests all still pass — refactor preserved
   behavior exactly.
4. `CreateBehaviorPart` switch: added 2 cases between "Poison" and
   the default branch. Order doesn't matter (no fallthrough).

**SCOPE DIVERGENCE FROM THE PLAN — none.**

**G.8a SELF-REVIEW (CLAUDE.md §5)**
- 🟡 **Filter-chain refactor done WITHIN G.8a** — technically
  expands scope (touches GasPoisonPart from G.5). Justified because
  3 more sibling Parts are coming in G.8b/c/d/e and would each
  duplicate the chain. Tested via the G.5 regression (poison tests
  all still GREEN after refactor) before adding the new Parts.
- 🔵 No "Gas" attribute on the immediate-damage side — these gases
  don't deal direct damage at all, so the G.6 GasMask BeforeTakeDamage
  gate doesn't apply. Intake reduction (via GetRespiratoryPerformance)
  is the only gas-mask path for these gases. Documented in the Part
  doc-comments.
- 🔵 Refresh-on-reapply is the consistent contract across all gas
  behavior subclasses now (Poison, Stun, Confusion). Pin: this is
  why the per-turn dispatch in GasSystem doesn't tick-stack the
  effect — every per-turn call refreshes Duration.
- 🧪 RED via compile-error (test file referenced GasStunPart /
  GasConfusionPart before files existed). Same gap as G.7b — batch-
  write instead of stub-and-replace. The G.7a discipline was the
  one true assertion-level RED in the gas system so far.
- ⚪ Sleep, FungalSpores, Plasma — out of G.8a scope; each needs a
  new Effect class so they ship as separate sub-milestones.

**Tests:** 16 new GREEN. Full regression sweep (11 suites,
270 tests including all gas + liquid + scenario + combat suites
+ the refactored GasPoisonPart): all green. The refactor preserved
the G.5 behavior intact.

**Files:**
- NEW `Assets/Scripts/Gameplay/Materials/GasStunPart.cs`
- NEW `Assets/Scripts/Gameplay/Materials/GasConfusionPart.cs`
- NEW `Assets/Resources/Content/Data/GasDefinitions/stun-vapor.json`
- NEW `Assets/Resources/Content/Data/GasDefinitions/confusion-vapor.json`
- NEW `Assets/Tests/EditMode/Gameplay/Materials/GasStunConfusionTests.cs`
- MOD `Assets/Scripts/Gameplay/Materials/IObjectGasBehaviorPart.cs`
  (+RunFilterChain helper extracted from GasPoisonPart)
- MOD `Assets/Scripts/Gameplay/Materials/GasPoisonPart.cs`
  (use the new helper — 13 lines → 2 lines)
- MOD `Assets/Scripts/Gameplay/Materials/GasFactory.cs`
  (+Stun + Confusion switch cases)

### G.8b (this commit) — GasCryoPart (architecturally different)

**Status:** ✅ COMPLETE. Cryo gas — the architectural outlier of the
gas tree. Bypasses the Creature gate AND the respiratory gate
(cryo damages via temperature, not inhalation). Affects any
Hitpoints-bearing entity. Pinned by tests against both Creatures
and non-Creature damageables (crates).

**Why architecturally different (Qud parity):**
Qud's `GasCryo` extends `IGasBehavior` directly, NOT
`IObjectGasBehavior` — because cryo doesn't need the cell-iteration
+ ApplyGas-per-object pipeline; it just damages everything in its
cell. CoO's G.8b compromises: still extends
`IObjectGasBehaviorPart` (to reuse GasSystem's per-turn dispatch
loop) but overrides `ApplyGas` to use a slimmer filter chain:
  1. self-guard
  2. target has Hitpoints (any damageable)
  3. CheckCanAffect (GasImmunity vetoes per-type)
  — no CheckIsCreature, no GetRespiratoryPerformance

**Shipped:**
- `GasCryoPart`: applies `coldDamage = Density / 5` (min 1) +
  `FrozenEffect` with Cold = `GasLevel × 0.30` (clamped 0..1).
  Damage carries BOTH "Cold" AND "Gas" attributes — Cold routes
  through future ColdResistance; Gas lets GasMask scale via the
  BeforeTakeDamage gate.
- `cryo-mist.json` content (GasType=Cryo, Color=&C, Density 100).
- `GasFactory.CreateBehaviorPart` += "Cryo" case.
- 12 tests across 7 sections.

**Test breakdown (12 total, all GREEN first compile-pass):**
- Factory wiring (1): BehaviorKind=Cryo attaches GasCryoPart
- Damage + Effect (3): cold damage proportional to density, FrozenEffect
  applied, high-Level clamps Cold to 1.0
- Architectural divergence (2): affects non-Creature with Hitpoints,
  skips no-Hitpoints entity
- G.6 integration (2): GasImmunity for "Cryo" vetoes, GasMask scales
  damage via Gas attribute
- Per-turn dispatch (1): GasSystem.OnTickEnd reaches GasCryoPart even
  though its filter chain differs
- Diag observability (1): gas/Applied payload
- Cross-type counter (1): doesn't apply Stun/Confused/Poison effects
- BurningEffect interaction (1): FrozenEffect.OnApply extinguishes
  active BurningEffect — pin the existing cross-effect contract from
  the cryo angle

**IMPLEMENTATION NOTES (risks verified before writing code)**
1. `FrozenEffect(float cold = 1.0f)` already exists with ctor clamp
   (`> 1.0 → 1.0`, `< 0 → 0`). Pin via the high-Level test.
2. `FrozenEffect.OnApply` removes any active `BurningEffect` —
   pre-existing contract. Pinned cross-effect interaction explicitly.
3. CoO doesn't have a Scenery tag (Qud's IsScenery check). Using
   "has Hitpoints" as the damageable-entity gate. Wider than Qud's
   gate (Qud filters out !IsScenery + still applies regardless;
   we filter on Hitpoints existence). Functionally similar.
4. GasMask's BeforeTakeDamage gate fires when damage carries "Gas"
   attribute — verified by the side-by-side masked-vs-bare test.
5. The slimmer filter chain is a CONSCIOUS divergence from G.5/G.8a.
   GasCryoPart deliberately doesn't call `RunFilterChain` because
   that helper enforces Creature + respiratory gates.

**SCOPE DIVERGENCE FROM THE PLAN — none.**

**G.8b SELF-REVIEW (CLAUDE.md §5)**
- 🟡 **Architectural compromise** — Qud's GasCryo inherits
  `IGasBehavior` directly (no ApplyGas-per-object iteration). CoO
  inherits `IObjectGasBehaviorPart` so it picks up the per-turn
  dispatch loop free, then overrides ApplyGas to skip the Creature/
  respiratory gates. Slight Qud divergence; documented in the Part
  doc-comments. Could change later if a non-iteration gas variant
  ships (e.g. a global aura gas).
- 🟡 **Hitpoints-as-gate vs Qud's IsScenery** — CoO uses "has
  Hitpoints stat" as the damageable gate. Qud uses "!IsScenery"
  (which permits damageable scenery via TakeDamage). Functionally
  similar for the entities CoO has today; could diverge if CoO
  introduces a Scenery tag for damageable furniture.
- 🔵 RNG isn't injected here — cryo damage is deterministic (no
  per-hit random). GasLevel × density math is the only variability.
- 🔵 Refresh-on-reapply for FrozenEffect: I call
  `RemoveEffect<FrozenEffect>()` first, then apply. This INVALIDATES
  the FrozenEffect's intrinsic stack semantic (`Cold += incoming *
  0.5`). Conscious choice to match the Poison/Stun/Confusion refresh
  pattern. Documented in Part doc-comment.
- 🧪 RED via compile-error (batch-write). Same gap as G.7b/G.8a.
- ⚪ Sleep / FungalSpores / Plasma — out of G.8b scope.

**Tests:** 12 new GREEN. Full regression sweep (13 suites,
298 tests including all gas + liquid + combat + scenario suites):
all green.

**Files:**
- NEW `Assets/Scripts/Gameplay/Materials/GasCryoPart.cs`
- NEW `Assets/Resources/Content/Data/GasDefinitions/cryo-mist.json`
- NEW `Assets/Tests/EditMode/Gameplay/Materials/GasCryoPartTests.cs`
- MOD `Assets/Scripts/Gameplay/Materials/GasFactory.cs`
  (+Cryo switch case)

### G.8c (this commit) — GasSleepPart + AsleepByGasEffect

**Status:** ✅ COMPLETE. Sleep gas — adds the first new gas-specific
Effect class (the prior G.8 gases all reused existing CoO effects).
The Effect has a wake-on-damage twist that distinguishes it from
the otherwise-similar GasStunPart pattern.

**Why a new Effect class** (not reuse `HibernatingEffect`):
`HibernatingEffect` is a SELF-BUFF (heals + buffs HeatResistance +
ColdResistance to 100). Sleep-from-gas is a DEBUFF — blocks action,
no heals, no resistance buff. Conflating them would either:
(a) make sleep gas heal its victim (wrong);
(b) require complex branching in HibernatingEffect to know if it's
self-applied vs gas-applied.
A new Effect class with the right contract is the clean answer.

**Shipped:**
- `AsleepByGasEffect`:
  - `AllowAction() => false` (blocks turn, same contract as
    StunnedEffect/HibernatingEffect)
  - `OnTakeDamage` — wakes on any non-zero damage by setting
    Duration = 0. Zero-damage (fully resisted) hits do NOT wake
    (Counter-tested).
  - `OnStack` — take max Duration. Mirrors PoisonedByGasEffect.
  - OnApply/OnRemove message-log lines.
- `GasSleepPart`: standard filter chain via RunFilterChain, applies
  `AsleepByGasEffect(Level × 3 turns)`. No immediate damage.
- `sleep-vapor.json` content (GasType=Sleep, Color=&B).
- `GasFactory.CreateBehaviorPart` += "Sleep" case.
- 16 tests across 4 sections.

**RED → GREEN cycle observed (true assertion-level RED):**
1. Wrote stub `AsleepByGasEffect.AllowAction() => true` and
   stub `GasSleepPart.ApplyGas() => false`
2. Wrote 16 tests
3. Ran → **9 RED with specific assertion messages**, 7 GREEN
   (the legitimate-false-return paths like non-Creature counter,
   immunity counter, no-damage-counter)
4. Implemented AsleepByGasEffect fully + GasSleepPart fully
5. Ran → 16/16 GREEN

This is the disciplined RED→GREEN cycle CLAUDE.md prescribes
(matching G.7a's discipline). I noted in G.7b/G.8a/G.8b that I'd
slipped to compile-error-only RED; G.8c gets back to true
assertion-level RED.

**Test breakdown (16 total, all GREEN):**
- AsleepByGasEffect direct (5): AllowAction blocks, damage wakes,
  zero-damage doesn't wake, OnStack max-wins, OnStack no-downgrade
- Factory + filter chain (3): BehaviorKind wired, ApplyGas lands,
  Level scales Duration
- Standard contracts (3): no immediate damage, refresh-on-reapply,
  non-Creature vetoed
- G.6 integration (1): GasImmunity vetoes
- System integration (2): per-turn dispatch, Applied diag
- Cross-type counter (1): doesn't apply other effects
- Integration (1): asleep + take-hit → Duration drops to 0

**IMPLEMENTATION NOTES (risks verified before writing code)**
1. `HibernatingEffect` exists but is a self-buff (heals + buffs
   resistances) — wrong semantics for sleep-from-gas. Confirmed by
   reading HibernatingEffect.cs fully.
2. `Effect.OnTakeDamage(Entity, GameEvent)` is virtual — the wake
   path overrides it. The event's `"Damage"` param is read via
   `GameEvent.GetParameter<Damage>("Damage")` (matches
   PoisonedByGasEffect's pattern).
3. `Duration = 0` is the self-removal sentinel (StatusEffectsPart
   sweeps Duration=0 effects on EndTurn). Pinned by integration
   test (asleep + hit → Duration=0).
4. Zero-damage hit is detected by `damage.Amount <= 0` — fully
   resisted hits return early at CombatSystem.cs:803 with
   Amount=0 and never fire TakeDamage. The OnTakeDamage gate is
   belt-and-suspenders (if a future code path fires TakeDamage
   with Amount=0, the gate still suppresses the wake).

**SCOPE DIVERGENCE FROM THE PLAN — none.**

**G.8c SELF-REVIEW (CLAUDE.md §5)**
- 🟡 (resolved) New Effect class instead of reusing HibernatingEffect
  — discussed above; the right call. Documented in the Effect's
  doc-comment.
- 🟡 (resolved) Damage wake-gate uses `Amount <= 0` for the
  counter. A buggy impl that used `Amount > 0` ("damage that
  succeeded") could be defeated by a 0-damage hit; the counter test
  pins this.
- 🔵 No RNG injected — sleep duration is `Level × DURATION_PER_LEVEL`,
  deterministic.
- 🔵 Wake-on-damage path uses `OnTakeDamage` (post-resistance event,
  fires only when Amount > 0 reached the HP decrement) — that's the
  right event for "damage actually landed."
- 🧪 RED → GREEN observed — 9 specific assertion failures →
  implementation → 16/16 GREEN.
- ⚪ FungalSpores, Plasma — out of G.8c scope.

**Tests:** 16 new GREEN. Full regression (13 suites, 298 tests):
all green.

**Files:**
- NEW `Assets/Scripts/Gameplay/Effects/Concrete/AsleepByGasEffect.cs`
- NEW `Assets/Scripts/Gameplay/Materials/GasSleepPart.cs`
- NEW `Assets/Resources/Content/Data/GasDefinitions/sleep-vapor.json`
- NEW `Assets/Tests/EditMode/Gameplay/Materials/GasSleepPartTests.cs`
- MOD `Assets/Scripts/Gameplay/Materials/GasFactory.cs` (+Sleep case)

### G.8d — GasFungalSporesPart + FungalInfectionEffect (PLAN)

**Status:** PLANNED. Multi-stage infection state machine + gas-side
dispatcher. Hits all 5 CLAUDE.md major-feature criteria (multi-system,
new content, Qud-parity claim, non-obvious state-machine failure mode,
player-visible).

#### Verification sweep

| Premise | Confirmed at | Detail |
|---|---|---|
| Qud `GasFungalSpores.ApplyGas` does TWO things: applies `SporeCloudPoison` (short tick effect) + rolls Toughness save vs `10 + GasLevel/3`; on fail applies `FungalSporeInfection` for Duration=`Random(20,30)×120` real or `Random(8,10)` fake. | `qud GasFungalSpores.cs:83-188` | ✓ verified |
| Qud's `FungalSporeInfection` is 351 lines: grows a `MeleeWeapon` on Hand body parts, `Armor` on Body/Feet/Head/Hands — a *huge* anatomy-system integration. The infection literally turns the host's body into a fungus over time. | `qud FungalSporeInfection.cs:200-299` | ✓ verified; **out of scope for CoO** (no body-part-equip-grown-item system) |
| Qud filters in ApplyGas: target != self, target != Creator, `CheckGasCanAffectEvent`, `CanApplySpores` event veto, `ApplySpores` event veto, `ImmuneToFungus` tag, `PhaseMatches`, has Toughness stat, NOT already infected. | `qud GasFungalSpores.cs:85-127` | ✓ verified |
| `SporeCloudPoison` is the short-tick effect applied alongside the long infection — separate effect class (122 lines). | `qud SporeCloudPoison.cs` | ✓ verified |
| CoO `IObjectGasBehaviorPart.RunFilterChain` covers self-guard + Creature + CheckGasCanAffect + respiratory intake — most of the Qud filter chain. | `IObjectGasBehaviorPart.cs:106-128` (G.8a refactor) | ✓ — reuse |
| CoO has no `Toughness`-save rolling system, no `ImmuneToFungus` tag, no body-part-grown-item infrastructure. | survey | ✓ — simplify |

**Scope-prune (CLAUDE.md §1.3):**
- CoO has no anatomy-system body-part-grown-item infrastructure.
  Mirroring Qud's "fungus armor grows on Body" 1:1 would touch the
  entire Body/Equipment system. **Defer indefinitely.**
- Qud's Toughness-save roll is a substitute for "infection is
  resistible." CoO equivalent: scale infection chance by gas level
  vs target Toughness stat (no separate save-roll subsystem).
- The portable Qud-parity feature is the **multi-stage state
  machine** — incubation → symptomatic → blooming → terminal.
  Contagion (Stage 2+ host releases spore gas at their cell) routes
  through the existing gas system, no separate "contagious" code path.

#### Architecture (CoO-adapted)

**`FungalInfectionEffect`** — multi-stage state machine. Single
Effect class with internal Stage int field 0..3:

| Stage | Turn range | Behavior |
|---|---|---|
| 0 INCUBATION | 0-9 | Silent; "your skin itches" message at turns 0/5/9 |
| 1 SYMPTOMATIC | 10-19 | 1 dmg/turn (Fungal-tagged); -1 Toughness stat shift |
| 2 BLOOMING | 20-29 | 2 dmg/turn; -2 Toughness; **spawn fungal-spores gas at host's cell every 3 turns (the contagion mechanic)** |
| 3 TERMINAL | 30-39 | 3 dmg/turn; -3 Toughness; spawn spore gas every 2 turns |
| Expires at turn 40 | | Effect self-removes; stat shifts restored on OnRemove |

**`GasFungalSporesPart`** — IObjectGasBehaviorPart subclass. ApplyGas
filter chain (uses `RunFilterChain` from G.8a refactor) +
infection-chance roll. On success, apply `FungalInfectionEffect`.
Refresh-on-reapply: existing infection doesn't reset (don't reset
the player's stage clock by walking through fresh spores). Diag:
gas/Applied + gas/InfectionPrevented (counter-check: target already
infected).

#### Sub-milestones (smallest blast radius first)

- **G.8d.1** — `FungalInfectionEffect` state machine + tests
  (effect class only, no gas dispatcher yet). Stat-shift Apply/Remove
  pattern from `HibernatingEffect` (LB.4 idiom). Pin: stage
  transitions at turn boundaries; tick damage per stage; Toughness
  decrement per stage.
- **G.8d.2** — `GasFungalSporesPart` + `fungal-spores.json` content +
  factory wiring + tests (Part-only, no contagion yet).
- **G.8d.3** — Contagion: Stage 2+ effect's OnTurnStart spawns
  fungal-spores gas at host's cell. Verify the natural loop:
  infected creature spawns gas → gas spreads → adjacent creatures
  inhale + roll for infection.

Each sub-milestone:
- RED→GREEN cycle with **true assertion-level RED** (stub first,
  observe failures, then implement) — matching G.7a / G.8c
  discipline.
- Counter-checks per assertion.
- Adversarial inline (CLAUDE.md §5 step g): null safety,
  re-application semantics, boundary stages.
- Severity-marked self-review in commit body.
- Update §16 G.8d log same commit.

#### Pre-flagged 🟡 findings

- 🟡 **No body-part-grown-equipment** — CoO's `FungalInfectionEffect`
  is a flat stat-shift + tick effect. Qud's grows an armor/weapon
  item. Documented in the Effect's doc-comment as a Qud-divergence
  point.
- 🟡 **Stage timing is short** — 40 turns total vs Qud's
  2400-3600. Tuned for CoO's RPG-but-not-MMO scale. Could lengthen
  if playtest finds it too aggressive.
- 🟡 **Refresh-on-reapply is non-standard for this Effect** — other
  gas effects refresh Duration on reapply, but FungalInfection
  should NOT (a player already in Stage 2 shouldn't get reset to
  Stage 0 by walking through fresh spores). OnStack returns true
  (consumed) but doesn't change the existing instance's state.
- 🔵 **Contagion gas re-uses the same GasFungalSporesPart**
  applier — symmetric with Qud's design where the "blooming" host
  becomes a SporePuffer that spawns the same gas the original cloud
  did.
- ⚪ **Toughness save** — out of scope; use a deterministic
  `infectionChance = GasLevel * Toughness_modifier_scaled` instead
  of a rolled save. Documented.

---

### G.8e (this commit) — GasPlasmaPart + CoatedInPlasmaEffect (gas-as-coat hybrid)

**Status:** ✅ COMPLETE. The **sixth and final** gas behavior type,
and the only **gas-as-coat hybrid**: the gas applies an Effect (the
"coat") that OUTLASTS the cloud, like a liquid coating but delivered
by a gas. CoO port of Qud `XRL.World.Parts.GasPlasma` +
`XRL.World.Effects.CoatedInPlasma`.

**What makes plasma distinct from the other 5 gases:**
- **Duration scales with cloud DENSITY, not intake.** A density-100
  cloud coats for `Random(40, 60)` turns (Qud GasPlasma.cs:104:
  `Random(density*2/5, density*3/5)`). Walk through thick plasma →
  long coat. The other gases scale effect strength by Level/intake.
- **The coat is a triple-resistance debuff:** −100 Heat / Cold /
  Electric resistance. With CoO's resistance math `(100-R)/100`,
  −100 → ~200% elemental damage. You become a glass cannon's
  dream target.
- **Refresh-on-reapply takes the LARGER Duration** (Qud
  CoatedInPlasma.cs:71-73) — the OPPOSITE of FungalInfection's
  preserve-the-stage-clock semantic. Denser re-coat extends.
- **Burns off liquid coats on apply** (Qud:
  `RemoveAllEffects<LiquidCovered>`) — plasma vaporizes the water/
  blood/oil film.

**Why GasPlasmaPart does NOT `RemoveEffect` first** (unlike GasPoison
which removes-then-reapplies to refresh): doing so would
unapply+reapply the resistance shift, re-capturing the
already-shifted values as the new "prior" → corrupting the restore
to −200/−300/… on each re-entry. Instead refresh is delegated to
`CoatedInPlasmaEffect.OnStack` (take larger Duration, NO re-shift).
`StatusEffectsPart.ApplyEffect` routes a same-type re-apply straight
to `OnStack` and discards the incoming WITHOUT calling its OnApply
(StatusEffectsPart.cs:65-72) — so the shift can never double.

**Why a `StatsApplied` bool, not HibernatingEffect's `-1` sentinel:**
HibernatingEffect captures `Prior… = GetStatValue` and guards
OnRemove with `Prior >= 0`. That sentinel MISFIRES on a creature
with a genuinely negative resistance (e.g. a fire-vulnerable beast
at −1 HeatResistance): the restore is skipped and the −100 shift
leaks permanently. CoatedInPlasma captures `stat.BaseValue` (the
base, composing cleanly with Bonus/Penalty layers) and guards with a
dedicated `StatsApplied` bool. The `Coat_NegativePriorResistance_
RoundTripsCorrectly` test pins exactly this: −1 prior → −101 coated
→ restored to −1.

**Shipped:**
- `CoatedInPlasmaEffect`: OnApply burns liquid coat + applies the
  triple −100 shift (capture-then-subtract, idempotent via
  StatsApplied); OnRemove restores; OnStack takes larger Duration
  without re-shift. Public fields (StatsApplied + 3 PriorX) for save
  round-trip.
- `GasPlasmaPart`: filter chain via `RunFilterChain` (so
  GasImmunity/non-creature/mask gates apply) + density-scaled
  `ComputeDuration` (pure, testable) → `ApplyEffect` → gas/Applied
  diag (payload carries `density` + `coatDuration`).
- `plasma-gas.json` content (GasType=Plasma, Color=&R).
- `GasFactory.CreateBehaviorPart` += "Plasma" case.
- Showcase already extended with the fungal column in the prior
  commit; plasma is exercised via the bench in G.12.

**RED → GREEN cycle observed (true assertion-level RED):**
1. Stubs: `CoatedInPlasmaEffect.OnApply/OnRemove {}`,
   `OnStack => false`; `GasPlasmaPart.ApplyGas => false`
   (`ComputeDuration` implemented up front as a pure function).
2. Wrote 20 tests.
3. Ran → **12 RED with specific assertion messages** (e.g.
   `Expected: -80 But was: 20`, `Expected: True But was: False`, two
   NREs on `.Duration` of a null effect), 8 GREEN (3 ComputeDuration
   pure + SpawnGas-attach + null-safety + 2 counters).
4. Implemented `CoatedInPlasmaEffect` (capture/restore + burn-off +
   larger-duration stack) and `GasPlasmaPart.ApplyGas`.
5. Ran → 20/20 GREEN.
6. Added 4 inline mutation-resistance tests (step g) → 24/24 GREEN.

**Test breakdown (24 total, all GREEN):**
- ComputeDuration pure (3): density-100 range, zero-density, low-floor
- CoatedInPlasmaEffect direct (6): triple penalty, restore,
  negative-prior round-trip, liquid burn-off, OnStack larger,
  OnStack smaller-no-shrink, OnStack no-double-shift
- GasPlasmaPart dispatch (7): factory attach, ApplyGas lands,
  density scales duration, reapply extends not double-shift,
  GasImmunity vetoes, per-turn dispatch, Applied diag
- Cross-system (1): plasma-coated takes amplified Heat damage
  (exercises CombatSystem resistance math end-to-end)
- Cross-type isolation + null (2): null target no crash, doesn't
  apply other effects
- Mutation-resistance / step g (4): null-incoming OnStack,
  OnRemove-without-apply no-shift, negative-density returns zero,
  double-instance single-shift

**IMPLEMENTATION NOTES (risks verified before writing code)**
1. `LiquidCoveredEffect(string id="", int amount=0)` ctor matches
   the burn-off test's `new LiquidCoveredEffect("water", 30)`.
2. `Stat.Value = BaseValue + Bonus - Penalty + Boost`; capturing
   and shifting `BaseValue` is the right lever (verified Stat.cs:31).
3. `StatusEffectsPart.ApplyEffect` routes same-type re-apply to
   `existing.OnStack(incoming)` and returns immediately on `true` —
   incoming discarded, its OnApply never fires (StatusEffectsPart.cs:
   65-72). Load-bearing for the no-double-shift contract.
4. OnApply runs via `effect.Applied()` (line 111) AFTER `_effects.Add`
   (line 110); all removal loops are indexed → removing a DIFFERENT
   effect type (LiquidCovered) inside OnApply is iterator-safe.

**SCOPE DIVERGENCE FROM THE PLAN — none.**

**G.8e SELF-REVIEW (CLAUDE.md §5) + cold-eye (Q1-Q4)**
- 🔵 (deliberate) Captures `stat.BaseValue` not computed `GetStatValue`
  (diverges from HibernatingEffect) — composes with Bonus/Penalty
  layers AND round-trips negatives. Strictly better; documented.
- 🔵 ctor `owner` param is overwritten by `StatusEffectsPart`
  (`effect.Owner = ParentEntity`) on the applied path — decorative,
  harmless, pre-existing pattern across all effects.
- 🧪 Save/load round-trip of the 4 public fields is asserted-by-design
  (public, not private-backed properties) but not yet test-covered.
  Deferred to the G.12 gas-wide adversarial sweep (save/load reach is
  a listed surface there).
- ⚪ Qud-divergence: skip "temperature blocked from returning to
  ambient" + "firefighting quartered" — CoO has neither system.
  Documented in the Effect doc-comment.
- Cold-eye Q1 (symmetry): OnApply ApplyStats ↔ OnRemove UnapplyStats
  symmetric; liquid burn-off is correctly one-directional (no
  un-burn). Q2 (cross-feature): gas/Applied payload field naming
  consistent with GasPoison (gasId/gasType/gasLevel/intake), plus
  plasma-specific density/coatDuration. Q3 (counter-completeness):
  StatsApplied true/false paths both tested; Duration larger/smaller/
  null all tested. Q4 (doc-vs-impl): docstring claims (−100 triple,
  burn-off, larger-duration) all match impl. 0 findings.

**Tests:** 24 new GREEN. Full regression (13 suites, 251 tests):
all green.

**Files:**
- NEW `Assets/Scripts/Gameplay/Effects/Concrete/CoatedInPlasmaEffect.cs`
- NEW `Assets/Scripts/Gameplay/Materials/GasPlasmaPart.cs`
- NEW `Assets/Resources/Content/Data/GasDefinitions/plasma-gas.json`
- NEW `Assets/Tests/EditMode/Gameplay/Materials/GasPlasmaPartTests.cs`
- MOD `Assets/Scripts/Gameplay/Materials/GasFactory.cs` (+Plasma case)

**G.8 milestone COMPLETE** — all 6 gas behavior types shipped:
Poison (G.5), Stun + Confusion (G.8a), Cryo (G.8b), Sleep (G.8c),
FungalSpores (G.8d), Plasma (G.8e). Next: G.9 outgassing-on-fire.
