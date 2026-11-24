using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class VesselOilTorchTests : FiftySecondPreparationFixture
    {
        const string Prefix = "RefuelVesselTorch|";
        Entity Torch(float fuel = 5)
        {
            var torch = Carry("Torch");
            torch.GetPart<LightSourcePart>().Enabled = false;
            torch.GetPart<FuelPart>().FuelMass = fuel;
            return torch;
        }
        string Row(Entity oil, Entity torch) => Actions(oil).Single(a =>
            a.Command.StartsWith(Prefix, StringComparison.Ordinal) && a.Command.Split('|')[2] == Id(torch)).Command;
        InventoryAction[] Rows(Entity oil) => Actions(oil).Where(a => a.Command.StartsWith(Prefix, StringComparison.Ordinal)).ToArray();

        [TestCase(5, 30)] [TestCase(45, 50)]
        public void OneOilFeedsOnlyTheChosenTorchWithVisibleCap(float before, float after)
        {
            var oil = Flask(2, "oil"); var torch = Torch(before); var other = Torch(2);
            string row = Row(oil, torch);
            StringAssert.Contains("1 oil", Actions(oil).Single(a => a.Command == row).Display);
            Assert.True(Act(oil, row)); Assert.AreEqual(1, Units(oil));
            Assert.AreEqual(after, torch.GetPart<FuelPart>().FuelMass);
            Assert.AreEqual(2, other.GetPart<FuelPart>().FuelMass);
            Assert.False(torch.GetPart<LightSourcePart>().Enabled); Assert.AreEqual(0, Count("LampOil"));
        }
        [Test] public void LastUnitLeavesTheSameCanonicalEmptyFlaskAndReadersExplainBothFuels()
        {
            var oil = Flask(1, "oil"); var torch = Torch(); string id = oil.ID;
            Assert.True(Act(oil, Row(oil, torch))); Assert.AreEqual(id, oil.ID);
            Assert.True(Pack.Objects.Contains(oil)); Assert.AreEqual(0, Units(oil));
            Assert.AreEqual("", oil.GetPart<LiquidVesselPart>().LiquidId);
            Assert.True(ItemExamineService.TryDescribeDetails(torch, out var text)); StringAssert.Contains("flask", text);
        }
        [Test] public void UsefulRefillIsReadableAndPrecedesTheManyTransferRows()
        {
            var oil = Flask(3, "oil"); var torch = Torch(48.8000031f);
            Flask(0);
            var row = Actions(oil).Single(a => a.Command == Row(oil, torch));
            StringAssert.Contains("48.8→50/50", row.Display);
            Assert.AreEqual(ActionTime.OneAction, row.Time);
            var transfers = Actions(oil).Where(a => a.Command.StartsWith("Transfer")).ToArray();
            Assert.IsNotEmpty(transfers);
            Assert.Greater(row.Priority, transfers.Max(a => a.Priority));
        }
        [TestCase("water")] [TestCase("bog-mire")] [TestCase("empty")]
        [TestCase("stacked")] [TestCase("duplicate")]
        public void UnsupportedSourcesNeverOfferOrSpendFuel(string kind)
        {
            var oil = Flask(kind == "empty" ? 0 : 2, kind == "water" || kind == "bog-mire" ? kind : "oil"); var torch = Torch();
            if (kind == "stacked") { var s = oil.GetPart<StackerPart>(); if (s == null) oil.AddPart(s = new StackerPart()); s.StackCount = 2; }
            if (kind == "duplicate") oil.AddPart(new LiquidVesselPart { Volume = 2, LiquidId = "oil" });
            Assert.IsEmpty(Rows(oil)); Assert.AreEqual(5, torch.GetPart<FuelPart>().FuelMass);
        }
        [TestCase("full")] [TestCase("lit")] [TestCase("ground")] [TestCase("stacked")]
        [TestCase("duplicate-fuel")] [TestCase("nan")]
        public void UnqualifiedTorchesNeverOfferRefilling(string kind)
        {
            var oil = Flask(2, "oil"); var torch = Torch();
            if (kind == "full") torch.GetPart<FuelPart>().FuelMass = 50;
            if (kind == "lit") torch.GetPart<LightSourcePart>().Enabled = true;
            if (kind == "ground") { Pack.RemoveObject(torch); Zone.AddEntity(torch, 10, 10); }
            if (kind == "stacked") torch.GetPart<StackerPart>().StackCount = 2;
            if (kind == "duplicate-fuel") torch.AddPart(new FuelPart());
            if (kind == "nan") torch.GetPart<FuelPart>().FuelMass = float.NaN;
            Assert.IsEmpty(Rows(oil)); Assert.AreEqual(2, Units(oil));
        }
        [Test] public void MenusArePureAndAChangedQuotedTorchRefusesWithoutSpending()
        {
            var oil = Flask(2, "oil"); var torch = Torch(); string command = Row(oil, torch);
            for (int i = 0; i < 4; i++) Assert.AreEqual(command, Row(oil, torch));
            torch.GetPart<FuelPart>().FuelMass = 6;
            Assert.False(Act(oil, command)); Assert.AreEqual(2, Units(oil)); Assert.AreEqual(6, torch.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void BeforeCallbackChangedSourceCannotRedeemOldQuote()
        {
            var oil = Flask(2, "oil"); var torch = Torch(); string command = Row(oil, torch);
            Actor.AddPart(new Probe { Event = "BeforeInventoryAction", Run = () => oil.GetPart<LiquidVesselPart>().Volume = 1 });
            Assert.False(Act(oil, command)); Assert.AreEqual(1, Units(oil)); Assert.AreEqual(5, torch.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void FailedOuterCallbackRestoresOnlyPaymentAndPreservesInjury()
        {
            var oil = Flask(1, "oil"); var torch = Torch(); string command = Row(oil, torch); int hp = Actor.GetStat("Hitpoints").Value;
            Actor.AddPart(new Probe { Run = () => Actor.GetStat("Hitpoints").BaseValue -= 2, Fail = true });
            Assert.False(Act(oil, command)); Assert.AreEqual(1, Units(oil)); Assert.AreEqual("oil", oil.GetPart<LiquidVesselPart>().LiquidId);
            Assert.AreEqual(5, torch.GetPart<FuelPart>().FuelMass); Assert.AreEqual(hp - 2, Actor.GetStat("Hitpoints").Value);
        }
        [Test] public void IndependentFuelAndOilChangesSurviveFailedCommit()
        {
            var oil = Flask(2, "oil"); var torch = Torch(); string command = Row(oil, torch);
            Actor.AddPart(new Probe { Run = () => { oil.GetPart<LiquidVesselPart>().Volume++; torch.GetPart<FuelPart>().FuelMass -= 2; } });
            Assert.False(Act(oil, command)); Assert.AreEqual(3, Units(oil)); Assert.AreEqual(3, torch.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void ReplacedLiquidStateIsPreservedWhileFuelIsUndone()
        {
            var oil = Flask(2, "oil"); var torch = Torch(); string command = Row(oil, torch);
            Actor.AddPart(new Probe { Run = () => { oil.GetPart<LiquidVesselPart>().LiquidId = "water"; oil.GetPart<LiquidVesselPart>().Volume = 7; } });
            Assert.False(Act(oil, command)); Assert.AreEqual(7, Units(oil)); Assert.AreEqual("water", oil.GetPart<LiquidVesselPart>().LiquidId);
            Assert.AreEqual(5, torch.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void ForcedDropRemainsDroppedAfterRollback()
        {
            var oil = Flask(2, "oil"); var torch = Torch(); string command = Row(oil, torch);
            Actor.AddPart(new Probe { Run = () => { Pack.RemoveObject(oil); Zone.AddEntity(oil, 10, 10); } });
            Assert.False(Act(oil, command)); Assert.False(Pack.Objects.Contains(oil));
            Assert.True(Zone.GetCell(10, 10).Objects.Contains(oil)); Assert.AreEqual(2, Units(oil)); Assert.AreEqual(5, torch.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void ReentrantRefillCannotUseClaimedOwners()
        {
            var oil = Flask(3, "oil"); var torch = Torch(); string command = Row(oil, torch); bool nested = true;
            Actor.AddPart(new Probe { Run = () => nested = Act(oil, Row(oil, torch)) });
            Assert.True(Act(oil, command)); Assert.False(nested); Assert.AreEqual(2, Units(oil)); Assert.AreEqual(30, torch.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void SavedOilAndFuelRemainFiniteAndCanContinueAfterGraphReload()
        {
            var oil = Flask(2, "oil"); var torch = Torch(); string oilID = oil.ID, torchID = torch.ID;
            Assert.True(Act(oil, Row(oil, torch))); var loaded = RoundTrip();
            Actor = loaded.Player; Zone = loaded.ZoneManager.ActiveZone; SettlementRuntime.ActiveZone = Zone;
            oil = Pack.Objects.Single(e => e.ID == oilID); torch = Pack.Objects.Single(e => e.ID == torchID);
            Assert.AreEqual(1, Units(oil)); Assert.AreEqual(30, torch.GetPart<FuelPart>().FuelMass);
            Assert.True(Act(oil, Row(oil, torch))); Assert.AreEqual(0, Units(oil)); Assert.AreEqual(50, torch.GetPart<FuelPart>().FuelMass);
        }
        public sealed class Probe : Part
        {
            public string Event = "AfterInventoryAction"; public Action Run; public bool Fail; bool done;
            public override bool HandleEvent(GameEvent e)
            {
                if (!done && e.ID == Event) { done = true; Run?.Invoke(); if (Fail) throw new InvalidOperationException("oil fuel rollback probe"); }
                return true;
            }
        }
    }
}
