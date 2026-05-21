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

            int spawnCount = 0;
            for (int i = 0; i < 10; i++) // turns 20..29
            {
                int before = zone.GetEntitiesWithTag("Gas").Count;
                fx.OnTurnStart(host, ContextWithZone(zone));
                int after = zone.GetEntitiesWithTag("Gas").Count;
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

            int spawnCount = 0;
            for (int i = 0; i < 10; i++) // turns 30..39
            {
                int before = zone.GetEntitiesWithTag("Gas").Count;
                fx.OnTurnStart(host, ContextWithZone(zone));
                int after = zone.GetEntitiesWithTag("Gas").Count;
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
            fx.OnTurnStart(host, ContextWithZone(zone));
            Assert.AreEqual(1, zone.GetEntitiesWithTag("Gas").Count, "contagion spawned");

            // Now run the gas's per-turn ApplyToCell pass — should
            // find the host already infected and bail.
            Diag.ResetAll();
            GasSystem.OnTickEnd(zone);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "InfectionAlreadyPresent", Limit = 5 }).Records;
            Assert.Greater(recs.Count, 0,
                "host's already-infected status bails the gas dispatcher");
            // Stage clock preserved.
            Assert.GreaterOrEqual(fx.TurnsInfected, 20,
                "stage clock not reset by own spore cloud");
        }
    }
}
