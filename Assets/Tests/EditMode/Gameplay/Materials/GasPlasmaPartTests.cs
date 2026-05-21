using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// G.8e — GasPlasmaPart + CoatedInPlasmaEffect (gas-as-coat hybrid).
    /// The interesting mechanics worth dedicated tests:
    ///   (a) Triple-resistance penalty (-100 Heat/Cold/Electric) with
    ///       capture/restore (StatsApplied bool, not -1 sentinel).
    ///   (b) Burns off liquid coatings on apply.
    ///   (c) Duration scales with cloud density.
    ///   (d) Refresh-on-reapply takes the LARGER duration (opposite of
    ///       FungalInfection's preserve-clock).
    /// </summary>
    public class GasPlasmaPartTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""plasma-gas"", ""GasType"":""Plasma"",
                ""Glyph"":""°"", ""Color"":""&R"",
                ""DefaultDensity"":100, ""DefaultLevel"":1,
                ""BehaviorKind"":""Plasma"" } ] }");
            // Liquid registry for the burn-off-liquid-coat test.
            LiquidRegistry.Initialize(@"{ ""Liquids"":[
              { ""Id"":""water"", ""Adjective"":""wet"",
                ""Fluidity"":30, ""Evaporativity"":20 } ] }");
        }

        [TearDown]
        public void TearDown()
        {
            GasRegistry.ResetForTests();
            LiquidRegistry.ResetForTests();
            GasPlasmaPart.TestRng = null;
        }

        private static Entity MakeCreature(Zone zone, int x, int y, int hpMax = 200)
        {
            var e = new Entity { ID = "c_" + x + "_" + y, BlueprintName = "TestCreature" };
            e.Tags["Creature"] = "";
            void S(string n, int v, int max = 400) => e.Statistics[n] =
                new Stat { Owner = e, Name = n, BaseValue = v, Min = -200, Max = max };
            S("Hitpoints", hpMax, hpMax); S("Toughness", 12);
            S("Agility", 14); S("DV", 6); S("AV", 0);
            S("HeatResistance", 0); S("ColdResistance", 0); S("ElectricResistance", 0);
            e.AddPart(new RenderPart { DisplayName = "c" });
            e.AddPart(new StatusEffectsPart());
            zone.AddEntity(e, x, y);
            return e;
        }

        // ════════════════════════════════════════════════════════════
        //   PART I — ComputeDuration (pure)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void ComputeDuration_Density100_InRange40To60()
        {
            // density*2/5 = 40, density*3/5 = 60. With a seeded RNG,
            // result is in [40, 60].
            var rng = new System.Random(42);
            for (int i = 0; i < 50; i++)
            {
                int d = GasPlasmaPart.ComputeDuration(100, rng);
                Assert.GreaterOrEqual(d, 40);
                Assert.LessOrEqual(d, 60);
            }
        }

        [Test]
        public void ComputeDuration_ZeroDensity_ReturnsZero()
        {
            Assert.AreEqual(0, GasPlasmaPart.ComputeDuration(0, new System.Random(42)));
        }

        [Test]
        public void ComputeDuration_LowDensity_FlooredAtOne()
        {
            // density 1 → min = 0, max = 0 → floored to 1.
            Assert.AreEqual(1, GasPlasmaPart.ComputeDuration(1, new System.Random(42)));
        }

        // ════════════════════════════════════════════════════════════
        //   PART II — CoatedInPlasmaEffect direct tests
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Coat_OnApply_AppliesTripleResistancePenalty()
        {
            var creature = MakeCreature(new Zone("CoatStats"), 5, 5);
            // Start with some positive resistance to verify the shift
            // is relative, not absolute.
            creature.GetStat("HeatResistance").BaseValue = 20;
            creature.GetStat("ColdResistance").BaseValue = 10;
            creature.GetStat("ElectricResistance").BaseValue = 5;

            creature.ApplyEffect(new CoatedInPlasmaEffect(duration: 10));

            Assert.AreEqual(20 - 100, creature.GetStatValue("HeatResistance"),
                "Heat: prior - 100");
            Assert.AreEqual(10 - 100, creature.GetStatValue("ColdResistance"));
            Assert.AreEqual(5 - 100, creature.GetStatValue("ElectricResistance"));
        }

        [Test]
        public void Coat_OnRemove_RestoresResistances()
        {
            var creature = MakeCreature(new Zone("CoatRestore"), 5, 5);
            creature.GetStat("HeatResistance").BaseValue = 20;
            creature.ApplyEffect(new CoatedInPlasmaEffect(duration: 10));
            Assert.AreEqual(-80, creature.GetStatValue("HeatResistance"), "precondition");

            creature.GetPart<StatusEffectsPart>().RemoveEffect<CoatedInPlasmaEffect>();
            Assert.AreEqual(20, creature.GetStatValue("HeatResistance"),
                "restored to prior on removal");
            Assert.AreEqual(0, creature.GetStatValue("ColdResistance"));
            Assert.AreEqual(0, creature.GetStatValue("ElectricResistance"));
        }

        [Test]
        public void Coat_NegativePriorResistance_RoundTripsCorrectly()
        {
            // The StatsApplied bool (not -1 sentinel) means a creature
            // with a genuinely negative resistance restores correctly.
            var creature = MakeCreature(new Zone("CoatNegPrior"), 5, 5);
            creature.GetStat("ColdResistance").BaseValue = -1; // would be a sentinel false-positive
            var fx = new CoatedInPlasmaEffect(duration: 10);
            creature.ApplyEffect(fx);
            Assert.AreEqual(-101, creature.GetStatValue("ColdResistance"),
                "-1 prior - 100 = -101");
            creature.GetPart<StatusEffectsPart>().RemoveEffect<CoatedInPlasmaEffect>();
            Assert.AreEqual(-1, creature.GetStatValue("ColdResistance"),
                "restored to the genuinely-negative prior (not corrupted by sentinel logic)");
        }

        [Test]
        public void Coat_OnApply_BurnsOffLiquidCoat()
        {
            // Plasma vaporizes a liquid coat (Qud: RemoveAllEffects<LiquidCovered>).
            var creature = MakeCreature(new Zone("CoatBurnLiquid"), 5, 5);
            creature.ApplyEffect(new LiquidCoveredEffect("water", 30));
            Assert.IsNotNull(creature.GetEffect<LiquidCoveredEffect>(), "precondition: wet");

            creature.ApplyEffect(new CoatedInPlasmaEffect(duration: 10));

            Assert.IsNull(creature.GetEffect<LiquidCoveredEffect>(),
                "plasma coat burned off the liquid coat");
        }

        [Test]
        public void Coat_OnStack_TakesLargerDuration()
        {
            // Refresh-on-reapply: larger Duration wins (Qud parity).
            var fx1 = new CoatedInPlasmaEffect(duration: 10);
            var fx2 = new CoatedInPlasmaEffect(duration: 25);
            bool stacked = fx1.OnStack(fx2);
            Assert.IsTrue(stacked);
            Assert.AreEqual(25, fx1.Duration, "larger duration wins");
        }

        [Test]
        public void Coat_OnStack_SmallerDuration_DoesNotShrink()
        {
            var fx1 = new CoatedInPlasmaEffect(duration: 25);
            var fx2 = new CoatedInPlasmaEffect(duration: 5);
            fx1.OnStack(fx2);
            Assert.AreEqual(25, fx1.Duration, "smaller incoming doesn't shrink");
        }

        [Test]
        public void Coat_OnStack_DoesNotReApplyStats_NoDoubleShift()
        {
            // CRITICAL: stacking must NOT re-apply the resistance shift
            // (would stack to -200). The existing instance keeps its
            // single -100 shift; only Duration updates.
            var creature = MakeCreature(new Zone("CoatNoDoubleShift"), 5, 5);
            var fx1 = new CoatedInPlasmaEffect(duration: 10);
            creature.ApplyEffect(fx1);
            Assert.AreEqual(-100, creature.GetStatValue("HeatResistance"), "precondition: single shift");

            // Re-apply via the StatusEffectsPart (routes to OnStack).
            creature.ApplyEffect(new CoatedInPlasmaEffect(duration: 30));

            Assert.AreEqual(-100, creature.GetStatValue("HeatResistance"),
                "still single -100 shift (not -200) after restack");
            Assert.AreEqual(30, creature.GetEffect<CoatedInPlasmaEffect>().Duration,
                "duration updated to the larger value");
        }

        // ════════════════════════════════════════════════════════════
        //   PART III — GasPlasmaPart dispatch
        // ════════════════════════════════════════════════════════════

        [Test]
        public void SpawnGas_BehaviorKindPlasma_AttachesGasPlasmaPart()
        {
            var zone = new Zone("PlasmaFactory");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "plasma-gas");
            Assert.IsNotNull(gas.GetPart<GasPlasmaPart>());
            Assert.IsNotNull(gas.GetPart<IObjectGasBehaviorPart>());
        }

        [Test]
        public void PlasmaGas_ApplyGas_AppliesCoatedInPlasma()
        {
            var zone = new Zone("PlasmaApply");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "plasma-gas", density: 100);
            var target = MakeCreature(zone, 5, 5);

            bool applied = gas.GetPart<GasPlasmaPart>().ApplyGas(target, zone);

            Assert.IsTrue(applied);
            Assert.IsNotNull(target.GetEffect<CoatedInPlasmaEffect>());
            Assert.AreEqual(-100, target.GetStatValue("HeatResistance"),
                "coat applied the resistance penalty");
        }

        [Test]
        public void PlasmaGas_Duration_ScalesWithDensity()
        {
            // Density 200 → range 80-120. Density 50 → range 20-30.
            // Pin that denser cloud → longer coat (compare two seeds).
            GasPlasmaPart.TestRng = new System.Random(1);
            var zoneHigh = new Zone("PlasmaHighDensity");
            var gasHigh = GasFactory.SpawnGas(zoneHigh, 5, 5, "plasma-gas", density: 200);
            var tHigh = MakeCreature(zoneHigh, 5, 5);
            gasHigh.GetPart<GasPlasmaPart>().ApplyGas(tHigh, zoneHigh);
            int highDur = tHigh.GetEffect<CoatedInPlasmaEffect>().Duration;

            GasPlasmaPart.TestRng = new System.Random(1);
            var zoneLow = new Zone("PlasmaLowDensity");
            var gasLow = GasFactory.SpawnGas(zoneLow, 5, 5, "plasma-gas", density: 50);
            var tLow = MakeCreature(zoneLow, 5, 5);
            gasLow.GetPart<GasPlasmaPart>().ApplyGas(tLow, zoneLow);
            int lowDur = tLow.GetEffect<CoatedInPlasmaEffect>().Duration;

            Assert.Greater(highDur, lowDur,
                $"denser cloud → longer coat (high={highDur}, low={lowDur})");
        }

        [Test]
        public void PlasmaGas_Reapply_ExtendsCoat_NotDoubleShift()
        {
            // Integration: walking into a second (denser) plasma cloud
            // extends the coat duration but doesn't double the
            // resistance shift.
            var zone = new Zone("PlasmaReapply");
            var weakGas = GasFactory.SpawnGas(zone, 5, 5, "plasma-gas", density: 50);
            var strongGas = GasFactory.SpawnGas(zone, 6, 5, "plasma-gas", density: 200);
            var target = MakeCreature(zone, 5, 5);

            GasPlasmaPart.TestRng = new System.Random(7);
            weakGas.GetPart<GasPlasmaPart>().ApplyGas(target, zone);
            int dur1 = target.GetEffect<CoatedInPlasmaEffect>().Duration;

            GasPlasmaPart.TestRng = new System.Random(7);
            strongGas.GetPart<GasPlasmaPart>().ApplyGas(target, zone);
            int dur2 = target.GetEffect<CoatedInPlasmaEffect>().Duration;

            Assert.GreaterOrEqual(dur2, dur1,
                "denser re-coat extends (or holds) duration");
            Assert.AreEqual(-100, target.GetStatValue("HeatResistance"),
                "still single -100 shift after re-coat (no -200)");
        }

        [Test]
        public void PlasmaGas_GasImmunity_Vetoes()
        {
            var zone = new Zone("PlasmaImmunity");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "plasma-gas");
            var target = MakeCreature(zone, 5, 5);
            target.AddPart(new GasImmunityPart { GasType = "Plasma" });
            bool applied = gas.GetPart<GasPlasmaPart>().ApplyGas(target, zone);
            Assert.IsFalse(applied);
            Assert.IsNull(target.GetEffect<CoatedInPlasmaEffect>());
            Assert.AreEqual(0, target.GetStatValue("HeatResistance"), "no shift");
        }

        [Test]
        public void PlasmaGas_PerTurnDispatch_FromGasSystem()
        {
            var zone = new Zone("PlasmaTickDispatch");
            GasFactory.SpawnGas(zone, 5, 5, "plasma-gas", density: 500);
            var target = MakeCreature(zone, 5, 5);
            GasSystem.OnTickEnd(zone);
            Assert.IsNotNull(target.GetEffect<CoatedInPlasmaEffect>());
        }

        [Test]
        public void PlasmaGas_EmitsAppliedDiag()
        {
            GasPlasmaPart.TestRng = new System.Random(3);
            var zone = new Zone("PlasmaDiag");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "plasma-gas", density: 100);
            var target = MakeCreature(zone, 5, 5);
            Diag.ResetAll();
            gas.GetPart<GasPlasmaPart>().ApplyGas(target, zone);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "Applied", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"gasId\":\"plasma-gas\"", recs[0].PayloadJson);
            StringAssert.Contains("\"gasType\":\"Plasma\"", recs[0].PayloadJson);
            StringAssert.Contains("\"density\":100", recs[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════════
        //   PART IV — Cross-system: amplifies elemental damage
        // ════════════════════════════════════════════════════════════

        [Test]
        public void PlasmaCoated_TakesAmplifiedHeatDamage()
        {
            // The whole point: -100 HeatResistance means a Heat hit
            // deals ~double. Pin the cross-system interaction with
            // CombatSystem's resistance math.
            var zone = new Zone("PlasmaAmplify");
            var bare = MakeCreature(zone, 5, 5);
            var coated = MakeCreature(zone, 6, 5);
            coated.ApplyEffect(new CoatedInPlasmaEffect(duration: 10));

            int bareHp0 = bare.GetStatValue("Hitpoints");
            int coatedHp0 = coated.GetStatValue("Hitpoints");
            var heatBare = new Damage(50); heatBare.AddAttribute("Heat");
            var heatCoated = new Damage(50); heatCoated.AddAttribute("Heat");
            CombatSystem.ApplyDamage(bare, heatBare, null, zone);
            CombatSystem.ApplyDamage(coated, heatCoated, null, zone);

            int bareDmg = bareHp0 - bare.GetStatValue("Hitpoints");
            int coatedDmg = coatedHp0 - coated.GetStatValue("Hitpoints");
            Assert.Greater(coatedDmg, bareDmg,
                $"plasma-coated takes amplified Heat damage (bare={bareDmg}, coated={coatedDmg})");
        }

        // ════════════════════════════════════════════════════════════
        //   PART V — Cross-type isolation + null safety
        // ════════════════════════════════════════════════════════════

        [Test]
        public void PlasmaGas_NullTarget_NoCrash()
        {
            var zone = new Zone("PlasmaNull");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "plasma-gas");
            Assert.DoesNotThrow(() => gas.GetPart<GasPlasmaPart>().ApplyGas(null, zone));
        }

        [Test]
        public void PlasmaGas_DoesNotApplyOtherEffects_Counter()
        {
            var zone = new Zone("PlasmaCounter");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "plasma-gas");
            var target = MakeCreature(zone, 5, 5);
            gas.GetPart<GasPlasmaPart>().ApplyGas(target, zone);
            Assert.IsNull(target.GetEffect<StunnedEffect>());
            Assert.IsNull(target.GetEffect<FrozenEffect>());
            Assert.IsNull(target.GetEffect<FungalInfectionEffect>());
            Assert.IsNull(target.GetEffect<PoisonedByGasEffect>());
        }

        // ════════════════════════════════════════════════════════════
        //   PART VI — Mutation-resistance (step g)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Coat_OnStack_NullIncoming_NoCrash()
        {
            // A buggy OnStack that dereferences incoming without a guard
            // would NRE here. Returns true (handled) + leaves Duration.
            var fx = new CoatedInPlasmaEffect(duration: 12);
            bool stacked = false;
            Assert.DoesNotThrow(() => stacked = fx.OnStack(null));
            Assert.IsTrue(stacked);
            Assert.AreEqual(12, fx.Duration, "null incoming doesn't change duration");
        }

        [Test]
        public void Coat_OnRemove_WithoutApply_NoShift_NoCrash()
        {
            // StatsApplied guard: OnRemove must be a no-op if OnApply
            // never ran (a buggy impl that restored unconditionally would
            // write a stale/zero "prior" over a live resistance).
            var creature = MakeCreature(new Zone("CoatRemoveNoApply"), 5, 5);
            creature.GetStat("HeatResistance").BaseValue = 33;
            var fx = new CoatedInPlasmaEffect(duration: 10);
            Assert.DoesNotThrow(() => fx.OnRemove(creature));
            Assert.AreEqual(33, creature.GetStatValue("HeatResistance"),
                "no shift written when stats were never applied");
        }

        [Test]
        public void ComputeDuration_NegativeDensity_ReturnsZero()
        {
            // density <= 0 guard — a grenade fizzle or a decayed cloud
            // must not produce a negative/garbage coat length.
            Assert.AreEqual(0, GasPlasmaPart.ComputeDuration(-50, new System.Random(42)));
        }

        [Test]
        public void Coat_DoubleApplyStatsViaTwoInstances_OnlySingleShiftOnSecond()
        {
            // Adversarial: two SEPARATE coat instances applied in
            // sequence. The first shifts -100; the second routes through
            // OnStack (same type already present) and must NOT shift
            // again. Counter to the "double-shift to -200" bug class.
            var creature = MakeCreature(new Zone("CoatDoubleInstance"), 5, 5);
            creature.ApplyEffect(new CoatedInPlasmaEffect(duration: 5));
            creature.ApplyEffect(new CoatedInPlasmaEffect(duration: 5));
            Assert.AreEqual(-100, creature.GetStatValue("ColdResistance"),
                "two equal-duration coats still net a single -100 shift");
            // And removing the (single) live instance fully restores.
            creature.GetPart<StatusEffectsPart>().RemoveEffect<CoatedInPlasmaEffect>();
            Assert.AreEqual(0, creature.GetStatValue("ColdResistance"),
                "single removal restores fully (no residual -100)");
        }
    }
}
