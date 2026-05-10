using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// 16×24 creature-sprite registry (Phase A).
    ///
    /// At first access (Editor only), scans <c>Assets/coo_16x24/</c> and
    /// builds a <c>family-name → Sprite</c> map. The "family name" is the
    /// snake_case top-level folder under <c>coo_16x24/</c> (e.g.
    /// <c>pallid_archivist</c>). For each family, picks one variant: the
    /// PNG containing <c>_default_</c> in its name if present, else the
    /// first non-<c>_source_bw</c> variant alphabetically.
    ///
    /// Mapping policy (Phase A):
    /// <list type="number">
    ///   <item>Convert blueprint name to snake_case (PallidArchivist →
    ///         pallid_archivist).</item>
    ///   <item>Look up an explicit override in <see cref="OverrideMap"/>.
    ///         Wins over convention. (Lets us prove the pipeline by mapping
    ///         <c>Snapjaw</c> → <c>pallid_archivist</c> without needing a
    ///         <c>PallidArchivist</c> blueprint to exist yet.)</item>
    ///   <item>Else look up snake_case key directly. If found → return.</item>
    ///   <item>Else return null. The caller falls back to CP437.</item>
    /// </list>
    ///
    /// Lazy + idempotent: <see cref="TryGet"/> initializes on first call.
    /// </summary>
    public static class CreatureSpriteRegistry
    {
        private const string ROOT_PATH = "Assets/coo_16x24";

        // Phase A explicit overrides: blueprint-name → family-name.
        // Lets us demo the pipeline against real creatures even when
        // no blueprint shares a name with the sprite families yet.
        // Add entries as new mappings are needed; remove when content
        // gets renamed to match convention.
        private static readonly Dictionary<string, string> OverrideMap =
            new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "Snapjaw",          "pallid_archivist" },
                { "SnapjawHunter",    "pallid_archivist" },
                { "SnapjawScavenger", "pallid_archivist" },
                { "SnapjawChieftain", "pallid_archivist" },
            };

        private static Dictionary<string, Sprite> _spritesByFamily;
        private static bool _initialized;

        public static int CountForTests => _spritesByFamily?.Count ?? 0;

        public static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            _spritesByFamily = new Dictionary<string, Sprite>(
                System.StringComparer.OrdinalIgnoreCase);
            LoadAll();
        }

        public static bool TryGet(string blueprintName, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrEmpty(blueprintName)) return false;
            EnsureInitialized();

            // Override wins
            if (OverrideMap.TryGetValue(blueprintName, out var overrideFamily)
                && _spritesByFamily.TryGetValue(overrideFamily, out sprite)
                && sprite != null)
            {
                return true;
            }

            // Convention: snake_case the blueprint name → family lookup
            string snake = ToSnakeCase(blueprintName);
            if (_spritesByFamily.TryGetValue(snake, out sprite) && sprite != null)
                return true;

            sprite = null;
            return false;
        }

        /// <summary>
        /// PallidArchivistElder → pallid_archivist_elder.
        /// </summary>
        public static string ToSnakeCase(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new System.Text.StringBuilder(s.Length + 4);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsUpper(c))
                {
                    if (i > 0 && (char.IsLower(s[i - 1]) || char.IsDigit(s[i - 1])))
                        sb.Append('_');
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

#if UNITY_EDITOR
        private static void LoadAll()
        {
            // Walk top-level: family folders + bare PNGs at root.
            // Family folders contribute one entry per folder (the
            // chosen variant). Bare PNGs at root contribute one entry
            // each, keyed by filename-without-extension.
            string root = ROOT_PATH;
            if (!System.IO.Directory.Exists(root))
            {
                Debug.LogWarning($"[CreatureSpriteRegistry] {root} not found.");
                return;
            }

            foreach (var entry in System.IO.Directory.EnumerateFileSystemEntries(root))
            {
                string name = System.IO.Path.GetFileName(entry);
                if (name.StartsWith(".")) continue;

                if (System.IO.Directory.Exists(entry))
                {
                    string family = name; // already snake_case by convention
                    string chosenPath = ChooseVariantPath(entry, family);
                    if (chosenPath != null)
                    {
                        var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(chosenPath);
                        if (sprite != null)
                            _spritesByFamily[family] = sprite;
                    }
                }
                else if (name.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                {
                    string family = System.IO.Path.GetFileNameWithoutExtension(name);
                    // Strip a trailing "_black_and_white" suffix so e.g.
                    // myconid_pilgrim_black_and_white.png keys as
                    // "myconid_pilgrim".
                    if (family.EndsWith("_black_and_white",
                        System.StringComparison.OrdinalIgnoreCase))
                    {
                        family = family.Substring(0, family.Length - "_black_and_white".Length);
                    }
                    var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(entry);
                    if (sprite != null)
                        _spritesByFamily[family] = sprite;
                }
            }

            Debug.Log($"[CreatureSpriteRegistry] Loaded {_spritesByFamily.Count} families: "
                + string.Join(", ", _spritesByFamily.Keys));
        }

        /// <summary>
        /// Pick one variant per family: prefer <c>_default_</c>, else the
        /// first non-<c>_source_bw</c> variant alphabetically.
        /// </summary>
        private static string ChooseVariantPath(string folder, string family)
        {
            var candidates = new List<string>();
            foreach (var f in System.IO.Directory.EnumerateFiles(folder, "*.png"))
            {
                string fname = System.IO.Path.GetFileName(f);
                if (fname.Contains("_source_bw")) continue; // master stencil, skip
                candidates.Add(f);
            }
            candidates.Sort(System.StringComparer.OrdinalIgnoreCase);

            // Prefer "_default_"
            foreach (var c in candidates)
            {
                if (System.IO.Path.GetFileName(c).Contains("_default_"))
                    return c;
            }
            return candidates.Count > 0 ? candidates[0] : null;
        }
#else
        private static void LoadAll()
        {
            // Runtime-build path: Phase A is Editor-only. A future phase
            // can swap in a Resources.LoadAll or Addressables-based
            // loader so the registry works in builds. Out of scope.
        }
#endif

        // ── Test seam ─────────────────────────────────────────────

        public static void TestOnly_Reset()
        {
            _initialized = false;
            _spritesByFamily = null;
        }

        public static IReadOnlyDictionary<string, Sprite> TestOnly_All =>
            _spritesByFamily;
    }
}
