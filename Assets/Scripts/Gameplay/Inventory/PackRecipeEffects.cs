using System;
using System.Collections.Generic;
using System.Globalization;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>Read the authored product's effects without creating an item or running its events.</summary>
    internal static class PackRecipeEffects
    {
        internal static (string Details, IReadOnlyList<string> Highlights) FromBlueprint(EntityFactory factory, string product)
        {
            if (string.IsNullOrEmpty(product) || factory?.Blueprints.TryGetValue(product, out var blueprint) != true)
                return default;

            var lines = new List<string>();
            var highlights = new List<string>();
            if (blueprint.Parts.TryGetValue("WeaponComponent", out var component))
            {
                if (WeaponForgingService.IsBladeSlot(Read(component, "Slot")))
                {
                    string damage = Read(component, "BaseDamage");
                    if (string.IsNullOrWhiteSpace(damage)) damage = "1d2";
                    lines.Add("Weapon damage: " + damage + " per penetration.");
                    highlights.Add(damage + " weapon damage");
                }
                AddBonus(lines, highlights, "Hit bonus", "hit", Number(component, "HitBonus"));
                AddBonus(lines, highlights, "Penetration bonus", "penetration", Number(component, "PenBonus"));
                int cap = Number(component, "MaxStrengthBonus", -1);
                if (cap >= 0)
                {
                    lines.Add("Weapon Strength cap: " + cap + " from this component.");
                    highlights.Add("Strength cap " + cap);
                }
                var previewRandom = new Random(0);
                foreach (var spec in OnHitEffectSpec.Parse(Read(component, "OnHitEffectSpec")))
                {
                    var effect = OnHitEffectFactory.Create(spec, null, previewRandom);
                    if (effect == null) continue;
                    int chance = Math.Clamp(spec.ChancePercent, 0, 100);
                    lines.Add("On damaging hits, " + chance + "% chance. " + EffectDescriber.Describe(effect));
                    string label = effect is BleedingEffect ? "bleed" : effect.DisplayName;
                    highlights.Add(chance + "% " + label + " on damaging hit");
                }
            }
            if (blueprint.Parts.TryGetValue("Armor", out var armor))
            {
                AddBonus(lines, highlights, "Armor value", "AV", Number(armor, "AV"));
                AddBonus(lines, highlights, "Dodge value", "DV", Number(armor, "DV"));
            }
            if (blueprint.Parts.TryGetValue("CureTonic", out var cure) && Read(cure, "CureEffect") == nameof(BleedingEffect))
            {
                lines.Add("Stops bleeding when applied. Restores no health.");
                highlights.Add("Stops bleeding");
            }
            if (blueprint.Parts.TryGetValue("Reagent", out var reagent) && Read(reagent, "FlavorText") is { Length: > 0 } properties)
                lines.Add(properties);
            if (blueprint.Parts.TryGetValue("Food", out var food))
            {
                if (Read(food, "Healing") is { Length: > 0 } healing && healing != "0")
                {
                    highlights.Add("Heals " + healing + " HP");
                    lines.Add("Heals " + healing + " HP when eaten.");
                }
                string stat = Read(food, "MealStat");
                int bonus = Number(food, "MealBonus");
                int duration = Number(food, "MealDuration", 100);
                string meal = FoodPart.DescribeMeal(stat, bonus, duration);
                if (!string.IsNullOrEmpty(meal))
                {
                    lines.Add(meal);
                    string label = PreparedMealEffect.Label(stat).Replace(" resistance", " resist", StringComparison.Ordinal);
                    highlights.Add("+" + bonus + " " + label + " (" + duration + " turns)");
                }
            }
            return (lines.Count == 0 ? null : string.Join("\n", lines), highlights.Count == 0 ? null : highlights);
        }

        private static void AddBonus(List<string> lines, List<string> highlights, string name, string shortName, int bonus)
        {
            if (bonus == 0) return;
            string amount = bonus.ToString("+0;-0", CultureInfo.InvariantCulture);
            lines.Add(name + ": " + amount + ".");
            highlights.Add(amount + " " + shortName);
        }

        private static string Read(Dictionary<string, string> fields, string key)
            => fields.TryGetValue(key, out var value) ? value : "";

        private static int Number(Dictionary<string, string> fields, string key, int fallback = 0)
            => int.TryParse(Read(fields, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;
    }
}
