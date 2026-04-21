using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Phase 10 companion overlay — a partially-transparent column on the
    /// right edge of the play area that lists, for every Creature-tagged
    /// entity in the current zone, their name and current
    /// <see cref="BrainPart.LastThought"/>. Non-blocking: the player can
    /// walk/act normally while the overlay is visible; it refreshes on
    /// every <c>RenderZone</c> call (which already fires on every turn
    /// advance), so thoughts update "in real time" in turn-based terms.
    ///
    /// Rendering strategy:
    /// - Draws directly onto the main world tilemap + bg tilemap so every
    ///   RenderZone sweep naturally re-stamps the overlay. No separate
    ///   tilemap needed; no clear-on-hide needed.
    /// - Bg tilemap gets a dim-alpha SolidBlock behind each overlay cell
    ///   so game glyphs behind the panel are visible-but-dimmed (the
    ///   "partially transparent menu" requirement).
    /// - Skip the play-area cells behind the overlay column are overdrawn
    ///   by the overlay's characters, so the world content in that
    ///   column is temporarily obscured — expected for a floating panel.
    /// </summary>
    public sealed class ThoughtLogOverlayRenderer
    {
        /// <summary>Overlay panel width in map cells (column count).</summary>
        public const int PanelWidth = 24;

        /// <summary>Right-edge X of the overlay (exclusive — last drawn col is X-1).</summary>
        private const int RightEdgeX = Zone.Width; // 80

        /// <summary>Vertical padding from the top of the zone grid.</summary>
        private const int TopPadding = 1;

        /// <summary>Max entries listed before collapsing overflow into "... (N more)".</summary>
        private const int MaxEntries = 20;

        /// <summary>Semi-transparent navy backdrop so world glyphs dim through.</summary>
        private static readonly Color PanelBgColor = new Color(0.05f, 0.05f, 0.12f, 0.78f);
        private static readonly Color HeaderColor = QudColorParser.BrightYellow;
        private static readonly Color NameColor = QudColorParser.White;
        private static readonly Color ThoughtColor = QudColorParser.Gray;
        private static readonly Color EmptyThoughtColor = QudColorParser.DarkGray;
        private static readonly Color OverflowColor = QudColorParser.DarkGray;

        /// <summary>
        /// Paint the overlay to the given tilemaps in world-space grid coords.
        /// Caller is responsible for the Unity→roguelike Y inversion passed in
        /// through <paramref name="yInvert"/>: for any roguelike row Y, the
        /// Unity tile row is <c>yInvert - Y</c>.
        /// </summary>
        public void Draw(Zone zone, Tilemap fg, Tilemap bg, int yInvert)
        {
            if (zone == null || fg == null) return;

            List<ThoughtEntry> entries = CollectThoughts(zone);
            int startX = RightEdgeX - PanelWidth; // 56 by default

            // Header line: "-- THOUGHTS --" centered in the panel width.
            int lineY = TopPadding;
            DrawPanelRow(fg, bg, startX, lineY, yInvert, "-- THOUGHTS --", HeaderColor);
            lineY++;

            if (entries.Count == 0)
            {
                DrawPanelRow(fg, bg, startX, lineY, yInvert, "(no creatures in zone)", EmptyThoughtColor);
                // Continue to fill the bg for the rest of the panel height so
                // the alpha panel looks uniform instead of raggedly tall.
                FillRemainingBg(bg, startX, lineY + 1, yInvert);
                return;
            }

            int shown = Mathf.Min(entries.Count, MaxEntries);
            for (int i = 0; i < shown; i++)
            {
                var e = entries[i];
                // Line 1: name (truncated)
                string name = Truncate(e.Name, PanelWidth);
                DrawPanelRow(fg, bg, startX, lineY, yInvert, name, NameColor);
                lineY++;

                // Line 2: indented thought (or "..." in dark gray for empty)
                bool empty = string.IsNullOrEmpty(e.Thought);
                string body = empty ? "..." : "  " + e.Thought;
                Color color = empty ? EmptyThoughtColor : ThoughtColor;
                DrawPanelRow(fg, bg, startX, lineY, yInvert, Truncate(body, PanelWidth), color);
                lineY++;

                // Safety: stop before we overflow the visible zone height.
                if (lineY >= Zone.Height - 1) break;
            }

            int overflow = entries.Count - shown;
            if (overflow > 0 && lineY < Zone.Height - 1)
            {
                DrawPanelRow(fg, bg, startX, lineY, yInvert,
                    "... (" + overflow + " more)", OverflowColor);
                lineY++;
            }

            FillRemainingBg(bg, startX, lineY, yInvert);
        }

        /// <summary>
        /// Gather one <see cref="ThoughtEntry"/> per Creature-tagged entity
        /// with a <see cref="BrainPart"/>. Sorted deterministically by name
        /// so the order doesn't jitter between renders (a list whose entries
        /// reorder each tick is painful to read).
        /// </summary>
        private static List<ThoughtEntry> CollectThoughts(Zone zone)
        {
            var result = new List<ThoughtEntry>();
            foreach (var entity in zone.GetReadOnlyEntities())
            {
                if (entity == null) continue;
                if (!entity.HasTag("Creature")) continue;
                var brain = entity.GetPart<BrainPart>();
                if (brain == null) continue;

                string name = entity.GetDisplayName() ?? entity.BlueprintName ?? "?";
                result.Add(new ThoughtEntry(name, brain.LastThought ?? string.Empty));
            }
            result.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return result;
        }

        private static void DrawPanelRow(
            Tilemap fg, Tilemap bg, int startX, int rowRoguelikeY, int yInvert,
            string text, Color textColor)
        {
            int unityY = yInvert - rowRoguelikeY;
            PaintBgRow(bg, startX, unityY);

            if (fg == null || string.IsNullOrEmpty(text)) return;
            int len = Mathf.Min(text.Length, PanelWidth);
            for (int i = 0; i < len; i++)
            {
                char c = text[i];
                if (c == ' ') continue; // skip — bg alpha already paints the cell
                Tile tile = CP437TilesetGenerator.GetTile(c);
                if (tile == null) continue;
                Vector3Int p = new Vector3Int(startX + i, unityY, 0);
                fg.SetTile(p, tile);
                fg.SetTileFlags(p, TileFlags.None);
                fg.SetColor(p, textColor);
            }
        }

        private static void PaintBgRow(Tilemap bg, int startX, int unityY)
        {
            if (bg == null) return;
            Tile block = CP437TilesetGenerator.GetTile(CP437TilesetGenerator.SolidBlock);
            if (block == null) return;
            for (int i = 0; i < PanelWidth; i++)
            {
                Vector3Int p = new Vector3Int(startX + i, unityY, 0);
                bg.SetTile(p, block);
                bg.SetTileFlags(p, TileFlags.None);
                bg.SetColor(p, PanelBgColor);
            }
        }

        /// <summary>
        /// Paint the dim panel background from <paramref name="fromRoguelikeY"/>
        /// down to the bottom of the zone so the overlay looks like a
        /// contiguous column rather than a short list.
        /// </summary>
        private static void FillRemainingBg(Tilemap bg, int startX, int fromRoguelikeY, int yInvert)
        {
            if (bg == null) return;
            for (int y = fromRoguelikeY; y < Zone.Height; y++)
                PaintBgRow(bg, startX, yInvert - y);
        }

        private static string Truncate(string s, int width)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            if (s.Length <= width) return s;
            if (width <= 1) return s.Substring(0, width);
            return s.Substring(0, width - 1) + ">";
        }

        private readonly struct ThoughtEntry
        {
            public readonly string Name;
            public readonly string Thought;
            public ThoughtEntry(string name, string thought) { Name = name; Thought = thought; }
        }
    }
}
