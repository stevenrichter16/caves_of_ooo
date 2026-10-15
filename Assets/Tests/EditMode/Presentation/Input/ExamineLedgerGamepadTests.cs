using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace CavesOfOoo.Tests
{
 public sealed class ExamineLedgerGamepadTests
 {
  readonly InputTestFixture scope=new InputTestFixture();Gamepad pad;GameObject host;AnnouncementUI ui;
  void State(GamepadState state){InputSystem.QueueStateEvent(pad,state);InputSystem.Update();InputHelper.GetKey(KeyCode.None);}
  [SetUp]public void Setup(){scope.Setup();pad=InputSystem.AddDevice<Gamepad>();pad.MakeCurrent();State(new GamepadState());NativeGamepadInput.SetContext(GamepadInputContext.Menu);State(new GamepadState());host=new GameObject("Deck ledger controls");host.SetActive(false);ui=host.AddComponent<AnnouncementUI>();}
  [TearDown]public void Cleanup(){ui.Close();Object.DestroyImmediate(host);NativeGamepadInput.SetContext(GamepadInputContext.Menu);scope.TearDown();}
  void Long(){ui.Open(string.Join("\n",Enumerable.Range(0,80).Select(i=>"Line "+i)));Assert.Greater(ui.PageCount,1);}
  [TestCase(GamepadButton.RightShoulder,0,1)][TestCase(GamepadButton.LeftShoulder,1,0)]
  [TestCase(GamepadButton.DpadRight,0,1)][TestCase(GamepadButton.DpadLeft,1,0)]
  public void ShoulderAndDpadPageOnceWithoutWorldCommand(GamepadButton button,int from,int to)
  {
   Long();ui.GoToPage(from);State(new GamepadState().WithButton(button));ui.HandleInput();Assert.AreEqual(to,ui.CurrentPage);
   State(new GamepadState().WithButton(button));ui.HandleInput();Assert.AreEqual(to,ui.CurrentPage);
   Assert.False(NativeGamepadInput.IsPressed(GamepadCommand.AttackNearest));Assert.False(NativeGamepadInput.IsPressed(GamepadCommand.Fire));
   State(new GamepadState());ui.HandleInput();Assert.True(ui.IsOpen);
  }
  [Test]public void AAdvancesAndOnlyClosesLastPage_BClosesFromAnyPage()
  {
   Long();StringAssert.Contains("[LB]",ui.FooterText);StringAssert.Contains("[RB]",ui.FooterText);State(new GamepadState().WithButton(GamepadButton.South));ui.HandleInput();Assert.True(ui.IsOpen);Assert.AreEqual(1,ui.CurrentPage);
   State(new GamepadState());ui.GoToPage(ui.PageCount-1);State(new GamepadState().WithButton(GamepadButton.South));ui.HandleInput();Assert.False(ui.IsOpen);
   State(new GamepadState());Long();State(new GamepadState().WithButton(GamepadButton.East));ui.HandleInput();Assert.False(ui.IsOpen);
  }
  [Test]public void ShortNoticeShowsDeckHintsAndACloses()
  {ui.Open("Level up");StringAssert.Contains("[A] Close",ui.FooterText);StringAssert.Contains("[B] Back",ui.FooterText);StringAssert.DoesNotContain("Enter",ui.FooterText);State(new GamepadState().WithButton(GamepadButton.South));ui.HandleInput();Assert.False(ui.IsOpen);}
 }
}
