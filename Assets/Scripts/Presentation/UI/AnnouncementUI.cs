using System.Collections.Generic;
using CavesOfOoo.Data;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Renders a centered modal popup for important game announcements.
    /// Uses the centered popup overlay grid inside the gameplay viewport.
    /// Player must press Enter/Space/Escape or click to dismiss.
    /// </summary>
    public class AnnouncementUI : MonoBehaviour
    {
        public Tilemap Tilemap;
        public Tilemap BgTilemap;
        public Camera PopupCamera;

        private const int POPUP_W = 56;
        private const int MAX_TEXT_WIDTH = POPUP_W - 4; // 2 chars padding each side
        private static readonly Color PopupBgColor = new Color(0f, 0f, 0f, 1f);

        private bool _isOpen;
        private string _message;
        private readonly List<string> _wrappedLines = new List<string>();

        private int _worldOriginX;
        private int _worldTopY;
        private int _popupH;
        private bool _bgDrawn;
        private int _bgDrawnW;
        private int _bgDrawnH;
        private int _bgDrawnOriginX;
        private int _bgDrawnTopY;

        // Previously-drawn FG region, tracked so Render() can erase the
        // actual tiles from the last draw (even if position/size changed)
        // before painting the new popup. Without this, chaining Open() for
        // differently-sized messages left top-border / action-bar fragments
        // visible outside the new rectangle — 4 grimoires in a row produced
        // 4 ghost top-bars stacked above the active popup.
        private bool _fgDrawn;
        private int _fgDrawnW;
        private int _fgDrawnH;
        private int _fgDrawnOriginX;
        private int _fgDrawnTopY;

        public bool IsOpen => _isOpen;

        public void Open(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            _message = message;
            _isOpen = true;
            _wrappedLines.Clear();
            WrapText(message, MAX_TEXT_WIDTH, _wrappedLines);
            Render();
        }

        public void Close()
        {
            if (!_isOpen) return;
            ClearFgRegion();
            ClearBgRegion();
            _isOpen = false;
        }

        public void HandleInput()
        {
            if (!_isOpen) return;

            if (InputHelper.GetKeyDown(KeyCode.Return) || InputHelper.GetKeyDown(KeyCode.Space) || InputHelper.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                Close();
                return;
            }
        }

        // ===== Rendering =====

        private void ComputePopupPosition()
        {
            int textLines = _wrappedLines.Count;

            // Layout: top border + blank + text lines + blank + action bar + bottom border
            _popupH = 2 + textLines + 2 + 1;
            _worldOriginX = CenteredPopupLayout.GetCenteredOriginX(POPUP_W);
            _worldTopY = CenteredPopupLayout.GetCenteredTopY(_popupH);
        }

        private void Render()
        {
            if (Tilemap == null) return;

            // Erase whatever was drawn by the previous Open() call FIRST —
            // the new popup's rectangle may not fully overlap the old one's
            // (different text wraps to different heights, shifting the
            // centered origin), so tiles outside the new rectangle would
            // otherwise be left behind as ghost artifacts.
            ClearFgRegion();
            ClearBgRegion();
            ComputePopupPosition();

            int textLines = _wrappedLines.Count;

            DrawBgFill(0, 0, POPUP_W, _popupH - 1);

            // Top border
            DrawChar(0, 0, CP437TilesetGenerator.BoxTopLeft, QudColorParser.Gray);
            for (int i = 1; i < POPUP_W - 1; i++)
                DrawChar(i, 0, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);
            DrawChar(POPUP_W - 1, 0, CP437TilesetGenerator.BoxTopRight, QudColorParser.Gray);

            // Message text lines
            int y = 1;
            for (int i = 0; i < textLines; i++)
            {
                DrawChar(0, y, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
                DrawChar(POPUP_W - 1, y, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
                DrawText(2, y, _wrappedLines[i], QudColorParser.BrightYellow);
                y++;
            }

            // Blank line before action bar
            DrawChar(0, y, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
            DrawChar(POPUP_W - 1, y, CP437TilesetGenerator.BoxVertical, QudColorParser.Gray);
            y++;

            // Bottom border
            DrawChar(0, y, CP437TilesetGenerator.BoxBottomLeft, QudColorParser.Gray);
            for (int i = 1; i < POPUP_W - 1; i++)
                DrawChar(i, y, CP437TilesetGenerator.BoxHorizontal, QudColorParser.Gray);
            DrawChar(POPUP_W - 1, y, CP437TilesetGenerator.BoxBottomRight, QudColorParser.Gray);
            y++;

            // Action bar
            string actions = " [Enter] okay";
            DrawText(0, y, actions, QudColorParser.DarkGray);

            // Record the full FG footprint we just painted (box rows +
            // action bar row below) so the NEXT Render/Close can erase it
            // even if _worldOriginX/_worldTopY/_popupH change in between.
            _fgDrawn = true;
            _fgDrawnW = POPUP_W;
            _fgDrawnH = _popupH + 1;
            _fgDrawnOriginX = _worldOriginX;
            _fgDrawnTopY = _worldTopY;
        }

        // ===== Word Wrap =====

        private static void WrapText(string text, int maxWidth, List<string> lines)
        {
            if (string.IsNullOrEmpty(text))
            {
                lines.Add("");
                return;
            }

            string[] paragraphs = text.Split('\n');
            for (int p = 0; p < paragraphs.Length; p++)
            {
                string para = paragraphs[p];
                if (para.Length <= maxWidth)
                {
                    lines.Add(para);
                    continue;
                }

                int pos = 0;
                while (pos < para.Length)
                {
                    int remaining = para.Length - pos;
                    if (remaining <= maxWidth)
                    {
                        lines.Add(para.Substring(pos));
                        break;
                    }

                    int breakAt = para.LastIndexOf(' ', pos + maxWidth - 1, maxWidth);
                    if (breakAt <= pos)
                        breakAt = pos + maxWidth;

                    lines.Add(para.Substring(pos, breakAt - pos));
                    pos = breakAt;
                    if (pos < para.Length && para[pos] == ' ')
                        pos++;
                }
            }
        }

        // ===== Drawing Helpers (world-space, same as DialogueUI) =====

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

        /// <summary>
        /// Erase the FG tiles painted by the previous Render() call, using
        /// the origin/size we recorded at that time (not the current ones —
        /// those may already have been reassigned for the NEW popup).
        /// </summary>
        private void ClearFgRegion()
        {
            if (!_fgDrawn || Tilemap == null) return;

            for (int dy = 0; dy < _fgDrawnH; dy++)
            {
                for (int dx = 0; dx < _fgDrawnW; dx++)
                {
                    int wx = _fgDrawnOriginX + dx;
                    int wy = _fgDrawnTopY - dy;
                    Tilemap.SetTile(new Vector3Int(wx, wy, 0), null);
                }
            }

            _fgDrawn = false;
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
