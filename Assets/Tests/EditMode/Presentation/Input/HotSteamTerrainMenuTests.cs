using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class HotSteamTerrainMenuTests
 {
  const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
  HotbarSaveFixture scope;InputHandler input;WorldActionMenuUI menu;EntityFactory factory;Zone zone;Entity player;
  [SetUp]public void Setup()
  {
   scope=new HotbarSaveFixture(true,false);input=scope.Input;player=input.PlayerEntity;zone=input.CurrentZone;
   menu=scope.Root.AddComponent<WorldActionMenuUI>();input.WorldActionMenuUI=menu;
   factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
  }
  [TearDown]public void Cleanup(){scope?.Dispose();scope=null;}
  List<InventoryAction> Actions=>(List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions",Flags).GetValue(menu);
  object Invoke(string method,params object[] args)=>typeof(InputHandler).GetMethod(method,Flags).Invoke(input,args);
  Entity Put(string name,int x=4,int y=4){var e=factory.CreateEntity(name);Assert.NotNull(e,name);Assert.True(zone.AddEntity(e,x,y));return e;}
  void Open(int x,int y,bool direction=false)
  {
   var f=typeof(InputHandler).GetField("_worldActionMenuReturnState",Flags);f.SetValue(input,Enum.Parse(f.FieldType,direction?"Normal":"LookMode"));
   if(direction)Invoke("InteractInDirection",x-3,y-4);else Invoke("OpenWorldActionMenuOrThrow",x,y);
   Assert.True(menu.IsOpen);
  }
  void Select(string command)
  {
   var action=Actions.SingleOrDefault(a=>a.Command==command);Assert.NotNull(action,"Actual current menu must offer "+command);
   Invoke("ExecuteWorldActionSelection",action,menu.SelectedTarget,menu.SelectedCell,menu.SelectedCellIsPile);
  }
  void Back(){Assert.AreEqual(1,Actions.Count(a=>a.Command==WorldInteractionSystem.PickCellCommand));Select(WorldInteractionSystem.PickCellCommand);}
  [TestCase(true,true,true)][TestCase(false,true,true)][TestCase(false,false,true)][TestCase(false,true,false)]
  public void ActualSourceCanBeSelectedAndExaminedWithoutTimeOrCellMutation(bool direction,bool cloud,bool hot)
  {
   int x=direction?4:15;var grass=Put("Grass",x);var source=Put("OilSeep",x);var water=Put("WaterPuddle",x);var top=cloud?Put("SteamCloud",x):water;
   source.ApplyEffect(new SteamEffect(.7f));source.GetPart<ThermalPart>().Temperature=hot?700:25;
   var cell=zone.GetCell(x,4);var owners=cell.Occupants.ToArray();Assert.False(WorldInteractionSystem.IsPileCell(cell));
   int tick=input.TurnManager.TickCount,energy=input.TurnManager.GetEnergy(player),hp=player.GetStatValue("Hitpoints");float density=source.GetEffect<SteamEffect>().Density;
   Open(x,4,direction);Assert.AreSame(top,menu.SelectedTarget);Assert.False(menu.SelectedCellIsPile);Back();
   Assert.AreEqual(owners.Length,Actions.Count);Select(WorldInteractionSystem.PickTargetCommandPrefix+source.ID);Assert.AreSame(source,menu.SelectedTarget);Assert.False(menu.SelectedCellIsPile);
   long serial=MessageLog.NextSerialValue;Select("Examine");
   var lines=MessageLog.GetRecentEntries(24).Where(e=>e.Serial>=serial&&e.Text.StartsWith("You see",StringComparison.Ordinal)).ToArray();
   Assert.AreEqual(1,lines.Length);StringAssert.Contains(source.GetDisplayName(),lines[0].Text);StringAssert.Contains(hot?"Hot steam - scalds":"Steam - cools",lines[0].Text);
   Assert.AreEqual(tick,input.TurnManager.TickCount);Assert.AreEqual(energy,input.TurnManager.GetEnergy(player));Assert.AreEqual(hp,player.GetStatValue("Hitpoints"));Assert.AreEqual(density,source.GetEffect<SteamEffect>().Density);CollectionAssert.AreEqual(owners,cell.Occupants);
  }
  [TestCase(false)][TestCase(true)]public void OnlyOneValidSelectableOwnerDoesNotOfferPointlessBackNavigation(bool invalidOwner)
  {
   var only=Put("Grass");if(invalidOwner){var other=Put("Grass");other.ID="";}
   Open(4,4);Assert.False(menu.SelectedCellIsPile);Assert.False(Actions.Any(a=>a.Command==WorldInteractionSystem.PickCellCommand));
   Assert.AreEqual(1,WorldInteractionSystem.BuildTargetPickerActions(zone.GetCell(4,4)).Count);
  }
  [Test]public void OneRealItemOverTerrainCanReachTheFloorWithoutBecomingALootPile()
  {var floor=Put("Grass");var item=Put("Dagger");Open(4,4,true);Assert.AreSame(item,menu.SelectedTarget);Assert.False(menu.SelectedCellIsPile);Back();Select(WorldInteractionSystem.PickTargetCommandPrefix+floor.ID);Assert.AreSame(floor,menu.SelectedTarget);Assert.AreEqual(1,Actions.Count(a=>a.Command==WorldInteractionSystem.PickCellCommand));}
  [Test]public void ExistingLootPileKeepsItsSummaryAndExactlyOneRouteToIndividualOwners()
  {Put("Grass");var first=Put("Dagger");Put("Tepuibone");Open(4,4,true);Assert.True(menu.SelectedCellIsPile);Assert.True(Actions.Any(a=>a.Command==WorldInteractionSystem.ViewPileCommand));Back();Assert.False(menu.SelectedCellIsPile);Select(WorldInteractionSystem.PickTargetCommandPrefix+first.ID);Assert.AreSame(first,menu.SelectedTarget);Assert.False(menu.SelectedCellIsPile);Assert.AreEqual(1,Actions.Count(a=>a.Command==WorldInteractionSystem.PickCellCommand));}
  [Test]public void StalePickerOwnerCannotBeExaminedAfterRemoval()
  {
   Put("Grass");var source=Put("OilSeep");Put("SteamCloud");Open(4,4);Back();Assert.True(zone.RemoveEntity(source));
   int tick=input.TurnManager.TickCount;long serial=MessageLog.NextSerialValue;Select(WorldInteractionSystem.PickTargetCommandPrefix+source.ID);
   Assert.AreEqual("LookMode",typeof(InputHandler).GetField("_inputState",Flags).GetValue(input).ToString());Assert.AreEqual(tick,input.TurnManager.TickCount);Assert.False(MessageLog.GetRecentEntries(24).Any(e=>e.Serial>=serial&&e.Text.StartsWith("You see",StringComparison.Ordinal)));
  }
  [Test]public void ReturningFromPickerUsesCurrentRemainingOwnersWithoutBackLoop()
  {
   var source=Put("OilSeep");var cloud=Put("SteamCloud");Open(4,4);Back();Assert.True(zone.RemoveEntity(cloud));Select(WorldInteractionSystem.PickTargetCommandPrefix+source.ID);
   Assert.AreSame(source,menu.SelectedTarget);Assert.False(Actions.Any(a=>a.Command==WorldInteractionSystem.PickCellCommand));Assert.True(Actions.Any(a=>a.Command=="Examine"));
  }
 }
}
