using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP6.17 — ShortBlades_Shank active-ability tests + WSP6.16
    /// TYPE_NEGATIVE backfill verification.
    ///
    /// <para>Coverage:
    /// <list type="bullet">
    ///   <item>Spec shape: DeclareActivatedAbility command + cooldown +
    ///         targeting.</item>
    ///   <item>Negative-effect counter: zero effects → 0; N negative
    ///         effects → N; mix of negative + non-negative → only counts
    ///         negative (verifies the WSP6.16 TYPE_NEGATIVE backfill).</item>
    ///   <item>OnCommand positive: piercing weapon + adjacent target with
    ///         debuffs → swing fires (Shank marker in log) + observable
    ///         damage delta scales with debuff count.</item>
    ///   <item>Counter-checks: no Piercing weapon / no adjacent target /
    ///         null Rng / null Zone — all bail safely.</item>
    ///   <item>Pen-bonus leak guard: after a Shank swing,
    ///         OnGetPenetrationModifier returns 0 for non-Shank swings
    ///         (the try/finally reset works).</item>
    /// </list></para>
    /// </summary>
    public class ShortBladesShankTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
        }

        // ── Fixture helpers ─────────────────────────────────────────────

        private static Entity MakeBodiedCreature(string name = "creature",
            int strength = 16, int hp = 200)
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
            e.AddPart(new ActivatedAbilitiesPart());
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

        private static (Entity attacker, Entity defender, Zone zone, ShortBlades_Shank shank)
            MakeShankFixture()
        {
            var attacker = MakeBodiedCreature("attacker");
            EquipInPrimary(attacker,
                MakeWeaponEntity("dagger", "1d4", "Piercing"));
            var shank = new ShortBlades_Shank();
            attacker.GetPart<SkillsPart>().AddSkill(shank, source: "test");

            var defender = MakeBodiedCreature("defender");
            // High AV so pen bonus differential is visible across seeds.
            defender.GetPart<ArmorPart>().AV = 4;

            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);
            return (attacker, defender, zone, shank);
        }

        // ════════════════════════════════════════════════════════════════
        // Spec shape pin
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Shank_DeclareActivatedAbility_ReturnsExpectedSpec()
        {
            var shank = new ShortBlades_Shank();
            var spec = shank.DeclareActivatedAbility(actor: null);

            Assert.IsNotNull(spec);
            Assert.AreEqual("CommandShank", spec.Command);
            Assert.AreEqual(ShortBlades_Shank.COOLDOWN, spec.Cooldown);
            Assert.AreEqual(AbilityTargetingMode.AdjacentCell, spec.TargetingMode);
            Assert.AreEqual("Shank", spec.DisplayName);
        }

        // ════════════════════════════════════════════════════════════════
        // CountNegativeEffects (WSP6.16 TYPE_NEGATIVE backfill verification)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void CountNegativeEffects_NoEffects_ReturnsZero()
        {
            var target = MakeBodiedCreature("clean");
            Assert.AreEqual(0, ShortBlades_Shank.CountNegativeEffects(target));
        }

        [Test]
        public void CountNegativeEffects_NullTarget_ReturnsZero()
        {
            Assert.AreEqual(0, ShortBlades_Shank.CountNegativeEffects(null));
        }

        [Test]
        public void CountNegativeEffects_TargetWithoutStatusEffectsPart_ReturnsZero()
        {
            // Some test-only entities skip the StatusEffectsPart attach;
            // CountNegativeEffects must handle that gracefully.
            var target = new Entity { ID = "barebones" };
            target.Tags["Creature"] = "";
            target.AddPart(new RenderPart { DisplayName = "barebones" });
            // No StatusEffectsPart.
            Assert.AreEqual(0, ShortBlades_Shank.CountNegativeEffects(target));
        }

        [Test]
        public void CountNegativeEffects_BleedingStunnedAndConfused_ReturnsThree()
        {
            // Verifies the WSP6.16 backfill — Bleeding/Stunned/Confused
            // all carry TYPE_NEGATIVE, so the count is 3. Pre-WSP6.16
            // this would have been 0 (no overrides → all defaulted to
            // TYPE_GENERAL).
            var target = MakeBodiedCreature();
            target.ApplyEffect(new BleedingEffect());
            target.ApplyEffect(new StunnedEffect());
            target.ApplyEffect(new ConfusedEffect());
            Assert.AreEqual(3, ShortBlades_Shank.CountNegativeEffects(target));
        }

        [Test]
        public void CountNegativeEffects_OnlyPositiveEffects_ReturnsZero()
        {
            // Counter-check: Berserk is a self-buff (default
            // TYPE_GENERAL, NOT TYPE_NEGATIVE). The count should be 0.
            // This pins the "TYPE_NEGATIVE backfill is unambiguous" claim
            // — only the effects we explicitly flagged as negative count.
            var target = MakeBodiedCreature();
            target.ApplyEffect(new BerserkEffect());
            Assert.AreEqual(0, ShortBlades_Shank.CountNegativeEffects(target),
                "BerserkEffect is a buff (no TYPE_NEGATIVE flag); must not count.");
        }

        [Test]
        public void CountNegativeEffects_MixOfNegativeAndPositive_OnlyCountsNegative()
        {
            var target = MakeBodiedCreature();
            target.ApplyEffect(new BleedingEffect());      // negative
            target.ApplyEffect(new BerserkEffect());        // positive
            target.ApplyEffect(new BurningEffect());        // negative
            Assert.AreEqual(2, ShortBlades_Shank.CountNegativeEffects(target),
                "Should count Bleeding + Burning (both negative) but NOT Berserk (positive).");
        }

        // ════════════════════════════════════════════════════════════════
        // OnCommand counter-checks
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Shank_WithoutPiercingWeapon_FailsWithMessage()
        {
            var attacker = MakeBodiedCreature();
            EquipInPrimary(attacker, MakeWeaponEntity("mace", "1d8+1", "Bludgeoning Cudgel"));
            var shank = new ShortBlades_Shank();
            attacker.GetPart<SkillsPart>().AddSkill(shank);

            var defender = MakeBodiedCreature("def");
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            shank.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(0),
            });

            bool foundFailMessage = false;
            foreach (var msg in MessageLog.GetRecent(5))
                if (msg.Contains("piercing")) foundFailMessage = true;
            Assert.IsTrue(foundFailMessage,
                "Expected a 'needs piercing-class weapon' message in the log.");
        }

        [Test]
        public void Shank_WithNoAdjacentTarget_FailsWithMessage()
        {
            var (attacker, _, _, shank) = MakeShankFixture();
            // Place attacker alone — drop the defender from MakeShankFixture's setup.
            var emptyZone = new Zone();
            emptyZone.AddEntity(attacker, 5, 5);

            shank.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = emptyZone, Rng = new Random(0),
            });

            bool foundFailMessage = false;
            foreach (var msg in MessageLog.GetRecent(5))
                if (msg.Contains("nothing to shank")) foundFailMessage = true;
            Assert.IsTrue(foundFailMessage,
                "Expected a 'nothing to shank' message in the log.");
        }

        [Test]
        public void Shank_WithNullRng_NoOps_NoCrash()
        {
            var (attacker, defender, zone, shank) = MakeShankFixture();
            int hpBefore = defender.GetStatValue("Hitpoints");
            Assert.DoesNotThrow(() =>
                shank.OnCommand(new SkillEventContext
                {
                    Attacker = attacker, Defender = attacker,
                    Zone = zone, Rng = null,
                }), "Shank with null Rng must not crash.");
            Assert.AreEqual(hpBefore, defender.GetStatValue("Hitpoints"),
                "Shank with null Rng must not deal damage.");
        }

        [Test]
        public void Shank_WithNullZone_NoOps_NoCrash()
        {
            var attacker = MakeBodiedCreature();
            EquipInPrimary(attacker, MakeWeaponEntity("dagger", "1d4", "Piercing"));
            var shank = new ShortBlades_Shank();
            attacker.GetPart<SkillsPart>().AddSkill(shank);

            Assert.DoesNotThrow(() =>
                shank.OnCommand(new SkillEventContext
                {
                    Attacker = attacker, Defender = attacker,
                    Zone = null, Rng = new Random(0),
                }), "Shank with null Zone must not crash.");
        }

        // ════════════════════════════════════════════════════════════════
        // OnCommand positive — Shank fires + (Shank) marker in log
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Shank_WithPiercingWeaponAndAdjacentTarget_FiresShankSwing()
        {
            // Smoke test: actor with Shank + Piercing weapon + adjacent
            // target → OnCommand fires PerformSingleAttack with the
            // (Shank) marker. Don't pin damage exactly (RNG-dependent),
            // just verify the marker appears in the log.
            var (attacker, defender, zone, shank) = MakeShankFixture();

            shank.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(0),
            });

            bool foundShankMarker = false;
            foreach (var msg in MessageLog.GetRecent(20))
                if (msg.Contains("(Shank)")) foundShankMarker = true;
            Assert.IsTrue(foundShankMarker,
                "Expected '(Shank)' attack-source marker in the log.");
        }

        // ════════════════════════════════════════════════════════════════
        // Pen-bonus integration — Shank with debuffs deals more damage
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Shank_WithStatusRiddenTarget_DealsMoreDamage()
        {
            // Statistical pin: a target with 3 negative effects should
            // take strictly more total damage than a clean target across
            // many seeds (the +6 pen bonus pushes more pens through).
            const int SEEDS = 200;

            int totalAgainstClean = 0;
            int totalAgainstDebuffed = 0;

            for (int seed = 0; seed < SEEDS; seed++)
            {
                var (att1, def1, z1, s1) = MakeShankFixture();
                int hp1Before = def1.GetStatValue("Hitpoints");
                MessageLog.Clear();
                s1.OnCommand(new SkillEventContext
                {
                    Attacker = att1, Defender = att1,
                    Zone = z1, Rng = new Random(seed),
                });
                totalAgainstClean += System.Math.Max(0, hp1Before - def1.GetStatValue("Hitpoints"));
            }

            for (int seed = 0; seed < SEEDS; seed++)
            {
                var (att2, def2, z2, s2) = MakeShankFixture();
                def2.ApplyEffect(new BleedingEffect());
                def2.ApplyEffect(new StunnedEffect());
                def2.ApplyEffect(new ConfusedEffect());
                int hp2Before = def2.GetStatValue("Hitpoints");
                MessageLog.Clear();
                s2.OnCommand(new SkillEventContext
                {
                    Attacker = att2, Defender = att2,
                    Zone = z2, Rng = new Random(seed),
                });
                totalAgainstDebuffed += System.Math.Max(0, hp2Before - def2.GetStatValue("Hitpoints"));
            }

            Assert.Greater(totalAgainstDebuffed, totalAgainstClean,
                "Shank against a status-ridden target should deal STRICTLY MORE total damage than against a clean target. " +
                $"Debuffed: {totalAgainstDebuffed}; clean: {totalAgainstClean} (over {SEEDS} seeds).");
        }

        // ════════════════════════════════════════════════════════════════
        // Pen-bonus leak guard — after Shank, the bonus is reset
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Shank_AfterSwing_PenBonusResetToZero()
        {
            // After OnCommand returns, OnGetPenetrationModifier MUST return
            // 0 — the try/finally guarantees the per-swing buff doesn't
            // leak to subsequent non-Shank swings or save/load.
            var (attacker, defender, zone, shank) = MakeShankFixture();
            // Stack debuffs so the bonus during OnCommand is non-trivial.
            defender.ApplyEffect(new BleedingEffect());
            defender.ApplyEffect(new StunnedEffect());

            shank.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(0),
            });

            // Post-swing query — should be 0 because the finally block reset it.
            int postBonus = shank.OnGetPenetrationModifier(attacker, weapon: null);
            Assert.AreEqual(0, postBonus,
                "After Shank's OnCommand returns, OnGetPenetrationModifier must read 0 — " +
                "the try/finally must reset _activePenBonus so the buff doesn't leak to " +
                "non-Shank swings.");
        }
    }
}
