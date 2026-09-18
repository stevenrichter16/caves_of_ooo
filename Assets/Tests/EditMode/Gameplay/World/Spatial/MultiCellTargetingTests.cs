using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Body contact resolves one native identity; every new hit remains independent.</summary>
    public class MultiCellTargetingTests
    {
        private const string Square = "0,0;1,0;0,1;1,1";
        private const string LongBody = "0,0;1,0;2,0;3,0";

        private static Entity Body(string name, string shape = Square, bool creature = false,
            bool solid = false, bool terrain = false)
        {
            var e = new Entity { ID = System.Guid.NewGuid().ToString(), BlueprintName = name };
            e.AddPart(new RenderPart { DisplayName = name, RenderLayer = terrain ? 0 : 5 });
            e.AddPart(new PhysicsPart { Solid = solid });
            if (shape != null) e.AddPart(new SpatialFootprintPart { CellsRaw = shape });
            if (solid) e.Tags["Solid"] = "";
            if (terrain) e.Tags["Terrain"] = "";
            if (creature)
            {
                e.Tags["Creature"] = "";
                e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = 100, Min = 0, Max = 100 };
            }
            else e.AddPart(new DestructiblePart { HP = 100, MaxHP = 100 });
            return e;
        }

        [TestCase(true)] [TestCase(false)]
        public void RadiusHitsCoveredEdgeOutsideAnchorOnlyWhenBodyOccupiesIt(bool footprint)
        {
            var zone = new Zone(); var target = Body("toad", footprint ? Square : null, true);
            Assert.IsTrue(zone.AddEntity(target, 10, 10));
            var hits = SpellTargeting.GetCreaturesInRadius(zone, 12, 11, 1);
            Assert.AreEqual(footprint ? 1 : 0, hits.Count);
            if (footprint) Assert.AreSame(target, hits[0]);
        }

        [Test]
        public void RadiusReturnsOneOwnerAndNewPulseCanDamageItAgain()
        {
            var zone = new Zone(); var target = Body("toad", creature: true);
            zone.AddEntity(target, 10, 10);
            for (int pulse = 0; pulse < 2; pulse++)
            {
                var hits = SpellTargeting.GetCreaturesInRadius(zone, 10, 10, 3);
                CollectionAssert.AreEqual(new[] { target }, hits);
                foreach (var hit in hits) DestructionSystem.RouteDamage(hit, new Damage(7), null, zone);
                Assert.AreEqual(100 - 7 * (pulse + 1), target.GetStatValue("Hitpoints"));
            }
        }

        [Test]
        public void RadiusPreservesExclusionCreatureFilterAndEmptyHole()
        {
            var zone = new Zone(); var target = Body("toad", "1,0;0,1;1,1", true);
            var prop = Body("rock", LongBody);
            zone.AddEntity(target, 10, 10); zone.AddEntity(prop, 20, 10);
            Assert.IsEmpty(SpellTargeting.GetCreaturesInRadius(zone, 10, 10, 0));
            Assert.IsEmpty(SpellTargeting.GetCreaturesInRadius(zone, 11, 11, 0, target));
            Assert.IsEmpty(SpellTargeting.GetCreaturesInRadius(zone, 20, 10, 4));
            CollectionAssert.AreEqual(new[] { target }, SpellTargeting.GetCreaturesInRadius(zone, 11, 11, 0));
        }

        [TestCase(true)] [TestCase(false)]
        public void BeamHitsBodyAcrossSeveralCellsOnceAndDoesNotHitAnAdjacentSingleCell(bool footprint)
        {
            var zone = new Zone(); var caster = Body("caster", null, true);
            var target = Body("toad", footprint ? "0,0;0,1;1,1;2,1" : null, true);
            zone.AddEntity(caster, 5, 11); zone.AddEntity(target, 8, 10);
            var trace = SpellTargeting.TraceBeam(zone, caster, 5, 11, 1, 0, 10);
            Assert.AreEqual(footprint ? 1 : 0, trace.HitEntities.Count);
            if (footprint) Assert.AreSame(target, trace.HitEntities[0]);
        }

        [TestCase(true)] [TestCase(false)]
        public void BeamStopsAtOccupiedRidgeSurfaceAndPassesItsEmptyControl(bool footprint)
        {
            var zone = new Zone(); var ridge = Body("ridge", footprint ? Square : null, solid: true, terrain: true);
            var target = Body("toad", null, true);
            zone.AddEntity(ridge, 8, 10); zone.AddEntity(target, 12, 11);
            var trace = SpellTargeting.TraceBeam(zone, null, 5, 11, 1, 0, 10);
            Assert.AreEqual(footprint, trace.BlockedBySolid);
            Assert.AreEqual(footprint ? 0 : 1, trace.HitEntities.Count);
            if (footprint) Assert.AreEqual(8, trace.ImpactCell.X);
        }

        [Test]
        public void ConeFindsOffAnchorBodyOnceAndKeepsCreatureFilter()
        {
            var zone = new Zone(); var target = Body("toad", "0,0;0,1;1,1;2,1", true);
            var prop = Body("grass", null);
            zone.AddEntity(target, 6, 9); zone.AddEntity(prop, 8, 11);
            CollectionAssert.AreEqual(new[] { target },
                SpellTargeting.GetCreaturesInCone(zone, null, 5, 10, 1, 0, 3));
            Assert.IsEmpty(SpellTargeting.GetCreaturesInCone(zone, target, 5, 10, 1, 0, 3));
        }

        [TestCase(true)] [TestCase(false)]
        public void ChainUsesNearestBodyContactAtBothEnds(bool footprint)
        {
            var zone = new Zone(); var first = Body("first", footprint ? LongBody : null, true);
            var next = Body("next", "-3,0;-2,0;-1,0;0,0", true);
            zone.AddEntity(first, 10, 10); zone.AddEntity(next, 18, 10);
            var hits = SpellTargeting.FindChainTargets(zone, null, first, 3, 2);
            Assert.AreEqual(footprint ? 1 : 0, hits.Count);
            if (footprint) Assert.AreSame(next, hits[0]);
        }

        [TestCase(true)] [TestCase(false)]
        public void ProjectileImpactsDestructibleRidgeBodyButOrdinaryTerrainStaysGeometry(bool footprint)
        {
            var zone = new Zone(); var ridge = Body("ridge", footprint ? Square : null, solid: true, terrain: true);
            zone.AddEntity(ridge, 8, 10);
            var trace = LineTargeting.TraceFirstImpact(zone, null, 5, footprint ? 11 : 10, 1, 0, 8);
            Assert.AreEqual(8, trace.ImpactCell.X);
            Assert.AreEqual(footprint ? 11 : 10, trace.ImpactCell.Y);
            if (footprint) Assert.AreSame(ridge, trace.HitEntity);
            else { Assert.IsNull(trace.HitEntity); Assert.IsTrue(trace.BlockedBySolid); }
        }

        [Test]
        public void ProjectileToCellHitsCoveredCreatureEdgeAndPreservesEmptyHole()
        {
            var zone = new Zone(); var target = Body("toad", "0,0;0,2;1,2", true);
            zone.AddEntity(target, 8, 10);
            Assert.AreSame(target, LineTargeting.TraceFirstImpactToTarget(zone, null, 5, 12, 9, 12, 8).HitEntity);
            Assert.IsNull(LineTargeting.TraceFirstImpactToTarget(zone, null, 5, 11, 9, 11, 8).HitEntity);
        }

        [Test]
        public void ElementalLineCollectsBodyOnceAndIndependentCastCanHitAgain()
        {
            var zone = new Zone(); var target = Body("pipe", "0,0;0,1;1,1;2,1");
            zone.AddEntity(target, 8, 10);
            for (int cast = 0; cast < 2; cast++)
            {
                var hits = SkillLine.Collect(zone, null, 5, 11, 1, 0, 8);
                CollectionAssert.AreEqual(new[] { target }, hits);
                foreach (var hit in hits) DestructionSystem.RouteDamage(hit, new Damage(7), null, zone);
                Assert.AreEqual(100 - 7 * (cast + 1), target.GetPart<DestructiblePart>().HP);
            }
            Assert.IsEmpty(SkillLine.Collect(zone, target, 5, 11, 1, 0, 8));
        }

        [Test]
        public void ElementalLineCollectsRidgeSurfaceBeforeStoppingAndCreatureWinsSameCell()
        {
            var zone = new Zone(); var ridge = Body("ridge", Square, solid: true, terrain: true);
            var target = Body("toad", null, true); var behind = Body("behind", null, true);
            zone.AddEntity(ridge, 8, 10); zone.AddEntity(target, 8, 11); zone.AddEntity(behind, 10, 11);
            var hits = SkillLine.Collect(zone, null, 5, 11, 1, 0, 8, out bool blocked);
            CollectionAssert.AreEqual(new[] { target, ridge }, hits);
            Assert.IsTrue(blocked);
        }

        [TestCase(true)] [TestCase(false)]
        public void RiteSeesCreatureEdgeWhileEmptyNeighborRemainsUntargeted(bool footprint)
        {
            var zone = new Zone(); var target = Body("toad", footprint ? Square : null, true);
            zone.AddEntity(target, 8, 10);
            var method = typeof(Entity).Assembly.GetType("CavesOfOoo.Core.RiteTargeting")
                .GetMethod("FirstCreatureInLine", BindingFlags.Static | BindingFlags.NonPublic);
            var hit = method.Invoke(null, new object[] { zone, null, 5, 11, 1, 0, 8 });
            Assert.AreSame(footprint ? target : null, hit);
        }

        [Test]
        public void InteractionResolvesBodyAtEveryEdgeButNotAnchorHole()
        {
            var zone = new Zone(); var prop = Body("pipe", "1,0;0,1;1,1"); zone.AddEntity(prop, 10, 10);
            var edge = zone.GetCell(11, 11); var hole = zone.GetCell(10, 10);
            Assert.AreSame(prop, WorldInteractionSystem.ResolveTarget(edge));
            Assert.AreSame(prop, WorldInteractionSystem.FindInCell(edge, prop.ID));
            StringAssert.Contains("pipe", WorldInteractionSystem.DescribeCell(edge));
            Assert.AreEqual(1, WorldInteractionSystem.BuildTargetPickerActions(edge).Count);
            Assert.IsFalse(WorldInteractionSystem.IsPileCell(edge));
            Assert.IsNull(WorldInteractionSystem.ResolveTarget(hole));
            Assert.IsNull(WorldInteractionSystem.FindInCell(hole, prop.ID));
            Assert.AreEqual("You see nothing here.", WorldInteractionSystem.DescribeCell(hole));
            Assert.IsEmpty(WorldInteractionSystem.BuildTargetPickerActions(hole));
        }

        [Test]
        public void InteractionKeepsTerrainAndRenderPriorityAcrossBodyAndAnchorOccupants()
        {
            var zone = new Zone(); var ridge = Body("ridge", terrain: true);
            var item = Body("coin", null); var taller = Body("post", Square);
            taller.GetPart<RenderPart>().RenderLayer = 9;
            zone.AddEntity(ridge, 10, 10); zone.AddEntity(taller, 10, 10); zone.AddEntity(item, 11, 11);
            var edge = zone.GetCell(11, 11);
            Assert.AreSame(taller, WorldInteractionSystem.ResolveTarget(edge));
            Assert.IsTrue(WorldInteractionSystem.IsPileCell(edge));
            var commands = WorldInteractionSystem.BuildTargetPickerActions(edge).Select(row => row.Command).ToArray();
            CollectionAssert.AreEqual(new[] { taller, item, ridge }
                .Select(e => WorldInteractionSystem.PickTargetCommandPrefix + e.ID), commands);
        }

        [TestCase(true)] [TestCase(false)]
        public void StructuralReachMeasuresBothBodiesInsteadOfTheirAnchors(bool footprint)
        {
            var zone = new Zone(); var target = Body("ridge", footprint ? LongBody : null);
            var actor = Body("actor", "0,0;-1,0", true);
            zone.AddEntity(target, 10, 10); zone.AddEntity(actor, 15, 10);
            Assert.AreEqual(footprint, DestructionSystem.IsWithinStrikeReach(actor, target, zone));
            zone.MoveEntity(actor, 16, 10);
            Assert.IsFalse(DestructionSystem.IsWithinStrikeReach(actor, target, zone));
        }

        [Test]
        public void DestructionFromEdgeRemovesWholeBodyAndSpillsContentsOnce()
        {
            var zone = new Zone(); var chest = Body("chest"); var loot = Body("coin", null);
            var container = new ContainerPart(); container.Contents.Add(loot); chest.AddPart(container);
            var events = new DestroyCounter(); chest.AddPart(events); zone.AddEntity(chest, 10, 10);
            var picked = WorldInteractionSystem.FindInCell(zone.GetCell(11, 11), chest.ID);
            Assert.AreSame(chest, picked);
            DestructionSystem.RouteDamage(picked, new Damage(100), null, zone);
            DestructionSystem.Destroy(chest, null, zone, "second source");
            Assert.AreEqual(1, events.Destroyed);
            Assert.AreEqual(1, zone.EntityCount);
            Assert.AreEqual((10, 10), zone.GetEntityPosition(loot));
            foreach (var cell in new[] { zone.GetCell(10,10), zone.GetCell(10,11), zone.GetCell(11,10), zone.GetCell(11,11) })
                Assert.IsNull(WorldInteractionSystem.FindInCell(cell, chest.ID));
        }

        [TestCase(true)] [TestCase(false)]
        public void EmberVeinHeatPassTouchesCoveredOwnerOncePerCast(bool footprint)
        {
            var zone = new Zone(); var caster = Body("caster", null, true);
            caster.AddPart(new ActivatedAbilitiesPart()); caster.AddPart(new SkillsPart());
            var skill = new Pyromancy_EmberVein();
            Assert.IsTrue(caster.GetPart<SkillsPart>().AddSkill(skill));
            var target = Body("pipe", footprint ? "0,0;0,1;1,1;2,1" : null);
            target.AddPart(new ThermalPart { HeatCapacity = 1000f, FlameTemperature = 99999f });
            var heat = new HeatCounter(); target.AddPart(heat);
            zone.AddEntity(caster, 5, 11); zone.AddEntity(target, 8, 10);
            for (int cast = 0; cast < 2; cast++)
            {
                caster.GetPart<ActivatedAbilitiesPart>().GetAbility(skill.ActivatedAbilityID).CooldownRemaining = 0;
                var cmd = GameEvent.New("CommandEmberVein");
                cmd.SetParameter("Zone", (object)zone);
                cmd.SetParameter("RNG", (object)new System.Random(7));
                cmd.SetParameter("SourceCell", (object)zone.GetEntityCell(caster));
                cmd.SetParameter("DirectionX", 1); cmd.SetParameter("DirectionY", 0);
                caster.FireEvent(cmd); Assert.IsTrue(cmd.Handled); cmd.Release();
                Assert.AreEqual(footprint ? cast + 1 : 0, heat.Applied);
            }
        }

        private sealed class HeatCounter : Part
        {
            public int Applied;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "ApplyHeat") Applied++;
                return true;
            }
        }

        private sealed class DestroyCounter : Part
        {
            public int Destroyed;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "Destroyed") Destroyed++;
                return true;
            }
        }
    }
}
