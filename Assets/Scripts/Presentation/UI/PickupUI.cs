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
        public Tilemap BgTilemap;
        public Camera PopupCamera;
        public Entity PlayerEntity;
        public Zone CurrentZone;

        private const int POPUP_W = 46;
        private const int POPUP_MAX_VISIBLE = 20;
        private static readonly Color PopupBgColor = new Color(0f, 0f, 0f, 1f);

        private bool _isOpen;
        private List<Entity> _items = new List<Entity>();
        private int _cursorIndex;
        private int _scrollOffset;
        private bool _pickedUpAny;
        private string _statusMessage;

        /// <summary>
        /// When non-null, the popup is operating in "container loot" mode —
        /// it pulls items FROM this container via TakeFromContainerCommand
        /// instead of FROM the ground via PickupCommand. Set by the
        /// <see cref="Open(List{Entity}, Entity)"/> overload. Chest-opening
        /// path in InputHandler uses this seam.
        /// </summary>
        private Entity _sourceContainer;

        // Popup anchor in centered popup-grid coordinates.
        // All drawing uses popup-local grid coords (0,0 = top-left, Y down)
        // which are converted to overlay grid coords via these anchors.
        private int _worldOriginX;
        private int _worldTopY;
        private int _popupH;
        private bool _bgDrawn;
        private int _bgDrawnW;
        private int _bgDrawnH;
        private int _bgDrawnOriginX;
        private int _bgDrawnTopY;

        public bool IsOpen => _isOpen;
        public bool PickedUpAny => _pickedUpAny;

        public void Open(List<Entity> items)
        {
            Open(items, sourceContainer: null);
        }

        /// <summary>
        /// Open in "container loot" mode. Items are taken FROM <paramref name="sourceContainer"/>
        /// via TakeFromContainerCommand instead of from the ground. The passed
        /// item list should typically be <c>container.GetPart&lt;ContainerPart&gt;().Contents</c>.
        /// </summary>
        public void Open(List<Entity> items, Entity sourceContainer)
        {
            if (items == null || items.Count == 0) return;
            _isOpen = true;
            _items = new List<Entity>(items);
            _sourceContainer = sourceContainer;
            _cursorIndex = 0;
            _scrollOffset = 0;
            _pickedUpAny = false;
            _statusMessage = null;
            Render();
        }

        public void Close()
        {
            ClearRegion(0, 0, POPUP_W, _popupH);
            ClearBgRegion();
            _isOpen = false;
            _items.Clear();
            _sourceContainer = null;
        }

        public void HandleInput()
        {
            if (!_isOpen) return;

            // Close on Escape or G
            if (InputHelper.GetKeyDown(KeyCode.Escape) || InputHelper.GetKeyDown(KeyCode.G))
            {
                Close();
                return;
            }

            // Take all (Tab)
            if (InputHelper.GetKeyDown(KeyCode.Tab))
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
            if (InputHelper.GetKeyDown(KeyCode.UpArrow) || InputHelper.GetKeyDown(KeyCode.K))
            {
                if (_cursorIndex > 0)
                {
                    _cursorIndex--;
                    ScrollIntoView();
                }
                Render();
                return;
            }

            if (InputHelper.GetKeyDown(KeyCode.DownArrow) || InputHelper.GetKeyDown(KeyCode.J))
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
            if (InputHelper.GetKeyDown(KeyCode.Return) || InputHelper.GetKeyDown(KeyCode.Space))
            {
                if (_items.Count > 0)
                    PickupItem(_cursorIndex);
                return;
            }

            // Hotkeys a-z
            for (int i = 0; i < 26 && i < _items.Count; i++)
            {
                if (InputHelper.GetKeyDown(KeyCode.A + i))
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
            string error;
            if (TryPickupViaCommand(item, out error))
            {
                _statusMessage = null;
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
            else
            {
                _statusMessage = error ?? $"Can't pick up {item.GetDisplayName()}!";
            }

            Render();
        }

        private void TakeAll()
        {
            string unused;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (TryPickupViaCommand(_items[i], out unused))
                {
                    _pickedUpAny = true;
                    _items.RemoveAt(i);
                }
            }

            Close();
        }

        /// <summary>
        /// Command-first pickup seam. Dispatches to PickupCommand (ground)
        /// or TakeFromContainerCommand (container-loot mode) depending on
        /// whether <see cref="_sourceContainer"/> was set by Open().
        /// </summary>
        private bool TryPickupViaCommand(Entity item, out string errorMessage)
        {
            errorMessage = null;
            if (item == null)
                return false;

            InventoryCommandResult result;
            if (_sourceContainer != null)
            {
                result = InventorySystem.ExecuteCommand(
                    new TakeFromContainerCommand(_sourceContainer, item),
                    PlayerEntity,
                    CurrentZone);
            }
            else
            {
                result = InventorySystem.ExecuteCommand(
                    new PickupCommand(item),
                    PlayerEntity,
                    CurrentZone);
            }

            if (result.Success)
                return true;

            errorMessage = result.ErrorMessage;
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
        /// Compute popup anchor from the fixed centered popup grid.
        /// </summary>
        private void ComputePopupPosition()
        {
            int totalRows = _items.Count;
            int visibleCount = Mathf.Min(totalRows > 0 ? totalRows : 1, POPUP_MAX_VISIBLE);
            _popupH = visibleCount + 6; // top border + title + separator + content + bottom border + action bar + status
            _worldOriginX = CenteredPopupLayout.GetCenteredOriginX(POPUP_W);
            _worldTopY = CenteredPopupLayout.GetCenteredTopY(_popupH);
        }

        private void Render()
        {
            if (Tilemap == null) return;

            ClearBgRegion();
            ComputePopupPosition();

            int totalRows = _items.Count;
            int visibleCount = Mathf.Min(totalRows > 0 ? totalRows : 1, POPUP_MAX_VISIBLE);
            int borderH = visibleCount + 4; // border rows (not including action bar)

            ClearRegion(0, 0, POPUP_W, _popupH);
            DrawBgFill(0, 0, POPUP_W, borderH);

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

            // Status message (e.g. "too heavy" feedback)
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                string msg = _statusMessage;
                if (msg.Length > POPUP_W - 2)
                    msg = msg.Substring(0, POPUP_W - 2);
                DrawText(1, borderH + 1, msg, QudColorParser.BrightRed);
            }
        }

        // ===== Mouse =====

        private int GetRowAtMouse()
        {
            if (!CenteredPopupLayout.ScreenToGrid(PopupCamera, Tilemap, Input.mousePosition, out int gridX, out int gridY))
                return -1;

            int gx = gridX - _worldOriginX;
            int gy = _worldTopY - gridY;

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

        private void DrawBgFill(int gx, int gy, int width, int height)
        {
            if (BgTilemap == null) return;
            var blockTile = CP437TilesetGenerator.GetTile(CP437TilesetGenerator.SolidBlock);
            if (blockTile == null) return;

            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    int wx = _worldOriginX + gx + dx;
                    int wy = _worldTopY - (gy + dy);
                    var pos = new Vector3Int(wx, wy, 0);
                    BgTilemap.SetTile(pos, blockTile);
                    BgTilemap.SetTileFlags(pos, TileFlags.None);
                    BgTilemap.SetColor(pos, PopupBgColor);
                }
            }

            _bgDrawn = true;
            _bgDrawnW = width;
            _bgDrawnH = height;
            _bgDrawnOriginX = _worldOriginX + gx;
            _bgDrawnTopY = _worldTopY - gy;
        }

        private void ClearBgRegion()
        {
            if (!_bgDrawn || BgTilemap == null) return;

            for (int dy = 0; dy < _bgDrawnH; dy++)
            {
                for (int dx = 0; dx < _bgDrawnW; dx++)
                {
                    int wx = _bgDrawnOriginX + dx;
                    int wy = _bgDrawnTopY - dy;
                    BgTilemap.SetTile(new Vector3Int(wx, wy, 0), null);
                }
            }

            _bgDrawn = false;
        }

        private void DrawPopupBorder(int x, int y, int w, int h, int contentRows)
        {
            // Top border
            DrawChar(x, y, CP437TilesetGenerator.BoxTopLeft, QudColorParser.Gray);
            for (int i = 1; i < w - 1; i++)
                DrawChar(x + i, y, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);
            DrawChar(x + w - 1, y, CP437TilesetGenerator.BoxTopRight, QudColorParser.Gray);

            // Title row sides
            DrawChar(x, y + 1, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
            DrawChar(x + w - 1, y + 1, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);

            // Separator under title
            DrawChar(x, y + 2, CP437TilesetGenerator.BoxTeeLeft, QudColorParser.Gray);
            for (int i = 1; i < w - 1; i++)
                DrawChar(x + i, y + 2, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);
            DrawChar(x + w - 1, y + 2, CP437TilesetGenerator.BoxTeeRight, QudColorParser.Gray);

            // Content row sides
            for (int r = 0; r < contentRows; r++)
            {
                DrawChar(x, y + 3 + r, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
                DrawChar(x + w - 1, y + 3 + r, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
            }

            // Bottom border
            int botY = y + 3 + contentRows;
            DrawChar(x, botY, CP437TilesetGenerator.BoxBottomLeft, QudColorParser.Gray);
            for (int i = 1; i < w - 1; i++)
                DrawChar(x + i, botY, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);
            DrawChar(x + w - 1, botY, CP437TilesetGenerator.BoxBottomRight, QudColorParser.Gray);
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
