using System;
using System.Collections.Generic;
using System.Globalization;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>Read the authored product's effects without creating an item or running its events.</summary>
    internal static class PackRecipeEffects
    {
        internal static string FromBlueprint(EntityFactory factory, string product)
        {
            if (string.IsNullOrEmpty(product) || factory?.Blueprints.TryGetValue(product, out var blueprint) != true)
                return null;

            var lines = new List<string>();
            if (blueprint.Parts.TryGetValue("WeaponComponent", out var component))
            {
                if (Read(component, "Slot") == "Blade" && Read(component, "BaseDamage") is { Length: > 0 } damage)
                    lines.Add("Weapon damage: " + damage + " per penetration.");
                AddBonus(lines, "Hit bonus", Number(component, "HitBonus"));
                AddBonus(lines, "Penetration bonus", Number(component, "PenBonus"));
                int cap = Number(component, "MaxStrengthBonus", -1);
                if (cap >= 0) lines.Add("Weapon Strength cap: " + cap + " from this component.");
                var previewRandom = new Random(0);
                foreach (var spec in OnHitEffectSpec.Parse(Read(component, "OnHitEffectSpec")))
                {
                    var effect = OnHitEffectFactory.Create(spec, null, previewRandom);
                    if (effect != null)
                        lines.Add("On damaging hits, " + Math.Clamp(spec.ChancePercent, 0, 100) + "% chance. "
                            + EffectDescriber.Describe(effect));
                }
            }
            if (blueprint.Parts.TryGetValue("CureTonic", out var cure) && Read(cure, "CureEffect") == nameof(BleedingEffect))
                lines.Add("Stops bleeding when applied. Restores no health.");
            if (blueprint.Parts.TryGetValue("Reagent", out var reagent) && Read(reagent, "FlavorText") is { Length: > 0 } properties)
                lines.Add(properties);
            if (blueprint.Parts.TryGetValue("Food", out var food))
            {
                if (Read(food, "Healing") is { Length: > 0 } healing) lines.Add("Heals " + healing + " HP when eaten.");
                string meal = FoodPart.DescribeMeal(Read(food, "MealStat"), Number(food, "MealBonus"), Number(food, "MealDuration", 100));
                if (!string.IsNullOrEmpty(meal)) lines.Add(meal);
            }
            return lines.Count == 0 ? null : string.Join("\n", lines);
        }

        private static void AddBonus(List<string> lines, string name, int bonus)
        {
            if (bonus != 0) lines.Add(name + ": " + bonus.ToString("+0;-0", CultureInfo.InvariantCulture) + ".");
        }

        private static string Read(Dictionary<string, string> fields, string key)
            => fields.TryGetValue(key, out var value) ? value : "";

        private static int Number(Dictionary<string, string> fields, string key, int fallback = 0)
            => int.TryParse(Read(fields, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;
    }
}
