using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class DensityLairRewardBudgetTests
    {
        DensityLootTestScope scope;
        [SetUp] public void Setup(){scope=new DensityLootTestScope();}
        [TearDown] public void Cleanup(){scope.Dispose();}
        [TestCase(BiomeType.Spread,1,120,175,.90)]
        [TestCase(BiomeType.Spread,2,120,185,.80)]
        [TestCase(BiomeType.Spread,3,140,205,.80)]
        [TestCase(BiomeType.Sodden,1,75,115,.80)]
        [TestCase(BiomeType.Sodden,2,80,125,.80)]
        [TestCase(BiomeType.Sodden,3,105,150,.80)]
        [TestCase(BiomeType.Beating,1,120,185,.85)]
        [TestCase(BiomeType.Beating,2,120,185,.85)]
        [TestCase(BiomeType.Beating,3,115,190,.80)]
        [TestCase(BiomeType.Grovelands,1,20,38,0)]
        [TestCase(BiomeType.Grovelands,2,28,48,0)]
        [TestCase(BiomeType.Grovelands,3,22,43,0)]
        public void BoundedWholeStackRetainsReviewedTierBiomeDistribution(BiomeType biome,int tier,double minimum,double maximum,double equipmentRate)
        {
            double value=0;int useful=0;
            for(int seed=0;seed<256;seed++)
            {
                var rng=new Random(9187+seed*71+tier*13);
                int earlier=tier<3?1:2;
                var approaches=new List<Entity>();
                for(int depth=0;depth<earlier;depth++)approaches.AddRange(LairRewardBudget.CreateApproach(biome,tier,depth,scope.Factory,rng).GetPart<ContainerPart>().Contents);
                var owner=LairRewardBudget.Create(biome,tier,scope.Factory,rng,earlier);
                Assert.NotNull(owner);var items=owner.GetPart<ContainerPart>().Contents;
                Assert.That(items.Count,Is.InRange(1,32));
                var loose=LairRewardBudget.CreateLooseFind(biome,tier,scope.Factory,rng);
                value+=approaches.Sum(TradeSystem.GetItemValue)+items.Sum(TradeSystem.GetItemValue)+(loose==null?0:TradeSystem.GetItemValue(loose));
                if(items.Any(IsEquipment)||approaches.Any(IsEquipment)||loose!=null&&IsEquipment(loose))useful++;
                if(biome==BiomeType.Grovelands)Assert.False(items.Any(IsEquipment),"Choir reward remains natural");
            }
            Assert.That(value/256,Is.InRange(minimum,maximum),"full stock value at fixed reviewed cohort");
            Assert.GreaterOrEqual(useful/256.0,equipmentRate,"actual meaningful equipment incidence");
        }
        [TestCase(BiomeType.Spread)] [TestCase(BiomeType.Sodden)] [TestCase(BiomeType.Beating)] [TestCase(BiomeType.Grovelands)]
        public void EveryStockEntryBelongsToActualSelectedContainerTierPool(BiomeType biome)
        {
            for(int tier=1;tier<=3;tier++)for(int seed=0;seed<32;seed++)
            {
                var owner=LairRewardBudget.Create(biome,tier,scope.Factory,new Random(seed));
                var kind=ContainerPlacementService.PoolFor(biome,ContainerPlacementService.ZoneKind.Lair).Single(k=>k.Blueprint==owner.BlueprintName);
                var allowed=new HashSet<string>{"GoldCoin"};Gather(kind.TablePrefix+tier,allowed);
                foreach(var item in owner.GetPart<ContainerPart>().Contents)Assert.That(allowed.Contains(item.BlueprintName),Is.True,item.BlueprintName+" in "+owner.BlueprintName);
            }
        }
        [Test]
        public void OtherContainersKeepAuthoredCapacityAndHigherTiersClamp()
        {
            int original=scope.Factory.CreateEntity("Crate").GetPart<ContainerPart>().MaxItems;
            LairRewardBudget.Create(BiomeType.Spread,3,scope.Factory,new Random(9));
            Assert.AreEqual(original,scope.Factory.CreateEntity("Crate").GetPart<ContainerPart>().MaxItems);
            var a=LairRewardBudget.Create(BiomeType.Beating,3,scope.Factory,new Random(71));
            var b=LairRewardBudget.Create(BiomeType.Beating,8,scope.Factory,new Random(71));
            Assert.AreEqual(Signature(a),Signature(b));
        }
        [TestCase(BiomeType.Spread)] [TestCase(BiomeType.Beating)] [TestCase(BiomeType.Grovelands)]
        public void NonSoddenLooseControlConsumesNoRng(BiomeType biome)
        {
            var a=new Random(31);var b=new Random(31);
            Assert.Null(LairRewardBudget.CreateLooseFind(biome,2,scope.Factory,a));
            Assert.AreEqual(b.Next(),a.Next());
        }
        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void OptionalLooseFindUsesOnlyItsTierAndHasRealRefusals(int tier)
        {
            var allowed=new HashSet<string>();Gather("FindWeaponT"+tier,allowed);Gather("FindArmorT"+tier,allowed);
            int yes=0,no=0;
            for(int seed=0;seed<64;seed++)
            {
                var item=LairRewardBudget.CreateLooseFind(BiomeType.Sodden,tier,scope.Factory,new Random(seed));
                if(item==null){no++;continue;}yes++;
                Assert.True(allowed.Contains(item.BlueprintName));Assert.True(IsEquipment(item));
            }
            Assert.Greater(yes,0);Assert.Greater(no,0);
        }
        [TestCase(BiomeType.Spread)] [TestCase(BiomeType.Sodden)] [TestCase(BiomeType.Beating)] [TestCase(BiomeType.Grovelands)]
        public void EarlierStockKeepsItsOwnFamilyAndProgressiveTier(BiomeType biome)
        {
            for(int depth=0;depth<=1;depth++)for(int seed=0;seed<32;seed++)
            {
                var owner=LairRewardBudget.CreateApproach(biome,3,depth,scope.Factory,new Random(seed));
                var kind=ContainerPlacementService.PoolFor(biome,ContainerPlacementService.ZoneKind.Lair).Single(k=>k.Blueprint==owner.BlueprintName);
                var allowed=new HashSet<string>{"GoldCoin"};Gather(kind.TablePrefix+(depth+1),allowed);
                foreach(var item in owner.GetPart<ContainerPart>().Contents)Assert.True(allowed.Contains(item.BlueprintName));
                Assert.AreEqual(scope.Factory.CreateEntity(owner.BlueprintName).GetPart<ContainerPart>().MaxItems,owner.GetPart<ContainerPart>().MaxItems);
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void RewardDiagnosticsFollowChannelWithoutChangingStock(bool enabled)
        {
            bool previous=Diag.IsChannelEnabled("worldgen");string cause=Guid.NewGuid().ToString("N");
            try
            {
                Diag.SetChannel("worldgen",enabled);
                using(Diag.WithCause(cause))
                {
                    var a=LairRewardBudget.Create(BiomeType.Spread,2,scope.Factory,new Random(11));
                    var b=LairRewardBudget.CreateApproach(BiomeType.Spread,2,0,scope.Factory,new Random(11));
                    Assert.NotNull(a);Assert.NotNull(b);
                    LairRewardBudget.CreateLooseFind(BiomeType.Sodden,2,scope.Factory,new Random(11));
                    Assert.Null(LairRewardBudget.Create(BiomeType.Cave,2,scope.Factory,new Random(11)));
                }
                foreach(string kind in new[]{"LairRewardStocked","LairApproachStocked","LairLooseRewardRoll","LairRewardRejected"})
                    Assert.AreEqual(enabled?1:0,DiagQuery.Apply(new DiagQuery.Filter{Kind=kind,CauseTraceId=cause,Limit=100}).Records.Count);
            }
            finally{Diag.SetChannel("worldgen",previous);}
        }
        static bool IsEquipment(Entity item)=>item.HasPart<MeleeWeaponPart>()||item.HasPart<ArmorPart>()||item.HasPart<GrimoirePart>();
        static string Signature(Entity owner)=>owner.BlueprintName+"|"+string.Join("|",owner.GetPart<ContainerPart>().Contents.Select(e=>e.BlueprintName+":"+(e.GetPart<StackerPart>()?.StackCount??1)));
        static void Gather(string table,HashSet<string> result)
        {foreach(var entry in LootTableRegistry.Get(table).Entries){if(!string.IsNullOrEmpty(entry.Blueprint))result.Add(entry.Blueprint);if(!string.IsNullOrEmpty(entry.TableRef))Gather(entry.TableRef,result);}}
    }
}
