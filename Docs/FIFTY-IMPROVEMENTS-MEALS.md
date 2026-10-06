# Iterations 46–50: food as expedition preparation

Status: **iterations 46–50 complete**. All 41 dedicated meal tests pass in the final 831-case native Unity run. All 20 controlled cooking/eating/clock/save checks also pass in the final Play-mode run. Plans, source sweep, RED evidence, counters and independent review are recorded below. CoO-original design.

## Baseline sweep and scope

FoodPart currently consumes a carried unit, heals, and displays flavor. The five existing cooked outputs use the same general healing role. CookingService already converts carried raw stacks at nearby campfires transactionally; those routes and rendered food assets need no replacement. Status effects already age on owner EndTurn and save public payload fields; saved stats include Boost and effects do not rerun OnApply on load. Thus meal boosts must save their applied amount and revert it exactly once.

A prepared meal lasts 100 owner turns and occupies one meal effect. Eating another prepared meal replaces the previous benefit; repeating the same meal refreshes rather than accumulating. Raw food still heals without replacing a prepared meal. These benefits supplement healing and are available at full HP. Consumption remains one carried unit, never remote/ground food. Armor, tonics and meal bonuses retain their existing separate sources. Existing saves preserve serialized food parts; new/generated/cooked items receive the new authored fields.

## Per-iteration plans and designs

| ID | Plan / player purpose | Design | Acceptance and counter |
|---|---|---|---|
| 46 | Prepare for hot approaches with cooked grain | ToastedEmberwheat gives +20 HeatResistance for 100 owner turns; gather emberwheat and cook by a real station | Eat one cooked unit: resist rises and Heat damage drops; raw Emberwheat grants none; Cold unchanged |
| 47 | Prepare for freezing enemies and caves | RoastedHearthbulb gives +20 ColdResistance for 100 owner turns; existing planted crop and fire route | Cold damage reduced, Heat unchanged; raw bulb has no protection |
| 48 | Prepare for acid and corrosive expeditions | RoastedMushroom gives +20 AcidResistance for 100 owner turns | Acid damage reduced, physical damage unchanged; raw mushroom has no protection |
| 49 | Favor endurance and recovery saves | CookedMeat gives +2 Toughness for 100 owner turns; existing corpse harvest/cook route | Toughness and its modifier increase, existing injuries are not magically healed beyond food healing; raw meat grants none |
| 50 | Favor evasion over resistance/endurance | RoastedStarapple gives +1 DV for 100 owner turns; existing foraged fruit/cook route | Effective combat DV rises, AV unchanged; raw fruit grants none |

Each choice has an opportunity cost: it replaces the other four meal benefits. No hunger meter, permanent stats, recipe book, new model requirement or free inventory grant is introduced. Inspection must state magnitude, duration and replacement.

## Verification plan

Native RED on all five authored foods; counters for raw counterparts and uncarried units. GREEN adds replacement/refresh/expiry, positive effect classification, death handling, full HP, save/load/removal, real cook transformation, finite stat bounds and ordinary damage routing. Independent review will assess stat lifecycle symmetry and whether content descriptions agree with actual fields.

## In-phase review

- 🟡 Fixed during review: a failed outer inventory action restored food/stats but left a newly replaced meal effect. A dedicated native RED reproduced this; preparation now runs only after the complete consuming transaction commits. Existing raw-food healing behavior is unchanged.
- 🔵 Initial native GREEN: 35/35 including all five cooking routes, damage routing, missing player resistance stats, replacement, expiry and save/remove. Final regression must include the new rollback counter.
- 🔵 Stat application/removal uses saved AppliedBonus; unrelated boosts survive expiry. Created resistance stats remain neutral after removal to preserve later contributions.
- 🧪 One-meal duration/magnitudes are intentionally modest design choices; tests cannot prove long-term balance.

- 🟡 Fixed after native RED: old unbuffed saved dishes could merge with a newly prepared same-blueprint meal. Food payload identity now compares healing and all meal fields; four differing-payload tests failed before this change. Matching meals still stack.
- Native RED baseline for the full program is `Verification/FiftyImprovements/all-red.xml`: 232 total, 112 pass, 120 fail. The meal group is 36 pass/4 fail (the four stack identity cases); the remaining 116 failures exercise not-yet-implemented workstreams.

## Dedicated adversarial sweep

`PreparedMealAdversarialTests` contains 24 existing post-RED cases extracted from the original fixture without duplicates: replacement/raw-food preservation, refresh/expiry, five saved contribution round trips, five ownership refusals, damage/DV/Toughness integration, missing-stat preservation, dead consumption, failed outer command, and four incompatible stack payloads. `PreparedMealTests` retains 17 primary acceptance cases including the 20-check native-runtime scenario. Total 41 cases; extraction changes fixture names, not the prior RED evidence.

First combined implementation run: 40/41 meal tests passed. The scenario-wrapper case failed before invoking the benchmark because `ScenarioContext` rejects a null clock. Its fixture now creates a real caller clock and restores the preceding global clock in `finally`; no production meal change was needed. At that point the native Play benchmark had not yet run.

Focused native receipt `Verification/FiftyImprovements/focused-green.xml`: 342/342 overall, including all 41 meal cases and the 20-check detached benchmark inside EditMode. This proves real cook/eat command, owner clock and replacement-save behavior in the native runtime; it does not yet constitute a Play-mode keyboard witness or ordinary discovery proof.

## Independent review

A separate read-only review found no further actionable defect in saved stat contributions, meal replacement/expiry, transactional consumption, or stack identity. The positive and refusal tests cover all five cooked outcomes, full HP, raw food, dead actors, ground food, unrelated boosts, save replacement, and rollback. Long-term balance and the natural discovery journey remain playtest questions.

## Final acceptance

[Final native XML](Verification/FiftyImprovements/final-green.xml): 831/831 overall, including 41/41 meal cases. [Final Play report](Verification/FiftyImprovements/NativeClarity/f84726915d704c829dea68be2ad672ee/report.json): 20/20 meal command/owner-clock/replacement-save checks and all 60 combined checks passed with zero errors. The prepared-food screenshot was inspected and remained byte-identical in the final rerun: healing, +20 heat resistance, 100 owner turns and replacement cost are all readable. This is controlled command and UI evidence, not ordinary crop discovery or campaign balance.
