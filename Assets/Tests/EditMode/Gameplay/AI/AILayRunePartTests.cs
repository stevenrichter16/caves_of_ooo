using System;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// M6.3 — AILayRunePart (AIBoredEvent handler that pushes
    /// <see cref="LayRuneGoal"/>). Covers the probability gate, the
    /// stack-cleanliness gate (no double-push), the per-zone quota cap
    /// (MaxRunesPerZone), the null-zone / no-target graceful paths, and
    /// the blueprint-list random-pick.
    ///
    /// Mirrors Qud's <c>XRL.World.Parts.Miner</c> BeginTakeActionEvent
    /// tests (Miner.cs:95-128).
    /// </summary>
    [TestFixture]
    public class AILayRunePartTests
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
                        { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""DisplayName"", ""Value"": ""test rune"" }]},
                        { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""false"" }]},
                        { ""Name"": ""RuneFlameTrigger"", ""Params"": [{ ""Key"": ""Damage"", ""Value"": ""5"" }]}
                    ],
                    ""Tags"": [{ ""Key"": ""Rune"", ""Value"": """" }]
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
            LayRuneGoal.Factory = null;
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private Entity CreateCultist(
            Zone zone, int x, int y,
            int chance = 100,
            int maxRunes = 5,
            int searchRadius = 4,
            string runeBlueprints = "TestRune",
            int seed = 42)
        {
            var e = new Entity { BlueprintName = "Cultist", ID = "cultist-1" };
            e.AddPart(new RenderPart { DisplayName = "cultist" });
            e.AddPart(new PhysicsPart { Solid = false });
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            e.SetTag("Faction", "Cultists");
            var brain = new BrainPart { CurrentZone = zone, Rng = new Random(seed) };
            e.AddPart(brain);
            e.AddPart(new AILayRunePart
            {
                Chance = chance,
                MaxRunesPerZone = maxRunes,
                SearchRadius = searchRadius,
                RuneBlueprints = runeBlueprints
            });
            zone.AddEntity(e, x, y);
            return e;
        }

        /// <summary>
        /// Fire AIBoredEvent.Check on the cultist the same way BoredGoal would.
        /// Returns the "unhandled" result (true = proceed with default idle,
        /// false = a behavior part consumed the event).
        /// </summary>
        private bool FireBored(Entity cultist)
        {
            return AIBoredEvent.Check(cultist);
        }

        // ====================================================================
        // Happy path
        // ====================================================================

        [Test]
        public void AILayRune_PushesLayRuneGoal_OnBoredTick()
        {
            var zone = new Zone("TestZone");
            var cultist = CreateCultist(zone, 10, 10, chance: 100);
            var brain = cultist.GetPart<BrainPart>();

            bool unhandled = FireBored(cultist);

            Assert.IsFalse(unhandled,
                "Chance=100 + empty stack + zero existing runes must consume the AIBored event.");
            Assert.IsTrue(brain.HasGoal<LayRuneGoal>(),
                "AILayRune must push LayRuneGoal on a successful bored tick.");
        }

        [Test]
        public void AILayRune_LayRuneGoal_TargetsPassableCellWithinRadius()
        {
            var zone = new Zone("TestZone");
            var cultist = CreateCultist(zone, 10, 10, chance: 100, searchRadius: 3);
            var brain = cultist.GetPart<BrainPart>();

            FireBored(cultist);

            var goal = (LayRuneGoal)brain.PeekGoalAt(brain.GoalCount - 1);
            Assert.IsNotNull(goal);
            int dx = System.Math.Abs(goal.TargetX - 10);
            int dy = System.Math.Abs(goal.TargetY - 10);
            Assert.LessOrEqual(System.Math.Max(dx, dy), 3,
                "Target cell must be within SearchRadius Chebyshev distance of the NPC.");
            Assert.IsFalse(dx == 0 && dy == 0,
                "Target must not be the NPC's own cell.");
        }

        // ====================================================================
        // Gates
        // ====================================================================

        [Test]
        public void AILayRune_ProbabilityGate_SkipsWhenUnlucky()
        {
            var zone = new Zone("TestZone");
            // Chance=0 means Rng.Next(100) >= 0 is always true → skip.
            var cultist = CreateCultist(zone, 10, 10, chance: 0);
            var brain = cultist.GetPart<BrainPart>();

            bool unhandled = FireBored(cultist);

            Assert.IsTrue(unhandled,
                "Chance=0 must not consume the AIBored event — other idle behaviors should still run.");
            Assert.IsFalse(brain.HasGoal<LayRuneGoal>(),
                "No goal should be pushed when the probability gate fails.");
        }

        [Test]
        public void AILayRune_DoesNotDoublePush_WhenLayRuneGoalAlreadyOnStack()
        {
            var zone = new Zone("TestZone");
            var cultist = CreateCultist(zone, 10, 10, chance: 100);
            var brain = cultist.GetPart<BrainPart>();

            // Pre-push a LayRuneGoal directly.
            brain.PushGoal(new LayRuneGoal(5, 5, "TestRune"));
            int initialCount = brain.GoalCount;

            bool unhandled = FireBored(cultist);

            Assert.IsTrue(unhandled,
                "When a LayRuneGoal is already on the stack, AILayRune must let other behaviors try (don't consume).");
            Assert.AreEqual(initialCount, brain.GoalCount,
                "No second LayRuneGoal should be pushed while one is already active.");
        }

        [Test]
        public void AILayRune_ZoneQuota_SkipsWhenCapReached()
        {
            var zone = new Zone("TestZone");

            // Pre-populate with MaxRunesPerZone runes.
            for (int i = 0; i < 3; i++)
            {
                var rune = _factory.CreateEntity("TestRune");
                Assert.IsNotNull(rune);
                zone.AddEntity(rune, 2 + i, 2);
            }

            var cultist = CreateCultist(zone, 10, 10, chance: 100, maxRunes: 3);
            var brain = cultist.GetPart<BrainPart>();

            bool unhandled = FireBored(cultist);

            Assert.IsTrue(unhandled,
                "Once MaxRunesPerZone is hit, AILayRune must step aside so other idle behaviors run.");
            Assert.IsFalse(brain.HasGoal<LayRuneGoal>(),
                "No goal should be pushed when the zone quota is saturated.");
        }

        [Test]
        public void AILayRune_NoTargetCell_GracefullyDoesNothing()
        {
            var zone = new Zone("TestZone");
            // Place cultist in a corner with radius 0 — no neighbors → no candidates.
            var cultist = CreateCultist(zone, 0, 0, chance: 100, searchRadius: 0);
            var brain = cultist.GetPart<BrainPart>();

            bool unhandled = FireBored(cultist);

            Assert.IsTrue(unhandled,
                "When no passable target cell is found, the part must let other behaviors run.");
            Assert.IsFalse(brain.HasGoal<LayRuneGoal>(),
                "No goal should be pushed when no target is found.");
        }

        // ====================================================================
        // Rune blueprint selection
        // ====================================================================

        [Test]
        public void AILayRune_PicksBlueprintFromList()
        {
            var zone = new Zone("TestZone");
            var cultist = CreateCultist(zone, 10, 10, chance: 100,
                runeBlueprints: "TestRune");
            var brain = cultist.GetPart<BrainPart>();

            FireBored(cultist);

            var goal = (LayRuneGoal)brain.PeekGoalAt(brain.GoalCount - 1);
            Assert.IsNotNull(goal);
            Assert.AreEqual("TestRune", goal.RuneBlueprint,
                "LayRuneGoal's blueprint must be one of the configured RuneBlueprints.");
        }

        [Test]
        public void AILayRune_EmptyBlueprintList_DoesNothing()
        {
            var zone = new Zone("TestZone");
            var cultist = CreateCultist(zone, 10, 10, chance: 100,
                runeBlueprints: "");
            var brain = cultist.GetPart<BrainPart>();

            bool unhandled = FireBored(cultist);

            Assert.IsTrue(unhandled,
                "Empty RuneBlueprints must not consume the event (graceful unconfigured path).");
            Assert.IsFalse(brain.HasGoal<LayRuneGoal>());
        }

        [Test]
        public void AILayRune_BlueprintListParses_CommaAndWhitespace()
        {
            var zone = new Zone("TestZone");
            var cultist = CreateCultist(zone, 10, 10, chance: 100,
                runeBlueprints: " TestRune , TestRune ");
            var brain = cultist.GetPart<BrainPart>();

            FireBored(cultist);

            var goal = (LayRuneGoal)brain.PeekGoalAt(brain.GoalCount - 1);
            Assert.IsNotNull(goal);
            Assert.AreEqual("TestRune", goal.RuneBlueprint,
                "Whitespace around comma-separated entries must be trimmed.");
        }

        // ====================================================================
        // Null-safety
        // ====================================================================

        [Test]
        public void AILayRune_NoBrain_DoesNothing()
        {
            var e = new Entity { BlueprintName = "Orphan" };
            e.AddPart(new AILayRunePart { Chance = 100 });
            // Deliberately no BrainPart.

            bool unhandled = AIBoredEvent.Check(e);

            Assert.IsTrue(unhandled,
                "AILayRune must be a safe no-op when the entity has no BrainPart.");
        }
    }
}
