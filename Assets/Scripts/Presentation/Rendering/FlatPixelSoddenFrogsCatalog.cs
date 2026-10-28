using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;
namespace CavesOfOoo.Rendering
{
    /// <summary>Exact neutral Sodden frogs, portable with their living owners.
    /// Names, relationships, services and belongings remain simulation-owned.</summary>
    public sealed class FlatPixelSoddenFrogsCatalog : IDisposable
    {
        readonly Tile reedfrog, bandfrog, ginfrog;
        bool disposed;
        public FlatPixelSoddenFrogsCatalog()
            : this(Resources.Load<Texture2D>("Sprites/Live16x24/reedfrog"),
                Resources.Load<Texture2D>("Sprites/Live16x24/bandfrog"),
                Resources.Load<Texture2D>("Sprites/Environment/gin_frog")) { }
        public FlatPixelSoddenFrogsCatalog(Texture2D reedfrog16, Texture2D bandfrog16, Texture2D ginfrog16)
        {
            reedfrog = Build(reedfrog16, 16, 24, 0, "reedfrog");
            bandfrog = Build(bandfrog16, 16, 24, 0, "bandfrog");
            ginfrog = Build(ginfrog16, 16, 16, 0, "gin frog");
        }
        public static bool HasKnownRole(Entity owner)
            => owner?.BlueprintName == "Reedfrog" || owner?.BlueprintName == "Bandfrog"
                || owner?.BlueprintName == "GinFrog";
        public bool TryResolve(Entity owner, out Tile tile)
        {
            tile = null;
            if (disposed || !HasKnownRole(owner) || !owner.HasTag("Creature")
                || owner.HasTag("Player") || owner.HasTag("Item") || owner.HasTag("Terrain") || owner.HasTag("Corpse")
                || ForeignMeaning(owner)) return false;
            var r = Owned<RenderPart>(owner); var p = Owned<PhysicsPart>(owner);
            var hp = owner.GetStat("Hitpoints");
            if (r == null || !r.Visible || r.RenderLayer != 10 || r.RenderString != "f"
                || r.ColorString != (owner.BlueprintName == "Reedfrog" ? "&G" : owner.BlueprintName == "Bandfrog" ? "&R" : "&Y")
                || !string.IsNullOrEmpty(r.BackgroundColor) || !string.IsNullOrEmpty(r.VisualID)
                || !string.IsNullOrEmpty(r.VisualVariant) || !string.IsNullOrEmpty(r.GlyphVariants)
                || p == null || p.Takeable || p.InInventory != null || p.Equipped != null
                || Owned<Body>(owner) == null || Owned<BrainPart>(owner) == null
                || Owned<ExaminablePart>(owner) == null || Owned<InventoryPart>(owner) == null
                || hp == null || hp.Owner != owner || hp.BaseValue <= 0 || !SingleCell(owner)
                || !OptionalOwned<StatusEffectsPart>(owner)) return false;
            if (owner.HasPart<StackerPart>())
            { var stack = Owned<StackerPart>(owner); if (stack == null || stack.StackCount <= 0) return false; }
            if (owner.BlueprintName == "Bandfrog" ? Owned<CausticSkinPart>(owner) == null : owner.HasPart<CausticSkinPart>()) return false;
            tile = owner.BlueprintName == "Reedfrog" ? reedfrog
                : owner.BlueprintName == "Bandfrog" ? bandfrog : ginfrog;
            return tile != null;
        }
        static bool ForeignMeaning(Entity owner)
            => owner.HasPart<TraderPart>() || owner.HasPart<ConversationPart>() || owner.HasPart<ContainerPart>() || owner.HasPart<LockPart>() || owner.HasPart<ForgePart>()
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
            return part != null && part.Offsets.Length == 1 && part.Offsets[0] == Vector2Int.zero
                && (owner.SpatialZone == null || owner.SpatialZone.IsFootprintCurrent(owner));
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
            DestroyFrame(reedfrog); DestroyFrame(bandfrog); DestroyFrame(ginfrog);
        }
        static void DestroyFrame(Tile tile)
        { if (tile == null) return; DestroyOwned(tile.sprite); DestroyOwned(tile); }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
