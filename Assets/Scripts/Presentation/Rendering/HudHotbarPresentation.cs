using System;
using System.Text;
using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    public enum SkillHudIcon { Fallback, EmberSpit, FlamingHands, JetBlast, GroundSurge, RimeGrip, Calm }

    /// <summary>Bounded display rules only. Never executes or predicts an ability.</summary>
    public static class HudHotbarPresentation
    {
        public static SkillHudIcon IconFor(string sourcePowerClass)
        {
            switch (sourcePowerClass)
            {
                case "Pyromancy_EmberSpit": return SkillHudIcon.EmberSpit;
                case "Pyromancy_FlamingHands": return SkillHudIcon.FlamingHands;
                case "Hydromancy_JetBlast": return SkillHudIcon.JetBlast;
                case "Galvanism_GroundSurge": return SkillHudIcon.GroundSurge;
                case "Cryomancy_RimeGrip": return SkillHudIcon.RimeGrip;
                case "Spellcraft_Calm": return SkillHudIcon.Calm;
                default: return SkillHudIcon.Fallback;
            }
        }

        /// <summary>Two bounded rows; > explicitly discloses a remaining suffix.</summary>
        public static void NameLines(string name, out string first, out string second)
        {
            first = second = string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return;
            var normalized = new StringBuilder(name.Length);
            bool space = false;
            foreach (char c in name)
            {
                if (char.IsWhiteSpace(c)) { space = normalized.Length > 0; continue; }
                if (space) normalized.Append(' ');
                normalized.Append(c); space = false;
            }
            string text = normalized.ToString();
            const int width = GameplayHotbarLayout.SlotWidth;
            if (text.Length <= width) { first = text; return; }
            int split = text.LastIndexOf(' ', width - 1, width);
            if (split < 1) split = width;
            first = text.Substring(0, split);
            text = text.Substring(split).TrimStart();
            second = Fit(text, width);
        }

        public static string Fit(string text, int width)
        {
            if (width < 1 || string.IsNullOrEmpty(text)) return string.Empty;
            return text.Length <= width ? text : text.Substring(0, width - 1) + ">";
        }

        public static string StateLabel(HotbarSlotSnapshot slot)
        {
            if (!slot.Occupied) return "EMPTY";
            if (slot.Pending) return "AIM";
            if (slot.CooldownRemaining <= 0) return "READY";
            return slot.CooldownRemaining > 999 ? "CD999+" : "CD" + slot.CooldownRemaining;
        }

        public static string TargetLabel(HotbarSlotSnapshot slot)
        {
            switch (slot.TargetingMode)
            {
                case AbilityTargetingMode.SelfCentered: return "self";
                case AbilityTargetingMode.AdjacentCell: return "adjacent";
                case AbilityTargetingMode.DirectionLine: return "line " + slot.Range;
                default: return "target " + slot.Range;
            }
        }
    }
}
