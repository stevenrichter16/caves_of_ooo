using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>W6.4 taxonomy gate: authored geometry on hostile starting
    /// terrain, stair collisions, cached saves, command bypass, clock edges,
    /// cross-source war, and dangling dialogue registrations.</summary>
    public sealed class FoundingVillageAdversarialTests
    {
        private EntityFactory _factory;
        private NarrativeStatePart _oldState;
        [OneTimeSetUp] public void Load()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void SetUp()
        {
            FactionManager.Initialize(); PlayerReputation.Set("CatacombFolk", 50);
            _oldState = NarrativeStatePart.Current; NarrativeStatePart.Current = new NarrativeStatePart();
        }
        [TearDown] public void TearDown()
        {
            NarrativeStatePart.Current = _oldState; FactionManager.Reset(); ConversationLoader.Reset();
        }
        private Zone Terrain(bool solid)
        {
            var zone = new Zone("Overworld.4.6.2");
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++)
            {
                var e = new Entity { BlueprintName = solid ? "TestRock" : "StoneFloor" };
                if (solid) e.AddPart(new PhysicsPart { Solid = true });
                zone.AddEntity(e, x, y);
            }
            return zone;
        }
        private void CheckGap(Zone zone)
        {
            var body = zone.GetAllEntities().Single(e => e.BlueprintName == "TheRooted");
            var p = zone.GetEntityPosition(body);
            Assert.IsTrue(zone.GetCell(65, p.y).BlocksMovement(), "explicit east root-wall face");
            for (int x = p.x + 1; x < 65; x++) for (int y = p.y - 1; y <= p.y + 1; y++)
            {
                Assert.IsFalse(zone.GetCell(x, y).BlocksMovement(), "sacred gap is physically open");
                Assert.IsTrue(zone.GetCell(x, y).Objects.All(e => e.BlueprintName == "StoneFloor"), "no stamped plume/stairs/loot/population in gap");
                Assert.IsTrue(zone.GenReservedCells.Contains((x, y)));
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_OpenOrSolidBaseCannotSupplyAnAccidentalEastWall(bool solid)
        {
            var zone = Terrain(solid);
            Assert.IsTrue(new FoundingVillageBuilder().BuildZone(zone, _factory, new System.Random(4)));
            CheckGap(zone);
            foreach (var niche in zone.GetAllEntities().Where(e => e.BlueprintName == "NicheHome"))
            {
                var p = zone.GetEntityPosition(niche);
                Assert.IsTrue(new[] { (0, -1), (0, 1), (-1, 0), (1, 0) }.Any(d =>
                    zone.GetCell(p.x + d.Item1, p.y + d.Item2)?.Objects.Any(e => e.HasTag("Wall")) == true), "niches are carved into an actual wall");
            }
        }
        [TestCase(65, 12)] [TestCase(58, 12)] [TestCase(66, 14)] [TestCase(72, 8)]
        public void Adversarial_ForcedStairsKeepTheirIdentityAndReachableApproach(int x, int y)
        {
            var zone = Terrain(true);
            foreach (var e in zone.GetCell(x, y).Objects.ToArray()) zone.RemoveEntity(e);
            var stair = new Entity(); stair.AddPart(new StairsUpPart()); zone.AddEntity(stair, x, y);
            Assert.IsTrue(new FoundingVillageBuilder().BuildZone(zone, _factory, new System.Random(5)));
            Assert.AreEqual((x, y), zone.GetEntityPosition(stair)); CheckGap(zone);
            var reached = Flood(zone, (x, y));
            Assert.IsTrue(zone.GetAllEntities().Where(e => e.BlueprintName == "FoundingPlume").Any(e => reached.Contains(zone.GetEntityPosition(e))));
            foreach (var npc in zone.GetAllEntities().Where(e => e.BlueprintName == "FoundingListener" || e.BlueprintName == "FoundingPlaqueTender"))
            {
                var p = zone.GetEntityPosition(npc);
                Assert.IsTrue(reached.Any(c => System.Math.Max(System.Math.Abs(c.x - p.x), System.Math.Abs(c.y - p.y)) <= 1), "NPC approachable");
            }
        }
        [TestCase(2, false)] [TestCase(2, true)] [TestCase(38, false)] [TestCase(38, true)] [TestCase(97, false)] [TestCase(97, true)]
        public void Adversarial_ActualPipelineKeepsGapAndConnectedPlume(int seed, bool surfaceFirst)
        {
            var manager = new OverworldZoneManager(_factory, seed);
            if (surfaceFirst) manager.GetZone("Overworld.4.6.0");
            var zone = manager.GetZone("Overworld.4.6.2"); CheckGap(zone);
            foreach (var stair in zone.GetAllEntities().Where(e => e.HasPart<StairsUpPart>() || e.HasPart<StairsDownPart>()))
            {
                var reached = Flood(zone, zone.GetEntityPosition(stair));
                Assert.IsTrue(zone.GetAllEntities().Where(e => e.BlueprintName == "FoundingPlume").Any(e => reached.Contains(zone.GetEntityPosition(e))));
            }
        }
        [TestCase("TheRooted")] [TestCase("FoundingPlume")] [TestCase("FoundingListener")]
        public void Adversarial_MissingDependencyRefusesBeforeClearingAnything(string missing)
        {
            var factory = new EntityFactory(); factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            factory.Blueprints.Remove(missing);
            var zone = Terrain(false); var before = zone.GetAllEntities().ToArray();
            Assert.IsFalse(new FoundingVillageBuilder().BuildZone(zone, factory, new System.Random(1)));
            CollectionAssert.AreEquivalent(before, zone.GetAllEntities());
        }
        [Test] public void Adversarial_RebuildIsIdempotent()
        {
            var zone = Terrain(false); var builder = new FoundingVillageBuilder(); builder.BuildZone(zone, _factory, new System.Random(3));
            var before = zone.GetAllEntities().ToArray(); builder.BuildZone(zone, _factory, new System.Random(4));
            CollectionAssert.AreEquivalent(before, zone.GetAllEntities());
        }
        [Test] public void Adversarial_TwoStairsBesideTheSacredStripDoNotRejectEveryAnchor()
        {
            var zone = Terrain(false); var stairs = new List<Entity>();
            foreach (int y in new[] { 9, 15 })
            { var e = new Entity(); e.AddPart(new StairsUpPart()); zone.AddEntity(e, 60, y); stairs.Add(e); }
            Assert.IsTrue(new FoundingVillageBuilder().BuildZone(zone, _factory, new System.Random(1)));
            CheckGap(zone);
            Assert.AreEqual((60, 9), zone.GetEntityPosition(stairs[0]));
            Assert.AreEqual((60, 15), zone.GetEntityPosition(stairs[1]));
        }
        [TestCase("Ginmere")] [TestCase("the Deepest Cathedral")]
        public void Adversarial_FoundingProfileControlsTheFloorIndependentlyOfDisplayName(string name)
        {
            var manager = new OverworldZoneManager(_factory, 64);
            manager.WorldMap.GetPOI(4, 6).Name = name;
            Assert.AreEqual("FoundingVillage", manager.WorldMap.GetPOI(4, 6).Profile);
            var floor = manager.GetZone("Overworld.4.6.2");
            Assert.AreEqual(1, floor.GetAllEntities().Count(e => e.BlueprintName == "TheRooted"));
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_SaveBeforeOrAfterFloorAuthoringPreservesPlayerChanges(bool authored)
        {
            var manager = new OverworldZoneManager(_factory, 64);
            var zone = manager.GetZone(authored ? "Overworld.4.6.2" : "Overworld.4.6.0");
            if (authored) zone.RemoveEntity(zone.GetAllEntities().First(e => e.BlueprintName == "FoundingPlume"));
            int count = zone.GetAllEntities().Count(e => e.BlueprintName == "FoundingPlume");
            var actor = Actor(zone); var turns = TurnManager.Active; manager.SetActiveZone(zone);
            var world = new Entity(); world.AddPart(NarrativeStatePart.Current);
            NarrativeStatePart.Current.SetFact("RootedMet", 1); NarrativeStatePart.Current.SetFact("FoundingStoneOffered", 1);
            actor.SetIntProperty("FoundingBloomUntilTick", 9000);
            var state = GameSessionState.Capture("founding-gate", "test", manager, turns, actor, world: world);
            using (var stream = new MemoryStream())
            {
                state.Save(new SaveWriter(stream)); stream.Position = 0;
                var loaded = GameSessionState.Load(new SaveReader(stream, _factory));
                Assert.AreEqual("FoundingVillage", loaded.ZoneManager.WorldMap.GetPOI(4, 6).Profile);
                var floor = loaded.ZoneManager.GetZone("Overworld.4.6.2");
                Assert.AreEqual(1, floor.GetAllEntities().Count(e => e.BlueprintName == "TheRooted"));
                Assert.AreEqual(authored ? count : 11, floor.GetAllEntities().Count(e => e.BlueprintName == "FoundingPlume"));
                Assert.AreEqual(1, loaded.World.GetPart<NarrativeStatePart>().GetFact("RootedMet"));
                Assert.AreEqual(1, loaded.World.GetPart<NarrativeStatePart>().GetFact("FoundingStoneOffered"));
                Assert.AreEqual(9000, loaded.Player.GetIntProperty("FoundingBloomUntilTick"));
            }
        }
        [TestCase("TheRooted", true)] [TestCase("TheRooted", false)] [TestCase("FoundingPlume", true)] [TestCase("FoundingPlume", false)]
        public void Adversarial_RealDamageRouteUsesSharedWarLedgerOnlyForPlayer(string bp, bool player)
        {
            var zone = new Zone(); var source = Actor(zone); if (!player) source.Tags.Remove("Player");
            var target = _factory.CreateEntity(bp); zone.AddEntity(target, 11, 10);
            DestructionSystem.RouteDamage(target, new Damage(1), source, zone);
            Assert.AreEqual(player ? -150 : 50, PlayerReputation.Get("CatacombFolk"));
            if (bp == "TheRooted") Assert.IsNotNull(zone.GetEntityCell(target));
        }
        [TestCase("actor")] [TestCase("zone")] [TestCase("state")] [TestCase("clock")] [TestCase("dead")]
        public void Adversarial_MissingContextCannotCauseFreeRestOrResurrection(string missing)
        {
            var zone = new Zone(); var actor = Actor(zone); var turns = TurnManager.Active;
            var plume = _factory.CreateEntity("FoundingPlume"); zone.AddEntity(plume, 10, 10);
            if (missing == "state") NarrativeStatePart.Current = null;
            if (missing == "clock") new TurnManager();
            if (missing == "dead") actor.GetStat("Hitpoints").BaseValue = 0;
            int hp = actor.GetStatValue("Hitpoints"), tick = turns.TickCount;
            var e = GameEvent.New("InventoryAction"); e.SetParameter("Command", "SleepOnFoundingPlume");
            if (missing != "actor") e.SetParameter("Actor", actor);
            if (missing != "zone") e.SetParameter("Zone", zone);
            plume.FireEventAndRelease(e);
            Assert.AreEqual(hp, actor.GetStatValue("Hitpoints")); Assert.AreEqual(tick, turns.TickCount);
        }
        [TestCase(16799, true)] [TestCase(16800, false)] [TestCase(16801, false)]
        public void Adversarial_PatchBloomRecognitionExpiresAfterTwoWorldWeeks(int elapsed, bool scented)
        {
            Assert.IsTrue(ConversationPredicates.IsRegistered("IfFoundingBloom"));
            var zone = new Zone(); var actor = Actor(zone); var plume = _factory.CreateEntity("FoundingPlume"); zone.AddEntity(plume, 10, 10);
            var e = GameEvent.New("InventoryAction"); e.SetParameter("Command", "SleepOnFoundingPlume"); e.SetParameter("Actor", actor); e.SetParameter("Zone", zone); plume.FireEventAndRelease(e);
            TurnManager.Active.AdvanceClock(elapsed);
            Assert.AreEqual(scented, ConversationPredicates.Evaluate("IfFoundingBloom", null, actor, ""));
        }
        [Test] public void Adversarial_DialogueNamesAreRegisteredAndDoNotLeakTheMortalName()
        {
            foreach (var id in new[] { "FoundingListener_1", "FoundingPlaqueTender_1" })
            {
                var data = ConversationLoader.Get(id); Assert.IsNotNull(data);
                foreach (var node in data.Nodes)
                {
                    StringAssert.DoesNotContain("Dohren", node.Text);
                    foreach (var c in node.Choices)
                    {
                        if (c.Predicates != null) foreach (var p in c.Predicates) Assert.IsTrue(ConversationPredicates.IsRegistered(p.Key), p.Key);
                        if (c.Actions != null) foreach (var a in c.Actions) Assert.IsTrue(ConversationActions.IsRegistered(a.Key), a.Key);
                        Assert.IsTrue(c.Target == "End" || data.GetNode(c.Target) != null);
                    }
                }
            }
        }
        private static Entity Actor(Zone zone)
        {
            var actor = new Entity(); actor.SetTag("Player");
            actor.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 5, Max = 40 };
            actor.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Max = 200 };
            zone.AddEntity(actor, 10, 10); var turns = new TurnManager(); turns.AddEntity(actor); turns.ProcessUntilPlayerTurn(); return actor;
        }
        private static HashSet<(int x, int y)> Flood(Zone zone, (int x, int y) start)
        {
            var seen = new HashSet<(int x, int y)> { start }; var queue = new Queue<(int x, int y)>(); queue.Enqueue(start);
            while (queue.Count != 0)
            {
                var p = queue.Dequeue();
                foreach (var d in new[] { (0, -1), (0, 1), (-1, 0), (1, 0) })
                {
                    var n = (p.x + d.Item1, p.y + d.Item2); var cell = zone.GetCell(n.Item1, n.Item2);
                    if (cell != null && !cell.BlocksMovement() && seen.Add(n)) queue.Enqueue(n);
                }
            }
            return seen;
        }
    }
}
