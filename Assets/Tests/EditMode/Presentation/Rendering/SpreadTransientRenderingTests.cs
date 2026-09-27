using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadTransientRenderingTests
 {
  Dictionary<string,GasDefinition> prior;bool initialized;
  Dictionary<string,LiquidDefinition> priorLiquids;bool liquidsInitialized;
  static FieldInfo Liquids=>typeof(LiquidRegistry).GetField("_byId",BindingFlags.NonPublic|BindingFlags.Static);
  static FieldInfo LiquidsInitialized=>typeof(LiquidRegistry).GetField("_initialized",BindingFlags.NonPublic|BindingFlags.Static);
  static FieldInfo Registry=>typeof(GasRegistry).GetField("_byId",BindingFlags.NonPublic|BindingFlags.Static);
  static FieldInfo Initialized=>typeof(GasRegistry).GetField("_initialized",BindingFlags.NonPublic|BindingFlags.Static);
  [SetUp]public void Setup(){priorLiquids=new Dictionary<string,LiquidDefinition>((Dictionary<string,LiquidDefinition>)Liquids.GetValue(null));liquidsInitialized=(bool)LiquidsInitialized.GetValue(null);LiquidRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/LiquidDefinitions"),"*.json").Select(File.ReadAllText));prior=new Dictionary<string,GasDefinition>((Dictionary<string,GasDefinition>)Registry.GetValue(null));initialized=(bool)Initialized.GetValue(null);GasRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/GasDefinitions"),"*.json").Select(File.ReadAllText));}
  [TearDown]public void Cleanup(){var liquids=(Dictionary<string,LiquidDefinition>)Liquids.GetValue(null);liquids.Clear();foreach(var p in priorLiquids)liquids[p.Key]=p.Value;LiquidsInitialized.SetValue(null,liquidsInitialized);var map=(Dictionary<string,GasDefinition>)Registry.GetValue(null);map.Clear();foreach(var p in prior)map[p.Key]=p.Value;Initialized.SetValue(null,initialized);}
  static bool GasView(SpawnRing3DIntegrationFixture f,Entity owner,out GameObject root,out object sample)
  {var method=f.Presenter.GetType().GetMethod("TryGetGasVolume",BindingFlags.Public|BindingFlags.Instance);Assert.NotNull(method,"Native gas volume hook is intentionally absent before this slice.");object[] args={owner,null,null};bool found=(bool)method.Invoke(f.Presenter,args);root=args[1]as GameObject;sample=args[2];return found;}
  static bool TileView(SpawnRing3DIntegrationFixture f,int x,int y,out GameObject root,out object sample)
  {var method=f.Presenter.GetType().GetMethod("TryGetElementVolume",BindingFlags.Public|BindingFlags.Instance);Assert.NotNull(method,"Native live element hook is intentionally absent before this slice.");object[] args={x,y,null,null};bool found=(bool)method.Invoke(f.Presenter,args);root=args[2]as GameObject;sample=args[3];return found;}
  static T Field<T>(object value,string name)=>(T)value.GetType().GetField(name).GetValue(value);
  static void NearColor(Color expected,Color actual)
  {Assert.That(actual.r,Is.EqualTo(expected.r).Within(.000001f));Assert.That(actual.g,Is.EqualTo(expected.g).Within(.000001f));Assert.That(actual.b,Is.EqualTo(expected.b).Within(.000001f));Assert.That(actual.a,Is.EqualTo(expected.a).Within(.000001f));}
  static Mesh AssertActualVolume(SpawnRing3DIntegrationFixture f,GameObject root,string color)
  {
   Assert.NotNull(root);Assert.True(root.activeInHierarchy);var mesh=root.GetComponent<MeshFilter>()?.sharedMesh;var renderer=root.GetComponent<MeshRenderer>();
   Assert.NotNull(mesh);Assert.Greater(mesh.vertexCount,0);Assert.Greater(mesh.triangles.Length,0);Assert.NotNull(renderer);Assert.True(renderer.enabled);Assert.False(renderer.forceRenderingOff);
   var surface=((SpawnRing3DPresenter)f.Presenter).ActiveSurface;Assert.NotNull(surface);Assert.AreSame(surface.MaterialFor(f.Library.WorldMaterial),renderer.sharedMaterial);
   var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);Assert.AreEqual(1,block.GetFloat("_Transient"));Assert.AreSame(Texture2D.whiteTexture,block.GetTexture("_BaseMap"));
   NearColor(QudColorParser.Parse(color).linear,block.GetColor("_BaseColor"));Assert.AreSame(surface.FogTexture,renderer.sharedMaterial.GetTexture("_FogLight"));
   Assert.AreEqual(ShadowCastingMode.Off,renderer.shadowCastingMode);Assert.Zero(root.GetComponentsInChildren<Collider>(true).Length);Assert.Zero(root.GetComponentsInChildren<Rigidbody>(true).Length);
   Assert.LessOrEqual(mesh.bounds.size.x,1f);Assert.LessOrEqual(mesh.bounds.size.z,1f);return mesh;
  }
  [TestCase("bloom-spores")][TestCase("confusion-vapor")][TestCase("cryo-mist")][TestCase("fungal-spores")][TestCase("marsh-gas")]
  [TestCase("plasma-gas")][TestCase("poison-vapor")][TestCase("sleep-vapor")][TestCase("stun-vapor")]
  public void ActualCurrentGasHasExactNativeColorAndOwnedVisibleVolume(string id)
  {using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){var gas=GasFactory.SpawnGas(f.Zone,20,10,id,80);var pool=gas.GetPart<GasPoolPart>();string before=f.Zone.TileState.ToSaveString();int count=f.Zone.EntityCount,version=f.Zone.EntityVersion;f.Refresh();Assert.True(GasView(f,gas,out var root,out var sample));Assert.AreSame(gas,Field<Entity>(sample,"Owner"));Assert.AreEqual(id,Field<string>(sample,"Kind"));Assert.AreEqual(80,Field<int>(sample,"Amount"));var mesh=AssertActualVolume(f,root,pool.ColorString);Assert.True(f.Authored(gas));Assert.True(f.Rendered(gas));f.Refresh();Assert.True(GasView(f,gas,out var repeated,out _));Assert.AreSame(root,repeated);Assert.AreSame(mesh,repeated.GetComponent<MeshFilter>().sharedMesh);Assert.AreEqual(count,f.Zone.EntityCount);Assert.AreEqual(version,f.Zone.EntityVersion);Assert.AreEqual(before,f.Zone.TileState.ToSaveString());Assert.AreEqual(80,pool.Density);}}
  [TestCase("hidden")][TestCase("remembered")][TestCase("removed")][TestCase("zero")][TestCase("foreign")][TestCase("same-id")][TestCase("custom")]
  public void ExactVisibilityOwnershipOrStateLossRelinquishesThePriorVolume(string change)
  {using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){var gas=GasFactory.SpawnGas(f.Zone,20,10,"poison-vapor",80);f.Refresh();Assert.True(GasView(f,gas,out var old,out _));if(change=="hidden")gas.GetPart<RenderPart>().Visible=false;else if(change=="remembered")f.Zone.GetCell(20,10).IsVisible=false;else if(change=="removed")f.Zone.RemoveEntity(gas);else if(change=="zero")gas.GetPart<GasPoolPart>().Density=0;else if(change=="foreign")f.Manager.WorldMap.Tiles[12,10]=BiomeType.Sodden;else if(change=="custom")gas.GetPart<RenderPart>().VisualID="custom";else gas=new Entity{ID=gas.ID,BlueprintName=gas.BlueprintName};f.Refresh();Assert.False(GasView(f,gas,out _,out _));if(change!="same-id")SpawnRing3DIntegrationFixture.Hidden(old);}}
  [Test]public void DensityAndColorChangeRefreshWithoutDuplicatingOrMutatingTheReceiver()
  {using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){var gas=GasFactory.SpawnGas(f.Zone,20,10,"poison-vapor",29);f.Refresh();Assert.True(GasView(f,gas,out var root,out _));var thin=root.GetComponent<MeshFilter>().sharedMesh;Assert.AreSame(gas,GasFactory.SpawnGas(f.Zone,20,10,"poison-vapor",1));f.Refresh();Assert.True(GasView(f,gas,out root,out var sample));var medium=root.GetComponent<MeshFilter>().sharedMesh;Assert.Greater(medium.vertexCount,thin.vertexCount);Assert.AreEqual(30,Field<int>(sample,"Amount"));gas.GetPart<GasPoolPart>().Density=31;GasVisuals.Refresh(gas,gas.GetPart<GasPoolPart>(),f.Zone);f.Refresh();Assert.True(GasView(f,gas,out root,out sample));Assert.AreSame(medium,root.GetComponent<MeshFilter>().sharedMesh);Assert.AreEqual(31,Field<int>(sample,"Amount"));var p=gas.GetPart<GasPoolPart>();p.ColorString=gas.GetPart<RenderPart>().ColorString="&M";GasVisuals.Refresh(gas,p,f.Zone);f.Refresh();Assert.True(GasView(f,gas,out root,out _));AssertActualVolume(f,root,"&M");}}
  [TestCase("steam")][TestCase("smoke")][TestCase("charge")][TestCase("heat")][TestCase("cold")]
  public void RealElementStateCreatesAndClearsOnlyItsCurrentVisibleView(string kind)
  {using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){f.Zone.TileState.Clear(20,10);if(kind=="heat")f.Zone.TileState.AddHeat(20,10,2);else if(kind=="cold")f.Zone.TileState.AddCold(20,10,2);else if(kind=="charge")f.Zone.TileState.AddCharge(20,10,2);else f.Zone.TileState.WriteCloud(20,10,kind,2);string before=f.Zone.TileState.ToSaveString();f.Refresh();Assert.True(TileView(f,20,10,out var root,out var sample));Assert.AreEqual(kind,Field<string>(sample,"Kind"));Assert.NotNull(root.GetComponent<MeshFilter>().sharedMesh);Assert.AreEqual(before,f.Zone.TileState.ToSaveString());f.Zone.TileState.Clear(20,10);f.Refresh();Assert.False(TileView(f,20,10,out _,out _));SpawnRing3DIntegrationFixture.Hidden(root);}}
  [TestCase("coating")][TestCase("residue")][TestCase("unknown")]
  public void SupportedHigherPriorityStateClaimsItsOwnMarkAndUnknownStateRefuses(string state)
  {using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){f.Zone.TileState.Clear(20,10);f.Zone.TileState.WriteCloud(20,10,"steam",2);if(state=="coating")f.Zone.TileState.WriteCoating(20,10,"water",2);else if(state=="residue")f.Zone.TileState.WriteResidue(20,10,"embers",2);else f.Zone.TileState.WriteCloud(20,10,"unknown",2);f.Refresh();bool supported=TileView(f,20,10,out _,out var sample);Assert.AreEqual(state!="unknown",supported);if(supported)Assert.AreEqual(state=="coating"?"coating:water":"residue:embers",Field<string>(sample,"Kind"));}}
  [Test]public void SpreadForeignReturnAndPresenterDisposeReleaseOnlyOwnedVolumes()
  {GameObject old;Mesh owned;Material borrowed;using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){borrowed=f.Library.WorldMaterial;var gas=GasFactory.SpawnGas(f.Zone,20,10,"cryo-mist",80);f.Refresh();Assert.True(GasView(f,gas,out old,out _));owned=old.GetComponent<MeshFilter>().sharedMesh;f.Manager.WorldMap.Tiles[12,10]=BiomeType.Sodden;f.Refresh();Assert.False(GasView(f,gas,out _,out _));SpawnRing3DIntegrationFixture.Hidden(old);Assert.True(owned==null);Assert.True(borrowed!=null);f.Manager.WorldMap.Tiles[12,10]=BiomeType.Spread;f.Refresh();Assert.True(GasView(f,gas,out old,out _));owned=old.GetComponent<MeshFilter>().sharedMesh;}Assert.True(old==null);Assert.True(owned==null);Assert.True(borrowed!=null);}
 }
}
