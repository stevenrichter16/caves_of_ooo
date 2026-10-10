using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CavesOfOoo.Tests
{
    internal sealed class SaveQueueProbe : IDisposable
    {
        private readonly object _queue;
        private readonly Type _type;
        internal readonly ManualResetEventSlim Entered = new ManualResetEventSlim();
        internal readonly ManualResetEventSlim Release = new ManualResetEventSlim();
        internal readonly List<int> Written = new List<int>();
        internal readonly List<int> Completed = new List<int>();
        internal readonly List<Exception> Errors = new List<Exception>();
        internal int WorkerThread, CompletionThread, FailValue = -1;
        internal SaveQueueProbe()
        {
            var open = typeof(SaveGameService).Assembly.GetType("CavesOfOoo.Core.BackgroundSaveQueue`1");
            Assert.NotNull(open, "Save work must use a bounded serial background queue.");
            _type = open.MakeGenericType(typeof(int));
            _queue = Activator.CreateInstance(_type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new object[] { (Action<int>)(value =>
                {
                    WorkerThread = Thread.CurrentThread.ManagedThreadId;
                    if (value == 1) { Entered.Set(); if (!Release.Wait(5000)) throw new TimeoutException("Test worker gate timed out."); }
                    lock (Written) Written.Add(value);
                    if (value == FailValue) throw new IOException("injected disk full");
                }) }, null);
        }
        internal void Enqueue(int value) => _type.GetMethod("Enqueue").Invoke(_queue, new object[] { value });
        internal void Pump() => _type.GetMethod("Pump").Invoke(_queue, new object[] { Callback });
        internal bool Flush() => (bool)_type.GetMethod("Flush").Invoke(_queue, new object[] { Callback });
        private Action<int, Exception> Callback => (value, error) =>
        { CompletionThread = Thread.CurrentThread.ManagedThreadId; Completed.Add(value); Errors.Add(error); };
        internal bool HasPending => (bool)_type.GetProperty("HasPending").GetValue(_queue);
        public void Dispose() { Release.Set(); if (_queue != null) Flush(); Entered.Dispose(); Release.Dispose(); }
    }

    internal static class StagedSaveProbe
    {
        internal static MethodInfo Method(string name)
        { var method = typeof(SaveGameService).GetMethod(name, NewGameSaveFixture.Static); Assert.NotNull(method, name + " lifecycle API is required."); return method; }
        internal static bool Request() => (bool)Method("RequestQuickSave").Invoke(null, null);
        internal static bool Flush() => (bool)Method("FlushPendingSaves").Invoke(null, null);
        internal static void FlushIfPresent() => typeof(SaveGameService).GetMethod("FlushPendingSaves", NewGameSaveFixture.Static)?.Invoke(null, null);
        internal static byte[] Bytes(GameSessionState state)
        { using (var stream = new MemoryStream()) { state.Save(new SaveWriter(stream)); return stream.ToArray(); } }
        internal static byte[] Unzip(string path)
        { using (var file = File.OpenRead(path)) using (var gzip = new GZipStream(file, CompressionMode.Decompress)) using (var data = new MemoryStream()) { gzip.CopyTo(data); return data.ToArray(); } }
    }

    [TestFixture]
    public sealed class SteamDeckSaveStagingTests
    {
        [Test]
        public void WorkerIsBackgroundAndCompletionRunsOnlyWhenPumped()
        {
            using (var q = new SaveQueueProbe())
            {
                int caller = Thread.CurrentThread.ManagedThreadId;
                q.Enqueue(1); Assert.IsTrue(q.Entered.Wait(5000));
                Assert.AreNotEqual(caller, q.WorkerThread); Assert.IsTrue(q.HasPending);
                q.Pump(); CollectionAssert.IsEmpty(q.Completed);
                q.Release.Set(); Assert.IsTrue(q.Flush());
                CollectionAssert.AreEqual(new[] { 1 }, q.Completed); Assert.AreEqual(caller, q.CompletionThread); Assert.IsFalse(q.HasPending);
            }
        }
        [Test]
        public void RapidTravelKeepsActiveAndNewestPendingCaptureOnly()
        {
            using (var q = new SaveQueueProbe())
            {
                q.Enqueue(1); Assert.IsTrue(q.Entered.Wait(5000)); q.Enqueue(2); q.Enqueue(3); q.Enqueue(4);
                q.Release.Set(); Assert.IsTrue(q.Flush());
                CollectionAssert.AreEqual(new[] { 1, 4 }, q.Written); CollectionAssert.AreEqual(q.Written, q.Completed);
            }
        }
        [Test]
        public void QueueFailureIsReportedAndLaterCheckpointStillCommits()
        {
            using (var q = new SaveQueueProbe())
            {
                q.FailValue = 1; q.Enqueue(1); Assert.IsTrue(q.Entered.Wait(5000)); q.Enqueue(2);
                q.Release.Set(); Assert.IsFalse(q.Flush());
                CollectionAssert.AreEqual(new[] { 1, 2 }, q.Written); Assert.IsInstanceOf<IOException>(q.Errors[0]); Assert.IsNull(q.Errors[1]);
                Assert.IsTrue(q.Flush(), "A delivered failure must not poison future saves.");
            }
        }
        [Test]
        public void CapturedBytesMatchLegacyFormatAndIgnoreSubsequentLiveMutations()
        {
            using (var f = new NewGameSaveFixture())
            {
                try
                {
                    var expected = StagedSaveProbe.Bytes(f.Fresh); Assert.IsTrue(StagedSaveProbe.Request());
                    f.Fresh.WorldSeed = 999; f.Fresh.Player.BlueprintName = "MutatedAfterCapture";
                    Assert.IsTrue(StagedSaveProbe.Flush());
                    string path = Path.Combine(f.Root, f.NewID, "Quick.sav.gz"); CollectionAssert.AreEqual(expected, StagedSaveProbe.Unzip(path));
                    Assert.IsTrue(SaveGameService.QuickLoad()); Assert.AreEqual(222, f.Loaded.WorldSeed); Assert.AreEqual("SaveProbe", f.Loaded.Player.BlueprintName);
                    Assert.AreEqual(f.NewID, PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey)); f.OldUnchanged();
                }
                finally { StagedSaveProbe.FlushIfPresent(); }
            }
        }
        [Test]
        public void ImmediateLoadDrainsPendingAutosave()
        {
            using (var f = new NewGameSaveFixture())
            {
                try { Assert.IsTrue(StagedSaveProbe.Request()); f.Fresh.WorldSeed = 333; Assert.IsTrue(SaveGameService.QuickLoad()); Assert.AreEqual(222, f.Loaded.WorldSeed); }
                finally { StagedSaveProbe.FlushIfPresent(); }
            }
        }
        [Test]
        public void ManualSaveAfterPendingAutosaveWins()
        {
            using (var f = new NewGameSaveFixture())
            {
                try { Assert.IsTrue(StagedSaveProbe.Request()); f.Fresh.WorldSeed = 333; Assert.IsTrue(SaveGameService.QuickSave()); Assert.IsTrue(StagedSaveProbe.Flush()); Assert.IsTrue(SaveGameService.QuickLoad()); Assert.AreEqual(333, f.Loaded.WorldSeed); }
                finally { StagedSaveProbe.FlushIfPresent(); }
            }
        }
        [Test]
        public void ChangingSaveRootDrainsOldPathBeforeReturning()
        {
            using (var f = new NewGameSaveFixture())
            {
                string next = Path.Combine(f.Root, "next-root");
                try
                {
                    Assert.IsTrue(StagedSaveProbe.Request()); SaveGameService.SaveRootOverride = next;
                    Assert.IsTrue(File.Exists(Path.Combine(f.Root, f.NewID, "Quick.sav.gz"))); Assert.IsFalse(Directory.Exists(next));
                    Assert.IsTrue(StagedSaveProbe.Flush()); Assert.IsFalse(Directory.Exists(next));
                }
                finally { StagedSaveProbe.FlushIfPresent(); }
            }
        }
        [Test]
        public void SerializableFieldMetadataIsReusedWithoutFilterContamination()
        {
            var method = typeof(SaveGraphSerializer).GetMethod("GetSerializablePublicFields", BindingFlags.Static | BindingFlags.NonPublic);
            var args = new object[] { typeof(RenderPart), null };
            var first = (FieldInfo[])method.Invoke(null, args); var second = (FieldInfo[])method.Invoke(null, args);
            Assert.AreSame(first, second, "Repeated entities must not repeat field discovery/allocation.");
            Assert.That(Array.Exists(first, field => field.Name == "ParentEntity"), Is.False);
        }
    }
}
