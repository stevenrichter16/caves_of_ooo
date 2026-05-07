using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP7.0 — Magic & grimoire skill tests. Covers:
    /// <list type="bullet">
    ///   <item>The new <c>OnGetSpellDamageModifier</c> hook +
    ///         dispatcher.</item>
    ///   <item>The new <c>MutationDamageHelpers.ApplySpellDamage</c>
    ///         shared damage path.</item>
    ///   <item>SpellcraftSkill (universal +1 spell damage).</item>
    ///   <item>PyromancySkill (+25% Heat damage to Burning targets).</item>
    ///   <item>The fact that mutations now actually tag damage with
    ///         element + Spell attributes (so resistances fire too —
    ///         pre-WSP7 this was silently broken).</item>
    /// </list>
    /// </summary>
    public class Wsp7MagicSkillsTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
        }

        // ── Fixture helpers ─────────────────────────────────────────────

        private static Entity MakeCaster(string name = "caster", int hp = 200)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Strength"] = new Stat
                { Owner = e, Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
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
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        // ════════════════════════════════════════════════════════════════
        // Dispatcher unit pin — null guards + sum semantics
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void GetSpellDamageModifier_NullAttacker_ReturnsZero()
        {
            int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                attacker: null, defender: MakeTarget(),
                elementAttribute: "Heat", baseDamage: 10);
            Assert.AreEqual(0, bonus,
                "GetSpellDamageModifier must return 0 on null attacker (defense-in-depth).");
        }

        [Test]
        public void GetSpellDamageModifier_AttackerWithoutSkillsPart_ReturnsZero()
        {
            var bare = new Entity { ID = "bare" };
            bare.AddPart(new RenderPart { DisplayName = "bare" });
            int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                bare, MakeTarget(), "Heat", 10);
            Assert.AreEqual(0, bonus,
                "Actors without SkillsPart contribute 0 (matches the GetSkillHitModifier shape).");
        }

        [Test]
        public void GetSpellDamageModifier_SumsAcrossOwnedSkills()
        {
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new SpellcraftSkill(), source: "test");
            // Add a second stub that returns +5 unconditionally to verify
            // the dispatcher SUMS across owned skills (not picks the max).
            caster.GetPart<SkillsPart>().AddSkill(new TestPlusFiveStub(), source: "test");

            int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                caster, MakeTarget(), "Fire", 10);
            // Spellcraft = +1, stub = +5, sum = +6.
            Assert.AreEqual(SpellcraftSkill.SPELL_DAMAGE_BONUS + 5, bonus,
                "Dispatcher must SUM contributions from all owned magic skills.");
        }

        public class TestPlusFiveStub : BaseSkillPart
        {
            public override int OnGetSpellDamageModifier(Entity attacker,
                Entity defender, string elementAttribute, int baseDamage) => 5;
        }

        // ════════════════════════════════════════════════════════════════
        // Spellcraft — universal +1 spell damage (any element)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Spellcraft_ReturnsBonus_OnAnyElement()
        {
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new SpellcraftSkill(), source: "test");
            string[] elements = new[] { "Heat", "Fire", "Cold", "Electric", "Acid", "Light", "" };
            foreach (var elem in elements)
            {
                int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                    caster, MakeTarget(), elem, baseDamage: 10);
                Assert.AreEqual(SpellcraftSkill.SPELL_DAMAGE_BONUS, bonus,
                    $"Spellcraft must contribute the bonus on element '{elem}' (universal). " +
                    $"Element-gated skills shouldn't exist on Spellcraft.");
            }
        }

        // ════════════════════════════════════════════════════════════════
        // Pyromancy — +25% Heat damage to Burning targets only
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Pyromancy_OnHeatToBurningTarget_ReturnsBonus()
        {
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new PyromancySkill(), source: "test");

            var target = MakeTarget();
            target.ApplyEffect(new BurningEffect(intensity: 1.0f), source: caster, zone: null);

            int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                caster, target, "Heat", baseDamage: 12);
            // 12 / 4 = 3, floor 1 = 3.
            Assert.AreEqual(3, bonus,
                "Pyromancy on Heat→Burning target with baseDamage=12 must return 12/4=3.");
        }

        [Test]
        public void Pyromancy_AcceptsBothHeatAndFireElementStrings()
        {
            // The DamageAttributeFlags.Heat alias accepts both "Heat" and
            // "Fire" — Pyromancy must accept both element strings to be
            // mutation-source-agnostic. Some mutations pass "Fire", some
            // pass "Heat"; both should trigger the bonus.
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new PyromancySkill(), source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new BurningEffect(), source: caster, zone: null);

            int heatBonus = SkillEventDispatcher.GetSpellDamageModifier(
                caster, target, "Heat", 12);
            int fireBonus = SkillEventDispatcher.GetSpellDamageModifier(
                caster, target, "Fire", 12);
            Assert.AreEqual(heatBonus, fireBonus,
                "Pyromancy must accept both 'Heat' and 'Fire' elementAttribute strings.");
            Assert.Greater(heatBonus, 0, "Bonus must be non-zero for Heat→Burning.");
        }

        [Test]
        public void Pyromancy_OnNonHeatElement_ReturnsZero()
        {
            // Counter-check: cold/electric/acid spells get 0 bonus from
            // Pyromancy (that's what the OTHER elemental trees are for).
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new PyromancySkill(), source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new BurningEffect(), source: caster, zone: null);

            string[] otherElements = new[] { "Cold", "Electric", "Acid", "Light", "" };
            foreach (var elem in otherElements)
            {
                int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                    caster, target, elem, 12);
                Assert.AreEqual(0, bonus,
                    $"Pyromancy must NOT contribute on element '{elem}' " +
                    $"(only Heat/Fire benefit from fire mastery).");
            }
        }

        [Test]
        public void Pyromancy_OnNonBurningTarget_ReturnsZero()
        {
            // Counter-check: Heat damage to a non-Burning target gets no
            // Pyromancy bonus. The skill rewards already-burning targets
            // — first-hit Heat damage shouldn't get the bonus.
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new PyromancySkill(), source: "test");
            var target = MakeTarget();
            // No BurningEffect on target.

            int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                caster, target, "Heat", 12);
            Assert.AreEqual(0, bonus,
                "Pyromancy must NOT contribute when target isn't Burning. " +
                "Skill rewards sustained fire focus, not opening Heat damage.");
        }

        [Test]
        public void Pyromancy_FloorsAtOne_OnLowBaseDamage()
        {
            // baseDamage=2 → 2/4 = 0 → floored to 1. Non-zero spell
            // damage must always get at least +1 from Pyromancy when
            // gates pass.
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new PyromancySkill(), source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new BurningEffect(), source: caster, zone: null);

            int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                caster, target, "Heat", baseDamage: 2);
            Assert.AreEqual(1, bonus,
                "Pyromancy bonus must floor at 1 even when baseDamage/4 rounds to 0.");
        }

        // ════════════════════════════════════════════════════════════════
        // Spellcraft + Pyromancy STACKING (additive)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void SpellcraftPlusPyromancy_StackAdditively_OnHeatBurning()
        {
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new SpellcraftSkill(), source: "test");
            caster.GetPart<SkillsPart>().AddSkill(new PyromancySkill(), source: "test");
            var target = MakeTarget();
            target.ApplyEffect(new BurningEffect(), source: caster, zone: null);

            // baseDamage=12 → Spellcraft +1, Pyromancy +3 → total +4.
            int bonus = SkillEventDispatcher.GetSpellDamageModifier(
                caster, target, "Heat", 12);
            Assert.AreEqual(SpellcraftSkill.SPELL_DAMAGE_BONUS + 3, bonus,
                "Spellcraft + Pyromancy on a Heat→Burning target must stack additively.");
        }

        // ════════════════════════════════════════════════════════════════
        // MutationDamageHelpers — end-to-end real-pipeline test
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void ApplySpellDamage_TagsDamageWithSpellAndElement_AndAppliesResistance()
        {
            // Pre-WSP7 BUG: int-overload of ApplyDamage built a Damage
            // with NO attributes, so HeatResistance / ColdResistance /
            // etc. never fired on spells. The new ApplySpellDamage
            // helper builds a proper typed Damage with "Spell" + element
            // attribute, so resistance now works.
            //
            // Verify by giving the target HeatResistance=50 (50% damage
            // reduction). With the helper's typed Damage, expected
            // damage = baseDamage / 2. Without the typed Damage, the
            // resistance wouldn't apply.
            var caster = MakeCaster();
            var target = MakeTarget();
            target.Statistics["HeatResistance"] = new Stat
                { Owner = target, Name = "HeatResistance", BaseValue = 50, Min = -100, Max = 100 };

            int hpBefore = target.GetStatValue("Hitpoints");
            int landed = MutationDamageHelpers.ApplySpellDamage(
                target, baseDamage: 20, elementAttribute: "Fire",
                attacker: caster, zone: null);

            // 50% Heat resistance halves 20 → ~10 damage landed.
            // Resistance algorithm in CombatSystem.ApplyResistanceFor
            // uses (100 - resist) / 100 — 20 * 50/100 = 10.
            int hpAfter = target.GetStatValue("Hitpoints");
            int hpDelta = hpBefore - hpAfter;
            Assert.AreEqual(10, hpDelta,
                $"Heat-tagged spell damage must respect HeatResistance. " +
                $"Expected 10 damage (20 * 50% reduction); got HP delta {hpDelta}, " +
                $"helper-returned {landed}. If 20: damage isn't being tagged " +
                $"with the Heat attribute; resistance pipeline is silently bypassed.");
        }

        [Test]
        public void ApplySpellDamage_NoTarget_ReturnsZero_NoCrash()
        {
            int landed = MutationDamageHelpers.ApplySpellDamage(
                target: null, baseDamage: 10, elementAttribute: "Fire",
                attacker: MakeCaster(), zone: null);
            Assert.AreEqual(0, landed,
                "ApplySpellDamage on null target returns 0 without crashing.");
        }

        [Test]
        public void ApplySpellDamage_ZeroBaseDamage_ReturnsZero()
        {
            int landed = MutationDamageHelpers.ApplySpellDamage(
                MakeTarget(), baseDamage: 0, "Fire", MakeCaster(), zone: null);
            Assert.AreEqual(0, landed,
                "ApplySpellDamage on baseDamage=0 returns 0 (no skill query, no damage).");
        }

        [Test]
        public void ApplySpellDamage_WithSpellcraft_DealsBonusDamage()
        {
            // End-to-end: caster has Spellcraft → cast spell on target →
            // damage landed = baseDamage + 1 (Spellcraft's universal
            // bonus).
            var caster = MakeCaster();
            caster.GetPart<SkillsPart>().AddSkill(new SpellcraftSkill(), source: "test");
            var target = MakeTarget();
            int hpBefore = target.GetStatValue("Hitpoints");

            MutationDamageHelpers.ApplySpellDamage(
                target, baseDamage: 10, elementAttribute: "",
                attacker: caster, zone: null);
            int hpAfter = target.GetStatValue("Hitpoints");
            int damage = hpBefore - hpAfter;

            Assert.AreEqual(11, damage,
                "Spellcraft owner casting baseDamage=10 spell should deal 11 (10 + 1 bonus).");
        }

        // ════════════════════════════════════════════════════════════════
        // JSON content — Spellcraft + Pyromancy registered via blueprint
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Wsp7MagicSkills_AllRegisteredInSkillRegistryFromJson()
        {
            SkillRegistry.EnsureInitialized();

            string[] wsp7TreeRoots = new[] { "SpellcraftSkill", "PyromancySkill" };

            foreach (var className in wsp7TreeRoots)
            {
                bool found = SkillRegistry.TryGetSkillByClass(className, out var data);
                Assert.IsTrue(found,
                    $"WSP7 tree-root '{className}' must register from JSON. " +
                    $"Players must be able to see it in the skills menu.");
                Assert.AreEqual(1, data.Cost,
                    $"WSP7 tree-root '{className}' must cost 1 SP per the convention.");
                Assert.IsFalse(string.IsNullOrEmpty(data.Description),
                    $"WSP7 tree-root '{className}' must have a non-empty Description.");
            }
        }
    }
}
