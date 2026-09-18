using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class CanonicalVillageQuestAdversarialTests
    {
        private HotbarSaveFixture scope;
        private Dictionary<string,LootTableData> oldLoot;
        private bool initialized;
        private static readonly FieldInfo Tables=typeof(LootTableRegistry).GetField("_byName",BindingFlags.Static|BindingFlags.NonPublic);
        private static readonly FieldInfo Initialized=typeof(LootTableRegistry).GetField("_initialized",BindingFlags.Static|BindingFlags.NonPublic);
        [SetUp] public void Setup()
        {
            oldLoot=new Dictionary<string,LootTableData>((Dictionary<string,LootTableData>)Tables.GetValue(null));initialized=LootTableRegistry.IsInitialized;
            scope=new HotbarSaveFixture(false,false);CinderholdCompositionTests.LoadLoot();
        }
        [TearDown] public void Teardown()
        {
            try {var t=(Dictionary<string,LootTableData>)Tables.GetValue(null);t.Clear();foreach(var p in oldLoot)t.Add(p.Key,p.Value);Initialized.SetValue(null,initialized);}
            finally {scope?.Dispose();}
        }

        [TestCase("Overworld.13.7.00")][TestCase("overworld.13.7.0")]
        [TestCase("Overworld.13.7.0.extra")][TestCase(" Overworld.13.7.0")]
        [TestCase("Overworld.13.7.-1")][TestCase("WorldMap")]
        public void MalformedOrForeignAddressCannotClaimAStory(string id)
        { Assert.IsNull(VillagePopulationBuilder.PickVillageQuest(id));Assert.AreEqual("CrunchyLocket",VillagePopulationBuilder.PickVillageQuest("Overworld.13.7.0")); }

        [Test] public void NonhostPopulationDoesNotCreateFallbackShrineOrQuestCargo()
        {
            var factory=GrovelandsCompositionTests.Factory();
            var poi=new PointOfInterest(POIType.Village,"Counter town","Villagers");
            foreach(string id in new[]{"Overworld.11.10.0","Overworld.18.18.0"})
            {
                var zone=new Zone(id);Assert.IsTrue(new VillageBuilder(BiomeType.Spread,poi).BuildZone(zone,factory,new Random(64)));
                Assert.IsTrue(new VillagePopulationBuilder(poi).BuildZone(zone,factory,new Random(64)));
                Assert.IsFalse(zone.GetAllEntities().Any(e=>e.HasPart<QuestBeaconPart>()),id);
                Assert.IsFalse(zone.GetAllEntities().Any(e=>e.BlueprintName=="HiddenShrineMarker"||e.BlueprintName=="CrunchyLocket"||e.HasPart<AddFactWhenSlain>()),id);
                Assert.IsTrue(zone.GetAllEntities().Any(e=>e.BlueprintName=="Merchant"),"Services remain available.");
            }
        }
        [TestCase(false)][TestCase(true)]
        public void WellmeetWarrenModelsRequireActualAuthoredQuestContracts(bool giver)
        {
            var manager=OverworldZoneManager.CreateDetached(GrovelandsCompositionTests.Factory(),64);
            var zone=manager.GetZone("Overworld.8.16.0");
            var owner=giver?zone.GetAllEntities().Single(e=>e.GetPart<QuestBeaconPart>()?.Quest=="ClearTheWarren")
                :zone.GetAllEntities().First(e=>e.GetPart<AddFactWhenSlain>()?.Fact=="warren_gnomes_routed");
            var library=UnityEngine.Resources.Load<CavesOfOoo.Rendering.SpawnRing3DLibrary>(CavesOfOoo.Rendering.SpawnRing3DLibrary.ResourcePath);
            var recipe=CavesOfOoo.Rendering.SpawnRing3DRecipes.Resolve(zone,owner,library.Definition);
            Assert.NotNull(recipe.ModelId,recipe.Failure);Assert.NotNull(library.FindModel(recipe.ModelId));
            Assert.AreSame(owner,recipe.Owner);Assert.IsTrue(recipe.Transient);Assert.IsFalse(recipe.Batched);
            if(giver)owner.GetPart<ConversationPart>().ConversationID="Scribe_1";
            else owner.GetPart<AddFactWhenSlain>().Fact="unrelated_fact";
            var rejected=CavesOfOoo.Rendering.SpawnRing3DRecipes.Resolve(zone,owner,library.Definition);
            Assert.IsNull(rejected.ModelId);Assert.AreEqual("reskinned-native-actor",rejected.Failure);
            if(giver)owner.GetPart<ConversationPart>().ConversationID="Warren_Quest";
            else owner.GetPart<AddFactWhenSlain>().Fact="warren_gnomes_routed";
            Assert.AreEqual(recipe.ModelId,CavesOfOoo.Rendering.SpawnRing3DRecipes.Resolve(zone,owner,library.Definition).ModelId);
        }

        [TestCase(false)][TestCase(true)]
        public void NewHostGenerationAndSaveRoundTripPreserveGlobalProgress(bool completed)
        {
            var state=new StoryletPart();StoryletPart.Current=state;
            state.StartQuest(new QuestState{QuestId="CrunchyLocket",CurrentStageIndex=1,EnteredStageAtTurn=123});
            if(completed)state.MarkQuestCompleted("CrunchyLocket");
            var manager=OverworldZoneManager.CreateDetached(GrovelandsCompositionTests.Factory(),64);
            // Visit a different canonical host first, then this quest's host.
            manager.GetZone("Overworld.7.8.0");var host=manager.GetZone("Overworld.13.7.0");
            Assert.AreSame(host,manager.GetZone("Overworld.13.7.0"));
            Assert.AreEqual(1,host.GetAllEntities().Count(e=>e.GetPart<QuestBeaconPart>()?.Quest=="CrunchyLocket"));
            Assert.AreEqual(completed,state.IsQuestCompleted("CrunchyLocket"));
            if(!completed){Assert.AreEqual(1,state.GetQuestState("CrunchyLocket").CurrentStageIndex);Assert.AreEqual(123,state.GetQuestState("CrunchyLocket").EnteredStageAtTurn);}
            using(var stream=new MemoryStream())
            {
                state.Save(new SaveWriter(stream));stream.Position=0;var loaded=new StoryletPart();loaded.Load(new SaveReader(stream,null));StoryletPart.Current=loaded;
                Assert.AreEqual(completed,loaded.IsQuestCompleted("CrunchyLocket"));
                Assert.IsFalse(ConversationPredicates.Evaluate("IfQuestNotStarted",null,null,"CrunchyLocket"));
                Assert.IsTrue(ConversationPredicates.Evaluate("IfQuestNotStarted",null,null,"HiddenShrine"),"Another unique story remains available.");
                ConversationActions.Execute("StartQuest",null,null,"CrunchyLocket");
                Assert.AreEqual(completed,loaded.IsQuestCompleted("CrunchyLocket"));
                if(!completed)Assert.AreEqual(1,loaded.GetQuestState("CrunchyLocket").CurrentStageIndex,"Offer action must not reset progress.");
                else Assert.IsFalse(loaded.IsQuestActive("CrunchyLocket"));
            }
        }
    }
}
