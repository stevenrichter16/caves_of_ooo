using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class SpreadExplorationGeometryTests
    {
        // Captured from actual unchanged Unity before production edits, E0 accepted V2 run.
        [TestCase(1,Formation.Hedgerow,"998eea2e7ba9829f00d51aafe74ae97f6ca51d1443c499154ea6e509cf4ef54a")]
        [TestCase(1,Formation.FieldStrips,"847380bba2e5998d8b2ee2cb2234992f88178db5781e92ac98d0058a90430bb5")]
        [TestCase(1,Formation.OldRoad,"62f3fcc39d80281e68198f432378c801474fd77d45bcc20451f3bc1b0b9a4253")]
        [TestCase(1,Formation.FlowerMeadow,"4767ae4cba8a4df09a9c3c7aac4ea26b476718b24616975cf012cc03b261ffc5")]
        [TestCase(1,Formation.Fallow,"0f2d1d5cb9513463c73995c0158b7639a1b2296c4f936d409638de3641f50ea5")]
        [TestCase(1,Formation.RiverMeadow,"dea23e45e5aed4b3d1b1042c0ba6a083304ce1a10e988066a83d4c22fba13bdd")]
        [TestCase(64,Formation.Hedgerow,"fc97f5b1e25b75d4b7cf617d3186ad96d87f0dffe7c01ee5e6c5d35e041ca101")]
        [TestCase(64,Formation.FieldStrips,"a49fadaf4e186228fe94213505f924fae899390143b1feddc1e8b0e9329675de")]
        [TestCase(64,Formation.OldRoad,"a549eede7fb0d0bb685697edb17c8352dc1bf06b82eb95a8a6c49895a63096d9")]
        [TestCase(64,Formation.FlowerMeadow,"b6e58c304b5e356166704c09e6bd3ba2929163c251268077ee3d4d5c562e800c")]
        [TestCase(64,Formation.Fallow,"d7030baccfc6b95be3fc5d1ebf49abd1369621a565ff1cf4541a85fb29084278")]
        [TestCase(64,Formation.RiverMeadow,"a4a8cbcf65be5c90e0b333aa1a7783dccc2bb8803a3184be202301d50dbe1668")]
        [TestCase(1729,Formation.Hedgerow,"e8bb1ec517818d66f616db91fb498d00caa3d7b637f919c3a01e5021e187aefa")]
        [TestCase(1729,Formation.FieldStrips,"c580d0d6ccb172b93a0ef347d80e1273253f3f656272df377e1beb5edd0818fe")]
        [TestCase(1729,Formation.OldRoad,"9ca883c6890465cea5ee92ce9e1b2d91aa449875c0e625935ab89fdc56cf4ee3")]
        [TestCase(1729,Formation.FlowerMeadow,"0d5e618ed07761b0593757612e1f6d523eaa20f965631c0bd0443f992780bc98")]
        [TestCase(1729,Formation.Fallow,"f5b738d03afc9b5242c08a08219cac49a8531b0008070dde3ba6dc57cabd1282")]
        [TestCase(1729,Formation.RiverMeadow,"56f554442d6096b8ff4a5bf1bb35ceabb409624f59eab462ede8c80eff74b848")]
        public void LegacyGeometryMatchesFrozenNativeBaseline(int seed,Formation formation,string expected)
        {
            using(var hash=System.Security.Cryptography.SHA256.Create())
            {
                string signature=SpreadCompositionPlan.Create(Id,seed,formation).Signature();
                string actual=BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signature))).Replace("-","").ToLowerInvariant();
                Assert.AreEqual(expected,actual,"Legacy saves retain exact authored geometry, not only repeatability of a new implementation.");
            }
        }
        const string Id="Overworld.8.4.0";
        static SpreadCompositionPlan Variant(string id,int seed,Formation formation,string topology)
        {
            var type=typeof(SpreadCompositionPlan).Assembly.GetType("CavesOfOoo.Core.SpreadExplorationTopology");
            Assert.NotNull(type,"New-world exploration requires an explicit topology contract; legacy geometry alone cannot vary routes.");
            var create=typeof(SpreadCompositionPlan).GetMethod("Create",new[]{typeof(string),typeof(int),typeof(Formation),type});
            Assert.NotNull(create,"Opted-in topology must reach the actual composition plan.");
            return (SpreadCompositionPlan)create.Invoke(null,new[]{(object)id,seed,formation,Enum.Parse(type,topology)});
        }
        [TestCase("OffsetLanes")][TestCase("BrokenEnclosures")][TestCase("BankCrossing")]
        public void NewTopologyChangesRoutesWithoutChangingBoundaryPorts(string topology)
        {
            var legacy=SpreadCompositionPlan.Create(Id,64,Formation.Hedgerow);
            var changed=Variant(Id,64,Formation.Hedgerow,topology);
            Assert.AreEqual(legacy.WestY,changed.WestY);Assert.AreEqual(legacy.EastY,changed.EastY);
            Assert.AreEqual(legacy.NorthX,changed.NorthX);Assert.AreEqual(legacy.SouthX,changed.SouthX);
            int changedApproaches=0;
            for(int x=2;x<Zone.Width-2;x++)for(int y=2;y<Zone.Height-2;y++)
                if(legacy.IsApproach(x,y)!=changed.IsApproach(x,y))changedApproaches++;
            Assert.Greater(changedApproaches,80,"A renamed or cosmetically mirrored field is not a new route grammar.");
            Assert.AreNotEqual(legacy.Signature(),changed.Signature());
            Assert.AreEqual(changed.Signature(),Variant(Id,64,Formation.Hedgerow,topology).Signature());
        }
        [TestCase("OffsetLanes")][TestCase("BrokenEnclosures")][TestCase("BankCrossing")]
        public void EveryInteriorAndBorderApproachIsConnectedAndDry(string topology)
        {
            foreach(var form in new[]{Formation.Hedgerow,Formation.FieldStrips,Formation.OldRoad,Formation.FlowerMeadow,Formation.Fallow,Formation.RiverMeadow})
            foreach(int seed in new[]{1,35,64,1729})
            {
                var p=Variant(Id,seed,form,topology);
                var reached=new HashSet<int>();var queue=new Queue<(int x,int y)>();
                queue.Enqueue((0,p.WestY));reached.Add(p.WestY*Zone.Width);
                while(queue.Count>0){var c=queue.Dequeue();foreach(var d in new[]{(1,0),(-1,0),(0,1),(0,-1)}){
                    int x=c.x+d.Item1,y=c.y+d.Item2;if(x<0||x>=Zone.Width||y<0||y>=Zone.Height)continue;
                    string bp=p.ObjectAt(x,y);if(bp=="Hedge"||bp=="Tree"||p.IsWater(x,y)||!reached.Add(y*Zone.Width+x))continue;queue.Enqueue((x,y));}}
                Assert.IsTrue(reached.Contains(p.EastY*Zone.Width+79),form+" east "+seed);
                Assert.IsTrue(reached.Contains(p.NorthX),form+" north "+seed);
                Assert.IsTrue(reached.Contains(24*Zone.Width+p.SouthX),form+" south "+seed);
                for(int x=0;x<Zone.Width;x++)for(int y=0;y<Zone.Height;y++)if(p.IsApproach(x,y)){
                    Assert.IsFalse(p.IsWater(x,y));Assert.IsTrue(reached.Contains(y*Zone.Width+x),form+" disconnected approach "+x+","+y+" seed "+seed);}
            }
        }
        [TestCase("OffsetLanes")][TestCase("BrokenEnclosures")][TestCase("BankCrossing")]
        public void NeighboringVariantsKeepSameSharedCrossings(string topology)
        {
            var a=Variant(Id,1729,Formation.Hedgerow,topology);
            var east=Variant("Overworld.9.4.0",1729,Formation.FieldStrips,"BrokenEnclosures");
            var south=SpreadCompositionPlan.Create("Overworld.8.5.0",1729,Formation.FlowerMeadow);
            Assert.AreEqual(a.EastY,east.WestY);Assert.AreEqual(a.SouthX,south.NorthX);
            for(int n=-1;n<=1;n++){
                Assert.IsTrue(a.IsApproach(79,a.EastY+n));Assert.IsTrue(east.IsApproach(0,east.WestY+n));
                Assert.IsTrue(a.IsApproach(a.SouthX+n,24));Assert.IsTrue(south.IsApproach(south.NorthX+n,0));}
        }
        [TestCase("OffsetLanes")][TestCase("BrokenEnclosures")][TestCase("BankCrossing")]
        public void FormationStillDeterminesMaterialIdentity(string topology)
        {
            foreach(var form in new[]{Formation.Hedgerow,Formation.FieldStrips,Formation.OldRoad,Formation.FlowerMeadow,Formation.Fallow,Formation.RiverMeadow}){
                var p=Variant(Id,64,form,topology);var objects=new HashSet<string>();int water=0;
                for(int x=0;x<Zone.Width;x++)for(int y=0;y<Zone.Height;y++){objects.Add(p.ObjectAt(x,y));if(p.IsWater(x,y))water++;}
                Assert.AreEqual(form,p.Formation);
                if(form==Formation.FieldStrips)Assert.IsTrue(objects.Contains("CropRow"));else Assert.IsFalse(objects.Contains("CropRow"));
                if(form==Formation.FlowerMeadow)Assert.IsTrue(objects.Contains("FlowerField"));else Assert.IsFalse(objects.Contains("FlowerField"));
                if(form==Formation.RiverMeadow){Assert.Greater(water,0);Assert.IsTrue(objects.Contains("Reeds"));}else Assert.AreEqual(0,water);
            }
        }
        [TestCase("OffsetLanes")][TestCase("BrokenEnclosures")][TestCase("BankCrossing")]
        public void NewLayoutsRespectExistingCropAndForestBudgets(string topology)
        {
            foreach(int seed in new[]{0,1,35,64,1729,int.MaxValue,-1})
            foreach(var form in new[]{Formation.Hedgerow,Formation.FieldStrips,Formation.OldRoad,Formation.FlowerMeadow,Formation.Fallow,Formation.RiverMeadow}){
                var p=Variant(Id,seed,form,topology);int crops=0,trees=0;
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++){
                    if(p.ObjectAt(x,y)=="CropRow")crops++;if(p.ObjectAt(x,y)=="Tree")trees++;
                    if(x<2||x>77||y<2||y>22)Assert.IsNull(p.ObjectAt(x,y),"No new border blocker");}
                Assert.Less(crops,260);Assert.Less(trees,65);
            }
        }
        [Test]public void ExplicitLegacyKeepsExistingDefaultOutput()
        {
            foreach(int seed in new[]{1,64,1729})
            foreach(var form in new[]{Formation.Hedgerow,Formation.FieldStrips,Formation.OldRoad,Formation.FlowerMeadow,Formation.Fallow,Formation.RiverMeadow})
                Assert.AreEqual(SpreadCompositionPlan.Create(Id,seed,form).Signature(),Variant(Id,seed,form,"Legacy").Signature());
        }
        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void LegacyEntryPointStillRepeatsAndConnectsItsBorders(int seed)
        {
            var a=SpreadCompositionPlan.Create(Id,seed,Formation.Hedgerow);
            Assert.AreEqual(a.Signature(),SpreadCompositionPlan.Create(Id,seed,Formation.Hedgerow).Signature());
            Assert.AreEqual(a.EastY,SpreadCompositionPlan.Create("Overworld.9.4.0",seed,Formation.FieldStrips).WestY);
            Assert.IsTrue(a.IsApproach(0,a.WestY));Assert.IsTrue(a.IsApproach(79,a.EastY));
        }
    }
}
