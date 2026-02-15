using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    public class FactionAITests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        // ========================
        // Helper Methods
        // ========================

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
            entity.GetPart<RenderPart>().DisplayName = "you";
            return entity;
        }

        private Entity CreateCreatureWithBrain(string faction, Zone zone, int x, int y, int sightRadius = 10)
        {
            var entity = CreateCreature(faction);
            var brain = new BrainPart
            {
                SightRadius = sightRadius,
                Wanders = true,
                WandersRandomly = true,
                CurrentZone = zone,
                Rng = new Random(42)
            };
            entity.AddPart(brain);
            zone.AddEntity(entity, x, y);
            return entity;
        }

        private Zone CreateZone()
        {
            return new Zone("TestZone");
        }

        // ========================
        // FactionManager
        // ========================

        [Test]
        public void FactionManager_SameFaction_Returns100()
        {
            Assert.AreEqual(100, FactionManager.GetFactionFeeling("Snapjaws", "Snapjaws"));
        }

        [Test]
        public void FactionManager_HostileFactions_ReturnsNegative()
        {
            Assert.AreEqual(-100, FactionManager.GetFactionFeeling("Snapjaws", "Player"));
            Assert.AreEqual(-100, FactionManager.GetFactionFeeling("Player", "Snapjaws"));
        }

        [Test]
        public void FactionManager_UnknownFaction_ReturnsNeutral()
        {
            Assert.AreEqual(0, FactionManager.GetFactionFeeling("Snapjaws", "Robots"));
        }

        [Test]
        public void FactionManager_SetFactionFeeling_Overrides()
        {
            FactionManager.SetFactionFeeling("Snapjaws", "Player", 50);
            Assert.AreEqual(50, FactionManager.GetFactionFeeling("Snapjaws", "Player"));
        }

        [Test]
        public void FactionManager_GetFeeling_EntityToEntity_Hostile()
        {
            var snapjaw = CreateCreature("Snapjaws");
            var player = CreatePlayer();
            Assert.Less(FactionManager.GetFeeling(snapjaw, player), FactionManager.HOSTILE_THRESHOLD);
        }

        [Test]
        public void FactionManager_GetFeeling_SameFactionEntities_Allied()
        {
            var snap1 = CreateCreature("Snapjaws");
            var snap2 = CreateCreature("Snapjaws");
            Assert.AreEqual(100, FactionManager.GetFeeling(snap1, snap2));
        }

        [Test]
        public void FactionManager_IsHostile_True()
        {
            var snapjaw = CreateCreature("Snapjaws");
            var player = CreatePlayer();
            Assert.IsTrue(FactionManager.IsHostile(snapjaw, player));
        }

        [Test]
        public void FactionManager_IsHostile_False_SameFaction()
        {
            var snap1 = CreateCreature("Snapjaws");
            var snap2 = CreateCreature("Snapjaws");
            Assert.IsFalse(FactionManager.IsHostile(snap1, snap2));
        }

        [Test]
        public void FactionManager_GetFaction_PlayerTag()
        {
            var player = CreatePlayer();
            Assert.AreEqual("Player", FactionManager.GetFaction(player));
        }

        [Test]
        public void FactionManager_GetFaction_FactionTag()
        {
            var snapjaw = CreateCreature("Snapjaws");
            Assert.AreEqual("Snapjaws", FactionManager.GetFaction(snapjaw));
        }

        [Test]
        public void FactionManager_Reset_ClearsState()
        {
            FactionManager.Reset();
            Assert.AreEqual(0, FactionManager.GetFactionFeeling("Snapjaws", "Player"));
        }

        // ========================
        // AIHelpers - Distance
        // ========================

        [Test]
        public void ChebyshevDistance_Adjacent_Is1()
        {
            Assert.AreEqual(1, AIHelpers.ChebyshevDistance(5, 5, 6, 6));
        }

        [Test]
        public void ChebyshevDistance_Same_Is0()
        {
            Assert.AreEqual(0, AIHelpers.ChebyshevDistance(5, 5, 5, 5));
        }

        [Test]
        public void ChebyshevDistance_Far()
        {
            Assert.AreEqual(10, AIHelpers.ChebyshevDistance(0, 0, 10, 5));
        }

        // ========================
        // AIHelpers - StepToward
        // ========================

        [Test]
        public void StepToward_Diagonal()
        {
            var (dx, dy) = AIHelpers.StepToward(5, 5, 8, 8);
            Assert.AreEqual(1, dx);
            Assert.AreEqual(1, dy);
        }

        [Test]
        public void StepToward_Cardinal()
        {
            var (dx, dy) = AIHelpers.StepToward(5, 5, 5, 8);
            Assert.AreEqual(0, dx);
            Assert.AreEqual(1, dy);
        }

        [Test]
        public void StepToward_SamePosition()
        {
            var (dx, dy) = AIHelpers.StepToward(5, 5, 5, 5);
            Assert.AreEqual(0, dx);
            Assert.AreEqual(0, dy);
        }

        [Test]
        public void StepAway_MovesAway()
        {
            var (dx, dy) = AIHelpers.StepAway(5, 5, 3, 3);
            Assert.AreEqual(1, dx);
            Assert.AreEqual(1, dy);
        }

        // ========================
        // AIHelpers - IsAdjacent
        // ========================

        [Test]
        public void IsAdjacent_True()
        {
            Assert.IsTrue(AIHelpers.IsAdjacent(5, 5, 6, 5));
            Assert.IsTrue(AIHelpers.IsAdjacent(5, 5, 6, 6));
        }

        [Test]
        public void IsAdjacent_False()
        {
            Assert.IsFalse(AIHelpers.IsAdjacent(5, 5, 7, 5));
            Assert.IsFalse(AIHelpers.IsAdjacent(5, 5, 5, 5));
        }

        // ========================
        // AIHelpers - Line of Sight
        // ========================

        [Test]
        public void HasLineOfSight_Clear()
        {
            var zone = CreateZone();
            Assert.IsTrue(AIHelpers.HasLineOfSight(zone, 5, 5, 10, 5));
        }

        [Test]
        public void HasLineOfSight_Blocked()
        {
            var zone = CreateZone();
            // Place a wall between (5,5) and (10,5)
            var wall = new Entity();
            wall.Tags["Solid"] = "";
            wall.AddPart(new RenderPart());
            wall.AddPart(new PhysicsPart { Solid = true });
            zone.AddEntity(wall, 7, 5);

            Assert.IsFalse(AIHelpers.HasLineOfSight(zone, 5, 5, 10, 5));
        }

        // ========================
        // AIHelpers - FindNearestHostile
        // ========================

        [Test]
        public void FindNearestHostile_FindsEnemy()
        {
            var zone = CreateZone();
            var snapjaw = CreateCreatureWithBrain("Snapjaws", zone, 5, 5);
            var player = CreatePlayer();
            zone.AddEntity(player, 8, 5);

            var target = AIHelpers.FindNearestHostile(snapjaw, zone, 10);
            Assert.AreEqual(player, target);
        }

        [Test]
        public void FindNearestHostile_IgnoresAllies()
        {
            var zone = CreateZone();
            var snap1 = CreateCreatureWithBrain("Snapjaws", zone, 5, 5);
            var snap2 = CreateCreature("Snapjaws");
            zone.AddEntity(snap2, 6, 5);

            var target = AIHelpers.FindNearestHostile(snap1, zone, 10);
            Assert.IsNull(target);
        }

        [Test]
        public void FindNearestHostile_OutOfRange_ReturnsNull()
        {
            var zone = CreateZone();
            var snapjaw = CreateCreatureWithBrain("Snapjaws", zone, 5, 5);
            var player = CreatePlayer();
            zone.AddEntity(player, 20, 5); // distance 15, beyond radius 10

            var target = AIHelpers.FindNearestHostile(snapjaw, zone, 10);
            Assert.IsNull(target);
        }

        [Test]
        public void FindNearestHostile_BehindWall_ReturnsNull()
        {
            var zone = CreateZone();
            var snapjaw = CreateCreatureWithBrain("Snapjaws", zone, 5, 5);
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 5);

            // Place wall between them
            var wall = new Entity();
            wall.Tags["Solid"] = "";
            wall.AddPart(new RenderPart());
            wall.AddPart(new PhysicsPart { Solid = true });
            zone.AddEntity(wall, 7, 5);

            var target = AIHelpers.FindNearestHostile(snapjaw, zone, 10);
            Assert.IsNull(target);
        }

        // ========================
        // AIHelpers - RandomPassableDirection
        // ========================

        [Test]
        public void RandomPassableDirection_FindsOpenCell()
        {
            var zone = CreateZone();
            var entity = CreateCreature("Snapjaws");
            zone.AddEntity(entity, 10, 10);

            var (dx, dy) = AIHelpers.RandomPassableDirection(entity, zone, new Random(42));
            Assert.IsTrue(dx != 0 || dy != 0, "Should find at least one open direction");
        }

        [Test]
        public void RandomPassableDirection_Surrounded_ReturnsZero()
        {
            var zone = CreateZone();
            var entity = CreateCreature("Snapjaws");
            zone.AddEntity(entity, 10, 10);

            // Surround with walls
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var wall = new Entity();
                    wall.Tags["Solid"] = "";
                    wall.AddPart(new RenderPart());
                    wall.AddPart(new PhysicsPart { Solid = true });
                    zone.AddEntity(wall, 10 + dx, 10 + dy);
                }
            }

            var (rdx, rdy) = AIHelpers.RandomPassableDirection(entity, zone, new Random(42));
            Assert.AreEqual(0, rdx);
            Assert.AreEqual(0, rdy);
        }

        // ========================
        // BrainPart
        // ========================

        [Test]
        public void BrainPart_HandlesTakeTurnEvent()
        {
            var zone = CreateZone();
            var snapjaw = CreateCreatureWithBrain("Snapjaws", zone, 5, 5);

            // Fire TakeTurn — should not throw
            snapjaw.FireEvent(GameEvent.New("TakeTurn"));
        }

        [Test]
        public void BrainPart_ChasesHostile()
        {
            var zone = CreateZone();
            var snapjaw = CreateCreatureWithBrain("Snapjaws", zone, 5, 5);
            var player = CreatePlayer();
            zone.AddEntity(player, 8, 5); // distance 3

            snapjaw.FireEvent(GameEvent.New("TakeTurn"));

            // Should have moved closer to player
            var pos = zone.GetEntityPosition(snapjaw);
            Assert.AreEqual(6, pos.x, "Snapjaw should move toward player (x)");
            Assert.AreEqual(5, pos.y, "Snapjaw should stay on same row (y)");

            var brain = snapjaw.GetPart<BrainPart>();
            Assert.AreEqual(AIState.Chase, brain.CurrentState);
        }

        [Test]
        public void BrainPart_AttacksAdjacentHostile()
        {
            var zone = CreateZone();
            var snapjaw = CreateCreatureWithBrain("Snapjaws", zone, 5, 5);
            var player = CreatePlayer(hp: 50);
            zone.AddEntity(player, 6, 5); // adjacent

            int hpBefore = player.GetStatValue("Hitpoints");
            snapjaw.FireEvent(GameEvent.New("TakeTurn"));

            // Snapjaw should not have moved (still at 5,5)
            var pos = zone.GetEntityPosition(snapjaw);
            Assert.AreEqual(5, pos.x);
            Assert.AreEqual(5, pos.y);

            // Combat should have been attempted (message log should have content)
            Assert.Greater(MessageLog.Count, 0, "Should have produced combat messages");
        }

        [Test]
        public void BrainPart_WandersWhenNoTarget()
        {
            var zone = CreateZone();
            var snapjaw = CreateCreatureWithBrain("Snapjaws", zone, 10, 10);
            // No player in zone — should wander

            snapjaw.FireEvent(GameEvent.New("TakeTurn"));

            var brain = snapjaw.GetPart<BrainPart>();
            Assert.AreEqual(AIState.Wander, brain.CurrentState);

            // Should have moved somewhere
            var pos = zone.GetEntityPosition(snapjaw);
            bool moved = pos.x != 10 || pos.y != 10;
            Assert.IsTrue(moved, "Snapjaw should have wandered to a new cell");
        }

        [Test]
        public void BrainPart_IdleWhenWanderDisabled()
        {
            var zone = CreateZone();
            var snapjaw = CreateCreature("Snapjaws");
            var brain = new BrainPart
            {
                SightRadius = 10,
                Wanders = false,
                WandersRandomly = false,
                CurrentZone = zone,
                Rng = new Random(42)
            };
            snapjaw.AddPart(brain);
            zone.AddEntity(snapjaw, 10, 10);

            snapjaw.FireEvent(GameEvent.New("TakeTurn"));

            Assert.AreEqual(AIState.Idle, brain.CurrentState);
            var pos = zone.GetEntityPosition(snapjaw);
            Assert.AreEqual(10, pos.x);
            Assert.AreEqual(10, pos.y);
        }

        [Test]
        public void BrainPart_IgnoresOutOfSightTarget()
        {
            var zone = CreateZone();
            var snapjaw = CreateCreatureWithBrain("Snapjaws", zone, 5, 5, sightRadius: 3);
            var player = CreatePlayer();
            zone.AddEntity(player, 15, 5); // distance 10, beyond sight radius 3

            snapjaw.FireEvent(GameEvent.New("TakeTurn"));

            var brain = snapjaw.GetPart<BrainPart>();
            Assert.IsNull(brain.Target, "Should not have acquired target beyond sight radius");
        }

        [Test]
        public void BrainPart_StopsChasingDeadTarget()
        {
            var zone = CreateZone();
            var snapjaw = CreateCreatureWithBrain("Snapjaws", zone, 5, 5);
            var player = CreatePlayer(hp: 50);
            zone.AddEntity(player, 8, 5);

            // First turn: acquire target
            snapjaw.FireEvent(GameEvent.New("TakeTurn"));
            var brain = snapjaw.GetPart<BrainPart>();
            Assert.IsNotNull(brain.Target);

            // Kill the target (remove from zone)
            zone.RemoveEntity(player);

            // Next turn: should clear target
            snapjaw.FireEvent(GameEvent.New("TakeTurn"));
            Assert.IsNull(brain.Target);
        }

        [Test]
        public void BrainPart_SkipsPlayerEntity()
        {
            var zone = CreateZone();
            var player = CreatePlayer();
            var brain = new BrainPart
            {
                CurrentZone = zone,
                Rng = new Random(42)
            };
            player.AddPart(brain);
            zone.AddEntity(player, 5, 5);

            // Should not throw or act
            player.FireEvent(GameEvent.New("TakeTurn"));

            // Player should not have moved
            var pos = zone.GetEntityPosition(player);
            Assert.AreEqual(5, pos.x);
            Assert.AreEqual(5, pos.y);
        }

        [Test]
        public void BrainPart_NoopWhenNoZone()
        {
            var snapjaw = CreateCreature("Snapjaws");
            var brain = new BrainPart { CurrentZone = null };
            snapjaw.AddPart(brain);

            // Should not throw
            snapjaw.FireEvent(GameEvent.New("TakeTurn"));
        }

        [Test]
        public void BrainPart_DoesNotSeeThoughWalls()
        {
            var zone = CreateZone();
            var snapjaw = CreateCreatureWithBrain("Snapjaws", zone, 5, 5);
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 5);

            // Place wall between them
            var wall = new Entity();
            wall.Tags["Solid"] = "";
            wall.AddPart(new RenderPart());
            wall.AddPart(new PhysicsPart { Solid = true });
            zone.AddEntity(wall, 7, 5);

            snapjaw.FireEvent(GameEvent.New("TakeTurn"));

            var brain = snapjaw.GetPart<BrainPart>();
            Assert.IsNull(brain.Target, "Should not see through walls");
        }

        // ========================
        // Integration: NPC Combat
        // ========================

        [Test]
        public void Integration_NPC_KillsTarget()
        {
            var zone = CreateZone();
            var snapjaw = CreateCreatureWithBrain("Snapjaws", zone, 5, 5);
            // Give snapjaw a strong weapon
            snapjaw.GetPart<MeleeWeaponPart>().BaseDamage = "10d6";

            var player = CreatePlayer(hp: 1); // 1 HP, will die
            zone.AddEntity(player, 6, 5); // adjacent

            snapjaw.FireEvent(GameEvent.New("TakeTurn"));

            // Player should be dead (removed from zone)
            Assert.IsNull(zone.GetEntityCell(player), "Player should be removed from zone after death");
        }

        // ========================
        // Blueprint Integration
        // ========================

        [Test]
        public void Blueprint_CreatureHasBrain()
        {
            var factory = new EntityFactory();
            string json = @"{
                ""Objects"": [
                    { ""Name"": ""PhysicalObject"", ""Parts"": [
                        { ""Name"": ""Render"", ""Params"": [] },
                        { ""Name"": ""Physics"", ""Params"": [] }
                    ], ""Stats"": [], ""Tags"": [] },
                    { ""Name"": ""Creature"", ""Inherits"": ""PhysicalObject"", ""Parts"": [
                        { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }] },
                        { ""Name"": ""MeleeWeapon"", ""Params"": [] },
                        { ""Name"": ""Armor"", ""Params"": [] },
                        { ""Name"": ""Brain"", ""Params"": [{ ""Key"": ""SightRadius"", ""Value"": ""10"" }] }
                    ], ""Stats"": [
                        { ""Name"": ""Hitpoints"", ""Value"": 15, ""Min"": 0, ""Max"": 15 },
                        { ""Name"": ""Speed"", ""Value"": 100, ""Min"": 25, ""Max"": 200 }
                    ], ""Tags"": [{ ""Key"": ""Creature"", ""Value"": """" }] }
                ]
            }";
            factory.LoadBlueprints(json);
            var creature = factory.CreateEntity("Creature");

            Assert.IsNotNull(creature.GetPart<BrainPart>(), "Creature should have BrainPart");
        }

        [Test]
        public void Blueprint_BrainParams_SetFromJson()
        {
            var factory = new EntityFactory();
            string json = @"{
                ""Objects"": [
                    { ""Name"": ""PhysicalObject"", ""Parts"": [
                        { ""Name"": ""Render"", ""Params"": [] },
                        { ""Name"": ""Physics"", ""Params"": [] }
                    ], ""Stats"": [], ""Tags"": [] },
                    { ""Name"": ""Creature"", ""Inherits"": ""PhysicalObject"", ""Parts"": [
                        { ""Name"": ""Brain"", ""Params"": [
                            { ""Key"": ""SightRadius"", ""Value"": ""15"" },
                            { ""Key"": ""Wanders"", ""Value"": ""true"" }
                        ] }
                    ], ""Stats"": [], ""Tags"": [{ ""Key"": ""Creature"", ""Value"": """" }] }
                ]
            }";
            factory.LoadBlueprints(json);
            var creature = factory.CreateEntity("Creature");

            var brain = creature.GetPart<BrainPart>();
            Assert.IsNotNull(brain);
            Assert.AreEqual(15, brain.SightRadius);
            Assert.IsTrue(brain.Wanders);
        }

        // ========================
        // Per-Entity Hostility Tests
        // ========================

        [Test]
        public void SetPersonallyHostile_MakesNPCHostileToPlayer()
        {
            var npc = CreateCreature("Villagers");
            var brain = new BrainPart();
            npc.AddPart(brain);
            var player = CreatePlayer();

            Assert.IsFalse(FactionManager.IsHostile(npc, player));
            Assert.IsFalse(FactionManager.IsHostile(player, npc));

            brain.SetPersonallyHostile(player);

            Assert.IsTrue(FactionManager.IsHostile(npc, player));
            Assert.IsTrue(FactionManager.IsHostile(player, npc));
            Assert.AreEqual(player, brain.Target);
            Assert.IsFalse(brain.InConversation);
        }

        [Test]
        public void PersonalHostility_DoesNotAffectOtherNPCs()
        {
            var npc1 = CreateCreature("Villagers");
            var brain1 = new BrainPart();
            npc1.AddPart(brain1);

            var npc2 = CreateCreature("Villagers");
            var brain2 = new BrainPart();
            npc2.AddPart(brain2);

            var player = CreatePlayer();

            brain1.SetPersonallyHostile(player);

            Assert.IsTrue(FactionManager.IsHostile(npc1, player));
            Assert.IsFalse(FactionManager.IsHostile(npc2, player));
        }

        [Test]
        public void IsPersonallyHostileTo_ReturnsFalseByDefault()
        {
            var brain = new BrainPart();
            var player = CreatePlayer();
            Assert.IsFalse(brain.IsPersonallyHostileTo(player));
            Assert.IsFalse(brain.IsPersonallyHostileTo(null));
        }

        [Test]
        public void SetPersonallyHostile_Idempotent()
        {
            var npc = CreateCreature("Villagers");
            var brain = new BrainPart();
            npc.AddPart(brain);
            var player = CreatePlayer();

            brain.SetPersonallyHostile(player);
            brain.SetPersonallyHostile(player);

            Assert.IsTrue(brain.IsPersonallyHostileTo(player));
            Assert.AreEqual(1, brain.PersonalEnemies.Count);
        }
    }
}
