using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class BrowserSaveServiceTests
    {
        private readonly Dictionary<FieldInfo, object> _original = new Dictionary<FieldInfo, object>();
        private string _root;
        private FakeStore _store;
        private GameSessionState _state;
        private int _captures, _loads, _flash, _serial;
        private bool _hadPref;
        private string _pref;
        private List<MessageLog.Entry> _log;
        private List<string> _announcements;
        private Dictionary<string, int> _reputation;

        private sealed class FakeStore : IBrowserSaveStore
        {
            public bool IsReady { get; set; } = true;
            public readonly List<byte[]> Payloads = new List<byte[]>();
            public Action<Exception> Complete;
            public string Root;
            public void Commit(string gameID, string slot, byte[] compressed, string metadata, Action<Exception> completed)
            {
                Payloads.Add(compressed);
                Complete = error => {
                    if (error == null) {
                        string dir = Path.Combine(Root, gameID); Directory.CreateDirectory(dir);
                        File.WriteAllBytes(Path.Combine(dir, slot + ".sav.gz"), compressed);
                        File.WriteAllText(Path.Combine(dir, slot + ".json"), metadata);
                    }
                    completed(error);
                };
            }
        }

        [SetUp]
        public void SetUp()
        {
            SaveGameService.FlushPendingSaves();
            _hadPref = PlayerPrefs.HasKey(SaveGameService.LastGameIDPrefsKey);
            _pref = PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey);
            _log = MessageLog.GetAllEntries(); _announcements = MessageLog.GetPendingAnnouncementsSnapshot();
            _flash = MessageLog.FlashStamp; _serial = MessageLog.NextSerialValue;
            _reputation = new Dictionary<string, int>(PlayerReputation.GetAll());
            foreach (var field in typeof(SaveGameService).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                if (!field.IsLiteral && !field.IsInitOnly) _original[field] = field.GetValue(null);
            _root = Path.Combine(Path.GetTempPath(), "coo-browser-test-" + Guid.NewGuid().ToString("N"));
            _store = new FakeStore { Root = _root };
            _state = new GameSessionState { GameID = "browser-game", GameVersion = "test", WorldSeed = 42,
                Player = new Entity { ID = "browser-player", BlueprintName = "Test" } };
            _captures = _loads = 0;
            SaveGameService.ConfigureBrowserStore(_store, _root);
            SaveGameService.RegisterRuntime(() => { _captures++; return _state; }, _ => _loads++, _state.GameID);
            SaveGameService.SetActiveGameID(_state.GameID);
        }

        [TearDown]
        public void TearDown()
        {
            // Restore static service ownership without touching a real save directory.
            foreach (var entry in _original) entry.Key.SetValue(null, entry.Value);
            _original.Clear();
            if (_hadPref) PlayerPrefs.SetString(SaveGameService.LastGameIDPrefsKey, _pref);
            else PlayerPrefs.DeleteKey(SaveGameService.LastGameIDPrefsKey);
            PlayerPrefs.Save(); MessageLog.Restore(_log, _announcements, _flash, _serial);
            PlayerReputation.Restore(_reputation);
            var deferred = (HashSet<string>)typeof(SaveGameService).GetField("DeferredStableSlots", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            deferred.Clear();
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        [Test]
        public void BrowserRootDoesNotSuppressTheOrdinaryStartingBuildPicker()
        {
            Assert.That(NativeAuditBootstrapSettings.SuppressBuildChoiceForAudit(), Is.False);
            Assert.Throws<InvalidOperationException>(() => SaveGameService.SaveRootOverride = _root + "-other");
        }

        [Test]
        public void AutosaveCannotSupersedeAnAcceptedManualPrimaryCheckpoint()
        {
            Assert.That(SaveGameService.SavePrimary(), Is.True);
            _state.WorldSeed = 99;
            Assert.That(SaveGameService.RequestQuickSave(), Is.True);
            SaveGameService.PumpPendingSaves();
            _store.Complete(null); SaveGameService.PumpPendingSaves(); SaveGameService.PumpPendingSaves();
            _store.Complete(null); SaveGameService.PumpPendingSaves();
            Assert.That(SaveGameService.LoadState(Path.Combine(_root, _state.GameID, "Primary.sav.gz"), null).WorldSeed, Is.EqualTo(42));
            Assert.That(SaveGameService.LoadState(Path.Combine(_root, _state.GameID, "Quick.sav.gz"), null).WorldSeed, Is.EqualTo(99));
        }

        [Test]
        public void EveryAcceptedUnstableSlotWaitsForSettlementAndPersistsSerially()
        {
            using (ActionResolutionGuard.Enter())
            {
                Assert.That(SaveGameService.SavePrimary(), Is.True);
                Assert.That(SaveGameService.QuickSave(), Is.True);
                Assert.That(_captures, Is.Zero);
            }
            Assert.That(SaveGameService.IsStorageBusy, Is.True);
            Assert.That(SaveGameService.BeginNewGame(), Is.False);
            Assert.That(SaveGameService.FlushStableRequests(), Is.True);
            SaveGameService.PumpPendingSaves();
            _store.Complete(null); SaveGameService.PumpPendingSaves(); SaveGameService.PumpPendingSaves();
            _store.Complete(null); SaveGameService.PumpPendingSaves();
            Assert.That(SaveGameService.HasPrimarySave(), Is.True);
            Assert.That(SaveGameService.HasQuickSave(), Is.True);
            Assert.That(SaveGameService.IsStorageBusy, Is.False);
        }

        [Test]
        public void FirstSaveIsPendingAndCannotLoadOrRebindUntilAcknowledged()
        {
            Assert.That(SaveGameService.QuickSave(), Is.True);
            Assert.That(SaveGameService.HasPendingSave, Is.True);
            Assert.That(SaveGameService.FlushPendingSaves(), Is.False);
            Assert.That(SaveGameService.HasQuickSave(), Is.False);
            Assert.That(SaveGameService.QuickLoad(), Is.False);
            Assert.That(SaveGameService.BeginNewGame(), Is.False);
            Assert.Throws<InvalidOperationException>(() => SaveGameService.SaveRootOverride = _root + "-changed");
            Assert.Throws<InvalidOperationException>(() => SaveGameService.SetActiveGameID("other"));
            Assert.Throws<InvalidOperationException>(() => SaveGameService.RegisterRuntime(() => _state, _ => {}));
            Assert.That(_loads, Is.Zero);
            _store.Complete(null); SaveGameService.PumpPendingSaves();
            Assert.That(SaveGameService.HasPendingSave, Is.False);
            Assert.That(SaveGameService.HasQuickSave(), Is.True);
            Assert.That(SaveGameService.QuickLoad(), Is.True);
            Assert.That(_loads, Is.EqualTo(1));
        }

        [Test]
        public void HydrationFailureCannotCaptureOrCreateInitialCheckpoint()
        {
            _store.IsReady = false;
            Assert.That(SaveGameService.BeginNewGame(), Is.False);
            Assert.That(SaveGameService.QuickSave(), Is.False);
            Assert.That(SaveGameService.RequestQuickSave(), Is.False);
            Assert.That(SaveGameService.HasQuickSave(), Is.False);
            Assert.That(_captures, Is.Zero);
        }

        [Test]
        public void SnapshotBytesAreDetachedAndPendingAutosavesCoalesce()
        {
            Assert.That(SaveGameService.RequestQuickSave(), Is.True);
            SaveGameService.PumpPendingSaves();
            _state.WorldSeed = 100; Assert.That(SaveGameService.RequestQuickSave(), Is.True);
            _state.WorldSeed = 200; Assert.That(SaveGameService.RequestQuickSave(), Is.True);
            _state.WorldSeed = 300;
            Assert.That(_store.Payloads.Count, Is.EqualTo(1));
            _store.Complete(null); SaveGameService.PumpPendingSaves(); SaveGameService.PumpPendingSaves();
            Assert.That(_store.Payloads.Count, Is.EqualTo(2));
            _store.Complete(null); SaveGameService.PumpPendingSaves();
            var saved = SaveGameService.LoadState(Path.Combine(_root, _state.GameID, "Quick.sav.gz"), null);
            Assert.That(saved.WorldSeed, Is.EqualTo(200));
        }
    }
}
