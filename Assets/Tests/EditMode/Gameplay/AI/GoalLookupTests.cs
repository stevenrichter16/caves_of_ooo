using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase 5 tests: BrainPart goal-lookup primitives and CommandGoal.
    /// Mirrors the subset of Qud's composition API that's actually used in practice.
    /// </summary>
    [TestFixture]
    public class GoalLookupTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        private Entity CreateMinimalCreature(Zone zone, int x, int y)
        {
            var entity = new Entity { BlueprintName = "Test" };
            entity.Tags["Creature"] = "";
            entity.Tags["Faction"] = "Villagers";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart());
            entity.AddPart(new PhysicsPart { Solid = true });
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(42) };
            entity.AddPart(brain);
            zone.AddEntity(entity, x, y);
            return entity;
        }

        // ========================
        // HasGoal(string)
        // ========================

        [Test]
        public void HasGoal_String_MatchesByClassName()
        {
            var zone = new Zone("TestZone");
            var entity = CreateMinimalCreature(zone, 5, 5);
            var brain = entity.GetPart<BrainPart>();

            brain.PushGoal(new WaitGoal(5));
            Assert.IsTrue(brain.HasGoal("WaitGoal"));
            Assert.IsFalse(brain.HasGoal("KillGoal"));
        }

        [Test]
        public void HasGoal_String_EmptyOrNull_ReturnsFalse()
        {
            var zone = new Zone("TestZone");
            var entity = CreateMinimalCreature(zone, 5, 5);
            var brain = entity.GetPart<BrainPart>();

            brain.PushGoal(new WaitGoal(5));
            Assert.IsFalse(brain.HasGoal(""));
            Assert.IsFalse(brain.HasGoal(null));
        }

        // ========================
        // FindGoal<T> and FindGoal(string)
        // ========================

        [Test]
        public void FindGoal_Generic_ReturnsInstance()
        {
            var zone = new Zone("TestZone");
            var entity = CreateMinimalCreature(zone, 5, 5);
            var brain = entity.GetPart<BrainPart>();

            var wait = new WaitGoal(7);
            brain.PushGoal(wait);

            var found = brain.FindGoal<WaitGoal>();
            Assert.IsNotNull(found);
            Assert.AreSame(wait, found, "FindGoal<T> should return the exact instance");
            Assert.AreEqual(7, found.Duration);
        }

        [Test]
        public void FindGoal_Generic_ReturnsNullWhenMissing()
        {
            var zone = new Zone("TestZone");
            var entity = CreateMinimalCreature(zone, 5, 5);
            var brain = entity.GetPart<BrainPart>();

            Assert.IsNull(brain.FindGoal<WaitGoal>());
        }

        [Test]
        public void FindGoal_Generic_ReturnsTopmostWhenMultiple()
        {
            var zone = new Zone("TestZone");
            var entity = CreateMinimalCreature(zone, 5, 5);
            var brain = entity.GetPart<BrainPart>();

            var lowWait = new WaitGoal(1);
            var highWait = new WaitGoal(99);
            brain.PushGoal(lowWait);
            brain.PushGoal(highWait);

            var found = brain.FindGoal<WaitGoal>();
            Assert.AreSame(highWait, found, "FindGoal should scan top-down and return the most recent");
        }

        [Test]
        public void FindGoal_String_MatchesByClassName()
        {
            var zone = new Zone("TestZone");
            var entity = CreateMinimalCreature(zone, 5, 5);
            var brain = entity.GetPart<BrainPart>();

            var wait = new WaitGoal(5);
            brain.PushGoal(wait);

            var found = brain.FindGoal("WaitGoal");
            Assert.AreSame(wait, found);
        }

        [Test]
        public void FindGoal_String_ReturnsNullForMissing()
        {
            var zone = new Zone("TestZone");
            var entity = CreateMinimalCreature(zone, 5, 5);
            var brain = entity.GetPart<BrainPart>();

            brain.PushGoal(new WaitGoal(1));
            Assert.IsNull(brain.FindGoal("KillGoal"));
            Assert.IsNull(brain.FindGoal(""));
            Assert.IsNull(brain.FindGoal(null));
        }

        // ========================
        // HasGoalOtherThan
        // ========================

        [Test]
        public void HasGoalOtherThan_EmptyStack_ReturnsFalse()
        {
            var zone = new Zone("TestZone");
            var entity = CreateMinimalCreature(zone, 5, 5);
            var brain = entity.GetPart<BrainPart>();

            Assert.IsFalse(brain.HasGoalOtherThan("BoredGoal"));
        }

        [Test]
        public void HasGoalOtherThan_OnlyBoredGoal_ReturnsFalse()
        {
            var zone = new Zone("TestZone");
            var entity = CreateMinimalCreature(zone, 5, 5);
            var brain = entity.GetPart<BrainPart>();

            brain.PushGoal(new BoredGoal());
            Assert.IsFalse(brain.HasGoalOtherThan("BoredGoal"),
                "Stack with only BoredGoal should return false");
        }

        [Test]
        public void HasGoalOtherThan_BoredPlusOther_ReturnsTrue()
        {
            var zone = new Zone("TestZone");
            var entity = CreateMinimalCreature(zone, 5, 5);
            var brain = entity.GetPart<BrainPart>();

            brain.PushGoal(new BoredGoal());
            brain.PushGoal(new WaitGoal(5));

            Assert.IsTrue(brain.HasGoalOtherThan("BoredGoal"),
                "Stack with BoredGoal + WaitGoal should return true (other goals exist)");
        }

        // ========================
        // CommandGoal
        // ========================

        /// <summary>Part that records whether a specific command event was received.</summary>
        private class CommandListener : Part
        {
            public override string Name => "CommandListener";
            public string ReceivedCommand;

            public override bool HandleEvent(GameEvent e)
            {
                ReceivedCommand = e.ID;
                return true;
            }
        }

        [Test]
        public void CommandGoal_FiresEventOnEntity()
        {
            var zone = new Zone("TestZone");
            var entity = CreateMinimalCreature(zone, 5, 5);
            var listener = new CommandListener();
            entity.AddPart(listener);

            var goal = new CommandGoal("CommandSubmerge");
            var brain = entity.GetPart<BrainPart>();
            brain.PushGoal(goal);
            goal.TakeAction();

            Assert.AreEqual("CommandSubmerge", listener.ReceivedCommand,
                "CommandGoal should fire the named event on ParentEntity");
        }

        [Test]
        public void CommandGoal_PopsItselfAfterFiring()
        {
            var zone = new Zone("TestZone");
            var entity = CreateMinimalCreature(zone, 5, 5);
            var brain = entity.GetPart<BrainPart>();

            var goal = new CommandGoal("TestCommand");
            brain.PushGoal(goal);
            Assert.AreEqual(1, brain.GoalCount);

            goal.TakeAction();

            Assert.AreEqual(0, brain.GoalCount,
                "CommandGoal should pop itself after firing the event");
        }

        [Test]
        public void CommandGoal_CanFightIsFalse()
        {
            var goal = new CommandGoal("TestCommand");
            Assert.IsFalse(goal.CanFight(),
                "CommandGoal.CanFight should return false — command is an atomic action");
        }

        [Test]
        public void CommandGoal_NullOrEmptyCommand_NoOp()
        {
            var zone = new Zone("TestZone");
            var entity = CreateMinimalCreature(zone, 5, 5);
            var listener = new CommandListener();
            entity.AddPart(listener);
            var brain = entity.GetPart<BrainPart>();

            var goal = new CommandGoal("");
            brain.PushGoal(goal);
            goal.TakeAction();

            Assert.IsNull(listener.ReceivedCommand,
                "Empty command should not fire an event");
            Assert.AreEqual(0, brain.GoalCount, "Goal should still pop");
        }
    }
}
