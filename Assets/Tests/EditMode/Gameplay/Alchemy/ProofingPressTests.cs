using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Pressing a reagent into a flask makes the bottled proofing (Docs/MACHINE-PROOFING.md): the temporary tier's carried form. The element is whatever element property the reagent carries, and the
    /// reagent's level for that property is how many coats it makes, so a stronger reagent is a fuller bottle and a new reagent needs no new recipe. It needs no machine and no fire, only a flask that is
    /// empty or already holds the same proofing. One reagent and the room it needs are spent, whole, or nothing is.
    /// </summary>
    public sealed class ProofingPressTests : FiftyWorldFixture
    {
        [SetUp] public void SetupPress() { ProofingRegistry.ResetForTests(); Diag.ResetAll(); MessageLog.Clear(); }

        InventoryAction[] PressRows(Entity reagent) => Actions(reagent).Where(a => a.Name == "PressProofing").ToArray();
        LiquidVesselPart Vessel(Entity flask) => flask.GetPart<LiquidVesselPart>();
        int Reagents(string blueprint) => Pack.Objects.Where(o => o.BlueprintName == blueprint).Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1);

        [Test] public void AReagentWithAnElementOffersToPressIntoAnEmptyFlaskAndSaysHowManyCoats()
        {
            var lichen = Carry("FrostLichen"); var flask = Flask(0); var rows = PressRows(lichen);
            Assert.AreEqual(1, rows.Length, string.Join(" / ", Actions(lichen).Select(a => a.Display))); Assert.False(rows[0].IsDisabled);
            StringAssert.Contains("cold", rows[0].Display); StringAssert.Contains("2 coats", rows[0].Display); StringAssert.StartsWith("PressProofing|cold|" + Id(flask), rows[0].Command);
        }

        [Test] public void PressingSpendsOneReagentAndFillsTheFlaskWithTheProofingAtTheReagentsLevel()
        {
            var salt = Carry("GlacierSalt"); var flask = Flask(0); Assert.True(Act(salt, PressRows(salt).Single().Command), string.Join("|", MessageLog.GetRecent(3)));
            Assert.AreEqual("cold-proofing", Vessel(flask).LiquidId); Assert.AreEqual(3, Vessel(flask).Volume, "cold:3 makes three coats"); Assert.AreEqual(0, Reagents("GlacierSalt"));
            var records = DiagQuery.Apply(new DiagQuery.Filter { Category = "liquid", Kind = "ProofingPressed", Limit = 5 }).Records; Assert.AreEqual(1, records.Count); StringAssert.Contains("cold", records[0].PayloadJson);
        }

        [Test] public void OnlyOneReagentOfAStackIsSpent()
        {
            var a = Carry("FrostLichen"); var b = Carry("FrostLichen"); var flask = Flask(0); Assert.AreEqual(2, Reagents("FrostLichen"));
            Assert.True(Act(a, PressRows(a).First().Command)); Assert.AreEqual(1, Reagents("FrostLichen")); Assert.AreEqual(2, Vessel(flask).Volume);
        }

        [Test] public void AFlaskAlreadyHoldingTheSameProofingTakesMoreUpToItsCapacity()
        {
            var lichen = Carry("FrostLichen"); var flask = Flask(5, "cold-proofing"); Assert.True(Act(lichen, PressRows(lichen).Single().Command)); Assert.AreEqual(7, Vessel(flask).Volume);
        }

        [Test] public void AFlaskWithTooLittleRoomIsRefusedWithTheNumbers()
        {
            var salt = Carry("GlacierSalt"); Flask(11, "cold-proofing"); var row = PressRows(salt).Single(); Assert.True(row.IsDisabled); Assert.AreEqual("no-room", row.DisabledCode);
            StringAssert.Contains("3", row.DisabledReason); StringAssert.Contains("1", row.DisabledReason); Assert.False(Act(salt, "PressProofing|cold|nope")); Assert.AreEqual(1, Reagents("GlacierSalt"));
        }

        [Test] public void AFlaskHoldingSomethingElseOrAWaterskinIsNotAnOption_Counter()
        {
            var lichen = Carry("FrostLichen"); Flask(4, "water"); Flask(3, "heat-proofing"); Skin(2);
            var rows = PressRows(lichen); Assert.AreEqual(1, rows.Length); Assert.True(rows[0].IsDisabled); Assert.AreEqual("no-flask", rows[0].DisabledCode); StringAssert.Contains("flask", rows[0].DisabledReason);
        }

        [Test] public void AReagentCarryingTwoElementsOffersBothAsSeparateRows()
        {
            var brine = Carry("GlimmerBrine"); var flask = Flask(0); var rows = PressRows(brine); Assert.AreEqual(2, rows.Length);
            CollectionAssert.AreEquivalent(new[] { "PressProofing|electric|", "PressProofing|acid|" }, rows.Select(r => r.Command.Substring(0, r.Command.IndexOf(Id(flask), StringComparison.Ordinal))).ToArray());
            Assert.True(Act(brine, rows.Single(r => r.Command.StartsWith("PressProofing|acid|")).Command)); Assert.AreEqual("acid-proofing", Vessel(flask).LiquidId); Assert.AreEqual(2, Vessel(flask).Volume);
        }

        [Test] public void AReagentWithNoElementOffersNoPressing_Counter()
        {
            Flask(0); foreach (var name in new[] { "LampOil", "MendleafSprig", "BogSap" }) Assert.IsEmpty(PressRows(Carry(name)), name);
        }

        [Test] public void AnInterruptedPressGivesTheReagentBackAndLeavesTheFlaskAsItWas()
        {
            var lichen = Carry("FrostLichen"); var flask = Flask(0); string command = PressRows(lichen).Single().Command; FailAfter();
            Assert.False(Act(lichen, command)); Assert.AreEqual(1, Reagents("FrostLichen")); Assert.AreEqual(0, Vessel(flask).Volume); Assert.AreEqual("", Vessel(flask).LiquidId);
        }

        [Test] public void AReagentLyingOnTheGroundOffersNothing_Counter()
        {
            Flask(0); var lichen = Place("FrostLichen", 12, 10); Assert.IsEmpty(PressRows(lichen));
        }
    }
}
