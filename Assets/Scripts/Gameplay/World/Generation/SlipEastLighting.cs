using System.Collections.Generic;
using System.Linq;

namespace CavesOfOoo.Core
{
    /// <summary>Places Slip's two east lamps without changing its ordinary village layout.</summary>
    internal static class SlipEastLighting
    {
        internal const string ZoneId = "Overworld.16.11.0";
        internal const string Blueprint = "SlipEastLamp";
        internal const string Condition = "Slip.EastLampsLit";

        private static readonly (int X, int Y)[] NearWell = { (4, -2), (3, -2), (4, 2), (3, 2) };
        private static readonly (int X, int Y)[] FurtherEast = { (9, -2), (8, -2), (9, 2), (8, 2), (5, 2) };
        private static readonly (int X, int Y)[] Cardinal = { (1, 0), (0, 1), (-1, 0), (0, -1) };

        internal static void Install(OverworldZoneManager manager, Zone zone)
        {
            if (zone?.ZoneID != ZoneId || manager?.WorldMap == null
                || manager.WorldMap.GetBiome(16, 11) != BiomeType.Spread
                || manager.WorldMap.GetPOI(16, 11) is not { Type: POIType.Village, Name: "Slip", Faction: "Villagers" }
                || manager.CachedZones.TryGetValue(ZoneId, out var cached) && cached != zone
                || WorldLocationContext.For(zone) is OverworldZoneManager attached && attached != manager
                || manager.Factory?.Blueprints?.ContainsKey(Blueprint) != true
                || zone.GetReadOnlyEntities().Any(owner => owner.BlueprintName == Blueprint)) return;

            var center = VillageBuilder.GetVillageSquareCenter();
            var wellCell = zone.GetCell(center.x, center.y);
            var wells = wellCell.Objects.Where(owner => owner.BlueprintName == "Well").ToArray();
            if (wells.Length != 1 || wells[0].GetProperty("SettlementId") != ZoneId
                || !WorldResourceActions.Ground(wells[0], zone)) return;

            var reachable = DryReachFromWell(zone, wellCell);
            foreach (var nearOffset in NearWell)
            {
                var near = zone.GetCell(center.x + nearOffset.X, center.y + nearOffset.Y);
                if (!reachable.Contains(near) || !BareGrass(zone, near)) continue;
                foreach (var eastOffset in FurtherEast)
                {
                    var east = zone.GetCell(center.x + eastOffset.X, center.y + eastOffset.Y);
                    int dx = east.X - near.X, dy = east.Y - near.Y;
                    if (dx * dx + dy * dy < 16 || !reachable.Contains(east) || !BareGrass(zone, east)) continue;
                    PlacePair(manager, zone, near, east);
                    return;
                }
            }
        }

        private static void PlacePair(OverworldZoneManager manager, Zone zone, Cell near, Cell east)
        {
            int version = zone.EntityVersion;
            var first = CreateLamp(manager, near);
            var second = CreateLamp(manager, east);
            if (first == null || second == null || first.ID == second.ID || zone.EntityVersion != version
                || zone.GetReadOnlyEntities().Any(owner => owner.ID == first.ID || owner.ID == second.ID
                    || owner.BlueprintName == Blueprint)
                || !BareGrass(zone, near) || !BareGrass(zone, east)) return;

            if (!zone.AddEntity(first, near.X, near.Y)) return;
            if (!zone.AddEntity(second, east.X, east.Y)) zone.RemoveEntity(first);
        }

        private static Entity CreateLamp(OverworldZoneManager manager, Cell cell)
        {
            var owner = manager.Factory.CreateEntity(Blueprint);
            if (owner?.BlueprintName != Blueprint || !DragRules.IsRooted(owner) || owner.HasTag("Creature")
                || owner.HasPart<DraggedPart>() || owner.Parts.Count(part => part is PhysicsPart) != 1
                || owner.GetPart<PlaceConditionLightPart>() is not { } light || light.ParentEntity != owner
                || owner.Parts.Count(part => part is PlaceConditionLightPart) != 1
                || owner.GetPart<LightSourcePart>() is not { Enabled: true, Radius: 3, Intensity: .65f } source
                || source.ParentEntity != owner || owner.Parts.Count(part => part is LightSourcePart) != 1
                || owner.GetPart<PhysicsPart>() is not { Solid: false, Takeable: false, InInventory: null, Equipped: null } physics
                || physics.ParentEntity != owner || owner.SpatialZone != null || string.IsNullOrEmpty(owner.ID)
                || owner.HasTag("Solid") || owner.HasTag("Wall") || owner.HasTag("Barren")
                || owner.HasPart<SpatialFootprintPart>()) return null;
            light.SettlementId = ZoneId;
            light.Condition = Condition;
            light.SiteX = cell.X;
            light.SiteY = cell.Y;
            light.LightsAuthoredGround = true;
            return owner;
        }

        private static HashSet<Cell> DryReachFromWell(Zone zone, Cell well)
        {
            var reachable = new HashSet<Cell>();
            var frontier = new Queue<Cell>();

            void Visit(int x, int y)
            {
                if (x < well.X - 3 || x > well.X + 12 || y < well.Y - 5 || y > well.Y + 5) return;
                var cell = zone.GetCell(x, y);
                if (DryExterior(zone, cell) && reachable.Add(cell)) frontier.Enqueue(cell);
            }

            foreach (var step in Cardinal) Visit(well.X + step.X, well.Y + step.Y);
            while (frontier.Count > 0)
            {
                var cell = frontier.Dequeue();
                foreach (var step in Cardinal) Visit(cell.X + step.X, cell.Y + step.Y);
            }
            return reachable;
        }

        private static bool DryExterior(Zone zone, Cell cell)
        {
            if (cell == null || cell.IsInterior || cell.BlocksMovement() || zone.TileState.Has(cell.X, cell.Y)) return false;
            bool hasGround = false;
            foreach (var owner in cell.Occupants)
            {
                if (owner.BlueprintName == "WaterPuddle" || owner.HasPart<LiquidPoolPart>() || owner.HasPart<GreatdewSnarePart>())
                    return false;
                hasGround |= owner.HasTag("Terrain");
            }
            return hasGround;
        }

        private static bool BareGrass(Zone zone, Cell cell)
            => DryExterior(zone, cell) && !zone.GenReservedCells.Contains((cell.X, cell.Y))
                && cell.Objects.Count == 1 && cell.Objects[0].BlueprintName == "Grass" && cell.Occupants.Count == 1;
    }
}
