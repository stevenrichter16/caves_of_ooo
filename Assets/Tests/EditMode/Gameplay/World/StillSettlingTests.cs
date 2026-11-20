using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Settling at the still (Docs/MACHINE-INVENTED-USES.md, "fractionating still"). One finite feed, bog-mire drawn into a flask, becomes either clean water or oil, four units to one,
    /// chosen at the still, over a lit fire that spends a fixed measure of its own fuel. The two outputs compete for the same feed; the dregs are spent. Brine is deliberately not
    /// settled (a free salt source would inflate the Beating's money). Carried, the still is a kit and offers nothing.
    /// </summary>
    public sealed class StillSettlingTests : FiftyWorldFixture
    {
        [SetUp] public void SetupSettling() { Diag.ResetAll(); MessageLog.Clear(); }

        Entity Still(int x = 11) => Place("AlchemyStill", x, 10);
        Entity Fire(int x = 9, float fuel = 25f) { var f = Place("SpreadCookingCoals", x, 10); f.GetPart<FuelPart>().FuelMass = fuel; f.GetPart<ThermalPart>().Temperature = 500; return f; }
        float Fuel(Entity f) => f.GetPart<FuelPart>().FuelMass;
        InventoryAction[] Rows(Entity still) => Actions(still).Where(a => a.Name == "SettleLiquid").ToArray();
        string Row(Entity still, string target, int volume = 12) => Rows(still).Single(a => !a.IsDisabled && a.Command.StartsWith("SettleLiquid|" + target + "|", StringComparison.Ordinal)).Command;
        LiquidVesselPart Vessel(Entity f) => f.GetPart<LiquidVesselPart>();

        [Test] public void BothTheCommunalAndTheCampStillCarryThePartSoTheirRulesMatch()
        {
            Assert.NotNull(Factory.CreateEntity("AlchemyStill").GetPart<LiquidSettlingPart>()); Assert.NotNull(Factory.CreateEntity("CampStill").GetPart<LiquidSettlingPart>());
        }

        [Test] public void AFlaskOfMireSettlesToWaterOverALitFireAndTheFireSpendsItsFuel()
        {
            var still = Still(); var fire = Fire(); var flask = Flask(12, "bog-mire"); StringAssert.Contains("3 water", Rows(still).First(a => a.Command.Contains("|water|")).Display);
            Assert.True(Act(still, Row(still, "water")), string.Join("|", MessageLog.GetRecent(3)));
            Assert.AreEqual("water", Vessel(flask).LiquidId); Assert.AreEqual(3, Vessel(flask).Volume, "four units of mire for one of water"); Assert.AreEqual(20f, Fuel(fire), 0.001f, "the fire spent 5 fuel");
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "liquid", Kind = "Settled", Limit = 5 }).Records.Count);
        }

        [Test] public void TheSameFeedCanGoToOilInstead_TwoCompetingOutputs()
        {
            var still = Still(); var fire = Fire(); var flask = Flask(12, "bog-mire"); Assert.True(Act(still, Row(still, "oil")));
            Assert.AreEqual("oil", Vessel(flask).LiquidId); Assert.AreEqual(3, Vessel(flask).Volume); Assert.AreEqual(20f, Fuel(fire), 0.001f);
        }

        [Test] public void TheRemainderBelowFourIsSpentAndNeverBecomesAnythingBigger()
        {
            var still = Still(); Fire(); var flask = Flask(11, "bog-mire"); Assert.True(Act(still, Row(still, "water", 11))); Assert.AreEqual(2, Vessel(flask).Volume, "11 mire is two batches of four; the 3 left over are the dregs");
            var big = Flask(12, "bog-mire"); Assert.True(Act(still, Row(still, "water"))); Assert.LessOrEqual(Vessel(big).Volume * 4, 12, "output times four never exceeds the feed");
        }

        [Test] public void WithoutALitFireTheRowIsDisabledAndSaysSo_NothingChanges()
        {
            var still = Still(); var flask = Flask(12, "bog-mire"); var rows = Rows(still); Assert.True(rows.Length > 0 && rows.All(a => a.IsDisabled)); StringAssert.Contains("fire", rows[0].DisabledReason.ToLowerInvariant());
            Assert.False(Act(still, "SettleLiquid|water|" + flask.ID + "|12")); Assert.AreEqual("bog-mire", Vessel(flask).LiquidId); Assert.AreEqual(12, Vessel(flask).Volume);
        }

        [Test] public void AColdOrSpentOrDistantFireDoesNotCount_Counters()
        {
            var still = Still(); var flask = Flask(12, "bog-mire"); var cold = Fire(9, 25f); cold.GetPart<ThermalPart>().Temperature = 25; Assert.True(Rows(still).All(a => a.IsDisabled), "a cold fire");
            cold.GetPart<ThermalPart>().Temperature = 500; cold.GetPart<FuelPart>().FuelMass = 0; Assert.True(Rows(still).All(a => a.IsDisabled), "a spent fire");
            cold.GetPart<FuelPart>().FuelMass = 25; Zone.RemoveEntity(cold); Place("SpreadCookingCoals", 16, 10); Assert.True(Rows(still).All(a => a.IsDisabled), "a fire too far from the still");
            Assert.AreEqual(12, Vessel(flask).Volume);
        }

        [Test] public void AFireThatCannotSpareTheFuelRefusesWithTheNumbers()
        {
            var still = Still(); var fire = Fire(9, 3f); var flask = Flask(12, "bog-mire"); var rows = Rows(still); Assert.True(rows.All(a => a.IsDisabled)); StringAssert.Contains("5", rows[0].DisabledReason); StringAssert.Contains("3", rows[0].DisabledReason);
            Assert.False(Act(still, "SettleLiquid|water|" + flask.ID + "|12")); Assert.AreEqual(3f, Fuel(fire), 0.001f); Assert.AreEqual(12, Vessel(flask).Volume);
        }

        [Test] public void ALitCampHearthPaysEvenWhenItIsHotToCookButNotAsHotAsAFlame()
        {
            var still = Still(); var hearth = Place("CampHearth", 9, 10); hearth.GetPart<PhysicsPart>().Takeable = false; hearth.GetPart<FuelPart>().FuelMass = 20; hearth.GetPart<ThermalPart>().Temperature = 200;
            Assert.False(hearth.GetPart<ThermalPart>().IsAflame, "200 is below the 300 flame temperature, yet the hearth is burning by its own rule"); var flask = Flask(12, "bog-mire");
            Assert.True(Act(still, Row(still, "oil"))); Assert.AreEqual(15f, Fuel(hearth), 0.001f); Assert.AreEqual("oil", Vessel(flask).LiquidId);
            hearth.GetPart<ThermalPart>().Temperature = 100; var flask2 = Flask(12, "bog-mire"); Assert.True(Rows(still).All(a => a.IsDisabled), "below cooking heat it is out"); Assert.NotNull(flask2);
        }

        [Test] public void SeveralMireFlasksGetDistinguishableRows()
        {
            var still = Still(); Fire(); Flask(12, "bog-mire"); Flask(12, "bog-mire"); var water = Rows(still).Where(a => a.Command.Contains("|water|")).Select(a => a.Display).ToArray();
            Assert.AreEqual(2, water.Length); Assert.AreEqual(2, water.Distinct().Count(), string.Join(" / ", water));
        }

        // ── settle all ──

        InventoryAction[] AllRows(Entity still) => Actions(still).Where(a => a.Name == "SettleAll").ToArray();

        [Test] public void SettleAllIsOfferedForTwoOrMoreFlasksAndSaysWhatItWillYield()
        {
            var still = Still(); Fire(); Flask(12, "bog-mire"); Assert.IsEmpty(AllRows(still), "one flask: the ordinary rows are enough"); Flask(8, "bog-mire");
            var rows = AllRows(still); Assert.AreEqual(2, rows.Length); var water = rows.Single(r => r.Command == "SettleAll|water"); StringAssert.Contains("2 flasks", water.Display); StringAssert.Contains("5 water", water.Display, "3 from the 12 and 2 from the 8");
            Assert.AreEqual(ActionTime.OneAction, water.Time);
        }

        [Test] public void SettleAllSettlesEveryFlaskAndEachBatchPaysItsOwnFuel()
        {
            var still = Still(); var fire = Fire(9, 25f); var a = Flask(12, "bog-mire"); var b = Flask(8, "bog-mire");
            Assert.True(Act(still, "SettleAll|oil"), string.Join("|", MessageLog.GetRecent(3))); Assert.AreEqual("oil", Vessel(a).LiquidId); Assert.AreEqual(3, Vessel(a).Volume); Assert.AreEqual("oil", Vessel(b).LiquidId); Assert.AreEqual(2, Vessel(b).Volume);
            Assert.AreEqual(15f, Fuel(fire), 0.001f, "two batches, five fuel each"); Assert.AreEqual(2, DiagQuery.Apply(new DiagQuery.Filter { Category = "liquid", Kind = "Settled", Limit = 9 }).Records.Count);
        }

        [Test] public void SettleAllDoesAsManyFlasksAsTheFireCanSpareAndSaysSoBeforeHand()
        {
            var still = Still(); var fire = Fire(9, 7f); var a = Flask(12, "bog-mire"); var b = Flask(8, "bog-mire"); var row = AllRows(still).Single(r => r.Command == "SettleAll|water"); StringAssert.Contains("1 of 2 flasks", row.Display);
            Assert.True(Act(still, "SettleAll|water")); Assert.AreEqual(2f, Fuel(fire), 0.001f); Assert.AreEqual("water", Vessel(a).LiquidId); Assert.AreEqual("bog-mire", Vessel(b).LiquidId, "the second flask waits for more fuel"); Assert.AreEqual(8, Vessel(b).Volume);
        }

        [Test] public void SettleAllWithoutAFireOrWithAForgedTargetIsRefusedAndChangesNothing_Counters()
        {
            var still = Still(); var a = Flask(12, "bog-mire"); Flask(8, "bog-mire"); Assert.IsEmpty(AllRows(still), "no fire, no settle-all row (the per-flask rows say why)"); Assert.False(Act(still, "SettleAll|water"));
            var fire = Fire(); Assert.False(Act(still, "SettleAll|gold")); Assert.False(Act(still, "SettleAll|")); Assert.AreEqual(12, Vessel(a).Volume); Assert.AreEqual(25f, Fuel(fire), 0.001f);
        }

        [Test] public void SettleAllGivesEverythingBackWhenAnOuterStepFails()
        {
            var still = Still(); var fire = Fire(); var a = Flask(12, "bog-mire"); var b = Flask(8, "bog-mire"); FailAfter(); Assert.False(Act(still, "SettleAll|water"));
            Assert.AreEqual("bog-mire", Vessel(a).LiquidId); Assert.AreEqual(12, Vessel(a).Volume); Assert.AreEqual(8, Vessel(b).Volume); Assert.AreEqual(25f, Fuel(fire), 0.001f);
        }

        [Test] public void FewerThanFourUnitsOffersOnlyARefusal()
        {
            var still = Still(); Fire(); Flask(3, "bog-mire"); var rows = Rows(still); Assert.True(rows.All(a => a.IsDisabled)); StringAssert.Contains("4", rows[0].DisabledReason);
        }

        [Test] public void OtherLiquidsAreNotSettled_BrineWaterOilAcid_Counter()
        {
            var still = Still(); Fire(); foreach (var liquid in new[] { "brine", "water", "oil", "acid" }) { var f = Flask(12, liquid); Assert.False(Rows(still).Any(), liquid); Pack.RemoveObject(f); }
        }

        [Test] public void ACarriedStillIsAKitAndOffersNothing_Counter()
        {
            var kit = Carry("CampStill"); Fire(); Flask(12, "bog-mire"); Assert.False(Actions(kit).Any(a => a.Name == "SettleLiquid"));
        }

        [Test] public void AStillWithoutACarriedMireFlaskOffersNoRows_Counter()
        {
            var still = Still(); Fire(); Assert.False(Rows(still).Any()); Flask(12, "water"); Assert.False(Rows(still).Any());
        }

        [Test] public void AnOuterFailureGivesTheMireAndTheFuelBack()
        {
            var still = Still(); var fire = Fire(); var flask = Flask(12, "bog-mire"); string row = Row(still, "water"); FailAfter();
            Assert.False(Act(still, row)); Assert.AreEqual("bog-mire", Vessel(flask).LiquidId); Assert.AreEqual(12, Vessel(flask).Volume); Assert.AreEqual(25f, Fuel(fire), 0.001f);
        }

        [Test] public void AStaleOrForgedCommandIsRefusedAndChangesNothing()
        {
            var still = Still(); var fire = Fire(); var flask = Flask(12, "bog-mire"); string id = flask.ID;
            foreach (var bad in new[] { "SettleLiquid|", "SettleLiquid|gold|" + id + "|12", "SettleLiquid|water|nope|12", "SettleLiquid|water|" + id + "|99", "SettleLiquid|water|" + id + "|x" })
                Assert.False(Act(still, bad), bad);
            Vessel(flask).Volume = 8; Assert.False(Act(still, "SettleLiquid|water|" + id + "|12"), "the flask changed since the row was built");
            Assert.AreEqual(25f, Fuel(fire), 0.001f); Assert.AreEqual(8, Vessel(flask).Volume);
        }

        [Test] public void ADeadActorSettlesNothing()
        {
            var still = Still(); var fire = Fire(); var flask = Flask(12, "bog-mire"); string row = Row(still, "water"); Actor.GetStat("Hitpoints").BaseValue = 0;
            Assert.False(Act(still, row)); Assert.AreEqual(12, Vessel(flask).Volume); Assert.AreEqual(25f, Fuel(fire), 0.001f);
        }

        [Test] public void TwoFlasksAreSettledOneAtATimeEachPayingItsOwnFuel()
        {
            var still = Still(); var fire = Fire(); var a = Flask(12, "bog-mire"); var b = Flask(8, "bog-mire");
            var rows = Rows(still).Where(r => !r.IsDisabled && r.Command.Contains("|water|")).ToArray(); Assert.AreEqual(2, rows.Length);
            Assert.True(Act(still, rows.First(r => r.Command.Contains("|" + a.ID + "|")).Command)); Assert.True(Act(still, Rows(still).First(r => !r.IsDisabled && r.Command.Contains("|water|") && r.Command.Contains("|" + b.ID + "|")).Command));
            Assert.AreEqual(15f, Fuel(fire), 0.001f); Assert.AreEqual(3, Vessel(a).Volume); Assert.AreEqual(2, Vessel(b).Volume);
        }

        // ── baking clay (Docs/MACHINE-INVENTED-USES.md 3j) ──

        InventoryAction[] Bakes(Entity still) => Actions(still).Where(a => a.Name == "BakeClay").ToArray();
        int Clay() => Pack.Objects.Where(o => o.BlueprintName == "FireClay").Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1);
        [SetUp] public void WireTheCampsHands() { AlchemyStillPart.Factory = Factory; }
        [TearDown] public void UnwireTheCampsHands() { AlchemyStillPart.Factory = null; }

        [Test] public void AFlaskWithEightOrMoreMireOffersABakeRowAndLessDoesNot_Counter()
        {
            var still = Still(); Fire(); Flask(12, "bog-mire"); var row = Bakes(still).Single(); Assert.False(row.IsDisabled); Assert.AreEqual("bake 12 mire to 1 fire clay", row.Display); Assert.AreEqual(ActionTime.OneAction, row.Time); StringAssert.Contains("fire clay", row.Hint);
            var small = Flask(7, "bog-mire"); Assert.AreEqual(1, Bakes(still).Length, "seven is not enough for a clay; its settle rows are unaffected"); Assert.True(Rows(still).Any(a => a.Display.Contains("7 mire")));
        }

        [Test] public void BakingTurnsEightMireAndTenFuelIntoOneFireClayAndKeepsTheRestAsMire()
        {
            var still = Still(); var fire = Fire(9, 25f); var flask = Flask(12, "bog-mire"); Assert.True(Act(still, Bakes(still).Single().Command), string.Join("|", MessageLog.GetRecent(3)));
            Assert.AreEqual(1, Clay()); Assert.AreEqual(4, Vessel(flask).Volume); Assert.AreEqual("bog-mire", Vessel(flask).LiquidId); Assert.AreEqual(15f, Fuel(fire), 0.001f, "ten fuel from the fire that was lit");
            Assert.LessOrEqual(1 * LiquidSettlingPart.ClayRatio, 12, "the clay never exceeds the feed"); StringAssert.Contains("4 mire stays", string.Join("|", MessageLog.GetRecent(3)));
        }

        [Test] public void EightExactlyEmptiesTheFlask()
        {
            var still = Still(); Fire(); var flask = Flask(8, "bog-mire"); Assert.True(Act(still, Bakes(still).Single().Command)); Assert.AreEqual(0, Vessel(flask).Volume); Assert.AreEqual("", Vessel(flask).LiquidId); Assert.AreEqual(1, Clay());
        }

        [Test] public void WithoutALitFireOrEnoughFuelTheRowIsDisabledAndSaysWhy_AndNothingIsBaked()
        {
            var still = Still(); var flask = Flask(12, "bog-mire"); var none = Bakes(still).Single(); Assert.True(none.IsDisabled); StringAssert.Contains("lit fire", none.DisabledReason); Assert.False(Act(still, "BakeClay|" + flask.ID + "|12")); Assert.AreEqual(0, Clay()); Assert.AreEqual(12, Vessel(flask).Volume);
            Fire(9, 9f); var low = Bakes(still).Single(); Assert.True(low.IsDisabled); Assert.AreEqual("fuel_short", low.DisabledCode); StringAssert.Contains("burns 10", low.DisabledReason); Assert.AreEqual(0, Clay());
        }

        [Test] public void AnInterruptedBakeGivesBackTheMireTheFuelAndTheClay()
        {
            var still = Still(); var fire = Fire(9, 25f); var flask = Flask(12, "bog-mire"); string command = Bakes(still).Single().Command; FailAfter();
            Assert.False(Act(still, command)); Assert.AreEqual(0, Clay()); Assert.AreEqual(12, Vessel(flask).Volume); Assert.AreEqual(25f, Fuel(fire), 0.001f);
        }

        [Test] public void ForgedStaleAndOverloadedBakesAreRefusedAndChangeNothing()
        {
            var still = Still(); var fire = Fire(9, 25f); var flask = Flask(12, "bog-mire");
            foreach (var bad in new[] { "BakeClay|", "BakeClay|nobody|12", "BakeClay|" + flask.ID + "|11", "BakeClay|" + flask.ID + "|x", "BakeClay|" + flask.ID }) Assert.False(Act(still, bad), bad);
            Pack.MaxWeight = Pack.GetCarriedWeight(); Assert.False(Act(still, "BakeClay|" + flask.ID + "|12"), "no room for the clay"); Assert.AreEqual(0, Clay()); Assert.AreEqual(12, Vessel(flask).Volume); Assert.AreEqual(25f, Fuel(fire), 0.001f);
        }

        [Test] public void WithoutTheCampsHandsThereIsNoBakeRow_Counter()
        {
            AlchemyStillPart.Factory = null; var still = Still(); Fire(); Flask(12, "bog-mire"); Assert.IsEmpty(Bakes(still)); Assert.NotNull(Rows(still).FirstOrDefault());
        }

        [Test] public void BakedClayIsRealFireClayTheRepairsAndConcernsCount()
        {
            var still = Still(); Fire(9, 25f); Flask(12, "bog-mire"); Flask(12, "bog-mire"); foreach (var row in Bakes(still)) Assert.True(Act(still, row.Command), row.Display + ": " + string.Join("|", MessageLog.GetRecent(4)));
            Assert.AreEqual(2, CavesOfOoo.Concerns.ConcernItems.CountCarried(Actor, "FireClay"), "two flasks, two clay, the two the pump patch asks for");
        }

        [Test] public void TheReaderOffersTheBakeToo()
        {
            var still = Still(); Fire(); Flask(12, "bog-mire"); var lines = new System.Collections.Generic.List<string>(); StateReader.Collect(still, false, Zone, Zone.GetEntityCell(still), Actor, lines); StringAssert.Contains("bake 8 of it into fire clay", string.Join(" ", lines));
        }
    }
}
