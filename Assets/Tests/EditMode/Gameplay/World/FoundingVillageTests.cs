using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FoundingVillageTests
    {
        private EntityFactory _factory;
        [OneTimeSetUp] public void Load()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [Test]
        public void FreshAndRehydratedOlderdeepCarryTheAuthoredProfile()
        {
            var map = WorldGenerator.Generate(64);
            Assert.AreEqual("FoundingVillage", map.GetPOI(4, 6).Profile);
            map.GetPOI(4, 6).Profile = null;
            map.RehydrateAuthoredProfiles(); map.RehydrateAuthoredSinkholes();
            Assert.AreEqual("FoundingVillage", map.GetPOI(4, 6).Profile);
            Assert.AreNotEqual("FoundingVillage", map.GetPOI(12, 3).Profile);
            map.SetPOI(4, 6, new PointOfInterest(POIType.Sinkhole, "Changed by a story", null, 3));
            map.RehydrateAuthoredProfiles(); map.RehydrateAuthoredSinkholes();
            Assert.AreNotEqual("FoundingVillage", map.GetPOI(4, 6).Profile, "authored repair must not replace a different POI");
        }

        [TestCase(false)] [TestCase(true)]
        public void TheActualFoundingFloorHasOneRootedAndItsWalkableFoundingPlume(bool floorFirst)
        {
            var manager = new OverworldZoneManager(_factory, 64);
            if (!floorFirst) { manager.GetZone("Overworld.4.6.0"); manager.GetZone("Overworld.4.6.1"); }
            var floor = manager.GetZone("Overworld.4.6.2"); Assert.IsNotNull(floor);
            var rooted = floor.GetAllEntities().Where(e => e.BlueprintName == "TheRooted").ToList();
            Assert.AreEqual(1, rooted.Count);
            var plume = floor.GetAllEntities().Where(e => e.BlueprintName == "FoundingPlume").ToList();
            Assert.That(plume.Count, Is.InRange(6, 20));
            foreach (var e in plume)
            {
                Assert.IsNotNull(e.GetPart<HearthPatchPart>());
                Assert.IsNotNull(e.GetPart("FoundingPlume"));
                Assert.IsFalse(e.HasTag("Solid")); Assert.IsFalse(e.GetPart<PhysicsPart>().Solid);
                Assert.IsFalse(floor.GetEntityCell(e).Objects.Any(o => o.HasPart<StairsUpPart>() || o.HasPart<StairsDownPart>()));
            }
            Assert.AreEqual(0, floor.GetAllEntities().Count(e => e.BlueprintName == "HearthPatch"), "the torso plume is the founding hearth");
            Assert.AreEqual(1, floor.GetAllEntities().Count(e => e.BlueprintName == "FoundingListener"));
            Assert.AreEqual(1, floor.GetAllEntities().Count(e => e.BlueprintName == "FoundingPlaqueTender"));
            // Accessing a cached floor must preserve player changes.
            floor.RemoveEntity(rooted[0]);
            Assert.AreSame(floor, manager.GetZone(floor.ZoneID));
            Assert.IsFalse(floor.GetAllEntities().Any(e => e.BlueprintName == "TheRooted"));
        }

        [TestCase("Overworld.12.3.2")] [TestCase("Overworld.16.4.2")]
        public void OrdinaryCatacombVillagesKeepTheirOwnHearthAndPeople(string id)
        {
            var manager = new OverworldZoneManager(_factory, 64); var zone = manager.GetZone(id);
            Assert.IsTrue(zone.GetAllEntities().Any(e => e.BlueprintName == "HearthPatch"));
            Assert.IsFalse(zone.GetAllEntities().Any(e => e.BlueprintName == "TheRooted" || e.BlueprintName == "FoundingPlume"));
        }

        [TestCase("TheRooted", "the_rooted")]
        [TestCase("FoundingPlume", "founding_plume")]
        [TestCase("FoundingListener", "founding_listener")]
        [TestCase("FoundingPlaqueTender", "founding_plaque_tender")]
        public void EveryNewFoundingObjectHasItsOwnImportedSprite(string blueprint, string file)
        {
            Assert.IsTrue(_factory.Blueprints.ContainsKey(blueprint));
            Assert.IsNotNull(Resources.Load<Sprite>("Sprites/Environment/" + file));
            Assert.IsTrue(EnvironmentSpriteRenderer.FixtureSprites.Any(r => r.Blueprint == blueprint && r.File == file)
                || EnvironmentSpriteRenderer.NamedActorSprites.Any(r => r.Blueprint == blueprint && r.File == file));
            if (blueprint == "FoundingPlume") Assert.AreEqual(4, EnvironmentSpriteRenderer.FixtureVariantCounts[blueprint]);
        }
        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void RepeatedNicheHomesHaveImportedVisualVariants(int variant)
        {
            Assert.IsNotNull(Resources.Load<Sprite>("Sprites/Environment/niche_home_v" + variant));
            Assert.AreEqual(4, EnvironmentSpriteRenderer.FixtureVariantCounts["NicheHome"]);
        }
    }
}
