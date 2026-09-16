using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class WellmeetCompositionAdversarialTests
    {
        [TestCase("RoadStone")] [TestCase("DryBrush")] [TestCase("Rock")] [TestCase("Crate")]
        [TestCase("Sand")] [TestCase("StoneFloor")] [TestCase("TentWall")] [TestCase("Bed")]
        [TestCase("TentRightHost")] [TestCase("SaltMaster")] [TestCase("GuestClothPole")] [TestCase("Well")]
        public void MissingBaseOrLateProfileDependencyRejectsBeforeTerrainChanges(string bp)
        {
            var f=GrovelandsCompositionTests.Factory();Assert.IsTrue(f.Blueprints.Remove(bp));var z=new Zone("Overworld.8.16.0");z.GenReservedCells.Add((79,24));
            Assert.IsFalse(new WellmeetCompositionBuilder(64).BuildZone(z,f,new Random(1)));Assert.AreEqual(0,z.EntityCount);
            CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
        }
        [TestCase("TentWall","Physics")] [TestCase("TentWall","Destructible")] [TestCase("TentWall","Material")]
        [TestCase("TentWall","Thermal")] [TestCase("TentRightHost","Conversation")] [TestCase("SaltMaster","WantsMineral")]
        [TestCase("Well","Well")] [TestCase("Bed","Bed")] [TestCase("Chair","Chair")] [TestCase("Sand","Render")]
        public void MalformedNativeContentRejectsWithoutPartialTerrainOrShade(string bp,string part)
        {
            var f=GrovelandsCompositionTests.Factory();Assert.IsTrue(f.Blueprints[bp].Parts.Remove(part));var z=new Zone("Overworld.8.16.0");
            Assert.IsFalse(new WellmeetCompositionBuilder(64).BuildZone(z,f,new Random(1)));Assert.AreEqual(0,z.EntityCount);Assert.AreEqual(0,z.GenReservedCells.Count);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)Assert.IsFalse(z.GetCell(x,y).IsInterior);
        }
        [Test] public void ABlockedLateServiceCellCannotCauseAPartiallyPlacedProfile()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.8.16.0");var b=new WellmeetCompositionBuilder(64);
            Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
            var p=b.Plan;var blocker=f.CreateEntity("SandstoneWall");Assert.IsTrue(z.AddEntity(blocker,p.HostX,p.HostY));
            var before=z.GetAllEntities().ToArray();Assert.IsFalse(new WellmeetProfileBuilder(b).BuildZone(z,f,new Random(1)));
            CollectionAssert.AreEquivalent(before,z.GetAllEntities());
        }
        [TestCase(false)] [TestCase(true)]
        public void InteriorReservationPolicyIsOptInAndIndependentOfShade(bool respect)
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.8.16.0");
            Assert.IsTrue(z.AddEntity(f.CreateEntity("StoneFloor"),5,5));Assert.IsTrue(z.AddEntity(f.CreateEntity("StoneFloor"),6,5));z.GenReservedCells.Add((5,5));
            var pop=new VillagePopulationBuilder(new PointOfInterest(POIType.Village,"Wellmeet",profile:"TentCamp"));
            var field=typeof(VillagePopulationBuilder).GetProperty("RespectInteriorReservations");Assert.NotNull(field);Assert.AreEqual(false,field.GetValue(pop));field.SetValue(pop,respect);
            var method=typeof(VillagePopulationBuilder).GetMethod("GatherInteriorCells",BindingFlags.Instance|BindingFlags.NonPublic);
            var cells=(List<(int x,int y)>)method.Invoke(pop,new object[]{z});
            Assert.AreEqual(!respect,cells.Contains((5,5)));Assert.IsTrue(cells.Contains((6,5)));
        }
        [Test] public void ClearedGenerationAttemptCanRetryWithoutReusingItsOldProfileOwners()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.8.16.0");var b=new WellmeetCompositionBuilder(64);var late=new WellmeetProfileBuilder(b);
            Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsTrue(late.BuildZone(z,f,new Random(1)));
            var old=z.GetAllEntities().ToArray();foreach(var e in old)z.RemoveEntity(e);
            // Matches the native pipeline's cleanup before its next bounded attempt.
            Assert.IsTrue(b.BuildZone(z,f,new Random(2)));Assert.IsTrue(late.BuildZone(z,f,new Random(2)));
            Assert.IsFalse(z.GetAllEntities().Any(old.Contains));
            Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName=="TentRightHost"));
        }
        [Test] public void BedsAndChairsDoNotBecomePublicApproachTiles()
        {
            foreach(int seed in new[]{1,64,1729,729490642,int.MinValue,int.MaxValue})
            {
                var p=WellmeetCompositionPlan.Create("Overworld.8.16.0",seed);
                for(int y=0;y<25;y++)for(int x=0;x<80;x++)
                    if(p.ObjectAt(x,y)=="Bed"||p.ObjectAt(x,y)=="Chair")Assert.IsFalse(p.IsApproach(x,y),"Furniture on publicroute "+x+","+y+" seed "+seed);
            }
        }
        [Test] public void ActualCaveStairRollsAreProtectedBeforeLateVillageResidents()
        {
            var f=GrovelandsCompositionTests.Factory();int stairs=0;
            foreach(int seed in new[]{1,2,3,4,5,64,1729,729490642})
            {
                var z=new OverworldZoneManager(f,seed).GetZone("Overworld.8.16.0");
                foreach(var stair in z.GetAllEntities().Where(e=>e.HasPart<StairsDownPart>()))
                {
                    stairs++;var c=z.GetEntityCell(stair);Assert.IsTrue(z.GenReservedCells.Contains((c.X,c.Y)),"Actual cave arrival must be reserved, seed "+seed);
                    Assert.IsFalse(c.BlocksMovement(),"An NPC/service cannot occupy the new cave link, seed "+seed);
                }
            }
            Assert.Greater(stairs,0,"Countercontrol confirms the native roll actually produced stairs.");
        }
        [Test] public void NativeHostAndSaltMasterRetainRealOathAndPaidMineralActions()
        {
            FactionManager.Initialize();
            try
            {
                var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.8.16.0");var b=new WellmeetCompositionBuilder(64);
                Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsTrue(new WellmeetProfileBuilder(b).BuildZone(z,f,new Random(1)));
                var host=z.GetAllEntities().Single(e=>e.BlueprintName=="TentRightHost");var saltMaster=z.GetAllEntities().Single(e=>e.BlueprintName=="SaltMaster");
                var visitor=new Entity{BlueprintName="Player"};visitor.SetTag("Player");visitor.AddPart(new InventoryPart());
                ConversationActions.Execute("ClaimGuestRight",host,visitor,"");Assert.IsTrue(visitor.HasEffect<UnderTheClothEffect>());
                var person=new Entity();person.SetTag("Faction","TentRight");var beast=new Entity();beast.SetTag("Faction","Beasts");
                Assert.IsTrue(UnderTheClothEffect.Protects(person,visitor));Assert.IsFalse(UnderTheClothEffect.Protects(beast,visitor));
                Assert.AreEqual(WorldClock.CurrentTick+UnderTheClothEffect.OathTicks,visitor.GetPart<StatusEffectsPart>().GetEffect<UnderTheClothEffect>().ExpiryTick);
                var inv=visitor.GetPart<InventoryPart>();Assert.IsTrue(inv.AddObject(f.CreateEntity("PaleSalt")));int before=PlayerReputation.Get("TentRight");
                ConversationActions.Execute("SellMineral",saltMaster,visitor,"PaleSalt");Assert.IsFalse(inv.Objects.Any(e=>e.BlueprintName=="PaleSalt"));
                Assert.AreEqual(before+5,PlayerReputation.Get("TentRight"));ConversationActions.Execute("SellMineral",saltMaster,visitor,"PaleSalt");
                Assert.AreEqual(before+5,PlayerReputation.Get("TentRight"),"No unpaid reputation on a repeated empty-inventory action.");
            }
            finally{FactionManager.Reset();}
        }
        [Test] public void RealTentInteriorStopsGlareWhileItsOutdoorCountercontrolDoesNot()
        {
            BeatingGlareSystem.ResetForTests();
            try
            {
                var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.8.16.0");var b=new WellmeetCompositionBuilder(64);
                Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var t=b.Plan.Tents[0];int x=t.X+2,y=t.Y+2;
                var visitor=new Entity{BlueprintName="Player"};visitor.SetTag("Player");visitor.SetTag("Creature");
                visitor.Statistics["Strength"]=new Stat{Name="Strength",BaseValue=16};visitor.Statistics["Agility"]=new Stat{Name="Agility",BaseValue=16};
                Assert.IsTrue(z.AddEntity(visitor,x,y));Assert.IsTrue(z.GetCell(x,y).IsInterior);
                for(int n=0;n<20;n++)BeatingGlareSystem.OnPlayerTurnEnd(visitor,z,350);Assert.IsFalse(visitor.HasEffect<ParchedEffect>());
                z.GetCell(x,y).IsInterior=false;
                for(int n=0;n<10;n++)BeatingGlareSystem.OnPlayerTurnEnd(visitor,z,350);Assert.IsTrue(visitor.HasEffect<ParchedEffect>());
            }
            finally{BeatingGlareSystem.ResetForTests();}
        }
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void FinalNativePublicServicesHaveReachableFrontagesAndDoorsStayOpen(int seed)
        {
            var f=GrovelandsCompositionTests.Factory();var z=new OverworldZoneManager(f,seed).GetZone("Overworld.8.16.0");var p=WellmeetCompositionPlan.Create(z.ZoneID,seed);
            var services=z.GetAllEntities().Where(e=>new[]{"TentRightHost","SaltMaster","Well","Shrine","Merchant","Quartermaster"}.Contains(e.BlueprintName))
                .Select(e=>(bp:e.BlueprintName,pos:z.GetEntityPosition(e))).ToArray();
            foreach(var actor in z.GetAllEntities().Where(e=>e.HasPart<BrainPart>()||e.HasTag("Creature")).ToArray())z.RemoveEntity(actor);
            var reached=FormationReachability.FloodFromWest(z);
            foreach(var t in p.Tents)Assert.IsFalse(z.GetCell(t.DoorX,t.DoorY).BlocksMovement(),"Late native furniture cannot plug a tent doorway.");
            Assert.IsTrue(FormationReachability.FullyReached(z,reached));
            foreach(var service in services)
            {
                bool accessible=false;
                foreach(var d in new[]{(-1,0),(1,0),(0,-1),(0,1)})
                {var c=z.GetCell(service.pos.x+d.Item1,service.pos.y+d.Item2);if(c!=null&&!c.BlocksMovement())accessible=true;}
                Assert.IsTrue(accessible,service.bp+" has no standing frontage");
            }
        }
    }
}
