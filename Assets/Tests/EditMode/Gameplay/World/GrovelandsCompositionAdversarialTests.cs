using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class GrovelandsCompositionAdversarialTests
    {
        [TestCase(null)] [TestCase("")] [TestCase("Overworld.2.6.1")]
        [TestCase("Overworld.30.6.0")] [TestCase("garbage")]
        public void InvalidCompositionAddressFailsExplicitly(string id)
            => Assert.Throws<ArgumentException>(()=>GrovelandsCompositionPlan.Create(id,1));

        [TestCase("Overworld.0.0.0",true)]
        [TestCase("Overworld.1.8.0",true)]
        [TestCase("Overworld.3.6.0",false)] // town owns its authored scene
        [TestCase("Overworld.6.6.0",false)] // Cinderhold
        [TestCase("Overworld.10.10.0",false)]
        [TestCase("Overworld.1.8.1",false)]
        public void WildernessPredicateDoesNotOverwriteSitesOrOtherBiomes(string id,bool supported)
        {
            Assert.AreEqual(supported,GrovelandsCompositionPlan.IsWildernessZone(id));
            if(supported) Assert.IsTrue(VoxelWorldPresentation.IsSupported(id),"Composed wilderness needs its voxel view beyond the original four chunks.");
        }
        [TestCase(0)] [TestCase(17)] [TestCase(64)] [TestCase(729490642)]
        public void AllFormationsRemainReachableAfterNativeConnectivity(int seed)
        {
            var factory=GrovelandsCompositionTests.Factory();
            foreach(Formation formation in new[]{Formation.Grove,Formation.TendrilFen,Formation.FruitingWall,Formation.CompostingField})
            {
                var zone=new Zone("Overworld.2.6.0");
                var terrain=new GrovelandsCompositionBuilder(seed){FormationOverride=formation};
                Assert.IsTrue(terrain.BuildZone(zone,factory,new Random(seed)));
                var p=terrain.Plan;
                for(int x=0;x<Zone.Width;x++)for(int y=0;y<Zone.Height;y++)
                    if(p.IsReserved(x,y))Assert.IsFalse(zone.GetCell(x,y).BlocksMovement(),"Vegetation occupied reserved space.");
                new GrovelandsFormationBuilder{Composition=terrain,Override=formation}.BuildZone(zone,factory,new Random(seed));
                new ConnectivityBuilder{FloorBlueprint="Grass"}.BuildZone(zone,factory,new Random(seed));
                var seen=new HashSet<(int,int)>();var queue=new Queue<(int,int)>();
                queue.Enqueue((0,p.WestY));seen.Add((0,p.WestY));
                while(queue.Count>0)
                {
                    var at=queue.Dequeue();
                    for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)
                    {
                        int x=at.Item1+dx,y=at.Item2+dy;
                        if(zone.InBounds(x,y)&&!zone.GetCell(x,y).BlocksMovement()&&seen.Add((x,y)))queue.Enqueue((x,y));
                    }
                }
                for(int x=0;x<Zone.Width;x++)for(int y=0;y<Zone.Height;y++)
                    if(!zone.GetCell(x,y).BlocksMovement())Assert.IsTrue(seen.Contains((x,y)),formation+" seed="+seed+" unreachable="+x+","+y);
                Assert.IsTrue(seen.Contains((Zone.Width-1,p.EastY)));
                if(formation==Formation.Grove)
                {
                    Assert.AreEqual(1,zone.GetAllEntities().Count(e=>e.BlueprintName=="GroveSeep"));
                    Assert.AreEqual(1,zone.GetAllEntities().Count(e=>e.BlueprintName=="GroveSign"));
                }
            }
        }
        [TestCase(Formation.Grove)] [TestCase(Formation.TendrilFen)]
        public void FormationKeepsEntirePlannedApproachDryAndOpen(Formation form)
        {
            var f=GrovelandsCompositionTests.Factory();
            for(int seed=0;seed<24;seed++)
            {
                var z=new Zone("Overworld.2.6.0");var b=new GrovelandsCompositionBuilder(seed){FormationOverride=form};
                b.BuildZone(z,f,new Random(seed));new GrovelandsFormationBuilder{Composition=b,Override=form}.BuildZone(z,f,new Random(seed));
                if(form==Formation.Grove)
                {
                    Assert.IsFalse(b.Plan.IsApproach(b.Plan.FocalX,b.Plan.FocalY));
                    Assert.IsTrue(z.TileState.HasCoating(b.Plan.FocalX,b.Plan.FocalY,"water"),"Seep must remain a real water destination.");
                }
                for(int x=1;x<Zone.Width-1;x++)for(int y=1;y<Zone.Height-1;y++)
                {
                    if(b.Plan.IsApproach(x,y))
                    {
                        Assert.IsFalse(z.GetCell(x,y).BlocksMovement(),form+" seed="+seed+" corridor "+x+","+y);
                        Assert.IsFalse(z.TileState.HasCoating(x,y,"water"),"Dry approach became water.");
                    }
                    if(form==Formation.TendrilFen && z.TileState.HasCoating(x,y,"water"))
                        Assert.Less(b.Plan.WaterDistance(x,y),1.2,"Actual fen water diverged from its plan.");
                }
            }
        }
        [Test]
        public void SharedPoiPipelineRetainsLegacyTerrain()
        {
            var manager=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),64);
            var method=typeof(OverworldZoneManager).GetMethod("CreateMerchantCampPipeline",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            var pipeline=(ZoneGenerationPipeline)method.Invoke(manager,new object[]{BiomeType.Grovelands,1});
            Assert.IsFalse(pipeline.Builders.Any(b=>b is GrovelandsCompositionBuilder));
            Assert.IsTrue(pipeline.Builders.Any(b=>b is JungleBuilder));
        }
        [TestCase("Overworld.4.6.0")] [TestCase("Overworld.2.7.0")] [TestCase("Overworld.1.6.0")]
        public void ReservedSitesDoNotAcquireWildernessComposition(string id)
            => Assert.IsFalse(GrovelandsCompositionPlan.IsWildernessZone(id));

        [Test]
        public void OccupiedTerrainIsRejectedWithoutDeletingNativeOwners()
        {
            var f=GrovelandsCompositionTests.Factory();var zone=new Zone("Overworld.2.6.0");
            var owner=f.CreateEntity("Tree");zone.AddEntity(owner,20,10);
            Assert.IsFalse(new GrovelandsCompositionBuilder(1).BuildZone(zone,f,new Random(1)));
            Assert.AreSame(owner,zone.GetCell(20,10).Objects.Single());
        }
        [Test]
        public void RegionalFieldsContinueAcrossTheChunkSeam()
        {
            var a=GrovelandsCompositionPlan.Create("Overworld.2.6.0",64);
            var b=GrovelandsCompositionPlan.Create("Overworld.3.6.0",64);
            for(int y=0;y<Zone.Height;y++)
            {
                Assert.AreEqual(a.Growth(Zone.Width,y),b.Growth(0,y),1e-10);
                Assert.AreEqual(a.Moisture(Zone.Width,y),b.Moisture(0,y),1e-10);
            }
        }
        [TestCase(Formation.Grove)] [TestCase(Formation.TendrilFen)]
        [TestCase(Formation.FruitingWall)] [TestCase(Formation.CompostingField)]
        public void RealizedNativeLayoutRepeatsRegardlessOfCallerRandomHistory(Formation form)
        {
            var f=GrovelandsCompositionTests.Factory();
            Func<int,string> snapshot=history=>
            {
                var rng=new Random(123);for(int i=0;i<history;i++)rng.Next();
                var z=new Zone("Overworld.2.6.0");var b=new GrovelandsCompositionBuilder(64){FormationOverride=form};
                Assert.IsTrue(b.BuildZone(z,f,rng));
                Assert.IsTrue(new GrovelandsFormationBuilder{Composition=b,Override=form}.BuildZone(z,f,rng));
                var rows=new List<string>();
                foreach(var e in z.GetAllEntities())
                { var at=z.GetEntityPosition(e);rows.Add(at.x+","+at.y+":"+e.BlueprintName); }
                for(int x=0;x<Zone.Width;x++)for(int y=0;y<Zone.Height;y++)
                    if(z.TileState.HasCoating(x,y,"water"))rows.Add("water:"+x+","+y);
                rows.Sort(StringComparer.Ordinal);return string.Join(";",rows);
            };
            Assert.AreEqual(snapshot(0),snapshot(100));
        }

        [Test]
        public void CompositionDoesNotConsumeCallerRandomOrUnityRandom()
        {
            var a=new Random(71);var b=new Random(71);var state=UnityEngine.Random.state;
            new GrovelandsCompositionBuilder(12).BuildZone(new Zone("Overworld.2.6.0"),GrovelandsCompositionTests.Factory(),a);
            Assert.AreEqual(a.Next(),b.Next());Assert.AreEqual(state,UnityEngine.Random.state);
        }
    }
}
