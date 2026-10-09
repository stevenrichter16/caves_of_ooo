using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public abstract class ItemUtility35CompanionFixture : FiftyWorldFixture
    {
        protected Entity Companion(int x = 11, int y = 10)
        {
            var target = Place("MarlbackScrabbler", x, y);
            Assert.True(target.GetPart<BrainPart>().SetPartyLeader(Actor));
            target.GetStat("Hitpoints").BaseValue = target.GetStat("Hitpoints").Max;
            return target;
        }
        protected string SelectCare(Entity item, Entity target, bool meal = false)
            => Choice(item, meal ? "ShareMeal|" : "TreatCompanion|", a => a.Command.Split('|')[4] == Id(target));
        protected static bool CareAction(InventoryAction a) => a.Command.StartsWith("TreatCompanion|", StringComparison.Ordinal)
            || a.Command.StartsWith("ShareMeal|", StringComparison.Ordinal);
        protected static int Amount(Entity item) => item.GetPart<StackerPart>()?.StackCount ?? 1;
        protected static Effect Need(Entity target, string item)
        {
            Effect effect = null;
            switch (item)
            {
                case "HealingTonic": case "KnitmossPad": target.GetStat("Hitpoints").BaseValue = 1; break;
                case "Antidote": case "SumpsievePad": case "SoddenFieldDressing": effect = new PoisonedEffect(); break;
                case "BurnSalve": case "SootrootPulp": effect = new BurningEffect(); break;
                case "Panacea": case "AbsentmintLeaf": effect = new ConfusedEffect(); break;
                default: effect = new BleedingEffect(); break;
            }
            if (effect != null) Assert.True(target.ApplyEffect(effect));
            return effect;
        }
        protected sealed class Hook : Part
        {
            readonly string eventId; readonly Func<GameEvent, bool> callback;
            public Hook(string id, Func<GameEvent, bool> action) { eventId = id; callback = action; }
            public override bool HandleEvent(GameEvent e) => e.ID != eventId || callback(e);
        }
    }

    public sealed class ItemUtility35CompanionTests : ItemUtility35CompanionFixture
    {
        [TestCase("HealingTonic")] [TestCase("Antidote")] [TestCase("BurnSalve")] [TestCase("Panacea")]
        [TestCase("KnotflaxBandage")] [TestCase("SoddenFieldDressing")] [TestCase("SumpsievePad")]
        [TestCase("ClaspbeanPulp")] [TestCase("MargincressRibbon")] [TestCase("AbsentmintLeaf")]
        [TestCase("KnitmossPad")] [TestCase("SootrootPulp")]
        public void RealCarriedRemedyTreatsAnActuallyNeedyCompanion(string blueprint)
        {
            var target = Companion(); var effect = Need(target, blueprint); var item = Carry(blueprint);
            int helperHp = Actor.GetStatValue("Hitpoints"), before = target.GetStatValue("Hitpoints");
            Assert.True(Act(item, SelectCare(item, target)));
            Assert.False(Pack.Objects.Contains(item)); Assert.AreEqual(helperHp, Actor.GetStatValue("Hitpoints"));
            if (effect != null) Assert.False(target.GetPart<StatusEffectsPart>().GetAllEffects().Contains(effect));
            else Assert.Greater(target.GetStatValue("Hitpoints"), before);
            if (blueprint == "BurnSalve" || blueprint == "SootrootPulp")
                Assert.LessOrEqual(target.GetPart<ThermalPart>().Temperature, target.GetPart<ThermalPart>().AmbientTemperature);
        }

        [TestCase("HealingTonic")] [TestCase("Antidote")] [TestCase("BurnSalve")] [TestCase("Panacea")]
        [TestCase("KnotflaxBandage")] [TestCase("SoddenFieldDressing")] [TestCase("SumpsievePad")]
        [TestCase("ClaspbeanPulp")] [TestCase("MargincressRibbon")] [TestCase("AbsentmintLeaf")]
        [TestCase("KnitmossPad")] [TestCase("SootrootPulp")] [TestCase("FieldMeal")]
        public void UnaffectedCompanionNeverOffersWastefulCare(string blueprint)
        {
            Companion(); var item = Carry(blueprint);
            Assert.False(Actions(item).Any(CareAction)); Assert.Contains(item, Pack.Objects);
        }

        [TestCase("CookedMeat", "Toughness", 2)] [TestCase("ToastedEmberwheat", "HeatResistance", 20)]
        [TestCase("RoastedMushroom", "AcidResistance", 20)] [TestCase("RoastedHearthbulb", "ColdResistance", 20)]
        [TestCase("RoastedStarapple", "DV", 1)]
        public void SharedPreparedMealChangesOnlyTheCompanionsPreparation(string blueprint, string stat, int bonus)
        {
            var target = Companion(); int original = target.GetStatValue(stat); var item = Carry(blueprint);
            Assert.True(Act(item, SelectCare(item, target, true)));
            var meal = target.GetEffect<PreparedMealEffect>(); Assert.NotNull(meal);
            Assert.AreEqual(stat, meal.StatName); Assert.AreEqual(bonus, meal.Bonus); Assert.AreEqual(100, meal.Duration);
            Assert.AreEqual(original + bonus, target.GetStatValue(stat)); Assert.False(Actor.HasEffect<PreparedMealEffect>());
            Assert.False(Pack.Objects.Contains(item));
        }

        [Test] public void FieldMealSharesItsActualHealingAndOneBleedWithoutReplacingPreparation()
        {
            var target = Companion(); target.GetStat("Hitpoints").BaseValue = 1;
            var bleed = new BleedingEffect(); Assert.True(target.ApplyEffect(bleed));
            var meal = new PreparedMealEffect("ColdResistance", 20); Assert.True(target.ApplyEffect(meal));
            var item = Carry("FieldMeal"); Assert.True(Act(item, SelectCare(item, target, true)));
            Assert.Greater(target.GetStatValue("Hitpoints"), 1); Assert.False(target.HasEffect<BleedingEffect>());
            Assert.AreSame(meal, target.GetEffect<PreparedMealEffect>()); Assert.False(Pack.Objects.Contains(item));
        }

        [TestCase("SoddenFieldDressing", false)] [TestCase("SumpsievePad", true)] [TestCase("Antidote", true)]
        public void RealGasPoisonCureDistinctionIsPreserved(string blueprint, bool treatsGas)
        {
            var target = Companion(); var poison = new PoisonedByGasEffect { Duration = 5 }; Assert.True(target.ApplyEffect(poison));
            var item = Carry(blueprint); Assert.AreEqual(treatsGas, Actions(item).Any(CareAction));
            if (treatsGas) Assert.True(Act(item, SelectCare(item, target)));
            Assert.AreEqual(!treatsGas, target.HasEffect<PoisonedByGasEffect>());
            Assert.AreEqual(!treatsGas, Pack.Objects.Contains(item));
        }

        [Test] public void BroadCurePreservesPositiveMealAndPartyAllegiance()
        {
            var target = Companion(); var meal = new PreparedMealEffect("HeatResistance", 20); target.ApplyEffect(meal);
            target.ApplyEffect(new ConfusedEffect()); target.ApplyEffect(new PoisonedEffect());
            var item = Carry("Panacea"); Assert.True(Act(item, SelectCare(item, target)));
            Assert.AreSame(meal, target.GetEffect<PreparedMealEffect>()); Assert.AreEqual(20, target.GetStatValue("HeatResistance"));
            Assert.False(target.HasEffect<ConfusedEffect>()); Assert.False(target.HasEffect<PoisonedEffect>());
            Assert.AreSame(Actor, target.GetPart<BrainPart>().PartyLeader);
        }

        [TestCase("stranger")] [TestCase("hostile")] [TestCase("dead")] [TestCase("distant")] [TestCase("self")]
        public void CareRequiresALivingWillingNearbyCompanion(string fault)
        {
            var target = fault == "self" ? Actor : Companion(); target.GetStat("Hitpoints").BaseValue = 1;
            if (fault == "stranger") target.GetPart<BrainPart>().SetPartyLeader(null);
            if (fault == "hostile") target.GetPart<BrainPart>().SetPersonallyHostile(Actor);
            if (fault == "dead") target.GetStat("Hitpoints").BaseValue = 0;
            if (fault == "distant") Zone.MoveEntity(target, 15, 10);
            var item = Carry("HealingTonic"); Assert.False(Actions(item).Any(CareAction)); Assert.Contains(item, Pack.Objects);
        }

        [Test] public void IncapacitatedCompanionCanReceiveTheCureTheyNeed()
        {
            var target = Companion(); target.ApplyEffect(new StunnedEffect()); target.ApplyEffect(new PoisonedEffect());
            var item = Carry("Antidote"); Assert.True(Act(item, SelectCare(item, target)));
            Assert.False(target.HasEffect<PoisonedEffect>()); Assert.True(target.HasEffect<StunnedEffect>());
        }

        [Test] public void ANewMealReplacesRatherThanStacksAndIdenticalFullMealIsANoOp()
        {
            var target = Companion(); int cold = target.GetStatValue("ColdResistance"), heat = target.GetStatValue("HeatResistance");
            target.ApplyEffect(new PreparedMealEffect("ColdResistance", 20));
            var item = Carry("ToastedEmberwheat"); Assert.True(Act(item, SelectCare(item, target, true)));
            Assert.AreEqual(cold, target.GetStatValue("ColdResistance")); Assert.AreEqual(heat + 20, target.GetStatValue("HeatResistance"));
            Assert.AreEqual(1, target.GetPart<StatusEffectsPart>().GetAllEffects().OfType<PreparedMealEffect>().Count());
            var second = Carry("ToastedEmberwheat"); Assert.False(Actions(second).Any(CareAction)); Assert.Contains(second, Pack.Objects);
        }

        [Test] public void SharedMealBenefitSurvivesNativeEntitySaveAndExpiresNormally()
        {
            var target = Companion(); var item = Carry("RoastedStarapple"); int before = target.GetStatValue("DV");
            Assert.True(Act(item, SelectCare(item, target, true)));
            var restored = PartRoundTripHelper.RoundTripEntityViaTokenGraph(target);
            Assert.AreEqual(before + 1, restored.GetStatValue("DV"));
            Assert.True(restored.GetPart<StatusEffectsPart>().RemoveEffect<PreparedMealEffect>());
            Assert.AreEqual(before, restored.GetStatValue("DV"));
        }
    }
}
