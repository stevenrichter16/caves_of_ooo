using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Natural heat at the still (Docs/MACHINE-INVENTED-USES.md, 3p). A terrain that declares a steady heat of its own (a steam vent, an ash bed) is a heat source the still can use
    /// for settling without a fire, so the batch spends no fuel. It is not a kiln: baking clay still needs a real fire. The source is read from the world's own rule (the terrain's
    /// declared steady heat), not from a list of names, so it is nothing a still could mint: no input is created, only the five fuel a fire would have spent is not spent.
    /// </summary>
    public sealed class NaturalHeatSettlingTests : FiftyWorldFixture
    {
        [SetUp] public void SetupNatural() { Diag.ResetAll(); MessageLog.Clear(); AlchemyStillPart.Factory = Factory; }
        [TearDown] public void TearNatural() { AlchemyStillPart.Factory = null; }

        Entity Still(int x = 11) => Place("AlchemyStill", x, 10);
        Entity Vent(int x = 9, string blueprint = "SteamVent") => Place(blueprint, x, 10);
        Entity Fire(int x = 9, float fuel = 25f) { var f = Place("SpreadCookingCoals", x, 10); f.GetPart<FuelPart>().FuelMass = fuel; f.GetPart<ThermalPart>().Temperature = 500; return f; }
        float Fuel(Entity f) => f.GetPart<FuelPart>().FuelMass;
        LiquidVesselPart Vessel(Entity f) => f.GetPart<LiquidVesselPart>();
        InventoryAction[] Rows(Entity still) => Actions(still).Where(a => a.Name == "SettleLiquid").ToArray();
        InventoryAction[] AllRows(Entity still) => Actions(still).Where(a => a.Name == "SettleAll").ToArray();
        InventoryAction[] Bakes(Entity still) => Actions(still).Where(a => a.Name == "BakeClay").ToArray();
        string Row(Entity still, string target) => Rows(still).Single(a => !a.IsDisabled && a.Command.StartsWith("SettleLiquid|" + target + "|", StringComparison.Ordinal)).Command;
        int Clay() => Pack.Objects.Where(o => o.BlueprintName == "FireClay").Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1);
        string Read(Entity e) { var l = new List<string>(); StateReader.Collect(e, true, Zone, Zone.GetEntityCell(e), Actor, l); return string.Join(" ", l); }   // Full: the quick reader passes no viewer, so a describer that needs one is read here
        InventoryAction[] WorldRows(Entity still)
        {
            var at = Zone.GetEntityCell(still);
            var origin = Zone.GetEntityCell(Actor);
            at.IsVisible = at.Explored = origin.IsVisible = origin.Explored = true;
            return CellMenu(at.X, at.Y).ToArray();
        }

        [Test] public void AVentBesideTheStillSettlesMireWithNoFireAndSpendsNoFuel()
        {
            var still = Still(); Vent(); var flask = Flask(12, "bog-mire");
            Assert.True(Rows(still).Any(a => !a.IsDisabled), "a vent two cells away is heat enough for settling"); StringAssert.Contains("vent", Rows(still).First(a => !a.IsDisabled).Hint.ToLowerInvariant());
            Assert.True(Act(still, Row(still, "water")), string.Join("|", MessageLog.GetRecent(3)));
            Assert.AreEqual("water", Vessel(flask).LiquidId); Assert.AreEqual(3, Vessel(flask).Volume, "still four mire to one; nothing is created");
            var record = DiagQuery.Apply(new DiagQuery.Filter { Category = "liquid", Kind = "Settled", Limit = 5 }).Records.Single(); StringAssert.Contains("\"fuel\":0", record.PayloadJson); StringAssert.Contains("vent", record.PayloadJson);
            StringAssert.Contains("vent", string.Join("|", MessageLog.GetRecent(3)).ToLowerInvariant());
        }

        [Test] public void TheVentPaysBeforeAFireSoTheFiresFuelIsKept_AndWithoutTheVentTheFirePaysAsBefore()
        {
            var still = Still(); var vent = Vent(9); var fire = Fire(10, 25f); var flask = Flask(12, "bog-mire");
            Assert.True(Act(still, Row(still, "oil"))); Assert.AreEqual(25f, Fuel(fire), 0.001f, "the vent paid; the fire kept all its fuel"); Assert.AreEqual("oil", Vessel(flask).LiquidId);
            Zone.RemoveEntity(vent); var next = Flask(12, "bog-mire");
            Assert.True(Act(still, Rows(still).First(a => !a.IsDisabled && a.Command.Contains("|water|") && a.Command.Contains(next.ID)).Command)); Assert.AreEqual(20f, Fuel(fire), 0.001f, "no vent now: five fuel as before");
        }

        [Test] public void AVentOutOfReachOrNoLongerSteadyDoesNotCount_Counters()
        {
            var still = Still(); var flask = Flask(12, "bog-mire");
            var far = Vent(14); Assert.True(Rows(still).All(a => a.IsDisabled), "three cells is out of reach"); Assert.False(Act(still, "SettleLiquid|water|" + flask.ID + "|12")); Zone.RemoveEntity(far);
            var vent = Vent(9); var source = vent.GetPart<TileStateSourcePart>(); Assert.True(Rows(still).Any(a => !a.IsDisabled), "in reach and steady");
            source.Heat = 0; Assert.True(Rows(still).All(a => a.IsDisabled), "a terrain that no longer seeds heat is not a source");
            source.Heat = 1; source.Cold = 1; Assert.True(Rows(still).All(a => a.IsDisabled), "a source that also chills is not steady heat");
            Assert.AreEqual(12, Vessel(flask).Volume); Assert.AreEqual("bog-mire", Vessel(flask).LiquidId);
        }

        [Test] public void AnAshBedCountsToo_TheRuleIsTheTerrainsDeclaredHeatNotAName()
        {
            var still = Still(); Vent(9, "AshBed"); var flask = Flask(12, "bog-mire"); Assert.True(Act(still, Row(still, "water"))); Assert.AreEqual("water", Vessel(flask).LiquidId);
        }

        [Test] public void SettleAllUsesTheVentForEveryFlaskWithNoFuelCap()
        {
            var still = Still(); Vent(); var a = Flask(12, "bog-mire"); var b = Flask(8, "bog-mire"); var c = Flask(16, "bog-mire");
            var row = AllRows(still).Single(r => r.Command == "SettleAll|water"); StringAssert.Contains("all 3 flasks", row.Display); StringAssert.Contains("9 water", row.Display, "3 + 2 + 4");
            Assert.True(Act(still, "SettleAll|oil"), string.Join("|", MessageLog.GetRecent(3))); Assert.AreEqual(3, Vessel(a).Volume); Assert.AreEqual(2, Vessel(b).Volume); Assert.AreEqual(4, Vessel(c).Volume);
            Assert.AreEqual(3, DiagQuery.Apply(new DiagQuery.Filter { Category = "liquid", Kind = "Settled", Limit = 9 }).Records.Count);
        }

        [TestCase(false, 9)]
        [TestCase(true, 12)]
        public void ASettlingReceiptNamesTheActualDiscardedMire(bool batch, int discarded)
        {
            var still = Still();
            Vent();
            var fire = Fire(10, 25f);
            var first = Flask(12, "bog-mire");
            var second = batch ? Flask(4, "bog-mire") : null;
            var firstVessel = Vessel(first);
            var secondVessel = second == null ? null : Vessel(second);
            string command = batch
                ? AllRows(still).Single(row => !row.IsDisabled && row.Command == "SettleAll|water").Command
                : Row(still, "water");

            Assert.True(Act(still, command));
            Assert.AreSame(firstVessel, Vessel(first));
            Assert.AreSame(Actor, first.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual("water", firstVessel.LiquidId);
            Assert.AreEqual(3, firstVessel.Volume);
            if (batch)
            {
                Assert.AreSame(secondVessel, Vessel(second));
                Assert.AreSame(Actor, second.GetPart<PhysicsPart>().InInventory);
                Assert.AreEqual("water", secondVessel.LiquidId);
                Assert.AreEqual(1, secondVessel.Volume);
            }
            Assert.AreEqual(25f, Fuel(fire), 0.001f);
            string receipt = string.Join(" ", MessageLog.GetRecent(4));
            StringAssert.Contains(discarded + " measures of mire are discarded as dregs.", receipt);
            StringAssert.DoesNotContain("The dregs stay in the pot", receipt);
        }

        [Test]
        public void APartialSettlingReceiptCountsOnlyTheFlaskTheFireCouldPayFor()
        {
            var still = Still();
            var fire = Fire(fuel: 5f);
            var flasks = new[] { Flask(4, "bog-mire"), Flask(11, "bog-mire") };
            var ordered = flasks.OrderBy(flask => flask.ID, StringComparer.Ordinal).ToArray();
            var first = Vessel(ordered[0]);
            var second = Vessel(ordered[1]);
            first.Volume = 11;
            second.Volume = 4;
            var row = AllRows(still).Single(action => !action.IsDisabled && action.Command == "SettleAll|water");

            Assert.True(Act(still, row.Command));
            Assert.AreEqual("water", first.LiquidId);
            Assert.AreEqual(2, first.Volume);
            Assert.AreEqual("bog-mire", second.LiquidId);
            Assert.AreEqual(4, second.Volume);
            Assert.AreSame(first, Vessel(ordered[0]));
            Assert.AreSame(second, Vessel(ordered[1]));
            Assert.AreEqual(0f, Fuel(fire));
            string receipt = string.Join(" ", MessageLog.GetRecent(4));
            StringAssert.Contains("You settle 1 of 2 flasks of mire over the fire: 2 water.", receipt);
            StringAssert.Contains("9 measures of mire are discarded as dregs.", receipt);
        }

        [Test] public void AVentIsNotAKilnSoBakingClayStillNeedsAFireAndSaysWhy()
        {
            var still = Still(); Vent(); var flask = Flask(12, "bog-mire"); var row = Bakes(still).Single(); Assert.True(row.IsDisabled); StringAssert.Contains("not hot enough", row.DisabledReason);
            Assert.False(Act(still, "BakeClay|" + flask.ID + "|12")); Assert.AreEqual(0, Clay()); Assert.AreEqual(12, Vessel(flask).Volume);
            var fire = Fire(10, 25f); Assert.False(Bakes(still).Single().IsDisabled, "a real fire beside the vent bakes as before"); Assert.True(Act(still, Bakes(still).Single().Command)); Assert.AreEqual(15f, Fuel(fire), 0.001f); Assert.AreEqual(1, Clay());
        }

        [Test] public void TheReaderSaysTheVentIsDoingTheWork_AndNamesNoFuel()
        {
            var still = Still(); Vent(); Flask(12, "bog-mire"); string text = Read(still); StringAssert.Contains("settle your mire", text); StringAssert.Contains("vent", text.ToLowerInvariant()); StringAssert.DoesNotContain("fuel", text.ToLowerInvariant());
        }

        [Test]
        public void VentOnlyReaderOffersSettlingAndRepeatsTheWorldMenusClayRefusal()
        {
            var still = Still();
            Vent();
            var flask = Flask(12, "bog-mire");
            var rows = WorldRows(still);
            Assert.True(rows.Any(row => row.Name == "SettleLiquid" && !row.IsDisabled));
            var bake = rows.Single(row => row.Name == "BakeClay");
            Assert.True(bake.IsDisabled);
            Assert.That(bake.DisabledReason, Does.Contain("not hot enough").IgnoreCase);

            string text = Read(still);
            Assert.That(text, Does.Contain(bake.DisabledReason.TrimEnd('.')).IgnoreCase);
            Assert.That(text, Does.Not.Contain("or bake").IgnoreCase);
            Assert.That(text, Does.Not.Contain("you can bake").IgnoreCase);
            Assert.That(text, Does.Not.Contain("fuel").IgnoreCase);
            Assert.AreEqual(12, Vessel(flask).Volume);
        }

        [TestCase(5f)] [TestCase(9f)]
        public void AFireThatCanSettleButCannotBakeExplainsTheSameTenFuelRequirement(float fuel)
        {
            var still = Still();
            var fire = Fire(fuel: fuel);
            var flask = Flask(12, "bog-mire");
            var rows = WorldRows(still);
            Assert.True(rows.Any(row => row.Name == "SettleLiquid" && !row.IsDisabled));
            var bake = rows.Single(row => row.Name == "BakeClay");
            Assert.True(bake.IsDisabled);
            Assert.That(bake.DisabledReason, Does.Contain("10 fuel").IgnoreCase);
            Assert.That(Read(still), Does.Contain(bake.DisabledReason.TrimEnd('.')).IgnoreCase);
            Assert.That(Read(still), Does.Not.Contain("or bake").IgnoreCase);
            Assert.AreEqual(fuel, Fuel(fire));
            Assert.AreEqual(12, Vessel(flask).Volume);
        }

        [Test]
        public void AReadyBakeNamesThePayingFireAndTheFourMireThatStayInTheFlask()
        {
            var still = Still();
            var fire = Fire(9, 12f);
            fire.GetPart<RenderPart>().DisplayName = "workshop coals";
            Zone.GetEntityCell(fire).IsVisible = true;
            var nearerFire = Fire(10, 7f);
            var flask = Flask(12, "bog-mire");
            var bake = WorldRows(still).Single(row => row.Name == "BakeClay" && !row.IsDisabled);
            foreach (var preview in new[] { bake.Hint, Read(still) })
            {
                Assert.That(preview, Does.Contain("workshop coals").IgnoreCase);
                Assert.That(preview, Does.Contain("10 fuel").IgnoreCase);
                Assert.That(preview, Does.Contain("4 mire").IgnoreCase);
            }

            Assert.True(ChooseRow(bake, 11, 10));
            Assert.AreEqual(2f, Fuel(fire));
            Assert.AreEqual(7f, Fuel(nearerFire));
            Assert.AreEqual(4, Vessel(flask).Volume);
            Assert.AreEqual("bog-mire", Vessel(flask).LiquidId);
            Assert.AreEqual(1, Clay());
        }

        [Test]
        public void AnUnseenFireCanSupplyHeatWithoutRevealingItsNameOrFuel()
        {
            var still = Still();
            var fire = Fire(9, 17.25f);
            fire.GetPart<RenderPart>().DisplayName = "workshop coals";
            Zone.GetEntityCell(fire).IsVisible = false;
            Flask(12, "bog-mire");

            var bake = WorldRows(still).Single(row => row.Name == "BakeClay" && !row.IsDisabled);
            foreach (var preview in new[] { bake.Hint, Read(still) })
            {
                Assert.That(preview, Does.Contain("10 fuel"));
                Assert.That(preview, Does.Not.Contain("workshop coals"));
                Assert.That(preview, Does.Not.Contain("17.25"));
            }
        }

        [Test]
        public void TwoShortFlasksNeedFourMireInOneFlaskAndNeverPreviewZeroOutput()
        {
            var still = Still();
            Vent();
            var first = Flask(2, "bog-mire");
            var second = Flask(2, "bog-mire");
            var rows = WorldRows(still).Where(row => row.Name == "SettleLiquid" || row.Name == "SettleAll").ToArray();
            Assert.That(rows, Is.Not.Empty);
            Assert.That(rows.All(row => row.IsDisabled), Is.True);
            Assert.That(rows.All(row => row.DisabledReason.IndexOf("in one flask", StringComparison.OrdinalIgnoreCase) >= 0), Is.True);

            string text = Read(still);
            Assert.That(text, Does.Contain("in one flask").IgnoreCase);
            Assert.That(text, Does.Not.Match(@"(?i)\b0 (?:clean )?(?:water|oil)\b"));
            Assert.AreEqual(2, Vessel(first).Volume);
            Assert.AreEqual(2, Vessel(second).Volume);
        }

        [Test] public void TheVentSaysWhatItIsGoodForOnlyWhereAStillCouldStandAndToSomeoneWhoCouldUseIt()
        {
            // this fixture zone takes no set-down kit, so only a standing still within reach makes the line true
            var vent = Vent(20); var flask = Flask(12, "bog-mire");
            Assert.False(Read(vent).Contains("steady heat"), "no still near and a zone that takes no kit: the line would promise a use that is not there");
            var far = Still(24); Assert.False(Read(vent).Contains("steady heat"), "a still four cells away is out of reach"); Zone.RemoveEntity(far);
            var near = Still(22); string text = Read(vent); StringAssert.Contains("steady heat", text); StringAssert.Contains("no fuel", text); StringAssert.Contains("not hot enough to bake", text);
            Pack.RemoveObject(flask); Assert.False(Read(vent).Contains("steady heat"), "empty-handed: nothing"); var water = Flask(12, "water"); Assert.False(Read(vent).Contains("steady heat"), "a flask of something else does not count"); Pack.RemoveObject(water);
            var grass = Place("Grass", 21, 11); grass.AddPart(new TileStateSourcePart { Heat = 0, Coating = "water" }); Flask(12, "bog-mire"); Assert.False(Read(grass).Contains("steady heat"), "a terrain with no steady heat says nothing"); Assert.NotNull(near);
        }

        [Test] public void ACarriedOrWornThingIsNeverASourceEvenIfItDeclaresHeat_Counter()
        {
            var vent = Factory.CreateEntity("SteamVent"); Assert.True(NaturalHeat.IsSource(vent), "a loose vent entity declares steady heat"); Assert.True(Pack.AddObject(vent));
            Assert.False(NaturalHeat.IsSource(vent), "carried, it is a thing in a pack and no heat source");
        }

        [Test] public void ACarriedStillOffersNothingNearAVent_Counter()
        {
            var kit = Carry("CampStill"); Vent(); Flask(12, "bog-mire"); Assert.False(Actions(kit).Any(a => a.Name == "SettleLiquid" || a.Name == "SettleAll"));
        }

        [Test] public void AnOuterFailureGivesTheMireBack_AndTheVentIsNeverTouched()
        {
            var still = Still(); var vent = Vent(); var flask = Flask(12, "bog-mire"); string row = Row(still, "water"); int heatBefore = vent.GetPart<TileStateSourcePart>().Heat; FailAfter();
            Assert.False(Act(still, row)); Assert.AreEqual("bog-mire", Vessel(flask).LiquidId); Assert.AreEqual(12, Vessel(flask).Volume); Assert.AreEqual(heatBefore, vent.GetPart<TileStateSourcePart>().Heat); Assert.NotNull(vent.GetPart<TileStateSourcePart>());
        }

        [Test] public void AFireThatAlsoDeclaresAHeatSourceStillPaysItsFuel_Counter()
        {
            var still = Still(); var fire = Fire(9, 25f); fire.AddPart(new TileStateSourcePart { Heat = 1 }); var flask = Flask(12, "bog-mire");
            Assert.True(Act(still, Row(still, "water"))); Assert.AreEqual(20f, Fuel(fire), 0.001f, "a fuel-burning fire is a fire however it seeds heat: it is never a free source"); Assert.AreEqual("water", Vessel(flask).LiquidId);
        }

        [Test] public void ACreatureDeclaringHeatIsNeverASource_Counter()
        {
            var still = Still(); var flask = Flask(12, "bog-mire");
            var stand = Place("Magpie", 10, 11); stand.AddPart(new TileStateSourcePart { Heat = 1 }); Assert.True(Rows(still).All(a => a.IsDisabled), "a creature that seeds heat is not terrain"); Assert.AreEqual(12, Vessel(flask).Volume);
        }
    }

    /// <summary>The same rule met the way a player meets it: a camp still set down with the real verb in wild country, a couple of cells from a vent, settling on the vent's heat.</summary>
    public sealed class NaturalHeatCampTests : FiftyWorldFixture
    {
        OverworldZoneManager manager; Zone wild; const int VentX = 30, Row = 11;

        [SetUp] public void SetupCamp()
        {
            PackableFixtureRegistry.ResetForTests(); Diag.ResetAll(); MessageLog.Clear(); Clock = new TurnManager();
            manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            string wildId = Enumerable.Range(0, WorldMap.Width * WorldMap.Height).Select(i => WorldMap.ToZoneID(i % WorldMap.Width, i / WorldMap.Width, 0))
                .First(id => HeldPlaceZones.Classify(manager, id) == HeldPlaceZoneClass.WildSurface);
            wild = new Zone(wildId);
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++) Assert.True(wild.AddEntity(Factory.CreateEntity("Grass"), x, y));
            Zone.RemoveEntity(Actor); Zone = wild; manager.ReplaceLoadedState(new Dictionary<string, Zone> { { wildId, wild } }, wildId, new Dictionary<string, List<ZoneConnection>>());
            SettlementRuntime.ActiveZone = wild; Pack.MaxWeight = 400;
            var vent = Factory.CreateEntity("SteamVent"); Assert.True(wild.AddEntity(vent, VentX, Row)); vent.GetPart<TileStateSourcePart>().Seed(wild, VentX, Row);
        }
        [TearDown] public void TearCamp() { PackableFixtureRegistry.ResetForTests(); }

        InventoryAction[] SetDownRows(Entity kit) => Actions(kit).Where(a => a.Name == "SetDownFixture" && !a.IsDisabled).ToArray();
        static string Dir(InventoryAction a) => a.Display.StartsWith("set down ", StringComparison.Ordinal) ? a.Display.Substring(9).Split(' ')[0] : "";
        void StandAt(int x) { if (wild.GetEntityCell(Actor) != null) wild.RemoveEntity(Actor); Assert.True(wild.AddEntity(Actor, x, Row)); }

        string Read(Entity e) { var l = new List<string>(); StateReader.Collect(e, true, wild, wild.GetEntityCell(e), Actor, l); return string.Join(" ", l); }
        Entity Vent() => wild.GetReadOnlyEntities().Single(e => e.BlueprintName == "SteamVent");

        [Test] public void InOpenWildGroundTheVentSaysItsUseToACarrierOfMireOrAStillKitAndToNoOneElse()
        {
            StandAt(27); var vent = Vent(); Assert.False(Read(vent).Contains("steady heat"), "empty-handed");
            var kit = Carry("CampStill"); StringAssert.Contains("steady heat", Read(vent), "a still kit, in a zone that takes it"); Pack.RemoveObject(kit);
            var water = Flask(12, "water"); Assert.False(Read(vent).Contains("steady heat"), "a flask of water"); Pack.RemoveObject(water);
            Flask(12, "bog-mire"); StringAssert.Contains("settles bog-mire", Read(vent), "mire, in a zone that takes a kit");
        }

        [Test] public void TheCellBesideTheVentIsRefusedBecauseOfTheVent_AndTheSameCellIsOpenWithoutIt()
        {
            StandAt(28); var kit = Carry("CampStill"); Assert.False(SetDownRows(kit).Any(r => Dir(r) == "east"), "the cell beside the vent (29) is a hazard; nothing else is near it: " + string.Join(" / ", Actions(kit).Select(a => a.Display + (a.IsDisabled ? " [" + a.DisabledCode + "]" : ""))));
            wild.RemoveEntity(Vent()); wild.TileState.Clear(VentX, Row); Assert.True(SetDownRows(kit).Any(r => Dir(r) == "east"), "the same cell, with the vent gone, takes the kit: the vent was the only reason it was refused");
        }

        [Test] public void AStillSetDownTwoCellsFromAVentSettlesOnItsHeat()
        {
            StandAt(27); var kit = Carry("CampStill"); var row = SetDownRows(kit).FirstOrDefault(r => Dir(r) == "east");
            Assert.NotNull(row, "two cells from the vent is open ground: " + string.Join(" / ", Actions(kit).Select(a => a.Display + (a.IsDisabled ? " [" + a.DisabledCode + "]" : ""))));
            Assert.True(Act(kit, row.Command), string.Join("|", MessageLog.GetRecent(3)));
            var flask = Flask(12, "bog-mire"); var settle = Actions(kit).First(a => a.Name == "SettleLiquid" && !a.IsDisabled && a.Command.Contains("|water|")); StringAssert.Contains("vent", settle.Hint.ToLowerInvariant());
            Assert.True(Act(kit, settle.Command), string.Join("|", MessageLog.GetRecent(3))); Assert.AreEqual("water", flask.GetPart<LiquidVesselPart>().LiquidId); Assert.AreEqual(3, flask.GetPart<LiquidVesselPart>().Volume);
            // the refusal beside the vent, with a control that it is the vent, is TheCellBesideTheVentIsRefusedBecauseOfTheVent_AndTheSameCellIsOpenWithoutIt
        }
    }
}
