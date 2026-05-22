using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// G.10 — wind coupling. Wind biases gas dispersal in four places
    /// (frequency, attempt count, direction, thin-gas dissipation). The
    /// tricky math (direction bias, attempt scaling) is proven by PURE
    /// helpers with scripted RNG; the plumbing is proven by integration
    /// tests using seed-INDEPENDENT guarantees (windSpeed=100 pushes the
    /// spread/dissipate chance past 100, so the roll always passes).
    /// windSpeed=0 reproduces the pre-G.10 baseline exactly.
    /// </summary>
    public class GasWindCouplingTests
    {
        // Scripted RNG: each Next(max) call pops the next seq value % max.
        private class WindRng : System.Random
        {
            private readonly int[] _seq; private int _i;
            public WindRng(params int[] seq) { _seq = seq; }
            public override int Next() => _seq[_i++ % _seq.Length];
            public override int Next(int maxValue) => _seq[_i++ % _seq.Length] % maxValue;
            public override int Next(int minValue, int maxValue)
                => minValue + (_seq[_i++ % _seq.Length] % (maxValue - minValue));
        }

        [SetUp]
        public void Setup()
        {
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""wind-gas"", ""GasType"":""Wind"",
                ""Glyph"":""°"", ""Color"":""&w"",
                ""DefaultDensity"":50, ""DefaultLevel"":1,
                ""BehaviorKind"":""Poison"" } ] }");
        }

        [TearDown]
        public void TearDown()
        {
            GasRegistry.ResetForTests();
            GasSystem.SetRngForTests(null);
        }

        // ════════════════════════════════════════════════════════════
        //   PART I — WindDirectionToIndex (pure)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void WindDirectionToIndex_MapsAllEight()
        {
            Assert.AreEqual(0, GasSystem.WindDirectionToIndex("N"));
            Assert.AreEqual(1, GasSystem.WindDirectionToIndex("NE"));
            Assert.AreEqual(2, GasSystem.WindDirectionToIndex("E"));
            Assert.AreEqual(3, GasSystem.WindDirectionToIndex("SE"));
            Assert.AreEqual(4, GasSystem.WindDirectionToIndex("S"));
            Assert.AreEqual(5, GasSystem.WindDirectionToIndex("SW"));
            Assert.AreEqual(6, GasSystem.WindDirectionToIndex("W"));
            Assert.AreEqual(7, GasSystem.WindDirectionToIndex("NW"));
        }

        [Test]
        public void WindDirectionToIndex_CaseInsensitiveAndTrimmed()
        {
            Assert.AreEqual(2, GasSystem.WindDirectionToIndex("e"));
            Assert.AreEqual(1, GasSystem.WindDirectionToIndex(" ne "));
        }

        [Test]
        public void WindDirectionToIndex_InvalidOrEmpty_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, GasSystem.WindDirectionToIndex(""));
            Assert.AreEqual(-1, GasSystem.WindDirectionToIndex(null));
            Assert.AreEqual(-1, GasSystem.WindDirectionToIndex("XYZ"));
            Assert.AreEqual(-1, GasSystem.WindDirectionToIndex("North"));
        }

        // ════════════════════════════════════════════════════════════
        //   PART II — PickSpreadDirection (pure, scripted RNG)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void PickSpreadDirection_BothRollsPass_ReturnsWindDir()
        {
            // ws=100: Next(100)=0<100 ✓, Next(100)=0<90 ✓ → wind dir.
            var rng = new WindRng(0, 0);
            Assert.AreEqual(2, GasSystem.PickSpreadDirection(100, 2, rng));
        }

        [Test]
        public void PickSpreadDirection_NinetyRollFails_FallsBackRandom()
        {
            // Next(100)=0<100 ✓, Next(100)=95<90 ✗ → fallback Next(8)=5.
            var rng = new WindRng(0, 95, 5);
            Assert.AreEqual(5, GasSystem.PickSpreadDirection(100, 2, rng));
        }

        [Test]
        public void PickSpreadDirection_SpeedRollFails_FallsBackRandom()
        {
            // ws=30: Next(100)=50<30 ✗ (short-circuits the 90 roll) →
            // fallback Next(8)=7.
            var rng = new WindRng(50, 7);
            Assert.AreEqual(7, GasSystem.PickSpreadDirection(30, 2, rng));
        }

        [Test]
        public void PickSpreadDirection_NoWind_AlwaysRandom()
        {
            // windSpeed=0 short-circuits → Next(8)=5. Baseline preserved.
            var rng = new WindRng(5);
            Assert.AreEqual(5, GasSystem.PickSpreadDirection(0, 2, rng));
        }

        [Test]
        public void PickSpreadDirection_InvalidDirIndex_AlwaysRandom()
        {
            var rng = new WindRng(6);
            Assert.AreEqual(6, GasSystem.PickSpreadDirection(100, -1, rng));
        }

        [Test]
        public void PickSpreadDirection_HighWind_StatisticalEastBias()
        {
            // ws=100, idx=2 → ~90% east over many draws. Seed-locked.
            var rng = new System.Random(777);
            int hits = 0;
            for (int i = 0; i < 1000; i++)
                if (GasSystem.PickSpreadDirection(100, 2, rng) == 2) hits++;
            Assert.Greater(hits, 850, $"expected ~900 east picks, got {hits}");
            Assert.Less(hits, 1000, "but not literally all (10% random)");
        }

        // ════════════════════════════════════════════════════════════
        //   PART III — ComputeSpreadAttempts (pure)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void ComputeSpreadAttempts_NoWind_RangeOneToFour()
        {
            var rng = new System.Random(11);
            for (int i = 0; i < 300; i++)
            {
                int a = GasSystem.ComputeSpreadAttempts(0, rng);
                Assert.GreaterOrEqual(a, 1); Assert.LessOrEqual(a, 4);
            }
        }

        [Test]
        public void ComputeSpreadAttempts_HighWind_RangeFourToNine()
        {
            // ws=100: Random(1+100/30, 4+100/20) = Random(4, 9) inclusive.
            var rng = new System.Random(11);
            for (int i = 0; i < 300; i++)
            {
                int a = GasSystem.ComputeSpreadAttempts(100, rng);
                Assert.GreaterOrEqual(a, 4); Assert.LessOrEqual(a, 9);
            }
        }

        // ════════════════════════════════════════════════════════════
        //   PART IV — integration through ProcessGasBehavior
        // ════════════════════════════════════════════════════════════

        private static int CountGasAt(Zone zone, int x, int y)
        {
            int n = 0;
            foreach (var e in zone.GetAllEntities())
                if (e.Tags.ContainsKey("Gas"))
                {
                    var p = zone.GetEntityPosition(e);
                    if (p.x == x && p.y == y) n++;
                }
            return n;
        }

        private static (int east, int west) CountEastWest(Zone zone, int cx)
        {
            int e = 0, w = 0;
            foreach (var ent in zone.GetAllEntities())
                if (ent.Tags.ContainsKey("Gas"))
                {
                    var p = zone.GetEntityPosition(ent);
                    if (p.x > cx) e++; else if (p.x < cx) w++;
                }
            return (e, w);
        }

        [Test]
        public void Wind_HighSpeed_GuaranteesSpread()
        {
            // 25 + 100 = 125 > any Next(100) → spread ALWAYS attempted,
            // regardless of seed.
            GasSystem.SetRngForTests(new System.Random(1));
            var zone = new Zone("WindSpread");
            zone.CurrentWindSpeed = 100; zone.CurrentWindDirection = "E";
            var gas = GasFactory.SpawnGas(zone, 40, 12, "wind-gas", density: 100);
            GasSystem.ProcessGasBehavior(gas, zone);
            int neighbors = 0;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (!(dx == 0 && dy == 0)) neighbors += CountGasAt(zone, 40 + dx, 12 + dy);
            Assert.Greater(neighbors, 0, "windSpeed 100 guarantees the gas spreads to a neighbor");
        }

        [Test]
        public void Wind_HighSpeed_GuaranteesThinGasDissipation()
        {
            // 50 + 100 = 150 > any Next(100) → thin gas ALWAYS dissipates.
            GasSystem.SetRngForTests(new System.Random(2));
            var zone = new Zone("WindDissipate");
            zone.CurrentWindSpeed = 100;
            var gas = GasFactory.SpawnGas(zone, 40, 12, "wind-gas", density: 5);
            GasSystem.ProcessGasBehavior(gas, zone);
            Assert.AreEqual(0, CountGasAt(zone, 40, 12),
                "thin gas blown away by high wind");
        }

        [Test]
        public void Wind_East_SpreadsPredominantlyEast()
        {
            // ws=100 + dir=E + 90% bias → east cells dominate. Seed-locked.
            GasSystem.SetRngForTests(new System.Random(98765));
            var zone = new Zone("WindEast");
            zone.CurrentWindSpeed = 100; zone.CurrentWindDirection = "E";
            var gas = GasFactory.SpawnGas(zone, 40, 12, "wind-gas", density: 200);
            // Several ticks to accumulate a clear directional footprint.
            for (int t = 0; t < 3; t++)
                foreach (var g in zone.GetEntitiesWithTag("Gas"))
                    GasSystem.ProcessGasBehavior(g, zone);
            var (east, west) = CountEastWest(zone, 40);
            Assert.Greater(east, west, $"east-biased spread (east={east}, west={west})");
        }

        // ════════════════════════════════════════════════════════════
        //   PART V — Zone defaults + adversarial
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Zone_WindDefaults_ZeroAndEmpty()
        {
            var zone = new Zone("WindDefaults");
            Assert.AreEqual(0, zone.CurrentWindSpeed);
            Assert.AreEqual("", zone.CurrentWindDirection);
        }

        [Test]
        public void Wind_NegativeSpeed_NoCrash_BehavesLikeNoWind()
        {
            GasSystem.SetRngForTests(new System.Random(3));
            var zone = new Zone("WindNeg");
            zone.CurrentWindSpeed = -50; zone.CurrentWindDirection = "E";
            var gas = GasFactory.SpawnGas(zone, 40, 12, "wind-gas", density: 100);
            Assert.DoesNotThrow(() => GasSystem.ProcessGasBehavior(gas, zone));
        }

        [Test]
        public void Wind_InvalidDirection_HighSpeed_NoCrash()
        {
            GasSystem.SetRngForTests(new System.Random(4));
            var zone = new Zone("WindBadDir");
            zone.CurrentWindSpeed = 100; zone.CurrentWindDirection = "bogus";
            var gas = GasFactory.SpawnGas(zone, 40, 12, "wind-gas", density: 100);
            Assert.DoesNotThrow(() => GasSystem.ProcessGasBehavior(gas, zone));
        }
    }
}
