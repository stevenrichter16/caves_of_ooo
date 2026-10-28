using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Neutral bodies for five exact portable supplies. World disclosure
    /// and reader custody remain caller-owned; resolving art never uses the item.</summary>
    public sealed class FlatPixelExpeditionEssentialsCatalog : IDisposable
    {
        public const string ItemsResourcePath = "Sprites/Live16x24/opening-items";
        public const string BookResourcePath = "Sprites/Environment/item_book";
        public const string PlotProductsResourcePath = "Sprites/Live16x24/plot-products";
        readonly Tile tonic, meat, book, gladrootSeed, emberwheatSeed;
        bool disposed;

        public FlatPixelExpeditionEssentialsCatalog()
            : this(Resources.Load<Texture2D>(ItemsResourcePath), Resources.Load<Texture2D>(BookResourcePath),
                Resources.Load<Texture2D>(PlotProductsResourcePath)) { }

        /// <summary>Independent optional borrowed textures. Missing/malformed
        /// input never reloads Resources or changes the original importer/pixels.</summary>
        public FlatPixelExpeditionEssentialsCatalog(Texture2D items, Texture2D bookTexture)
            : this(items, bookTexture, null) { }

        public FlatPixelExpeditionEssentialsCatalog(Texture2D items, Texture2D bookTexture, Texture2D plotProducts)
        {
            if (items != null && items.width == 192 && items.height == 24)
            {
                tonic = Build(items, new Rect(112, 0, 16, 24), "healing tonic");
                meat = Build(items, new Rect(128, 0, 16, 24), "dried meat");
            }
            if (bookTexture != null && bookTexture.width == 16 && bookTexture.height == 16)
                book = Build(bookTexture, new Rect(0, 0, 16, 16), "watering grimoire");
            if (plotProducts != null && plotProducts.width == 128 && plotProducts.height == 24)
            {
                gladrootSeed = Build(plotProducts, new Rect(16, 0, 16, 24), "gladroot seed");
                emberwheatSeed = Build(plotProducts, new Rect(32, 0, 16, 24), "emberwheat seed");
            }
        }

        public static bool HasKnownRole(Entity owner)
            => owner?.BlueprintName == "HealingTonic" || owner?.BlueprintName == "DriedMeat"
                || owner?.BlueprintName == "WateringGrimoire" || owner?.BlueprintName == "CandyCarrotSeed"
                || owner?.BlueprintName == "EmberwheatSeed";

        public bool TryResolve(Entity owner, out Tile tile)
        {
            tile = null;
            if (disposed || !HasKnownRole(owner) || !owner.HasTag("Item")
                || owner.HasTag("Creature") || owner.HasTag("Player") || owner.HasTag("Terrain")
                || owner.HasTag("Crop") || owner.HasTag("Corpse") || ForeignMeaning(owner)) return false;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            if (render == null || !render.Visible || physics == null || !physics.Takeable || physics.Solid
                || Owned<ExaminablePart>(owner) == null || !string.IsNullOrEmpty(render.VisualID)
                || !string.IsNullOrEmpty(render.VisualVariant) || !string.IsNullOrEmpty(render.GlyphVariants)
                || !SingleCell(owner) || !OptionalOwned<StatusEffectsPart>(owner)) return false;
            if (owner.HasPart<StackerPart>())
            {
                var stack = Owned<StackerPart>(owner);
                if (stack == null || stack.StackCount <= 0 || stack.StackCount > stack.MaxStack) return false;
            }
            switch (owner.BlueprintName)
            {
                case "HealingTonic":
                    if (!owner.HasTag("Tonic") || string.IsNullOrWhiteSpace(Owned<TonicPart>(owner)?.Healing)) return false;
                    tile = tonic; break;
                case "DriedMeat":
                    if (!owner.HasTag("Food") || Owned<FoodPart>(owner) == null) return false;
                    tile = meat; break;
                case "WateringGrimoire":
                    if (!owner.HasTag("Grimoire") || Owned<GrimoirePart>(owner)?.SkillClassName != "Hydromancy_ConjureRain") return false;
                    tile = book; break;
                case "CandyCarrotSeed":
                    if (!MatchesSeed(owner, "CandyCarrotCrop")) return false;
                    tile = gladrootSeed; break;
                case "EmberwheatSeed":
                    if (!MatchesSeed(owner, "EmberwheatCrop")) return false;
                    tile = emberwheatSeed; break;
            }
            return tile != null;
        }

        static bool MatchesSeed(Entity owner, string crop)
        {
            var seed = Owned<SeedPart>(owner);
            return seed != null && seed.CropBlueprint == crop && !seed.RequireCultivatedSoil;
        }

        static bool ForeignMeaning(Entity owner)
            => owner.HasPart<Body>() || owner.HasPart<BrainPart>() || owner.HasPart<TraderPart>()
                || owner.HasPart<ConversationPart>() || owner.HasPart<ContainerPart>() || owner.HasPart<DoorPart>()
                || owner.HasPart<ForgePart>() || owner.HasPart<WellPart>() || owner.HasPart<WellSitePart>()
                || owner.HasPart<AlchemyStillPart>() || owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>()
                || owner.HasPart<CultivatedSoilPart>() || owner.HasPart<CorpsePart>() || owner.HasPart<EquippablePart>()
                || owner.HasPart<MeleeWeaponPart>() || owner.HasPart<ArmorPart>();

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
            var footprint = Owned<SpatialFootprintPart>(owner);
            return footprint != null && footprint.Offsets.Length == 1 && footprint.Offsets[0] == Vector2Int.zero
                && (owner.SpatialZone == null || owner.SpatialZone.IsFootprintCurrent(owner));
        }
        static Tile Build(Texture2D texture, Rect rect, string name)
        {
            var sprite = Sprite.Create(texture, rect, new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
            sprite.name = "Live16x24 " + name; sprite.hideFlags = HideFlags.HideAndDontSave;
            var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
            tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
            tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None;
            return tile;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            DestroyFrame(tonic); DestroyFrame(meat); DestroyFrame(book); DestroyFrame(gladrootSeed); DestroyFrame(emberwheatSeed);
        }
        static void DestroyFrame(Tile tile)
        { if (tile != null) { DestroyOwned(tile.sprite); DestroyOwned(tile); } }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
