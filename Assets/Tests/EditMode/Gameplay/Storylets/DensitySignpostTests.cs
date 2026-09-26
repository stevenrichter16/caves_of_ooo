using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class DensitySignpostTests
    {
        private EntityFactory factory;
        private OverworldZoneManager manager;
        private Zone zone;
        private Entity sign, player;
        private NarrativeStatePart previousNarrative;
        private StoryletPart previousStory;

        [SetUp]
        public void Setup()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
            manager = OverworldZoneManager.CreateDetached(factory, 64);
            zone = new Zone("Overworld.13.7.0");
            manager.SetActiveZone(zone);
            sign = factory.CreateEntity("Signpost");
            player = factory.CreateEntity("Player");
            Assert.IsTrue(zone.AddEntity(sign, 11, 10));
            Assert.IsTrue(zone.AddEntity(player, 10, 10));
            previousNarrative = NarrativeStatePart.Current;
            previousStory = StoryletPart.Current;
            NarrativeStatePart.Current = new NarrativeStatePart();
            StoryletPart.Current = new StoryletPart();
            MessageLog.Clear();
        }

        [TearDown]
        public void Cleanup()
        {
            NarrativeStatePart.Current = previousNarrative;
            StoryletPart.Current = previousStory;
            MessageLog.Clear();
        }

        private string Examine(Entity target = null)
        {
            MessageLog.Clear();
            var command = GameEvent.New("InventoryAction");
            command.SetParameter("Command", "Examine");
            command.SetParameter("Actor", (object)player);
            command.SetParameter("Zone", (object)zone);
            Assert.IsFalse((target ?? sign).FireEventAndRelease(command), "ordinary Examine handles its command");
            return MessageLog.GetLast();
        }

        private static byte[] SavedState(ISaveSerializable value)
        {
            using (var stream = new MemoryStream())
            {
                value.Save(new SaveWriter(stream));
                return stream.ToArray();
            }
        }

        private List<(int x, int y, string name)> Nearest(int ox, int oy)
        {
            var result = new List<(int x, int y, string name)>();
            for (int y = 0; y < WorldMap.Height; y++)
                for (int x = 0; x < WorldMap.Width; x++)
                {
                    var poi = manager.WorldMap.GetPOI(x, y);
                    if (poi?.Type == POIType.Village && !string.IsNullOrWhiteSpace(poi.Name)
                        && (x != ox || y != oy)) result.Add((x, y, poi.Name));
                }
            return result.OrderBy(p => Math.Abs(p.x - ox) + Math.Abs(p.y - oy))
                .ThenBy(p => p.y).ThenBy(p => p.x).Take(4).ToList();
        }

        private void UseCardinalDestinations()
        {
            for (int y = 0; y < WorldMap.Height; y++)
                for (int x = 0; x < WorldMap.Width; x++) manager.WorldMap.SetPOI(x, y, null);
            manager.WorldMap.SetPOI(13, 6, new PointOfInterest(POIType.Village, "Northtown"));
            manager.WorldMap.SetPOI(12, 7, new PointOfInterest(POIType.Village, "Westtown"));
            manager.WorldMap.SetPOI(14, 7, new PointOfInterest(POIType.Village, "Easttown"));
            manager.WorldMap.SetPOI(13, 8, new PointOfInterest(POIType.Village, "Southtown"));
            manager.WorldMap.SetPOI(14, 8, new PointOfInterest(POIType.Village, "FurtherTown"));
        }

        [Test]
        public void RealFactorySignpost_ExamineShowsFourNearestActualDestinations()
        {
            string text = Examine();
            StringAssert.Contains("Carved directions:", text);
            var nearest = Nearest(13, 7);
            Assert.AreEqual(4, nearest.Count);
            int prior = -1;
            foreach (var place in nearest)
            {
                string label = place.name + " (" + place.x + "," + place.y + ")";
                int index = text.IndexOf(label, StringComparison.Ordinal);
                Assert.Greater(index, prior, label + " appears in distance/Y/X order");
                prior = index;
            }
            Assert.IsTrue(sign.Parts.Any(p => p.Name == "RegionalSignpost"), "the factory wires the runtime behavior");
        }

        [Test]
        public void CardinalDirectionsAndEqualDistanceTies_AgreeWithMapAxes()
        {
            UseCardinalDestinations();
            string text = Examine();
            foreach (string direction in new[] { "1 north", "1 west", "1 east", "1 south" })
                StringAssert.Contains(direction, text);
            var names = new[] { "Northtown", "Westtown", "Easttown", "Southtown" };
            for (int i = 1; i < names.Length; i++)
                Assert.Less(text.IndexOf(names[i - 1], StringComparison.Ordinal), text.IndexOf(names[i], StringComparison.Ordinal));
            StringAssert.DoesNotContain("FurtherTown", text);
        }

        [Test]
        public void SignDoesNotAdvertiseCurrentCellNonVillageOrBlankDestination()
        {
            UseCardinalDestinations();
            manager.WorldMap.SetPOI(13, 7, new PointOfInterest(POIType.Village, "CurrentTown"));
            manager.WorldMap.SetPOI(13, 6, new PointOfInterest(POIType.Lair, "NearbyLair"));
            manager.WorldMap.SetPOI(12, 7, new PointOfInterest(POIType.Village, " "));
            string text = Examine();
            StringAssert.Contains("Easttown", text);
            StringAssert.DoesNotContain("CurrentTown", text);
            StringAssert.DoesNotContain("NearbyLair", text);
            StringAssert.DoesNotContain("(12,7)", text);
        }

        [Test]
        public void RemovingMapDestination_UpdatesNextExamineWithoutChangingStoredFlavor()
        {
            UseCardinalDestinations();
            string flavor = sign.GetPart<ExaminablePart>().Text;
            StringAssert.Contains("Northtown", Examine());
            manager.WorldMap.SetPOI(13, 6, null);
            string after = Examine();
            StringAssert.DoesNotContain("Northtown", after);
            StringAssert.Contains("FurtherTown", after);
            Assert.AreEqual(flavor, sign.GetPart<ExaminablePart>().Text);
        }

        [Test]
        public void KnownEmptyVillageIsOmitted_WhileUngeneratedTownStillHasDirections()
        {
            UseCardinalDestinations();
            StringAssert.Contains("Northtown", Examine());
            manager.CachedZones["Overworld.13.6.0"] = new Zone("Overworld.13.6.0");
            string text = Examine();
            StringAssert.DoesNotContain("Northtown", text);
            StringAssert.Contains("Westtown", text);
            StringAssert.Contains("FurtherTown", text);
        }

        [Test]
        public void DirectionsAreReadOnly_AndDoNotInheritConversationWorkPromises()
        {
            var scribe = factory.CreateEntity("Scribe");
            Assert.IsTrue(zone.AddEntity(scribe, 12, 10));
            var leads = RegionalGuidance.Build(zone, scribe, player);
            Assert.AreEqual(4, leads.Count, "existing supported conversations remain a matched positive control");
            Assert.IsTrue(leads.Any(l => !string.IsNullOrEmpty(l.QuestID)), "there is real available work to suppress");
            int cached = manager.CachedZoneCount;
            var visited = (bool[,])manager.WorldMap.Visited.Clone();
            var pois = (PointOfInterest[,])manager.WorldMap.POIs.Clone();
            var properties = player.Properties.ToDictionary(p => p.Key, p => p.Value);
            byte[] narrative = SavedState(NarrativeStatePart.Current);
            byte[] story = SavedState(StoryletPart.Current);
            string first = Examine();
            StringAssert.Contains("Carved directions:", first);
            StringAssert.DoesNotContain("Local lead:", first);
            Assert.AreEqual(first, Examine());
            Assert.AreEqual(cached, manager.CachedZoneCount);
            CollectionAssert.AreEqual(visited, manager.WorldMap.Visited);
            CollectionAssert.AreEqual(pois, manager.WorldMap.POIs);
            CollectionAssert.AreEquivalent(properties, player.Properties);
            CollectionAssert.AreEqual(narrative, SavedState(NarrativeStatePart.Current));
            CollectionAssert.AreEqual(story, SavedState(StoryletPart.Current));
            Assert.IsEmpty(RegionalTravelNotes.Read(player));
        }

        [TestCase("Shrine")] [TestCase("Bookshelf")]
        public void OrdinaryFixturesKeepTheirOwnExamineText(string blueprint)
        {
            var fixture = factory.CreateEntity(blueprint);
            Assert.IsTrue(zone.AddEntity(fixture, 15, 10));
            string text = Examine(fixture);
            StringAssert.Contains(fixture.GetPart<ExaminablePart>().Text, text);
            StringAssert.DoesNotContain("Carved directions:", text);
            Assert.IsFalse(fixture.Parts.Any(p => p.Name == "RegionalSignpost"));
        }

        [TestCase("detached")] [TestCase("underground")] [TestCase("invalid")]
        [TestCase("unbound")] [TestCase("stale")]
        public void InvalidLocationRetainsFlavorWithoutInventingDirections(string context)
        {
            if (context == "stale") manager.SetActiveZone(new Zone(zone.ZoneID));
            else
            {
                zone.RemoveEntity(sign);
                if (context != "detached")
                {
                    var destination = new Zone(context == "underground" ? "Overworld.13.7.1"
                        : context == "invalid" ? "not-a-world-zone" : "Overworld.13.7.0");
                    if (context != "unbound") manager.SetActiveZone(destination);
                    Assert.IsTrue(destination.AddEntity(sign, 11, 10));
                }
            }
            string text = Examine();
            StringAssert.Contains(sign.GetPart<ExaminablePart>().Text, text);
            StringAssert.DoesNotContain("Carved directions:", text);
        }

        [Test]
        public void MovedSignUsesActualNewGraph_NotStaleExamineEventContext()
        {
            StringAssert.Contains("from (13,7)", Examine());
            var destination = new Zone("Overworld.4.10.0");
            manager.SetActiveZone(destination);
            Assert.IsTrue(zone.RemoveEntity(sign));
            Assert.IsTrue(destination.AddEntity(sign, 11, 10));
            // Examine still passes the original zone; entity ownership is authoritative.
            string text = Examine();
            StringAssert.Contains("from (4,10)", text);
            StringAssert.DoesNotContain("from (13,7)", text);
            foreach (var place in Nearest(4, 10)) StringAssert.Contains(place.name, text);
        }

        [Test]
        public void NewSavedSignRecomputesDirectionsAfterNormalContextReattachment()
        {
            string before = Examine();
            StringAssert.Contains("Carved directions:", before);
            Entity loaded;
            using (var stream = new MemoryStream())
            {
                var writer = new SaveWriter(stream);
                writer.WriteEntityReference(sign); writer.WriteQueuedEntityBodies();
                stream.Position = 0;
                var reader = new SaveReader(stream, factory);
                loaded = reader.ReadEntityReference(); reader.ReadEntityBodies();
            }
            StringAssert.DoesNotContain("Carved directions:", Examine(loaded));
            Assert.IsTrue(zone.RemoveEntity(sign));
            Assert.IsTrue(zone.AddEntity(loaded, 11, 10));
            Assert.AreEqual(before, Examine(loaded));
        }

        [Test]
        public void OldSignWithoutRuntimePartRemainsStatic_WhileNewSignGivesDirections()
        {
            var old = factory.CreateEntity("Signpost");
            var behavior = old.Parts.FirstOrDefault(p => p.Name == "RegionalSignpost");
            if (behavior != null) Assert.IsTrue(old.RemovePart(behavior));
            old.GetPart<ExaminablePart>().Text = "The old lettering is gone.";
            Assert.IsTrue(zone.AddEntity(old, 15, 10));
            StringAssert.DoesNotContain("Carved directions:", Examine(old));
            StringAssert.Contains("The old lettering is gone.", Examine(old));
            StringAssert.Contains("Carved directions:", Examine());
        }

        [Test]
        public void NoVillageDestinationsProducesFlavorWithoutAnEmptyDirectionsHeading()
        {
            UseCardinalDestinations();
            for (int y = 0; y < WorldMap.Height; y++)
                for (int x = 0; x < WorldMap.Width; x++) manager.WorldMap.SetPOI(x, y, null);
            string text = Examine();
            StringAssert.Contains(sign.GetPart<ExaminablePart>().Text, text);
            StringAssert.DoesNotContain("Carved directions:", text);
        }
    }
}
