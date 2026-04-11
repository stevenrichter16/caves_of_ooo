# Plan: Expand the Emergent Material System
 
## Context
 
The fire + organic material pipeline is verified end-to-end as fully functional (see Part 1 audit below). The foundation is solid: `ThermalPart`, `MaterialPart`, `FuelPart`, `WetEffect`, `BurningEffect`, `MaterialReactionResolver`, `MaterialSimSystem.TickMaterialEntities`, and data-driven reactions (`fire_plus_organic.json`) all work together, initialized from `GameBootstrap` and ticked from `InputHandler.EndTurnAndProcess`.
 
The goal now is to **expand emergence** in three layers:
 
1. **Foundation** — Add the cold / electricity / acid / vapor primitives that the existing `ThermalPart` and `MaterialPart` fields already hint at but don't yet use. Wire the currently-dead `Conductivity`, `Porosity`, `Volatility`, and `Brittleness` fields so they earn their place in the sim.
2. **Data** — Add new data-driven reactions and new reaction effect types so combinations (cold+metal, fire+food, oil+fire, water+fire, lightning+metal) become authorable in JSON rather than hard-coded.
3. **Content** — Ship two waves of grimoires on top of the new primitives:
   - **Combat grimoires** — high-cost, high-impact spells that exercise the new reactions.
   - **Everyday grimoires** — low-cost, short-cooldown utility spells the player reaches for between fights (light a torch, dry off, warm food, conjure water, cleanse rust).
 
The design principle throughout: **every new grimoire must connect to a shared material primitive**, so spells compose with each other and with the environment rather than existing in a silo. A cooking-fire everyday spell should still ignite a wooden barrel if miscast; a combat lightning spell should still arc along a puddle or a metal fence. That's the "emergent" promise.
 
---
 
## Part 1: Verification — Fire + Organic Is Fully Wired
 
| # | Flow | Status | Evidence |
|---|---|---|---|
| 1 | **Ignition**: Kindle/Conflagration → `ApplyHeat` event → `ThermalPart.HandleApplyHeat` → crossing `FlameTemperature` → `TryIgnite` → `WetEffect` suppression check → `MaterialPart` combustibility veto → `BurningEffect` applied | WORKING | `KindleMutation.cs:25-39`, `ThermalPart.cs:35-99`, `MaterialPart.cs` veto on `TryIgnite` |
| 2 | **Burn loop**: `BurningEffect.OnTurnStart` → `ConsumeFuel` event → `FuelPart` decrement → damage tier by intensity → self-heat `ApplyHeat` → `MaterialReactionResolver.EvaluateReactions` → `MaterialSimSystem.EmitHeatToAdjacent` → neighbors ignite → `FuelExhausted` → `CharredEffect` + `SmolderingEffect` on burnout | WORKING | `BurningEffect.cs:54-117`, `MaterialSimSystem.cs:87-123` |
| 3 | **Quench / extinguish**: `QuenchMutation` → `WetEffect` + negative `ApplyHeat` → `ThermalPart` cools → `TryExtinguish` removes `BurningEffect` → fires `Extinguished` event | WORKING | `QuenchMutation.cs:25-40`, `ThermalPart.cs:61-64`, `ThermalPart.cs:101-111` |
| 4 | **Data-driven reactions**: `fire_plus_organic.json` loaded at boot → matched against entity state each burn tick → `ModifyBurnIntensity` / `ModifyFuelConsumption` / `EmitBonusHeat` applied | WORKING | `GameBootstrap.cs:59-67`, `MaterialReactionResolver.cs:16-55`, `fire_plus_organic.json:1-19`, called from `BurningEffect.cs:112` |
| 5 | **Tick wiring**: Player turn ends → `TurnManager.ProcessUntilPlayerTurn` → `MaterialSimSystem.TickMaterialEntities` fires `BeginTakeAction+EndTurn` on burning non-creatures and `EndTurn` on wet/heated non-creatures | WORKING | `InputHandler.cs:449-456`, `MaterialSimSystem.cs:20-81` |
| 6 | **Test coverage**: `MaterialSystemTests` validates ignition, burning, heat propagation, chain ignition, quenching, wet suppression, and material reactions | WORKING | `Assets/Tests/EditMode/.../MaterialSystemTests.cs` |
 
**Verdict:** The pipeline is fully operational. No gaps, no broken links, no orphaned fields in the fire/wet/thermal path. Safe to build new primitives on top.
 
**Currently unused building blocks** (to be activated by this plan):
 
- `MaterialPart.Conductivity`, `MaterialPart.Porosity`, `MaterialPart.Volatility`, `MaterialPart.Brittleness` — declared in blueprint schema but no code reads them.
- `ThermalPart.FreezeTemperature`, `ThermalPart.VaporTemperature`, `ThermalPart.BrittleTemperature` — declared with sensible defaults but no crossing events fire.
- `MaterialReactionResolver.SpawnParticle` — stub case exists but does nothing.
- `MaterialTags` beyond `Organic` / `Flammable` — no blueprints tagged `Metal`, `Stone`, `Cloth`, `Liquid`, `Ice`, `Oil`, `Food`, `Glass`.

---

## Part 2: Foundation Expansion
 
### 2A. New effects (4)
 
Each mirrors the `WetEffect` / `BurningEffect` pattern exactly: `OnApply` / `OnRemove` / `OnTurnStart` / `OnTurnEnd` / `OnStack`, an intensity or moisture field, and render color override. All live under `Assets/Scripts/Gameplay/Effects/Concrete/`.
 
1. **`FrozenEffect.cs`**
   - Field: `Cold` (0..1) — how deeply frozen.
   - `AllowAction` returns `false` when `Cold > 0.5` → like `StunnedEffect`, target cannot act.
   - `OnTurnEnd` thaws based on `ThermalPart.Temperature` (inverse of `WetEffect`'s evaporation). Above `0°C` lose `(temp / 100) * 0.02` per turn; below, hold.
   - Interaction: on apply, if the entity has a `BurningEffect`, remove it and fire `Extinguished`. Cold defeats fire, symmetric to how fire defeats wet.
   - Interaction: on apply, if `MaterialPart.Brittleness > 0.5` AND `ThermalPart.Temperature < BrittleTemperature`, fire `TryShatter` event (see 2C). Glass, ice, brittle metal crack under freeze shock.
 
2. **`ElectrifiedEffect.cs`**
   - Field: `Charge` (float joules-equivalent).
   - Duration default: 2 turns.
   - `OnApply` fires `ApplyStun` for 1 turn on creatures, and fires `TryChainElectricity` event (see 2C) on `OnTurnStart` so propagation happens during the sim tick, not at application.
   - Interaction: if target is wet (`WetEffect.Moisture > 0.2`), double `Charge` and extend duration — water amplifies electricity.
   - Interaction: if `MaterialPart` has `Metal` tag or `Conductivity > 0.5`, propagate via `TryChainElectricity`.
 
3. **`AcidicEffect.cs`**
   - Field: `Corrosion` (0..1).
   - `OnTurnStart` deals flat damage to `Organic`-tagged entities (1 + floor(Corrosion * 4)) and decrements their `MaterialPart.Combustibility` by `0.05 * Corrosion` (wet/soaked organic material loses its ability to burn — a neat cross-reaction with Quench).
   - `OnTurnEnd` degrades (`Corrosion -= 0.05`), removed when `Corrosion <= 0`.
   - Interaction: on apply to `Metal`-tagged targets, also applies `RustedEffect` queued for a later phase if we want (skippable).
 
4. **`SteamEffect.cs`**
   - Field: `Density` (0..1).
   - `OnTurnEnd` dissipates (`Density -= 0.1`), removed at 0.
   - Sits on cells/entities; primary role is as a *reaction product* spawned by `water_plus_fire` — applied via new `SpawnEntity` reaction effect, where the spawned entity is a short-lived vapor cloud carrying `SteamEffect`.
   - Interaction: cools `ThermalPart` on adjacent entities by a small amount each turn (calls `EmitHeatToAdjacent` with negative joules). Also lightly wets entities it touches (low-moisture `WetEffect`).
 
### 2B. Wire the dead `MaterialPart` fields
 
Each currently-unused field gets at least one consumer so it stops being dead weight:
 
| Field | New consumer | Behavior |
|---|---|---|
| `Conductivity` | `ElectrifiedEffect` chain | When `TryChainElectricity` fires, scan 8 neighbors; propagate to any target with `Conductivity > 0.5` or `Metal` tag. Intensity scales with source's `Conductivity`. |
| `Porosity` | `WetEffect` absorption | On `WetEffect` apply, scale incoming moisture by `(1 + Porosity)`. High-porosity (cloth, sponge) soaks more. On `OnTurnEnd`, scale evaporation by `(1 - Porosity * 0.5)` — porous things stay wet longer. |
| `Volatility` | `ThermalPart.TryIgnite` | Before the `FlameTemperature` check, subtract `Volatility * 100` from the effective threshold. Oil and alcohol ignite at much lower temperatures. Also increase `BurningEffect.Intensity` starting value by `Volatility`. |
| `Brittleness` | `TryShatter` event (new) | When `ThermalPart` detects a rapid temperature delta (> 200° in one `ApplyHeat` call) OR a freeze crossing, and `Brittleness > 0.5`, fire `TryShatter`. `MaterialPart` handles it: decrement HP by a percentage, or destroy the entity entirely at high brittleness. |
 
### 2C. `ThermalPart` crossing events
 
`ThermalPart.HandleApplyHeat` currently checks for the `FlameTemperature` crossing (ignition) and the reverse crossing (extinguish). Add three more:
 
- **Freeze crossing**: `wasAbove = Temperature > FreezeTemperature; ...; if (wasAbove && Temperature <= FreezeTemperature) TryFreeze();`
  - `TryFreeze` fires a `TryFreeze` event (like `TryIgnite`), then applies `FrozenEffect(cold: 1.0f)` if unvetoed.
  - Used by IceLance / Chill Draft.
 
- **Vapor crossing**: `wasBelow = Temperature < VaporTemperature; ...; if (wasBelow && Temperature >= VaporTemperature) TryVaporize();`
  - `TryVaporize` removes any `WetEffect` entirely and spawns a steam cloud entity at the source cell. If the entity *is* a liquid (Liquid tag), destroy it and spawn steam.
  - Used by water_plus_fire reaction.
 
- **Brittle shock**: inside `HandleApplyHeat`, track `float delta = joules / HeatCapacity;` and if `Math.Abs(delta) > 200f` fire `TryShatter` (regardless of absolute temperature).
  - `MaterialPart` handles `TryShatter` with a `Brittleness > 0.5` guard.
 
### 2D. New `MaterialReactionResolver` effect types
 
Add four new cases to the `ApplyEffects` switch in `MaterialReactionResolver.cs:111-138`. Each becomes an authorable JSON effect type:
 
| Type | Params | Behavior |
|---|---|---|
| `ApplyStatusEffect` | `StringValue` = effect class name, `FloatValue` = intensity/duration | Reflection-based lookup (similar to `MutationsPart.CreateMutationByName`), construct effect, call `entity.ApplyEffect`. Lets JSON grant `FrozenEffect`, `ElectrifiedEffect`, etc. without new C#. |
| `SwapBlueprint` | `StringValue` = new blueprint name | Destroys the current entity and spawns `StringValue` in its cell. Drives raw→cooked food, ice→water, wood→ash. |
| `PropagateAlongTag` | `StringValue` = material tag, `FloatValue` = chain joules | Generalized chain propagation. Iterate 8 neighbors, apply `ApplyHeat` (or the relevant event) to any entity whose `MaterialPart` has the given tag. Used by lightning_plus_conductor. |
| `SpawnEntity` | `StringValue` = blueprint name, `FloatValue` = radius | Spawns a short-lived entity (steam cloud, ash pile, scorch mark) in the source cell or within `radius` cells. Uses existing `EntityFactory` / blueprint system. |
 
The existing `SpawnParticle` stub can also be filled in (non-blocking visual FX call).
 
---
 
 ## Part 4: New Grimoires
 
Twelve new grimoires total: six combat, six everyday. Each maps to one primitive from Part 2 so they're easy to test in isolation and they compose with each other naturally.
 
All combat grimoires follow the existing Kindle/Quench/Conflagration pattern: new mutation class under `Assets/Scripts/Gameplay/Mutations/`, new blueprint in `Objects.json` (inheriting the grimoire shape), new entry in `Mutations.json`, `ExcludeFromPool: true`. Everyday grimoires follow the same pattern but with shorter cooldowns, lower damage dice, and often non-damaging effects.
 
### 4A. Combat Grimoires (6)
 
| # | Name | Targeting | Cooldown | Primary interaction | Notes |
|---|---|---|---|---|---|
| C1 | **Ice Lance** | DirectionLine, range 6 | 8 | `ApplyHeat` −300J direct + `FrozenEffect(cold:0.8)` | Crosses `FreezeTemperature`, fires `TryFreeze`. On Metal+Brittle target, triggers `TryShatter` via the shock delta check. Counterpart to Kindle. |
| C2 | **Arc Bolt** | DirectionLine, range 5 | 7 | Applies `ElectrifiedEffect(charge:1.0)` + 2d4 damage | `lightning_plus_conductor.json` fires on next tick → propagates to adjacent metal via `PropagateAlongTag`. Wet creatures take double charge. |
| C3 | **Acid Spray** | DirectionLine, range 4 | 10 | Applies `AcidicEffect(corrosion:0.8)` + 1d4 damage | `acid_plus_organic.json` handles the per-turn decay. Degrades `Combustibility` on wet organic targets — good for stripping enemy shields. |
| C4 | **Thunderclap** | SelfCentered, radius 2 | 18 | All creatures in radius take 2d6, all `Metal`+`Conductor` entities get `ElectrifiedEffect` | Follows `ConflagrationMutation` structure exactly but substitutes the electricity primitive. Extra damage to wet targets. |
| C5 | **Rime Nova** | SelfCentered, radius 2 | 15 | All creatures in radius take 1d6 cold damage + `FrozenEffect(cold:0.6)` | Cold AoE mirror of Conflagration. Extinguishes any burning entities in radius (free firefighting fallback). |
| C6 | **Ember Vein** | DirectionLine, range 7 | 12 | Beam that applies `ApplyHeat` +150J *per cell traversed* (not just the impact cell) | Uses `SpellTargeting.TraceBeam` instead of `LineTargeting.TraceFirstImpact`. Beams through a row of wooden crates ignite every one. Showcases heat propagation. |
 
### 4B. Everyday Grimoires (6)
 
Philosophy: these should feel like *appliances*. Low damage or none, short cooldown, narrow-purpose, obviously useful between fights. Each one has a clean "I want to do X" pitch.
 
| # | Name | Targeting | Cooldown | Pitch | Implementation |
|---|---|---|---|---|---|
| E1 | **Kindle Flame** | AdjacentCell, range 1 | 2 | *"Light this torch / campfire / wick."* | Fires `ApplyHeat` +50J direct on the target cell's contents, but only if the target's `FlameTemperature < 250` (torches/wicks only). Zero damage. Skips creatures entirely. Good for lighting a path without burning down the inn. |
| E2 | **Drying Breeze** | SelfCentered, radius 1 | 3 | *"Dry yourself off after a bath / quench mishap."* | Removes `WetEffect` from self and all adjacent entities. No heat applied. Useful after wading through water or after Quench friendly-fire. |
| E3 | **Hearthwarm** | AdjacentCell, range 1 | 4 | *"Cook this raw meat without burning it."* | Applies sustained low-grade heat (`ApplyHeat` +80J radiant) to the target cell for 3 turns via a persistent `HearthAuraEffect` on the caster. Drives `fire_plus_raw_meat.json` without crossing `FlameTemperature` on the meat itself. |
| E4 | **Conjure Water** | AdjacentCell, range 2 | 4 | *"Make a puddle appear to feed a well / fight a fire / quench thirst."* | Spawns `WaterPuddle` at target cell. If the target cell contains a burning entity, the puddle immediately triggers `water_plus_fire.json`. Also applies `WetEffect(moisture:0.5)` to any entity already in the cell. |
| E5 | **Chill Draft** | SelfCentered, radius 1 | 5 | *"Preserve food, cool a fever, freeze a lock."* | Applies `ApplyHeat` −100J to self and adjacent entities. Low enough to cool without freezing creatures solid (unless they're already cold). Cracks brittle locks (Metal+Brittle) via shock delta. Useful non-combat: extends food freshness if we add a Freshness timer later. |
| E6 | **Ward Gleam** | Self, targets held item | 15 | *"Clean rust / acid / scorch from a piece of gear."* | Removes `AcidicEffect`, `CharredEffect`, and `RustedEffect` (if added) from the currently-equipped primary weapon and armor slots. No combat use; pure maintenance. |
 
### 4C. Grimoire blueprints + mutation registry entries
 
For each of the 12, add:
 
1. **Mutation class** under `Assets/Scripts/Gameplay/Mutations/`:
   - `IceLanceMutation.cs`, `ArcBoltMutation.cs`, `AcidSprayMutation.cs`, `ThunderclapMutation.cs`, `RimeNovaMutation.cs`, `EmberVeinMutation.cs`
   - `KindleFlameMutation.cs`, `DryingBreezeMutation.cs`, `HearthwarmMutation.cs`, `ConjureWaterMutation.cs`, `ChillDraftMutation.cs`, `WardGleamMutation.cs`
   - Combat ones extend `DirectionalProjectileMutationBase` (C1–C3, C6) or `BaseMutation` directly (C4, C5, all everyday spells with AdjacentCell/Self targeting).
2. **Grimoire blueprint** in `Assets/Resources/Content/Blueprints/Objects.json`, same shape as existing `KindleGrimoire` / `QuenchGrimoire` / `ConflagrationGrimoire`. Combat grimoires get Commerce values of 100–250, everyday grimoires 30–60 (they're commodity items).
3. **Mutation registry entry** in `Assets/Resources/Content/Blueprints/Mutations.json`, `ExcludeFromPool: true`. Combat ones `Cost: 3-5`, everyday ones `Cost: 1-2`.
4. **Loot placement** — add all 12 grimoires to the village grimoire chest so they're testable from spawn. Everyday grimoires should eventually be common-ish drops in village shops; combat grimoires stay rare / in caches.
 
### 4D. New persistent effect: `HearthAuraEffect`
 
`HearthwarmMutation` needs a 3-turn aura that emits heat to a specific cell each turn. The cleanest way is a new `Effect` subclass on the *caster*:
 
- Holds `Cell TargetCell` (or target X/Y).
- `OnTurnStart` fires `ApplyHeat` +80J radiant on all entities in the target cell. Duration: 3.
- `OnRemove` silently ends.
 
This is the first grimoire where the spell persists across turns rather than resolving in one cast — a useful pattern for future channeled/sustained spells. No infrastructure changes needed; `StatusEffectsPart` already ticks caster effects every turn.
 
---
 
 ## Part 5: Implementation Phasing
 
This is a large body of work. Each phase lands independently, runs tests, and commits before the next begins. Stop after any phase if the scope feels right.
 
### Phase A — Foundation primitives (Part 2)
Files: `FrozenEffect.cs`, `ElectrifiedEffect.cs`, `AcidicEffect.cs`, `SteamEffect.cs` (new); `ThermalPart.cs`, `MaterialPart.cs`, `MaterialReactionResolver.cs`, `MaterialReactionBlueprint.cs` (modify).
- Add the four new effects.
- Wire `Conductivity`/`Porosity`/`Volatility`/`Brittleness` consumers.
- Add `TryFreeze`/`TryVaporize`/`TryShatter` crossings to `ThermalPart`.
- Add `ApplyStatusEffect`/`SwapBlueprint`/`PropagateAlongTag`/`SpawnEntity`/`DealDamage` effect types to `MaterialReactionResolver`.
- Extend `ReactionConditions` schema with `MaxTemperature`/`MinBrittleness`/`MinConductivity`/`MinVolatility`/expanded `SourceState` values.
- **Tests**: new `FrozenEffectTests.cs`, `ElectrifiedEffectTests.cs`, `AcidicEffectTests.cs`, `MaterialPartFieldTests.cs`, `ThermalCrossingTests.cs`. All existing `MaterialSystemTests` must still pass.
- **Commit**: "Add frost/electricity/acid/vapor primitives to material sim."
 
### Phase B — Data layer (Part 3)
Files: 6 new JSON reaction files; `Objects.json` tagging pass; 3 new reaction-product blueprints (`SteamCloud`, `AshPile`, `WaterPuddle`).
- Tag metal weapons/armor, food, stone, existing liquids.
- Author `water_plus_fire.json`, `oil_plus_fire.json`, `cold_plus_metal.json`, `lightning_plus_conductor.json`, `acid_plus_organic.json`, `fire_plus_raw_meat.json`, `fire_plus_raw_starapple.json`.
- Add `SteamCloud`/`AshPile`/`WaterPuddle` blueprints.
- Verify `GameBootstrap` loads all reaction files (may need to scan the directory instead of hardcoding `fire_plus_organic.json` — check how `Initialize` is currently called; switch to a `Resources.LoadAll<TextAsset>` pattern if needed).
- **Tests**: `MaterialReactionResolverTests.cs` loads each JSON and asserts conditions match expected entity states.
- **Commit**: "Add six new material reactions and supporting tagged blueprints."
 
### Phase C — Combat grimoires (Part 4A)
Files: 6 new mutation classes; 6 new blueprint entries in `Objects.json`; 6 new entries in `Mutations.json`; loot placement updates.
- Implement Ice Lance, Arc Bolt, Acid Spray, Thunderclap, Rime Nova, Ember Vein.
- Add grimoires to village chest.
- **Tests**: per-spell tests following the `KindleTests` / `QuenchTests` / `ConflagrationTests` pattern. Focus on observable material interactions: "Ice Lance on metal barrel → Frozen + TryShatter fires", "Arc Bolt on metal weapon → ElectrifiedEffect chains to adjacent metal", etc.
- **Commit**: "Add six combat grimoires exercising frost/electricity/acid primitives."
 
### Phase D — Everyday grimoires (Part 4B)
Files: 6 new mutation classes including `HearthAuraEffect.cs`; 6 new blueprint entries; 6 new registry entries; loot placement.
- Implement Kindle Flame, Drying Breeze, Hearthwarm, Conjure Water, Chill Draft, Ward Gleam.
- Add `HearthAuraEffect` persistent caster aura.
- Add everyday grimoires to village chest (and eventually to shop stock).
- **Tests**: "Hearthwarm cooks raw meat via SwapBlueprint without crossing FlameTemperature", "Conjure Water + burning barrel → extinguished via water_plus_fire reaction", "Drying Breeze removes WetEffect in radius", "Ward Gleam cleans AcidicEffect from equipped sword".
- **Commit**: "Add six everyday grimoires for out-of-combat utility."
 
### Phase E — Integration playtest
- Add a dedicated "material sandbox" plot near spawn: a row of raw meat on a plate, a wooden barrel, a metal chain, a water puddle, a torch. Let the player cast the full spellbook at it.
- Verify chain reactions work as expected (drop oil, light oil, watch it spread along a line of raw food, cooking all of it).
- Adjust constants if any reaction feels too weak or too strong.
- **No code commit** unless playtesting surfaces a bug or a tuning issue.
 
---
 
## Verification
 
### Automated
1. **Existing tests pass unchanged**: `MaterialSystemTests`, `StatusEffectTests`, `GrimoireSpellTests`.
2. **New primitive tests pass** (Phase A): `FrozenEffectTests`, `ElectrifiedEffectTests`, `AcidicEffectTests`, `ThermalCrossingTests`, `MaterialPartFieldTests`.
3. **Reaction JSON loads and matches** (Phase B): `MaterialReactionResolverTests` constructs each of the 6+ new JSONs and asserts reactions fire under expected conditions.
4. **Combat grimoire tests pass** (Phase C): one test file per mutation, asserting material-system observables (damage + effect application + chain behavior).
5. **Everyday grimoire tests pass** (Phase D): one test file per mutation, asserting non-combat observables (no damage to non-targets, effect removal, blueprint swap, puddle spawn).
 
### Manual in-game checklist
After all phases land, walk through this sandbox sequence:
 
1. Read all 12 new grimoires from the village chest. All 12 abilities appear in slots.
2. Cast **Kindle Flame** on an unlit torch → torch lights, no damage to adjacent entities.
3. Cast **Hearthwarm** on raw meat → after 3 turns, raw meat swaps to cooked meat.
4. Cast **Conjure Water** next to a burning barrel → water puddle spawns, barrel extinguishes, steam cloud appears.
5. Cast **Drying Breeze** on self after stepping in the puddle → `WetEffect` removed.
6. Cast **Ice Lance** on a metal weapon → Frozen + shatter message (or HP loss on the weapon).
7. Cast **Arc Bolt** on a metal-weapon-carrying creature standing next to another metal-weapon-carrier → electricity chains, both get `ElectrifiedEffect`.
8. Cast **Acid Spray** on a wooden barrel → barrel rots (Combustibility drops), subsequent Kindle fails to ignite it.
9. Cast **Rime Nova** on a burning crate row → all crates extinguished, creatures in radius Frozen.
10. Cast **Ember Vein** through a 4-crate row → all 4 crates ignite from the single beam.
11. Cast **Thunderclap** in a room with metal armor stands → all armor stands electrified.
12. Cast **Ward Gleam** with an acidic sword equipped → `AcidicEffect` cleared from the weapon.
13. Cast **Chill Draft** on a brittle lock (or brittle-tagged entity) → shatter fires.
 
### Regression sanity
- Wooden barrel + Kindle + Quench interaction still works identically to pre-expansion (Phase 1 audit flows must remain green).
- Existing `ConflagrationMutation` chain-ignition in the 5 barrel layouts near spawn still works identically.
- Grimoire chest still contains the original 3 combat grimoires from the previous wave.
 
---
 
## File Summary
 
| File | Action | Phase |
|---|---|---|
| `Assets/Scripts/Gameplay/Effects/Concrete/FrozenEffect.cs` | New | A |
| `Assets/Scripts/Gameplay/Effects/Concrete/ElectrifiedEffect.cs` | New | A |
| `Assets/Scripts/Gameplay/Effects/Concrete/AcidicEffect.cs` | New | A |
| `Assets/Scripts/Gameplay/Effects/Concrete/SteamEffect.cs` | New | A |
| `Assets/Scripts/Gameplay/Effects/Concrete/HearthAuraEffect.cs` | New | D |
| `Assets/Scripts/Gameplay/Materials/ThermalPart.cs` | Modify (crossings) | A |
| `Assets/Scripts/Gameplay/Materials/MaterialPart.cs` | Modify (field consumers, shatter) | A |
| `Assets/Scripts/Gameplay/Materials/MaterialReactionResolver.cs` | Modify (new effect types, new source states) | A |
| `Assets/Scripts/Gameplay/Materials/MaterialReactionBlueprint.cs` | Modify (schema) | A |
| `Assets/Resources/Content/Data/MaterialReactions/water_plus_fire.json` | New | B |
| `Assets/Resources/Content/Data/MaterialReactions/oil_plus_fire.json` | New | B |
| `Assets/Resources/Content/Data/MaterialReactions/cold_plus_metal.json` | New | B |
| `Assets/Resources/Content/Data/MaterialReactions/lightning_plus_conductor.json` | New | B |
| `Assets/Resources/Content/Data/MaterialReactions/acid_plus_organic.json` | New | B |
| `Assets/Resources/Content/Data/MaterialReactions/fire_plus_raw_meat.json` | New | B |
| `Assets/Resources/Content/Data/MaterialReactions/fire_plus_raw_starapple.json` | New | B |
| `Assets/Resources/Content/Blueprints/Objects.json` | Modify (tagging + 3 product blueprints + 12 grimoires) | B/C/D |
| `Assets/Resources/Content/Blueprints/Mutations.json` | Modify (12 new entries) | C/D |
| `Assets/Scripts/Gameplay/Mutations/IceLanceMutation.cs` | New | C |
| `Assets/Scripts/Gameplay/Mutations/ArcBoltMutation.cs` | New | C |
| `Assets/Scripts/Gameplay/Mutations/AcidSprayMutation.cs` | New | C |
| `Assets/Scripts/Gameplay/Mutations/ThunderclapMutation.cs` | New | C |
| `Assets/Scripts/Gameplay/Mutations/RimeNovaMutation.cs` | New | C |
| `Assets/Scripts/Gameplay/Mutations/EmberVeinMutation.cs` | New | C |
| `Assets/Scripts/Gameplay/Mutations/KindleFlameMutation.cs` | New | D |
| `Assets/Scripts/Gameplay/Mutations/DryingBreezeMutation.cs` | New | D |
| `Assets/Scripts/Gameplay/Mutations/HearthwarmMutation.cs` | New | D |
| `Assets/Scripts/Gameplay/Mutations/ConjureWaterMutation.cs` | New | D |
| `Assets/Scripts/Gameplay/Mutations/ChillDraftMutation.cs` | New | D |
| `Assets/Scripts/Gameplay/Mutations/WardGleamMutation.cs` | New | D |
| `Assets/Scripts/Gameplay/World/Generation/Builders/VillagePopulationBuilder.cs` | Modify (chest loot) | C/D |
| `Assets/Tests/EditMode/Gameplay/...` | New (one file per new effect, per new mutation) | A/C/D |
 

---