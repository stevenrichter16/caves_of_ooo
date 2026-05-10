using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SL.6.3 — Effect round-trip audit, Tier-B effects with Entity
    /// references. See <c>Docs/SAVE-LOAD-AUDIT.md §SL.6</c>.
    ///
    /// <para><b>Tier-B</b> = effects with at least one public field of
    /// type <c>Entity</c>. The reflection path at
    /// <c>SaveSystem.WriteFieldValue:1662</c> routes those through
    /// <c>WriteEntityReference</c> — but the referenced entity's BODY
    /// only round-trips if the SAME save graph queues + flushes it.
    /// We use <c>RoundTripEntityViaTokenGraph</c> (SL.4 helper) which
    /// queues source+descendants and runs OnAfterLoad/FinalizeLoad —
    /// the same path the production save system uses.</para>
    ///
    /// <para><b>Targets:</b>
    /// <list type="bullet">
    ///   <item><see cref="BurningEffect.IgnitionSource"/> (the attacker
    ///         that set us on fire — used by burn-damage attribution)</item>
    ///   <item><see cref="HookedEffect.Hooker"/> (the entity holding the
    ///         hook — used by per-turn pull-toward-hooker behavior)</item>
    /// </list></para>
    /// </summary>
    public class EffectRoundTripEntityRefTests
    {
        // ── A. BurningEffect.IgnitionSource ─────────────────────

        [Test]
        public void BurningEffect_IgnitionSource_RoundTrips_To_LoadedEntity()
        {
            // Setup: defender has a BurningEffect whose IgnitionSource
            // points at attacker. After round-trip we expect the loaded
            // defender's BurningEffect.IgnitionSource to be a non-null
            // Entity with the attacker's ID — proving WriteEntityReference
            // queued + ReadEntityReference resolved the back-pointer.
            var attacker = new Entity { ID = "attacker-id", BlueprintName = "Attacker" };
            var defender = new Entity { ID = "defender-id", BlueprintName = "Defender" };
            defender.ForceApplyEffect(new BurningEffect(intensity: 1.5f, source: attacker));

            // Pre-condition: the live effect knows who lit it.
            Assert.AreSame(attacker, defender.GetEffect<BurningEffect>().IgnitionSource);

            var loadedDefender = PartRoundTripHelper.RoundTripEntityViaTokenGraph(defender);
            var loadedEffect = loadedDefender.GetEffect<BurningEffect>();
            Assert.IsNotNull(loadedEffect, "BurningEffect itself round-trips.");
            Assert.IsNotNull(loadedEffect.IgnitionSource,
                "IgnitionSource is a public Entity field — must round-trip "
                + "via WriteEntityReference at SaveSystem.cs:1662, NOT be "
                + "left null. Burn-attribution depends on this back-pointer.");
            Assert.AreEqual("attacker-id", loadedEffect.IgnitionSource.ID,
                "The ID must match the original attacker — a buggy load "
                + "that interns a fresh empty Entity would still pass the "
                + "IsNotNull check above.");
        }

        [Test]
        public void BurningEffect_Intensity_CoSurvives_IgnitionSource()
        {
            // Pin that float payload AND the Entity ref both round-trip
            // in the same effect — catches a bug where the entity-ref
            // serializer consumes too many bytes off the stream and
            // the float field reads garbage.
            var attacker = new Entity { ID = "atk", BlueprintName = "Attacker" };
            var defender = new Entity { ID = "def", BlueprintName = "Defender" };
            defender.ForceApplyEffect(new BurningEffect(intensity: 2.75f, source: attacker));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(defender);
            var effect = loaded.GetEffect<BurningEffect>();
            Assert.AreEqual(2.75f, effect.Intensity, 0.0001f,
                "Intensity round-trips alongside IgnitionSource.");
            Assert.IsNotNull(effect.IgnitionSource);
        }

        [Test]
        public void BurningEffect_NullIgnitionSource_RoundTripsAsNull()
        {
            // Counter-check: a BurningEffect with no source (e.g. lit
            // by environment, not by a creature) must round-trip with
            // IgnitionSource still null — NOT spuriously resolved to
            // some other entity ID.
            var defender = new Entity { ID = "self-lit", BlueprintName = "Defender" };
            defender.ForceApplyEffect(new BurningEffect(intensity: 1f, source: null));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(defender);
            var effect = loaded.GetEffect<BurningEffect>();
            Assert.IsNotNull(effect);
            Assert.IsNull(effect.IgnitionSource,
                "Null IgnitionSource at save time must remain null at load. "
                + "If this regresses to a non-null ID, burn-attribution "
                + "would credit the wrong creature for environmental fire.");
        }

        // ── B. HookedEffect.Hooker ──────────────────────────────

        [Test]
        public void HookedEffect_Hooker_RoundTrips_To_LoadedEntity()
        {
            // Same shape as BurningEffect.IgnitionSource but on
            // HookedEffect — verifies per-effect entity-ref handling
            // isn't a one-off in BurningEffect.
            var hooker = new Entity { ID = "puller-id", BlueprintName = "Puller" };
            var victim = new Entity { ID = "victim-id", BlueprintName = "Victim" };
            victim.ForceApplyEffect(new HookedEffect(duration: 6, hooker: hooker, saveTarget: 22));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(victim);
            var effect = loaded.GetEffect<HookedEffect>();
            Assert.IsNotNull(effect);
            Assert.IsNotNull(effect.Hooker,
                "Hooker is a public Entity field — must round-trip.");
            Assert.AreEqual("puller-id", effect.Hooker.ID);
            Assert.AreEqual(22, effect.SaveTarget,
                "SaveTarget round-trips alongside the entity ref.");
            Assert.AreEqual(6, effect.Duration);
        }

        [Test]
        public void HookedEffect_NullHooker_RoundTripsAsNull()
        {
            var victim = new Entity { ID = "v", BlueprintName = "Victim" };
            victim.ForceApplyEffect(new HookedEffect(duration: 5, hooker: null));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(victim);
            var effect = loaded.GetEffect<HookedEffect>();
            Assert.IsNotNull(effect);
            Assert.IsNull(effect.Hooker,
                "Null Hooker survives as null after round-trip.");
        }

        // ── C. Multi-effect on same actor ────────────────────────

        [Test]
        public void MultipleEntityRefEffects_OnSameActor_BothPreserveTheirRefs()
        {
            // Adversarial: two distinct entity-ref effects on the same
            // defender. Either can independently fail entity-ref resolution.
            // Pin both — catches a bug where the second effect's load
            // mis-reads the first's ref bytes and crosses the wires.
            var attacker = new Entity { ID = "atk", BlueprintName = "Attacker" };
            var puller = new Entity { ID = "pul", BlueprintName = "Puller" };
            var victim = new Entity { ID = "vic", BlueprintName = "Victim" };
            victim.ForceApplyEffect(new BurningEffect(intensity: 1f, source: attacker));
            victim.ForceApplyEffect(new HookedEffect(duration: 4, hooker: puller));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(victim);
            var burning = loaded.GetEffect<BurningEffect>();
            var hooked = loaded.GetEffect<HookedEffect>();
            Assert.IsNotNull(burning?.IgnitionSource);
            Assert.IsNotNull(hooked?.Hooker);
            Assert.AreEqual("atk", burning.IgnitionSource.ID,
                "BurningEffect.IgnitionSource still points at attacker.");
            Assert.AreEqual("pul", hooked.Hooker.ID,
                "HookedEffect.Hooker still points at puller — NOT the attacker. "
                + "If these cross the wires, both effects would still 'work' "
                + "but burn damage would be credited to the puller.");
        }
    }
}
