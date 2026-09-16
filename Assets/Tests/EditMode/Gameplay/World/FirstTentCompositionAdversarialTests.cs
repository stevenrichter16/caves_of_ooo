using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Hypotheses about interrupted generation, malformed inherited
    /// content, real cave arrivals, later services, and the limits of hospitality.</summary>
    public class FirstTentCompositionAdversarialTests
    {
        private const string Id=FirstTentCompositionTests.Id;
        [SetUp] public void LoadNativeLoot()=>CinderholdCompositionTests.LoadLoot();
        [TearDown] public void ResetNativeLoot()=>LootTableRegistry.ResetForTests();
        [TestCase("Sand")] [TestCase("StoneFloor")] [TestCase("RoadStone")] [TestCase("TentWall")]
        [TestCase("Bed")] [TestCase("Chair")] [TestCase("Crate")] [TestCase("DryBrush")] [TestCase("Rock")]
        [TestCase("TentRightHost")] [TestCase("SaltMaster")] [TestCase("GuestClothPole")] [TestCase("Well")]
        public void MissingEarlyOrLateDependencyCannotLeaveAnUnfinishedOathCourt(string bp)
        {
            var f=FirstTentCompositionTests.Factory();Assert.IsTrue(f.Blueprints.Remove(bp));var z=new Zone(Id);z.GenReservedCells.Add((79,24));var b=new FirstTentCompositionBuilder(64);
            Assert.IsFalse(b.BuildZone(z,f,new Random(1)));Assert.IsNull(b.Plan);Assert.IsNull(b.RealizedZone);Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)Assert.IsFalse(z.GetCell(x,y).IsInterior);
        }
        [TestCase("Sand","Render","RenderString","?")] [TestCase("StoneFloor","Physics","Takeable","true")]
        [TestCase("RoadStone","Physics","Solid","true")] [TestCase("TentWall","Destructible","HP","0")]
        [TestCase("TentWall","Material",null,null)] [TestCase("TentWall","Thermal",null,null)]
        [TestCase("Bed","Bed",null,null)] [TestCase("Chair","Chair",null,null)] [TestCase("Crate","Container",null,null)]
        [TestCase("DryBrush","Destructible",null,null)] [TestCase("Rock","Material",null,null)]
        [TestCase("TentRightHost","Conversation","ConversationID","Merchant_1")]
        [TestCase("TentRightHost","Render","RenderString","?")] [TestCase("TentRightHost","AISelfPreservation",null,null)]
        [TestCase("SaltMaster","WantsMineral","Minerals","ChoirIron")] [TestCase("SaltMaster","WantsMineral","Faction","Villagers")]
        [TestCase("SaltMaster","WantsMineral","RepReward","500")] [TestCase("GuestClothPole","Physics","Solid","true")]
        [TestCase("Well","Well",null,null)]
        public void FailSoftPartFieldsCannotPublishVisibleButMechanicallyFalseOwners(string bp,string part,string key,string value)
        {
            var f=FirstTentCompositionTests.Factory();if(key==null)Assert.IsTrue(f.Blueprints[bp].Parts.Remove(part));else f.Blueprints[bp].Parts[part][key]=value;
            var z=new Zone(Id);z.GenReservedCells.Add((79,24));Assert.IsFalse(new FirstTentCompositionBuilder(64).BuildZone(z,f,new Random(1)));Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
        }
        [TestCase(-1,0)] [TestCase(80,25)] [TestCase(int.MinValue,int.MaxValue)]
        public void InvalidCoordinatesNeverClampToSacredOrShadedGround(int x,int y)
        {var p=FirstTentCompositionPlan.Create(Id,64);Assert.IsNull(p.GroundAt(x,y));Assert.IsNull(p.ObjectAt(x,y));Assert.IsFalse(p.IsInterior(x,y));Assert.IsFalse(p.IsApproach(x,y));Assert.IsFalse(p.IsReserved(x,y));Assert.IsFalse(p.IsOathCourt(x,y));}
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void MissingZoneFactoryOrRandomCannotMutateFlags(int missing)
        {var z=new Zone(Id);var b=new FirstTentCompositionBuilder(64);Assert.IsFalse(b.BuildZone(missing==0?null:z,missing==1?null:FirstTentCompositionTests.Factory(),missing==2?null:new Random(1)));Assert.IsNull(b.Plan);Assert.AreEqual(0,z.EntityCount);}
        [TestCase(false)] [TestCase(true)]
        public void RejectedReuseCannotInvalidateAnAlreadySuccessfulOwnersPendingProfile(bool foreign)
        {
            var f=FirstTentCompositionTests.Factory();var z=new Zone(Id);var b=new FirstTentCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var p=b.Plan;var other=foreign?new Zone(Id):z;
            Assert.IsFalse(b.BuildZone(other,f,new Random(2)));Assert.AreSame(p,b.Plan);Assert.AreSame(z,b.RealizedZone);
            if(foreign){Assert.IsFalse(new FirstTentProfileBuilder(b).BuildZone(other,f,new Random(1)));Assert.IsFalse(new FirstTentArrivalReservationBuilder(b).BuildZone(other,f,new Random(1)));Assert.AreEqual(0,other.EntityCount);}
            Assert.IsTrue(new FirstTentProfileBuilder(b).BuildZone(z,f,new Random(3)));
        }
        [Test] public void RemovingAHostNeverResurrectsItButAnExplicitEmptyRetryGetsFreshOwners()
        {
            var f=FirstTentCompositionTests.Factory();var z=new Zone(Id);var b=new FirstTentCompositionBuilder(64);var late=new FirstTentProfileBuilder(b);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsTrue(late.BuildZone(z,f,new Random(1)));
            var original=z.GetAllEntities().ToArray();z.RemoveEntity(original.First(e=>e.BlueprintName=="TentRightHost"));var survivors=z.GetAllEntities().ToArray();
            Assert.IsFalse(b.BuildZone(z,f,new Random(2)));Assert.IsFalse(late.BuildZone(z,f,new Random(2)));CollectionAssert.AreEquivalent(survivors,z.GetAllEntities());
            foreach(var e in survivors)z.RemoveEntity(e);Assert.IsTrue(b.BuildZone(z,f,new Random(3)));Assert.IsTrue(late.BuildZone(z,f,new Random(3)));Assert.IsFalse(z.GetAllEntities().Any(original.Contains));Assert.AreEqual(2,z.GetAllEntities().Count(e=>e.BlueprintName=="TentRightHost"));
        }
        [TestCase(false)] [TestCase(true)]
        public void LastOwnerFailureCannotPublishHalfTheMonument(bool malformed)
        {
            var f=FirstTentCompositionTests.Factory();var z=new Zone(Id);var b=new FirstTentCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var last=b.Plan.Profile.Last();
            if(malformed)f.Blueprints[last.Blueprint].Parts["Physics"]["Takeable"]="true";else Assert.IsTrue(z.AddEntity(f.CreateEntity("SandstoneWall"),last.X,last.Y));var before=z.GetAllEntities().ToArray();
            Assert.IsFalse(new FirstTentProfileBuilder(b).BuildZone(z,f,new Random(1)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
        }
        [TestCase(POIType.Village,"TentCamp")] [TestCase(POIType.Village,null)] [TestCase(POIType.Lair,"TentCampFirst")] [TestCase(POIType.Village,"tentcampfirst")]
        public void ActualMapProfileAndPlaceTypeVetoTheComposition(POIType type,string profile)
        {
            var m=new OverworldZoneManager(FirstTentCompositionTests.Factory(),64);Assert.IsTrue(CinderholdCompositionTests.Pipeline(m,Id).Builders.OfType<FirstTentCompositionBuilder>().Any());
            m.WorldMap.SetPOI(5,17,new PointOfInterest(type,"the First Tent",profile:profile));Assert.IsFalse(CinderholdCompositionTests.Pipeline(m,Id).Builders.OfType<FirstTentCompositionBuilder>().Any());
        }
        [Test] public void RenamingThePlaceCannotRemoveItsProfileOrSpreadItToAnotherAddress()
        {
            var m=new OverworldZoneManager(FirstTentCompositionTests.Factory(),64);m.WorldMap.GetPOI(5,17).Name="the mortal promise";Assert.IsTrue(CinderholdCompositionTests.Pipeline(m,Id).Builders.OfType<FirstTentCompositionBuilder>().Any());
            m.WorldMap.SetPOI(6,17,new PointOfInterest(POIType.Village,"the First Tent",profile:"TentCampFirst"));Assert.IsFalse(CinderholdCompositionTests.Pipeline(m,"Overworld.6.17.0").Builders.OfType<FirstTentCompositionBuilder>().Any());
        }
        [Test] public void ThirtyTwoSeedsAndExtremeSeedsKeepEveryOpenBaseCellReachable()
        {
            var f=FirstTentCompositionTests.Factory();foreach(int seed in Enumerable.Range(0,32).Concat(new[]{int.MinValue,int.MaxValue}))
            {
                var z=new Zone(Id);var b=new FirstTentCompositionBuilder(seed);Assert.DoesNotThrow(()=>Assert.IsTrue(b.BuildZone(z,f,new Random(1))),"native base seed "+seed);var seen=DrownedLedgerCompositionTests.DryReach(z);DrownedLedgerCompositionTests.AssertFourDirections(seen);
                for(int y=0;y<25;y++)for(int x=0;x<80;x++)if(!z.GetCell(x,y).BlocksMovement())Assert.IsTrue(seen.Contains((x,y)),"seed "+seed+" pocket "+x+","+y);
            }
        }
        [TestCase("WaterPuddle")] [TestCase("SandstoneWall")]
        public void NativeCaveGateTestsLiveNeighborsNotJustAnEmptyCandidateCentre(string obstacle)
        {
            var f=FirstTentCompositionTests.Factory();var z=new Zone(Id);var b=new FirstTentCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var at=Candidate(b,z);Assert.IsTrue(b.CanPlaceCaveEntrance(z,z.GetCell(at.x,at.y)));
            Assert.IsFalse(b.CanPlaceCaveEntrance(new Zone(Id),z.GetCell(at.x,at.y)));Assert.IsFalse(b.CanPlaceCaveEntrance(z,new Zone(Id).GetCell(at.x,at.y)));
            var blocked=f.CreateEntity(obstacle);Assert.IsTrue(z.AddEntity(blocked,at.x+1,at.y));Assert.IsFalse(b.CanPlaceCaveEntrance(z,z.GetCell(at.x,at.y)));z.RemoveEntity(blocked);Assert.IsTrue(b.CanPlaceCaveEntrance(z,z.GetCell(at.x,at.y)));
        }
        public static (int x,int y) Candidate(FirstTentCompositionBuilder b,Zone z)
        {for(int y=1;y<24;y++)for(int x=1;x<79;x++)if(b.CanPlaceCaveEntrance(z,z.GetCell(x,y)))return(x,y);Assert.Fail("A native cave roll must retain eligible ground.");return(-1,-1);}
        [Test] public void ActualCaveRollsAvoidCourtAndSheltersAndReserveRealDryArrivals()
        {
            int stairs=0;var f=FirstTentCompositionTests.Factory();foreach(int seed in new[]{1,2,3,4,5,64,1729,729490642})
            {
                var z=new OverworldZoneManager(f,seed).GetZone(Id);var p=FirstTentCompositionPlan.Create(Id,seed);
                foreach(var owner in z.GetAllEntities().Where(e=>e.HasPart<StairsDownPart>()))
                {
                    stairs++;var c=z.GetEntityCell(owner);Assert.IsFalse(c.BlocksMovement());Assert.IsFalse(p.IsReserved(c.X,c.Y));Assert.IsFalse(p.IsInterior(c.X,c.Y));Assert.IsTrue(z.GenReservedCells.Contains((c.X,c.Y)));
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {var n=z.GetCell(c.X+dx,c.Y+dy);Assert.NotNull(n);Assert.IsFalse(n.IsInterior);Assert.IsFalse(n.BlocksMovement());Assert.IsTrue(z.GenReservedCells.Contains((n.X,n.Y)));Assert.IsFalse(n.Objects.Any(e=>e.HasPart<LiquidPoolPart>()));}
                }
            }Assert.Greater(stairs,0,"Exercise actual successful native rolls, not an empty loop.");
        }
        [Test] public void ShadeStopsActualGlareWhileTheUnroofedMonumentDoesNot()
        {
            BeatingGlareSystem.ResetForTests();try
            {
                var z=new Zone(Id);var b=new FirstTentCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,FirstTentCompositionTests.Factory(),new Random(1)));var r=b.Plan.Rooms[0];
                var player=CinderholdCompositionTests.Player();player.SetTag("Creature");player.Statistics["Agility"]=new Stat{Name="Agility",BaseValue=16};Assert.IsTrue(z.AddEntity(player,r.X+2,r.Y+2));
                for(int n=0;n<20;n++)BeatingGlareSystem.OnPlayerTurnEnd(player,z,350);Assert.IsFalse(player.HasEffect<ParchedEffect>());
                z.RemoveEntity(player);Assert.IsTrue(z.AddEntity(player,b.Plan.OathX,b.Plan.OathY));Assert.IsFalse(z.GetEntityCell(player).IsInterior);
                for(int n=0;n<20;n++)BeatingGlareSystem.OnPlayerTurnEnd(player,z,350);Assert.IsTrue(player.HasEffect<ParchedEffect>());
            }finally{BeatingGlareSystem.ResetForTests();}
        }
        [TestCase(false)] [TestCase(true)]
        public void ARealAttackOnPeopleBreaksTheOathWhileAttackingABeastDoesNot(bool beast)
        {
            FirstTentCompositionTests.WithGuest((f,z,player)=>
            {
                var host=z.GetAllEntities().First(e=>e.BlueprintName=="TentRightHost");FirstTentCompositionTests.Claim(host,player);var target=host;
                if(beast){target=f.CreateEntity("MawToad");Assert.NotNull(target);Assert.AreEqual("Beasts",target.GetTag("Faction"));var cell=DrownedLedgerCompositionTests.DryReach(z).Last();Assert.IsTrue(z.AddEntity(target,cell.Item1,cell.Item2));}
                var position=z.GetEntityPosition(target);var stand=CinderholdCompositionTests.Neighbors(position.x,position.y).First(n=>z.InBounds(n.x,n.y)&&!z.GetCell(n.x,n.y).BlocksMovement());z.RemoveEntity(player);Assert.IsTrue(z.AddEntity(player,stand.x,stand.y));
                int before=PlayerReputation.Get("TentRight");CombatSystem.PerformMeleeAttack(player,target,z,new Random(3));Assert.AreEqual(beast,player.HasEffect<UnderTheClothEffect>());Assert.AreEqual(before+(beast?0:UnderTheClothEffect.OathbreakRepLoss),PlayerReputation.Get("TentRight"));
            });
        }
        // Camera review exposed three nearly identical north-reception towns.
        // Semantic formation must change who faces whom, not merely wall length.
        [Test] public void FormationNamesRepresentThreeDifferentRoleRelationships()
        {
            var formation=typeof(FirstTentCompositionPlan).GetProperty("FormationName");Assert.NotNull(formation,"The selected semantic formation must be exposed to diagnostics/previews.");
            var names=new System.Collections.Generic.HashSet<string>();var arrangements=new System.Collections.Generic.HashSet<string>();
            foreach(int seed in Enumerable.Range(0,64))
            {
                var p=FirstTentCompositionPlan.Create(Id,seed);var name=(string)formation.GetValue(p);Assert.IsNotEmpty(name);names.Add(name);
                var guest=p.Rooms.Single(r=>r.Role=="GuestThreshold");var salt=p.Rooms.Single(r=>r.Role=="SaltService");
                bool guestNorth=guest.Y<p.OathY,saltNorth=salt.Y<p.OathY;arrangements.Add(guestNorth+":"+saltNorth);
                foreach(var r in p.Rooms)
                {
                    Assert.AreEqual(r.Y<p.OathY?r.Y+r.Height-1:r.Y,r.DoorY,"The doorway faces the common approach, not the outer desert.");
                    for(int dx=-1;dx<=1;dx++){Assert.IsNull(p.ObjectAt(r.DoorX+dx,r.DoorY));Assert.IsTrue(p.IsApproach(r.DoorX+dx,r.DoorY));}
                }
                Assert.GreaterOrEqual(Enumerable.Range(guest.X,guest.Width).Count(x=>p.ObjectAt(x,guest.DoorY)==null),5,"Guest receiving opens broadly toward the court, unlike the private household doorway.");
                var thresholdHost=p.Profile.Single(o=>o.Blueprint=="TentRightHost"&&!p.IsOathCourt(o.X,o.Y));
                Assert.That(Math.Abs(thresholdHost.X-guest.DoorX)+Math.Abs(thresholdHost.Y-guest.DoorY),Is.InRange(1,6));
                CollectionAssert.AreEquivalent(FirstTentCompositionTests.Profile,p.Profile.Select(o=>o.Blueprint));
            }
            Assert.AreEqual(3,names.Count);Assert.AreEqual(3,arrangements.Count,"Three labels may not conceal a single fixed formation.");
        }
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void MonumentRemainsOpenSandWithoutPavedSpursToEachPole(int seed)
        {
            var p=FirstTentCompositionPlan.Create(Id,seed);int natural=0;
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)if(p.IsOathCourt(x,y))
            {Assert.AreEqual("Sand",p.GroundAt(x,y),"The monument is an open gathering, not a road diagram.");Assert.IsTrue(p.IsReserved(x,y));Assert.IsFalse(p.IsInterior(x,y));natural++;}
            Assert.GreaterOrEqual(natural,85);
            foreach(var pole in p.Profile.Where(o=>o.Blueprint=="GuestClothPole"&&p.IsOathCourt(o.X,o.Y)))
                Assert.IsTrue(CinderholdCompositionTests.Neighbors(pole.X,pole.Y).Any(n=>p.ObjectAt(n.x,n.y)==null),"A pole still has ordinary standing access without a special paved spur.");
            Assert.IsTrue(Enumerable.Range(0,25).Any(y=>p.GroundAt(0,y)=="RoadStone"&&p.GroundAt(0,y+1)=="RoadStone"),"Keep the mapped road's broad welcome at the western edge.");
        }
        [Test] public void FailedArrivalOwnershipReportsRejectionAndKeepsAllOwnersUntouched()
        {
            Diag.ResetAll();Diag.SetChannel("worldgen",true);try
            {
                var f=FirstTentCompositionTests.Factory();var z=new Zone(Id);var b=new FirstTentCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var arrivals=new FirstTentArrivalReservationBuilder(b);Assert.AreEqual(3870,arrivals.Priority);var before=z.GetAllEntities().ToArray();
                Assert.IsFalse(arrivals.BuildZone(new Zone(Id),f,new Random(1)));Assert.IsTrue(arrivals.BuildZone(z,f,new Random(1)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
                Assert.AreEqual(1,DiagQuery.Apply(new DiagQuery.Filter{Category="worldgen",Kind="FirstTentArrivalsRejected",Limit=10}).Records.Count);
                Assert.AreEqual(1,DiagQuery.Apply(new DiagQuery.Filter{Category="worldgen",Kind="FirstTentArrivalsReserved",Limit=10}).Records.Count);
            }finally{Diag.ResetAll();}
        }
    }
}
