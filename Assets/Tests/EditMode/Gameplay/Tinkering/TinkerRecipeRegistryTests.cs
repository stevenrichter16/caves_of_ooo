using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class TinkerRecipeRegistryTests
    {
        [SetUp]
        public void Setup()
        {
            TinkerRecipeRegistry.ResetForTests();
        }

        [Test]
        public void EnsureInitialized_HasBuildRecipeForEachCraftableMeleeWeaponBlueprint()
        {
            TextAsset blueprintAsset = Resources.Load<TextAsset>("Content/Blueprints/Objects");
            Assert.NotNull(blueprintAsset, "Content/Blueprints/Objects.json must exist in Resources.");

            Dictionary<string, Blueprint> blueprints = BlueprintLoader.LoadFromJson(blueprintAsset.text);
            Assert.NotNull(blueprints);
            Assert.IsTrue(blueprints.Count > 0);

            TinkerRecipeRegistry.EnsureInitialized();

            var missingBlueprints = new List<string>();
            foreach (Blueprint blueprint in blueprints.Values)
            {
                if (!IsCraftableMeleeWeaponBlueprint(blueprint))
                    continue;

                if (!HasBuildRecipeForBlueprint(blueprint.Name))
                    missingBlueprints.Add(blueprint.Name);
            }

            Assert.IsEmpty(
                missingBlueprints,
                "Missing build recipes for melee blueprints: " + string.Join(", ", missingBlueprints));
        }

        [Test]
        public void EnsureInitialized_DoesNotOverwriteExplicitJsonRecipe()
        {
            TinkerRecipeRegistry.EnsureInitialized();

            Assert.IsTrue(TinkerRecipeRegistry.TryGetRecipe("craft_dagger", out TinkerRecipe recipe));
            Assert.NotNull(recipe);
            Assert.AreEqual("Dagger", recipe.Blueprint);
            Assert.AreEqual("BC", recipe.Cost);
        }

        private static bool HasBuildRecipeForBlueprint(string blueprintName)
        {
            foreach (TinkerRecipe recipe in TinkerRecipeRegistry.GetAllRecipes())
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

        private static bool IsCraftableMeleeWeaponBlueprint(Blueprint blueprint)
        {
            if (blueprint == null || string.IsNullOrWhiteSpace(blueprint.Name))
                return false;

            if (string.Equals(blueprint.Name, "MeleeWeapon", StringComparison.OrdinalIgnoreCase))
                return false;

            if (blueprint.Parts == null || !blueprint.Parts.ContainsKey("MeleeWeapon"))
                return false;

            if (TryGetPartParam(blueprint, "Physics", "Takeable", out string takeableRaw)
                && bool.TryParse(takeableRaw, out bool takeable)
                && !takeable)
            {
                return false;
            }

            return true;
        }

        private static bool TryGetPartParam(Blueprint blueprint, string partName, string paramName, out string value)
        {
            value = null;
            if (blueprint?.Parts == null)
                return false;

            if (!blueprint.Parts.TryGetValue(partName, out Dictionary<string, string> parameters) || parameters == null)
                return false;

            return parameters.TryGetValue(paramName, out value);
        }
    }
}
