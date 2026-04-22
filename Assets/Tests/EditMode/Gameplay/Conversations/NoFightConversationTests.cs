using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M2.1 — verifies the <c>PushNoFightGoal</c> dialogue action registered
    /// in <see cref="ConversationActions"/>. Covers argument parsing,
    /// idempotency, the no-BrainPart safety branch, and wiring via the
    /// <c>ExecuteAll</c> entry point that real dialogue choices use.
    ///
    /// Note: M2.1's consolidated plan also called for a
    /// <c>ConversationManager.StartConversation</c> auto-pacify. During
    /// implementation we found <see cref="BrainPart.HandleTakeTurn"/>
    /// already early-returns on <c>InConversation == true</c> (BrainPart.cs:231)
    /// and on the Player tag (line 234), making auto-pacify functionally
    /// redundant — and the idempotency guard here would silently suppress
    /// the dialogue-chosen pacification if auto-pacify pushed first. Scope
    /// pruned accordingly; see the M2.1 commit message for rationale.
    /// </summary>
    public class NoFightConversationTests
    {
        [SetUp]
        public void Setup()
        {
            FactionManager.Initialize();
            ConversationActions.Reset();   // forces RegisterDefaults on next Execute
            MessageLog.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            FactionManager.Reset();
        }

        [Test]
        public void PushNoFightGoal_WithValidDuration_PushesWithParsedDuration()
        {
            var speaker = CreateNPCWithBrain();
            var listener = CreatePlayerListener();

            ConversationActions.Execute("PushNoFightGoal", speaker, listener, "200");

            var brain = speaker.GetPart<BrainPart>();
            Assert.IsTrue(brain.HasGoal<NoFightGoal>(),
                "PushNoFightGoal should push a NoFightGoal onto the speaker's brain.");
            var goal = brain.FindGoal<NoFightGoal>();
            Assert.AreEqual(200, goal.Duration,
                "Duration must match the parsed argument.");
            Assert.IsFalse(goal.Wander,
                "Pacification goal should idle in place (wander=false), not wander randomly.");
        }

        [Test]
        public void PushNoFightGoal_WithEmptyArg_DefaultsTo100()
        {
            var speaker = CreateNPCWithBrain();
            var listener = CreatePlayerListener();

            ConversationActions.Execute("PushNoFightGoal", speaker, listener, "");

            var goal = speaker.GetPart<BrainPart>().FindGoal<NoFightGoal>();
            Assert.IsNotNull(goal, "Goal should be pushed even with empty argument.");
            Assert.AreEqual(100, goal.Duration, "Empty arg should fall back to the 100-turn default.");
        }

        [Test]
        public void PushNoFightGoal_WithInvalidArg_DefaultsTo100()
        {
            var speaker = CreateNPCWithBrain();
            var listener = CreatePlayerListener();

            ConversationActions.Execute("PushNoFightGoal", speaker, listener, "notanumber");

            var goal = speaker.GetPart<BrainPart>().FindGoal<NoFightGoal>();
            Assert.IsNotNull(goal, "Goal should be pushed even with unparseable argument.");
            Assert.AreEqual(100, goal.Duration, "Invalid arg should fall back to the 100-turn default.");
        }

        [Test]
        public void PushNoFightGoal_Idempotent_DoesNotStackIfAlreadyPresent()
        {
            var speaker = CreateNPCWithBrain();
            var listener = CreatePlayerListener();
            var brain = speaker.GetPart<BrainPart>();

            // Pre-push with a short duration so we can detect extension/replacement.
            brain.PushGoal(new NoFightGoal(duration: 50, wander: false));
            int countBefore = brain.GoalCount;

            // The dialogue action tries to push Duration=999. Must be a no-op.
            ConversationActions.Execute("PushNoFightGoal", speaker, listener, "999");

            Assert.AreEqual(countBefore, brain.GoalCount,
                "Idempotent: must not stack a second NoFightGoal.");
            var goal = brain.FindGoal<NoFightGoal>();
            Assert.IsNotNull(goal);
            Assert.AreEqual(50, goal.Duration,
                "Existing NoFightGoal must not have its duration replaced or extended by a second call.");
        }

        [Test]
        public void PushNoFightGoal_SpeakerWithoutBrain_IsNoOp()
        {
            // Safety branch: a speaker entity that somehow lacks a BrainPart
            // (a non-creature quest-giver prop?) must not throw when the
            // action fires. The action should quietly return.
            var speakerNoBrain = new Entity { BlueprintName = "PropNPC" };
            speakerNoBrain.AddPart(new RenderPart { DisplayName = "prop" });
            var listener = CreatePlayerListener();

            Assert.DoesNotThrow(() =>
                ConversationActions.Execute("PushNoFightGoal", speakerNoBrain, listener, "100"),
                "Action must handle a brainless speaker gracefully.");
        }

        [Test]
        public void PushNoFightGoal_NullSpeaker_IsNoOp()
        {
            // Defensive: ConversationActions can be invoked with a null
            // speaker in corner cases (no live ConversationManager state).
            // Must not throw.
            var listener = CreatePlayerListener();
            Assert.DoesNotThrow(() =>
                ConversationActions.Execute("PushNoFightGoal", null, listener, "100"),
                "Null speaker must be handled without exception.");
        }

        [Test]
        public void PushNoFightGoal_ViaChoiceActionsList_WiresThroughExecuteAll()
        {
            // Real dialogue choices fire via ExecuteAll(List<ConversationParam>, ...).
            // Pin that wiring end-to-end so a dialogue JSON entry of the form
            //   "Actions": [{ "Key": "PushNoFightGoal", "Value": "200" }]
            // actually reaches the handler we registered.
            var speaker = CreateNPCWithBrain();
            var listener = CreatePlayerListener();

            var actions = new List<ConversationParam>
            {
                new ConversationParam { Key = "PushNoFightGoal", Value = "200" }
            };

            ConversationActions.ExecuteAll(actions, speaker, listener);

            var goal = speaker.GetPart<BrainPart>().FindGoal<NoFightGoal>();
            Assert.IsNotNull(goal,
                "ExecuteAll should fan out to the same handler Register installed.");
            Assert.AreEqual(200, goal.Duration);
        }

        [Test]
        public void PushNoFightGoal_PushedOnSpeakerNotListener()
        {
            // Speaker is the NPC, listener is the player. The action must
            // pacify the NPC (so their combat acquisition is suppressed),
            // NOT the player (whose combat routes through input, not the
            // goal stack). Asymmetry matters — pinning it explicitly.
            var speaker = CreateNPCWithBrain();
            var listener = CreatePlayerListener(); // player has no BrainPart in this test harness
            // Give the listener a brain so we can prove PushNoFightGoal still targets speaker only.
            listener.AddPart(new BrainPart());

            ConversationActions.Execute("PushNoFightGoal", speaker, listener, "200");

            Assert.IsTrue(speaker.GetPart<BrainPart>().HasGoal<NoFightGoal>(),
                "Speaker (the NPC) must receive the pacification.");
            Assert.IsFalse(listener.GetPart<BrainPart>().HasGoal<NoFightGoal>(),
                "Listener (the player) must NOT be pacified by a PushNoFightGoal action.");
        }

        // ===== Helpers (mirror ConversationTests.cs conventions) =====

        private static Entity CreateNPCWithBrain(string faction = "Villagers")
        {
            var entity = new Entity { BlueprintName = "TestNPC" };
            entity.Tags["Creature"] = "";
            if (!string.IsNullOrEmpty(faction))
                entity.Tags["Faction"] = faction;
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 10 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = "Test NPC" });
            entity.AddPart(new BrainPart());
            return entity;
        }

        private static Entity CreatePlayerListener()
        {
            var entity = new Entity { BlueprintName = "Player" };
            entity.Tags["Creature"] = "";
            entity.Tags["Player"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = "you" });
            entity.AddPart(new InventoryPart());
            return entity;
        }
    }
}
