using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public abstract class HotSteamContactFixture
 {
  static readonly FieldInfo Reactions=typeof(MaterialReactionResolver).GetField("_reactions",BindingFlags.Static|BindingFlags.NonPublic);
  static readonly FieldInfo Initialized=typeof(MaterialReactionResolver).GetField("_initialized",BindingFlags.Static|BindingFlags.NonPublic);
  internal DensityLootTestScope scope;object oldReactions;bool oldInitialized,snapshotCaptured;CavesOfOoo.Data.EntityFactory oldFactory;
  protected Zone zone; protected Entity source,actor; protected SteamEffect steam;
  [SetUp]public void Setup()
  {
   oldReactions=Reactions.GetValue(null);oldInitialized=(bool)Initialized.GetValue(null);oldFactory=MaterialReactionResolver.Factory;snapshotCaptured=true;
   try
   {
    scope=new DensityLootTestScope();Reactions.SetValue(null,new List<MaterialReactionBlueprint>());
    MaterialReactionResolver.Factory=scope.Factory;
    MaterialReactionResolver.Initialize(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Data/MaterialReactions/water_plus_fire.json")));
    zone=new Zone("hot-steam-contract");source=scope.Factory.CreateEntity("OilSeep");actor=scope.Factory.CreateEntity("Player");
    Assert.True(zone.AddEntity(source,10,10));Assert.True(zone.AddEntity(actor,11,10));
    // The production reaction creates the status on its actual source. Direct
    // normalization below is a labelled isolated admission control, not a claim
    // of natural acquisition or an ordinary player's ability to set temperature.
    var burn=new BurningEffect(1,null,new System.Random(1));source.ApplyEffect(burn);source.ApplyEffect(new WetEffect(.6f));
    MaterialReactionResolver.EvaluateReactions(source,zone,burn);
    steam=source.GetEffect<SteamEffect>();Assert.NotNull(steam);Assert.AreSame(source,steam.Owner);
    Assert.AreEqual(1,zone.GetAllEntities().Count(e=>e.BlueprintName=="SteamCloud"));
    source.RemoveEffect<BurningEffect>();source.RemoveEffect<WetEffect>();
    source.GetPart<ThermalPart>().Temperature=700;source.GetPart<ThermalPart>().AmbientDecayRate=0;
    actor.Statistics["HeatResistance"]=new Stat{Owner=actor,Name="HeatResistance",BaseValue=0,Min=-100,Max=100};
   }catch{try{Cleanup();}catch{/* Preserve the original setup failure. */}throw;}
  }
  [TearDown]public void Cleanup()
  {
   var ownedScope=scope;scope=null;
   try{ownedScope?.Dispose();}
   finally
   {
    if(snapshotCaptured)
    {
     // Null is a valid detached prior snapshot, not a reason to keep test state.
     Reactions.SetValue(null,oldReactions);Initialized.SetValue(null,oldInitialized);MaterialReactionResolver.Factory=oldFactory;snapshotCaptured=false;
    }
   }
  }
  protected void Pulse()=>MaterialSimSystem.TickMaterialEntities(zone);
  protected void DirectPulse()
  {var e=GameEvent.New("BeginTakeAction");e.SetParameter("Zone",(object)zone);source.FireEventAndRelease(e);}
  protected Entity RoundTrip(Entity value)
  {using(var bytes=new MemoryStream()){var w=new SaveWriter(bytes);w.WriteEntityReference(value);w.WriteQueuedEntityBodies();bytes.Position=0;var r=new SaveReader(bytes,scope.Factory);var copy=r.ReadEntityReference();r.ReadEntityBodies();return copy;}}
 }
 public sealed class HotSteamContactTests : HotSteamContactFixture
 {
  [TestCase(25f,0)][TestCase(100f,0)][TestCase(100.01f,2)][TestCase(700f,2)]
  public void OnlyHotReactionOwnedSteamScaldsAndBothClassesStillCoolAndWet(float temperature,int damage)
  {
   source.GetPart<ThermalPart>().Temperature=temperature;int hp=actor.GetStatValue("Hitpoints");float before=actor.GetPart<ThermalPart>().Temperature,density=steam.Density;
   Pulse();Assert.AreEqual(damage,hp-actor.GetStatValue("Hitpoints"));
   Assert.Less(actor.GetPart<ThermalPart>().Temperature,before);Assert.Greater(actor.GetEffect<WetEffect>()?.Moisture??0,0);
   Assert.AreEqual(density-.1f,steam.Density,1e-6f);Assert.AreSame(source,steam.Owner);Assert.AreSame(source,zone.GetCell(10,10).Objects.First(e=>e==source));
  }
  [TestCase(0,2)][TestCase(50,1)][TestCase(100,0)][TestCase(-50,3)]
  public void ScaldUsesTheCanonicalHeatResistancePath(int resistance,int damage)
  {actor.GetStat("HeatResistance").BaseValue=resistance;int hp=actor.GetStatValue("Hitpoints");Pulse();Assert.AreEqual(damage,hp-actor.GetStatValue("Hitpoints"));}
  [TestCase("no-steam")][TestCase("no-thermal")][TestCase("empty-density")][TestCase("expired")]
  [TestCase("detached-source")][TestCase("detached-target")][TestCase("dead-target")][TestCase("death-handled")]
  [TestCase("foreign-effect-owner")][TestCase("foreign-thermal-owner")]
  public void MissingOrStaleLiveAdmissionCannotScald(string state)
  {
   switch(state)
   {
    case "no-steam":source.RemoveEffect<SteamEffect>();break;
    case "no-thermal":source.RemovePart(source.GetPart<ThermalPart>());break;
    case "empty-density":steam.Density=0;break;
    case "expired":steam.Duration=0;break;
    case "detached-source":Assert.True(zone.RemoveEntity(source));break;
    case "detached-target":Assert.True(zone.RemoveEntity(actor));break;
    case "dead-target":actor.GetStat("Hitpoints").BaseValue=0;break;
    case "death-handled":actor.SetTag("_DeathHandled");break;
    case "foreign-effect-owner":steam.Owner=new Entity();break;
    case "foreign-thermal-owner":source.GetPart<ThermalPart>().ParentEntity=new Entity();break;
   }
   int hp=actor.GetStatValue("Hitpoints");Pulse();Assert.AreEqual(hp,actor.GetStatValue("Hitpoints"));
  }
  [TestCase(float.NaN)][TestCase(float.PositiveInfinity)][TestCase(float.NegativeInfinity)]
  public void NonfiniteSourceTemperatureCannotManufactureDamage(float temperature)
  {source.GetPart<ThermalPart>().Temperature=temperature;int hp=actor.GetStatValue("Hitpoints");Pulse();Assert.AreEqual(hp,actor.GetStatValue("Hitpoints"));}
  [TestCase("0,0",11,10,true)][TestCase("0,0;0,1",11,10,true)]
  [TestCase("-4,0;-4,1",15,10,true)][TestCase("0,0",12,10,false)]
  public void ActualPhysicalContactCountsOnceAndRemoteAnchorIsNotTheBody(string cells,int x,int y,bool touches)
  {
   Assert.True(zone.RemoveEntity(actor));actor.AddPart(new SpatialFootprintPart{CellsRaw=cells});Assert.True(zone.AddEntity(actor,x,y));
   int hp=actor.GetStatValue("Hitpoints");Pulse();Assert.AreEqual(touches?2:0,hp-actor.GetStatValue("Hitpoints"));
  }
  [Test]public void SharingTheNonSolidHotSourceCellCannotEvadeScald()
  {Assert.True(zone.RemoveEntity(actor));Assert.True(zone.AddEntity(actor,10,10));int hp=actor.GetStatValue("Hitpoints");Pulse();Assert.AreEqual(2,hp-actor.GetStatValue("Hitpoints"));}
  [TestCase(false)][TestCase(true)]public void CreatureSourceIsAdvancedOnlyByItsExistingScheduler(bool creature)
  {
   if(creature)source.SetTag("Creature");int hp=actor.GetStatValue("Hitpoints");Pulse();Assert.AreEqual(creature?0:2,hp-actor.GetStatValue("Hitpoints"));
   if(creature){var turn=GameEvent.New("BeginTakeAction");turn.SetParameter("Zone",(object)zone);source.FireEventAndRelease(turn);Assert.AreEqual(2,hp-actor.GetStatValue("Hitpoints"));}
  }
  [TestCase(false)][TestCase(true)]public void LiveSteamReadoutDistinguishesHotContactFromCoolingOnly(bool hot)
  {
   source.GetPart<ThermalPart>().Temperature=hot?700:25;string line=EffectDescriber.Describe(steam).ToLowerInvariant();
   Assert.That(line,Does.Contain(hot?"scald":"cool"));Assert.That(line,Does.Contain("steam"));
  }
 }
}
