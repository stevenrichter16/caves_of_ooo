using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The scraper (Docs/MACHINE-INVENTED-USES.md 3u): one level-one reagent at once from a source's own shared stock, loudly. It is the settling tray's quick cousin and the way cold is carried out of the deep (the frost
    /// vents and ice sheets are underground, where a camp kit cannot be set). It spends only time and noise, shares the source's finite supply with every tray, and what it takes cannot be sold.
    /// </summary>
    public sealed class ScraperTests : FiftyWorldFixture
    {
        [SetUp] public void SetupScraper() { ProofingRegistry.ResetForTests(); Diag.ResetAll(); MessageLog.Clear(); Pack.MaxWeight = 1000; Actor.IntProperties.Remove(Noise.Property); }
        [TearDown] public void TearScraper() { ProofingRegistry.ResetForTests(); }

        Entity Source(string blueprint = "SteamVent", int x = 12, int y = 10) => Place(blueprint, x, y);
        InventoryAction[] Rows(Entity scraper) => Actions(scraper).Where(a => a.Name == "ScrapeSource").ToArray();
        int Got(string blueprint) => Pack.Objects.Where(o => o.BlueprintName == blueprint).Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1);
        SourceStockPart Stock(Entity source) => source.GetPart<SourceStockPart>();
        bool Heard() => Noise.Recent(Actor, 1);

        [TestCase("SteamVent", "VentScale")] [TestCase("AshBed", "VentScale")] [TestCase("FrostVent", "RimeFlake")] [TestCase("IceSheet", "RimeFlake")]
        [TestCase("BrinePool", "BrineCrust")] [TestCase("AcidPool", "AcidCrust")] [TestCase("MirePool", "AcidCrust")]
        public void EachSourceGivesItsElementsReagentAtOnce_AndItIsFreeUnsellableAndLoud(string blueprint, string reagent)
        {
            var source = Source(blueprint); var scraper = Carry("Scraper"); var rows = Rows(scraper); Assert.AreEqual(1, rows.Length, string.Join(" / ", Actions(scraper).Select(a => a.Display)));
            StringAssert.Contains("scrape", rows[0].Display); StringAssert.Contains(reagent == "VentScale" ? "vent scale" : reagent == "RimeFlake" ? "rime flake" : reagent == "BrineCrust" ? "brine crust" : "acid crust", rows[0].Display);
            StringAssert.Contains("rings", rows[0].Hint + " rings");
            Assert.True(Act(scraper, rows[0].Command), string.Join("|", MessageLog.GetRecent(3)));
            Assert.AreEqual(1, Got(reagent)); var made = Pack.Objects.First(o => o.BlueprintName == reagent); Assert.True(made.HasTag("NoTrade")); Assert.False(TradeSystem.CanBeTraded(made, Actor, Actor, "Sell"));
            Assert.AreEqual(Stock(source).Capacity - 1, Stock(source).Available, "one drawn from the source's stock"); Assert.True(Heard(), "the scrape rang through the stone");
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "furniture", Kind = "SourceScraped", Limit = 5 }).Records.Count); Assert.True(Pack.Objects.Contains(scraper), "the scraper itself is not spent");
        }

        [Test] public void NothingToScrapeHereMeansNoRow_AndThreeCellsOutIsOutOfReach_Counter()
        {
            var scraper = Carry("Scraper"); Assert.IsEmpty(Rows(scraper)); Source("SteamVent", 13, 10); Assert.IsEmpty(Rows(scraper), "three cells away is out of reach"); Source("SteamVent", 12, 10); Assert.AreEqual(1, Rows(scraper).Length);
        }

        [Test] public void APoolYouPouredIsNotASourceForTheScraperEither_Counter()
        {
            var scraper = Carry("Scraper"); Pool(8, "brine", 12, 10); Assert.IsEmpty(Rows(scraper));
        }

        [Test] public void ItMustBeCarriedAndAGroundScraperOffersNothing_Counter()
        {
            Source(); var lying = Factory.CreateEntity("Scraper"); Assert.True(Zone.AddEntity(lying, 11, 10)); Assert.IsEmpty(Rows(lying));
        }

        [Test] public void ASourceGivesAsMuchAsItsCapacityThenIsSpent_AndANewDayBringsOneBack()
        {
            var source = Source(); var scraper = Carry("Scraper"); int capacity = SourceStockPart.CapacityFor(Zone, source);
            for (int i = 0; i < capacity; i++) Assert.True(Act(scraper, Rows(scraper).Single().Command), "scrape " + i);
            var row = Rows(scraper).Single(); Assert.True(row.IsDisabled); Assert.AreEqual("spent", row.DisabledCode); StringAssert.Contains("a little more each day", row.DisabledReason);
            Assert.False(Act(scraper, "ScrapeSource"), "a refused scrape"); Assert.AreEqual(capacity, Got("VentScale"));
            Clock.AdvanceClock(WorldClock.DayLengthTicks + 1); Assert.False(Rows(scraper).Single().IsDisabled, "a day later there is one more"); Assert.True(Act(scraper, "ScrapeSource")); Assert.AreEqual(capacity + 1, Got("VentScale"));
        }

        [Test] public void AScrapeIsHeardAndARefusedOneIsNot()
        {
            var source = Source(); var scraper = Carry("Scraper"); var stock = SourceStockPart.For(Zone, source, WorldClock.CurrentTick); stock.Available = 0; stock.LastTick = WorldClock.CurrentTick;
            Actor.IntProperties.Remove(Noise.Property); Assert.False(Act(scraper, "ScrapeSource")); Assert.False(Heard(), "nothing was struck, nothing rang");
            stock.Available = 1; Assert.True(Act(scraper, "ScrapeSource")); Assert.True(Heard());
        }

        [Test] public void AnInterruptedScrapeTakesNothingFromTheSourceGivesNothingAndMakesNoNoise()
        {
            var source = Source(); var scraper = Carry("Scraper"); var stock = SourceStockPart.For(Zone, source, WorldClock.CurrentTick); int before = stock.Available; FailAfter();
            Assert.False(Act(scraper, "ScrapeSource")); Assert.AreEqual(before, stock.Available); Assert.AreEqual(0, Got("VentScale")); Assert.False(Heard());
        }

        [Test] public void AFullPackRefusesWithoutDrawingOnTheSource_Counter()
        {
            var source = Source(); var scraper = Carry("Scraper"); var stock = SourceStockPart.For(Zone, source, WorldClock.CurrentTick); int before = stock.Available; Pack.MaxWeight = Pack.GetCarriedWeight();
            Assert.False(Act(scraper, "ScrapeSource")); Assert.AreEqual(before, stock.Available); Assert.AreEqual(0, Got("VentScale"));
        }

        [Test] public void AMultiCellPoolIsOneSourceWhereverYouStand_Counter()
        {
            var pool = new Entity[2, 2]; for (int i = 0; i < 2; i++) for (int j = 0; j < 2; j++) pool[i, j] = Source("BrinePool", 12 + i, 9 + j);
            var scraper = Carry("Scraper"); Assert.True(Act(scraper, "ScrapeSource")); var holders = pool.Cast<Entity>().Where(e => Stock(e) != null).ToList();
            Assert.AreEqual(1, holders.Count, "one stock for the whole pool"); Assert.AreSame(pool[0, 0], holders[0]);
            Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, 18, 10)); Assert.IsEmpty(Rows(scraper), "fixture: out of reach from there");
            Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, 14, 11)); Assert.True(Act(scraper, "ScrapeSource")); Assert.AreEqual(1, pool.Cast<Entity>().Count(e => Stock(e) != null), "from the far side it is still the same one stock");
            Assert.AreEqual(Stock(pool[0, 0]).Capacity - 2, Stock(pool[0, 0]).Available);
        }

        [Test] public void TheReaderSaysWhatItDoesAndThatItIsLoud()
        {
            var scraper = Carry("Scraper"); var l = new List<string>(); StateReader.Collect(scraper, true, Zone, null, Actor, l); string text = string.Join(" ", l);
            StringAssert.Contains("the same supply a settling tray draws on", text); StringAssert.Contains("loud", text);
        }

        [Test] public void TheScraperIsRegisteredStockedAndSavedWithoutFields()
        {
            var item = Factory.CreateEntity("Scraper"); Assert.NotNull(item.GetPart<ScraperPart>()); Assert.True(item.GetPart<PhysicsPart>().Takeable);
            Assert.True(System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")).Contains("\"Scraper\""));
            Assert.NotNull(PartRoundTripHelper.RoundTripEntityViaTokenGraph(item).GetPart<ScraperPart>());
        }
    }
}
