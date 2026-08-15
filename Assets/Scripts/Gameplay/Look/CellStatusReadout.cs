using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// THE read-only answer to "what is the status of this cell's
    /// ground?" — one service consumed by look mode, the sidebar FOCUS
    /// panel, and the interact-menu text, so tile-layer state can never
    /// again be visible on the map but unnameable in text
    /// (Docs/STATUS-EFFECTS-STUDY-2026-08.md §6 Option 1; the Jet Blast
    /// bug was the symptom, fixed in step 1 and promoted here).
    ///
    /// <para>Both queries mirror the renderer's fog rule
    /// (ZoneRenderer.PaintTileStateMark): never reveal state the glyph
    /// layer hides. Permanent pool projections
    /// (Turns == ZoneTileState.Permanent) print no turns count. Ids
    /// resolve through <see cref="TileStateCatalog"/>.</para>
    /// </summary>
    public static class CellStatusReadout
    {
        /// <summary>
        /// Full detail line for look mode / FOCUS panel:
        /// "On the ground: water (5 turns), embers (3 turns), charged".
        /// Null when there is nothing to say (sparse store: absent means
        /// clean) or the cell is fogged.
        /// </summary>
        public static string GroundLine(Zone zone, Cell cell, int x, int y)
        {
            var parts = Collect(zone, cell, x, y, withCounts: true);
            if (parts == null || parts.Count == 0)
                return null;
            return "On the ground: " + string.Join(", ", parts);
        }

        /// <summary>
        /// Compact name list for titles and messages: "water, embers".
        /// Same visibility rules as <see cref="GroundLine"/>, no counts.
        /// </summary>
        public static string GroundSummary(Zone zone, Cell cell, int x, int y)
        {
            var parts = Collect(zone, cell, x, y, withCounts: false);
            if (parts == null || parts.Count == 0)
                return null;
            return string.Join(", ", parts);
        }

        private static List<string> Collect(
            Zone zone, Cell cell, int x, int y, bool withCounts)
        {
            if (zone == null || cell == null || !cell.Explored || !cell.IsVisible)
                return null;

            var state = zone.TileState.Get(x, y);
            if (state == null)
                return null;

            var parts = new List<string>();
            for (int i = 0; i < state.Coatings.Count; i++)
                parts.Add(DescribeLayer(state.Coatings[i], withCounts));
            for (int i = 0; i < state.Residues.Count; i++)
                parts.Add(DescribeLayer(state.Residues[i], withCounts));
            if (state.Heat > 0) parts.Add("hot");
            if (state.Cold > 0) parts.Add("cold");
            if (state.Charge > 0) parts.Add("charged");
            if (!string.IsNullOrEmpty(state.Cloud))
            {
                string cloud = TileStateCatalog.DisplayName(state.Cloud) + " cloud";
                if (withCounts)
                    cloud += " (" + state.CloudTurns
                        + (state.CloudTurns == 1 ? " turn)" : " turns)");
                parts.Add(cloud);
            }
            return parts;
        }

        private static string DescribeLayer(ZoneTileState.Layer layer, bool withCounts)
        {
            string name = TileStateCatalog.DisplayName(layer.Id);
            if (!withCounts || layer.Turns == ZoneTileState.Permanent)
                return name;
            return name + " (" + layer.Turns
                + (layer.Turns == 1 ? " turn)" : " turns)");
        }
    }
}
