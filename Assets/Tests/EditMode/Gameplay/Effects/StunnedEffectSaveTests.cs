using System;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Saving-throw recovery for stuns (user-directed design, 2026-07-19),
    /// mirroring BleedingEffect's shape: each end-of-turn while stunned,
    /// roll 1d20 + Toughness modifier vs SaveTarget; success shakes the
    /// stun off early (cause save_succeeded); the target eases by 1 per
    /// failed turn. The save is OPT-IN per source (saveTarget = 0 disables)
    /// so plain countdown stuns — and every pre-existing test — stay
    /// deterministic. Duration remains the hard ceiling.
    /// </summary>
    public class StunnedEffectSaveTests
    {
        [SetUp]
        public void Setup() => MessageLog.Clear();

        private static Entity MakeVictim(int toughness)
        {
            var e = new Entity { ID = "victim", BlueprintName = "victim" };
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = "victim" });
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = 50, Min = 0, Max = 50 };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = toughness, Min = 1, Max = 60 };
            e.Statistics["DV"] = new Stat
                { Owner = e, Name = "DV", BaseValue = 10, Min = 0, Max = 50 };
            return e;
        }

        private static void FireTurnEnd(Entity entity)
        {
            var ev = GameEvent.New("EndTurn");
            ev.SetParameter("Target", (object)entity);
            entity.FireEvent(ev);
        }

        [Test]
        public void GuaranteedSave_ShakesOffOnFirstTurn_WithSaveSucceededCause()
        {
            // Toughness 60 → +22 modifier; 1d20+22 vs target 5 cannot fail.
            var victim = MakeVictim(toughness: 60);
            Assert.IsTrue(victim.ApplyEffect(new StunnedEffect(duration: 2, saveTarget: 5)));
            var probe = new CauseProbe();
            victim.AddPart(probe);

            FireTurnEnd(victim);

            Assert.IsFalse(victim.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "a guaranteed save ends the stun on the first turn");
            Assert.AreEqual(Effect.CAUSE_SAVE_SUCCEEDED, probe.LastCapturedCause);
        }

        [Test]
        public void ImpossibleSave_RunsTheFullCeiling_ThenExpires()
        {
            // 1d20+(-3) vs 999 cannot succeed within the 2-turn ceiling.
            var victim = MakeVictim(toughness: 10);
            Assert.IsTrue(victim.ApplyEffect(new StunnedEffect(duration: 2, saveTarget: 999)));
            var probe = new CauseProbe();
            victim.AddPart(probe);

            FireTurnEnd(victim);
            Assert.IsTrue(victim.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "failed save: the countdown ceiling still governs (turn 1 of 2)");

            FireTurnEnd(victim);
            Assert.IsFalse(victim.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "ceiling reached — stun expires normally");
            Assert.AreEqual(Effect.CAUSE_DURATION_EXPIRED, probe.LastCapturedCause);
        }

        [Test]
        public void DefaultCtor_NoSave_PureCountdownEvenWithHugeToughness()
        {
            // Back-compat counter-check: sources that don't opt in keep the
            // deterministic countdown regardless of stats.
            var victim = MakeVictim(toughness: 60);
            Assert.IsTrue(victim.ApplyEffect(new StunnedEffect(duration: 2)));

            FireTurnEnd(victim);
            Assert.IsTrue(victim.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                "no saveTarget → no save, even at Toughness 60");

            FireTurnEnd(victim);
            Assert.IsFalse(victim.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>());
        }

        [Test]
        public void ElectrifiedContactStun_IsTwoTurnCeilingWithSave()
        {
            // The user-directed retune: the Electrified contact-stun is a
            // 2-turn ceiling WITH a Toughness save (was a flat 1 turn).
            var victim = MakeVictim(toughness: 10);
            Assert.IsTrue(victim.ApplyEffect(new ElectrifiedEffect(1f)));

            var stun = victim.GetPart<StatusEffectsPart>().GetEffect<StunnedEffect>();
            Assert.IsNotNull(stun, "electrified contact still stuns");
            Assert.AreEqual(2, stun.Duration, "ceiling raised to 2 turns");
            Assert.Greater(stun.SaveTarget, 0, "contact stun opts into the Toughness save");
        }

        [Test]
        public void Stack_ExtendsCeiling_AndKeepsTheWorseSaveTarget()
        {
            var victim = MakeVictim(toughness: 10);
            Assert.IsTrue(victim.ApplyEffect(new StunnedEffect(duration: 2, saveTarget: 10)));
            Assert.IsTrue(victim.ApplyEffect(new StunnedEffect(duration: 1, saveTarget: 18)));

            var stun = victim.GetPart<StatusEffectsPart>().GetEffect<StunnedEffect>();
            Assert.AreEqual(3, stun.Duration, "stacking extends the ceiling");
            Assert.AreEqual(18, stun.SaveTarget, "the harder save wins the merge");
        }

        /// <summary>Captures the Cause of EffectRemoved events.</summary>
        private class CauseProbe : Part
        {
            public override string Name => "CauseProbe";
            public string LastCapturedCause { get; private set; }

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "EffectRemoved")
                    LastCapturedCause = e.GetStringParameter("Cause");
                return true;
            }
        }
    }
}
