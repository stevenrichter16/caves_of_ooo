using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class DensityEnhancementFixtureIsolationTests
    {
        private const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
        private Dictionary<string, Type> outerClasses, outerNames;
        private bool outerInitialized;
        private static Dictionary<string, Type> Registry(string field)
            => (Dictionary<string, Type>)typeof(EnhancementFactory).GetField(field, Flags).GetValue(null);
        private static bool Initialized
        {
            get => (bool)typeof(EnhancementFactory).GetField("_initialized", Flags).GetValue(null);
            set => typeof(EnhancementFactory).GetField("_initialized", Flags).SetValue(null, value);
        }
        [SetUp] public void RememberOuterRegistry()
        {
            outerClasses = new Dictionary<string, Type>(Registry("_byClassName"));
            outerNames = new Dictionary<string, Type>(Registry("_byDisplayName"));
            outerInitialized = Initialized;
        }
        [TearDown] public void RestoreOuterRegistry()
        {
            Restore("_byClassName", outerClasses); Restore("_byDisplayName", outerNames); Initialized = outerInitialized;
        }
        private static void Restore(string field, Dictionary<string, Type> before)
        {
            var registry = Registry(field); registry.Clear(); foreach (var row in before) registry.Add(row.Key, row.Value);
        }
        private static void RunActualTearDown(ItemEnhancingTests fixture)
        {
            foreach (var method in typeof(ItemEnhancingTests).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttributes(typeof(TearDownAttribute), true).Length > 0)) method.Invoke(fixture, null);
        }
        [TestCase(false, false)] [TestCase(true, false)] [TestCase(false, true)] [TestCase(true, true)]
        public void ExistingFixtureRestoresExactBorrowedMapsAndInitializationEvenWhenBodyFails(bool initialized, bool bodyFails)
        {
            // Deliberately retain a partial registry. Resetting to all production
            // types would conceal the leak but still violate borrowed ownership.
            EnhancementFactory.ResetForTests();
            EnhancementFactory.Register(typeof(EnhancementGlowQuartz)); EnhancementFactory.Register(typeof(EnhancementPaleSalt));
            Initialized = initialized;
            var expectedClasses = new Dictionary<string, Type>(Registry("_byClassName"));
            var expectedNames = new Dictionary<string, Type>(Registry("_byDisplayName"));
            var actualClassMap = Registry("_byClassName"); var actualNameMap = Registry("_byDisplayName");
            var fixture = new ItemEnhancingTests();
            try
            {
                fixture.Setup();
                Assert.AreEqual(4, Registry("_byClassName").Count);
                Assert.True(Registry("_byClassName").Values.All(t => t.DeclaringType == typeof(ItemEnhancingTests)));
                if (bodyFails) throw new InvalidOperationException("Simulated failed test body.");
            }
            catch (InvalidOperationException error) { Assert.True(bodyFails); Assert.AreEqual("Simulated failed test body.", error.Message); }
            finally { RunActualTearDown(fixture); }
            Assert.AreEqual(initialized, Initialized, "No-domain-reload Play must keep its borrowed initialized state.");
            Assert.AreSame(actualClassMap, Registry("_byClassName")); Assert.AreSame(actualNameMap, Registry("_byDisplayName"));
            CollectionAssert.AreEquivalent(expectedClasses, Registry("_byClassName"));
            CollectionAssert.AreEquivalent(expectedNames, Registry("_byDisplayName"));
        }
        [Test] public void ActualFixtureStillUsesOnlyItsFourIsolatedStubsDuringTheBody()
        {
            var fixture = new ItemEnhancingTests();
            try
            {
                fixture.Setup();
                Assert.True(Initialized);
                CollectionAssert.AreEquivalent(new[] { typeof(ItemEnhancingTests.StubA), typeof(ItemEnhancingTests.StubB),
                    typeof(ItemEnhancingTests.StubC), typeof(ItemEnhancingTests.StubRejecter) }, Registry("_byClassName").Values);
            }
            finally { RunActualTearDown(fixture); }
        }
        [Test] public void TeardownBeforeSetupLeavesTheExistingRegistryAlone()
        {
            var classes = new Dictionary<string, Type>(Registry("_byClassName"));
            var names = new Dictionary<string, Type>(Registry("_byDisplayName")); bool initialized = Initialized;
            RunActualTearDown(new ItemEnhancingTests());
            CollectionAssert.AreEquivalent(classes, Registry("_byClassName")); CollectionAssert.AreEquivalent(names, Registry("_byDisplayName"));
            Assert.AreEqual(initialized, Initialized);
        }
    }
}
