using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// P2b (Docs/PLACE-WORK-AND-PROGRESSION.md): attributes that grow with level.
    /// Qud parity (XRL.World.Parts/Leveler.cs): attribute points (AP) arrive at levels
    /// 3, 9, 15…, every attribute rises by one at 6, 12, 18…, and a Toughness change
    /// retroactively adjusts maximum health by the modifier change for every level taken
    /// (Leveler's Toughness stat-change handler). Points are spent one at a time from
    /// the skills screen's Attributes group. Emits progression/AttributeRaised and
    /// progression/AttributeRefused.
    /// </summary>
    public static class AttributeAdvancement
    {
        public static readonly string[] Attributes = { "Strength", "Agility", "Toughness", "Intelligence", "Willpower", "Ego" };
        /// <summary>Skills-screen discipline id and row-id prefix for the Attributes group.</summary>
        public const string DisciplineClass = "Attributes";
        public const string RowPrefix = "Attribute:";
        public static string RowClass(string attribute) => RowPrefix + attribute;
        public static bool IsRowClass(string cls) => cls != null && cls.StartsWith(RowPrefix, StringComparison.Ordinal);
        public static string AttributeOf(string rowClass) => IsRowClass(rowClass) ? rowClass.Substring(RowPrefix.Length) : null;

        /// <summary>Qud: an attribute point at levels divisible by 3 but not by 6.</summary>
        public static bool GrantsPoint(int newLevel) => newLevel % 3 == 0 && newLevel % 6 != 0;
        /// <summary>Qud: every attribute rises at levels divisible by 6.</summary>
        public static bool GrantsBonus(int newLevel) => newLevel > 0 && newLevel % 6 == 0;

        public static bool IsAttribute(string name) => name != null && Array.IndexOf(Attributes, name) >= 0;

        /// <summary>Spend one attribute point on <paramref name="attribute"/>.</summary>
        public static bool TrySpend(Entity actor, string attribute, out string failure)
        {
            failure = null;
            var ap = actor?.GetStat("AP");
            if (!IsAttribute(attribute) || actor.GetStat(attribute) == null) failure = "That is not an attribute you can train.";
            else if (ap == null || ap.BaseValue < 1) failure = "You have no attribute point to spend.";
            if (failure != null)
            {
                Record("AttributeRefused", actor, attribute, 0, failure);
                return false;
            }
            ap.BaseValue -= 1;
            Raise(actor, attribute, 1);
            Record("AttributeRaised", actor, attribute, actor.GetStatValue(attribute), "point");
            return true;
        }

        /// <summary>
        /// Raise an attribute's base value. A Toughness modifier change adds (or removes)
        /// that change once per level already taken, to maximum and current health alike.
        /// </summary>
        public static void Raise(Entity actor, string attribute, int amount)
        {
            var stat = actor?.GetStat(attribute);
            if (stat == null || amount == 0) return;
            int oldMod = StatUtils.GetModifier(stat.BaseValue);
            stat.BaseValue += amount;
            if (attribute != "Toughness") return;
            int delta = (StatUtils.GetModifier(stat.BaseValue) - oldMod) * Math.Max(0, actor.GetStatValue("Level", 1) - 1);
            var hp = actor.GetStat("Hitpoints");
            if (hp == null || delta == 0) return;
            hp.Max += delta;
            hp.BaseValue = Math.Max(Math.Min(hp.BaseValue + delta, hp.Max), Math.Min(hp.BaseValue, hp.Max));
        }

        static void Record(string kind, Entity actor, string attribute, int value, string reason)
        {
            if (!CavesOfOoo.Diagnostics.Diag.IsChannelEnabled("progression")) return;
            CavesOfOoo.Diagnostics.Diag.Record(category: "progression", kind: kind, actor: actor,
                payload: new { attribute, value, ap = actor?.GetStatValue("AP", 0) ?? 0, reason });
        }
    }
}
