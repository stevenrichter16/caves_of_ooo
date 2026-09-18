using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Hypotheses about identities, mutating heat reactions and holes, beyond query happy paths.</summary>
    public class MultiCellTargetingAdversarialTests
    {
        private const string BodyCells = "0,0;0,1;1,1;2,1";

        private static Entity Body(string cells = BodyCells, bool creature = false)
        {
            var e = new Entity { ID = Guid.NewGuid().ToString(), BlueprintName = "same_blueprint" };
            e.AddPart(new PhysicsPart());
            e.AddPart(new RenderPart { DisplayName = "test body", RenderLayer = 5 });
            if (cells != null) e.AddPart(new SpatialFootprintPart { CellsRaw = cells });
            if (creature)
            {
                e.Tags["Creature"] = "";
                e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = 100, Min = 0, Max = 100 };
            }
            else e.AddPart(new DestructiblePart { HP = 100, MaxHP = 100 });
            return e;
        }

        private static Entity Caster(Zone zone, int x = 5)
        {
            var e = Body(null, true);
            e.AddPart(new ActivatedAbilitiesPart()); e.AddPart(new SkillsPart());
            Assert.IsTrue(e.GetPart<SkillsPart>().AddSkill(new Pyromancy_EmberVein()));
            Assert.IsTrue(zone.AddEntity(e, x, 11));
            return e;
        }

        private static HeatProbe Thermal(Entity e)
        {
            e.AddPart(new ThermalPart { HeatCapacity = 1000f, FlameTemperature = 99999f });
            var probe = new HeatProbe(); e.AddPart(probe); return probe;
        }

        private static void Cast(Entity caster, Zone zone)
        {
            var skill = caster.GetPart<Pyromancy_EmberVein>();
            caster.GetPart<ActivatedAbilitiesPart>().GetAbility(skill.ActivatedAbilityID).CooldownRemaining = 0;
            var command = GameEvent.New("CommandEmberVein");
            command.SetParameter("Zone", (object)zone);
            command.SetParameter("RNG", (object)new Random(17));
            command.SetParameter("SourceCell", (object)zone.GetEntityCell(caster));
            command.SetParameter("DirectionX", 1); command.SetParameter("DirectionY", 0);
            try { caster.FireEvent(command); Assert.IsTrue(command.Handled); }
            finally { command.Release(); }
        }

        [Test]
        public void HeatReactionMovingBodyForwardCannotDoseItAgainInSameCast()
        {
            var zone = new Zone(); var caster = Caster(zone); var body = Body();
            var heat = Thermal(body); zone.AddEntity(body, 8, 10);
            heat.React = () => Assert.IsTrue(zone.MoveEntity(body, 10, 10));
            Cast(caster, zone);
            Assert.AreEqual(1, heat.Count);
            Assert.AreEqual((10, 10), zone.GetEntityPosition(body));
        }

        [Test]
        public void HeatReactionAddingFutureTargetDoesNotExpandCurrentSnapshot()
        {
            var zone = new Zone(); var caster = Caster(zone); var first = Body(); var added = Body();
            var firstHeat = Thermal(first); var addedHeat = Thermal(added); zone.AddEntity(first, 8, 10);
            firstHeat.React = () => zone.AddEntity(added, 10, 10);
            Cast(caster, zone); Assert.AreEqual(0, addedHeat.Count);
            Cast(caster, zone); Assert.AreEqual(1, addedHeat.Count);
        }

        [Test]
        public void HeatReactionRemovingFutureOwnerDoesNotFireOnStaleTarget()
        {
            var zone = new Zone(); var caster = Caster(zone); var first = Body(); var removed = Body();
            var firstHeat = Thermal(first); var removedHeat = Thermal(removed);
            zone.AddEntity(first, 8, 10); zone.AddEntity(removed, 10, 10);
            firstHeat.React = () => zone.RemoveEntity(removed);
            Cast(caster, zone);
            Assert.AreEqual(1, firstHeat.Count); Assert.AreEqual(0, removedHeat.Count);
            Assert.IsNull(zone.GetEntityCell(removed));
        }

        [Test]
        public void HeatReactionRemovingSelfDoesNotSkipTheNextOwner()
        {
            var zone = new Zone(); var caster = Caster(zone); var first = Body(); var second = Body();
            var firstHeat = Thermal(first); var secondHeat = Thermal(second);
            zone.AddEntity(first, 8, 10); zone.AddEntity(second, 10, 10);
            firstHeat.React = () => zone.RemoveEntity(first);
            Cast(caster, zone);
            Assert.AreEqual(1, firstHeat.Count); Assert.AreEqual(1, secondHeat.Count);
        }

        [Test]
        public void NestedHeatCastKeepsBothInvocationSnapshotsIndependent()
        {
            var zone = new Zone(); var caster = Caster(zone); var nestedCaster = Caster(zone, 3);
            var body = Body(); var heat = Thermal(body); zone.AddEntity(body, 8, 10);
            heat.React = () => { heat.React = null; Cast(nestedCaster, zone); };
            Cast(caster, zone);
            Assert.AreEqual(2, heat.Count, "one dose for each actual cast, despite shared world/turn");
        }

        [Test]
        public void HeatReactionMovingUntargetedOwnerIntoBeamCannotAddItMidCast()
        {
            var zone = new Zone(); var caster = Caster(zone); var first = Body(); var moved = Body();
            var firstHeat = Thermal(first); var movedHeat = Thermal(moved);
            zone.AddEntity(first, 8, 10); zone.AddEntity(moved, 10, 14);
            firstHeat.React = () => zone.MoveEntity(moved, 10, 10);
            Cast(caster, zone);
            Assert.AreEqual(0, movedHeat.Count);
            Assert.AreEqual((10, 10), zone.GetEntityPosition(moved));
        }

        [Test]
        public void EqualBlueprintNamesStillRepresentTwoRadiusDamageTargets()
        {
            var zone = new Zone(); var first = Body(creature: true); var second = Body(creature: true);
            zone.AddEntity(first, 10, 10); zone.AddEntity(second, 12, 10);
            CollectionAssert.AreEquivalent(new[] { first, second },
                SpellTargeting.GetCreaturesInRadius(zone, 11, 11, 3));
        }

        [Test]
        public void EqualBlueprintNamesStillReceiveSeparateHeatDoses()
        {
            var zone = new Zone(); var caster = Caster(zone); var first = Body(); var second = Body();
            var firstHeat = Thermal(first); var secondHeat = Thermal(second);
            zone.AddEntity(first, 8, 10); zone.AddEntity(second, 10, 10);
            Cast(caster, zone);
            Assert.AreEqual(1, firstHeat.Count); Assert.AreEqual(1, secondHeat.Count);
        }

        [Test]
        public void DestroyingFirstCollectedOwnerDoesNotEraseSecondSnapshotTarget()
        {
            var zone = new Zone(); var first = Body(); var second = Body();
            zone.AddEntity(first, 8, 10); zone.AddEntity(second, 10, 10);
            var hits = SkillLine.Collect(zone, null, 5, 11, 1, 0, 7);
            CollectionAssert.AreEquivalent(new[] { first, second }, hits);
            foreach (var hit in hits) DestructionSystem.RouteDamage(hit, new Damage(100), null, zone);
            Assert.AreEqual(0, zone.EntityCount);
            CollectionAssert.AreEquivalent(new[] { first, second }, hits);
        }

        [Test]
        public void ElementalLineExcludesEverySurfaceOfLargeCaster()
        {
            var zone = new Zone(); var caster = Body(creature: true); var other = Body();
            zone.AddEntity(caster, 5, 10); zone.AddEntity(other, 9, 10);
            CollectionAssert.AreEqual(new[] { other }, SkillLine.Collect(zone, caster, 5, 11, 1, 0, 7));
        }

        [Test]
        public void BeamExcludesEverySurfaceOfLargeCaster()
        {
            var zone = new Zone(); var caster = Body(creature: true); var other = Body(creature: true);
            zone.AddEntity(caster, 5, 10); zone.AddEntity(other, 9, 10);
            CollectionAssert.AreEqual(new[] { other },
                SpellTargeting.TraceBeam(zone, caster, 5, 11, 1, 0, 7).HitEntities);
        }

        [TestCase(true)] [TestCase(false)]
        public void ConeRidgeEdgeOccludesCenterRayOnlyWhenActuallyOccupied(bool footprint)
        {
            var zone = new Zone(); var ridge = Body(footprint ? "0,0;0,1" : null);
            ridge.Tags["Solid"] = ""; var target = Body(null, true);
            zone.AddEntity(ridge, 6, 10); zone.AddEntity(target, 7, 11);
            Assert.AreEqual(footprint ? 0 : 1,
                SpellTargeting.GetCreaturesInCone(zone, null, 5, 11, 1, 0, 2).Count);
        }

        [TestCase(true)] [TestCase(false)]
        public void FootprintTerrainIsProjectileTargetOnlyWithStructuralPart(bool destructible)
        {
            var zone = new Zone(); var ridge = Body(); ridge.Tags["Terrain"] = ""; ridge.Tags["Solid"] = "";
            if (!destructible) ridge.RemovePart(ridge.GetPart<DestructiblePart>());
            zone.AddEntity(ridge, 8, 10);
            var hit = LineTargeting.TraceFirstImpact(zone, null, 5, 11, 1, 0, 7);
            Assert.AreSame(destructible ? ridge : null, hit.HitEntity);
            Assert.AreEqual(!destructible, hit.BlockedBySolid);
        }

        [Test]
        public void LegacyEqualLayerPickerRetainsLastAddedFirstPriority()
        {
            var zone = new Zone(); var first = Body(null); var second = Body(null);
            zone.AddEntity(first, 10, 10); zone.AddEntity(second, 10, 10);
            var cell = zone.GetCell(10, 10);
            Assert.AreSame(second, WorldInteractionSystem.ResolveTarget(cell));
            CollectionAssert.AreEqual(new[] { second, first }.Select(e => WorldInteractionSystem.PickTargetCommandPrefix + e.ID),
                WorldInteractionSystem.BuildTargetPickerActions(cell).Select(row => row.Command));
        }

        [TestCase(true)] [TestCase(false)]
        public void UnderfootActionsNeedAnOccupiedActorSurfaceInsteadOfAnchorMembership(bool covered)
        {
            var zone = new Zone(); var actor = Body("1,0", true); var fixture = Body(null);
            fixture.Tags["UnderfootInteractable"] = "";
            zone.AddEntity(actor, 10, 10); zone.AddEntity(fixture, covered ? 11 : 10, 10);
            var actions = new List<InventoryAction>();
            WorldInteractionSystem.AppendUnderfootActions(actions, zone.GetCell(covered ? 11 : 10, 10), actor);
            Assert.AreEqual(covered ? 1 : 0, actions.Count);
        }

        [Test]
        public void ChainNeverRevisitsCoveredCellsOfPriorOwners()
        {
            var zone = new Zone(); var first = Body(creature: true); var second = Body(creature: true);
            var third = Body(creature: true);
            zone.AddEntity(first, 10, 10); zone.AddEntity(second, 14, 10); zone.AddEntity(third, 18, 10);
            CollectionAssert.AreEqual(new[] { second, third },
                SpellTargeting.FindChainTargets(zone, null, first, 12, 2));
            zone.RemoveEntity(second);
            Assert.IsEmpty(SpellTargeting.FindChainTargets(zone, null, first, 12, 2));
        }

        [Test]
        public void SerializedDamagedBodyRemainsTargetableAtLoadedEdgeWithOneHpPool()
        {
            var original = Body(); original.GetPart<DestructiblePart>().HP = 73;
            var loaded = PartRoundTripHelper.RoundTripEntity(original); var zone = new Zone();
            Assert.IsTrue(zone.AddEntity(loaded, 10, 10));
            var target = WorldInteractionSystem.FindInCell(zone.GetCell(12, 11), loaded.ID);
            Assert.AreSame(loaded, target);
            DestructionSystem.RouteDamage(target, new Damage(5), null, zone);
            Assert.AreEqual(68, loaded.GetPart<DestructiblePart>().HP);
            Assert.AreEqual(73, original.GetPart<DestructiblePart>().HP);
        }

        [TestCase(true)] [TestCase(false)]
        public void RadiusAtZoneCornerClipsCellsWithoutInventingAnchorHoleBody(bool occupied)
        {
            var zone = new Zone(); var body = Body(occupied ? "0,0;1,0;0,1;1,1" : "0,0;1,0;0,1", true);
            Assert.IsTrue(zone.AddEntity(body, Zone.Width - 2, Zone.Height - 2));
            Assert.AreEqual(occupied ? 1 : 0,
                SpellTargeting.GetCreaturesInRadius(zone, Zone.Width - 1, Zone.Height - 1, 0).Count);
            CollectionAssert.AreEqual(new[] { body },
                SpellTargeting.GetCreaturesInRadius(zone, Zone.Width - 1, Zone.Height - 1, 2));
        }

        private sealed class HeatProbe : Part
        {
            public int Count;
            public Action React;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "ApplyHeat") { Count++; React?.Invoke(); }
                return true;
            }
        }
    }
}
