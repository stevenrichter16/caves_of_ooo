using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP7.2 — Tests for 4 magic-melee crossover powers:
    /// Cryomancy_BrittleStrike, Galvanism_GroundStrike,
    /// Pyromancy_Charsplit, Corrosion_Etch.
    ///
    /// <para>Pattern: each is a passive that fires from
    /// <c>OnAttackerAfterAttack</c> when the defender has the
    /// corresponding elemental status effect (Frozen / Electrified /
    /// Charred / Acidic). Bonus damage is applied as a separate
    /// elemental damage call.</para>
    /// </summary>
    public class Wsp72MagicMeleeCrossoverTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
        }

        // ── Fixture helpers ─────────────────────────────────────────────

        private static Entity MakeAttacker(string name = "attacker")
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new SkillsPart());
            return e;
        }

        private static Entity MakeTarget(string name = "target", int hp = 100)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static SkillEventContext MakeMeleeContext(Entity attacker, Entity defender,
            int actualDamage = 10)
        {
            var damage = new Damage(actualDamage);
            damage.AddAttribute("Melee");
            return new SkillEventContext
            {
                Attacker = attacker, Defender = defender,
                Damage = damage, ActualDamage = actualDamage,
                Zone = null, Rng = new Random(0),
            };
        }

        // ════════════════════════════════════════════════════════════════
        // BrittleStrike — +50% melee damage on Frozen
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void BrittleStrike_OnFrozenTarget_DealsBonusDamage()
        {
            var attacker = MakeAttacker();
            var skill = new Cryomancy_BrittleStrike();
            attacker.GetPart<SkillsPart>().AddSkill(skill, source: "test");

            var target = MakeTarget();
            target.ApplyEffect(new FrozenEffect(), null, null);

            int hpBefore = target.GetStatValue("Hitpoints");
            skill.OnAttackerAfterAttack(MakeMeleeContext(attacker, target, actualDamage: 10));
            int hpAfter = target.GetStatValue("Hitpoints");

            // 10 * 50% = 5 bonus damage applied. HP delta should be 5
            // (the original 10 was already applied separately by the
            // combat pipeline; this test only invokes the skill's hook,
            // not the full PerformSingleAttack — so we just measure the
            // bonus damage from the skill).
            Assert.AreEqual(5, hpBefore - hpAfter,
                "BrittleStrike on Frozen target should deal 50% of actualDamage as bonus.");
        }

        [Test]
        public void BrittleStrike_OnNonFrozenTarget_NoBonusDamage()
        {
            var attacker = MakeAttacker();
            var skill = new Cryomancy_BrittleStrike();
            attacker.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var target = MakeTarget();
            // No FrozenEffect.

            int hpBefore = target.GetStatValue("Hitpoints");
            skill.OnAttackerAfterAttack(MakeMeleeContext(attacker, target));
            int hpAfter = target.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore, hpAfter,
                "BrittleStrike on non-Frozen target must NOT deal bonus damage.");
        }

        [Test]
        public void BrittleStrike_OnZeroDamageHit_NoOps()
        {
            // If the original swing didn't land any damage (resisted, etc.),
            // BrittleStrike must not fire — the bonus is a multiple of
            // 0, so HP delta is 0, but the skill should also not crash.
            var attacker = MakeAttacker();
            var skill = new Cryomancy_BrittleStrike();
            attacker.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new FrozenEffect(), null, null);

            int hpBefore = target.GetStatValue("Hitpoints");
            skill.OnAttackerAfterAttack(MakeMeleeContext(attacker, target, actualDamage: 0));
            Assert.AreEqual(hpBefore, target.GetStatValue("Hitpoints"),
                "Zero-actualDamage swing must not fire BrittleStrike.");
        }

        [Test]
        public void BrittleStrike_NullDefender_NoCrash()
        {
            var skill = new Cryomancy_BrittleStrike();
            var ctx = new SkillEventContext
            {
                Attacker = MakeAttacker(), Defender = null,
                Damage = new Damage(10), ActualDamage = 10, Rng = new Random(0),
            };
            Assert.DoesNotThrow(() => skill.OnAttackerAfterAttack(ctx),
                "BrittleStrike with null Defender must not crash.");
        }

        // ════════════════════════════════════════════════════════════════
        // GroundStrike — +50% melee damage on Electrified
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void GroundStrike_OnElectrifiedTarget_DealsBonusDamage()
        {
            var attacker = MakeAttacker();
            var skill = new Galvanism_GroundStrike();
            attacker.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new ElectrifiedEffect(), null, null);

            int hpBefore = target.GetStatValue("Hitpoints");
            skill.OnAttackerAfterAttack(MakeMeleeContext(attacker, target, actualDamage: 10));
            Assert.AreEqual(5, hpBefore - target.GetStatValue("Hitpoints"),
                "GroundStrike on Electrified target should deal 50% of actualDamage as bonus.");
        }

        [Test]
        public void GroundStrike_OnNonElectrifiedTarget_NoBonusDamage()
        {
            var attacker = MakeAttacker();
            var skill = new Galvanism_GroundStrike();
            attacker.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var target = MakeTarget();

            int hpBefore = target.GetStatValue("Hitpoints");
            skill.OnAttackerAfterAttack(MakeMeleeContext(attacker, target));
            Assert.AreEqual(hpBefore, target.GetStatValue("Hitpoints"),
                "GroundStrike on non-Electrified target must NOT deal bonus damage.");
        }

        // ════════════════════════════════════════════════════════════════
        // Charsplit — +50% melee damage on Charred
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Charsplit_OnCharredTarget_DealsBonusDamage()
        {
            var attacker = MakeAttacker();
            var skill = new Pyromancy_Charsplit();
            attacker.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new CharredEffect(), null, null);

            int hpBefore = target.GetStatValue("Hitpoints");
            skill.OnAttackerAfterAttack(MakeMeleeContext(attacker, target, actualDamage: 10));
            Assert.AreEqual(5, hpBefore - target.GetStatValue("Hitpoints"),
                "Charsplit on Charred target should deal 50% of actualDamage as bonus.");
        }

        [Test]
        public void Charsplit_OnBurningButNotCharred_NoBonus()
        {
            // Counter-check: Burning is NOT Charred. Charsplit is the
            // Charred-specific power; the Burning state is rewarded by
            // PyromancySkill (the spell-damage tree-root), not by
            // Charsplit (the melee-damage power).
            var attacker = MakeAttacker();
            var skill = new Pyromancy_Charsplit();
            attacker.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new BurningEffect(), null, null);  // Burning, not Charred

            int hpBefore = target.GetStatValue("Hitpoints");
            skill.OnAttackerAfterAttack(MakeMeleeContext(attacker, target));
            Assert.AreEqual(hpBefore, target.GetStatValue("Hitpoints"),
                "Charsplit must NOT fire on Burning-but-not-Charred targets.");
        }

        // ════════════════════════════════════════════════════════════════
        // Etch — +50% melee damage on Acidic
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Etch_OnAcidicTarget_DealsBonusDamage()
        {
            var attacker = MakeAttacker();
            var skill = new Corrosion_Etch();
            attacker.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new AcidicEffect(), null, null);

            int hpBefore = target.GetStatValue("Hitpoints");
            skill.OnAttackerAfterAttack(MakeMeleeContext(attacker, target, actualDamage: 10));
            Assert.AreEqual(5, hpBefore - target.GetStatValue("Hitpoints"),
                "Etch on Acidic target should deal 50% of actualDamage as bonus.");
        }

        [Test]
        public void Etch_OnNonAcidicTarget_NoBonusDamage()
        {
            var attacker = MakeAttacker();
            var skill = new Corrosion_Etch();
            attacker.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var target = MakeTarget();

            int hpBefore = target.GetStatValue("Hitpoints");
            skill.OnAttackerAfterAttack(MakeMeleeContext(attacker, target));
            Assert.AreEqual(hpBefore, target.GetStatValue("Hitpoints"),
                "Etch on non-Acidic target must NOT deal bonus damage.");
        }

        // ════════════════════════════════════════════════════════════════
        // CROSS-SKILL: 4 crossover skills on a hyper-effected target
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void AllFourCrossovers_OnHyperEffectedTarget_AllFireIndependently()
        {
            // Target with Frozen + Electrified + Charred + Acidic. Owner
            // has all 4 crossover powers. Each should fire its bonus
            // independently. Total bonus = 4 × 50% × 10 = 20 HP.
            var attacker = MakeAttacker();
            attacker.GetPart<SkillsPart>().AddSkill(new Cryomancy_BrittleStrike(), source: "test");
            attacker.GetPart<SkillsPart>().AddSkill(new Galvanism_GroundStrike(), source: "test");
            attacker.GetPart<SkillsPart>().AddSkill(new Pyromancy_Charsplit(), source: "test");
            attacker.GetPart<SkillsPart>().AddSkill(new Corrosion_Etch(), source: "test");

            var target = MakeTarget(hp: 200);
            target.ApplyEffect(new FrozenEffect(), null, null);
            target.ApplyEffect(new ElectrifiedEffect(), null, null);
            target.ApplyEffect(new CharredEffect(), null, null);
            target.ApplyEffect(new AcidicEffect(), null, null);

            int hpBefore = target.GetStatValue("Hitpoints");
            // Fire all 4 hooks via the dispatcher (the real path
            // CombatSystem uses to invoke OnAttackerAfterAttack).
            SkillEventDispatcher.AttackerAfterAttack(attacker,
                MakeMeleeContext(attacker, target, actualDamage: 10));
            int totalBonus = hpBefore - target.GetStatValue("Hitpoints");

            Assert.AreEqual(20, totalBonus,
                "All 4 crossover skills firing on a hyper-effected target should sum to 4×5=20 bonus HP. " +
                $"Got {totalBonus}. If less, one of the skills isn't firing or is double-counting.");
        }

        [Test]
        public void OneCrossoverSkill_DoesNotFireOnWrongState()
        {
            // Counter-check pin: BrittleStrike (Frozen-gated) on a
            // target who has Electrified but NOT Frozen — should NOT
            // fire even with the WSP6 dispatcher running.
            var attacker = MakeAttacker();
            attacker.GetPart<SkillsPart>().AddSkill(new Cryomancy_BrittleStrike(), source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new ElectrifiedEffect(), null, null);  // wrong state for Brittle

            int hpBefore = target.GetStatValue("Hitpoints");
            SkillEventDispatcher.AttackerAfterAttack(attacker,
                MakeMeleeContext(attacker, target, actualDamage: 10));
            Assert.AreEqual(hpBefore, target.GetStatValue("Hitpoints"),
                "BrittleStrike must NOT fire on an Electrified-but-not-Frozen target.");
        }

        // ════════════════════════════════════════════════════════════════
        // JSON CONTENT — all 4 powers register
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Wsp72CrossoverPowers_AllRegisteredInSkillRegistryFromJson()
        {
            SkillRegistry.EnsureInitialized();
            string[] powers = new[]
            {
                "Cryomancy_BrittleStrike",
                "Galvanism_GroundStrike",
                "Pyromancy_Charsplit",
                "Corrosion_Etch",
            };
            foreach (var className in powers)
            {
                Assert.IsTrue(SkillRegistry.TryGetPowerByClass(className, out var power),
                    $"WSP7.2 power '{className}' must register from JSON.");
                Assert.AreEqual(1, power.Cost);
                Assert.IsFalse(string.IsNullOrEmpty(power.Description));
            }
        }
    }
}
