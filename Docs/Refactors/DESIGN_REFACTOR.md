# Inventory Refactor Architecture (Closeout)

## Canonical Tracker
The authoritative completion tracker remains:

`/Users/steven/caves-of-ooo/INVENTORY_REFACTOR_DEFINITION_OF_DONE.md`

Use that file for status and completion gating. This file documents architecture and extension seams.

## Final Architecture
- `InventorySystem` is a facade/compatibility layer for mutation APIs.
- All live mutation flows route through `InventorySystem.ExecuteCommand(...)`.
- Command execution pipeline:
  - `InventoryContext` carries actor/zone/body/inventory references.
  - `IInventoryCommand` defines `Validate(...)` + `Execute(...)`.
  - `InventoryCommandExecutor` enforces validation + transactional execution.
  - `InventoryTransaction` commits on success and rolls back on any failure/exception.
- Equip planning:
  - `EquipPlanner` + rules (`TargetPartCompatibilityRule`, `SlotCountRule`, `SlotAvailabilityRule`, `DisplacementRule`) are the body-aware equip decision source.
  - `InventorySystem.PreviewDisplacements(...)` and `EquipCommand` share planner outputs.

## Runtime Mutation Flows
- Pickup/drop/equip/unequip/item-action/container flows from rendering/input are command-routed.
- Multi-container pickup uses explicit `ContainerPickerUI` selection instead of implicit first-container behavior.
- Per-container put actions are exposed in inventory popup actions and routed through `PutInContainerCommand`.
- Auto-equip on pickup runs through `AutoEquipCommand`; failures are non-fatal for pickup.

## Transaction and Rollback Invariants
- Any command that mutates inventory/equipment state must register undo actions for each committed mutation step.
- Rollback order is reverse-application order via `InventoryTransaction`.
- Rollback for equip/unequip is deterministic and does not depend on vetoable `BeforeEquip`/`BeforeUnequip` recursion.
- Stack-split equip rollback restores source stack counts and removes transient split entities from carried/equipped state to avoid ghost entries.
- Drop/drop-partial fail fast when actor has no zone cell, preventing silent item loss.

## Extension Points
- New mutation operation:
  - Add command in `Assets/Scripts/Core/Inventory/Commands/`.
  - Implement strict `Validate(...)`.
  - Register each mutation step with `transaction.Do(..., undo: ...)`.
  - Surface through UI/input by calling `InventorySystem.ExecuteCommand(...)`.
- New equip behavior rule:
  - Add `IEquipRule` implementation in `Assets/Scripts/Core/Inventory/Rules/`.
  - Wire the rule into `EquipPlanner` rule list.
  - Add planner parity and execution tests.
- New UI inventory flow:
  - Keep rendering/input as orchestrators only.
  - Do not call direct inventory mutators from rendering/input.

## Migration Summary
- Removed rendering/input fallback branches to direct mutators.
- Reduced `InventorySystem` mutators to command delegation wrappers.
- Consolidated equip/displacement logic around planner-driven command execution.
- Added command-first container flow wiring (explicit selection + target container operations).
- Hardened rollback behavior for veto and stack-split edge cases.

## Remaining Refactor Work
- Final remaining work and sign-off criteria are tracked only in:

`/Users/steven/caves-of-ooo/INVENTORY_REFACTOR_DEFINITION_OF_DONE.md`
