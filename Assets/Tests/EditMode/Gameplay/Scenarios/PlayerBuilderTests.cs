using CavesOfOoo.Core;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CavesOfOoo.Tests.Scenarios
{
    /// <summary>
    /// Phase 2c tests — PlayerBuilder methods. Integration-style: each test
    /// gets a fresh ScenarioContext (with a real Player entity) from the
    /// shared <see cref="ScenarioTestHarness"/> and asserts state after
    /// applying builder methods.
    /// </summary>
    [TestFixture]
    public class PlayerBuilderTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() => _harness?.Dispose();

        /// <summary>
        /// Fresh context with a REAL Player blueprint at (40, 12). The Player
        /// blueprint is required here because PlayerBuilder exercises parts
        /// (MutationsPart, InventoryPart) that the minimal stub doesn't carry.
        /// </summary>
        private static (ScenarioContext ctx, Zone zone, Entity player) BuildContext()
        {
            var ctx = _harness.CreateContext(rngSeed: 98765, playerBlueprint: "Player");
            return (ctx, ctx.Zone, ctx.PlayerEntity);
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
            // (Value kept below the alpha-tuned blueprint Max of 40 —
            // SetHp clamps to Max by contract; SetHpMax raises the
            // ceiling. See AlphaCombatStakesTests for the 40 HP pin.)
            var (ctx, _, player) = BuildContext();
            ctx.Player.SetHp(30);
            Assert.AreEqual(30, player.GetStatValue("Hitpoints", -1));
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
        public void AddSkill_AttachesSkillAsPart()
        {
            // Migration M4: the builder grants SKILLS. Ember Spit is a
            // real class the player also starts with via the kit — use a
            // non-kit power so the attach is unambiguous.
            var (ctx, _, player) = BuildContext();
            ctx.Player.AddSkill("Pyromancy_Kindle");
            var skill = player.GetPart<CavesOfOoo.Skills.Pyromancy_Kindle>();
            Assert.IsNotNull(skill, "Pyromancy_Kindle should be attached after AddSkill.");
        }

        [Test]
        public void AddSkill_UnknownClass_LogsAndSkips()
        {
            LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex(@"AddSkill returned false"));
            LogAssert.ignoreFailingMessages = true;
            try
            {
                var (ctx, _, _) = BuildContext();
                Assert.DoesNotThrow(() => ctx.Player.AddSkill("DefinitelyFakeSkill"));
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
