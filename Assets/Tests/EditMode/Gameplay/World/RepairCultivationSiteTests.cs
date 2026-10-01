using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Player-loss and access checks for the actual finite near-town
    /// allotment. Controlled grass fields isolate preservation; native seeds pin
    /// the real east-center arrival route without inventing content owners.</summary>
    public sealed class RepairCultivationSiteTests
    {
        HaulingContentScope scope;
        EntityFactory oldCropFactory,oldHarvestFactory;
        Zone oldActive;
        [SetUp] public void Setup()
        {
            scope=new HaulingContentScope();scope.Seed(64);
            oldCropFactory=CropSystem.Factory;oldHarvestFactory=HarvestablePart.Factory;oldActive=SettlementRuntime.ActiveZone;
            CropSystem.Factory=scope.Factory;HarvestablePart.Factory=scope.Factory;
            RepairRecipeRegistry.ResetForTests();
        }
        [TearDown] public void Cleanup()
        {
            CropSystem.Factory=oldCropFactory;HarvestablePart.Factory=oldHarvestFactory;SettlementRuntime.ActiveZone=oldActive;
            RepairRecipeRegistry.ResetForTests();scope?.Dispose();
        }
        Zone Plain(string id=RepairCultivationSite.ZoneID)
        {
            var zone=new Zone(id);
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)Assert.True(zone.AddEntity(scope.Factory.CreateEntity("Grass"),x,y));
            return zone;
        }
        static Dictionary<Entity,(string id,int x,int y)> Snapshot(Zone zone)=>zone.GetReadOnlyEntities().ToDictionary(e=>e,e=>
        {var p=zone.GetEntityPosition(e);return(e.ID,p.x,p.y);});
        static void OriginalsRemain(Zone zone,Dictionary<Entity,(string id,int x,int y)> before)
        {
            foreach(var pair in before)
            {
                Assert.NotNull(zone.GetEntityCell(pair.Key),pair.Value.id);
                Assert.AreEqual(pair.Value.id,pair.Key.ID);Assert.AreEqual((pair.Value.x,pair.Value.y),zone.GetEntityPosition(pair.Key));
            }
        }
        Entity Role(Zone zone,string role)=>zone.GetReadOnlyEntities().Single(e=>e.GetProperty(RepairCultivationSite.RoleKey)==role);
        static HashSet<(int,int)> Flood(Zone zone)
        {
            var seen=new HashSet<(int,int)>();var queue=new Queue<(int x,int y)>();var start=(Zone.Width-1,Zone.Height/2);
            Assert.False(zone.GetCell(start.Item1,start.Item2).BlocksMovement(),"Actual Morrowfast-side arrival is open.");seen.Add(start);queue.Enqueue(start);
            var dirs=new[]{(1,0),(-1,0),(0,1),(0,-1)};
            while(queue.Count>0)
            {
                var at=queue.Dequeue();foreach(var d in dirs)
                {var next=(at.x+d.Item1,at.y+d.Item2);var cell=zone.GetCell(next.Item1,next.Item2);if(cell!=null&&!cell.BlocksMovement()&&seen.Add(next))queue.Enqueue(next);}
            }
            return seen;
        }
        static (int x,int y) Frontage(Zone zone,Entity target)
        {
            var at=zone.GetEntityPosition(target);
            foreach(var d in new[]{(1,0),(-1,0),(0,1),(0,-1)})
            {var next=(at.x+d.Item1,at.y+d.Item2);if(zone.GetCell(next.Item1,next.Item2)?.BlocksMovement()==false)return next;}
            Assert.Fail("No open frontage for "+target.BlueprintName);return(-1,-1);
        }
        Entity PlayerBeside(Zone zone,Entity target)
        {
            var player=scope.Factory.CreateEntity("Player");var at=Frontage(zone,target);Assert.True(zone.AddEntity(player,at.x,at.y));return player;
        }
        static void MoveBeside(Zone zone,Entity player,Entity target)
        {var at=Frontage(zone,target);Assert.True(zone.MoveEntity(player,at.x,at.y));}
        [Test] public void WrongZoneCannotAcquireOrModifyAnAllotment()
        {
            var zone=Plain("Overworld.1.6.0");var before=Snapshot(zone);Assert.False(RepairCultivationSite.TryInstall(zone,scope.Factory));
            OriginalsRemain(zone,before);Assert.AreEqual(before.Count,zone.EntityCount);Assert.False(zone.GetReadOnlyEntities().Any(e=>e.HasPart<CultivatedSoilPart>()));
        }
        [TestCase("RepairLinedWell")][TestCase("KnotflaxCrop")]
        public void MissingDependencyLeavesExactOwnersAndSoilUnchanged(string missing)
        {
            var zone=Plain();var before=Snapshot(zone);var blueprint=scope.Factory.Blueprints[missing];scope.Factory.Blueprints.Remove(missing);
            try{Assert.False(RepairCultivationSite.TryInstall(zone,scope.Factory));OriginalsRemain(zone,before);Assert.AreEqual(before.Count,zone.EntityCount);Assert.False(zone.GetReadOnlyEntities().Any(e=>e.HasPart<CultivatedSoilPart>()));}
            finally{scope.Factory.Blueprints.Add(missing,blueprint);}
            Assert.True(RepairCultivationSite.TryInstall(zone,scope.Factory));OriginalsRemain(zone,before);
        }
        [Test] public void PlacementPreservesPriorOwnersAndRepeatCannotRefillOrMoveAnything()
        {
            var zone=Plain();var oldItem=scope.Factory.CreateEntity("FireClay");Assert.True(zone.AddEntity(oldItem,63,11));
            var before=Snapshot(zone);Assert.True(RepairCultivationSite.TryInstall(zone,scope.Factory));OriginalsRemain(zone,before);
            Assert.AreEqual(13,zone.GetReadOnlyEntities().Count(e=>e.GetProperty(RepairCultivationSite.RoleKey)!=null));
            Assert.AreEqual(6,zone.GetReadOnlyEntities().Count(e=>e.HasPart<CultivatedSoilPart>()));
            var installed=Snapshot(zone);Assert.False(RepairCultivationSite.TryInstall(zone,scope.Factory));OriginalsRemain(zone,installed);Assert.AreEqual(installed.Count,zone.EntityCount);
        }
        [TestCase(64)][TestCase(1729)]
        public void ActualEastCenterArrivalReachesEveryGeneratedActionFrontage(int seed)
        {
            scope.Seed(seed);var manager=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);var zone=manager.GetZone(RepairCultivationSite.ZoneID);var reached=Flood(zone);
            var owners=zone.GetReadOnlyEntities().Where(e=>e.GetProperty(RepairCultivationSite.RoleKey)!=null).ToArray();Assert.AreEqual(13,owners.Length);
            foreach(var owner in owners)
            {
                var at=zone.GetEntityPosition(owner);
                Assert.True(reached.Any(p=>Math.Abs(p.Item1-at.x)<=1&&Math.Abs(p.Item2-at.y)<=1),owner.GetProperty(RepairCultivationSite.RoleKey)+" has no reachable interaction frontage.");
            }
            Assert.AreEqual(1,zone.GetReadOnlyEntities().Count(e=>e.GetIntProperty("MorrowfastDryGoodsCache")==1));
        }
        [Test] public void HarvestedPlotKeepsExactReusableSoilThroughExplicitUnload()
        {
            var zone=Plain();Assert.True(RepairCultivationSite.TryInstall(zone,scope.Factory));var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);manager.SetActiveZone(zone);
            var crop=Role(zone,"knotflax-ripe");var cell=zone.GetEntityCell(crop);var soil=cell.Objects.Single(e=>e.HasPart<CultivatedSoilPart>());var player=PlayerBeside(zone,crop);SettlementRuntime.ActiveZone=zone;
            Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(crop,"HarvestCultivatedCrop"),player,zone).Success);
            Assert.IsNull(zone.GetEntityCell(crop));Assert.True(CultivatedSoilPart.IsCultivated(zone,cell));Assert.AreSame(soil,cell.Objects.Single(e=>e.HasPart<CultivatedSoilPart>()));
            var owners=Snapshot(zone);manager.UnloadZone(zone.ZoneID);Assert.AreSame(zone,manager.GetZone(zone.ZoneID));OriginalsRemain(zone,owners);Assert.AreEqual(owners.Count,zone.EntityCount);Assert.False(RepairCultivationSite.TryInstall(zone,scope.Factory));
        }
        [Test] public void NativeSaveRetainsRepairedWellDepletedSourceAndHarvestedBed()
        {
            using(var isolation=new HotbarSaveFixture(false,false))
            {
                var zone=Plain();Assert.True(RepairCultivationSite.TryInstall(zone,scope.Factory));var manager=OverworldZoneManager.CreateDetached(scope.Factory,64,true);manager.SetActiveZone(zone);
                var bank=Role(zone,"clay");var player=PlayerBeside(zone,bank);SettlementRuntime.ActiveZone=zone;HarvestablePart.Factory=scope.Factory;CropSystem.Factory=scope.Factory;
                Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(bank,"Harvest"),player,zone).Success);Assert.IsNull(zone.GetEntityCell(bank));
                var well=Role(zone,"lining");MoveBeside(zone,player,well);Assert.True(well.GetPart<RepairablePart>().TryRepair(player,zone));
                var crop=Role(zone,"knotflax-ripe");var at=zone.GetEntityPosition(crop);var soil=zone.GetCell(at.x,at.y).Objects.Single(e=>e.HasPart<CultivatedSoilPart>());MoveBeside(zone,player,crop);
                Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(crop,"HarvestCultivatedCrop"),player,zone).Success);
                var loaded=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("repair-cultivation","world-save",manager,null,player));var restored=loaded.ZoneManager.ActiveZone;
                var restoredWell=restored.GetReadOnlyEntities().Single(e=>e.ID==well.ID);Assert.True(restoredWell.GetPart<RepairablePart>().Repaired);Assert.True(restoredWell.GetPart<WellPart>().IsUsable);
                Assert.False(restored.GetReadOnlyEntities().Any(e=>e.ID==bank.ID||e.ID==crop.ID));
                var savedSoil=restored.GetReadOnlyEntities().Single(e=>e.ID==soil.ID);Assert.True(CultivatedSoilPart.IsCultivated(restored,restored.GetEntityCell(savedSoil)));
                Assert.AreEqual(2,restored.GetCell(at.x,at.y).Objects.Count(e=>e.BlueprintName=="KnotflaxCord"));Assert.AreEqual(1,restored.GetCell(at.x,at.y).Objects.Count(e=>e.BlueprintName=="KnotflaxSeed"));
                var savedOwners=Snapshot(restored);loaded.ZoneManager.UnloadZone(restored.ZoneID);Assert.AreSame(restored,loaded.ZoneManager.GetZone(restored.ZoneID));OriginalsRemain(restored,savedOwners);Assert.AreEqual(savedOwners.Count,restored.EntityCount);
                Assert.False(RepairCultivationSite.TryInstall(restored,scope.Factory));
            }
        }
    }
}
