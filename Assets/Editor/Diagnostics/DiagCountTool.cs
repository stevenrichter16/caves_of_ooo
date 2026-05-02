using CavesOfOoo.Diagnostics;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;

namespace CavesOfOoo.Editor.Diagnostics
{
    /// <summary>
    /// MCP tool — token-cheap aggregation over the <see cref="Diag"/>
    /// ring buffer. Answers "how many records match this filter?"
    /// without returning the records themselves.
    ///
    /// Plan ref: <c>Docs/D2-HOOKS-PLAN.md</c> §4 D2.5.
    /// Pairs with <c>diag_query</c>: use <c>diag_count</c> to ask
    /// "did this fire 0/1/many times?", then follow up with
    /// <c>diag_query</c> to inspect specific records.
    ///
    /// Auto-discovered by <see cref="MCPForUnity.Editor.Tools.CommandRegistry"/>.
    /// Callable via:
    /// <c>execute_custom_tool {tool_name: "diag_count", parameters: {category: "damage"}}</c>
    ///
    /// Response shape (inside the SuccessResponse envelope):
    /// <code>
    /// {
    ///     count: int,
    ///     total_scanned: int,
    ///     sample_first_trace_id: string | null,
    ///     sample_first_kind: string | null,
    ///     tool_version: "diag_count/1"
    /// }
    /// </code>
    ///
    /// The <c>sample_first_*</c> fields let the caller follow up with
    /// <c>diag_query</c> on a specific record without first listing
    /// all of them. <c>null</c> when zero matches.
    /// </summary>
    [McpForUnityTool(
        name: "diag_count",
        Description = "Count records in the diag ring buffer matching a filter. Token-cheap aggregation paired with diag_query.")]
    public static class DiagCountTool
    {
        public class Parameters
        {
            [ToolParameter("Filter by category (e.g., 'effect', 'damage', 'turn'). Omit for all.", Required = false)]
            public string category { get; set; }

            [ToolParameter("Filter by kind within a category (e.g., 'OnApply', 'OnRemove', 'DamageDealt'). Omit for all.", Required = false)]
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

        private const string ToolVersion = "diag_count/1";

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
                    count = result.Count,
                    total_scanned = result.TotalScanned,
                    sample_first_trace_id = result.SampleFirstTraceId,
                    sample_first_kind = result.SampleFirstKind,
                    tool_version = ToolVersion,
                });
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                return new ErrorResponse($"diag_count failed: {ex.Message}");
            }
        }
    }
}
