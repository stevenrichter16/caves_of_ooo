# Snake venom damage and duration correction

Status: complete. 469/469 selected native Unity EditMode tests pass; the existing staged snake PlayMode encounter passes 13/13.

## Problem and scope

The player reports a poisonous snake killing them in roughly three steps and leaving unusually long poison. The actual poisonous snakes are Viper and SpreadLatchcoil. SariSnake causes bleeding instead. This is a balance and stacking correction to original CoO behavior, not a Qud parity claim.

## Verification sweep

| Question | Inspected source | Finding |
|---|---|---|
| How strong is a bite? | NaturalWeaponFactory.cs, ViperBite | 75% after a damaging hit; 1d6 poison per owner action for eight actions. One dose averages 28 and can deal 48, against a fresh 40 HP player. |
| Why does it last? | PoisonedEffect.OnStack | Adds the full incoming duration with no cap. Repeat bites can accumulate 16, 24 or more actions. |
| Does every world tick deal damage? | StatusEffectsPart, TurnManager, InputHandler | Poison starts once at the owner's BeginTakeAction and ages at EndTurn. NPC compatibility TakeTurn skips duplicates. Extra travel clock ticks do not each trigger poison. |
| Does three steps prove a double tick? | Poison dice and combat | Three old ticks alone deal at most 18. Bite damage, previous injury or another hazard can contribute; exact reported combat circumstances are unknown. |
| Are repeat bites possible? | Viper blueprint, body weapon collection | Speed 130 and existing two-hand natural attack structure give extra bite opportunities. Do not redesign anatomy in this bug fix. |
| Does changing the factory repair old saves? | Body.OnAfterLoad, SaveSystem natural defaults | No. Natural weapon entities are saved literally. Upgrade only exact old stock fang payloads on saved Viper/latchcoil bodies, retaining their IDs and custom payloads. |
| Can active saved poison be identified as snake venom? | Effect fields and SaveEffect | No source weapon identity is stored. Do not guess from dice and rewrite unrelated active effects. Existing active effects remain literal until cured or expired. |
| Are poison rolls reproducible through combat? | OnHitEffectFactory | The supplied RNG is not forwarded to PoisonedEffect, unlike other random effects. Forward it and test the actual dispatcher. |

## Contract and implementation plan

1. Write and observe native RED for actual Viper/latchcoil venom, repeat applications, owner-action cadence, exact stock save upgrade and custom/other-creature counterchecks.
2. Retune ViperBite to **75% chance, 1d2 per owner action, four actions**. A single dose is 4–8 poison damage; direct bites remain unchanged.
3. Change ordinary PoisonedEffect reapplication to **refresh to the greater remaining/incoming duration**, never add durations. Either indefinite effect keeps -1. One instance continues to own its original damage dice; do not silently introduce new mixed-potency rules. This deliberate stacking correction also applies to ordinary scorpion/spider/rotling/contact/trap/tonic poison. Gas poison is a separate effect and keeps its behavior.
4. Forward combat RNG to the poison constructor, matching neighboring effect factories.
5. During body load, update only exact stock old ViperBite natural weapons belonging to Viper or SpreadLatchcoil. Preserve object identity, equipment, custom fang data, unknown recipes and already active effects. No save version increase is needed.
6. Native regression sweep: status effects, weapon hooks, actual natural weapons, save round trips, gas and treatment. Run the existing disclosed staged-viper PlayMode scenario as a combat integration check; separately state whether poison actually occurs.
7. Cold review Q1–Q4, fix significant findings, record evidence, commit with the project template, fetch/rebase and push main.

## Review and evidence

The already published Sodden circuit is independent at `1861ec801`.

- Native save regression RED: 19 cases, 14 passing custom/unrelated controls and five old-stock upgrade failures. Initial fixture corrections (an Anatomy import and explicit stable saved fang IDs) preceded the behavioral run; the intermediate output is retained separately.
- Native combined RED: 56 cases, 33 passing and 23 expected failures, after fixture compile corrections. The poison-only subset has 29 cases, 15 intended failures and 14 passing controls (matching standalone). Failures expose actual 1d6 doses, additive duration, indefinite-duration corruption, integer overflow, dropped RNG and missing old-save upgrade.
- These behavioral failures preceded the production changes. `PoisonedEffect.OnStack` now uses max refresh and preserves indefinite; the on-hit factory forwards RNG; ViperBite uses 1d2/four; the body load hook upgrades exact old stock fangs in place.


## Implementation and review

- `NaturalWeaponFactory.ViperVenom` is the single current stock declaration. Direct bite damage, penetration and proc chance remain unchanged.
- `Body.OnAfterLoad` sends attached and detached default slots through the same repair path. Only Viper/latchcoil slots named ViperBite reach the legacy checker. The checker matches the exact old natural-only fang object, both native parts, combat payload and default appearance before changing its raw venom in place; ID, references, flags and other fields are retained. Custom variants remain literal.
- Ordinary poison keeps one effect instance and its original potency/RNG. Its duration becomes the longer remaining exposure, with explicit indefinite handling. Gas poison is unchanged.
- `OnHitEffectFactory` forwards the caller's RNG as neighboring random effects already do.

### Q1–Q4 cold review

**Q1:** attached/detached body load paths share the same hook; fresh and upgraded stock fangs use the same venom constant. Poison RNG routing now matches stun/bleeding. Existing cure/removal and aura rollback paths are untouched.

**Q2:** no new poison class or special treatment exception. Antidote and field dressing continue curing ordinary poison, and leave gas poison alone. No new composition, inventory, UI or biome state is introduced.

**Q3:** actual damaging attack and chance-boundary controls, zero-damage/no-venom controls, four owner-action ticks versus clock-only advances, NPC duplicate dispatch, repeated dosing, both finite/indefinite orders, mixed potency, integer overflow, stock save upgrade, customized/foreign controls and repeated load are covered.

**Q4:** updated current latchcoil design wording and added a retune note to the historical alpha-readiness record. Independent review found no significant unresolved production defect. The report of death in three steps cannot be attributed solely to the old poison's maximum 18 damage in those three ticks.

### Evidence and limits

The first combined native GREEN is **137/137** (`focused-native-green.xml`). The final wider native sweep is **469/469 passed, zero failures/skips**, 12.41 seconds across 20 fixtures (`final-native-green.xml`), including 48 new cases, ordinary status effects, weapon hooks, natural weapons, body/save graphs, gas, cures, Sodden treatment/input and turn processing. Native tests force a declared damaging bite/chance roll and maximum poison rolls, then use the real TurnManager; this proves the four-action/eight-damage bound without claiming blind encounter balance.

Existing active poison is intentionally not rewritten: saves contain no venom-source identity. Stock saved snakes get corrected future bites on load, but a dose already present keeps its stored potency and remaining time until cured or expired. This is distinct from requiring a new world; a new world is not required for the snake fix.

### Files

Production: `NaturalWeaponFactory.cs`, `Body.cs`, `PoisonedEffect.cs`, `OnHitEffectFactory.cs`.
Tests: new `PoisonBalanceRegressionTests.cs` (29 cases) and `SnakeVenomSaveTests.cs` (19 cases), plus their metadata; deliberately updated old poison-stack and Viper recipe pins in `StatusEffectTests.cs` and `GameAuditNaturalWeaponActivationAdversarialTests.cs`.
Documents: this living fix record, `SPREAD-FIRST-HOUR-PLAN.md`, and the historical note in `ALPHA-READINESS.md`. Raw RED/GREEN evidence is under `Docs/Verification/PoisonBalance`.


## PlayMode integration witness

Existing reviewed launcher: `Caves Of Ooo/Scenarios/World/First-Hour Staged Viper Native Audit`.
Run [`ab7aa29bf81c451e9c5574c938891ffa/report.json`](Verification/SpreadFirstHour/M5a/Native/ab7aa29bf81c451e9c5574c938891ffa/report.json): **13/13 checks passed**, complete=true, failures=0, unexpectedErrors=0, 13.71 seconds, 25 completed player turns. This is the previously disclosed staged geometry and one original-player transfer, followed by normal native controls. The editor returned to idle SampleScene.

**Can verify (script-observable):** real source builder, warning, sleeping bypass, native sight wake and bite attempt, native combat defeat, natural corpse/finite harvest, and saved/restored source outcomes. It used one original control skill and two dagger attacks; no tonic or injected RNG was needed.

**Cannot verify:** poison did **not** occur in this one natural-roll encounter (`poisonObserved=false`), so this PlayMode run is not evidence for venom damage or duration. Those bounds are proven by the controlled actual-bite/native-turn EditMode tests. This run also does not prove blind discovery, broad balance or guaranteed survival. No reroll was made to obtain a preferred outcome. The bite-state screenshot was inspected; no graphics change is claimed by this fix.

### Remaining boundary

The original report's exact three-step death was not reproduced from a full-health player using poison alone; the old dose nonetheless had a maximum of 48 damage and uncapped duration growth. The fix reduces that dose and removes growth while retaining direct bite danger. Existing poisoned saves can still use ordinary Antidote or the new field dressing; no unrelated saved effect is silently cleared.
