using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios;
namespace CavesOfOoo.Tests
{
 public sealed class ReferenceGladeRouteControlTests
 {
  private Zone zone; private Entity actor,target; private ActivatedAbilitiesPart abilities; private Guid original;
  [SetUp] public void Setup()
  {
   zone=new Zone("route-control"); actor=Creature("player");target=Creature("target");
   actor.Tags["Player"]="";zone.AddEntity(actor,0,12);zone.AddEntity(target,2,12);
   target.GetPart<BrainPart>().CurrentZone=zone;target.GetPart<BrainPart>().Target=actor;zone.GetCell(2,12).IsVisible=true;
   abilities=new ActivatedAbilitiesPart();actor.AddPart(abilities);
   original=abilities.AddAbility("Calm","CommandCalm","Spellcraft",AbilityTargetingMode.DirectionLine,6);
   abilities.GetAbility(original).MaxCooldown=20;
  }
  private static Entity Creature(string id)
  {var e=new Entity{ID=id,BlueprintName=id};e.Tags["Creature"]="";e.AddPart(new PhysicsPart{Solid=true});e.AddPart(new RenderPart());e.AddPart(new BrainPart());e.Statistics["Hitpoints"]=new Stat{Owner=e,Name="Hitpoints",BaseValue=40,Min=0,Max=40};return e;}
  private NoFightGoal Calm(){var goal=new NoFightGoal(50,false);target.GetPart<BrainPart>().PushGoal(goal);return goal;}
  [Test] public void ActualReadyExactRayIsReadOnly()
  {
   var goals=target.GetPart<BrainPart>().GetGoalsSnapshot();var before=zone.GetReadOnlyEntities().Count;
   Assert.True(ReferenceGladeRouteControl.TryCalm(zone,actor,target,original,out var slot,out var dx,out var dy));
   Assert.AreEqual(0,slot);Assert.AreEqual(1,dx);Assert.AreEqual(0,dy);Assert.AreEqual(0,abilities.GetAbility(original).CooldownRemaining);
   Assert.AreEqual(before,zone.GetReadOnlyEntities().Count);CollectionAssert.AreEqual(goals,target.GetPart<BrainPart>().GetGoalsSnapshot());
  }
  [Test] public void ActiveCalmOnlySuppressesWhileExactUnfinishedTopGoal()
  {Assert.False(ReferenceGladeRouteControl.HasLiveCalm(zone,target));var goal=Calm();Assert.True(ReferenceGladeRouteControl.HasLiveCalm(zone,target));goal.Age=49;Assert.True(ReferenceGladeRouteControl.HasLiveCalm(zone,target));goal.Age=50;Assert.False(ReferenceGladeRouteControl.HasLiveCalm(zone,target));}
  [TestCase("removed")][TestCase("finished")][TestCase("buried")][TestCase("foreign-brain")][TestCase("foreign-zone")][TestCase("foreign-goal")][TestCase("wandering")][TestCase("dead")]
  public void InvalidOrEndedCalmRestoresThreat(string change)
  {
   var goal=Calm();Assert.True(ReferenceGladeRouteControl.HasLiveCalm(zone,target));var brain=target.GetPart<BrainPart>();
   switch(change){case "removed":brain.RemoveGoal(goal);break;case "finished":goal.Age=50;break;case "buried":brain.PushGoal(new BoredGoal());break;
    case "foreign-brain":brain.ParentEntity=new Entity();break;case "foreign-zone":brain.CurrentZone=new Zone("other");break;
    case "foreign-goal":goal.ParentBrain=new BrainPart();break;case "wandering":goal.Wander=true;break;case "dead":target.GetStat("Hitpoints").BaseValue=0;break;}
   Assert.False(ReferenceGladeRouteControl.HasLiveCalm(zone,target));
  }
  [TestCase("cooldown")][TestCase("replaced-id")][TestCase("unbound")][TestCase("mode")][TestCase("range")][TestCase("hidden")][TestCase("not-visible")]
  [TestCase("removed")][TestCase("foreign-brain")][TestCase("foreign-zone")][TestCase("dead")][TestCase("actor-dead")][TestCase("calmed")][TestCase("friendly")][TestCase("nonaligned")][TestCase("too-far")]
  public void SameSetupRefusesInvalidCast(string change)
  {
   Assert.True(ReferenceGladeRouteControl.TryCalm(zone,actor,target,original,out _,out _,out _));
   switch(change){case "cooldown":abilities.GetAbility(original).CooldownRemaining=1;break;case "replaced-id":original=Guid.NewGuid();break;
    case "unbound":abilities.SlotAssignments[0]=Guid.Empty;break;case "mode":abilities.GetAbility(original).TargetingMode=AbilityTargetingMode.AdjacentCell;break;
    case "range":abilities.GetAbility(original).Range=1;break;case "hidden":target.GetPart<RenderPart>().Visible=false;break;
    case "not-visible":zone.GetCell(2,12).IsVisible=false;break;case "removed":zone.RemoveEntity(target);break;
    case "foreign-brain":target.GetPart<BrainPart>().ParentEntity=new Entity();break;case "foreign-zone":target.GetPart<BrainPart>().CurrentZone=new Zone("other");break;
    case "dead":target.GetStat("Hitpoints").BaseValue=0;break;case "actor-dead":actor.GetStat("Hitpoints").BaseValue=0;break;case "calmed":Calm();break;
    case "friendly":target.GetPart<BrainPart>().Target=null;break;
    case "nonaligned":zone.RemoveEntity(target);zone.AddEntity(target,2,13);zone.GetCell(2,13).IsVisible=true;break;
    case "too-far":zone.RemoveEntity(target);zone.AddEntity(target,7,12);zone.GetCell(7,12).IsVisible=true;break;}
   Assert.False(ReferenceGladeRouteControl.TryCalm(zone,actor,target,original,out _,out _,out _));
  }
  [TestCase("creature")][TestCase("structure")][TestCase("solid")]
  public void ActualFirstImpactCannotBeSkipped(string kind)
  {
   var blocker=kind=="creature"?Creature("first"):new Entity{ID="first"};
   if(kind=="structure")blocker.AddPart(new DestructiblePart());if(kind=="solid"){blocker.AddPart(new PhysicsPart{Solid=true});blocker.Tags["Solid"]="";}
   zone.AddEntity(blocker,1,12);Assert.False(ReferenceGladeRouteControl.TryCalm(zone,actor,target,original,out _,out _,out _));
  }
  [Test]public void PhysicsOnlyIsNotATaggedSpellRayWall()
  {var prop=new Entity{ID="physics-only"};prop.AddPart(new PhysicsPart{Solid=true});zone.AddEntity(prop,1,12);Assert.True(ReferenceGladeRouteControl.TryCalm(zone,actor,target,original,out _,out _,out _));}
  [Test]public void SameCellCreaturePriorityMatchesNativeSpell()
  {var bush=new Entity{ID="bush"};bush.AddPart(new DestructiblePart());zone.AddEntity(bush,2,12);Assert.True(ReferenceGladeRouteControl.TryCalm(zone,actor,target,original,out _,out _,out _));}
  [TestCase("buried")][TestCase("finished")]
  public void ProductionRefusesAnyExistingNoFightSoPreflightDoesToo(string shape)
  {
   var goal=Calm();if(shape=="buried")target.GetPart<BrainPart>().PushGoal(new BoredGoal());else goal.Age=50;
   Assert.False(ReferenceGladeRouteControl.HasLiveCalm(zone,target));
   Assert.False(ReferenceGladeRouteControl.TryCalm(zone,actor,target,original,out _,out _,out _));
  }
  [TestCase(49,true)][TestCase(50,false)]
  public void SavedGoalStateKeepsExactExpiryAfterRealGraphLoad(int age,bool protectedNow)
  {
   var goal=Calm();goal.Age=age;var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(target);
   Assert.AreNotSame(target,loaded);var loadedActor=loaded.GetPart<BrainPart>().Target;
   var loadedZone=new Zone("loaded");loadedZone.AddEntity(loadedActor,0,12);loadedZone.AddEntity(loaded,2,12);
   loaded.GetPart<BrainPart>().CurrentZone=loadedZone;loadedZone.GetCell(2,12).IsVisible=true;
   Assert.AreEqual(original,loadedActor.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(0).ID);
   Assert.AreEqual(protectedNow,ReferenceGladeRouteControl.HasLiveCalm(loadedZone,loaded));
   Assert.False(ReferenceGladeRouteControl.HasLiveCalm(loadedZone,target));
   Assert.False(ReferenceGladeRouteControl.TryCalm(loadedZone,loadedActor,loaded,original,out _,out _,out _));
  }
  [Test] public void NullGraphsRefuseWithoutMutation()
  {Assert.False(ReferenceGladeRouteControl.HasLiveCalm(null,target));Assert.False(ReferenceGladeRouteControl.HasLiveCalm(zone,null));Assert.False(ReferenceGladeRouteControl.TryCalm(null,actor,target,original,out _,out _,out _));Assert.False(ReferenceGladeRouteControl.TryCalm(zone,null,target,original,out _,out _,out _));}
  [Test]public void RemovedSameIdOwnerCannotReuseCalm()
  {Calm();zone.RemoveEntity(target);var clone=Creature(target.ID);zone.AddEntity(clone,2,12);Assert.False(ReferenceGladeRouteControl.HasLiveCalm(zone,target));Assert.False(ReferenceGladeRouteControl.TryCalm(zone,actor,target,original,out _,out _,out _));}
 }
}
