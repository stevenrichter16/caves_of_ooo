using System;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class CompanionDirectCombatTests
    {
        [SetUp] public void SetUp() { FactionManager.Initialize(); MessageLog.Clear(); }

        internal sealed class DirectSpell : SpellSkillPart
        {
            public Entity[] Targets; public bool Succeeds = true;
            public Action AfterDamage;
            public override string Name => "CompanionDirectTestSpell";
            protected override bool ResolveSpell(SkillEventContext ctx)
            {
                foreach (var target in Targets)
                    SpellDamageHelpers.ApplySpellDamage(target, 3, "Heat", ctx.Attacker, ctx.Zone);
                AfterDamage?.Invoke();
                return Succeeds;
            }
        }
        internal static bool Cast(CompanionCombatTests.Fixture f, params Entity[] targets)
            => new DirectSpell { Targets = targets }.OnCommand(Context(f));
        internal static SkillEventContext Context(CompanionCombatTests.Fixture f)
            => new SkillEventContext { Attacker = f.Player, Zone = f.Zone, DirectionX = 1, Rng = new Random(42) };

        [Test] public void CommittedDirectSpellRalliesWithoutFreeAttack()
        {
            var f = new CompanionCombatTests.Fixture(); var position = f.Zone.GetEntityPosition(f.Follower);
            Assert.True(Cast(f, f.Enemy));
            Assert.AreEqual(97, f.Enemy.GetStatValue("Hitpoints"));
            Assert.AreSame(f.Enemy, CompanionCombatTests.Fight(f.Follower)?.Target);
            Assert.AreEqual(position, f.Zone.GetEntityPosition(f.Follower));
            Assert.AreEqual(2, f.Brain.GoalCount);
        }
        [Test] public void RealEmberSpitCommandRalliesAndPaysItsCooldown()
        {
            var f = new CompanionCombatTests.Fixture(); f.Player.AddPart(new ActivatedAbilitiesPart());
            f.Player.AddPart(new SkillsPart()); var skill = new Pyromancy_EmberSpit();
            f.Player.GetPart<SkillsPart>().AddSkill(skill, "test");
            var e = GameEvent.New("CommandEmberSpit"); e.SetParameter("Zone", (object)f.Zone);
            e.SetParameter("RNG", (object)new Random(42)); e.SetParameter("DirectionX", 1); e.SetParameter("DirectionY", 0);
            f.Player.FireEvent(e); bool handled = e.Handled; e.Release();
            Assert.True(handled); Assert.Less(f.Enemy.GetStatValue("Hitpoints"), 100);
            Assert.AreSame(f.Enemy, CompanionCombatTests.Fight(f.Follower)?.Target);
            Assert.AreEqual(Pyromancy_EmberSpit.COOLDOWN, f.Player.GetPart<ActivatedAbilitiesPart>().GetAbility(skill.ActivatedAbilityID).CooldownRemaining);
        }
        [Test] public void FailedSpellDoesNotRallyEvenIfItsResolverDamagedSomeone()
        {
            var f = new CompanionCombatTests.Fixture();
            Assert.False(new DirectSpell { Targets = new[] { f.Enemy }, Succeeds = false }.OnCommand(Context(f)));
            Assert.AreEqual(97, f.Enemy.GetStatValue("Hitpoints")); Assert.Null(CompanionCombatTests.Fight(f.Follower));
            Assert.True(Cast(f, f.Enemy)); Assert.NotNull(CompanionCombatTests.Fight(f.Follower));
        }
        [Test] public void DamageHelperOutsideActiveCastDoesNotCreateOffensiveOrder()
        {
            var f = new CompanionCombatTests.Fixture();
            SpellDamageHelpers.ApplySpellDamage(f.Enemy, 3, "Heat", f.Player, f.Zone);
            Assert.AreEqual(97, f.Enemy.GetStatValue("Hitpoints")); Assert.Null(CompanionCombatTests.Fight(f.Follower));
            Cast(f, f.Enemy); Assert.NotNull(CompanionCombatTests.Fight(f.Follower));
        }
        [Test] public void AttributedEnvironmentalDamageDoesNotCreateOffensiveOrder()
        {
            var f = new CompanionCombatTests.Fixture(); CombatSystem.ApplyDamage(f.Enemy, 3, f.Player, f.Zone);
            Assert.Null(CompanionCombatTests.Fight(f.Follower)); Cast(f, f.Enemy); Assert.NotNull(CompanionCombatTests.Fight(f.Follower));
        }
        [Test] public void MultipleTargetsChooseFirstEligibleAndNeverReplaceExistingFight()
        {
            var f = new CompanionCombatTests.Fixture(); var other = CompanionCombatTests.Actor(f.Zone, "other", 7, 6, "OutlandRaiders");
            Cast(f, f.Enemy, other); var original = CompanionCombatTests.Fight(f.Follower);
            Assert.AreSame(f.Enemy, original?.Target); Cast(f, other); Assert.AreSame(original, CompanionCombatTests.Fight(f.Follower));
            Assert.AreEqual(2, f.Brain.GoalCount);
        }
        [Test] public void HiddenFirstTargetDoesNotPreventAssistingAgainstVisibleSecondTarget()
        {
            var f = new CompanionCombatTests.Fixture(); f.Enemy.GetPart<RenderPart>().Visible = false;
            var other = CompanionCombatTests.Actor(f.Zone, "other", 7, 6, "OutlandRaiders");
            Cast(f, f.Enemy, other); Assert.AreSame(other, CompanionCombatTests.Fight(f.Follower)?.Target);
        }
        [Test] public void GuaranteedWeaponSkillDamageAlsoRallies()
        {
            var f = new CompanionCombatTests.Fixture();
            Assert.AreEqual(3, SkillCombatHelpers.DealGuaranteedHitDamage(f.Player, f.Enemy, null, 3, f.Zone, new Random(42)));
            Assert.AreSame(f.Enemy, CompanionCombatTests.Fight(f.Follower)?.Target);
        }
        [Test] public void PositiveLungeHitRalliesWhereASingleStrikeMissDoesNot()
        {
            var f = new CompanionCombatTests.Fixture(); var inv = new InventoryPart(); f.Player.AddPart(inv);
            var body = new Body(); f.Player.AddPart(body); body.SetBody(AnatomyFactory.CreateHumanoid());
            var sword = new Entity { ID = "sword", BlueprintName = "test-long-blade" }; sword.SetTag("Item");
            sword.AddPart(new PhysicsPart { Takeable = true }); sword.AddPart(new EquippablePart { Slot = "Hand" });
            var weapon = new MeleeWeaponPart { BaseDamage = "1d1", Attributes = "Cutting LongBlades", HitBonus = -1000, PenBonus = 10 };
            sword.AddPart(weapon); Assert.True(inv.EquipToBodyPart(sword, body.GetParts().Find(p => p.Type == "Hand")));
            var skill = new LongBlades_Lunge();
            Assert.True(skill.OnCommand(Context(f))); Assert.Null(CompanionCombatTests.Fight(f.Follower));
            weapon.HitBonus = 1000; var ctx = Context(f); ctx.Rng = new Random(42);
            Assert.True(skill.OnCommand(ctx)); Assert.Less(f.Enemy.GetStatValue("Hitpoints"), 100);
            Assert.AreSame(f.Enemy, CompanionCombatTests.Fight(f.Follower)?.Target);
        }
    }
}
