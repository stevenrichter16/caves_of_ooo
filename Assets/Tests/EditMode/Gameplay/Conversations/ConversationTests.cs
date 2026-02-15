using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    public class ConversationTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            ConversationLoader.Reset();
            ConversationPredicates.Reset();
            ConversationActions.Reset();
            ConversationManager.EndConversation();
            MessageLog.Clear();
        }

        // ========================
        // Helpers
        // ========================

        private Entity CreateNPC(string conversationID = "TestConv", string faction = "Villagers")
        {
            var entity = new Entity { BlueprintName = "TestNPC" };
            entity.Tags["Creature"] = "";
            if (!string.IsNullOrEmpty(faction))
                entity.Tags["Faction"] = faction;
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = "Test NPC", RenderString = "@", ColorString = "&M" });
            entity.AddPart(new BrainPart());
            var convPart = new ConversationPart { ConversationID = conversationID };
            entity.AddPart(convPart);
            return entity;
        }

        private Entity CreatePlayer()
        {
            var entity = new Entity { BlueprintName = "Player" };
            entity.Tags["Creature"] = "";
            entity.Tags["Player"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 18, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = "you" });
            entity.AddPart(new InventoryPart());
            return entity;
        }

        private ConversationData CreateSimpleConversation(string id = "TestConv")
        {
            var conv = new ConversationData { ID = id };

            var startNode = new NodeData
            {
                ID = "Start",
                Text = "Hello, traveler.",
                Choices = new List<ChoiceData>
                {
                    new ChoiceData { Text = "Tell me more.", Target = "More" },
                    new ChoiceData { Text = "Goodbye.", Target = "End" }
                }
            };

            var moreNode = new NodeData
            {
                ID = "More",
                Text = "This land is dangerous.",
                Choices = new List<ChoiceData>
                {
                    new ChoiceData { Text = "I see.", Target = "Start" },
                    new ChoiceData { Text = "Farewell.", Target = "End" }
                }
            };

            conv.Nodes.Add(startNode);
            conv.Nodes.Add(moreNode);
            return conv;
        }

        // ========================
        // ConversationData Tests
        // ========================

        [Test]
        public void ConversationData_GetStartNode_ReturnsStartNode()
        {
            var conv = CreateSimpleConversation();
            var start = conv.GetStartNode();
            Assert.IsNotNull(start);
            Assert.AreEqual("Start", start.ID);
        }

        [Test]
        public void ConversationData_GetStartNode_FallsBackToFirstNode()
        {
            var conv = new ConversationData { ID = "NoStart" };
            conv.Nodes.Add(new NodeData { ID = "First", Text = "Hello" });
            var start = conv.GetStartNode();
            Assert.IsNotNull(start);
            Assert.AreEqual("First", start.ID);
        }

        [Test]
        public void ConversationData_GetNode_FindsByID()
        {
            var conv = CreateSimpleConversation();
            var more = conv.GetNode("More");
            Assert.IsNotNull(more);
            Assert.AreEqual("This land is dangerous.", more.Text);
        }

        [Test]
        public void ConversationData_GetNode_ReturnsNullForMissing()
        {
            var conv = CreateSimpleConversation();
            Assert.IsNull(conv.GetNode("Nonexistent"));
        }

        // ========================
        // ConversationLoader Tests
        // ========================

        [Test]
        public void ConversationLoader_Register_MakesConversationRetrievable()
        {
            var conv = CreateSimpleConversation();
            ConversationLoader.Register(conv);

            var loaded = ConversationLoader.Get("TestConv");
            Assert.IsNotNull(loaded);
            Assert.AreEqual("TestConv", loaded.ID);
            Assert.AreEqual(2, loaded.Nodes.Count);
        }

        [Test]
        public void ConversationLoader_Get_ReturnsNullForUnknown()
        {
            var result = ConversationLoader.Get("DoesNotExist");
            Assert.IsNull(result);
        }

        [Test]
        public void ConversationLoader_LoadFromJson_ParsesConversation()
        {
            string json = @"{
                ""Conversations"": [{
                    ""ID"": ""JsonConv"",
                    ""Nodes"": [{
                        ""ID"": ""Start"",
                        ""Text"": ""Hello from JSON."",
                        ""Choices"": [{
                            ""Text"": ""Bye."",
                            ""Target"": ""End""
                        }]
                    }]
                }]
            }";

            ConversationLoader.LoadFromJson(json, "test");
            var loaded = ConversationLoader.Get("JsonConv");
            Assert.IsNotNull(loaded);
            Assert.AreEqual("Hello from JSON.", loaded.GetStartNode().Text);
            Assert.AreEqual(1, loaded.GetStartNode().Choices.Count);
        }

        [Test]
        public void ConversationLoader_LoadFromJson_ParsesPredicatesAndActions()
        {
            string json = @"{
                ""Conversations"": [{
                    ""ID"": ""PA"",
                    ""Nodes"": [{
                        ""ID"": ""Start"",
                        ""Text"": ""Test."",
                        ""Choices"": [{
                            ""Text"": ""Choice"",
                            ""Target"": ""End"",
                            ""Predicates"": [{ ""Key"": ""IfHaveTag"", ""Value"": ""Trader"" }],
                            ""Actions"": [{ ""Key"": ""SetTag"", ""Value"": ""Talked"" }]
                        }]
                    }]
                }]
            }";

            ConversationLoader.LoadFromJson(json, "test");
            var conv = ConversationLoader.Get("PA");
            var choice = conv.GetStartNode().Choices[0];
            Assert.AreEqual("IfHaveTag", choice.Predicates[0].Key);
            Assert.AreEqual("Trader", choice.Predicates[0].Value);
            Assert.AreEqual("SetTag", choice.Actions[0].Key);
            Assert.AreEqual("Talked", choice.Actions[0].Value);
        }

        // ========================
        // ConversationPredicates Tests
        // ========================

        [Test]
        public void Predicate_IfHaveTag_TrueWhenPlayerHasTag()
        {
            var player = CreatePlayer();
            player.SetTag("Trader", "");
            bool result = ConversationPredicates.Evaluate("IfHaveTag", null, player, "Trader");
            Assert.IsTrue(result);
        }

        [Test]
        public void Predicate_IfHaveTag_FalseWhenPlayerLacksTag()
        {
            var player = CreatePlayer();
            bool result = ConversationPredicates.Evaluate("IfHaveTag", null, player, "Trader");
            Assert.IsFalse(result);
        }

        [Test]
        public void Predicate_IfNotHaveTag_InvertsIfHaveTag()
        {
            var player = CreatePlayer();
            bool result = ConversationPredicates.Evaluate("IfNotHaveTag", null, player, "Trader");
            Assert.IsTrue(result);

            player.SetTag("Trader", "");
            result = ConversationPredicates.Evaluate("IfNotHaveTag", null, player, "Trader");
            Assert.IsFalse(result);
        }

        [Test]
        public void Predicate_IfHaveProperty_ChecksPlayerProperties()
        {
            var player = CreatePlayer();
            Assert.IsFalse(ConversationPredicates.Evaluate("IfHaveProperty", null, player, "QuestDone"));
            player.Properties["QuestDone"] = "true";
            Assert.IsTrue(ConversationPredicates.Evaluate("IfHaveProperty", null, player, "QuestDone"));
        }

        [Test]
        public void Predicate_IfStatAtLeast_ChecksStatThreshold()
        {
            var player = CreatePlayer(); // Ego=18
            Assert.IsTrue(ConversationPredicates.Evaluate("IfStatAtLeast", null, player, "Ego:18"));
            Assert.IsTrue(ConversationPredicates.Evaluate("IfStatAtLeast", null, player, "Ego:10"));
            Assert.IsFalse(ConversationPredicates.Evaluate("IfStatAtLeast", null, player, "Ego:20"));
        }

        [Test]
        public void Predicate_IfHaveItem_ChecksInventory()
        {
            var player = CreatePlayer();
            var item = new Entity { BlueprintName = "HealingTonic" };
            var inv = player.GetPart<InventoryPart>();

            Assert.IsFalse(ConversationPredicates.Evaluate("IfHaveItem", null, player, "HealingTonic"));

            inv.AddObject(item);
            Assert.IsTrue(ConversationPredicates.Evaluate("IfHaveItem", null, player, "HealingTonic"));
        }

        [Test]
        public void Predicate_IfSpeakerHaveTag_ChecksSpeaker()
        {
            var speaker = CreateNPC();
            speaker.SetTag("Angry", "");
            Assert.IsTrue(ConversationPredicates.Evaluate("IfSpeakerHaveTag", speaker, null, "Angry"));
            Assert.IsFalse(ConversationPredicates.Evaluate("IfSpeakerHaveTag", speaker, null, "Happy"));
        }

        [Test]
        public void Predicate_CheckAll_ReturnsTrueForEmptyList()
        {
            Assert.IsTrue(ConversationPredicates.CheckAll(null, null, null));
            Assert.IsTrue(ConversationPredicates.CheckAll(new List<ConversationParam>(), null, null));
        }

        [Test]
        public void Predicate_CheckAll_AllMustPass()
        {
            var player = CreatePlayer();
            player.SetTag("Trader", "");

            var predicates = new List<ConversationParam>
            {
                new ConversationParam { Key = "IfHaveTag", Value = "Trader" },
                new ConversationParam { Key = "IfStatAtLeast", Value = "Ego:18" }
            };

            Assert.IsTrue(ConversationPredicates.CheckAll(predicates, null, player));

            // Remove the tag → first predicate fails
            player.Tags.Remove("Trader");
            Assert.IsFalse(ConversationPredicates.CheckAll(predicates, null, player));
        }

        // ========================
        // ConversationActions Tests
        // ========================

        [Test]
        public void Action_SetTag_SetsTagOnListener()
        {
            var player = CreatePlayer();
            ConversationActions.Execute("SetTag", null, player, "QuestAccepted");
            Assert.IsTrue(player.HasTag("QuestAccepted"));
        }

        [Test]
        public void Action_SetTag_WithValue_SetsTagAndValue()
        {
            var player = CreatePlayer();
            ConversationActions.Execute("SetTag", null, player, "Quest:Started");
            Assert.IsTrue(player.HasTag("Quest"));
            Assert.AreEqual("Started", player.GetTag("Quest"));
        }

        [Test]
        public void Action_SetProperty_SetsPropertyOnListener()
        {
            var player = CreatePlayer();
            ConversationActions.Execute("SetProperty", null, player, "ConvState:visited");
            Assert.AreEqual("visited", player.Properties["ConvState"]);
        }

        [Test]
        public void Action_SetSpeakerProperty_SetsPropertyOnSpeaker()
        {
            var speaker = CreateNPC();
            ConversationActions.Execute("SetSpeakerProperty", speaker, null, "AlreadyTalked:true");
            Assert.AreEqual("true", speaker.Properties["AlreadyTalked"]);
        }

        [Test]
        public void Action_SetIntProperty_SetsIntOnListener()
        {
            var player = CreatePlayer();
            ConversationActions.Execute("SetIntProperty", null, player, "QuestStage:2");
            Assert.AreEqual(2, player.GetIntProperty("QuestStage"));
        }

        [Test]
        public void Action_AddMessage_AddsToMessageLog()
        {
            MessageLog.Clear();
            string msg = null;
            MessageLog.OnMessage = m => msg = m;

            ConversationActions.Execute("AddMessage", null, null, "The elder nods.");
            Assert.AreEqual("The elder nods.", msg);
        }

        [Test]
        public void Action_TakeItem_RemovesFromInventory()
        {
            var player = CreatePlayer();
            var item = new Entity { BlueprintName = "OldSword" };
            item.AddPart(new RenderPart { DisplayName = "old sword" });
            var inv = player.GetPart<InventoryPart>();
            inv.AddObject(item);

            Assert.AreEqual(1, inv.Objects.Count);
            ConversationActions.Execute("TakeItem", null, player, "OldSword");
            Assert.AreEqual(0, inv.Objects.Count);
        }

        [Test]
        public void Action_ChangeFactionFeeling_ModifiesReputation()
        {
            int before = FactionManager.GetFactionFeeling("Villagers", "Player");
            ConversationActions.Execute("ChangeFactionFeeling", null, null, "Villagers:Player:10");
            int after = FactionManager.GetFactionFeeling("Villagers", "Player");
            Assert.AreEqual(before + 10, after);
        }

        // ========================
        // ConversationManager Tests
        // ========================

        [Test]
        public void Manager_StartConversation_SetsActiveState()
        {
            var conv = CreateSimpleConversation();
            ConversationLoader.Register(conv);
            var speaker = CreateNPC();
            var player = CreatePlayer();

            bool started = ConversationManager.StartConversation(speaker, player);
            Assert.IsTrue(started);
            Assert.IsTrue(ConversationManager.IsActive);
            Assert.AreEqual(speaker, ConversationManager.Speaker);
            Assert.AreEqual(player, ConversationManager.Listener);
            Assert.AreEqual("Start", ConversationManager.CurrentNode.ID);
        }

        [Test]
        public void Manager_StartConversation_SetsInConversationOnBrain()
        {
            var conv = CreateSimpleConversation();
            ConversationLoader.Register(conv);
            var speaker = CreateNPC();
            var player = CreatePlayer();

            ConversationManager.StartConversation(speaker, player);
            var brain = speaker.GetPart<BrainPart>();
            Assert.IsTrue(brain.InConversation);
        }

        [Test]
        public void Manager_StartConversation_FailsIfHostile()
        {
            var conv = CreateSimpleConversation();
            ConversationLoader.Register(conv);
            var speaker = CreateNPC(faction: "Snapjaws");
            var player = CreatePlayer();
            player.Tags["Faction"] = "Player";

            bool started = ConversationManager.StartConversation(speaker, player);
            Assert.IsFalse(started);
            Assert.IsFalse(ConversationManager.IsActive);
        }

        [Test]
        public void Manager_StartConversation_FailsIfNoConversationData()
        {
            // Don't register any conversation data
            var speaker = CreateNPC("Missing");
            var player = CreatePlayer();

            bool started = ConversationManager.StartConversation(speaker, player);
            Assert.IsFalse(started);
        }

        [Test]
        public void Manager_StartConversation_FailsIfNoConversationPart()
        {
            var speaker = new Entity { BlueprintName = "NoPart" };
            speaker.AddPart(new RenderPart { DisplayName = "nobody" });
            var player = CreatePlayer();

            bool started = ConversationManager.StartConversation(speaker, player);
            Assert.IsFalse(started);
        }

        [Test]
        public void Manager_SelectChoice_NavigatesToTargetNode()
        {
            var conv = CreateSimpleConversation();
            ConversationLoader.Register(conv);
            var speaker = CreateNPC();
            var player = CreatePlayer();
            ConversationManager.StartConversation(speaker, player);

            // Choice 0 = "Tell me more" -> "More"
            bool continues = ConversationManager.SelectChoice(0);
            Assert.IsTrue(continues);
            Assert.AreEqual("More", ConversationManager.CurrentNode.ID);
        }

        [Test]
        public void Manager_SelectChoice_EndTarget_EndsConversation()
        {
            var conv = CreateSimpleConversation();
            ConversationLoader.Register(conv);
            var speaker = CreateNPC();
            var player = CreatePlayer();
            ConversationManager.StartConversation(speaker, player);

            // Choice 1 = "Goodbye" -> "End"
            bool continues = ConversationManager.SelectChoice(1);
            Assert.IsFalse(continues);
            Assert.IsFalse(ConversationManager.IsActive);
        }

        [Test]
        public void Manager_SelectChoice_StartTarget_ReturnsToStart()
        {
            var conv = CreateSimpleConversation();
            ConversationLoader.Register(conv);
            var speaker = CreateNPC();
            var player = CreatePlayer();
            ConversationManager.StartConversation(speaker, player);

            // Navigate to "More"
            ConversationManager.SelectChoice(0);
            Assert.AreEqual("More", ConversationManager.CurrentNode.ID);

            // Choice 0 in "More" = "I see" -> "Start"
            bool continues = ConversationManager.SelectChoice(0);
            Assert.IsTrue(continues);
            Assert.AreEqual("Start", ConversationManager.CurrentNode.ID);
        }

        [Test]
        public void Manager_EndConversation_ClearsState()
        {
            var conv = CreateSimpleConversation();
            ConversationLoader.Register(conv);
            var speaker = CreateNPC();
            var player = CreatePlayer();
            ConversationManager.StartConversation(speaker, player);

            ConversationManager.EndConversation();
            Assert.IsFalse(ConversationManager.IsActive);
            Assert.IsNull(ConversationManager.Speaker);
            Assert.IsNull(ConversationManager.Listener);
            Assert.IsNull(ConversationManager.CurrentNode);
        }

        [Test]
        public void Manager_EndConversation_ClearsInConversationOnBrain()
        {
            var conv = CreateSimpleConversation();
            ConversationLoader.Register(conv);
            var speaker = CreateNPC();
            var player = CreatePlayer();
            ConversationManager.StartConversation(speaker, player);

            Assert.IsTrue(speaker.GetPart<BrainPart>().InConversation);
            ConversationManager.EndConversation();
            Assert.IsFalse(speaker.GetPart<BrainPart>().InConversation);
        }

        [Test]
        public void Manager_VisibleChoices_FilteredByPredicates()
        {
            var conv = new ConversationData { ID = "Filtered" };
            var node = new NodeData
            {
                ID = "Start",
                Text = "What do you need?",
                Choices = new List<ChoiceData>
                {
                    new ChoiceData
                    {
                        Text = "I'm a trader.",
                        Target = "End",
                        Predicates = new List<ConversationParam>
                        {
                            new ConversationParam { Key = "IfHaveTag", Value = "Trader" }
                        }
                    },
                    new ChoiceData
                    {
                        Text = "Just browsing.",
                        Target = "End"
                    }
                }
            };
            conv.Nodes.Add(node);
            ConversationLoader.Register(conv);

            var speaker = CreateNPC("Filtered");
            var player = CreatePlayer();
            ConversationManager.StartConversation(speaker, player);

            // Player lacks "Trader" tag — only "Just browsing" + "[Attack]" should be visible
            Assert.AreEqual(2, ConversationManager.VisibleChoices.Count);
            Assert.AreEqual("Just browsing.", ConversationManager.VisibleChoices[0].Text);

            // Give player the tag and refresh
            ConversationManager.EndConversation();
            player.SetTag("Trader", "");
            ConversationManager.StartConversation(speaker, player);

            Assert.AreEqual(3, ConversationManager.VisibleChoices.Count);
        }

        [Test]
        public void Manager_SelectChoice_ExecutesActions()
        {
            var conv = new ConversationData { ID = "WithActions" };
            var node = new NodeData
            {
                ID = "Start",
                Text = "Take this gift.",
                Choices = new List<ChoiceData>
                {
                    new ChoiceData
                    {
                        Text = "Thanks!",
                        Target = "End",
                        Actions = new List<ConversationParam>
                        {
                            new ConversationParam { Key = "SetTag", Value = "ReceivedGift" }
                        }
                    }
                }
            };
            conv.Nodes.Add(node);
            ConversationLoader.Register(conv);

            var speaker = CreateNPC("WithActions");
            var player = CreatePlayer();
            ConversationManager.StartConversation(speaker, player);

            Assert.IsFalse(player.HasTag("ReceivedGift"));
            ConversationManager.SelectChoice(0);
            Assert.IsTrue(player.HasTag("ReceivedGift"));
        }

        [Test]
        public void Manager_SelectChoice_InvalidIndex_ReturnsFalse()
        {
            var conv = CreateSimpleConversation();
            ConversationLoader.Register(conv);
            var speaker = CreateNPC();
            var player = CreatePlayer();
            ConversationManager.StartConversation(speaker, player);

            Assert.IsFalse(ConversationManager.SelectChoice(-1));
            Assert.IsFalse(ConversationManager.SelectChoice(99));
            // Conversation should still be active
            Assert.IsTrue(ConversationManager.IsActive);
        }

        // ========================
        // ConversationPart Tests
        // ========================

        [Test]
        public void ConversationPart_Name_IsConversation()
        {
            var part = new ConversationPart();
            Assert.AreEqual("Conversation", part.Name);
        }

        [Test]
        public void ConversationPart_StoresConversationID()
        {
            var part = new ConversationPart { ConversationID = "Elder_1" };
            Assert.AreEqual("Elder_1", part.ConversationID);
        }

        // ========================
        // ChoiceData Helper Tests
        // ========================

        [Test]
        public void ChoiceData_GetPredicate_FindsByKey()
        {
            var choice = new ChoiceData
            {
                Predicates = new List<ConversationParam>
                {
                    new ConversationParam { Key = "IfHaveTag", Value = "Foo" }
                }
            };
            Assert.AreEqual("Foo", choice.GetPredicate("IfHaveTag"));
            Assert.IsNull(choice.GetPredicate("Missing"));
        }

        [Test]
        public void ChoiceData_GetAction_FindsByKey()
        {
            var choice = new ChoiceData
            {
                Actions = new List<ConversationParam>
                {
                    new ConversationParam { Key = "SetTag", Value = "Bar" }
                }
            };
            Assert.AreEqual("Bar", choice.GetAction("SetTag"));
            Assert.IsNull(choice.GetAction("Missing"));
        }

        // ========================
        // Integration: Multi-step conversation flow
        // ========================

        [Test]
        public void Integration_FullConversationFlow()
        {
            // Create a conversation with conditional choices and actions
            var conv = new ConversationData { ID = "FullFlow" };
            conv.Nodes.Add(new NodeData
            {
                ID = "Start",
                Text = "Welcome.",
                Choices = new List<ChoiceData>
                {
                    new ChoiceData { Text = "Help me.", Target = "Help" },
                    new ChoiceData
                    {
                        Text = "Secret option.",
                        Target = "Secret",
                        Predicates = new List<ConversationParam>
                        {
                            new ConversationParam { Key = "IfHaveTag", Value = "KnowsSecret" }
                        }
                    },
                    new ChoiceData { Text = "Bye.", Target = "End" }
                }
            });
            conv.Nodes.Add(new NodeData
            {
                ID = "Help",
                Text = "I can teach you.",
                Choices = new List<ChoiceData>
                {
                    new ChoiceData
                    {
                        Text = "Teach me.",
                        Target = "Start",
                        Actions = new List<ConversationParam>
                        {
                            new ConversationParam { Key = "SetTag", Value = "KnowsSecret" }
                        }
                    }
                }
            });
            conv.Nodes.Add(new NodeData
            {
                ID = "Secret",
                Text = "You found the secret!",
                Choices = new List<ChoiceData>
                {
                    new ChoiceData { Text = "Neat.", Target = "End" }
                }
            });
            ConversationLoader.Register(conv);

            var speaker = CreateNPC("FullFlow");
            var player = CreatePlayer();

            // Start conversation
            ConversationManager.StartConversation(speaker, player);
            Assert.AreEqual("Start", ConversationManager.CurrentNode.ID);

            // Secret option should be hidden (player doesn't have tag): "Help me." + "Bye." + "[Attack]"
            Assert.AreEqual(3, ConversationManager.VisibleChoices.Count);
            Assert.AreEqual("Help me.", ConversationManager.VisibleChoices[0].Text);
            Assert.AreEqual("Bye.", ConversationManager.VisibleChoices[1].Text);

            // Go to Help
            ConversationManager.SelectChoice(0);
            Assert.AreEqual("Help", ConversationManager.CurrentNode.ID);

            // Select "Teach me" → sets KnowsSecret tag, returns to Start
            ConversationManager.SelectChoice(0);
            Assert.IsTrue(player.HasTag("KnowsSecret"));
            Assert.AreEqual("Start", ConversationManager.CurrentNode.ID);

            // Now Secret option should be visible: "Help me." + "Secret option." + "Bye." + "[Attack]"
            Assert.AreEqual(4, ConversationManager.VisibleChoices.Count);
            Assert.AreEqual("Secret option.", ConversationManager.VisibleChoices[1].Text);

            // Navigate to Secret
            ConversationManager.SelectChoice(1);
            Assert.AreEqual("Secret", ConversationManager.CurrentNode.ID);

            // End conversation
            bool continues = ConversationManager.SelectChoice(0);
            Assert.IsFalse(continues);
            Assert.IsFalse(ConversationManager.IsActive);
        }
    }
}
