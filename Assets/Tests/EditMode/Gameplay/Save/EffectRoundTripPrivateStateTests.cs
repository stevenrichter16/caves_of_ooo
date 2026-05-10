using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SL.6.4 — Effect round-trip audit, Tier-C effects with private
    /// state or non-default constructors. See
    /// <c>Docs/SAVE-LOAD-AUDIT.md §SL.6</c>.
    ///
    /// <para><b>Tier-C</b> = effects whose serialization is structurally
    /// at risk because either:</para>
    /// <list type="bullet">
    ///   <item>They use <c>{ get; private set; }</c> properties whose
    ///         compiler-generated backing fields are private (and so
    ///         silently skipped by <c>WritePublicFields</c> at
    ///         <c>SaveSystem.cs:1593</c>).</item>
    ///   <item>They have only parameterized constructors — load uses
    ///         <c>FormatterServices.GetUninitializedObject</c> to bypass
    ///         the ctor entirely, so any field-initializer or
    ///         constructor-set state is lost unless serialized.</item>
    ///   <item>They keep private state that is intentionally NOT
    ///         persisted (regenerated on load).</item>
    /// </list>
    ///
    /// <para><b>Findings:</b></para>
    /// <list type="bullet">
    ///   <item>🔴 <c>HibernatingEffect.PriorHeatResistance</c>,
    ///         <c>PriorColdResistance</c> — public properties with
    ///         private setters. Backing fields are private → save drops
    ///         them. Loading a hibernating creature wakes it up with
    ///         resistances stuck at the +100 buff (OnRemove sees the
    ///         -1 sentinel because it wasn't persisted). <b>Fix in
    ///         the same commit:</b> convert to public fields with a
    ///         note that they're persistence-managed.</item>
    ///   <item>⚪ <c>CharredEffect._originalCombustibility</c>,
    ///         <c>_hasStoredOriginal</c> — fully private. Intentional
    ///         per source (OnApply re-captures on load; design
    ///         choice). Pin it as ⚪ so a future contributor doesn't
    ///         "fix" the dropped state.</item>
    /// </list>
    /// </summary>
    public class EffectRoundTripPrivateStateTests
    {
        private static Entity NewActor(string id = "actor")
        {
            return new Entity { ID = id, BlueprintName = "Test" };
        }

        // ── A. HibernatingEffect prior-resistance round-trip (was 🔴) ──

        [Test]
        public void HibernatingEffect_PriorHeatResistance_RoundTrips()
        {
            // Pre-load: simulate a creature that entered hibernation
            // with HeatResistance=25 → OnApply captures
            // PriorHeatResistance=25 → bumps live HeatResistance to 100.
            //
            // We bypass OnApply/target stat-bumps and just set the
            // prior-resistance values directly via reflection, since
            // those properties have a PRIVATE setter. This isolates
            // the round-trip contract from OnApply's stat-bump logic.
            var actor = NewActor("hibernator");
            actor.ForceApplyEffect(new HibernatingEffect(duration: 8));
            // Set values AFTER ForceApplyEffect — OnApply runs during
            // apply and writes whatever the actor's HeatResistance/
            // ColdResistance stats currently are (0 for a synthetic
            // test actor). Setting after-the-fact mirrors the live
            // mid-hibernation save state we want to pin.
            var live = actor.GetEffect<HibernatingEffect>();
            live.PriorHeatResistance = 25;
            live.PriorColdResistance = -10;

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var loadedHib = loaded.GetEffect<HibernatingEffect>();
            Assert.IsNotNull(loadedHib);
            Assert.AreEqual(25, loadedHib.PriorHeatResistance,
                "PriorHeatResistance must round-trip — it's the value "
                + "OnRemove restores when the creature wakes up. If this "
                + "regresses to -1 (the sentinel), OnRemove silently skips "
                + "the restore and the creature is stuck at +100 resistance.");
            Assert.AreEqual(-10, loadedHib.PriorColdResistance,
                "PriorColdResistance same contract.");
        }

        [Test]
        public void HibernatingEffect_NegativeValues_RoundTripFaithfully()
        {
            // Counter-check: PriorHeatResistance can legitimately be
            // negative if the creature has heat-vulnerability before
            // hibernation. Pin that negative ints round-trip — catches
            // a bug where the int serialization treats negatives as
            // unsigned and corrupts to a huge positive (or vice versa).
            var actor = NewActor("vuln");
            actor.ForceApplyEffect(new HibernatingEffect(duration: 4));
            var live = actor.GetEffect<HibernatingEffect>();
            live.PriorHeatResistance = -50;
            live.PriorColdResistance = -25;

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var loadedHib = loaded.GetEffect<HibernatingEffect>();
            Assert.AreEqual(-50, loadedHib.PriorHeatResistance,
                "Negative PriorHeatResistance must round-trip exactly.");
            Assert.AreEqual(-25, loadedHib.PriorColdResistance);
        }

        // ── B. PoisonedEffect.DamageDice + System.Random ──────────

        [Test]
        public void PoisonedEffect_DamageDice_RoundTrips()
        {
            var actor = NewActor();
            actor.ForceApplyEffect(new PoisonedEffect(duration: 6, damageDice: "1d4"));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var effect = loaded.GetEffect<PoisonedEffect>();
            Assert.IsNotNull(effect);
            Assert.AreEqual("1d4", effect.DamageDice);
            Assert.AreEqual(6, effect.Duration);
        }

        [Test]
        public void PoisonedEffect_NonDefaultCtor_FormatterServicesPath_Works()
        {
            // PoisonedEffect has a parameterized-only ctor pattern (well,
            // with all-default args, but ResolveType + GetUninitializedObject
            // bypasses the ctor entirely). Pin that the bypass works:
            // the loaded effect is the right type AND its fields are
            // populated from the saved bytes (not left at uninitialized
            // defaults).
            var actor = NewActor();
            actor.ForceApplyEffect(new PoisonedEffect(duration: 99, damageDice: "5d6"));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var effect = loaded.GetEffect<PoisonedEffect>();
            Assert.IsInstanceOf<PoisonedEffect>(effect);
            Assert.AreEqual(99, effect.Duration);
            Assert.AreEqual("5d6", effect.DamageDice,
                "DamageDice must NOT be the GetUninitializedObject default "
                + "(null) — must come from the saved bytes.");
        }

        // ── C. FrozenEffect.Cold ──────────────────────────────────

        [Test]
        public void FrozenEffect_Cold_RoundTrips()
        {
            var actor = NewActor();
            actor.ForceApplyEffect(new FrozenEffect(cold: 0.85f));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var effect = loaded.GetEffect<FrozenEffect>();
            Assert.IsNotNull(effect);
            Assert.AreEqual(0.85f, effect.Cold, 0.0001f);
        }

        // ── D. CharredEffect: private state intentionally not preserved ──

        [Test]
        public void CharredEffect_PrivateState_IntentionallyNotPersisted()
        {
            // CharredEffect tracks `_originalCombustibility` and
            // `_hasStoredOriginal` as fully private fields. By design
            // (per source comment), OnApply re-captures these on load
            // — they're NOT meant to round-trip. Pin the contract so
            // a future contributor doesn't "fix" the dropped state
            // and break the OnApply re-capture logic.
            var actor = NewActor();
            actor.ForceApplyEffect(new CharredEffect());

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var effect = loaded.GetEffect<CharredEffect>();
            Assert.IsNotNull(effect, "CharredEffect itself round-trips.");

            // The private fields were not serialized → they're at
            // GetUninitializedObject defaults (false / 0).
            // Probe via reflection to PIN the contract.
            bool hasStored = (bool)typeof(CharredEffect)
                .GetField("_hasStoredOriginal",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(effect);
            Assert.IsFalse(hasStored,
                "CharredEffect._hasStoredOriginal stays false on load — "
                + "intentional. OnApply re-captures original combustibility "
                + "post-load. If this changes to true, the OnApply re-capture "
                + "would be skipped (treating saved garbage as 'already captured').");
        }

    }
}
