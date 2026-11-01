using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// EC1 (Docs/ECONOMY-SYSTEM.md §5.2): a trader generated with a zone stocks its shelf from the world
    /// seed and the zone, so the same world gives the same shops. Before EC1, <see cref="TraderPart"/>
    /// rolled its opening stock from an unseeded generator whenever nothing had set
    /// <see cref="TraderPart.Rng"/>.
    /// </summary>
    public sealed class TraderStockDeterminismTests
    {
        static EntityFactory factory;
        EntityFactory oldFactory; System.Random oldRng;

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            FactionManager.Initialize(Resources.Load<TextAsset>("Content/Data/Factions").text);
            LootTableRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/Loot").Select(t => t.text).ToList());
        }
        [SetUp] public void Setup() { oldFactory = TraderPart.Factory; oldRng = TraderPart.Rng; TraderPart.Factory = factory; TraderPart.Rng = null; }
        [TearDown] public void TearDown() { TraderPart.Factory = oldFactory; TraderPart.Rng = oldRng; }

        sealed class Place : IZoneBuilder
        {
            public string Name => "PlaceMerchants"; public int Priority => 4000;
            public bool BuildZone(Zone zone, EntityFactory f, System.Random rng)
            {
                for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) zone.AddEntity(f.CreateEntity("Grass"), x, y);
                for (int i = 0; i < 3; i++) zone.AddEntity(f.CreateEntity("Merchant"), 10 + i * 5, 10);
                return true;
            }
        }
        sealed class Market : ZoneManager
        {
            public Market(EntityFactory f, int seed) : base(f, seed) { }
            protected override ZoneGenerationPipeline GetPipelineForZone(string id) { var p = new ZoneGenerationPipeline(); p.AddBuilder(new Place()); return p; }
        }

        static string Shelves(Zone z) => string.Join(" | ", z.GetAllEntities().Where(e => e.BlueprintName == "Merchant")
            .OrderBy(e => z.GetEntityCell(e).X)
            .Select(e => string.Join(",", e.GetPart<InventoryPart>().Objects.Select(i => i.BlueprintName + "x" + (i.GetPart<StackerPart>()?.StackCount ?? 1)).OrderBy(s => s))));

        [Test] public void TheSameWorldGivesTheSameShops()
        {
            string a = Shelves(new Market(factory, 64).GetZone("Market.1"));
            string b = Shelves(new Market(factory, 64).GetZone("Market.1"));
            Assert.IsNotEmpty(a.Replace("|", "").Replace(" ", ""), "precondition: the merchants are stocked");
            Assert.AreEqual(a, b);
        }

        /// <summary>Counter-check: the seed really keys on the zone and the world, so shops differ.</summary>
        [Test] public void AnotherZoneOrWorldStocksItsOwnShelves()
        {
            string a = Shelves(new Market(factory, 64).GetZone("Market.1"));
            var others = new[] { Shelves(new Market(factory, 64).GetZone("Market.2")), Shelves(new Market(factory, 64).GetZone("Market.3")), Shelves(new Market(factory, 65).GetZone("Market.1")) };
            Assert.True(others.Any(o => o != a), "every other zone and world had exactly the same shelves");
        }

        /// <summary>A caller that sets its own generator (a fixture, a traveller) keeps it, and generation
        /// leaves no generator of its own behind for creation outside a zone.</summary>
        [Test] public void ACallersGeneratorIsRespectedAndNoneLeaksOut()
        {
            var mine = new System.Random(5);
            TraderPart.Rng = mine;
            new Market(factory, 64).GetZone("Market.1");
            Assert.AreSame(mine, TraderPart.Rng, "the caller's generator is restored");
            TraderPart.Rng = null;
            new Market(factory, 64).GetZone("Market.1");
            Assert.IsNull(TraderPart.Rng, "generation does not leak its generator");
        }
    }
}
