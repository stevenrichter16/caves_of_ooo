using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Diagnostics;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CavesOfOoo.Editor.Diagnostics
{
    /// <summary>
    /// MCP tool for filtering the <see cref="Diag"/> ring buffer.
    ///
    /// Plan ref: <c>Docs/D1-SPIKE-PLAN.md</c> §5 D1.3.
    /// Design contract: <c>Docs/AI-OBSERVABILITY.md</c> §3 Layer 2.
    ///
    /// Auto-discovered by <see cref="MCPForUnity.Editor.Tools.CommandRegistry"/>.
    /// First-class MCP tool: callable directly via
    /// <c>/tmp/mcp-call.sh diag_query '{"category":"effect", ...}'</c>
    /// (no <c>execute_custom_tool</c> wrapper — auto-registered tools
    /// are wrapped as first-class FastMCP tools by
    /// <c>custom_tool_service.py</c>).
    ///
    /// Spike scope (D1.3): filter by category/kind/target/limit. The
    /// fields=[...] projection and since_turn/until_turn time-window
    /// filters are documented in the schema but not yet implemented;
    /// they ship in D3 (post-spike).
    ///
    /// Response shape:
    /// <code>
    /// {
    ///     meta: {
    ///         session_id, timestamp_unix_ms, buffer_fill_pct,
    ///         dropped_records, tool_version
    ///     },
    ///     data: [ Entry, Entry, ... ],
    ///     truncated: bool,
    ///     would_be_size_bytes: int (only when truncated)
    /// }
    /// </code>
    /// </summary>
    [McpForUnityTool(
        name: "diag_query",
        Description = "Filter the AI-debugging diag ring buffer. Returns records matching category/kind/target with optional limit + budget enforcement.")]
    public static class DiagQueryTool
    {
        /// <summary>Schema for the LLM-facing tool definition.</summary>
        public class Parameters
        {
            [ToolParameter("Filter by category (e.g., 'effect', 'event', 'damage'). Omit for all.", Required = false)]
            public string category { get; set; }

            [ToolParameter("Filter by kind within a category (e.g., 'OnApply', 'OnRemove'). Omit for all.", Required = false)]
            public string kind { get; set; }

            [ToolParameter("Filter by target Entity ID. Omit for all.", Required = false)]
            public string target { get; set; }

            [ToolParameter("Filter by actor Entity ID. Omit for all.", Required = false)]
            public string actor { get; set; }

            [ToolParameter("Max records (default 50, max 500).", Required = false, DefaultValue = "50")]
            public int? limit { get; set; }

            [ToolParameter("Response-size budget in KB (default 100, max 1000). Tool refuses to return responses over budget; override here when intentionally pulling large data.", Required = false, DefaultValue = "100")]
            public int? budget_kb { get; set; }
        }

        // Tool metadata constants
        private const int DefaultLimit = 50;
        private const int MaxLimit = 500;
        private const int DefaultBudgetKb = 100;
        private const int MaxBudgetKb = 1000;
        private const string ToolVersion = "diag_query/1";

        /// <summary>
        /// Entry point invoked by the MCP dispatcher when a caller sends
        /// <c>diag_query</c>. The <paramref name="params"/> JObject carries
        /// the runtime parameter values; nested objects work even though
        /// the schema declares scalars (per Step 0 finding #3).
        ///
        /// Wraps the body in try/catch per spike-plan R9: substrate bugs
        /// become tool errors, not silent crashes.
        /// </summary>
        public static object HandleCommand(JObject @params)
        {
            try
            {
                @params ??= new JObject();

                // Parse filters (all optional)
                string category = @params["category"]?.ToString();
                string kind = @params["kind"]?.ToString();
                string targetId = @params["target"]?.ToString();
                string actorId = @params["actor"]?.ToString();
                int limit = ClampLimit(@params["limit"]?.ToObject<int?>() ?? DefaultLimit);
                int budgetKb = ClampBudget(@params["budget_kb"]?.ToObject<int?>() ?? DefaultBudgetKb);

                // Delegate filter logic to the runtime-side DiagQuery helper
                // so EditMode tests can exercise filtering directly without
                // pulling in the Editor assembly + Newtonsoft. The wrapper
                // here stays thin: parse params → call DiagQuery → wrap.
                var filterResult = DiagQuery.Apply(new DiagQuery.Filter
                {
                    Category = category,
                    Kind = kind,
                    Actor = actorId,
                    Target = targetId,
                    Limit = limit,
                });
                var filtered = filterResult.Records;

                // Build the inner response payload. We construct THEN size-
                // check — an alternative would be streaming with running
                // size, but for a 100KB budget the simpler one-pass
                // approach is fine.
                var responseData = new
                {
                    meta = BuildMeta(filtered.Count, filterResult.TotalScanned),
                    data = filtered,
                    truncated = false,
                };

                // Pre-flight size check (per AI-OBSERVABILITY.md §3 Layer 2
                // budget enforcement). Sized on the inner payload only —
                // the SuccessResponse envelope adds ~50 bytes of overhead,
                // negligible vs. the 100KB default budget.
                string serialized = JsonConvert.SerializeObject(responseData);
                int sizeBytes = serialized.Length;
                int budgetBytes = budgetKb * 1024;
                if (sizeBytes > budgetBytes)
                {
                    return new SuccessResponse(message: null, data: new
                    {
                        meta = BuildMeta(0, filterResult.TotalScanned),
                        data = (object)null,
                        truncated = true,
                        would_be_size_bytes = sizeBytes,
                        hint = $"Response of {sizeBytes / 1024}KB exceeded {budgetKb}KB budget. " +
                               "Use a smaller limit, narrow filters (category/kind/target), " +
                               $"or pass budget_kb=N (max {MaxBudgetKb}) to override.",
                    });
                }

                // Wrap in SuccessResponse so the FastMCP Python normalizer
                // (custom_tool_service._normalize_response) places the
                // whole {meta, data, truncated} object into the envelope's
                // `data` field. Without this wrapping, the normalizer
                // pulls out our inner `data` field and discards `meta`
                // and `truncated`. See AI-OBSERVABILITY.md §3 Layer 2.
                return new SuccessResponse(message: null, data: responseData);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                return new ErrorResponse($"diag_query failed: {ex.Message}");
            }
        }

        // ====================================================================
        // Internals
        // ====================================================================

        private static int ClampLimit(int requested)
        {
            if (requested <= 0) return DefaultLimit;
            if (requested > MaxLimit) return MaxLimit;
            return requested;
        }

        private static int ClampBudget(int requested)
        {
            if (requested <= 0) return DefaultBudgetKb;
            if (requested > MaxBudgetKb) return MaxBudgetKb;
            return requested;
        }

        private static object BuildMeta(int returned, int totalScanned)
        {
            return new
            {
                session_id = Diag.SessionId,
                timestamp_unix_ms = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                buffer_fill_pct = Diag.BufferFillPercent,
                dropped_records = Diag.DroppedCount,
                returned_count = returned,
                total_scanned = totalScanned,
                tool_version = ToolVersion,
            };
        }
    }
}
