using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    [TestFixture]
    public class AIBoredEventTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        /// <summary>Test part that intercepts AIBored and consumes it.</summary>
        private class TestBoredHandler : AIBehaviorPart
        {
            public override string Name => "TestBoredHandler";
            public bool WasCalled;

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == AIBoredEvent.ID)
                {
                    WasCalled = true;
                    e.Handled = true;
                    return false;
                }
                return true;
            }
        }

        /// <summary>Test part that sees AIBored but does NOT consume it.</summary>
        private class TestPassthroughHandler : AIBehaviorPart
        {
            public override string Name => "TestPassthrough";
            public bool WasCalled;

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == AIBoredEvent.ID)
                    WasCalled = true;
                return true;
            }
        }

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

        private Entity CreatePlayer(int hp = 20)
        {
            var entity = CreateCreature(null, hp);
            entity.Tags["Player"] = "";
            return entity;
        }

        [Test]
        public void AIBoredEvent_HandlerConsumes_CreatureDoesNotWander()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature("Snapjaws");
            var handler = new TestBoredHandler();
            creature.AddPart(handler);
            var brain = new BrainPart
            {
                Wanders = true,
                WandersRandomly = true,
                CurrentZone = zone,
                Rng = new Random(42)
            };
            creature.AddPart(brain);
            zone.AddEntity(creature, 10, 10);

            creature.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsTrue(handler.WasCalled, "AIBored handler should have been called");
            // NPC should NOT have moved (handler consumed the event)
            var pos = zone.GetEntityPosition(creature);
            Assert.AreEqual(10, pos.x);
            Assert.AreEqual(10, pos.y);
        }

        [Test]
        public void AIBoredEvent_NoHandler_CreatureWanders()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature("Snapjaws");
            var brain = new BrainPart
            {
                Wanders = true,
                WandersRandomly = true,
                CurrentZone = zone,
                Rng = new Random(42)
            };
            creature.AddPart(brain);
            zone.AddEntity(creature, 10, 10);

            creature.FireEvent(GameEvent.New("TakeTurn"));

            var pos = zone.GetEntityPosition(creature);
            Assert.IsTrue(pos.x != 10 || pos.y != 10, "Creature should wander when no handler");
        }

        [Test]
        public void AIBoredEvent_HostileTakesPriority()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature("Snapjaws");
            var handler = new TestBoredHandler();
            creature.AddPart(handler);
            var brain = new BrainPart
            {
                SightRadius = 10,
                CurrentZone = zone,
                Rng = new Random(42)
            };
            creature.AddPart(brain);
            zone.AddEntity(creature, 5, 5);

            var player = CreatePlayer();
            zone.AddEntity(player, 8, 5);

            creature.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsFalse(handler.WasCalled, "Handler should NOT be called when hostile is present");
            Assert.AreEqual(AIState.Chase, brain.CurrentState);
        }

        [Test]
        public void AIBoredEvent_UnhandledPassthrough_CreatureWanders()
        {
            var zone = new Zone("TestZone");
            var creature = CreateCreature("Snapjaws");
            var passthrough = new TestPassthroughHandler();
            creature.AddPart(passthrough);
            var brain = new BrainPart
            {
                Wanders = true,
                WandersRandomly = true,
                CurrentZone = zone,
                Rng = new Random(42)
            };
            creature.AddPart(brain);
            zone.AddEntity(creature, 10, 10);

            creature.FireEvent(GameEvent.New("TakeTurn"));

            Assert.IsTrue(passthrough.WasCalled, "Passthrough handler should see the event");
            var pos = zone.GetEntityPosition(creature);
            Assert.IsTrue(pos.x != 10 || pos.y != 10, "Creature should still wander (event not consumed)");
        }
    }
}
