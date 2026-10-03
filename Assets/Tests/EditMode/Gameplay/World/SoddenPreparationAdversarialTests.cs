using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Saved owner authority, visibility, callback substitutions, money boundaries,
    /// alias corruption and native transaction re-entry for the bounded preparation service.</summary>
    public sealed class SoddenPreparationAdversarialTests : SoddenPreparationFixture
    {
        [TestCase("dead")][TestCase("hostile")][TestCase("blocked")][TestCase("distant")][TestCase("missing")][TestCase("hidden")]
        public void Adversarial_UnavailableActualWorkerCannotBeReplacedByAnotherNearbyCutter(string fault)
        {
            Ingredients();
            switch (fault)
            {
                case "dead": Worker.GetStat("Hitpoints").BaseValue = 0; break;
                case "hostile": Worker.GetPart<BrainPart>().SetPersonallyHostile(Actor, false); break;
                case "blocked": Worker.GetPart<StatusEffectsPart>().ApplyEffect(new StunnedEffect { Duration = 2 }); break;
                case "distant": Assert.True(Zone.MoveEntity(Worker, 10, 10)); break;
                case "missing": Assert.True(Zone.RemoveEntity(Worker)); break;
                case "hidden": Worker.GetPart<RenderPart>().Visible = false; break;
            }
            var other = Make("PeatCutter", false); other.SetTag("Creature"); other.AddPart(new BrainPart()); other.AddPart(new InventoryPart());
            other.Statistics["Hitpoints"] = new Stat { Owner = other, BaseValue = 20, Max = 20 }; Assert.True(Zone.AddEntity(other, 5, 5));
            Assert.False(Prepare()); Assert.AreEqual(1, Units("SumpsievePad")); Assert.AreEqual(0, Units("SoddenFieldDressing")); Assert.AreEqual(10, TradeSystem.GetDrams(Actor));
        }
        [TestCase("moved")][TestCase("invisible")][TestCase("unseen")][TestCase("unexplored")][TestCase("replaced-part")][TestCase("duplicate-part")][TestCase("missing-worker-reference")]
        public void Adversarial_StaleBenchOrHiddenOwnerCannotOfferOrPerformWork(string fault)
        {
            Ingredients();
            switch (fault)
            {
                case "moved": Assert.True(Zone.MoveEntity(Bench, 5, 5)); break;
                case "invisible": Bench.GetPart<RenderPart>().Visible = false; break;
                case "unseen": Zone.GetEntityCell(Bench).IsVisible = false; break;
                case "unexplored": Zone.GetEntityCell(Bench).Explored = false; break;
                case "replaced-part": Bench.RemovePart(Service); Bench.AddPart(new SoddenPreparationPart()); break;
                case "duplicate-part": Bench.AddPart(new SoddenPreparationPart()); break;
                case "missing-worker-reference": Service.Worker = null; break;
            }
            Assert.False(WorldInteractionSystem.GatherActions(Bench, Actor).Any(a => a.Command == SoddenPreparationPart.PrepareCommand));
            Assert.False(Prepare()); Assert.AreEqual(1, Units("KnotflaxCord")); Assert.AreEqual(10, TradeSystem.GetDrams(Actor));
        }
        [TestCase("equipped")][TestCase("backlink")][TestCase("duplicate-id")][TestCase("zero")][TestCase("oversized")]
        public void Adversarial_CorruptCarriedIngredientCannotPay(string fault)
        {
            Ingredients(); var pad = Pack.Objects.Single(e => e.BlueprintName == "SumpsievePad");
            switch (fault)
            {
                case "equipped": Pack.EquippedItems["Hand"] = pad; break;
                case "backlink": pad.GetPart<PhysicsPart>().InInventory = Worker; break;
                case "duplicate-id": Supply("Unrelated").ID = pad.ID; break;
                case "zero": pad.GetPart<StackerPart>().StackCount = 0; break;
                case "oversized": pad.GetPart<StackerPart>().StackCount = 21; break;
            }
            Assert.False(Prepare()); Assert.AreEqual(1, Units("KnotflaxCord")); Assert.AreEqual(0, Units("SoddenFieldDressing")); Assert.AreEqual(10, TradeSystem.GetDrams(Actor));
        }
        [TestCase("worker-moved")][TestCase("station-moved")][TestCase("input-count")][TestCase("input-part")][TestCase("output-owned")][TestCase("output-count")][TestCase("output-id")]
        public void Adversarial_FactoryCallbackCannotChangeQuotedOwnerInputOrResult(string fault)
        {
            Ingredients(); var pad = Pack.Objects.Single(e => e.BlueprintName == "SumpsievePad");
            Probe(output =>
            {
                switch (fault)
                {
                    case "worker-moved": Zone.MoveEntity(Worker, 10, 10); break;
                    case "station-moved": Zone.MoveEntity(Bench, 5, 5); break;
                    case "input-count": pad.GetPart<StackerPart>().StackCount = 2; break;
                    case "input-part": pad.RemovePart(pad.GetPart<PhysicsPart>()); pad.AddPart(new PhysicsPart { Takeable = true, InInventory = Actor }); break;
                    case "output-owned": output.GetPart<PhysicsPart>().InInventory = Worker; break;
                    case "output-count": output.GetPart<StackerPart>().StackCount = 2; break;
                    case "output-id": output.ID = Worker.ID; break;
                }
            });
            Assert.False(Prepare()); Assert.AreEqual(1, Units("KnotflaxCord")); Assert.AreEqual(0, Units("SoddenFieldDressing")); Assert.AreEqual(10, TradeSystem.GetDrams(Actor)); Assert.AreEqual(7, TradeSystem.GetDrams(Worker));
        }
        [Test] public void Adversarial_PoorPlayerFullWorkerAndOverweightOutputAllRefuseWithoutPartialPayment()
        {
            Ingredients(); TradeSystem.SetDrams(Actor, 1); Assert.False(Prepare()); TradeSystem.SetDrams(Actor, 10);
            TradeSystem.SetDrams(Worker, int.MaxValue); Assert.False(Prepare()); TradeSystem.SetDrams(Worker, 7);
            Factory.Blueprints["SoddenFieldDressing"].Parts["Physics"]["Weight"] = "100"; Pack.MaxWeight = 20; Assert.False(Prepare());
            Assert.AreEqual(1, Units("SumpsievePad")); Assert.AreEqual(1, Units("KnotflaxCord")); Assert.AreEqual(10, TradeSystem.GetDrams(Actor)); Assert.AreEqual(7, TradeSystem.GetDrams(Worker));
        }
        [Test] public void Adversarial_ReentrantPreparationCannotSpendASecondSetOfIngredients()
        {
            Ingredients(2); bool? nested = null; Probe(_ => { if (nested == null) { nested = false; nested = Prepare(); } });
            Assert.True(Prepare()); Assert.AreEqual(false, nested); Assert.AreEqual(1, Units("SumpsievePad")); Assert.AreEqual(1, Units("SoddenFieldDressing")); Assert.AreEqual(8, TradeSystem.GetDrams(Actor));
        }
        [TestCase("healthy")][TestCase("gas")][TestCase("fungal")][TestCase("burning")]
        public void Adversarial_DressingDoesNotSpendOrTreatUnrelatedConditions(string condition)
        {
            var dressing = Supply("SoddenFieldDressing");
            if (condition == "gas") Status.ApplyEffect(new PoisonedByGasEffect { Duration = 5 });
            if (condition == "fungal") Status.ApplyEffect(new FungalInfectionEffect { Duration = 20 });
            if (condition == "burning") Status.ApplyEffect(new BurningEffect { Duration = 5 });
            int count = Status.EffectCount; Assert.False(Act(dressing, SoddenDressingPart.ApplyCommand));
            Assert.AreEqual(1, Units("SoddenFieldDressing")); Assert.AreEqual(count, Status.EffectCount); Assert.AreEqual(20, Actor.GetStatValue("Hitpoints"));
        }
        [TestCase("ground")][TestCase("equipped")][TestCase("blocked")][TestCase("duplicate-part")]
        public void Adversarial_DressingNeedsOneGenuineCarriedOwnerAndAvailableActor(string fault)
        {
            var dressing = Supply("SoddenFieldDressing"); Status.ApplyEffect(new PoisonedEffect());
            switch (fault)
            {
                case "ground": Pack.RemoveObject(dressing); Zone.AddEntity(dressing, 4, 4); break;
                case "equipped": Pack.EquippedItems["Hand"] = dressing; break;
                case "blocked": Status.ApplyEffect(new StunnedEffect { Duration = 2 }); break;
                case "duplicate-part": dressing.AddPart(new SoddenDressingPart()); break;
            }
            Assert.False(Act(dressing, SoddenDressingPart.ApplyCommand)); Assert.True(Status.HasEffect<PoisonedEffect>()); Assert.AreEqual(1, dressing.GetPart<StackerPart>().StackCount);
        }
        [TestCase(false)][TestCase(true)]
        public void Adversarial_OuterRollbackRestoresPoisonAuraAlongsideTheActualPoisonEffect(bool rollback)
        {
            // Hypothesis: the shared inventory undo restores the effect list but does not
            // restart its native aura after RemoveEffect emitted AuraStop.
            AsciiFxBus.Clear(); var dressing = Supply("SoddenFieldDressing");
            Assert.True(Status.ApplyEffect(new PoisonedEffect(), zone: Zone));
            Assert.True(AsciiFxBus.Drain().Any(r => r.Type == AsciiFxRequestType.AuraStart));
            if (rollback) Actor.AddPart(new FailAfter());
            Assert.AreEqual(!rollback, Act(dressing, SoddenDressingPart.ApplyCommand));
            var requests = AsciiFxBus.Drain().Where(r => r.Anchor == Actor && r.Theme == AsciiFxTheme.Poison).ToArray();
            Assert.AreEqual(rollback, Status.HasEffect<PoisonedEffect>());
            CollectionAssert.AreEqual(rollback ? new[] { AsciiFxRequestType.AuraStop, AsciiFxRequestType.AuraStart }
                : new[] { AsciiFxRequestType.AuraStop }, requests.Select(r => r.Type).ToArray());
        }
        [Test] public void Adversarial_CancelledNativeActionAndBareEventNeverCreateFreeOutput()
        {
            Ingredients(); var cancel = new CancelBefore(); Actor.AddPart(cancel); Assert.False(Prepare()); Actor.RemovePart(cancel);
            var e = GameEvent.New("InventoryAction"); e.SetParameter("Actor", Actor); e.SetParameter("Zone", Zone); e.SetParameter("Command", SoddenPreparationPart.PrepareCommand);
            Assert.True(Bench.FireEvent(e)); Assert.False(e.Handled); e.Release(); Assert.AreEqual(1, Units("SumpsievePad")); Assert.AreEqual(0, Units("SoddenFieldDressing"));
        }
    }
}
