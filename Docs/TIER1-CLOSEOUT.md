# Tier 1 Content Closeout — plan

> Final ship for Tier 1 of `Docs/CONTENT-ROADMAP.md`. Closes the four
> remaining 💡/📋 items: BleedTonic, CharredTonic, ElementalCreatureZoo,
> TonicTestBench. Each is content-shaped (blueprint + content + smoke
> test); two of the four require small dispatcher edits. ~1 hour
> total focused work.

## Verification sweep (pre-implementation)

| Premise | Status | Source |
|---|---|---|
| `BleedingEffect` exists with `(int saveTarget=15, string damageDice="1d2", System.Random rng=null)` ctor | ✅ confirmed | `Effects/Concrete/BleedingEffect.cs:9, 17` |
| `CharredEffect` exists with `()` parameterless ctor (`Duration = DURATION_INDEFINITE` set in body) | ✅ confirmed | `Effects/Concrete/CharredEffect.cs:7, 14-17` |
| `CharredEffect` reduces `MaterialPart.Combustibility` by 70% on apply, restores on remove | ✅ confirmed | `CharredEffect.cs:19-39` |
| `StatusTonicPart.CreateEffect` switch covers poison/fire/wet/acid/shock/ice/stoneskin **but NOT bleeding/charred** | ⚠️ **needs case additions** | `StatusTonicPart.cs:35-90` |
| `StatusTonicPart` returns `null` from `CreateEffect` on unknown EffectName → tonic.ApplyTo no-ops silently. Means a BleedTonic blueprint without a dispatcher case would build cleanly but apply nothing — silent content bug | ✅ confirmed (real risk) | `StatusTonicPart.cs:88-90` |
| Existing tonic blueprint pattern: `{Render, Physics, Tonic(Message), StatusTonic(EffectName,EffectMagnitude), Commerce(Value)}` | ✅ confirmed | `Objects.json` (FrostTonic, AcidTonic, etc.) |
| Existing tonic test pattern: `_harness.Factory.CreateEntity` + `FireApplyTonic` + `HasEffect<T>()` assertion | ✅ confirmed | `FrostTonicTests.cs:1-50` |
| Existing scenario showcase pattern: `[Scenario(...)]` attr + `IScenario.Apply(ctx)` + menu entry + smoke test + scenario diag fixture | ✅ confirmed (12+ existing) | `Scenarios/Custom/*.cs` |
| Resistance creatures matrix (9 unique creature blueprints): | ✅ confirmed via `Objects.json` grep | see roadmap §"Resistant creatures" |

| Axis | -50 (vulnerable) | 25 | 50 (resistant) | 100 (immune) |
|---|---|---|---|---|
| Acid | Scorpion | — | CaveSlime | — |
| Cold | CharredHusk | Snapjaw | SnapjawHunter | IceWight |
| Heat | IceWight | — | Glowmaw | CharredHusk |
| Electric | BrassHusk | — | StoneGolem | — |

**1 false premise corrected.** The roadmap framed the tonics as
"pure JSON + tests" but `StatusTonicPart.CreateEffect` doesn't
dispatch to BleedingEffect or CharredEffect yet. Without case
additions the blueprints would load + the tonic items would be
takeable + drinkable, but the drink would fire no effect. **Silent
content bug — visible only in playtest.** Surfaced pre-impl by
this sweep; planned into T1.2 / T1.3.

**No other false premises.** The effect classes exist with the
expected ctors. The blueprint loader's part-name resolution
(EntityFactory.cs:230-249, suffix-lookup pattern) handles
"StatusTonic" → StatusTonicPart correctly.

## Sub-milestones (smallest blast radius first)

### T1.1 — Plan + branch (this commit)

- `Docs/TIER1-CLOSEOUT.md` (this file)
- Branch `feat/tier1-content-closeout` cut from `main` at `c267ce3`

### T1.2 — BleedTonic (one commit)

**Modify** `Assets/Scripts/Gameplay/Items/StatusTonicPart.cs`:

```csharp
case "bleed":
case "bleeding":
case "bleedingeffect":
    return new BleedingEffect(
        saveTarget: EffectDuration > 0 ? EffectDuration : 15,
        damageDice: string.IsNullOrWhiteSpace(EffectDamageDice) ? "1d2" : EffectDamageDice);
```

**Note**: `BleedingEffect` doesn't have a `magnitude` param — it has
`saveTarget` (DC for the save-vs-bleed roll on each tick) and
`damageDice`. The dispatcher's `EffectDuration` field semantically
maps to `saveTarget` here (re-using a numeric content-author field
rather than adding a new one). Documented inline.

**New blueprint** in `Objects.json`:

```json
{
  "Name": "BleedTonic",
  "Inherits": "TonicItem",
  "Parts": [
    { "Name": "Render", "Params": [
      { "Key": "DisplayName", "Value": "bleed tonic" },
      { "Key": "RenderString", "Value": "!" },
      { "Key": "ColorString", "Value": "&r" },
      { "Key": "RenderLayer", "Value": "5" }
    ]},
    { "Name": "Physics", "Params": [{ "Key": "Takeable", "Value": "true" }, { "Key": "Weight", "Value": "1" }] },
    { "Name": "Tonic", "Params": [{ "Key": "Message", "Value": "Sharp pain blooms — your wounds reopen." }] },
    { "Name": "StatusTonic", "Params": [
      { "Key": "EffectName", "Value": "Bleeding" },
      { "Key": "EffectDuration", "Value": "12" }
    ]},
    { "Name": "Commerce", "Params": [{ "Key": "Value", "Value": "18" }] }
  ],
  "Stats": [],
  "Tags": [{ "Key": "Tier", "Value": "1" }]
}
```

**RED → GREEN tests** in `Tests/.../Items/BleedTonicTests.cs`:
1. `BleedTonic_DrinkApplies_BleedingEffect_ToDrinker` — positive
2. `BleedTonic_BlueprintMagnitude_BecomesEffectSaveTarget` — content-to-effect param mapping
3. `BleedTonic_AppliedEffect_IsBleedingEffect_NotOtherElement` — counter-check (rules out cross-pollination)
4. `BleedTonic_OnEntityWithoutMaterial_NoCrash` — adversarial (BleedingEffect's tick path reads target stats; any null path?)

### T1.3 — CharredTonic (one commit)

**Modify** `StatusTonicPart.cs` again:

```csharp
case "char":
case "charred":
case "charredeffect":
    return new CharredEffect();
```

Simpler — `CharredEffect` is parameterless. `EffectMagnitude` /
`EffectDuration` fields on the blueprint get ignored
(intentionally — Charred is a permanent vulnerability state).

**New blueprint**:

```json
{
  "Name": "CharredTonic",
  "Inherits": "TonicItem",
  "Parts": [
    { "Name": "Render", "Params": [
      { "Key": "DisplayName", "Value": "charred tonic" },
      { "Key": "RenderString", "Value": "!" },
      { "Key": "ColorString", "Value": "&K" },
      { "Key": "RenderLayer", "Value": "5" }
    ]},
    { "Name": "Physics", "Params": [{ "Key": "Takeable", "Value": "true" }, { "Key": "Weight", "Value": "1" }] },
    { "Name": "Tonic", "Params": [{ "Key": "Message", "Value": "Bitter ash coats your tongue — your skin hardens against future fire." }] },
    { "Name": "StatusTonic", "Params": [
      { "Key": "EffectName", "Value": "Charred" }
    ]},
    { "Name": "Commerce", "Params": [{ "Key": "Value", "Value": "30" }] }
  ],
  "Stats": [],
  "Tags": [{ "Key": "Tier", "Value": "1" }]
}
```

**RED → GREEN tests** in `Tests/.../Items/CharredTonicTests.cs`:
1. `CharredTonic_DrinkApplies_CharredEffect_ToDrinker`
2. `CharredTonic_AppliesPermanentDuration` — verify `Duration == DURATION_INDEFINITE`
3. `CharredTonic_ReducesCombustibility_OnDrinkerWithMaterial` — pin the gameplay effect (70% reduction per `CharredEffect.OnApply`)
4. `CharredTonic_AppliedEffect_IsCharredEffect_NotOtherElement` — counter-check

### T1.4 — ElementalCreatureZoo scenario (one commit, 📋 → ✅)

**New file** `Scenarios/Custom/ElementalCreatureZoo.cs`:

A QA-aid scenario placing all 9 resistance creatures in a
labeled grid around the player, plus a labeling row showing
the resistance value. No combat — just a "look at the lineup"
content-QA tool.

Layout (relative to player center; `R+` = positive resistance,
`R-` = vulnerable, `R++` = 100% immune):

```
            ColdR=25   ColdR=50   ColdR=100/HeatR=-50  HeatR=100/ColdR=-50
              ↓           ↓                ↓                    ↓
[Player] →  Snapjaw  SnapjawHunter      IceWight            CharredHusk

            HeatR=50   ElecR=50    ElecR=-50    AcidR=+50    AcidR=-50
              ↓           ↓            ↓            ↓             ↓
            Glowmaw   StoneGolem   BrassHusk    CaveSlime    Scorpion
```

Player loadout: HP 999 (so they survive walking through the zoo),
each elemental weapon in inventory (FlamingSword / IceSword /
ThunderHammer / AcidicDagger).

Menu entry priority 118 (after Quest Showcase at 117).
Smoke test in `ScenarioCustomSmokeTests`.

**Scenario diag fixture** `Tests/.../Scenarios/ElementalCreatureZooDiagTests.cs`:
1. `Apply_SpawnsAllNineResistanceCreatures` — pin the count
2. `Apply_EachAxisHasCorrectResistanceValueOnTarget` — pin Snapjaw.ColdResistance=25, Glowmaw.HeatResistance=50, etc. (rules out blueprint regression)
3. `Apply_NoCombatHappens_NoQuestNoTradeRecords` — counter-check: pure layout, no diag side effects on apply

### T1.5 — TonicTestBench scenario (one commit)

**New file** `Scenarios/Custom/TonicTestBench.cs`:

A drink-and-observe scenario: vials of every tonic on a shelf
around the player. Player has full HP, can drink any tonic, observe
status effects. Pairs with the new `effect/OnApply` diag channel
(D2 substrate) so observability is automatic.

Tonics included (10 total — the entire current tonic set):
- HealingTonic, PoisonTonic, FireTonic, FrostTonic, AcidTonic,
  LightningTonic, WaterTonic, StoneskinTonic, BleedTonic (T1.2),
  CharredTonic (T1.3)

Layout — 2 rows of 5 floor-placed tonic items east of the player:

```
                  Heal   Poison   Fire   Frost   Acid
  [Player] →
                  Light  Water    Stone  Bleed   Charred
```

Menu entry priority 119. Smoke test.

**Scenario diag fixture** `Tests/.../Scenarios/TonicTestBenchDiagTests.cs`:
1. `Apply_PlacesAllTenTonicsOnFloor`
2. `DrinkBleedTonic_RecordsEffectOnApplyDiag` — pin that the BleedTonic from T1.2 fires `effect/OnApply` for `BleedingEffect`
3. `DrinkCharredTonic_RecordsEffectOnApplyDiag` — same for CharredTonic / CharredEffect
4. Counter-check: `Apply_NoTonicsConsumed_NoOnApplyRecords` — proves scenario apply doesn't trigger drinks

### T1.6 — Cold-eye review + roadmap update + merge + push

Per CLAUDE.md §3.4. Update roadmap entries:
- Tier 1 § Status tonics: flip BleedTonic + CharredTonic 💡 → ✅
- Tier 1 § Lightweight scenarios: flip ElementalCreatureZoo 📋 → ✅, TonicTestBench 💡 → ✅
- "Recently shipped" table: prepend TIER-1 closeout entry

Cold-eye Q1-Q4:
- Q1: BleedTonic + CharredTonic test fixtures mirror existing FrostTonicTests shape
- Q2: dispatcher case style matches existing 7 cases (lowercase aliases + ctor)
- Q3: cross-pollination counter-checks on both tonics; no-effect-spawn counter-check on both scenarios
- Q4: T1.x sub-milestones in this plan ↔ shipped commits

## Critical files

### New files (T1.2 → T1.5)

| Path | Purpose |
|---|---|
| `Docs/TIER1-CLOSEOUT.md` | Plan doc (this file) |
| `Tests/.../Items/BleedTonicTests.cs` | T1.2 4 tests |
| `Tests/.../Items/CharredTonicTests.cs` | T1.3 4 tests |
| `Scripts/Scenarios/Custom/ElementalCreatureZoo.cs` | T1.4 scenario |
| `Tests/.../Scenarios/ElementalCreatureZooDiagTests.cs` | T1.4 3 tests |
| `Scripts/Scenarios/Custom/TonicTestBench.cs` | T1.5 scenario |
| `Tests/.../Scenarios/TonicTestBenchDiagTests.cs` | T1.5 4 tests |

### Modified files

| Path | Change |
|---|---|
| `Scripts/Gameplay/Items/StatusTonicPart.cs` | T1.2 + T1.3 dispatcher cases (~10 new lines) |
| `Resources/Content/Blueprints/Objects.json` | T1.2 + T1.3 blueprints (~30 lines) |
| `Editor/Scenarios/ScenarioMenuItems.cs` | T1.4 + T1.5 menu entries (priorities 118 + 119) |
| `Tests/.../Scenarios/ScenarioCustomSmokeTests.cs` | T1.4 + T1.5 smoke tests |
| `Docs/CONTENT-ROADMAP.md` | T1.6 flips + Recently Shipped entry |

## Reusable utilities (don't reinvent)

| Utility | Path |
|---|---|
| `_harness.Factory.CreateEntity(blueprintName)` | `Tests/.../ScenarioTestHarness.cs` (existing test pattern) |
| `FireApplyTonic(tonic, drinker)` | `Tests/.../ItemTestHelper` (used by 6 existing TonicTests) |
| `HasEffect<T>()` / `GetEffect<T>()` on `StatusEffectsPart` | `Effects/StatusEffectsPart.cs:215, 220` |
| `[Scenario(name, category, description)]` attr + `IScenario.Apply(ctx)` | `Scenarios/IScenario.cs` |
| `ctx.Spawn(blueprintName).At(x, y)` chain | `Scenarios/ZoneBuilder.cs` |
| `Diag.Record(channel, kind, ..., payload)` (existing 7 channels — no new channel needed) | `Shared/Utilities/Diag.cs` |
| `DiagQuery.Apply(filter)` for scenario diag verification | `Shared/Utilities/DiagQuery.cs` |

## Diag observability — reuse existing channels

No new diag channel required. T1.4 / T1.5 scenario diag fixtures
will assert against the EXISTING substrate's records:
- `effect/OnApply` — fires on every tonic-applied effect (D2 substrate)
- `damage/DamageDealt` — would fire if any combat happens (counter-check that scenarios don't trigger it)

This is good — reuses the existing 7-channel substrate without
adding a "tonic" channel that would split observability.

## Self-review pre-flagged 🟡 findings

These are designed-in tradeoffs to flag before committing — fix or
defer with a note per CLAUDE.md §5.

- **🟡 BleedTonic uses `EffectDuration` field as `saveTarget`.**
  StatusTonicPart's blueprint shape has `EffectMagnitude` (float)
  and `EffectDuration` (int). BleedingEffect's ctor is `(saveTarget,
  damageDice, rng)` — `saveTarget` is an int that conceptually
  matches `EffectDuration`'s int slot, but semantically they're
  different (EffectDuration = how-many-turns vs saveTarget = DC for
  save-roll). The roadmap's later "BleedTonic blueprint authoring"
  doc should clarify this mapping. Document in T1.2 commit body.
- **🟡 CharredTonic ignores `EffectMagnitude`/`EffectDuration`.**
  CharredEffect is parameterless (Duration = INDEFINITE in body).
  A blueprint that sets EffectMagnitude=2.0 would have no effect.
  Acceptable v1; document.
- **🟡 ElementalCreatureZoo creature locations.** Hand-coded
  positions for 9 creatures; if a content edit moves one, the
  diag test pinning resistance values per spawn would fail. Mitigate
  by keying the diag test on creature blueprint name (which is
  stable), not position.
- **🔵 TonicTestBench's diag tests fire actual drinks.**
  This means MessageLog accrues; tests need MessageLog.Clear in
  SetUp (existing pattern). Also: in the scenario itself, the
  player has to MANUALLY drink each tonic to observe — that's
  fine, the scenario is a manual-playtest tool.
- **⚪ No new diag channel for tonics.** Reusing `effect/OnApply`.
  If future "tonic-specific" observability is wanted (e.g. a
  TonicConsumption channel), add then; not now.

## Verification (post-implementation)

Three layers:

1. **Per-fixture RED → GREEN cycles** during T1.2-T1.5:
   - T1.2: 4 tests
   - T1.3: 4 tests
   - T1.4: 3 scenario diag + 1 smoke = 4
   - T1.5: 4 scenario diag + 1 smoke = 5
   - **Total**: 17 new tests

2. **Targeted regression sweep** after T1.5:
   ```
   run_tests EditMode group_names=[
     "BleedTonicTests", "CharredTonicTests",
     "FrostTonicTests", "AcidTonicTests", "LightningTonicTests",
     "WaterTonicTests", "StoneskinTonicTests", "ThrowableTonicTests",
     "ElementalCreatureZooDiagTests", "TonicTestBenchDiagTests",
     "ScenarioCustomSmokeTests"
   ]
   ```
   Expected: 100/100 GREEN.

3. **Manual playtest** via the two new scenarios (T1.4 + T1.5):
   - Click `Caves Of Ooo / Scenarios / Combat Stress / Elemental Creature Zoo`
   - Visually confirm 9 creatures at expected positions
   - Click `Caves Of Ooo / Scenarios / Combat Stress / Tonic Test Bench`
   - Walk + pick up tonics + drink each + watch for color changes / log lines

## Implementation sequence (paced for Unity MCP)

```
1. Plan to disk (T1.1, this commit)
2. T1.2 BleedTonic — 1 dispatcher case + 1 blueprint + 4 tests
   → 1 refresh + 1 test run
3. T1.3 CharredTonic — same shape
   → 1 refresh + 1 test run
4. T1.4 ElementalCreatureZoo scenario + diag fixture
   → 1 refresh + 1 test run
5. T1.5 TonicTestBench scenario + diag fixture
   → 1 refresh + 1 test run
6. Targeted regression sweep
   → 1 test run (no refresh — code stable)
7. Cold-eye review + roadmap update + commit T1.6 + merge --no-ff + push
```

7 total Unity refreshes/runs across the cycle. Spaced with
`sleep 22` between commands (the established pacing from prior
sessions to avoid Unity MCP keep-alive disconnects).

Expected total: ~50 lines new code (mostly dispatcher cases) +
~120 lines blueprint JSON + ~250 lines new tests + ~150 lines
scenario code + this plan (~300 lines). ~1 hour focused work.

## What gets observable to the player after this ship

| Today | After T1 closeout |
|---|---|
| 8 status tonics (Heal/Poison/Fire/Frost/Acid/Lightning/Water/Stoneskin) | + BleedTonic + CharredTonic = 10 tonics, completes the elemental tonic row |
| StatusTonicPart dispatches 7 EffectName variants | + bleeding/charred = 9 variants (table in dispatcher matches the effect catalog 1:1) |
| Roadmap §"Lightweight scenarios": 2 ✅, 2 💡/📋 | All 4 ✅ — Tier 1 `Lightweight scenarios` row CLOSED |
| Tier 1 has 4 outstanding 💡/📋 items | **Tier 1 has 0 outstanding 💡/📋 items** — first roadmap tier fully closed |
| 7 diag channels (event/effect/damage/turn/furniture/trade/quest) | Same 7. Reuse via the scenario fixtures. |
