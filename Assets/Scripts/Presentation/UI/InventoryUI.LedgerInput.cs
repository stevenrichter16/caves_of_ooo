using System;
using UnityEngine;
using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    public partial class InventoryUI
    {
        private Vector2 _ledgerLastPointer = new Vector2(float.NaN, float.NaN);
        private bool LedgerHasPopup => _displaceConfirm != null || _equipPopup != null || _itemActionPopup != null
            || _modTargetPopup != null || _grimoirePicker != null || _ledgerCommandsOpen || _loadoutNameEditing;

        // All pointer input is resolved against the same rectangles that paint.
        // Returning true consumes every click, including blank/modal-backdrop clicks.
        private bool HandleLedgerPointer()
        {
            // Keep position, button edges and wheel on one backend. Legacy mouse
            // state may remain stale while the Input System owns the native device.
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && !mouse.enabled) mouse = null;
            Vector2 screen = mouse != null ? mouse.position.ReadValue() : (Vector2)Input.mousePosition;
            bool moved = screen != _ledgerLastPointer;
            _ledgerLastPointer = screen;
            bool inside = LedgerPoint(screen, out var point);
            int hitIndex = inside ? LedgerHitAt(point) : -1;
            bool click = mouse != null ? mouse.leftButton.wasPressedThisFrame : Input.GetMouseButtonDown(0);
            if (hitIndex >= 0)
            {
                var hit = _ledgerHits[hitIndex];
                if (click) { ActivateLedgerHit(hit); return true; }
                if (moved && SelectLedgerHit(hit)) Render();
            }
            else if (click) { if (LedgerHasPopup && !inside) CancelLedgerPopup(); return true; }
            float wheel = mouse != null ? mouse.scroll.ReadValue().y : Input.mouseScrollDelta.y;
            if (inside && Mathf.Abs(wheel) > .01f)
            {
                MoveLedgerSelection(wheel > 0 ? -1 : 1); Render(); return true;
            }
            return click;
        }

        private int LedgerHitAt(Vector2 point)
        {
            for (int i = _ledgerHits.Count - 1; i >= 0; i--)
                if (_ledgerHits[i].Bounds.Contains(point)) return i;
            return -1;
        }

        private bool SelectLedgerHit(LedgerHit hit)
        {
            switch (hit.Kind)
            {
                case LedgerHitKind.Equipment:
                    if (_panel == PANEL_EQUIPMENT && _equipCursorIndex == hit.Index) return false;
                    _panel = PANEL_EQUIPMENT; _equipCursorIndex = hit.Index; return true;
                case LedgerHitKind.LoadoutRow:
                    return SelectLoadout(hit.Index);
                case LedgerHitKind.Item:
                    if (_panel == PANEL_INVENTORY && _cursorIndex == hit.Index) return false;
                    _panel = PANEL_INVENTORY; _cursorIndex = hit.Index; return true;
                case LedgerHitKind.TinkerRow:
                    if (_tinkerCursorIndex == hit.Index) return false; _tinkerCursorIndex = hit.Index; return true;
                case LedgerHitKind.CraftRow:
                    if (_craftCursorIndex == hit.Index) return false; _craftCursorIndex = hit.Index; return true;
                case LedgerHitKind.AbilitySlot:
                    if (_abilitiesCursorSlot == hit.Index) return false; _abilitiesCursorSlot = hit.Index; return true;
                case LedgerHitKind.PopupRow:
                    if (_ledgerCommandsOpen) { if (_ledgerCommandIndex == hit.Index) return false; _ledgerCommandIndex = hit.Index; return true; }
                    if (_equipPopup != null) { if (_equipPopup.CursorIndex == hit.Index) return false; _equipPopup.CursorIndex = hit.Index; return true; }
                    if (_itemActionPopup != null)
                    {
                        if (_itemActionPopup.InBodyPartPicker) { if (_itemActionPopup.BodyPartCursor == hit.Index) return false; _itemActionPopup.BodyPartCursor = hit.Index; }
                        else { if (_itemActionPopup.CursorIndex == hit.Index) return false; _itemActionPopup.CursorIndex = hit.Index; }
                        return true;
                    }
                    if (_modTargetPopup != null) { if (_modTargetPopup.CursorIndex == hit.Index) return false; _modTargetPopup.CursorIndex = hit.Index; return true; }
                    if (_grimoirePicker != null) { if (_grimoirePicker.CursorIndex == hit.Index) return false; _grimoirePicker.CursorIndex = hit.Index; return true; }
                    return false;
                case LedgerHitKind.DisplaceYes: if (_displaceConfirm == null || _displaceConfirm.CursorOnYes) return false; _displaceConfirm.CursorOnYes = true; return true;
                case LedgerHitKind.DisplaceNo: if (_displaceConfirm == null || !_displaceConfirm.CursorOnYes) return false; _displaceConfirm.CursorOnYes = false; return true;
                default: return false;
            }
        }

        private void ActivateLedgerHit(LedgerHit hit)
        {
            SelectLedgerHit(hit);
            switch (hit.Kind)
            {
                case LedgerHitKind.Tab: _panel = hit.Index; Render(); return;
                case LedgerHitKind.LoadoutRow: Render(); return;
                case LedgerHitKind.LoadoutAction: ExecuteLoadoutAction(hit.Index); return;
                case LedgerHitKind.Equipment:
                    if (_equipSlots != null && hit.Index >= 0 && hit.Index < _equipSlots.Count) OpenEquipPopup(_equipSlots[hit.Index]); return;
                case LedgerHitKind.Item:
                    if (hit.Index >= 0 && hit.Index < _rows.Count && !_rows[hit.Index].IsHeader && _rows[hit.Index].Item != null) OpenItemActionPopup(_rows[hit.Index].Item); return;
                case LedgerHitKind.TinkerRow: Render(); return;
                case LedgerHitKind.AbilitySlot: OpenGrimoirePicker(hit.Index); return;
                case LedgerHitKind.CraftRow: ToggleCraftPickUnderCursor(); return;
                case LedgerHitKind.TinkerMode:
                    _tinkeringMode = (TinkeringMode)hit.Index; _tinkerCursorIndex = _tinkerScrollOffset = 0; Rebuild(); Render(); return;
                case LedgerHitKind.CraftMode: SwitchCraftingMode((CraftingMode)hit.Index); return;
                case LedgerHitKind.ClearPicks: ClearCraftPicks(); return;
                case LedgerHitKind.CraftCommit:
                    if (_panel == PANEL_TINKERING)
                    {
                        if (_tinkeringMode == TinkeringMode.Mod) OpenModTargetPopupForSelectedRecipe();
                        else { TryCraftSelectedRecipeViaCommand(); Rebuild(); Render(); }
                    }
                    else ExecuteCraft(hit.Index != 0);
                    return;
                case LedgerHitKind.ClearAbility:
                    GetAbilitiesPart()?.AssignAbilityToSlot(Guid.Empty, _abilitiesCursorSlot); Render(); return;
                case LedgerHitKind.DisplaceYes: ConfirmDisplacement(); return;
                case LedgerHitKind.DisplaceNo: _displaceConfirm = null; Render(); return;
                case LedgerHitKind.Cancel: CancelLedgerPopup(); return;
                case LedgerHitKind.Close: Close(); return;
                case LedgerHitKind.Details: _ledgerDetailsRequested = true; return;
                case LedgerHitKind.PopupRow:
                    if (_ledgerCommandsOpen) { ExecuteLedgerCommand(hit.Index); return; }
                    if (_equipPopup != null)
                    {
                        if (_equipPopup.CursorOnRemove) UnequipFromPopup(); else EquipFromPopup(_equipPopup.CursorItemIndex);
                    }
                    else if (_itemActionPopup != null)
                    {
                        if (_itemActionPopup.InBodyPartPicker) EquipToBodyPart(hit.Index); else ExecuteItemAction(hit.Index);
                    }
                    else if (_modTargetPopup != null) ApplySelectedModToPopupTarget(hit.Index);
                    else if (_grimoirePicker != null) ConfirmGrimoirePickerSelection(hit.Index);
                    return;
            }
        }

        private void CancelLedgerPopup()
        {
            if (_loadoutNameEditing) _loadoutNameEditing = false;
            else if (_ledgerCommandsOpen) _ledgerCommandsOpen = false;
            else if (_displaceConfirm != null) _displaceConfirm = null;
            else if (_itemActionPopup != null && _itemActionPopup.InBodyPartPicker) _itemActionPopup.InBodyPartPicker = false;
            else if (_equipPopup != null) _equipPopup = null;
            else if (_itemActionPopup != null) _itemActionPopup = null;
            else if (_modTargetPopup != null) _modTargetPopup = null;
            else if (_grimoirePicker != null) { _grimoirePicker.CancelWithoutCallback = true; CloseGrimoirePicker(); return; }
            Render();
        }

        private void MoveLedgerSelection(int delta)
        {
            if (_loadoutNameEditing) return;
            if (_ledgerCommandsOpen) _ledgerCommandIndex = Mathf.Clamp(_ledgerCommandIndex + delta, 0, _ledgerCommands.Count - 1);
            else if (_displaceConfirm != null) _displaceConfirm.CursorOnYes = !_displaceConfirm.CursorOnYes;
            else if (_equipPopup != null) _equipPopup.CursorIndex = Mathf.Clamp(_equipPopup.CursorIndex + delta, 0, Math.Max(0, _equipPopup.TotalRows - 1));
            else if (_itemActionPopup != null)
            {
                if (_itemActionPopup.InBodyPartPicker) _itemActionPopup.BodyPartCursor = Mathf.Clamp(_itemActionPopup.BodyPartCursor + delta, 0, Math.Max(0, _itemActionPopup.BodyParts.Count - 1));
                else _itemActionPopup.CursorIndex = Mathf.Clamp(_itemActionPopup.CursorIndex + delta, 0, Math.Max(0, _itemActionPopup.Actions.Count - 1));
            }
            else if (_modTargetPopup != null) _modTargetPopup.CursorIndex = Mathf.Clamp(_modTargetPopup.CursorIndex + delta, 0, Math.Max(0, _modTargetPopup.TotalRows - 1));
            else if (_grimoirePicker != null) _grimoirePicker.CursorIndex = Mathf.Clamp(_grimoirePicker.CursorIndex + delta, 0, Math.Max(0, _grimoirePicker.Grimoires.Count - 1));
            else if (_panel == PANEL_EQUIPMENT) MoveEquipCursor(0, delta);
            else if (_panel == PANEL_INVENTORY) MoveCursor(delta);
            else if (_panel == PANEL_TINKERING) MoveTinkeringCursor(delta);
            else if (_panel == PANEL_CRAFTING) MoveCraftCursor(delta);
            else if (_panel == PANEL_LOADOUTS) MoveLoadoutSelection(delta);
            else _abilitiesCursorSlot = Mathf.Clamp(_abilitiesCursorSlot + delta, 0, ActivatedAbilitiesPart.SlotCount - 1);
        }
    }
}
