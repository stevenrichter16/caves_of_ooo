using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadTransientRenderingAdversarialTests
 {
  Dictionary<string,GasDefinition> prior;bool initialized;
  Dictionary<string,LiquidDefinition> priorLiquids;bool liquidsInitialized;
  static FieldInfo Liquids=>typeof(LiquidRegistry).GetField("_byId",BindingFlags.NonPublic|BindingFlags.Static);
  static FieldInfo LiquidsInitialized=>typeof(LiquidRegistry).GetField("_initialized",BindingFlags.NonPublic|BindingFlags.Static);
  static FieldInfo Registry=>typeof(GasRegistry).GetField("_byId",BindingFlags.NonPublic|BindingFlags.Static);
  static FieldInfo Initialized=>typeof(GasRegistry).GetField("_initialized",BindingFlags.NonPublic|BindingFlags.Static);
  [SetUp]public void Setup(){priorLiquids=new Dictionary<string,LiquidDefinition>((Dictionary<string,LiquidDefinition>)Liquids.GetValue(null));liquidsInitialized=(bool)LiquidsInitialized.GetValue(null);LiquidRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/LiquidDefinitions"),"*.json").Select(File.ReadAllText));prior=new Dictionary<string,GasDefinition>((Dictionary<string,GasDefinition>)Registry.GetValue(null));initialized=(bool)Initialized.GetValue(null);GasRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/GasDefinitions"),"*.json").Select(File.ReadAllText));}
  [TearDown]public void Cleanup(){var liquids=(Dictionary<string,LiquidDefinition>)Liquids.GetValue(null);liquids.Clear();foreach(var p in priorLiquids)liquids[p.Key]=p.Value;LiquidsInitialized.SetValue(null,liquidsInitialized);var map=(Dictionary<string,GasDefinition>)Registry.GetValue(null);map.Clear();foreach(var p in prior)map[p.Key]=p.Value;Initialized.SetValue(null,initialized);}
  static void NearColor(Color expected,Color actual)
  {Assert.That(actual.r,Is.EqualTo(expected.r).Within(.000001f));Assert.That(actual.g,Is.EqualTo(expected.g).Within(.000001f));Assert.That(actual.b,Is.EqualTo(expected.b).Within(.000001f));Assert.That(actual.a,Is.EqualTo(expected.a).Within(.000001f));}
  static bool Gas(SpawnRing3DIntegrationFixture f,Entity owner,out GameObject root){object[] args={owner,null,null};bool found=(bool)f.Call("TryGetGasVolume",args);root=args[1]as GameObject;return found;}
  static bool Tile(SpawnRing3DIntegrationFixture f,out GameObject root){object[] args={20,10,null,null};bool found=(bool)f.Call("TryGetElementVolume",args);root=args[2]as GameObject;return found;}
  [TestCase(false)][TestCase(true)]public void OffsetParentCannotClaimTheOldCell(bool mutate)
  {using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){var owner=GasFactory.SpawnGas(f.Zone,20,10,"poison-vapor",80);f.Refresh();Assert.True(Gas(f,owner,out var root));if(mutate){var shift=new GameObject("Owned adversarial offset");shift.transform.SetParent(((SpawnRing3DPresenter)f.Presenter).ActiveSurface.ContentRoot,false);shift.transform.localPosition=Vector3.right*10;root.transform.SetParent(shift.transform,false);}Assert.AreEqual(!mutate,Gas(f,owner,out _));f.Refresh();Assert.True(Gas(f,owner,out root));Assert.AreEqual(Village3DProjection.CellCentre(20,10),root.transform.position);Assert.AreSame(((SpawnRing3DPresenter)f.Presenter).ActiveSurface.ContentRoot,root.transform.parent);}}
  [TestCase(false)][TestCase(true)]public void ExtraMaterialCannotSubmitDifferentStyleUnderOneProof(bool mutate)
  {using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){var owner=GasFactory.SpawnGas(f.Zone,20,10,"poison-vapor",80);f.Refresh();Assert.True(Gas(f,owner,out var root));var renderer=root.GetComponent<MeshRenderer>();if(mutate)renderer.sharedMaterials=new[]{renderer.sharedMaterial,f.Library.WaterMaterial};Assert.AreEqual(!mutate,Gas(f,owner,out _));f.Refresh();Assert.True(Gas(f,owner,out root));Assert.AreEqual(1,root.GetComponent<MeshRenderer>().sharedMaterials.Length);}}
  [TestCase(false)][TestCase(true)]public void WrongCameraLayerCannotClaimSubmittedGeometry(bool mutate)
  {using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){var owner=GasFactory.SpawnGas(f.Zone,20,10,"poison-vapor",80);f.Refresh();Assert.True(Gas(f,owner,out var root));if(mutate)root.layer=0;Assert.AreEqual(!mutate,Gas(f,owner,out _));f.Refresh();Assert.True(Gas(f,owner,out root));Assert.AreEqual(NativeZone3DRenderSurface.WorldLayer,root.layer);}}
  [TestCase(false)][TestCase(true)]public void ElementOverActualRepresentedPermanentWaterMatchesNativePriority(bool represented)
  {using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){f.Zone.TileState.Clear(20,10);if(represented){var owner=f.Factory.CreateEntity("PouredLiquidPool");var pool=owner.GetPart<LiquidPoolPart>();pool.LiquidId="water";pool.Volume=3;pool.Initialize();Assert.True(f.Zone.AddEntity(owner,20,10));}else f.Zone.TileState.WriteCoating(20,10,"water",3);f.Zone.TileState.AddCharge(20,10,2);f.Refresh();if(represented)Assert.True(f.Water(20,10));Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetElementVolume(20,10,out _,out var sample));Assert.AreEqual(represented?"charge":"coating:water",sample.Kind);}}
  [TestCase(false)][TestCase(true)]public void OwnedIndexedOverrideIsClearedBeforeClaimingRepairedVolume(bool mutate)
  {using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){var owner=GasFactory.SpawnGas(f.Zone,20,10,"poison-vapor",80);f.Refresh();Assert.True(Gas(f,owner,out var root));var r=root.GetComponent<MeshRenderer>();if(mutate){var b=new MaterialPropertyBlock();b.SetFloat("_Transient",0);r.SetPropertyBlock(b,0);}Assert.AreEqual(!mutate,Gas(f,owner,out _));f.Refresh();Assert.True(Gas(f,owner,out root));var after=new MaterialPropertyBlock();root.GetComponent<MeshRenderer>().GetPropertyBlock(after,0);Assert.True(after.isEmpty);}}
  [TestCase(false)][TestCase(true)]public void WrongSemanticColorNeverPassesTolerantNativeProof(bool mutate)
  {using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){var owner=GasFactory.SpawnGas(f.Zone,20,10,"poison-vapor",80);f.Refresh();Assert.True(Gas(f,owner,out var root));var r=root.GetComponent<MeshRenderer>();if(mutate){var b=new MaterialPropertyBlock();r.GetPropertyBlock(b);b.SetColor("_BaseColor",Color.magenta);r.SetPropertyBlock(b);}Assert.AreEqual(!mutate,Gas(f,owner,out _));f.Refresh();Assert.True(Gas(f,owner,out root));var after=new MaterialPropertyBlock();root.GetComponent<MeshRenderer>().GetPropertyBlock(after);NearColor(QudColorParser.Parse("&g").linear,after.GetColor("_BaseColor"));}}
  [TestCase("heat",1f,.35f,.15f)][TestCase("cold",.65f,.9f,1f)][TestCase("charge",1f,.95f,.35f)]
  [TestCase("smoke",.8f,.8f,.85f)][TestCase("steam",.8f,.8f,.85f)]
  public void ExactPriorElementRgbReachesOwnedRendererOnce(string kind,float red,float green,float blue)
  {using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){f.Zone.TileState.Clear(20,10);if(kind=="heat")f.Zone.TileState.AddHeat(20,10,1);else if(kind=="cold")f.Zone.TileState.AddCold(20,10,1);else if(kind=="charge")f.Zone.TileState.AddCharge(20,10,1);else f.Zone.TileState.WriteCloud(20,10,kind,2);f.Refresh();Assert.True(Tile(f,out var root));var block=new MaterialPropertyBlock();root.GetComponent<Renderer>().GetPropertyBlock(block);NearColor(new Color(red,green,blue).linear,block.GetColor("_BaseColor"));}}
 }
}
