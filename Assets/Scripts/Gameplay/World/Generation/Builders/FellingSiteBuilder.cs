using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>CoO-original staging of the Felling circle. All seven
    /// positions are reachable; arrival at (40,12) is ordinary ground.
    /// Exact geometry conveys no historical identities or compass order.</summary>
    public sealed class FellingSiteBuilder : IZoneBuilder
    {
        public string Name => "FellingSite";
        public int Priority => 1000;
        public const int WorldX = 3, WorldY = 5;
        public const string SiteName = "the Felling-Site";
        public const string ZoneID = "Overworld.3.5.0";
        public const int SeventhX = 33, SeventhY = 8;
        private static readonly (int x, int y)[] BarePositions =
            { (40, 5), (47, 8), (49, 14), (44, 18), (36, 18), (31, 14) };
        private static readonly string[] Required = { "TepuiStone", "FellingScar", "FellingBarePosition", "SeventhPosition" };

        public bool BuildZone(Zone zone, EntityFactory factory, Random rng)
        {
            if (zone == null || factory == null) return false;
            // Resolve every object before touching the old zone: factories
            // fail soft, so missing content cannot leave half a circle.
            foreach (string bp in Required) if (!factory.Blueprints.ContainsKey(bp)) return Refuse(zone, bp);
            foreach (var e in zone.GetReadOnlyEntities()) if (e.BlueprintName == "SeventhPosition") return true;
            var staged = new List<(Entity entity, int x, int y)>();
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++)
            {
                var stone = factory.CreateEntity("TepuiStone"); if (stone == null) return Refuse(zone, "TepuiStone");
                staged.Add((stone, x, y));
                double radius = (x - 40.0) * (x - 40.0) / 100.0 + (y - 12.0) * (y - 12.0) / 49.0;
                if (radius >= 0.83 && radius <= 1.17 && !IsPosition(x, y))
                {
                    var scar = factory.CreateEntity("FellingScar"); if (scar == null) return Refuse(zone, "FellingScar");
                    staged.Add((scar, x, y));
                }
            }
            foreach (var p in BarePositions)
            {
                var bare = factory.CreateEntity("FellingBarePosition"); if (bare == null) return Refuse(zone, "FellingBarePosition");
                staged.Add((bare, p.x, p.y));
            }
            var seventh = factory.CreateEntity("SeventhPosition"); if (seventh == null) return Refuse(zone, "SeventhPosition");
            staged.Add((seventh, SeventhX, SeventhY));
            foreach (var e in zone.GetAllEntities()) zone.RemoveEntity(e);
            zone.GenReservedCells.Clear();
            foreach (var p in staged) zone.AddEntity(p.entity, p.x, p.y);
            for (int x = 28; x <= 52; x++) for (int y = 3; y <= 21; y++) zone.GenReservedCells.Add((x, y));
            if (Diag.IsChannelEnabled("worldgen")) Diag.Record("worldgen", "FellingSiteBuilt", payload:
                new { zone = zone.ZoneID, barePositions = BarePositions.Length, seventhX = SeventhX, seventhY = SeventhY });
            return true;
        }
        private static bool IsPosition(int x, int y)
        {
            if (x == SeventhX && y == SeventhY) return true;
            foreach (var p in BarePositions) if (p.x == x && p.y == y) return true;
            return false;
        }
        private static bool Refuse(Zone zone, string blueprint)
        {
            if (Diag.IsChannelEnabled("worldgen")) Diag.Record("worldgen", "FellingSiteRefused", payload: new { zone = zone.ZoneID, blueprint });
            return false;
        }
    }
}
