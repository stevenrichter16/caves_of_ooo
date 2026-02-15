using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Full-screen faction standings UI. Shows player reputation with all visible factions.
    /// Follows the 80x45 tilemap grid pattern used by InventoryUI and TradeUI.
    /// Opened with the F key.
    /// </summary>
    public class FactionUI : MonoBehaviour
    {
        public Tilemap Tilemap;
        public Entity PlayerEntity;

        private const int W = 80;
        private const int H = 45;

        private bool _isOpen;
        public bool IsOpen => _isOpen;

        private int _cursor;
        private List<FactionRow> _rows = new List<FactionRow>();

        private struct FactionRow
        {
            public string Name;
            public string DisplayName;
            public int Reputation;
            public PlayerReputation.Attitude Attitude;
        }

        public void Open()
        {
            _isOpen = true;
            _cursor = 0;
            Rebuild();
            Render();
        }

        public void Close()
        {
            _isOpen = false;
        }

        public void HandleInput()
        {
            if (!_isOpen) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.K))
            {
                if (_cursor > 0)
                {
                    _cursor--;
                    Render();
                }
                return;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.J))
            {
                if (_cursor < _rows.Count - 1)
                {
                    _cursor++;
                    Render();
                }
                return;
            }
        }

        private void Rebuild()
        {
            _rows.Clear();

            var factions = FactionManager.GetAllVisibleFactions();
            for (int i = 0; i < factions.Count; i++)
            {
                string name = factions[i];
                _rows.Add(new FactionRow
                {
                    Name = name,
                    DisplayName = FactionManager.GetDisplayName(name),
                    Reputation = PlayerReputation.Get(name),
                    Attitude = PlayerReputation.GetAttitude(name)
                });
            }

            if (_cursor >= _rows.Count)
                _cursor = _rows.Count > 0 ? _rows.Count - 1 : 0;
        }

        // ===== Rendering =====

        private void Render()
        {
            if (Tilemap == null) return;
            Tilemap.ClearAllTiles();

            // Title
            DrawText(1, 0, "Faction Standings", QudColorParser.BrightYellow);

            // Separator
            DrawHLine(0, 1, W);

            // Column headers
            DrawText(4, 2, "Faction", QudColorParser.DarkGray);
            DrawText(38, 2, "Rep", QudColorParser.DarkGray);
            DrawText(44, 2, "Standing", QudColorParser.DarkGray);
            DrawText(56, 2, "Status", QudColorParser.DarkGray);

            // Faction rows
            for (int i = 0; i < _rows.Count; i++)
            {
                int y = 4 + i * 2; // Space rows out for readability
                if (y >= H - 3) break;

                var row = _rows[i];
                Color color = GetAttitudeColor(row.Attitude);
                bool selected = i == _cursor;

                // Cursor
                if (selected)
                    DrawChar(1, y, '>', QudColorParser.White);

                // Display name (padded to 30 chars)
                string displayName = row.DisplayName;
                if (displayName.Length > 30)
                    displayName = displayName.Substring(0, 30);
                DrawText(4, y, displayName, selected ? QudColorParser.White : color);

                // Reputation value
                string repStr = row.Reputation.ToString();
                // Right-align the rep value in a 5-char field at column 36
                int repX = 41 - repStr.Length;
                DrawText(repX, y, repStr, color);

                // Reputation bar: [==========] 10 chars
                DrawChar(44, y, '[', QudColorParser.DarkGray);
                DrawBar(45, y, 10, row.Reputation, color);
                DrawChar(55, y, ']', QudColorParser.DarkGray);

                // Attitude label
                string label = PlayerReputation.GetAttitudeLabel(row.Attitude);
                DrawText(57, y, label, color);
            }

            // Bottom separator
            DrawHLine(0, H - 3, W);

            // Action bar
            DrawText(1, H - 2, "[Esc] close", QudColorParser.DarkGray);
            DrawText(1, H - 1, "Reputation range: -200 to +200", QudColorParser.DarkGray);
        }

        /// <summary>
        /// Draw a 10-char reputation bar. Maps [-200, 200] to [0, 10] filled chars.
        /// </summary>
        private void DrawBar(int x, int y, int width, int rep, Color fillColor)
        {
            // Map rep from [-200, 200] to [0, width]
            float normalized = (rep + 200f) / 400f; // 0.0 to 1.0
            int filled = (int)(normalized * width + 0.5f);
            if (filled < 0) filled = 0;
            if (filled > width) filled = width;

            for (int i = 0; i < width; i++)
            {
                if (i < filled)
                    DrawChar(x + i, y, '=', fillColor);
                else
                    DrawChar(x + i, y, '-', QudColorParser.DarkGray);
            }
        }

        private Color GetAttitudeColor(PlayerReputation.Attitude attitude)
        {
            switch (attitude)
            {
                case PlayerReputation.Attitude.Hated: return QudColorParser.BrightRed;
                case PlayerReputation.Attitude.Disliked: return QudColorParser.DarkRed;
                case PlayerReputation.Attitude.Liked: return QudColorParser.DarkGreen;
                case PlayerReputation.Attitude.Loved: return QudColorParser.BrightGreen;
                default: return QudColorParser.Gray;
            }
        }

        // ===== Drawing Helpers =====

        private void DrawText(int x, int y, string text, Color color)
        {
            if (text == null || Tilemap == null) return;
            for (int i = 0; i < text.Length && x + i < W; i++)
            {
                char c = text[i];
                if (c == ' ') continue;
                var tilePos = new Vector3Int(x + i, H - 1 - y, 0);
                var tile = CP437TilesetGenerator.GetTile(c);
                if (tile == null) continue;
                Tilemap.SetTile(tilePos, tile);
                Tilemap.SetTileFlags(tilePos, TileFlags.None);
                Tilemap.SetColor(tilePos, color);
            }
        }

        private void DrawChar(int x, int y, char c, Color color)
        {
            if (Tilemap == null || x < 0 || x >= W || y < 0 || y >= H) return;
            var tilePos = new Vector3Int(x, H - 1 - y, 0);
            var tile = CP437TilesetGenerator.GetTile(c);
            if (tile == null) return;
            Tilemap.SetTile(tilePos, tile);
            Tilemap.SetTileFlags(tilePos, TileFlags.None);
            Tilemap.SetColor(tilePos, color);
        }

        private void DrawHLine(int x, int y, int width)
        {
            var tile = CP437TilesetGenerator.GetTile('-');
            if (tile == null) return;
            for (int i = x; i < x + width && i < W; i++)
            {
                var tilePos = new Vector3Int(i, H - 1 - y, 0);
                Tilemap.SetTile(tilePos, tile);
                Tilemap.SetTileFlags(tilePos, TileFlags.None);
                Tilemap.SetColor(tilePos, QudColorParser.DarkGray);
            }
        }
    }
}
