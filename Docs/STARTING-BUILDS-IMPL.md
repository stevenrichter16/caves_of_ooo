# Starting builds - implementation

**Status:** implemented and unit-tested outside Unity; **not yet run in the
Unity editor.** Design and evidence: `Docs/Design/STARTING-BUILDS.md` and
`Docs/Design/StartingBuildsProbe/`.
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
