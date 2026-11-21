using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// What elemental harm the viewer has actually seen in a zone (Docs/MACHINE-PROOFING.md): the proofing trough, the press rows and the coat rows use it to say which element the place in front of you has
    /// shown you, so a player who knows what they are about to meet is pointed at the right proofing. It reads only what the viewer has seen: a creature only while its cell is in view (creatures move), terrain
    /// once its cell has been seen and a pool only while it is in view (its volume changes). It reads three honest facts and no names: a creature's elemental skills (Pyromancy, Cryomancy, Galvanism, Corrosion), a terrain's declared steady heat, cold or
    /// charge, and the kind of damage a liquid pool does. A new creature or hazard that declares one of those joins it with no list to edit.
    /// </summary>
    public static class ElementalThreats
    {
        const int MaxNames = 3;
        static readonly (string prefix, string element)[] SkillFamilies = { ("Pyromancy_", "heat"), ("Cryomancy_", "cold"), ("Galvanism_", "electric"), ("Corrosion_", "acid") };

        static string ElementOfDamage(string damageType)
        {
            switch ((damageType ?? "").ToLowerInvariant())
            {
                case "acid": return "acid";
                case "fire": case "heat": return "heat";
                case "cold": case "ice": case "freeze": return "cold";
                case "electric": case "lightning": case "shock": return "electric";
                default: return null;
            }
        }

        static void Add(Dictionary<string, List<string>> seen, string element, string name)
        {
            if (element == null || string.IsNullOrEmpty(name)) return;
            if (!seen.TryGetValue(element, out var list)) seen[element] = list = new List<string>();
            if (!list.Contains(name) && list.Count < MaxNames) list.Add(name);
        }

        /// <summary>Element id to the distinct names of what the viewer has seen that harms with it (at most three each, sorted).</summary>
        public static Dictionary<string, List<string>> Seen(Zone zone, Entity viewer)
        {
            var seen = new Dictionary<string, List<string>>(); if (zone == null) return seen;
            foreach (var e in zone.GetAllEntities())
            {
                if (e == null || e == viewer) continue;
                var cell = zone.GetEntityCell(e); if (cell == null) continue;
                bool creature = e.HasTag("Creature");
                if (creature ? !cell.IsVisible : !(cell.IsVisible || cell.Explored)) continue;
                string name = e.GetDisplayName();
                if (creature)
                {
                    var skills = e.GetPart<SkillsPart>(); if (skills == null) continue;
                    foreach (var skill in skills.SkillList) { string cls = skill?.GetType().Name ?? ""; foreach (var (prefix, element) in SkillFamilies) if (cls.StartsWith(prefix, StringComparison.Ordinal)) Add(seen, element, name); }
                    continue;
                }
                var source = e.GetPart<TileStateSourcePart>();
                if (source != null) { if (source.Heat > 0) Add(seen, "heat", name); if (source.Cold > 0) Add(seen, "cold", name); if (source.Charge > 0) Add(seen, "electric", name); }
                var pool = e.GetPart<LiquidPoolPart>();
                if (pool != null && cell.IsVisible && pool.Volume > 0 && LiquidRegistry.IsInitialized) Add(seen, ElementOfDamage(LiquidRegistry.Get(pool.LiquidId)?.PerTurnDamage?.Type), name);
            }
            foreach (var list in seen.Values) list.Sort(StringComparer.Ordinal);
            return seen;
        }

        /// <summary>"You have seen heat (steam vent), acid (Marlback soursprayer) in this place." or empty when nothing elemental has been seen.</summary>
        public static string Summary(Zone zone, Entity viewer)
        {
            var seen = Seen(zone, viewer); if (seen.Count == 0) return "";
            var parts = new List<string>();
            foreach (var element in ProofingRegistry.All) if (seen.TryGetValue(element.Id, out var names)) parts.Add(element.Label + " (" + string.Join(", ", names) + ")");
            return parts.Count == 0 ? "" : "You have seen " + string.Join(", ", parts) + " in this place.";
        }

        /// <summary>The names of what has been seen for one element, joined, or empty.</summary>
        public static string ForElement(Zone zone, Entity viewer, string elementId)
            => Seen(zone, viewer).TryGetValue(elementId ?? "", out var names) ? string.Join(", ", names) : "";
    }
}
