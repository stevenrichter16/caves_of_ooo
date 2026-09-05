using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>A discoverable underfoot current, deliberately excluded from
    /// automatic stair searches. The floor end remembers its indicator spawn.</summary>
    public sealed class WaterPassagePart : Part
    {
        public override string Name => "WaterPassage";
        public bool IndicatorSpawned;
    }

    /// <summary>CoO-original Door connection within the Drowned-Sima family.
    /// The registry, marker entities and latch use the existing save graph.
    /// Pairing runs after successful generation, never inside a retried stamp.</summary>
    public static class HelmwoodPassages
    {
        public const string ConnectionType = "WaterPassage";
        private const string Blueprint = "HelmwoodWaterPassage";

        public static bool HasPassage(Cell cell)
        {
            if (cell == null) return false;
            for (int i = 0; i < cell.Objects.Count; i++)
                if (cell.Objects[i].HasPart<WaterPassagePart>()) return true;
            return false;
        }

        public static ZoneTransitionResult TryTravel(Entity actor, Zone zone, bool goingDown, ZoneManager manager)
        {
            if (actor == null || zone == null || manager == null)
                return Refuse(actor, "No usable water passage here.");
            var cell = zone.GetEntityCell(actor);
            if (!HasPassage(cell))
                return Refuse(actor, "No usable water passage here.");
            var connection = FindConnection(manager, zone.ZoneID, cell.X, cell.Y);
            // Unloading the surface removes its owned connections. A surviving
            // authored marker may lazily regenerate that peer, just as stairs
            // do. Never repair an orphan in an already cached, altered zone.
            if (connection == null && manager is OverworldZoneManager overworld
                && WorldMap.IsOverworldZoneID(zone.ZoneID)
                && MarkerAt(cell)?.BlueprintName == Blueprint)
            {
                var (wx, wy, wz) = WorldMap.FromZoneID(zone.ZoneID);
                var poi = overworld.WorldMap.GetPOI(wx, wy);
                if ((wz == 0 || wz == 2) && poi?.Type == POIType.Sinkhole
                    && SinkholeArchetypes.ForSite(poi) == SinkholeArchetype.DrownedSima)
                {
                    string peer = $"Overworld.{wx}.{wy}.{(wz == 0 ? 2 : 0)}";
                    if (!manager.CachedZones.ContainsKey(peer)) manager.GetZone(peer);
                    connection = FindConnection(manager, zone.ZoneID, cell.X, cell.Y);
                }
            }
            if (connection == null) return Refuse(actor, "The water has no open route beyond this crack.");
            bool fromSource = connection.SourceZoneID == zone.ZoneID;
            string destination = fromSource ? connection.TargetZoneID : connection.SourceZoneID;
            int depthChange = WorldMap.GetDepth(destination) - WorldMap.GetDepth(zone.ZoneID);
            if (depthChange == 0 || (depthChange > 0) != goingDown)
                return Refuse(actor, "The passage runs in the other direction.");
            var targetZone = manager.GetZone(destination);
            if (targetZone == null) return Refuse(actor, "The far end of the passage is closed.");
            // Generation may have repaired a missing peer and its coordinates.
            // Resolve again before committing; never travel to a stale endpoint.
            connection = FindConnection(manager, zone.ZoneID, cell.X, cell.Y);
            if (connection == null) return Refuse(actor, "The far end of the passage is closed.");
            fromSource = connection.SourceZoneID == zone.ZoneID;
            if ((fromSource ? connection.TargetZoneID : connection.SourceZoneID) != destination)
                return Refuse(actor, "The current has changed its course.");
            int x = fromSource ? connection.TargetX : connection.SourceX;
            int y = fromSource ? connection.TargetY : connection.SourceY;
            var arrival = targetZone.GetCell(x, y);
            if (!HasPassage(arrival) || arrival.BlocksMovement())
                return Refuse(actor, "The far end of the passage is blocked.");
            zone.RemoveEntity(actor);
            targetZone.AddEntity(actor, x, y);
            var brain = actor.GetPart<BrainPart>();
            if (brain != null) brain.CurrentZone = targetZone;
            ZoneTransitionSystem.TransitPartyMembers(actor, zone, targetZone, x, y);
            if (Diag.IsChannelEnabled("worldmap"))
                Diag.Record("worldmap", "WaterPassageTravel", actor, null,
                    new { source = zone.ZoneID, destination, x, y });
            return new ZoneTransitionResult { Success = true, NewZone = targetZone, NewPlayerX = x, NewPlayerY = y };
        }

        private static ZoneConnection FindConnection(ZoneManager manager, string zoneID, int x, int y)
        {
            var connections = manager.GetConnections(zoneID);
            for (int i = 0; i < connections.Count; i++)
            {
                var c = connections[i];
                if (c.Type != ConnectionType) continue;
                if (c.SourceZoneID == zoneID && c.SourceX == x && c.SourceY == y) return c;
                if (c.TargetZoneID == zoneID && c.TargetX == x && c.TargetY == y) return c;
            }
            return null;
        }

        private static ZoneTransitionResult Refuse(Entity actor, string reason)
        {
            if (Diag.IsChannelEnabled("worldmap"))
                Diag.Record("worldmap", "WaterPassageRejected", actor, null, new { reason });
            return new ZoneTransitionResult { ErrorReason = reason };
        }

        public static void OnZoneGenerated(Zone generated, OverworldZoneManager manager)
        {
            if (generated == null || manager?.Factory == null || !WorldMap.IsOverworldZoneID(generated.ZoneID)) return;
            var (wx, wy, wz) = WorldMap.FromZoneID(generated.ZoneID);
            if (wz != 0 && wz != 2) return;
            var poi = manager.WorldMap.GetPOI(wx, wy);
            if (poi == null || poi.Type != POIType.Sinkhole
                || SinkholeArchetypes.ForSite(poi) != SinkholeArchetype.DrownedSima) return;
            // Stable one-in-eight worlds, independent of generation order/RNG.
            if (FormationSelector.StableIndex("HelmwoodDoor|" + manager.WorldSeed + "|" + wx + "|" + wy, 8) != 0) return;
            string surfaceID = $"Overworld.{wx}.{wy}.0", floorID = $"Overworld.{wx}.{wy}.2";
            Zone surface = generated, floor = generated;
            if (wz != 0 && !manager.CachedZones.TryGetValue(surfaceID, out surface)) return;
            if (wz != 2 && !manager.CachedZones.TryGetValue(floorID, out floor)) return;
            var factory = manager.Factory;
            if (!factory.Blueprints.ContainsKey(Blueprint) || !factory.Blueprints.ContainsKey("HelmwoodFrog")) return;
            var surfaceCell = FindEndpoint(surface, false);
            var floorCell = FindEndpoint(floor, true);
            if (surfaceCell == null || floorCell == null) return;
            // Instantiate both ends before any world mutation. Existing ends
            // retain their saved indicator latch and player-altered surroundings.
            Entity source = MarkerAt(surfaceCell), target = MarkerAt(floorCell);
            source = source ?? factory.CreateEntity(Blueprint);
            target = target ?? factory.CreateEntity(Blueprint);
            if (source?.GetPart<WaterPassagePart>() == null || target?.GetPart<WaterPassagePart>() == null) return;
            if (surface.GetEntityCell(source) == null) surface.AddEntity(source, surfaceCell.X, surfaceCell.Y);
            if (floor.GetEntityCell(target) == null) floor.AddEntity(target, floorCell.X, floorCell.Y);

            // Loaded registries contain value copies under both keys. Replace
            // both indexes, including old coordinates left by endpoint unload.
            manager.GetConnections(surfaceID).RemoveAll(c => IsThisPair(c, surfaceID, floorID));
            manager.GetConnections(floorID).RemoveAll(c => IsThisPair(c, surfaceID, floorID));
            manager.RegisterConnection(new ZoneConnection { Type = ConnectionType,
                SourceZoneID = surfaceID, SourceX = surfaceCell.X, SourceY = surfaceCell.Y,
                TargetZoneID = floorID, TargetX = floorCell.X, TargetY = floorCell.Y });

            var marker = target.GetPart<WaterPassagePart>();
            if (!marker.IndicatorSpawned)
            {
                foreach (var e in floor.GetReadOnlyEntities())
                    if (e.BlueprintName == "HelmwoodFrog") { marker.IndicatorSpawned = true; break; }
                if (!marker.IndicatorSpawned)
                    for (int radius = 1; radius <= 2 && !marker.IndicatorSpawned; radius++)
                        for (int dx = -radius; dx <= radius && !marker.IndicatorSpawned; dx++)
                            for (int dy = -radius; dy <= radius && !marker.IndicatorSpawned; dy++)
                            {
                                if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius) continue;
                                var c = floor.GetCell(floorCell.X + dx, floorCell.Y + dy);
                                if (c == null || c.BlocksMovement() || floor.GenReservedCells.Contains((c.X, c.Y))) continue;
                                var frog = factory.CreateEntity("HelmwoodFrog");
                                if (frog?.GetPart<BrainPart>() == null) continue;
                                floor.AddEntity(frog, c.X, c.Y);
                                frog.GetPart<BrainPart>().CurrentZone = floor;
                                frog.GetPart<BrainPart>().Rng = new Random(manager.WorldSeed);
                                if (manager.ActiveZone == floor) TurnManager.Active?.AddEntity(frog);
                                marker.IndicatorSpawned = true;
                            }
            }
            if (Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen", "HelmwoodPassagePaired", target, null,
                    new { surfaceID, floorID, indicator = marker.IndicatorSpawned });
        }

        private static bool IsThisPair(ZoneConnection c, string surface, string floor)
            => c.Type == ConnectionType && c.SourceZoneID == surface && c.TargetZoneID == floor;

        private static Entity MarkerAt(Cell cell)
        {
            for (int i = 0; i < cell.Objects.Count; i++)
                if (cell.Objects[i].HasPart<WaterPassagePart>()) return cell.Objects[i];
            return null;
        }

        private static Cell FindEndpoint(Zone zone, bool preferBasin)
        {
            foreach (var e in zone.GetReadOnlyEntities())
                if (e.HasPart<WaterPassagePart>()) return zone.GetEntityCell(e);
            // Prefer real basin water below. Surface endpoint is itself a
            // freshwater pool; never carve, replace items or overwrite a POI.
            for (int pass = preferBasin ? 0 : 1; pass <= 1; pass++)
                for (int x = 3; x < Zone.Width - 3; x++)
                    for (int y = 3; y < Zone.Height - 3; y++)
                    {
                        var cell = zone.GetCell(x, y);
                        if (cell.BlocksMovement() || zone.GenReservedCells.Contains((x, y))) continue;
                        bool occupied = false;
                        for (int i = 0; i < cell.Objects.Count; i++)
                        {
                            var e = cell.Objects[i];
                            if (e.HasTag("Creature") || e.GetPart<PhysicsPart>()?.Takeable == true
                                || e.HasPart<StairsDownPart>() || e.HasPart<StairsUpPart>()) { occupied = true; break; }
                        }
                        if (occupied || (pass == 0 && !StumpFaunaHabitat.Contains(cell, "MirePool"))) continue;
                        return cell;
                    }
            return null;
        }
    }
}
