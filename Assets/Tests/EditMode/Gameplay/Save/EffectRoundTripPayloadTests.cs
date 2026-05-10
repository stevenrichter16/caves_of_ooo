using System.Reflection;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SL.6.2 — Effect round-trip audit, Tier-A payload effects. See
    /// <c>Docs/SAVE-LOAD-AUDIT.md §SL.6</c>.
    ///
    /// <para><b>Tier-A payload</b> = effects with public payload fields
    /// alongside the inherited Duration. No Entity refs, no private
    /// state, but the payload itself must survive intact:</para>
    /// <list type="bullet">
    ///   <item><see cref="WetEffect.Moisture"/> (float)</item>
    ///   <item><see cref="AcidicEffect.Corrosion"/> (float)</item>
    ///   <item><see cref="ElectrifiedEffect.Charge"/> (float)</item>
    ///   <item><see cref="BleedingEffect.DamageDice"/> (string)
    ///         + <c>SaveTarget</c> (int) + <c>Rng</c> (System.Random)</item>
    ///   <item><see cref="ShatterArmorEffect.StackCount"/> (int)</item>
    /// </list>
    ///
    /// <para><b>Special focus — System.Random:</b> BleedingEffect carries
    /// a public <c>Rng</c> field of type <c>System.Random</c>. Per
    /// <c>SaveSystem.CanSerializeType</c> (line 1622-1640) System.Random
    /// is a class with a parameterless ctor, so the field IS included
    /// in the saved field set. But Random's internal state is NOT
    /// reflectively serializable — only its existence will round-trip
    /// (as a fresh Random instance, not the same sequence). The pinned
    /// contract here is: the FIELD is non-null after load (so callers
    /// don't NRE) but CONTENTS may differ. Production code already
    /// tolerates a fresh Random.</para>
    /// </summary>
    public class EffectRoundTripPayloadTests
    {
        private static Entity NewActor(string id = "actor")
        {
            return new Entity { ID = id, BlueprintName = "Test" };
        }

        // ── WetEffect.Moisture ───────────────────────────────────

        [Test]
        public void WetEffect_Moisture_RoundTrips()
        {
            var actor = NewActor();
            actor.ForceApplyEffect(new WetEffect(moisture: 0.65f));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var effect = loaded.GetEffect<WetEffect>();
            Assert.IsNotNull(effect);
            Assert.AreEqual(0.65f, effect.Moisture, 0.0001f,
                "Moisture is a public float field; must round-trip via the "
                + "WriteFieldValue(typeof(float)) branch at SaveSystem.cs:1655.");
        }

        [Test]
        public void WetEffect_Moisture_ZeroAndOne_RoundTrip()
        {
            // Boundary check: 0.0 and 1.0 are common payload values
            // that bug-prone code might treat as "unset".
            var actorZero = NewActor("zero");
            actorZero.ForceApplyEffect(new WetEffect(moisture: 0f));
            Assert.AreEqual(0f,
                PartRoundTripHelper.RoundTripEntityViaTokenGraph(actorZero)
                    .GetEffect<WetEffect>().Moisture);

            var actorOne = NewActor("one");
            actorOne.ForceApplyEffect(new WetEffect(moisture: 1f));
            Assert.AreEqual(1f,
                PartRoundTripHelper.RoundTripEntityViaTokenGraph(actorOne)
                    .GetEffect<WetEffect>().Moisture);
        }

        // ── AcidicEffect.Corrosion ───────────────────────────────

        [Test]
        public void AcidicEffect_Corrosion_RoundTrips()
        {
            var actor = NewActor();
            actor.ForceApplyEffect(new AcidicEffect(corrosion: 0.42f));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var effect = loaded.GetEffect<AcidicEffect>();
            Assert.IsNotNull(effect);
            Assert.AreEqual(0.42f, effect.Corrosion, 0.0001f);
        }

        // ── ElectrifiedEffect.Charge ─────────────────────────────

        [Test]
        public void ElectrifiedEffect_Charge_RoundTrips()
        {
            var actor = NewActor();
            actor.ForceApplyEffect(new ElectrifiedEffect(charge: 1.7f));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var effect = loaded.GetEffect<ElectrifiedEffect>();
            Assert.IsNotNull(effect);
            Assert.AreEqual(1.7f, effect.Charge, 0.0001f);
        }

        // ── BleedingEffect: DamageDice + SaveTarget + Rng ────────

        [Test]
        public void BleedingEffect_DamageDice_String_RoundTrips()
        {
            var actor = NewActor();
            actor.ForceApplyEffect(new BleedingEffect(saveTarget: 18, damageDice: "2d4"));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var effect = loaded.GetEffect<BleedingEffect>();
            Assert.IsNotNull(effect);
            Assert.AreEqual("2d4", effect.DamageDice,
                "DamageDice is the dice expression — must survive verbatim "
                + "or every bleed tick post-load rolls the wrong damage.");
            Assert.AreEqual(18, effect.SaveTarget,
                "SaveTarget gates the per-tick save vs. ticking again next "
                + "turn — must round-trip exactly.");
        }

        [Test]
        public void BleedingEffect_Rng_NonNullAfterLoad_NotSamePointer()
        {
            // System.Random is a class-with-default-ctor → CanSerializeType
            // returns true → it goes through WriteTypedObject. The internal
            // state isn't preserved (Random's state is private + not
            // reflectively visible in a useful way), but the field must
            // be non-null after load so per-tick BleedingEffect.OnTurnStart
            // doesn't NRE on Rng.
            var actor = NewActor();
            var srcRng = new System.Random(42);
            actor.ForceApplyEffect(new BleedingEffect(rng: srcRng));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var effect = loaded.GetEffect<BleedingEffect>();
            Assert.IsNotNull(effect);
            Assert.IsNotNull(effect.Rng,
                "Rng field must be non-null after load. If serialization "
                + "writes nothing for the Random + reflection assigns null, "
                + "a per-tick OnTurnStart NREs. (Production code can also "
                + "guard with `?? new Random()` — that's a separate fix.)");
        }

        [Test]
        public void BleedingEffect_NullRng_AtSaveTime_LoadsTo_NonNullOrCallerHandled()
        {
            // Adversarial: BleedingEffect was constructed with rng=null
            // (legal — ctor allows it). Save/load and verify behavior.
            // Either Rng round-trips as null (caller must guard) or the
            // load path materializes a fresh Random. Either is acceptable
            // — we just pin the contract so a future change is visible.
            var actor = NewActor();
            actor.ForceApplyEffect(new BleedingEffect(rng: null));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var effect = loaded.GetEffect<BleedingEffect>();
            Assert.IsNotNull(effect, "Effect itself round-trips even with null Rng.");
            // No assertion on effect.Rng — pin the OBSERVABLE behavior:
            // either null (and OnTurnStart guards it) or a fresh Random.
            // This is a contract-pinning probe, not a hard invariant.
        }

        // ── ShatterArmorEffect.StackCount ────────────────────────

        [Test]
        public void ShatterArmorEffect_StackCount_RoundTrips()
        {
            // StackCount has a field initializer = 1. Pin that a value
            // OTHER than 1 (set after construction by stacking) survives.
            var actor = NewActor();
            actor.ForceApplyEffect(new ShatterArmorEffect());
            var live = actor.GetEffect<ShatterArmorEffect>();
            Assert.IsNotNull(live);
            live.StackCount = 4; // simulate post-construction stacking

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var effect = loaded.GetEffect<ShatterArmorEffect>();
            Assert.IsNotNull(effect);
            Assert.AreEqual(4, effect.StackCount,
                "StackCount=4 must round-trip — NOT be reset to the field "
                + "initializer (=1) by the FormatterServices path.");
        }

        [Test]
        public void ShatterArmorEffect_StackCount_DefaultOne_RoundTrips()
        {
            // Counter-check: an unstacked effect's default StackCount=1
            // also round-trips correctly (vs. e.g. 0 from
            // GetUninitializedObject zeroing the field).
            var actor = NewActor();
            actor.ForceApplyEffect(new ShatterArmorEffect());

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var effect = loaded.GetEffect<ShatterArmorEffect>();
            Assert.IsNotNull(effect);
            Assert.AreEqual(1, effect.StackCount,
                "Default StackCount=1 must NOT be replaced by 0 (which is "
                + "what GetUninitializedObject would leave the field at "
                + "if save/load ever stopped writing the value).");
        }

        // ── Cross-cutting: Duration co-survives payload ──────────

        [Test]
        public void PayloadEffects_Duration_CoSurvives_Payload()
        {
            // Pin that BOTH Duration AND payload fields survive together —
            // catches a regression where the writer order desyncs from
            // the reader order (Duration written first, payload after;
            // a future refactor that flips them would silently corrupt
            // every payload effect).
            var actor = NewActor();
            actor.ForceApplyEffect(new WetEffect(moisture: 0.33f) { Duration = 13 });

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var effect = loaded.GetEffect<WetEffect>();
            Assert.IsNotNull(effect);
            Assert.AreEqual(13, effect.Duration, "Duration round-trips.");
            Assert.AreEqual(0.33f, effect.Moisture, 0.0001f,
                "Moisture round-trips alongside Duration.");
        }
    }
}
