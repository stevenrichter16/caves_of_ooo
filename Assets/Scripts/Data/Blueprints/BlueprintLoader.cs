using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CavesOfOoo.Data
{
    /// <summary>
    /// Loads blueprint definitions from JSON files and resolves inheritance.
    /// Uses Unity's JsonUtility-compatible format via a simple JSON parser.
    /// 
    /// JSON format:
    /// {
    ///   "Objects": [
    ///     {
    ///       "Name": "Creature",
    ///       "Parts": { "Render": { "RenderString": "?" } },
    ///       "Stats": { "Hitpoints": { "Value": 1 } },
    ///       "Tags": { "Creature": "" }
    ///     },
    ///     {
    ///       "Name": "Snapjaw",
    ///       "Inherits": "Creature",
    ///       "Parts": { "Render": { "DisplayName": "snapjaw", "RenderString": "s", "ColorString": "&amp;w" } },
    ///       "Stats": { "Hitpoints": { "Value": 15 }, "Strength": { "Value": 16 } }
    ///     }
    ///   ]
    /// }
    /// </summary>
    public static class BlueprintLoader
    {
        /// <summary>
        /// Load all blueprints from a JSON string, resolve inheritance, return by name.
        /// </summary>
        public static Dictionary<string, Blueprint> LoadFromJson(string json)
        {
            var data = JsonUtility.FromJson<BlueprintFileData>(json);
            var blueprints = new Dictionary<string, Blueprint>();

            if (data?.Objects == null)
                return blueprints;

            // First pass: parse all blueprints
            foreach (var obj in data.Objects)
            {
                var bp = new Blueprint();
                bp.Name = obj.Name;
                bp.Inherits = obj.Inherits;

                // Parts
                if (obj.Parts != null)
                {
                    foreach (var part in obj.Parts)
                    {
                        var paramDict = new Dictionary<string, string>();
                        if (part.Params != null)
                        {
                            foreach (var p in part.Params)
                                paramDict[p.Key] = p.Value;
                        }
                        bp.Parts[part.Name] = paramDict;
                    }
                }

                // Stats
                if (obj.Stats != null)
                {
                    foreach (var stat in obj.Stats)
                    {
                        bp.Stats[stat.Name] = new StatBlueprint
                        {
                            Name = stat.Name,
                            Value = stat.Value,
                            Min = stat.Min,
                            Max = stat.Max,
                            Boost = stat.Boost,
                            sValue = stat.sValue ?? ""
                        };
                    }
                }

                // Tags
                if (obj.Tags != null)
                {
                    foreach (var tag in obj.Tags)
                        bp.Tags[tag.Key] = tag.Value;
                }

                // Props
                if (obj.Props != null)
                {
                    foreach (var prop in obj.Props)
                        bp.Props[prop.Key] = prop.Value;
                }

                // IntProps
                if (obj.IntProps != null)
                {
                    foreach (var prop in obj.IntProps)
                        bp.IntProps[prop.Key] = prop.Value;
                }

                blueprints[bp.Name] = bp;
            }

            // Second pass: resolve inheritance
            foreach (var bp in blueprints.Values)
            {
                Bake(bp, blueprints);
            }

            return blueprints;
        }

        /// <summary>
        /// Resolve inheritance for a single blueprint. Recurses up the chain.
        /// Child values override parent values (parts are merged, not replaced wholesale).
        /// </summary>
        private static void Bake(Blueprint bp, Dictionary<string, Blueprint> all)
        {
            if (bp.Baked) return;
            bp.Baked = true;

            if (string.IsNullOrEmpty(bp.Inherits)) return;

            if (!all.TryGetValue(bp.Inherits, out Blueprint parent))
            {
                Debug.LogWarning($"Blueprint '{bp.Name}' inherits from unknown '{bp.Inherits}'");
                return;
            }

            // Ensure parent is baked first
            Bake(parent, all);
            bp.Parent = parent;

            // Inherit parts: parent parts first, then child overrides
            var mergedParts = new Dictionary<string, Dictionary<string, string>>();
            foreach (var kvp in parent.Parts)
            {
                mergedParts[kvp.Key] = new Dictionary<string, string>(kvp.Value);
            }
            foreach (var kvp in bp.Parts)
            {
                if (mergedParts.TryGetValue(kvp.Key, out var existing))
                {
                    // Merge: child params override parent params
                    foreach (var param in kvp.Value)
                        existing[param.Key] = param.Value;
                }
                else
                {
                    mergedParts[kvp.Key] = new Dictionary<string, string>(kvp.Value);
                }
            }
            bp.Parts = mergedParts;

            // Inherit stats: parent stats, then child overrides
            var mergedStats = new Dictionary<string, StatBlueprint>();
            foreach (var kvp in parent.Stats)
            {
                mergedStats[kvp.Key] = new StatBlueprint
                {
                    Name = kvp.Value.Name,
                    Value = kvp.Value.Value,
                    Min = kvp.Value.Min,
                    Max = kvp.Value.Max,
                    Boost = kvp.Value.Boost,
                    sValue = kvp.Value.sValue
                };
            }
            foreach (var kvp in bp.Stats)
            {
                mergedStats[kvp.Key] = kvp.Value;
            }
            bp.Stats = mergedStats;

            // Inherit tags
            var mergedTags = new Dictionary<string, string>(parent.Tags);
            foreach (var kvp in bp.Tags)
                mergedTags[kvp.Key] = kvp.Value;
            bp.Tags = mergedTags;

            // Inherit props
            var mergedProps = new Dictionary<string, string>(parent.Props);
            foreach (var kvp in bp.Props)
                mergedProps[kvp.Key] = kvp.Value;
            bp.Props = mergedProps;

            // Inherit int props
            var mergedIntProps = new Dictionary<string, int>(parent.IntProps);
            foreach (var kvp in bp.IntProps)
                mergedIntProps[kvp.Key] = kvp.Value;
            bp.IntProps = mergedIntProps;
        }

        /// <summary>
        /// Load from a TextAsset (for Unity Resources.Load workflow).
        /// </summary>
        public static Dictionary<string, Blueprint> LoadFromTextAsset(TextAsset asset)
        {
            return LoadFromJson(asset.text);
        }
    }

    // --- JSON Serialization Data Classes ---
    // These mirror the JSON structure for JsonUtility deserialization.

    [Serializable]
    public class BlueprintFileData
    {
        public List<BlueprintObjectData> Objects;
    }

    [Serializable]
    public class BlueprintObjectData
    {
        public string Name;
        public string Inherits;
        public List<BlueprintPartData> Parts;
        public List<BlueprintStatData> Stats;
        public List<BlueprintKVP> Tags;
        public List<BlueprintKVP> Props;
        public List<BlueprintIntKVP> IntProps;
    }

    [Serializable]
    public class BlueprintPartData
    {
        public string Name;
        public List<BlueprintKVP> Params;
    }

    [Serializable]
    public class BlueprintStatData
    {
        public string Name;
        public int Value;
        public int Min;
        public int Max = 999;
        public int Boost;
        public string sValue;
    }

    [Serializable]
    public class BlueprintKVP
    {
        public string Key;
        public string Value;
    }

    [Serializable]
    public class BlueprintIntKVP
    {
        public string Key;
        public int Value;
    }
}
