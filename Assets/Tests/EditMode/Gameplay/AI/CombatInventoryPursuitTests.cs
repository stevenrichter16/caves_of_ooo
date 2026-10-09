using System;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Inventory cover must change enemy knowledge, not merely its presentation.</summary>
    public sealed class CombatInventoryPursuitTests
    {
        DensityCombatFixture f;
        Entity actor, target;
        BrainPart brain;
        KillGoal goal;
        [SetUp] public void Setup()
        {
            f = new DensityCombatFixture(); actor = f.Actor(skills: ""); target = f.Target(16, 10);
            brain = actor.GetPart<BrainPart>(); brain.Wanders = brain.WandersRandomly = false;
            goal = new KillGoal(target); brain.PushGoal(goal);
        }
        [TearDown] public void Cleanup() => f.Dispose();
        void Turn() => actor.FireEventAndRelease(GameEvent.New("TakeTurn"));
        void Screen(int x = 12)
        { for (int y = 0; y < Zone.Height; y++) f.Zone.TileState.WriteCloud(x, y, "smoke", 20); }
        internal static T Field<T>(object owner, string name)
        {
            var field = owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(field, "Saved public field required: " + name); return (T)field.GetValue(owner);
        }
        internal static void Set(object owner, string name, object value)
        {
            var field = owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(field, "Saved public field required: " + name); field.SetValue(owner, value);
        }

        [Test] public void HiddenMovingTargetDoesNotRedirectNextStepFromItsLastSeenCell()
        {
            Turn(); Assert.AreEqual((11, 10), f.Zone.GetEntityPosition(actor));
            Screen(); f.Zone.MoveEntity(target, 16, 14); Turn();
            Assert.AreEqual((12, 10), f.Zone.GetEntityPosition(actor));
            Assert.AreEqual(16, Field<int>(goal, "LastSeenX")); Assert.AreEqual(10, Field<int>(goal, "LastSeenY"));
        }
        [Test] public void VisibleMovingTargetUpdatesKnowledgeAndPursuit()
        {
            Turn(); f.Zone.MoveEntity(target, 16, 14); Turn();
            Assert.AreEqual((12, 11), f.Zone.GetEntityPosition(actor));
            Assert.AreEqual(14, Field<int>(goal, "LastSeenY")); Assert.False(Field<bool>(goal, "Searching"));
        }
        [Test] public void NewOrLegacyGoalCannotInventLastSeenCoordinatesForHiddenTarget()
        {
            Screen(11); Turn(); Assert.AreEqual((10, 10), f.Zone.GetEntityPosition(actor));
            Assert.True(goal.Finished()); Assert.Null(brain.Target);
        }
        [Test] public void HiddenSearchHasSixActionsAndNeverRefreshesFromInvisibleMotion()
        {
            Turn(); Screen(); f.Zone.MoveEntity(target, 20, 14);
            // Keep the observer behind the screen to exercise the full budget.
            foreach (var d in new[] { (-1,-1), (0,-1), (1,-1), (-1,0), (1,0), (-1,1), (0,1), (1,1) })
                f.Wall(11+d.Item1, 10+d.Item2);
            for (int i = 0; i < 5; i++) { Turn(); Assert.False(goal.Finished(), "Must search for a bounded opportunity, not instantly forget."); }
            Turn(); Assert.True(goal.Finished()); Assert.Null(brain.Target);
            Assert.AreEqual(10, Field<int>(goal, "LastSeenY"));
        }
        [Test] public void ReacquisitionRefreshesMemoryAndResumesPursuit()
        {
            Turn(); Screen(); f.Zone.MoveEntity(target, 16, 14); Turn();
            for (int y = 0; y < Zone.Height; y++) f.Zone.TileState.Clear(12, y);
            Turn(); Assert.False(goal.Finished()); Assert.False(Field<bool>(goal, "Searching"));
            Assert.AreEqual(14, Field<int>(goal, "LastSeenY")); Assert.AreEqual(6, Field<int>(goal, "SearchRemaining"));
        }
        [Test] public void AdjacentTargetRemainsAttackableInsideMist()
        {
            f.Zone.MoveEntity(target, 11, 10); f.Zone.TileState.WriteCloud(10, 10, "smoke", 3);
            f.Zone.TileState.WriteCloud(11, 10, "smoke", 3); var count = new AttackCounter(); actor.AddPart(count);
            Turn(); Assert.AreEqual(1, count.Count); Assert.False(goal.Finished());
        }
        [TestCase(false)] [TestCase(true)] public void MissingOrDeadTargetFinishesWithoutMovement(bool dead)
        {
            if (dead) target.GetStat("Hitpoints").BaseValue = 0; else f.Zone.RemoveEntity(target);
            Assert.True(goal.Finished());
        }
        [Test] public void GoalRoundTripRetainsSearchBudgetAndLastSeenRatherThanCurrentHiddenCell()
        {
            Turn(); Screen(); f.Zone.MoveEntity(target, 16, 14); Turn();
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var loadedBrain = loaded.GetPart<BrainPart>(); var loadedGoal = loadedBrain.FindGoal<KillGoal>();
            Assert.NotNull(loadedGoal); Assert.AreEqual(16, Field<int>(loadedGoal, "LastSeenX"));
            Assert.AreEqual(10, Field<int>(loadedGoal, "LastSeenY")); Assert.AreEqual(5, Field<int>(loadedGoal, "SearchRemaining"));
            Assert.True(Field<bool>(loadedGoal, "Searching")); Assert.True(Field<bool>(loadedGoal, "HasLastSeen"));
            Assert.AreSame(loadedBrain.Target, loadedGoal.Target); Assert.AreSame(loadedBrain, loadedGoal.ParentBrain);
        }
        [TestCase("smoke", false)] [TestCase("steam", true)]
        public void AuthoredRangedAiRequiresVisibleTargetButTransparentCloudDoesNotBlock(string cloud, bool canCast)
        {
            var caster = f.Actor(10, 16); var victim = f.Target(13, 16); f.Zone.TileState.WriteCloud(11, 16, cloud, 3);
            Assert.AreEqual(canCast, f.Cast(caster, victim));
            Assert.AreEqual(canCast, DensityCombatFixture.Ability(caster).CooldownRemaining > 0);
        }
        [TestCase("smoke", false)] [TestCase("steam", true)]
        public void GenericRangedAiAlsoRequiresVisibleTarget(string cloud, bool canCast)
        {
            var caster = f.Actor(10, 16); var victim = f.Target(13, 16);
            caster.RemovePart(caster.GetPart<CombatTacticsPart>()); f.Zone.TileState.WriteCloud(11, 16, cloud, 3);
            Assert.AreEqual(canCast, AIHelpers.TryUseRangedAbility(caster, f.Zone, new DensityCombatFixture.LowRandom(), (10,16), (13,16)));
            Assert.AreEqual(canCast, DensityCombatFixture.Ability(caster).CooldownRemaining > 0);
        }
        [Test] public void ScreenBlocksNewAcquisitionWithoutBecomingAProjectileWall()
        {
            Screen(); Assert.Null(AIHelpers.FindNearestHostile(actor, f.Zone, 10));
            Assert.AreSame(target, LineTargeting.TraceFirstImpactToTarget(f.Zone, actor, 10, 10, 16, 10, 10).HitEntity);
        }
        [Test] public void AlreadyCommittedSwingStillResolvesItsOriginalRayThroughNewSmoke()
        {
            f.Zone.MoveEntity(target, 12, 10); var committed = new CommittedMeleePart { Reach = 2 }; actor.AddPart(committed);
            Turn(); Assert.True(committed.IsWindingUp); f.Zone.TileState.WriteCloud(11, 10, "smoke", 3);
            Turn(); Assert.True(committed.IsRecovering);
        }
        sealed class AttackCounter : Part
        {
            public override string Name => "PursuitAttackCounter"; public int Count;
            public override bool HandleEvent(GameEvent e) { if (e.ID == "BeforeMeleeAttack") Count++; return true; }
        }
    }
}
