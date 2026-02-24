using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CavesOfOoo.Data;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Runtime registry of tinkering recipes loaded from JSON.
    /// Qud-like static cache with test reset hooks.
    /// </summary>
    public static class TinkerRecipeRegistry
    {
        [Serializable]
        private class TinkerRecipeFileData
        {
            public List<TinkerRecipe> Recipes;
        }

        private static readonly Dictionary<string, TinkerRecipe> RecipesById =
            new Dictionary<string, TinkerRecipe>(StringComparer.OrdinalIgnoreCase);

        private static readonly string[] ResourcePaths =
        {
            "Content/Data/Tinkering/Recipes_V1",
            "Content/Data/Tinkering/Recipes"
        };

        private static bool _initialized;

        public static void EnsureInitialized()
        {
            if (_initialized)
                return;

            _initialized = true;

            bool loadedFromJson = false;
            for (int i = 0; i < ResourcePaths.Length; i++)
            {
                TextAsset asset = Resources.Load<TextAsset>(ResourcePaths[i]);
                if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                    continue;

                try
                {
                    LoadFromJson(asset.text, clearExisting: true);
                    loadedFromJson = true;
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError("TinkerRecipeRegistry: failed to parse recipes from '" + ResourcePaths[i] + "': " + ex.Message);
                }
            }

            MergeGeneratedEquipmentBuildRecipes();

            if (!loadedFromJson && RecipesById.Count == 0)
                Debug.LogWarning("TinkerRecipeRegistry: no recipe files found and no generated build recipes were available.");
        }

        public static void InitializeFromJson(string json)
        {
            _initialized = true;
            RecipesById.Clear();
            LoadFromJson(json, clearExisting: true);
        }

        public static void ResetForTests()
        {
            _initialized = false;
            RecipesById.Clear();
        }

        public static bool TryGetRecipe(string id, out TinkerRecipe recipe)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(id))
            {
                recipe = null;
                return false;
            }

            return RecipesById.TryGetValue(id, out recipe);
        }

        public static IEnumerable<TinkerRecipe> GetAllRecipes()
        {
            EnsureInitialized();
            return RecipesById.Values;
        }

        public static bool TryGetBuildRecipeForBlueprint(string blueprintName, out TinkerRecipe recipe)
        {
            EnsureInitialized();
            recipe = null;

            if (string.IsNullOrWhiteSpace(blueprintName))
                return false;

            foreach (TinkerRecipe candidate in RecipesById.Values)
            {
                if (candidate == null)
                    continue;

                if (!string.Equals(candidate.Type, "Build", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.Equals(candidate.Blueprint, blueprintName, StringComparison.OrdinalIgnoreCase))
                    continue;

                recipe = candidate;
                return true;
            }

            return false;
        }

        private static void LoadFromJson(string json, bool clearExisting)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;

            if (clearExisting)
                RecipesById.Clear();

            TinkerRecipeFileData file = JsonUtility.FromJson<TinkerRecipeFileData>(json);
            if (file == null || file.Recipes == null)
                return;

            for (int i = 0; i < file.Recipes.Count; i++)
            {
                TinkerRecipe recipe = file.Recipes[i];
                if (recipe == null || string.IsNullOrWhiteSpace(recipe.ID))
                    continue;

                RecipesById[recipe.ID] = recipe;
            }
        }

        private static void MergeGeneratedEquipmentBuildRecipes()
        {
            TextAsset blueprintsAsset = Resources.Load<TextAsset>("Content/Blueprints/Objects");
            if (blueprintsAsset == null || string.IsNullOrWhiteSpace(blueprintsAsset.text))
                return;

            Dictionary<string, Blueprint> blueprints;
            try
            {
                blueprints = BlueprintLoader.LoadFromJson(blueprintsAsset.text);
            }
            catch (Exception ex)
            {
                Debug.LogError("TinkerRecipeRegistry: failed to load blueprint data for generated build recipes: " + ex.Message);
                return;
            }

            int added = 0;
            foreach (Blueprint blueprint in blueprints.Values)
            {
                if (!IsCraftableBuildBlueprint(blueprint))
                    continue;

                if (HasBuildRecipeForBlueprint(blueprint.Name))
                    continue;

                TinkerRecipe generated = CreateGeneratedBuildRecipe(blueprint);
                if (generated == null || string.IsNullOrWhiteSpace(generated.ID))
                    continue;

                string id = generated.ID;
                int suffix = 2;
                while (RecipesById.ContainsKey(id))
                {
                    id = generated.ID + "_" + suffix;
                    suffix++;
                }

                generated.ID = id;
                RecipesById[id] = generated;
                added++;
            }

            if (added > 0)
                Debug.Log("TinkerRecipeRegistry: generated " + added + " equipment build recipes from blueprints.");
        }

        private static bool IsCraftableBuildBlueprint(Blueprint blueprint)
        {
            return IsCraftableMeleeWeaponBlueprint(blueprint) || IsCraftableArmorBlueprint(blueprint);
        }

        private static bool IsCraftableMeleeWeaponBlueprint(Blueprint blueprint)
        {
            if (blueprint == null || string.IsNullOrWhiteSpace(blueprint.Name))
                return false;

            if (string.Equals(blueprint.Name, "MeleeWeapon", StringComparison.OrdinalIgnoreCase))
                return false;

            if (blueprint.Parts == null || !blueprint.Parts.ContainsKey("MeleeWeapon"))
                return false;

            if (TryGetPartParam(blueprint, "Physics", "Takeable", out string takeable)
                && !ParseBoolOrDefault(takeable, defaultValue: true))
            {
                return false;
            }

            if (TryGetPartParam(blueprint, "TinkerItem", "CanBuild", out string canBuild)
                && !ParseBoolOrDefault(canBuild, defaultValue: true))
            {
                return false;
            }

            return true;
        }

        private static bool IsCraftableArmorBlueprint(Blueprint blueprint)
        {
            if (blueprint == null || string.IsNullOrWhiteSpace(blueprint.Name))
                return false;

            // Skip the template base armor item; generate concrete armor pieces only.
            if (string.Equals(blueprint.Name, "ArmorItem", StringComparison.OrdinalIgnoreCase))
                return false;

            if (blueprint.Parts == null
                || !blueprint.Parts.ContainsKey("Armor")
                || !blueprint.Parts.ContainsKey("Equippable"))
            {
                return false;
            }

            if (TryGetPartParam(blueprint, "Physics", "Takeable", out string takeable)
                && !ParseBoolOrDefault(takeable, defaultValue: true))
            {
                return false;
            }

            if (TryGetPartParam(blueprint, "TinkerItem", "CanBuild", out string canBuild)
                && !ParseBoolOrDefault(canBuild, defaultValue: true))
            {
                return false;
            }

            // Armor template rows with no numeric armor values should not surface as craft recipes.
            bool hasArmorNumbers =
                HasNumericPartParam(blueprint, "Armor", "AV")
                || HasNumericPartParam(blueprint, "Armor", "DV")
                || HasNumericPartParam(blueprint, "Armor", "SpeedPenalty");
            if (!hasArmorNumbers)
                return false;

            return true;
        }

        private static bool HasBuildRecipeForBlueprint(string blueprintName)
        {
            if (string.IsNullOrWhiteSpace(blueprintName))
                return false;

            foreach (TinkerRecipe recipe in RecipesById.Values)
            {
                if (recipe == null)
                    continue;

                if (!string.Equals(recipe.Type, "Build", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.Equals(recipe.Blueprint, blueprintName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static TinkerRecipe CreateGeneratedBuildRecipe(Blueprint blueprint)
        {
            string recipeId = "craft_" + ToSnakeCase(blueprint.Name);
            string displayName = "Craft " + ResolveRecipeDisplayName(blueprint);
            string cost = ResolveRecipeCost(blueprint);

            int numberMade = 1;
            if (TryGetPartParam(blueprint, "TinkerItem", "NumberMade", out string numberMadeRaw)
                && int.TryParse(numberMadeRaw, out int parsedNumberMade)
                && parsedNumberMade > 0)
            {
                numberMade = parsedNumberMade;
            }

            string ingredient = string.Empty;
            if (TryGetPartParam(blueprint, "TinkerItem", "Ingredient", out string ingredientRaw))
                ingredient = ingredientRaw?.Trim() ?? string.Empty;

            return new TinkerRecipe
            {
                ID = recipeId,
                DisplayName = displayName,
                Blueprint = blueprint.Name,
                Type = "Build",
                Cost = string.IsNullOrEmpty(cost) ? "BC" : cost,
                Ingredient = ingredient,
                NumberMade = numberMade
            };
        }

        private static string ResolveRecipeDisplayName(Blueprint blueprint)
        {
            if (TryGetPartParam(blueprint, "Render", "DisplayName", out string displayName)
                && !string.IsNullOrWhiteSpace(displayName))
            {
                return TitleCaseWords(displayName);
            }

            return TitleCaseWords(SplitIdentifierIntoWords(blueprint.Name));
        }

        private static string ResolveRecipeCost(Blueprint blueprint)
        {
            if (TryGetPartParam(blueprint, "TinkerItem", "BuildCost", out string explicitCost))
            {
                string normalized = BitCost.Normalize(explicitCost);
                if (!string.IsNullOrEmpty(normalized))
                    return normalized;
            }

            int tier = 1;
            if (blueprint.Tags != null
                && blueprint.Tags.TryGetValue("Tier", out string tierRaw)
                && int.TryParse(tierRaw, out int parsedTier))
            {
                tier = Math.Max(1, parsedTier);
            }

            int value = 0;
            if (TryGetPartParam(blueprint, "Commerce", "Value", out string valueRaw))
                int.TryParse(valueRaw, out value);

            string cost;
            if (tier <= 1)
                cost = "BC";
            else if (tier == 2)
                cost = "BBC";
            else if (tier == 3)
                cost = "BBCC";
            else
                cost = "BBBCC";

            if (value >= 25)
                cost += "C";
            if (value >= 45)
                cost += "R";
            if (value >= 65)
                cost += "B";

            if (blueprint.Tags != null && blueprint.Tags.ContainsKey("TwoHanded"))
                cost += "B";

            return BitCost.Normalize(cost);
        }

        private static bool TryGetPartParam(Blueprint blueprint, string partName, string paramName, out string value)
        {
            value = null;

            if (blueprint?.Parts == null)
                return false;

            if (!blueprint.Parts.TryGetValue(partName, out Dictionary<string, string> partParams) || partParams == null)
                return false;

            return partParams.TryGetValue(paramName, out value);
        }

        private static bool ParseBoolOrDefault(string value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            if (bool.TryParse(value, out bool parsed))
                return parsed;

            if (value == "1")
                return true;
            if (value == "0")
                return false;

            return defaultValue;
        }

        private static bool HasNumericPartParam(Blueprint blueprint, string partName, string paramName)
        {
            if (!TryGetPartParam(blueprint, partName, paramName, out string raw))
                return false;

            if (string.IsNullOrWhiteSpace(raw))
                return false;

            return int.TryParse(raw, out _);
        }

        private static string ToSnakeCase(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "recipe";

            var builder = new StringBuilder(value.Length + 8);
            bool lastWasUnderscore = false;

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsLetterOrDigit(c))
                {
                    if (char.IsUpper(c) && builder.Length > 0 && !lastWasUnderscore)
                    {
                        char previous = value[i - 1];
                        bool splitBeforeUpper = char.IsLower(previous)
                            || (i + 1 < value.Length && char.IsLower(value[i + 1]));
                        if (splitBeforeUpper)
                        {
                            builder.Append('_');
                            lastWasUnderscore = true;
                        }
                    }

                    builder.Append(char.ToLowerInvariant(c));
                    lastWasUnderscore = false;
                }
                else if (!lastWasUnderscore && builder.Length > 0)
                {
                    builder.Append('_');
                    lastWasUnderscore = true;
                }
            }

            string snake = builder.ToString().Trim('_');
            return string.IsNullOrEmpty(snake) ? "recipe" : snake;
        }

        private static string SplitIdentifierIntoWords(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var builder = new StringBuilder(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (i > 0
                    && char.IsUpper(c)
                    && (char.IsLower(value[i - 1]) || (i + 1 < value.Length && char.IsLower(value[i + 1]))))
                {
                    builder.Append(' ');
                }

                builder.Append(c == '_' || c == '-' ? ' ' : c);
            }

            return builder.ToString();
        }

        private static string TitleCaseWords(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string[] words = value
                .Trim()
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
                return string.Empty;

            string merged = string.Join(" ", words).ToLowerInvariant();
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(merged);
        }
    }
}
