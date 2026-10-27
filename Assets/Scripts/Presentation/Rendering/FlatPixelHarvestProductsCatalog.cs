using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Exact portable harvest products, shared by world and authorized
    /// readers. This is semantic art, not disclosure authority. Textures remain
    /// borrowed; only cached native sprites and tiles belong to this catalog.</summary>
    public sealed class FlatPixelHarvestProductsCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/world-harvest-products";
        public const string ItemsResourcePath = "Sprites/Live16x24/opening-items";
        public const string WorksResourcePath = "Sprites/Live16x24/north-works-products";
        readonly Tile[] _products, _items, _works;
        bool _disposed;

        public FlatPixelHarvestProductsCatalog()
            : this(Resources.Load<Texture2D>(ResourcePath), Resources.Load<Texture2D>(ItemsResourcePath),
                Resources.Load<Texture2D>(WorksResourcePath)) { }

        /// <summary>Three independently optional borrowed groups. Missing or
        /// malformed injection stays unavailable without a Resources lookup.</summary>
        public FlatPixelHarvestProductsCatalog(Texture2D products8, Texture2D openingItems12, Texture2D worksProducts7)
        {
            _products = Frames(products8, 8, "Harvest product");
            _items = Frames(openingItems12, 12, "Opening item reuse");
            _works = Frames(worksProducts7, 7, "Works product reuse");
        }

        internal static bool HasKnownRole(Entity owner)
        {
            switch (owner?.BlueprintName)
            {
                case "PaleSalt": case "Bone": case "SaltbriarSprig": case "ChoirIron": case "Honeycomb":
                case "GroveRed": case "Tepuibone": case "KnotflaxCord": case "GlowQuartz": case "Mushroom":
                case "FireClay": case "SalvagedTimber": case "StoneburrSeed": case "FrostLichen":
                case "MendleafSprig": case "WildBerries": case "GoldCoin": return true;
                default: return false;
            }
        }

        /// <summary>Returns a neutral current product frame. The caller must
        /// separately authorize current world placement or inventory custody.</summary>
        public bool TryResolve(Entity owner, out Tile tile)
        {
            tile = null;
            if (_disposed || !HasKnownRole(owner) || !PlainProduct(owner)) return false;
            int frame; Tile[] group;
            switch (owner.BlueprintName)
            {
                case "PaleSalt": if (!InfusionMineral(owner)) return false; group = _products; frame = 0; break;
                case "Bone": if (Owned<HandlingPart>(owner) == null) return false; group = _products; frame = 1; break;
                case "SaltbriarSprig": if (!Meal(owner)) return false; group = _products; frame = 2; break;
                case "ChoirIron": if (!InfusionMineral(owner)) return false; group = _products; frame = 3; break;
                case "Honeycomb": if (!Meal(owner)) return false; group = _products; frame = 4; break;
                case "GroveRed":
                    if (!Reagent(owner, BrewProperties.Vital) || !Reagent(owner, BrewProperties.Toxic)
                        || !Recipe(owner, "detox_grove_red")) return false;
                    group = _products; frame = 5; break;
                case "Tepuibone":
                    if (!owner.HasTag("Mineral") || !Recipe(owner, "shape_head")) return false;
                    group = _products; frame = 6; break;
                case "KnotflaxCord":
                    if (!Recipe(owner, "braid_binding") || !Recipe(owner, "bandage")) return false;
                    group = _products; frame = 7; break;
                case "GlowQuartz": if (!InfusionMineral(owner)) return false; group = _items; frame = 6; break;
                case "Mushroom":
                    if (!Meal(owner) || Owned<CookablePart>(owner)?.Into != "RoastedMushroom") return false;
                    group = _items; frame = 4; break;
                case "FireClay": group = _items; frame = 9; break;
                case "SalvagedTimber": if (!Recipe(owner, "shape_haft")) return false; group = _items; frame = 10; break;
                case "StoneburrSeed": if (!Reagent(owner, BrewProperties.Binding)) return false; group = _works; frame = 0; break;
                case "FrostLichen": if (!Reagent(owner, BrewProperties.Cold)) return false; group = _works; frame = 1; break;
                case "MendleafSprig":
                    if (!Reagent(owner, BrewProperties.Vital) || !Recipe(owner, "concentrate_mendleaf")) return false;
                    group = _works; frame = 2; break;
                case "WildBerries": if (!Meal(owner)) return false; group = _works; frame = 3; break;
                case "GoldCoin": group = _works; frame = 4; break;
                default: return false;
            }
            tile = group == null ? null : group[frame];
            return tile != null;
        }

        static bool PlainProduct(Entity owner)
        {
            if (!owner.HasTag("Item") || owner.HasTag("Creature") || owner.HasTag("Player")
                || owner.HasTag("Terrain") || owner.HasTag("Crop") || owner.HasPart<CropPart>()) return false;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            var stack = Owned<StackerPart>(owner);
            if (render == null || !render.Visible || physics == null || !physics.Takeable || physics.Solid
                || stack == null || stack.StackCount <= 0 || stack.StackCount > stack.MaxStack
                || Owned<CommercePart>(owner) == null || !string.IsNullOrEmpty(render.VisualID)
                || !string.IsNullOrEmpty(render.VisualVariant) || !string.IsNullOrEmpty(render.GlyphVariants)) return false;
            if (owner.HasPart<SpatialFootprintPart>())
            {
                var footprint = Owned<SpatialFootprintPart>(owner);
                if (footprint == null || footprint.Offsets.Length != 1 || footprint.Offsets[0] != Vector2Int.zero
                    || owner.SpatialZone != null && !owner.SpatialZone.IsFootprintCurrent(owner)) return false;
            }
            // Observe existing color state without consuming a Render event.
            // Carried products legitimately have no spatial zone or material.
            if (owner.HasPart<StatusEffectsPart>())
            {
                var effects = Owned<StatusEffectsPart>(owner);
                if (effects == null || effects.GetRenderColorOverride() != null) return false;
            }
            return true;
        }
        static bool InfusionMineral(Entity owner) => owner.HasTag("Mineral") && Owned<MineralPreparationPart>(owner) != null;
        static bool Meal(Entity owner) => owner.HasTag("Food") && Owned<FoodPart>(owner)?.Cooking == "Meal";
        static bool Reagent(Entity owner, string expected)
        {
            var part = Owned<ReagentPart>(owner);
            if (part == null) return false;
            var properties = part.GetProperties(); // Parsed and invalidated by the actual owner.
            for (int i = 0; i < properties.Count; i++)
            { var property = properties[i]; if (property != null && property.Potency > 0 && property.Property == expected) return true; }
            return false;
        }
        static bool Recipe(Entity owner, string expected)
        {
            var part = Owned<PreparationRecipePart>(owner); var raw = part?.RecipeIds;
            if (string.IsNullOrEmpty(raw)) return false;
            // Match the authored exact comma-token semantics of Allows without
            // allocating Split arrays or executing preparation during drawing.
            for (int start = 0; start < raw.Length;)
            {
                int end = raw.IndexOf(',', start); if (end < 0) end = raw.Length;
                if (end - start == expected.Length && string.CompareOrdinal(raw, start, expected, 0, expected.Length) == 0) return true;
                start = end + 1;
            }
            return false;
        }
        static T Owned<T>(Entity owner) where T : Part
        {
            T found = null;
            for (int i = 0; i < owner.Parts.Count; i++)
                if (owner.Parts[i] is T part)
                { if (found != null || part.ParentEntity != owner) return null; found = part; }
            return found;
        }
        static Tile[] Frames(Texture2D texture, int count, string name)
        {
            if (texture == null || texture.width != count * 16 || texture.height != 24) return null;
            var tiles = new Tile[count];
            for (int i = 0; i < count; i++)
            {
                var sprite = Sprite.Create(texture, new Rect(i * 16, 0, 16, 24), new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
                sprite.name = "Live16x24 " + name + " " + i; sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name; tile.hideFlags = HideFlags.HideAndDontSave;
                tile.sprite = sprite; tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None; tiles[i] = tile;
            }
            return tiles;
        }
        public void Dispose()
        {
            if (_disposed) return; _disposed = true;
            DestroyFrames(_products); DestroyFrames(_items); DestroyFrames(_works);
        }
        static void DestroyFrames(Tile[] frames)
        {
            if (frames == null) return;
            for (int i = 0; i < frames.Length; i++)
                if (frames[i] != null) { DestroyOwned(frames[i].sprite); DestroyOwned(frames[i]); }
        }
        static void DestroyOwned(Object value)
        { if (value != null) { if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); } }
    }
}
