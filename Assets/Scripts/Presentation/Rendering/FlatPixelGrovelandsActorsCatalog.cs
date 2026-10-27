using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Three exact living Grovelands actors, portable with their owners.
    /// Borrows textures and owns only three cached sprite/tile pairs. Callers
    /// retain current graph, disclosure, selected-owner and reader authority.</summary>
    public sealed class FlatPixelGrovelandsActorsCatalog : IDisposable
    {
        public const string ShamblerResourcePath = "Sprites/Live16x24/shambler";
        public const string MosshulkResourcePath = "Sprites/Environment/mosshulk";
        public const string ChoirTendrilResourcePath = "Sprites/Environment/choir_tendril";
        readonly Tile shambler, mosshulk, choirTendril;
        bool disposed;

        public FlatPixelGrovelandsActorsCatalog()
            : this(Resources.Load<Texture2D>(ShamblerResourcePath),
                Resources.Load<Texture2D>(MosshulkResourcePath),
                Resources.Load<Texture2D>(ChoirTendrilResourcePath)) { }

        /// <summary>Independent optional inputs: one native 16x24 Shambler and
        /// two unchanged 16x16 sources. Explicit null or malformed input never
        /// reloads a default resource or mutates a borrowed texture.</summary>
        public FlatPixelGrovelandsActorsCatalog(Texture2D shamblerTexture,
            Texture2D mosshulkTexture, Texture2D choirTexture)
        {
            shambler = Build(shamblerTexture, 24, "Shambler");
            mosshulk = Build(mosshulkTexture, 16, "Mosshulk");
            choirTendril = Build(choirTexture, 16, "ChoirTendril");
        }

        /// <summary>Exact registered identity only; neither a visibility grant
        /// nor a promise that the current body is supported. In particular,
        /// plain Shambler and SporeShambler are different owners.</summary>
        public static bool HasKnownRole(Entity owner)
            => owner?.BlueprintName == "Shambler" || owner?.BlueprintName == "Mosshulk"
                || owner?.BlueprintName == "ChoirTendril";

        /// <summary>Pure body lookup: surviving damage, commitment, status,
        /// relationships and merchant stock/purse changes retain identity.
        /// The painter supplies the existing single Render-event color cue.</summary>
        public bool TryResolve(Entity owner, out Tile tile)
        {
            tile = null;
            if (disposed || !HasKnownRole(owner) || !owner.HasTag("Creature")
                || owner.HasTag("Player") || owner.HasTag("Item")) return false;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            var hp = owner.GetStat("Hitpoints");
            if (render == null || !render.Visible || physics == null || physics.Takeable
                || physics.InInventory != null || physics.Equipped != null
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)
                || !string.IsNullOrEmpty(render.GlyphVariants) || !SingleCell(owner)
                || Owned<Body>(owner) == null || Owned<BrainPart>(owner) == null
                || hp == null || hp.Owner != owner || hp.BaseValue <= 0) return false;
            if (owner.HasPart<StackerPart>())
            { var stack = Owned<StackerPart>(owner); if (stack == null || stack.StackCount <= 0) return false; }
            switch (owner.BlueprintName)
            {
                case "Shambler":
                    if (render.RenderString == "z" && Owned<CommittedMeleePart>(owner) != null) tile = shambler;
                    break;
                case "Mosshulk":
                    if (render.RenderString == "M") tile = mosshulk;
                    break;
                case "ChoirTendril":
                    if (render.RenderString == "F" && Owned<ConversationPart>(owner) != null
                        && Owned<TraderPart>(owner) != null && Owned<InventoryPart>(owner) != null) tile = choirTendril;
                    break;
            }
            return tile != null;
        }

        static T Owned<T>(Entity owner) where T : Part
        {
            T found = null;
            for (int i = 0; i < owner.Parts.Count; i++)
                if (owner.Parts[i] is T part)
                { if (found != null || part.ParentEntity != owner) return null; found = part; }
            return found;
        }
        static bool SingleCell(Entity owner)
        {
            if (!owner.HasPart<SpatialFootprintPart>()) return true;
            var footprint = Owned<SpatialFootprintPart>(owner);
            return footprint != null && footprint.Offsets.Length == 1 && footprint.Offsets[0] == Vector2Int.zero;
        }
        static Tile Build(Texture2D texture, int height, string identity)
        {
            if (texture == null || texture.width != 16 || texture.height != height) return null;
            // Center the original square sources in the taller world cell;
            // do not stretch them or inherit legacy importer sprite geometry.
            var sprite = Sprite.Create(texture, new Rect(0, 0, 16, height), new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
            sprite.name = "Live16x24 static " + identity; sprite.hideFlags = HideFlags.HideAndDontSave;
            var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
            tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
            tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None;
            return tile;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            DestroyFrame(shambler); DestroyFrame(mosshulk); DestroyFrame(choirTendril);
        }
        static void DestroyFrame(Tile tile)
        { if (tile == null) return; DestroyOwned(tile.sprite); DestroyOwned(tile); }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
