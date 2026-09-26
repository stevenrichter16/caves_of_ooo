using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class DensityTravellerIntegrationContractTests
 {
  DensityLootTestScope scope;OverworldZoneManager manager;Entity player;Zone zone;string readyId;
  [SetUp]public void Setup(){scope=new DensityLootTestScope();FactionManager.Initialize();manager=OverworldZoneManager.CreateDetached(scope.Factory,718);player=scope.Factory.CreateEntity("Player");readyId=FindWinningZone();ClearMemory();zone.RemoveEntity(zone.GetReadOnlyEntities().Single(e=>e.GetProperty(WorldTravellers.OriginProperty)!=null));}
  [TearDown]public void Cleanup(){scope.Dispose();FactionManager.Reset();}
  void ClearMemory(){foreach(var k in player.IntProperties.Keys.Where(k=>k.StartsWith("Traveller")).ToArray())player.IntProperties.Remove(k);foreach(var k in player.Properties.Keys.Where(k=>k.StartsWith("Traveller")).ToArray())player.Properties.Remove(k);}
  void Attach(Zone z){if(zone?.GetEntityCell(player)!=null)zone.RemoveEntity(player);zone=z;Assert.True(zone.AddEntity(player,10,10));manager.ReplaceLoadedState(new Dictionary<string,Zone>{{zone.ZoneID,zone}},zone.ZoneID,new Dictionary<string,List<ZoneConnection>>());}
  string FindWinningZone(){for(int x=0;x<WorldMap.Width;x++)for(int y=0;y<WorldMap.Height;y++){if(WorldMapAuthoring.TierAt(x,y)>3||manager.WorldMap.GetPOI(x,y)!=null)continue;Attach(new Zone(WorldMap.ToZoneID(x,y,0)));if(WorldTravellers.OnZoneEntered(player,zone))return zone.ZoneID;}Assert.Fail("At least one eligible zone must win the bounded entry roll.");return null;}
  Entity Spawn(){Assert.True(WorldTravellers.OnZoneEntered(player,zone));return zone.GetReadOnlyEntities().Single(e=>e.GetProperty(WorldTravellers.OriginProperty)!=null);}

  [Test]public void EntryHookPrecedesCreatureRegistrationAndAutosave(){string text=System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath,"Scripts/Presentation/Input/InputHandler.cs"));int start=text.IndexOf("private void HandleZoneTransition(",StringComparison.Ordinal);int hook=text.IndexOf("WorldTravellers.OnZoneEntered(PlayerEntity, result.NewZone)",start,StringComparison.Ordinal);int register=text.IndexOf("var newCreatures =",start,StringComparison.Ordinal);int save=text.IndexOf("SaveGameService.QuickSave()",start,StringComparison.Ordinal);Assert.Greater(hook,start,"entry service must be integrated into actual successful transfer");Assert.Less(hook,register);Assert.Less(hook,save);}
  [TestCase("Start")][TestCase("Sources")]
  public void ActualConversationManagerDecoratesCurrentTextWithoutMutatingSharedNode(string node){var m=Spawn();ConversationManager.Speaker=m;ConversationManager.Listener=player;ConversationManager.CurrentConversation=new CavesOfOoo.Data.ConversationData{ID="Merchant_1"};ConversationManager.CurrentNode=new CavesOfOoo.Data.NodeData{ID=node,Text="authored fallback"};Assert.AreNotEqual("authored fallback",ConversationManager.CurrentText);Assert.AreEqual("authored fallback",ConversationManager.CurrentNode.Text);}
  [Test]public void OrdinaryMerchantStillUsesItsAuthoredNode(){var m=scope.Factory.CreateEntity("Merchant");zone.AddEntity(m,11,10);ConversationManager.Speaker=m;ConversationManager.Listener=player;ConversationManager.CurrentConversation=new CavesOfOoo.Data.ConversationData{ID="Merchant_1"};ConversationManager.CurrentNode=new CavesOfOoo.Data.NodeData{ID="Start",Text="authored fallback"};Assert.AreEqual("authored fallback",ConversationManager.CurrentText);}
 }
}
