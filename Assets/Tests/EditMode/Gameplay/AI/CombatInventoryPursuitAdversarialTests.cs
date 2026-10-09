using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Cross-observer, saved-state and alternative AI-entry probes for cover.</summary>
    public sealed class CombatInventoryPursuitAdversarialTests
    {
        DensityCombatFixture f;
        [SetUp] public void Setup() { f = new DensityCombatFixture(); Diag.ResetAll(); }
        [TearDown] public void Cleanup() => f.Dispose();
        static void Set(object owner,string name,object value)=>CombatInventoryPursuitTests.Set(owner,name,value);
        static T Field<T>(object owner,string name)=>CombatInventoryPursuitTests.Field<T>(owner,name);
        KillGoal Hunt(Entity actor, Entity target)
        { var brain=actor.GetPart<BrainPart>();brain.Wanders=brain.WandersRandomly=false;var goal=new KillGoal(target);brain.PushGoal(goal);return goal; }
        void Turn(Entity actor)=>actor.FireEventAndRelease(GameEvent.New("TakeTurn"));
        void Screen(int x) { for(int y=0;y<Zone.Height;y++)f.Zone.TileState.WriteCloud(x,y,"smoke",20); }

        [TestCase(0)][TestCase(-1)][TestCase(int.MinValue)]
        public void NonpositiveSavedBudgetCannotStartAnotherHiddenSearch(int budget)
        {
            var actor=f.Actor(skills:"");var target=f.Target(16,10);var goal=Hunt(actor,target);
            Set(goal,"HasLastSeen",true);Set(goal,"LastSeenX",16);Set(goal,"LastSeenY",10);Set(goal,"SearchRemaining",budget);
            Screen(11);Turn(actor);Assert.True(goal.Finished());Assert.AreEqual((10,10),f.Zone.GetEntityPosition(actor));
        }
        [TestCase(-1,10)][TestCase(80,10)][TestCase(10,-1)][TestCase(10,25)]
        public void InvalidRememberedCoordinatesAbandonWithoutMoving(int x,int y)
        {
            var actor=f.Actor(skills:"");var target=f.Target(16,10);var goal=Hunt(actor,target);
            Set(goal,"HasLastSeen",true);Set(goal,"LastSeenX",x);Set(goal,"LastSeenY",y);Set(goal,"SearchRemaining",6);
            Screen(11);Turn(actor);Assert.True(goal.Finished());Assert.AreEqual((10,10),f.Zone.GetEntityPosition(actor));
        }
        [Test] public void OversizedSavedAllowanceIsClampedBeforeTheFirstHiddenAction()
        {
            var actor=f.Actor(skills:"");var target=f.Target(16,10);var goal=Hunt(actor,target);
            Set(goal,"HasLastSeen",true);Set(goal,"LastSeenX",16);Set(goal,"LastSeenY",10);Set(goal,"SearchRemaining",int.MaxValue);
            Screen(11);Turn(actor);Assert.AreEqual(5,Field<int>(goal,"SearchRemaining"));
        }
        [Test] public void LosingOneTargetDoesNotClearAnotherGoalsNewTarget()
        {
            var actor=f.Actor(skills:"");var target=f.Target(16,10);var other=f.Target(10,15);
            var goal=Hunt(actor,target);actor.GetPart<BrainPart>().Target=other;Screen(11);goal.TakeAction();
            Assert.True(goal.Finished());Assert.AreSame(other,actor.GetPart<BrainPart>().Target);
        }
        [Test] public void AbandoningSearchDoesNotForgivePersonalHostility()
        {
            var actor=f.Actor(skills:"");var target=f.Target(16,10);var brain=actor.GetPart<BrainPart>();
            brain.SetPersonallyHostile(target);var goal=Hunt(actor,target);Screen(11);Turn(actor);
            Assert.True(goal.Finished());Assert.Null(brain.Target);Assert.True(brain.IsPersonallyHostileTo(target));
        }
        [Test] public void DifferentObserversHaveIndependentSightAndMemory()
        {
            var a=f.Actor(10,10,"");var b=f.Actor(18,10,"");var target=f.Target(15,10);
            var ga=Hunt(a,target);var gb=Hunt(b,target);Turn(a);Turn(b);Screen(12);f.Zone.MoveEntity(target,15,13);
            Turn(a);Turn(b);Assert.AreEqual(10,Field<int>(ga,"LastSeenY"));Assert.AreEqual(13,Field<int>(gb,"LastSeenY"));
            Assert.True(Field<bool>(ga,"Searching"));Assert.False(Field<bool>(gb,"Searching"));
        }
        [Test] public void HiddenFreshGoalCannotStartAReachCommitment()
        {
            var actor=f.Actor(skills:"");var target=f.Target(12,10);var committed=new CommittedMeleePart{Reach=2};actor.AddPart(committed);
            var goal=Hunt(actor,target);Screen(11);Turn(actor);Assert.False(committed.IsWindingUp);Assert.True(goal.Finished());
        }
        [Test] public void ScreenRemovalLetsAnIdleObserverAcquireAgainWithoutExistingGoalMemory()
        {
            var actor=f.Actor(skills:"");var target=f.Target(16,10);var goal=Hunt(actor,target);Screen(11);Turn(actor);
            Assert.True(goal.Finished());for(int y=0;y<Zone.Height;y++)f.Zone.TileState.Clear(11,y);Turn(actor);
            Assert.True(actor.GetPart<BrainPart>().HasGoal<KillGoal>());Assert.AreEqual((11,10),f.Zone.GetEntityPosition(actor));
        }
        [Test] public void RemovingAnActorOrZoneDoesNotCrashTheGoal()
        {
            var actor=f.Actor(skills:"");var target=f.Target();var goal=Hunt(actor,target);f.Zone.RemoveEntity(actor);
            Assert.DoesNotThrow(()=>goal.TakeAction());Assert.True(goal.Finished());
        }
        [Test] public void MissingBrainAndTargetCannotCrashLifecycleCleanup()
        {
            var goal=new KillGoal(null);Assert.DoesNotThrow(()=>goal.TakeAction());Assert.DoesNotThrow(()=>goal.OnPop());
        }
        [Test] public void ClearingTheBrainPopsKnowledgeWithoutChangingHostility()
        {
            var actor=f.Actor(skills:"");var target=f.Target();var brain=actor.GetPart<BrainPart>();brain.SetPersonallyHostile(target);
            Hunt(actor,target);brain.ClearGoals();Assert.Null(brain.Target);Assert.True(brain.IsPersonallyHostileTo(target));
        }
        [TestCase("Cryomancy_IceLance")][TestCase("Pyromancy_EmberSpit")][TestCase("Corrosion_AcidSpray")][TestCase("Galvanism_ArcBolt")]
        public void AuthoredProjectileFamiliesCannotAimAtHiddenLiveCoordinates(string skill)
        {
            var actor=f.Actor(skills:skill);var target=f.Target();Screen(11);
            Assert.False(f.Cast(actor,target));Assert.Zero(DensityCombatFixture.Ability(actor).CooldownRemaining);
        }
        [Test] public void ATargetOutsideSightDoesNotReceiveDirectAuthoredRangedAim()
        {
            var actor=f.Actor();var target=f.Target();actor.GetPart<BrainPart>().SightRadius=2;
            Assert.False(f.Cast(actor,target));actor.GetPart<BrainPart>().SightRadius=3;Assert.True(f.Cast(actor,target));
        }
        [Test] public void FakeGasTagOnAnOrdinaryObjectDoesNotMakeItProjectileTransparent()
        {
            var actor=f.Actor(skills:"");var target=f.Target(16,10);var objectInWay=new Entity();objectInWay.SetTag("Gas");
            objectInWay.AddPart(new PhysicsPart{Takeable=true});f.Zone.AddEntity(objectInWay,12,10);
            Assert.AreSame(objectInWay,LineTargeting.TraceFirstImpactToTarget(f.Zone,actor,10,10,16,10,10).HitEntity);
        }
        [TestCase(false)][TestCase(true)]
        public void SightTransitionsHaveDiagnosticsAndOnlyVisibleObserversSpeak(bool actorVisible)
        {
            bool previous=Diag.IsChannelEnabled("ai");
            try
            {
                Diag.SetChannel("ai",true);var actor=f.Actor(skills:"");var target=f.Target(16,10);Hunt(actor,target);Turn(actor);
                Screen(12);f.Zone.GetEntityCell(actor).IsVisible=actorVisible;MessageLog.Clear();Turn(actor);
                Assert.AreEqual(1,DiagQuery.Apply(new DiagQuery.Filter{Category="ai",Kind="PursuitLostSight",Actor=actor.ID}).Records.Count);
                Assert.AreEqual(actorVisible,MessageLog.GetAllEntries().Any(m=>m.Text.Contains("loses sight")));
                for(int y=0;y<Zone.Height;y++)f.Zone.TileState.Clear(12,y);Turn(actor);
                Assert.AreEqual(1,DiagQuery.Apply(new DiagQuery.Filter{Category="ai",Kind="PursuitReacquired",Actor=actor.ID}).Records.Count);
            }
            finally {Diag.SetChannel("ai",previous);}
        }
        [Test] public void VisibleLargeBodyContactSuppliesMemoryWithoutLeakingHiddenAnchor()
        {
            var actor=f.Actor(skills:"");var target=f.Target(14,10);f.Zone.RemoveEntity(target);
            target.AddPart(new SpatialFootprintPart{CellsRaw="0,0;-3,0"});Assert.True(f.Zone.AddEntity(target,14,10));Screen(12);
            Assert.False(AIHelpers.HasLineOfSight(f.Zone,10,10,14,10));var goal=Hunt(actor,target);Turn(actor);
            Assert.False(goal.Finished());Assert.AreEqual(11,Field<int>(goal,"LastSeenX"));Assert.AreEqual(10,Field<int>(goal,"LastSeenY"));
        }
        [TestCase(false)][TestCase(true)]
        public void HiddenLowHealthRetreatStillYieldsToOneCarriedTreatment(bool medicine)
        {
            var actor=f.Actor(skills:"");var target=f.Target(16,10);var goal=Hunt(actor,target);Turn(actor);
            var brain=actor.GetPart<BrainPart>();brain.FleeThreshold=.25f;actor.GetStat("Hitpoints").BaseValue=10;
            if(medicine){actor.AddPart(new FieldMedicinePart());Assert.True(actor.GetPart<InventoryPart>().AddObject(f.Factory.CreateEntity("HealingTonic")));}
            Screen(12);f.Zone.MoveEntity(target,16,14);Turn(actor);
            Assert.AreEqual((11,10),f.Zone.GetEntityPosition(actor));Assert.AreEqual(medicine,brain.HasGoal<KillGoal>());
            if(medicine){Assert.Greater(actor.GetStatValue("Hitpoints"),10);Assert.AreEqual(5,Field<int>(goal,"SearchRemaining"));}
            else Assert.Null(brain.Target);
        }
        [TestCase(false)][TestCase(true)]
        public void VisibleLargeBodyDoesNotLicenseAimAtAnotherHiddenBodyContact(bool hideEast)
        {
            var actor=f.Actor();var target=f.Target(13,10);f.Zone.RemoveEntity(target);
            target.AddPart(new SpatialFootprintPart{CellsRaw="0,0;0,1;0,2;0,3"});Assert.True(f.Zone.AddEntity(target,13,10));
            if(hideEast)f.Zone.TileState.WriteCloud(12,10,"smoke",4);
            var aim=new AimProbe();actor.AddPart(aim);actor.Parts.Remove(aim);actor.Parts.Insert(0,aim);
            Assert.True(f.Cast(actor,target));Assert.AreEqual(1,aim.Dx);Assert.AreEqual(hideEast?1:0,aim.Dy);
        }
        [TestCase(false)][TestCase(true)]
        public void FollowerKeepsFollowingInsteadOfRepeatedlyJoiningAnUnseenFight(bool hidden)
        {
            var follower=f.Actor(10,10,"");var leader=f.Actor(14,10,"");var target=f.Target(17,10);
            Hunt(leader,target);leader.GetPart<BrainPart>().Target=target;
            var brain=follower.GetPart<BrainPart>();brain.PushGoal(new FollowLeaderGoal(leader));if(hidden)Screen(12);
            Turn(follower);Assert.AreEqual((11,10),f.Zone.GetEntityPosition(follower));Assert.AreEqual(!hidden,brain.HasGoal<KillGoal>());
        }
        sealed class AimProbe:Part
        {
            public int Dx,Dy;
            public override bool HandleEvent(GameEvent e)
            {if(e.ID=="CommandIceLance"){Dx=e.GetIntParameter("DirectionX");Dy=e.GetIntParameter("DirectionY");}return true;}
        }
    }
}
