using System;
namespace CavesOfOoo.Core
{
    /// <summary>Finite cost for actual liquid contact and floor slipping. Pure
    /// queries only: no effects, events, RNG, allocations or persistent cache.
    /// Tile energy alone is reaction state, not invented contact damage.</summary>
    public static class TerrainNavigationWeight
    {
        public const int MaxPenalty = 90;
        public const int BasePenalty = 20;
        /// <summary>Maximum gas/terrain penalty across the prospective physical
        /// body. Taking a maximum avoids charging projected pools or wide bodies
        /// repeatedly. Null actors preserve legacy unweighted searches.</summary>
        public static int ForStep(Zone zone, int x, int y, Entity actor)
        {
            if (zone == null || actor == null) return 0;
            int max = 0;
            foreach (var cell in zone.GetOccupiedCells(actor, x, y))
            {
                if (cell == null) continue;
                max = Math.Max(max, GasNavigationWeight.ForCell(cell, actor));
                max = Math.Max(max, ForCell(cell, actor));
            }
            return max;
        }
        /// <summary>Actual nonempty pools can coat creatures; floor coatings
        /// independently cause slips. Full relevant resistance removes damage
        /// cost but not action blocking or harmful stat changes. Incoming negative
        /// resistance modifiers are included conservatively before claiming immunity.</summary>
        public static int ForCell(Cell cell, Entity actor)
        {
            if (cell == null || actor == null || !actor.HasTag("Creature") || !LiquidRegistry.IsInitialized) return 0;
            int max = 0;
            var state = cell.ParentZone?.TileState.Get(cell.X, cell.Y);
            if (state != null)
                for (int i = 0; i < state.Coatings.Count; i++)
                {
                    var layer = state.Coatings[i];
                    if (layer.Turns <= 0) continue;
                    var def = LiquidRegistry.Get(layer.Id);
                    if (def?.Slippery == true && def.SlipChance > 0)
                        max = Math.Max(max, BasePenalty + Math.Min(100, def.SlipChance) / 4);
                }
            // This is the pool's existing creature exposure gate, not a flight
            // exemption (the contact/slip systems do not have one).
            long capacity = (long)actor.GetStatValue("Strength") + actor.GetStatValue("Toughness");
            if (capacity <= 0) return max;
            foreach (var owner in cell.Occupants)
            {
                var pool = owner?.GetPart<LiquidPoolPart>();
                if (pool == null || pool.Volume <= 0) continue;
                var def = LiquidRegistry.Get(pool.LiquidId);
                if (def == null) continue;
                int weight = def.BlockAction ? MaxPenalty : 0;
                if (HasNegative(actor, def.StatModifiers) || HasNegative(actor, def.ResistanceModifiers))
                    weight = Math.Max(weight, BasePenalty);
                if (def.PerTurnDamage?.Amount > 0 && !Resists(actor, def))
                    weight = Math.Max(weight, (int)Math.Min(MaxPenalty, BasePenalty + (long)def.PerTurnDamage.Amount * 6));
                max = Math.Max(max, weight);
            }
            return max;
        }
        private static bool HasNegative(Entity actor, System.Collections.Generic.List<LiquidStatMod> modifiers)
        {
            if (modifiers != null)
                for (int i = 0; i < modifiers.Count; i++)
                    if (modifiers[i] != null && modifiers[i].Delta < 0 && !string.IsNullOrEmpty(modifiers[i].Stat) && actor.GetStat(modifiers[i].Stat) != null) return true;
            return false;
        }
        private static bool Resists(Entity actor, LiquidDefinition def)
        {
            string stat = ResistanceFor(def.PerTurnDamage.Type);
            if (stat == null) return false;
            // A newly acquired coating may replace an older immunity coating;
            // only an immunity supplied by this same liquid is unconditional.
            string element = stat == "AcidResistance" ? "Acid" : stat == "HeatResistance" ? "Heat"
                : stat == "ColdResistance" ? "Cold" : "Electric";
            if (string.Equals(def.ImmuneElement, element, StringComparison.OrdinalIgnoreCase)) return true;
            long resistance = actor.GetStatValue(stat);
            if (def.ResistanceModifiers != null)
                for (int i = 0; i < def.ResistanceModifiers.Count; i++)
                {
                    var mod = def.ResistanceModifiers[i];
                    if (mod != null && mod.Stat == stat && mod.Delta < 0) resistance += mod.Delta;
                }
            return resistance >= 100;
        }
        private static string ResistanceFor(string type)
        {
            // Damage.AddAttribute's aliases are case-sensitive.
            switch (type)
            {
                case "Acid": return "AcidResistance";
                case "Fire": case "Heat": return "HeatResistance";
                case "Cold": case "Ice": case "Freeze": return "ColdResistance";
                case "Electric": case "Shock": case "Lightning": case "Electricity": return "ElectricResistance";
                default: return null;
            }
        }
    }
}
