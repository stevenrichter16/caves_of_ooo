using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Phase 4d of the World Action Menu feature. Renders a centered popup
    /// listing actions the player can take on the entity at a clicked cell
    /// in look mode. Pattern mirrors <see cref="ContainerPickerUI"/>:
    /// foreground-glyph tilemap + background-fill tilemap + CP437 box chars.
    ///
    /// Lifecycle:
    ///   Open(actor, target, cell, actions)  — caller sets state, menu
    ///                                          appears on next Render
    ///   HandleInput()                       — call per frame while open
    ///   Cancel / Select                     — sets flags, closes menu
    ///   SelectionMade + SelectedAction      — caller polls, executes, and
    ///                                          calls ConsumeSelection
    ///
    /// Action execution is deliberately NOT done here — the caller (input
    /// handler) fires the <c>InventoryAction</c> event and handles side
    /// effects like opening a dialogue UI after a Chat action. This keeps
    /// the UI component dumb and testable.
    ///
    /// "Pile" note: when the target cell has 2+ non-terrain entities
    /// (<see cref="WorldInteractionSystem.IsPileCell"/>), the caller may
    /// want the Examine action to log the cell-level pile summary instead
    /// of the single target's description. The caller detects this by
    /// checking <see cref="SelectedCellIsPile"/> on a resolved Examine
    /// command.
    /// </summary>
    public class WorldActionMenuUI : MonoBehaviour
    {
        public Tilemap Tilemap;
        public Tilemap BgTilemap;
        public Camera PopupCamera;

        private const int POPUP_W = 48;
        private const int POPUP_MAX_VISIBLE = 12;
        private static readonly Color PopupBgColor = new Color(0f, 0f, 0f, 1f);

        // ---- Open state ----
        private bool _isOpen;
        private Entity _actor;
        private Entity _target;
        private Cell _cell;
        private bool _cellIsPile;
        private readonly List<InventoryAction> _actions = new List<InventoryAction>();
        private int _cursorIndex;
        private int _scrollOffset;

        // ---- Selection output ----
        private bool _selectionMade;
        private InventoryAction _selectedAction;
        private bool _selectionCancelled;

        // ---- Popup positioning / bg-fill tracking (copied idioms from
        //      ContainerPickerUI so the tilemap clear logic is identical) ----
        private int _worldOriginX;
        private int _worldTopY;
        private int _popupH;
        private bool _bgDrawn;
        private int _bgDrawnW;
        private int _bgDrawnH;
        private int _bgDrawnOriginX;
        private int _bgDrawnTopY;

        // ---- Public API ----

        public bool IsOpen => _isOpen;
        public bool SelectionMade => _selectionMade;
        public bool SelectionCancelled => _selectionCancelled;
        public InventoryAction SelectedAction => _selectedAction;
        public Entity SelectedTarget => _target;
        public Cell SelectedCell => _cell;
        public bool SelectedCellIsPile => _cellIsPile;
        public Entity Actor => _actor;

        /// <summary>
        /// Show the menu. Caller is responsible for having run target
        /// resolution + action gathering (via <see cref="WorldInteractionSystem"/>).
        /// An empty/null actions list means the menu renders a single row
        /// reading "(no actions available)" — caller can check emptiness
        /// beforehand and skip opening if they prefer.
        /// </summary>
        public void Open(Entity actor, Entity target, Cell cell, List<InventoryAction> actions)
        {
            _isOpen = true;
            _actor = actor;
            _target = target;
            _cell = cell;
            _cellIsPile = WorldInteractionSystem.IsPileCell(cell);
            _actions.Clear();
            if (actions != null)
                _actions.AddRange(actions);

            _cursorIndex = 0;
            _scrollOffset = 0;
            _selectionMade = false;
            _selectionCancelled = false;
            _selectedAction = null;
            // DIAG [Phase4d] — confirm Open ran with usable state.
            UnityEngine.Debug.Log($"[ActionMenu:ui-open] actions={_actions.Count} " +
                $"Tilemap={(Tilemap != null ? "set" : "NULL")} " +
                $"BgTilemap={(BgTilemap != null ? "set" : "NULL")}");
            Render();
        }

        /// <summary>
        /// Clear the selection flags after the caller has consumed the
        /// result. Does NOT reset target/actor/cell — those remain readable
        /// until the next Open() call.
        /// </summary>
        public void ConsumeSelection()
        {
            _selectionMade = false;
            _selectionCancelled = false;
            _selectedAction = null;
        }

        /// <summary>
        /// Per-frame input processing. Returns after any action is
        /// taken (cursor move / select / cancel) so the caller can
        /// immediately poll SelectionMade / SelectionCancelled.
        /// </summary>
        public void HandleInput()
        {
            if (!_isOpen) return;

            if (InputHelper.GetKeyDown(KeyCode.Escape))
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
                if (clickedRow >= 0 && clickedRow < _actions.Count)
                {
                    SelectAction(clickedRow);
                    return;
                }
                if (clickedRow < 0)
                {
                    // Click outside menu area: treat as cancel.
                    Cancel();
                    return;
                }
            }

            if (InputHelper.GetKeyDown(KeyCode.UpArrow) || InputHelper.GetKeyDown(KeyCode.K))
            {
                if (_cursorIndex > 0)
                {
                    _cursorIndex--;
                    ScrollIntoView();
                    Render();
                }
                return;
            }

            if (InputHelper.GetKeyDown(KeyCode.DownArrow) || InputHelper.GetKeyDown(KeyCode.J))
            {
                if (_cursorIndex < _actions.Count - 1)
                {
                    _cursorIndex++;
                    ScrollIntoView();
                    Render();
                }
                return;
            }

            if (InputHelper.GetKeyDown(KeyCode.Return) || InputHelper.GetKeyDown(KeyCode.KeypadEnter))
            {
                SelectAction(_cursorIndex);
                return;
            }

            // Hotkey matching — if any action's Key equals the pressed letter.
            for (int i = 0; i < _actions.Count; i++)
            {
                char k = _actions[i].Key;
                if (k == '\0' || k == ' ') continue;
                if (InputHelper.GetKeyDown(CharToKeyCode(k)))
                {
                    SelectAction(i);
                    return;
                }
            }
        }

        // ---- Selection helpers ----

        private void SelectAction(int index)
        {
            if (index < 0 || index >= _actions.Count) return;
            _selectionMade = true;
            _selectedAction = _actions[index];
            Close();
        }

        private void Cancel()
        {
            _selectionCancelled = true;
            _selectedAction = null;
            Close();
        }

        private void Close()
        {
            ClearRegion(0, 0, POPUP_W, _popupH);
            ClearBgRegion();
            _isOpen = false;
            // Actions kept for the caller; cleared on next Open.
        }

        private void ScrollIntoView()
        {
            int visible = Mathf.Min(_actions.Count, POPUP_MAX_VISIBLE);
            if (visible <= 0) return;

            if (_cursorIndex < _scrollOffset)
                _scrollOffset = _cursorIndex;
            else if (_cursorIndex >= _scrollOffset + visible)
                _scrollOffset = _cursorIndex - visible + 1;
        }

        private static KeyCode CharToKeyCode(char c)
        {
            // Lowercase letters map directly to KeyCode.A..Z (Unity treats
            // `KeyCode.A` as the a key regardless of shift state).
            char lo = char.ToLowerInvariant(c);
            if (lo >= 'a' && lo <= 'z')
                return (KeyCode)((int)KeyCode.A + (lo - 'a'));
            return KeyCode.None;
        }

        // ---- Rendering ----
        //
        // Layout (width = POPUP_W, height = actions + 4 + 1 title line):
        //
        //   ┌──────────────────────────────────────────────┐
        //   │ {title — cell description}                   │
        //   ├──────────────────────────────────────────────┤
        //   │ > a) open                                    │
        //   │   b) examine                                 │
        //   └──────────────────────────────────────────────┘
        //     [Enter]select [Esc]cancel

        private void Render()
        {
            if (Tilemap == null) return;

            ClearBgRegion();
            ComputePopupPosition();

            int totalRows = _actions.Count;
            int visibleCount = Mathf.Min(totalRows > 0 ? totalRows : 1, POPUP_MAX_VISIBLE);
            int borderH = visibleCount + 4;

            ClearRegion(0, 0, POPUP_W, _popupH);
            DrawBgFill(0, 0, POPUP_W, borderH);
            DrawPopupBorder(0, 0, POPUP_W, borderH, visibleCount);

            // Title = cell description (pile / single / terrain / empty)
            string title = WorldInteractionSystem.DescribeCell(_cell);
            int maxTitleLen = POPUP_W - 4;
            if (!string.IsNullOrEmpty(title) && title.Length > maxTitleLen)
                title = title.Substring(0, maxTitleLen - 1) + "~";
            DrawText(2, 1, title, QudColorParser.BrightYellow);

            // Hint row at the bottom-right of title bar
            const string hint = "[Enter]ok [Esc]x";
            int hintX = POPUP_W - hint.Length - 2;
            if (hintX > title.Length + 4)
                DrawText(hintX, 1, hint, QudColorParser.DarkGray);

            int contentY = 3;
            if (totalRows == 0)
            {
                DrawText(2, contentY, "(no actions available)", QudColorParser.DarkGray);
            }
            else
            {
                for (int vi = 0; vi < visibleCount; vi++)
                {
                    int idx = _scrollOffset + vi;
                    if (idx >= totalRows) break;

                    int rowY = contentY + vi;
                    bool selected = idx == _cursorIndex;
                    var action = _actions[idx];

                    if (selected)
                        DrawChar(1, rowY, '>', QudColorParser.White);

                    char hotkey = action.Key != '\0' ? action.Key : ' ';
                    if (hotkey != ' ')
                    {
                        DrawText(2, rowY, hotkey + ")",
                            selected ? QudColorParser.White : QudColorParser.Gray);
                    }

                    string label = action.Display ?? action.Name ?? "";
                    int maxLabelLen = POPUP_W - 7;
                    if (label.Length > maxLabelLen)
                        label = label.Substring(0, maxLabelLen - 1) + "~";
                    DrawText(5, rowY, label,
                        selected ? QudColorParser.White : QudColorParser.Gray);
                }

                if (_scrollOffset > 0)
                    DrawChar(POPUP_W - 2, contentY, '^', QudColorParser.Gray);
                if (_scrollOffset + visibleCount < totalRows)
                    DrawChar(POPUP_W - 2, contentY + visibleCount - 1, 'v', QudColorParser.Gray);
            }
        }

        private void ComputePopupPosition()
        {
            int totalRows = _actions.Count;
            int visibleCount = Mathf.Min(totalRows > 0 ? totalRows : 1, POPUP_MAX_VISIBLE);
            _popupH = visibleCount + 5;
            _worldOriginX = CenteredPopupLayout.GetCenteredOriginX(POPUP_W);
            _worldTopY = CenteredPopupLayout.GetCenteredTopY(_popupH);
        }

        private int GetRowAtMouse()
        {
            if (!CenteredPopupLayout.ScreenToGrid(PopupCamera, Tilemap, Input.mousePosition,
                    out int gridX, out int gridY))
                return -1;

            int gx = gridX - _worldOriginX;
            int gy = _worldTopY - gridY;

            int totalRows = _actions.Count;
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

        // ---- Tilemap primitives (identical to ContainerPickerUI) ----

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
