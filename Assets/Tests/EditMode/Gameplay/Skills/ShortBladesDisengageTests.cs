using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.3 — ShortBlades_Disengage tests. Pins the "move N cells
    /// without attacking" mechanic.
    /// </summary>
    public class ShortBladesDisengageTests
    {
        [SetUp] public void Setup() { MessageLog.Clear(); SkillRegistry.ResetForTests(); Diag.ResetAll(); }

        private static Entity MakeBodied(string name = "c", int hp = 50)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Strength"] = new Stat { Owner = e, Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
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

        private static Entity MakeWeapon(string attrs)
        {
            var e = new Entity { ID = "dagger", BlueprintName = "dagger" };
            e.AddPart(new RenderPart { DisplayName = "dagger" });
            e.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
            e.AddPart(new MeleeWeaponPart { BaseDamage = "1d4", Attributes = attrs });
            e.AddPart(new EquippablePart { Slot = "Hand" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static (Entity atk, Zone zone, ShortBlades_Disengage skill) Fixture(string attrs = "Piercing")
        {
            var atk = MakeBodied("atk");
            var hand = atk.GetPart<Body>().GetParts().Find(p => p.Type == "Hand");
            atk.GetPart<InventoryPart>().EquipToBodyPart(MakeWeapon(attrs), hand);
            var skill = new ShortBlades_Disengage();
            atk.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            return (atk, new Zone(), skill);
        }

        [Test]
        public void Disengage_Spec_ReturnsExpectedShape()
        {
            var spec = new ShortBlades_Disengage().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandDisengage", spec.Command);
            Assert.AreEqual(AbilityTargetingMode.DirectionLine, spec.TargetingMode);
        }

        [Test]
        public void Disengage_OpenLine_ActorMovesFullDistance()
        {
            var (atk, zone, skill) = Fixture();
            zone.AddEntity(atk, 5, 5);
            skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0),
                DirectionX = 1, DirectionY = 0,
            });
            var pos = zone.GetEntityPosition(atk);
            Assert.AreEqual(8, pos.x, "Disengage walks 3 cells East from (5,5) to (8,5).");
        }

        [Test]
        public void Disengage_CreatureBlocks_ActorStopsBeforeIt()
        {
            var (atk, zone, skill) = Fixture();
            var blocker = MakeBodied("blocker");
            zone.AddEntity(atk, 5, 5);
            zone.AddEntity(blocker, 7, 5); // 2 cells east
            skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0),
                DirectionX = 1, DirectionY = 0,
            });
            var pos = zone.GetEntityPosition(atk);
            Assert.AreEqual(6, pos.x, "Disengage stops one cell before the blocker.");
            Assert.AreEqual(50, blocker.GetStatValue("Hitpoints"),
                "Disengage does NOT attack — blocker untouched.");
        }

        [Test]
        public void Disengage_NoPiercingWeapon_RefusesAndEmitsDiag()
        {
            var (atk, zone, skill) = Fixture(attrs: "Bludgeoning Cudgel");
            zone.AddEntity(atk, 5, 5);
            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0),
                DirectionX = 1, DirectionY = 0,
            });
            var pos = zone.GetEntityPosition(atk);
            Assert.AreEqual(5, pos.x, "Without Piercing, actor must not move.");
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("no_weapon", recs[0].PayloadJson);
        }

        [Test]
        public void Disengage_NoDirection_EmitsDiag()
        {
            var (atk, zone, skill) = Fixture();
            zone.AddEntity(atk, 5, 5);
            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext
            {
                Attacker = atk, Defender = atk, Zone = zone, Rng = new Random(0),
                DirectionX = 0, DirectionY = 0,
            });
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("no_direction", recs[0].PayloadJson);
        }

        [Test]
        public void Disengage_NullZone_NoCrash()
        {
            var (atk, _, skill) = Fixture();
            Assert.DoesNotThrow(() =>
                skill.OnCommand(new SkillEventContext
                {
                    Attacker = atk, Defender = atk, Zone = null, Rng = new Random(0),
                    DirectionX = 1, DirectionY = 0,
                }));
        }
    }
}
