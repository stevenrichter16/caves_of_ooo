using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CavesOfOoo.Core
{
    /// <summary>One active write plus the newest pending autosave. Enqueue and
    /// completion pumping belong to the main thread; only the supplied commit
    /// action runs on a worker. Items must own detached immutable save data.</summary>
    internal sealed class BackgroundSaveQueue<T>
    {
        private readonly object _gate = new object();
        private readonly Action<T> _commit;
        private readonly Queue<Completion> _completed = new Queue<Completion>();
        private T _pending;
        private bool _hasPending, _running;
        private Task _worker;

        private readonly struct Completion
        {
            internal readonly T Item;
            internal readonly Exception Error;
            internal Completion(T item, Exception error) { Item = item; Error = error; }
        }

        public BackgroundSaveQueue(Action<T> commit)
        { _commit = commit ?? throw new ArgumentNullException(nameof(commit)); }

        public bool HasPending
        { get { lock (_gate) return _running || _hasPending || _completed.Count != 0; } }

        public void Enqueue(T item)
        {
            lock (_gate)
            {
                _pending = item;
                _hasPending = true;
                if (_running) return;
                _running = true;
                _worker = Task.Run(Run);
            }
        }

        private void Run()
        {
            while (true)
            {
                T item;
                lock (_gate)
                {
                    if (!_hasPending) { _running = false; return; }
                    item = _pending;
                    _pending = default;
                    _hasPending = false;
                }
                Exception error = null;
                try { _commit(item); }
                catch (Exception ex) { error = ex; }
                lock (_gate) _completed.Enqueue(new Completion(item, error));
            }
        }

        /// <summary>Deliver already completed writes on the calling thread.
        /// Returns false if this delivery included a failed write.</summary>
        public bool Pump(Action<T, Exception> completed)
        {
            bool success = true;
            while (true)
            {
                Completion result;
                lock (_gate)
                {
                    if (_completed.Count == 0) return success;
                    result = _completed.Dequeue();
                }
                if (result.Error != null) success = false;
                completed?.Invoke(result.Item, result.Error);
            }
        }

        /// <summary>Drain accepted work before replacing runtime state or quitting.
        /// A failure is reported once, when its completion is delivered.</summary>
        public bool Flush(Action<T, Exception> completed)
        {
            bool success = true;
            while (true)
            {
                Task worker;
                lock (_gate) worker = _worker;
                worker?.GetAwaiter().GetResult();
                success &= Pump(completed);
                lock (_gate)
                    if (!_running && !_hasPending && _completed.Count == 0) return success;
            }
        }
    }

    /// <summary>Elapsed wall time for one completed save. Capture includes live
    /// serialization and metadata on the main thread. Compression is in memory;
    /// commit includes directory, atomic payload/metadata writes and backups.
    /// These measurements describe a sample, not a hardware frame budget.</summary>
    public sealed class SavePerformanceSample
    {
        public double CaptureMilliseconds { get; }
        public double CompressionMilliseconds { get; }
        public double CommitMilliseconds { get; }
        public int UncompressedBytes { get; }
        public bool Succeeded { get; }
        internal SavePerformanceSample(double capture, double compression, double commit, int bytes, bool succeeded)
        { CaptureMilliseconds = capture; CompressionMilliseconds = compression; CommitMilliseconds = commit; UncompressedBytes = bytes; Succeeded = succeeded; }
    }
}
