using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class WorldAffordanceQueryTests
 {
  public sealed class Trap:Part{public int Calls;public override string Name=>"AffordanceTrap";public override bool HandleEvent(GameEvent e){Calls++;throw new InvalidOperationException("Query fired "+e.ID);}}
  internal sealed class Fixture:IDisposable
  {
   readonly EntityFactory old=HarvestablePart.Factory;public readonly Zone Zone=new Zone("affordance");public readonly Entity Player;
   public Fixture(){Player=Owner("player",10,10);Player.Tags["Player"]="true";Player.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=40};HarvestablePart.Factory=new EntityFactory();var bp=new Blueprint{Name="Yield"};bp.Parts["Physics"]=new Dictionary<string,string>{{"Takeable","true"}};HarvestablePart.Factory.Blueprints[bp.Name]=bp;}
   public Entity Owner(string id,int x,int y){var e=new Entity{ID=id,BlueprintName=id};e.AddPart(new PhysicsPart());e.AddPart(new RenderPart{Visible=true,DisplayName=id});Assert.True(Zone.AddEntity(e,x,y));Zone.GetCell(x,y).IsVisible=true;Zone.GetCell(x,y).Explored=true;return e;}
   public Entity Row(int x=11,int y=10){var e=Owner("row",x,y);e.AddPart(new FieldHarvestPart{YieldBlueprint="Yield"});return e;}
   public object Find(bool focus=false,int x=11,int y=10){var t=typeof(Entity).Assembly.GetType("CavesOfOoo.Core.WorldAffordanceQuery");Assert.NotNull(t,"Missing read-only affordance query");return t.GetMethod("Find").Invoke(null,new object[]{Player,Zone,focus,x,y});}
   public static object Get(object v,string p)=>v?.GetType().GetProperty(p).GetValue(v);
   public void Dispose(){HarvestablePart.Factory=old;}
  }
  [Test] public void AdjacentHarvestNamesActualMenuKeyAndOwner(){using(var f=new Fixture()){var row=f.Row();var q=f.Find();Assert.AreSame(row,Fixture.Get(q,"Target"));Assert.AreEqual("Harvest",Fixture.Get(q,"Command"));Assert.AreEqual("C, D: menu / harvest",Fixture.Get(q,"Hint"));}}
  [Test] public void FocusUsesEnterMenuAndCannotFallBackToAnotherCell(){using(var f=new Fixture()){f.Row();Assert.AreEqual("Enter: menu / harvest",Fixture.Get(f.Find(true),"Hint"));Assert.IsNull(f.Find(true,9,10));}}
  [Test] public void UnderfootUsesPeriodNotEnterOrMoveKey(){using(var f=new Fixture()){f.Row(10,10);Assert.AreEqual("C, .: menu / harvest",Fixture.Get(f.Find(),"Hint"));}}
  [TestCase("spent")][TestCase("missing-factory")][TestCase("missing-yield")][TestCase("zero-yield")]
  public void UnavailableFieldNeverAdvertisesHarvest(string why){using(var f=new Fixture()){var r=f.Row().GetPart<FieldHarvestPart>();if(why=="spent")r.Harvested=true;if(why=="missing-factory")HarvestablePart.Factory=null;if(why=="missing-yield")r.YieldBlueprint="unknown";if(why=="zero-yield")r.YieldCount=0;Assert.IsNull(f.Find());}}
  [TestCase("hidden-cell")][TestCase("unexplored")][TestCase("hidden-part")][TestCase("foreign-render")][TestCase("carried")][TestCase("equipped")][TestCase("removed")][TestCase("moved")][TestCase("foreign-physics")]
  public void CurrentVisibleGroundOwnerOnly(string why){using(var f=new Fixture()){var r=f.Row();var c=f.Zone.GetEntityCell(r);if(why=="hidden-cell")c.IsVisible=false;if(why=="unexplored")c.Explored=false;if(why=="hidden-part")r.GetPart<RenderPart>().Visible=false;if(why=="foreign-render")r.GetPart<RenderPart>().ParentEntity=f.Player;if(why=="carried")r.GetPart<PhysicsPart>().InInventory=f.Player;if(why=="equipped")r.GetPart<PhysicsPart>().Equipped=f.Player;if(why=="foreign-physics")r.GetPart<PhysicsPart>().ParentEntity=f.Player;if(why=="removed")f.Zone.RemoveEntity(r);if(why=="moved"){Assert.True(f.Zone.MoveEntity(r,14,10));Assert.AreEqual(14,f.Zone.GetEntityCell(r).X);}Assert.IsNull(f.Find());}}
  [Test] public void VisibleFarFocusDoesNotAdvertiseAnOutOfReachAction(){using(var f=new Fixture()){f.Row(14,10);Assert.IsNull(f.Find(true,14,10));}}
  [Test] public void ForeignSameCoordinatesAndSameIdNeverSupplyAQuery(){using(var f=new Fixture()){var row=f.Row();Assert.True(f.Zone.RemoveEntity(row));var other=new Zone(f.Zone.ZoneID);Assert.True(other.AddEntity(row,11,10));Assert.IsNull(f.Find());}}
  [Test] public void DeadOrDetachedPlayerHasNoActionCue(){using(var f=new Fixture()){f.Row();f.Player.GetStat("Hitpoints").BaseValue=0;Assert.IsNull(f.Find());f.Player.GetStat("Hitpoints").BaseValue=40;f.Zone.RemoveEntity(f.Player);Assert.IsNull(f.Find());}}
  [TestCase(false)][TestCase(true)] public void LockedContainerRefusesButContentsNeverChangeHint(bool legacy){using(var f=new Fixture()){var c=f.Owner("cache",11,10);var p=new ContainerPart();c.AddPart(p);Assert.AreEqual("OpenContainer",Fixture.Get(f.Find(),"Command"));p.Contents.Add(new Entity{ID="hidden-stock"});Assert.AreEqual("OpenContainer",Fixture.Get(f.Find(),"Command"));if(legacy)p.Locked=true;else c.AddPart(new LockPart{IsLocked=true});Assert.IsNull(f.Find());}}
  [Test] public void ReadSignRequiresExactExamineProvider(){using(var f=new Fixture()){var s=f.Owner("Signpost",11,10);s.AddPart(new RegionalSignpostPart());Assert.IsNull(f.Find());s.AddPart(new ExaminablePart{Text="Read me"});Assert.AreEqual("Examine",Fixture.Get(f.Find(),"Command"));Assert.AreEqual("C, D: menu / read",Fixture.Get(f.Find(),"Hint"));}}
  [Test] public void DoorUsesCurrentOpenCloseAndCannotCloseOccupiedDoorway(){using(var f=new Fixture()){var d=f.Owner("door",11,10);d.AddPart(new DoorPart{IsOpen=false});Assert.AreEqual(DoorPart.OpenCommand,Fixture.Get(f.Find(),"Command"));d.GetPart<DoorPart>().IsOpen=true;Assert.AreEqual(DoorPart.CloseCommand,Fixture.Get(f.Find(),"Command"));f.Owner("blocker",11,10);Assert.IsNull(f.Find());}}
  [TestCase("locked")][TestCase("foreign-owner")][TestCase("takeable")]
  public void DoorPermissionRefusalsAreNotOpenHints(string why){using(var f=new Fixture()){var d=f.Owner("door",11,10);d.AddPart(new DoorPart{IsOpen=false});if(why=="locked")d.AddPart(new LockPart{IsLocked=true});if(why=="foreign-owner")d.GetPart<DoorPart>().OwnerId="someone-else";if(why=="takeable")d.GetPart<PhysicsPart>().Takeable=true;Assert.IsNull(f.Find());}}
  [TestCase(false)][TestCase(true)]public void OrdinaryFiniteHarvestReflectsSpentStateWithoutPromisingRoll(bool spent){using(var f=new Fixture()){var c=f.Owner("corpse",11,10);c.AddPart(new HarvestablePart{YieldBlueprint="Yield",YieldChance=0,Harvested=spent});if(spent)Assert.IsNull(f.Find());else Assert.AreEqual("Harvest",Fixture.Get(f.Find(),"Command"));}}
  [Test] public void QueryNeverDispatchesCallbacksOrChangesSources(){using(var f=new Fixture()){var r=f.Row();var a=new Trap();var b=new Trap();f.Player.AddPart(a);r.AddPart(b);for(int i=0;i<100;i++)Assert.NotNull(f.Find());Assert.AreEqual(0,a.Calls+b.Calls);Assert.False(r.GetPart<FieldHarvestPart>().Harvested);Assert.AreEqual(1,r.GetPart<FieldHarvestPart>().YieldCount);Assert.AreEqual(2,f.Zone.GetReadOnlyEntities().Count);}}
  [Test] public void UnderfootWinsAndOneResultNeverRevealsRemoteOwners(){using(var f=new Fixture()){var first=f.Row(10,10);var remote=f.Owner("remote",15,10);remote.AddPart(new ContainerPart());Assert.AreSame(first,Fixture.Get(f.Find(),"Target"));}}
 }
}
