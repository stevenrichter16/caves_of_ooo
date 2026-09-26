using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class DensityTerrainNavigationTests
 {
  Zone zone; Entity actor;
  [SetUp] public void Setup()
  {
   LiquidRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/LiquidDefinitions"),"*.json").Select(File.ReadAllText));
   zone=new Zone("TerrainNavigation"); actor=new Entity{ID="terrain-walker"};actor.SetTag("Creature");actor.AddPart(new PhysicsPart{Solid=true});actor.AddPart(new StatusEffectsPart());
   foreach(string s in new[]{"Hitpoints","Strength","Toughness","Agility"})actor.Statistics[s]=new Stat{Name=s,BaseValue=20,Max=100};
   Assert.True(zone.AddEntity(actor,5,5));
  }
  [TearDown] public void Cleanup()=>LiquidRegistry.ResetForTests();
  Entity Pool(string id,int volume=100,int x=8,int y=5)
  {var e=new Entity{ID=Guid.NewGuid().ToString("N")};e.AddPart(new LiquidPoolPart{LiquidId=id,Volume=volume});Assert.True(zone.AddEntity(e,x,y));return e;}
  int Cost(int x=8,int y=5)=>TerrainNavigationWeight.ForCell(zone.GetCell(x,y),actor);
  HashSet<(int,int)> Cells(FindPath path){int x=5,y=5;var result=new HashSet<(int,int)>();foreach(var step in path.Steps){x+=step.dx;y+=step.dy;result.Add((x,y));}return result;}
  [TestCase("acid")][TestCase("lava")][TestCase("bog-mire")][TestCase("held-breath-lacquer")]
  public void HarmfulActualPoolAddsFiniteCost(string id){Pool(id);Assert.That(Cost(),Is.InRange(1,TerrainNavigationWeight.MaxPenalty));}
  [TestCase("water")][TestCase("convalessence")][TestCase("unknown")]
  public void HarmlessHealingOrUnknownPoolHasNoInventedThreat(string id){Pool(id);Assert.Zero(Cost());}
  [TestCase(0)][TestCase(-1)]public void EmptyDamagingPoolHasNoContactDamage(int volume){Pool("acid",volume);Assert.Zero(Cost());}
  [Test]public void TileOnlyAcidDoesNotPretendToBeAnExposurePool(){zone.TileState.WriteCoating(8,5,"acid",10);Assert.Zero(Cost());}
  [Test]public void TileIceUsesRealSlipRisk(){zone.TileState.WriteCoating(8,5,"ice",10);Assert.Greater(Cost(),0);}
  [Test]public void SlipChanceZeroRemovesSlipCost(){LiquidRegistry.Get("ice").SlipChance=0;zone.TileState.WriteCoating(8,5,"ice",10);Assert.Zero(Cost());}
  [Test]public void SlipperyProjectionIsNotChargedTwice(){Pool("ice");int poolCost=Cost();Assert.True(zone.TileState.HasCoating(8,5,"ice"));zone.TileState.RemoveCoating(8,5,"ice");Assert.Zero(Cost(),"only real floor slip state causes a slip");zone.TileState.WriteCoating(8,5,"ice",10);Assert.AreEqual(poolCost,Cost());}
  [TestCase(0,true)][TestCase(99,true)][TestCase(100,false)][TestCase(150,false)]
  public void FullAcidResistanceOnlyRemovesActualAcidDamage(int resistance,bool danger){actor.Statistics["AcidResistance"]=new Stat{Name="AcidResistance",BaseValue=resistance,Max=1000};Pool("acid");Assert.AreEqual(danger,Cost()>0);}
  [Test]public void IncomingLavaPenaltyPreventsFalseFullImmunity(){actor.Statistics["HeatResistance"]=new Stat{Name="HeatResistance",BaseValue=100,Max=1000};Pool("lava");Assert.Greater(Cost(),0);}
  [Test]public void WrongResistanceDoesNotRemoveDamage(){actor.Statistics["HeatResistance"]=new Stat{Name="HeatResistance",BaseValue=1000,Max=1000};Pool("acid");Assert.Greater(Cost(),0);}
  [Test]public void DamageImmunityDoesNotRemoveOtherHarmfulModifiers(){actor.Statistics["AcidResistance"]=new Stat{Name="AcidResistance",BaseValue=100,Max=1000};Pool("bog-mire");Assert.Greater(Cost(),0);}
  [Test]public void NoCreatureOrNoExposureHasNoPoolDamage(){Pool("acid");actor.Tags.Remove("Creature");Assert.Zero(Cost());actor.SetTag("Creature");actor.GetStat("Strength").BaseValue=0;actor.GetStat("Toughness").BaseValue=0;Assert.Zero(Cost());}
  [Test]public void NullAndUninitializedInputsAreHarmless(){Pool("acid");Assert.Zero(TerrainNavigationWeight.ForCell(null,actor));Assert.Zero(TerrainNavigationWeight.ForCell(zone.GetCell(8,5),null));LiquidRegistry.ResetForTests();Assert.Zero(Cost());}
  [Test]public void ActorDetoursAroundActualAcid(){Pool("acid");var path=FindPath.Search(zone,5,5,11,5,actor:actor);Assert.True(path.Usable);Assert.False(Cells(path).Contains((8,5)));}
  [Test]public void LegacyNoActorAndImmuneActorTakeTheDirectRoute(){Pool("acid");Assert.True(Cells(FindPath.Search(zone,5,5,11,5)).Contains((8,5)));actor.Statistics["AcidResistance"]=new Stat{Name="AcidResistance",BaseValue=100,Max=1000};Assert.True(Cells(FindPath.Search(zone,5,5,11,5,actor:actor)).Contains((8,5)));}
  [Test]public void SoleHazardousPassageRemainsUsable(){for(int x=0;x<Zone.Width;x++)for(int y=0;y<Zone.Height;y++)if(y!=5){var wall=new Entity();wall.SetTag("Solid");zone.AddEntity(wall,x,y);}Pool("acid");var path=FindPath.Search(zone,5,5,11,5,actor:actor);Assert.True(path.Usable);Assert.True(Cells(path).Contains((8,5)));}
  [Test]public void ActorCanEscapeHazardousStart(){Pool("acid",100,5,5);var path=FindPath.Search(zone,5,5,11,5,actor:actor);Assert.True(path.Usable);Assert.AreEqual(6,path.Steps.Count);}
  [Test]public void CandidatePhysicalBodyIncludesRemoteFootCell(){actor.AddPart(new SpatialFootprintPart{CellsRaw="0,0;0,1"});Pool("acid",100,8,6);Assert.Zero(Cost());Assert.Greater(TerrainNavigationWeight.ForStep(zone,8,5,actor),0);}
  [Test]public void RepeatedQueryDoesNotAlterHpEffectsOrTileState(){Pool("acid");int hp=actor.GetStatValue("Hitpoints"),volume=zone.GetCell(8,5).Objects[0].GetPart<LiquidPoolPart>().Volume;for(int i=0;i<100;i++)Assert.Greater(Cost(),0);Assert.AreEqual(hp,actor.GetStatValue("Hitpoints"));Assert.AreEqual(0,actor.GetPart<StatusEffectsPart>().GetAllEffects().Count);Assert.AreEqual(volume,zone.GetCell(8,5).Objects[0].GetPart<LiquidPoolPart>().Volume);}
 }
}
