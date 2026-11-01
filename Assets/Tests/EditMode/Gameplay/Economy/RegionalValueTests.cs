using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// EC5 (Docs/ECONOMY-SYSTEM.md §5.2): salt is cheap near the flats and precious far from them
    /// (Lore/History/08_MaterialCulture.md §II), so carrying it inland pays.
    /// </summary>
    public sealed class RegionalValueTests
    {
        static EntityFactory factory;
        EntityFactory oldTrader, oldLoadout;

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup()
        {
            FactionManager.Initialize(); PlayerReputation.Reset();
            oldTrader = TraderPart.Factory; oldLoadout = LoadoutPart.Factory; TraderPart.Factory = null; LoadoutPart.Factory = null;
        }
        [TearDown] public void TearDown() { TraderPart.Factory = oldTrader; LoadoutPart.Factory = oldLoadout; FactionManager.Reset(); PlayerReputation.Reset(); }

        static (int x, int y) Cell(System.Func<int, bool> distance)
        {
            for (int x = 0; x < WorldMap.Width; x++) for (int y = 0; y < WorldMap.Height; y++)
                if (distance(RegionalValue.DistanceToFlats(WorldMap.ToZoneID(x, y, 0)))) return (x, y);
            Assert.Fail("no such cell"); return (-1, -1);
        }
        static Entity TraderAt((int x, int y) cell)
        {
            var zone = new Zone(WorldMap.ToZoneID(cell.x, cell.y, 0));
            var trader = factory.CreateEntity("Merchant"); zone.AddEntity(trader, 10, 10);
            return trader;
        }

        [Test] public void SaltIsCheapNearTheFlatsAndPreciousFarFromThem()
        {
            var near = TraderAt(Cell(d => d == 0)); var far = TraderAt(Cell(d => d >= 10));
            var salt = factory.CreateEntity("PaleSalt");
            Assert.True(salt.HasTag(RegionalValue.SaltTag), "precondition: pale salt is trade salt");
            Assert.Less(TradeSystem.GetBuyPrice(salt, 0.6, near), TradeSystem.GetBuyPrice(salt, 0.6, far));
            Assert.Less(TradeSystem.GetSellPrice(salt, 0.6, near), TradeSystem.GetSellPrice(salt, 0.6, far));
            Assert.AreEqual(RegionalValue.Near, RegionalValue.Modifier(salt, near));
            Assert.AreEqual(RegionalValue.Far, RegionalValue.Modifier(salt, far));
        }

        /// <summary>Counter-check: other goods cost the same everywhere.</summary>
        [Test] public void OtherGoodsKeepOnePrice()
        {
            var near = TraderAt(Cell(d => d == 0)); var far = TraderAt(Cell(d => d >= 10));
            var apples = factory.CreateEntity("Starapple");
            Assert.AreEqual(TradeSystem.GetBuyPrice(apples, 0.6, near), TradeSystem.GetBuyPrice(apples, 0.6, far));
            Assert.AreEqual(TradeSystem.GetSellPrice(apples, 0.6, near), TradeSystem.GetSellPrice(apples, 0.6, far));
            Assert.AreEqual(1.0, RegionalValue.Modifier(apples, far));
        }

        /// <summary>A shrewd trader (high Ego) profits carrying salt inland; nobody profits buying and selling
        /// it in the same place.</summary>
        [Test] public void CarryingSaltInlandPaysAndStandingStillNever()
        {
            var near = TraderAt(Cell(d => d == 0)); var far = TraderAt(Cell(d => d >= 10));
            var salt = factory.CreateEntity("PaleSalt");
            Assert.Greater(TradeSystem.GetSellPrice(salt, 0.95, far), TradeSystem.GetBuyPrice(salt, 0.95, near), "the caravan's margin");
            foreach (var trader in new[] { near, far })
                foreach (double perf in new[] { 0.05, 0.35, 0.95 })
                    Assert.Greater(TradeSystem.GetBuyPrice(salt, perf, trader), TradeSystem.GetSellPrice(salt, perf, trader), "no loop in one place");
        }

        [Test] public void TheFactorIsBoundedAndNeverFallsWithDistance()
        {
            double last = 0;
            for (int d = 0; d <= 30; d++)
            {
                double f = RegionalValue.Factor(d);
                Assert.That(f, Is.InRange(RegionalValue.Near, RegionalValue.Far));
                Assert.GreaterOrEqual(f, last); last = f;
            }
            Assert.AreEqual(RegionalValue.Near, RegionalValue.Factor(-3), "a negative distance is the flats");
        }

        /// <summary>A trader standing nowhere on the surface map, or nowhere at all, keeps the neutral price.</summary>
        [Test] public void ATraderOffTheMapKeepsTheNeutralPrice()
        {
            var salt = factory.CreateEntity("PaleSalt");
            var loose = factory.CreateEntity("Merchant");
            Assert.AreEqual(1.0, RegionalValue.Modifier(salt, loose));
            Assert.AreEqual(1.0, RegionalValue.Modifier(salt, null));
            var inside = new Zone("Sill.Interior.2"); var keeper = factory.CreateEntity("Merchant"); inside.AddEntity(keeper, 5, 5);
            Assert.AreEqual(1.0, RegionalValue.Modifier(salt, keeper));
            Assert.AreEqual(-1, RegionalValue.DistanceToFlats(null));
            Assert.AreEqual(-1, RegionalValue.DistanceToFlats("Overworld.garbage"));
        }

        /// <summary>The world really has salt pans, and the distance map spans near and far.</summary>
        [Test] public void TheWorldHasFlatsAndHinterland()
        {
            var distances = Enumerable.Range(0, WorldMap.Width).SelectMany(x => Enumerable.Range(0, WorldMap.Height)
                .Select(y => RegionalValue.DistanceToFlats(WorldMap.ToZoneID(x, y, 0)))).ToList();
            Assert.Contains(0, distances, "salt pans exist");
            Assert.GreaterOrEqual(distances.Max(), 10, "the far country is far");
        }
    }
}
