using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public partial class InventoryUI
    {
        private string _inventoryQuery = "";
        private bool _inventorySearchActive;
        private Entity _searchReturnItem;
        public bool IsSearching => _inventorySearchActive;
        public string SearchQuery => _inventoryQuery;
        public void HideForDecisionReader() { if (Tilemap != null) Tilemap.ClearAllTiles(); }
        public void RestoreAfterDecisionReader() { if (_isOpen) { Rebuild(); ClampCursor(); Render(); } }
        public string SelectedDecisionDetails()
        {
            if (!_isOpen || _inventorySearchActive) return null;
            if (_itemActionPopup != null)
            {
                var item = _itemActionPopup.Item;
                if (!InventoryDecisionDetails.Owned(PlayerEntity, item)) return null;
                if (_itemActionPopup.CursorIndex >= 0 && _itemActionPopup.CursorIndex < _itemActionPopup.Actions.Count)
                {
                    var action = _itemActionPopup.Actions[_itemActionPopup.CursorIndex];
                    if (action.Command == "put_container") return InventoryDecisionDetails.Container(PlayerEntity, CurrentZone, action.Container, item);
                    if (action.Command == "disassemble") return item.GetDisplayName() + "\n\n" + InventoryDecisionDetails.Disassembly(PlayerEntity, item);
                }
                return DetailsForItem(item);
            }
            if (_modTargetPopup != null && _modTargetPopup.CursorIndex >= 0 && _modTargetPopup.CursorIndex < _modTargetPopup.Targets.Count)
            {
                var item = _modTargetPopup.Targets[_modTargetPopup.CursorIndex];
                return InventoryDecisionDetails.Owned(PlayerEntity, item) ? InventoryDecisionDetails.Modification(item, _modTargetPopup.Recipe) : null;
            }
            if (_equipPopup != null && _equipPopup.CursorItemIndex >= 0 && _equipPopup.CursorItemIndex < _equipPopup.Items.Count)
                return DetailsForItem(_equipPopup.Items[_equipPopup.CursorItemIndex]);
            if (_panel == PANEL_CRAFTING)
            {
                // Current selection is re-read through the existing pure previews.
                BuildCraftingRows();
                string text = _craftingMode == CraftingMode.Forge ? InventoryDecisionDetails.Forge(_forgePreview) : InventoryDecisionDetails.Brew(_brewPreview);
                IEnumerable<Entity> picks = _craftingMode == CraftingMode.Forge
                    ? new[] { _pickedBlade, _pickedHaft, _pickedBinding, _pickedQuench } : _pickedReagents;
                text += "\n\nSelected inputs: " + string.Join(", ", picks.Where(p => p != null).Select(p => p.GetDisplayName())) + ".";
                text += "\n" + (_atStation ? "At the required station." : "Away from the required station: one result at a time.");
                if (_craftBatchMax > 1) text += " Batch available at station: " + _craftBatchMax + ".";
                return text;
            }
            if (_panel == PANEL_TINKERING)
            {
                var row = GetSelectedTinkeringRow(); if (!row.HasValue) return null;
                var recipe = row.Value.Recipe;
                return recipe.DisplayName + "\n\n" + recipe.Description + "\nCost: " + recipe.Cost
                    + "\nProduces: " + recipe.NumberMade + "\nIngredient: " + (recipe.Ingredient ?? "none")
                    + (row.Value.Affordable ? "\nCurrent prerequisites met; execution rechecks." : "\nMissing current prerequisites; see the recipe list.");
            }
            if (_panel == PANEL_INVENTORY && _cursorIndex >= 0 && _cursorIndex < _rows.Count) return DetailsForItem(_rows[_cursorIndex].Item?.Item);
            if (_panel == PANEL_EQUIPMENT && _equipCursorIndex >= 0 && _equipCursorIndex < _equipSlots.Count)
                return DetailsForItem(_equipSlots[_equipCursorIndex].EquippedItem);
            return null;
        }
        private string DetailsForItem(Entity item)
        {
            if (item == null || !InventoryDecisionDetails.Owned(PlayerEntity, item)) return null;
            if (item.HasPart<FoodPart>()) return InventoryDecisionDetails.Food(PlayerEntity, item) + "\n\n" + InventoryDecisionDetails.Handling(PlayerEntity, item);
            if (item.HasPart<EquippablePart>() && EquipmentComparisonService.TryDescribe(PlayerEntity, item, out var comparison, out _))
                return comparison + "\n\n" + InventoryDecisionDetails.Handling(PlayerEntity, item) + "\n" + InventoryDecisionDetails.Disassembly(PlayerEntity, item);
            if (MaterialUseDescription.TryDescribe(PlayerEntity, item, EntityFactory, out var material)) return material + "\n\n" + InventoryDecisionDetails.Handling(PlayerEntity, item);
            return InventoryDecisionDetails.Item(PlayerEntity, item);
        }
        private void BeginInventorySearch()
        {
            _panel = PANEL_INVENTORY; _inventorySearchActive = true;
            _searchReturnItem = _cursorIndex >= 0 && _cursorIndex < _rows.Count ? _rows[_cursorIndex].Item?.Item : null;
            Render();
        }
        private void SetInventoryQuery(string query)
        {
            var selected = _cursorIndex >= 0 && _cursorIndex < _rows.Count ? _rows[_cursorIndex].Item?.Item : null;
            _inventoryQuery = query ?? ""; Rebuild();
            int row = _rows.FindIndex(r => r.Item?.Item == selected && selected != null);
            _cursorIndex = row >= 0 ? row : _rows.FindIndex(r => r.Item != null);
            if (_cursorIndex < 0) _cursorIndex = 0;
            _scrollOffset = 0; ClampCursor(); Render();
        }
        private void HandleInventorySearchInput()
        {
            if (InputHelper.GetKeyDown(KeyCode.Escape))
            {
                _inventorySearchActive = false; SetInventoryQuery("");
                int row = _rows.FindIndex(r => _searchReturnItem != null && r.Item?.Item == _searchReturnItem);
                if (row >= 0) { _cursorIndex = row; ClampCursor(); } Render(); return;
            }
            if (InputHelper.GetKeyDown(KeyCode.Return)) { _inventorySearchActive = false; Render(); return; }
            if (InputHelper.GetKeyDown(KeyCode.Backspace)) { if (_inventoryQuery.Length > 0) SetInventoryQuery(_inventoryQuery.Substring(0, _inventoryQuery.Length - 1)); return; }
            if (_inventoryQuery.Length >= 48) return;
            for (int i = 0; i < 26; i++) if (InputHelper.GetKeyDown(KeyCode.A + i)) { SetInventoryQuery(_inventoryQuery + (char)('a' + i)); return; }
            for (int i = 0; i < 10; i++) if (InputHelper.GetKeyDown(KeyCode.Alpha0 + i)) { SetInventoryQuery(_inventoryQuery + (char)('0' + i)); return; }
            if (InputHelper.GetKeyDown(KeyCode.Space)) SetInventoryQuery(_inventoryQuery + " ");
            else if (InputHelper.GetKeyDown(KeyCode.Minus)) SetInventoryQuery(_inventoryQuery + "-");
            else if (InputHelper.GetKeyDown(KeyCode.Quote)) SetInventoryQuery(_inventoryQuery + "'");
        }
    }
}
