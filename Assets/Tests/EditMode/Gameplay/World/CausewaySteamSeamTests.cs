using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class CausewaySteamSeamTests
    {
        const string Surface = "Overworld.17.9.0", Below = "Overworld.17.9.1";

        [TestCase(false)]
        [TestCase(true)]
        public void FirstAccessFromEitherLevelFindsOnePhysicalSourceAndOnePassiveTrace(bool belowFirst)
        {
            using var scope = new HaulingContentScope();
            scope.Seed(64);
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
            if (belowFirst) Assert.NotNull(manager.GetZone(Below));
            var surface = manager.GetZone(Surface);
            var vent = surface.GetAllEntities().Single(e => e.BlueprintName == "SteamVent");
            StringAssert.Contains("fissure", vent.GetPart<ExaminablePart>().GetDescriptionText());
            Assert.IsFalse(vent.HasPart<SourceStockPart>());
            Assert.AreEqual(belowFirst, manager.CachedZones.ContainsKey(Below));
            var link = manager.GetConnections(Surface).Single(c => c.TargetZoneID == Below && c.Type == "StairsDown");
            var stairsDown = surface.GetCell(link.SourceX, link.SourceY).Objects.Single(e => e.HasPart<StairsDownPart>());
            Assert.IsTrue(stairsDown.HasTag("StairsDown"));
            var below = manager.GetZone(Below);
            var stairsUp = below.GetCell(link.TargetX, link.TargetY).Objects.Single(e => e.BlueprintName == "StairsUp");
            Assert.IsTrue(stairsUp.HasPart<StairsUpPart>());
            StringAssert.Contains("mineral stain", stairsUp.GetPart<ExaminablePart>().GetDescriptionText());
            Assert.IsFalse(stairsUp.HasPart<SourceStockPart>());
            Assert.IsFalse(stairsUp.HasPart<TileStateSourcePart>());
            Assert.IsFalse(below.GetAllEntities().Any(e => e.BlueprintName == "SteamVent"));
            Assert.AreEqual(2, manager.CachedZoneCount);
        }

        [Test]
        public void AnOlderWorldKeepsItsOriginalSurfaceAndCave()
        {
            using var scope = new HaulingContentScope();
            scope.Seed(64);
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
            SoddenDistrictIntegrationTests.Version(manager, 28);
            var surface = manager.GetZone(Surface);
            Assert.IsFalse(surface.GetAllEntities().Any(e => e.BlueprintName == "SteamVent"));
            var below = manager.GetZone(Below);
            Assert.IsFalse(below.GetAllEntities().Any(e => e.HasPart<StairsUpPart>()
                && e.GetPart<ExaminablePart>()?.GetDescriptionText()?.Contains("mineral stain") == true));
        }

        [Test]
        public void RemovedSurfaceSourceStaysAbsentAndCannotMarkANewCave()
        {
            using var scope = new HaulingContentScope();
            scope.Seed(64);
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
            var surface = manager.GetZone(Surface);
            var vent = surface.GetAllEntities().Single(e => e.BlueprintName == "SteamVent");
            surface.RemoveEntity(vent);
            manager.UnloadZone(Surface);
            Assert.AreSame(surface, manager.GetZone(Surface));
            var below = manager.GetZone(Below);
            Assert.IsFalse(below.GetAllEntities().Any(e => e.HasPart<StairsUpPart>()
                && e.GetPart<ExaminablePart>()?.GetDescriptionText()?.Contains("mineral stain") == true));
        }
    }
}
