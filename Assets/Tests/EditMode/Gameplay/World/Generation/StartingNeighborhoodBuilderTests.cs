using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    public class StartingNeighborhoodBuilderTests
    {
        private EntityFactory _factory;
        private StartingNeighborhoodBuilder _builder;

        [SetUp]
        public void SetUp()
        {
            _factory = new EntityFactory();
            string blueprintPath = Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json");
            _factory.LoadBlueprints(File.ReadAllText(blueprintPath));
            _builder = new StartingNeighborhoodBuilder();
        }

        [Test]
        public void BuildZone_NonTargetZone_IsNoOp()
        {
            var zone = new Zone("Overworld.8.8.0");

            bool result = _builder.BuildZone(zone, _factory, new Random(42));

            Assert.IsTrue(result);
            Assert.AreEqual(0, zone.EntityCount);
        }

        [Test]
        public void BuildZone_RuinsOfSparkwright_PlacesSetPieceAndThemedContent()
        {
            var zone = new Zone("Overworld.10.9.0");

            bool result = _builder.BuildZone(zone, _factory, new Random(42));

            Assert.IsTrue(result);
            AssertHasAnyBlueprint(zone, "BrokenCapacitor", "WaterPuddle", "WoodenBarrel");
            AssertHasAnyBlueprint(zone, "BrassHusk");
            AssertHasAnyBlueprint(zone, "ChainMail", "OldWorldPipe");
        }

        [Test]
        public void BuildZone_SaltglassDunes_PlacesSetPieceAndThemedContent()
        {
            var zone = new Zone("Overworld.11.10.0");

            bool result = _builder.BuildZone(zone, _factory, new Random(42));

            Assert.IsTrue(result);
            AssertHasAnyBlueprint(zone, "OilSeep", "RawMeat", "Starapple");
            AssertHasAnyBlueprint(zone, "GlassScorpion");
            AssertHasAnyBlueprint(zone, "GlassblownStiletto", "LanternOil");
        }

        [Test]
        public void BuildZone_VerdantRotbog_PlacesSetPieceAndThemedContent()
        {
            var zone = new Zone("Overworld.10.11.0");

            bool result = _builder.BuildZone(zone, _factory, new Random(42));

            Assert.IsTrue(result);
            AssertHasAnyBlueprint(zone, "AcidPond", "SporeShambler");
            AssertHasAnyBlueprint(zone, "SporeShambler");
            AssertHasAnyBlueprint(zone, "Sporeblade", "FirstRootGlaive");
        }

        [Test]
        public void BuildZone_FrostfangGrotto_PlacesSetPieceAndThemedContent()
        {
            var zone = new Zone("Overworld.9.10.0");

            bool result = _builder.BuildZone(zone, _factory, new Random(42));

            Assert.IsTrue(result);
            AssertHasAnyBlueprint(zone, "IceStalactite", "IceWight");
            AssertHasAnyBlueprint(zone, "IceWight");
            AssertHasAnyBlueprint(zone, "TemporalShard", "EchoKnife");
        }

        private static void AssertHasAnyBlueprint(Zone zone, params string[] blueprintNames)
        {
            Assert.IsTrue(ZoneHasAnyBlueprint(zone, blueprintNames),
                "Expected zone to contain one of: " + string.Join(", ", blueprintNames));
        }

        private static bool ZoneHasAnyBlueprint(Zone zone, IReadOnlyList<string> blueprintNames)
        {
            List<Entity> entities = zone.GetAllEntities();
            for (int i = 0; i < entities.Count; i++)
            {
                string blueprintName = entities[i].BlueprintName;
                for (int j = 0; j < blueprintNames.Count; j++)
                {
                    if (blueprintName == blueprintNames[j])
                        return true;
                }
            }

            return false;
        }
    }
}
