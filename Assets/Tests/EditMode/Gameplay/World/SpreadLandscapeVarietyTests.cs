using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadLandscapeVarietyTests
    {
        const string North="Overworld.11.9.0",East="Overworld.12.10.0",South="Overworld.11.11.0";
        static SpreadCompositionPlan Plan(string id,int seed=64,SpreadExplorationTopology topology=SpreadExplorationTopology.OffsetLanes)
            =>SpreadCompositionPlan.Create(id,seed,Formation.None,topology);
        static string Landscape(SpreadCompositionPlan plan)
        {return plan.Landscape;}
        static int Count(SpreadCompositionPlan p,string bp)=>Enumerable.Range(0,Zone.Width*Zone.Height).Count(i=>p.ObjectAt(i%Zone.Width,i/Zone.Width)==bp);
        [TestCase(North,"overgrown crofts")][TestCase(East,"flower avenues")][TestCase(South,"crescent hollow")]
        public void ThreeDirectionsFromSpawnHaveDifferentLandscapes(string id,string expected)
        {Assert.AreEqual(expected,Landscape(Plan(id)));Assert.AreEqual(expected,Landscape(Plan(id,1729)));}
        [TestCase(North,1)][TestCase(North,64)][TestCase(North,1729)]
        [TestCase(East,1)][TestCase(East,64)][TestCase(East,1729)]
        [TestCase(South,1)][TestCase(South,64)][TestCase(South,1729)]
        public void NewGeographyHasBoundedNativeCoverAndOpenConnectedApproaches(string id,int seed)
        {
            foreach(var topology in new[]{SpreadExplorationTopology.OffsetLanes,SpreadExplorationTopology.BrokenEnclosures,SpreadExplorationTopology.BankCrossing})
            {
            var p=Plan(id,seed,topology);Assert.False(string.IsNullOrEmpty(Landscape(p)));
            Assert.That(Count(p,"Tree"),Is.InRange(4,85));
            if(id==North)Assert.AreEqual(0,Count(p,"FlowerField"));else Assert.That(Count(p,"FlowerField"),Is.InRange(40,360));
            Assert.AreEqual(0,Count(p,"CropRow"));
            var reached=new HashSet<(int,int)>();var pending=new Queue<(int,int)>();pending.Enqueue((0,p.WestY));
            while(pending.Count>0){var c=pending.Dequeue();if(c.Item1<0||c.Item2<0||c.Item1>=80||c.Item2>=25||reached.Contains(c))continue;
                string bp=p.ObjectAt(c.Item1,c.Item2);if(bp=="Tree"||bp=="Hedge")continue;reached.Add(c);
                pending.Enqueue((c.Item1+1,c.Item2));pending.Enqueue((c.Item1-1,c.Item2));pending.Enqueue((c.Item1,c.Item2+1));pending.Enqueue((c.Item1,c.Item2-1));}
            Assert.True(reached.Contains((79,p.EastY))&&reached.Contains((p.NorthX,0))&&reached.Contains((p.SouthX,24)));
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
                if(p.IsApproach(x,y)){Assert.Null(p.ObjectAt(x,y));Assert.True(reached.Contains((x,y)));Assert.False(p.IsWater(x,y));}
            Assert.AreEqual(p.EastY,SpreadCompositionPlan.Create(WorldMap.ToZoneID(p.WorldX+1,p.WorldY),seed,Formation.FlowerMeadow,topology).WestY);
            }
        }
        [TestCase(North)][TestCase(East)][TestCase(South)]
        public void SeedChangesDetailsWithoutChangingTheNearSpawnLandscape(string id)
        {var a=Plan(id);Assert.AreEqual(a.Signature(),Plan(id).Signature());Assert.AreNotEqual(a.Signature(),Plan(id,1729).Signature());Assert.AreEqual(Landscape(a),Landscape(Plan(id,1729)));}
        [TestCase(North)][TestCase(East)][TestCase(South)]
        public void LegacyCallsDoNotAcquireNewLandscape(string id)
        {Assert.Null(Landscape(Plan(id,64,SpreadExplorationTopology.Legacy)));Assert.AreEqual(SpreadCompositionPlan.Create(id,64).Signature(),Plan(id,64,SpreadExplorationTopology.Legacy).Signature());}
        [TestCase(Formation.Hedgerow)][TestCase(Formation.FieldStrips)][TestCase(Formation.OldRoad)][TestCase(Formation.RiverMeadow)]
        public void OtherFormationsKeepTheirOwnGrammar(Formation form)
        {Assert.Null(Landscape(SpreadCompositionPlan.Create(East,64,form,SpreadExplorationTopology.OffsetLanes)));}
        [TestCase(North)][TestCase(East)][TestCase(South)]
        public void ActualNativeOwnersCarryUsefulContextWithoutInventedHarvestStock(string id)
        {
            using(var scope=new DensityLootTestScope())
            {
                var z=new Zone(id);var b=new SpreadCompositionBuilder(64){Topology=SpreadExplorationTopology.OffsetLanes};Assert.True(b.BuildZone(z,scope.Factory,new Random(99)));
                var trees=z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="Tree").ToArray();Assert.Greater(trees.Length,3);
                Assert.True(trees.All(e=>e.GetPart<PhysicsPart>().Solid));
                string cue=id==North?"croft":id==East?"avenue":"hollow";
                Assert.True(z.GetReadOnlyEntities().Any(e=>e.GetPart<ExaminablePart>()?.Text.Contains(cue)==true));
                Assert.False(z.GetReadOnlyEntities().Any(e=>e.HasPart<HarvestablePart>()||e.HasPart<FieldHarvestPart>()||e.HasTag("Creature")),"Terrain does not mint food or actors; later existing builders own those budgets.");
                var snapshot=z.GetReadOnlyEntities().ToArray();Assert.False(b.BuildZone(z,scope.Factory,new Random(99)));CollectionAssert.AreEqual(snapshot,z.GetReadOnlyEntities());
            }
        }
        [Test] public void OtherEligibleMeadowsUseBothNewProfiles()
        {
            var names=new HashSet<string>();
            for(int x=0;x<20;x++)for(int y=0;y<20;y++){string id=WorldMap.ToZoneID(x,y);if(id==East||id==South||!SpreadCompositionPlan.IsWildernessZone(id)||FormationSelector.For(BiomeType.Spread,id)!=Formation.FlowerMeadow)continue;names.Add(Landscape(Plan(id)));}
            CollectionAssert.AreEquivalent(new[]{"flower avenues","crescent hollow"},names);
        }
    }
}
