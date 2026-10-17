# Starting builds - implementation

**Status:** original implementation and historical evidence below; later native
bootstrap coverage is in `Docs/COMPOSABLE-BUILDS.md`. The dedicated Hurler
addition is planned and verified separately in the dated section below.
Original design: `Docs/Design/STARTING-BUILDS.md` and `Docs/Design/StartingBuildsProbe/`.
**Qud reference:** none. Classified **CoO-original** (§4.2): Qud's character
creation is a long genotype/caste flow; this is a one-screen pick of a
pre-built kit and makes no parity claim.

## What the player sees

On a new game the player now chooses how they begin, before the first
checkpoint is written:

```
+------------------------------------------------------------------------+
| Choose how you begin                                                   |
+----------------------+-------------------------------------------------+
| > 1 The Duelist      | The Duelist                                     |
|   2 The Breaker      | "<tagline>"                                     |
|   3 The Stormcaller  | HP 40   DV 11   Speed 100                       |
|   4 The Bombardier   | Str 16  Agi 22  Tou 14  Ego 18                  |
|   5 Classic          | Wearing / Carrying / Skills / Weakness / First buy |
+----------------------+-------------------------------------------------+
 [Up/Down] browse  [1-5] jump  [Enter] begin
```

Keys: Up/Down or W/S, digits, Enter/Space. Mouse hover and click work.
Escape does nothing (there is nothing to go back to; Classic is on the list).
With an existing save, the boot menu still comes first; its "New game" opens
the picker, "Continue" never does.

| Build | Str/Agi/Tou/Ego | Kit | Skills | DV |
|---|---|---|---|---|
| Duelist | 16/22/14/18 | dagger, buckler, cloak | Short Blades tree (6) | 11 |
| Breaker | 20/16/16/18 | cudgel, buckler | Cudgel tree (6) | 7 |
| Stormcaller | 16/20/16/18 | dagger, cloak | Kindle, Arc Bolt, Jet Blast, Ground Surge, Calm, Spellcraft | 9 |
| Bombardier | 16/18/14/22 | dagger, leather cap, cloak, poison gas grenade, lightning tonic, frost tonic | Drench Lob, Cold Snap, Calm | 8 |
| Classic | 18/18/18/16 | the legacy kit, dagger equipped, six spells | (unchanged) | - |

Every build also carries 2 healing tonics and 2 dried meat (and the farming
kit, which is granted for everyone as before).

## Design

- **Data:** `Assets/Resources/Content/Data/Builds/StartingBuilds.json`.
  `StartingBuildRegistry` loads it tolerantly (a malformed file leaves an empty
  registry, never an exception) and validates it against the blueprint factory
  at boot: attribute sum 70 (even values), at most 10 skills (hotbar slots),
  known skill classes, known blueprints, `Equip` only on equippable items, and a
  weapon-first rule (a non-weapon in a hand slot ahead of the weapon would
  demote it to the 15% off-hand; see `CombatSystem.GatherMeleeWeapons`).
- **Applier:** `StartingBuildService.Apply` validates *before* mutating (atomic
  refusal), sets the four attributes, grants items in order (equipped ones via
  `InventorySystem.Equip`), grants skills in order (`AddSkill`, so hotbar slots
  bind in the listed order), then checks the primary-hand weapon is the intended
  one. Refuses a second build on the same player. Stamps
  `Properties["StartingBuild"]`.
- **Classic** reproduces the legacy kit through the same service
  (`NewGameLoadout.Grant`, dagger equipped, `StartingSpellKit.GrantAll`).
  `ApplyClassicFallback` is the safety net when the picker cannot open or a build
  is rejected: a bare, weaponless, spell-less character is never checkpointed.
- **When the picker appears** (`StartingBuildSelection.ShouldPrompt`): never in
  DevMode, with a scenario pending, under a native-audit save root, or in batch
  mode, and never when no builds loaded.
- **Boot flow** (`GameBootstrap`): `_awaitingBuildChoice` is decided before step
  6; the default kit and spells are withheld; after boot the picker opens
  immediately for a fresh game (or via the boot menu's New game when a save
  exists); `BeginNewGame()` runs after the choice.
- **Layers:** pure model/card/policy in `Gameplay/Bootstrap` (unit-tested);
  `StartingBuildMenuController` (keys) and `StartingBuildMenuUI` (tilemap
  popup, PauseMenuUI idiom) in Presentation; `BootMenuController.NewGameGate`
  is the one change to existing UI code.

## Verification-sweep corrections (§1.2)

| Premise | Reality | Consequence |
|---|---|---|
| Boot menu "New game" can simply open the picker | It calls `BeginNewGame()` immediately, checkpointing the empty character | Added `BootMenuController.NewGameGate`; checkpoint moves to after the choice |
| Scenario launches can be detected at prompt time | `ScenarioRunner.PendingScenario` is cleared inside `OnAfterBootstrap` | Decision is taken before step 6 |
| Skill classes can be validated through `SkillsPart` | `ResolveSkillType` is private | Added `SkillsPart.IsKnownSkillClass` |
| `Count` defaults to 1 in JSON | `JsonUtility` leaves a missing int at 0 | `StartingBuildItem.EffectiveCount` (0 or negative = 1) |
| A buckler/torch in the first hand slot is harmless | It makes the fist the primary weapon and demotes the real weapon to a 15% off-hand | Validator rule + equip order (weapon first) + service post-check |
| Toughness raises HP | It does not (HP is fixed at 40 for every build) | Toughness shown, never sold as HP |

## Divergences from the design doc

| # | Design | Shipped | Why |
|---|---|---|---|
| 1 | Four builds | Four builds **plus Classic** as a fifth, selectable entry | User request |

The four builds' attributes, kits and skills are the ones in
`Docs/Design/STARTING-BUILDS.md` §6 and in the sim rows it cites.

## Evidence (the real service, not a hand-built player)

`Docs/Design/StartingBuildsProbe/BuildFightSimImpl.cs` rebuilds every player
through `StartingBuildService.Apply` and replays the six scenarios (60 runs per
cell, about +/-6 points). Output: `fight-sim-implemented.txt`.

| Scenario | Classic | Duelist | Breaker | Stormcaller | Bombardier |
|---|---|---|---|---|---|
| S1 glade (3 foes) | 37% | 73% | 90% | 73% | 95% |
| S2 lair (3 foes) | 3% | 22% | 37% | 47% | 83% |
| S3 one ape | 82% | 80% | 100% | 100% | 85% |
| S4 Sodden | 100% | 100% | 100% | 100% | 100% |
| S5 ambush glade | 53% | 73% | 90% | 85% | 67% |
| S6 ambush lair | 0% | 22% | 58% | 7% | 0% |

- The service-built Duelist, Breaker and Stormcaller reproduce the design-time
  rows to within sampling noise (design-time A/B/C2: S1 73/90/75, S2 22/37/48,
  S3 80/100/100, S6 20/58/5), so the data file matches what was designed and
  simulated.
- **Classic is 37% in S1, not the 80% of the design doc.** The service-built
  Classic equals the design-time "Today" row run on the post-freeze code
  (37/3/82/100/53/0, file `fight-sim-designtime-builds-on-freeze-code.txt`), so
  the drop is the freeze change (the Rime Grip lock is gone, see
  `Docs/FREEZE-THAW.md`), not a defect in the applier.
- **The Bombardier is a little weaker than designed** where the frost lock
  carried it: S3 85% (design 93%), S5 67% (design 75%). Both are 8 points, just
  outside the +/-6 noise, and match the freeze windows now being 4-10 turns
  instead of permanent. S1 and S2 are within noise. It remains the strongest
  start in the two glade/lair open fights (S1, S2); S6 is still 0%.
- The sim is not bit-reproducible (some randomness is not seeded): two runs of
  the same code differ by a few points per cell, and one cell (Bombardier with
  tonics only, S1) moved 12% to 47% when the ignition rule changed, so treat
  single cells as +/-8, not as measurements.

## In-phase self-review

- 🟡 *Fixed pre-commit.* The picker and the boot menu both wanted to checkpoint.
  Resolved by moving the checkpoint into the choice callback; the boot-menu gate
  has a counter-check that Continue never opens it.
- 🟡 *Fixed pre-commit.* `ApplyClassicFallback` + the safety path in
  `OnStartingBuildChosen`: a rejected build must not leave a bare character.
  Mutation-checked (a stubbed fallback fails `ClassicFallback_WorksEvenWhenNoBuildsAreLoaded`).
- 🔵 `StartingBuildMenuUI` is a near copy of `PauseMenuUI`'s tilemap primitives.
  Extracting a shared helper is a refactor out of scope here.
- 🧪 Not covered by tests: the Unity glue (`InputHandler` modal ordering,
  `GameBootstrap` flow, popup rendering). Roslyn compiles it with only
  Unity-type noise; it has not been run.
- ⚪ The tagline, weakness and first-buy lines are content copy, not tuned values.

## Hypothesis-driven pass (save/load reach)

Question asked: *what does a player do that the unit tests do not simulate?* They
quit and come back. Six tests pin that a built character survives a **whole-session**
save: marker, four attributes, skill set, primary-hand weapon and hotbar order for
each of the four builds; a loaded built player refuses a second build; and a
counter-check that an unbuilt player round-trips with no marker and no build weapon.
Result: **0 bugs, 6 pinned-as-correct** (5 invariants + 1 counter-check). A first
attempt that used a bare `SaveEntityBody` round trip showed the weapon as missing;
that was the test harness (equipment is saved as graph references and only resolves
in a full session save), not a defect, and is why the helper saves a whole session.

## Honesty bounds

**Can verify (script-observable):** build composition, order, validation and
atomic refusal; derived DV equals the real `CombatSystem.GetDV`; the card text;
key dispatch and the boot-menu gate; the sim numbers above (real TurnManager,
AI, combat, skills).

**Cannot verify (needs the Unity editor):** how the popup renders (CP437
glyphs, layout in an 80x45 grid, mouse hit-testing), that `InputHandler`'s
modal ordering swallows every other key while the picker is up, the
`GameBootstrap` first-boot and with-save flows end to end, and that the hotbar
and sidebar refresh after the choice. Run a fresh game, then `New game` over a
save, before merging to the shipped branch.

## Automated Play sessions

The picker is a modal: a Play-mode session driven by `manage_input` (or any script
that expects to act immediately) must answer it first. The highlight starts on
the Duelist; Enter begins, digit 5 then Enter picks Classic. DevMode, a launched
scenario, a native audit and batch mode skip the picker entirely.

## Open decisions for the owner

1. **Breaker Str 20 vs Str 18/Agi 18:** the design-time sim difference is within
   noise (S1/S2/S6 wins 90/37/58 vs 87/38/53). Shipped at 20 for the weapon-damage
   reason in the design doc (F3: about 15 per swing at Str 20 with its skills); the
   18/18 variant buys +1 DV and is a one-line data change.
2. **Grenade price and stock** for the Bombardier's refill economy are
   untouched; the build buys relevance, not supply.
3. **What Toughness means** is still undefined (no HP, no resistances); the
   builds give it low priority for that reason.

## Files

- NEW `Assets/Scripts/Gameplay/Bootstrap/StartingBuild.cs` - defs, registry, validator.
- NEW `Assets/Scripts/Gameplay/Bootstrap/StartingBuildService.cs` - applier + Classic fallback.
- NEW `Assets/Scripts/Gameplay/Bootstrap/StartingBuildMenu.cs` - model, card, prompt policy.
- NEW `Assets/Resources/Content/Data/Builds/StartingBuilds.json` (+ folder meta).
- NEW `Assets/Scripts/Presentation/UI/StartingBuildMenuController.cs`, `StartingBuildMenuUI.cs`.
- MOD `Assets/Scripts/Presentation/UI/BootMenuController.cs` - `NewGameGate`.
- MOD `Assets/Scripts/Presentation/Input/InputHandler.cs` - picker ownership, modal ordering.
- MOD `Assets/Scripts/Presentation/Bootstrap/GameBootstrap.cs` - registry load, prompt decision, post-boot flow.
- MOD `Assets/Scripts/Gameplay/Skills/SkillsPart.cs` - `IsKnownSkillClass`.
- MOD `Assets/Scripts/Shared/Utilities/Diag.cs` - `build` category default-on.
- NEW tests: `StartingBuildTests` (36), `StartingBuildAdversarialTests` (31),
  `StartingBuildMenuTests` (34), `StartingBuildMenuControllerTests` (19).
- MOD `Tools/EditModeRunner/*` - compile the new Presentation controllers.
- NEW `Docs/Design/StartingBuildsProbe/BuildFightSimImpl.cs`, `fight-sim-implemented.txt`,
  `fight-sim-designtime-builds-on-freeze-code.txt`.

## Diag

`build/Applied {id, success, skills, items, errors}` and
`build/Rejected {id, reason}` (default-on category `build`). Query:
`diag_assert category=build kind=Applied`.


## Dedicated throwing start — 16 October 2026

Status: IMPLEMENTED and verified in native Unity 6000.3.4f1. User asks for a throwing starting
build. Bombardier already supplies three disposable projectiles, but grants
Drench Lob/Cold Snap/Calm and no Throwing skills. Add **The Hurler**, leaving
all six existing starts and their historical comparison cohorts unchanged.
This new starter supersedes the prior throwing audit's no-new-start scope by
explicit user request; it is not a retroactive fix to that audit's results.

### Design and verified references

| Contract | Verified source / decision |
| --- | --- |
| Menu entry | `StartingBuilds.json`, after Warden and before Classic; seven choices. Existing five specialist indices stay stable, Classic becomes row7. |
| Attributes | Strength20 / Agility20 / Toughness16 / Ego14, same70-point even-score budget. HP remains40; no claim that Toughness raises it. |
| Supplies | Equipped dagger first, three carried daggers, equipped cloak, two healing tonics, two dried meat. The common farming kit still comes from bootstrap. No new blueprints or restocking rules. |
| Skills | Sure Grip (`ThrowingSkill`), Measured Throw, Recovering Step, granted in that order through `StartingBuildService`; two initial hotbar buttons. Driving Throw and Focused Splash remain earned one-point options. |
| Identity | Prepared physical throws and recovery; low protection and finite blades. Recovering Step needs an adjacent eligible thrown item and may lead into danger. No returning-weapon or infinite-ammo promise. |
| Application | Existing service validates first, equips before adding ammunition, grants abilities in authored order and rejects duplicate application. Test exact equipped-versus-carried identities because identical blueprints may stack. |
| Presentation | Existing menu supports1–9 and directional confirmation. New card must fit50columns and22content rows. No separate menu implementation. |
| Native proof | Existing `SpreadDiscoveryNativeBatch.LaunchOpening("hurler",64,"bootstrap")` exercises actual New game/picker and authored starting stats, custody, skills/hotbar and free selection with private save isolation. This is startup evidence, not an expedition. |

### Implementation steps

1. Write and run failing roster, navigation and focused Hurler tests. Include
   exact ammunition/equipment, skill ownership and hotbar order, paid skill
   progression, prepare/throw/recovery mechanics and save roundtrip controls.
2. Add the single authored build and concise honest card text. Use existing
   services and skills; change production C# only if a real integration defect
   is reproduced. Confirm parsed existing build objects are identical.
3. Run native Unity focused and affected starting-build/throwing/save/controller
   tests; independently review the diff and counter-checks.
4. Run the ordinary bootstrap route, inspect the card and starting hotbar at
   Deck resolution, restore editor/save settings, record exact evidence and
   limits, then fetch/rebase/push main.

Readiness: 🟢 skills, item blueprints, menu, carried/equipped dagger custody,
save roundtrip and native startup verified. No new models or animation are
needed because every kit item already has presentation assets.

### Implementation and verification log

This is CoO-original starter content using the implemented Throwing tree, not
a claim of Qud build parity. The only production change is the `hurler` entry
in `StartingBuilds.json`; the existing application, inventory, skill and menu
services required no changes. The authored skills are:

```json
"Skills": ["ThrowingSkill", "Throwing_MeasuredThrow", "Throwing_RecoveringStep"]
```

| Sweep correction / scope | Resolution |
| --- | --- |
| Bombardier might already be a throwing start | Its three projectiles accompany three spells; none grants Throwing. Keep it intact and add a distinct physical-throwing start. |
| Reusable ammunition might merge into the equipped backup | Equip the first dagger, then grant the three carried units. Integration tests prove separation, one-unit throwing and exact-unit recovery without auto-equipping or duplication. |
| Roster has six entries | Seven now; Hurler is digit6, Classic digit7. Production navigation and native scenario launchers resolve the roster dynamically. Historical five-/six-build comparison results remain historical. |
| Old throwing audit excluded new starts | Explicitly superseded by this request. No additional production mechanics, balance rewrite or new supply economy added. |

Tests ran in the real Unity editor, not the headless stub runner:

| Gate | Result / receipt |
| --- | --- |
| RED roster/navigation before content | 6 completed: 3 expected missing-entry/index failures, 3 controls passed. Job `2880b9fc24de4c6fb4932fa966a6eb8d`; `Verification/Throwing/hurler-roster-red.json`. |
| RED focused behavior before content | 6 completed: 5 expected missing-Hurler failures, unchanged Bombardier control passed. Job `3d7aa6edb9d94941b892a614ea860e40`; `Verification/Throwing/hurler-focused-red.json`. |
| First GREEN affected start suites | 144/144 passed, 0 failed/skipped. Job `e62643ce1a364412b8be4b3f2564e772`; `Verification/Throwing/hurler-start-green.json`. |
| Review reinforcement and throwing regression | 177/177 passed, 0 failed/skipped: starting-build, menu, adversarial, controller, focused Hurler and ThrowingSkillTests. Added the card-height/content check and included existing 32 throwing cases. Job `9a5037657a89493ba87396d9fb454243`; `Verification/Throwing/hurler-reviewed-green.json`. |
| Native 1280×800 startup | `Verification/SpreadDiscoveryExpeditions/Native/ecc9954e3301433db95f6fbc9b75b734/report.json`: 5/5 named checks, 0 failures/errors; two screenshots inspected. |
| Native 1280×720 startup | `Verification/SpreadDiscoveryExpeditions/Native/287f5793293f47e495701f895bcd701c/report.json`: 5/5 named checks, 0 failures/errors; two screenshots inspected. |

The native checks use ordinary New Game → digit6 → Enter with private save
isolation. Both verify seed64, ordinary spawn, 40HP, 50drams, exact attributes,
equipped/carried ownership, the common farming kit, three rank1 skills, two
ready hotbar slots, and no turn/energy cost for build selection. The card fits
14 rows (maximum49 columns, within22×50), all seven choices remain visible,
and post-start HUD DV9 matches the card and combat formula. The starting
hotbar displays Measured Throw and Recovering Step. The native session ended
in EditMode; save-root override is null, isolation inactive and exit code0.
The prior Game view size and audit-added custom sizes were restored. Console
error check returned zero entries.

### In-phase and independent review

- 🔵 All six preceding build objects compare identical after JSON parsing.
  Bombardier is also a behavioral counter-check. Existing starts and historical
  strategy comparisons have not been silently redefined.
- 🔵 The six new focused cases cover exact kit/skills, both one-point purchases
  versus zero-point refusal, preserved Bombardier, prepare→throw→recover and
  token-graph save/load with duplicate-start refusal. Distance, occupied landing,
  unthrown lookalike and second-pickup refusal check the recovery exclusions.
- 🔵 The independent reviewer checked roster callers, authored data, item
  ownership, progression and card wrapping; no actionable production findings.
  Review added a card-height/content regression alongside the existing width
  test. An apparent DV mismatch during visual inspection was ruled out by
  reading the original screenshot and verifying `CombatSystem.GetDV` directly.
- 🧪 Can verify: the named native startup checks, inspected screenshots, and
  the specific integration/counter-check assertions above. The focused prepare
  fixture invokes the owner-turn hook explicitly; it does not itself prove
  native action scheduling. Existing throwing tests cover that system separately.
- 🧪 Cannot verify from this work: expedition balance, sustained ammunition
  economy, physical Steam Deck input/performance, or a newly packaged Deck
  build. Native startup reports stop before combat; no campaign was played to
  completion. No such claims are made.

Files changed: `StartingBuilds.json`, `StartingBuildTests.cs`,
`StartingBuildMenuTests.cs`, new `ThrowingStartingBuildTests.cs` and handwritten
`.meta`, this living doc, four Unity test receipts, and the two native report/
screenshot sets. No production C# or blueprint changes were necessary.
