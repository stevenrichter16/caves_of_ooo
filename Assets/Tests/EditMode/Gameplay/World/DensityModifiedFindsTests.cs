using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    internal sealed class DensityFoundEnhancementScope : IDisposable
    {
        private const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
        private readonly Dictionary<string, Type> classes, names;
        private readonly bool initialized;
        private static Dictionary<string, Type> Registry(string name)
            => (Dictionary<string, Type>)typeof(EnhancementFactory).GetField(name, Flags).GetValue(null);
        public DensityFoundEnhancementScope()
        {
            classes = new Dictionary<string, Type>(Registry("_byClassName"));
            names = new Dictionary<string, Type>(Registry("_byDisplayName"));
            initialized = (bool)typeof(EnhancementFactory).GetField("_initialized", Flags).GetValue(null);
            EnhancementFactory.ForceReinitialize();
        }
        public void Dispose()
        {
            var byClass = Registry("_byClassName"); byClass.Clear();
            foreach (var pair in classes) byClass.Add(pair.Key, pair.Value);
            var byName = Registry("_byDisplayName"); byName.Clear();
            foreach (var pair in names) byName.Add(pair.Key, pair.Value);
            typeof(EnhancementFactory).GetField("_initialized", Flags).SetValue(null, initialized);
        }
    }

    public class DensityModifiedFindsTests
    {
        private DensityLootTestScope scope;
        private DensityFoundEnhancementScope enhancements;
        private static readonly string[] Pieces = { "TemperedLongSword", "CounterweightMaul", "FineRingMail", "RivetedPlate" };

        [SetUp] public void Setup() { scope = new DensityLootTestScope(); enhancements = new DensityFoundEnhancementScope(); }
        [TearDown] public void Cleanup() { enhancements.Dispose(); scope.Dispose(); }

        private Entity Stock(string table, Random rng)
        {
            var chest = scope.Factory.CreateEntity("LockedChest");
            LootStocker.StockContainer(chest, table, scope.Factory, rng);
            return chest;
        }

        [Test]
        public void ActualDeepStockingSometimesOffersOneCompatibleModifiedFind()
        {
            int modified = 0;
            var seen = new HashSet<string>();
            for (int seed = 0; seed < 128; seed++)
            {
                var chest = Stock("DeepReliquaryT4", new Random(seed));
                var item = chest.GetPart<ContainerPart>().Contents.Single(e => Pieces.Contains(e.BlueprintName));
                var enhancements = item.Parts.OfType<IItemEnhancement>().ToArray();
                Assert.LessOrEqual(enhancements.Length, 1);
                Assert.IsTrue(chest.GetPart<LockPart>().IsLocked);
                Assert.AreSame(chest, item.GetPart<PhysicsPart>().InInventory);
                foreach (var enhancement in enhancements)
                {
                    Assert.IsTrue(enhancement.Applicable(item));
                    Assert.AreEqual(1, enhancement.Tier);
                    Assert.That(item.GetPart<ExaminablePart>().BuildExamineLine(), Does.Contain(enhancement.GetEffectDescription()));
                }
                if (enhancements.Length == 1) { modified++; seen.Add(item.BlueprintName); }
            }
            Assert.That(modified, Is.InRange(16, 48), "A bounded minority of actual deep finds should be enhanced.");
            CollectionAssert.AreEquivalent(Pieces, seen);
        }

        [TestCase("SealedVaultT3")]
        [TestCase("CrateT3")]
        [TestCase("BasketT3")]
        [TestCase("HollowLogT3")]
        public void ExistingSourcesKeepTheirContentsAndRandomStream(string table)
        {
            for (int seed = 0; seed < 16; seed++)
            {
                var actual = new Random(seed); var expected = new Random(seed);
                var names = LootTableRegistry.Roll(table, expected);
                var chest = Stock(table, actual);
                Assert.IsFalse(chest.GetPart<ContainerPart>().Contents.Any(e => e.Parts.OfType<IItemEnhancement>().Any()));
                Assert.AreEqual(expected.Next(), actual.Next(), table + " must consume no decorator RNG.");
                Assert.IsTrue(chest.GetPart<ContainerPart>().Contents.All(e => names.Contains(e.BlueprintName)));
            }
        }

        [Test]
        public void DeepSourceReplaysItsFindsAndEnhancementKinds()
        {
            for (int seed = 0; seed < 32; seed++)
            {
                var a = new Random(seed); var b = new Random(seed);
                var first = Stock("DeepReliquaryT4", a); var second = Stock("DeepReliquaryT4", b);
                Func<Entity, string> signature = e => e.BlueprintName + ":" + string.Join(",", e.Parts.OfType<IItemEnhancement>().Select(p => p.Name + ":" + p.Tier));
                CollectionAssert.AreEqual(first.GetPart<ContainerPart>().Contents.Select(signature), second.GetPart<ContainerPart>().Contents.Select(signature));
                Assert.AreEqual(a.Next(), b.Next());
            }
        }
    }
}
