using System;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
#if UNITY_EDITOR
using Unity.Profiling;
#endif

namespace CavesOfOoo.Tests
{
    public sealed class SpreadHuntPhaseValidationTests
    {
        Zone zone;
        SpreadPredatorPart role;
        Func<Zone, bool> valid;
        static object allocationSink;
        [SetUp] public void Setup()
        {
            zone = new Zone("phase-validation");
            role = new SpreadPredatorPart { HomeX = 10, HomeY = 10, PursuitRemaining = 24, SearchRemaining = 6 };
            valid = (Func<Zone, bool>)typeof(SpreadPredatorPart)
                .GetMethod("ValidSavedBounds", BindingFlags.Instance | BindingFlags.NonPublic)
                .CreateDelegate(typeof(Func<Zone, bool>), role);
        }
        [TearDown] public void Cleanup() { allocationSink = null; }
        [TestCase(0, true)] [TestCase(1, true)] [TestCase(2, true)]
        [TestCase(3, true)] [TestCase(4, true)] [TestCase(5, true)]
        [TestCase(6, true)] [TestCase(7, true)] [TestCase(8, true)]
        [TestCase(-1, false)] [TestCase(9, false)]
        [TestCase(int.MinValue, false)] [TestCase(int.MaxValue, false)]
        public void SavedBoundsRecognizeEveryDefinedPhaseAndRejectMalformedValues(int phase, bool expected)
        {
            role.Phase = (SpreadHuntPhase)phase;
            Assert.AreEqual(expected, valid(zone));
            Assert.AreEqual(phase, (int)role.Phase); Assert.AreEqual(24, role.PursuitRemaining); Assert.AreEqual(6, role.SearchRemaining);
        }
        static long Measure(Action action)
        {
#if UNITY_EDITOR
            using (var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GC.Alloc", 32,
                ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
            {
                action(); recorder.Stop();
                if (!recorder.Valid) Assert.Inconclusive("Native allocation event counter unsupported, not zero.");
                Assert.False(recorder.WrappedAround); Assert.Less(recorder.Count, 32);
                long count = 0; for (int i = 0; i < recorder.Count; i++) count += recorder.GetSample(i).Count;
                return count;
            }
#else
            long before = GC.GetAllocatedBytesForCurrentThread(); action(); return GC.GetAllocatedBytesForCurrentThread() - before;
#endif
        }
        [Test] public void RepeatedBoundsValidationDoesNotAllocateOrBoxItsPhase()
        {
            Action empty = () => { }; Action positive = () => allocationSink = new byte[1024];
            Measure(positive); long zero = Measure(empty), allocated = Measure(positive);
#if UNITY_EDITOR
            if (zero != 0 || allocated != 1) Assert.Inconclusive("Native event counter failed empty/single calibration.");
#else
            if (zero != 0 || allocated < 1024) Assert.Inconclusive("Managed byte counter failed empty/positive calibration.");
#endif
            Action check = () => valid(zone);
            for (int i = 0; i < 64; i++) check();
            long worst = 0; for (int i = 0; i < 64; i++) worst = Math.Max(worst, Measure(check));
            TestContext.WriteLine("Repeated phase validation allocation amount=" + worst + " (native events / private bytes).");
            Assert.Zero(worst, "Validation of the finite saved phase must not allocate each time a hunter/grazer checks its current pair.");
        }
    }
}
