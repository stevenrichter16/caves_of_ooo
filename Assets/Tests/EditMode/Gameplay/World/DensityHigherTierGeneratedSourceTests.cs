using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class DensityHigherTierGeneratedSourceTests
    {
        [Serializable] public class Row { public int seed, depth, sources, value; public string biome, zone; public List<string> items = new List<string>(); public bool keyReachable = true, chestReachable = true; }
        [Serializable] public class Report { public List<Row> rows = new List<Row>(); public string boundary = "Integrated C10/C11 source generated in the executing runtime; the standalone runner uses a stable hash whose maps differ from Unity. Reachability ignores living creatures that can be fought, but retains solid terrain/props. No native input, combat balance or visual claim."; }
        [Test]
        public void OrdinaryDeepGeneratedZonesActuallySourceFindsAndPreserveExcludedColumns()
        {
            var report = new Report();
            using (var scope = new DensityLootTestScope())
            {
                foreach (int seed in DensityLootCensusTests.Seeds)
                {
                    var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed);
                    foreach (var biome in new[] { BiomeType.Spread, BiomeType.Sodden, BiomeType.Beating, BiomeType.Grovelands })
                    {
                        var pos = Enumerable.Range(0, WorldMap.Height).SelectMany(y => Enumerable.Range(0, WorldMap.Width).Select(x => (x, y)))
                            .First(p => manager.WorldMap.GetBiome(p.x, p.y) == biome && manager.WorldMap.GetPOI(p.x, p.y) == null
                                && !OverworldZoneManager.AuthoredWildernessZoneIDs.Contains(WorldMap.ToZoneID(p.x, p.y, 0))
                                && WorldMap.ToZoneID(p.x, p.y, 0) != MultiCellPilotRuntime.ZoneID);
                        foreach (int depth in new[] { 8, 9, 12 })
                        {
                            scope.Seed(unchecked(seed * 31 + pos.x * 1009 + pos.y * 97 + depth));
                            var zone = manager.GetZone(WorldMap.ToZoneID(pos.x, pos.y, depth));
                            var row = new Row { seed = seed, biome = biome.ToString(), depth = depth, zone = zone.ZoneID };
                            foreach (var chest in zone.GetReadOnlyEntities().Where(e => e.HasPart<ContainerPart>()))
                            {
                                var items = chest.GetPart<ContainerPart>().Contents.Where(i => DensityHigherTierFindsTests.Names.Contains(i.BlueprintName)).ToArray();
                                if (items.Length == 0) continue;
                                row.sources++; row.items.AddRange(items.Select(i => i.BlueprintName)); row.value += items.Sum(TradeSystem.GetItemValue);
                                Assert.AreEqual(1, items.Length); Assert.AreEqual("LockedChest", chest.BlueprintName); Assert.IsTrue(chest.GetPart<LockPart>().IsLocked);
                                var c = zone.GetEntityPosition(chest); var key = zone.GetCell(c.x + 2, c.y + 1).Objects.SingleOrDefault(e => e.BlueprintName == "IronKey");
                                Assert.NotNull(key, zone.ZoneID + " source key");
                                Assert.IsTrue(zone.GetCell(c.x + 3, c.y).Objects.Any(e => e.BlueprintName == "VaultSentinel"));
                                var reachable = Reachable(zone);
                                row.keyReachable &= Near(reachable, zone.GetEntityPosition(key)); row.chestReachable &= Near(reachable, c);
                            }
                            if (depth < 9 || biome == BiomeType.Grovelands) Assert.AreEqual(0, row.sources, "excluded context " + zone.ZoneID);
                            report.rows.Add(row);
                        }
                    }
                }
            }
            string path = Environment.GetEnvironmentVariable("COO_DENSITY_T4_OUTPUT") ?? Path.Combine(Path.GetTempPath(), "density-tier4-source-census.json");
            File.WriteAllText(path, JsonUtility.ToJson(report, true)); TestContext.Progress.WriteLine("T4 source census: " + path);
            Assert.AreEqual(60, report.rows.Count); Assert.Greater(report.rows.Sum(r => r.sources), 0, "actual ordinary deep source must be reachable in the bounded corpus");
            Assert.IsTrue(report.rows.All(r => r.keyReachable && r.chestReachable), "a generated source must have a real terrain/prop route to its key and lock");
        }
        static bool Near(HashSet<(int x, int y)> reachable, (int x, int y) p) => reachable.Any(r => Math.Abs(r.x-p.x)<=1 && Math.Abs(r.y-p.y)<=1);
        static HashSet<(int x, int y)> Reachable(Zone zone)
        {
            var origin = zone.GetReadOnlyEntities().First(e => e.HasPart<StairsUpPart>()); var start = zone.GetEntityPosition(origin);
            var seen = new HashSet<(int x, int y)> { start }; var q = new Queue<(int x, int y)>(); q.Enqueue(start);
            while(q.Count>0)
            {
                var c=q.Dequeue();
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {
                    if(dx==0&&dy==0)continue;var next=(x:c.x+dx,y:c.y+dy);var cell=zone.GetCell(next.x,next.y);
                    if(cell==null||seen.Contains(next)||cell.Occupants.Any(e=>!e.HasTag("Creature")&&(e.HasTag("Solid")||e.GetPart<PhysicsPart>()?.Solid==true)))continue;
                    seen.Add(next);q.Enqueue(next);
                }
            }
            return seen;
        }
    }
}
