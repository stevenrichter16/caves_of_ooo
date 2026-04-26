using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    public class HouseDramaRuntimeTests
    {
        private const string DramaId = "TestDrama";

        private static HouseDramaData BuildMinimalDrama(string id = DramaId)
        {
            return new HouseDramaData
            {
                ID = id,
                Name = "Test Drama",
                PressurePoints = new List<PressurePointData>
                {
                    new PressurePointData
                    {
                        Id = "PP1",
                        Paths = new List<PathData>
                        {
                            new PathData { Id = "PathA", EmotionalCostMagnitude = 2, CorruptionContribution = 1,
                                Costs = new List<CostData> { new CostData { Magnitude = 3 } } },
                            new PathData { Id = "PathB", EmotionalCostMagnitude = 1, CorruptionContribution = 0,
                                Costs = new List<CostData>() }
                        }
                    },
                    new PressurePointData
                    {
                        Id = "PP2",
                        Paths = new List<PathData>
                        {
                            new PathData { Id = "PathC", EmotionalCostMagnitude = 3, CorruptionContribution = 2,
                                Costs = new List<CostData>() }
                        }
                    }
                },
                WitnessMap = new WitnessMapData
                {
                    Entries = new List<WitnessEntryData>
                    {
                        new WitnessEntryData
                        {
                            SubjectId = "npc1",
                            Knows = new List<string> { "fact_alpha" }
                        }
                    }
                },
                EndStates = new List<EndStateData>
                {
                    new EndStateData
                    {
                        Id = "Restored",
                        PathSignature = new List<string> { "PathA", "PathC" }
                    },
                    new EndStateData
                    {
                        Id = "Corrupted",
                        PathSignature = new List<string> { "PathB" }
                    }
                },
                Crossovers = new List<CrossoverEdgeData>
                {
                    new CrossoverEdgeData
                    {
                        FromPointId   = "PP1",
                        FromCondition = "state:resolved",
                        ToEffect      = "reveal:npc2:fact_beta"
                    }
                }
            };
        }

        [SetUp]
        public void Setup()
        {
            HouseDramaRuntime.Reset();
            HouseDramaLoader.Reset();
        }

        // ── Registration ──────────────────────────────────────────────────────

        [Test]
        public void RegisterDrama_SeedsPressurePointsAsDormant()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());

            Assert.AreEqual("dormant", HouseDramaRuntime.GetPressurePointState(DramaId, "PP1"));
            Assert.AreEqual("dormant", HouseDramaRuntime.GetPressurePointState(DramaId, "PP2"));
        }

        [Test]
        public void RegisterDrama_SeedsWitnessKnowledge()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());

            Assert.IsTrue(HouseDramaRuntime.WitnessKnows(DramaId, "npc1", "fact_alpha"));
            Assert.IsFalse(HouseDramaRuntime.WitnessKnows(DramaId, "npc1", "fact_unknown"));
        }

        [Test]
        public void RegisterDrama_Null_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => HouseDramaRuntime.RegisterDrama(null));
        }

        // ── Activation ───────────────────────────────────────────────────────

        [Test]
        public void ActivateDrama_SetsDormantPointsToActive()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());
            HouseDramaRuntime.ActivateDrama(DramaId);

            Assert.IsTrue(HouseDramaRuntime.IsDramaActive(DramaId));
            Assert.AreEqual("active", HouseDramaRuntime.GetPressurePointState(DramaId, "PP1"));
            Assert.AreEqual("active", HouseDramaRuntime.GetPressurePointState(DramaId, "PP2"));
        }

        [Test]
        public void IsDramaActive_ReturnsFalseBeforeActivation()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());
            Assert.IsFalse(HouseDramaRuntime.IsDramaActive(DramaId));
        }

        [Test]
        public void IsDramaActive_ReturnsFalseForUnknownDrama()
        {
            Assert.IsFalse(HouseDramaRuntime.IsDramaActive("ghost_drama"));
        }

        // ── Pressure Point Advancement ────────────────────────────────────────

        [Test]
        public void AdvancePressurePoint_UpdatesState()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());
            HouseDramaRuntime.ActivateDrama(DramaId);

            HouseDramaRuntime.AdvancePressurePoint(DramaId, "PP1", "resolved", "PathA");

            Assert.AreEqual("resolved", HouseDramaRuntime.GetPressurePointState(DramaId, "PP1"));
            Assert.AreEqual("PathA",    HouseDramaRuntime.GetPathTaken(DramaId, "PP1"));
        }

        [Test]
        public void AdvancePressurePoint_AccumulatesCorruption()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());
            HouseDramaRuntime.ActivateDrama(DramaId);

            // PathA: CorruptionContribution=1 → total 1 (EmotionalCostMagnitude no longer feeds corruption)
            HouseDramaRuntime.AdvancePressurePoint(DramaId, "PP1", "resolved", "PathA");

            Assert.AreEqual(1, HouseDramaRuntime.GetCorruption(DramaId));
        }

        [Test]
        public void AdvancePressurePoint_FiresCrossoverEffect()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());
            HouseDramaRuntime.ActivateDrama(DramaId);

            // Resolving PP1 should fire: reveal:npc2:fact_beta
            HouseDramaRuntime.AdvancePressurePoint(DramaId, "PP1", "resolved");

            Assert.IsTrue(HouseDramaRuntime.WitnessKnows(DramaId, "npc2", "fact_beta"));
        }

        [Test]
        public void AdvancePressurePoint_UnknownDrama_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                HouseDramaRuntime.AdvancePressurePoint("ghost_drama", "PP1", "resolved"));
        }

        // ── Witness Knowledge ─────────────────────────────────────────────────

        [Test]
        public void RevealWitnessFact_AddsToExistingEntry()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());

            HouseDramaRuntime.RevealWitnessFact(DramaId, "npc1", "fact_new");

            Assert.IsTrue(HouseDramaRuntime.WitnessKnows(DramaId, "npc1", "fact_alpha"));
            Assert.IsTrue(HouseDramaRuntime.WitnessKnows(DramaId, "npc1", "fact_new"));
        }

        [Test]
        public void RevealWitnessFact_CreatesNewEntry()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());

            HouseDramaRuntime.RevealWitnessFact(DramaId, "npc_new", "fact_x");

            Assert.IsTrue(HouseDramaRuntime.WitnessKnows(DramaId, "npc_new", "fact_x"));
        }

        // ── Corruption ───────────────────────────────────────────────────────

        [Test]
        public void AddCorruption_AccumulatesPositive()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());
            HouseDramaRuntime.AddCorruption(DramaId, 5);
            HouseDramaRuntime.AddCorruption(DramaId, 3);

            Assert.AreEqual(8, HouseDramaRuntime.GetCorruption(DramaId));
        }

        [Test]
        public void AddCorruption_NeverGoesNegative()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());
            HouseDramaRuntime.AddCorruption(DramaId, -99);

            Assert.AreEqual(0, HouseDramaRuntime.GetCorruption(DramaId));
        }

        // ── Min Corruption Path Cost ──────────────────────────────────────────

        [Test]
        public void ComputeMinCorruptionPathCost_SelectsCheapestPerPoint()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());

            // PP1: PathA cost = EmotionalCost(2) + Mechanical(3) = 5
            //      PathB cost = EmotionalCost(1) + Mechanical(0) = 1  ← min
            // PP2: PathC cost = EmotionalCost(3) + Mechanical(0) = 3
            // Total min = 1 + 3 = 4
            int cost = HouseDramaRuntime.ComputeMinCorruptionPathCost(DramaId);

            Assert.AreEqual(4, cost);
        }

        // ── Closed Paths ─────────────────────────────────────────────────────

        [Test]
        public void ClosePath_MarksPathClosed()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());
            HouseDramaRuntime.ClosePath(DramaId, "PP1", "PathA");

            Assert.IsTrue(HouseDramaRuntime.IsPathClosed(DramaId, "PP1", "PathA"));
            Assert.IsFalse(HouseDramaRuntime.IsPathClosed(DramaId, "PP1", "PathB"));
        }

        // ── End State Evaluation ─────────────────────────────────────────────

        [Test]
        public void EvaluateEndState_MatchesSignatureSubset()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());
            HouseDramaRuntime.ActivateDrama(DramaId);

            HouseDramaRuntime.AdvancePressurePoint(DramaId, "PP1", "resolved", "PathA");
            HouseDramaRuntime.AdvancePressurePoint(DramaId, "PP2", "resolved", "PathC");

            string endState = HouseDramaRuntime.EvaluateEndState(DramaId);

            Assert.AreEqual("Restored", endState);
        }

        [Test]
        public void EvaluateEndState_ReturnsNullWhenNoMatch()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());
            HouseDramaRuntime.ActivateDrama(DramaId);

            string endState = HouseDramaRuntime.EvaluateEndState(DramaId);

            Assert.IsNull(endState);
        }

        [Test]
        public void EvaluateEndState_MatchesCorruptedSignature()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());
            HouseDramaRuntime.ActivateDrama(DramaId);

            HouseDramaRuntime.AdvancePressurePoint(DramaId, "PP1", "resolved", "PathB");

            string endState = HouseDramaRuntime.EvaluateEndState(DramaId);

            Assert.AreEqual("Corrupted", endState);
        }

        // ── Crossover Effects ─────────────────────────────────────────────────

        [Test]
        public void ApplyCrossoverEffect_State_AdvancesPoint()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());
            HouseDramaRuntime.ActivateDrama(DramaId);

            HouseDramaRuntime.ApplyCrossoverEffect(DramaId, "state:PP2:failed");

            Assert.AreEqual("failed", HouseDramaRuntime.GetPressurePointState(DramaId, "PP2"));
        }

        [Test]
        public void ApplyCrossoverEffect_Reveal_AddsWitnessKnowledge()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());

            HouseDramaRuntime.ApplyCrossoverEffect(DramaId, "reveal:npc1:fact_beta");

            Assert.IsTrue(HouseDramaRuntime.WitnessKnows(DramaId, "npc1", "fact_beta"));
        }

        [Test]
        public void ApplyCrossoverEffect_Corruption_AddsScore()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());

            HouseDramaRuntime.ApplyCrossoverEffect(DramaId, "corruption:7");

            Assert.AreEqual(7, HouseDramaRuntime.GetCorruption(DramaId));
        }

        [Test]
        public void ApplyCrossoverEffect_Close_MarksPathClosed()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());

            HouseDramaRuntime.ApplyCrossoverEffect(DramaId, "close:PP1:PathA");

            Assert.IsTrue(HouseDramaRuntime.IsPathClosed(DramaId, "PP1", "PathA"));
        }

        // ── EvaluateEndState: Specificity (Fix 4) ────────────────────────────

        [Test]
        public void EvaluateEndState_PrefersMoreSpecificMatchOverFirstMatch()
        {
            // "Short" is listed first but has only 1 signature entry.
            // "Long" is listed second but has 2 signature entries.
            // Both are subsets of paths taken — Fix 4 must return "Long" (most specific).
            var drama = new HouseDramaData
            {
                ID = DramaId,
                EndStates = new List<EndStateData>
                {
                    new EndStateData { Id = "Short", PathSignature = new List<string> { "PathA" } },
                    new EndStateData { Id = "Long",  PathSignature = new List<string> { "PathA", "PathC" } },
                },
                PressurePoints = new List<PressurePointData>
                {
                    new PressurePointData { Id = "PP1" },
                    new PressurePointData { Id = "PP2" },
                }
            };
            HouseDramaRuntime.RegisterDrama(drama);
            HouseDramaRuntime.ActivateDrama(DramaId);
            HouseDramaRuntime.AdvancePressurePoint(DramaId, "PP1", "resolved", "PathA");
            HouseDramaRuntime.AdvancePressurePoint(DramaId, "PP2", "resolved", "PathC");

            Assert.AreEqual("Long", HouseDramaRuntime.EvaluateEndState(DramaId));
        }

        [Test]
        public void EvaluateEndState_WhenOnlyOneMatches_ReturnsIt()
        {
            // Sanity: when only the shorter end state matches, it is returned.
            var drama = new HouseDramaData
            {
                ID = DramaId,
                EndStates = new List<EndStateData>
                {
                    new EndStateData { Id = "Short", PathSignature = new List<string> { "PathA" } },
                    new EndStateData { Id = "Long",  PathSignature = new List<string> { "PathA", "PathC" } },
                },
                PressurePoints = new List<PressurePointData> { new PressurePointData { Id = "PP1" } }
            };
            HouseDramaRuntime.RegisterDrama(drama);
            HouseDramaRuntime.ActivateDrama(DramaId);
            HouseDramaRuntime.AdvancePressurePoint(DramaId, "PP1", "resolved", "PathA");
            // PathC not taken → "Long" does not match → "Short" is the only match

            Assert.AreEqual("Short", HouseDramaRuntime.EvaluateEndState(DramaId));
        }

        // ── Corruption: EmotionalCostMagnitude excluded (Fix 5) ──────────────

        [Test]
        public void AdvancePressurePoint_ZeroCorruptionContribution_AddsNoCorruption()
        {
            // PathB: CorruptionContribution=0, EmotionalCostMagnitude=1.
            // Before Fix 5 the emotional cost was added too; now only CorruptionContribution counts.
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());
            HouseDramaRuntime.ActivateDrama(DramaId);

            HouseDramaRuntime.AdvancePressurePoint(DramaId, "PP1", "resolved", "PathB");

            Assert.AreEqual(0, HouseDramaRuntime.GetCorruption(DramaId));
        }

        [Test]
        public void AdvancePressurePoint_MultiplePathsAccumulateOnlyCorruptionContribution()
        {
            // PathA: CorruptionContribution=1, EmotionalCostMagnitude=2 → adds 1
            // PathC: CorruptionContribution=2, EmotionalCostMagnitude=3 → adds 2
            // Total should be 3 (not 1+2+2+3=8 as it was before Fix 5).
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());
            HouseDramaRuntime.ActivateDrama(DramaId);

            HouseDramaRuntime.AdvancePressurePoint(DramaId, "PP1", "resolved", "PathA");
            HouseDramaRuntime.AdvancePressurePoint(DramaId, "PP2", "resolved", "PathC");

            Assert.AreEqual(3, HouseDramaRuntime.GetCorruption(DramaId));
        }

        // ── IsDramaRegistered (Issue 10) ─────────────────────────────────────

        [Test]
        public void IsDramaRegistered_ReturnsTrueAfterRegisterDrama()
        {
            // RED: IsDramaRegistered does not exist yet on HouseDramaRuntime.
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());

            Assert.IsTrue(HouseDramaRuntime.IsDramaRegistered(DramaId));
        }

        [Test]
        public void IsDramaRegistered_ReturnsFalseForUnknownDrama()
        {
            Assert.IsFalse(HouseDramaRuntime.IsDramaRegistered("ghost_drama"));
        }

        [Test]
        public void IsDramaRegistered_ReturnsTrueBeforeActivation()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());
            // Registered but not yet activated
            Assert.IsFalse(HouseDramaRuntime.IsDramaActive(DramaId));

            Assert.IsTrue(HouseDramaRuntime.IsDramaRegistered(DramaId));
        }

        [Test]
        public void IsDramaRegistered_ReturnsFalseAfterReset()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());
            HouseDramaRuntime.Reset();

            Assert.IsFalse(HouseDramaRuntime.IsDramaRegistered(DramaId));
        }

        // ── Reset ────────────────────────────────────────────────────────────

        [Test]
        public void Reset_ClearsAllState()
        {
            HouseDramaRuntime.RegisterDrama(BuildMinimalDrama());
            HouseDramaRuntime.ActivateDrama(DramaId);
            HouseDramaRuntime.Reset();

            Assert.IsFalse(HouseDramaRuntime.IsDramaActive(DramaId));
            Assert.AreEqual(0, HouseDramaRuntime.GetAllDramaIds().Count);
        }
    }
}
