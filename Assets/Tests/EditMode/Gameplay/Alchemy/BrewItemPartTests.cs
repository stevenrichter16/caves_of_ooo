using System;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// BrewItemPart — the multi-effect consumable carrier. Fires on the same
    /// "ApplyTonic" event StatusTonicPart uses; these tests build that event
    /// exactly the way TonicPart.ApplyTo does and fire it on the item.
    /// </summary>
    public class BrewItemPartTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        private static Entity MakeDrinker(bool withStatusEffects = true)
        {
            var e = new Entity { ID = "drinker", BlueprintName = "drinker" };
            e.Statistics["Hitpoints"] = new Stat
            {
                Owner = e, Name = "Hitpoints", BaseValue = 50, Min = 0, Max = 50
            };
            e.AddPart(new RenderPart { DisplayName = "drinker" });
            if (withStatusEffects)
                e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static Entity MakeBrew(string effectsRaw)
        {
            var brew = new Entity { ID = "brew", BlueprintName = "BrewedTonic" };
            brew.AddPart(new RenderPart { DisplayName = "test brew" });
            brew.AddPart(new BrewItemPart { EffectsRaw = effectsRaw, Form = "Tonic" });
            return brew;
        }

        /// <summary>Mirror of TonicPart.ApplyTo's ApplyTonic event construction.</summary>
        private static void FireApplyTonic(Entity brew, Entity drinker)
        {
            var applyEvent = GameEvent.New("ApplyTonic");
            applyEvent.SetParameter("Actor", (object)drinker);
            applyEvent.SetParameter("Tonic", (object)brew);
            applyEvent.SetParameter("Effect", "");
            applyEvent.SetParameter("Duration", 0);
            applyEvent.SetParameter("Zone", (object)null);
            applyEvent.SetParameter("Random", (object)new Random(7));
            applyEvent.SetParameter("Source", (object)drinker);
            brew.FireEventAndRelease(applyEvent);
        }

        [Test]
        public void SingleEffect_IsApplied()
        {
            var drinker = MakeDrinker();
            var brew = MakeBrew("Burning:2");

            FireApplyTonic(brew, drinker);

            Assert.IsTrue(drinker.GetPart<StatusEffectsPart>().HasEffect<BurningEffect>(),
                "a Burning:2 brew must apply BurningEffect on consumption.");
        }

        [Test]
        public void MultiEffect_GalvanicDraught_AppliesBoth()
        {
            // The emergent-combo payoff: one flask, two effects.
            var drinker = MakeDrinker();
            var brew = MakeBrew("Acidic:2;Electrified:1");

            FireApplyTonic(brew, drinker);

            var effects = drinker.GetPart<StatusEffectsPart>();
            Assert.IsTrue(effects.HasEffect<AcidicEffect>(), "galvanic draught must apply Acidic.");
            Assert.IsTrue(effects.HasEffect<ElectrifiedEffect>(), "galvanic draught must apply Electrified.");
        }

        [Test]
        public void NonListedEffects_AreNotApplied()
        {
            // Counter-check: a Burning-only brew must not leak sibling effects.
            var drinker = MakeDrinker();
            var brew = MakeBrew("Burning:2");

            FireApplyTonic(brew, drinker);

            var effects = drinker.GetPart<StatusEffectsPart>();
            Assert.IsFalse(effects.HasEffect<AcidicEffect>());
            Assert.IsFalse(effects.HasEffect<ElectrifiedEffect>());
            Assert.IsFalse(effects.HasEffect<FrozenEffect>());
        }

        [Test]
        public void PotencyFlowsIntoEffectMagnitude()
        {
            // Acidic exposes Corrosion — pin that brew potency reaches the
            // effect's magnitude slot through TonicEffectFactory.
            var drinker = MakeDrinker();
            var brew = MakeBrew("Acidic:3");

            FireApplyTonic(brew, drinker);

            var acid = drinker.GetPart<StatusEffectsPart>().GetEffect<AcidicEffect>();
            Assert.IsNotNull(acid);
            Assert.AreEqual(3f, acid.Corrosion, 0.001f,
                "potency 3 must arrive as Corrosion 3 via the factory magnitude slot.");
        }

        [Test]
        public void UnknownEffectName_IsSkipped_NoCrash()
        {
            var drinker = MakeDrinker();
            var brew = MakeBrew("UltraMegaQuantumBurst:9;Burning:1");

            Assert.DoesNotThrow(() => FireApplyTonic(brew, drinker));
            Assert.IsTrue(drinker.GetPart<StatusEffectsPart>().HasEffect<BurningEffect>(),
                "valid entries after an unknown one must still apply.");
        }

        [Test]
        public void EmptyEffectsRaw_NoOp_NoCrash()
        {
            var drinker = MakeDrinker();
            var brew = MakeBrew("");

            Assert.DoesNotThrow(() => FireApplyTonic(brew, drinker));
        }

        [Test]
        public void OtherEvents_AreIgnored()
        {
            // Counter-check on the event gate: a non-ApplyTonic event with the
            // same parameters must not apply anything.
            var drinker = MakeDrinker();
            var brew = MakeBrew("Burning:2");

            var wrongEvent = GameEvent.New("SomeOtherEvent");
            wrongEvent.SetParameter("Actor", (object)drinker);
            brew.FireEventAndRelease(wrongEvent);

            Assert.IsFalse(drinker.GetPart<StatusEffectsPart>().HasEffect<BurningEffect>(),
                "BrewItemPart must only respond to ApplyTonic.");
        }
    }
}
