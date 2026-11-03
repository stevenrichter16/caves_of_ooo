using System;

namespace CavesOfOoo.Core
{
    /// <summary>Single-threaded browser queue. Keeps the active checkpoint and
    /// newest pending capture. Only Pump publishes callbacks into gameplay.
    /// The commit delegate must complete asynchronously or throw; no worker or wait.</summary>
    public sealed class BrowserSaveQueue<T>
    {
        private readonly Action<T, Action<Exception>> _commit;
        private T _active, _pending;
        private bool _hasActive, _hasPending, _completed;
        private Exception _error;
        private long _operation;

        public BrowserSaveQueue(Action<T, Action<Exception>> commit)
        { _commit = commit ?? throw new ArgumentNullException(nameof(commit)); }

        public bool HasPending => _hasActive || _hasPending;

        public void Enqueue(T snapshot) { _pending = snapshot; _hasPending = true; }

        public void Pump(Action<T, Exception> publish)
        {
            if (_hasActive)
            {
                if (!_completed) return;
                T item = _active;
                Exception error = _error;
                _active = default; _hasActive = false; _completed = false; _error = null;
                publish?.Invoke(item, error);
                return; // At most one capture/completion per frame.
            }
            if (!_hasPending) return;
            _active = _pending; _pending = default; _hasPending = false;
            _hasActive = true; _completed = false;
            long operation = ++_operation;
            Action<Exception> done = error =>
            {
                if (!_hasActive || _completed || operation != _operation) return;
                _error = error; _completed = true;
            };
            try { _commit(_active, done); }
            catch (Exception error) { done(error); }
        }
    }

    /// <summary>The browser owns durable storage; canonical local files contain
    /// only acknowledged checkpoints. Implementations publish local files before
    /// invoking completed, and refuse writes after a cross-tab revision conflict.</summary>
    public interface IBrowserSaveStore
    {
        bool IsReady { get; }
        void Commit(string gameID, string slot, byte[] compressed, string metadata,
            Action<Exception> completed);
    }
}
