using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Data;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class StillleafCompositionAdversarialTests
    {
        [TestCase("TepuiWall",0)] [TestCase("Bush",0)] [TestCase("SandstoneFloor",1)] [TestCase("Sack",1)] [TestCase("HealingTonic",1)] [TestCase("SandstoneWall",2)]
        public void MissingDependencyCannotLeavePartialTerrain(string bp,int depth)
        {
            var f=GrovelandsCompositionTests.Factory();Assert.IsTrue(f.Blueprints.Remove(bp));
            var z=new Zone("Overworld.2.4."+depth);z.GenReservedCells.Add((79,24));
            Assert.IsFalse(new StillleafCompositionBuilder(64).BuildZone(z,f,new Random(1)));
            Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
        }
        [TestCase("TepuiWall",0)] [TestCase("SandstoneWall",2)]
        public void MalformedNativeSolidRejectsBeforePlacement(string bp,int depth)
        {
            var f=GrovelandsCompositionTests.Factory();Assert.IsTrue(f.Blueprints[bp].Parts.Remove("Physics"));
            var z=new Zone("Overworld.2.4."+depth);z.GenReservedCells.Add((79,24));
            Assert.IsFalse(new StillleafCompositionBuilder(64).BuildZone(z,f,new Random(1)));
            Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
        }
        [TestCase("TepuiStone","Physics",0)] [TestCase("TepuiStone","Render",0)]
        [TestCase("TepuiWall","Render",0)] [TestCase("SandstoneFloor","Physics",2)]
        [TestCase("SandstoneFloor","Render",2)] [TestCase("SandstoneWall","Render",2)]
        public void GroundAndArchitectureRequireActualNativePartsWithCompleteContentCountercontrol(string bp,string part,int depth)
        {
            var good=GrovelandsCompositionTests.Factory();var goodZone=new Zone("Overworld.2.4."+depth);
            Assert.IsTrue(new StillleafCompositionBuilder(64).BuildZone(goodZone,good,new Random(1)),"Complete native content must pass the same route.");
            var bad=GrovelandsCompositionTests.Factory();Assert.IsTrue(bad.Blueprints[bp].Parts.Remove(part));
            var z=new Zone(goodZone.ZoneID);z.GenReservedCells.Add((79,24));
            Assert.IsFalse(new StillleafCompositionBuilder(64).BuildZone(z,bad,new Random(1)));
            Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
        }
        [Test] public void MalformedSupplyContainerRejectsBeforePlacement()
        {
            var f=GrovelandsCompositionTests.Factory();Assert.IsTrue(f.Blueprints["Sack"].Parts.Remove("Container"));
            var z=new Zone("Overworld.2.4.1");z.GenReservedCells.Add((79,24));
            Assert.IsFalse(new StillleafCompositionBuilder(64).BuildZone(z,f,new Random(1)));
            Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
        }
        [Test] public void FinalPipelineKeepsSlopeFlagRowsAndTierThreeArchivePopulation()
        {
            var m=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),64);
            var method=typeof(OverworldZoneManager).GetMethod("GetPipelineForZone",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.NotNull(method);
            var surface=(ZoneGenerationPipeline)method.Invoke(m,new object[]{"Overworld.2.4.0"});
            var pop=surface.Builders.OfType<PopulationBuilder>().Single().Table;
            StringAssert.StartsWith("StumpSlopes",pop.Name);
            foreach(var species in new[]{"SariSnake","SkySari"})Assert.AreEqual("UrquActive",pop.Entries.Single(e=>e.BlueprintName==species).RequiresWorldFlag);
            Assert.IsTrue(pop.Entries.Any(e=>e.BlueprintName=="Wardline"));
            var floor=(ZoneGenerationPipeline)method.Invoke(m,new object[]{"Overworld.2.4.2"});
            var actual=floor.Builders.OfType<PopulationBuilder>().Single().Table;
            var expected=PopulationTable.CaveTier3();Assert.AreEqual(expected.Name,actual.Name);
            CollectionAssert.AreEqual(expected.Entries.Select(e=>e.BlueprintName+":"+e.Weight+":"+e.MinCount+":"+e.MaxCount),
                actual.Entries.Select(e=>e.BlueprintName+":"+e.Weight+":"+e.MinCount+":"+e.MaxCount));
        }
        [TestCase(false)] [TestCase(true)]
        public void RealSurfacePopulationObeysUrquManifestFlag(bool active)
        {
            var prior=NarrativeStatePart.Current;
            try
            {
                var state=new NarrativeStatePart();NarrativeStatePart.Current=state;state.SetFact("UrquActive",active?1:0);
                var f=GrovelandsCompositionTests.Factory();var m=new OverworldZoneManager(f,64);
                var method=typeof(OverworldZoneManager).GetMethod("GetPipelineForZone",BindingFlags.Instance|BindingFlags.NonPublic);
                var pipeline=(ZoneGenerationPipeline)method.Invoke(m,new object[]{"Overworld.2.4.0"});
                var population=pipeline.Builders.OfType<PopulationBuilder>().Single();
                var row=population.Table.Entries.Single(e=>e.BlueprintName=="SariSnake");
                Assert.AreEqual("UrquActive",row.RequiresWorldFlag);
                var forced=new PopulationTable{Name="Stillleaf native flag countercontrol"};
                forced.Entries.Add(new PopulationEntry{BlueprintName=row.BlueprintName,Weight=1,MinCount=1,MaxCount=1,
                    RequiresWorldFlag=row.RequiresWorldFlag,ForbidsWorldFlag=row.ForbidsWorldFlag});population.Table=forced;
                var z=new Zone("Overworld.2.4.0");Assert.IsTrue(pipeline.Generate(z,f,new Random(1)));
                Assert.AreEqual(active,z.GetAllEntities().Any(e=>e.BlueprintName=="SariSnake"));
            }
            finally{NarrativeStatePart.Current=prior;}
        }
        [TestCase(1)] [TestCase(2)]
        public void DirectLowerLoadKeepsRealReciprocalStaircase(int depth)
        {
            var m=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),64);var z=m.GetZone("Overworld.2.4."+depth);
            var parent=m.GetZone("Overworld.2.4."+(depth-1));
            var edge=m.GetConnections(parent.ZoneID).Single(c=>c.SourceZoneID==parent.ZoneID&&c.TargetZoneID==z.ZoneID&&c.Type=="StairsDown");
            Assert.IsTrue(parent.GetCell(edge.SourceX,edge.SourceY).Objects.Any(e=>e.HasPart<StairsDownPart>()));
            Assert.IsTrue(z.GetCell(edge.TargetX,edge.TargetY).Objects.Any(e=>e.HasPart<StairsUpPart>()));
        }
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void FinalSealedRoomIsExcludedButExteriorStairsAndEdgesRemainAccessible(int seed)
        {
            var f=GrovelandsCompositionTests.Factory();var z=new OverworldZoneManager(f,seed).GetZone("Overworld.2.4.2");
            var door=z.GetAllEntities().Single(e=>e.BlueprintName=="SealedLibraryDoor");var dc=z.GetEntityCell(door);
            foreach(var actor in z.GetAllEntities().Where(e=>e.HasPart<BrainPart>()||e.HasTag("Creature")).ToArray())z.RemoveEntity(actor);
            var seen=Flood(z,(dc.X-1,dc.Y));var inside=z.GetAllEntities().Where(e=>e.HasTag("ExcludeZoneArrival")).Select(z.GetEntityPosition).ToArray();
            Assert.Greater(inside.Length,100);Assert.IsFalse(inside.Any(seen.Contains));
            foreach(var stair in z.GetAllEntities().Where(e=>e.HasPart<StairsUpPart>()||e.HasPart<StairsDownPart>()))Assert.IsTrue(seen.Contains(z.GetEntityPosition(stair)));
            foreach(var p in new[]{(0,12),(79,12),(40,0),(40,24)})Assert.IsTrue(seen.Contains(p));
            foreach(var p in inside)
            {
                var c=z.GetCell(p.x,p.y);Assert.IsTrue(z.GenReservedCells.Contains(p));
                Assert.IsTrue(c.Objects.All(e=>e.BlueprintName=="SealedLibraryFloor"||e.BlueprintName=="SealedArchiveShelf"||e.ID==StillleafArchive.RegisterId)); // SA.1: the authored register
            }
            var visitor=new Entity();var inv=new InventoryPart();visitor.AddPart(inv);
            var key=new Entity();key.AddPart(new KeyPart{KeyId=SealedLibraryBuilder.KeyID});inv.Objects.Add(key);
            var action=GameEvent.New("InventoryAction");action.SetParameter("Command","Unlock");action.SetParameter("Actor",(object)visitor);
            door.FireEventAndRelease(action);Assert.IsFalse(door.GetPart<SealedLibraryBarrierPart>().IsClosed);
            Assert.IsFalse(z.GetCell(dc.X,dc.Y).BlocksMovement());seen=Flood(z,(dc.X-1,dc.Y));
            Assert.IsTrue(inside.Where(p=>!z.GetCell(p.x,p.y).BlocksMovement()).All(seen.Contains));
            Assert.IsTrue(inside.All(p=>z.GetCell(p.x,p.y).HasObjectWithTag("ExcludeZoneArrival")),"Unlocking is not permission for travel fallback to materialize inside.");
        }
        private static HashSet<(int x,int y)> Flood(Zone z,(int x,int y) start)
        {
            var seen=new HashSet<(int x,int y)>{start};var q=new Queue<(int x,int y)>();q.Enqueue(start);
            while(q.Count>0){var p=q.Dequeue();foreach(var d in new[]{(-1,0),(1,0),(0,-1),(0,1)})
                {var n=(p.x+d.Item1,p.y+d.Item2);var c=z.GetCell(n.Item1,n.Item2);if(c!=null&&!c.BlocksMovement()&&seen.Add(n))q.Enqueue(n);}}
            return seen;
        }
    }
}
