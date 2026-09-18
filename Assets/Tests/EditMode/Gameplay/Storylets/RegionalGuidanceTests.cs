using System;
using System.Linq;
using CavesOfOoo.Data;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public class RegionalGuidanceTests
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
        [Test] public void FourNearestActualVillagesHaveTruthfulCoordinatesAndStableOrdering()
        {
            var before=manager.CachedZoneCount;
            var leads=RegionalGuidance.Build(zone,speaker,player);
            Assert.AreEqual(4,leads.Count);Assert.AreEqual(4,leads.Select(x=>x.DestinationZoneID).Distinct().Count());
            Assert.AreEqual(before,manager.CachedZoneCount,"Guidance must not generate destinations.");
            foreach(var lead in leads)
            {
                var at=WorldMap.FromZoneID(lead.DestinationZoneID);var poi=manager.WorldMap.GetPOI(at.x,at.y);
                Assert.AreEqual(POIType.Village,poi.Type);StringAssert.Contains(poi.Name,lead.Text);
                StringAssert.Contains(at.x+","+at.y,lead.Text);Assert.AreNotEqual(zone.ZoneID,lead.DestinationZoneID);
            }
            var expected=(from y in Enumerable.Range(0,20) from x in Enumerable.Range(0,20)
                let poi=manager.WorldMap.GetPOI(x,y)
                where poi?.Type==POIType.Village && (x!=13||y!=7)
                orderby Math.Abs(x-13)+Math.Abs(y-7),y,x
                select WorldMap.ToZoneID(x,y,0)).Take(4).ToArray();
            CollectionAssert.AreEqual(expected,leads.Select(x=>x.DestinationZoneID));
            CollectionAssert.AreEqual(leads.Select(x=>x.DestinationZoneID),RegionalGuidance.Build(zone,speaker,player).Select(x=>x.DestinationZoneID));
        }
        [Test] public void CompletedCanonicalWorkDoesNotRemainAnAvailableWorkPromise()
        {
            var first=RegionalGuidance.Build(zone,speaker,player).Single(x=>x.DestinationZoneID=="Overworld.14.9.0");
            Assert.AreEqual("HiddenShrine",first.QuestID);
            StoryletPart.Current.MarkQuestCompleted("HiddenShrine");
            Assert.IsTrue(string.IsNullOrEmpty(RegionalGuidance.Build(zone,speaker,player).Single(x=>x.DestinationZoneID==first.DestinationZoneID).QuestID));
        }
        [TestCase("wrong")][TestCase("dead")][TestCase("detached")]
        public void InvalidSpeakerCannotProvideOrRecordDirections(string kind)
        {
            if(kind=="wrong")speaker.GetPart<ConversationPart>().ConversationID="EncasedElder_1";
            if(kind=="dead")speaker.GetStat("Hitpoints").BaseValue=0;
            if(kind=="detached")zone.RemoveEntity(speaker);
            Assert.IsEmpty(RegionalGuidance.Build(zone,speaker,player));
            Assert.IsNotNull(RegionalGuidance.TryRemember(zone,speaker,player,"Overworld.14.9.0"));
            Assert.IsEmpty(RegionalTravelNotes.Read(player));
        }
        [Test] public void RemovedMapDestinationInvalidatesPreviouslyOfferedChoice()
        {
            var lead=RegionalGuidance.Build(zone,speaker,player)[0];var at=WorldMap.FromZoneID(lead.DestinationZoneID);
            manager.WorldMap.SetPOI(at.x,at.y,null);
            Assert.IsNotNull(RegionalGuidance.TryRemember(zone,speaker,player,lead.DestinationZoneID));
            Assert.IsEmpty(RegionalTravelNotes.Read(player));
            var valid=RegionalGuidance.Build(zone,speaker,player)[0];
            Assert.IsNull(RegionalGuidance.TryRemember(zone,speaker,player,valid.DestinationZoneID));
            Assert.AreEqual(1,RegionalTravelNotes.Read(player).Count);
        }
        [Test] public void AlreadyCachedEmptyVillageIsNotAdvertisedAsLivingWork()
        {
            manager.SetActiveZone(new Zone("Overworld.14.9.0"));manager.SetActiveZone(zone);
            Assert.IsFalse(RegionalGuidance.Build(zone,speaker,player).Any(x=>x.DestinationZoneID=="Overworld.14.9.0"));
            Assert.IsTrue(RegionalGuidance.Build(zone,speaker,player).Count>0,"Unvisited villages remain usable leads.");
        }
        [Test] public void SelectingSameLeadTwiceCreatesOnePersistentPlayerNote()
        {
            var lead=RegionalGuidance.Build(zone,speaker,player)[0];
            Assert.IsNull(RegionalGuidance.TryRemember(zone,speaker,player,lead.DestinationZoneID));
            var properties=player.Properties.ToDictionary(x=>x.Key,x=>x.Value);
            Assert.IsNull(RegionalGuidance.TryRemember(zone,speaker,player,lead.DestinationZoneID));
            Assert.AreEqual(1,RegionalTravelNotes.Read(player).Count);
            var restored=CinderholdCompositionTests.Player();foreach(var p in properties)restored.Properties[p.Key]=p.Value;
            CollectionAssert.AreEqual(RegionalTravelNotes.Read(player),RegionalTravelNotes.Read(restored),"Notes must reside in ordinary serialized properties, not a static session cache.");
        }
        [Test] public void JournalTabShowsNotesEvenWithNoQuestsAndReturnsWithoutClosing()
        {
            Assert.IsNull(RegionalGuidance.TryRemember(zone,speaker,player,RegionalGuidance.Build(zone,speaker,player)[0].DestinationZoneID));
            var go=new GameObject("guidance-journal-test");
            try
            {
                var ui=go.AddComponent<QuestLogUI>();ui.Open();
                ui.HandleInput(new Keys(KeyCode.Tab));Assert.IsTrue(ui.IsOpen);Assert.IsTrue(ui.NotesVisible);
                ui.HandleInput(new Keys(KeyCode.PageDown));Assert.AreEqual(0,ui.NotesPage,"A one-note page must clamp.");
                ui.HandleInput(new Keys(KeyCode.Tab));Assert.IsTrue(ui.IsOpen);Assert.IsFalse(ui.NotesVisible);
                ui.HandleInput(new Keys(KeyCode.Escape));Assert.IsFalse(ui.IsOpen);Assert.AreEqual(1,RegionalTravelNotes.Read(player).Count);
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        [TestCase("Scribe_1","RegionOverview")][TestCase("Innkeeper_1","Rumors")]
        public void RealConversationChoiceRouteRecordsDirectionsAndDoesNotLeakIntoOtherNodes(string conversation,string node)
        {
            speaker.GetPart<ConversationPart>().ConversationID=conversation;
            ConversationManager.Speaker=speaker;ConversationManager.Listener=player;
            ConversationManager.CurrentConversation=new ConversationData{ID=conversation};
            ConversationManager.CurrentNode=new NodeData{ID=node};
            ConversationManager.RefreshVisibleChoices();
            var choices=ConversationManager.VisibleChoices.Where(c=>c.Actions.Any(a=>a.Key=="RememberRegionalDirection")).ToArray();
            Assert.AreEqual(4,choices.Length);
            var selected=choices[0];int index=ConversationManager.VisibleChoices.ToList().IndexOf(selected);
            ConversationManager.SelectChoice(index);
            Assert.AreEqual(1,RegionalTravelNotes.Read(player).Count);
            ConversationManager.CurrentNode=new NodeData{ID="Explain"};ConversationManager.RefreshVisibleChoices();
            Assert.IsFalse(ConversationManager.VisibleChoices.Any(c=>c.Actions.Any(a=>a.Key=="RememberRegionalDirection")));
        }
        private sealed class Keys:IInputProbe
        {private readonly KeyCode key;public Keys(KeyCode key){this.key=key;}public bool GetKeyDown(KeyCode candidate)=>candidate==key;}
    }
}
