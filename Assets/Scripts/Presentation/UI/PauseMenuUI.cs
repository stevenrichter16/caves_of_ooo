using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using static CavesOfOoo.Rendering.LedgerPalette;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Draws the pause menu and forwards input to <see cref="PauseMenuController"/>.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        public Tilemap Tilemap;
        public Tilemap BgTilemap;
        public Camera PopupCamera;
        internal bool UseLedgerView;

        private const int POPUP_W = 28;
        private static readonly Color PopupBgColor = new Color(0f, 0f, 0f, 1f);
        private const int LedgerWidth = 596;
        private const int LedgerInset = 26;
        private const int LedgerFirstRow = 87;
        private const int LedgerRowHeight = 34;
        private const int LedgerRowStride = 38;
        private LedgerModalView _ledgerView;
        private bool _ledgerGamepad;

        // The controller is owned externally (InputHandler creates one).
        // We accept a reference so this UI is testable independently
        // (don't construct one here).
        public PauseMenuController Controller { get; set; }

        public ISaveLoadService SaveLoadService { get; set; }
        public System.Action<string> Log { get; set; }

        // Track whether we currently have something rendered so Close
        // can clear the same region we drew (idiom from WorldActionMenuUI).
        private bool _wasOpenLastFrame;
        private int _worldOriginX;
        private int _worldTopY;
        private int _renderedHeight;
        private bool _hasRenderedBounds;
        private bool _hasMousePosition;
        private Vector2 _lastMousePosition;

        public bool IsOpen => Controller != null && Controller.IsOpen;

        /// <summary>
        /// Per-frame entry point. Polls keyboard via the controller,
        /// processes mouse hover/click for the active menu, and
        /// re-renders when state changes. Returns true if input was
        /// consumed (host should short-circuit subsequent input).
        /// </summary>
        public bool HandleInput(IInputProbe input)
        {
            if (Controller == null) return false;

            int prevSelected = Controller.SelectedIndex;
            bool wasOpen = Controller.IsOpen;
            int previousRevision = Controller.ViewRevision;
            Vector2 mousePosition = default;
            bool clicked = UseLedgerView ? LedgerModalView.ReadPointer(out mousePosition) : Input.GetMouseButtonDown(0);
            if (!UseLedgerView) mousePosition = Input.mousePosition;
            bool mouseMoved = _hasMousePosition && mousePosition != _lastMousePosition;
            _lastMousePosition = mousePosition;
            _hasMousePosition = true;

            // Mouse hover (only meaningful while open).
            if (Controller.IsOpen)
            {
                Vector2 point = default;
                bool inCard = UseLedgerView && (mouseMoved || clicked)
                    && _ledgerView != null && _ledgerView.TryPoint(mousePosition, out point);
                int hoverRow = UseLedgerView ? (inCard ? GetLedgerRow(point) : -1) : GetRowAtMouse(mousePosition);
                // A stationary cursor must not undo a Dpad/keyboard selection.
                if (mouseMoved && hoverRow >= 0 && hoverRow != Controller.SelectedIndex)
                    Controller.HoverSelect(hoverRow);

                if (clicked)
                {
                    if (hoverRow >= 0)
                        Controller.ClickSelect(hoverRow, SaveLoadService, Log);
                    else if (!inCard)
                        Controller.Close();
                    else return true;
                    OnControllerStateChanged(wasOpen);
                    return true;
                }
            }

            bool consumed = Controller.Tick(input, SaveLoadService, Log);

            // Render when state changed or selection changed (either due to
            // keyboard nav or mouse hover handled above).
            if (Controller.IsOpen != wasOpen
                || (Controller.IsOpen && (Controller.SelectedIndex != prevSelected || Controller.ViewRevision != previousRevision)))
            {
                OnControllerStateChanged(wasOpen);
            }
            else if (Controller.IsOpen && (!_wasOpenLastFrame
                || (UseLedgerView && _ledgerGamepad != NativeGamepadInput.IsConnected)))
            {
                Render();
            }

            _wasOpenLastFrame = Controller.IsOpen;
            return consumed;
        }

        private void OnControllerStateChanged(bool wasOpenBefore)
        {
            if (Controller.IsOpen)
                Render();
            else if (wasOpenBefore)
                ClearAll();
            _wasOpenLastFrame = Controller.IsOpen;
        }

        // ---- Rendering ----
        //
        // Layout:
        //
        //   ┌──────────────────────────┐
        //   │ Pause                    │
        //   ├──────────────────────────┤
        //   │ > Save game              │   ← selected: '>' + white
        //   │   Load game              │
        //   └──────────────────────────┘
        //   [Enter]select [Tab]close

        private void Render()
        {
            if (Controller == null) return;
            if (UseLedgerView)
            {
                RenderLedger();
                return;
            }
            if (Tilemap == null) return;

            // Erase the previous rectangle before changing page height/origin.
            // Otherwise returning from the taller graphics page leaves stale rows.
            ClearAll();
            int contentRows = Controller.VisibleItemCount;
            int borderHeight = contentRows + 4;
            int popupHeight = borderHeight + 1;
            ComputePopupPosition(popupHeight);
            _renderedHeight = popupHeight;
            _hasRenderedBounds = true;
            ClearRegion(0, 0, POPUP_W, popupHeight);
            DrawBgFill(0, 0, POPUP_W, popupHeight);
            DrawPopupBorder(0, 0, POPUP_W, borderHeight, contentRows);

            // Title row
            DrawText(2, 1, Controller.IsGraphicsOpen ? "Graphics" : Controller.IsAudioOpen ? "Audio" : "Pause", QudColorParser.BrightYellow);

            // Current page rows
            const int contentY = 3;
            for (int i = 0; i < contentRows; i++)
            {
                int rowY = contentY + i;
                bool selected = i == Controller.SelectedIndex;

                if (selected)
                    DrawChar(1, rowY, '>', QudColorParser.White);

                Color labelColor = selected ? QudColorParser.White : QudColorParser.Gray;
                DrawText(3, rowY, Controller.GetItemLabel(i), labelColor);
            }

            // Hint at bottom (outside the border)
            string hint = Controller.IsGraphicsOpen || Controller.IsAudioOpen
                ? (NativeGamepadInput.IsConnected ? "[A]change [B]back" : "[Enter]change [Esc]back")
                : (NativeGamepadInput.IsConnected ? "[A]select [B]close" : "[Enter]select [Tab]close");
            if(Controller.IsAudioOpen&&Controller.SelectedIndex<=PauseMenuController.SfxVolumeIndex)
                hint=NativeGamepadInput.IsConnected?"[Left/Right]volume [B]back":"[Left/Right]volume [Esc]back";
            DrawText(0, borderHeight, hint, QudColorParser.Gray);
        }

        private void RenderLedger()
        {
            _ledgerView ??= LedgerModalView.Create(transform, "Pause Ledger");
            int rows = Controller.VisibleItemCount;
            _ledgerGamepad = NativeGamepadInput.IsConnected;
            bool graphics = Controller.IsGraphicsOpen;
            bool audio = Controller.IsAudioOpen;
            string confirm = _ledgerGamepad ? "A" : "Enter";
            string back = _ledgerGamepad ? "B" : graphics || audio ? "Esc" : "Tab / Esc";
            string footer = graphics || audio
                ? $"← / → Adjust    ·    {confirm} Select    ·    {back} Back"
                : $"↑ / ↓ Browse    ·    {confirm} Select    ·    {back} Close";

            _ledgerView.Begin(PopupCamera, LedgerWidth, LedgerFirstRow + rows * LedgerRowStride + 60);
            _ledgerView.Card(graphics ? "Graphics" : audio ? "Audio" : "Pause", footer);
            for (int row = 0; row < rows; row++)
            {
                string label = Controller.GetItemLabel(row);
                int separator = graphics || audio ? label.IndexOf(": ", System.StringComparison.Ordinal) : -1;
                string value = null;
                if (separator >= 0)
                {
                    value = label.Substring(separator + 2);
                    label = label.Substring(0, separator);
                }
                _ledgerView.Row(LedgerRowBounds(row), label, row == Controller.SelectedIndex, aside: value);
            }
            _ledgerView.End();
        }

        private static Rect LedgerRowBounds(int row)
            => new Rect(LedgerInset, LedgerFirstRow + LedgerRowStride * row, LedgerWidth - 2 * LedgerInset, LedgerRowHeight);

        private void OnDisable()
        {
            ClearAll();
            _wasOpenLastFrame = false;
        }

        private void ClearAll()
        {
            _ledgerView?.Hide();
            if (!_hasRenderedBounds || Tilemap == null) return;
            ClearRegion(0, 0, POPUP_W, _renderedHeight);
            ClearBgRegion(0, 0, POPUP_W, _renderedHeight);
            _hasRenderedBounds = false;
        }

        private void ComputePopupPosition(int popupHeight)
        {
            _worldOriginX = CenteredPopupLayout.GetCenteredOriginX(POPUP_W);
            _worldTopY = CenteredPopupLayout.GetCenteredTopY(popupHeight);
        }

        private int GetLedgerRow(Vector2 point)
        {
            int row = Mathf.FloorToInt((point.y - LedgerFirstRow) / LedgerRowStride);
            return row >= 0 && row < Controller.VisibleItemCount && LedgerRowBounds(row).Contains(point) ? row : -1;
        }

        private int GetRowAtMouse(Vector2 mousePosition)
        {
            if (PopupCamera == null || Tilemap == null) return -1;
            if (!CenteredPopupLayout.ScreenToGrid(PopupCamera, Tilemap, mousePosition, out int gridX, out int gridY))
                return -1;

            int gx = gridX - _worldOriginX;
            int gy = _worldTopY - gridY;

            const int contentY = 3;
            if (gx > 0 && gx < POPUP_W - 1 && gy >= contentY && gy < contentY + Controller.VisibleItemCount)
                return gy - contentY;
            return -1;
        }

        // ---- Tilemap primitives (idiom-shared with WorldActionMenuUI) ----

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
        }

        private void ClearBgRegion(int gx, int gy, int width, int height)
        {
            if (BgTilemap == null) return;
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    int wx = _worldOriginX + gx + dx;
                    int wy = _worldTopY - (gy + dy);
                    BgTilemap.SetTile(new Vector3Int(wx, wy, 0), null);
                }
            }
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
            var tile = CP437TilesetGenerator.GetUiTile(c);
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
