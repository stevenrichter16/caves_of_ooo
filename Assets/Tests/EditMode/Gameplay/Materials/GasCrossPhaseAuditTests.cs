using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Comprehensive cross-phase gas audit (hypothesis-driven deep audit per
    /// CLAUDE.md). Each test states a player-flow / cross-system hypothesis
    /// the per-phase tests don't simulate, then either confirms a bug (RED)
    /// or pins the invariant as correct (GREEN = permanent regression infra).
    /// Surfaces probed: save/load reach of gas-effect STAT SHIFTS (not just
    /// fields), G.11 nav-immunity vs ApplyGas-immunity AGREEMENT, dispersal
    /// boundary at the low-density threshold, dangling Creator, wind at the
    /// zone edge, overlapping different-type gases.
    /// </summary>
    public class GasCrossPhaseAuditTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""ax-poison"", ""GasType"":""Poison"", ""Glyph"":""°"", ""Color"":""&g"",
                ""DefaultDensity"":50, ""DefaultLevel"":1, ""BehaviorKind"":""Poison"" },
              { ""Id"":""ax-stun"", ""GasType"":""Stun"", ""Glyph"":""°"", ""Color"":""&Y"",
                ""DefaultDensity"":50, ""DefaultLevel"":1, ""BehaviorKind"":""Stun"" },
              { ""Id"":""ax-plasma"", ""GasType"":""Plasma"", ""Glyph"":""°"", ""Color"":""&R"",
                ""DefaultDensity"":100, ""DefaultLevel"":1, ""BehaviorKind"":""Plasma"" } ] }");
            SettlementRuntime.ActiveZone = null;
        }

        [TearDown]
        public void TearDown()
        {
            GasRegistry.ResetForTests();
            GasSystem.SetRngForTests(null);
            SettlementRuntime.ActiveZone = null;
            GasPlasmaPart.TestRng = null;
        }

        private static Entity Creature(Zone z, int x, int y)
        {
            var e = new Entity { ID = "cr_" + x + "_" + y, BlueprintName = "AxCreature" };
            e.Tags["Creature"] = "";
            void S(string n, int v) => e.Statistics[n] =
                new Stat { Owner = e, Name = n, BaseValue = v, Min = -200, Max = 100000 };
            S("Hitpoints", 1000); S("Toughness", 12);
            S("HeatResistance", 0); S("ColdResistance", 0); S("ElectricResistance", 0);
            e.AddPart(new RenderPart { DisplayName = "cr" });
            e.AddPart(new StatusEffectsPart());
            if (x >= 0) z.AddEntity(e, x, y);
            return e;
        }

        // ════════════════════════════════════════════════════════════
        // H1 — A gas-applied stat shift (CoatedInPlasma -100 resistance)
        // must survive save/load WITHOUT being lost or double-applied.
        // (Save mid-coat → reload → resistance still -100, removal → 0.)
        // ════════════════════════════════════════════════════════════
        [Test]
        public void H1_CoatedInPlasma_StatShift_SurvivesRoundTrip_NoDoubleApply()
        {
            var z = new Zone("AxH1");
            var c = Creature(z, 5, 5);
            c.ApplyEffect(new CoatedInPlasmaEffect(duration: 20));
            Assert.AreEqual(-100, c.GetStatValue("HeatResistance"), "precondition: coated");

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(c);
            var fx = loaded.GetPart<StatusEffectsPart>()?.GetEffect<CoatedInPlasmaEffect>();
            Assert.IsNotNull(fx, "coat survives save/load");
            Assert.AreEqual(-100, loaded.GetStatValue("HeatResistance"),
                "resistance shift preserved — not lost (0) and not double-applied (-200)");

            loaded.GetPart<StatusEffectsPart>().RemoveEffect<CoatedInPlasmaEffect>();
            Assert.AreEqual(0, loaded.GetStatValue("HeatResistance"),
                "removal restores to the round-tripped prior (0)");
        }

        // ════════════════════════════════════════════════════════════
        // H2 — FungalInfection's stage clock + Toughness shift survive
        // save/load (the infection doesn't reset to Incubation on reload).
        // ════════════════════════════════════════════════════════════
        [Test]
        public void H2_FungalInfection_StageClock_SurvivesRoundTrip()
        {
            var z = new Zone("AxH2");
            var c = Creature(z, 5, 5);
            var inf = new FungalInfectionEffect { TurnsInfected = 22 }; // Blooming
            c.ApplyEffect(inf);

            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(c);
            var fx = loaded.GetPart<StatusEffectsPart>()?.GetEffect<FungalInfectionEffect>();
            Assert.IsNotNull(fx, "infection survives save/load");
            Assert.AreEqual(22, fx.TurnsInfected,
                "stage clock preserved — reload doesn't reset the infection to turn 0");
        }

        // ════════════════════════════════════════════════════════════
        // H3 — G.11 nav-immunity and ApplyGas-immunity must AGREE: a
        // creature immune to a gas neither avoids it (nav weight 0) nor is
        // dosed by it (ApplyGas vetoes). A non-immune creature both avoids
        // AND is dosed. (Two different immunity code paths — nav iterates
        // Parts; ApplyGas fires CheckGasCanAffect.)
        // ════════════════════════════════════════════════════════════
        [Test]
        public void H3_NavImmunity_And_ApplyImmunity_Agree()
        {
            var z = new Zone("AxH3");
            var gas = GasFactory.SpawnGas(z, 5, 5, "ax-poison", density: 100);
            var behavior = gas.GetPart<IObjectGasBehaviorPart>();
            var cell = z.GetCell(5, 5);

            var immune = Creature(z, 5, 5);
            immune.AddPart(new GasImmunityPart { GasType = "Poison" });
            Assert.AreEqual(0, GasNavigationWeight.ForCell(cell, immune), "immune → nav doesn't avoid");
            Assert.IsFalse(behavior.ApplyGas(immune, z), "immune → ApplyGas vetoes");

            var bare = Creature(z, 6, 5);
            Assert.Greater(GasNavigationWeight.ForCell(cell, bare), 0, "non-immune → nav avoids");
            Assert.IsTrue(behavior.ApplyGas(bare, z), "non-immune → ApplyGas doses");
        }

        // ════════════════════════════════════════════════════════════
        // H4 — Dispersal boundary at LOW_DENSITY_THRESHOLD (10): density
        // exactly 10 must NOT spread (the gate is `> threshold`). A buggy
        // `>=` would let a density-10 cloud keep spreading forever.
        // ════════════════════════════════════════════════════════════
        [Test]
        public void H4_DensityExactlyAtThreshold_DoesNotSpread()
        {
            // Stable so it can't decay below 10; force no dissipation by
            // making the dissipate roll fail (seed) — we only care that
            // NO spread record is emitted at density == threshold.
            var z = new Zone("AxH4");
            GasSystem.SetRngForTests(new System.Random(1));
            var gas = GasFactory.SpawnGas(z, 40, 12, "ax-poison",
                density: GasSystem.LOW_DENSITY_THRESHOLD); // exactly 10
            gas.GetPart<GasPoolPart>().Stable = true; // no decay
            Diag.ResetAll();
            GasSystem.ProcessGasBehavior(gas, z);
            var spreads = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "Spread", Limit = 5 }).Records;
            Assert.AreEqual(0, spreads.Count, "density == threshold must not spread (gate is strictly >)");
        }

        // ════════════════════════════════════════════════════════════
        // H5 — A gas whose Creator was removed from the zone must still
        // disperse without crashing (the CreatorModifyGasDispersal event
        // fires on a now-orphaned entity).
        // ════════════════════════════════════════════════════════════
        [Test]
        public void H5_DanglingCreator_DispersesWithoutCrash()
        {
            var z = new Zone("AxH5");
            var creator = Creature(z, 5, 5);
            var gas = GasFactory.SpawnGas(z, 6, 5, "ax-poison", density: 100, creator: creator);
            z.RemoveEntity(creator); // creator gone, gas still references it
            Assert.DoesNotThrow(() => GasSystem.ProcessGasBehavior(gas, z),
                "orphaned-creator gas disperses without NRE");
        }

        // ════════════════════════════════════════════════════════════
        // H6 — Wind blowing toward the zone edge must not crash or push gas
        // out of bounds (spread to out-of-bounds cells is skipped).
        // ════════════════════════════════════════════════════════════
        [Test]
        public void H6_WindTowardEdge_NoCrash_StaysInBounds()
        {
            var z = new Zone("AxH6");
            z.CurrentWindSpeed = 100; z.CurrentWindDirection = "W"; // blow toward x=0
            GasSystem.SetRngForTests(new System.Random(2));
            var gas = GasFactory.SpawnGas(z, 0, 12, "ax-poison", density: 200); // at the west edge
            Assert.DoesNotThrow(() => GasSystem.ProcessGasBehavior(gas, z));
            // Any gas that exists is in-bounds (spawn skips OOB cells).
            foreach (var e in z.GetAllEntities())
                if (e.Tags.ContainsKey("Gas"))
                {
                    var p = z.GetEntityPosition(e);
                    Assert.IsTrue(z.InBounds(p.x, p.y), $"gas at ({p.x},{p.y}) is in bounds");
                }
        }

        // ════════════════════════════════════════════════════════════
        // H7 — Two DIFFERENT-type gases in the same cell dose a creature
        // INDEPENDENTLY (poison + stun → both effects). They must not merge
        // (different GasType) and both ApplyGas paths must fire.
        // ════════════════════════════════════════════════════════════
        [Test]
        public void H7_OverlappingDifferentGases_DoseIndependently()
        {
            var z = new Zone("AxH7");
            var poison = GasFactory.SpawnGas(z, 5, 5, "ax-poison", density: 100);
            var stun = GasFactory.SpawnGas(z, 5, 5, "ax-stun", density: 100);
            var victim = Creature(z, 5, 5);

            poison.GetPart<IObjectGasBehaviorPart>().ApplyGas(victim, z);
            stun.GetPart<IObjectGasBehaviorPart>().ApplyGas(victim, z);

            Assert.IsNotNull(victim.GetEffect<PoisonedByGasEffect>(), "poison applied");
            Assert.IsNotNull(victim.GetEffect<StunnedEffect>(), "stun applied — overlapping gases stack effects");
        }

        // ════════════════════════════════════════════════════════════
        // H8 — DESIGN PIN (documented asymmetry): a GasMask reduces gas
        // ENTRY damage (tagged "Gas") but NOT the lingering per-turn
        // PoisonedByGasEffect DoT (tagged "Poison" only). This models a
        // mask as an INHALATION filter — it cuts the inhaled dose, not the
        // poison already in the bloodstream (consistent with Qud's
        // intake-reduction model). Pinning the contract so a future change
        // to the DoT's attributes is a deliberate decision, not a silent
        // drift. (If design later wants the mask to also cut the DoT, add
        // the "Gas" tag in PoisonedByGasEffect.OnTurnStart:69 + flip this.)
        // ════════════════════════════════════════════════════════════
        [Test]
        public void H8_GasMask_DoesNotReduce_LingeringPoisonDoT_InhalationModel()
        {
            var z = new Zone("AxH8");
            SettlementRuntime.ActiveZone = z;
            var masked = Creature(z, 5, 5);
            masked.AddPart(new GasMaskPart { Power = 50 }); // strong mask
            var dot = new PoisonedByGasEffect { Duration = 5, DamagePerTurn = 10, GasTypeKey = "Poison" };
            masked.ApplyEffect(dot);

            var hp = masked.GetStat("Hitpoints");
            int hp0 = hp.BaseValue;
            dot.OnTurnStart(masked, GameEvent.New("BeginTakeAction"));
            int dealt = hp0 - hp.BaseValue;

            Assert.AreEqual(10, dealt,
                "lingering poison DoT is NOT mask-reduced (mask filters inhalation, " +
                "not absorbed poison) — DoT carries \"Poison\" only, not \"Gas\"");
        }

        // ════════════════════════════════════════════════════════════
        // H9 — CONFIRMED BUG (found by the read-based audit): the G.11 nav
        // immunity check and the ApplyGas immunity veto must use the SAME
        // case-sensitivity. GasImmunityPart is deliberately case-SENSITIVE
        // (GasImmunityPart.cs:30-42); G.11's GasNavigationWeight.IsImmune
        // used OrdinalIgnoreCase → under a case mismatch the creature was
        // nav-immune (won't avoid) but NOT apply-immune (still dosed) —
        // it walks into gas it isn't protected from. They must AGREE.
        // ════════════════════════════════════════════════════════════
        [Test]
        public void H9_NavAndApplyImmunity_AgreeUnderCaseMismatch()
        {
            var z = new Zone("AxH9");
            var gas = GasFactory.SpawnGas(z, 5, 5, "ax-poison", density: 100); // GasType "Poison"
            var behavior = gas.GetPart<IObjectGasBehaviorPart>();
            var cell = z.GetCell(5, 5);

            var actor = Creature(z, 5, 5);
            actor.AddPart(new GasImmunityPart { GasType = "poison" }); // lowercase — case mismatch

            bool navAvoids = GasNavigationWeight.ForCell(cell, actor) > 0;
            bool applyDoses = behavior.ApplyGas(actor, z);

            // Both paths must reach the SAME verdict on whether this
            // case-mismatched immunity counts. With both case-sensitive,
            // "poison" != "Poison" → NOT immune → nav avoids AND apply doses.
            Assert.AreEqual(navAvoids, applyDoses,
                "nav-avoidance and apply-dosing must agree on the same immunity verdict " +
                "(divergent case-sensitivity = walk into unprotected gas)");
        }

        // ════════════════════════════════════════════════════════════
        // H10 — coverage gap (agent-identified): Sleep gas doses a
        // stationary creature via the per-turn GasSystem.OnTickEnd dispatch
        // (cryo/plasma/poison have this test; sleep didn't). Pins the
        // full tick → ApplyToCell → ApplyGas chain for sleep.
        // ════════════════════════════════════════════════════════════
        [Test]
        public void H10_SleepGas_PerTurnDispatch_DosesStationarySleeper()
        {
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""ax-sleep"", ""GasType"":""Sleep"", ""Glyph"":""°"", ""Color"":""&B"",
                ""DefaultDensity"":50, ""DefaultLevel"":1, ""BehaviorKind"":""Sleep"" } ] }");
            var z = new Zone("AxH10");
            GasFactory.SpawnGas(z, 5, 5, "ax-sleep", density: 500);
            var sleeper = Creature(z, 5, 5);
            GasSystem.OnTickEnd(z);
            Assert.IsNotNull(sleeper.GetEffect<AsleepByGasEffect>(),
                "per-turn tick dispatch doses a creature standing in sleep gas");
        }

        // ════════════════════════════════════════════════════════════
        // H11 — CONFIRMED BUG (found while diagnosing "gas invisible"):
        // GasPoolPart.Stable is documented as "persist indefinitely", but
        // the low-density dissipation flickered stable gas out anyway (it
        // spreads thin, then the edges dissipate). A stable, non-decaying,
        // non-spreading (≤threshold) cloud must persist. Fixed by gating
        // the low-density dissipation on !Stable.
        // ════════════════════════════════════════════════════════════
        [Test]
        public void H11_StableGas_PersistsAtLowDensity()
        {
            var z = new Zone("AxH11");
            GasSystem.SetRngForTests(new System.Random(1));
            var gas = GasFactory.SpawnGas(z, 40, 12, "ax-poison",
                density: GasSystem.LOW_DENSITY_THRESHOLD - 2); // 8, ≤ threshold, won't spread
            gas.GetPart<GasPoolPart>().Stable = true;
            for (int t = 0; t < 50; t++) GasSystem.OnTickEnd(z);
            int cnt = 0;
            foreach (var e in z.GetAllEntities()) if (e.Tags.ContainsKey("Gas")) cnt++;
            Assert.AreEqual(1, cnt, "stable low-density gas persists (no flicker-out)");
            Assert.AreEqual(GasSystem.LOW_DENSITY_THRESHOLD - 2,
                gas.GetPart<GasPoolPart>().Density, "stable gas neither decays nor dissipates");
        }

        [Test]
        public void H11b_NonStableLowDensityGas_DissipatesQuickly_Counter()
        {
            var z = new Zone("AxH11b");
            GasSystem.SetRngForTests(new System.Random(1));
            GasFactory.SpawnGas(z, 40, 12, "ax-poison",
                density: GasSystem.LOW_DENSITY_THRESHOLD - 2); // NOT stable
            for (int t = 0; t < 50; t++) GasSystem.OnTickEnd(z);
            int cnt = 0;
            foreach (var e in z.GetAllEntities()) if (e.Tags.ContainsKey("Gas")) cnt++;
            Assert.AreEqual(0, cnt, "non-stable low-density gas DOES dissipate (counter to H11)");
        }
    }
}
