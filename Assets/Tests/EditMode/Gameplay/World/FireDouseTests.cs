using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Water on your own lit hearth (Docs/MACHINE-INVENTED-USES.md 3m): the plainest common-sense interaction, which used to write the water to the ground and leave the fire burning. A cold
    /// dose through the same ApplyHeat event the frost abilities use, scaled by the amount poured; the fuel is kept; fuel-like liquids never douse; and the rule is only for fixtures you set down.
    /// </summary>
    public sealed class FireDouseTests : FiftyWorldFixture
    {
        const int AX = 20, AY = 12;

        [SetUp] public void SetupDouse() { MessageLog.Clear(); Diag.ResetAll(); Pack.MaxWeight = 400; PackableFixtureRegistry.ResetForTests(); }
        [TearDown] public void TearDownDouse() { PackableFixtureRegistry.ResetForTests(); }

        /// <summary>A hearth of yours, one cell east of the actor, lit hot with some fuel in it.</summary>
        Entity LitHearthEast(float temperature = 450f, float fuel = 10f, bool yours = true)
        {
            var hearth = Factory.CreateEntity("CampHearth"); var at = Zone.GetEntityCell(Actor);
            Assert.True(Zone.AddEntity(hearth, at.X + 1, at.Y));
            // "Yours" is what a real set-down leaves behind: a Player-origin marker and an installed, portable fixture state.
            if (yours) { hearth.AddPart(new InstallationMarkerPart { InstallationId = "player:" + hearth.ID, Origin = HeldFixtureActions.PlayerOrigin, RetainZone = true }); var inst = hearth.GetPart<InstallablePart>() ?? new InstallablePart(); inst.Mode = InstallablePart.Portable; inst.State = InstallablePart.Installed; if (hearth.GetPart<InstallablePart>() == null) hearth.AddPart(inst); }
            hearth.GetPart<FuelPart>().FuelMass = fuel; hearth.GetPart<ThermalPart>().Temperature = temperature; hearth.GetPart<LightSourcePart>().Enabled = temperature >= CookingService.MinimumFiniteCookingTemperature && fuel > 0;
            return hearth;
        }

        Entity Flask(string liquid, int volume) { var f = Factory.CreateEntity("LiquidFlask"); f.GetPart<LiquidVesselPart>().LiquidId = liquid; f.GetPart<LiquidVesselPart>().Volume = volume; Assert.True(Pack.AddObject(f)); return f; }
        InventoryAction Row(Entity flask, string name) => Actions(flask).Single(a => a.Name == name && a.Display.EndsWith(" east") || a.Name == name && a.Display.Contains(" east ("));
        float Temp(Entity h) => h.GetPart<ThermalPart>().Temperature;
        bool LightOn(Entity h) => h.GetPart<LightSourcePart>().Enabled;

        [Test] public void AFullFlaskOfWaterPutsOutYourHearthAndLeavesItsFuel()
        {
            var hearth = LitHearthEast(); var flask = Flask("water", 12); Assert.True(LightOn(hearth), "fixture: lit, so it glows");
            Assert.True(Act(flask, Row(flask, "PourLiquid").Command), string.Join("|", MessageLog.GetRecent(3)));
            Assert.Less(Temp(hearth), CookingService.MinimumFiniteCookingTemperature, "below cooking heat is out"); Assert.False(LightOn(hearth), "the glow follows at once");
            Assert.AreEqual(hearth.GetPart<ThermalPart>().AmbientTemperature, Temp(hearth), 0.01f, "twelve units would be 480 degrees, but water cools a fire to the air and no further");
            Assert.False(hearth.HasEffect<FrozenEffect>(), "counter: a drink of water never freezes a hearth");
            Assert.AreEqual(10f, hearth.GetPart<FuelPart>().FuelMass, 0.001f, "the timber was not burned by being doused");
            StringAssert.Contains("goes out", string.Join(" ", MessageLog.GetRecent(4))); Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "furniture", Kind = "HearthDoused", Limit = 5 }).Records.Count);
        }

        [Test] public void OneSplashCoolsItButDoesNotPutItOut()
        {
            var hearth = LitHearthEast(); var flask = Flask("water", 12); float before = Temp(hearth);
            Assert.True(Act(flask, Row(flask, "PourOneLiquid").Command));
            Assert.AreEqual(before - FireDouse.DegreesPerUnit, Temp(hearth), 0.5f, "one unit, one step"); Assert.True(LightOn(hearth)); StringAssert.Contains("burns lower", string.Join(" ", MessageLog.GetRecent(4)));
            Assert.AreEqual(11, flask.GetPart<LiquidVesselPart>().Volume, "the water was spent");
        }

        [Test] public void DousingScalesWithWhatYouPour_AndAFiercerFireTakesMore()
        {
            var weak = LitHearthEast(200f); var flask = Flask("water", 12); Assert.True(Act(flask, Row(flask, "PourLiquid").Command)); Assert.Less(Temp(weak), 150f);
            Zone.RemoveEntity(weak);
            var fierce = LitHearthEast(800f); var small = Flask("water", 4); Assert.True(Act(small, Row(small, "PourLiquid").Command));
            Assert.Greater(Temp(fierce), 150f, "four units cannot put out a fire at 800"); Assert.AreEqual(800f - 4 * FireDouse.DegreesPerUnit, Temp(fierce), 1f);
        }

        [Test] public void AnythingThatBurnsNeverDouses_Counter()
        {
            foreach (var oil in new[] { "oil", "pitch" })
            {
                var hearth = LitHearthEast(); var flask = Flask(oil, 12); float before = Temp(hearth);
                // Either the world refuses to pour fuel onto a lit fire at all (no east row) or the pour happens and the fire is not cooled; never a douse.
                var east = Actions(flask).FirstOrDefault(a => a.Name == "PourLiquid" && a.Display.Contains(" east ("));
                if (east != null) Act(flask, east.Command);
                Assert.GreaterOrEqual(Temp(hearth), before, oil + " does not cool a fire"); Assert.True(LightOn(hearth)); Zone.RemoveEntity(hearth); foreach (var e in Pack.Objects.ToArray()) Pack.RemoveObject(e);
            }
            Assert.False(FireDouse.Douses("oil")); Assert.False(FireDouse.Douses("pitch")); Assert.True(FireDouse.Douses("water")); Assert.True(FireDouse.Douses("bog-mire")); Assert.False(FireDouse.Douses("no-such-liquid"));
        }

        [Test] public void OnlyYourOwnHearthIsDoused_ATownsHearthIsNotYoursToPutOut_Counter()
        {
            var theirs = LitHearthEast(yours: false); var flask = Flask("water", 12); float before = Temp(theirs);
            Assert.True(Act(flask, Row(flask, "PourLiquid").Command)); Assert.AreEqual(before, Temp(theirs), 0.001f); Assert.True(LightOn(theirs));
            Assert.AreEqual(0, DiagQuery.Apply(new DiagQuery.Filter { Category = "furniture", Kind = "HearthDoused", Limit = 5 }).Records.Count);
        }

        [Test] public void AnUnlitHearthAndAnEmptyCellSaySomethingOfNothing_Counters()
        {
            var cold = LitHearthEast(25f, 10f); var flask = Flask("water", 12); MessageLog.Clear();
            Assert.True(Act(flask, Row(flask, "PourLiquid").Command)); Assert.AreEqual(25f, Temp(cold), 0.001f); StringAssert.DoesNotContain("hisses", string.Join(" ", MessageLog.GetRecent(5)));
            Zone.RemoveEntity(cold); var flask2 = Flask("water", 6); MessageLog.Clear(); var west = Actions(flask2).First(a => a.Name == "PourLiquid" && a.Display.Contains(" west ("));
            Assert.True(Act(flask2, west.Command)); StringAssert.DoesNotContain("hisses", string.Join(" ", MessageLog.GetRecent(5)));
        }

        [Test] public void ThePourRowSaysWhatItWouldDoToYourFire_AndNothingElsesRowsGainAHint()
        {
            var hearth = LitHearthEast(); var flask = Flask("water", 12);
            StringAssert.Contains("Puts out your hearth", Row(flask, "PourLiquid").Hint); StringAssert.Contains("Cools your hearth", Row(flask, "PourOneLiquid").Hint);
            var west = Actions(flask).First(a => a.Name == "PourLiquid" && a.Display.Contains(" west (")); Assert.IsEmpty(west.Hint ?? "", "a cell with no fire has no hint");
            var oil = Flask("oil", 12); Assert.IsEmpty(Actions(oil).FirstOrDefault(a => a.Name == "PourLiquid" && a.Display.Contains(" east ("))?.Hint ?? "", "oil would not douse, so its row (if the world offers one) is silent");
        }

        [Test] public void OnlyWaterAndLiquidsThatActAsWaterDouse_NotEveryLiquidThatDoesNotBurn()
        {
            foreach (var wet in new[] { "water", "bog-mire", "brine" }) Assert.True(FireDouse.Douses(wet), wet);
            foreach (var other in new[] { "acid", "lava", "gel", "carapace-ichor", "oil", "pitch", "honey" }) Assert.False(FireDouse.Douses(other), other + " is not a way to put a fire out");
        }

        [Test] public void ASpentHearthWithCoalsStillHotIsAlreadyDarkAndIsNotPutOut_Counter()
        {
            var coals = LitHearthEast(300f, 0f); Assert.False(LightOn(coals), "fixture: no fuel, so no flame"); var flask = Flask("water", 12); MessageLog.Clear(); float before = Temp(coals);
            Assert.IsEmpty(Row(flask, "PourOneLiquid").Hint ?? "", "its row makes no promise about a fire that is already dark");
            Assert.True(Act(flask, Row(flask, "PourLiquid").Command)); Assert.AreEqual(before, Temp(coals), 0.001f); StringAssert.DoesNotContain("goes out", string.Join(" ", MessageLog.GetRecent(5)));
        }

        [Test] public void ADousedHearthCanBeRelitFromAFlameAndStillHasItsFuel()
        {
            var hearth = LitHearthEast(); var flask = Flask("water", 12); Assert.True(Act(flask, Row(flask, "PourLiquid").Command)); Assert.False(LightOn(hearth));
            // the ordinary relight path: the fire is warmed past cooking heat again (a flame in reach does this); here we apply the same heat the lighting verbs apply
            var heat = GameEvent.New("ApplyHeat"); heat.SetParameter("Joules", (object)(400f * hearth.GetPart<ThermalPart>().HeatCapacity)); heat.SetParameter("Radiant", (object)false); hearth.FireEvent(heat); heat.Release();
            var end = GameEvent.New("EndTurn"); hearth.FireEvent(end); Assert.True(LightOn(hearth), "lit again, as the hearth's own turn sees it"); Assert.AreEqual(10f - 0.25f, hearth.GetPart<FuelPart>().FuelMass, 0.001f, "and it burns the fuel it still has, one turn's worth");
        }
    }
}
