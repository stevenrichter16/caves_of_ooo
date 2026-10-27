using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>Six current ordinary shale-cave owners. Borrows immutable
    /// textures and the selector; owns only cached sprite/tile views.</summary>
    public sealed class FlatPixelShaleCavesCatalog : IDisposable
    {
        public const string ResourcePath = "Sprites/Live16x24/shale-caves";
        public const int FrameCount = 6;
        const int Width = 16, Height = 24;
        readonly FlatPixelFallbackCatalog selector;
        readonly Tile[] walls, floors, rubble, stalagmite, down, up;
        readonly Tile[] parityWalls, parityFloors, parityRubble;
        bool disposed;
        struct Address
        {
            public int X, Y;
            public string SurfaceID;
            public Address(int x, int y, string surface) { X = x; Y = y; SurfaceID = surface; }
        }
        static readonly Dictionary<string, Address> addresses = BuildAddresses();

        // Canonical strings and immutable coordinate facts are prepared once.
        // Mutable map, saved-column and current-graph authority are never cached.
        static Dictionary<string, Address> BuildAddresses()
        {
            var result = new Dictionary<string, Address>(StringComparer.Ordinal);
            for (int x = 0; x < WorldMap.Width; x++)
                for (int y = 0; y < WorldMap.Height; y++)
                {
                    if (SinkholeSites.IsMouth(x, y) || x == RootSiteBuilder.WorldX && y == RootSiteBuilder.WorldY
                        || x == MultiCellPilotRuntime.WorldX && y == MultiCellPilotRuntime.WorldY) continue;
                    string surface = WorldMap.ToZoneID(x, y);
                    bool reserved = false;
                    foreach (var id in OverworldZoneManager.AuthoredWildernessZoneIDs)
                        if (id == surface) { reserved = true; break; }
                    if (reserved) continue;
                    var address = new Address(x, y, surface);
                    result.Add(WorldMap.ToZoneID(x, y, 5), address);
                    result.Add(WorldMap.ToZoneID(x, y, 6), address);
                }
            return result;
        }
        public FlatPixelShaleCavesCatalog(FlatPixelFallbackCatalog borrowedSelector)
            : this(Resources.Load<Texture2D>(ResourcePath),
                Resources.Load<Texture2D>("Sprites/Environment/rubble"),
                Resources.Load<Texture2D>("Sprites/Environment/stalagmite"),
                Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.AnchorsResourcePath),
                Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.CellarResourcePath),
                Resources.Load<Texture2D>(FlatPixelShaleParityStudy.ResourcePath), borrowedSelector,
                Resources.Load<Texture2D>(FlatPixelShaleParityStudy.RubbleResourcePath)) { }

        /// <summary>Each optional group fails independently. Explicit null input
        /// never reloads a resource. Square sources remain centered 16 PPU.</summary>
        public FlatPixelShaleCavesCatalog(Texture2D shale6, Texture2D rubble16, Texture2D stalagmite16, Texture2D anchors12,
            Texture2D cellar9, FlatPixelFallbackCatalog borrowedSelector)
            : this(shale6, rubble16, stalagmite16, anchors12, cellar9, null, borrowedSelector) { }

        /// <summary>The optional sibling study never replaces the preserved base
        /// atlas. Explicit null/wrong dimensions retain the original presentation.</summary>
        public FlatPixelShaleCavesCatalog(Texture2D shale6, Texture2D rubble16, Texture2D stalagmite16, Texture2D anchors12,
            Texture2D cellar9, Texture2D parity72, FlatPixelFallbackCatalog borrowedSelector)
            : this(shale6, rubble16, stalagmite16, anchors12, cellar9, parity72, borrowedSelector, null) { }

        public FlatPixelShaleCavesCatalog(Texture2D shale6, Texture2D rubble16, Texture2D stalagmite16, Texture2D anchors12,
            Texture2D cellar9, Texture2D parity72, FlatPixelFallbackCatalog borrowedSelector, Texture2D parityRubble16)
        {
            selector = borrowedSelector;
            if (selector == null) return;
            walls = Frames(shale6, FrameCount, Height, 4, 2, "wall");
            floors = Frames(shale6, FrameCount, Height, 0, 4, "floor");
            rubble = Frames(rubble16, 1, Width, 0, 1, "rubble");
            stalagmite = Frames(stalagmite16, 1, Width, 0, 1, "stalagmite");
            down = Frames(anchors12, 12, Height, 4, 1, "down");
            up = Frames(cellar9, 9, Height, 0, 1, "up");
            parityFloors = Frames(parity72, FlatPixelShaleParityStudy.FrameCount, Height, 0,
                FlatPixelShaleParityStudy.FloorVariants, "study floor");
            parityWalls = Frames(parity72, FlatPixelShaleParityStudy.FrameCount, Height,
                FlatPixelShaleParityStudy.FloorVariants, 16 * FlatPixelShaleParityStudy.WallVariants, "study wall");
            parityRubble = Frames(parityRubble16, 1, Width, 0, 1, "study rubble");
        }
        public static bool IsSupportedZone(Zone zone)
        {
            if (zone?.ZoneID == null || !addresses.TryGetValue(zone.ZoneID, out var address)) return false;
            var manager = WorldLocationContext.For(zone);
            if (manager?.CachedZones == null || !manager.CachedZones.TryGetValue(zone.ZoneID, out var current)
                || !ReferenceEquals(current, zone)) return false;
            var map = manager.WorldMap;
            if (map?.Tiles == null || map.POIs == null
                || map.Tiles.GetLength(0) != WorldMap.Width || map.Tiles.GetLength(1) != WorldMap.Height
                || map.POIs.GetLength(0) != WorldMap.Width || map.POIs.GetLength(1) != WorldMap.Height) return false;
            // Derived current-column eligibility, not a claim about an arbitrary
            // edited/restored graph's original (unsaved) generation provenance.
            return map.GetPOI(address.X, address.Y) == null
                && !LairStacks.HasSavedColumn(manager, address.SurfaceID);
        }
        public bool TryResolveGround(Zone zone, int x, int y, out Tile tile, out Entity floor)
        {
            tile = null; floor = null;
            if (floors == null || !ReadyCell(zone, x, y, out var cell)) return false;
            var state = zone.TileState.Get(x, y);
            if (state != null && !state.IsEmpty) return false;
            Entity actual = null;
            foreach (var owner in cell.Occupants)
            {
                if (!Placed(zone, cell, owner) || !VisiblePhysicalOwner(owner)) continue;
                if (owner.BlueprintName == "ShaleFloor")
                { if (actual != null || !OrdinaryFloor(owner)) return false; actual = owner; }
                else if (CompetingGround(owner)) return false;
            }
            if (actual == null) return false;
            floor = actual;
            tile = ParityActive(zone) ? parityFloors[StudyVariantAt(x, y)] : floors[VariantAt(x, y)];
            return true;
        }
        public bool TryResolveForeground(Zone zone, int x, int y, Entity owner, out Tile tile)
        {
            tile = null;
            if (!ReadyCell(zone, x, y, out var cell) || !Selected(zone, x, y, owner)
                || !Placed(zone, cell, owner) || !OrdinaryForeground(owner)) return false;
            Tile[] group;
            switch (owner.BlueprintName)
            {
                case "ShaleWall": group = walls; break;
                case "Rubble": group = parityRubble != null && ParityActive(zone) ? parityRubble : rubble; break;
                case "Stalagmite": group = stalagmite; break;
                case "StairsDown": group = down; break;
                case "StairsUp": group = up; break;
                default: return false;
            }
            if (group == null) return false;
            if (owner.BlueprintName == "ShaleWall" && ParityActive(zone))
            {
                int mask = ExposedFloorMask(zone, x, y);
                // A recessed silhouette requires safe backing. Otherwise retain
                // the original opaque body instead of exposing a false floor.
                if (mask == 0 || SafeWallRecessCell(zone, cell, owner))
                {
                    tile = parityWalls[mask * FlatPixelShaleParityStudy.WallVariants
                        + StudyVariantAt(x, y) % FlatPixelShaleParityStudy.WallVariants];
                    return true;
                }
            }
            tile = group[owner.BlueprintName == "ShaleWall" && VariantAt(x, y) != 0 ? 1 : 0];
            return true;
        }

        /// <summary>Pattern beneath the selected wall's shallow silhouette cuts.
        /// This is presentation backing, never a floor owner or passable cell.</summary>
        public bool TryResolveWallRecessBackdrop(Zone zone, int x, int y, Entity owner, out Tile tile, out Color tint)
        {
            tile = null; tint = default;
            if (owner?.BlueprintName != "ShaleWall" || !ParityActive(zone)
                || !TryResolveForeground(zone, x, y, owner, out _)
                || ExposedFloorMask(zone, x, y) == 0
                || !SafeWallRecessCell(zone, zone.GetCell(x, y), owner)) return false;
            tile = parityFloors[StudyVariantAt(x, y)]; tint = Color.white;
            return true;
        }

        static bool SafeWallRecessCell(Zone zone, Cell cell, Entity wall)
        {
            if (cell == null) return false;
            var state = zone.TileState.Get(cell.X, cell.Y);
            if (state != null && !state.IsEmpty) return false;
            foreach (var owner in cell.Occupants)
            {
                if (ReferenceEquals(owner, wall) || !Placed(zone, cell, owner) || !VisiblePhysicalOwner(owner)) continue;
                // The selected wall's authored ^k is replaced only here. Other
                // real substrates, sources and authored backgrounds retain authority.
                if (owner.HasTag("Terrain") || CompetingGround(owner) || ForeignMeaning(owner)
                    || !string.IsNullOrEmpty(Owned<RenderPart>(owner).BackgroundColor)) return false;
            }
            return true;
        }

        bool ParityActive(Zone zone) => parityFloors != null && parityWalls != null
            && FlatPixelShaleParityStudy.IsActive(zone, true); // This catalog serves flat presentation only.

        int ExposedFloorMask(Zone zone, int x, int y)
            => (DisclosedFloor(zone, x, y - 1) ? 1 : 0)
             | (DisclosedFloor(zone, x + 1, y) ? 2 : 0)
             | (DisclosedFloor(zone, x, y + 1) ? 4 : 0)
             | (DisclosedFloor(zone, x - 1, y) ? 8 : 0);

        bool DisclosedFloor(Zone zone, int x, int y)
        {
            // Unknown/remembered space is not evidence of an exposed face.
            // Ground validation also excludes tile state and foreign materials.
            if (!TryResolveGround(zone, x, y, out _, out _)) return false;
            var cell = zone.GetCell(x, y);
            foreach (var owner in cell.Occupants)
            {
                if (!Placed(zone, cell, owner) || !VisiblePhysicalOwner(owner)) continue;
                if (Owned<PhysicsPart>(owner).Solid || owner.HasTag("Solid") || owner.HasTag("Wall")) return false;
            }
            return true;
        }

        static int StudyVariantAt(int x, int y)
        {
            uint value = unchecked((uint)x * 0x9e3779b1u ^ (uint)y * 0x85ebca77u);
            value ^= value >> 16; value = unchecked(value * 0x7feb352du); value ^= value >> 15;
            return (int)(value % FlatPixelShaleParityStudy.FloorVariants);
        }
        /// <summary>Quiet visible-cell canvas, not a terrain owner or material.
        /// It cannot replace real ground, authored backgrounds or live hazards.
        /// Hidden/custodied occupants never alter this presentation decision.</summary>
        public bool TryResolveCellBackdrop(Zone zone, int x, int y, out Tile tile, out Color color)
        {
            tile = null; color = default;
            if (!ReadyCell(zone, x, y, out var cell) || selector.BackgroundTile == null) return false;
            var state = zone.TileState.Get(x, y);
            if (state != null && !state.IsEmpty) return false;
            foreach (var owner in cell.Occupants)
            {
                if (!Placed(zone, cell, owner) || !VisiblePhysicalOwner(owner)) continue;
                // A missing/rejected floor resource is not permission to
                // conceal that owner's fallback with an ambient substitute.
                if (owner.BlueprintName == "ShaleFloor" || CompetingGround(owner)
                    || !string.IsNullOrEmpty(Owned<RenderPart>(owner).BackgroundColor)
                    || owner.BlueprintName == "Rubble" && !OrdinaryForeground(owner)) return false;
            }
            tile = selector.BackgroundTile;
            color = ParityActive(zone) ? new Color32(63, 68, 74, 255) : new Color32(84, 88, 92, 255);
            return true;
        }
        public bool TryGetSubjectSprite(Zone zone, Entity owner, int x, int y, out Sprite sprite)
        {
            sprite = null;
            if (!ReadyCell(zone, x, y, out _) || !Selected(zone, x, y, owner)) return false;
            if (TryResolveForeground(zone, x, y, owner, out var foreground))
            { sprite = foreground.sprite; return sprite != null; }
            if (TryResolveGround(zone, x, y, out var ground, out var floor) && ReferenceEquals(floor, owner))
            { sprite = ground.sprite; return sprite != null; }
            return false;
        }
        bool ReadyCell(Zone zone, int x, int y, out Cell cell)
        {
            cell = null;
            if (disposed || selector == null || !IsSupportedZone(zone)) return false;
            cell = zone.GetCell(x, y);
            return cell != null && cell.ParentZone == zone && cell.X == x && cell.Y == y && cell.Explored && cell.IsVisible;
        }
        bool Selected(Zone zone, int x, int y, Entity owner)
            => owner != null && selector.TryResolve(zone, x, y, out var picked)
                && !picked.Remembered && ReferenceEquals(picked.Owner, owner);
        static bool OrdinaryFloor(Entity owner)
        {
            if (!PlainOwner(owner) || ForeignMeaning(owner) || !Terrain(owner) || !NoMaterialState(owner)) return false;
            var render = Owned<RenderPart>(owner);
            return Render(render, ".", "&K", 0) && render.GlyphVariants == "...,.'`"
                && string.IsNullOrEmpty(render.BackgroundColor);
        }
        static bool OrdinaryForeground(Entity owner)
        {
            if (!PlainOwner(owner) || ForeignMeaning(owner)) return false;
            var render = Owned<RenderPart>(owner);
            if (!string.IsNullOrEmpty(render.GlyphVariants)) return false;
            switch (owner.BlueprintName)
            {
                case "ShaleWall":
                    return Render(render, "#", "&K", 0) && render.BackgroundColor == "^k"
                        && SolidObject(owner) && owner.HasTag("Wall") && Stone(owner)
                        && SurvivingWall(owner) && NeutralThermal(owner);
                case "Rubble":
                    return Render(render, ",", "&y", 0) && string.IsNullOrEmpty(render.BackgroundColor)
                        && Terrain(owner) && NoMaterialState(owner);
                case "Stalagmite":
                    return Render(render, "^", "&y", 1) && string.IsNullOrEmpty(render.BackgroundColor)
                        && SolidObject(owner) && !owner.HasTag("Wall") && NoMaterialState(owner);
                case "StairsDown":
                    return Render(render, ">", "&W", 0) && string.IsNullOrEmpty(render.BackgroundColor)
                        && Terrain(owner) && NoMaterialState(owner) && owner.HasTag("StairsDown")
                        && !owner.HasTag("StairsUp") && Owned<StairsDownPart>(owner) != null;
                case "StairsUp":
                    return Render(render, "<", "&W", 0) && string.IsNullOrEmpty(render.BackgroundColor)
                        && Terrain(owner) && NoMaterialState(owner) && owner.HasTag("StairsUp")
                        && !owner.HasTag("StairsDown") && Owned<StairsUpPart>(owner) != null;
                default: return false;
            }
        }
        static bool Render(RenderPart render, string glyph, string color, int layer)
            => render.RenderString == glyph && render.ColorString == color && render.RenderLayer == layer;
        static bool Terrain(Entity owner)
            => owner.HasTag("Terrain") && !owner.HasTag("Wall") && !owner.HasTag("Vegetation")
                && !owner.HasTag("Solid") && !Owned<PhysicsPart>(owner).Solid;
        static bool SolidObject(Entity owner)
            => !owner.HasTag("Terrain") && !owner.HasTag("Vegetation") && owner.HasTag("Solid") && Owned<PhysicsPart>(owner).Solid;
        static bool PlainOwner(Entity owner)
        {
            if (owner == null || owner.HasTag("Creature") || owner.HasTag("Player") || owner.HasTag("Item")
                || owner.HasTag("Plantable") || owner.HasTag("Barren") || owner.HasPart<BrainPart>() || owner.HasPart("Body")
                || owner.GetStat("Hitpoints") != null || Owned<ExaminablePart>(owner) == null) return false;
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            if (render == null || !render.Visible || physics == null || physics.Takeable
                || physics.InInventory != null || physics.Equipped != null
                || !string.IsNullOrEmpty(render.VisualID) || !string.IsNullOrEmpty(render.VisualVariant)) return false;
            if (owner.HasPart<StackerPart>())
            { var stack = Owned<StackerPart>(owner); if (stack == null || stack.StackCount <= 0) return false; }
            if (owner.HasPart<SpatialFootprintPart>())
            {
                var footprint = Owned<SpatialFootprintPart>(owner);
                if (footprint == null || footprint.Offsets.Length != 1 || footprint.Offsets[0] != Vector2Int.zero) return false;
            }
            if (owner.HasPart<StatusEffectsPart>())
            { var effects = Owned<StatusEffectsPart>(owner); if (effects == null || effects.GetRenderColorOverride() != null) return false; }
            return true;
        }
        static bool ForeignMeaning(Entity owner)
            => owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>() || owner.HasPart<CultivatedSoilPart>()
                || owner.HasPart<HarvestablePart>() || owner.HasPart<LiquidPoolPart>() || owner.HasPart<WellPart>()
                || owner.HasPart<RepairablePart>() || owner.HasPart<DoorPart>() || owner.HasPart<ContainerPart>()
                || owner.HasPart<ConversationPart>() || owner.HasPart<TraderPart>()
                || owner.HasPart("Restable") || owner.HasTag("Restable") || owner.HasPart<RegionalSignpostPart>()
                || owner.HasPart<CampfirePart>() || owner.HasPart<TorchLightPart>() || owner.HasPart<LightSourcePart>()
                || owner.HasPart<AlchemyStillPart>() || owner.HasPart<SoddenPreparationPart>() || owner.HasPart<TriggerOnStepPart>()
                || owner.BlueprintName != "StairsDown" && (owner.HasPart<StairsDownPart>() || owner.HasTag("StairsDown"))
                || owner.BlueprintName != "StairsUp" && (owner.HasPart<StairsUpPart>() || owner.HasTag("StairsUp"))
                || owner.HasPart<TileStateSourcePart>() || owner.HasPart<GasPoolPart>()
                || owner.HasPart<FiniteLiquidFilmPart>() || owner.HasPart<BurnOffGasPart>()
                || owner.HasTag("WellGroundMarker") || owner.HasTag("OvenGroundMarker") || owner.HasTag("LanternGroundMarker");
        static bool NoMaterialState(Entity owner)
            => !owner.HasPart<MaterialPart>() && !owner.HasPart<ThermalPart>() && !owner.HasPart<DestructiblePart>();
        static bool Stone(Entity owner)
        {
            var part = Owned<MaterialPart>(owner);
            return part != null && part.MaterialID == "Stone" && float.IsFinite(part.Combustibility) && part.Combustibility == 0f
                && part.HasMaterialTag("Stone") && part.HasMaterialTag("Mineral") && owner.HasTag("Stone") && owner.HasTag("Mineral");
        }
        static bool SurvivingWall(Entity owner)
        {
            var part = Owned<DestructiblePart>(owner);
            return part != null && !part.Gone && !part.Indestructible && part.HP > 0 && part.HP <= part.MaxHP
                && part.MaxHP == 28 && part.Hardness == 2 && part.WreckageBlueprint == "Rubble";
        }
        static bool NeutralThermal(Entity owner)
        {
            var part = Owned<ThermalPart>(owner);
            return part != null && float.IsFinite(part.Temperature) && float.IsFinite(part.FreezeTemperature)
                && float.IsFinite(part.FlameTemperature) && part.Temperature > part.FreezeTemperature && part.Temperature < part.FlameTemperature;
        }
        static bool CompetingGround(Entity owner)
            => owner.HasPart<CropPart>() || owner.HasPart<FieldHarvestPart>() || owner.HasPart<CultivatedSoilPart>()
                || owner.HasPart<LiquidPoolPart>() || owner.HasPart<GasPoolPart>() || owner.HasPart<TileStateSourcePart>()
                || owner.HasPart<TriggerOnStepPart>() || owner.HasPart<FiniteLiquidFilmPart>()
                || owner.HasTag("WellGroundMarker") || owner.HasTag("OvenGroundMarker") || owner.HasTag("LanternGroundMarker")
                || owner.HasTag("Terrain") && !OrdinaryForeground(owner);
        static bool Placed(Zone zone, Cell cell, Entity owner)
        {
            if (owner == null || owner.SpatialZone != zone) return false;
            var anchor = zone.GetEntityCell(owner);
            if (anchor == null || !anchor.Objects.Contains(owner)) return false;
            if (!owner.HasPart<SpatialFootprintPart>()) return anchor == cell;
            var footprint = Owned<SpatialFootprintPart>(owner);
            return footprint != null && zone.IsFootprintCurrent(owner)
                && SpatialQuery.DistanceToCell(zone, owner, cell.X, cell.Y) == 0;
        }
        static bool VisiblePhysicalOwner(Entity owner)
        {
            var render = Owned<RenderPart>(owner); var physics = Owned<PhysicsPart>(owner);
            if (render == null || !render.Visible || physics == null || physics.InInventory != null || physics.Equipped != null) return false;
            if (!owner.HasPart<StackerPart>()) return true;
            var stack = Owned<StackerPart>(owner); return stack != null && stack.StackCount > 0;
        }
        static T Owned<T>(Entity owner) where T : Part
        {
            T found = null;
            if (owner == null) return null;
            for (int i = 0; i < owner.Parts.Count; i++)
                if (owner.Parts[i] is T part)
                { if (found != null || part.ParentEntity != owner) return null; found = part; }
            return found;
        }
        static int VariantAt(int x, int y)
        {
            uint value = unchecked((uint)x * 0x9e3779b1u ^ (uint)y * 0x85ebca77u);
            value ^= value >> 16; value = unchecked(value * 0x7feb352du); value ^= value >> 15;
            return (value & 15u) < 12u ? 0 : 1 + (int)((value >> 4) % 3u);
        }
        static Tile[] Frames(Texture2D texture, int atlasCount, int height, int first, int count, string group)
        {
            if (texture == null || texture.width != atlasCount * Width || texture.height != height) return null;
            var tiles = new Tile[count];
            for (int i = 0; i < count; i++)
            {
                var sprite = Sprite.Create(texture, new Rect((first + i) * Width, 0, Width, height),
                    new Vector2(.5f, .5f), Width, 0, SpriteMeshType.FullRect);
                sprite.name = "Live16x24 Shale Cave " + group + " " + i; sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name;
                tile.hideFlags = HideFlags.HideAndDontSave; tile.sprite = sprite;
                tile.flags = TileFlags.None; tile.colliderType = Tile.ColliderType.None; tiles[i] = tile;
            }
            return tiles;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            DestroyFrames(walls); DestroyFrames(floors); DestroyFrames(rubble);
            DestroyFrames(stalagmite); DestroyFrames(down); DestroyFrames(up);
            DestroyFrames(parityWalls); DestroyFrames(parityFloors); DestroyFrames(parityRubble);
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
