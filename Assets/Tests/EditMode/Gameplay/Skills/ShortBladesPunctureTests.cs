using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP6.6 — ShortBlades_Puncture passive tests + dispatcher
    /// entry-point pin for the new <c>OnGetPenetrationModifier</c> hook.
    ///
    /// <para>Coverage:
    /// <list type="bullet">
    ///   <item>Dispatcher: <see cref="SkillEventDispatcher.GetSkillPenetrationModifier"/>
    ///         sums across owned skills, zero on null actor, zero on no
    ///         SkillsPart.</item>
    ///   <item>Puncture positive: Piercing-attribute weapon → +2 pen.</item>
    ///   <item>Puncture counter-check: non-Piercing weapon → 0
    ///         (Cutting / Bludgeoning / empty / null).</item>
    ///   <item>Integration: actor with Puncture against fixed-AV defender
    ///         lands more penetrations across seeds than the same actor
    ///         without Puncture — statistical pin.</item>
    /// </list></para>
    /// </summary>
    public class ShortBladesPunctureTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
        }

        // ── Fixture helpers (mirror SkillSystemTier2Tests) ───────────────

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

        // ════════════════════════════════════════════════════════════════
        // Dispatcher pin — GetSkillPenetrationModifier basics
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void GetSkillPenetrationModifier_NullAttacker_ReturnsZero()
        {
            // Defense-in-depth — the dispatcher is called from
            // CombatSystem.PerformSingleAttack at every melee swing;
            // defending against null actors keeps a stack of edge cases
            // (e.g. environmental damage with no attacker) safe.
            int bonus = SkillEventDispatcher.GetSkillPenetrationModifier(
                attacker: null,
                weapon: MakeWeapon("Piercing"));
            Assert.AreEqual(0, bonus,
                "GetSkillPenetrationModifier must return 0 when attacker is null.");
        }

        [Test]
        public void GetSkillPenetrationModifier_AttackerWithoutSkillsPart_ReturnsZero()
        {
            // Attacker with no SkillsPart — same defense-in-depth concern
            // as the to-hit hook (combat NPCs may not be skill-bearing).
            var bareActor = new Entity { ID = "bare" };
            bareActor.AddPart(new RenderPart { DisplayName = "bare" });
            int bonus = SkillEventDispatcher.GetSkillPenetrationModifier(
                bareActor, MakeWeapon("Piercing"));
            Assert.AreEqual(0, bonus,
                "GetSkillPenetrationModifier must return 0 on actors without SkillsPart.");
        }

        [Test]
        public void GetSkillPenetrationModifier_SumsAcrossOwnedSkills()
        {
            // Two Puncture-style skills on the same actor → contributions
            // sum (Puncture itself can't dupe — same Type — but two
            // distinct skills each returning a pen bonus must aggregate).
            var stub1 = new TestPenStubA();   // returns +3 always
            var stub2 = new TestPenStubB();   // returns +5 always
            var actor = MakeAttackerWithSkill(stub1);
            actor.GetPart<SkillsPart>().AddSkill(stub2, source: "test");

            int bonus = SkillEventDispatcher.GetSkillPenetrationModifier(
                actor, MakeWeapon("Piercing"));

            Assert.AreEqual(3 + 5, bonus,
                "GetSkillPenetrationModifier must SUM contributions from all owned skills.");
        }

        // ════════════════════════════════════════════════════════════════
        // Puncture passive — positive + counter-checks
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Puncture_OnPiercingWeapon_AddsPenBonus()
        {
            var actor = MakeAttackerWithSkill(new ShortBlades_Puncture());
            var dagger = MakeWeapon("Piercing");
            int bonus = SkillEventDispatcher.GetSkillPenetrationModifier(actor, dagger);
            Assert.AreEqual(ShortBlades_Puncture.PEN_BONUS, bonus,
                "Puncture must add PEN_BONUS when wielding a Piercing-attribute weapon.");
        }

        [Test]
        public void Puncture_OnCuttingWeapon_NoBonus()
        {
            var actor = MakeAttackerWithSkill(new ShortBlades_Puncture());
            var sword = MakeWeapon("Cutting LongBlades");
            int bonus = SkillEventDispatcher.GetSkillPenetrationModifier(actor, sword);
            Assert.AreEqual(0, bonus,
                "Puncture must NOT add a pen bonus on Cutting (LongBlades) weapons.");
        }

        [Test]
        public void Puncture_OnBludgeoningWeapon_NoBonus()
        {
            var actor = MakeAttackerWithSkill(new ShortBlades_Puncture());
            var mace = MakeWeapon("Bludgeoning Cudgel");
            int bonus = SkillEventDispatcher.GetSkillPenetrationModifier(actor, mace);
            Assert.AreEqual(0, bonus,
                "Puncture must NOT add a pen bonus on Bludgeoning (Cudgel) weapons.");
        }

        [Test]
        public void Puncture_OnEmptyAttributesWeapon_NoBonus()
        {
            var actor = MakeAttackerWithSkill(new ShortBlades_Puncture());
            var bareWeapon = MakeWeapon("");
            int bonus = SkillEventDispatcher.GetSkillPenetrationModifier(actor, bareWeapon);
            Assert.AreEqual(0, bonus,
                "Puncture must NOT add a pen bonus on weapons with empty Attributes.");
        }

        [Test]
        public void Puncture_OnNullWeapon_NoBonus()
        {
            var actor = MakeAttackerWithSkill(new ShortBlades_Puncture());
            int bonus = SkillEventDispatcher.GetSkillPenetrationModifier(actor, weapon: null);
            Assert.AreEqual(0, bonus,
                "Puncture must NOT crash and must return 0 when weapon is null.");
        }

        // ════════════════════════════════════════════════════════════════
        // Integration — Puncture observable through PerformSingleAttack
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Puncture_ObservedThroughPerformSingleAttack_BoostsTotalDamage()
        {
            // Seed a deterministic shootout: same defender (fixed AV from
            // ArmorPart), same attacker stats, same weapon, same RNG seeds.
            // With Puncture: skillPenBonus = +2 → RollPenetrations gets a
            // higher bonus → more pens land → more total damage dealt.
            //
            // STATISTICAL pin — pen rolls are randomized so a single-seed
            // comparison would be noisy. Run many seeds and compare
            // total damage dealt (which scales with pen count + per-pen
            // damage roll). HP delta on the defender is the cleanest
            // observable: "fails to penetrate" leaves HP unchanged, every
            // successful pen takes a chunk.
            const int SEEDS = 200;

            int withPuncture = SimulateTotalDamageDealt(includePuncture: true, seeds: SEEDS);
            int withoutPuncture = SimulateTotalDamageDealt(includePuncture: false, seeds: SEEDS);

            Assert.Greater(withPuncture, withoutPuncture,
                "Across many seeds, an attacker with Puncture should deal STRICTLY MORE total damage than the same actor without Puncture. " +
                $"With Puncture: {withPuncture} HP; without Puncture: {withoutPuncture} HP (over {SEEDS} seeds).");
        }

        private static int SimulateTotalDamageDealt(bool includePuncture, int seeds)
        {
            // Inline because the helper depends on test-fixture types.
            // Damage proxy: defender HP delta. Each swing: capture HP
            // before, swing, capture HP after, accumulate the delta.
            // Pens manifest as HP loss; "fails to penetrate" returns 0
            // delta; equally-resisted hits also return 0 delta.
            int totalDamage = 0;
            for (int seed = 0; seed < seeds; seed++)
            {
                var (att, def, wpn, zone) = MakeCombatFixture();
                if (includePuncture)
                    att.GetPart<SkillsPart>().AddSkill(new ShortBlades_Puncture(), source: "test");

                int hpBefore = def.GetStatValue("Hitpoints");
                MessageLog.Clear();
                CombatSystem.PerformSingleAttack(
                    attacker: att, defender: def,
                    weapon: wpn, isPrimary: true,
                    zone: zone, rng: new Random(seed),
                    attackSourceDesc: null);
                int hpAfter = def.GetStatValue("Hitpoints");
                totalDamage += System.Math.Max(0, hpBefore - hpAfter);
            }
            return totalDamage;
        }

        private static (Entity attacker, Entity defender, MeleeWeaponPart weapon, Zone zone)
            MakeCombatFixture()
        {
            var att = MakeBodiedFighter("attacker", strength: 16);
            var dagger = MakeWeaponEntity("dagger", "1d4", "Piercing");
            EquipInPrimary(att, dagger);

            // Defender: solid AV via ArmorPart so penetrations are tightly
            // gated on the bonus value. Higher AV = lower base pen rate,
            // making the +2 bonus differential more visible across seeds.
            var def = MakeBodiedFighter("defender");
            def.GetPart<ArmorPart>().AV = 4;

            var zone = new Zone();
            zone.AddEntity(att, 5, 5);
            zone.AddEntity(def, 6, 5);
            return (att, def, dagger.GetPart<MeleeWeaponPart>(), zone);
        }

        private static Entity MakeBodiedFighter(string name, int strength = 16, int hp = 200)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Strength"] = new Stat
                { Owner = e, Name = "Strength", BaseValue = strength, Min = 1, Max = 50 };
            e.Statistics["Agility"] = new Stat
                { Owner = e, Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Speed"] = new Stat
                { Owner = e, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            e.Statistics["DV"] = new Stat
                { Owner = e, Name = "DV", BaseValue = 0, Min = -50, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new ArmorPart());
            e.AddPart(new InventoryPart { MaxWeight = 150 });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new SkillsPart());
            var body = new Body();
            e.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());
            return e;
        }

        private static Entity MakeWeaponEntity(string name, string dice, string attributes)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new PhysicsPart { Takeable = true, Weight = 5 });
            e.AddPart(new MeleeWeaponPart
            {
                BaseDamage = dice, PenBonus = 0,
                Attributes = attributes,
            });
            e.AddPart(new EquippablePart { Slot = "Hand" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static void EquipInPrimary(Entity actor, Entity weaponEntity)
        {
            var hand = actor.GetPart<Body>().GetParts().Find(p => p.Type == "Hand");
            actor.GetPart<InventoryPart>().EquipToBodyPart(weaponEntity, hand);
        }

        // ── Test stubs ───────────────────────────────────────────────────

        public class TestPenStubA : BaseSkillPart
        {
            public override int OnGetPenetrationModifier(Entity actor, MeleeWeaponPart weapon)
            {
                return 3;
            }
        }

        public class TestPenStubB : BaseSkillPart
        {
            public override int OnGetPenetrationModifier(Entity actor, MeleeWeaponPart weapon)
            {
                return 5;
            }
        }
    }
}
