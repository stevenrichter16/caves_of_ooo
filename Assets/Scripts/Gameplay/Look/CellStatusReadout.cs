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
        public static string GroundLine(Zone zone, Cell cell)
        {
            var parts = Collect(zone, cell, withCounts: true);
            if (parts == null || parts.Count == 0)
                return null;
            return "On the ground: " + string.Join(", ", parts);
        }

        /// <summary>
        /// Compact name list for titles and messages: "water, embers".
        /// Same visibility rules as <see cref="GroundLine"/>, no counts.
        /// </summary>
        public static string GroundSummary(Zone zone, Cell cell)
        {
            var parts = Collect(zone, cell, withCounts: false);
            if (parts == null || parts.Count == 0)
                return null;
            return string.Join(", ", parts);
        }

        // ── Entity afflictions ───────────────────────────────────────
        //
        // Moved here from LookQueryService so the 'c' menu can ask the
        // same question look mode asks — a frozen viper showed its
        // status in look mode and NOT in the interact menu, because the
        // affliction block was private to one surface (the entity-side
        // twin of the Jet Blast tile gap).

        /// <summary>
        /// Append "Afflicted:" plus one line per live status effect on
        /// <paramref name="entity"/> — the look-mode / FOCUS-panel long
        /// form. No-op for null entities and clean targets, so the
        /// common hover contributes zero lines.
        /// </summary>
        public static void AppendAfflictionLines(Entity entity, List<string> details)
        {
            var effectsPart = entity?.GetPart<StatusEffectsPart>();
            if (effectsPart == null)
                return;

            IReadOnlyList<Effect> effects = effectsPart.GetAllEffects();
            if (effects == null || effects.Count == 0)
                return;

            details.Add("Afflicted:");
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i] != null)
                    details.Add("- " + EffectDescriber.Describe(effects[i]));
            }
        }


        private static List<string> Collect(Zone zone, Cell cell, bool withCounts)
        {
            if (zone == null || cell == null || !cell.Explored || !cell.IsVisible)
                return null;

            // Coordinates come from the cell itself, read only after the
            // null guard: the free x,y that used to ride alongside could
            // never legitimately differ from them (adversarial review S1),
            // so the signature no longer offers a way to desync.
            var state = zone.TileState.Get(cell.X, cell.Y);
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
                parts.Add(TileStateCatalog.DisplayName(state.Cloud) + " cloud"
                    + FormatTurns(state.CloudTurns, withCounts));
            return parts;
        }

        private static string DescribeLayer(ZoneTileState.Layer layer, bool withCounts)
            => TileStateCatalog.DisplayName(layer.Id) + FormatTurns(layer.Turns, withCounts);

        /// <summary>" (N turns)" / " (1 turn)" — or nothing, when counts
        /// are off or the layer is a permanent projection
        /// (<see cref="ZoneTileState.Permanent"/>: rivers, pools). One
        /// formatter for coatings, residues AND clouds — the cloud branch
        /// used to lack the Permanent guard (adversarial review H9).</summary>
        private static string FormatTurns(int turns, bool withCounts)
        {
            if (!withCounts || turns == ZoneTileState.Permanent)
                return "";
            return " (" + turns + (turns == 1 ? " turn)" : " turns)");
        }
    }
}
