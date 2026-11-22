using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ProofingDestinationAccessTests : FiftyWorldFixture
    {
        [SetUp] public void SetupPress() { ProofingRegistry.ResetForTests(); }

        InventoryAction[] Rows(Entity reagent) => Actions(reagent).Where(a => a.Name == "PressProofing").ToArray();
        string Destination(Entity reagent, Entity flask) => Rows(reagent)
            .Single(r => !r.IsDisabled && r.Command == "PressProofing|cold|" + Id(flask)).Command;
        Entity[] FilledChoices() => Enumerable.Range(0, 9).Select(n => Flask(n, "cold-proofing")).ToArray();

        [TestCase(false)] [TestCase(true)]
        public void EveryEligibleDestinationIsOfferedForEachReagentElement(bool dualElement)
        {
            var reagent = Carry(dualElement ? "GlimmerBrine" : "FrostLichen");
            var flasks = Enumerable.Range(0, 9).Select(_ => Flask(0)).ToArray();
            var rows = Rows(reagent);
            Assert.AreEqual(dualElement ? 18 : 9, rows.Length);
            Assert.AreEqual(rows.Length, rows.Select(r => r.Command).Distinct().Count());
            foreach (var flask in flasks)
                Assert.AreEqual(dualElement ? 2 : 1, rows.Count(r => !r.IsDisabled && r.Command.EndsWith("|" + Id(flask), StringComparison.Ordinal)));
        }

        [Test] public void ASelectedLateEmptyFlaskReceivesOnlyItsPaidUnitsWhilePreferredFullerFlasksStayUnchanged()
        {
            var reagent = Carry("FrostLichen"); var flasks = FilledChoices();
            Assert.True(Act(reagent, Destination(reagent, flasks[0])));
            Assert.False(Pack.Objects.Contains(reagent));
            Assert.AreEqual("cold-proofing", flasks[0].GetPart<LiquidVesselPart>().LiquidId);
            Assert.AreEqual(2, Units(flasks[0]));
            for (int i = 1; i < flasks.Length; i++) Assert.AreEqual(i, Units(flasks[i]));
        }

        [TestCase("full")] [TestCase("wrong-liquid")] [TestCase("removed")]
        public void AFormerlyLateDestinationIsRevalidatedBeforeAnyPayment(string fault)
        {
            var reagent = Carry("FrostLichen"); var flasks = FilledChoices();
            string command = Destination(reagent, flasks[0]);
            var vessel = flasks[0].GetPart<LiquidVesselPart>();
            if (fault == "full") { vessel.Volume = 12; vessel.LiquidId = "cold-proofing"; }
            if (fault == "wrong-liquid") { vessel.Volume = 1; vessel.LiquidId = "water"; }
            if (fault == "removed") Assert.True(Pack.RemoveObject(flasks[0]));
            int volume = vessel.Volume; string liquid = vessel.LiquidId;
            Assert.False(Act(reagent, command));
            Assert.True(Pack.Objects.Contains(reagent));
            vessel = flasks[0].GetPart<LiquidVesselPart>();
            Assert.AreEqual(volume, vessel.Volume); Assert.AreEqual(liquid, vessel.LiquidId);
            for (int i = 1; i < flasks.Length; i++) Assert.AreEqual(i, Units(flasks[i]));
        }

        [Test] public void InterruptedLateDestinationRestoresBothReagentAndExactVesselState()
        {
            var reagent = Carry("FrostLichen"); var flasks = FilledChoices();
            string command = Destination(reagent, flasks[0]); FailAfter();
            Assert.False(Act(reagent, command));
            Assert.True(Pack.Objects.Contains(reagent));
            Assert.AreEqual(0, Units(flasks[0]));
            Assert.AreEqual("", flasks[0].GetPart<LiquidVesselPart>().LiquidId);
            for (int i = 1; i < flasks.Length; i++) Assert.AreEqual(i, Units(flasks[i]));
        }

        [Test] public void RemovingTheMenuLimitDoesNotOfferFullMixedMalformedStackedOrUnownedVessels()
        {
            var reagent = Carry("FrostLichen"); Flask(12, "cold-proofing"); Flask(1, "water"); Skin(0);
            var malformed = Flask(0); malformed.GetPart<LiquidVesselPart>().LiquidId = "cold-proofing";
            var unowned = Flask(0); unowned.GetPart<PhysicsPart>().InInventory = new Entity();
            var stacked = Flask(0); var stack = stacked.GetPart<StackerPart>();
            if (stack == null) { stack = new StackerPart(); stacked.AddPart(stack); }
            stack.StackCount = 2;
            var rows = Rows(reagent);
            Assert.AreEqual(1, rows.Length); Assert.True(rows[0].IsDisabled);
            Assert.True(Pack.Objects.Contains(reagent));
        }
    }
}
