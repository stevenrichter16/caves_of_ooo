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

        // ========================
        // TraceFirstImpactToTarget
        // ========================

        [Test]
        public void TraceFirstImpactToTarget_HitsCreatureAtTarget()
        {
            var zone = new Zone("TraceZone");
            var caster = CreateCreature("caster");
            var target = CreateCreature("target");

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 8, 5);

            LineTraceResult trace = LineTargeting.TraceFirstImpactToTarget(zone, caster, 5, 5, 8, 5, 6);

            Assert.AreEqual(3, trace.Path.Count);
            Assert.AreEqual(target, trace.HitEntity);
            Assert.AreEqual(8, trace.ImpactCell.X);
            Assert.AreEqual(5, trace.ImpactCell.Y);
            Assert.IsFalse(trace.BlockedBySolid);
        }

        [Test]
        public void TraceFirstImpactToTarget_HitsCreatureBeforeTarget()
        {
            var zone = new Zone("TraceZone");
            var caster = CreateCreature("caster");
            var blocker = CreateCreature("blocker");

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(blocker, 7, 5);

            LineTraceResult trace = LineTargeting.TraceFirstImpactToTarget(zone, caster, 5, 5, 9, 5, 6);

            Assert.AreEqual(blocker, trace.HitEntity);
            Assert.AreEqual(7, trace.ImpactCell.X);
            Assert.AreEqual(5, trace.ImpactCell.Y);
        }

        [Test]
        public void TraceFirstImpactToTarget_StopsAtSolid_LandsInLastTraversableCell()
        {
            var zone = new Zone("TraceZone");
            var caster = CreateCreature("caster");
            var wall = CreateWall();

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(wall, 7, 5);

            LineTraceResult trace = LineTargeting.TraceFirstImpactToTarget(zone, caster, 5, 5, 9, 5, 6);

            Assert.IsNull(trace.HitEntity);
            Assert.IsTrue(trace.BlockedBySolid);
            Assert.AreEqual(7, trace.ImpactCell.X);
            Assert.IsNotNull(trace.LastTraversableCell);
            Assert.AreEqual(6, trace.LastTraversableCell.X);
        }

        [Test]
        public void TraceFirstImpactToTarget_SameCellTarget_ReturnsImpactCellWithEmptyPath()
        {
            var zone = new Zone("TraceZone");
            var caster = CreateCreature("caster");

            zone.AddEntity(caster, 5, 5);

            LineTraceResult trace = LineTargeting.TraceFirstImpactToTarget(zone, caster, 5, 5, 5, 5, 6);

            Assert.AreEqual(0, trace.Path.Count);
            Assert.IsNotNull(trace.ImpactCell);
            Assert.AreEqual(5, trace.ImpactCell.X);
            Assert.AreEqual(5, trace.ImpactCell.Y);
            Assert.IsNull(trace.HitEntity);
            Assert.IsFalse(trace.BlockedBySolid);
        }

        [Test]
        public void TraceFirstImpactToTarget_DiagonalTrace_HitsCreatureAtTarget()
        {
            var zone = new Zone("TraceZone");
            var caster = CreateCreature("caster");
            var target = CreateCreature("target");

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 7, 7);

            LineTraceResult trace = LineTargeting.TraceFirstImpactToTarget(zone, caster, 5, 5, 7, 7, 4);

            Assert.AreEqual(target, trace.HitEntity);
            Assert.AreEqual(7, trace.ImpactCell.X);
            Assert.AreEqual(7, trace.ImpactCell.Y);
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
