using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadReaderPagingKeyTests
 {
  readonly InputTestFixture input=new InputTestFixture();Keyboard keyboard;GameObject owned;AnnouncementUI reader;
  [SetUp]public void Setup(){input.Setup();keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();owned=new GameObject("Owned reader paging input");owned.SetActive(false);reader=owned.AddComponent<AnnouncementUI>();reader.Open(string.Join("\n",Enumerable.Range(0,80).Select(i=>"reader paragraph "+i)));Assert.AreEqual(3,reader.PageCount);Release();}
  [TearDown]public void Cleanup(){if(owned!=null)Object.DestroyImmediate(owned);input.TearDown();}
  void Release(){InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();}
  [TestCase(Key.PageDown,KeyCode.PageDown,0,1)][TestCase(Key.PageUp,KeyCode.PageUp,1,0)]
  [TestCase(Key.RightArrow,KeyCode.RightArrow,0,1)][TestCase(Key.LeftArrow,KeyCode.LeftArrow,1,0)]
  public void ActualNewBackendPagingPressHoldReleaseUsesTheExistingReader(Key key,KeyCode legacy,int start,int expected)
  {
   Assert.True(reader.GoToPage(start));InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));InputSystem.Update();
   Assert.True(keyboard[key].wasPressedThisFrame,"Actual isolated new input press premise.");Assert.True(InputHelper.GetKeyDown(legacy));Assert.True(InputHelper.GetKey(legacy));
   reader.HandleInput();Assert.AreEqual("reader paragraph "+(expected*33),reader.VisibleLines[0]);
   InputSystem.Update();Assert.False(InputHelper.GetKeyDown(legacy),"Holding must not become a second press.");reader.HandleInput();Assert.AreEqual("reader paragraph "+(expected*33),reader.VisibleLines[0]);
   Release();Assert.True(InputHelper.GetKeyUp(legacy));Assert.False(InputHelper.GetKey(legacy));reader.HandleInput();Assert.True(reader.IsOpen);Assert.AreEqual("reader paragraph "+(expected*33),reader.VisibleLines[0]);
  }
 }
}
