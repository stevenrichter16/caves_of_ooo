using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class DensityLairStackCensusTests
    {
        [Serializable] public sealed class CacheItem
        { public string blueprint, category; public int units; public double weight,value; }
        [Serializable] public sealed class Cache
        { public string blueprint;public bool claimedFinal,locked;public int entries,units,largestStack;public double weight;public List<CacheItem> items=new List<CacheItem>(); }
        [Serializable] public sealed class Row
        {
            public int seed, x, y, tier, depth, floors, creatures, bosses, containers, mimics, traps, itemEntities, itemUnits;
            public string mode, surface, biome, boss;
            public double cacheCommerce, carriedCommerce, groundCommerce;
            public string[] species, cacheKinds;
            public List<Cache> caches=new List<Cache>();
        }
        [Serializable] public sealed class Report
        {
            public string canVerify = "All generated lair columns in five fixed world maps; old single surface versus entire new stack; actual instantiated stock and gear. Gear RNG is reset per floor. Commerce counts stock regardless of access or combat acquisition.";
            public string cannotVerify = "Standalone stable-hash layouts differ from native Unity. This is a finite structural and generated-stock census, not sale income, natural discovery, encounter difficulty or survival balance.";
            public List<Row> rows = new List<Row>();
        }
        [Test]
        public void FiveWorldsCompareEveryOldLairWithItsWholeNewStack()
        {
            var report = new Report();
            using (var scope = new DensityLootTestScope())
            {
                foreach (int seed in DensityLootCensusTests.Seeds)
                {
                    var reference = OverworldZoneManager.CreateDetached(scope.Factory, seed);
                    int lairs = 0;
                    for (int x = 0; x < WorldMap.Width; x++) for (int y = 0; y < WorldMap.Height; y++)
                    {
                        var poi = reference.WorldMap.GetPOI(x, y);
                        if (poi == null || poi.Type != POIType.Lair) continue;
                        lairs++;
                        foreach (bool legacy in new[] { true, false })
                        {
                            var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed);
                            if (legacy) LairStacks.Restore(manager, null);
                            string surface = WorldMap.ToZoneID(x, y, 0);
                            var row = new Row { seed = seed, x = x, y = y, surface = surface, mode = legacy ? "old-single-floor" : "new-whole-stack", biome = reference.WorldMap.Tiles[x, y].ToString(), tier = poi.Tier, boss = poi.BossBlueprint };
                            var species = new List<string>(); var kinds = new List<string>();
                            int final = legacy ? 0 : (poi.Tier <= 2 ? 1 : 2);
                            for (int depth = 0; depth <= final; depth++)
                            {
                                scope.Seed(unchecked(seed ^ (x * 73856093) ^ (y * 19349663) ^ (depth * 83492791)));
                                var zone = manager.GetZone(WorldMap.ToZoneID(x, y, depth));
                                Assert.NotNull(zone, surface + " " + depth + " " + row.mode);
                                row.floors++; row.depth = depth;
                                foreach (var entity in zone.GetReadOnlyEntities())
                                {
                                    if (entity.HasTag("Creature"))
                                    {
                                        row.creatures++; species.Add(entity.BlueprintName);
                                        if (entity.BlueprintName == poi.BossBlueprint) row.bosses++;
                                        foreach (var item in DensityLootTestScope.Gear(entity)) Add(row, item, 1);
                                    }
                                    if (entity.HasTag("Trap")) row.traps++;
                                    var cache = entity.GetPart<ContainerPart>();
                                    if (cache != null)
                                    {
                                        row.containers++; kinds.Add(entity.BlueprintName);
                                        if (entity.HasPart<AIAmbushPart>()) row.mimics++;
                                        var observation=new Cache{blueprint=entity.BlueprintName,claimedFinal=!legacy&&LairStacks.Inspect(manager,surface).RewardID==entity.ID,locked=cache.IsLocked,entries=cache.Contents.Count};
                                        foreach (var item in cache.Contents)
                                        {
                                            Add(row,item,0);int units=item.GetPart<StackerPart>()?.StackCount??1;
                                            double weight=InventoryPart.GetItemWeight(item);
                                            observation.units+=units;observation.largestStack=Math.Max(observation.largestStack,units);observation.weight+=weight;
                                            observation.items.Add(new CacheItem{blueprint=item.BlueprintName,category=Category(item),units=units,weight=weight,value=TradeSystem.GetItemValue(item)});
                                        }
                                        row.caches.Add(observation);
                                    }
                                    if (!entity.HasTag("Creature") && cache == null && entity.GetPart<PhysicsPart>()?.Takeable == true)
                                        Add(row, entity, 2);
                                }
                            }
                            row.species = species.OrderBy(n => n).ToArray(); row.cacheKinds = kinds.OrderBy(n => n).ToArray();
                            Assert.AreEqual(1, row.bosses, surface + " " + row.mode);
                            if (!legacy) { Assert.LessOrEqual(row.creatures, 10); Assert.AreEqual(final + 1, row.containers - row.mimics); Assert.That(row.traps, Is.InRange(1, 2)); }
                            report.rows.Add(row);
                        }
                    }
                    Assert.That(lairs, Is.InRange(3, 5), "actual finite world lairs seed=" + seed);
                }
            }
            string output=Environment.GetEnvironmentVariable("COO_LAIR_CENSUS_OUTPUT")
                ?? Path.Combine(Path.GetTempPath(),"coo-density-lair-stack-census.json");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            File.WriteAllText(output,JsonUtility.ToJson(report,true));
            double before=report.rows.Where(r=>r.mode=="old-single-floor").Sum(r=>r.cacheCommerce+r.groundCommerce);
            double after=report.rows.Where(r=>r.mode=="new-whole-stack").Sum(r=>r.cacheCommerce+r.groundCommerce);
            Assert.That(after/before,Is.InRange(.75,1.35),"whole-stack sources retain the reviewed aggregate budget");
        }
        static string Category(Entity item)
        {
            if(item.HasPart<MeleeWeaponPart>())return "weapon";
            if(item.HasPart<ArmorPart>())return "armor";
            if(item.HasPart<GrimoirePart>())return "grimoire";
            if(item.BlueprintName=="GoldCoin")return "currency";
            if(item.HasPart<TonicPart>())return "tonic";
            if(item.HasPart<FoodPart>())return "food";
            return "other-material-or-utility";
        }
        static void Add(Row row, Entity item, int kind)
        {
            row.itemEntities++; row.itemUnits += item.GetPart<StackerPart>()?.StackCount ?? 1;
            double value = TradeSystem.GetItemValue(item);
            if (kind == 0) row.cacheCommerce += value;
            else if (kind == 1) row.carriedCommerce += value;
            else row.groundCommerce += value;
        }
    }
}
