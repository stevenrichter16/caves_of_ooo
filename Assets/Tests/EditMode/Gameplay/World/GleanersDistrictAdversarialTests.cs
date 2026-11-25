using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class GleanersDistrictAdversarialTests
    {
        const string Surface="Overworld.11.10.0",Cellar="Overworld.11.10.1";
        const string ClayTray = "LiningClaySupplyTray";
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

        [TestCase(24, false)]
        [TestCase(25, true)]
        public void ColdGenerationAddsOneLooseClayTrayOnlyInTheNewManifest(int version, bool hasTray)
        {
            var manager = Manager();
            if (version == 24) RestoreV24Manifest(manager);
            Assert.That(manager.Exploration.Version, Is.EqualTo(version));
            Assert.False(manager.CachedZones.ContainsKey(Surface));

            var surface = manager.GetZone(Surface);
            AssertParcelService(surface);
            Assert.False(Well(surface).GetPart<WellPart>().IsUsable);
            var trays = surface.GetReadOnlyEntities().Where(e => e.BlueprintName == ClayTray).ToArray();
            Assert.That(trays, Has.Length.EqualTo(hasTray ? 1 : 0));
            if (!hasTray)
            {
                Assert.That(surface.GetCell(42, 9).Objects.Any(e => e.HasPart<DeliveryStoragePart>()), Is.False);
                return;
            }

            var tray = trays.Single();
            AssertLooseClayTray(surface, tray);
            Assert.That(tray.GetPart<ContainerPart>().Contents, Is.Empty);
            Assert.That(surface.GetReadOnlyEntities().Select(e => e.GetPart<DeliveryStoragePart>())
                .Where(storage => storage != null).Count(IsLooseStock), Is.EqualTo(1));
            Assert.That(manager.GetZone(Surface).GetReadOnlyEntities().Single(e => e.BlueprintName == ClayTray), Is.SameAs(tray));
        }

        [Test]
        public void ColdWorldWithoutAnEnabledManifestKeepsTheWellAndOmitsTheLooseClayTray()
        {
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, enableExploration: false);
            Assert.False(manager.Exploration.Enabled);
            Assert.That(manager.Exploration.Version, Is.EqualTo(SpreadExplorationPlan.CurrentVersion));
            Assert.False(manager.CachedZones.ContainsKey(Surface));

            var surface = manager.GetZone(Surface);
            Assert.That(surface.GetReadOnlyEntities().Any(e => e.BlueprintName == ClayTray), Is.False);
            Assert.False(Well(surface).GetPart<WellPart>().IsUsable);
            Assert.True(GleanersDistrict.CanBuildCellar(manager));
        }

        [Test]
        public void GeneratedLooseClayTrayRetainsItsIdentityAndStockAcrossSave()
        {
            using var isolation = new HotbarSaveFixture(false, false);
            var manager = Manager();
            var surface = manager.GetZone(Surface);
            manager.SetActiveZone(surface);
            SettlementRuntime.ActiveZone = surface;
            var tray = surface.GetReadOnlyEntities().Single(e => e.BlueprintName == ClayTray);
            var actor = scope.Factory.CreateEntity("Player");
            var clay = scope.Factory.CreateEntity("PreparedLiningClay");
            clay.GetPart<StackerPart>().StackCount = 2;
            Assert.True(surface.AddEntity(actor, 42, 9));
            Assert.True(actor.GetPart<InventoryPart>().AddObject(clay));
            Assert.True(InventorySystem.ExecuteCommand(new PutInContainerCommand(tray, clay), actor, surface).Success);

            using var bytes = new MemoryStream();
            GameSessionState.Capture("gleaners-tray", "saved loose measures", manager, null, actor).Save(new SaveWriter(bytes));
            bytes.Position = 0;
            var loaded = GameSessionState.Load(new SaveReader(bytes, scope.Factory));
            var restored = loaded.ZoneManager.GetZone(Surface);
            SettlementRuntime.ActiveZone = restored;
            Assert.That(loaded.ZoneManager.Exploration.Version, Is.EqualTo(25));
            Assert.That(restored, Is.Not.SameAs(surface));
            var savedTray = restored.GetReadOnlyEntities().Single(e => e.BlueprintName == ClayTray);
            Assert.That(savedTray.ID, Is.EqualTo(tray.ID));
            AssertLooseClayTray(restored, savedTray);
            var contents = savedTray.GetPart<ContainerPart>().Contents;
            var savedClay = contents.Single();
            Assert.That(savedClay.ID, Is.EqualTo(clay.ID));
            Assert.That(savedClay.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(savedClay.GetPart<PhysicsPart>().InInventory, Is.SameAs(savedTray));
            Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(savedTray, savedClay), loaded.Player, restored).Success);
            Assert.That(contents, Is.Empty);
            Assert.That(savedClay.GetPart<PhysicsPart>().InInventory, Is.SameAs(loaded.Player));
            Assert.That(savedTray.GetPart<DeliveryStoragePart>().ContentsWithdrawn, Is.False);
            Assert.False(Well(restored).GetPart<WellPart>().IsUsable);
            AssertParcelService(restored);
        }

        private static void RestoreV24Manifest(OverworldZoneManager manager)
        {
            var world = SpreadExplorationPlan.BindForSave(manager, null);
            var wire = world.GetProperty(SpreadExplorationPlan.PropertyKey);
            world.Properties[SpreadExplorationPlan.PropertyKey] = "24" + wire.Substring(wire.IndexOf('|'));
            var restore = typeof(SpreadExplorationPlan).GetMethod("Restore", BindingFlags.NonPublic | BindingFlags.Static);
            var plan = restore.Invoke(null, new object[] { manager, world });
            typeof(OverworldZoneManager).GetProperty("Exploration").SetValue(manager, plan);
        }

        private static bool IsLooseStock(DeliveryStoragePart storage)
        {
            var mode = typeof(DeliveryStoragePart).GetField("AcceptsLooseStock");
            Assert.That(mode, Is.Not.Null);
            return (bool)mode.GetValue(storage);
        }

        private static void AssertLooseClayTray(Zone zone, Entity tray)
        {
            Assert.That(zone.GetEntityPosition(tray), Is.EqualTo((42, 9)));
            Assert.That(zone.GetCell(42, 9).Objects, Does.Contain(tray));
            var physics = tray.GetPart<PhysicsPart>();
            Assert.False(physics.Takeable || physics.Solid);
            Assert.That(physics.InInventory, Is.Null);
            Assert.That(tray.GetPart<ContainerLoadPart>(), Is.Null);
            var container = tray.GetPart<ContainerPart>();
            Assert.False(container.IsLocked);
            Assert.That(container.MaxItems, Is.EqualTo(2));
            Assert.That(tray.Parts.Count(p => p is DeliveryStoragePart), Is.EqualTo(1));
            var storage = tray.GetPart<DeliveryStoragePart>();
            Assert.False(storage.ReceivesParcels);
            Assert.True(IsLooseStock(storage));
        }

        private static void AssertParcelService(Zone zone)
        {
            var owners = zone.GetReadOnlyEntities();
            var box = owners.Single(e => e.BlueprintName == "LiningClayParcel");
            var shelf = owners.Single(e => e.BlueprintName == "LiningClayReceivingShelf");
            var porter = owners.Single(e => e.BlueprintName == "GleanersPorter");
            var marker = owners.Single(e => e.BlueprintName == "GleanersLoadingMarker");
            Assert.True(box.GetPart<PhysicsPart>().Takeable && box.HasPart<ContainerLoadPart>());
            Assert.True(shelf.GetPart<DeliveryStoragePart>().ReceivesParcels);
            Assert.That(shelf.GetPart<ContainerPart>().MaxItems, Is.EqualTo(1));
            Assert.That(shelf.GetPart<ContainerPart>().Contents, Is.Empty);
            var ledger = marker.GetPart<LocalPorterLedgerPart>();
            Assert.That(ledger.Box, Is.SameAs(box));
            Assert.That(ledger.Shelf, Is.SameAs(shelf));
            Assert.That(ledger.Worker, Is.SameAs(porter));
            Assert.That(porter.GetPart<LocalPorterPart>().Ledger, Is.SameAs(marker));
            Assert.That(zone.GetEntityPosition(marker), Is.EqualTo(zone.GetEntityPosition(box)));
            var from = zone.GetEntityPosition(porter);
            var to = zone.GetEntityPosition(shelf);
            Assert.True(FindPath.Search(zone, from.x, from.y, to.x, to.y).Usable);
        }
    }
}
