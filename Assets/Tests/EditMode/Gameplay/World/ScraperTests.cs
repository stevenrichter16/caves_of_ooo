using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
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

        [Test] public void InterruptedTrayPickupBlocksASharedScrapeAndPreservesAnIndependentScrape()
        {
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            string id = Enumerable.Range(0, WorldMap.Width * WorldMap.Height)
                .Select(i => WorldMap.ToZoneID(i % WorldMap.Width, i / WorldMap.Width, 0))
                .First(zoneId => HeldPlaceZones.Classify(manager, zoneId) == HeldPlaceZoneClass.WildSurface);
            Zone.RemoveEntity(Actor);
            Zone = new Zone(id);
            for (int x = 8; x <= 17; x++)
                for (int y = 8; y <= 12; y++)
                    Place("Grass", x, y);
            Assert.True(Zone.AddEntity(Actor, 10, 10));
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { id, Zone } }, id, new Dictionary<string, List<ZoneConnection>>());
            SettlementRuntime.ActiveZone = Zone;

            var shared = Source("SteamVent", 13, 10);
            var independent = Source("FrostVent", 15, 10);
            var sharedStock = SourceStockPart.For(Zone, shared, WorldClock.CurrentTick);
            var independentStock = SourceStockPart.For(Zone, independent, WorldClock.CurrentTick);
            var tray = Carry("CampSettlingTray");
            var setDown = Actions(tray).Single(row => row.Name == "SetDownFixture" && !row.IsDisabled && row.Display.StartsWith("set down east"));
            Assert.True(Act(tray, setDown.Command));
            var trayCell = Zone.GetEntityCell(tray);
            var collector = tray.GetPart<SettlingTrayPart>();
            Clock.AdvanceClock(WorldClock.DayLengthTicks * 3 / 5);
            int now = WorldClock.CurrentTick;
            Assert.AreEqual(1, collector.HeldNow(Zone, now));
            Assert.Greater(sharedStock.AvailableAt(now), 1);
            var originalStock = (sharedStock.Available, sharedStock.LastTick, sharedStock.Capacity);
            var originalTray = (collector.Held, collector.Progress, collector.LastTick, collector.Element);
            int remainingIndependent = independentStock.AvailableAt(now) - 1;

            var rival = Factory.CreateEntity("Player");
            Assert.True(Zone.AddEntity(rival, 14, 10));
            var rivalPack = rival.GetPart<InventoryPart>();
            rivalPack.MaxWeight = 400;
            var scraper = Factory.CreateEntity("Scraper");
            Assert.True(rivalPack.AddObject(scraper));
            InventoryAction Quote(Entity site) => ScrapeRows(WorldInteractionSystem.GatherCellActions(site, Zone.GetEntityCell(site), rival)).Single();
            var sharedRow = Quote(shared);
            var independentRow = Quote(independent);
            Assert.False(sharedRow.IsDisabled || independentRow.IsDisabled);
            Assert.AreSame(scraper, sharedRow.Via);
            Assert.AreSame(scraper, independentRow.Via);

            bool staged = false, sharedSucceeded = false, independentSucceeded = false;
            Actor.AddPart(new LateFixturePickupProbe
            {
                Fixture = tray,
                Observe = () =>
                {
                    staged = Zone.GetEntityCell(tray) == null && Pack.Objects.Contains(tray);
                    sharedSucceeded = InventorySystem.PerformAction(rival, scraper, sharedRow.Command, Zone);
                    independentSucceeded = InventorySystem.PerformAction(rival, scraper, independentRow.Command, Zone);
                }
            });
            string pickup = WorldInteractionSystem.GatherActions(tray, Actor).Single(row => row.Name == "TakeUpFixture").Command;

            Assert.False(Act(tray, pickup));
            Assert.True(staged);
            Assert.False(sharedSucceeded);
            Assert.True(independentSucceeded, "a different finite source is outside the pending tray receipt");
            Assert.AreSame(trayCell, Zone.GetEntityCell(tray));
            Assert.True(HeldFixtureActions.IsPlayerHeld(tray));
            Assert.False(Pack.Objects.Contains(tray));
            Assert.AreEqual(originalTray, (collector.Held, collector.Progress, collector.LastTick, collector.Element));
            Assert.AreSame(sharedStock, Stock(shared));
            Assert.AreEqual(originalStock, (sharedStock.Available, sharedStock.LastTick, sharedStock.Capacity));
            Assert.AreSame(independentStock, Stock(independent));
            Assert.AreEqual(remainingIndependent, independentStock.Available);
            Assert.AreEqual(1, rivalPack.Objects.Where(item => item.BlueprintName == "RimeFlake").Sum(item => item.GetPart<StackerPart>()?.StackCount ?? 1));
            Assert.False(rivalPack.Objects.Any(item => item.BlueprintName == "VentScale"));
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

        // ───────── the source's own interact (C) menu: the scrape row needs no trip through the pack ─────────

        /// <summary>What the player is shown when they press C and a direction: the menu of the cell's target, built the one way the input handler builds it.</summary>
        List<InventoryAction> Menu(Entity target, int x, int y) => WorldInteractionSystem.GatherCellActions(target, Zone.GetCell(x, y), Actor);
        InventoryAction[] ScrapeRows(IEnumerable<InventoryAction> rows) => rows.Where(a => a.Name == "ScrapeSource").ToArray();
        /// <summary>Choosing the row, exactly as the world menu runs it: the row's own tool does the work, else the menu's target.</summary>
        bool Choose(InventoryAction row, Entity target) => InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(row.Via ?? target, row.Command), Actor, Zone).Success;
        string Noun(string reagent) => reagent == "VentScale" ? "vent scale" : reagent == "RimeFlake" ? "rime flake" : reagent == "BrineCrust" ? "brine crust" : "acid crust";

        [TestCase("SteamVent", "VentScale")] [TestCase("AshBed", "VentScale")] [TestCase("FrostVent", "RimeFlake")] [TestCase("IceSheet", "RimeFlake")]
        [TestCase("BrinePool", "BrineCrust")] [TestCase("AcidPool", "AcidCrust")] [TestCase("MirePool", "AcidCrust")]
        public void TheSourcesOwnMenuOffersToScrapeWithTheScraperYouCarry_SoThePackIsNeverOpened(string blueprint, string reagent)
        {
            var source = Source(blueprint, 11, 10); var scraper = Carry("Scraper"); var rows = ScrapeRows(Menu(source, 11, 10));
            Assert.AreEqual(1, rows.Length, string.Join(" / ", Menu(source, 11, 10).Select(a => a.Display))); var row = rows[0];
            StringAssert.Contains("scrape", row.Display); StringAssert.Contains(Noun(reagent), row.Display); StringAssert.Contains(source.GetDisplayName(), row.Display);
            Assert.AreSame(scraper, row.Via, "the carried scraper does it: nothing is picked from the pack"); Assert.AreEqual(ActionTime.OneAction, row.Time, "it costs one action, like the pack row"); Assert.False(row.IsDisabled);
            StringAssert.Contains("rings", row.Hint, "the row says it is loud before you choose it");
            Assert.True(Choose(row, source), string.Join("|", MessageLog.GetRecent(3)));
            Assert.AreEqual(1, Got(reagent)); Assert.True(Pack.Objects.First(o => o.BlueprintName == reagent).HasTag("NoTrade"));
            Assert.AreEqual(Stock(source).Capacity - 1, Stock(source).Available); Assert.True(Heard(), "it rings just as the pack row's does"); Assert.True(Pack.Objects.Contains(scraper), "the scraper is not spent");
            var record = DiagQuery.Apply(new DiagQuery.Filter { Category = "furniture", Kind = "SourceScraped", Limit = 5 }).Records; Assert.AreEqual(1, record.Count);
        }

        [Test] public void NoScraperNoRow_AndNoRowForASourceTwoCellsOff_OrANonSource_Counter()
        {
            var vent = Source("SteamVent", 11, 10); Assert.IsEmpty(ScrapeRows(Menu(vent, 11, 10)), "nothing carried");
            var onGround = Factory.CreateEntity("Scraper"); Assert.True(Zone.AddEntity(onGround, 9, 10)); Assert.IsEmpty(ScrapeRows(Menu(vent, 11, 10)), "a scraper lying on the ground does nothing");
            Carry("Scraper"); Assert.AreEqual(1, ScrapeRows(Menu(vent, 11, 10)).Length, "carried, beside the vent: the row");
            var far = Source("SteamVent", 12, 10); Assert.IsEmpty(ScrapeRows(Menu(far, 12, 10)), "a vent two cells off is not beside you; the look cursor can point there but the row does not follow");
            var chest = Place("Chest", 10, 11); Assert.IsEmpty(ScrapeRows(Menu(chest, 10, 11)), "an ordinary object offers nothing to scrape");
            Assert.IsEmpty(ScrapeRows(Menu(null, 9, 9)), "an empty cell");
        }

        [Test] public void ThePoolYouPouredOffersNothing_Counter()
        {
            Carry("Scraper"); var poured = Pool(8, "brine", 11, 10); Assert.IsEmpty(ScrapeRows(Menu(poured, 11, 10)), "a flask-made pool is not a source");
        }

        [Test] public void TheRowIsForTheCellYouOpened_NotForWhicheverSourceIsNearest_Counter()
        {
            var steam = Source("SteamVent", 11, 10); var frost = Source("FrostVent", 10, 11); var scraper = Carry("Scraper");
            Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, 10, 10)); var nearest = ProofingSources.Find(Zone, Zone.GetEntityCell(Actor), out _);
            var chosen = nearest == steam ? frost : steam; var cell = Zone.GetEntityCell(chosen); var rows = ScrapeRows(Menu(chosen, cell.X, cell.Y)); Assert.AreEqual(1, rows.Length);
            Assert.True(Choose(rows[0], chosen)); Assert.AreEqual(chosen == steam ? "VentScale" : "RimeFlake", Pack.Objects.Single(o => o.BlueprintName == "VentScale" || o.BlueprintName == "RimeFlake").BlueprintName, "the reagent of the vent you chose");
            Assert.AreEqual(1, Stock(chosen).Capacity - Stock(chosen).Available, "drawn from the chosen vent"); Assert.IsNull(Stock(nearest), "the nearer one was never touched");
        }

        [Test] public void ALootItemLyingOnTheSourceDoesNotHideTheRow()
        {
            var vent = Source("SteamVent", 11, 10); var item = Place("Scraper", 11, 10); Carry("Scraper");
            Assert.AreEqual(1, ScrapeRows(Menu(item, 11, 10)).Length, "the menu of the item on the vent still offers to scrape the vent"); Assert.AreEqual(1, ScrapeRows(Menu(vent, 11, 10)).Length);
        }

        [Test] public void AMultiCellPoolIsOneStockWhicheverCellYouOpen()
        {
            var pool = new Entity[2, 2]; for (int i = 0; i < 2; i++) for (int j = 0; j < 2; j++) pool[i, j] = Source("BrinePool", 11 + i, 9 + j); var scraper = Carry("Scraper");
            var near = ScrapeRows(Menu(pool[0, 1], 11, 10)); Assert.AreEqual(1, near.Length); Assert.IsEmpty(ScrapeRows(Menu(pool[1, 1], 12, 10)), "two cells from you");
            Assert.True(Choose(near[0], pool[0, 1])); var holders = pool.Cast<Entity>().Where(e => Stock(e) != null).ToList(); Assert.AreEqual(1, holders.Count); Assert.AreSame(pool[0, 0], holders[0], "the shared stock lives on the pool's anchor cell");
            var other = ScrapeRows(Menu(pool[0, 0], 11, 9)); Assert.True(Choose(other[0], pool[0, 0])); Assert.AreEqual(Stock(pool[0, 0]).Capacity - 2, Stock(pool[0, 0]).Available);
        }

        [Test] public void ASpentSourceListsADisabledRow_AndAnotherDayBringsTheRowBack()
        {
            var source = Source("SteamVent", 11, 10); var scraper = Carry("Scraper"); int capacity = SourceStockPart.CapacityFor(Zone, source);
            for (int i = 0; i < capacity; i++) { var row = ScrapeRows(Menu(source, 11, 10)).Single(); Assert.False(row.IsDisabled, "scrape " + i); Assert.True(Choose(row, source)); }
            var spent = ScrapeRows(Menu(source, 11, 10)).Single(); Assert.True(spent.IsDisabled); Assert.AreEqual("spent", spent.DisabledCode); StringAssert.Contains("a little more each day", spent.DisabledReason); Assert.AreEqual(capacity, Got("VentScale"));
            Clock.AdvanceClock(WorldClock.DayLengthTicks + 1); var back = ScrapeRows(Menu(source, 11, 10)).Single(); Assert.False(back.IsDisabled); Assert.True(Choose(back, source)); Assert.AreEqual(capacity + 1, Got("VentScale"));
        }

        [Test] public void ARowGoneStaleIsRefusedAndChangesNothing_TheSourceWalkedFromOrRemoved()
        {
            var source = Source("SteamVent", 11, 10); var scraper = Carry("Scraper"); var stock = SourceStockPart.For(Zone, source, WorldClock.CurrentTick); int before = stock.Available;
            var row = ScrapeRows(Menu(source, 11, 10)).Single(); Actor.IntProperties.Remove(Noise.Property);
            Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, 18, 10)); Assert.False(Choose(row, source), "you have walked away since the menu was drawn"); Assert.AreEqual(before, stock.Available); Assert.AreEqual(0, Got("VentScale")); Assert.False(Heard());
            Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, 10, 10)); Zone.RemoveEntity(source); Assert.False(Choose(row, source), "the source is gone"); Assert.AreEqual(0, Got("VentScale")); Assert.False(Heard());
        }

        [Test] public void ACommandNamingSomethingThatIsNotASourceOrIsMalformedIsRefused_Counter()
        {
            Source("SteamVent", 11, 10); var scraper = Carry("Scraper"); var chest = Place("Chest", 10, 11); var stock0 = Zone.GetReadOnlyEntities().Count(e => e.GetPart<SourceStockPart>() != null);
            foreach (string command in new[] { "ScrapeSource|" + System.Uri.EscapeDataString(chest.ID), "ScrapeSource|" + System.Uri.EscapeDataString(Actor.ID), "ScrapeSource|", "ScrapeSource|%zz", "ScrapeSource|no-such-id", "ScrapeSource|" + System.Uri.EscapeDataString(scraper.ID) })
            {
                int before = Diag.Snapshot(500).Count(r => r.Kind == "ScraperRefused");
                Assert.False(Act(scraper, command), command);
                var refusals = Diag.Snapshot(500).Where(r => r.Kind == "ScraperRefused").ToList();
                Assert.AreEqual(before + 1, refusals.Count, "a refusal is recorded for " + command + " (a crash further on records nothing)");
                Assert.True(refusals.All(r => r.PayloadJson.Contains("no-source")), "refused for the intended reason: " + command);
            }
            Assert.AreEqual(0, Got("VentScale")); Assert.False(Heard()); Assert.AreEqual(stock0, Zone.GetReadOnlyEntities().Count(e => e.GetPart<SourceStockPart>() != null), "no stock was made for a thing that is not a source");
        }

        [Test] public void AStunnedPlayerIsOfferedNoRowAndAnInterruptedCellScrapeChangesNothing()
        {
            var source = Source("SteamVent", 11, 10); var scraper = Carry("Scraper"); var row = ScrapeRows(Menu(source, 11, 10)).Single();
            FailAfter(); Assert.False(Choose(row, source)); Assert.AreEqual(0, Got("VentScale")); Assert.False(Heard()); Assert.AreEqual(0, Stock(source) == null ? 0 : Stock(source).Capacity - Stock(source).Available, "an interrupted scrape draws nothing");
            Actor.RemovePart(Actor.GetPart<FiftyWorldThrowAfterPart>()); Assert.True(Actor.ApplyEffect(new StunnedEffect(duration: 5)), "fixture: the stun lands"); Assert.True(Actor.GetPart<StatusEffectsPart>().IsActionBlocked(), "fixture: stunned");
            Assert.IsEmpty(ScrapeRows(Menu(source, 11, 10)), "no row while you cannot act");
        }

        [Test] public void TheCompositionKeepsTheUnderfootRowsTheMenuAlwaysHad_AndOnlyAddsTheScrapeRowBeside()
        {
            var rug = Place("Chest", 10, 10); rug.Tags["UnderfootInteractable"] = ""; var other = Place("Scraper", 10, 10);
            var rows = WorldInteractionSystem.GatherCellActions(other, Zone.GetCell(10, 10), Actor); Assert.AreEqual(1, rows.Count(a => a.Name == "Underfoot"), "the underfoot row is still there"); Assert.IsEmpty(ScrapeRows(rows));
            Source("SteamVent", 11, 10); Carry("Scraper"); rows = WorldInteractionSystem.GatherCellActions(other, Zone.GetCell(10, 10), Actor); Assert.IsEmpty(ScrapeRows(rows), "the scrape row belongs to the vent's own cell, not to the cell you stand in");
        }

        [Test] public void TheInputHandlerBuildsTheCMenuThroughTheOneCompositionThatCarriesTheRow()
        {
            string text = System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath, "Scripts/Presentation/Input/InputHandler.cs"));
            StringAssert.Contains("WorldInteractionSystem.GatherCellActions(", text);
            StringAssert.DoesNotContain("AppendIgnitionActions(", text, "the handler must not hand-compose the rows, or a new row is dropped from the live menu"); StringAssert.DoesNotContain("AppendUnderfootActions(", text, "the details reader once rebuilt the menu by hand and lost the tool rows");
        }

        [Test] public void TheReaderSaysWhatItDoesAndThatItIsLoud()
        {
            var scraper = Carry("Scraper"); var l = new List<string>(); UseGuides.Collect(scraper, null, null, Actor, l); string text = string.Join(" ", l);
            StringAssert.Contains("the same supply a settling tray there draws on", text); StringAssert.Contains("rings through the stone", text); StringAssert.Contains("(C)", text, "the reader says the source's own interact menu is the way in");
            var state = new List<string>(); StateReader.Collect(scraper, true, Zone, null, Actor, state); Assert.IsEmpty(state, "the scraper has no state of its own: its how-to is in the use block");
        }

        [Test] public void TheScraperIsRegisteredStockedAndSavedWithoutFields()
        {
            var item = Factory.CreateEntity("Scraper"); Assert.NotNull(item.GetPart<ScraperPart>()); Assert.True(item.GetPart<PhysicsPart>().Takeable);
            Assert.True(System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")).Contains("\"Scraper\""));
            Assert.NotNull(PartRoundTripHelper.RoundTripEntityViaTokenGraph(item).GetPart<ScraperPart>());
        }
    }
}
