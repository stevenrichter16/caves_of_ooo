using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class ComposableEncounterTests
 {
  sealed class AttackCounter:Part {public int Count;public override bool HandleEvent(GameEvent e){if(e.ID=="BeforeMeleeAttack")Count++;return true;}}
  DensityCombatFixture f;Entity actor,target;BrainPart brain;WorksiteGuardPart guard;
  [SetUp] public void Setup(){f=new DensityCombatFixture();actor=f.Actor(skills:"");target=f.Target(13,10);brain=actor.GetPart<BrainPart>();brain.Wanders=brain.WandersRandomly=false;actor.AddPart(guard=new WorksiteGuardPart{ZoneID=f.Zone.ZoneID,HomeX=10,HomeY=10,PursuitRadius=4});}
  [TearDown] public void Cleanup()=>f.Dispose();
  void Turn()=>actor.FireEventAndRelease(GameEvent.New("TakeTurn"));
  KillGoal Chase(){brain.ClearGoals();var g=new KillGoal(target);brain.PushGoal(g);return g;}
  [TestCase(true)][TestCase(false)] public void LocalGuardStopsOutsideItsBoundsWhileOrdinaryMeleePursues(bool duty)
  {if(!duty)actor.RemovePart(guard);f.Zone.MoveEntity(target,16,10);var g=Chase();Turn();Assert.AreEqual(duty,g.Abandoned);Assert.AreEqual(duty?(10,10):(11,10),f.Zone.GetEntityPosition(actor));}
  [Test] public void LocalThreatIsPursuedAndAttacked(){Chase();Turn();Assert.AreEqual((11,10),f.Zone.GetEntityPosition(actor));f.Zone.MoveEntity(target,12,10);var c=new AttackCounter();actor.AddPart(c);Turn();Assert.AreEqual(1,c.Count);}
  [Test] public void WithdrawalDropsPursuitThenReturnsByOneOrdinaryStep(){var g=Chase();Turn();f.Zone.MoveEntity(target,16,10);Turn();Assert.True(g.Abandoned);Turn();Assert.AreEqual((10,10),f.Zone.GetEntityPosition(actor));Assert.AreEqual(500,target.GetStatValue("Hitpoints"));}
  [Test] public void HiddenOutsideMotionDoesNotReplaceRememberedLocalPosition(){var g=Chase();Turn();for(int y=0;y<Zone.Height;y++)f.Zone.TileState.WriteCloud(12,y,"smoke",20);f.Zone.MoveEntity(target,17,14);Turn();Assert.False(g.Abandoned);Assert.AreEqual(13,g.LastSeenX);Assert.AreEqual(10,g.LastSeenY);Assert.AreEqual((12,10),f.Zone.GetEntityPosition(actor));}
  [Test] public void IdleDoesNotRepeatedlyReacquireVisibleDistantThreat(){f.Zone.MoveEntity(target,16,10);for(int i=0;i<5;i++)Turn();Assert.Null(brain.Target);Assert.False(brain.HasGoal<KillGoal>());Assert.AreEqual((10,10),f.Zone.GetEntityPosition(actor));}
  [Test] public void LowHealthFleeHasPriorityOverDuty(){f.Zone.MoveEntity(target,16,10);brain.FleeThreshold=.4f;actor.GetStat("Hitpoints").BaseValue=20;Turn();Assert.True(brain.HasGoal<FleeGoal>());}
  [Test] public void PartyMembershipDisablesLocalLeash(){brain.SetPartyLeader(f.Actor(8,8,""));Assert.False(guard.Applies(brain,f.Zone));var g=Chase();f.Zone.MoveEntity(target,16,10);Turn();Assert.False(g.Abandoned);Assert.AreEqual((11,10),f.Zone.GetEntityPosition(actor));}
  [TestCase("peace")][TestCase("conversation")]
  public void SuppressedDutyDoesNotMoveOrAcquire(string mode){if(mode=="peace")brain.PushGoal(new NoFightGoal(3));if(mode=="conversation")brain.InConversation=true;Assert.False(guard.Applies(brain,f.Zone));Turn();Assert.AreEqual((10,10),f.Zone.GetEntityPosition(actor));Assert.Null(brain.Target);}
  [Test] public void WrongZoneYieldsToOrdinaryAi(){guard.ZoneID="other";Assert.False(guard.Applies(brain,f.Zone));Chase();Turn();Assert.AreEqual((11,10),f.Zone.GetEntityPosition(actor));}
  [Test] public void BlockedReturnDoesNotTeleportOrRefill(){f.Zone.MoveEntity(actor,15,10);f.Zone.MoveEntity(target,25,10);actor.GetStat("Hitpoints").BaseValue=100;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(dx!=0||dy!=0)f.Wall(15+dx,10+dy);for(int i=0;i<3;i++)Turn();Assert.AreEqual((15,10),f.Zone.GetEntityPosition(actor));Assert.AreEqual(100,actor.GetStatValue("Hitpoints"));Assert.Null(brain.Target);}
  [TestCase(true)][TestCase(false)] public void LargeTargetDetourCannotStepAcrossDutyBoundary(bool duty)
  {
   if(!duty)actor.RemovePart(guard);f.Zone.MoveEntity(actor,10,6);f.Zone.RemoveEntity(target);target.AddPart(new SpatialFootprintPart{CellsRaw="0,0;0,1"});Assert.True(f.Zone.AddEntity(target,12,6));
   foreach(var p in new[]{(11,6),(11,7),(10,7),(9,7),(9,6)})f.Actor(p.Item1,p.Item2,"");
   var path=FindPath.ToContact(f.Zone,actor,target);Assert.True(path.Usable);Assert.Less(6+path.Steps[0].dy,6,"fixture must offer only an outward first detour");
   var g=Chase();Turn();Assert.False(g.Abandoned);Assert.AreEqual(duty?6:5,f.Zone.GetEntityPosition(actor).y);
  }
  [TestCase(true)][TestCase(false)] public void PassiveGuardOnlyInitiatesAgainstItsPersonalEnemy(bool personal)
  {brain.Passive=true;if(personal)brain.SetPersonallyHostile(target);Turn();Assert.AreEqual(personal,brain.HasGoal<KillGoal>());}
  [Test] public void HigherWaitGoalKeepsDutyFromTakingAnotherAction()
  {f.Zone.MoveEntity(actor,15,10);brain.ClearGoals();brain.PushGoal(new WaitGoal(2));Turn();Assert.AreEqual((15,10),f.Zone.GetEntityPosition(actor));Assert.Null(brain.Target);}
  [Test] public void DisplacedGuardReturnsWithoutAttackingAdjacentIntruder()
  {f.Zone.MoveEntity(actor,16,10);f.Zone.MoveEntity(target,17,10);Chase();Turn();Assert.AreEqual(500,target.GetStatValue("Hitpoints"));Turn();Assert.AreEqual((15,10),f.Zone.GetEntityPosition(actor));}
  sealed class EndCounter:Part {public int Count;public override bool HandleEvent(GameEvent e){if(e.ID=="EndTurn")Count++;return true;}}
  [Test] public void ScheduledReturnSpendsOneActorCompletionPerStep()
  {
   var previous=TurnManager.Active;
   try{f.Zone.MoveEntity(actor,16,10);f.Zone.MoveEntity(target,25,10);var counter=new EndCounter();actor.AddPart(counter);actor.GetStat("Hitpoints").BaseValue=100;
    var turns=new TurnManager();turns.AddEntity(target);turns.AddEntity(actor);Assert.AreSame(target,turns.ProcessUntilPlayerTurn());int before=counter.Count;
    turns.EndTurn(target,f.Zone);Assert.AreSame(target,turns.ProcessUntilPlayerTurn());Assert.AreEqual(before+1,counter.Count);Assert.AreEqual((15,10),f.Zone.GetEntityPosition(actor));Assert.AreEqual(100,actor.GetStatValue("Hitpoints"));
   }finally{typeof(TurnManager).GetProperty("Active").SetValue(null,previous);}
  }
  [Test] public void SavedWorldResumesOrdinaryReturnWithInjurySupplyAndCooldown()
  {
   using(var save=new FirstHourAmbushSaveTests.Fixture("MarlbackCindercaller",false))
   {
    save.Brain.FleeThreshold=0;save.Actor.AddPart(new WorksiteGuardPart{ZoneID=save.Zone.ZoneID,HomeX=15,HomeY=10});save.Actor.GetStat("Hitpoints").BaseValue=8;
    var tonic=f.Factory.CreateEntity("HealingTonic");Assert.True(save.Actor.GetPart<InventoryPart>().AddObject(tonic));string id=tonic.ID;
    var ability=save.Actor.GetPart<ActivatedAbilitiesPart>().AbilityList.First();ability.CooldownRemaining=5;var aid=ability.ID;
    save.RoundTrip();save.Turn();Assert.AreEqual((19,10),save.Zone.GetEntityPosition(save.Actor));Assert.AreEqual(8,save.Actor.GetStatValue("Hitpoints"));Assert.AreEqual(5,save.Actor.GetPart<ActivatedAbilitiesPart>().GetAbility(aid).CooldownRemaining);Assert.AreEqual(1,save.Actor.GetPart<InventoryPart>().Objects.Count(e=>e.ID==id));
   }
  }
  [Test] public void CurrentSavePreservesAnchorInjuryAndActualCooldown(){var caster=f.Actor(20,10);caster.AddPart(new WorksiteGuardPart{ZoneID=f.Zone.ZoneID,HomeX=19,HomeY=10,PursuitRadius=4});caster.GetStat("Hitpoints").BaseValue=120;DensityCombatFixture.Ability(caster).CooldownRemaining=5;var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(caster);var p=loaded.GetPart<WorksiteGuardPart>();Assert.AreEqual(f.Zone.ZoneID,p.ZoneID);Assert.AreEqual(19,p.HomeX);Assert.AreEqual(10,p.HomeY);Assert.AreEqual(4,p.PursuitRadius);Assert.AreEqual(120,loaded.GetStatValue("Hitpoints"));Assert.AreEqual(5,DensityCombatFixture.Ability(loaded).CooldownRemaining);}
 }
}
