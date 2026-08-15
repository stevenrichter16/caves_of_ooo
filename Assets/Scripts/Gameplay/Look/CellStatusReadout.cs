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

        /// <summary>
        /// Compact comma list for one-line surfaces: "frozen over,
        /// soaked" (capped at <paramref name="maxNamed"/> names, then
        /// "+N"). Null when the entity is clean.
        ///
        /// <para>Labels are derived from <see cref="EffectDescriber"/>'s
        /// own lines — the text before each "Label - detail" separator —
        /// so the short and long forms share ONE wording source and can
        /// never disagree.</para>
        /// </summary>
        public static string AfflictionSummary(Entity entity, int maxNamed = 2)
        {
            var effectsPart = entity?.GetPart<StatusEffectsPart>();
            if (effectsPart == null)
                return null;

            IReadOnlyList<Effect> effects = effectsPart.GetAllEffects();
            if (effects == null || effects.Count == 0)
                return null;

            var labels = new List<string>();
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i] != null)
                    labels.Add(ShortLabel(effects[i]));
            }
            if (labels.Count == 0)
                return null;

            if (labels.Count > maxNamed)
            {
                int extra = labels.Count - maxNamed;
                labels.RemoveRange(maxNamed, extra);
                return string.Join(", ", labels) + ", +" + extra;
            }
            return string.Join(", ", labels);
        }

        private static string ShortLabel(Effect effect)
        {
            string full = EffectDescriber.Describe(effect);
            int cut = full.IndexOf(" - ", System.StringComparison.Ordinal);
            // Lines without the separator ("Drenched in oil (3).") fall
            // back to the whole sentence minus its period.
            string label = cut > 0 ? full.Substring(0, cut) : full.TrimEnd('.');
            if (label.Length > 0 && char.IsUpper(label[0]))
                label = char.ToLowerInvariant(label[0]) + label.Substring(1);
            return label;
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
