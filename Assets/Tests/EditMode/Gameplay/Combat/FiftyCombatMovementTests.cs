using System;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;
using F = CavesOfOoo.Tests.MultiCellAbilityConsumerTests;

namespace CavesOfOoo.Tests
{
    public sealed class FiftyCombatMovementTests
    {
        static BaseSkillPart Skill(string name) => name == "Charge" ? (BaseSkillPart)new Cudgel_ChargingStrike() : name == "Disengage" ? new ShortBlades_Disengage() : name == "Vault" ? new Acrobatics_Vault() : new Acrobatics_Tumble();
        static bool Cast(Entity actor, BaseSkillPart skill, Zone zone, Cell target = null)
        {
            var command = GameEvent.New(skill.DeclareActivatedAbility(actor).Command);
            command.SetParameter("Zone", (object)zone); command.SetParameter("RNG", (object)new Random(17));
            command.SetParameter("SourceCell", (object)zone.GetEntityCell(actor)); command.SetParameter("DirectionX", 1);
            if (target != null) command.SetParameter("TargetCell", (object)target);
            try { actor.FireEvent(command); return command.Handled; } finally { command.Release(); }
        }
        static Entity Actor(Zone zone, BaseSkillPart skill)
        {
            var a = F.ArmedActor(skill is ShortBlades_Disengage ? "Piercing" : "Bludgeoning Cudgel");
            a.AddPart(new SkillsPart()); a.AddPart(new ActivatedAbilitiesPart());
            Assert.True(a.GetPart<SkillsPart>().AddSkill(skill, "fifty-test")); F.Place(zone, a, 10, 10); return a;
        }
        static void Wall(Zone zone, int x) { var w = new Entity(); w.SetTag("Solid"); F.Place(zone, w, x, 10); }
        [TestCase(11, 10, false)] [TestCase(12, 11, true)] [TestCase(0, 13, true)]
        public void DisengagePaysForCommittedTravelOnly(int wallX, int expectedX, bool paid)
        {
            var z = new Zone(); var s = new ShortBlades_Disengage(); var a = Actor(z, s); if (wallX > 0) Wall(z, wallX);
            Assert.AreEqual(paid, Cast(a, s, z)); Assert.AreEqual((expectedX, 10), z.GetEntityPosition(a));
            Assert.AreEqual(paid ? ShortBlades_Disengage.COOLDOWN : 0, a.GetPart<ActivatedAbilitiesPart>().GetAbility(s.ActivatedAbilityID).CooldownRemaining);
        }
        [TestCase("Charge", true)] [TestCase("Disengage", true)] [TestCase("Vault", true)] [TestCase("Tumble", true)]
        [TestCase("Charge", false)] [TestCase("Disengage", false)] [TestCase("Vault", false)] [TestCase("Tumble", false)]
        public void RootedActorCannotUseVoluntaryMobilityButUnrootedControlCan(string name, bool rooted)
        {
            var z = new Zone(); var s = Skill(name); var a = Actor(z, s);
            if (name == "Vault") Wall(z, 11);
            if (name == "Tumble") F.Place(z, F.Owner(creature: true), 11, 10);
            if (rooted) Assert.True(a.ApplyEffect(new RootedEffect(), a, z));
            Assert.AreEqual(!rooted, Cast(a, s, z, name == "Tumble" ? z.GetCell(11, 10) : null));
            Assert.AreEqual(rooted, z.GetEntityPosition(a) == (10, 10));
            Assert.AreEqual(rooted ? 0 : s.DeclareActivatedAbility(a).Cooldown, a.GetPart<ActivatedAbilitiesPart>().GetAbility(s.ActivatedAbilityID).CooldownRemaining);
        }
        [Test] public void RootedChargeStillAttacksAdjacentEnemyWithoutMoving()
        {
            var z = new Zone(); var s = new Cudgel_ChargingStrike(); var a = Actor(z, s); var enemy = F.Owner(creature: true);
            F.Place(z, enemy, 11, 10); a.ApplyEffect(new RootedEffect(), a, z);
            Assert.True(Cast(a, s, z)); Assert.Less(enemy.GetStatValue("Hitpoints"), 1000); Assert.AreEqual((10,10), z.GetEntityPosition(a));
        }
        [Test] public void RootedVictimStillAcceptsExternallyForcedMovement()
        {
            var z = new Zone(); var a = F.Owner(creature: true); F.Place(z,a,10,10); a.ApplyEffect(new RootedEffect(), null, z);
            Assert.True(MovementSystem.ForceMoveTo(a,z,11,10)); Assert.AreEqual((11,10),z.GetEntityPosition(a));
        }
        [Test] public void DisengageTrapEntryCannotRefundCommittedMovement()
        {
            var z = new Zone(); var s = new ShortBlades_Disengage(); var a = Actor(z,s);
            var trap = F.Owner(); trap.AddPart(new SpikeTrapTriggerPart { Damage = 12 }); F.Place(z,trap,11,10); Wall(z,12);
            Assert.True(Cast(a,s,z)); Assert.AreEqual(988,a.GetStatValue("Hitpoints")); Assert.Null(z.GetEntityCell(trap));
            Assert.AreEqual(ShortBlades_Disengage.COOLDOWN,a.GetPart<ActivatedAbilitiesPart>().GetAbility(s.ActivatedAbilityID).CooldownRemaining);
        }
        [Test] public void DisengageEntryReturningToStartStillPays()
        {
            var z = new Zone(); var s = new ShortBlades_Disengage(); var a = Actor(z,s); var floor = F.Owner(); F.Place(z,floor,11,10);
            floor.GetPart<MultiCellAbilityProbePart>().OnEntry = () => z.MoveEntity(a,10,10);
            Assert.True(Cast(a,s,z)); Assert.AreEqual(ShortBlades_Disengage.COOLDOWN,a.GetPart<ActivatedAbilitiesPart>().GetAbility(s.ActivatedAbilityID).CooldownRemaining);
        }
    }
}
