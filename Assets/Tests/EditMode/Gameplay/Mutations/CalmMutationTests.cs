using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M2.2 — CalmMutation pushes <see cref="NoFightGoal"/> on hit, scales with
    /// mutation level, and is idempotent when the target is already pacified.
    ///
    /// Tests exercise <c>Cast</c> through a line-of-sight-clear zone so the
    /// ApplyOnHitEffect branch (DirectionalProjectileMutationBase.cs:92)
    /// is reached on the first struck creature. Zero-damage behavior relies on
    /// DiceRoller's invalid-pattern fallthrough — see CalmMutation xml-doc.
    /// </summary>
    public class CalmMutationTests
    {
        [SetUp]
        public void Setup()
        {
            AsciiFxBus.Clear();
            MessageLog.Clear();
            FactionManager.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            FactionManager.Reset();
        }

        [Test]
        public void CalmMutation_AppliesNoFightGoalOnHit_WithBaseDurationAndLevel1()
        {
            // Level 1 → Duration = BaseDuration(40) + 1*10 = 50
            var zone = new Zone("CalmZone");
            var caster = CreateCaster();
            var target = CreateTargetWithBrain();

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 7, 5);

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new CalmMutation(), 1);
            var calm = mutations.GetMutation<CalmMutation>();

            bool cast = calm.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            Assert.IsTrue(cast, "Cast should succeed against a target in line.");
            var brain = target.GetPart<BrainPart>();
            Assert.IsNotNull(brain);
            Assert.IsTrue(brain.HasGoal<NoFightGoal>(),
                "Target should gain NoFightGoal after Calm hits.");

            var goal = brain.PeekGoal() as NoFightGoal;
            Assert.IsNotNull(goal, "NoFightGoal should be the top goal on the stack.");
            Assert.AreEqual(50, goal.Duration,
                "At Level=1, pacification lasts BaseDuration (40) + Level*10 = 50 turns.");
        }

        [Test]
        public void CalmMutation_LevelScalesDuration()
        {
            // Level 3 → Duration = 40 + 30 = 70.
            // Level 5 → Duration = 40 + 50 = 90.
            // Pins the linear scaling formula so future tuning stays intentional.
            foreach (var (level, expectedDuration) in new[] { (3, 70), (5, 90), (10, 140) })
            {
                var zone = new Zone("CalmZone.L" + level);
                var caster = CreateCaster();
                var target = CreateTargetWithBrain();

                zone.AddEntity(caster, 5, 5);
                zone.AddEntity(target, 7, 5);

                var mutations = caster.GetPart<MutationsPart>();
                mutations.AddMutation(new CalmMutation(), level);
                var calm = mutations.GetMutation<CalmMutation>();

                Assert.IsTrue(calm.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42)),
                    "Cast should succeed at level " + level);

                var brain = target.GetPart<BrainPart>();
                var goal = brain.PeekGoal() as NoFightGoal;
                Assert.IsNotNull(goal, "NoFightGoal missing at level " + level);
                Assert.AreEqual(expectedDuration, goal.Duration,
                    "Level " + level + " should produce duration " + expectedDuration + ".");
            }
        }

        [Test]
        public void CalmMutation_Idempotent_DoesNotStackOrExtendIfAlreadyPacified()
        {
            // If the target already has a NoFightGoal, a second Calm cast must
            // NOT push another goal and must NOT extend the existing duration.
            // Pinned to prevent accidental stacking regressions.
            var zone = new Zone("CalmZone.Idempotent");
            var caster = CreateCaster();
            var target = CreateTargetWithBrain();

            zone.AddEntity(caster, 5, 5);
            zone.AddEntity(target, 7, 5);

            // Pre-pacify with Duration=10 so we can detect extension.
            var brain = target.GetPart<BrainPart>();
            brain.PushGoal(new NoFightGoal(duration: 10, wander: false));
            int goalCountBefore = brain.GoalCount;

            var mutations = caster.GetPart<MutationsPart>();
            mutations.AddMutation(new CalmMutation(), 5);
            var calm = mutations.GetMutation<CalmMutation>();

            calm.Cast(zone, zone.GetCell(5, 5), 1, 0, new Random(42));

            Assert.AreEqual(goalCountBefore, brain.GoalCount,
                "Idempotent cast must not add a second NoFightGoal.");

            var goal = brain.FindGoal<NoFightGoal>();
            Assert.IsNotNull(goal, "Original NoFightGoal must still be present.");
            Assert.AreEqual(10, goal.Duration,
                "Existing pacification duration must not be replaced or extended by a second cast.");
        }

        // ===== Helpers =====

        private static Entity CreateCaster()
        {
            var entity = CreateCreatureBase("caster", hp: 20);
            entity.AddPart(new ActivatedAbilitiesPart());
            entity.AddPart(new MutationsPart());
            return entity;
        }

        private static Entity CreateTargetWithBrain()
        {
            // Target needs BrainPart because CalmMutation.ApplyOnHitEffect
            // pushes a goal onto it. Faction needs to differ from caster so
            // LineTargeting in DirectionalProjectileMutationBase.Cast treats
            // the target as a valid impact.
            var entity = CreateCreatureBase("target", hp: 20);
            entity.Tags["Faction"] = "Monsters";
            entity.AddPart(new BrainPart());
            return entity;
        }

        private static Entity CreateCreatureBase(string name, int hp)
        {
            var entity = new Entity { BlueprintName = name };
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Toughness"] = new Stat { Name = "Toughness", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 10, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = name });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = "1d2" });
            entity.AddPart(new ArmorPart());
            entity.AddPart(new InventoryPart { MaxWeight = 150 });
            return entity;
        }
    }
}
