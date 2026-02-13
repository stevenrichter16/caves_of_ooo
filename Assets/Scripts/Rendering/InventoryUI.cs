using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Renders the inventory/equipment screen onto the tilemap.
    /// Classic roguelike fullscreen overlay: categories with items,
    /// equipment slots, weight/drams summary, and item actions.
    /// Keyboard-driven: arrows to navigate, tab to switch tabs,
    /// enter for actions, escape to close.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        public Tilemap Tilemap;
        public Entity PlayerEntity;
        public Zone CurrentZone;

        // Layout constants (80x25 grid)
        private const int W = 80;
        private const int H = 25;
        private const int CONTENT_START = 3;
        private const int CONTENT_END = 22;
        private const int VISIBLE_ROWS = CONTENT_END - CONTENT_START;

        private bool _isOpen;
        private int _tab; // 0 = Items, 1 = Equipment
        private int _cursorIndex;
        private int _scrollOffset;
        private InventoryScreenData.ScreenState _state;

        // Flattened display rows for navigation
        private List<DisplayRow> _rows = new List<DisplayRow>();

        public bool IsOpen => _isOpen;

        private struct DisplayRow
        {
            public bool IsHeader;
            public string Text;
            public Color Color;
            public InventoryScreenData.ItemDisplay Item;
            public InventoryScreenData.EquipmentSlot Slot;
        }

        public void Open()
        {
            if (PlayerEntity == null) return;
            _isOpen = true;
            _tab = 0;
            _cursorIndex = 0;
            _scrollOffset = 0;
            Rebuild();
            Render();
        }

        public void Close()
        {
            _isOpen = false;
        }

        /// <summary>
        /// Handle input while inventory is open. Returns true if input was consumed.
        /// </summary>
        public bool HandleInput()
        {
            if (!_isOpen) return false;

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
                Rebuild();
                Render();
                return true;
            }

            // Navigation
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.K))
            {
                MoveCursor(-1);
                Render();
                return true;
            }
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.J))
            {
                MoveCursor(1);
                Render();
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
            if (_tab == 1 && _rows.Count > 0 && _cursorIndex < _rows.Count)
            {
                var row = _rows[_cursorIndex];
                if (row.Slot != null && row.Slot.EquippedItem != null)
                {
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        InventorySystem.UnequipItem(PlayerEntity, row.Slot.EquippedItem);
                        Rebuild();
                        ClampCursor();
                        Render();
                        return true;
                    }
                }
            }

            return true; // consume all input while open
        }

        private void Rebuild()
        {
            _state = InventoryScreenData.Build(PlayerEntity);
            _rows.Clear();

            if (_tab == 0)
                BuildItemRows();
            else
                BuildEquipmentRows();
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

        private void BuildEquipmentRows()
        {
            for (int i = 0; i < _state.Equipment.Count; i++)
            {
                var slot = _state.Equipment[i];
                string line = slot.BodyPartName.PadRight(20) + " " + slot.ItemName;

                _rows.Add(new DisplayRow
                {
                    IsHeader = false,
                    Text = line,
                    Color = slot.EquippedItem != null ? QudColorParser.BrightCyan : QudColorParser.DarkGray,
                    Slot = slot
                });
            }
        }

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
                    // Highlight selected row
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

            // Separator
            DrawHLine(0, CONTENT_END, W, QudColorParser.DarkGray);

            // Action bar
            string actions;
            if (_tab == 0)
                actions = " [d]rop [e]quip [Enter]use [Tab]equip tab [Esc]close";
            else
                actions = " [e]unequip [Tab]items tab [Esc]close";

            DrawText(0, H - 2, actions, QudColorParser.Gray);

            // Selected item detail line
            if (_tab == 0 && _cursorIndex < _rows.Count && _rows[_cursorIndex].Item != null)
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
