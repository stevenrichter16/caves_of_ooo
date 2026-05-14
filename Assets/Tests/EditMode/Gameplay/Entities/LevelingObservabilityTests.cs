using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Observability-driven leveling tests. The leveling category emits
    /// three kinds: <c>Awarded</c> (XP grant succeeded), <c>Rejected</c>
    /// (XP grant short-circuited with a reason), and <c>LeveledUp</c>
    /// (one per level transition with HP/MP/SP grant details).
    ///
    /// <para>Each test dumps records to TestContext.WriteLine so the
    /// test output is the same artifact a live <c>diag_query
    /// category=leveling</c> would produce.</para>
    ///
    /// <para>Spec coverage:</para>
    /// <list type="bullet">
    ///   <item>Awarded fires with xpBefore/xpAfter/xpToNext on successful kill</item>
    ///   <item>NullArg / NoXPValue / NoExperienceStat reject paths emit Rejected with reason</item>
    ///   <item>Single-level transition emits exactly one LeveledUp record</item>
    ///   <item>Multi-level overflow emits one LeveledUp per transition</item>
    ///   <item>Entity without SP stat: gainedSP=false in LeveledUp payload</item>
    /// </list>
    /// </summary>
    public class LevelingObservabilityTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
        }

        // ── Fixture helpers ──────────────────────────────────────

        private static Entity MakePlayer(
            string id = "player",
            int level = 1,
            int experience = 0,
            int hp = 12,
            int mp = 0,
            int sp = 0,
            bool includeSPStat = true)
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Creature"] = "";
            e.Tags["Player"] = "";
            e.Statistics["Level"] = new Stat
            { Name = "Level", BaseValue = level, Value = level, Min = 1, Max = 99 };
            e.Statistics["Experience"] = new Stat
            { Name = "Experience", BaseValue = experience, Value = experience, Min = 0, Max = 999999 };
            e.Statistics["Hitpoints"] = new Stat
            { Name = "Hitpoints", BaseValue = hp, Value = hp, Min = 0, Max = hp };
            e.Statistics["MP"] = new Stat
            { Name = "MP", BaseValue = mp, Value = mp, Min = 0, Max = 99 };
            if (includeSPStat)
            {
                e.Statistics["SP"] = new Stat
                { Name = "SP", BaseValue = sp, Value = sp, Min = 0, Max = 999 };
            }
            return e;
        }

        private static Entity MakeVictim(string id = "snapjaw", int xpValue = 20)
        {
            var v = new Entity { ID = id, BlueprintName = id };
            if (xpValue > 0)
            {
                v.Statistics["XPValue"] = new Stat
                { Name = "XPValue", BaseValue = xpValue, Value = xpValue, Min = 0, Max = 9999 };
            }
            return v;
        }

        private static void DumpLevelingRecords(string label)
        {
            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "leveling",
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
        public void SuccessfulXPAward_EmitsAwarded_NoLevelUp()
        {
            // Player at level 1 with 0 XP. Kill awards 20. Threshold for
            // level 2 is 100+15=115, so no level up.
            var player = MakePlayer(level: 1, experience: 0);
            var victim = MakeVictim(xpValue: 20);

            LevelingSystem.AwardKillXP(player, victim, null);

            DumpLevelingRecords("XP award below level threshold");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "leveling", Limit = 20,
            }).Records;

            Assert.AreEqual(1, records.Count,
                "One Awarded record, no LeveledUp.");
            Assert.AreEqual("Awarded", records[0].Kind);
            StringAssert.Contains("\"xpGained\":20", records[0].PayloadJson);
            StringAssert.Contains("\"xpBefore\":0", records[0].PayloadJson);
            StringAssert.Contains("\"xpAfter\":20", records[0].PayloadJson);
            // Threshold for level 1 → 2 = 1*1*1*15 + 100 = 115
            StringAssert.Contains("\"xpToNext\":115", records[0].PayloadJson);
        }

        [Test]
        public void XPAward_NullVictim_EmitsRejected()
        {
            var player = MakePlayer();
            LevelingSystem.AwardKillXP(player, null, null);

            DumpLevelingRecords("null victim");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "leveling", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("Rejected", records[0].Kind);
            StringAssert.Contains("\"reason\":\"NullArg\"", records[0].PayloadJson);
        }

        [Test]
        public void XPAward_VictimWithoutXPValue_EmitsRejected()
        {
            var player = MakePlayer();
            var victim = MakeVictim(xpValue: 0);  // no XPValue stat

            LevelingSystem.AwardKillXP(player, victim, null);

            DumpLevelingRecords("victim has no XPValue");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "leveling", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("Rejected", records[0].Kind);
            StringAssert.Contains("\"reason\":\"VictimHasNoXPValue\"",
                records[0].PayloadJson);
        }

        [Test]
        public void XPAward_KillerWithoutExperienceStat_EmitsRejected()
        {
            // Killer has no Experience stat — e.g. an NPC with no
            // progression. Pre-fix this was a silent return.
            var killer = new Entity { ID = "feral", BlueprintName = "Feral" };
            killer.Tags["Creature"] = "";
            // intentionally NO Experience stat
            var victim = MakeVictim(xpValue: 50);

            LevelingSystem.AwardKillXP(killer, victim, null);

            DumpLevelingRecords("killer has no Experience stat");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "leveling", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("Rejected", records[0].Kind);
            StringAssert.Contains("\"reason\":\"KillerHasNoExperienceStat\"",
                records[0].PayloadJson);
        }

        [Test]
        public void SingleLevelUp_EmitsAwardedThenLeveledUp()
        {
            // 100 XP + kill for 20 → 120 ≥ threshold 115 → level up.
            var player = MakePlayer(level: 1, experience: 100, hp: 12);
            var victim = MakeVictim(xpValue: 20);

            LevelingSystem.AwardKillXP(player, victim, null);

            DumpLevelingRecords("single level-up via XP award");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "leveling", Limit = 20,
            }).Records;
            Assert.AreEqual(2, records.Count);
            Assert.AreEqual("Awarded", records[0].Kind);
            Assert.AreEqual("LeveledUp", records[1].Kind);
            StringAssert.Contains("\"prevLevel\":1", records[1].PayloadJson);
            StringAssert.Contains("\"newLevel\":2", records[1].PayloadJson);
            StringAssert.Contains("\"hpMaxBefore\":12", records[1].PayloadJson);
            StringAssert.Contains("\"hpMaxAfter\":14", records[1].PayloadJson);
            StringAssert.Contains("\"healedToFull\":true", records[1].PayloadJson);
            StringAssert.Contains("\"gainedMP\":1", records[1].PayloadJson);
            StringAssert.Contains("\"gainedSP\":true", records[1].PayloadJson);
            // The XP threshold was 115; remaining = 120 - 115 = 5.
            StringAssert.Contains("\"xpRemaining\":5", records[1].PayloadJson);
        }

        [Test]
        public void MultiLevelOverflow_EmitsOneLeveledUpPerTransition()
        {
            // 350 XP at level 1 crosses thresholds for 2 and 3:
            //   level 1 → 2 needs 115;  remaining: 350 - 115 = 235
            //   level 2 → 3 needs 8*15+100 = 220; remaining: 235 - 220 = 15
            var player = MakePlayer(level: 1, experience: 350);

            LevelingSystem.CheckLevelUp(player, null);

            DumpLevelingRecords("two-level overflow");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "leveling", Limit = 20,
            }).Records;
            Assert.AreEqual(2, records.Count,
                "Two level transitions should emit two LeveledUp records.");
            Assert.IsTrue(records.All(r => r.Kind == "LeveledUp"));
            StringAssert.Contains("\"prevLevel\":1", records[0].PayloadJson);
            StringAssert.Contains("\"newLevel\":2", records[0].PayloadJson);
            StringAssert.Contains("\"prevLevel\":2", records[1].PayloadJson);
            StringAssert.Contains("\"newLevel\":3", records[1].PayloadJson);
            StringAssert.Contains("\"xpRemaining\":15", records[1].PayloadJson);
        }

        [Test]
        public void LevelUp_OnEntityWithoutSPStat_HasGainedSPFalse()
        {
            // NPC without SP. CheckLevelUp must not crash AND the
            // LeveledUp record must reflect gainedSP=false.
            var npc = MakePlayer(level: 1, experience: 115, includeSPStat: false);

            LevelingSystem.CheckLevelUp(npc, null);

            DumpLevelingRecords("level-up without SP stat");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "leveling", Limit = 20,
            }).Records;
            Assert.AreEqual(1, records.Count);
            Assert.AreEqual("LeveledUp", records[0].Kind);
            StringAssert.Contains("\"gainedSP\":false", records[0].PayloadJson);
        }

        [Test]
        public void TwoKills_DifferentVictims_EmitTwoAwardedRecords()
        {
            // Counter-check: two awards land as two separate records,
            // each correctly attributing target ID.
            var player = MakePlayer();
            var snap = MakeVictim("snapjaw", xpValue: 10);
            var rat = MakeVictim("rat", xpValue: 5);

            LevelingSystem.AwardKillXP(player, snap, null);
            LevelingSystem.AwardKillXP(player, rat, null);

            DumpLevelingRecords("two kills, two awards");

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "leveling", Limit = 20,
            }).Records;
            Assert.AreEqual(2, records.Count);
            Assert.AreEqual("snapjaw", records[0].TargetId);
            Assert.AreEqual("rat", records[1].TargetId);
            StringAssert.Contains("\"xpGained\":10", records[0].PayloadJson);
            StringAssert.Contains("\"xpGained\":5", records[1].PayloadJson);
        }
    }
}
