using CavesOfOoo.Core;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class LevelingSystemTests
    {
        // ST.4: ScenarioTestHarness is needed for the blueprint-loading
        // test; per-fixture singleton lifecycle.
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() { _harness?.Dispose(); _harness = null; }

        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            MessageLog.Clear();
        }

        [Test]
        public void AwardKillXP_PreservesOverflowTowardNextLevel()
        {
            var player = CreatePlayer(level: 1, experience: 100, hitpoints: 12, maxHitpoints: 12, mp: 2);
            var victim = new Entity { BlueprintName = "Snapjaw" };
            victim.Statistics["XPValue"] = new Stat { Name = "XPValue", BaseValue = 20, Value = 20, Min = 0, Max = 9999 };

            LevelingSystem.AwardKillXP(player, victim, null);

            Assert.AreEqual(2, player.GetStatValue("Level"));
            Assert.AreEqual(5, player.GetStatValue("Experience"));
            Assert.AreEqual(14, player.GetStat("Hitpoints").Max);
            Assert.AreEqual(14, player.GetStatValue("Hitpoints"));
            Assert.AreEqual(3, player.GetStatValue("MP"));
        }

        [Test]
        public void CheckLevelUp_CarriesOverflowAcrossMultipleLevels()
        {
            var player = CreatePlayer(level: 1, experience: 350, hitpoints: 10, maxHitpoints: 10, mp: 1);

            LevelingSystem.CheckLevelUp(player, null);

            Assert.AreEqual(3, player.GetStatValue("Level"));
            Assert.AreEqual(15, player.GetStatValue("Experience"));
            Assert.AreEqual(14, player.GetStat("Hitpoints").Max);
            Assert.AreEqual(14, player.GetStatValue("Hitpoints"));
            Assert.AreEqual(3, player.GetStatValue("MP"));
        }

        // ====================================================================
        // ST.4 — Skill Points (SP) grant on level-up
        // ====================================================================

        [Test]
        public void PlayerBlueprint_HasSPStat_WithDefaultZero()
        {
            // Pin the Player-blueprint contract: SP stat must exist
            // (so SkillsPart users can rely on it being there) and start
            // at 0 (no free skill points before earning a level).
            var player = _harness.Factory.CreateEntity("Player");
            Assert.IsNotNull(player, "Player blueprint must exist.");

            var sp = player.GetStat("SP");
            Assert.IsNotNull(sp, "Player blueprint must declare an SP stat.");
            Assert.AreEqual(0, sp.BaseValue,
                "SP must start at 0 — players earn SP via level-up, not character creation.");
            Assert.AreEqual(999, sp.Max,
                "SP cap should be the documented 999 (well above any " +
                "realistic single-character accumulation).");
        }

        [Test]
        public void CheckLevelUp_OnSingleLevelTransition_GrantsOneSP()
        {
            // Seed XP exactly at the threshold for level 1→2. CheckLevelUp
            // should advance one level and grant exactly 1 SP.
            int threshold = LevelingSystem.XPToNextLevel(1);
            var player = CreatePlayer(level: 1, experience: threshold,
                                       hitpoints: 10, maxHitpoints: 10, mp: 0,
                                       sp: 0);

            LevelingSystem.CheckLevelUp(player, null);

            Assert.AreEqual(2, player.GetStatValue("Level"),
                "Level should advance to 2.");
            Assert.AreEqual(1, player.GetStatValue("SP"),
                "Single level-up grants exactly 1 SP.");
        }

        [Test]
        public void CheckLevelUp_AcrossMultipleLevels_AccumulatesSPLinearly()
        {
            // XP enough for 3 levels: 1→2 + 2→3 + 3→4. SP should be 3
            // (one per level transition, not three-from-one-call).
            int xp = LevelingSystem.XPToNextLevel(1)
                   + LevelingSystem.XPToNextLevel(2)
                   + LevelingSystem.XPToNextLevel(3);
            var player = CreatePlayer(level: 1, experience: xp,
                                       hitpoints: 10, maxHitpoints: 10, mp: 0,
                                       sp: 0);

            LevelingSystem.CheckLevelUp(player, null);

            Assert.AreEqual(4, player.GetStatValue("Level"),
                "Three level-ups should advance Level by 3 (1→2→3→4).");
            Assert.AreEqual(3, player.GetStatValue("SP"),
                "Three level transitions accumulate 3 SP.");
        }

        [Test]
        public void CheckLevelUp_EntityWithoutSPStat_DoesNotCrash()
        {
            // Counter-check: NPCs / minor actors without an SP stat
            // (e.g. enemies that gain XP from killing the player but
            // have no skill-economy participation) must level up
            // cleanly without crashing on the SP grant.
            int threshold = LevelingSystem.XPToNextLevel(1);
            var npc = new Entity { BlueprintName = "TestNPC" };
            npc.Statistics["Level"] = new Stat
            { Name = "Level", BaseValue = 1, Value = 1, Min = 1, Max = 99 };
            npc.Statistics["Experience"] = new Stat
            { Name = "Experience", BaseValue = threshold, Value = threshold, Min = 0, Max = 999999 };
            npc.Statistics["Hitpoints"] = new Stat
            { Name = "Hitpoints", BaseValue = 10, Value = 10, Min = 0, Max = 10 };
            npc.Statistics["MP"] = new Stat
            { Name = "MP", BaseValue = 0, Value = 0, Min = 0, Max = 99 };
            // Intentionally NO SP stat — this is the counter-check.

            Assert.DoesNotThrow(() => LevelingSystem.CheckLevelUp(npc, null),
                "Entity without SP stat must level up cleanly without crashing.");
            Assert.AreEqual(2, npc.GetStatValue("Level"),
                "Level should still advance even without the SP stat.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Entity CreatePlayer(int level, int experience, int hitpoints, int maxHitpoints, int mp, int sp = 0)
        {
            var player = new Entity { BlueprintName = "Player" };
            player.SetTag("Player");
            player.SetTag("Creature");
            player.Statistics["Level"] = new Stat { Name = "Level", BaseValue = level, Value = level, Min = 1, Max = 99 };
            player.Statistics["Experience"] = new Stat { Name = "Experience", BaseValue = experience, Value = experience, Min = 0, Max = 999999 };
            player.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hitpoints, Value = hitpoints, Min = 0, Max = maxHitpoints };
            player.Statistics["MP"] = new Stat { Name = "MP", BaseValue = mp, Value = mp, Min = 0, Max = 99 };
            player.Statistics["SP"] = new Stat { Name = "SP", BaseValue = sp, Value = sp, Min = 0, Max = 999 };
            return player;
        }
    }
}
