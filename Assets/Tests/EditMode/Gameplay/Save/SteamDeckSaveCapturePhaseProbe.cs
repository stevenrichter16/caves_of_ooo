using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CavesOfOoo.Tests
{
    public sealed class SteamDeckSaveCapturePhaseProbe
    {
        public static string LastReportJson;
        private const BindingFlags Hidden = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly MethodInfo TileWrite = typeof(SaveWriter).Assembly.GetType("CavesOfOoo.Core.SessionTileStateSerializer")
            .GetMethod("Write", Hidden);
        private static readonly FieldInfo EntityQueue = typeof(SaveWriter).GetField("_entityQueue", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo PublicFields = typeof(SaveGraphSerializer).GetMethod("GetSerializablePublicFields", Hidden);

        [Serializable] public sealed class PartRow
        {
            public string type;
            public int owners, fieldsPerOwner, scalarFieldsPerOwner;
        }
        [Serializable] public sealed class Row
        {
            public string arm;
            public bool metadataCachesCleared;
            public bool allocationCounterAvailable;
            public int zones, placedOwners, graphBodies, sample, bytes;
            public long allocatedBytes;
            public double totalMs, bindingMs, zoneManagerMs, globalsMs, entityBodiesMs, tileStateMs, metadataMs;
        }
        [Serializable] public sealed class Report
        {
            public string utc, unityVersion, operatingSystem, canVerify, cannotVerify;
            public bool allocationCounterAvailable;
            public List<Row> rows = new List<Row>();
            public List<PartRow> parts = new List<PartRow>();
        }

        [UnityTest, Explicit("Detached full-graph phase attribution with an exact production-byte oracle.")]
        public IEnumerator ProfileExistingCapturePhases() => Run(false);

        [UnityTest, Explicit("Alternating legacy/current full-graph comparison; requires writer-local legacy verification mode.")]
        public IEnumerator CompareLegacyAndDescriptorCapture() => Run(true);

        private static IEnumerator Run(bool paired)
        {
            LastReportJson = null;
            var legacy = typeof(SaveWriter).GetProperty("UseLegacyFieldWriters", BindingFlags.Instance | BindingFlags.NonPublic);
            if (paired) Assert.NotNull(legacy);
            var report = new Report
            {
                utc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion,
                operatingSystem = SystemInfo.operatingSystem,
                allocationCounterAvailable = CheckAllocationCounter(),
                canVerify = "Detached generated 1/20-zone graph, exact phased/ordinary save byte equivalence, writer-local paired arms when available, section elapsed times, actual serialized Part census. Caller allocation counts are valid only when allocationCounterAvailable is true; otherwise they are -1.",
                cannotVerify = "Steam Deck, release-player performance, complete process-cold startup, worker cost, frame feel, or live-world mutation between frames. Initial fixture sample can reuse process metadata caches."
            };
            using (var content = new DensityLootTestScope())
            using (var fixture = new NewGameSaveFixture())
            {
                var manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
                var ids = new List<string>();
                for (int y = 8; y < 12; y++) for (int x = 8; x < 13; x++) ids.Add(WorldMap.ToZoneID(x, y, 0));
                foreach (int target in new[] { 1, 20 })
                {
                    UnityEngine.Debug.Log("[DeckSaveCapturePhases] Generating " + target + "-zone workload.");
                    for (int i = manager.CachedZones.Count; i < target; i++)
                    {
                        content.Seed(unchecked(64 ^ FormationSelector.StableIndex(ids[i], int.MaxValue)));
                        Assert.NotNull(manager.GetZone(ids[i]));
                    }
                    manager.SetActiveZone(ids[0]);
                    var state = fixture.Fresh;
                    state.ZoneManager = manager; state.WorldSeed = manager.WorldSeed; state.ActiveZoneID = ids[0];
                    int owners = 0; foreach (var zone in manager.CachedZones.Values) owners += zone.EntityCount;
                    Assert.AreEqual(target, manager.CachedZones.Count); Assert.Greater(owners, target * 1000);
                    // Stabilize BindForSave's world identity before comparing two writes.
                    byte[] expected = Ordinary(state, false, legacy);
                    Assert.Greater(expected.Length, owners * 32);
                    using (var input = new MemoryStream(expected))
                    {
                        var restored = GameSessionState.Load(new SaveReader(input, content.Factory));
                        Assert.AreEqual(target, restored.ZoneManager.CachedZones.Count);
                        int restoredOwners = 0;
                        foreach (var entry in manager.CachedZones)
                        {
                            Assert.AreEqual(entry.Value.EntityCount, restored.ZoneManager.CachedZones[entry.Key].EntityCount);
                            restoredOwners += restored.ZoneManager.CachedZones[entry.Key].EntityCount;
                        }
                        Assert.AreEqual(owners, restoredOwners);
                    }
                    // Loading restores process globals; stabilize the independent
                    // ordinary oracle again before the paired sequence.
                    expected = Ordinary(state, false, legacy);
                    UnityEngine.Debug.Log("[DeckSaveCapturePhases] Validated " + target + " zones, " + owners + " owners, " + expected.Length + " bytes.");
                    yield return null;
                    if (ClearWriterMetadata())
                    {
                        var cold = new Row { arm = "current_cold_metadata", metadataCachesCleared = true,
                            allocationCounterAvailable = report.allocationCounterAvailable,
                            zones = target, placedOwners = owners, sample = -2 };
                        SaveWriter coldWriter;
                        AssertExactBytes(expected, Phased(state, cold, false, legacy, out coldWriter));
                        report.rows.Add(cold);
                        UnityEngine.Debug.Log("[DeckSaveCapturePhases] " + target + " zones, cold descriptor metadata: "
                            + cold.totalMs.ToString("F2") + " ms; exact bytes verified.");
                        coldWriter = null;
                        yield return null;
                    }
                    for (int sample = -1; sample < 5; sample++)
                    {
                        int arms = paired ? 2 : 1;
                        for (int arm = 0; arm < arms; arm++)
                        {
                            bool useLegacy = paired && ((sample + arm + 2) % 2 == 0);
                            var row = new Row { arm = useLegacy ? "legacy" : "current", zones = target,
                                allocationCounterAvailable = report.allocationCounterAvailable,
                                placedOwners = owners, sample = sample };
                            SaveWriter writer;
                            byte[] bytes = Phased(state, row, useLegacy, legacy, out writer);
                            AssertExactBytes(expected, bytes);
                            Assert.Greater(row.graphBodies, owners - 1);
                            if (sample >= 0) report.rows.Add(row);
                            if (target == 20 && sample == 4 && arm == arms - 1) Census(writer, report.parts);
                            UnityEngine.Debug.Log("[DeckSaveCapturePhases] " + target + " zones, " + row.arm
                                + " sample " + sample + ": " + row.totalMs.ToString("F2") + " ms; exact bytes verified.");
                            // The iterator otherwise retains the previous arm's
                            // byte copy, 64 MB stream buffer and entity queue
                            // across its yield and into the next capture.
                            bytes = null; writer = null;
                            yield return null;
                        }
                    }
                }
            }
            LastReportJson = JsonUtility.ToJson(report, true);
            UnityEngine.Debug.Log("[DeckSaveCapturePhases] " + LastReportJson);
        }

        private static void AssertExactBytes(byte[] expected, byte[] actual)
        {
            Assert.AreEqual(expected.Length, actual.Length, "Full graph byte length changed.");
            // NUnit's generic collection equality boxes each byte; a grown-world
            // payload would create tens of millions of assertion objects. Compare
            // raw bytes and construct an assertion only at the first mismatch.
            for (int i = 0; i < expected.Length; i++)
                if (expected[i] != actual[i])
                    Assert.Fail("Full graph byte mismatch at " + i + ": expected " + expected[i] + ", actual " + actual[i]);
        }

        private static bool ClearWriterMetadata()
        {
            var first = typeof(SaveGraphSerializer).GetField("PublicWriterCache", Hidden);
            if (first == null) return false; // baseline phase probe before implementation
            object gate = typeof(SaveGraphSerializer).GetField("FieldCacheGate", Hidden).GetValue(null);
            lock (gate)
                foreach (string name in new[] { "PublicWriterCache", "EffectWriterCache", "GoalWriterCache", "FieldWriterCache", "EncodedTypeNameCache" })
                    ((IDictionary)typeof(SaveGraphSerializer).GetField(name, Hidden).GetValue(null)).Clear();
            // Clear descriptor/encoding caches only. Existing reflection metadata,
            // runtime JIT and content stay warm; this is not a process-cold claim.
            return true;
        }

        private static bool CheckAllocationCounter()
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            var probe = new byte[128 * 1024];
            long delta = GC.GetAllocatedBytesForCurrentThread() - before;
            GC.KeepAlive(probe);
            return delta >= probe.Length;
        }

        private static byte[] Ordinary(GameSessionState state, bool legacy, PropertyInfo mode)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new SaveWriter(stream); if (mode != null) mode.SetValue(writer, legacy);
                state.Save(writer); return stream.ToArray();
            }
        }

        private static byte[] Phased(GameSessionState state, Row row, bool legacy, PropertyInfo mode, out SaveWriter writer)
        {
            using (var stream = new MemoryStream())
            {
                writer = new SaveWriter(stream); if (mode != null) mode.SetValue(writer, legacy);
                long allocated = GC.GetAllocatedBytesForCurrentThread();
                var total = Stopwatch.StartNew(); var phase = Stopwatch.StartNew();
                state.World = LocalPeople.BindForSave(state.ZoneManager, state.World);
                state.World = LairStacks.BindForSave(state.ZoneManager, state.World);
                state.World = SpreadRareEncounterPlan.BindForSave(state.ZoneManager, state.World);
                state.World = SpreadWayhousePlan.BindForSave(state.ZoneManager, state.World);
                state.World = SpreadExplorationPlan.BindForSave(state.ZoneManager, state.World);
                row.bindingMs = phase.Elapsed.TotalMilliseconds; phase.Restart();
                writer.WriteHeader(state.GameVersion); writer.WriteCheck("GameSession.Begin");
                writer.Write(state.SaveVersion); writer.WriteString(state.GameID); writer.WriteString(state.GameVersion);
                writer.Write(state.WorldSeed); writer.WriteString(state.ActiveZoneID); writer.Write(state.SelectedHotbarSlot);
                writer.WriteCheck("Player"); writer.WriteEntityReference(state.Player);
                writer.WriteCheck("World"); writer.WriteEntityReference(state.World);
                writer.WriteCheck("ZoneManager"); SaveGraphSerializer.SaveOverworldZoneManager(state.ZoneManager, writer);
                row.zoneManagerMs = phase.Elapsed.TotalMilliseconds; phase.Restart();
                writer.WriteCheck("TurnManager"); SaveGraphSerializer.SaveTurnManager(state.TurnManager, writer);
                writer.WriteCheck("MessageLog"); SaveGraphSerializer.SaveMessageLog(writer);
                writer.WriteCheck("PlayerReputation"); SaveGraphSerializer.SavePlayerReputation(writer);
                row.globalsMs = phase.Elapsed.TotalMilliseconds; phase.Restart();
                writer.WriteQueuedEntityBodies();
                row.entityBodiesMs = phase.Elapsed.TotalMilliseconds; phase.Restart();
                TileWrite.Invoke(null, new object[] { state.ZoneManager, writer });
                writer.WriteCheck("GameSession.End");
                row.tileStateMs = phase.Elapsed.TotalMilliseconds; phase.Restart();
                string metadata = JsonUtility.ToJson(state.CreateInfo(), true);
                row.metadataMs = phase.Elapsed.TotalMilliseconds;
                row.totalMs = total.Elapsed.TotalMilliseconds;
                row.allocatedBytes = row.allocationCounterAvailable ? GC.GetAllocatedBytesForCurrentThread() - allocated : -1;
                row.bytes = checked((int)stream.Length);
                row.graphBodies = ((IList)EntityQueue.GetValue(writer)).Count;
                Assert.IsNotEmpty(metadata);
                return stream.ToArray(); // oracle copy is outside measured time/allocations
            }
        }

        private static void Census(SaveWriter writer, List<PartRow> rows)
        {
            var counts = new Dictionary<Type, int>();
            foreach (Entity owner in (IList)EntityQueue.GetValue(writer))
                foreach (var part in owner.Parts)
                {
                    Type type = part.GetType(); counts.TryGetValue(type, out int count); counts[type] = count + 1;
                }
            foreach (var entry in counts)
            {
                var fields = (FieldInfo[])PublicFields.Invoke(null, new object[] { entry.Key, null });
                int scalar = 0;
                foreach (var field in fields)
                    if (field.FieldType.IsPrimitive || field.FieldType.IsEnum || field.FieldType == typeof(string)) scalar++;
                rows.Add(new PartRow { type = entry.Key.FullName, owners = entry.Value,
                    fieldsPerOwner = fields.Length, scalarFieldsPerOwner = scalar });
            }
            rows.Sort((a, b) => b.owners.CompareTo(a.owners));
        }
    }
}
