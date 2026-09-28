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
namespace CavesOfOoo.Tests
{
 public sealed class SpreadExplorationContentTests
 {
  EntityFactory factory;Dictionary<string,LiquidDefinition> saved;bool initialized;
  static FieldInfo Registry=>typeof(LiquidRegistry).GetField("_byId",BindingFlags.Static|BindingFlags.NonPublic);
  static FieldInfo Init=>typeof(LiquidRegistry).GetField("_initialized",BindingFlags.Static|BindingFlags.NonPublic);
  [SetUp]public void Setup(){factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
   saved=new Dictionary<string,LiquidDefinition>((Dictionary<string,LiquidDefinition>)Registry.GetValue(null));initialized=(bool)Init.GetValue(null);
   LiquidRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/LiquidDefinitions"),"*.json").Select(File.ReadAllText));}
  [TearDown]public void Cleanup(){var d=(Dictionary<string,LiquidDefinition>)Registry.GetValue(null);d.Clear();foreach(var p in saved)d[p.Key]=p.Value;Init.SetValue(null,initialized);}
  Entity Make(string bp){Assert.IsTrue(factory.Blueprints.ContainsKey(bp),"Shipped ordinary content definition missing: "+bp);return factory.CreateEntity(bp);}
  [Test]public void OriginalGrazerHasRealPassiveBodySavedRoleAndNoInventedWeapon()
  {
   var e=Make("ReedbackGrazer");Assert.IsTrue(e.HasTag("Creature"));Assert.IsTrue(e.GetPart<BrainPart>().Passive);
   Assert.NotNull(e.GetPart<Body>());Assert.NotNull(e.GetPart("SpreadGrazer"));Assert.AreEqual("Beasts",e.GetTag("Faction"));
   Assert.AreEqual(10,e.GetStatValue("Hitpoints"));Assert.AreEqual(0,e.GetStatValue("XPValue"));
   Assert.IsTrue(string.IsNullOrEmpty(e.GetProperty("NaturalWeapon")));Assert.AreEqual("ReedbackGrazerCorpse",e.GetPart<CorpsePart>().CorpseBlueprint);
   Assert.IsFalse(Make("PetDog").HasPart("SpreadGrazer"));Assert.IsFalse(Make("MarlbackScrabbler").HasPart("SpreadTerritory"));
  }
  [Test]public void GrazerCorpseHasDistinctIdentityWithoutBonusGleanings()
  {var e=Make("ReedbackGrazerCorpse");Assert.True(e.HasTag("Corpse"));Assert.True(e.GetPart<PhysicsPart>().Takeable);Assert.False(e.HasPart("LootDrop"));Assert.False(e.HasPart<FieldHarvestPart>());StringAssert.Contains("reedback",e.GetDisplayName());}
  [Test]public void RealDrawPointFillsOnlyOneOrdinaryWaterskinAndRemainsAsEmptySource()
  {
   var source=Make("SpreadDrawPoint");var actor=Make("Player");var skin=Make("Waterskin");var z=new Zone("spread-draw-test");
   Assert.True(z.AddEntity(actor,10,10));Assert.True(z.AddEntity(source,11,10));Assert.True(actor.GetPart<InventoryPart>().AddObject(skin));
   var pool=source.GetPart<LiquidPoolPart>();int capacity=skin.GetPart<WaterskinPart>().Capacity;
   Assert.AreEqual("water",pool.LiquidId);Assert.AreEqual(capacity,pool.Volume);Assert.False(source.HasPart<TileStateSourcePart>());Assert.False(source.HasPart<WellPart>());
   Assert.True(InventorySystem.PerformAction(actor,skin,"FillWaterskin",z));Assert.AreEqual(capacity,skin.GetPart<WaterskinPart>().Charges);Assert.AreEqual(0,pool.Volume);Assert.NotNull(z.GetEntityCell(source));
   StringAssert.Contains("empty",source.GetPart<ExaminablePart>().BuildExamineLine().ToLowerInvariant());
   Assert.True(InventorySystem.PerformAction(actor,skin,"DrinkWaterskin",z));Assert.False(InventorySystem.PerformAction(actor,skin,"FillWaterskin",z));Assert.AreEqual(capacity-1,skin.GetPart<WaterskinPart>().Charges);
  }
  [Test]public void CoatingAloneDoesNotOfferADrinkAndCancelledDrawPreservesSource()
  {
   var source=Make("SpreadDrawPoint");var actor=Make("Player");var skin=Make("Waterskin");var z=new Zone("spread-draw-control");
   Assert.True(z.AddEntity(actor,10,10));Assert.True(actor.GetPart<InventoryPart>().AddObject(skin));z.TileState.WriteCoating(11,10,"water",ZoneTileState.Permanent);
   Assert.False(InventorySystem.PerformAction(actor,skin,"FillWaterskin",z));Assert.True(z.AddEntity(source,11,10));
   int before=source.GetPart<LiquidPoolPart>().Volume;var tx=new InventoryTransaction();
   Assert.True(WaterVesselService.TryAct(actor,skin,z,"FillWaterskin",tx));tx.Rollback();
   Assert.AreEqual(before,source.GetPart<LiquidPoolPart>().Volume);Assert.AreEqual(0,skin.GetPart<WaterskinPart>().Charges);
   StringAssert.DoesNotContain("empty",source.GetPart<ExaminablePart>().BuildExamineLine().ToLowerInvariant());
  }
 }
}
