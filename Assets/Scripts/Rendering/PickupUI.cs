using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Renders a pickup popup overlaid on the game view when the player presses G
    /// with multiple items on the ground. Mimics Caves of Qud's PickItem popup.
    /// Single item: auto-pickup (handled by InputHandler).
    /// Multiple items: this popup appears over the game world, allowing individual selection.
    /// The game world remains visible around the popup.
    /// </summary>
    public class PickupUI : MonoBehaviour
    {
        public Tilemap Tilemap;
        public Entity PlayerEntity;
        public Zone CurrentZone;

        private const int POPUP_W = 46;
        private const int POPUP_MAX_VISIBLE = 20;

        private bool _isOpen;
        private List<Entity> _items = new List<Entity>();
        private int _cursorIndex;
        private int _scrollOffset;
        private bool _pickedUpAny;

        // Popup anchor in world tile coordinates.
        // All drawing uses popup-local grid coords (0,0 = top-left, Y down)
        // which are converted to world coords via these anchors.
        private int _worldOriginX;   // world X of popup grid column 0
        private int _worldTopY;      // world Y of popup grid row 0
        private int _popupH;

        public bool IsOpen => _isOpen;
        public bool PickedUpAny => _pickedUpAny;

        public void Open(List<Entity> items)
        {
            if (items == null || items.Count == 0) return;
            _isOpen = true;
            _items = new List<Entity>(items);
            _cursorIndex = 0;
            _scrollOffset = 0;
            _pickedUpAny = false;
            Render();
        }

        public void Close()
        {
            _isOpen = false;
            _items.Clear();
        }

        public void HandleInput()
        {
            if (!_isOpen) return;

            // Close on Escape or G
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.G))
            {
                Close();
                return;
            }

            // Take all (Tab)
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                TakeAll();
                return;
            }

            // Mouse hover — update cursor to follow mouse
            {
                int hoverRow = GetRowAtMouse();
                if (hoverRow >= 0 && hoverRow != _cursorIndex)
                {
                    _cursorIndex = hoverRow;
                    Render();
                }
            }

            // Mouse click
            if (Input.GetMouseButtonDown(0))
            {
                int clickedRow = GetRowAtMouse();
                if (clickedRow >= 0 && clickedRow < _items.Count)
                {
                    PickupItem(clickedRow);
                    return;
                }
                else if (clickedRow < 0)
                {
                    Close();
                    return;
                }
            }

            // Up/Down navigation
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.K))
            {
                if (_cursorIndex > 0)
                {
                    _cursorIndex--;
                    ScrollIntoView();
                }
                Render();
                return;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.J))
            {
                if (_cursorIndex < _items.Count - 1)
                {
                    _cursorIndex++;
                    ScrollIntoView();
                }
                Render();
                return;
            }

            // Enter/Space: pick up selected
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                if (_items.Count > 0)
                    PickupItem(_cursorIndex);
                return;
            }

            // Hotkeys a-z
            for (int i = 0; i < 26 && i < _items.Count; i++)
            {
                if (Input.GetKeyDown(KeyCode.A + i))
                {
                    PickupItem(i);
                    return;
                }
            }
        }

        private void PickupItem(int index)
        {
            if (index < 0 || index >= _items.Count) return;

            var item = _items[index];
            if (TryPickupViaCommand(item))
            {
                _pickedUpAny = true;
                _items.RemoveAt(index);

                if (_items.Count == 0)
                {
                    Close();
                    return;
                }

                // Clamp cursor
                if (_cursorIndex >= _items.Count)
                    _cursorIndex = _items.Count - 1;
                ScrollIntoView();
            }

            Render();
        }

        private void TakeAll()
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (TryPickupViaCommand(_items[i]))
                {
                    _pickedUpAny = true;
                    _items.RemoveAt(i);
                }
            }

            Close();
        }

        /// <summary>
        /// Command-first pickup seam.
        /// </summary>
        private bool TryPickupViaCommand(Entity item)
        {
            if (item == null)
                return false;

            var result = InventorySystem.ExecuteCommand(
                new PickupCommand(item),
                PlayerEntity,
                CurrentZone);

            if (result.Success)
                return true;

            Debug.LogWarning(
                "[Inventory/Refactor] PickupUI command failed. " +
                $"Code={result.ErrorCode}, Message={result.ErrorMessage}");
            return false;
        }

        private void ScrollIntoView()
        {
            int visible = Mathf.Min(_items.Count, POPUP_MAX_VISIBLE);
            if (visible <= 0) return;
            if (_cursorIndex < _scrollOffset)
                _scrollOffset = _cursorIndex;
            else if (_cursorIndex >= _scrollOffset + visible)
                _scrollOffset = _cursorIndex - visible + 1;
        }

        // ===== Rendering =====

        /// <summary>
        /// Compute popup world-space anchor from camera position.
        /// The popup is centered on the camera's visible area.
        /// </summary>
        private void ComputePopupPosition()
        {
            var cam = Camera.main;
            if (cam == null) return;

            int totalRows = _items.Count;
            int visibleCount = Mathf.Min(totalRows > 0 ? totalRows : 1, POPUP_MAX_VISIBLE);
            _popupH = visibleCount + 5; // top border + title + separator + content + bottom border + action bar

            float camX = cam.transform.position.x;
            float camY = cam.transform.position.y;

            _worldOriginX = Mathf.RoundToInt(camX) - POPUP_W / 2;
            _worldTopY = Mathf.RoundToInt(camY) + _popupH / 2;
        }

        private void Render()
        {
            if (Tilemap == null) return;

            ComputePopupPosition();

            int totalRows = _items.Count;
            int visibleCount = Mathf.Min(totalRows > 0 ? totalRows : 1, POPUP_MAX_VISIBLE);
            int borderH = visibleCount + 4; // border rows (not including action bar)

            // Clear popup area to dark background (null tiles show camera bg color)
            ClearRegion(0, 0, POPUP_W, _popupH);

            // Border
            DrawPopupBorder(0, 0, POPUP_W, borderH, visibleCount);

            // Title
            string title = "Pick up (" + totalRows + " item" + (totalRows != 1 ? "s" : "") + ")";
            DrawText(2, 1, title, QudColorParser.BrightYellow);

            // Tab hint on right side of title
            string hint = "[Tab]take all";
            DrawText(POPUP_W - 2 - hint.Length, 1, hint, QudColorParser.DarkGray);

            // Content rows
            int contentY = 3;
            if (totalRows == 0)
            {
                DrawText(2, contentY, "(no items)", QudColorParser.DarkGray);
            }
            else
            {
                for (int vi = 0; vi < visibleCount; vi++)
                {
                    int idx = _scrollOffset + vi;
                    if (idx >= totalRows) break;

                    int rowY = contentY + vi;
                    bool selected = (idx == _cursorIndex);
                    var item = _items[idx];

                    if (selected)
                        DrawChar(1, rowY, '>', QudColorParser.White);

                    // Hotkey
                    if (idx < 26)
                    {
                        char hotkey = (char)('a' + idx);
                        string hk = hotkey + ")";
                        DrawText(2, rowY, hk,
                            selected ? QudColorParser.White : QudColorParser.Gray);
                    }

                    // Item glyph
                    var render = item.GetPart<RenderPart>();
                    if (render != null && !string.IsNullOrEmpty(render.RenderString))
                    {
                        char glyph = render.RenderString[0];
                        Color glyphColor = QudColorParser.Parse(render.ColorString);
                        DrawChar(5, rowY, glyph, glyphColor);
                    }

                    // Item name + stack count
                    string name = item.GetDisplayName();
                    var stacker = item.GetPart<StackerPart>();
                    if (stacker != null && stacker.StackCount > 1)
                        name += " (x" + stacker.StackCount + ")";

                    int maxNameLen = POPUP_W - 9;
                    if (name.Length > maxNameLen)
                        name = name.Substring(0, maxNameLen - 1) + "~";

                    Color nameColor = selected ? QudColorParser.White : QudColorParser.Gray;
                    DrawText(7, rowY, name, nameColor);
                }

                // Scroll indicators
                if (_scrollOffset > 0)
                    DrawChar(POPUP_W - 2, contentY, '^', QudColorParser.Gray);
                if (_scrollOffset + visibleCount < totalRows)
                    DrawChar(POPUP_W - 2, contentY + visibleCount - 1, 'v', QudColorParser.Gray);
            }

            // Action bar below popup border
            string actions = " [Enter]take [Tab]take all [a-z]select [Esc/g]close";
            DrawText(0, borderH, actions, QudColorParser.DarkGray);
        }

        // ===== Mouse =====

        private int GetRowAtMouse()
        {
            var cam = Camera.main;
            if (cam == null) return -1;

            Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
            int worldX = Mathf.FloorToInt(world.x);
            int worldY = Mathf.FloorToInt(world.y);

            // Convert world tile to popup grid coords
            int gx = worldX - _worldOriginX;
            int gy = _worldTopY - worldY;

            int totalRows = _items.Count;
            int visibleCount = Mathf.Min(totalRows, POPUP_MAX_VISIBLE);
            int contentY = 3;

            if (gx > 0 && gx < POPUP_W - 1
                && gy >= contentY && gy < contentY + visibleCount)
            {
                int vi = gy - contentY;
                int rowIdx = _scrollOffset + vi;
                if (rowIdx < totalRows)
                    return rowIdx;
            }
            return -1;
        }

        // ===== Drawing Helpers =====
        // All coordinates are in popup-local grid space:
        //   (0,0) = top-left of popup, X increases right, Y increases down.
        // Internally converted to world tile positions via _worldOriginX / _worldTopY.

        /// <summary>
        /// Clear a region in popup grid space by setting tiles to null.
        /// The camera's dark background color shows through cleared tiles.
        /// </summary>
        private void ClearRegion(int gx, int gy, int width, int height)
        {
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    int wx = _worldOriginX + gx + dx;
                    int wy = _worldTopY - (gy + dy);
                    Tilemap.SetTile(new Vector3Int(wx, wy, 0), null);
                }
            }
        }

        private void DrawPopupBorder(int x, int y, int w, int h, int contentRows)
        {
            // Top border
            DrawChar(x, y, '+', QudColorParser.Gray);
            for (int i = 1; i < w - 1; i++)
                DrawChar(x + i, y, '-', QudColorParser.Gray);
            DrawChar(x + w - 1, y, '+', QudColorParser.Gray);

            // Title row sides
            DrawChar(x, y + 1, '|', QudColorParser.Gray);
            DrawChar(x + w - 1, y + 1, '|', QudColorParser.Gray);

            // Separator under title
            DrawChar(x, y + 2, '+', QudColorParser.Gray);
            for (int i = 1; i < w - 1; i++)
                DrawChar(x + i, y + 2, '-', QudColorParser.Gray);
            DrawChar(x + w - 1, y + 2, '+', QudColorParser.Gray);

            // Content row sides
            for (int r = 0; r < contentRows; r++)
            {
                DrawChar(x, y + 3 + r, '|', QudColorParser.Gray);
                DrawChar(x + w - 1, y + 3 + r, '|', QudColorParser.Gray);
            }

            // Bottom border
            int botY = y + 3 + contentRows;
            DrawChar(x, botY, '+', QudColorParser.Gray);
            for (int i = 1; i < w - 1; i++)
                DrawChar(x + i, botY, '-', QudColorParser.Gray);
            DrawChar(x + w - 1, botY, '+', QudColorParser.Gray);
        }

        /// <summary>
        /// Draw a character at popup grid coordinates.
        /// Converts to world tile position before setting the tile.
        /// </summary>
        private void DrawChar(int gx, int gy, char c, Color color)
        {
            if (Tilemap == null) return;
            int wx = _worldOriginX + gx;
            int wy = _worldTopY - gy;
            var tilePos = new Vector3Int(wx, wy, 0);
            var tile = CP437TilesetGenerator.GetTile(c);
            if (tile == null) return;
            Tilemap.SetTile(tilePos, tile);
            Tilemap.SetTileFlags(tilePos, TileFlags.None);
            Tilemap.SetColor(tilePos, color);
        }

        /// <summary>
        /// Draw text at popup grid coordinates.
        /// </summary>
        private void DrawText(int gx, int gy, string text, Color color)
        {
            if (text == null || Tilemap == null) return;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == ' ') continue;
                DrawChar(gx + i, gy, c, color);
            }
        }
    }
}
