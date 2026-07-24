using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.3 — Cudgel_ChargingStrike tests. Pins the "move N cells
    /// then strike with bonus damage" mechanic.
    /// </summary>
    public class CudgelChargingStrikeTests
    {
        [SetUp] public void Setup() { MessageLog.Clear(); SkillRegistry.ResetForTests(); Diag.ResetAll(); }

        private static Entity MakeBodied(string name = "c", int hp = 50)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Strength"] = new Stat { Owner = e, Name = "Strength", BaseValue = 18, Min = 1, Max = 50 };
            e.Statistics["Agility"] = new Stat { Owner = e, Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Toughness"] = new Stat { Owner = e, Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            e.Statistics["Speed"] = new Stat { Owner = e, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            e.Statistics["DV"] = new Stat { Owner = e, Name = "DV", BaseValue = 0, Min = -50, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new ArmorPart());
            e.AddPart(new InventoryPart { MaxWeight = 150 });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new SkillsPart());
            var body = new Body(); e.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());
            return e;
        }

        private static Entity MakeWeapon(string attrs = "Bludgeoning Cudgel")
        {
            var e = new Entity { ID = "mace", BlueprintName = "mace" };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = "mace" });
            e.AddPart(new PhysicsPart { Takeable = true, Weight = 5 });
            e.AddPart(new MeleeWeaponPart { BaseDamage = "1d8+1", Attributes = attrs });
            e.AddPart(new EquippablePart { Slot = "Hand" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static (Entity atk, Zone zone, Cudgel_ChargingStrike skill) Fixture(string attrs = "Bludgeoning Cudgel")
        {
            var atk = MakeBodied("atk");
            var hand = atk.GetPart<Body>().GetParts().Find(p => p.Type == "Hand");
            atk.GetPart<InventoryPart>().EquipToBodyPart(MakeWeapon(attrs), hand);
            var skill = new Cudgel_ChargingStrike();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            return (atk, new Zone(), skill);
        }

        private static Entity MakeWall()
        {
            var w = new Entity { ID = "wall", BlueprintName = "wall" };
            w.Tags["Solid"] = ""; w.Tags["Wall"] = "";
            w.AddPart(new RenderPart { DisplayName = "wall" });
            return w;
        }

        [Test]
        public void ChargingStrike_Spec_ReturnsExpectedShape()
        {
            var spec = new Cudgel_ChargingStrike().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandChargingStrike", spec.Command);
            Assert.AreEqual(AbilityTargetingMode.DirectionLine, spec.TargetingMode);
            Assert.AreEqual(Cudgel_ChargingStrike.CHARGE_DISTANCE, spec.Range);
        }

        [Test]
        public void ChargingStrike_TargetAtMaxRange_MovesAndStrikes()
        {
            var (atk, zone, skill) = Fixture();
            var def = MakeBodied("def", hp: 100);
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(def, 8, 5); // 3 cells East = max charge distance
            int hpBefore = def.GetStatValue("Hitpoints");

            skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(42),
                DirectionX = 1, DirectionY = 0,
            });

            // Actor moved up to one cell short of target.
            var atkPos = zone.GetEntityPosition(atk);
            Assert.AreEqual(7, atkPos.x, "Actor must stop one cell short of target.");
            Assert.Less(def.GetStatValue("Hitpoints"), hpBefore, "Defender must take damage from the strike.");
        }

        [Test]
        public void ChargingStrike_NoTargetInLine_ActorMovesNoStrike()
        {
            var (atk, zone, skill) = Fixture();
            zone.AddEntity(atk, 5, 5);
            // No defender in path.

            skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(42),
                DirectionX = 1, DirectionY = 0,
            });

            var atkPos = zone.GetEntityPosition(atk);
            Assert.AreEqual(8, atkPos.x, "With no target, actor walks the full charge distance.");
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            Assert.GreaterOrEqual(recs.Count, 1);
            StringAssert.Contains("no_target", recs[0].PayloadJson);
        }

        [Test]
        public void ChargingStrike_WallBlocksPath_ActorStopsBeforeWall()
        {
            var (atk, zone, skill) = Fixture();
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(MakeWall(), 7, 5); // wall at distance 2

            skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(42),
                DirectionX = 1, DirectionY = 0,
            });

            var atkPos = zone.GetEntityPosition(atk);
            Assert.AreEqual(6, atkPos.x, "Actor must stop one cell before the wall.");
        }

        [Test]
        public void ChargingStrike_NoCudgel_RefusesAndEmitsDiag()
        {
            var (atk, zone, skill) = Fixture(attrs: "Cutting LongBlades");
            var def = MakeBodied("def");
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 8, 5);
            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(42),
                DirectionX = 1, DirectionY = 0,
            });
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("no_weapon", recs[0].PayloadJson);
        }

        [Test]
        public void ChargingStrike_NoDirection_EmitsDiag()
        {
            var (atk, zone, skill) = Fixture();
            zone.AddEntity(atk, 5, 5);
            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(42),
                DirectionX = 0, DirectionY = 0,
            });
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("no_direction", recs[0].PayloadJson);
        }

        [Test]
        public void ChargingStrike_NullRng_NoCrash()
        {
            var (atk, zone, skill) = Fixture();
            zone.AddEntity(atk, 5, 5);
            Assert.DoesNotThrow(() =>
                skill.OnCommand(new SkillEventContext
                {
                    Attacker = atk, Defender = atk, Zone = zone, Rng = null,
                    DirectionX = 1, DirectionY = 0,
                }));
        }

        // ════════════════════════════════════════════════════════════════
        // Docs/COMBAT-SYSTEM-AUDIT-2026-07.md -- ChargingStrike's bonus
        // (+50% momentum) damage must route through the on-hit dispatch
        // chain (SkillCombatHelpers.DealGuaranteedHitDamage), not the raw
        // ApplyDamage(int) overload that skips it entirely. Mirrors the
        // SM8/B2 fix already applied to Cudgel_Slam / Cudgel_GroundPound.
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void ChargingStrike_BonusDamage_DispatchesItemEnhancementTwice()
        {
            // The BASE swing already dispatches IItemEnhancement.OnAttackerHit
            // once via CombatSystem.PerformSingleAttack -- a boolean "did it
            // fire" probe can't distinguish "base swing only" from "base
            // swing + bonus damage also dispatched." This test counts
            // dispatches instead: 1 = bug (bonus momentum damage bypassed
            // the on-hit chain via the raw ApplyDamage(int) overload), 2 =
            // fixed (bonus damage got its own proc roll, same as the base
            // swing gets).
            var atk = MakeBodied("atk");
            var hand = atk.GetPart<Body>().GetParts().Find(p => p.Type == "Hand");
            var weaponEntity = new Entity { ID = "mace", BlueprintName = "mace" };
            weaponEntity.Tags["Item"] = "";
            weaponEntity.AddPart(new RenderPart { DisplayName = "mace" });
            weaponEntity.AddPart(new PhysicsPart { Takeable = true, Weight = 5 });
            weaponEntity.AddPart(new MeleeWeaponPart
            {
                // Large guaranteed-hit, guaranteed-heavy-damage weapon so
                // both the base swing AND the +50% bonus clear the bonus
                // >= 1 gate deterministically (mirrors the "killing blow"
                // fixture in CombatSystemSpecTests.cs).
                BaseDamage = "100d6", PenBonus = 50, HitBonus = 50,
                Attributes = "Bludgeoning Cudgel",
            });
            weaponEntity.AddPart(new EquippablePart { Slot = "Hand" });
            weaponEntity.AddPart(new StatusEffectsPart());
            var probe = new CountingEnhancementProbe();
            weaponEntity.AddPart(probe);
            atk.GetPart<InventoryPart>().EquipToBodyPart(weaponEntity, hand);
            var skill = new Cudgel_ChargingStrike();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");

            var def = MakeBodied("def", hp: 10000); // survives base + bonus comfortably
            var zone = new Zone();
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(def, 8, 5); // 3 cells East = max charge distance

            skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(42),
                DirectionX = 1, DirectionY = 0,
            });

            Assert.AreEqual(2, probe.FireCount,
                "ChargingStrike must dispatch IItemEnhancement.OnAttackerHit TWICE -- once "
                + "for the base swing (PerformSingleAttack) and once for the +50% momentum "
                + "bonus damage. The bonus is a second hit-sized chunk of damage and deserves "
                + "its own on-hit proc roll (e.g. a Serrated weapon's bleed chance, a "
                + "Lifesteal mace's heal) -- not just a silent, un-procced HP subtraction.");
        }
    }

    /// <summary>
    /// Counting variant of CombatSystemSpecTests' AlwaysFiresEnhancementProbe
    /// -- a boolean "Fired" flag can't distinguish one dispatch from two,
    /// which is exactly the distinction the ChargingStrike bonus-damage
    /// dispatch bug needs a test to make.
    /// </summary>
    public class CountingEnhancementProbe : IItemEnhancement
    {
        public int FireCount;

        public override void OnAttackerHit(
            Entity defender, Entity attacker, Damage damage,
            int actualDamage, Zone zone, System.Random rng)
        {
            FireCount++;
        }
    }
}
