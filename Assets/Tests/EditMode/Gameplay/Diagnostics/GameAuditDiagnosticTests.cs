using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Whole-game audit A20/A26: reproduce through runtime and actual editor adapters.</summary>
    public class GameAuditDiagnosticTests
    {
        [SetUp] public void SetUp() => Diag.ResetAll();
        [TearDown] public void TearDown() => Diag.ResetAll();

        [TestCase("wanted", 1)]
        [TestCase("missing", 0)]
        [TestCase(null, 2)]
        public void CauseCount_MatchesQueryAndSample(string cause, int expected)
        {
            Diag.Record("event", "Other", cause: "other");
            Diag.Record("event", "Wanted", cause: "wanted");
            var filter = new DiagQuery.Filter { CauseTraceId = cause };
            var query = DiagQuery.Apply(filter);
            var count = DiagQuery.Count(filter);
            Assert.AreEqual(expected, query.Records.Count);
            Assert.AreEqual(expected, count.Count);
            Assert.AreEqual(expected == 0 ? null : query.Records[0].TraceId, count.SampleFirstTraceId);
            Assert.AreEqual(expected == 0 ? null : query.Records[0].Kind, count.SampleFirstKind);
        }

        [TestCase("DiagQueryTool")]
        [TestCase("DiagCountTool")]
        [TestCase("DiagAssertTool")]
        public void ActualAdapter_HonorsCauseAndOmittedControl(string tool)
        {
            Diag.Record("event", "Other", cause: "other");
            Diag.Record("event", "Wanted", cause: "wanted");
            object filtered = DiagnosticAdapterProbe.Call(tool, "{\"cause_trace_id\":\"wanted\"}");
            Assert.AreEqual(1, DiagnosticAdapterProbe.Count(tool, filtered));
            Assert.AreEqual(2, DiagnosticAdapterProbe.Count(tool, DiagnosticAdapterProbe.Call(tool, "{}")));
            object absent = DiagnosticAdapterProbe.Call(tool, "{\"cause_trace_id\":\"missing\"}");
            Assert.AreEqual(0, DiagnosticAdapterProbe.Count(tool, absent));
            if (tool == "DiagAssertTool")
            {
                Assert.IsTrue((bool)DiagnosticAdapterProbe.Member(filtered, "matched"));
                Assert.IsFalse((bool)DiagnosticAdapterProbe.Member(absent, "matched"));
            }
        }

        [TestCase("DiagQueryTool")]
        [TestCase("DiagCountTool")]
        [TestCase("DiagAssertTool")]
        public void CauseFilter_IsExposedInActualToolSchema(string tool)
        {
            Type parameters = DiagnosticAdapterProbe.Tool(tool).GetNestedType("Parameters");
            var property = parameters.GetProperty("cause_trace_id");
            Assert.NotNull(property, "The callable filter must also be advertised to tool clients.");
            Assert.AreEqual(typeof(string), property.PropertyType);
            Assert.IsTrue(Array.Exists(property.GetCustomAttributes(false),
                a => a.GetType().Name == "ToolParameterAttribute"));
        }

        [Test]
        public void QueryBudget_UsesUtf8Bytes_WithAsciiControl()
        {
            Diag.Record("event", "Unicode", payload: new { text = new string('漢', 300) });
            object unrestricted = DiagnosticAdapterProbe.Call("DiagQueryTool", "{\"budget_kb\":100}");
            string serialized = DiagnosticAdapterProbe.Serialize(unrestricted);
            Assert.Less(serialized.Length, 1024, "Fixture must distinguish character and byte budgets.");
            int bytes = Encoding.UTF8.GetByteCount(serialized);
            Assert.Greater(bytes, 1024);
            object limited = DiagnosticAdapterProbe.Call("DiagQueryTool", "{\"budget_kb\":1}");
            Assert.IsTrue((bool)DiagnosticAdapterProbe.Member(limited, "truncated"));
            Assert.IsNull(DiagnosticAdapterProbe.Member(limited, "data"));
            Assert.AreEqual(bytes, DiagnosticAdapterProbe.Member(limited, "would_be_size_bytes"));

            Diag.ResetAll();
            Diag.Record("event", "Unicode", payload: new { text = new string('a', 300) });
            object ascii = DiagnosticAdapterProbe.Call("DiagQueryTool", "{\"budget_kb\":1}");
            Assert.IsFalse((bool)DiagnosticAdapterProbe.Member(ascii, "truncated"));
            Assert.AreEqual(1, DiagnosticAdapterProbe.Count("DiagQueryTool", ascii));
        }

        [Test]
        public void AllRetainedRecords_AreSearchableAndCounted()
        {
            Diag.Record("event", "Early", cause: "early-cause");
            for (int i = 1; i < Diag.BufferCapacity; i++) Diag.Record("event", "Later");
            var query = DiagQuery.Apply(new DiagQuery.Filter { Kind = "Early" });
            Assert.AreEqual(1, query.Records.Count, "Retained oldest entries must not disappear at a smaller scan ceiling.");
            Assert.AreEqual(Diag.BufferCapacity, query.TotalScanned);
            Assert.AreEqual(Diag.BufferCapacity, DiagQuery.Count(null).Count);
            Assert.AreEqual(1, DiagQuery.Count(new DiagQuery.Filter { CauseTraceId = "early-cause" }).Count);
        }

        [Test]
        public void RetainedEarlyCausalAncestor_RemainsInspectable()
        {
            Diag.Record("event", "Root");
            string root = Diag.Snapshot(1)[0].TraceId;
            for (int i = 1; i < Diag.BufferCapacity - 1; i++) Diag.Record("event", "Other");
            Diag.Record("event", "Child", cause: root);
            string child = Diag.Snapshot(1)[0].TraceId;
            var inspection = DiagQuery.InspectRecord(child);
            Assert.AreEqual(1, inspection.CausedBy.Count);
            Assert.AreEqual(root, inspection.CausedBy[0].TraceId);
            Assert.AreEqual(child, DiagQuery.InspectRecord(root).Caused[0].TraceId);
        }
    }

    /// <summary>Calls loaded production editor adapters without adding editor/plugin assembly dependencies.</summary>
    internal static class DiagnosticAdapterProbe
    {
        public static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName, false);
                if (type != null) return type;
            }
            Assert.Fail("Loaded production type not found: " + fullName);
            return null;
        }

        public static Type Tool(string name) => FindType("CavesOfOoo.Editor.Diagnostics." + name);

        public static object Call(string name, string json)
        {
            Type jsonObject = FindType("Newtonsoft.Json.Linq.JObject");
            object parameters = json == null ? null : jsonObject.GetMethod("Parse", new[] { typeof(string) })
                .Invoke(null, new object[] { json });
            object envelope = Tool(name).GetMethod("HandleCommand").Invoke(null, new[] { parameters });
            Assert.IsTrue((bool)Member(envelope, "Success"), "Production adapter must return a successful envelope.");
            return Member(envelope, "Data");
        }

        public static object Member(object obj, string name)
        {
            Assert.NotNull(obj);
            PropertyInfo property = obj.GetType().GetProperty(name);
            Assert.NotNull(property, "Missing response field: " + name);
            return property.GetValue(obj);
        }

        public static int Count(string tool, object data)
        {
            if (tool != "DiagQueryTool") return (int)Member(data, "count");
            int count = 0;
            foreach (object ignored in (IEnumerable)Member(data, "data")) count++;
            return count;
        }

        public static string Serialize(object value) => (string)FindType("Newtonsoft.Json.JsonConvert")
            .GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { value });
    }
}
