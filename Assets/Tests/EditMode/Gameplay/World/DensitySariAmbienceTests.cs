using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class DensitySariAmbienceTests
 {
  Entity player; Zone zone;
  [SetUp] public void Setup(){player=new Entity{ID="sari-test-player"};player.SetTag("Player");player.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=40,Max=40};MessageLog.Clear();Place(3);}
  void Place(int tier,int depth=0){if(zone?.GetEntityCell(player)!=null)zone.RemoveEntity(player);for(int x=0;x<WorldMap.Width;x++)for(int y=0;y<WorldMap.Height;y++)if(WorldMapAuthoring.TierAt(x,y)==tier){zone=new Zone(WorldMap.ToZoneID(x,y,depth));Assert.True(zone.AddEntity(player,10,10));return;}Assert.Fail("No authored tier "+tier);}
  int Heard()=>MessageLog.GetMessages().Count(s=>s.IndexOf("sari",StringComparison.OrdinalIgnoreCase)>=0);
  [TestCase(1)][TestCase(2)]public void LowTiersStaySilentAndDoNotTouchSillFact(int tier){Place(tier);for(int i=0;i<2000;i++)WorldAmbience.OnPlayerTurnEnded(player,zone);Assert.Zero(Heard());Assert.Zero(player.GetIntProperty("sill_sari_heard"));}
  [TestCase(3)][TestCase(4)][TestCase(5)]public void AuthoredHighTiersEventuallySound(int tier){Place(tier);for(int i=0;i<1000;i++)WorldAmbience.OnPlayerTurnEnded(player,zone);Assert.Greater(Heard(),0);Assert.AreEqual(tier,SariAmbience.TierFor(zone));}
  [TestCase(1,2)][TestCase(2,3)][TestCase(4,5)][TestCase(5,5)]public void UndergroundAddsOnlyOneCanonTier(int surface,int expected){Place(surface,5);Assert.AreEqual(expected,SariAmbience.TierFor(zone));}
  [TestCase("cave.3")][TestCase("Overworld.bad.4.0")][TestCase("Overworld.20.1.0")][TestCase("Overworld.1.1.-1")][TestCase("Overworld")]
  public void UnknownMalformedOrMapLocationsStayQuiet(string id){zone.ZoneID=id;for(int i=0;i<30;i++)WorldAmbience.OnPlayerTurnEnded(player,zone);Assert.Zero(Heard());Assert.Zero(SariAmbience.TierFor(zone));}
  [Test]public void TierFiveArrivalIsImmediateAndDoesNotReplayOnReentry(){Place(5);Assert.True(SariAmbience.OnZoneEntered(player,zone));Assert.AreEqual(1,Heard());Assert.False(SariAmbience.OnZoneEntered(player,zone));WorldAmbience.OnPlayerTurnEnded(player,zone);Assert.AreEqual(1,Heard());}
  [TestCase(EndingSpine.VesselPath)][TestCase(EndingSpine.GatheredPath)]public void SilencingEndingsBlockArrivalAndPeriodic(int ending){Place(5);player.SetIntProperty(EndingSpine.EndingProperty,ending);Assert.False(SariAmbience.OnZoneEntered(player,zone));for(int i=0;i<500;i++)WorldAmbience.OnPlayerTurnEnded(player,zone);Assert.Zero(Heard());}
  [TestCase(EndingSpine.PracticePath,true)][TestCase(EndingSpine.KeptPath,false)]public void ContinuingEndingsUseTheRightPitch(int ending,bool changed){Place(5);player.SetIntProperty(EndingSpine.EndingProperty,ending);Assert.True(SariAmbience.OnZoneEntered(player,zone));Assert.AreEqual(changed,MessageLog.GetLast().Contains("pitch has changed"));}
  [Test]public void PeriodicMessagesRespectSharedTwentyActionSpacing(){int last=-100;for(int i=1;i<=1500;i++){int before=Heard();WorldAmbience.OnPlayerTurnEnded(player,zone);if(Heard()>before){Assert.GreaterOrEqual(i-last,20);last=i;}}Assert.Greater(last,0);}
  [TestCase("dead")][TestCase("death-handled")][TestCase("npc")][TestCase("removed")][TestCase("foreign")]
  public void InvalidActorsNeitherSoundNorAdvanceCosmeticState(string state){Place(5);if(state=="dead")player.GetStat("Hitpoints").BaseValue=0;if(state=="death-handled")player.SetTag("_DeathHandled");if(state=="npc")player.Tags.Remove("Player");if(state=="removed")zone.RemoveEntity(player);if(state=="foreign")zone=new Zone(zone.ZoneID);Assert.False(SariAmbience.OnZoneEntered(player,zone));WorldAmbience.OnPlayerTurnEnded(player,zone);Assert.Zero(Heard());Assert.Zero(player.GetIntProperty(WorldAmbience.TurnProperty));}
  [Test]public void RealSchedulerCallsAmbienceAfterPlayerTurn(){Place(5);var old=TurnManager.Active;var oldWorld=TurnManager.World;try{TurnManager.World=null;var turns=new TurnManager();turns.AddEntity(player);turns.EndTurn(player,zone);Assert.AreEqual(1,Heard());Assert.AreEqual(1,player.GetIntProperty(WorldAmbience.TurnProperty));}finally{TurnManager.World=oldWorld;typeof(TurnManager).GetProperty("Active").SetValue(null,old);}}
 }
}
