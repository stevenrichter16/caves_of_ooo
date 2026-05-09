using System.Reflection;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SL.5 — Save/Load Round-Trip Audit, Tier-3 Parts with private /
    /// internal state. See <c>Docs/SAVE-LOAD-AUDIT.md</c> for the
    /// audit plan.
    ///
    /// <para>The save system at <c>SaveSystem.cs:1593</c> uses
    /// <c>BindingFlags.Instance | BindingFlags.Public</c> when reflecting
    /// fields — so ONLY public instance fields round-trip. Private,
    /// internal, protected, and static fields are silently skipped.
    /// On load, <c>Activator.CreateInstance</c> runs the default
    /// constructor (including any field initializers) but never reads
    /// private state from the save buffer.</para>
    ///
    /// <para><b>This is the audit-doc-predicted "highest probability of
    /// finding 🔴 bugs" milestone.</b> Per CLAUDE.md adversarial
    /// methodology, a "bug" here means private state that the GAME
    /// depends on persisting across saves but doesn't. The contract
    /// pinned here is: private state DOES reset to default (or to its
    /// field-initializer value) on load. Tests exist to make this
    /// contract visible to future contributors.</para>
    ///
    /// <para><b>Findings (from source survey, no actual bugs found):</b>
    /// <list type="bullet">
    ///   <item><c>DamageFlashPart._flashFramesRemaining</c> — reset
    ///         to 0 on load. Behavior: flash mid-animation when saved
    ///         is lost, post-load TakeDamage starts a fresh flash. This
    ///         is the CORRECT behavior (resuming a partial flash would
    ///         be janky).</item>
    ///   <item><c>LanternSitePart._renderFrameCounter</c>,
    ///         <c>_proximityMessageShown</c>, <c>_lastAppliedStage</c>,
    ///         <c>_auraStarted</c> — all reset on load. Public fields
    ///         <c>SettlementId</c> + <c>SiteId</c> round-trip. The
    ///         "one-shot proximity message" will fire again if player
    ///         saves + reloads in the same zone — that's intentional
    ///         (welcome-back behavior) per source comment.</item>
    ///   <item><c>MeleeWeaponPart._cachedOnHitEffectSpecs</c> — derived
    ///         from public <c>OnHitEffectsRaw</c>. Private cache is
    ///         silently rebuilt by the property getter on first access
    ///         after load. No bug.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class Tier3PrivateStateRoundTripTests
    {
        // ── Helper: peek at a private/internal instance field via reflection ─

        private static T GetPrivateField<T>(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field,
                $"Test bug: no private field '{fieldName}' on "
                + obj.GetType().Name);
            return (T)field.GetValue(obj);
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field,
                $"Test bug: no private field '{fieldName}' on "
                + obj.GetType().Name);
            field.SetValue(obj, value);
        }

        // ── A. DamageFlashPart: private flash counter resets on load ────

        [Test]
        public void Adversarial_DamageFlashPart_FlashCounterResetsToZeroOnLoad()
        {
            // Setup: simulate a TakeDamage having set _flashFramesRemaining=3
            // (mid-flash). Round-trip; verify the counter resets to 0,
            // NOT to 3.
            var entity = new Entity { ID = "actor", BlueprintName = "Test" };
            entity.AddPart(new DamageFlashPart());
            var flash = entity.GetPart<DamageFlashPart>();

            // Drive the counter via the public TakeDamage event so we
            // mirror the production code path (rather than poking the
            // private field directly).
            var ev = GameEvent.New("TakeDamage");
            ev.SetParameter("Amount", 5);
            flash.HandleEvent(ev);
            Assert.AreEqual(3, GetPrivateField<int>(flash, "_flashFramesRemaining"),
                "Setup: TakeDamage sets _flashFramesRemaining=3 (FlashDuration).");

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(entity);
            var loadedFlash = loaded.GetPart<DamageFlashPart>();
            Assert.IsNotNull(loadedFlash, "DamageFlashPart itself round-trips.");
            Assert.AreEqual(0,
                GetPrivateField<int>(loadedFlash, "_flashFramesRemaining"),
                "Private _flashFramesRemaining resets to 0 (default) on load. "
                + "Mid-flash state is NOT preserved — matches the design "
                + "that a fresh flash should start cleanly post-load.");
        }

        [Test]
        public void Adversarial_DamageFlashPart_PostLoadTakeDamage_FlashesNormally()
        {
            // Counter-check: even if pre-save state had a partial flash,
            // post-load TakeDamage starts a brand-new full-duration flash.
            // Confirms the reset doesn't break the flash mechanism.
            var entity = new Entity { ID = "actor", BlueprintName = "Test" };
            entity.AddPart(new DamageFlashPart());
            var flash = entity.GetPart<DamageFlashPart>();
            flash.HandleEvent(GameEvent.New("TakeDamage").SetParameter("Amount", 1));
            Assert.AreEqual(3, GetPrivateField<int>(flash, "_flashFramesRemaining"));

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(entity);
            var loadedFlash = loaded.GetPart<DamageFlashPart>();
            Assert.AreEqual(0,
                GetPrivateField<int>(loadedFlash, "_flashFramesRemaining"));

            // Trigger another TakeDamage on the loaded entity.
            loadedFlash.HandleEvent(
                GameEvent.New("TakeDamage").SetParameter("Amount", 1));
            Assert.AreEqual(3,
                GetPrivateField<int>(loadedFlash, "_flashFramesRemaining"),
                "Post-load TakeDamage starts a fresh full-duration "
                + "flash — not a partial one.");
        }

        // ── B. LanternSitePart: public fields preserved, private reset ──

        [Test]
        public void Adversarial_LanternSitePart_PublicFields_RoundTripDespitePrivateReset()
        {
            // Public SettlementId + SiteId preserved; the four private
            // fields all reset. Pins the contract that the
            // public-field reflection path picks up the public state
            // independent of any private-state churn.
            var entity = new Entity { ID = "lantern", BlueprintName = "TestLantern" };
            entity.AddPart(new LanternSitePart
            {
                SettlementId = "village_42",
                SiteId = "lantern_3"
            });
            var lantern = entity.GetPart<LanternSitePart>();

            // Mutate all four private fields away from defaults.
            SetPrivateField(lantern, "_renderFrameCounter", 99);
            SetPrivateField(lantern, "_proximityMessageShown", true);
            SetPrivateField(lantern, "_lastAppliedStage", RepairStage.StableRepair);
            SetPrivateField(lantern, "_auraStarted", true);

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(entity);
            var loadedLantern = loaded.GetPart<LanternSitePart>();

            Assert.IsNotNull(loadedLantern);
            // Public fields preserved.
            Assert.AreEqual("village_42", loadedLantern.SettlementId);
            Assert.AreEqual("lantern_3", loadedLantern.SiteId);

            // All four private fields reset to defaults / initializers.
            Assert.AreEqual(0,
                GetPrivateField<int>(loadedLantern, "_renderFrameCounter"),
                "_renderFrameCounter resets to 0 on load (private fields "
                + "skipped by reflection).");
            Assert.AreEqual(false,
                GetPrivateField<bool>(loadedLantern, "_proximityMessageShown"),
                "_proximityMessageShown resets to false — the welcome-back "
                + "message will fire again if player reloads in same zone. "
                + "INTENTIONAL per LanternSitePart docstring.");
            Assert.AreEqual(RepairStage.Fouled,
                GetPrivateField<RepairStage>(loadedLantern, "_lastAppliedStage"),
                "_lastAppliedStage resets to its FIELD-INITIALIZER value "
                + "(RepairStage.Fouled), NOT to the enum default (None). "
                + "Activator.CreateInstance runs the constructor "
                + "(SaveSystem.cs:1138).");
            Assert.AreEqual(false,
                GetPrivateField<bool>(loadedLantern, "_auraStarted"),
                "_auraStarted resets to false — particle aura will "
                + "re-init on next Render frame.");
        }

        // ── C. MeleeWeaponPart: private cache rebuilds correctly after load

        [Test]
        public void Adversarial_MeleeWeaponPart_PrivateLazyCache_RebuildsAfterLoad()
        {
            // The _cachedOnHitEffectSpecs + _cachedOnHitEffectsRawSnapshot
            // private fields together form a lazy cache derived from the
            // public OnHitEffectsRaw string. After load, both private
            // fields are at default (null), but the public OnHitEffectsRaw
            // round-trips. The first call to OnHitEffectsCachedSpecs
            // post-load should rebuild the cache from the raw string.
            var entity = new Entity { ID = "weapon", BlueprintName = "TestSword" };
            entity.AddPart(new MeleeWeaponPart
            {
                OnHitEffectsRaw = "Burning,30,,5,1.0",
                BaseDamage = "1d6"
            });
            var weapon = entity.GetPart<MeleeWeaponPart>();

            // Prime the cache pre-save.
            var preSpecs = weapon.OnHitEffectsCachedSpecs;
            Assert.IsNotNull(preSpecs);
            Assert.AreEqual(1, preSpecs.Count);
            Assert.IsNotNull(
                GetPrivateField<object>(weapon, "_cachedOnHitEffectSpecs"),
                "Setup: cache is populated pre-save.");

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(entity);
            var loadedWeapon = loaded.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(loadedWeapon);

            // Pre-access: the cache should be at default (null).
            Assert.IsNull(
                GetPrivateField<object>(loadedWeapon, "_cachedOnHitEffectSpecs"),
                "Loaded weapon has null _cachedOnHitEffectSpecs — private "
                + "field skipped by reflection load.");

            // First access rebuilds.
            var loadedSpecs = loadedWeapon.OnHitEffectsCachedSpecs;
            Assert.IsNotNull(loadedSpecs,
                "First-access getter rebuilds the cache from "
                + "OnHitEffectsRaw (which IS public and round-trips).");
            Assert.AreEqual(1, loadedSpecs.Count,
                "Rebuilt cache has the same content as the pre-save "
                + "cache — derived state is correctly recoverable.");
            Assert.AreEqual("Burning", loadedSpecs[0].EffectName);
            Assert.AreEqual(30, loadedSpecs[0].ChancePercent);

            // Second access returns the same cached instance (cached path).
            var secondAccess = loadedWeapon.OnHitEffectsCachedSpecs;
            Assert.AreSame(loadedSpecs, secondAccess,
                "Second access returns the same cached List instance "
                + "— the cache is stable post-rebuild.");
        }

        // ── D. Settlement Parts: public-field-only is the contract ──────
        //
        // Counter-check that ALL four settlement Parts share the same
        // private-state-resets-on-load behavior. If a future contributor
        // adds private state intended to persist across saves, this
        // family of tests will break visibly.

        [Test]
        public void Adversarial_OvenSitePart_PrivateFieldsReset_OnLoad()
        {
            var entity = new Entity { ID = "oven", BlueprintName = "TestOven" };
            entity.AddPart(new OvenSitePart
            {
                SettlementId = "v",
                SiteId = "oven_a"
            });
            var oven = entity.GetPart<OvenSitePart>();
            SetPrivateField(oven, "_renderFrameCounter", 50);
            SetPrivateField(oven, "_proximityMessageShown", true);
            SetPrivateField(oven, "_auraStarted", true);

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(entity);
            var loadedOven = loaded.GetPart<OvenSitePart>();

            Assert.AreEqual("v", loadedOven.SettlementId);
            Assert.AreEqual("oven_a", loadedOven.SiteId);
            Assert.AreEqual(0, GetPrivateField<int>(loadedOven, "_renderFrameCounter"));
            Assert.AreEqual(false, GetPrivateField<bool>(loadedOven, "_proximityMessageShown"));
            Assert.AreEqual(false, GetPrivateField<bool>(loadedOven, "_auraStarted"));
        }

        [Test]
        public void Adversarial_WellSitePart_PrivateFieldsReset_OnLoad()
        {
            var entity = new Entity { ID = "well", BlueprintName = "TestWell" };
            entity.AddPart(new WellSitePart
            {
                SettlementId = "v",
                SiteId = "well_a"
            });
            var well = entity.GetPart<WellSitePart>();
            SetPrivateField(well, "_renderFrameCounter", 50);
            SetPrivateField(well, "_proximityMessageShown", true);
            SetPrivateField(well, "_auraStarted", true);

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(entity);
            var loadedWell = loaded.GetPart<WellSitePart>();

            Assert.AreEqual("v", loadedWell.SettlementId);
            Assert.AreEqual("well_a", loadedWell.SiteId);
            Assert.AreEqual(0, GetPrivateField<int>(loadedWell, "_renderFrameCounter"));
            Assert.AreEqual(false, GetPrivateField<bool>(loadedWell, "_proximityMessageShown"));
            Assert.AreEqual(false, GetPrivateField<bool>(loadedWell, "_auraStarted"));
        }

        // ── E. Field-initializer contract pin ───────────────────────────
        //
        // Adversarial: confirm that _lastAppliedStage on Lantern/Oven/Well
        // resets to RepairStage.Fouled (the field initializer), NOT to
        // RepairStage.None (the enum default). This tests an important
        // behavior: Activator.CreateInstance runs the constructor.

        [Test]
        public void Adversarial_LanternSitePart_LastAppliedStage_ResetsToFieldInitializer()
        {
            // Setup with non-default state — pre-save the field is
            // StableRepair. Verify post-load it's Fouled (field
            // initializer), not StableRepair (would mean private
            // state persisted) and not None (would mean field
            // initializer didn't run).
            var entity = new Entity { ID = "lantern2", BlueprintName = "TestLantern2" };
            entity.AddPart(new LanternSitePart { SettlementId = "v", SiteId = "l" });
            var lantern = entity.GetPart<LanternSitePart>();
            SetPrivateField(lantern, "_lastAppliedStage", RepairStage.StableRepair);

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(entity);
            var loadedLantern = loaded.GetPart<LanternSitePart>();

            var stage = GetPrivateField<RepairStage>(loadedLantern, "_lastAppliedStage");
            Assert.AreEqual(RepairStage.Fouled, stage,
                "Field initializer (= RepairStage.Fouled) ran during "
                + "Activator.CreateInstance. Confirms private state "
                + "respects ctor field initializers, NOT raw type defaults.");
            Assert.AreNotEqual(RepairStage.None, stage,
                "Counter: NOT enum default (None). If it were None, "
                + "the field initializer would not have run.");
            Assert.AreNotEqual(RepairStage.StableRepair, stage,
                "Counter: NOT the pre-save value. If it were the "
                + "pre-save value, private state would have persisted.");
        }
    }
}
