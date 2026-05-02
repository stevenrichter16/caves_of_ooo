using CavesOfOoo.Diagnostics;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;

namespace CavesOfOoo.Editor.Diagnostics
{
    /// <summary>
    /// MCP tool — predicate match. Answers "did at least one record
    /// matching the filter exist in the buffer?" as a boolean, plus
    /// the count and a sample trace-id for follow-up inspection.
    ///
    /// Plan ref: <c>Docs/D3-TOOLS-PLAN.md</c> §4 D3.2.
    ///
    /// Why a separate tool when <c>diag_count</c> exists: prompt-shape
    /// clarity. An LLM asking "did effect/OnApply fire for the player
    /// during turn 47?" wants a boolean back, not a number to compare
    /// against zero. Both are token-cheap; pick whichever frames the
    /// question best.
    ///
    /// Wraps <see cref="DiagQuery.Count"/> internally:
    ///   matched = (count > 0)
    ///
    /// Auto-discovered by <see cref="MCPForUnity.Editor.Tools.CommandRegistry"/>.
    /// Callable via:
    /// <c>execute_custom_tool {tool_name: "diag_assert", parameters: {category: "effect", kind: "OnRemove", target: "player"}}</c>
    ///
    /// Response shape (inside the SuccessResponse envelope):
    /// <code>
    /// {
    ///     matched: bool,
    ///     count: int,
    ///     sample_first_trace_id: string | null,
    ///     sample_first_kind: string | null,
    ///     tool_version: "diag_assert/1"
    /// }
    /// </code>
    /// Field names mirror <c>diag_count</c> intentionally — they're
    /// derived from the same <c>DiagQuery.CountResult</c>.
    /// </summary>
    [McpForUnityTool(
        name: "diag_assert",
        Description = "Predicate match: did at least one record matching the filter exist? Returns matched bool plus count + sample trace-id.")]
    public static class DiagAssertTool
    {
        public class Parameters
        {
            [ToolParameter("Filter by category (e.g., 'effect', 'damage', 'turn'). Omit for all.", Required = false)]
            public string category { get; set; }

            [ToolParameter("Filter by kind within a category. Omit for all.", Required = false)]
            public string kind { get; set; }

            [ToolParameter("Filter by actor Entity ID. Omit for all.", Required = false)]
            public string actor { get; set; }

            [ToolParameter("Filter by target Entity ID. Omit for all.", Required = false)]
            public string target { get; set; }

            [ToolParameter("Lower-bound turn filter (inclusive). Records with Turn=null are EXCLUDED from any windowed query.", Required = false)]
            public int? since_turn { get; set; }

            [ToolParameter("Upper-bound turn filter (inclusive). Same null-Turn exclusion as since_turn.", Required = false)]
            public int? until_turn { get; set; }
        }

        private const string ToolVersion = "diag_assert/1";

        public static object HandleCommand(JObject @params)
        {
            try
            {
                @params ??= new JObject();

                var result = DiagQuery.Count(new DiagQuery.Filter
                {
                    Category = @params["category"]?.ToString(),
                    Kind = @params["kind"]?.ToString(),
                    Actor = @params["actor"]?.ToString(),
                    Target = @params["target"]?.ToString(),
                    SinceTurn = @params["since_turn"]?.ToObject<int?>(),
                    UntilTurn = @params["until_turn"]?.ToObject<int?>(),
                });

                return new SuccessResponse(message: null, data: new
                {
                    matched = result.Count > 0,
                    count = result.Count,
                    // Field names must match diag_count's response (these
                    // come from the same CountResult struct). Earlier
                    // version used `first_trace_id` / `first_kind` — the
                    // doc spec was inconsistent with diag_count and the
                    // cold-eye pass caught it. AI-OBSERVABILITY.md updated.
                    sample_first_trace_id = result.SampleFirstTraceId,
                    sample_first_kind = result.SampleFirstKind,
                    tool_version = ToolVersion,
                });
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                return new ErrorResponse($"diag_assert failed: {ex.Message}");
            }
        }
    }
}
