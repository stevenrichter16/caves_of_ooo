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
    }
}
