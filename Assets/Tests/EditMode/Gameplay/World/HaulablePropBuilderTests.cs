using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using Application = UnityEngine.Application;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The placement pass that makes hauling reachable at all.
    ///
    /// <para>D1–D5 were correct and completely unmeetable: no worldgen path
    /// placed a single haulable object anywhere. These tests exist so that
    /// cannot silently become true again.</para>
    /// </summary>
    public class HaulablePropBuilderTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        /// <summary>An empty, fully open zone — nothing to block placement,
        /// so the only variable is the builder.</summary>
        private static Zone OpenZone()
        {
            var zone = new Zone("T");
            return zone;
        }

        private static int CountHaulables(Zone zone)
        {
            int n = 0;
            foreach (var e in zone.GetAllEntities())
            {
                var handling = e.GetPart<HandlingPart>();
                if (handling != null && !handling.Carryable) n++;
            }
            return n;
        }

        [Test]
        public void ItPlacesAHaulableWhenTheRollSucceeds()
        {
            var zone = OpenZone();
            var builder = new HaulablePropBuilder(BiomeType.Spread) { ChancePerMille = 1000 };

            builder.BuildZone(zone, _factory, new Random(1));

            Assert.AreEqual(1, CountHaulables(zone), "a guaranteed roll must place exactly one");
        }

        [Test]
        public void ItPlacesNothingWhenTheRollFails()
        {
            // Counter-check: without this, a builder that ignored its own
            // chance would pass the test above and litter every zone.
            var zone = OpenZone();
            var builder = new HaulablePropBuilder(BiomeType.Spread) { ChancePerMille = 0 };

            builder.BuildZone(zone, _factory, new Random(1));

            Assert.AreEqual(0, CountHaulables(zone));
        }

        [Test]
        public void WhateverItPlacesIsActuallyDraggable()
        {
            // The invariant that ties placement back to the mechanic: it is
            // no use scattering objects the drag rules refuse.
            var strongEnough = new Entity { ID = "hauler", BlueprintName = "Player" };
            strongEnough.Tags["Creature"] = "";
            strongEnough.Statistics["Strength"] = new Stat
            { Owner = strongEnough, Name = "Strength", BaseValue = 40, Min = 0, Max = 40 };

            foreach (BiomeType biome in new[]
            {
                BiomeType.Spread, BiomeType.Sodden, BiomeType.Beating,
                BiomeType.Grovelands, BiomeType.Overwrit, BiomeType.Stump,
                BiomeType.Cave, BiomeType.Desert, BiomeType.Jungle, BiomeType.Ruins,
            })
            {
                for (int seed = 1; seed <= 6; seed++)
                {
                    var zone = OpenZone();
                    new HaulablePropBuilder(biome) { ChancePerMille = 1000 }
                        .BuildZone(zone, _factory, new Random(seed));

                    foreach (var e in zone.GetAllEntities())
                    {
                        var handling = e.GetPart<HandlingPart>();
                        if (handling == null || handling.Carryable) continue;
                        Assert.AreEqual(DragVerdict.Ok, DragRules.CanDrag(strongEnough, e),
                            $"{biome}/{seed} placed {e.BlueprintName}, which cannot be hauled");
                    }
                }
            }
        }

        [Test]
        public void ItNeverPlugsAGap()
        {
            // A solid object dropped into a one-cell corridor can seal a
            // zone, and the only verb that would clear it is the drag this
            // object exists for — which the player may be too weak to use.
            var zone = OpenZone();
            for (int y = 0; y < Zone.Height; y++)
            {
                if (y == 12) continue;                  // the single gap
                var wall = _factory.CreateEntity("Wall");
                if (wall != null) zone.AddEntity(wall, 40, y);
            }

            for (int seed = 1; seed <= 25; seed++)
            {
                new HaulablePropBuilder(BiomeType.Spread) { ChancePerMille = 1000 }
                    .BuildZone(zone, _factory, new Random(seed));
            }

            var gap = zone.GetCell(40, 12);
            Assert.IsFalse(gap.BlocksMovement(), "the corridor's only gap was plugged");
        }

        [Test]
        public void NullsAreSafe()
        {
            var builder = new HaulablePropBuilder(BiomeType.Spread);
            Assert.DoesNotThrow(() => builder.BuildZone(null, _factory, new Random(1)));
            Assert.DoesNotThrow(() => builder.BuildZone(OpenZone(), null, new Random(1)));
            Assert.DoesNotThrow(() => builder.BuildZone(OpenZone(), _factory, null));
        }

        [Test]
        public void EveryBiomeHasAPool()
        {
            // The staleness guard. The builder tolerates a missing pool by
            // placing nothing, which is the right failure mode but a silent
            // one — a biome added later would simply never grow a haulable
            // and nobody would notice. This makes that loud.
            foreach (BiomeType biome in System.Enum.GetValues(typeof(BiomeType)))
            {
                var zone = OpenZone();
                new HaulablePropBuilder(biome) { ChancePerMille = 1000 }
                    .BuildZone(zone, _factory, new Random(3));
                Assert.AreEqual(1, CountHaulables(zone),
                    $"{biome} has no haulable pool — add one to HaulablePropBuilder.Pools");
            }
        }
    }
}
