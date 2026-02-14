using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Renders a popup that lets the player choose one container from multiple
    /// nearby containers before executing container transfer actions.
    /// </summary>
    public class ContainerPickerUI : MonoBehaviour
    {
        public Tilemap Tilemap;

        private const int POPUP_W = 54;
        private const int POPUP_MAX_VISIBLE = 12;

        private bool _isOpen;
        private readonly List<Entity> _containers = new List<Entity>();
        private int _cursorIndex;
        private int _scrollOffset;

        private bool _selectionMade;
        private Entity _selectedContainer;

        private int _worldOriginX;
        private int _worldTopY;
        private int _popupH;

        public bool IsOpen => _isOpen;
        public bool SelectionMade => _selectionMade;
        public Entity SelectedContainer => _selectedContainer;

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
            Render();
        }

        public void HandleInput()
        {
            if (!_isOpen)
                return;

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.G))
            {
                Cancel();
                return;
            }

            int hoverRow = GetRowAtMouse();
            if (hoverRow >= 0 && hoverRow != _cursorIndex)
            {
                _cursorIndex = hoverRow;
                Render();
            }

            if (Input.GetMouseButtonDown(0))
            {
                int clickedRow = GetRowAtMouse();
                if (clickedRow >= 0 && clickedRow < _containers.Count)
                {
                    SelectContainer(clickedRow);
                    return;
                }

                if (clickedRow < 0)
                {
                    Cancel();
                    return;
                }
            }

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
                if (_cursorIndex < _containers.Count - 1)
                {
                    _cursorIndex++;
                    ScrollIntoView();
                }
                Render();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                SelectContainer(_cursorIndex);
                return;
            }

            for (int i = 0; i < 26 && i < _containers.Count; i++)
            {
                if (Input.GetKeyDown(KeyCode.A + i))
                {
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
            Close();
        }

        private void Close()
        {
            _isOpen = false;
            _containers.Clear();
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
            var cam = Camera.main;
            if (cam == null)
                return;

            int totalRows = _containers.Count;
            int visibleCount = Mathf.Min(totalRows > 0 ? totalRows : 1, POPUP_MAX_VISIBLE);
            _popupH = visibleCount + 5;

            float camX = cam.transform.position.x;
            float camY = cam.transform.position.y;

            _worldOriginX = Mathf.RoundToInt(camX) - POPUP_W / 2;
            _worldTopY = Mathf.RoundToInt(camY) + _popupH / 2;
        }

        private void Render()
        {
            if (Tilemap == null)
                return;

            ComputePopupPosition();

            int totalRows = _containers.Count;
            int visibleCount = Mathf.Min(totalRows > 0 ? totalRows : 1, POPUP_MAX_VISIBLE);
            int borderH = visibleCount + 4;

            ClearRegion(0, 0, POPUP_W, _popupH);
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

                    if (idx < 26)
                    {
                        char hotkey = (char)('a' + idx);
                        DrawText(2, rowY, hotkey + ")", selected ? QudColorParser.White : QudColorParser.Gray);
                    }

                    var render = container.GetPart<RenderPart>();
                    if (render != null && !string.IsNullOrEmpty(render.RenderString))
                    {
                        char glyph = render.RenderString[0];
                        Color glyphColor = QudColorParser.Parse(render.ColorString);
                        DrawChar(5, rowY, glyph, glyphColor);
                    }

                    string name = container.GetDisplayName();
                    var containerPart = container.GetPart<ContainerPart>();
                    if (containerPart != null)
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
        }

        private int GetRowAtMouse()
        {
            var cam = Camera.main;
            if (cam == null)
                return -1;

            Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
            int worldX = Mathf.FloorToInt(world.x);
            int worldY = Mathf.FloorToInt(world.y);

            int gx = worldX - _worldOriginX;
            int gy = _worldTopY - worldY;

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
            DrawChar(x, y, '+', QudColorParser.Gray);
            for (int i = 1; i < w - 1; i++)
                DrawChar(x + i, y, '-', QudColorParser.Gray);
            DrawChar(x + w - 1, y, '+', QudColorParser.Gray);

            DrawChar(x, y + 1, '|', QudColorParser.Gray);
            DrawChar(x + w - 1, y + 1, '|', QudColorParser.Gray);

            DrawChar(x, y + 2, '+', QudColorParser.Gray);
            for (int i = 1; i < w - 1; i++)
                DrawChar(x + i, y + 2, '-', QudColorParser.Gray);
            DrawChar(x + w - 1, y + 2, '+', QudColorParser.Gray);

            for (int r = 0; r < contentRows; r++)
            {
                DrawChar(x, y + 3 + r, '|', QudColorParser.Gray);
                DrawChar(x + w - 1, y + 3 + r, '|', QudColorParser.Gray);
            }

            int botY = y + 3 + contentRows;
            DrawChar(x, botY, '+', QudColorParser.Gray);
            for (int i = 1; i < w - 1; i++)
                DrawChar(x + i, botY, '-', QudColorParser.Gray);
            DrawChar(x + w - 1, botY, '+', QudColorParser.Gray);
        }

        private void DrawChar(int gx, int gy, char c, Color color)
        {
            if (Tilemap == null)
                return;

            int wx = _worldOriginX + gx;
            int wy = _worldTopY - gy;
            var tilePos = new Vector3Int(wx, wy, 0);
            var tile = CP437TilesetGenerator.GetTile(c);
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
