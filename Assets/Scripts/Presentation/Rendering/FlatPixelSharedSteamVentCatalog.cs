using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;
namespace CavesOfOoo.Rendering
{
    /// <summary>Neutral natural SteamVent body. Temperature/effects do not
    /// determine whether its independent tile source emits. Owns views only.</summary>
    public sealed class FlatPixelSharedSteamVentCatalog : IDisposable
    {
        readonly Tile body;
        bool disposed;
        public FlatPixelSharedSteamVentCatalog()
            : this(Resources.Load<Texture2D>("Sprites/Environment/steam_vent")) { }
        public FlatPixelSharedSteamVentCatalog(Texture2D natural16)
        { body = Build(natural16, 16, 16, 0, "natural steam vent"); }
        public static bool HasKnownRole(Entity owner) => owner?.BlueprintName == "SteamVent";
        public bool TryResolve(Entity owner, out Tile tile)
        {
            tile = null;
            if (disposed || !IsEligibleBody(owner)) return false;
            tile = body; return tile != null;
        }
        internal static bool IsEligibleBody(Entity owner)
        {
            if (!HasKnownRole(owner) || !owner.HasTag("Terrain") || owner.HasTag("Solid")
                || owner.HasTag("Player") || owner.HasTag("Creature") || owner.HasTag("Item")
                || owner.HasTag("Crop") || ForeignMeaning(owner)) return false;
            var r = Owned<RenderPart>(owner); var p = Owned<PhysicsPart>(owner);
            var m = Owned<MaterialPart>(owner); var t = Owned<ThermalPart>(owner);
            var s = Owned<TileStateSourcePart>(owner);
            if (r == null || !r.Visible || r.RenderLayer != 1 || r.RenderString != "^" || r.ColorString != "&W"
                || !string.IsNullOrEmpty(r.BackgroundColor) || !string.IsNullOrEmpty(r.VisualID)
                || !string.IsNullOrEmpty(r.VisualVariant) || !string.IsNullOrEmpty(r.GlyphVariants)
                || p == null || p.Solid || p.Takeable || p.InInventory != null || p.Equipped != null
                || Owned<ExaminablePart>(owner) == null || !SingleCell(owner)
                || !OptionalOwned<StatusEffectsPart>(owner)) return false;
            if (owner.HasPart<StackerPart>())
            { var stack = Owned<StackerPart>(owner); if (stack == null || stack.StackCount <= 0) return false; }
            return m != null && m.MaterialID == "Steam" && m.Conductivity == 65f
                && m.Combustibility == 0f && m.Porosity == 1f && m.HasMaterialTag("Vapor") && m.HasMaterialTag("Water")
                && t != null && float.IsFinite(t.Temperature) && t.VaporTemperature == 100f && t.HeatCapacity == 1.8f
                && s != null && s.Coating == "water" && s.CoatingTurns == 2 && s.Heat == 1
                && s.Cold == 0 && s.Charge == 0 && !s.RadiatesEnergy && string.IsNullOrEmpty(s.Residue);
        }
        static bool ForeignMeaning(Entity owner)
            => owner.HasPart<Body>() || owner.HasPart<BrainPart>() || owner.GetStat("Hitpoints") != null
                || owner.HasPart<ContainerPart>() || owner.HasPart<LockPart>() || owner.HasPart<ForgePart>()
                || owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>() || owner.HasPart<CultivatedSoilPart>()
                || owner.HasPart<HarvestablePart>() || owner.HasPart<LiquidPoolPart>() || owner.HasPart<WellPart>()
                || owner.HasPart<RepairablePart>() || owner.HasPart<DoorPart>() || owner.HasPart<ConversationPart>()
                || owner.HasPart<TraderPart>() || owner.HasPart<RegionalSignpostPart>() || owner.HasPart<CampfirePart>()
                || owner.HasPart<AlchemyStillPart>() || owner.HasPart<SoddenPreparationPart>() || owner.HasPart<KitchenBatchPart>()
                || owner.HasPart<TriggerOnStepPart>() || owner.HasPart<StairsDownPart>() || owner.HasPart<StairsUpPart>()
                || owner.HasPart<DestructiblePart>() || owner.HasPart<GasPoolPart>() || owner.HasPart<FiniteLiquidFilmPart>()
                || owner.HasPart<BurnOffGasPart>() || owner.HasPart("Restable") || owner.HasTag("Restable")
                || owner.HasTag("Plantable") || owner.HasTag("WellGroundMarker")
                || owner.HasTag("OvenGroundMarker") || owner.HasTag("LanternGroundMarker");
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
            DestroyFrame(body);
        }
        static void DestroyFrame(Tile tile)
        { if (tile == null) return; DestroyOwned(tile.sprite); DestroyOwned(tile); }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
