using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Pins the contract of <see cref="Effect.LastRemovalCause"/> across the
    /// three known cause paths:
    ///
    ///   1. CAUSE_DURATION_EXPIRED (default) — a non-save effect ticks down
    ///      to Duration=0 via the standard <see cref="Effect.OnTurnEnd"/>.
    ///   2. CAUSE_SAVE_SUCCEEDED — a save-based effect rolls a successful
    ///      save in its OnTurnEnd and sets the cause before Duration=0.
    ///   3. CAUSE_EXTERNAL — a public <see cref="StatusEffectsPart.RemoveEffect"/>
    ///      overload is invoked, tagging the effect with the external cause.
    ///
    /// And pins that the cause flows through to the <c>EffectRemoved</c>
    /// event's <c>Cause</c> parameter so listeners (probes, UI, achievements)
    /// can read it without per-type heuristics.
    /// </summary>
    public class EffectRemovalCauseTests
    {
        [SetUp]
        public void Setup() => MessageLog.Clear();

        // ====================================================================
        // 1. Default: duration-tick removal carries CAUSE_DURATION_EXPIRED
        // ====================================================================

        [Test]
        public void StunnedEffect_OnDurationExpire_CauseIsDurationExpired()
        {
            var entity = MakeEntity();
            var sep = entity.GetPart<StatusEffectsPart>();
            var probe = new RemovalCauseProbe();
            entity.AddPart(probe);

            sep.ApplyEffect(new StunnedEffect(duration: 1));
            Assert.IsTrue(sep.HasEffect<StunnedEffect>());
            Assert.AreEqual(Effect.CAUSE_DURATION_EXPIRED, sep.GetEffect<StunnedEffect>().LastRemovalCause,
                "Default cause should be CAUSE_DURATION_EXPIRED on a fresh effect.");

            FireTurnEnd(entity);

            Assert.IsFalse(sep.HasEffect<StunnedEffect>(),
                "After turn-end with Duration=1, Stunned should be cleaned up.");
            Assert.AreEqual(Effect.CAUSE_DURATION_EXPIRED, probe.LastCapturedCause,
                "EffectRemoved event should carry CAUSE_DURATION_EXPIRED for normal duration ticks.");
        }

        // ====================================================================
        // 2. Save-based removal: BleedingEffect carries CAUSE_SAVE_SUCCEEDED
        // ====================================================================

        [Test]
        public void BleedingEffect_OnSaveSuccess_CauseIsSaveSucceeded()
        {
            // Force a save success by making the target's Toughness so high
            // it always saves, and using a deterministic RNG.
            var entity = MakeEntity(toughness: 50);  // +20 mod
            var sep = entity.GetPart<StatusEffectsPart>();
            var probe = new RemovalCauseProbe();
            entity.AddPart(probe);

            // SaveTarget=15 vs d20+20 = 21..40 — always >= 15. Save always succeeds.
            sep.ApplyEffect(new BleedingEffect(saveTarget: 15, damageDice: "1d2", rng: new Random(0)));
            Assert.IsTrue(sep.HasEffect<BleedingEffect>());

            FireTurnEnd(entity);

            Assert.IsFalse(sep.HasEffect<BleedingEffect>(),
                "Bleeding should save out on turn-end given Toughness 50 vs DC 15.");
            Assert.AreEqual(Effect.CAUSE_SAVE_SUCCEEDED, probe.LastCapturedCause,
                "EffectRemoved event should carry CAUSE_SAVE_SUCCEEDED when bleeding is saved out.");
        }

        // ====================================================================
        // 3. Counter-check: failing the save → effect persists, no event
        // ====================================================================

        [Test]
        public void BleedingEffect_OnSaveFail_StillBleedingNoRemovalEvent()
        {
            // Make the save impossible: Toughness so low it always fails.
            var entity = MakeEntity(toughness: 1);  // -5 mod
            var sep = entity.GetPart<StatusEffectsPart>();
            var probe = new RemovalCauseProbe();
            entity.AddPart(probe);

            // SaveTarget=20 vs d20-5 = -4..15 — never >= 20. Save always fails.
            sep.ApplyEffect(new BleedingEffect(saveTarget: 20, damageDice: "1d2", rng: new Random(0)));

            FireTurnEnd(entity);

            Assert.IsTrue(sep.HasEffect<BleedingEffect>(),
                "Bleeding should persist on failed save.");
            Assert.IsNull(probe.LastCapturedCause,
                "EffectRemoved should not fire for a failed save.");
        }

        // ====================================================================
        // 4. External removal: any RemoveEffect overload tags CAUSE_EXTERNAL
        // ====================================================================

        [Test]
        public void RemoveEffectByType_TagsCauseExternal()
        {
            var entity = MakeEntity();
            var sep = entity.GetPart<StatusEffectsPart>();
            var probe = new RemovalCauseProbe();
            entity.AddPart(probe);
            sep.ApplyEffect(new StunnedEffect(duration: 5));

            bool removed = sep.RemoveEffect<StunnedEffect>();

            Assert.IsTrue(removed);
            Assert.AreEqual(Effect.CAUSE_EXTERNAL, probe.LastCapturedCause,
                "Public RemoveEffect<T> should tag the cause as CAUSE_EXTERNAL.");
        }

        [Test]
        public void RemoveEffectByPredicate_TagsCauseExternal()
        {
            var entity = MakeEntity();
            var sep = entity.GetPart<StatusEffectsPart>();
            var probe = new RemovalCauseProbe();
            entity.AddPart(probe);
            sep.ApplyEffect(new StunnedEffect(duration: 5));

            bool removed = sep.RemoveEffect(ef => ef is StunnedEffect);
            Assert.IsTrue(removed);
            Assert.AreEqual(Effect.CAUSE_EXTERNAL, probe.LastCapturedCause);
        }

        [Test]
        public void RemoveEffectByInstance_TagsCauseExternal()
        {
            var entity = MakeEntity();
            var sep = entity.GetPart<StatusEffectsPart>();
            var probe = new RemovalCauseProbe();
            entity.AddPart(probe);
            var stun = new StunnedEffect(duration: 5);
            sep.ApplyEffect(stun);

            bool removed = sep.RemoveEffect(stun);
            Assert.IsTrue(removed);
            Assert.AreEqual(Effect.CAUSE_EXTERNAL, probe.LastCapturedCause);
        }

        // ====================================================================
        // 5. Adversarial: stacking re-application doesn't fire spurious removals
        // ====================================================================

        [Test]
        public void StackingStunned_DoesNotFireRemovalEvent()
        {
            // Stunned.OnStack returns true and extends duration in-place;
            // no removal event should fire because the existing effect
            // wasn't actually removed.
            var entity = MakeEntity();
            var sep = entity.GetPart<StatusEffectsPart>();
            var probe = new RemovalCauseProbe();
            entity.AddPart(probe);

            sep.ApplyEffect(new StunnedEffect(duration: 2));
            sep.ApplyEffect(new StunnedEffect(duration: 3));  // stacks

            Assert.IsTrue(sep.HasEffect<StunnedEffect>());
            Assert.AreEqual(0, probe.RemovalEventCount,
                "Stacking should not fire a spurious EffectRemoved event.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Entity MakeEntity(int toughness = 14)
        {
            var e = new Entity { ID = "fighter" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = 100, Min = 0, Max = 100 };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = toughness, Min = 1, Max = 50 };
            e.Statistics["Agility"] = new Stat
                { Owner = e, Name = "Agility", BaseValue = 10, Min = 0, Max = 50 };
            e.Statistics["DV"] = new Stat
                { Owner = e, Name = "DV", BaseValue = 0, Min = -50, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = "fighter" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static void FireTurnEnd(Entity entity)
        {
            // Event ID is "EndTurn", not "TurnEnd" (per StatusEffectsPart.cs:281).
            var ev = GameEvent.New("EndTurn");
            ev.SetParameter("Target", (object)entity);
            entity.FireEvent(ev);
        }

        /// <summary>
        /// Test-only Part: captures EffectRemoved events and exposes the
        /// last received Cause string for assertion.
        /// </summary>
        private class RemovalCauseProbe : Part
        {
            public override string Name => "RemovalCauseProbe";
            public string LastCapturedCause { get; private set; }
            public int RemovalEventCount { get; private set; }

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "EffectRemoved")
                {
                    // GetStringParameter reads from the StringParameters dict
                    // (where SetParameter(name, string) writes); the typed
                    // GetParameter<string> only reads Parameters and would
                    // return null even when the Cause is set.
                    LastCapturedCause = e.GetStringParameter("Cause", null);
                    RemovalEventCount++;
                }
                return true;
            }
        }
    }
}
