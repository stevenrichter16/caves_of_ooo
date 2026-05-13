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

        [TearDown]
        public void TearDown()
        {
            // Defensive: leave registry in a clean-but-uninitialized state
            // so the next caller (test or live Play) gets a fresh auto-load
            // from the production JSON.
            TinkerRecipeRegistry.ResetForTests();
        }

        // ── Regression pin against test-state pollution ───────────

        [Test]
        public void Production_Registry_HasNoTestOnlyRecipeIds()
        {
            // E.5.5 regression pin: the Tinker registry must NEVER serve
            // recipes that only exist in test-fixture embedded JSON.
            //
            // Bug history: TinkeringServiceTests + TinkeringCommandTests
            // call InitializeFromJson(TestRecipesJson) in their Setup,
            // which loads test-only recipes
            // (craft_thorn_dagger / craft_plain_knife / craft_torch_from_scrap).
            // Both fixtures previously lacked a TearDown that called
            // ResetForTests — so after they ran, the registry was left
            // polluted with those test recipes. A live Play session
            // querying the registry next saw a sparse 8-recipe set
            // (test recipes + only the mod recipes shared between test
            // and production) instead of the full ~50 production
            // recipes — including the 3 mineral mods MISSING entirely
            // because they're only in the production JSON.
            //
            // This test catches the regression by forcing a clean
            // auto-load + asserting the test-only IDs are NOT present.
            TinkerRecipeRegistry.ResetForTests();
            TinkerRecipeRegistry.EnsureInitialized();

            string[] testOnlyIds =
            {
                "craft_thorn_dagger",
                "craft_torch_from_scrap",
                "craft_plain_knife",
            };
            foreach (var id in testOnlyIds)
            {
                Assert.IsFalse(TinkerRecipeRegistry.TryGetRecipe(id, out _),
                    $"Test-only recipe '{id}' must NOT exist in the " +
                    "production registry. If this fails, either " +
                    "(a) the production Recipes_V1.json accidentally " +
                    "gained this entry, or (b) a previous test fixture " +
                    "polluted the registry and forgot to clean up — " +
                    "check Setup/TearDown symmetry on TinkeringServiceTests " +
                    "+ TinkeringCommandTests.");
            }

            // Positive sibling: the 3 mineral mods MUST exist after a
            // clean production auto-load. If they're missing, the bug
            // is the inverse — production JSON was overwritten or
            // truncated.
            Assert.IsTrue(TinkerRecipeRegistry.TryGetRecipe("mod_palesalt_infuse", out _));
            Assert.IsTrue(TinkerRecipeRegistry.TryGetRecipe("mod_choiriron_infuse", out _));
            Assert.IsTrue(TinkerRecipeRegistry.TryGetRecipe("mod_glowquartz_infuse", out _));
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
        public void EnsureInitialized_HasBuildRecipeForEachCraftableArmorBlueprint()
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
                if (!IsCraftableArmorBlueprint(blueprint))
                    continue;

                if (!HasBuildRecipeForBlueprint(blueprint.Name))
                    missingBlueprints.Add(blueprint.Name);
            }

            Assert.IsEmpty(
                missingBlueprints,
                "Missing build recipes for armor blueprints: " + string.Join(", ", missingBlueprints));
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
                && !ParseBoolOrDefault(takeableRaw, defaultValue: true))
            {
                return false;
            }

            return true;
        }

        private static bool IsCraftableArmorBlueprint(Blueprint blueprint)
        {
            if (blueprint == null || string.IsNullOrWhiteSpace(blueprint.Name))
                return false;

            if (string.Equals(blueprint.Name, "ArmorItem", StringComparison.OrdinalIgnoreCase))
                return false;

            if (blueprint.Parts == null
                || !blueprint.Parts.ContainsKey("Armor")
                || !blueprint.Parts.ContainsKey("Equippable"))
            {
                return false;
            }

            if (TryGetPartParam(blueprint, "Physics", "Takeable", out string takeableRaw)
                && !ParseBoolOrDefault(takeableRaw, defaultValue: true))
            {
                return false;
            }

            bool hasArmorNumbers =
                HasNumericPartParam(blueprint, "Armor", "AV")
                || HasNumericPartParam(blueprint, "Armor", "DV")
                || HasNumericPartParam(blueprint, "Armor", "SpeedPenalty");
            if (!hasArmorNumbers)
                return false;

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

        private static bool HasNumericPartParam(Blueprint blueprint, string partName, string paramName)
        {
            if (!TryGetPartParam(blueprint, partName, paramName, out string raw))
                return false;

            if (string.IsNullOrWhiteSpace(raw))
                return false;

            return int.TryParse(raw, out _);
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
    }
}
