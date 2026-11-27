using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CavesOfOoo.Core
{
    /// <summary>The copyists' saved supply keeps their two original work lamps lit.</summary>
    internal static class QuillholdDeskLighting
    {
        internal const string Condition = "Quillhold.DesksLit";
        private const string DeskBlueprint = "QuillholdCopyDesk";
        internal const int Radius = 3;
        internal const float Intensity = .65f;

        private sealed class DeskSites
        {
            internal readonly List<(int X, int Y)> Positions = new List<(int, int)>(2);

            internal DeskSites(OverworldZoneManager manager)
            {
                var plan = QuillholdCompositionPlan.Create(QuillholdCompositionPlan.ZoneID, manager.WorldSeed);
                var hall = plan.Rooms.Single(room => room.Role == "CopyHall");
                for (int y = hall.Y + 1; y < hall.Y + hall.Height - 1; y++)
                    for (int x = hall.X + 1; x < hall.X + hall.Width - 1; x++)
                        if (plan.ObjectAt(x, y) == DeskBlueprint) Positions.Add((x, y));
            }
        }

        private static readonly ConditionalWeakTable<OverworldZoneManager, DeskSites> Sites =
            new ConditionalWeakTable<OverworldZoneManager, DeskSites>();

        private static OverworldZoneManager Manager(Zone zone)
        {
            if (zone?.ZoneID != QuillholdCompositionPlan.ZoneID) return null;
            var manager = WorldLocationContext.For(zone);
            if (manager?.WorldMap == null || !manager.CachedZones.TryGetValue(zone.ZoneID, out var cached)
                || cached != zone || manager.WorldMap.GetBiome(14, 9) != BiomeType.Spread
                || !AreaCompositionScope.IsQuillholdSite(manager.WorldMap.GetPOI(14, 9))) return null;
            return manager;
        }

        private static bool Supplied(OverworldZoneManager manager)
            => manager.SettlementManager.HasExistingCondition(QuillholdCompositionPlan.ZoneID, Condition);

        private static Entity DeskAt(Zone zone, Cell cell)
        {
            if (cell?.IsInterior != true) return null;
            Entity desk = null;
            foreach (var owner in cell.Objects)
            {
                if (owner.BlueprintName != DeskBlueprint) continue;
                if (desk != null) return null;
                desk = owner;
            }
            return WorldResourceActions.Ground(desk, zone) && QuillholdCompositionBuilder.Valid(desk, DeskBlueprint)
                && !desk.HasPart<DraggedPart>() && desk.GetPart<RenderPart>().ParentEntity == desk
                && desk.GetPart<DestructiblePart>().ParentEntity == desk ? desk : null;
        }

        internal static (Cell Left, Cell Right) Sources(Zone zone)
        {
            var manager = Manager(zone);
            if (manager == null || !Supplied(manager)) return default;
            var sites = Sites.GetValue(manager, world => new DeskSites(world)).Positions;
            if (sites.Count != 2) return default;
            var left = zone.GetCell(sites[0].X, sites[0].Y);
            var right = zone.GetCell(sites[1].X, sites[1].Y);
            return (DeskAt(zone, left) != null ? left : null, DeskAt(zone, right) != null ? right : null);
        }

        internal static string Describe(Entity owner, Zone zone)
        {
            if (owner?.BlueprintName != DeskBlueprint) return null;
            var manager = Manager(zone);
            if (manager == null || zone.GetEntityCell(owner) is not Cell cell) return null;
            var positions = Sites.GetValue(manager, world => new DeskSites(world)).Positions;
            if (!positions.Contains((cell.X, cell.Y)) || DeskAt(zone, cell) != owner) return null;
            return Supplied(manager) ? "The desk lamp is lit." : "The desk lamp is out.";
        }

        internal static bool IsWorkFloor(Zone zone, int x, int y)
        {
            if (zone?.ZoneID != QuillholdCompositionPlan.ZoneID || zone.GetCell(x, y) is not { IsInterior: true } cell
                || cell.IsWall()) return false;
            var manager = Manager(zone);
            if (manager == null) return false;
            foreach (var site in Sites.GetValue(manager, world => new DeskSites(world)).Positions)
            {
                int dx = x - site.X, dy = y - site.Y;
                if (dx * dx + dy * dy <= Radius * Radius) return true;
            }
            return false;
        }

        internal static void InvalidateActive(SettlementManager settlement, string zoneId)
        {
            var active = SettlementRuntime.ActiveZone;
            if (active?.ZoneID == zoneId && Manager(active)?.SettlementManager == settlement)
                SettlementRuntime.MarkZoneDirty();
        }
    }
}
