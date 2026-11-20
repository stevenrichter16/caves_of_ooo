using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Where a carried fixture may be set down (Docs/HOME-BASE-DESIGN.md sections 7.1 and 7.2, slice PB1). A pure function of saved data: the
    /// world map, the plan tables and the zone's saved entities. Nothing here reads generation-time state such as <c>Zone.GenReservedCells</c>
    /// (it is not serialised), so a placement decided after a save round trip agrees with one decided before it.
    /// </summary>
    public enum HeldPlaceZoneClass
    {
        /// <summary>Not an overworld zone id, or out of the map.</summary>
        Unsupported,
        /// <summary>A zone that holds a point of interest: villages, camps, lairs, Choir nodes, catacomb villages, Root, sinkholes, scenes.</summary>
        PoiZone,
        /// <summary>The opening's pinned zones, authored wilderness scenes, the rare-encounter pair, regional-situation zones and similar.</summary>
        ProtectedGround,
        /// <summary>A surface biome outside the allow-list (Grovelands, Overwrit, caves).</summary>
        DisallowedBiome,
        /// <summary>A Spread exploration family owns the zone; its composer reads the ordinary owners.</summary>
        FamilyOwned,
        /// <summary>Ordinary wild surface ground in an allowed biome.</summary>
        WildSurface,
        /// <summary>Underground. Refused in v1 until the column audit (PB7).</summary>
        Column
    }

    public static class HeldPlaceZones
    {
        /// <summary>Surface biomes that may take a set-down fixture. Grovelands is excluded by Choir law (everything there is somebody); the Overwrit by canon.</summary>
        public static readonly BiomeType[] AllowedBiomes = { BiomeType.Spread, BiomeType.Sodden, BiomeType.Beating, BiomeType.Stump };

        /// <summary>
        /// Zones refused on top of <c>WaysitePlan</c>'s exclusions. 11.11.0 is the opening trapper's-store yard (opening-only extras and a yard-duty guard) and is the one opening worksite
        /// that list misses (independent family audit, Docs/HOME-BASE-IMPLEMENTATION.md). Data, not logic: add an address here to protect a place.
        /// </summary>
        public static readonly string[] ProtectedExtras = { "Overworld.11.11.0" };

        public static HeldPlaceZoneClass Classify(OverworldZoneManager manager, string zoneId)
        {
            if (manager == null || string.IsNullOrEmpty(zoneId) || !zoneId.StartsWith("Overworld.", StringComparison.Ordinal)) return HeldPlaceZoneClass.Unsupported;
            var (x, y, z) = WorldMap.FromZoneID(zoneId);
            if (!WorldMapAuthoring.InBounds(x, y)) return HeldPlaceZoneClass.Unsupported;
            if (z > 0) return HeldPlaceZoneClass.Column;
            if (z < 0) return HeldPlaceZoneClass.Unsupported;
            if (manager.WorldMap?.GetPOI(x, y) != null) return HeldPlaceZoneClass.PoiZone;
            if (!WaysitePlan.IsEligibleIgnoringFamily(manager, zoneId)
                || Array.IndexOf(OverworldZoneManager.AuthoredWildernessZoneIDs, zoneId) >= 0
                || Array.IndexOf(ProtectedExtras, zoneId) >= 0
                || zoneId == MultiCellPilotRuntime.ZoneID) return HeldPlaceZoneClass.ProtectedGround;
            if (manager.WorldMap == null || Array.IndexOf(AllowedBiomes, manager.WorldMap.GetBiome(x, y)) < 0) return HeldPlaceZoneClass.DisallowedBiome;
            if (WaysitePlan.IsFamilyOwned(manager, zoneId)) return HeldPlaceZoneClass.FamilyOwned;
            return HeldPlaceZoneClass.WildSurface;
        }

        /// <summary>The refusal code for a class, or null when the class admits placement.</summary>
        public static string RefusalCode(HeldPlaceZoneClass zoneClass)
        {
            switch (zoneClass)
            {
                case HeldPlaceZoneClass.WildSurface: return null;
                case HeldPlaceZoneClass.PoiZone: return PlacementCheck.PoiZone;
                case HeldPlaceZoneClass.ProtectedGround: return PlacementCheck.ProtectedGround;
                case HeldPlaceZoneClass.DisallowedBiome: return PlacementCheck.Biome;
                // Lifted after an independent audit of every family consumer: each is generation-only or bound to references saved at generation, and every selector honours the
                // Owned tag a set-down fixture carries. The class stays so tests and readers can still tell a family zone; only the refusal is gone.
                case HeldPlaceZoneClass.FamilyOwned: return null;
                case HeldPlaceZoneClass.Column: return PlacementCheck.ColumnPending;
                default: return PlacementCheck.UnsupportedZone;
            }
        }
    }

    /// <summary>What a placement needs to know about the fixture being set down. Mirrors the data row in the design (section 5.1) so the check never reads a blueprint list.</summary>
    public struct PlacementSpec
    {
        /// <summary>Whether the standing fixture is solid (a box is; a bench is not).</summary>
        public bool Solid;
        /// <summary>"OpenGround" (any ground that passes) or "BareGround" (no vegetation). Anything else fails closed.</summary>
        public string GroundClass;
    }

    public sealed class PlacementResult
    {
        public readonly List<string> Reasons = new List<string>(2);
        /// <summary>True when the protection-capability check ran. It is dormant until protection tags exist, and a preview must never claim it checked when it did not.</summary>
        public bool ProtectedNearChecked;
        public bool Ok => Reasons.Count == 0;
        public string First => Reasons.Count > 0 ? Reasons[0] : null;
        /// <summary>How many further reasons also fail (the preview prints the first and this count).</summary>
        public int Others => Math.Max(0, Reasons.Count - 1);
    }

    /// <summary>One pure function shared by the preview, the action and diagnostics. It decides on saved data only and mutates nothing.</summary>
    public static class PlacementCheck
    {
        public const string PoiZone = "poi-zone", ProtectedGround = "protected-ground", Biome = "biome", FamilyOwned = "family-owned",
            ColumnPending = "column-pending", UnsupportedZone = "unsupported-zone", Ground = "ground", CellOccupied = "cell-occupied",
            OpenNeighbours = "open-neighbours", EdgeMargin = "edge-margin", StairsDoors = "stairs-doors", CapZone = "cap-zone", CapWorld = "cap-world",
            State = "state", ClassSpecific = "class-specific";

        public const string OpenGround = "OpenGround", BareGround = "BareGround";
        /// <summary>Terrain that is the public route and never takes a fixture.</summary>
        static readonly string[] RouteGround = { "RoadStone" };
        public const int EdgeCells = 3, DoorStairReach = 2;
        /// <summary>Provisional caps (design section 5.4): profile before raising.</summary>
        public const int ZoneCap = 8, WorldCap = 32;

        public static PlacementResult Evaluate(OverworldZoneManager manager, Zone zone, int x, int y, PlacementSpec spec, Entity fixture = null)
            => new PlacementBatch(manager, zone).Evaluate(x, y, spec, fixture);

        /// <summary>
        /// The per-spot rules for one zone with the zone-wide answers (class, caps) computed once, so a menu that checks eight neighbouring cells scans the world once, not eight times.
        /// </summary>
        public sealed class PlacementBatch
        {
            readonly Zone zone; readonly string zoneCode; readonly int zoneCount; readonly int worldCount;
            public PlacementBatch(OverworldZoneManager manager, Zone zone)
            {
                this.zone = zone;
                if (zone == null) return;
                zoneCode = HeldPlaceZones.RefusalCode(HeldPlaceZones.Classify(manager, zone.ZoneID));
                zoneCount = CountInZone(zone); worldCount = manager != null ? CountInWorld(manager) : 0;
            }

            public PlacementResult Evaluate(int x, int y, PlacementSpec spec, Entity fixture = null)
            {
                var result = new PlacementResult();
                if (zone == null || !zone.InBounds(x, y)) { result.Reasons.Add(Ground); return result; }

                // 1. zone class
                if (zoneCode != null) result.Reasons.Add(zoneCode);

                var cell = zone.GetCell(x, y);
                // 2. ground
                if (!GroundIsOpen(zone, cell)) result.Reasons.Add(Ground);
                // 3. occupancy: terrain only (the fixture's own entity never counts)
                if (Occupied(cell, fixture)) result.Reasons.Add(CellOccupied);
                // 4. open neighbours
                if (!NeighboursOpen(zone, x, y, spec.Solid, fixture)) result.Reasons.Add(OpenNeighbours);
                // 5. edge margin
                if (x < EdgeCells || y < EdgeCells || x > Zone.Width - 1 - EdgeCells || y > Zone.Height - 1 - EdgeCells) result.Reasons.Add(EdgeMargin);
                // 6. stairs, doors, wells
                if (NearStairsDoorsWell(zone, x, y)) result.Reasons.Add(StairsDoors);
                // 7. protected-near is dormant: no protection capability exists at HEAD, so it is neither checked nor claimed (ProtectedNearChecked stays false)
                // 8. caps, counted by scan over saved entities
                if (zoneCount >= ZoneCap) result.Reasons.Add(CapZone);
                if (worldCount >= WorldCap) result.Reasons.Add(CapWorld);
                // 9. fixture state
                if (fixture != null && !StateOk(fixture)) result.Reasons.Add(State);
                // 10. class specific (fail closed on an unknown class)
                if (!ClassOk(cell, spec.GroundClass)) result.Reasons.Add(ClassSpecific);
                return result;
            }
        }

        /// <summary>The tile and occupant hazards a fixture must never be set down on or beside (the same style as the predator's safe-cell test).</summary>
        public static bool Hazard(Zone zone, Cell cell, Entity self = null)
        {
            if (zone == null || cell == null) return true;
            var tile = zone.TileState.Get(cell.X, cell.Y);
            if (tile != null && (tile.Heat > 0 || tile.Cold > 0 || tile.Charge > 0 || !string.IsNullOrEmpty(tile.Cloud) || tile.Coatings.Count > 0)) return true;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var owner = cell.Objects[i];
                if (owner == self) continue;   // a fixture being moved never counts as a hazard (or a blocker) to its own new cell
                if (owner.HasPart<TriggerOnStepPart>() || owner.HasPart<LiquidPoolPart>() || owner.HasPart<GasPoolPart>()
                    || owner.HasEffect<BurningEffect>() || owner.GetPart<ThermalPart>()?.IsAflame == true) return true;
            }
            return false;
        }

        static bool IsTerrain(Entity e) => e != null && e.HasTag("Terrain");

        /// <summary>
        /// The structure-only form of <see cref="Cell.BlocksMovement"/>: a creature standing beside the spot (the person setting it down, a passer-by) is
        /// transient and never plugs an opening, so it is ignored; walls, solid furniture, closed doors and archive barriers still block.
        /// </summary>
        static bool BlocksWithoutCreatures(Cell cell, Entity self = null)
        {
            if (MorrowfastSceneRuntime.BlockingOwner(cell) != null) return true;
            for (int i = 0; i < cell.Occupants.Count; i++)
            {
                var o = cell.Occupants[i];
                if (o == null || o == self || o.HasTag("Creature")) continue;
                if (o.HasTag("Solid")) return true;
                var physics = o.GetPart<PhysicsPart>();
                if (physics != null && physics.Solid) return true;
                if (o.GetPart<DoorPart>()?.IsClosed == true || o.GetPart<SealedLibraryBarrierPart>()?.IsClosed == true) return true;
            }
            return false;
        }

        /// <summary>Open ground: a hazard-free cell with at least one terrain object, none of them solid or the public route. Other occupants are the occupancy rule's business.</summary>
        static bool GroundIsOpen(Zone zone, Cell cell)
        {
            if (cell == null || Hazard(zone, cell)) return false;
            bool terrain = false;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var e = cell.Objects[i];
                if (!IsTerrain(e)) continue;
                terrain = true;
                if (Array.IndexOf(RouteGround, e.BlueprintName) >= 0 || e.HasTag("Solid") || e.GetPart<PhysicsPart>()?.Solid == true) return false;
            }
            return terrain;
        }

        static bool Occupied(Cell cell, Entity fixture)
        {
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var e = cell.Objects[i];
                if (e == fixture || IsTerrain(e)) continue;
                return true;
            }
            return false;
        }

        static bool NeighboursOpen(Zone zone, int x, int y, bool solid, Entity self = null)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    // A non-solid fixture only needs its four orthogonal neighbours open.
                    if (!solid && dx != 0 && dy != 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if (!zone.InBounds(nx, ny)) return false;
                    var n = zone.GetCell(nx, ny);
                    if (BlocksWithoutCreatures(n, self) || Hazard(zone, n, self)) return false;
                }
            return true;
        }

        static bool NearStairsDoorsWell(Zone zone, int x, int y)
        {
            for (int dx = -DoorStairReach; dx <= DoorStairReach; dx++)
                for (int dy = -DoorStairReach; dy <= DoorStairReach; dy++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (!zone.InBounds(nx, ny)) continue;
                    var objects = zone.GetCell(nx, ny).Objects;
                    for (int i = 0; i < objects.Count; i++)
                    {
                        var e = objects[i];
                        if (e.HasPart<DoorPart>() || e.HasPart<StairsUpPart>() || e.HasPart<StairsDownPart>() || e.HasPart<WellPart>()) return true;
                    }
                }
            return false;
        }

        static bool StateOk(Entity fixture)
        {
            if (fixture.GetPart<DestructiblePart>() is DestructiblePart d && (d.Gone || d.HP <= 0)) return false;
            if (fixture.GetPart<PhysicsPart>()?.Equipped != null) return false;
            if (fixture.HasPart<DraggedPart>()) return false;
            if ((fixture.GetPart<StackerPart>()?.StackCount ?? 1) != 1) return false;   // a stack is not one fixture
            return true;
        }

        static bool ClassOk(Cell cell, string groundClass)
        {
            if (groundClass == OpenGround) return true;
            if (groundClass == BareGround)
            {
                for (int i = 0; i < cell.Objects.Count; i++) if (IsTerrain(cell.Objects[i]) && cell.Objects[i].HasTag("Vegetation")) return false;
                return true;
            }
            return false; // unknown class: fail closed
        }

        static bool IsHeld(Entity e)
        {
            var marker = e.GetPart<InstallationMarkerPart>();
            return marker != null && (marker.Origin == "Player" || marker.Origin == "Relocated");
        }

        /// <summary>Standing fixtures the player holds in one zone (a marker with a Player or Relocated origin), counted by scan.</summary>
        public static int CountInZone(Zone zone)
        {
            if (zone == null) return 0;
            int n = 0; var owners = zone.GetReadOnlyEntitiesWithPart<InstallationMarkerPart>();
            for (int i = 0; i < owners.Count; i++) if (IsHeld(owners[i])) n++;
            return n;
        }

        /// <summary>Standing fixtures the player holds across every cached zone.</summary>
        public static int CountInWorld(OverworldZoneManager manager)
        {
            if (manager == null) return 0;
            int n = 0;
            foreach (var pair in manager.CachedZones) n += CountInZone(pair.Value);
            return n;
        }
    }
}
