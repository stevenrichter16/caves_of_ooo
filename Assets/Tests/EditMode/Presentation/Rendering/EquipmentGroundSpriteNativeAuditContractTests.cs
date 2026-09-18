using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Scenarios.Custom;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Receipt-only counterchecks: these do not substitute for a live native run.
    public class EquipmentGroundSpriteNativeAuditContractTests
    {
        // Editor tooling lives in a separate assembly, as in the existing
        // native-report tests; do not widen the gameplay test asmdef for this seam.
        private static bool Validate(string json, string id, string mode, string root)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("CavesOfOoo.Editor.EquipmentGroundSpriteNativeAuditBatch"))
                .FirstOrDefault(t => t != null);
            Assert.NotNull(type, "The actual Editor report validator must be loaded.");
            var method = type.GetMethod("ValidateReport", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method);
            return (bool)method.Invoke(null, new object[] { json, id, mode, root });
        }
        private const string Id = "f9b08a1d88184f5b91c214c7b29c9616";
        private const string Root = "/private-owned-ground-audit";
        [TestCase("before")]
        [TestCase("after")]
        public void CompleteReceiptAcceptsItsExactModeAndRejectsTheOpposite(string mode)
        {
            string json = JsonUtility.ToJson(Complete(mode));
            Assert.IsTrue(Validate(json, Id, mode, Root));
            Assert.IsFalse(Validate(json, Id, mode == "before" ? "after" : "before", Root));
            Assert.IsFalse(Validate(json, Guid.NewGuid().ToString("N"), mode, Root));
            Assert.IsFalse(Validate(json, Id, mode, Root + "-other"));
        }
        [TestCase("short")][TestCase("empty")][TestCase("missing-phase")][TestCase("invalid-marker")]
        [TestCase("missing-active-samples")][TestCase("wrong-units")][TestCase("incomplete-work")]
        [TestCase("unrestored")][TestCase("missing-claim")][TestCase("wrong-claim")]
        [TestCase("stale-artifact")][TestCase("missing-overlay-cycles")][TestCase("raw-gap")]
        public void PartialOrMisleadingEvidenceCannotPass(string mutation)
        {
            var r = Complete("after");
            switch (mutation)
            {
                case "short": r.phases[0].seconds = 24.9; break;
                case "empty": r.phases = Array.Empty<EquipmentGroundSpriteNativeAudit.Phase>(); break;
                case "missing-phase": r.phases[1] = null; break;
                case "invalid-marker": r.phases[0].metrics[0].available = false; break;
                case "missing-active-samples": r.phases[1].metrics[0].count = 0; break;
                case "wrong-units": r.phases[0].metrics[0].units = "Bytes"; break;
                case "incomplete-work": r.phases[1].operations = 99; break;
                case "unrestored": r.cleanupObserved = false; break;
                case "missing-claim": r.claims[0] = null; break;
                case "wrong-claim": r.claims[0].tile = "WeaponGround"; break;
                case "stale-artifact": r.screenshots[0] = "/old-run-normal.png"; break;
                case "missing-overlay-cycles": r.phases[2].overlayChecks = 0; break;
                case "raw-gap": r.phases[1].rawStart++; break;
            }
            Assert.IsFalse(Validate(JsonUtility.ToJson(r), Id, "after", Root), mutation);
        }
        [Test]
        public void ValidSparseIdleIsDistinctFromAnUnavailableMarkerOrStaleReplayedWork()
        {
            var r = Complete("after");
            Assert.AreEqual(0, r.phases[0].metrics[0].count);
            Assert.IsTrue(Validate(JsonUtility.ToJson(r), Id, "after", Root));
            r.phases[0].metrics[0].sum = 100; r.phases[0].metrics[0].average = 100d / 1500;
            Assert.IsFalse(Validate(JsonUtility.ToJson(r), Id, "after", Root));
        }
        [Test]
        public void EmptyOrMalformedReportFailsClosed()
        {
            foreach (string json in new[] { null, "", "{}", "not json" })
                Assert.IsFalse(Validate(json, Id, "after", Root));
        }
        private static EquipmentGroundSpriteNativeAudit.Report Complete(string mode)
        {
            var checks = new List<EquipmentGroundSpriteNativeAudit.Check>();
            foreach (string name in new[] { "real_production_renderer", "actual_14_blueprints_four_copies", "expected_logical_mode", "dim_uses_native_lightmap",
                "release_restores_item_glyph", "release_reclaims_item", "workload_complete", "native_inventory_roundtrip", "owned_save_only" })
                checks.Add(new EquipmentGroundSpriteNativeAudit.Check { name = name, pass = true });
            string[] bps = { "Dagger", "ShortSword", "LongSword", "Spear", "LeatherBoots", "IronshodBoots", "LeatherGloves", "LeatherCap", "IronHelmet", "Mace", "FireTonic", "LeatherArmor", "Chest", "WatchLantern" };
            string[] after = { "item_dagger", "item_sword", "item_sword", "item_spear", "item_boots", "item_boots", "item_gloves", "item_helmet", "item_helmet", "item_mace", "item_vial", "item_armor", "Chest", "Lantern" };
            string[] before = { "WeaponGround", "WeaponGround", "WeaponGround", "WeaponGround", "item_armor", "item_armor", "item_armor", "item_armor", "item_armor", "item_vial", "item_vial", "item_armor", "Chest", "Lantern" };
            var claims = new List<EquipmentGroundSpriteNativeAudit.Claim>();
            foreach (string light in new[] { "normal", "dim" }) for (int i = 0; i < bps.Length; i++)
            {
                string tile = mode == "after" ? after[i] : before[i];
                claims.Add(new EquipmentGroundSpriteNativeAudit.Claim { blueprint = bps[i], light = light, expected = tile, tile = tile, sprite = "native-sprite", x = 5 + 5 * i, zoneY = 5 });
                checks.Add(new EquipmentGroundSpriteNativeAudit.Check { name = light + "_claim_" + bps[i], pass = true });
            }
            var phases = new EquipmentGroundSpriteNativeAudit.Phase[3];
            string[] names = { "idle", "walk_full_redraw", "pickup_drop_incremental" };
            string[] metrics = { "COO.EnvSprites.PostRender", "COO.ZoneRenderer.LateUpdate", "COO.Input.Update", "Main Thread", "GC Allocated In Frame" };
            for (int i = 0; i < phases.Length; i++)
            {
                var p = new EquipmentGroundSpriteNativeAudit.Phase { name = names[i], seconds = 25.02, frames = 1500, rawStart = i * 1500,
                    operations = i == 0 ? 0 : 100, moves = i == 1 ? 100 : 0, pickups = i == 2 ? 50 : 0, drops = i == 2 ? 50 : 0,
                    overlayChecks = i == 2 ? 100 : 0, counters = new long[8], metrics = new EquipmentGroundSpriteNativeAudit.Metric[5] };
                if (i == 1) { p.counters[0] = 100; p.counters[1] = 100; p.counters[3] = 200000; }
                if (i == 2) { p.counters[0] = 100; p.counters[2] = 100; p.counters[3] = 900; }
                for (int j = 0; j < metrics.Length; j++)
                {
                    bool sparse = i == 0 && j == 0; long value = sparse || j == 4 ? 0 : 10;
                    p.metrics[j] = new EquipmentGroundSpriteNativeAudit.Metric { name = metrics[j], units = j == 4 ? "Bytes" : "TimeNanoseconds",
                        available = true, count = sparse ? 0 : 1500, observedFrames = 1500, zeroEmissionFrames = sparse ? 1500 : 0,
                        sum = value * 1500, average = value, max = value, p99 = value };
                    checks.Add(new EquipmentGroundSpriteNativeAudit.Check { name = names[i] + "_marker_" + j, pass = true });
                }
                phases[i] = p;
            }
            string stem = "/artifacts/EGSN-" + mode + "-" + Id;
            return new EquipmentGroundSpriteNativeAudit.Report { runId = Id, mode = mode, privateRoot = Root, workloadVersion = EquipmentGroundSpriteNativeAudit.WorkloadVersion,
                width = 1920, height = 1080, targetFrameRate = 60, rawFrameCount = 4500, phases = phases, checks = checks.ToArray(), claims = claims.ToArray(),
                cleanupObserved = true, settingsRestored = true, inputUntouched = true, rawFramesPath = stem + "-frames.csv",
                screenshots = new[] { stem + "-normal.png", stem + "-dim.png", stem + "-normal-world-nearest2x.png", stem + "-dim-world-nearest2x.png" } };
        }
    }
}
