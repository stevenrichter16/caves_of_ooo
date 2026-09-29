using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using Random = System.Random;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadHuntLiveGapTests
 {
  SpreadExplorationActorTests.Scope scope;EntityFactory factory,oldCorpse;Entity h,p,player;Zone z;SpreadPredatorPart role;BrainPart brain;NorthRng rng;bool oldAI;System.Collections.Generic.HashSet<string> prior;
  public sealed class NorthRng:Random{public int Calls;public override int Next(int max){Calls++;return max-1;}}
  public sealed class CorpseRoll:Random{readonly int roll;public CorpseRoll(int value){roll=value;}public override int Next(int max)=>roll;}
  [SetUp] public void Setup()
  {
   scope=new SpreadExplorationActorTests.Scope();prior=Diag.Snapshot(8192).Select(e=>e.TraceId).ToHashSet();oldAI=Diag.IsChannelEnabled("ai");Diag.SetChannel("ai",true);oldCorpse=CorpsePart.Factory;
   FactionManager.Initialize(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Data/Factions.json")));
   factory=new EntityFactory();string candidate=Environment.GetEnvironmentVariable("COO_FURROWSTALKER_BLUEPRINTS");factory.LoadBlueprints(File.ReadAllText(string.IsNullOrEmpty(candidate)?Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json"):candidate));
   z=new Zone("Overworld.8.8.0");h=Place("Furrowstalker",14,6);p=Place("ReedbackGrazer",14,10);player=Place("Player",60,20);role=h.GetPart<SpreadPredatorPart>();brain=p.GetPart<BrainPart>();rng=new NorthRng();brain.Rng=rng;CorpsePart.Factory=factory;
  }
  [TearDown]public void Cleanup(){CorpsePart.Factory=oldCorpse;Diag.SetChannel("ai",oldAI);scope.Dispose();}
  Entity Place(string bp,int x,int y){var e=factory.CreateEntity(bp);Assert.NotNull(e,bp);Assert.True(z.AddEntity(e,x,y));var b=e.GetPart<BrainPart>();if(b!=null){b.CurrentZone=z;b.Rng=new Random(1);}return e;}
  static void Turn(Entity e)=>e.FireEventAndRelease(GameEvent.New("TakeTurn"));
  void Pair(){Assert.True(role.Configure(z,p));Assert.True(z.MoveEntity(h,14,8));}
  GoalHandler Wander(string mode)
  {var bored=new BoredGoal();brain.PushGoal(bored);if(mode=="random"){var w=new WanderRandomlyGoal();bored.PushChildGoal(w);return w;}if(mode=="shaken"){Assert.True(p.ApplyEffect(new WitnessedEffect(20)));return brain.PeekGoal();}var d=new WanderDurationGoal(20);bored.PushChildGoal(d);return d;}
  Diag.Entry[] Records()=>Diag.Snapshot(8192).Where(e=>!prior.Contains(e.TraceId)&&e.Category=="ai"&&e.ActorId==h.ID&&e.Kind.StartsWith("SpreadHunt")).ToArray();
  [Serializable]public sealed class Payload{public string reason,phase;}
  static string Reason(Diag.Entry e)=>JsonUtility.FromJson<Payload>(e.PayloadJson).reason;
  [TestCase("random")][TestCase("duration")][TestCase("shaken")]
  public void GenuineNearbyHunterInterruptsOnlyPacingStepAndPreservesItsOwner(string mode)
  {Pair();var goal=Wander(mode);var effect=p.GetEffect<WitnessedEffect>();int moves=0;EntityVisualHooks.MovedCallback=(e,zone,ox,oy,nx,ny,forced)=>{if(e==p)moves++;};Turn(p);var at=z.GetEntityPosition(p);Assert.GreaterOrEqual(AIHelpers.ChebyshevDistance(at.x,at.y,14,8),3);Assert.AreEqual(1,moves);Assert.Zero(rng.Calls,"Flight chooses the existing safe-step path, not a random pacing direction.");Assert.Contains(goal,brain.GetGoalsSnapshot());if(goal is WanderDurationGoal)StringAssert.Contains("ticks=1/20",goal.GetDetails());if(effect!=null){Assert.AreSame(effect,p.GetEffect<WitnessedEffect>());Assert.AreEqual(20,effect.Duration);}Assert.AreEqual(2,p.GetPart<SpreadGrazerPart>().FlightRemaining);Assert.AreEqual(24,role.PursuitRemaining);}
  [TestCase("far")][TestCase("hidden")][TestCase("unpaired")][TestCase("spent")]
  public void NoCurrentVisibleNearPairLeavesOrdinaryPacingAuthoritative(string fault)
  {Pair();Wander("shaken");if(fault=="far")Assert.True(z.MoveEntity(h,14,5));if(fault=="hidden")Place("Tree",14,9);if(fault=="unpaired")p.GetPart<SpreadGrazerPart>().Hunter=null;if(fault=="spent")role.Phase=SpreadHuntPhase.Escaped;Turn(p);Assert.Greater(rng.Calls,0);Assert.Zero(p.GetPart<SpreadGrazerPart>().FlightRemaining);Assert.NotNull(p.GetEffect<WitnessedEffect>());}
  [TestCase("calm")][TestCase("work")][TestCase("follow")][TestCase("combat")][TestCase("conversation")][TestCase("party")]
  public void HigherAuthorityStillOwnsPacingChildEvenWithHunterNear(string mode)
  {
   Pair();Wander("shaken");GoalHandler authority=null;
   if(mode=="calm")authority=new NoFightGoal(10,true);
   if(mode=="work")authority=new WaitGoal(10);
   if(mode=="follow")authority=new FollowLeaderGoal(player);
   if(mode=="combat")authority=new KillGoal(h);
   if(mode=="conversation")brain.InConversation=true;
   if(mode=="party")brain.SetPartyLeader(player);
   if(authority!=null){brain.PushGoal(authority);authority.PushChildGoal(new WanderRandomlyGoal());}
   Turn(p);Assert.Zero(p.GetPart<SpreadGrazerPart>().FlightRemaining);Assert.AreEqual(24,role.PursuitRemaining);if(authority!=null)Assert.Contains(authority,brain.GetGoalsSnapshot());
  }
  [Test]public void ShakenPacingFlightSpendsOneScheduledActionAndEffectStillTicks()
  {Pair();var pace=Wander("shaken");int moves=0;EntityVisualHooks.MovedCallback=(e,zone,ox,oy,nx,ny,forced)=>{if(e==p)moves++;};var t=new TurnManager();TurnManager.World=null;t.AddEntity(p);t.AddEntity(player);Assert.AreSame(player,t.ProcessUntilPlayerTurn());Assert.AreEqual(10,t.TickCount);Assert.Zero(t.GetEnergy(p));Assert.AreEqual(1,moves);Assert.AreEqual(19,p.GetEffect<WitnessedEffect>().Duration);StringAssert.Contains("ticks=1/20",pace.GetDetails());Assert.AreEqual(2,p.GetPart<SpreadGrazerPart>().FlightRemaining);}
  [TestCase(false)][TestCase(true)]public void ReplacementSaveKeepsPacingOwnerAndExactVisiblePair(bool unpaired)
  {
   Pair();Wander("shaken");if(unpaired)p.GetPart<SpreadGrazerPart>().Hunter=null;string hi=h.ID,pi=p.ID;
   var manager=OverworldZoneManager.CreateDetached(factory,64);manager.SetActiveZone(z);var state=GameSessionState.Capture("hunt-pacing","prototype",manager,new TurnManager(),player);
   using(var bytes=new MemoryStream()){state.Save(new SaveWriter(bytes));bytes.Position=0;var loaded=GameSessionState.Load(new SaveReader(bytes,factory));z=loaded.ZoneManager.ActiveZone;player=loaded.Player;}
   h=z.GetReadOnlyEntities().Single(e=>e.ID==hi);p=z.GetReadOnlyEntities().Single(e=>e.ID==pi);role=h.GetPart<SpreadPredatorPart>();brain=p.GetPart<BrainPart>();rng=new NorthRng();brain.Rng=rng;brain.CurrentZone=z;h.GetPart<BrainPart>().CurrentZone=z;
   var goal=brain.GetGoalsSnapshot().OfType<WanderDurationGoal>().Single();var effect=p.GetEffect<WitnessedEffect>();Assert.NotNull(effect);Turn(p);
   Assert.AreEqual(unpaired?0:2,p.GetPart<SpreadGrazerPart>().FlightRemaining);Assert.AreEqual(unpaired,rng.Calls>0);Assert.Contains(goal,brain.GetGoalsSnapshot());Assert.AreSame(effect,p.GetEffect<WitnessedEffect>());StringAssert.Contains("ticks=1/20",goal.GetDetails());
  }
  [Test]public void WitnessedEffectStillRemovesItsOwnPacingGoalAfterFlight()
  {Pair();var goal=Wander("shaken");Turn(p);Assert.Contains(goal,brain.GetGoalsSnapshot());Assert.True(p.RemoveEffect<WitnessedEffect>());Assert.False(brain.GetGoalsSnapshot().Contains(goal));Assert.Null(p.GetEffect<WitnessedEffect>());Assert.AreEqual(2,p.GetPart<SpreadGrazerPart>().FlightRemaining);}
  [Test]public void HiddenLastThreatMemoryDoesNotTakeOwnershipOfPacing()
  {Pair();Wander("shaken");var grazer=p.GetPart<SpreadGrazerPart>();grazer.FlightRemaining=2;grazer.ThreatX=14;grazer.ThreatY=8;Place("Tree",14,9);Turn(p);Assert.Greater(rng.Calls,0);Assert.AreEqual(2,grazer.FlightRemaining);}
  [TestCase(true)][TestCase(false)]public void AdmissionDiagnosticsArePredicatePairedAndUseTopLevelOwners(bool valid)
  {if(!valid)brain.CurrentZone=new Zone("foreign");Assert.AreEqual(valid,role.Configure(z,p));var e=Records().Single(x=>x.Kind=="SpreadHuntAdmission");Assert.AreEqual(valid?"pair-admitted":"pair-ineligible",Reason(e));Assert.AreEqual(h.ID,e.ActorId);Assert.AreEqual(p.ID,e.TargetId);StringAssert.DoesNotContain("actorId",e.PayloadJson);Assert.AreEqual(valid,role.Configured);}
  [TestCase(true)][TestCase(false)]public void DisabledDiagnosticsDoNotChangeHuntOrCreateRecords(bool enabled)
  {Diag.SetChannel("ai",enabled);Pair();Turn(h);var records=Records();Assert.AreEqual(enabled?2:0,records.Length);Assert.AreEqual(23,role.PursuitRemaining);Assert.AreEqual(SpreadHuntPhase.Pursuing,role.Phase);Assert.AreEqual((14,9),z.GetEntityPosition(h));}
  [Test]public void SightLossAndReacquisitionRecordOncePerTransitionWithoutSearchSpam()
  {Assert.True(role.Configure(z,p));Turn(h);var trees=Enumerable.Range(0,Zone.Width).Select(x=>Place("Tree",x,8)).ToArray();Turn(h);Turn(h);Assert.AreEqual(1,Records().Count(e=>Reason(e)=="quarry-lost-sight"));foreach(var tree in trees)z.RemoveEntity(tree);Turn(h);Assert.AreEqual(1,Records().Count(e=>Reason(e)=="quarry-reacquired"));Assert.AreEqual(1,Records().Count(e=>Reason(e)=="quarry-seen"));}
  [TestCase("exhausted")][TestCase("removed")][TestCase("leash")]
  public void TerminalOutcomeKeepsExactTargetAndReasonOnce(string fault)
  {Pair();if(fault=="exhausted")role.PursuitRemaining=0;if(fault=="removed")z.RemoveEntity(p);if(fault=="leash")Assert.True(z.MoveEntity(h,30,8));Turn(h);Turn(h);var e=Records().Single(x=>x.Kind=="SpreadHuntOutcome");Assert.AreEqual(fault=="exhausted"?"pursuit-budget-spent":fault=="removed"?"prey-unavailable":"outside-home-leash",Reason(e));Assert.AreEqual(p.ID,e.TargetId);Assert.Null(role.Prey);}
  [TestCase(69,true)][TestCase(70,false)]public void FeedingDiagnosticsDescribeActualNativeReceiptAndConsumption(int roll,bool created)
  {
   Pair();Assert.True(z.MoveEntity(p,14,9));h.GetPart<MeleeWeaponPart>().BaseDamage="1d1+99";h.GetPart<MeleeWeaponPart>().PenBonus=10;h.GetStat("Strength").BaseValue=100;h.GetStat("Agility").BaseValue=100;p.GetPart<CorpsePart>().TestRng=new CorpseRoll(roll);
   for(int i=0;i<24&&!CombatSystem.IsDeathHandled(p);i++)Turn(h);Assert.True(CombatSystem.IsDeathHandled(p),"Native strong-strike fixture must reach requested lifecycle branch.");
   var corpse=p.GetPart<CorpsePart>().CreatedCorpse;if(created){Assert.NotNull(corpse);var claim=Records().Single(e=>Reason(e)=="own-kill-meal-claimed");Assert.AreEqual(corpse.ID,claim.TargetId);for(int i=0;i<2;i++)Turn(h);Assert.False(Records().Any(e=>Reason(e)=="meal-consumed"));Assert.NotNull(z.GetEntityCell(corpse));Turn(h);Assert.Null(z.GetEntityCell(corpse));var outcome=Records().Single(e=>Reason(e)=="meal-consumed");Assert.AreEqual(corpse.ID,outcome.TargetId);Assert.AreEqual("Fed",JsonUtility.FromJson<Payload>(outcome.PayloadJson).phase);}else{Assert.Null(corpse);Assert.False(Records().Any(e=>Reason(e)=="own-kill-meal-claimed"));var outcome=Records().Single(e=>Reason(e)=="own-kill-no-valid-meal");Assert.AreEqual(p.ID,outcome.TargetId);}
  }
 }
}
