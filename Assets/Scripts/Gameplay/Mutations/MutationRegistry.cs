using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Registry of mutation categories and mutation metadata.
    /// Backed by Resources/Content/Blueprints/Mutations.json with sensible fallbacks.
    /// </summary>
    public static class MutationRegistry
    {
        [Serializable]
        private class MutationRegistryFileData
        {
            public List<MutationCategoryDefinition> Categories;
            public List<MutationDefinition> Mutations;
        }

        private static bool _initialized;
        private static readonly Dictionary<string, MutationCategoryDefinition> _categoriesByName =
            new Dictionary<string, MutationCategoryDefinition>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, MutationDefinition> _mutationsByClass =
            new Dictionary<string, MutationDefinition>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, MutationDefinition> _mutationsByName =
            new Dictionary<string, MutationDefinition>(StringComparer.OrdinalIgnoreCase);

        public static bool IsInitialized => _initialized;

        public static void EnsureInitialized()
        {
            if (_initialized)
                return;

            _initialized = true;
            RegisterFallbackCategories();

            TextAsset asset = Resources.Load<TextAsset>("Content/Blueprints/Mutations");
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                return;

            try
            {
                LoadFromJson(asset.text, clearExistingMutations: true);
            }
            catch (Exception ex)
            {
                Debug.LogError("MutationRegistry: Failed to parse Content/Blueprints/Mutations.json: " + ex.Message);
            }
        }

        /// <summary>
        /// Allows tests/tools to provide data without going through Unity Resources.
        /// </summary>
        public static void InitializeFromJson(string json)
        {
            _initialized = true;
            _categoriesByName.Clear();
            _mutationsByClass.Clear();
            _mutationsByName.Clear();
            RegisterFallbackCategories();
            LoadFromJson(json, clearExistingMutations: true);
        }

        /// <summary>
        /// Test helper: drop cached state so next query re-initializes.
        /// </summary>
        public static void ResetForTests()
        {
            _initialized = false;
            _categoriesByName.Clear();
            _mutationsByClass.Clear();
            _mutationsByName.Clear();
        }

        private static void LoadFromJson(string json, bool clearExistingMutations)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;

            MutationRegistryFileData data = JsonUtility.FromJson<MutationRegistryFileData>(json);
            if (data == null)
                return;

            if (data.Categories != null)
            {
                for (int i = 0; i < data.Categories.Count; i++)
                {
                    MutationCategoryDefinition category = data.Categories[i];
                    if (category == null || string.IsNullOrWhiteSpace(category.Name))
                        continue;
                    _categoriesByName[category.Name] = category;
                }
            }

            if (clearExistingMutations)
            {
                _mutationsByClass.Clear();
                _mutationsByName.Clear();
            }

            if (data.Mutations != null)
            {
                for (int i = 0; i < data.Mutations.Count; i++)
                {
                    MutationDefinition mutation = data.Mutations[i];
                    if (mutation == null || string.IsNullOrWhiteSpace(mutation.ClassName))
                        continue;

                    _mutationsByClass[mutation.ClassName] = mutation;
                    if (!string.IsNullOrWhiteSpace(mutation.Name))
                        _mutationsByName[mutation.Name] = mutation;
                }
            }
        }

        private static void RegisterFallbackCategories()
        {
            RegisterCategory("Physical", "Physical");
            RegisterCategory("Mental", "Mental");
            RegisterCategory("PhysicalDefects", "Physical Defects");
            RegisterCategory("MentalDefects", "Mental Defects");
            RegisterCategory("Morphotypes", "Morphotypes");
        }

        private static void RegisterCategory(string name, string displayName)
        {
            _categoriesByName[name] = new MutationCategoryDefinition
            {
                Name = name,
                DisplayName = displayName
            };
        }

        public static bool TryGetByClassName(string className, out MutationDefinition definition)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(className))
            {
                definition = null;
                return false;
            }
            return _mutationsByClass.TryGetValue(className, out definition);
        }

        public static bool TryGetByName(string name, out MutationDefinition definition)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(name))
            {
                definition = null;
                return false;
            }
            return _mutationsByName.TryGetValue(name, out definition);
        }

        public static bool TryGetCategory(string categoryName, out MutationCategoryDefinition category)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                category = null;
                return false;
            }
            return _categoriesByName.TryGetValue(categoryName, out category);
        }

        public static IEnumerable<MutationDefinition> GetAllDefinitions()
        {
            EnsureInitialized();
            return _mutationsByClass.Values;
        }

        public static int GetMaxLevelForClass(string className, int fallback = 10)
        {
            if (TryGetByClassName(className, out MutationDefinition definition) && definition.MaxLevel > 0)
                return definition.MaxLevel;
            return fallback;
        }

        public static string GetStatForClass(string className)
        {
            if (TryGetByClassName(className, out MutationDefinition definition))
            {
                if (!string.IsNullOrWhiteSpace(definition.Stat))
                    return definition.Stat;
                if (TryGetCategory(definition.Category, out MutationCategoryDefinition category))
                    return category.Stat;
            }
            return null;
        }

        public static string GetCategoryModifierPropertyForClass(string className)
        {
            if (TryGetByClassName(className, out MutationDefinition definition) &&
                TryGetCategory(definition.Category, out MutationCategoryDefinition category))
            {
                return category.GetCategoryModifierPropertyName();
            }
            return null;
        }
    }
}
