using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using UnityEngine;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    public class StumpSummitEcologyTests
    {
        private EntityFactory _factory;
        [OneTimeSetUp] public void Load()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup() { FactionManager.Initialize(); NarrativeStatePart.Current = null; }
        [TearDown] public void Cleanup() { NarrativeStatePart.Current = null; }

        [TestCase(StumpBand.Summit)]
        [TestCase(StumpBand.Slopes)]
        public void EaglesOnlyRollWhileUrquIsManifest(StumpBand band)
        {
            var table = PopulationTable.GetStumpTable(band, 3);
            Assert.IsTrue(table.Entries.Exists(e => e.BlueprintName == "SkySari"));
            var state = new NarrativeStatePart();
            NarrativeStatePart.Current = state;
            for (int seed = 0; seed < 60; seed++) Assert.IsFalse(table.Roll(new Random(seed)).Contains("SkySari"));
            state.SetFact("UrquActive", 1);
            Assert.IsTrue(Enumerable.Range(0, 100).Any(s => table.Roll(new Random(s)).Contains("SkySari")));
            state.SetFact("UrquActive", 0);
            Assert.IsFalse(table.Roll(new Random(5)).Contains("SkySari"));
        }

        [Test]
        public void SummitEndemicsDoNotLeakDownhill_AndSingerIsNotDespawnedForSilence()
        {
            foreach (var name in new[] { "SummitSinger", "BrocchiniaSentinel" })
            {
                var rows = PopulationTable.GetStumpTable(StumpBand.Summit, 2).Entries;
                Assert.IsTrue(rows.Exists(e => e.BlueprintName == name));
                Assert.IsFalse(PopulationTable.GetStumpTable(StumpBand.Foothills, 3).Entries.Exists(e => e.BlueprintName == name));
                Assert.IsFalse(PopulationTable.GetStumpTable(StumpBand.Slopes, 3).Entries.Exists(e => e.BlueprintName == name));
            }
            var singer = PopulationTable.GetStumpTable(StumpBand.Summit, 2).Entries.Find(e => e.BlueprintName == "SummitSinger");
            Assert.IsTrue(string.IsNullOrEmpty(singer.RequiresWorldFlag));
            Assert.IsTrue(string.IsNullOrEmpty(singer.ForbidsWorldFlag));
        }

        [Test]
        public void HelmwoodHasALivingPopulationInGeneratedSurfaceForest()
        {
            int frogs = 0;
            for (int seed = 1; seed <= 8; seed++)
            {
                var manager = new OverworldZoneManager(_factory, seed);
                for (int x = 0; x <= 5; x++)
                {
                    if (manager.WorldMap.GetPOI(x, 0) != null) continue;
                    var zone = manager.GetZone($"Overworld.{x}.0.0");
                    foreach (var frog in zone.GetEntitiesWithTag("Creature").Where(e => e.BlueprintName == "HelmwoodFrog"))
                    {
                        frogs++;
                        var cell = zone.GetEntityCell(frog);
                        Assert.IsTrue(Near(zone, cell.X, cell.Y, "Tree", 2));
                    }
                }
            }
            Assert.Greater(frogs, 0, "table rows alone do not prove a spawn is reachable");
            foreach (var band in new[] { StumpBand.Foothills, StumpBand.Slopes })
                Assert.IsFalse(PopulationTable.GetStumpTable(band, 2).Entries.Exists(e => e.BlueprintName == "HelmwoodFrog"), "bare bands have no forest habitat");
        }

        [Test]
        public void RealSummitZonesPutEndemicsBesideTheirHabitat()
        {
            int singers = 0, sentinels = 0;
            for (int seed = 1; seed <= 4; seed++)
            {
                var manager = new OverworldZoneManager(_factory, seed);
                for (int x = 1; x <= 5; x++) for (int y = 1; y <= 5; y++)
                {
                    if (StumpBands.BandAt(x, y) != StumpBand.Summit) continue;
                    var zone = manager.GetZone($"Overworld.{x}.{y}.0");
                    foreach (var animal in zone.GetEntitiesWithTag("Creature"))
                    {
                        var cell = zone.GetEntityCell(animal);
                        if (animal.BlueprintName == "SummitSinger")
                        {
                            singers++;
                            Assert.IsTrue(Near(zone, cell.X, cell.Y, "Tree", 2));
                        }
                        if (animal.BlueprintName == "BrocchiniaSentinel")
                        {
                            sentinels++;
                            Assert.IsTrue(Near(zone, cell.X, cell.Y, "TankBrocchinia", 2));
                        }
                    }
                }
            }
            Assert.Greater(singers, 0); Assert.Greater(sentinels, 0);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SentinelRetreatsOneCellIntoCoverAndStaysThere(bool friendlyVisitor)
        {
            var zone = new Zone();
            var animal = _factory.CreateEntity("BrocchiniaSentinel");
            var brain = animal.GetPart<BrainPart>(); brain.CurrentZone = zone; brain.Rng = new Random(3);
            zone.AddEntity(animal, 10, 10);
            var visitor = Actor();
            if (friendlyVisitor) visitor.Tags["Faction"] = "CatacombFolk";
            zone.AddEntity(visitor, 9, 10);
            zone.AddEntity(_factory.CreateEntity("TankBrocchinia"), 11, 10);
            animal.FireEventAndRelease(GameEvent.New("TakeTurn"));
            Assert.AreEqual((11, 10), zone.GetEntityPosition(animal));
            animal.FireEventAndRelease(GameEvent.New("TakeTurn"));
            Assert.AreEqual((11, 10), zone.GetEntityPosition(animal));
            Assert.AreEqual(1000, visitor.GetStatValue("Hitpoints"), "approach is not an attack");
        }

        [Test]
        public void QuietSentinelRemainsCryptic()
        {
            var zone = new Zone(); var animal = _factory.CreateEntity("BrocchiniaSentinel");
            animal.GetPart<BrainPart>().CurrentZone = zone; animal.GetPart<BrainPart>().Rng = new Random(3);
            zone.AddEntity(animal, 10, 10);
            for (int i = 0; i < 12; i++) animal.FireEventAndRelease(GameEvent.New("TakeTurn"));
            Assert.AreEqual((10, 10), zone.GetEntityPosition(animal));
        }

        [Test]
        public void DisturbingNestReleasesSixteenDistinctScheduledDefenders_OnlyOnce()
        {
            var zone = new Zone(); var turns = new TurnManager();
            var nest = Nest(zone, 10, 10); var actor = Actor(); zone.AddEntity(actor, 9, 10);
            Assert.IsTrue(MovementSystem.TryMove(actor, zone, 1, 0));
            var defenders = zone.GetEntitiesWithTag("Creature").Where(e => e.BlueprintName == "PrickleBrowGecko").ToList();
            Assert.AreEqual(16, defenders.Count);
            Assert.AreEqual(16, defenders.Select(e => zone.GetEntityPosition(e)).Distinct().Count());
            foreach (var defender in defenders)
            {
                Assert.IsTrue(turns.IsRegistered(defender));
                Assert.IsTrue(defender.GetPart<BrainPart>().IsPersonallyHostileTo(actor));
                Assert.AreSame(zone, defender.GetPart<BrainPart>().CurrentZone);
            }
            Assert.IsTrue(MovementSystem.TryMove(actor, zone, -1, 0));
            Assert.IsTrue(MovementSystem.TryMove(actor, zone, 1, 0));
            Assert.AreEqual(16, zone.GetEntitiesWithTag("Creature").Count(e => e.BlueprintName == "PrickleBrowGecko"));
            Assert.IsNotNull(zone.GetEntityCell(nest));
        }

        [Test]
        public void NestIgnoresItsOwnFaunaAndNonlivingMovers()
        {
            foreach (bool living in new[] { true, false })
            {
                var zone = new Zone(); new TurnManager(); Nest(zone, 10, 10);
                var actor = living ? _factory.CreateEntity("PrickleBrowGecko") : new Entity();
                zone.AddEntity(actor, 9, 10);
                Assert.IsTrue(MovementSystem.TryMove(actor, zone, 1, 0));
                Assert.AreEqual(living ? 1 : 0, zone.GetEntitiesWithTag("Creature").Count);
            }
        }

        [Test]
        public void GinmereHasNestAndAmbientGeckos_ButSurfaceAndDescentDoNot()
        {
            var manager = new OverworldZoneManager(_factory, 3);
            var site = SinkholeSites.All.First(s => s.Name == "Ginmere");
            for (int z = 0; z <= 2; z++)
            {
                var zone = manager.GetZone($"Overworld.{site.X}.{site.Y}.{z}");
                bool nest = zone.GetAllEntities().Any(e => e.BlueprintName == "PricklebrowNest");
                bool gecko = zone.GetAllEntities().Any(e => e.BlueprintName == "PrickleBrowGecko");
                Assert.AreEqual(z == 2, nest); Assert.AreEqual(z == 2, gecko);
            }
        }

        private Entity Nest(Zone zone, int x, int y)
        {
            Assert.IsTrue(_factory.Blueprints.ContainsKey("PricklebrowNest"), "communal nests must ship");
            var nest = _factory.CreateEntity("PricklebrowNest");
            var part = nest.GetPart("PricklebrowNest");
            Assert.IsNotNull(part);
            part.GetType().GetField("Factory").SetValue(null, _factory);
            zone.AddEntity(nest, x, y); return nest;
        }
        private static Entity Actor()
        {
            var e = new Entity(); e.Tags["Creature"] = ""; e.Tags["Player"] = "";
            e.AddPart(new PhysicsPart { Solid = true });
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 1000, Max = 1000 };
            return e;
        }
        private static bool Near(Zone zone, int x, int y, string name, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++) for (int dy = -radius; dy <= radius; dy++)
            {
                var c = zone.GetCell(x + dx, y + dy);
                if (c != null && c.Objects.Any(e => e.BlueprintName == name)) return true;
            }
            return false;
        }
    }
}
