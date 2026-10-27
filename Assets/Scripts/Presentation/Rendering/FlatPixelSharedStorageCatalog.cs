using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Five exact storage bodies and the live chest mimic's identical
    /// disguise. Borrows four immutable textures; owns only cached views.
    /// Callers retain current graph, placement, selection and disclosure authority.</summary>
    public sealed class FlatPixelSharedStorageCatalog : IDisposable
    {
        readonly Tile chest, crate, strongBox, oreCache;
        bool disposed;

        public FlatPixelSharedStorageCatalog()
            : this(Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.AnchorsResourcePath),
                Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.CellarResourcePath),
                Resources.Load<Texture2D>("Sprites/Environment/cont_strongbox"),
                Resources.Load<Texture2D>("Sprites/Environment/cont_ore_cache")) { }

        /// <summary>Independent optional resources. Explicit null does not reload.
        /// Square sources use their full original 16x16 bounds at 16 PPU.</summary>
        public FlatPixelSharedStorageCatalog(Texture2D openingAnchors, Texture2D openingCellar,
            Texture2D strongBoxTexture, Texture2D oreCacheTexture)
        {
            chest = Build(openingAnchors, 192, 24, 96, "chest");
            crate = Build(openingCellar, 144, 24, 16, "crate");
            strongBox = Build(strongBoxTexture, 16, 16, 0, "strongbox");
            oreCache = Build(oreCacheTexture, 16, 16, 0, "ore cache");
        }

        /// <summary>Identity only, not permission to reveal or render an owner.</summary>
        public static bool HasKnownRole(Entity owner)
            => owner?.BlueprintName == "Chest" || owner?.BlueprintName == "LockedChest"
                || owner?.BlueprintName == "Crate" || owner?.BlueprintName == "StrongBox"
                || owner?.BlueprintName == "OreCache" || owner?.BlueprintName == "MimicChest";

        /// <summary>Pure invariant body lookup. Locks, contents, damage and mimic
        /// goals never select a new frame or mutate gameplay. Existing Render
        /// event cues are supplied once by the painter, not dispatched here.</summary>
        public bool TryResolve(Entity owner, out Tile tile)
        {
            tile = null;
            if (disposed || !HasKnownRole(owner) || owner.HasTag("Player") || owner.HasTag("Item")
                || owner.HasTag("Terrain") || owner.HasTag("Crop") || ForeignMeaning(owner)) return false;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            if (render == null || !render.Visible || physics == null || physics.Takeable
                || physics.InInventory != null || physics.Equipped != null
                || Owned<ContainerPart>(owner) == null || Owned<ExaminablePart>(owner) == null
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)
                || !string.IsNullOrEmpty(render.GlyphVariants) || !SingleCell(owner)
                || !OptionalOwned<LockPart>(owner) || !OptionalOwned<StatusEffectsPart>(owner)) return false;
            if (owner.HasPart<StackerPart>())
            { var stack = Owned<StackerPart>(owner); if (stack == null || stack.StackCount <= 0) return false; }
            if (owner.BlueprintName == "MimicChest")
            {
                var hp = owner.GetStat("Hitpoints");
                if (!owner.HasTag("Creature") || render.RenderString != "="
                    || Owned<Body>(owner) == null || Owned<BrainPart>(owner) == null
                    || Owned<AIAmbushPart>(owner) == null || hp == null || hp.Owner != owner || hp.BaseValue <= 0)
                    return false;
                // Awake and dormant use the same body; no goal scan or early tell.
                tile = chest;
                return tile != null;
            }
            if (owner.HasTag("Creature") || owner.HasPart<BrainPart>() || owner.HasPart<Body>()
                || owner.GetStat("Hitpoints") != null || render.RenderLayer != 1) return false;
            switch (owner.BlueprintName)
            {
                case "Chest":
                    if (render.RenderString == "=" && WoodBody(owner, 14, 1, true)) tile = chest;
                    break;
                case "LockedChest":
                    if (render.RenderString == "=" && Owned<LockPart>(owner) != null
                        && WoodBody(owner, 18, 2, true)) tile = chest;
                    break;
                case "Crate":
                    if (render.RenderString == "0" && WoodBody(owner, 10, 0, false)) tile = crate;
                    break;
                case "StrongBox":
                    if (render.RenderString == "=" && Owned<LockPart>(owner) != null && NoStructure(owner)) tile = strongBox;
                    break;
                case "OreCache":
                    if (render.RenderString == "*" && NoStructure(owner)) tile = oreCache;
                    break;
            }
            return tile != null;
        }

        static bool WoodBody(Entity owner, int maximum, int hardness, bool thermal)
        {
            var material = Owned<MaterialPart>(owner); var structure = Owned<DestructiblePart>(owner);
            return material != null && material.MaterialID == "Wood" && structure != null
                && !structure.Gone && !structure.Indestructible && structure.HP > 0 && structure.HP <= structure.MaxHP
                && structure.MaxHP == maximum && structure.Hardness == hardness
                && string.IsNullOrEmpty(structure.WreckageBlueprint)
                && (thermal ? Owned<ThermalPart>(owner) != null : !owner.HasPart<ThermalPart>());
        }
        static bool NoStructure(Entity owner)
            => !owner.HasPart<MaterialPart>() && !owner.HasPart<DestructiblePart>() && !owner.HasPart<ThermalPart>();
        static bool ForeignMeaning(Entity owner)
            => owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>() || owner.HasPart<CultivatedSoilPart>()
                || owner.HasPart<HarvestablePart>() || owner.HasPart<LiquidPoolPart>() || owner.HasPart<WellPart>()
                || owner.HasPart<RepairablePart>() || owner.HasPart<DoorPart>() || owner.HasPart<ConversationPart>()
                || owner.HasPart<TraderPart>() || owner.HasPart<RegionalSignpostPart>() || owner.HasPart<CampfirePart>()
                || owner.HasPart<AlchemyStillPart>() || owner.HasPart<SoddenPreparationPart>() || owner.HasPart<TriggerOnStepPart>()
                || owner.HasPart<StairsDownPart>() || owner.HasPart<StairsUpPart>() || owner.HasPart<TileStateSourcePart>()
                || owner.HasPart<GasPoolPart>() || owner.HasPart<FiniteLiquidFilmPart>() || owner.HasPart<BurnOffGasPart>()
                || owner.HasPart("Restable") || owner.HasTag("Restable") || owner.HasTag("Plantable")
                || owner.HasTag("WellGroundMarker") || owner.HasTag("OvenGroundMarker") || owner.HasTag("LanternGroundMarker");
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
            DestroyFrame(chest); DestroyFrame(crate); DestroyFrame(strongBox); DestroyFrame(oreCache);
        }
        static void DestroyFrame(Tile tile)
        { if (tile == null) return; DestroyOwned(tile.sprite); DestroyOwned(tile); }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
