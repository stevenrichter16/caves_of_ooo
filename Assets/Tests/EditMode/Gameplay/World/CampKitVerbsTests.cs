using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Quality-of-life verbs for the set-down kits (Docs/MACHINE-INVENTED-USES.md section 3e): hints on where a kit is best set down, "make camp" (lay the kits out in one action, in a layout
    /// that already satisfies the rules), "pack up camp" (take up everything of yours within reach in one action) and "move" (relocate one kit a cell). Every composite is one transaction.
    /// </summary>
    public sealed class CampKitVerbsTests : FiftyWorldFixture
    {
        OverworldZoneManager manager; string wildId; Zone wild; const int AX = 20, AY = 12;

        [SetUp] public void SetupVerbs()
        {
            PackableFixtureRegistry.ResetForTests(); Diag.ResetAll(); MessageLog.Clear();
            manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            wildId = Enumerable.Range(0, WorldMap.Width * WorldMap.Height).Select(i => WorldMap.ToZoneID(i % WorldMap.Width, i / WorldMap.Width, 0))
                .First(id => HeldPlaceZones.Classify(manager, id) == HeldPlaceZoneClass.WildSurface);
            wild = new Zone(wildId);
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++) Assert.True(wild.AddEntity(Factory.CreateEntity("Grass"), x, y));
            Zone.RemoveEntity(Actor); Zone = wild; Assert.True(Zone.AddEntity(Actor, AX, AY));
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { wildId, wild } }, wildId, new Dictionary<string, List<ZoneConnection>>());
            SettlementRuntime.ActiveZone = wild; Pack.MaxWeight = 400;
        }
        [TearDown] public void TearDownVerbs() { PackableFixtureRegistry.ResetForTests(); }

        InventoryAction[] SetDownRows(Entity kit) => Actions(kit).Where(a => a.Name == "SetDownFixture" && !a.IsDisabled).ToArray();
        string SetDownRow(Entity kit, string direction) => SetDownRows(kit).Single(a => a.Display == "set down " + direction || a.Display.StartsWith("set down " + direction + " (", StringComparison.Ordinal)).Command;
        Entity Standing(string blueprint, string direction) { var k = Carry(blueprint); Assert.True(Act(k, SetDownRow(k, direction)), string.Join("|", MessageLog.GetRecent(2))); return k; }
        List<InventoryAction> World(Entity fixture) => WorldInteractionSystem.GatherActions(fixture, Actor);
        static int Dist(Zone z, Entity a, Entity b) { var ca = z.GetEntityCell(a); var cb = z.GetEntityCell(b); return Math.Max(Math.Abs(ca.X - cb.X), Math.Abs(ca.Y - cb.Y)); }
        void Light(Entity hearth) { hearth.GetPart<FuelPart>().FuelMass = 10; hearth.GetPart<ThermalPart>().Temperature = 450; }
        List<Entity> Held() => Zone.GetReadOnlyEntities().Where(HeldFixtureActions.IsPlayerHeld).ToList();

        // ── placement hints ──

        [Test] public void AStillSaysWhenACellIsByYourHearthOrYourFire()
        {
            var hearth = Standing("CampHearth", "east"); var still = Carry("CampStill"); var cold = SetDownRows(still); Assert.True(cold.Length > 0);
            Assert.True(cold.All(r => r.Display.Contains("(by your hearth)")), string.Join(" / ", cold.Select(r => r.Display)));
            Light(hearth); var lit = SetDownRows(still); Assert.True(lit.All(r => r.Display.Contains("(by your fire)")), string.Join(" / ", lit.Select(r => r.Display)));
        }

        [Test] public void AHearthSaysWhenACellIsByYourStill_AndNothingElseGainsAHint_Counters()
        {
            Standing("CampStill", "west"); var hearth = Carry("CampHearth"); Assert.True(SetDownRows(hearth).All(r => r.Display.Contains("(by your still)")));
            var box = Carry("CampBox"); Assert.True(SetDownRows(box).All(r => !r.Display.Contains("(by")), "a box has no hint");
        }

        [Test] public void NoHintWhenNothingOfYoursIsNear_Counter()
        {
            var still = Carry("CampStill"); Assert.True(SetDownRows(still).All(r => !r.Display.Contains("(by")));
        }

        // ── make camp ──

        [Test] public void MakeCampSetsDownEveryCarriedKitInOneActionInALayoutThatSatisfiesTheRules()
        {
            var hearth = Carry("CampHearth"); var still = Carry("CampStill"); var bench = Carry("CampBench"); var box = Carry("CampBox");
            var row = Actions(hearth).Single(a => a.Name == "MakeCamp"); StringAssert.Contains("4 kits", row.Display); Assert.AreEqual(ActionTime.OneAction, row.Time);
            Assert.True(Act(hearth, row.Command), string.Join("|", MessageLog.GetRecent(6)));
            Assert.AreEqual(4, Held().Count); Assert.False(Pack.Objects.Any(o => o.HasPart<PackableFixturePart>()), "everything is out of the pack");
            Assert.LessOrEqual(Dist(Zone, still, hearth), LiquidSettlingPart.FireReach, "the still is within reach of the hearth's fire");
            Assert.GreaterOrEqual(Dist(Zone, box, hearth), 2, "the wooden box is not beside the fire");
            Assert.True(AlchemyStillPart.IsNearStill(Actor, Zone)); Assert.NotNull(still.GetPart<InstallationMarkerPart>());
        }

        [Test] public void MakeCampIsOfferedOnEveryCarriedKitsMenuOnlyWithTwoOrMoreKinds_AndNotInRefusedGround()
        {
            var hearth = Carry("CampHearth"); Assert.False(Actions(hearth).Any(a => a.Name == "MakeCamp"), "one kit: just set it down"); var box = Carry("CampBox");
            Assert.True(Actions(hearth).Any(a => a.Name == "MakeCamp")); Assert.True(Actions(box).Any(a => a.Name == "MakeCamp"));
            var plain = Factory.CreateEntity("Torch"); Assert.True(Pack.AddObject(plain)); Assert.False(Actions(plain).Any(a => a.Name == "MakeCamp"), "only kits offer it");
        }

        [Test] public void MakeCampGivesEverythingBackWhenAnOuterStepFails()
        {
            var hearth = Carry("CampHearth"); Carry("CampStill"); string command = Actions(hearth).Single(a => a.Name == "MakeCamp").Command; FailAfter();
            Assert.False(Act(hearth, command)); Assert.AreEqual(0, Held().Count); Assert.True(Pack.Objects.Count(o => o.HasPart<PackableFixturePart>()) == 2);
        }

        [Test] public void AForgedMakeCampCommandIsRefused_Counter()
        {
            var hearth = Carry("CampHearth"); Carry("CampStill"); string good = Actions(hearth).Single(a => a.Name == "MakeCamp").Command; string[] f = good.Split('|');
            foreach (var bad in new[] { "MakeCamp|", "MakeCamp|x|1|2|" + hearth.ID, string.Join("|", f.Take(4).Concat(new[] { "nope" })), f[0] + "|" + f[1] + "|99|99|" + f[4] }) Assert.False(Act(hearth, bad), bad);
            Assert.AreEqual(0, Held().Count);
        }

        [Test] public void TheKitVerbsSayWhatTheyDoInSmallText()
        {
            var hearth = Standing("CampHearth", "east"); var box = Standing("CampBox", "north"); var rows = World(box);
            StringAssert.Contains("every kit of yours", rows.Single(a => a.Name == "PackUpCamp").Hint); StringAssert.Contains("fuel and contents", rows.First(a => a.Name == "MoveFixture").Hint);
            var still = Carry("CampStill"); Carry("CampBench"); StringAssert.Contains("every kit you carry", Actions(still).Single(a => a.Name == "MakeCamp").Hint);
            Assert.IsEmpty(Actions(still).First(a => a.Name == "SetDownFixture").Hint, "a plain set-down needs no explanation");
        }

        // ── pack up camp ──

        [Test] public void PackUpCampTakesUpEverythingOfYoursWithinReachInOneActionAndPutsTheFireOut()
        {
            var hearth = Standing("CampHearth", "east"); var still = Standing("CampStill", "west"); var box = Standing("CampBox", "north"); Light(hearth);
            var row = World(box).Single(a => a.Name == "PackUpCamp"); StringAssert.Contains("3 things", row.Display); Assert.AreEqual(ActionTime.OneAction, row.Time);
            Assert.True(Act(box, row.Command), string.Join("|", MessageLog.GetRecent(6))); Assert.AreEqual(0, Held().Count); Assert.True(Pack.Objects.Contains(hearth) && Pack.Objects.Contains(still) && Pack.Objects.Contains(box));
            Assert.AreEqual(hearth.GetPart<ThermalPart>().AmbientTemperature, hearth.GetPart<ThermalPart>().Temperature, 0.001f, "taking a burning hearth up puts it out"); Assert.AreEqual(10f, hearth.GetPart<FuelPart>().FuelMass, 0.001f);
        }

        [Test] public void PackUpCampLeavesWhatIsOutOfReachAndIsNotOfferedForASingleKit_Counters()
        {
            var box = Standing("CampBox", "north"); Assert.False(World(box).Any(a => a.Name == "PackUpCamp"), "one thing: just take it up");
            Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, AX + 14, AY)); var far = Standing("CampBox", "east");
            Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, AX, AY)); Standing("CampStill", "west");
            var row = World(box).Single(a => a.Name == "PackUpCamp"); StringAssert.Contains("2 things", row.Display); Assert.True(Act(box, row.Command)); Assert.AreEqual(1, Held().Count); Assert.True(Held().Contains(far));
        }

        [Test] public void PackUpCampRefusesTheWholeThingWhenYouCannotCarryItAll_NothingIsTaken()
        {
            var box = Standing("CampBox", "north"); Standing("CampHearth", "east"); string command = World(box).Single(a => a.Name == "PackUpCamp").Command;
            Pack.MaxWeight = Pack.GetCarriedWeight() + 14; Assert.False(Act(box, command)); Assert.AreEqual(2, Held().Count, "the camp stands as it was"); Assert.True(MessageLog.GetRecent(4).Any(m => m.Contains("carry")));
        }

        [Test] public void PackUpCampGivesEverythingBackWhenAnOuterStepFails()
        {
            var box = Standing("CampBox", "north"); Standing("CampHearth", "east"); string command = World(box).Single(a => a.Name == "PackUpCamp").Command; FailAfter();
            Assert.False(Act(box, command)); Assert.AreEqual(2, Held().Count); Assert.False(Pack.Objects.Any(o => o.HasPart<PackableFixturePart>()));
        }

        // ── move a kit one cell ──

        [Test] public void AStandingKitOffersMovesToTheOpenCellsBesideItAndMovingKeepsItsIdAndFuel()
        {
            var hearth = Standing("CampHearth", "east"); hearth.GetPart<FuelPart>().FuelMass = 12; var rows = World(hearth).Where(a => a.Name == "MoveFixture").ToArray(); Assert.True(rows.Length >= 3);
            var before = Zone.GetEntityCell(hearth); var north = rows.Single(r => r.Display == "move north"); Assert.AreEqual(ActionTime.OneAction, north.Time);
            Assert.True(Act(hearth, north.Command), string.Join("|", MessageLog.GetRecent(4))); var after = Zone.GetEntityCell(hearth); Assert.AreEqual(before.X, after.X); Assert.AreEqual(before.Y - 1, after.Y);
            Assert.True(HeldFixtureActions.IsPlayerHeld(hearth)); Assert.AreEqual(12f, hearth.GetPart<FuelPart>().FuelMass, 0.001f); Assert.False(Pack.Objects.Contains(hearth)); Assert.AreEqual(1, Held().Count);
        }

        [Test] public void ASolidKitCanMoveOneCellEvenThoughItsOwnOldCellWasBesideTheNewOne()
        {
            var box = Standing("CampBox", "east"); var rows = World(box).Where(a => a.Name == "MoveFixture").ToArray(); Assert.True(rows.Any(r => r.Display == "move north"), "the box's own old cell does not count against it");
            var before = Zone.GetEntityCell(box); Assert.True(Act(box, rows.Single(r => r.Display == "move north").Command)); Assert.AreEqual(before.Y - 1, Zone.GetEntityCell(box).Y); Assert.True(box.GetPart<PhysicsPart>().Solid);
        }

        [Test] public void NoMoveRowForAnOccupiedOrRefusedCell_AndNoneForAKitYouHaveHandedOver_Counters()
        {
            var hearth = Standing("CampHearth", "east"); var still = Standing("CampStill", "north"); var rows = World(hearth).Where(a => a.Name == "MoveFixture").ToArray();
            var sc = Zone.GetEntityCell(still); var hc = Zone.GetEntityCell(hearth); Assert.False(rows.Any(r => r.Display == "move north" && sc.X == hc.X && sc.Y == hc.Y - 1), "the still stands there");
            Assert.True(Act(hearth, "HandOver|" + Uri.EscapeDataString(wildId) + "|" + hearth.ID)); Assert.False(World(hearth).Any(a => a.Name == "MoveFixture"));
        }

        [Test] public void MovingGivesTheKitBackToItsOldCellWhenAnOuterStepFails()
        {
            var hearth = Standing("CampHearth", "east"); string command = World(hearth).Where(a => a.Name == "MoveFixture").Single(r => r.Display == "move south").Command; var before = Zone.GetEntityCell(hearth); FailAfter();
            Assert.False(Act(hearth, command)); var after = Zone.GetEntityCell(hearth); Assert.AreEqual(before.X, after.X); Assert.AreEqual(before.Y, after.Y); Assert.True(HeldFixtureActions.IsPlayerHeld(hearth)); Assert.False(Pack.Objects.Contains(hearth));
        }

        [Test] public void AForgedMoveCommandIsRefused_Counter()
        {
            var hearth = Standing("CampHearth", "east"); string good = World(hearth).First(a => a.Name == "MoveFixture").Command; string[] f = good.Split('|');
            foreach (var bad in new[] { "MoveFixture|", "MoveFixture|x|" + hearth.ID + "|1|1", string.Join("|", f.Take(4).Concat(new[] { "zzz" })), string.Join("|", f.Take(3).Concat(new[] { "70", "20" })) }) Assert.False(Act(hearth, bad), bad);
            Assert.True(HeldFixtureActions.IsPlayerHeld(hearth)); Assert.AreEqual(1, Held().Count);
        }

        // ── the paths the rules reach: a burning kit moving, the zone cap, a veto partway through ──

        sealed class Veto : Part
        {
            public string Event; public override string Name => "CampKitVeto"; public override bool HandleEvent(GameEvent e) => !(e.ID == Event);
        }

        [Test] public void ALitHearthCanBeMovedAndKeepsItsFuelButItsFlameGoesOut_AsTakingItUpDoes()
        {
            var hearth = Standing("CampHearth", "east"); Light(hearth);
            var rows = World(hearth).Where(a => a.Name == "MoveFixture").ToArray(); Assert.True(rows.Length >= 2, "a burning hearth is offered cells to move to");
            StringAssert.Contains("goes out", rows.First().Hint);
            var before = Zone.GetEntityCell(hearth); Assert.True(Act(hearth, rows.First().Command), string.Join("|", MessageLog.GetRecent(4))); Assert.False(Zone.GetEntityCell(hearth) == before);
            Assert.AreEqual(10f, hearth.GetPart<FuelPart>().FuelMass, 0.001f, "it keeps its fuel"); Assert.AreEqual(hearth.GetPart<ThermalPart>().AmbientTemperature, hearth.GetPart<ThermalPart>().Temperature, 0.001f, "the flame goes out, as in taking it up");
        }

        [Test] public void MakeCampStopsAtTheZoneCapAndLeavesTheRestInThePackSayingSo()
        {
            for (int k = 0; k < PlacementCheck.ZoneCap - 2; k++)
            {
                Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, AX - 16 + 6 * k, AY)); Standing("CampBox", "east");
            }
            Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, AX, AY + 6)); Assert.AreEqual(PlacementCheck.ZoneCap - 2, Held().Count);
            var hearth = Carry("CampHearth"); Carry("CampStill"); Carry("CampBench"); string command = Actions(hearth).Single(a => a.Name == "MakeCamp").Command;
            Assert.True(Act(hearth, command), string.Join("|", MessageLog.GetRecent(4))); Assert.AreEqual(PlacementCheck.ZoneCap, Held().Count, "the zone holds eight and no more");
            Assert.AreEqual(1, Pack.Objects.Count(o => o.HasPart<PackableFixturePart>()), "the third kit stays in the pack"); StringAssert.Contains("could not be set down", string.Join("|", MessageLog.GetRecent(4)));
        }

        [Test] public void MakeCampGivesBackEveryKitWhenALaterOneIsVetoedAfterEarlierOnesWereSetDown()
        {
            var hearth = Carry("CampHearth"); var still = Carry("CampStill"); still.AddPart(new Veto { Event = "BeforeInstall" }); string command = Actions(hearth).Single(a => a.Name == "MakeCamp").Command;
            Assert.False(Act(hearth, command)); Assert.AreEqual(0, Held().Count, "the hearth that went down first is back in the pack"); Assert.True(Pack.Objects.Contains(hearth) && Pack.Objects.Contains(still));
            Assert.False(hearth.HasPart<InstallationMarkerPart>());
        }

        [TestCase("hearth")] [TestCase("box")]
        public void PackUpCampGivesBackEveryKitWhenEitherOneIsVetoed_SoWhicheverIsTakenFirstGoesBack(string vetoed)
        {
            var hearth = Standing("CampHearth", "east"); var box = Standing("CampBox", "north"); string command = World(box).Single(a => a.Name == "PackUpCamp").Command;
            (vetoed == "hearth" ? hearth : box).AddPart(new Veto { Event = "BeforeUninstall" });
            Assert.False(Act(box, command)); Assert.AreEqual(2, Held().Count, "the camp stands as it was"); Assert.False(Pack.Objects.Contains(hearth) || Pack.Objects.Contains(box));
        }

        // ── name this camp ──

        InventoryAction NameRow(Entity fixture) => World(fixture).Single(a => a.Name == "NameCamp");
        string CampNameOn(Entity e) => e.GetPart<PackableFixturePart>().CampName;

        [Test] public void NameThisCampCyclesThroughTheNamesForEveryKitOfTheCampThenClears()
        {
            var hearth = Standing("CampHearth", "east"); var box = Standing("CampBox", "north"); Assert.AreEqual(ActionTime.Free, NameRow(hearth).Time, "naming spends no turn");
            foreach (string expected in HeldFixtureReader.CampNames)
            {
                var row = NameRow(hearth); Assert.AreEqual("name this camp: " + expected, row.Display); Assert.True(Act(hearth, row.Command), string.Join("|", MessageLog.GetRecent(3)));
                Assert.AreEqual(expected, CampNameOn(hearth)); Assert.AreEqual(expected, CampNameOn(box), "the whole camp carries one name"); StringAssert.Contains("You call this camp " + expected, string.Join("|", MessageLog.GetRecent(2)));
            }
            var clear = NameRow(box); Assert.AreEqual("clear this camp's name", clear.Display); Assert.True(Act(box, clear.Command)); Assert.AreEqual("", CampNameOn(hearth)); Assert.AreEqual("", CampNameOn(box));
            Assert.AreEqual("name this camp: Home", NameRow(hearth).Display, "and it begins again");
        }

        [Test] public void AFarCampKeepsItsOwnName_Counter()
        {
            var near = Standing("CampBox", "east"); Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, AX + 20, AY)); var far = Standing("CampBox", "east");
            Assert.True(Act(far, NameRow(far).Command)); Assert.AreEqual("Home", CampNameOn(far)); Assert.AreEqual("", CampNameOn(near), "more than eight cells away is another camp");
            Assert.AreEqual(2, HeldFixtureReader.ClustersIn(wildId, wild).Count);
        }

        [Test] public void NamingGivesTheNamesBackWhenAnOuterStepFails_AndAForgedCommandIsRefused()
        {
            var hearth = Standing("CampHearth", "east"); var box = Standing("CampBox", "north"); string command = NameRow(hearth).Command; FailAfter();
            Assert.False(Act(hearth, command)); Assert.AreEqual("", CampNameOn(hearth)); Assert.AreEqual("", CampNameOn(box));
            foreach (var bad in new[] { "NameCamp|", "NameCamp|x|" + hearth.ID, "NameCamp|" + Uri.EscapeDataString(wildId) + "|nobody" }) Assert.False(Act(hearth, bad), bad);
            Assert.AreEqual("", CampNameOn(hearth));
        }

        [Test] public void ACampNameSurvivesASaveAndAnOldSaveLoadsUnnamed()
        {
            var hearth = Standing("CampHearth", "east"); Assert.True(Act(hearth, NameRow(hearth).Command)); var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(hearth);
            Assert.AreEqual("Home", copy.GetPart<PackableFixturePart>().CampName); Assert.AreEqual("", Factory.CreateEntity("CampHearth").GetPart<PackableFixturePart>().CampName, "a fresh kit, like an old save, has no name");
        }

        [Test] public void TheNameAppearsInTheArrivalNoteAndTheMapNote_AndAnUnnamedCampReadsAsBefore()
        {
            var hearth = Standing("CampHearth", "east"); var box = Standing("CampBox", "north");
            Assert.AreEqual("You keep here: camp box, camp hearth.", HeldFixtureReader.ArrivalNote(wild), "unnamed, with nothing to report: exactly the old wording");
            var (x, y, _) = WorldMap.FromZoneID(wildId); CollectionAssert.AreEqual(new[] { "You keep camp box, camp hearth: this place." }, HeldFixtureReader.MapNotes(manager, x, y));
            Assert.True(Act(hearth, NameRow(hearth).Command));
            Assert.AreEqual("You keep here: Home (camp box, camp hearth).", HeldFixtureReader.ArrivalNote(wild)); CollectionAssert.AreEqual(new[] { "You keep Home (camp box, camp hearth): this place." }, HeldFixtureReader.MapNotes(manager, x, y));
        }

        // ── as you left it ──

        [Test] public void TheNotesSayWhatTheHearthAndTheBoxHoldAsYouLeftThem()
        {
            var hearth = Standing("CampHearth", "east"); var box = Standing("CampBox", "north"); Light(hearth); hearth.GetPart<FuelPart>().FuelMass = 12;
            Assert.True(box.GetPart<ContainerPart>().AddItem(Factory.CreateEntity("Dagger"))); Assert.True(box.GetPart<ContainerPart>().AddItem(Factory.CreateEntity("Dagger")));
            string note = HeldFixtureReader.ArrivalNote(wild); StringAssert.Contains("As you left it:", note); StringAssert.Contains("hearth lit, 12 fuel", note); StringAssert.Contains("box holds", note);
            var (x, y, _) = WorldMap.FromZoneID(wildId); StringAssert.Contains("hearth lit, 12 fuel", HeldFixtureReader.MapNotes(manager, x, y).Single());
            hearth.GetPart<ThermalPart>().Temperature = 25; StringAssert.Contains("hearth cold, 12 fuel", HeldFixtureReader.ArrivalNote(wild));
        }

        [Test] public void ABareHearthAndAnEmptyBoxReportNothing_Counter()
        {
            Standing("CampHearth", "east"); Standing("CampBox", "north"); StringAssert.DoesNotContain("As you left it", HeldFixtureReader.ArrivalNote(wild));
            Assert.AreEqual("", HeldFixtureReader.Status(null)); Assert.AreEqual("", HeldFixtureReader.Status(new List<Entity>()));
        }

        // ── what can I do here? ──

        List<string> Lines(Entity e) { var l = new List<string>(); StateReader.Collect(e, false, Zone, Zone.GetEntityCell(e), Actor, l); return l; }

        [Test] public void AFireSaysWhatYouCouldDoAtItNow_AndNothingWhenThereIsNothing()
        {
            var hearth = Standing("CampHearth", "east"); Assert.False(Lines(hearth).Any(l => l.StartsWith("Here you can")), "an empty-handed visitor to a bare hearth has nothing to do");
            for (int i = 0; i < 3; i++) Carry("SalvagedTimber"); Carry("FlintStriker"); hearth.GetPart<FuelPart>().FuelMass = 5;
            var line = Lines(hearth).Single(l => l.StartsWith("Here you can")); StringAssert.Contains("feed it (3 timber)", line); StringAssert.Contains("light it", line);
            Light(hearth); var wet = Carry("Dagger"); Assert.True(wet.ApplyEffect(new WetEffect(1f))); StringAssert.Contains("dry 1 wet thing", Lines(hearth).Single(l => l.StartsWith("Here you can")));
        }

        [Test] public void AHearthSaysWhatARestWouldCost_OnlyWhenARestCouldBeTaken()
        {
            var hearth = Standing("CampHearth", "east"); Assert.False(Lines(hearth).Any(l => l.StartsWith("You could rest")), "no fire, no rest");
            hearth.GetPart<FuelPart>().FuelMass = 30; hearth.GetPart<ThermalPart>().Temperature = 450; StringAssert.Contains("You could rest here: it burns about", Lines(hearth).Single(l => l.StartsWith("You could rest")));
            Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, AX + 9, AY + 9)); Assert.False(Lines(hearth).Any(l => l.StartsWith("You could rest") || l.StartsWith("Here you can")), "from across the camp you cannot");
        }

        [Test] public void AStillSaysWhatYourMireWouldSettleToOrWhatItIsMissing()
        {
            var still = Standing("CampStill", "west"); Assert.False(Lines(still).Any(l => l.Contains("mire")), "nothing carried, nothing said");
            Flask(12, "bog-mire"); StringAssert.Contains("needs a lit fire within 2 cells", Lines(still).Single(l => l.Contains("mire")));
            var hearth = Carry("CampHearth"); var rows = SetDownRows(hearth); Assert.True(Act(hearth, rows.First(r => r.Display.Contains("(by your still)")).Command)); var fire = Held().Single(e => e.GetPart<PackableFixturePart>().Kind == "camp-hearth"); Light(fire);
            StringAssert.Contains("3 clean water or oil from 1 flask", Lines(still).Single(l => l.Contains("mire")));
        }
    }
}
