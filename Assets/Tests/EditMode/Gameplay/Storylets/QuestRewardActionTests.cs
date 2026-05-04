using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// QS.5 tests (Docs/QUEST-SYSTEM.md) for the two reward
    /// conversation actions:
    ///
    ///   AwardXP(amount)    — wraps LevelingSystem.CheckLevelUp
    ///   GiveDrams(amount)  — wraps TradeSystem.SetDrams
    ///
    /// Both share the same defensive shape: parse arg as int,
    /// no-op on parse failure or non-positive amount, no-op on
    /// null listener. This pins those defensive paths so a typo
    /// in quest content (e.g., GiveDrams "many") can't grant
    /// absurd values or crash.
    /// </summary>
    [TestFixture]
    public class QuestRewardActionTests
    {
        [SetUp]
        public void SetUp()
        {
            ConversationActions.Reset();
            MessageLog.Clear();
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Entity CreatePlayerWithXPAndDrams(int startingDrams = 0)
        {
            var player = new Entity { BlueprintName = "Player" };
            player.Tags["Creature"] = "";
            player.Tags["Player"] = "";
            player.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            player.Statistics["Experience"] = new Stat { Name = "Experience", BaseValue = 0, Min = 0, Max = 99999 };
            player.Statistics["Level"] = new Stat { Name = "Level", BaseValue = 1, Min = 1, Max = 50 };
            player.AddPart(new RenderPart { DisplayName = "you" });
            TradeSystem.SetDrams(player, startingDrams);
            return player;
        }

        // ====================================================================
        // 1. AwardXP — adds to Experience stat
        // ====================================================================

        [Test]
        public void AwardXP_AddsToExperienceStat()
        {
            var player = CreatePlayerWithXPAndDrams();

            ConversationActions.Execute("AwardXP", null, player, "50");

            Assert.AreEqual(50, player.GetStatValue("Experience"),
                "AwardXP must add the parsed amount to the Experience stat.");
        }

        // ====================================================================
        // 2. AwardXP — triggers level-up at threshold
        //
        // LevelingSystem's threshold for level 1→2 is whatever
        // XPToNextLevel(1) returns. We award enough XP to cross it
        // and verify the level changed.
        // ====================================================================

        [Test]
        public void AwardXP_TriggersLevelUpWhenThresholdCrossed()
        {
            var player = CreatePlayerWithXPAndDrams();
            int levelBefore = player.GetStatValue("Level");

            // Award a large amount certain to cross the level-1→2
            // threshold (formula is floor(Level^3 * 15) + 100, so
            // L1→L2 threshold is 115 XP). 1000 XP should easily
            // bump at least one level.
            ConversationActions.Execute("AwardXP", null, player, "1000");

            int levelAfter = player.GetStatValue("Level");
            Assert.Greater(levelAfter, levelBefore,
                "AwardXP must trigger LevelingSystem.CheckLevelUp — " +
                "amount above threshold must increase the Level stat.");
        }

        // ====================================================================
        // 3. AwardXP — defensive: zero / negative amount no-ops
        // ====================================================================

        [Test]
        public void AwardXP_ZeroOrNegativeAmount_NoOp()
        {
            var player = CreatePlayerWithXPAndDrams();
            int xpBefore = player.GetStatValue("Experience");

            ConversationActions.Execute("AwardXP", null, player, "0");
            ConversationActions.Execute("AwardXP", null, player, "-100");

            Assert.AreEqual(xpBefore, player.GetStatValue("Experience"),
                "Zero or negative AwardXP amounts must NOT decrement XP " +
                "(or do anything at all). A typo in JSON content " +
                "(e.g., \"AwardXP\" : \"-50\") shouldn't punish the player.");
        }

        // ====================================================================
        // 4. AwardXP — defensive: malformed amount no-ops
        // ====================================================================

        [Test]
        public void AwardXP_MalformedAmount_NoOp()
        {
            var player = CreatePlayerWithXPAndDrams();

            // Author typed a word instead of a number.
            ConversationActions.Execute("AwardXP", null, player, "many");

            Assert.AreEqual(0, player.GetStatValue("Experience"),
                "Non-integer AwardXP arg must NOT mutate Experience.");
        }

        // ====================================================================
        // 5. GiveDrams — adds to currency property
        // ====================================================================

        [Test]
        public void GiveDrams_AddsToCurrencyProperty()
        {
            var player = CreatePlayerWithXPAndDrams(startingDrams: 100);

            ConversationActions.Execute("GiveDrams", null, player, "50");

            Assert.AreEqual(150, TradeSystem.GetDrams(player),
                "GiveDrams must add the parsed amount to the existing " +
                "Drams IntProperty (additive — not replacement).");
        }

        // ====================================================================
        // 6. GiveDrams — defensive: zero / negative no-ops + counter-check
        //    that "GiveDrams -100" doesn't actually take drams
        // ====================================================================

        [Test]
        public void GiveDrams_ZeroOrNegativeAmount_NoOp()
        {
            var player = CreatePlayerWithXPAndDrams(startingDrams: 100);

            ConversationActions.Execute("GiveDrams", null, player, "0");
            ConversationActions.Execute("GiveDrams", null, player, "-50");

            Assert.AreEqual(100, TradeSystem.GetDrams(player),
                "Zero or negative GiveDrams must NOT decrement drams. " +
                "Cost-style actions belong on a future TakeDrams (not " +
                "this one) — keeping GiveDrams unidirectional means a " +
                "typo can't accidentally drain the player.");
        }

        // ====================================================================
        // 7. Both actions — defensive: null listener no-ops
        //    (covers tick-driven dispatch which passes null/null)
        // ====================================================================

        [Test]
        public void RewardActions_NullListener_NoOpWithoutCrash()
        {
            // Tick-driven OnEnter dispatch passes null listener. Reward
            // actions on a tick-driven advance (e.g., terminal-stage
            // auto-completion) shouldn't crash but they also can't grant
            // anything (no recipient). Document the limitation in plan
            // — content authors should put rewards on player-initiated
            // actions, not tick-trigger advances.
            Assert.DoesNotThrow(() =>
                ConversationActions.Execute("AwardXP", null, null, "100"));
            Assert.DoesNotThrow(() =>
                ConversationActions.Execute("GiveDrams", null, null, "50"));
        }
    }
}
