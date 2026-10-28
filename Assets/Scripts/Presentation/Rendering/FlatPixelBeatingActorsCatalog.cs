using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;
namespace CavesOfOoo.Rendering
{
    /// <summary>Exact neutral Beating actors, portable with their living owners.
    /// Names, relationships, services and belongings remain simulation-owned.</summary>
    public sealed class FlatPixelBeatingActorsCatalog : IDisposable
    {
        readonly Tile scorpion, hound, drifter;
        bool disposed;
        public FlatPixelBeatingActorsCatalog()
            : this(Resources.Load<Texture2D>("Sprites/Environment/scorpion"),
                Resources.Load<Texture2D>("Sprites/Environment/brittle_hound"),
                Resources.Load<Texture2D>("Sprites/Environment/glassblown_drifter")) { }
        public FlatPixelBeatingActorsCatalog(Texture2D scorpion16, Texture2D hound16, Texture2D drifter16)
        {
            scorpion = Build(scorpion16, 16, 16, 0, "scorpion");
            hound = Build(hound16, 16, 16, 0, "brittle hound");
            drifter = Build(drifter16, 16, 16, 0, "glassblown drifter");
        }
        public static bool HasKnownRole(Entity owner)
            => owner?.BlueprintName == "Scorpion" || owner?.BlueprintName == "BrittleHound"
                || owner?.BlueprintName == "GlassblownDrifter";
        public bool TryResolve(Entity owner, out Tile tile)
        {
            tile = null;
            if (disposed || !HasKnownRole(owner) || !owner.HasTag("Creature")
                || owner.HasTag("Player") || owner.HasTag("Item") || owner.HasTag("Terrain")
                || ForeignMeaning(owner)) return false;
            var r = Owned<RenderPart>(owner); var p = Owned<PhysicsPart>(owner);
            var hp = owner.GetStat("Hitpoints");
            if (r == null || !r.Visible || r.RenderLayer != 10 || r.RenderString != (owner.BlueprintName == "Scorpion" ? "x" : owner.BlueprintName == "BrittleHound" ? "b" : "@")
                || r.ColorString != (owner.BlueprintName == "GlassblownDrifter" ? "&R" : "&W")
                || !string.IsNullOrEmpty(r.BackgroundColor) || !string.IsNullOrEmpty(r.VisualID)
                || !string.IsNullOrEmpty(r.VisualVariant) || !string.IsNullOrEmpty(r.GlyphVariants)
                || p == null || p.Takeable || p.InInventory != null || p.Equipped != null
                || Owned<Body>(owner) == null || Owned<BrainPart>(owner) == null
                || Owned<ExaminablePart>(owner) == null || Owned<InventoryPart>(owner) == null
                || hp == null || hp.Owner != owner || hp.BaseValue <= 0 || !SingleCell(owner)
                || !OptionalOwned<StatusEffectsPart>(owner)) return false;
            if (owner.HasPart<StackerPart>())
            { var stack = Owned<StackerPart>(owner); if (stack == null || stack.StackCount <= 0) return false; }
            if (owner.BlueprintName == "GlassblownDrifter"
                && (Owned<TraderPart>(owner) == null || Owned<ConversationPart>(owner) == null)) return false;
            tile = owner.BlueprintName == "Scorpion" ? scorpion
                : owner.BlueprintName == "BrittleHound" ? hound : drifter;
            return tile != null;
        }
        static bool ForeignMeaning(Entity owner)
            => owner.HasPart<ContainerPart>() || owner.HasPart<LockPart>() || owner.HasPart<ForgePart>()
                || owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>() || owner.HasPart<CultivatedSoilPart>()
                || owner.HasPart<HarvestablePart>() || owner.HasPart<LiquidPoolPart>() || owner.HasPart<WellPart>()
                || owner.HasPart<DoorPart>() || owner.HasPart<RegionalSignpostPart>() || owner.HasPart<CampfirePart>()
                || owner.HasPart<AlchemyStillPart>() || owner.HasPart<TriggerOnStepPart>()
                || owner.HasPart<StairsDownPart>() || owner.HasPart<StairsUpPart>()
                || owner.HasPart<DestructiblePart>() || owner.HasPart<GasPoolPart>()
                || owner.HasPart<TileStateSourcePart>();
        static bool OptionalOwned<T>(Entity owner) where T : Part => !owner.HasPart<T>() || Owned<T>(owner) != null;
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
            var part = Owned<SpatialFootprintPart>(owner);
            return part != null && part.Offsets.Length == 1 && part.Offsets[0] == Vector2Int.zero;
        }
        static Tile Build(Texture2D texture, int width, int height, int x, string label)
        {
            if (texture == null || texture.width != width || texture.height != height) return null;
            var sprite = Sprite.Create(texture, new Rect(x, 0, 16, height), new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
            sprite.name = "Live16x24 shared " + label; sprite.hideFlags = HideFlags.HideAndDontSave;
            var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
            tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
            tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None;
            return tile;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            DestroyFrame(scorpion); DestroyFrame(hound); DestroyFrame(drifter);
        }
        static void DestroyFrame(Tile tile)
        { if (tile == null) return; DestroyOwned(tile.sprite); DestroyOwned(tile); }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
