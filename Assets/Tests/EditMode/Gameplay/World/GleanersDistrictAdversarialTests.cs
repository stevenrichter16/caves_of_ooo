using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class GleanersDistrictAdversarialTests
    {
        const string Surface="Overworld.11.10.0",Cellar="Overworld.11.10.1";
        HaulingContentScope scope;Zone oldActive;
        [SetUp] public void Setup(){scope=new HaulingContentScope();scope.Seed(64);oldActive=SettlementRuntime.ActiveZone;RepairRecipeRegistry.ResetForTests();}
        [TearDown] public void Cleanup(){SurfaceCreationProbe.Remembered=null;SurfaceCreationProbe.Manager=null;SettlementRuntime.ActiveZone=oldActive;RepairRecipeRegistry.ResetForTests();scope.Dispose();}
        OverworldZoneManager Manager()=>OverworldZoneManager.CreateDetached(scope.Factory,64,true);
        Entity Well(Zone z)=>z.GetReadOnlyEntities().Single(e=>e.GetProperty("GleanersDistrict.Role")=="well");
        Zone BareGlade(){var z=new Zone(Surface);Assert.True(new ReferenceGladeBuilder(64).BuildZone(z,scope.Factory,new Random(7)));return z;}

        [Test] public void LateralFirstAccessBuildsActualSurfaceEndpointBeforeCellar()
        {
            var m=Manager();var below=m.GetZone(Cellar);var above=m.CachedZones[Surface];
            Assert.NotNull(Well(above));Assert.True(below.GetCell(40,12).Objects.Any(e=>e.HasPart<StairsUpPart>()));
            Assert.AreEqual(1,m.GetConnections(Surface).Count(c=>c.TargetZoneID==Cellar));
        }
        [Test] public void OldCachedCellarPreventsNewSurfaceHalfPair()
        {
            var m=Manager();var old=new Zone(Cellar);var item=scope.Factory.CreateEntity("FireClay");old.AddEntity(item,5,5);m.CachedZones[Cellar]=old;
            var surface=m.GetZone(Surface);
            Assert.False(surface.GetReadOnlyEntities().Any(e=>e.GetProperty("GleanersDistrict.Role")!=null));
            Assert.IsEmpty(m.GetConnections(Surface));Assert.AreSame(old,m.GetZone(Cellar));Assert.AreSame(item,old.GetCell(5,5).Objects.Single());
        }
        [Test] public void OldCachedSurfacePreventsNewAuthoredCellarHalfPair()
        {
            var m=Manager();var old=BareGlade();m.CachedZones[Surface]=old;
            var cellar=m.GetZone(Cellar);Assert.NotNull(cellar);
            Assert.False(cellar.GetReadOnlyEntities().Any(e=>e.GetProperty("GleanersCellar.Role")!=null));
            Assert.AreSame(old,m.GetZone(Surface));Assert.IsEmpty(m.GetConnections(Surface));
        }
        [TestCase(BiomeType.Beating)][TestCase(BiomeType.Grovelands)]
        public void SameAddressInAnotherBiomeCannotGainDistrict(BiomeType biome)
        {
            var m=Manager();m.WorldMap.Tiles[11,10]=biome;var z=BareGlade();int count=z.EntityCount;
            Assert.False(GleanersDistrict.TryInstall(z,m));Assert.AreEqual(count,z.EntityCount);Assert.False(GleanersDistrict.SupportsColumn(m));Assert.IsEmpty(m.GetConnections(Surface));
        }
        [TestCase(POIType.Village)][TestCase(POIType.Lair)]
        public void AuthoredPoiCannotGainDistrict(POIType type)
        {
            var m=Manager();m.WorldMap.SetPOI(11,10,new PointOfInterest(type,"different-place"));var z=BareGlade();int count=z.EntityCount;
            Assert.False(GleanersDistrict.TryInstall(z,m));Assert.AreEqual(count,z.EntityCount);Assert.IsEmpty(m.GetConnections(Surface));
        }
        [TestCase("RepairLinedWell")][TestCase("StairsDown")][TestCase("StairsUp")][TestCase("FireClay")][TestCase("Buckler")]
        public void MissingOptionalContentLeavesExistingGladeAndRoutesUntouched(string blueprint)
        {
            var m=Manager();var z=BareGlade();var old=z.GetReadOnlyEntities().ToArray();scope.Factory.Blueprints.Remove(blueprint);
            Assert.False(GleanersDistrict.TryInstall(z,m));CollectionAssert.AreEquivalent(old,z.GetReadOnlyEntities());Assert.IsEmpty(m.GetConnections(Surface));
        }
        [TestCase(41,8)][TestCase(28,19)][TestCase(41,10)]
        public void OccupiedAnchorCannotDeleteOrMoveOldOwner(int x,int y)
        {
            var m=Manager();var z=BareGlade();var obstruction=scope.Factory.CreateEntity("Crate");z.AddEntity(obstruction,x,y);var old=z.GetReadOnlyEntities().ToArray();
            Assert.False(GleanersDistrict.TryInstall(z,m));CollectionAssert.AreEquivalent(old,z.GetReadOnlyEntities());Assert.AreEqual((x,y),z.GetEntityPosition(obstruction));Assert.IsEmpty(m.GetConnections(Surface));
        }
        [TestCase("well")][TestCase("stairs")]
        public void InvalidNativeOwnerDoesNotPublishPartialSite(string owner)
        {
            var m=Manager();var z=BareGlade();var old=z.GetReadOnlyEntities().ToArray();
            scope.Factory.Blueprints[owner=="well"?"RepairLinedWell":"StairsDown"].Parts.Remove(owner=="well"?"Repairable":"StairsDown");
            Assert.False(GleanersDistrict.TryInstall(z,m));CollectionAssert.AreEquivalent(old,z.GetReadOnlyEntities());Assert.IsEmpty(m.GetConnections(Surface));
        }
        [Test] public void RepeatInstallationAndForgedCachedAliasDoNotDuplicateContents()
        {
            var m=Manager();var z=m.GetZone(Surface);var old=z.GetReadOnlyEntities().ToArray();
            Assert.False(GleanersDistrict.TryInstall(z,m));CollectionAssert.AreEquivalent(old,z.GetReadOnlyEntities());
            Assert.AreEqual(1,m.GetConnections(Surface).Count);
            m.CachedZones[Surface]=new Zone("Overworld.1.1.0");Assert.False(GleanersDistrict.CanBuildCellar(m));
        }
        [Test] public void RemovingPhysicalStairRevokesNewCellarAuthority()
        {
            var m=Manager();var z=m.GetZone(Surface);Assert.True(GleanersDistrict.CanBuildCellar(m));
            z.RemoveEntity(z.GetReadOnlyEntities().Single(e=>e.GetProperty("GleanersDistrict.Role")=="stairs"));
            Assert.False(GleanersDistrict.CanBuildCellar(m));
        }
        [TestCase("change-well")][TestCase("change-map")]
        public void LateFactoryCallbackCannotPublishChangedRepairOrWorldAuthority(string mode)
        {
            var m=Manager();var z=BareGlade();var old=z.GetReadOnlyEntities().ToArray();
            scope.Factory.RegisterPartType<SurfaceCreationProbe>();SurfaceCreationProbe.Manager=m;
            scope.Factory.Blueprints["RepairLinedWell"].Parts["SurfaceCreationProbe"]=new Dictionary<string,string>{{"Mode","remember"}};
            scope.Factory.Blueprints["StairsDown"].Parts["SurfaceCreationProbe"]=new Dictionary<string,string>{{"Mode",mode}};
            Assert.False(GleanersDistrict.TryInstall(z,m));CollectionAssert.AreEquivalent(old,z.GetReadOnlyEntities());Assert.IsEmpty(m.GetConnections(Surface));
        }
        public sealed class SurfaceCreationProbe:Part
        {
            public string Mode;public static Entity Remembered;public static OverworldZoneManager Manager;
            public override bool HandleEvent(GameEvent e)
            {
                if(e.ID!="ObjectCreated")return true;
                if(Mode=="remember")Remembered=ParentEntity;
                if(Mode=="change-well")Remembered.GetPart<RepairablePart>().Repaired=true;
                if(Mode=="change-map")Manager.WorldMap.Tiles[11,10]=BiomeType.Beating;
                return true;
            }
        }
        [TestCase("solid-stairs")][TestCase("invisible-well")][TestCase("missing-composition")][TestCase("wrong-composition")]
        public void MalformedInitialOwnerCannotPublishAnUnusableDistrict(string mode)
        {
            var m=Manager();var z=BareGlade();var old=z.GetReadOnlyEntities().ToArray();
            var well=scope.Factory.Blueprints["RepairLinedWell"];
            if(mode=="solid-stairs")scope.Factory.Blueprints["StairsDown"].Parts["Physics"]["Solid"]="true";
            if(mode=="invisible-well")well.Parts["Render"]["Visible"]="false";
            if(mode=="missing-composition")well.Parts.Remove("Composition");
            if(mode=="wrong-composition")well.Parts["Composition"]["MaterialsRaw"]="Wood";
            Assert.False(GleanersDistrict.TryInstall(z,m));CollectionAssert.AreEquivalent(old,z.GetReadOnlyEntities());Assert.IsEmpty(m.GetConnections(Surface));
        }
        [Test] public void RepairRequiresTwoRecoveredUnitsAndSaveRetainsSpentSupplyAndFunctionalWell()
        {
            using(var isolation=new HotbarSaveFixture(false,false))
            {
                var m=Manager();var surface=m.GetZone(Surface);var below=m.GetZone(Cellar);var well=Well(surface);
                var actor=scope.Factory.CreateEntity("Player");var box=below.GetReadOnlyEntities().Single(e=>e.GetProperty("GleanersCellar.Role")=="supplies");
                var container=box.GetPart<ContainerPart>();var clay=container.Contents.Where(e=>e.BlueprintName=="FireClay").ToArray();
                Assert.AreEqual(2,clay.Sum(e=>e.GetPart<StackerPart>()?.StackCount??1));
                var p=below.GetEntityPosition(box);Assert.True(below.AddEntity(actor,p.x-1,p.y));m.SetActiveZone(below);SettlementRuntime.ActiveZone=below;
                foreach(var item in clay)Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(box,item),actor,below).Success);
                Assert.False(container.Contents.Any(e=>e.BlueprintName=="FireClay"));
                Assert.True(below.TryTransferEntityTo(actor,surface,40,8));m.SetActiveZone(surface);SettlementRuntime.ActiveZone=surface;
                Assert.False(well.GetPart<WellPart>().IsUsable);
                Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(well,"RepairObject"),actor,surface).Success);
                Assert.True(well.GetPart<WellPart>().IsUsable);Assert.False(actor.GetPart<InventoryPart>().Objects.Any(e=>e.BlueprintName=="FireClay"));
                Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(well,"RepairObject"),actor,surface).Success);
                var saved=HotbarSaveFixture.RoundTrip(GameSessionState.Capture("gleaners-save","depth-loop",m,null,actor));
                var restored=saved.ZoneManager;var freshSurface=restored.GetZone(Surface);var freshBelow=restored.GetZone(Cellar);
                Assert.AreNotSame(surface,freshSurface);Assert.True(Well(freshSurface).GetPart<WellPart>().IsUsable);
                var sameBox=freshBelow.GetReadOnlyEntities().Single(e=>e.ID==box.ID);
                Assert.False(sameBox.GetPart<ContainerPart>().Contents.Any(e=>e.BlueprintName=="FireClay"));
                Assert.AreEqual(1,restored.GetConnections(Surface).Count(c=>c.TargetZoneID==Cellar));
                restored.UnloadZone(Surface);restored.UnloadZone(Cellar);
                Assert.AreSame(freshSurface,restored.GetZone(Surface));Assert.AreSame(freshBelow,restored.GetZone(Cellar));
            }
        }
    }
}
