using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class DensityLairPacingTests
    {
        const string Surface="Overworld.4.9.0";
        DensityLootTestScope scope;
        [SetUp] public void Setup(){scope=new DensityLootTestScope();}
        [TearDown] public void Cleanup(){ForeignContainer.Destination=null;ForeignItem.Destination=null;CacheCapture.Owner=null;MoveCacheFromStock.Destination=null;MoveCacheFromStock.Mode=null;scope.Dispose();}
        OverworldZoneManager Manager(BiomeType biome,int tier)
        {var m=OverworldZoneManager.CreateDetached(scope.Factory,64);m.WorldMap.Tiles[4,9]=biome;m.WorldMap.SetPOI(4,9,new PointOfInterest(POIType.Lair,"pacing probe",null,tier,"MarlbackWallkeeper"));return m;}
        [TestCase(BiomeType.Spread,1)] [TestCase(BiomeType.Spread,2)] [TestCase(BiomeType.Spread,3)]
        [TestCase(BiomeType.Sodden,1)] [TestCase(BiomeType.Sodden,2)] [TestCase(BiomeType.Sodden,3)]
        [TestCase(BiomeType.Beating,1)] [TestCase(BiomeType.Beating,2)] [TestCase(BiomeType.Beating,3)]
        [TestCase(BiomeType.Grovelands,1)] [TestCase(BiomeType.Grovelands,2)] [TestCase(BiomeType.Grovelands,3)]
        public void EveryFloorHasOneRealOffRouteCacheAndOnlyFinalClaimOwnsBoss(BiomeType biome,int tier)
        {
            var m=Manager(biome,tier);int final=tier<3?1:2;int owners=0;
            for(int depth=0;depth<=final;depth++)
            {
                var z=m.GetZone(WorldMap.ToZoneID(4,9,depth));Assert.NotNull(z);
                var caches=z.GetReadOnlyEntities().Where(e=>e.HasPart<ContainerPart>()&&!e.HasPart<AIAmbushPart>()).ToArray();
                Assert.AreEqual(1,caches.Length,"earlier rooms must contain a find, not only a final-floor hoard");owners++;
                var cache=caches[0];Assert.Greater(cache.GetPart<ContainerPart>().Contents.Count,0);
                foreach(var c in z.GetOccupiedCells(cache))Assert.False(z.GenReservedCells.Contains((c.X,c.Y)));
                var plan=LairStacks.Inspect(m,Surface);
                if(depth<final){Assert.Null(plan.RewardID);Assert.Null(plan.BossID);Assert.AreEqual(Surface,cache.GetProperty("LairApproachCacheSurface"));Assert.LessOrEqual(cache.GetPart<ContainerPart>().MaxItems,8);}
                else Assert.AreEqual(cache.ID,plan.RewardID);
            }
            Assert.AreEqual(final+1,owners);
        }
        [TestCase(false)] [TestCase(true)]
        public void EmptyOrRemovedApproachCachePersistsAcrossUnloadAndSave(bool removed)
        {
            var m=Manager(BiomeType.Spread,3);var z=m.GetZone(Surface);
            var cache=z.GetReadOnlyEntities().SingleOrDefault(e=>e.GetProperty("LairApproachCacheSurface")==Surface);
            Assert.NotNull(cache,"actual approach owner");string id=cache.ID;cache.GetPart<ContainerPart>().Contents.Clear();if(removed)z.RemoveEntity(cache);
            var p=scope.Factory.CreateEntity("Player");Assert.True(z.AddEntity(p,44,12));m.SetActiveZone(z);
            var old=TurnManager.Active;
            try
            {
                var turns=new TurnManager();turns.RestoreSavedState(71,true,p,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=p,Energy=1000}});
                var state=GameSessionState.Capture("lair-pacing-test","v",m,turns,p);
                using(var bytes=new MemoryStream())
                {
                    state.Save(new SaveWriter(bytes));bytes.Position=0;var loaded=GameSessionState.Load(new SaveReader(bytes,scope.Factory));
                    loaded.ZoneManager.UnloadZone(Surface);var saved=loaded.ZoneManager.GetZone(Surface);
                    var restored=saved.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==id);
                    if(removed)Assert.Null(restored);else{Assert.NotNull(restored);Assert.Zero(restored.GetPart<ContainerPart>().Contents.Count);}
                    Assert.Null(LairStacks.Inspect(loaded.ZoneManager,Surface).RewardID,"early cache does not claim final reward");
                }
            }
            finally{typeof(TurnManager).GetProperty("Active").SetValue(null,old);}
        }
        public sealed class ForeignContainer:ContainerPart
        {
            public static Zone Destination;
            public int InitialCapacity;
            public override bool HandleEvent(GameEvent e)
            {if(e.ID=="ObjectCreated"){InitialCapacity=MaxItems;Destination.AddEntity(ParentEntity,1,1);}return base.HandleEvent(e);}
        }
        public sealed class ForeignItem:PhysicsPart
        {
            public static Zone Destination;
            public override bool HandleEvent(GameEvent e)
            {if(e.ID=="ObjectCreated"&&ParentEntity.GetPart<ContainerPart>()==null)Destination.AddEntity(ParentEntity,1,1);return base.HandleEvent(e);}
        }
        [TestCase(false)] [TestCase(true)]
        public void ForeignFactoryCacheIsRefusedWithoutChangingItsStockOrCapacity(bool approach)
        {
            ForeignContainer.Destination=new Zone("foreign-cache");scope.Factory.RegisterPartType<ForeignContainer>("Container");
            Assert.Null(approach?LairRewardBudget.CreateApproach(BiomeType.Spread,2,0,scope.Factory,new Random(7)):LairRewardBudget.Create(BiomeType.Spread,2,scope.Factory,new Random(7)));
            Assert.Greater(ForeignContainer.Destination.GetReadOnlyEntities().Count,0);
            foreach(var owner in ForeignContainer.Destination.GetReadOnlyEntities())
            {var cache=owner.GetPart<ContainerPart>();Assert.Zero(cache.Contents.Count);Assert.AreEqual(((ForeignContainer)cache).InitialCapacity,cache.MaxItems);}
        }
        [TestCase(false)] [TestCase(true)]
        public void ForeignFactoryItemsCannotBeDuplicatedInsideAReward(bool approach)
        {
            ForeignItem.Destination=new Zone("foreign-items");scope.Factory.RegisterPartType<ForeignItem>("Physics");
            Assert.Null(approach?LairRewardBudget.CreateApproach(BiomeType.Spread,2,0,scope.Factory,new Random(7)):LairRewardBudget.Create(BiomeType.Spread,2,scope.Factory,new Random(7)));
            Assert.Greater(ForeignItem.Destination.GetReadOnlyEntities().Count,0);
            foreach(var item in ForeignItem.Destination.GetReadOnlyEntities())Assert.Null(item.GetPart<PhysicsPart>().InInventory);
        }

        public sealed class CacheCapture : ContainerPart
        {
            public static Entity Owner;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "ObjectCreated") Owner = ParentEntity;
                return base.HandleEvent(e);
            }
        }
        public sealed class MoveCacheFromStock : PhysicsPart
        {
            public static Zone Destination;
            public static string Mode;
            public static int Callbacks;
            public static ContainerPart CapturedPart;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "ObjectCreated" && ParentEntity.GetPart<ContainerPart>() == null && CacheCapture.Owner != null)
                {
                    Callbacks++;
                    var owner = CacheCapture.Owner;
                    CapturedPart = owner.GetPart<ContainerPart>();
                    if (Mode == "zone") Destination.AddEntity(owner, 1, 1);
                    else if (Mode == "inventory") owner.GetPart<PhysicsPart>().InInventory = new Entity { ID = "foreign-purse" };
                    else if (Mode == "part") owner.RemovePart(CapturedPart);
                }
                return base.HandleEvent(e);
            }
        }
        [TestCase(false, false, "zone")] [TestCase(false, true, "zone")]
        [TestCase(true, false, "zone")] [TestCase(true, true, "zone")]
        [TestCase(false, false, "inventory")] [TestCase(false, true, "inventory")]
        [TestCase(true, false, "inventory")] [TestCase(true, true, "inventory")]
        [TestCase(false, false, "part")] [TestCase(false, true, "part")]
        [TestCase(true, false, "part")] [TestCase(true, true, "part")]
        [TestCase(false, false, "unchanged")] [TestCase(false, true, "unchanged")]
        [TestCase(true, false, "unchanged")] [TestCase(true, true, "unchanged")]
        public void StockCreationCallbackCannotChangeTheCacheOwnerOrPart(bool approach, bool fallback, string mutation)
        {
            MoveCacheFromStock.Mode = mutation;
            MoveCacheFromStock.Destination = new Zone("callback-cache");
            MoveCacheFromStock.Callbacks = 0;
            MoveCacheFromStock.CapturedPart = null;
            scope.Factory.RegisterPartType<CacheCapture>("Container");
            scope.Factory.RegisterPartType<MoveCacheFromStock>("Physics");
            var tables = ContainerPlacementService.PoolFor(BiomeType.Spread, ContainerPlacementService.ZoneKind.Lair)
                .SelectMany(kind => new[] { kind.TablePrefix + "1", kind.TablePrefix + "2" }).Distinct();
            string entries = fallback ? "[]" : "[{\"Blueprint\":\"Dagger\",\"Chance\":100}]";
            LootTableRegistry.Initialize("{\"Tables\":[" + string.Join(",", tables.Select(name =>
                "{\"Name\":\"" + name + "\",\"Entries\":" + entries + "}")) + "]}");
            var result = approach
                ? LairRewardBudget.CreateApproach(BiomeType.Spread, 2, 0, scope.Factory, new Random(7))
                : LairRewardBudget.Create(BiomeType.Spread, 2, scope.Factory, new Random(7));
            Assert.Greater(MoveCacheFromStock.Callbacks, 0, "real item-creation callback must execute");
            Assert.NotNull(CacheCapture.Owner);
            Assert.NotNull(MoveCacheFromStock.CapturedPart);
            if (mutation == "unchanged")
            {
                Assert.AreSame(CacheCapture.Owner, result);
                Assert.Greater(result.GetPart<ContainerPart>().Contents.Count, 0);
            }
            else
            {
                Assert.IsNull(result, "refuse the changed owner before stocking it");
                Assert.Zero(MoveCacheFromStock.CapturedPart.Contents.Count, "do not mutate stock after losing authority");
                if (mutation == "zone") Assert.NotNull(MoveCacheFromStock.Destination.GetEntityCell(CacheCapture.Owner));
                else if (mutation == "inventory") Assert.AreEqual("foreign-purse", CacheCapture.Owner.GetPart<PhysicsPart>().InInventory.ID);
                else Assert.IsNull(CacheCapture.Owner.GetPart<ContainerPart>());
            }
        }
    }
}
