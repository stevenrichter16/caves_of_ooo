using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadCollectorCarryTests
 {
  internal sealed class Fixture:IDisposable
  {
   readonly SpreadExplorationActorTests.Scope scope;
   public SpawnRing3DIntegrationFixture F;public Entity Actor,Home,Item;public Part Role;
   public SpawnRing3DPresenter Presenter=>(SpawnRing3DPresenter)F.Presenter;
   public Entity Carried=>(Entity)Role.GetType().GetProperty("CurrentCarriedItem").GetValue(Role);
   public string Phase=>Role.GetType().GetField("Phase").GetValue(Role).ToString();
   public Fixture(string blueprint="Hatchet")
   {
    scope=new SpreadExplorationActorTests.Scope();try
    {
     F=new SpawnRing3DIntegrationFixture("Overworld.12.10.0");int ax=-1,ay=-1;
     for(int y=2;y<23&&ax<0;y++)for(int x=2;x<77&&ax<0;x++)if(Enumerable.Range(x-1,3).All(cx=>!F.Zone.GetCell(cx,y).BlocksMovement()&&!F.Zone.GetCell(cx,y).Objects.Any(e=>e.HasTag("Creature")||e.HasTag("Player")))){ax=x;ay=y;}
     Assert.Greater(ax,0);Actor=F.Add("Tatterjay",ax,ay);Home=F.Add("Sack",ax-1,ay);Item=F.Add(blueprint,ax+1,ay);Actor.GetPart<BrainPart>().CurrentZone=F.Zone;
     Role=Actor.GetPart("SpreadCollector");Assert.NotNull(Role,"Publish actual collector role/data before these integration cases.");
     Assert.True((bool)Role.GetType().GetMethod("Configure").Invoke(Role,new object[]{F.Zone,Home,Item}));F.Refresh();Assert.True(F.Find(Actor,out _,out string id));Assert.AreEqual("spread-tatterjay",id);
    }catch{Dispose();throw;}
   }
   // Dispatch the actual role's AIBored event/transaction, explicitly staged.
   // Actor scheduler/ordinary discovery are separately covered by gameplay tests.
   public void Act(){var e=GameEvent.New(AIBoredEvent.ID);try{Role.HandleEvent(e);}finally{e.Release();}}
   public void Pick(){Act();Assert.AreSame(Item,Carried);Assert.AreEqual("Carrying",Phase);Assert.IsNull(F.Zone.GetEntityCell(Item));Assert.IsEmpty(Actor.GetPart<InventoryPart>().EquippedItems);F.Refresh();}
   public bool View(out GameObject root){var m=typeof(SpawnRing3DPresenter).GetMethod("TryGetCollectorCarryView");Assert.NotNull(m,"Exact carried collector view API missing");object[] a={Actor,Item,null};bool found=(bool)m.Invoke(Presenter,a);root=a[2]as GameObject;return found;}
   public SpreadBiomeStyleEvidence Proof(){var m=typeof(SpawnRing3DPresenter).GetMethod("TryGetApprovedCollectorCarryStyle");Assert.NotNull(m,"Exact carried source style API missing");object[] a={Actor,Item,null};Assert.True((bool)m.Invoke(Presenter,a));return(SpreadBiomeStyleEvidence)a[2];}
   public object BodyView{get{var views=(IDictionary)typeof(SpawnRing3DPresenter).GetField("views",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(Presenter);return views[Actor];}}
   public string ActionState=>(string)BodyView.GetType().GetField("ActionState").GetValue(BodyView);
   public void Load(){string actor=Actor.ID;var state=F.RoundTrip();F.BindLoaded(state);Actor=F.Zone.GetReadOnlyEntities().Single(e=>e.ID==actor);Role=Actor.GetPart("SpreadCollector");Item=(Entity)Role.GetType().GetField("Target").GetValue(Role);Home=(Entity)Role.GetType().GetField("Home").GetValue(Role);}
   public void Dispose(){F?.Dispose();scope?.Dispose();}
  }
  [TestCase("Hatchet")][TestCase("Cudgel")][TestCase("LeatherBoots")]
  public void ActualWholeCarriedOwnerUsesExactPortableMeshPaletteAndOwnedBill(string blueprint)
  {
   using(var f=new Fixture(blueprint))
   {
    Assert.False(f.View(out _));f.Pick();Assert.True(f.View(out var carry));Assert.True(SpawnRing3DIntegrationFixture.Drawn(carry));Assert.True(f.F.Find(f.Actor,out var body,out _));var socket=body.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Collector.Bill");Assert.True(carry.transform.IsChildOf(socket));
    Assert.True(SpreadPortable3DLibrary.TryRecipe(f.Item,out string id));var entry=SpreadPortable3DLibrary.Load().Find(id);var proof=f.Proof();Assert.AreSame(entry.Mesh,proof.ExpectedMesh);Assert.AreEqual(1,proof.PieceCount);Assert.AreSame(entry.Mesh,carry.GetComponentInChildren<MeshFilter>().sharedMesh);Assert.That(carry.GetComponentsInChildren<Collider>(true),Is.Empty);
    var filter=carry.GetComponentInChildren<MeshFilter>();Assert.Less(filter.sharedMesh.vertices.Min(v=>Vector3.Distance(filter.transform.TransformPoint(v),socket.position)),.08f,"Actual cargo geometry must touch the authored bill grip, not merely share its parent.");
    Assert.True(f.Presenter.TryGetApprovedStyle(f.Actor,out var bodyProof),bodyProof.Failure);Assert.AreEqual(1,bodyProof.PieceCount);Assert.AreNotSame(proof.ExpectedMesh,bodyProof.ExpectedMesh);Assert.AreSame(f.Actor,f.Item.GetPart<PhysicsPart>().InInventory);Assert.AreEqual("Pickup",f.ActionState);
    var vertices=entry.Mesh.vertices;f.F.Frame();Assert.True(f.View(out var second));Assert.AreSame(carry,second);CollectionAssert.AreEqual(vertices,entry.Mesh.vertices);
    f.Act();Assert.AreEqual("Deposited",f.Phase);Assert.AreSame(f.Item,f.Home.GetPart<ContainerPart>().Contents.Single());f.F.Refresh();Assert.False(f.View(out _));SpawnRing3DIntegrationFixture.Hidden(carry);Assert.AreEqual("Deposit",f.ActionState);
   }
  }
  [Test]
  public void StoppedAtDestroyedHomeStillShowsActualRetainedOwner()
  {using(var f=new Fixture()){f.Pick();Assert.True(f.View(out var before));Assert.True(f.F.Zone.RemoveEntity(f.Home));f.Act();Assert.AreEqual("Stopped",f.Phase);Assert.AreSame(f.Item,f.Carried);f.F.Frame();Assert.True(f.View(out var after));Assert.AreSame(before,after);f.Proof();}}
  [TestCase("equipped")][TestCase("other-owner")][TestCase("removed-item")][TestCase("removed-actor")][TestCase("hidden")]
  public void ActualLostOrUnshownCarryClearsWithoutInventoryRepair(string change)
  {
   using(var f=new Fixture())
   {
    f.Pick();Assert.True(f.View(out var before));if(change=="equipped")f.Item.GetPart<PhysicsPart>().Equipped=f.Actor;
    if(change=="other-owner")f.Item.GetPart<PhysicsPart>().InInventory=f.F.Player;
    if(change=="removed-item")Assert.True(f.Actor.GetPart<InventoryPart>().RemoveObject(f.Item));
    if(change=="removed-actor")Assert.True(f.F.Zone.RemoveEntity(f.Actor));
    if(change=="hidden"){f.F.Zone.GetEntityCell(f.Actor).IsVisible=false;f.F.Refresh();}
    var p=f.Item.GetPart<PhysicsPart>();var holder=p.InInventory;var equipped=p.Equipped;f.F.Frame();Assert.False(f.View(out _));SpawnRing3DIntegrationFixture.Hidden(before);Assert.AreSame(holder,p.InInventory);Assert.AreSame(equipped,p.Equipped);
   }
  }
  [Test]
  public void LegitimatePositiveQuantityChangeStillShowsActualRetainedItem()
  {using(var f=new Fixture()){f.Pick();var stack=f.Item.GetPart<StackerPart>();Assert.NotNull(stack);stack.StackCount++;Assert.AreSame(f.Item,f.Carried);f.F.Frame();Assert.True(f.View(out _));f.Proof();}}
  [Test]
  public void LoadedCarryingBindsReplacementOwnerWithoutFalsePickupGesture()
  {using(var f=new Fixture()){f.Pick();Assert.True(f.View(out var old));var oldItem=f.Item;f.Load();Assert.AreNotSame(oldItem,f.Item);Assert.AreSame(f.Item,f.Carried);Assert.True(f.View(out var now));Assert.AreNotSame(old,now);SpawnRing3DIntegrationFixture.Hidden(old);Assert.AreNotEqual("Pickup",f.ActionState);Assert.AreNotEqual("Deposit",f.ActionState);f.Proof();}}
  [Test]
  public void DeathClearsAttachmentAndOrdinaryDropKeepsSameRealItemOnce()
  {using(var f=new Fixture()){f.Pick();Assert.True(f.View(out var before));CombatSystem.HandleDeath(f.Actor,null,f.F.Zone);f.F.Refresh();Assert.False(f.View(out _));SpawnRing3DIntegrationFixture.Hidden(before);Assert.AreEqual(1,f.F.Zone.GetReadOnlyEntities().Count(e=>ReferenceEquals(e,f.Item)));Assert.IsNull(f.Item.GetPart<PhysicsPart>().InInventory);Assert.IsNull(f.Item.GetPart<PhysicsPart>().Equipped);}}
  [Test]
  public void PresenterReleaseDestroysOnlyOwnedCarryAndLeavesActualInventory()
  {using(var f=new Fixture()){f.Pick();Assert.True(f.View(out var before));var item=f.Item;UnityEngine.Object.DestroyImmediate(f.F.Presenter);SpawnRing3DIntegrationFixture.Hidden(before);Assert.AreSame(f.Actor,item.GetPart<PhysicsPart>().InInventory);Assert.True(f.Actor.GetPart<InventoryPart>().Objects.Contains(item));}}
 }
}
