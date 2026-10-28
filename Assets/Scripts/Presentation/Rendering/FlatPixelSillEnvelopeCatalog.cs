using System;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Visible materials and selected static memory for the actual Sill graph. Borrows
    /// immutable source textures and the current-owner selector; owns only
    /// cached sprites/tiles. Never adds geometry, changes state or expands the opening.</summary>
    public sealed class FlatPixelSillEnvelopeCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/sill-envelope";
        public const string EnvironmentResourcePath = "Sprites/Live16x24/sill-environment";
        public const string ZoneID = "Overworld.10.10.0";
        const int Width = 16, Height = 24;
        readonly FlatPixelFallbackCatalog selector;
        readonly Tile[] envelope, masonry, works, stoneFloor, environment;
        bool disposed;

        public FlatPixelSillEnvelopeCatalog(FlatPixelFallbackCatalog borrowedSelector)
            : this(Resources.Load<Texture2D>(ResourcePath),
                Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.CellarResourcePath),
                Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.WorksResourcePath),
                Resources.Load<Texture2D>(FlatPixelCellarFloorCatalog.ResourcePath), borrowedSelector,
                Resources.Load<Texture2D>(EnvironmentResourcePath)) { }

        /// <summary>Each injected group is independent. Missing/malformed
        /// injection never reloads Resources or modifies a borrowed dependency.</summary>
        public FlatPixelSillEnvelopeCatalog(Texture2D source, Texture2D cellar,
            Texture2D northWorks, Texture2D floors, FlatPixelFallbackCatalog borrowedSelector)
            : this(source, cellar, northWorks, floors, borrowedSelector, null) { }

        /// <summary>Optional 144x24 atlas: Grass 0–3, StoneFloor 4–7, StoneWall 8.
        /// Null or malformed art preserves each legacy group. Textures and selector
        /// remain borrowed; only cached tile/sprite views are owned here.</summary>
        public FlatPixelSillEnvelopeCatalog(Texture2D source, Texture2D cellar,
            Texture2D northWorks, Texture2D floors, FlatPixelFallbackCatalog borrowedSelector,
            Texture2D settlementMaterials)
        {
            selector = borrowedSelector;
            if (selector == null) return;
            envelope = Frames(source, 24, "envelope");
            masonry = Frames(cellar, 9, "stone");
            works = Frames(northWorks, 9, "still");
            stoneFloor = Frames(floors, 4, "floor");
            environment = Frames(settlementMaterials, 9, "environment");
        }

        public static bool IsSupportedZone(Zone zone)
        {
            if (zone?.ZoneID != ZoneID) return false;
            var manager = WorldLocationContext.For(zone);
            if (manager?.CachedZones == null || !manager.CachedZones.TryGetValue(ZoneID, out var current)
                || !ReferenceEquals(current, zone)) return false;
            var map = manager.WorldMap;
            if (map?.Tiles == null || map.POIs == null
                || map.Tiles.GetLength(0) != WorldMap.Width || map.Tiles.GetLength(1) != WorldMap.Height
                || map.POIs.GetLength(0) != WorldMap.Width || map.POIs.GetLength(1) != WorldMap.Height
                || map.GetBiome(10, 10) != BiomeType.Spread) return false;
            var poi = map.GetPOI(10, 10);
            return poi != null && poi.Type == POIType.Village && poi.Name == "Sill"
                && poi.Faction == "Villagers" && string.IsNullOrEmpty(poi.Profile);
        }

        /// <summary>Only the fallback-selected static identity may retain its
        /// approved material in memory. This is not a last-seen snapshot: no
        /// live substrate, wall condition or neighboring cell is inspected.
        /// The caller applies one memory tint and grants no portrait/collision authority.</summary>
        public bool TryResolveRemembered(Zone zone, int x, int y, Entity owner,
            out Tile groundTile, out Tile foregroundTile)
        {
            groundTile = null; foregroundTile = null;
            if (disposed || selector == null || !IsSupportedZone(zone)) return false;
            var cell = zone.GetCell(x, y);
            if (cell == null || cell.ParentZone != zone || cell.X != x || cell.Y != y
                || !cell.Explored || cell.IsVisible || owner == null
                || !selector.TryResolve(zone, x, y, out var selected)
                || !selected.Remembered || !ReferenceEquals(selected.Owner, owner)) return false;

            bool grass = owner.BlueprintName == "Grass";
            bool stone = owner.BlueprintName == "StoneFloor";
            bool wall = owner.BlueprintName == "StoneWall";
            bool vine = owner.BlueprintName == "VineWall";
            if (!(grass || stone || wall || vine) || !PlainOwner(owner, grass || stone)) return false;
            if (grass || stone)
            {
                if (!owner.HasTag("Terrain") || owner.HasTag("Wall")
                    || (grass ? !owner.HasTag("Plantable") || !owner.HasTag("Vegetation")
                        : owner.HasTag("Vegetation"))) return false;
                int variant = VariantAt(x, y);
                groundTile = environment != null ? environment[(stone ? 4 : 0) + variant]
                    : stone ? stoneFloor == null ? null : stoneFloor[variant]
                    : envelope == null ? null : envelope[20 + variant];
            }
            else
            {
                // Ownership/category checks are static. Do not call the live
                // ForegroundFrame: it reads HP/Gone and current solidity.
                if (Owned<RenderPart>(owner).RenderString != "#" || !owner.HasTag("Wall")
                    || Owned<DestructiblePart>(owner) == null
                    || vine && !owner.HasTag("Vegetation")) return false;
                foregroundTile = vine ? envelope == null ? null : envelope[0]
                    : environment != null ? environment[8] : masonry == null ? null : masonry[8];
            }
            return groundTile != null || foregroundTile != null;
        }

        public bool TryResolveForeground(Zone zone, int x, int y, Entity owner, out Tile tile)
        {
            tile = null;
            if (!ReadyCell(zone, x, y, out _) || !Selected(zone, x, y, owner)) return false;
            int frame = ForegroundFrame(owner);
            if (frame < 0) return false;
            if (owner.BlueprintName == "StoneWall")
                tile = environment != null ? environment[8] : masonry == null ? null : masonry[8];
            else if (owner.BlueprintName == "AlchemyStill") tile = works == null ? null : works[0];
            else if (envelope != null)
            {
                if (owner.BlueprintName == "VineWall")
                {
                    frame = 0;
                    if (VineAt(zone, x, y - 1)) frame |= 1;
                    if (VineAt(zone, x + 1, y)) frame |= 2;
                    if (VineAt(zone, x, y + 1)) frame |= 4;
                    if (VineAt(zone, x - 1, y)) frame |= 8;
                }
                tile = envelope[frame];
            }
            return tile != null;
        }

        /// <summary>The lower actual material remains independent of a covering
        /// actor/item. Specific ground owners, cultivation and hazards outrank it.</summary>
        public bool TryResolveGround(Zone zone, int x, int y, out Tile tile, out Entity floor)
        {
            tile = null; floor = null;
            if (!ReadyCell(zone, x, y, out var cell)) return false;
            var state = zone.TileState.Get(x, y);
            if (state != null && !state.IsEmpty) return false;
            // Owner-local cultivation is checked below with disclosed owned parts.
            // The gameplay helper ignores Render visibility and would leak hidden owners.
            Entity grass = null, stone = null;
            foreach (var owner in cell.Occupants)
            {
                if (!Placed(zone, cell, owner)) continue;
                // An undisclosed or non-world occupant cannot reveal itself by
                // changing the visible substrate. Malformed actual floors still
                // take the conservative ambiguity path below.
                if (VisiblePhysicalOwner(owner) && CompetingGround(owner)) return false;
                if (owner.BlueprintName == "Grass")
                {
                    if (grass != null || !OrdinaryFloor(owner, true)) return false;
                    grass = owner;
                }
                else if (owner.BlueprintName == "StoneFloor")
                {
                    if (stone != null || !OrdinaryFloor(owner, false)) return false;
                    stone = owner;
                }
            }
            if (stone != null)
            { floor = stone; tile = environment != null ? environment[4 + VariantAt(x, y)]
                : stoneFloor == null ? null : stoneFloor[VariantAt(x, y)]; }
            else if (grass != null)
            { floor = grass; tile = environment != null ? environment[VariantAt(x, y)]
                : envelope == null ? null : envelope[20 + VariantAt(x, y)]; }
            if (tile != null) return true;
            floor = null; return false;
        }

        /// <summary>Only the same currently selected visible world subject may
        /// borrow this portrait. Caller still owns examine-document authorization.</summary>
        public bool TryGetSubjectSprite(Zone zone, Entity owner, int x, int y, out Sprite sprite)
        {
            sprite = null;
            if (!ReadyCell(zone, x, y, out _) || !Selected(zone, x, y, owner)) return false;
            if (owner.BlueprintName == "VineWall")
            {
                if (envelope == null || ForegroundFrame(owner) < 0) return false;
                sprite = envelope[0].sprite; return sprite != null;
            }
            if (TryResolveForeground(zone, x, y, owner, out var foreground))
            { sprite = foreground.sprite; return sprite != null; }
            if (TryResolveGround(zone, x, y, out var ground, out var floor) && floor == owner)
            { sprite = ground.sprite; return sprite != null; }
            return false;
        }

        bool ReadyCell(Zone zone, int x, int y, out Cell cell)
        {
            cell = null;
            if (disposed || selector == null || !IsSupportedZone(zone)) return false;
            cell = zone.GetCell(x, y);
            return cell != null && cell.ParentZone == zone && cell.X == x && cell.Y == y
                && cell.Explored && cell.IsVisible;
        }
        bool Selected(Zone zone, int x, int y, Entity owner)
            => owner != null && selector.TryResolve(zone, x, y, out var value)
                && !value.Remembered && ReferenceEquals(value.Owner, owner);
        bool VineAt(Zone zone, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            return cell != null && cell.Explored && cell.IsVisible
                && selector.TryResolve(zone, x, y, out var value) && !value.Remembered
                && value.Owner?.BlueprintName == "VineWall" && ForegroundFrame(value.Owner) == 0;
        }
        static int ForegroundFrame(Entity owner)
        {
            if (!PlainOwner(owner, false)) return -1;
            var physics = Owned<PhysicsPart>(owner);
            var render = Owned<RenderPart>(owner);
            switch (owner.BlueprintName)
            {
                case "StoneWall":
                case "VineWall":
                    var structure = Owned<DestructiblePart>(owner);
                    if (render.RenderString != "#" || !owner.HasTag("Wall") || !physics.Solid
                        || structure == null || structure.Gone || structure.HP <= 0) return -1;
                    if (owner.BlueprintName == "VineWall" && !owner.HasTag("Vegetation")) return -1;
                    return owner.BlueprintName == "VineWall" ? 0 : 8;
                case "AlchemyStill":
                    return render.RenderString == "&" && !physics.Solid && !owner.HasTag("Solid")
                        && Owned<AlchemyStillPart>(owner) != null ? 0 : -1;
                case "VillageDoor":
                    var door = Owned<DoorPart>(owner);
                    var keyhole = owner.GetPart<LockPart>();
                    if (door == null || door.QuarterTurns < 0 || door.QuarterTurns > 3
                        || physics.Solid || owner.HasTag("Solid")
                        || render.RenderString != "/" && render.RenderString != "+"
                        || keyhole != null && keyhole.ParentEntity != owner
                        || owner.HasPart<RepairablePart>() || owner.HasPart<MorrowfastDoorPart>()
                        || owner.HasPart<SealedLibraryBarrierPart>()) return -1;
                    return 16 + (door.QuarterTurns & 1) * 2 + (door.IsClosed ? 0 : 1);
                default: return -1;
            }
        }
        static bool OrdinaryFloor(Entity owner, bool grass)
        {
            if (!PlainOwner(owner, true) || !owner.HasTag("Terrain") || owner.HasTag("Barren")
                || owner.HasTag("Wall") || owner.HasTag("Solid") || Owned<PhysicsPart>(owner).Solid) return false;
            return grass ? owner.HasTag("Plantable") && owner.HasTag("Vegetation") : !owner.HasTag("Vegetation");
        }
        static bool PlainOwner(Entity owner, bool ground)
        {
            if (owner == null || owner.HasTag("Creature") || owner.HasTag("Player") || owner.HasTag("Item")) return false;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            if (render == null || !render.Visible || physics == null || physics.Takeable
                || physics.InInventory != null || physics.Equipped != null
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)
                || (ground ? render.RenderString != "." || render.GlyphVariants != "...,.'`"
                    : !string.IsNullOrEmpty(render.GlyphVariants))) return false;
            int renders = 0, physical = 0;
            for (int i = 0; i < owner.Parts.Count; i++)
            { if (owner.Parts[i] is RenderPart) renders++; if (owner.Parts[i] is PhysicsPart) physical++; }
            if (renders != 1 || physical != 1) return false;
            var stack = owner.GetPart<StackerPart>();
            if (stack != null && (stack.ParentEntity != owner || stack.StackCount <= 0)) return false;
            var footprint = owner.GetPart<SpatialFootprintPart>();
            if (footprint == null) return true;
            var offsets = footprint.Offsets;
            return footprint.ParentEntity == owner && offsets.Length == 1 && offsets[0] == Vector2Int.zero;
        }
        static bool Placed(Zone zone, Cell cell, Entity owner)
        {
            if (owner == null || owner.SpatialZone != zone) return false;
            var anchor = zone.GetEntityCell(owner);
            if (anchor == null || !anchor.Objects.Contains(owner)) return false;
            var footprint = owner.GetPart<SpatialFootprintPart>();
            return footprint == null ? anchor == cell
                : footprint.ParentEntity == owner && zone.IsFootprintCurrent(owner)
                    && SpatialQuery.DistanceToCell(zone, owner, cell.X, cell.Y) == 0;
        }
        static bool VisiblePhysicalOwner(Entity owner)
        {
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            var stack = owner.GetPart<StackerPart>();
            return render != null && render.Visible && physics != null
                && physics.InInventory == null && physics.Equipped == null
                && (stack == null || stack.ParentEntity == owner && stack.StackCount > 0);
        }
        static bool CompetingGround(Entity owner)
            => owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>() || owner.HasPart<CultivatedSoilPart>()
                || owner.HasPart<LiquidPoolPart>() || owner.HasPart<GasPoolPart>() || owner.HasPart<TileStateSourcePart>() || owner.HasPart<TriggerOnStepPart>()
                || owner.HasTag("WellGroundMarker") || owner.HasTag("OvenGroundMarker") || owner.HasTag("LanternGroundMarker")
                || owner.HasTag("Terrain") && !owner.HasTag("Wall")
                    && owner.BlueprintName != "Grass" && owner.BlueprintName != "StoneFloor";
        static T Owned<T>(Entity owner) where T : Part
        { var part = owner?.GetPart<T>(); return part != null && part.ParentEntity == owner ? part : null; }
        static int VariantAt(int x, int y)
        {
            // Quiet majority, stable coordinate-only marks. No simulation RNG.
            uint value = unchecked((uint)x * 0x9e3779b1u ^ (uint)y * 0x85ebca77u);
            value ^= value >> 16; value = unchecked(value * 0x7feb352du); value ^= value >> 15;
            return (value & 15u) < 12u ? 0 : 1 + (int)((value >> 4) % 3u);
        }
        static Tile[] Frames(Texture2D texture, int count, string group)
        {
            if (texture == null || texture.width != count * Width || texture.height != Height) return null;
            var tiles = new Tile[count];
            for (int i = 0; i < count; i++)
            {
                var sprite = Sprite.Create(texture, new Rect(i * Width, 0, Width, Height),
                    new Vector2(.5f, .5f), Width, 0, SpriteMeshType.FullRect);
                sprite.name = "Live16x24 Sill " + group + " " + i; sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
                tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
                tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None; tiles[i] = tile;
            }
            return tiles;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            DestroyFrames(envelope); DestroyFrames(masonry); DestroyFrames(works); DestroyFrames(stoneFloor);
            DestroyFrames(environment);
        }
        static void DestroyFrames(Tile[] tiles)
        {
            if (tiles == null) return;
            foreach (var tile in tiles)
            { if (tile == null) continue; DestroyOwned(tile.sprite); DestroyOwned(tile); }
        }
        static void DestroyOwned(Object value)
        { if (value == null) return; if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); }
    }
}
