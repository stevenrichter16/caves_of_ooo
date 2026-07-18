using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// ReagentPart raw-string parsing + caching. The parser is the shared
    /// BrewPropertyAmount.ParseList grammar, so its robustness pins live
    /// here (name:potency entries, bare-name default, malformed drops).
    /// </summary>
    public class ReagentPartTests
    {
        [Test]
        public void Parse_BasicPairs()
        {
            var part = new ReagentPart { PropertiesRaw = "heat:2, volatile:1" };
            var props = part.GetProperties();

            Assert.AreEqual(2, props.Count);
            Assert.AreEqual("heat", props[0].Property);
            Assert.AreEqual(2, props[0].Potency);
            Assert.AreEqual("volatile", props[1].Property);
            Assert.AreEqual(1, props[1].Potency);
        }

        [Test]
        public void Parse_BareName_DefaultsToPotencyOne()
        {
            var part = new ReagentPart { PropertiesRaw = "combustible" };
            var props = part.GetProperties();

            Assert.AreEqual(1, props.Count);
            Assert.AreEqual("combustible", props[0].Property);
            Assert.AreEqual(1, props[0].Potency);
        }

        [Test]
        public void Parse_MalformedPotency_DropsEntry_NotZero()
        {
            // int.TryParse failure writes 0 to the out var — the parser must
            // DROP the entry, never treat the failed parse as potency 0/1.
            var part = new ReagentPart { PropertiesRaw = "heat:abc, cold:2" };
            var props = part.GetProperties();

            Assert.AreEqual(1, props.Count, "malformed entry must be dropped entirely.");
            Assert.AreEqual("cold", props[0].Property);
        }

        [Test]
        public void Parse_ZeroOrNegativePotency_Dropped()
        {
            var part = new ReagentPart { PropertiesRaw = "heat:0; cold:-3; conductive:1" };
            var props = part.GetProperties();

            Assert.AreEqual(1, props.Count);
            Assert.AreEqual("conductive", props[0].Property);
        }

        [Test]
        public void Parse_NamesAreLowercased()
        {
            var part = new ReagentPart { PropertiesRaw = "HEAT:2" };

            Assert.AreEqual("heat", part.GetProperties()[0].Property);
        }

        [Test]
        public void Parse_EmptyAndGarbage_YieldEmptyList_NoCrash()
        {
            Assert.AreEqual(0, new ReagentPart { PropertiesRaw = "" }.GetProperties().Count);
            Assert.AreEqual(0, new ReagentPart { PropertiesRaw = "   " }.GetProperties().Count);
            Assert.AreEqual(0, new ReagentPart { PropertiesRaw = ",,;;||" }.GetProperties().Count);
            Assert.AreEqual(0, new ReagentPart { PropertiesRaw = ":5, :" }.GetProperties().Count);
        }

        [Test]
        public void Cache_InvalidatesWhenRawChanges()
        {
            // Mirrors the MeleeWeaponPart.OnHitEffectsRaw snapshot-cache
            // contract: mutating the raw string must invalidate the parse.
            var part = new ReagentPart { PropertiesRaw = "heat:2" };
            Assert.AreEqual("heat", part.GetProperties()[0].Property);

            part.PropertiesRaw = "cold:3";
            var props = part.GetProperties();

            Assert.AreEqual(1, props.Count);
            Assert.AreEqual("cold", props[0].Property);
            Assert.AreEqual(3, props[0].Potency);
        }

        [Test]
        public void Cache_SameRaw_ReturnsSameListInstance()
        {
            // Counter-check to the invalidation test: an UNCHANGED raw string
            // must serve the cached list (no per-call re-parse allocation).
            var part = new ReagentPart { PropertiesRaw = "heat:2" };

            Assert.AreSame(part.GetProperties(), part.GetProperties());
        }
    }
}
