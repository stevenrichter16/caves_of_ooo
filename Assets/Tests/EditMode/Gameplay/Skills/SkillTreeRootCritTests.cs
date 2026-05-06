using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP4.4 — Tree-root crit-hook tests for LongBladesSkill and
    /// ShortBladesSkill. Closes the cold-eye 🧪 #8 gap (the WSP3.4 /
    /// WSP4.0 fixtures covered Cudgel + Axe tree-root crits but
    /// skipped LongBlades + ShortBlades).
    ///
    /// <para>Also includes WSP4.5 defense-in-depth Critical-gate
    /// counter-checks (Cudgel + Axe non-Critical hits don't fire) and
    /// WSP4.4 🧪 #9 Cudgel_Hammer Body-but-no-equipped no-op coverage.
    /// LongBlades_Expertise tests moved to SkillSystemTier2Tests.cs in
    /// WSP5.1 alongside the other 3 Expertise tests (cold-eye Finding 5).</para>
    /// </summary>
    public class SkillTreeRootCritTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
        }

        // ── Fixture helpers ─────────────────────────────────────────────

        private static Entity MakeFighter()
        {
            var e = new Entity { ID = "fighter" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = 100, Min = 0, Max = 100 };
            e.AddPart(new RenderPart { DisplayName = "fighter" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static Entity MakeAttackerWithSkill(BaseSkillPart skill)
        {
            var e = new Entity { ID = "attacker" };
            e.AddPart(new RenderPart { DisplayName = "attacker" });
            e.AddPart(new SkillsPart());
            Assert.IsTrue(e.GetPart<SkillsPart>().AddSkill(skill, source: "test"));
            return e;
        }

        private static MeleeWeaponPart MakeWeapon(string attributes)
        {
            var weaponEntity = new Entity { ID = "weapon", BlueprintName = "TestWeapon" };
            weaponEntity.AddPart(new RenderPart { DisplayName = "test weapon" });
            var w = new MeleeWeaponPart
            {
                BaseDamage = "1d6",
                HitBonus = 0,
                PenBonus = 0,
                MaxStrengthBonus = 3,
                Attributes = attributes,
            };
            weaponEntity.AddPart(w);
            return w;
        }

        private static SkillEventContext MakeCritHitContext(Entity attacker,
            Entity defender, int seed, params string[] damageAttrs)
        {
            var damage = new Damage(10);
            foreach (var a in damageAttrs) damage.AddAttribute(a);
            damage.AddAttribute("Critical");
            return new SkillEventContext
            {
                Attacker = attacker, Defender = defender,
                Damage = damage, ActualDamage = 10,
                Zone = null, Rng = new Random(seed),
            };
        }

        // ════════════════════════════════════════════════════════════════
        // CudgelSkill + AxeSkill: defense-in-depth Critical-attribute gate
        // (parallel to the LongBlades + ShortBlades counter-checks below)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void CudgelSkillCrit_OnNonCriticalCudgel_DoesNotStun()
        {
            // Defense-in-depth: even calling OnWeaponMadeCriticalHit
            // directly with non-Critical damage must bail. The skill's
            // own gate prevents leakage if the dispatcher ever
            // accidentally fires WeaponMadeCriticalHit without a real crit.
            for (int seed = 0; seed < 50; seed++)
            {
                var skill = new CudgelSkill();
                var actor = MakeAttackerWithSkill(skill);
                var defender = MakeFighter();
                var damage = new Damage(10);
                damage.AddAttribute("Cudgel");
                // NO "Critical" attribute.
                skill.OnWeaponMadeCriticalHit(new SkillEventContext
                {
                    Attacker = actor, Defender = defender,
                    Damage = damage, ActualDamage = 10,
                    Zone = null, Rng = new Random(seed),
                });
                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<StunnedEffect>(),
                    $"Seed {seed}: CudgelSkill.OnWeaponMadeCriticalHit must bail when " +
                    $"the damage isn't Critical (defense-in-depth gate, WSP4.4).");
            }
        }

        [Test]
        public void AxeSkillCrit_OnNonCriticalAxe_DoesNotCleave()
        {
            // Defense-in-depth for the Axe tree-root cleave hook.
            // Without a Zone, ExecuteCleave bails anyway, so we set up
            // a minimal Zone with a viable cleave target and verify
            // it's NOT damaged when Critical attribute is absent.
            for (int seed = 0; seed < 50; seed++)
            {
                var skill = new AxeSkill();
                var actor = MakeAttackerWithSkill(skill);
                var defender = MakeFighter();
                var cleaveTarget = MakeFighter();
                cleaveTarget.ID = "cleave_target";
                cleaveTarget.Statistics["Hitpoints"].BaseValue = 50;

                var zone = new Zone();
                zone.AddEntity(actor, 5, 5);
                zone.AddEntity(defender, 6, 5);
                zone.AddEntity(cleaveTarget, 7, 5);
                int hpBefore = cleaveTarget.GetStatValue("Hitpoints");

                var damage = new Damage(10);
                damage.AddAttribute("Axe");
                damage.AddAttribute("Cutting");
                // NO "Critical".
                skill.OnWeaponMadeCriticalHit(new SkillEventContext
                {
                    Attacker = actor, Defender = defender,
                    Damage = damage, ActualDamage = 10,
                    Zone = zone, Rng = new Random(seed),
                });
                Assert.AreEqual(hpBefore, cleaveTarget.GetStatValue("Hitpoints"),
                    $"Seed {seed}: AxeSkill.OnWeaponMadeCriticalHit must bail when " +
                    $"the damage isn't Critical — cleave-target HP must not change.");
            }
        }

        // ════════════════════════════════════════════════════════════════
        // LongBladesSkill: force-Bleed on Critical LongBlades hit
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void LongBladesSkillCrit_OnLongBladesCritical_AppliesBleeding()
        {
            // Acceptance: across many seeds, every Critical+LongBlades hit
            // by an actor owning LongBladesSkill produces a Bleeding effect.
            // (Force-applied — no chance roll.)
            for (int seed = 0; seed < 50; seed++)
            {
                var skill = new LongBladesSkill();
                var actor = MakeAttackerWithSkill(skill);
                var defender = MakeFighter();
                skill.OnWeaponMadeCriticalHit(
                    MakeCritHitContext(actor, defender, seed, "Cutting", "LongBlades"));
                Assert.IsTrue(defender.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>(),
                    $"Seed {seed}: LongBladesSkill crit on Critical+LongBlades hit MUST apply Bleeding.");
            }
        }

        [Test]
        public void LongBladesSkillCrit_OnNonLongBladesCritical_DoesNotBleed()
        {
            // Counter-check: Critical hit but NOT LongBlades attribute (e.g.
            // Critical+Cutting+Axe). LongBladesSkill must not fire — gates
            // on the LongBlades sub-class, not the broader Cutting class.
            for (int seed = 0; seed < 100; seed++)
            {
                var skill = new LongBladesSkill();
                var actor = MakeAttackerWithSkill(skill);
                var defender = MakeFighter();
                skill.OnWeaponMadeCriticalHit(
                    MakeCritHitContext(actor, defender, seed, "Cutting", "Axe"));
                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>(),
                    $"Seed {seed}: Critical+Axe hit must NOT trigger LongBladesSkill — " +
                    $"the gate is the LongBlades attribute, not Cutting.");
            }
        }

        [Test]
        public void LongBladesSkillCrit_OnNonCriticalLongBlades_DoesNotBleed()
        {
            // Counter-check: LongBlades hit without Critical attribute.
            // Tree-root hooks fire only on critical hits.
            for (int seed = 0; seed < 100; seed++)
            {
                var skill = new LongBladesSkill();
                var actor = MakeAttackerWithSkill(skill);
                var defender = MakeFighter();
                var damage = new Damage(10);
                damage.AddAttribute("Cutting");
                damage.AddAttribute("LongBlades");
                // NO "Critical" attribute.
                skill.OnWeaponMadeCriticalHit(new SkillEventContext
                {
                    Attacker = actor, Defender = defender,
                    Damage = damage, ActualDamage = 10,
                    Zone = null, Rng = new Random(seed),
                });
                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>(),
                    $"Seed {seed}: LongBlades hit without Critical attribute must not " +
                    $"fire the tree-root crit hook.");
            }
        }

        // ════════════════════════════════════════════════════════════════
        // ShortBladesSkill: force-Bleed on Critical Piercing hit
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void ShortBladesSkillCrit_OnPiercingCritical_AppliesBleeding()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var skill = new ShortBladesSkill();
                var actor = MakeAttackerWithSkill(skill);
                var defender = MakeFighter();
                skill.OnWeaponMadeCriticalHit(
                    MakeCritHitContext(actor, defender, seed, "Piercing"));
                Assert.IsTrue(defender.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>(),
                    $"Seed {seed}: ShortBladesSkill crit on Critical+Piercing hit MUST apply Bleeding.");
            }
        }

        [Test]
        public void ShortBladesSkillCrit_OnNonPiercingCritical_DoesNotBleed()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var skill = new ShortBladesSkill();
                var actor = MakeAttackerWithSkill(skill);
                var defender = MakeFighter();
                skill.OnWeaponMadeCriticalHit(
                    MakeCritHitContext(actor, defender, seed, "Bludgeoning", "Cudgel"));
                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>(),
                    $"Seed {seed}: Critical+Cudgel hit must NOT trigger ShortBladesSkill " +
                    $"(gate is Piercing).");
            }
        }

        [Test]
        public void ShortBladesSkillCrit_OnNonCriticalPiercing_DoesNotBleed()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                var skill = new ShortBladesSkill();
                var actor = MakeAttackerWithSkill(skill);
                var defender = MakeFighter();
                var damage = new Damage(10);
                damage.AddAttribute("Piercing");
                // NO "Critical".
                skill.OnWeaponMadeCriticalHit(new SkillEventContext
                {
                    Attacker = actor, Defender = defender,
                    Damage = damage, ActualDamage = 10,
                    Zone = null, Rng = new Random(seed),
                });
                Assert.IsFalse(defender.GetPart<StatusEffectsPart>().HasEffect<BleedingEffect>(),
                    $"Seed {seed}: Piercing hit without Critical must not fire tree-root.");
            }
        }

        // (LongBlades_Expertise tests moved to SkillSystemTier2Tests.cs
        // alongside the other 3 Expertise tests in WSP5.1 — see cold-eye
        // Finding 5. The +to-hit Expertise group lives in the Tier-2
        // passives fixture, not the tree-root crit-hooks fixture.)

        // ════════════════════════════════════════════════════════════════
        // Cold-eye 🧪 #9 — Cudgel_Hammer Body-but-no-equipped distinct path
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Hammer_DefenderWithBodyButNoEquippedItems_NoOps()
        {
            // Cold-eye finding 🧪 #9 split — the existing
            // Hammer_NoEquippedItems_NoOps covers the "no Body" early-out.
            // This test covers the "Body present but candidates list empty"
            // branch where ForeachEquippedObject yields nothing. Must not
            // crash + must not apply Broken to any phantom item.
            var skill = new Cudgel_Hammer();
            var actor = MakeAttackerWithSkill(skill);
            for (int seed = 0; seed < 50; seed++)
            {
                // Bodied defender with no equipped items.
                var defender = new Entity { ID = "bodied_defender" };
                defender.Tags["Creature"] = "";
                defender.AddPart(new RenderPart { DisplayName = "bodied_defender" });
                defender.AddPart(new InventoryPart { MaxWeight = 100 });
                defender.AddPart(new StatusEffectsPart());
                var body = new Body();
                defender.AddPart(body);
                body.SetBody(CavesOfOoo.Core.Anatomy.AnatomyFactory.CreateHumanoid());
                // No EquipToBodyPart calls — every hand stays empty.

                var damage = new Damage(10);
                damage.AddAttribute("Cudgel");
                var ctx = new SkillEventContext
                {
                    Attacker = actor, Defender = defender,
                    Damage = damage, ActualDamage = 10,
                    Zone = null, Rng = new Random(seed),
                };
                Assert.DoesNotThrow(() => skill.OnAttackerAfterAttack(ctx),
                    $"Seed {seed}: Body-with-no-equipment defender must not throw.");
            }
        }
    }
}
