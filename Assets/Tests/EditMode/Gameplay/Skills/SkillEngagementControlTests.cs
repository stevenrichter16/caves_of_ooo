using System;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class SkillEngagementControlTests
    {
        [SetUp] public void Setup() { SkillRegistry.ResetForTests(); MessageLog.Clear(); }
        [TearDown] public void Cleanup() { SkillRegistry.ResetForTests(); }

        private static Entity Creature(string id)
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Creature"] = "";
            foreach (var pair in new[] { ("Hitpoints", 100), ("DV", 5), ("Toughness", 16), ("Ego", 18), ("SP", 5) })
                e.Statistics[pair.Item1] = new Stat { Owner = e, Name = pair.Item1, BaseValue = pair.Item2, Min = 0, Max = 100 };
            e.AddPart(new PhysicsPart { Solid = true });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new ActivatedAbilitiesPart());
            e.AddPart(new SkillsPart());
            return e;
        }

        [Test] public void DecapitatePurchase_RequiresDismember_ThenSpendsOnePoint()
        {
            var actor = Creature("buyer");
            actor.GetPart<SkillsPart>().AddSkill(new AxeSkill());
            Assert.AreEqual(BuySkillAction.FailureReason.MissingPrereq,
                BuySkillAction.Execute(actor, "Axe_Decapitate").Reason);
            Assert.AreEqual(5, actor.GetStatValue("SP"));
            Assert.IsFalse(actor.GetPart<SkillsPart>().HasSkill("Axe_Decapitate"));
            actor.GetPart<SkillsPart>().AddSkill(new Axe_Dismember());
            Assert.IsTrue(BuySkillAction.Execute(actor, "Axe_Decapitate").Succeeded);
            Assert.AreEqual(4, actor.GetStatValue("SP"));
        }

        [Test] public void DismissPurchase_RequiresRecruit_ThenCostsNoPoints()
        {
            var actor = Creature("buyer");
            actor.GetPart<SkillsPart>().AddSkill(new PersuasionSkill());
            Assert.AreEqual(BuySkillAction.FailureReason.MissingPrereq,
                BuySkillAction.Execute(actor, "Persuasion_Dismiss").Reason);
            actor.GetPart<SkillsPart>().AddSkill(new Persuasion_Recruit());
            actor.GetStat("SP").BaseValue = 0;
            actor.GetStat("Ego").BaseValue = 8; // managing an existing companion needs no fresh persuasion check
            Assert.IsTrue(BuySkillAction.Execute(actor, "Persuasion_Dismiss").Succeeded);
            Assert.AreEqual(0, actor.GetStatValue("SP"));
        }

        private static (Entity caster, Entity target, Zone zone) CalmFixture()
        {
            var zone = new Zone("calm-engagement");
            var caster = Creature("caster"); caster.Tags["Player"] = "";
            var target = Creature("target"); target.AddPart(new BrainPart());
            zone.AddEntity(caster, 5, 5); zone.AddEntity(target, 7, 5);
            caster.GetPart<SkillsPart>().AddSkill(new Spellcraft_Calm());
            var command = GameEvent.New("CommandCalm");
            command.SetParameter("Zone", (object)zone);
            command.SetParameter("RNG", (object)new Random(11));
            command.SetParameter("SourceCell", (object)zone.GetEntityCell(caster));
            command.SetParameter("DirectionX", 1); command.SetParameter("DirectionY", 0);
            command.SetParameter("Range", 6);
            caster.FireEvent(command);
            Assert.IsTrue(command.Handled); command.Release();
            Assert.IsTrue(target.GetPart<BrainPart>().HasGoal<NoFightGoal>());
            Assert.AreEqual(100, target.GetStatValue("Hitpoints"));
            return (caster, target, zone);
        }

        [TestCase(true)] [TestCase(false)]
        public void Calm_ActualHarmBreaksPeace_FromAttacksOrEnvironment(bool sourced)
        {
            var f = CalmFixture();
            CombatSystem.ApplyDamage(f.target, new Damage(1), sourced ? f.caster : null, f.zone);
            Assert.AreEqual(99, f.target.GetStatValue("Hitpoints"));
            Assert.IsFalse(f.target.GetPart<BrainPart>().HasGoal<NoFightGoal>());
        }

        [Test] public void Calm_ZeroAndFullyResistedDamageKeepPeace()
        {
            var f = CalmFixture();
            CombatSystem.ApplyDamage(f.target, new Damage(0), f.caster, f.zone);
            f.target.Statistics["HeatResistance"] = new Stat { Owner = f.target, Name = "HeatResistance", BaseValue = 100, Max = 100 };
            var fire = new Damage(12); fire.AddAttribute("Fire");
            CombatSystem.ApplyDamage(f.target, fire, f.caster, f.zone);
            Assert.AreEqual(100, f.target.GetStatValue("Hitpoints"));
            Assert.IsTrue(f.target.GetPart<BrainPart>().HasGoal<NoFightGoal>());
        }

        [Test] public void Damage_DoesNotBreakQuestPacifism()
        {
            var target = Creature("quest-witness"); target.AddPart(new BrainPart());
            target.GetPart<BrainPart>().PushGoal(new NoFightGoal());
            CombatSystem.ApplyDamage(target, new Damage(3), null, null);
            Assert.AreEqual(97, target.GetStatValue("Hitpoints"));
            Assert.IsTrue(target.GetPart<BrainPart>().HasGoal<NoFightGoal>());
        }

        private sealed class AbsorbFinalDamagePart : Part
        {
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "TakeDamage") e.GetParameter<Damage>("Damage").Amount = 0;
                return true;
            }
        }

        [Test] public void Calm_LateDamageCancellationDoesNotBreakPeace()
        {
            var f = CalmFixture(); f.target.AddPart(new AbsorbFinalDamagePart());
            CombatSystem.ApplyDamage(f.target, new Damage(9), f.caster, f.zone);
            Assert.AreEqual(100, f.target.GetStatValue("Hitpoints"));
            Assert.IsTrue(f.target.GetPart<BrainPart>().HasGoal<NoFightGoal>());
        }

        [TestCase(true)] [TestCase(false)]
        public void SaveGraph_PreservesFragileCalmAndDurableQuestPeace(bool fragile)
        {
            var f = CalmFixture();
            if (!fragile)
            {
                f.target.GetPart<BrainPart>().ClearGoals();
                f.target.GetPart<BrainPart>().PushGoal(new NoFightGoal(50));
            }
            using (var stream = new System.IO.MemoryStream())
            {
                var writer = new SaveWriter(stream);
                writer.WriteEntityReference(f.target); writer.WriteQueuedEntityBodies();
                stream.Position = 0;
                var reader = new SaveReader(stream, new CavesOfOoo.Data.EntityFactory());
                var loaded = reader.ReadEntityReference(); reader.ReadEntityBodies();
                Assert.IsTrue(loaded.GetPart<BrainPart>().HasGoal<NoFightGoal>());
                CombatSystem.ApplyDamage(loaded, new Damage(1), null, null);
                Assert.AreEqual(99, loaded.GetStatValue("Hitpoints"));
                Assert.AreEqual(!fragile, loaded.GetPart<BrainPart>().HasGoal<NoFightGoal>());
            }
        }

        [Test] public void Bludgeon_RepeatedProcsCannotBankAnEncounterOfStun()
        {
            var target = Creature("victim"); var attacker = Creature("attacker");
            var damage = new Damage(1); damage.AddAttribute("Cudgel");
            var skill = new Cudgel_Bludgeon();
            for (int seed = 0; seed < 100; seed++)
                skill.OnAttackerAfterAttack(new SkillEventContext { Attacker = attacker, Defender = target,
                    Damage = damage, ActualDamage = 1, Rng = new Random(seed) });
            var stun = target.GetEffect<StunnedEffect>();
            Assert.IsNotNull(stun, "Non-vacuous: procs occurred across seeded hits");
            TestContext.WriteLine("100 passive hit opportunities banked " + stun.Duration + " turns of stun");
            Assert.LessOrEqual(stun.Duration, 2, "This passive must leave recovery opportunities");
            Assert.Greater(stun.SaveTarget, 0, "Toughness must help against the passive");
        }

        [Test] public void Bludgeon_DoesNotExtendAnActiveConkStun()
        {
            var target = Creature("victim"); target.ApplyEffect(new StunnedEffect(4));
            var damage = new Damage(1); damage.AddAttribute("Cudgel");
            for (int seed = 0; seed < 50; seed++)
                new Cudgel_Bludgeon().OnAttackerAfterAttack(new SkillEventContext {
                    Defender = target, Damage = damage, ActualDamage = 1, Rng = new Random(seed) });
            Assert.AreEqual(4, target.GetEffect<StunnedEffect>().Duration);
            Assert.AreEqual(0, target.GetEffect<StunnedEffect>().SaveTarget, "Do not alter the active's guarantee");
        }

        [TestCase("Cutting", 1)] [TestCase("Cudgel", 0)]
        public void Bludgeon_RequiresItsWeaponFamilyAndActualDamage(string attribute, int actual)
        {
            var target = Creature("victim"); var damage = new Damage(1); damage.AddAttribute(attribute);
            for (int seed = 0; seed < 100; seed++)
                new Cudgel_Bludgeon().OnAttackerAfterAttack(new SkillEventContext {
                    Defender = target, Damage = damage, ActualDamage = actual, Rng = new Random(seed) });
            Assert.IsFalse(target.HasEffect<StunnedEffect>());
        }

        [Test] public void Lunge_IsAvailableWithinTenTurns_WithoutExtendingReach()
        {
            var spec = new LongBlades_Lunge().DeclareActivatedAbility(Creature("duelist"));
            Assert.LessOrEqual(spec.Cooldown, 10, "Reach should recur within a sustained encounter");
            Assert.AreEqual(2, spec.Range);
        }

        [Test] public void HobbledReadout_ExplainsActualDefensePenalty()
        {
            var text = EffectDescriber.Describe(new HobbledEffect(6));
            StringAssert.Contains("DV", text);
            StringAssert.DoesNotContain("slowed", text);
        }
    }
}
