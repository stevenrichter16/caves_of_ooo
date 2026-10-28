using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;
namespace CavesOfOoo.Rendering
{
    /// <summary>Three exact camp bodies. Queries never open, stock, drink or repair;
    /// callers retain current graph, disclosure and subject authorization.</summary>
    public sealed class FlatPixelSharedCampPropsCatalog : IDisposable
    {
        readonly Tile sack, urn, well;
        bool disposed;
        public FlatPixelSharedCampPropsCatalog()
            : this(Resources.Load<Texture2D>("Sprites/Environment/cont_sack"),
                Resources.Load<Texture2D>("Sprites/Environment/cont_urn"),
                Resources.Load<Texture2D>("Sprites/Environment/well")) { }
        public FlatPixelSharedCampPropsCatalog(Texture2D sack16, Texture2D urn16, Texture2D well16)
        { sack = Build(sack16, "sack"); urn = Build(urn16, "urn"); well = Build(well16, "well"); }
        public static bool HasKnownRole(Entity owner)
            => owner?.BlueprintName == "Sack" || owner?.BlueprintName == "Urn" || owner?.BlueprintName == "Well";
        public bool TryResolve(Entity owner, out Tile tile)
        {
            tile = null;
            if (disposed || !HasKnownRole(owner) || owner.HasTag("Creature") || owner.HasTag("Player")
                || owner.HasTag("Item") || owner.HasTag("Terrain") || owner.HasTag("Crop")
                || owner.GetStat("Hitpoints") != null || ForeignMeaning(owner)) return false;
            var r = Owned<RenderPart>(owner); var p = Owned<PhysicsPart>(owner);
            bool isWell = owner.BlueprintName == "Well", isSack = owner.BlueprintName == "Sack";
            if (r == null || !r.Visible || r.RenderLayer != 1 || r.RenderString != (isWell ? "O" : isSack ? "u" : "8")
                || r.ColorString != (isWell ? "&c" : isSack ? "&w" : "&y")
                || !string.IsNullOrEmpty(r.BackgroundColor) || !string.IsNullOrEmpty(r.VisualID)
                || !string.IsNullOrEmpty(r.VisualVariant) || !string.IsNullOrEmpty(r.GlyphVariants)
                || p == null || p.Takeable || p.InInventory != null || p.Equipped != null
                || Owned<ExaminablePart>(owner) == null || !SingleCell(owner)
                || !OptionalOwned<StatusEffectsPart>(owner) || !OptionalOwned<LockPart>(owner)) return false;
            if (owner.HasPart<StackerPart>())
            { var stack = Owned<StackerPart>(owner); if (stack == null || stack.StackCount <= 0) return false; }
            if (isWell)
            {
                // Ordinary wells have no finite pool or settlement-stage body.
                if (Owned<WellPart>(owner) == null || owner.HasPart<ContainerPart>() || owner.HasPart<LockPart>()) return false;
                tile = well;
            }
            else
            {
                if (Owned<ContainerPart>(owner) == null || owner.HasPart<WellPart>()) return false;
                tile = isSack ? sack : urn;
            }
            return tile != null;
        }
        static bool ForeignMeaning(Entity owner)
            => owner.HasPart<BrainPart>() || owner.HasPart<Body>() || owner.HasPart<TraderPart>()
                || owner.HasPart<ConversationPart>() || owner.HasPart<WellSitePart>() || owner.HasPart<RepairablePart>()
                || owner.HasPart<LiquidPoolPart>() || owner.HasPart<DestructiblePart>() || owner.HasPart<ForgePart>()
                || owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>() || owner.HasPart<CultivatedSoilPart>()
                || owner.HasPart<HarvestablePart>() || owner.HasPart<DoorPart>() || owner.HasPart<RegionalSignpostPart>()
                || owner.HasPart<CampfirePart>() || owner.HasPart<AlchemyStillPart>() || owner.HasPart<SoddenPreparationPart>()
                || owner.HasPart<TriggerOnStepPart>() || owner.HasPart<StairsDownPart>() || owner.HasPart<StairsUpPart>()
                || owner.HasPart<GasPoolPart>() || owner.HasPart<TileStateSourcePart>() || owner.HasPart<FiniteLiquidFilmPart>()
                || owner.HasPart<BurnOffGasPart>() || owner.HasPart("Restable") || owner.HasTag("Restable")
                || owner.HasTag("Plantable") || owner.HasTag("WellGroundMarker") || owner.HasTag("OvenGroundMarker")
                || owner.HasTag("LanternGroundMarker");
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
        static Tile Build(Texture2D texture, string label)
        {
            if (texture == null || texture.width != 16 || texture.height != 16) return null;
            var sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
            sprite.name = "Live16x24 shared " + label; sprite.hideFlags = HideFlags.HideAndDontSave;
            var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
            tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
            tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None;
            return tile;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            DestroyFrame(sack); DestroyFrame(urn); DestroyFrame(well);
        }
        static void DestroyFrame(Tile tile)
        { if (tile == null) return; DestroyOwned(tile.sprite); DestroyOwned(tile); }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
