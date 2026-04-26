using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Phase 4d — visual rendering for the pause menu modal. Pattern
    /// mirrors <see cref="WorldActionMenuUI"/>: foreground-glyph
    /// tilemap + background-fill tilemap + CP437 box characters,
    /// centered via <see cref="CenteredPopupLayout"/>.
    ///
    /// <para><b>Roles split.</b> The pure dispatch logic (keyboard
    /// navigation, save/load dispatch, lifecycle) lives in
    /// <see cref="PauseMenuController"/> and is unit-tested. This
    /// MonoBehaviour does ONLY rendering + mouse hit-testing,
    /// delegating actual decisions to the controller.</para>
    ///
    /// <para><b>Lifecycle.</b> Always present on the GameBootstrap
    /// GameObject. Tilemap refs wired by GameBootstrap. HandleInput
    /// is called every frame from <see cref="InputHandler"/>; only
    /// renders when controller's <c>IsOpen</c> is true.</para>
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        public Tilemap Tilemap;
        public Tilemap BgTilemap;
        public Camera PopupCamera;

        // ---- Layout constants ----
        private const int POPUP_W = 28;
        private const int CONTENT_ROWS = 2;          // Save, Load
        private const int BORDER_H = CONTENT_ROWS + 4;  // top + title + sep + content + bottom
        private const int POPUP_H = BORDER_H + 1;       // +1 for hint line
        private static readonly Color PopupBgColor = new Color(0f, 0f, 0f, 1f);

        private static readonly string[] LABELS = { "Save game", "Load game" };

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

        public bool IsOpen => Controller != null && Controller.IsOpen;

        /// <summary>
        /// Per-frame entry point. Polls keyboard via the controller,
        /// processes mouse hover/click for the active menu, and
        /// re-renders when state changes. Returns true if input was
        /// consumed (host should short-circuit subsequent input).
        /// </summary>
        public bool HandleInput(IInputProbe input)
        {
            // [PauseMenuDiag] Diagnostic for "Tab doesn't open menu" bug.
            // Fires once per Tab press in the UI layer (after InputHandler routed here).
            // Remove this block once the issue is diagnosed.
            bool tabDiag = input != null
                && input.GetKeyDown(Controller != null ? Controller.OpenCloseKey : KeyCode.Tab);
            if (tabDiag)
            {
                Debug.Log($"[PauseMenuDiag/UI.HandleInput] " +
                    $"Controller={(Controller != null ? "set" : "NULL")} | " +
                    $"SaveLoadService={(SaveLoadService != null ? "set" : "NULL")} | " +
                    $"Log={(Log != null ? "set" : "NULL")} | " +
                    $"Tilemap={(Tilemap != null ? "set" : "NULL")} | " +
                    $"BgTilemap={(BgTilemap != null ? "set" : "NULL")} | " +
                    $"PopupCamera={(PopupCamera != null ? "set" : "NULL")} | " +
                    $"PreTick.IsOpen={(Controller?.IsOpen.ToString() ?? "n/a")}");
            }

            if (Controller == null) return false;

            int prevSelected = Controller.SelectedIndex;
            bool wasOpen = Controller.IsOpen;

            // Mouse hover (only meaningful while open).
            if (Controller.IsOpen)
            {
                int hoverRow = GetRowAtMouse();
                if (hoverRow >= 0 && hoverRow != Controller.SelectedIndex)
                    Controller.HoverSelect(hoverRow);

                // Mouse click → confirm at clicked row, or close on outside-click.
                if (UnityEngine.Input.GetMouseButtonDown(0))
                {
                    int clickedRow = GetRowAtMouse();
                    if (clickedRow >= 0 && SaveLoadService != null)
                    {
                        Controller.ClickSelect(clickedRow, SaveLoadService, Log);
                        OnControllerStateChanged(wasOpen);
                        return true;
                    }
                    if (clickedRow < 0)
                    {
                        // Click outside the menu area dismisses (matches WorldActionMenuUI).
                        Controller.Close();
                        OnControllerStateChanged(wasOpen);
                        return true;
                    }
                }
            }

            bool consumed = Controller.Tick(input, SaveLoadService, Log);

            // [PauseMenuDiag] Post-Tick diagnostic: did the controller open?
            if (tabDiag)
            {
                Debug.Log($"[PauseMenuDiag/UI.HandleInput.PostTick] " +
                    $"consumed={consumed} | " +
                    $"PostTick.IsOpen={Controller.IsOpen} | " +
                    $"OpenChanged={Controller.IsOpen != wasOpen} | " +
                    $"SelectedIndex={Controller.SelectedIndex}");
            }

            // Render when state changed or selection changed (either due to
            // keyboard nav or mouse hover handled above).
            if (Controller.IsOpen != wasOpen
                || (Controller.IsOpen && Controller.SelectedIndex != prevSelected))
            {
                OnControllerStateChanged(wasOpen);
            }
            else if (Controller.IsOpen && !_wasOpenLastFrame)
            {
                // First frame open after Open() was called externally.
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
            // [PauseMenuDiag] Render call diagnostic.
            Debug.Log($"[PauseMenuDiag/UI.Render] " +
                $"Tilemap={(Tilemap != null ? "set" : "NULL")} | " +
                $"BgTilemap={(BgTilemap != null ? "set" : "NULL")} | " +
                $"Controller={(Controller != null ? "set" : "NULL")} | " +
                $"IsOpen={(Controller?.IsOpen.ToString() ?? "n/a")} | " +
                $"SelectedIndex={(Controller?.SelectedIndex.ToString() ?? "n/a")}");

            if (Tilemap == null || Controller == null) return;

            ComputePopupPosition();
            ClearRegion(0, 0, POPUP_W, POPUP_H);
            DrawBgFill(0, 0, POPUP_W, BORDER_H);
            DrawPopupBorder(0, 0, POPUP_W, BORDER_H, CONTENT_ROWS);

            // Title row
            DrawText(2, 1, "Pause", QudColorParser.BrightYellow);

            // Two button rows
            const int contentY = 3;
            for (int i = 0; i < LABELS.Length; i++)
            {
                int rowY = contentY + i;
                bool selected = i == Controller.SelectedIndex;

                if (selected)
                    DrawChar(1, rowY, '>', QudColorParser.White);

                Color labelColor = selected ? QudColorParser.White : QudColorParser.Gray;
                DrawText(3, rowY, LABELS[i], labelColor);
            }

            // Hint at bottom (outside the border)
            int hintY = BORDER_H;
            DrawText(0, hintY, "[Enter]select [Tab]close", QudColorParser.DarkGray);
        }

        private void ClearAll()
        {
            ClearRegion(0, 0, POPUP_W, POPUP_H);
            ClearBgRegion(0, 0, POPUP_W, BORDER_H);
        }

        private void ComputePopupPosition()
        {
            _worldOriginX = CenteredPopupLayout.GetCenteredOriginX(POPUP_W);
            _worldTopY = CenteredPopupLayout.GetCenteredTopY(POPUP_H);
        }

        private int GetRowAtMouse()
        {
            if (PopupCamera == null || Tilemap == null) return -1;
            if (!CenteredPopupLayout.ScreenToGrid(PopupCamera, Tilemap,
                    UnityEngine.Input.mousePosition, out int gridX, out int gridY))
                return -1;

            int gx = gridX - _worldOriginX;
            int gy = _worldTopY - gridY;

            const int contentY = 3;
            if (gx > 0 && gx < POPUP_W - 1 && gy >= contentY && gy < contentY + CONTENT_ROWS)
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
