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
    /// Tests for Phase 2b EntityBuilder modifier methods: WithStat, WithStatMax,
    /// Passive/Hostile, AsPersonalEnemyOf, WithStartingCell, WithEquipment,
    /// WithInventory, WithGoal.
    ///
    /// These are integration-style tests: they construct a ScenarioContext with
    /// a real EntityFactory (loaded from the live blueprints JSON) and a
    /// synthetic Zone, then call the full spawn pipeline and assert end state.
    /// The EntityFactory is expensive to set up so we share it at the fixture
    /// level via OneTimeSetUp.
    /// </summary>
    [TestFixture]
    public class EntityBuilderModifierTests
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

        /// <summary>Build a fresh test context with a synthetic zone, stub player at (40,12), and a new TurnManager.</summary>
        private static (ScenarioContext ctx, Zone zone, Entity player) BuildContext()
        {
            var zone = new Zone("ModifierTestZone");

            // Minimal player — just an Entity with a Hitpoints stat and a zone position.
            var player = new Entity { BlueprintName = "TestPlayer" };
            player.Tags["Player"] = "";
            player.Tags["Creature"] = "";
            player.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 100, Max = 100, Min = 0 };
            zone.AddEntity(player, 40, 12);

            var tm = new TurnManager();
            var ctx = new ScenarioContext(zone, _sharedFactory, player, tm, rngSeed: 12345);
            return (ctx, zone, player);
        }

        // ===========================================
        // WithStat / WithStatMax
        // ===========================================

        [Test]
        public void WithStat_SetsStatBaseValue()
        {
            var (ctx, _, _) = BuildContext();
            var snapjaw = ctx.Spawn("Snapjaw")
                             .WithStat("Strength", 25)
                             .At(42, 12);
            Assert.IsNotNull(snapjaw);
            Assert.AreEqual(25, snapjaw.GetStatValue("Strength", -1));
        }

        [Test]
        public void WithStat_Alone_ClampsToDefaultMax30_OnUnlistedStats()
        {
            // Sanity: without WithStatMax, setting a stat above its Max=30 default
            // clamps silently. This pins the documented caveat so regressions are visible.
            var (ctx, _, _) = BuildContext();
            var snapjaw = ctx.Spawn("Snapjaw")
                             .WithStat("Strength", 999)
                             .At(42, 12);
            var stat = snapjaw.GetStat("Strength");
            Assert.LessOrEqual(stat.BaseValue, stat.Max,
                "Without WithStatMax, high values must clamp to the blueprint Max (~30).");
        }

        [Test]
        public void WithStatMax_AndWithStat_TogetherRaiseCeilingAndBaseValue()
        {
            var (ctx, _, _) = BuildContext();
            var snapjaw = ctx.Spawn("Snapjaw")
                             .WithStatMax("Strength", 100)
                             .WithStat("Strength", 80)
                             .At(42, 12);
            var stat = snapjaw.GetStat("Strength");
            Assert.AreEqual(100, stat.Max, "Max should be raised to 100.");
            Assert.AreEqual(80, stat.BaseValue, "BaseValue should land at 80 (under raised Max).");
        }

        [Test]
        public void WithStat_UnknownStat_LogsAndContinues()
        {
            // Log warning but don't throw.
            var (ctx, _, _) = BuildContext();
            Assert.DoesNotThrow(() =>
            {
                ctx.Spawn("Snapjaw")
                   .WithStat("DoesNotExistStat", 5)
                   .At(42, 12);
            });
        }

        // ===========================================
        // Passive / Hostile
        // ===========================================

        [Test]
        public void Passive_SetsBrainPassiveFlag()
        {
            var (ctx, _, _) = BuildContext();
            var snapjaw = ctx.Spawn("Snapjaw").Passive().At(42, 12);
            Assert.IsTrue(snapjaw.GetPart<BrainPart>().Passive);
        }

        [Test]
        public void Hostile_ExplicitlyUnPassivates()
        {
            // Scribe has Passive=true in blueprint. .Hostile() should flip it.
            var (ctx, _, _) = BuildContext();
            var scribe = ctx.Spawn("Scribe").Hostile().At(42, 12);
            Assert.IsFalse(scribe.GetPart<BrainPart>().Passive,
                ".Hostile() should override a blueprint-set Passive=true.");
        }

        // ===========================================
        // AsPersonalEnemyOf
        // ===========================================

        [Test]
        public void AsPersonalEnemyOf_AddsTargetToPersonalEnemies()
        {
            var (ctx, _, player) = BuildContext();
            var snapjaw = ctx.Spawn("Snapjaw").AsPersonalEnemyOf(player).At(42, 12);
            Assert.IsTrue(snapjaw.GetPart<BrainPart>().IsPersonallyHostileTo(player));
        }

        // ===========================================
        // WithStartingCell
        // ===========================================

        [Test]
        public void WithStartingCell_OverridesDefaultAutoSet()
        {
            var (ctx, _, _) = BuildContext();
            var snapjaw = ctx.Spawn("Snapjaw")
                             .WithStartingCell(55, 20)
                             .At(42, 12); // spawn at 42,12 but home is 55,20
            var brain = snapjaw.GetPart<BrainPart>();
            Assert.AreEqual(55, brain.StartingCellX);
            Assert.AreEqual(20, brain.StartingCellY);
        }

        [Test]
        public void NoStartingCell_DefaultsToSpawnCell()
        {
            // Sanity: without the override, StartingCell auto-sets to the spawn position.
            var (ctx, _, _) = BuildContext();
            var snapjaw = ctx.Spawn("Snapjaw").At(42, 12);
            var brain = snapjaw.GetPart<BrainPart>();
            Assert.AreEqual(42, brain.StartingCellX);
            Assert.AreEqual(12, brain.StartingCellY);
        }

        // ===========================================
        // WithInventory
        // ===========================================

        [Test]
        public void WithInventory_AddsItemsToInventoryPart()
        {
            var (ctx, _, _) = BuildContext();
            var snapjaw = ctx.Spawn("Snapjaw")
                             .WithInventory("ShortSword", "HealingTonic")
                             .At(42, 12);
            var inv = snapjaw.GetPart<InventoryPart>();
            Assert.IsNotNull(inv, "Snapjaw should have an InventoryPart via Creature inheritance.");
            Assert.AreEqual(2, inv.Objects.Count,
                "Two items should have been added to carried inventory.");
        }

        [Test]
        public void WithInventory_UnknownBlueprint_LogsAndSkipsOnlyThatItem()
        {
            // EntityFactory.CreateEntity logs Debug.LogError on unknown blueprint;
            // expect it so the test runner doesn't fail on the unexpected log.
            LogAssert.Expect(LogType.Error, "EntityFactory: unknown blueprint 'TotallyFakeBlueprint'");
            // EntityBuilder then emits a warn-level skip.
            LogAssert.Expect(LogType.Warning,
                "[Scenario] WithInventory item blueprint 'TotallyFakeBlueprint' not found — skipping item.");

            var (ctx, _, _) = BuildContext();
            var snapjaw = ctx.Spawn("Snapjaw")
                             .WithInventory("ShortSword", "TotallyFakeBlueprint", "HealingTonic")
                             .At(42, 12);
            var inv = snapjaw.GetPart<InventoryPart>();
            Assert.AreEqual(2, inv.Objects.Count,
                "Only the 2 real blueprints should have been added; the fake is skipped.");
        }

        // ===========================================
        // WithEquipment
        // ===========================================

        [Test]
        public void WithEquipment_EquipsItemOnSpawnedCreature()
        {
            var (ctx, _, _) = BuildContext();
            var snapjaw = ctx.Spawn("Snapjaw")
                             .WithEquipment("ShortSword")
                             .At(42, 12);
            // Confirm the item ended up somewhere on the creature (inventory or body)
            var inv = snapjaw.GetPart<InventoryPart>();
            Assert.IsNotNull(inv);
            bool swordFound = false;
            foreach (var carried in inv.Objects)
                if (carried.BlueprintName == "ShortSword") swordFound = true;
            foreach (var kvp in inv.EquippedItems)
                if (kvp.Value != null && kvp.Value.BlueprintName == "ShortSword") swordFound = true;
            Assert.IsTrue(swordFound, "ShortSword should be either carried or equipped after WithEquipment.");
        }

        // ===========================================
        // WithGoal
        // ===========================================

        [Test]
        public void WithGoal_PushesGoalOntoBrainStack()
        {
            var (ctx, _, _) = BuildContext();
            var customGoal = new BoredGoal();
            var snapjaw = ctx.Spawn("Snapjaw")
                             .WithGoal(customGoal)
                             .At(42, 12);
            var brain = snapjaw.GetPart<BrainPart>();
            Assert.IsTrue(brain.HasGoal<BoredGoal>(),
                "BoredGoal should be on the brain's goal stack after WithGoal.");
        }
    }
}
