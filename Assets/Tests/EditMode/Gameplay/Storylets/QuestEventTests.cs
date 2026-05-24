using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Q4.1 (Docs/QUEST-EVENTS.md) — quest lifecycle GameEvents. Verifies
    /// StoryletPart fires QuestStarted / QuestObjectiveFinished /
    /// QuestStageAdvanced / QuestCompleted / QuestFailed on LocalPlayer with
    /// the right params, via a capturing Part installed as LocalPlayer.
    /// Counter-checks: no LocalPlayer → no throw + diag still fires; no-op
    /// lifecycle paths fire no event.
    /// </summary>
    public class QuestEventTests
    {
        /// <summary>Captures quest GameEvents fired on its entity.</summary>
        private class QuestEventCapture : Part
        {
            public override string Name => "QuestEventCapture";
            public readonly List<(string id, string questId, string objId, int from, int to)> Events
                = new List<(string, string, string, int, int)>();

            public override bool WantEvent(int eventID) => true;

            public override bool HandleEvent(GameEvent e)
            {
                switch (e.ID)
                {
                    case "QuestStarted":
                    case "QuestObjectiveFinished":
                    case "QuestStageAdvanced":
                    case "QuestCompleted":
                    case "QuestFailed":
                        Events.Add((e.ID,
                            e.GetStringParameter("QuestId"),
                            e.GetStringParameter("ObjectiveId"),
                            e.GetIntParameter("FromIndex", -1),
                            e.GetIntParameter("ToIndex", -1)));
                        break;
                }
                return true;
            }

            public int Count(string id) => Events.Count(ev => ev.id == id);
            public (string id, string questId, string objId, int from, int to) Last(string id)
                => Events.Last(ev => ev.id == id);
        }

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

        private static QuestObjectiveData Obj(string id, bool optional = false)
            => new QuestObjectiveData { ID = id, Optional = optional };

        private static QuestStageData Stage(string id, params QuestObjectiveData[] objs)
        {
            var s = new QuestStageData { ID = id };
            if (objs != null) s.Objectives.AddRange(objs);
            return s;
        }

        /// <summary>Install a capturing player as LocalPlayer, register the
        /// quest, and return (part, capture). Does NOT start the quest — the
        /// test does, so QuestStarted is observable.</summary>
        private static (StoryletPart part, QuestEventCapture cap) Setup(
            string questId, params QuestStageData[] stages)
        {
            var player = new Entity { ID = "player", BlueprintName = "Player" };
            var cap = new QuestEventCapture();
            player.AddPart(cap);
            StoryletPart.LocalPlayer = player;

            var sd = new StoryletData { ID = questId, Quest = new QuestData() };
            foreach (var s in stages) sd.Quest.Stages.Add(s);
            StoryletRegistry.Register(sd);

            var part = new StoryletPart();
            StoryletPart.Current = part;
            return (part, cap);
        }

        // ════════════════ each event fires with the right params ════════════════

        [Test]
        public void StartQuest_FiresQuestStarted()
        {
            var (part, cap) = Setup("Q", Stage("s0", Obj("a")), Stage("s1"));
            part.StartQuest(new QuestState { QuestId = "Q" });
            Assert.AreEqual(1, cap.Count("QuestStarted"));
            Assert.AreEqual("Q", cap.Last("QuestStarted").questId);
        }

        [Test]
        public void StartQuest_AlreadyActive_DoesNotRefire()
        {
            var (part, cap) = Setup("Q", Stage("s0", Obj("a")), Stage("s1"));
            part.StartQuest(new QuestState { QuestId = "Q" });
            part.StartQuest(new QuestState { QuestId = "Q" }); // re-start
            Assert.AreEqual(1, cap.Count("QuestStarted"),
                "re-StartQuest on an already-active quest must not re-fire");
        }

        [Test]
        public void FinishObjective_FiresObjectiveFinished()
        {
            var (part, cap) = Setup("Q", Stage("s0", Obj("a"), Obj("b")), Stage("s1"));
            part.StartQuest(new QuestState { QuestId = "Q" });
            part.FinishObjective("Q", "a");
            Assert.AreEqual(1, cap.Count("QuestObjectiveFinished"));
            var ev = cap.Last("QuestObjectiveFinished");
            Assert.AreEqual("Q", ev.questId);
            Assert.AreEqual("a", ev.objId);
        }

        [Test]
        public void FinishLastRequired_FiresObjectiveFinished_AndStageAdvanced()
        {
            var (part, cap) = Setup("Q", Stage("s0", Obj("a")), Stage("s1"));
            part.StartQuest(new QuestState { QuestId = "Q" });
            part.FinishObjective("Q", "a"); // last required → advance
            Assert.AreEqual(1, cap.Count("QuestObjectiveFinished"));
            Assert.AreEqual(1, cap.Count("QuestStageAdvanced"));
            var adv = cap.Last("QuestStageAdvanced");
            Assert.AreEqual("Q", adv.questId);
            Assert.AreEqual(0, adv.from);
            Assert.AreEqual(1, adv.to);
        }

        [Test]
        public void CompleteQuest_FiresQuestCompleted()
        {
            var (part, cap) = Setup("Q", Stage("s0", Obj("a")), Stage("s1"));
            part.StartQuest(new QuestState { QuestId = "Q" });
            part.CompleteQuest("Q");
            Assert.AreEqual(1, cap.Count("QuestCompleted"));
            Assert.AreEqual("Q", cap.Last("QuestCompleted").questId);
        }

        [Test]
        public void FailQuest_FiresQuestFailed()
        {
            var (part, cap) = Setup("Q", Stage("s0", Obj("a")), Stage("s1"));
            part.StartQuest(new QuestState { QuestId = "Q" });
            Assert.IsTrue(part.FailQuest("Q"));
            Assert.AreEqual(1, cap.Count("QuestFailed"));
            Assert.AreEqual("Q", cap.Last("QuestFailed").questId);
        }

        // ════════════════ counter-checks ════════════════

        [Test]
        public void NoLocalPlayer_NoThrow_AndDiagStillFires()
        {
            // Null-guard: with no LocalPlayer the events are skipped but the
            // diag record (observability) must still fire and nothing throws.
            var sd = new StoryletData { ID = "Q", Quest = new QuestData() };
            sd.Quest.Stages.Add(Stage("s0", Obj("a")));
            sd.Quest.Stages.Add(Stage("s1"));
            StoryletRegistry.Register(sd);
            var part = new StoryletPart();
            StoryletPart.Current = part;
            StoryletPart.LocalPlayer = null; // explicit

            part.StartQuest(new QuestState { QuestId = "Q" });
            Assert.DoesNotThrow(() => part.CompleteQuest("Q"));

            int completedDiag = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "quest", Kind = "Completed", Limit = 50 }).Records.Count;
            Assert.GreaterOrEqual(completedDiag, 1,
                "quest/Completed diag fires even with no LocalPlayer (event is skipped, diag isn't)");
        }

        [Test]
        public void NoOpFinish_FiresNoObjectiveFinishedEvent()
        {
            var (part, cap) = Setup("Q", Stage("s0", Obj("a"), Obj("b")), Stage("s1"));
            part.StartQuest(new QuestState { QuestId = "Q" });
            part.FinishObjective("Q", "a");
            int after = cap.Count("QuestObjectiveFinished");
            part.FinishObjective("Q", "a");           // already finished → no-op
            part.FinishObjective("Q", "nonexistent");  // not in stage → no-op
            Assert.AreEqual(after, cap.Count("QuestObjectiveFinished"),
                "no-op finishes fire no QuestObjectiveFinished event");
        }
    }
}
