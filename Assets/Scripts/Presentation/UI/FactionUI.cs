using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using static CavesOfOoo.Rendering.LedgerPalette;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Faction standings opened with the F key. Shows player reputation and
    /// attitude for all visible factions.
    /// </summary>
    public class FactionUI : MonoBehaviour
    {
        public Tilemap Tilemap;
        public Entity PlayerEntity;
        internal bool UseLedgerView;
        internal Camera GameplayCamera;

        private const int W = 80;
        private const int H = 45;

        private bool _isOpen;
        private LedgerModalView _ledgerView;
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
            _ledgerView?.Hide();
            if (!UseLedgerView && Tilemap != null)
                Tilemap.ClearAllTiles();
        }

        private void OnDisable()
        {
            if (_isOpen) Close();
        }

        public void HandleInput()
        {
            if (!_isOpen) return;

            if (InputHelper.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }

            if (InputHelper.GetKeyDown(KeyCode.UpArrow) || InputHelper.GetKeyDown(KeyCode.K))
            {
                if (_cursor > 0)
                {
                    _cursor--;
                    Render();
                }
                return;
            }

            if (InputHelper.GetKeyDown(KeyCode.DownArrow) || InputHelper.GetKeyDown(KeyCode.J))
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
            if (UseLedgerView)
            {
                RenderLedger();
                return;
            }
            _ledgerView?.Hide();
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

        private void RenderLedger()
        {
            const int width = 848;
            const int inset = 24;
            const int contentWidth = width - inset * 2;
            const int headerY = 77;
            const int listY = 111;
            const int rowHeight = 42;
            const int maxRows = 12;
            const int factionX = 40;
            const int factionWidth = 316;
            const int reputationX = 374;
            const int reputationWidth = 62;
            const int standingX = 480;
            const int standingWidth = 176;
            const int attitudeX = 701;
            const int attitudeWidth = 105;

            int visibleRows = Mathf.Min(maxRows, _rows.Count);
            int first = Mathf.Clamp(_cursor - maxRows / 2, 0, _rows.Count - visibleRows);
            int summaryY = listY + Mathf.Max(2, visibleRows) * rowHeight + 7;
            int height = summaryY + 79;
            if (_ledgerView == null) _ledgerView = LedgerModalView.Create(transform, "FactionStandingsLedger");
            _ledgerView.Begin(GameplayCamera, width, height);
            _ledgerView.Card("Faction standings", "[↑/↓ or J/K] navigate   [Esc / B] close");
            _ledgerView.Label(new Rect(factionX, headerY, factionWidth, 25), "Faction", LMuted, 16, bold: true);
            _ledgerView.Label(new Rect(reputationX, headerY, reputationWidth, 25), "Rep", LMuted, 16,
                TextAnchor.MiddleRight, bold: true);
            _ledgerView.Label(new Rect(standingX, headerY, standingWidth, 25), "Standing", LMuted, 16, bold: true);
            _ledgerView.Label(new Rect(attitudeX, headerY, attitudeWidth, 25), "Status", LMuted, 16, bold: true);
            _ledgerView.Box(new Rect(inset, 104, contentWidth, 1), LBorder);

            if (visibleRows == 0)
                _ledgerView.Label(new Rect(factionX, listY, factionWidth, rowHeight), "No known factions.", LMuted, 18);
            for (int i = 0; i < visibleRows; i++)
            {
                int index = first + i;
                var row = _rows[index];
                bool selected = index == _cursor;
                int y = listY + i * rowHeight;
                Color color = row.Attitude switch
                {
                    PlayerReputation.Attitude.Hated => LRed,
                    PlayerReputation.Attitude.Disliked => Color.Lerp(LInk, LRed, .7f),
                    PlayerReputation.Attitude.Liked => Color.Lerp(LInk, LGreen, .7f),
                    PlayerReputation.Attitude.Loved => LGreen,
                    _ => LMuted
                };
                _ledgerView.Box(new Rect(inset, y, contentWidth, rowHeight - 3), selected ? LSelected : LPanel);
                if (selected) _ledgerView.Box(new Rect(inset, y, 3, rowHeight - 3), LGold);
                _ledgerView.Label(new Rect(factionX, y, factionWidth, rowHeight - 3), row.DisplayName, LInk, 19, bold: selected, fit: true);
                _ledgerView.Label(new Rect(reputationX, y, reputationWidth, rowHeight - 3), row.Reputation.ToString(), color, 18,
                    TextAnchor.MiddleRight);
                _ledgerView.Label(new Rect(attitudeX, y, attitudeWidth, rowHeight - 3),
                    PlayerReputation.GetAttitudeLabel(row.Attitude), color, 17);
                DrawLedgerBar(new Rect(standingX, y + 16, standingWidth, 7), row.Reputation, color);
            }

            if (_rows.Count > maxRows)
                _ledgerView.Label(new Rect(inset, summaryY, 252, 24), $"{first + 1}–{first + visibleRows} of {_rows.Count}", LMuted, 16);
            _ledgerView.Label(new Rect(width - inset - 310, summaryY, 310, 24),
                $"Reputation range: {PlayerReputation.MIN_REP} to +{PlayerReputation.MAX_REP}", LMuted, 16, TextAnchor.MiddleRight);
            _ledgerView.End();
        }

        private void DrawLedgerBar(Rect bounds, int reputation, Color tone)
        {
            _ledgerView.Frame(bounds, LBorder, LBackground);
            float filled = (bounds.width - 2) * Mathf.InverseLerp(PlayerReputation.MIN_REP, PlayerReputation.MAX_REP, reputation);
            if (filled > 0)
                _ledgerView.Box(new Rect(bounds.x + 1, bounds.y + 1, filled, bounds.height - 2), tone);
            _ledgerView.Box(new Rect(bounds.center.x, bounds.y - 2, 1, bounds.height + 4), LMuted);
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
                var tile = CP437TilesetGenerator.GetUiTile(c);
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
            var tile = CP437TilesetGenerator.GetUiTile(c);
            if (tile == null) return;
            Tilemap.SetTile(tilePos, tile);
            Tilemap.SetTileFlags(tilePos, TileFlags.None);
            Tilemap.SetColor(tilePos, color);
        }

        private void DrawHLine(int x, int y, int width)
        {
            var tile = CP437TilesetGenerator.GetUiTile(CP437TilesetGenerator.BoxHorizontal);
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
