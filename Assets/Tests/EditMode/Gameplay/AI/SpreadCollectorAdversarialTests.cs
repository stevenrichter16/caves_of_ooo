using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public class SpreadCollectorAdversarialTests
 {
  [TestCase("party")] [TestCase("nonpassive")] [TestCase("foreign-zone")]
  public void ActualCarryPresentationSurvivesDutyInterruption(string state)
  {using(var f=new SpreadCollectorTests.Fixture()){f.Pick();if(state=="party")f.Brain.SetPartyLeader(f.Base.Player);if(state=="nonpassive")f.Brain.Passive=false;if(state=="foreign-zone"){Assert.True(f.Zone.RemoveEntity(f.Actor));var z=new Zone("Overworld.1.1.0");Assert.True(z.AddEntity(f.Actor,10,10));f.Brain.CurrentZone=z;}Assert.AreSame(f.Item,f.Carried,"actual owned carried salvage remains visible even though original duty cannot run");}}
  [TestCase("removed")] [TestCase("dead")] [TestCase("wrong-backlink")] [TestCase("equipped")]
  public void InvalidLiveCarryNeverSuppliesVisualOwner(string state)
  {using(var f=new SpreadCollectorTests.Fixture()){if(state=="equipped")f.Item.GetPart<StackerPart>().StackCount=1;f.Pick();if(state=="removed")f.Zone.RemoveEntity(f.Actor);if(state=="dead")f.Actor.Statistics["Hitpoints"].BaseValue=0;if(state=="wrong-backlink")f.Item.GetPart<PhysicsPart>().InInventory=f.Base.Player;if(state=="equipped")Assert.True(InventorySystem.Equip(f.Actor,f.Item));Assert.IsNull(f.Carried);}}
  [Test] public void PickRetainsNewIdentityBesideCompatibleCarriedStack()
  {using(var f=new SpreadCollectorTests.Fixture()){var resident=f.Goods("resident",5);Assert.True(f.Inventory.AddObject(resident));f.Pick();Assert.AreEqual(2,f.Inventory.Objects.Count);Assert.AreEqual(5,resident.GetPart<StackerPart>().StackCount);Assert.AreEqual(3,f.Item.GetPart<StackerPart>().StackCount);f.Turn();Assert.AreSame(resident,f.Inventory.Objects.Single());Assert.AreSame(f.Item,f.Container.Contents.Single());}}
  [Test] public void FullHomeCannotPartiallyMergeAndLoseRemainder()
  {using(var f=new SpreadCollectorTests.Fixture()){var resident=f.Goods("resident",98);Assert.True(f.Container.AddItem(resident));f.Container.MaxItems=1;f.Pick();f.Turn();Assert.AreEqual(98,resident.GetPart<StackerPart>().StackCount);Assert.AreEqual(3,f.Item.GetPart<StackerPart>().StackCount);Assert.AreSame(f.Item,f.Carried);}}
  [Test] public void RealDeathDropsOnlyExactCollectedGoodsOnce()
  {using(var f=new SpreadCollectorTests.Fixture()){f.Pick();var oldFactory=LootDropSystem.Factory;try{LootDropSystem.Factory=null;CombatSystem.HandleDeath(f.Actor,null,f.Zone);CombatSystem.HandleDeath(f.Actor,null,f.Zone);}finally{LootDropSystem.Factory=oldFactory;}Assert.IsNull(f.Carried);Assert.AreEqual(1,f.Zone.GetReadOnlyEntities().Count(x=>ReferenceEquals(x,f.Item)));Assert.IsEmpty(f.Inventory.Objects);Assert.AreEqual(3,f.Item.GetPart<StackerPart>().StackCount);Assert.IsNull(f.Item.GetPart<PhysicsPart>().InInventory);}}
  [Test] public void CompetingExactCollectorsCannotDuplicateOneSource()
  {using(var f=new SpreadCollectorTests.Fixture()){var other=f.Base.Player;other.Tags.Remove("Player");other.Tags["Faction"]="Beasts";f.Base.Move(other,12,10);var part=(Part)Activator.CreateInstance(f.Role.GetType());other.AddPart(part);Assert.True((bool)part.GetType().GetMethod("Configure").Invoke(part,new object[]{f.Zone,f.Home,f.Item}));f.Pick();other.FireEventAndRelease(GameEvent.New("TakeTurn"));Assert.IsEmpty(other.GetPart<InventoryPart>().Objects);Assert.AreSame(f.Item,f.Carried);Assert.AreEqual("Stopped",part.GetType().GetField("Phase").GetValue(part).ToString());}}
  [TestCase("BeforePickup")] [TestCase("BeforeBeingPickedUp")]
  public void ThrowingBeforeHookLeavesOriginalGroundAndNoSavedCarry(string name)
  {using(var f=new SpreadCollectorTests.Fixture()){Assert.True(f.Configure());(name=="BeforePickup"?f.Actor:f.Item).AddPart(new SpreadCollectorTests.Callback{Event=name,Action=()=>throw new InvalidOperationException("before callback")});f.Turn();Assert.IsNull(f.Carried);Assert.NotNull(f.Zone.GetEntityCell(f.Item));Assert.IsEmpty(f.Inventory.Objects);Assert.AreEqual(3,f.Item.GetPart<StackerPart>().StackCount);}}
  [Test] public void CompletionDropIsIndependentCommittedWork()
  {using(var f=new SpreadCollectorTests.Fixture()){Assert.True(f.Configure());int calls=0;f.Actor.AddPart(new SpreadCollectorTests.Callback{Event="AfterPickup",Action=()=>{calls++;Assert.True(InventorySystem.Drop(f.Actor,f.Item,f.Zone));}});f.Turn();Assert.AreEqual(1,calls);Assert.IsNull(f.Carried);Assert.IsEmpty(f.Inventory.Objects);Assert.AreEqual(1,f.Zone.GetReadOnlyEntities().Count(x=>x==f.Item));f.Turn();Assert.AreEqual("Stopped",f.Phase);Assert.IsEmpty(f.Container.Contents);}}
  [Test] public void ThrowingCompletionCannotRollBackCommittedAcquisition()
  {using(var f=new SpreadCollectorTests.Fixture()){Assert.True(f.Configure());f.Item.AddPart(new SpreadCollectorTests.Callback{Event="Taken",Action=()=>throw new InvalidOperationException("after commit")});f.Turn();Assert.AreSame(f.Item,f.Carried);Assert.IsNull(f.Zone.GetEntityCell(f.Item));Assert.AreEqual("Carrying",f.Phase);}}
  [Test] public void ReentrantBoredHookDoesNotDepositInPickupTurn()
  {using(var f=new SpreadCollectorTests.Fixture()){Assert.True(f.Configure());f.Actor.AddPart(new SpreadCollectorTests.Callback{Event="AfterPickup",Action=()=>f.Actor.FireEventAndRelease(GameEvent.New("AIBored"))});f.Turn();Assert.AreSame(f.Item,f.Carried);Assert.IsEmpty(f.Container.Contents);}}
  [Test] public void LockedOrMovedHomeDoesNotChaseOrPutRemotely()
  {using(var f=new SpreadCollectorTests.Fixture()){f.Pick();f.Base.Move(f.Home,9,11);f.Turn();Assert.AreSame(f.Item,f.Carried);Assert.IsEmpty(f.Container.Contents);Assert.AreEqual("Stopped",f.Phase);}}
  [Test] public void SaveStoppedCarriedItemDoesNotMintReplacementHome()
  {using(var f=new SpreadCollectorTests.Fixture()){f.Pick();f.Container.MaxItems=0;f.Turn();Assert.AreEqual("Stopped",f.Phase);f.RoundTrip();Assert.AreSame(f.Item,f.Carried);f.Container.MaxItems=5;f.Turn();Assert.IsEmpty(f.Container.Contents);Assert.False(f.Configure());}}
  [Test] public void FinitePathBudgetHasNoSearchOrRngFallback()
  {using(var f=new SpreadCollectorTests.Fixture()){f.Base.Move(f.Item,20,10);Assert.True(f.Configure());f.Role.GetType().GetField("Actions").SetValue(f.Role,48);var pos=f.Zone.GetEntityPosition(f.Actor);f.Turn();Assert.AreEqual("Stopped",f.Phase);Assert.AreEqual(pos,f.Zone.GetEntityPosition(f.Actor));Assert.NotNull(f.Zone.GetEntityCell(f.Item));}}
  [TestCase(false)] [TestCase(true)] public void CarryQueryRefusesMissingOrBorrowedInventory(bool missing)
  {using(var f=new SpreadCollectorTests.Fixture()){f.Pick();if(missing)f.Actor.RemovePart(f.Inventory);else f.Inventory.ParentEntity=f.Base.Player;Assert.IsNull(f.Carried);}}

  [TestCase("NoTrade")] [TestCase("Owned")] [TestCase("quantity")] [TestCase("blueprint")]
  public void ActualCarryRemainsVisibleButChangedDutyGoodsCannotDeposit(string mutation)
  {using(var f=new SpreadCollectorTests.Fixture()){f.Pick();if(mutation=="quantity")f.Item.GetPart<StackerPart>().StackCount=2;else if(mutation=="blueprint")f.Item.BlueprintName="Cudgel";else f.Item.Tags[mutation]="true";Assert.AreSame(f.Item,f.Carried,"presentation observes ownership, not old pickup eligibility");f.Turn();Assert.AreEqual("Stopped",f.Phase);Assert.AreSame(f.Item,f.Carried);Assert.IsEmpty(f.Container.Contents);}}
  [Test] public void EmptySpentStackIsNotCarriedGeometry()
  {using(var f=new SpreadCollectorTests.Fixture()){f.Pick();f.Item.GetPart<StackerPart>().StackCount=0;Assert.IsNull(f.Carried);}}
  [Test] public void RefusedPickupDoesNotModifyRoleTransferredByCallback()
  {using(var f=new SpreadCollectorTests.Fixture()){Assert.True(f.Configure());f.Actor.AddPart(new SpreadCollectorTests.Callback{Event="BeforePickup",Action=()=>{f.Actor.RemovePart(f.Role);f.Base.Player.AddPart(f.Role);f.Role.GetType().GetField("Phase").SetValue(f.Role,Enum.Parse(f.Role.GetType().GetField("Phase").FieldType,"Deposited"));}});f.Turn();Assert.AreSame(f.Base.Player,f.Role.ParentEntity);Assert.AreEqual("Deposited",f.Phase);Assert.IsEmpty(f.Inventory.Objects);Assert.NotNull(f.Zone.GetEntityCell(f.Item));}}
  [Test] public void DepositRollbackDoesNotModifyRoleTransferredByObserver()
  {using(var f=new SpreadCollectorTests.Fixture()){f.Pick();f.Actor.AddPart(new RenderPart{DisplayName="tatterjay"});f.Home.AddPart(new RenderPart{DisplayName="sack"});f.Zone.GetEntityCell(f.Actor).IsVisible=true;f.Zone.GetEntityCell(f.Home).IsVisible=true;int calls=0;MessageLog.OnMessage=message=>{calls++;f.Actor.RemovePart(f.Role);f.Base.Player.AddPart(f.Role);f.Role.GetType().GetField("Phase").SetValue(f.Role,Enum.Parse(f.Role.GetType().GetField("Phase").FieldType,"Seeking"));throw new InvalidOperationException("foreign role observer");};f.Turn();Assert.AreEqual(1,calls);Assert.AreSame(f.Base.Player,f.Role.ParentEntity);Assert.AreEqual("Seeking",f.Phase);Assert.IsEmpty(f.Container.Contents);Assert.AreSame(f.Actor,f.Item.GetPart<PhysicsPart>().InInventory);}}
 }
}
