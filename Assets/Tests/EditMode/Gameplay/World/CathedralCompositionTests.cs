using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>CoO-original pilgrimage composition. Assertions describe native
    /// navigation and useful places, not a screenshot or a new god encounter.</summary>
    public class CathedralCompositionTests
    {
        public static string Id(int depth) => "Overworld.5.4." + depth;
        public static ZoneGenerationPipeline Pipeline(OverworldZoneManager manager,string id)
            => (ZoneGenerationPipeline)typeof(OverworldZoneManager).GetMethod("GetPipelineForZone",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager,new object[]{id});
        public static IEnumerable<(int x,int y)> Neighbors(int x,int y)
        { yield return (x-1,y);yield return (x+1,y);yield return (x,y-1);yield return (x,y+1); }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void NativePlansAreDeterministicSeedVariableAndExactlyThreeLevels(int depth)
        {
            Assert.IsTrue(CathedralCompositionPlan.IsSupportedZone(Id(depth)));
            var p=CathedralCompositionPlan.Create(Id(depth),64);
            Assert.AreEqual(depth,p.Depth);
            Assert.AreEqual(p.Signature(),CathedralCompositionPlan.Create(Id(depth),64).Signature());
            Assert.AreNotEqual(p.Signature(),CathedralCompositionPlan.Create(Id(depth),1729).Signature());
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)Assert.IsNotNull(p.GroundAt(x,y));
        }

        [TestCase(null)] [TestCase("")] [TestCase("Overworld.5.4.3")]
        [TestCase("Overworld.4.6.2")] [TestCase("Overworld.2.4.2")]
        [TestCase("Overworld.5.4.-1")] [TestCase("Overworld.5.4.02")]
        [TestCase("Overworld.05.4.0")] [TestCase(" Overworld.5.4.0")]
        [TestCase("Overworld.5.4.0.extra")]
        public void OnlyTheNamedCathedralStackHasCompositionAuthority(string id)
        {
            Assert.IsFalse(CathedralCompositionPlan.IsSupportedZone(id));
            Assert.Throws<ArgumentException>(()=>CathedralCompositionPlan.Create(id,64));
            if(id==null)return;
            var z=new Zone(id);z.GenReservedCells.Add((79,24));
            Assert.IsFalse(new CathedralCompositionBuilder(64).BuildZone(z,GrovelandsCompositionTests.Factory(),new Random(1)));
            Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
        }

        [Test] public void PilgrimageMouthIsGreenAroundANativeWalkableStoneLanding()
        {
            var p=CathedralCompositionPlan.Create(Id(0),64);
            Assert.AreEqual("SandstoneFloor",p.GroundAt(40,12));Assert.AreEqual("Grass",p.GroundAt(5,12));
            var z=new Zone(Id(0));Assert.IsTrue(new CathedralCompositionBuilder(64).BuildZone(z,GrovelandsCompositionTests.Factory(),new Random(1)));
            Assert.IsFalse(z.GetCell(40,12).BlocksMovement(),"A painted sinkhole must not invent lethal falling on a walkable cell.");
            Assert.IsTrue(z.GetCell(5,12).Objects.Any(e=>e.HasTag("Plantable")));
            Assert.That(z.GetAllEntities().Count(e=>e.BlueprintName=="Tree"),Is.InRange(4,140));
            Assert.Greater(z.GetAllEntities().Count(e=>e.BlueprintName=="Bush"),0);
            Assert.IsFalse(z.GetAllEntities().Any(e=>e.BlueprintName=="ChoirNode"||e.BlueprintName=="EncasedElder"),"The destination must not be copied onto the arrival country.");
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void RealizationKeepsEveryApproachOpenReservedAndEveryCellOwnerUnique(int depth)
        {
            var f=GrovelandsCompositionTests.Factory();
            foreach(int seed in new[]{0,64,1729,int.MinValue,int.MaxValue})
            {
                var z=new Zone(Id(depth));var b=new CathedralCompositionBuilder(seed);
                Assert.IsTrue(b.BuildZone(z,f,new Random(99)));Assert.AreEqual(1000,b.Priority);Assert.NotNull(b.Plan);
                Assert.IsTrue(FormationReachability.FullyReached(z,FormationReachability.FloodFromWest(z)),"static pocket depth"+depth+" seed"+seed);
                int unreserved=0;
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                {
                    var c=z.GetCell(x,y);Assert.IsFalse(c.Objects.GroupBy(e=>e.BlueprintName).Any(g=>g.Count()>1));
                    if(!c.BlocksMovement()&&!z.GenReservedCells.Contains((x,y)))unreserved++;
                    if(!b.Plan.IsApproach(x,y))continue;
                    Assert.IsFalse(c.BlocksMovement(),"approach "+x+","+y);Assert.IsTrue(z.GenReservedCells.Contains((x,y)));
                }
                Assert.Greater(unreserved,60,"Later native hazard/population/container passes need meaningful unclaimed space.");
            }
        }

        [Test] public void ExpeditionShelvesAreBroadConnectedToStoneAndCarryOneActualSupplyCache()
        {
            var f=GrovelandsCompositionTests.Factory();
            foreach(int seed in new[]{64,1729,729490642})
            {
                var z=new Zone(Id(1));var b=new CathedralCompositionBuilder(seed);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
                var seen=new HashSet<(int,int)>();int groups=0,broad=0;
                for(int y=0;y<25;y++)for(int x=0;x<80;x++)
                {
                    if(b.Plan.ObjectAt(x,y)!="DescentLedge"||seen.Contains((x,y)))continue;
                    groups++;bool anchored=false;var q=new Queue<(int x,int y)>();q.Enqueue((x,y));int area=0;
                    while(q.Count>0)
                    {
                        var c=q.Dequeue();if(!seen.Add(c))continue;area++;
                        foreach(var n in Neighbors(c.x,c.y))
                        {string bp=b.Plan.ObjectAt(n.x,n.y);if(bp=="SandstoneWall")anchored=true;if(bp=="DescentLedge"&&!seen.Contains(n))q.Enqueue(n);}
                    }
                    Assert.IsTrue(anchored,"A shelf should grow from cliff stone rather than float as a stripe.");
                    if(area>=12)broad++;
                }
                Assert.That(groups,Is.InRange(3,6));Assert.AreEqual(groups,broad);
                Assert.AreEqual(3,z.GetAllEntities().Count(e=>e.BlueprintName=="RopeAnchor"));
                var sack=z.GetAllEntities().Single(e=>e.BlueprintName=="Sack");
                CollectionAssert.AreEquivalent(new[]{"Torch","DriedMeat","HealingTonic"},sack.GetPart<ContainerPart>().Contents.Select(e=>e.BlueprintName));
                var c2=z.GetEntityCell(sack);Assert.IsTrue(Neighbors(c2.X,c2.Y).Any(n=>z.GetCell(n.x,n.y)?.Objects.Any(e=>e.BlueprintName=="Bones")==true));
                Assert.IsTrue(Neighbors(c2.X,c2.Y).Any(n=>z.GetCell(n.x,n.y)?.BlocksMovement()==false));
            }
        }

        [Test] public void NativeNaveStampFitsTheBaseAndIsTheOnlySourceOfEldersNodeAndTendrils()
        {
            var f=GrovelandsCompositionTests.Factory();
            foreach(int seed in new[]{64,1729,729490642})
            {
                var z=new Zone(Id(2));Assert.IsTrue(new CathedralCompositionBuilder(seed).BuildZone(z,f,new Random(1)));
                Assert.IsFalse(z.GetAllEntities().Any(e=>e.BlueprintName=="ChoirNode"||e.BlueprintName=="EncasedElder"||e.BlueprintName=="ChoirTendril"));
                Assert.IsTrue(new ChoirCathedralBuilder().BuildZone(z,f,new Random(seed)));
                Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName=="ChoirNode"));
                Assert.That(z.GetAllEntities().Count(e=>e.BlueprintName=="EncasedElder"),Is.InRange(2,3));
                Assert.AreEqual(3,z.GetAllEntities().Count(e=>e.BlueprintName=="ChoirTendril"));
                foreach(var e in z.GetAllEntities().Where(e=>e.HasPart<BrainPart>()).ToArray())z.RemoveEntity(e);
                Assert.IsTrue(FormationReachability.FullyReached(z,FormationReachability.FloodFromWest(z)),"Native nave cannot seal off side alcoves.");
                for(int x=8;x<65;x++)for(int y=11;y<=13;y++)Assert.IsFalse(z.GetCell(x,y).BlocksMovement(),"The processional middle must remain legible and broad.");
            }
        }

        // A sequence of isolated wall islands is not a grown bay. Each planned
        // buttress must join the retained nave's own wall through actual native
        // substrate cells, rather than borrowing invisible geometry or stone.
        [Test] public void EveryGrownButtressMeetsTheNativeNaveWall()
        {
            var f=GrovelandsCompositionTests.Factory();
            foreach(int seed in new[]{64,1729,729490642})
            {
                var z=new Zone(Id(2));var b=new CathedralCompositionBuilder(seed);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
                Assert.IsTrue(new ChoirCathedralBuilder().BuildZone(z,f,new Random(seed)));
                var seen=new HashSet<(int,int)>();int buttresses=0;
                for(int y=0;y<25;y++)for(int x=0;x<80;x++)
                {
                    if(b.Plan.ObjectAt(x,y)!="SubstrateVault"||seen.Contains((x,y)))continue;
                    buttresses++;bool joinsNave=false;var q=new Queue<(int x,int y)>();q.Enqueue((x,y));
                    while(q.Count>0)
                    {
                        var c=q.Dequeue();if(!seen.Add(c))continue;
                        if((c.y==7||c.y==17)&&c.x>=6&&c.x<=73)joinsNave=true;
                        foreach(var n in Neighbors(c.x,c.y))
                            if(!seen.Contains(n)&&z.GetCell(n.x,n.y)?.Objects.Any(e=>e.BlueprintName=="SubstrateVault")==true)q.Enqueue(n);
                    }
                    Assert.IsTrue(joinsNave,"Detached grown buttress seed"+seed+" at"+x+","+y);
                }
                Assert.Greater(buttresses,0,"Must exercise the planned grown architecture.");
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void ActualManagerUsesTheNewBaseWithNativeTravelAndLaterSemanticPasses(int depth)
        {
            var manager=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),64);var p=Pipeline(manager,Id(depth));
            Assert.AreEqual(1,p.Builders.Count(b=>b is CathedralCompositionBuilder));
            Assert.IsTrue(p.Builders.Any(b=>b is PopulationBuilder));Assert.IsTrue(p.Builders.Any(b=>b is ContainerBuilder));
            Assert.IsTrue(p.Builders.Any(b=>b is HazardTerrainBuilder));
            Assert.AreEqual(depth==0,p.Builders.Any(b=>b is SinkholeMouthBuilder));
            Assert.AreEqual(depth==2,p.Builders.Any(b=>b is ChoirCathedralBuilder));
            Assert.IsFalse(p.Builders.Any(b=>b is SinkholeDescentBuilder),"The authored anchored shelves replace the loose legacy descent scatter once.");
            Assert.IsFalse(Pipeline(manager,"Overworld.5.4.3").Builders.Any(b=>b is CathedralCompositionBuilder));
            Assert.IsFalse(Pipeline(manager,"Overworld.16.4.2").Builders.Any(b=>b is CathedralCompositionBuilder));
        }

        [Test] public void CathedralKeepsItsNativeDarknessAndRealNodeLightWithoutBorrowingSimaSunlight()
        {
            var manager=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),64);var z=manager.GetZone(Id(2));
            Assert.AreEqual(OverworldZoneManager.GetDepthAmbient(2),z.AmbientLevel);
            Assert.Less(z.AmbientLevel,manager.GetZone("Overworld.2.7.2").AmbientLevel);
            var node=z.GetAllEntities().Single(e=>e.BlueprintName=="ChoirNode");var light=node.GetPart<LightSourcePart>();
            Assert.AreEqual(5,light.Radius);Assert.AreEqual(.7f,light.Intensity,.0001f);Assert.AreEqual("&M",light.LightColor);
            Assert.IsFalse(node.HasPart<StairsDownPart>()||node.HasPart<StairsUpPart>());
            Assert.IsTrue(z.GetCell(40,12).IsInterior);
        }
    }
}
