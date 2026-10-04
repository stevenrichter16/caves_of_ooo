using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    internal sealed class DensityCombatFixture : IDisposable
    {
        readonly EntityFactory previousFactory = LoadoutPart.Factory;
        readonly System.Random previousRng = LoadoutPart.Rng;
        public readonly EntityFactory Factory = new EntityFactory();
        public readonly Zone Zone = new Zone("density-combat");
        public DensityCombatFixture()
        {
            Factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            LoadoutPart.Factory = Factory; LoadoutPart.Rng = new System.Random(418);
            FactionManager.Initialize(); MessageLog.Clear();
        }
        public Entity Actor(int x = 10, int y = 10, string skills = "Cryomancy_IceLance", string faction = "OutlandRaiders", bool assist = false)
        {
            var e = Factory.CreateEntity("Creature"); e.ID = "density-actor-" + Guid.NewGuid().ToString("N"); e.Tags["Faction"] = faction;
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 500, Max = 500, Min = 0 };
            e.AddPart(new CombatTacticsPart { SkillClasses = skills, AbilityChance = 100, AssistAllies = assist });
            e.FireEventAndRelease(GameEvent.New("ObjectCreated"));
            Zone.AddEntity(e, x, y);
            var brain = e.GetPart<BrainPart>(); brain.CurrentZone = Zone; brain.Rng = new LowRandom(); brain.FleeThreshold = 0;
            return e;
        }
        public Entity Target(int x = 13, int y = 10)
        {
            var e = Factory.CreateEntity("Player"); e.ID = "density-target-" + Guid.NewGuid().ToString("N");
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 500, Max = 500, Min = 0 };
            Zone.AddEntity(e, x, y); return e;
        }
        public Entity Equip(Entity actor, string blueprint)
        {
            var item = Factory.CreateEntity(blueprint);
            Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(item));
            Assert.IsTrue(InventorySystem.Equip(actor, item)); return item;
        }
        public Entity Wall(int x, int y)
        {
            var wall = new Entity(); wall.SetTag("Solid"); wall.AddPart(new PhysicsPart { Solid = true });
            Zone.AddEntity(wall, x, y); return wall;
        }
        public static ActivatedAbility Ability(Entity actor) => actor.GetPart<ActivatedAbilitiesPart>().AbilityList.Single();
        public bool Cast(Entity actor, Entity target, System.Random rng = null) => actor.GetPart<CombatTacticsPart>().TryUseAbility(target, Zone, rng ?? new LowRandom());
        public void Dispose() { LoadoutPart.Factory = previousFactory; LoadoutPart.Rng = previousRng; FactionManager.Reset(); }
        public sealed class LowRandom : System.Random { public override int Next(int maxValue) => 0; }
        public sealed class HighRandom : System.Random { public override int Next(int maxValue) => maxValue - 1; }
    }

    public sealed class DensityCombatTacticsTests
    {
        DensityCombatFixture f;
        [SetUp] public void Setup() => f = new DensityCombatFixture();
        [TearDown] public void TearDown() => f.Dispose();

        [Test] public void ObjectCreatedGrantsSkillsAndTheirRegisteredCooldownOnce()
        {
            var actor = f.Actor(); var skill = actor.GetPart<SkillsPart>().SkillList.Single();
            var ability = DensityCombatFixture.Ability(actor);
            Assert.AreEqual(skill.ActivatedAbilityID, ability.ID); Assert.AreEqual(8, ability.MaxCooldown);
            actor.FireEventAndRelease(GameEvent.New("ObjectCreated"));
            Assert.AreEqual(1, actor.GetPart<SkillsPart>().SkillList.Count);
            Assert.AreEqual(1, actor.GetPart<ActivatedAbilitiesPart>().AbilityList.Count);
        }
        [TestCase("Cryomancy_IceLance")] [TestCase("Corrosion_AcidSpray")] [TestCase("Pyromancy_EmberSpit")]
        public void RealProjectileHitsEnemyAndUsesRegisteredCooldown(string skill)
        {
            var actor = f.Actor(skills: skill); var target = f.Target();
            Assert.IsTrue(f.Cast(actor, target)); Assert.Less(target.GetStatValue("Hitpoints"), 500);
            var ability = DensityCombatFixture.Ability(actor); Assert.AreEqual(ability.MaxCooldown, ability.CooldownRemaining);
            Assert.IsFalse(f.Cast(actor, target));
            actor.FireEventAndRelease(GameEvent.New("EndTurn"));
            Assert.AreEqual(ability.MaxCooldown - 1, ability.CooldownRemaining);
        }
        [Test] public void AdjacentShankReplacesNormalAttackInKillGoal()
        {
            var actor = f.Actor(skills: "ShortBlades_Shank"); f.Equip(actor, "Dagger"); var target = f.Target(11, 10);
            actor.GetPart<BrainPart>().PushGoal(new KillGoal(target));
            actor.FireEventAndRelease(GameEvent.New("TakeTurn"));
            Assert.Greater(DensityCombatFixture.Ability(actor).CooldownRemaining, 0);
            Assert.AreEqual(500, actor.GetStatValue("Hitpoints"));
        }
        [Test] public void AdjacentAllyDoesNotRedirectShankFromChosenEnemy()
        {
            var actor = f.Actor(skills: "ShortBlades_Shank"); f.Equip(actor, "Dagger");
            var ally = f.Actor(10, 9, ""); var target = f.Target(11, 10);
            Assert.IsTrue(f.Cast(actor, target)); Assert.AreEqual(500, ally.GetStatValue("Hitpoints"));
            Assert.Greater(DensityCombatFixture.Ability(actor).CooldownRemaining, 0);
            Assert.IsFalse(f.Cast(actor, target));
        }
        [Test] public void ReachAttackHitsAtTwoCellsWithoutMoving()
        {
            var actor = f.Actor(skills: "LongBlades_Lunge"); f.Equip(actor, "ShortSword"); var target = f.Target(12, 10);
            Assert.IsTrue(f.Cast(actor, target)); Assert.AreEqual((10, 10), f.Zone.GetEntityPosition(actor));
            Assert.Greater(DensityCombatFixture.Ability(actor).CooldownRemaining, 0);
        }
        [Test] public void BerserkRequiresEquippedAxeAndNearbyEnemy()
        {
            var actor = f.Actor(skills: "Axe_Berserk"); var target = f.Target();
            Assert.IsFalse(f.Cast(actor, target)); f.Equip(actor, "BreacherCleaver");
            Assert.IsFalse(f.Cast(actor, target)); f.Zone.MoveEntity(target, 11, 10);
            Assert.IsTrue(f.Cast(actor, target)); Assert.IsTrue(actor.HasEffect<BerserkEffect>());
        }
        [TestCase(0, false)] [TestCase(100, true)]
        public void ChanceEndpointsHaveMeaningfulCounterChecks(int chance, bool expected)
        {
            var actor = f.Actor(); actor.GetPart<CombatTacticsPart>().AbilityChance = chance;
            var target = f.Target(); Assert.AreEqual(expected, f.Cast(actor, target, new DensityCombatFixture.HighRandom()));
            Assert.AreEqual(expected, target.GetStatValue("Hitpoints") < 500);
        }
        [TestCase(12, 11)] [TestCase(17, 10)]
        public void OffRayOrOutOfRangeDoesNotWasteCooldown(int x, int y)
        {
            var actor = f.Actor(); var target = f.Target(x, y);
            Assert.IsFalse(f.Cast(actor, target)); Assert.AreEqual(0, DensityCombatFixture.Ability(actor).CooldownRemaining);
        }
        [Test] public void WallsAndFirstBodiesBlockRangedCasting()
        {
            var actor = f.Actor(); var target = f.Target(); var wall = f.Wall(11, 10);
            Assert.IsFalse(f.Cast(actor, target)); f.Zone.RemoveEntity(wall);
            var ally = f.Actor(11, 10, ""); Assert.IsFalse(f.Cast(actor, target));
            Assert.AreEqual(500, ally.GetStatValue("Hitpoints")); f.Zone.RemoveEntity(ally);
            Assert.IsTrue(f.Cast(actor, target));
        }
        [Test] public void DeclinedAbilityFallsBackToMovement()
        {
            var actor = f.Actor(); actor.GetPart<CombatTacticsPart>().AbilityChance = 0; var target = f.Target();
            actor.GetPart<BrainPart>().PushGoal(new KillGoal(target)); actor.FireEventAndRelease(GameEvent.New("TakeTurn"));
            Assert.AreEqual((11, 10), f.Zone.GetEntityPosition(actor));
            Assert.AreEqual(0, DensityCombatFixture.Ability(actor).CooldownRemaining);
        }
        [Test] public void PersonalAttackAlertsOptedInNearbyFactionMate()
        {
            var actor = f.Actor(skills: "", assist: true); var ally = f.Actor(10, 12, "", assist: true); var target = f.Target();
            actor.GetPart<BrainPart>().SetPersonallyHostile(target);
            Assert.IsTrue(ally.GetPart<BrainPart>().IsPersonallyHostileTo(target));
        }
        [TestCase(false)] [TestCase(true)]
        public void AssistanceDoesNotRecruitUnwillingOrOccludedReceivers(bool wall)
        {
            var actor = f.Actor(skills: "", assist: true); var ally = f.Actor(10, 12, "", assist: wall); var target = f.Target();
            if (wall) f.Wall(10, 11);
            actor.GetPart<BrainPart>().SetPersonallyHostile(target);
            Assert.IsFalse(ally.GetPart<BrainPart>().IsPersonallyHostileTo(target));
        }
    }
}
