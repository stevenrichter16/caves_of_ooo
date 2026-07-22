using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Runtime registry of loot tables loaded from JSON. Mirrors
    /// TinkerRecipeRegistry / BrewRuleRegistry's init/reset test-hook
    /// convention exactly: EnsureInitialized (lazy, idempotent),
    /// InitializeFromJson (test injection), ResetForTests.
    /// </summary>
    public static class LootTableRegistry
    {
        [Serializable]
        private class LootTableFileData
        {
            public List<LootTable> Tables;
        }

        private static readonly Dictionary<string, LootTable> TablesById =
            new Dictionary<string, LootTable>(StringComparer.OrdinalIgnoreCase);

        private const string ResourcePath = "Content/Data/Loot/LootTables";

        private static bool _initialized;

        public static void EnsureInitialized()
        {
            if (_initialized)
                return;

            _initialized = true;

            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
            {
                Debug.LogWarning("LootTableRegistry: no loot-table file found at '" + ResourcePath + "'.");
                return;
            }

            try
            {
                LoadFromJson(asset.text, clearExisting: true);
            }
            catch (Exception ex)
            {
                Debug.LogError("LootTableRegistry: failed to parse loot tables from '" + ResourcePath + "': " + ex.Message);
            }
        }

        public static void InitializeFromJson(string json)
        {
            _initialized = true;
            TablesById.Clear();
            LoadFromJson(json, clearExisting: true);
        }

        public static void ResetForTests()
        {
            _initialized = false;
            TablesById.Clear();
        }

        public static bool TryGetTable(string id, out LootTable table)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(id))
            {
                table = null;
                return false;
            }

            return TablesById.TryGetValue(id, out table);
        }

        private static void LoadFromJson(string json, bool clearExisting)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;

            if (clearExisting)
                TablesById.Clear();

            LootTableFileData file = JsonUtility.FromJson<LootTableFileData>(json);
            if (file == null || file.Tables == null)
                return;

            for (int i = 0; i < file.Tables.Count; i++)
            {
                LootTable table = file.Tables[i];
                if (table == null || string.IsNullOrWhiteSpace(table.ID))
                    continue;

                TablesById[table.ID] = table;
            }
        }
    }
}
