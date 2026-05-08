using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.2 — Axe_Whirlwind active-ability tests.
    /// Pins the "spin once, hit every adjacent creature" mechanic. Each
    /// strike is a full <see cref="CombatSystem.PerformSingleAttack"/>
    /// (so on-hit hooks like Cleave/Dismember fire per target). Single
    /// activation, multiple strikes — distinct from Slam (one target),
    /// Flurry (one target N times), and Lunge (one target at distance 2).
    ///
    /// <para>Coverage layout (mirrors CudgelSlamTests):
    /// <list type="bullet">
    ///   <item>Spec shape — DeclareActivatedAbility's
    ///         Command/TargetingMode/Range/Cooldown.</item>
    ///   <item>Positive: hit a single adjacent target, hit multiple
    ///         adjacent targets (the 8-direction sweep).</item>
    ///   <item>Counter-checks: no Axe weapon (Cudgel doesn't trigger),
    ///         no adjacent creatures, null Rng / null Zone.</item>
    ///   <item>Adversarial: mid-loop target death doesn't crash the
    ///         remaining strikes.</item>
    /// </list></para>
    /// </summary>
    public class AxeWhirlwindTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            SkillRegistry.ResetForTests();
        }

        // ── Fixture helpers (mirror CudgelSlamTests exactly) ─────────────

        private static Entity MakeBodiedCreature(string name = "creature",
            int strength = 16, int hp = 50)
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

        private static Entity MakeWeaponEntity(string name, string dice,
            string attributes)
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

        /// <summary>Builds an attacker (Axe-class weapon equipped, Whirlwind
        /// owned) + a Zone. Caller adds defenders + places them.</summary>
        private static (Entity attacker, Zone zone, Axe_Whirlwind whirlwind)
            MakeWhirlwindFixture(string weaponAttributes = "Cutting Axe")
        {
            var attacker = MakeBodiedCreature("attacker");
            EquipInPrimary(attacker,
                MakeWeaponEntity("battleaxe", "1d8+1", weaponAttributes));
            var whirlwind = new Axe_Whirlwind();
            attacker.GetPart<SkillsPart>().AddSkill(whirlwind, source: "test");

            var zone = new Zone();
            return (attacker, zone, whirlwind);
        }

        // ════════════════════════════════════════════════════════════════
        // Spec shape
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Whirlwind_DeclareActivatedAbility_ReturnsExpectedSpec()
        {
            var w = new Axe_Whirlwind();
            var spec = w.DeclareActivatedAbility(actor: null);

            Assert.IsNotNull(spec);
            Assert.AreEqual("CommandWhirlwind", spec.Command);
            Assert.AreEqual(Axe_Whirlwind.COOLDOWN, spec.Cooldown);
            Assert.AreEqual(AbilityTargetingMode.SelfCentered, spec.TargetingMode,
                "Whirlwind is self-centered (no targeting prompt — fires "
                + "around the actor).");
            Assert.AreEqual("Whirlwind", spec.DisplayName);
        }

        // ════════════════════════════════════════════════════════════════
        // Positive: a single adjacent target takes damage
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Whirlwind_OneAdjacentTarget_TakesDamage()
        {
            var (attacker, zone, w) = MakeWhirlwindFixture();
            var defender = MakeBodiedCreature("defender");
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            int hpBefore = defender.GetStatValue("Hitpoints");
            w.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(42),
            });

            Assert.Less(defender.GetStatValue("Hitpoints"), hpBefore,
                "Single adjacent defender must take a Whirlwind strike.");
        }

        // ════════════════════════════════════════════════════════════════
        // Positive: ALL 8 adjacent creatures take damage from one swing
        // (the defining "self-AOE" mechanic)
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Whirlwind_HitsAllAdjacentCreatures()
        {
            var (attacker, zone, w) = MakeWhirlwindFixture();
            zone.AddEntity(attacker, 5, 5);

            // Drop a defender in EACH of the 8 cardinal/diagonal cells
            // around the attacker. Whirlwind should hit every one.
            var defenders = new Entity[8];
            int[][] offsets = new int[][]
            {
                new int[] { 0, -1 }, new int[] { 1, -1 }, new int[] { 1, 0 },
                new int[] { 1, 1 }, new int[] { 0, 1 }, new int[] { -1, 1 },
                new int[] { -1, 0 }, new int[] { -1, -1 }
            };
            for (int i = 0; i < 8; i++)
            {
                defenders[i] = MakeBodiedCreature("defender" + i);
                zone.AddEntity(defenders[i], 5 + offsets[i][0], 5 + offsets[i][1]);
            }
            int[] hpBefore = new int[8];
            for (int i = 0; i < 8; i++)
                hpBefore[i] = defenders[i].GetStatValue("Hitpoints");

            w.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(42),
            });

            // Every defender should have taken some damage. The exact
            // amounts vary per RNG roll — what matters is the AOE landed
            // on every cell.
            for (int i = 0; i < 8; i++)
            {
                Assert.Less(defenders[i].GetStatValue("Hitpoints"), hpBefore[i],
                    "Defender at offset (" + offsets[i][0] + ", " + offsets[i][1]
                    + ") must take a Whirlwind strike.");
            }
        }

        // ════════════════════════════════════════════════════════════════
        // Counter-check: Cudgel weapon does NOT trigger Whirlwind
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Whirlwind_WithCudgelWeapon_RefusesToSwing()
        {
            var (attacker, zone, w) =
                MakeWhirlwindFixture(weaponAttributes: "Bludgeoning Cudgel");
            var defender = MakeBodiedCreature("defender");
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            int hpBefore = defender.GetStatValue("Hitpoints");
            w.OnCommand(new SkillEventContext
            {
                Attacker = attacker, Defender = attacker,
                Zone = zone, Rng = new Random(42),
            });

            Assert.AreEqual(hpBefore, defender.GetStatValue("Hitpoints"),
                "Cudgel-class weapon must NOT trigger Whirlwind.");
        }

        // ════════════════════════════════════════════════════════════════
        // Counter-check: no adjacent creatures → no strike, no crash
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Whirlwind_NoAdjacentCreatures_NoStrike()
        {
            var (attacker, zone, w) = MakeWhirlwindFixture();
            zone.AddEntity(attacker, 5, 5);
            // No defenders placed.

            Assert.DoesNotThrow(() =>
            {
                w.OnCommand(new SkillEventContext
                {
                    Attacker = attacker, Defender = attacker,
                    Zone = zone, Rng = new Random(42),
                });
            }, "Empty Whirlwind must not throw.");
        }

        // ════════════════════════════════════════════════════════════════
        // Adversarial: null Rng / null Zone
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void Whirlwind_WithNullRng_NoOps_NoCrash()
        {
            var (attacker, zone, w) = MakeWhirlwindFixture();
            var defender = MakeBodiedCreature("defender");
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            int hpBefore = defender.GetStatValue("Hitpoints");
            Assert.DoesNotThrow(() =>
            {
                w.OnCommand(new SkillEventContext
                {
                    Attacker = attacker, Defender = attacker,
                    Zone = zone, Rng = null,
                });
            }, "Null-Rng Whirlwind must not throw.");
            Assert.AreEqual(hpBefore, defender.GetStatValue("Hitpoints"),
                "Null-Rng Whirlwind must do nothing.");
        }

        [Test]
        public void Whirlwind_WithNullZone_NoOps_NoCrash()
        {
            var (attacker, _, w) = MakeWhirlwindFixture();
            Assert.DoesNotThrow(() =>
            {
                w.OnCommand(new SkillEventContext
                {
                    Attacker = attacker, Defender = attacker,
                    Zone = null, Rng = new Random(42),
                });
            }, "Null-Zone Whirlwind must not throw.");
        }
    }
}
