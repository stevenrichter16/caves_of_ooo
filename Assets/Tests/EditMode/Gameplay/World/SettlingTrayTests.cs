using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The settling tray (Docs/MACHINE-PROOFING.md, "the settling tray"): one kit, set down within two cells of a place that gives off one of the four elements, that settles a level-one reagent of that element
    /// every half day up to four, drawing each from the source's own shared stock. Time is reconciled on use, never by a timer; reading it changes nothing; a source is what the world declares, not a name.
    /// What it catches can never be sold, and a poured pool is never a source, so nothing here makes money or a source from a carried liquid.
    /// </summary>
    public sealed class SettlingTrayTests : FiftyWorldFixture
    {
        OverworldZoneManager manager; string wildId; Zone wild; const int AX = 27, AY = 11, SX = 30, SY = 11;   // the actor stands three cells from the source; the tray set down east of the actor is two from it

        [SetUp] public void SetupTray()
        {
            PackableFixtureRegistry.ResetForTests(); ProofingRegistry.ResetForTests(); Diag.ResetAll(); MessageLog.Clear(); Clock = new TurnManager();
            manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            wildId = Enumerable.Range(0, WorldMap.Width * WorldMap.Height).Select(i => WorldMap.ToZoneID(i % WorldMap.Width, i / WorldMap.Width, 0))
                .First(id => HeldPlaceZones.Classify(manager, id) == HeldPlaceZoneClass.WildSurface);
            wild = new Zone(wildId);
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++) Assert.True(wild.AddEntity(Factory.CreateEntity("Grass"), x, y));
            Zone.RemoveEntity(Actor); Zone = wild; Assert.True(Zone.AddEntity(Actor, AX, AY));
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { wildId, wild } }, wildId, new Dictionary<string, List<ZoneConnection>>());
            SettlementRuntime.ActiveZone = wild; Pack.MaxWeight = 400;
        }
        [TearDown] public void TearDownTray() { PackableFixtureRegistry.ResetForTests(); ProofingRegistry.ResetForTests(); }

        // ── helpers ──

        /// <summary>A source placed in the world. By default its stock is made now and set to the largest capacity, so a test that needs four catches does not depend on the cell's hash; <paramref name="rich"/> false leaves it untouched (no stock yet), as a source nobody has drawn on is.</summary>
        Entity Source(string blueprint = "SteamVent", int x = SX, int y = SY, bool rich = true)
        {
            var e = Factory.CreateEntity(blueprint); Assert.NotNull(e, blueprint); Assert.True(wild.AddEntity(e, x, y));
            if (rich) { var stock = SourceStockPart.For(wild, e, WorldClock.CurrentTick); stock.Capacity = SourceStockPart.MaxCapacity; stock.Available = stock.Capacity; }
            return e;
        }
        InventoryAction[] SetDownRows(Entity kit) => Actions(kit).Where(a => a.Name == "SetDownFixture" && !a.IsDisabled).ToArray();
        static string Dir(InventoryAction a) => a.Display.StartsWith("set down ", StringComparison.Ordinal) ? a.Display.Substring(9).Split(' ')[0] : "";
        /// <summary>Walk the actor to (x, y), carry a tray, and set it down in the named direction through the real verb.</summary>
        Entity SetTray(int ax = AX, int ay = AY, string direction = "east")
        {
            wild.RemoveEntity(Actor); Assert.True(wild.AddEntity(Actor, ax, ay));
            var kit = Carry("CampSettlingTray"); var row = SetDownRows(kit).FirstOrDefault(r => Dir(r) == direction);
            Assert.NotNull(row, "a set-down row " + direction + " at (" + ax + "," + ay + "): " + string.Join(" / ", Actions(kit).Select(a => a.Display + (a.IsDisabled ? " [" + a.DisabledCode + "]" : ""))));
            Assert.True(Act(kit, row.Command), string.Join("|", MessageLog.GetRecent(3))); return kit;
        }
        SettlingTrayPart Part(Entity tray) => tray.GetPart<SettlingTrayPart>();
        void Days(double d) => Clock.AdvanceClock((int)(d * WorldClock.DayLengthTicks));
        List<InventoryAction> World(Entity fixture) => WorldInteractionSystem.GatherActions(fixture, Actor);
        InventoryAction TakeRow(Entity tray)
        {
            var c = wild.GetEntityCell(tray); var me = wild.GetEntityCell(Actor);
            if (Math.Max(Math.Abs(c.X - me.X), Math.Abs(c.Y - me.Y)) > 1) { wild.RemoveEntity(Actor); Assert.True(wild.AddEntity(Actor, c.X - 1, c.Y)); }
            return World(tray).Single(a => a.Name == "GatherTray");
        }
        int Got(string blueprint) => Pack.Objects.Where(o => o.BlueprintName == blueprint).Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1);
        string Read(Entity e) { var l = new List<string>(); StateReader.Collect(e, true, wild, wild.GetEntityCell(e), Actor, l); return string.Join(" ", l); }
        static string BandAt(int tick) => WorldClock.BandName(WorldClock.GetBand(tick), 0);
        static readonly string[] Collected = { "VentScale", "RimeFlake", "BrineCrust", "AcidCrust" };

        Cell Reveal(Entity owner)
        {
            var cell = wild.GetEntityCell(owner);
            cell.IsVisible = cell.Explored = true;
            return cell;
        }

        (bool shown, char glyph, string color) TrayCue(Entity tray)
        {
            var get = typeof(FlatPixelFallbackCatalog).Assembly.GetType("CavesOfOoo.Rendering.FieldOpportunityAppearance")
                ?.GetMethod("TryGet", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(get);
            object[] args = { wild, tray, '\0', null };
            return ((bool)get.Invoke(null, args), (char)args[2], (string)args[3]);
        }

        [Test]
        public void TrayCueProjectsOnlyWithTheExactSourceAndStockHolderInViewWithoutCreatingStock()
        {
            var nearest = Source("BrinePool", SX, SY, rich: false);
            var holder = Source("BrinePool", SX + 1, SY - 1, rich: false);
            var tray = SetTray();
            var trayCell = Reveal(tray);
            var nearCell = Reveal(nearest);
            var holderCell = Reveal(holder);
            Assert.That(ProofingSources.Find(wild, trayCell, out var kind), Is.SameAs(nearest));
            Assert.That(ProofingSources.StockHolder(wild, nearest, kind), Is.SameAs(holder));
            Assert.False(TrayCue(tray).shown);
            Assert.AreEqual("=", tray.GetPart<RenderPart>().RenderString);

            Days(0.6);
            var part = Part(tray);
            var checkpoint = (part.Held, part.Progress, part.LastTick, part.Element, Clock.TickCount);
            Assert.AreEqual(1, part.HeldNow(wild, Clock.TickCount));
            Assert.That(TrayCue(tray), Is.EqualTo((true, 'u', "&Y")));
            foreach (var cell in new[] { nearCell, holderCell, trayCell })
            {
                cell.IsVisible = false;
                Assert.False(TrayCue(tray).shown);
                cell.IsVisible = true;
                cell.Explored = false;
                Assert.False(TrayCue(tray).shown);
                cell.Explored = true;
                Assert.That(TrayCue(tray), Is.EqualTo((true, 'u', "&Y")));
            }
            foreach (var owner in new[] { nearest, holder })
            {
                var render = owner.GetPart<RenderPart>();
                Assert.True(render.Visible);
                render.Visible = false;
                Assert.False(TrayCue(tray).shown);
                render.Visible = true;
                Assert.That(TrayCue(tray), Is.EqualTo((true, 'u', "&Y")));
            }
            Assert.AreEqual(checkpoint, (part.Held, part.Progress, part.LastTick, part.Element, Clock.TickCount));
            Assert.IsNull(nearest.GetPart<SourceStockPart>());
            Assert.IsNull(holder.GetPart<SourceStockPart>());
            Assert.AreEqual(0, Got("BrineCrust"));
            Assert.AreEqual("=", tray.GetPart<RenderPart>().RenderString);
        }

        [Test]
        public void TrayCueForecastDisappearsWhenARivalGathersTheLastUnitInTheSameTick()
        {
            var source = Source();
            var stock = source.GetPart<SourceStockPart>();
            stock.Available = 1;
            var tray = SetTray();
            var rival = SetTray(AX, AY + 2);
            Reveal(source);
            var trayCell = Reveal(tray);
            Reveal(rival);
            Days(0.6);
            int tick = Clock.TickCount;
            Assert.That(TrayCue(tray), Is.EqualTo((true, 'u', "&Y")));
            Assert.That(TrayCue(rival), Is.EqualTo((true, 'u', "&Y")));
            Assert.AreEqual(1, stock.Available);
            Assert.AreEqual(0, Part(tray).Held);

            var gather = TakeRow(rival);
            var dirtyAt = new List<int>();
            var priorCellDirty = ZoneRenderHooks.CellDirtyCallback;
            try
            {
                ZoneRenderHooks.CellDirtyCallback = (x, y, _) =>
                {
                    if (x == trayCell.X && y == trayCell.Y) dirtyAt.Add(Clock.TickCount);
                };
                Assert.True(Act(rival, gather.Command));
            }
            finally
            {
                ZoneRenderHooks.CellDirtyCallback = priorCellDirty;
            }
            Assert.AreEqual(tick, Clock.TickCount);
            CollectionAssert.Contains(dirtyAt, tick, "Gathering invalidates the other tray's cell in the same turn.");
            Assert.AreEqual(0, stock.Available);
            Assert.AreEqual(1, Got("VentScale"));
            Assert.AreEqual(0, Part(tray).Held);
            Assert.False(TrayCue(tray).shown);
            Assert.False(TrayCue(rival).shown);
        }

        [Test]
        public void TrayCueShowsActualHeldCatchAfterItLosesTheSourceAndOnlyWhileStandingInView()
        {
            var source = Source();
            var tray = SetTray();
            var trayCell = Reveal(tray);
            var sourceCell = Reveal(source);
            Days(0.6);
            Assert.AreEqual(1, Part(tray).Reconcile(wild, Clock.TickCount));
            Assert.That(TrayCue(tray), Is.EqualTo((true, 'U', "&Y")));
            sourceCell.IsVisible = false;
            sourceCell.Explored = false;
            Assert.That(TrayCue(tray), Is.EqualTo((true, 'U', "&Y")));
            Assert.True(wild.RemoveEntity(source));
            Assert.That(TrayCue(tray), Is.EqualTo((true, 'U', "&Y")));
            trayCell.IsVisible = false;
            Assert.False(TrayCue(tray).shown);
            trayCell.IsVisible = true;
            Assert.True(Act(tray, World(tray).Single(row => row.Name == "TakeUpFixture").Command));
            Assert.AreEqual(1, Part(tray).Held);
            Assert.False(TrayCue(tray).shown);
        }

        // ── the data: four elements, four reagents, one kit ──

        [Test] public void EveryElementNamesAReagentItCatchesThatCarriesExactlyThatElementAtLevelOne()
        {
            Assert.AreEqual(4, ProofingRegistry.All.Count);
            foreach (var element in ProofingRegistry.All)
            {
                var reagent = Factory.CreateEntity(element.Collects); Assert.NotNull(reagent, element.Id + " collects " + element.Collects);
                CollectionAssert.AreEqual(new[] { element }, ProofingRegistry.ElementsOf(reagent), element.Id + ": the reagent carries only its own element");
                Assert.AreEqual(1, ProofingRegistry.Units(reagent, element), element.Id + ": the weakest reagent, one coat");
                Assert.True(reagent.HasTag("Reagent") && reagent.HasTag("Alchemy"), "an ordinary reagent");
            }
        }

        [Test] public void EachElementHasExactlyOneKindOfSource_AndTheKindsAreKnown()
        {
            foreach (var element in ProofingRegistry.All)
            {
                bool tile = !string.IsNullOrEmpty(element.SourceTile), liquid = !string.IsNullOrWhiteSpace(element.SourceLiquids);
                Assert.True(tile ^ liquid, element.Id + " must have a tile source or a liquid source, not both and not neither");
                if (tile) CollectionAssert.Contains(new[] { "heat", "cold" }, element.SourceTile);
                if (liquid) foreach (var id in element.SourceLiquids.Split(',')) Assert.NotNull(LiquidRegistry.Get(id.Trim()), element.Id + " names an unknown liquid '" + id + "'");
            }
            Assert.AreEqual(4, ProofingRegistry.All.Select(e => e.Collects).Distinct().Count(), "no two elements share a catch");
        }

        [Test] public void WhatTheTrayCatchesCanNeverBeSoldOrBought_Counter()
        {
            foreach (var bp in Collected)
            {
                var reagent = Factory.CreateEntity(bp);
                Assert.True(reagent.HasTag("NoTrade"), bp); Assert.False(TradeSystem.CanBeTraded(reagent, Actor, Actor, "Sell"), bp + " cannot be sold");
                Assert.AreEqual(0, reagent.GetPart<CommercePart>()?.Value ?? 0, bp + " is worth nothing in drams");
            }
            Assert.True(TradeSystem.CanBeTraded(Factory.CreateEntity("FireMoss"), Actor, Actor, "Sell"), "counter: an ordinary reagent still trades");
        }

        [Test] public void TheKitIsRegisteredStockedAndAShippedBlueprint()
        {
            var def = PackableFixtureRegistry.Get("camp-tray"); Assert.NotNull(def); Assert.AreEqual(PlacementCheck.OpenGround, def.GroundClass); Assert.False(def.Solid);
            Assert.AreEqual("camp-tray", CampKinds.Tray);
            var made = Factory.CreateEntity("CampSettlingTray"); Assert.NotNull(made.GetPart<SettlingTrayPart>()); Assert.True(made.GetPart<PhysicsPart>().Takeable); Assert.AreEqual("camp-tray", made.GetPart<PackableFixturePart>().Kind);
            Assert.True(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")).Contains("\"CampSettlingTray\""), "the Tinker stocks it");
        }

        // ── what is a source ──

        [TestCase("SteamVent", "heat")] [TestCase("AshBed", "heat")] [TestCase("FrostVent", "cold")] [TestCase("IceSheet", "cold")]
        [TestCase("BrinePool", "electric")] [TestCase("AcidPool", "acid")] [TestCase("MirePool", "acid")]
        public void TheWorldsOwnHazardsAreSourcesOfTheirDeclaredElement(string blueprint, string element)
        {
            var e = Factory.CreateEntity(blueprint); var kind = ProofingSources.ElementOf(e); Assert.NotNull(kind, blueprint); Assert.AreEqual(element, kind.Id);
        }

        [Test] public void ThingsThatOnlyLookLikeSourcesAreNot_Counters()
        {
            Assert.IsNull(ProofingSources.ElementOf(Factory.CreateEntity("Grass")), "ground");
            Assert.IsNull(ProofingSources.ElementOf(Factory.CreateEntity("PeatBog")), "a bog coats, it does not give off an element");
            var creature = Factory.CreateEntity("SteamVent"); creature.Tags["Creature"] = ""; Assert.IsNull(ProofingSources.ElementOf(creature), "a creature is never a source");
            var fire = Factory.CreateEntity("SteamVent"); fire.AddPart(new FuelPart()); Assert.IsNull(ProofingSources.ElementOf(fire), "a fire that burns fuel is not a free source: the tray never turns your timber into scale");
            var chilled = Factory.CreateEntity("SteamVent"); chilled.GetPart<TileStateSourcePart>().Cold = 1; Assert.IsNull(ProofingSources.ElementOf(chilled), "heat and cold at once is not a steady source of either");
            var cooled = Factory.CreateEntity("SteamVent"); cooled.GetPart<TileStateSourcePart>().Heat = 0; Assert.IsNull(ProofingSources.ElementOf(cooled), "a terrain that no longer seeds heat");
            var dry = Factory.CreateEntity("BrinePool"); dry.GetPart<LiquidPoolPart>().Volume = 0; Assert.IsNull(ProofingSources.ElementOf(dry), "a pool drawn dry");
            var water = Factory.CreateEntity("BrinePool"); water.GetPart<LiquidPoolPart>().LiquidId = "water"; Assert.IsNull(ProofingSources.ElementOf(water), "a pool of a liquid no element names");
        }

        [Test] public void APoolYouPouredIsNeverASource_SoACarriedLiquidMakesNoSourceOrStock_Counter()
        {
            foreach (var liquid in new[] { "brine", "acid", "bog-mire" }) Assert.IsNull(ProofingSources.ElementOf(Pool(8, liquid, 12, 12)), "poured " + liquid);
            Assert.NotNull(ProofingSources.ElementOf(Factory.CreateEntity("BrinePool")), "counter: the world's own brine pool is");
        }

        [Test] public void TheNearestSourceWithinTwoCellsIsFoundAndTiesGoToTheLowerID_AndThreeCellsIsOutOfReach()
        {
            var cell = wild.GetCell(28, 11); Assert.IsNull(ProofingSources.Find(wild, cell, out _), "nothing near");
            var far = Source("SteamVent", 31, 11); Assert.IsNull(ProofingSources.Find(wild, cell, out _), "three cells is out of reach");
            var two = Source("BrinePool", 30, 11); var found = ProofingSources.Find(wild, cell, out var kind); Assert.AreSame(two, found); Assert.AreEqual("electric", kind.Id);
            var one = Source("FrostVent", 29, 12); found = ProofingSources.Find(wild, cell, out kind); Assert.AreSame(one, found, "the nearer one wins over the one further out"); Assert.AreEqual("cold", kind.Id);
            var other = Source("AshBed", 27, 10); string lower = string.CompareOrdinal(one.ID, other.ID) < 0 ? one.ID : other.ID;
            found = ProofingSources.Find(wild, cell, out kind); Assert.AreEqual(lower, found.ID, "equally near: the lower ID, every time");
            wild.RemoveEntity(far); Assert.IsNotNull(ProofingSources.Find(wild, cell, out _));
        }

        // ── the stock a source holds ──

        [Test] public void ASourcesStockStartsFullAtAStableCapacityAndRefillsOneADay()
        {
            var vent = Source("SteamVent", SX, SY, rich: false); int now = WorldClock.CurrentTick; int expected = SourceStockPart.CapacityFor(wild, vent);
            Assert.That(expected, Is.InRange(SourceStockPart.MinCapacity, SourceStockPart.MaxCapacity)); Assert.AreEqual(expected, SourceStockPart.CapacityFor(wild, vent), "pure and stable");
            var stock = SourceStockPart.For(wild, vent, now); Assert.AreEqual(expected, stock.Capacity, "the stock is made at the capacity the readers assumed"); Assert.AreEqual(expected, stock.Available);
            Assert.AreSame(stock, SourceStockPart.For(wild, vent, now + 999), "asking again gives the same stock");
            stock.Available = 0; stock.LastTick = now;
            Assert.AreEqual(0, stock.AvailableAt(now + WorldClock.DayLengthTicks - 1)); Assert.AreEqual(1, stock.AvailableAt(now + WorldClock.DayLengthTicks)); Assert.AreEqual(Math.Min(2, expected), stock.AvailableAt(now + 2 * WorldClock.DayLengthTicks));
            Assert.AreEqual(expected, stock.AvailableAt(now + 99 * WorldClock.DayLengthTicks), "never more than its capacity");
            Assert.AreEqual(0, stock.Available, "reading changes nothing"); stock.Reconcile(now + 2 * WorldClock.DayLengthTicks + 5); Assert.AreEqual(Math.Min(2, expected), stock.Available);
        }

        [Test] public void CapacityDependsOnWhereTheSourceIs_NotOnWhichSourceYouAsk()
        {
            var seen = new HashSet<int>(); for (int x = 30; x < 50; x++) { var e = Source("SteamVent", x, 4, rich: false); seen.Add(SourceStockPart.CapacityFor(wild, e)); }
            Assert.Greater(seen.Count, 1, "twenty cells do not all hash to one capacity"); Assert.True(seen.All(c => c >= SourceStockPart.MinCapacity && c <= SourceStockPart.MaxCapacity));
        }

        [Test] public void TheLookOfASourceIsAHintNeverACount()
        {
            Assert.AreEqual("spent for now", SourceStockPart.Look(0, 4)); Assert.AreEqual("thin", SourceStockPart.Look(1, 4)); Assert.AreEqual("well drawn but not spent", SourceStockPart.Look(3, 4)); Assert.AreEqual("plentiful", SourceStockPart.Look(4, 4));
        }

        // ── setting it down ──

        [Test] public void ItStandsTwoCellsFromASourceThroughTheRealVerb()
        {
            Source(); var tray = SetTray(); var at = wild.GetEntityPosition(tray); Assert.AreEqual(AX + 1, at.x); Assert.AreEqual(2, Math.Max(Math.Abs(at.x - SX), Math.Abs(at.y - SY)), "two cells from the source");
            Assert.True(HeldFixtureActions.IsPlayerHeld(tray)); Assert.False(PackableFixturePart.IsInertKit(tray));
        }

        [Test] public void TheCellRightBesideASourceIsRefusedAsForEveryKit_Counter()
        {
            Source("BrinePool"); wild.RemoveEntity(Actor); Assert.True(wild.AddEntity(Actor, SX - 2, SY)); var kit = Carry("CampSettlingTray");   // a pool is a hazard owner at once; a vent only becomes one once the zone has ticked its tile state
            CollectionAssert.DoesNotContain(SetDownRows(kit).Select(Dir).ToList(), "east", "one cell from the source is refused");
            wild.RemoveEntity(Actor); Assert.True(wild.AddEntity(Actor, SX - 3, SY)); CollectionAssert.Contains(SetDownRows(kit).Select(Dir).ToList(), "east", "counter: two cells from it is fine");
        }

        [Test] public void TheSetDownRowsSayWhatTheTrayWouldCatchInEachCell_AndOtherKitsRowsAreUnchanged_Counter()
        {
            Source("FrostVent"); var kit = Carry("CampSettlingTray"); var rows = SetDownRows(kit);
            Assert.AreEqual("set down east (catches cold)", rows.Single(r => Dir(r) == "east").Display, "two cells from the vent: the cell says what it would settle");
            StringAssert.EndsWith("(nothing to catch here)", rows.Single(r => Dir(r) == "west").Display, "four cells away it says there is nothing");
            var awning = Carry("CampAwning"); Assert.AreEqual("set down east", SetDownRows(awning).Single(r => Dir(r) == "east").Display, "counter: another kit's rows carry no such hint");
        }

        [Test] public void MakeCampSetsTheTrayDownOnlyWhereASourceIsInReach_ElseItStaysInThePack()
        {
            var hearth = Carry("CampHearth"); var tray = Carry("CampSettlingTray"); var row = Actions(hearth).Single(a => a.Name == "MakeCamp"); Assert.True(Act(hearth, row.Command), string.Join("|", MessageLog.GetRecent(3)));
            Assert.False(Pack.Objects.Contains(hearth), "the hearth went down"); Assert.True(Pack.Objects.Contains(tray), "with nothing to settle from, the tray stays in the pack"); StringAssert.Contains("could not be set down", string.Join(" ", MessageLog.GetRecent(3)));
        }

        [Test] public void MakeCampSetsTheTrayDownWhenASourceIsInReach()
        {
            Source("AshBed"); var hearth = Carry("CampHearth"); var tray = Carry("CampSettlingTray"); var row = Actions(hearth).Single(a => a.Name == "MakeCamp"); Assert.True(Act(hearth, row.Command), string.Join("|", MessageLog.GetRecent(3)));
            Assert.False(Pack.Objects.Contains(tray), "the tray went down"); var at = wild.GetEntityPosition(tray); Assert.AreEqual(2, Math.Max(Math.Abs(at.x - SX), Math.Abs(at.y - SY))); Assert.False(PackableFixturePart.IsInertKit(tray));
        }

        string Guide(Entity e, Entity who = null) { var g = new System.Collections.Generic.List<string>(); UseGuides.Collect(e, null, null, who ?? Actor, g); return string.Join(" ", g); }

        [Test] public void ACarriedTrayIsAKitThatOffersNothingAndSaysWhatItIs_Counter()
        {
            var kit = Carry("CampSettlingTray"); Assert.IsEmpty(Actions(kit).Where(a => a.Name == "GatherTray")); StringAssert.Contains("Set it down", Guide(kit)); StringAssert.Contains("steam vent", Guide(kit));
            Assert.AreEqual(0, kit.GetPart<SettlingTrayPart>().Reconcile(wild, WorldClock.CurrentTick), "a kit never catches");
        }

        // ── catching, reading and taking: every element through the real verb ──

        [TestCase("SteamVent", "VentScale", "heat")] [TestCase("AshBed", "VentScale", "heat")] [TestCase("FrostVent", "RimeFlake", "cold")] [TestCase("IceSheet", "RimeFlake", "cold")]
        [TestCase("BrinePool", "BrineCrust", "electric")] [TestCase("AcidPool", "AcidCrust", "acid")] [TestCase("MirePool", "AcidCrust", "acid")]
        public void EachSourceGivesItsElementsReagent_AndThatReagentPressesIntoThatElementsProofing(string blueprint, string reagent, string element)
        {
            Source(blueprint); var tray = SetTray(); Days(1.1); Assert.True(Act(tray, TakeRow(tray).Command), string.Join("|", MessageLog.GetRecent(3)));
            Assert.AreEqual(2, Got(reagent), blueprint + ": two in a day and a tenth"); Assert.AreEqual(0, Collected.Where(c => c != reagent).Sum(Got), "and nothing of another element");
            var caught = Pack.Objects.First(o => o.BlueprintName == reagent); var kind = ProofingRegistry.ById(element);
            var flask = Flask(0); var press = Actions(caught).Single(a => a.Name == "PressProofing"); Assert.False(press.IsDisabled, press.DisabledReason); Assert.True(Act(caught, press.Command));
            Assert.AreEqual(kind.Liquid, flask.GetPart<LiquidVesselPart>().LiquidId, "it presses into that element's proofing"); Assert.AreEqual(1, flask.GetPart<LiquidVesselPart>().Volume, "level one: one coat");
        }

        [Test] public void ItCatchesOneEveryHalfDayUpToFour_AndReadingChangesNothing()
        {
            var vent = Source(); var tray = SetTray(); var part = Part(tray);
            Assert.True(TakeRow(tray).IsDisabled); Assert.AreEqual("empty", TakeRow(tray).DisabledCode); StringAssert.Contains("half day", TakeRow(tray).DisabledReason);
            Days(0.5); Assert.AreEqual(1, part.HeldNow(wild, WorldClock.CurrentTick)); Assert.AreEqual(0, part.Held, "reading never changes the tray"); StringAssert.Contains("gather the vent scale (1)", TakeRow(tray).Display);
            Days(1.5); Assert.AreEqual(4, part.HeldNow(wild, WorldClock.CurrentTick)); Days(10); Assert.AreEqual(4, part.HeldNow(wild, WorldClock.CurrentTick), "four at most");
            Assert.AreEqual(0, part.Held);
        }

        [Test] public void ReadingATrayNeverMakesOrChangesTheSourcesStock()
        {
            var vent = Source("SteamVent", SX, SY, rich: false); var tray = SetTray(); Days(1.1); Part(tray).HeldNow(wild, WorldClock.CurrentTick); Read(tray); TakeRow(tray); HeldFixtureReader.Status(new[] { tray });
            Assert.IsNull(vent.GetPart<SourceStockPart>(), "reading did not even make the source's stock"); Assert.AreEqual(0, Part(tray).Held); Assert.AreEqual(-1, Part(tray).LastTick);
        }

        [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void WhatTheReaderPromisesIsExactlyWhatTheGatherGives_NeverMoreThanTheSourceHolds(int capacity)
        {
            var vent = Source(); var stock = vent.GetPart<SourceStockPart>(); stock.Capacity = capacity; stock.Available = capacity; var tray = SetTray(); Days(5);
            int promised = Part(tray).HeldNow(wild, WorldClock.CurrentTick); Assert.AreEqual(Math.Min(capacity, SettlingTrayPart.MaxHeld), promised, "a source holding " + capacity + " promises that many, not four");
            StringAssert.Contains("(" + promised + ")", TakeRow(tray).Display); Assert.True(Act(tray, TakeRow(tray).Command)); Assert.AreEqual(promised, Got("VentScale"), "and gives exactly that"); Assert.AreEqual(capacity - promised, stock.Available);
        }

        [Test] public void AnUntouchedSourceWhoseCapacityIsThreePromisesThreeNotFour_AndTheGatherAgrees()
        {
            Entity vent = null; int ax = 0;
            for (int x = 34; x < 50 && vent == null; x++)
            {
                var candidate = Source("SteamVent", x, SY, rich: false);
                if (SourceStockPart.CapacityFor(wild, candidate) == SourceStockPart.MinCapacity) { vent = candidate; ax = x - 3; } else wild.RemoveEntity(candidate);
            }
            Assert.NotNull(vent, "some cell on this row hashes to the smallest capacity");
            var tray = SetTray(ax, SY, "east"); Days(5); Assert.IsNull(vent.GetPart<SourceStockPart>(), "nothing has drawn on it yet");
            Assert.AreEqual(SourceStockPart.MinCapacity, Part(tray).HeldNow(wild, WorldClock.CurrentTick), "three, not the four a pan could hold"); Assert.True(Act(tray, TakeRow(tray).Command));
            Assert.AreEqual(SourceStockPart.MinCapacity, Got("VentScale"), "and the gather gives exactly that"); Assert.AreEqual(0, vent.GetPart<SourceStockPart>().Available);
        }

        [Test] public void GatheringGivesRealReagentEmptiesTheTrayDrawsTheStockAndTheTimeKeepsRunning()
        {
            var vent = Source(); var tray = SetTray(); var part = Part(tray); Days(2.1);
            Assert.True(Act(tray, TakeRow(tray).Command), string.Join("|", MessageLog.GetRecent(3)));
            Assert.AreEqual(4, Got("VentScale")); Assert.AreEqual(0, part.Held); Assert.AreEqual("", part.Element, "an emptied pan holds no element (found by mutation: nothing else pinned it)"); StringAssert.Contains("take 4 vent scale", string.Join(" ", MessageLog.GetRecent(3)));
            var stock = vent.GetPart<SourceStockPart>(); Assert.NotNull(stock, "the source's stock exists once someone draws on it"); Assert.AreEqual(stock.Capacity - 4, stock.Available);
            var records = DiagQuery.Apply(new DiagQuery.Filter { Category = "furniture", Kind = "TrayGathered", Limit = 5 }).Records; Assert.AreEqual(1, records.Count); StringAssert.Contains("heat", records[0].PayloadJson);
            Days(0.5); Assert.AreEqual(1, part.HeldNow(wild, WorldClock.CurrentTick), "it settles again");
        }

        [Test] public void TwoTraysShareOneSourceAndItRefillsOneADay_AndNothingIsMadeFromNothing()
        {
            var vent = Source(); var a = SetTray(AX, AY, "east"); var b = SetTray(AX, AY + 2, "east");
            Days(3); Assert.True(Act(a, TakeRow(a).Command)); var stock = vent.GetPart<SourceStockPart>(); Assert.AreEqual(stock.Capacity - 4, stock.Available);
            stock.Available = 1;   // the source is nearly spent
            Assert.True(Act(b, TakeRow(b).Command)); Assert.AreEqual(0, stock.Available, "the second tray took what was left"); Assert.AreEqual(5, Got("VentScale"), "four from the first, one from the second, and none made from nothing");
            Days(0.6); Assert.AreEqual(0, Part(a).HeldNow(wild, WorldClock.CurrentTick), "a spent source gives a tray nothing yet");
            Days(1.0); Assert.AreEqual(1, stock.AvailableAt(WorldClock.CurrentTick), "one has come back a day later");
            Part(a).Reconcile(wild, WorldClock.CurrentTick); Part(b).Reconcile(wild, WorldClock.CurrentTick);
            Assert.AreEqual(1, Part(a).Held + Part(b).Held, "two trays, one catch: they share it"); Assert.AreEqual(0, stock.Available);
        }

        [Test] public void ASourceNeverGivesMoreThanItsCapacityPlusWhatItRefills_AndDifferentSourcesAreIndependent()
        {
            var vent = Source(); var vent2 = Source("AshBed", SX, SY + 4, rich: false); var trays = new[] { SetTray(AX, AY - 1, "east"), SetTray(AX, AY, "east"), SetTray(AX, AY + 1, "east") };
            Days(5); foreach (var t in trays) { var row = TakeRow(t); if (!row.IsDisabled) Assert.True(Act(t, row.Command)); }
            var stock = vent.GetPart<SourceStockPart>(); Assert.NotNull(stock); Assert.LessOrEqual(Got("VentScale"), stock.Capacity, "three trays and five days still cannot beat the supply"); Assert.AreEqual(stock.Capacity - Got("VentScale"), stock.Available);
            Assert.IsNull(vent2.GetPart<SourceStockPart>(), "another source is untouched and has its own stock");
        }

        [Test] public void TakingFromASourceNeverDrainsTheWorldsOwnPool_AndAGatheredBrineCrustIsNotTheBrine()
        {
            var pool = Source("BrinePool"); int volume = pool.GetPart<LiquidPoolPart>().Volume; var tray = SetTray(); Days(2.1); Assert.True(Act(tray, TakeRow(tray).Command));
            Assert.AreEqual(volume, pool.GetPart<LiquidPoolPart>().Volume, "the pool is a place, not a stock of liquid: nothing is drawn from it"); Assert.AreEqual(4, Got("BrineCrust"));
        }

        // ── one source is one stock, however many cells it is made of (review) ──

        Entity[,] Pool3x3(string blueprint = "BrinePool", int x0 = 30, int y0 = 10)
        {
            var cells = new Entity[3, 3]; for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) cells[i, j] = Source(blueprint, x0 + i, y0 + j, rich: false); return cells;
        }

        [TestCase(20, 20)]
        [TestCase(21, 20)]
        [TestCase(50, 20)]
        [TestCase(Zone.Width, Zone.Height)]
        public void LargeConnectedSourceKeepsOneStockFromEveryCornerAndAfterDrying(int width, int height)
        {
            var pool = new Entity[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    pool[x, y] = Source("BrinePool", x, y, rich: false);
            var element = ProofingRegistry.ById("electric");
            var anchor = pool[0, 0];
            var stock = SourceStockPart.For(wild, anchor, WorldClock.CurrentTick);
            stock.Available = 0;
            anchor.GetPart<LiquidPoolPart>().Volume = 0;

            foreach (var source in new[] { pool[0, height - 1], pool[width - 1, 0], pool[width - 1, height - 1] })
            {
                var holder = ProofingSources.StockHolder(wild, source, element);
                Assert.AreSame(anchor, holder, "one connected source must have the same holder from every edge");
                var resolved = SourceStockPart.For(wild, holder, WorldClock.CurrentTick);
                Assert.AreSame(stock, resolved);
                Assert.AreEqual(0, resolved.Available, "approaching another edge must not create fresh stock");
            }
            Assert.AreEqual(1, pool.Cast<Entity>().Count(e => e.HasPart<SourceStockPart>()));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LargeSourceRetainsSavedLegacyBudgetInsteadOfRefillingAtNewAnchor(bool duplicateLegacyBudget)
        {
            var pool = new Entity[50, 20];
            for (int x = 0; x < 50; x++)
                for (int y = 0; y < 20; y++)
                    pool[x, y] = Source("BrinePool", x, y, rich: false);
            var legacy = pool[30, 0];
            var savedStock = SourceStockPart.For(wild, legacy, WorldClock.CurrentTick);
            savedStock.Capacity = 4; savedStock.Available = 0; savedStock.LastTick = WorldClock.CurrentTick;
            var restored = PartRoundTripHelper.RoundTripEntityViaTokenGraph(legacy);
            wild.RemoveEntity(legacy); Assert.True(wild.AddEntity(restored, 30, 0)); pool[30, 0] = restored;
            var stock = restored.GetPart<SourceStockPart>();
            if (duplicateLegacyBudget) SourceStockPart.For(wild, pool[40, 0], WorldClock.CurrentTick);

            foreach (var source in new[] { pool[0, 0], pool[0, 19], pool[49, 19] })
            {
                var holder = ProofingSources.StockHolder(wild, source, ProofingRegistry.ById("electric"));
                Assert.AreSame(restored, holder, "keep the first existing legacy budget in coordinate order");
                Assert.AreSame(stock, SourceStockPart.For(wild, holder, WorldClock.CurrentTick));
            }
            Assert.AreEqual(4, stock.Capacity);
            Assert.AreEqual(0, stock.Available);
            Assert.AreEqual(WorldClock.CurrentTick, stock.LastTick);
            Assert.IsNull(pool[0, 0].GetPart<SourceStockPart>(), "an upgrade must not mint a new budget");
            Assert.AreEqual(duplicateLegacyBudget ? 2 : 1, pool.Cast<Entity>().Count(e => e.HasPart<SourceStockPart>()));
        }

        [Test] public void TraysAtDifferentEdgesOfOneMultiCellPoolShareOneStock()
        {
            var pool = Pool3x3(); var west = SetTray(AX, 11, "east");   // (28,11): two cells from the pool's west edge
            var north = SetTray(31, 7, "south");   // (31,8): two cells from the north edge
            var south = SetTray(31, 15, "north");   // (31,14): two cells from the south edge
            Days(5); foreach (var t in new[] { west, north, south }) { var row = TakeRow(t); if (!row.IsDisabled) Assert.True(Act(t, row.Command)); }
            var holders = pool.Cast<Entity>().Where(e => e.GetPart<SourceStockPart>() != null).ToList(); Assert.AreEqual(1, holders.Count, "one stock for the whole pool, not one a cell");
            Assert.AreSame(pool[0, 0], holders[0], "kept on the lowest cell, as a pond's is on its anchor"); Assert.LessOrEqual(Got("BrineCrust"), holders[0].GetPart<SourceStockPart>().Capacity, "three trays on three sides still cannot beat the one supply");
            Assert.AreEqual(holders[0].GetPart<SourceStockPart>().Capacity, Got("BrineCrust") + holders[0].GetPart<SourceStockPart>().Available);
        }

        [Test] public void MovingATrayAlongTheShoreOrDrawingTheAnchorCellDryNeverResetsTheStock()
        {
            var pool = Pool3x3(); var tray = SetTray(AX, 11, "east"); Days(5); Assert.True(Act(tray, TakeRow(tray).Command)); var holder = pool[0, 0]; var stock = holder.GetPart<SourceStockPart>(); Assert.NotNull(stock);
            int left = stock.Available; int cap = stock.Capacity;
            holder.GetPart<LiquidPoolPart>().Volume = 0;   // a flask drew the anchor cell dry
            var other = SetTray(AX, 13, "east"); Days(0.5); Part(other).Reconcile(wild, WorldClock.CurrentTick);   // another tray, further along the shore
            Assert.AreSame(stock, holder.GetPart<SourceStockPart>()); Assert.IsNull(pool[1, 1].GetPart<SourceStockPart>(), "no second stock was started"); Assert.AreEqual(cap, stock.Capacity);
            Assert.LessOrEqual(stock.Available, left, "the dry anchor cell did not give the new tray a full stock"); Assert.AreSame(holder, ProofingSources.StockHolder(wild, pool[2, 2], ProofingRegistry.ById("electric")), "seen from the far corner it is the same anchor");
        }

        [Test] public void TwoSeparatePoolsAreTwoSourcesWithTheirOwnStock_Counter()
        {
            var a = Source("BrinePool", 30, 11, rich: false); var b = Source("BrinePool", 30, 18, rich: false); var ta = SetTray(AX, 11, "east"); var tb = SetTray(AX, 18, "east");
            Days(5); Assert.True(Act(ta, TakeRow(ta).Command)); Assert.True(Act(tb, TakeRow(tb).Command)); Assert.NotNull(a.GetPart<SourceStockPart>()); Assert.NotNull(b.GetPart<SourceStockPart>()); Assert.AreNotSame(a.GetPart<SourceStockPart>(), b.GetPart<SourceStockPart>());
        }

        [Test] public void NextToEachOtherABrinePoolAndAnAcidPoolAreTwoSourcesOfTwoElements_Counter()
        {
            var brine = Source("BrinePool", 30, 11, rich: false); var acid = Source("AcidPool", 31, 11, rich: false); var electric = ProofingRegistry.ById("electric"); var acidKind = ProofingRegistry.ById("acid");
            Assert.AreSame(brine, ProofingSources.StockHolder(wild, brine, electric)); Assert.AreSame(acid, ProofingSources.StockHolder(wild, acid, acidKind), "a different element is never part of the same stock");
        }

        // ── free means unsellable all the way down (review) ──

        [Test] public void ATonicBrewedFromAFreeReagentIsAsUnsellableAsTheReagent_ButOrdinaryBrewsStillSell_Counter()
        {
            BrewRuleRegistry.ResetForTests(); BrewRuleRegistry.EnsureInitialized();
            var free = Carry("RimeFlake"); Assert.True(BrewingService.TryBrew(Actor, Factory, new List<Entity> { free }, out var tonic, out var result, out var reason), reason);
            Assert.NotNull(tonic); Assert.AreEqual(BrewOutcomeKind.Brew, result.Kind); Assert.True(tonic.HasTag("NoTrade"), "a tonic made of what the place gave for nothing cannot be sold"); Assert.False(TradeSystem.CanBeTraded(tonic, Actor, Actor, "Sell"));
            var plain = Carry("FrostLichen"); Assert.True(BrewingService.TryBrew(Actor, Factory, new List<Entity> { plain }, out var ordinary, out _, out reason), reason); Assert.False(ordinary.HasTag("NoTrade"), "counter: an ordinary reagent makes an ordinary tonic"); Assert.True(TradeSystem.CanBeTraded(ordinary, Actor, Actor, "Sell"));
        }

        [Test] public void AFreeItemNeverStacksWithATradableOneOfTheSameBlueprint_InEitherDirection()
        {
            var a = Factory.CreateEntity("FireMoss"); var b = Factory.CreateEntity("FireMoss"); Assert.True(a.GetPart<StackerPart>().CanStackWith(b), "fixture: two plain moss stack");
            b.SetTag("NoTrade"); Assert.False(a.GetPart<StackerPart>().CanStackWith(b)); Assert.False(b.GetPart<StackerPart>().CanStackWith(a));
            var c = Factory.CreateEntity("FireMoss"); c.SetTag("NoTrade"); Assert.True(b.GetPart<StackerPart>().CanStackWith(c), "counter: two free ones still stack");
        }

        // ── reading never creates anything (review) ──

        [Test] public void ReadingATrayAndItsRowsAndTheCampNoteNeverAdvancesTheFactorysIDsOrCreatesAnEntity()
        {
            Source(); var tray = SetTray(); Days(1.1); int Next() => int.Parse(Factory.CreateEntity("Grass").ID);
            int first = Next(); Read(tray); TakeRow(tray); HeldFixtureReader.Status(new[] { tray }); Actions(tray); int second = Next();
            Assert.AreEqual(first + 1, second, "looking at the tray created no entity");
        }

        [Test] public void AScraperAndATrayAtOneSourceShareOneFiniteSupply_Counter()
        {
            var vent = Source(); var tray = SetTray(); var scraper = Carry("Scraper"); var stock = SourceStockPart.For(wild, vent, WorldClock.CurrentTick); stock.Capacity = 4; stock.Available = 4;
            wild.RemoveEntity(Actor); Assert.True(wild.AddEntity(Actor, AX + 1, AY));   // beside the tray, two cells from the vent
            Assert.True(Act(scraper, "ScrapeSource")); Assert.True(Act(scraper, "ScrapeSource")); Assert.AreEqual(2, stock.Available, "two scraped");
            Days(1.1); Assert.AreEqual(2, Part(tray).HeldNow(wild, WorldClock.CurrentTick), "a day and a tenth: two catches in the pan"); Assert.True(Act(tray, TakeRow(tray).Command));
            Assert.AreEqual(4, Got("VentScale"), "two scraped and two settled: one supply of four"); Assert.AreEqual(1, stock.Available, "four drawn from a supply of four plus the one a day brings back, and no more");
            Days(0.5); Assert.AreEqual(0, Part(tray).Held); int caught = Part(tray).HeldNow(wild, WorldClock.CurrentTick); Assert.LessOrEqual(caught, 1, "what is left is only what has come back");
        }

        // ── when there is nothing to settle from ──

        [Test] public void WithNoSourceItSaysSoCatchesNothingAndOffersADisabledRowThatNamesTheRule()
        {
            var vent = Source(); var tray = SetTray(); Days(1.1); wild.RemoveEntity(vent);
            var row = TakeRow(tray); Assert.True(row.IsDisabled); Assert.AreEqual("no-source", row.DisabledCode); StringAssert.Contains("two cells", row.DisabledReason);
            Assert.AreEqual(0, Part(tray).HeldNow(wild, WorldClock.CurrentTick)); StringAssert.Contains("nothing near enough", Read(tray));
        }

        [Test] public void ASpentSourceSaysSpentAndPromisesNothing_Counter()
        {
            var vent = Source(); var tray = SetTray(); Days(1.1); Act(tray, TakeRow(tray).Command); vent.GetPart<SourceStockPart>().Available = 0; vent.GetPart<SourceStockPart>().LastTick = WorldClock.CurrentTick; string line = Read(tray);
            StringAssert.Contains("spent for now", line); StringAssert.DoesNotContain("due", line); var row = TakeRow(tray); Assert.AreEqual("empty", row.DisabledCode); StringAssert.Contains("a little more each day", row.DisabledReason);
        }

        [Test] public void APanHoldingOneElementWillNotMixAnotherInUntilItIsEmptied_ThenItStartsOverForTheNew()
        {
            var vent = Source(); var tray = SetTray(); var part = Part(tray); Days(1.1); part.Reconcile(wild, WorldClock.CurrentTick); Assert.AreEqual(2, part.Held); Assert.AreEqual("heat", part.Element);
            wild.RemoveEntity(vent); Source("FrostVent"); Days(3);
            Assert.AreEqual(2, part.HeldNow(wild, WorldClock.CurrentTick), "a source of another element adds nothing to a pan that holds heat"); StringAssert.Contains("settles nothing more until you empty it", Read(tray));
            var row = TakeRow(tray); Assert.False(row.IsDisabled); StringAssert.Contains("vent scale (2)", row.Display); Assert.True(Act(tray, row.Command)); Assert.AreEqual(2, Got("VentScale")); Assert.AreEqual(0, Got("RimeFlake"));
            Days(0.5); Assert.AreEqual(1, part.HeldNow(wild, WorldClock.CurrentTick), "emptied, it settles the new element"); part.Reconcile(wild, WorldClock.CurrentTick); Assert.AreEqual("cold", part.Element);
            StringAssert.Contains("rime flake", Read(tray)); Days(0.6); Assert.True(Act(tray, TakeRow(tray).Command)); Assert.AreEqual(2, Got("RimeFlake")); Assert.AreEqual(2, Got("VentScale"), "the heat in the pack is untouched");
        }

        [Test] public void ASecondSourceOfTheSameElementDoesNotMixAnything_Counter()
        {
            var vent = Source(); var tray = SetTray(); Days(1.1); Part(tray).Reconcile(wild, WorldClock.CurrentTick); wild.RemoveEntity(vent); Source("AshBed"); Days(0.5);
            Assert.AreEqual(3, Part(tray).HeldNow(wild, WorldClock.CurrentTick), "heat from an ash bed joins heat from a vent"); Assert.IsFalse(Read(tray).Contains("settles nothing more"));
        }

        // ── safety ──

        [Test] public void AnInterruptedGatherLeavesTheTrayAndTheSourceAsTheyWereAndYouCarryNothing()
        {
            var vent = Source(); var tray = SetTray(); Days(2.1); string command = TakeRow(tray).Command; int last = Part(tray).LastTick, progress = Part(tray).Progress; FailAfter();
            Assert.False(Act(tray, command)); Assert.AreEqual(0, Got("VentScale")); Assert.AreEqual(0, Part(tray).Held, "nothing was committed to the tray"); Assert.AreEqual(last, Part(tray).LastTick); Assert.AreEqual(progress, Part(tray).Progress); Assert.AreEqual("", Part(tray).Element);
            var stock = vent.GetPart<SourceStockPart>(); if (stock != null) Assert.AreEqual(stock.Capacity, stock.Available, "and the source was not drawn on");
        }

        [Test] public void AFullPackTakesAsManyAsFitAndTheRestStayInTheTray()
        {
            Source(); var tray = SetTray(); Days(2.1); var scale = Factory.CreateEntity("VentScale"); int unit = HandlingService.GetWeight(scale); Pack.MaxWeight = Pack.GetCarriedWeight() + unit * 2;
            Assert.True(Act(tray, TakeRow(tray).Command)); Assert.AreEqual(2, Got("VentScale")); Assert.AreEqual(2, Part(tray).Held, "two stay in the tray"); Assert.AreEqual("heat", Part(tray).Element, "and it still holds heat"); StringAssert.Contains("pack is full", string.Join(" ", MessageLog.GetRecent(3)));
            Pack.MaxWeight = Pack.GetCarriedWeight(); Assert.False(Act(tray, TakeRow(tray).Command)); Assert.AreEqual(2, Part(tray).Held, "a full pack takes nothing and changes nothing");
        }

        [Test] public void TakingTheTrayUpBringsItUpToDateFirst_ItKeepsWhatItHeldAndSettlesNoMoreUntilSetDownAgain()
        {
            var vent = Source(); var tray = SetTray(); Days(1.1); Assert.AreEqual(0, Part(tray).Held, "fixture: nothing has reconciled the tray yet");
            var take = World(tray).Single(a => a.Name == "TakeUpFixture"); Assert.True(Act(tray, take.Command)); Assert.True(Pack.Objects.Contains(tray));
            Assert.AreEqual(2, Part(tray).Held, "the two it settled while it stood were counted before the clock stopped"); Assert.AreEqual(vent.GetPart<SourceStockPart>().Capacity - 2, vent.GetPart<SourceStockPart>().Available, "and drawn from the source");
            Days(5); Assert.AreEqual(2, Part(tray).HeldNow(wild, WorldClock.CurrentTick), "carried, it settles nothing"); Assert.IsEmpty(Actions(tray).Where(a => a.Name == "GatherTray")); StringAssert.Contains("holds 2 vent scale", Read(tray));
            wild.RemoveEntity(Actor); Assert.True(wild.AddEntity(Actor, AX, AY)); var row = SetDownRows(tray).First(r => Dir(r) == "east"); Assert.True(Act(tray, row.Command));
            Days(0.5); Assert.AreEqual(3, Part(tray).HeldNow(wild, WorldClock.CurrentTick), "set down again it settles again, from where it left off");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FailedFullPackTakeUpPreservesTheTrayAndSharedStockUntilAValidRetry(bool existingStock)
        {
            var source = Source(rich: existingStock);
            var tray = SetTray();
            var part = Part(tray);
            var fixture = tray.GetPart<PackableFixturePart>();
            var cell = wild.GetEntityCell(tray);
            var stock = source.GetPart<SourceStockPart>();
            if (stock != null) stock.Available = 1;
            var stockBefore = stock == null ? default : (stock.Available, stock.LastTick, stock.Capacity);

            Days(0.2);
            Assert.AreEqual(0, part.Reconcile(wild, WorldClock.CurrentTick));
            Days(0.9);
            int tick = WorldClock.CurrentTick;
            int projected = part.HeldNow(wild, tick);
            int available = stock?.AvailableAt(tick) ?? SourceStockPart.CapacityFor(wild, source);
            Assert.AreEqual(2, projected);
            var before = (part.Held, part.Progress, part.LastTick, part.Element, fixture.SetTick);
            string command = World(tray).Single(row => row.Name == "TakeUpFixture").Command;
            int limit = Pack.MaxWeight;
            Pack.MaxWeight = Pack.GetCarriedWeight();

            Assert.False(Act(tray, command));
            Assert.AreSame(cell, wild.GetEntityCell(tray));
            Assert.True(HeldFixtureActions.IsPlayerHeld(tray));
            Assert.False(Pack.Objects.Contains(tray));
            Assert.AreEqual(tick, WorldClock.CurrentTick);
            Assert.AreEqual(before, (part.Held, part.Progress, part.LastTick, part.Element, fixture.SetTick));
            Assert.AreSame(stock, source.GetPart<SourceStockPart>());
            if (stock != null) Assert.AreEqual(stockBefore, (stock.Available, stock.LastTick, stock.Capacity));

            Pack.MaxWeight = limit;
            Assert.True(Act(tray, command));
            Assert.True(Pack.Objects.Contains(tray));
            Assert.AreEqual(projected, part.Held);
            Assert.AreEqual("heat", part.Element);
            Assert.AreEqual(tick, part.LastTick);
            var settled = source.GetPart<SourceStockPart>();
            Assert.NotNull(settled);
            if (stock != null) Assert.AreSame(stock, settled);
            Assert.AreEqual(available - projected, settled.Available);
            Assert.False(Act(tray, command), "a completed pickup cannot be paid twice");
            Assert.AreEqual(available - projected, settled.Available);
            Assert.AreEqual(projected, part.Held);
        }

        [Test] public void AnInterruptedPickupRestoresTheLoadedTrayAndItsExactSource()
        {
            var source = Source();
            var stock = source.GetPart<SourceStockPart>();
            stock.Available = 3;
            var tray = SetTray();
            var part = Part(tray);
            var cell = wild.GetEntityCell(tray);
            var fixture = tray.GetPart<PackableFixturePart>();
            Days(0.6);
            Assert.AreEqual(1, part.Reconcile(wild, WorldClock.CurrentTick));
            Days(0.9);
            int tick = WorldClock.CurrentTick;
            int expected = part.HeldNow(wild, tick);
            int expectedRemaining = stock.AvailableAt(tick) - (expected - part.Held);
            Assert.AreEqual(3, expected);
            var before = (part.Held, part.Progress, part.LastTick, part.Element, fixture.SetTick);
            var stockBefore = (stock.Available, stock.LastTick, stock.Capacity);
            string command = World(tray).Single(row => row.Name == "TakeUpFixture").Command;
            bool staged = false;
            var interrupt = new LateFixturePickupProbe
            {
                Fixture = tray,
                Observe = () => staged = wild.GetEntityCell(tray) == null && Pack.Objects.Contains(tray)
            };
            Actor.AddPart(interrupt);

            Assert.False(Act(tray, command));
            Assert.True(staged, "the callback ran after provisional custody changed");
            Assert.AreSame(cell, wild.GetEntityCell(tray));
            Assert.True(HeldFixtureActions.IsPlayerHeld(tray));
            Assert.False(Pack.Objects.Contains(tray));
            Assert.AreEqual(before, (part.Held, part.Progress, part.LastTick, part.Element, fixture.SetTick));
            Assert.AreSame(stock, source.GetPart<SourceStockPart>());
            Assert.AreEqual(stockBefore, (stock.Available, stock.LastTick, stock.Capacity));

            Assert.True(Actor.RemovePart(interrupt));
            Assert.True(Act(tray, command));
            Assert.True(Pack.Objects.Contains(tray));
            Assert.AreEqual(expected, part.Held);
            Assert.AreEqual(expectedRemaining, stock.Available);
        }

        [Test] public void TheTrayAndItsSourceRideTheSave()
        {
            var vent = Source(); var tray = SetTray(); Days(2.1); Act(tray, TakeRow(tray).Command); Days(1); Part(tray).Reconcile(wild, WorldClock.CurrentTick); var stock = vent.GetPart<SourceStockPart>();
            var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(tray).GetPart<SettlingTrayPart>(); Assert.AreEqual(Part(tray).Held, copy.Held); Assert.AreEqual(Part(tray).LastTick, copy.LastTick); Assert.AreEqual(Part(tray).Progress, copy.Progress); Assert.AreEqual(Part(tray).Element, copy.Element);
            var s2 = PartRoundTripHelper.RoundTripEntityViaTokenGraph(vent).GetPart<SourceStockPart>(); Assert.AreEqual(stock.Capacity, s2.Capacity); Assert.AreEqual(stock.Available, s2.Available); Assert.AreEqual(stock.LastTick, s2.LastTick);
        }

        [Test] public void TheCampNoteProjectsWhatTheTrayWouldHoldByNowWithoutChangingIt()
        {
            Source(); var tray = SetTray(); Assert.AreEqual("", HeldFixtureReader.Status(new[] { tray }), "an empty tray says nothing");
            Days(1.1); StringAssert.Contains("settling tray holds 2 by now", HeldFixtureReader.Status(new[] { tray })); Assert.AreEqual(0, Part(tray).Held, "reading the note changes nothing");
        }

        // ── the reader (quality of life) ──

        [Test] public void TheReaderNamesWhatItSettlesFromWhatAndHowTheSourceLooks_NeverACount()
        {
            Source(); var tray = SetTray(); string fresh = Read(tray); StringAssert.Contains("Settling vent scale from the", fresh); StringAssert.Contains("Holds 0 (it takes 4)", fresh); StringAssert.Contains("plentiful", fresh);
            Days(1.1); string line = Read(tray); StringAssert.Contains("Ready to gather 2", line); StringAssert.DoesNotContain("Holds 2", line); StringAssert.Contains("plentiful", line, "The forecast has not drawn from the physical source.");
            Assert.False(System.Text.RegularExpressions.Regex.IsMatch(line, @"\b\d+ of \d+\b"), "no source arithmetic is shown"); Assert.AreEqual(0, Part(tray).Held, "reading changes nothing");
        }

        [Test] public void TheReaderSaysWhenTheNextIsDue_InTheNameOfTheTimeOfDayNeverATickCount()
        {
            Source(); var tray = SetTray(); int t0 = WorldClock.CurrentTick; string line = Read(tray);
            StringAssert.Contains("The next is due around " + BandAt(t0 + SettlingTrayPart.CatchTicks), line); Assert.False(System.Text.RegularExpressions.Regex.IsMatch(line, @"\b\d{3,}\b"), "no tick counts: " + line);
            Days(0.45); StringAssert.Contains("due soon", Read(tray)); StringAssert.DoesNotContain("due around", Read(tray));
            Assert.AreEqual(0, Part(tray).Held, "reading changes nothing");
        }

        [Test] public void AFullTraySaysItIsFullAndPromisesNothingFurther_Counter()
        {
            Source(); var tray = SetTray(); Days(2.5); string preview = Read(tray);
            StringAssert.Contains("Ready to gather 4", preview); StringAssert.DoesNotContain("Holds 4", preview); StringAssert.DoesNotContain("due", preview);
            Assert.AreEqual(4, Part(tray).Reconcile(wild, WorldClock.CurrentTick));
            string held = Read(tray); StringAssert.Contains("Holds 4", held); StringAssert.Contains("It is full: it settles no more until you empty it", held); StringAssert.DoesNotContain("due", held);
        }

        [Test] public void TheDisabledGatherRowOfAnEmptyTraySaysWhenToComeBack()
        {
            Source(); var tray = SetTray(); var row = TakeRow(tray); Assert.AreEqual("empty", row.DisabledCode); StringAssert.Contains("half day", row.DisabledReason); StringAssert.Contains("due around " + BandAt(WorldClock.CurrentTick + SettlingTrayPart.CatchTicks), row.DisabledReason);
        }
    }
}
