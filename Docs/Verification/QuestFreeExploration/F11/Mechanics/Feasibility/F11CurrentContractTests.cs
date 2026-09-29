using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 // Private feasibility probes. Pins current behavior, not proposed F11 behavior.
 [TestFixture] public sealed class F11CurrentContractTests
 {
  EntityFactory factory; Zone zone;
  sealed class FixedRoll : Random { readonly int roll; public FixedRoll(int n){roll=n;} public override int Next(int maxValue)=>roll; }
  public sealed class EndProbe : Part { public override string Name=>"F11PrivateEndProbe";public int Ends;public override bool HandleEvent(GameEvent e){if(e.ID=="EndTurn")Ends++;return true;} }
  [SetUp] public void Setup(){MessageLog.OnMessage=null;MessageLog.Clear();TurnManager.World=null;FactionManager.Initialize(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Data/Factions.json")));factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));zone=new Zone("Overworld.8.8.0");CorpsePart.Factory=factory;LootDropSystem.Factory=null;}
  [TearDown] public void TearDown(){CorpsePart.Factory=null;LootDropSystem.Factory=null;LootDropSystem.Rng=null;TurnManager.World=null;}
  Entity Place(string bp,int x,int y){var e=factory.CreateEntity(bp);Assert.True(zone.AddEntity(e,x,y));var b=e.GetPart<BrainPart>();if(b!=null){b.CurrentZone=zone;b.Rng=new Random(7);}return e;}
  static void Turn(Entity e)=>e.FireEventAndRelease(GameEvent.New("TakeTurn"));
  [TestCase(false,false)][TestCase(true,false)][TestCase(false,true)]
  public void HealthyGrazerRecognizesPlayerOrPersonalThreatButNotAlliedBeast(bool player,bool personal)
  {
   var grazer=Place("ReedbackGrazer",10,10);var threat=Place(player?"Player":"ReedbackGrazer",12,10);
   if(personal)grazer.GetPart<BrainPart>().SetPersonallyHostile(threat,false);
   if(!player&&!personal)Assert.False(FactionManager.IsHostile(grazer,threat));
   Turn(grazer);Assert.AreEqual(player||personal?(9,10):(10,10),zone.GetEntityPosition(grazer));Assert.AreEqual(10,grazer.GetStatValue("Hitpoints"));
  }
  [TestCase("Hedge",true)][TestCase("Tree",false)]
  public void RealSightCoverDiffersFromPhysicalOnlyBarrier(string bp,bool seen)
  {
   var grazer=Place("ReedbackGrazer",10,10);Place("Player",12,10);Place(bp,11,10);
   Assert.True(zone.GetCell(11,10).BlocksMovement());Assert.AreEqual(seen,AIHelpers.HasLineOfSight(zone,10,10,12,10));
   Turn(grazer);Assert.AreEqual(seen?(9,10):(10,10),zone.GetEntityPosition(grazer));
  }
  [TestCase(false)][TestCase(true)] public void CardinalGreedyFlightDoesNotSearchAvailableSideExit(bool blocked)
  {
   var grazer=Place("ReedbackGrazer",10,10);Place("Player",12,10);if(blocked)Place("Tree",9,10);
   Assert.False(zone.GetCell(9,9).BlocksMovement());Assert.False(zone.GetCell(10,9).BlocksMovement());
   Turn(grazer);Assert.AreEqual(blocked?(10,10):(9,10),zone.GetEntityPosition(grazer));
  }
  [TestCase(10,true)][TestCase(2,false)] public void FleeGoalCannotProvideHealthyProximityFlight(int hp,bool finished)
  {
   var grazer=Place("ReedbackGrazer",10,10);var threat=Place("Player",12,10);grazer.GetStat("Hitpoints").BaseValue=hp;
   var goal=new FleeGoal(threat){ParentBrain=grazer.GetPart<BrainPart>()};Assert.AreEqual(finished,goal.Finished());
  }
  [TestCase(8,9)][TestCase(12,11)] public void GenericKillGoalUsesHiddenCurrentPosition(int targetY,int nextY)
  {
   var hunter=Place("ReedbackGrazer",10,10);var prey=Place("ReedbackGrazer",14,targetY);
   for(int y=1;y<Zone.Height-1;y++)Place("Tree",12,y);
   Assert.False(AIHelpers.HasLineOfSight(zone,10,10,14,targetY));var goal=new KillGoal(prey){ParentBrain=hunter.GetPart<BrainPart>()};
   Assert.False(goal.Finished());goal.TakeAction();Assert.AreEqual((11,nextY),zone.GetEntityPosition(hunter));
   zone.RemoveEntity(prey);Assert.True(goal.Finished());
  }
  [TestCase("none",true)][TestCase("calm",false)][TestCase("work",false)][TestCase("conversation",false)][TestCase("party",false)]
  public void HigherPriorityStatePreemptsHealthyGrazerRole(string mode,bool flees)
  {
   var grazer=Place("ReedbackGrazer",10,10);var player=Place("Player",12,10);var brain=grazer.GetPart<BrainPart>();
   if(mode=="calm")brain.PushGoal(new NoFightGoal(10));if(mode=="work")brain.PushGoal(new WaitGoal(10));if(mode=="conversation")brain.InConversation=true;
   if(mode=="party"){brain.SetPartyLeader(player);brain.PushGoal(new FollowLeaderGoal(player));}
   Turn(grazer);Assert.AreEqual(flees?(9,10):(10,10),zone.GetEntityPosition(grazer));
  }
  [Test] public void ExistingGrazerRoleTakesOnePaidSchedulerAction()
  {
   var grazer=Place("ReedbackGrazer",10,10);var player=Place("Player",12,10);var probe=new EndProbe();grazer.AddPart(probe);var turns=new TurnManager();turns.AddEntity(grazer);turns.AddEntity(player);
   Assert.AreSame(player,turns.ProcessUntilPlayerTurn());Assert.AreEqual((9,10),zone.GetEntityPosition(grazer));Assert.AreEqual(1,probe.Ends);Assert.AreEqual(0,turns.GetEnergy(grazer));
  }
  [TestCase(69,true)][TestCase(70,false)] public void NativeSeventyPercentCorpseHasExactProvenanceAndNoHarvest(int roll,bool created)
  {
   var prey=Place("ReedbackGrazer",10,10);var killer=Place("Player",11,10);var c=prey.GetPart<CorpsePart>();Assert.AreEqual(70,c.CorpseChance);Assert.True(string.IsNullOrEmpty(c.HarvestBlueprint));c.TestRng=new FixedRoll(roll);
   CombatSystem.HandleDeath(prey,killer,zone);var corpses=zone.GetReadOnlyEntities().Where(e=>e.HasTag("Corpse")).ToArray();Assert.AreEqual(created?1:0,corpses.Length);
   if(created){var body=corpses.Single();Assert.AreEqual("ReedbackGrazerCorpse",body.BlueprintName);Assert.AreEqual(prey.ID,body.GetProperty("SourceID"));Assert.AreEqual(killer.ID,body.GetProperty("KillerID"));Assert.False(body.HasPart<HarvestablePart>());Assert.True(body.GetPart<PhysicsPart>().Takeable);}
   CombatSystem.HandleDeath(prey,killer,zone);Assert.AreEqual(corpses.Length,zone.GetReadOnlyEntities().Count(e=>e.HasTag("Corpse")));
  }
  [Test] public void NativeCorpseIdentityAndProvenanceSurviveReplacementSaveGraph()
  {
   var prey=Place("ReedbackGrazer",10,10);var player=Place("Player",11,10);prey.GetPart<CorpsePart>().TestRng=new FixedRoll(69);CombatSystem.HandleDeath(prey,player,zone);
   var corpse=zone.GetReadOnlyEntities().Single(e=>e.HasTag("Corpse"));var manager=OverworldZoneManager.CreateDetached(factory,64);manager.SetActiveZone(zone);var turns=new TurnManager();turns.AddEntity(player);
   var state=GameSessionState.Capture("f11-private","current-contract",manager,turns,player);GameSessionState loaded;
   using(var bytes=new MemoryStream()){state.Save(new SaveWriter(bytes));bytes.Position=0;loaded=GameSessionState.Load(new SaveReader(bytes,factory));}
   var replacement=loaded.ZoneManager.ActiveZone.GetReadOnlyEntities().Single(e=>e.ID==corpse.ID);Assert.AreNotSame(corpse,replacement);Assert.AreEqual(prey.ID,replacement.GetProperty("SourceID"));Assert.AreEqual(loaded.Player.ID,replacement.GetProperty("KillerID"));Assert.False(replacement.HasPart<HarvestablePart>());Assert.True(replacement.GetPart<PhysicsPart>().Takeable);
  }
 }
}
