using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ConnectedLegacyAdmissionReviewTests
    {
        private static void Restore(OverworldZoneManager manager, Entity world)
        {
            var restore = typeof(SpreadExplorationPlan).GetMethod("Restore", BindingFlags.NonPublic | BindingFlags.Static);
            var plan = restore.Invoke(null, new object[] { manager, world });
            typeof(OverworldZoneManager).GetProperty("Exploration").SetValue(manager, plan);
        }

        private static void SetMode(OverworldZoneManager manager, string mode)
        {
            if (mode == "fresh")
            {
                Assert.True(manager.Exploration.Enabled);
                Assert.GreaterOrEqual(manager.Exploration.Version, 11);
                Assert.False(string.IsNullOrEmpty(manager.Exploration.WorldKey));
                return;
            }

            var world = new Entity { BlueprintName = "World" };
            if (mode == "v10") world.Properties[SpreadExplorationPlan.PropertyKey] = "10|64|0";
            Restore(manager, world);
            Assert.AreEqual(mode == "v10", manager.Exploration.Enabled);
            Assert.IsEmpty(manager.Exploration.WorldKey);
            if (mode == "v10") Assert.AreEqual(10, manager.Exploration.Version);
        }

        [TestCase("missing")]
        [TestCase("v10")]
        [TestCase("fresh")]
        public void ColdGladePreservesOriginalWellAndCellarAccess(string mode)
        {
            using (var content = new HaulingContentScope())
            {
                content.Seed(64);
                var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
                SetMode(manager, mode);
                var zone = manager.GetZone(GleanersDistrict.SurfaceID);
                Assert.NotNull(zone);
                var well = zone.GetReadOnlyEntities().SingleOrDefault(e => e.GetProperty(GleanersDistrict.RoleKey) == "well");
                Assert.NotNull(well,
                    "Disabled legacy metadata must not suppress the prior well and cellar entrance by requiring a new world key.");
                Assert.AreEqual(mode == "fresh", well.HasPart<ConnectedSpreadSourcePart>());
                Assert.True(GleanersDistrict.CanBuildCellar(manager));
                var notice = zone.GetReadOnlyEntities().Single(e => e.GetProperty(GleanersDistrict.RoleKey) == "notice");
                Assert.AreEqual(mode == "fresh", notice.GetPart<ExaminablePart>().Text.Contains("batch pan"));
            }
        }

        [TestCase("missing")]
        [TestCase("v10")]
        [TestCase("fresh")]
        public void ReceivingHallAddsConnectedInkServiceOnlyForAnEnabledNewWorld(string mode)
        {
            using (var content = new HaulingContentScope())
            {
                content.Seed(64);
                var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
                SetMode(manager, mode);
                var zone = manager.GetZone("Overworld.12.12.0");
                Assert.NotNull(zone);
                Assert.NotNull(zone.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == "CurationIntakeIndex"));
                Assert.AreEqual(mode == "fresh", zone.GetReadOnlyEntities().Any(e => e.BlueprintName == "BotanicalInkDesk"));
                var ivrin = zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "CurationJuniorIndexer");
                Assert.AreEqual(mode == "fresh", ivrin.GetPart<InventoryPart>().Objects.Any(e => e.BlueprintName == "ShatteredRimeGrimoire"));
                Assert.AreEqual(mode == "fresh", ivrin.HasPart<TraderPart>());
            }
        }

        [TestCase("missing")]
        [TestCase("v10")]
        [TestCase("fresh")]
        public void OriginalSurfaceAuthorizesOnlyItsVersionOfCellarContent(string mode)
        {
            using (var content = new HaulingContentScope())
            {
                content.Seed(64);
                var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
                SetMode(manager, mode == "fresh" ? "fresh" : "v10");
                Assert.NotNull(manager.GetZone(GleanersDistrict.SurfaceID));
                Assert.True(GleanersDistrict.CanBuildCellar(manager));
                // A missing-manifest save may already hold the original district stair.
                if (mode == "missing") SetMode(manager, "missing");
                var zone = manager.GetZone(GleanersCellarBuilder.ZoneID);
                Assert.NotNull(zone);
                var stores = zone.GetReadOnlyEntities().Single(e => e.GetProperty(GleanersCellarBuilder.RoleKey) == "supplies");
                var clay = stores.GetPart<ContainerPart>().Contents.Where(e => e.BlueprintName == "FireClay").ToArray();
                Assert.AreEqual(2, clay.Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
                Assert.AreEqual(mode == "fresh", clay.Any(e => e.HasPart<ConnectedClayOriginPart>()));
                Assert.AreEqual(mode == "fresh" ? 2 : 0, zone.GetReadOnlyEntities().Count(e => e.BlueprintName == "SootrootCrop"),
                    "An old district stair authorizes its original cellar, not connected crop additions.");
            }
        }

        [Test]
        public void MalformedCurrentManifestIsRejectedInsteadOfBecomingLegacyGenerationAuthority()
        {
            using (var content = new HaulingContentScope())
            {
                var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
                var before = manager.Exploration;
                var world = new Entity { BlueprintName = "World" };
                world.Properties[SpreadExplorationPlan.PropertyKey] = "11|64|0";
                var failure = Assert.Throws<TargetInvocationException>(() => Restore(manager, world));
                Assert.IsInstanceOf<InvalidDataException>(failure.InnerException);
                Assert.AreSame(before, manager.Exploration);
                Assert.AreEqual(0, manager.CachedZoneCount);
            }
        }
    }
}
