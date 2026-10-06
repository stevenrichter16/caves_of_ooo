using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>Numeric facts for the five direct-stat mods. The same constants drive Apply.
    /// Other mods retain their recipe description without running enhancement callbacks.</summary>
    public static class TinkerModificationPreview
    {
        public static string Describe(Entity item, string id)
        {
            if (!TinkerModificationRegistry.TryCreate(id, out var mod)) return "Numeric preview unavailable for this modification.";
            if (!(mod is SharpTinkerModification || mod is ReinforcedPlatingTinkerModification || mod is FlexweaveTinkerModification
                || mod is HardenedShellTinkerModification || mod is DuelistCutTinkerModification))
                return "Numeric preview unavailable for this conditional enhancement; see the recipe description.";
            if (!mod.CanApply(item, out string reason)) return "Unavailable: " + reason;
            var rows = new List<string>(); var armor = item.GetPart<ArmorPart>();
            void Row(string name, int before, int delta) { if (delta != 0) rows.Add(name + ": " + before + " -> " + (before + delta)); }
            switch (mod)
            {
                case SharpTinkerModification _: Row("Penetration", item.GetPart<MeleeWeaponPart>().PenBonus, SharpTinkerModification.PenetrationBonus); break;
                case ReinforcedPlatingTinkerModification _: Row("AV", armor.AV, ReinforcedPlatingTinkerModification.AVDelta); Row("DV", armor.DV, ReinforcedPlatingTinkerModification.DVDelta); break;
                case FlexweaveTinkerModification _: Row("AV", armor.AV, FlexweaveTinkerModification.AVDelta); Row("DV", armor.DV, FlexweaveTinkerModification.DVDelta); break;
                case HardenedShellTinkerModification _: Row("AV", armor.AV, HardenedShellTinkerModification.AVDelta); Row("Speed penalty", armor.SpeedPenalty, HardenedShellTinkerModification.SpeedPenaltyDelta); break;
                case DuelistCutTinkerModification _:
                    Row("AV", armor.AV, DuelistCutTinkerModification.AVDelta);
                    int agility = 0;
                    foreach (string pair in (item.GetPart<EquippablePart>()?.EquipBonuses ?? "").Split(','))
                    { var entry = pair.Trim(); int split = entry.IndexOf(':'); if (split > 0 && string.Equals(entry.Substring(0, split).Trim(), "Agility", StringComparison.OrdinalIgnoreCase) && int.TryParse(entry.Substring(split + 1), out int amount)) agility += amount; }
                    Row("Equipped Agility contribution", agility, DuelistCutTinkerModification.BonusAmount); break;
            }
            return string.Join("\n", rows) + "\nItem contributions only. Application rechecks costs and eligibility.";
        }
    }
}
