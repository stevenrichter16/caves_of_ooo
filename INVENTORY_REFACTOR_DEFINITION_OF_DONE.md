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
- Status: `IN_PROGRESS`
- Current evidence:
- `Assets/Scripts/Core/Inventory/Planning/EquipPlanner.cs` and rules are implemented.
- `Assets/Scripts/Core/InventorySystem.cs` uses planner in `PreviewDisplacements`, body-part equip flow, and body-part auto-equip flow.
- Remaining gap: complete consolidation of any remaining non-planner equip-side validation paths.

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
- Remaining gap: remove remaining command-level rollback/bridge calls that still rely on legacy mutators (for example select rollback hooks and pickup auto-equip bridge paths).

4. **`InventorySystem` reduced to facade/compatibility layer**
- End-state criteria:
- `InventorySystem` delegates to command/planner services.
- Legacy duplicated operational logic is removed or contained as compatibility wrappers only.
- Status: `IN_PROGRESS`
- Current evidence:
- `ExecuteCommand(...)` seam exists and planner integration started.
- Large legacy logic still lives inside `InventorySystem`.

5. **Container flows are first-class and command-routed**
- End-state criteria:
- UI supports explicit container selection (not implicit first-container behavior).
- Take/put actions run via command executor in normal gameplay flows.
- Status: `IN_PROGRESS`
- Current evidence:
- Input pickup now attempts container take-all when no loose items exist.
- Inventory item popup now exposes one `put_container` action per nearby container, with explicit target container command execution.
- Multi-container `g` take-all no longer silently defaults; it now reports missing picker support and skips implicit transfer.
- Remaining gap: dedicated picker/flow for multi-container take flows.

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
- Status: `IN_PROGRESS`
- Current evidence:
- End-state checklist exists (this file), but closeout summary is not yet complete.

## Remaining Work Queue (Ordered)

1. Move remaining mutating logic from legacy `InventorySystem` methods into command `Execute(...)` implementations with real `InventoryTransaction` rollback steps.
2. Add dedicated container picker UI/state for multi-container and multi-item take flows.
3. Add EditMode tests for:
- command parity (old vs new behavior),
- planner preview/execution parity,
- rollback correctness.
4. Reduce `InventorySystem` to a delegation facade and remove dead legacy internals.
5. Final documentation closeout and explicit refactor completion sign-off.

## Refactor Completion Gate

The refactor is complete only when checklist items `1` through `8` are all `DONE`.
