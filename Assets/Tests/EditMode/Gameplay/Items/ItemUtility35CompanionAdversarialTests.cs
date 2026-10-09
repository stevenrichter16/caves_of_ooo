using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ItemUtility35CompanionAdversarialTests : ItemUtility35CompanionFixture
    {
        [TestCase("actor")] [TestCase("target")] [TestCase("quantity")] [TestCase("owner")]
        [TestCase("duplicate-id")] [TestCase("party")] [TestCase("dead")] [TestCase("payload")]
        public void StaleTreatmentCannotSpendOrHeal(string change)
        {
            var target = Companion(); target.GetStat("Hitpoints").BaseValue = 1; var item = Carry("HealingTonic");
            item.GetPart<StackerPart>().StackCount = 3; string command = SelectCare(item, target);
            if (change == "actor") Zone.MoveEntity(Actor, 9, 10);
            if (change == "target") Zone.MoveEntity(target, 10, 11);
            if (change == "quantity") item.GetPart<StackerPart>().StackCount = 2;
            if (change == "owner") item.GetPart<PhysicsPart>().InInventory = target;
            if (change == "duplicate-id") Place("MarlbackScrabbler", 10, 11).ID = target.ID;
            if (change == "party") target.GetPart<BrainPart>().SetPartyLeader(null);
            if (change == "dead") target.GetStat("Hitpoints").BaseValue = 0;
            if (change == "payload") item.GetPart<TonicPart>().Healing = "1d2";
            int count = Amount(item), hp = target.GetStatValue("Hitpoints");
            Assert.False(Act(item, command)); Assert.AreEqual(count, Amount(item)); Assert.AreEqual(hp, target.GetStatValue("Hitpoints"));
        }

        [TestCase("Stunned")] [TestCase("Frozen")]
        public void IncapacitatedHelperCannotTreat(string condition)
        {
            var target = Companion(); Need(target, "Antidote"); var item = Carry("Antidote"); string command = SelectCare(item, target);
            Actor.ApplyEffect(condition == "Stunned" ? (Effect)new StunnedEffect() : new FrozenEffect());
            Assert.False(Act(item, command)); Assert.True(target.HasEffect<PoisonedEffect>()); Assert.Contains(item, Pack.Objects);
        }

        [TestCase("HealingTonic")] [TestCase("AbsentmintLeaf")] [TestCase("SoddenFieldDressing")] [TestCase("BurnSalve")]
        public void OuterFailureRestoresRecipientAndExactSupply(string blueprint)
        {
            var target = Companion(); var effect = Need(target, blueprint); var item = Carry(blueprint);
            int hp = target.GetStatValue("Hitpoints"), dv = target.GetStatValue("DV"), agi = target.GetStatValue("Agility");
            float heat = target.GetPart<ThermalPart>().Temperature; string command = SelectCare(item, target); FailAfter();
            Assert.False(Act(item, command)); Assert.Contains(item, Pack.Objects); Assert.AreEqual(hp, target.GetStatValue("Hitpoints"));
            Assert.AreEqual(dv, target.GetStatValue("DV")); Assert.AreEqual(agi, target.GetStatValue("Agility"));
            Assert.AreEqual(heat, target.GetPart<ThermalPart>().Temperature);
            if (effect != null) Assert.Contains(effect, target.GetPart<StatusEffectsPart>().GetAllEffects().ToList());
        }

        [TestCase(false)] [TestCase(true)]
        public void FailedMealRestoresPreviousMealIdentityAndBonuses(bool previousMeal)
        {
            var target = Companion(); target.GetStat("Hitpoints").BaseValue = 1;
            var previous = previousMeal ? new PreparedMealEffect("ColdResistance", 20, 55) : null;
            if (previous != null) target.ApplyEffect(previous);
            int heat = target.GetStatValue("HeatResistance"), cold = target.GetStatValue("ColdResistance");
            var item = Carry("ToastedEmberwheat"); string command = SelectCare(item, target, true); FailAfter();
            Assert.False(Act(item, command)); Assert.Contains(item, Pack.Objects); Assert.AreEqual(1, target.GetStatValue("Hitpoints"));
            Assert.AreEqual(heat, target.GetStatValue("HeatResistance")); Assert.AreEqual(cold, target.GetStatValue("ColdResistance"));
            Assert.AreSame(previous, target.GetEffect<PreparedMealEffect>());
            if (previous != null) { Assert.AreEqual("ColdResistance", previous.StatName); Assert.AreEqual(55, previous.Duration); }
        }

        [Test] public void MealVetoDoesNotChargeAFullyHealedCompanion()
        {
            var target = Companion(); var item = Carry("RoastedHearthbulb"); string command = SelectCare(item, target, true);
            target.AddPart(new Hook("BeforeApplyEffect", e => !(e.GetParameter<Effect>("Effect") is PreparedMealEffect)));
            Assert.False(Act(item, command)); Assert.Contains(item, Pack.Objects); Assert.False(target.HasEffect<PreparedMealEffect>());
        }

        [TestCase("move")] [TestCase("party")] [TestCase("hostile")]
        public void LastMomentRecipientChangesAreRevalidatedBeforePaymentCommits(string change)
        {
            var target = Companion(); target.GetStat("Hitpoints").BaseValue = 1; var item = Carry("HealingTonic");
            string command = SelectCare(item, target);
            Actor.AddPart(new Hook("AfterInventoryAction", e =>
            {
                if (change == "move") Zone.MoveEntity(target, 12, 10);
                if (change == "party") target.GetPart<BrainPart>().SetPartyLeader(null);
                if (change == "hostile") target.GetPart<BrainPart>().SetPersonallyHostile(Actor);
                return true;
            }));
            Assert.False(Act(item, command)); Assert.Contains(item, Pack.Objects); Assert.AreEqual(1, target.GetStatValue("Hitpoints"));
            if (change == "move") Assert.AreEqual(12, Zone.GetEntityCell(target).X);
        }

        [Test] public void IndependentConditionAndStatDamageSurviveTreatmentRollback()
        {
            var target = Companion(); var confusion = Need(target, "AbsentmintLeaf"); int dv = target.GetStatValue("DV");
            int hp = target.GetStatValue("Hitpoints"); var item = Carry("AbsentmintLeaf"); string command = SelectCare(item, target);
            var hobble = new HobbledEffect();
            Actor.AddPart(new Hook("AfterInventoryAction", e =>
            {
                target.ApplyEffect(hobble); target.GetStat("Hitpoints").BaseValue -= 1;
                throw new InvalidOperationException("independent changes then abort");
            }));
            Assert.False(Act(item, command)); Assert.AreSame(confusion, target.GetEffect<ConfusedEffect>());
            Assert.AreSame(hobble, target.GetEffect<HobbledEffect>()); Assert.Less(target.GetStatValue("DV"), dv);
            Assert.AreEqual(hp - 1, target.GetStatValue("Hitpoints")); Assert.Contains(item, Pack.Objects);
        }

        [Test] public void PanaceaRollbackRestoresMaterialStatsAndPacingGoalBookkeeping()
        {
            var target = Companion(); var witnessed = new WitnessedEffect(); var charred = new CharredEffect();
            target.ApplyEffect(witnessed); target.ApplyEffect(charred); target.ApplyEffect(new ConfusedEffect());
            var brain = target.GetPart<BrainPart>(); var goals = brain.GetGoalsSnapshot().ToArray(); Assert.IsNotEmpty(goals);
            float combustion = target.GetPart<MaterialPart>().Combustibility; int dv = target.GetStatValue("DV");
            var item = Carry("Panacea"); string command = SelectCare(item, target); FailAfter();
            Assert.False(Act(item, command)); Assert.AreSame(witnessed, target.GetEffect<WitnessedEffect>());
            Assert.AreSame(charred, target.GetEffect<CharredEffect>()); Assert.AreEqual(combustion, target.GetPart<MaterialPart>().Combustibility);
            Assert.AreEqual(dv, target.GetStatValue("DV")); CollectionAssert.AreEqual(goals, brain.GetGoalsSnapshot());
            target.GetPart<StatusEffectsPart>().RemoveEffect(witnessed);
            Assert.False(brain.GetGoalsSnapshot().Contains(goals.Last()), "Restored effect must still own its restored goal.");
        }

        [Test] public void RemovalObserverExceptionCannotLeaveHalfCuredStats()
        {
            var target = Companion(); var confusion = Need(target, "AbsentmintLeaf"); int dv = target.GetStatValue("DV");
            var item = Carry("AbsentmintLeaf"); string command = SelectCare(item, target);
            target.AddPart(new Hook("EffectRemoved", e => throw new InvalidOperationException("removal observer")));
            Assert.False(Act(item, command)); Assert.AreSame(confusion, target.GetEffect<ConfusedEffect>());
            Assert.AreEqual(dv, target.GetStatValue("DV")); Assert.Contains(item, Pack.Objects);
        }

        [Test] public void ReentrantCareCannotUseTheSameSupplyOrRecipientTwice()
        {
            var target = Companion(); target.GetStat("Hitpoints").BaseValue = 1; var item = Carry("KnitmossPad");
            item.GetPart<StackerPart>().StackCount = 3; string command = SelectCare(item, target); bool nested = true;
            Actor.AddPart(new Hook("AfterInventoryAction", e => { nested = Act(item, command); return true; }));
            Assert.True(Act(item, command)); Assert.False(nested); Assert.AreEqual(2, Amount(item));
        }

        [TestCase(false)] [TestCase(true)]
        public void LifecycleObserverChangesSurviveFailedCureOrMeal(bool meal)
        {
            var target = Companion(); Effect condition = meal ? null : Need(target, "AbsentmintLeaf");
            int dv = target.GetStatValue("DV"), hp = target.GetStatValue("Hitpoints");
            var item = Carry(meal ? "RoastedMushroom" : "AbsentmintLeaf"); string command = SelectCare(item, target, meal);
            var hobble = new HobbledEffect();
            target.AddPart(new Hook(meal ? "EffectApplied" : "EffectRemoved", e =>
            {
                var changed = e.GetParameter<Effect>("Effect");
                if (changed == hobble) return true;
                target.ApplyEffect(hobble); target.GetStat("Hitpoints").BaseValue -= 1;
                throw new InvalidOperationException("lifecycle observer changed independent state");
            }));
            Assert.False(Act(item, command)); Assert.Contains(item, Pack.Objects);
            Assert.AreSame(hobble, target.GetEffect<HobbledEffect>()); Assert.AreEqual(hp - 1, target.GetStatValue("Hitpoints"));
            Assert.Less(target.GetStatValue("DV"), dv); Assert.False(target.HasEffect<PreparedMealEffect>());
            Assert.AreEqual(0, target.GetStatValue("AcidResistance"));
            if (!meal) Assert.AreSame(condition, target.GetEffect<ConfusedEffect>());
        }

        [Test] public void IntrinsicRemovalExceptionStillRecordsAndRestoresPartialBookkeeping()
        {
            var target = Companion(); var condition = new ThrowingRemoval(); target.ApplyEffect(condition);
            int dv = target.GetStatValue("DV"); var item = Carry("Panacea"); string command = SelectCare(item, target);
            Assert.False(Act(item, command)); Assert.AreEqual(dv, target.GetStatValue("DV"));
            Assert.Contains(condition, target.GetPart<StatusEffectsPart>().GetAllEffects().ToList()); Assert.Contains(item, Pack.Objects);
        }

        [TestCase(false)] [TestCase(true)]
        public void FinalCommitRejectsAReplacedRecipientStatusManager(bool replace)
        {
            var target = Companion(); Need(target, "Antidote"); var manager = target.GetPart<StatusEffectsPart>();
            var replacement = new StatusEffectsPart(); var item = Carry("Antidote"); string command = SelectCare(item, target);
            Actor.AddPart(new Hook("AfterInventoryAction", e =>
            {
                if (replace) { target.RemovePart(manager); target.AddPart(replacement); }
                return true;
            }));
            Assert.AreEqual(!replace, Act(item, command)); Assert.AreEqual(replace, Pack.Objects.Contains(item));
            Assert.AreSame(replace ? replacement : manager, target.GetPart<StatusEffectsPart>());
        }

        [TestCase(false)] [TestCase(true)]
        public void FinalCommitRejectsAReplacedRecipientHealthRecord(bool replace)
        {
            var target = Companion(); target.GetStat("Hitpoints").BaseValue = 1;
            var original = target.GetStat("Hitpoints"); var replacement = new Stat(original) { Owner = target, BaseValue = 3 };
            var item = Carry("HealingTonic"); string command = SelectCare(item, target);
            Actor.AddPart(new Hook("AfterInventoryAction", e => { if (replace) target.Statistics["Hitpoints"] = replacement; return true; }));
            Assert.AreEqual(!replace, Act(item, command)); Assert.AreEqual(replace, Pack.Objects.Contains(item));
            Assert.AreSame(replace ? replacement : original, target.GetStat("Hitpoints"));
            if (replace) Assert.AreEqual(3, target.GetStatValue("Hitpoints"));
        }

        [TestCase(false, false)] [TestCase(false, true)]
        [TestCase(true, false)] [TestCase(true, true)]
        public void PreApplyMealChangesCannotRedirectThePreparationReceipt(bool priorMeal, bool changes)
        {
            var target = Companion();
            if (priorMeal) target.ApplyEffect(new PreparedMealEffect("ColdResistance", 20));
            int acid = target.GetStatValue("AcidResistance"), heat = target.GetStatValue("HeatResistance");
            var item = Carry("ToastedEmberwheat"); string command = SelectCare(item, target, true); bool inserted = false;
            target.AddPart(new Hook("BeforeApplyEffect", e =>
            {
                if (changes && !inserted && e.GetParameter<Effect>("Effect") is PreparedMealEffect)
                { inserted = true; target.ApplyEffect(new PreparedMealEffect("AcidResistance", 20)); }
                return true;
            }));
            Assert.AreEqual(!changes, Act(item, command)); Assert.AreEqual(changes, Pack.Objects.Contains(item));
            Assert.AreEqual(changes ? "AcidResistance" : "HeatResistance", target.GetEffect<PreparedMealEffect>().StatName);
            Assert.AreEqual(heat + (changes ? 0 : 20), target.GetStatValue("HeatResistance"));
            Assert.AreEqual(acid + (changes ? 20 : 0), target.GetStatValue("AcidResistance"));
        }

        [TestCase("Antidote")] [TestCase("SumpsievePad")]
        public void ExpiredGasPoisonIsNotAReasonToSpendCare(string blueprint)
        {
            var target = Companion(); target.ApplyEffect(new PoisonedByGasEffect { Duration = 0 }); var item = Carry(blueprint);
            Assert.False(Actions(item).Any(CareAction)); Assert.Contains(item, Pack.Objects);
        }

        [TestCase(false, false)] [TestCase(false, true)]
        [TestCase(true, false)] [TestCase(true, true)]
        public void IndependentLaterMealKeepsItsOwnPreparationWhenCareRollsBack(bool priorMeal, bool replaces)
        {
            var target = Companion(); int heat = target.GetStatValue("HeatResistance"), acid = target.GetStatValue("AcidResistance");
            int cold = target.GetStatValue("ColdResistance");
            var previous = priorMeal ? new PreparedMealEffect("ColdResistance", 20) : null;
            if (previous != null) target.ApplyEffect(previous);
            var item = Carry("ToastedEmberwheat"); string command = SelectCare(item, target, true);
            Actor.AddPart(new Hook("AfterInventoryAction", e =>
            {
                if (replaces) target.ApplyEffect(new PreparedMealEffect("AcidResistance", 20));
                throw new InvalidOperationException("outer observer stops the shared meal");
            }));
            Assert.False(Act(item, command)); Assert.Contains(item, Pack.Objects);
            Assert.AreEqual(heat, target.GetStatValue("HeatResistance"));
            Assert.AreEqual(acid + (replaces ? 20 : 0), target.GetStatValue("AcidResistance"));
            Assert.AreEqual(cold + (!replaces && priorMeal ? 20 : 0), target.GetStatValue("ColdResistance"));
            if (replaces) Assert.AreEqual("AcidResistance", target.GetEffect<PreparedMealEffect>().StatName);
            else Assert.AreSame(previous, target.GetEffect<PreparedMealEffect>());
        }

        [Test] public void PreApplyObserverCannotReplaceTheSelectedMealPayload()
        {
            var target = Companion(); var item = Carry("RoastedHearthbulb"); int heat = target.GetStatValue("HeatResistance");
            string command = SelectCare(item, target, true);
            target.AddPart(new Hook("BeforeApplyEffect", e =>
            {
                if (e.GetParameter<Effect>("Effect") is PreparedMealEffect meal) meal.StatName = "HeatResistance";
                return true;
            }));
            Assert.False(Act(item, command)); Assert.Contains(item, Pack.Objects); Assert.False(target.HasEffect<PreparedMealEffect>());
            Assert.AreEqual(heat, target.GetStatValue("HeatResistance"));
        }

        sealed class ThrowingRemoval : Effect
        {
            public override string DisplayName => "receipt test ailment";
            public ThrowingRemoval() { Duration = 20; }
            public override int GetEffectType() => TYPE_NEGATIVE;
            public override void OnApply(Entity target) => target.GetStat("DV").Penalty += 3;
            public override void OnRemove(Entity target)
            { target.GetStat("DV").Penalty -= 3; throw new InvalidOperationException("partial intrinsic removal"); }
        }
    }
}
