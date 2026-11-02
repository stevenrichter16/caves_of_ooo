using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class GinmereCompositionTests
    {
        [Test] public void MouthInteriorIsNativeBareStoneWhileOuterCountryRemainsPlantableGrass()
        {
            var f=GrovelandsCompositionTests.Factory();var p=GinmereCompositionPlan.Create("Overworld.2.7.0",64);
            Assert.AreEqual("SandstoneFloor",p.GroundAt(40,12));Assert.AreEqual("Grass",p.GroundAt(5,12));
            var z=new Zone("Overworld.2.7.0");Assert.IsTrue(new GinmereCompositionBuilder(64).BuildZone(z,f,new Random(1)));
            var inner=z.GetCell(40,12).Objects.Single(e=>e.HasTag("Terrain"));
            var outer=z.GetCell(5,12).Objects.Single(e=>e.HasTag("Terrain"));
            Assert.AreEqual("SandstoneFloor",inner.BlueprintName);Assert.IsFalse(inner.HasTag("Plantable"));
            Assert.AreEqual("Grass",outer.BlueprintName);Assert.IsTrue(outer.HasTag("Plantable"));
            Assert.IsFalse(z.GetCell(40,12).BlocksMovement(),"Bare native stone is not an invented lethal abyss or an invisible blocker.");
        }
        [TestCase(1)] [TestCase(2)]
        public void UndergroundCliffsHaveSolidInteriorMassRatherThanOnlyOneCellEdgeRails(int depth)
        {
            foreach(int seed in new[]{64,1729,729490642})
            {
                var p=GinmereCompositionPlan.Create("Overworld.2.7."+depth,seed);int blocks=0;
                for(int y=0;y<24;y++)for(int x=0;x<79;x++)
                    if(p.ObjectAt(x,y)=="SandstoneWall"&&p.ObjectAt(x+1,y)=="SandstoneWall"
                        &&p.ObjectAt(x,y+1)=="SandstoneWall"&&p.ObjectAt(x+1,y+1)=="SandstoneWall")blocks++;
                Assert.GreaterOrEqual(blocks,100,"Cliff masses need coherent two-dimensional thickness, seed "+seed);
            }
        }
        [Test] public void ExpeditionShelvesGrowFromCliffSidesRatherThanFloatingIsolatedStrips()
        {
            foreach(int seed in new[]{64,1729,729490642})
            {
                var p=GinmereCompositionPlan.Create("Overworld.2.7.1",seed);var seen=new HashSet<(int,int)>();int groups=0;
                for(int y=0;y<25;y++)for(int x=0;x<80;x++)
                {
                    if(p.ObjectAt(x,y)!="DescentLedge"||seen.Contains((x,y)))continue;
                    groups++;var q=new Queue<(int,int)>();q.Enqueue((x,y));bool anchored=false;
                    while(q.Count>0)
                    {
                        var c=q.Dequeue();if(!seen.Add(c))continue;
                        foreach(var n in new[]{(c.Item1-1,c.Item2),(c.Item1+1,c.Item2),(c.Item1,c.Item2-1),(c.Item1,c.Item2+1)})
                        {string bp=p.ObjectAt(n.Item1,n.Item2);if(bp=="SandstoneWall")anchored=true;
                            if(bp=="DescentLedge"&&!seen.Contains(n))q.Enqueue(n);}
                    }
                    Assert.IsTrue(anchored,"Each connected shelf must meet a native cliff, seed "+seed);
                }
                Assert.GreaterOrEqual(groups,3);
            }
        }
        [Test] public void GinFrogsOccupyIrregularDryBankPositionsNearTheBasin()
        {
            var f=GrovelandsCompositionTests.Factory();
            foreach(int seed in new[]{64,1729,729490642})
            {
                var z=new Zone("Overworld.2.7.2");Assert.IsTrue(new GinmereCompositionBuilder(seed).BuildZone(z,f,new Random(1)));
                var frogs=z.GetAllEntities().Where(e=>e.BlueprintName=="GinFrog").Select(z.GetEntityCell).ToArray();
                Assert.That(frogs.Length,Is.InRange(2,4));Assert.Greater(frogs.Select(c=>c.Y).Distinct().Count(),1,"Residents must not line up on one programmed row.");
                foreach(var c in frogs)
                {
                    Assert.IsFalse(c.Objects.Any(e=>e.BlueprintName=="MirePool"));bool near=false;
                    for(int dy=-3;dy<=3;dy++)for(int dx=-3;dx<=3;dx++)
                        if(z.InBounds(c.X+dx,c.Y+dy)&&z.GetCell(c.X+dx,c.Y+dy).Objects.Any(e=>e.BlueprintName=="MirePool"))near=true;
                    Assert.IsTrue(near,"Gin frogs belong to the basin bank, not an unrelated display line.");
                }
            }
        }
        [TestCase("Overworld.2.7.0",0)] [TestCase("Overworld.2.7.1",1)] [TestCase("Overworld.2.7.2",2)]
        public void ExactThreeLevelsHaveRepeatableDistinctNativePlans(string id,int depth)
        {
            Assert.IsTrue(GinmereCompositionPlan.IsSupportedZone(id));
            var a=GinmereCompositionPlan.Create(id,64);
            Assert.AreEqual(depth,a.Depth);
            Assert.AreEqual(a.Signature(),GinmereCompositionPlan.Create(id,64).Signature());
            Assert.AreNotEqual(a.Signature(),GinmereCompositionPlan.Create(id,1729).Signature());
        }

        [TestCase(null)] [TestCase("")] [TestCase("Overworld.2.7.3")]
        [TestCase("Overworld.4.6.2")] [TestCase("Overworld.2.4.2")]
        [TestCase("Overworld.2.7.02")] [TestCase(" Overworld.2.7.0")]
        public void UnrelatedSitesDepthsAndAliasesAreRejectedWithoutMutation(string id)
        {
            Assert.IsFalse(GinmereCompositionPlan.IsSupportedZone(id));
            Assert.Throws<ArgumentException>(()=>GinmereCompositionPlan.Create(id,64));
            if(id==null)return;
            var z=new Zone(id);var f=GrovelandsCompositionTests.Factory();
            Assert.IsFalse(new GinmereCompositionBuilder(64).BuildZone(z,f,new Random(1)));
            Assert.AreEqual(0,z.EntityCount);Assert.AreEqual(0,z.GenReservedCells.Count);
        }

        [TestCase(0,"Tree")] [TestCase(1,"DescentLedge")] [TestCase(2,"MirePool")]
        public void NativeLandformsKeepOpenTravelAndOneOwnerPerBlueprintCell(int depth,string signature)
        {
            var f=GrovelandsCompositionTests.Factory();
            foreach(int seed in new[]{0,64,1729,int.MaxValue,int.MinValue})
            {
                var z=new Zone("Overworld.2.7."+depth);var b=new GinmereCompositionBuilder(seed);
                Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.NotNull(b.Plan);
                Assert.IsTrue(z.GetAllEntities().Any(e=>e.BlueprintName==signature));
                Assert.IsTrue(FormationReachability.FullyReached(z,FormationReachability.FloodFromWest(z)));
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                {
                    var cell=z.GetCell(x,y);
                    Assert.IsFalse(cell.Objects.GroupBy(e=>e.BlueprintName).Any(g=>g.Count()>1));
                    if(!b.Plan.IsApproach(x,y))continue;
                    Assert.IsFalse(cell.BlocksMovement());
                    Assert.IsFalse(cell.Objects.Any(e=>e.BlueprintName=="MirePool"));
                    Assert.IsTrue(z.GenReservedCells.Contains((x,y)));
                }
            }
        }

        [Test] public void DrownedBasinIsOneJoinedNativeLiquidBodyWithUsableDryNestSpace()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.2.7.2");
            var b=new GinmereCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
            var water=z.GetAllEntities().Where(e=>e.BlueprintName=="MirePool").ToArray();
            Assert.Greater(water.Length,100);
            var cells=new HashSet<(int,int)>(water.Select(e=>{var c=z.GetEntityCell(e);return(c.X,c.Y);}));
            var q=new Queue<(int,int)>();var seen=new HashSet<(int,int)>();q.Enqueue(cells.First());
            while(q.Count>0){var p=q.Dequeue();if(!seen.Add(p))continue;
                foreach(var n in new[]{(p.Item1-1,p.Item2),(p.Item1+1,p.Item2),(p.Item1,p.Item2-1),(p.Item1,p.Item2+1)})
                    if(cells.Contains(n)&&!seen.Contains(n))q.Enqueue(n);}
            Assert.AreEqual(cells.Count,seen.Count,"The basin must not be sliced into unrelated ponds.");
            foreach(var e in water){Assert.NotNull(e.GetPart<LiquidPoolPart>());Assert.NotNull(e.GetPart<TileStateSourcePart>());}
            var defenders=new List<Cell>();int usable=0;
            for(int y=3;y<22;y++)for(int x=3;x<77;x++)
            {
                var c=z.GetCell(x,y);if(c.BlocksMovement()||cells.Contains((x,y))||z.GenReservedCells.Contains((x,y)))continue;
                PricklebrowNestPart.CollectDefenderCells(z,x,y,defenders);
                if(defenders.Count==PricklebrowNestPart.DefenderCount)usable++;
            }
            Assert.Greater(usable,0,"Reservations must leave real native defender capacity.");
        }

        [Test] public void ExpeditionHasThreeAnchorsAndStockedSuppliesWithoutAddingClimbMechanics()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.2.7.1");
            Assert.IsTrue(new GinmereCompositionBuilder(64).BuildZone(z,f,new Random(1)));
            Assert.AreEqual(3,z.GetAllEntities().Count(e=>e.BlueprintName=="RopeAnchor"));
            var sack=z.GetAllEntities().Single(e=>e.BlueprintName=="Sack");
            Assert.NotNull(sack.GetPart<ContainerPart>());
            CollectionAssert.AreEquivalent(new[]{"Torch","DriedMeat","HealingTonic"},
                sack.GetPart<ContainerPart>().Contents.Select(e=>e.BlueprintName));
            Assert.IsTrue(z.GetAllEntities().Any(e=>e.BlueprintName=="Bones"));
        }

        [Test] public void RebuildingARealizedZoneIsRejectedWithoutDeletingItsOwners()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone("Overworld.2.7.2");var b=new GinmereCompositionBuilder(64);
            Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var owners=z.GetAllEntities().ToArray();
            Assert.IsFalse(b.BuildZone(z,f,new Random(1)));CollectionAssert.AreEquivalent(owners,z.GetAllEntities());
        }
    }
}

namespace CavesOfOoo.Tests
{
    public sealed class GinmereRibsTests
    {
        DensityLootTestScope scope;
        [SetUp] public void Setup()=>scope=new DensityLootTestScope();
        [TearDown] public void Cleanup()=>scope.Dispose();
        sealed class Fixture
        {
            internal OverworldZoneManager Manager; internal Zone Zone; internal GinmereCompositionBuilder Terrain; internal IZoneBuilder Ribs;
            internal Random Rng=new Random(901);
            internal Fixture(CavesOfOoo.Data.EntityFactory factory,int seed=704860946)
            {
                Manager=OverworldZoneManager.CreateDetached(factory,seed,true); Zone=new Zone("Overworld.2.7.1");
                var pipeline=(ZoneGenerationPipeline)typeof(OverworldZoneManager).GetMethod("GetPipelineForZone",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(Manager,new object[]{Zone.ZoneID});
                Terrain=pipeline.Builders.OfType<GinmereCompositionBuilder>().Single(); Ribs=pipeline.Builders.SingleOrDefault(b=>b.Name=="GinmereStoneRibs");
                foreach(var b in pipeline.Builders.OrderBy(b=>b.Priority).Where(b=>b.Priority<4350))Assert.True(b.BuildZone(Zone,factory,Rng),b.Name);
            }
            internal bool Run(CavesOfOoo.Data.EntityFactory f){Assert.NotNull(Ribs,"Missing bounded cold Ginmere stone ribs");return Ribs.BuildZone(Zone,f,Rng);}
        }
        [TestCase(704860946)][TestCase(64)][TestCase(1729)]
        public void ColdRibsPreserveExistingOwnersRoutesPlanReservesAndRandom(int seed)
        {
            var a=new Fixture(scope.Factory,seed);var control=new Fixture(scope.Factory,seed);var z=a.Zone;
            var original=z.GetReadOnlyEntities().ToArray();var places=original.Select(z.GetEntityPosition).ToArray();var reserved=z.GenReservedCells.ToArray();string state=z.TileState.ToSaveString(),plan=a.Terrain.Plan.Signature();
            Assert.True(a.Run(scope.Factory));var added=z.GetReadOnlyEntities().Except(original).ToArray();Assert.That(added.Length,Is.InRange(7,17));
            for(int i=0;i<original.Length;i++)Assert.AreEqual(places[i],z.GetEntityPosition(original[i]));
            foreach(var e in added){Assert.That(e.BlueprintName,Is.EqualTo("Stalagmite").Or.EqualTo("Rubble"));var c=z.GetEntityCell(e);Assert.AreEqual(e.BlueprintName=="Stalagmite",c.BlocksMovement());Assert.False(e.HasTag("Wall"));for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)Assert.False(a.Terrain.Plan.IsApproach(c.X+x,c.Y+y));}
            Assert.AreEqual(plan,a.Terrain.Plan.Signature());Assert.AreEqual(state,z.TileState.ToSaveString());CollectionAssert.AreEquivalent(reserved,z.GenReservedCells);Assert.AreEqual(control.Rng.Next(),a.Rng.Next());
            Assert.True(FormationReachability.FullyReached(z,FormationReachability.FloodFromWest(z)));var owners=z.GetReadOnlyEntities().ToArray();Assert.True(a.Run(scope.Factory));CollectionAssert.AreEquivalent(owners,z.GetReadOnlyEntities());
            var controlOriginal=control.Zone.GetReadOnlyEntities().ToArray();Assert.True(control.Run(scope.Factory));CollectionAssert.AreEqual(added.Select(z.GetEntityPosition).OrderBy(p=>p).ToArray(),control.Zone.GetReadOnlyEntities().Except(controlOriginal).Select(control.Zone.GetEntityPosition).OrderBy(p=>p).ToArray());
        }
        [TestCase("cached")][TestCase("active")][TestCase("map")][TestCase("poi")][TestCase("factory")][TestCase("producer")]
        public void LoadedOrForeignAuthorityDoesNotDressGraph(string fault)
        {
            var f=new Fixture(scope.Factory);if(fault=="cached")f.Manager.CachedZones.Add(f.Zone.ZoneID,f.Zone);if(fault=="active")f.Manager.SetActiveZone(f.Zone);if(fault=="map")f.Manager.WorldMap.POIs=new PointOfInterest[1,1];if(fault=="poi")f.Manager.WorldMap.GetPOI(2,7).Name="foreign";if(fault=="producer")f.Terrain.BuildZone(new Zone("Overworld.2.7.1"),scope.Factory,new Random(8));
            var before=f.Zone.GetReadOnlyEntities().ToArray();Assert.True(f.Run(fault=="factory"?GrovelandsCompositionTests.Factory():scope.Factory));CollectionAssert.AreEquivalent(before,f.Zone.GetReadOnlyEntities());
        }
        [TestCase("reserved")][TestCase("wet")][TestCase("occupied")][TestCase("missing")]
        public void UnavailableTerrainOrPayloadDoesNotPublish(string fault)
        {
            var f=new Fixture(scope.Factory);if(fault=="missing")scope.Factory.Blueprints.Remove("Stalagmite");else for(int y=2;y<23;y++)for(int x=2;x<78;x++){if(fault=="reserved")f.Zone.GenReservedCells.Add((x,y));if(fault=="wet")f.Zone.TileState.WriteCoating(x,y,"water",ZoneTileState.Permanent);if(fault=="occupied")f.Zone.AddEntity(new Entity{ID="fixture"+x+"-"+y,BlueprintName="foreign"},x,y);}
            var before=f.Zone.GetReadOnlyEntities().ToArray();Assert.True(f.Run(scope.Factory));CollectionAssert.AreEquivalent(before,f.Zone.GetReadOnlyEntities());
        }
        [TestCase("solid")][TestCase("wall")][TestCase("duplicate")][TestCase("tile")][TestCase("reserve")]
        public void PayloadOrCallbackChangesCannotPublishRibs(string fault)
        {
            var f=new Fixture(scope.Factory);var before=f.Zone.GetReadOnlyEntities().ToArray();Entity first=null;
            scope.Factory.RegisterPartType<SpreadGenerationReceiptTests.ReceiptCreatedPart>("ReceiptCreated");scope.Factory.Blueprints["Stalagmite"].Parts["ReceiptCreated"]=new Dictionary<string,string>();
            SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=e=>{e.RemovePart(e.GetPart<SpreadGenerationReceiptTests.ReceiptCreatedPart>());if(first==null){first=e;return;}if(fault=="solid")first.GetPart<PhysicsPart>().Solid=false;else if(fault=="wall")first.Tags["Wall"]="";else if(fault=="duplicate")first.ID=e.ID;else if(fault=="tile")f.Zone.TileState.WriteCoating(30,6,"water",ZoneTileState.Permanent);else f.Zone.GenReservedCells.Add((30,6));};
            try{f.Run(scope.Factory);Assert.NotNull(first);CollectionAssert.AreEquivalent(before,f.Zone.GetReadOnlyEntities());}finally{SpreadGenerationReceiptTests.ReceiptCreatedPart.Callback=null;}
        }
    }
}
