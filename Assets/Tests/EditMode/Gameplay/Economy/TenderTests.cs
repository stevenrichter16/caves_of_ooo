using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// EC6a (Docs/ECONOMY-SYSTEM.md §12.2): who takes what. Canon (Lore/History/08_MaterialCulture.md §II):
    /// the Concord underwrites drams; Tent-Right is oath-first and catacomb folk barter-first; drams are dead
    /// weight in the deep wasteland; the Choir sells nothing.
    /// </summary>
    public sealed class TenderTests
    {
        static EntityFactory factory;
        EntityFactory oldTrader, oldLoadout, oldConversation; System.Random oldRng;

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup()
        {
            FactionManager.Initialize(); PlayerReputation.Reset(); MessageLog.Clear(); Diag.ResetAll();
            LootTableRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/Loot").Select(t => t.text).ToList());
            oldTrader = TraderPart.Factory; oldLoadout = LoadoutPart.Factory; oldConversation = ConversationActions.Factory; oldRng = TraderPart.Rng;
            TraderPart.Factory = factory; TraderPart.Rng = new System.Random(64); LoadoutPart.Factory = null; ConversationActions.Factory = factory;
            ConversationLoader.Reset(); ConversationPredicates.Reset(); ConversationActions.Reset();
            ConversationManager.EndConversation(); ConversationManager.PendingTradePartner = null;
        }
        [TearDown] public void TearDown()
        {
            ConversationManager.EndConversation(); ConversationManager.PendingTradePartner = null; ConversationLoader.Reset();
            TraderPart.Factory = oldTrader; TraderPart.Rng = oldRng; LoadoutPart.Factory = oldLoadout; ConversationActions.Factory = oldConversation;
            FactionManager.Reset(); PlayerReputation.Reset(); MessageLog.Clear(); Diag.ResetAll();
        }

        /// <summary>Reference implementation: Chebyshev distance from a Beating cell to any other biome.</summary>
        static int Depth(int x, int y)
        {
            if (WorldMapAuthoring.BiomeAt(x, y) != BiomeType.Beating) return 0;
            int best = int.MaxValue;
            for (int xx = 0; xx < WorldMap.Width; xx++) for (int yy = 0; yy < WorldMap.Height; yy++)
                if (WorldMapAuthoring.BiomeAt(xx, yy) != BiomeType.Beating) best = System.Math.Min(best, System.Math.Max(System.Math.Abs(xx - x), System.Math.Abs(yy - y)));
            return best;
        }
        static (int x, int y) Cell(System.Func<int, int, bool> where)
        {
            for (int y = 0; y < WorldMap.Height; y++) for (int x = 0; x < WorldMap.Width; x++) if (where(x, y)) return (x, y);
            Assert.Fail("no such cell"); return (-1, -1);
        }
        static (int, int) Deep => Cell((x, y) => Tender.IsDeepWasteland(WorldMap.ToZoneID(x, y, 0)));
        static (int, int) Frontier => Cell((x, y) => WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Beating && Depth(x, y) == 1);
        static (int, int) Spread => Cell((x, y) => WorldMapAuthoring.BiomeAt(x, y) == BiomeType.Spread);

        static readonly Dictionary<Entity, Zone> Zones = new Dictionary<Entity, Zone>();
        static Entity At((int x, int y) cell, string blueprint, int z = 0)
        {
            var zone = new Zone(WorldMap.ToZoneID(cell.x, cell.y, z));
            var e = factory.CreateEntity(blueprint); zone.AddEntity(e, 10, 10); Zones[e] = zone;
            return e;
        }

        [Test] public void TheDeepWastelandIsTheBeatingsInterior()
        {
            var deep = new List<(int, int)>();
            for (int x = 0; x < WorldMap.Width; x++) for (int y = 0; y < WorldMap.Height; y++)
            {
                bool expected = Depth(x, y) >= Tender.DeepWastelandDepth;
                Assert.AreEqual(expected, Tender.IsDeepWasteland(WorldMap.ToZoneID(x, y, 0)), $"cell {x},{y}");
                if (expected) deep.Add((x, y));
            }
            Assert.AreEqual(52, deep.Count, "the authored map's deep wasteland");
            Assert.True(Tender.IsDeepWasteland(WorldMap.ToZoneID(18, 18, 0)), "the Last Counter stands in it");
            Assert.False(Tender.IsDeepWasteland(WorldMap.ToZoneID(8, 16, 0)), "Wellmeet stands on the frontier");
            Assert.False(Tender.IsDeepWasteland(WorldMap.ToZoneID(10, 14, 0)), "Tally is in the Spread");
            Assert.False(Tender.IsDeepWasteland(WorldMap.ToZoneID(18, 18, 1)), "underground is not the wasteland");
            Assert.False(Tender.IsDeepWasteland(null)); Assert.False(Tender.IsDeepWasteland("Sill.Interior.2")); Assert.False(Tender.IsDeepWasteland("Overworld.garbage"));
        }

        [Test] public void AMerchantInTheDeepWastelandBarters()
        {
            Assert.AreEqual(TenderKind.Barter, Tender.For(At(Deep, "Merchant")));
            Assert.AreEqual(TenderKind.Barter, Tender.For(At(Deep, "DesertHermit")), "the salt-walker too");
            Assert.AreEqual(TenderKind.Drams, Tender.For(At(Frontier, "Merchant")), "counter-check: the frontier still takes drams");
            Assert.AreEqual(TenderKind.Drams, Tender.For(At(Spread, "Merchant")), "counter-check: the Spread takes drams");
        }

        [Test] public void TheConcordHonoursDramsEvenInTheDeepWasteland()
        {
            Assert.AreEqual(TenderKind.Drams, Tender.For(At(Deep, "SaccharineEnvoy")));
        }

        [TestCase("TentRight")] [TestCase("CatacombFolk")]
        public void OathAndBarterFirstPeopleBarterEverywhere(string faction)
        {
            var trader = At(Spread, "Merchant"); trader.Tags["Faction"] = faction;
            Assert.AreEqual(TenderKind.Barter, Tender.For(trader));
        }

        [TestCase("ChoirTendril")] [TestCase("Grib")]
        public void TheChoirKeepsNoShopAnywhere(string blueprint)
        {
            Assert.AreEqual(TenderKind.None, Tender.For(At(Spread, blueprint)));
            Assert.AreEqual(TenderKind.None, Tender.For(At(Deep, blueprint)));
        }

        [Test] public void ATraderStandingNowhereTakesDrams()
        {
            Assert.AreEqual(TenderKind.Drams, Tender.For(factory.CreateEntity("Merchant")));
            Assert.AreEqual(TenderKind.Drams, Tender.For(null));
        }

        /// <summary>Drams are dead weight there: neither direct path moves goods or drams with a barter trader.</summary>
        [Test] public void ABarterTraderRefusesDramsBothWays()
        {
            var trader = At(Deep, "Merchant"); var player = factory.CreateEntity("Player");
            Zones[trader].AddEntity(player, 11, 10);
            TradeSystem.SetDrams(player, 500); int traderDrams = TradeSystem.GetDrams(trader);
            var stock = trader.GetPart<InventoryPart>(); var goods = factory.CreateEntity("Starapple"); stock.AddObject(goods);
            var mine = factory.CreateEntity("Dagger"); player.GetPart<InventoryPart>().AddObject(mine);

            Assert.False(TradeSystem.BuyFromTrader(player, trader, goods, out string buyReason));
            StringAssert.Contains("no drams", buyReason);
            Assert.False(TradeSystem.SellToTrader(player, trader, mine, out string sellReason));
            StringAssert.Contains("no drams", sellReason);
            Assert.AreEqual(500, TradeSystem.GetDrams(player)); Assert.AreEqual(traderDrams, TradeSystem.GetDrams(trader));
            Assert.True(stock.Contains(goods)); Assert.True(player.GetPart<InventoryPart>().Contains(mine));
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "trade", Kind = "BuyRejected", Limit = 5 }).Records.Count);

            var frontier = At(Frontier, "Merchant");
            var apple = factory.CreateEntity("Starapple"); frontier.GetPart<InventoryPart>().AddObject(apple);
            var buyer = factory.CreateEntity("Player"); Zones[frontier].AddEntity(buyer, 11, 10); TradeSystem.SetDrams(buyer, 500);
            Assert.True(TradeSystem.BuyFromTrader(buyer, frontier, apple, out _), "counter-check: a frontier merchant sells for drams");
        }

        // ════════════════ F1: the Choir's shrine keepers ════════════════

        static ConversationData Simple(string id)
        {
            var conv = new ConversationData { ID = id };
            conv.Nodes.Add(new NodeData { ID = "Start", Text = "So.", Choices = new List<ChoiceData> { new ChoiceData { Text = "Goodbye.", Target = "End" } } });
            return conv;
        }
        static bool Offers() => ConversationManager.VisibleChoices.Any(c => c.Actions != null && c.Actions.Any(a => a.Key == "OfferToChoir"));
        static bool Trades() => ConversationManager.VisibleChoices.Any(c => c.Actions != null && c.Actions.Any(a => a.Key == "StartTrade"));

        /// <summary>F1: EC3 closed the shrine keepers' shops without giving them the offering, so their shelves
        /// became unreachable. Any Choir speaker who holds something offers it.</summary>
        [Test] public void AShrineKeeperWithAShelfOffersTheOffering()
        {
            ConversationLoader.Register(Simple("Grib_1"));
            var grib = At(Spread, "Grib"); var player = factory.CreateEntity("Player"); Zones[grib].AddEntity(player, 11, 10);
            Assert.IsNotEmpty(grib.GetPart<InventoryPart>().Objects, "precondition: the keeper holds its shelf");
            Assert.True(ConversationManager.StartConversation(grib, player));
            Assert.True(Offers(), "the keeper offers what it holds");
            Assert.False(Trades(), "and still sells nothing");
            int index = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Actions != null && c.Actions.Any(a => a.Key == "OfferToChoir"));
            var held = grib.GetPart<InventoryPart>().Objects.Select(o => o.BlueprintName).OrderBy(n => n).ToArray();
            ConversationManager.SelectChoice(index);
            CollectionAssert.AreEquivalent(held, player.GetPart<InventoryPart>().Objects.Select(o => o.BlueprintName).ToArray(), "what it held, it gives");
            Assert.False(Offers(), "an emptied keeper offers nothing more");
        }

        /// <summary>Counter-checks: an empty keeper offers nothing; a merchant never offers; the tendril's authored
        /// offering is not doubled.</summary>
        [Test] public void TheOfferingNeverLiesAndIsNeverDoubled()
        {
            ConversationLoader.Register(Simple("Grib_1")); ConversationLoader.Register(Simple("Merchant_1"));
            var player = factory.CreateEntity("Player");
            var grib = At(Spread, "Grib"); grib.GetPart<InventoryPart>().Objects.Clear(); Zones[grib].AddEntity(player, 11, 10);
            Assert.True(ConversationManager.StartConversation(grib, player)); Assert.False(Offers(), "an empty shelf offers nothing");
            ConversationManager.EndConversation();
            var merchant = At(Spread, "Merchant");
            Assert.True(ConversationManager.StartConversation(merchant, player)); Assert.False(Offers()); Assert.True(Trades(), "counter-check: the merchant trades");
            ConversationManager.EndConversation();

            var tendrilConversation = Simple("ChoirTendril_1");
            tendrilConversation.Nodes[0].Choices.Insert(0, new ChoiceData { Text = "[Offer] Let the soil taste you.", Target = "End",
                Actions = new List<ConversationParam> { new ConversationParam { Key = "OfferToChoir", Value = "" } } });
            ConversationLoader.Register(tendrilConversation);
            var tendril = At(Spread, "ChoirTendril");
            Assert.True(ConversationManager.StartConversation(tendril, player));
            Assert.AreEqual(1, ConversationManager.VisibleChoices.Count(c => c.Actions != null && c.Actions.Any(a => a.Key == "OfferToChoir")), "one offering, not two");
        }
    }
}
