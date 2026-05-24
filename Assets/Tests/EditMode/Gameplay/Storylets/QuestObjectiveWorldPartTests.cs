using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Q5.1 (Docs/QUEST-WORLD-PARTS.md) — FinishObjectiveWhenSlain: a quest
    /// objective finishes when the entity carrying the Part is slain (the
    /// "Died" event). The world-side complement to the FinishObjective
    /// conversation action ("kill X to advance").
    /// </summary>
    public class QuestObjectiveWorldPartTests
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

        private static Entity MakeMob(string quest, string objective)
        {
            var e = new Entity { ID = "mob", BlueprintName = "Snapjaw" };
            e.AddPart(new FinishObjectiveWhenSlain { Quest = quest, Objective = objective });
            return e;
        }

        private static void FireDied(Entity e, Entity killer = null)
        {
            var died = GameEvent.New("Died");
            died.SetParameter("Killer", (object)killer);
            died.SetParameter("Target", (object)e);
            e.FireEventAndRelease(died);
        }

        [Test]
        public void Slain_FinishesItsObjective()
        {
            var part = SetupQuest("Q", "kill_boss", "other"); // 2 required
            var mob = MakeMob("Q", "kill_boss");
            FireDied(mob);
            Assert.IsTrue(part.IsObjectiveFinished("Q", "kill_boss"),
                "slaying the entity finishes its objective");
            Assert.IsFalse(part.IsObjectiveFinished("Q", "other"));
        }

        [Test]
        public void Slain_LastRequired_AdvancesStage()
        {
            var part = SetupQuest("Q", "kill_boss"); // single required
            var killer = new Entity { ID = "player" };
            StoryletPart.LocalPlayer = killer;
            var mob = MakeMob("Q", "kill_boss");
            FireDied(mob, killer);
            Assert.AreEqual(1, part.GetQuestState("Q").CurrentStageIndex,
                "slaying the last required objective's target advances the stage");
        }

        [Test]
        public void Slain_NoActiveQuest_NoOpNoThrow()
        {
            StoryletPart.Current = new StoryletPart(); // nothing started
            var mob = MakeMob("Q", "kill_boss");
            Assert.DoesNotThrow(() => FireDied(mob));
        }

        [Test]
        public void Slain_ObjectiveNotInCurrentStage_NoOp()
        {
            var part = SetupQuest("Q", "kill_boss");
            var mob = MakeMob("Q", "not_an_objective");
            FireDied(mob);
            Assert.IsFalse(part.IsObjectiveFinished("Q", "not_an_objective"));
            Assert.IsFalse(part.IsObjectiveFinished("Q", "kill_boss"));
        }

        [Test]
        public void Slain_EmptyQuestOrObjectiveFields_NoOpNoThrow()
        {
            SetupQuest("Q", "kill_boss");
            var mob = new Entity { ID = "mob" };
            mob.AddPart(new FinishObjectiveWhenSlain { Quest = "", Objective = "" });
            Assert.DoesNotThrow(() => FireDied(mob));
        }

        [Test]
        public void NonDiedEvent_Ignored()
        {
            var part = SetupQuest("Q", "kill_boss");
            var mob = MakeMob("Q", "kill_boss");
            // A non-Died event must not finish the objective.
            mob.FireEventAndRelease(GameEvent.New("BeforeTakeDamage"));
            Assert.IsFalse(part.IsObjectiveFinished("Q", "kill_boss"));
        }
    }
}
