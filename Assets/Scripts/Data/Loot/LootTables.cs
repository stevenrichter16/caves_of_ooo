using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Data
{
    /// <summary>
    /// One row of a loot table. Exactly ONE of <see cref="Blueprint"/> /
    /// <see cref="TableRef"/> must be set (validated at load, not
    /// runtime). Count rolled uniformly in [MinCount, MaxCount].
    /// In independent mode <see cref="Chance"/> gates the entry;
    /// in pick mode <see cref="Weight"/> drives selection and Chance
    /// is ignored.
    /// </summary>
    [Serializable]
    public class LootEntryData
    {
        public string Blueprint;
        public string TableRef;
        public int Weight = 1;
        public int MinCount = 1;
        public int MaxCount = 1;
        public int Chance = 100;
    }

    /// <summary>
    /// A named loot table. Two modes:
    /// independent (default) — every entry rolls its own Chance;
    /// pick (<see cref="PickOne"/> = true) — rolls
    /// [MinPicks, MaxPicks] weighted selections from the entries.
    /// </summary>
    [Serializable]
    public class LootTableData
    {
        public string Name;
        public bool PickOne = false;
        public int MinPicks = 1;
        public int MaxPicks = 1;
        public List<LootEntryData> Entries = new List<LootEntryData>();
    }

    [Serializable]
    public class LootTableCollection
    {
        public List<LootTableData> Tables;
    }

    /// <summary>
    /// BIOME-OVERHAUL A1 — static registry of loot tables loaded from
    /// <c>Resources/Content/Data/Loot/*.json</c>. Loader shape mirrors
    /// <see cref="CavesOfOoo.Core.GasRegistry"/> (Initialize /
    /// InitializeFromJsonSources / ResetForTests / JsonUtility), but the
    /// validation posture mirrors <c>StoryletRegistry</c>: content
    /// problems are surfaced LOUDLY via <see cref="Validate"/> at boot
    /// and pinned by tests — never a silent in-game no-op.
    /// TableRef nesting is allowed to any acyclic depth; cycles are a
    /// validation error and a guarded runtime no-op.
    /// </summary>
    public static class LootTableRegistry
    {
        private static readonly Dictionary<string, LootTableData> _byName =
            new Dictionary<string, LootTableData>();
        private static bool _initialized;

        public static bool IsInitialized => _initialized;
        public static int Count => _byName.Count;

        public static void Initialize(string json)
        {
            _byName.Clear();
            AppendJson(json);
            _initialized = true;
        }

        /// <summary>Merge multiple JSON documents; later wins on name collision.</summary>
        public static void InitializeFromJsonSources(IEnumerable<string> jsonSources)
        {
            _byName.Clear();
            if (jsonSources != null)
            {
                foreach (var json in jsonSources)
                    AppendJson(json);
            }
            _initialized = true;
        }

        private static void AppendJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            LootTableCollection collection;
            try
            {
                collection = JsonUtility.FromJson<LootTableCollection>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LootTableRegistry] Skipping malformed loot JSON: {ex.Message}");
                return;
            }

            if (collection?.Tables == null)
                return;

            for (int i = 0; i < collection.Tables.Count; i++)
            {
                var table = collection.Tables[i];
                if (table == null || string.IsNullOrEmpty(table.Name))
                    continue;
                _byName[table.Name] = table;
            }
        }

        public static LootTableData Get(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            return _byName.TryGetValue(name, out var table) ? table : null;
        }

        /// <summary>
        /// Roll a table into a list of blueprint names. Unknown table →
        /// empty list + warning. Cycles are guarded (skipped + error log)
        /// even though Validate should have rejected them at boot.
        /// </summary>
        public static List<string> Roll(string tableName, System.Random rng)
        {
            var result = new List<string>();
            RollInto(tableName, rng, result, new HashSet<string>());
            return result;
        }

        private static void RollInto(string tableName, System.Random rng,
            List<string> into, HashSet<string> stack)
        {
            var table = Get(tableName);
            if (table == null)
            {
                Debug.LogWarning($"[LootTableRegistry] Unknown loot table '{tableName}'");
                return;
            }
            if (!stack.Add(tableName))
            {
                Debug.LogError($"[LootTableRegistry] TableRef cycle at '{tableName}' — skipping");
                return;
            }

            try
            {
                if (table.PickOne)
                    RollPicks(table, rng, into, stack);
                else
                    RollIndependent(table, rng, into, stack);
            }
            finally
            {
                stack.Remove(tableName);
            }
        }

        private static void RollIndependent(LootTableData table, System.Random rng,
            List<string> into, HashSet<string> stack)
        {
            foreach (var entry in table.Entries)
            {
                if (entry == null) continue;
                if (entry.Chance < 100 && rng.Next(100) >= entry.Chance) continue;
                ResolveEntry(entry, rng, into, stack);
            }
        }

        private static void RollPicks(LootTableData table, System.Random rng,
            List<string> into, HashSet<string> stack)
        {
            int totalWeight = 0;
            foreach (var entry in table.Entries)
                if (entry != null && entry.Weight > 0)
                    totalWeight += entry.Weight;
            if (totalWeight <= 0) return;

            int picks = table.MaxPicks > table.MinPicks
                ? rng.Next(table.MinPicks, table.MaxPicks + 1)
                : table.MinPicks;

            for (int p = 0; p < picks; p++)
            {
                int r = rng.Next(totalWeight);
                foreach (var entry in table.Entries)
                {
                    if (entry == null || entry.Weight <= 0) continue;
                    r -= entry.Weight;
                    if (r < 0)
                    {
                        ResolveEntry(entry, rng, into, stack);
                        break;
                    }
                }
            }
        }

        private static void ResolveEntry(LootEntryData entry, System.Random rng,
            List<string> into, HashSet<string> stack)
        {
            int count = entry.MaxCount > entry.MinCount
                ? rng.Next(entry.MinCount, entry.MaxCount + 1)
                : entry.MinCount;

            for (int i = 0; i < count; i++)
            {
                if (!string.IsNullOrEmpty(entry.TableRef))
                    RollInto(entry.TableRef, rng, into, stack);
                else if (!string.IsNullOrEmpty(entry.Blueprint))
                    into.Add(entry.Blueprint);
            }
        }

        /// <summary>
        /// Structural + referential validation. Returns one message per
        /// problem (empty = clean). Called at boot with the factory's
        /// blueprint checker and pinned by the content-integrity test.
        /// </summary>
        public static List<string> Validate(Func<string, bool> blueprintExists)
        {
            var problems = new List<string>();

            foreach (var kvp in _byName)
            {
                var table = kvp.Value;
                if (table.Entries == null || table.Entries.Count == 0)
                {
                    problems.Add($"table '{table.Name}': no entries");
                    continue;
                }
                if (table.PickOne)
                {
                    bool anyWeight = false;
                    foreach (var e in table.Entries)
                        if (e != null && e.Weight > 0) { anyWeight = true; break; }
                    if (!anyWeight)
                        problems.Add($"table '{table.Name}': pick mode with no positive weights");
                }

                foreach (var entry in table.Entries)
                {
                    if (entry == null) continue;
                    bool hasBlueprint = !string.IsNullOrEmpty(entry.Blueprint);
                    bool hasRef = !string.IsNullOrEmpty(entry.TableRef);
                    if (hasBlueprint == hasRef)
                    {
                        problems.Add($"table '{table.Name}': entry must set exactly one of Blueprint/TableRef" +
                            (hasBlueprint ? $" (has both: '{entry.Blueprint}'/'{entry.TableRef}')" : " (has neither)"));
                        continue;
                    }
                    if (hasBlueprint && blueprintExists != null && !blueprintExists(entry.Blueprint))
                        problems.Add($"table '{table.Name}': unknown blueprint '{entry.Blueprint}'");
                    if (hasRef && !_byName.ContainsKey(entry.TableRef))
                        problems.Add($"table '{table.Name}': unknown TableRef '{entry.TableRef}'");
                    if (entry.MaxCount < entry.MinCount)
                        problems.Add($"table '{table.Name}': MaxCount < MinCount on '{entry.Blueprint}{entry.TableRef}'");
                    if (entry.Chance < 0 || entry.Chance > 100)
                        problems.Add($"table '{table.Name}': Chance {entry.Chance} outside [0,100]");
                }
            }

            DetectCycles(problems);
            return problems;
        }

        private static void DetectCycles(List<string> problems)
        {
            var visited = new HashSet<string>();
            foreach (var name in _byName.Keys)
            {
                var stack = new HashSet<string>();
                if (WalkForCycle(name, stack, visited, out string cycleAt))
                {
                    problems.Add($"table '{cycleAt}': TableRef cycle detected");
                    return; // one report is enough to fail the gate
                }
            }
        }

        private static bool WalkForCycle(string name, HashSet<string> stack,
            HashSet<string> done, out string cycleAt)
        {
            cycleAt = null;
            if (done.Contains(name)) return false;
            if (!stack.Add(name)) { cycleAt = name; return true; }
            if (_byName.TryGetValue(name, out var table) && table.Entries != null)
            {
                foreach (var entry in table.Entries)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.TableRef)) continue;
                    if (!_byName.ContainsKey(entry.TableRef)) continue;
                    if (WalkForCycle(entry.TableRef, stack, done, out cycleAt)) return true;
                }
            }
            stack.Remove(name);
            done.Add(name);
            return false;
        }

        /// <summary>TEST ONLY — mirror of GasRegistry.ResetForTests.</summary>
        public static void ResetForTests()
        {
            _byName.Clear();
            _initialized = false;
        }
    }
}
