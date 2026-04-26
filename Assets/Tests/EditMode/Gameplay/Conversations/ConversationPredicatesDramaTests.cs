using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Unit tests for the five House Drama conversation predicates and their
    /// auto-generated IfNot* inverses. Validates that each predicate correctly
    /// delegates to HouseDramaRuntime and that the IfNot* inversion built into
    /// ConversationPredicates.Evaluate works for the drama family.
    /// </summary>
    public class ConversationPredicatesDramaTests
    {
        private const string DramaId = "PredTestDrama";

        private static HouseDramaData BuildTestDrama() => new HouseDramaData
        {
            ID = DramaId,
            PressurePoints = new List<PressurePointData>
            {
                new PressurePointData { Id = "PP1" }
            }
        };

        [SetUp]
        public void Setup()
        {
            ConversationPredicates.Reset();
            HouseDramaRuntime.Reset();
            HouseDramaLoader.Reset();
        }

        // ── IfDramaActive ─────────────────────────────────────────────────────

        [Test]
        public void IfDramaActive_ReturnsTrueWhenActive()
        {
            var drama = BuildTestDrama();
            HouseDramaRuntime.RegisterDrama(drama);
            HouseDramaRuntime.ActivateDrama(DramaId);

            bool result = ConversationPredicates.Evaluate("IfDramaActive", null, null, DramaId);

            Assert.IsTrue(result);
        }

        [Test]
        public void IfDramaActive_ReturnsFalseWhenNotActive()
        {
            bool result = ConversationPredicates.Evaluate("IfDramaActive", null, null, DramaId);

            Assert.IsFalse(result);
        }

        [Test]
        public void IfNotDramaActive_InvertsResult()
        {
            // Drama not active → IfDramaActive=false → IfNotDramaActive=true
            bool result = ConversationPredicates.Evaluate("IfNotDramaActive", null, null, DramaId);

            Assert.IsTrue(result);
        }

        // ── IfPressurePointState ──────────────────────────────────────────────

        [Test]
        public void IfPressurePointState_ReturnsTrueWhenStateMatches()
        {
            HouseDramaRuntime.RegisterDrama(BuildTestDrama());
            HouseDramaRuntime.ActivateDrama(DramaId);
            HouseDramaRuntime.AdvancePressurePoint(DramaId, "PP1", "resolved");

            bool result = ConversationPredicates.Evaluate(
                "IfPressurePointState", null, null, $"{DramaId}:PP1:resolved");

            Assert.IsTrue(result);
        }

        [Test]
        public void IfPressurePointState_ReturnsFalseWhenStateMismatches()
        {
            HouseDramaRuntime.RegisterDrama(BuildTestDrama());
            HouseDramaRuntime.ActivateDrama(DramaId);

            bool result = ConversationPredicates.Evaluate(
                "IfPressurePointState", null, null, $"{DramaId}:PP1:resolved");

            Assert.IsFalse(result);
        }

        [Test]
        public void IfPressurePointState_TooFewArgParts_ReturnsFalse()
        {
            bool result = ConversationPredicates.Evaluate(
                "IfPressurePointState", null, null, "DramaID:PointID");

            Assert.IsFalse(result);
        }

        [Test]
        public void IfNotPressurePointState_InvertsWhenMismatches()
        {
            HouseDramaRuntime.RegisterDrama(BuildTestDrama());
            HouseDramaRuntime.ActivateDrama(DramaId);
            // PP1 is "active", not "resolved" → IfPressurePointState=false → IfNot=true

            bool result = ConversationPredicates.Evaluate(
                "IfNotPressurePointState", null, null, $"{DramaId}:PP1:resolved");

            Assert.IsTrue(result);
        }

        // ── IfPathTaken ───────────────────────────────────────────────────────

        [Test]
        public void IfPathTaken_ReturnsTrueWhenPathMatches()
        {
            HouseDramaRuntime.RegisterDrama(BuildTestDrama());
            HouseDramaRuntime.ActivateDrama(DramaId);
            HouseDramaRuntime.AdvancePressurePoint(DramaId, "PP1", "resolved", "PathA");

            bool result = ConversationPredicates.Evaluate(
                "IfPathTaken", null, null, $"{DramaId}:PP1:PathA");

            Assert.IsTrue(result);
        }

        [Test]
        public void IfPathTaken_ReturnsFalseWhenNoPathTaken()
        {
            HouseDramaRuntime.RegisterDrama(BuildTestDrama());
            HouseDramaRuntime.ActivateDrama(DramaId);

            bool result = ConversationPredicates.Evaluate(
                "IfPathTaken", null, null, $"{DramaId}:PP1:PathA");

            Assert.IsFalse(result);
        }

        // ── IfWitnessKnows ────────────────────────────────────────────────────

        [Test]
        public void IfWitnessKnows_ReturnsTrueWhenFactKnown()
        {
            HouseDramaRuntime.RegisterDrama(BuildTestDrama());
            HouseDramaRuntime.RevealWitnessFact(DramaId, "npc1", "secret_fact");

            bool result = ConversationPredicates.Evaluate(
                "IfWitnessKnows", null, null, $"{DramaId}:npc1:secret_fact");

            Assert.IsTrue(result);
        }

        [Test]
        public void IfWitnessKnows_ReturnsFalseWhenFactUnknown()
        {
            HouseDramaRuntime.RegisterDrama(BuildTestDrama());

            bool result = ConversationPredicates.Evaluate(
                "IfWitnessKnows", null, null, $"{DramaId}:npc1:secret_fact");

            Assert.IsFalse(result);
        }

        // ── IfCorruptionAtLeast ───────────────────────────────────────────────

        [Test]
        public void IfCorruptionAtLeast_ReturnsTrueWhenScoreMeetsThreshold()
        {
            HouseDramaRuntime.RegisterDrama(BuildTestDrama());
            HouseDramaRuntime.AddCorruption(DramaId, 5);

            bool result = ConversationPredicates.Evaluate(
                "IfCorruptionAtLeast", null, null, $"{DramaId}:5");

            Assert.IsTrue(result);
        }

        [Test]
        public void IfCorruptionAtLeast_ReturnsFalseWhenScoreBelowThreshold()
        {
            HouseDramaRuntime.RegisterDrama(BuildTestDrama());
            HouseDramaRuntime.AddCorruption(DramaId, 3);

            bool result = ConversationPredicates.Evaluate(
                "IfCorruptionAtLeast", null, null, $"{DramaId}:5");

            Assert.IsFalse(result);
        }

        [Test]
        public void IfNotCorruptionAtLeast_InvertsWhenBelowThreshold()
        {
            HouseDramaRuntime.RegisterDrama(BuildTestDrama());
            // Corruption is 0 < 5 → IfCorruptionAtLeast=false → IfNot=true

            bool result = ConversationPredicates.Evaluate(
                "IfNotCorruptionAtLeast", null, null, $"{DramaId}:5");

            Assert.IsTrue(result);
        }

        [Test]
        public void IfCorruptionAtLeast_MissingColon_ReturnsFalse()
        {
            bool result = ConversationPredicates.Evaluate(
                "IfCorruptionAtLeast", null, null, DramaId);

            Assert.IsFalse(result);
        }
    }
}
