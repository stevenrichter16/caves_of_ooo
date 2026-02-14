# Inventory Refactor Plan (Active)

## Canonical Tracker
The canonical source of truth for inventory refactor completion is:

`/Users/steven/caves-of-ooo/INVENTORY_REFACTOR_DEFINITION_OF_DONE.md`

Use that file for:
- end-state criteria,
- current status (`DONE` / `IN_PROGRESS` / `NOT_STARTED`),
- ordered remaining-work queue,
- final completion gate.

## Current Summary
- Command pipeline foundation is implemented (`InventoryContext`, `IInventoryCommand`, `InventoryCommandExecutor`, `InventoryTransaction`).
- Live pickup/drop/equip/unequip/item-action flows are command-routed with no rendering/input fallback branches.
- Equip planner/rules are implemented and integrated into displacement preview + body-part-aware equip path.
- Container wiring exists (take-all-on-`g` when no loose items; explicit per-container put actions in inventory popup).

## In-Scope End State
1. Remove fallback direct mutator calls from rendering/input.
2. Move command internals to transactional `Do/Undo` implementations.
3. Complete planner unification (including auto-equip path).
4. Add explicit container picker UI for multi-container/multi-item interactions.
5. Add EditMode regression tests for command parity, planner parity, and rollback.
6. Reduce `InventorySystem` to a thin facade/delegation layer.

## Process Rule
Any change to implementation sequencing or completion status must update:

`/Users/steven/caves-of-ooo/INVENTORY_REFACTOR_DEFINITION_OF_DONE.md`
