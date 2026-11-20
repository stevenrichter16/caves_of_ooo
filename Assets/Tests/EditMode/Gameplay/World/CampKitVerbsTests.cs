using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
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

        [Test] public void MakeCampIsOfferedOnEveryCarriedKitsMenuOnlyWithTwoOrMoreKits()
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
            StringAssert.Contains("every kit of yours", rows.Single(a => a.Name == "PackUpCamp").Hint); StringAssert.Contains("goes out", rows.Single(a => a.Name == "MoveFixture").Hint);
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

        // ── move: one row, a cell picker, and a check that the colours tell the truth ──

        string MoveTo(Entity fixture, int dx, int dy) { var at = Zone.GetEntityCell(fixture); return HeldFixtureActions.MoveCommand(wild, fixture, at.X + dx, at.Y + dy); }
        bool Fits(Entity fixture, int x, int y) => HeldFixtureActions.CanMoveTo(Actor, fixture, wild, x, y, out _);
        string Why(Entity fixture, int x, int y) { HeldFixtureActions.CanMoveTo(Actor, fixture, wild, x, y, out string code); return code; }

        [Test] public void AStandingKitOffersOneMoveRowThatOpensThePicker_NotARowPerDirection()
        {
            var hearth = Standing("CampHearth", "east"); var rows = World(hearth).Where(a => a.Name == "MoveFixture").ToArray();
            Assert.AreEqual(1, rows.Length); Assert.AreEqual("move", rows[0].Display); StringAssert.StartsWith(HeldFixtureActions.PickMovePrefix, rows[0].Command); StringAssert.Contains("green", rows[0].Hint); StringAssert.Contains("red", rows[0].Hint);
            Assert.False(HeldFixtureActions.IsCommand(rows[0].Command), "the row is for the picker; only the chosen cell becomes a command the kit runs");
        }

        [Test] public void MovingToAnyCellWithinReachKeepsTheKitsIdAndFuel_DiagonalsAndTwoCellsIncluded()
        {
            var hearth = Standing("CampHearth", "east"); hearth.GetPart<FuelPart>().FuelMass = 12; string id = hearth.ID; var at = Zone.GetEntityCell(hearth);
            int tx = AX + 2, ty = AY - 2; Assert.True(Fits(hearth, tx, ty), "a diagonal two cells from the player");
            Assert.True(Act(hearth, HeldFixtureActions.MoveCommand(wild, hearth, tx, ty)), string.Join("|", MessageLog.GetRecent(4))); var now = Zone.GetEntityCell(hearth);
            Assert.AreEqual(tx, now.X); Assert.AreEqual(ty, now.Y); Assert.AreEqual(id, hearth.ID); Assert.True(HeldFixtureActions.IsPlayerHeld(hearth)); Assert.AreEqual(12f, hearth.GetPart<FuelPart>().FuelMass, 0.001f);
            Assert.False(Pack.Objects.Contains(hearth)); Assert.AreEqual(1, Held().Count);
        }

        [Test] public void TheColoursTellTheTruth_EveryRedCellIsRefusedWithNoChange_AndTheFirstGreenCellEachRoundMoves()
        {
            var hearth = Standing("CampHearth", "east"); Standing("CampStill", "northeast");
            for (int round = 0; round < 4; round++)
            {
                var at = Zone.GetEntityCell(hearth); int greens = 0, reds = 0; (int x, int y)? firstGreen = null;
                for (int x = AX - 4; x <= AX + 4; x++) for (int y = AY - 4; y <= AY + 4; y++)
                {
                    if (Fits(hearth, x, y)) { greens++; if (firstGreen == null && Math.Abs(x - AX) <= 1 && Math.Abs(y - AY) <= 1) firstGreen = (x, y); continue; }   // stay within reach so the next round can lift it again
                    reds++; var before = Zone.GetEntityCell(hearth); Assert.False(Act(hearth, HeldFixtureActions.MoveCommand(wild, hearth, x, y)), "red cell " + x + "," + y + " (" + Why(hearth, x, y) + ") must be refused");
                    Assert.AreSame(before, Zone.GetEntityCell(hearth)); Assert.AreEqual(2, Held().Count, "a refused move changes nothing"); Assert.True(HeldFixtureActions.IsPlayerHeld(hearth));
                }
                Assert.Greater(greens, 0, "round " + round); Assert.Greater(reds, 0, "round " + round);
                Assert.NotNull(firstGreen, "a green cell beside the player"); Assert.True(Act(hearth, HeldFixtureActions.MoveCommand(wild, hearth, firstGreen.Value.x, firstGreen.Value.y)), "green cell " + firstGreen); Assert.AreEqual(2, Held().Count);
            }
        }

        [Test] public void EachRedCellHasTheRightReason()
        {
            var hearth = Standing("CampHearth", "east"); var still = Standing("CampStill", "northeast"); var sc = Zone.GetEntityCell(still);
            Assert.AreEqual("same-cell", Why(hearth, AX + 1, AY)); Assert.AreEqual("out-of-reach", Why(hearth, AX + 3, AY)); Assert.AreEqual("out-of-reach", Why(hearth, AX - 3, AY - 3));
            Assert.False(Fits(hearth, sc.X, sc.Y), "another kit's cell"); Assert.False(Fits(hearth, AX, AY), "your own cell"); Assert.False(Fits(hearth, -1, 5), "off the map"); Assert.AreEqual("out-of-reach", Why(hearth, -1, 5));
            Pack.MaxWeight = Pack.GetCarriedWeight() + InventoryPart.GetItemWeight(hearth) - 1; Assert.AreEqual("overweight-load", Why(hearth, AX + 1, AY + 1), "a kit you could not lift cannot be moved");
            var row = World(hearth).Single(a => a.Name == "MoveFixture"); Assert.True(row.IsDisabled); StringAssert.Contains("cannot carry", row.DisabledReason);
            Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, AX + 6, AY + 6)); Assert.AreEqual("stale-selection", Why(hearth, AX + 6, AY + 7), "not beside the kit");
        }

        [Test] public void ASolidKitCanMoveEvenThoughItsOwnOldCellWasBesideTheNewOne()
        {
            var box = Standing("CampBox", "east"); var at = Zone.GetEntityCell(box); Assert.True(Fits(box, at.X, at.Y - 1), "the box's own old cell does not count against it");
            Assert.True(Act(box, MoveTo(box, 0, -1))); Assert.AreEqual(at.Y - 1, Zone.GetEntityCell(box).Y); Assert.True(box.GetPart<PhysicsPart>().Solid);
        }

        [Test] public void NoMoveToAnOccupiedCellTheActorsCellOrBeyondReach_AndNoRowForAKitYouHaveHandedOver_Counters()
        {
            var hearth = Standing("CampHearth", "east"); var still = Standing("CampStill", "northeast"); var sc = Zone.GetEntityCell(still);
            Assert.False(Fits(hearth, sc.X, sc.Y)); Assert.False(Act(hearth, HeldFixtureActions.MoveCommand(wild, hearth, sc.X, sc.Y)), "the still stands there"); Assert.False(Act(hearth, HeldFixtureActions.MoveCommand(wild, hearth, AX, AY)), "that is you");
            Assert.False(Act(hearth, HeldFixtureActions.MoveCommand(wild, hearth, AX + 3, AY)), "three cells away");
            Assert.True(Act(hearth, "HandOver|" + Uri.EscapeDataString(wildId) + "|" + hearth.ID)); Assert.False(World(hearth).Any(a => a.Name == "MoveFixture"));
        }

        [Test] public void MovingGivesTheKitBackToItsOldCellWhenAnOuterStepFails()
        {
            var hearth = Standing("CampHearth", "east"); string command = MoveTo(hearth, 0, 1); var before = Zone.GetEntityCell(hearth); FailAfter();
            Assert.False(Act(hearth, command)); var after = Zone.GetEntityCell(hearth); Assert.AreEqual(before.X, after.X); Assert.AreEqual(before.Y, after.Y); Assert.True(HeldFixtureActions.IsPlayerHeld(hearth)); Assert.False(Pack.Objects.Contains(hearth));
        }

        [Test] public void AForgedMoveCommandIsRefused_IncludingCoordinatesThatWouldOverflow()
        {
            var hearth = Standing("CampHearth", "east"); string good = MoveTo(hearth, 0, 1); string[] f = good.Split('|');
            var at = Zone.GetEntityCell(hearth);
            foreach (var bad in new[] { "MoveFixture|", "MoveFixture|x|" + hearth.ID + "|1|1", string.Join("|", f.Take(4).Concat(new[] { "zzz" })), string.Join("|", f.Take(3).Concat(new[] { "70", "20" })),
                string.Join("|", f.Take(3).Concat(new[] { (at.X + int.MinValue).ToString(), "12" })), string.Join("|", f.Take(3).Concat(new[] { "12", int.MaxValue.ToString() })), string.Join("|", f.Take(2).Concat(new[] { "nobody", f[3], f[4] })) })
                Assert.False(Act(hearth, bad), bad);
            Assert.True(HeldFixtureActions.IsPlayerHeld(hearth)); Assert.AreEqual(1, Held().Count); Assert.True(Act(hearth, good), "the real one still works");
        }

        [Test] public void AMoveVetoedAtTheSetDownHalfPutsTheKitBackAndKeepsItsName()
        {
            var hearth = Standing("CampHearth", "east"); Assert.True(Act(hearth, World(hearth).Single(a => a.Name == "NameCamp").Command)); var before = Zone.GetEntityCell(hearth);
            hearth.AddPart(new Veto { Event = "BeforeInstall" }); Assert.False(Act(hearth, MoveTo(hearth, 0, 1)));
            Assert.AreSame(before, Zone.GetEntityCell(hearth)); Assert.True(HeldFixtureActions.IsPlayerHeld(hearth)); Assert.AreEqual("Home", hearth.GetPart<PackableFixturePart>().CampName); Assert.False(Pack.Objects.Contains(hearth));
        }

        [Test] public void AMovedKitKeepsItsCampsName_ButATakenUpOneLeavesItBehind()
        {
            var hearth = Standing("CampHearth", "east"); var box = Standing("CampBox", "north"); Assert.True(Act(hearth, World(hearth).Single(a => a.Name == "NameCamp").Command));
            Assert.True(Act(hearth, MoveTo(hearth, 0, 1))); Assert.AreEqual("Home", hearth.GetPart<PackableFixturePart>().CampName); Assert.AreEqual("Home", box.GetPart<PackableFixturePart>().CampName);
            Assert.True(Act(hearth, "TakeUp|" + Uri.EscapeDataString(wildId) + "|" + hearth.ID)); Assert.AreEqual("", hearth.GetPart<PackableFixturePart>().CampName, "a kit you took away is no longer part of that camp");
            Assert.AreEqual("Home", box.GetPart<PackableFixturePart>().CampName);
        }

        // ── the paths the rules reach: a burning kit moving, the zone cap, a veto partway through ──

        sealed class Veto : Part
        {
            public string Event; public override string Name => "CampKitVeto"; public override bool HandleEvent(GameEvent e) => !(e.ID == Event);
        }

        [Test] public void ALitHearthCanBeMovedAndKeepsItsFuelButItsFlameGoesOut_AsTakingItUpDoes()
        {
            var hearth = Standing("CampHearth", "east"); Light(hearth);
            StringAssert.Contains("goes out", World(hearth).Single(a => a.Name == "MoveFixture").Hint);
            var before = Zone.GetEntityCell(hearth); Assert.True(Act(hearth, MoveTo(hearth, 0, 1)), string.Join("|", MessageLog.GetRecent(4))); Assert.False(Zone.GetEntityCell(hearth) == before);
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

        [Test] public void ACampNameSurvivesASave_AndAFreshKitHasNone()
        {
            var hearth = Standing("CampHearth", "east"); Assert.True(Act(hearth, NameRow(hearth).Command)); var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(hearth);
            Assert.AreEqual("Home", copy.GetPart<PackableFixturePart>().CampName); Assert.AreEqual("", Factory.CreateEntity("CampHearth").GetPart<PackableFixturePart>().CampName, "a fresh kit has no name; a public string absent from a saved part loads as this default");
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

        // ── hints fit the menus they are shown in; make camp beside an existing fire; the border colours ──

        [Test] public void EveryShippedHintFitsTheMenuItAppearsIn_WorldMenuWrappedWithoutACut_PackMenuOneLine()
        {
            var hearth = Standing("CampHearth", "east"); var still = Standing("CampStill", "west"); var box = Standing("CampBox", "north");
            for (int i = 0; i < 3; i++) Carry("SalvagedTimber"); var striker = Carry("FlintStriker"); Carry("IronSpikeComponent"); Flask(12, "bog-mire"); Flask(12, "bog-mire"); Carry("MendleafSprig");
            var seen = new List<InventoryAction>();
            foreach (var temp in new[] { 25f, 450f }) { hearth.GetPart<FuelPart>().FuelMass = 12; hearth.GetPart<ThermalPart>().Temperature = temp; seen.AddRange(World(hearth)); seen.AddRange(World(still)); seen.AddRange(World(box)); }
            seen.AddRange(World(Place("AlchemyStill", 15, 10))); striker.GetPart<FlintStrikerPart>().Strikes = 5;
            var pack = new List<InventoryAction>(); pack.AddRange(Actions(striker)); var carriedHearth = Carry("CampHearth"); Carry("CampBox"); pack.AddRange(Actions(carriedHearth));
            var withHints = seen.Where(a => !string.IsNullOrEmpty(a.Hint)).ToList(); Assert.Greater(withHints.Count, 8, "the rows under test: " + string.Join(", ", seen.Select(a => a.Name).Distinct()));
            foreach (var row in withHints) { var lines = WorldActionMenuUI.WrapHint(row.Hint, 44, WorldActionMenuUI.MaxHintRows); Assert.False(lines[lines.Count - 1].EndsWith("~"), row.Name + " is cut at the narrowest world menu: " + row.Hint); }
            var packHints = pack.Where(a => !string.IsNullOrEmpty(a.Hint)).ToList(); Assert.True(packHints.Any(a => a.Name == "KnapStriker") && packHints.Any(a => a.Name == "MakeCamp"), "the pack rows under test: " + string.Join(", ", pack.Select(a => a.Name)));
            foreach (var row in packHints) Assert.LessOrEqual(row.Hint.Length, 78, row.Name + " does not fit the pack menu's detail line: " + row.Hint);
        }

        [Test] public void MakeCampBesideAnExistingHearthKeepsTheStillWithinItsReach()
        {
            var hearth = Standing("CampHearth", "east"); var still = Carry("CampStill"); Carry("CampBench"); var row = Actions(still).Single(a => a.Name == "MakeCamp");
            Assert.True(Act(still, row.Command), string.Join("|", MessageLog.GetRecent(4))); Assert.AreEqual(3, Held().Count); Assert.LessOrEqual(Dist(Zone, still, hearth), LiquidSettlingPart.FireReach, "the new still is placed by the fire that was already standing");
        }

        [Test] public void AMakeCampThatCannotPlaceAnythingSaysWhyInsteadOfAGenericRefusal()
        {
            var hearth = Carry("CampHearth"); Carry("CampStill"); string command = Actions(hearth).Single(a => a.Name == "MakeCamp").Command;
            for (int x = AX - 1; x <= AX + 1; x++) for (int y = AY - 1; y <= AY + 1; y++) if (x != AX || y != AY) Place("StoneWall", x, y);
            Assert.False(Act(hearth, command)); Assert.AreEqual(0, Held().Count); Assert.IsNotEmpty(MessageLog.GetRecent(2));
        }

        [Test] public void ThePlacementBorderIsGreenWhereItFitsAndRedWhereItDoesNot()
        {
            Assert.AreEqual(QudColorParser.BrightGreen, WorldCursorRenderer.PlaceColor(true)); Assert.AreEqual(QudColorParser.BrightRed, WorldCursorRenderer.PlaceColor(false)); Assert.AreNotEqual(WorldCursorRenderer.PlaceColor(true), WorldCursorRenderer.PlaceColor(false));
            var state = new WorldCursorState(); state.Activate(WorldCursorMode.PlaceTarget, Zone, 5, 5, 5, 5, 2, true); Assert.False(state.PlaceValid, "a fresh picker is red until a cell is checked"); state.SetPlaceValid(true); Assert.True(state.PlaceValid);
            state.Activate(WorldCursorMode.PlaceTarget, Zone, 5, 5, 5, 5, 2, true); Assert.False(state.PlaceValid, "activating resets it");
        }

        // ── the steeping trough (Docs/MACHINE-INVENTED-USES.md 3h) ──

        InventoryAction[] SteepRows(Entity trough) => World(trough).Where(a => a.Name == "Steep").ToArray();
        void GiveReagents(int resin = 1, int clay = 1) { for (int i = 0; i < resin; i++) Carry("PitchpodResin"); for (int i = 0; i < clay; i++) Carry("FireClay"); }
        EnhancementSteeped Steeped(Entity garment) => garment.Parts.OfType<EnhancementSteeped>().FirstOrDefault();

        [Test] public void TheTroughIsACampKitThatOffersNothingWhileCarried_Counter()
        {
            Assert.NotNull(PackableFixtureRegistry.Get("camp-trough")); var kit = Carry("CampTrough"); var garment = Carry("LeatherArmor"); GiveReagents();
            Assert.IsEmpty(Actions(kit).Where(a => a.Name == "Steep"), "carried, it is a kit and not a station"); Assert.NotNull(Actions(kit).FirstOrDefault(a => a.Name == "SetDownFixture"));
        }

        [Test] public void ASetDownTroughOffersARowForEachFittingGarmentAndNothingElse()
        {
            var trough = Standing("CampTrough", "east"); var leather = Carry("LeatherArmor"); var cloth = Carry("KilnfeltApron"); Carry("ChainMail"); Carry("Dagger"); GiveReagents();
            var rows = SteepRows(trough); Assert.AreEqual(2, rows.Length, string.Join(" / ", rows.Select(r => r.Display))); Assert.True(rows.All(r => !r.IsDisabled && r.Time == ActionTime.OneAction));
            Assert.True(rows.Any(r => r.Command.EndsWith(leather.ID)) && rows.Any(r => r.Command.EndsWith(cloth.ID))); StringAssert.Contains("acid resistance", rows[0].Hint); StringAssert.DoesNotContain("bath", string.Join(" ", rows.Select(r => r.Display + r.Hint)).ToLowerInvariant());
        }

        [Test] public void SteepingTakesOneResinAndOneClayAndLeavesTheGarmentWithTheTreatment()
        {
            var trough = Standing("CampTrough", "east"); var garment = Carry("LeatherArmor"); GiveReagents(2, 2); var row = SteepRows(trough).Single(); StringAssert.Contains("leather armor", row.Display);
            Assert.True(Act(trough, row.Command), string.Join("|", MessageLog.GetRecent(3))); var steep = Steeped(garment); Assert.NotNull(steep); Assert.AreEqual(35, steep.Amount); Assert.AreEqual(100, steep.TurnsLeft);
            Assert.AreEqual(1, Pack.Objects.Where(o => o.BlueprintName == "PitchpodResin").Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1)); Assert.AreEqual(1, Pack.Objects.Where(o => o.BlueprintName == "FireClay").Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1)); Assert.True(Pack.Objects.Contains(garment), "the garment is not consumed");
            StringAssert.Contains("Steeped against acid", garment.GetPart<EnhancementSteeped>().GetEffectDescription()); StringAssert.Contains("at least 1 damage", steep.GetEffectDescription());
        }

        [Test] public void WithoutTheReagentsTheRowsAreDisabledAndSaySoAndNothingChanges()
        {
            var trough = Standing("CampTrough", "east"); var garment = Carry("LeatherArmor"); var rows = SteepRows(trough); Assert.True(rows.Single().IsDisabled); StringAssert.Contains("pitchpod resin and 1 fire clay", rows.Single().DisabledReason);
            GiveReagents(1, 0); StringAssert.Contains("fire clay", SteepRows(trough).Single().DisabledReason); StringAssert.DoesNotContain("resin", SteepRows(trough).Single().DisabledReason); Assert.False(Act(trough, "SteepGarment|" + garment.ID)); Assert.Null(Steeped(garment));
            Assert.AreEqual(1, Pack.Objects.Count(o => o.BlueprintName == "PitchpodResin"), "nothing was taken");
        }

        [Test] public void AWornGarmentAMetalOneAndAStackAreNotOffered_Counters()
        {
            var trough = Standing("CampTrough", "east"); var worn = Carry("LeatherArmor"); GiveReagents(); Assert.True(InventorySystem.Equip(Actor, worn)); Assert.IsEmpty(SteepRows(trough).Where(r => !r.IsDisabled), "take it off first");
            Carry("ChainMail"); var rows = SteepRows(trough); Assert.AreEqual(1, rows.Length); Assert.True(rows[0].IsDisabled); StringAssert.Contains("leather or cloth armor", rows[0].DisabledReason);
            Assert.False(Act(trough, "SteepGarment|" + worn.ID)); Assert.Null(Steeped(worn));
        }

        [Test] public void AnInterruptedSteepGivesBackTheReagentsAndLeavesNoTreatment()
        {
            var trough = Standing("CampTrough", "east"); var garment = Carry("LeatherArmor"); GiveReagents(); string command = SteepRows(trough).Single().Command; FailAfter();
            Assert.False(Act(trough, command)); Assert.Null(Steeped(garment)); Assert.AreEqual(1, Pack.Objects.Count(o => o.BlueprintName == "PitchpodResin")); Assert.AreEqual(1, Pack.Objects.Count(o => o.BlueprintName == "FireClay"));
        }

        [Test] public void SteepingAgainReplacesWhatIsLeftAndSaysSo()
        {
            var trough = Standing("CampTrough", "east"); var garment = Carry("LeatherArmor"); GiveReagents(2, 2); Assert.True(Act(trough, SteepRows(trough).Single().Command)); Steeped(garment).TurnsLeft = 37;
            var row = SteepRows(trough).Single(); StringAssert.Contains("replaces 37 turns", row.Display); Assert.True(Act(trough, row.Command)); Assert.AreEqual(100, Steeped(garment).TurnsLeft); Assert.AreEqual(1, garment.Parts.OfType<EnhancementSteeped>().Count(), "one treatment, not two");
        }

        [Test] public void APieceAlreadyHoldingTwoOtherTreatmentsRefusesAThird_ButASteepedOneCanBeReSteeped()
        {
            var trough = Standing("CampTrough", "east"); var garment = Carry("LeatherArmor"); GiveReagents(2, 2);
            garment.AddPart(new EnhancementLacquered()); garment.AddPart(new EnhancementLacquered()); Assert.AreEqual(ItemEnhancing.MAX_ENHANCEMENTS_PER_ITEM, ItemEnhancing.CountEnhancements(garment));
            var row = SteepRows(trough).Single(); Assert.True(row.IsDisabled); Assert.AreEqual("slot-cap", row.DisabledCode); StringAssert.Contains("two treatments", row.DisabledReason); Assert.False(Act(trough, "SteepGarment|" + garment.ID)); Assert.Null(Steeped(garment));
            var other = Carry("FilterHood"); other.AddPart(new EnhancementLacquered()); Assert.True(Act(trough, SteepRows(trough).Single(r => r.Command.EndsWith(other.ID)).Command)); Assert.NotNull(Steeped(other), "one slot was free");
            Assert.True(Act(trough, SteepRows(trough).Single(r => r.Command.EndsWith(other.ID)).Command), "a steeped piece is re-steeped in place, never a third treatment"); Assert.AreEqual(2, ItemEnhancing.CountEnhancements(other));
        }

        [Test] public void TheTreatmentRefusesMetalAndCreaturesEvenWhenAppliedDirectly_Counter()
        {
            Assert.False(ItemEnhancing.Apply(Carry("ChainMail"), "EnhancementSteeped"), "metal takes no steeping"); Assert.False(ItemEnhancing.Apply(Carry("Dagger"), "EnhancementSteeped"), "not armor");
            Assert.False(ItemEnhancing.Apply(Factory.CreateEntity("CaveBear"), "EnhancementSteeped"), "a creature, whatever its armor part"); Assert.True(ItemEnhancing.Apply(Carry("LeatherArmor"), "EnhancementSteeped"));
        }

        // ── wearing it ──

        [Test] public void WearingASteepedGarmentGivesAcidResistanceForItsTurnsAndTakingItOffKeepsWhatIsLeft()
        {
            var garment = Carry("LeatherArmor"); Assert.True(ItemEnhancing.Apply(garment, "EnhancementSteeped")); var steep = Steeped(garment); int baseline = Actor.GetStatValue("AcidResistance");
            Assert.True(InventorySystem.Equip(Actor, garment)); Assert.AreEqual(baseline + 35, Actor.GetStatValue("AcidResistance")); var effect = Actor.GetEffect<SteepedAcidEffect>(); Assert.NotNull(effect); Assert.AreEqual(100, effect.Duration);
            for (int i = 0; i < 30; i++) effect.OnTurnEnd(Actor); effect.JustApplied = false;
            Assert.True(InventorySystem.UnequipItem(Actor, garment)); Assert.AreEqual(baseline, Actor.GetStatValue("AcidResistance"), "off, no protection"); Assert.IsNull(Actor.GetEffect<SteepedAcidEffect>());
            Assert.AreEqual(70, steep.TurnsLeft, "paused at what was left"); StringAssert.Contains("70 more turns", steep.GetEffectDescription());
            Assert.True(InventorySystem.Equip(Actor, garment)); Assert.AreEqual(70, Actor.GetEffect<SteepedAcidEffect>().Duration, "and resumes from it");
        }

        [Test] public void WhenTheTurnsRunOutTheGarmentIsPlainAgainAndTheWearerSaysSo()
        {
            var garment = Carry("LeatherArmor"); Assert.True(ItemEnhancing.Apply(garment, "EnhancementSteeped")); Steeped(garment).TurnsLeft = 2; int baseline = Actor.GetStatValue("AcidResistance");
            Assert.True(InventorySystem.Equip(Actor, garment)); var effect = Actor.GetEffect<SteepedAcidEffect>(); effect.JustApplied = false;
            var status = Actor.GetPart<StatusEffectsPart>(); for (int i = 0; i < 3; i++) { var end = GameEvent.New("EndTurn"); Actor.FireEvent(end); }
            Assert.Null(Actor.GetEffect<SteepedAcidEffect>()); Assert.AreEqual(baseline, Actor.GetStatValue("AcidResistance")); Assert.Null(Steeped(garment), "the treatment is gone from the garment"); Assert.True(MessageLog.GetRecent(6).Any(m => m.Contains("has worn off")));
        }

        [Test] public void TwoSteepedGarmentsWornTogetherEachGiveTheirOwn_AndSteepingNeverStacksPastTheCap()
        {
            var body = Carry("LeatherArmor"); var hood = Carry("FilterHood"); foreach (var g in new[] { body, hood }) Assert.True(ItemEnhancing.Apply(g, "EnhancementSteeped")); int baseline = Actor.GetStatValue("AcidResistance");
            Assert.True(InventorySystem.Equip(Actor, body)); Assert.True(InventorySystem.Equip(Actor, hood)); Assert.AreEqual(2, Actor.GetPart<StatusEffectsPart>().GetAllEffects().Count(e => e is SteepedAcidEffect));
            Assert.AreEqual(baseline + 70, Actor.GetStatValue("AcidResistance"), "each garment lands its own +35");
            Assert.True(InventorySystem.UnequipItem(Actor, body)); Assert.AreEqual(baseline + 35, Actor.GetStatValue("AcidResistance"), "only the hood's now"); Assert.AreEqual(1, Actor.GetPart<StatusEffectsPart>().GetAllEffects().Count(e => e is SteepedAcidEffect)); Assert.True(InventorySystem.UnequipItem(Actor, hood)); Assert.AreEqual(baseline, Actor.GetStatValue("AcidResistance"));
        }

        [Test] public void TheTreatmentTravelsWithTheGarmentAndSavesWithIt()
        {
            var garment = Carry("LeatherArmor"); Assert.True(ItemEnhancing.Apply(garment, "EnhancementSteeped")); Steeped(garment).TurnsLeft = 64; var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(garment);
            Assert.AreEqual(64, copy.Parts.OfType<EnhancementSteeped>().Single().TurnsLeft); Assert.AreEqual(35, copy.Parts.OfType<EnhancementSteeped>().Single().Amount);
        }

        [Test] public void TheTroughReaderSaysWhatItDoesAndWhatYouCarry()
        {
            var trough = Standing("CampTrough", "east"); var lines = new List<string>(); StateReader.Collect(trough, false, Zone, Zone.GetEntityCell(trough), Actor, lines);
            StringAssert.Contains("Nothing you carry fits", string.Join(" ", lines)); Carry("LeatherArmor"); GiveReagents(); lines.Clear(); StateReader.Collect(trough, false, Zone, Zone.GetEntityCell(trough), Actor, lines);
            StringAssert.Contains("1 garment you carry could be steeped", string.Join(" ", lines)); StringAssert.Contains("1 pitchpod resin and 1 fire clay", string.Join(" ", lines));
        }

        [Test] public void TheGarmentsReaderIsRightWhileItIsWorn_AndAnEffectThatEndsAnotherWayNeverRefillsIt()
        {
            var garment = Carry("LeatherArmor"); Assert.True(ItemEnhancing.Apply(garment, "EnhancementSteeped")); Assert.True(InventorySystem.Equip(Actor, garment)); var effect = Actor.GetEffect<SteepedAcidEffect>(); effect.JustApplied = false;
            for (int i = 0; i < 12; i++) { var end = GameEvent.New("EndTurn"); Actor.FireEvent(end); }
            Assert.AreEqual(88, Steeped(garment).TurnsLeft, "worn: the reader counts down with the wearer"); StringAssert.Contains("88 more turns", Steeped(garment).GetEffectDescription());
            Actor.GetPart<StatusEffectsPart>().RemoveEffect(effect);   // dispelled, not taken off
            Assert.True(InventorySystem.UnequipItem(Actor, garment)); Assert.True(InventorySystem.Equip(Actor, garment)); Assert.AreEqual(88, Actor.GetEffect<SteepedAcidEffect>().Duration, "put back on, it resumes from what was left, not from a full count");
        }

        [Test] public void AGarmentWornAtSaveTimeLoadsWithItsResistanceAndItsTurnsAndNeverGainsOrLosesAny()
        {
            var garment = Carry("LeatherArmor"); Assert.True(ItemEnhancing.Apply(garment, "EnhancementSteeped")); Assert.True(InventorySystem.Equip(Actor, garment)); Actor.GetEffect<SteepedAcidEffect>().JustApplied = false;
            for (int i = 0; i < 5; i++) { var end = GameEvent.New("EndTurn"); Actor.FireEvent(end); }
            int resist = Actor.GetStatValue("AcidResistance"); var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor); var effect = copy.GetEffect<SteepedAcidEffect>();
            Assert.NotNull(effect, "the effect is saved"); Assert.AreEqual(95, effect.Duration); Assert.AreEqual(35, effect.AppliedBonus); Assert.AreEqual(resist, copy.GetStatValue("AcidResistance"), "the saved boost is not applied twice on load");
        }
    }
}
