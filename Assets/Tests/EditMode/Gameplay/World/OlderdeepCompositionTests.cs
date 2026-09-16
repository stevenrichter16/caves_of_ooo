using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Native founding journey, not an additional god encounter.
    /// These specifications precede the implementation and preserve the old
    /// chamber stamp's authority over its body, people and eleven plume cells.</summary>
    public class OlderdeepCompositionTests
    {
        public static string Id(int depth)=>"Overworld.4.6."+depth;
        public static IEnumerable<(int x,int y)> Neighbors(int x,int y)
        {yield return(x-1,y);yield return(x+1,y);yield return(x,y-1);yield return(x,y+1);}
        public static void RemoveMobileActors(Zone z)
        {foreach(var e in z.GetAllEntities().Where(e=>e.HasPart<BrainPart>()||e.HasTag("Creature")).ToArray())z.RemoveEntity(e);}
        public static bool[,] Flood(Zone z)
        {
            // The older formation helper deliberately excludes the border and
            // seeds every open western cell. A four-edge gate needs one actual
            // seam origin and cardinal edges, including all border cells.
            var seen=new bool[80,25];var q=new Queue<(int x,int y)>();
            for(int y=0;y<25;y++)if(!z.GetCell(0,y).BlocksMovement()){q.Enqueue((0,y));seen[0,y]=true;break;}
            while(q.Count>0){var c=q.Dequeue();foreach(var n in Neighbors(c.x,c.y))
                if(z.InBounds(n.x,n.y)&&!seen[n.x,n.y]&&!z.GetCell(n.x,n.y).BlocksMovement()){seen[n.x,n.y]=true;q.Enqueue(n);}}
            return seen;
        }
        public static void AssertGap(Zone z)
        {
            var body=z.GetAllEntities().Single(e=>e.BlueprintName=="TheRooted");var c=z.GetEntityCell(body);
            Assert.AreEqual(58,c.X);Assert.IsFalse(body.HasTag("Creature"));Assert.IsNull(body.GetPart<BrainPart>());
            Assert.IsTrue(body.GetPart<DestructiblePart>().Indestructible);
            for(int x=59;x<=64;x++)for(int y=c.Y-1;y<=c.Y+1;y++)
            {
                CollectionAssert.AreEquivalent(new[]{"StoneFloor"},z.GetCell(x,y).Objects.Select(e=>e.BlueprintName),"Empty embrace "+x+","+y);
                Assert.IsFalse(z.GetCell(x,y).BlocksMovement());Assert.IsTrue(z.GenReservedCells.Contains((x,y)));
            }
            for(int y=c.Y-1;y<=c.Y+1;y++)
            {Assert.IsTrue(z.GetCell(65,y).BlocksMovement());Assert.IsTrue(z.GetCell(65,y).Objects.Any(e=>e.BlueprintName=="SandstoneWall"));}
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void ThreeDistinctLevelsHaveRepeatableButSeedVariableNativePlans(int depth)
        {
            Assert.IsTrue(OlderdeepCompositionPlan.IsSupportedZone(Id(depth)));
            var p=OlderdeepCompositionPlan.Create(Id(depth),64);Assert.AreEqual(depth,p.Depth);
            Assert.AreEqual(p.Signature(),OlderdeepCompositionPlan.Create(Id(depth),64).Signature());
            Assert.AreNotEqual(p.Signature(),OlderdeepCompositionPlan.Create(Id(depth),1729).Signature());
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)Assert.IsNotNull(p.GroundAt(x,y));
        }

        [TestCase(null)] [TestCase("")] [TestCase("Overworld.4.6.3")]
        [TestCase("Overworld.4.6.-1")] [TestCase("Overworld.4.6.02")]
        [TestCase("Overworld.04.6.0")] [TestCase("Overworld.4.6.0.extra")]
        [TestCase("Overworld.5.4.2")] [TestCase("Overworld.12.3.2")]
        [TestCase("Overworld.16.4.2")] [TestCase(" Overworld.4.6.0")]
        public void OtherPlacesAndMalformedAddressesDoNotAcquireFoundingAuthority(string id)
        {
            Assert.IsFalse(OlderdeepCompositionPlan.IsSupportedZone(id));
            Assert.Throws<ArgumentException>(()=>OlderdeepCompositionPlan.Create(id,64));
            if(id==null)return;
            var z=new Zone(id);z.GenReservedCells.Add((79,24));
            Assert.IsFalse(new OlderdeepCompositionBuilder(64).BuildZone(z,GrovelandsCompositionTests.Factory(),new Random(1)));
            Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
        }

        [Test] public void GreenMouthFramesTheNativeStoneArrivalWithoutPrematureSacredOwners()
        {
            var z=new Zone(Id(0));var b=new OlderdeepCompositionBuilder(64);
            Assert.IsTrue(b.BuildZone(z,GrovelandsCompositionTests.Factory(),new Random(1)));
            Assert.AreEqual("Grass",b.Plan.GroundAt(5,12));Assert.AreEqual("StoneFloor",b.Plan.GroundAt(40,12));
            Assert.That(z.GetAllEntities().Count(e=>e.BlueprintName=="Tree"),Is.InRange(8,160));
            Assert.Greater(z.GetAllEntities().Count(e=>e.BlueprintName=="Bush"),0);
            Assert.IsTrue(z.GetCell(5,12).Objects.Any(e=>e.HasTag("Plantable")));
            Assert.IsFalse(z.GetAllEntities().Any(e=>e.BlueprintName=="TheRooted"||e.BlueprintName=="FoundingPlume"||e.BlueprintName=="FoundingListener"));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void FreshBaseHasUniqueOwnersClearReservedApproachesAndSpaceForLaterNativePasses(int depth)
        {
            foreach(int seed in new[]{0,64,1729,int.MinValue,int.MaxValue})
            {
                var z=new Zone(Id(depth));var b=new OlderdeepCompositionBuilder(seed);
                Assert.IsTrue(b.BuildZone(z,GrovelandsCompositionTests.Factory(),new Random(777)));Assert.AreEqual(1000,b.Priority);
                Assert.NotNull(b.Plan);int free=0;
                for(int y=0;y<25;y++)for(int x=0;x<80;x++)
                {
                    var c=z.GetCell(x,y);Assert.IsFalse(c.Objects.GroupBy(e=>e.BlueprintName).Any(g=>g.Count()>1));
                    if(!c.BlocksMovement()&&!z.GenReservedCells.Contains((x,y)))free++;
                    if(!b.Plan.IsApproach(x,y))continue;
                    Assert.IsFalse(c.BlocksMovement(),"Blocked base route "+x+","+y);Assert.IsTrue(z.GenReservedCells.Contains((x,y)));
                }
                Assert.Greater(free,60,"Native population and hazards still need space.");
            }
        }

        // Broad stopping courts distinguish a used descent from Cathedral's
        // thin shelf bars. Physical floor and ledge owners must agree; models
        // cannot fake a cliff support or a supply cache.
        [Test] public void UsedDescentHasBroadStoneAnchoredStoppingPlacesAndRealSupplies()
        {
            foreach(int seed in new[]{64,1729,729490642})
            {
                var z=new Zone(Id(1));var b=new OlderdeepCompositionBuilder(seed);
                Assert.IsTrue(b.BuildZone(z,GrovelandsCompositionTests.Factory(),new Random(1)));
                var seen=new HashSet<(int,int)>();int groups=0;
                for(int y=0;y<25;y++)for(int x=0;x<80;x++)
                {
                    if(b.Plan.ObjectAt(x,y)!="DescentLedge"||seen.Contains((x,y)))continue;
                    var q=new Queue<(int x,int y)>();q.Enqueue((x,y));int area=0,minX=x,maxX=x,minY=y,maxY=y;bool anchored=false;
                    while(q.Count>0)
                    {
                        var c=q.Dequeue();if(!seen.Add(c))continue;area++;minX=Math.Min(minX,c.x);maxX=Math.Max(maxX,c.x);minY=Math.Min(minY,c.y);maxY=Math.Max(maxY,c.y);
                        foreach(var n in Neighbors(c.x,c.y))
                        {var bp=b.Plan.ObjectAt(n.x,n.y);if(bp=="SandstoneWall")anchored=true;if(bp=="DescentLedge"&&!seen.Contains(n))q.Enqueue(n);}
                    }
                    groups++;Assert.IsTrue(anchored,"Stopping court has no physical shoulder.");
                    Assert.GreaterOrEqual(area,20);Assert.GreaterOrEqual(maxX-minX+1,6);Assert.GreaterOrEqual(maxY-minY+1,4);
                }
                Assert.That(groups,Is.InRange(2,3));Assert.AreEqual(3,z.GetAllEntities().Count(e=>e.BlueprintName=="RopeAnchor"));
                Assert.GreaterOrEqual(z.GetAllEntities().Count(e=>e.BlueprintName=="BeetleJar"),2);
                var cache=z.GetAllEntities().Single(e=>e.BlueprintName=="Sack");
                CollectionAssert.AreEquivalent(new[]{"Torch","DriedMeat","HealingTonic"},cache.GetPart<ContainerPart>().Contents.Select(e=>e.BlueprintName));
                var cc=z.GetEntityCell(cache);Assert.IsTrue(Neighbors(cc.X,cc.Y).Any(n=>z.GetCell(n.x,n.y)?.Objects.Any(e=>e.BlueprintName=="Bones")==true));
            }
        }

        [Test] public void BaseDoesNotDuplicateOrMoveTheUnchangedFoundingStamp()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(Id(2));
            Assert.IsTrue(new OlderdeepCompositionBuilder(64).BuildZone(z,f,new Random(1)));
            Assert.IsFalse(z.GetAllEntities().Any(e=>e.BlueprintName=="TheRooted"||e.BlueprintName=="FoundingPlume"||e.BlueprintName=="FoundingListener"||e.BlueprintName=="FoundingPlaqueTender"));
            Assert.IsTrue(new FoundingVillageBuilder().BuildZone(z,f,new Random(64)));AssertGap(z);
            Assert.AreEqual(11,z.GetAllEntities().Count(e=>e.BlueprintName=="FoundingPlume"));
            Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName=="FoundingListener"));Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName=="FoundingPlaqueTender"));
            Assert.AreEqual(0,z.GetAllEntities().Count(e=>e.BlueprintName=="HearthPatch"));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void ActualRoutingKeepsNativeSemanticStagesAndReplacesOnlyTheOldBase(int depth)
        {
            var m=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),64);
            var p=CathedralCompositionTests.Pipeline(m,Id(depth));
            Assert.AreEqual(1,p.Builders.Count(b=>b is OlderdeepCompositionBuilder));
            Assert.AreEqual(depth!=2,p.Builders.Any(b=>b is ConnectivityBuilder),"The founding floor owns its four entries; generic random edge tunnels are later sealed by its native shell.");
            Assert.IsTrue(p.Builders.Any(b=>b is HazardTerrainBuilder));Assert.IsTrue(p.Builders.Any(b=>b is PopulationBuilder));Assert.IsTrue(p.Builders.Any(b=>b is ContainerBuilder));
            Assert.IsTrue(p.Builders.Any(b=>b is OlderdeepArrivalReservationBuilder));
            Assert.AreEqual(depth==0,p.Builders.Any(b=>b is SinkholeMouthBuilder));
            Assert.IsFalse(p.Builders.Any(b=>b is SinkholeDescentBuilder));
            Assert.IsFalse(CathedralCompositionTests.Pipeline(m,Id(3)).Builders.Any(b=>b is OlderdeepCompositionBuilder));
            Assert.IsFalse(CathedralCompositionTests.Pipeline(m,"Overworld.12.3.2").Builders.Any(b=>b is OlderdeepCompositionBuilder));
        }

        // An earlier open base is not proof of connectivity: the later native
        // founding oval can seal every surrounding pocket. Exercise the whole
        // pipeline, retain the physical body/wall, and strip only mobile actors.
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void FinalNativePipelineHasAllFourEntriesAndNoUnreachableOpenPockets(int seed)
        {
            var m=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),seed);
            for(int depth=0;depth<3;depth++)
            {
                var z=m.GetZone(Id(depth));if(depth==2)AssertGap(z);
                foreach(var stair in z.GetAllEntities().Where(e=>e.HasPart<StairsDownPart>()||e.HasPart<StairsUpPart>()))
                {var c=z.GetEntityCell(stair);Assert.IsFalse(c.BlocksMovement());Assert.IsTrue(z.GenReservedCells.Contains((c.X,c.Y)));}
                RemoveMobileActors(z);var reached=Flood(z);
                for(int y=0;y<25;y++)for(int x=0;x<80;x++)
                    if(!z.GetCell(x,y).BlocksMovement())Assert.IsTrue(reached[x,y],z.ZoneID+" seed"+seed+" stranded open pocket "+x+","+y);
                Assert.IsTrue(Enumerable.Range(0,25).Any(y=>reached[0,y]));Assert.IsTrue(Enumerable.Range(0,25).Any(y=>reached[79,y]));
                Assert.IsTrue(Enumerable.Range(0,80).Any(x=>reached[x,0]));Assert.IsTrue(Enumerable.Range(0,80).Any(x=>reached[x,24]));
                if(depth==2)AssertGap(z);
            }
        }

        [Test] public void FoundingLightRemainsNativeAndDoesNotTurnIntoSimaDaylight()
        {
            var m=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),64);var z=m.GetZone(Id(2));
            Assert.AreEqual(OverworldZoneManager.GetDepthAmbient(2),z.AmbientLevel);
            Assert.Less(z.AmbientLevel,m.GetZone("Overworld.2.7.2").AmbientLevel);Assert.IsTrue(z.GetCell(40,12).IsInterior);
            foreach(var e in z.GetAllEntities().Where(e=>e.BlueprintName=="FoundingPlume"))
            {var l=e.GetPart<LightSourcePart>();Assert.AreEqual(5,l.Radius);Assert.AreEqual(.8f,l.Intensity,.0001f);Assert.AreEqual("&G",l.LightColor);Assert.NotNull(e.GetPart<FoundingPlumePart>());}
        }
    }
}
