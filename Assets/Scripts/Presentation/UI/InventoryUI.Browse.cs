using System;
using System.Linq;
using CavesOfOoo.Core;
using UnityEngine;
using static CavesOfOoo.Rendering.LedgerPalette;

namespace CavesOfOoo.Rendering
{
    public partial class InventoryUI
    {
        string _inventoryCategory = "";
        string _inventorySort = "Category";
        Entity _categoryReturnItem;
        string _ledgerCommandTitle = "COMMANDS";
        Entity BrowseSelected => _cursorIndex >= 0 && _cursorIndex < _rows.Count ? _rows[_cursorIndex].Item?.Item : null;

        void RememberBrowseSelection()
        {
            if (_inventorySearchActive) return;
            if (!string.IsNullOrEmpty(_inventoryQuery)) _searchReturnItem = BrowseSelected;
            if (!string.IsNullOrEmpty(_inventoryCategory)) _categoryReturnItem = BrowseSelected;
        }
        void SetInventoryCategory(string category)
        {
            var selected = BrowseSelected;
            if (string.IsNullOrEmpty(_inventoryCategory)) _categoryReturnItem = selected;
            if (string.IsNullOrEmpty(category) && _categoryReturnItem != null) selected = _categoryReturnItem;
            _inventoryCategory = category ?? "";
            RefreshBrowse(selected);
        }
        void SetInventorySort(string sort)
        {
            if (sort != "Category" && sort != "Name" && sort != "Weight") return;
            _inventorySort = sort; RefreshBrowse(BrowseSelected);
        }
        static string InventorySortLabel(string sort) => sort switch
        {
            "Name" => "Name A-Z in category",
            "Weight" => "Heaviest in category",
            _ => "Category order"
        };
        void RefreshBrowse(Entity selected)
        {
            if (_state == null) Rebuild(); else RebuildInventoryRows(selected, DenseOrdinal);
            // A filter fallback is visible selection, not a deliberate replacement
            // of the remembered owner. Never leave an excluded owner actionable.
            int row = _rows.FindIndex(r => selected != null && ReferenceEquals(r.Item?.Item, selected));
            _cursorIndex = row >= 0 ? row : _rows.FindIndex(r => r.Item != null);
            if (_cursorIndex < 0) _cursorIndex = 0;
            _scrollOffset = 0; ClampCursor(); Render();
        }
        static string BrowseGroupAnchor(InventoryItemGroup group)
            => group.Members.Select(m => m.Item.ID).OrderBy(id => id, StringComparer.Ordinal).First();
        int CompareBrowseGroups(InventoryItemGroup a, InventoryItemGroup b)
        {
            int order = _inventorySort == "Weight" ? b.TotalWeight.CompareTo(a.TotalWeight) : 0;
            if (order == 0) order = StringComparer.OrdinalIgnoreCase.Compare(DenseName(a.Selected), DenseName(b.Selected));
            return order != 0 ? order : StringComparer.Ordinal.Compare(BrowseGroupAnchor(a), BrowseGroupAnchor(b));
        }
        void OpenCategoryChooser()
        {
            _ledgerCommands.Clear(); _ledgerCommandIndex = 0; _ledgerCommandTitle = "CATEGORIES";
            AddLedgerCommand((string.IsNullOrEmpty(_inventoryCategory) ? "[x] " : "[ ] ") + "All categories", () => SetInventoryCategory(""));
            foreach (var group in _state.Categories)
            {
                string category = group.CategoryName;
                AddLedgerCommand((_inventoryCategory == category ? "[x] " : "[ ] ") + category, () => SetInventoryCategory(category));
            }
            _ledgerCommandsOpen = true; Render();
        }
        void OpenSortChooser()
        {
            _ledgerCommands.Clear(); _ledgerCommandIndex = 0; _ledgerCommandTitle = "SORT ITEMS";
            foreach (string sort in new[] { "Category", "Name", "Weight" })
            {
                string choice = sort;
                string label = choice == "Category" ? "Category (original order)" : InventorySortLabel(choice);
                AddLedgerCommand((_inventorySort == choice ? "[x] " : "[ ] ") + label, () => SetInventorySort(choice));
            }
            _ledgerCommandsOpen = true; Render();
        }
        void OpenBrowseCommands()
        {
            _ledgerCommands.Clear(); _ledgerCommandIndex = 0; _ledgerCommandTitle = "BROWSE ITEMS";
            AddLedgerCommand("Choose category", OpenCategoryChooser); AddLedgerCommand("Sort items", OpenSortChooser);
            _ledgerCommandsOpen = true; Render();
        }
        void RenderBrowseHeader()
        {
            var list = _denseLayout.ListBounds;
            bool compact = list.width < 760;
            float detailsWidth = _denseLayout.HasDetails ? 0 : 120;
            float controlsWidth = (compact ? 198 : 556) + detailsWidth;
            float queryWidth = Mathf.Max(1, list.width - controlsWidth);
            int queryColumns = Math.Max(1,(int)(queryWidth / 10));
            string query = string.IsNullOrEmpty(_inventoryQuery) ? "/ Find" : (compact ? "" : "Find: ") + _inventoryQuery;
            if (_inventorySearchActive)
            {
                query += "_";
                // Keep the insertion end visible while typing in a narrow pack.
                if (query.Length > queryColumns) query = query.Substring(query.Length - queryColumns);
            }
            else query = queryColumns < 4 ? query.Substring(0,Math.Min(query.Length,queryColumns)) : SkillScreenText.Fit(query,queryColumns,1);
            LLabel(new Rect(list.x + 10, 143, queryWidth, 24), query, _inventorySearchActive ? LGold : LMuted, 20);
            var grouping = new Rect(list.xMax - (compact ? 92 : 132) - detailsWidth, 140, compact ? 80 : 120, 27);
            BrowseHeaderButton(grouping, compact ? (_inventoryUngrouped ? "Singles" : "Grouped") : (_inventoryUngrouped ? "G: Singles" : "G: Grouped"), LedgerHitKind.PackGrouping);
            if (compact) BrowseHeaderButton(new Rect(grouping.x - 88, 140, 80, 27), "Browse", LedgerHitKind.PackBrowse);
            else
            {
                var sort = new Rect(grouping.x - 188, 140, 180, 27);
                BrowseHeaderButton(sort, "F4: " + (_inventorySort == "Weight" ? "Weight in section" : _inventorySort == "Name" ? "A-Z in section" : "Category"), LedgerHitKind.PackSort);
                BrowseHeaderButton(new Rect(sort.x - 208, 140, 200, 27), "F3: " + (string.IsNullOrEmpty(_inventoryCategory) ? "All categories" : _inventoryCategory), LedgerHitKind.PackCategory);
            }
        }
        void BrowseHeaderButton(Rect bounds, string label, LedgerHitKind kind)
        {
            LFrame(bounds, LBorder, LBackground);
            LLabel(new Rect(bounds.x+5,bounds.y+4,bounds.width-10,20),SkillScreenText.Fit(label,Math.Max(1,(int)((bounds.width-10)/8)),1),LGold,16);
            if (!_inventorySearchActive) LHit(bounds,kind);
        }
    }
}
