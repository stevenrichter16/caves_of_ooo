using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Installation marker and generic retention (Docs/MACHINE-INSTALLATIONS.md, S2). A village whose settlement repair relapses is
    /// unloaded and regenerated on the next visit; a cached village that holds a retaining marker, or a pinned legacy finite fixture,
    /// is kept. Counter-checks everywhere: an identical zone without the marker still regenerates.
    /// </summary>
    public sealed class InstallationRetentionTests
    {
        const string Tine = "Overworld.13.7.0", Gantry = "Overworld.7.8.0", Sill = SettlementSiteDefinitions.StartingVillageZoneId;
        HaulingContentScope scope; int turn;

        [SetUp] public void Setup()
        {
            scope = new HaulingContentScope(); scope.Seed(64); turn = 10; Diag.ResetAll();
            PlaceSiteCatalog.LoadFromJson(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/PlaceSites.json")));
            // These tests install their own markers into otherwise unmarked towns; the shipped rows (Tine's weir is a retained site object) must not make the control town retain.
            InstallationRegistry.ResetForTests(); Assert.IsEmpty(InstallationRegistry.InitializeFromJson("{\"Version\":1,\"Installations\":[]}"));
        }
        [TearDown] public void Cleanup() { PlaceSiteCatalog.Reset(); InstallationRegistry.ResetForTests(); scope.Dispose(); }

        OverworldZoneManager NewManager()
        {
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true); manager.SetTurnProvider(() => turn); return manager;
        }

        /// <summary>The Tine weir patch is due to relapse in the past, so the next access to the (inactive) village unloads it.</summary>
        void DueToRelapse(OverworldZoneManager manager)
        {
            var site = manager.SettlementManager.GetSite(Tine, "Weir"); Assert.NotNull(site, "Tine has a weir site in PlaceSites.json");
            site.Stage = RepairStage.TemporarilyPurified; site.ResolvedByMethod = RepairMethodId.PatchedRepair; site.ResolvedAtTurn = 1; site.RelapseAtTurn = 5;
            turn = 20;
        }

        /// <summary>The second relapse source (home/base PB0a): a well-keeper who knows the purification rite renews the well on arrival, which also reports a change and unloads the inactive village.</summary>
        void DueToWellKeeperRenewal(OverworldZoneManager manager)
        {
            manager.SettlementManager.SetCondition(Sill, WellKeeperPurification.Condition, true);
            var well = manager.SettlementManager.GetSite(Sill, SettlementSiteDefinitions.MainWellSiteId); Assert.NotNull(well, "only the starting village and Wellmeet track a main well");
            Assert.AreEqual(RepairableSiteType.Well, well.SiteType);
            well.Stage = RepairStage.TemporarilyPurified; well.ResolvedByMethod = RepairMethodId.PatchedRepair; well.ResolvedAtTurn = 1; well.RelapseAtTurn = 5;
            turn = 20;
        }

        Zone SillWithoutSettlingDeposit(OverworldZoneManager manager)
        {
            var zone = manager.GetZone(Sill);
            Assert.NotNull(zone);
            // This finite source independently keeps Sill cached after a well renewal.
            var deposits = zone.GetReadOnlyEntitiesWithPart<SillSettlingDepositPart>();
            Assert.AreEqual(1, deposits.Count);
            Assert.True(zone.RemoveEntity(deposits[0]));
            Assert.IsEmpty(zone.GetReadOnlyEntitiesWithPart<SillSettlingDepositPart>());
            Assert.False(InstallationMarkerPart.Retain(manager, Sill));
            return zone;
        }

        Entity Install(Zone zone, string blueprint, bool retain, string origin = "Generated", int x = 2, int y = 2)
        {
            var entity = scope.Factory.CreateEntity(blueprint); Assert.NotNull(entity, blueprint);
            entity.AddPart(new InstallationMarkerPart { InstallationId = "test:" + blueprint, RetainZone = retain, Origin = origin });
            Assert.True(zone.AddEntity(entity, x, y)); return entity;
        }

        // ── Step 0: the premise. Without a marker a relapsing village really is regenerated ──

        [Test] public void RelapseRegeneratesUnmarkedVillage_Control()
        {
            var manager = NewManager(); var first = manager.GetZone(Tine); Assert.NotNull(first);
            DueToRelapse(manager); var second = manager.GetZone(Tine);
            Assert.AreNotSame(first, second, "the hazard: a relapse unloads the village and the next access regenerates it");
        }

        [Test] public void WellKeeperRenewalRegeneratesUnmarkedVillage_Control()
        {
            var manager = NewManager(); var first = SillWithoutSettlingDeposit(manager);
            manager.SetActiveZone(Tine);
            DueToWellKeeperRenewal(manager); var second = manager.GetZone(Sill);
            Assert.AreNotSame(first, second, "the second hazard: a renewal also unloads the inactive village");
            Assert.True(manager.SettlementManager.GetSite(Sill, SettlementSiteDefinitions.MainWellSiteId).RelapseAtTurn > 20, "the renewal ran (this is the renewal source, not the relapse)");
        }

        [Test] public void WellKeeperRenewalDoesNotUnloadAMarkedVillage()
        {
            var manager = NewManager(); var first = SillWithoutSettlingDeposit(manager);
            var machine = Install(first, "TinkersForge", retain: true);
            Assert.True(InstallationMarkerPart.Retain(manager, Sill));
            manager.SetActiveZone(Tine);
            DueToWellKeeperRenewal(manager); var second = manager.GetZone(Sill);
            Assert.AreSame(first, second); Assert.NotNull(second.GetCell(2, 2).Objects.FirstOrDefault(e => e == machine));
            Assert.True(manager.SettlementManager.GetSite(Sill, SettlementSiteDefinitions.MainWellSiteId).RelapseAtTurn > 20, "the renewal ran and the zone was still kept");
        }

        // ── retention ──

        [Test] public void MarkedVillageKeepsZoneInstanceAcrossRelapse()
        {
            var manager = NewManager(); var first = manager.GetZone(Tine); var machine = Install(first, "TinkersForge", retain: true);
            DueToRelapse(manager); var second = manager.GetZone(Tine);
            Assert.AreSame(first, second); Assert.AreSame(first, manager.CachedZones[Tine]);
            Assert.NotNull(second.GetCell(2, 2).Objects.FirstOrDefault(e => e == machine));
        }

        [Test] public void MarkedEntityStateAndIdentitySurviveRelapse()
        {
            var manager = NewManager(); var first = manager.GetZone(Tine); var machine = Install(first, "TinkersForge", retain: true);
            machine.Properties["kept"] = "state"; string id = machine.ID; int count = first.EntityCount;
            DueToRelapse(manager); var second = manager.GetZone(Tine);
            Assert.AreEqual(count, second.EntityCount); var again = second.GetReadOnlyEntities().Single(e => e.ID == id);
            Assert.AreSame(machine, again); Assert.AreEqual("state", again.Properties["kept"]);
        }

        [Test] public void RetainZoneFalseGeneratedMarkerDoesNotRetain()
        {
            var manager = NewManager(); var first = manager.GetZone(Tine); Install(first, "TinkersForge", retain: false);
            DueToRelapse(manager); Assert.AreNotSame(first, manager.GetZone(Tine));
        }

        [Test] public void ARelocatedOriginRetainsByItself()
        {
            var manager = NewManager(); var first = manager.GetZone(Tine); Install(first, "TinkersForge", retain: false, origin: "Relocated");
            DueToRelapse(manager); Assert.AreSame(first, manager.GetZone(Tine));
        }

        /// <summary>A Player or unknown origin retains only through RetainZone, so a dropped or carried fixture can never pin a village (the install verbs set and clear it).</summary>
        [TestCase("Player", false)] [TestCase("SomethingElse", false)] [TestCase("Player", true)]
        public void APlayerOrUnknownOriginRetainsOnlyThroughRetainZone(string origin, bool retainZone)
        {
            var manager = NewManager(); var first = manager.GetZone(Tine); Install(first, "TinkersForge", retain: retainZone, origin: origin);
            DueToRelapse(manager); var second = manager.GetZone(Tine);
            if (retainZone) Assert.AreSame(first, second); else Assert.AreNotSame(first, second);
        }

        [TestCase("PeatOilStrainer")] [TestCase("AbandonedFieldWorkRig")] [TestCase("CinderholdOilRecoveryTray")]
        public void LegacyFiniteFixturesRetainTheirZoneWithoutAMarker(string blueprint)
        {
            var manager = NewManager(); var first = manager.GetZone(Tine);
            var fixture = scope.Factory.CreateEntity(blueprint); Assert.NotNull(fixture); Assert.False(fixture.HasPart<InstallationMarkerPart>(), "legacy blueprints stay Part-free");
            Assert.True(first.AddEntity(fixture, 2, 2)); DueToRelapse(manager);
            Assert.AreSame(first, manager.GetZone(Tine));
        }

        [Test] public void UnlistedBlueprintWithoutAMarkerDoesNotRetain()
        {
            var manager = NewManager(); var first = manager.GetZone(Tine);
            Assert.True(first.AddEntity(scope.Factory.CreateEntity("TinkersForge"), 2, 2)); DueToRelapse(manager);
            Assert.AreNotSame(first, manager.GetZone(Tine));
        }

        [Test] public void AMarkerInAnotherVillageDoesNotHoldThisOne()
        {
            var manager = NewManager(); var tine = manager.GetZone(Tine); var gantry = manager.GetZone(Gantry);
            Install(gantry, "TinkersForge", retain: true); DueToRelapse(manager);
            Assert.AreNotSame(tine, manager.GetZone(Tine)); Assert.AreSame(gantry, manager.GetZone(Gantry));
        }

        [Test] public void ARetainingEntityThatLeavesTheZoneEndsRetention()
        {
            var manager = NewManager(); var first = manager.GetZone(Tine); var machine = Install(first, "TinkersForge", retain: true);
            Assert.True(first.RemoveEntity(machine)); DueToRelapse(manager); Assert.AreNotSame(first, manager.GetZone(Tine));
        }

        [Test] public void OneRetainingMarkerAmongSeveralIsEnough()
        {
            var manager = NewManager(); var first = manager.GetZone(Tine);
            Install(first, "TinkersForge", retain: false, x: 2, y: 2); Install(first, "AlchemyStill", retain: true, x: 3, y: 2); Install(first, "TinkersForge", retain: false, x: 4, y: 2);
            DueToRelapse(manager); Assert.AreSame(first, manager.GetZone(Tine));
        }

        // ── saves ──

        [Test] public void MarkerPartRoundTripsPublicFields()
        {
            var entity = scope.Factory.CreateEntity("TinkersForge");
            entity.AddPart(new InstallationMarkerPart { InstallationId = "row-7", Origin = "Relocated", Revision = 4, RetainZone = true });
            var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(entity); var marker = copy.GetPart<InstallationMarkerPart>();
            Assert.NotNull(marker); Assert.AreEqual("row-7", marker.InstallationId); Assert.AreEqual("Relocated", marker.Origin);
            Assert.AreEqual(4, marker.Revision); Assert.True(marker.RetainZone); Assert.True(InstallationMarkerPart.Retains(copy));
        }

        [Test] public void SaveWithoutAMarkerLoadsAndIsNotRetained()
        {
            var entity = scope.Factory.CreateEntity("TinkersForge"); var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(entity);
            Assert.False(copy.HasPart<InstallationMarkerPart>()); Assert.False(InstallationMarkerPart.Retains(copy));
        }

        [Test] public void AMarkerAddedAfterLoadStartsRetaining()
        {
            var manager = NewManager(); var first = manager.GetZone(Tine); var machine = scope.Factory.CreateEntity("TinkersForge"); Assert.True(first.AddEntity(machine, 2, 2));
            DueToRelapse(manager); machine.AddPart(new InstallationMarkerPart { RetainZone = true });
            Assert.AreSame(first, manager.GetZone(Tine));
        }

        // ── observability ──

        [Test] public void ZoneRetainedDiagFiresWithReasonAndCount()
        {
            var manager = NewManager(); var first = manager.GetZone(Tine); Install(first, "TinkersForge", retain: true); Diag.ResetAll();
            DueToRelapse(manager); manager.GetZone(Tine);
            var record = Diag.Snapshot(50).Single(r => r.Category == "worldgen" && r.Kind == "ZoneRetained");
            Assert.That(record.PayloadJson, Does.Contain(Tine)); Assert.That(record.PayloadJson, Does.Contain("marker")); Assert.That(record.PayloadJson, Does.Contain("\"count\":1"));
        }

        [Test] public void NoRetentionDiagWhenNothingRetains()
        {
            var manager = NewManager(); manager.GetZone(Tine); Diag.ResetAll(); DueToRelapse(manager); manager.GetZone(Tine);
            Assert.IsEmpty(Diag.Snapshot(50).Where(r => r.Kind == "ZoneRetained"));
        }

        // ── adversarial: Retain never throws and never invents retention ──

        [Test] public void NullManagerUnknownZoneAndEmptyIdAreSafe()
        {
            Assert.False(InstallationMarkerPart.Retain(null, Tine));
            var manager = NewManager();
            Assert.False(InstallationMarkerPart.Retain(manager, null)); Assert.False(InstallationMarkerPart.Retain(manager, ""));
            Assert.False(InstallationMarkerPart.Retain(manager, "Overworld.1.1.0"), "never generated, so never cached");
            Assert.False(InstallationMarkerPart.Retains(null));
        }

        [Test] public void AMarkedEntityThatIsInNoZoneRetainsNothing()
        {
            var manager = NewManager(); var first = manager.GetZone(Tine);
            var loose = scope.Factory.CreateEntity("TinkersForge"); loose.AddPart(new InstallationMarkerPart { RetainZone = true });
            DueToRelapse(manager); Assert.AreNotSame(first, manager.GetZone(Tine));
        }

        [Test] public void RetentionReasonNamesTheSource()
        {
            var marked = scope.Factory.CreateEntity("TinkersForge"); marked.AddPart(new InstallationMarkerPart { RetainZone = true });
            Assert.AreEqual("marker", InstallationMarkerPart.RetentionReason(marked));
            Assert.AreEqual("legacy-blueprint", InstallationMarkerPart.RetentionReason(scope.Factory.CreateEntity("PeatOilStrainer")));
            Assert.AreEqual("", InstallationMarkerPart.RetentionReason(scope.Factory.CreateEntity("TinkersForge")));
        }
    }
}
