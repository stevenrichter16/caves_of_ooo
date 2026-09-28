using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;
namespace CavesOfOoo.Tests
{
    /// <summary>Measures an intentional fixed expedition reward replacement,
    /// never claims zero economy impact or acquired player income.</summary>
    public sealed class SpreadWayhouseEconomyCensusTests
    {
        DensityLootTestScope scope;bool oldWorldgen;
        [SetUp]public void Setup(){scope=new DensityLootTestScope();oldWorldgen=Diag.IsChannelEnabled("worldgen");Diag.SetChannel("worldgen",true);}
        [TearDown]public void Cleanup(){Diag.SetChannel("worldgen",oldWorldgen);scope.Dispose();}
        sealed class Capture:IZoneBuilder
        {readonly Action<Zone> callback;internal Capture(Action<Zone> callback){this.callback=callback;}public string Name=>"TestOnlyBeforeWayhouse";public int Priority=>4299;public bool BuildZone(Zone z,EntityFactory f,Random r){callback(z);return true;}}
        sealed class Observed:OverworldZoneManager
        {
            readonly bool baseline;internal Entity[] Owners;internal (int x,int y)[] Positions;internal string[] Facts;
            internal Entity[] Actors,Containers;
            internal Observed(EntityFactory f,int seed,bool baseline):base(f,seed){this.baseline=baseline;}
            protected override ZoneGenerationPipeline GetPipelineForZone(string id)
            {
                var p=base.GetPipelineForZone(id);Assert.AreEqual(1,p.Builders.OfType<SpreadWayhouseBuilder>().Count(),"actual selected wayhouse pipeline");
                if(baseline)p.RemoveBuilders<SpreadWayhouseBuilder>();
                var actors=p.Builders.OfType<PopulationBuilder>().Single();var containers=p.Builders.OfType<ContainerBuilder>().Single();
                p.AddBuilder(new Capture(z=>{Owners=z.GetReadOnlyEntities().ToArray();Positions=Owners.Select(e=>z.GetEntityPosition(e)).ToArray();Facts=Owners.Select(Exact).ToArray();Actors=actors.SourceReceipt.Owners.ToArray();Containers=containers.SourceReceipt.Owners.ToArray();}));return p;
            }
        }
        [Serializable]public sealed class ItemRow{public string id,blueprint;public int units,baseValue,totalValue;public bool noTrade;}
        [Serializable]public sealed class Report
        {
            public int seed,worldX,worldY,mapDistanceFromSill,mapDistanceFromDefaultGlade,oldHostiles,newHostiles,oldContainers,newContainers,ordinaryLootContainersBefore,ordinaryLootContainersAfter,removedBushes;
            public string zone,boundary,rewardId,keyId,refusalReason;public string[] generationReceipts;public bool installed;public string[] oldHostileBlueprints,oldHostileIds;
            public int oldCacheStockValue,oldHostileGearValue,newGuardGearValue,rewardValue,totalStockGearBefore,totalStockGearAfter,intentionalStockGearDelta;
            public ItemRow[] replacedStock,oldHostileGear,newGuardGear;
        }
        [Serializable]public sealed class Refusal{public string zone,reason;}
        static int Units(Entity e)=>e.GetPart<StackerPart>()?.StackCount??1;
        static int Value(Entity e)=>e.HasTag("NoTrade")?0:(e.GetPart<CommercePart>()?.Value??0)*Units(e);
        static string Item(Entity e)=>e.BlueprintName+":"+Units(e)+":"+(e.GetPart<CommercePart>()?.Value??0)+":"+e.HasTag("NoTrade")+":"+string.Join("/",e.Statistics.OrderBy(s=>s.Key).Select(s=>s.Key+":"+s.Value.BaseValue+":"+s.Value.Value));
        static string Shape(Entity e)=>Item(e)+";stock="+string.Join(",",(e.GetPart<ContainerPart>()?.Contents??new List<Entity>()).Select(Item).OrderBy(s=>s))+";gear="+string.Join(",",DensityLootTestScope.Gear(e).Select(Item).OrderBy(s=>s));
        static string Exact(Entity e)=>e.ID+":"+Shape(e)+":"+string.Join(",",Goods(e).Select(i=>i.ID+":"+i.GetPart<PhysicsPart>()?.InInventory?.ID+":"+i.GetPart<PhysicsPart>()?.Equipped?.ID).OrderBy(s=>s));
        static IEnumerable<Entity> Goods(Entity e)=>(e.GetPart<ContainerPart>()?.Contents??new List<Entity>()).Concat(DensityLootTestScope.Gear(e)).Distinct();
        static int Total(Zone z)=>z.GetReadOnlyEntities().SelectMany(Goods).Distinct().Sum(Value);
        static ItemRow[] Rows(IEnumerable<Entity> items)=>items.Distinct().Select(e=>new ItemRow{id=e.ID,blueprint=e.BlueprintName,units=Units(e),baseValue=e.GetPart<CommercePart>()?.Value??0,totalValue=Value(e),noTrade=e.HasTag("NoTrade")}).ToArray();
        static void Write(Report report)
        {string json=JsonUtility.ToJson(report,true);TestContext.WriteLine(json);string output=Environment.GetEnvironmentVariable("COO_WAYHOUSE_ECONOMY_CENSUS_OUTPUT");if(!string.IsNullOrEmpty(output)){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"seed-"+report.seed+".json"),json);}}
        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void ActualColdWayhouseReplacementMeasuresBoundedIntentionalRewardDelta(int seed)
        {
            var baseline=new Observed(scope.Factory,seed,true);string id=baseline.Wayhouse.ZoneID;Assert.IsNotEmpty(id);int globalSeed=unchecked(seed^FormationSelector.StableIndex(id,int.MaxValue));scope.Seed(globalSeed);
            var before=baseline.GetZone(id);Assert.NotNull(before);var beforeOwners=before.GetReadOnlyEntities().ToArray();var beforeShapes=beforeOwners.Select(Shape).ToArray();var beforePositions=beforeOwners.Select(e=>before.GetEntityPosition(e)).ToArray();
            int totalBefore=Total(before),containerCount=beforeOwners.Count(e=>e.HasPart<ContainerPart>()),creatureCount=beforeOwners.Count(e=>e.HasTag("Creature"));
            scope.Seed(globalSeed);var current=new Observed(scope.Factory,seed,false);Assert.AreEqual(id,current.Wayhouse.ZoneID,"same frozen selected address");var oldTraceIds=new HashSet<string>(Diag.Snapshot(Diag.BufferCapacity).Select(e=>e.TraceId));var after=current.GetZone(id);Assert.NotNull(after,"actual selected zone generates");
            var sourceRecords=Diag.Snapshot(Diag.BufferCapacity).Where(e=>!oldTraceIds.Contains(e.TraceId)&&e.Category=="worldgen"&&(e.Kind=="SpreadWayhouseRefused"||e.Kind=="SpreadWayhouseStaged")).ToArray();
            var generationReceipts=sourceRecords.Select(e=>e.Kind+":"+e.PayloadJson).ToArray();
            CollectionAssert.AreEqual(beforeShapes,current.Owners.Select(Shape),"all ordinary rolls/stock/gear identical before late replacement");CollectionAssert.AreEqual(beforePositions,current.Positions,"all ordinary coordinates identical before late replacement");
            if(!after.GetReadOnlyEntities().Any(e=>e.GetProperty(SpreadWayhouseBuilder.RoleKey)=="anchor"))
            {
                var refused=sourceRecords.Where(e=>e.Kind=="SpreadWayhouseRefused").Select(e=>JsonUtility.FromJson<Refusal>(e.PayloadJson)).Single(e=>e.zone==id);Assert.IsNotEmpty(refused.reason,"fresh actual source refusal reason");
                CollectionAssert.AreEquivalent(current.Owners,after.GetReadOnlyEntities(),"refused site preserves all original references");
                for(int i=0;i<current.Owners.Length;i++){Assert.AreEqual(current.Positions[i],after.GetEntityPosition(current.Owners[i]));Assert.AreEqual(current.Facts[i],Exact(current.Owners[i]));}
                Assert.AreEqual(totalBefore,Total(after));var location=WorldMap.FromZoneID(id);var startCell=WorldMap.FromZoneID(ReferenceGladePlan.ZoneID);var sillCell=WorldMap.FromZoneID(WorldMap.StartingZoneID);
                var refusal=new Report{installed=false,seed=seed,zone=id,worldX=location.x,worldY=location.y,mapDistanceFromSill=Math.Max(Math.Abs(location.x-sillCell.x),Math.Abs(location.y-sillCell.y)),mapDistanceFromDefaultGlade=Math.Max(Math.Abs(location.x-startCell.x),Math.Abs(location.y-startCell.y)),oldHostiles=current.Actors.Length,newHostiles=current.Actors.Length,oldContainers=containerCount,newContainers=after.GetReadOnlyEntities().Count(e=>e.HasPart<ContainerPart>()),ordinaryLootContainersBefore=containerCount,ordinaryLootContainersAfter=containerCount,oldHostileBlueprints=current.Actors.Select(e=>e.BlueprintName).ToArray(),oldHostileIds=current.Actors.Select(e=>e.ID).ToArray(),replacedStock=Array.Empty<ItemRow>(),oldHostileGear=Rows(current.Actors.SelectMany(DensityLootTestScope.Gear)),newGuardGear=Array.Empty<ItemRow>(),totalStockGearBefore=totalBefore,totalStockGearAfter=Total(after),intentionalStockGearDelta=0,refusalReason=refused.reason,generationReceipts=generationReceipts,boundary="Actual full-manager cold optional source refusal; no reward installation, player acquisition or economy progression is established."};
                Write(refusal);return;
            }
            Assert.That(current.Actors.Length,Is.InRange(1,2));Assert.Greater(current.Containers.Length,0);var replaced=current.Containers[0];var expectedRemoved=new HashSet<Entity>(current.Actors.Concat(new[]{replaced}));
            var now=after.GetReadOnlyEntities().ToArray();var removed=current.Owners.Where(e=>after.GetEntityCell(e)==null).ToArray();
            foreach(var e in expectedRemoved)Assert.Contains(e,removed);foreach(var e in removed.Where(e=>!expectedRemoved.Contains(e)))Assert.True(e.BlueprintName=="Bush"&&!e.HasPart<HarvestablePart>(),"only planned nonharvested scrub can be cleared");
            for(int i=0;i<current.Owners.Length;i++)if(!removed.Contains(current.Owners[i])){var e=current.Owners[i];Assert.AreEqual(current.Positions[i],after.GetEntityPosition(e),"unrelated source coordinates");Assert.AreEqual(current.Facts[i],Exact(e),"unrelated actual owner stock/gear/state");}
            Entity Role(string role)=>now.Single(e=>e.GetProperty(SpreadWayhouseBuilder.RoleKey)==role);
            var cache=Role("cache");var sack=Role("key-sack");var guard=Role("guard");var door=Role("door");Assert.AreEqual("MarlbackScrabbler",guard.BlueprintName);Assert.AreEqual(15,guard.GetStatValue("Hitpoints"));
            Assert.AreEqual(1,cache.GetPart<ContainerPart>().Contents.Count);var reward=cache.GetPart<ContainerPart>().Contents.Single();Assert.AreEqual("Buckler",reward.BlueprintName);Assert.AreEqual(1,Units(reward));Assert.AreEqual(20,Value(reward));Assert.AreSame(cache,reward.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(1,sack.GetPart<ContainerPart>().Contents.Count);var key=sack.GetPart<ContainerPart>().Contents.Single();Assert.AreEqual("IronKey",key.BlueprintName);Assert.True(key.HasTag("NoTrade"));Assert.AreEqual(0,Value(key));Assert.AreSame(sack,key.GetPart<PhysicsPart>().InInventory);Assert.AreEqual(key.GetPart<KeyPart>().KeyId,door.GetPart<LockPart>().KeyId);Assert.IsNotEmpty(key.GetPart<KeyPart>().KeyId);
            Assert.AreEqual(containerCount+1,now.Count(e=>e.HasPart<ContainerPart>()),"one ordinary container replaced plus one key-only sack");Assert.AreEqual(creatureCount-current.Actors.Length+1,now.Count(e=>e.HasTag("Creature")),"one new ordinary guard replaces rolled group");
            var oldStock=replaced.GetPart<ContainerPart>().Contents.ToArray();var oldGear=current.Actors.SelectMany(DensityLootTestScope.Gear).Distinct().ToArray();var newGear=DensityLootTestScope.Gear(guard).ToArray();
            int expectedDelta=20+newGear.Sum(Value)-oldStock.Sum(Value)-oldGear.Sum(Value);Assert.AreEqual(totalBefore+expectedDelta,Total(after),"explicit finite stock/gear delta, not zero-impact claim");
            var at=WorldMap.FromZoneID(id);var sill=WorldMap.FromZoneID(WorldMap.StartingZoneID);var start=WorldMap.FromZoneID(ReferenceGladePlan.ZoneID);
            var report=new Report{installed=true,generationReceipts=generationReceipts,seed=seed,zone=id,worldX=at.x,worldY=at.y,mapDistanceFromSill=Math.Max(Math.Abs(at.x-sill.x),Math.Abs(at.y-sill.y)),mapDistanceFromDefaultGlade=Math.Max(Math.Abs(at.x-start.x),Math.Abs(at.y-start.y)),oldHostiles=current.Actors.Length,newHostiles=1,oldContainers=containerCount,newContainers=now.Count(e=>e.HasPart<ContainerPart>()),ordinaryLootContainersBefore=containerCount,ordinaryLootContainersAfter=containerCount,removedBushes=removed.Length-expectedRemoved.Count,oldHostileBlueprints=current.Actors.Select(e=>e.BlueprintName).ToArray(),oldHostileIds=current.Actors.Select(e=>e.ID).ToArray(),rewardId=reward.ID,keyId=key.ID,replacedStock=Rows(oldStock),oldHostileGear=Rows(oldGear),newGuardGear=Rows(newGear),oldCacheStockValue=oldStock.Sum(Value),oldHostileGearValue=oldGear.Sum(Value),newGuardGearValue=newGear.Sum(Value),rewardValue=20,totalStockGearBefore=totalBefore,totalStockGearAfter=Total(after),intentionalStockGearDelta=expectedDelta,boundary="Actual isolated full manager cold GetZone, paired no-wayhouse-builder baseline with same frozen selection and reset global source RNG. Records generated stock/gear commerce, not sell price, player income, natural acquisition, death drops or campaign balance. Added ordinary guard may consume extra loadout RNG; no unchanged-RNG claim."};
            Write(report);
        }
    }
}
