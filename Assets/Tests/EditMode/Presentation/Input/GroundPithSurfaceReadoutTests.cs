using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class GroundPithSurfaceReadoutTests : FiftyWorldFixture
    {
        const int NearX = 11;
        const int GroundY = 10;
        const int FilmTurns = 4;

        [Test]
        public void VisibleReachableOilFilmNamesCarriedPithWithoutUsingIt()
        {
            var pith = Carry("PrismreedPith");
            var stack = pith.GetPart<StackerPart>();
            int beforeCount = stack?.StackCount ?? 1;
            var cell = OilFilm(NearX);
            int beforeTick = Clock.TickCount;

            string text = Surface(cell, Actor);

            StringAssert.Contains("on the ground", text.ToLowerInvariant());
            StringAssert.Contains("slip chance", text.ToLowerInvariant());
            StringAssert.Contains("wick", text.ToLowerInvariant());
            StringAssert.Contains("prismreed pith", text.ToLowerInvariant());
            Assert.AreEqual(FilmTurns, Zone.TileState.CoatingTurns(NearX, GroundY, "oil"));
            Assert.AreEqual(beforeTick, Clock.TickCount);
            CollectionAssert.Contains(Pack.Objects, pith);
            Assert.AreEqual(beforeCount, stack?.StackCount ?? 1);
        }

        [Test]
        public void OilOverAStockedPoolRetainsSurfaceFactsWithoutOfferingWicking()
        {
            Carry("PrismreedPith");
            var pool = Pool(5);
            var cell = OilFilm(NearX);

            string text = Surface(cell, Actor);

            StringAssert.Contains("slip chance", text.ToLowerInvariant());
            AssertNoWick(text);
            Assert.AreEqual(FilmTurns, Zone.TileState.CoatingTurns(NearX, GroundY, "oil"));
            Assert.AreEqual(5, pool.GetPart<LiquidPoolPart>().Volume);
        }

        [TestCase("fog")]
        [TestCase("unexplored")]
        [TestCase("distant")]
        [TestCase("without pith")]
        public void UnavailableWickingDoesNotLeakAnAction(string control)
        {
            if (control != "without pith") Carry("PrismreedPith");
            var cell = OilFilm(control == "distant" ? NearX + 1 : NearX);
            if (control == "fog") cell.IsVisible = false;
            if (control == "unexplored") cell.Explored = false;

            string text = Surface(cell, Actor);

            if (control == "fog" || control == "unexplored") Assert.IsNull(text);
            else StringAssert.Contains("slip chance", text.ToLowerInvariant());
            AssertNoWick(text);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StaleAttachedGroundDoesNotOfferWicking(bool replace)
        {
            Carry("PrismreedPith");
            var cell = OilFilm(NearX);
            var manager = OverworldZoneManager.CreateDetached(Factory, 73);
            manager.SetActiveZone(Zone);
            StringAssert.Contains("wick", Surface(cell, Actor).ToLowerInvariant());

            if (replace) manager.CachedZones[Zone.ZoneID] = new Zone(Zone.ZoneID);
            else manager.SetActiveZone(new Zone("another-current-zone"));

            Assert.IsNull(CellStatusReadout.GroundLine(Zone, cell));
            AssertNoWick(Surface(cell, Actor));
        }

        Cell OilFilm(int x)
        {
            Place("Grass", x, GroundY);
            var cell = Zone.GetCell(x, GroundY);
            cell.IsVisible = cell.Explored = true;
            Zone.TileState.WriteCoating(x, GroundY, "oil", FilmTurns);
            return cell;
        }

        string Surface(Cell cell, Entity viewer) => InventoryDecisionDetails.Surface(Zone, cell, viewer);

        static void AssertNoWick(string text)
        {
            StringAssert.DoesNotContain("wick", (text ?? "").ToLowerInvariant());
        }
    }
}
