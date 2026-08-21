using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Wx optimization review §1a + §1c
    /// (Docs/FELLING-WX-OPTIMIZATION-REVIEW.md) — merge-on-spawn.
    ///
    /// <para>§1a: <c>GasFactory.SpawnGas</c> created a fresh entity per
    /// call with no look-before-spawn, so BurnOffGas (one density-60
    /// cloud per 6 fire damage, no cap) stacked duplicate clouds on the
    /// same cell — a 10-20 tile peat fire sustained 50-250 concurrent
    /// gas entities, the scale of the documented 2026-05-23 236-entity
    /// editor freeze, and stacked siblings multiplied per-turn work and
    /// dose events. The fix reuses the shipped spread-merge semantics
    /// (GasSystem.MergeChunk: sum density, max level, OR seeping, keep
    /// receiver's creator) at spawn time: a spawn onto a cell holding a
    /// compatible gas (same GasType + ColorString, the IsMergeCompatible
    /// gate) grows that cloud instead of stacking a twin.</para>
    ///
    /// <para>§1c ride-along: <c>GasVisuals.Refresh</c> marked the cell
    /// dirty on every per-tick decay call even when the shade glyph and
    /// background were unchanged — one redundant repaint per gas per
    /// stationary turn. Refresh now skips the writes and the dirty mark
    /// when both outputs are unchanged (density bands only move at
    /// 30/80, so ~97% of decay ticks skip).</para>
    /// </summary>
    public class GasSpawnMergeTests
    {
        private int _gasDirtyMarks;

        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            Diag.ResetAll();
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""poison-vapor"", ""DisplayName"":""poison vapor"",
                ""GasType"":""Poison"", ""Glyph"":""°"", ""Color"":""&g"",
                ""DefaultDensity"":60, ""DefaultLevel"":1,
                ""BehaviorKind"":""Poison"" },
              { ""Id"":""toxin-mist"", ""DisplayName"":""toxin mist"",
                ""GasType"":""Poison"", ""Glyph"":""°"", ""Color"":""&r"",
                ""DefaultDensity"":60, ""DefaultLevel"":1,
                ""BehaviorKind"":""Poison"" },
              { ""Id"":""inert-mist"", ""DisplayName"":""inert mist"",
                ""GasType"":""Inert"", ""Glyph"":""°"", ""Color"":""&g"",
                ""DefaultDensity"":60, ""DefaultLevel"":1,
                ""BehaviorKind"":"""" } ] }");
            _gasDirtyMarks = 0;
            ZoneRenderHooks.CellDirtyCallback = (x, y, src) =>
            { if (src == "Gas") _gasDirtyMarks++; };
        }

        [TearDown]
        public void TearDown()
        {
            ZoneRenderHooks.CellDirtyCallback = null;
            GasRegistry.ResetForTests();
            SettlementRuntime.Reset();
        }

        private static System.Collections.Generic.List<GasPoolPart> GasAt(
            Zone zone, int x, int y)
        {
            var pools = new System.Collections.Generic.List<GasPoolPart>();
            foreach (var e in zone.GetAllEntities())
            {
                if (!e.Tags.ContainsKey("Gas")) continue;
                var pos = zone.GetEntityPosition(e);
                if (pos.x == x && pos.y == y) pools.Add(e.GetPart<GasPoolPart>());
            }
            return pools;
        }

        // ════════════════════════════════════════════════════════════
        //   §1a — merge-on-spawn
        // ════════════════════════════════════════════════════════════

        [Test]
        public void SpawnGas_OntoCompatibleCloud_GrowsIt_InsteadOfStacking()
        {
            var zone = new Zone("Merge1");
            GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 40);
            GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 25);

            var pools = GasAt(zone, 5, 5);
            Assert.AreEqual(1, pools.Count,
                "a compatible spawn merges — no stacked twin entities");
            Assert.AreEqual(65, pools[0].Density,
                "the receiver holds the summed density");
        }

        [Test]
        public void SpawnGas_Merge_ReturnsTheReceivingEntity()
        {
            var zone = new Zone("Merge2");
            var first = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 40);
            var second = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 25);
            Assert.AreSame(first, second,
                "callers get the cloud their density went into");
        }

        [Test]
        public void SpawnGas_Merge_TakesMaxLevel_KeepsReceiverCreator()
        {
            // Mirror of MergeChunk semantics (GasSystem.cs): max level,
            // receiver's creator wins when it has one.
            var zone = new Zone("Merge3");
            var creatorA = new Entity { ID = "A", BlueprintName = "A" };
            var creatorB = new Entity { ID = "B", BlueprintName = "B" };
            GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 40, level: 1, creator: creatorA);
            GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 10, level: 3, creator: creatorB);

            var pool = GasAt(zone, 5, 5)[0];
            Assert.AreEqual(3, pool.Level, "potency merges upward");
            Assert.AreSame(creatorA, pool.Creator,
                "the established cloud keeps its owner (MergeChunk parity)");
        }

        [Test]
        public void SpawnGas_SameType_DifferentColor_DoesNotMerge()
        {
            // Counter-check: the gate is IsMergeCompatible parity —
            // GasType AND ColorString. Poison &g and Poison &r coexist.
            var zone = new Zone("Merge4");
            GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 40);
            GasFactory.SpawnGas(zone, 5, 5, "toxin-mist", density: 25);
            Assert.AreEqual(2, GasAt(zone, 5, 5).Count,
                "different colors stay distinct clouds");
        }

        [Test]
        public void SpawnGas_DifferentType_SameColor_DoesNotMerge()
        {
            var zone = new Zone("Merge5");
            GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 40);
            GasFactory.SpawnGas(zone, 5, 5, "inert-mist", density: 25);
            Assert.AreEqual(2, GasAt(zone, 5, 5).Count,
                "different gas types stay distinct clouds");
        }

        [Test]
        public void SpawnGas_AdjacentCell_DoesNotMerge()
        {
            // Counter-check: the merge is cell-scoped.
            var zone = new Zone("Merge6");
            GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 40);
            GasFactory.SpawnGas(zone, 6, 5, "poison-vapor", density: 25);
            Assert.AreEqual(1, GasAt(zone, 5, 5).Count);
            Assert.AreEqual(1, GasAt(zone, 6, 5).Count);
        }

        [Test]
        public void SpawnGas_Merge_EmitsSpawnMergedDiag()
        {
            // Observability rule: the merge gate emits its record.
            var zone = new Zone("Merge7");
            GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 40);
            Diag.ResetAll();
            GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 25);

            var recs = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "SpawnMerged", Limit = 5 }).Records;
            Assert.AreEqual(1, recs.Count, "one SpawnMerged per merged spawn");
            StringAssert.Contains("\"receiverAfter\":65", recs[0].PayloadJson);
            var created = DiagQuery.Apply(new DiagQuery.Filter
            { Category = "gas", Kind = "Created", Limit = 5 }).Records;
            Assert.AreEqual(0, created.Count,
                "a merged spawn is not a Created — no phantom entity in the stream");
        }

        [Test]
        public void BurnOff_RepeatedThresholds_GrowOneCloud()
        {
            // The end-to-end scenario that motivated §1a: a burning peat
            // entity crossing its damage threshold repeatedly must feed
            // ONE cloud, not stack twins on its own cell.
            var zone = new Zone("MergeBurn");
            var peat = new Entity { ID = "peat", BlueprintName = "Peat" };
            peat.Tags["Creature"] = "";
            void S(string n, int v, int max = 100000) => peat.Statistics[n] =
                new Stat { Owner = peat, Name = n, BaseValue = v, Min = -200, Max = max };
            S("Hitpoints", 100000, 100000);
            S("HeatResistance", 0);
            peat.AddPart(new RenderPart { DisplayName = "peat" });
            peat.AddPart(new StatusEffectsPart());
            peat.AddPart(new BurnOffGasPart
            {
                GasId = "poison-vapor", DamagePer = 6, Chance = 100,
                Number = "1", GasDensity = 20
            });
            zone.AddEntity(peat, 5, 5);
            SettlementRuntime.ActiveZone = zone;

            for (int hit = 0; hit < 2; hit++)
            {
                var d = new Damage(6);
                d.AddAttribute("Heat");
                CombatSystem.ApplyDamage(peat, d, null, zone);
            }

            var pools = GasAt(zone, 5, 5);
            Assert.AreEqual(1, pools.Count,
                "two burn-off thresholds feed one cloud, not stacked twins");
            Assert.AreEqual(40, pools[0].Density,
                "both burn-offs' density landed in it");
        }

        // ════════════════════════════════════════════════════════════
        //   §1c — Refresh skips redundant repaints
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Refresh_UnchangedGlyphAndBackground_SkipsTheDirtyMark()
        {
            // Per-tick decay within one shade band (30/80 thresholds)
            // repainted the cell every turn for nothing.
            var zone = new Zone("Skip1");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 50);
            var pool = gas.GetPart<GasPoolPart>();

            _gasDirtyMarks = 0;
            GasVisuals.Refresh(gas, pool, zone); // density still 50 — ▒, same bg
            Assert.AreEqual(0, _gasDirtyMarks,
                "no visual change, no repaint");
        }

        [Test]
        public void Refresh_DensityCrossesShadeBand_MarksTheCell()
        {
            // Counter-check: a real band crossing still repaints.
            var zone = new Zone("Skip2");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 50);
            var pool = gas.GetPart<GasPoolPart>();

            _gasDirtyMarks = 0;
            pool.Density = 85; // ▒ → ▓
            GasVisuals.Refresh(gas, pool, zone);
            Assert.AreEqual(1, _gasDirtyMarks, "the ▓ core must paint");
            Assert.AreEqual(GasVisuals.SHADE_DARK.ToString(),
                gas.GetPart<RenderPart>().RenderString);
        }

        [Test]
        public void Refresh_ColorStringChange_MarksTheCell()
        {
            // Counter-check: a background change alone still repaints
            // (GasTumbler-style runtime recolor).
            var zone = new Zone("Skip3");
            var gas = GasFactory.SpawnGas(zone, 5, 5, "poison-vapor", density: 50);
            var pool = gas.GetPart<GasPoolPart>();

            _gasDirtyMarks = 0;
            pool.ColorString = "&r";
            GasVisuals.Refresh(gas, pool, zone);
            Assert.AreEqual(1, _gasDirtyMarks, "recolored cloud must repaint");
        }
    }
}
