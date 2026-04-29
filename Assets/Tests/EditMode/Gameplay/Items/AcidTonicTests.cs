using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// AcidTonic — applies AcidicEffect on drink. The fourth elemental
    /// tonic in the StatusTonicPart dispatcher's existing capability set
    /// (the dispatcher already routes "Acid" → AcidicEffect; this class
    /// pins the blueprint that exposes that path to players).
    ///
    /// Mirrors StoneskinTonicTests' six-test pattern:
    ///   1. Drinking applies AcidicEffect
    ///   2. Magnitude flows from blueprint param
    ///   3. Counter-check (effect class is correct)
    ///   4. Counter-check (HealingTonic does NOT apply AcidicEffect)
    ///   5. Counter-check (FireTonic does NOT apply AcidicEffect)
    ///   6. Null-safety (actor without StatusEffectsPart)
    /// </summary>
    public class AcidTonicTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetup() => _harness = new ScenarioTestHarness();
        [OneTimeTearDown]
        public void OneTimeTearDown() { _harness?.Dispose(); _harness = null; }

        [SetUp]
        public void Setup() => MessageLog.Clear();

        [Test]
        public void AcidTonic_DrinkApplies_AcidicEffect_ToDrinker()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("AcidTonic");
            Assert.IsNotNull(tonic, "AcidTonic blueprint must exist");

            FireApplyTonic(tonic, drinker);

            var effects = drinker.GetPart<StatusEffectsPart>();
            Assert.IsTrue(effects.HasEffect<AcidicEffect>(),
                "After drinking AcidTonic, the drinker should have AcidicEffect");
        }

        [Test]
        public void AcidTonic_BlueprintMagnitude_BecomesEffectCorrosion()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("AcidTonic");
            FireApplyTonic(tonic, drinker);

            var acid = drinker.GetPart<StatusEffectsPart>().GetEffect<AcidicEffect>();
            Assert.IsNotNull(acid, "AcidicEffect must be applied");
            Assert.Greater(acid.Corrosion, 0f,
                "Tonic-applied AcidicEffect should have positive Corrosion");
        }

        [Test]
        public void AcidTonic_AppliedEffect_IsAcidicEffect_NotOtherElement()
        {
            // Counter-check: the dispatcher routes "Acid" specifically;
            // the drinker should not receive any unrelated elemental effect.
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("AcidTonic");
            FireApplyTonic(tonic, drinker);

            var effects = drinker.GetPart<StatusEffectsPart>();
            Assert.IsFalse(effects.HasEffect<BurningEffect>(),
                "AcidTonic must not apply BurningEffect");
            Assert.IsFalse(effects.HasEffect<FrozenEffect>(),
                "AcidTonic must not apply FrozenEffect");
            Assert.IsFalse(effects.HasEffect<ElectrifiedEffect>(),
                "AcidTonic must not apply ElectrifiedEffect");
            Assert.IsFalse(effects.HasEffect<WetEffect>(),
                "AcidTonic must not apply WetEffect");
        }

        [Test]
        public void HealingTonic_DoesNotApply_AcidicEffect()
        {
            // Counter-check: HealingTonic shouldn't accidentally route through
            // the Acid case. Catches dispatcher mis-wiring.
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("HealingTonic");
            Assert.IsNotNull(tonic, "HealingTonic blueprint must exist (sanity)");
            FireApplyTonic(tonic, drinker);

            Assert.IsFalse(drinker.GetPart<StatusEffectsPart>().HasEffect<AcidicEffect>(),
                "HealingTonic must NOT apply AcidicEffect");
        }

        [Test]
        public void FireTonic_DoesNotApply_AcidicEffect()
        {
            // Counter-check across siblings: FireTonic exists, applies
            // BurningEffect, must not bleed into the Acid path.
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("FireTonic");
            Assert.IsNotNull(tonic, "FireTonic blueprint must exist (sibling sanity)");
            FireApplyTonic(tonic, drinker);

            Assert.IsFalse(drinker.GetPart<StatusEffectsPart>().HasEffect<AcidicEffect>(),
                "FireTonic must NOT apply AcidicEffect");
        }

        [Test]
        public void AcidTonic_NoStatusEffectsPart_DoesNotCrash()
        {
            var dummy = new Entity { ID = "dummy", BlueprintName = "TestDummy" };
            dummy.Statistics["Hitpoints"] = new Stat
                { Owner = dummy, Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            dummy.AddPart(new RenderPart { DisplayName = "dummy" });

            var tonic = _harness.Factory.CreateEntity("AcidTonic");
            Assert.DoesNotThrow(() => FireApplyTonic(tonic, dummy),
                "Drinking AcidTonic on an actor without StatusEffectsPart must not crash");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Entity MakeDrinker(int hp = 50)
        {
            var entity = new Entity { ID = "drinker", BlueprintName = "TestDrinker" };
            entity.Statistics["Hitpoints"] = new Stat
                { Owner = entity, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.AddPart(new RenderPart { DisplayName = "drinker" });
            entity.AddPart(new StatusEffectsPart());
            return entity;
        }

        private static void FireApplyTonic(Entity tonic, Entity actor)
        {
            var e = GameEvent.New("ApplyTonic");
            e.SetParameter("Actor", (object)actor);
            e.SetParameter("Source", (object)actor);
            tonic.FireEvent(e);
        }
    }
}
