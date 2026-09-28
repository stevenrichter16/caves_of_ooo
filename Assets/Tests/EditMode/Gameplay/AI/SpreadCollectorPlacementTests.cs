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
 public sealed class SpreadCollectorPlacementTests
 {
  sealed class Fixture:IDisposable
  {
   readonly SpreadExplorationActorTests.Scope scope=new SpreadExplorationActorTests.Scope();
   public readonly EntityFactory Factory=new EntityFactory();public readonly Zone Z=new Zone("Overworld.7.9.0");
   public readonly Entity Bird,Home,Item,Stock;public Func<bool> Final;
   public SpreadCollectorPart Role=>Bird.GetPart<SpreadCollectorPart>();
   public Fixture(string item="Hatchet")
   {
    Factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
    for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++){var g=new Entity{ID="g-"+x+","+y,BlueprintName="Grass"};g.Tags["Terrain"]="true";Assert.True(Z.AddEntity(g,x,y));}
    Bird=Put("Tatterjay",16,8);Home=Put("Sack",40,12);Item=Put(item,65,18);Stock=Factory.CreateEntity("Starapple");Assert.True(Home.GetPart<ContainerPart>().AddItem(Stock));
    Assert.NotNull(Role);Assert.False(Role.Configured);Assert.True(Bird.HasTag("NoRandomStock"));
   }
   public Entity Put(string bp,int x,int y){var e=Factory.CreateEntity(bp);Assert.NotNull(e);Assert.True(Z.AddEntity(e,x,y));if(e.GetPart<BrainPart>() is BrainPart b)b.CurrentZone=Z;return e;}
   public bool Run(Func<bool> authority=null)
   {var m=typeof(SpreadExplorationActorPlacement).GetMethod("TryCollectorReturn",BindingFlags.Public|BindingFlags.Static);Assert.NotNull(m,"Missing exact-source collector placement API");object[] args={Z,Bird,Home,Item,authority??(()=>true),null};bool result=(bool)m.Invoke(null,args);Final=(Func<bool>)args[5];return result;}
   public void Dispose()=>scope.Dispose();
  }
  static int Units(Entity e)=>e.GetPart<StackerPart>()?.StackCount??1;
  static string Facts(Entity e)=>e.ID+"|"+e.BlueprintName+"|"+Units(e)+"|"+string.Join(",",e.Properties.OrderBy(k=>k.Key).Select(k=>k.Key+"="+k.Value));
  [TestCase("Hatchet")][TestCase("Cudgel")][TestCase("LeatherBoots")]
  public void ExactExistingBirdGoodAndFiniteHomeBecomeOneRealTrip(string kind)
  {using(var f=new Fixture(kind)){var owners=f.Z.GetReadOnlyEntities().ToArray();var stock=f.Home.GetPart<ContainerPart>().Contents.ToArray();var at=f.Z.GetEntityPosition(f.Home);string item=Facts(f.Item),priorStock=Facts(f.Stock);
   Assert.True(f.Run());Assert.True(f.Final());CollectionAssert.AreEquivalent(owners,f.Z.GetReadOnlyEntities());CollectionAssert.AreEqual(stock,f.Home.GetPart<ContainerPart>().Contents);Assert.AreEqual(at,f.Z.GetEntityPosition(f.Home));Assert.AreEqual(item,Facts(f.Item));Assert.AreEqual(priorStock,Facts(f.Stock));
   Assert.AreSame(f.Home,f.Role.Home);Assert.AreSame(f.Item,f.Role.Target);Assert.AreEqual(Units(f.Item),f.Role.Quantity);Assert.AreEqual(SpreadCollectorPhase.Seeking,f.Role.Phase);Assert.Zero(f.Role.Actions);Assert.IsEmpty(f.Bird.GetPart<InventoryPart>().Objects);Assert.AreEqual(1,f.Bird.Parts.Count(p=>p is SpreadCollectorPart));}}
  [TestCase(false)][TestCase(true)]public void InitialColdPositionsAreNotAnIncidentalLiveLeash(bool far)
  {using(var f=new Fixture()){Assert.True(f.Z.MoveEntity(f.Bird,far?3:37,far?3:10));Assert.True(f.Z.MoveEntity(f.Item,far?75:44,far?21:14));Assert.True(f.Run());Assert.True(f.Final());Assert.LessOrEqual(SpatialQuery.Distance(f.Z,f.Bird,f.Item),12);Assert.LessOrEqual(SpatialQuery.Distance(f.Z,f.Home,f.Item),12);}}
  [TestCase("locked")][TestCase("owned-item")][TestCase("quest-home")][TestCase("configured")][TestCase("wrong-item")][TestCase("missing-item")][TestCase("full-home")][TestCase("overweight")][TestCase("party")]
  public void InvalidInitialSourceRefusesWithoutAnyOwnershipOrPositionMutation(string fault)
  {using(var f=new Fixture()){if(fault=="locked")f.Home.GetPart<ContainerPart>().Locked=true;if(fault=="owned-item")f.Item.Properties["OwnerID"]="someone";if(fault=="quest-home")f.Home.Tags["Quest"]="true";if(fault=="configured")f.Role.Configured=true;if(fault=="wrong-item")f.Item.BlueprintName="Dagger";if(fault=="missing-item")Assert.True(f.Z.RemoveEntity(f.Item));if(fault=="full-home")f.Home.GetPart<ContainerPart>().MaxItems=1;if(fault=="overweight")f.Bird.GetPart<InventoryPart>().MaxWeight=0;if(fault=="party")f.Bird.GetPart<BrainPart>().SetPartyLeader(f.Home);
   var owners=f.Z.GetReadOnlyEntities().ToArray();var positions=owners.Select(f.Z.GetEntityPosition).ToArray();int version=f.Z.EntityVersion;bool configured=f.Role.Configured;
   Assert.False(f.Run());Assert.IsNull(f.Final);Assert.AreEqual(version,f.Z.EntityVersion);CollectionAssert.AreEqual(owners,f.Z.GetReadOnlyEntities());CollectionAssert.AreEqual(positions,owners.Select(f.Z.GetEntityPosition));Assert.AreEqual(configured,f.Role.Configured);}}
  [TestCase(false)][TestCase(true)]public void FullHomeMustAcceptTheEntireActualIncomingStack(bool allFits)
  {using(var f=new Fixture("Cudgel")){var c=f.Home.GetPart<ContainerPart>();Assert.True(c.RemoveItem(f.Stock));var existing=f.Factory.CreateEntity("Cudgel");Assert.True(c.AddItem(existing));c.MaxItems=1;
   var incoming=f.Item.GetPart<StackerPart>();var current=existing.GetPart<StackerPart>();Assert.NotNull(incoming);Assert.NotNull(current);incoming.StackCount=3;current.MaxStack=10;current.StackCount=allFits?7:8;f.Bird.GetPart<InventoryPart>().MaxWeight=-1;
   Assert.AreEqual(allFits,f.Run());Assert.AreEqual(3,incoming.StackCount);Assert.AreEqual(allFits?7:8,current.StackCount);Assert.AreSame(existing,c.Contents.Single());}}
  [Test]public void RefusedCurrentAuthorityDoesNotConfigureOrMoveAnything()
  {using(var f=new Fixture()){int version=f.Z.EntityVersion;Assert.False(f.Run(()=>false));Assert.AreEqual(version,f.Z.EntityVersion);Assert.False(f.Role.Configured);Assert.IsNull(f.Role.Target);Assert.IsNull(f.Role.Home);}}
  [TestCase("item-quantity")][TestCase("home-stock")][TestCase("actor-stock")][TestCase("foreign-role")]
  public void ReentrantAuthorityChangesAreRefusedAndNotRepairedAsOwnedWork(string fault)
  {using(var f=new Fixture()){var origin=f.Z.GetEntityPosition(f.Bird);bool changed=false;var role=f.Role;Entity added=null;
   Assert.False(f.Run(()=>{if(!changed&&f.Z.GetEntityPosition(f.Bird)!=origin){changed=true;if(fault=="item-quantity")f.Item.GetPart<StackerPart>().StackCount++;if(fault=="home-stock"){added=f.Factory.CreateEntity("Mushroom");Assert.True(f.Home.GetPart<ContainerPart>().AddItem(added));}if(fault=="actor-stock"){added=f.Factory.CreateEntity("Mushroom");Assert.True(f.Bird.GetPart<InventoryPart>().AddObject(added));}if(fault=="foreign-role"){Assert.True(f.Bird.RemovePart(role));f.Home.AddPart(role);}}return true;}));Assert.True(changed);Assert.IsNull(f.Final);
   if(fault=="item-quantity")Assert.AreEqual(2,Units(f.Item));if(fault=="home-stock")CollectionAssert.Contains(f.Home.GetPart<ContainerPart>().Contents,added);if(fault=="actor-stock")CollectionAssert.Contains(f.Bird.GetPart<InventoryPart>().Objects,added);if(fault=="foreign-role")Assert.AreSame(f.Home,role.ParentEntity);}}
  [TestCase("off-route",true)][TestCase("border-block",false)][TestCase("home-stock",false)][TestCase("item-quantity",false)]
  public void FinalProofRetainsSourcesAndOriginalCriticalRoutes(string mutation,bool expected)
  {using(var f=new Fixture()){Assert.True(f.Run());if(mutation=="off-route"||mutation=="border-block"){var wall=new Entity{ID="independent-wall"};wall.AddPart(new PhysicsPart{Solid=true});Assert.True(f.Z.AddEntity(wall,mutation=="off-route"?70:0,mutation=="off-route"?18:0));}if(mutation=="home-stock")f.Stock.GetPart<StackerPart>().StackCount++;if(mutation=="item-quantity")f.Item.GetPart<StackerPart>().StackCount++;Assert.AreEqual(expected,f.Final());}}
  [TestCase("reserved")][TestCase("hot")][TestCase("covered")]
  public void FixedHomeCannotBeAProtectedOrCoveredSourceAnchor(string fault)
  {using(var f=new Fixture()){var at=f.Z.GetEntityPosition(f.Home);if(fault=="reserved")f.Z.GenReservedCells.Add(at);if(fault=="hot")f.Z.TileState.AddHeat(at.x,at.y,2);if(fault=="covered"){var wall=new Entity{ID="independent-home-wall"};wall.AddPart(new PhysicsPart{Solid=true});Assert.True(f.Z.AddEntity(wall,at.x,at.y));}int version=f.Z.EntityVersion;Assert.False(f.Run());Assert.AreEqual(version,f.Z.EntityVersion);Assert.False(f.Role.Configured);}}
  [TestCase("unwired",true)][TestCase("foreign",false)][TestCase("current",true)]
  public void ColdConfigurationUsesActualSpatialOwnerBeforeSchedulerWiring(string mode,bool expected)
  {using(var f=new Fixture()){Assert.True(f.Z.MoveEntity(f.Bird,39,12));Assert.True(f.Z.MoveEntity(f.Item,38,12));var brain=f.Bird.GetPart<BrainPart>();brain.CurrentZone=mode=="unwired"?null:mode=="foreign"?new Zone(f.Z.ZoneID):f.Z;var original=brain.CurrentZone;
   Assert.AreEqual(expected,f.Role.Configure(f.Z,f.Home,f.Item));Assert.AreSame(original,brain.CurrentZone,"Configuring source ownership must not activate a scheduler.");}}
  [Test]public void ExistingRoleSourceAndRealTransactionRemainAWorkingControl()
  {using(var f=new Fixture()){Assert.True(f.Z.MoveEntity(f.Bird,39,12));Assert.True(f.Z.MoveEntity(f.Item,38,12));Assert.True(f.Role.Configure(f.Z,f.Home,f.Item));var e=GameEvent.New(AIBoredEvent.ID);try{f.Role.HandleEvent(e);}finally{e.Release();}Assert.AreSame(f.Item,f.Role.CurrentCarriedItem);Assert.AreEqual(SpreadCollectorPhase.Carrying,f.Role.Phase);}}
 }
}
