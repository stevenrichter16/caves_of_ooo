using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.3 — Cudgel_Disarm tests. Pins the "unequip target's
    /// melee weapon, drop it on their cell" mechanic.
    /// </summary>
    public class CudgelDisarmTests
    {
        [SetUp] public void Setup() { MessageLog.Clear(); SkillRegistry.ResetForTests(); Diag.ResetAll(); }

        private static Entity MakeBodied(string name = "c", int hp = 50)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
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

        private static Entity MakeWeapon(string name, string attrs)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Item"] = "";
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new PhysicsPart { Takeable = true, Weight = 5 });
            e.AddPart(new MeleeWeaponPart { BaseDamage = "1d8", Attributes = attrs });
            e.AddPart(new EquippablePart { Slot = "Hand" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static (Entity atk, Entity def, Entity defWeapon, Zone zone, Cudgel_Disarm skill) Fixture(string atkAttrs = "Bludgeoning Cudgel", string defAttrs = "Cutting LongBlades")
        {
            var atk = MakeBodied("atk");
            var atkHand = atk.GetPart<Body>().GetParts().Find(p => p.Type == "Hand");
            atk.GetPart<InventoryPart>().EquipToBodyPart(MakeWeapon("mace", atkAttrs), atkHand);

            var skill = new Cudgel_Disarm();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");

            var def = MakeBodied("def");
            var defWeapon = MakeWeapon("longsword", defAttrs);
            var defHand = def.GetPart<Body>().GetParts().Find(p => p.Type == "Hand");
            def.GetPart<InventoryPart>().AddObject(defWeapon);
            def.GetPart<InventoryPart>().EquipToBodyPart(defWeapon, defHand);

            return (atk, def, defWeapon, new Zone(), skill);
        }

        [Test]
        public void Disarm_Spec_ReturnsExpectedShape()
        {
            var spec = new Cudgel_Disarm().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandDisarm", spec.Command);
            Assert.AreEqual(AbilityTargetingMode.AdjacentCell, spec.TargetingMode);
        }

        [Test]
        public void Disarm_DropsTargetWeaponOnTargetCell()
        {
            var (atk, def, defWeapon, zone, skill) = Fixture();
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);

            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0) });

            // Weapon should be in the zone at target's cell.
            var weaponCell = zone.GetEntityCell(defWeapon);
            Assert.IsNotNull(weaponCell, "Disarmed weapon must be placed in the zone.");
            Assert.AreEqual((6, 5), (weaponCell.X, weaponCell.Y),
                "Weapon must drop on target's cell.");
        }

        [Test]
        public void Disarm_TargetUnarmed_RejectsAndEmitsDiag()
        {
            var (atk, def, defWeapon, zone, skill) = Fixture();
            // Unequip the def's weapon BEFORE the disarm attempt.
            InventorySystem.UnequipItem(def, defWeapon);
            def.GetPart<InventoryPart>().RemoveObject(defWeapon);
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);

            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0) });
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("target_unarmed", recs[0].PayloadJson);
        }

        [Test]
        public void Disarm_NoCudgelWeapon_RejectsAndEmitsDiag()
        {
            var (atk, def, _, zone, skill) = Fixture(atkAttrs: "Cutting LongBlades");
            zone.AddEntity(atk, 5, 5); zone.AddEntity(def, 6, 5);
            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0) });
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("no_weapon", recs[0].PayloadJson);
        }

        [Test]
        public void Disarm_NoTarget_EmitsDiag()
        {
            var (atk, _, _, zone, skill) = Fixture();
            zone.AddEntity(atk, 5, 5); // no def
            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext { Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0) });
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("no_target", recs[0].PayloadJson);
        }
    }
}
