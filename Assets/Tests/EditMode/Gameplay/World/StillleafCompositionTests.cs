using System;
using System.Linq;
using System.Collections.Generic;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class StillleafCompositionTests
    {
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void ExactDepthPlansAreDeterministicButRespondToSeed(int depth)
        {
            string id="Overworld.2.4."+depth;
            Assert.IsTrue(StillleafCompositionPlan.IsSupportedZone(id));
            var p=StillleafCompositionPlan.Create(id,64);Assert.AreEqual(depth,p.Depth);
            Assert.AreEqual(p.Signature(),StillleafCompositionPlan.Create(id,64).Signature());
            Assert.AreNotEqual(p.Signature(),StillleafCompositionPlan.Create(id,1729).Signature());
        }
        [TestCase(null)] [TestCase("")] [TestCase("Overworld.2.4.3")]
        [TestCase("Overworld.2.4.02")] [TestCase("Overworld.5.4.2")]
        [TestCase("Overworld.2.7.2")] [TestCase(" Overworld.2.4.0")]
        public void ScopeRejectsOtherAreasAndAliases(string id)
        {
            Assert.IsFalse(StillleafCompositionPlan.IsSupportedZone(id));
            Assert.Throws<ArgumentException>(()=>StillleafCompositionPlan.Create(id,64));
        }
        [TestCase(0,"TepuiWall")] [TestCase(1,"SandstoneWall")] [TestCase(2,"SandstoneWall")]
        public void ComposedNativeGroundAndMassesRetainConnectedReservedApproaches(int depth,string wall)
        {
            var f=GrovelandsCompositionTests.Factory();
            foreach(int seed in new[]{64,1729,int.MinValue,int.MaxValue})
            {
                var z=new Zone("Overworld.2.4."+depth);var b=new StillleafCompositionBuilder(seed);
                Assert.AreEqual(1000,b.Priority);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
                var p=StillleafCompositionPlan.Create(z.ZoneID,seed);int thick=0,approaches=0;
                for(int y=0;y<25;y++)for(int x=0;x<80;x++)
                {
                    var c=z.GetCell(x,y);Assert.NotNull(p.GroundAt(x,y));
                    Assert.AreEqual(1,c.Objects.Count(e=>e.BlueprintName==p.GroundAt(x,y)),"ground "+x+","+y);
                    Assert.IsFalse(c.Objects.GroupBy(e=>e.BlueprintName).Any(g=>g.Count()>1));
                    if(p.ObjectAt(x,y)==wall&&p.ObjectAt(x+1,y)==wall&&p.ObjectAt(x,y+1)==wall&&p.ObjectAt(x+1,y+1)==wall)thick++;
                    if(!p.IsApproach(x,y))continue;approaches++;
                    Assert.IsFalse(c.BlocksMovement());Assert.IsTrue(z.GenReservedCells.Contains((x,y)));
                }
                Assert.Greater(thick,20,"Use coherent cliff masses rather than only rails.");
                Assert.Greater(approaches,100);Assert.IsTrue(FormationReachability.FullyReached(z,FormationReachability.FloodFromWest(z)));
            }
        }
        [Test] public void DescentShelvesAttachToCliffsAndHoldNativeExpeditionSupplies()
        {
            var f=GrovelandsCompositionTests.Factory();
            foreach(int seed in new[]{64,1729,729490642})
            {
                var p=StillleafCompositionPlan.Create("Overworld.2.4.1",seed);
                var seen=new HashSet<(int,int)>();int groups=0;
                for(int y=0;y<25;y++)for(int x=0;x<80;x++)
                {
                    if(p.ObjectAt(x,y)!="DescentLedge"||seen.Contains((x,y)))continue;
                    var q=new Queue<(int,int)>();q.Enqueue((x,y));bool anchored=false;groups++;
                    while(q.Count>0)
                    {
                        var c=q.Dequeue();if(!seen.Add(c))continue;
                        foreach(var n in new[]{(c.Item1-1,c.Item2),(c.Item1+1,c.Item2),(c.Item1,c.Item2-1),(c.Item1,c.Item2+1)})
                        {var bp=p.ObjectAt(n.Item1,n.Item2);if(bp=="SandstoneWall")anchored=true;
                            if(bp=="DescentLedge"&&!seen.Contains(n))q.Enqueue(n);}
                    }
                    Assert.IsTrue(anchored,"Shelves meet native cliff faces, seed "+seed);
                }
                Assert.GreaterOrEqual(groups,3);
                var z=new Zone("Overworld.2.4.1");Assert.IsTrue(new StillleafCompositionBuilder(seed).BuildZone(z,f,new Random(1)));
                Assert.AreEqual(3,z.GetAllEntities().Count(e=>e.BlueprintName=="RopeAnchor"));
                var sack=z.GetAllEntities().Single(e=>e.BlueprintName=="Sack");
                CollectionAssert.AreEquivalent(new[]{"Torch","DriedMeat","HealingTonic"},sack.GetPart<ContainerPart>().Contents.Select(e=>e.BlueprintName));
                Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName=="Bones"));
            }
        }
        [Test] public void OvergrownMouthHasThreeQuietNativeBushColoniesOutsideItsStoneLanding()
        {
            foreach(int seed in new[]{64,1729,729490642})
            {
                var p=StillleafCompositionPlan.Create("Overworld.2.4.0",seed);int count=0;
                for(int y=0;y<25;y++)for(int x=0;x<80;x++)if(p.ObjectAt(x,y)=="Bush")
                {
                    count++;double d=(x-40)*(x-40)/169.0+(y-12)*(y-12)/36.0;
                    Assert.Greater(d,1.0,"The mouth landing and lip remain native stone.");
                    Assert.IsFalse(p.IsApproach(x,y));
                }
                Assert.That(count,Is.InRange(24,110));
                var colonies=Groups(p,"Bush");Assert.That(colonies.Count,Is.InRange(2,4));
                Assert.IsTrue(colonies.All(c=>c.Count>=6),"Growth is grouped, not isolated voxel confetti.");
            }
        }
        [Test] public void ArchiveDescentHasThreeUnequalShelvesIncludingABroadLowerCacheLanding()
        {
            foreach(int seed in new[]{64,1729,729490642})
            {
                var p=StillleafCompositionPlan.Create("Overworld.2.4.1",seed);var groups=Groups(p,"DescentLedge");
                Assert.AreEqual(3,groups.Count,"An asymmetric archive descent differs from the four equal Cathedral terraces.");
                Assert.AreEqual(3,groups.Select(c=>c.Count).Distinct().Count());
                Assert.GreaterOrEqual(groups.Max(c=>c.Max(v=>v.y)-c.Min(v=>v.y)+1),5);
                Assert.AreEqual(2,groups.Count(c=>c.Average(v=>v.x)<40));
                Assert.AreEqual(1,groups.Count(c=>c.Average(v=>v.x)>40));
                var cache=Enumerable.Range(0,25).SelectMany(y=>Enumerable.Range(0,80).Select(x=>(x,y)))
                    .Single(v=>p.ObjectAt(v.x,v.y)=="Sack");
                Assert.GreaterOrEqual(cache.y,15);Assert.Less(cache.x,40);
            }
        }
        [Test] public void OuterArchiveHasTwoBroadAttachedRockProjectionsBeyondTheVaultApproach()
        {
            foreach(int seed in new[]{64,1729,729490642})
            {
                var p=StillleafCompositionPlan.Create("Overworld.2.4.2",seed);int north=0,south=0;
                for(int x=28;x<74;x++)
                {
                    for(int y=6;y<=9;y++)if(p.ObjectAt(x,y)=="SandstoneWall")north++;
                    for(int y=16;y<=19;y++)if(p.ObjectAt(x,y)=="SandstoneWall")south++;
                }
                Assert.GreaterOrEqual(north,16);Assert.GreaterOrEqual(south,16);
                foreach(var group in Groups(p,"SandstoneWall"))
                    Assert.IsTrue(group.Any(c=>c.x==0||c.x==79||c.y==0||c.y==24),"Rock grows from chamber sides rather than isolated obstacles.");
            }
        }
        private static List<List<(int x,int y)>> Groups(StillleafCompositionPlan p,string bp)
        {
            var groups=new List<List<(int x,int y)>>();var seen=new HashSet<(int x,int y)>();
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
            {
                if(p.ObjectAt(x,y)!=bp||seen.Contains((x,y)))continue;
                var group=new List<(int x,int y)>();var q=new Queue<(int x,int y)>();q.Enqueue((x,y));
                while(q.Count>0)
                {
                    var c=q.Dequeue();if(!seen.Add(c))continue;group.Add(c);
                    foreach(var n in new[]{(c.x-1,c.y),(c.x+1,c.y),(c.x,c.y-1),(c.x,c.y+1)})
                        if(p.ObjectAt(n.Item1,n.Item2)==bp&&!seen.Contains(n))q.Enqueue(n);
                }
                groups.Add(group);
            }
            return groups;
        }
        [Test] public void FloorCompositionLeavesSealedArchiveToTheNativeLateStamp()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.2.4.2");
            Assert.IsTrue(new StillleafCompositionBuilder(64).BuildZone(z,f,new Random(1)));
            Assert.IsFalse(z.GetAllEntities().Any(e=>e.HasPart<SealedLibraryBarrierPart>()));
            var up=f.CreateEntity("StairsUp");Assert.IsTrue(z.AddEntity(up,12,12));
            var down=f.CreateEntity("StairsDown");Assert.IsTrue(z.AddEntity(down,65,12));
            Assert.IsTrue(new SealedLibraryBuilder().BuildZone(z,f,new Random(1)));
            Assert.AreSame(up,z.GetCell(12,12).Objects.Single(e=>e.HasPart<StairsUpPart>()));
            Assert.AreSame(down,z.GetCell(65,12).Objects.Single(e=>e.HasPart<StairsDownPart>()));
            Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName=="SealedLibraryDoor"));
            Assert.AreEqual(12,z.GetAllEntities().Count(e=>e.BlueprintName=="SealedArchiveShelf"));
        }
        [Test] public void ExistingOwnersAndReservationsAreNotErasedByRebuilding()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.2.4.0");
            var e=f.CreateEntity("TepuiWall");Assert.IsTrue(z.AddEntity(e,4,4));z.GenReservedCells.Add((4,4));
            Assert.IsFalse(new StillleafCompositionBuilder(64).BuildZone(z,f,new Random(1)));
            CollectionAssert.AreEquivalent(new[]{e},z.GetAllEntities());Assert.IsTrue(z.GenReservedCells.Contains((4,4)));
        }
    }
}
