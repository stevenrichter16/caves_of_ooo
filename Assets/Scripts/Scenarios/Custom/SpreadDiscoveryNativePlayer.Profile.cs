using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class SpreadDiscoveryNativePlayer
    {
        // Audit-only observer: no world mutation, new actions, or padded waits.
        static readonly string[] SoddenMetricNames = { "COO.ZoneRenderer.LateUpdate", "Main Thread", "GC Allocated In Frame" };
        ProfilerRecorder[] _soddenRecorders;
        long[][] _soddenMetricSamples;
        string[] _soddenMetricUnits;
        bool[] _soddenMetricValid;
        int _soddenMetricFrames;
        double _soddenMetricStarted;
        const int SoddenMetricCapacity = 20000;

        void BeginSoddenProfile()
        {
            if (!_soddenDistrict || _soddenRecorders != null) return;
            _soddenRecorders = new ProfilerRecorder[SoddenMetricNames.Length];
            _soddenMetricSamples = new long[SoddenMetricNames.Length][];
            _soddenMetricUnits = new string[SoddenMetricNames.Length];
            _soddenMetricValid = new bool[SoddenMetricNames.Length];
            _soddenMetricFrames = 0; _soddenMetricStarted = Time.realtimeSinceStartupAsDouble;
            for (int i = 0; i < SoddenMetricNames.Length; i++)
            {
                _soddenMetricSamples[i] = new long[SoddenMetricCapacity];
                var category = i == 0 ? ProfilerCategory.Scripts : i == 1 ? ProfilerCategory.Internal : ProfilerCategory.Memory;
                var options = ProfilerRecorderOptions.Default;
                if (i == 0) options |= ProfilerRecorderOptions.SumAllSamplesInFrame;
                _soddenRecorders[i] = ProfilerRecorder.StartNew(category, SoddenMetricNames[i], 2, options);
                _soddenMetricValid[i] = _soddenRecorders[i].Valid;
                if (_soddenMetricValid[i]) _soddenMetricUnits[i] = _soddenRecorders[i].UnitType.ToString();
            }
        }
        void SampleSoddenProfile()
        {
            if (_soddenRecorders == null) return;
            if (_soddenMetricFrames < SoddenMetricCapacity)
            {
                for (int i = 0; i < _soddenRecorders.Length; i++)
                    if (_soddenMetricValid[i]) _soddenMetricSamples[i][_soddenMetricFrames] = _soddenRecorders[i].LastValue;
                _soddenMetricFrames++;
            }
            if (Time.realtimeSinceStartupAsDouble - _soddenMetricStarted >= 90) EndSoddenProfile();
        }
        void EndSoddenProfile()
        {
            if (_soddenRecorders == null) return;
            var rows = new List<object>();
            try
            {
                for (int i = 0; i < _soddenRecorders.Length; i++)
                {
                    var samples = _soddenMetricSamples[i].Take(_soddenMetricFrames).Skip(2).ToArray();
                    var sorted = samples.OrderBy(v => v).ToArray();
                    rows.Add(new { name = SoddenMetricNames[i], valid = _soddenMetricValid[i], unit = _soddenMetricUnits[i],
                        frames = samples.Length, max = sorted.Length == 0 ? 0 : sorted[sorted.Length - 1],
                        p95 = sorted.Length == 0 ? 0 : sorted[(int)((sorted.Length - 1) * .95)],
                        mean = samples.Length == 0 ? 0 : samples.Average(), samples });
                }
                _observations.Add(new { phase = "sodden-native-profiler", seconds = Time.realtimeSinceStartupAsDouble - _soddenMetricStarted,
                    capacityReached = _soddenMetricFrames == SoddenMetricCapacity, rows,
                    bound = "ProfilerRecorder latest completed sample, potentially the preceding frame. Editor gameplay includes paced input, AI, screenshots and audit IO. Missing markers are invalid, not zero. No pre-change baseline, isolated renderer attribution, GPU timing or standalone-build claim." });
            }
            finally
            {
                for (int i = 0; i < _soddenRecorders.Length; i++) _soddenRecorders[i].Dispose();
                _soddenRecorders = null; _soddenMetricSamples = null;
            }
        }
    }
}
