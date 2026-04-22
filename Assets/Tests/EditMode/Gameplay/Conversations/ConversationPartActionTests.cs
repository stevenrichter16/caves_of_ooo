using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase 4b of the World Action Menu plan — verifies
    /// <see cref="ConversationPart"/> declares a "Chat" action and handles
    /// the command:
    /// - Adds Chat action with hotkey 'c', priority 10
    /// - Chat command on an NPC with a valid ConversationID starts the
    ///   conversation (sets ConversationManager.IsActive)
    /// - Chat command on an NPC with an empty/invalid ConversationID logs
    ///   the fallback "Hi." greeting
    /// - Chat command on a hostile NPC does NOT double-log (StartConversation
    ///   logs the "refuses to speak" line; we skip the fallback)
    /// - Entities WITHOUT ConversationPart don't contribute Chat (sanity —
    ///   e.g., a Snapjaw)
    /// </summary>
    [TestFixture]
    public class ConversationPartActionTests
    {
        private EntityFactory _factory;

        [SetUp]
        public void SetUp()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
            ConversationManager.EndConversation(); // ensure clean state
            _factory = new EntityFactory();
            string blueprintPath = Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json");
            _factory.LoadBlueprints(File.ReadAllText(blueprintPath));
        }

        [TearDown]
        public void TearDown()
        {
            ConversationManager.EndConversation();
            FactionManager.Reset();
        }

        // ==========================================================
        // Action declaration
        // ==========================================================

        [Test]
        public void ConversationPart_DeclaresChatAction()
        {
            var npc = new Entity { BlueprintName = "TestNPC" };
            npc.AddPart(new RenderPart { DisplayName = "friend" });
            npc.AddPart(new ConversationPart { ConversationID = "" });

            var actions = new InventoryActionList();
            var e = GameEvent.New("GetInventoryActions");
            e.SetParameter("Actions", actions);
            npc.FireEvent(e);

            InventoryAction chat = null;
            foreach (var a in actions.Actions)
                if (a.Command == "Chat") chat = a;

            Assert.IsNotNull(chat, "ConversationPart should add a Chat action.");
            Assert.AreEqual("Chat", chat.Name);
            Assert.AreEqual("chat", chat.Display);
            Assert.AreEqual('c', chat.Key);
            Assert.AreEqual(10, chat.Priority,
                "Chat should rank above Examine (0) but below Open (30).");
        }

        [Test]
        public void EntityWithoutConversationPart_DoesNotDeclareChat()
        {
            // Sanity: a Snapjaw (hostile creature, no ConversationPart) should
            // not surface a Chat action.
            var snapjaw = _factory.CreateEntity("Snapjaw");

            var actions = new InventoryActionList();
            var e = GameEvent.New("GetInventoryActions");
            e.SetParameter("Actions", actions);
            snapjaw.FireEvent(e);

            foreach (var a in actions.Actions)
                Assert.AreNotEqual("Chat", a.Command,
                    "Snapjaw lacks ConversationPart so Chat should not appear.");
        }

        // ==========================================================
        // Chat command execution
        // ==========================================================

        [Test]
        public void ChatCommand_WithValidConversationID_StartsConversation()
        {
            // Scribe has ConversationID="Scribe_1" per the blueprint, and
            // Scribe_1 is a real loaded conversation in the game content.
            // Firing Chat on the blueprint-loaded Scribe should start it.
            var scribe = _factory.CreateEntity("Scribe");
            var player = new Entity { BlueprintName = "Player" };
            player.Tags["Player"] = "";
            player.Tags["Faction"] = "Player"; // non-hostile to Villagers

            Assert.IsFalse(ConversationManager.IsActive,
                "Pre-condition: no conversation active.");

            scribe.FireEvent(BuildChatCommand(player));

            Assert.IsTrue(ConversationManager.IsActive,
                "Chat command on a Scribe should start a conversation.");
            Assert.AreSame(scribe, ConversationManager.Speaker);
            Assert.AreSame(player, ConversationManager.Listener);
        }

        [Test]
        public void ChatCommand_WithEmptyConversationID_LogsHiFallback()
        {
            // An NPC with ConversationPart but no ConversationID — should fall
            // back to a "Hi." greeting instead of silently failing.
            var npc = new Entity { BlueprintName = "Mute NPC" };
            npc.AddPart(new RenderPart { DisplayName = "farmhand" });
            npc.AddPart(new ConversationPart { ConversationID = "" });

            var player = new Entity { BlueprintName = "Player" };
            player.Tags["Player"] = "";

            npc.FireEvent(BuildChatCommand(player));

            Assert.IsFalse(ConversationManager.IsActive,
                "StartConversation should fail when ConversationID is empty.");
            Assert.That(MessageLog.GetMessages(),
                Does.Contain("farmhand says, \"Hi.\""),
                "Fallback 'Hi.' greeting should log when no real dialogue exists.");
        }

        [Test]
        public void ChatCommand_WithUnknownConversationID_LogsHiFallback()
        {
            // ConversationID set but doesn't resolve to loaded data.
            var npc = new Entity { BlueprintName = "Mysterious NPC" };
            npc.AddPart(new RenderPart { DisplayName = "stranger" });
            npc.AddPart(new ConversationPart { ConversationID = "NotARealConversation" });

            var player = new Entity { BlueprintName = "Player" };
            player.Tags["Player"] = "";

            npc.FireEvent(BuildChatCommand(player));

            Assert.IsFalse(ConversationManager.IsActive);
            Assert.That(MessageLog.GetMessages(),
                Does.Contain("stranger says, \"Hi.\""));
        }

        [Test]
        public void ChatCommand_WithHostileNPC_DoesNotLogHiFallback()
        {
            // Hostile NPC — StartConversation logs "refuses to speak" itself.
            // Our fallback must NOT also fire or we get double-message.
            // Use two faction-tagged entities to get hostile resolution.
            var npc = new Entity { BlueprintName = "Hostile NPC" };
            npc.Tags["Creature"] = "";
            npc.Tags["Faction"] = "Snapjaws"; // hostile to Villagers/Player
            npc.AddPart(new RenderPart { DisplayName = "brute" });
            npc.AddPart(new ConversationPart { ConversationID = "" });

            var player = new Entity { BlueprintName = "Player" };
            player.Tags["Player"] = "";
            player.Tags["Creature"] = "";
            player.Tags["Faction"] = "Villagers";

            Assume.That(FactionManager.IsHostile(npc, player), Is.True,
                "Pre-condition: Snapjaws vs Villagers should be hostile.");

            npc.FireEvent(BuildChatCommand(player));

            Assert.IsFalse(ConversationManager.IsActive);
            foreach (var msg in MessageLog.GetMessages())
                StringAssert.DoesNotContain("says, \"Hi.\"", msg,
                    "Hostile NPCs should not fire the 'Hi.' fallback — " +
                    "StartConversation already logged 'refuses to speak'.");
        }

        [Test]
        public void ChatCommand_WithNullActor_IsNoOp()
        {
            var npc = new Entity { BlueprintName = "NPC" };
            npc.AddPart(new RenderPart { DisplayName = "someone" });
            npc.AddPart(new ConversationPart { ConversationID = "" });

            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "Chat");
            // Deliberately don't set Actor.
            Assert.DoesNotThrow(() => npc.FireEvent(e));
            Assert.IsFalse(ConversationManager.IsActive);
        }

        // ==========================================================
        // Blueprint integration — conversational NPCs surface Chat
        // ==========================================================

        [TestCase("Scribe")]
        [TestCase("Warden")]
        [TestCase("Tinker")]
        [TestCase("Merchant")]
        [TestCase("Farmer")]
        public void Blueprint_ConversationalNPCs_SurfaceChatAction(string blueprintName)
        {
            var npc = _factory.CreateEntity(blueprintName);
            Assert.IsNotNull(npc, $"Blueprint '{blueprintName}' should resolve.");
            Assert.IsNotNull(npc.GetPart<ConversationPart>(),
                $"{blueprintName} should have ConversationPart.");

            var actions = new InventoryActionList();
            var e = GameEvent.New("GetInventoryActions");
            e.SetParameter("Actions", actions);
            npc.FireEvent(e);

            bool foundChat = false;
            bool foundExamine = false;
            foreach (var a in actions.Actions)
            {
                if (a.Command == "Chat") foundChat = true;
                if (a.Command == "Examine") foundExamine = true;
            }

            Assert.IsTrue(foundChat, $"{blueprintName} should declare Chat.");
            Assert.IsTrue(foundExamine,
                $"{blueprintName} should also declare Examine (cascaded from PhysicalObject).");
        }

        // ==========================================================
        // Helpers
        // ==========================================================

        private static GameEvent BuildChatCommand(Entity actor)
        {
            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "Chat");
            e.SetParameter("Actor", (object)actor);
            return e;
        }
    }
}
