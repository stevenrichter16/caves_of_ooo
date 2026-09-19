using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Breadth pass B.2 (Docs/BREADTH-PASS.md): Morrowfast stands on the Grovelands'
    /// edge, not the Stump, and its ground reads as what it shows — grove turf
    /// outdoors, a flagstone floor indoors, a house wall where a wall stands — while
    /// the blueprint (and so the voxel mapping) stays TepuiStone. Older saves are
    /// renamed when the cached zone is upgraded.
    /// </summary>
    public sealed class MorrowfastGroundNamesTests
    {
        private EntityFactory factory; private OverworldZoneManager manager; private Zone town; private MorrowfastSceneDefinition def;

        [SetUp] public void SetUp()
        {
            factory = GrovelandsCompositionTests.Factory(); manager = OverworldZoneManager.CreateDetached(factory, 64);
            town = manager.GetZone(MorrowfastSceneRuntime.ZoneID); def = MorrowfastSceneDefinition.Load();
            Assert.IsNotNull(def); Assert.IsNotNull(MorrowfastSceneRuntime.GetState(town), "the authored town is installed");
        }

        private Entity LandAt(MorrowfastSceneDefinition.CellSpec c) => town.GetCell(c.x, c.y).Objects.Single(o => o.HasTag(MorrowfastSceneRuntime.TerrainTag) && o.BlueprintName == "TepuiStone");
        private MorrowfastSceneDefinition.CellSpec First(System.Func<MorrowfastSceneDefinition.CellSpec, bool> kind) => def.cells.First(kind);

        [Test]
        public void OutdoorGround_IsGroveTurf_IndoorsFlagstone_WallsHouseWall()
        {
            var outside = LandAt(First(c => !c.water && !c.solid && !c.interior));
            Assert.AreEqual(MorrowfastSceneRuntime.GroundName, outside.GetDisplayName()); StringAssert.Contains("grass", outside.GetPart<ExaminablePart>().Text);
            var inside = LandAt(First(c => !c.water && !c.solid && c.interior));
            Assert.AreEqual(MorrowfastSceneRuntime.FloorName, inside.GetDisplayName());
            var wall = LandAt(First(c => !c.water && c.solid && c.opaque));
            Assert.AreEqual(MorrowfastSceneRuntime.WallName, wall.GetDisplayName());
        }

        [Test]
        public void NoMorrowfastLandCell_StillCallsItselfPinkStone_AndEveryOneIsStillTepuiStone()
        {
            int land = def.cells.Count(c => !c.water);
            var named = def.cells.Where(c => !c.water).Select(LandAt).ToList();
            Assert.AreEqual(land, named.Count);
            Assert.IsFalse(named.Any(e => e.GetDisplayName() == "pink stone"), "Morrowfast is not the Stump");
            Assert.IsTrue(named.All(e => e.BlueprintName == "TepuiStone"), "the voxel mapping keys on the blueprint and is untouched");
            StringAssert.DoesNotContain("stump", string.Join(" ", named.Select(e => e.GetPart<ExaminablePart>().Text)).ToLowerInvariant());
        }

        [Test]
        public void TheFellingSitesStone_IsStillPinkStone()
        {
            // Counter-check: only Morrowfast's authored terrain is renamed.
            var site = manager.GetZone(FellingSiteBuilder.ZoneID);
            Assert.IsTrue(site.GetAllEntities().Any(e => e.BlueprintName == "TepuiStone" && e.GetDisplayName() == "pink stone"));
        }

        [Test]
        public void AnOlderSave_IsRenamedWhenItsCachedTownIsUpgraded()
        {
            var cell = First(c => !c.water && !c.solid && !c.interior); var e = LandAt(cell);
            e.GetPart<RenderPart>().DisplayName = "pink stone"; e.GetPart<ExaminablePart>().Text = "old text";
            Assert.IsTrue(MorrowfastSceneRuntime.UpgradeCachedZone(town, factory));
            Assert.AreEqual(MorrowfastSceneRuntime.GroundName, LandAt(cell).GetDisplayName());
            Assert.AreEqual(MorrowfastSceneRuntime.GroundText, LandAt(cell).GetPart<ExaminablePart>().Text);
        }

        [Test]
        public void TheName_SurvivesAnEntityRoundTrip()
        {
            var e = LandAt(First(c => !c.water && !c.solid && c.interior));
            var loaded = PartRoundTripHelper.RoundTripEntity(e);
            Assert.AreEqual(MorrowfastSceneRuntime.FloorName, loaded.GetDisplayName());
        }

        [Test]
        public void StandingOutdoors_TheCellIsDescribedAsGroveTurf()
        {
            var c = First(x => !x.water && !x.solid && !x.interior && town.GetCell(x.x, x.y).Objects.Count == 1);
            var player = factory.CreateEntity("Player"); Assert.IsTrue(town.AddEntity(player, c.x, c.y));
            Assert.AreEqual("You see the grove turf.", WorldInteractionSystem.DescribeCell(town.GetCell(c.x, c.y)));
        }
    }
}
