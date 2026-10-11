using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class CompanionNavigationTests
    {
        Zone zone;
        Entity leader, follower;
        FollowLeaderGoal goal;

        [SetUp] public void SetUp()
        {
            FactionManager.Initialize();
            LiquidRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,
                "Resources/Content/Data/LiquidDefinitions"), "*.json").Select(File.ReadAllText));
            zone = new Zone("companion-navigation");
            leader = Creature("leader", 16, 10); leader.SetTag("Player");
            follower = Creature("follower", 10, 10);
            Assert.True(follower.GetPart<BrainPart>().SetPartyLeader(leader));
            goal = new FollowLeaderGoal(leader);
            follower.GetPart<BrainPart>().PushGoal(goal);
        }
        [TearDown] public void TearDown() { LiquidRegistry.ResetForTests(); }
        Entity Creature(string id, int x, int y)
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.SetTag("Creature"); e.SetTag("Faction", "Villagers");
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new BrainPart { CurrentZone = zone, Rng = new Random(17) });
            e.AddPart(new StatusEffectsPart());
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 100, Max = 100 };
            foreach (string stat in new[] { "Strength", "Toughness", "Agility" })
                e.Statistics[stat] = new Stat { Name = stat, BaseValue = 20, Max = 100 };
            Assert.True(zone.AddEntity(e, x, y)); return e;
        }
        void Wall(int x, int y)
        {
            var e = new Entity(); e.SetTag("Solid"); e.AddPart(new PhysicsPart { Solid = true });
            Assert.True(zone.AddEntity(e, x, y));
        }
        void Pool(int x, int y, string liquid)
        {
            var e = new Entity(); e.AddPart(new LiquidPoolPart { LiquidId = liquid, Volume = 100 });
            Assert.True(zone.AddEntity(e, x, y));
        }
        void Turn() => follower.FireEventAndRelease(GameEvent.New("TakeTurn"));
        (int x, int y) Position => zone.GetEntityPosition(follower);
        int Distance => Math.Max(Math.Abs(Position.x - 16), Math.Abs(Position.y - 10));

        [Test] public void ActualFollowTurnRoutesAroundBuildingWallAndRemainsFollowing()
        {
            for (int y = 7; y <= 13; y++) Wall(11, y);
            var start = Position;
            for (int i = 0; i < 25 && Distance > goal.CloseEnoughDistance; i++) Turn();
            Assert.LessOrEqual(Distance, goal.CloseEnoughDistance);
            Assert.AreNotEqual(start, Position);
            Assert.AreSame(goal, follower.GetPart<BrainPart>().PeekGoal());
            Assert.AreSame(leader, follower.GetPart<BrainPart>().PartyLeader);
        }
        [TestCase("acid", false)] [TestCase("acid", true)] [TestCase("water", false)]
        public void ActualFollowTurnHonorsActorTerrainCost(string liquid, bool immune)
        {
            if (immune) follower.Statistics["AcidResistance"] = new Stat { Name = "AcidResistance", BaseValue = 100, Max = 100 };
            Pool(11, 10, liquid);
            bool dangerous = liquid == "acid" && !immune;
            Assert.AreEqual(dangerous, TerrainNavigationWeight.ForStep(zone, 11, 10, follower) > 0);
            Turn();
            Assert.AreEqual(!dangerous, Position == (11, 10));
            Assert.Zero(TerrainNavigationWeight.ForStep(zone, Position.x, Position.y, follower));
        }
        [Test] public void SoleHazardousPassageStillPermitsFollowing()
        {
            for (int x = 0; x < Zone.Width; x++) { Wall(x, 9); Wall(x, 11); }
            Pool(11, 10, "acid");
            Turn(); Assert.AreEqual((11, 10), Position);
            Assert.Greater(TerrainNavigationWeight.ForStep(zone, Position.x, Position.y, follower), 0);
        }
        [TestCase("capable")] [TestCase("incapable")] [TestCase("locked")]
        public void FollowingPreservesDoorPermissionAndStationaryOpeningAction(string mode)
        {
            for (int y = 0; y < Zone.Height; y++) if (y != 10) Wall(11, y);
            var door = new Entity(); door.AddPart(new PhysicsPart()); door.AddPart(new RenderPart());
            var part = new DoorPart { IsOpen = false }; door.AddPart(part);
            if (mode != "incapable") follower.SetTag("CanOpenDoors");
            if (mode == "locked") door.AddPart(new LockPart { KeyId = "missing", IsLocked = true });
            Assert.True(zone.AddEntity(door, 11, 10));
            Turn(); Assert.AreEqual(mode == "capable", part.IsOpen); Assert.AreEqual((10, 10), Position);
            if (mode == "capable") { Turn(); Assert.AreEqual((11, 10), Position); }
        }
        [Test] public void UnreachableLeaderDoesNotTeleportOrChangePartyOwnership()
        {
            for (int y = 0; y < Zone.Height; y++) Wall(11, y);
            for (int i = 0; i < 8; i++) Turn();
            Assert.AreEqual((10, 10), Position);
            Assert.AreSame(leader, follower.GetPart<BrainPart>().PartyLeader);
            Assert.AreSame(goal, follower.GetPart<BrainPart>().PeekGoal());
        }
        [Test] public void LeaderMoveReplansNextTurnAndCloseFollowerIdles()
        {
            Turn(); Assert.AreEqual((11, 10), Position);
            Assert.True(zone.MoveEntity(leader, 11, 16));
            Turn(); Assert.AreEqual((11, 11), Position);
            Assert.True(zone.MoveEntity(leader, 11, 12));
            Turn(); Assert.AreEqual((11, 11), Position); Assert.Zero(goal.Age);
        }
    }
}
