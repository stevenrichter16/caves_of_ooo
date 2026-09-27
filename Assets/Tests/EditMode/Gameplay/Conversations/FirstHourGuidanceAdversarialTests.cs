using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public class FirstHourGuidanceAdversarialTests
    {
        [TestCase("dead-speaker")][TestCase("dead-listener")][TestCase("handled-speaker")][TestCase("handled-listener")]
        [TestCase("removed-speaker")][TestCase("removed-listener")][TestCase("foreign-graph")][TestCase("wrong-conversation")]
        [TestCase("speaker-hostile")][TestCase("listener-hostile")][TestCase("wrong-physics")][TestCase("carried-speaker")]
        public void StaleOrInvalidParticipantsCannotBorrowCurrentGladeAuthority(string kind)
        {
            using(var f=new FirstHourGuidanceFixture("Warden_1"))
            {
                f.Node("PassingThrough");StringAssert.Contains("Sill",f.Text);
                switch(kind)
                {
                    case "dead-speaker":f.Speaker.GetStat("Hitpoints").BaseValue=0;break;
                    case "dead-listener":f.Player.GetStat("Hitpoints").BaseValue=0;break;
                    case "handled-speaker":f.Speaker.Tags["_DeathHandled"]="true";break;
                    case "handled-listener":f.Player.Tags["_DeathHandled"]="true";break;
                    case "removed-speaker":f.Zone.RemoveEntity(f.Speaker);break;
                    case "removed-listener":f.Zone.RemoveEntity(f.Player);break;
                    case "foreign-graph":f.Manager.SetActiveZone(new Zone(f.Zone.ZoneID));break;
                    case "wrong-conversation":f.Speaker.GetPart<ConversationPart>().ConversationID="Villager_1";break;
                    case "speaker-hostile":f.Speaker.AddPart(new BrainPart());f.Speaker.GetPart<BrainPart>().PersonalEnemies.Add(f.Player);break;
                    case "listener-hostile":f.Player.AddPart(new BrainPart());f.Player.GetPart<BrainPart>().PersonalEnemies.Add(f.Speaker);break;
                    case "wrong-physics":f.Speaker.GetPart<PhysicsPart>().ParentEntity=f.Player;break;
                    case "carried-speaker":f.Speaker.GetPart<PhysicsPart>().InInventory=f.Player;break;
                }
                Assert.AreEqual(ConversationManager.CurrentNode.Text,f.Text);
            }
        }
        [TestCase("biome")][TestCase("poi")][TestCase("empty-destination")][TestCase("no-villages")]
        public void GladeAndDestinationMapChangesNeverLeaveCachedPromises(string change)
        {
            using(var f=new FirstHourGuidanceFixture("Warden_1"))
            {
                f.Node("PassingThrough");StringAssert.Contains("Sill",f.Text);
                if(change=="biome")f.Manager.WorldMap.Tiles[11,10]=BiomeType.Desert;
                if(change=="poi")f.Manager.WorldMap.SetPOI(11,10,new PointOfInterest(POIType.Village,"Foreign village"));
                if(change=="empty-destination"){f.Manager.SetActiveZone(new Zone(WorldMap.StartingZoneID));f.Manager.SetActiveZone(f.Zone);}
                if(change=="no-villages")for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++)f.Manager.WorldMap.SetPOI(x,y,null);
                if(change=="biome"||change=="poi")Assert.AreEqual(ConversationManager.CurrentNode.Text,f.Text);
                else {StringAssert.DoesNotContain("Sill",f.Text);if(change=="no-villages")StringAssert.Contains("no current directions",f.Text);}
            }
        }
        [TestCase("absent")][TestCase("wrong-fact")][TestCase("zero-value")][TestCase("wrong-part-owner")]
        [TestCase("wrong-physics-owner")][TestCase("carried")][TestCase("takeable")][TestCase("duplicate")]
        [TestCase("foreign")][TestCase("wrong-blueprint")][TestCase("solid")][TestCase("faction-filter")]
        public void InoperableOrAmbiguousMarkerDoesNotSupplyDirections(string kind)
        {
            using(var f=new FirstHourGuidanceFixture())
            {
                var marker=f.Marker(35,12);StringAssert.Contains("west",f.Text);
                switch(kind)
                {
                    case "absent":f.Zone.RemoveEntity(marker);break;
                    case "wrong-fact":marker.GetPart<QuestMarkerTriggerPart>().Fact="another_fact";break;
                    case "zero-value":marker.GetPart<QuestMarkerTriggerPart>().Value=0;break;
                    case "wrong-part-owner":marker.GetPart<QuestMarkerTriggerPart>().ParentEntity=f.Player;break;
                    case "wrong-physics-owner":marker.GetPart<PhysicsPart>().ParentEntity=f.Player;break;
                    case "carried":marker.GetPart<PhysicsPart>().InInventory=f.Player;break;
                    case "takeable":marker.GetPart<PhysicsPart>().Takeable=true;break;
                    case "duplicate":f.Marker(45,12);break;
                    case "foreign":f.Zone.RemoveEntity(marker);new Zone("Overworld.9.10.0").AddEntity(marker,35,12);break;
                    case "wrong-blueprint":marker.BlueprintName="Tree";break;
                    case "solid":marker.GetPart<PhysicsPart>().Solid=true;break;
                    case "faction-filter":f.Player.Tags["Faction"]="Villagers";marker.GetPart<QuestMarkerTriggerPart>().TriggerFaction=FactionManager.GetFaction(f.Player);break;
                }
                StringAssert.Contains("can't point",f.Text);Assert.AreEqual(0,NarrativeStatePart.Current.GetFact("bmo_stump_reached"));
            }
        }
        [Test] public void ATriggerIgnoringADifferentFactionStillGivesDirections()
        {
            using(var f=new FirstHourGuidanceFixture())
            {var marker=f.Marker(35,12);marker.GetPart<QuestMarkerTriggerPart>().TriggerFaction="DefinitelyAnotherFaction";StringAssert.Contains("west",f.Text);}
        }
        [TestCase("backlink")][TestCase("equipped")][TestCase("ground")][TestCase("empty-stack")]
        public void AStaleInventoryRowCannotStandInForAnActuallyCarriedBook(string kind)
        {
            using(var f=new FirstHourGuidanceFixture("RootBeerGuy_Quest"))
            {
                var book=f.Book();StringAssert.DoesNotContain("Find my witness-book",f.Text);
                if(kind=="backlink")book.GetPart<PhysicsPart>().InInventory=f.Speaker;
                if(kind=="equipped")book.GetPart<PhysicsPart>().Equipped=f.Player;
                if(kind=="ground")f.Zone.AddEntity(book,36,12);
                if(kind=="empty-stack")book.AddPart(new StackerPart{StackCount=0});
                StringAssert.Contains("Find my witness-book",f.Text);
            }
        }
        [TestCase("BMO_Quest")][TestCase("RootBeerGuy_Quest")]
        public void ReportStageAndCompletedProgressNeverRequestTheFinishedWorkAgain(string conversation)
        {
            using(var f=new FirstHourGuidanceFixture(conversation))
            {
                string quest=conversation=="BMO_Quest"?"BmoCartridge":"RootBeerGuyCase";
                StoryletPart.Current.StartQuest(new QuestState{QuestId=quest,CurrentStageIndex=1});
                StringAssert.DoesNotContain("Find my witness-book",f.Text);StringAssert.DoesNotContain("Drive off",f.Text);StringAssert.DoesNotContain("can't point",f.Text);
                StoryletPart.Current.MarkQuestCompleted(quest);
                StringAssert.Contains(conversation=="BMO_Quest"?"sings again":"Nothing else",f.Text);
            }
        }
        [TestCase("no-beacon")][TestCase("wrong-beacon")][TestCase("other-listener")][TestCase("wrong-map")]
        public void QuestContextCannotReadProgressOnBehalfOfForeignOwners(string kind)
        {
            using(var f=new FirstHourGuidanceFixture())
            {
                f.Marker(35,12);StringAssert.Contains("west",f.Text);
                if(kind=="no-beacon")f.Speaker.RemovePart(f.Speaker.GetPart<QuestBeaconPart>());
                if(kind=="wrong-beacon")f.Speaker.GetPart<QuestBeaconPart>().Quest="other";
                if(kind=="other-listener")StoryletPart.LocalPlayer=FirstHourGuidanceFixture.Actor("Player");
                if(kind=="wrong-map")f.Manager.WorldMap.SetPOI(10,10,null);
                Assert.AreEqual(ConversationManager.CurrentNode.Text,f.Text);
            }
        }
        [Test] public void ReadingAndRefreshingDoesNotAlterFactsRewardsNodesOrRng()
        {
            using(var f=new FirstHourGuidanceFixture())
            {
                f.Marker(35,12);var source=JsonUtility.ToJson(ConversationManager.CurrentConversation);var properties=f.Player.Properties.ToArray();
                #if UNITY_5_3_OR_NEWER
                var random=UnityEngine.Random.state;
#endif
                int count=f.Manager.CachedZoneCount;
                for(int i=0;i<12;i++){StringAssert.Contains("west",f.Text);ConversationManager.RefreshVisibleChoices();}
                Assert.AreEqual(source,JsonUtility.ToJson(ConversationManager.CurrentConversation));CollectionAssert.AreEqual(properties,f.Player.Properties.ToArray());
                #if UNITY_5_3_OR_NEWER
                Assert.AreEqual(random,UnityEngine.Random.state);
#endif
                Assert.AreEqual(count,f.Manager.CachedZoneCount);Assert.AreEqual(0,NarrativeStatePart.Current.GetFact("bmo_stump_reached"));
                Assert.False(StoryletPart.Current.IsQuestActive("BmoCartridge"));Assert.IsEmpty(f.Player.GetPart<InventoryPart>().Objects);
            }
        }
        [Test] public void SameCellMarkerIsTruthfulAndAcceptAfterPrevisitKeepsOriginalActions()
        {
            using(var f=new FirstHourGuidanceFixture())
            {
                f.Marker(40,12);StringAssert.Contains("right here",f.Text);
                NarrativeStatePart.Current.SetFact("bmo_stump_reached",1);f.Node("Start");
                var original=ConversationManager.CurrentNode.Choices.Single(c=>c.Target=="Accepted");var shown=ConversationManager.VisibleChoices.Single(c=>c.Target=="Accepted");
                StringAssert.Contains("found",shown.Text);Assert.AreSame(original.Actions,shown.Actions);Assert.AreSame(original.Predicates,shown.Predicates);Assert.AreEqual(original.Target,shown.Target);
            }
        }
        [Test] public void CurrentMarkerDirectionsSurviveExactGraphSaveLoadWithoutRenamingOrRegeneration()
        {
            using(var f=new FirstHourGuidanceFixture())
            {
                var marker=f.Marker(35,12);var world=new Entity{ID="World",BlueprintName="World"};world.AddPart(StoryletPart.Current);world.AddPart(NarrativeStatePart.Current);
                var turns=new TurnManager();var state=GameSessionState.Capture("m1-test","m1-test",f.Manager,turns,f.Player,0,world);
                var loaded=HotbarSaveFixture.RoundTrip(state);var zone=loaded.ZoneManager.ActiveZone;
                Assert.AreNotSame(f.Player,loaded.Player);ConversationManager.Listener=loaded.Player;StoryletPart.LocalPlayer=loaded.Player;
                ConversationManager.Speaker=zone.GetAllEntities().Single(e=>e.ID==f.Speaker.ID);
                StoryletPart.Current=loaded.World.GetPart<StoryletPart>();NarrativeStatePart.Current=loaded.World.GetPart<NarrativeStatePart>();
                StringAssert.Contains("west",ConversationManager.CurrentText);Assert.AreEqual(marker.ID,zone.GetAllEntities().Single(e=>e.BlueprintName=="OldStump").ID);
                Assert.AreEqual(f.Manager.CachedZoneCount,loaded.ZoneManager.CachedZoneCount);
            }
        }
    }
}
