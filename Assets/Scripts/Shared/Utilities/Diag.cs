using System;
using System.Collections.Generic;
using System.Threading;
using CavesOfOoo.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace CavesOfOoo.Diagnostics
{
    /// <summary>
    /// AI-debugging diagnostic substrate. Captures structured records from
    /// gameplay code paths into an in-memory ring buffer that can be queried
    /// from external Claude sessions via the Unity MCP tool surface.
    ///
    /// Design contract: <c>Docs/AI-OBSERVABILITY.md</c>.
    /// First-ship plan: <c>Docs/D1-SPIKE-PLAN.md</c>.
    ///
    /// The substrate is system-agnostic: combat is the first consumer in
    /// Tier 1 D1.2, but every API field is generic. Adding a new system
    /// (save, quest, worldgen, dialogue, ...) needs zero substrate changes
    /// — just a new category string and <c>Diag.Record</c> calls at the
    /// system's state transitions. See AI-OBSERVABILITY.md §10 for the
    /// extension recipe.
    ///
    /// Threading model: gameplay is single-threaded; substrate assumes all
    /// <c>Record</c> calls come from the Unity main thread. Tool dispatch
    /// (D1.3) returns to the main thread before reading the buffer. If
    /// future evidence shows otherwise, switch <c>_writeIndex</c> /
    /// <c>_filledCount</c> to <c>Volatile</c> reads and add a single lock
    /// around <c>Append</c>/<c>Snapshot</c>.
    ///
    /// Cost when channel is disabled: one dictionary lookup
    /// (<c>IsChannelEnabled</c>) + early return. Hot-path callers should
    /// guard expensive payload construction at the call site (because C#
    /// evaluates arguments before the call, anonymous-object allocation
    /// for the payload cannot be elided by an internal channel check):
    /// <code>
    ///     if (Diag.IsChannelEnabled("effect"))
    ///         Diag.Record("effect", "OnApply", target: ent, payload: ExpensivePayload());
    /// </code>
    /// </summary>
    public static class Diag
    {
        // ====================================================================
        // Public types
        // ====================================================================

        /// <summary>
        /// One captured observable. Stored in the ring buffer; returned by
        /// <see cref="Snapshot"/>; serialized to JSON by the MCP tool layer.
        ///
        /// All string fields except Category/Kind are nullable. Turn is
        /// nullable to support events fired outside the turn loop
        /// (worldgen, save, bootstrap, UI menu).
        /// </summary>
        public struct Entry
        {
            /// <summary>Short hex UUID slice (8 chars) — unique per record.</summary>
            public string TraceId;

            /// <summary>Free-form category. Examples: "event", "effect", "damage", "turn", "save", "quest".</summary>
            public string Category;

            /// <summary>Free-form kind within a category. Examples: "OnApply", "EndTurn", "WriteEntity".</summary>
            public string Kind;

            /// <summary>Wall-clock unix-ms; always populated.</summary>
            public long TimestampUnixMs;

            /// <summary>
            /// Snapshot of <see cref="TurnManager.TickCount"/> at record time, or
            /// null when fired outside the turn loop (worldgen, save, bootstrap).
            /// Queries that filter by since_turn/until_turn EXCLUDE null-Turn
            /// records; use since_unix_ms/until_unix_ms to include them.
            /// </summary>
            public int? Turn;

            /// <summary>Optional <see cref="Entity.ID"/> of the actor (acting/source entity).</summary>
            public string ActorId;

            /// <summary>Optional <see cref="Entity.ID"/> of the target.</summary>
            public string TargetId;

            /// <summary>Optional trace-ID of the upstream record that caused this one.</summary>
            public string CauseTraceId;

            /// <summary>
            /// Category-specific payload, JSON-serialized synchronously at
            /// Record() time (eager). See AI-OBSERVABILITY.md §3 Layer 0.
            /// May be null when the recorder didn't pass a payload.
            /// </summary>
            public string PayloadJson;
        }

        // ====================================================================
        // Configuration
        // ====================================================================

        private const int BufferSize = 1024;

        /// <summary>
        /// Categories enabled by default. Per AI-OBSERVABILITY.md §3 Layer 1
        /// these correspond to the meta-foundational hooks (event, turn) and
        /// the first deeply-observed system's hooks (effect, damage). Other
        /// categories (`material`, `ai`, future per-system additions) start
        /// disabled until <see cref="SetChannel"/> turns them on.
        /// </summary>
        private static readonly string[] DefaultOnCategories =
            { "event", "effect", "damage", "turn", "furniture", "trade", "quest" };

        // ====================================================================
        // Storage
        // ====================================================================

        private static readonly Entry[] _buffer = new Entry[BufferSize];
        private static int _writeIndex;       // next write slot, 0..BufferSize-1
        private static int _filledCount;      // total slots filled, capped at BufferSize
        private static long _droppedCount;    // records dropped because the buffer wrapped
        private static readonly Dictionary<string, bool> _channels = new Dictionary<string, bool>();
        private static readonly string _sessionId = Guid.NewGuid().ToString("N").Substring(0, 12);

        /// <summary>
        /// Newtonsoft serializer settings tuned for diag payloads:
        /// <list type="bullet">
        /// <item><c>ReferenceLoopHandling.Ignore</c> — silently drop cycles
        /// rather than recurse forever (R1 mitigation in D1-SPIKE-PLAN §3).</item>
        /// <item><c>MaxDepth = 4</c> — bound serialization for accidentally
        /// deep object graphs (e.g., entity → part → owner → entity).</item>
        /// </list>
        /// </summary>
        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            MaxDepth = 4,
            NullValueHandling = NullValueHandling.Include,
        };

        static Diag()
        {
            foreach (var cat in DefaultOnCategories)
                _channels[cat] = true;
        }

        // ====================================================================
        // Public API
        // ====================================================================

        /// <summary>
        /// True if the named category is currently enabled for recording.
        /// Unknown categories return false (off-by-default per
        /// AI-OBSERVABILITY.md §10 Step 2).
        /// </summary>
        public static bool IsChannelEnabled(string category)
        {
            if (string.IsNullOrEmpty(category)) return false;
            return _channels.TryGetValue(category, out bool enabled) && enabled;
        }

        /// <summary>
        /// Enable or disable recording for a named category. Categories are
        /// free-form strings; calling SetChannel with a never-before-used
        /// name registers it. Off-by-default unless in
        /// <see cref="DefaultOnCategories"/>.
        /// </summary>
        public static void SetChannel(string category, bool enabled)
        {
            if (string.IsNullOrEmpty(category)) return;
            _channels[category] = enabled;
        }

        /// <summary>
        /// Record an observable into the ring buffer. No-op when the
        /// category is disabled.
        ///
        /// The <paramref name="payload"/> is JSON-serialized synchronously
        /// (eager); subsequent mutations to the payload object do not
        /// affect the captured record. See AI-OBSERVABILITY.md §3 Layer 0.
        ///
        /// On exception during serialization (e.g., a malformed object),
        /// the record is dropped and a warning is logged via
        /// <see cref="Debug.LogWarning"/>; gameplay never throws on a bad
        /// Record call.
        /// </summary>
        /// <param name="category">Category string (e.g., "effect", "damage").</param>
        /// <param name="kind">Kind within category (e.g., "OnApply").</param>
        /// <param name="actor">Optional acting entity; only its ID is captured.</param>
        /// <param name="target">Optional target entity; only its ID is captured.</param>
        /// <param name="payload">Optional payload object; JSON-serialized eagerly.</param>
        /// <param name="cause">Optional upstream trace-ID for causal linking.</param>
        public static void Record(
            string category,
            string kind,
            Entity actor = null,
            Entity target = null,
            object payload = null,
            string cause = null)
        {
            if (string.IsNullOrEmpty(category)) return;
            if (string.IsNullOrEmpty(kind)) return;
            if (!IsChannelEnabled(category)) return;

            try
            {
                var rec = new Entry
                {
                    TraceId = Guid.NewGuid().ToString("N").Substring(0, 8),
                    Category = category,
                    Kind = kind,
                    TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Turn = TryGetCurrentTurn(),
                    ActorId = actor?.ID,
                    TargetId = target?.ID,
                    CauseTraceId = cause ?? _currentCause.Value,
                    PayloadJson = payload != null
                        ? JsonConvert.SerializeObject(payload, _serializerSettings)
                        : null,
                };

                Append(rec);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[Diag] Record failed for category={category} kind={kind}: {ex.Message}");
            }
        }

        /// <summary>
        /// Snapshot up to <paramref name="limit"/> most-recent records,
        /// oldest-first. Read-only; does not mutate the buffer.
        /// </summary>
        public static IReadOnlyList<Entry> Snapshot(int limit = 50)
        {
            if (limit <= 0) return Array.Empty<Entry>();
            int count = Math.Min(limit, _filledCount);
            if (count == 0) return Array.Empty<Entry>();

            var result = new List<Entry>(count);

            // Walk backwards from newest, then reverse for oldest-first.
            int newestIndex = (_writeIndex - 1 + BufferSize) % BufferSize;
            for (int i = 0; i < count; i++)
            {
                int idx = (newestIndex - i + BufferSize) % BufferSize;
                result.Add(_buffer[idx]);
            }
            result.Reverse();
            return result;
        }

        /// <summary>Total records dropped because the ring buffer wrapped past them.</summary>
        public static long DroppedCount => _droppedCount;

        /// <summary>Buffer fill percentage (0–100). Useful for the meta block in tool responses.</summary>
        public static int BufferFillPercent =>
            (int)((100L * _filledCount) / BufferSize);

        /// <summary>Buffer capacity. Public for tests; not configurable in D1.</summary>
        public static int BufferCapacity => BufferSize;

        /// <summary>Stable session ID for the running Unity instance (12-char hex).</summary>
        public static string SessionId => _sessionId;

        /// <summary>
        /// Reset the ring buffer, dropped count, and channel state to
        /// defaults. <strong>TEST ONLY</strong> — calling this in production
        /// destroys every captured record.
        /// </summary>
        public static void ResetAll()
        {
            for (int i = 0; i < BufferSize; i++)
                _buffer[i] = default;
            _writeIndex = 0;
            _filledCount = 0;
            _droppedCount = 0;
            _channels.Clear();
            foreach (var cat in DefaultOnCategories)
                _channels[cat] = true;
            // Clear the ambient WithCause scope. Test isolation: a leaky
            // scope in one test must not bleed into the next.
            _currentCause.Value = null;
        }

        /// <summary>
        /// Set an ambient cause-trace-ID for nested <see cref="Record"/>
        /// calls within the using-scope. Records inside the scope auto-link
        /// to <paramref name="traceId"/> via their <c>CauseTraceId</c> field
        /// unless an explicit <c>cause</c> argument overrides it.
        ///
        /// Implementation uses <see cref="AsyncLocal{T}"/> so concurrent
        /// flows (TPL Tasks, async methods) don't interfere — each logical
        /// call chain has its own ambient cause.
        ///
        /// Nesting: inner scope shadows outer; outer is restored on dispose.
        ///
        /// <strong>Anti-pattern:</strong> don't put scopes inside per-effect-tick
        /// or per-cell-render paths — the <see cref="CauseScope"/> allocation
        /// per call adds up. Use scopes at the level of "an attack" or
        /// "a trap firing" (once per high-level action).
        /// </summary>
        public static IDisposable WithCause(string traceId)
        {
            string previous = _currentCause.Value;
            _currentCause.Value = traceId;
            return new CauseScope(previous);
        }

        /// <summary>
        /// Current ambient cause-trace-ID set by an active
        /// <see cref="WithCause"/> scope on the current async flow, or
        /// <c>null</c> if no scope is active.
        /// </summary>
        public static string CurrentCause => _currentCause.Value;

        // ====================================================================
        // Internals
        // ====================================================================

        /// <summary>
        /// Resolve current turn from the static <see cref="TurnManager.Active"/>.
        /// Returns null when no game is running (e.g., bootstrap, EditMode tests
        /// that don't construct a TurnManager) or when CurrentActor is unset
        /// (between turns; worldgen; save/load).
        /// </summary>
        private static int? TryGetCurrentTurn()
        {
            var tm = TurnManager.Active;
            return (tm != null && tm.CurrentActor != null)
                ? (int?)tm.TickCount
                : null;
        }

        /// <summary>
        /// Append one record to the ring buffer. Wraps on overflow, incrementing
        /// <see cref="_droppedCount"/>. Single-threaded — no locks.
        /// </summary>
        private static void Append(in Entry rec)
        {
            _buffer[_writeIndex] = rec;
            _writeIndex = (_writeIndex + 1) % BufferSize;
            if (_filledCount < BufferSize)
                _filledCount++;
            else
                _droppedCount++;
        }

        // ====================================================================
        // WithCause ambient scope (D2.3)
        // ====================================================================

        // AsyncLocal so each logical call chain has its own ambient cause —
        // concurrent Tasks / async methods don't interfere.
        private static readonly AsyncLocal<string> _currentCause = new AsyncLocal<string>();

        /// <summary>
        /// IDisposable that restores the previous ambient cause when
        /// disposed. Allocated per <see cref="WithCause"/> call (small
        /// — one ref + one string field).
        /// </summary>
        private sealed class CauseScope : IDisposable
        {
            private readonly string _previous;
            private bool _disposed;

            public CauseScope(string previous)
            {
                _previous = previous;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _currentCause.Value = _previous;
            }
        }
    }
}
