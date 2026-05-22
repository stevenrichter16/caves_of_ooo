using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// G.12 — dedicated adversarial sweep for the whole gas system
    /// (CLAUDE.md "Adversarial test sweep" gate). Probes bug classes the
    /// per-feature happy-path + counter-check tests can't see: state
    /// atomicity (merge conservation, density clamp), parser malformed
    /// inputs (registry/factory), cross-actor flows, boundary/null safety,
    /// mid-tick death/dissipation, stacking semantics, save/load reach,
    /// and probability boundaries.
    ///
    /// <para>Surfaces probed are documented per-section. 0 bugs found does
    /// NOT prove the system bug-free (bounded by imagined bug classes) —
    /// the value is the rare bug it finds + the regression infrastructure.</para>
    /// </summary>
    public class GasSystemAdversarialTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""adv-gas"", ""GasType"":""Adv"", ""Glyph"":""°"",
                ""Color"":""&w"", ""DefaultDensity"":50, ""DefaultLevel"":1,
                ""BehaviorKind"":""Poison"" },
              { ""Id"":""adv-gas-2"", ""GasType"":""Adv2"", ""Glyph"":""°"",
                ""Color"":""&r"", ""DefaultDensity"":50, ""DefaultLevel"":1,
                ""BehaviorKind"":""Poison"" } ] }");
        }

        [TearDown]
        public void TearDown()
        {
            GasRegistry.ResetForTests();
            GasSystem.SetRngForTests(null);
            SettlementRuntime.ActiveZone = null;
        }

        private static GasPoolPart Pool(Zone z, int x, int y, string id = "adv-gas", int density = 50)
            => GasFactory.SpawnGas(z, x, y, id, density: density)?.GetPart<GasPoolPart>();

        // ════════════════════════════════════════════════════════════
        //   § State atomicity — MergeChunk conservation + Density clamp
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_MergeChunk_ConservesDensity()
        {
            var z = new Zone("AdvMergeCons");
            var src = Pool(z, 5, 5, density: 50);
            var dst = Pool(z, 6, 5, density: 30);
            GasSystem.MergeChunk(src, dst, 20);
            Assert.AreEqual(30, src.Density, "donor lost exactly 20");
            Assert.AreEqual(50, dst.Density, "receiver gained exactly 20");
            Assert.AreEqual(80, src.Density + dst.Density, "total conserved (50+30)");
        }

        [Test]
        public void Adversarial_MergeChunk_ChunkExceedsDonor_CappedAtDonor()
        {
            var z = new Zone("AdvMergeCap");
            var src = Pool(z, 5, 5, density: 10);
            var dst = Pool(z, 6, 5, density: 10);
            GasSystem.MergeChunk(src, dst, 999);
            Assert.AreEqual(0, src.Density, "donor fully drained, not negative");
            Assert.AreEqual(20, dst.Density, "receiver took only the donor's 10");
        }

        [Test]
        public void Adversarial_MergeChunk_ZeroOrNegativeChunk_NoOp()
        {
            var z = new Zone("AdvMergeZero");
            var src = Pool(z, 5, 5, density: 40);
            var dst = Pool(z, 6, 5, density: 40);
            GasSystem.MergeChunk(src, dst, 0);
            GasSystem.MergeChunk(src, dst, -5);
            Assert.AreEqual(40, src.Density);
            Assert.AreEqual(40, dst.Density);
        }

        [Test]
        public void Adversarial_Density_NegativeAssignment_ClampsAtZero()
        {
            var z = new Zone("AdvDensClamp");
            var p = Pool(z, 5, 5, density: 10);
            p.Density = -100;
            Assert.AreEqual(0, p.Density, "negative density clamps to 0");
        }

        [Test]
        public void Adversarial_MergeChunk_ReceiverTakesMaxLevel()
        {
            var z = new Zone("AdvMergeLevel");
            var src = Pool(z, 5, 5); src.Level = 5;
            var dst = Pool(z, 6, 5); dst.Level = 2;
            GasSystem.MergeChunk(src, dst, 10);
            Assert.AreEqual(5, dst.Level, "receiver takes the higher level");
        }

        // ════════════════════════════════════════════════════════════
        //   § Parser malformed inputs — registry + factory
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_Factory_UnknownGasId_ReturnsNull_Rejected()
        {
            var z = new Zone("AdvUnknown");
            Diag.ResetAll();
            var gas = GasFactory.SpawnGas(z, 5, 5, "no-such-gas");
            Assert.IsNull(gas);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "SpawnRejected", Limit = 5 }).Records;
            Assert.GreaterOrEqual(recs.Count, 1, "unknown gas emits SpawnRejected");
        }

        [Test]
        public void Adversarial_Factory_NullAndEmptyGasId_ReturnNull()
        {
            var z = new Zone("AdvNullId");
            Assert.IsNull(GasFactory.SpawnGas(z, 5, 5, null));
            Assert.IsNull(GasFactory.SpawnGas(z, 5, 5, ""));
        }

        [Test]
        public void Adversarial_Factory_NullZone_ReturnsNull_NoCrash()
        {
            Assert.IsNull(GasFactory.SpawnGas(null, 5, 5, "adv-gas"));
        }

        [Test]
        public void Adversarial_Factory_UninitializedRegistry_ReturnsNull()
        {
            GasRegistry.ResetForTests();
            var z = new Zone("AdvUninit");
            Assert.IsNull(GasFactory.SpawnGas(z, 5, 5, "adv-gas"));
        }

        [Test]
        public void Adversarial_Registry_MalformedJson_NoCrash_StaysEmpty()
        {
            GasRegistry.ResetForTests();
            Assert.DoesNotThrow(() => GasRegistry.Initialize("{ not valid json ]["));
            Assert.AreEqual(0, GasRegistry.Count, "malformed JSON yields no defs");
        }

        [Test]
        public void Adversarial_Registry_EmptyGasesArray_ZeroCount()
        {
            GasRegistry.ResetForTests();
            GasRegistry.Initialize(@"{ ""Gases"":[] }");
            Assert.AreEqual(0, GasRegistry.Count);
        }

        // ════════════════════════════════════════════════════════════
        //   § Boundary / null safety across the public API
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_ProcessGasBehavior_NullArgs_NoCrash()
        {
            var z = new Zone("AdvNullProc");
            Assert.DoesNotThrow(() => GasSystem.ProcessGasBehavior(null, z));
            Assert.DoesNotThrow(() => GasSystem.ProcessGasBehavior(null, null));
        }

        [Test]
        public void Adversarial_OnTickEnd_NullAndEmptyZone_NoCrash()
        {
            Assert.DoesNotThrow(() => GasSystem.OnTickEnd(null));
            Assert.DoesNotThrow(() => GasSystem.OnTickEnd(new Zone("AdvEmptyTick")));
        }

        [Test]
        public void Adversarial_Dissipate_NullGas_NoCrash()
        {
            var z = new Zone("AdvDissipateNull");
            Assert.DoesNotThrow(() => GasSystem.Dissipate(null, null, z, "test"));
        }

        // ════════════════════════════════════════════════════════════
        //   § Mid-tick death / dissipation / iteration safety
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_ThinGas_DissipatesMidTick_NoDoubleProcess()
        {
            // A density-1 unstable gas decays to 0 and dissipates within
            // its own ProcessGasBehavior; the OnTickEnd DispatchPerTurnApply
            // must skip it (pos.x < 0 guard) — no crash, gas gone.
            GasSystem.SetRngForTests(new System.Random(1));
            var z = new Zone("AdvMidTickDie");
            var gas = GasFactory.SpawnGas(z, 5, 5, "adv-gas", density: 1);
            Assert.DoesNotThrow(() => GasSystem.OnTickEnd(z));
            int remaining = 0;
            foreach (var e in z.GetAllEntities()) if (e.Tags.ContainsKey("Gas")) remaining++;
            Assert.AreEqual(0, remaining, "density-1 gas dissipated cleanly");
        }

        [Test]
        public void Adversarial_OnTickEnd_SnapshotIterate_ChainedSpreadNoCorruption()
        {
            // High-density gas spawns NEW gas mid-tick; the snapshot-iterate
            // contract means the freshly-spawned gas isn't processed this
            // same tick (no infinite cascade / iteration corruption).
            GasSystem.SetRngForTests(new System.Random(5));
            var z = new Zone("AdvChain");
            GasFactory.SpawnGas(z, 40, 12, "adv-gas", density: 100);
            Assert.DoesNotThrow(() => GasSystem.OnTickEnd(z));
        }

        // ════════════════════════════════════════════════════════════
        //   § Cross-actor flows
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_CreatorInOwnGas_IsAffected_QudParity()
        {
            // Qud's GasPoison only excludes `Object == ParentObject` (the
            // cloud), NOT the Creator (GasPoison.cs:96). CoO's RunFilterChain
            // matches — the creator IS affected by their own gas. Pins parity.
            var z = new Zone("AdvCreatorSelf");
            var creator = MakeCreature(z, 5, 5);
            var gas = GasFactory.SpawnGas(z, 5, 5, "adv-gas", density: 100, creator: creator);
            bool applied = gas.GetPart<GasPoisonPart>().ApplyGas(creator, z);
            Assert.IsTrue(applied, "creator is NOT immune to their own gas (Qud parity)");
        }

        [Test]
        public void Adversarial_NullCreator_GasSpawnsAndApplies()
        {
            var z = new Zone("AdvNullCreator");
            var gas = GasFactory.SpawnGas(z, 5, 5, "adv-gas", density: 100, creator: null);
            Assert.IsNotNull(gas);
            var victim = MakeCreature(z, 5, 5);
            Assert.IsTrue(gas.GetPart<GasPoisonPart>().ApplyGas(victim, z),
                "environmental (null-creator) gas still applies");
        }

        // ════════════════════════════════════════════════════════════
        //   § Stacking semantics — re-apply each gas effect's OnStack
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_PoisonedByGas_OnStack_TakesMaxDuration()
        {
            var a = new PoisonedByGasEffect { Duration = 3 };
            Assert.IsTrue(a.OnStack(new PoisonedByGasEffect { Duration = 8 }));
            Assert.AreEqual(8, a.Duration, "longer refresh wins");
            a.OnStack(new PoisonedByGasEffect { Duration = 2 });
            Assert.AreEqual(8, a.Duration, "shorter doesn't shrink");
        }

        [Test]
        public void Adversarial_AsleepByGas_OnStack_TakesMaxDuration()
        {
            var a = new AsleepByGasEffect(3);
            a.OnStack(new AsleepByGasEffect(8));
            Assert.AreEqual(8, a.Duration);
        }

        [Test]
        public void Adversarial_FungalInfection_OnStack_PreservesStageClock()
        {
            // CRITICAL: walking through fresh spores must NOT reset an
            // advanced infection back to Incubation.
            var a = new FungalInfectionEffect { TurnsInfected = 15 }; // Symptomatic
            bool stacked = a.OnStack(new FungalInfectionEffect());
            Assert.IsTrue(stacked, "stack consumed");
            Assert.AreEqual(15, a.TurnsInfected, "stage clock preserved, not reset to 0");
        }

        [Test]
        public void Adversarial_CoatedInPlasma_OnStack_TakesLargerDuration()
        {
            var a = new CoatedInPlasmaEffect(10);
            a.OnStack(new CoatedInPlasmaEffect(25));
            Assert.AreEqual(25, a.Duration);
        }

        // ════════════════════════════════════════════════════════════
        //   § Save/load reach — public state round-trips via reflection
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_GasPoolPart_Density_RoundTrips()
        {
            // Density is a PROPERTY backed by a private field. If the save
            // reflection (public-fields-only) misses it, a saved cloud
            // reloads at Density 0 and instantly dissipates.
            var src = new Entity { ID = "gp", BlueprintName = "AdvGasEntity" };
            var pool = new GasPoolPart { GasId = "adv-gas", Level = 3 };
            src.AddPart(pool);
            pool.Density = 77;
            var loaded = PartRoundTripHelper.RoundTripEntity(src);
            var lp = loaded.GetPart<GasPoolPart>();
            Assert.IsNotNull(lp, "GasPoolPart survives round-trip");
            Assert.AreEqual(77, lp.Density, "Density must survive save/load");
        }

        [Test]
        public void Adversarial_GasPoolPart_PublicFields_RoundTrip()
        {
            var src = new Entity { ID = "gp2", BlueprintName = "AdvGasEntity" };
            src.AddPart(new GasPoolPart
            {
                GasId = "adv-gas", Level = 4, Seeping = true, Stable = true,
                GasType = "Adv", ColorString = "&r"
            });
            var lp = PartRoundTripHelper.RoundTripEntity(src).GetPart<GasPoolPart>();
            Assert.AreEqual("adv-gas", lp.GasId);
            Assert.AreEqual(4, lp.Level);
            Assert.IsTrue(lp.Seeping);
            Assert.IsTrue(lp.Stable);
            Assert.AreEqual("Adv", lp.GasType);
            Assert.AreEqual("&r", lp.ColorString);
        }

        [Test]
        public void Adversarial_BurnOffGasPart_DamageTaken_RoundTrips()
        {
            var src = new Entity { ID = "bo", BlueprintName = "AdvBurnEntity" };
            src.AddPart(new BurnOffGasPart { GasId = "adv-gas", DamageTaken = 7, DamagePer = 20 });
            var lp = PartRoundTripHelper.RoundTripEntity(src).GetPart<BurnOffGasPart>();
            Assert.AreEqual(7, lp.DamageTaken, "accumulator survives save/load");
            Assert.AreEqual(20, lp.DamagePer);
        }

        // (Gas-effect public-field round-trip — StatsApplied/PriorX,
        // DamagePerTurn, TurnsInfected — goes through StatusEffectsPart's
        // ISaveSerializable path, which is covered by the SL.6 effect
        // round-trip suite; not re-tested here to avoid coupling to that
        // subsystem's custom save hook.)

        // ════════════════════════════════════════════════════════════
        //   § Probability boundaries
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_PickSpreadDirection_NegativeWind_AlwaysRandom()
        {
            // windSpeed<0 ⇒ the `windSpeed > 0` guard short-circuits → random.
            var rng = new System.Random(9);
            for (int i = 0; i < 100; i++)
            {
                int d = GasSystem.PickSpreadDirection(-50, 2, rng);
                Assert.GreaterOrEqual(d, 0); Assert.LessOrEqual(d, 7);
            }
        }

        [Test]
        public void Adversarial_ComputeSpreadAttempts_AlwaysAtLeastOne()
        {
            var rng = new System.Random(9);
            for (int ws = 0; ws <= 200; ws += 25)
                for (int i = 0; i < 20; i++)
                    Assert.GreaterOrEqual(GasSystem.ComputeSpreadAttempts(ws, rng), 1,
                        $"attempts >= 1 even at windSpeed {ws}");
        }

        // ════════════════════════════════════════════════════════════
        //   § Multi-instance independence
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_TwoCloudsSameType_DifferentCells_Independent()
        {
            var z = new Zone("AdvTwoClouds");
            var a = Pool(z, 5, 5, density: 40);
            var b = Pool(z, 60, 20, density: 60);
            a.Density = 10;
            Assert.AreEqual(10, a.Density);
            Assert.AreEqual(60, b.Density, "distant same-type cloud is unaffected");
        }

        // ──────────── fixture helper ────────────
        private static Entity MakeCreature(Zone zone, int x, int y)
        {
            var e = new Entity { ID = "cr_" + x + "_" + y, BlueprintName = "AdvCreature" };
            e.Tags["Creature"] = "";
            void S(string n, int v) => e.Statistics[n] =
                new Stat { Owner = e, Name = n, BaseValue = v, Min = -200, Max = 100000 };
            S("Hitpoints", 1000); S("Toughness", 12);
            e.AddPart(new RenderPart { DisplayName = "cr" });
            e.AddPart(new StatusEffectsPart());
            zone.AddEntity(e, x, y);
            return e;
        }
    }
}
