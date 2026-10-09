using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class CombatSupplyDousingTests : FiftyWorldFixture
    {
        string Selection(Entity item, Entity target, bool clay = false)
            => Choice(item, clay ? "SmotherFire|" : "DrenchCreature|", a => a.Command.Split('|')[4] == Id(target));
        bool Offered(Entity item, Entity target, bool clay = false)
            => Actions(item).Any(a => a.Command.StartsWith(clay ? "SmotherFire|" : "DrenchCreature|", StringComparison.Ordinal)
                && a.Command.Split('|')[4] == Id(target));
        static BurningEffect Burn(Entity target)
        { var burn = new BurningEffect(1); Assert.True(target.ApplyEffect(burn)); return burn; }
        static void Amount(Entity item, int count)
        { if (!item.HasPart<StackerPart>()) item.AddPart(new StackerPart()); item.GetPart<StackerPart>().StackCount = count; }

        [SetUp] public void SetupDousing() { MessageLog.Clear(); Diag.ResetAll(); }

        [TestCase(false)] [TestCase(true)]
        public void CarriedWaterDousesSelfAndKeepsTheEmptyVessel(bool flask)
        {
            var item = flask ? Flask(1) : Skin(1); Burn(Actor);
            Assert.True(Act(item, Selection(item, Actor)));
            Assert.False(Actor.HasEffect<BurningEffect>()); Assert.AreEqual(1f, Actor.GetEffect<WetEffect>().Moisture);
            Assert.Less(Actor.GetPart<ThermalPart>().Temperature, Actor.GetPart<ThermalPart>().FlameTemperature);
            Assert.AreEqual(0, Units(item)); Assert.Contains(item, Pack.Objects);
            Assert.False(Offered(item, Actor));
        }

        [Test] public void DrenchingBeforeFireIsPossibleButWetnessAmplifiesLaterElectricity()
        {
            var item = Skin(2); Assert.True(Act(item, Selection(item, Actor)));
            Assert.False(Actor.HasEffect<BurningEffect>());
            var shock = new ElectrifiedEffect(1); Assert.True(Actor.ApplyEffect(shock)); Assert.AreEqual(2, shock.Charge);
            var dry = Place("MarlbackScrabbler", 12, 10); var dryShock = new ElectrifiedEffect(1);
            Assert.True(dry.ApplyEffect(dryShock)); Assert.AreEqual(1, dryShock.Charge);
        }

        [Test] public void FireClaySmothersWithoutAddingWetAndDoesNotRemoveOtherConditions()
        {
            var item = Carry("FireClay"); Amount(item, 3); Burn(Actor);
            var acid = new AcidicEffect(.4f); Assert.True(Actor.ApplyEffect(acid));
            var poison = new PoisonedEffect(4, "1"); Assert.True(Actor.ApplyEffect(poison));
            Assert.True(Act(item, Selection(item, Actor, true)));
            Assert.False(Actor.HasEffect<BurningEffect>()); Assert.False(Actor.HasEffect<WetEffect>());
            Assert.AreSame(acid, Actor.GetEffect<AcidicEffect>()); Assert.AreSame(poison, Actor.GetEffect<PoisonedEffect>());
            Assert.AreEqual(2, item.GetPart<StackerPart>().StackCount);
        }

        [TestCase(false)] [TestCase(true)]
        public void RescueAdjacentCreatureAndClayAlsoReachesBurningScenery(bool clay)
        {
            var target = Place("MarlbackScrabbler"); var brain = target.GetPart<BrainPart>(); Burn(target);
            var item = clay ? Carry("FireClay") : Skin(2);
            Assert.True(Act(item, Selection(item, target, clay)));
            Assert.False(target.HasEffect<BurningEffect>()); Assert.False(brain.IsPersonallyHostileTo(Actor));
            Assert.AreEqual(!clay, target.HasEffect<WetEffect>());
            var prop = Place("WoodenBarrel", 10, 11); Burn(prop);
            if (clay) { var more = Carry("FireClay"); Assert.True(Act(more, Selection(more, prop, true))); Assert.False(prop.HasEffect<BurningEffect>()); }
            else Assert.False(Offered(item, prop));
        }

        [Test] public void UninvitedDrenchingProvokesAnOutsiderButNotAPartyMember()
        {
            var target = Place("MarlbackScrabbler"); var item = Skin(3);
            Assert.True(Act(item, Selection(item, target))); Assert.True(target.GetPart<BrainPart>().IsPersonallyHostileTo(Actor));
            var friend = Place("MarlbackScrabbler", 10, 11); Assert.True(friend.GetPart<BrainPart>().SetPartyLeader(Actor));
            Assert.True(Act(item, Selection(item, friend))); Assert.False(friend.GetPart<BrainPart>().IsPersonallyHostileTo(Actor));
        }

        [TestCase(false)] [TestCase(true)]
        public void NoOpAndDuplicateSelectionsNeverSpendSupplies(bool clay)
        {
            var item = clay ? Carry("FireClay") : Skin(3); if (clay) Amount(item, 3);
            if (clay) { Assert.False(Offered(item, Actor, true)); Burn(Actor); }
            string command = Selection(item, Actor, clay); Assert.True(Act(item, command));
            Assert.False(Offered(item, Actor, clay)); Assert.False(Act(item, command));
            Assert.AreEqual(2, clay ? item.GetPart<StackerPart>().StackCount : Units(item));
        }

        [TestCase("actor")] [TestCase("target")] [TestCase("amount")] [TestCase("drop")]
        public void StaleWaterSelectionCannotRetargetOrSpend(string change)
        {
            var item = Skin(3); var target = Place("MarlbackScrabbler"); string command = Selection(item, target);
            if (change == "actor") Assert.True(Zone.MoveEntity(Actor, 9, 10));
            if (change == "target") Assert.True(Zone.MoveEntity(target, 10, 11));
            if (change == "amount") item.GetPart<WaterskinPart>().Charges = 2;
            if (change == "drop") { Assert.True(Pack.RemoveObject(item)); Assert.True(Zone.AddEntity(item, 10, 10)); }
            Assert.False(Act(item, command)); Assert.False(target.HasEffect<WetEffect>());
            Assert.AreEqual(change == "amount" ? 2 : 3, Units(item));
        }

        [TestCase(false)] [TestCase(true)]
        public void OuterFailureRestoresTheExactBurnWetnessTemperatureAndPayment(bool clay)
        {
            var item = clay ? Carry("FireClay") : Skin(2); var target = Place("MarlbackScrabbler");
            var burn = Burn(target); burn.LastRemovalCause = "prior"; var wet = new WetEffect(.2f); Assert.True(target.ApplyEffect(wet));
            float temperature = target.GetPart<ThermalPart>().Temperature;
            var before = target.GetPart<StatusEffectsPart>().GetAllEffects().ToArray();
            string command = Selection(item, target, clay); FailAfter();
            Assert.False(Act(item, command)); Assert.AreSame(burn, target.GetEffect<BurningEffect>());
            CollectionAssert.AreEqual(before, target.GetPart<StatusEffectsPart>().GetAllEffects());
            Assert.AreEqual("prior", burn.LastRemovalCause); Assert.AreSame(wet, target.GetEffect<WetEffect>()); Assert.AreEqual(.2f, wet.Moisture);
            Assert.AreEqual(temperature, target.GetPart<ThermalPart>().Temperature); Assert.Contains(item, Pack.Objects);
            if (!clay) Assert.AreEqual(2, Units(item));
            Assert.False(target.GetPart<BrainPart>().IsPersonallyHostileTo(Actor));
            Assert.False(DiagQuery.Apply(new DiagQuery.Filter { Category = "event", Kind = "EmergencyDousingUsed" }).Records.Any());
        }

        [Test] public void WetVetoDoesNotSpendWaterOrRemoveBurning()
        {
            var item = Skin(2); var burn = Burn(Actor); Actor.AddPart(new CombatSupplyRejectWetPart());
            string command = Selection(item, Actor); Assert.False(Act(item, command));
            Assert.AreSame(burn, Actor.GetEffect<BurningEffect>()); Assert.False(Actor.HasEffect<WetEffect>()); Assert.AreEqual(2, Units(item));
        }

        [TestCase("empty")] [TestCase("acid")] [TestCase("unrelated")]
        public void InvalidSupplyHasNoDrenchOrSmotherMenu(string invalid)
        {
            Burn(Actor); var item = invalid == "empty" ? Skin(0) : invalid == "acid" ? Flask(2, "acid") : Carry("PaleSalt");
            Assert.False(Offered(item, Actor)); Assert.False(Offered(item, Actor, true));
        }

        [TestCase(false)] [TestCase(true)]
        public void BlockedActorAndDistantTargetCannotUseSupplies(bool clay)
        {
            var item = clay ? Carry("FireClay") : Skin(2); var target = Place("MarlbackScrabbler", 12, 10); Burn(target);
            Assert.False(Offered(item, target, clay)); Assert.True(Zone.MoveEntity(target, 11, 10));
            string command = Selection(item, target, clay); Assert.True(Actor.ApplyEffect(new StunnedEffect(2)));
            Assert.False(Act(item, command)); Assert.False(Offered(item, target, clay)); Assert.True(target.HasEffect<BurningEffect>());
        }

        [Test] public void ClayPreservesExistingWetnessAndDoesNotMakeTheRecipientFireproof()
        {
            var item = Carry("FireClay"); var wet = new WetEffect(.2f); Actor.ApplyEffect(wet); Burn(Actor);
            Assert.True(Act(item, Selection(item, Actor, true))); Assert.AreSame(wet, Actor.GetEffect<WetEffect>()); Assert.AreEqual(.2f, wet.Moisture);
            Burn(Actor); Assert.True(Actor.HasEffect<BurningEffect>());
        }

        [Test] public void DrenchingSavedSuppliesWorksWithoutRebuildingTheirParts()
        {
            Skin(2); var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor); Zone.RemoveEntity(Actor); Actor = copy;
            Assert.True(Zone.AddEntity(Actor, 10, 10)); var item = Pack.Objects.Single(e => e.BlueprintName == "Waterskin");
            Assert.True(Act(item, Selection(item, Actor))); Assert.AreEqual(1, Units(item)); Assert.True(Actor.HasEffect<WetEffect>());
        }
    }

    public sealed class CombatSupplyRejectWetPart : Part
    {
        public override bool HandleEvent(GameEvent e)
            => !(e.ID == "BeforeApplyEffect" && e.GetParameter<Effect>("Effect") is WetEffect);
    }
}
