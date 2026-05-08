using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP8.3 — Acrobatics_Vault tests. Pins the "leap 2 cells,
    /// skipping the cell at distance 1" mechanic.
    /// </summary>
    public class AcrobaticsVaultTests
    {
        [SetUp] public void Setup() { MessageLog.Clear(); SkillRegistry.ResetForTests(); Diag.ResetAll(); }

        private static Entity MakeBodied(string name = "actor")
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Owner = e, Name = "Hitpoints", BaseValue = 50, Min = 0, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = name });
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new SkillsPart());
            return e;
        }

        private static Entity MakeWall()
        {
            var w = new Entity { ID = "wall", BlueprintName = "wall" };
            w.Tags["Solid"] = "";
            w.AddPart(new RenderPart { DisplayName = "wall" });
            return w;
        }

        [Test]
        public void Vault_Spec_ReturnsExpectedShape()
        {
            var spec = new Acrobatics_Vault().DeclareActivatedAbility(null);
            Assert.AreEqual("CommandVault", spec.Command);
            Assert.AreEqual(AbilityTargetingMode.DirectionLine, spec.TargetingMode);
            Assert.AreEqual(Acrobatics_Vault.VAULT_DISTANCE, spec.Range);
        }

        [Test]
        public void Vault_OverWall_LandsOnFarSide()
        {
            var actor = MakeBodied();
            var skill = new Acrobatics_Vault();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var zone = new Zone();
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(MakeWall(), 6, 5); // wall at distance 1
            // Cell (7, 5) is open by default

            skill.OnCommand(new SkillEventContext
            {
                Attacker = actor, Defender = actor, Zone = zone, Rng = new Random(0),
                DirectionX = 1, DirectionY = 0,
            });

            var pos = zone.GetEntityPosition(actor);
            Assert.AreEqual((7, 5), (pos.x, pos.y),
                "Vault must skip over the wall and land at distance 2.");
        }

        [Test]
        public void Vault_LandingOccupiedByCreature_RejectedAndEmitsDiag()
        {
            var actor = MakeBodied();
            var skill = new Acrobatics_Vault();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var zone = new Zone();
            zone.AddEntity(actor, 5, 5);
            var blocker = MakeBodied("blocker");
            zone.AddEntity(blocker, 7, 5); // landing cell occupied

            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext
            {
                Attacker = actor, Defender = actor, Zone = zone, Rng = new Random(0),
                DirectionX = 1, DirectionY = 0,
            });

            var pos = zone.GetEntityPosition(actor);
            Assert.AreEqual((5, 5), (pos.x, pos.y), "Actor must not move when landing is occupied.");
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("landing_occupied", recs[0].PayloadJson);
        }

        [Test]
        public void Vault_LandingIsSolid_RejectedAndEmitsDiag()
        {
            var actor = MakeBodied();
            var skill = new Acrobatics_Vault();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var zone = new Zone();
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(MakeWall(), 7, 5); // landing IS the wall

            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext
            {
                Attacker = actor, Defender = actor, Zone = zone, Rng = new Random(0),
                DirectionX = 1, DirectionY = 0,
            });

            var pos = zone.GetEntityPosition(actor);
            Assert.AreEqual((5, 5), (pos.x, pos.y));
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("landing_blocked", recs[0].PayloadJson);
        }

        [Test]
        public void Vault_NoDirection_EmitsDiag()
        {
            var actor = MakeBodied();
            var skill = new Acrobatics_Vault();
            actor.GetPart<SkillsPart>().AddSkill(skill, source: "test");
            var zone = new Zone();
            zone.AddEntity(actor, 5, 5);
            Diag.ResetAll();
            skill.OnCommand(new SkillEventContext
            {
                Attacker = actor, Defender = actor, Zone = zone, Rng = new Random(0),
                DirectionX = 0, DirectionY = 0,
            });
            var recs = DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "SkillRejected", Limit = 5 }).Records;
            StringAssert.Contains("no_direction", recs[0].PayloadJson);
        }
    }
}
