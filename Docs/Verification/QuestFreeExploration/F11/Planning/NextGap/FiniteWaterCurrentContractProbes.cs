using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests {
// Private planning probes of CURRENT behavior. No proposed production feature.
public sealed class FiniteWaterCurrentContractProbes {
EntityFactory factory; Entity actor; Zone zone; EntityFactory oldFactory;
Dictionary<string,LiquidDefinition> saved; bool initialized;
static FieldInfo Registry=>typeof(LiquidRegistry).GetField("_byId",BindingFlags.Static|BindingFlags.NonPublic);
static FieldInfo Init=>typeof(LiquidRegistry).GetField("_initialized",BindingFlags.Static|BindingFlags.NonPublic);
[SetUp] public void Setup(){factory=new EntityFactory(); factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
oldFactory=MaterialReactionResolver.Factory;MaterialReactionResolver.Factory=factory;
saved=new Dictionary<string,LiquidDefinition>((Dictionary<string,LiquidDefinition>)Registry.GetValue(null));initialized=(bool)Init.GetValue(null);
LiquidRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/LiquidDefinitions"),"*.json").Select(File.ReadAllText));
actor=factory.CreateEntity("Player");zone=new Zone("planning-water");Assert.True(zone.AddEntity(actor,10,10));}
[TearDown] public void Cleanup(){MaterialReactionResolver.Factory=oldFactory;var d=(Dictionary<string,LiquidDefinition>)Registry.GetValue(null);d.Clear();foreach(var p in saved)d[p.Key]=p.Value;Init.SetValue(null,initialized);}
Entity Carry(string bp){var e=factory.CreateEntity(bp);Assert.True(actor.GetPart<InventoryPart>().AddObject(e));return e;}
[Test] public void RealWaterskinFillHasNoIncrementalCarriedWeight(){var skin=Carry("Waterskin");int before=actor.GetPart<InventoryPart>().GetCarriedWeight();var pool=factory.CreateEntity("SpreadDrawPoint");zone.AddEntity(pool,11,10);Assert.True(InventorySystem.PerformAction(actor,skin,"FillWaterskin",zone));Assert.AreEqual(3,skin.GetPart<WaterskinPart>().Charges);Assert.AreEqual(before,actor.GetPart<InventoryPart>().GetCarriedWeight());Carry("Waterskin");Assert.Greater(actor.GetPart<InventoryPart>().GetCarriedWeight(),before);}
[TestCase(1,true)][TestCase(2,false)] public void FillRangeIsOneCellEvenWhenSourceCellIsBlocked(int dx,bool expected){var skin=Carry("Waterskin");var pool=factory.CreateEntity("SpreadDrawPoint");zone.AddEntity(pool,10+dx,10);var wall=new Entity();wall.AddPart(new PhysicsPart{Solid=true});zone.AddEntity(wall,10+dx,10);Assert.True(zone.GetCell(10+dx,10).BlocksMovement());Assert.AreEqual(expected,InventorySystem.PerformAction(actor,skin,"FillWaterskin",zone));Assert.AreEqual(expected?0:3,pool.GetPart<LiquidPoolPart>().Volume);}
[TestCase(true)][TestCase(false)] public void WetCoatingCuresParchedWithoutAnyFinitePool(bool wet){var effect=new ParchedEffect();actor.ApplyEffect(effect);if(wet)zone.TileState.WriteCoating(10,10,"water",ZoneTileState.Permanent);var e=GameEvent.New("EndTurn");e.SetParameter("Zone",(object)zone);effect.OnTurnEnd(actor,e);e.Release();Assert.AreEqual(wet?0:-1,effect.Duration);Assert.False(zone.GetReadOnlyEntities().Any(x=>x.HasPart<LiquidPoolPart>()));}
[Test] public void DrinkingWaterDoesNotWetActor(){var skin=Carry("Waterskin");skin.GetPart<WaterskinPart>().Charges=1;Assert.True(InventorySystem.PerformAction(actor,skin,"DrinkWaterskin",zone));Assert.Null(actor.GetEffect<WetEffect>());Assert.Null(actor.GetEffect<LiquidCoveredEffect>());}
[Test] public void WaterPourDoesNotWaterCropWhileCropWaterDoes(){var flask=Carry("LiquidFlask");flask.GetPart<LiquidVesselPart>().LiquidId="water";flask.GetPart<LiquidVesselPart>().Volume=3;var crop=new Entity();crop.AddPart(new PhysicsPart());crop.AddPart(new RenderPart());crop.AddPart(new CropPart());zone.AddEntity(crop,11,10);string cmd=InventorySystem.GetActions(actor,flask).Single(x=>x.Name=="PourLiquid"&&x.Command.EndsWith("|11|10")).Command;Assert.True(InventorySystem.PerformAction(actor,flask,cmd,zone));Assert.True(zone.TileState.HasCoating(11,10,"water"));Assert.AreEqual(0,crop.GetPart<CropPart>().MoistureTicks);crop.GetPart<CropPart>().Water(40);Assert.AreEqual(40,crop.GetPart<CropPart>().MoistureTicks);}
[Test] public void SourceActionsDoNotPretendVesselFillIsAWorldAction(){var source=factory.CreateEntity("SpreadDrawPoint");zone.AddEntity(source,11,10);Assert.False(InventorySystem.GetActions(actor,source).Any(x=>x.Command=="FillWaterskin"||x.Command=="DrinkWaterskin"));var skin=Carry("Waterskin");Assert.True(InventorySystem.GetActions(actor,skin).Any(x=>x.Command=="FillWaterskin"));}
}}
