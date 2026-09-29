# Native counter probe

The root native job7ea91263dc7b4c65b32168f97d7056d6 showed GC.GetAllocatedBytesForCurrentThread returns0 even for a deliberate1024byteallocation. Its warmed-budget passes are invalid and retained.

Unity documents an immediate current-thread GC.Alloc ProfilerRecorder with explicit CollectOnlyOnCurrentThread options (no default per-frame aggregation): https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorderOptions.CollectOnlyOnCurrentThread.html .

This2-case diagnostic tries documented Internal and Memory categories, stops before reading, records validity/unit/overflow/sample values/count plus empty/1024/16384byte controls. Only a valid Bytes counter with positive controls and zero empty qualifies; all other results are explicitly inconclusive, not a pass. Full actual EditMode-reference compile succeeds. Root may copy this temporary file+meta, run2cases, preserve output, then remove it after selecting or declaring unsupported counter. No Unity calls by this agent.
