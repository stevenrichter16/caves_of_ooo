using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using static CavesOfOoo.Rendering.LedgerPalette;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Renders a popup that lets the player choose one container from multiple
    /// nearby containers before executing container transfer actions.
    /// </summary>
    public class ContainerPickerUI : MonoBehaviour
    {
        public Tilemap Tilemap;
        public Tilemap BgTilemap;
        public Camera PopupCamera;
        internal bool UseLedgerView;

        private const int POPUP_W = 54;
        private const int POPUP_MAX_VISIBLE = 12;
        private static readonly Color PopupBgColor = new Color(0f, 0f, 0f, 1f);
        private const int LedgerWidth = 760;
        private const int LedgerInset = 24;
        private const int LedgerRowsTop = 68;
        private const int LedgerRowHeight = 38;
        private const int LedgerCounterHeight = 24;
        private const int LedgerFooterHeight = 44;

        private bool _isOpen;
        private readonly List<Entity> _containers = new List<Entity>();
        private int _cursorIndex;
        private int _scrollOffset;
        private LedgerModalView _ledgerView;
        private Vector2 _lastLedgerPointer;

        private bool _selectionMade;
        private Entity _selectedContainer;

        private int _worldOriginX;
        private int _worldTopY;
        private int _popupH;
        private bool _fgDrawn;
        private bool _bgDrawn;
        private int _bgDrawnW;
        private int _bgDrawnH;
        private int _bgDrawnOriginX;
        private int _bgDrawnTopY;

        public bool IsOpen => _isOpen;
        public bool SelectionMade => _selectionMade;
        public Entity SelectedContainer => _selectedContainer;
        /// <summary>Pressed selection letter, or None for mouse/confirm/cancel.
        /// The caller reads this on close before normal held movement resumes.</summary>
        public KeyCode SelectedActivationKey { get; private set; }

        public void Open(List<Entity> containers)
        {
            if (containers == null || containers.Count == 0)
                return;

            _isOpen = true;
            _containers.Clear();
            _containers.AddRange(containers);
            _cursorIndex = 0;
            _scrollOffset = 0;
            _selectionMade = false;
            _selectedContainer = null;
            SelectedActivationKey = KeyCode.None;
            if (UseLedgerView) _lastLedgerPointer = ReadLedgerPointer(out _);
            Render();
        }

        public void HandleInput()
        {
            if (!_isOpen)
                return;
            SelectedActivationKey = KeyCode.None;

            if (InputHelper.GetKeyDown(KeyCode.Escape) || InputHelper.GetKeyDown(KeyCode.G))
            {
                Cancel();
                return;
            }

            int pointerRow;
            bool clicked;
            if (UseLedgerView)
            {
                Vector2 pointer = ReadLedgerPointer(out clicked);
                pointerRow = clicked || pointer != _lastLedgerPointer ? GetLedgerRow(pointer) : -1;
                _lastLedgerPointer = pointer;
            }
            else
            {
                pointerRow = GetRowAtMouse();
                clicked = Input.GetMouseButtonDown(0);
            }

            if (pointerRow >= 0 && pointerRow != _cursorIndex)
            {
                _cursorIndex = pointerRow;
                Render();
            }

            if (clicked)
            {
                if (pointerRow >= 0) SelectContainer(pointerRow);
                else Cancel();
                return;
            }

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
                if (_cursorIndex < _containers.Count - 1)
                {
                    _cursorIndex++;
                    ScrollIntoView();
                }
                Render();
                return;
            }

            if (InputHelper.GetKeyDown(KeyCode.Return) || InputHelper.GetKeyDown(KeyCode.Space))
            {
                SelectContainer(_cursorIndex);
                return;
            }

            for (int i = 0; i < _containers.Count; i++)
            {
                var key = MenuShortcutMap.Key(MenuShortcutMap.Positional(i, closesWithG: true));
                if (key == KeyCode.None) break;
                if (InputHelper.GetKeyDown(key))
                {
                    SelectedActivationKey = key;
                    SelectContainer(i);
                    return;
                }
            }
        }

        private void SelectContainer(int index)
        {
            if (index < 0 || index >= _containers.Count)
                return;

            _selectionMade = true;
            _selectedContainer = _containers[index];
            Close();
        }

        private void Cancel()
        {
            _selectionMade = false;
            _selectedContainer = null;
            SelectedActivationKey = KeyCode.None;
            Close();
        }

        private void Close()
        {
            _ledgerView?.Hide();
            ClearFgRegion();
            ClearBgRegion();
            _isOpen = false;
            _containers.Clear();
        }

        private void OnDisable()
        {
            if (_isOpen) Cancel();
        }

        private void ScrollIntoView()
        {
            int visible = Mathf.Min(_containers.Count, POPUP_MAX_VISIBLE);
            if (visible <= 0)
                return;

            if (_cursorIndex < _scrollOffset)
                _scrollOffset = _cursorIndex;
            else if (_cursorIndex >= _scrollOffset + visible)
                _scrollOffset = _cursorIndex - visible + 1;
        }

        private void ComputePopupPosition()
        {
            int totalRows = _containers.Count;
            int visibleCount = Mathf.Min(totalRows > 0 ? totalRows : 1, POPUP_MAX_VISIBLE);
            _popupH = visibleCount + 5;
            _worldOriginX = CenteredPopupLayout.GetCenteredOriginX(POPUP_W);
            _worldTopY = CenteredPopupLayout.GetCenteredTopY(_popupH);
        }

        private void Render()
        {
            ClearFgRegion();
            ClearBgRegion();
            if (UseLedgerView)
            {
                RenderLedger();
                return;
            }

            _ledgerView?.Hide();
            if (Tilemap == null)
                return;

            ComputePopupPosition();

            int totalRows = _containers.Count;
            int visibleCount = Mathf.Min(totalRows > 0 ? totalRows : 1, POPUP_MAX_VISIBLE);
            int borderH = visibleCount + 4;

            ClearRegion(0, 0, POPUP_W, _popupH);
            DrawBgFill(0, 0, POPUP_W, borderH);
            DrawPopupBorder(0, 0, POPUP_W, borderH, visibleCount);

            string title = "Choose container (" + totalRows + ")";
            DrawText(2, 1, title, QudColorParser.BrightYellow);
            DrawText(POPUP_W - 28, 1, "[Enter]select [Esc/g]cancel", QudColorParser.DarkGray);

            int contentY = 3;
            if (totalRows == 0)
            {
                DrawText(2, contentY, "(no containers)", QudColorParser.DarkGray);
            }
            else
            {
                for (int vi = 0; vi < visibleCount; vi++)
                {
                    int idx = _scrollOffset + vi;
                    if (idx >= totalRows)
                        break;

                    int rowY = contentY + vi;
                    bool selected = idx == _cursorIndex;
                    var container = _containers[idx];

                    if (selected)
                        DrawChar(1, rowY, '>', QudColorParser.White);

                    char hotkey = MenuShortcutMap.Positional(idx, closesWithG: true);
                    if (hotkey != '\0')
                    {
                        DrawText(2, rowY, hotkey + ")", selected ? QudColorParser.White : QudColorParser.Gray);
                    }

                    InventoryItemIcons.Paint(Tilemap, new Vector3Int(_worldOriginX + 5, _worldTopY - rowY, 0), container);

                    string name = container.GetDisplayName();
                    var containerPart = container.GetPart<ContainerPart>();
                    if (containerPart?.NeedsRepair == true)
                        name += " (needs repair)";
                    else if (containerPart != null)
                        name += " (" + containerPart.Contents.Count + " item" + (containerPart.Contents.Count == 1 ? "" : "s") + ")";

                    int maxNameLen = POPUP_W - 9;
                    if (name.Length > maxNameLen)
                        name = name.Substring(0, maxNameLen - 1) + "~";

                    DrawText(7, rowY, name, selected ? QudColorParser.White : QudColorParser.Gray);
                }

                if (_scrollOffset > 0)
                    DrawChar(POPUP_W - 2, contentY, '^', QudColorParser.Gray);
                if (_scrollOffset + visibleCount < totalRows)
                    DrawChar(POPUP_W - 2, contentY + visibleCount - 1, 'v', QudColorParser.Gray);
            }

            DrawText(0, borderH, " Select a container to take all contents.", QudColorParser.DarkGray);
            _fgDrawn = true;
        }

        private void RenderLedger()
        {
            int count = _containers.Count;
            int visible = Mathf.Min(count, POPUP_MAX_VISIBLE);
            int rowsEnd = LedgerRowsTop + visible * LedgerRowHeight;
            int counterHeight = count > visible ? LedgerCounterHeight : 0;
            _ledgerView ??= LedgerModalView.Create(transform, "ContainerPickerLedger");
            _ledgerView.Begin(PopupCamera, LedgerWidth, rowsEnd + counterHeight + LedgerFooterHeight);
            _ledgerView.Card("Choose container (" + count + ")", NativeGamepadInput.IsConnected
                ? "[A] Take all contents   [B] Cancel"
                : "[Enter] Take all contents   [Esc/G] Cancel");

            for (int i = 0; i < visible; i++)
            {
                int index = _scrollOffset + i;
                if (index >= count) break;
                Entity container = _containers[index];
                ContainerPart contents = container.GetPart<ContainerPart>();
                string aside = null;
                if (contents?.NeedsRepair == true)
                    aside = "Needs repair";
                else if (contents != null)
                    aside = contents.Contents.Count + (contents.Contents.Count == 1 ? " item" : " items");
                char key = MenuShortcutMap.Positional(index, closesWithG: true);
                _ledgerView.Row(LedgerRowBounds(i), container.GetDisplayName(), index == _cursorIndex,
                    key == '\0' ? null : key.ToString(), aside, icon: InventoryItemIcons.For(container));
            }

            if (counterHeight > 0)
                _ledgerView.Label(new Rect(LedgerInset, rowsEnd, LedgerWidth - 2 * LedgerInset, counterHeight),
                    $"{_scrollOffset + 1}–{_scrollOffset + visible} of {count}", LMuted, 16, TextAnchor.MiddleRight);
            _ledgerView.End();
        }

        private static Rect LedgerRowBounds(int row)
            => new Rect(LedgerInset, LedgerRowsTop + row * LedgerRowHeight, LedgerWidth - 2 * LedgerInset, LedgerRowHeight);

        private int GetLedgerRow(Vector2 screen)
        {
            if (_ledgerView == null || !_ledgerView.TryPoint(screen, out var point)) return -1;
            int row = Mathf.FloorToInt((point.y - LedgerRowsTop) / LedgerRowHeight);
            int index = _scrollOffset + row;
            return row >= 0 && row < POPUP_MAX_VISIBLE && index < _containers.Count && LedgerRowBounds(row).Contains(point) ? index : -1;
        }

        private static Vector2 ReadLedgerPointer(out bool clicked)
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.enabled)
            {
                clicked = mouse.leftButton.wasPressedThisFrame;
                return mouse.position.ReadValue();
            }

            clicked = Input.GetMouseButtonDown(0);
            return Input.mousePosition;
        }

        private int GetRowAtMouse()
        {
            if (!CenteredPopupLayout.ScreenToGrid(PopupCamera, Tilemap, Input.mousePosition, out int gridX, out int gridY))
                return -1;

            int gx = gridX - _worldOriginX;
            int gy = _worldTopY - gridY;

            int totalRows = _containers.Count;
            int visibleCount = Mathf.Min(totalRows, POPUP_MAX_VISIBLE);
            int contentY = 3;

            if (gx > 0 && gx < POPUP_W - 1 && gy >= contentY && gy < contentY + visibleCount)
            {
                int vi = gy - contentY;
                int rowIdx = _scrollOffset + vi;
                if (rowIdx < totalRows)
                    return rowIdx;
            }

            return -1;
        }

        private void ClearRegion(int gx, int gy, int width, int height)
        {
            if (Tilemap == null) return;
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

        private void ClearFgRegion()
        {
            if (!_fgDrawn) return;
            ClearRegion(0, 0, POPUP_W, _popupH);
            _fgDrawn = false;
        }

        private void DrawBgFill(int gx, int gy, int width, int height)
        {
            if (BgTilemap == null) return;
            var blockTile = CP437TilesetGenerator.GetUiTile(CP437TilesetGenerator.SolidBlock);
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
            DrawChar(x, y, CP437TilesetGenerator.BoxTopLeft, QudColorParser.Gray);
            for (int i = 1; i < w - 1; i++)
                DrawChar(x + i, y, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);
            DrawChar(x + w - 1, y, CP437TilesetGenerator.BoxTopRight, QudColorParser.Gray);

            DrawChar(x, y + 1, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
            DrawChar(x + w - 1, y + 1, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);

            DrawChar(x, y + 2, CP437TilesetGenerator.BoxTeeLeft, QudColorParser.Gray);
            for (int i = 1; i < w - 1; i++)
                DrawChar(x + i, y + 2, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);
            DrawChar(x + w - 1, y + 2, CP437TilesetGenerator.BoxTeeRight, QudColorParser.Gray);

            for (int r = 0; r < contentRows; r++)
            {
                DrawChar(x, y + 3 + r, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
                DrawChar(x + w - 1, y + 3 + r, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
            }

            int botY = y + 3 + contentRows;
            DrawChar(x, botY, CP437TilesetGenerator.BoxBottomLeft, QudColorParser.Gray);
            for (int i = 1; i < w - 1; i++)
                DrawChar(x + i, botY, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);
            DrawChar(x + w - 1, botY, CP437TilesetGenerator.BoxBottomRight, QudColorParser.Gray);
        }

        private void DrawChar(int gx, int gy, char c, Color color)
        {
            if (Tilemap == null)
                return;

            int wx = _worldOriginX + gx;
            int wy = _worldTopY - gy;
            var tilePos = new Vector3Int(wx, wy, 0);
            var tile = CP437TilesetGenerator.GetUiTile(c);
            if (tile == null)
                return;

            Tilemap.SetTile(tilePos, tile);
            Tilemap.SetTileFlags(tilePos, TileFlags.None);
            Tilemap.SetColor(tilePos, color);
        }

        private void DrawText(int gx, int gy, string text, Color color)
        {
            if (text == null || Tilemap == null)
                return;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == ' ')
                    continue;

                DrawChar(gx + i, gy, c, color);
            }
        }
    }
}
