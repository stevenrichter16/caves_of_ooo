using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP4.4 — Tree-root crit-hook tests for LongBladesSkill,
    /// ShortBladesSkill, and LongBlades_Expertise. Closes the cold-eye
    /// 🧪 #8 gap (the WSP3.4 / WSP4.0 fixtures covered Cudgel + Axe
    /// tree-root crits but skipped LongBlades + ShortBlades) and
    /// covers the new LongBlades_Expertise added in WSP4.4 (🔵 #5).
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

        // ════════════════════════════════════════════════════════════════
        // LongBlades_Expertise: +2 to-hit when wielding LongBlades
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void LongBladesExpertise_WithLongBladesWeapon_AddsHitBonus()
        {
            var actor = MakeAttackerWithSkill(new LongBlades_Expertise());
            var weapon = MakeWeapon("Cutting LongBlades");
            Assert.AreEqual(LongBlades_Expertise.HIT_BONUS,
                SkillEventDispatcher.GetSkillHitModifier(actor, weapon),
                "LongBlades_Expertise must contribute HIT_BONUS when weapon has LongBlades attribute.");
        }

        [Test]
        public void LongBladesExpertise_WithNonLongBladesWeapon_NoBonus()
        {
            var actor = MakeAttackerWithSkill(new LongBlades_Expertise());
            var weapon = MakeWeapon("Cutting Axe");
            Assert.AreEqual(0,
                SkillEventDispatcher.GetSkillHitModifier(actor, weapon),
                "LongBlades_Expertise must NOT contribute when weapon is Axe-class (not LongBlades).");
        }

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
