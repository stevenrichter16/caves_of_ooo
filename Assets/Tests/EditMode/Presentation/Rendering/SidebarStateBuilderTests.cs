using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class SidebarStateBuilderTests
    {
        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            FactionManager.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            MessageLog.Clear();
            FactionManager.Reset();
        }

        [Test]
        public void Build_FormatsCoreVitalsAndStatusFromPlayerData()
        {
            var zone = new Zone("SidebarZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 4, 5);

            var inventory = player.GetPart<InventoryPart>();
            inventory.AddObject(new Entity
            {
                BlueprintName = "Anvil"
            }.AddPartAndReturn(new PhysicsPart { Weight = 12 }));
            TradeSystem.SetDrams(player, 27);
            player.ApplyEffect(new PoisonedEffect(2), player, zone);

            SidebarSnapshot snapshot = SidebarStateBuilder.Build(player, zone, null);

            CollectionAssert.AreEqual(
                new[]
                {
                    "HP 25/30 | MP 3",
                    "LV 2 | XP 10/220",
                    "AV 0 | DV 6",
                    "WT 12/150 | DR 27"
                },
                snapshot.VitalLines);
            Assert.AreEqual("poisoned", snapshot.StatusText);
        }

        [Test]
        public void Build_UsesPlayerCellFallbackFocus_WhenNoLookSnapshotIsPresent()
        {
            var zone = new Zone("SidebarZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 7, 9);

            SidebarSnapshot snapshot = SidebarStateBuilder.Build(player, zone, null);

            Assert.IsNotNull(snapshot.FocusSnapshot);
            Assert.AreEqual(7, snapshot.FocusSnapshot.X);
            Assert.AreEqual(9, snapshot.FocusSnapshot.Y);
            StringAssert.Contains("you", snapshot.FocusSnapshot.Header);
        }

        [Test]
        public void Build_CoalescesAdjacentDuplicateMessages_NewestFirst()
        {
            MessageLog.Add("older");
            MessageLog.Add("older");
            MessageLog.Add("newest");

            SidebarSnapshot snapshot = SidebarStateBuilder.Build(null, null, null);

            Assert.AreEqual(2, snapshot.LogEntriesNewestFirst.Count);
            Assert.AreEqual("newest", snapshot.LogEntriesNewestFirst[0].Text);
            Assert.AreEqual(1, snapshot.LogEntriesNewestFirst[0].Count);
            Assert.AreEqual("older", snapshot.LogEntriesNewestFirst[1].Text);
            Assert.AreEqual(2, snapshot.LogEntriesNewestFirst[1].Count);
        }

        [Test]
        public void Build_RetainsOnlyTheLatestThirtyRawMessages()
        {
            for (int i = 0; i < 35; i++)
                MessageLog.Add("msg-" + i.ToString("00"));

            SidebarSnapshot snapshot = SidebarStateBuilder.Build(null, null, null);

            Assert.AreEqual(30, snapshot.LogEntriesNewestFirst.Count);
            Assert.AreEqual("msg-34", snapshot.LogEntriesNewestFirst[0].Text);
            Assert.AreEqual("msg-05", snapshot.LogEntriesNewestFirst[29].Text);
        }

        [Test]
        public void FormatLog_WrapsWithPrefixes_AndPreservesCoalescedSuffix()
        {
            var lines = SidebarTextFormatter.FormatLog(
                new[] { new SidebarLogEntry("This is a wrapped message", 12, 2) },
                12,
                6);

            Assert.Greater(lines.Count, 1);
            StringAssert.StartsWith(":: ", lines[0].Text);
            StringAssert.StartsWith("   ", lines[1].Text);
            Assert.IsTrue(lines.Any(line => line.Text.Contains("(x2)")));
        }

        private static Entity CreatePlayer()
        {
            var player = new Entity { BlueprintName = "Player" };
            player.SetTag("Player");
            player.SetTag("Creature");
            player.AddPart(new RenderPart { DisplayName = "you", RenderString = "@", ColorString = "&Y", RenderLayer = 10 });
            player.AddPart(new PhysicsPart { Solid = true });
            player.AddPart(new InventoryPart());
            player.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 25, Value = 25, Min = 0, Max = 30 };
            player.Statistics["MP"] = new Stat { Name = "MP", BaseValue = 3, Value = 3, Min = 0, Max = 6 };
            player.Statistics["Level"] = new Stat { Name = "Level", BaseValue = 2, Value = 2, Min = 1, Max = 99 };
            player.Statistics["XP"] = new Stat { Name = "XP", BaseValue = 10, Value = 10, Min = 0, Max = 99999 };
            player.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 10, Value = 10, Min = 1, Max = 100 };
            player.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Value = 100, Min = 1, Max = 200 };
            return player;
        }
    }

    internal static class SidebarStateBuilderTestExtensions
    {
        public static Entity AddPartAndReturn<TPart>(this Entity entity, TPart part) where TPart : Part
        {
            entity.AddPart(part);
            return entity;
        }
    }
}
