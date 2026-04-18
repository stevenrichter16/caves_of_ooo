using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Scenarios;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests.Scenarios
{
    /// <summary>
    /// Phase 2c tests — PlayerBuilder methods. Integration-style: each test
    /// constructs a ScenarioContext with a real Player entity from the live
    /// blueprint JSON, applies a builder method, and asserts state.
    ///
    /// Shares the EntityFactory across the fixture via OneTimeSetUp so blueprint
    /// loading isn't repeated per test.
    /// </summary>
    [TestFixture]
    public class PlayerBuilderTests
    {
        private static EntityFactory _sharedFactory;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            FactionManager.Initialize();
            _sharedFactory = new EntityFactory();
            string blueprintPath = Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json");
            _sharedFactory.LoadBlueprints(File.ReadAllText(blueprintPath));
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            FactionManager.Reset();
            _sharedFactory = null;
        }

        /// <summary>
        /// Builds a fresh context with a real Player entity placed at (40, 12)
        /// in a synthetic Zone.
        /// </summary>
        private static (ScenarioContext ctx, Zone zone, Entity player) BuildContext()
        {
            var zone = new Zone("PlayerBuilderTestZone");
            var player = _sharedFactory.CreateEntity("Player");
            zone.AddEntity(player, 40, 12);

            var tm = new TurnManager();
            var ctx = new ScenarioContext(zone, _sharedFactory, player, tm, rngSeed: 98765);
            return (ctx, zone, player);
        }

        // ======================================================
        // Teleport
        // ======================================================

        [Test]
        public void Teleport_MovesPlayerToNewCell()
        {
            var (ctx, zone, player) = BuildContext();
            ctx.Player.Teleport(50, 15);
            var pos = zone.GetEntityPosition(player);
            Assert.AreEqual((50, 15), (pos.x, pos.y));
        }

        [Test]
        public void Teleport_OutOfBounds_LogsAndLeavesPlayerInPlace()
        {
            var (ctx, zone, player) = BuildContext();
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"out of zone bounds"));
            ctx.Player.Teleport(-5, 999);
            var pos = zone.GetEntityPosition(player);
            Assert.AreEqual((40, 12), (pos.x, pos.y), "Player should stay at original cell after out-of-bounds teleport.");
        }

        // ======================================================
        // HP
        // ======================================================

        [Test]
        public void SetHp_SetsAbsoluteBaseValue()
        {
            var (ctx, _, player) = BuildContext();
            ctx.Player.SetHp(100);
            Assert.AreEqual(100, player.GetStatValue("Hitpoints", -1));
        }

        [Test]
        public void SetHp_ClampsToMax()
        {
            var (ctx, _, player) = BuildContext();
            var stat = player.GetStat("Hitpoints");
            ctx.Player.SetHp(stat.Max + 5000);
            Assert.AreEqual(stat.Max, stat.BaseValue, "Over-Max values should clamp to Max.");
        }

        [Test]
        public void SetHpFraction_SetsValueAsFractionOfMax()
        {
            var (ctx, _, player) = BuildContext();
            var max = player.GetStat("Hitpoints").Max;
            ctx.Player.SetHpFraction(0.5f);
            Assert.AreEqual(max / 2, player.GetStatValue("Hitpoints", -1),
                "SetHpFraction(0.5) should set BaseValue to Max/2.");
        }

        [Test]
        public void SetHpMax_FullyHeals()
        {
            var (ctx, _, player) = BuildContext();
            ctx.Player.SetHpFraction(0.1f);
            ctx.Player.SetHpMax();
            var stat = player.GetStat("Hitpoints");
            Assert.AreEqual(stat.Max, stat.BaseValue);
        }

        // ======================================================
        // Stats
        // ======================================================

        [Test]
        public void SetStat_SetsBaseValue()
        {
            var (ctx, _, player) = BuildContext();
            ctx.Player.SetStat("Strength", 25);
            Assert.AreEqual(25, player.GetStatValue("Strength", -1));
        }

        [Test]
        public void SetStatMax_ThenSetStat_LetsValueExceedDefaultCap()
        {
            var (ctx, _, player) = BuildContext();
            ctx.Player.SetStatMax("Strength", 100);
            ctx.Player.SetStat("Strength", 80);
            var stat = player.GetStat("Strength");
            Assert.AreEqual(100, stat.Max);
            Assert.AreEqual(80, stat.BaseValue);
        }

        [Test]
        public void SetStat_UnknownStat_LogsAndSkips()
        {
            var (ctx, _, _) = BuildContext();
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"stat not found"));
            Assert.DoesNotThrow(() => ctx.Player.SetStat("NoSuchStat", 5));
        }

        // ======================================================
        // Mutations
        // ======================================================

        [Test]
        public void AddMutation_AttachesMutationAsPart()
        {
            // FireBoltMutation is a real class (player doesn't start with it —
            // the starting mutation is FlamingHandsMutation). After AddMutation,
            // the mutation should be an attached Part on the player.
            //
            // Note: we check BaseLevel, not Level. Level is CAPPED by the player's
            // Level stat via GetMutationCap (level/2+1 at Level 1 = 1). BaseLevel
            // is the raw level the scenario library asked for, unaffected by the
            // player-level cap — which is what the library's contract covers.
            var (ctx, _, player) = BuildContext();
            ctx.Player.AddMutation("FireBoltMutation", level: 2);
            var mutation = player.GetPart<FireBoltMutation>();
            Assert.IsNotNull(mutation, "FireBoltMutation should be attached after AddMutation.");
            Assert.AreEqual(2, mutation.BaseLevel,
                "BaseLevel should match the requested level (Level is separately capped by player Level).");
        }

        [Test]
        public void AddMutation_DefaultLevel3_WhenOmitted()
        {
            // Phase 2c default level = 3 (boosted vs. blueprint level 1).
            // Check BaseLevel — see note on AddMutation_AttachesMutationAsPart.
            var (ctx, _, player) = BuildContext();
            ctx.Player.AddMutation("FireBoltMutation");
            var mutation = player.GetPart<FireBoltMutation>();
            Assert.IsNotNull(mutation);
            Assert.AreEqual(3, mutation.BaseLevel,
                "Default BaseLevel should be 3 per Phase 2c decision.");
        }

        [Test]
        public void AddMutation_UnknownClass_LogsAndSkips()
        {
            // MutationsPart logs internally for unknown class; our wrapper also warns.
            LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex(@"AddMutation returned false"));
            // MutationsPart may also emit a log of its own — accept any log during the call.
            LogAssert.ignoreFailingMessages = true;
            try
            {
                var (ctx, _, _) = BuildContext();
                Assert.DoesNotThrow(() => ctx.Player.AddMutation("DefinitelyFakeMutation"));
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }

        // ======================================================
        // Inventory
        // ======================================================

        [Test]
        public void GiveItem_AddsOneItemToCarriedInventory()
        {
            var (ctx, _, player) = BuildContext();
            int beforeCount = player.GetPart<InventoryPart>().Objects.Count;
            ctx.Player.GiveItem("HealingTonic");
            int afterCount = player.GetPart<InventoryPart>().Objects.Count;
            Assert.AreEqual(beforeCount + 1, afterCount,
                "Carried inventory count should increase by exactly 1.");
        }

        [Test]
        public void GiveItem_WithCount_StacksViaStackerPart()
        {
            // ShortSword has a StackerPart, so 3 spawns auto-merge into a single
            // entry with StackCount=3 (rather than 3 distinct entries). This pins
            // that behavior and documents the contract: GiveItem(count) always
            // results in `count` total of the item, but the shape depends on
            // whether the blueprint is stackable.
            var (ctx, _, player) = BuildContext();
            var inv = player.GetPart<InventoryPart>();

            ctx.Player.GiveItem("ShortSword", count: 3);

            int totalShortSwords = 0;
            foreach (var item in inv.Objects)
            {
                if (item.BlueprintName != "ShortSword") continue;
                var stacker = item.GetPart<StackerPart>();
                totalShortSwords += (stacker != null) ? stacker.StackCount : 1;
            }
            Assert.AreEqual(3, totalShortSwords,
                "Three GiveItem calls should produce 3 items total — either as 3 entries (non-stackable) or 1 stack of 3 (stackable).");
        }

        [Test]
        public void GiveItem_UnknownBlueprint_LogsAndSkips()
        {
            LogAssert.Expect(LogType.Error, "EntityFactory: unknown blueprint 'NotARealItem'");
            LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex(@"blueprint 'NotARealItem' not found"));
            var (ctx, _, _) = BuildContext();
            Assert.DoesNotThrow(() => ctx.Player.GiveItem("NotARealItem"));
        }

        [Test]
        public void Equip_AddsItemAndEquipsItOnBody()
        {
            var (ctx, _, player) = BuildContext();
            ctx.Player.Equip("ShortSword");
            var inv = player.GetPart<InventoryPart>();
            bool swordEquipped = false;
            foreach (var kvp in inv.EquippedItems)
                if (kvp.Value != null && kvp.Value.BlueprintName == "ShortSword")
                    swordEquipped = true;
            Assert.IsTrue(swordEquipped, "ShortSword should appear in EquippedItems after Equip call.");
        }

        [Test]
        public void ClearInventory_RemovesAllCarriedItems()
        {
            var (ctx, _, player) = BuildContext();
            ctx.Player.GiveItem("HealingTonic", 2).GiveItem("ShortSword");
            ctx.Player.ClearInventory();
            Assert.AreEqual(0, player.GetPart<InventoryPart>().Objects.Count,
                "All carried items should be removed.");
        }

        // ======================================================
        // Faction reputation
        // ======================================================

        [Test]
        public void SetFactionReputation_ChangesValue()
        {
            var (ctx, _, _) = BuildContext();
            ctx.Player.SetFactionReputation("Villagers", -75);
            Assert.AreEqual(-75, PlayerReputation.Get("Villagers"));
        }

        [Test]
        public void ModifyFactionReputation_AppliesDelta()
        {
            var (ctx, _, _) = BuildContext();
            ctx.Player.SetFactionReputation("Villagers", 0);
            ctx.Player.ModifyFactionReputation("Villagers", 30);
            Assert.AreEqual(30, PlayerReputation.Get("Villagers"));
        }

        // ======================================================
        // Fluent chaining (sanity that multiple methods chain cleanly)
        // ======================================================

        [Test]
        public void FluentChain_AppliesAllMethodsInOrder()
        {
            var (ctx, zone, player) = BuildContext();
            ctx.Player
               .Teleport(55, 16)
               .SetStatMax("Strength", 100).SetStat("Strength", 75)
               .SetHpFraction(0.5f)
               .GiveItem("HealingTonic", 2)
               .Equip("ShortSword")
               .SetFactionReputation("Villagers", 100);

            var pos = zone.GetEntityPosition(player);
            Assert.AreEqual((55, 16), (pos.x, pos.y));
            Assert.AreEqual(75, player.GetStatValue("Strength", -1));
            Assert.AreEqual(100, player.GetStat("Strength").Max);
            var hp = player.GetStat("Hitpoints");
            Assert.AreEqual(hp.Max / 2, hp.BaseValue);
            // HealingTonic may stack — assert at least 1 entry in inventory (the stack, or the individuals).
            Assert.GreaterOrEqual(player.GetPart<InventoryPart>().Objects.Count, 1);
            Assert.AreEqual(100, PlayerReputation.Get("Villagers"));
        }
    }
}
