using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class DensityTerrainNavigationAdversarialTests
 {
  Zone z; Entity a;
  [SetUp]public void Setup(){LiquidRegistry.Initialize("{\"Liquids\":[{\"Id\":\"hurt\",\"PerTurnDamage\":{\"Amount\":3,\"Type\":\"Acid\"}},{\"Id\":\"slick\",\"Slippery\":true,\"SlipChance\":50},{\"Id\":\"mod\",\"StatModifiers\":[{\"Stat\":\"Agility\",\"Delta\":-2}]}]}");z=new Zone("AdversarialNavigation");a=new Entity();a.SetTag("Creature");a.Statistics["Strength"]=new Stat{Name="Strength",BaseValue=10,Max=1000};a.Statistics["Toughness"]=new Stat{Name="Toughness",BaseValue=10,Max=1000};a.Statistics["AcidResistance"]=new Stat{Name="AcidResistance",BaseValue=0,Max=1000};a.AddPart(new StatusEffectsPart());z.AddEntity(a,1,1);}
  [TearDown]public void Cleanup(){LiquidRegistry.ResetForTests();GasRegistry.ResetForTests();}
  void Pool(string id="hurt",int x=5,int y=5){var e=new Entity();e.AddPart(new LiquidPoolPart{LiquidId=id,Volume=100});z.AddEntity(e,x,y);}
  int Cost()=>TerrainNavigationWeight.ForCell(z.GetCell(5,5),a);
  [TestCase("Acid")][TestCase("acid")][TestCase("ACID")]
  public void SameLiquidImmunityUsesActualCaseInsensitiveContract(string declared){LiquidRegistry.Get("hurt").ImmuneElement=declared;Pool();Assert.Zero(Cost());}
  [TestCase("Acidity")][TestCase("")][TestCase(null)][TestCase("Heat")]
  public void NonmatchingImmunityDoesNotEraseDamage(string declared){LiquidRegistry.Get("hurt").ImmuneElement=declared;Pool();Assert.Greater(Cost(),0);}
  [TestCase("Acid",true)][TestCase("acid",false)][TestCase("ACID",false)]
  public void DamageTypeIsCaseSensitiveEvenThoughDeclaredImmunityIsNot(string type,bool immune){a.GetStat("AcidResistance").BaseValue=100;LiquidRegistry.Get("hurt").PerTurnDamage.Type=type;Pool();Assert.AreEqual(!immune,Cost()>0);}
  [Test]public void MissingStatIsNotARealNegativeModifier(){Pool("mod");Assert.Zero(Cost());a.Statistics["Agility"]=new Stat{Name="Agility",BaseValue=10,Max=100};Assert.Greater(Cost(),0);}
  [Test]public void PositiveModifiersDoNotBecomeHazards(){LiquidRegistry.Get("mod").StatModifiers[0].Delta=2;a.Statistics["Agility"]=new Stat{Name="Agility",BaseValue=10,Max=100};Pool("mod");Assert.Zero(Cost());}
  [Test]public void NullModifierEntriesAreIgnored(){LiquidRegistry.Get("mod").StatModifiers.Add(null);Pool("mod");Assert.DoesNotThrow(()=>Cost());}
  [Test]public void EnormousDamageIsCappedWithoutOverflow(){LiquidRegistry.Get("hurt").PerTurnDamage.Amount=int.MaxValue;Pool();Assert.AreEqual(TerrainNavigationWeight.MaxPenalty,Cost());}
  [TestCase(-1)][TestCase(0)][TestCase(int.MinValue)]
  public void HealingOrZeroDamageNeverCreatesAPenalty(int amount){LiquidRegistry.Get("hurt").PerTurnDamage.Amount=amount;Pool();Assert.Zero(Cost());}
  [Test]public void EnormousSlipChanceIsBounded(){LiquidRegistry.Get("slick").SlipChance=int.MaxValue;z.TileState.WriteCoating(5,5,"slick",5);Assert.That(Cost(),Is.InRange(1,TerrainNavigationWeight.MaxPenalty));}
  [Test]public void RegistryReplacementIsObservedWithoutStaleCache(){Pool();Assert.Greater(Cost(),0);LiquidRegistry.Initialize("{\"Liquids\":[{\"Id\":\"hurt\"}]}");Assert.Zero(Cost());}
  [Test]public void ResistanceChangesAreObservedWithoutStaleCache(){Pool();Assert.Greater(Cost(),0);a.GetStat("AcidResistance").BaseValue=100;Assert.Zero(Cost());a.GetStat("AcidResistance").BaseValue=99;Assert.Greater(Cost(),0);}
  [Test]public void IndependentActorImmunityCannotLeak(){Pool();var b=new Entity();b.SetTag("Creature");b.Statistics["Strength"]=new Stat{Name="Strength",BaseValue=10,Max=100};b.Statistics["AcidResistance"]=new Stat{Name="AcidResistance",BaseValue=100,Max=100};Assert.Zero(TerrainNavigationWeight.ForCell(z.GetCell(5,5),b));Assert.Greater(Cost(),0);}
  [Test]public void MultipleIdenticalPoolsDoNotMultiplyStepCost(){Pool();int one=Cost();Pool();Pool();Assert.AreEqual(one,Cost());}
  [Test]public void AnchorOutsidePhysicalShapeDoesNotAddPhantomDanger(){a.AddPart(new SpatialFootprintPart{CellsRaw="1,0"});Pool();Assert.Zero(TerrainNavigationWeight.ForStep(z,5,5,a));Assert.Greater(TerrainNavigationWeight.ForStep(z,4,5,a),0);}
  [Test]public void PhysicalPoolOffsetIsFoundThroughOccupants(){var p=new Entity();p.AddPart(new LiquidPoolPart{LiquidId="hurt",Volume=100});p.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0"});Assert.True(z.AddEntity(p,4,5));Assert.Greater(Cost(),0);}
  [Test]public void OffMapCandidateAndNullContextDoNotThrow(){Assert.DoesNotThrow(()=>TerrainNavigationWeight.ForStep(z,-1,-1,a));Assert.Zero(TerrainNavigationWeight.ForStep(null,0,0,a));Assert.Zero(TerrainNavigationWeight.ForStep(z,0,0,null));}
  [Test]public void UnrelatedTileEnergyDoesNotInventContactDamage(){z.TileState.AddHeat(5,5,2);z.TileState.AddCold(5,5,2);z.TileState.AddCharge(5,5,2);Assert.Zero(Cost());}
  [Test]public void WholeBodyCostsRetainExistingGasImmunityAndTerrainThreat(){GasRegistry.Initialize("{\"Gases\":[{\"Id\":\"nav-poison\",\"GasType\":\"Poison\",\"DefaultDensity\":100,\"BehaviorKind\":\"Poison\"}]}");a.AddPart(new SpatialFootprintPart{CellsRaw="0,0;0,1"});GasFactory.SpawnGas(z,5,6,"nav-poison",density:100);Assert.Greater(TerrainNavigationWeight.ForStep(z,5,5,a),0);a.AddPart(new GasImmunityPart{GasType="Poison"});Assert.Zero(TerrainNavigationWeight.ForStep(z,5,5,a));Pool();Assert.Greater(TerrainNavigationWeight.ForStep(z,5,5,a),0);}
  [Test]public void CostsDoNotWriteToEmptySparseTileState(){int before=z.TileState.WrittenCount;for(int i=0;i<100;i++)Assert.Zero(TerrainNavigationWeight.ForStep(z,5,5,a));Assert.AreEqual(before,z.TileState.WrittenCount);}
 }
}
