using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    public class LineTargetingTests
    {
        [Test]
        public void TraceFirstImpact_StopsAtFirstCreature()
        {
            var zone = new Zone("TraceZone");
            var caster = CreateCreature("caster");
            var target = CreateCreature("target");

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 8, 5);

            LineTraceResult trace = LineTargeting.TraceFirstImpact(zone, caster, 5, 5, 1, 0, 6);

            Assert.AreEqual(3, trace.Path.Count);
            Assert.AreEqual(target, trace.HitEntity);
            Assert.AreEqual(8, trace.ImpactCell.X);
            Assert.AreEqual(5, trace.ImpactCell.Y);
            Assert.IsFalse(trace.BlockedBySolid);
        }

        [Test]
        public void TraceFirstImpact_StopsAtFirstSolidCell_WhenNoCreatureIsHit()
        {
            var zone = new Zone("TraceZone");
            var caster = CreateCreature("caster");
            var wall = CreateWall();

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(wall, 7, 5);

            LineTraceResult trace = LineTargeting.TraceFirstImpact(zone, caster, 5, 5, 1, 0, 6);

            Assert.AreEqual(2, trace.Path.Count);
            Assert.IsNull(trace.HitEntity);
            Assert.IsTrue(trace.BlockedBySolid);
            Assert.AreEqual(7, trace.ImpactCell.X);
            Assert.AreEqual(5, trace.ImpactCell.Y);
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
