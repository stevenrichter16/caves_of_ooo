using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using UnityEngine;
using UnityEngine.Tilemaps;
using static CavesOfOoo.Rendering.LedgerPalette;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Draws the new-game build picker and forwards input to
    /// <see cref="StartingBuildMenuController"/>.
    /// </summary>
    public class StartingBuildMenuUI : MonoBehaviour
    {
        public Tilemap Tilemap;
        public Tilemap BgTilemap;
        public Camera PopupCamera;
        internal bool UseLedgerView;

        /// <summary>Source of display names and armor values for the card.</summary>
        public EntityFactory Factory { get; set; }
        public StartingBuildMenuController Controller { get; set; }

        private const int POPUP_W = 76;
        private const int LIST_W = 22;                 // left pane incl. its border column
        private const int CARD_X = LIST_W + 2;         // card text starts two columns after the divider
        private const int CONTENT_ROWS = 22;
        private const int BORDER_H = CONTENT_ROWS + 4; // top + title + separator + content + bottom
        private const int POPUP_H = BORDER_H + 1;      // + hint line
        private const int CONTENT_Y = 3;
        private static readonly Color PopupBgColor = new Color(0f, 0f, 0f, 1f);

        private const int LedgerWidth = 984;
        private const int LedgerHeight = 642;
        private const int LedgerVisibleRows = 9;
        private static readonly Rect LedgerDetailBounds = new Rect(296, 86, 662, 492);
        private static readonly Rect LedgerBeginBounds = new Rect(704, 528, 230, 36);
        private LedgerModalView _ledgerView;
        private int _ledgerFirstRow;
        private bool _ledgerGamepad;
        private bool _hasLedgerPointer;
        private Vector2 _lastLedgerPointer;

        private bool _wasOpenLastFrame;
        private bool _tilesVisible;
        private int _worldOriginX;
        private int _worldTopY;

        public bool IsOpen => Controller != null && Controller.IsOpen;

        /// <summary>Per-frame entry. Returns true when input was consumed.</summary>
        public bool HandleInput(IInputProbe input)
        {
            if (Controller == null) return false;

            bool wasOpen = Controller.IsOpen;
            int prevSelected = Controller.SelectedIndex;

            if (Controller.IsOpen)
            {
                int clicked = ReadClickedRow();
                if (clicked >= 0)
                {
                    Controller.ClickConfirm(clicked);
                    AfterChange(wasOpen);
                    return true;
                }
            }

            bool consumed = Controller.Tick(input);

            if (Controller.IsOpen != wasOpen || (Controller.IsOpen && Controller.SelectedIndex != prevSelected))
                AfterChange(wasOpen);
            else if (Controller.IsOpen && (!_wasOpenLastFrame
                || (UseLedgerView && _ledgerGamepad != NativeGamepadInput.IsConnected)))
                Render();

            _wasOpenLastFrame = Controller.IsOpen;
            return consumed;
        }

        private void AfterChange(bool wasOpenBefore)
        {
            if (Controller.IsOpen) Render();
            else if (wasOpenBefore) ClearAll();
            _wasOpenLastFrame = Controller.IsOpen;
        }

        private void Render()
        {
            var model = Controller?.Model;
            if (model == null) return;
            if (UseLedgerView)
            {
                RenderLedger(model);
                return;
            }
            if (Tilemap == null) return;

            ComputePopupPosition();
            ClearRegion(0, 0, POPUP_W, POPUP_H);
            DrawBgFill(0, 0, POPUP_W, BORDER_H);
            DrawFrame();
            _tilesVisible = true;

            DrawText(2, 1, "Choose how you begin", QudColorParser.BrightYellow);

            for (int i = 0; i < model.Options.Count && i < CONTENT_ROWS; i++)
            {
                bool selected = i == model.SelectedIndex;
                int rowY = CONTENT_Y + i;
                if (selected) DrawChar(1, rowY, '>', QudColorParser.White);
                string label = (i + 1) + " " + (model.Options[i].Name ?? model.Options[i].Id);
                if (label.Length > LIST_W - 4) label = label.Substring(0, LIST_W - 4);
                DrawText(3, rowY, label, selected ? QudColorParser.White : QudColorParser.Gray);
            }

            List<string> lines = model.CardLines(Factory);
            for (int i = 0; i < lines.Count && i < CONTENT_ROWS; i++)
            {
                Color color = QudColorParser.Gray;
                if (i == 0) color = QudColorParser.BrightYellow;
                else if (lines[i].StartsWith("\"")) color = QudColorParser.White;
                else if (lines[i].StartsWith("Weakness")) color = QudColorParser.BrightRed;
                else if (lines[i].StartsWith("HP ")) color = QudColorParser.BrightCyan;
                DrawText(CARD_X, CONTENT_Y + i, lines[i], color);
            }

            DrawText(0, BORDER_H, NativeGamepadInput.IsConnected
                ? "[D-pad] browse  [A] begin"
                : "[Up/Down] browse  [1-" + model.Options.Count + "] jump  [Enter] begin", QudColorParser.DarkGray);
        }

        private void RenderLedger(StartingBuildMenuModel model)
        {
            _ledgerView ??= LedgerModalView.Create(transform, "Starting Build Ledger");
            _ledgerGamepad = NativeGamepadInput.IsConnected;
            int optionCount = model.Options.Count;
            int lastStart = Mathf.Max(0, optionCount - LedgerVisibleRows);
            _ledgerFirstRow = Mathf.Clamp(
                Mathf.Clamp(_ledgerFirstRow, model.SelectedIndex - LedgerVisibleRows + 1, model.SelectedIndex),
                0, lastStart);
            int visibleCount = Mathf.Min(optionCount, LedgerVisibleRows);
            int lastRow = _ledgerFirstRow + visibleCount;
            string footer = _ledgerGamepad
                ? "D-pad Browse    ·    A Begin"
                : $"↑ / ↓ Browse    ·    1–{Mathf.Min(optionCount, 9)} Jump    ·    Enter Begin";

            _ledgerView.Begin(PopupCamera, LedgerWidth, LedgerHeight);
            _ledgerView.Card("Choose how you begin", footer);
            for (int index = _ledgerFirstRow; index < lastRow; index++)
            {
                var option = model.Options[index];
                string key = !_ledgerGamepad && index < 9 ? (index + 1).ToString() : null;
                _ledgerView.Row(LedgerOptionBounds(index - _ledgerFirstRow), option.Name ?? option.Id,
                    index == model.SelectedIndex, key);
            }
            if (optionCount > visibleCount)
                _ledgerView.Label(new Rect(34, 495, 224, 24), $"Builds {_ledgerFirstRow + 1}–{lastRow} of {optionCount}", LMuted, 16);

            _ledgerView.Frame(LedgerDetailBounds, LBorder, LBackground);
            RenderLedgerDetails(model);
            _ledgerView.Row(LedgerBeginBounds, "Begin", true, aside: _ledgerGamepad ? "A" : "Enter");
            _ledgerView.End();
        }

        private void RenderLedgerDetails(StartingBuildMenuModel model)
        {
            List<string> lines = model.CardLines(Factory);
            _ledgerView.Label(new Rect(320, 102, 614, 38), model.Selected.Name ?? model.Selected.Id, LGold, 29, bold: true);
            _ledgerView.Label(new Rect(320, 144, 614, 46), model.Selected.Tagline, LInk, 21, TextAnchor.UpperLeft, wrap: true);
            _ledgerView.Box(new Rect(320, 192, 614, 1), LBorder);

            // The model supplies the same stats and equipment names applied by the selected build.
            int statsLine = lines.IndexOf(string.Empty) + 1;
            _ledgerView.Label(new Rect(320, 207, 614, 28), lines[statsLine], LGreen, 21, bold: true);
            _ledgerView.Label(new Rect(320, 237, 614, 25), lines[statsLine + 1], LMuted, 18);

            float y = 274;
            for (int i = statsLine + 2; i < lines.Count; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                int separator = line.IndexOf(": ", System.StringComparison.Ordinal);
                string heading = separator >= 0 ? line.Substring(0, separator) : string.Empty;
                string body = separator >= 0 ? line.Substring(separator + 2) : line;
                int lineCount = 1;
                while (i + 1 < lines.Count && lines[i + 1].StartsWith("  ", System.StringComparison.Ordinal))
                {
                    body += " " + lines[++i].Trim();
                    lineCount++;
                }
                float height = Mathf.Max(32, lineCount * 22);
                _ledgerView.Label(new Rect(320, y, 106, height), heading, heading == "Weakness" ? LRed : LMuted,
                    18, TextAnchor.UpperLeft, bold: true);
                _ledgerView.Label(new Rect(430, y, 504, height), body, LInk, 19, TextAnchor.UpperLeft, wrap: true);
                y += height + 5;
            }
        }

        private static Rect LedgerOptionBounds(int row) => new Rect(26, 86 + row * 44, 252, 38);

        private void DrawFrame()
        {
            int w = POPUP_W, h = BORDER_H;
            DrawChar(0, 0, CP437TilesetGenerator.BoxTopLeft, QudColorParser.Gray);
            DrawChar(w - 1, 0, CP437TilesetGenerator.BoxTopRight, QudColorParser.Gray);
            DrawChar(0, h - 1, CP437TilesetGenerator.BoxBottomLeft, QudColorParser.Gray);
            DrawChar(w - 1, h - 1, CP437TilesetGenerator.BoxBottomRight, QudColorParser.Gray);
            for (int x = 1; x < w - 1; x++)
            {
                DrawChar(x, 0, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);
                DrawChar(x, 2, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);
                DrawChar(x, h - 1, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);
            }
            DrawChar(0, 2, CP437TilesetGenerator.BoxTeeLeft, QudColorParser.Gray);
            DrawChar(w - 1, 2, CP437TilesetGenerator.BoxTeeRight, QudColorParser.Gray);
            for (int y = 1; y < h - 1; y++)
            {
                if (y == 2) continue;
                DrawChar(0, y, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
                DrawChar(w - 1, y, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
            }
            // vertical divider between the list and the card
            for (int y = CONTENT_Y; y < h - 1; y++)
                DrawChar(LIST_W, y, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
        }

        private void ClearAll()
        {
            _ledgerView?.Hide();
            _ledgerFirstRow = 0;
            _hasLedgerPointer = false;
            if (!_tilesVisible || Tilemap == null) return;
            ClearRegion(0, 0, POPUP_W, POPUP_H);
            ClearBgRegion(0, 0, POPUP_W, BORDER_H);
            _tilesVisible = false;
        }

        private void OnDisable()
        {
            ClearAll();
            _wasOpenLastFrame = false;
        }

        private void ComputePopupPosition()
        {
            _worldOriginX = CenteredPopupLayout.GetCenteredOriginX(POPUP_W);
            _worldTopY = CenteredPopupLayout.GetCenteredTopY(POPUP_H);
        }

        private int ReadClickedRow()
        {
            if (!UseLedgerView)
            {
                int hoveredTileRow = GetRowAtMouse();
                Controller.HoverSelect(hoveredTileRow);
                return Input.GetMouseButtonDown(0) ? hoveredTileRow : -1;
            }

            bool clicked = LedgerModalView.ReadPointer(out Vector2 pointer);
            bool moved = _hasLedgerPointer && pointer != _lastLedgerPointer;
            _lastLedgerPointer = pointer;
            _hasLedgerPointer = true;
            if (!clicked && !moved) return -1;

            Vector2 point = default;
            bool inCard = _ledgerView != null && _ledgerView.TryPoint(pointer, out point);
            int hoveredRow = inCard ? GetLedgerRow(point) : -1;
            if (moved) Controller.HoverSelect(hoveredRow);
            if (!clicked || !inCard) return -1;
            if (hoveredRow >= 0) return hoveredRow;
            return LedgerBeginBounds.Contains(point) ? Controller.SelectedIndex : -1;
        }

        private int GetLedgerRow(Vector2 point)
        {
            for (int visible = 0; visible < LedgerVisibleRows && visible + _ledgerFirstRow < Controller.Model.Options.Count; visible++)
                if (LedgerOptionBounds(visible).Contains(point)) return visible + _ledgerFirstRow;
            return -1;
        }

        private int GetRowAtMouse()
        {
            if (PopupCamera == null || Tilemap == null || Controller?.Model == null) return -1;
            // The popup origin is otherwise only computed by Render(); a hover or click
            // on the very first frame (before the first Render) must not hit-test
            // against a zero origin.
            ComputePopupPosition();
            if (!CenteredPopupLayout.ScreenToGrid(PopupCamera, Tilemap, UnityEngine.Input.mousePosition, out int gridX, out int gridY))
                return -1;

            int gx = gridX - _worldOriginX;
            int gy = _worldTopY - gridY;
            int row = gy - CONTENT_Y;
            if (gx > 0 && gx < LIST_W && row >= 0 && row < Controller.Model.Options.Count) return row;
            return -1;
        }

        // ---- Tilemap primitives (idiom shared with PauseMenuUI) ----

        private void ClearRegion(int gx, int gy, int width, int height)
        {
            for (int dy = 0; dy < height; dy++)
                for (int dx = 0; dx < width; dx++)
                    Tilemap.SetTile(new Vector3Int(_worldOriginX + gx + dx, _worldTopY - (gy + dy), 0), null);
        }

        private void DrawBgFill(int gx, int gy, int width, int height)
        {
            if (BgTilemap == null) return;
            var block = CP437TilesetGenerator.GetUiTile(CP437TilesetGenerator.SolidBlock);
            if (block == null) return;
            for (int dy = 0; dy < height; dy++)
                for (int dx = 0; dx < width; dx++)
                {
                    var pos = new Vector3Int(_worldOriginX + gx + dx, _worldTopY - (gy + dy), 0);
                    BgTilemap.SetTile(pos, block);
                    BgTilemap.SetTileFlags(pos, TileFlags.None);
                    BgTilemap.SetColor(pos, PopupBgColor);
                }
        }

        private void ClearBgRegion(int gx, int gy, int width, int height)
        {
            if (BgTilemap == null) return;
            for (int dy = 0; dy < height; dy++)
                for (int dx = 0; dx < width; dx++)
                    BgTilemap.SetTile(new Vector3Int(_worldOriginX + gx + dx, _worldTopY - (gy + dy), 0), null);
        }

        private void DrawChar(int gx, int gy, char c, Color color)
        {
            if (Tilemap == null) return;
            var pos = new Vector3Int(_worldOriginX + gx, _worldTopY - gy, 0);
            var tile = CP437TilesetGenerator.GetUiTile(c);
            if (tile == null) return;
            Tilemap.SetTile(pos, tile);
            Tilemap.SetTileFlags(pos, TileFlags.None);
            Tilemap.SetColor(pos, color);
        }

        private void DrawText(int gx, int gy, string text, Color color)
        {
            if (text == null || Tilemap == null) return;
            for (int i = 0; i < text.Length; i++)
                if (text[i] != ' ') DrawChar(gx + i, gy, text[i], color);
        }
    }
}
