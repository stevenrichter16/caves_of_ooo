using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// CharredTonic — applies CharredEffect on drink. Test pattern mirrors
    /// BleedTonicTests / FrostTonicTests. Pins:
    ///   1. Drinking the tonic actually applies CharredEffect
    ///   2. CharredEffect is permanent (Duration = DURATION_INDEFINITE)
    ///   3. CharredEffect reduces drinker's Combustibility to 30% of original
    ///      (i.e. 70% reduction, matching CharredEffect.OnApply's *0.3f)
    ///   4. Counter-check: doesn't apply other elemental effects
    ///   5. Adversarial: drinker without MaterialPart doesn't crash
    ///      (CharredEffect.OnApply has a null-check that should tolerate this)
    /// </summary>
    public class CharredTonicTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetup() => _harness = new ScenarioTestHarness();
        [OneTimeTearDown]
        public void OneTimeTearDown() { _harness?.Dispose(); _harness = null; }

        [SetUp]
        public void Setup() => MessageLog.Clear();

        [Test]
        public void CharredTonic_DrinkApplies_CharredEffect_ToDrinker()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("CharredTonic");
            Assert.IsNotNull(tonic, "CharredTonic blueprint must exist");

            FireApplyTonic(tonic, drinker);

            Assert.IsTrue(drinker.GetPart<StatusEffectsPart>().HasEffect<CharredEffect>(),
                "After drinking CharredTonic, the drinker should have CharredEffect");
        }

        [Test]
        public void CharredTonic_AppliesPermanentDuration()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("CharredTonic");
            FireApplyTonic(tonic, drinker);

            var charred = drinker.GetPart<StatusEffectsPart>().GetEffect<CharredEffect>();
            Assert.IsNotNull(charred, "CharredEffect must be applied");
            // CharredEffect ctor sets Duration = DURATION_INDEFINITE.
            // Pin this contract: the tonic always confers a permanent
            // vulnerability state (no auto-tickdown). EffectDuration on
            // the blueprint is intentionally ignored by the dispatcher.
            Assert.AreEqual(Effect.DURATION_INDEFINITE, charred.Duration,
                "CharredEffect from tonic must have indefinite duration");
        }

        [Test]
        public void CharredTonic_ReducesCombustibility_OnDrinkerWithMaterial()
        {
            // The tonic's gameplay-visible payload: drinker becomes harder
            // to ignite. Pin the 0.3x multiplier (70% reduction) from
            // CharredEffect.OnApply against an explicit before/after.
            var drinker = MakeDrinker();
            drinker.AddPart(new MaterialPart
            {
                MaterialID = "Flesh",
                Combustibility = 0.5f,
            });

            var tonic = _harness.Factory.CreateEntity("CharredTonic");
            FireApplyTonic(tonic, drinker);

            var material = drinker.GetPart<MaterialPart>();
            Assert.IsNotNull(material, "MaterialPart should still exist");
            // 0.5 * 0.3 = 0.15 (using a tolerance for float drift)
            Assert.That(material.Combustibility, Is.EqualTo(0.15f).Within(0.0001f),
                "CharredEffect.OnApply must reduce Combustibility to 30% of original (0.5 → 0.15)");
        }

        [Test]
        public void CharredTonic_AppliedEffect_IsCharredEffect_NotOtherElement()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("CharredTonic");
            FireApplyTonic(tonic, drinker);

            var effects = drinker.GetPart<StatusEffectsPart>();
            Assert.IsFalse(effects.HasEffect<BurningEffect>(),
                "CharredTonic must not apply BurningEffect (the conceptual opposite)");
            Assert.IsFalse(effects.HasEffect<FrozenEffect>(),
                "CharredTonic must not apply FrozenEffect");
            Assert.IsFalse(effects.HasEffect<AcidicEffect>(),
                "CharredTonic must not apply AcidicEffect");
            Assert.IsFalse(effects.HasEffect<BleedingEffect>(),
                "CharredTonic must not apply BleedingEffect");
            Assert.IsFalse(effects.HasEffect<PoisonedEffect>(),
                "CharredTonic must not apply PoisonedEffect");
        }

        [Test]
        public void HealingTonic_DoesNotApply_CharredEffect()
        {
            // Counter-check: a different tonic does NOT accidentally
            // apply CharredEffect — proves our positive assertion isn't
            // from some shared default path.
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("HealingTonic");
            FireApplyTonic(tonic, drinker);

            Assert.IsFalse(drinker.GetPart<StatusEffectsPart>().HasEffect<CharredEffect>(),
                "HealingTonic must NOT apply CharredEffect");
        }

        [Test]
        public void CharredTonic_NoMaterialPart_DoesNotCrash()
        {
            // Adversarial: drinker has StatusEffectsPart but no MaterialPart.
            // CharredEffect.OnApply's null-guard must hold — the effect is
            // still applied (the entity still gets the "charred" status),
            // but the combustibility-reduction step is a no-op.
            var drinker = MakeDrinker();
            // Intentionally do NOT add a MaterialPart.
            var tonic = _harness.Factory.CreateEntity("CharredTonic");

            Assert.DoesNotThrow(() => FireApplyTonic(tonic, drinker),
                "Drinking CharredTonic on an actor without MaterialPart must not crash");
            Assert.IsTrue(drinker.GetPart<StatusEffectsPart>().HasEffect<CharredEffect>(),
                "CharredEffect should still be applied even without MaterialPart");
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
