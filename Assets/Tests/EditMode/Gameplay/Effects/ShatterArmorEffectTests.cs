using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP2.1 — ShatterArmorEffect contract tests + GetAV integration:
    ///   - OnApply / OnRemove (no stat modification — AV is computed)
    ///   - OnStack accumulates StackCount + extends Duration
    ///   - CombatSystem.GetAV reads ShatterArmorEffect and subtracts
    ///     AV_REDUCTION * StackCount, clamped to 0
    /// </summary>
    public class ShatterArmorEffectTests
    {
        [SetUp] public void Setup() => MessageLog.Clear();

        private static Entity MakeArmoredFighter(int avRating)
        {
            var e = new Entity { ID = "fighter" };
            e.AddPart(new RenderPart { DisplayName = "fighter" });
            e.AddPart(new StatusEffectsPart());
            // Synthetic natural-armor AV via a Body-less ArmorPart for test simplicity.
            e.AddPart(new ArmorPart { AV = avRating, DV = 0 });
            return e;
        }

        [Test]
        public void GetAV_WithNoShatter_ReturnsBaseArmor()
        {
            var target = MakeArmoredFighter(avRating: 6);
            Assert.AreEqual(6, CombatSystem.GetAV(target),
                "Without ShatterArmorEffect, GetAV should return the natural-armor AV.");
        }

        [Test]
        public void GetAV_WithSingleShatter_SubtractsReduction()
        {
            var target = MakeArmoredFighter(avRating: 6);
            target.ApplyEffect(new ShatterArmorEffect(4), source: null, zone: null);
            Assert.AreEqual(6 - ShatterArmorEffect.AV_REDUCTION,
                CombatSystem.GetAV(target),
                "With one ShatterArmorEffect stack, GetAV should subtract AV_REDUCTION (2).");
        }

        [Test]
        public void GetAV_WithMultipleShatterStacks_SubtractsScaled()
        {
            // Apply ShatterArmor twice — stack count becomes 2, reduction = 4.
            var target = MakeArmoredFighter(avRating: 6);
            target.ApplyEffect(new ShatterArmorEffect(4), source: null, zone: null);
            target.ApplyEffect(new ShatterArmorEffect(4), source: null, zone: null);
            Assert.AreEqual(6 - 2 * ShatterArmorEffect.AV_REDUCTION,
                CombatSystem.GetAV(target),
                "Two stacks should subtract AV_REDUCTION * 2 (4 total).");
        }

        [Test]
        public void GetAV_ClampsToZero_NotNegative()
        {
            var target = MakeArmoredFighter(avRating: 1);
            // Apply many stacks — far more reduction than AV.
            for (int i = 0; i < 5; i++)
                target.ApplyEffect(new ShatterArmorEffect(4), source: null, zone: null);
            Assert.AreEqual(0, CombatSystem.GetAV(target),
                "GetAV must clamp to 0 — armor can be shattered to nothing but not negative.");
        }

        [Test]
        public void PerformSingleAttack_ShatteredBodyHavingDefender_TakesMoreDamage_ThanUnshattered_SameSeed()
        {
            // Docs/COMBAT-AUDIT-BUGFIX-PLAN-2026-07.md SM5/A3. Every test
            // above calls CombatSystem.GetAV(target) directly against a
            // Body-LESS fixture -- but PerformSingleAttack's real AV-selection
            // line (`hitPart != null ? GetPartAV(defender, hitPart) : GetAV(defender)`)
            // picks GetPartAV for any Body-having defender, which is
            // virtually all real combat (SelectHitLocation returns non-null
            // for any normal AnatomyFactory body). Pre-fix, GetPartAV never
            // read ShatterArmorEffect at all -- Cudgel_ShatteringBlows procs,
            // the UI shows the effect active, and the very next attack's
            // penetration roll was computed against the UNSHATTERED AV. This
            // is the first test in the whole codebase to exercise the
            // interaction through the real PerformSingleAttack path rather
            // than calling GetAV directly.
            var zoneShattered = new Zone("ShatterAV.Shattered");
            var attackerShattered = MakeAttacker();
            var weaponShattered = MakeWeapon("2d6");
            var defenderShattered = MakeBodyHavingDefender(av: 10);
            for (int i = 0; i < 3; i++) // 3 stacks -> AV 10 - 3*2 = 4
                defenderShattered.ApplyEffect(new ShatterArmorEffect(10), source: null, zone: null);
            zoneShattered.AddEntity(attackerShattered, 5, 5);
            zoneShattered.AddEntity(defenderShattered, 6, 5);

            int hpBeforeShattered = defenderShattered.GetStatValue("Hitpoints", 1000);
            CombatSystem.PerformSingleAttack(attackerShattered, defenderShattered, weaponShattered,
                true, zoneShattered, new Random(1));
            int damageShattered = hpBeforeShattered - defenderShattered.GetStatValue("Hitpoints", 1000);

            var zoneUnshattered = new Zone("ShatterAV.Unshattered");
            var attackerUnshattered = MakeAttacker();
            var weaponUnshattered = MakeWeapon("2d6");
            var defenderUnshattered = MakeBodyHavingDefender(av: 10);
            zoneUnshattered.AddEntity(attackerUnshattered, 5, 5);
            zoneUnshattered.AddEntity(defenderUnshattered, 6, 5);

            int hpBeforeUnshattered = defenderUnshattered.GetStatValue("Hitpoints", 1000);
            CombatSystem.PerformSingleAttack(attackerUnshattered, defenderUnshattered, weaponUnshattered,
                true, zoneUnshattered, new Random(1));
            int damageUnshattered = hpBeforeUnshattered - defenderUnshattered.GetStatValue("Hitpoints", 1000);

            Assert.Greater(damageShattered, damageUnshattered,
                "A Body-having defender with 3 ShatterArmorEffect stacks (AV 10 -> 4) must take "
                + "strictly more damage than an identical unshattered defender (AV 10), at the same "
                + "seed -- proving GetPartAV genuinely reads the Shatter reduction through the real "
                + "PerformSingleAttack path, not just when GetAV is called directly.");
        }

        [Test]
        public void OnStack_ExtendsDuration_AndIncrementsCount()
        {
            var target = MakeArmoredFighter(avRating: 6);
            target.ApplyEffect(new ShatterArmorEffect(4), source: null, zone: null);
            target.ApplyEffect(new ShatterArmorEffect(4), source: null, zone: null);
            var shatter = target.GetPart<StatusEffectsPart>().GetEffect<ShatterArmorEffect>();
            Assert.IsNotNull(shatter);
            Assert.AreEqual(8, shatter.Duration, "Duration should be 4 + 4 = 8 after stack.");
            Assert.AreEqual(2, shatter.StackCount, "StackCount should be 1 + 1 = 2.");
        }

        // ===== Helpers for the PerformSingleAttack-level test =====

        private static Entity MakeAttacker()
        {
            var entity = new Entity();
            entity.BlueprintName = "TestAttacker";
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Owner = entity, Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            entity.Statistics["Strength"] = new Stat { Owner = entity, Name = "Strength", BaseValue = 20, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Owner = entity, Name = "Agility", BaseValue = 20, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Owner = entity, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = "attacker" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new ArmorPart());
            return entity;
        }

        private static MeleeWeaponPart MakeWeapon(string damage)
        {
            // HitBonus=30 removes to-hit variance -- isolates the
            // penetration/AV comparison this test is actually probing.
            return new MeleeWeaponPart { BaseDamage = damage, PenBonus = 5, HitBonus = 30 };
        }

        private static Entity MakeBodyHavingDefender(int av)
        {
            var entity = new Entity();
            entity.BlueprintName = "TestDefender";
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Owner = entity, Name = "Hitpoints", BaseValue = 1000, Min = 0, Max = 1000 };
            entity.Statistics["Strength"] = new Stat { Owner = entity, Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Owner = entity, Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Owner = entity, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = "defender" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new ArmorPart { AV = av, DV = 0 });
            entity.AddPart(new InventoryPart { MaxWeight = 150 });
            entity.AddPart(new StatusEffectsPart());
            var body = new Body();
            entity.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());
            return entity;
        }
    }
}
