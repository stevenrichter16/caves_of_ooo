using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios.Custom;
using CavesOfOoo.Skills;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests.Scenarios
{
    public class SpellFxShowcaseTests
    {
        private ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void Setup() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void Cleanup() => _harness.Dispose();

        [SetUp]
        public void BeforeEach()
        {
            SkillRegistry.ResetForTests();
            ResonanceSystem.EnsureInitialized();
            AsciiFxBus.Clear();
            SpellFxBus.Clear();
            MessageLog.Clear();
        }

        [TearDown]
        public void AfterEach()
        {
            AsciiFxBus.Clear();
            SpellFxBus.Clear();
            Cryomancy_GlacialWall.Factory = null;
            MaterialReactionResolver.Factory = null;
        }

        [Test]
        public void Cases_IncludeEveryRegisteredCastableAndRequiredOutcomeVariants()
        {
            var cases = SpellFxShowcase.CreateCases();
            Assert.AreEqual(49, cases.Select(c => c.SkillID).Distinct().Count());
            foreach (string school in new[] { "Pyromancy", "Cryomancy", "Galvanism", "Hydromancy", "Corrosion", "Spellcraft", "Rites" })
                Assert.IsTrue(cases.Any(c => c.SkillID.StartsWith(school + "_")), school);
            foreach (SpellFxShowcase.Variant variant in System.Enum.GetValues(typeof(SpellFxShowcase.Variant)))
                Assert.IsTrue(cases.Any(c => c.Outcome == variant), variant.ToString());
        }

        [Test]
        public void EveryPreparedCase_RoutesAnActualCastOrItsIntentionalRefusal()
        {
            var context = _harness.CreateContext(playerBlueprint: "Player");
            foreach (var definition in SpellFxShowcase.CreateCases())
            {
                AsciiFxBus.Clear();
                SpellFxBus.Clear();
                var stage = SpellFxShowcase.PrepareCase(context, definition);
                Assert.AreEqual(definition.ExpectAccepted, stage.Execute(), definition.Label);
                if (definition.ExpectAccepted)
                {
                    Assert.AreEqual(1, SpellFxBus.PendingCount, definition.Label + " must publish one actual resolution");
                    SpellFxSequence sequence = SpellFxBus.Drain()[0];
                    Assert.AreEqual(definition.SkillID, sequence.SpellId, definition.Label);
                    if (definition.Outcome == SpellFxShowcase.Variant.HighResonance)
                        Assert.Greater(sequence.MarksConsumed, 0, definition.Label + " must really consume staged marks");
                    if (definition.Outcome == SpellFxShowcase.Variant.ZeroMarks)
                        Assert.AreEqual(0, sequence.MarksConsumed, definition.Label);
                    if (definition.Outcome == SpellFxShowcase.Variant.Resisted)
                        Assert.IsTrue(sequence.Targets.Any(t => t.Resisted), definition.Label);
                    if (definition.Outcome == SpellFxShowcase.Variant.Blocked)
                    {
                        Assert.AreEqual(1, sequence.Path.Count, definition.Label);
                        Assert.IsFalse(sequence.Targets.Any(t => t.TargetId == stage.PrimaryTarget.ID),
                            "The creature beyond the wall must not be presented as hit.");
                    }
                }
                else
                    Assert.AreEqual(0, SpellFxBus.PendingCount, definition.Label + " must not publish expenditure");
                Assert.IsFalse(stage.Execute(), "A prepared stage resolves once: " + definition.Label);
                Assert.AreEqual(0, SpellFxBus.PendingCount, "A second execution must not republish: " + definition.Label);
            }
        }

        [Test]
        public void JetBlastMovesLivingTarget_AndDeathCaseRemovesItAtTheCapturedCell()
        {
            var context = _harness.CreateContext(playerBlueprint: "Player");
            var cases = SpellFxShowcase.CreateCases();
            var moved = SpellFxShowcase.PrepareCase(context,
                cases.First(c => c.Outcome == SpellFxShowcase.Variant.ForcedMovement));
            var origin = context.Zone.GetEntityPosition(moved.PrimaryTarget);
            Assert.IsTrue(moved.Execute());
            Assert.AreNotEqual(origin, context.Zone.GetEntityPosition(moved.PrimaryTarget));
            var dead = SpellFxShowcase.PrepareCase(context,
                cases.First(c => c.Outcome == SpellFxShowcase.Variant.Death));
            Assert.IsTrue(dead.Execute());
            Assert.IsNull(context.Zone.GetEntityCell(dead.PrimaryTarget));
        }

        [Test]
        public void PreparingSuccessiveCases_PreservesRealFloorWithoutAccumulatingCopies()
        {
            var context = _harness.CreateContext(playerBlueprint: "Player");
            var cases = SpellFxShowcase.CreateCases();
            SpellFxShowcase.PrepareCase(context, cases[0]);
            Entity originalFloor = context.Zone.GetCell(38, 12).Objects.Single(e => e.BlueprintName == "StoneFloor");
            Assert.IsFalse(originalFloor.HasTag("Floor"), "The shipped blueprint inherits Terrain, not a Floor tag.");
            SpellFxShowcase.PrepareCase(context, cases[1]);
            Assert.AreSame(originalFloor, context.Zone.GetCell(38, 12).Objects.Single(e => e.BlueprintName == "StoneFloor"));
            Assert.AreEqual((34, 12), context.Zone.GetEntityPosition(context.PlayerEntity));
            for (int y = 3; y <= 21; y++)
                for (int x = 28; x <= 54; x++)
                    Assert.AreEqual(1, context.Zone.GetCell(x, y).Objects.Count(e => e.BlueprintName == "StoneFloor"),
                        $"Visible arena ground at ({x}, {y})");
            Assert.AreEqual(1, context.Zone.GetCell(8, 12).Objects.Count(e => e.BlueprintName == "StoneFloor"));
        }
    }
}
