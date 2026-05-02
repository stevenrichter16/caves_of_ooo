using CavesOfOoo.Diagnostics;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;

namespace CavesOfOoo.Editor.Diagnostics
{
    /// <summary>
    /// MCP tool — inspect one record + its causal neighborhood. Given
    /// a trace-id, returns:
    ///   - the record itself
    ///   - <c>caused_by</c>: ordered ancestors (backward walk via
    ///     <see cref="Diag.Entry.CauseTraceId"/>)
    ///   - <c>caused</c>: descendants (other records whose
    ///     CauseTraceId equals this record's TraceId)
    ///
    /// Plan ref: <c>Docs/D3-TOOLS-PLAN.md</c> §4 D3.3.
    ///
    /// Use case: an LLM has a trace-id from <c>diag_query</c> /
    /// <c>diag_count</c> / <c>diag_assert</c> and wants to know
    /// "what caused this?" or "what did this trigger?" — one call
    /// returns both directions.
    ///
    /// Auto-discovered by <see cref="MCPForUnity.Editor.Tools.CommandRegistry"/>.
    /// Callable via:
    /// <c>execute_custom_tool {tool_name: "diag_inspect_record", parameters: {trace_id: "abc12345"}}</c>
    ///
    /// Response shape (inside the SuccessResponse envelope):
    /// <code>
    /// {
    ///     record: Entry | null,
    ///     caused_by: [Entry, ...],   // ancestors, immediate-first
    ///     caused: [Entry, ...],      // descendants, oldest-first
    ///     tool_version: "diag_inspect_record/1"
    /// }
    /// </code>
    ///
    /// Returns <c>record: null</c> when the trace-id is not in the
    /// buffer (either invalid or already overwritten by ring rotation).
    /// </summary>
    [McpForUnityTool(
        name: "diag_inspect_record",
        Description = "Inspect one record + its causal neighbors (ancestors via CauseTraceId backward walk, descendants via buffer scan).")]
    public static class DiagInspectRecordTool
    {
        public class Parameters
        {
            [ToolParameter("Trace-id of the record to inspect (8-char Guid prefix, from a prior diag_query/diag_count/diag_assert response).", Required = true)]
            public string trace_id { get; set; }

            [ToolParameter("Maximum length of the backward causal chain to walk (default 16). Cycle-protected; protects against buggy hooks creating loops.", Required = false, DefaultValue = "16")]
            public int? chain_limit { get; set; }
        }

        private const string ToolVersion = "diag_inspect_record/1";

        public static object HandleCommand(JObject @params)
        {
            try
            {
                @params ??= new JObject();

                string traceId = @params["trace_id"]?.ToString();
                if (string.IsNullOrEmpty(traceId))
                {
                    return new ErrorResponse("diag_inspect_record requires trace_id parameter");
                }

                int chainLimit = @params["chain_limit"]?.ToObject<int?>() ?? 16;
                if (chainLimit <= 0) chainLimit = 16;

                var result = DiagQuery.InspectRecord(traceId, chainLimit);

                // Not-found surfaces as record:null + empty arrays.
                // Doesn't error: caller may legitimately ask "is this
                // trace-id still in the buffer?" and expect a boolean-
                // like null check rather than an exception.
                if (result == null)
                {
                    return new SuccessResponse(message: null, data: new
                    {
                        record = (object)null,
                        caused_by = new object[0],
                        caused = new object[0],
                        tool_version = ToolVersion,
                    });
                }

                return new SuccessResponse(message: null, data: new
                {
                    record = result.Record,
                    caused_by = result.CausedBy,
                    caused = result.Caused,
                    tool_version = ToolVersion,
                });
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                return new ErrorResponse($"diag_inspect_record failed: {ex.Message}");
            }
        }
    }
}
