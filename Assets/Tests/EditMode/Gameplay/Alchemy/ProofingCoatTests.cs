using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Coating a garment from a bottle (Docs/MACHINE-PROOFING.md): the temporary tier. A flask holding an element's proofing offers one row per garment you hold (worn or not, any armor, not just leather and
    /// cloth); one unit of the liquid is one coat: +35 of that resistance for 100 worn turns, paused while the garment is off. The same element tops the turns up to 300, another replaces. It needs no machine.
    /// </summary>
    public sealed class ProofingCoatTests : FiftyWorldFixture
    {
        [SetUp] public void SetupCoat() { EnhancementFactory.ForceReinitialize(); ProofingRegistry.ResetForTests(); Diag.ResetAll(); MessageLog.Clear(); Pack.MaxWeight = 400; }

        InventoryAction[] CoatRows(Entity flask) => Actions(flask).Where(a => a.Name == "CoatGarment").ToArray();
        LiquidVesselPart Vessel(Entity flask) => flask.GetPart<LiquidVesselPart>();
        EnhancementSteeped Coat(Entity garment) => garment.Parts.OfType<EnhancementSteeped>().FirstOrDefault();
        int Stat(string name) => Actor.GetStatValue(name, 0);
        string Row(Entity flask, Entity garment) => CoatRows(flask).Single(r => !r.IsDisabled && r.Command.EndsWith(Uri.EscapeDataString(garment.ID), StringComparison.Ordinal)).Command;

        [Test] public void AFlaskOfProofingOffersARowForEveryGarmentYouHold_AnyArmorWornOrNot()
        {
            var flask = Flask(3, "cold-proofing"); var leather = Carry("LeatherArmor"); var mail = Carry("ChainMail"); var worn = Carry("KilnfeltApron"); Assert.True(InventorySystem.Equip(Actor, worn));
            var rows = CoatRows(flask); Assert.AreEqual(3, rows.Length, string.Join(" / ", rows.Select(r => r.Display))); Assert.True(rows.All(r => !r.IsDisabled));
            StringAssert.Contains("+35 cold", rows[0].Hint); StringAssert.Contains("freezing", rows[0].Hint);
        }

        [Test] public void NothingToCoatWithWhenTheFlaskHoldsAnythingElse_Counter()
        {
            Carry("LeatherArmor"); foreach (var (volume, liquid) in new[] { (3, "water"), (3, "oil"), (3, "bog-mire") }) Assert.IsEmpty(CoatRows(Flask(volume, liquid)), liquid);
            Assert.IsEmpty(CoatRows(Flask(0)), "an empty flask");
        }

        [Test] public void NothingToCoatIsOneDisabledRowThatSaysWhat()
        {
            var flask = Flask(2, "heat-proofing"); Carry("Dagger"); var rows = CoatRows(flask); Assert.AreEqual(1, rows.Length); Assert.True(rows[0].IsDisabled); Assert.AreEqual("no-garment", rows[0].DisabledCode); StringAssert.Contains("armor", rows[0].DisabledReason);
        }

        [Test] public void CoatingSpendsOneUnitAndLeavesTheGarmentWithAColdCoatThatWorksWhenWorn()
        {
            var flask = Flask(3, "cold-proofing"); var garment = Carry("LeatherArmor"); Assert.True(Act(flask, Row(flask, garment)), string.Join("|", MessageLog.GetRecent(3)));
            Assert.AreEqual(2, Vessel(flask).Volume); var coat = Coat(garment); Assert.NotNull(coat); Assert.AreEqual("cold", coat.Kind); Assert.AreEqual(35, coat.Amount); Assert.AreEqual(100, coat.TurnsLeft);
            Assert.AreEqual(0, Stat("ColdResistance"), "not worn: nothing yet"); Assert.True(InventorySystem.Equip(Actor, garment)); Assert.AreEqual(35, Stat("ColdResistance"));
            Assert.True(InventorySystem.UnequipItem(Actor, garment)); Assert.AreEqual(0, Stat("ColdResistance"), "taken off, the bonus goes and the turns are kept"); Assert.AreEqual(100, Coat(garment).TurnsLeft);
        }

        [Test] public void CoatingAWornGarmentWorksAtOnce()
        {
            var flask = Flask(2, "heat-proofing"); var garment = Carry("LeatherArmor"); Assert.True(InventorySystem.Equip(Actor, garment));
            Assert.True(Act(flask, Row(flask, garment))); Assert.AreEqual(35, Stat("HeatResistance"), "no need to take it off and put it on again");
        }

        [Test] public void TheLastUnitEmptiesTheFlaskCleanly()
        {
            var flask = Flask(1, "acid-proofing"); var garment = Carry("LeatherArmor"); Assert.True(Act(flask, Row(flask, garment))); Assert.AreEqual(0, Vessel(flask).Volume); Assert.AreEqual("", Vessel(flask).LiquidId);
        }

        [Test] public void AnotherCoatOfTheSameElementAddsTurnsUpToTheCap_AndThenRefuses()
        {
            var flask = Flask(6, "cold-proofing"); var garment = Carry("LeatherArmor"); Assert.True(InventorySystem.Equip(Actor, garment));
            Assert.True(Act(flask, Row(flask, garment))); StringAssert.Contains("adds", CoatRows(flask).Single().Display);
            Assert.True(Act(flask, Row(flask, garment))); Assert.AreEqual(200, Coat(garment).TurnsLeft); Assert.True(Act(flask, Row(flask, garment))); Assert.AreEqual(300, Coat(garment).TurnsLeft);
            Assert.AreEqual(35, Stat("ColdResistance"), "still one coat's strength: coats add time, not power");
            var row = CoatRows(flask).Single(); Assert.True(row.IsDisabled); Assert.AreEqual("max-coat", row.DisabledCode); Assert.AreEqual(3, Vessel(flask).Volume, "the refused row spent nothing");
        }

        [Test] public void ATopUpFromAPartlyWornCoatStopsAtTheCapNotAboveIt()
        {
            var flask = Flask(3, "cold-proofing"); var garment = Carry("LeatherArmor"); Assert.True(Act(flask, Row(flask, garment))); Coat(garment).TurnsLeft = 250; Coat(garment).TurnsTotal = 300;
            Assert.True(Act(flask, Row(flask, garment))); Assert.AreEqual(ProofingRules.CoatMaxTurns, Coat(garment).TurnsLeft, "250 + 100 is held to 300"); Assert.AreEqual(ProofingRules.CoatMaxTurns, Coat(garment).TurnsTotal);
        }

        [Test] public void TheTopUpRowSaysHowManyTurnsItReallyAddsWhenTheCapIsNear()
        {
            var flask = Flask(3, "cold-proofing"); var garment = Carry("LeatherArmor"); Assert.True(Act(flask, Row(flask, garment))); Coat(garment).TurnsLeft = 250; Coat(garment).TurnsTotal = 300;
            StringAssert.Contains("adds 50 turns to 250", CoatRows(flask).Single().Display); Coat(garment).TurnsLeft = 100; StringAssert.Contains("adds 100 turns to 100", CoatRows(flask).Single().Display);
        }

        [Test] public void ATopUpOfAWornCoatWhoseEffectWasStrippedPutsTheEffectBack()
        {
            var flask = Flask(3, "cold-proofing"); var garment = Carry("LeatherArmor"); Assert.True(InventorySystem.Equip(Actor, garment)); Assert.True(Act(flask, Row(flask, garment))); Assert.AreEqual(35, Stat("ColdResistance"));
            Actor.GetPart<StatusEffectsPart>().RemoveEffect(Actor.GetEffect<SteepedAcidEffect>()); Assert.AreEqual(0, Stat("ColdResistance"), "fixture: dispelled while the garment stays worn");
            Assert.True(Act(flask, Row(flask, garment))); Assert.AreEqual(35, Stat("ColdResistance"), "the top-up does not pay for a coat that gives nothing"); Assert.AreEqual(200, Actor.GetEffect<SteepedAcidEffect>().Duration);
        }

        [Test] public void AnotherElementReplacesTheCoatAndSaysSo()
        {
            var cold = Flask(2, "cold-proofing"); var heat = Flask(2, "heat-proofing"); var garment = Carry("LeatherArmor"); Assert.True(InventorySystem.Equip(Actor, garment)); Assert.True(Act(cold, Row(cold, garment)));
            StringAssert.Contains("replaces", CoatRows(heat).Single().Display); Assert.True(Act(heat, Row(heat, garment)));
            Assert.AreEqual(0, Stat("ColdResistance")); Assert.AreEqual(35, Stat("HeatResistance")); Assert.AreEqual("heat", Coat(garment).Kind); Assert.AreEqual(100, Coat(garment).TurnsLeft);
        }

        [Test] public void AGarmentWithTwoOtherTreatmentsRefusesACoat_ButOneWithACoatCanBeCoatedAgain()
        {
            var flask = Flask(4, "cold-proofing"); var full = Carry("LeatherArmor"); full.AddPart(new EnhancementLacquered()); full.AddPart(new EnhancementGlowQuartz());
            var row = CoatRows(flask).Single(); Assert.True(row.IsDisabled); Assert.AreEqual("slot-cap", row.DisabledCode); Assert.False(Act(flask, "CoatGarment|" + Uri.EscapeDataString(full.ID))); Assert.AreEqual(4, Vessel(flask).Volume); Assert.Null(Coat(full));
            var open = Carry("FilterHood"); open.AddPart(new EnhancementLacquered()); Assert.True(Act(flask, Row(flask, open))); Assert.NotNull(Coat(open)); Assert.True(Act(flask, Row(flask, open)), "re-coating a coated piece uses the coat's own slot");
        }

        [Test] public void AnInterruptedCoatGivesBackTheUnitAndLeavesTheGarmentAndWearerAsTheyWere()
        {
            var flask = Flask(2, "cold-proofing"); var garment = Carry("LeatherArmor"); Assert.True(InventorySystem.Equip(Actor, garment)); string command = Row(flask, garment); FailAfter();
            Assert.False(Act(flask, command)); Assert.AreEqual(2, Vessel(flask).Volume); Assert.Null(Coat(garment)); Assert.AreEqual(0, Stat("ColdResistance"));
        }

        [Test] public void AnInterruptedReplacementPutsTheOldCoatBack()
        {
            var cold = Flask(2, "cold-proofing"); var heat = Flask(2, "heat-proofing"); var garment = Carry("LeatherArmor"); Assert.True(InventorySystem.Equip(Actor, garment)); Assert.True(Act(cold, Row(cold, garment)));
            string command = Row(heat, garment); FailAfter(); Assert.False(Act(heat, command)); Assert.AreEqual(2, Vessel(heat).Volume); Assert.AreEqual("cold", Coat(garment).Kind); Assert.AreEqual(35, Stat("ColdResistance")); Assert.AreEqual(0, Stat("HeatResistance"));
        }

        [Test] public void ACoatedWearerShortensTheFreezeItself_TheWholePointOfTheStatusRule()
        {
            var flask = Flask(1, "cold-proofing"); var garment = Carry("LeatherArmor"); Assert.True(InventorySystem.Equip(Actor, garment)); Assert.True(Act(flask, Row(flask, garment)));
            Assert.True(Actor.ApplyEffect(new FrozenEffect(0.6f))); Assert.AreEqual(0.39f, Actor.GetEffect<FrozenEffect>().Cold, 1e-3f);
        }

        [Test] public void TheCoatWearsOnlyOnTheWearersTurnsAndThenIsGone()
        {
            var flask = Flask(1, "heat-proofing"); var garment = Carry("LeatherArmor"); Assert.True(InventorySystem.Equip(Actor, garment)); Assert.True(Act(flask, Row(flask, garment)));
            var effect = Actor.GetPart<StatusEffectsPart>().GetAllEffects().OfType<SteepedAcidEffect>().Single(); effect.Duration = 1; Actor.FireEvent(GameEvent.New("EndTurn"));
            Assert.AreEqual(0, Stat("HeatResistance")); Assert.Null(Coat(garment), "ran out: the garment is plain again"); StringAssert.Contains("worn off", string.Join("|", MessageLog.GetRecent(4)));
        }

        [Test] public void ExaminingAFlaskOfProofingSaysWhatToDoWithIt_AndAPlainFlaskSaysNothingOfTheKind_Counter()
        {
            Assert.True(ItemExamineService.TryDescribeDetails(Flask(3, "cold-proofing"), out string text));
            StringAssert.Contains("Contents: cold-proofing", text); StringAssert.Contains("Coat a garment", text); StringAssert.Contains("+35 cold", text); StringAssert.Contains("100 worn turns", text); StringAssert.Contains("trough", text);
            Assert.True(ItemExamineService.TryDescribeDetails(Flask(3, "water"), out string plain)); StringAssert.DoesNotContain("Coat a garment", plain);
            Assert.True(ItemExamineService.TryDescribeDetails(Flask(0), out string empty)); StringAssert.DoesNotContain("Coat a garment", empty);
        }

        [Test] public void TheCoatRowSaysWhatYourResistanceWouldBeAndWhenThatIsImmunity()
        {
            var flask = Flask(4, "cold-proofing"); var cloak = Carry("ColdwardCloak"); var body = Carry("LeatherArmor");
            StringAssert.Contains("Your cold resistance: 0 → 35 once worn", CoatRows(flask).First(r => r.Display.Contains("leather")).Hint, "unworn: what it would be once you put it on");
            Assert.True(InventorySystem.Equip(Actor, cloak)); StringAssert.Contains("Your cold resistance: 50 → 85", CoatRows(flask).First(r => r.Display.Contains("(worn)")).Hint);
            Assert.True(InventorySystem.Equip(Actor, body)); Assert.True(Act(flask, Row(flask, body))); string hint = CoatRows(flask).First(r => r.Display.Contains("coldward")).Hint; StringAssert.Contains("Your cold resistance: 85 → 100 (immune to freezing)", hint);
            StringAssert.Contains("adds turns", CoatRows(flask).First(r => r.Display.Contains("leather")).Hint); StringAssert.DoesNotContain("→", CoatRows(flask).First(r => r.Display.Contains("leather")).Hint, "a top-up of a coat you already count changes only the turns");
        }

        [Test] public void EveryGarmentYouHoldIsOfferedUpToACapOfTwelve_WornFirst_AndTheRestAreCountedNotHidden()
        {
            var flask = Flask(4, "cold-proofing"); var names = new[] { "LeatherArmor", "ChainMail", "FilterHood", "KilnfeltApron", "ColdwardCloak", "IronshodBoots", "GroundwireScreen", "AcidworkerApron" };
            var held = new System.Collections.Generic.List<Entity>(); foreach (var n in names) held.Add(Carry(n));
            Assert.AreEqual(8, CoatRows(flask).Count(r => !r.IsDisabled), "eight garments, eight rows (the old cap of six hid two)");
            foreach (var n in new[] { "Buckler", "IronHelmet", "LeatherBoots", "LeatherGloves", "LeatherCap", "WardedCloak", "PlateArmor" }) held.Add(Carry(n));   // distinct blueprints: armor of one kind would stack into one entry
            Assert.True(InventorySystem.Equip(Actor, held[0]), "equip: " + string.Join("|", MessageLog.GetRecent(3))); var rows = CoatRows(flask);
            Assert.AreEqual(13, rows.Length, "twelve garments and one row saying how many more"); Assert.True(rows[0].Display.Contains("(worn)"), "what you wear is listed first: " + string.Join(" / ", rows.Select(r => r.Display)));
            var more = rows.Single(r => r.IsDisabled && r.DisabledCode == "more"); StringAssert.Contains("3 more", more.DisabledReason);   // fifteen held, twelve shown
        }

        // ── a stack of identical garments: the coat goes on one unit of it ──

        int Plain(string blueprint) => Pack.Objects.Where(o => o.BlueprintName == blueprint && Coat(o) == null).Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1);

        [Test] public void AStackOfGarmentsOffersOneRowAndCoatingSplitsOneUnitOffItself()
        {
            var flask = Flask(3, "cold-proofing"); var stack = Carry("LeatherArmor"); Carry("LeatherArmor"); Carry("LeatherArmor"); Assert.AreEqual(3, stack.GetPart<StackerPart>().StackCount, "fixture: armor of one kind stacks");
            var row = CoatRows(flask).Single(); Assert.False(row.IsDisabled); StringAssert.Contains("1 of 3", row.Display); Assert.True(Act(flask, row.Command), string.Join("|", MessageLog.GetRecent(3)));
            Assert.AreEqual(2, stack.GetPart<StackerPart>().StackCount); Assert.Null(Coat(stack), "the rest of the stack is untouched"); var coated = Pack.Objects.Single(o => Coat(o) != null); Assert.AreNotSame(stack, coated);
            Assert.AreEqual(1, coated.GetPart<StackerPart>()?.StackCount ?? 1); Assert.AreEqual("cold", Coat(coated).Kind); Assert.AreEqual(2, Vessel(flask).Volume); Assert.AreEqual(2, Plain("LeatherArmor"), "two plain ones remain, never merged with the coated one");
        }

        [Test] public void ACoatedUnitNeverStacksBackWithThePlainOnes_Counter()
        {
            var flask = Flask(4, "cold-proofing"); Carry("LeatherArmor"); Carry("LeatherArmor"); Assert.True(Act(flask, CoatRows(flask).First().Command)); Carry("LeatherArmor");
            Assert.AreEqual(2, Pack.Objects.Count(o => o.BlueprintName == "LeatherArmor"), "one coated single and one plain stack, even after another plain one arrives"); Assert.AreEqual(1, Pack.Objects.Count(o => Coat(o) != null));
        }

        [Test] public void AnInterruptedCoatOfAStackPutsTheUnitBack()
        {
            var flask = Flask(3, "cold-proofing"); var stack = Carry("LeatherArmor"); Carry("LeatherArmor"); string command = CoatRows(flask).Single().Command; FailAfter();
            Assert.False(Act(flask, command)); Assert.AreEqual(2, stack.GetPart<StackerPart>().StackCount); Assert.AreEqual(1, Pack.Objects.Count(o => o.BlueprintName == "LeatherArmor")); Assert.AreEqual(3, Vessel(flask).Volume); Assert.IsFalse(Pack.Objects.Any(o => Coat(o) != null));
        }

        [Test] public void TheCoatsReaderSaysWhatItDoesAndWhatItAlsoBlunts()
        {
            var flask = Flask(1, "electric".Length > 0 ? "shock-proofing" : ""); var garment = Carry("LeatherArmor"); Assert.True(Act(flask, Row(flask, garment)));
            string text = Coat(garment).GetEffectDescription(); StringAssert.Contains("+35", text); StringAssert.Contains("shock", text); StringAssert.Contains("100 more turns", text); StringAssert.Contains("shock stun", text);
        }
    }
}
