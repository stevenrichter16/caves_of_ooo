using System;

namespace CavesOfOoo.Core
{
    // Restore only fields these two transformations write, on the original Part/Stat
    // instances. Equipment, enhancement occurrences and unrelated modifiers stay intact.
    internal static class WeaponPayloadUndo
    {
        internal static Action Capture(Entity weapon)
        {
            var melee = weapon.GetPart<MeleeWeaponPart>();
            string damage = melee?.BaseDamage, attributes = melee?.Attributes, effects = melee?.OnHitEffectsRaw;
            int pen = melee?.PenBonus ?? 0, hit = melee?.HitBonus ?? 0, strength = melee?.MaxStrengthBonus ?? 0;
            var assembly = weapon.GetPart<WeaponAssemblyPart>();
            string blade = assembly?.BladeBlueprint, haft = assembly?.HaftBlueprint, binding = assembly?.BindingBlueprint;
            var temper = weapon.GetPart<WeaponTemperPart>();
            int count = temper?.TemperCount ?? 0, penalty = temper?.HpPenaltyTotal ?? 0;
            string specs = temper?.AppliedSpecsRaw;
            var render = weapon.GetPart<RenderPart>(); string name = render?.DisplayName;
            var hp = weapon.GetStat("Hitpoints"); int max = hp?.Max ?? 0, value = hp?.BaseValue ?? 0;
            return () =>
            {
                if (melee != null)
                {
                    melee.BaseDamage = damage; melee.PenBonus = pen; melee.HitBonus = hit;
                    melee.MaxStrengthBonus = strength; melee.Attributes = attributes; melee.OnHitEffectsRaw = effects;
                }
                if (assembly != null)
                { assembly.BladeBlueprint = blade; assembly.HaftBlueprint = haft; assembly.BindingBlueprint = binding; }
                if (temper != null) { temper.TemperCount = count; temper.HpPenaltyTotal = penalty; temper.AppliedSpecsRaw = specs; }
                if (render != null) render.DisplayName = name;
                if (hp != null) { hp.Max = max; hp.BaseValue = value; }
            };
        }
    }
}
