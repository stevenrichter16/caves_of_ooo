using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Rendering;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class WorldAudioItemHandlingTests:WorldAudioWorkFixture
 {
  Entity Item(string material,bool ground)
  {var item=ground?Place("Dagger",10,10):Carry("Dagger");var m=item.GetPart<MaterialPart>();if(m==null)item.AddPart(m=new MaterialPart());m.MaterialID=material;return item;}
  void One(string cue){CollectionAssert.AreEqual(new[]{cue+" take 0"},Assigned());Assert.AreEqual(1,Audio.AcceptedSoundCount);Assert.AreEqual(16,Audio.AllocatedSources);Assert.AreEqual(0,Audio.CombatFocusCount);}
  [TestCase("Wood","HandleWood")][TestCase("Steel","HandleMetal")][TestCase("Iron","HandleMetal")]
  [TestCase("Copper","HandleMetal")][TestCase("Brass","HandleMetal")][TestCase("RustedIron","HandleMetal")]
  [TestCase("Cloth","HandleSoft")][TestCase("Leather","HandleSoft")][TestCase("unknown","Pickup")]
  public void ActualPickupUsesItsSettledMaterialAndRetainsUnknownFallback(string material,string cue)
  {var item=Item(material,true);Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(item,false),Actor,Zone).Success);Assert.True(Pack.Objects.Contains(item));One(cue);}
  [TestCase("Wood","HandleWood")][TestCase("Steel","HandleMetal")][TestCase("Leather","HandleSoft")]
  public void ActualDropUsesItsRealGroundOwner(string material,string cue)
  {var item=Item(material,false);Assert.True(InventorySystem.Drop(Actor,item,Zone));Assert.True(Zone.GetEntityCell(item)==Zone.GetEntityCell(Actor));Assert.False(Pack.Objects.Contains(item));One(cue);}
  [TestCase("Wood","HandleWood")][TestCase("Steel","HandleMetal")][TestCase("Cloth","HandleSoft")]
  public void ExplicitEquipAndUnequipEachUseThePhysicalMaterial(string material,string cue)
  {var item=Item(material,false);Assert.True(InventorySystem.Equip(Actor,item));One(cue);Audio.Update(1);Assert.True(InventorySystem.UnequipItem(Actor,item));Assert.AreEqual(2,Audio.AcceptedSoundCount);Assert.True(Pack.Objects.Contains(item));Assert.True(Assigned().Any(n=>n==cue+" take 1"));}
  [TestCase("veto")][TestCase("throw")]
  public void FailedPickupKeepsItsSourceAndPublishesNothing(string failure)
  {var item=Item("Wood",true);var hook=new HandlingAudioPart{Action=e=>{if(e.ID!="BeforePickup")return true;if(failure=="throw")throw new InvalidOperationException("expected rollback");return false;}};Actor.AddPart(hook);Assert.False(InventorySystem.ExecuteCommand(new PickupCommand(item,false),Actor,Zone).Success);Assert.NotNull(Zone.GetEntityCell(item));Assert.IsEmpty(Assigned());hook.Action=null;Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(item,false),Actor,Zone).Success);One("HandleWood");}
  [TestCase("move")][TestCase("alias")][TestCase("hidden")]
  public void ReentrantAfterPickupCannotPublishStaleOwnership(string mutation)
  {var item=Item("Wood",true);var hook=new HandlingAudioPart{Action=e=>{if(e.ID=="AfterPickup"){if(mutation=="move")Zone.MoveEntity(Actor,10,9);if(mutation=="alias")item.GetPart<PhysicsPart>().InInventory=null;if(mutation=="hidden")item.GetPart<RenderPart>().Visible=false;}return true;}};Actor.AddPart(hook);Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(item,false),Actor,Zone).Success);Assert.AreEqual(0,Audio.AcceptedSoundCount);hook.Action=null;Zone.MoveEntity(Actor,10,10);item.GetPart<PhysicsPart>().InInventory=Actor;item.GetPart<RenderPart>().Visible=true;Assert.True(InventorySystem.Drop(Actor,item,Zone));One("HandleWood");}
  [Test]public void SplitEquipUsesPreparedCloneAfterBeforeEquipMutation()
  {
   var item=Item("Wood",false);var stack=item.GetPart<StackerPart>();if(stack==null)item.AddPart(stack=new StackerPart());stack.StackCount=2;
   Entity prepared=null;Actor.AddPart(new HandlingAudioPart{Action=e=>{if(e.ID=="BeforeEquip"){prepared=e.GetParameter<Entity>("Item");prepared.GetPart<MaterialPart>().MaterialID="Steel";}return true;}});
   Assert.True(InventorySystem.Equip(Actor,item));Assert.NotNull(prepared);Assert.AreNotSame(item,prepared);Assert.AreEqual("Wood",item.GetPart<MaterialPart>().MaterialID);Assert.True(InventorySystem.IsEquipped(Actor,prepared));Assert.AreEqual(1,stack.StackCount);One("HandleMetal");
  }
  [Test]public void ForceRemovedGearAfterEquipHasNoFalseClothingReceipt()
  {var item=Item("Cloth",false);Actor.AddPart(new HandlingAudioPart{Action=e=>{if(e.ID=="AfterEquip")Pack.UnequipFromBodyPart(Pack.FindEquippedBodyPart(e.GetParameter<Entity>("Item")));return true;}});Assert.True(InventorySystem.Equip(Actor,item));Assert.False(InventorySystem.IsEquipped(Actor,item));Assert.AreEqual(0,Audio.AcceptedSoundCount);}
  [Test]public void PartialStackDropKeepsTheEstablishedGenericFallback()
  {var item=Item("Wood",false);var stack=item.GetPart<StackerPart>();if(stack==null)item.AddPart(stack=new StackerPart());stack.StackCount=3;Assert.True(InventorySystem.DropPartial(Actor,item,1,Zone));Assert.AreEqual(2,stack.StackCount);Assert.True(Pack.Objects.Contains(item));One("Pickup");}
  [Test]public void GoldBecomesCurrencyAndKeepsItsEstablishedGenericFallback()
  {var coin=Place("GoldCoin",10,10);int units=coin.GetPart<StackerPart>()?.StackCount??1;int before=TradeSystem.GetDrams(Actor);Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(coin,false),Actor,Zone).Success);Assert.AreEqual(before+units*5,TradeSystem.GetDrams(Actor));Assert.Null(Zone.GetEntityCell(coin));Assert.False(Pack.Objects.Contains(coin));One("Pickup");}
  [TestCase("Wood")][TestCase("unknown")]
  public void NonvisualOwnedItemKeepsGenericFeedbackWithoutInventingMaterialCue(string material)
  {var item=Item(material,true);item.RemovePart(item.GetPart<RenderPart>());Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(item,false),Actor,Zone).Success);Assert.True(Pack.Objects.Contains(item));One("Pickup");}
  [TestCase("HandleSoft")][TestCase("HandleWood")][TestCase("HandleMetal")]
  public void GenericHookCannotForgeMaterialHandling(string name)
  {Assert.True(Enum.TryParse(name,out WorldAudioCue cue));WorldAudioHooks.Emit(cue,Actor,Zone,10,10);Assert.IsEmpty(Assigned());var item=Item("Wood",true);Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(item,false),Actor,Zone).Success);One("HandleWood");}
 }
 public sealed class HandlingAudioPart:Part
 {public Func<GameEvent,bool> Action;public override bool HandleEvent(GameEvent e)=>Action?.Invoke(e)??true;}
}
