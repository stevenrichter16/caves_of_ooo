using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Runtime registry of brew-resolution rules loaded from JSON.
    /// Mirrors <c>TinkerRecipeRegistry</c>: static cache, lazy auto-load from
    /// Resources, explicit reset/inject hooks for tests.
    /// </summary>
    public static class BrewRuleRegistry
    {
        [Serializable]
        private class BrewRuleFileData
        {
            public List<BrewRule> Rules;
        }

        private static readonly Dictionary<string, BrewRule> RulesById =
            new Dictionary<string, BrewRule>(StringComparer.OrdinalIgnoreCase);

        // Insertion-ordered list so resolution iterates rules deterministically
        // regardless of dictionary bucket order.
        private static readonly List<BrewRule> RulesInOrder = new List<BrewRule>();

        private const string ResourcePath = "Content/Data/Alchemy/BrewRules";

        private static bool _initialized;

        public static void EnsureInitialized()
        {
            if (_initialized)
                return;

            _initialized = true;

            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
            {
                Debug.LogWarning("BrewRuleRegistry: no brew-rule file found at '" + ResourcePath + "'.");
                return;
            }

            try
            {
                LoadFromJson(asset.text, clearExisting: true);
            }
            catch (Exception ex)
            {
                Debug.LogError("BrewRuleRegistry: failed to parse brew rules from '" + ResourcePath + "': " + ex.Message);
            }
        }

        public static void InitializeFromJson(string json)
        {
            _initialized = true;
            LoadFromJson(json, clearExisting: true);
        }

        public static void ResetForTests()
        {
            _initialized = false;
            RulesById.Clear();
            RulesInOrder.Clear();
        }

        public static bool TryGetRule(string id, out BrewRule rule)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(id))
            {
                rule = null;
                return false;
            }

            return RulesById.TryGetValue(id, out rule);
        }

        public static IReadOnlyList<BrewRule> GetAllRules()
        {
            EnsureInitialized();
            return RulesInOrder;
        }

        private static void LoadFromJson(string json, bool clearExisting)
        {
            if (clearExisting)
            {
                RulesById.Clear();
                RulesInOrder.Clear();
            }

            if (string.IsNullOrWhiteSpace(json))
                return;

            BrewRuleFileData file = JsonUtility.FromJson<BrewRuleFileData>(json);
            if (file == null || file.Rules == null)
                return;

            for (int i = 0; i < file.Rules.Count; i++)
            {
                BrewRule rule = file.Rules[i];
                if (rule == null || string.IsNullOrWhiteSpace(rule.ID))
                    continue;

                if (!RulesById.ContainsKey(rule.ID))
                    RulesInOrder.Add(rule);

                RulesById[rule.ID] = rule;
            }
        }
    }
}
