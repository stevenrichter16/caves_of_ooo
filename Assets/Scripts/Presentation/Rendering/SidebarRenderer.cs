using System.Collections.Generic;
using CavesOfOoo.Diagnostics;
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
        private readonly List<SidebarTextFormatter.LogLine> _visibleLogLines = new List<SidebarTextFormatter.LogLine>();
        private int _cachedLogWidth = -1;
        private int _cachedLogCount = -1;
        private int _cachedNewestSerial = -1;
        private int _cachedOldestSerial = -1;
        private int _cachedNewestCount = -1;
        private int _cachedOldestCount = -1;
        private int _logScrollOffsetRows;
        private int _maxLogScrollOffsetRows;
        private int _lastLogViewportHeight = -1;

        public GameplaySidebarRenderer(Tilemap tilemap, Tilemap backgroundTilemap, Transform gridTransform, float referenceZoom)
        {
            _tilemap = tilemap;
            _backgroundTilemap = backgroundTilemap;
            _gridTransform = gridTransform;
            _referenceZoom = referenceZoom;
        }

        public bool IsVisible { get; private set; }
        public int LogScrollOffsetRows => _logScrollOffsetRows;

        public void Invalidate()
        {
            _cachedLogWidth = -1;
            _cachedLogCount = -1;
            _cachedNewestSerial = -1;
            _cachedOldestSerial = -1;
            _cachedNewestCount = -1;
            _cachedOldestCount = -1;
            _cachedLogLines.Clear();
            _visibleLogLines.Clear();
        }

        public void Clear()
        {
            IsVisible = false;
            if (_tilemap != null)
            {
                _tilemap.ClearAllTiles();
                PerformanceDiagnostics.RecordTilemapClear();
            }

            if (_backgroundTilemap != null)
            {
                _backgroundTilemap.ClearAllTiles();
                PerformanceDiagnostics.RecordTilemapClear();
            }
        }

        public bool ScrollOlder(int rows = 1)
        {
            int safeRows = Mathf.Max(1, rows);
            int next = Mathf.Clamp(_logScrollOffsetRows + safeRows, 0, _maxLogScrollOffsetRows);
            if (next == _logScrollOffsetRows)
                return false;

            _logScrollOffsetRows = next;
            return true;
        }

        public bool ScrollNewer(int rows = 1)
        {
            int safeRows = Mathf.Max(1, rows);
            int next = Mathf.Max(0, _logScrollOffsetRows - safeRows);
            if (next == _logScrollOffsetRows)
                return false;

            _logScrollOffsetRows = next;
            return true;
        }

        public void ResetLogScroll()
        {
            _logScrollOffsetRows = 0;
        }

        public void Render(SidebarSnapshot snapshot, Camera camera, int sidebarWidthChars, bool flashActive, float flashT)
        {
            using (PerformanceMarkers.Ui.SidebarRender.Auto())
            {
                PerformanceDiagnostics.RecordSidebarRender();
                Clear();

                if (_tilemap == null || _backgroundTilemap == null || _gridTransform == null || camera == null || sidebarWidthChars <= 0)
                    return;

                SidebarCameraMetrics metrics = GameplayViewportLayout.MeasureSidebarCamera(camera, _referenceZoom, sidebarWidthChars);
                _gridTransform.localScale = new Vector3(metrics.Scale, metrics.Scale, 1f);

            int startX = metrics.StartCharX;
            int width = sidebarWidthChars;
            int rows = metrics.VisibleRowCount;
            int topY = metrics.TopTextY;
            int bottomY = metrics.BottomTextY;
            int contentX = startX + 2;
            int contentWidth = Mathf.Max(1, width - 3);

            DrawBackground(startX, bottomY, width, rows, flashActive, flashT);
            DrawDivider(startX, bottomY, rows);

            int y = topY;

            // Phase 10 — renderer is data-driven: a snapshot with empty
            // vitals, null status, null focus, and non-null thoughts becomes
            // a standalone thought-overlay instance of this same class.
            // Each section only draws its header when the data for that
            // section is actually populated; empty sections compress away.
            bool hasVitals = snapshot?.VitalLines != null && snapshot.VitalLines.Count > 0;
            bool hasStatus = !string.IsNullOrEmpty(snapshot?.StatusText) && snapshot.StatusText != "-";
            bool hasFocus = snapshot?.FocusSnapshot != null;

            if (hasVitals || hasStatus)
            {
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
            }

            if (hasFocus)
            {
                DrawSectionHeader(startX, contentX, y, contentWidth, "FOCUS", QudColorParser.White);
                y--;

                int remainingAfterFocusHeader = y - bottomY + 1;
                // Phase 10 — when the AI goal-stack inspector is populated on the
                // focus snapshot, grant extra height to the focus panel so the
                // inspector block isn't clipped to 6 lines.
                bool inspectorActive = snapshot?.FocusSnapshot?.GoalStackLines != null
                    || snapshot?.FocusSnapshot?.LastThought != null;
                int focusCeiling = inspectorActive ? 14 : 6;
                int focusMaxLines = Mathf.Clamp(remainingAfterFocusHeader - 4, 2, focusCeiling);
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
            }

            int bottomHeaderY = y;
            int bottomHeight = Mathf.Max(1, bottomHeaderY - bottomY);

            if (snapshot?.ThoughtEntries != null)
            {
                // Thought-overlay mode: bottom panel is THOUGHTS instead of LOG.
                DrawThoughtsPanel(
                    startX, contentX, bottomHeaderY, bottomY, bottomHeight,
                    contentWidth, snapshot.ThoughtEntries);
            }
            else
            {
                DrawLogPanel(
                    startX, contentX, bottomHeaderY, bottomY, bottomHeight,
                    contentWidth, snapshot, flashActive);
            }

                IsVisible = true;
            }
        }

        /// <summary>Render the bottom panel as the live message log.</summary>
        private void DrawLogPanel(
            int startX, int contentX, int headerY, int bottomY, int height,
            int contentWidth, SidebarSnapshot snapshot, bool flashActive)
        {
            List<SidebarTextFormatter.LogLine> logLines = GetVisibleLogLines(
                snapshot,
                contentWidth,
                height,
                out bool hasOlderRows,
                out bool hasNewerRows);
            string logHeader = BuildLogHeader(hasOlderRows, hasNewerRows);
            Color logHeaderColor = flashActive ? QudColorParser.BrightYellow : QudColorParser.White;
            DrawSectionHeader(startX, contentX, headerY, contentWidth, logHeader, logHeaderColor);

            int firstLogY = bottomY + logLines.Count - 1;
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
        }

        /// <summary>
        /// Render the bottom panel as a per-creature thought list. Used when
        /// the renderer is instantiated as a standalone thought-overlay
        /// (Phase 10 't' toggle). Same tilemap + DrawText helpers as
        /// DrawLogPanel — that's what "reuse the same class as the logger"
        /// means in this refactor: one <c>GameplaySidebarRenderer</c> class
        /// serves both the main sidebar and the thought overlay, the only
        /// difference is which <c>SidebarSnapshot</c> it's given.
        /// </summary>
        private void DrawThoughtsPanel(
            int startX, int contentX, int headerY, int bottomY, int height,
            int contentWidth, IReadOnlyList<SidebarThoughtEntry> entries)
        {
            DrawSectionHeader(startX, contentX, headerY, contentWidth,
                "THOUGHTS", QudColorParser.BrightYellow);

            if (entries.Count == 0)
            {
                DrawText(contentX, headerY - 1, "(no creatures)",
                    QudColorParser.DarkGray, contentWidth);
                return;
            }

            int maxEntriesNoOverflow = Mathf.Max(1, height / 2);
            int maxEntriesWithOverflow = Mathf.Max(1, (height - 1) / 2);
            bool needsOverflow = entries.Count > maxEntriesNoOverflow;
            int shown = needsOverflow
                ? Mathf.Min(entries.Count, maxEntriesWithOverflow)
                : entries.Count;

            int y = headerY - 1;
            for (int i = 0; i < shown && y >= bottomY; i++)
            {
                var entry = entries[i];
                DrawText(contentX, y, entry.Name, QudColorParser.White, contentWidth);
                y--;
                if (y < bottomY) break;

                bool empty = string.IsNullOrEmpty(entry.Thought);
                string body = empty ? "  ..." : "  " + entry.Thought;
                Color color = empty ? QudColorParser.DarkGray : QudColorParser.Gray;
                DrawText(contentX, y, body, color, contentWidth);
                y--;
            }

            int overflow = entries.Count - shown;
            if (overflow > 0 && y >= bottomY)
            {
                DrawText(contentX, y, "... (" + overflow + " more)",
                    QudColorParser.DarkGray, contentWidth);
            }
        }

        private List<SidebarTextFormatter.LogLine> GetVisibleLogLines(
            SidebarSnapshot snapshot,
            int width,
            int height,
            out bool hasOlderRows,
            out bool hasNewerRows)
        {
            int logCount = snapshot?.LogEntriesNewestFirst?.Count ?? 0;
            int newestSerial = logCount > 0 ? snapshot.LogEntriesNewestFirst[0].NewestSerial : -1;
            int oldestSerial = logCount > 0 ? snapshot.LogEntriesNewestFirst[logCount - 1].NewestSerial : -1;
            int newestCount = logCount > 0 ? snapshot.LogEntriesNewestFirst[0].Count : 0;
            int oldestCount = logCount > 0 ? snapshot.LogEntriesNewestFirst[logCount - 1].Count : 0;
            bool cacheChanged = _cachedLogWidth != width ||
                                _cachedLogCount != logCount ||
                                _cachedNewestSerial != newestSerial ||
                                _cachedOldestSerial != oldestSerial ||
                                _cachedNewestCount != newestCount ||
                                _cachedOldestCount != oldestCount;

            bool viewportChanged = _lastLogViewportHeight != height;
            SidebarTextFormatter.LogLine? anchor = null;
            if (_logScrollOffsetRows > 0 && _cachedLogLines.Count > 0)
            {
                int oldEndExclusive = Mathf.Clamp(_cachedLogLines.Count - _logScrollOffsetRows, 1, _cachedLogLines.Count);
                anchor = _cachedLogLines[oldEndExclusive - 1];
            }

            if (cacheChanged)
            {
                _cachedLogLines.Clear();
                _cachedLogLines.AddRange(SidebarTextFormatter.FormatLog(snapshot?.LogEntriesNewestFirst, width));
                _cachedLogWidth = width;
                _cachedLogCount = logCount;
                _cachedNewestSerial = newestSerial;
                _cachedOldestSerial = oldestSerial;
                _cachedNewestCount = newestCount;
                _cachedOldestCount = oldestCount;
            }

            if ((cacheChanged || viewportChanged) && anchor.HasValue)
            {
                int anchorIndex = FindMatchingAnchorIndex(_cachedLogLines, anchor.Value);
                if (anchorIndex >= 0)
                    _logScrollOffsetRows = Mathf.Max(0, _cachedLogLines.Count - (anchorIndex + 1));
            }

            _maxLogScrollOffsetRows = Mathf.Max(0, _cachedLogLines.Count - Mathf.Max(1, height));
            _logScrollOffsetRows = Mathf.Clamp(_logScrollOffsetRows, 0, _maxLogScrollOffsetRows);
            _lastLogViewportHeight = height;

            int endExclusive = _cachedLogLines.Count == 0
                ? 0
                : Mathf.Clamp(_cachedLogLines.Count - _logScrollOffsetRows, 1, _cachedLogLines.Count);
            int start = Mathf.Max(0, endExclusive - Mathf.Max(1, height));

            hasOlderRows = start > 0;
            hasNewerRows = endExclusive < _cachedLogLines.Count;

            _visibleLogLines.Clear();
            for (int i = start; i < endExclusive; i++)
                _visibleLogLines.Add(_cachedLogLines[i]);

            return _visibleLogLines;
        }

        private static int FindMatchingAnchorIndex(
            List<SidebarTextFormatter.LogLine> lines,
            SidebarTextFormatter.LogLine anchor)
        {
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (lines[i].MatchesIdentity(anchor))
                    return i;
            }

            if (anchor.EntryNewestSerial >= 0)
            {
                int bestIndex = -1;
                int bestDistance = int.MaxValue;
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].EntryNewestSerial != anchor.EntryNewestSerial)
                        continue;

                    int distance = Mathf.Abs(lines[i].RowIndexWithinEntry - anchor.RowIndexWithinEntry);
                    if (distance < bestDistance || (distance == bestDistance && i > bestIndex))
                    {
                        bestDistance = distance;
                        bestIndex = i;
                    }
                }

                return bestIndex;
            }

            return -1;
        }

        private static string BuildLogHeader(bool hasOlderRows, bool hasNewerRows)
        {
            if (hasOlderRows && hasNewerRows)
                return "LOG ^v";
            if (hasOlderRows)
                return "LOG ^";
            if (hasNewerRows)
                return "LOG v";
            return "LOG";
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
            for (int x = dividerX + 1; x < dividerX + width; x++)
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
