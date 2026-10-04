using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Authored melee commits a cell ray, leaving a real response and recovery turn.</summary>
    public sealed class CommittedMeleeTests
    {
        DensityCombatFixture f;
        Entity actor, target;
        CommittedMeleePart committed;
        BrainPart brain;
        [SetUp] public void Setup()
        {
            f = new DensityCombatFixture();
            actor = f.Actor(skills: ""); target = f.Target(12, 10);
            actor.AddPart(committed = new CommittedMeleePart { AttackName = "stone swing", Reach = 2 });
            brain = actor.GetPart<BrainPart>(); brain.Rng = new MeleeRandom();
            brain.PushGoal(new KillGoal(target));
        }
        [TearDown] public void TearDown() => f.Dispose();
        void Turn()
        {
            actor.FireEventAndRelease(GameEvent.New("TakeTurn"));
            actor.FireEventAndRelease(GameEvent.New("EndTurn"));
        }
        int HP => target.GetStatValue("Hitpoints");

        [Test] public void FirstActionWindsUpSecondResolvesOneNormalAttackThirdRecovers()
        {
            var count = new AttackCounter(); actor.AddPart(count);
            int dv = CombatSystem.GetDV(actor);
            Turn(); Assert.True(committed.IsWindingUp); Assert.AreEqual(500, HP); Assert.Zero(count.Count);
            Assert.True(committed.ThreatensCell(f.Zone, 11, 10)); Assert.True(committed.ThreatensCell(f.Zone, 12, 10));
            Assert.False(committed.ThreatensCell(f.Zone, 12, 11));
            Turn(); Assert.True(committed.IsRecovering); Assert.Less(HP, 500); Assert.AreEqual(1, count.Count);
            Assert.AreEqual(dv - 2, CombatSystem.GetDV(actor)); int after = HP;
            Turn(); Assert.False(committed.IsRecovering); Assert.AreEqual(after, HP); Assert.AreEqual(1, count.Count);
            Assert.AreEqual(dv, CombatSystem.GetDV(actor)); Assert.AreEqual(0, actor.GetStatValue("DV"));
        }
        [Test] public void SidestepLeavesOriginalRayEmptyWithoutTrackingTarget()
        {
            Turn(); f.Zone.MoveEntity(target, 12, 11); Turn();
            Assert.AreEqual(500, HP); Assert.True(committed.IsRecovering);
        }
        [Test] public void NewBodyInCommittedRayInterceptsExactlyOneStrike()
        {
            Turn(); var interceptor = f.Actor(11, 10, ""); var count = new AttackCounter(); actor.AddPart(count);
            Turn(); Assert.Less(interceptor.GetStatValue("Hitpoints"), 500); Assert.AreEqual(500, HP); Assert.AreEqual(1, count.Count);
        }
        [TestCase("wall")] [TestCase("door")] [TestCase("physics")]
        public void SolidInterpositionSpendsAttackWithoutDamagingPropOrTarget(string kind)
        {
            Turn(); var prop = new Entity();
            prop.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 99, Max = 99 };
            if (kind == "wall") prop.SetTag("Solid");
            if (kind == "door") prop.AddPart(new DoorPart { IsOpen = false });
            if (kind == "physics") prop.AddPart(new PhysicsPart { Solid = true });
            f.Zone.AddEntity(prop, 11, 10); Turn();
            Assert.AreEqual(500, HP); Assert.AreEqual(99, prop.GetStatValue("Hitpoints")); Assert.True(committed.IsRecovering);
        }
        [Test] public void ThreatReadoutStopsAtCurrentPhysicalInterception()
        {
            Turn(); Assert.True(committed.ThreatensCell(f.Zone, 12, 10));
            var prop = new Entity(); prop.AddPart(new PhysicsPart { Solid = true }); f.Zone.AddEntity(prop, 11, 10);
            Assert.False(committed.ThreatensCell(f.Zone, 11, 10)); Assert.False(committed.ThreatensCell(f.Zone, 12, 10));
            f.Zone.RemoveEntity(prop); var interceptor = f.Actor(11, 10, "");
            Assert.True(committed.ThreatensCell(f.Zone, 11, 10)); Assert.False(committed.ThreatensCell(f.Zone, 12, 10));
            f.Zone.RemoveEntity(interceptor); Assert.True(committed.ThreatensCell(f.Zone, 12, 10));
        }
        [Test] public void NonSolidLooseItemDoesNotStopTheMeleeRay()
        {
            Turn(); var item = new Entity(); item.AddPart(new PhysicsPart { Solid = false, Takeable = true });
            f.Zone.AddEntity(item, 11, 10); Turn(); Assert.Less(HP, 500);
        }
        [TestCase(12, 11)] [TestCase(13, 10)]
        public void OffRayOrOutOfRangeCannotStart(int x, int y)
        {
            f.Zone.MoveEntity(target, x, y); Assert.False(committed.TryBegin(target, f.Zone)); Assert.False(committed.IsWindingUp);
        }
        [Test] public void ClearExactTargetIsRequiredBeforeCommitting()
        {
            var ally = f.Actor(11, 10, ""); Assert.False(committed.TryBegin(target, f.Zone));
            f.Zone.RemoveEntity(ally); Assert.True(committed.TryBegin(target, f.Zone));
        }
        [TestCase("stun")] [TestCase("freeze")] [TestCase("paralysis")] [TestCase("sleep")]
        public void AppliedDisablingEffectCancelsEvenWhenRemovedBeforeNextAction(string kind)
        {
            Turn(); Effect effect = kind == "stun" ? new StunnedEffect() : kind == "freeze" ? new FrozenEffect()
                : kind == "paralysis" ? new ParalyzedEffect() : (Effect)new AsleepByGasEffect();
            Assert.True(actor.ApplyEffect(effect)); Assert.False(committed.IsWindingUp); Assert.True(committed.IsRecovering);
            actor.GetPart<StatusEffectsPart>().RemoveEffect(effect); Turn();
            Assert.AreEqual(500, HP); Assert.False(committed.IsRecovering);
            Turn(); Assert.True(committed.IsWindingUp); Assert.AreEqual(500, HP);
        }
        [Test] public void RefusedStatusDoesNotInterruptCommitment()
        {
            actor.AddPart(new RefuseEffect()); Turn(); Assert.False(actor.ApplyEffect(new StunnedEffect()));
            Assert.True(committed.IsWindingUp); Turn(); Assert.Less(HP, 500);
        }
        [Test] public void HarmlessAppliedStatusDoesNotInterruptCommitment()
        {
            Turn(); Assert.True(actor.ApplyEffect(new WetEffect())); Assert.True(committed.IsWindingUp);
            Turn(); Assert.Less(HP, 500);
        }
        [Test] public void DisplacementCancelsAndConsumesThatActionWithoutMovingAgain()
        {
            Turn(); f.Zone.MoveEntity(actor, 10, 11); Turn();
            Assert.AreEqual(500, HP); Assert.AreEqual((10, 11), f.Zone.GetEntityPosition(actor)); Assert.False(committed.IsWindingUp);
        }
        [TestCase("calm")] [TestCase("conversation")] [TestCase("party")] [TestCase("lost-target")]
        public void LosingHostileIntentCancelsWithoutAnExtraAction(string cause)
        {
            Turn();
            if (cause == "calm") brain.PushGoal(new NoFightGoal(1));
            if (cause == "conversation") brain.InConversation = true;
            if (cause == "party") brain.SetPartyLeader(target);
            if (cause == "lost-target") brain.Target = null;
            Turn(); Assert.AreEqual(500, HP); Assert.False(committed.IsWindingUp);
        }
        [Test] public void UnrelatedNewGoalDoesNotBypassPendingResolutionOrRecovery()
        {
            Turn(); brain.ClearGoals(); brain.PushGoal(new WanderRandomlyGoal()); Turn();
            Assert.Less(HP, 500); Assert.True(committed.IsRecovering); var pos = f.Zone.GetEntityPosition(actor);
            Turn(); Assert.AreEqual(pos, f.Zone.GetEntityPosition(actor));
        }
        [Test] public void ActorWithoutPartKeepsImmediateAdjacentMelee()
        {
            actor.RemovePart(committed); f.Zone.MoveEntity(target, 11, 10); Turn(); Assert.Less(HP, 500);
        }
        [Test] public void PlayerGetsAnInputOpportunityBetweenWindupAndResolutionAtSpeedSeventy()
        {
            var previous = TurnManager.Active;
            try
            {
                actor.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 70, Max = 200 };
                var turns = new TurnManager(); turns.AddEntity(target); turns.AddEntity(actor);
                for (int i = 0; i < 6 && !committed.IsWindingUp; i++)
                { Assert.AreSame(target, turns.ProcessUntilPlayerTurn()); if (!committed.IsWindingUp) turns.EndTurn(target, f.Zone); }
                Assert.True(committed.IsWindingUp); Assert.AreEqual(500, HP);
                Assert.AreSame(target, turns.CurrentActor); Assert.True(turns.WaitingForInput); Assert.AreEqual(500, HP);
                f.Zone.MoveEntity(target, 12, 11); turns.EndTurn(target, f.Zone);
                Assert.AreSame(target, turns.ProcessUntilPlayerTurn()); Assert.AreEqual(500, HP);
            }
            finally { typeof(TurnManager).GetProperty("Active").SetValue(null, previous); }
        }
        [Test] public void WindupSaveLoadPreservesDirectionAndOriginalTargetReference()
        {
            using (var save = new FirstHourAmbushSaveTests.Fixture("CurationHalfSet", false))
            {
                save.Zone.MoveEntity(save.Player, 22, 10);
                save.Player.SetTag("Creature");
                if (!save.Actor.HasPart<CommittedMeleePart>()) save.Actor.AddPart(new CommittedMeleePart { Reach = 2 });
                var p = save.Actor.GetPart<CommittedMeleePart>();
                save.Brain.SetPersonallyHostile(save.Player); Assert.True(p.TryBegin(save.Player, save.Zone));
                save.RoundTrip(); p = save.Actor.GetPart<CommittedMeleePart>();
                Assert.True(p.IsWindingUp); Assert.True(p.ThreatensCell(save.Zone, 22, 10));
                Assert.AreSame(save.Player, p.OriginalTarget);
                save.Zone.MoveEntity(save.Player, 22, 11); int hp = save.Player.GetStatValue("Hitpoints"); save.Turn();
                Assert.True(p.IsRecovering); Assert.AreEqual(hp, save.Player.GetStatValue("Hitpoints"));
            }
        }
        [Test] public void VisibleWarningAndTransitionsHaveDiagnosticsWithoutHiddenWarnings()
        {
            bool enabled = Diag.IsChannelEnabled("ai");
            try
            {
                Diag.SetChannel("ai", true); MessageLog.Clear(); Turn();
                Assert.False(MessageLog.GetAllEntries().Any(m => m.Text.Contains("stone swing")));
                Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "ai", Kind = "CommittedMeleeWindup", Actor = actor.ID }).Records.Count);
                Turn(); Turn(); f.Zone.GetEntityCell(actor).IsVisible = true; MessageLog.Clear(); Turn();
                Assert.True(MessageLog.GetAllEntries().Any(m => m.Text.Contains("stone swing")));
                Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "ai", Kind = "CommittedMeleeResolved", Actor = actor.ID }).Records.Count);
            }
            finally { Diag.SetChannel("ai", enabled); }
        }
        [Test] public void ZoneChangeNeverResolvesTheSavedWindup()
        {
            Turn(); f.Zone.RemoveEntity(actor); var other = new Zone("committed-other-zone"); other.AddEntity(actor, 10, 10);
            brain.CurrentZone = other; Turn(); Assert.False(committed.IsWindingUp); Assert.AreEqual(500, HP);

        }
        [Test] public void ForcedDisplacementOutAndBackStillInterrupts()
        {
            Turn(); Assert.True(MovementSystem.ForceMoveTo(actor, f.Zone, 10, 11));
            Assert.True(MovementSystem.ForceMoveTo(actor, f.Zone, 10, 10)); Turn();
            Assert.AreEqual(500, HP); Assert.False(committed.IsWindingUp);
        }
        [Test] public void DeathClearsARealPendingWindup()
        {
            Turn(); Assert.True(committed.IsWindingUp);
            actor.GetStat("Hitpoints").BaseValue = 0; actor.FireEventAndRelease(GameEvent.New("Died"));
            Assert.False(committed.IsWindingUp); Assert.False(committed.IsRecovering);
            Assert.AreEqual(500, HP);
        }
        [Test] public void SavedStationaryTargetStillReceivesTheCommittedStrike()
        {
            using (var save = new FirstHourAmbushSaveTests.Fixture("CurationHalfSet", false))
            {
                save.Zone.MoveEntity(save.Player, 22, 10); save.Player.SetTag("Creature");
                if (!save.Actor.HasPart<CommittedMeleePart>()) save.Actor.AddPart(new CommittedMeleePart());
                var p = save.Actor.GetPart<CommittedMeleePart>(); save.Brain.SetPersonallyHostile(save.Player);
                Assert.True(p.TryBegin(save.Player, save.Zone)); save.RoundTrip();
                p = save.Actor.GetPart<CommittedMeleePart>(); save.Brain.Rng = new MeleeRandom();
                int hp = save.Player.GetStatValue("Hitpoints"); save.Turn();
                Assert.Less(save.Player.GetStatValue("Hitpoints"), hp); Assert.True(p.IsRecovering);
            }
        }
        [Test] public void BothHandsNeverTurnOneCommitmentIntoTwoWeaponStrikes()
        {
            bool enabled = Diag.IsChannelEnabled("damage");
            try
            {
                Diag.SetChannel("damage", true);
                actor.Statistics["MultiWeaponSkillBonus"] = new Stat { Name = "MultiWeaponSkillBonus", BaseValue = 100, Max = 200 };
                var body = actor.GetPart<Body>();
                // Creature's normal humanoid defaults provide two real fist weapons.
                Assert.GreaterOrEqual(body.GetParts().Count(p => p.Type == "Hand" && p._DefaultBehavior != null), 2);
                Turn(); Turn();
                Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "damage", Kind = "HitRoll", Actor = actor.ID }).Records.Count);
                actor.RemovePart(committed); f.Zone.MoveEntity(target, 11, 10); Turn();
                Assert.AreEqual(3, DiagQuery.Apply(new DiagQuery.Filter { Category = "damage", Kind = "HitRoll", Actor = actor.ID }).Records.Count);
            }
            finally { Diag.SetChannel("damage", enabled); }
        }
        sealed class MeleeRandom : System.Random
        {
            public override int Next(int maxValue) => maxValue - 1;
            public override int Next(int minValue, int maxValue) => maxValue == 11 ? 9 : maxValue - 1;
        }
        sealed class AttackCounter : Part
        { public int Count; public override bool HandleEvent(GameEvent e) { if (e.ID == "BeforeMeleeAttack") Count++; return true; } }
        sealed class RefuseEffect : Part
        { public override bool HandleEvent(GameEvent e) => e.ID != "BeforeApplyEffect"; }
    }
}
