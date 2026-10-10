using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Detached actual-content capture workload, never the player's live
    /// graph. Invoke explicitly; absolute timing is evidence, not a pass threshold.</summary>
    public sealed class SteamDeckSavePerformanceProbe
    {
        public static string LastReportJson;
        [Serializable] public sealed class Row
        {
            public int requestedZones, cachedZones, placedOwners, sample, bytes;
            public double captureMilliseconds, compressionMilliseconds, commitMilliseconds;
        }
        [Serializable] public sealed class Report
        {
            public string utc, unityVersion, operatingSystem, canVerify, cannotVerify;
            public int seed;
            public List<Row> rows = new List<Row>();
        }

        [Test, Explicit("Detached actual-content save performance sample; no absolute speed assertion.")]
        public void MeasureOneAndTwentyGeneratedZones()
        {
            LastReportJson = null;
            var report = new Report
            {
                utc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion,
                operatingSystem = SystemInfo.operatingSystem, seed = 64,
                canVerify = "Native Unity elapsed capture, memory gzip and atomic file/metadata commit for detached generated content with production loot wiring.",
                cannotVerify = "Steam Deck CPU/GPU/frame time, visual feel, ordinary travel binding/generation cost, and historical paired speedup. This is a current-arm save-stage sample."
            };
            using (var content = new DensityLootTestScope())
            // Content scope has its own save fixture. Install our isolated root
            // and capture binding last, and restore them before disposing it.
            using (var f = new NewGameSaveFixture())
            {
                try
                {
                    var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
                    var ids = new List<string>();
                    for (int y = 8; y < 12; y++) for (int x = 8; x < 13; x++) ids.Add(WorldMap.ToZoneID(x, y, 0));
                    foreach (int target in new[] { 1, 20 })
                    {
                        for (int i = manager.CachedZones.Count; i < target; i++)
                        {
                            content.Seed(unchecked(64 ^ FormationSelector.StableIndex(ids[i], int.MaxValue)));
                            Assert.NotNull(manager.GetZone(ids[i]), ids[i]);
                        }
                        manager.SetActiveZone(ids[0]);
                        f.Fresh.ZoneManager = manager; f.Fresh.WorldSeed = manager.WorldSeed; f.Fresh.ActiveZoneID = ids[0];
                        int owners = 0; foreach (var zone in manager.CachedZones.Values) owners += zone.EntityCount;
                        Assert.AreEqual(target, manager.CachedZones.Count);
                        Assert.Greater(owners, target * 1000, "The measured workload must contain generated terrain owners.");
                        f.Register(() =>
                        {
                            f.Captures++;
                            Assert.AreSame(manager, f.Fresh.ZoneManager, "Capture must serialize the generated manager.");
                            Assert.AreEqual(target, f.Fresh.ZoneManager.CachedZones.Count);
                            return f.Fresh;
                        }, f.NewID);
                        // Warm one capture/commit for metadata cache/JIT and first file creation.
                        CaptureAndDrain(f, owners);
                        VerifyCheckpoint(f, content.Factory, manager, owners);
                        for (int sample = 0; sample < 3; sample++)
                        {
                            CaptureAndDrain(f, owners);
                            var timings = SaveGameService.LastCompletedSavePerformance;
                            Assert.IsTrue(timings.Succeeded);
                            report.rows.Add(new Row { requestedZones = target, cachedZones = manager.CachedZones.Count,
                                placedOwners = owners, sample = sample, bytes = timings.UncompressedBytes,
                                captureMilliseconds = timings.CaptureMilliseconds,
                                compressionMilliseconds = timings.CompressionMilliseconds,
                                commitMilliseconds = timings.CommitMilliseconds });
                        }
                        VerifyCheckpoint(f, content.Factory, manager, owners);
                    }
                }
                finally { SaveGameService.FlushPendingSaves(); }
            }
            LastReportJson = JsonUtility.ToJson(report, true);
            Debug.Log("[DeckSavePerformance] " + LastReportJson);
        }

        private static void CaptureAndDrain(NewGameSaveFixture fixture, int owners)
        {
            int before = fixture.Captures;
            Assert.IsTrue(SaveGameService.RequestQuickSave());
            Assert.AreEqual(before + 1, fixture.Captures, "The registered workload callback must be invoked.");
            Assert.IsTrue(SaveGameService.FlushPendingSaves());
            Assert.AreEqual(fixture.NewID, fixture.ActiveID);
            Assert.Greater(SaveGameService.LastCompletedSavePerformance.UncompressedBytes, owners * 32,
                "A tiny fixture session is not a valid generated-world timing sample.");
        }

        private static void VerifyCheckpoint(NewGameSaveFixture fixture, EntityFactory factory,
            OverworldZoneManager expected, int owners)
        {
            var loaded = SaveGameService.LoadState(Path.Combine(fixture.Root, fixture.NewID, "Quick.sav.gz"), factory);
            Assert.AreEqual(fixture.NewID, loaded.GameID);
            Assert.NotNull(loaded.ZoneManager);
            Assert.AreEqual(expected.CachedZones.Count, loaded.ZoneManager.CachedZones.Count);
            int restoredOwners = 0;
            foreach (var entry in expected.CachedZones)
            {
                Assert.IsTrue(loaded.ZoneManager.CachedZones.TryGetValue(entry.Key, out var restored), entry.Key);
                Assert.AreEqual(entry.Value.EntityCount, restored.EntityCount, entry.Key);
                restoredOwners += restored.EntityCount;
            }
            Assert.AreEqual(owners, restoredOwners, "All generated owners must survive the checkpoint roundtrip.");
        }
    }
}
