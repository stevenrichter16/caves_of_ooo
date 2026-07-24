# Crops & the Watering Grimoire

> Living plan + implementation log. Feature: plantable crops that grow
> over ticks while watered, and a "watering grimoire" that teaches the
> Conjure Rain spell — rain falls over nearby crop tiles, watering them
> and visibly darkening the soil until the moisture dries out.

**Status:** 📋 IN PROGRESS.
**Origin:** user directive 2026-07-23 — "plan a feature where you can
plant crops and water them with a 'watering grimoire', where it spawns
rain above the crop tiles and waters them and darkens the dirt. after
planning, implement fully without my intervention."
**Genre note (PROJECT-IDENTITY):** RPG framing — crops persist across
save/load and sessions; a field you plant today is still growing when
you come back. Dry crops pause, they don't die.

---

## 0. Verification sweep — references read before planning

All read directly this session (my own reads; a 5-agent research pass
ran in parallel as cross-check — its findings agreed and added the
corrections table below):

| # | Fact | Verified against |
|---|---|---|
| 1 | Floor tiles ARE entities: `Grass`/`Floor`/`StoneFloor` inherit blueprint `Terrain` (RenderLayer 0, tag `Terrain`), placed at gen time via EntityFactory | `Objects.json:303-326, 1307-1320` |
| 2 | Renderer paints ONLY the top visible entity per cell (`Cell.GetTopVisibleObject`) and fires the `Render` event only on it; the event mediates `ColorString`/`DetailColor` ONLY. `BackgroundColor` is read directly off the top entity's RenderPart and painted as a solid block behind the glyph, auto-darkened via `QudColorParser.DarkenForBackground` | `ZoneRenderer.cs:840-944` |
| 3 | `ZoneRenderHooks.MarkCellDirty(x,y,source)` + `(cell,source)` overloads; callers must mark dirty themselves (AddEntity/RemoveEntity don't) | `ZoneRenderHooks.cs:42,60`; call sites `CombatSystem.cs:1090`, `GasSystem.cs:304` |
| 4 | `AsciiFxBus.EmitParticle(zone,x,y,glyph,colorString,lifetime,dy,moveInterval,delay)` supports moving particles (floating numbers use `dy:-1`; rain = `dy:+1`). `AsciiFxTheme.Water` exists. Bus is pure C# enqueue → EditMode-safe; `Drain()` lets tests assert requests | `AsciiFxBus.cs:348-398, 24, 425-431` |
| 5 | Non-creature per-turn ticking: Part-on-World routing `TickEnd` → static system (`GasSystemPart` → `GasSystem.OnTickEnd(SettlementRuntime.ActiveZone)`), attached at `GameBootstrap.cs:244`. `TickEnd` fires ONCE PER ACTOR-TURN (N×/round with N actors) — pinned by `TickEndTests.EndTurn_MultipleActors_FiresTickEndEachTime`. Tests drive the static directly (`GasSystemTests.cs:411` loop pattern) | `GasSystemPart.cs:22-31`, `GameBootstrap.cs:229-244`, `TurnManager.cs:389-394`, `TickEndTests.cs:47-64` |
| 6 | Brain-less entities can NOT tick via StatusEffectsPart (turn events fire only on scheduled actors) — central system tick is the only correct growth driver | `StatusEffectsPart.cs:368-396`, `TurnManager.cs:313-356` |
| 7 | **Grimoires already exist as a system**: `GrimoirePart` "Read" action teaches a mutation via `MutationClassName`/`MutationLevel`, item NOT consumed; "Grimoire Spells" is an established ability category with water-themed spells | `GrimoirePart.cs` (read in full) |
| 8 | Self-centered radius spell template: `DryingBreezeMutation` — `AddMyActivatedAbility(name, COMMAND, "Grimoire Spells", AbilityTargetingMode.SelfCentered, RADIUS)`, command event carries `Zone`+`SourceCell`, Chebyshev-clamped double loop, `CooldownMyActivatedAbility(id, N)`. Turn cost handled by `ResolveAbilityCommand` (InputHandler.cs:2991) — free | `DryingBreezeMutation.cs` (read in full) |
| 9 | Runtime blueprint spawning from mutations/parts: `MaterialReactionResolver.Factory` static (ConjureWaterMutation spawns `WaterPuddle` with it); `CorpsePart.Factory` same convention, set by GameBootstrap; graceful no-op when null in tests | `ConjureWaterMutation.cs:91-98`, `CorpsePart.cs:49-55` |
| 10 | No existing crop/farming system: `StoneburrSeed` is an alchemy reagent; `Farmer` is a conversation villager. Clean namespace | `Objects.json:906, 4895`; grep `class.*Crop/Farm/Seed` → zero |
| 11 | Zone iteration: snapshot via `zone.GetEntitiesWithTag(...)` (gas pattern) — never mutate while iterating `GetReadOnlyEntities()` | `GasSystem.cs:70-83`; CLAUDE.md pitfalls |
| 12 | Item consume-on-use is StackerPart-aware (`TonicPart.ConsumeItem`) — copy for seeds | `TonicPart.cs:165-178` |

### Corrections table (research pass vs. original draft)

| # | Original draft assumption | Correction |
|---|---|---|
| C1 | Grimoire = item with a custom "Invoke" inventory action (new `WateringGrimoirePart`) | **Superseded.** Grimoires in this codebase TEACH spells (`GrimoirePart` + "Grimoire Spells" ability category already exist, incl. water spells). The consistent design: WateringGrimoire is pure content (GrimoirePart params) teaching a new `ConjureRainMutation` — hotbar-cast, free turn-cost + cooldown via the ability pipeline, zero UI changes |
| C2 | Darken the TERRAIN entity's ColorString (mutate + restore), tracked by a `WateredSoilPart` | **Superseded.** The renderer paints only the TOP entity — terrain recolor is invisible under a crop. The wet-soil look rides on the CROP entity's `RenderPart.BackgroundColor` (solid block behind the glyph, auto-darkened by the renderer = literally darkened soil around the plant). Set on watering, cleared on dry-out; crop blueprints author no bg so "restore" is just empty-string. `WateredSoilPart` is CUT — one fewer Part, no restore bookkeeping. Known simplification: after maturity converts the crop to produce, the bare tile shows dry immediately |
| C3 | 3 visible growth stages (seed/sprout/mature) with a separate harvest step | **Simplified.** Stages 0 (seed) and 1 (sprout); completing the sprout stage REPLACES the crop with its produce item(s) lying in the cell — the produce on the ground IS the visible "ready" state, and the existing ground-pickup flow closes the loop. No new harvest UX |
| C4 | Growth thresholds "in turns" | Tick-denominated (`TickEnd` fires per actor-turn). Same convention gas uses; documented, tests drive `CropSystem.OnTickEnd` directly |

---

## 1. Scope

### In scope
1. **Plant** — seed items with a "Plant" inventory action (TonicPart
   event shape): plants a crop entity into the actor's cell if it has
   `Plantable`-tagged terrain and no existing crop. Consumes one seed
   (stack-aware). Rejects with reasons (diag-pinned).
2. **Grow** — crops advance seed → sprout → produce over ticks, ONLY
   while `MoistureTicks > 0`. Dry = paused, never dies.
3. **Water via Conjure Rain** — `WateringGrimoire` item (GrimoirePart)
   teaches `ConjureRainMutation` ("Grimoire Spells", SelfCentered,
   radius 3, cooldown 5). Casting waters every crop in radius (moisture
   top-up, not additive), spawns falling rain particles above each
   watered crop tile + a Water-theme splash, and darkens the soil
   (crop bg block) until the moisture runs out.
4. **Yield** — completing the sprout stage replaces the crop with
   `YieldCount` × `YieldBlueprint` produce items in the cell.
5. **Content** — 2 data-driven crop lines (CandyCarrot: fast, 1
   produce; Emberwheat: slow, 2 produce), `Plantable` tag on `Grass`,
   `WateringGrimoire`, produce items, farming starter kit at bootstrap.
6. **Observability** — new `crop` diag category in
   `Diag.DefaultOnCategories`; records on every gate.
7. **Save/load** — public-fields-only Parts (Tier-3 reflection);
   round-trip pins for mid-growth, mid-moisture, wet-bg state.
8. **Showcase scenario** — manual playtest for the visual half (rain
   motion, soil darkening) that EditMode cannot verify.

### Explicitly out of scope (v1)
- Standing "mature plant + Harvest world-action" UX (C3 covers v1).
- Crop death/withering; fertilizer/soil quality/seasons/weather system.
- NPC farmers tending crops.
- Non-grimoire water sources (buckets, puddles, standing rain).
- Tilling (any `Plantable` terrain accepts seeds directly).
- Watering non-crop tiles (rain targets crop cells only, per the ask).

---

## 2. Design

### 2.1 `CropPart` (new; public fields only → auto save/load)
- `int GrowthStage` — 0 seed, 1 sprout.
- `int TicksInStage`, `int TicksPerStage` (CandyCarrot 20, Emberwheat 35).
- `int MoistureTicks` — growth advances + decrements only while > 0.
- `string StageGlyphsRaw` — CSV per stage, e.g. `".,τ"`.
- `string StageColorsRaw` — CSV per stage, e.g. `"&w,&g"`.
- `string YieldBlueprint`, `int YieldCount`.
- `public void Water(int ticks)` — `MoistureTicks = max(MoistureTicks,
  ticks)`; sets own `RenderPart.BackgroundColor = WET_SOIL_BG` (`"^w"`);
  `MarkCellDirty`. (Idempotent re-watering; top-up semantics.)
- Dry-out (moisture hits 0, done by the system tick): clears
  `BackgroundColor`, `MarkCellDirty`.

### 2.2 `SeedPart` (new; TonicPart event shape)
- `string CropBlueprint`; `public static EntityFactory Factory`
  (CorpsePart convention).
- `GetInventoryActions` → `("Plant", "plant", "PlantSeed", 'p', 20)`.
- `InventoryAction` "PlantSeed" gates, each with diag reject reason:
  zone/position resolvable (`no_zone`); cell has `Terrain`-tagged
  entity that also has tag `Plantable` (`not_plantable`); no entity
  with `CropPart` already in cell (`already_planted`); Factory +
  blueprint resolve (`no_factory`). Success: spawn crop (stage 0, dry),
  consume one seed (StackerPart-aware), message + diag `CropPlanted`,
  `MarkCellDirty`.

### 2.3 `CropSystem` (new static) + `CropSystemPart` (GasSystemPart clone)
`CropSystem.OnTickEnd(zone)`: null-guard; snapshot
`zone.GetEntitiesWithTag("Crop")`; per crop with `MoistureTicks > 0`:
- `MoistureTicks--`; `TicksInStage++`.
- If moisture just hit 0 → clear wet bg + `MarkCellDirty` + diag
  `SoilDried`.
- If `TicksInStage >= TicksPerStage`:
  - stage 0 → 1: swap glyph/color from CSVs, reset `TicksInStage`,
    `MarkCellDirty`, diag `StageAdvanced`.
  - stage 1 complete: spawn `YieldCount` × `YieldBlueprint` via
    `CropSystem.Factory` into the cell, remove crop, `MarkCellDirty`,
    message, diag `CropMatured`. Factory null → crop stays at
    stage-1-complete (graceful; retried next tick), diag `no_factory`.
Moisture bookkeeping lives ONLY here — no double-decrement paths.

### 2.4 `ConjureRainMutation` (new; DryingBreezeMutation shape)
- `COMMAND "CommandConjureRain"`, `COOLDOWN 5`, `RADIUS 3`,
  `MOISTURE_TICKS 40`.
- `Mutate`: `AddMyActivatedAbility(DisplayName, COMMAND,
  "Grimoire Spells", AbilityTargetingMode.SelfCentered, RADIUS)`.
- `Cast(zone, sourceCell)`: Chebyshev radius-3 clamped double loop; per
  cell, find entity with `CropPart` → `crop.Water(MOISTURE_TICKS)` +
  rain FX for that cell. Always succeeds + cooldown (DryingBreeze
  convention); message varies: crops watered → "Rain patters down over
  your crops." else "The conjured rain finds no crops to nourish."
  Diag `RainConjured {cropsWatered, radius}` + per-crop `CropWatered`.
- Rain FX per watered cell (EditMode-safe enqueues):
  - 3 falling drops: `(x, y-2)` delay 0, `(x, y-1)` delay 0.12,
    `(x, y-2)` delay 0.24 — glyphs `'|'`, `'''`, `'.'`, colors
    `&B`/`&b`, `dy:+1`, `moveInterval 0.1`, `lifetime 0.45`.
  - `EmitBurst(zone, x, y, AsciiFxTheme.Water, false, delay 0.3)`.

### 2.5 Content (Objects.json)
- `Grass` gains tag `Plantable` (only change to existing content).
- `CandyCarrotSeed`/`EmberwheatSeed` — Items with `SeedPart` + Stacker.
- `CandyCarrotCrop` — `Terrain`-independent entity: RenderLayer 1,
  non-solid, NOT takeable, tag `Crop`; CropPart params (20/`".,τ"`/
  `"&w,&g"`/`CandyCarrot`×1).
- `EmberwheatCrop` — 35/`".,ι"`/`"&w,&y"`/`Emberwheat`×2.
- `CandyCarrot`/`Emberwheat` — produce Items (Takeable, Commerce,
  flavor).
- `WateringGrimoire` — Item, glyph `"` color `&B`, `GrimoirePart`
  params: `MutationClassName ConjureRainMutation`, `MutationLevel 1`,
  LearnMessage. Commerce ~30.
- Mutation registration: wherever mutations map name→type (check
  `MutationsPart.AddMutation` resolution — likely reflection by class
  name; verify in SM3).

### 2.6 Bootstrap wiring (SM4)
- `_world.AddPart(new CropSystemPart())` beside GasSystemPart
  (GameBootstrap.cs:244) — AND check the load path (`ApplyLoadedGame`,
  ~line 730): mirror exactly what GasSystemPart does on load.
- `SeedPart.Factory = _factory; CropSystem.Factory = _factory;` beside
  the existing Factory-static assignments.
- `GivePlayerFarmingStarterKit()` (grimoire + 4×CandyCarrotSeed +
  2×EmberwheatSeed) beside `GivePlayerCraftingStarterKit`.

### 2.7 Observability
`crop` category added to `Diag.DefaultOnCategories`. Records:
`CropPlanted`, `PlantRejected{reason}`, `RainConjured{cropsWatered}`,
`CropWatered{moistureTicks}`, `StageAdvanced{stage}`,
`CropMatured{yieldBlueprint,yieldCount}`, `SoilDried`. All test-pinned.

### 2.8 Performance (per-turn path checklist)
- `OnTickEnd` mirrors gas exactly (same snapshot allocation profile,
  same per-actor-turn cadence) — established precedent.
- `MarkCellDirty` per-cell, only on actual visual transitions.
- FX requests pooled (AsciiFxBus Rent/Release).

### 2.9 Honesty bounds
- EditMode CAN verify: every state transition, diag record, bg/glyph/
  color string values, FX requests (`AsciiFxBus.Drain()`).
- EditMode CANNOT verify: rain motion on screen, whether `^w` bg reads
  as "wet earth", glyph readability → showcase scenario + manual
  playtest.

---

## 3. Sub-milestones (smallest blast radius first)

| # | Scope | Status |
|---|---|---|
| SM1 | Content blueprints + `crop` diag category + `CropPart` + `SeedPart` + planting flow | ✅ 2026-07-23 |
| SM2 | `CropSystem` + `CropSystemPart`: moisture-gated growth, stage swap, dry-out bg clear, maturity produce-replace | ⏳ |
| SM3 | `ConjureRainMutation` + `WateringGrimoire` content + rain FX + watering/darkening | ⏳ |
| SM4 | Bootstrap wiring + starter kit + save/load round-trip pins + showcase scenario + smoke test | ⏳ |
| SM5 | Adversarial sweep (dedicated file — CSV parser malformed inputs, top-up stacking semantics, save/load reach, boundary radius, diag contracts, Factory-null paths) + cold-eye review + close-out | ⏳ |

## 4. Test plan sketch
- Plant on Plantable grass succeeds / on plain `Floor` rejects
  `not_plantable` (counter-pair); double-plant rejects
  `already_planted`; stack decrements by exactly 1; last seed removed.
- Dry crop: N ticks → zero progress (counter-check). Watered crop:
  advances at exactly the threshold tick, not before.
- Moisture top-up: watering at 10 remaining sets 40, not 50.
- Dry-out at 0 clears bg exactly once + diag `SoilDried`.
- Sprout completion replaces crop with exactly YieldCount produce;
  crop entity gone from zone.
- Conjure Rain: crops at Chebyshev 3 watered, at 4 not (counter-pair);
  no-crop cast → cooldown still applied, `cropsWatered=0`.
- FX: `Drain()` yields dy=+1 particles + Water burst per watered cell;
  zero FX on no-crop cast.
- Save/load: mid-growth + mid-moisture + wet-bg round-trip via
  `PartRoundTripHelper`.
- All diag emissions + reject reasons pinned.

## 5. Implementation log

### SM1 — content + planting (shipped 2026-07-23)
- New: `Assets/Scripts/Gameplay/Farming/CropPart.cs` (state-only Part:
  stages/moisture/CSV lookups, `Water()` top-up + wet-bg,
  `OnDriedOut()` bg-clear — no self-ticking, CropSystem owns the tick),
  `SeedPart.cs` (Plant action, 5 reject gates with diag reasons,
  StackerPart-aware consume, static Factory convention).
- Content: `Plantable` tag on Grass; CandyCarrotSeed/EmberwheatSeed,
  CandyCarrotCrop/EmberwheatCrop (tag `Crop`, RenderLayer 1, stage
  CSVs use plain-ASCII sprout glyphs 't'/'i' — CP437 Greek was an
  avoidable atlas-miss risk), CandyCarrot/Emberwheat produce.
- `crop` appended to `Diag.DefaultOnCategories`.
- Tests: `CropPlantingTests.cs` — 15 tests: content sanity (blueprints
  resolve, params land via reflection, Grass plantable / Floor NOT —
  counter-pair), action offer, success path (spawn+consume+diag),
  all 5 reject gates incl. graceful no-factory and unknown-blueprint
  (with LogAssert for EntityFactory's error log), stack decrement,
  Water top-up semantics, wet-bg set/clear. 15/15 GREEN.
- Sequencing disclosure: parts were written immediately before their
  tests in the same pass (not strict RED-first); every reject gate and
  the top-up semantics were verified by real Unity runs before commit.
  One real harness catch during the run: Unity fails tests on
  unexpected [Error] logs — the unknown-blueprint test now Expects it.
