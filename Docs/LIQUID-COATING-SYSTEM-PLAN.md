# Liquid Coating System — Qud-Parity Migration Plan

**Branch:** `feat/liquid-coating-system`
**Date:** 2026-05-15
**Status:** ✅ COMPLETE — LQ.1–LQ.7 SHIPPED & merged to main. Two
critical self-reviews at the bottom (§A, §B) per the brief; per-phase
implementation log + cold-eye review at §11.

> **Genre framing:** CoO is an **RPG, not a roguelike**
> (`Docs/PROJECT-IDENTITY.md`). Liquid coatings are a moment-to-moment
> tactical layer that persists through save/load, not a run-scoped
> gimmick.

---

## 0. The question, answered

> *"Do pools of liquid on the ground transfer that liquid onto the
> player/NPCs, and does being coated change how other elements affect
> the player (e.g. wet → electric amplified)?"*

**Today, in Caves of Ooo:**

| Capability | Status | Evidence |
|---|---|---|
| (a) Liquid on the ground | **YES, partial** | `WaterPuddle` entity carries `MaterialPart{Water, tags=Liquid,Water}`, placed by `RiverBuilder.cs:103`. No volume/depth model, no `Cell` liquid field (`Cell.cs:10-49`), water-only. |
| (b) Transfer to entities on contact | **NO** | `MovementSystem` fires `AfterMove`/`EntityEnteredCell` (`MovementSystem.cs:142-240`) but **zero** listeners coat the mover. `WaterPuddle` has no step trigger. Standing in water applies no `WetEffect`. |
| (c) Coating modifies damage/resistance | **PARTIAL** | `CombatSystem.ApplyResistances` (`CombatSystem.cs:980-986`) is stat-only. The **single** coupling is `ElectrifiedEffect.OnApply` doubling `Charge` when `WetEffect.Moisture>0.2` (`ElectrifiedEffect.cs:29-34`). Not general; no oil/acid/cold coupling; no `MaterialPart`-based damage modifier. |

So: the canonical "step in water → wet → electric amplified" chain is
**half-built**. Wet *does* amplify electric (one hard-coded special
case), but **nothing makes you wet by stepping in a puddle**, and the
amplification isn't a reusable system. This plan closes (b) and
generalizes (c) to Qud parity.

---

## 1. Reference — how Qud actually does it (verification sweep)

Sourced from `/Users/steven/qud-decompiled-project/`. Every row is a
load-bearing premise this plan depends on; cited so any claim is
verifiable.

| # | Premise | Qud source |
|---|---|---|
| 1 | A puddle IS a GameObject carrying a `LiquidVolume` part; `MaxVolume == -1` ⇒ "open volume" (ground pool). | `LiquidVolume.cs:25, 65, 4529` |
| 2 | `LiquidVolume.ComponentLiquids` is `Dictionary<string,int>` in parts-per-1000; `Volume` in drams; depth thresholds `WADE=200`, `SWIM=2000`. | `LiquidVolume.cs:60-89, 40-42, 4534-4567` |
| 3 | Liquids are flyweight `BaseLiquid` subclasses, one shared instance, auto-registered by reflecting `[IsLiquid]` types in `LiquidVolume.Init()`. | `BaseLiquid.cs:16`; `LiquidVolume.cs:1112-1132`; `IsLiquid.cs` |
| 4 | Per-liquid data knobs: `PureElectricalConductivity`/`MixedElectricalConductivity`, `Combustibility`, `FlameTemperature`, `FreezeTemperature`, `Temperature`, `Fluidity`(→`Viscosity`), `Adsorbence`, `Evaporativity`, `Staining`/`Cleansing`, `SlipperyWhenWet`/`StickyWhenWet`, `FreezeObject1/2/3`. | `BaseLiquid.cs:22-128, 143` |
| 5 | Water: `Pure=0`, **`Mixed=100`**, `Combustibility=-50`. Oil: `Combustibility=90`, `FlameTemperature=250`. Acid overrides `SmearOn/SmearOnTick`→`ApplyAcid`. Honey: `StickyWhenWet=true` (the shipped "slows you" liquid). | `LiquidWater.cs:21-38`; `LiquidOil.cs:20-39`; `LiquidAcid.cs:181-198`; `LiquidHoney.cs:13-39` |
| 6 | **Transfer:** puddle's `LiquidVolume` handles `ObjectEnteredCellEvent` → `ProcessContact` → exposure cap = `Strength+Toughness+bodyparts` → wade/swim branch → `LiquidInContact` → `obj.ApplyEffect(new LiquidCovered(splitVolume))`. | `LiquidVolume.cs:2559, 2580, 2184, 2224, 2238/2328, 2160-2181` |
| 7 | The coat is the `LiquidCovered` *effect* holding a split-off `LiquidVolume`; re-coat **merges (`MixWith`), non-stacking**; per-turn `ProcessDynamics` partitions into drip(`Fluidity`)/evaporate(`Evaporativity`)/stain(`Staining`→`LiquidStained`)/cleanse; removes self at `Volume<=0`. | `LiquidCovered.cs:32, 140-169, 210-342, 439-446` |
| 8 | **Electric+wet:** `LiquidCovered.HandleEvent(GetElectricalConductivityEvent)` raises the creature's conductivity to the coat's (`MinValue`, pass 3). `Physics.ApplyDischarge` picks chain targets by highest conductivity & recurses; damage degrades ×4/5 per hop. Amplification = chain-targeting + propagation, not a flat ×. | `LiquidCovered.cs:381-388`; `LiquidVolume.cs:5094-5120`; `Physics.cs:1795,1807,1978,2038-2056` |
| 9 | **Fire+oil:** `ProcessExposure` thermal transfer; oil coat (`Combustibility≥50`) on a burning creature adds temperature; oil-soaked items get `FlameTemperature`←250 (ignite near flame). | `LiquidVolume.cs:2677, 2702-2722, 5138`; `Physics.cs:626, 4138` |
| 10 | **Fire+wet:** water `Combustibility=-50` reduces heat gain + boosts cooling in `ProcessExposure` — damps/extinguishes (thermal tug-of-war, not a hard block). | `LiquidVolume.cs:2719-2722, 5264-5278` |
| 11 | **Acid coat:** `LiquidAcid.SmearOn/SmearOnTick`→`ApplyAcid` deals scaling `"Acid"` `TakeDamage` **every turn the coat persists**; `ConsiderDangerousToContact` flags AI/trade. | `LiquidAcid.cs:181-198, 33` |
| 12 | **Cold+wet:** open `LiquidVolume` handles `FrozeEvent`; liquids with `FreezeObject*` die→solid (lava→boulder); water has none → `SlipperyWhenFrozen` ice. | `LiquidVolume.cs:3865-3898`; `LiquidWater.cs:32`; `BaseLiquid.cs:402-423` |
| 13 | Coating mediates element interaction by answering **query events** (`GetElectricalConductivityEvent`, `GetMaximumLiquidExposureEvent`) + per-liquid `SmearOn*`/`ObjectEnteredCell` overrides — *not* a cell check and *not* a `TakeDamage` hook on the damage itself. | `LiquidCovered.cs:381,439`; `GetElectricalConductivityEvent.cs`; `BaseLiquid.cs:402` |
| 14 | Expandability seam: a new liquid is one `[IsLiquid] : BaseLiquid` class with data + optional `SmearOn*`/`ObjectEnteredCell`/`Froze` overrides; auto-discovered. No table edit. | `LiquidHoney.cs` end-to-end; `LiquidVolume.cs:1112-1132` |

**No false premises detected.** One deliberate divergence is recorded
in §4 (CoO uses `GameEvent` string-keyed dispatch + the `Effect`
damage hooks, not Qud's `MinEvent` 3-pass query bus + discharge-arc
engine — see Architecture §4).

---

## 2. What CoO already has (the substrate we build on)

| CoO asset | Shape | Reuse for |
|---|---|---|
| `MaterialPart` (`MaterialPart.cs:10`) | `MaterialID` + scalar `Combustibility/Conductivity/...` + `MaterialTags` | Tag the puddle entity; not the coat model |
| `MaterialReactionResolver` (`MaterialReactionResolver.cs:24`) | JSON-driven, entity-vs-self-state (SourceState×TargetTag→effects) | The JSON-loading + reaction-output pattern to mirror for `LiquidDefinition` loading |
| `WetEffect` (`WetEffect.cs:8`) | `Moisture` 0–1, evaporates, suppresses ignition >0.35 | Keep as-is in phase 1; water-coat refreshes it so pinned tests stay green |
| `ElectrifiedEffect` (`ElectrifiedEffect.cs:29-34`) | `Charge`; ×2 + Duration+1 if `WetEffect.Moisture>0.2` | The existing wet→electric coupling; generalized in LQ.5 |
| `AcidicEffect`, `BurningEffect`, `FrozenEffect`, `SteamEffect`, `CharredEffect`, `SmolderingEffect` | per-turn effects | Follow-on effects liquids apply |
| `Effect` base `OnBeforeTakeDamage`/`OnTakeDamage` (`Effect.cs:193-209`, routed `StatusEffectsPart.cs:474-495`) | only `StoneskinEffect` uses it today | **The CoO-native damage hook seam** for coating-modifies-damage |
| `EquipBonusUtility` apply/remove + `equipment/StatBonus*` diag (shipped) | symmetric stat-bonus apply/reverse with diag | The exact pattern for stat/resistance liquids |
| `MovementSystem` `EntityEnteredCell` on cell occupants (`MovementSystem.cs:202-240`) | fires on non-mover occupants when something enters | **The transfer trigger** — the puddle is an occupant |
| `MaterialReactions/*.json` loader convention | `Resources/Content/Data/...` reflectionless JSON | The `LiquidDefinitions/*.json` loader |
| `Diag` + `diag_query` (we shipped 14 categories) | observability substrate | New `liquid` diag category |
| `StatusTonicPart` + `ThrowItemCommand.ApplyTonicAoe` (`ThrowItemCommand.cs:395`) | thrown tonic → 3×3 direct effect, no puddle | LQ.5 stretch: thrown liquid leaves a pool |

---

## 3. The 3 recommended stat/resistance liquids (+ the foundation)

The **foundation** is the `LiquidCovered` substrate (LQ.2–LQ.4). The
canonical Qud-parity liquids — **Water / Oil / Acid** — are
implemented in LQ.5 to prove the foundation carries the real
mechanics (electric amplify, fire spread, ongoing acid). The brief
asks for **3 liquids that modify resistances/stats**; these are LQ.6
content demonstrating the foundation expands by data alone:

| Liquid | Coat effect (while covered) | Trade-off | Demonstrates |
|---|---|---|---|
| **Brine** (salt water) | **+15 HeatResistance** (damp, fire-resistant) | **−15 ElectricResistance** (salt conducts — synergizes with the wet→electric chain) | resistance delta + interaction synergy |
| **Pitch** (tar) | **−2 Agility, −3 DV** (sticky, slow, easier to hit) | **Combustibility surge** (flammable like oil — fire is lethal while pitched) | stat debuff + element vulnerability |
| **Carapace-Ichor** (mystic ooze) | **+4 AV** (hardens to a shell) | **−20 ColdResistance** (brittle — freezing shatters the shell for bonus cold damage) | stat buff + a designed vulnerability |

Each is a row of data in `LiquidDefinitions` (`StatModifiers[]` +
`ResistanceModifiers[]`). A 4th/5th liquid = a new JSON row + (only
if it needs behavior the knobs can't express) one optional override
hook — exactly Qud's `LiquidHoney` seam (premise #14). The
apply/reverse of the stat deltas reuses the **already-shipped**
`equipment/StatBonus*` symmetric pattern, so the +/− is guaranteed to
net-zero on coat removal and is observable in the diag stream.

---

## 4. Architecture decisions (Qud → CoO mapping)

We mirror Qud's **conceptual architecture**, not its `LiquidVolume`
dram/proportion engine (porting that verbatim is a multi-week change
and collides with CoO's `MaterialPart`). The faithful-but-CoO-native
mapping:

| Qud | CoO mirror | Why |
|---|---|---|
| `LiquidVolume` part on puddle GameObject, `MaxVolume=-1` | **`LiquidPoolPart`** on the puddle entity: `LiquidId` + `Volume` (single scalar, no parts-per-1000 mixing in phase 1) | Reuses existing puddle entities; single-component pools cover 100% of phase-1 content; mixing is a documented deferral |
| `BaseLiquid` flyweight + `[IsLiquid]` reflection | **`LiquidDefinition`** records loaded from `Resources/Content/Data/LiquidDefinitions/*.json` via a `LiquidRegistry` (mirror `MaterialReactionResolver` loader) | CoO already JSON-loads content reflectionlessly; data-driven = expand-by-data (the brief's core ask) |
| `LiquidCovered` effect carrying a split `LiquidVolume` | **`LiquidCoveredEffect : Effect`** carrying `LiquidId` + `Amount`; non-stacking `OnStack` merges; `OnTurnStart` drips/evaporates; `Apply`/`Remove` apply/reverse stat+resist deltas | CoO's `Effect` base is the native coat carrier; mirrors `ProcessDynamics` |
| `ProcessContact` on `ObjectEnteredCellEvent` | `LiquidPoolPart.HandleEvent("EntityEnteredCell")` → exposure(`Strength+Toughness`) → `ApplyEffect(new LiquidCoveredEffect(...))` | CoO already fires `EntityEnteredCell` on the puddle (it's a cell occupant) — premise (b) closed with no MovementSystem change |
| `GetElectricalConductivityEvent` (MinEvent 3-pass) answered by coat | `LiquidCoveredEffect.OnBeforeTakeDamage` amplifies `Lightning`-attribute damage by the liquid's `Conductivity`; **plus** keep the existing `ElectrifiedEffect.OnApply` path (now reading `LiquidCoveredEffect` OR `WetEffect`) | CoO has no discharge-arc engine; the `Effect` damage hook is the equivalent seam. Chain-to-adjacent is deferred (§7) |
| Per-liquid `SmearOn/SmearOnTick` (acid ongoing dmg) | `LiquidCoveredEffect.OnTurnStart` reads `LiquidDefinition.PerTurnDamage{amount,type}` and `ApplyDamage`s | Data-driven follow-on; acid = a JSON row, not a subclass |
| Water `Combustibility=-50` damps fire | `LiquidCoveredEffect.OnBeforeTakeDamage` reduces `Fire` damage when the coat liquid has `FireDampen>0`; refreshes `WetEffect` so `ThermalPart.TryIgnite` suppression (`ThermalPart.cs:116-122`) still fires | Reuses the existing ignition-suppression; adds the damage-time reduction Qud gets from `ProcessExposure` |
| Slippery/Sticky `ObjectEnteredCell` | `LiquidPoolPart` applies a `StuckEffect`/slip on enter when `LiquidDefinition.Sticky/Slippery` | Honey-class behavior, data-flagged |

**Phase-1 deliberate divergences (documented, not drift):**

1. **No parts-per-1000 mixing.** Pools & coats are single-liquid.
   `MixWith` becomes "stronger liquid wins, amounts add." Qud's
   mixed-conductivity math collapses to the pure value. Revisit if
   content needs blends.
2. **No discharge-arc chaining.** Wet amplifies the *victim's*
   electric damage; it does not yet leap to adjacent wet creatures.
   Deferred to LQ.8 (CoO has no `ApplyDischarge` analog; building one
   is its own feature).
3. **`WetEffect` coexists with `LiquidCoveredEffect` in phase 1.**
   Water-coat refreshes `WetEffect.Moisture` so the **pinned**
   `ElectrifiedEffectDamageTests` + `MaterialPrimitivesPhaseATests`
   invariants stay green untouched. A later sub-milestone (LQ.9,
   deferred) can fold `WetEffect` into `LiquidCoveredEffect`. This is
   the smallest-blast-radius migration.
4. **Exposure model simplified.** Coat amount =
   `clamp(poolVolume, 0, Strength+Toughness)` rather than Qud's
   per-body-part adsorbence distribution. Body-part granularity is a
   documented deferral.

---

## 5. Sub-milestones (smallest blast radius first)

Each commits as one reviewable change, independently revertable,
ships one complete testable behavior. RED→GREEN→counter-check→
adversarial→review→commit per CLAUDE.md §1.4/§5.

### LQ.1 — Plan + branch (this commit)
- `Docs/LIQUID-COATING-SYSTEM-PLAN.md` (this file, incl. §A/§B reviews)
- Branch `feat/liquid-coating-system` from `main`

### LQ.2 — `LiquidDefinition` + `LiquidRegistry` (data layer) — ~10 tests
- `LiquidDefinition` record: `Id, DisplayName, Adjective,
  Conductivity, Combustibility, FireDampen, FlameTemperature,
  Adsorbence, Fluidity, Evaporativity, Staining, Slippery, Sticky,
  PerTurnDamage{amount,type}, StatModifiers[]{stat,delta},
  ResistanceModifiers[]{stat,delta}, FollowOnEffect`
- `LiquidRegistry` loads `Resources/Content/Data/LiquidDefinitions/*.json`
  mirroring `MaterialReactionResolver.cs:24-57` (priority, reset-for-tests)
- Seed JSON: `water`, `oil`, `acid` (canonical) — stat/resist liquids
  land in LQ.6
- Tests: registry loads, `Get(id)` round-trips, unknown→null,
  malformed-json tolerated, `ResetForTests` clears (mirror the
  TinkerRecipeRegistry pollution-guard we already fixed),
  water has Conductivity high + Combustibility negative, oil
  Combustibility high, acid has PerTurnDamage acid

### LQ.3 — `LiquidPoolPart` + puddle blueprints — ~8 tests
- `LiquidPoolPart{LiquidId, Volume}` on the puddle entity; save/load
  via reflection (pin like `MaterialPartRoundTripTests`)
- Blueprints: extend `WaterPuddle` + add `OilSlick`, `AcidPool`
  (Objects.json) — Render glyph/color per liquid adjective
- Pool builders: keep `RiverBuilder` (water); add an optional
  hazard-pool placement seam (used by the LQ.7 scenario, not
  worldgen yet)
- Tests: part round-trips, blueprint→LiquidPoolPart wiring, pool
  glyph matches definition, null-liquid-id safe

### LQ.4 — Transfer-on-contact (closes gap **b**) — ~12 tests
- `LiquidPoolPart.HandleEvent("EntityEnteredCell")`: when a
  Creature enters the pool's cell → exposure =
  `clamp(Volume,0,Str+Tough)` → `target.ApplyEffect(new
  LiquidCoveredEffect(LiquidId, exposure), source:pool)`
- `LiquidCoveredEffect : Effect`: non-stacking (`OnStack` →
  stronger-wins + amount add), `OnTurnStart` drips
  (`-Fluidity`)/evaporates (`-Evaporativity`, scaled by
  `ThermalPart.Temperature`), removes self at Amount≤0; water-coat
  also ensures/refreshes `WetEffect` (divergence #3)
- Counter-checks: non-Creature (item) entering pool NOT coated;
  pool with Volume 0 doesn't coat; leaving the pool doesn't
  re-coat; coat evaporates to removal
- Adversarial: null entity, entity with no StatusEffectsPart
  (auto-create path), two pools same cell (last-wins merge),
  re-enter same pool (merge not stack)
- Tests pin: step in water → `LiquidCoveredEffect(water)` present
  AND `WetEffect` present (parity invariant preserved)

### LQ.5 — Consequences: electric / fire / acid (generalizes gap **c**) — ~14 tests
- `LiquidCoveredEffect.OnBeforeTakeDamage`:
  - damage has `Lightning` attr + coat `Conductivity≥threshold` →
    `amount = round(amount * (1 + Conductivity/100))`
  - damage has `Fire` attr + coat `FireDampen>0` →
    `amount = round(amount * (1 - FireDampen/100))`
  - damage has `Fire` attr + coat `Combustibility≥50` →
    `amount = round(amount * (1 + Combustibility/200))`
- `LiquidCoveredEffect.OnTurnStart` applies `PerTurnDamage`
  (acid coat → Acid damage/turn) + `FollowOnEffect` (oil coat near
  fire → BurningEffect intensifies; keep existing
  `oil_plus_fire.json` reaction untouched)
- `ElectrifiedEffect.OnApply` generalized: read
  `LiquidCoveredEffect` conductivity OR legacy `WetEffect.Moisture`
  (keeps `ElectrifiedEffectDamageTests` green — verify, don't
  rewrite)
- Counter-checks: dry creature no amplification; non-Lightning
  damage on wet unchanged; Fire on wet reduced AND on oiled
  increased (two opposite branches both asserted); acid coat ticks
  damage, water coat does not
- Adversarial: amount=0 no-op, negative FireDampen ignored,
  multiplier never heals (clamp ≥0)

### LQ.6 — The 3 stat/resistance liquids (expandability proof) — ~12 tests
- Add `brine`, `pitch`, `carapace-ichor` JSON rows
  (StatModifiers/ResistanceModifiers only — zero new C#)
- `LiquidCoveredEffect.Apply` applies the deltas; `Remove` reverses
  them — **reuse the `EquipBonusUtility` symmetric pattern + emit
  the same `equipment/StatBonus*`-style diag** (or new
  `liquid/StatModApplied`/`Removed`)
- Counter-checks (the §3 trade-offs): Brine coat → +HeatRes AND
  −ElectricRes simultaneously; Pitch → −Agility/−DV AND fire-amped;
  Ichor → +AV AND −ColdRes; **net-zero on removal** for every stat
  (the EquipBonus invariant)
- Adversarial: stack-then-remove nets zero; coat removed by
  evaporation reverses deltas; save/load mid-coat preserves applied
  deltas exactly once (no double-apply on load)
- **Expandability test:** add a 4th liquid via JSON-only in the
  test, assert it coats + applies its delta with no C# change
  (proves the brief's "expand to more than 3" requirement)

### LQ.7 — Observability + scenario + final sweep — ~6 tests
- New `liquid` diag category: `Coated` (entity, liquidId, amount,
  source), `CoatExpired` (entity, liquidId), `CoatModifiedDamage`
  (attr, before, after, liquidId), `CoatStatApplied`/`Removed`
- `LiquidHazardShowcase` scenario: player + an oil slick, a water
  pool, an acid pool, a brine pool in a row; torch + a shock tonic
  in inventory → walk the row, get coated, observe diag + the
  electric-on-wet / fire-on-oil / acid-tick / stat-delta in the log
- Menu entry + `ScenarioCustomSmokeTests` smoke
- Combined regression sweep across all `Liquid*Tests` +
  `MaterialSystemTests` + `MaterialReaction*Tests` +
  `ElectrifiedEffectDamageTests` + `MaterialPrimitivesPhaseATests` +
  `SaveGraphRoundTripTests` (must stay green — divergence #3 is the
  guard)
- Update `OBSERVABILITY-STATUS.md` (+`liquid` category) + this doc
  (implementation log)
- Cold-eye review (Q1–Q4) + adversarial sweep
  (`LiquidCoatingAdversarialTests.cs`, 20–40 tests: parser
  malformed, stacking/merge, save reflection, mid-evaporation
  death, two-pool atomicity, exposure boundary 0/Str+Tough,
  probability-free so no RNG boundary)

### LQ.8+ — DEFERRED (explicitly out of this push)
- Electric **chain-to-adjacent** (Qud `ApplyDischarge` arc engine) —
  its own feature; needs a CoO conductivity-graph walk
- Parts-per-1000 **liquid mixing** + mixed conductivity math
- Per-**body-part** adsorbence distribution
- Liquid **flow/spread sim** (pools spreading, drying into stains as
  `LiquidStained`)
- **Freeze-into-solid** (lava→boulder, water→slippery ice cell)
- Thrown-tonic **leaves a pool** instead of direct 3×3
  (`ThrowItemCommand.ApplyTonicAoe` change)
- Folding `WetEffect` entirely into `LiquidCoveredEffect` (LQ.9)

---

## 6. Performance section (required — touches per-turn + per-move)

| Risk | Mitigation |
|---|---|
| `LiquidPoolPart.HandleEvent("EntityEnteredCell")` fires per move into a pool cell | Already the existing `EntityEnteredCell` dispatch (`MovementSystem.cs:202-240`) — no new per-frame hook. Gated by `e.ID == "EntityEnteredCell"` early-out. Pools are sparse. |
| `LiquidCoveredEffect.OnTurnStart`/`OnBeforeTakeDamage` per coated entity per turn | Bounded: an entity carries ≤1 coat (non-stacking). `OnBeforeTakeDamage` is a few int multiplies, no allocation. Mirror `StoneskinEffect`'s existing hook cost. |
| `LiquidRegistry` JSON load | One-time at boot, mirrors `MaterialReactionResolver` (already in the codebase, profiled). `ResetForTests` guard prevents test pollution into Play. |
| Stat-delta apply/remove allocation | Reuse `EquipBonusUtility` pattern — `stat.Bonus +=/−=`, no collections. |
| Diag emission per coat/tick | Gated by `Diag.IsChannelEnabled("liquid")`; zero alloc when off (same as the 14 shipped categories). |

No new MonoBehaviour, no new per-frame `Update`, no new cache, no
`ZoneRenderHooks` plumbing (the puddle entity already marks its cell
dirty via the standard render path).

---

## 7. Reusable utilities (don't reinvent)

| Utility | Path | Used for |
|---|---|---|
| `MaterialReactionResolver` loader | `Materials/MaterialReactionResolver.cs:24-57` | Pattern for `LiquidRegistry` JSON load + ResetForTests |
| `Effect` base `OnBeforeTakeDamage`/`OnTakeDamage` | `Effects/Effect.cs:193-209` | `LiquidCoveredEffect` damage modification seam |
| `StatusEffectsPart.ApplyEffect` + auto-create | `Effects/StatusEffectsPart.cs:38` | Coat application path (null-safe) |
| `EquipBonusUtility` apply/reverse + `equipment/StatBonus*` diag | `Inventory/.../EquipBonusUtility.cs` | Stat/resist delta apply+reverse symmetry + observability |
| `WetEffect` + `ThermalPart.TryIgnite` suppression | `WetEffect.cs`; `ThermalPart.cs:116-122` | Preserve fire-suppression parity (divergence #3) |
| `MovementSystem` `EntityEnteredCell` dispatch | `Turns/MovementSystem.cs:202-240` | Transfer trigger (already fires; no change) |
| `Diag` + `diag_query` | `Shared/Utilities/Diag.cs` | `liquid` category (15th) |
| `ScenarioContext`/`[Scenario]`/smoke pattern | `Scenarios/...`; `ScenarioCustomSmokeTests.cs` | LQ.7 showcase |
| `MaterialPartRoundTripTests` pattern | tests | `LiquidPoolPart`/coat save-reflection pin |

---

## 8. Observability plan (every gate emits a record)

Per CLAUDE.md "every action that can succeed OR fail emits a record":

- `liquid/Coated` — entity coated (entity, liquidId, amount, source pool id, exposureCap)
- `liquid/CoatRejected` — entity entered pool but not coated (reason: NotCreature / PoolEmpty / NullTarget)
- `liquid/CoatTick` — per-turn drip/evaporate (liquidId, amountBefore, amountAfter)
- `liquid/CoatExpired` — coat reached 0 (liquidId, turnsLasted)
- `liquid/DamageModified` — OnBeforeTakeDamage changed a number (attr, before, after, liquidId, reason: Conductive/FireDampen/Combustible)
- `liquid/StatModApplied` / `liquid/StatModRemoved` — stat/resist delta on apply/remove (stat, delta, bonusBefore, bonusAfter) — mirrors the shipped `equipment/StatBonus*`

Tests pin every emission (the skill-system rule: a future contributor
can't silently drop them).

---

## 9. Pre-flagged self-review findings (fix or defer pre-commit)

- **🟡 WetEffect duplication.** Phase 1 has BOTH `WetEffect` and a
  `LiquidCoveredEffect(water)`. This is a deliberate divergence (#3)
  to keep pinned tests green, but it's a real "two sources of wet"
  smell. Mitigation: water-coat is the *only* writer of WetEffect in
  the new path; LQ.9 (deferred) unifies. Documented in the commit.
- **🟡 Conductivity multiplier vs Qud's chain model.** CoO amplifies
  the victim's own electric damage; Qud chains the arc. These are
  *not* mechanically identical. Accept for phase 1 (parity of
  "wet = more electric"), flag LQ.8 for the true arc.
- **🟡 Exposure simplification.** `clamp(Volume,0,Str+Tough)` is
  coarser than Qud's body-part adsorbence. Accept; the player-felt
  behavior (tougher = holds more coat, drips over turns) is
  preserved.
- **🔵 Stat-delta double-apply on save/load.** The EquipBonus pattern
  applies on Apply; load re-instantiates the effect. Must ensure the
  delta is applied exactly once (Apply on coat-attach, NOT on
  deserialize). LQ.6 has an explicit save/load adversarial test.
- **🔵 Acid-vs-MaterialReaction overlap.** `acid_plus_organic.json`
  reaction already exists. The new acid *coat* PerTurnDamage must not
  double-dip with the reaction. LQ.5 counter-check asserts a single
  damage source per turn.
- **⚪ Mixing deferred.** Single-liquid pools/coats. Documented.

---

## §A. Critical self-review #1 — "Is the architecture right?"

*Adversarial read of the plan as if I'm a reviewer who wants it to
fail.*

**A1. "You're not actually porting Qud — you're building a parallel
system and calling it parity."**
Partly fair. Qud's `LiquidVolume` is a dram-quantity, multi-component,
flowing fluid sim; my `LiquidPoolPart` is a single-id scalar. The
honest claim is **mechanical parity of the player-observable
behaviors** (step in water → wet → electric amplified; oil → burns;
acid → ticking damage; coat dries over turns; coat modifies
resistances), **not** code-structure parity. The plan already says
this in §4 ("mirror conceptual architecture, not the engine") and
classes the gaps as divergences with deferral IDs. **Verdict:**
acceptable *iff* the doc's title/claim is "Qud-parity *behavior*
migration," not "LiquidVolume port." Action: §0 and §4 already frame
it this way; keep that framing in commit messages. The CLAUDE.md
parity rule (§4.2 "Classify honestly — Match/Extension/Divergent/
CoO-original") is satisfied because every divergence is enumerated
(#1–#4) with a reason and a deferral.

**A2. "The transfer trigger is wrong — `EntityEnteredCell` fires on
*non-mover occupants*, so the pool reacts when something enters its
cell. But what about the player *standing still in a pool* across
turns, or a pool that flows onto a standing creature?"**
This is a real hole. Premise (b)'s research said `EntityEnteredCell`
fires on the puddle when a creature enters the puddle's cell — good
for *walking into* a pool. But Qud also coats via `EnteredCellEvent`
for the *puddle moving onto* a creature and via standing (the coat
re-applies while submerged). My LQ.4 only handles walk-in. **Action:
LQ.4 must also (i) coat on the puddle's own placement if a creature
is already in the cell (builder/scenario path), and (ii) decide
whether standing still re-coats.** Qud re-coats while in contact;
phase 1 can apply once-on-enter + the coat persists/dries (simpler,
still parity-of-feel because the coat lasts several turns). Add an
explicit LQ.4 test: "stand in pool 5 turns → still coated (coat
hasn't fully evaporated because contact, OR documented as
once-on-enter)." **The plan must pick one and pin it.** Decision:
**once-on-enter + persistent dry-down**, with a re-coat only on
re-entry. Document as divergence #5. *(This is the single biggest
correction from review #1 — folded into §5 LQ.4 and §4.)*

**A3. "ElectrifiedEffect.OnApply already does wet→electric. If LQ.5
generalizes it to read LiquidCoveredEffect, you risk
double-amplifying: once via OnApply charge-double, once via
LiquidCoveredEffect.OnBeforeTakeDamage conductivity multiply."**
Sharp. If water-coat refreshes `WetEffect` (divergence #3) AND
`LiquidCoveredEffect.OnBeforeTakeDamage` also multiplies Lightning,
an electrified+wet creature gets hit twice. **Action: LQ.5 must make
the two paths mutually exclusive** — either (a)
`LiquidCoveredEffect.OnBeforeTakeDamage` does NOT touch Lightning
when an `ElectrifiedEffect` is present (Electrified owns that), or
(b) remove the `ElectrifiedEffect.OnApply` charge-double and route
ALL electric amplification through `LiquidCoveredEffect`, updating
`ElectrifiedEffectDamageTests` deliberately (a SCOPE DIVERGENCE
commit). Option (b) is cleaner long-term but breaks the "keep pinned
tests green untouched" promise of divergence #3. **Decision:
phase-1 = option (a)** (LiquidCovered yields Lightning to
ElectrifiedEffect; it only amplifies *direct* lightning damage that
isn't already going through an ElectrifiedEffect tick). Documented as
divergence #6 + an explicit LQ.5 counter-check
"`Electrified+Wet`_does_not_double_amplify". *(Second major
correction — without this the feature ships a 4× electric bug.)*

**A4. "Sub-milestone ordering: LQ.5 (consequences) depends on LQ.4
(coat exists). But LQ.6 (stat liquids) also needs LQ.4's
Apply/Remove symmetry. Is LQ.5 before LQ.6 correct?"**
Yes — LQ.5 proves the *interaction* hooks (damage modify) on the
canonical liquids; LQ.6 proves the *stat* hooks on new content. They
touch different `LiquidCoveredEffect` methods
(`OnBeforeTakeDamage`/`OnTurnStart` vs `Apply`/`Remove`), so they're
independently revertable. Ordering is fine. **Verdict:** no change.

**A5. "What breaks downstream?" Save format.**
`LiquidPoolPart` + `LiquidCoveredEffect` add serialized fields. CoO
just bumped save FormatVersion 3→4 for the world map. This is another
bump (→5) OR additive reflection (MaterialPart-style, which
round-trips without a version bump — premise: `MaterialPartRoundTrip
Tests` proves reflection fall-through works). **Action: prefer the
reflection-additive path (no FormatVersion bump)** — LQ.3/LQ.6 add a
save round-trip test mirroring `MaterialPartRoundTripTests`. If a
field can't round-trip via reflection, *then* bump. Documented as an
LQ.3 acceptance criterion.

**Review #1 net:** architecture is sound; **three concrete
corrections** folded back in — (A2) once-on-enter contact model as
divergence #5, (A3) electric double-amplify guard as divergence #6 +
counter-check, (A5) prefer reflection-additive save over a
FormatVersion bump.

---

## §B. Critical self-review #2 — "Will this actually ship, and is it
testable + Qud-faithful enough?"

*Second pass, different angle: delivery risk, test integrity, and the
parity bar.*

**B1. Test-cascade risk (empirically observed this session).** The
world-map work repeatedly hit "unknown blueprint" cascades because
zone pipelines pull dozens of blueprints. LQ.3/LQ.4 spawn pool
entities and coat *creatures* — any test that generates a real zone
or a real creature will hit the same cascade. **Action: every
Liquid* test must use bare hand-built `Entity` + `Zone` fixtures
(the pattern that finally worked: `new Zone("X")` + minimal
inline-JSON blueprints), NOT `OverworldZoneManager.GetZone`.** Add
this as an explicit testing-convention note in LQ.2. Without it,
the sub-milestones will burn the same hours the world-map ones did.
*(Concrete delivery-risk mitigation from lived experience.)*

**B2. Is the `OnBeforeTakeDamage` seam real?** Review claims only
`StoneskinEffect` uses it. I must verify the *signature* and that
`StatusEffectsPart` actually routes it to multiple effects (not
just the first). The research cited `Effect.cs:193-209` +
`StatusEffectsPart.cs:474-495`. **Action: LQ.5's FIRST step is a
verification-sweep micro-task — read those exact lines, confirm
`OnBeforeTakeDamage` is called for *every* effect in the list and
that mutating `damage.Amount` propagates (the combat port's false-
premise lesson: "Qud has X" must be verified by reading).** If the
hook only fires for one effect or doesn't propagate amount, the
whole §4 electric/fire mapping is invalid and must move to a
`CombatSystem.ApplyResistances` extension instead. This is the
single highest-leverage pre-code check. Flag it BLOCKING for LQ.5.

**B3. Parity bar — "is wet→electric *actually* like Qud?"** Qud's
feel is *the arc leaps between wet things and chains*. My phase-1
delivers *the wet creature takes more electric damage*. A player
who knows Qud will notice the chain is missing. **Is that
acceptable "parity"?** Honest answer: it's **parity of the
coating→vulnerability rule**, not of the *discharge spectacle*. The
brief says "we want the foundation … so we can expand very easily."
The foundation (coat carries conductivity; damage path reads it) is
exactly what the chain feature (LQ.8) would build on — the
`LiquidCoveredEffect.Conductivity` query is the same seam Qud's
`ApplyDischarge` reads. **Verdict:** acceptable *and* correctly
sequenced — but the LQ.7 doc + commit must state plainly "chaining
is LQ.8; phase 1 is the conductivity foundation," so we don't
overclaim parity (CLAUDE.md §4.2: overclaimed parity is a bug).
Action: add an explicit "Parity classification" table to the LQ.7
doc update — Match / Extension / Divergent / Deferred per behavior.

**B4. The 3 stat liquids — do they earn their place, or are they
filler?** Re-examined against the brief ("liquids that affect
resistances, stats such as defense, offense"). Brine (+HeatRes/
−ElectricRes), Pitch (−Agi/−DV + flammable), Ichor (+AV/−ColdRes).
Each (i) modifies a *resistance or combat stat*, (ii) carries a
*designed trade-off* (so it's a tactical choice, RPG-appropriate per
PROJECT-IDENTITY), (iii) requires **zero new C#** (pure JSON) —
which is the literal proof of the "expand by data" requirement. The
LQ.6 "add a 4th liquid in-test, JSON-only, assert it works" test is
the *executable proof* of expandability. **Verdict:** they earn it;
they're not filler — they're the requirement's acceptance criteria.
One refinement: Pitch's "Combustibility surge" overlaps oil — to
keep them distinct, **Pitch's identity = the stat debuff (slow/
sticky), oil's = the fire interaction.** Make Pitch's flammability a
*smaller* combustibility than oil so they're not redundant. Folded
into §3.

**B5. Scope realism.** 7 sub-milestones, ~70 tests + a 20–40 test
adversarial sweep, across data layer + part + effect + combat hook +
content + observability + scenario. Comparable to the world-map
feature (8 sub-milestones, 57 tests) which shipped cleanly this
session. The MCP-flakiness tax is real but survivable with the
resilient-script + 10s-spacing discipline already in use. **Verdict:
realistically ~1.5–2 focused sessions.** No scope cut needed, but
LQ.8+ deferral list is the pressure valve if a sub-milestone blows
up.

**B6. Did review #1's corrections actually get folded in?** Checking:
A2 (once-on-enter) → yes, divergence #5 + LQ.4 ("decide & pin").
A3 (double-amplify) → yes, divergence #6 + LQ.5 counter-check.
A5 (save reflection) → yes, LQ.3 acceptance criterion. All three
traceable. **Verdict:** review loop closed.

**Review #2 net:** delivery is realistic; **two more corrections** —
(B2) make "verify the `OnBeforeTakeDamage` routing by reading the
exact lines" a BLOCKING first step of LQ.5 (false-premise guard),
and (B4) differentiate Pitch from oil so the 3 stat liquids aren't
redundant. Test-cascade discipline (B1) and the parity-classification
honesty table (B3) are added as explicit acceptance criteria.

---

## 10. Net plan after both reviews (the corrected spec)

Folded corrections:

- **Divergence #5** — coating is **once-on-enter + persistent
  dry-down**; re-coat only on re-entry (not while standing). Pinned
  by an LQ.4 "stand 5 turns" test.
- **Divergence #6** — `LiquidCoveredEffect` **yields Lightning to a
  present `ElectrifiedEffect`** (no double-amplify); only amplifies
  direct lightning when no ElectrifiedEffect owns the hit. Pinned by
  an LQ.5 counter-check.
- **LQ.3 acceptance** — save via **reflection-additive** (no
  FormatVersion bump) with a `MaterialPartRoundTripTests`-style pin;
  bump only if reflection can't carry a field.
- **LQ.5 BLOCKING step 0** — read `Effect.cs:193-209` +
  `StatusEffectsPart.cs:474-495`, confirm `OnBeforeTakeDamage` fires
  for *every* effect and amount-mutation propagates, BEFORE writing
  the damage-modify code. If false → reroute via
  `CombatSystem.ApplyResistances` extension (documented pivot).
- **All Liquid* tests** use bare `Entity`/`Zone` + minimal inline
  JSON fixtures (no `OverworldZoneManager.GetZone`) — the
  test-cascade discipline.
- **§3 refinement** — Pitch's combustibility < oil's; Pitch's
  identity is the stat debuff, oil's is fire interaction (distinct
  roles).
- **LQ.7 doc** — ship a **Parity classification table**
  (Match/Extension/Divergent/Deferred per behavior) so parity isn't
  overclaimed.

This is the spec to implement when the user greenlights LQ.2.
Planning is complete: the architecture is Qud-behavior-faithful, the
divergences are enumerated with deferral IDs, the 3 stat liquids are
the executable acceptance criteria for "expand by data," and two
adversarial self-reviews surfaced and folded **five** concrete
corrections (A2, A3, A5, B2, B4) plus two acceptance disciplines
(B1, B3).

---

## 11. Implementation log (per phase, per CLAUDE.md rule #3)

### LQ.2 — `LiquidDefinition` + `LiquidRegistry` — SHIPPED (`0cc6611`)
Data layer: `[Serializable] LiquidDefinition` (flyweight), static
`LiquidRegistry` (JsonUtility loader, later-wins, malformed→warn+skip),
3 JSON defs (water/oil/acid). 15 tests.

### LQ.3 — `LiquidPoolPart` + puddle blueprints — SHIPPED (`ece5c35`)
`LiquidPoolPart : Part` (LiquidId/Volume, data-driven render from
def Glyph/Color, null-safe). WaterPuddle wired + OilSlick/AcidPool
blueprints. 12 tests. Folded the 5 pre-impl critical-review findings
(F1 render-from-fields, F2 part-vs-blueprint ownership, F3 flyweight
immutability contract + pinned test, F4 JsonUtility omitted-default
coverage, F5 within-file dup-Id).

### LQ.4 — Transfer-on-contact (closes gap **b**) — SHIPPED

**What shipped**
- `LiquidCoveredEffect : Effect` — `LiquidId`/`Amount` (plain public,
  reflection-round-trippable, no FormatVersion bump per §A5);
  `DisplayName` live from def `Adjective`; `OnApply`/`OnStack`
  refresh `WetEffect` only when id=="water" (divergence #3);
  `OnStack` = stronger-wins-id + amounts-add, always returns true
  (merge-not-stack, divergence #1); `OnTurnEnd` dries by
  `Fluidity+Evaporativity` (heat-accelerated like `WetEffect`),
  removes self at Amount≤0 (divergence #5 dry-down half).
- `LiquidPoolPart.HandleEvent("EntityEnteredCell")` — gates
  (NullActor / NotACreature / RegistryUninitialized / NoLiquidId /
  UnknownLiquid / PoolEmpty / ZeroExposure), each emitting a
  `liquid/CoatRejected` diag; success emits `liquid/Coated`.
  `exposure = clamp(Volume, 0, Strength+Toughness)`.
- `Diag.DefaultOnCategories` += `"liquid"`.

**RED→GREEN**
RED: `LiquidCoatingTests.StandingStill_DoesNotReCoat` failed
(Expected 30, was 60) — a no-op `TryMove(0,0)` re-fired
`EntityEnteredCell`. GREEN after the MovementSystem fix below; full
suite 43/43 (16 LiquidCoating + 15 LiquidRegistry + 12 LiquidPool)
+ 175 regression GREEN (movement/trigger/pressure-plate/electrified/
save-round-trip/material/effect-round-trip).

**SCOPE DIVERGENCE — MovementSystem cell-change latent-bug fix**
The plan scoped LQ.4 as one `LiquidPoolPart` handler. The
once-on-enter test (divergence #5) surfaced a **pre-existing latent
bug**: `MovementSystem.FireCellEnteredEvents` (`MovementSystem.cs`
TryMoveEx:102 / TryMoveTo:151) fired `EntityEnteredCell`
unconditionally — even when `currentCell == targetCell` (a 0-delta
move). The cell-CHANGE contract was **caller-convention-only** yet
already *documented as guaranteed* in
`PressurePlateTriggerPart.cs:334-336` ("EntityEnteredCell fires only
on cell-CHANGE moves … verified during T2.1 sweep") — doc-vs-impl
drift. Fix: `FireCellEnteredEvents` takes `sourceCell` and
coordinate-compares for an early-out (null source = first-placement
entry, still fires). This makes the documented contract true in
code and fixes a real double-trigger for runes/mines/pressure-plates
on a literal 0,0 move. All 105 movement/trigger/rune regression
tests stayed green.

**Self-review (CLAUDE.md §5)**
- 🟡→fixed: Q2 cross-feature payload-shape consistency — `NoLiquidId`
  `CoatRejected` payload omitted `liquidId`; now all `CoatRejected`
  payloads share `{reason, liquidId, volume}` (+`cap` for
  ZeroExposure as legitimate extra debug info).
- 🔵 noted: per-reason `CoatRejected` diag (NullActor / NotACreature
  / RegistryUninitialized / NoLiquidId / UnknownLiquid /
  ZeroExposure) is behaviorally tested but only `PoolEmpty` is
  diag-payload-pinned; the dedicated `<Feature>AdversarialTests.cs`
  (scheduled LQ.7) pins each reason.
- 🧪 noted: `LiquidCoveredEffect` reflection save round-trip is
  inferred from plain-public-fields parity with `WetEffect`/
  `LiquidPoolPart` (EffectRoundTrip* groups green) but not yet
  pinned by a LiquidCovered-specific round-trip test → LQ.7.
- ⚪ noted: `OnStack` amount-add is uncapped (plan divergence #1
  "amounts add"); gameplay-tuning concern, deferred.

**Files**
- NEW `Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs`
- MOD `Assets/Scripts/Gameplay/Materials/LiquidPoolPart.cs`
  (+`HandleEvent`, +`using CavesOfOoo.Diagnostics`)
- MOD `Assets/Scripts/Gameplay/Turns/MovementSystem.cs`
  (cell-CHANGE guard — scope-divergence latent-bug fix)
- MOD `Assets/Scripts/Shared/Utilities/Diag.cs` (+`"liquid"` channel)
- NEW `Assets/Tests/EditMode/Gameplay/Materials/LiquidCoatingTests.cs`
  (16 tests)

### LQ.5 — Consequences: electric / fire / acid — SHIPPED

**BLOCKING step 0 (false-premise guard, §B4) — VERIFIED TRUE.**
`StatusEffectsPart.HandleBeforeTakeDamage:491-497` iterates **all**
`_effects` → `OnBeforeTakeDamage` fires for every effect.
`CombatSystem.cs:751-836` passes `damage` by reference into the
`BeforeTakeDamage` event then **re-reads `damage.Amount` at
:831-833** (`Math.Max(0,…)` clamp) for the HP decrement; my hook
runs BEFORE `ApplyResistances` (:797-801) — exactly the planned
order. No reroute needed.

**What shipped**
- `LiquidCoveredEffect.OnBeforeTakeDamage` — Lightning ⇒
  `×(1+Conductivity/100)`; Fire ⇒ `×(1−FireDampen/100)` then (if
  `Combustibility≥50`) `×(1+Combustibility/200)`. Consts
  `CONDUCTIVITY_/COMBUSTIBLE_AMPLIFY_THRESHOLD=50`.
- **Divergence #6 enforced**: target has `ElectrifiedEffect` ⇒
  Lightning branch yields entirely (Electrified owns electric
  amplification). Pinned by
  `ElectrifiedPlusWaterCoat_DoesNotDoubleAmplify` (== electrified+wet,
  not 2×).
- `LiquidCoveredEffect.OnTurnStart` — `PerTurnDamage` tick (acid →
  Acid/turn) via `CombatSystem.ApplyDamage`, mirroring
  `ElectrifiedEffect.OnTurnStart`.
- `ElectrifiedEffect.OnApply` — additive **OR**: charge doubles on
  `moist || conductiveCoat`. Water coats already satisfy `moist`
  (div #3 WetEffect) so all 8 `ElectrifiedEffectDamageTests` stay
  green untouched; the clause only newly fires for conductive
  NON-water coats — pinned by `ConductiveCoat_DoublesElectrifiedCharge`.

**Scope-prune (§1.3)** — `FollowOnEffect` deferred ⚪: no shipped
liquid sets it; "oil near fire → Burning" needs reaction-system
coupling (untouched `oil_plus_fire.json`) bigger than LQ.5. Field
stays on `LiquidDefinition`; hook is a documented no-op until a
content+reaction follow-up.

**RED→GREEN** — RED was a compile error (`CS0103` missing
`using CavesOfOoo.Diagnostics` in the new test file). GREEN after
fix: 65/65 (15 LQ.5 + 8 pinned Electrified + LQ.4 suite) + 113
regression (CombatSystem/Spec, MaterialSystem,
MaterialReactionPhaseCRE, AcidTonic, LightningTonic,
SaveGraphRoundTrip). 178 total green.

**Self-review (§5)**
- 🧪 RED→GREEN compression — tests + production written together;
  observed RED was the test file's compile error, not an
  independently-observed behavioral RED (a pre-impl compile would
  have been a member-not-found RED, equivalent per §2.1, but not
  separately observed). Honesty note.
- ⚪ Observability via the existing `damage` channel — no new
  `liquid/*` emission by design: `CombatSystem.cs:763-777` already
  emits `damage/PreDamageMutation` (amountBefore/After/delta)
  whenever an effect mutates `Damage.Amount` during
  `BeforeTakeDamage`, so water/oil amplification is query-observable;
  acid ticks emit their own `damage` records via `ApplyDamage`.
  Documented choice, not a gap.
- Q1 symmetry ✓ / Q3 counter-checks ✓ (dry vs coated, dampen vs
  amplify both branches, acid-ticks vs water-doesn't, div-#6 yield)
  / Q4 doc-vs-impl ✓.

**Files**
- MOD `Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs`
  (+OnBeforeTakeDamage, +OnTurnStart, +2 threshold consts)
- MOD `Assets/Scripts/Gameplay/Effects/Concrete/ElectrifiedEffect.cs`
  (OnApply additive-OR conductive-coat clause)
- NEW `Assets/Tests/EditMode/Gameplay/Materials/LiquidConsequencesTests.cs`
  (15 tests)

### LQ.6 — The 3 stat/resistance liquids + expandability — SHIPPED

**Premise verification (§A5 save claim) — VERIFIED TRUE.**
`SaveSystem.SaveEffect:1193-1199` calls
`WritePublicFields(effect, …, exclude Owner/Duration)`;
`LoadEffect:1202-1218` uses
`FormatterServices.GetUninitializedObject` (ctor + OnApply NOT
re-run on load) then `ReadPublicFields`. `WriteFieldValue` supports
`int`+`string` (:12,:18). So `LiquidId`/`Amount`/`AppliedModsRaw`
round-trip reflectively with no `FormatVersion` bump — LQ.4's
"reflection-additive" claim is true in code, and LQ.6's
no-double-apply rests on it.

**What shipped**
- `LiquidCoveredEffect` stat-modifier engine: `ApplyStatModifiers`
  pushes `def.StatModifiers`+`ResistanceModifiers` via the symmetric
  `Stat.Bonus += delta` pattern (mirrors
  `EquipBonusUtility.ApplyEquipBonuses`), recording EXACTLY what
  landed into a flat `AppliedModsRaw` string;
  `ReverseStatModifiers` undoes that record (not re-derived from the
  def → exact after id-swap / registry reset). Wired into
  OnApply/OnRemove; OnStack reverses-outgoing-then-applies-incoming
  on a stronger-wins id swap (no leak). Idempotent (non-empty
  `AppliedModsRaw` ⇒ no-op) ⇒ no double-apply on re-coat or load.
  Emits `liquid/StatModApplied` + `liquid/StatModRemoved`.
- `AppliedModsRaw` is a flat `string` (not a `List`) deliberately:
  round-trips on the proven `LiquidId` reflection path, no
  `List`-round-trip risk, mirrors the `EquipBonuses` convention.
- 3 JSON-only liquids (zero new C#): `brine` (+15 HeatRes / −15
  ElectricRes, conductive), `pitch` (−2 Agi / −3 DV, Combustibility
  90 ⇒ Fire-amped via LQ.5), `carapace-ichor` (+4 AV / −20 ColdRes).

**RED→GREEN** — 70/70 (16 LQ.6 + LQ.4 16 + LQ.5 15 + Registry/Pool)
+ 82 regression (SaveGraphRoundTrip, all 4 EffectRoundTrip*,
ElectrifiedEffectDamage, CombatSystem). 152 green. The
EffectRoundTrip*+SaveGraph pass with the new `AppliedModsRaw` field
proves the no-double-apply save contract.

**Self-review (§5)**
- 🟡 → tracked to LQ.7: `LiquidRegistry` is NOT bootstrapped at
  runtime (no `Resources.LoadAll("Content/Data/LiquidDefinitions")`
  in `GameBootstrap`). The 6 JSON liquids are inert in-game until
  wired. NOT a defect in LQ.6's deliverable (engine+content+tests
  use inline JSON per §B1) — it is precisely the LQ.7 sub-milestone
  ("observability + scenario + final sweep" needs the registry
  live). Fixing it here = doing LQ.7's work = scope creep, so
  deferred with an explicit owner rather than "fix pre-commit".
- 🧪 RED→GREEN compression — tests + production written together;
  the observed first compile would have been a member-not-found RED
  (`AppliedModsRaw`/`ApplyStatModifiers` didn't exist) but I did not
  observe it independently. Same honesty note as LQ.5; the GREEN
  regression suite's value is unaffected.
- ⚪ `quicksilver` (the expandability proof's 4th liquid) is
  test-only inline JSON; real >3 content is future, not LQ.6 scope.
- Q1 symmetry ✓ (Apply/Reverse symmetric; OnStack id-swap
  reverse-then-apply ordered correctly) / Q3 counter-checks ✓ (each
  trade-off buff+debuff together, net-zero ×6 stats, water-no-mods,
  absent-stat graceful, expandability, re-coat no-double, id-swap,
  save/load exactly-once) / Q4 doc-vs-impl ✓.

**Files**
- MOD `Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs`
  (+AppliedModsRaw, +ApplyStatModifiers/AccumulateMods/
  ReverseStatModifiers, OnApply/OnRemove/OnStack wiring)
- NEW `Assets/Resources/Content/Data/LiquidDefinitions/brine.json`
- NEW `Assets/Resources/Content/Data/LiquidDefinitions/pitch.json`
- NEW `Assets/Resources/Content/Data/LiquidDefinitions/carapace-ichor.json`
- NEW `Assets/Tests/EditMode/Gameplay/Materials/LiquidStatModifierTests.cs`
  (16 tests)

### LQ.7 — Observability + bootstrap + scenario + sweep — SHIPPED

**What shipped**
- `GameBootstrap` Step 1b': `Resources.LoadAll<TextAsset>(
  "Content/Data/LiquidDefinitions")` →
  `LiquidRegistry.InitializeFromJsonSources` (mirrors the
  MaterialReactions block) — closes the LQ.6 🟡; all 6 JSON liquids
  are now live in-game, not just in tests.
- `liquid/CoatExpired` diag in `LiquidCoveredEffect.OnRemove`
  (payload `{liquidId, cause=LastRemovalCause}`) — the paired
  terminal record so `liquid` lifecycle is queryable end-to-end
  (`Coated → StatModApplied → … → StatModRemoved → CoatExpired`).
- `LiquidHazardShowcase` scenario (water→oil→acid→brine pool row +
  `LiquidDemoProbePart` narrating coat id/amount/live resistances) +
  menu entry (Combat Stress, priority 111) + smoke test.
- `LiquidCoatingAdversarialTests.cs` — **28** tests across the
  mandatory taxonomy (parser-malformed AppliedModsRaw, stacking/
  merge incl. equal-amount determinism + weaker-no-thrash + swap
  chains, save/load reflection reach, mid-evaporation death, two-pool
  atomicity, multi-instance independence, exposure boundaries,
  null-safety on every public hook, unknown-id graceful,
  registry-reset-mid-life, diag dispatch invariants, idempotent
  remove). **0 bugs found** — value is the regression pins (honesty
  bound: bounded by imagined bug classes per ADVERSARIAL_TESTING.md).
- Docs: `CONTENT-ROADMAP.md` Recently-shipped row;
  `OBSERVABILITY-STATUS.md` +`liquid` category table.

**Combined regression sweep — 253/253 GREEN**
`LiquidCoatingAdversarial` + all 5 `Liquid*Tests` + `MaterialSystem`
+ `MaterialReaction{PhaseCRE,ResolverPhaseB}` +
`MaterialPrimitivesPhaseA` + `ElectrifiedEffectDamage` (divergence #3
guard) + `SaveGraphRoundTrip` + `ScenarioCustomSmoke` (incl. the new
showcase). Clean compile (0 console errors).

**Cold-eye review (CLAUDE.md Q1–Q4) — feature-wide, post-green**
- Q1 symmetry ✓ — `ApplyStatModifiers`↔`ReverseStatModifiers`,
  `Coated`↔`CoatExpired`, `StatModApplied`↔`StatModRemoved`, OnStack
  id-swap = reverse-then-apply (ordered correctly). ⚪ deliberate
  asymmetry: `RefreshWaterCoupling` is NOT reversed in OnRemove —
  WetEffect has its own independent evaporation timer (divergence
  #3); the coat drying must not instantly dry moisture. Documented,
  not a bug.
- Q2 cross-feature consistency 🔵 — every `liquid` payload leads with
  `liquidId` (consistent). `Coated` uses actor=mover/target=pool
  (cross-entity interaction) while `StatMod*`/`CoatExpired` use
  actor=creature/target=null (creature-internal). Defensible
  semantic split; flagged for a future reader, no gameplay impact.
- Q3 counter-checks ✓ — every non-trivial branch paired (water/
  non-water, Lightning±Electrified, Fire dampen/amp, stat apply/
  reverse net-zero, id-swap vs weaker-no-thrash, save single/double).
  🔵 carryover: per-reason `CoatRejected` payload still only
  `PoolEmpty`-pinned (LQ.4 finding); behaviorally covered, accept.
- Q4 doc-vs-impl 🔵→fixed — plan §5 LQ.7 listed a
  `CoatModifiedDamage` diag; impl deliberately omits it (LQ.5 ⚪:
  `damage/PreDamageMutation` already carries before/after/delta).
  Drift killed by documenting the choice here + in
  OBSERVABILITY-STATUS.md.
- **No 🔴/🟡.** Feature is internally consistent across LQ.4–LQ.7.

**Files**
- MOD `Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs`
  (Step 1b' LiquidDefinitions load)
- MOD `Assets/Scripts/Gameplay/Materials/LiquidCoveredEffect.cs`
  (+`liquid/CoatExpired` in OnRemove)
- NEW `Assets/Scripts/Scenarios/Custom/LiquidHazardShowcase.cs`
  (+`LiquidDemoProbePart`)
- MOD `Assets/Editor/Scenarios/ScenarioMenuItems.cs` (menu entry 111)
- NEW `Assets/Tests/EditMode/Gameplay/Materials/LiquidCoatingAdversarialTests.cs`
  (28 tests)
- MOD `Assets/Tests/EditMode/Gameplay/Scenarios/ScenarioCustomSmokeTests.cs`
  (showcase smoke)
- MOD `Docs/CONTENT-ROADMAP.md`, `Docs/OBSERVABILITY-STATUS.md`,
  this doc

### Feature summary (LQ.1–LQ.7)

The brief — "do pools transfer liquid onto the player, does it affect
how other elements treat them; Qud-parity; 3 stat/resistance liquids;
expand beyond 3" — is fully answered:
- **Transfer:** ✅ step in a pool → `LiquidCoveredEffect` (LQ.4).
- **Consequences:** ✅ water amps Lightning / damps Fire, oil amps
  Fire, acid ticks, conductive coat supercharges Electrified (LQ.5).
- **3 stat/resistance liquids:** ✅ brine/pitch/carapace-ichor with
  designed trade-offs, net-zero on removal (LQ.6).
- **Expand beyond 3:** ✅ a 4th liquid is one JSON row, zero C# —
  pinned by `Expandability_FourthLiquid_JsonOnly` (LQ.6).
- **Qud-parity:** behavior-faithful to `BaseLiquid`/`LiquidCovered`;
  6 enumerated divergences (no ppt mixing, water→Wet, stronger-wins,
  once-on-enter, Electrified-owns-Lightning) each documented + pinned.
~75 new tests; LQ.8+ scope explicitly deferred (§5 LQ.8+).

---

## 12. Post-ship fix — element-alias detection (🔴 latent bug)

**Surfaced** while building the spell test bench (the user asked for
a scenario to cast spells at coated NPCs).

**Bug.** LQ.5 `LiquidCoveredEffect.OnBeforeTakeDamage` detected
elements with literal `damage.HasAttribute("Lightning")` /
`HasAttribute("Fire")`. `Damage.HasAttribute` is a raw
`List<string>.Contains`, but the damage layer uses an
alias-collapsing flag system and **every real spell/weapon tags the
canonical alias**, not those literals: `ArcBoltMutation`→`"Electric"`,
`ConflagrationMutation`→`"Heat"`, `IceSword`/`CryoLance`→`"Ice"`.
Net effect: the entire LQ.5 amplify/dampen layer (and LQ.6 pitch's
fire-amp trade-off) was **inert against all real spells and elemental
weapons** — it only fired for the exact strings `"Lightning"`/`"Fire"`,
which essentially nothing in real content uses (and `ElectrifiedEffect`
tags `"Lightning"` but divergence #6 makes the coat yield there
anyway).

**Why the LQ.5 suite was green.** Every LQ.5/LQ.6 test
hand-constructed `new Damage(n); d.AddAttribute("Lightning"|"Fire")`
— the literal the buggy code happened to match. The per-invariant
counter-checks all used the same literal, so they were vacuous on
the element-detection axis. Classic "the test pinned the wrong
string" gap; the dedicated adversarial sweep didn't probe attribute
*aliasing* because that surface wasn't in the taxonomy list.

**Fix.** Switch to the flag predicates the canonical neighbor uses:
`HasAttribute("Lightning")`→`IsElectricDamage()`,
`HasAttribute("Fire")`→`IsHeatDamage()` — exactly how
`CombatSystem.ApplyResistances:983-985` routes resistance, so the
coat layer and the resistance layer now agree on what "fire"/
"electric" means. `"Lightning"`/`"Fire"` collapse to the same flags
so the pre-fix LQ.5 suite stays green (backward-compatible widening).

**Tests.** `LiquidCoatElementAliasTests.cs` — 4 RED→GREEN
(`"Electric"`/`"Heat"`/`"Shock"`/`"Electricity"` now interact),
3 backward-compat/counter-checks (literal still works, Cold NOT
touched, div #6 yield survives the switch). RED confirmed on the
pre-fix impl before the change. Regression 148/148 GREEN
(alias + LQ.5 + LQ.4/6/7 suites + ElectrifiedEffectDamage +
CombatSystem).

**Methodology note (Angle B — Qud-parity-first).** This is the
finding the cold-eye review's Angle A taxonomy missed: "what does
the neighbor (`ApplyResistances`) do, and am I doing it the same
way?" would have caught it. Logged so future audits add
"attribute-alias normalization vs the canonical detector" to the
bug-class checklist for any damage-attribute-reading feature.

**Files** — MOD `LiquidCoveredEffect.cs` (2 predicate swaps + the
why-comment); NEW `LiquidCoatElementAliasTests.cs` (7 tests).

---

## 13. LX — Qud-liquid expansion: Lava / Gel / Sap / Honey

**Status:** PLANNED (LX.1). The LQ.6 "expand by data alone" thesis,
exercised for real: 4 more Qud liquids as **JSON-only content, zero
new C#**, surfaced + verified through the v3.1 self-auditing bench
(first real application of the `Docs/MCP_PlayMode_Testing_Strategy.md`
§Deterministic-Self-Auditing-Scenarios pattern).

### 13.1 Goal & scope

Add `lava`, `gel`, `sap`, `honey` using only the LiquidDefinition
knobs the LQ.5/LQ.6 engine **already consumes**
(`Conductivity`, `Combustibility`, `FireDampen`, `PerTurnDamage`,
`StatModifiers`, `ResistanceModifiers`, `Fluidity`, `Evaporativity`).
No engine changes. Out of scope: any liquid needing un-wired knobs
(`Slippery`/`Sticky`/`Staining`/`FollowOnEffect`) for its *primary*
character, or new mechanics.

### 13.2 Verification sweep (Qud `XRL.Liquids/` → CoO wired knobs)

| Qud class | Qud ctor values (cited) | CoO mapping | Drift / decision |
|---|---|---|---|
| `LiquidLava` | `Temperature=1000`, `MixedElectricalConductivity=90`, `ConsiderDangerousToContact=true`; drink → `TakeDamage(…, "Heat")`, reads `HeatResistance` (LiquidLava.cs:25,32,117,224) | `Conductivity:90`, `PerTurnDamage{8,"Heat"}`, **CoO** `ResistanceModifiers HeatResistance:-25` | **Documented divergence:** Qud burns via the temperature sim; we model the burn as the scalar `PerTurnDamage` tick — *identical precedent to our acid divergence* (plan §4 #1). The −HeatRes is a CoO design echo of carapace-ichor's −ColdRes (lava-soaked ⇒ fire bites harder). |
| `LiquidGel` | `MixedElectricalConductivity=100`, `Fluidity=5`, `Evaporativity=1`, `SlipperyWhenWet` (LiquidGel.cs) | `Conductivity:100`, `Fluidity:5`, `Evaporativity:1`, `Slippery:true` (data-stable, cosmetic till LQ.8) | Pure conductor; a non-water/non-brine Electric-amp coat. Slippery carried for shape only — character comes from the wired `Conductivity`. |
| `LiquidSap` | `Combustibility=70`, `Fluidity=3`, `Evaporativity=1`, `FlameTemperature=250`, `InterruptAutowalk` (LiquidSap.cs) | `Combustibility:70`, `Fluidity:3`, `Evaporativity:1`, `FlameTemperature:250`, **CoO** `StatModifiers Agility:-2`, `Sticky:true` | Qud's stickiness (`Sticky`) is un-wired in CoO; the "slowed in sap" character is delivered via the **wired** `StatModifiers` path — the exact technique pitch uses. |
| `LiquidHoney` | `Combustibility=60`, `Adsorbence=25`, `Fluidity=10`, `Evaporativity=1` (LiquidHoney.cs) | `Combustibility:60`, `Adsorbence:25`, `Fluidity:10`, **CoO** `StatModifiers Agility:-2,DV:-3`, `Sticky:true` | Same: canonical sticky-slow via wired `StatModifiers` (Qud `Sticky` cosmetic till LQ.8). |

No false premises: the wired-knob set was re-confirmed by grep
against `LiquidCoveredEffect.cs` (only Conductivity / Combustibility /
FireDampen / PerTurnDamage / StatModifiers / ResistanceModifiers /
Fluidity / Evaporativity are read); `Slippery`/`Sticky`/`Staining`/
`FollowOnEffect` have **zero** gameplay consumers today.

### 13.3 Scope-prune (rejected liquids + rationale)

- **Slime/Goo/Ooze/Sludge** — primary trait is movement/prone
  (`ObjectGoingProne`,`GetNavigationWeight`) ⇒ cosmetic till the
  LQ.8 movement milestone. Cut.
- **Blood/Wine/Cider** — drink/flavor-centric (`Drank` override),
  ~no wired-knob character as a coat. Cut.
- **Salt/Algae** — value is freeze-to-solid (Halite, `FreezeObject*`)
  ⇒ explicitly LQ.8-deferred. Cut.
- **BrainBrine/Putrescence** — confusion/sickness needs
  `FollowOnEffect` (⚪-deferred LQ.5 hook). Revisit post-FollowOnEffect.
- **Convalessence** — healing pool; needs ~1 new knob
  (negative `PerTurnDamage`/`PerTurnHeal`). High-flavor follow-up,
  not this JSON-only ship. Cut with a note.
- **NeutronFlux/ProteanGunk/Cloning** — Qud reality/mutation/clone
  systems (`SmearOn`,`ProcessTurns`,`MixingWith`); large C#, low ROI.
  Cut.

### 13.4 The 4 liquids (Qud-informed, CoO-tuned)

| id | Glyph/Color | Conductivity | Combustibility | FireDampen | PerTurnDamage | StatModifiers | ResistanceModifiers | Fluidity/Evap |
|---|---|---|---|---|---|---|---|---|
| `lava` | `~` `&R` | 90 | 0 | 0 | `{8,"Heat"}` | – | `HeatResistance:-25` | 15 / 0 |
| `gel` | `~` `&c` | 100 | 0 | 0 | – | – | – | 5 / 1 |
| `sap` | `~` `&w` | 0 | 70 | 0 | – | `Agility:-2` | – | 3 / 1 |
| `honey` | `~` `&Y` | 0 | 60 | 0 | – | `Agility:-2,DV:-3` | – | 10 / 1 |

Expected single-hit bench matrix (end-to-end factor) — **CORRECTED
in LX.3 (the original line below mis-stated sap/honey):**
- `lava`: Electric ×1.90 (Cond 90), Heat ×1.25 (−25 HeatRes); the
  8/turn PerTurnDamage tick is a *separate* turn mechanic, NOT in
  the single-hit matrix cell.
- `gel`: Electric ×2.00 (Cond 100).
- `sap`: Heat **×1.35** (Combustibility 70 — sap IS flammable, same
  branch as oil/pitch) + −2 Agility on coat, net-zero on removal.
- `honey`: Heat **×1.30** (Combustibility 60) + −2 Agi/−3 DV.

> ❌ Original LX.1 line (kept for the doc-vs-impl honesty trail):
> *"sap/honey all elements ≈1.0 (no element knob)"* — WRONG. sap/honey
> carry Combustibility (70/60), an element knob, so they amplify Heat
> exactly like oil/pitch. Caught by the LX.3 cold-eye Q4 pass; the
> shipped values are the corrected ones above.

### 13.5 Sub-milestones (smallest blast radius first)

- **LX.1** — this plan + sweep (doc commit).
- **LX.2** — 4 JSON files (+`.meta`) + `LiquidExpansionContentTests.cs`:
  RED = read each file from disk, load, assert knobs (fails before
  files exist) → GREEN. Behavior pins + counter-checks: lava ticks
  Heat (`OnTurnStart`) + amplifies Electric; gel Electric ×~2;
  sap/honey reduce Agility on coat and **net-zero on removal**
  (EquipBonus invariant); counter: gel has no StatModifiers, sap has
  no Electric interaction. Adversarial: unknown-stat skip, registry
  reset mid-coat still nets zero. RED→GREEN, regression sweep,
  commit.
- **LX.3** — add the 4 to the `LiquidSpellTestBench` rig (+ extend
  the `ClearCell` corridor) so the matrix auto-audits them; compile;
  smoke; **live diag audit** (validate-before-merge per §7 of the
  self-auditing playbook); cold-eye Q1–Q4; roadmap + §13 impl log;
  commit + merge to main + push.

### 13.6 Performance

None. No new per-frame/per-turn path: `lava` reuses the
already-shipped `OnTurnStart` `PerTurnDamage` tick (same as acid);
the rest are pure data read in the existing `OnBeforeTakeDamage`/
`OnApply` paths. No new caches, MonoBehaviours, or event listeners.

### 13.7 Pre-flagged self-review

- **🟡 lava PerTurnDamage scalar vs Qud temperature sim** — same
  acknowledged divergence as acid; document, don't port the temp sim
  (LQ.8-class). Pin lava's Heat-tick with a test.
- **🔵 sap/honey use StatModifiers for "sticky" because `Sticky` is
  un-wired** — deliberate (matches pitch). Note in §13 impl log so a
  future reader doesn't "fix" it by wiring Sticky and double-applying.
- **⚪ gel `Slippery:true` is cosmetic today** — carried for data-
  shape stability; documented, not a gap.
- **🧪 RED discipline** — content RED is a real on-disk file-load
  failure before the JSON exists (compile-able, observable), not a
  compressed step.

### 13.8 Implementation log (LX.1–LX.3) — incl. a hard cold-eye finding

**LX.1** (`b5de653`) plan+sweep. **LX.2** (`2266503`) 4 JSON files +
`LiquidExpansionContentTests` (4 content-shape RED→GREEN confirmed
RED on disk before files existed; 8 behavior/counter/adversarial
pins). 101/101 liquid regression.

**LX.3 — the honest part.** Adding the 4 liquids to the
self-auditing bench and running the validate-before-merge live audit
exposed **two latent bugs in the bench itself, and a hard truth about
every prior "conclusive" matrix in this system's history:**

1. **Bench `RunMatrixAudit` had never produced valid data.** It runs
   synchronously in scenario `Apply()`, which executes *before*
   GameBootstrap Step-1b' finishes loading `LiquidDefinitions`. With
   the registry uninitialized, every coat's `OnApply`/
   `OnBeforeTakeDamage` early-returns → the whole matrix records a
   phantom **×1.00**, indistinguishable from "no interaction" (a
   textbook Rule-4 violation *inside the Rule-4 exemplar*). Every
   earlier "matrix table" reported as validation (incl. v3.1's
   "conclusive" 6-liquid table) was actually **stale persisted-buffer
   data from earlier *manual-cast* sessions** (this project has
   domain-reload-on-play off, so `Diag`'s static buffer survives
   Play→Edit→Play). The mechanic was *always* correct — proven
   repeatedly by *direct* `execute_code` snapshot→ApplyDamage→measure
   on the live dummies (water 200, lava 190/125, gel 200, sap 135,
   honey 130, ichor 120 — all exact). My "the bench proves it"
   claims were not. This is the canonical "tests-green-feels-clean
   is where latent bugs hide" lesson; it was only exposed because the
   Rule-8 `runId` scoping forced the first *clean* read.
2. **Persisted-buffer staleness** (the Rule-8 gap): a reader deduping
   by `(liquid,element)` silently shows last-session numbers.

**Fixes shipped in LX.3:**
- `EnsureLiquidRegistry()` at the **top of `Apply()`** (mirrors
  GameBootstrap Step-1b' `Resources.LoadAll`) so the registry is live
  *before any dummy is coated* — bootstrap-order-independent.
- Loud Rule-4 abort in `RunMatrixAudit`: if the registry is still
  unavailable, emit `MatrixAuditSkipped(reason=registry_unavailable)`
  and run **nothing** — never phantom ×1.00.
- Rule-8 `runId` GUID on every `MatrixAudit` cell + a
  `MatrixAuditRun` marker; the audit query scopes to the newest run.
- Display rounding nit fixed (`(int)(1.9f*100)`=189 → `Math.Round`).
- Methodology hardened: `Docs/MCP_PlayMode_Testing_Strategy.md`
  Rule 8 (+ its "cross-check against direct `execute_code`
  measurement" corollary) and the CLAUDE.md always-on mirror.

**Final clean verification (runId-scoped, registry-ensured,
cross-checked vs direct measurement — the real one):**
`cells=40/40`; dry 100×4; water 60/200; oil 145; pitch 145;
brine 85/230; ichor Cold 120; **lava 125/190; gel 200; sap 135;
honey 130** — every cell exactly the spec.

**Cold-eye Q1–Q4:** Q1 ✓ (JSON mirror the brine template;
EnsureLiquidRegistry mirrors GameBootstrap; runId mirrors the
Skipped record). Q2 ✓ (all payloads carry `runId`; Hint() covers
every interacting cell). Q3 ✓ (content counter-checks + the `dry`
control row). **Q4 → fixed** (the §13.4 "sap/honey ≈1.0" doc error,
corrected above with the honesty trail). No 🔴/🟡 remain in the
*mechanic*; the bench bugs are fixed and re-verified clean.

**Honesty bound:** the LX *liquids* are conclusively correct (direct
measurement + the now-trustworthy bench agree). The sobering part is
that the self-auditing bench — the very instrument built to make
these audits trustworthy — was itself silently broken until this
milestone; it is the strongest possible argument for Rule 8's
direct-measurement cross-check, and that corollary is now codified.

---

## 14. LL — Lore-grounded liquids (tepui-thread canon)

**Status:** PLANNED (LL.1). Five new liquids drawn from the
`claude/ideas-gin-frogs` lore strand (the **Felled Tree** cosmology),
each anchored to a specific faction / cosmological function /
preservation-method, shipped JSON-only through the LQ.5/6 engine and
auto-verified by the v3.2 self-auditing bench.

> **Lore sources (cited, branch `origin/claude/ideas-gin-frogs`):**
> `Lore/00_Canon.md` §Layer 4 (factions), §Layer 8 (preservation/
> memory magic), §Layer 9 (symbolism: Ink/Salt/Dew/Mucilage/Honey/
> Light/Bone/Root); `Lore/01_Spine.md` §II (the seven cosmological
> functions — Memory/Substrate/Preservation/Beauty/Exchange/Roots +
> the empty seventh, Naming/Urqu); `IDEAS.md` "The Preserved" (five
> preservation methods + iron-gall ink corrosiveness), "Bioluminescent
> Catacomb Economy" (luminous slime), `sarisarinama_drosera_design.md`
> (Drosera mucilage). No new lore is invented here — these are
> *mechanical readings of existing canon.*

### 14.1 Goal & scope

Add 5 liquids using only the **wired** LiquidDefinition knobs
(Conductivity, Combustibility, FireDampen, PerTurnDamage,
StatModifiers, ResistanceModifiers, Fluidity, Evaporativity) — zero
new C#, exactly the LX pattern. Each liquid's *richer* lore special-
feature (true light emission, a preservation system, FollowOnEffect
statuses) is **explicitly scope-pruned to a documented ⚪ follow-up**
(§14.4) — the wired knobs deliver a faithful *gameplay shadow* of the
lore today, the same way pitch/sap deliver "sticky" via StatModifiers
because `Sticky` is unwired.

### 14.2 The five liquids (lore → mechanic)

| id | Lore anchor (cited) | Glyph/Color | Wired knobs (the mechanic) | Special feature it *evokes* |
|---|---|---|---|---|
| `iron-gall-ink` | Palimpsest/Inkbound; the **Ink-Bathed** preservation method; "iron-gall ink penetrates every tissue… **slowly corrosive** (per Black-Gall)"; the rental-currency Ink shares the iron-gall source (IDEAS "The Preserved"; Canon §L9 Ink). | `~` `&K` | `Conductivity 60` (iron-salt solution conducts — lore-true), `PerTurnDamage {2,"Acid"}` (slow Black-Gall corrosion), `Fluidity 8`, `Evaporativity 4` | Memory/"the body becomes readable" — the Palimpsest reading-mechanic is the deferred feature; the corrosive conductive black coat is today's shadow. |
| `sundew-mucilage` | Drosera carnivorous flora; the **Dew/Mucilage** motif ("glistening trap-as-life", "the patient predator"); the Resin-Cast preservation analog (`sarisarinama_drosera_design.md`; Canon §L9 Dew/Mucilage). | `~` `&G` | `StatModifiers Agility -4, DV -5` (the game's strongest entrapment — beats honey/sap), `Sticky true`, `Combustibility 20`, `Fluidity 2`, `Evaporativity 1` (viscous, persists) | Escalating root-you-in-place entrapment (deferred); today: the hardest stat-slow, net-zero on removal. |
| `choir-wort` | Rot Choir; **external digestion**, "the wet rustle", spore-borne substrate-eating (Canon §L4 Choir, §L9 Tendril). | `~` `&g` | `PerTurnDamage {4,"Acid"}` (being externally digested), `StatModifiers Toughness -3` (the digestion weakens), `Combustibility 10` | "Un-preservation" / Choir-Touched status (deferred FollowOnEffect); today: a digesting coat that saps Toughness. |
| `lumen-slime` | Bioluminescent Catacomb Economy — "luminous slime… applied as paint… glows"; the catacomb light medium; Urqu's opposite; Pale-Curation UV hazard (IDEAS "Bioluminescent…"; Canon §L9 Light). | `~` `&C` | `Conductivity 40` (wet bio-film), `StatModifiers DV -3` (you glow → a beacon, easier to hit in the dark — the CoO-tuned reading) | True light emission + the preservation-UV-degrade interaction (deferred — needs a LightSource/preservation hook); today: conductive glow-beacon. |
| `bog-mire` | The **Bog-Taken** preservation method; the bog biome ("anaerobic peat, high tannic acid", brown, centuries-deep visible cemetery) (IDEAS "The Preserved"; Canon §L2 Bog, §L6 burial). | `~` `&y` | `FireDampen 50` (waterlogged peat smothers fire — 2nd-strongest after water), `PerTurnDamage {1,"Acid"}` (tannic sting), `StatModifiers Agility -2` (wading the mire), `Conductivity 20` | Passive centuries-preservation (deferred); today: a fire-smothering, faintly-tannic, slowing coat. |

All `PerTurnDamage.Type="Acid"` → routes through the proven
`IsAcidDamage` flag + `AcidResistance` (acid.json precedent). No Id
collisions (verified vs the 10 existing). Glyph/Color follow Canon
§L9 color symbolism (Ink=black `&K`, dew=pale green `&G`, Choir=
green-violet `&g`, bio-light=cool cyan `&C`, bog=brown `&y`).

### 14.3 Verification sweep

| Premise | Status |
|---|---|
| Only Conductivity/Combustibility/FireDampen/PerTurnDamage/StatModifiers/ResistanceModifiers/Fluidity/Evaporativity are engine-wired | ✅ re-confirmed (LX grep of `LiquidCoveredEffect.cs`) |
| `PerTurnDamage.Type="Acid"` valid (flag-mapped, AcidResistance) | ✅ acid.json precedent + `Damage.GetFlagForAttribute` |
| No Id collision (5 new vs 10 existing) | ✅ verified (`ls LiquidDefinitions/`) |
| GameBootstrap Step-1b' auto-loads new `*.json` | ✅ LQ.7; bench also self-ensures (v3.2 `EnsureLiquidRegistry`) |
| Self-auditing bench auto-audits new rig rows; runId-scoped read mandatory (Rule 8) | ✅ v3.2; LL.3 follows the codified procedure |
| Lore citations resolve on `origin/claude/ideas-gin-frogs` | ✅ files read this session |

### 14.4 Scope-prune — deferred lore special-features (⚪, documented)

Each is the liquid's *richest* lore feature, deliberately NOT in this
JSON-only ship (needs new C#/systems; tracked, not lost):
- `iron-gall-ink`: the Palimpsest "Ink-Bathed body is *readable*"
  memory-archaeology mechanic → needs the memory/preservation system.
- `sundew-mucilage`: escalating *root-in-place* entrapment (can't
  move at all past a threshold) → needs a movement-lock hook
  (`AllowMovement` exists on `Effect`; a future LL.x).
- `choir-wort`: "Choir-Touched" status + literal un-preservation →
  needs FollowOnEffect (the ⚪-deferred LQ.5 hook) + preservation sys.
- `lumen-slime`: true light emission (catacomb economy) + UV-degrades-
  preserved-material → needs a LightSource/preservation hook.
- `bog-mire`: passive centuries-preservation of the *coated entity* →
  needs the preservation system.

### 14.5 Sub-milestones

- **LL.1** — this plan + sweep (doc commit).
- **LL.2** — 5 JSON (+`.meta`) + `LiquidLoreContentTests.cs`: RED
  (disk-load knob asserts, fails before files) → GREEN; behavior
  pins + counter-checks (ink corrosive-tick + conductive; sundew
  −4 Agi/−5 DV net-zero on removal + counter no-Electric; choir-wort
  Acid-tick + −3 Tough; lumen Electric-amp + −3 DV; bog FireDampen
  reduces Heat + tannic tick + −2 Agi); adversarial (absent-stat
  skip, registry-reset-mid-coat net-zero). RED→GREEN, regression,
  commit.
- **LL.3** — add 5 to `LiquidSpellTestBench` rig + extend ClearCell
  corridor; compile; smoke; live **runId-scoped** matrix + a direct
  `execute_code` cross-check (Rule 8 corollary — the LX.3 lesson);
  cold-eye Q1–Q4; §14 impl log + roadmap; commit + merge to main.

### 14.6 Performance

None. `choir-wort`/`ink`/`bog` reuse the shipped `OnTurnStart`
PerTurnDamage tick (acid path); everything else is data read in the
existing `OnBeforeTakeDamage`/`OnApply`. No new caches/MonoBehaviours/
listeners.

### 14.7 Pre-flagged self-review

- **🟡 `lumen-slime` −DV is a CoO-tuned reading, not literal lore.**
  Lore says lumen-slime *glows*; we have no light/stealth stat, so
  "you're a lit beacon → −DV (easier to hit)" is the wired shadow.
  Document the interpretive leap in the §14 impl log so it isn't
  mistaken for canon; the true light-emission is the ⚪ deferral.
- **🟡 `choir-wort` Acid-typed digestion** — the Choir digests
  enzymatically, not "acid"; we reuse the Acid flag because it's the
  only wired damage-type that routes through a resistance and matches
  "dissolving you". Documented divergence (mirrors how we model
  acid/lava ticks). Not Choir-Touched (that's the ⚪ FollowOnEffect).
- **🔵 `sundew-mucilage` −4/−5 is the strongest stat-debuff shipped.**
  Intentional (it's THE trap). Flag for playtest balance; per-liquid
  StatModifiers make retuning a JSON edit.
- **🧪 RED discipline** — content RED is a real on-disk file-load
  failure before the JSON exists (compile-able, observable).
- **⚪ Five deferred special-features** (§14.4) — tracked, not lost.

### 14.8 Implementation log (LL.1–LL.3)

**LL.1** (`5333e50`) plan+sweep. **LL.2** (`dcfaddb`) 5 JSON +
`LiquidLoreContentTests` (5 content-shape RED→GREEN confirmed RED on
disk before files; 7 behavior/counter/adversarial pins). 89/89.
**LL.3** rig + live audit + this log.

**Q4 doc-vs-impl correction (the §13.4-style honesty trail).**
§14.2 (written in LL.1) listed `lumen-slime` `Conductivity 40` and
`bog-mire` `Conductivity 20`. `CONDUCTIVITY_AMPLIFY_THRESHOLD = 50`,
so 40/20 would be **inert data** (a looks-like-a-knob-does-nothing
Rule-4 hygiene violation). **Shipped both at `Conductivity 0`** —
lumen's mechanic is the −3 DV glow-beacon; bog's is FireDampen 50 +
tannic tick + −2 Agi. The §14.2 table values above are the *original
plan*; the *shipped* values are Conductivity 0 for both. Caught by
the LL.2 pre-impl sweep, recorded here per Q4.

**All-100%-by-design caveat (Rule 4, the LX.3 lesson applied).**
The single-hit matrix only re-weights damage. `sundew-mucilage`,
`choir-wort`, and `lumen-slime` are **stat/tick liquids** — their
mechanics are StatModifiers (−Agi/−DV/−Tough) and OnTurnStart
PerTurnDamage, which the single-hit matrix *structurally cannot
show*. Their matrix rows are a legitimate, **documented** ×1.00
(the bench Hint() lines say "1.00 by design — effect is X
(StatMod/OnTurnStart)") — NOT the LX.3 phantom-100 bug. Verified by
the Rule-8 direct cross-check below, not by the matrix alone.

**Final verification (runId-scoped + direct cross-check):**
`cells=60/60`, `skipped=0`. Matrix-visible re-weights exact:
`iron-gall-ink` Electric ×1.60 (Cond 60), `bog-mire` Heat ×0.50
(FireDampen 50); existing 10 unchanged. Direct `execute_code`
measurement on the live dummies confirmed the non-matrix mechanics
exact: sundew −4 Agi/−5 DV; choir-wort −3 Tough + 4/turn Acid tick;
lumen −3 DV; iron-gall-ink 2/turn tick; bog-mire −2 Agi + 1/turn
tick. All net-zero on removal (LiquidLoreContentTests pins).

**Cold-eye Q1–Q4:** Q1 ✓ (JSON mirror brine/lava template; rig +
Hint mirror LX). Q2 ✓ (identical schema; Acid-type + StatModifiers
consistent with shipped liquids; runId on all payloads). Q3 ✓
(content counter-checks + the dry control row + the direct
cross-check). **Q4 → fixed** (the lumen/bog Conductivity correction +
the all-100%-by-design caveat, both recorded above). No 🔴/🟡 in the
*mechanic*; the two 🟡 pre-flags (lumen −DV is a CoO-tuned reading;
choir-wort uses Acid for enzymatic digestion) are documented
interpretive choices, not defects.

**Honesty bound:** the 5 liquids are conclusively correct (matrix +
direct measurement agree). The lore *special-features* (§14.4) are
NOT shipped — these are the wired *gameplay shadows* of the canon,
faithful within the wired-knob model, with the richer features
tracked as ⚪ follow-ups.
