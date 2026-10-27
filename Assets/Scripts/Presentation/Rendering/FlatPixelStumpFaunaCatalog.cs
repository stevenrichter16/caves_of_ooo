using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Five exact living species, using unchanged south-idle sheet cells.
    /// Owns five cached sprite/tile pairs; borrows textures. Current graph,
    /// disclosure, selection and reader authority remain with presentation callers.</summary>
    public sealed class FlatPixelStumpFaunaCatalog : IDisposable
    {
        public const string CascadeFatherResourcePath = "Sprites/Actors/cascade_father_sheet";
        public const string GlasspaneFrogResourcePath = "Sprites/Actors/glasspane_frog_sheet";
        public const string YellowfootWayfarerResourcePath = "Sprites/Actors/yellowfoot_wayfarer_sheet";
        public const string WardlineResourcePath = "Sprites/Actors/wardline_sheet";
        public const string SariSnakeResourcePath = "Sprites/Actors/sari_snake_sheet";
        readonly Tile cascadeFather, glasspaneFrog, yellowfootWayfarer, wardline, sariSnake;
        bool disposed;

        public FlatPixelStumpFaunaCatalog()
            : this(Resources.Load<Texture2D>(CascadeFatherResourcePath), Resources.Load<Texture2D>(GlasspaneFrogResourcePath),
                Resources.Load<Texture2D>(YellowfootWayfarerResourcePath), Resources.Load<Texture2D>(WardlineResourcePath),
                Resources.Load<Texture2D>(SariSnakeResourcePath)) { }

        /// <summary>Optional sheets fail independently. Explicit null/malformed
        /// injection never reloads resources or changes a borrowed texture.</summary>
        public FlatPixelStumpFaunaCatalog(Texture2D cascade, Texture2D glasspane, Texture2D yellowfoot,
            Texture2D wardlineSheet, Texture2D sari)
        {
            cascadeFather = Build(cascade, "CascadeFather");
            glasspaneFrog = Build(glasspane, "GlasspaneFrog");
            yellowfootWayfarer = Build(yellowfoot, "YellowfootWayfarer");
            wardline = Build(wardlineSheet, "Wardline");
            sariSnake = Build(sari, "SariSnake");
        }

        /// <summary>Exact blueprint recognition only; does not grant visibility
        /// or promise that the owner's present body/state is supported.</summary>
        public static bool HasKnownRole(Entity owner)
            => owner?.BlueprintName == "CascadeFather" || owner?.BlueprintName == "GlasspaneFrog"
                || owner?.BlueprintName == "YellowfootWayfarer" || owner?.BlueprintName == "Wardline"
                || owner?.BlueprintName == "SariSnake";

        /// <summary>Pure semantic body lookup. Surviving damage, relationships,
        /// authored base color and status effects do not change species identity.
        /// The submitted painter separately preserves the real Render color cue.</summary>
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
                case "CascadeFather": if (render.RenderString == "f") tile = cascadeFather; break;
                case "GlasspaneFrog": if (render.RenderString == "f") tile = glasspaneFrog; break;
                case "YellowfootWayfarer": if (render.RenderString == "t") tile = yellowfootWayfarer; break;
                case "Wardline": if (render.RenderString == "s") tile = wardline; break;
                case "SariSnake": if (render.RenderString == "s") tile = sariSnake; break;
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
        static Tile Build(Texture2D texture, string identity)
        {
            if (texture == null || texture.width != 64 || texture.height != 384) return null;
            // The existing four-column/sixteen-row sheets place South idle0 at
            // top-left. Center this separate tile sprite; do not borrow the
            // legacy bottom-pivot motion sprites or instantiate their full sets.
            var sprite = Sprite.Create(texture, new Rect(0, 360, 16, 24), new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
            sprite.name = "Live16x24 static " + identity; sprite.hideFlags = HideFlags.HideAndDontSave;
            var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
            tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
            tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None;
            return tile;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            DestroyFrame(cascadeFather); DestroyFrame(glasspaneFrog); DestroyFrame(yellowfootWayfarer);
            DestroyFrame(wardline); DestroyFrame(sariSnake);
        }
        static void DestroyFrame(Tile tile)
        { if (tile == null) return; DestroyOwned(tile.sprite); DestroyOwned(tile); }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
