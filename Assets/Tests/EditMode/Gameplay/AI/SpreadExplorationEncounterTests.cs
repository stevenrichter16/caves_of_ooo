using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadExplorationEncounterTests
 {
  sealed class Fixture:IDisposable
  {
   readonly SpreadExplorationActorTests.Scope scope=new SpreadExplorationActorTests.Scope();
   readonly EntityFactory oldFactory=LoadoutPart.Factory;readonly Random oldRng=LoadoutPart.Rng;readonly EntityFactory oldHarvest=HarvestablePart.Factory;
   public readonly EntityFactory Factory=new EntityFactory();public readonly Zone Z=new Zone("Overworld.7.9.0");
   public readonly Entity A,B,Food;public readonly Entity[] Actors;public Func<bool> Final;
   public Fixture(bool snakes=false,string source="BerryBush")
   {
    Factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
    LoadoutPart.Factory=HarvestablePart.Factory=Factory;LoadoutPart.Rng=new Random(718);
    for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++){var g=new Entity{ID="ground-"+x+","+y,BlueprintName="Grass"};g.Tags["Terrain"]="true";Assert.True(Z.AddEntity(g,x,y));}
    A=Put(snakes?"Viper":"MarlbackScrabbler",31,11);B=Put(snakes?"Viper":"MarlbackScrabbler",31,13);Actors=new[]{A,B};
    Food=Put(source,43,12);
    for(int y=10;y<=14;y++)Put("Tree",40,y);
   }
   public Entity Put(string blueprint,int x,int y){var e=Factory.CreateEntity(blueprint);Assert.NotNull(e);Assert.True(Z.AddEntity(e,x,y));if(e.GetPart<BrainPart>() is BrainPart b)b.CurrentZone=Z;return e;}
   public bool Run(bool snakes=false,bool occluded=false,Func<bool> authority=null,IReadOnlyList<Entity> actors=null)
   {
    var method=typeof(SpreadExplorationActorPlacement).GetMethod(snakes?"TrySnakeForage":"TryWorkGang",BindingFlags.Public|BindingFlags.Static);Assert.NotNull(method,"Missing bounded encounter placement API");
    object[] args={Z,actors??Actors,snakes?(object)Food:occluded,authority??(()=>true),null};bool ok=(bool)method.Invoke(null,args);Final=(Func<bool>)args[4];return ok;
   }
   public void Dispose(){LoadoutPart.Factory=oldFactory;LoadoutPart.Rng=oldRng;HarvestablePart.Factory=oldHarvest;scope.Dispose();}
  }
  static Entity[] Gear(Entity e)=>e.GetPart<InventoryPart>().Objects.Concat(e.GetPart<InventoryPart>().EquippedItems.Values).Concat(e.GetPart<Body>().GetParts().Select(p=>p.Equipped)).Where(i=>i!=null).Distinct().ToArray();
  static bool Los(Zone z,Entity a,Entity b){var p=z.GetEntityPosition(a);var q=z.GetEntityPosition(b);return AIHelpers.HasLineOfSight(z,p.x,p.y,q.x,q.y);}
  [TestCase(false)][TestCase(true)]public void OriginalColdRollPositionsDoNotSetALiveTravelLeash(bool far)
  {using(var f=new Fixture()){if(far){Assert.True(f.Z.MoveEntity(f.A,3,4));Assert.True(f.Z.MoveEntity(f.B,73,20));}var a=Gear(f.A);var b=Gear(f.B);Assert.True(f.Run());Assert.True(f.Final());CollectionAssert.AreEquivalent(a,Gear(f.A));CollectionAssert.AreEquivalent(b,Gear(f.B));Assert.LessOrEqual(SpatialQuery.Distance(f.Z,f.A,f.B),6);}}
  [Test]public void BoundedPostSearchPrioritizesAUsefulInteriorPocketOverEdgeDecoys()
  {using(var f=new Fixture()){for(int x=2;x<34;x++)f.Put("Signpost",x,2);Assert.True(f.Run());Assert.True(f.Final());}}
  [Test]public void OccludedVariantPrioritizesActualNativeSightCoverOverVisualHedges()
  {using(var f=new Fixture()){for(int x=2;x<34;x++)f.Put("Hedge",x,12);Assert.True(f.Run(occluded:true));Assert.True(f.Final());Assert.False(Los(f.Z,f.A,f.B));}}
  [Test]public void ActualOrdinaryKitHasNoAssistanceOrReachSkillToBorrow()
  {using(var f=new Fixture()){foreach(var a in f.Actors){Assert.IsNull(a.GetPart<CombatTacticsPart>());var weapons=Gear(a).Where(e=>e.HasPart<MeleeWeaponPart>()).ToArray();Assert.AreEqual(1,weapons.Length);CollectionAssert.Contains(new[]{"Dagger","Hatchet","Cudgel"},weapons[0].BlueprintName);Assert.IsNull(a.GetPart<ActivatedAbilitiesPart>());}}}
  [TestCase(false)][TestCase(true)]public void ExactPairKeepsAllGearAndReceivesOnlyAssistOptIn(bool occluded)
  {using(var f=new Fixture()){var before=f.Z.GetReadOnlyEntities().ToArray();var a=Gear(f.A);var b=Gear(f.B);var rng=LoadoutPart.Rng;Assert.True(f.Run(occluded:occluded));Assert.NotNull(f.Final);Assert.True(f.Final());CollectionAssert.AreEquivalent(before,f.Z.GetReadOnlyEntities());CollectionAssert.AreEquivalent(a,Gear(f.A));CollectionAssert.AreEquivalent(b,Gear(f.B));Assert.AreSame(rng,LoadoutPart.Rng);Assert.AreEqual(!occluded,Los(f.Z,f.A,f.B));foreach(var e in f.Actors){var t=e.GetPart<CombatTacticsPart>();Assert.NotNull(t);Assert.True(t.AssistAllies);Assert.AreEqual("",t.SkillClasses);Assert.Zero(t.AbilityChance);Assert.IsNull(e.GetPart<ActivatedAbilitiesPart>());}}}
  [TestCase("BerryBush",1)][TestCase("Beehive",2)]public void ActualSnakePacketLeavesFiniteForageUntouched(string source,int count)
  {using(var f=new Fixture(true,source)){if(count==1)Assert.True(f.Z.RemoveEntity(f.B));var owners=f.Actors.Take(count).ToArray();var before=f.Z.GetReadOnlyEntities().ToArray();var at=f.Z.GetEntityPosition(f.Food);var h=f.Food.GetPart<HarvestablePart>();int max=h.YieldMax;Assert.True(f.Run(true,actors:owners));Assert.True(f.Final());CollectionAssert.AreEquivalent(before,f.Z.GetReadOnlyEntities());Assert.AreEqual(at,f.Z.GetEntityPosition(f.Food));Assert.AreSame(h,f.Food.GetPart<HarvestablePart>());Assert.False(h.Harvested);Assert.AreEqual(max,h.YieldMax);foreach(var v in owners){Assert.AreEqual(8,v.GetStatValue("Hitpoints"));Assert.AreEqual(130,v.GetStatValue("Speed"));Assert.AreEqual(10,v.GetPart<BrainPart>().SightRadius);Assert.IsNull(v.GetPart<AIAmbushPart>());Assert.IsNull(v.GetPart<CombatTacticsPart>());}}}
  [TestCase("one")][TestCase("duplicate")][TestCase("wrong-blueprint")][TestCase("party")][TestCase("busy")][TestCase("existing-tactics")][TestCase("no-post")]
  public void InvalidPairRefusesWithoutMovingOrAlteringStock(string fault)
  {using(var f=new Fixture()){IReadOnlyList<Entity> input=f.Actors;if(fault=="one")input=new[]{f.A};if(fault=="duplicate")input=new[]{f.A,f.A};if(fault=="wrong-blueprint")f.B.BlueprintName="SpreadDitchMate";if(fault=="party")f.B.GetPart<BrainPart>().SetPartyLeader(f.A);if(fault=="busy")f.B.GetPart<BrainPart>().PushGoal(new WaitGoal(3));if(fault=="existing-tactics")f.B.AddPart(new CombatTacticsPart());if(fault=="no-post")foreach(var p in f.Z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="Tree").ToArray())f.Z.RemoveEntity(p);var at=f.Actors.Select(f.Z.GetEntityPosition).ToArray();var parts=f.Actors.Select(e=>e.Parts.ToArray()).ToArray();Assert.False(f.Run(actors:input));Assert.IsNull(f.Final);CollectionAssert.AreEqual(at,f.Actors.Select(f.Z.GetEntityPosition));for(int i=0;i<2;i++)CollectionAssert.AreEqual(parts[i],f.Actors[i].Parts);}}
  [TestCase("spent")][TestCase("foreign")][TestCase("changed-kind")][TestCase("missing-part")]
  public void InvalidFoodCannotBecomeAClaimedForageOpportunity(string fault)
  {using(var f=new Fixture(true)){if(fault=="spent")f.Food.GetPart<HarvestablePart>().Harvested=true;if(fault=="foreign")f.Z.RemoveEntity(f.Food);if(fault=="changed-kind")f.Food.BlueprintName="HollowStump";if(fault=="missing-part")f.Food.RemovePart(f.Food.GetPart<HarvestablePart>());var at=f.Actors.Select(f.Z.GetEntityPosition).ToArray();Assert.False(f.Run(true));CollectionAssert.AreEqual(at,f.Actors.Select(f.Z.GetEntityPosition));Assert.IsNull(f.Final);}}
  [TestCase(false)][TestCase(true)]public void RefusedAuthorityMakesNoActorOrWorldMutation(bool snakes)
  {using(var f=new Fixture(snakes)){var at=f.Actors.Select(f.Z.GetEntityPosition).ToArray();int version=f.Z.EntityVersion;Assert.False(f.Run(snakes,authority:()=>false));Assert.AreEqual(version,f.Z.EntityVersion);CollectionAssert.AreEqual(at,f.Actors.Select(f.Z.GetEntityPosition));}}
  [TestCase(false)][TestCase(true)]public void ClosedBorderRefusesRatherThanClaimingAnOptionalBypass(bool snakes)
  {using(var f=new Fixture(snakes)){for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)if(x==0||y==0||x==Zone.Width-1||y==Zone.Height-1){var wall=new Entity{ID=Guid.NewGuid().ToString("N")};wall.AddPart(new PhysicsPart{Solid=true});Assert.True(f.Z.AddEntity(wall,x,y));}Assert.False(f.Run(snakes));Assert.IsNull(f.Final);}}
  [TestCase(false)][TestCase(true)]public void FinalValidatorRejectsChangedActorAndKeepsUnrelatedChange(bool snakes)
  {using(var f=new Fixture(snakes)){Assert.True(f.Run(snakes));var wall=new Entity{ID="harmless"};wall.AddPart(new PhysicsPart{Solid=true});Assert.True(f.Z.AddEntity(wall,40,12));Assert.True(f.Final());f.A.Statistics["Strength"].BaseValue++;Assert.False(f.Final());}}
  [Test]public void LateAuthorityGearMutationRefusesWithoutRestoringForeignChange()
  {using(var f=new Fixture()){var gear=Gear(f.A).First();bool changed=false;Assert.False(f.Run(authority:()=>{if(f.A.GetPart<CombatTacticsPart>()!=null&&!changed){gear.Properties["foreign-change"]="kept";changed=true;}return true;}));Assert.True(changed);Assert.AreEqual("kept",gear.Properties["foreign-change"]);Assert.IsNull(f.A.GetPart<CombatTacticsPart>());Assert.IsNull(f.B.GetPart<CombatTacticsPart>());}}
  [TestCase("normal",true)][TestCase("calm",false)][TestCase("expired",true)][TestCase("removed",true)][TestCase("indefinite",false)][TestCase("buried",true)][TestCase("covered",false)][TestCase("passive",false)][TestCase("party",false)]
  public void RealAssistanceAdmissionHonorsCurrentCalmAndSight(string mode,bool expected)
  {using(var f=new Fixture()){Assert.True(f.Z.MoveEntity(f.A,20,12));Assert.True(f.Z.MoveEntity(f.B,22,12));var target=f.Put("Player",21,15);foreach(var e in f.Actors)e.AddPart(new CombatTacticsPart{SkillClasses="",AbilityChance=0,AssistAllies=true});var brain=f.B.GetPart<BrainPart>();if(mode=="calm"||mode=="expired"||mode=="removed"||mode=="indefinite"||mode=="buried"){var goal=new NoFightGoal(8);brain.PushGoal(goal);if(mode=="expired")goal.Age=8;if(mode=="indefinite")goal.Duration=0;if(mode=="buried")brain.PushGoal(new WaitGoal(3));if(mode=="removed")brain.RemoveGoal(goal);}if(mode=="covered")f.Put("Tree",21,12);if(mode=="passive")brain.Passive=true;if(mode=="party")brain.SetPartyLeader(target);f.A.GetPart<CombatTacticsPart>().AlertAllies(target,f.Z);Assert.AreEqual(expected,brain.IsPersonallyHostileTo(target));}}
  [TestCase(false)][TestCase(true)]public void MidPlacementCallbackCannotEraseAnOriginalCriticalBorder(bool snakes)
  {using(var f=new Fixture(snakes)){var origin=f.Z.GetEntityPosition(f.A);bool changed=false;Assert.False(f.Run(snakes,authority:()=>{if(!changed&&f.Z.GetEntityPosition(f.A)!=origin){changed=true;var wall=new Entity{ID="foreign-border"};wall.AddPart(new PhysicsPart{Solid=true});Assert.True(f.Z.AddEntity(wall,0,0));}return true;}));Assert.True(changed);Assert.IsNull(f.Final);}}
  [TestCase(false)][TestCase(true)]public void LateGenerationBlockerInvalidatesExactAcceptedGeometry(bool snakes)
  {using(var f=new Fixture(snakes)){Assert.True(f.Run(snakes));var wall=new Entity{ID="late-border"};wall.AddPart(new PhysicsPart{Solid=true});Assert.True(f.Z.AddEntity(wall,0,0));Assert.False(f.Final());}}
  [TestCase("moved")][TestCase("replaced")][TestCase("changed-yield")]
  public void FoodCallbackMutationRefusesWithoutRepairingIndependentSource(string fault)
  {using(var f=new Fixture(true)){var origin=f.Z.GetEntityPosition(f.A);bool changed=false;Entity foreign=null;Assert.False(f.Run(true,authority:()=>{if(!changed&&f.Z.GetEntityPosition(f.A)!=origin){changed=true;if(fault=="moved")Assert.True(f.Z.MoveEntity(f.Food,60,20));if(fault=="changed-yield")f.Food.GetPart<HarvestablePart>().YieldMax=9;if(fault=="replaced"){string id=f.Food.ID;f.Z.RemoveEntity(f.Food);foreign=f.Put("BerryBush",43,12);foreign.ID=id;}}return true;}));Assert.True(changed);if(fault=="moved")Assert.AreEqual((60,20),f.Z.GetEntityPosition(f.Food));if(fault=="changed-yield")Assert.AreEqual(9,f.Food.GetPart<HarvestablePart>().YieldMax);if(fault=="replaced")Assert.AreSame(foreign,f.Z.GetCell(43,12).Objects.Single(e=>e.ID==f.Food.ID));}}
  [TestCase(false)][TestCase(true)]public void ForeignActorReplacementIsNotRemovedOrReclaimed(bool snakes)
  {using(var f=new Fixture(snakes)){var origin=f.Z.GetEntityPosition(f.A);Entity foreign=null;Assert.False(f.Run(snakes,authority:()=>{if(foreign==null&&f.Z.GetEntityPosition(f.A)!=origin){var at=f.Z.GetEntityPosition(f.A);f.Z.RemoveEntity(f.A);foreign=f.Put(snakes?"Viper":"MarlbackScrabbler",at.x,at.y);foreign.ID=f.A.ID;}return true;}));Assert.NotNull(foreign);Assert.NotNull(f.Z.GetEntityCell(foreign));Assert.IsNull(f.Z.GetEntityCell(f.A));}}
  [TestCase(false,false)][TestCase(true,false)][TestCase(false,true)]
  public void ActualPlacedPairUsesNativeSightAndCalmAdmission(bool occluded,bool calm)
  {using(var f=new Fixture()){Assert.True(f.Run(occluded:occluded));var a=f.Z.GetEntityPosition(f.A);var b=f.Z.GetEntityPosition(f.B);Cell chosen=null;
   for(int y=1;y<Zone.Height-1&&chosen==null;y++)for(int x=1;x<Zone.Width-1;x++){var c=f.Z.GetCell(x,y);if(c.BlocksMovement()||c.Occupants.Any(e=>e.HasTag("Creature")))continue;bool sa=Math.Max(Math.Abs(x-a.x),Math.Abs(y-a.y))<=3&&AIHelpers.HasLineOfSight(f.Z,a.x,a.y,x,y);bool sb=Math.Max(Math.Abs(x-b.x),Math.Abs(y-b.y))<=10&&AIHelpers.HasLineOfSight(f.Z,b.x,b.y,x,y);if(sa&&(occluded?!sb:sb)){chosen=c;break;}}
   Assert.NotNull(chosen,"Actual geometry must expose the exact threat ray before observing assistance.");var player=f.Put("Player",chosen.X,chosen.Y);if(calm)f.B.GetPart<BrainPart>().PushGoal(new NoFightGoal(8));f.A.FireEventAndRelease(GameEvent.New("TakeTurn"));Assert.AreSame(player,f.A.GetPart<BrainPart>().Target);Assert.AreEqual(!occluded&&!calm,f.B.GetPart<BrainPart>().IsPersonallyHostileTo(player));}}
  [Test]public void SavedAssistOnlyPartRetainsActualGearAndCalmWithoutAnyNewAbility()
  {using(var f=new Fixture()){Assert.True(f.Run());f.A.GetPart<BrainPart>().PushGoal(new NoFightGoal(8){Age=2});var ids=Gear(f.A).Select(e=>e.ID+":"+e.BlueprintName).OrderBy(v=>v).ToArray();var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(f.A);Assert.AreNotSame(f.A,loaded);CollectionAssert.AreEqual(ids,Gear(loaded).Select(e=>e.ID+":"+e.BlueprintName).OrderBy(v=>v));var t=loaded.GetPart<CombatTacticsPart>();Assert.AreSame(loaded,t.ParentEntity);Assert.True(t.AssistAllies);Assert.AreEqual(6,t.AssistRadius);Assert.AreEqual("",t.SkillClasses);Assert.Zero(t.AbilityChance);Assert.IsNull(loaded.GetPart<ActivatedAbilitiesPart>());var calm=loaded.GetPart<BrainPart>().PeekGoal() as NoFightGoal;Assert.NotNull(calm);Assert.AreEqual(2,calm.Age);Assert.AreEqual(8,calm.Duration);}}

  [TestCase("BerryBush",false)][TestCase("Beehive",true)]
  public void OptionalNativeHarvestKeepsAuthoredYieldAndDepletionAcrossFullSave(string kind,bool overflow)
  {using(var f=new Fixture(true,kind)){Assert.True(f.Run(true));var actor=f.Factory.CreateEntity("Player");var cell=f.Z.GetCell(43,12);var approach=new[]{(43,11),(43,13),(42,12),(44,12)}.First(p=>f.Z.CanPlaceFootprint(actor,p.Item1,p.Item2));Assert.True(f.Z.AddEntity(actor,approach.Item1,approach.Item2));if(overflow)actor.GetPart<InventoryPart>().MaxWeight=0;
   string product=f.Food.GetPart<HarvestablePart>().YieldBlueprint;int max=f.Food.GetPart<HarvestablePart>().YieldMax;string id=f.Food.ID;
   Func<bool> harvest=()=>InventorySystem.ExecuteCommand(new CavesOfOoo.Core.Inventory.Commands.PerformInventoryActionCommand(f.Food,"Harvest"),actor,f.Z).Success;
   Assert.True(harvest());Assert.True(f.Food.GetPart<HarvestablePart>().Harvested);Assert.IsNull(f.Z.GetEntityCell(f.Food));Assert.False(harvest());
   int Packed(Entity a)=>a.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName==product).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
   int Floor(Zone z)=>z.GetReadOnlyEntities().Where(e=>e.BlueprintName==product).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
   int total=Packed(actor)+Floor(f.Z);Assert.That(total,Is.InRange(1,max));Assert.AreEqual(overflow?0:total,Packed(actor));Assert.AreEqual(overflow?total:0,Floor(f.Z));
   var manager=OverworldZoneManager.CreateDetached(f.Factory,64);manager.SetActiveZone(f.Z);var session=GameSessionState.Capture("forage","fixture",manager,new TurnManager(),actor,0);GameSessionState loaded;
   using(var stream=new MemoryStream()){session.Save(new SaveWriter(stream));stream.Position=0;loaded=GameSessionState.Load(new SaveReader(stream,f.Factory));}
   var restored=loaded.ZoneManager.ActiveZone;Assert.AreNotSame(f.Z,restored);Assert.AreNotSame(actor,loaded.Player);Assert.False(restored.GetReadOnlyEntities().Any(e=>e.ID==id));Assert.AreEqual(total,Packed(loaded.Player)+Floor(restored));CollectionAssert.AreEquivalent(f.Actors.Select(e=>e.ID),restored.GetReadOnlyEntities().Where(e=>e.BlueprintName=="Viper").Select(e=>e.ID));
  }}

  [TestCase(7,16,false)][TestCase(7,16,true)][TestCase(43,12,false)][TestCase(43,12,true)]
  public void ExposedForageDoesNotRequireAnUnrelatedNearbyPost(int x,int y,bool covered)
  {using(var f=new Fixture(true)){foreach(var post in f.Z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="Tree").ToArray())Assert.True(f.Z.RemoveEntity(post));
   Assert.True(f.Z.MoveEntity(f.Food,x,y));if(covered)f.Put("Tree",x+3,y);Assert.True(f.Z.RemoveEntity(f.B));
   var original=f.Z.GetReadOnlyEntities().ToArray();var food=f.Food.GetPart<HarvestablePart>();
   Assert.True(f.Run(true,actors:new[]{f.A}));Assert.True(f.Final());CollectionAssert.AreEquivalent(original,f.Z.GetReadOnlyEntities());
   Assert.AreEqual((x,y),f.Z.GetEntityPosition(f.Food));Assert.AreSame(food,f.Food.GetPart<HarvestablePart>());Assert.False(food.Harvested);
   Assert.AreEqual(10,f.A.GetPart<BrainPart>().SightRadius);Assert.AreEqual(130,f.A.GetStatValue("Speed"));}}

  [Test]public void RemovedChosenForageCoverInvalidatesTheColdSituationClaim()
  {using(var f=new Fixture(true)){Assert.True(f.Run(true));foreach(var post in f.Z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="Tree").ToArray())Assert.True(f.Z.RemoveEntity(post));Assert.False(f.Final());}}

 }
}
