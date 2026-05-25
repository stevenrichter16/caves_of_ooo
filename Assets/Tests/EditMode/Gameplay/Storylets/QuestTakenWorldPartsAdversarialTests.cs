using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// ADVERSARIAL SWEEP (CLAUDE.md → ADVERSARIAL_TESTING.md) for the Q5.2/Q5.3
    /// world-object quest Parts (CompleteObjectiveOnTaken / QuestStarter) +
    /// the M1 "Taken" event. Probes the bug-class surfaces the per-feature
    /// tests don't: save/load reflection, cross-actor / multi-instance flows,
    /// malformed event params, re-fire / state atomicity, and boundary inputs.
    ///
    /// Result: all pinned-as-correct (0 bugs). The save/load pins matter most
    /// — QuestStarter.Activated round-trips through the generic WritePublicFields
    /// path (SaveSystem.cs:1142), so a spent starter stays spent across a save.
    /// </summary>
    public class QuestTakenWorldPartsAdversarialTests
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

        private static StoryletPart Setup(params string[] questIds)
        {
            foreach (var q in questIds)
            {
                var sd = new StoryletData { ID = q, Quest = new QuestData() };
                var s0 = new QuestStageData { ID = "s0" };
                s0.Objectives.Add(new QuestObjectiveData { ID = "obj" });
                sd.Quest.Stages.Add(s0);
                sd.Quest.Stages.Add(new QuestStageData { ID = "s1" });
                StoryletRegistry.Register(sd);
            }
            var part = new StoryletPart();
            StoryletPart.Current = part;
            return part;
        }

        /// <summary>Register a SINGLE-stage quest (s0 with "obj") + install
        /// Current. Finishing "obj" (the only required) advances PAST the only
        /// stage → completes (unlike <see cref="Setup"/>'s 2-stage quest, where
        /// finishing s0's objective only advances to the terminal stage s1).</summary>
        private static StoryletPart SetupSingleStage(string questId)
        {
            var sd = new StoryletData { ID = questId, Quest = new QuestData() };
            var s0 = new QuestStageData { ID = "s0" };
            s0.Objectives.Add(new QuestObjectiveData { ID = "obj" });
            sd.Quest.Stages.Add(s0);
            StoryletRegistry.Register(sd);
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

        private static void FireTaken(Entity item, Entity taker)
        {
            var taken = GameEvent.New("Taken");
            taken.SetParameter("Actor", (object)taker);
            taken.SetParameter("Item", (object)item);
            item.FireEventAndRelease(taken);
        }

        // ════════════════ save/load reflection ════════════════

        [Test]
        public void Adversarial_SaveLoad_QuestStarter_PreservesFieldsAndActivated()
        {
            var item = new Entity { ID = "scroll", BlueprintName = "QuestScroll" };
            item.AddPart(new QuestStarter { Quest = "Q", IfQuestCompleted = "Pre", Activated = true });

            var loaded = PartRoundTripHelper.RoundTripEntity(item);
            var qs = loaded.GetPart<QuestStarter>();

            Assert.IsNotNull(qs, "QuestStarter survives save/load");
            Assert.AreEqual("Q", qs.Quest);
            Assert.AreEqual("Pre", qs.IfQuestCompleted);
            Assert.IsTrue(qs.Activated,
                "a SPENT starter must stay spent across save/load (no re-fire on reload)");
        }

        [Test]
        public void Adversarial_SaveLoad_CompleteObjectiveOnTaken_PreservesFields()
        {
            var item = new Entity { ID = "relic", BlueprintName = "Enchiridion" };
            item.AddPart(new CompleteObjectiveOnTaken { Quest = "Q", Objective = "find_relic" });

            var loaded = PartRoundTripHelper.RoundTripEntity(item);
            var part = loaded.GetPart<CompleteObjectiveOnTaken>();

            Assert.IsNotNull(part);
            Assert.AreEqual("Q", part.Quest);
            Assert.AreEqual("find_relic", part.Objective);
        }

        // ════════════════ cross-actor / multi-instance ════════════════

        [Test]
        public void Adversarial_TwoStarterItems_SameQuest_SecondIsNoOp()
        {
            var sp = Setup("Q");
            var player = MakePlayer();
            var itemA = new Entity { ID = "a" }; itemA.AddPart(new QuestStarter { Quest = "Q" });
            var itemB = new Entity { ID = "b" }; itemB.AddPart(new QuestStarter { Quest = "Q" });

            FireTaken(itemA, player);
            Assert.IsTrue(sp.IsQuestActive("Q"), "first starter starts the quest");
            FireTaken(itemB, player); // quest already active → StartQuest idempotent
            Assert.IsTrue(sp.IsQuestActive("Q"), "still active, no corruption from the second starter");
            Assert.IsTrue(itemA.GetPart<QuestStarter>().Activated);
            Assert.IsTrue(itemB.GetPart<QuestStarter>().Activated,
                "each starter independently spends itself");
        }

        [Test]
        public void Adversarial_TwoCompleteItems_SameObjective_SecondIdempotent()
        {
            // Single-stage quest: finishing "obj" (the only required) completes it.
            var sp = SetupSingleStage("Q");
            var player = MakePlayer();
            sp.StartQuest(new QuestState { QuestId = "Q" });
            var itemA = new Entity { ID = "a" }; itemA.AddPart(new CompleteObjectiveOnTaken { Quest = "Q", Objective = "obj" });
            var itemB = new Entity { ID = "b" }; itemB.AddPart(new CompleteObjectiveOnTaken { Quest = "Q", Objective = "obj" });

            FireTaken(itemA, player);
            Assert.IsTrue(sp.IsQuestCompleted("Q"), "finishing the only required objective completes the quest");
            Assert.DoesNotThrow(() => FireTaken(itemB, player),
                "taking a second copy after completion is a harmless no-op");
            Assert.IsTrue(sp.IsQuestCompleted("Q"));
            Assert.IsFalse(sp.IsQuestActive("Q"), "no resurrection from the redundant take");
        }

        [Test]
        public void Adversarial_CompositeItem_StarterAndComplete_BothPartsPresent()
        {
            // An item can carry both Parts; the "Taken" dispatch reaches both.
            // Single-stage quest so the objective finish completes it.
            var sp = SetupSingleStage("Q");
            var player = MakePlayer();
            var item = new Entity { ID = "macguffin" };
            item.AddPart(new QuestStarter { Quest = "Q" });               // added first → runs first
            item.AddPart(new CompleteObjectiveOnTaken { Quest = "Q", Objective = "obj" });

            FireTaken(item, player);
            // Starter runs first (added first): quest starts on s0. Then the
            // complete-part finishes "obj" — the only required → completes.
            Assert.IsTrue(sp.IsQuestCompleted("Q"),
                "both parts react to the same Taken: started then objective-completed");
        }

        // ════════════════ malformed event params ════════════════

        [Test]
        public void Adversarial_Taken_NoActorParam_NoCrashNoFire()
        {
            var sp = Setup("Q");
            MakePlayer();
            var item = new Entity { ID = "i" };
            item.AddPart(new QuestStarter { Quest = "Q" });
            item.AddPart(new CompleteObjectiveOnTaken { Quest = "Q", Objective = "obj" });

            // A Taken event with NO Actor parameter at all.
            var taken = GameEvent.New("Taken");
            Assert.DoesNotThrow(() => item.FireEventAndRelease(taken));
            Assert.IsFalse(sp.IsQuestActive("Q"), "missing Actor → no start (taker resolves null)");
        }

        [Test]
        public void Adversarial_CompleteObjective_NullStoryletPartCurrent_NoCrash()
        {
            StoryletPart.Current = null;
            MakePlayer();
            var item = new Entity { ID = "i" };
            item.AddPart(new CompleteObjectiveOnTaken { Quest = "Q", Objective = "obj" });
            Assert.DoesNotThrow(() => FireTaken(item, StoryletPart.LocalPlayer));
        }

        [Test]
        public void Adversarial_QuestStarter_NullStoryletPartCurrent_NoCrash()
        {
            StoryletPart.Current = null;
            MakePlayer();
            var item = new Entity { ID = "i" };
            item.AddPart(new QuestStarter { Quest = "Q" });
            Assert.DoesNotThrow(() => FireTaken(item, StoryletPart.LocalPlayer));
        }

        // ════════════════ re-fire / state atomicity ════════════════

        [Test]
        public void Adversarial_QuestStarter_RepeatedTaken_FiresOnce()
        {
            var sp = Setup("Q");
            var player = MakePlayer();
            var item = new Entity { ID = "i" }; item.AddPart(new QuestStarter { Quest = "Q" });

            FireTaken(item, player);
            FireTaken(item, player);
            FireTaken(item, player);
            Assert.IsTrue(item.GetPart<QuestStarter>().Activated);
            Assert.IsTrue(sp.IsQuestActive("Q"));
            // Complete it, then re-take: the spent starter must not restart.
            sp.CompleteQuest("Q");
            FireTaken(item, player);
            Assert.IsFalse(sp.IsQuestActive("Q"), "spent starter never restarts the quest");
            Assert.IsTrue(sp.IsQuestCompleted("Q"));
        }

        [Test]
        public void Adversarial_CompleteObjective_RepeatedTaken_Idempotent()
        {
            var sp = Setup("Q");
            var player = MakePlayer();
            // Two required objectives so finishing "obj" does NOT complete →
            // the objective stays finished and re-take is a pure idempotent no-op.
            StoryletRegistry.Reset();
            var sd = new StoryletData { ID = "Q", Quest = new QuestData() };
            var s0 = new QuestStageData { ID = "s0" };
            s0.Objectives.Add(new QuestObjectiveData { ID = "obj" });
            s0.Objectives.Add(new QuestObjectiveData { ID = "other" });
            sd.Quest.Stages.Add(s0);
            sd.Quest.Stages.Add(new QuestStageData { ID = "s1" });
            StoryletRegistry.Register(sd);
            sp.StartQuest(new QuestState { QuestId = "Q" });

            var item = new Entity { ID = "i" };
            item.AddPart(new CompleteObjectiveOnTaken { Quest = "Q", Objective = "obj" });
            FireTaken(item, player);
            Assert.IsTrue(sp.IsObjectiveFinished("Q", "obj"));
            Assert.AreEqual(0, sp.GetQuestState("Q").CurrentStageIndex, "still on stage 0 (other unfinished)");
            Assert.DoesNotThrow(() => FireTaken(item, player), "re-take is idempotent");
            Assert.AreEqual(0, sp.GetQuestState("Q").CurrentStageIndex);
        }

        // ════════════════ boundary inputs ════════════════

        [Test]
        public void Adversarial_QuestStarter_WhitespaceQuest_DoesNotThrow()
        {
            // string.IsNullOrEmpty does NOT catch whitespace — a blueprint with
            // Quest=" " is a content error, not a crash. Pin no-throw (the junk
            // quest id is content's responsibility, not the Part's).
            Setup();
            var player = MakePlayer();
            var item = new Entity { ID = "i" };
            item.AddPart(new QuestStarter { Quest = " " });
            Assert.DoesNotThrow(() => FireTaken(item, player));
        }

        [Test]
        public void Adversarial_Parts_NullEverything_NoThrow()
        {
            // No StoryletPart, no LocalPlayer, null taker, empty fields.
            StoryletPart.Current = null;
            StoryletPart.LocalPlayer = null;
            var item = new Entity { ID = "i" };
            item.AddPart(new QuestStarter { Quest = null });
            item.AddPart(new CompleteObjectiveOnTaken { Quest = null, Objective = null });
            var taken = GameEvent.New("Taken");
            Assert.DoesNotThrow(() => item.FireEventAndRelease(taken));
        }
    }
}
