# Crops & the Watering Grimoire

> Living plan + implementation log. Feature: plantable crops that grow
> over ticks while watered, and a "watering grimoire" that teaches the
> Conjure Rain spell — rain falls over nearby crop tiles, watering them
> and visibly darkening the soil until the moisture dries out.

**Status:** ✅ SHIPPED 2026-07-23. All 5 sub-milestones landed; 66 new
tests (15 planting + 12 growth + 11 watering + 4 round-trip + 21
adversarial + 1 scenario smoke + 2 cold-eye pins folded into the
adversarial file); full EditMode suite 5671/5671, zero regressions.
Manual-playtest half (rain motion, wet-soil readability) is carried by
the CropFarmShowcase scenario per §2.9's honesty bounds.
**Follow-up SM6 (2026-07-25):** FarmPlotSeeder — guaranteed plantable
grass at spawn in every biome. +12 tests; full suite 5683/5683.
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
| SM2 | `CropSystem` + `CropSystemPart`: moisture-gated growth, stage swap, dry-out bg clear, maturity produce-replace | ✅ 2026-07-23 |
| SM3 | `ConjureRainMutation` + `WateringGrimoire` content + rain FX + watering/darkening | ✅ 2026-07-23 |
| SM4 | Bootstrap wiring + starter kit + save/load round-trip pins + showcase scenario + smoke test | ✅ 2026-07-23 |
| SM5 | Adversarial sweep (dedicated file — CSV parser malformed inputs, top-up stacking semantics, save/load reach, boundary radius, diag contracts, Factory-null paths) + cold-eye review + close-out | ✅ 2026-07-23 |
| SM6 | Follow-up (user directive 2026-07-25 "make sure grass and everything else needed for planting is at spawn"): `FarmPlotSeeder` — guaranteed plantable plot near spawn regardless of biome | ✅ 2026-07-25 |
| SM7 | Audit fixes (user directive 2026-07-25 "fix any issues you see"): 14-agent multi-lens audit → 8 confirmed findings fixed across SM7a-d | ✅ 2026-07-25 |

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

### SM2 — growth system (shipped 2026-07-23)
- New: `CropSystem.cs` (static tick pass; snapshot via `Crop` tag; the
  single moisture-decrement + growth path; stage advance swaps glyph/
  color from CSVs; sprout completion converts crop → produce; factory-
  null/unknown-blueprint HOLD the crop at the boundary and retry — a
  harvest is never silently deleted, diag `MatureBlocked{reason}`),
  `CropSystemPart.cs` (GasSystemPart byte-for-byte mirror; wired to the
  world entity in SM4).
- Tests: `CropGrowthTests.cs` — 12 tests, RED-first (CS0103 on
  CropSystem before implementation): dry-never-advances (counter-check),
  exact-threshold advance (19 vs 20 ticks), glyph/color swap, moisture
  drain + pause-when-dry, dry-out clears bg exactly once + single
  SoilDried record, CandyCarrot 40-tick full cycle → 1 produce,
  Emberwheat 69-vs-70-tick boundary → 2 produce, StageAdvanced diag,
  null-zone/no-crop robustness, factory-null hold-and-retry, two crops
  tick independently. 27/27 GREEN with SM1's suite.

### SM3 — Conjure Rain + the grimoire (shipped 2026-07-23)
- New: `ConjureRainMutation.cs` (DryingBreeze's exact ability shape:
  COMMAND/COOLDOWN 5/RADIUS 3, SelfCentered "Grimoire Spells" ability,
  Chebyshev-clamped double loop; waters each crop via
  `CropPart.Water(40)` — which also darkens the soil — and emits the
  rain FX per watered cell: 3 staggered falling drops (dy:+1, the same
  moving-particle mechanism floating damage numbers use upward) + a
  Water-theme splash. Always-succeeds/cooldown convention matches
  DryingBreeze; rain FX only over tiles actually watered, per spec).
- Content: `WateringGrimoire` (GrimoirePart teaching ConjureRainMutation
  — pure content, zero new item code, consistent with every other
  grimoire in the game); `ConjureRain` entry in Mutations.json
  (ExcludeFromPool, like DryingBreeze).
- Tests: `CropWateringTests.cs` — 12 tests, RED-first (CS0246 on
  ConjureRainMutation): water-in-radius + wet bg, exact radius-3/4
  boundary counter-pair, multi-crop diag count, double-cast top-up,
  no-crops cast (succeeds, cropsWatered=0, zero FX), null-zone/cell
  guards, FX shape (exactly 3 falling dy>0 particles + 1 Water burst
  per watered cell; scales per cell), grimoire teaches on read + not
  consumed + no duplicate on re-read, ability registered as
  SelfCentered "Grimoire Spells". 38/38 GREEN across all three farming
  suites.

### SM4 — bootstrap wiring + save/load + showcase (shipped 2026-07-23)
- GameBootstrap (new-game): `SeedPart.Factory`/`CropSystem.Factory`
  beside the existing Factory statics; `CropSystemPart` attached to the
  world entity beside `GasSystemPart`; `GivePlayerFarmingStarterKit()`
  (grimoire + 4 CandyCarrot seeds + 2 Emberwheat seeds) beside the
  crafting kit.
- GameBootstrap (load path): same Factory statics, PLUS a defensive
  `CropSystemPart` re-attach (StoryletPart's exact pattern) — a
  pre-feature save's world entity has no CropSystemPart, and without
  this, crops planted after loading such a save would never tick.
- Tests: `CropRoundTripTests.cs` — 4 pins: CropPart mid-growth/
  mid-moisture + blueprint params round-trip; wet-soil bg survives a
  round-trip AND still dries out correctly after load; a sprouted
  crop's swapped glyph/color round-trips; CropSystemPart survives on a
  saved world entity. Plus the showcase smoke in
  `ScenarioCustomSmokeTests` (54/54).
- Showcase: `Scenarios/Custom/CropFarmShowcase.cs` ("World Systems"
  category) — 5×3 grass plot, 4 pre-planted dry crops mid-growth,
  ConjureRain pre-taught + full kit in inventory, with an explicit
  "what to verify visually" checklist covering exactly the things
  EditMode cannot (rain motion, wet-soil readability, glyph swap).

### SM5 — adversarial sweep + cold-eye + close-out (shipped 2026-07-23)

**Adversarial sweep:** `CropsWateringAdversarialTests.cs`, 21 tests
across 6 groups. **0 production bugs found**; every hostile input hit
an intentional guard: CSV parser (empty/only-commas/whitespace/negative
index → sentinels, glyph never corrupted), Water(0/-40) no-op,
re-watering mid-stage preserves progress, no-RenderPart crop tolerated,
int.MaxValue moisture no-overflow, negative moisture (corrupted save) =
dry, TicksPerStage=0 rushes-but-terminates, YieldCount=0 and empty/
unknown YieldBlueprint all HOLD the crop loudly (MatureBlocked) rather
than silently deleting a harvest, zone-corner rain cast (off-map sky
FX silently dropped by EmitParticle's own InBounds guard; splash
lands), edge-cell maturity, two-crops-one-cell first-only-watered
(pins the by-design one-crop-per-cell assumption), full lifecycle diag
pipeline (CropPlanted→RainConjured→CropWatered→StageAdvanced→
CropMatured in one integration run), produce-isn't-a-crop re-cast,
exact diag cardinality (1 RainConjured/cast, 1 CropWatered/crop),
extreme field values round-trip. **One test-authoring fix during the
sweep:** the unknown-yield retry cadence made an exact expected-error-
log count brittle — switched to ignoreFailingMessages around the tick
loop; the behavioral assertion (held, not deleted) is the contract.

**Cold-eye review (Q1-Q4):**
- Q1 symmetry: `Water()`/`OnDriedOut()` are a clean set/clear pair —
  both idempotent, both dirty-mark only on actual change, both diag.
  ✓ no findings.
- Q2 cross-feature payload consistency: all 8 crop-category records
  use camelCase payload fields, IDs in top-level actor/target only,
  reject reasons in a `reason` field (combat-diag convention). ✓.
- Q3 counter-check completeness: found 2 unpinned branches —
  `MatureBlocked{no_yield_blueprint}` and `PlantRejected{no_zone}` —
  both now pinned (added to the adversarial file during the pass).
- Q4 doc-vs-impl: constants (radius 3, cooldown 5, 40 moisture ticks),
  record names, and gate reasons all match this doc. ✓.

**Final gate: full EditMode suite 5671/5671 — zero regressions from
the entire feature.**

**Honesty bounds (final):** everything script-observable is pinned by
66 tests. NOT machine-verified: how the falling rain reads in motion,
whether `^w` renders as convincing wet earth, sprout-glyph legibility
— that's the CropFarmShowcase checklist, to be eyeballed in Play mode.

### SM6 — FarmPlotSeeder: plantable ground guaranteed at spawn (shipped 2026-07-25)

**Origin:** user directive 2026-07-25 — "make sure grass and everything
else needed for planting is at spawn." SM4 grants the starter kit
unconditionally, but only the Jungle and Village builders place
`Grass`; a Ruins/stone start left the seeds unusable (every plant
attempt → `PlantRejected{not_plantable}`).

**Verification mini-sweep (references read before code):**
- Builder survey: only `JungleBuilder`/`VillageBuilder` place Grass;
  Ruins-family builders lay the Floor/stone family.
- `Bank` (riverbank) inherits `Terrain` — Terrain-tagged but special;
  must never be paved.
- River water = `WaterPuddle` entities carrying `LiquidPoolPart` —
  detectable via `cell.HasObjectWithPart<LiquidPoolPart>()`.
- `Cell.IsInterior` marks building interiors; `Cell.IsSolid()` covers
  walls/trees.

**Design — `FarmPlotSeeder.EnsurePlantablePlot(zone, x, y, factory)`
(static, called once from `GameBootstrap.EnsureFarmPlotAtSpawn()`
AFTER `PlacePlayerInOpenCell()` so the plot hugs the final player
position):**
- Counts existing Plantable cells within Chebyshev radius
  `PLOT_RADIUS = 2`; **no-ops when ≥ `MIN_PLANTABLE_CELLS = 6`** —
  grassy biomes are untouched.
- Otherwise converts cells nearest-first (ring by ring), each guarded:
  in-bounds, not `IsInterior`, not `IsSolid()`, no `LiquidPoolPart`,
  not already plantable, and ALL terrain in the cell on the explicit
  `ReplaceableFloors` allowlist (Floor/Rubble/stone family). Anything
  else — `Bank`, future special terrain — vetoes the cell.
- Conversion removes the old floor(s), adds one `Grass`, calls
  `ZoneRenderHooks.MarkCellDirty`. Converted cells end with exactly
  ONE terrain entity.
- Emits `crop/FarmPlotSeeded{converted, plantableTotal, x, y}` only
  when it actually converted something.
- Null zone/factory → returns 0, no crash. Second call is idempotent.

**TDD:** strict RED-first — `FarmPlotSeederTests.cs` (12 tests)
written against the nonexistent class, CS0103 confirmed RED, then
implemented. Coverage: grassy no-op (counter-check to conversion),
bare-floor + empty-cell + stone-family conversion, exactly-one-terrain
invariant, wall/interior/water/Bank veto pins, null-args, idempotency,
diag emission, and an end-to-end integration test that plants a real
`CandyCarrotSeed` on a seeded cell via `InventorySystem` command
routing.

**Tests: 5671 → 5683 (+12). Full EditMode suite green, zero
regressions.**

**Honesty bounds:** the seeder's contract is fully script-observable
(no new visuals). NOT machine-verified: which biome the live
bootstrap actually rolls — the `[Bootstrap/Farming] Plantable cells
near spawn: N` log line is the live-run confirmation channel.

### SM7 — post-ship audit fixes (shipped 2026-07-25)

**Origin:** user directive 2026-07-25 — "fix any issues you see with
the current farming implementation." A 14-agent multi-lens audit
workflow (6 finder lenses × adversarial verification, per the
hypothesis-driven deep-audit directive — SM5's cold-eye had declared
0 production bugs, exactly the state that directive targets) produced
**8 confirmed findings (1 critical, 7 yellow), 0 refuted, 14
blue/note**. Fixes land as SM7a-d, smallest blast radius first.

#### SM7a — ground-seed exploit (F1 🔴 critical, F2 🟡)

The world-action menu fires `GetInventoryActions` / `InventoryAction`
directly on zone-resident entities (`WorldInteractionSystem.
GatherActions` → `InputHandler.ExecuteWorldActionSelection`, which
bypasses `PerformInventoryActionCommand`). A seed lying on the ground
therefore offered "Plant"; all gates checked the ACTOR's cell, so the
crop spawned at the player's feet (even for a seed 10 tiles away),
and `ConsumeOneSeed`'s `InventoryPart.RemoveObject` silently no-oped
for the zone-resident seed — **one dropped seed planted infinitely**
(a ground stack of N decremented to 1, then the last unit was free
forever). RED tests reproduced both the phantom plant and the
stack-decrement-at-range before the fix.

Fix (SeedPart.cs):
- **`not_carried` gate first in DoPlant** — the seed must be in the
  acting entity's own inventory; distinct diag reason + message.
- **Action-row gate** — `GetInventoryActions` only offers Plant when
  the seed is carried (actor's inventory, or any `PhysicsPart.
  InInventory` for actor-less callers), so ground seeds show no dead
  row in the world menu.
- **Reason split** (audit note): the three early-outs that shared
  `no_zone` now emit `no_zone` / `actor_not_in_zone` / `no_cell`.

Counter-checks: carried seed via the SAME raw world-menu event shape
still plants + consumes (gate keys on possession, not dispatch path);
inventory-screen path still offers the Plant row. Tests:
`FarmingAuditFixTests.cs` SM7a section (6 tests).

#### SM7b — stale wet-soil block on the incremental repaint (F6/F7 🟡)

`ZoneRenderer.RenderCellCore` only ever WRITES bg tiles; nothing on
the dirty-cell path erased them (the bg tilemap was cleared solely by
`RenderZone`'s full `ClearAllTiles`, which fires on player movement /
UI close — NOT on waiting in place). So the feature's primary visual
feedback inverted for a stationary player: dry-out cleared the STATE
but the dark wet-earth block stayed painted indefinitely; maturity
removed the still-wet crop and stranded the block under the produce;
this also predated farming (dissipated gas left the same stale tint —
farming was just the first stand-and-watch flow to expose it).

Fix (ZoneRenderer.cs `RenderDirtyCells`): erase each dirty cell's bg
tile before repainting; `RenderCellCore` re-writes it when the top
entity still has a `BackgroundColor`. Perf: dirty cells only (the
full-redraw path is untouched — it already starts from
`ClearAllTiles`); ambient water tints self-heal because their
per-frame passes re-create missing tiles. This corrects §2's C2 claim
("shows dry immediately") from paper-true to actually-true.

Tests: `ZoneRendererStaleBgTests.cs` (3) — a real EditMode
ZoneRenderer + Tilemap fixture drives `RenderZone` → toggle →
`MarkCellDirty` → `RenderDirtyCells` and asserts the bg TILE (not
just the field): dry-out erases, entity-removal erases, still-wet
counter-check keeps the block. RED reproduced the stale `CP437_DB`
block on both erase paths before the fix.
