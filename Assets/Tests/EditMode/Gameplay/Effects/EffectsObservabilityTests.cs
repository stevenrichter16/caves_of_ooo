using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Observability-driven status-effect tests. StatusEffectsPart emits
    /// <c>effect/OnApply</c> on successful application and
    /// <c>effect/OnRemove</c> on removal. This fixture pins the
    /// emission contract + the stacking-no-double-emission rule, and
    /// shows the diag-record dumps in TestContext.
    ///
    /// <para>Critical correlation test: when a damaging effect like
    /// BleedingEffect ticks, the damage records emit under category
    /// "damage" while the effect's apply/remove emit under "effect".
    /// A live debugger correlating "did the bleed cause the death?"
    /// queries BOTH categories.</para>
    ///
    /// <para>Spec coverage:</para>
    /// <list type="bullet">
    ///   <item>ApplyEffect → exactly one OnApply record</item>
    ///   <item>Re-apply same effect type (stacking) → NO second OnApply</item>
    ///   <item>RemoveEffect → exactly one OnRemove record</item>
    ///   <item>ForceApplyEffect → OnApply with forced=true</item>
    ///   <item>Reset between tests is clean</item>
    /// </list>
    /// </summary>
    public class EffectsObservabilityTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
        }

        // ── Fixture helpers ──────────────────────────────────────

        private static Entity MakeTarget(string id = "victim")
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
            { Name = "Hitpoints", BaseValue = 100, Min = 0, Max = 100 };
            e.Statistics["Toughness"] = new Stat
            { Name = "Toughness", BaseValue = 10, Min = 1, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static void DumpEffectRecords(string label)
        {
            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "effect",
                Limit = 20,
            }).Records;

            TestContext.WriteLine($"\n=== {label} ===");
            TestContext.WriteLine($"Records: {records.Count}");
            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                TestContext.WriteLine($"  [{i}] {r.Kind,-10} actor={r.ActorId,-10} target={r.TargetId,-10} :: {r.PayloadJson}");
            }
        }

        // ── Tests ────────────────────────────────────────────────

        [Test]
        public void SingleApply_EmitsOneOnApplyRecord()
        {
            var target = MakeTarget("snapjaw");
            var effects = target.GetPart<StatusEffectsPart>();
            var source = MakeTarget("attacker");

            bool ok = effects.ApplyEffect(new BurningEffect(intensity: 1.0f), source);
            Assert.IsTrue(ok);

            DumpEffectRecords("burning applied");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "effect", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("OnApply", records[0].Kind);
            StringAssert.Contains("\"effect\":\"BurningEffect\"", records[0].PayloadJson);
            StringAssert.Contains("\"forced\":false", records[0].PayloadJson);
            // Source set as actor
            Assert.AreEqual("attacker", records[0].ActorId);
            Assert.AreEqual("snapjaw", records[0].TargetId);
        }

        [Test]
        public void StackingReApplication_DoesNotEmitSecondOnApply()
        {
            // Counter-check that stacking is a single-record contract:
            // applying StunnedEffect twice extends duration; the second
            // application is handled inside OnStack and MUST NOT emit a
            // second OnApply record (pinned in StatusEffectsPart docs).
            var target = MakeTarget();
            var effects = target.GetPart<StatusEffectsPart>();

            effects.ApplyEffect(new StunnedEffect(duration: 1));
            effects.ApplyEffect(new StunnedEffect(duration: 2));  // stacks

            DumpEffectRecords("stack re-apply: stunned twice");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "effect", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count,
                "Stacking apply should produce only the FIRST OnApply record. " +
                "If this fails, stack-detection is being skipped and a duplicate " +
                "is being emitted — debugging would see ghost double-effects.");
            Assert.AreEqual("OnApply", records[0].Kind);
        }

        [Test]
        public void ApplyThenRemove_EmitsApplyThenRemove()
        {
            var target = MakeTarget();
            var effects = target.GetPart<StatusEffectsPart>();

            effects.ApplyEffect(new FrozenEffect(cold: 1.0f));
            bool removed = effects.RemoveEffect<FrozenEffect>();
            Assert.IsTrue(removed);

            DumpEffectRecords("apply then remove frozen");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "effect", Limit = 20,
            }).Records;
            Assert.AreEqual(2, records.Count);
            Assert.AreEqual("OnApply", records[0].Kind);
            Assert.AreEqual("OnRemove", records[1].Kind);
            StringAssert.Contains("\"effect\":\"FrozenEffect\"", records[0].PayloadJson);
            StringAssert.Contains("\"effect\":\"FrozenEffect\"", records[1].PayloadJson);
        }

        [Test]
        public void ForceApply_EmitsRecordWithForcedTrue()
        {
            // Counter-check: forced=true distinguishes Force vs normal apply
            // in the diag stream. A buggy impl that didn't propagate the
            // `forced` flag would fail this test.
            var target = MakeTarget();
            var effects = target.GetPart<StatusEffectsPart>();

            effects.ForceApplyEffect(new BurningEffect(intensity: 2.0f));

            DumpEffectRecords("force-applied burning");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "effect", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("\"forced\":true", records[0].PayloadJson);
        }

        [Test]
        public void MultipleDifferentEffects_EmitOneOnApplyEach()
        {
            // Three different effect types stack independently. Each
            // gets its own OnApply. Order matters: applying Frozen LAST
            // because FrozenEffect.OnApply removes BurningEffect (cold
            // extinguishes fire) and would produce a 4th record. This
            // test uses BleedingEffect instead of Frozen for the third
            // slot — no auto-side-effect.
            var target = MakeTarget();
            var effects = target.GetPart<StatusEffectsPart>();

            effects.ApplyEffect(new BurningEffect());
            effects.ApplyEffect(new StunnedEffect());
            effects.ApplyEffect(new BleedingEffect(saveTarget: 15,
                damageDice: "1d2", rng: new System.Random(1)));

            DumpEffectRecords("three different effects applied");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "effect", Limit = 20,
            }).Records;
            Assert.AreEqual(3, records.Count);
            Assert.IsTrue(records.All(r => r.Kind == "OnApply"));
        }

        [Test]
        public void FrozenOnBurning_AppliesAndAutoRemovesBurning_DocumentedSideEffect()
        {
            // SURFACE OBSERVATION: FrozenEffect.OnApply REMOVES any
            // BurningEffect via target.RemoveEffect<BurningEffect>().
            // The diag stream surfaces this as a 4-record sequence:
            //   [0] OnApply  BurningEffect
            //   [1] OnApply  FrozenEffect
            //   [2] OnRemove BurningEffect   ← cold defeats fire
            // (Order [1]/[2] depends on implementation — diag records
            //  capture the actual chronological order.)
            // This test pins the contract so a future refactor that
            // drops the auto-remove is caught.
            var target = MakeTarget();
            var effects = target.GetPart<StatusEffectsPart>();

            effects.ApplyEffect(new BurningEffect(intensity: 1.0f));
            effects.ApplyEffect(new FrozenEffect(cold: 1.0f));

            DumpEffectRecords("frozen extinguishes burning");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "effect", Limit = 20,
            }).Records;
            // Exact ordering is impl-defined but the SET should be:
            //   2x OnApply (Burning, Frozen) + 1x OnRemove (Burning)
            Assert.AreEqual(3, records.Count);
            Assert.AreEqual(2, records.Count(r => r.Kind == "OnApply"));
            Assert.AreEqual(1, records.Count(r => r.Kind == "OnRemove"));
            // The removed effect MUST be BurningEffect
            var removed = records.First(r => r.Kind == "OnRemove");
            StringAssert.Contains("\"effect\":\"BurningEffect\"", removed.PayloadJson);
        }

        [Test]
        public void ApplyEffectWithCorrelation_BleedingTickEmitsDamageRecords()
        {
            // Cross-system correlation test: BleedingEffect's OnTurnStart
            // calls CombatSystem.ApplyDamage, which emits damage records.
            // Apply the effect, force a tick via the OnTurnStart event,
            // and verify BOTH effect and damage records exist.
            var target = MakeTarget("victim");
            var source = MakeTarget("attacker");
            var effects = target.GetPart<StatusEffectsPart>();
            effects.ApplyEffect(new BleedingEffect(saveTarget: 99, damageDice: "1d2",
                rng: new System.Random(12345)), source);

            // Trigger one tick. StatusEffectsPart dispatches OnTurnStart
            // from its HandleBeginTakeAction (event ID "BeginTakeAction"
            // or "TakeTurn"), not from a raw "TurnStart" — verified in
            // StatusEffectsPart.cs:320-328.
            var ev = GameEvent.New("BeginTakeAction");
            ev.SetParameter("Actor", (object)target);
            target.FireEventAndRelease(ev);

            // Effect category
            DumpEffectRecords("bleeding apply + tick");
            // Damage category
            var damageRecords = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage", Limit = 20,
            }).Records;
            TestContext.WriteLine($"\n=== damage records during bleed tick ===");
            TestContext.WriteLine($"Records: {damageRecords.Count}");
            for (int i = 0; i < damageRecords.Count; i++)
                TestContext.WriteLine($"  [{i}] {damageRecords[i].Kind,-20} :: {damageRecords[i].PayloadJson}");

            // Effect: 1 OnApply
            var effectRecords = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "effect", Limit = 20,
            }).Records;
            Assert.AreEqual(1, effectRecords.Count);
            Assert.AreEqual("OnApply", effectRecords[0].Kind);

            // Damage: at least one DamageDealt from the bleed tick (saveTarget=99
            // makes save reliably fail so damage WILL roll).
            Assert.IsTrue(damageRecords.Any(r => r.Kind == "DamageDealt"),
                "Bleed tick should produce at least one DamageDealt record.");
        }

        [Test]
        public void NullEffect_ApplyEffect_NoCrash_NoRecord()
        {
            // Adversarial: passing a null effect should silently return
            // false without emitting anything.
            var target = MakeTarget();
            var effects = target.GetPart<StatusEffectsPart>();

            bool ok = effects.ApplyEffect(null);
            Assert.IsFalse(ok);

            DumpEffectRecords("null effect apply");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "effect", Limit = 20,
            }).Records;
            Assert.AreEqual(0, records.Count,
                "Null effect should not emit an OnApply.");
        }
    }
}
