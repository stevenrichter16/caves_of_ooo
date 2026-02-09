using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    public class TurnMovementTests
    {
        private Zone _zone;
        private EntityFactory _factory;

        private const string TestBlueprints = @"{
          ""Objects"": [
            {
              ""Name"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""?"" }] },
                { ""Name"": ""Physics"", ""Params"": [] }
              ],
              ""Stats"": [],
              ""Tags"": []
            },
            {
              ""Name"": ""Creature"",
              ""Inherits"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderLayer"", ""Value"": ""10"" }] },
                { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }] }
              ],
              ""Stats"": [
                { ""Name"": ""Hitpoints"", ""Value"": 10, ""Min"": 0, ""Max"": 100 },
                { ""Name"": ""Speed"", ""Value"": 100, ""Min"": 25, ""Max"": 200 }
              ],
              ""Tags"": [
                { ""Key"": ""Creature"", ""Value"": """" }
              ]
            },
            {
              ""Name"": ""Player"",
              ""Inherits"": ""Creature"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""DisplayName"", ""Value"": ""you"" },
                  { ""Key"": ""RenderString"", ""Value"": ""@"" },
                  { ""Key"": ""RenderLayer"", ""Value"": ""20"" }
                ]}
              ],
              ""Stats"": [],
              ""Tags"": [
                { ""Key"": ""Player"", ""Value"": """" }
              ]
            },
            {
              ""Name"": ""Wall"",
              ""Inherits"": ""PhysicalObject"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""RenderString"", ""Value"": ""#"" },
                  { ""Key"": ""RenderLayer"", ""Value"": ""0"" }
                ]},
                { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }] }
              ],
              ""Stats"": [],
              ""Tags"": [
                { ""Key"": ""Solid"", ""Value"": """" },
                { ""Key"": ""Wall"", ""Value"": """" }
              ]
            },
            {
              ""Name"": ""FastCreature"",
              ""Inherits"": ""Creature"",
              ""Parts"": [
                { ""Name"": ""Render"", ""Params"": [
                  { ""Key"": ""RenderString"", ""Value"": ""f"" }
                ]}
              ],
              ""Stats"": [
                { ""Name"": ""Speed"", ""Value"": 200, ""Min"": 25, ""Max"": 200 }
              ],
              ""Tags"": []
            }
          ]
        }";

        [SetUp]
        public void SetUp()
        {
            _zone = new Zone("TestZone");
            _factory = new EntityFactory();
            _factory.LoadBlueprints(TestBlueprints);
        }

        // ========================
        // PhysicsPart Tests
        // ========================

        [Test]
        public void PhysicsPart_BlocksMovementIntoSolidCell()
        {
            var player = _factory.CreateEntity("Player");
            _zone.AddEntity(player, 5, 5);

            var wall = _factory.CreateEntity("Wall");
            _zone.AddEntity(wall, 6, 5);

            // Try to move east into the wall
            bool moved = MovementSystem.TryMove(player, _zone, 1, 0);
            Assert.IsFalse(moved);

            // Player should still be at (5, 5)
            var pos = _zone.GetEntityPosition(player);
            Assert.AreEqual(5, pos.x);
            Assert.AreEqual(5, pos.y);
        }

        [Test]
        public void PhysicsPart_AllowsMovementIntoEmptyCell()
        {
            var player = _factory.CreateEntity("Player");
            _zone.AddEntity(player, 5, 5);

            bool moved = MovementSystem.TryMove(player, _zone, 1, 0);
            Assert.IsTrue(moved);

            var pos = _zone.GetEntityPosition(player);
            Assert.AreEqual(6, pos.x);
            Assert.AreEqual(5, pos.y);
        }

        [Test]
        public void PhysicsPart_BlocksMovementIntoCreature()
        {
            var player = _factory.CreateEntity("Player");
            _zone.AddEntity(player, 5, 5);

            var enemy = _factory.CreateEntity("Creature");
            _zone.AddEntity(enemy, 6, 5);

            // Creature has Physics.Solid=true, should block
            bool moved = MovementSystem.TryMove(player, _zone, 1, 0);
            Assert.IsFalse(moved);
        }

        [Test]
        public void MovementSystem_BlocksOutOfBounds()
        {
            var player = _factory.CreateEntity("Player");
            _zone.AddEntity(player, 0, 0);

            // Try to move west out of bounds
            bool moved = MovementSystem.TryMove(player, _zone, -1, 0);
            Assert.IsFalse(moved);
        }

        [Test]
        public void MovementSystem_FiresAfterMoveEvent()
        {
            var player = _factory.CreateEntity("Player");
            _zone.AddEntity(player, 5, 5);

            bool afterMoveFired = false;
            var listener = new TestListenerPart("AfterMove", () => afterMoveFired = true);
            player.AddPart(listener);

            MovementSystem.TryMove(player, _zone, 1, 0);

            Assert.IsTrue(afterMoveFired);
        }

        [Test]
        public void MovementSystem_OldCellCleared_NewCellPopulated()
        {
            var player = _factory.CreateEntity("Player");
            _zone.AddEntity(player, 5, 5);

            MovementSystem.TryMove(player, _zone, 0, 1);

            Assert.AreEqual(0, _zone.GetCell(5, 5).Objects.Count);
            Assert.AreEqual(1, _zone.GetCell(5, 6).Objects.Count);
        }

        // ========================
        // TurnManager Tests
        // ========================

        [Test]
        public void TurnManager_EntityGainsEnergyOnTick()
        {
            var tm = new TurnManager();
            var player = _factory.CreateEntity("Player");
            tm.AddEntity(player);

            // Speed 100, threshold 1000 — needs 10 ticks
            for (int i = 0; i < 9; i++)
                tm.Tick();

            Assert.AreEqual(900, tm.GetEnergy(player));

            // 10th tick should push over threshold
            var actor = tm.Tick();
            Assert.AreEqual(player, actor);
        }

        [Test]
        public void TurnManager_FasterEntityActsFirst()
        {
            var tm = new TurnManager();
            var slow = _factory.CreateEntity("Creature"); // Speed 100
            var fast = _factory.CreateEntity("FastCreature"); // Speed 200
            tm.AddEntity(slow);
            tm.AddEntity(fast);

            // After 5 ticks: slow=500, fast=1000 → fast acts first
            Entity firstActor = null;
            for (int i = 0; i < 10; i++)
            {
                var actor = tm.Tick();
                if (actor != null && firstActor == null)
                {
                    firstActor = actor;
                    break;
                }
            }

            Assert.AreEqual(fast, firstActor);
        }

        [Test]
        public void TurnManager_EndTurn_SpendsEnergy()
        {
            var tm = new TurnManager();
            var player = _factory.CreateEntity("Player");
            tm.AddEntity(player);

            // Tick 10 times to reach threshold
            for (int i = 0; i < 10; i++)
                tm.Tick();

            Assert.AreEqual(1000, tm.GetEnergy(player));

            tm.EndTurn(player);
            Assert.AreEqual(0, tm.GetEnergy(player));
        }

        [Test]
        public void TurnManager_ProcessUntilPlayerTurn_StopsAtPlayer()
        {
            var tm = new TurnManager();
            var player = _factory.CreateEntity("Player");
            player.SetTag("Player");
            tm.AddEntity(player);

            var result = tm.ProcessUntilPlayerTurn();
            Assert.AreEqual(player, result);
            Assert.IsTrue(tm.WaitingForInput);
        }

        [Test]
        public void TurnManager_RemoveEntity_StopsTracking()
        {
            var tm = new TurnManager();
            var entity = _factory.CreateEntity("Creature");
            tm.AddEntity(entity);
            Assert.AreEqual(1, tm.EntityCount);

            tm.RemoveEntity(entity);
            Assert.AreEqual(0, tm.EntityCount);
        }

        // ========================
        // Helper Part for testing events
        // ========================

        private class TestListenerPart : Part
        {
            private string _eventID;
            private System.Action _callback;

            public TestListenerPart(string eventID, System.Action callback)
            {
                _eventID = eventID;
                _callback = callback;
            }

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == _eventID)
                    _callback?.Invoke();
                return true;
            }
        }
    }
}
