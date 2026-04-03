using System.Collections.Generic;
using CavesOfOoo.Data;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Renders a centered modal popup for important game announcements.
    /// Follows the same world-space tilemap popup pattern as DialogueUI.
    /// Player must press Enter/Space/Escape or click to dismiss.
    /// </summary>
    public class AnnouncementUI : MonoBehaviour
    {
        public Tilemap Tilemap;

        private const int POPUP_W = 56;
        private const int MAX_TEXT_WIDTH = POPUP_W - 4; // 2 chars padding each side

        private bool _isOpen;
        private string _message;
        private readonly List<string> _wrappedLines = new List<string>();

        private int _worldOriginX;
        private int _worldTopY;
        private int _popupH;

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
            ClearRegion(0, 0, POPUP_W, _popupH);
            _isOpen = false;
        }

        public void HandleInput()
        {
            if (!_isOpen) return;

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
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
            var cam = Camera.main;
            if (cam == null) return;

            int textLines = _wrappedLines.Count;

            // Layout: top border + blank + text lines + blank + action bar + bottom border
            _popupH = 2 + textLines + 2 + 1;

            float camX = cam.transform.position.x;
            float camY = cam.transform.position.y;

            _worldOriginX = Mathf.RoundToInt(camX) - POPUP_W / 2;
            _worldTopY = Mathf.RoundToInt(camY) + _popupH / 2;
        }

        private void Render()
        {
            if (Tilemap == null) return;

            ComputePopupPosition();

            int textLines = _wrappedLines.Count;

            ClearRegion(0, 0, POPUP_W, _popupH);

            // Top border
            DrawChar(0, 0, '+', QudColorParser.Gray);
            for (int i = 1; i < POPUP_W - 1; i++)
                DrawChar(i, 0, '-', QudColorParser.Gray);
            DrawChar(POPUP_W - 1, 0, '+', QudColorParser.Gray);

            // Message text lines
            int y = 1;
            for (int i = 0; i < textLines; i++)
            {
                DrawChar(0, y, '|', QudColorParser.Gray);
                DrawChar(POPUP_W - 1, y, '|', QudColorParser.Gray);
                DrawText(2, y, _wrappedLines[i], QudColorParser.BrightYellow);
                y++;
            }

            // Blank line before action bar
            DrawChar(0, y, '|', QudColorParser.Gray);
            DrawChar(POPUP_W - 1, y, '|', QudColorParser.Gray);
            y++;

            // Bottom border
            DrawChar(0, y, '+', QudColorParser.Gray);
            for (int i = 1; i < POPUP_W - 1; i++)
                DrawChar(i, y, '-', QudColorParser.Gray);
            DrawChar(POPUP_W - 1, y, '+', QudColorParser.Gray);
            y++;

            // Action bar
            string actions = " [Enter] okay";
            DrawText(0, y, actions, QudColorParser.DarkGray);
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
