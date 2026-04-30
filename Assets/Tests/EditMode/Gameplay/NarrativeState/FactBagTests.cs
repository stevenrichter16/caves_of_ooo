using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests.EditMode.Gameplay.NarrativeState
{
    /// <summary>
    /// Unit tests for FactBag: the shared int-quality fact store.
    /// </summary>
    public class FactBagTests
    {
        // --- Get / Set / Clear ---

        [Test]
        public void Get_UnknownKey_ReturnsZero()
        {
            var bag = new FactBag();
            Assert.AreEqual(0, bag.Get("anything"));
        }

        [Test]
        public void Set_ThenGet_ReturnsSameValue()
        {
            var bag = new FactBag();
            bag.Set("score", 5);
            Assert.AreEqual(5, bag.Get("score"));
        }

        [Test]
        public void Set_OverwritesPreviousValue()
        {
            var bag = new FactBag();
            bag.Set("x", 3);
            bag.Set("x", 7);
            Assert.AreEqual(7, bag.Get("x"));
        }

        [Test]
        public void Clear_RemovesKey()
        {
            var bag = new FactBag();
            bag.Set("x", 9);
            bag.Clear("x");
            Assert.AreEqual(0, bag.Get("x"));
        }

        [Test]
        public void Clear_NonExistentKey_DoesNotThrow()
        {
            var bag = new FactBag();
            Assert.DoesNotThrow(() => bag.Clear("nope"));
        }

        [Test]
        public void Add_IncrementsFact()
        {
            var bag = new FactBag();
            bag.Set("count", 3);
            bag.Add("count", 2);
            Assert.AreEqual(5, bag.Get("count"));
        }

        [Test]
        public void Add_ToMissingKey_TreatsBaseAsZero()
        {
            var bag = new FactBag();
            bag.Add("fresh", 4);
            Assert.AreEqual(4, bag.Get("fresh"));
        }

        [Test]
        public void Add_NegativeDelta_Decrements()
        {
            var bag = new FactBag();
            bag.Set("hp", 10);
            bag.Add("hp", -3);
            Assert.AreEqual(7, bag.Get("hp"));
        }

        // --- Has ---

        [Test]
        public void Has_ExistingNonZeroKey_ReturnsTrue()
        {
            var bag = new FactBag();
            bag.Set("flag", 1);
            Assert.IsTrue(bag.Has("flag"));
        }

        [Test]
        public void Has_MissingKey_ReturnsFalse()
        {
            var bag = new FactBag();
            Assert.IsFalse(bag.Has("flag"));
        }

        [Test]
        public void Has_KeySetToZero_ReturnsFalse()
        {
            var bag = new FactBag();
            bag.Set("flag", 0);
            Assert.IsFalse(bag.Has("flag"));
        }

        // --- Multiple keys are independent ---

        [Test]
        public void MultipleKeys_AreIndependent()
        {
            var bag = new FactBag();
            bag.Set("a", 1);
            bag.Set("b", 2);
            Assert.AreEqual(1, bag.Get("a"));
            Assert.AreEqual(2, bag.Get("b"));
        }
    }
}
