using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Renders the inventory/equipment screen onto the tilemap.
    /// Items tab: classic roguelike list with categories.
    /// Equipment tab: paperdoll-style spatial squares showing equipped items
    /// on body parts, with labels below each square. Press Enter on a square
    /// to open a popup listing compatible items from inventory.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        public Tilemap Tilemap;
        public Entity PlayerEntity;
        public Zone CurrentZone;

        // Layout constants (80x45 grid — fills 16:9 visible area for 80 columns)
        private const int W = 80;
        private const int H = 45;
        private const int CONTENT_START = 2;
        private const int CONTENT_END = 42;
        private const int VISIBLE_ROWS = CONTENT_END - CONTENT_START;
        private const int BOX_W = 5;
        private const int BOX_H = 3;

        // Equip popup constants
        private const int POPUP_W = 46;
        private const int POPUP_MAX_VISIBLE = 25;

        private bool _isOpen;
        private int _tab; // 0 = Items, 1 = Equipment
        private int _cursorIndex;
        private int _scrollOffset;
        private InventoryScreenData.ScreenState _state;

        // Items tab: flattened display rows for navigation
        private List<DisplayRow> _rows = new List<DisplayRow>();

        // Equipment tab: spatial slot navigation
        private List<InventoryScreenData.EquipmentSlot> _equipSlots;
        private int _equipCursorIndex;

        // Equip-from-slot popup
        private EquipPopupState _equipPopup;

        public bool IsOpen => _isOpen;

        private struct DisplayRow
        {
            public bool IsHeader;
            public string Text;
            public Color Color;
            public InventoryScreenData.ItemDisplay Item;
            public InventoryScreenData.EquipmentSlot Slot;
        }

        private class EquipPopupState
        {
            public InventoryScreenData.EquipmentSlot TargetSlot;
            public List<Entity> Items;
            public int CursorIndex;
            public int ScrollOffset;
            public bool HasRemoveOption;

            /// <summary>Total selectable rows: remove option (if present) + items.</summary>
            public int TotalRows => (HasRemoveOption ? 1 : 0) + Items.Count;

            /// <summary>Whether the cursor is on the remove row.</summary>
            public bool CursorOnRemove => HasRemoveOption && CursorIndex == 0;

            /// <summary>Item index for the current cursor position, or -1 if on remove row.</summary>
            public int CursorItemIndex => HasRemoveOption ? CursorIndex - 1 : CursorIndex;
        }

        public void Open()
        {
            if (PlayerEntity == null) return;
            _isOpen = true;
            _tab = 0;
            _cursorIndex = 0;
            _scrollOffset = 0;
            _equipCursorIndex = 0;
            _equipPopup = null;
            Rebuild();
            Render();
        }

        public void Close()
        {
            _isOpen = false;
            _equipPopup = null;
        }

        /// <summary>
        /// Handle input while inventory is open. Returns true if input was consumed.
        /// </summary>
        public bool HandleInput()
        {
            if (!_isOpen) return false;

            // Equip popup intercepts all input when active
            if (_equipPopup != null)
            {
                HandleEquipPopupInput();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return true;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _tab = (_tab + 1) % 2;
                _cursorIndex = 0;
                _scrollOffset = 0;
                _equipCursorIndex = 0;
                Rebuild();
                Render();
                return true;
            }

            // Navigation
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.K))
            {
                if (_tab == 1)
                    MoveEquipCursor(0, -1);
                else
                    MoveCursor(-1);
                Render();
                return true;
            }
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.J))
            {
                if (_tab == 1)
                    MoveEquipCursor(0, 1);
                else
                    MoveCursor(1);
                Render();
                return true;
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.H))
            {
                if (_tab == 1)
                {
                    MoveEquipCursor(-1, 0);
                    Render();
                }
                return true;
            }
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.L))
            {
                if (_tab == 1)
                {
                    MoveEquipCursor(1, 0);
                    Render();
                }
                return true;
            }

            // Item actions (Items tab only)
            if (_tab == 0 && _rows.Count > 0 && _cursorIndex < _rows.Count)
            {
                var row = _rows[_cursorIndex];
                if (row.Item != null)
                {
                    // Drop
                    if (Input.GetKeyDown(KeyCode.D))
                    {
                        InventorySystem.Drop(PlayerEntity, row.Item.Item, CurrentZone);
                        Rebuild();
                        ClampCursor();
                        Render();
                        return true;
                    }

                    // Equip/Unequip
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        if (row.Item.IsEquipped)
                            InventorySystem.UnequipItem(PlayerEntity, row.Item.Item);
                        else
                            InventorySystem.Equip(PlayerEntity, row.Item.Item);
                        Rebuild();
                        ClampCursor();
                        Render();
                        return true;
                    }

                    // Use/Apply (enter or 'u')
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.U))
                    {
                        var actions = row.Item.Actions;
                        if (actions != null && actions.Count > 0)
                        {
                            InventorySystem.PerformAction(
                                PlayerEntity, row.Item.Item, actions[0].Command, CurrentZone);
                            Rebuild();
                            ClampCursor();
                            Render();
                        }
                        return true;
                    }
                }
            }

            // Equipment tab actions
            if (_tab == 1 && _equipSlots != null && _equipSlots.Count > 0)
            {
                // Mouse click on an equipment square
                if (Input.GetMouseButtonDown(0))
                {
                    int clicked = GetEquipSlotAtMouse();
                    if (clicked >= 0)
                    {
                        _equipCursorIndex = clicked;
                        OpenEquipPopup(_equipSlots[clicked]);
                        return true;
                    }
                }

                if (_equipCursorIndex < _equipSlots.Count)
                {
                    var slot = _equipSlots[_equipCursorIndex];

                    // Unequip
                    if (slot.EquippedItem != null && Input.GetKeyDown(KeyCode.E))
                    {
                        InventorySystem.UnequipItem(PlayerEntity, slot.EquippedItem);
                        Rebuild();
                        if (_equipSlots != null && _equipCursorIndex >= _equipSlots.Count)
                            _equipCursorIndex = _equipSlots.Count > 0 ? _equipSlots.Count - 1 : 0;
                        Render();
                        return true;
                    }

                    // Open equip popup
                    if (Input.GetKeyDown(KeyCode.Return))
                    {
                        OpenEquipPopup(slot);
                        return true;
                    }
                }
            }

            return true; // consume all input while open
        }

        // --- Equip Popup ---

        private void OpenEquipPopup(InventoryScreenData.EquipmentSlot slot)
        {
            var compatible = BuildCompatibleItems(slot);
            _equipPopup = new EquipPopupState
            {
                TargetSlot = slot,
                Items = compatible,
                CursorIndex = 0,
                ScrollOffset = 0,
                HasRemoveOption = slot.EquippedItem != null
            };
            Render();
        }

        private List<Entity> BuildCompatibleItems(InventoryScreenData.EquipmentSlot slot)
        {
            var inventory = PlayerEntity.GetPart<InventoryPart>();
            if (inventory == null) return new List<Entity>();

            string slotType = slot.SlotName;
            var compatible = new List<Entity>();

            for (int i = 0; i < inventory.Objects.Count; i++)
            {
                var item = inventory.Objects[i];
                var equippable = item.GetPart<EquippablePart>();
                if (equippable == null) continue;

                string[] slots = equippable.GetSlotArray();
                for (int j = 0; j < slots.Length; j++)
                {
                    if (slots[j].Trim() == slotType)
                    {
                        compatible.Add(item);
                        break;
                    }
                }
            }

            // Sort by category then name
            compatible.Sort((a, b) =>
            {
                string catA = ItemCategory.GetCategory(a);
                string catB = ItemCategory.GetCategory(b);
                int catOrdA = ItemCategory.GetSortOrder(catA);
                int catOrdB = ItemCategory.GetSortOrder(catB);
                int cmp = catOrdA.CompareTo(catOrdB);
                if (cmp != 0) return cmp;
                return string.Compare(a.GetDisplayName(), b.GetDisplayName(), StringComparison.Ordinal);
            });

            return compatible;
        }

        private void HandleEquipPopupInput()
        {
            int totalRows = _equipPopup.TotalRows;

            // Escape: close popup, stay on equipment tab
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _equipPopup = null;
                Render();
                return;
            }

            // Mouse click: act on popup row or close if outside
            if (Input.GetMouseButtonDown(0))
            {
                int clickedRow = GetPopupRowAtMouse();
                if (clickedRow >= 0 && totalRows > 0)
                {
                    if (_equipPopup.HasRemoveOption && clickedRow == 0)
                    {
                        UnequipFromPopup();
                        return;
                    }
                    int itemIdx = clickedRow - (_equipPopup.HasRemoveOption ? 1 : 0);
                    if (itemIdx >= 0 && itemIdx < _equipPopup.Items.Count)
                    {
                        EquipFromPopup(itemIdx);
                        return;
                    }
                }
                else if (clickedRow < 0)
                {
                    // Click anywhere outside popup content closes it
                    _equipPopup = null;
                    Render();
                    return;
                }
            }

            // Navigate up
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.K))
            {
                if (totalRows > 0 && _equipPopup.CursorIndex > 0)
                {
                    _equipPopup.CursorIndex--;
                    ScrollPopupIntoView();
                }
                Render();
                return;
            }

            // Navigate down
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.J))
            {
                if (totalRows > 0 && _equipPopup.CursorIndex < totalRows - 1)
                {
                    _equipPopup.CursorIndex++;
                    ScrollPopupIntoView();
                }
                Render();
                return;
            }

            // Enter: remove or equip selected item
            if (Input.GetKeyDown(KeyCode.Return) && totalRows > 0)
            {
                if (_equipPopup.CursorOnRemove)
                {
                    UnequipFromPopup();
                    return;
                }
                EquipFromPopup(_equipPopup.CursorItemIndex);
                return;
            }

            // Hotkey letters a-z (mapped to item indices, not the remove row)
            int itemCount = _equipPopup.Items.Count;
            for (int i = 0; i < 26 && i < itemCount; i++)
            {
                if (Input.GetKeyDown(KeyCode.A + i))
                {
                    EquipFromPopup(i);
                    return;
                }
            }
        }

        private void ScrollPopupIntoView()
        {
            int visible = Mathf.Min(_equipPopup.TotalRows, POPUP_MAX_VISIBLE);
            if (visible <= 0) return;
            if (_equipPopup.CursorIndex < _equipPopup.ScrollOffset)
                _equipPopup.ScrollOffset = _equipPopup.CursorIndex;
            else if (_equipPopup.CursorIndex >= _equipPopup.ScrollOffset + visible)
                _equipPopup.ScrollOffset = _equipPopup.CursorIndex - visible + 1;
        }

        private void EquipFromPopup(int index)
        {
            if (index < 0 || index >= _equipPopup.Items.Count) return;

            var item = _equipPopup.Items[index];
            var slot = _equipPopup.TargetSlot;

            // Find the target BodyPart by ID
            BodyPart targetBodyPart = null;
            var body = PlayerEntity.GetPart<Body>();
            if (body != null && slot.BodyPartID > 0)
            {
                var parts = body.GetParts();
                for (int i = 0; i < parts.Count; i++)
                {
                    if (parts[i].ID == slot.BodyPartID)
                    {
                        targetBodyPart = parts[i];
                        break;
                    }
                }
            }

            InventorySystem.Equip(PlayerEntity, item, targetBodyPart);

            _equipPopup = null;
            Rebuild();
            Render();
        }

        private void UnequipFromPopup()
        {
            var slot = _equipPopup.TargetSlot;
            if (slot.EquippedItem != null)
                InventorySystem.UnequipItem(PlayerEntity, slot.EquippedItem);

            _equipPopup = null;
            Rebuild();
            Render();
        }

        // --- Rebuild ---

        private void Rebuild()
        {
            _state = InventoryScreenData.Build(PlayerEntity);
            _rows.Clear();

            if (_tab == 0)
            {
                BuildItemRows();
            }
            else
            {
                _equipSlots = _state.Equipment;
                if (_equipSlots.Count > 0 && _equipCursorIndex >= _equipSlots.Count)
                    _equipCursorIndex = _equipSlots.Count - 1;
            }
        }

        private void BuildItemRows()
        {
            for (int i = 0; i < _state.Categories.Count; i++)
            {
                var cat = _state.Categories[i];
                _rows.Add(new DisplayRow
                {
                    IsHeader = true,
                    Text = cat.CategoryName,
                    Color = QudColorParser.BrightYellow
                });

                for (int j = 0; j < cat.Items.Count; j++)
                {
                    var item = cat.Items[j];
                    string name = item.Name;
                    if (item.StackCount > 1)
                        name += " (x" + item.StackCount + ")";

                    string equipped = item.IsEquipped ? " [E]" : "";
                    string line = "  " + name;

                    // Pad to column 50 for weight/value
                    if (line.Length < 50)
                        line = line + new string(' ', 50 - line.Length);
                    else
                        line = line.Substring(0, 50);

                    line += item.Weight.ToString().PadLeft(4) + " lb  ";
                    line += item.Value.ToString().PadLeft(4) + "$";
                    line += equipped;

                    _rows.Add(new DisplayRow
                    {
                        IsHeader = false,
                        Text = line,
                        Color = item.IsEquipped ? QudColorParser.BrightCyan : QudColorParser.Gray,
                        Item = item
                    });
                }
            }
        }

        // --- Navigation ---

        private void MoveCursor(int delta)
        {
            if (_rows.Count == 0) return;

            int next = _cursorIndex + delta;

            // Skip headers
            while (next >= 0 && next < _rows.Count && _rows[next].IsHeader)
                next += delta;

            if (next >= 0 && next < _rows.Count)
                _cursorIndex = next;

            // Scroll into view
            int viewIndex = _cursorIndex - _scrollOffset;
            if (viewIndex < 0)
                _scrollOffset = _cursorIndex;
            else if (viewIndex >= VISIBLE_ROWS)
                _scrollOffset = _cursorIndex - VISIBLE_ROWS + 1;
        }

        /// <summary>
        /// Move the equipment cursor spatially toward the nearest slot in the given direction.
        /// </summary>
        private void MoveEquipCursor(int dx, int dy)
        {
            if (_equipSlots == null || _equipSlots.Count <= 1) return;

            var current = _equipSlots[_equipCursorIndex];
            int cx = current.GridX;
            int cy = current.GridY;

            int bestIdx = -1;
            float bestDist = float.MaxValue;

            for (int i = 0; i < _equipSlots.Count; i++)
            {
                if (i == _equipCursorIndex) continue;
                var s = _equipSlots[i];
                int sx = s.GridX;
                int sy = s.GridY;

                // Direction filtering
                if (dx < 0 && sx >= cx) continue;
                if (dx > 0 && sx <= cx) continue;
                if (dy < 0 && sy >= cy) continue;
                if (dy > 0 && sy <= cy) continue;

                // Distance with directional weighting
                float primaryDist, crossDist;
                if (dx != 0)
                {
                    primaryDist = Mathf.Abs(sx - cx);
                    crossDist = Mathf.Abs(sy - cy);
                }
                else
                {
                    primaryDist = Mathf.Abs(sy - cy);
                    crossDist = Mathf.Abs(sx - cx);
                }

                float dist = primaryDist + crossDist * 2f;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }

            if (bestIdx >= 0)
                _equipCursorIndex = bestIdx;
        }

        private void ClampCursor()
        {
            if (_rows.Count == 0)
            {
                _cursorIndex = 0;
                return;
            }

            if (_cursorIndex >= _rows.Count)
                _cursorIndex = _rows.Count - 1;

            // Skip to non-header
            while (_cursorIndex > 0 && _rows[_cursorIndex].IsHeader)
                _cursorIndex--;

            if (_scrollOffset > _cursorIndex)
                _scrollOffset = _cursorIndex;
        }

        // --- Rendering ---

        private void Render()
        {
            if (Tilemap == null) return;

            Tilemap.ClearAllTiles();

            // Title bar
            string titleTab0 = _tab == 0 ? "[Items]" : " Items ";
            string titleTab1 = _tab == 1 ? "[Equipment]" : " Equipment ";
            string title = " " + titleTab0 + "  " + titleTab1;
            DrawText(0, 0, title, QudColorParser.White);

            // Weight and drams on right side
            string info = "Wt: " + _state.CarriedWeight + "/" + _state.MaxCarryWeight
                        + "  $" + _state.Drams;
            DrawText(W - info.Length - 1, 0, info, QudColorParser.Gray);

            // Separator
            DrawHLine(0, 1, W, QudColorParser.DarkGray);

            if (_tab == 0)
            {
                RenderItemsTab();
            }
            else
            {
                RenderEquipmentTab();
            }

            // Equip popup overlay (drawn on top of paperdoll)
            if (_equipPopup != null)
            {
                RenderEquipPopup();
            }

            // Bottom separator
            DrawHLine(0, CONTENT_END, W, QudColorParser.DarkGray);

            // Action bar
            string actions;
            if (_equipPopup != null)
                actions = " [Enter]select  [a-z]quick select  [Esc]cancel";
            else if (_tab == 0)
                actions = " [d]rop [e]quip [Enter]use [Tab]equip tab [Esc]close";
            else
                actions = " [Enter]equip [e]unequip [Tab]items tab [Esc]close";

            DrawText(0, H - 2, actions, QudColorParser.Gray);

            // Selected item detail line
            if (_equipPopup != null)
            {
                if (_equipPopup.CursorOnRemove)
                {
                    string detail = "Remove " + (_equipPopup.TargetSlot.ItemName ?? "item")
                                  + " from " + _equipPopup.TargetSlot.BodyPartName;
                    DrawText(1, H - 1, detail, QudColorParser.BrightRed);
                }
                else if (_equipPopup.CursorItemIndex >= 0
                    && _equipPopup.CursorItemIndex < _equipPopup.Items.Count)
                {
                    var item = _equipPopup.Items[_equipPopup.CursorItemIndex];
                    var equip = item.GetPart<EquippablePart>();
                    string detail = item.GetDisplayName();
                    if (equip != null && !string.IsNullOrEmpty(equip.EquipBonuses))
                        detail += "  (" + equip.EquipBonuses + ")";
                    DrawText(1, H - 1, detail, QudColorParser.BrightCyan);
                }
            }
            else if (_tab == 0 && _cursorIndex < _rows.Count && _rows[_cursorIndex].Item != null)
            {
                var item = _rows[_cursorIndex].Item;
                var itemActions = item.Actions;
                if (itemActions != null && itemActions.Count > 0)
                {
                    string actionStr = " Actions:";
                    for (int i = 0; i < itemActions.Count && i < 4; i++)
                    {
                        actionStr += " [" + itemActions[i].Display + "]";
                    }
                    DrawText(0, H - 1, actionStr, QudColorParser.DarkCyan);
                }
            }
            else if (_tab == 1 && _equipSlots != null && _equipCursorIndex < _equipSlots.Count)
            {
                var slot = _equipSlots[_equipCursorIndex];
                string detail = slot.BodyPartName + ": " + slot.ItemName;
                DrawText(1, H - 1, detail, QudColorParser.BrightCyan);
            }
        }

        private void RenderItemsTab()
        {
            // Hint about items count
            string countText = _state.TotalItems + " items";
            DrawText(1, 2, countText, QudColorParser.DarkGray);

            // Content rows
            int y = CONTENT_START;
            for (int i = _scrollOffset; i < _rows.Count && y < CONTENT_END; i++, y++)
            {
                var row = _rows[i];
                bool selected = (i == _cursorIndex);

                if (selected && !row.IsHeader)
                {
                    DrawText(0, y, ">", QudColorParser.White);
                    DrawText(1, y, row.Text, QudColorParser.White);
                }
                else
                {
                    DrawText(1, y, row.Text, row.Color);
                }
            }

            // Scroll indicators
            if (_scrollOffset > 0)
                DrawText(W - 2, CONTENT_START, "^", QudColorParser.Gray);
            if (_scrollOffset + VISIBLE_ROWS < _rows.Count)
                DrawText(W - 2, CONTENT_END - 1, "v", QudColorParser.Gray);
        }

        private void RenderEquipmentTab()
        {
            if (_equipSlots == null) return;

            for (int i = 0; i < _equipSlots.Count; i++)
            {
                bool selected = (i == _equipCursorIndex) && _equipPopup == null;
                DrawEquipmentSquare(_equipSlots[i], selected);
            }
        }

        /// <summary>
        /// Draw a single equipment square: a bordered box with the item glyph inside
        /// and a body part label below.
        /// </summary>
        private void DrawEquipmentSquare(InventoryScreenData.EquipmentSlot slot, bool selected)
        {
            int x = slot.GridX;
            int y = slot.GridY;

            Color borderColor = selected ? QudColorParser.White : QudColorParser.DarkGray;

            // Top border: +---+
            DrawChar(x, y, '+', borderColor);
            for (int i = 1; i < BOX_W - 1; i++)
                DrawChar(x + i, y, '-', borderColor);
            DrawChar(x + BOX_W - 1, y, '+', borderColor);

            // Middle row: |   |
            DrawChar(x, y + 1, '|', borderColor);
            DrawChar(x + BOX_W - 1, y + 1, '|', borderColor);

            // Bottom border: +---+
            DrawChar(x, y + 2, '+', borderColor);
            for (int i = 1; i < BOX_W - 1; i++)
                DrawChar(x + i, y + 2, '-', borderColor);
            DrawChar(x + BOX_W - 1, y + 2, '+', borderColor);

            // Item glyph centered in box
            if (slot.EquippedItem != null)
            {
                var render = slot.EquippedItem.GetPart<RenderPart>();
                if (render != null && !string.IsNullOrEmpty(render.RenderString))
                {
                    char glyph = render.RenderString[0];
                    Color glyphColor = QudColorParser.Parse(render.ColorString);
                    DrawChar(x + 2, y + 1, glyph, glyphColor);
                }
            }

            // Label below box, centered on box width
            string label = slot.ShortLabel ?? slot.BodyPartName;
            int labelStart = x + (BOX_W - label.Length) / 2;
            if (labelStart < 0) labelStart = 0;
            Color labelColor = selected ? QudColorParser.BrightYellow : QudColorParser.Gray;
            DrawText(labelStart, y + BOX_H, label, labelColor);

            // Stats line below label (with 1 row gap after label)
            if (slot.EquippedItem != null)
            {
                string stats = BuildSlotStats(slot.EquippedItem);
                if (stats.Length > 0)
                {
                    int statsStart = x + (BOX_W - stats.Length) / 2;
                    if (statsStart < 0) statsStart = 0;
                    DrawText(statsStart, y + BOX_H + 2, stats, QudColorParser.DarkCyan);
                }
            }
        }

        /// <summary>
        /// Build a compact stat summary for an equipped item.
        /// Weapons: "1d4 +2", Armor: "4AV 1DV", or EquipBonuses fallback.
        /// </summary>
        private string BuildSlotStats(Entity item)
        {
            var parts = new List<string>();

            var weapon = item.GetPart<MeleeWeaponPart>();
            if (weapon != null)
            {
                parts.Add(weapon.BaseDamage);
                if (weapon.HitBonus > 0) parts.Add("+" + weapon.HitBonus);
                else if (weapon.HitBonus < 0) parts.Add(weapon.HitBonus.ToString());
                if (weapon.PenBonus > 0) parts.Add(weapon.PenBonus + "PV");
                else if (weapon.PenBonus < 0) parts.Add(weapon.PenBonus + "PV");
            }

            var armor = item.GetPart<ArmorPart>();
            if (armor != null)
            {
                if (armor.AV != 0) parts.Add(armor.AV + "AV");
                if (armor.DV != 0) parts.Add(armor.DV + "DV");
            }

            if (parts.Count == 0)
            {
                var equippable = item.GetPart<EquippablePart>();
                if (equippable != null && !string.IsNullOrEmpty(equippable.EquipBonuses))
                {
                    // Compact the bonus string: "Strength:2" -> "Str+2"
                    string[] bonuses = equippable.EquipBonuses.Split(',');
                    for (int i = 0; i < bonuses.Length; i++)
                    {
                        string b = bonuses[i].Trim();
                        int colon = b.IndexOf(':');
                        if (colon < 0) continue;
                        string stat = b.Substring(0, colon);
                        string val = b.Substring(colon + 1);
                        // Abbreviate stat name to 3 chars
                        if (stat.Length > 3) stat = stat.Substring(0, 3);
                        if (!val.StartsWith("-")) val = "+" + val;
                        parts.Add(stat + val);
                    }
                }
            }

            return string.Join(" ", parts);
        }

        /// <summary>
        /// Render the equip-from-slot popup as a centered bordered list
        /// overlaying the paperdoll.
        /// </summary>
        private void RenderEquipPopup()
        {
            int totalRows = _equipPopup.TotalRows;
            int visibleCount = Mathf.Min(totalRows > 0 ? totalRows : 1, POPUP_MAX_VISIBLE);
            int popupH = visibleCount + 4; // top border + title + separator + content + bottom border
            int popupX = (W - POPUP_W) / 2;
            int popupY = (H - popupH) / 2;

            // Clear the region behind the popup
            ClearRegion(popupX, popupY, POPUP_W, popupH);

            // Top border
            DrawChar(popupX, popupY, '+', QudColorParser.Gray);
            for (int i = 1; i < POPUP_W - 1; i++)
                DrawChar(popupX + i, popupY, '-', QudColorParser.Gray);
            DrawChar(popupX + POPUP_W - 1, popupY, '+', QudColorParser.Gray);

            // Title row sides
            DrawChar(popupX, popupY + 1, '|', QudColorParser.Gray);
            DrawChar(popupX + POPUP_W - 1, popupY + 1, '|', QudColorParser.Gray);

            // Separator under title
            int sepY = popupY + 2;
            DrawChar(popupX, sepY, '+', QudColorParser.Gray);
            for (int i = 1; i < POPUP_W - 1; i++)
                DrawChar(popupX + i, sepY, '-', QudColorParser.Gray);
            DrawChar(popupX + POPUP_W - 1, sepY, '+', QudColorParser.Gray);

            // Content row sides
            for (int r = 0; r < visibleCount; r++)
            {
                int ry = popupY + 3 + r;
                DrawChar(popupX, ry, '|', QudColorParser.Gray);
                DrawChar(popupX + POPUP_W - 1, ry, '|', QudColorParser.Gray);
            }

            // Bottom border
            int botY = popupY + 3 + visibleCount;
            DrawChar(popupX, botY, '+', QudColorParser.Gray);
            for (int i = 1; i < POPUP_W - 1; i++)
                DrawChar(popupX + i, botY, '-', QudColorParser.Gray);
            DrawChar(popupX + POPUP_W - 1, botY, '+', QudColorParser.Gray);

            // Title text
            string titleText = "Equip to: " + _equipPopup.TargetSlot.BodyPartName;
            if (titleText.Length > POPUP_W - 4)
                titleText = titleText.Substring(0, POPUP_W - 4);
            DrawText(popupX + 2, popupY + 1, titleText, QudColorParser.BrightYellow);

            // Content rows
            int contentY = popupY + 3;
            if (totalRows == 0)
            {
                DrawText(popupX + 2, contentY, "(no items available)", QudColorParser.DarkGray);
            }
            else
            {
                int removeOffset = _equipPopup.HasRemoveOption ? 1 : 0;

                for (int vi = 0; vi < visibleCount; vi++)
                {
                    int idx = _equipPopup.ScrollOffset + vi;
                    if (idx >= totalRows) break;

                    int rowY = contentY + vi;
                    bool selected = (idx == _equipPopup.CursorIndex);

                    // Cursor indicator
                    if (selected)
                        DrawChar(popupX + 1, rowY, '>', QudColorParser.White);

                    if (_equipPopup.HasRemoveOption && idx == 0)
                    {
                        // Remove row — show currently equipped item name
                        string removeName = _equipPopup.TargetSlot.ItemName ?? "item";
                        string removeText = "- Remove " + removeName;
                        int maxLen = POPUP_W - 5;
                        if (removeText.Length > maxLen)
                            removeText = removeText.Substring(0, maxLen - 1) + "~";
                        Color removeColor = selected ? QudColorParser.White : QudColorParser.BrightRed;
                        DrawText(popupX + 2, rowY, removeText, removeColor);
                    }
                    else
                    {
                        // Item row
                        int itemIdx = idx - removeOffset;
                        var item = _equipPopup.Items[itemIdx];

                        // Hotkey letter
                        if (itemIdx < 26)
                        {
                            char hotkey = (char)('a' + itemIdx);
                            string hk = hotkey + ")";
                            DrawText(popupX + 2, rowY, hk,
                                selected ? QudColorParser.White : QudColorParser.Gray);
                        }

                        // Item glyph
                        var render = item.GetPart<RenderPart>();
                        if (render != null && !string.IsNullOrEmpty(render.RenderString))
                        {
                            char glyph = render.RenderString[0];
                            Color glyphColor = QudColorParser.Parse(render.ColorString);
                            DrawChar(popupX + 5, rowY, glyph, glyphColor);
                        }

                        // Item name
                        string name = item.GetDisplayName();
                        int maxNameLen = POPUP_W - 9;
                        if (name.Length > maxNameLen)
                            name = name.Substring(0, maxNameLen - 1) + "~";
                        Color nameColor = selected ? QudColorParser.White : QudColorParser.Gray;
                        DrawText(popupX + 7, rowY, name, nameColor);
                    }
                }

                // Scroll indicators
                if (_equipPopup.ScrollOffset > 0)
                    DrawChar(popupX + POPUP_W - 2, contentY, '^', QudColorParser.Gray);
                if (_equipPopup.ScrollOffset + visibleCount < totalRows)
                    DrawChar(popupX + POPUP_W - 2, contentY + visibleCount - 1, 'v', QudColorParser.Gray);
            }
        }

        // --- Mouse Helpers ---

        /// <summary>
        /// Convert mouse screen position to inventory grid coordinates.
        /// Returns (-1,-1) if outside the grid.
        /// </summary>
        private Vector2Int MouseToGrid()
        {
            var cam = Camera.main;
            if (cam == null) return new Vector2Int(-1, -1);

            Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
            // Tilemap cells have origin at bottom-left, so FloorToInt maps
            // any point within a cell to the correct grid position.
            int tx = Mathf.FloorToInt(world.x);
            int ty = Mathf.FloorToInt(world.y);
            // Convert tilemap Y back to inventory Y (row 0 = top = tilemap Y = H-1)
            int gx = tx;
            int gy = H - 1 - ty;

            if (gx < 0 || gx >= W || gy < 0 || gy >= H)
                return new Vector2Int(-1, -1);
            return new Vector2Int(gx, gy);
        }

        /// <summary>
        /// Returns the equipment slot index under the mouse, or -1 if none.
        /// Hit area covers the box (BOX_W x BOX_H) and the label row below it.
        /// </summary>
        private int GetEquipSlotAtMouse()
        {
            if (_equipSlots == null) return -1;
            var grid = MouseToGrid();
            if (grid.x < 0) return -1;

            for (int i = 0; i < _equipSlots.Count; i++)
            {
                var s = _equipSlots[i];
                if (grid.x >= s.GridX && grid.x < s.GridX + BOX_W
                    && grid.y >= s.GridY && grid.y <= s.GridY + BOX_H)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns the popup row index (0-based across all rows including remove)
        /// under the mouse, or -1 if click is outside the popup content area.
        /// </summary>
        private int GetPopupRowAtMouse()
        {
            var grid = MouseToGrid();
            if (grid.x < 0) return -1;

            int totalRows = _equipPopup.TotalRows;
            int visibleCount = Mathf.Min(totalRows, POPUP_MAX_VISIBLE);
            int popupH = visibleCount + 4;
            int popupX = (W - POPUP_W) / 2;
            int popupY = (H - popupH) / 2;
            int contentY = popupY + 3;

            // Check if click is within content rows
            if (grid.x > popupX && grid.x < popupX + POPUP_W - 1
                && grid.y >= contentY && grid.y < contentY + visibleCount)
            {
                int vi = grid.y - contentY;
                int rowIdx = _equipPopup.ScrollOffset + vi;
                if (rowIdx < totalRows)
                    return rowIdx;
            }
            return -1;
        }

        // --- Drawing Helpers ---

        private void ClearRegion(int x, int y, int width, int height)
        {
            for (int fy = y; fy < y + height; fy++)
            {
                for (int fx = x; fx < x + width; fx++)
                {
                    if (fx < 0 || fx >= W || fy < 0 || fy >= H) continue;
                    var tilePos = new Vector3Int(fx, H - 1 - fy, 0);
                    Tilemap.SetTile(tilePos, null);
                }
            }
        }

        private void DrawChar(int x, int y, char c, Color color)
        {
            if (Tilemap == null || x < 0 || x >= W || y < 0 || y >= H) return;
            var tilePos = new Vector3Int(x, H - 1 - y, 0);
            var tile = CP437TilesetGenerator.GetTile(c);
            if (tile == null) return;
            Tilemap.SetTile(tilePos, tile);
            Tilemap.SetTileFlags(tilePos, TileFlags.None);
            Tilemap.SetColor(tilePos, color);
        }

        private void DrawText(int x, int y, string text, Color color)
        {
            if (text == null || Tilemap == null) return;

            for (int i = 0; i < text.Length && x + i < W; i++)
            {
                char c = text[i];
                if (c == ' ') continue;

                var tilePos = new Vector3Int(x + i, H - 1 - y, 0);
                var tile = CP437TilesetGenerator.GetTile(c);
                if (tile == null) continue;

                Tilemap.SetTile(tilePos, tile);
                Tilemap.SetTileFlags(tilePos, TileFlags.None);
                Tilemap.SetColor(tilePos, color);
            }
        }

        private void DrawHLine(int x, int y, int width, Color color)
        {
            var tilePos = new Vector3Int(0, H - 1 - y, 0);
            var tile = CP437TilesetGenerator.GetTile('-');
            if (tile == null) return;

            for (int i = x; i < x + width && i < W; i++)
            {
                tilePos.x = i;
                Tilemap.SetTile(tilePos, tile);
                Tilemap.SetTileFlags(tilePos, TileFlags.None);
                Tilemap.SetColor(tilePos, color);
            }
        }
    }
}
