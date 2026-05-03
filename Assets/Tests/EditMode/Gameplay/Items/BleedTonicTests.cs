using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// BleedTonic — applies BleedingEffect on drink. Test pattern mirrors
    /// FrostTonicTests / AcidTonicTests. Pins:
    ///   1. Drinking the tonic actually applies BleedingEffect
    ///   2. Blueprint EffectDuration → effect SaveTarget mapping
    ///      (BleedingEffect doesn't take a duration, so EffectDuration's
    ///      int slot is repurposed as saveTarget DC)
    ///   3. Counter-check: BleedTonic doesn't accidentally apply other
    ///      elemental effects (rules out cross-pollination if the
    ///      dispatcher's switch fall-through were ever broken)
    ///   4. Adversarial: drinking on an entity without StatusEffectsPart
    ///      doesn't crash
    /// </summary>
    public class BleedTonicTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetup() => _harness = new ScenarioTestHarness();
        [OneTimeTearDown]
        public void OneTimeTearDown() { _harness?.Dispose(); _harness = null; }

        [SetUp]
        public void Setup() => MessageLog.Clear();

        [Test]
        public void BleedTonic_DrinkApplies_BleedingEffect_ToDrinker()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("BleedTonic");
            Assert.IsNotNull(tonic, "BleedTonic blueprint must exist");

            FireApplyTonic(tonic, drinker);

            Assert.IsTrue(drinker.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>(),
                "After drinking BleedTonic, the drinker should have BleedingEffect");
        }

        [Test]
        public void BleedTonic_BlueprintEffectDuration_BecomesEffectSaveTarget()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("BleedTonic");
            FireApplyTonic(tonic, drinker);

            var bleed = drinker.GetPart<StatusEffectsPart>().GetEffect<BleedingEffect>();
            Assert.IsNotNull(bleed, "BleedingEffect must be applied");
            // Blueprint sets EffectDuration=12 → dispatcher passes that as
            // saveTarget. (Pin the content-author contract: blueprints can
            // drop the field and we'd default to 15; the BleedTonic blueprint
            // explicitly sets 12 to make recovery slightly easier than default.)
            Assert.AreEqual(12, bleed.SaveTarget,
                "Tonic-applied BleedingEffect SaveTarget should match blueprint EffectDuration=12");
        }

        [Test]
        public void BleedTonic_AppliedEffect_IsBleedingEffect_NotOtherElement()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("BleedTonic");
            FireApplyTonic(tonic, drinker);

            var effects = drinker.GetPart<StatusEffectsPart>();
            Assert.IsFalse(effects.HasEffect<BurningEffect>(),
                "BleedTonic must not apply BurningEffect");
            Assert.IsFalse(effects.HasEffect<FrozenEffect>(),
                "BleedTonic must not apply FrozenEffect");
            Assert.IsFalse(effects.HasEffect<AcidicEffect>(),
                "BleedTonic must not apply AcidicEffect");
            Assert.IsFalse(effects.HasEffect<ElectrifiedEffect>(),
                "BleedTonic must not apply ElectrifiedEffect");
            Assert.IsFalse(effects.HasEffect<PoisonedEffect>(),
                "BleedTonic must not apply PoisonedEffect");
        }

        [Test]
        public void HealingTonic_DoesNotApply_BleedingEffect()
        {
            // Counter-check: a different tonic (the most basic, non-status one)
            // does NOT accidentally apply BleedingEffect — proves our positive
            // assertion above isn't from some shared default path.
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("HealingTonic");
            FireApplyTonic(tonic, drinker);

            Assert.IsFalse(drinker.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>(),
                "HealingTonic must NOT apply BleedingEffect");
        }

        [Test]
        public void BleedTonic_NoStatusEffectsPart_DoesNotCrash()
        {
            // Adversarial: stripped-down entity without StatusEffectsPart.
            // Entity.ApplyEffect auto-creates the part if missing; verify
            // the dispatcher → ApplyEffect path tolerates absence gracefully.
            var dummy = new Entity { ID = "dummy", BlueprintName = "TestDummy" };
            dummy.Statistics["Hitpoints"] = new Stat
                { Owner = dummy, Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            dummy.AddPart(new RenderPart { DisplayName = "dummy" });

            var tonic = _harness.Factory.CreateEntity("BleedTonic");
            Assert.DoesNotThrow(() => FireApplyTonic(tonic, dummy),
                "Drinking BleedTonic on an actor without StatusEffectsPart must not crash");
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
