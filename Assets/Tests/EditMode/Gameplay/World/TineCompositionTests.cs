using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class TineCompositionTests
    {
        public const string Id="Overworld.13.7.0";
        public static EntityFactory Factory()=>GrovelandsCompositionTests.Factory();
        [SetUp] public void LoadLoot()=>CinderholdCompositionTests.LoadLoot();
        [TearDown] public void ResetLoot()=>LootTableRegistry.ResetForTests();
        [Test] public void ExactLocalLakeKeepsMappedRoadButDoesNotPretendToBeAMappedRiver()
        {
            Assert.AreEqual(Id,TineCompositionPlan.ZoneID);Assert.AreEqual("LakesideVillage",TineCompositionPlan.ProfileID);
            Assert.IsTrue(TineCompositionPlan.IsSupportedZone(Id));
            Assert.AreEqual(BiomeType.Spread,WorldMapAuthoring.BiomeAt(13,7));Assert.AreEqual(1,WorldMapAuthoring.TierAt(13,7));Assert.IsTrue(WorldMapAuthoring.IsRoad(13,7));Assert.IsFalse(WorldMapAuthoring.IsRiver(13,7));
            Assert.AreEqual(TineCompositionPlan.Create(Id,64).Signature(),TineCompositionPlan.Create(Id,64).Signature());
            Assert.AreNotEqual(TineCompositionPlan.Create(Id,64).Signature(),TineCompositionPlan.Create(Id,1729).Signature());
        }
        [TestCase(null)] [TestCase("")] [TestCase("Overworld.13.7.1")] [TestCase("Overworld.13.07.0")] [TestCase("Overworld.13.7.00")]
        [TestCase(" Overworld.13.7.0")] [TestCase("Overworld.13.7.0 ")] [TestCase("overworld.13.7.0")] [TestCase("Overworld.15.6.0")]
        public void ScopeDoesNotLeakIntoAnotherVillageDepthOrMalformedAddress(string id)
        {Assert.IsFalse(TineCompositionPlan.IsSupportedZone(id));Assert.Throws<ArgumentException>(()=>TineCompositionPlan.Create(id,64));}
        [Test] public void ThreeFormationsChangeLakeSideAndRetreatRelationshipsRatherThanOnlyJitter()
        {
            var names=new HashSet<string>();var relationships=new HashSet<string>();
            for(int seed=0;seed<64;seed++)
            {
                var p=TineCompositionPlan.Create(Id,seed);names.Add(p.FormationName);
                var wet=new List<(int x,int y)>();for(int y=0;y<25;y++)for(int x=0;x<80;x++)if(p.IsWet(x,y))wet.Add((x,y));
                var retreat=p.Rooms.Single(r=>r.Role=="ScribeRetreat");
                relationships.Add((wet.Average(c=>c.x)>55?"east":wet.Average(c=>c.y)>14?"south":"north")+":"+(retreat.Y>12?"lower":"upper"));
            }
            CollectionAssert.AreEquivalent(new[]{"SouthernReach","EasternCove","NorthernInlet"},names);Assert.AreEqual(3,relationships.Count);
        }
        [TestCase(1)] [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void BaseRealizesConnectedDryWorkingFingersAroundRealWater(int seed)
        {
            var z=new Zone(Id);var b=new TineCompositionBuilder(seed);Assert.AreEqual(1000,b.Priority);Assert.IsTrue(b.BuildZone(z,Factory(),new Random(1)));var p=b.Plan;Assert.AreSame(z,b.RealizedZone);
            CollectionAssert.AreEquivalent(new[]{"ScribeRetreat","ShoreStore","LakeHouse"},p.Rooms.Select(r=>r.Role));Assert.AreEqual(2,p.Profile.Count);Assert.IsTrue(p.Profile.All(o=>o.Blueprint=="BoatFrame"));
            var reached=DryReach(z);AssertFourDirections(reached);int wet=0,boards=0;
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
            {
                var c=z.GetCell(x,y);Assert.AreEqual(1,c.Objects.Count(e=>e.BlueprintName==p.GroundAt(x,y)));
                if(p.IsWet(x,y)){wet++;Assert.IsTrue(c.Objects.Any(e=>e.GetPart<LiquidPoolPart>()?.LiquidId=="water"));Assert.IsTrue(p.IsReserved(x,y));Assert.IsFalse(p.IsInterior(x,y));Assert.IsFalse(p.IsApproach(x,y));}
                if(p.ObjectAt(x,y)=="Duckboard"){boards++;Assert.IsFalse(p.IsWet(x,y));Assert.IsFalse(c.Objects.Any(e=>e.HasPart<LiquidPoolPart>()));Assert.IsTrue(reached.Contains((x,y)));}
                if(p.IsInterior(x,y)){Assert.AreEqual("StoneFloor",p.GroundAt(x,y));Assert.IsFalse(p.IsWet(x,y));}
                if(p.IsApproach(x,y)){Assert.IsTrue(reached.Contains((x,y)),x+","+y);Assert.IsTrue(p.IsReserved(x,y));}
                if(!c.BlocksMovement()&&!c.Objects.Any(e=>e.HasPart<LiquidPoolPart>()))Assert.IsTrue(reached.Contains((x,y)),"Unreachable dry pocket "+x+","+y);
            }
            Assert.That(wet,Is.InRange(180,650));Assert.That(boards,Is.InRange(20,120));Assert.IsTrue(reached.Contains((40,12)));Assert.IsTrue(p.IsReserved(40,12));
            foreach(var room in p.Rooms)Assert.IsTrue(reached.Contains((room.DoorX,room.DoorY)),room.Role);
            Assert.IsTrue(p.IsInterior(p.ScribeX,p.ScribeY));Assert.IsTrue(reached.Contains((p.ScribeX,p.ScribeY)));
            Assert.AreEqual(0,z.GetAllEntities().Count(e=>e.BlueprintName=="BoatFrame"));
        }
        [Test] public void TwoFrameOwnersPublishOnceWithoutAWorkingBoatOrFishingMechanic()
        {
            var z=new Zone(Id);var f=Factory();var b=new TineCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var late=new TineProfileBuilder(b);Assert.AreEqual(3860,late.Priority);Assert.IsTrue(late.BuildZone(z,f,new Random(1)));
            var frames=z.GetAllEntities().Where(e=>e.BlueprintName=="BoatFrame").ToArray();Assert.AreEqual(2,frames.Length);
            foreach(var e in frames){Assert.IsTrue(e.GetPart<PhysicsPart>().Solid);Assert.IsFalse(e.GetPart<PhysicsPart>().Takeable);Assert.IsFalse(e.HasPart<ContainerPart>());Assert.IsFalse(e.HasPart<DestructiblePart>());Assert.IsTrue(e.HasPart<ExaminablePart>());Assert.IsFalse(e.HasPart<BrainPart>());}
            var before=z.GetAllEntities().ToArray();Assert.IsFalse(late.BuildZone(z,f,new Random(2)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
        }
        [TestCase(1)] [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void FinalNativePipelineKeepsOneRealCopyServiceAndAllDryArrivals(int seed)
        {
            var m=new OverworldZoneManager(Factory(),seed);var pipeline=CinderholdCompositionTests.Pipeline(m,Id);
            Assert.AreEqual(1,pipeline.Builders.OfType<TineCompositionBuilder>().Count());Assert.AreEqual(1,pipeline.Builders.OfType<TineProfileBuilder>().Count());Assert.AreEqual(1,pipeline.Builders.OfType<TineArrivalReservationBuilder>().Count());Assert.IsFalse(pipeline.Builders.OfType<RiverChunkBuilder>().Any());
            var z=m.GetZone(Id);Assert.AreEqual(2,z.GetAllEntities().Count(e=>e.BlueprintName=="BoatFrame"));
            foreach(var bp in new[]{"Scribe","Merchant","Quartermaster","Innkeeper","Elder"})Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName==bp),bp);
            var scribe=z.GetAllEntities().Single(e=>e.BlueprintName=="Scribe");var plan=TineCompositionPlan.Create(Id,seed);Assert.IsTrue(plan.TryGetServiceCell("Scribe",out int sx,out int sy));Assert.AreEqual((sx,sy),z.GetEntityPosition(scribe));Assert.AreEqual("Scribe_1",scribe.GetPart<ConversationPart>().ConversationID);Assert.IsTrue(scribe.HasPart<TraderPart>());Assert.IsTrue(scribe.GetPart<InventoryPart>().Objects.Any(e=>e.BlueprintName=="InkVial"));
            var frontages=z.GetAllEntities().Where(e=>new[]{"Scribe","Merchant","Quartermaster","Well","BoatFrame","Shrine"}.Contains(e.BlueprintName)).Select(e=>z.GetEntityPosition(e)).ToArray();
            foreach(var e in z.GetAllEntities().Where(e=>e.HasTag("Creature")||e.HasPart<BrainPart>()).ToArray())z.RemoveEntity(e);
            var reached=DryReach(z);AssertFourDirections(reached);
            foreach(var p in frontages)Assert.IsTrue(new[]{(-1,0),(1,0),(0,-1),(0,1)}.Any(d=>reached.Contains((p.x+d.Item1,p.y+d.Item2))),"No dry frontage "+p);
        }
        public static HashSet<(int,int)> DryReach(Zone z)
        {
            var q=new Queue<(int x,int y)>();var seen=new HashSet<(int,int)>();q.Enqueue((0,12));seen.Add((0,12));
            while(q.Count>0){var c=q.Dequeue();foreach(var d in new[]{(-1,0),(1,0),(0,-1),(0,1)}){var n=(x:c.x+d.Item1,y:c.y+d.Item2);if(!z.InBounds(n.x,n.y)||seen.Contains(n)||z.GetCell(n.x,n.y).BlocksMovement()||z.GetCell(n.x,n.y).Objects.Any(e=>e.HasPart<LiquidPoolPart>()))continue;seen.Add(n);q.Enqueue(n);}}return seen;
        }
        public static void AssertFourDirections(HashSet<(int,int)> reached)
        {Assert.IsTrue(reached.Any(p=>p.Item1==0));Assert.IsTrue(reached.Any(p=>p.Item1==79));Assert.IsTrue(reached.Any(p=>p.Item2==0));Assert.IsTrue(reached.Any(p=>p.Item2==24));}
    }
}
