using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// G.8d.3 — contagion mechanic. The infected host releases spore
    /// gas at their cell during Blooming + Terminal stages, enabling
    /// the natural infection-spreads-via-gas loop (host spawns gas →
    /// gas spreads → adjacent creatures inhale + roll for infection
    /// → those creatures eventually become hosts → spread continues).
    ///
    /// <para>Cadence:
    /// <list type="bullet">
    ///   <item>Stage Blooming (turns 20-29): every 3 turns → 20, 23, 26, 29</item>
    ///   <item>Stage Terminal (turns 30-39): every 2 turns → 30, 32, 34, 36, 38</item>
    /// </list>
    /// Incubation + Symptomatic + Expired: no contagion spawns.</para>
    /// </summary>
    public class FungalInfectionContagionTests
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
            SettlementRuntime.Reset();
        }

        // Wx §1a merge-on-spawn: the cadence tests detect spawn events
        // by density growth (entity count no longer moves per spawn —
        // repeat spawns merge into the standing cloud).
        private static int TotalGasDensity(Zone zone)
        {
            int total = 0;
            foreach (var e in zone.GetEntitiesWithTag("Gas"))
                total += e.GetPart<GasPoolPart>()?.Density ?? 0;
            return total;
        }

        private static Entity MakeCreatureInZone(Zone zone, int x, int y,
            int hpMax = 200, int toughness = 14)
        {
            var e = new Entity { ID = "c_" + x + "_" + y, BlueprintName = "TestCreature" };
            e.Tags["Creature"] = "";
            void S(string n, int v, int max = 400) => e.Statistics[n] =
                new Stat { Owner = e, Name = n, BaseValue = v, Min = -200, Max = max };
            S("Hitpoints", hpMax, hpMax);
            S("Toughness", toughness);
            S("Agility", 14); S("DV", 6); S("AV", 0);
            e.AddPart(new RenderPart { DisplayName = "infected" });
            e.AddPart(new StatusEffectsPart());
            zone.AddEntity(e, x, y);
            return e;
        }

        private static GameEvent ContextWithZone(Zone zone)
        {
            var ctx = GameEvent.New("BeginTakeAction");
            ctx.SetParameter("Zone", (object)zone);
            return ctx;
        }

        // ════════════════════════════════════════════════════════════
        //   PART I — Stage-gated contagion (no spawn pre-Blooming)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Contagion_StageIncubation_NoSpawn()
        {
            var zone = new Zone("ContagionIncubation");
            var host = MakeCreatureInZone(zone, 5, 5);
            var fx = new FungalInfectionEffect();
            host.ApplyEffect(fx);
            // TurnsInfected = 0 → first OnTurnStart bumps to 1 → still Incubation.
            fx.OnTurnStart(host, ContextWithZone(zone));
            // Only the host should be in the zone — no gas entities.
            Assert.AreEqual(0, zone.GetEntitiesWithTag("Gas").Count,
                "no contagion at Incubation");
        }

        [Test]
        public void Contagion_StageSymptomatic_NoSpawn()
        {
            var zone = new Zone("ContagionSymptomatic");
            var host = MakeCreatureInZone(zone, 5, 5);
            var fx = new FungalInfectionEffect();
            host.ApplyEffect(fx);
            fx.TurnsInfected = 14; // already in Symptomatic
            fx.OnTurnStart(host, ContextWithZone(zone));
            Assert.AreEqual(0, zone.GetEntitiesWithTag("Gas").Count,
                "no contagion at Symptomatic");
        }

        // ════════════════════════════════════════════════════════════
        //   PART II — Blooming contagion (cadence 3)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Contagion_StageBlooming_FirstTurn_Spawns()
        {
            // TurnsInfected = 19, OnTurnStart bumps to 20 → enters Blooming
            // → (20-20) % 3 == 0 → spawn.
            var zone = new Zone("ContagionBlooming0");
            var host = MakeCreatureInZone(zone, 5, 5);
            var fx = new FungalInfectionEffect();
            host.ApplyEffect(fx);
            fx.TurnsInfected = 19;

            fx.OnTurnStart(host, ContextWithZone(zone));

            Assert.AreEqual(1, zone.GetEntitiesWithTag("Gas").Count,
                "Blooming turn 0 spawns one contagion gas");
        }

        [Test]
        public void Contagion_Blooming_CadenceThree_SpawnsOnTurns_20_23_26_29()
        {
            // Walk through Blooming stage turn-by-turn and count spawns.
            // Expected: spawn at turns 20, 23, 26, 29 = 4 cadence hits.
            var zone = new Zone("ContagionBloomingCadence");
            var host = MakeCreatureInZone(zone, 5, 5);
            var fx = new FungalInfectionEffect();
            host.ApplyEffect(fx);
            fx.TurnsInfected = 19;

            // Wx §1a merge-on-spawn: repeat spawns at the stationary
            // host's cell merge into one growing cloud, so entity count
            // only moves on the FIRST hit. Total density strictly
            // increases on every cadence hit — that is the spawn signal.
            int spawnCount = 0;
            for (int i = 0; i < 10; i++) // turns 20..29
            {
                int before = TotalGasDensity(zone);
                fx.OnTurnStart(host, ContextWithZone(zone));
                int after = TotalGasDensity(zone);
                if (after > before) spawnCount++;
            }
            // Expected: at turns 20, 23, 26, 29 → 4 spawn events
            Assert.AreEqual(4, spawnCount,
                "4 contagion spawns at Blooming cadence-3 (turns 20, 23, 26, 29)");
        }

        // ════════════════════════════════════════════════════════════
        //   PART III — Terminal contagion (cadence 2 — faster)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Contagion_Terminal_CadenceTwo_SpawnsOnEvenTurns()
        {
            // Walk through Terminal turn-by-turn. Expected: spawn at
            // turns 30, 32, 34, 36, 38 = 5 cadence hits.
            var zone = new Zone("ContagionTerminalCadence");
            var host = MakeCreatureInZone(zone, 5, 5);
            var fx = new FungalInfectionEffect();
            host.ApplyEffect(fx);
            fx.TurnsInfected = 29;

            // Wx §1a merge-on-spawn: density, not entity count, is the
            // spawn signal (see the Blooming cadence test above).
            int spawnCount = 0;
            for (int i = 0; i < 10; i++) // turns 30..39
            {
                int before = TotalGasDensity(zone);
                fx.OnTurnStart(host, ContextWithZone(zone));
                int after = TotalGasDensity(zone);
                if (after > before) spawnCount++;
            }
            Assert.AreEqual(5, spawnCount,
                "5 contagion spawns at Terminal cadence-2 (turns 30, 32, 34, 36, 38)");
        }

        // ════════════════════════════════════════════════════════════
        //   PART IV — Spawned gas properties
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Contagion_SpawnedGas_HasHostAsCreator()
        {
            // Provenance: the contagion gas credits the infected host
            // as its Creator. If another creature gets infected from
            // it, the diag chain traces back to the host.
            var zone = new Zone("ContagionCreator");
            var host = MakeCreatureInZone(zone, 5, 5);
            var fx = new FungalInfectionEffect();
            host.ApplyEffect(fx);
            fx.TurnsInfected = 19;
            fx.OnTurnStart(host, ContextWithZone(zone));

            var gas = zone.GetEntitiesWithTag("Gas")[0];
            Assert.AreSame(host, gas.GetPart<GasPoolPart>().Creator,
                "contagion gas credits the host as Creator");
        }

        [Test]
        public void Contagion_SpawnedAtHostsCell()
        {
            var zone = new Zone("ContagionPosition");
            var host = MakeCreatureInZone(zone, 7, 9);
            var fx = new FungalInfectionEffect();
            host.ApplyEffect(fx);
            fx.TurnsInfected = 19;
            fx.OnTurnStart(host, ContextWithZone(zone));

            var gas = zone.GetEntitiesWithTag("Gas")[0];
            var pos = zone.GetEntityPosition(gas);
            Assert.AreEqual(7, pos.x);
            Assert.AreEqual(9, pos.y);
        }

        [Test]
        public void Contagion_TerminalDensityHigherThanBlooming()
        {
            // Terminal cadence (every 2 turns) + bigger density (50 vs
            // 30) means a Terminal host is a much worse vector than
            // a Blooming one. Pin the density bump.
            var zone1 = new Zone("Bloom");
            var hostB = MakeCreatureInZone(zone1, 5, 5);
            var fxB = new FungalInfectionEffect();
            hostB.ApplyEffect(fxB);
            fxB.TurnsInfected = 19;
            fxB.OnTurnStart(hostB, ContextWithZone(zone1));
            int bloomDensity = zone1.GetEntitiesWithTag("Gas")[0].GetPart<GasPoolPart>().Density;

            var zone2 = new Zone("Terminal");
            var hostT = MakeCreatureInZone(zone2, 5, 5);
            var fxT = new FungalInfectionEffect();
            hostT.ApplyEffect(fxT);
            fxT.TurnsInfected = 29;
            fxT.OnTurnStart(hostT, ContextWithZone(zone2));
            int termDensity = zone2.GetEntitiesWithTag("Gas")[0].GetPart<GasPoolPart>().Density;

            Assert.Greater(termDensity, bloomDensity,
                $"Terminal contagion is denser than Blooming (term={termDensity}, bloom={bloomDensity})");
        }

        // ════════════════════════════════════════════════════════════
        //   PART V — Adversarial: zone-resolution + safety
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Contagion_NullZone_NoSpawn_NoCrash()
        {
            // No zone in context, no ActiveZone → contagion skipped.
            // No crash; the host still takes per-stage damage.
            SettlementRuntime.Reset(); // ensure ActiveZone is null
            var creature = new Entity { ID = "orphan", BlueprintName = "T" };
            creature.Tags["Creature"] = "";
            void S(string n, int v, int max = 400) => creature.Statistics[n] =
                new Stat { Owner = creature, Name = n, BaseValue = v, Min = -200, Max = max };
            S("Hitpoints", 200); S("Toughness", 14);
            creature.AddPart(new RenderPart { DisplayName = "orphan" });
            creature.AddPart(new StatusEffectsPart());

            var fx = new FungalInfectionEffect();
            creature.ApplyEffect(fx);
            fx.TurnsInfected = 19;

            Assert.DoesNotThrow(() => fx.OnTurnStart(creature, null));
            // No way to count gas entities (no zone) — just confirm
            // no crash. Damage path was still exercised.
        }

        [Test]
        public void Contagion_FallsBackToSettlementRuntimeActiveZone()
        {
            // When context has no Zone param, the effect should fall
            // back to SettlementRuntime.ActiveZone (mirroring how the
            // damage-tick path does).
            var zone = new Zone("ContagionFallback");
            SettlementRuntime.ActiveZone = zone;
            try
            {
                var host = MakeCreatureInZone(zone, 5, 5);
                var fx = new FungalInfectionEffect();
                host.ApplyEffect(fx);
                fx.TurnsInfected = 19;

                // Pass context with NO Zone param — effect must fall
                // back to ActiveZone for contagion spawn.
                var ctx = GameEvent.New("BeginTakeAction");
                fx.OnTurnStart(host, ctx);
                ctx.Release();

                Assert.AreEqual(1, zone.GetEntitiesWithTag("Gas").Count,
                    "fell back to SettlementRuntime.ActiveZone");
            }
            finally { SettlementRuntime.Reset(); }
        }

        [Test]
        public void Contagion_HostNotInZone_NoSpawn_NoCrash()
        {
            // The host is somehow detached from the zone (e.g. limbo
            // entity state). GetEntityPosition returns (-1,-1). No
            // crash, no spawn.
            var zone = new Zone("ContagionOrphan");
            var orphan = new Entity { ID = "orphan", BlueprintName = "Orphan" };
            orphan.Tags["Creature"] = "";
            void S(string n, int v, int max = 400) => orphan.Statistics[n] =
                new Stat { Owner = orphan, Name = n, BaseValue = v, Min = -200, Max = max };
            S("Hitpoints", 200); S("Toughness", 14);
            orphan.AddPart(new RenderPart { DisplayName = "orphan" });
            orphan.AddPart(new StatusEffectsPart());
            // Intentionally NOT zone.AddEntity(orphan).

            var fx = new FungalInfectionEffect();
            orphan.ApplyEffect(fx);
            fx.TurnsInfected = 19;

            Assert.DoesNotThrow(() => fx.OnTurnStart(orphan, ContextWithZone(zone)));
            Assert.AreEqual(0, zone.GetEntitiesWithTag("Gas").Count,
                "orphan host (not in zone) doesn't spawn contagion");
        }

        // ════════════════════════════════════════════════════════════
        //   PART VI — Diag observability
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Contagion_EmitsDiag()
        {
            var zone = new Zone("ContagionDiag");
            var host = MakeCreatureInZone(zone, 5, 5);
            var fx = new FungalInfectionEffect();
            host.ApplyEffect(fx);
            fx.TurnsInfected = 19;
            Diag.ResetAll();

            fx.OnTurnStart(host, ContextWithZone(zone));

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "Contagion", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"stage\":\"Blooming\"", recs[0].PayloadJson);
            StringAssert.Contains("\"turnsInfected\":20", recs[0].PayloadJson);
            StringAssert.Contains("\"cadence\":3", recs[0].PayloadJson);
            StringAssert.Contains("\"spawnX\":5", recs[0].PayloadJson);
            StringAssert.Contains("\"spawnY\":5", recs[0].PayloadJson);
        }

        // ════════════════════════════════════════════════════════════
        //   PART VII — Self-immunity (host doesn't re-infect themselves)
        // ════════════════════════════════════════════════════════════

        // Regression diagnostic for the old intermittent precondition: an unstable
        // cloud can leave the host before per-turn exposure dispatch, which is valid.
        [TestCase(false)] [TestCase(true)]
        public void Contagion_PerTurnSelfExposureDependsOnCloudRemainingAtHost(bool disperse)
        {
            var zone = new Zone("SelfExposureBoundary"); var host = MakeCreatureInZone(zone, 5, 5);
            var fx = new FungalInfectionEffect(); Assert.IsTrue(host.ApplyEffect(fx)); fx.TurnsInfected = 19;
            var context = ContextWithZone(zone); try { fx.OnTurnStart(host, context); } finally { context.Release(); }
            var cloud = zone.GetEntitiesWithTag("Gas")[0]; Assert.AreEqual((5, 5), zone.GetEntityPosition(cloud)); Assert.AreEqual(30, cloud.GetPart<GasPoolPart>().Density);
            Diag.ResetAll(); TickWithSpreadChoice(zone, disperse);
            int records = DiagQuery.Count(new DiagQuery.Filter { Category = "gas", Kind = "InfectionAlreadyPresent", Target = host.ID }).Count;
            Assert.AreEqual(disperse ? 0 : 1, records);
            Assert.AreEqual(disperse ? (-1, -1) : (5, 5), zone.GetEntityPosition(cloud));
            if (disperse) Assert.Greater(zone.GetEntitiesWithTag("Gas").Count, 0, "The cloud moved away; exposure was not silently lost.");
            else Assert.Greater(cloud.GetPart<GasPoolPart>().Density, 10);
            Assert.AreSame(fx, host.GetEffect<FungalInfectionEffect>()); Assert.AreEqual(20, fx.TurnsInfected);
        }
        private static void TickWithSpreadChoice(Zone zone, bool disperse)
        {
            var field = typeof(GasSystem).GetField("_rng", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var previous = (System.Random)field.GetValue(null);
            try { GasSystem.SetRngForTests(new SpreadChoiceRandom(disperse)); GasSystem.OnTickEnd(zone); }
            finally { GasSystem.SetRngForTests(previous); }
        }
        private sealed class SpreadChoiceRandom : System.Random
        {
            private readonly bool _disperse;
            public SpreadChoiceRandom(bool disperse) { _disperse = disperse; }
            public override int Next(int maxValue) => _disperse ? 0 : maxValue - 1;
            public override int Next(int minValue, int maxValue) => maxValue - 1;
        }

        [Test]
        public void Contagion_HostInOwnSporeCloud_AlreadyInfected_NoReInfection()
        {
            // The host's own contagion gas DOES end up in the cell.
            // When GasFungalSporesPart.ApplyGas fires next turn on the
            // host, the "already infected" gate should bail with
            // gas/InfectionAlreadyPresent.
            var zone = new Zone("SelfImmunity");
            var host = MakeCreatureInZone(zone, 5, 5);
            var fx = new FungalInfectionEffect();
            host.ApplyEffect(fx);
            fx.TurnsInfected = 19;
            // Spawn contagion at host's cell
            var context = ContextWithZone(zone);
            try { fx.OnTurnStart(host, context); } finally { context.Release(); }
            Assert.AreEqual(1, zone.GetEntitiesWithTag("Gas").Count, "contagion spawned");
            var cloud = zone.GetEntitiesWithTag("Gas")[0];

            // Gas disperses BEFORE exposure. Pin no-spread for this self-exposure
            // invariant; the paired test above verifies the legitimate moved-cloud case.
            Diag.ResetAll();
            TickWithSpreadChoice(zone, false);
            Assert.AreEqual((5, 5), zone.GetEntityPosition(cloud), "Host must actually remain exposed.");
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "InfectionAlreadyPresent", Limit = 5 }).Records;
            Assert.Greater(recs.Count, 0,
                "host's already-infected status bails the gas dispatcher");
            // Stage clock preserved.
            Assert.AreSame(fx, host.GetEffect<FungalInfectionEffect>());
            Assert.AreEqual(20, fx.TurnsInfected, "stage clock unchanged by own spore cloud");
        }
    }
}
