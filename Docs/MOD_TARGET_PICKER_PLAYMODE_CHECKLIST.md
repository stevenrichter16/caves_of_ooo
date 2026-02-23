# Mod Target Picker Playmode Checklist

This checklist validates the current mod flow:

1. `InventoryUI` mod recipe list renders.
2. Pressing Enter on a mod opens target picker popup.
3. Selecting a target runs `ApplyModificationCommand`.
4. Command/service constraints behave correctly (target required, stack blocked, etc.).

## Preconditions

- You can enter play mode without compile errors.
- Player has:
  - `InventoryPart`
  - `BitLockerPart`
  - at least one known mod recipe (for now: `mod_sharp_melee`)
  - enough bits for the recipe (for sharp mod: `BC`)
- Player owns at least one compatible melee weapon (e.g. `PlainKnife`).

If no mod recipes appear, verify recipe load and knowledge first:
- `Assets/Resources/Content/Data/Tinkering/Recipes_V1.json`
- `Assets/Scripts/Gameplay/Tinkering/TinkerRecipeRegistry.cs`
- `Assets/Scripts/Gameplay/Parts/BitLockerPart.cs`

## Smoke Test (Keyboard)

1. Press `I` to open inventory.
2. Move to Tinkering tab with `RightArrow`/`Tab`.
3. Press `M` for Mod mode.
4. Confirm recipe list is visible (not placeholder text).
5. Select a mod recipe with `Up/Down`.
6. Press `Enter`.
7. Confirm popup appears: title like `Apply Sharp -> choose target`.
8. Move in popup with `Up/Down`.
9. Press `Enter` on a target.
10. Confirm popup closes and inventory refreshes.

Expected:
- Target item updates (for sharp: gains `ModSharp` and `PenBonus +1`).
- Bit cost consumed.
- Message log includes successful apply text from `TinkeringService`.

## Smoke Test (Mouse)

1. Open same popup (`I` -> Tinkering -> `M` -> `Enter` on recipe).
2. Hover a target row.
3. Click row to apply.

Expected:
- Hover changes popup cursor.
- Click applies mod and closes popup.
- Clicking outside popup closes popup without applying.

## Edge Case Tests

## A) No Compatible Target

Setup:
- Remove all compatible melee weapons from player inventory/equipment.

Action:
- Try opening target picker on a mod recipe.

Expected:
- No popup.
- Message log: `No compatible target item for this mod.`

## B) Stacked Target Blocked

Setup:
- Make a compatible item stack count > 1.

Action:
- Try applying mod to that stack.

Expected:
- Command fails validation.
- No bits consumed.
- Reason includes `Split the stack before applying a modification.`

## C) Equipped Target

Setup:
- Equip compatible item on player body slot.

Action:
- Apply mod to equipped item via popup.

Expected:
- Works the same as carried item.
- Target row/details show `[E]`.

## D) Multiple Compatible Targets

Setup:
- Carry/equip at least 2 compatible items.

Action:
- Choose non-first item in popup and apply.

Expected:
- Only selected item gets modified.
- Other compatible items remain unchanged.

## E) Already Modified Item

Setup:
- Apply `mod_sharp_melee` once to target.

Action:
- Try applying same mod again.

Expected:
- Fails with reason containing `already`.
- No second cost spend.

## Regression Checks

1. Build mode still crafts (`B` mode in Tinkering + `Enter`).
2. Inventory item action popup still opens from inventory panel (`Enter` on item).
3. Disassemble action still works for valid items.
4. Equip/unequip and container item actions still work.

## Debug Signals to Watch

- Command failure logs:
  - prefix: `[Inventory/Refactor]`
  - source: `InventoryUI.LogCommandFailure(...)`
- Tinkering debug hotkey logs:
  - prefix: `[Tinkering/Debug]`
  - source: `InputHandler` F9 path

If popup does not appear when pressing `Enter` in Mod mode, inspect:
- `OpenModTargetPopupForSelectedRecipe()`
- `BuildCompatibleModTargets(...)`
- `TinkeringService.CanApplyModificationTarget(...)`

File references:
- `Assets/Scripts/Presentation/UI/InventoryUI.cs`
- `Assets/Scripts/Gameplay/Inventory/Commands/Actions/ApplyModificationCommand.cs`
- `Assets/Scripts/Gameplay/Tinkering/TinkeringService.cs`
- `Assets/Scripts/Gameplay/Tinkering/Mods/SharpTinkerModification.cs`
