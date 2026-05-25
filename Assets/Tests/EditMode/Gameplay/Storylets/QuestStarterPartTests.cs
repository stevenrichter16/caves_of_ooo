using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Q5.3 (Docs/QUEST-WORLD-PARTS.md) — QuestStarter: auto-starts a quest
    /// when the PLAYER takes the item carrying this Part (the M1 "Taken"
    /// event). CoO port of Qud's <c>XRL.World.Parts.QuestStarter</c> (Taken
    /// trigger only — zone-presence triggers Created/Seen/OnScreen are
    /// deferred by the Taken-only scope).
    ///
    /// Qud→CoO mapping:
    /// - Qud <c>IfFinishedQuestStep</c> gate → CoO <see cref="QuestStarter.IfQuestCompleted"/>
    ///   (CoO tracks completed QUESTS permanently, not finished objectives).
    /// - Qud <c>Activated</c> + self-removal → an <see cref="QuestStarter.Activated"/>
    ///   flag (fires once ever; avoids removing a Part mid event-dispatch).
    /// - Player gate (Qud <c>IsPlayer()</c>) → taker == LocalPlayer.
    /// </summary>
    public class QuestStarterPartTests
    {
        [SetUp]
        public void SetUp()
        {
            StoryletRegistry.Reset();
            Diag.ResetAll();
            StoryletPart.Current = null;
            StoryletPart.LocalPlayer = null;
        }

        [TearDown]
        public void TearDown()
        {
            StoryletRegistry.Reset();
            StoryletPart.Current = null;
            StoryletPart.LocalPlayer = null;
        }

        /// <summary>Register a quest (so StartQuest has a definition) and
        /// install a StoryletPart as Current. Does NOT start it.</summary>
        private static StoryletPart Setup(params string[] questIds)
        {
            foreach (var q in questIds)
            {
                var sd = new StoryletData { ID = q, Quest = new QuestData() };
                sd.Quest.Stages.Add(new QuestStageData { ID = "s0" });
                sd.Quest.Stages.Add(new QuestStageData { ID = "s1" });
                StoryletRegistry.Register(sd);
            }
            var part = new StoryletPart();
            StoryletPart.Current = part;
            return part;
        }

        private static Entity MakePlayer()
        {
            var p = new Entity { ID = "player", BlueprintName = "Player" };
            StoryletPart.LocalPlayer = p;
            return p;
        }

        private static Entity MakeStarterItem(string quest, string ifQuestCompleted = null)
        {
            var e = new Entity { ID = "scroll", BlueprintName = "QuestScroll" };
            e.AddPart(new QuestStarter { Quest = quest, IfQuestCompleted = ifQuestCompleted });
            return e;
        }

        private static void FireTaken(Entity item, Entity taker)
        {
            var taken = GameEvent.New("Taken");
            taken.SetParameter("Actor", (object)taker);
            taken.SetParameter("Item", (object)item);
            item.FireEventAndRelease(taken);
        }

        [Test]
        public void Taken_ByPlayer_StartsQuest()
        {
            var part = Setup("Q");
            var player = MakePlayer();
            var item = MakeStarterItem("Q");
            FireTaken(item, player);
            Assert.IsTrue(part.IsQuestActive("Q"),
                "the player taking the item starts the quest");
        }

        [Test]
        public void Taken_ByNonPlayer_DoesNotStart()
        {
            var part = Setup("Q");
            MakePlayer();
            var npc = new Entity { ID = "npc", BlueprintName = "Snapjaw" };
            var item = MakeStarterItem("Q");
            FireTaken(item, npc);
            Assert.IsFalse(part.IsQuestActive("Q"),
                "a non-player taker must not start the quest");
        }

        [Test]
        public void Taken_NullActor_DoesNotStart()
        {
            var part = Setup("Q");
            MakePlayer();
            var item = MakeStarterItem("Q");
            FireTaken(item, null);
            Assert.IsFalse(part.IsQuestActive("Q"));
        }

        [Test]
        public void Taken_NoLocalPlayer_DoesNotStart()
        {
            var part = Setup("Q");
            StoryletPart.LocalPlayer = null;
            var taker = new Entity { ID = "someone" };
            var item = MakeStarterItem("Q");
            FireTaken(item, taker);
            Assert.IsFalse(part.IsQuestActive("Q"));
        }

        [Test]
        public void Activated_FiresOnceEver_NoRestartAfterFail()
        {
            // Qud parity: a QuestStarter fires ONCE. Without the Activated
            // guard, re-taking the item after FAILING the quest would restart
            // it (StartQuest re-activates a failed quest). The guard prevents
            // that — the starter is spent after the first activation.
            var part = Setup("Q");
            var player = MakePlayer();
            var item = MakeStarterItem("Q");

            FireTaken(item, player);
            Assert.IsTrue(part.IsQuestActive("Q"), "first take starts the quest");

            part.FailQuest("Q");
            Assert.IsTrue(part.IsQuestFailed("Q"));

            FireTaken(item, player); // re-take after fail
            Assert.IsFalse(part.IsQuestActive("Q"),
                "the spent starter must NOT restart the quest after a fail");
            Assert.IsTrue(part.IsQuestFailed("Q"),
                "the quest stays failed — re-take did not clear it");
        }

        [Test]
        public void Taken_IfQuestCompleted_PrereqIncomplete_DoesNotStart()
        {
            // Gate: only start Q if the prerequisite quest "Pre" is completed.
            var part = Setup("Q", "Pre");
            var player = MakePlayer();
            var item = MakeStarterItem("Q", ifQuestCompleted: "Pre");
            FireTaken(item, player);
            Assert.IsFalse(part.IsQuestActive("Q"),
                "gate unmet (Pre not completed) → Q does not start");
        }

        [Test]
        public void Taken_IfQuestCompleted_PrereqComplete_Starts()
        {
            var part = Setup("Q", "Pre");
            var player = MakePlayer();
            part.StartQuest(new QuestState { QuestId = "Pre" });
            part.CompleteQuest("Pre");
            Assert.IsTrue(part.IsQuestCompleted("Pre"));

            var item = MakeStarterItem("Q", ifQuestCompleted: "Pre");
            FireTaken(item, player);
            Assert.IsTrue(part.IsQuestActive("Q"),
                "gate met (Pre completed) → Q starts");
        }

        [Test]
        public void Taken_EmptyQuestField_NoOpNoThrow()
        {
            Setup();
            var player = MakePlayer();
            var item = new Entity { ID = "scroll" };
            item.AddPart(new QuestStarter { Quest = "" });
            Assert.DoesNotThrow(() => FireTaken(item, player));
        }

        [Test]
        public void NonTakenEvent_Ignored()
        {
            var part = Setup("Q");
            var player = MakePlayer();
            var item = MakeStarterItem("Q");
            item.FireEventAndRelease(GameEvent.New("AfterPickup"));
            Assert.IsFalse(part.IsQuestActive("Q"));
        }
    }
}
