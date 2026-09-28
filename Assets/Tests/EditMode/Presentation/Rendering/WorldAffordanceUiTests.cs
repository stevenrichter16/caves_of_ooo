using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace CavesOfOoo.Tests
{
 public sealed class WorldAffordanceUiTests
 {
  internal sealed class Fixture:IDisposable
  {
   public readonly WorldAffordanceQueryTests.Fixture Core=new WorldAffordanceQueryTests.Fixture();
   public readonly GameObject Root=new GameObject("owned-affordance-test");public InputHandler Input;public object Marker;
   public Fixture(){Input=Root.AddComponent<InputHandler>();Input.WorldActionMenuUI=Root.AddComponent<WorldActionMenuUI>();Input.PlayerEntity=Core.Player;Input.CurrentZone=Core.Zone;Input.TurnManager=(TurnManager)Activator.CreateInstance(typeof(TurnManager),BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{false},null);typeof(TurnManager).GetProperty("CurrentActor").SetValue(Input.TurnManager,Core.Player);typeof(TurnManager).GetProperty("WaitingForInput").SetValue(Input.TurnManager,true);}
   public void State(string s){var f=typeof(InputHandler).GetField("_inputState",BindingFlags.Instance|BindingFlags.NonPublic);f.SetValue(Input,Enum.Parse(f.FieldType,s));}
   public object Query(){var m=typeof(InputHandler).GetMethod("QueryWorldAffordance");Assert.NotNull(m,"Missing actual input-state hint gate");return m.Invoke(Input,new object[]{Core.Zone,Core.Player});}
   public void CreateMarker(){var t=typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.WorldAffordanceRenderer");Assert.NotNull(t,"Missing restrained one-cell marker renderer");var grid=Root.AddComponent<Grid>();var tile=new GameObject("owned tiles");tile.transform.SetParent(Root.transform);Marker=Activator.CreateInstance(t,new object[]{Root.transform,tile.AddComponent<Tilemap>(),0});}
   public void Refresh(object q)=>Marker.GetType().GetMethod("Refresh").Invoke(Marker,new[]{Core.Player,(object)Core.Zone,q});
   public bool Visible=>(bool)Marker.GetType().GetProperty("IsVisible").GetValue(Marker);
   public void Dispose(){(Marker as IDisposable)?.Dispose();UnityEngine.Object.DestroyImmediate(Root);Core.Dispose();}
  }
  [Test]public void NormalAndLookUseRealDifferentMenuKeysWithoutPaying()
  {using(var f=new Fixture()){f.Core.Row();int ticks=WorldClock.CurrentTick;int energy=f.Input.TurnManager.GetEnergy(f.Core.Player);Assert.AreEqual("C, D: menu / harvest",WorldAffordanceQueryTests.Fixture.Get(f.Query(),"Hint"));var cursor=(WorldCursorState)typeof(InputHandler).GetField("_worldCursorState",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(f.Input);cursor.Activate(WorldCursorMode.Look,f.Core.Zone,11,10,10,10);f.State("LookMode");Assert.AreEqual("Enter: menu / harvest",WorldAffordanceQueryTests.Fixture.Get(f.Query(),"Hint"));Assert.AreEqual(ticks,WorldClock.CurrentTick);Assert.AreEqual(energy,f.Input.TurnManager.GetEnergy(f.Core.Player));}}
  [TestCase("InventoryOpen")][TestCase("WorldActionMenuOpen")][TestCase("AwaitingTalkDirection")][TestCase("WaitingForFxResolution")][TestCase("AnnouncementOpen")]
  public void NonWorldInputStatesShowNoActionCue(string state){using(var f=new Fixture()){f.Core.Row();Assert.NotNull(f.Query());f.State(state);Assert.IsNull(f.Query());}}
  [TestCase("no-menu")][TestCase("not-turn")][TestCase("other-actor")][TestCase("disabled")][TestCase("foreign-zone")]
  public void OnlyActualCurrentPlayerInputCanAdvertise(string changed){using(var f=new Fixture()){f.Core.Row();Assert.NotNull(f.Query());if(changed=="no-menu")f.Input.WorldActionMenuUI=null;if(changed=="not-turn")typeof(TurnManager).GetProperty("WaitingForInput").SetValue(f.Input.TurnManager,false);if(changed=="other-actor")typeof(TurnManager).GetProperty("CurrentActor").SetValue(f.Input.TurnManager,new Entity());if(changed=="disabled")f.Input.enabled=false;if(changed=="foreign-zone")f.Input.CurrentZone=new Zone(f.Core.Zone.ZoneID);Assert.IsNull(f.Query());}}
  // Native paired pixels showed .035 below one screen pixel; the GPU area
  // fixture owns the readable lower bound. Keep this as a broad restraint cap.
  [Test]public void SingleSubtleMarkerUsesCurrentCellAndNoPickingGeometry()
  {using(var f=new Fixture()){var row=f.Core.Row();f.CreateMarker();var q=f.Core.Find();f.Refresh(q);Assert.True(f.Visible);Assert.AreEqual(new Vector2Int(11,10),f.Marker.GetType().GetProperty("CurrentCell").GetValue(f.Marker));var lines=f.Root.GetComponentsInChildren<LineRenderer>();Assert.AreEqual(2,lines.Count(l=>l.enabled));Assert.True(lines.All(l=>l.positionCount==3&&!l.loop&&l.widthMultiplier<=.1f&&l.sortingOrder==7));Assert.IsEmpty(f.Root.GetComponentsInChildren<Collider>());Assert.False(row.GetPart<FieldHarvestPart>().Harvested);}}
  [TestCase("spent")][TestCase("hidden")][TestCase("foreign")]
  public void RetainedMarkerCannotRevealOrKeepStaleSource(string changed){using(var f=new Fixture()){var row=f.Core.Row();f.CreateMarker();var q=f.Core.Find();f.Refresh(q);Assert.True(f.Visible);if(changed=="spent")row.GetPart<FieldHarvestPart>().Harvested=true;if(changed=="hidden")f.Core.Zone.GetEntityCell(row).IsVisible=false;if(changed=="foreign"){f.Core.Zone.RemoveEntity(row);new Zone("foreign").AddEntity(row,11,10);}f.Refresh(q);Assert.False(f.Visible);Assert.AreEqual(new Vector2Int(-1,-1),f.Marker.GetType().GetProperty("CurrentCell").GetValue(f.Marker));}}
  [Test]public void DisposeReleasesOwnedMarkerMaterialAndChildrenOnly()
  {using(var f=new Fixture()){f.Core.Row();var foreign=new GameObject("foreign");foreign.transform.SetParent(f.Root.transform);f.CreateMarker();f.Refresh(f.Core.Find());var lines=f.Root.GetComponentsInChildren<LineRenderer>();var material=lines[0].sharedMaterial;((IDisposable)f.Marker).Dispose();Assert.True(material==null);Assert.True(lines.All(l=>l==null));Assert.True(foreign!=null);f.Marker=null;}}
  [Test]public void HintIsFirstFocusLineAndSharesItsExistingLineBudget()
  {var m=typeof(SidebarTextFormatter).GetMethod("FormatFocus",new[]{typeof(LookSnapshot),typeof(int),typeof(int),typeof(string)});Assert.NotNull(m,"Missing bounded focus hint layout");var snapshot=new LookSnapshot(1,1,"row","",new[]{"detail"},null,null);var lines=(System.Collections.Generic.List<string>)m.Invoke(null,new object[]{snapshot,40,2,"C, .: menu / harvest"});Assert.AreEqual(2,lines.Count);Assert.AreEqual("C, .: menu / harvest",lines[0]);Assert.AreEqual("row",lines[1]);}
 }
}
