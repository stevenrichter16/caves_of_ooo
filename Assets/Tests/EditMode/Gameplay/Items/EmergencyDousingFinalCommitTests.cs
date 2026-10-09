using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class EmergencyDousingFinalCommitTests : FiftyWorldFixture
    {
        string Select(Entity item, Entity target)
            => Choice(item, "DrenchCreature|", action => action.Command.Split('|')[4] == Id(target));
        static BurningEffect Ignite(Entity target)
        { var fire = new BurningEffect(1); Assert.True(target.ApplyEffect(fire)); return fire; }
        DousingFinalMutationProbe observer;
        bool Execute(Entity item, string command)
        {
            var result = new InventoryCommandExecutor().Execute(new PerformInventoryActionCommand(item, command),
                new InventoryContext(Actor, Zone));
            Assert.True(observer.Completed, "The independent callback must complete normally, not merely throw into rollback.");
            return result.Success;
        }
        void After(Action mutation) { observer = new DousingFinalMutationProbe(mutation); Actor.AddPart(observer); }
        void NoPaidReceipt()
            => Assert.False(DiagQuery.Apply(new DiagQuery.Filter { Category = "event", Kind = "EmergencyDousingUsed" }).Records.Any());
        void ResetReceipts() => Diag.ResetAll();

        [Test] public void SuccessfulAfterCallbackMovementRejectsTheOldSelectionButPreservesTheMove()
        {
            var target = Place("MarlbackScrabbler"); var fire = Ignite(target); var water = Skin(2);
            float heat = target.GetPart<ThermalPart>().Temperature; string command = Select(water, target);
            After(() => Assert.True(Zone.MoveEntity(Actor, 9, 10))); ResetReceipts();
            Assert.False(Execute(water, command), "Final origin changed after TryAct returned, without throwing.");
            Assert.AreEqual((9, 10), Zone.GetEntityPosition(Actor), "Independent movement is not transaction-owned.");
            Assert.AreEqual(2, Units(water)); Assert.AreSame(fire, target.GetEffect<BurningEffect>());
            Assert.False(target.HasEffect<WetEffect>()); Assert.AreEqual(heat, target.GetPart<ThermalPart>().Temperature); NoPaidReceipt();
        }

        [TestCase(false)] [TestCase(true)]
        public void ReplacingThePaidVesselPartRefundsOnlyTheCapturedPart(bool flask)
        {
            var target = Place("MarlbackScrabbler"); var fire = Ignite(target); var water = flask ? Flask(2) : Skin(2);
            var skin = water.GetPart<WaterskinPart>(); var vessel = water.GetPart<LiquidVesselPart>();
            Part original = (Part)skin ?? vessel; Part replacement = null; string command = Select(water, target);
            After(() =>
            {
                Assert.True(water.RemovePart(original));
                replacement = flask ? (Part)new LiquidVesselPart { Capacity = 9, Volume = 7, LiquidId = "acid" }
                    : new WaterskinPart { Capacity = 9, Charges = 7 };
                water.AddPart(replacement);
            }); ResetReceipts();
            Assert.False(Execute(water, command));
            Assert.AreEqual(2, flask ? vessel.Volume : skin.Charges, "Refund the precise vessel that paid.");
            Assert.AreSame(replacement, flask ? (Part)water.GetPart<LiquidVesselPart>() : water.GetPart<WaterskinPart>());
            Assert.AreEqual(7, Units(water), "Never overwrite the independently installed vessel's stock.");
            if (flask) Assert.AreEqual("acid", water.GetPart<LiquidVesselPart>().LiquidId);
            Assert.AreSame(fire, target.GetEffect<BurningEffect>()); Assert.False(target.HasEffect<WetEffect>()); NoPaidReceipt();
        }

        [Test] public void ReplacingTheEffectsManagerPreservesTheIndependentManagerAndItsCondition()
        {
            var target = Place("MarlbackScrabbler"); var fire = Ignite(target); var original = target.GetPart<StatusEffectsPart>();
            var replacement = new StatusEffectsPart(); var acid = new AcidicEffect(.3f); var water = Skin(2);
            string command = Select(water, target);
            After(() => { Assert.True(target.RemovePart(original)); target.AddPart(replacement); Assert.True(target.ApplyEffect(acid)); });
            ResetReceipts(); Assert.False(Execute(water, command)); Assert.AreEqual(2, Units(water));
            Assert.AreSame(replacement, target.GetPart<StatusEffectsPart>()); Assert.AreSame(acid, target.GetEffect<AcidicEffect>());
            Assert.False(target.HasEffect<BurningEffect>(), "Do not graft the detached manager's original fire into the replacement.");
            Assert.False(target.HasEffect<WetEffect>());
            Assert.AreSame(fire, original.GetEffect<BurningEffect>(), "The captured manager receives its own removed record back.");
            Assert.False(original.HasEffect<WetEffect>(), "Remove the operation's Wet from the captured manager, not its replacement."); NoPaidReceipt();
        }

        [Test] public void ReplacingThermalStateKeepsTheReplacementAndRefundsTheCapturedCooling()
        {
            var target = Place("MarlbackScrabbler"); var fire = Ignite(target); var original = target.GetPart<ThermalPart>();
            float temperature = original.Temperature; var replacement = new ThermalPart { Temperature = 137, AmbientTemperature = 21, FlameTemperature = 501 };
            var water = Skin(2); string command = Select(water, target);
            After(() => { Assert.True(target.RemovePart(original)); target.AddPart(replacement); }); ResetReceipts();
            Assert.False(Execute(water, command)); Assert.AreEqual(2, Units(water));
            Assert.AreSame(replacement, target.GetPart<ThermalPart>()); Assert.AreEqual(137, replacement.Temperature);
            Assert.AreEqual(temperature, original.Temperature); Assert.AreSame(fire, target.GetEffect<BurningEffect>());
            Assert.False(target.HasEffect<WetEffect>()); NoPaidReceipt();
        }

        [Test] public void IndependentReignitionIsNotErasedDuplicatedOrCooledByRollback()
        {
            var target = Place("MarlbackScrabbler"); Ignite(target); var water = Skin(2); BurningEffect independent = null;
            string command = Select(water, target);
            After(() =>
            {
                Assert.True(target.RemoveEffect<WetEffect>()); independent = Ignite(target);
                target.GetPart<ThermalPart>().Temperature = 777; // Independent new heat, not the paid cooling.
            }); ResetReceipts();
            Assert.False(Execute(water, command)); Assert.AreEqual(2, Units(water));
            Assert.AreSame(independent, target.GetEffect<BurningEffect>());
            Assert.AreEqual(1, target.GetPart<StatusEffectsPart>().GetAllEffects().OfType<BurningEffect>().Count(),
                "Restoring the old flame alongside an independently lit one would create an ambiguous duplicate.");
            Assert.AreEqual(777, target.GetPart<ThermalPart>().Temperature);
            Assert.False(target.HasEffect<WetEffect>()); NoPaidReceipt();
        }

        [TestCase(false)] [TestCase(true)]
        public void IndependentWetRemovalOrReplacementRefusesThePromisedDrenchWithoutOverwritingIt(bool replace)
        {
            var target = Place("MarlbackScrabbler"); var fire = Ignite(target); var water = Skin(2); WetEffect independent = null;
            string command = Select(water, target);
            After(() =>
            {
                Assert.True(target.RemoveEffect<WetEffect>());
                if (replace) { independent = new WetEffect(.25f); Assert.True(target.ApplyEffect(independent)); }
            }); ResetReceipts();
            Assert.False(Execute(water, command)); Assert.AreEqual(2, Units(water)); Assert.AreSame(fire, target.GetEffect<BurningEffect>());
            Assert.AreSame(independent, target.GetEffect<WetEffect>());
            if (replace) Assert.AreEqual(.25f, independent.Moisture);
            NoPaidReceipt();
        }

        sealed class DousingFinalMutationProbe : Part
        {
            readonly Action mutation;
            bool applied;
            public bool Completed;
            public DousingFinalMutationProbe(Action mutation) { this.mutation = mutation; }
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "AfterInventoryAction" && !applied)
                { applied = true; mutation(); Completed = true; }
                return true; // Deliberately successful: failure must come from final validation.
            }
        }
    }
}
