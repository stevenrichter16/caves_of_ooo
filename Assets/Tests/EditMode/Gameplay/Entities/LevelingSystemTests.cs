using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class LevelingSystemTests
    {
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

        private static Entity CreatePlayer(int level, int experience, int hitpoints, int maxHitpoints, int mp)
        {
            var player = new Entity { BlueprintName = "Player" };
            player.SetTag("Player");
            player.SetTag("Creature");
            player.Statistics["Level"] = new Stat { Name = "Level", BaseValue = level, Value = level, Min = 1, Max = 99 };
            player.Statistics["Experience"] = new Stat { Name = "Experience", BaseValue = experience, Value = experience, Min = 0, Max = 999999 };
            player.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hitpoints, Value = hitpoints, Min = 0, Max = maxHitpoints };
            player.Statistics["MP"] = new Stat { Name = "MP", BaseValue = mp, Value = mp, Min = 0, Max = 99 };
            return player;
        }
    }
}
