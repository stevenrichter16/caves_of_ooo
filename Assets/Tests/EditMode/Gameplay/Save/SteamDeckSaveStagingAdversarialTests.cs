using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CavesOfOoo.Tests
{
    public sealed class SaveStagingThreadPart : Part
    {
        public static int BeforeThread, AfterThread;
        public override string Name => "SaveStagingThread";
        public int Value;
        public override void OnBeforeSave(SaveWriter writer) { BeforeThread = Thread.CurrentThread.ManagedThreadId; }
        public override void OnAfterSave(SaveWriter writer) { AfterThread = Thread.CurrentThread.ManagedThreadId; }
    }

    [TestFixture]
    public sealed class SteamDeckSaveStagingAdversarialTests
    {
        [TestCase(false)] [TestCase(true)]
        public void SerializationFailureNeverRestoresStaleBackupOverCurrentSave(bool text)
        {
            string root = Path.Combine(Path.GetTempPath(), "coo-save-atomic-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            string path = Path.Combine(root, "Quick.sav.gz");
            try
            {
                File.WriteAllBytes(path, new byte[] { 31, 139, 3, 4 }); File.WriteAllBytes(path + ".bak", new byte[] { 31, 139, 1, 2 });
                byte[] current = File.ReadAllBytes(path), backup = File.ReadAllBytes(path + ".bak");
                if (text)
                {
                    Directory.CreateDirectory(path + ".tmp");
                    var method = typeof(SaveGameService).GetMethod("WriteTextAtomically", NewGameSaveFixture.Static);
                    Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[] { path, "new metadata" }));
                }
                else
                {
                    var method = typeof(SaveGameService).GetMethod("WriteSaveAtomically", NewGameSaveFixture.Static);
                    Action<Stream> fail = stream => { stream.WriteByte(31); throw new IOException("injected disk full"); };
                    Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[] { path, fail }));
                }
                CollectionAssert.AreEqual(current, File.ReadAllBytes(path)); CollectionAssert.AreEqual(backup, File.ReadAllBytes(path + ".bak"));
            }
            finally { Directory.Delete(root, true); }
        }
        [TestCase(false)] [TestCase(true)]
        public void SuccessfulAtomicReplacementRetainsPreviousDestinationAsBackup(bool existed)
        {
            string root = Path.Combine(Path.GetTempPath(), "coo-save-replace-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
            string path = Path.Combine(root, "Quick.sav.gz"); byte[] old = { 31, 139, 1, 2 };
            try
            {
                if (existed) { File.WriteAllBytes(path, old); File.WriteAllBytes(path + ".bak", new byte[] { 99 }); }
                Action<Stream> write = stream => { using (var gzip = new GZipStream(stream, CompressionMode.Compress, true)) gzip.WriteByte(42); };
                typeof(SaveGameService).GetMethod("WriteSaveAtomically", NewGameSaveFixture.Static).Invoke(null, new object[] { path, write });
                CollectionAssert.AreEqual(new byte[] { 42 }, StagedSaveProbe.Unzip(path)); Assert.IsFalse(File.Exists(path + ".tmp"));
                if (existed) CollectionAssert.AreEqual(old, File.ReadAllBytes(path + ".bak")); else Assert.IsFalse(File.Exists(path + ".bak"));
            }
            finally { Directory.Delete(root, true); }
        }
        [Test]
        public void IncompleteGzipTempDoesNotChangeCurrentOrBackup()
        {
            string root = Path.Combine(Path.GetTempPath(), "coo-save-invalid-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root); string path = Path.Combine(root, "Quick.sav.gz");
            try
            {
                File.WriteAllBytes(path, new byte[] { 31, 139, 7 }); File.WriteAllBytes(path + ".bak", new byte[] { 31, 139, 6 });
                Action<Stream> invalid = stream => stream.WriteByte(0);
                Assert.Throws<TargetInvocationException>(() => typeof(SaveGameService).GetMethod("WriteSaveAtomically", NewGameSaveFixture.Static).Invoke(null, new object[] { path, invalid }));
                CollectionAssert.AreEqual(new byte[] { 31, 139, 7 }, File.ReadAllBytes(path)); CollectionAssert.AreEqual(new byte[] { 31, 139, 6 }, File.ReadAllBytes(path + ".bak"));
                Assert.IsFalse(File.Exists(path + ".tmp"));
            }
            finally { Directory.Delete(root, true); }
        }
        [TestCase("register")] [TestCase("identity")] [TestCase("new-game")] [TestCase("root")]
        public void SessionBoundaryDrainsAcceptedOldCheckpoint(string boundary)
        {
            using (var f = new NewGameSaveFixture())
            {
                try
                {
                    Assert.IsTrue(StagedSaveProbe.Request());
                    if (boundary == "register") SaveGameService.RegisterRuntime(() => f.Old, _ => { }, f.OldID);
                    if (boundary == "identity") SaveGameService.SetActiveGameID(f.OldID);
                    if (boundary == "new-game") { f.Fresh.WorldSeed = 333; Assert.IsTrue(SaveGameService.BeginNewGame()); }
                    if (boundary == "root") SaveGameService.SaveRootOverride = Path.Combine(f.Root, "unused");
                    Assert.IsTrue(File.Exists(Path.Combine(f.Root, f.NewID, "Quick.sav.gz")));
                    Assert.IsTrue(StagedSaveProbe.Flush());
                    if (boundary != "new-game") Assert.AreEqual(222, SaveGameService.LoadState(Path.Combine(f.Root, f.NewID, "Quick.sav.gz"), null).WorldSeed);
                    else Assert.AreEqual(333, SaveGameService.LoadState(Path.Combine(f.Root, f.NewID, "Quick.sav.gz"), null).WorldSeed);
                }
                finally { StagedSaveProbe.FlushIfPresent(); }
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void MissingRuntimeOrNullCaptureRejectsWithoutPendingWrite(bool absent)
        {
            using (var f = new NewGameSaveFixture())
            {
                try { SaveGameService.RegisterRuntime(absent ? null : (Func<GameSessionState>)(() => null), null); Assert.IsFalse(StagedSaveProbe.Request()); Assert.IsTrue(StagedSaveProbe.Flush()); Assert.IsFalse(Directory.Exists(Path.Combine(f.Root, f.NewID))); f.OldUnchanged(); }
                finally { StagedSaveProbe.FlushIfPresent(); }
            }
        }
        [Test]
        public void CaptureExceptionIsContainedAndDoesNotOverwriteLastCheckpoint()
        {
            using (var f = new NewGameSaveFixture())
            {
                try
                {
                    StagedSaveProbe.Method("RequestQuickSave");
                    SaveGameService.RegisterRuntime(() => throw new InvalidOperationException("capture failed"), null);
                    LogAssert.Expect(LogType.Error, new Regex("\\[Save\\].*failed")); Assert.IsFalse(StagedSaveProbe.Request()); Assert.IsTrue(StagedSaveProbe.Flush()); f.OldUnchanged();
                }
                finally { StagedSaveProbe.FlushIfPresent(); }
            }
        }
        [Test]
        public void DiskFailureIsDeliveredOnFlushAndRecoveryCanSaveAgain()
        {
            using (var f = new NewGameSaveFixture())
            {
                try
                {
                    StagedSaveProbe.Method("RequestQuickSave"); File.WriteAllText(Path.Combine(f.Root, f.NewID), "blocks directory creation");
                    Assert.IsTrue(StagedSaveProbe.Request()); LogAssert.Expect(LogType.Error, new Regex("\\[Save\\].*failed")); Assert.IsFalse(StagedSaveProbe.Flush());
                    File.Delete(Path.Combine(f.Root, f.NewID)); Assert.IsTrue(StagedSaveProbe.Request()); Assert.IsTrue(StagedSaveProbe.Flush()); Assert.IsTrue(SaveGameService.QuickLoad()); Assert.AreEqual(222, f.Loaded.WorldSeed); f.OldUnchanged();
                }
                finally { StagedSaveProbe.FlushIfPresent(); }
            }
        }
        [Test]
        public void SaveHooksRunOnCaptureThreadAndAreNotRepeatedByWorker()
        {
            using (var f = new NewGameSaveFixture())
            {
                try
                {
                    SaveStagingThreadPart.BeforeThread = 0; SaveStagingThreadPart.AfterThread = 0; f.Fresh.Player.AddPart(new SaveStagingThreadPart { Value = 7 });
                    int caller = Thread.CurrentThread.ManagedThreadId; Assert.IsTrue(StagedSaveProbe.Request());
                    Assert.AreEqual(caller, SaveStagingThreadPart.BeforeThread); Assert.AreEqual(caller, SaveStagingThreadPart.AfterThread);
                    SaveStagingThreadPart.BeforeThread = -1; SaveStagingThreadPart.AfterThread = -1; Assert.IsTrue(StagedSaveProbe.Flush());
                    Assert.AreEqual(-1, SaveStagingThreadPart.BeforeThread); Assert.AreEqual(-1, SaveStagingThreadPart.AfterThread);
                    Assert.IsTrue(SaveGameService.QuickLoad()); Assert.AreEqual(7, f.Loaded.Player.GetPart<SaveStagingThreadPart>().Value);
                }
                finally { StagedSaveProbe.FlushIfPresent(); SaveStagingThreadPart.BeforeThread = 0; SaveStagingThreadPart.AfterThread = 0; }
            }
        }
        [TestCase(1)] [TestCase(2)] [TestCase(7)] [TestCase(100)]
        public void BoundedQueueAlwaysFinishesWithNewestAcceptedRequest(int count)
        {
            using (var q = new SaveQueueProbe())
            {
                q.Enqueue(1); Assert.IsTrue(q.Entered.Wait(5000)); for (int i = 2; i <= count; i++) q.Enqueue(i);
                q.Release.Set(); Assert.IsTrue(q.Flush()); Assert.AreEqual(count, q.Written[q.Written.Count - 1]); Assert.LessOrEqual(q.Written.Count, 2);
                Assert.AreEqual(q.Written.Count, q.Completed.Count); q.Pump(); Assert.AreEqual(q.Written.Count, q.Completed.Count);
            }
        }
        [Test]
        public void IdleQueueHasNoWorkAndFlushDoesNotInvokeCompletion()
        { using (var q = new SaveQueueProbe()) { Assert.IsFalse(q.HasPending); Assert.IsTrue(q.Flush()); q.Pump(); CollectionAssert.IsEmpty(q.Completed); } }
        [Test]
        public void ReflectionFilterNeverPoisonsUnfilteredCache()
        {
            var method = typeof(SaveGraphSerializer).GetMethod("GetSerializablePublicFields", BindingFlags.Static | BindingFlags.NonPublic);
            var first = (FieldInfo[])method.Invoke(null, new object[] { typeof(RenderPart), null });
            var filtered = (FieldInfo[])method.Invoke(null, new object[] { typeof(RenderPart), (Func<FieldInfo, bool>)(_ => false) });
            var again = (FieldInfo[])method.Invoke(null, new object[] { typeof(RenderPart), null });
            Assert.Greater(first.Length, 0); CollectionAssert.IsEmpty(filtered); CollectionAssert.AreEqual(first, again); Assert.AreSame(first, again);
        }
        [Test]
        public void UnchangedZoneMembershipDoesNotHideInactivePartAndTileMutations()
        {
            using (var f = new NewGameSaveFixture())
            {
                try
                {
                    var active = new Zone("DeckActive"); var inactive = new Zone("DeckInactive");
                    active.AddEntity(f.Fresh.Player, 1, 1);
                    var owner = new Entity { ID = "inactive-owner", BlueprintName = "Probe" };
                    owner.AddPart(new SaveStagingThreadPart { Value = 7 }); inactive.AddEntity(owner, 2, 2);
                    var manager = OverworldZoneManager.CreateDetached(null, 64);
                    manager.ReplaceLoadedState(new System.Collections.Generic.Dictionary<string, Zone>
                        { { active.ZoneID, active }, { inactive.ZoneID, inactive } }, active.ZoneID,
                        new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<ZoneConnection>>());
                    f.Fresh.ZoneManager = manager; f.Fresh.ActiveZoneID = active.ZoneID;
                    Assert.IsTrue(StagedSaveProbe.Request()); Assert.IsTrue(StagedSaveProbe.Flush());
                    int membershipVersion = inactive.EntityVersion;
                    owner.GetPart<SaveStagingThreadPart>().Value = 91;
                    inactive.TileState.WriteCoating(5, 6, "water", ZoneTileState.Permanent);
                    Assert.AreEqual(membershipVersion, inactive.EntityVersion);
                    Assert.IsTrue(StagedSaveProbe.Request()); Assert.IsTrue(StagedSaveProbe.Flush());
                    Assert.IsTrue(SaveGameService.QuickLoad());
                    var restored = f.Loaded.ZoneManager.CachedZones[inactive.ZoneID];
                    Assert.AreEqual(91, restored.GetCell(2, 2).Objects[0].GetPart<SaveStagingThreadPart>().Value);
                    Assert.AreEqual(ZoneTileState.Permanent, restored.TileState.CoatingTurns(5, 6, "water"));
                }
                finally { StagedSaveProbe.FlushIfPresent(); }
            }
        }
        [Test]
        public void ChangingPredicateClosureIsEvaluatedAgain()
        {
            var method = typeof(SaveGraphSerializer).GetMethod("GetSerializablePublicFields", BindingFlags.Static | BindingFlags.NonPublic);
            bool include = false; Func<FieldInfo, bool> filter = _ => include;
            var empty = (FieldInfo[])method.Invoke(null, new object[] { typeof(RenderPart), filter });
            include = true; var full = (FieldInfo[])method.Invoke(null, new object[] { typeof(RenderPart), filter });
            CollectionAssert.IsEmpty(empty); Assert.Greater(full.Length, 0);
        }
        [Test]
        public void StageMeasurementsPublishOnlyAfterCompletedWrite()
        {
            using (var f = new NewGameSaveFixture())
            {
                try
                {
                    var type = typeof(SaveGameService); var property = type.GetProperty("LastCompletedSavePerformance");
                    Assert.NotNull(property); var previous = property.GetValue(null);
                    Assert.IsTrue(StagedSaveProbe.Request()); Assert.AreSame(previous, property.GetValue(null));
                    Assert.IsTrue(StagedSaveProbe.Flush()); var sample = property.GetValue(null); Assert.AreNotSame(previous, sample);
                    Assert.IsTrue((bool)sample.GetType().GetProperty("Succeeded").GetValue(sample));
                    Assert.Greater((int)sample.GetType().GetProperty("UncompressedBytes").GetValue(sample), 0);
                    foreach (string field in new[] { "CaptureMilliseconds", "CompressionMilliseconds", "CommitMilliseconds" })
                        Assert.GreaterOrEqual((double)sample.GetType().GetProperty(field).GetValue(sample), 0);
                }
                finally { StagedSaveProbe.FlushIfPresent(); }
            }
        }
    }
}
