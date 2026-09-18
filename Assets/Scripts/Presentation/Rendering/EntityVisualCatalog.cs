using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Presentation states in the standard four-direction actor sheet.
    /// </summary>
    public enum EntityVisualState
    {
        Idle = 0,
        Walk = 1,
        Attack = 2,
        Hurt = 3,
        Cast = 4,
    }

    [Serializable]
    public sealed class EntityVisualDefinitionFile
    {
        public EntityVisualDefinition[] Definitions;
    }

    /// <summary>
    /// Data-only actor visual definition. Each sheet has four columns and sixteen
    /// rows: four facings for Idle, Walk, Attack, then Hurt. Rows are authored
    /// top-to-bottom; runtime slicing accounts for Unity's bottom-left texture origin.
    /// </summary>
    [Serializable]
    public sealed class EntityVisualDefinition
    {
        public string ID;
        public string CanonicalFamily;
        public string Sheet;
        // Optional four-facing sheet, independent of the existing sixteen-row sheet.
        // Missing or malformed casting art falls back to the Attack frames.
        public string CastSheet;
        public string[] Blueprints;
        public string CanonicalGlyph;

        public int FrameWidth = 16;
        public int FrameHeight = 24;
        public int PixelsPerUnit = 16;
        public float PivotX = 0.5f;
        public float PivotY = 0f;

        public int IdleFrames = 1;
        public int WalkFrames = 1;
        public int AttackFrames = 1;
        public int HurtFrames = 1;
        public int CastFrames = 0;

        public float IdleFPS = 2f;
        public float MoveDuration = 0.10f;
        public float AttackDuration = 0.16f;
        public float HurtDuration = 0.14f;
        public float CastDuration = 0.16f;

        public int GetFrameCount(EntityVisualState state)
        {
            switch (state)
            {
                case EntityVisualState.Walk: return Mathf.Clamp(WalkFrames, 1, 4);
                case EntityVisualState.Attack: return Mathf.Clamp(AttackFrames, 1, 4);
                case EntityVisualState.Hurt: return Mathf.Clamp(HurtFrames, 1, 4);
                case EntityVisualState.Cast: return Mathf.Clamp(CastFrames > 0 ? CastFrames : AttackFrames, 1, 4);
                default: return Mathf.Clamp(IdleFrames, 1, 4);
            }
        }

        public float GetDuration(EntityVisualState state)
        {
            switch (state)
            {
                case EntityVisualState.Walk: return Mathf.Max(0.01f, MoveDuration);
                case EntityVisualState.Attack: return Mathf.Max(0.01f, AttackDuration);
                case EntityVisualState.Hurt: return Mathf.Max(0.01f, HurtDuration);
                case EntityVisualState.Cast: return Mathf.Max(0.01f, CastDuration);
                default: return 0f;
            }
        }
    }

    /// <summary>
    /// Runtime-sliced sprites for one visual definition. This intentionally uses a
    /// small custom frame player instead of one AnimatorController per world entity.
    /// </summary>
    public sealed class EntityVisualAsset
    {
        private const int StateCount = 5;
        private const int FacingCount = 4;
        private const int MaxFrames = 4;

        private readonly Sprite[,,] _frames = new Sprite[StateCount, FacingCount, MaxFrames];

        public EntityVisualDefinition Definition { get; }
        public bool HasCastingArt { get; private set; }

        public EntityVisualAsset(EntityVisualDefinition definition, Texture2D sheet, Texture2D castSheet = null)
        {
            Definition = definition;
            Slice(sheet);
            SliceCasting(castSheet);
        }

        public int GetFrameCount(EntityVisualState state)
            => Definition.GetFrameCount(ResolveState(state));

        private EntityVisualState ResolveState(EntityVisualState state)
            => state == EntityVisualState.Cast && !HasCastingArt ? EntityVisualState.Attack : state;

        public Sprite GetFrame(EntityVisualState state, EntityVisualFacing facing, int frame)
        {
            state = ResolveState(state);
            int count = Definition.GetFrameCount(state);
            int safeFrame = count > 0 ? Mathf.Abs(frame) % count : 0;
            return _frames[(int)state, (int)facing, safeFrame];
        }

        private void Slice(Texture2D sheet)
        {
            int frameWidth = Mathf.Max(1, Definition.FrameWidth);
            int frameHeight = Mathf.Max(1, Definition.FrameHeight);
            int ppu = Mathf.Max(1, Definition.PixelsPerUnit);
            Vector2 pivot = new Vector2(
                Mathf.Clamp01(Definition.PivotX),
                Mathf.Clamp01(Definition.PivotY));

            for (int state = 0; state < (int)EntityVisualState.Cast; state++)
            {
                int frameCount = Definition.GetFrameCount((EntityVisualState)state);
                for (int facing = 0; facing < FacingCount; facing++)
                {
                    int authoredRow = state * FacingCount + facing;
                    int textureY = sheet.height - (authoredRow + 1) * frameHeight;
                    for (int frame = 0; frame < frameCount; frame++)
                    {
                        Rect rect = new Rect(frame * frameWidth, textureY, frameWidth, frameHeight);
                        Sprite sprite = Sprite.Create(
                            sheet,
                            rect,
                            pivot,
                            ppu,
                            0,
                            SpriteMeshType.FullRect);
                        sprite.name = $"{Definition.ID}_{(EntityVisualState)state}_{(EntityVisualFacing)facing}_{frame}";
                        sprite.hideFlags = HideFlags.DontSave;
                        _frames[state, facing, frame] = sprite;
                    }
                }
            }
        }

        private void SliceCasting(Texture2D sheet)
        {
            int count = Definition.GetFrameCount(EntityVisualState.Cast);
            int width = Mathf.Max(1, Definition.FrameWidth);
            int height = Mathf.Max(1, Definition.FrameHeight);
            if (sheet == null || Definition.CastFrames <= 0
                || sheet.width < count * width || sheet.height < FacingCount * height)
                return;

            sheet.filterMode = FilterMode.Point;
            for (int facing = 0; facing < FacingCount; facing++)
            {
                for (int frame = 0; frame < count; frame++)
                {
                    Sprite sprite = Sprite.Create(sheet,
                        new Rect(frame * width, sheet.height - (facing + 1) * height, width, height),
                        new Vector2(Mathf.Clamp01(Definition.PivotX), Mathf.Clamp01(Definition.PivotY)),
                        Mathf.Max(1, Definition.PixelsPerUnit), 0, SpriteMeshType.FullRect);
                    sprite.name = $"{Definition.ID}_Cast_{(EntityVisualFacing)facing}_{frame}";
                    sprite.hideFlags = HideFlags.DontSave;
                    _frames[(int)EntityVisualState.Cast, facing, frame] = sprite;
                }
            }
            HasCastingArt = true;
        }
    }

    /// <summary>
    /// Lazy, data-driven lookup from stable visual IDs or legacy blueprint IDs to
    /// animated sprite sheets. Invalid or absent data returns false so the existing
    /// static-sprite and CP437 fallback chain remains intact.
    /// </summary>
    public static class EntityVisualCatalog
    {
        public const string ResourcePath = "Content/Data/EntityVisuals";

        private static readonly Dictionary<string, EntityVisualDefinition> _byID
            = new Dictionary<string, EntityVisualDefinition>(StringComparer.Ordinal);
        private static readonly Dictionary<string, EntityVisualDefinition> _byBlueprint
            = new Dictionary<string, EntityVisualDefinition>(StringComparer.Ordinal);
        private static readonly Dictionary<string, EntityVisualAsset> _assets
            = new Dictionary<string, EntityVisualAsset>(StringComparer.Ordinal);
        private static readonly List<string> _validationIssues = new List<string>();

        private static bool _loaded;

        public static IReadOnlyList<string> ValidationIssues
        {
            get
            {
                EnsureLoaded();
                return _validationIssues;
            }
        }

        public static int DefinitionCount
        {
            get
            {
                EnsureLoaded();
                return _byID.Count;
            }
        }

        public static bool TryGetDefinition(Entity entity, out EntityVisualDefinition definition)
        {
            definition = null;
            if (entity == null) return false;
            EnsureLoaded();

            RenderPart render = entity.GetPart<RenderPart>();
            bool explicitVisualRequested = render != null && !string.IsNullOrWhiteSpace(render.VisualID);
            bool resolvedExplicitVisual = explicitVisualRequested
                && _byID.TryGetValue(render.VisualID, out definition);

            if (definition == null && !string.IsNullOrEmpty(entity.BlueprintName))
                _byBlueprint.TryGetValue(entity.BlueprintName, out definition);
            if (definition == null)
                return false;

            // Blueprint fallback preserves the existing reskin guard. A quest may
            // reuse Villager or Snapjaw and replace its current glyph; that entity
            // must keep honest fallback art unless it explicitly chooses a VisualID.
            if (!resolvedExplicitVisual && !string.IsNullOrEmpty(definition.CanonicalGlyph))
            {
                char currentGlyph = CurrentGlyph(render);
                if (currentGlyph == '\0' || currentGlyph != definition.CanonicalGlyph[0])
                {
                    definition = null;
                    return false;
                }
            }

            return true;
        }

        public static bool TryGetAsset(Entity entity, out EntityVisualAsset asset)
        {
            asset = null;
            if (!TryGetDefinition(entity, out EntityVisualDefinition definition))
                return false;

            if (_assets.TryGetValue(definition.ID, out asset))
                return asset != null;

            Texture2D sheet = Resources.Load<Texture2D>(definition.Sheet);
            if (sheet == null)
            {
                _validationIssues.Add($"{definition.ID}: missing sheet Resources/{definition.Sheet}");
                _assets[definition.ID] = null;
                return false;
            }

            int requiredWidth = Mathf.Max(1, definition.FrameWidth) * 4;
            int requiredHeight = Mathf.Max(1, definition.FrameHeight) * 16;
            if (sheet.width < requiredWidth || sheet.height < requiredHeight)
            {
                _validationIssues.Add(
                    $"{definition.ID}: sheet is {sheet.width}x{sheet.height}; expected at least {requiredWidth}x{requiredHeight}");
                _assets[definition.ID] = null;
                return false;
            }

            Texture2D castSheet = string.IsNullOrWhiteSpace(definition.CastSheet)
                ? null : Resources.Load<Texture2D>(definition.CastSheet);
            asset = new EntityVisualAsset(definition, sheet, castSheet);
            _assets[definition.ID] = asset;
            return true;
        }

        public static bool HasDefinitionForBlueprint(string blueprint)
        {
            EnsureLoaded();
            return !string.IsNullOrEmpty(blueprint) && _byBlueprint.ContainsKey(blueprint);
        }

        public static List<string> ValidateJson(string json)
        {
            var issues = new List<string>();
            ParseDefinitions(json, null, null, issues);
            return issues;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetRuntimeState()
        {
            _loaded = false;
            _byID.Clear();
            _byBlueprint.Clear();
            _assets.Clear();
            _validationIssues.Clear();
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                _validationIssues.Add($"Missing Resources/{ResourcePath}.json");
                return;
            }

            ParseDefinitions(asset.text, _byID, _byBlueprint, _validationIssues);
            for (int i = 0; i < _validationIssues.Count; i++)
                Debug.LogWarning("[EntityVisualCatalog] " + _validationIssues[i]);
        }

        private static void ParseDefinitions(
            string json,
            Dictionary<string, EntityVisualDefinition> byID,
            Dictionary<string, EntityVisualDefinition> byBlueprint,
            List<string> issues)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                issues.Add("Visual catalog JSON is empty.");
                return;
            }

            EntityVisualDefinitionFile file;
            try
            {
                file = JsonUtility.FromJson<EntityVisualDefinitionFile>(json);
            }
            catch (Exception ex)
            {
                issues.Add("Visual catalog JSON could not be parsed: " + ex.Message);
                return;
            }

            if (file?.Definitions == null)
            {
                issues.Add("Visual catalog has no Definitions array.");
                return;
            }

            var seenIDs = new HashSet<string>(StringComparer.Ordinal);
            var seenBlueprints = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < file.Definitions.Length; i++)
            {
                EntityVisualDefinition definition = file.Definitions[i];
                if (definition == null)
                {
                    issues.Add($"Definition {i} is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(definition.ID))
                {
                    issues.Add($"Definition {i} has no ID.");
                    continue;
                }
                if (!seenIDs.Add(definition.ID))
                {
                    issues.Add($"Duplicate visual ID '{definition.ID}'.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(definition.Sheet))
                    issues.Add($"{definition.ID}: Sheet is empty.");
                if (definition.FrameWidth <= 0 || definition.FrameHeight <= 0 || definition.PixelsPerUnit <= 0)
                    issues.Add($"{definition.ID}: frame dimensions and PixelsPerUnit must be positive.");
                if (definition.Blueprints == null || definition.Blueprints.Length == 0)
                    issues.Add($"{definition.ID}: no Blueprints are mapped.");

                Normalize(definition);

                byID?.Add(definition.ID, definition);
                if (definition.Blueprints == null) continue;
                for (int b = 0; b < definition.Blueprints.Length; b++)
                {
                    string blueprint = definition.Blueprints[b];
                    if (string.IsNullOrWhiteSpace(blueprint))
                    {
                        issues.Add($"{definition.ID}: contains an empty blueprint ID.");
                        continue;
                    }
                    if (!seenBlueprints.Add(blueprint))
                    {
                        issues.Add($"Blueprint '{blueprint}' has more than one visual definition.");
                        continue;
                    }
                    byBlueprint?.Add(blueprint, definition);
                }
            }
        }

        private static void Normalize(EntityVisualDefinition definition)
        {
            if (definition.FrameWidth <= 0) definition.FrameWidth = 16;
            if (definition.FrameHeight <= 0) definition.FrameHeight = 24;
            if (definition.PixelsPerUnit <= 0) definition.PixelsPerUnit = 16;
            definition.IdleFrames = Mathf.Clamp(definition.IdleFrames, 1, 4);
            definition.WalkFrames = Mathf.Clamp(definition.WalkFrames, 1, 4);
            definition.AttackFrames = Mathf.Clamp(definition.AttackFrames, 1, 4);
            definition.HurtFrames = Mathf.Clamp(definition.HurtFrames, 1, 4);
            definition.CastFrames = Mathf.Clamp(definition.CastFrames, 0, 4);
            if (definition.IdleFPS <= 0f) definition.IdleFPS = 2f;
            if (definition.MoveDuration <= 0f) definition.MoveDuration = 0.10f;
            if (definition.AttackDuration <= 0f) definition.AttackDuration = 0.16f;
            if (definition.HurtDuration <= 0f) definition.HurtDuration = 0.14f;
            if (definition.CastDuration <= 0f) definition.CastDuration = 0.16f;
        }

        private static char CurrentGlyph(RenderPart render)
        {
            string value = render?.RenderString;
            return string.IsNullOrEmpty(value) ? '\0' : value[0];
        }
    }
}
