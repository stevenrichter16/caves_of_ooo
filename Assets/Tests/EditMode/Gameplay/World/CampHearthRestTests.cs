using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Resting beside the camp hearth (Docs/MACHINE-INVENTED-USES.md, "a camp you can sleep at"). The hearth is a finite fire; until now nothing simulated a fire through the clock jump of a
    /// rest, so it was kept from resting at all. This pins the honest accounting: while you sleep the fire burns what it would have burned in those ticks (ticks x its own burn rate),
    /// refused up front when it could not last, with the same heal, bleeding cure and hostile rule every other fire gives. It is a separate Part on the hearth, so no other campfire's rest
    /// rule changes.
    /// </summary>
    public sealed class CampHearthRestTests : FiftyWorldFixture
    {
        [SetUp] public void SetupRest() { Diag.ResetAll(); MessageLog.Clear(); Actor.GetStat("Hitpoints").BaseValue = 3; }

        Entity Hearth(float fuel = 30f, bool lit = true, bool standing = true)
        {
            var h = Place("CampHearth", 11, 10); h.GetPart<PhysicsPart>().Takeable = !standing; h.GetPart<FuelPart>().FuelMass = fuel; h.GetPart<ThermalPart>().Temperature = lit ? 450 : 25; h.GetPart<LightSourcePart>().Enabled = lit && fuel > 0; return h;
        }
        float Fuel(Entity h) => h.GetPart<FuelPart>().FuelMass;
        int Hp => Actor.GetStatValue("Hitpoints");
        InventoryAction Row(Entity h, string name) => Actions(h).SingleOrDefault(a => a.Name == name);
        void Enemy(int x = 18)
        {
            var enemy = new Entity { ID = "hearth-rest-hostile" }; enemy.AddPart(new PhysicsPart()); enemy.AddPart(new BrainPart()); enemy.GetPart<BrainPart>().SetPersonallyHostile(Actor, false); Assert.True(Zone.AddEntity(enemy, x, 10));
        }

        [Test] public void TheShippedHearthCarriesTheRestPartAndStillOptsOutOfTheOrdinaryCampfireRest()
        {
            var h = Factory.CreateEntity("CampHearth"); Assert.NotNull(h.GetPart<HearthRestPart>()); Assert.False(h.GetPart<CampfirePart>().AllowRest, "the ordinary free campfire rest stays off; this is its own, paid rest");
            var g = new System.Collections.Generic.List<string>(); UseGuides.Collect(h, null, null, null, g);
            StringAssert.Contains("rest", string.Join(" ", g).ToLowerInvariant(), "resting beside the hearth is explained in its how-to block, not in the flavor text");
        }

        [Test] public void ALitHearthRestsFullyAndBurnsTheFuelItWouldBurnInTheTimeYouSlept()
        {
            var h = Hearth(30); Assert.True(Act(h, "RestAtHearth"), string.Join("|", MessageLog.GetRecent(3)));
            Assert.AreEqual(Actor.GetStat("Hitpoints").Max, Hp); Assert.AreEqual(60, Clock.TickCount); Assert.AreEqual(15f, Fuel(h), 0.001f, "60 ticks at 0.25 a tick"); Assert.That(h.GetPart<ThermalPart>().Temperature, Is.InRange(150f, 153f), "the fire cooled by the night it burned"); Assert.True(h.GetPart<LightSourcePart>().Enabled, "still burning with fuel left");
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "furniture", Kind = "HearthRested", Limit = 5 }).Records.Count);
        }

        [Test] public void TheRowStatesTheFuelTheRestWillBurn()
        {
            var h = Hearth(30); var row = Row(h, "HearthRest"); Assert.NotNull(row); Assert.False(row.IsDisabled); StringAssert.Contains("15 fuel", row.Display); Assert.AreEqual("RestAtHearth", row.Command);
        }

        [Test] public void AColdHearthRefusesAndTheRowSaysToLightIt_NothingChanges()
        {
            var h = Hearth(30, lit: false); var row = Row(h, "HearthRest"); Assert.True(row.IsDisabled); StringAssert.Contains("out", row.DisabledReason.ToLowerInvariant());
            Assert.False(Act(h, "RestAtHearth")); Assert.AreEqual(3, Hp); Assert.AreEqual(0, Clock.TickCount); Assert.AreEqual(30f, Fuel(h), 0.001f);
        }

        [Test] public void ShortFuelRefusesBeforeAnythingChangesAndTheReasonNamesTheNumbers()
        {
            var h = Hearth(10); var row = Row(h, "HearthRest"); Assert.True(row.IsDisabled); StringAssert.Contains("15", row.DisabledReason); StringAssert.Contains("10", row.DisabledReason);
            Assert.False(Act(h, "RestAtHearth")); Assert.AreEqual(3, Hp); Assert.AreEqual(0, Clock.TickCount); Assert.AreEqual(10f, Fuel(h), 0.001f);
        }

        [Test] public void ExactlyEnoughFuelRestsAndTheFireBurnsOutInTheNight()
        {
            var h = Hearth(15); Assert.True(Act(h, "RestAtHearth")); Assert.AreEqual(0f, Fuel(h), 0.001f); Assert.AreEqual(60, Clock.TickCount); Assert.AreEqual(h.GetPart<ThermalPart>().AmbientTemperature, h.GetPart<ThermalPart>().Temperature, 0.001f, "burnt out is cold"); Assert.False(h.GetPart<LightSourcePart>().Enabled, "and dark without waiting for the next turn");
        }

        [Test] public void RestUntilTheNextBandIsAvailableAtEveryClockPositionForALitFedFire_TheReportedDeadZone()
        {
            // Reported from play: the row was disabled for most of each 300-tick band (fuel short for a night longer than the hearth can hold, then "relight first" at full heat) and
            // only came on in the last 60 ticks, so one more action seemed to switch it on. A lit hearth with a night's fuel can always sleep to morning.
            var h = Hearth(30); for (int tick = 0; tick < 300; tick += 7)
            {
                h.GetPart<FuelPart>().FuelMass = 30; h.GetPart<ThermalPart>().Temperature = 450; var band = Row(h, "HearthRestBand");
                Assert.False(band.IsDisabled, "tick " + tick + ": " + band.DisabledReason); Clock.AdvanceClock(7);
            }
        }

        [Test] public void ALongNightBurnsTheFireOutAndTheRowSaysSo()
        {
            var h = Hearth(30); var band = Row(h, "HearthRestBand"); StringAssert.Contains("15 fuel", band.Display); StringAssert.Contains("goes out", band.Display);
            Assert.True(Act(h, "RestUntilNextBandAtHearth"), string.Join("|", MessageLog.GetRecent(3))); Assert.AreEqual(300, Clock.TickCount); Assert.AreEqual(Actor.GetStat("Hitpoints").Max, Hp);
            Assert.AreEqual(15f, Fuel(h), 0.001f, "it burned for its 60 ticks of life, not for the 300 of the night"); Assert.AreEqual(h.GetPart<ThermalPart>().AmbientTemperature, h.GetPart<ThermalPart>().Temperature, 0.001f, "and is cold in the morning");
            Assert.False(h.GetPart<LightSourcePart>().Enabled);
        }

        [Test] public void ALateBandRestKeepsTheFireBurningAndChargesOnlyWhatItSlept()
        {
            var h = Hearth(30); Clock.AdvanceClock(270); var band = Row(h, "HearthRestBand"); StringAssert.Contains("8 fuel", band.Display); StringAssert.DoesNotContain("goes out", band.Display);
            Assert.True(Act(h, "RestUntilNextBandAtHearth")); Assert.AreEqual(300, Clock.TickCount); Assert.AreEqual(22f, Fuel(h), 0.001f, "30 ticks at a quarter a tick, rounded up"); Assert.True(h.GetPart<LightSourcePart>().Enabled);
            Assert.Greater(h.GetPart<ThermalPart>().Temperature, 200f, "still burning when morning comes");
        }

        [Test] public void ABandRestNeedsTheSameNightAsANormalOne_FuelAndHeat()
        {
            var low = Hearth(30); low.GetPart<ThermalPart>().Temperature = 300; var band = Row(low, "HearthRestBand"); Assert.True(band.IsDisabled); Assert.AreEqual("fire_low", band.DisabledCode); StringAssert.Contains("light it again", band.DisabledReason);
            Assert.False(Act(low, "RestUntilNextBandAtHearth")); Assert.AreEqual(0, Clock.TickCount); low.GetPart<ThermalPart>().Temperature = 450; Assert.False(Row(low, "HearthRestBand").IsDisabled, "relighting is what fixes it");
            Zone.RemoveEntity(low); var thin = Hearth(10); band = Row(thin, "HearthRestBand"); Assert.True(band.IsDisabled); Assert.AreEqual("fuel_short", band.DisabledCode);
            StringAssert.Contains("15", band.DisabledReason); StringAssert.Contains("10", band.DisabledReason);
        }

        [Test] public void AHostileWithinEightCellsRefusesAndTheFuelIsKept()
        {
            var h = Hearth(30); Enemy(); Assert.False(Act(h, "RestAtHearth")); Assert.AreEqual(3, Hp); Assert.AreEqual(0, Clock.TickCount); Assert.AreEqual(30f, Fuel(h), 0.001f);
        }

        [Test] public void ADescribedRestSaysWhenAHostileIsNear_AndOtherwiseOffersIt_Counter()
        {
            var h = Hearth(30); var rest = h.GetPart<HearthRestPart>(); var rows = new InventoryActionList(); rest.AddUseRows(Actor, Zone, rows);
            Assert.That(rows.Actions.Any(r => r.Name == "HearthRest" && !r.IsDisabled), "with no hostile near the rest is offered");
            Enemy(); rows = new InventoryActionList(); rest.AddUseRows(Actor, Zone, rows);
            var rest1 = rows.Actions.Where(r => r.Name == "HearthRest").ToList(); Assert.That(rest1, Is.Not.Empty); Assert.That(rest1.All(r => r.IsDisabled && r.DisabledCode == "hostile-near"), "a hostile within " + RestSystem.HostileScanRadius + " cells refuses every rest");
            StringAssert.Contains(RestSystem.HostileScanRadius + " cells", rest1[0].DisabledReason);
            Assert.False(Act(h, "RestAtHearth"), "and the real rest agrees");
        }

        [Test] public void AnOuterFailureGivesTheFuelBackAndNothingElseHappened()
        {
            var h = Hearth(30); FailAfter(); Assert.False(Act(h, "RestAtHearth")); Assert.AreEqual(30f, Fuel(h), 0.001f); Assert.AreEqual(3, Hp); Assert.AreEqual(0, Clock.TickCount);
        }

        [Test] public void ADeadActorCannotRestAndBurnsNothing()
        {
            var h = Hearth(30); Actor.GetStat("Hitpoints").BaseValue = 0; Assert.False(Act(h, "RestAtHearth")); Assert.AreEqual(30f, Fuel(h), 0.001f); Assert.AreEqual(0, Clock.TickCount);
        }

        [Test] public void ACarriedHearthOffersNoRest_Counter()
        {
            var h = Carry("CampHearth"); h.GetPart<FuelPart>().FuelMass = 30; h.GetPart<ThermalPart>().Temperature = 450;
            Assert.IsNull(Row(h, "HearthRest")); Assert.IsNull(Row(h, "HearthRestBand")); Assert.False(Act(h, "RestAtHearth")); Assert.AreEqual(30f, Fuel(h), 0.001f); Assert.AreEqual(0, Clock.TickCount);
        }

        [Test] public void OtherFiresKeepTheirOwnRestRules_Counter()
        {
            var coals = Place("SpreadCookingCoals", 11, 10); Assert.IsNull(Row(coals, "HearthRest"), "the shipped cooking coals never gain a rest");
            var camp = Place("Campfire", 9, 10); float fuel = camp.GetPart<FuelPart>()?.FuelMass ?? 0; Assert.True(Act(camp, "RestAtCampfire"), "an ordinary waysite fire still rests for free"); Assert.AreEqual(60, Clock.TickCount);
            Assert.AreEqual(fuel, camp.GetPart<FuelPart>()?.FuelMass ?? 0, 0.001f, "and burns nothing for it");
        }

        [Test] public void ARestAtAFireThatIsNotAFiniteStandingHearthIsRefused_Forged()
        {
            var h = Hearth(30); h.GetPart<CampfirePart>().FiniteCooking = false; Assert.False(Act(h, "RestAtHearth")); Assert.AreEqual(30f, Fuel(h), 0.001f); Assert.AreEqual(0, Clock.TickCount);
        }

        [Test] public void OneLightingIsAboutOneNight_TheSecondNeedsAFlameAgain()
        {
            var h = Hearth(30); Assert.True(Act(h, "RestAtHearth")); Assert.AreEqual(15f, Fuel(h), 0.001f);
            var row = Row(h, "HearthRest"); Assert.True(row.IsDisabled, "the fire has cooled to the edge of cooking heat; another 60 ticks would let it die"); StringAssert.Contains("light it again", row.DisabledReason);
            Actor.GetStat("Hitpoints").BaseValue = 3; Assert.False(Act(h, "RestAtHearth")); Assert.AreEqual(3, Hp); Assert.AreEqual(60, Clock.TickCount); Assert.AreEqual(15f, Fuel(h), 0.001f);
            h.GetPart<ThermalPart>().Temperature = 450; Assert.True(Act(h, "RestAtHearth"), "relit from a flame (or the flint striker) it sleeps a second night"); Assert.AreEqual(0f, Fuel(h), 0.001f); Assert.AreEqual(120, Clock.TickCount);
        }

        [Test] public void AFireTooLowToLastTheNightRefusesAndSaysToRelight()
        {
            var h = Hearth(30); h.GetPart<ThermalPart>().Temperature = 300; var row = Row(h, "HearthRest"); Assert.True(row.IsDisabled); StringAssert.Contains("light it again", row.DisabledReason);
            Assert.False(Act(h, "RestAtHearth")); Assert.AreEqual(30f, Fuel(h), 0.001f); Assert.AreEqual(300f, h.GetPart<ThermalPart>().Temperature, 0.001f); Assert.AreEqual(0, Clock.TickCount);
        }

        [Test] public void AnEmptyHearthSaysToFeedItNotThatItIsOut()
        {
            var h = Hearth(0, lit: true); var row = Row(h, "HearthRest"); Assert.True(row.IsDisabled); StringAssert.Contains("Feed", row.DisabledReason); Assert.AreEqual("no_fuel", row.DisabledCode);
        }

        [Test] public void TheWorldMenuTheUiBuildsOffersTheHearthRestRows()
        {
            var h = Hearth(30); var rows = WorldInteractionSystem.GatherActions(h, Actor);
            Assert.True(rows.Any(a => a.Name == "HearthRest" && !a.IsDisabled && a.Time == ActionTime.Free), "the world menu runs it as a free, declared-time row"); Assert.True(rows.Any(a => a.Name == "HearthRestBand"));
            Assert.False(rows.Any(a => a.Command == "RestAtCampfire"), "the ordinary free rest is still not offered");
        }

        [Test] public void TheRestPartRoundTripsAsAnEmptyPartAndSurvivesTheHearthSave()
        {
            var h = Hearth(21); var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(h); Assert.NotNull(copy.GetPart<HearthRestPart>()); Assert.AreEqual(21f, copy.GetPart<FuelPart>().FuelMass, 0.001f);
        }
    }
}
