# Combat supplies: second uses

Status: implemented and verified, 8 October 2026. Base main `cb3b4ef35`.

## Aim and design prompt

Find obtainable supplies whose physical properties suggest a useful combat action. Prefer a new decision involving position, preparation, rescue or escape over another damage consumable. Trace acquisition, existing consumers, action costs, enemy behavior and saves before choosing. Preserve competing uses and make consequences legible in inventory and the world.

The reference is the user's qualitative Caves of Qud goal: ordinary finds reward informed improvisation. This is original Caves of Ooo design, not a source or numerical parity claim.

## Brainstorm and selection

| Candidate | Decision and reason |
|---|---|
| Knotflax cord foot snare | Implement. A visible, single-use trap on adjacent unoccupied ground briefly prevents movement. Any creature can trigger it, including the player; attacks and spells remain possible. Costs a coil and an action; competes with repairs, bandaging and bindings. |
| Fire clay emergency smothering | Implement. Spend one clay and an action to extinguish an already burning self, nearby creature or world object without adding wetness. Useful when water would worsen electrical danger. No lasting fire immunity or cleansing of unrelated ailments. |
| Water-vessel drenching | Implement. Spend one water and an action to wet self or an adjacent creature, extinguishing current flames if present. Supports rescue, ignition prevention and electrical preparation; water remains a finite carried resource. |
| Ink in the eyes | Defer. No complete blindness/sight substrate or eye metadata exists. Needs honest player/AI sight symmetry, not confusion relabeled as blindness. |
| Spurgrass caltrops | Defer this tranche. Overlaps the chosen ground-control role; Hobbled is a dodge penalty, not movement slowing. |
| Slipsedge lubricant to escape hooks | Prune: an embedded hook does not intuitively slip free; existing healing/tonic uses remain. |
| Timber door wedge | Prune: most ordinary pursuers cannot open doors, so this adds little without related AI work. |

## Verification sweep and corrections before implementation

| Premise checked | Source and correction |
|---|---|
| The supplies can be obtained | Knotflax exists in repair cultivation, mender stock and crop yields. Fire clay exists in merchant repair supplies, clay banks and Marlroot processing. Reusable water vessels have live stock/crop sources. Targeted discovery tests will check the shipped paths. |
| Existing traps only trigger on creatures | `TriggerOnStepPart` has no general Creature check. The cord trap must explicitly filter props, validate current contact and consume only once. No inherited faction protection. |
| Hobbled slows a pursuer | False: `HobbledEffect` reduces DV only. `RootedEffect.AllowMovement` prevents movement while retaining attacks/casts. Avoid additive-duration exploits for the snare. |
| Existing dousing includes allies | False: `DouseWorldActions.Target` explicitly excludes Creature. It only treats burning world objects. Keep this old action intact and add actual emergency creature use. |
| Water is a generic item stack | False: `WaterTransferActions.Vessel/SetUnits` distinguish water-only charges from pure-water liquid volume and reject malformed ownership/content. New uses must respect both. |
| Wetness is just cosmetic | False: `ThermalPart.TryIgnite` suppresses ignition above 0.35 moisture; `ElectrifiedEffect.OnApply` doubles charge and adds one duration turn above 0.2 moisture. Demonstrate both branches. |
| Inventory success automatically spends a turn | False: native `InventoryUI` has a paid-action allowlist. Route new commands through `CombatUtilityActions` so its existing native turn boundary applies. |
| Effects and item payment are atomic automatically | False: outer action snapshots do not restore arbitrary target effects or world placements. Each new action must join the native transaction and undo its own changes after failures. |
| New world object gets the intended graphics automatically | False: Spread scenery uses exact recipe gates and a validated model pack. Add a cord loop and stakes, with visible/hidden and malformed-owner counterchecks. |

## Implementation prompt and milestones

Implement these three uses through the existing carried inventory command transaction, with bounded exact selection checks, ownership validation and no-op refusal. First add failing player-flow tests, run them red, then implement. Keep item acquisition and old uses intact. Add paired countercases, adversarial rollback/stale-selection/save tests, and native menu/action-cost evidence. Review shared movement, material and rendering behavior before shipping; document limitations rather than claiming general NPC inventory intelligence or environmental immunity.

1. Tests first for placement/trigger, dousing and presentation. Record native RED evidence.
2. Implement independent cord and emergency-dousing services, dispatch through the existing paid utility action boundary.
3. Add contextual carried help, visible snare geometry and exact acquisition evidence. No new vendor flood or player starter gifts.
4. Dedicated adversarial review: invalid/stale selection, ownership, hook exceptions, multi-cell contact, repeated trigger, blocked action, immunity, existing effects, save/expiry and no-op costs.
5. Run affected native EditMode suites and an isolated, reproducible PlayMode scenario using real inventory menus and turns. Preserve the user's save and scene.
6. Cold-eye review, update this living document, commit with the repository template, fetch/rebase and push main.

## Readiness

- Green: ordinary acquisition and existing item consumers; paid inventory transaction; movement entry events; Wet/Burning/Rooted effect substrates; scenery model pipeline.
- Yellow: exact undo of removed effects and callback mutations; root duration/entry timing; native input and presentation integration. Must be tested before completion.
- Out of scope: autonomous NPC choice to use these supplies, complete blindness, generalized traps/tinkering, persistent new crafting recipes.

## Implementation log, review and evidence

The three services now run through `CombatUtilityActions`, preserving the normal before/after inventory hooks and one-action UI payment. Existing cord and clay saved Parts receive the new actions without reconstruction. Water supports both water-only charges and a pure-water liquid vessel; empty vessels remain. A dry outsider's non-rescue drenching is marked **provokes** and changes hostility only after successful commit.

```csharp
if (CordSnareActions.IsCommand(command))
    return CordSnareActions.TryAct(actor, item, zone, command, transaction);
if (EmergencyDousingActions.IsCommand(command))
    return EmergencyDousingActions.TryAct(actor, item, zone, command, transaction);
```

The snare is a saved physical object, inert until its inventory transaction commits. The first actual creature footprint contact consumes it before dispatching the root. Existing roots are not lengthened. The ordinary movement model has no general airborne capability; the snare introduces no invented flight exception. Terrain remains allowed underneath placement, while objects/creatures prevent laying there. The two new cord models contain 31 cuboids each. All 64 previous source models and the palette are byte-for-byte equal after parsing; native source import contains 66 models / 862 boxes.

### Self-review and scope corrections

- 🔵 WetEffect does not itself extinguish Burning. The new service explicitly removes the real flame and cools below the current ignition threshold. Clay performs this rescue without adding wetness and preserves existing wetness, acid and poison.
- 🔵 Rooted prevents voluntary movement, not forced displacement, attacking or casting. Snare inspection states this limit; existing roots retain their duration rather than accumulating holds.
- 🔵 Ordinary actors cannot distinguish who laid a physical loop. There is no faction immunity. Autonomous snare avoidance and attribution/retaliation are not introduced.
- 🟡 Fixed before commit: an exception in the Extinguished observer could suppress later render/receipt notifications after payment. Independent commit callbacks now allow later notifications to finish; paired native regression recorded.
- 🟡 Fixed before commit: failed rescues now restart the exact restored flame's aura, without resurrecting a detached recipient or replacing another status manager.
- 🧪 Native geometry is confirmed; natural encounter balance, novice understanding and long-term enjoyment still require playtesting.

### Verification evidence

Native RED: `native-red.xml`, 71 cases / 68 failed / 3 passed before production. Failures were absent menu actions, missing carried guidance and absent snare blueprint/art; the three passing cases were existing invalid-source rejection. Two initial test-code compile mistakes (missing namespace and internal-field access) were corrected before this behavioral RED run.

Initial GREEN attempt: `native-first-green.xml`, 356 cases / 355 passed / 1 failed. All gameplay, native inventory payment and discovery cases passed. The presentation test wrongly expected explored static scenery to disappear. It now explicitly checks retained fog memory, disappearance when unexplored, and disappearance on consumption. No renderer behavior changed for that correction.

Broad native regression: `native-regression-1104.xml`, **1,104/1,104 passed**, zero skipped. Covers the new supplies, prior combat inventory uses, scenery, traps, roots, liquid vessels, electrical effects and inventory core. A subsequent review isolated after-commit notifications: one paired regression failed RED when an Extinguished observer threw and hid the success receipt; its fix separates post-commit observers. The first targeted GREEN passed 50/50. A second review found that rollback restored the removed flame mechanics but not its stopped fire aura: paired RED was 2 passed / 2 failed, fixed with lifecycle-aware aura resumption. The final mixed run `native-dousing-final-green-with-next-red.xml` contains **54/54 passing dousing cases** plus the next tranche's deliberately failing guidance/transaction tests (42 next-tranche cases, 1 passed / 41 failed); it is not an all-green XML.

Live scenario: `Native/968e6a85ce4e43278148985457bf20a3/report.json`, **11/11 checks passed, zero errors**. The reproducible menu is **Caves Of Ooo / Scenarios / UI / Combat Supply Second Uses Native Audit**. It owns isolated saves and restores scene setup/input/camera state. The scenario provides one cord, one clay and a two-charge waterskin on a controlled empty floor; it does not claim those supplies were naturally acquired. Separate discovery tests harvest actual installed cord bundles, ripe knotflax and clay banks in `Overworld.2.6.0` and execute the resulting supply actions.

**Can verify (script-observable):** ordinary I/arrow/Enter inventory selections each pay one unit and one +10-tick action; native walking catches the player, blocks movement temporarily, then permits walking after expiry; the consumed snare loses its world model; clay extinguishes without Wet; water extinguishes with Wet. EditMode countercases cover outsider hostility, party rescue, save state, forced/multi-cell contact, malformed ownership, callbacks, and rollback.

**Cannot verify (visual / feel):** these controlled scenarios do not prove natural encounter difficulty or that a new player will infer every tactic. The inspected screenshots show a distinct pale loop beside the 3D player and readable clay/water menu actions; geometry submission alone is not a visual quality claim. The current native scenario does not exercise autonomous NPC use or a natural pursuit escape.

### Files changed

- New gameplay: `CordSnareActions`, `CordSnarePart`, `EmergencyDousingActions`; new exact `KnotflaxSnare` blueprint. Parsed blueprint comparison confirms every existing object unchanged.
- Integration: `CombatUtilityActions`, `MaterialUseDescription`.
- Presentation: scenery recipe/source/library, two new original source/native mesh and prefab variants, procedural build and preview scripts; reviewed hash/count pins.
- Tests: CombatSupply snare/dousing initial and adversarial fixtures, discovery, inventory UI and native presentation fixtures; existing scenery count/hash pins.
- Native witness: `CombatSupplyNativePlayer`, `CombatSupplyNativeBatch`, isolated report/screenshots and original art preview.

