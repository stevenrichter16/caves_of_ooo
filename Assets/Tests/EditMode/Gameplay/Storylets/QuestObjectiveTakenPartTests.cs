using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Q5.2 (Docs/QUEST-WORLD-PARTS.md) — CompleteObjectiveOnTaken: a quest
    /// objective finishes when the PLAYER takes the item carrying this Part
    /// (the M1 "Taken" event). CoO port of Qud's
    /// <c>XRL.World.Parts.CompleteQuestOnTaken</c> (which gates on
    /// <c>Actor.IsPlayer()</c>). The world-side complement for "fetch the
    /// MacGuffin" objectives.
    ///
    /// Key Qud-parity behavior (vs FinishObjectiveWhenSlain, which has NO
    /// actor gate): only the PLAYER taking the item counts — an NPC picking
    /// it up must NOT complete the objective.
    /// </summary>
    public class QuestObjectiveTakenPartTests
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

        private static StoryletPart SetupQuest(string questId, params string[] stage0Objectives)
        {
            var sd = new StoryletData { ID = questId, Quest = new QuestData() };
            var s0 = new QuestStageData { ID = "s0" };
            foreach (var o in stage0Objectives)
                s0.Objectives.Add(new QuestObjectiveData { ID = o });
            sd.Quest.Stages.Add(s0);
            sd.Quest.Stages.Add(new QuestStageData { ID = "s1" });
            StoryletRegistry.Register(sd);
            var part = new StoryletPart();
            part.StartQuest(new QuestState { QuestId = questId, CurrentStageIndex = 0 });
            StoryletPart.Current = part;
            return part;
        }

        private static Entity MakeItem(string quest, string objective)
        {
            var e = new Entity { ID = "relic", BlueprintName = "Enchiridion" };
            e.AddPart(new CompleteObjectiveOnTaken { Quest = quest, Objective = objective });
            return e;
        }

        private static Entity MakePlayer()
        {
            var p = new Entity { ID = "player", BlueprintName = "Player" };
            StoryletPart.LocalPlayer = p;
            return p;
        }

        private static void FireTaken(Entity item, Entity taker)
        {
            var taken = GameEvent.New("Taken");
            taken.SetParameter("Actor", (object)taker);
            taken.SetParameter("Item", (object)item);
            item.FireEventAndRelease(taken);
        }

        [Test]
        public void Taken_ByPlayer_FinishesObjective()
        {
            var part = SetupQuest("Q", "find_relic", "other"); // 2 required
            var player = MakePlayer();
            var item = MakeItem("Q", "find_relic");
            FireTaken(item, player);
            Assert.IsTrue(part.IsObjectiveFinished("Q", "find_relic"),
                "the player taking the item finishes its objective");
            Assert.IsFalse(part.IsObjectiveFinished("Q", "other"));
        }

        [Test]
        public void Taken_ByNonPlayer_DoesNotFinish()
        {
            // Qud-parity gate: an NPC taking the item must NOT complete it.
            var part = SetupQuest("Q", "find_relic");
            MakePlayer(); // a player exists, but the taker below is someone else
            var npc = new Entity { ID = "npc", BlueprintName = "Snapjaw" };
            var item = MakeItem("Q", "find_relic");
            FireTaken(item, npc);
            Assert.IsFalse(part.IsObjectiveFinished("Q", "find_relic"),
                "a non-player taker must not finish the objective");
        }

        [Test]
        public void Taken_NullActor_DoesNotFinish()
        {
            // Defensive counter-check: a null taker is not the player even if
            // LocalPlayer is null — the null==null trap must not complete it.
            var part = SetupQuest("Q", "find_relic");
            MakePlayer();
            var item = MakeItem("Q", "find_relic");
            FireTaken(item, null);
            Assert.IsFalse(part.IsObjectiveFinished("Q", "find_relic"),
                "a null taker must not finish the objective");
        }

        [Test]
        public void Taken_NoLocalPlayer_DoesNotFinish()
        {
            // If LocalPlayer is unset (pre-bootstrap), we cannot confirm the
            // taker is the player → no completion, even for a real taker.
            var part = SetupQuest("Q", "find_relic");
            StoryletPart.LocalPlayer = null;
            var taker = new Entity { ID = "someone" };
            var item = MakeItem("Q", "find_relic");
            FireTaken(item, taker);
            Assert.IsFalse(part.IsObjectiveFinished("Q", "find_relic"),
                "with no LocalPlayer, nothing counts as the player");
        }

        [Test]
        public void Taken_LastRequired_ByPlayer_AdvancesStage()
        {
            var part = SetupQuest("Q", "find_relic"); // single required
            var player = MakePlayer();
            var item = MakeItem("Q", "find_relic");
            FireTaken(item, player);
            Assert.AreEqual(1, part.GetQuestState("Q").CurrentStageIndex,
                "taking the last required objective's item advances the stage");
        }

        [Test]
        public void Taken_NoActiveQuest_NoOpNoThrow()
        {
            StoryletPart.Current = new StoryletPart(); // nothing started
            var player = MakePlayer();
            var item = MakeItem("Q", "find_relic");
            Assert.DoesNotThrow(() => FireTaken(item, player));
        }

        [Test]
        public void Taken_ObjectiveNotInCurrentStage_NoOp()
        {
            var part = SetupQuest("Q", "find_relic");
            var player = MakePlayer();
            var item = MakeItem("Q", "not_an_objective");
            FireTaken(item, player);
            Assert.IsFalse(part.IsObjectiveFinished("Q", "not_an_objective"));
            Assert.IsFalse(part.IsObjectiveFinished("Q", "find_relic"));
        }

        [Test]
        public void Taken_EmptyQuestOrObjectiveFields_NoOpNoThrow()
        {
            SetupQuest("Q", "find_relic");
            var player = MakePlayer();
            var item = new Entity { ID = "relic" };
            item.AddPart(new CompleteObjectiveOnTaken { Quest = "", Objective = "" });
            Assert.DoesNotThrow(() => FireTaken(item, player));
        }

        [Test]
        public void NonTakenEvent_Ignored()
        {
            var part = SetupQuest("Q", "find_relic");
            var player = MakePlayer();
            var item = MakeItem("Q", "find_relic");
            // A non-Taken event must not finish the objective.
            item.FireEventAndRelease(GameEvent.New("AfterPickup"));
            Assert.IsFalse(part.IsObjectiveFinished("Q", "find_relic"));
        }
    }
}
