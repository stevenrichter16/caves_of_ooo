using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    internal sealed class FirstHourGuidanceFixture : IDisposable
    {
        readonly HotbarSaveFixture scope = new HotbarSaveFixture(false, false);
        readonly StoryletPart oldStory = StoryletPart.Current;
        readonly Entity oldPlayer = StoryletPart.LocalPlayer;
        readonly NarrativeStatePart oldFacts = NarrativeStatePart.Current;
        public readonly EntityFactory Factory = new EntityFactory();
        public readonly OverworldZoneManager Manager;
        public Zone Zone;
        public Entity Player, Speaker;
        public FirstHourGuidanceFixture(string conversation = "BMO_Quest")
        {
            Factory.LoadBlueprints(Resources.Load<TextAsset>("Content/Blueprints/Objects").text);
            Manager = OverworldZoneManager.CreateDetached(Factory, 64);
            Zone = new Zone(conversation == "Warden_1" ? ReferenceGladePlan.ZoneID : WorldMap.StartingZoneID);
            Manager.SetActiveZone(Zone);
            Player = Actor("Player"); Player.Tags["Player"] = "true"; Player.AddPart(new InventoryPart());
            Speaker = Actor(conversation == "Warden_1" ? "Warden" : "Villager");
            Speaker.AddPart(new ConversationPart { ConversationID = conversation });
            if (conversation != "Warden_1") Speaker.AddPart(new QuestBeaconPart { Quest = conversation == "BMO_Quest" ? "BmoCartridge" : "RootBeerGuyCase" });
            Assert.True(Zone.AddEntity(Player, 39, 12)); Assert.True(Zone.AddEntity(Speaker, 40, 12));
            StoryletPart.Current = new StoryletPart(); StoryletPart.LocalPlayer = Player; NarrativeStatePart.Current = new NarrativeStatePart();
            var file = conversation == "Warden_1" ? "Wardens" : conversation;
            ConversationManager.CurrentConversation = JsonUtility.FromJson<ConversationFileData>(Resources.Load<TextAsset>("Content/Conversations/" + file).text).Conversations.Single(c => c.ID == conversation);
            ConversationManager.Speaker = Speaker; ConversationManager.Listener = Player; Node("Start");
        }
        public static Entity Actor(string blueprint)
        {
            var e = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = blueprint };
            e.AddPart(new PhysicsPart()); e.AddPart(new RenderPart { DisplayName = blueprint });
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 40, Max = 40, Owner = e }; return e;
        }
        public void Node(string id) { ConversationManager.CurrentNode = ConversationManager.CurrentConversation.GetNode(id); Assert.NotNull(ConversationManager.CurrentNode); ConversationManager.RefreshVisibleChoices(); }
        public Entity Marker(int x, int y)
        {
            var e = new Entity { ID = "OldStump", BlueprintName = "OldStump" };
            e.AddPart(new PhysicsPart()); e.AddPart(new QuestMarkerTriggerPart { Fact = "bmo_stump_reached", Value = 1 });
            Assert.True(Zone.AddEntity(e, x, y)); return e;
        }
        public Entity Book()
        {
            var item = new Entity { ID = "DetectiveNotebook", BlueprintName = "DetectiveNotebook" };
            item.AddPart(new PhysicsPart { Takeable = true }); Player.GetPart<InventoryPart>().AddObject(item); return item;
        }
        public string Text => ConversationManager.CurrentText;
        public void Dispose() { ConversationManager.EndConversation(); scope.Dispose(); StoryletPart.Current = oldStory; StoryletPart.LocalPlayer = oldPlayer; NarrativeStatePart.Current = oldFacts; }
    }

    public class FirstHourGuidanceTests
    {
        [TestCase("Start")][TestCase("PassingThrough")][TestCase("AboutVillage")][TestCase("Threats")][TestCase("OfferHelp")][TestCase("Creatures")]
        public void GladeWardenDoesNotInventVillageServicesOrRewards(string node)
        {
            using (var f = new FirstHourGuidanceFixture("Warden_1"))
            {
                f.Node(node); var authored = ConversationManager.CurrentNode.Text;
                Assert.AreNotEqual(authored, f.Text); StringAssert.DoesNotContain("elder", f.Text.ToLowerInvariant());
                StringAssert.DoesNotContain("merchant", f.Text.ToLowerInvariant()); StringAssert.DoesNotContain("reward", f.Text.ToLowerInvariant());
                Assert.AreEqual(authored, ConversationManager.CurrentNode.Text);
            }
        }
        [Test] public void GladeLeadUsesActualMapWithoutGeneratingOrRecordingNotes()
        {
            using (var f = new FirstHourGuidanceFixture("Warden_1"))
            {
                f.Node("PassingThrough"); int count = f.Manager.CachedZoneCount;
                StringAssert.Contains("Sill", f.Text); StringAssert.Contains("west", f.Text);
                Assert.AreEqual(count, f.Manager.CachedZoneCount); Assert.IsEmpty(RegionalTravelNotes.Read(f.Player));
                f.Manager.WorldMap.GetPOI(10, 10).Name = "Changed village";
                StringAssert.Contains("Changed village", f.Text); StringAssert.DoesNotContain("Sill", f.Text);
            }
        }
        [Test] public void GladeChoiceLabelsAreContextualCopiesAndVillageWardensKeepAuthoredChoices()
        {
            using (var f = new FirstHourGuidanceFixture("Warden_1"))
            {
                var original = ConversationManager.CurrentNode.Choices.Single(c => c.Target == "AboutVillage");
                var offered = ConversationManager.VisibleChoices.Single(c => c.Target == "AboutVillage");
                Assert.AreNotSame(original, offered); StringAssert.DoesNotContain("village", offered.Text);
                Assert.AreEqual("Tell me about this village.", original.Text);
                var village = new Zone(WorldMap.StartingZoneID); f.Zone.RemoveEntity(f.Player); f.Zone.RemoveEntity(f.Speaker);
                village.AddEntity(f.Player,39,12); village.AddEntity(f.Speaker,40,12); f.Manager.SetActiveZone(village); f.Node("Start");
                Assert.AreEqual(ConversationManager.CurrentNode.Text, f.Text);
                Assert.AreSame(original, ConversationManager.VisibleChoices.Single(c => c.Target == "AboutVillage"));
            }
        }
        [TestCase(45,12,"east")][TestCase(35,12,"west")][TestCase(40,7,"north")][TestCase(40,17,"south")]
        [TestCase(45,7,"northeast")][TestCase(35,7,"northwest")][TestCase(45,17,"southeast")][TestCase(35,17,"southwest")]
        public void EllunPointsFromCurrentSpeakerToActualMarker(int x, int y, string direction)
        {
            using (var f = new FirstHourGuidanceFixture())
            {
                f.Marker(x,y);
                foreach (var node in new[] { "Start", "Accepted", "Looking" }) { f.Node(node); StringAssert.Contains(direction, f.Text); StringAssert.DoesNotContain("past the buildings", f.Text); }
            }
        }
        [Test] public void EllunTracksCurrentMovedMarkerAndDoesNotCacheDirection()
        {
            using (var f = new FirstHourGuidanceFixture())
            {
                var stump=f.Marker(45,12); StringAssert.Contains("east",f.Text);
                f.Zone.RemoveEntity(stump); f.Zone.AddEntity(stump,35,12); StringAssert.Contains("west",f.Text);
                f.Zone.RemoveEntity(stump); StringAssert.Contains("can't point",f.Text);
            }
        }
        [Test] public void EllunPreacceptanceFactDoesNotAskPlayerToVisitAgain()
        {
            using (var f = new FirstHourGuidanceFixture())
            { f.Marker(35,12); NarrativeStatePart.Current.SetFact("bmo_stump_reached",1); StringAssert.Contains("found",f.Text); StringAssert.DoesNotContain("west",f.Text); Assert.False(StoryletPart.Current.IsQuestActive("BmoCartridge")); }
        }
        [TestCase(false,false)][TestCase(true,false)][TestCase(false,true)][TestCase(true,true)]
        public void HallunNamesOnlyRemainingObjectivesIncludingPreacceptanceProgress(bool book, bool gremlin)
        {
            using (var f = new FirstHourGuidanceFixture("RootBeerGuy_Quest"))
            {
                if(book)f.Book(); if(gremlin)NarrativeStatePart.Current.SetFact("rbg_gremlin_routed",1);
                foreach(var node in new[]{"Start","Accepted","Searching"})
                {
                    f.Node(node);
                    Assert.AreEqual(!book,f.Text.Contains("Find my witness-book"));
                    Assert.AreEqual(!gremlin,f.Text.Contains("Drive off the soot gremlin"));
                    if(book&&gremlin)StringAssert.Contains("both accounted for",f.Text);
                }
                Assert.False(StoryletPart.Current.IsQuestActive("RootBeerGuyCase"));
            }
        }
        [Test] public void HallunStickyObjectiveRemainsFinishedEvenWhenBookIsNoLongerCarried()
        {
            using(var f=new FirstHourGuidanceFixture("RootBeerGuy_Quest"))
            {
                var state=new QuestState{QuestId="RootBeerGuyCase"};state.FinishedObjectives.Add("find_notebook");StoryletPart.Current.StartQuest(state);
                f.Node("Searching");StringAssert.DoesNotContain("Find my witness-book",f.Text);StringAssert.Contains("Drive off the soot gremlin",f.Text);
            }
        }
        [TestCase("BMO_Quest",120,40)][TestCase("RootBeerGuy_Quest",150,60)]
        public void AuthoredQuestRewardsAndReportGateRemainUnchanged(string conversation,int xp,int drams)
        {
            using(var f=new FirstHourGuidanceFixture(conversation))
            {
                var choice=ConversationManager.CurrentNode.Choices.Single(c=>c.Actions?.Any(a=>a.Key=="CompleteQuest")==true);
                Assert.AreEqual(xp.ToString(),choice.GetAction("AwardXP"));Assert.AreEqual(drams.ToString(),choice.GetAction("GiveDrams"));
                Assert.That(choice.GetPredicate("IfQuestStage"),Does.EndWith(":report"));
                Assert.False(ConversationManager.VisibleChoices.Contains(choice));
            }
        }
    }
}
