using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M4 — MoveToInteriorGoal / MoveToExteriorGoal + AIHelpers.FindNearestCellWhere.
    /// Exercises the BFS predicate search, goal Finished/TakeAction paths,
    /// and the MaxTurns safety net (a CoO-native addition; Qud has no timeout
    /// on these goals).
    /// </summary>
    [TestFixture]
    public class MoveToInteriorExteriorGoalTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        // ========================
        // Helpers
        // ========================

        private Entity CreateCreature(Zone zone, int x, int y)
        {
            var entity = new Entity { BlueprintName = "TestCreature" };
            entity.Tags["Creature"] = "";
            entity.Tags["Faction"] = "Villagers";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Toughness"] = new Stat { Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = "villager" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = "1d4" });
            entity.AddPart(new ArmorPart());
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(42) };
            entity.AddPart(brain);
            zone.AddEntity(entity, x, y);
            brain.StartingCellX = x;
            brain.StartingCellY = y;
            return entity;
        }

        /// <summary>Mark a rectangular block of cells as IsInterior=true.</summary>
        private void TagInterior(Zone zone, int x0, int y0, int x1, int y1)
        {
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                {
                    var c = zone.GetCell(x, y);
                    if (c != null) c.IsInterior = true;
                }
        }

        /// <summary>Place a solid wall entity at (x, y) to block BFS.</summary>
        private void AddWall(Zone zone, int x, int y)
        {
            var wall = new Entity { BlueprintName = "TestWall" };
            wall.Tags["Solid"] = "";
            wall.AddPart(new RenderPart { DisplayName = "wall" });
            wall.AddPart(new PhysicsPart { Solid = true });
            zone.AddEntity(wall, x, y);
        }

        // ========================
        // MoveToInteriorGoal
        // ========================

        [Test]
        public void MoveToInteriorGoal_Finished_WhenAlreadyInterior()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 5, 5);
            zone.GetCell(5, 5).IsInterior = true;
            var brain = creature.GetPart<BrainPart>();

            var goal = new MoveToInteriorGoal();
            brain.PushGoal(goal);

            Assert.IsTrue(goal.Finished(),
                "MoveToInteriorGoal should finish immediately when NPC is already on an interior cell.");
        }

        [Test]
        public void MoveToInteriorGoal_PushesMoveToChild_WhenExterior_WithReachableInterior()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 1, 1);
            TagInterior(zone, 5, 5, 7, 7);
            var brain = creature.GetPart<BrainPart>();

            var goal = new MoveToInteriorGoal();
            brain.PushGoal(goal);
            goal.TakeAction();

            Assert.IsTrue(brain.HasGoal<MoveToGoal>(),
                "MoveToInteriorGoal should push a MoveToGoal child when the NPC is on an exterior cell.");
        }

        [Test]
        public void MoveToInteriorGoal_FailsToParent_WhenNoInteriorWithinRadius()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 10, 10);
            var brain = creature.GetPart<BrainPart>();

            // No interior cells anywhere — search should return null and goal fails.
            var goal = new MoveToInteriorGoal(maxSearchRadius: 5);
            brain.PushGoal(goal);
            goal.TakeAction();

            Assert.IsFalse(brain.HasGoal<MoveToInteriorGoal>(),
                "MoveToInteriorGoal should pop itself (FailToParent) when no interior cell is reachable.");
        }

        [Test]
        public void MoveToInteriorGoal_Finished_WhenAgeExceedsMaxTurns()
        {
            var goal = new MoveToInteriorGoal(maxTurns: 5);
            goal.Age = 6;
            Assert.IsTrue(goal.Finished(), "Age > MaxTurns should terminate the goal (CoO safety net).");
        }

        [Test]
        public void MoveToInteriorGoal_MaxTurnsClampsRegardlessOfCurrentCellState()
        {
            // Regression pin for the doc-claim that MaxTurns is the CoO
            // safety net: even if the NPC is mid-pathing on an exterior
            // cell (not yet at an interior), Age > MaxTurns MUST terminate
            // the goal. This prevents an infinite spin if FindNearestCellWhere
            // is called every tick and the chosen target becomes unreachable
            // partway through.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 1, 1);
            TagInterior(zone, 20, 20, 22, 22);
            var brain = creature.GetPart<BrainPart>();

            var goal = new MoveToInteriorGoal(maxTurns: 5);
            brain.PushGoal(goal);

            // Simulate: NPC has been pathing for 6 ticks but hasn't reached
            // any interior cell. Current position still exterior.
            goal.Age = 6;

            // Precondition: creature is on an exterior cell (counter-check
            // that the test isn't trivially passing via the already-interior
            // branch).
            var hereCell = zone.GetCell(1, 1);
            Assert.IsFalse(hereCell.IsInterior,
                "Test setup: creature should be on an exterior cell at (1,1).");

            Assert.IsTrue(goal.Finished(),
                "Age > MaxTurns must terminate the goal even mid-journey (CoO safety net).");
        }

        [Test]
        public void MoveToInteriorGoal_IsBusyAndCannotFight()
        {
            var goal = new MoveToInteriorGoal();
            Assert.IsTrue(goal.IsBusy(), "Sheltering NPC should be busy.");
            Assert.IsFalse(goal.CanFight(), "Sheltering NPC should not engage combat mid-path.");
        }

        [Test]
        public void MoveToInteriorGoal_EmitsSeekingShelterThought_WhenExterior()
        {
            // Regression pin for the Phase-10 narrative-signal contract.
            // Previously fixed for FleeLocationGoal in commit 045580d;
            // mirror that pin here so a future refactor can't silently
            // drop the Think() call.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 1, 1);
            TagInterior(zone, 5, 5, 7, 7);
            var brain = creature.GetPart<BrainPart>();

            var goal = new MoveToInteriorGoal();
            brain.PushGoal(goal);
            goal.TakeAction();

            Assert.AreEqual("seeking shelter", brain.LastThought,
                "MoveToInteriorGoal should write 'seeking shelter' to BrainPart.LastThought.");
        }

        [Test]
        public void MoveToInteriorGoal_DoesNotEmitThought_WhenAlreadyInterior()
        {
            // Counter-check to the previous test: already-inside early-return
            // should NOT call Think(). Confirms we don't accidentally set
            // LastThought on the already-satisfied path.
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 5, 5);
            zone.GetCell(5, 5).IsInterior = true;
            var brain = creature.GetPart<BrainPart>();

            var goal = new MoveToInteriorGoal();
            brain.PushGoal(goal);
            // Clear any baseline Think set by PushGoal internals (defensive).
            brain.LastThought = null;
            goal.TakeAction();

            Assert.IsNull(brain.LastThought,
                "Already-interior NPC should not emit the 'seeking shelter' narrative signal.");
        }

        // ========================
        // MoveToExteriorGoal
        // ========================

        [Test]
        public void MoveToExteriorGoal_Finished_WhenAlreadyExterior()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature(zone, 5, 5);
            // Default IsInterior=false ⇒ creature is already outside.
            var brain = creature.GetPart<BrainPart>();

            var goal = new MoveToExteriorGoal();
            brain.PushGoal(goal);

            Assert.IsTrue(goal.Finished(),
                "MoveToExteriorGoal should finish immediately when NPC is already on an exterior cell.");
        }

        [Test]
        public void MoveToExteriorGoal_PushesMoveToChild_WhenInterior_WithReachableExterior()
        {
            var zone = new Zone("TestZone");
            // Tag a small interior block surrounded by exterior cells; put
            // the creature in the middle.
            TagInterior(zone, 4, 4, 8, 8);
            var creature = CreateCreature(zone, 6, 6);
            var brain = creature.GetPart<BrainPart>();

            var goal = new MoveToExteriorGoal();
            brain.PushGoal(goal);
            goal.TakeAction();

            Assert.IsTrue(brain.HasGoal<MoveToGoal>(),
                "MoveToExteriorGoal should push a MoveToGoal child when the NPC is on an interior cell.");
        }

        [Test]
        public void MoveToExteriorGoal_FailsToParent_WhenAllReachableCellsAreInterior()
        {
            var zone = new Zone("TestZone");
            // Tag every cell in a 5-radius bubble as interior; outside the
            // bubble is exterior but not reachable within maxSearchRadius=3.
            TagInterior(zone, 0, 0, 30, 30);
            var creature = CreateCreature(zone, 10, 10);
            var brain = creature.GetPart<BrainPart>();

            var goal = new MoveToExteriorGoal(maxSearchRadius: 3);
            brain.PushGoal(goal);
            goal.TakeAction();

            Assert.IsFalse(brain.HasGoal<MoveToExteriorGoal>(),
                "MoveToExteriorGoal should pop itself when no exterior cell is reachable within radius.");
        }

        [Test]
        public void MoveToExteriorGoal_Finished_WhenAgeExceedsMaxTurns()
        {
            var goal = new MoveToExteriorGoal(maxTurns: 5);
            goal.Age = 6;
            Assert.IsTrue(goal.Finished(), "Age > MaxTurns should terminate the goal (CoO safety net).");
        }

        [Test]
        public void MoveToExteriorGoal_EmitsHeadingOutsideThought_WhenInterior()
        {
            // Mirrors the MoveToInteriorGoal narrative-signal pin above.
            var zone = new Zone("TestZone");
            TagInterior(zone, 4, 4, 8, 8);
            var creature = CreateCreature(zone, 6, 6);
            var brain = creature.GetPart<BrainPart>();

            var goal = new MoveToExteriorGoal();
            brain.PushGoal(goal);
            goal.TakeAction();

            Assert.AreEqual("heading outside", brain.LastThought,
                "MoveToExteriorGoal should write 'heading outside' to BrainPart.LastThought.");
        }

        // ========================
        // AIHelpers.FindNearestCellWhere
        // ========================

        [Test]
        public void FindNearestCellWhere_ReturnsClosestMatch()
        {
            var zone = new Zone("TestZone");
            // Two candidate cells — one at distance 3, one at distance 10.
            zone.GetCell(5, 8).IsInterior = true;   // distance 3 from (5, 5)
            zone.GetCell(5, 15).IsInterior = true;  // distance 10

            var target = AIHelpers.FindNearestCellWhere(
                zone, 5, 5,
                c => c.IsInterior,
                maxRadius: 20);

            Assert.IsNotNull(target, "Expected a match within radius.");
            Assert.AreEqual(5, target.Value.x);
            Assert.AreEqual(8, target.Value.y);
        }

        [Test]
        public void FindNearestCellWhere_ReturnsNull_WhenNoMatchInRadius()
        {
            var zone = new Zone("TestZone");
            zone.GetCell(20, 20).IsInterior = true;  // out of range

            var target = AIHelpers.FindNearestCellWhere(
                zone, 5, 5,
                c => c.IsInterior,
                maxRadius: 5);

            Assert.IsNull(target, "No cell should match within a 5-radius.");
        }

        [Test]
        public void FindNearestCellWhere_DoesNotTraverseSolidCells()
        {
            var zone = new Zone("TestZone");

            // Build a wall line that seals off the interior target.
            // Search starts at (5, 5). Target at (5, 10).
            // Wall line at y=7 from x=0 to x=40 blocks the 4-neighbor BFS.
            for (int wx = 0; wx <= 40; wx++)
                AddWall(zone, wx, 7);

            // Target sits behind the wall.
            zone.GetCell(5, 10).IsInterior = true;

            var target = AIHelpers.FindNearestCellWhere(
                zone, 5, 5,
                c => c.IsInterior,
                maxRadius: 30);

            Assert.IsNull(target, "BFS should not reach a cell sealed off by walls.");
        }

        [Test]
        public void FindNearestCellWhere_HonorsMaxRadius()
        {
            var zone = new Zone("TestZone");
            zone.GetCell(5, 18).IsInterior = true;  // distance 13 from (5, 5)

            var tooShort = AIHelpers.FindNearestCellWhere(
                zone, 5, 5, c => c.IsInterior, maxRadius: 10);
            Assert.IsNull(tooShort, "Match at distance 13 should not be found with maxRadius=10.");

            var farEnough = AIHelpers.FindNearestCellWhere(
                zone, 5, 5, c => c.IsInterior, maxRadius: 20);
            Assert.IsNotNull(farEnough, "Match at distance 13 should be found with maxRadius=20.");
        }
    }
}
