using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public class PutInContainerNarrationTests
 {
  [TestCase("player")] [TestCase("npc")]
  public void VisibleDepositNamesTheActualActorAndKeepsExactTransfer(string who)
  {using(var f=new SpreadCollectorTests.Fixture(false)){Prepare(f);if(who=="player")f.Actor.Tags["Player"]="true";var log=new List<string>();MessageLog.OnMessage=log.Add;var result=InventorySystem.ExecuteCommand(new PutInContainerCommand(f.Home,f.Item),f.Actor,f.Zone);Assert.True(result.Success);Assert.AreSame(f.Home,f.Item.GetPart<PhysicsPart>().InInventory);Assert.IsEmpty(f.Inventory.Objects);Assert.AreEqual(1,log.Count);StringAssert.StartsWith(who=="player"?"You put ":"tatterjay puts ",log[0]);}}
  [TestCase("actor-hidden")] [TestCase("home-hidden")] [TestCase("actor-detached")] [TestCase("home-detached")] [TestCase("inactive")] [TestCase("actor-invisible")] [TestCase("home-invisible")]
  public void UnobservedNpcDepositDoesNotRevealSourceButStillTransfers(string state)
  {using(var f=new SpreadCollectorTests.Fixture(false)){Prepare(f);if(state=="actor-invisible")f.Actor.GetPart<RenderPart>().Visible=false;if(state=="home-invisible")f.Home.GetPart<RenderPart>().Visible=false;if(state=="actor-hidden")f.Zone.GetEntityCell(f.Actor).IsVisible=false;if(state=="home-hidden")f.Zone.GetEntityCell(f.Home).IsVisible=false;if(state=="actor-detached")f.Zone.RemoveEntity(f.Actor);if(state=="home-detached")f.Zone.RemoveEntity(f.Home);if(state=="inactive"){var m=OverworldZoneManager.CreateDetached(new EntityFactory(),64);m.SetActiveZone(f.Zone);m.SetActiveZone(new Zone("Overworld.1.1.0"));}var log=new List<string>();MessageLog.OnMessage=log.Add;var result=InventorySystem.ExecuteCommand(new PutInContainerCommand(f.Home,f.Item),f.Actor,f.Zone);Assert.True(result.Success,"this is narration admission, not a transfer rule change");Assert.AreSame(f.Home,f.Item.GetPart<PhysicsPart>().InInventory);Assert.IsEmpty(log);}}
  [TestCase(false)] [TestCase(true)]
  public void UnseenNpcCapacityAndLockRefusalsAreSilentAndLeaveSource(bool locked)
  {using(var f=new SpreadCollectorTests.Fixture(false)){Prepare(f);f.Zone.GetEntityCell(f.Actor).IsVisible=false;if(locked)f.Home.AddPart(new LockPart{IsLocked=true});else f.Container.MaxItems=0;var log=new List<string>();MessageLog.OnMessage=log.Add;var result=InventorySystem.ExecuteCommand(new PutInContainerCommand(f.Home,f.Item),f.Actor,f.Zone);Assert.False(result.Success);Assert.AreSame(f.Actor,f.Item.GetPart<PhysicsPart>().InInventory);Assert.AreEqual(3,f.Item.GetPart<StackerPart>().StackCount);Assert.IsEmpty(f.Container.Contents);Assert.IsEmpty(log);}}
  [Test] public void PlayerDepositKeepsFeedbackOutsideAVisibleWorldCell()
  {using(var f=new SpreadCollectorTests.Fixture(false)){Prepare(f);f.Actor.Tags["Player"]="true";f.Zone.GetEntityCell(f.Actor).IsVisible=false;var log=new List<string>();MessageLog.OnMessage=log.Add;Assert.True(InventorySystem.ExecuteCommand(new PutInContainerCommand(f.Home,f.Item),f.Actor,f.Zone).Success);Assert.AreEqual(1,log.Count);StringAssert.StartsWith("You put ",log[0]);}}
  internal static void Prepare(SpreadCollectorTests.Fixture f)
  {f.Actor.AddPart(new RenderPart{DisplayName="tatterjay"});f.Home.AddPart(new RenderPart{DisplayName="sack"});f.Zone.GetEntityCell(f.Actor).IsVisible=true;f.Zone.GetEntityCell(f.Home).IsVisible=true;Assert.True(f.Zone.RemoveEntity(f.Item));Assert.True(f.Inventory.AddObject(f.Item));Assert.True(f.Inventory.Objects.Contains(f.Item));}
 }
}
