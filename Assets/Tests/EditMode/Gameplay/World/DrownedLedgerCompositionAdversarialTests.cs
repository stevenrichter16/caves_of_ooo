using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Player-flow hypotheses beyond the layout snapshots: changed
    /// content, wet arrivals, rejected replay, lost witness ownership and native
    /// source removal. These are independent gates, not manual scene repairs.</summary>
    public class DrownedLedgerCompositionAdversarialTests
    {
        [SetUp] public void LoadNativeLoot()=>CinderholdCompositionTests.LoadLoot();
        [TearDown] public void ResetNativeLoot()=>LootTableRegistry.ResetForTests();
        private const string Id=DrownedLedgerCompositionTests.Id;
        [TestCase("Floor")] [TestCase("StoneFloor")] [TestCase("TentWall")] [TestCase("SandstoneWall")]
        [TestCase("WaterPuddle")] [TestCase("PeatBank")] [TestCase("Duckboard")] [TestCase("Reeds")]
        [TestCase("DeadTree")]
        [TestCase("Bed")] [TestCase("Chair")] [TestCase("Crate")]
        [TestCase("PreFellingBody")] [TestCase("SurveyStake")] [TestCase("ReadingTable")]
        [TestCase("RecensionScribe")] [TestCase("CurationSorter")]
        public void MissingEarlyOrLateDependencyCannotLeaveHalfAnExcavation(string bp)
        {
            var f=DrownedLedgerCompositionTests.Factory();Assert.IsTrue(f.Blueprints.Remove(bp));var z=new Zone(Id);z.GenReservedCells.Add((79,24));
            var b=new DrownedLedgerCompositionBuilder(64);Assert.IsFalse(b.BuildZone(z,f,new Random(1)));Assert.IsNull(b.Plan);Assert.IsNull(b.RealizedZone);
            Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)Assert.IsFalse(z.GetCell(x,y).IsInterior);
        }
        [TestCase("Floor","Render","RenderString","?")]
        [TestCase("StoneFloor","Physics","Takeable","true")]
        [TestCase("TentWall","Destructible","HP","0")]
        [TestCase("SandstoneWall","Material",null,null)]
        [TestCase("WaterPuddle","LiquidPool","LiquidId","oil")]
        [TestCase("WaterPuddle","LiquidPool","Volume","0")]
        [TestCase("PeatBank","Destructible",null,null)]
        [TestCase("Duckboard","Material",null,null)]
        [TestCase("DeadTree","Destructible",null,null)] [TestCase("DeadTree","Material",null,null)]
        [TestCase("PreFellingBody","Physics","Takeable","true")]
        [TestCase("PreFellingBody","Physics","Weight","1")]
        [TestCase("PreFellingBody","Render","Visible","false")]
        [TestCase("SurveyStake","Physics","Solid","true")]
        [TestCase("ReadingTable","Examinable",null,null)]
        [TestCase("ReadingTable","Physics","Solid","false")]
        [TestCase("RecensionScribe","Conversation","ConversationID","Scribe_1")]
        [TestCase("CurationSorter","Conversation","ConversationID","Merchant_1")]
        [TestCase("CurationSorter","Render","RenderString","?")]
        [TestCase("CurationSorter","AISelfPreservation",null,null)]
        public void FailSoftNativeFieldsAreRejectedBeforeMutatingAnyCell(string bp,string part,string key,string value)
        {
            var f=DrownedLedgerCompositionTests.Factory();if(key==null)Assert.IsTrue(f.Blueprints[bp].Parts.Remove(part));else f.Blueprints[bp].Parts[part][key]=value;
            // PhysicsPart.Initialize honors the native Solid tag. Corrupt both
            // independent authorities to produce an actually non-solid table.
            if(bp=="ReadingTable"&&part=="Physics"&&key=="Solid")
            {Assert.IsTrue(f.Blueprints[bp].Tags.Remove("Solid"));Assert.IsFalse(f.CreateEntity(bp).GetPart<PhysicsPart>().Solid);}
            var z=new Zone(Id);z.GenReservedCells.Add((79,24));Assert.IsFalse(new DrownedLedgerCompositionBuilder(64).BuildZone(z,f,new Random(1)));
            Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
        }
        [Test] public void NativeSolidTagNormalizationStillProducesAValidReadingTable()
        {
            var f=DrownedLedgerCompositionTests.Factory();f.Blueprints["ReadingTable"].Parts["Physics"]["Solid"]="false";
            Assert.IsTrue(f.CreateEntity("ReadingTable").GetPart<PhysicsPart>().Solid,"PhysicsPart.Initialize restores native tag authority.");
            var z=new Zone(Id);var b=new DrownedLedgerCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsTrue(new DrownedLedgerProfileBuilder(b).BuildZone(z,f,new Random(1)));
            Assert.IsTrue(z.GetAllEntities().Single(e=>e.BlueprintName=="ReadingTable").GetPart<PhysicsPart>().Solid);
        }
        [TestCase(-1,0)] [TestCase(80,0)] [TestCase(0,25)] [TestCase(int.MinValue,int.MaxValue)]
        public void InvalidCoordinateQueriesNeverClampToARealWorksite(int x,int y)
        {
            var p=DrownedLedgerCompositionPlan.Create(Id,64);Assert.IsNull(p.GroundAt(x,y));Assert.IsNull(p.ObjectAt(x,y));
            Assert.IsFalse(p.IsWet(x,y));Assert.IsFalse(p.IsInterior(x,y));Assert.IsFalse(p.IsApproach(x,y));Assert.IsFalse(p.IsReserved(x,y));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void MissingZoneFactoryOrRandomRejectsWithoutChangingAnExistingPlan(int missing)
        {
            var f=DrownedLedgerCompositionTests.Factory();var z=new Zone(Id);var b=new DrownedLedgerCompositionBuilder(64);
            Assert.IsFalse(b.BuildZone(missing==0?null:z,missing==1?null:f,missing==2?null:new Random(1)));Assert.IsNull(b.Plan);Assert.AreEqual(0,z.EntityCount);
        }
        [TestCase(false)] [TestCase(true)]
        public void RefusedReusePreservesThePendingProfileOfTheSuccessfulZone(bool foreign)
        {
            var f=DrownedLedgerCompositionTests.Factory();var z=new Zone(Id);var b=new DrownedLedgerCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var p=b.Plan;
            var refused=foreign?new Zone(Id):z;Assert.IsFalse(b.BuildZone(refused,f,new Random(2)));Assert.AreSame(p,b.Plan);Assert.AreSame(z,b.RealizedZone);
            if(foreign){Assert.IsFalse(new DrownedLedgerProfileBuilder(b).BuildZone(refused,f,new Random(3)));Assert.AreEqual(0,refused.EntityCount);}
            Assert.IsTrue(new DrownedLedgerProfileBuilder(b).BuildZone(z,f,new Random(3)));Assert.AreEqual(3,z.GetAllEntities().Count(e=>e.BlueprintName=="PreFellingBody"));
        }
        [Test] public void RemovedWitnessIsNeverResurrectedButExplicitEmptyZoneRetryUsesFreshOwners()
        {
            var f=DrownedLedgerCompositionTests.Factory();var z=new Zone(Id);var b=new DrownedLedgerCompositionBuilder(64);var late=new DrownedLedgerProfileBuilder(b);
            Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsTrue(late.BuildZone(z,f,new Random(1)));var owners=z.GetAllEntities().ToArray();
            var body=owners.First(e=>e.BlueprintName=="PreFellingBody");z.RemoveEntity(body);var remaining=z.GetAllEntities().ToArray();
            Assert.IsFalse(b.BuildZone(z,f,new Random(2)));Assert.IsFalse(late.BuildZone(z,f,new Random(2)));CollectionAssert.AreEquivalent(remaining,z.GetAllEntities());
            foreach(var e in remaining)z.RemoveEntity(e);Assert.IsTrue(b.BuildZone(z,f,new Random(3)));Assert.IsTrue(late.BuildZone(z,f,new Random(3)));
            Assert.IsFalse(z.GetAllEntities().Any(owners.Contains));Assert.AreEqual(3,z.GetAllEntities().Count(e=>e.BlueprintName=="PreFellingBody"));
        }
        [TestCase(false)] [TestCase(true)]
        public void FailureAtTheLastLateOwnerCannotPartiallyPublishTheEarlierWitnesses(bool malformed)
        {
            var f=DrownedLedgerCompositionTests.Factory();var z=new Zone(Id);var b=new DrownedLedgerCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
            var last=b.Plan.Profile.Last();if(malformed)f.Blueprints[last.Blueprint].Parts["Physics"]["Takeable"]="true";else z.AddEntity(f.CreateEntity("SandstoneWall"),last.X,last.Y);
            var before=z.GetAllEntities().ToArray();Assert.IsFalse(new DrownedLedgerProfileBuilder(b).BuildZone(z,f,new Random(1)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
        }
        [TestCase(POIType.Village,"Intake")] [TestCase(POIType.Village,null)]
        [TestCase(POIType.Lair,"ExcavationCamp")] [TestCase(POIType.Village,"excavationcamp")]
        public void NativeRoutingRespectsActualProfileAndPlaceTypeAtTheExactAddress(POIType type,string profile)
        {
            var m=new OverworldZoneManager(DrownedLedgerCompositionTests.Factory(),64);Assert.IsTrue(CinderholdCompositionTests.Pipeline(m,Id).Builders.OfType<DrownedLedgerCompositionBuilder>().Any());
            m.WorldMap.SetPOI(17,5,new PointOfInterest(type,"Drowned Ledger",profile:profile));Assert.IsFalse(CinderholdCompositionTests.Pipeline(m,Id).Builders.OfType<DrownedLedgerCompositionBuilder>().Any());
        }
        [Test] public void ADisplayNameCannotCreateOrRemoveTheFinitePlaceContract()
        {
            var m=new OverworldZoneManager(DrownedLedgerCompositionTests.Factory(),64);m.WorldMap.GetPOI(17,5).Name="A renamed expedition";
            Assert.IsTrue(CinderholdCompositionTests.Pipeline(m,Id).Builders.OfType<DrownedLedgerCompositionBuilder>().Any());
            m.WorldMap.SetPOI(17,6,new PointOfInterest(POIType.Village,"Drowned Ledger",profile:"ExcavationCamp"));Assert.IsFalse(CinderholdCompositionTests.Pipeline(m,"Overworld.17.6.0").Builders.OfType<DrownedLedgerCompositionBuilder>().Any());
        }
        [Test] public void ThirtyTwoSeedsKeepEveryDryBaseCellReachableAndEveryBasinWhole()
        {
            var f=DrownedLedgerCompositionTests.Factory();
            foreach(int seed in Enumerable.Range(0,32).Concat(new[]{int.MinValue,int.MaxValue}))
            {
                var z=new Zone(Id);var b=new DrownedLedgerCompositionBuilder(seed);Assert.IsTrue(b.BuildZone(z,f,new Random(9)));var reached=DrownedLedgerCompositionTests.DryReach(z);
                for(int y=0;y<25;y++)for(int x=0;x<80;x++)if(!b.Plan.IsWet(x,y)&&!z.GetCell(x,y).BlocksMovement())Assert.IsTrue(reached.Contains((x,y)),"seed "+seed+" dry pocket "+x+","+y);
                DrownedLedgerCompositionTests.AssertFourDirections(reached);var pools=DrownedLedgerCompositionTests.WaterComponents(z);Assert.AreEqual(3,pools.Count);Assert.IsTrue(pools.All(p=>p.Count>=25));
            }
        }
        [Test] public void CaveCandidateGateRejectsForeignCellsWetBaysAndProtectedReadingSpace()
        {
            var f=DrownedLedgerCompositionTests.Factory();var z=new Zone(Id);var b=new DrownedLedgerCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));int valid=0,rejected=0;
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
            {
                bool candidate=b.CanPlaceCaveEntrance(z,z.GetCell(x,y));
                if(b.Plan.IsWet(x,y)||b.Plan.IsReserved(x,y)||b.Plan.IsInterior(x,y)||z.GetCell(x,y).BlocksMovement()){Assert.IsFalse(candidate);rejected++;}else if(candidate)valid++;
            }
            Assert.Greater(valid,0);Assert.Greater(rejected,100);Assert.IsFalse(b.CanPlaceCaveEntrance(new Zone(Id),z.GetCell(0,0)));Assert.IsFalse(b.CanPlaceCaveEntrance(z,new Zone(Id).GetCell(0,0)));Assert.IsFalse(b.CanPlaceCaveEntrance(z,null));
        }
        [Test] public void ActualCaveRollsUseDryUnprotectedGroundAndReserveTheirArrivals()
        {
            int count=0;var f=DrownedLedgerCompositionTests.Factory();
            foreach(int seed in new[]{1,2,3,4,5,64,1729,729490642})
            {
                var z=new OverworldZoneManager(f,seed).GetZone(Id);var p=DrownedLedgerCompositionPlan.Create(Id,seed);
                foreach(var e in z.GetAllEntities().Where(e=>e.HasPart<StairsDownPart>()))
                {
                    count++;var c=z.GetEntityCell(e);Assert.IsFalse(c.BlocksMovement());Assert.IsFalse(p.IsWet(c.X,c.Y));Assert.IsFalse(p.IsInterior(c.X,c.Y));Assert.IsFalse(p.IsReserved(c.X,c.Y));Assert.IsTrue(z.GenReservedCells.Contains((c.X,c.Y)));
                    foreach(var n in CinderholdCompositionTests.Neighbors(c.X,c.Y))if(z.InBounds(n.x,n.y)&&!z.GetCell(n.x,n.y).BlocksMovement()&&!p.IsWet(n.x,n.y))Assert.IsTrue(z.GenReservedCells.Contains(n));
                }
            }
            Assert.Greater(count,0,"Exercise the native roll rather than only a synthetic stair.");
        }
        [TestCase("WaterPuddle")] [TestCase("SandstoneWall")]
        public void InstalledCaveFilterRequiresDryClearArrivalNeighborsNotOnlyAnEmptyCentre(string blocker)
        {
            var f=DrownedLedgerCompositionTests.Factory();var m=new OverworldZoneManager(f,64);var pipeline=CinderholdCompositionTests.Pipeline(m,Id);
            var b=pipeline.Builders.OfType<DrownedLedgerCompositionBuilder>().Single();var entrance=pipeline.Builders.OfType<CaveEntranceBuilder>().Single();var z=new Zone(Id);
            Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.NotNull(entrance.PlacementFilter);
            Cell candidate=null;
            for(int y=2;y<23&&candidate==null;y++)for(int x=2;x<78&&candidate==null;x++)
            {
                bool clear=true;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    if(b.Plan.IsReserved(x+dx,y+dy)||b.Plan.IsWet(x+dx,y+dy)||b.Plan.IsInterior(x+dx,y+dy)||z.GetCell(x+dx,y+dy).BlocksMovement())clear=false;
                if(clear)candidate=z.GetCell(x,y);
            }
            Assert.NotNull(candidate);Assert.IsTrue(b.CanPlaceCaveEntrance(z,candidate));Assert.IsTrue(entrance.PlacementFilter(z,candidate));
            Assert.IsFalse(entrance.PlacementFilter(z,new Zone(Id).GetCell(candidate.X,candidate.Y)));
            var obstruction=f.CreateEntity(blocker);Assert.IsTrue(z.AddEntity(obstruction,candidate.X+1,candidate.Y));
            Assert.IsFalse(b.CanPlaceCaveEntrance(z,candidate));Assert.IsFalse(entrance.PlacementFilter(z,candidate));
            z.RemoveEntity(obstruction);Assert.IsTrue(entrance.PlacementFilter(z,candidate));
        }
        [Test] public void ArrivalDiagnosticsExposeWrongOwnerWithoutChangingItsGraph()
        {
            Diag.ResetAll();try
            {
                var f=DrownedLedgerCompositionTests.Factory();var z=new Zone(Id);var b=new DrownedLedgerCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
                var gate=new DrownedLedgerArrivalReservationBuilder(b);Assert.AreEqual(3870,gate.Priority);var old=z.GetAllEntities().ToArray();Assert.IsTrue(gate.BuildZone(z,f,new Random(1)));CollectionAssert.AreEquivalent(old,z.GetAllEntities());
                var foreign=new Zone(Id);foreign.GenReservedCells.Add((79,24));Assert.IsFalse(gate.BuildZone(foreign,f,new Random(1)));Assert.AreEqual(0,foreign.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},foreign.GenReservedCells);
                foreach(var name in new[]{"DrownedLedgerCompositionPlanned","DrownedLedgerArrivalsReserved","DrownedLedgerArrivalsRejected"})Assert.AreEqual(1,DiagQuery.Apply(new DiagQuery.Filter{Category="worldgen",Kind=name,Limit=10}).Records.Count,name);
            }finally{Diag.ResetAll();}
        }
        [TestCase("TentWall")] [TestCase("PeatBank")] [TestCase("Duckboard")]
        public void BreakingNativeMaterialsDoesNotSpawnWaterOrEraseWitnesses(string bp)
        {
            var f=DrownedLedgerCompositionTests.Factory();var z=new Zone(Id);var b=new DrownedLedgerCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsTrue(new DrownedLedgerProfileBuilder(b).BuildZone(z,f,new Random(1)));
            var e=z.GetAllEntities().First(o=>o.BlueprintName==bp);var c=z.GetEntityCell(e);var bodies=z.GetAllEntities().Where(o=>o.BlueprintName=="PreFellingBody").ToArray();
            Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(e,1000,null,z));Assert.IsFalse(c.Objects.Contains(e));Assert.IsFalse(c.Objects.Any(o=>o.HasPart<LiquidPoolPart>()));CollectionAssert.AreEquivalent(bodies,z.GetAllEntities().Where(o=>o.BlueprintName=="PreFellingBody"));
        }
        [Test] public void RemovingOneNativePoolClearsOnlyItsProjectionAndPreservesNeighborWater()
        {
            var f=DrownedLedgerCompositionTests.Factory();var z=new Zone(Id);var b=new DrownedLedgerCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
            var pools=z.GetAllEntities().Where(e=>e.BlueprintName=="WaterPuddle").Take(2).ToArray();Assert.AreEqual(2,pools.Length);var a=z.GetEntityPosition(pools[0]);var other=z.GetEntityPosition(pools[1]);
            Assert.IsFalse(pools[0].HasPart<TileStateSourcePart>());Assert.IsTrue(z.TileState.Get(a.x,a.y).Coatings.Any(c=>c.Id=="water"&&c.Turns==ZoneTileState.Permanent));
            z.RemoveEntity(pools[0]);Assert.IsFalse(z.TileState.Get(a.x,a.y)?.Coatings.Any(c=>c.Id=="water")==true);Assert.IsTrue(z.TileState.Get(other.x,other.y).Coatings.Any(c=>c.Id=="water"));
            Assert.IsTrue(z.GetCell(a.x,a.y).Objects.Any(e=>e.BlueprintName==b.Plan.GroundAt(a.x,a.y)));
        }
        // WI08 native camera review: standing bank faces occluded the witnesses,
        // aligned cuts repeated Sumphold, and long board routes dominated the camp.
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void ExcavationHeadsAreStaggeredBeyondOneRepeatedRow(int seed)
        {
            var p=DrownedLedgerCompositionPlan.Create(Id,seed);
            Assert.GreaterOrEqual(p.Bays.Max(b=>b.Y)-p.Bays.Min(b=>b.Y),4);
            Assert.GreaterOrEqual(p.Bays.Select(b=>b.Width*b.Height).Distinct().Count(),2);
        }
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void EachOutdoorWitnessHasThreeLowCellsBeforeThePeatFace(int seed)
        {
            var p=DrownedLedgerCompositionPlan.Create(Id,seed);var bodies=p.Profile.Where(o=>o.Blueprint=="PreFellingBody"&&!p.IsInterior(o.X,o.Y)).ToArray();Assert.AreEqual(2,bodies.Length);
            foreach(var body in bodies)for(int x=body.X-1;x<=body.X+1;x++)
            {Assert.AreNotEqual("PeatBank",p.ObjectAt(x,body.Y+1));Assert.AreNotEqual("DeadTree",p.ObjectAt(x,body.Y+1));Assert.IsFalse(p.IsWet(x,body.Y+1));Assert.IsTrue(p.IsReserved(x,body.Y+1));}
        }
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void BoardsStayOutOfTheMiddleCommonsAndMarkOnlyLocalWorkOrSouthernAccess(int seed)
        {
            var p=DrownedLedgerCompositionPlan.Create(Id,seed);int boards=0;
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)if(p.ObjectAt(x,y)=="Duckboard")
            {
                boards++;Assert.IsFalse(x>=35&&x<=45&&y>=10&&y<=14,"The central commons should not become a boardwalk crossing.");
                bool southern=x>=28&&x<=32&&y>=14;
                bool local=p.Bays.Any(b=>x>=b.X-2&&x<=b.X+b.Width+1&&y>=b.Y-3&&y<=b.Y-1);
                Assert.IsTrue(southern||local,"Board outside local work or southern access: "+x+","+y);Assert.IsFalse(p.IsWet(x,y));
            }
            Assert.GreaterOrEqual(boards,15);
        }
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void WetMarginColoniesUseSparseSnagsAndGroupedReedsWithClearTraffic(int seed)
        {
            var p=DrownedLedgerCompositionPlan.Create(Id,seed);var snags=new System.Collections.Generic.List<(int x,int y)>();
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)if(p.ObjectAt(x,y)=="DeadTree")snags.Add((x,y));
            Assert.That(snags.Count,Is.InRange(3,6));
            foreach(var s in snags)
            {
                int reeds=0;bool nearWater=false;
                for(int dy=-3;dy<=3;dy++)for(int dx=-3;dx<=3;dx++)
                {if(p.ObjectAt(s.x+dx,s.y+dy)=="Reeds")reeds++;if(p.IsWet(s.x+dx,s.y+dy))nearWater=true;}
                Assert.IsTrue(nearWater);Assert.GreaterOrEqual(reeds,4);Assert.IsFalse(p.IsWet(s.x,s.y));Assert.IsFalse(p.IsApproach(s.x,s.y));
            }
        }
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void ReadingRoomSuppliesClusterAroundTheActivityWithoutAddingAWitness(int seed)
        {
            var p=DrownedLedgerCompositionPlan.Create(Id,seed);var r=p.Rooms.Single(room=>room.Role=="ReadingTent");int crates=0;
            for(int y=r.Y+1;y<r.Y+r.Height-1;y++)for(int x=r.X+1;x<r.X+r.Width-1;x++)if(p.ObjectAt(x,y)=="Crate")
            {crates++;Assert.IsTrue(p.IsReserved(x,y));Assert.IsFalse(p.IsApproach(x,y));}
            Assert.That(crates,Is.InRange(3,4));Assert.AreEqual(3,p.Profile.Count(o=>o.Blueprint=="PreFellingBody"));
        }
    }
}
