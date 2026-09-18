using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Native wildlife remains alive, sparse and save-backed. These
    /// tests precede the population extension to the previously empty scene.</summary>
    public sealed class FellingScenePopulationTests
    {
        private EntityFactory factory;
        [SetUp] public void Setup()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
        }
        private Zone Fresh() => new OverworldZoneManager(factory, 64).GetZone(FellingSiteBuilder.ZoneID);
        private static Entity[] Fauna(Zone zone) => zone.GetAllEntities()
            .Where(e => e.HasTag(FellingScenePopulation.FaunaTag)).ToArray();
        private static HashSet<(int x, int y)> Reachable(Zone zone)
        {
            var result = new HashSet<(int, int)> { (40, 20) };
            var queue = new Queue<(int x, int y)>(); queue.Enqueue((40, 20));
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                foreach (var d in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    var n = (p.x + d.Item1, p.y + d.Item2);
                    if (zone.GetCell(n.Item1, n.Item2)?.BlocksMovement() == false && result.Add(n))
                        queue.Enqueue(n);
                }
            }
            return result;
        }
        private static void RemovePopulationAndResetRevision(Zone zone)
        {
            foreach (var animal in Fauna(zone)) zone.RemoveEntity(animal);
            FellingSceneRuntime.GetState(zone).PopulationRevision = 0;
        }
        [Test] public void FreshSceneContainsThreeNativePassiveAnimalsWithRealActions()
        {
            var zone = Fresh(); var animals = Fauna(zone);
            Assert.AreEqual(3, animals.Length);
            CollectionAssert.AreEquivalent(new[] { "GlasspaneFrog", "GlasspaneFrog", "YellowfootWayfarer" }, animals.Select(e => e.BlueprintName));
            Assert.AreEqual(FellingScenePopulation.CurrentRevision, FellingSceneRuntime.GetState(zone).PopulationRevision);
            foreach (var animal in animals)
            {
                Assert.IsTrue(animal.HasTag("Creature")); Assert.Greater(animal.GetStatValue("Hitpoints"), 0);
                Assert.IsTrue(animal.GetPart<BrainPart>().Passive);
                Assert.IsTrue(animal.GetPart<BrainPart>().WandersRandomly);
                Assert.IsTrue(animal.GetPart<PhysicsPart>().Solid);
                Assert.IsTrue(WorldInteractionSystem.GatherActions(animal).Any(a => a.Command == "Examine"));
                Assert.IsFalse(animal.HasPart<FellingScenePropPart>(), "Living creatures must not be clearable decorations.");
                Assert.IsTrue(StumpFaunaHabitat.Allows(animal.BlueprintName, zone.GetEntityCell(animal)));
            }
        }
        [Test] public void SparseHabitatPlacementsKeepEntranceLorePositionsAndEveryPropApproachOpen()
        {
            var zone = Fresh(); var reachable = Reachable(zone); var definition = FellingSceneDefinition.Load();
            Assert.AreEqual(3, Fauna(zone).Length);
            foreach (var animal in Fauna(zone))
            {
                var p = zone.GetEntityPosition(animal);
                Assert.IsFalse(p.x >= 30 && p.x <= 50 && p.y >= 8 && p.y <= 24, "Keep the ritual clearing and south approach empty.");
                Assert.IsFalse(definition.cells.Single(c => c.x == p.x && c.y == p.y).water);
                Assert.IsTrue(definition.layers.All(l => Math.Abs(l.anchorX - p.x) + Math.Abs(l.anchorY - p.y) > 1));
            }
            Assert.IsTrue(reachable.Contains((40, 24)));
            for (int y = 0; y < Zone.Height; y++)
                for (int x = 0; x < Zone.Width; x++)
                    if ((x == 0 || x == Zone.Width - 1 || y == 0 || y == Zone.Height - 1)
                        && !zone.GetCell(x, y).BlocksMovement())
                        Assert.IsTrue(reachable.Contains((x, y)), "Wildlife must not cut off open zone exits: " + x + "," + y);
            foreach (var landmark in definition.landmarks) Assert.IsTrue(reachable.Contains((landmark.x, landmark.y)));
            foreach (var layer in definition.layers.Where(l => l.mutable))
                Assert.IsTrue(new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }
                    .Any(d => reachable.Contains((layer.anchorX + d.Item1, layer.anchorY + d.Item2))), layer.id);
        }
        [Test] public void OrdinaryDamageKillsNativeWildlifeAndReentryDoesNotRespawnIt()
        {
            var zone = Fresh(); var animal = Fauna(zone).First(e => e.BlueprintName == "GlasspaneFrog");
            CombatSystem.ApplyDamage(animal, 100, null, zone);
            Assert.IsTrue(CombatSystem.IsDeathHandled(animal)); Assert.IsNull(zone.GetEntityCell(animal));
            Assert.IsTrue(FellingSceneRuntime.UpgradeCachedZone(zone, factory));
            Assert.AreEqual(2, Fauna(zone).Length); Assert.IsFalse(Fauna(zone).Any(e => e.ID == animal.ID));
        }
        [Test] public void ExistingPopulationRevisionDoesNotRestoreRemovedAnimals()
        {
            var zone = Fresh(); foreach (var animal in Fauna(zone)) zone.RemoveEntity(animal);
            Assert.IsTrue(FellingScenePopulation.EnsurePopulation(zone, factory));
            Assert.AreEqual(0, Fauna(zone).Length);
        }
        [Test] public void OlderScenePopulatesOnceWithoutChangingExistingOccupants()
        {
            var zone = Fresh(); RemovePopulationAndResetRevision(zone);
            var item = factory.CreateEntity("Tepuibone"); zone.AddEntity(item, 40, 20);
            Assert.IsTrue(FellingSceneRuntime.UpgradeCachedZone(zone, factory));
            var ids = Fauna(zone).Select(e => e.ID).OrderBy(id => id).ToArray(); Assert.AreEqual(3, ids.Length);
            Assert.IsTrue(FellingSceneRuntime.UpgradeCachedZone(zone, factory));
            CollectionAssert.AreEqual(ids, Fauna(zone).Select(e => e.ID).OrderBy(id => id));
            Assert.AreEqual((40, 20), zone.GetEntityPosition(item));
        }
        [TestCase("GlasspaneFrog")] [TestCase("YellowfootWayfarer")]
        public void MissingPopulationBlueprintDoesNotPartiallySpawnOrConsumeRevision(string missing)
        {
            var zone = Fresh(); RemovePopulationAndResetRevision(zone); factory.Blueprints.Remove(missing);
            var before = zone.GetAllEntities().Select(e => e.ID).OrderBy(id => id).ToArray();
            Assert.IsFalse(FellingScenePopulation.EnsurePopulation(zone, factory));
            Assert.AreEqual(0, FellingSceneRuntime.GetState(zone).PopulationRevision);
            CollectionAssert.AreEqual(before, zone.GetAllEntities().Select(e => e.ID).OrderBy(id => id));
        }
        [Test] public void UnknownSavedBlockerIsPreservedAndAnimalsUseOtherSafeHabitatCells()
        {
            var zone = Fresh(); RemovePopulationAndResetRevision(zone);
            var blocker = factory.CreateEntity("Rock"); zone.AddEntity(blocker, 22, 2);
            Assert.IsTrue(FellingScenePopulation.EnsurePopulation(zone, factory));
            Assert.AreEqual((22, 2), zone.GetEntityPosition(blocker)); Assert.AreEqual(3, Fauna(zone).Length);
            Assert.IsFalse(Fauna(zone).Any(e => zone.GetEntityPosition(e) == (22, 2)));
            Assert.IsTrue(Reachable(zone).Contains((0, 0)), "Fallback animals must not occupy the upper river crossing.");
        }
        [Test] public void NonFellingAndNullArgumentsDoNotInventPopulation()
        {
            var other = new Zone("Overworld.2.5.0");
            Assert.IsFalse(FellingScenePopulation.EnsurePopulation(other, factory)); Assert.AreEqual(0, other.EntityCount);
            Assert.IsFalse(FellingScenePopulation.EnsurePopulation(null, factory));
            Assert.IsFalse(FellingScenePopulation.EnsurePopulation(Fresh(), null));
        }
        [TestCase(true)] [TestCase(false)]
        public void OlderSavedSceneRegistersNewAnimalsOnlyWhenThatZoneIsActive(bool active)
        {
            var manager = new OverworldZoneManager(factory, 64); var site = manager.GetZone(FellingSiteBuilder.ZoneID);
            RemovePopulationAndResetRevision(site);
            var zone = active ? site : manager.GetZone(WorldMap.WorldMapZoneID);
            var player = factory.CreateEntity("Player"); zone.AddEntity(player, 40, 20); manager.SetActiveZone(zone);
            var turns = new TurnManager(); turns.AddEntity(player);
            var state = GameSessionState.Capture("felling-fauna", "test", manager, turns, player);
            GameSessionState loaded;
            using (var stream = new MemoryStream())
            { state.Save(new SaveWriter(stream)); stream.Position = 0; loaded = GameSessionState.Load(new SaveReader(stream, factory)); }
            var loadedSite = loaded.ZoneManager.CachedZones[FellingSiteBuilder.ZoneID]; Assert.AreEqual(3, Fauna(loadedSite).Length);
            foreach (var animal in Fauna(loadedSite)) Assert.AreEqual(active, loaded.TurnManager.IsRegistered(animal));
        }
        [Test] public void KilledFaunaRemainAbsentAcrossRealSaveReloadAndRepeatedAccess()
        {
            var manager = new OverworldZoneManager(factory, 64); var site = manager.GetZone(FellingSiteBuilder.ZoneID);
            var animal = Fauna(site).First(); string deadId = animal.ID; CombatSystem.ApplyDamage(animal, 100, null, site);
            var player = factory.CreateEntity("Player"); site.AddEntity(player, 40, 20); manager.SetActiveZone(site);
            var turns = new TurnManager(); turns.AddEntity(player);
            var state = GameSessionState.Capture("felling-fauna", "test", manager, turns, player); GameSessionState loaded;
            using (var stream = new MemoryStream())
            { state.Save(new SaveWriter(stream)); stream.Position = 0; loaded = GameSessionState.Load(new SaveReader(stream, factory)); }
            var loadedSite = loaded.ZoneManager.GetZone(FellingSiteBuilder.ZoneID);
            Assert.AreEqual(2, Fauna(loadedSite).Length); Assert.IsFalse(Fauna(loadedSite).Any(e => e.ID == deadId));
            Assert.AreEqual(FellingScenePopulation.CurrentRevision, FellingSceneRuntime.GetState(loadedSite).PopulationRevision);
        }
        [Test] public void ThreeSupplementalPropsRetainNativeHarvestAndPickupWithoutReplacingSourceOwners()
        {
            var zone = Fresh(); Assert.AreEqual(3, FellingScenePopulation.DressingSpecs.Length);
            Assert.AreEqual(55, zone.GetAllEntities().Count(e => e.HasPart<FellingScenePropPart>()));
            foreach (var spec in FellingScenePopulation.DressingSpecs)
            {
                var owner = FellingScenePopulation.FindDressingOwner(zone, spec.id); Assert.IsNotNull(owner, spec.id);
                Assert.AreEqual(spec.blueprint, owner.BlueprintName); Assert.IsTrue(owner.HasTag(FellingScenePopulation.DressingTag));
                Assert.IsTrue(WorldInteractionSystem.GatherActions(owner).Any(a => a.Command == "Examine"));
                Assert.IsFalse(owner.HasPart<FellingScenePropPart>());
                Assert.IsFalse(zone.GetEntityCell(owner).BlocksMovement());
                Assert.IsTrue(spec.blueprint == "MushroomRing" ? owner.HasPart<HarvestablePart>() : owner.GetPart<PhysicsPart>().Takeable);
            }
        }
        [Test] public void NativeHarvestProducesMushroomsAndRemovedDressingDoesNotRespawn()
        {
            var oldFactory = HarvestablePart.Factory; HarvestablePart.Factory = factory;
            try
            {
                var zone = Fresh(); var spec = FellingScenePopulation.DressingSpecs.First(s => s.blueprint == "MushroomRing");
                var owner = FellingScenePopulation.FindDressingOwner(zone, spec.id); var player = factory.CreateEntity("Player");
                zone.AddEntity(player, spec.x, spec.y);
                var action = GameEvent.New("InventoryAction"); action.SetParameter("Command", "Harvest");
                action.SetParameter("Actor", (object)player); action.SetParameter("Zone", (object)zone);
                action.SetParameter("Random", (object)new System.Random(71)); owner.FireEventAndRelease(action);
                Assert.IsNull(zone.GetEntityCell(owner)); Assert.IsNull(FellingScenePopulation.FindDressingOwner(zone, spec.id));
                Assert.IsTrue(player.GetPart<InventoryPart>().Objects.Any(e => e.BlueprintName == "Mushroom"));
                Assert.IsTrue(FellingSceneRuntime.UpgradeCachedZone(zone, factory));
                Assert.IsNull(FellingScenePopulation.FindDressingOwner(zone, spec.id));
            }
            finally { HarvestablePart.Factory = oldFactory; }
        }
        [Test] public void NativeLooseStonePickupAndDropMoveTheSameDressingOwner()
        {
            var zone = Fresh(); var spec = FellingScenePopulation.DressingSpecs.Single(s => s.blueprint == "Tepuibone");
            var owner = FellingScenePopulation.FindDressingOwner(zone, spec.id); var player = factory.CreateEntity("Player");
            zone.AddEntity(player, spec.x, spec.y); Assert.IsTrue(InventorySystem.Pickup(player, owner, zone));
            Assert.IsNull(FellingScenePopulation.FindDressingOwner(zone, spec.id));
            zone.MoveEntity(player, 40, 20); Assert.IsTrue(InventorySystem.Drop(player, owner, zone));
            Assert.AreSame(owner, FellingScenePopulation.FindDressingOwner(zone, spec.id));
            Assert.AreEqual((40, 20), zone.GetEntityPosition(owner));
        }
        [Test] public void TakenAndHarvestedSupplementalPropsRemainAbsentAfterSaveReload()
        {
            var manager = new OverworldZoneManager(factory, 64); var zone = manager.GetZone(FellingSiteBuilder.ZoneID);
            foreach (var spec in FellingScenePopulation.DressingSpecs) zone.RemoveEntity(FellingScenePopulation.FindDressingOwner(zone, spec.id));
            var player = factory.CreateEntity("Player"); zone.AddEntity(player, 40, 20); manager.SetActiveZone(zone);
            var turns = new TurnManager(); turns.AddEntity(player);
            var state = GameSessionState.Capture("felling-dressing", "test", manager, turns, player); GameSessionState loaded;
            using (var stream = new MemoryStream())
            { state.Save(new SaveWriter(stream)); stream.Position = 0; loaded = GameSessionState.Load(new SaveReader(stream, factory)); }
            var loadedZone = loaded.ZoneManager.GetZone(FellingSiteBuilder.ZoneID);
            foreach (var spec in FellingScenePopulation.DressingSpecs) Assert.IsNull(FellingScenePopulation.FindDressingOwner(loadedZone, spec.id));
            Assert.AreEqual(FellingScenePopulation.CurrentRevision, FellingSceneRuntime.GetState(loadedZone).DressingRevision);
        }
    }
}
