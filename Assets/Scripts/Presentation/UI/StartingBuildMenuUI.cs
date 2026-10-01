using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Rendering and mouse hit-testing for the new-game build picker. Pattern
    /// mirrors <see cref="PauseMenuUI"/> (foreground glyph tilemap + background
    /// fill, CP437 box characters, centered via <see cref="CenteredPopupLayout"/>);
    /// all decisions live in <see cref="StartingBuildMenuController"/>.
    ///
    ///   +------------------------------------------------------------------------+
    ///   | Choose how you begin                                                   |
    ///   +----------------------+-------------------------------------------------+
    ///   | > 1 The Duelist      | The Duelist                                     |
    ///   |   2 The Breaker      | "Let them swing. Punish the miss."              |
    ///   |   3 The Stormcaller  |                                                 |
    ///   |   4 The Bombardier   | HP 40   DV 11   Speed 100                       |
    ///   |   5 Classic          | Str 16  Agi 22  Tou 14  Ego 18                  |
    ///   +----------------------+-------------------------------------------------+
    ///    [Up/Down] browse  [1-5] jump  [Enter] begin
    /// </summary>
    public class StartingBuildMenuUI : MonoBehaviour
    {
        public Tilemap Tilemap;
        public Tilemap BgTilemap;
        public Camera PopupCamera;

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

        private bool _wasOpenLastFrame;
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
                int hover = GetRowAtMouse();
                if (hover >= 0 && hover != Controller.SelectedIndex) Controller.HoverSelect(hover);

                if (UnityEngine.Input.GetMouseButtonDown(0))
                {
                    int clicked = GetRowAtMouse();
                    if (clicked >= 0)
                    {
                        Controller.ClickConfirm(clicked);
                        AfterChange(wasOpen);
                        return true;
                    }
                }
            }

            bool consumed = Controller.Tick(input);

            if (Controller.IsOpen != wasOpen || (Controller.IsOpen && Controller.SelectedIndex != prevSelected))
                AfterChange(wasOpen);
            else if (Controller.IsOpen && !_wasOpenLastFrame)
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
            if (Tilemap == null || Controller?.Model == null) return;

            ComputePopupPosition();
            ClearRegion(0, 0, POPUP_W, POPUP_H);
            DrawBgFill(0, 0, POPUP_W, BORDER_H);
            DrawFrame();

            DrawText(2, 1, "Choose how you begin", QudColorParser.BrightYellow);

            var model = Controller.Model;
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

            DrawText(0, BORDER_H, "[Up/Down] browse  [1-" + model.Options.Count + "] jump  [Enter] begin", QudColorParser.DarkGray);
        }

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
