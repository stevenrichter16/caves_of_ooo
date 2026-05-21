using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// G.8d.2 — GasFungalSporesPart tests. Applies the multi-stage
    /// FungalInfectionEffect (G.8d.1) via the shared filter chain.
    /// Has two non-standard mechanics worth dedicated tests:
    ///   (a) Refresh-on-reapply preserves the stage clock (Qud parity
    ///       invariant — re-exposure doesn't reset progression).
    ///   (b) Infection chance is deterministic by GasLevel vs target
    ///       Toughness (CoO simplification of Qud's Toughness save).
    /// </summary>
    public class GasFungalSporesPartTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""fungal-spores"", ""GasType"":""FungalSpores"",
                ""Glyph"":""°"", ""Color"":""&G"",
                ""DefaultDensity"":80, ""DefaultLevel"":1,
                ""BehaviorKind"":""FungalSpores"" } ] }");
        }

        [TearDown]
        public void TearDown()
        {
            GasRegistry.ResetForTests();
            GasFungalSporesPart.TestRng = null;
        }

        private static Entity MakeCreature(Zone zone, int x, int y,
            int hpMax = 200, int toughness = 14)
        {
            var e = new Entity { ID = "c_" + x + "_" + y, BlueprintName = "TestCreature" };
            e.Tags["Creature"] = "";
            void S(string n, int v, int max = 400) => e.Statistics[n] =
                new Stat { Owner = e, Name = n, BaseValue = v, Min = -200, Max = max };
            S("Hitpoints", hpMax, hpMax);
            S("Toughness", toughness);
            S("Agility", 14); S("DV", 6); S("AV", 0);
            e.AddPart(new RenderPart { DisplayName = "c" });
            e.AddPart(new StatusEffectsPart());
            zone.AddEntity(e, x, y);
            return e;
        }

        // ════════════════════════════════════════════════════════════
        //   PART I — ComputeInfectionChance (pure function)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Chance_Level1_Tough14_DefaultCase()
        {
            // 30 + 10 - 28 = 12
            Assert.AreEqual(12, GasFungalSporesPart.ComputeInfectionChance(1, 14));
        }

        [Test]
        public void Chance_HighLevel_HighChance()
        {
            // Level 5 vs Tough 10 → 30 + 50 - 20 = 60
            Assert.AreEqual(60, GasFungalSporesPart.ComputeInfectionChance(5, 10));
        }

        [Test]
        public void Chance_HighToughness_FloorAtZero()
        {
            // Level 1 vs Tough 100 → 30 + 10 - 200 → clamp at 0
            Assert.AreEqual(0, GasFungalSporesPart.ComputeInfectionChance(1, 100));
        }

        [Test]
        public void Chance_VeryHighLevel_CeilingAtHundred()
        {
            // Level 20 vs Tough 0 → 30 + 200 - 0 → clamp at 100
            Assert.AreEqual(100, GasFungalSporesPart.ComputeInfectionChance(20, 0));
        }

        // ════════════════════════════════════════════════════════════
        //   PART II — Factory wiring
        // ════════════════════════════════════════════════════════════

        [Test]
        public void SpawnGas_BehaviorKindFungalSpores_AttachesGasFungalSporesPart()
        {
            var zone = new Zone("FungalFactory");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "fungal-spores");
            Assert.IsNotNull(gas.GetPart<GasFungalSporesPart>());
            Assert.IsNotNull(gas.GetPart<IObjectGasBehaviorPart>(),
                "picked up by GasSystem per-turn dispatch");
        }

        // ════════════════════════════════════════════════════════════
        //   PART III — ApplyGas: deterministic roll via TestRng
        // ════════════════════════════════════════════════════════════

        [Test]
        public void ApplyGas_LowRoll_AppliesFungalInfection()
        {
            // Chance for Level 1 vs Tough 14 = 12. A seeded RNG that
            // returns Next(100) = 5 < 12 → infection applies.
            GasFungalSporesPart.TestRng = new SequenceRng(new int[] { 5 });
            var zone = new Zone("FungalApplyLow");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "fungal-spores", level: 1);
            var target = MakeCreature(zone, 5, 5, toughness: 14);

            bool applied = gas.GetPart<GasFungalSporesPart>().ApplyGas(target, zone);

            Assert.IsTrue(applied);
            Assert.IsNotNull(target.GetEffect<FungalInfectionEffect>(),
                "infection applied on low roll (5 < 12 chance)");
        }

        [Test]
        public void ApplyGas_HighRoll_DoesNotInfect()
        {
            // RNG returns 50 >= 12 → no infection.
            GasFungalSporesPart.TestRng = new SequenceRng(new int[] { 50 });
            var zone = new Zone("FungalApplyHigh");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "fungal-spores", level: 1);
            var target = MakeCreature(zone, 5, 5, toughness: 14);

            bool applied = gas.GetPart<GasFungalSporesPart>().ApplyGas(target, zone);

            Assert.IsFalse(applied);
            Assert.IsNull(target.GetEffect<FungalInfectionEffect>());
        }

        [Test]
        public void ApplyGas_NonCreature_VetoedBeforeRoll()
        {
            // Filter chain rejects before the chance roll even happens.
            // Verify by setting RNG to a guaranteed-infect roll AND
            // confirming the item still isn't infected.
            GasFungalSporesPart.TestRng = new SequenceRng(new int[] { 0 });
            var zone = new Zone("FungalNonCreature");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "fungal-spores");
            var item = new Entity { ID = "item", BlueprintName = "Item" };
            item.AddPart(new RenderPart { DisplayName = "item" });
            item.AddPart(new StatusEffectsPart());
            zone.AddEntity(item, 5, 5);

            bool applied = gas.GetPart<GasFungalSporesPart>().ApplyGas(item, zone);

            Assert.IsFalse(applied);
            Assert.IsNull(item.GetEffect<FungalInfectionEffect>());
        }

        [Test]
        public void ApplyGas_HighToughness_NeverInfects_Counter()
        {
            // Counter: a Toughness-100 creature can never be infected
            // by Level-1 spores (chance floored at 0). Even with a roll
            // of 0, 0 < 0 is false → no infection.
            GasFungalSporesPart.TestRng = new SequenceRng(new int[] { 0 });
            var zone = new Zone("FungalImmuneByTough");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "fungal-spores", level: 1);
            var target = MakeCreature(zone, 5, 5, toughness: 100);

            bool applied = gas.GetPart<GasFungalSporesPart>().ApplyGas(target, zone);

            Assert.IsFalse(applied,
                "Toughness 100 vs Level 1 → 0% chance → never infects");
            Assert.IsNull(target.GetEffect<FungalInfectionEffect>());
        }

        [Test]
        public void ApplyGas_GasImmunity_Vetoes()
        {
            // G.6 integration: GasImmunityPart for "FungalSpores" vetoes.
            GasFungalSporesPart.TestRng = new SequenceRng(new int[] { 0 });
            var zone = new Zone("FungalImmunity");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "fungal-spores");
            var target = MakeCreature(zone, 5, 5);
            target.AddPart(new GasImmunityPart { GasType = "FungalSpores" });

            bool applied = gas.GetPart<GasFungalSporesPart>().ApplyGas(target, zone);

            Assert.IsFalse(applied);
            Assert.IsNull(target.GetEffect<FungalInfectionEffect>());
        }

        // ════════════════════════════════════════════════════════════
        //   PART IV — Refresh-on-reapply preserves stage clock
        // ════════════════════════════════════════════════════════════

        [Test]
        public void ApplyGas_AlreadyInfected_DoesNotResetStageClock()
        {
            // Critical Qud-parity invariant: walking through fresh
            // spores while at Stage Blooming stays at Blooming.
            GasFungalSporesPart.TestRng = new SequenceRng(new int[] { 0 });
            var zone = new Zone("FungalReapply");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "fungal-spores");
            var target = MakeCreature(zone, 5, 5);

            // Apply once → infection at Stage Incubation.
            gas.GetPart<GasFungalSporesPart>().ApplyGas(target, zone);
            var fx = target.GetEffect<FungalInfectionEffect>();
            Assert.IsNotNull(fx, "precondition: infected");
            fx.TurnsInfected = 22; // simulate progression to Blooming

            // Apply AGAIN with guaranteed-infect roll → should NOT reset.
            GasFungalSporesPart.TestRng = new SequenceRng(new int[] { 0 });
            gas.GetPart<GasFungalSporesPart>().ApplyGas(target, zone);

            var fxAfter = target.GetEffect<FungalInfectionEffect>();
            Assert.AreSame(fx, fxAfter, "same instance — not replaced");
            Assert.AreEqual(22, fxAfter.TurnsInfected,
                "stage clock preserved (no reset to 0)");
            Assert.AreEqual(FungalInfectionEffect.InfectionStage.Blooming, fxAfter.CurrentStage);
        }

        // ════════════════════════════════════════════════════════════
        //   PART V — Per-turn dispatch + diag observability
        // ════════════════════════════════════════════════════════════

        [Test]
        public void GasSystem_OnTickEnd_DispatchesFungalSpores()
        {
            // Force-infect via guaranteed roll → after one OnTickEnd,
            // creature has the infection effect.
            GasFungalSporesPart.TestRng = new SequenceRng(new int[] { 0, 0, 0, 0, 0 });
            var zone = new Zone("FungalTickDispatch");
            GasFactory.SpawnGas(zone, 5, 5, "fungal-spores", density: 500);
            var target = MakeCreature(zone, 5, 5);

            GasSystem.OnTickEnd(zone);

            Assert.IsNotNull(target.GetEffect<FungalInfectionEffect>());
        }

        [Test]
        public void ApplyGas_EmitsAppliedDiag()
        {
            GasFungalSporesPart.TestRng = new SequenceRng(new int[] { 0 });
            var zone = new Zone("FungalDiag");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "fungal-spores", level: 2);
            var target = MakeCreature(zone, 5, 5, toughness: 12);
            Diag.ResetAll();

            gas.GetPart<GasFungalSporesPart>().ApplyGas(target, zone);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "Applied", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"gasId\":\"fungal-spores\"", recs[0].PayloadJson);
            StringAssert.Contains("\"gasType\":\"FungalSpores\"", recs[0].PayloadJson);
            // 30 + 20 - 24 = 26
            StringAssert.Contains("\"chance\":26", recs[0].PayloadJson);
            StringAssert.Contains("\"infected\":true", recs[0].PayloadJson);
        }

        [Test]
        public void ApplyGas_FailedRoll_EmitsRollFailedDiag()
        {
            // Counter / observability: a failed roll emits a different
            // diag kind so post-hoc audits can tell "the creature was
            // exposed but the chance roll didn't hit" apart from
            // "the gas was vetoed at an earlier gate."
            GasFungalSporesPart.TestRng = new SequenceRng(new int[] { 99 });
            var zone = new Zone("FungalFailed");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "fungal-spores");
            var target = MakeCreature(zone, 5, 5);
            Diag.ResetAll();

            gas.GetPart<GasFungalSporesPart>().ApplyGas(target, zone);

            var rolledRecs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "Applied", Limit = 5 }).Records;
            // Either: emit Applied with infected=false, OR a separate
            // RollFailed kind. The diag's job is "show me what happened"
            // — pin that SOME diag fires with infected=false when the
            // roll fails. Implementation choice.
            Assert.AreEqual(1, rolledRecs.Count);
            StringAssert.Contains("\"infected\":false", rolledRecs[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════════
        //   PART VI — Cross-type isolation + null safety
        // ════════════════════════════════════════════════════════════

        [Test]
        public void ApplyGas_NullTarget_NoCrash()
        {
            GasFungalSporesPart.TestRng = new SequenceRng(new int[] { 0 });
            var zone = new Zone("FungalNullTarget");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "fungal-spores");
            Assert.DoesNotThrow(() =>
                gas.GetPart<GasFungalSporesPart>().ApplyGas(null, zone));
        }

        [Test]
        public void ApplyGas_DoesNotApplyOtherEffects_Counter()
        {
            // Counter: spores don't apply the other gas effects.
            GasFungalSporesPart.TestRng = new SequenceRng(new int[] { 0 });
            var zone = new Zone("FungalCrossCounter");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "fungal-spores");
            var target = MakeCreature(zone, 5, 5);

            gas.GetPart<GasFungalSporesPart>().ApplyGas(target, zone);

            Assert.IsNull(target.GetEffect<StunnedEffect>());
            Assert.IsNull(target.GetEffect<ConfusedEffect>());
            Assert.IsNull(target.GetEffect<AsleepByGasEffect>());
            Assert.IsNull(target.GetEffect<PoisonedByGasEffect>());
            Assert.IsNull(target.GetEffect<FrozenEffect>());
        }
    }

    /// <summary>Test-support RNG: returns a fixed sequence of Next()
    /// values. Wraps modulo when the sequence is shorter than the call
    /// count (so we don't have to count exactly how many Next() calls
    /// the production code makes).</summary>
    internal class SequenceRng : System.Random
    {
        private readonly int[] _seq;
        private int _idx;
        public SequenceRng(int[] sequence) { _seq = sequence; _idx = 0; }
        public override int Next() => _seq[_idx++ % _seq.Length];
        public override int Next(int maxValue) => _seq[_idx++ % _seq.Length] % maxValue;
        public override int Next(int minValue, int maxValue)
        {
            int v = _seq[_idx++ % _seq.Length];
            return minValue + (v % (maxValue - minValue));
        }
    }
}
