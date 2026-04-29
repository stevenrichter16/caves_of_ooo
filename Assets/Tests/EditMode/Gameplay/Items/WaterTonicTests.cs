using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WaterTonic — applies WetEffect on drink. The mundane elemental
    /// tonic; pairs with future fire-extinguishing / electric-conduction
    /// content. Test pattern mirrors AcidTonicTests / StoneskinTonicTests.
    /// </summary>
    public class WaterTonicTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetup() => _harness = new ScenarioTestHarness();
        [OneTimeTearDown]
        public void OneTimeTearDown() { _harness?.Dispose(); _harness = null; }

        [SetUp]
        public void Setup() => MessageLog.Clear();

        [Test]
        public void WaterTonic_DrinkApplies_WetEffect_ToDrinker()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("WaterTonic");
            Assert.IsNotNull(tonic, "WaterTonic blueprint must exist");

            FireApplyTonic(tonic, drinker);

            Assert.IsTrue(drinker.GetPart<StatusEffectsPart>().HasEffect<WetEffect>(),
                "After drinking WaterTonic, the drinker should have WetEffect");
        }

        [Test]
        public void WaterTonic_BlueprintMagnitude_BecomesEffectMoisture()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("WaterTonic");
            FireApplyTonic(tonic, drinker);

            var wet = drinker.GetPart<StatusEffectsPart>().GetEffect<WetEffect>();
            Assert.IsNotNull(wet, "WetEffect must be applied");
            Assert.Greater(wet.Moisture, 0f,
                "Tonic-applied WetEffect should have positive Moisture");
        }

        [Test]
        public void WaterTonic_AppliedEffect_IsWetEffect_NotOtherElement()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("WaterTonic");
            FireApplyTonic(tonic, drinker);

            var effects = drinker.GetPart<StatusEffectsPart>();
            Assert.IsFalse(effects.HasEffect<BurningEffect>(),
                "WaterTonic must not apply BurningEffect");
            Assert.IsFalse(effects.HasEffect<FrozenEffect>(),
                "WaterTonic must not apply FrozenEffect");
            Assert.IsFalse(effects.HasEffect<AcidicEffect>(),
                "WaterTonic must not apply AcidicEffect");
            Assert.IsFalse(effects.HasEffect<ElectrifiedEffect>(),
                "WaterTonic must not apply ElectrifiedEffect");
        }

        [Test]
        public void HealingTonic_DoesNotApply_WetEffect()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("HealingTonic");
            FireApplyTonic(tonic, drinker);

            Assert.IsFalse(drinker.GetPart<StatusEffectsPart>().HasEffect<WetEffect>(),
                "HealingTonic must NOT apply WetEffect");
        }

        [Test]
        public void FireTonic_DoesNotApply_WetEffect()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("FireTonic");
            FireApplyTonic(tonic, drinker);

            Assert.IsFalse(drinker.GetPart<StatusEffectsPart>().HasEffect<WetEffect>(),
                "FireTonic must NOT apply WetEffect");
        }

        [Test]
        public void WaterTonic_NoStatusEffectsPart_DoesNotCrash()
        {
            var dummy = new Entity { ID = "dummy", BlueprintName = "TestDummy" };
            dummy.Statistics["Hitpoints"] = new Stat
                { Owner = dummy, Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            dummy.AddPart(new RenderPart { DisplayName = "dummy" });

            var tonic = _harness.Factory.CreateEntity("WaterTonic");
            Assert.DoesNotThrow(() => FireApplyTonic(tonic, dummy),
                "Drinking WaterTonic on an actor without StatusEffectsPart must not crash");
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
