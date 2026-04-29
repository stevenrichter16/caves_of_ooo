using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// FrostTonic — applies FrozenEffect on drink. Pairs with existing
    /// IceSword and frost-aware combat scenarios. Test pattern mirrors
    /// AcidTonicTests / StoneskinTonicTests.
    /// </summary>
    public class FrostTonicTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetup() => _harness = new ScenarioTestHarness();
        [OneTimeTearDown]
        public void OneTimeTearDown() { _harness?.Dispose(); _harness = null; }

        [SetUp]
        public void Setup() => MessageLog.Clear();

        [Test]
        public void FrostTonic_DrinkApplies_FrozenEffect_ToDrinker()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("FrostTonic");
            Assert.IsNotNull(tonic, "FrostTonic blueprint must exist");

            FireApplyTonic(tonic, drinker);

            Assert.IsTrue(drinker.GetPart<StatusEffectsPart>().HasEffect<FrozenEffect>(),
                "After drinking FrostTonic, the drinker should have FrozenEffect");
        }

        [Test]
        public void FrostTonic_BlueprintMagnitude_BecomesEffectCold()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("FrostTonic");
            FireApplyTonic(tonic, drinker);

            var frozen = drinker.GetPart<StatusEffectsPart>().GetEffect<FrozenEffect>();
            Assert.IsNotNull(frozen, "FrozenEffect must be applied");
            Assert.Greater(frozen.Cold, 0f,
                "Tonic-applied FrozenEffect should have positive Cold");
        }

        [Test]
        public void FrostTonic_AppliedEffect_IsFrozenEffect_NotOtherElement()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("FrostTonic");
            FireApplyTonic(tonic, drinker);

            var effects = drinker.GetPart<StatusEffectsPart>();
            Assert.IsFalse(effects.HasEffect<BurningEffect>(),
                "FrostTonic must not apply BurningEffect");
            Assert.IsFalse(effects.HasEffect<AcidicEffect>(),
                "FrostTonic must not apply AcidicEffect");
            Assert.IsFalse(effects.HasEffect<ElectrifiedEffect>(),
                "FrostTonic must not apply ElectrifiedEffect");
            Assert.IsFalse(effects.HasEffect<WetEffect>(),
                "FrostTonic must not apply WetEffect");
        }

        [Test]
        public void HealingTonic_DoesNotApply_FrozenEffect()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("HealingTonic");
            FireApplyTonic(tonic, drinker);

            Assert.IsFalse(drinker.GetPart<StatusEffectsPart>().HasEffect<FrozenEffect>(),
                "HealingTonic must NOT apply FrozenEffect");
        }

        [Test]
        public void FireTonic_DoesNotApply_FrozenEffect()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("FireTonic");
            FireApplyTonic(tonic, drinker);

            Assert.IsFalse(drinker.GetPart<StatusEffectsPart>().HasEffect<FrozenEffect>(),
                "FireTonic must NOT apply FrozenEffect");
        }

        [Test]
        public void FrostTonic_NoStatusEffectsPart_DoesNotCrash()
        {
            var dummy = new Entity { ID = "dummy", BlueprintName = "TestDummy" };
            dummy.Statistics["Hitpoints"] = new Stat
                { Owner = dummy, Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            dummy.AddPart(new RenderPart { DisplayName = "dummy" });

            var tonic = _harness.Factory.CreateEntity("FrostTonic");
            Assert.DoesNotThrow(() => FireApplyTonic(tonic, dummy),
                "Drinking FrostTonic on an actor without StatusEffectsPart must not crash");
        }

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
