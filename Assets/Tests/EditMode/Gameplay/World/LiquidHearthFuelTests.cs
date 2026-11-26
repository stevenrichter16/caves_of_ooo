using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class LiquidHearthFuelTests : FiftySecondPreparationFixture
    {
        const string Prefix = "FeedHearthOil|";

        Entity Hearth(float fuel = 4, float heat = 25, string blueprint = "CampHearth")
        {
            var hearth = Place(blueprint);
            hearth.GetPart<PhysicsPart>().Takeable = false;
            var install = hearth.GetPart<InstallablePart>();
            if (install == null) hearth.AddPart(install = new InstallablePart());
            install.State = InstallablePart.Installed;
            hearth.GetPart<CampfirePart>().FiniteCooking = true;
            var store = hearth.GetPart<FuelPart>();
            store.FuelMass = fuel;
            store.MaxFuel = 30;
            store.BurnRate = .25f;
            hearth.GetPart<ThermalPart>().Temperature = heat;
            return hearth;
        }

        static FuelPart Fuel(Entity hearth) => hearth.GetPart<FuelPart>();
        static LiquidVesselPart Vessel(Entity flask) => flask.GetPart<LiquidVesselPart>();
        InventoryAction[] Rows(Entity hearth) => Actions(hearth).Where(row => row.Command.StartsWith(Prefix, StringComparison.Ordinal)).ToArray();

        [TestCase(4f, 1, 5f, 0)]
        [TestCase(4f, 7, 7f, 4)]
        [TestCase(28.25f, 7, 29.25f, 6)]
        public void FiniteSeparateUnitsCreditExactFuelAndKeepTheFlask(float before, int oil, float after, int remainder)
        {
            var hearth = Hearth(before);
            var flask = Flask(oil, "oil");
            var timber = Carry("SalvagedTimber");
            var row = CellMenu(11, 10).Single(action => action.Command.StartsWith(Prefix, StringComparison.Ordinal));
            StringAssert.Contains($"{oil - remainder} oil", row.Display);
            Assert.AreEqual(ActionTime.OneAction, row.Time);
            Assert.AreEqual(0, Clock.TickCount);
            Assert.AreEqual(oil, Units(flask));
            Assert.True(ChooseRow(row, 11, 10));
            Assert.AreEqual(after, Fuel(hearth).FuelMass);
            Assert.AreEqual(25, hearth.GetPart<ThermalPart>().Temperature);
            Assert.False(hearth.GetPart<LightSourcePart>().Enabled);
            Assert.AreEqual(remainder, Units(flask));
            Assert.AreEqual(remainder > 0 ? "oil" : "", Vessel(flask).LiquidId);
            Assert.Contains(flask, Pack.Objects);
            Assert.Contains(timber, Pack.Objects);
        }

        [Test] public void TheTwoCarriedFlasksOfferDistinctRowsAndNeverSubstitute()
        {
            var hearth = Hearth(4, 450);
            var first = Flask(2, "oil");
            var second = Flask(3, "oil");
            var rows = Rows(hearth);
            Assert.AreEqual(2, rows.Length);
            string chosen = rows.Single(row => row.Display.Contains("3 oil")).Command;
            Assert.True(Act(hearth, chosen));
            Assert.AreEqual(2, Units(first));
            Assert.AreEqual(0, Units(second));
            Assert.AreEqual(7, Fuel(hearth).FuelMass);
            Assert.AreEqual(450, hearth.GetPart<ThermalPart>().Temperature);
            Assert.False(Act(hearth, chosen));
        }

        [TestCase("water")] [TestCase("stack")] [TestCase("duplicate")]
        [TestCase("too-little-room")] [TestCase("hot-exhausted")] [TestCase("hidden")]
        [TestCase("free")] [TestCase("broken")] [TestCase("loose")]
        public void UnsupportedInputsOrHearthsOfferNoOilAction(string state)
        {
            var hearth = Hearth(state == "hot-exhausted" ? 0 : state == "too-little-room" ? 29.25f : 4,
                state == "hot-exhausted" ? 450 : 25);
            var flask = Flask(3, state == "water" ? "water" : "oil");
            if (state == "stack")
            {
                var stack = flask.GetPart<StackerPart>();
                if (stack == null) flask.AddPart(stack = new StackerPart());
                stack.StackCount = 2;
            }
            if (state == "duplicate") flask.AddPart(new LiquidVesselPart { Volume = 3, LiquidId = "oil" });
            if (state == "hidden") Zone.GetCell(11, 10).IsVisible = false;
            if (state == "free") hearth.GetPart<CampfirePart>().FiniteCooking = false;
            if (state == "broken") hearth.ApplyEffect(new BrokenEffect());
            if (state == "loose") hearth.GetPart<InstallablePart>().State = InstallablePart.Loose;
            Assert.IsEmpty(Rows(hearth));
            Assert.AreEqual(3, Units(flask));
        }

        [TestCase("hearth")]
        [TestCase("flask")]
        public void StaleQuoteDoesNotSpendAnotherLoad(string state)
        {
            var hearth = Hearth();
            var flask = Flask(3, "oil");
            var command = Rows(hearth).Single().Command;
            if (state == "hearth") Fuel(hearth).FuelMass++;
            else Vessel(flask).Volume++;
            Assert.False(Act(hearth, command));
            Assert.AreEqual(state == "hearth" ? 5 : 4, Fuel(hearth).FuelMass);
            Assert.AreEqual(state == "hearth" ? 3 : 4, Units(flask));
        }

        [Test] public void LateInterruptionRefundsOnlyTheFeedAndPreservesOtherChanges()
        {
            var hearth = Hearth();
            var flask = Flask(5, "oil");
            var command = Rows(hearth).Single().Command;
            var hp = Actor.GetStat("Hitpoints");
            var before = hp.Value;
            Actor.AddPart(new ReagentFuelCallbackPart { Callback = () =>
            {
                hp.Penalty += 2;
                Fuel(hearth).FuelMass += 2;
                Vessel(flask).Volume++;
            } });
            Assert.False(Act(hearth, command));
            Assert.AreEqual(before - 2, hp.Value);
            Assert.AreEqual(6, Fuel(hearth).FuelMass);
            Assert.AreEqual(6, Units(flask));
        }

        [Test] public void TimberRemainsTheOrdinaryTenFuelOption()
        {
            var hearth = Hearth();
            Carry("SalvagedTimber");
            Assert.True(Act(hearth, "FeedCookingFire"));
            Assert.AreEqual(14, Fuel(hearth).FuelMass);
        }

        [Test] public void DroppedFlaskWithReplacementLiquidKeepsItsIndependentContents()
        {
            var hearth = Hearth();
            var flask = Flask(3, "oil");
            var command = Rows(hearth).Single().Command;
            Actor.AddPart(new ReagentFuelCallbackPart { Callback = () =>
            {
                Pack.RemoveObject(flask);
                Zone.AddEntity(flask, 10, 10);
                Vessel(flask).LiquidId = "water";
                Vessel(flask).Volume = 2;
            } });
            Assert.False(Act(hearth, command));
            Assert.AreSame(Zone.GetCell(10, 10), Zone.GetEntityCell(flask));
            Assert.AreEqual("water", Vessel(flask).LiquidId);
            Assert.AreEqual(2, Units(flask));
            Assert.AreEqual(4, Fuel(hearth).FuelMass);
        }
    }
}
