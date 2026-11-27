using System;
using System.Collections.Generic;
using System.Globalization;

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
            => GroundLine(zone, cell, includeFishActivity: true);

        internal static string GroundLine(Zone zone, Cell cell, bool includeFishActivity)
        {
            var parts = Collect(zone, cell, withCounts: true, includeFishActivity);
            if (parts == null || parts.Count == 0)
                return null;
            return "On the ground: " + string.Join(", ", parts);
        }

        /// <summary>
        /// Compact fuel and flame lines for fires. Other fueled objects keep their
        /// usual fuel readout. Null for anything without fuel.
        /// </summary>
        public static string FuelLine(Entity entity)
        {
            if (TryFireLines(entity, out string fuelLine, out string flameLine))
                return fuelLine + "\n" + flameLine;

            var fuel = entity?.GetPart<FuelPart>();
            if (fuel == null || fuel.ParentEntity != entity || float.IsNaN(fuel.FuelMass) || float.IsInfinity(fuel.FuelMass) || fuel.FuelMass < 0f) return null;
            if (fuel.FuelMass < 0.5f) return "Fuel: none left.";
            int left = (int)System.Math.Floor(fuel.FuelMass);
            if (float.IsNaN(fuel.BurnRate) || float.IsInfinity(fuel.BurnRate) || fuel.BurnRate <= 0f) return "Fuel: " + left + " left.";
            int turns = (int)System.Math.Min(int.MaxValue / 2, System.Math.Floor(fuel.FuelMass / (double)fuel.BurnRate + 1e-6));
            bool burning = entity.GetPart<ThermalPart>()?.IsAflame == true;
            return "Fuel: " + left + " left, about " + turns + " turns" + (burning ? " of burning." : " once you light it.");
        }

        internal static bool TryFireLines(Entity entity, out string fuelLine, out string flameLine)
        {
            fuelLine = flameLine = null;
            var fire = entity?.GetPart<CampfirePart>();
            var fuel = entity?.GetPart<FuelPart>();
            var heat = entity?.GetPart<ThermalPart>();
            if (fire == null || fire.ParentEntity != entity || fuel == null || fuel.ParentEntity != entity
                || heat == null || heat.ParentEntity != entity) return false;

            bool validFuel = WorldResourceActions.Finite(fuel.FuelMass) && WorldResourceActions.Finite(fuel.MaxFuel)
                && fuel.FuelMass >= 0f && fuel.MaxFuel > 0f && fuel.FuelMass <= fuel.MaxFuel;
            fuelLine = validFuel
                ? "Fuel: " + fuel.FuelMass.ToString("G5", CultureInfo.InvariantCulture)
                    + " / " + fuel.MaxFuel.ToString("G5", CultureInfo.InvariantCulture)
                : "Fuel: unavailable";
            flameLine = "Flame remaining: " + (validFuel ? EstimateFlameTurns(entity, fire, fuel, heat) : "unavailable");
            return true;
        }

        private static string EstimateFlameTurns(Entity owner, CampfirePart fire, FuelPart fuel, ThermalPart heat)
        {
            // Destructive combustion has intensity and self-heating beyond an
            // ordinary hearth's passive countdown. Do not invent a lifetime.
            var burning = owner.GetEffect<BurningEffect>();
            if (burning?.Owner == owner && burning.Duration != 0) return "unavailable";
            float threshold = fire.FiniteCooking ? CookingService.MinimumFiniteCookingTemperature : heat.FlameTemperature;
            float temperature = heat.Temperature;
            float ambient = heat.AmbientTemperature;
            float decay = heat.AmbientDecayRate;
            if (!WorldResourceActions.Finite(temperature) || !WorldResourceActions.Finite(threshold)
                || !WorldResourceActions.Finite(ambient) || !WorldResourceActions.Finite(decay)
                || decay < 0f || decay > 1f) return "unavailable";
            if (temperature < threshold || fuel.FuelMass == 0f
                || owner.GetPart<PackableFixturePart>() != null && (PackableFixturePart.IsInertKit(owner)
                    || owner.GetPart<PhysicsPart>()?.InInventory != null || owner.GetPart<PhysicsPart>()?.Equipped != null))
                return "0 turns";

            // Only the finite hearth spends fuel on its own. Coals and legacy
            // campfires cool without that debit, so their fuel does not set a timer.
            double fuelTurns = double.PositiveInfinity;
            var glow = owner.GetPart<HearthGlowPart>();
            if (fire.FiniteCooking && glow?.ParentEntity == owner
                && owner.GetPart<LightSourcePart>()?.ParentEntity == owner)
            {
                if (!WorldResourceActions.Finite(fuel.BurnRate) || fuel.BurnRate <= 0f) return "unavailable";
                fuelTurns = Math.Ceiling(fuel.FuelMass / (double)fuel.BurnRate);
            }

            // Mirror ThermalPart's ordinary per-turn cooling and HearthGlow's
            // surviving-turn convention on local values. The reader never advances time.
            const int predictionLimit = 2000;
            for (int turn = 1; turn <= predictionLimit; turn++)
            {
                float next = temperature;
                if (next != ambient)
                {
                    next -= (next - ambient) * decay;
                    if (Math.Abs(next - ambient) < 0.5f) next = ambient;
                }
                if (!WorldResourceActions.Finite(next)) return "unavailable";
                if (next < threshold) return FormatFlameTurns(turn - 1);
                if (turn >= fuelTurns) return FormatFlameTurns(turn);
                if (next == temperature)
                {
                    if (double.IsPositiveInfinity(fuelTurns)) return "indefinite";
                    if (fuelTurns <= int.MaxValue) return FormatFlameTurns((int)fuelTurns);
                    break;
                }
                temperature = next;
            }
            return "at least " + predictionLimit.ToString(CultureInfo.InvariantCulture) + " turns";
        }

        private static string FormatFlameTurns(int turns) => turns == 0 ? "0 turns"
            : "about " + turns.ToString(CultureInfo.InvariantCulture) + (turns == 1 ? " turn" : " turns");

        /// <summary>
        /// Compact name list for titles and messages: "water, embers".
        /// Same visibility rules as <see cref="GroundLine"/>, no counts.
        /// </summary>
        public static string GroundSummary(Zone zone, Cell cell)
        {
            var parts = Collect(zone, cell, withCounts: false, includeFishActivity: true);
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


        private static List<string> Collect(Zone zone, Cell cell, bool withCounts, bool includeFishActivity)
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
            var activity = includeFishActivity ? FishPond.VisibleActivity(zone, cell, WorldClock.CurrentTick) : FishPond.Activity.None;
            string fishActivity = activity switch
            {
                FishPond.Activity.Active => "fish rings",
                FishPond.Activity.Thin => "faint fish rings",
                FishPond.Activity.Quiet => "water quiet",
                _ => null
            };
            if (state == null)
            {
                if (!preparedBed && fishActivity == null) return null;
                var visible = new List<string>();
                if (preparedBed) visible.Add("prepared planting bed");
                if (fishActivity != null) visible.Add(fishActivity);
                return visible;
            }

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
            if (fishActivity != null) parts.Add(fishActivity);
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
