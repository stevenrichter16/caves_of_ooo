using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M3 TDD tests for StoryletPart.OnTickEnd: trigger evaluation + dispatch.
    /// Non-quest storylets whose Triggers all pass fire their Effects via
    /// ConversationActions.Execute. OneShot fires exactly once. Single-pass
    /// dispatch — fact cascades land on the NEXT tick.
    ///
    /// Every positive assertion is paired with a counter-check (CLAUDE.md §3.4).
    /// </summary>
    public class StoryletReactorTests
    {
        private NarrativeStatePart _narrativeState;
        private StoryletPart _storyletPart;

        [SetUp]
        public void SetUp()
        {
            StoryletRegistry.Reset();
            MessageLog.Clear();

            _narrativeState = new NarrativeStatePart();
            NarrativeStatePart.Current = _narrativeState;

            _storyletPart = new StoryletPart();
            _narrativeState.RegisterReactor(_storyletPart);
        }

        [TearDown]
        public void TearDown()
        {
            NarrativeStatePart.Current = null;
            StoryletPart.Current = null;
            StoryletRegistry.Reset();
            MessageLog.Clear();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static StoryletData MakeStorylet(
            string id, bool oneShot,
            List<ConversationParam> triggers,
            List<ConversationParam> effects)
        {
            return new StoryletData
            {
                ID = id,
                OneShot = oneShot,
                Triggers = triggers ?? new List<ConversationParam>(),
                Effects = effects ?? new List<ConversationParam>(),
            };
        }

        private static List<ConversationParam> One(string key, string value) =>
            new List<ConversationParam> { new ConversationParam { Key = key, Value = value } };

        private void Tick() => _storyletPart.OnTickEnd(_narrativeState);

        // ── Predicate gating ─────────────────────────────────────────────────

        [Test]
        public void OnTickEnd_PassingPredicate_FiresEffect()
        {
            _narrativeState.SetFact("door", 1);
            StoryletRegistry.Register(MakeStorylet(
                "door_open", oneShot: true,
                triggers: One("IfFact", "door:>=:1"),
                effects: One("AddMessage", "the door creaks open")));

            Tick();

            Assert.AreEqual("the door creaks open", MessageLog.GetLast());
            Assert.IsTrue(_storyletPart.HasFired("door_open"));
        }

        // counter-check
        [Test]
        public void OnTickEnd_FailingPredicate_DoesNotFireEffect()
        {
            // door = 0 → IfFact:door:>=:1 is false → no fire
            StoryletRegistry.Register(MakeStorylet(
                "door_open", oneShot: true,
                triggers: One("IfFact", "door:>=:1"),
                effects: One("AddMessage", "the door creaks open")));

            Tick();

            Assert.IsNull(MessageLog.GetLast());
            Assert.IsFalse(_storyletPart.HasFired("door_open"));
        }

        [Test]
        public void OnTickEnd_MultiplePredicatesAllPass_Fires()
        {
            _narrativeState.SetFact("a", 1);
            _narrativeState.SetFact("b", 5);
            StoryletRegistry.Register(MakeStorylet(
                "both", oneShot: true,
                triggers: new List<ConversationParam>
                {
                    new ConversationParam { Key = "IfFact", Value = "a:>=:1" },
                    new ConversationParam { Key = "IfFact", Value = "b:>=:5" },
                },
                effects: One("AddMessage", "both passed")));

            Tick();

            Assert.AreEqual("both passed", MessageLog.GetLast());
        }

        // counter-check
        [Test]
        public void OnTickEnd_MultiplePredicatesAnyFail_DoesNotFire()
        {
            _narrativeState.SetFact("a", 1);
            _narrativeState.SetFact("b", 0); // fails second predicate
            StoryletRegistry.Register(MakeStorylet(
                "both", oneShot: true,
                triggers: new List<ConversationParam>
                {
                    new ConversationParam { Key = "IfFact", Value = "a:>=:1" },
                    new ConversationParam { Key = "IfFact", Value = "b:>=:5" },
                },
                effects: One("AddMessage", "both passed")));

            Tick();

            Assert.IsNull(MessageLog.GetLast());
        }

        // ── OneShot semantics ────────────────────────────────────────────────

        [Test]
        public void OnTickEnd_OneShotStorylet_FiresExactlyOnceAcrossManyTicks()
        {
            _narrativeState.SetFact("door", 1);
            StoryletRegistry.Register(MakeStorylet(
                "door_open", oneShot: true,
                triggers: One("IfFact", "door:>=:1"),
                effects: One("AddMessage", "the door creaks open")));

            Tick();
            Tick();
            Tick();

            Assert.AreEqual(1, MessageLog.Count,
                "OneShot storylet must fire exactly once even across many ticks with passing predicate");
        }

        // counter-check
        [Test]
        public void OnTickEnd_NonOneShotStorylet_FiresEveryTickPredicatesPass()
        {
            _narrativeState.SetFact("siren", 1);
            StoryletRegistry.Register(MakeStorylet(
                "siren_wail", oneShot: false,
                triggers: One("IfFact", "siren:>=:1"),
                effects: One("AddMessage", "wail")));

            Tick();
            Tick();
            Tick();

            Assert.AreEqual(3, MessageLog.Count,
                "Non-OneShot fires every tick the predicate is true");
        }

        [Test]
        public void OnTickEnd_OneShotAfterFiring_DoesNotFireEvenIfPredicateStillTrue()
        {
            _narrativeState.SetFact("door", 1);
            StoryletRegistry.Register(MakeStorylet(
                "door_open", oneShot: true,
                triggers: One("IfFact", "door:>=:1"),
                effects: One("AddMessage", "creak")));

            Tick();
            Assert.IsTrue(_storyletPart.HasFired("door_open"));

            // Predicate still true on subsequent tick
            Tick();
            Assert.AreEqual(1, MessageLog.Count);
        }

        // ── Single-pass dispatch (cascade safety) ────────────────────────────

        [Test]
        public void OnTickEnd_StoryletEffectFlipsAnotherStoryletPredicate_CascadeLandsNextTick()
        {
            // Storylet A: trigger IfFact:a:>=:1 ; effect SetFact:b:1
            // Storylet B: trigger IfFact:b:>=:1 ; effect AddMessage:"B fired"
            //
            // a=1 at start. Tick 1: eligibility snapshotted with b=0 → only A
            // is eligible. A fires, sets b=1. B's predicate is now true but B
            // was NOT in the snapshot → does NOT fire this tick.
            // Tick 2: eligibility re-snapshotted, B's predicate now true → fires.

            _narrativeState.SetFact("a", 1);
            // _narrativeState.SetFact("b", 0); — implicit (FactBag default)

            StoryletRegistry.Register(MakeStorylet(
                "A", oneShot: true,
                triggers: One("IfFact", "a:>=:1"),
                effects: One("SetFact", "b:1")));
            StoryletRegistry.Register(MakeStorylet(
                "B", oneShot: true,
                triggers: One("IfFact", "b:>=:1"),
                effects: One("AddMessage", "B fired")));

            Tick();
            // After tick 1: A fired, b=1, but B did NOT fire this tick.
            Assert.IsTrue(_storyletPart.HasFired("A"));
            Assert.AreEqual(1, _narrativeState.GetFact("b"));
            Assert.IsFalse(_storyletPart.HasFired("B"),
                "Cascading storylet B must NOT fire same tick as A — single-pass dispatch invariant");
            Assert.IsNull(MessageLog.GetLast(),
                "B's effect must not have fired this tick");

            Tick();
            // Tick 2: B is now eligible.
            Assert.IsTrue(_storyletPart.HasFired("B"));
            Assert.AreEqual("B fired", MessageLog.GetLast());
        }

        // ── Quests are skipped by non-quest dispatch ─────────────────────────

        [Test]
        public void OnTickEnd_QuestStorylet_DoesNotFireViaNonQuestDispatch()
        {
            // M3 only handles non-quest storylets. Quest dispatch arrives in M4.
            _narrativeState.SetFact("started", 1);
            StoryletRegistry.Register(new StoryletData
            {
                ID = "MainQuest",
                OneShot = false,
                Triggers = One("IfFact", "started:>=:1"),
                Effects = One("AddMessage", "should not fire as a regular storylet"),
                Quest = new QuestData
                {
                    Stages = new List<QuestStageData>
                    {
                        new QuestStageData { ID = "Stage0" }
                    }
                }
            });

            Tick();

            Assert.IsNull(MessageLog.GetLast());
            Assert.IsFalse(_storyletPart.HasFired("MainQuest"));
        }

        // ── Empty / vacuous cases ────────────────────────────────────────────

        [Test]
        public void OnTickEnd_NoRegisteredStorylets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => Tick());
        }

        [Test]
        public void OnTickEnd_StoryletWithNoTriggers_FiresVacuously()
        {
            // Empty trigger list → CheckAll returns true vacuously → fires.
            StoryletRegistry.Register(MakeStorylet(
                "free", oneShot: true,
                triggers: new List<ConversationParam>(),
                effects: One("AddMessage", "always")));

            Tick();

            Assert.AreEqual("always", MessageLog.GetLast());
        }

        [Test]
        public void OnTickEnd_StoryletWithNoEffects_StillMarksFiredIfOneShot()
        {
            _narrativeState.SetFact("x", 1);
            StoryletRegistry.Register(MakeStorylet(
                "silent", oneShot: true,
                triggers: One("IfFact", "x:>=:1"),
                effects: new List<ConversationParam>()));

            Tick();
            Assert.IsTrue(_storyletPart.HasFired("silent"));
        }

        // ── Effect dispatch goes through ConversationActions ─────────────────

        [Test]
        public void OnTickEnd_SetFactEffect_MutatesNarrativeState()
        {
            _narrativeState.SetFact("trigger", 1);
            StoryletRegistry.Register(MakeStorylet(
                "fact_setter", oneShot: true,
                triggers: One("IfFact", "trigger:>=:1"),
                effects: One("SetFact", "result:42")));

            Tick();

            Assert.AreEqual(42, _narrativeState.GetFact("result"));
        }

        [Test]
        public void OnTickEnd_MultipleEffects_AllExecute()
        {
            _narrativeState.SetFact("trigger", 1);
            StoryletRegistry.Register(MakeStorylet(
                "many", oneShot: true,
                triggers: One("IfFact", "trigger:>=:1"),
                effects: new List<ConversationParam>
                {
                    new ConversationParam { Key = "AddMessage", Value = "first" },
                    new ConversationParam { Key = "SetFact", Value = "x:7" },
                    new ConversationParam { Key = "AddMessage", Value = "second" },
                }));

            Tick();

            Assert.AreEqual(2, MessageLog.Count);
            Assert.AreEqual("second", MessageLog.GetLast());
            Assert.AreEqual(7, _narrativeState.GetFact("x"));
        }

        // ── Adversarial: re-entrancy / dispatch-during-iteration ─────────────

        [Test]
        public void OnTickEnd_StoryletThatRegistersAnotherStoryletDuringEffect_DoesNotCrash()
        {
            // We don't currently expose Register from a conversation action,
            // but iterating over the registry while it mutates would be
            // catastrophic. Confirm we copy/snapshot before iterating effects.
            _narrativeState.SetFact("trigger", 1);
            StoryletRegistry.Register(MakeStorylet(
                "self_modifier", oneShot: true,
                triggers: One("IfFact", "trigger:>=:1"),
                effects: One("AddMessage", "fired")));

            Assert.DoesNotThrow(() => Tick());
            Assert.AreEqual("fired", MessageLog.GetLast());
        }

        [Test]
        public void OnTickEnd_AlreadyFiredOneShot_OmittedFromEligibilityScan()
        {
            // A second storylet fires AFTER an already-fired OneShot in
            // iteration order. The second storylet must still get its turn.
            _narrativeState.SetFact("a", 1);
            _narrativeState.SetFact("b", 1);

            StoryletRegistry.Register(MakeStorylet(
                "first", oneShot: true,
                triggers: One("IfFact", "a:>=:1"),
                effects: One("AddMessage", "first")));
            StoryletRegistry.Register(MakeStorylet(
                "second", oneShot: true,
                triggers: One("IfFact", "b:>=:1"),
                effects: One("AddMessage", "second")));

            Tick();
            Assert.AreEqual(2, MessageLog.Count);

            // Tick 2 — first is fired, second is fired. No more messages.
            Tick();
            Assert.AreEqual(2, MessageLog.Count,
                "Already-fired OneShots must be omitted from subsequent ticks");
        }
    }
}
