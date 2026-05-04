using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// StoneskinTonic — first content ship that uses Phase F (BeforeTakeDamage)
    /// + Phase T2.4 (StoneskinEffect) as a customer.
    ///
    /// User-visible invariant: "Drinking a stoneskin tonic applies a
    /// duration-bound StoneskinEffect to the drinker. The effect reduces
    /// incoming damage by Reduction (default 2) for Duration turns,
    /// then fades."
    ///
    /// Coverage:
    ///   1. Tonic ApplyTonic event applies StoneskinEffect
    ///   2. Reduction value flows from blueprint params
    ///   3. Duration value flows from blueprint params
    ///   4. Drinking actually reduces damage during the active window
    ///   5. Counter-check: a non-stoneskin tonic doesn't apply Stoneskin
    ///   6. Counter-check: actor without StatusEffectsPart doesn't crash
    /// </summary>
    public class StoneskinTonicTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetup() => _harness = new ScenarioTestHarness();
        [OneTimeTearDown]
        public void OneTimeTearDown() { _harness?.Dispose(); _harness = null; }

        [SetUp]
        public void Setup() => MessageLog.Clear();

        // ====================================================================
        // 1. Drinking a StoneskinTonic applies a StoneskinEffect
        // ====================================================================

        [Test]
        public void StoneskinTonic_DrinkApplies_StoneskinEffect_ToDrinker()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("StoneskinTonic");
            Assert.IsNotNull(tonic, "StoneskinTonic blueprint must exist");

            // Fire ApplyTonic event on the tonic with the drinker as the actor
            FireApplyTonic(tonic, drinker);

            var effects = drinker.GetPart<StatusEffectsPart>();
            Assert.IsTrue(effects.HasEffect<StoneskinEffect>(),
                "After drinking, the drinker should have StoneskinEffect");
        }

        // ====================================================================
        // 2. Reduction flows from blueprint EffectMagnitude param
        // ====================================================================

        [Test]
        public void StoneskinTonic_BlueprintMagnitude_BecomesEffectReduction()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("StoneskinTonic");
            FireApplyTonic(tonic, drinker);

            var stoneskin = drinker.GetPart<StatusEffectsPart>().GetEffect<StoneskinEffect>();
            Assert.IsNotNull(stoneskin);
            Assert.Greater(stoneskin.Reduction, 0,
                "Tonic-applied StoneskinEffect should have a positive Reduction value");
        }

        // ====================================================================
        // 3. Duration flows from blueprint EffectDuration param
        // ====================================================================

        [Test]
        public void StoneskinTonic_BlueprintDuration_BecomesEffectDuration()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("StoneskinTonic");
            FireApplyTonic(tonic, drinker);

            var stoneskin = drinker.GetPart<StatusEffectsPart>().GetEffect<StoneskinEffect>();
            Assert.IsNotNull(stoneskin);
            Assert.Greater(stoneskin.Duration, 0,
                "Tonic-applied StoneskinEffect should have a positive Duration");
            Assert.Less(stoneskin.Duration, 100,
                "Duration should be a reasonable bounded value, not indefinite");
        }

        // ====================================================================
        // 4. Drinking the tonic actually reduces damage during active window
        // ====================================================================

        [Test]
        public void StoneskinTonic_ReducesIncomingDamage_DuringActiveWindow()
        {
            var drinker = MakeDrinker(hp: 100);
            var tonic = _harness.Factory.CreateEntity("StoneskinTonic");
            FireApplyTonic(tonic, drinker);

            int hpBefore = drinker.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(drinker, new Damage(10), source: null, zone: null);
            int hpAfter = drinker.GetStatValue("Hitpoints");
            int damageTaken = hpBefore - hpAfter;

            Assert.Less(damageTaken, 10,
                $"Drinker with active StoneskinEffect should take less than 10 damage from a 10-damage hit. " +
                $"Got delta {damageTaken} (full damage was {10}).");
            Assert.Greater(damageTaken, 0,
                $"Stoneskin shouldn't fully block at default settings. Got delta {damageTaken}.");
        }

        // ====================================================================
        // 5. Counter-check: non-stoneskin tonic doesn't apply stoneskin
        // ====================================================================

        [Test]
        public void HealingTonic_DoesNotApply_StoneskinEffect()
        {
            var drinker = MakeDrinker();
            var tonic = _harness.Factory.CreateEntity("HealingTonic");
            Assert.IsNotNull(tonic, "HealingTonic blueprint must exist (sanity)");
            FireApplyTonic(tonic, drinker);

            Assert.IsFalse(drinker.GetPart<StatusEffectsPart>().HasEffect<StoneskinEffect>(),
                "HealingTonic must NOT apply StoneskinEffect — that would mean " +
                "the dispatcher is mis-routing 'Stoneskin' to all tonics");
        }

        // ====================================================================
        // 6. Counter-check: actor without StatusEffectsPart doesn't crash
        // ====================================================================

        [Test]
        public void StoneskinTonic_NoStatusEffectsPart_DoesNotCrash()
        {
            // Wooden dummy with no StatusEffectsPart — drinking should silently
            // no-op rather than throwing.
            var dummy = new Entity { ID = "dummy", BlueprintName = "TestDummy" };
            dummy.Statistics["Hitpoints"] = new Stat
                { Owner = dummy, Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            dummy.AddPart(new RenderPart { DisplayName = "dummy" });

            var tonic = _harness.Factory.CreateEntity("StoneskinTonic");
            Assert.DoesNotThrow(() => FireApplyTonic(tonic, dummy),
                "Drinking on an actor without StatusEffectsPart must not crash");
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
