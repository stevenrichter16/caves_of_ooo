using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CavesOfOoo.Rendering
{
    public partial class InventoryUI
    {
        readonly List<int> _denseRows = new List<int>();
        InventoryPackLayout _denseLayout;
        float _denseCameraAspect;
        float _denseTextScale = 1;
        Vector2 _ledgerPaintOffset;
        float _ledgerPaintScale = 1;
        Rect _ledgerModalBounds;
        bool DensePack => _panel == PANEL_INVENTORY;
        bool _inventoryUngrouped;

        void ToggleInventoryGrouping()
        {
            var selected = _cursorIndex >= 0 && _cursorIndex < _rows.Count ? _rows[_cursorIndex].Item?.Item : null;
            _inventoryUngrouped = !_inventoryUngrouped;
            RebuildInventoryRows(selected, DenseOrdinal);
            Render();
        }
        InventoryItemGroup GroupFor(Entity item) => item == null ? null : _rows.Select(r => r.Group).FirstOrDefault(g => g != null && g.Contains(item));
        string GroupActionScope(Entity item)
        {
            var group = GroupFor(item);
            if (group == null || group.Members.Count < 2) return "";
            int count = group.Members.First(m => m.Item == item).StackCount;
            return "Selected " + count + " of " + group.TotalUnits + ". Esc, then G / Back, then Commands: individuals.";
        }

        void BuildDenseRows()
        {
            _denseRows.Clear();
            for (int i = 0; i < _rows.Count; i++) if (!_rows[i].IsHeader && _rows[i].Item != null) _denseRows.Add(i);
        }
        int DenseOrdinal => Math.Max(0, _denseRows.IndexOf(_cursorIndex));
        void MoveDensePack(PackNavigation direction)
        {
            if (_denseRows.Count == 0 || _denseLayout == null) return;
            int before = _cursorIndex;
            _cursorIndex = _denseRows[_denseLayout.Move(DenseOrdinal, _denseRows.Count, direction)];
            if (_cursorIndex != before) RememberBrowseSelection();
        }
        void ChangeDenseTextScale(int delta)
        { _denseTextScale = Mathf.Clamp(_denseTextScale + delta * .25f, 1, 2); Render(); }

        void ConfigureLedgerGeometry()
        {
            var pixels = _ledgerCanvas.pixelRect;
            float width = pixels.width > 0 ? pixels.width : Screen.width, height = pixels.height > 0 ? pixels.height : Screen.height;
            var scaler = _ledgerRoot.GetComponent<CanvasScaler>();
            if (DensePack)
            {
                float aspect = _ledgerCanvas.worldCamera != null ? _ledgerCanvas.worldCamera.aspect : 0;
                bool changed = _denseLayout == null || _denseLayout.PixelWidth != width || _denseLayout.PixelHeight != height || _denseLayout.RequestedScale != _denseTextScale;
                if (changed)
                    _denseLayout = InventoryPackLayout.Create(width, height, _denseTextScale);
                bool settle = changed || scaler.uiScaleMode != CanvasScaler.ScaleMode.ConstantPixelSize || _ledgerCanvas.scaleFactor != _denseLayout.Scale;
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize; scaler.scaleFactor = _denseLayout.Scale;
                _ledgerCanvas.scaleFactor = _denseLayout.Scale;
                _ledgerContent.sizeDelta = new Vector2(_denseLayout.Width, _denseLayout.Height);
                if (settle) Canvas.ForceUpdateCanvases();
                // Screen-space camera canvases can retain an old root rect until
                // render. Derive the horizontal compensation from the current
                // projection instead; it remains correct before/after settlement.
                _denseCameraAspect = aspect;
                _ledgerContent.localScale = new Vector3(aspect > 0 ? aspect * height / width : 1, 1, 1);
            }
            else
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                _ledgerCanvas.scaleFactor = Mathf.Min(width / 1280, height / 800);
                _ledgerContent.sizeDelta = new Vector2(1280, 800);
                _ledgerContent.localScale = Vector3.one;
            }
        }
        void RefreshDenseViewport()
        {
            if (!DensePack || _ledgerCanvas == null || _denseLayout == null) return;
            var pixels = _ledgerCanvas.pixelRect;
            float aspect = _ledgerCanvas.worldCamera != null ? _ledgerCanvas.worldCamera.aspect : 0;
            if (pixels.width == _denseLayout.PixelWidth && pixels.height == _denseLayout.PixelHeight
                && aspect == _denseCameraAspect && _denseTextScale == _denseLayout.RequestedScale) return;
            Render();
            // Resizing alone must not move the selected owner under a parked pointer.
            var mouse = UnityEngine.InputSystem.Mouse.current;
            _ledgerLastPointer = mouse != null && mouse.enabled ? mouse.position.ReadValue() : (Vector2)Input.mousePosition;
        }
        Rect LedgerPaintRect(Rect bounds) => new Rect(_ledgerPaintOffset + bounds.position * _ledgerPaintScale, bounds.size * _ledgerPaintScale);

        bool HandleDensePackKeys()
        {
            if (InputHelper.GetKeyDown(KeyCode.F3)) { OpenCategoryChooser(); return true; }
            if (InputHelper.GetKeyDown(KeyCode.F4)) { OpenSortChooser(); return true; }
            if (InputHelper.GetKeyDown(KeyCode.G)) { ToggleInventoryGrouping(); return true; }
            PackNavigation? direction = null;
            if (InputHelper.GetKeyDown(KeyCode.UpArrow) || InputHelper.GetKeyDown(KeyCode.K)) direction = PackNavigation.Up;
            else if (InputHelper.GetKeyDown(KeyCode.DownArrow) || InputHelper.GetKeyDown(KeyCode.J)) direction = PackNavigation.Down;
            else if (InputHelper.GetKeyDown(KeyCode.LeftArrow) || InputHelper.GetKeyDown(KeyCode.H)) direction = PackNavigation.Left;
            else if (InputHelper.GetKeyDown(KeyCode.RightArrow) || InputHelper.GetKeyDown(KeyCode.L)) direction = PackNavigation.Right;
            else if (InputHelper.GetKeyDown(KeyCode.PageUp)) direction = PackNavigation.PageUp;
            else if (InputHelper.GetKeyDown(KeyCode.PageDown)) direction = PackNavigation.PageDown;
            else if (InputHelper.GetKeyDown(KeyCode.Home)) direction = PackNavigation.First;
            else if (InputHelper.GetKeyDown(KeyCode.End)) direction = PackNavigation.Last;
            if (direction.HasValue) { MoveDensePack(direction.Value); Render(); return true; }
            if (InputHelper.GetKeyDown(KeyCode.Minus)) { ChangeDenseTextScale(-1); return true; }
            if (InputHelper.GetKeyDown(KeyCode.Equals)) { ChangeDenseTextScale(1); return true; }
            return false;
        }

        static string DenseName(InventoryScreenData.ItemDisplay item)
        {
            string name = item.Name ?? "";
            string suffix = " (x" + item.StackCount + ")";
            return item.StackCount > 1 && name.EndsWith(suffix, StringComparison.Ordinal) ? name.Substring(0, name.Length - suffix.Length) : name;
        }
        static string DenseNumber(long value)
        {
            long n = Math.Abs(value); string sign = value < 0 ? "-" : "";
            if (n < 10000) return value.ToString();
            long unit = n >= 1000000000 ? 1000000000 : n >= 1000000 ? 1000000 : 1000;
            return "~" + sign + n / unit + (unit == 1000000000 ? "B" : unit == 1000000 ? "M" : "k");
        }
        static string DenseRowName(InventoryScreenData.ItemDisplay item, int columns)
        {
            string name = DenseName(item), state = item.State;
            if (string.IsNullOrEmpty(state)) return SkillScreenText.Fit(name, columns, 1);
            int stateColumns = Math.Min(state.Length, Math.Max(5, columns / 2 - 2));
            return SkillScreenText.Fit(name, Math.Max(1, columns - stateColumns - 3), 1)
                + " [" + SkillScreenText.Fit(state, stateColumns, 1) + "]";
        }
        void RenderDensePack()
        {
            var g = _denseLayout; var list = g.ListBounds;
            LFrame(new Rect(list.x, 132, list.width, g.Height - 212), LGold, LPanel);
            RenderBrowseHeader();
            for (int column = 0; column < g.Columns; column++)
            {
                float x = list.x + column * (g.ColumnWidth + 12);
                LLabel(new Rect(x + 10, 171, g.NameWidth, 18), "ITEM", LMuted, 16);
                LLabel(new Rect(x + g.ColumnWidth - 126, 171, 40, 18), "QTY", LMuted, 16);
                LLabel(new Rect(x + g.ColumnWidth - 78, 171, 54, 18), "WT", LMuted, 16);
            }
            int first = g.PageStart(DenseOrdinal), end = Math.Min(first + g.Capacity, _denseRows.Count);
            if (_denseRows.Count == 0)
                LLabel(new Rect(list.x + 10, list.y + 8, list.width - 20, 72), !string.IsNullOrEmpty(_inventoryQuery) ? "No items match this search. Escape clears the filter." : !string.IsNullOrEmpty(_inventoryCategory) ? "No items in this category. Escape shows all categories." : "Your pack is empty.", LMuted, 20);
            for (int ordinal = first; ordinal < end; ordinal++)
            {
                int index = _denseRows[ordinal]; var item = _rows[index].Item; var row = g.RowBounds(ordinal);
                bool selected = index == _cursorIndex;
                if (selected) { LBox(row, LSelected); LBox(new Rect(row.x, row.y, 3, row.height), LGold); }
                LLabel(new Rect(row.x + 10, row.y + 1, g.NameWidth, 22), DenseRowName(item, (int)(g.NameWidth / 10)), selected ? LGold : LInk, 20);
                var group = _rows[index].Group;
                LLabel(new Rect(row.x + row.width - 126, row.y + 3, 40, 20), DenseNumber(group?.TotalUnits ?? item.StackCount), LInk, 16);
                LLabel(new Rect(row.x + row.width - 78, row.y + 3, 54, 20), DenseNumber(group?.TotalWeight ?? item.Weight), LInk, 16);
                if (item.IsEquipped) LLabel(new Rect(row.xMax - 24, row.y + 3, 24, 20), "[E]", LGold, 16);
                LHit(row, LedgerHitKind.Item, index);
            }
            long owners = _denseRows.Sum(i => (long)(_rows[i].Group?.Members.Count ?? 1));
            long units = _denseRows.Sum(i => _rows[i].Group?.TotalUnits ?? _rows[i].Item.StackCount);
            string status = (string.IsNullOrEmpty(_inventoryCategory) ? "All categories" : _inventoryCategory)
                + " / " + (_inventorySort == "Weight" ? "Heaviest first" : _inventorySort == "Name" ? "Name A-Z" : "Category order")
                + "   " + (_denseRows.Count == 0 ? "0" : (first + 1) + "-" + end) + " / " + _denseRows.Count
                + " rows; " + owners + " stacks / " + units + " items (includes equipped)   ~ rounded";
            LLabel(new Rect(24, g.Height - 73, g.Width - 48, 20), SkillScreenText.Fit(status, Math.Max(1,(int)((g.Width-48)/8)),1), LMuted, 16);
            RenderDenseSelection();
        }
        void RenderDenseSelection()
        {
            var g = _denseLayout;
            var document = LedgerSelectionPresentation(out string decision);
            if (!g.HasDetails)
            {
                if (document != null) { var hit = new Rect(g.Width - 130, 137, 100, 26); LLabel(hit, "F1 Details", LGold, 16); LHit(hit, LedgerHitKind.Details); }
                return;
            }
            var pane = g.DetailBounds; LFrame(pane, LBorder, LPanel);
            if (document?.Subject == null || string.IsNullOrWhiteSpace(decision))
            { LLabel(new Rect(pane.x + 12, pane.y + 16, pane.width - 24, 76), SkillScreenText.Fit("Select an item to inspect it.", (int)((pane.width - 24) / 10), 3), LMuted, 20); return; }
            var item = _rows[_cursorIndex].Item;
            var group = _rows[_cursorIndex].Group;
            LLabel(new Rect(pane.x + 12, pane.y + 16, pane.width - 24, 78), SkillScreenText.Fit(DenseName(item), (int)((pane.width - 24) / 11), 3), LGold, 22);
            LLabel(new Rect(pane.x + 12, pane.y + 100, pane.width - 24, 66), SkillScreenText.Fit("Quantity " + (group?.TotalUnits ?? item.StackCount) + "\nWeight " + (group?.TotalWeight ?? item.Weight) + " lb\n" + item.Category, (int)((pane.width - 24) / 9), 3), LMuted, 18);
            float overviewY = pane.y + 176;
            if (group != null && group.Members.Count > 1)
            {
                var scope = SkillScreenText.Wrap(group.Scope, Mathf.Max(1, (int)((pane.width - 24) / 9)));
                float scopeHeight = scope.Count * 22;
                if (overviewY + scopeHeight < pane.yMax - 46)
                {
                    LLabel(new Rect(pane.x + 12, overviewY, pane.width - 24, scopeHeight), string.Join("\n", scope), LMuted, 18);
                    overviewY += scopeHeight + 8;
                }
            }
            if (overviewY < pane.yMax - 46)
                RenderItemSectionOverview(new Rect(pane.x + 12, overviewY, pane.width - 24, pane.yMax - 46 - overviewY), document, decision);
            var details = new Rect(pane.x + 12, pane.yMax - 36, pane.width - 24, 26);
            LFrame(details, LBorder, LBackground); LLabel(new Rect(details.x + 8, details.y + 4, details.width - 16, 20), NativeGamepadInput.IsConnected ? "LT+RT Full details" : "F1 Full details", LGold, 16); LHit(details, LedgerHitKind.Details);
        }
        void RenderDenseFooter()
        {
            var g = _denseLayout;
            LBox(new Rect(24, g.Height - 39, g.Width - 48, 1), LBorder);
            string hint = _inventorySearchActive ? "Type to filter   Enter: accept   Esc: clear search"
                : NativeGamepadInput.IsConnected ? "D-pad: items/pages   A: actions   X: commands   B: back   Y/LB/RB: mode   LT+RT: details"
                : "Arrows: items/pages  PgUp/Dn Home/End  Enter: actions  / Find  Tab: mode  F1 Details  -/+ Text";
            LLabel(new Rect(24, g.Height - 28, g.Width - 164, 24), SkillScreenText.Fit(hint, (int)((g.Width - 164) / 8), 1), LMuted, 16);
            for (int i = 0; i < 2; i++)
            {
                var button = new Rect(g.Width - 116 + i * 48, g.Height - 32, 44, 26);
                LFrame(button, LBorder, LBackground); LLabel(new Rect(button.x + 10, button.y + 4, 30, 20), i == 0 ? "A-" : "A+", LGold, 16);
                LHit(button, LedgerHitKind.PackTextScale, i == 0 ? -1 : 1);
            }
        }
    }
}
