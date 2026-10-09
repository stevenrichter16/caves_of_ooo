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
 public sealed class SpreadTransientSourceTests
 {
  Dictionary<string,GasDefinition> prior;bool initialized;OverworldZoneManager manager;Zone zone;
  static FieldInfo Registry=>typeof(GasRegistry).GetField("_byId",BindingFlags.NonPublic|BindingFlags.Static);
  static FieldInfo Initialized=>typeof(GasRegistry).GetField("_initialized",BindingFlags.NonPublic|BindingFlags.Static);
  [SetUp] public void Setup()
  {
   prior=new Dictionary<string,GasDefinition>((Dictionary<string,GasDefinition>)Registry.GetValue(null));initialized=(bool)Initialized.GetValue(null);
   GasRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/GasDefinitions"),"*.json").Select(File.ReadAllText));
   manager=OverworldZoneManager.CreateDetached(new EntityFactory(),64);manager.WorldMap.Tiles[4,9]=BiomeType.Spread;manager.WorldMap.SetPOI(4,9,null);
   zone=new Zone("Overworld.4.9.0");manager.SetActiveZone(zone);foreach(var cell in zone.Cells){cell.IsVisible=true;cell.Explored=true;}
  }
  [TearDown] public void Cleanup(){var map=(Dictionary<string,GasDefinition>)Registry.GetValue(null);map.Clear();foreach(var p in prior)map[p.Key]=p.Value;Initialized.SetValue(null,initialized);}
  Entity Gas(string id="poison-vapor",int density=80)=>GasFactory.SpawnGas(zone,20,10,id,density);
  [TestCase("bloom-spores","&m")][TestCase("confusion-vapor","&M")][TestCase("cryo-mist","&C")]
  [TestCase("fungal-spores","&G")][TestCase("marsh-gas","&g")][TestCase("plasma-gas","&R")]
  [TestCase("poison-vapor","&g")][TestCase("sleep-vapor","&B")][TestCase("stun-vapor","&Y")]
  public void ActualRegisteredSourceHasExactIdentityDensityAndColor(string id,string color)
  {
   var gas=Gas(id,83);int version=zone.EntityVersion;string tiles=zone.TileState.ToSaveString();var parts=gas.Parts.ToArray();
   Assert.True(SpreadTransientSource.TryGas(zone,gas,out var sample));Assert.AreSame(gas,sample.Owner);Assert.AreEqual(id,sample.Kind);Assert.AreEqual(color,sample.Color);
   Assert.AreEqual(83,sample.Amount);Assert.AreEqual(3,sample.Band);Assert.AreEqual(20,sample.X);Assert.AreEqual(10,sample.Y);
   for(int i=0;i<3;i++)Assert.True(SpreadTransientSource.TryGas(zone,gas,out _));
   Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(tiles,zone.TileState.ToSaveString());CollectionAssert.AreEqual(parts,gas.Parts);
   Assert.AreEqual(83,gas.GetPart<GasPoolPart>().Density);Assert.AreEqual(color,gas.GetPart<GasPoolPart>().ColorString);
   manager.WorldMap.Tiles[4,9]=BiomeType.Beating;Assert.False(SpreadTransientSource.TryGas(zone,gas,out _));
  }
  [TestCase(0,0)][TestCase(1,1)][TestCase(29,1)][TestCase(30,2)][TestCase(79,2)][TestCase(80,3)][TestCase(int.MaxValue,3)]
  public void DensityBandsReuseNativeBoundariesWithoutChangingRawAmount(int amount,int expected)
  {var gas=Gas(density:amount);bool accepted=SpreadTransientSource.TryGas(zone,gas,out var sample);Assert.AreEqual(expected>0,accepted);if(accepted){Assert.AreEqual(expected,sample.Band);Assert.AreEqual(amount,sample.Amount);}}
  [TestCase("hidden")][TestCase("remembered")][TestCase("unexplored")][TestCase("removed")][TestCase("same-id")]
  [TestCase("part-owner")][TestCase("render-owner")][TestCase("physics-owner")][TestCase("portable")][TestCase("carried")][TestCase("equipped")]
  [TestCase("solid")][TestCase("creature")][TestCase("item")][TestCase("missing-gas-tag")][TestCase("footprint")]
  [TestCase("visual-id")][TestCase("visual-variant")][TestCase("glyph-variant")][TestCase("glyph")][TestCase("unknown-gas")]
  [TestCase("wrong-blueprint")][TestCase("invalid-color")][TestCase("render-color")][TestCase("detached-zone")][TestCase("replaced-cache")]
  public void NearlyIdenticalInvalidSourceRetainsFallback(string fault)
  {
   var gas=Gas();Assert.True(SpreadTransientSource.TryGas(zone,gas,out _));var pool=gas.GetPart<GasPoolPart>();var render=gas.GetPart<RenderPart>();var physics=gas.GetPart<PhysicsPart>();
   switch(fault){case "hidden":render.Visible=false;break;case "remembered":zone.GetCell(20,10).IsVisible=false;break;case "unexplored":zone.GetCell(20,10).Explored=false;break;
   case "removed":zone.RemoveEntity(gas);break;case "same-id":gas=new Entity{ID=gas.ID,BlueprintName=gas.BlueprintName};break;
   case "part-owner":pool.ParentEntity=new Entity();break;case "render-owner":render.ParentEntity=new Entity();break;case "physics-owner":physics.ParentEntity=new Entity();break;
   case "portable":physics.Takeable=true;break;case "carried":physics.InInventory=new Entity();break;case "equipped":physics.Equipped=new Entity();break;
   case "solid":physics.Solid=true;break;case "creature":gas.SetTag("Creature");break;case "item":gas.SetTag("Item");break;case "missing-gas-tag":gas.Tags.Remove("Gas");break;
   case "footprint":gas.AddPart(new SpatialFootprintPart());break;case "visual-id":render.VisualID="other";break;case "visual-variant":render.VisualVariant="other";break;
   case "glyph-variant":render.GlyphVariants="?";break;case "glyph":render.RenderString="?";break;case "unknown-gas":pool.GasId="unknown";break;case "wrong-blueprint":gas.BlueprintName="spoof";break;
   case "invalid-color":pool.ColorString=render.ColorString="not-a-color";break;case "render-color":render.ColorString="&R";break;
   case "detached-zone":zone=new Zone(zone.ZoneID);break;case "replaced-cache":manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);break;}
   Assert.False(SpreadTransientSource.TryGas(zone,gas,out _),fault);
  }
  [Test] public void ActualMergeRetainsTheLiveReceiverAndNewRawDensity()
  {var first=Gas(density:29);Assert.True(SpreadTransientSource.TryGas(zone,first,out var before));Assert.AreEqual(1,before.Band);var second=Gas(density:1);Assert.AreSame(first,second);Assert.True(SpreadTransientSource.TryGas(zone,first,out var after));Assert.AreEqual(30,after.Amount);Assert.AreEqual(2,after.Band);Assert.AreEqual(1,zone.EntityCount);}
  [Test] public void CurrentColorAndTypeAreObservedWithoutMutatingTheRegistry()
  {var gas=Gas();var definition=GasRegistry.Get("poison-vapor");var pool=gas.GetPart<GasPoolPart>();pool.ColorString=gas.GetPart<RenderPart>().ColorString="&M";pool.GasType="ObservedType";GasVisuals.Refresh(gas,pool,zone);Assert.True(SpreadTransientSource.TryGas(zone,gas,out var sample));Assert.AreEqual("&M",sample.Color);Assert.AreEqual("ObservedType",sample.GasType);Assert.AreEqual("&g",definition.Color);}
  [TestCase("smoke")][TestCase("steam")][TestCase("heat")][TestCase("cold")][TestCase("charge")]
  public void ActualTileElementIsVisibleOnlyAndEndsWithItsNativeState(string kind)
  {
   if(kind=="heat")zone.TileState.AddHeat(20,10,2);else if(kind=="cold")zone.TileState.AddCold(20,10,2);else if(kind=="charge")zone.TileState.AddCharge(20,10,2);else zone.TileState.WriteCloud(20,10,kind,2);
   string state=zone.TileState.ToSaveString();Assert.True(SpreadTransientSource.TryElement(zone,20,10,out var sample));Assert.AreEqual(kind,sample.Kind);Assert.AreEqual(2,sample.Amount);Assert.IsNull(sample.Owner);Assert.AreEqual(state,zone.TileState.ToSaveString());
   zone.GetCell(20,10).IsVisible=false;Assert.False(SpreadTransientSource.TryElement(zone,20,10,out _));zone.GetCell(20,10).IsVisible=true;zone.TileState.Clear(20,10);Assert.False(SpreadTransientSource.TryElement(zone,20,10,out _));
  }
  [TestCase("coating")][TestCase("residue")][TestCase("unknown-cloud")][TestCase("expired-cloud")][TestCase("foreign")]
  public void UnrepresentedOrHigherPriorityTileStateMustNotBeSuppressed(string kind)
  {zone.TileState.WriteCloud(20,10,"steam",2);if(kind=="coating")zone.TileState.WriteCoating(20,10,"water",2);else if(kind=="residue")zone.TileState.WriteResidue(20,10,"embers",2);else if(kind=="unknown-cloud")zone.TileState.WriteCloud(20,10,"unknown",2);else if(kind=="expired-cloud")zone.TileState.Get(20,10).CloudTurns=0;else manager.WorldMap.Tiles[4,9]=BiomeType.Beating;Assert.False(SpreadTransientSource.TryElement(zone,20,10,out _));}
  [Test] public void TileEnergyPriorityMatchesExistingNativeMark()
  {zone.TileState.WriteCloud(20,10,"smoke",3);zone.TileState.AddHeat(20,10,1);zone.TileState.AddCold(20,10,1);zone.TileState.AddCharge(20,10,1);Assert.True(SpreadTransientSource.TryElement(zone,20,10,out var s));Assert.AreEqual("charge",s.Kind);zone.TileState.Get(20,10).Charge=0;Assert.True(SpreadTransientSource.TryElement(zone,20,10,out s));Assert.AreEqual("heat",s.Kind);}
  [Test] public void NullAndOutOfRangeQueriesDoNotInventState(){Assert.False(SpreadTransientSource.TryGas(null,null,out _));Assert.False(SpreadTransientSource.TryElement(zone,-1,0,out _));Assert.False(SpreadTransientSource.TryElement(zone,Zone.Width,0,out _));Assert.AreEqual(0,zone.TileState.WrittenCount);}
  [TestCase("single-permanent",true)][TestCase("finite",false)][TestCase("mixed",false)][TestCase("unrepresented",false)]
  public void OnlyActuallyRepresentedSinglePermanentWaterAllowsItsUnderlyingElement(string state,bool expected)
  {zone.TileState.WriteCoating(20,10,"water",state=="finite"?3:ZoneTileState.Permanent);zone.TileState.AddCharge(20,10,2);if(state=="mixed")zone.TileState.WriteCoating(20,10,"oil",3);Assert.AreEqual(expected,SpreadTransientSource.TryElement(zone,20,10,out var sample,state!="unrepresented"));if(expected)Assert.AreEqual("charge",sample.Kind);}
 }
}
