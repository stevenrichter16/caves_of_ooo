using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Exact neutral stone architecture. Borrows approved textures;
    /// owns cached views only. Callers retain placement, disclosure and selection.</summary>
    public sealed class FlatPixelSharedStonePropsCatalog : IDisposable
    {
        readonly Tile pillar, broken;
        bool disposed;
        public FlatPixelSharedStonePropsCatalog()
            : this(Resources.Load<Texture2D>("Sprites/Environment/pillar"),
                Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.ResourcePath)) { }
        public FlatPixelSharedStonePropsCatalog(Texture2D pillar16, Texture2D openingBase10)
        {
            pillar = Build(pillar16, 16, 16, 0, "pillar");
            broken = Build(openingBase10, 160, 24, 96, "broken column");
        }
        public static bool HasKnownRole(Entity owner)
            => owner?.BlueprintName == "Pillar" || owner?.BlueprintName == "BrokenColumn";

        // Preserve only the already-authored glade body. Other custom identities
        // remain known refusals, so weaker local fallbacks cannot bypass guards.
        public static bool UsesAuthoredGladeBody(Entity owner)
            => owner?.BlueprintName == "BrokenColumn" && ValidBody(owner, true);

        /// <summary>A neutral fragment overlays an existing real floor; it never
        /// creates terrain or authorizes hazards, services or authored variants.</summary>
        public static bool IsNeutralFloorOverlay(Entity owner)
            => owner?.BlueprintName == "BrokenColumn" && ValidBody(owner, false);

        public bool TryResolve(Entity owner, out Tile tile)
        {
            tile = null;
            if (disposed || !ValidBody(owner, false)) return false;
            tile = owner.BlueprintName == "Pillar" ? pillar : broken;
            return tile != null;
        }
        static bool ValidBody(Entity owner, bool glade)
        {
            if (!HasKnownRole(owner) || owner.HasTag("Player") || owner.HasTag("Creature")
                || owner.HasTag("Item") || owner.HasTag("Crop") || ForeignMeaning(owner)) return false;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            if (render == null || !render.Visible || physics == null || physics.Takeable
                || physics.InInventory != null || physics.Equipped != null || Owned<ExaminablePart>(owner) == null
                || render.ColorString != "&w" || !string.IsNullOrEmpty(render.BackgroundColor)
                || !string.IsNullOrEmpty(render.VisualVariant) || !string.IsNullOrEmpty(render.GlyphVariants)
                || !SingleCell(owner) || !OptionalOwned<StatusEffectsPart>(owner)
                || (glade ? render.VisualID != "reference-glade-dark-ruin" : !string.IsNullOrEmpty(render.VisualID))) return false;
            if (owner.HasPart<StackerPart>())
            { var stack = Owned<StackerPart>(owner); if (stack == null || stack.StackCount <= 0) return false; }
            if (owner.BlueprintName == "BrokenColumn")
                return owner.HasTag("Terrain") && !owner.HasTag("Solid") && !physics.Solid
                    && render.RenderLayer == 0 && render.RenderString == ","
                    && !owner.HasPart<MaterialPart>() && !owner.HasPart<ThermalPart>() && !owner.HasPart<DestructiblePart>();
            if (glade || owner.HasTag("Terrain") || !owner.HasTag("Solid") || !physics.Solid
                || render.RenderLayer != 1 || render.RenderString != "I") return false;
            var material = Owned<MaterialPart>(owner); var structure = Owned<DestructiblePart>(owner);
            var thermal = Owned<ThermalPart>(owner);
            return material != null && material.MaterialID == "Stone" && material.Combustibility == 0f
                && material.HasMaterialTag("Stone") && material.HasMaterialTag("Mineral")
                && owner.HasTag("Stone") && owner.HasTag("Mineral")
                && structure != null && !structure.Gone && !structure.Indestructible
                && structure.HP > 0 && structure.HP <= 50 && structure.MaxHP == 50
                && structure.Hardness == 4 && structure.WreckageBlueprint == "Rubble"
                && thermal != null && float.IsFinite(thermal.Temperature)
                && float.IsFinite(thermal.FreezeTemperature) && thermal.Temperature > thermal.FreezeTemperature
                && thermal.Temperature < thermal.FlameTemperature && thermal.FlameTemperature == 1200f
                && thermal.HeatCapacity == 3f;
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
                || owner.HasPart<TileStateSourcePart>() || owner.HasPart<GasPoolPart>() || owner.HasPart<FiniteLiquidFilmPart>()
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
            DestroyFrame(pillar); DestroyFrame(broken);
        }
        static void DestroyFrame(Tile tile)
        { if (tile == null) return; DestroyOwned(tile.sprite); DestroyOwned(tile); }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
