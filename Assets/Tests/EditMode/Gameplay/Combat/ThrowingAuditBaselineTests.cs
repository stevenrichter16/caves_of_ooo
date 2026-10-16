using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Audit 1: real shipped dagger and real command, before height implementation.
    /// Direct-command evidence does not prove native input's scheduler payment or rendered pixels.</summary>
    public sealed class ThrowingAuditBaselineTests : FiftyWorldFixture
    {
        sealed class RecordedRandom : Random
        {
            public readonly List<string> Draws = new List<string>();
            public RecordedRandom() : base(123) { }
            public override int Next(int min, int max)
            { int value = base.Next(min, max); Draws.Add(min + ":" + max + "=" + value); return value; }
            public override int Next(int max)
            { int value = base.Next(max); Draws.Add("0:" + max + "=" + value); return value; }
        }
        sealed class Outcome
        {
            public Zone Zone;
            public Entity Actor, Target, Projectile, Clutter;
            public int TargetDamage, ClutterHpBefore, ClutterHpAfter, TickBefore, TickAfter;
            public Cell Landing;
            public RecordedRandom Rng;
            public List<Point> FxPath;
        }
        Outcome Run(string obstacle = null, int agility = 16, int targetX = 16)
        {
            var zone = new Zone("throw-audit-" + (obstacle ?? "clear"));
            var actor = Factory.CreateEntity("Player");
            actor.GetStat("Strength").BaseValue = 16; actor.GetStat("Agility").BaseValue = agility;
            Assert.False(actor.Parts.Any(p => p is BaseSkillPart), "The reference actor has no purchased skills.");
            Assert.True(zone.AddEntity(actor, 10, 10));
            var target = new Entity { BlueprintName = "ThrowAuditTarget" }; target.SetTag("Creature");
            target.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 200, Max = 200 };
            target.Statistics["AV"] = new Stat { Name = "AV", BaseValue = 0 };
            target.AddPart(new PhysicsPart()); Assert.True(zone.AddEntity(target, 16, 10));
            Entity clutter = null;
            if (obstacle == "dagger") { clutter = Factory.CreateEntity("Dagger"); Assert.True(zone.AddEntity(clutter, 13, 10)); }
            if (obstacle == "wall") { clutter = new Entity(); clutter.SetTag("Wall"); clutter.SetTag("Solid"); Assert.True(zone.AddEntity(clutter, 13, 10)); }
            var projectile = Factory.CreateEntity("Dagger");
            Assert.True(actor.GetPart<InventoryPart>().AddObject(projectile));
            Assert.True(HandlingService.CanThrow(actor, projectile, out string reason), reason);
            var result = new Outcome { Zone = zone, Actor = actor, Target = target, Projectile = projectile, Clutter = clutter,
                ClutterHpBefore = clutter?.GetStatValue("Hitpoints") ?? 0, TickBefore = Clock.TickCount, Rng = new RecordedRandom() };
            AsciiFxBus.Clear();
            var command = InventorySystem.ExecuteCommand(new ThrowItemCommand(projectile, targetX, 10, result.Rng), actor, zone);
            Assert.True(command.Success, command.ErrorMessage);
            result.TargetDamage = 200 - target.GetStatValue("Hitpoints");
            result.ClutterHpAfter = clutter?.GetStatValue("Hitpoints") ?? 0;
            result.Landing = zone.GetEntityCell(projectile); result.TickAfter = Clock.TickCount;
            var requests = AsciiFxBus.Drain();
            foreach (var fx in requests)
            {
                if (fx.Type == AsciiFxRequestType.Projectile && fx.Zone == zone)
                { Assert.Null(result.FxPath, "Exactly one ordinary throw flight request."); Assert.True(fx.BlocksTurnAdvance); result.FxPath = new List<Point>(fx.Path); }
                AsciiFxBus.Release(fx);
            }
            Assert.False(actor.GetPart<InventoryPart>().Objects.Contains(projectile));
            Assert.NotNull(result.Landing); Assert.AreSame(projectile, zone.GetCell(result.Landing.X, 10).Objects.Single(e => e == projectile));
            Assert.Null(projectile.GetPart<PhysicsPart>().InInventory); Assert.Null(projectile.GetPart<PhysicsPart>().Equipped);
            Assert.AreEqual(1, projectile.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(result.TickBefore, result.TickAfter, "Direct command does not pay the native input action; do not claim otherwise.");
            return result;
        }

        [Test] public void ClearNoSkillThrowHasOneOriginalLandedUnitAndDeterministicCombatAndFx()
        {
            var first = Run(); var second = Run();
            Assert.Greater(first.TargetDamage, 0); Assert.AreEqual(first.TargetDamage, second.TargetDamage);
            CollectionAssert.AreEqual(first.Rng.Draws, second.Rng.Draws); Assert.Greater(first.Rng.Draws.Count, 1);
            Assert.AreEqual(16, first.Landing.X); Assert.AreEqual(16, first.FxPath.Last().X);
            TestContext.WriteLine("Baseline damage=" + first.TargetDamage + "; landing=(16,10); RNG=" + string.Join(",", first.Rng.Draws));
        }
        [Test] public void OrdinaryThrowAtStandingCreatureClearsShippedFloorDaggerWithoutChangingRngOrItem()
        {
            var clear = Run(); var cluttered = Run("dagger");
            Assert.Greater(clear.TargetDamage, 0, "Positive no-clutter control.");
            Assert.AreEqual(clear.TargetDamage, cluttered.TargetDamage, "A floor dagger must not steal a body-directed throw.");
            CollectionAssert.AreEqual(clear.Rng.Draws, cluttered.Rng.Draws, "Geometry must not add RNG draws for equivalent encounters.");
            Assert.AreEqual(cluttered.ClutterHpBefore, cluttered.ClutterHpAfter);
            Assert.AreSame(cluttered.Clutter, cluttered.Zone.GetCell(13, 10).Objects.Single(e => e == cluttered.Clutter));
            Assert.AreEqual(16, cluttered.Landing.X); Assert.AreEqual(16, cluttered.FxPath.Last().X);
        }
        [Test] public void RealWallStillInterceptsAndLandsOnApproachSideWithoutCombatRolls()
        {
            var blocked = Run("wall"); Assert.Zero(blocked.TargetDamage); Assert.IsEmpty(blocked.Rng.Draws);
            Assert.AreEqual(12, blocked.Landing.X); Assert.AreEqual(13, blocked.FxPath.Last().X);
        }
        [Test] public void DeliberatelySelectedDaggerCellRemainsAnAffectedLowTarget()
        {
            var aimed = Run("dagger", targetX: 13); Assert.Zero(aimed.TargetDamage);
            Assert.Less(aimed.ClutterHpAfter, aimed.ClutterHpBefore); Assert.AreEqual(13, aimed.Landing.X);
        }
        [Test] public void PhysicalAccuracyMissStopsAtTheFirstCreatureAndStillLandsOnePaidUnit()
        {
            var missed = Run(agility: 1); Assert.Zero(missed.TargetDamage); Assert.AreEqual(1, missed.Rng.Draws.Count);
            Assert.AreEqual(16, missed.Landing.X); Assert.AreEqual(16, missed.FxPath.Last().X);
        }
    }
}
