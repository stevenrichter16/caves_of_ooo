using System;
using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Tests for Tier 3b (AIGuardPart) and Tier 3c (AIWellVisitorPart).
    /// Validates that concrete AIBehaviorPart subclasses respond to AIBoredEvent
    /// and push the correct goals onto the brain's stack.
    /// </summary>
    [TestFixture]
    public class AIBehaviorPartTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        // --- Helpers ---

        private Entity CreateCreature(string faction, int hp = 15)
        {
            var entity = new Entity { BlueprintName = "TestCreature" };
            entity.Tags["Creature"] = "";
            if (!string.IsNullOrEmpty(faction))
                entity.Tags["Faction"] = faction;
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Toughness"] = new Stat { Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = faction ?? "creature" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = "1d4" });
            entity.AddPart(new ArmorPart());
            return entity;
        }

        private Entity CreateWarden(Zone zone, int x, int y)
        {
            var entity = CreateCreature("Villagers");
            var brain = new BrainPart
            {
                SightRadius = 12,
                Wanders = false,
                WandersRandomly = false,
                Staying = true,
                CurrentZone = zone,
                Rng = new Random(42)
            };
            entity.AddPart(brain);
            entity.AddPart(new AIGuardPart());
            zone.AddEntity(entity, x, y);

            // Set starting cell (normally done by GameBootstrap)
            brain.StartingCellX = x;
            brain.StartingCellY = y;

            return entity;
        }

        private Entity CreateFarmerWithWellVisitor(Zone zone, int x, int y, int chance = 100)
        {
            var entity = CreateCreature("Villagers");
            entity.Tags["AllowIdleBehavior"] = "";
            var brain = new BrainPart
            {
                Wanders = false,
                WandersRandomly = false,
                Staying = true,
                CurrentZone = zone,
                Rng = new Random(42)
            };
            entity.AddPart(brain);
            entity.AddPart(new AIWellVisitorPart { Chance = chance });
            zone.AddEntity(entity, x, y);
            brain.StartingCellX = x;
            brain.StartingCellY = y;
            return entity;
        }

        private Entity CreateWell(Zone zone, int x, int y)
        {
            var well = new Entity { BlueprintName = "Well" };
            well.Tags["Solid"] = "";
            well.AddPart(new RenderPart { DisplayName = "well", RenderString = "O" });
            well.AddPart(new PhysicsPart { Solid = true });
            zone.AddEntity(well, x, y);
            return well;
        }

        // ========================
        // Tier 3b: AIGuardPart
        // ========================

        [Test]
        public void AIGuard_PushesGuardGoalOnBored()
        {
            var zone = new Zone("TestZone");
            var warden = CreateWarden(zone, 10, 10);
            var brain = warden.GetPart<BrainPart>();

            warden.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsTrue(brain.HasGoal<GuardGoal>(),
                "AIGuard should push GuardGoal when the NPC is bored");
        }

        [Test]
        public void AIGuard_GuardGoalScansForHostiles()
        {
            var zone = new Zone("TestZone");
            var warden = CreateWarden(zone, 10, 10);
            var brain = warden.GetPart<BrainPart>();

            // Add hostile Snapjaw within sight (Villagers are hostile to Snapjaws)
            var snapjaw = CreateCreature("Snapjaws");
            zone.AddEntity(snapjaw, 12, 10);

            // First tick: BoredGoal detects hostile (before AIGuard fires)
            warden.FireEvent(GameEvent.New("TakeTurn"));

            Assert.AreEqual(AIState.Chase, brain.CurrentState,
                "Warden should chase hostile Snapjaw (BoredGoal hostile scan fires before AIGuard)");
        }

        [Test]
        public void AIGuard_ReturnsToPostAfterCombat()
        {
            var zone = new Zone("TestZone");
            var warden = CreateWarden(zone, 10, 10);
            var brain = warden.GetPart<BrainPart>();

            // First tick: pushes GuardGoal
            warden.FireEvent(GameEvent.New("TakeTurn"));

            // Displace the warden from its post
            zone.MoveEntity(warden, 15, 10);

            // Run several ticks — GuardGoal should push MoveToGoal to return
            for (int i = 0; i < 15; i++)
                warden.FireEvent(GameEvent.New("TakeTurn"));

            var pos = zone.GetEntityPosition(warden);
            Assert.AreEqual(10, pos.x, "Warden should have returned to guard post X");
            Assert.AreEqual(10, pos.y, "Warden should have returned to guard post Y");
        }

        [Test]
        public void AIGuard_DoesNotConsumeBoredWithoutStartingCell()
        {
            // Test via direct AIBored event (not TakeTurn) because HandleTakeTurn
            // auto-sets StartingCell before the goal stack runs.
            var zone = new Zone("TestZone");
            var entity = CreateCreature("Villagers");
            var brain = new BrainPart
            {
                CurrentZone = zone,
                Rng = new Random(42)
            };
            entity.AddPart(brain);
            entity.AddPart(new AIGuardPart());
            zone.AddEntity(entity, 10, 10);
            // StartingCell is NOT set (default -1, -1)

            bool consumed = !AIBoredEvent.Check(entity);

            Assert.IsFalse(consumed,
                "AIGuard should not consume AIBored without a StartingCell");
            Assert.IsFalse(brain.HasGoal<GuardGoal>(),
                "AIGuard should not push GuardGoal without a StartingCell");
        }

        [Test]
        public void AIGuard_AtPost_ReactsToHostileImmediately()
        {
            // Verifies the GuardGoal reactivity fix: at post, GuardGoal doesn't
            // push WaitGoal, so it re-scans for hostiles every tick.
            var zone = new Zone("TestZone");
            var warden = CreateWarden(zone, 10, 10);
            var brain = warden.GetPart<BrainPart>();

            // First tick: pushes GuardGoal, which idles at post
            warden.FireEvent(GameEvent.New("TakeTurn"));
            Assert.IsTrue(brain.HasGoal<GuardGoal>());

            // Add hostile Snapjaw — should react on the very next tick
            var snapjaw = CreateCreature("Snapjaws");
            zone.AddEntity(snapjaw, 12, 10);

            warden.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsTrue(brain.HasGoal<KillGoal>(),
                "Warden should react to hostile Snapjaw immediately (no WaitGoal blocking)");
        }

        // ========================
        // Tier 3c: AIWellVisitorPart
        [Test]
        public void AIGuard_GoalStackStable_AfterMultipleTurns()
        {
            // GuardGoal should stay on the stack without duplicating.
            // After N turns with no hostiles, stack should be exactly [BoredGoal, GuardGoal].
            var zone = new Zone("TestZone");
            var warden = CreateWarden(zone, 10, 10);
            var brain = warden.GetPart<BrainPart>();

            for (int i = 0; i < 20; i++)
                warden.FireEvent(GameEvent.New("TakeTurn"));

            // Stack should be stable: BoredGoal at bottom, GuardGoal on top
            Assert.AreEqual(2, brain.GoalCount,
                "Stack should be [BoredGoal, GuardGoal] — no extra goals from repeated ticks");
            Assert.IsTrue(brain.HasGoal<BoredGoal>());
            Assert.IsTrue(brain.HasGoal<GuardGoal>());

            // Warden should still be at post
            var pos = zone.GetEntityPosition(warden);
            Assert.AreEqual(10, pos.x);
            Assert.AreEqual(10, pos.y);
        }

        [Test]
        public void AIGuard_FullCombatCycle_ChaseAndReturnToPost()
        {
            // Full integration: hostile appears, warden chases, hostile dies,
            // warden walks back to post.
            var zone = new Zone("TestZone");
            var warden = CreateWarden(zone, 10, 10);
            var brain = warden.GetPart<BrainPart>();
            // Give warden a strong weapon to ensure kill
            warden.GetPart<MeleeWeaponPart>().BaseDamage = "10d10";

            // First tick: pushes GuardGoal
            warden.FireEvent(GameEvent.New("TakeTurn"));
            Assert.IsTrue(brain.HasGoal<GuardGoal>());

            // Snapjaw appears nearby
            var snapjaw = CreateCreature("Snapjaws");
            snapjaw.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 1, Min = 0, Max = 1 };
            zone.AddEntity(snapjaw, 12, 10);

            // Run ticks — warden should chase and kill the snapjaw
            for (int i = 0; i < 5; i++)
            {
                warden.FireEvent(GameEvent.New("TakeTurn"));
                // If snapjaw is dead (removed from zone), stop
                if (zone.GetEntityCell(snapjaw) == null) break;
            }

            // Snapjaw should be dead
            Assert.IsNull(zone.GetEntityCell(snapjaw),
                "Snapjaw should be dead after warden attack");

            // Warden drifted from post during chase. Run more ticks to return.
            for (int i = 0; i < 15; i++)
                warden.FireEvent(GameEvent.New("TakeTurn"));

            var pos = zone.GetEntityPosition(warden);
            Assert.AreEqual(10, pos.x, "Warden should return to guard post X after combat");
            Assert.AreEqual(10, pos.y, "Warden should return to guard post Y after combat");
        }

        // ========================
        // Tier 3c: AIWellVisitorPart
        // ========================

        [Test]
        public void AIWellVisitor_WalksTowardWell()
        {
            var zone = new Zone("TestZone");
            CreateWell(zone, 20, 12);
            // Chance=100 ensures it always fires
            var farmer = CreateFarmerWithWellVisitor(zone, 5, 5, chance: 100);
            var brain = farmer.GetPart<BrainPart>();

            farmer.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsTrue(brain.HasGoal<MoveToGoal>(),
                "AIWellVisitor should push MoveToGoal toward the well");
        }

        [Test]
        public void AIWellVisitor_TargetsAdjacentToWell()
        {
            var zone = new Zone("TestZone");
            CreateWell(zone, 20, 12);
            var farmer = CreateFarmerWithWellVisitor(zone, 5, 5, chance: 100);
            var brain = farmer.GetPart<BrainPart>();

            // Directly fire AIBored to get the MoveToGoal pushed onto the stack
            // before TakeTurn's child-chain executes it (which would start walking).
            AIBoredEvent.Check(farmer);

            Assert.IsTrue(brain.HasGoal<MoveToGoal>(),
                "AIWellVisitor should push MoveToGoal");

            // Find the MoveToGoal on the stack
            MoveToGoal moveGoal = null;
            // PeekGoal returns the top — which should be MoveToGoal since AIBored pushed it
            var top = brain.PeekGoal();
            if (top is MoveToGoal m) moveGoal = m;

            Assert.IsNotNull(moveGoal, "Top goal should be MoveToGoal");

            // The target should be adjacent to the well (20, 12), not on the well itself
            int dist = AIHelpers.ChebyshevDistance(moveGoal.TargetX, moveGoal.TargetY, 20, 12);
            Assert.AreEqual(1, dist,
                $"MoveToGoal target ({moveGoal.TargetX},{moveGoal.TargetY}) should be 1 cell from well (20,12)");
        }

        [Test]
        public void AIWellVisitor_NoWellInZone_DoesNothing()
        {
            var zone = new Zone("TestZone");
            // No well placed
            var farmer = CreateFarmerWithWellVisitor(zone, 5, 5, chance: 100);
            var brain = farmer.GetPart<BrainPart>();

            farmer.FireEvent(GameEvent.New("TakeTurn"));

            // Without a well, AIWellVisitor should not consume the event.
            // The NPC falls through to normal staying behavior.
            Assert.IsFalse(brain.HasGoal<MoveToGoal>(),
                "AIWellVisitor should not push MoveToGoal when no well exists");
        }

        [Test]
        public void AIWellVisitor_ProbabilityGate_WithZeroChance()
        {
            var zone = new Zone("TestZone");
            CreateWell(zone, 20, 12);
            var farmer = CreateFarmerWithWellVisitor(zone, 5, 5, chance: 0);
            var brain = farmer.GetPart<BrainPart>();

            // With Chance=0, should never fire
            for (int i = 0; i < 20; i++)
                farmer.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsFalse(brain.HasGoal<MoveToGoal>(),
                "AIWellVisitor with Chance=0 should never push MoveToGoal");
        }

        // ========================
        // Blueprint integration (loads real Objects.json)
        // ========================

        [Test]
        public void Warden_Blueprint_HasAIGuard()
        {
            FactionManager.Initialize();
            var factory = new EntityFactory();
            string blueprintPath = Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json");
            factory.LoadBlueprints(File.ReadAllText(blueprintPath));

            var warden = factory.CreateEntity("Warden");
            Assert.IsNotNull(warden);
            Assert.IsNotNull(warden.GetPart<AIGuardPart>(),
                "Warden blueprint should have AIGuardPart attached");
        }

        [Test]
        public void Farmer_Blueprint_HasAIWellVisitor()
        {
            FactionManager.Initialize();
            var factory = new EntityFactory();
            string blueprintPath = Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json");
            factory.LoadBlueprints(File.ReadAllText(blueprintPath));

            var farmer = factory.CreateEntity("Farmer");
            Assert.IsNotNull(farmer);
            var wellVisitor = farmer.GetPart<AIWellVisitorPart>();
            Assert.IsNotNull(wellVisitor,
                "Farmer blueprint should have AIWellVisitorPart attached");
            Assert.AreEqual(5, wellVisitor.Chance,
                "Farmer's AIWellVisitor should have 5% chance per tick");
        }
    }
}
