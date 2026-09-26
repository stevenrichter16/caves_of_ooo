using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class DensityWorldRemarksAdversarialTests
 {
  Entity player,speaker,chair;Zone zone;int ready;
  [SetUp]public void Setup(){FactionManager.Initialize();MessageLog.Clear();zone=new Zone("Overworld.2.2.0");player=Person("Player","you",10);player.SetTag("Player");speaker=Person("Scribe","Iven, scribe",12);chair=new Entity{BlueprintName="Chair"};chair.AddPart(new ChairPart{Occupied=true});zone.AddEntity(chair,12,10);speaker.AddPart(new StatusEffectsPart());Assert.True(speaker.ApplyEffect(new SittingEffect(chair)));ready=FindReady();MessageLog.Clear();player.IntProperties[WorldAmbience.HasMessageProperty]=0;player.IntProperties[WorldAmbience.TurnProperty]=ready;}
  [TearDown]public void Cleanup(){FactionManager.Reset();MessageLog.Clear();}
  Entity Person(string id,string name,int x){var e=new Entity{ID=id,BlueprintName=id};e.SetTag("Creature");e.Tags["Faction"]=id=="Player"?"Player":"Villagers";e.AddPart(new RenderPart{DisplayName=name});e.AddPart(new BrainPart());e.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=20,Max=20};Assert.True(zone.AddEntity(e,x,10));zone.GetCell(x,10).IsVisible=true;return e;}
  int FindReady(){for(int i=0;i<2000;i++){player.IntProperties[WorldAmbience.TurnProperty]=i;if(WorldRemarks.TryEmit(player,zone))return i;}Assert.Fail("The actual seated scribe must have a passing cosmetic roll.");return 0;}

  [TestCase("dead")][TestCase("unoccupied")][TestCase("foreign")][TestCase("fake")]
  public void AStaleSittingEffectCannotInventRestingFurniture(string mode){if(mode=="dead")chair.SetTag("_DeathHandled");if(mode=="unoccupied")chair.GetPart<ChairPart>().Occupied=false;if(mode=="foreign"){zone.RemoveEntity(chair);new Zone("other").AddEntity(chair,12,10);}if(mode=="fake")chair.RemovePart(chair.GetPart<ChairPart>());Assert.False(WorldRemarks.TryEmit(player,zone));}
  [TestCase("Innkeeper")][TestCase("Farmer")]
  public void DestroyedContextCannotAdvertiseAService(string role){speaker.BlueprintName=role;var item=new Entity();if(role=="Innkeeper")item.AddPart(new CampfirePart());else item.AddPart(new WellSitePart());item.SetTag("_DeathHandled");zone.AddEntity(item,12,11);Assert.False(WorldRemarks.TryEmit(player,zone));}
  [TestCase("visible-body",true)][TestCase("hidden-body",false)]
  public void SpeakerVisibilityUsesThePhysicalBodyRatherThanEmptyAnchor(string mode,bool expected){zone.RemoveEntity(speaker);speaker.AddPart(new SpatialFootprintPart{CellsRaw="2,0"});zone.AddEntity(speaker,10,10);zone.GetCell(10,10).IsVisible=mode!="visible-body";zone.GetCell(12,10).IsVisible=mode=="visible-body";Assert.AreEqual(expected,WorldRemarks.TryEmit(player,zone));}
  [Test]public void APlayerBodyNearTheSpeakerCanHearDespiteADistantSaveAnchor(){zone.RemoveEntity(player);player.AddPart(new SpatialFootprintPart{CellsRaw="0,0;10,0"});zone.AddEntity(player,0,10);Assert.True(WorldRemarks.TryEmit(player,zone));}
  [Test]public void NoRemarkChangesGameplayRandomStateOrFactEvents(){var events=new Counter();player.AddPart(events);var oldL=LoadoutPart.Rng;var oldT=TraderPart.Rng;try{var rng=new ForbiddenRandom();LoadoutPart.Rng=TraderPart.Rng=rng;Assert.True(WorldRemarks.TryEmit(player,zone));Assert.AreSame(rng,LoadoutPart.Rng);Assert.AreEqual(0,events.Count);}finally{LoadoutPart.Rng=oldL;TraderPart.Rng=oldT;}}
  sealed class Counter:Part{public int Count;public override bool HandleEvent(GameEvent e){if(e.ID=="IntPropertyChanged")Count++;return true;}}
  sealed class ForbiddenRandom:Random{public override int Next(int maxValue)=>throw new Exception("gameplay rng consumed");public override int Next(int minValue,int maxValue)=>throw new Exception("gameplay rng consumed");}
 }
}
