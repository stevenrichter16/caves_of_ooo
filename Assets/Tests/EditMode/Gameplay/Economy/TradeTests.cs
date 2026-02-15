using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    public class TradeTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            ConversationLoader.Reset();
            ConversationPredicates.Reset();
            ConversationActions.Reset();
            ConversationManager.EndConversation();
            ConversationManager.PendingTradePartner = null;
            ConversationManager.PendingAttackTarget = null;
            MessageLog.Clear();
        }

        // ========================
        // Helpers
        // ========================

        private Entity CreateNPC(string conversationID = "TestConv")
        {
            var entity = new Entity { BlueprintName = "TestNPC" };
            entity.Tags["Creature"] = "";
            entity.Tags["Faction"] = "Villagers";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 10, Min = 1, Max = 50 };
            entity.AddPart(new RenderPart { DisplayName = "Test NPC", RenderString = "@", ColorString = "&M" });
            entity.AddPart(new BrainPart());
            entity.AddPart(new ConversationPart { ConversationID = conversationID });
            entity.AddPart(new InventoryPart());
            return entity;
        }

        private Entity CreateNPCWithoutInventory(string conversationID = "TestConv")
        {
            var entity = new Entity { BlueprintName = "NoInvNPC" };
            entity.Tags["Creature"] = "";
            entity.Tags["Faction"] = "Villagers";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = "Ghost", RenderString = "g" });
            entity.AddPart(new BrainPart());
            entity.AddPart(new ConversationPart { ConversationID = conversationID });
            // No InventoryPart
            return entity;
        }

        private Entity CreatePlayer(int drams = 100)
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
            TradeSystem.SetDrams(entity, drams);
            return entity;
        }

        private Entity CreateTradeItem(string name, int value, int weight = 1)
        {
            var item = new Entity { BlueprintName = name };
            item.AddPart(new RenderPart { DisplayName = name.ToLower() });
            item.AddPart(new CommercePart { Value = value });
            item.AddPart(new PhysicsPart { Weight = weight, Takeable = true });
            return item;
        }

        private ConversationData CreateSimpleConversation(string id = "TestConv")
        {
            var conv = new ConversationData { ID = id };
            conv.Nodes.Add(new NodeData
            {
                ID = "Start",
                Text = "Hello.",
                Choices = new List<ChoiceData>
                {
                    new ChoiceData { Text = "Goodbye.", Target = "End" }
                }
            });
            return conv;
        }

        private ConversationData CreateMultiNodeConversation()
        {
            var conv = new ConversationData { ID = "TestConv" };
            conv.Nodes.Add(new NodeData
            {
                ID = "Start",
                Text = "Hello.",
                Choices = new List<ChoiceData>
                {
                    new ChoiceData { Text = "Tell me more.", Target = "More" },
                    new ChoiceData { Text = "Bye.", Target = "End" }
                }
            });
            conv.Nodes.Add(new NodeData
            {
                ID = "More",
                Text = "More info.",
                Choices = new List<ChoiceData>
                {
                    new ChoiceData { Text = "Back.", Target = "Start" }
                }
            });
            return conv;
        }

        // ========================
        // Trade Choice Injection Tests
        // ========================

        [Test]
        public void RefreshVisibleChoices_InjectsTradeChoice_WhenSpeakerHasInventory()
        {
            var conv = CreateSimpleConversation();
            ConversationLoader.Register(conv);
            var speaker = CreateNPC();
            var player = CreatePlayer();

            ConversationManager.StartConversation(speaker, player);

            // Should have original "Goodbye." + "[Let's trade.]" + "[Attack]"
            Assert.AreEqual(3, ConversationManager.VisibleChoices.Count);
            Assert.AreEqual("Goodbye.", ConversationManager.VisibleChoices[0].Text);
            Assert.AreEqual("[Let's trade.]", ConversationManager.VisibleChoices[1].Text);
            Assert.AreEqual("[Attack]", ConversationManager.VisibleChoices[2].Text);
        }

        [Test]
        public void RefreshVisibleChoices_NoTradeChoice_WhenSpeakerLacksInventory()
        {
            var conv = CreateSimpleConversation();
            ConversationLoader.Register(conv);
            var speaker = CreateNPCWithoutInventory();
            var player = CreatePlayer();

            ConversationManager.StartConversation(speaker, player);

            // Should have original "Goodbye." + "[Attack]" (no trade — no inventory)
            Assert.AreEqual(2, ConversationManager.VisibleChoices.Count);
            Assert.AreEqual("Goodbye.", ConversationManager.VisibleChoices[0].Text);
            Assert.AreEqual("[Attack]", ConversationManager.VisibleChoices[1].Text);
        }

        [Test]
        public void TradeChoice_AppearsOnEveryNode()
        {
            var conv = CreateMultiNodeConversation();
            ConversationLoader.Register(conv);
            var speaker = CreateNPC();
            var player = CreatePlayer();

            ConversationManager.StartConversation(speaker, player);

            // Start node: "Tell me more" + "Bye" + "[Let's trade.]" + "[Attack]"
            Assert.AreEqual(4, ConversationManager.VisibleChoices.Count);
            Assert.AreEqual("[Let's trade.]", ConversationManager.VisibleChoices[2].Text);
            Assert.AreEqual("[Attack]", ConversationManager.VisibleChoices[3].Text);

            // Navigate to "More" node
            ConversationManager.SelectChoice(0);
            Assert.AreEqual("More", ConversationManager.CurrentNode.ID);

            // More node: "Back" + "[Let's trade.]" + "[Attack]"
            Assert.AreEqual(3, ConversationManager.VisibleChoices.Count);
            Assert.AreEqual("[Let's trade.]", ConversationManager.VisibleChoices[1].Text);
            Assert.AreEqual("[Attack]", ConversationManager.VisibleChoices[2].Text);
        }

        [Test]
        public void TradeChoice_AppearsBeforeAttack()
        {
            var conv = CreateSimpleConversation();
            ConversationLoader.Register(conv);
            var speaker = CreateNPC();
            var player = CreatePlayer();

            ConversationManager.StartConversation(speaker, player);

            // Trade should be second-to-last, Attack should be last
            int count = ConversationManager.VisibleChoices.Count;
            Assert.AreEqual("[Let's trade.]", ConversationManager.VisibleChoices[count - 2].Text);
            Assert.AreEqual("[Attack]", ConversationManager.VisibleChoices[count - 1].Text);
        }

        // ========================
        // StartTrade Action Tests
        // ========================

        [Test]
        public void StartTrade_Action_SetsPendingTradePartner()
        {
            var speaker = CreateNPC();
            var player = CreatePlayer();

            Assert.IsNull(ConversationManager.PendingTradePartner);
            ConversationActions.Execute("StartTrade", speaker, player, "");
            Assert.AreEqual(speaker, ConversationManager.PendingTradePartner);
        }

        [Test]
        public void SelectTradeChoice_EndsConversation_AndSetsPendingTrader()
        {
            var conv = CreateSimpleConversation();
            ConversationLoader.Register(conv);
            var speaker = CreateNPC();
            var player = CreatePlayer();

            ConversationManager.StartConversation(speaker, player);

            // Find and select the trade choice (second-to-last, before [Attack])
            int tradeIdx = ConversationManager.VisibleChoices.Count - 2;
            Assert.AreEqual("[Let's trade.]", ConversationManager.VisibleChoices[tradeIdx].Text);

            bool continues = ConversationManager.SelectChoice(tradeIdx);
            Assert.IsFalse(continues); // Target is "End"
            Assert.IsFalse(ConversationManager.IsActive);
            Assert.AreEqual(speaker, ConversationManager.PendingTradePartner);
        }

        // ========================
        // TradeSystem Buy/Sell Tests
        // ========================

        [Test]
        public void BuyFromTrader_TransfersItemAndDrams()
        {
            var player = CreatePlayer(100);
            var trader = CreateNPC();
            TradeSystem.SetDrams(trader, 200);

            var item = CreateTradeItem("Dagger", 10);
            trader.GetPart<InventoryPart>().AddObject(item);

            double perf = TradeSystem.GetTradePerformance(player);
            int buyPrice = TradeSystem.GetBuyPrice(item, perf, trader);

            bool success = TradeSystem.BuyFromTrader(player, trader, item);
            Assert.IsTrue(success);

            // Item moved to player
            Assert.IsTrue(player.GetPart<InventoryPart>().Objects.Contains(item));
            Assert.IsFalse(trader.GetPart<InventoryPart>().Objects.Contains(item));

            // Drams transferred
            Assert.AreEqual(100 - buyPrice, TradeSystem.GetDrams(player));
            Assert.AreEqual(200 + buyPrice, TradeSystem.GetDrams(trader));
        }

        [Test]
        public void SellToTrader_TransfersItemAndDrams()
        {
            var player = CreatePlayer(50);
            var trader = CreateNPC();
            TradeSystem.SetDrams(trader, 200);

            var item = CreateTradeItem("Sword", 25);
            player.GetPart<InventoryPart>().AddObject(item);

            double perf = TradeSystem.GetTradePerformance(player);
            int sellPrice = TradeSystem.GetSellPrice(item, perf, trader);

            bool success = TradeSystem.SellToTrader(player, trader, item);
            Assert.IsTrue(success);

            // Item moved to trader
            Assert.IsTrue(trader.GetPart<InventoryPart>().Objects.Contains(item));
            Assert.IsFalse(player.GetPart<InventoryPart>().Objects.Contains(item));

            // Drams transferred
            Assert.AreEqual(50 + sellPrice, TradeSystem.GetDrams(player));
            Assert.AreEqual(200 - sellPrice, TradeSystem.GetDrams(trader));
        }

        [Test]
        public void BuyFromTrader_Fails_WhenPlayerCantAfford()
        {
            var player = CreatePlayer(0); // No drams
            var trader = CreateNPC();

            var item = CreateTradeItem("ExpensiveItem", 100);
            trader.GetPart<InventoryPart>().AddObject(item);

            bool success = TradeSystem.BuyFromTrader(player, trader, item);
            Assert.IsFalse(success);

            // Item stays with trader
            Assert.IsTrue(trader.GetPart<InventoryPart>().Objects.Contains(item));
            Assert.AreEqual(0, TradeSystem.GetDrams(player));
        }

        [Test]
        public void SellToTrader_Fails_WhenTraderCantAfford()
        {
            var player = CreatePlayer(50);
            var trader = CreateNPC();
            TradeSystem.SetDrams(trader, 0); // No drams

            var item = CreateTradeItem("ExpensiveItem", 100);
            player.GetPart<InventoryPart>().AddObject(item);

            bool success = TradeSystem.SellToTrader(player, trader, item);
            Assert.IsFalse(success);

            // Item stays with player
            Assert.IsTrue(player.GetPart<InventoryPart>().Objects.Contains(item));
        }

        [Test]
        public void GetTraderStock_ReturnsOnlyItemsWithCommerce()
        {
            var trader = CreateNPC();
            var inv = trader.GetPart<InventoryPart>();

            var tradeItem = CreateTradeItem("Dagger", 10);
            inv.AddObject(tradeItem);

            // Item without CommercePart
            var nonTradeItem = new Entity { BlueprintName = "Rock" };
            nonTradeItem.AddPart(new RenderPart { DisplayName = "rock" });
            nonTradeItem.AddPart(new PhysicsPart { Weight = 5 });
            inv.AddObject(nonTradeItem);

            var stock = TradeSystem.GetTraderStock(trader);
            Assert.AreEqual(1, stock.Count);
            Assert.AreEqual(tradeItem, stock[0]);
        }

        // ========================
        // Trade Performance Tests
        // ========================

        [Test]
        public void TradePerformance_HigherEgo_BetterDeals()
        {
            var lowEgo = CreatePlayer();
            lowEgo.Statistics["Ego"].BaseValue = 10; // mod = 0
            var highEgo = CreatePlayer();
            highEgo.Statistics["Ego"].BaseValue = 22; // mod = +6

            double lowPerf = TradeSystem.GetTradePerformance(lowEgo);
            double highPerf = TradeSystem.GetTradePerformance(highEgo);

            Assert.Greater(highPerf, lowPerf);
        }

        [Test]
        public void BuyPrice_DecreasesWithHigherPerformance()
        {
            var item = CreateTradeItem("Sword", 50);
            int lowPrice = TradeSystem.GetBuyPrice(item, 0.3);
            int highPrice = TradeSystem.GetBuyPrice(item, 0.7);

            Assert.Greater(lowPrice, highPrice);
        }

        [Test]
        public void SellPrice_IncreasesWithHigherPerformance()
        {
            var item = CreateTradeItem("Sword", 50);
            int lowPrice = TradeSystem.GetSellPrice(item, 0.3);
            int highPrice = TradeSystem.GetSellPrice(item, 0.7);

            Assert.Greater(highPrice, lowPrice);
        }

        // ========================
        // Attack Choice Injection Tests
        // ========================

        [Test]
        public void RefreshVisibleChoices_InjectsAttackChoice_ForAllNPCs()
        {
            var conv = CreateSimpleConversation();
            ConversationLoader.Register(conv);
            var speaker = CreateNPC();
            var player = CreatePlayer();

            ConversationManager.StartConversation(speaker, player);

            bool hasAttack = false;
            for (int i = 0; i < ConversationManager.VisibleChoices.Count; i++)
            {
                if (ConversationManager.VisibleChoices[i].Text == "[Attack]")
                {
                    hasAttack = true;
                    break;
                }
            }
            Assert.IsTrue(hasAttack, "Should inject [Attack] choice");
        }

        [Test]
        public void AttackChoice_IsAlwaysLast()
        {
            var conv = CreateSimpleConversation();
            ConversationLoader.Register(conv);
            var speaker = CreateNPC();
            var player = CreatePlayer();

            ConversationManager.StartConversation(speaker, player);

            var lastChoice = ConversationManager.VisibleChoices[ConversationManager.VisibleChoices.Count - 1];
            Assert.AreEqual("[Attack]", lastChoice.Text);
        }

        [Test]
        public void AttackChoice_SetsPendingAttackTarget()
        {
            var conv = CreateSimpleConversation();
            ConversationLoader.Register(conv);
            var speaker = CreateNPC();
            var player = CreatePlayer();

            ConversationManager.StartConversation(speaker, player);

            // Find the [Attack] choice
            int attackIdx = -1;
            for (int i = 0; i < ConversationManager.VisibleChoices.Count; i++)
            {
                if (ConversationManager.VisibleChoices[i].Text == "[Attack]")
                {
                    attackIdx = i;
                    break;
                }
            }
            Assert.GreaterOrEqual(attackIdx, 0);

            ConversationManager.SelectChoice(attackIdx);
            Assert.AreEqual(speaker, ConversationManager.PendingAttackTarget);
        }

        [Test]
        public void AttackChoice_AppearsEvenWithoutInventory()
        {
            var conv = CreateSimpleConversation();
            ConversationLoader.Register(conv);
            var speaker = CreateNPCWithoutInventory();
            var player = CreatePlayer();

            ConversationManager.StartConversation(speaker, player);

            bool hasAttack = false;
            for (int i = 0; i < ConversationManager.VisibleChoices.Count; i++)
            {
                if (ConversationManager.VisibleChoices[i].Text == "[Attack]")
                {
                    hasAttack = true;
                    break;
                }
            }
            Assert.IsTrue(hasAttack, "[Attack] should appear even for NPCs without inventory");
        }
    }
}
