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
        /// Null when there is nothing to say or the cell is fogged.
        /// Prepared soil is an entity condition, independent of the sparse
        /// tile-state store used by coatings and hazards.
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
            if (zone == null || cell == null || !cell.Explored || !cell.IsVisible
                || cell.ParentZone != zone || zone.GetCell(cell.X, cell.Y) != cell)
                return null;

            var manager = WorldLocationContext.For(zone);
            if (manager != null && (manager.ActiveZone != zone
                || !manager.CachedZones.TryGetValue(zone.ZoneID, out var current) || current != zone))
                return null;

            // Coordinates come from the cell itself, read only after the
            // null guard: the free x,y that used to ride alongside could
            // never legitimately differ from them (adversarial review S1),
            // so the signature no longer offers a way to desync.
            var state = zone.TileState.Get(cell.X, cell.Y);
            bool preparedBed = HasDisclosedPreparedBed(zone, cell);
            if (state == null)
                return preparedBed ? new List<string> { "prepared planting bed" } : null;

            var parts = new List<string>();
            bool gritted = LiquidSlipSystem.HasGrit(zone, cell.X, cell.Y);
            for (int i = 0; i < state.Coatings.Count; i++)
            {
                var layer = state.Coatings[i]; bool duplicate = false;
                for (int j = 0; j < i; j++) if (state.Coatings[j].Id == layer.Id) { duplicate = true; break; }
                if (duplicate) continue;
                // Multiple finite owners share one named ground condition. Show the longest
                // current lease, not repeated copies of the same liquid in the focus panel.
                var longest = layer;
                for (int j = i + 1; j < state.Coatings.Count; j++)
                    if (state.Coatings[j].Id == layer.Id && state.Coatings[j].Turns > longest.Turns) longest = state.Coatings[j];
                parts.Add(DescribeLayer(longest, withCounts, gritted));
            }
            for (int i = 0; i < state.Residues.Count; i++)
                parts.Add(DescribeLayer(state.Residues[i], withCounts));
            if (state.Heat > 0) parts.Add("hot");
            if (state.Cold > 0) parts.Add("cold");
            if (state.Charge > 0) parts.Add("charged");
            if (!string.IsNullOrEmpty(state.Cloud))
                parts.Add(TileStateCatalog.DisplayName(state.Cloud) + " cloud"
                    + FormatTurns(state.CloudTurns, withCounts)
                    + (state.Cloud == "veil-mist" && state.CloudTurns > 0 ? " (blocks sight)" : ""));
            if (preparedBed) parts.Add("prepared planting bed");
            return parts;
        }

        private static bool HasDisclosedPreparedBed(Zone zone, Cell cell)
        {
            if (!CultivatedSoilPart.IsCultivated(zone, cell)) return false;

            bool disclosedSoil = false;
            foreach (var owner in cell.Objects)
            {
                var render = owner?.GetPart<RenderPart>();
                if (render == null || render.ParentEntity != owner || !render.Visible) continue;

                var crop = owner.GetPart<CropPart>();
                // A visible live crop already supplies its own growth readout.
                // Hidden or foreign parts must not disclose an unseen occupant.
                if (crop != null && crop.ParentEntity == owner && CropTime.IsCurrentOwner(crop, zone)
                    && zone.GetEntityCell(owner) == cell) return false;

                var soil = owner.GetPart<CultivatedSoilPart>();
                var physics = owner.GetPart<PhysicsPart>();
                // Cultivation and visibility must belong to the SAME eligible
                // terrain owner; a visible decorative row cannot vouch for it.
                if (soil != null && soil.ParentEntity == owner && owner.SpatialZone == zone
                    && zone.GetEntityCell(owner) == cell && owner.HasTag("Terrain") && owner.HasTag("Plantable")
                    && !owner.HasTag("Solid") && !owner.HasTag("Creature") && !owner.HasTag("Item")
                    && physics != null && physics.ParentEntity == owner && !physics.Solid && !physics.Takeable
                    && physics.InInventory == null && physics.Equipped == null) disclosedSoil = true;
            }
            return disclosedSoil;
        }

        /// <summary>"ice (4 turns, slippery)" / "ice (slippery)" / "oil".
        /// The slippery flag rides inside the same parenthetical as the
        /// turn count so a coating never grows two brackets. Residues
        /// share this path; their ids are not liquids, so the registry
        /// answers null and they are never flagged.</summary>
        private static string DescribeLayer(ZoneTileState.Layer layer, bool withCounts, bool gritted = false)
        {
            string name = TileStateCatalog.DisplayName(layer.Id);
            string turns = FormatTurns(layer.Turns, withCounts);   // "" or " (N turns)"
            if (layer.Id == "grit" && layer.Turns > 0) return name + turns + " (firm footing)";
            var def = LiquidRegistry.IsInitialized ? LiquidRegistry.Get(layer.Id) : null;
            bool slippery = def != null && def.Slippery && !gritted;
            if (!slippery) return name + turns;
            if (turns.Length == 0) return name + " (slippery)";
            return name + turns.Substring(0, turns.Length - 1) + ", slippery)";
        }

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
