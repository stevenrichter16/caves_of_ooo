using System;
using System.IO;
using System.IO.Compression;

namespace CavesOfOoo.Core
{
    public static partial class SaveGameService
    {
        private static IBrowserSaveStore _browserStore;
        private static BrowserSaveQueue<CapturedSave> _browserSaves;
        private static bool _browserManualPending;
        public static bool UsesBrowserStorage => _browserStore != null;
        public static bool IsStorageReady => _browserStore == null || _browserStore.IsReady;
        public static bool IsStorageBusy => !IsStorageReady || HasPendingSave
            || (UsesBrowserStorage && (DeferredStableSlots.Count > 0 || _deferredStableQuick));
        public static string LastBrowserSaveError { get; private set; }
        public static event Action<bool, string> BrowserSaveCompleted;

        /// <summary>Install after hydration, before gameplay bootstrap. The root
        /// is dedicated volatile storage, never Unity's automatically synced IDBFS.</summary>
        public static void ConfigureBrowserStore(IBrowserSaveStore store, string hydratedRoot)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            if (!store.IsReady) throw new InvalidOperationException("Browser storage has not hydrated.");
            if (string.IsNullOrEmpty(hydratedRoot)) throw new ArgumentException("A hydrated root is required.", nameof(hydratedRoot));
            RequireStorageIdle();
            FlushPendingSaves();
            _browserStore = store;
            _browserSaves = new BrowserSaveQueue<CapturedSave>(CommitBrowserSave);
            _saveRootOverride = hydratedRoot;
            LastBrowserSaveError = null;
        }

        private static void RequireStorageIdle()
        {
            if (UsesBrowserStorage && IsStorageBusy)
                throw new InvalidOperationException("Browser checkpoint is still saving. Try again when storage is ready.");
        }

        private static void CommitBrowserSave(CapturedSave save, Action<Exception> completed)
        {
            if (!_browserStore.IsReady) throw new InvalidOperationException("Browser storage is unavailable. Reload to recover.");
            byte[] payload;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            using (var stream = new MemoryStream())
            {
                using (var gzip = new GZipStream(stream, CompressionMode.Compress, leaveOpen: true))
                    gzip.Write(save.Buffer, 0, save.Length);
                payload = stream.ToArray();
            }
            save.CompressionMilliseconds = timer.Elapsed.TotalMilliseconds;
            timer.Restart();
            _browserStore.Commit(save.GameID, save.Name, payload, save.Metadata, error =>
            {
                save.CommitMilliseconds = timer.Elapsed.TotalMilliseconds;
                completed(error);
            });
        }

        private static void PublishBrowserSave(CapturedSave save, Exception error)
        {
            _browserManualPending = false;
            PublishSavePerformance(save, error == null);
            LastBrowserSaveError = error?.Message;
            if (error == null)
            {
                RememberSavedGame(save.GameID);
                MessageLog.Add("Game saved.");
            }
            else
            {
                UnityEngine.Debug.LogError("[Save] Browser checkpoint failed: " + error);
                MessageLog.Add("Save failed: " + error.Message);
            }
            BrowserSaveCompleted?.Invoke(error == null, error?.Message);
        }
    }
}
