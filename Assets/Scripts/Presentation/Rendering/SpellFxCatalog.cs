using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public enum SpellFxFamily { Projectile, Beam, Chain, GroundLine, Cone, Lob, Ring, Field, Inscription, Ward }
    public enum SpellFxPrimitive { Cast, Charge, Head, Trail, Beam, Wave, Overlay, Sigil, Mark }

    [Serializable]
    public sealed class SpellFxDefinitionFile { public SpellFxDefinition[] Definitions; }

    /// <summary>Presentation timing and original art only; no targeting or combat parameters.</summary>
    [Serializable]
    public sealed class SpellFxDefinition
    {
        public string ID;
        public string School;
        public string Family;
        public string DetailSheet;
        public string ImpactSheet;
        public string FallbackReason;
        public float CastDuration = 0.10f;
        public float ChargeDuration = 0.10f;
        public float StepDuration = 0.028f;
        public float ImpactDuration = 0.20f;
        public float AftermathDuration = 0.15f;
        public bool Domestic;
        public bool ReverseTravel;
        public bool IsRite;
        public int Variant;
        public SpellFxFamily AnimationFamily => Enum.TryParse(Family, out SpellFxFamily result) ? result : SpellFxFamily.Inscription;
        public bool IsExplicitFallback => !string.IsNullOrWhiteSpace(FallbackReason);
        public AsciiFxTheme Theme
        {
            get
            {
                switch (School)
                {
                    case "fire": return AsciiFxTheme.Fire;
                    case "ice": return AsciiFxTheme.Ice;
                    case "lightning": return AsciiFxTheme.Lightning;
                    case "water": return AsciiFxTheme.Water;
                    case "acid": return AsciiFxTheme.Poison;
                    default: return AsciiFxTheme.Arcane;
                }
            }
        }
    }

    /// <summary>
    /// Shared 6-frame flipbooks. Large impacts are cut into nine cell-sized fragments
    /// so fog clips their actual pixels, including the half-cell overhang on each side.
    /// </summary>
    public sealed class SpellFxAsset
    {
        public const int FrameCount = 6;
        private readonly Sprite[,] _detail = new Sprite[9, FrameCount];
        private readonly Sprite[,,] _impact = new Sprite[2, FrameCount, 9];
        public SpellFxAsset(Texture2D detail, Texture2D impact)
        {
            detail.filterMode = FilterMode.Point;
            impact.filterMode = FilterMode.Point;
            for (int row = 0; row < 9; row++)
                for (int frame = 0; frame < FrameCount; frame++)
                    _detail[row, frame] = Slice(detail, new Rect(frame * 16, detail.height - (row + 1) * 16, 16, 16));
            int[] offsets = { 0, 8, 24 };
            int[] sizes = { 8, 16, 8 };
            for (int row = 0; row < 2; row++)
                for (int frame = 0; frame < FrameCount; frame++)
                    for (int y = 0; y < 3; y++)
                        for (int x = 0; x < 3; x++)
                            _impact[row, frame, y * 3 + x] = Slice(impact, new Rect(
                                frame * 32 + offsets[x], impact.height - (row + 1) * 32 + offsets[y], sizes[x], sizes[y]));
        }
        public Sprite Detail(SpellFxPrimitive primitive, int frame) => _detail[(int)primitive, Mathf.Clamp(frame, 0, FrameCount - 1)];
        public Sprite Impact(int frame, int fragment, bool resisted) => _impact[resisted ? 1 : 0, Mathf.Clamp(frame, 0, FrameCount - 1), fragment];
        public void Dispose()
        {
            foreach (Sprite sprite in _detail) DestroySprite(sprite);
            foreach (Sprite sprite in _impact) DestroySprite(sprite);
        }
        private static Sprite Slice(Texture2D texture, Rect rect)
        {
            Sprite result = Sprite.Create(texture, rect, new Vector2(.5f, .5f), 16f, 0, SpriteMeshType.FullRect);
            result.hideFlags = HideFlags.DontSave;
            return result;
        }
        private static void DestroySprite(Sprite sprite)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(sprite);
            else UnityEngine.Object.DestroyImmediate(sprite);
        }
    }

    /// <summary>Uses the same Resources/JSON convention as EntityVisualCatalog, preserving explicit ASCII fallback.</summary>
    public static class SpellFxCatalog
    {
        public const string ResourcePath = "Content/Data/SpellVisuals";
        private static readonly Dictionary<string, SpellFxDefinition> Definitions = new Dictionary<string, SpellFxDefinition>(StringComparer.Ordinal);
        private static readonly Dictionary<string, SpellFxAsset> Assets = new Dictionary<string, SpellFxAsset>(StringComparer.Ordinal);
        private static readonly List<string> Issues = new List<string>();
        private static bool _loaded;
        public static int DefinitionCount { get { EnsureLoaded(); return Definitions.Count; } }
        public static IReadOnlyList<string> ValidationIssues { get { EnsureLoaded(); return Issues; } }
        public static SpellFxDefinition Find(string id) => TryGetDefinition(id, out SpellFxDefinition definition) ? definition : null;
        public static SpellFxDefinition GetOrFallback(string id) => Find(id) ?? new SpellFxDefinition
        { ID = id, School = "binding", Family = "Inscription", FallbackReason = "No registered sprite definition; use resolved ASCII geometry." };
        public static bool TryGetDefinition(string id, out SpellFxDefinition definition)
        {
            EnsureLoaded();
            definition = null;
            return !string.IsNullOrEmpty(id) && Definitions.TryGetValue(id, out definition);
        }
        public static bool TryGetAsset(SpellFxDefinition definition, out SpellFxAsset asset)
        {
            asset = null;
            if (definition == null || definition.IsExplicitFallback) return false;
            string key = definition.DetailSheet + "|" + definition.ImpactSheet;
            if (Assets.TryGetValue(key, out asset)) return asset != null;
            Texture2D detail = LoadTexture(definition.DetailSheet);
            Texture2D impact = LoadTexture(definition.ImpactSheet);
            if (detail == null || impact == null || detail.width != 96 || detail.height != 144 || impact.width != 192 || impact.height != 64)
            {
                Issues.Add(definition.ID + ": missing or invalid spell art; using ASCII fallback.");
                Assets[key] = null;
                return false;
            }
            asset = new SpellFxAsset(detail, impact);
            Assets[key] = asset;
            return true;
        }
        private static Texture2D LoadTexture(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            Texture2D texture = Resources.Load<Texture2D>(path);
            if (texture != null) return texture;
            Sprite importedSprite = Resources.Load<Sprite>(path);
            return importedSprite != null ? importedSprite.texture : null;
        }
        public static List<string> ValidateCoverage(IEnumerable<string> spellIds)
        {
            EnsureLoaded();
            var issues = new List<string>();
            if (spellIds != null)
                foreach (string id in spellIds)
                    if (Find(id) == null) issues.Add(id + ": no definition or explicit fallback.");
            return issues;
        }
        public static List<string> ValidateJson(string json)
        {
            var issues = new List<string>();
            Parse(json, null, issues);
            return issues;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetRuntimeState()
        {
            foreach (SpellFxAsset asset in Assets.Values) asset?.Dispose();
            Assets.Clear(); Definitions.Clear(); Issues.Clear(); _loaded = false;
        }
        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            TextAsset file = Resources.Load<TextAsset>(ResourcePath);
            if (file == null) Issues.Add("Missing Resources/" + ResourcePath + ".json");
            else Parse(file.text, Definitions, Issues);
            foreach (string issue in Issues) Debug.LogWarning("[SpellFxCatalog] " + issue);
        }
        private static void Parse(string json, Dictionary<string, SpellFxDefinition> output, List<string> issues)
        {
            if (string.IsNullOrWhiteSpace(json)) { issues.Add("Spell visual JSON is empty."); return; }
            SpellFxDefinitionFile file;
            try { file = JsonUtility.FromJson<SpellFxDefinitionFile>(json); }
            catch (Exception exception) { issues.Add("Invalid spell visual JSON: " + exception.Message); return; }
            if (file?.Definitions == null) { issues.Add("Spell visual catalog has no Definitions array."); return; }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (SpellFxDefinition definition in file.Definitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.ID)) { issues.Add("Spell visual definition is missing ID."); continue; }
                if (!seen.Add(definition.ID)) { issues.Add("Duplicate spell visual ID '" + definition.ID + "'."); continue; }
                if (!Enum.TryParse(definition.Family, out SpellFxFamily family)) issues.Add(definition.ID + ": unknown Family.");
                if (!definition.IsExplicitFallback && (string.IsNullOrWhiteSpace(definition.DetailSheet) || string.IsNullOrWhiteSpace(definition.ImpactSheet)))
                    issues.Add(definition.ID + ": missing art paths and no explicit fallback.");
                if (definition.CastDuration < 0 || definition.ChargeDuration < 0 || definition.StepDuration <= 0 || definition.ImpactDuration <= 0 || definition.AftermathDuration < 0
                    || !Finite(definition.CastDuration + definition.ChargeDuration + definition.StepDuration + definition.ImpactDuration + definition.AftermathDuration))
                    issues.Add(definition.ID + ": invalid phase timing.");
                output?.Add(definition.ID, definition);
            }
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
