using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    /// <summary>Independent native-owner review. No renderer or Unity driver is
    /// invoked here; visual and ordinary-stat play acceptance remain separate.</summary>
    public sealed class DensityReferenceGladeAdversarialTests
    {
        DensityLootTestScope scope;
        EntityFactory Factory => scope.Factory;
        [SetUp] public void Setup() { scope=new DensityLootTestScope(); }
        [TearDown] public void Cleanup() { scope.Dispose(); }
        Zone Build(int seed=64) {var z=new Zone(ReferenceGladePlan.ZoneID);Assert.True(new ReferenceGladeBuilder(seed).BuildZone(z,Factory,new Random(19)));return z;}
        [TestCase("Grass")][TestCase("Wall")][TestCase("Chest")][TestCase("GlowQuartzVein")]
        [TestCase("HealingTonic")][TestCase("Torch")][TestCase("DriedMeat")][TestCase("MarlbackScrabbler")]
        public void EveryRequiredContentRefusalPrecedesAnyZoneMutation(string id)
        {Factory.Blueprints.Remove(id);var z=new Zone(ReferenceGladePlan.ZoneID);z.GenReservedCells.Add((5,5));Assert.False(new ReferenceGladeBuilder(64).BuildZone(z,Factory,new Random(1)));Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(5,5)},z.GenReservedCells);}
        [TestCase("ground-render")][TestCase("chest-container")][TestCase("initialization-throws")]
        public void MalformedOrThrowingContentRefusesCleanlyBeforePublication(string mode)
        {var z=new Zone(ReferenceGladePlan.ZoneID);if(mode=="ground-render")Factory.Blueprints["Grass"].Parts.Remove("Render");if(mode=="chest-container")Factory.Blueprints["Chest"].Parts.Remove("Container");if(mode=="initialization-throws"){Factory.RegisterPartType<ThrowOnCreation>();Factory.Blueprints["MarlbackScrabbler"].Parts["ThrowOnCreation"]=new Dictionary<string,string>();}bool result=true;Assert.DoesNotThrow(()=>result=new ReferenceGladeBuilder(64).BuildZone(z,Factory,new Random(1)));Assert.False(result);Assert.AreEqual(0,z.EntityCount);Assert.AreEqual(0,z.GenReservedCells.Count);}
        public sealed class ThrowOnCreation:Part {public override bool HandleEvent(GameEvent e){if(e.ID=="ObjectCreated")throw new InvalidOperationException("glade initialization probe");return true;}}
        [TestCase("Wall")][TestCase("Chest")]
        public void LateInvalidFootprintRollsBackEarlierOwnersAndReservations(string blueprint)
        {Factory.Blueprints[blueprint].Parts["SpatialFootprint"]=new Dictionary<string,string>{{"CellsRaw","79,24"}};var z=new Zone(ReferenceGladePlan.ZoneID);z.GenReservedCells.Add((5,5));Assert.False(new ReferenceGladeBuilder(64).BuildZone(z,Factory,new Random(1)));Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(5,5)},z.GenReservedCells);for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)Assert.AreEqual(0,z.GetCell(x,y).Occupants.Count);}
        [TestCase("biome")][TestCase("poi")]
        public void AttachedWorldAuthorityOverridesTheSpecialAddress(string mode)
        {var manager=OverworldZoneManager.CreateDetached(Factory,64);var z=new Zone(ReferenceGladePlan.ZoneID);manager.ReplaceLoadedState(new Dictionary<string,Zone>{{z.ZoneID,z}},z.ZoneID,new Dictionary<string,List<ZoneConnection>>());if(mode=="biome")manager.WorldMap.Tiles[11,10]=BiomeType.Beating;else manager.WorldMap.SetPOI(11,10,new PointOfInterest(POIType.Village,"new village"));Assert.False(ReferenceGladePlan.IsActive(z));Assert.False(new ReferenceGladeBuilder(64).BuildZone(z,Factory,new Random(1)));Assert.AreEqual(0,z.EntityCount);}
        [TestCase(0)][TestCase(1)][TestCase(64)][TestCase(1729)][TestCase(int.MinValue)][TestCase(int.MaxValue)]
        public void SeedVarietyNeverClosesTheFourExitsOrFiniteRewardApproaches(int seed)
        {var z=Build(seed);var reached=DensityReferenceGladeTests.Reach(z,ReferenceGladePlan.StartX,ReferenceGladePlan.StartY);Assert.True(reached.Any(p=>p.Item1==0));Assert.True(reached.Any(p=>p.Item1==Zone.Width-1));Assert.True(reached.Any(p=>p.Item2==0));Assert.True(reached.Any(p=>p.Item2==Zone.Height-1));foreach(var e in z.GetReadOnlyEntities().Where(e=>e.HasPart<HarvestablePart>()||e.HasPart<ContainerPart>())){var p=z.GetEntityCell(e);Assert.True(reached.Any(c=>Math.Max(Math.Abs(c.Item1-p.X),Math.Abs(c.Item2-p.Y))<=1),e.BlueprintName);}Assert.AreEqual(3,z.GetEntitiesWithTag("Creature").Count(e=>e.BlueprintName.StartsWith("Marlback")));}
        [Test] public void RejectedOccupiedInputPreservesItsOwnerAndTileState()
        {var z=new Zone(ReferenceGladePlan.ZoneID);var actor=Factory.CreateEntity("Player");z.AddEntity(actor,40,12);z.TileState.WriteCoating(5,5,"acid",10);Assert.False(new ReferenceGladeBuilder(64).BuildZone(z,Factory,new Random(1)));Assert.AreSame(actor,z.GetCell(40,12).Objects.Single());Assert.True(z.TileState.Has(5,5));Assert.AreEqual(1,z.EntityCount);}
        [Test] public void PlanExposesReadOnlyPlacementView()
        {var list=ReferenceGladePlan.Create(64).Placements as IList<ReferenceGladePlan.Placement>;Assert.NotNull(list);Assert.True(list.IsReadOnly);Assert.Throws<NotSupportedException>(()=>list.RemoveAt(0));}
        [Test] public void CachedZoneCanBeCompletelyEmptiedWithoutRegeneratingAnyLoot()
        {var m=OverworldZoneManager.CreateDetached(Factory,64);var z=m.GetZone(ReferenceGladePlan.ZoneID);foreach(var e in z.GetReadOnlyEntities().ToArray())z.RemoveEntity(e);Assert.AreSame(z,m.GetZone(z.ZoneID));Assert.AreEqual(0,z.EntityCount);}
        [TestCase("contents")][TestCase("harvest")][TestCase("destruction")]
        public void DepletedNativeOwnersStayDepletedAcrossTheWholeSaveGraph(string action)
        {
            var m=OverworldZoneManager.CreateDetached(Factory,64);var z=m.GetZone(ReferenceGladePlan.ZoneID);m.SetActiveZone(z);var player=Factory.CreateEntity("Player");z.AddEntity(player,40,12);
            string removedId=null;Entity retained=null;
            var oldHarvest=HarvestablePart.Factory;var oldDestruction=DestructionSystem.EntityFactoryRef;var oldTurns=TurnManager.Active;
            try
            {
                if(action=="contents"){retained=z.GetReadOnlyEntities().Single(e=>e.BlueprintName=="Chest");retained.GetPart<ContainerPart>().Contents.Clear();}
                if(action=="harvest")
                {
                    retained=z.GetReadOnlyEntities().First(e=>e.BlueprintName=="GlowQuartzVein");removedId=retained.ID;var cell=z.GetEntityCell(retained);z.MoveEntity(player,cell.X-1,cell.Y);HarvestablePart.Factory=Factory;
                    var ev=GameEvent.New("InventoryAction");ev.SetParameter("Actor",player);ev.SetParameter("Command","Harvest");ev.SetParameter("Zone",z);retained.FireEventAndRelease(ev);Assert.True(retained.GetPart<HarvestablePart>().Harvested);Assert.Null(z.GetEntityCell(retained));
                }
                if(action=="destruction")
                {
                    retained=z.GetReadOnlyEntities().First(e=>e.BlueprintName=="Wall"&&e.GetPart<LightSourcePart>()!=null);removedId=retained.ID;DestructionSystem.EntityFactoryRef=Factory;DestructionSystem.Destroy(retained,player,z,"review");Assert.Null(z.GetEntityCell(retained));
                }
                var turns=new TurnManager();turns.RestoreSavedState(71,true,player,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=player,Energy=1000}});
                var state=GameSessionState.Capture("glade-review","test",m,turns,player);GameSessionState loaded;
                using(var stream=new MemoryStream()){state.Save(new SaveWriter(stream));stream.Position=0;loaded=GameSessionState.Load(new SaveReader(stream,Factory));}
                var saved=loaded.ZoneManager.ActiveZone;int count=saved.EntityCount;
                Assert.True(ReferenceGladePlan.IsActive(saved));Assert.AreSame(saved,loaded.ZoneManager.GetZone(saved.ZoneID));Assert.AreEqual(count,saved.EntityCount);
                if(removedId!=null)Assert.False(saved.GetReadOnlyEntities().Any(e=>e.ID==removedId));
                else Assert.AreEqual(0,saved.GetReadOnlyEntities().Single(e=>e.ID==retained.ID).GetPart<ContainerPart>().Contents.Count);
                Assert.False(new ReferenceGladeBuilder(64).BuildZone(saved,Factory,new Random(1)));Assert.AreEqual(count,saved.EntityCount);
            }
            finally{HarvestablePart.Factory=oldHarvest;DestructionSystem.EntityFactoryRef=oldDestruction;typeof(TurnManager).GetProperty("Active").SetValue(null,oldTurns);}
        }
        [TestCase("biome")][TestCase("poi")]
        public void RestoredWorldDoesNotResurrectAuthorityAfterItsMapChanged(string mode)
        {
            var m=OverworldZoneManager.CreateDetached(Factory,64);var z=m.GetZone(ReferenceGladePlan.ZoneID);m.SetActiveZone(z);var player=Factory.CreateEntity("Player");z.AddEntity(player,40,12);
            if(mode=="biome")m.WorldMap.Tiles[11,10]=BiomeType.Grovelands;else m.WorldMap.SetPOI(11,10,new PointOfInterest(POIType.Village,"altered"));
            var oldTurns=TurnManager.Active;try{var turns=new TurnManager();turns.RestoreSavedState(0,true,player,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=player,Energy=1000}});var state=GameSessionState.Capture("glade-auth","test",m,turns,player);using(var stream=new MemoryStream()){state.Save(new SaveWriter(stream));stream.Position=0;var loaded=GameSessionState.Load(new SaveReader(stream,Factory));Assert.False(ReferenceGladePlan.IsActive(loaded.ZoneManager.ActiveZone));}}finally{typeof(TurnManager).GetProperty("Active").SetValue(null,oldTurns);}
        }
        [TestCase("blueprint")][TestCase("required-part")]
        public void IncompleteContentPackFallsBackToOrdinarySpreadRatherThanBreakingTravel(string mode)
        {
            if(mode=="blueprint")Factory.Blueprints.Remove("GlowQuartzVein");else Factory.Blueprints["Chest"].Parts.Remove("Container");
            var manager=OverworldZoneManager.CreateDetached(Factory,64);var z=manager.GetZone(ReferenceGladePlan.ZoneID);
            Assert.NotNull(z);Assert.Greater(z.EntityCount,0);
            Assert.False(ReferenceGladePlan.IsActive(z),"ordinary fallback must not acquire the glade art profile");
            Assert.False(z.GetReadOnlyEntities().Any(e=>e.GetPart<RenderPart>()?.VisualID=="reference-glade-ground"));
            Assert.Greater(DensityReferenceGladeTests.Reach(z,40,12).Count,1);
        }
        [Test] public void CompleteActualContentPackStillSelectsTheNativeGlade()
        {var manager=OverworldZoneManager.CreateDetached(Factory,64);var z=manager.GetZone(ReferenceGladePlan.ZoneID);Assert.NotNull(z);Assert.True(z.GetReadOnlyEntities().Any(e=>e.GetPart<RenderPart>()?.VisualID=="reference-glade-ground"));}
        [Test] public void AuthoredRewardsAndEnemiesDoNotGrantDeveloperInvulnerabilityOrTinkering()
        {var z=Build();Assert.False(z.GetReadOnlyEntities().Any(e=>e.HasPart<BitLockerPart>()||e.HasTag("Invulnerable")));foreach(var e in z.GetEntitiesWithTag("Creature").Where(e=>e.BlueprintName.StartsWith("Marlback")))Assert.That(e.GetStatValue("Hitpoints"),Is.InRange(1,40));}
    }
}
