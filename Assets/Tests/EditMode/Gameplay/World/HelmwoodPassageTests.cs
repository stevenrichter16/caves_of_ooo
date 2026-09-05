using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class HelmwoodPassageTests
    {
        private EntityFactory _factory;
        [OneTimeSetUp] public void Load()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup() { FactionManager.Initialize(); }

        [Test]
        public void PassageContentHasActualWaterAndItsOwnSprite()
        {
            Assert.IsTrue(_factory.Blueprints.ContainsKey("HelmwoodWaterPassage"));
            var marker = _factory.CreateEntity("HelmwoodWaterPassage");
            Assert.IsNotNull(marker.GetPart<WaterPassagePart>());
            Assert.IsNotNull(marker.GetPart<LiquidPoolPart>());
            Assert.IsNull(marker.GetPart<StairsDownPart>()); Assert.IsNull(marker.GetPart<StairsUpPart>());
            Assert.IsTrue(EnvironmentSpriteRenderer.FixtureSprites.Any(r => r.Blueprint == "HelmwoodWaterPassage"));
            Assert.IsNotNull(Resources.Load<Sprite>("Sprites/Environment/helmwood_water_passage"));
        }

        [Test]
        public void WaterShortcutTravelsBothWaysAtExactEndpointsAndSkipsDescent()
        {
            var (manager, surface, floor, actor) = Pair();
            var down = HelmwoodPassages.TryTravel(actor, surface, true, manager);
            Assert.IsTrue(down.Success, down.ErrorReason); Assert.AreSame(floor, down.NewZone);
            Assert.AreEqual((20, 12), floor.GetEntityPosition(actor)); Assert.IsNull(surface.GetEntityCell(actor));
            Assert.IsFalse(manager.CachedZones.Keys.Any(id => id.EndsWith(".1")));
            var up = HelmwoodPassages.TryTravel(actor, floor, false, manager);
            Assert.IsTrue(up.Success, up.ErrorReason); Assert.AreSame(surface, up.NewZone);
            Assert.AreEqual((5, 5), surface.GetEntityPosition(actor)); Assert.IsNull(floor.GetEntityCell(actor));
        }

        [TestCase("wrong direction")] [TestCase("wrong cell")] [TestCase("missing source")]
        [TestCase("missing peer")] [TestCase("blocked peer")] [TestCase("no connection")]
        public void RefusedPassageDoesNotMoveOrDuplicateActor(string failure)
        {
            var (manager, surface, floor, actor) = Pair();
            if (failure == "wrong cell") surface.MoveEntity(actor, 4, 5);
            if (failure == "missing source") surface.RemoveEntity(surface.GetCell(5, 5).Objects.First(e => e.HasPart<WaterPassagePart>()));
            if (failure == "missing peer") floor.RemoveEntity(floor.GetCell(20, 12).Objects.First(e => e.HasPart<WaterPassagePart>()));
            if (failure == "blocked peer") { var block = new Entity(); block.AddPart(new PhysicsPart { Solid = true }); floor.AddEntity(block, 20, 12); }
            if (failure == "no connection") manager.GetConnections(surface.ZoneID).Clear();
            var original = surface.GetEntityPosition(actor);
            var result = HelmwoodPassages.TryTravel(actor, surface, failure != "wrong direction", manager);
            Assert.IsFalse(result.Success); Assert.IsNotEmpty(result.ErrorReason);
            Assert.AreEqual(original, surface.GetEntityPosition(actor)); Assert.IsNull(floor.GetEntityCell(actor));
        }

        [Test]
        public void HiddenPassageIsNeverAStairAutoWalkTarget()
        {
            var (manager, surface, floor, actor) = Pair();
            surface.MoveEntity(actor, 4, 5);
            Assert.IsNull(StairTravel.FindTarget(surface, actor, true));
            Assert.IsNull(StairTravel.FindTarget(surface, actor, false));
            var stairs = new Entity(); stairs.SetTag("StairsDown"); stairs.AddPart(new StairsDownPart()); surface.AddEntity(stairs, 8, 5);
            Assert.AreEqual((8, 5), StairTravel.FindTarget(surface, actor, true));
        }

        [Test]
        public void FollowersUseTheRealPassageWithTheirLeader()
        {
            var (manager, surface, floor, actor) = Pair();
            actor.AddPart(new BrainPart { CurrentZone = surface });
            var follower = _factory.CreateEntity("Wardline"); surface.AddEntity(follower, 6, 5);
            follower.GetPart<BrainPart>().CurrentZone = surface;
            follower.GetPart<BrainPart>().SetPartyLeader(actor);
            Assert.IsTrue(HelmwoodPassages.TryTravel(actor, surface, true, manager).Success);
            Assert.IsNull(surface.GetEntityCell(follower)); Assert.IsNotNull(floor.GetEntityCell(follower));
            Assert.AreSame(floor, follower.GetPart<BrainPart>().CurrentZone);
        }

        [Test]
        public void SavedPairedPassageStillMakesTheSameShortcut()
        {
            var (manager, surface, floor, actor) = Pair();
            var turns = new TurnManager(); turns.AddEntity(actor);
            manager.SetActiveZone(surface);
            var state = GameSessionState.Capture("helmwood-test", "test", manager, turns, actor);
            GameSessionState loaded;
            using (var stream = new MemoryStream())
            {
                state.Save(new SaveWriter(stream)); stream.Position = 0;
                loaded = GameSessionState.Load(new SaveReader(stream, _factory));
            }
            var result = HelmwoodPassages.TryTravel(loaded.Player, loaded.ZoneManager.ActiveZone, true, loaded.ZoneManager);
            Assert.IsTrue(result.Success, result.ErrorReason);
            Assert.AreEqual(floor.ZoneID, result.NewZone.ZoneID);
            Assert.AreEqual((20, 12), result.NewZone.GetEntityPosition(loaded.Player));
        }

        [Test]
        public void RareUndergroundHelmwoodAlwaysHasARealUsablePair()
        {
            var site = SinkholeSites.All.First(s => s.Name == "Ginmere");
            int encounters = 0;
            for (int seed = 1; seed <= 32; seed++)
            {
                var manager = new OverworldZoneManager(_factory, seed);
                var floor = manager.GetZone($"Overworld.{site.X}.{site.Y}.2");
                var frogs = floor.GetEntitiesWithTag("Creature").Where(e => e.BlueprintName == "HelmwoodFrog").ToList();
                if (frogs.Count == 0) continue;
                encounters++; Assert.AreEqual(1, frogs.Count);
                var passage = floor.GetAllEntities().Single(e => e.HasPart<WaterPassagePart>());
                var cell = floor.GetEntityCell(passage); var actor = Player(); floor.AddEntity(actor, cell.X, cell.Y);
                var travel = HelmwoodPassages.TryTravel(actor, floor, false, manager);
                Assert.IsTrue(travel.Success, travel.ErrorReason);
                Assert.AreEqual(0, WorldMap.GetDepth(travel.NewZone.ZoneID));
                var frogCell = floor.GetEntityCell(frogs[0]);
                Assert.LessOrEqual(AIHelpers.ChebyshevDistance(cell.X, cell.Y, frogCell.X, frogCell.Y), 2);
            }
            Assert.That(encounters, Is.InRange(1, 12), "rare, but non-vacuously shipped");
        }

        [TestCase(false)] [TestCase(true)]
        public void RegeneratingEitherEndpointKeepsOnePairAndOneFrog(bool surfaceFirst)
        {
            var site = SinkholeSites.All.First(s => s.Name == "Ginmere");
            OverworldZoneManager manager = null; Zone floor = null;
            for (int seed = 1; seed <= 32; seed++)
            {
                var candidate = new OverworldZoneManager(_factory, seed);
                var z = candidate.GetZone($"Overworld.{site.X}.{site.Y}.2");
                if (!z.GetAllEntities().Any(e => e.HasPart<WaterPassagePart>())) continue;
                manager = candidate; floor = z; break;
            }
            Assert.IsNotNull(manager, "must find an actual passage seed");
            string surfaceID = $"Overworld.{site.X}.{site.Y}.0";
            for (int repeat = 0; repeat < 2; repeat++)
            {
                string id = surfaceFirst ? surfaceID : floor.ZoneID;
                manager.UnloadZone(id); manager.GetZone(id);
                floor = manager.GetZone(floor.ZoneID);
                Assert.AreEqual(1, manager.GetConnections(surfaceID).Count(c => c.Type == "WaterPassage"));
                Assert.AreEqual(1, manager.GetConnections(floor.ZoneID).Count(c => c.Type == "WaterPassage"));
                Assert.AreEqual(1, floor.GetEntitiesWithTag("Creature").Count(e => e.BlueprintName == "HelmwoodFrog"));
                var marker = floor.GetAllEntities().Single(e => e.HasPart<WaterPassagePart>());
                var cell = floor.GetEntityCell(marker); var actor = Player(); floor.AddEntity(actor, cell.X, cell.Y);
                var up = HelmwoodPassages.TryTravel(actor, floor, false, manager);
                Assert.IsTrue(up.Success, up.ErrorReason); up.NewZone.RemoveEntity(actor);
                surfaceFirst = !surfaceFirst;
            }
        }

        private (OverworldZoneManager, Zone, Zone, Entity) Pair()
        {
            var manager = new OverworldZoneManager(_factory, 7);
            var surface = new Zone("Overworld.9.8.0"); var floor = new Zone("Overworld.9.8.2");
            manager.CachedZones[surface.ZoneID] = surface; manager.CachedZones[floor.ZoneID] = floor;
            var a = new Entity(); a.AddPart(new WaterPassagePart()); surface.AddEntity(a, 5, 5);
            var b = new Entity(); b.AddPart(new WaterPassagePart()); floor.AddEntity(b, 20, 12);
            manager.RegisterConnection(new ZoneConnection { SourceZoneID = surface.ZoneID, SourceX = 5, SourceY = 5,
                TargetZoneID = floor.ZoneID, TargetX = 20, TargetY = 12, Type = "WaterPassage" });
            var actor = Player(); surface.AddEntity(actor, 5, 5);
            return (manager, surface, floor, actor);
        }
        private static Entity Player()
        {
            var e = new Entity { BlueprintName = "TestPlayer" }; e.SetTag("Player"); e.SetTag("Creature");
            e.AddPart(new PhysicsPart { Solid = true });
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 1000, Max = 1000 };
            return e;
        }
    }
}
