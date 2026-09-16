using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class OlderdeepCompositionAdversarialTests
    {
        // H1: invalid coordinates must not alias an edge owner or route bit.
        [TestCase(-1,0)] [TestCase(80,24)] [TestCase(0,25)] [TestCase(int.MinValue,int.MaxValue)]
        public void Adversarial_OutOfBoundsQueriesHaveNoNativeOwnership(int x,int y)
        {
            for(int d=0;d<3;d++){var p=OlderdeepCompositionPlan.Create(OlderdeepCompositionTests.Id(d),64);
                Assert.IsNull(p.GroundAt(x,y));Assert.IsNull(p.ObjectAt(x,y));Assert.IsFalse(p.IsApproach(x,y));}
        }

        // H2: consuming a shared generator stream or retrying generation may
        // never reauthor a place whose owners the player has changed.
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void Adversarial_CallerRandomConsumptionDoesNotRearrangeThePlace(int depth)
        {
            var f=GrovelandsCompositionTests.Factory();var a=new Zone(OlderdeepCompositionTests.Id(depth));var b=new Zone(a.ZoneID);
            var one=new OlderdeepCompositionBuilder(64);var two=new OlderdeepCompositionBuilder(64);var rng=new Random(9);
            for(int i=0;i<400;i++)rng.Next();Assert.IsTrue(one.BuildZone(a,f,rng));Assert.IsTrue(two.BuildZone(b,f,new Random(1)));
            Assert.AreEqual(one.Plan.Signature(),two.Plan.Signature());
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)CollectionAssert.AreEquivalent(a.GetCell(x,y).Objects.Select(e=>e.BlueprintName),b.GetCell(x,y).Objects.Select(e=>e.BlueprintName));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void Adversarial_RejectedRebuildPreservesDestructionAndClearsPublishedPlan(int depth)
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(OlderdeepCompositionTests.Id(depth));var b=new OlderdeepCompositionBuilder(64);
            Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var victim=z.GetAllEntities().First(e=>e.BlueprintName==(depth==0?"Tree":"SandstoneWall"));z.RemoveEntity(victim);
            var owners=z.GetAllEntities().ToArray();var reserve=z.GenReservedCells.ToArray();
            Assert.IsFalse(b.BuildZone(z,f,new Random(1)));Assert.IsNull(b.Plan);CollectionAssert.AreEquivalent(owners,z.GetAllEntities());
            CollectionAssert.AreEquivalent(reserve,z.GenReservedCells);Assert.IsNull(z.GetEntityCell(victim));
        }

        // H3: missing supply or lamp dependencies must not be discovered only
        // after two thousand floor owners have already committed.
        [TestCase("Grass",0)] [TestCase("StoneFloor",0)] [TestCase("Tree",0)] [TestCase("Bush",0)]
        [TestCase("SandstoneWall",1)] [TestCase("DescentLedge",1)] [TestCase("RopeAnchor",1)] [TestCase("BeetleJar",1)]
        [TestCase("Sack",1)] [TestCase("Bones",1)] [TestCase("Torch",1)] [TestCase("DriedMeat",1)] [TestCase("HealingTonic",1)]
        public void Adversarial_MissingNativeDependencyRejectsBeforeMutation(string bp,int depth)
        {
            var f=GrovelandsCompositionTests.Factory();Assert.IsTrue(f.Blueprints.Remove(bp));var z=new Zone(OlderdeepCompositionTests.Id(depth));z.GenReservedCells.Add((79,24));
            var b=new OlderdeepCompositionBuilder(64);Assert.IsFalse(b.BuildZone(z,f,new Random(1)));Assert.IsNull(b.Plan);
            Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
        }

        // H4: CreateEntity fails soft; existence alone does not establish the
        // collision, light, examination, destruction or actual supply verbs.
        [TestCase("Grass","Physics","Solid","true",0)]
        [TestCase("StoneFloor","Physics","Takeable","true",0)]
        [TestCase("StoneFloor","Render","RenderString","?",0)]
        [TestCase("Tree","Destructible",null,null,0)]
        [TestCase("Bush","Material",null,null,0)]
        [TestCase("SandstoneWall","Destructible","HP","0",1)]
        [TestCase("DescentLedge","Physics","Solid","true",1)]
        [TestCase("RopeAnchor","Examinable",null,null,1)]
        [TestCase("BeetleJar","LightSource",null,null,1)]
        [TestCase("BeetleJar","Physics","Takeable","true",1)]
        [TestCase("Sack","Container",null,null,1)]
        [TestCase("Torch","Fuel",null,null,1)]
        [TestCase("DriedMeat","Food",null,null,1)]
        [TestCase("HealingTonic","Tonic",null,null,1)]
        public void Adversarial_MalformedNativePartsRejectWhileReservationsAndOwnersAreUntouched(string bp,string part,string key,string value,int depth)
        {
            var f=GrovelandsCompositionTests.Factory();if(key==null)Assert.IsTrue(f.Blueprints[bp].Parts.Remove(part));else f.Blueprints[bp].Parts[part][key]=value;
            var z=new Zone(OlderdeepCompositionTests.Id(depth));z.GenReservedCells.Add((79,24));
            Assert.IsFalse(new OlderdeepCompositionBuilder(64).BuildZone(z,f,new Random(1)));Assert.AreEqual(0,z.EntityCount);
            CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
        }

        // H5: lower-first access must produce reciprocal physical stairs,
        // rather than a view that looks like a descent with no way home.
        [TestCase(1)] [TestCase(2)]
        public void Adversarial_LowerFirstCreatesActualPairedReservedReturnStairs(int depth)
        {
            foreach(int seed in new[]{64,1729,2048})
            {
                var m=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),seed);var low=m.GetZone(OlderdeepCompositionTests.Id(depth));var up=m.GetZone(OlderdeepCompositionTests.Id(depth-1));
                var c=m.GetConnections(up.ZoneID).Single(e=>e.SourceZoneID==up.ZoneID&&e.TargetZoneID==low.ZoneID&&e.Type=="StairsDown");
                Assert.IsTrue(up.GetCell(c.SourceX,c.SourceY).Objects.Any(e=>e.HasPart<StairsDownPart>()));
                Assert.IsTrue(low.GetCell(c.TargetX,c.TargetY).Objects.Any(e=>e.HasPart<StairsUpPart>()));
                Assert.IsFalse(up.GetCell(c.SourceX,c.SourceY).BlocksMovement());Assert.IsFalse(low.GetCell(c.TargetX,c.TargetY).BlocksMovement());
                Assert.IsTrue(low.GenReservedCells.Contains((c.TargetX,c.TargetY)));
            }
        }

        // H6: a familiar address cannot override live profile/type authority.
        [TestCase(POIType.Village)] [TestCase(POIType.Sinkhole)]
        public void Adversarial_RuntimeReplacementVetoesTheOlderdeepComposition(POIType type)
        {
            var m=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),64);
            Assert.IsTrue(CathedralCompositionTests.Pipeline(m,OlderdeepCompositionTests.Id(2)).Builders.Any(b=>b is OlderdeepCompositionBuilder));
            m.WorldMap.SetPOI(4,6,new PointOfInterest(type,"Another place",tier:4));
            Assert.IsFalse(CathedralCompositionTests.Pipeline(m,OlderdeepCompositionTests.Id(2)).Builders.Any(b=>b is OlderdeepCompositionBuilder));
        }

        [Test] public void Adversarial_RenamedFoundingProfileStillUsesItsOwnNativeChamber()
        {
            var m=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),64);m.WorldMap.GetPOI(4,6).Name="A changed founding name";
            Assert.IsTrue(CathedralCompositionTests.Pipeline(m,OlderdeepCompositionTests.Id(2)).Builders.Any(b=>b is OlderdeepCompositionBuilder));
            OlderdeepCompositionTests.AssertGap(m.GetZone(OlderdeepCompositionTests.Id(2)));
        }

        // H7: physical access to the real listeners/homes matters independently
        // of graph floods after mobile actors have been removed.
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void Adversarial_FinalResidentsPlaquesAndHomesHaveReachableFrontages(int seed)
        {
            var z=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),seed).GetZone(OlderdeepCompositionTests.Id(2));
            var owners=z.GetAllEntities().Where(e=>new[]{"FoundingListener","FoundingPlaqueTender","NicheHome","PlaqueWall","PlaqueOldest"}.Contains(e.BlueprintName)).ToArray();
            Assert.Greater(owners.Length,8);var reached=OlderdeepCompositionTests.Flood(z);
            foreach(var e in owners)
            {
                var c=z.GetEntityCell(e);Assert.IsTrue(OlderdeepCompositionTests.Neighbors(c.X,c.Y).Any(n=>z.InBounds(n.x,n.y)&&reached[n.x,n.y]&&!z.GetCell(n.x,n.y).BlocksMovement()),e.BlueprintName+" lacks a usable frontage at"+c.X+","+c.Y);
            }
        }

        // H8: the actual composed place must still lead through guarded
        // tepuibone offering into same-cell native rest. The adjacent-cell
        // control catches a mesh mistakenly expanding the interaction area.
        [TestCase(true)] [TestCase(false)]
        public void Adversarial_RealTenderOfferingThenPlumeRestRetainsTrustAndSameCellContract(bool sameCell)
        {
            var oldZone=SettlementRuntime.ActiveZone;var oldState=NarrativeStatePart.Current;
            try
            {
                FactionManager.Initialize();PlayerReputation.Set("CatacombFolk",0);ConversationLoader.Reset();NarrativeStatePart.Current=new NarrativeStatePart();
                var f=GrovelandsCompositionTests.Factory();var z=SettlementRuntime.ActiveZone=new OverworldZoneManager(f,64).GetZone(OlderdeepCompositionTests.Id(2));
                var tender=z.GetAllEntities().Single(e=>e.BlueprintName=="FoundingPlaqueTender");var tc=z.GetEntityCell(tender);
                var player=new Entity{BlueprintName="Player"};player.SetTag("Player");player.SetTag("Creature");
                player.AddPart(new InventoryPart{MaxWeight=100});player.AddPart(new RenderPart());player.AddPart(new ExaminablePart());player.AddPart(new StatusEffectsPart());
                player.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=5,Min=0,Max=40};player.Statistics["Speed"]=new Stat{Name="Speed",BaseValue=100,Min=0,Max=200};
                var near=OlderdeepCompositionTests.Neighbors(tc.X,tc.Y).First(n=>z.InBounds(n.x,n.y)&&!z.GetCell(n.x,n.y).BlocksMovement());z.AddEntity(player,near.x,near.y);
                var stone=f.CreateEntity("Tepuibone");stone.GetPart<StackerPart>().StackCount=2;player.GetPart<InventoryPart>().AddObject(stone);
                ConversationActions.Execute("OfferFoundingStone",tender,player,"");Assert.AreEqual(50,PlayerReputation.Get("CatacombFolk"));Assert.AreEqual(1,stone.GetPart<StackerPart>().StackCount);
                Assert.AreEqual(1,NarrativeStatePart.Current.GetFact("FoundingStoneOffered"));
                var plume=z.GetAllEntities().First(e=>e.BlueprintName=="FoundingPlume");var pc=z.GetEntityCell(plume);
                foreach(var actor in z.GetAllEntities().Where(e=>e!=player&&(e.HasPart<BrainPart>()||e.HasTag("Creature"))).ToArray())z.RemoveEntity(actor);
                z.RemoveEntity(player);z.AddEntity(player,pc.X-(sameCell?0:1),pc.Y);
                var turns=new TurnManager();turns.AddEntity(player);turns.ProcessUntilPlayerTurn();int tick=turns.TickCount;
                var ev=GameEvent.New("InventoryAction");ev.SetParameter("Command",FoundingPlumePart.SleepCommand);ev.SetParameter("Actor",(object)player);ev.SetParameter("Zone",(object)z);plume.FireEventAndRelease(ev);
                Assert.AreEqual(tick+(sameCell?60:0),turns.TickCount);Assert.AreEqual(sameCell?40:5,player.GetStat("Hitpoints").Value);
                Assert.AreEqual(sameCell?1:0,NarrativeStatePart.Current.GetFact("RootedMet"));
            }
            finally{ConversationManager.EndConversation();ConversationLoader.Reset();SettlementRuntime.ActiveZone=oldZone;NarrativeStatePart.Current=oldState;FactionManager.Reset();}
        }

        // H9: the wall and plume retain native destruction while the living
        // body remains the explicit old indestructible exception.
        [Test] public void Adversarial_DestructionChangesActualOwnersWithoutRebuildingTheFoundingGraph()
        {
            var f=GrovelandsCompositionTests.Factory();var m=new OverworldZoneManager(f,64);var z=m.GetZone(OlderdeepCompositionTests.Id(2));
            var old=DestructionSystem.EntityFactoryRef;
            try
            {
                DestructionSystem.EntityFactoryRef=f;var plume=z.GetAllEntities().First(e=>e.BlueprintName=="FoundingPlume");
                var body=z.GetAllEntities().Single(e=>e.BlueprintName=="TheRooted");Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(plume,1000,null,z));
                DestructionSystem.Damage(body,10000,null,z);Assert.NotNull(z.GetEntityCell(body));Assert.IsNull(z.GetEntityCell(plume));
                Assert.AreSame(z,m.GetZone(z.ZoneID));Assert.AreEqual(10,z.GetAllEntities().Count(e=>e.BlueprintName=="FoundingPlume"));
                var jar=z.GetAllEntities().First(e=>e.BlueprintName=="BeetleJar");Assert.IsNull(jar.GetPart<DestructiblePart>());Assert.IsFalse(jar.GetPart<PhysicsPart>().Takeable);
            }
            finally{DestructionSystem.EntityFactoryRef=old;}
        }

        // H10: generic edge repair is not neutral. It adds arbitrary narrow
        // edge tunnels which the later founding shell can sever. The semantic
        // base already provides all four entry directions without that pass.
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void Adversarial_FoundingBaseAlreadyConnectsEveryCellWithoutRandomEdgeTunnels(int seed)
        {
            var z=new Zone(OlderdeepCompositionTests.Id(2));
            Assert.IsTrue(new OlderdeepCompositionBuilder(seed).BuildZone(z,GrovelandsCompositionTests.Factory(),new Random(1)));
            AssertAllOpenReached(z);
        }

        // H11: an unusually placed real return stair can move the native body
        // row. Entry restoration must retain its entire new gap/root barrier
        // and the exact stair owner, not simply protect fixed row12 art.
        [TestCase(58,12)] [TestCase(65,12)] [TestCase(66,14)] [TestCase(72,8)]
        public void Adversarial_EntryRestorationPreservesUnusualNativeStairsAndTheChosenSacredAnchor(int sx,int sy)
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(OlderdeepCompositionTests.Id(2));
            Assert.IsTrue(new OlderdeepCompositionBuilder(64).BuildZone(z,f,new Random(1)));
            foreach(var e in z.GetCell(sx,sy).Objects.Where(e=>e.BlueprintName=="SandstoneWall").ToArray())z.RemoveEntity(e);
            var stairs=f.CreateEntity("StairsUp");Assert.IsTrue(z.AddEntity(stairs,sx,sy));
            Assert.IsTrue(new FoundingVillageBuilder().BuildZone(z,f,new Random(64)));
            var sacred=z.GetAllEntities().Where(e=>e.BlueprintName=="TheRooted"||e.BlueprintName=="FoundingPlume").ToArray();
            Assert.IsTrue(new OlderdeepArrivalReservationBuilder(64).BuildZone(z,f,new Random(1)));
            CollectionAssert.AreEquivalent(sacred,z.GetAllEntities().Where(e=>e.BlueprintName=="TheRooted"||e.BlueprintName=="FoundingPlume"));
            Assert.AreEqual((sx,sy),z.GetEntityPosition(stairs));OlderdeepCompositionTests.AssertGap(z);
            OlderdeepCompositionTests.RemoveMobileActors(z);AssertAllOpenReached(z);
        }
        private static void AssertAllOpenReached(Zone z)
        {
            var reached=OlderdeepCompositionTests.Flood(z);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)if(!z.GetCell(x,y).BlocksMovement())Assert.IsTrue(reached[x,y],"Disconnected "+x+","+y);
            Assert.IsTrue(Enumerable.Range(0,80).Any(x=>reached[x,0]));Assert.IsTrue(Enumerable.Range(0,80).Any(x=>reached[x,24]));
            Assert.IsTrue(Enumerable.Range(0,25).Any(y=>reached[79,y]));
        }

        // H12: a failed late generation gate must identify why it refused,
        // rather than leaving an unexplained false result and silent world.
        [TestCase("Overworld.4.6.3","outside-olderdeep")]
        [TestCase("Overworld.4.6.2","missing-founding-stamp")]
        public void Adversarial_ArrivalRefusalIsDiagnosableAndLeavesTheGraphUntouched(string id,string reason)
        {
            Diag.ResetAll();
            try
            {
                var z=new Zone(id);z.GenReservedCells.Add((79,24));
                Assert.IsFalse(new OlderdeepArrivalReservationBuilder(64).BuildZone(z,GrovelandsCompositionTests.Factory(),new Random(1)));
                Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
                var records=DiagQuery.Apply(new DiagQuery.Filter{Category="worldgen",Kind="OlderdeepArrivalsRejected",Limit=10}).Records;
                Assert.AreEqual(1,records.Count);StringAssert.Contains(reason,records[0].PayloadJson);
                Assert.AreEqual(0,DiagQuery.Apply(new DiagQuery.Filter{Category="worldgen",Kind="OlderdeepArrivalsReserved",Limit=10}).Records.Count);
            }
            finally{Diag.ResetAll();}
        }
        [Test] public void Adversarial_ValidArrivalEmitsOnlyTheSuccessfulRecord()
        {
            Diag.ResetAll();
            try
            {
                var z=new Zone(OlderdeepCompositionTests.Id(0));var f=GrovelandsCompositionTests.Factory();
                Assert.IsTrue(z.AddEntity(f.CreateEntity("StairsDown"),40,12));
                Assert.IsTrue(new OlderdeepArrivalReservationBuilder(64).BuildZone(z,f,new Random(1)));
                Assert.IsTrue(z.GenReservedCells.Contains((40,12)));Assert.AreEqual(1,z.EntityCount);
                Assert.AreEqual(1,DiagQuery.Apply(new DiagQuery.Filter{Category="worldgen",Kind="OlderdeepArrivalsReserved",Limit=10}).Records.Count);
                Assert.AreEqual(0,DiagQuery.Apply(new DiagQuery.Filter{Category="worldgen",Kind="OlderdeepArrivalsRejected",Limit=10}).Records.Count);
            }
            finally{Diag.ResetAll();}
        }
    }
}
