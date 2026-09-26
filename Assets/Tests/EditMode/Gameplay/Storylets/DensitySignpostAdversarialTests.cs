using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Location-parser edges, cached graph authority and instance
    /// isolation. These pin already-correct post-implementation behavior; they
    /// are not claimed as new RED-to-GREEN gameplay discoveries.</summary>
    public class DensitySignpostAdversarialTests
    {
        private EntityFactory factory;
        private OverworldZoneManager manager;
        private Zone zone;
        private Entity sign;
        private Zone previousActive;

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
            Assert.IsTrue(zone.AddEntity(sign, 10, 10));
            previousActive = SettlementRuntime.ActiveZone;
            MessageLog.Clear();
        }
        [TearDown] public void Cleanup() { SettlementRuntime.ActiveZone = previousActive; MessageLog.Clear(); }

        private static string Examine(Entity target)
        {
            var action = GameEvent.New("InventoryAction");
            action.SetParameter("Command", "Examine");
            target.FireEventAndRelease(action);
            return MessageLog.GetLast();
        }

        private void MoveTo(string id)
        {
            Assert.IsTrue(zone.RemoveEntity(sign));
            zone = new Zone(id); manager.SetActiveZone(zone);
            Assert.IsTrue(zone.AddEntity(sign, 10, 10));
        }

        [TestCase("Overworld.-1.7.0")] [TestCase("Overworld.20.7.0")]
        [TestCase("Overworld.13.-1.0")] [TestCase("Overworld.13.20.0")]
        [TestCase("Overworld.x.7.0")] [TestCase("Overworld.13.y.0")]
        [TestCase("Overworld.13.7.z")] [TestCase("Overworld.13.7.0.extra")]
        [TestCase("Overworld.13.7.-1")] [TestCase("WorldMap")]
        [TestCase("overworld.13.7.0")]
        public void InvalidOrNonSurfaceOriginNeverFormatsFabricatedBearings(string id)
        {
            StringAssert.Contains("Carved directions:", Examine(sign));
            MoveTo(id);
            StringAssert.DoesNotContain("Carved directions:", Examine(sign));
        }

        [TestCase(0, 0)] [TestCase(0, 19)] [TestCase(19, 0)] [TestCase(19, 19)]
        public void MapCornersRemainValidAndUseTheActualOrigin(int x, int y)
        {
            MoveTo(WorldMap.ToZoneID(x, y, 0));
            string text = Examine(sign);
            StringAssert.Contains("from (" + x + "," + y + ")", text);
            Assert.AreEqual(4, text.Split(new[] { "world-map cells." }, StringSplitOptions.None).Length - 1);
            if (x == 0) StringAssert.DoesNotContain(" west", text);
            if (x == 19) StringAssert.DoesNotContain(" east", text);
            if (y == 0) StringAssert.DoesNotContain(" north", text);
            if (y == 19) StringAssert.DoesNotContain(" south", text);
        }

        [Test]
        public void LegacySurfaceAddressRetainsItsExistingParserSupport()
        {
            string current = Examine(sign);
            MoveTo("Overworld.13.7");
            Assert.AreEqual(current, Examine(sign));
        }

        [Test]
        public void SameAddressInAnotherWorldDoesNotStealTheSignsMapAuthority()
        {
            var other = OverworldZoneManager.CreateDetached(factory, 64);
            var otherZone = new Zone(zone.ZoneID); other.SetActiveZone(otherZone);
            for (int y = 0; y < WorldMap.Height; y++) for (int x = 0; x < WorldMap.Width; x++)
            {
                var first = manager.WorldMap.GetPOI(x, y);
                var second = other.WorldMap.GetPOI(x, y);
                if (first?.Type == POIType.Village) first.Name = "FirstWorld " + first.Name;
                if (second?.Type == POIType.Village) second.Name = "OtherWorld " + second.Name;
            }
            var otherSign = factory.CreateEntity("Signpost");
            Assert.IsTrue(otherZone.AddEntity(otherSign, 10, 10));
            SettlementRuntime.ActiveZone = otherZone;
            StringAssert.Contains("FirstWorld", Examine(sign));
            StringAssert.DoesNotContain("OtherWorld", Examine(sign));
            SettlementRuntime.ActiveZone = zone;
            StringAssert.Contains("OtherWorld", Examine(otherSign));
            StringAssert.DoesNotContain("FirstWorld", Examine(otherSign));
        }

        [Test]
        public void DuplicatePlaceNamesKeepDistinctCoordinatesAndDoNotCollapseDestinations()
        {
            for (int y = 0; y < WorldMap.Height; y++) for (int x = 0; x < WorldMap.Width; x++)
                if (manager.WorldMap.GetPOI(x, y)?.Type == POIType.Village)
                    manager.WorldMap.GetPOI(x, y).Name = "Repeated";
            var lines = Examine(sign).Split('\n').Where(line => line.Contains("Repeated (")).ToArray();
            Assert.AreEqual(4, lines.Length);
            Assert.AreEqual(4, lines.Distinct().Count());
        }

        [Test]
        public void ReattachingOriginalCachedGraphRestoresItsDirections()
        {
            string before = Examine(sign);
            StringAssert.Contains("Carved directions:", before);
            manager.SetActiveZone(new Zone(zone.ZoneID));
            StringAssert.DoesNotContain("Carved directions:", Examine(sign));
            manager.SetActiveZone(zone);
            Assert.AreEqual(before, Examine(sign));
        }

        [Test]
        public void RemovingBehaviorFromOneSignDoesNotMutateItsBlueprintOrSibling()
        {
            var behavior = sign.Parts.Single(p => p.Name == "RegionalSignpost");
            Assert.IsTrue(sign.RemovePart(behavior));
            StringAssert.DoesNotContain("Carved directions:", Examine(sign));
            var sibling = factory.CreateEntity("Signpost");
            Assert.IsTrue(zone.AddEntity(sibling, 11, 10));
            StringAssert.Contains("Carved directions:", Examine(sibling));
            Assert.IsTrue(factory.Blueprints["Signpost"].Parts.ContainsKey("RegionalSignpost"));
        }
    }
}
