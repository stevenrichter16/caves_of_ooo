using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    public class SpellTargetingTests
    {
        [Test]
        public void TraceBeam_PassesThroughCreatures_AndStopsAtWall()
        {
            var zone = new Zone("BeamTraceZone");
            var caster = CreateCreature("caster");
            var firstTarget = CreateCreature("first");
            var secondTarget = CreateCreature("second");
            var wall = CreateWall();

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(firstTarget, 7, 5);
            zone.AddEntity(secondTarget, 9, 5);
            zone.AddEntity(wall, 10, 5);

            BeamTraceResult trace = SpellTargeting.TraceBeam(zone, caster, 5, 5, 1, 0, 7);

            Assert.AreEqual(5, trace.Path.Count);
            CollectionAssert.AreEqual(new List<Entity> { firstTarget, secondTarget }, trace.HitEntities);
            Assert.IsTrue(trace.BlockedBySolid);
            Assert.AreEqual(10, trace.ImpactCell.X);
            Assert.AreEqual(5, trace.ImpactCell.Y);
        }

        [Test]
        public void GetCreaturesInRadius_UsesChebyshevRange_AndExcludesRequestedEntity()
        {
            var zone = new Zone("RadiusZone");
            var caster = CreateCreature("caster");
            var adjacent = CreateCreature("adjacent");
            var diagonal = CreateCreature("diagonal");
            var edge = CreateCreature("edge");
            var outside = CreateCreature("outside");

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(adjacent, 5, 4);
            zone.AddEntity(diagonal, 6, 6);
            zone.AddEntity(edge, 7, 5);
            zone.AddEntity(outside, 8, 5);

            List<Entity> targets = SpellTargeting.GetCreaturesInRadius(zone, 5, 5, 2, caster);

            CollectionAssert.AreEquivalent(new[] { adjacent, diagonal, edge }, targets);
            CollectionAssert.DoesNotContain(targets, outside);
        }

        [Test]
        public void FindChainTargets_PicksNearestUniqueTargets_InStableOrder()
        {
            var zone = new Zone("ChainZone");
            var caster = CreateCreature("caster");
            var primary = CreateCreature("primary");
            var first = CreateCreature("first");
            var second = CreateCreature("second");
            var tiedButLater = CreateCreature("later");

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(primary, 7, 5);
            zone.AddEntity(first, 8, 4);
            zone.AddEntity(tiedButLater, 8, 6);
            zone.AddEntity(second, 10, 4);

            List<Entity> chain = SpellTargeting.FindChainTargets(zone, caster, primary, maxJumps: 2, searchRadius: 3);

            Assert.AreEqual(2, chain.Count);
            Assert.AreEqual(first, chain[0]);
            Assert.AreEqual(second, chain[1]);
            CollectionAssert.DoesNotContain(chain, caster);
            CollectionAssert.DoesNotContain(chain, primary);
        }

        private static Entity CreateCreature(string name)
        {
            var entity = new Entity { BlueprintName = name };
            entity.Tags["Creature"] = "";
            entity.AddPart(new RenderPart { DisplayName = name });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            return entity;
        }

        private static Entity CreateWall()
        {
            var entity = new Entity { BlueprintName = "Wall" };
            entity.Tags["Solid"] = "";
            entity.Tags["Wall"] = "";
            entity.AddPart(new RenderPart { DisplayName = "wall" });
            entity.AddPart(new PhysicsPart { Solid = true });
            return entity;
        }
    }
}
