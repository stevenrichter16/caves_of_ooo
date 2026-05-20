using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// G.3+G.4 — dispersal + merge engine tests. Two halves:
    /// (1) the unit-level methods (GetDispersalRate, MergeChunk,
    ///     IsMergeCompatible, Dissipate, ProcessGasBehavior single-tick
    ///     deterministic edges) — no RNG control needed.
    /// (2) integration: seeded-RNG OnTickEnd loops that exercise the
    ///     full spread + dissipation cycle and assert observable
    ///     outcomes (gas count over time, blocked-by-solid, seeping
    ///     passes through, etc.). Probabilistic in fine detail but
    ///     deterministic in aggregate behavior.
    /// </summary>
    public class GasSystemTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            // Standard test gas: poison-vapor, unstable, non-seeping, GasType=Poison.
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""poison-vapor"", ""DisplayName"":""poison vapor"",
                ""GasType"":""Poison"", ""Glyph"":""°"", ""Color"":""&g"",
                ""DefaultDensity"":100, ""DefaultLevel"":1 },
              { ""Id"":""stable-vapor"", ""DisplayName"":""stable vapor"",
                ""GasType"":""Stable"", ""Glyph"":""°"", ""Color"":""&w"",
                ""DefaultDensity"":100, ""DefaultLevel"":1,
                ""Stable"":true },
              { ""Id"":""seeping-vapor"", ""DisplayName"":""seeping vapor"",
                ""GasType"":""Seeping"", ""Glyph"":""°"", ""Color"":""&m"",
                ""DefaultDensity"":100, ""DefaultLevel"":1,
                ""Seeping"":true },
              { ""Id"":""cryo-mist"", ""DisplayName"":""cryo mist"",
                ""GasType"":""Cryo"", ""Glyph"":""°"", ""Color"":""&C"",
                ""DefaultDensity"":100, ""DefaultLevel"":1 },
              { ""Id"":""poison-red"", ""DisplayName"":""red poison"",
                ""GasType"":""Poison"", ""Glyph"":""°"", ""Color"":""&R"",
                ""DefaultDensity"":100, ""DefaultLevel"":1 } ] }");
            // Reset RNG to a known seed per-test for repeatability where
            // we DO depend on stochastic outcomes.
            GasSystem.SetRngForTests(new System.Random(42));
        }

        [TearDown]
        public void TearDown()
        {
            GasRegistry.ResetForTests();
            GasSystem.SetRngForTests(new System.Random()); // unseed for next consumer
        }

        // ════════════════════════════════════════════════════════════
        //   PART I — Unit-level methods (no RNG dependence)
        // ════════════════════════════════════════════════════════════

        // ──────────── IsMergeCompatible ────────────

        [Test]
        public void IsMergeCompatible_SameTypeAndColor_True()
        {
            var a = new GasPoolPart { GasType = "Poison", ColorString = "&g" };
            var b = new GasPoolPart { GasType = "Poison", ColorString = "&g" };
            Assert.IsTrue(GasSystem.IsMergeCompatible(a, b));
        }

        [Test]
        public void IsMergeCompatible_DifferentType_False()
        {
            var a = new GasPoolPart { GasType = "Poison", ColorString = "&g" };
            var b = new GasPoolPart { GasType = "Cryo",   ColorString = "&g" };
            Assert.IsFalse(GasSystem.IsMergeCompatible(a, b));
        }

        [Test]
        public void IsMergeCompatible_DifferentColor_False()
        {
            // Same GasType but different color — Qud parity (reskinned
            // variants of the same gas don't merge).
            var a = new GasPoolPart { GasType = "Poison", ColorString = "&g" };
            var b = new GasPoolPart { GasType = "Poison", ColorString = "&R" };
            Assert.IsFalse(GasSystem.IsMergeCompatible(a, b));
        }

        [Test]
        public void IsMergeCompatible_NullSide_False()
        {
            var a = new GasPoolPart { GasType = "Poison", ColorString = "&g" };
            Assert.IsFalse(GasSystem.IsMergeCompatible(null, a));
            Assert.IsFalse(GasSystem.IsMergeCompatible(a, null));
        }

        // ──────────── MergeChunk ────────────

        [Test]
        public void MergeChunk_DensityMovesFromSrcToDst()
        {
            var zone = new Zone("MergeTest");
            var s = GasFactory.SpawnGas(zone, 1, 1, "poison-vapor", density: 50);
            var d = GasFactory.SpawnGas(zone, 2, 2, "poison-vapor", density: 30);
            var sp = s.GetPart<GasPoolPart>();
            var dp = d.GetPart<GasPoolPart>();
            GasSystem.MergeChunk(sp, dp, 20);
            Assert.AreEqual(30, sp.Density, "donor lost 20");
            Assert.AreEqual(50, dp.Density, "receiver gained 20");
        }

        [Test]
        public void MergeChunk_ChunkLargerThanSrc_ClampsToSrcDensity()
        {
            var zone = new Zone("MergeTest");
            var s = GasFactory.SpawnGas(zone, 1, 1, "poison-vapor", density: 5);
            var d = GasFactory.SpawnGas(zone, 2, 2, "poison-vapor", density: 30);
            var sp = s.GetPart<GasPoolPart>();
            var dp = d.GetPart<GasPoolPart>();
            GasSystem.MergeChunk(sp, dp, 100); // chunk > src.Density
            Assert.AreEqual(0, sp.Density, "donor fully drained, capped");
            Assert.AreEqual(35, dp.Density, "receiver only gained the 5 that was actually there");
        }

        [Test]
        public void MergeChunk_NegativeChunk_NoOp()
        {
            var zone = new Zone("MergeTest");
            var s = GasFactory.SpawnGas(zone, 1, 1, "poison-vapor", density: 50);
            var d = GasFactory.SpawnGas(zone, 2, 2, "poison-vapor", density: 30);
            var sp = s.GetPart<GasPoolPart>();
            var dp = d.GetPart<GasPoolPart>();
            GasSystem.MergeChunk(sp, dp, -5);
            Assert.AreEqual(50, sp.Density);
            Assert.AreEqual(30, dp.Density);
        }

        [Test]
        public void MergeChunk_ReceiverInheritsMaxLevel()
        {
            var zone = new Zone("MergeTest");
            var s = GasFactory.SpawnGas(zone, 1, 1, "poison-vapor", density: 50, level: 5);
            var d = GasFactory.SpawnGas(zone, 2, 2, "poison-vapor", density: 30, level: 2);
            var sp = s.GetPart<GasPoolPart>();
            var dp = d.GetPart<GasPoolPart>();
            GasSystem.MergeChunk(sp, dp, 10);
            Assert.AreEqual(5, dp.Level, "receiver promoted to higher level");
        }

        [Test]
        public void MergeChunk_ReceiverKeepsLevel_IfDonorLower()
        {
            // Counter: if donor's Level is LOWER than receiver's, receiver
            // does NOT downgrade.
            var zone = new Zone("MergeTest");
            var s = GasFactory.SpawnGas(zone, 1, 1, "poison-vapor", density: 50, level: 1);
            var d = GasFactory.SpawnGas(zone, 2, 2, "poison-vapor", density: 30, level: 4);
            var sp = s.GetPart<GasPoolPart>();
            var dp = d.GetPart<GasPoolPart>();
            GasSystem.MergeChunk(sp, dp, 10);
            Assert.AreEqual(4, dp.Level, "lower-level donor doesn't downgrade receiver");
        }

        [Test]
        public void MergeChunk_SeepingOrsIntoReceiver()
        {
            var zone = new Zone("MergeTest");
            var s = GasFactory.SpawnGas(zone, 1, 1, "seeping-vapor", density: 50);
            var d = GasFactory.SpawnGas(zone, 2, 2, "seeping-vapor", density: 30);
            var sp = s.GetPart<GasPoolPart>();
            var dp = d.GetPart<GasPoolPart>();
            sp.Seeping = true;
            dp.Seeping = false;
            GasSystem.MergeChunk(sp, dp, 10);
            Assert.IsTrue(dp.Seeping, "OR: donor seeping → receiver seeping");
        }

        [Test]
        public void MergeChunk_ReceiverKeepsSeeping_IfDonorNotSeeping()
        {
            // Counter: receiver doesn't LOSE seeping when donor isn't seeping.
            var zone = new Zone("MergeTest");
            var s = GasFactory.SpawnGas(zone, 1, 1, "poison-vapor", density: 50);
            var d = GasFactory.SpawnGas(zone, 2, 2, "poison-vapor", density: 30);
            var sp = s.GetPart<GasPoolPart>();
            var dp = d.GetPart<GasPoolPart>();
            sp.Seeping = false;
            dp.Seeping = true;
            GasSystem.MergeChunk(sp, dp, 10);
            Assert.IsTrue(dp.Seeping, "donor non-seeping doesn't disable receiver seeping");
        }

        [Test]
        public void MergeChunk_ReceiverInheritsCreator_IfNull()
        {
            var zone = new Zone("MergeTest");
            var creator = new Entity { ID = "c", BlueprintName = "Source" };
            var s = GasFactory.SpawnGas(zone, 1, 1, "poison-vapor", density: 50, creator: creator);
            var d = GasFactory.SpawnGas(zone, 2, 2, "poison-vapor", density: 30, creator: null);
            var sp = s.GetPart<GasPoolPart>();
            var dp = d.GetPart<GasPoolPart>();
            GasSystem.MergeChunk(sp, dp, 10);
            Assert.AreSame(creator, dp.Creator, "receiver inherits donor's Creator when its own is null");
        }

        [Test]
        public void MergeChunk_ReceiverKeepsCreator_IfAlreadySet()
        {
            // Counter: receiver already has a Creator → donor's doesn't override.
            var zone = new Zone("MergeTest");
            var donorCreator = new Entity { ID = "donor", BlueprintName = "Donor" };
            var receiverCreator = new Entity { ID = "rx", BlueprintName = "Receiver" };
            var s = GasFactory.SpawnGas(zone, 1, 1, "poison-vapor", density: 50, creator: donorCreator);
            var d = GasFactory.SpawnGas(zone, 2, 2, "poison-vapor", density: 30, creator: receiverCreator);
            var sp = s.GetPart<GasPoolPart>();
            var dp = d.GetPart<GasPoolPart>();
            GasSystem.MergeChunk(sp, dp, 10);
            Assert.AreSame(receiverCreator, dp.Creator, "receiver keeps its own Creator");
        }

        // ──────────── Dissipate ────────────

        [Test]
        public void Dissipate_RemovesEntityFromZone()
        {
            var zone = new Zone("DissipateTest");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor");
            GasSystem.Dissipate(gas, gas.GetPart<GasPoolPart>(), zone, "TestCleanup");
            Assert.AreEqual(-1, zone.GetEntityPosition(gas).x,
                "gas no longer in zone after Dissipate");
        }

        [Test]
        public void Dissipate_EmitsDiag()
        {
            var zone = new Zone("DissipateTest");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 7);
            Diag.ResetAll();
            GasSystem.Dissipate(gas, gas.GetPart<GasPoolPart>(), zone, "TestCleanup");
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "Dissipated", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count);
            StringAssert.Contains("\"cause\":\"TestCleanup\"", recs[0].PayloadJson);
            StringAssert.Contains("\"density\":7", recs[0].PayloadJson);
        }

        // ──────────── GetDispersalRate ────────────

        [Test]
        public void GetDispersalRate_ReturnsInRange()
        {
            // Without a creator, rate is uniformly in [MIN, MAX].
            var zone = new Zone("RateTest");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 100);
            var pool = gas.GetPart<GasPoolPart>();
            for (int i = 0; i < 50; i++)
            {
                int rate = GasSystem.GetDispersalRate(pool);
                Assert.GreaterOrEqual(rate, GasSystem.MIN_DISPERSAL_RATE);
                Assert.LessOrEqual(rate, GasSystem.MAX_DISPERSAL_RATE);
            }
        }

        [Test]
        public void GetDispersalRate_CreatorModifyEvent_AmplifiesRate()
        {
            // The Creator's CreatorModifyGasDispersal event listener can
            // mutate the rate. Verify the event fires and the modified
            // value is used. (GasTumbler in G.7 uses this hook.)
            var zone = new Zone("RateTest");
            var creator = new Entity { ID = "c", BlueprintName = "Source" };
            creator.AddPart(new GasDispersalRateDoubler());
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 100, creator: creator);
            var pool = gas.GetPart<GasPoolPart>();
            // With the doubler, rate must always be >= 2 (since baseline >= 1).
            for (int i = 0; i < 20; i++)
            {
                int rate = GasSystem.GetDispersalRate(pool);
                Assert.GreaterOrEqual(rate, 2, "doubler took effect");
            }
        }

        // ──────────── ProcessGasBehavior (single-tick deterministic) ────────────

        [Test]
        public void ProcessGasBehavior_UnstableGas_DensityDecreases()
        {
            var zone = new Zone("DispersalTest");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 100);
            int before = gas.GetPart<GasPoolPart>().Density;
            GasSystem.ProcessGasBehavior(gas, zone);
            int after = gas.GetPart<GasPoolPart>().Density;
            Assert.Less(after, before, "unstable gas decays each tick");
        }

        [Test]
        public void ProcessGasBehavior_StableGas_DensityUnchangedByDecay()
        {
            // Stable gas does NOT decay. Spread may still happen (in
            // which case density decreases by the spread chunk), so to
            // isolate "no decay" we set density just above the spread
            // threshold and confirm density never decreases by MORE than
            // possible from spread.
            var zone = new Zone("DispersalTest");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "stable-vapor", density: 100);
            // Make the surrounding cells solid so spread can't happen.
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (dx != 0 || dy != 0)
                        AddWall(zone, 5 + dx, 5 + dy);
            int before = gas.GetPart<GasPoolPart>().Density;
            for (int i = 0; i < 20; i++)
                GasSystem.ProcessGasBehavior(gas, zone);
            int after = gas.GetPart<GasPoolPart>().Density;
            Assert.AreEqual(before, after, "stable gas blocked by walls doesn't decay");
        }

        [Test]
        public void ProcessGasBehavior_ZeroDensity_Dissipates()
        {
            var zone = new Zone("DispersalTest");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 100);
            gas.GetPart<GasPoolPart>().Density = 0; // force directly
            GasSystem.ProcessGasBehavior(gas, zone);
            Assert.AreEqual(-1, zone.GetEntityPosition(gas).x,
                "zero-density gas dissipates on next tick");
        }

        [Test]
        public void ProcessGasBehavior_NullGas_NoCrash()
        {
            var zone = new Zone("DispersalTest");
            Assert.DoesNotThrow(() => GasSystem.ProcessGasBehavior(null, zone));
        }

        [Test]
        public void ProcessGasBehavior_NullZone_NoCrash()
        {
            var zone = new Zone("DispersalTest");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor");
            Assert.DoesNotThrow(() => GasSystem.ProcessGasBehavior(gas, null));
        }

        // ════════════════════════════════════════════════════════════
        //   PART II — Integration: seeded OnTickEnd loops
        // ════════════════════════════════════════════════════════════

        [Test]
        public void OnTickEnd_NullZone_NoCrash()
        {
            Assert.DoesNotThrow(() => GasSystem.OnTickEnd(null));
        }

        [Test]
        public void OnTickEnd_EmptyZone_NoCrash()
        {
            var zone = new Zone("Empty");
            Assert.DoesNotThrow(() => GasSystem.OnTickEnd(zone));
        }

        [Test]
        public void OnTickEnd_UnstableGas_EventuallyDissipates()
        {
            // Unstable gas in an open cell — over many ticks the
            // combination of decay + spread + low-density flicker should
            // remove it from the zone (density drains to 0 or low-flicker
            // catches it).
            var zone = new Zone("DissipateLoop");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 30);
            int ticks = 0;
            while (zone.GetEntityPosition(gas).x >= 0 && ticks < 500)
            {
                GasSystem.OnTickEnd(zone);
                ticks++;
            }
            Assert.Less(ticks, 500,
                $"unstable gas dissipates within 500 ticks (took {ticks})");
        }

        [Test]
        public void OnTickEnd_GasSpreadsToAdjacentCells_OverTime()
        {
            // Open zone, dense gas → spread events accumulate in diag.
            // Assertion is via `gas/Spread` record count, not live entity
            // count, because the spread spawns are themselves unstable
            // and may have already dissipated by the time the loop ends.
            // The diag captures every spread that EVER happened.
            var zone = new Zone("SpreadLoop");
            GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 500);
            for (int i = 0; i < 100; i++)
                GasSystem.OnTickEnd(zone);
            var spreads = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "Spread", Limit = 200 }).Records;
            Assert.Greater(spreads.Count, 0,
                $"gas spread at least once over 100 ticks (records={spreads.Count})");
        }

        [Test]
        public void OnTickEnd_GasBlockedBySolid_DoesNotSpread()
        {
            // Counter: surround a gas with walls; even after many ticks,
            // total gas count stays at 1 (gas can't escape).
            var zone = new Zone("BlockedSpread");
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (dx != 0 || dy != 0)
                        AddWall(zone, 5 + dx, 5 + dy);
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 100);
            for (int i = 0; i < 30; i++)
            {
                // Re-spawn if it dissipates so we keep the spread-attempt
                // counter alive without losing the topology.
                if (zone.GetEntityPosition(gas).x < 0)
                    gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 100);
                GasSystem.OnTickEnd(zone);
            }
            // Count gas entities INSIDE the walled cell only.
            int outsideWall = 0;
            var allGas = zone.GetEntitiesWithTag("Gas");
            foreach (var g in allGas)
            {
                var p = zone.GetEntityPosition(g);
                if (p.x != 5 || p.y != 5) outsideWall++;
            }
            Assert.AreEqual(0, outsideWall,
                "non-seeping gas couldn't escape the wall ring");
        }

        [Test]
        public void OnTickEnd_SeepingGas_PassesThroughSolidEventually()
        {
            // Counter to the above: seeping gas DOES escape walls over time.
            var zone = new Zone("SeepingSpread");
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (dx != 0 || dy != 0)
                        AddWall(zone, 5 + dx, 5 + dy);
            GasFactory.SpawnGas(zone, 5, 5, "seeping-vapor", density: 100);
            bool escaped = false;
            for (int i = 0; i < 100 && !escaped; i++)
            {
                GasSystem.OnTickEnd(zone);
                var allGas = zone.GetEntitiesWithTag("Gas");
                foreach (var g in allGas)
                {
                    var p = zone.GetEntityPosition(g);
                    if (p.x != 5 || p.y != 5)
                    {
                        escaped = true;
                        break;
                    }
                }
            }
            Assert.IsTrue(escaped, "seeping gas escapes the wall ring within 100 ticks");
        }

        [Test]
        public void OnTickEnd_TwoCompatibleGases_Merge_DonorChunkMovesToReceiver()
        {
            // G.4 integration: two compatible poison-vapor clouds in
            // adjacent cells. Donor spreads into receiver's cell;
            // receiver density should grow (donor merged in). Pin via
            // the diag count (a Merged record fires per merge).
            var zone = new Zone("MergeLoop");
            GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 200);
            GasFactory.SpawnGas(zone, 6, 5, "poison-vapor", density: 50);
            for (int i = 0; i < 30; i++)
                GasSystem.OnTickEnd(zone);
            var merges = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "Merged", Limit = 50 }).Records;
            Assert.Greater(merges.Count, 0,
                "at least one Merged record (compatible adjacent gases merged)");
        }

        [Test]
        public void OnTickEnd_TwoIncompatibleGases_NoCrossTypeMerges()
        {
            // Counter: different GasTypes (Poison vs Cryo) never merge.
            // Intra-species merges (poison→poison from spread spawns)
            // ARE expected and DO emit Merged records, so the assertion
            // is "no record has donorType != receiverType" — that would
            // be a bug in IsMergeCompatible.
            var zone = new Zone("IncompatMergeLoop");
            GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 300);
            GasFactory.SpawnGas(zone, 6, 5, "cryo-mist",    density: 300);
            for (int i = 0; i < 50; i++)
                GasSystem.OnTickEnd(zone);
            var merges = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "Merged", Limit = 200 }).Records;
            int crossType = 0;
            foreach (var r in merges)
            {
                string p = r.PayloadJson;
                // Extract donorType and receiverType, look for mismatch.
                if (p.Contains("\"donorType\":\"Poison\"") && p.Contains("\"receiverType\":\"Cryo\"")) crossType++;
                if (p.Contains("\"donorType\":\"Cryo\"")   && p.Contains("\"receiverType\":\"Poison\"")) crossType++;
            }
            Assert.AreEqual(0, crossType,
                "no merge crosses GasTypes (would indicate IsMergeCompatible bug)");
        }

        [Test]
        public void OnTickEnd_SameTypeDifferentColor_NoCrossColorMerges()
        {
            // Counter: same GasType but different ColorString — Qud
            // parity (color is part of merge identity). Same shape as
            // the cross-type test: assert NO Merged record has
            // donorColor != receiverColor.
            var zone = new Zone("ColorMergeLoop");
            GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 300); // GasType=Poison, Color=&g
            GasFactory.SpawnGas(zone, 6, 5, "poison-red",   density: 300); // GasType=Poison, Color=&R
            for (int i = 0; i < 50; i++)
                GasSystem.OnTickEnd(zone);
            var merges = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "Merged", Limit = 200 }).Records;
            int crossColor = 0;
            foreach (var r in merges)
            {
                string p = r.PayloadJson;
                if (p.Contains("\"donorColor\":\"&g\"") && p.Contains("\"receiverColor\":\"&R\"")) crossColor++;
                if (p.Contains("\"donorColor\":\"&R\"") && p.Contains("\"receiverColor\":\"&g\"")) crossColor++;
            }
            Assert.AreEqual(0, crossColor,
                "no merge crosses ColorStrings (would indicate IsMergeCompatible bug)");
        }

        [Test]
        public void OnTickEnd_DispersalEmitsDispersedDiag()
        {
            var zone = new Zone("DispDiag");
            GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 100);
            Diag.ResetAll();
            GasSystem.OnTickEnd(zone);
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "Dispersed", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count, "one Dispersed record per unstable gas per tick");
            StringAssert.Contains("\"gasId\":\"poison-vapor\"", recs[0].PayloadJson);
        }

        [Test]
        public void OnTickEnd_NewlySpawnedGas_DoesNotProcessSameTick()
        {
            // Iteration safety: new gases spawned via spread during this
            // tick should NOT process THIS tick (they'd process next tick).
            // Verify by checking that the Dispersed-record count equals
            // the SNAPSHOT count, not the post-tick count.
            var zone = new Zone("SnapshotSafety");
            GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 100);
            int before = zone.GetEntitiesWithTag("Gas").Count;
            Diag.ResetAll();
            GasSystem.OnTickEnd(zone);
            int after = zone.GetEntitiesWithTag("Gas").Count;
            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "Dispersed", Limit = 50 }).Records;
            // One Dispersed per original gas; new gases spawned this tick
            // shouldn't have produced their own Dispersed records.
            Assert.AreEqual(before, recs.Count,
                "Dispersed records match snapshot count (new gases queued for next tick)");
            Assert.GreaterOrEqual(after, before,
                "spread may have added new gas entities (counts as before+ for next tick)");
        }

        // ══════════════════════════════════════════════════════════════
        // Helpers
        // ══════════════════════════════════════════════════════════════

        private static void AddWall(Zone zone, int x, int y)
        {
            if (!zone.InBounds(x, y)) return;
            var wall = new Entity { ID = $"wall_{x}_{y}", BlueprintName = "Wall" };
            wall.Tags["Solid"] = "";
            wall.Tags["Wall"] = "";
            wall.AddPart(new RenderPart { DisplayName = "wall", RenderString = "#" });
            wall.AddPart(new PhysicsPart { Solid = true });
            zone.AddEntity(wall, x, y);
        }
    }

    /// <summary>
    /// Test-support Part: doubles the dispersal rate via the Qud-parity
    /// CreatorModifyGasDispersal event. Lives on a Creator entity; when
    /// that creator's gas runs its dispersal tick, this Part intercepts
    /// the event and writes 2× the proposed Rate back into the param.
    /// Stub for G.7's GasTumbler.
    /// </summary>
    internal class GasDispersalRateDoubler : Part
    {
        public override string Name => "GasDispersalRateDoubler";
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "CreatorModifyGasDispersal") return true;
            int rate = e.GetParameter<int>("Rate");
            e.SetParameter("Rate", (object)(rate * 2));
            return true;
        }
    }
}
