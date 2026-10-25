# Inventory action release

Status: native behavioral RED confirmed; minimum production correction is saved for native GREEN. Ordinary replay remains pending. This is separate from the Nella ground rendering packet.

## Observed problem and source sweep

During ordinary Nella Play, `b` successfully planted the prepared bed at (18,6) and closed inventory. The same approximately 100 ms hold then moved the player southwest from (18,7) to (17,8). The intended single inventory action therefore became an extra world move/turn.

The source audit is `/tmp/coo-inventory-action-release-review.md`. Verified seams:

| Premise | Current source and correction |
| --- | --- |
| Inventory selection closes before movement | `InventoryUI.HandleItemActionPopupInput` uses physical `GetKeyboardKeyDown` for alphabetic actions. `ExecuteItemAction` sets the existing pending-paid-turn boolean and closes after successful seed/resource actions. |
| This is not same-frame dispatch | `InputHandler.UpdateInput` returns after `HandleInventoryInput`. The unwanted move occurs on a later update while the key is still held. |
| Existing protection can be reused | `InputHandler._worldActionKeyToRelease` already blocks Normal input until release. Pickup, world-action, container and dialogue paths transfer exact activation provenance; inventory does not. |
| Controller quarantine is insufficient | `NativeGamepadInput.SetContext` quarantines gamepad controls only. `InputHelper.GetKeyboardKey` still sees physical `B`, which maps to southwest. |
| A repeat timer is not a release fence | Paid inventory handling and `EndTurnAndProcess` do not reset the movement timer. A delayed repeat would still accept the held key later. |
| Tests must use real behavior | Factory seed, actual prepared terrain and actual gathered `PlantSeedAt` command; queued `KeyboardState` through full `InputHandler.Update`. No direct command execution, injected action list, or manual release-fence assignment. |

## Bounded implementation plan

After native RED, retain the exact private `ExecuteItemAction(int)` signature and route it to a distinctly named executor that accepts exact activation provenance. Only alphabetic dispatch supplies its pressed key; pointer/selected-row confirmation retains no key. Existing reflection-based fixtures call the single-argument method by name, so changing that signature or overloading it would break compatibility. Preserve a one-shot closing receipt only on the existing successful paid-action branch. Reset it on Open/consumption, preserve it across Close, and consume it into InputHandler's existing release fence before resuming Normal input. A rejected or stale action must not leave a latent key for a later confirmation.

Expected runtime files are `InventoryUI.cs` and `InputHandler.cs`. Do not change planting rules, quantities, payment, movement repeat, controller mapping, throw targeting, popup geometry or art. Qud-style modal selection must remain one intentional action, with ordinary movement available again after release.

## Verification plan

`CavesOfOoo.Tests.InventoryActionReleaseTests` will cover successful actual adjacent planting by `B`, repeated held samples without extra movement/payment, neutral release and fresh `B` movement. Countercases retain a rejected action's exact menu and ownership without payment, cancel without a latent fence, and retry by Return without inheriting the earlier letter. Assertions pin the real menu's `B` action before invoking it.

Use the established EditMode timer bypass so unchanged `Time.time` cannot produce a false pass. Snapshot/restore borrowed input devices, native gamepad state, save/global bindings, render hooks and exact MessageLog queue provenance/counters. Native tests are run only by the parent agent. Existing `GameAuditShortcutTests`, `PaidSelfConsumptionInputTests` and `CompostSupplyInputTests` are the bounded affected regression groups.

## Self-review and evidence

- P2 source finding: inventory lacks the existing activation-release handoff. Parent supplied the ordinary observed movement; this audit did not independently operate Unity.
- Independent source check: `/root/projection_cold_review/look_focus_draft_review` confirmed keyboard/gamepad separation and the existing native release-test patterns. No runtime edits or tests were run by that reviewer.
- Native RED/GREEN, compilation, ordinary held-key replay and final acceptance remain pending. No performance or hardware acceptance claim.

## Tests-first handoff — 2026-10-25

Saved `Assets/Tests/EditMode/Presentation/Input/InventoryActionReleaseTests.cs` and a fresh two-line meta. Four cases use actual PitchpodSeed/Grass/PlantSeedAt, with an explicit assertion that the generated adjacent action is ordinal B. One case is expected to expose held-B movement; three are refusal/cancel/Return controls. These are source expectations, not executed results. The fixture creates its real TurnManager after HotbarSaveFixture so the active clock is correct, and restores exact MessageLog provenance/counters and native input state after nested fixture cleanup.

Pre-implementation correction from independent review: preserve `ExecuteItemAction(int)` for existing reflection callers; use a distinct helper rather than an optional-parameter signature or overload. Production remains unchanged pending the parent's native RED.

Independent frozen-fixture review by `/root/projection_cold_review/look_focus_draft_review`: no concrete compile/API/setup blocker found. Verified actual SeedPart action priorities (PlantSeed20, adjacent19, Examine0) give the intended B row without injected actions, full native keyboard Update delivery, current clock/custody and cleanup order. This is source review only, not compilation or execution.

## Native RED and minimum implementation — 2026-10-25

The parent ran `inventory-action-release-red.xml`/`inventory-action-release-red-summary.json` at **07:15:43–44Z**: **4 cases, 3 passed, 1 failed, 0 skipped**. The failing actual plant case expected the player to remain at (4,4) during the held B sample, but observed (3,5). This is the behavioral RED; no missing API or compile failure is substituted for it.

Implemented only:

- `InventoryUI.cs`: exact single-argument `ExecuteItemAction(int)` remains available for existing callers/reflection. Alphabetic dispatch uses a distinctly named helper. Only a completed paid closing action stores its exact alphabetic key; selected-row/pointer calls store None. The one-shot receipt survives Close, resets on Open, and clears on consumption.
- `InputHandler.cs`: consumes the receipt into the existing `_worldActionKeyToRelease` before CloseInventory/EndTurnAndProcess. Existing Normal-state release handling and movement logic are unchanged.
- Focused fixture: added explicit `Turns.WaitingForInput` and exact `CurrentActor == Player` assertions before held replay, as requested by independent review. These strengthen the original behavior after RED; they were not separately demonstrated failing.

Self-review: refusal never records a closing key; selected-row retry cannot inherit the failed shortcut; mouse calls remain the exact old single-argument API; payment remains the existing bool receipt and scheduler path. No gameplay validity, art, movement-repeat or controller mapping change. Focused/native regression GREEN and ordinary held-shortcut acceptance are still pending.

Independent settled-source review by `/root/projection_cold_review/look_focus_draft_review`: **no actionable P1/P2 found** in the two-file correction. Verified exact physical shortcut provenance, preserved single-argument reflection API, pointer/Return/controller defaults, success-only receipt write, Close/Open/consume lifecycle and consumption before scheduling. Refusal, nonclosing and throw paths do not record the key. The reviewer also checked the two post-RED scheduler assertions. Source-only; no Unity or tests run by the reviewer.

## Completed verification and self-review — 2026-10-25 07:26 UTC

Native RED4:3pass/1behavioral failure before production, expected4,4 but moved3,5.
Final native GREEN124/124 distinct tests,0failed/skipped:4new plus120 affected
shortcut/self-consumption/compost/inventory ledger/controller/examine/material
controls. Explicit WaitingForInput/current-player assertions were strengthened
after RED. `inventory-action-release-*.xml` retain exact native results.

Actual SampleScene Continue loaded the protected fresh-origin after-Nella save.
Ordinary Rain/waits matured crop9193; harvest/pickup supplied real seed9200 and
two more resin. Inventory search and pointer selection opened the actual B
prepared-bed18,6 action. Before:tick3780,18,7. Holding B300ms planted new9201,
consumed one seed and advanced only to3790 without moving. A fresh B90ms after
release moved17,8 at3800. HP32,42drams,equipment and four resin remained intact.
The three named before/after/fresh PNGs are unmodified Unity ScreenCapture.

- 🟡 Fixed: successful inventory letter no longer leaks into later world movement.
- 🔵 Retained exact single-argument reflection API; no gameplay/payment changes.
- 🔵 Independent source and actual image/state reviewers found no significant issue.
- 🧪 Native input proves held samples; screenshots show endpoints after release.
  Actual duration is from CUA input delivery, not inferred from a still image.
- 🧪 Physical Deck and current-package acceptance remain pending. No speedup claim.

F5 checkpoint exported to `/tmp/coo-fresh-opening-after-inventory-release/`
(character20cb263ebed24a8380168954e23981ca, tick3800,17,8Nella).
Quick.sav.gz SHA256 `e0d8d618f3359b967cfa97377f287dade07042752d7b5f5705b3f73e0b817455`.
Owned Play stopped; scene/preferences/view/save override restored07:26:22UTC.
Normal save and backup hashes match. Console0errors/2existing warnings.
Current packages predate this work; no cache deletion claimed.

Files: InventoryUI.cs, InputHandler.cs, new InventoryActionReleaseTests.cs/meta,
this plan, graphics living records and bounded native/ordinary verification.
Continue fresh-origin forge/kitchen next; preserve mixed coating6974 and brew
the remaining FrostLichen alone for tempering. Existing mixed Stoneskin would
protect struck enemies. Gather two actual raw glade grains and kitchen pulp,
preserve2FireClay for pan repair. Then the Nella scene-composition cycle and
seed65 visual counter remain.
