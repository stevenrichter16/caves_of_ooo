using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Renders the persistent gameplay sidebar using the narrow text atlas.
    /// </summary>
    public sealed class GameplaySidebarRenderer
    {
        private static readonly Color SidebarBgColor = new Color(0.04f, 0.04f, 0.08f, 0.95f);

        private readonly Tilemap _tilemap;
        private readonly Tilemap _backgroundTilemap;
        private readonly Transform _gridTransform;
        private readonly float _referenceZoom;

        private readonly List<SidebarTextFormatter.LogLine> _cachedLogLines = new List<SidebarTextFormatter.LogLine>();
        private int _cachedLogWidth = -1;
        private int _cachedLogHeight = -1;
        private int _cachedLogCount = -1;
        private string _cachedNewestText = string.Empty;
        private string _cachedOldestText = string.Empty;

        public GameplaySidebarRenderer(Tilemap tilemap, Tilemap backgroundTilemap, Transform gridTransform, float referenceZoom)
        {
            _tilemap = tilemap;
            _backgroundTilemap = backgroundTilemap;
            _gridTransform = gridTransform;
            _referenceZoom = referenceZoom;
        }

        public bool IsVisible { get; private set; }

        public void Invalidate()
        {
            _cachedLogWidth = -1;
            _cachedLogHeight = -1;
            _cachedLogCount = -1;
            _cachedNewestText = string.Empty;
            _cachedOldestText = string.Empty;
            _cachedLogLines.Clear();
        }

        public void Clear()
        {
            IsVisible = false;
            _tilemap?.ClearAllTiles();
            _backgroundTilemap?.ClearAllTiles();
        }

        public void Render(SidebarSnapshot snapshot, Camera camera, int sidebarWidthChars, bool flashActive, float flashT)
        {
            Clear();

            if (_tilemap == null || _backgroundTilemap == null || _gridTransform == null || camera == null || sidebarWidthChars <= 0)
                return;

            GameplayViewportMetrics metrics = GameplayViewportLayout.Measure(camera, _referenceZoom, sidebarWidthChars);
            _gridTransform.localScale = new Vector3(metrics.Scale, metrics.Scale, 1f);

            int startX = metrics.SidebarStartCharX;
            int width = sidebarWidthChars;
            int rows = metrics.VisibleRowCount;
            int topY = metrics.TopTextY;
            int bottomY = metrics.BottomTextY;
            int contentX = startX + 2;
            int contentWidth = Mathf.Max(1, width - 3);

            DrawBackground(startX, bottomY, width, rows, flashActive, flashT);
            DrawDivider(startX, bottomY, rows);

            int y = topY;
            DrawSectionHeader(startX, contentX, y, contentWidth, "VITALS", QudColorParser.White);
            y--;

            List<string> vitals = SidebarTextFormatter.FormatVitals(snapshot, contentWidth, 7);
            for (int i = 0; i < vitals.Count && y >= bottomY; i++, y--)
            {
                Color color = vitals[i].StartsWith("ST ", System.StringComparison.Ordinal)
                    ? QudColorParser.Gray
                    : QudColorParser.White;
                DrawText(contentX, y, vitals[i], color, contentWidth);
            }

            if (y >= bottomY)
                y--;

            DrawSectionHeader(startX, contentX, y, contentWidth, "FOCUS", QudColorParser.White);
            y--;

            int remainingAfterFocusHeader = y - bottomY + 1;
            int focusMaxLines = Mathf.Clamp(remainingAfterFocusHeader - 7, 2, 6);
            List<string> focusLines = SidebarTextFormatter.FormatFocus(snapshot?.FocusSnapshot, contentWidth, focusMaxLines);
            for (int i = 0; i < focusLines.Count && y >= bottomY; i++, y--)
            {
                Color color = i == 0
                    ? QudColorParser.White
                    : (i == 1 ? QudColorParser.Gray : QudColorParser.DarkGray);
                DrawText(contentX, y, focusLines[i], color, contentWidth);
            }

            if (y >= bottomY)
                y--;

            Color logHeaderColor = flashActive ? QudColorParser.BrightYellow : QudColorParser.White;
            DrawSectionHeader(startX, contentX, y, contentWidth, "LOG", logHeaderColor);
            y--;

            int logHeight = Mathf.Max(1, y - bottomY + 1);
            List<SidebarTextFormatter.LogLine> logLines = GetLogLines(snapshot, contentWidth, logHeight);
            int firstLogY = bottomY + logHeight - logLines.Count;
            int maxAge = 0;
            for (int i = 0; i < logLines.Count; i++)
            {
                if (logLines[i].AgeIndex > maxAge)
                    maxAge = logLines[i].AgeIndex;
            }

            for (int i = 0; i < logLines.Count; i++)
            {
                SidebarTextFormatter.LogLine line = logLines[i];
                float t = maxAge <= 0 ? 0f : (float)line.AgeIndex / maxAge;
                Color lineColor = Color.Lerp(QudColorParser.White, QudColorParser.DarkGray, t);
                if (flashActive && line.AgeIndex == 0)
                    lineColor = QudColorParser.BrightYellow;

                int rowY = firstLogY - i;
                DrawText(contentX, rowY, line.Text, lineColor, contentWidth);
            }

            IsVisible = true;
        }

        private List<SidebarTextFormatter.LogLine> GetLogLines(SidebarSnapshot snapshot, int width, int height)
        {
            int logCount = snapshot?.LogEntriesNewestFirst?.Count ?? 0;
            string newestText = logCount > 0 ? snapshot.LogEntriesNewestFirst[0].Text : string.Empty;
            string oldestText = logCount > 0 ? snapshot.LogEntriesNewestFirst[logCount - 1].Text : string.Empty;

            if (_cachedLogWidth == width &&
                _cachedLogHeight == height &&
                _cachedLogCount == logCount &&
                _cachedNewestText == newestText &&
                _cachedOldestText == oldestText)
            {
                return _cachedLogLines;
            }

            _cachedLogLines.Clear();
            _cachedLogLines.AddRange(SidebarTextFormatter.FormatLog(snapshot?.LogEntriesNewestFirst, width, height));
            _cachedLogWidth = width;
            _cachedLogHeight = height;
            _cachedLogCount = logCount;
            _cachedNewestText = newestText;
            _cachedOldestText = oldestText;
            return _cachedLogLines;
        }

        private void DrawBackground(int startX, int bottomY, int width, int rows, bool flashActive, float flashT)
        {
            Tile blockTile = CP437TilesetGenerator.GetTextTile(CP437TilesetGenerator.SolidBlock);
            if (blockTile == null)
                return;

            Color bgColor = flashActive
                ? Color.Lerp(SidebarBgColor, QudColorParser.DarkRed, Mathf.Clamp01(flashT))
                : SidebarBgColor;

            for (int y = 0; y < rows; y++)
            {
                int rowY = bottomY + y;
                for (int x = 0; x < width; x++)
                {
                    Vector3Int pos = new Vector3Int(startX + x, rowY, 0);
                    _backgroundTilemap.SetTile(pos, blockTile);
                    _backgroundTilemap.SetTileFlags(pos, TileFlags.None);
                    _backgroundTilemap.SetColor(pos, bgColor);
                }
            }
        }

        private void DrawDivider(int dividerX, int bottomY, int rows)
        {
            Tile dividerTile = CP437TilesetGenerator.GetTextTile(CP437TilesetGenerator.BoxVertical);
            if (dividerTile == null)
                return;

            for (int y = 0; y < rows; y++)
            {
                Vector3Int pos = new Vector3Int(dividerX, bottomY + y, 0);
                _tilemap.SetTile(pos, dividerTile);
                _tilemap.SetTileFlags(pos, TileFlags.None);
                _tilemap.SetColor(pos, QudColorParser.DarkGray);
            }
        }

        private void DrawSectionHeader(int dividerX, int contentX, int y, int width, string title, Color color)
        {
            if (y < 0)
                return;

            DrawChar(dividerX, y, CP437TilesetGenerator.BoxTeeLeft, QudColorParser.DarkGray);
            for (int x = dividerX + 1; x < dividerX + width + 2; x++)
                DrawChar(x, y, CP437TilesetGenerator.BoxHorizontal, QudColorParser.DarkGray);

            DrawText(contentX, y, title, color, width);
        }

        private void DrawText(int x, int y, string text, Color color, int maxChars)
        {
            if (_tilemap == null || string.IsNullOrEmpty(text) || maxChars <= 0)
                return;

            int len = Mathf.Min(text.Length, maxChars);
            for (int i = 0; i < len; i++)
            {
                char c = text[i];
                if (c == ' ')
                    continue;

                DrawChar(x + i, y, c, color);
            }
        }

        private void DrawChar(int x, int y, char c, Color color)
        {
            Tile tile = CP437TilesetGenerator.GetTextTile(c);
            if (tile == null)
                return;

            Vector3Int pos = new Vector3Int(x, y, 0);
            _tilemap.SetTile(pos, tile);
            _tilemap.SetTileFlags(pos, TileFlags.None);
            _tilemap.SetColor(pos, color);
        }
    }
}
