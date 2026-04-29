using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// LightningTonic — applies ElectrifiedEffect on drink. Pairs with
    /// existing ThunderHammer / electrified-aware combat scenarios.
    /// Test pattern mirrors AcidTonicTests / StoneskinTonicTests.
    /// </summary>
    public class LightningTonicTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetup() => _harness = new ScenarioTestHarness();
        [OneTimeTearDown]
        public void OneTimeTearDown() { _harness?.Dispose(); _harness = null; }

        [SetUp]
        public void Setup() => MessageLog.Clear();

        [Test]
        public void LightningTonic_DrinkApplies_ElectrifiedEffect_ToDrinker()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("LightningTonic");
            Assert.IsNotNull(tonic, "LightningTonic blueprint must exist");

            FireApplyTonic(tonic, drinker);

            Assert.IsTrue(drinker.GetPart<StatusEffectsPart>().HasEffect<ElectrifiedEffect>(),
                "After drinking LightningTonic, the drinker should have ElectrifiedEffect");
        }

        [Test]
        public void LightningTonic_BlueprintMagnitude_BecomesEffectCharge()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("LightningTonic");
            FireApplyTonic(tonic, drinker);

            var elec = drinker.GetPart<StatusEffectsPart>().GetEffect<ElectrifiedEffect>();
            Assert.IsNotNull(elec, "ElectrifiedEffect must be applied");
            Assert.Greater(elec.Charge, 0f,
                "Tonic-applied ElectrifiedEffect should have positive Charge");
        }

        [Test]
        public void LightningTonic_AppliedEffect_IsElectrifiedEffect_NotOtherElement()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("LightningTonic");
            FireApplyTonic(tonic, drinker);

            var effects = drinker.GetPart<StatusEffectsPart>();
            Assert.IsFalse(effects.HasEffect<BurningEffect>(),
                "LightningTonic must not apply BurningEffect");
            Assert.IsFalse(effects.HasEffect<FrozenEffect>(),
                "LightningTonic must not apply FrozenEffect");
            Assert.IsFalse(effects.HasEffect<AcidicEffect>(),
                "LightningTonic must not apply AcidicEffect");
            Assert.IsFalse(effects.HasEffect<WetEffect>(),
                "LightningTonic must not apply WetEffect");
        }

        [Test]
        public void HealingTonic_DoesNotApply_ElectrifiedEffect()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("HealingTonic");
            FireApplyTonic(tonic, drinker);

            Assert.IsFalse(drinker.GetPart<StatusEffectsPart>().HasEffect<ElectrifiedEffect>(),
                "HealingTonic must NOT apply ElectrifiedEffect");
        }

        [Test]
        public void FireTonic_DoesNotApply_ElectrifiedEffect()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("FireTonic");
            FireApplyTonic(tonic, drinker);

            Assert.IsFalse(drinker.GetPart<StatusEffectsPart>().HasEffect<ElectrifiedEffect>(),
                "FireTonic must NOT apply ElectrifiedEffect");
        }

        [Test]
        public void LightningTonic_NoStatusEffectsPart_DoesNotCrash()
        {
            var dummy = new Entity { ID = "dummy", BlueprintName = "TestDummy" };
            dummy.Statistics["Hitpoints"] = new Stat
                { Owner = dummy, Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            dummy.AddPart(new RenderPart { DisplayName = "dummy" });

            var tonic = _harness.Factory.CreateEntity("LightningTonic");
            Assert.DoesNotThrow(() => FireApplyTonic(tonic, dummy),
                "Drinking LightningTonic on an actor without StatusEffectsPart must not crash");
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
