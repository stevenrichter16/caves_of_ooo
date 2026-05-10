using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SL.6.1 — Effect round-trip audit, Tier-A simple effects. See
    /// <c>Docs/SAVE-LOAD-AUDIT.md §SL.6</c> for the audit plan.
    ///
    /// <para><b>Tier-A simple</b> = effects whose only persistent state
    /// is <c>Duration</c> (inherited from <see cref="Effect"/>) and
    /// whose default constructor argument is the only ctor parameter.
    /// They have no public payload fields, no Entity references, and
    /// no override of <c>OnBeforeSave</c> / <c>OnAfterSave</c>.</para>
    ///
    /// <para><b>What's pinned here:</b>
    /// <list type="bullet">
    ///   <item>The effect's TYPE round-trips — same concrete class
    ///         after load (proves
    ///         <see cref="System.Runtime.Serialization.FormatterServices.GetUninitializedObject"/>
    ///         used in <c>SaveSystem.LoadEffect</c> works for
    ///         parameterized constructors).</item>
    ///   <item><c>Duration</c> round-trips with the exact value the
    ///         effect was applied with (the
    ///         <c>writer.Write(effect.Duration)</c> /
    ///         <c>reader.ReadInt()</c> path at SaveSystem.cs:1182,1201).</item>
    ///   <item>The effect's <c>Owner</c> field is set on load (NOT
    ///         saved by SaveEffect; restored when <c>StatusEffectsPart</c>
    ///         re-binds effects to their host entity).</item>
    ///   <item>Stacking semantics survive — applying the same effect
    ///         twice extends Duration before save; the extended
    ///         Duration round-trips intact.</item>
    /// </list>
    /// </para>
    ///
    /// <para>Counter-check: an effect that was NEVER applied is NOT
    /// present on the loaded entity. Without this, all positive tests
    /// could pass on a buggy build that always returns the same effect
    /// regardless of what was saved.</para>
    /// </summary>
    public class EffectRoundTripSimpleTests
    {
        // ── Shared helpers ────────────────────────────────────────

        private static Entity NewActor(string id = "actor")
        {
            return new Entity { ID = id, BlueprintName = "Test" };
        }

        // ── Type-by-type positive round-trip ──────────────────────

        [Test]
        public void RootedEffect_RoundTrip_PreservesType_And_Duration()
        {
            var actor = NewActor();
            actor.ForceApplyEffect(new RootedEffect(duration: 7));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var effect = loaded.GetEffect<RootedEffect>();
            Assert.IsNotNull(effect, "RootedEffect must survive a save+load cycle.");
            Assert.AreEqual(7, effect.Duration,
                "Duration must round-trip exactly — saved separately at SaveSystem.cs:1182.");
        }

        [Test]
        public void BrokenEffect_RoundTrip_PreservesType_And_Duration()
        {
            var actor = NewActor();
            actor.ForceApplyEffect(new BrokenEffect(duration: 11));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var effect = loaded.GetEffect<BrokenEffect>();
            Assert.IsNotNull(effect);
            Assert.AreEqual(11, effect.Duration);
        }

        [Test]
        public void SmolderingEffect_RoundTrip_PreservesType_And_Duration()
        {
            var actor = NewActor();
            actor.ForceApplyEffect(new SmolderingEffect(duration: 6));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var effect = loaded.GetEffect<SmolderingEffect>();
            Assert.IsNotNull(effect);
            Assert.AreEqual(6, effect.Duration);
        }

        [Test]
        public void ConfusedEffect_RoundTrip_PreservesType_And_Duration()
        {
            var actor = NewActor();
            actor.ForceApplyEffect(new ConfusedEffect(duration: 3));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var effect = loaded.GetEffect<ConfusedEffect>();
            Assert.IsNotNull(effect);
            Assert.AreEqual(3, effect.Duration);
        }

        [Test]
        public void HobbledEffect_RoundTrip_PreservesType_And_Duration()
        {
            var actor = NewActor();
            actor.ForceApplyEffect(new HobbledEffect(duration: 9));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var effect = loaded.GetEffect<HobbledEffect>();
            Assert.IsNotNull(effect);
            Assert.AreEqual(9, effect.Duration);
        }

        [Test]
        public void StunnedEffect_RoundTrip_PreservesType_And_Duration()
        {
            var actor = NewActor();
            actor.ForceApplyEffect(new StunnedEffect(duration: 5));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var effect = loaded.GetEffect<StunnedEffect>();
            Assert.IsNotNull(effect);
            Assert.AreEqual(5, effect.Duration);
        }

        [Test]
        public void ParalyzedEffect_RoundTrip_PreservesType_And_Duration()
        {
            var actor = NewActor();
            actor.ForceApplyEffect(new ParalyzedEffect(duration: 4));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var effect = loaded.GetEffect<ParalyzedEffect>();
            Assert.IsNotNull(effect);
            Assert.AreEqual(4, effect.Duration);
        }

        // ── Owner re-binding (cross-cutting) ──────────────────────

        [Test]
        public void Effect_Owner_IsRebound_To_LoadedEntity()
        {
            // Owner is excluded from SaveEffect (filter at line 1183)
            // and never restored by LoadEffect — but StatusEffectsPart's
            // load-side hook re-binds each effect's Owner to the host
            // entity. Pin that contract: after load, effect.Owner
            // points to the loaded Entity, not to a leftover null.
            var actor = NewActor("owner-test");
            actor.ForceApplyEffect(new RootedEffect(duration: 4));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var effect = loaded.GetEffect<RootedEffect>();
            Assert.IsNotNull(effect);
            Assert.AreSame(loaded, effect.Owner,
                "Effect.Owner must be rebound to the loaded Entity, not "
                + "left null. SaveSystem excludes Owner from the field "
                + "filter (line 1183); StatusEffectsPart re-binds it.");
        }

        // ── Stacking semantics survive round-trip ─────────────────

        [Test]
        public void StunnedEffect_Stacked_DurationExtension_RoundTrips()
        {
            // Pre-save: apply stun twice. The second OnStack extends
            // Duration. Round-trip; verify the extended Duration
            // (not the first apply's duration) survives.
            var actor = NewActor("stack-test");
            actor.ForceApplyEffect(new StunnedEffect(duration: 2));
            int afterFirst = actor.GetEffect<StunnedEffect>().Duration;

            actor.ForceApplyEffect(new StunnedEffect(duration: 3));
            int afterSecond = actor.GetEffect<StunnedEffect>().Duration;
            Assert.Greater(afterSecond, afterFirst,
                "Setup precondition: a second StunnedEffect application "
                + "must extend the existing one's Duration via OnStack.");

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var loadedEffect = loaded.GetEffect<StunnedEffect>();
            Assert.AreEqual(afterSecond, loadedEffect.Duration,
                "The post-stack Duration must round-trip — proves we save "
                + "the LIVE effect state (Duration after OnStack), not the "
                + "value it was constructed with.");
        }

        // ── Counter-checks: confirm contracts are non-vacuous ─────

        [Test]
        public void CounterCheck_NoEffectApplied_Then_LoadHasNoEffect()
        {
            // Without this, every "loaded.GetEffect<X>() != null" test
            // could pass on a buggy build that always returns a fresh
            // X regardless of what was saved.
            var actor = NewActor("clean-actor");
            // No ApplyEffect call.

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.IsFalse(loaded.HasEffect<RootedEffect>(),
                "Entity that was never given a RootedEffect must not "
                + "have one after load. Catches a buggy default-construct "
                + "in StatusEffectsPart's load handler.");
            Assert.IsFalse(loaded.HasEffect<StunnedEffect>());
            Assert.IsFalse(loaded.HasEffect<ConfusedEffect>());
        }

        [Test]
        public void CounterCheck_WrongEffectType_DoesNotMatch_LoadedEffect()
        {
            // Apply RootedEffect; assert the loaded entity has it BUT
            // NOT, e.g., StunnedEffect. Catches a buggy load that ignores
            // the saved type name and always reifies the same kind.
            var actor = NewActor();
            actor.ForceApplyEffect(new RootedEffect(duration: 4));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            Assert.IsTrue(loaded.HasEffect<RootedEffect>(),
                "RootedEffect was applied — must be present after load.");
            Assert.IsFalse(loaded.HasEffect<StunnedEffect>(),
                "StunnedEffect was NEVER applied — must NOT be present.");
            Assert.IsFalse(loaded.HasEffect<ConfusedEffect>());
            Assert.IsFalse(loaded.HasEffect<HobbledEffect>());
        }

        [Test]
        public void DurationZero_ExpiresImmediately_BothBeforeAndAfterLoad()
        {
            // Adversarial: what if Duration is 0 at save time? The
            // effect should still round-trip (data is data), even
            // though gameplay code may remove it on the next tick.
            // Pin the data contract: 0 round-trips as 0, not as the
            // ctor-default value (which would be a bug in
            // FormatterServices.GetUninitializedObject + ReadPublicFields
            // path if anything were broken).
            var actor = NewActor();
            actor.ForceApplyEffect(new RootedEffect(duration: 0));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var effect = loaded.GetEffect<RootedEffect>();
            Assert.IsNotNull(effect, "Duration=0 effect should still round-trip.");
            Assert.AreEqual(0, effect.Duration,
                "Duration=0 must NOT be replaced by the ctor default (4). "
                + "Catches a bug where ReadInt returns 0 but the load path "
                + "treats 0 as 'unset' and falls back to the ctor default.");
        }
    }
}
