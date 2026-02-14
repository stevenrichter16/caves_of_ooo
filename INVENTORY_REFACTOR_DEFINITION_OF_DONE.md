# Inventory Refactor Definition Of Done

Last updated: 2026-02-14

## Status Legend
- `DONE`: end-state criteria fully met.
- `IN_PROGRESS`: partially implemented, not yet meeting end-state criteria.
- `NOT_STARTED`: no implementation yet for the criterion.

## Definition Of Done Checklist

1. **Single execution model for all inventory mutations**
- End-state criteria:
- All live UI/input flows use `InventorySystem.ExecuteCommand(...)` as the only mutating entrypoint.
- No runtime fallback calls to direct mutators (`InventorySystem.Pickup/Drop/Equip/...`) remain in rendering/input code.
- Status: `DONE`
- Current evidence:
- Command-first routing is active in `Assets/Scripts/Rendering/InputHandler.cs`, `Assets/Scripts/Rendering/PickupUI.cs`, and `Assets/Scripts/Rendering/InventoryUI.cs`.
- Rendering/input fallback direct mutator calls were removed; command failures now surface through command results/logging.

2. **Equip planner is the single source of truth**
- End-state criteria:
- Equip preview and equip execution share one plan build path.
- Slot claim/displacement logic is not duplicated in `InventorySystem`.
- Status: `DONE`
- Current evidence:
- `Assets/Scripts/Core/Inventory/Planning/EquipPlanner.cs` and rules are implemented.
- `Assets/Scripts/Core/InventorySystem.cs` preview path is planner-driven (`PreviewDisplacements`).
- `Assets/Scripts/Core/Inventory/Commands/EquipCommand.cs` execution path is planner-driven for body-aware equip.
- Legacy duplicated slot-claim/displacement logic was removed from `InventorySystem`.

3. **Transactional command safety**
- End-state criteria:
- Mutating commands implement explicit transaction steps with `Do/Undo`.
- Partial failures rollback to consistent pre-command state.
- Status: `IN_PROGRESS`
- Current evidence:
- `InventoryTransaction` is active in command execution and container transfer commands now perform transactional `Do/Undo` steps.
- `TakeFromContainerCommand` and `PutInContainerCommand` no longer fully delegate to legacy static wrappers.
- `PickupCommand`, `DropCommand`, `DropPartialCommand`, and `PerformInventoryActionCommand` now execute command-internal mutation/event flows without legacy fallback wrappers.
- `DropCommand` and `PutInContainerCommand` now reuse command-internal unequip flow (`UnequipCommand`) instead of directly calling legacy unequip mutators.
- `UnequipCommand` now performs command-internal event/stat/unequip mutation flow and rollback without direct `InventorySystem.UnequipItem(...)` delegation.
- `EquipCommand` and `AutoEquipCommand` now execute planner/inventory mutation logic directly in command code (no direct `InventorySystem.Equip/AutoEquip` delegation).
- `PickupCommand` auto-equip path is now command-native (`AutoEquipCommand`) instead of direct `InventorySystem.AutoEquip(...)` bridging.
- `DropCommand` and `DropPartialCommand` now explicitly fail when actor has no valid zone cell, preventing no-cell item loss scenarios.
- Equip/unequip rollback now uses deterministic direct state restoration helpers in `Assets/Scripts/Core/Inventory/Commands/UnequipCommand.cs` (no event-veto-dependent rollback path).
- Equip stacked-item rollback now removes transient split entities before stack restoration, preventing zero-count ghost stack entries.
- Unequip rollback now restores equip bonuses through transaction undo, ensuring stat parity after rolled-back nested commands.
- Equipped-state detection in `DropCommand`, `PutInContainerCommand`, and `UnequipCommand` now uses `UnequipCommand.CaptureEquippedState(...)` snapshots, hardening multi-slot/body-mode rollback behavior against slot-cache drift.
- Remaining gap: verify and harden rollback determinism across edge-case event veto/stack-split/equipment scenarios.

4. **`InventorySystem` reduced to facade/compatibility layer**
- End-state criteria:
- `InventorySystem` delegates to command/planner services.
- Legacy duplicated operational logic is removed or contained as compatibility wrappers only.
- Status: `DONE`
- Current evidence:
- Mutating methods in `Assets/Scripts/Core/InventorySystem.cs` now delegate to command execution (`Pickup`, `Drop`, `DropPartial`, `Equip`, `UnequipItem`, `AutoEquip`, `TakeFromContainer`, `PutInContainer`, `PerformAction`).
- `TakeAllFromContainer` is command-routed internally via `TakeFromContainerCommand` per item.
- Duplicated legacy mutation internals were removed from `InventorySystem`, leaving query helpers and compatibility wrappers.

5. **Container flows are first-class and command-routed**
- End-state criteria:
- UI supports explicit container selection (not implicit first-container behavior).
- Take/put actions run via command executor in normal gameplay flows.
- Status: `DONE`
- Current evidence:
- Input pickup now attempts container take-all when no loose items exist.
- Inventory item popup now exposes one `put_container` action per nearby container, with explicit target container command execution.
- Multi-container `g` take flow now opens an explicit container picker UI (`Assets/Scripts/Rendering/ContainerPickerUI.cs`) and runs command-routed transfers against the selected container.

6. **Regression test coverage for refactor parity**
- End-state criteria:
- EditMode tests cover command routing parity for pickup/drop/equip/unequip/container/item actions.
- Tests validate planner preview equals execution intent.
- Tests validate rollback behavior under forced failures.
- Status: `IN_PROGRESS`
- Current evidence:
- Existing test suite exists (`Assets/Tests/EditMode`).
- Added container command rollback/parity tests in `Assets/Tests/EditMode/InventorySystemTests.cs`.
- Added command-routing tests for equip/unequip/item-action and a preview-displacement parity test for equip execution intent in `Assets/Tests/EditMode/InventorySystemTests.cs`.
- Added edge-case rollback/parity tests for locked-container equipped-item preservation and auto-equip cancellation behavior in `Assets/Tests/EditMode/InventorySystemTests.cs`.
- Added no-zone-cell drop/drop-partial validation tests to assert no unintended unequip/split mutation in `Assets/Tests/EditMode/InventorySystemTests.cs`.
- Added rollback resilience tests proving equip/unequip transaction rollback is not blocked by `BeforeUnequip`/`BeforeEquip` veto hooks in `Assets/Tests/EditMode/InventorySystemTests.cs`.
- Added rollback tests for stack-split equip restoration (no ghost stack entries) and equip-bonus restoration after rolled-back unequip in `Assets/Tests/EditMode/InventorySystemTests.cs`.
- Added command edge-case parity tests for unequip-veto interactions in drop/container flows, equip displacement blocked by unequip-veto, and stacked-item auto-equip command behavior in `Assets/Tests/EditMode/InventorySystemTests.cs`.
- Added body-mode multi-slot rollback tests for `DropCommand` and `PutInContainerCommand` cancellation/full-container flows in `Assets/Tests/EditMode/InventorySystemTests.cs`.
- Added planner invalid-target tests (foreign actor target body part and abstract target body part) for equip/preview paths in `Assets/Tests/EditMode/InventorySystemTests.cs`.
- Remaining gap: expand parity/rollback cases for additional command combinations and edge-case planner scenarios.

7. **Fallback branch removal**
- End-state criteria:
- Command fallback branches in rendering/input are removed.
- Failures surface through command results and logs/messages only.
- Status: `DONE`
- Current evidence:
- Fallback branches were removed from `Assets/Scripts/Rendering/InputHandler.cs`, `Assets/Scripts/Rendering/PickupUI.cs`, and `Assets/Scripts/Rendering/InventoryUI.cs`.

8. **Documentation closeout**
- End-state criteria:
- This file and `DESIGN_REFACTOR.md` reflect final architecture, migration summary, and extension points.
- Status: `DONE`
- Current evidence:
- `DESIGN_REFACTOR.md` now contains final architecture documentation, runtime mutation flow summary, rollback invariants, extension points, and migration summary.
- This checklist remains the canonical completion/status tracker and completion gate.

## Remaining Work Queue (Ordered)

1. Add/EditMode tests for:
- command parity (old vs new behavior),
- planner preview/execution parity,
- rollback correctness.
2. Verify and harden rollback determinism across remaining edge-case event veto/stack-split/equipment scenarios.
3. Final refactor completion sign-off once items 1-2 are fully complete and validated.

## Refactor Completion Gate

The refactor is complete only when checklist items `1` through `8` are all `DONE`.
