using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class AcidRinseTests : FiftyWorldFixture
    {
        [SetUp] public void Reveal() { foreach (var cell in Zone.Cells) cell.IsVisible = cell.Explored = true; }
        Entity Friend() { var e = Place("MarlbackScrabbler"); Assert.True(e.GetPart<BrainPart>().SetPartyLeader(Actor)); return e; }
        LiquidCoveredEffect Acid(Entity e, int amount = 21) { var c = new LiquidCoveredEffect("acid", amount); Assert.True(e.ApplyEffect(c)); return c; }
        string Pick(Entity water, Entity target) => Choice(water, "RinseAcid|", a => a.Command.Split('|')[8] == Id(target));
        bool Offered(Entity water) => Actions(water).Any(a => a.Command.StartsWith("RinseAcid|", StringComparison.Ordinal));
        sealed class Hook : Part
        {
            readonly string id; readonly Func<GameEvent, bool> callback;
            public Hook(string id, Func<GameEvent, bool> callback) { this.id = id; this.callback = callback; }
            public override bool HandleEvent(GameEvent e) => e.ID != id || callback(e);
        }
        sealed class ObserveCommitBeforeRinse : IInventoryCommand
        {
            readonly PerformInventoryActionCommand inner;
            readonly Action observer;
            public ObserveCommitBeforeRinse(Entity water, string command, Action observer)
            { inner = new PerformInventoryActionCommand(water, command); this.observer = observer; }
            public string Name => inner.Name;
            public InventoryValidationResult Validate(InventoryContext context) => inner.Validate(context);
            public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
            {
                typeof(InventoryTransaction).GetMethod("AfterCommit", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(transaction, new object[] { observer });
                return inner.Execute(context, transaction);
            }
        }
        [TestCase(false, 1)] [TestCase(false, 20)] [TestCase(false, 21)]
        [TestCase(true, 1)] [TestCase(true, 20)] [TestCase(true, 21)]
        public void ExactAcidIsReducedForTwoWaterAndTargetBecomesWet(bool flask, int amount)
        {
            var target = Friend(); var coat = Acid(target, amount); var water = flask ? Flask(2) : Skin(2);
            int hp = target.GetStatValue("Hitpoints");
            Assert.True(Act(water, Pick(water, target))); Assert.AreEqual(0, Units(water)); Assert.Contains(water, Pack.Objects);
            Assert.AreEqual(hp, target.GetStatValue("Hitpoints")); Assert.AreEqual(1f, target.GetEffect<WetEffect>().Moisture);
            if (amount <= 20) Assert.False(target.HasEffect<LiquidCoveredEffect>());
            else { Assert.AreSame(coat, target.GetEffect<LiquidCoveredEffect>()); Assert.AreEqual(1, coat.Amount); }
            if (flask) Assert.AreEqual("", water.GetPart<LiquidVesselPart>().LiquidId);
        }
        [Test] public void FullWetDoesNotPreventSelfRinseAndOtherConditionsRemain()
        {
            var coat = Acid(Actor); var wet = new WetEffect(1); var poison = new PoisonedEffect(); var burn = new BurningEffect();
            Assert.True(Actor.ApplyEffect(wet)); Assert.True(Actor.ApplyEffect(poison)); Assert.True(Actor.ApplyEffect(burn));
            float heat = Actor.GetPart<ThermalPart>().Temperature; var water = Skin(3);
            Assert.True(Act(water, Pick(water, Actor))); Assert.AreEqual(1, coat.Amount); Assert.AreEqual(1, Units(water));
            Assert.AreSame(wet, Actor.GetEffect<WetEffect>()); Assert.AreSame(poison, Actor.GetEffect<PoisonedEffect>());
            Assert.AreSame(burn, Actor.GetEffect<BurningEffect>()); Assert.AreEqual(heat, Actor.GetPart<ThermalPart>().Temperature);
        }
        [TestCase(0)] [TestCase(1)]
        public void InsufficientWaterOffersNoRinse(int amount) { Acid(Actor); Assert.False(Offered(Skin(amount))); }
        [Test] public void SelfRinseDoesNotRequireSeeingOnesOwnBody()
        {
            var coat = Acid(Actor); var water = Skin(2);
            Actor.GetPart<RenderPart>().Visible = false; Zone.GetEntityCell(Actor).IsVisible = false;
            Assert.True(Act(water, Pick(water, Actor))); Assert.AreEqual(0, Units(water)); Assert.AreEqual(1, coat.Amount);
        }
        [TestCase("oil")] [TestCase("brine")] [TestCase("bog-mire")]
        public void NonWaterNeverAdvertisesRinse(string liquid) { Acid(Actor); var f = Flask(3); f.GetPart<LiquidVesselPart>().LiquidId = liquid; Assert.False(Offered(f)); }
        [TestCase("outsider")] [TestCase("hostile")] [TestCase("hidden")] [TestCase("dead")] [TestCase("far")]
        public void IneligibleTargetsOfferNoRinse(string change)
        {
            var target = Friend(); Acid(target); var water = Skin(2);
            if (change == "outsider") target.GetPart<BrainPart>().SetPartyLeader(null);
            if (change == "hostile") target.GetPart<BrainPart>().SetPersonallyHostile(Actor);
            if (change == "hidden") Zone.GetCell(11, 10).IsVisible = false;
            if (change == "dead") target.GetStat("Hitpoints").BaseValue = 0;
            if (change == "far") Assert.True(Zone.MoveEntity(target, 14, 10));
            Assert.False(Offered(water));
        }
        [TestCase("amount")] [TestCase("kind")] [TestCase("source-id")] [TestCase("replacement")]
        [TestCase("moved")] [TestCase("hostile")] [TestCase("dead")]
        public void RetainedSelectionRejectsChangedAuthority(string change)
        {
            var target = Friend(); var coat = Acid(target); var water = Flask(3); string command = Pick(water, target);
            if (change == "amount") water.GetPart<LiquidVesselPart>().Volume = 2;
            if (change == "kind") water.GetPart<LiquidVesselPart>().LiquidId = "brine";
            if (change == "source-id") water.ID = Guid.NewGuid().ToString("N");
            if (change == "replacement") { Assert.True(target.RemoveEffect<LiquidCoveredEffect>()); coat = Acid(target); }
            if (change == "moved") Assert.True(Zone.MoveEntity(target, 10, 11));
            if (change == "hostile") target.GetPart<BrainPart>().SetPersonallyHostile(Actor);
            if (change == "dead") target.GetStat("Hitpoints").BaseValue = 0;
            int units = Units(water); Assert.False(Act(water, command)); Assert.AreEqual(units, Units(water));
            Assert.AreEqual(21, coat.Amount); Assert.False(target.HasEffect<WetEffect>());
        }
        [Test] public void WetVetoRestoresPaymentWithoutTouchingAcid()
        {
            var coat = Acid(Actor); var water = Skin(2); string command = Pick(water, Actor); int calls = 0;
            Actor.AddPart(new Hook("BeforeApplyEffect", e => { calls++; return false; }));
            Assert.False(Act(water, command)); Assert.AreEqual(1, calls); Assert.AreEqual(2, Units(water));
            Assert.AreSame(coat, Actor.GetEffect<LiquidCoveredEffect>()); Assert.AreEqual(21, coat.Amount); Assert.False(Actor.HasEffect<WetEffect>());
        }
        [Test] public void WetVetoDoesNotDryAnExistingPartialWetEffect()
        {
            Acid(Actor); var wet = new WetEffect(.4f); Assert.True(Actor.ApplyEffect(wet)); var water = Skin(2);
            string command = Pick(water, Actor); int calls = 0;
            Actor.AddPart(new Hook("BeforeApplyEffect", e => { calls++; return false; }));
            Assert.False(Act(water, command)); Assert.AreEqual(1, calls); Assert.AreEqual(.4f, wet.Moisture);
            Assert.AreEqual(2, Units(water)); Assert.AreSame(wet, Actor.GetEffect<WetEffect>());
        }
        [Test] public void BeforeActionVetoSpendsNothing()
        {
            var coat = Acid(Actor); var water = Skin(2); string command = Pick(water, Actor); int calls = 0;
            Actor.AddPart(new Hook("BeforeInventoryAction", e => { calls++; return false; }));
            Assert.False(Act(water, command)); Assert.AreEqual(1, calls); Assert.AreEqual(2, Units(water));
            Assert.AreEqual(21, coat.Amount); Assert.False(Actor.HasEffect<WetEffect>());
        }
        [TestCase(false)] [TestCase(true)]
        public void FailedFinalCommitPreservesIndependentInjuryAndWaterLoss(bool flask)
        {
            var coat = Acid(Actor); var water = flask ? Flask(4) : Skin(3); int before = Units(water), hp = Actor.GetStatValue("Hitpoints");
            string command = Pick(water, Actor); int calls = 0;
            Actor.AddPart(new Hook("AfterInventoryAction", e => {
                calls++; Actor.GetStat("Hitpoints").BaseValue -= 3;
                if (flask) water.GetPart<LiquidVesselPart>().Volume--; else water.GetPart<WaterskinPart>().Charges--;
                return true;
            }));
            Assert.False(Act(water, command)); Assert.AreEqual(1, calls); Assert.AreEqual(hp - 3, Actor.GetStatValue("Hitpoints"));
            Assert.AreEqual(before - 1, Units(water)); Assert.AreEqual(21, coat.Amount); Assert.False(Actor.HasEffect<WetEffect>());
        }
        [TestCase(false)] [TestCase(true)]
        public void FailedRinseDoesNotRefundAVesselPartRehomedToAnotherItem(bool flask)
        {
            var coat = Acid(Actor); var water = flask ? Flask(4, "water", 10) : Skin(4, 10);
            var foreign = flask ? Flask(1, "water", 10) : Skin(1, 10);
            Part captured = flask ? (Part)water.GetPart<LiquidVesselPart>() : water.GetPart<WaterskinPart>();
            Part displaced = flask ? (Part)foreign.GetPart<LiquidVesselPart>() : foreign.GetPart<WaterskinPart>();
            string command = Pick(water, Actor); int calls = 0;
            Actor.AddPart(new Hook("AfterInventoryAction", e => {
                calls++; Assert.True(water.RemovePart(captured)); Assert.True(foreign.RemovePart(displaced));
                foreign.AddPart(captured);
                if (flask) { var vessel = (LiquidVesselPart)captured; vessel.Volume = 3; vessel.LiquidId = "water"; }
                else ((WaterskinPart)captured).Charges = 3;
                return true;
            }));
            Assert.False(Act(water, command)); Assert.AreEqual(1, calls);
            Assert.AreSame(foreign, captured.ParentEntity); Assert.Contains(captured, foreign.Parts);
            Assert.False(water.Parts.Contains(captured)); Assert.AreEqual(3, Units(foreign), "Rollback cannot refund another item's independently acquired water.");
            Assert.AreEqual(21, coat.Amount); Assert.False(Actor.HasEffect<WetEffect>());
        }
        [Test] public void FailedRinseDoesNotDryOrExpireAWetEffectRehomedToAnotherActor()
        {
            var foreign = Friend(); var coat = Acid(Actor);
            // An expired but still attached Wet is a supported input that the rinse refreshes.
            var wet = new WetEffect(.4f) { Duration = 0 }; Assert.True(Actor.ApplyEffect(wet));
            var water = Skin(2); string command = Pick(water, Actor); int calls = 0;
            Actor.AddPart(new Hook("AfterInventoryAction", e => {
                calls++; Assert.True(Actor.GetPart<StatusEffectsPart>().RemoveEffect(wet));
                Assert.True(foreign.ApplyEffect(wet)); wet.Moisture = .8f; wet.Duration = Effect.DURATION_INDEFINITE;
                return true;
            }));
            Assert.False(Act(water, command)); Assert.AreEqual(1, calls); Assert.AreEqual(2, Units(water));
            Assert.AreSame(foreign, wet.Owner); Assert.AreSame(wet, foreign.GetEffect<WetEffect>());
            Assert.AreEqual(.8f, wet.Moisture, .00001f); Assert.AreEqual(Effect.DURATION_INDEFINITE, wet.Duration);
            Assert.False(Actor.HasEffect<WetEffect>()); Assert.AreEqual(21, coat.Amount);
        }
        [Test] public void EarlierCommitObserverCannotMakeRinseReduceAnotherActorsAcidCoat()
        {
            var foreign = Friend(); var coat = Acid(Actor); var water = Skin(2);
            string command = Pick(water, Actor); int calls = 0;
            var result = InventorySystem.ExecuteCommand(new ObserveCommitBeforeRinse(water, command, () => {
                calls++; Assert.True(Actor.GetPart<StatusEffectsPart>().RemoveEffect(coat));
                Assert.True(foreign.ApplyEffect(coat));
            }), Actor, Zone);
            Assert.True(result.Success); Assert.AreEqual(1, calls); Assert.AreEqual(0, Units(water));
            Assert.AreSame(foreign, coat.Owner); Assert.AreSame(coat, foreign.GetEffect<LiquidCoveredEffect>());
            Assert.AreEqual(21, coat.Amount, "A committed rinse must not follow its former coating onto an unrelated recipient.");
            Assert.False(Actor.HasEffect<LiquidCoveredEffect>()); Assert.AreEqual(1f, Actor.GetEffect<WetEffect>().Moisture);
        }
        [TestCase("replacement")] [TestCase("move")] [TestCase("hostile")] [TestCase("death")]
        public void FinalCallbackChangesRejectWithoutCleaningOrUndoingTheIndependentChange(string change)
        {
            var target = Friend(); var coat = Acid(target); var water = Skin(2); string command = Pick(water, target); int calls = 0;
            Actor.AddPart(new Hook("AfterInventoryAction", e => {
                calls++;
                if (change == "replacement") { Assert.True(target.RemoveEffect<LiquidCoveredEffect>()); coat = Acid(target); }
                if (change == "move") Assert.True(Zone.MoveEntity(target, 10, 11));
                if (change == "hostile") target.GetPart<BrainPart>().SetPersonallyHostile(Actor);
                if (change == "death") target.GetStat("Hitpoints").BaseValue = 0;
                return true;
            }));
            Assert.False(Act(water, command)); Assert.AreEqual(1, calls); Assert.AreEqual(2, Units(water));
            Assert.AreSame(coat, target.GetEffect<LiquidCoveredEffect>()); Assert.AreEqual(21, coat.Amount); Assert.False(target.HasEffect<WetEffect>());
            if (change == "death") Assert.AreEqual(0, target.GetStatValue("Hitpoints"));
            if (change == "move") Assert.AreEqual((10, 11), Zone.GetEntityPosition(target));
        }
        [Test] public void ReentrantRinseCannotSpendTheSameActionTwice()
        {
            var coat = Acid(Actor, 41); var water = Flask(6); string command = Pick(water, Actor); bool nested = true; int calls = 0;
            Actor.AddPart(new Hook("AfterInventoryAction", e => { if (++calls == 1) nested = Act(water, Pick(water, Actor)); return true; }));
            Assert.True(Act(water, command)); Assert.False(nested); Assert.AreEqual(4, Units(water)); Assert.AreEqual(21, coat.Amount);
        }
        [Test] public void RinseStateAndRemainingWaterRoundTripThroughRealSaveBodies()
        {
            Acid(Actor, 21); var water = Flask(4); Assert.True(Act(water, Pick(water, Actor)));
            using (var stream = new MemoryStream()) {
                var writer = new SaveWriter(stream); writer.WriteEntityReference(Actor); writer.WriteQueuedEntityBodies(); stream.Position = 0;
                var reader = new SaveReader(stream, Factory); var loaded = reader.ReadEntityReference(); reader.ReadEntityBodies();
                Assert.AreEqual(1, loaded.GetEffect<LiquidCoveredEffect>().Amount); Assert.AreEqual(1f, loaded.GetEffect<WetEffect>().Moisture);
                var saved = loaded.GetPart<InventoryPart>().Objects.Single(e => e.ID == water.ID).GetPart<LiquidVesselPart>();
                Assert.AreEqual(2, saved.Volume); Assert.AreEqual("water", saved.LiquidId);
            }
        }
    }
}
