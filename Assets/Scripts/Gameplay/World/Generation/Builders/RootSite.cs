using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The other advertised routes, ER.1 (Docs/ENDING-ROUTES.md): the Root as a
    /// place. World cell (3,3), the stump's crown two cells north of the
    /// Felling-Site, reserved by the authored map and built here: a mouth on the
    /// surface (the cleft's way down, loose tepuibone beside it) and one chamber
    /// below, where the Tree's sleeping taproot shows its face. The face is a
    /// place before it is a choice; ER.2 offers the enactments there.
    /// </summary>
    public static class RootSiteBuilder
    {
        public const int WorldX = 3, WorldY = 3;
        public const string SiteName = "the Root";
        public const string MouthZoneID = "Overworld.3.3.0", ChamberZoneID = "Overworld.3.3.1";
        public const string FaceBlueprint = "TheRoot", FaceId = "root:face";
        /// <summary>The way down, sought outward from the crown's centre.</summary>
        public const int CleftX = 40, CleftY = 12;
        /// <summary>The face sits on the east rim of a hollow carved west of it.</summary>
        public const int FaceX = 46, FaceY = 12, HollowRadiusX = 6, HollowRadiusY = 3;
        public const int LooseTepuibone = 3, LooseRadius = 3;

        internal static (int x, int y)? FindOpenNear(Zone zone, int x, int y, int maxRadius)
        {
            for (int r = 0; r <= maxRadius; r++)
                foreach (var c in Ring((x, y), r))
                {
                    var cell = zone.GetCell(c.x, c.y);
                    if (cell != null && c.x > 0 && c.x < Zone.Width - 1 && c.y > 0 && c.y < Zone.Height - 1 && !cell.BlocksMovement()) return c;
                }
            return null;
        }

        /// <summary>Cells at Chebyshev distance r, in a fixed order (deterministic worlds).</summary>
        internal static IEnumerable<(int x, int y)> Ring((int x, int y) c, int r)
        {
            if (r == 0) { yield return c; yield break; }
            for (int dx = -r; dx <= r; dx++) for (int dy = -r; dy <= r; dy++)
                if (Math.Abs(dx) == r || Math.Abs(dy) == r) yield return (c.x + dx, c.y + dy);
        }

        /// <summary>Shortest four-neighbour path from start into the goal set through
        /// anything but the zone's border; the caller carves it. Null when no path exists.</summary>
        internal static List<(int x, int y)> ShortestPath(Zone zone, (int x, int y) start, HashSet<(int x, int y)> goal)
        {
            var prev = new Dictionary<(int x, int y), (int x, int y)> { [start] = start };
            var q = new Queue<(int x, int y)>(); q.Enqueue(start);
            (int x, int y)? found = goal.Contains(start) ? start : null;
            while (q.Count > 0 && found == null)
            {
                var p = q.Dequeue();
                foreach (var d in new[] { (0, -1), (0, 1), (-1, 0), (1, 0) })
                {
                    var n = (x: p.x + d.Item1, y: p.y + d.Item2);
                    if (n.x <= 0 || n.x >= Zone.Width - 1 || n.y <= 0 || n.y >= Zone.Height - 1 || prev.ContainsKey(n)) continue;
                    prev[n] = p; if (goal.Contains(n)) { found = n; break; } q.Enqueue(n);
                }
            }
            if (found == null) return null;
            var path = new List<(int x, int y)>();
            for (var p = found.Value; ; p = prev[p]) { path.Add(p); if (p == start) break; }
            return path;
        }
    }

    /// <summary>z=0: the stump's own wilderness with the cleft's way down at the
    /// crown, registered to the chamber, and loose tepuibone beside it — the
    /// first of the three name-holding stones lies where the Root is.</summary>
    public sealed class RootMouthBuilder : IZoneBuilder
    {
        public string Name => "RootMouth";
        public int Priority => 3650;
        private readonly ZoneManager _zoneManager;
        public RootMouthBuilder(ZoneManager zoneManager) { _zoneManager = zoneManager; }

        public bool BuildZone(Zone zone, EntityFactory factory, Random rng)
        {
            if (zone == null || factory == null) return false;
            if (zone.ZoneID != RootSiteBuilder.MouthZoneID) return Refuse(zone, "not_the_root");
            foreach (var bp in new[] { "StairsDown", "Tepuibone" }) if (!factory.Blueprints.ContainsKey(bp)) return Refuse(zone, "missing:" + bp);
            if (StairsUpBuilder.FindStair(zone, false) != null) return true; // built once
            var seat = RootSiteBuilder.FindOpenNear(zone, RootSiteBuilder.CleftX, RootSiteBuilder.CleftY, 12);
            if (seat == null) return Refuse(zone, "no_open_cleft");
            var stairs = factory.CreateEntity("StairsDown");
            if (stairs == null || !zone.AddEntity(stairs, seat.Value.x, seat.Value.y)) return Refuse(zone, "stairs_placement");
            string below = WorldMap.GetZoneBelow(zone.ZoneID);
            if (_zoneManager != null && below != null)
                _zoneManager.RegisterConnection(new ZoneConnection
                {
                    SourceZoneID = zone.ZoneID, SourceX = seat.Value.x, SourceY = seat.Value.y,
                    TargetZoneID = below, TargetX = seat.Value.x, TargetY = seat.Value.y, Type = "StairsDown"
                });
            int loose = 0; var taken = new HashSet<(int x, int y)> { seat.Value };
            for (int r = 1; r <= RootSiteBuilder.LooseRadius && loose < RootSiteBuilder.LooseTepuibone; r++)
                foreach (var c in RootSiteBuilder.Ring(seat.Value, r))
                {
                    if (loose >= RootSiteBuilder.LooseTepuibone) break;
                    var cell = zone.GetCell(c.x, c.y);
                    if (cell == null || cell.BlocksMovement() || taken.Contains(c)) continue;
                    if (BuilderSpawn.TryPlace(zone, factory, "Tepuibone", c.x, c.y) != null) { loose++; taken.Add(c); }
                }
            Diag.Record("worldgen", "RootMouth", payload: new { zone = zone.ZoneID, x = seat.Value.x, y = seat.Value.y, loose, linked = _zoneManager != null && below != null });
            return true;
        }

        private static bool Refuse(Zone zone, string reason)
        {
            Diag.Record("worldgen", "RootRefused", payload: new { zone = zone?.ZoneID, reason });
            return false;
        }
    }

    /// <summary>z=1: the ordinary underground base with a hollow carved west of
    /// the face, the face set into its east rim, a shortest approach carved from
    /// the way up, and nothing going further down. Built once; a revisit keeps it.</summary>
    public sealed class RootChamberBuilder : IZoneBuilder
    {
        public string Name => "RootChamber";
        public int Priority => 3650;

        public bool BuildZone(Zone zone, EntityFactory factory, Random rng)
        {
            if (zone == null || factory == null) return false;
            if (zone.ZoneID != RootSiteBuilder.ChamberZoneID) return Refuse(zone, "not_the_root");
            var (_, floorBP) = SolidEarthBuilder.GetMaterialsForDepth(WorldMap.GetDepth(zone.ZoneID));
            foreach (var bp in new[] { RootSiteBuilder.FaceBlueprint, floorBP }) if (!factory.Blueprints.ContainsKey(bp)) return Refuse(zone, "missing:" + bp);
            foreach (var e in zone.GetReadOnlyEntities()) if (e.HasPart<RootFacePart>()) return true; // built once
            var up = StairsUpBuilder.FindStair(zone, true);
            if (up == null) return Refuse(zone, "no_way_up");
            var u = zone.GetEntityPosition(up);

            var facePos = (x: RootSiteBuilder.FaceX, y: RootSiteBuilder.FaceY);
            if (facePos == (u.x, u.y)) facePos = (facePos.x + 1, facePos.y);
            var hollow = new HashSet<(int x, int y)>();
            int cx = facePos.x - RootSiteBuilder.HollowRadiusX, cy = facePos.y;
            for (int x = cx - RootSiteBuilder.HollowRadiusX; x < facePos.x; x++)
                for (int y = cy - RootSiteBuilder.HollowRadiusY; y <= cy + RootSiteBuilder.HollowRadiusY; y++)
                {
                    double nx = (x - cx) / (double)RootSiteBuilder.HollowRadiusX, ny = (y - cy) / (double)RootSiteBuilder.HollowRadiusY;
                    if (nx * nx + ny * ny <= 1.0 && x > 0 && x < Zone.Width - 1 && y > 0 && y < Zone.Height - 1) hollow.Add((x, y));
                }
            var approach = RootSiteBuilder.ShortestPath(zone, (u.x, u.y), hollow);
            if (approach == null) return Refuse(zone, "unreachable_hollow");
            var floors = new HashSet<(int x, int y)>(hollow);
            foreach (var p in approach) floors.Add(p);
            floors.Remove(facePos);

            // Stage, clear, place — as the Sealed Library does: stairs survive, everything else in a carved cell goes.
            var staged = new List<(Entity e, int x, int y)>();
            foreach (var p in floors)
            {
                var f = factory.CreateEntity(floorBP); if (f == null) return Refuse(zone, "floor_creation");
                staged.Add((f, p.x, p.y));
            }
            var face = factory.CreateEntity(RootSiteBuilder.FaceBlueprint);
            if (face?.GetPart<RootFacePart>() == null || face.GetPart<ExaminablePart>() == null || face.GetPart<PhysicsPart>()?.Solid != true) return Refuse(zone, "face_incomplete");
            face.ID = RootSiteBuilder.FaceId;
            foreach (var p in floors) Clear(zone, p);
            Clear(zone, facePos);
            foreach (var s in staged) zone.AddEntity(s.e, s.x, s.y);
            if (!zone.AddEntity(face, facePos.x, facePos.y)) return Refuse(zone, "face_placement");
            Diag.Record("worldgen", "RootPlaced", target: face,
                payload: new { zone = zone.ZoneID, x = facePos.x, y = facePos.y, hollow = hollow.Count, approach = approach.Count, stairsX = u.x, stairsY = u.y });
            return true;
        }

        private static void Clear(Zone zone, (int x, int y) p)
        {
            var cell = zone.GetCell(p.x, p.y); if (cell == null) return;
            for (int i = cell.Objects.Count - 1; i >= 0; i--)
                if (!cell.Objects[i].HasPart<StairsUpPart>() && !cell.Objects[i].HasPart<StairsDownPart>()) zone.RemoveEntity(cell.Objects[i]);
            zone.GenReservedCells.Add(p);
        }

        private static bool Refuse(Zone zone, string reason)
        {
            Diag.Record("worldgen", "RootRefused", payload: new { zone = zone?.ZoneID, reason });
            return false;
        }
    }
}
