using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
using Random=System.Random;

namespace CavesOfOoo.Tests
{
    public class SoddenDistrictGenerationTests
    {
        internal static EntityFactory Factory()
        {
            var factory=new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            return factory;
        }
        internal static readonly string[] Sites={"Overworld.15.7.0","Overworld.16.7.0","Overworld.17.7.0"};

        [Test] public void ThreeSoddenDestinationsFormAnExactChainBelowSumphold()
        {
            CollectionAssert.AreEqual(Sites,new[]{SoddenDistrictPlan.StopZoneID,SoddenDistrictPlan.CrossingZoneID,SoddenDistrictPlan.WorksZoneID});
            for(int i=0;i<Sites.Length;i++)
            {
                Assert.IsTrue(SoddenDistrictPlan.IsSupportedZone(Sites[i]));var at=WorldMap.FromZoneID(Sites[i]);
                Assert.AreEqual(BiomeType.Sodden,WorldMapAuthoring.BiomeAt(at.x,at.y));Assert.AreEqual(2,WorldMapAuthoring.TierAt(at.x,at.y));
                for(int seed=1;seed<=20;seed++)Assert.IsNull(WorldGenerator.Generate(seed).GetPOI(at.x,at.y),Sites[i]);
            }
            Assert.AreEqual(BiomeType.Spread,WorldMapAuthoring.BiomeAt(15,6));
            Assert.IsFalse(SoddenDistrictPlan.IsSupportedZone("Overworld.16.6.0"),"Existing oil shore is preserved.");
        }
        [TestCase(null)] [TestCase("")] [TestCase("Overworld.15.07.0")] [TestCase("Overworld.15.7.1")]
        [TestCase("Overworld.17.5.0")] [TestCase("Overworld.15.6.0")] [TestCase("Overworld.18.7.0")]
        public void UnsupportedAddressesNeverAcquireDistrictLayouts(string id)
        {Assert.IsFalse(SoddenDistrictPlan.IsSupportedZone(id));Assert.Throws<ArgumentException>(()=>SoddenDistrictPlan.Create(id,64));}

        [TestCase(1)] [TestCase(64)] [TestCase(1729)]
        public void LayoutsHaveDistinctSeedStableSignaturesAndRealWetTerrain(int seed)
        {
            var signatures=new List<string>();
            foreach(string id in Sites)
            {
                var p=SoddenDistrictPlan.Create(id,seed);signatures.Add(p.Signature());
                Assert.AreEqual(p.Signature(),SoddenDistrictPlan.Create(id,seed).Signature());
                Assert.AreNotEqual(p.Signature(),SoddenDistrictPlan.Create(id,seed+1).Signature());
                int wet=0,approach=0;
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                {
                    if(p.IsWet(x,y)){wet++;Assert.AreEqual("MirePool",p.ObjectAt(x,y));Assert.IsFalse(p.IsApproach(x,y));}
                    if(p.IsApproach(x,y)){approach++;Assert.IsTrue(p.IsReserved(x,y));Assert.IsFalse(p.IsWet(x,y));}
                }
                Assert.Greater(wet,80,id);Assert.Greater(approach,60,id);
            }
            Assert.AreEqual(3,signatures.Distinct().Count());
        }
        [TestCase(1)] [TestCase(64)] [TestCase(1729)]
        public void CrossingOffersAnActuallyShorterWetRouteAndConnectedDryDetour(int seed)
        {
            var plan=SoddenDistrictPlan.Create(SoddenDistrictPlan.CrossingZoneID,seed);
            int wet=Distance(plan,(20,12),(60,12),false),dry=Distance(plan,(20,12),(60,12),true);
            Assert.AreEqual(40,wet,"Unobstructed native wet shortcut.");Assert.GreaterOrEqual(dry,wet+12,"Dry alternative must cost meaningful distance.");
            Assert.Less(dry,85,"Detour remains a local choice.");
            foreach(var exit in new[]{(0,12),(79,12),(40,0),(40,24)})
                Assert.GreaterOrEqual(Distance(plan,(20,12),exit,true),0,"Required dry exit "+exit);
        }
        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void EightWayPlayerMovementStillMakesTheWetShortcutMateriallyShorter(int seed)
        {
            var plan=SoddenDistrictPlan.Create(SoddenDistrictPlan.CrossingZoneID,seed);
            int wet=Distance(plan,(20,12),(60,12),false,true),dry=Distance(plan,(20,12),(60,12),true,true);
            Assert.AreEqual(40,wet);Assert.GreaterOrEqual(dry,wet+12);Assert.Less(dry,85);
        }
        [TestCase(1)] [TestCase(64)] [TestCase(1729)]
        public void NativeOwnersExposeFiniteWorkStockAndAnInhabitedPreparationStop(int seed)
        {
            var factory=Factory();
            foreach(string id in Sites)
            {
                var zone=new Zone(id);var builder=new SoddenDistrictBuilder(seed);
                Assert.IsTrue(builder.BuildZone(zone,factory,new Random(5)),id);Assert.IsTrue(builder.ValidateFinal(zone));
                Assert.AreEqual(Zone.Width*Zone.Height,zone.GetAllEntities().Count(e=>e.HasTag("Terrain")&&(e.BlueprintName=="Grass"||e.BlueprintName=="StoneFloor")));
                foreach(var e in zone.GetAllEntities().Where(e=>e.BlueprintName=="MirePool"))
                {Assert.AreEqual("bog-mire",e.GetPart<LiquidPoolPart>().LiquidId);Assert.IsNotNull(e.GetPart<TileStateSourcePart>());
                    var wetCell=zone.GetEntityCell(e);Assert.IsTrue(zone.TileState.HasCoating(wetCell.X,wetCell.Y,"water"));}
                Assert.AreEqual(1,zone.GetAllEntities().Count(e=>e.BlueprintName=="SoddenRouteNotice"));
                if(id==SoddenDistrictPlan.StopZoneID)
                {
                    Assert.AreEqual(1,zone.GetAllEntities().Count(e=>e.BlueprintName=="SoddenDressingBench"));
                    Assert.AreEqual(1,zone.GetAllEntities().Count(e=>e.BlueprintName=="PeatCutter"));
                    var bench=zone.GetAllEntities().Single(e=>e.BlueprintName=="SoddenDressingBench");
                    var keeper=zone.GetAllEntities().Single(e=>e.BlueprintName=="PeatCutter");
                    Assert.LessOrEqual(SpatialQuery.Distance(zone,bench,keeper),1);
                    Assert.AreEqual(2,zone.GetAllEntities().Count(e=>e.BlueprintName=="SumpsieveCrop"));
                    Assert.GreaterOrEqual(zone.GetAllEntities().Count(e=>e.BlueprintName=="StoneWall"),30);
                }
                else Assert.IsFalse(zone.GetAllEntities().Any(e=>e.BlueprintName=="PeatCutter"||e.BlueprintName=="SoddenDressingBench"));
                if(id==SoddenDistrictPlan.WorksZoneID)
                {
                    var stock=zone.GetAllEntities().Single(e=>e.BlueprintName=="SoddenWorksLocker").GetPart<ContainerPart>();
                    CollectionAssert.AreEquivalent(new[]{"LeatherBoots","Buckler","KnotflaxCord","KnotflaxCord","PeatMalletHeadComponent","GroundwireScreen","OakHaftComponent","LeatherBindingComponent"},stock.Contents.SelectMany(e=>Enumerable.Repeat(e.BlueprintName,e.GetPart<StackerPart>()?.StackCount??1)));
                    Assert.AreEqual(1,zone.GetAllEntities().Count(e=>e.BlueprintName=="SoddenWorksSalvage"));
                    Assert.IsTrue(stock.Contents.All(e=>e.GetPart<PhysicsPart>().InInventory!=null));
                }
                else Assert.IsFalse(zone.GetAllEntities().Any(e=>e.BlueprintName=="SoddenWorksLocker"||e.BlueprintName=="SoddenWorksSalvage"));
            }
        }
        [Test] public void DressingKeeperKeepsItsAssignedStationWhileRetainingSelfPreservation()
        {
            var z=new Zone(SoddenDistrictPlan.StopZoneID);var b=new SoddenDistrictBuilder(64);Assert.IsTrue(b.BuildZone(z,Factory(),new Random(1)));
            var keeper=z.GetReadOnlyEntities().Single(e=>e.BlueprintName=="PeatCutter");var brain=keeper.GetPart<BrainPart>();
            Assert.IsFalse(brain.Wanders);Assert.IsFalse(brain.WandersRandomly);Assert.IsTrue(brain.Staying);Assert.AreEqual((35,6),(brain.StartingCellX,brain.StartingCellY));Assert.IsTrue(keeper.HasPart<AISelfPreservationPart>());Assert.IsFalse(keeper.HasTag("AllowIdleBehavior"));
            var ordinary=Factory().CreateEntity("PeatCutter").GetPart<BrainPart>();
            Assert.IsTrue(ordinary.Wanders);Assert.IsFalse(ordinary.Staying,"Only the bound dressing keeper receives the station assignment.");
        }
        [Test] public void WetShortcutExposesTheNativeBandfrogWhileTheDryBowStaysClear()
        {
            var factory=Factory();var z=new Zone(SoddenDistrictPlan.CrossingZoneID);var builder=new SoddenDistrictBuilder(64);
            Assert.IsTrue(builder.BuildZone(z,factory,new Random(1)));
            var frogs=z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="Bandfrog").ToArray();Assert.AreEqual(1,frogs.Length);
            var cell=z.GetEntityCell(frogs[0]);Assert.AreEqual(40,cell.X);Assert.AreEqual(13,cell.Y);
            Assert.IsTrue(z.GetCell(40,12).Objects.Any(e=>e.BlueprintName=="MirePool"));
            Assert.IsFalse(z.GetReadOnlyEntities().Any(e=>e.BlueprintName=="Reedfrog"));
            Assert.GreaterOrEqual(Math.Abs(cell.Y-3),10,"Raised dry bow avoids immediate contact with the starting frog.");
            Assert.IsTrue(frogs[0].Parts.Any(p=>p.Name=="CausticSkin"));
        }
        [Test] public void CrossingBandfrogRestsOnItsDryIslandWithoutChangingOrdinaryFrogs()
        {
            var factory=Factory();var z=new Zone(SoddenDistrictPlan.CrossingZoneID);var b=new SoddenDistrictBuilder(64);
            Assert.IsTrue(b.BuildZone(z,factory,new Random(1)));
            var frog=z.GetReadOnlyEntities().Single(e=>e.BlueprintName=="Bandfrog");var brain=frog.GetPart<BrainPart>();
            Assert.IsFalse(brain.Wanders);Assert.IsFalse(brain.WandersRandomly);Assert.IsTrue(brain.Staying);
            Assert.AreEqual((40,13),(brain.StartingCellX,brain.StartingCellY));Assert.IsFalse(b.Plan.IsWet(40,13));
            var ordinary=factory.CreateEntity("Bandfrog");var normal=ordinary.GetPart<BrainPart>();
            Assert.IsTrue(normal.Wanders);Assert.IsTrue(normal.WandersRandomly);Assert.IsFalse(normal.Staying);
            Assert.AreEqual(normal.Passive,brain.Passive);Assert.IsFalse(brain.Passive,"The resting frog still acquires real threats.");
            Assert.IsTrue(frog.HasPart<CausticSkinPart>());Assert.AreEqual(ordinary.HasPart<AISelfPreservationPart>(),frog.HasPart<AISelfPreservationPart>());
            Assert.AreEqual(ordinary.GetStatValue("AcidResistance"),frog.GetStatValue("AcidResistance"));
            brain.CurrentZone=z;brain.Rng=new Random(64);
            for(int turn=0;turn<240;turn++)frog.FireEvent("TakeTurn");
            Assert.AreEqual((40,13),z.GetEntityPosition(frog));Assert.AreEqual(9,frog.GetStatValue("Hitpoints"));
            var threat=factory.CreateEntity("Player");Assert.IsTrue(z.AddEntity(threat,40,12));
            brain.PersonalEnemies.Add(threat);frog.FireEvent("TakeTurn");
            Assert.AreSame(threat,brain.Target);Assert.IsTrue(brain.HasGoalOtherThan(nameof(BoredGoal)),"Stationing does not disable native combat goals.");
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(64)][TestCase(1729)]
        public void ShoreReedsFormSmallNativeColoniesWithoutOccupyingRoutesOrWater(int seed)
        {
            foreach(string id in Sites)
            {
                var z=new Zone(id);var b=new SoddenDistrictBuilder(seed);Assert.IsTrue(b.BuildZone(z,Factory(),new Random(1)));
                var reeds=z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="Reeds").ToArray();
                var remaining=new HashSet<(int x,int y)>(reeds.Select(e=>z.GetEntityPosition(e)));int colonies=0;
                foreach(var reed in reeds)
                {
                    var at=z.GetEntityPosition(reed);Assert.IsFalse(b.Plan.IsReserved(at.x,at.y));Assert.IsFalse(b.Plan.IsWet(at.x,at.y));
                    Assert.AreEqual("Grass",b.Plan.GroundAt(at.x,at.y));Assert.IsTrue(reed.HasTag("Vegetation"));Assert.IsFalse(reed.GetPart<PhysicsPart>().Solid);
                }
                while(remaining.Count>0)
                {
                    var first=remaining.First();var cluster=new List<(int x,int y)>{first};remaining.Remove(first);
                    for(int i=0;i<cluster.Count;i++)for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)
                    {var next=(cluster[i].x+dx,cluster[i].y+dy);if(remaining.Remove(next))cluster.Add(next);}
                    colonies++;Assert.That(cluster.Count,Is.InRange(3,6),id+" native colony size");
                    Assert.Greater(cluster.Select(p=>p.x).Distinct().Count(),1);Assert.Greater(cluster.Select(p=>p.y).Distinct().Count(),1);
                }
                Assert.That(colonies,Is.InRange(6,8),id+" separated shore colonies");
            }
        }
        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void ShelterPoolsHaveSteppedDryCornersButKeepTheirWetBodies(int seed)
        {
            var p=SoddenDistrictPlan.Create(SoddenDistrictPlan.StopZoneID,seed);int left=5+seed%5;
            Assert.IsFalse(p.IsWet(left,4));Assert.IsFalse(p.IsWet(left+1,4));Assert.IsTrue(p.IsWet(left+2,4));
            Assert.IsFalse(p.IsWet(left,5));Assert.IsTrue(p.IsWet(left+1,5));Assert.IsTrue(p.IsWet(left,6));
            Assert.IsTrue(p.IsWet(15,8));Assert.IsTrue(p.IsWet(65,15));
        }
        [Test] public void ShelterSecondRoomProvidesActualFreeSleepAndSeatActions()
        {
            var oldClock=TurnManager.Active;
            try{using(var scope=new HaulingContentScope())
            {
                scope.Seed(64);var z=new Zone(SoddenDistrictPlan.StopZoneID);var b=new SoddenDistrictBuilder(64);
                Assert.IsTrue(b.BuildZone(z,scope.Factory,new Random(1)));
                var beds=z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="Bed").ToArray();
                var chairs=z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="Chair").ToArray();Assert.AreEqual(1,beds.Length);Assert.AreEqual(1,chairs.Length);
                var bed=beds[0];var chair=chairs[0];
                foreach(var owner in new[]{bed,chair})
                {var at=z.GetEntityPosition(owner);Assert.IsTrue(b.Plan.IsInterior(at.x,at.y));Assert.IsFalse(b.Plan.IsWet(at.x,at.y));Assert.IsFalse(owner.GetPart<PhysicsPart>().Solid);}
                Assert.IsEmpty(bed.GetPart<BedPart>().Owner);Assert.IsFalse(bed.GetPart<BedPart>().Occupied);
                Assert.IsEmpty(chair.GetPart<ChairPart>().Owner);Assert.IsFalse(chair.GetPart<ChairPart>().Occupied);
                var actor=scope.Factory.CreateEntity("Player");
                // Native player bootstrap adds this runtime effect owner.
                if(!actor.HasPart<StatusEffectsPart>())actor.AddPart(new StatusEffectsPart());
                var atBed=z.GetEntityPosition(bed);Assert.IsTrue(z.AddEntity(actor,atBed.x,atBed.y));
                var clock=new TurnManager();var hp=actor.GetStat("Hitpoints");hp.BaseValue=hp.Max-5;
                Assert.IsTrue(PlayerBedService.TryAct(actor,bed,z,PlayerBedService.RestCommand));Assert.AreEqual(hp.Max,hp.Value);Assert.AreEqual(60,clock.TickCount);
                var atChair=z.GetEntityPosition(chair);Assert.IsTrue(z.MoveEntity(actor,atChair.x,atChair.y));
                Assert.IsTrue(InventorySystem.PerformAction(actor,chair,PlayerSeatService.SitCommand,z));Assert.IsTrue(chair.GetPart<ChairPart>().Occupied);
                Assert.IsFalse(PlayerBedService.TryAct(actor,bed,z,PlayerBedService.RestCommand),"A remote bed cannot grant more rest.");Assert.AreEqual(60,clock.TickCount);
                Assert.IsTrue(InventorySystem.PerformAction(actor,chair,PlayerSeatService.StandCommand,z));Assert.IsFalse(chair.GetPart<ChairPart>().Occupied);
            }}finally{typeof(TurnManager).GetProperty("Active").SetValue(null,oldClock);}
        }
        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void DryApproachesReachEveryUsefulOwnerAndAvoidStartingHostiles(int seed)
        {
            foreach(string id in Sites)
            {
                var z=new Zone(id);var b=new SoddenDistrictBuilder(seed);Assert.IsTrue(b.BuildZone(z,Factory(),new Random(1)));
                var hostiles=z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="Bandfrog"||e.BlueprintName=="MawToad").ToArray();
                var seen=new HashSet<(int x,int y)>{(0,12)};var queue=new Queue<(int x,int y)>();queue.Enqueue((0,12));
                while(queue.Count>0)
                {
                    var at=queue.Dequeue();foreach(var d in new[]{(1,0),(-1,0),(0,1),(0,-1)})
                    {
                        var next=(x:at.x+d.Item1,y:at.y+d.Item2);if(!z.InBounds(next.x,next.y)||seen.Contains(next)||b.Plan.IsWet(next.x,next.y)||z.GetCell(next.x,next.y).BlocksMovement())continue;
                        if(hostiles.Any(e=>{var p=z.GetEntityPosition(e);return Math.Max(Math.Abs(p.x-next.x),Math.Abs(p.y-next.y))<=e.GetPart<BrainPart>().SightRadius;}))continue;
                        seen.Add(next);queue.Enqueue(next);
                    }
                }
                foreach(var owner in z.GetReadOnlyEntities().Where(e=>new[]{"SoddenDressingBench","SoddenRouteNotice","SoddenWorksLocker","SoddenWorksSalvage","SumpsieveCrop","Bed","Chair"}.Contains(e.BlueprintName)))
                {
                    var p=z.GetEntityPosition(owner);Assert.IsTrue(new[]{(1,0),(-1,0),(0,1),(0,-1)}.Any(d=>seen.Contains((p.x+d.Item1,p.y+d.Item2))),id+" "+owner.BlueprintName);
                }
                Assert.IsTrue(seen.Contains((79,12)),"Dry through-route outside the actors' starting sight radius.");
            }
        }
        [Test] public void BuilderRefusesMissingDependenciesAndExistingGraphsWithoutChangingThem()
        {
            var factory=Factory();factory.Blueprints.Remove("MirePool");var empty=new Zone(SoddenDistrictPlan.CrossingZoneID);
            Assert.IsFalse(new SoddenDistrictBuilder(64).BuildZone(empty,factory,new Random(1)));Assert.AreEqual(0,empty.EntityCount);
            factory=Factory();var occupied=new Zone(SoddenDistrictPlan.StopZoneID);var source=factory.CreateEntity("Reeds");occupied.AddEntity(source,40,12);
            Assert.IsFalse(new SoddenDistrictBuilder(64).BuildZone(occupied,factory,new Random(1)));CollectionAssert.AreEquivalent(new[]{source},occupied.GetAllEntities());
            Assert.IsFalse(new SoddenDistrictBuilder(64).BuildZone(new Zone("Overworld.18.7.0"),factory,new Random(1)));
        }
        [Test] public void FinalValidationRejectsAMissingRequiredOwnerAndNeverRestoresIt()
        {
            var zone=new Zone(SoddenDistrictPlan.WorksZoneID);var builder=new SoddenDistrictBuilder(64);
            Assert.IsTrue(builder.BuildZone(zone,Factory(),new Random(1)));Assert.IsTrue(builder.ValidateFinal(zone));
            var locker=zone.GetAllEntities().Single(e=>e.BlueprintName=="SoddenWorksLocker");zone.RemoveEntity(locker);
            Assert.IsFalse(builder.ValidateFinal(zone));Assert.IsFalse(zone.GetAllEntities().Contains(locker));
        }
        internal static int Distance(SoddenDistrictPlan plan,(int x,int y) start,(int x,int y) end,bool dry,bool diagonal=false)
        {
            var queue=new Queue<((int x,int y) at,int d)>();var seen=new HashSet<(int,int)>{start};queue.Enqueue((start,0));
            while(queue.Count>0)
            {
                var c=queue.Dequeue();if(c.at==end)return c.d;
                foreach(var step in diagonal?new[]{(1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)}:new[]{(1,0),(-1,0),(0,1),(0,-1)})
                {
                    var n=(x:c.at.x+step.Item1,y:c.at.y+step.Item2);
                    if(n.x<0||n.y<0||n.x>=Zone.Width||n.y>=Zone.Height||!seen.Add(n)||dry&&plan.IsWet(n.x,n.y))continue;
                    string bp=plan.ObjectAt(n.x,n.y);if(bp=="PeatBank"||bp=="StoneWall"||bp=="DeadTree")continue;
                    queue.Enqueue((n,c.d+1));
                }
            }
            return -1;
        }
    }
}
