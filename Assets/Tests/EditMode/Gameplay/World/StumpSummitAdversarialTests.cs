using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>W6.3b gate: partial activation, cross-instance isolation,
    /// real movement vetoes, habitat counterconditions and save graph reach.</summary>
    public class StumpSummitAdversarialTests
    {
        private EntityFactory _factory, _previousFactory;
        [OneTimeSetUp] public void Load()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup()
        {
            FactionManager.Initialize();
            _previousFactory = PricklebrowNestPart.Factory;
            PricklebrowNestPart.Factory = _factory;
            new TurnManager();
        }
        [TearDown] public void Cleanup() { PricklebrowNestPart.Factory = _previousFactory; }

        [TestCase(0)] [TestCase(15)]
        public void InsufficientSpaceDoesNotConsumeNestOrPartiallySpawn(int spaces)
        {
            var zone = new Zone(); var nest = Nest(zone); var actor = Actor(zone);
            var walls = new List<Entity>(); int available = 0;
            for (int r = 2; r <= 5; r++) for (int dx = -r; dx <= r; dx++) for (int dy = -r; dy <= r; dy++)
            {
                if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                if (available++ < spaces) continue;
                var wall = new Entity(); wall.AddPart(new PhysicsPart { Solid = true });
                zone.AddEntity(wall, 10 + dx, 10 + dy); walls.Add(wall);
            }
            Enter(actor, zone);
            Assert.IsFalse(nest.GetPart<PricklebrowNestPart>().Triggered);
            Assert.AreEqual(0, Defenders(zone).Count);
            foreach (var wall in walls) zone.RemoveEntity(wall);
            Reenter(actor, zone);
            Assert.AreEqual(16, Defenders(zone).Count, "failed activation remains usable after space opens");
        }

        [TestCase(false)] [TestCase(true)]
        public void MissingFactoryOrBlueprintLeavesNestUsable(bool missingBlueprint)
        {
            var zone = new Zone(); var nest = Nest(zone); var actor = Actor(zone);
            PricklebrowNestPart.Factory = missingBlueprint ? new EntityFactory() : null;
            Enter(actor, zone);
            Assert.IsFalse(nest.GetPart<PricklebrowNestPart>().Triggered);
            Assert.AreEqual(0, Defenders(zone).Count);
            PricklebrowNestPart.Factory = _factory;
            Reenter(actor, zone);
            Assert.AreEqual(16, Defenders(zone).Count);
        }

        [TestCase(0)] [TestCase(-1)]
        public void DeadVisitorCannotDisturbNest(int hp)
        {
            var zone = new Zone(); var nest = Nest(zone); var actor = Actor(zone);
            actor.Statistics["Hitpoints"].BaseValue = hp;
            Enter(actor, zone);
            Assert.IsFalse(nest.GetPart<PricklebrowNestPart>().Triggered);
            Assert.AreEqual(0, Defenders(zone).Count);
        }

        [Test]
        public void ReservedCellsNeverReceiveDefenders()
        {
            var zone = new Zone(); Nest(zone); var actor = Actor(zone);
            for (int y = 5; y <= 15; y++) zone.GenReservedCells.Add((8, y));
            Enter(actor, zone);
            var defenders = Defenders(zone); Assert.AreEqual(16, defenders.Count);
            foreach (var e in defenders) Assert.IsFalse(zone.GenReservedCells.Contains(zone.GetEntityPosition(e)));
        }

        [Test]
        public void TwoNestsHaveIndependentActivationAndScheduling()
        {
            var zone = new Zone(); Nest(zone); Nest(zone, 30, 10); var actor = Actor(zone);
            Enter(actor, zone); zone.MoveEntity(actor, 29, 10); Enter(actor, zone);
            Assert.AreEqual(32, Defenders(zone).Count);
            Assert.AreEqual(32, TurnManager.Active.EntityCount);
        }

        [Test]
        public void DefendersNeverOverlapAnotherLivingCreature()
        {
            var zone = new Zone(); Nest(zone); var actor = Actor(zone);
            var bystander = _factory.CreateEntity("SummitSinger"); zone.AddEntity(bystander, 8, 8);
            Enter(actor, zone);
            Assert.AreEqual(16, Defenders(zone).Count);
            Assert.AreEqual(1, zone.GetCell(8, 8).Objects.Count(e => e.HasTag("Creature")));
        }

        [Test]
        public void PopulationWillNotPlaceCreatureOnExistingActor()
        {
            var zone = new Zone(); var actor = Actor(zone, 20, 10);
            var table = new PopulationTable();
            table.Entries.Add(new PopulationEntry { BlueprintName = "Wardline", MinCount = 1, MaxCount = 1 });
            new PopulationBuilder(table) { HabitatFilter = (bp, cell) => cell.X == 20 && cell.Y == 10 }
                .BuildZone(zone, _factory, new Random(1));
            Assert.AreEqual(1, zone.GetCell(20, 10).Objects.Count(e => e.HasTag("Creature")));
        }

        [Test]
        public void NarrowChamberCannotAcquireANestThatCanNeverActivate()
        {
            var zone = new Zone();
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++)
            {
                if (x >= 30 && x <= 34 && y >= 10 && y <= 14 && !(x == 30 && y == 10)) continue;
                var wall = new Entity(); wall.SetTag("Solid"); wall.AddPart(new PhysicsPart { Solid = true }); zone.AddEntity(wall, x, y);
            }
            new SimaNestBuilder().BuildZone(zone, _factory, new Random(4));
            foreach (var nest in zone.GetAllEntities().Where(e => e.BlueprintName == "PricklebrowNest").ToList())
            {
                var cell = zone.GetEntityCell(nest);
                var actor = Actor(zone, cell.X, cell.Y);
                var entered = GameEvent.New("EntityEnteredCell"); entered.SetParameter("Actor", actor); entered.SetParameter("Cell", cell);
                nest.FireEventAndRelease(entered);
                Assert.IsTrue(nest.GetPart<PricklebrowNestPart>().Triggered, "every stamped nest must have a viable sixteen-cell ring");
            }
        }

        [Test]
        public void RebuiltNestStampDoesNotAddASecondNest()
        {
            var zone = new Zone(); var builder = new SimaNestBuilder();
            builder.BuildZone(zone, _factory, new Random(4));
            builder.BuildZone(zone, _factory, new Random(55));
            Assert.AreEqual(1, zone.GetAllEntities().Count(e => e.BlueprintName == "PricklebrowNest"));
        }

        [TestCase("rooted")] [TestCase("occupied")] [TestCase("removed")] [TestCase("unseen")]
        public void SentinelCannotBypassMovementOrCoverValidity(string blocked)
        {
            var zone = new Zone(); var sentinel = _factory.CreateEntity("BrocchiniaSentinel");
            var brain = sentinel.GetPart<BrainPart>(); brain.CurrentZone = zone; brain.Rng = new Random(4);
            zone.AddEntity(sentinel, 10, 10);
            var visitor = Actor(zone, blocked == "unseen" ? 7 : 9, 10);
            var tank = _factory.CreateEntity("TankBrocchinia"); zone.AddEntity(tank, 11, 10);
            if (blocked == "rooted") sentinel.ApplyEffect(new RootedEffect(5), visitor, zone);
            if (blocked == "removed") zone.RemoveEntity(tank);
            if (blocked == "occupied") { var rock = new Entity(); rock.AddPart(new PhysicsPart { Solid = true }); zone.AddEntity(rock, 11, 10); }
            if (blocked == "unseen") { var rock = new Entity(); rock.SetTag("Solid"); rock.AddPart(new PhysicsPart { Solid = true }); zone.AddEntity(rock, 8, 10); }
            sentinel.FireEventAndRelease(GameEvent.New("TakeTurn"));
            Assert.AreEqual((10, 10), zone.GetEntityPosition(sentinel));
        }

        [Test]
        public void SentinelChoosesClearCoverWhenAnotherTankIsOccupied()
        {
            var zone = new Zone(); var sentinel = _factory.CreateEntity("BrocchiniaSentinel");
            sentinel.GetPart<BrainPart>().CurrentZone = zone; sentinel.GetPart<BrainPart>().Rng = new Random(4);
            zone.AddEntity(sentinel, 10, 10); Actor(zone);
            zone.AddEntity(_factory.CreateEntity("TankBrocchinia"), 11, 10);
            zone.AddEntity(_factory.CreateEntity("SummitSinger"), 11, 10);
            zone.AddEntity(_factory.CreateEntity("TankBrocchinia"), 11, 11);
            sentinel.FireEventAndRelease(GameEvent.New("TakeTurn"));
            Assert.AreEqual((11, 11), zone.GetEntityPosition(sentinel));
        }

        [TestCase("no_threat")] [TestCase("already_sheltered")]
        [TestCase("no_cover")] [TestCase("moved")]
        public void SentinelDecisionsAreObservableWhenAiDiagnosticsAreEnabled(string decision)
        {
            CavesOfOoo.Diagnostics.Diag.ResetAll();
            CavesOfOoo.Diagnostics.Diag.SetChannel("ai", true);
            try
            {
                var zone = new Zone(); var sentinel = _factory.CreateEntity("BrocchiniaSentinel");
                sentinel.GetPart<BrainPart>().CurrentZone = zone; sentinel.GetPart<BrainPart>().Rng = new Random(4);
                zone.AddEntity(sentinel, 10, 10);
                if (decision != "no_threat") Actor(zone);
                if (decision != "no_cover") zone.AddEntity(_factory.CreateEntity("TankBrocchinia"), decision == "already_sheltered" ? 10 : 11, 10);
                sentinel.FireEventAndRelease(GameEvent.New("TakeTurn"));
                var records = CavesOfOoo.Diagnostics.DiagQuery.Apply(new CavesOfOoo.Diagnostics.DiagQuery.Filter
                    { Category = "ai", Kind = "BromeliadRetreat", Limit = 10 }).Records;
                Assert.AreEqual(1, records.Count);
                StringAssert.Contains("\"decision\":\"" + decision + "\"", records[0].PayloadJson);
            }
            finally { CavesOfOoo.Diagnostics.Diag.ResetAll(); }
        }

        [TestCase(false)] [TestCase(true)]
        public void RebuildingSavedCellIndexRestoresCurrentTagsAndClearsStaleOnes(bool remove)
        {
            var zone = new Zone(); var animal = _factory.CreateEntity("SummitSinger");
            if (remove) { zone.AddEntity(animal, 5, 5); zone.GetCell(5, 5).RemoveObject(animal); }
            else zone.GetCell(5, 5).AddObject(animal); // graph loader fills cells before entity bodies
            zone.RebuildEntityCellsFromCells();
            Assert.AreEqual(remove ? 0 : 1, zone.GetEntitiesWithTag("Creature").Count);
        }

        [Test]
        public void HabitatSelectionDoesNotFavorTheCellAfterALongInvalidGap()
        {
            int first = 0;
            var table = new PopulationTable();
            table.Entries.Add(new PopulationEntry { BlueprintName = "Wardline", MinCount = 1, MaxCount = 1 });
            for (int seed = 1; seed <= 120; seed++)
            {
                var zone = new Zone();
                new PopulationBuilder(table) { HabitatFilter = (bp, cell) => cell.X == 1 && (cell.Y == 1 || cell.Y == 3) }
                    .BuildZone(zone, _factory, new Random(seed));
                var placed = zone.GetEntitiesWithTag("Creature").Single();
                if (zone.GetEntityCell(placed).Y == 1) first++;
            }
            Assert.That(first, Is.InRange(30, 90), "two equally valid cells share the draw despite surrounding invalid ground");
        }

        [TestCase("SummitSinger")] [TestCase("BrocchiniaSentinel")] [TestCase("HelmwoodFrog")]
        public void MissingHabitatSuppressesOnlyThatAnimal(string animal)
        {
            CavesOfOoo.Diagnostics.Diag.ResetAll();
            var table = new PopulationTable();
            table.Entries.Add(new PopulationEntry { BlueprintName = animal, MinCount = 1, MaxCount = 1 });
            table.Entries.Add(new PopulationEntry { BlueprintName = "Wardline", MinCount = 1, MaxCount = 1 });
            var zone = new Zone();
            new PopulationBuilder(table) { HabitatFilter = StumpFaunaHabitat.Allows }.BuildZone(zone, _factory, new Random(1));
            var creatures = zone.GetEntitiesWithTag("Creature");
            Assert.AreEqual(1, creatures.Count); Assert.AreEqual("Wardline", creatures[0].BlueprintName);
            var records = CavesOfOoo.Diagnostics.DiagQuery.Apply(new CavesOfOoo.Diagnostics.DiagQuery.Filter
                { Category = "worldgen", Kind = "HabitatPopulationRejected", Limit = 10 }).Records;
            Assert.Greater(records.Count, 0, "fail-soft habitat refusals must be observable");
            Assert.IsTrue(records.All(r => r.PayloadJson.Contains(animal)));
            CavesOfOoo.Diagnostics.Diag.ResetAll();
        }

        [TestCase(false)] [TestCase(true)]
        public void DefendersActuallyActWhenThePlayerWaits(bool disturbed)
        {
            var zone = new Zone(); Nest(zone); var actor = Actor(zone);
            var turns = TurnManager.Active; turns.AddEntity(actor); turns.ProcessUntilPlayerTurn();
            if (disturbed) Enter(actor, zone);
            var initial = Defenders(zone).ToDictionary(e => e.ID, e => zone.GetEntityPosition(e));
            // New actors start at zero energy; player wins the first tie.
            for (int i = 0; i < 2; i++) { turns.EndTurn(actor); turns.ProcessUntilPlayerTurn(); }
            Assert.IsTrue(turns.WaitingForInput);
            if (disturbed)
            {
                Assert.AreEqual(16, initial.Count);
                Assert.IsTrue(Defenders(zone).Any(e => zone.GetEntityPosition(e) != initial[e.ID]), "registered defenders must receive real scheduled turns");
            }
            else Assert.AreEqual(0, initial.Count);
        }

        [TestCase(false)] [TestCase(true)]
        public void NestStateAndScheduledDefendersSurviveWholeGameSave(bool activated)
        {
            var zone = new Zone("Overworld.9.9.2"); var nest = Nest(zone); var actor = Actor(zone);
            var turns = TurnManager.Active; turns.AddEntity(actor);
            if (activated) Enter(actor, zone);
            var manager = new OverworldZoneManager(_factory, 5); manager.SetActiveZone(zone);
            var state = GameSessionState.Capture("w63b-test", "test", manager, turns, actor);
            GameSessionState loaded;
            using (var stream = new MemoryStream())
            {
                state.Save(new SaveWriter(stream)); stream.Position = 0;
                loaded = GameSessionState.Load(new SaveReader(stream, _factory));
            }
            var savedZone = loaded.ZoneManager.ActiveZone;
            var savedNest = savedZone.GetAllEntities().Single(e => e.BlueprintName == "PricklebrowNest");
            Assert.AreEqual(activated, savedNest.GetPart<PricklebrowNestPart>().Triggered);
            if (activated) Reenter(loaded.Player, savedZone); else Enter(loaded.Player, savedZone);
            Assert.AreEqual(16, Defenders(savedZone).Count);
            foreach (var d in Defenders(savedZone))
            {
                Assert.IsTrue(loaded.TurnManager.IsRegistered(d));
                Assert.IsTrue(d.GetPart<BrainPart>().IsPersonallyHostileTo(loaded.Player));
            }
        }

        [TestCase("Frog", "SummitSinger")] [TestCase("Avian", "SkySari")]
        public void NewAnatomiesKeepNativePartsAndNoWeaponHands(string anatomy, string blueprint)
        {
            var actor = _factory.CreateEntity(blueprint);
            Assert.AreEqual(anatomy, actor.GetProperty("Anatomy"));
            var body = actor.GetPart<Body>();
            Assert.IsFalse(body.GetParts().Any(p => p.Type == "Hand"));
            Assert.IsTrue(actor.HasTag("BodyNaturalAttack"));
        }

        [TestCase("actor")] [TestCase("zone")] [TestCase("manager")]
        public void PassageNullContextRefusesWithoutThrowing(string missing)
        {
            var zone = new Zone(); var actor = Actor(zone);
            var manager = new OverworldZoneManager(_factory, 1);
            ZoneTransitionResult result = default;
            Assert.DoesNotThrow(() => result = HelmwoodPassages.TryTravel(
                missing == "actor" ? null : actor, missing == "zone" ? null : zone,
                true, missing == "manager" ? null : manager));
            Assert.IsFalse(result.Success);
        }

        [TestCase(false)] [TestCase(true)]
        public void AscendingRepairsAnUnloadedSurfaceWithoutAnExplicitPreload(bool saveFirst)
        {
            var (manager, floor) = LivePassage();
            var marker = floor.GetAllEntities().Single(e => e.HasPart<WaterPassagePart>());
            var cell = floor.GetEntityCell(marker); var actor = Actor(floor, cell.X, cell.Y);
            string surfaceID = WorldMap.GetZoneAbove(WorldMap.GetZoneAbove(floor.ZoneID));
            manager.UnloadZone(surfaceID);
            if (saveFirst)
            {
                var loaded = RoundTrip(manager, floor, actor);
                manager = loaded.ZoneManager; floor = manager.ActiveZone; actor = loaded.Player;
            }
            var result = HelmwoodPassages.TryTravel(actor, floor, false, manager);
            Assert.IsTrue(result.Success, result.ErrorReason);
            Assert.AreEqual(surfaceID, result.NewZone.ZoneID);
            Assert.AreEqual(1, manager.GetConnections(surfaceID).Count(c => c.Type == "WaterPassage"));
            Assert.AreEqual(1, manager.GetConnections(floor.ZoneID).Count(c => c.Type == "WaterPassage"));
        }

        [TestCase(false)] [TestCase(true)]
        public void RemovedIndicatorDoesNotReappearWhenOtherEndpointRegenerates(bool saveFirst)
        {
            var (manager, floor) = LivePassage();
            var frog = floor.GetAllEntities().Single(e => e.BlueprintName == "HelmwoodFrog");
            floor.RemoveEntity(frog);
            var actor = Actor(floor, 40, 10);
            string surfaceID = WorldMap.GetZoneAbove(WorldMap.GetZoneAbove(floor.ZoneID));
            if (saveFirst) { var loaded = RoundTrip(manager, floor, actor); manager = loaded.ZoneManager; floor = manager.ActiveZone; }
            manager.UnloadZone(surfaceID); manager.GetZone(surfaceID);
            Assert.IsFalse(floor.GetAllEntities().Any(e => e.BlueprintName == "HelmwoodFrog"));
            Assert.IsTrue(floor.GetAllEntities().Single(e => e.HasPart<WaterPassagePart>()).GetPart<WaterPassagePart>().IndicatorSpawned);
        }

        [Test]
        public void IndicatorUsesOuterSpaceInsteadOfBlockingThePassageItExplains()
        {
            var (manager, floor) = LivePassage();
            foreach (var frog in floor.GetAllEntities().Where(e => e.BlueprintName == "HelmwoodFrog").ToList()) floor.RemoveEntity(frog);
            var marker = floor.GetAllEntities().Single(e => e.HasPart<WaterPassagePart>());
            marker.GetPart<WaterPassagePart>().IndicatorSpawned = false;
            var cell = floor.GetEntityCell(marker);
            for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
                if (dx != 0 || dy != 0) floor.GenReservedCells.Add((cell.X + dx, cell.Y + dy));
            // Explicit viable radius-two control, without erasing a real solid.
            Cell outer = null;
            for (int dx = -2; dx <= 2 && outer == null; dx++) for (int dy = -2; dy <= 2 && outer == null; dy++)
            {
                if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != 2) continue;
                var c = floor.GetCell(cell.X + dx, cell.Y + dy);
                if (c != null && !c.BlocksMovement() && !floor.GenReservedCells.Contains((c.X, c.Y))) outer = c;
            }
            Assert.IsNotNull(outer);
            HelmwoodPassages.OnZoneGenerated(floor, manager);
            var spawned = floor.GetAllEntities().Single(e => e.BlueprintName == "HelmwoodFrog");
            Assert.AreNotEqual((cell.X, cell.Y), floor.GetEntityPosition(spawned));
            Assert.IsFalse(cell.BlocksMovement());
        }

        [TestCase(false)] [TestCase(true)]
        public void RealInputPassageUpdatesActiveZoneScheduleAndOneTurnOnlyOnSuccess(bool blocked)
        {
            var (manager, floor) = LivePassage();
            var connection = manager.GetConnections(floor.ZoneID).Single(c => c.Type == "WaterPassage");
            var source = manager.GetZone(connection.SourceZoneID);
            // Keep the turn assertion isolated from hostile population, while
            // retaining the actual passage and input transition path.
            foreach (var creature in source.GetEntitiesWithTag("Creature").ToList()) source.RemoveEntity(creature);
            foreach (var creature in floor.GetEntitiesWithTag("Creature").ToList()) floor.RemoveEntity(creature);
            var actor = Actor(source, connection.SourceX, connection.SourceY);
            var turns = new TurnManager(); turns.AddEntity(actor); turns.ProcessUntilPlayerTurn();
            int before = turns.TickCount;
            manager.SetActiveZone(source);
            if (blocked) { var wall = new Entity(); wall.AddPart(new PhysicsPart { Solid = true }); floor.AddEntity(wall, connection.TargetX, connection.TargetY); }
            var go = new GameObject("water-passage-input-test");
            // No runtime capture callback: exercising a transition must not
            // write a user's save from a test fixture.
            SaveGameService.RegisterRuntime(null, null);
            try
            {
                var input = go.AddComponent<InputHandler>(); input.PlayerEntity = actor;
                input.CurrentZone = source; input.ZoneManager = manager; input.TurnManager = turns;
                typeof(InputHandler).GetMethod("TryUseStairs", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(input, new object[] { true });
                Assert.AreSame(blocked ? source : floor, input.CurrentZone);
                Assert.AreSame(input.CurrentZone, manager.ActiveZone);
                Assert.AreEqual(before + (blocked ? 0 : 10), turns.TickCount);
                Assert.IsTrue(turns.IsRegistered(actor)); Assert.IsTrue(turns.WaitingForInput);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); SettlementRuntime.ActiveZone = null; }
        }

        private (OverworldZoneManager, Zone) LivePassage()
        {
            var site = SinkholeSites.All.First(s => s.Name == "Ginmere");
            for (int seed = 1; seed <= 32; seed++)
            {
                var manager = new OverworldZoneManager(_factory, seed);
                var floor = manager.GetZone($"Overworld.{site.X}.{site.Y}.2");
                if (floor.GetAllEntities().Any(e => e.HasPart<WaterPassagePart>())) return (manager, floor);
            }
            Assert.Fail("expected an authored passage seed"); return default;
        }
        private GameSessionState RoundTrip(OverworldZoneManager manager, Zone zone, Entity actor)
        {
            var turns = new TurnManager(); turns.AddEntity(actor); manager.SetActiveZone(zone);
            var state = GameSessionState.Capture("water-adversarial", "test", manager, turns, actor);
            using (var stream = new MemoryStream())
            {
                state.Save(new SaveWriter(stream)); stream.Position = 0;
                return GameSessionState.Load(new SaveReader(stream, _factory));
            }
        }

        private Entity Nest(Zone zone, int x = 10, int y = 10)
        { var e = _factory.CreateEntity("PricklebrowNest"); zone.AddEntity(e, x, y); return e; }
        private static Entity Actor(Zone zone, int x = 9, int y = 10)
        {
            var e = new Entity { BlueprintName = "TestVisitor" }; e.Tags["Creature"] = ""; e.Tags["Player"] = "";
            e.AddPart(new PhysicsPart { Solid = true });
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 1000, Max = 1000 };
            zone.AddEntity(e, x, y); return e;
        }
        private static List<Entity> Defenders(Zone zone) => zone.GetEntitiesWithTag("Creature").Where(e => e.BlueprintName == "PrickleBrowGecko").ToList();
        private static void Enter(Entity actor, Zone zone) => Assert.IsTrue(MovementSystem.TryMove(actor, zone, 1, 0));
        private static void Reenter(Entity actor, Zone zone)
        { Assert.IsTrue(MovementSystem.TryMove(actor, zone, -1, 0)); Enter(actor, zone); }
    }
}
