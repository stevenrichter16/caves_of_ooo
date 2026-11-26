using System;
using System.Collections.Generic;
using System.Linq;

namespace CavesOfOoo.Core
{
    // The clue borrows an accepted native stair pair. Its upper vent is the
    // only resource owner. The lower reader describes a local physical trace.
    internal static class CausewaySteamSeam
    {
        internal const int FirstVersion = 29;
        internal const string Surface = "Overworld.17.9.0", Below = "Overworld.17.9.1";
        private const string StairId = "CausewaySteamSeam.StairId", Fissure = "CausewaySteamSeam.Fissure";

        internal static bool Retain(OverworldZoneManager manager, string id) =>
            (id == Surface || id == Below) && manager.Exploration?.Enabled == true
                && manager.Exploration.Version >= FirstVersion;

        private static bool Supported(OverworldZoneManager manager, string id) => Retain(manager, id)
            && manager.WorldMap.Tiles[17, 9] == BiomeType.Sodden && manager.WorldMap.GetPOI(17, 9) == null
            && SoddenCompositionPlan.IsWildernessZone(Surface)
            && FormationSelector.For(BiomeType.Sodden, Surface) == Formation.Causeway;

        internal static void PrepareForAccess(OverworldZoneManager manager, string id)
        {
            // Cold lateral entry needs the ordinary upper edge so the native
            // StairsUpBuilder can establish the same return as a descent.
            if (id == Below && Supported(manager, id) && !manager.CachedZones.ContainsKey(Below)
                && !manager.CachedZones.ContainsKey(Surface)) manager.GetZone(Surface);
        }

        internal static void Install(OverworldZoneManager manager, Zone zone)
        {
            if (!Supported(manager, zone.ZoneID) || manager.CachedZones.ContainsKey(zone.ZoneID)) return;
            var link = ExactLink(manager);
            if (link == null) return;
            if (zone.ZoneID == Surface && !manager.CachedZones.ContainsKey(Below))
                InstallVent(manager, zone, link);
            if (zone.ZoneID == Below && manager.CachedZones.TryGetValue(Surface, out var surface))
                MarkReturn(zone, surface, link);
        }

        private static ZoneConnection ExactLink(OverworldZoneManager manager)
        {
            var links = manager.GetConnectionsTo(Below, "StairsDown");
            if (links.Count != 1) return null;
            var link = links[0];
            return link.SourceZoneID == Surface && link.SourceX == link.TargetX && link.SourceY == link.TargetY
                ? link : null;
        }

        private static Entity NativeStair<T>(Cell cell, string blueprint) where T : Part
        {
            var candidates = cell?.Objects.Where(e => e.HasTag(blueprint) && e.HasPart<T>()).Take(2).ToArray();
            return candidates?.Length == 1 && candidates[0].BlueprintName == blueprint ? candidates[0] : null;
        }

        private static void InstallVent(OverworldZoneManager manager, Zone zone, ZoneConnection link)
        {
            var stair = NativeStair<StairsDownPart>(zone.GetCell(link.SourceX, link.SourceY), "StairsDown");
            if (stair == null || string.IsNullOrEmpty(stair.ID) || !manager.Factory.Blueprints.ContainsKey("SteamVent")
                || !manager.Factory.Blueprints.TryGetValue("StairsUp", out var returnBlueprint)
                || !returnBlueprint.Parts.ContainsKey("StairsUp") || !returnBlueprint.Parts.ContainsKey("Examinable")) return;

            var end = (x: link.SourceX, y: link.SourceY);
            var start = (x: Zone.Width / 2, y: Zone.Height / 2);
            var geometry = new SpreadWildernessSituationBuilder.Geometry(zone, new HashSet<Entity>());
            var originalPath = FindPath.Search(zone, start.x, start.y, end.x, end.y);
            if (!originalPath.Usable || !geometry.Walk(start.x, start.y, null)) return;
            var approach = new HashSet<(int x, int y)> { start };
            var stepAt = start;
            foreach (var step in originalPath.Steps)
            {
                stepAt = (stepAt.x + step.dx, stepAt.y + step.dy);
                if (!geometry.Walk(stepAt.x, stepAt.y, null)) return;
                approach.Add(stepAt);
            }

            var composition = SoddenCompositionPlan.Create(Surface, manager.WorldSeed);
            var candidate = zone.Cells.Cast<Cell>().Where(cell =>
                    Math.Max(Math.Abs(cell.X - end.x), Math.Abs(cell.Y - end.y)) is >= 3 and <= 5
                    && !approach.Contains((cell.X, cell.Y)) && !composition.IsApproach(cell.X, cell.Y)
                    && geometry.Place(cell.X, cell.Y) && ClearShoulder(zone, cell))
                .OrderBy(cell => Math.Abs(cell.X - end.x) + Math.Abs(cell.Y - end.y - 4))
                .ThenBy(cell => cell.Y).ThenBy(cell => cell.X)
                .FirstOrDefault(cell => AccessibleShoulder(geometry, end, cell));
            if (candidate == null) return;

            var owners = zone.GetReadOnlyEntities().ToArray();
            var unchanged = SpreadGenerationReceipt.CaptureFinalState(zone, owners);
            var wet = zone.TileState.ToSaveString();
            var reserved = new HashSet<(int, int)>(zone.GenReservedCells);
            var vent = manager.Factory.CreateEntity("SteamVent");
            var description = vent?.GetPart<ExaminablePart>();
            if (vent?.BlueprintName != "SteamVent" || vent.SpatialZone != null || !vent.HasTag("Terrain")
                || vent.HasTag("Barren") || vent.HasPart<SpatialFootprintPart>() || vent.HasPart<SourceStockPart>()
                || vent.GetPart<RenderPart>()?.Visible != true || description == null || !SuppliesSteam(vent)
                || string.IsNullOrEmpty(vent.ID) || owners.Any(e => e.ID == vent.ID)
                || zone.EntityCount != owners.Length || !unchanged() || zone.TileState.ToSaveString() != wet
                || !Supported(manager, Surface) || !ReferenceEquals(link, ExactLink(manager))
                || manager.CachedZones.ContainsKey(Below) || !reserved.SetEquals(zone.GenReservedCells)) return;

            vent.Properties[StairId] = stair.ID;
            vent.Properties[Fissure] = PositionKey(candidate);
            description.Text = "Steam curls out of a fissure beside the stone steps. Pale mineral crust rims the opening.";
            zone.AddEntity(vent, candidate.X, candidate.Y);
        }

        private static bool ClearShoulder(Zone zone, Cell cell)
        {
            if (cell.Objects.Count != 1 || cell.Objects[0].BlueprintName != "Grass") return false;
            for (int x = cell.X - 1; x <= cell.X + 1; x++)
                for (int y = cell.Y - 1; y <= cell.Y + 1; y++)
                    if (zone.GetCell(x, y)?.Objects.Any(e => e.GetPart<TileStateSourcePart>()?.Heat > 0) == true)
                        return false;
            return true;
        }

        private static bool AccessibleShoulder(SpreadWildernessSituationBuilder.Geometry geometry,
            (int x, int y) stair, Cell cell)
        {
            var at = (cell.X, cell.Y);
            if (!geometry.PreservesRoutes(new[] { at })) return false;
            var reached = geometry.Flood(new HashSet<(int, int)> { at }, stair, 14, null);
            return reached[cell.X - 1, cell.Y] || reached[cell.X + 1, cell.Y]
                || reached[cell.X, cell.Y - 1] || reached[cell.X, cell.Y + 1];
        }

        private static bool SuppliesSteam(Entity vent) => vent.GetPart<MaterialPart>()?.MaterialID == "Steam"
            && vent.GetPart<TileStateSourcePart>() is TileStateSourcePart tile && tile.Heat > 0 && tile.Cold <= 0
            && tile.Coating == "water" && vent.GetPart<FuelPart>() == null;

        private static string PositionKey(Cell cell) => cell == null ? null : $"{cell.X},{cell.Y}";

        internal static string ScaleClue(Entity vent)
        {
            var zone = vent?.SpatialZone;
            if (zone?.ZoneID != Surface || vent.BlueprintName != "SteamVent" || !SuppliesSteam(vent)
                || string.IsNullOrEmpty(vent.GetProperty(StairId))) return null;

            var cell = zone.GetEntityCell(vent);
            return cell != null && cell.Objects.Contains(vent) && vent.GetProperty(Fissure) == PositionKey(cell)
                ? "Scraped vent scale can be pressed into heat-proofing for clothing." : null;
        }

        private static void MarkReturn(Zone below, Zone surface, ZoneConnection link)
        {
            if (surface.ZoneID != Surface) return;
            var down = NativeStair<StairsDownPart>(surface.GetCell(link.SourceX, link.SourceY), "StairsDown");
            if (down == null || !surface.GetReadOnlyEntities().Any(e =>
                e.BlueprintName == "SteamVent" && e.GetProperty(StairId) == down.ID && SuppliesSteam(e)
                && e.GetProperty(Fissure) is string fissure && fissure == PositionKey(surface.GetEntityCell(e)))) return;

            var cell = below.GetCell(link.TargetX, link.TargetY);
            var stairs = NativeStair<StairsUpPart>(cell, "StairsUp");
            if (stairs?.GetPart<ExaminablePart>() is ExaminablePart description
                && new SpreadWildernessSituationBuilder.Geometry(below, new HashSet<Entity>()).Walk(cell.X, cell.Y, null))
                description.Text += " A pale mineral stain runs up one side of the stone toward the surface.";
        }
    }
}
