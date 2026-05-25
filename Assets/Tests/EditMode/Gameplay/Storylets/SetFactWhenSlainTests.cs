using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Q5.4 (Docs/QUEST-IN-WORLD.md) — SetFactWhenSlain: sets a narrative fact
    /// when the entity carrying this Part is slain. The robust, ORDER-INDEPENDENT
    /// kill-detection primitive for WORLD quests: pair it with an
    /// <c>IfFact:&lt;fact&gt;:&gt;=:1</c> objective trigger and the kill completes
    /// the objective whether it happens before OR after the player accepts the
    /// quest — unlike FinishObjectiveWhenSlain, which no-ops if the quest isn't
    /// active yet (pre-accept soft-lock). Mirrors FinishObjectiveWhenSlain
    /// (HandleEvent on "Died", no killer gate — "X is dead, regardless of who
    /// killed it"), but sets a persisted fact instead of finishing an objective.
    /// </summary>
    public class SetFactWhenSlainTests
    {
        [SetUp]
        public void SetUp()
        {
            StoryletRegistry.Reset();
            Diag.ResetAll();
            NarrativeStatePart.Current = null;
            StoryletPart.Current = null;
            StoryletPart.LocalPlayer = null;
        }

        [TearDown]
        public void TearDown()
        {
            StoryletRegistry.Reset();
            NarrativeStatePart.Current = null;
            StoryletPart.Current = null;
            StoryletPart.LocalPlayer = null;
        }

        private static void FireDied(Entity e, Entity killer = null)
        {
            var died = GameEvent.New("Died");
            died.SetParameter("Killer", (object)killer);
            died.SetParameter("Target", (object)e);
            e.FireEventAndRelease(died);
        }

        private static Entity MakeMob(string fact, int value = 1)
        {
            var e = new Entity { ID = "mob", BlueprintName = "SootGremlin" };
            e.AddPart(new SetFactWhenSlain { Fact = fact, Value = value });
            return e;
        }

        // ════════════════ core behavior ════════════════

        [Test]
        public void Slain_SetsTheFact()
        {
            var ns = new NarrativeStatePart();
            NarrativeStatePart.Current = ns;
            var mob = MakeMob("gremlin_slain");
            FireDied(mob);
            Assert.AreEqual(1, ns.GetFact("gremlin_slain"), "slaying the entity sets its fact");
        }

        [Test]
        public void Slain_DefaultValueIsOne()
        {
            var ns = new NarrativeStatePart();
            NarrativeStatePart.Current = ns;
            var e = new Entity { ID = "mob" };
            e.AddPart(new SetFactWhenSlain { Fact = "x" }); // Value omitted
            FireDied(e);
            Assert.AreEqual(1, ns.GetFact("x"), "Value defaults to 1");
        }

        [Test]
        public void Slain_CustomValue()
        {
            var ns = new NarrativeStatePart();
            NarrativeStatePart.Current = ns;
            var e = new Entity { ID = "mob" };
            e.AddPart(new SetFactWhenSlain { Fact = "count", Value = 5 });
            FireDied(e);
            Assert.AreEqual(5, ns.GetFact("count"));
        }

        [Test]
        public void NoKillerGate_SetsRegardlessOfKiller()
        {
            // Parity with FinishObjectiveWhenSlain: "X is dead", regardless of
            // who killed it (player, an NPC, or null/environmental).
            var ns = new NarrativeStatePart();
            NarrativeStatePart.Current = ns;
            var mob = MakeMob("gremlin_slain");
            var someNpc = new Entity { ID = "warden" };
            FireDied(mob, someNpc); // killed by an NPC, not the player
            Assert.AreEqual(1, ns.GetFact("gremlin_slain"),
                "the fact is set even when a non-player kills it");
        }

        // ════════════════ counter-checks / boundaries ════════════════

        [Test]
        public void NonDiedEvent_Ignored()
        {
            var ns = new NarrativeStatePart();
            NarrativeStatePart.Current = ns;
            var mob = MakeMob("gremlin_slain");
            mob.FireEventAndRelease(GameEvent.New("BeforeTakeDamage"));
            Assert.AreEqual(0, ns.GetFact("gremlin_slain"), "a non-Died event sets nothing");
        }

        [Test]
        public void EmptyFact_NoOpNoThrow()
        {
            NarrativeStatePart.Current = new NarrativeStatePart();
            var e = new Entity { ID = "mob" };
            e.AddPart(new SetFactWhenSlain { Fact = "" });
            Assert.DoesNotThrow(() => FireDied(e));
        }

        [Test]
        public void NullNarrativeState_NoThrow()
        {
            NarrativeStatePart.Current = null;
            var mob = MakeMob("gremlin_slain");
            Assert.DoesNotThrow(() => FireDied(mob));
        }

        [Test]
        public void SaveLoad_RoundTripsFactAndValue()
        {
            var e = new Entity { ID = "mob", BlueprintName = "SootGremlin" };
            e.AddPart(new SetFactWhenSlain { Fact = "gremlin_slain", Value = 3 });
            var loaded = PartRoundTripHelper.RoundTripEntity(e);
            var part = loaded.GetPart<SetFactWhenSlain>();
            Assert.IsNotNull(part, "SetFactWhenSlain survives save/load");
            Assert.AreEqual("gremlin_slain", part.Fact);
            Assert.AreEqual(3, part.Value);
        }

        // ════════════════ THE UNBLOCK: order-independence ════════════════

        [Test]
        public void KillBeforeAccept_ThenAccept_ObjectiveCompletesOnTick()
        {
            // The whole point: a world kill objective must be order-independent.
            // Player kills the mob BEFORE accepting the quest → SetFactWhenSlain
            // sets the persisted fact → later the quest starts → the objective's
            // IfFact trigger passes on the next tick → it finishes. No soft-lock
            // (FinishObjectiveWhenSlain would have no-op'd the pre-accept kill).
            var ns = new NarrativeStatePart();
            NarrativeStatePart.Current = ns;

            // Quest: stage "hunt" with a kill objective gated on the slain fact.
            var sd = new StoryletData { ID = "Q", Quest = new QuestData() };
            var hunt = new QuestStageData { ID = "hunt" };
            var slay = new QuestObjectiveData { ID = "slay_gremlin" };
            slay.Triggers.Add(new ConversationParam { Key = "IfFact", Value = "gremlin_slain:>=:1" });
            hunt.Objectives.Add(slay);
            sd.Quest.Stages.Add(hunt);
            sd.Quest.Stages.Add(new QuestStageData { ID = "done" }); // terminal
            StoryletRegistry.Register(sd);

            var part = new StoryletPart();
            StoryletPart.Current = part;

            // 1. Kill the mob BEFORE the quest exists/active.
            var mob = MakeMob("gremlin_slain");
            FireDied(mob);
            Assert.AreEqual(1, ns.GetFact("gremlin_slain"), "kill set the fact pre-accept");

            // 2. Now accept the quest.
            part.StartQuest(new QuestState { QuestId = "Q", CurrentStageIndex = 0 });
            Assert.IsFalse(part.IsObjectiveFinished("Q", "slay_gremlin"),
                "not finished the instant the quest starts (tick hasn't run)");

            // 3. Next tick: the objective's IfFact trigger passes → it finishes
            //    → being the only required objective, the stage advances.
            part.OnTickEnd(ns);
            Assert.AreEqual(1, part.GetQuestState("Q").CurrentStageIndex,
                "the pre-accept kill completes the objective on the first tick → stage advances (ORDER-INDEPENDENT, no soft-lock)");
        }

        [Test]
        public void KillAfterAccept_AlsoCompletesOnTick()
        {
            // Counter-case: the normal order (accept, then kill) also works.
            var ns = new NarrativeStatePart();
            NarrativeStatePart.Current = ns;
            var sd = new StoryletData { ID = "Q", Quest = new QuestData() };
            var hunt = new QuestStageData { ID = "hunt" };
            var slay = new QuestObjectiveData { ID = "slay_gremlin" };
            slay.Triggers.Add(new ConversationParam { Key = "IfFact", Value = "gremlin_slain:>=:1" });
            hunt.Objectives.Add(slay);
            sd.Quest.Stages.Add(hunt);
            sd.Quest.Stages.Add(new QuestStageData { ID = "done" });
            StoryletRegistry.Register(sd);
            var part = new StoryletPart();
            StoryletPart.Current = part;

            part.StartQuest(new QuestState { QuestId = "Q", CurrentStageIndex = 0 });
            part.OnTickEnd(ns);
            Assert.AreEqual(0, part.GetQuestState("Q").CurrentStageIndex, "not yet — mob alive");

            var mob = MakeMob("gremlin_slain");
            FireDied(mob);
            part.OnTickEnd(ns);
            Assert.AreEqual(1, part.GetQuestState("Q").CurrentStageIndex,
                "killing after accepting also completes the objective on the next tick");
        }
    }
}
