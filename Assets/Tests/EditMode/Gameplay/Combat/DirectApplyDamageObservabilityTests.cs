using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Observability-driven tests for the <c>damage/DirectApply</c>
    /// record. Pre-ship: bleed/burn/trap/environmental damage emitted
    /// only <c>DamageDealt</c> with no upstream record — debugging
    /// "where did this damage come from?" required the
    /// <c>attributes:[]</c> heuristic. Post-ship: every direct
    /// <see cref="CombatSystem.ApplyDamage"/> call (one not nested inside
    /// <see cref="CombatSystem.PerformSingleAttack"/>'s <c>WithCause</c>
    /// scope) emits <c>DirectApply</c> first, then opens a cause scope
    /// so the downstream <c>PreDamageMutation</c> /
    /// <c>ResistanceApplied</c> / <c>DamageDealt</c> records all share
    /// a single 8-char trace id.
    ///
    /// <para>Spec coverage:</para>
    /// <list type="bullet">
    ///   <item>Direct ApplyDamage call emits DirectApply + DamageDealt</item>
    ///   <item>DirectApply and DamageDealt share CauseTraceId</item>
    ///   <item>BleedingEffect tick (via OnTurnStart) emits DirectApply</item>
    ///   <item>Damage with attributes (Fire) carries attributes into DirectApply</item>
    ///   <item>Damage with null source has hasSource=false</item>
    ///   <item>Two separate direct applies have DIFFERENT trace ids</item>
    /// </list>
    /// </summary>
    public class DirectApplyDamageObservabilityTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
        }

        // ── Fixture helpers ──────────────────────────────────────

        private static Entity MakeTarget(string id = "victim", int hp = 100)
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
            { Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Toughness"] = new Stat
            { Name = "Toughness", BaseValue = 10, Min = 1, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static void DumpDamageRecords(string label)
        {
            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage",
                Limit = 20,
            }).Records;

            TestContext.WriteLine($"\n=== {label} ===");
            TestContext.WriteLine($"Records: {records.Count}");
            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                TestContext.WriteLine(
                    $"  [{i}] {r.Kind,-20} cause={r.CauseTraceId ?? "(null)",-10} :: {r.PayloadJson}");
            }
        }

        // ── Tests ────────────────────────────────────────────────

        [Test]
        public void DirectApplyDamage_EmitsDirectApplyAndDamageDealt_SharedTraceId()
        {
            var target = MakeTarget();
            CombatSystem.ApplyDamage(target, 10, null, null);

            DumpDamageRecords("direct ApplyDamage, no source, no zone");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage", Limit = 20,
            }).Records;
            // Expect: DirectApply + DamageDealt (PreDamageMutation /
            // ResistanceApplied don't fire because no event mutations
            // and no resistance stats).
            Assert.AreEqual(2, records.Count);
            Assert.AreEqual("DirectApply", records[0].Kind);
            Assert.AreEqual("DamageDealt", records[1].Kind);
            // Same CauseTraceId — this is the key correlation invariant.
            Assert.AreEqual(records[0].CauseTraceId, records[1].CauseTraceId);
            Assert.IsNotNull(records[0].CauseTraceId,
                "DirectApply opens a cause scope; trace must be non-null.");
            Assert.AreEqual(8, records[0].CauseTraceId.Length,
                "DirectApply trace id is 8 chars (Guid.ToString N substring 0,8).");
        }

        [Test]
        public void DirectApply_NullSource_HasSourceFalse()
        {
            var target = MakeTarget();
            CombatSystem.ApplyDamage(target, 5, null, null);

            DumpDamageRecords("direct apply with null source");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage", Kind = "DirectApply", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("\"hasSource\":false", records[0].PayloadJson);
        }

        [Test]
        public void DirectApply_NonNullSource_HasSourceTrue()
        {
            var target = MakeTarget();
            var source = MakeTarget("attacker");
            CombatSystem.ApplyDamage(target, 5, source, null);

            DumpDamageRecords("direct apply with source entity");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage", Kind = "DirectApply", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("\"hasSource\":true", records[0].PayloadJson);
            Assert.AreEqual("attacker", records[0].ActorId);
            Assert.AreEqual("victim", records[0].TargetId);
        }

        [Test]
        public void DirectApply_TypedDamageWithAttributes_PropagatesAttributesIntoPayload()
        {
            var target = MakeTarget();
            var fireDamage = new Damage(8);
            fireDamage.AddAttribute("Fire");
            CombatSystem.ApplyDamage(target, fireDamage, null, null);

            DumpDamageRecords("direct fire damage with attributes");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage", Kind = "DirectApply", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("\"attributes\":[\"Fire\"]", records[0].PayloadJson);
        }

        [Test]
        public void BleedingTick_EmitsDirectApply_AsTheNonMeleeSignal()
        {
            // The flagship use case: a bleed tick should now be filterable
            // as `damage/DirectApply`, distinct from any melee swing.
            var target = MakeTarget();
            var bleed = new BleedingEffect(saveTarget: 99, damageDice: "1d2",
                rng: new System.Random(42));
            target.GetPart<StatusEffectsPart>().ApplyEffect(bleed);
            Diag.ResetAll();  // clear the OnApply record; focus on the tick

            // Fire the OnTurnStart pathway (StatusEffectsPart dispatches it
            // from HandleBeginTakeAction).
            var ev = GameEvent.New("BeginTakeAction");
            ev.SetParameter("Actor", (object)target);
            target.FireEventAndRelease(ev);

            DumpDamageRecords("bleed tick — DirectApply is the signal");

            var damageRecords = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage", Limit = 20,
            }).Records;
            // Must contain a DirectApply. May also contain DamageDealt.
            Assert.IsTrue(damageRecords.Any(r => r.Kind == "DirectApply"),
                "Non-melee bleed damage must emit DirectApply for query attribution.");
            var direct = damageRecords.First(r => r.Kind == "DirectApply");
            // Bleed has no attributes today
            StringAssert.Contains("\"attributes\":[]", direct.PayloadJson);
            StringAssert.Contains("\"hasSource\":false", direct.PayloadJson);
        }

        [Test]
        public void TwoSeparateDirectApplies_HaveDifferentTraceIds()
        {
            // Counter-check: each direct apply opens its OWN cause scope.
            // Two separate calls must NOT collapse into a single trace.
            var target = MakeTarget();
            CombatSystem.ApplyDamage(target, 5, null, null);
            CombatSystem.ApplyDamage(target, 5, null, null);

            DumpDamageRecords("two separate direct applies");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage", Kind = "DirectApply", Limit = 20,
            }).Records;
            Assert.AreEqual(2, records.Count);
            Assert.AreNotEqual(records[0].CauseTraceId, records[1].CauseTraceId,
                "Each direct apply opens its own trace — ids must differ. " +
                "If equal, the trace scope leaked across calls.");
        }

        [Test]
        public void DirectApply_ZeroDamage_DoesNotEmit()
        {
            // Counter-check: amount=0 is filtered before the DirectApply
            // emission (top-of-method guard at ApplyDamage:712).
            var target = MakeTarget();
            CombatSystem.ApplyDamage(target, 0, null, null);

            DumpDamageRecords("zero-damage call (filtered before emission)");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage", Limit = 20,
            }).Records;
            Assert.AreEqual(0, records.Count,
                "Zero damage must early-return before emitting DirectApply.");
        }

        [Test]
        public void DirectApply_AlreadyDeadTarget_DoesNotEmit()
        {
            // Counter-check on the dead-target gate. If hp <= 0, the
            // guard above the DirectApply block early-returns. No
            // record should fire — otherwise we'd see ghost damage
            // records on dead targets.
            var target = MakeTarget();
            target.GetStat("Hitpoints").BaseValue = 0;  // already dead

            CombatSystem.ApplyDamage(target, 10, null, null);

            DumpDamageRecords("dead-target apply (early-returned)");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "damage", Limit = 20,
            }).Records;
            Assert.AreEqual(0, records.Count);
        }
    }
}
