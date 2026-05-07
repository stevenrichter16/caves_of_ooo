using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP7.1 — Tests for the 3 new elemental tree-roots (Cryomancy /
    /// Galvanism / Corrosion) plus 2 power skills (Pyromancy_Cinder /
    /// Spellcraft_Empower).
    ///
    /// <para>Pattern mirrors <see cref="Wsp7MagicSkillsTests"/>:
    /// per-skill positive + counter-checks (wrong element, wrong
    /// defender state, null defender), plus stacking sanity checks
    /// across trees.</para>
    /// </summary>
    public class Wsp71ElementalTreesTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
        }

        // ── Fixture helpers ─────────────────────────────────────────────

        private static Entity MakeCaster(string name = "caster")
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new SkillsPart());
            e.AddPart(new StatusEffectsPart());
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

        // ════════════════════════════════════════════════════════════════
        // CRYOMANCY — +Cold damage to Wet/Frozen targets
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Cryomancy_OnColdToWetTarget_ReturnsBonus()
        {
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new CryomancySkill(), source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new WetEffect(), null, null);

            int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                caster, target, "Cold", baseDamage: 12);
            Assert.AreEqual(3, bonus,
                "Cryomancy on Cold→Wet target with baseDamage=12 must return 12/4=3.");
        }

        [Test]
        public void Cryomancy_OnColdToFrozenTarget_ReturnsBonus()
        {
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new CryomancySkill(), source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new FrozenEffect(), null, null);

            int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                caster, target, "Cold", 12);
            Assert.Greater(bonus, 0,
                "Cryomancy must contribute on Cold→Frozen target.");
        }

        [Test]
        public void Cryomancy_OnNonColdElement_ReturnsZero()
        {
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new CryomancySkill(), source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new WetEffect(), null, null);

            string[] otherElements = new[] { "Heat", "Fire", "Electric", "Acid", "Light", "" };
            foreach (var elem in otherElements)
            {
                int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                    caster, target, elem, 12);
                Assert.AreEqual(0, bonus,
                    $"Cryomancy must NOT contribute on element '{elem}'.");
            }
        }

        [Test]
        public void Cryomancy_OnNonWetNonFrozenTarget_ReturnsZero()
        {
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new CryomancySkill(), source: "test");
            var target = MakeTarget();  // no Wet, no Frozen

            int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                caster, target, "Cold", 12);
            Assert.AreEqual(0, bonus,
                "Cryomancy must NOT contribute on a clean (non-Wet, non-Frozen) target.");
        }

        [Test]
        public void Cryomancy_AcceptsAllColdAliases()
        {
            // DamageAttributeFlags.Cold accepts "Cold", "Ice", "Freeze"
            // — Cryomancy must accept all three.
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new CryomancySkill(), source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new WetEffect(), null, null);

            string[] coldAliases = new[] { "Cold", "Ice", "Freeze" };
            foreach (var elem in coldAliases)
            {
                int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                    caster, target, elem, 12);
                Assert.Greater(bonus, 0,
                    $"Cryomancy must accept the '{elem}' element alias.");
            }
        }

        // ════════════════════════════════════════════════════════════════
        // GALVANISM — +Electric damage to Wet/Electrified targets
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Galvanism_OnElectricToWetTarget_ReturnsBonus()
        {
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new GalvanismSkill(), source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new WetEffect(), null, null);

            int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                caster, target, "Electric", 12);
            Assert.AreEqual(3, bonus,
                "Galvanism on Electric→Wet target with baseDamage=12 must return 12/4=3.");
        }

        [Test]
        public void Galvanism_OnElectricToElectrifiedTarget_ReturnsBonus()
        {
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new GalvanismSkill(), source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new ElectrifiedEffect(), null, null);

            int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                caster, target, "Electric", 12);
            Assert.Greater(bonus, 0,
                "Galvanism must contribute on Electric→Electrified target.");
        }

        [Test]
        public void Galvanism_OnNonElectricElement_ReturnsZero()
        {
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new GalvanismSkill(), source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new WetEffect(), null, null);

            string[] otherElements = new[] { "Heat", "Fire", "Cold", "Acid", "Light" };
            foreach (var elem in otherElements)
            {
                int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                    caster, target, elem, 12);
                Assert.AreEqual(0, bonus,
                    $"Galvanism must NOT contribute on element '{elem}'.");
            }
        }

        [Test]
        public void Galvanism_AcceptsLightningAlias()
        {
            // ArcBoltMutation passes "Electric" but a future spell might
            // pass "Lightning" or "Shock". Galvanism accepts all three.
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new GalvanismSkill(), source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new ElectrifiedEffect(), null, null);

            string[] aliases = new[] { "Electric", "Lightning", "Shock" };
            foreach (var elem in aliases)
            {
                int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                    caster, target, elem, 12);
                Assert.Greater(bonus, 0,
                    $"Galvanism must accept '{elem}' as an electric-element alias.");
            }
        }

        // ════════════════════════════════════════════════════════════════
        // CORROSION — +Acid damage to Acidic-stacked targets
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Corrosion_OnAcidToAcidicTarget_ReturnsBonus()
        {
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new CorrosionSkill(), source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new AcidicEffect(), null, null);

            int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                caster, target, "Acid", 12);
            Assert.AreEqual(3, bonus,
                "Corrosion on Acid→Acidic target with baseDamage=12 must return 12/4=3.");
        }

        [Test]
        public void Corrosion_OnNonAcidElement_ReturnsZero()
        {
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new CorrosionSkill(), source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new AcidicEffect(), null, null);

            string[] otherElements = new[] { "Heat", "Cold", "Electric", "Light" };
            foreach (var elem in otherElements)
            {
                int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                    caster, target, elem, 12);
                Assert.AreEqual(0, bonus,
                    $"Corrosion must NOT contribute on element '{elem}'.");
            }
        }

        [Test]
        public void Corrosion_OnNonAcidicTarget_ReturnsZero()
        {
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new CorrosionSkill(), source: "test");
            var target = MakeTarget();  // no AcidicEffect

            int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                caster, target, "Acid", 12);
            Assert.AreEqual(0, bonus,
                "Corrosion must NOT contribute on a non-Acidic target.");
        }

        // ════════════════════════════════════════════════════════════════
        // PYROMANCY_CINDER — +33% Heat damage to Charred targets
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Cinder_OnHeatToCharredTarget_ReturnsBonus()
        {
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new Pyromancy_Cinder(), source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new CharredEffect(), null, null);

            int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                caster, target, "Heat", 12);
            Assert.AreEqual(4, bonus,
                "Cinder on Heat→Charred target with baseDamage=12 must return 12/3=4.");
        }

        [Test]
        public void Cinder_OnNonCharredTarget_ReturnsZero()
        {
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new Pyromancy_Cinder(), source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new BurningEffect(), null, null);  // burning, NOT charred

            int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                caster, target, "Heat", 12);
            Assert.AreEqual(0, bonus,
                "Cinder must NOT fire on Burning-but-not-Charred targets " +
                "(that's PyromancySkill's job — Cinder is the post-Burning residue rewarder).");
        }

        [Test]
        public void Cinder_StacksAdditivelyWithPyromancyOnDoublyEffectedTarget()
        {
            // A target with BOTH Burning AND Charred (e.g., they got
            // burned, took some Charred residue, then got burned again
            // before the Charred wore off) should trigger BOTH skills.
            // Pyromancy: +12/4=3, Cinder: +12/3=4, total: +7.
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new PyromancySkill(), source: "test");
            caster.GetPart<SkillsPart>().AddSkill(new Pyromancy_Cinder(), source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new BurningEffect(), null, null);
            target.ApplyEffect(new CharredEffect(), null, null);

            int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                caster, target, "Heat", 12);
            Assert.AreEqual(7, bonus,
                "Pyromancy (+3 on Burning) + Cinder (+4 on Charred) on doubly-effected " +
                "target should sum to +7. If one fires but not the other, the gating logic " +
                "is wrong — both states are checked independently.");
        }

        // ════════════════════════════════════════════════════════════════
        // SPELLCRAFT_EMPOWER — universal +2 (stacks with Spellcraft root)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Empower_ReturnsBonus_OnAnyElement()
        {
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new Spellcraft_Empower(), source: "test");

            string[] elements = new[] { "Heat", "Cold", "Electric", "Acid", "Light", "" };
            foreach (var elem in elements)
            {
                int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                    caster, MakeTarget(), elem, 10);
                Assert.AreEqual(Spellcraft_Empower.EMPOWER_BONUS, bonus,
                    $"Empower must contribute its bonus on element '{elem}' (universal).");
            }
        }

        [Test]
        public void SpellcraftPlusEmpower_StackToFlatPlusThree()
        {
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new SpellcraftSkill(), source: "test");
            caster.GetPart<SkillsPart>().AddSkill(new Spellcraft_Empower(), source: "test");

            int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                caster, MakeTarget(), "Heat", 10);
            Assert.AreEqual(SpellcraftSkill.SPELL_DAMAGE_BONUS + Spellcraft_Empower.EMPOWER_BONUS, bonus,
                "Spellcraft (+1) + Empower (+2) must stack to +3 universally.");
        }

        // ════════════════════════════════════════════════════════════════
        // FULL-STACK STRESS TEST — 3 skills on 1 hyper-effect target
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void FullStackOnHotTarget_AllRelevantSkillsContribute()
        {
            // Caster owns: Spellcraft (+1 universal), PyromancySkill
            // (+3 on Burning, baseDamage=12), Pyromancy_Cinder (+4 on
            // Charred, baseDamage=12). Target has BOTH Burning AND
            // Charred. Heat spell with baseDamage=12 should get
            // +1 + 3 + 4 = +8.
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new SpellcraftSkill(), source: "test");
            caster.GetPart<SkillsPart>().AddSkill(new PyromancySkill(), source: "test");
            caster.GetPart<SkillsPart>().AddSkill(new Pyromancy_Cinder(), source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new BurningEffect(), null, null);
            target.ApplyEffect(new CharredEffect(), null, null);

            int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                caster, target, "Heat", 12);
            Assert.AreEqual(8, bonus,
                "Spellcraft+Pyromancy+Cinder on Burning+Charred target with baseDamage=12 " +
                "should give +1+3+4=+8 total. If less, one of the gates is wrong.");
        }

        [Test]
        public void CrossElementSkills_DoNotInterfere_OnSingleElementSpell()
        {
            // Caster owns ALL 4 elemental tree-roots (Pyromancy +
            // Cryomancy + Galvanism + Corrosion). Target is Burning
            // (only Pyromancy's gate matches). Heat spell with
            // baseDamage=12 should get only Pyromancy's +3 — the
            // other 3 elemental skills must NOT contribute.
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new PyromancySkill(), source: "test");
            caster.GetPart<SkillsPart>().AddSkill(new CryomancySkill(), source: "test");
            caster.GetPart<SkillsPart>().AddSkill(new GalvanismSkill(), source: "test");
            caster.GetPart<SkillsPart>().AddSkill(new CorrosionSkill(), source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new BurningEffect(), null, null);
            // Also wet — Cryomancy's gate matches on Wet, but only for
            // Cold spells. Should still NOT fire on Heat.
            target.ApplyEffect(new WetEffect(), null, null);

            int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                caster, target, "Heat", 12);
            Assert.AreEqual(3, bonus,
                "On a Heat spell, only Pyromancy should contribute (+3 on Burning). " +
                "Cryomancy (Wet matches but element is Cold), Galvanism (Wet matches " +
                "but element is Electric), and Corrosion (no gate match) must NOT fire. " +
                "Got bonus=" + bonus + ".");
        }

        // ════════════════════════════════════════════════════════════════
        // JSON CONTENT — all 5 new skills register
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Wsp71Skills_AllRegisteredInSkillRegistryFromJson()
        {
            SkillRegistry.EnsureInitialized();

            // Tree-roots (in _skillsByClass).
            string[] roots = new[] { "CryomancySkill", "GalvanismSkill", "CorrosionSkill" };
            foreach (var className in roots)
            {
                Assert.IsTrue(SkillRegistry.TryGetSkillByClass(className, out var data),
                    $"WSP7.1 tree-root '{className}' must register from JSON.");
                Assert.AreEqual(1, data.Cost,
                    $"'{className}' must cost 1 SP.");
                Assert.IsFalse(string.IsNullOrEmpty(data.Description),
                    $"'{className}' must have a non-empty Description.");
            }

            // Powers (in _powersByClass).
            string[] powers = new[] { "Pyromancy_Cinder", "Spellcraft_Empower" };
            foreach (var className in powers)
            {
                Assert.IsTrue(SkillRegistry.TryGetPowerByClass(className, out var power),
                    $"WSP7.1 power '{className}' must register from JSON. " +
                    $"If missing, the JSON edit didn't include the Powers entry.");
                Assert.AreEqual(1, power.Cost, $"'{className}' must cost 1 SP.");
                Assert.IsFalse(string.IsNullOrEmpty(power.Description));
            }
        }
    }
}
