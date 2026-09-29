using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Bounded diagnostic only: no gameplay or profiler-window configuration.
    public sealed class SpreadHuntNativeCounterProbeTests
    {
        static object sink;
        [Serializable] public sealed class Row
        {
            public string category, action, unit;
            public bool valid, wrapped;
            public int count, capacity;
            public long valueSum, countSum, current, last;
        }
        [Serializable] public sealed class Report { public List<Row> rows = new List<Row>(); }
        static Row Measure(ProfilerCategory category, string label, Action action)
        {
            var row = new Row { category = category.ToString(), action = label };
            using (var recorder = ProfilerRecorder.StartNew(category, "GC.Alloc", 128, ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
            {
                action();
                recorder.Stop();
                row.valid = recorder.Valid;
                if (row.valid)
                {
                    row.unit = recorder.UnitType.ToString(); row.count = recorder.Count; row.capacity = recorder.Capacity;
                    row.wrapped = recorder.WrappedAround; row.current = recorder.CurrentValue; row.last = recorder.LastValue;
                    for (int i = 0; i < recorder.Count; i++) { var sample = recorder.GetSample(i); row.valueSum += sample.Value; row.countSum += sample.Count; }
                }
            }
            return row;
        }
        [TestCase(false)] [TestCase(true)]
        public void ReportSynchronousProfilerAllocationCounter(bool memory)
        {
            var category = memory ? ProfilerCategory.Memory : ProfilerCategory.Internal;
            Action empty = () => { }; Action small = () => sink = new byte[1024]; Action big = () => sink = new byte[16384];
            Measure(category, "warm", small);
            var report = new Report();
            report.rows.Add(Measure(category, "empty", empty)); report.rows.Add(Measure(category, "array1024", small)); report.rows.Add(Measure(category, "array16384", big));
            TestContext.WriteLine(JsonUtility.ToJson(report)); sink = null;
            var e = report.rows[0]; var s = report.rows[1]; var b = report.rows[2];
            if (!s.valid || s.unit != "Bytes" || s.valueSum < 1024 || b.valueSum < 16384 || e.valueSum != 0)
                Assert.Inconclusive("Synchronous native byte counter unavailable or uncalibrated; read retained rows. This is not a zero-allocation pass.");
        }
    }
}
