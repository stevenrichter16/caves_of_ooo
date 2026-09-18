using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine.Tilemaps;
using CavesOfOoo.Data;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public class RegionalGuidanceAdversarialTests
    {
        private HotbarSaveFixture scope;
        private OverworldZoneManager manager;
        private Zone zone;
        private Entity player,speaker;
        [SetUp] public void Setup()
        {
            scope=new HotbarSaveFixture(false,false);manager=OverworldZoneManager.CreateDetached(null,64);
            zone=new Zone("Overworld.13.7.0");manager.SetActiveZone(zone);SettlementRuntime.ActiveZone=zone;
            player=CinderholdCompositionTests.Player();speaker=CinderholdCompositionTests.Player();speaker.Tags.Remove("Player");
            speaker.AddPart(new ConversationPart{ConversationID="Scribe_1"});
            Assert.IsTrue(zone.AddEntity(player,10,10));Assert.IsTrue(zone.AddEntity(speaker,11,10));
            StoryletPart.Current=new StoryletPart();StoryletPart.LocalPlayer=player;
        }
        [TearDown] public void Teardown(){scope?.Dispose();}
        [Test] public void ActualMorrowfastClerkOffersNearbyDirectionsButAnImpostorCannot()
        {
            using(var content=new EntityEquipmentContentFixture())
            {
                zone.RemoveEntity(player); // Transfer the setup actor; AddEntity refuses cross-zone ownership.
                manager=OverworldZoneManager.CreateDetached(content.Factory,64);zone=manager.GetZone(MorrowfastSceneRuntime.ZoneID);SettlementRuntime.ActiveZone=zone;
                speaker=MorrowfastSceneRuntime.FindOwner(zone,"east-robed-resident");Assert.NotNull(speaker);
                var seat=zone.GetEntityCell(speaker);Cell landing=null;
                for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)
                {var c=zone.GetCell(seat.X+dx,seat.Y+dy);if(c!=null&&!c.BlocksMovement()&&(dx!=0||dy!=0))landing=c;}
                Assert.NotNull(landing);Assert.IsTrue(zone.AddEntity(player,landing.X,landing.Y));
                Assert.IsTrue(ConversationManager.StartConversation(speaker,player));
                int index=ConversationManager.VisibleChoices.ToList().FindIndex(c=>c.Target=="RegionOverview");
                Assert.GreaterOrEqual(index,0,"The actual nearby clerk must expose regional guidance through normal dialogue.");
                ConversationManager.SelectChoice(index);
                Assert.AreEqual(4,ConversationManager.VisibleChoices.Count(c=>c.Actions.Any(a=>a.Key==RegionalGuidance.ActionName)));
                var impostor=CinderholdCompositionTests.Player();impostor.Tags.Remove("Player");impostor.AddPart(new ConversationPart{ConversationID="Morrowfast_Vennit"});
                Cell impostorSeat=null;
                for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)
                {var c=zone.GetCell(seat.X+dx,seat.Y+dy);if(c!=null&&!c.BlocksMovement()&&c!=landing&&c!=seat)impostorSeat=c;}
                Assert.NotNull(impostorSeat);Assert.IsTrue(zone.AddEntity(impostor,impostorSeat.X,impostorSeat.Y));
                Assert.IsEmpty(RegionalGuidance.Build(zone,impostor,player));
                Assert.AreEqual(4,RegionalGuidance.Build(zone,speaker,player).Count);
            }
        }
        [Test] public void HostileContactCannotIssueDirectionsUnlessAuthoredToSpeakToHostiles()
        {
            speaker.SetTag("Faction","Villagers");PlayerReputation.Set("Villagers",-200);
            Assert.IsTrue(FactionManager.IsHostile(speaker,player));
            Assert.IsEmpty(RegionalGuidance.Build(zone,speaker,player));
            speaker.SetTag("SpeaksToHostiles");Assert.AreEqual(4,RegionalGuidance.Build(zone,speaker,player).Count);
        }
        [Test] public void DeathHandledPlayerWithPositiveHPStillCannotRecordNotes()
        {
            player.SetTag("_DeathHandled");Assert.IsEmpty(RegionalGuidance.Build(zone,speaker,player));
            player.Tags.Remove("_DeathHandled");Assert.AreEqual(4,RegionalGuidance.Build(zone,speaker,player).Count);
        }
        [Test] public void ReplacedSameAddressGraphCannotIssueNotesFromStaleOwners()
        {
            manager.SetActiveZone(new Zone(zone.ZoneID));
            Assert.IsEmpty(RegionalGuidance.Build(zone,speaker,player));
            manager.SetActiveZone(zone);Assert.AreEqual(4,RegionalGuidance.Build(zone,speaker,player).Count);
        }
        [Test] public void EntitySaveBodyRestoresNotesAndCompletedWorkIsNotAdvertisedAgain()
        {
            Assert.IsNull(RegionalGuidance.TryRemember(zone,speaker,player,"Overworld.14.9.0"));
            using(var stream=new MemoryStream())
            {
                SaveGraphSerializer.SaveEntityBody(player,new SaveWriter(stream));stream.Position=0;
                var loaded=new Entity();SaveGraphSerializer.LoadEntityBody(loaded,new SaveReader(stream,null));
                CollectionAssert.AreEqual(RegionalTravelNotes.Read(player),RegionalTravelNotes.Read(loaded));
                StoryletPart.Current.MarkQuestCompleted("HiddenShrine");
                Assert.IsFalse(RegionalTravelNotes.Read(loaded).Single().Contains("Local lead:"));
                StringAssert.Contains("Quillhold",RegionalTravelNotes.Read(loaded).Single());
            }
        }
        [Test] public void LongObjectiveActuallyRendersReturnContactBeyondFirstLine()
        {
            var go=new GameObject("objective-wrap-test");
            try
            {
                var map=go.AddComponent<Tilemap>();var ui=go.AddComponent<QuestLogUI>();ui.Tilemap=map;ui.Open();
                string text="Leave the village and follow the eastern road past the old hedges. Speak with the contact at the far well before gathering a permitted sample; avoid taking another person's supplies. Bring the sample back and RETURN TO MORROWFAST.";
                var entry=new QuestLogActiveEntry("TestTravel","outbound",0,0,
                    new[]{new QuestLogStageRow("outbound",QuestLogStageStatus.Current)},
                    new[]{new QuestLogObjectiveRow("travel",text,false,false)});
                typeof(QuestLogUI).GetField("_snapshot",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(ui,new QuestLogSnapshot(new[]{entry},null));
                typeof(QuestLogUI).GetMethod("Render",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ui,null);
                var glyphs=Enumerable.Range(32,95).ToDictionary(i=>CP437TilesetGenerator.GetTextTile((char)i),i=>(char)i);
                var rendered=new StringBuilder();
                for(int y=0;y<45;y++){for(int x=0;x<80;x++){var tile=map.GetTile(new Vector3Int(x,44-y,0));rendered.Append(tile!=null&&glyphs.TryGetValue((Tile)tile,out char c)?c:' ');}rendered.Append(' ');}
                StringAssert.Contains("MORROWFAST",rendered.ToString(),"The player must see the return destination, not just the first66characters.");
                StringAssert.Contains("[Tab]",rendered.ToString(),"Wrapping must not erase journal navigation.");
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
