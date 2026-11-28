using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class BrineRimStockTests : FiftyWorldFixture
    {
        const string Site = "Overworld.17.13.0";
        const string Crust = "BrineCrust";
        const string ScrapeLabel = "Scrape dry brine crust";
        const int WorldSeed = 65, X = 40, ApproachY = 13, RimY = 14, PoolY = 15;
        const int LastFairBeforeRain = 1799, Rain = 2390, Fair = 2400;

        OverworldZoneManager EnterRim()
        {
            var manager = OverworldZoneManager.CreateDetached(Factory, WorldSeed, true);
            Assert.True(Zone.RemoveEntity(Actor));
            Zone = manager.GetZone(Site);
            Assert.NotNull(Zone);
            manager.SetActiveZone(Zone);
            SettlementRuntime.ActiveZone = Zone;
            Assert.True(Zone.AddEntity(Actor, X, ApproachY));
            Zone.ForEachCell((cell, _, _) => { cell.Explored = true; cell.IsVisible = true; });
            return manager;
        }

        Entity Rim => Zone.GetCell(X, RimY).Objects.Single(owner => owner.BlueprintName == "SaltCrust");
        Entity NativePool => Zone.GetCell(X, PoolY).Objects.Single(owner => owner.BlueprintName == "BrinePool");
        static bool TryInstall(Zone zone, OverworldZoneManager manager)
            => (bool)typeof(BrineRimStockPart).GetMethod("TryInstall", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { zone, manager });

        InventoryAction[] ScrapeRows(int x = X, int y = RimY) => CellMenu(x, y)
            .Where(row => !row.IsDisabled && row.Display?.StartsWith(ScrapeLabel, StringComparison.OrdinalIgnoreCase) == true)
            .ToArray();

        int PackedCrust => Pack.Objects.Where(item => item.BlueprintName == Crust)
            .Sum(item => item.GetPart<StackerPart>()?.StackCount ?? 1);

        void AdvanceTo(int tick, bool raining)
        {
            Assert.That(tick, Is.GreaterThanOrEqualTo(Clock.TickCount));
            Clock.AdvanceClock(tick - Clock.TickCount);
            var weather = RegionalWeather.Current(Zone, Clock.TickCount, RegionalWeather.PolicyVersion);
            Assert.True(weather.Valid);
            Assert.That(weather.Raining, Is.EqualTo(raining));
        }

        [Test]
        public void NormalGenerationInstallsExactlyOneStockOnThePassableRim()
        {
            var manager = EnterRim();
            var rim = Rim;
            var stock = rim.GetPart<BrineRimStockPart>();
            Assert.NotNull(stock);
            Assert.False(stock.Spent);
            Assert.False(Zone.GetCell(X, RimY).BlocksMovement(Actor));
            Assert.False(Zone.GetCell(X, ApproachY).Occupants.Any(owner => owner.HasPart<LiquidPoolPart>()));
            Assert.That(NativePool.GetPart<LiquidPoolPart>().LiquidId, Is.EqualTo("brine"));

            Assert.False(TryInstall(Zone, manager));
            Assert.That(manager.GetZone(Site), Is.SameAs(Zone));
            Assert.That(Rim.GetPart<BrineRimStockPart>(), Is.SameAs(stock));
            Assert.That(Zone.GetReadOnlyEntities().Where(owner => owner.HasPart<BrineRimStockPart>()), Is.EqualTo(new[] { rim }));
            Assert.That(rim.Parts.Count(part => part is BrineRimStockPart), Is.EqualTo(1));
        }

        [TestCase("Overworld.16.13.0", "SaltCrust", "BrinePool")]
        [TestCase("Overworld.17.13.0", "SandstoneFloor", "BrinePool")]
        [TestCase("Overworld.17.13.0", "SaltCrust", "AcidPool")]
        public void NearbyOrLookalikeTerrainCannotAcquireTheStock(string address, string ground, string pool)
        {
            Zone.ZoneID = address;
            var manager = OverworldZoneManager.CreateDetached(Factory, WorldSeed, true);
            Place("Sand", X, ApproachY - 1);
            Place("Sand", X, ApproachY);
            Place("Sand", X, RimY);
            Place(ground, X, RimY);
            Place(pool, X, PoolY);

            Assert.False(TryInstall(Zone, manager));
            Assert.That(Zone.GetReadOnlyEntities().Any(owner => owner.HasPart<BrineRimStockPart>()), Is.False);
        }

        [Test]
        public void RainInvalidatesAQuotedScrapeAndFairWeatherReopensTheSameRim()
        {
            EnterRim();
            var stock = Rim.GetPart<BrineRimStockPart>();
            AdvanceTo(LastFairBeforeRain, raining: false);
            Assert.True(stock.IsWorkable(Zone));
            Assert.That(ScrapeRows(), Is.Empty, "The row needs a carried scraper.");
            Carry("Scraper");
            var stale = ScrapeRows().Single();

            AdvanceTo(Rain, raining: true);
            Assert.False(stock.IsWorkable(Zone));
            Assert.False(ChooseRow(stale, X, RimY));
            Assert.That(ScrapeRows(), Is.Empty);
            Assert.That(PackedCrust, Is.Zero);
            Assert.False(stock.Spent);

            AdvanceTo(Fair, raining: false);
            Assert.True(stock.IsWorkable(Zone));
            Assert.That(ScrapeRows(), Has.Length.EqualTo(1));
        }

        [TestCase(RimY)]
        [TestCase(ApproachY)]
        public void AWaterCoatingRefusesEvenAnAlreadyQuotedScrape(int wetY)
        {
            EnterRim();
            var stock = Rim.GetPart<BrineRimStockPart>();
            Carry("Scraper");
            AdvanceTo(Fair, raining: false);
            var stale = ScrapeRows().Single();
            Zone.TileState.WriteCoating(X, wetY, "water", 5);

            Assert.That(stock.IsWorkable(Zone), Is.EqualTo(wetY != RimY));
            Assert.False(ChooseRow(stale, X, RimY));
            Assert.That(ScrapeRows(), Is.Empty);
            Assert.That(PackedCrust, Is.Zero);
            Assert.False(stock.Spent);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void LateContextChangeReturnsOnlyTheAttemptedCrust(bool wet)
        {
            EnterRim();
            var rim = Rim;
            var stock = rim.GetPart<BrineRimStockPart>();
            var existing = Carry(Crust);
            existing.GetPart<StackerPart>().StackCount = 2;
            Carry("Scraper");
            AdvanceTo(Fair, raining: false);
            var otherPart = new LateChange();
            Actor.AddPart(new LateChange { Apply = () =>
            {
                if (wet) Zone.TileState.WriteCoating(X, RimY, "water", 5);
                else rim.AddPart(otherPart);
            } });

            Assert.False(ChooseRow(ScrapeRows().Single(), X, RimY));
            Assert.That(existing.GetPart<StackerPart>().StackCount, Is.EqualTo(2));
            Assert.That(PackedCrust, Is.EqualTo(2));
            Assert.False(stock.Spent);
            Assert.That(stock.IsWorkable(Zone), Is.EqualTo(!wet));
            if (!wet) Assert.That(rim.GetPart<LateChange>(), Is.SameAs(otherPart));
        }

        sealed class LateChange : Part
        {
            public override string Name => "BrineRimTestLateChange";
            internal Action Apply;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "AfterInventoryAction") Apply?.Invoke();
                return true;
            }
        }

        [Test]
        public void NativeSaltGroundKeepsItsArtAndShowsOnlyTheVisibleRimCue()
        {
            EnterRim();
            using var catalog = new FlatPixelGreyboxCatalog();
            var stock = Rim.GetPart<BrineRimStockPart>();
            AdvanceTo(Rain, raining: true);
            Assert.True(catalog.TryResolvePresentation(Zone, X, RimY, out var wet));
            AdvanceTo(Fair, raining: false);
            Assert.True(catalog.TryResolvePresentation(Zone, X, RimY, out var dry));
            stock.Spent = true;
            Assert.True(catalog.TryResolvePresentation(Zone, X, RimY, out var spent));

            Assert.That(dry.Owner, Is.SameAs(Rim));
            Assert.True(dry.LocalGround);
            Assert.NotNull(wet.ForegroundTile);
            Assert.NotNull(dry.ForegroundTile);
            Assert.NotNull(spent.ForegroundTile);
            Assert.That(dry.ForegroundTile, Is.Not.SameAs(wet.ForegroundTile));
            Assert.That(dry.ForegroundTile, Is.Not.SameAs(spent.ForegroundTile));
            Assert.That(dry.GroundTile, Is.SameAs(wet.GroundTile).And.SameAs(spent.GroundTile));
            Zone.GetCell(X, RimY).IsVisible = false;
            Assert.True(catalog.TryResolvePresentation(Zone, X, RimY, out var memory));
            Assert.That(memory.ForegroundTile, Is.Not.SameAs(dry.ForegroundTile).And.Not.SameAs(spent.ForegroundTile));
        }

        [Test]
        public void TheRimGivesOnePackUnitWithoutDrawingThePoolAndStaysSpentAfterSave()
        {
            var manager = EnterRim();
            var rim = Rim;
            var stock = rim.GetPart<BrineRimStockPart>();
            var pool = NativePool.GetPart<LiquidPoolPart>();
            int volume = pool.Volume;
            Carry("Scraper");
            AdvanceTo(Fair, raining: false);
            var row = ScrapeRows().Single();
            Assert.That(row.Time, Is.EqualTo(ActionTime.OneAction));
            Assert.That(ScrapeRows(X, ApproachY), Is.Empty);
            Assert.That(ScrapeRows(X, PoolY), Is.Empty);

            int capacity = Pack.MaxWeight;
            Pack.MaxWeight = Pack.GetCarriedWeight();
            Assert.False(ChooseRow(row, X, RimY));
            Assert.False(stock.Spent);
            Assert.That(PackedCrust, Is.Zero);
            Pack.MaxWeight = capacity;

            Assert.True(ChooseRow(ScrapeRows().Single(), X, RimY));
            Assert.That(PackedCrust, Is.EqualTo(1));
            Assert.True(stock.Spent);
            Assert.False(stock.IsWorkable(Zone));
            Assert.That(ScrapeRows(), Is.Empty);
            Assert.False(ChooseRow(row, X, RimY));
            Assert.That(PackedCrust, Is.EqualTo(1));
            Assert.That(NativePool.GetPart<LiquidPoolPart>(), Is.SameAs(pool));
            Assert.That(pool.Volume, Is.EqualTo(volume));
            Assert.That(pool.LiquidId, Is.EqualTo("brine"));

            var saved = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("brine-rim", "test", manager, Clock, Actor));
            var savedRim = saved.ZoneManager.GetZone(Site).GetCell(X, RimY).Objects.Single(owner => owner.ID == rim.ID);
            Assert.True(savedRim.GetPart<BrineRimStockPart>().Spent);
            Assert.That(saved.Player.GetPart<InventoryPart>().Objects.Count(item => item.BlueprintName == Crust), Is.EqualTo(1));
        }
    }
}
