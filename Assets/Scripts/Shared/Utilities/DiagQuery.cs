using System.Collections.Generic;

namespace CavesOfOoo.Diagnostics
{
    /// <summary>
    /// Filtering layer over the <see cref="Diag"/> ring buffer. Lives in the
    /// runtime assembly so EditMode tests can exercise it directly without
    /// needing the Editor-side <c>DiagQueryTool</c> MCP wrapper.
    ///
    /// Plan ref: <c>Docs/D1-SPIKE-PLAN.md</c> §5 D1.3.
    ///
    /// The MCP wrapper (<c>DiagQueryTool</c> in <c>Assets/Editor/Diagnostics/</c>)
    /// is intentionally thin: it parses JSON params, calls
    /// <see cref="Apply"/>, and adds the meta block + budget enforcement.
    /// The actual filter semantics live here.
    /// </summary>
    public static class DiagQuery
    {
        /// <summary>Filter spec passed to <see cref="Apply"/>. All fields optional.</summary>
        public class Filter
        {
            /// <summary>Filter by category string (exact match). Null = no category filter.</summary>
            public string Category;

            /// <summary>Filter by kind string (exact match). Null = no kind filter.</summary>
            public string Kind;

            /// <summary>Filter by actor entity ID (exact match). Null = no actor filter.</summary>
            public string Actor;

            /// <summary>Filter by target entity ID (exact match). Null = no target filter.</summary>
            public string Target;

            /// <summary>Max records to return. Clamped to [1, 500] by <see cref="Apply"/>; default 50.</summary>
            public int Limit = 50;
        }

        /// <summary>Result of <see cref="Apply"/> — filtered records plus stats.</summary>
        public class Result
        {
            /// <summary>Records matching the filter, ordered oldest-first, capped at <see cref="Filter.Limit"/>.</summary>
            public IReadOnlyList<Diag.Entry> Records;

            /// <summary>Total records scanned from the ring buffer (≥ Records.Count).</summary>
            public int TotalScanned;
        }

        private const int MaxLimit = 500;
        private const int DefaultLimit = 50;
        private const int SnapshotCap = 5000;

        /// <summary>
        /// Filter the ring buffer using the supplied <paramref name="filter"/>.
        /// Returns matching records oldest-first, capped at <c>Filter.Limit</c>.
        /// </summary>
        public static Result Apply(Filter filter)
        {
            filter ??= new Filter();

            int limit = filter.Limit;
            if (limit <= 0) limit = DefaultLimit;
            if (limit > MaxLimit) limit = MaxLimit;

            // Pull a generous slice; ring buffer holds ≤ 1024 records, so
            // SnapshotCap=5000 always returns the whole buffer.
            var all = Diag.Snapshot(SnapshotCap);

            var matched = new List<Diag.Entry>(limit);
            for (int i = 0; i < all.Count; i++)
            {
                var rec = all[i];
                if (filter.Category != null && rec.Category != filter.Category) continue;
                if (filter.Kind != null && rec.Kind != filter.Kind) continue;
                if (filter.Actor != null && rec.ActorId != filter.Actor) continue;
                if (filter.Target != null && rec.TargetId != filter.Target) continue;
                matched.Add(rec);
                if (matched.Count >= limit) break;
            }

            return new Result
            {
                Records = matched,
                TotalScanned = all.Count,
            };
        }

        /// <summary>Aggregation result for <see cref="Count"/>.</summary>
        public class CountResult
        {
            /// <summary>Number of records matching the filter (uncapped — counts up to <see cref="Diag.BufferCapacity"/>).</summary>
            public int Count;

            /// <summary>Total records scanned from the ring buffer (≥ <see cref="Count"/>).</summary>
            public int TotalScanned;

            /// <summary>TraceId of the first matching record, or null if no matches.</summary>
            public string SampleFirstTraceId;

            /// <summary>Kind of the first matching record, or null if no matches.</summary>
            public string SampleFirstKind;
        }

        /// <summary>
        /// Count records matching the filter without copying the full
        /// records list. Token-cheap: returns the count plus a sample
        /// trace-ID so the caller can follow up with <see cref="Apply"/>
        /// on a specific match if needed.
        ///
        /// Unlike <see cref="Apply"/>, the count is uncapped — the
        /// <see cref="Filter.Limit"/> is ignored. The scan ceiling is
        /// the ring buffer's capacity itself.
        ///
        /// Plan ref: <c>Docs/D2-HOOKS-PLAN.md</c> §4 D2.5.
        /// </summary>
        public static CountResult Count(Filter filter)
        {
            filter ??= new Filter();

            var all = Diag.Snapshot(SnapshotCap);

            int count = 0;
            string firstTraceId = null;
            string firstKind = null;
            for (int i = 0; i < all.Count; i++)
            {
                var rec = all[i];
                if (filter.Category != null && rec.Category != filter.Category) continue;
                if (filter.Kind != null && rec.Kind != filter.Kind) continue;
                if (filter.Actor != null && rec.ActorId != filter.Actor) continue;
                if (filter.Target != null && rec.TargetId != filter.Target) continue;
                if (count == 0)
                {
                    firstTraceId = rec.TraceId;
                    firstKind = rec.Kind;
                }
                count++;
            }

            return new CountResult
            {
                Count = count,
                TotalScanned = all.Count,
                SampleFirstTraceId = firstTraceId,
                SampleFirstKind = firstKind,
            };
        }
    }
}
