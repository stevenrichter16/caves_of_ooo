using System;
using System.Linq;
using System.Collections.Generic;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class SumpholdCompositionTests
    {
        public const string Id="Overworld.15.6.0";
        [Test] public void ExactPlanIsDeterministicButSeedChangesTheWorkShore()
        {
            Assert.IsTrue(SumpholdCompositionPlan.IsSupportedZone(Id));
            Assert.AreEqual(SumpholdCompositionPlan.Create(Id,64).Signature(),SumpholdCompositionPlan.Create(Id,64).Signature());
            Assert.AreNotEqual(SumpholdCompositionPlan.Create(Id,64).Signature(),SumpholdCompositionPlan.Create(Id,1729).Signature());
            Assert.AreEqual(BiomeType.Spread,WorldMapAuthoring.BiomeAt(15,6));Assert.IsFalse(WorldMapAuthoring.IsRiver(15,6));
        }
        [TestCase(null)] [TestCase("")] [TestCase("Overworld.15.6.1")] [TestCase("Overworld.15.06.0")]
        [TestCase("Overworld.6.6.0")] [TestCase("Overworld.8.16.0")]
        public void RejectsForeignSitesAndDepths(string id)
        {Assert.IsFalse(SumpholdCompositionPlan.IsSupportedZone(id));Assert.Throws<ArgumentException>(()=>SumpholdCompositionPlan.Create(id,64));}
        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void WorkingFingersAndSheltersHaveRealWetCutsAndConnectedDryFrontages(int seed)
        {
            var z=new Zone(Id);var f=GrovelandsCompositionTests.Factory();var b=new SumpholdCompositionBuilder(seed);
            Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var p=b.Plan;
            Assert.That(p.Rooms.Count,Is.InRange(3,4));Assert.GreaterOrEqual(p.Rooms.Select(r=>r.Role).Distinct().Count(),3);
            Assert.GreaterOrEqual(p.WorkAreas.Count,3);int wet=0,boards=0,banks=0;
            var reached=DryReach(z,p);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
            {
                var c=z.GetCell(x,y);Assert.AreEqual(1,c.Objects.Count(e=>e.BlueprintName==p.GroundAt(x,y)));
                if(p.IsWet(x,y))
                {
                    wet++;Assert.IsTrue(c.Objects.Any(e=>e.HasPart<LiquidPoolPart>()));Assert.IsTrue(p.IsReserved(x,y));
                    Assert.IsFalse(p.IsInterior(x,y));Assert.IsFalse(p.IsApproach(x,y));
                }
                if(p.ObjectAt(x,y)=="Duckboard"){boards++;Assert.IsFalse(p.IsWet(x,y));Assert.IsFalse(c.Objects.Any(e=>e.HasPart<LiquidPoolPart>()));}
                if(p.ObjectAt(x,y)=="PeatBank")banks++;
                if(p.IsApproach(x,y)){Assert.IsTrue(reached.Contains((x,y)),"Dry approach "+x+","+y);Assert.IsTrue(z.GenReservedCells.Contains((x,y)));}
                if(p.IsInterior(x,y)){Assert.IsFalse(p.IsWet(x,y));Assert.AreEqual("StoneFloor",p.GroundAt(x,y));}
            }
            Assert.That(wet,Is.InRange(100,450));Assert.That(boards,Is.InRange(16,150));Assert.That(banks,Is.InRange(12,100));
            foreach(var r in p.Rooms)Assert.IsTrue(reached.Contains((r.DoorX,r.DoorY)),r.Role);
            foreach(var w in p.WorkAreas)
            {Assert.IsTrue(reached.Contains((w.X,w.Y)),w.Role);Assert.IsFalse(p.IsWet(w.X,w.Y));}
            Assert.IsTrue(reached.Contains((40,12)));Assert.IsFalse(p.IsInterior(40,12));
        }
        [Test] public void BoatyardProfileIsExactlyFiveRealOwnersAndCannotReplayOrCrossApply()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(Id);var b=new SumpholdCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
            var late=new SumpholdProfileBuilder(b);Assert.AreEqual(3860,late.Priority);Assert.IsFalse(late.BuildZone(new Zone(Id),f,new Random(1)));
            Assert.IsTrue(late.BuildZone(z,f,new Random(1)));
            Assert.AreEqual(2,z.GetAllEntities().Count(e=>e.BlueprintName=="BoatFrame"));Assert.AreEqual(2,z.GetAllEntities().Count(e=>e.BlueprintName=="PeatCutter"));Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName=="TollRolls"));
            var before=z.GetAllEntities().ToArray();Assert.IsFalse(late.BuildZone(z,f,new Random(1)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
            foreach(var owner in z.GetAllEntities().Where(e=>e.BlueprintName=="BoatFrame"||e.BlueprintName=="TollRolls"))
            {Assert.IsNull(owner.GetPart<DestructiblePart>(),"Preserve actual descriptive-fixture policy.");Assert.IsTrue(owner.HasPart<ExaminablePart>());}
            foreach(var cutter in z.GetAllEntities().Where(e=>e.BlueprintName=="PeatCutter"))Assert.AreEqual("PeatCutter_1",cutter.GetPart<ConversationPart>().ConversationID);
        }
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void FinalManagerKeepsServicesAndAllDryFrontages(int seed)
        {
            var z=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),seed).GetZone(Id);var p=SumpholdCompositionPlan.Create(Id,seed);
            foreach(var bp in new[]{"Merchant","Quartermaster","Elder","TollRolls"})Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName==bp),bp);
            Assert.AreEqual(2,z.GetAllEntities().Count(e=>e.BlueprintName=="BoatFrame"));Assert.AreEqual(2,z.GetAllEntities().Count(e=>e.BlueprintName=="PeatCutter"));
            Assert.IsTrue(z.GetCell(40,12).Objects.Any(e=>e.BlueprintName=="Well"));
            var frontages=z.GetAllEntities().Where(e=>new[]{"Merchant","Quartermaster","Well","BoatFrame","TollRolls","PeatCutter","Shrine"}.Contains(e.BlueprintName)).Select(e=>z.GetEntityPosition(e)).ToArray();
            foreach(var e in z.GetAllEntities().Where(e=>e.HasPart<BrainPart>()||e.HasTag("Creature")).ToArray())z.RemoveEntity(e);
            var reach=DryReach(z,p);
            foreach(var r in p.Rooms)Assert.IsTrue(reach.Contains((r.DoorX,r.DoorY)));
            foreach(var c in frontages)Assert.IsTrue(new[]{(-1,0),(1,0),(0,-1),(0,1)}.Any(d=>reach.Contains((c.x+d.Item1,c.y+d.Item2))),"No dry service frontage "+c);
        }
        public static HashSet<(int,int)> DryReach(Zone z,SumpholdCompositionPlan p)
        {
            var seen=new HashSet<(int,int)>();var q=new Queue<(int,int)>();q.Enqueue((0,12));seen.Add((0,12));
            while(q.Count>0){var c=q.Dequeue();foreach(var d in new[]{(-1,0),(1,0),(0,-1),(0,1)}){var n=(c.Item1+d.Item1,c.Item2+d.Item2);if(!z.InBounds(n.Item1,n.Item2)||p.IsWet(n.Item1,n.Item2)||z.GetCell(n.Item1,n.Item2).BlocksMovement()||!seen.Add(n))continue;q.Enqueue(n);}}
            return seen;
        }
    }
}
