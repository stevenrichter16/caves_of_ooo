using System;
using System.Collections.Generic;
using System.Globalization;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Read-only item mechanics for both world Examine and inventory popups.
    /// These are this item's contributions, not the inspecting actor's final
    /// combat totals. Name, flavor, enhancements and afflictions belong to
    /// ExaminablePart and are intentionally absent from this detail block.
    /// </summary>
    public static class ItemExamineService
    {
        /// <summary>
        /// Describe the live fields of an Item-tagged weapon, armor or tonic.
        /// False means no supported details; callers preserve ordinary Examine.
        /// Previews construct effects without applying them or using game RNG.
        /// </summary>
        public static bool TryDescribeDetails(Entity item, out string details)
        {
            details = null;
            if (item == null || !item.HasTag("Item")) return false;

            var lines = new List<string>();
            var weapon = item.GetPart<MeleeWeaponPart>();
            var armor = item.GetPart<ArmorPart>();
            if (weapon != null) DescribeWeapon(weapon, lines);
            if (armor != null)
            {
                lines.Add("Armor contribution - AV: " + Signed(armor.AV)
                    + "; DV: " + Signed(armor.DV) + ".");
                if (armor.SpeedPenalty != 0)
                    lines.Add("Speed: " + Signed(-(long)armor.SpeedPenalty) + " while equipped.");
            }

            if (weapon != null || armor != null)
            {
                var equipment = item.GetPart<EquippablePart>();
                if (equipment != null)
                {
                    lines.Add("Equip slots: " + string.Join(",", equipment.GetSlotArray()) + ".");
                    DescribeEquipBonuses(equipment.EquipBonuses, lines);
                }
            }

            if (TonicExamineService.TryDescribeDetails(item, out string tonic))
            {
                if (lines.Count > 0) lines.Add("");
                lines.Add(tonic);
            }
            if (lines.Count == 0) return false;
            details = string.Join("\n", lines);
            return true;
        }

        private static void DescribeWeapon(MeleeWeaponPart weapon, List<string> lines)
        {
            lines.Add("Damage: " + (weapon.BaseDamage ?? "1d2") + " per penetration.");
            lines.Add("Hit bonus: " + Signed(weapon.HitBonus)
                + "; Penetration bonus: " + Signed(weapon.PenBonus) + ".");
            int cap = weapon.MaxStrengthBonus < 0
                ? CombatSystem.LEGACY_UNCAPPED_MAX_STR_BONUS : weapon.MaxStrengthBonus;
            lines.Add("Penetration uses " + (weapon.Stat ?? "Strength")
                + " modifier (cap " + cap.ToString(CultureInfo.InvariantCulture) + ").");
            if (!string.IsNullOrWhiteSpace(weapon.Attributes))
                lines.Add("Attributes: " + weapon.Attributes.Trim() + ".");

            var attempts = new List<string>();
            var damage = new Damage(0);
            damage.AddAttribute("Melee");
            damage.AddAttribute(weapon.Stat ?? "Strength");
            damage.AddAttributes(weapon.Attributes);
            // Match the class dispatcher's predicates, including Cudgel as
            // an alias for Bludgeoning. No target or combat event is created.
            var previewRng = new Random(0);
            if (damage.IsBludgeoningDamage())
                AddAttempt(attempts, OnHitClassEffects.BLUDGEONING_STUN_CHANCE_PERCENT,
                    new StunnedEffect(OnHitClassEffects.BLUDGEONING_STUN_DURATION,
                        OnHitClassEffects.BLUDGEONING_STUN_SAVE_TARGET, previewRng));
            if (damage.HasAttribute("Cutting"))
                AddAttempt(attempts, OnHitClassEffects.CUTTING_BLEED_CHANCE_PERCENT,
                    new BleedingEffect(OnHitClassEffects.CUTTING_BLEED_SAVE_TARGET,
                        OnHitClassEffects.CUTTING_BLEED_DAMAGE_DICE, previewRng));
            if (damage.HasAttribute("Piercing"))
                AddAttempt(attempts, OnHitClassEffects.PIERCING_CONFUSE_CHANCE_PERCENT,
                    new ConfusedEffect(OnHitClassEffects.PIERCING_CONFUSE_DURATION));

            // Parse on inspection rather than mutating the weapon's combat
            // cache. The shared parser preserves combat's invalid-row policy.
            foreach (var spec in OnHitEffectSpec.Parse(weapon.OnHitEffectsRaw))
            {
                Effect effect = OnHitEffectFactory.Create(spec, null, previewRng);
                if (effect == null)
                    attempts.Add("Unknown on-hit effect '" + spec.EffectName + "' has no effect.");
                else AddAttempt(attempts, spec.ChancePercent, effect);
            }
            if (attempts.Count > 0)
            {
                lines.Add("On damaging hits, independent attempts (targets may resist):");
                // A distinct marker avoids confusing these factual attempts
                // with ExaminablePart's existing enhancement bullet contract.
                foreach (string attempt in attempts) lines.Add("  " + attempt);
            }
        }

        private static void AddAttempt(List<string> attempts, int chance, Effect effect)
        {
            // Combat rolls Next(100); authored values above 100 still mean
            // certainty at the roll gate, not an impossible percentage.
            int effectiveChance = Math.Max(0, Math.Min(100, chance));
            attempts.Add(effectiveChance.ToString(CultureInfo.InvariantCulture)
                + "%: " + EffectDescriber.Describe(effect));
        }

        private static void DescribeEquipBonuses(string raw, List<string> lines)
        {
            if (string.IsNullOrEmpty(raw)) return;
            var bonuses = new List<string>();
            foreach (string pair in raw.Split(','))
            {
                string value = pair.Trim();
                int colon = value.IndexOf(':');
                if (colon < 0 || !int.TryParse(value.Substring(colon + 1), out int amount)) continue;
                // Same parsing as EquipBonusUtility. Effects require that
                // the wearer actually has the named stat.
                bonuses.Add(value.Substring(0, colon) + " " + Signed(amount));
            }
            if (bonuses.Count > 0)
                lines.Add("Equip bonuses (if the stat exists): " + string.Join(", ", bonuses) + ".");
        }

        private static string Signed(long value)
            => (value >= 0 ? "+" : "") + value.ToString(CultureInfo.InvariantCulture);
    }
}
