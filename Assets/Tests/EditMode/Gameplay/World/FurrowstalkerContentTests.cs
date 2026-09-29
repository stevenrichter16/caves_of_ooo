using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class FurrowstalkerContentTests
 {
  EntityFactory factory;
  [SetUp]public void Setup(){factory=new EntityFactory();factory.LoadBlueprints(File.ReadAllText(Environment.GetEnvironmentVariable("COO_FURROWSTALKER_BLUEPRINTS")??Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));}
  Entity Make(string name){Assert.True(factory.Blueprints.ContainsKey(name),"Missing original source "+name);return factory.CreateEntity(name);}
  [Test]public void OriginalHunterUsesRegisteredExactRoleAndQuadrupedBodyBite(){var e=Make("Furrowstalker");Assert.AreSame(e,e.GetPart("SpreadPredator").ParentEntity);Assert.AreEqual("SpreadPredatorPart",e.GetPart("SpreadPredator").GetType().Name);Assert.False((bool)e.GetPart("SpreadPredator").GetType().GetField("Configured").GetValue(e.GetPart("SpreadPredator")));Assert.AreEqual("Quadruped",e.GetProperty("Anatomy"));Assert.AreEqual(0,e.GetPart<Body>().GetPartsByType("Hand").Count);Assert.True(e.HasTag("BodyNaturalAttack"));Assert.AreEqual("1d4",e.GetPart<MeleeWeaponPart>().BaseDamage);Assert.IsNull(e.GetProperty("NaturalWeapon"));Assert.False(e.GetPart<PhysicsPart>().Takeable);Assert.IsNull(e.GetPart<LoadoutPart>());}
  [Test]public void OrdinaryHunterStatsAndFactionRemainLocalRatherThanGlobalPreyHostility(){var e=Make("Furrowstalker");Assert.AreEqual(12,e.GetStatValue("Hitpoints"));Assert.AreEqual(110,e.GetStatValue("Speed"));Assert.AreEqual(10,e.GetStatValue("Strength"));Assert.AreEqual(14,e.GetStatValue("Agility"));Assert.AreEqual("Beasts",e.GetTag("Faction"));Assert.AreEqual("1",e.GetTag("Tier"));Assert.AreEqual("DeathBeastT1",LootDropSystem.ResolveTableName(e));Assert.False(e.GetPart<BrainPart>().Passive);Assert.False(e.GetPart<BrainPart>().Wanders);Assert.False(e.GetPart<BrainPart>().WandersRandomly);Assert.AreEqual(8,e.GetPart<BrainPart>().SightRadius);}
  [Test]public void OriginalBodyAndRemainsKeepDistinctAuthoredRenderAndNoBonusHarvest(){var e=Make("Furrowstalker");var c=Make("FurrowstalkerCorpse");Assert.AreEqual("f",e.GetPart<RenderPart>().RenderString);Assert.AreEqual("%",c.GetPart<RenderPart>().RenderString);Assert.AreEqual("&y",e.GetPart<RenderPart>().ColorString);Assert.AreEqual("&y",c.GetPart<RenderPart>().ColorString);Assert.AreEqual(10,e.GetPart<RenderPart>().RenderLayer);Assert.AreEqual("FurrowstalkerCorpse",e.GetPart<CorpsePart>().CorpseBlueprint);Assert.AreEqual(70,e.GetPart<CorpsePart>().CorpseChance);Assert.True(string.IsNullOrEmpty(e.GetPart<CorpsePart>().HarvestBlueprint));Assert.IsNull(c.GetPart<HarvestablePart>());Assert.True(c.HasTag("Corpse"));Assert.True(c.GetPart<PhysicsPart>().Takeable);Assert.IsNull(c.GetPart<BrainPart>());}
  [Test]public void ExistingGrazerCorpseChanceAndHandRecipePopulationAreUnchanged(){var grazer=Make("ReedbackGrazer");Assert.AreEqual(70,grazer.GetPart<CorpsePart>().CorpseChance);Assert.AreEqual("ReedbackGrazerCorpse",grazer.GetPart<CorpsePart>().CorpseBlueprint);Assert.True(string.IsNullOrEmpty(grazer.GetPart<CorpsePart>().HarvestBlueprint));Assert.IsNull(grazer.GetPart("SpreadPredator"));Assert.False(grazer.HasTag("BodyNaturalAttack"));Assert.AreEqual(48,factory.Blueprints.Values.Count(b=>b.Props.TryGetValue("NaturalWeapon",out var v)&&!string.IsNullOrEmpty(v)));}
 }
}
