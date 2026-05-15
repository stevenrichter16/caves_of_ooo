using System;
using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Observability-driven cooldown tests. Pre-fix: "why is my skill
    /// still on cooldown?" required <c>execute_code</c> probing of
    /// <c>SkillsPart</c> state. Post-fix:
    /// <see cref="ActivatedAbilitiesPart.TickCooldowns"/> emits per
    /// decrement:
    ///
    /// <list type="bullet">
    ///   <item><c>skill/CooldownAdvanced</c> — per-tick decrement when CooldownRemaining stays &gt; 0</item>
    ///   <item><c>skill/CooldownReady</c> — single record on the transition to 0 (the user-visible "now usable" signal)</item>
    /// </list>
    ///
    /// <para>Payload: <c>ability</c> (display name), <c>class</c>,
    /// <c>command</c>, <c>before</c>, <c>after</c>, <c>maxCooldown</c>.
    /// Abilities at CooldownRemaining=0 are silent (no flood for idle
    /// abilities).</para>
    /// </summary>
    public class SkillCooldownObservabilityTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
        }

        // ── Fixture helpers ──────────────────────────────────────

        private static Entity MakeActorWithAbility(string command = "CommandSlam",
            string display = "Slam", int initialCooldown = 0, int maxCooldown = 0)
        {
            var e = new Entity { ID = "actor", BlueprintName = "actor" };
            e.AddPart(new RenderPart { DisplayName = "actor" });
            var abilities = new ActivatedAbilitiesPart();
            e.AddPart(abilities);
            var id = abilities.AddAbility(display, command, "Skills");
            if (initialCooldown > 0)
                abilities.CooldownAbility(id, initialCooldown);
            // CooldownAbility sets MaxCooldown to `turns` — override if test wants different.
            if (maxCooldown > 0 && abilities.AbilityByGuid.TryGetValue(id, out var ab))
                ab.MaxCooldown = maxCooldown;
            return e;
        }

        private static void DumpSkillRecords(string label)
        {
            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill",
                Limit = 20,
            }).Records;

            TestContext.WriteLine($"\n=== {label} ===");
            TestContext.WriteLine($"Records: {records.Count}");
            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                TestContext.WriteLine(
                    $"  [{i}] {r.Kind,-18} actor={r.ActorId,-8} :: {r.PayloadJson}");
            }
        }

        // ── Tests ────────────────────────────────────────────────

        [Test]
        public void TickCooldowns_NoAbilitiesOnCooldown_NoRecords()
        {
            // Counter-check on the flood guard: idle abilities (CR=0)
            // must NOT emit on tick. Otherwise a 9-slot ability bar
            // would emit 9 records per turn even when all idle.
            var actor = MakeActorWithAbility(initialCooldown: 0);

            actor.GetPart<ActivatedAbilitiesPart>().TickCooldowns();

            DumpSkillRecords("tick with all idle abilities");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill", Limit = 20,
            }).Records;
            Assert.AreEqual(0, records.Count,
                "Tick on idle ability must not emit records.");
        }

        [Test]
        public void TickCooldowns_MidCooldown_EmitsCooldownAdvanced()
        {
            // Ability at CR=3 → after tick CR=2, emit Advanced.
            var actor = MakeActorWithAbility(initialCooldown: 3);

            actor.GetPart<ActivatedAbilitiesPart>().TickCooldowns();

            DumpSkillRecords("mid-cooldown tick 3→2");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("CooldownAdvanced", records[0].Kind);
            StringAssert.Contains("\"ability\":\"Slam\"", records[0].PayloadJson);
            StringAssert.Contains("\"before\":3", records[0].PayloadJson);
            StringAssert.Contains("\"after\":2", records[0].PayloadJson);
        }

        [Test]
        public void TickCooldowns_TransitionToZero_EmitsCooldownReady()
        {
            // Ability at CR=1 → after tick CR=0, emit Ready (NOT Advanced).
            // This is the user-visible "your skill is ready again" moment.
            var actor = MakeActorWithAbility(initialCooldown: 1);

            actor.GetPart<ActivatedAbilitiesPart>().TickCooldowns();

            DumpSkillRecords("transition tick 1→0 (Ready)");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("CooldownReady", records[0].Kind);
            StringAssert.Contains("\"before\":1", records[0].PayloadJson);
            StringAssert.Contains("\"after\":0", records[0].PayloadJson);
        }

        [Test]
        public void TickCooldowns_FullSequence_AdvancedThenReady()
        {
            // CR=3 → 2 → 1 → 0 across 3 ticks. Expect 3 records: two
            // Advanced + one Ready. The transition record is the LAST one.
            var actor = MakeActorWithAbility(initialCooldown: 3);
            var abilities = actor.GetPart<ActivatedAbilitiesPart>();

            abilities.TickCooldowns();
            abilities.TickCooldowns();
            abilities.TickCooldowns();
            // One more tick at CR=0 should be silent (counter-check)
            abilities.TickCooldowns();

            DumpSkillRecords("full sequence 3→2→1→0 + idle tick");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill", Limit = 20,
            }).Records;
            Assert.AreEqual(3, records.Count,
                "3 ticks while CR>0 should emit 3 records; the post-zero idle tick is silent.");
            Assert.AreEqual("CooldownAdvanced", records[0].Kind);
            Assert.AreEqual("CooldownAdvanced", records[1].Kind);
            Assert.AreEqual("CooldownReady", records[2].Kind);
            // Sanity-check the ladder
            StringAssert.Contains("\"after\":2", records[0].PayloadJson);
            StringAssert.Contains("\"after\":1", records[1].PayloadJson);
            StringAssert.Contains("\"after\":0", records[2].PayloadJson);
        }

        [Test]
        public void TickCooldowns_MultipleAbilities_EmitsOnePerNonIdle()
        {
            // Two abilities: one on cooldown, one idle. Tick should
            // emit ONLY for the one on cooldown.
            var actor = MakeActorWithAbility(initialCooldown: 2, display: "Slam");
            var abilities = actor.GetPart<ActivatedAbilitiesPart>();
            // Add a second, idle ability
            abilities.AddAbility("Lance", "CommandLance", "Mutations");

            abilities.TickCooldowns();

            DumpSkillRecords("two abilities; one on CD");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count,
                "Only the non-idle ability should emit.");
            StringAssert.Contains("\"ability\":\"Slam\"", records[0].PayloadJson);
        }

        [Test]
        public void TickCooldowns_PayloadIncludesClassAndCommand()
        {
            // Counter-check that the payload's `class` and `command`
            // fields propagate correctly — a debugger filtering
            // by class="Skills" or command="CommandSlam" depends on these.
            var actor = MakeActorWithAbility(
                command: "CommandSlam", display: "Slam", initialCooldown: 2);

            actor.GetPart<ActivatedAbilitiesPart>().TickCooldowns();

            DumpSkillRecords("payload class+command");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "skill", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            // class is a reserved C# keyword in the anonymous payload —
            // serialized as "class" via @class. Check both.
            StringAssert.Contains("\"class\":\"Skills\"", records[0].PayloadJson);
            StringAssert.Contains("\"command\":\"CommandSlam\"", records[0].PayloadJson);
        }
    }
}
