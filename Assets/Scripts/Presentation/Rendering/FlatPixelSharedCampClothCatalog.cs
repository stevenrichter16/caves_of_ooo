using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;
namespace CavesOfOoo.Rendering
{
    /// <summary>Original straight cloth walls and passable guest pole. Body lookup
    /// does not apply heat, damage, services or disclosure; callers own those.</summary>
    public sealed class FlatPixelSharedCampClothCatalog : IDisposable
    {
        readonly Tile wallA, wallB, pole;
        bool disposed;
        public FlatPixelSharedCampClothCatalog()
            : this(Resources.Load<Texture2D>("Sprites/Live16x24/shared-camp-cloth")) { }
        public FlatPixelSharedCampClothCatalog(Texture2D atlas)
        { wallA = Build(atlas, 0); wallB = Build(atlas, 1); pole = Build(atlas, 2); }
        public static bool HasKnownRole(Entity e)
            => e?.BlueprintName == "TentWall" || e?.BlueprintName == "GuestClothPole";
        public bool TryResolve(Entity e, int x, int y, out Tile tile)
        {
            tile = null;
            if (disposed || !HasKnownRole(e) || e.HasTag("Creature") || e.HasTag("Player")
                || e.HasTag("Item") || e.GetStat("Hitpoints") != null || ForeignMeaning(e)) return false;
            bool wall = e.BlueprintName == "TentWall";
            var r = Owned<RenderPart>(e); var p = Owned<PhysicsPart>(e);
            if (r == null || !r.Visible || r.RenderLayer != 2 || r.RenderString != (wall ? "#" : "|")
                || r.ColorString != (wall ? "&y" : "&Y") || !string.IsNullOrEmpty(r.BackgroundColor)
                || !string.IsNullOrEmpty(r.VisualID) || !string.IsNullOrEmpty(r.VisualVariant)
                || !string.IsNullOrEmpty(r.GlyphVariants) || p == null || p.Solid != wall
                || p.Takeable || p.InInventory != null || p.Equipped != null
                || Owned<ExaminablePart>(e) == null || !SingleCell(e) || !OptionalOwned<StatusEffectsPart>(e)) return false;
            if (e.HasPart<StackerPart>())
            { var s = Owned<StackerPart>(e); if (s == null || s.StackCount <= 0) return false; }
            if (wall)
            {
                var m = Owned<MaterialPart>(e); var d = Owned<DestructiblePart>(e); var t = Owned<ThermalPart>(e);
                if (!e.HasTag("Terrain") || m == null || m.MaterialID != "Cloth" || d == null
                    || d.Gone || d.Indestructible || d.HP <= 0 || d.HP > d.MaxHP || d.MaxHP != 8
                    || d.Hardness != 0 || !string.IsNullOrEmpty(d.WreckageBlueprint) || t == null
                    || float.IsNaN(t.Temperature) || float.IsInfinity(t.Temperature)) return false;
                tile = ((x + y) & 1) == 0 ? wallA : wallB;
            }
            else
            {
                if (e.HasTag("Terrain") || !e.HasTag("Furniture") || e.HasPart<MaterialPart>()
                    || e.HasPart<DestructiblePart>() || e.HasPart<ThermalPart>()) return false;
                tile = pole;
            }
            return tile != null;
        }
        static bool ForeignMeaning(Entity e)
            => e.HasPart<BrainPart>() || e.HasPart<Body>() || e.HasPart<TraderPart>()
                || e.HasPart<ConversationPart>() || e.HasPart<ContainerPart>() || e.HasPart<LockPart>()
                || e.HasPart<WellPart>() || e.HasPart<WellSitePart>() || e.HasPart<RepairablePart>()
                || e.HasPart<LiquidPoolPart>() || e.HasPart<ForgePart>() || e.HasPart<CropPart>()
                || e.HasPart<FieldHarvestPart>() || e.HasPart<CultivatedSoilPart>() || e.HasPart<HarvestablePart>()
                || e.HasPart<DoorPart>() || e.HasPart<RegionalSignpostPart>() || e.HasPart<CampfirePart>()
                || e.HasPart<AlchemyStillPart>() || e.HasPart<SoddenPreparationPart>() || e.HasPart<TriggerOnStepPart>()
                || e.HasPart<StairsDownPart>() || e.HasPart<StairsUpPart>() || e.HasPart<GasPoolPart>()
                || e.HasPart<TileStateSourcePart>() || e.HasPart<FiniteLiquidFilmPart>() || e.HasPart<BurnOffGasPart>()
                || e.HasPart("Restable") || e.HasPart("ReclaimCloth") || e.HasTag("Restable") || e.HasTag("Plantable");
        static bool OptionalOwned<T>(Entity e) where T : Part => !e.HasPart<T>() || Owned<T>(e) != null;
        static T Owned<T>(Entity e) where T : Part
        {
            T found = null;
            foreach (var p in e.Parts) if (p is T typed)
            { if (found != null || typed.ParentEntity != e) return null; found = typed; }
            return found;
        }
        static bool SingleCell(Entity e)
        {
            if (!e.HasPart<SpatialFootprintPart>()) return true;
            var p = Owned<SpatialFootprintPart>(e);
            return p != null && p.Offsets.Length == 1 && p.Offsets[0] == Vector2Int.zero;
        }
        static Tile Build(Texture2D atlas, int frame)
        {
            if (atlas == null || atlas.width != 48 || atlas.height != 24) return null;
            var s = Sprite.Create(atlas, new Rect(frame * 16, 0, 16, 24), new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
            s.name = "Live16x24 camp cloth " + frame; s.hideFlags = HideFlags.HideAndDontSave;
            var t = ScriptableObject.CreateInstance<Tile>(); t.name = s.name; t.hideFlags = HideFlags.HideAndDontSave;
            t.sprite = s; t.flags = TileFlags.None; t.colliderType = Tile.ColliderType.None; return t;
        }
        public void Dispose()
        { if (disposed) return; disposed = true; DestroyFrame(wallA); DestroyFrame(wallB); DestroyFrame(pole); }
        static void DestroyFrame(Tile t) { if (t == null) return; DestroyOwned(t.sprite); DestroyOwned(t); }
        static void DestroyOwned(Object o)
        { if (o == null) return; if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o); }
    }
}
