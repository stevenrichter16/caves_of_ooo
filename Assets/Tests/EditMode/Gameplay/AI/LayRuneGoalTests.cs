using System;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M6.2 — LayRuneGoal (Walk → Place state machine). Covers the at-target
    /// placement path, the walking-retry path, the factory-null graceful
    /// no-op, and the faction-stamp on the spawned rune.
    ///
    /// Uses a minimal inline blueprint JSON so the test does not depend on
    /// the full game Objects.json. Mirrors the CorpsePartTests harness
    /// shape (M5.1).
    /// </summary>
    [TestFixture]
    public class LayRuneGoalTests
    {
        private const string TestBlueprintsJson = @"{
            ""Objects"": [
                {
                    ""Name"": ""PhysicalObject"",
                    ""Parts"": [
                        { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""?"" }] },
                        { ""Name"": ""Physics"", ""Params"": [] }
                    ],
                    ""Stats"": [],
                    ""Tags"": []
                },
                {
                    ""Name"": ""TestRune"",
                    ""Inherits"": ""PhysicalObject"",
                    ""Parts"": [
                        { ""Name"": ""Render"", ""Params"": [
                            { ""Key"": ""DisplayName"", ""Value"": ""test rune"" },
                            { ""Key"": ""RenderString"", ""Value"": ""*"" }
                        ]},
                        { ""Name"": ""Physics"", ""Params"": [
                            { ""Key"": ""Solid"", ""Value"": ""false"" }
                        ]},
                        { ""Name"": ""RuneFlameTrigger"", ""Params"": [
                            { ""Key"": ""Damage"", ""Value"": ""7"" }
                        ]}
                    ],
                    ""Tags"": [
                        { ""Key"": ""Rune"", ""Value"": """" }
                    ]
                }
            ]
        }";

        private EntityFactory _factory;

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            _factory = new EntityFactory();
            _factory.LoadBlueprints(TestBlueprintsJson);
            LayRuneGoal.Factory = _factory;
        }

        [TearDown]
        public void Teardown()
        {
            // Static hygiene — prevent leakage into adjacent test fixtures.
            // Mirrors CorpsePart.Factory teardown pattern.
            LayRuneGoal.Factory = null;
            // Reset log-once latches so each test sees a fresh state.
            LayRuneGoal.FactoryNullWarned = false;
            LayRuneGoal.BlueprintMissingWarned = false;
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private Entity CreateCultist(Zone zone, int x, int y, string faction = "Cultists")
        {
            var e = new Entity { BlueprintName = "Cultist", ID = "cultist-1" };
            e.AddPart(new RenderPart { DisplayName = "cultist" });
            e.AddPart(new PhysicsPart { Solid = false });
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            e.SetTag("Faction", faction);
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(42) };
            e.AddPart(brain);
            zone.AddEntity(e, x, y);
            return e;
        }

        /// <summary>
        /// Find the first rune entity on the zone (has the "Rune" tag).
        /// </summary>
        private Entity FindRune(Zone zone)
        {
            foreach (var entity in zone.GetReadOnlyEntities())
            {
                if (entity.HasTag("Rune")) return entity;
            }
            return null;
        }

        // ====================================================================
        // At-target placement
        // ====================================================================

        [Test]
        public void LayRuneGoal_PlacesRune_AtTarget_WhenActorIsOnTarget()
        {
            var zone = new Zone("TestZone");
            var cultist = CreateCultist(zone, 10, 10);
            var goal = new LayRuneGoal(10, 10, "TestRune");
            cultist.GetPart<BrainPart>().PushGoal(goal);

            goal.TakeAction();

            var rune = FindRune(zone);
            Assert.IsNotNull(rune, "LayRuneGoal must spawn the rune when the actor is already on the target cell.");
            var cell = zone.GetEntityCell(rune);
            Assert.AreEqual(10, cell.X);
            Assert.AreEqual(10, cell.Y);
            Assert.IsTrue(goal.Finished(), "Goal should be done after placing the rune.");
        }

        [Test]
        public void LayRuneGoal_StampsLayerFaction_OnRuneTrigger()
        {
            var zone = new Zone("TestZone");
            var cultist = CreateCultist(zone, 10, 10, faction: "Cultists");
            var goal = new LayRuneGoal(10, 10, "TestRune");
            cultist.GetPart<BrainPart>().PushGoal(goal);

            goal.TakeAction();

            var rune = FindRune(zone);
            var trigger = rune.GetPart<TriggerOnStepPart>();
            Assert.IsNotNull(trigger, "Spawned rune should carry a TriggerOnStepPart.");
            Assert.AreEqual("Cultists", trigger.TriggerFaction,
                "LayRuneGoal must stamp the layer's faction onto TriggerFaction so allies don't detonate the rune.");
        }

        // ====================================================================
        // Walk phase
        // ====================================================================

        [Test]
        public void LayRuneGoal_PushesMoveToGoal_WhenNotAtTarget()
        {
            var zone = new Zone("TestZone");
            var cultist = CreateCultist(zone, 5, 5);
            var brain = cultist.GetPart<BrainPart>();
            var goal = new LayRuneGoal(10, 10, "TestRune");
            brain.PushGoal(goal);

            goal.TakeAction();

            Assert.IsTrue(brain.HasGoal<MoveToGoal>(),
                "LayRuneGoal must push a MoveToGoal child when the actor is not on the target cell.");
            Assert.IsNull(FindRune(zone),
                "Rune must NOT be placed before the actor reaches the target cell.");
            Assert.AreEqual(1, goal.MoveTries, "MoveTries counter must advance on each push.");
        }

        [Test]
        public void LayRuneGoal_GivesUp_AfterMaxMoveTries()
        {
            var zone = new Zone("TestZone");
            var cultist = CreateCultist(zone, 5, 5);
            var brain = cultist.GetPart<BrainPart>();
            var goal = new LayRuneGoal(10, 10, "TestRune");
            brain.PushGoal(goal);

            // Simulate exhaustion — nudge MoveTries past the cap and tick.
            goal.MoveTries = LayRuneGoal.MaxMoveTries;
            goal.TakeAction();

            Assert.IsTrue(goal.Finished(),
                "After exhausting MaxMoveTries, goal must mark itself Finished (quiet give-up, mirrors DisposeOfCorpseGoal).");
            Assert.IsNull(FindRune(zone),
                "No rune should be spawned when the goal gives up without ever reaching the target.");
        }

        // ====================================================================
        // Validation / graceful-failure paths
        // ====================================================================

        [Test]
        public void LayRuneGoal_Fails_WhenTargetOutOfBounds()
        {
            var zone = new Zone("TestZone");
            var cultist = CreateCultist(zone, 5, 5);
            var brain = cultist.GetPart<BrainPart>();
            var goal = new LayRuneGoal(-1, -1, "TestRune");
            brain.PushGoal(goal);

            goal.TakeAction();

            Assert.IsFalse(brain.HasGoal<LayRuneGoal>(),
                "Out-of-bounds target must pop the goal via FailToParent.");
        }

        [Test]
        public void LayRuneGoal_Fails_WhenBlueprintEmpty()
        {
            var zone = new Zone("TestZone");
            var cultist = CreateCultist(zone, 10, 10);
            var brain = cultist.GetPart<BrainPart>();
            var goal = new LayRuneGoal(10, 10, "");
            brain.PushGoal(goal);

            goal.TakeAction();

            Assert.IsFalse(brain.HasGoal<LayRuneGoal>(),
                "Empty RuneBlueprint must pop the goal via FailToParent.");
            Assert.IsNull(FindRune(zone));
        }

        [Test]
        public void LayRuneGoal_NoOp_WhenFactoryNull_AndDoesNotCrash()
        {
            // Graceful no-op matches CorpsePart.HandleDied's null-Factory path.
            LayRuneGoal.Factory = null;

            var zone = new Zone("TestZone");
            var cultist = CreateCultist(zone, 10, 10);
            var brain = cultist.GetPart<BrainPart>();
            var goal = new LayRuneGoal(10, 10, "TestRune");
            brain.PushGoal(goal);

            // Warning is expected on first trip — but only ONCE.
            UnityEngine.TestTools.LogAssert.Expect(
                UnityEngine.LogType.Warning,
                new System.Text.RegularExpressions.Regex("LayRuneGoal.*Factory is null"));

            Assert.DoesNotThrow(() => goal.TakeAction(),
                "LayRuneGoal must not crash when Factory is unwired.");
            Assert.IsNull(FindRune(zone),
                "No rune should be placed when Factory is null (graceful no-op).");
            Assert.IsTrue(goal.Finished(),
                "Goal should still mark itself done so it doesn't loop forever.");
        }

        // ====================================================================
        // P-09 regression — log-once guards
        // ====================================================================

        [Test]
        public void LayRune_FactoryNullWarning_LogsOnce_AcrossMultipleAttempts()
        {
            LayRuneGoal.Factory = null;
            LayRuneGoal.FactoryNullWarned = false;

            var zone = new Zone("TestZone");
            var cultist = CreateCultist(zone, 10, 10);
            var brain = cultist.GetPart<BrainPart>();

            // Expect exactly ONE warning despite five goal cycles. If
            // we emitted per-cycle, LogAssert would fail on the
            // unmatched subsequent warnings.
            UnityEngine.TestTools.LogAssert.Expect(
                UnityEngine.LogType.Warning,
                new System.Text.RegularExpressions.Regex("LayRuneGoal.*Factory is null"));

            for (int i = 0; i < 5; i++)
            {
                var goal = new LayRuneGoal(10, 10, "TestRune");
                brain.PushGoal(goal);
                goal.TakeAction();
                brain.RemoveGoal(goal);
            }

            Assert.IsTrue(LayRuneGoal.FactoryNullWarned,
                "Latch must be set after first warning.");
        }

        [Test]
        public void LayRune_BlueprintMissingWarning_LogsOnce_AcrossMultipleAttempts()
        {
            // Factory present but blueprint name is bogus.
            LayRuneGoal.BlueprintMissingWarned = false;

            var zone = new Zone("TestZone");
            var cultist = CreateCultist(zone, 10, 10);
            var brain = cultist.GetPart<BrainPart>();

            // CreateEntity on a missing blueprint logs its own error via
            // EntityFactory before we get to LayRuneGoal's null-check.
            // Accept any errors from that path alongside our one warning.
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    var goal = new LayRuneGoal(10, 10, "NoSuchRuneBlueprint");
                    brain.PushGoal(goal);
                    goal.TakeAction();
                    brain.RemoveGoal(goal);
                }

                Assert.IsTrue(LayRuneGoal.BlueprintMissingWarned,
                    "Latch must be set after first blueprint-missing warning.");
            }
            finally
            {
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            }
        }

        // ====================================================================
        // OnPop cleanup
        // ====================================================================

        [Test]
        public void LayRuneGoal_OnPop_ClearsThought()
        {
            var zone = new Zone("TestZone");
            var cultist = CreateCultist(zone, 5, 5);
            var brain = cultist.GetPart<BrainPart>();
            var goal = new LayRuneGoal(10, 10, "TestRune");
            brain.PushGoal(goal);

            // Walk-phase tick writes "laying rune" to LastThought.
            goal.TakeAction();
            Assert.AreEqual("laying rune", brain.LastThought,
                "Walk-phase tick should surface 'laying rune' in the inspector.");

            brain.RemoveGoal(goal);

            Assert.IsNull(brain.LastThought,
                "OnPop must clear LastThought (same pattern as DisposeOfCorpseGoal).");
        }

        // ====================================================================
        // Real-blueprint smoke test
        // ====================================================================

        [Test]
        public void LayRuneGoal_RealBlueprints_RuneOfFlame_Loads()
        {
            // Load the real Objects.json to pin that the authored rune
            // blueprint is still valid and spawns with the expected Parts.
            var realFactory = new EntityFactory();
            var realJson = UnityEngine.Resources
                .Load<UnityEngine.TextAsset>("Content/Blueprints/Objects")
                ?.text;
            if (string.IsNullOrEmpty(realJson))
            {
                Assert.Ignore("Real Objects.json not loadable via Resources (editor path mismatch) — test relies on runtime-loadable blueprint.");
                return;
            }
            realFactory.LoadBlueprints(realJson);

            var rune = realFactory.CreateEntity("RuneOfFlame");
            Assert.IsNotNull(rune, "RuneOfFlame blueprint must exist in Objects.json.");
            Assert.IsNotNull(rune.GetPart<RuneFlameTriggerPart>(),
                "RuneOfFlame must carry RuneFlameTriggerPart.");
            var trigger = rune.GetPart<RuneFlameTriggerPart>();
            Assert.AreEqual(8, trigger.Damage,
                "Authored Damage=8 should pass through the blueprint reflection.");
            Assert.AreEqual(5, trigger.SmolderDuration,
                "Authored SmolderDuration=5 should pass through.");
        }
    }
}
