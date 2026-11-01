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
    /// EC3 (Docs/ECONOMY-SYSTEM.md §9): the Choir does not trade. A tendril keeps its renewing shelf, but
    /// nothing on it is for sale; a walker who lets it taste their blood receives what it holds. Canon:
    /// "you cannot buy anything from the Choir — you can only give yourself" (Lore/History/08_MaterialCulture.md §II).
    /// </summary>
    public sealed class ChoirOfferingTests
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
        }
        [TearDown] public void TearDown()
        {
            TraderPart.Factory = oldTrader; TraderPart.Rng = oldRng; LoadoutPart.Factory = oldLoadout; ConversationActions.Factory = oldConversation;
            FactionManager.Reset(); PlayerReputation.Reset(); MessageLog.Clear(); Diag.ResetAll();
        }

        static Entity Walker(int hp = 30)
        {
            var w = factory.CreateEntity("Player");
            var stat = w.GetStat("Hitpoints"); stat.Max = 30; stat.BaseValue = hp;
            return w;
        }
        static string[] Held(Entity e) => e.GetPart<InventoryPart>().Objects.Select(o => o.BlueprintName).OrderBy(n => n).ToArray();

        [Test] public void TheChoirKeepsAShelfButSellsNothing()
        {
            var tendril = factory.CreateEntity("ChoirTendril"); var walker = Walker();
            Assert.NotNull(tendril.GetPart<TraderPart>(), "the renewing shelf stays (cathedral, restock and art rely on it)");
            Assert.IsNotEmpty(Held(tendril), "precondition: the tendril holds its stock");
            Assert.False(ConversationManager.CanTrade(tendril), "no [Let's trade.] with the Choir");
            var item = tendril.GetPart<InventoryPart>().Objects[0];
            Assert.False(TradeSystem.CanBeTraded(item, walker, tendril, "Buy"), "nothing is bought from the Choir");
            Assert.False(TradeSystem.CanBeTraded(factory.CreateEntity("Starapple"), walker, tendril, "Sell"), "nothing is sold to it");
            var merchant = factory.CreateEntity("Merchant");
            Assert.True(ConversationManager.CanTrade(merchant), "counter-check: a merchant still trades");
            Assert.True(TradeSystem.CanBeTraded(factory.CreateEntity("Starapple"), walker, merchant, "Sell"));
        }

        [Test] public void AnOfferingCostsBloodAndGivesWhatTheTendrilHolds()
        {
            var tendril = factory.CreateEntity("ChoirTendril"); var walker = Walker();
            var held = Held(tendril);
            int rep = PlayerReputation.Get(ChoirOffering.Faction);
            Assert.IsNull(ConversationActions.ExecuteRequired("OfferToChoir", tendril, walker, ""));
            Assert.AreEqual(6, ChoirOffering.Cost(walker), "a fifth of thirty");
            Assert.AreEqual(24, walker.GetStat("Hitpoints").Value);
            CollectionAssert.AreEquivalent(held, Held(walker), "what it held, it gives");
            CollectionAssert.Contains(Held(walker), "Codex11", "the Choir's codex is given, not sold");
            CollectionAssert.IsEmpty(Held(tendril));
            Assert.AreEqual(rep + ChoirOffering.Regard, PlayerReputation.Get(ChoirOffering.Faction));
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "trade", Kind = "ChoirOffering", Limit = 5 }).Records.Count);
        }

        /// <summary>An emptied tendril asks nothing: a second offering costs no blood until its shelf renews.</summary>
        [Test] public void AnEmptyTendrilAsksNothing()
        {
            var tendril = factory.CreateEntity("ChoirTendril"); var walker = Walker();
            ConversationActions.ExecuteRequired("OfferToChoir", tendril, walker, "");
            int hp = walker.GetStat("Hitpoints").Value, rep = PlayerReputation.Get(ChoirOffering.Faction);
            Assert.AreEqual("nothing_to_give", ConversationActions.ExecuteRequired("OfferToChoir", tendril, walker, ""));
            Assert.AreEqual(hp, walker.GetStat("Hitpoints").Value);
            Assert.AreEqual(rep, PlayerReputation.Get(ChoirOffering.Faction), "no regard for an empty gesture");
            tendril.GetPart<InventoryPart>().AddObject(factory.CreateEntity("InkVial"));
            Assert.IsNull(ConversationActions.ExecuteRequired("OfferToChoir", tendril, walker, ""), "counter-check: a renewed shelf gives again");
        }

        /// <summary>The offering never kills, and a walker with too little left changes nothing.</summary>
        [TestCase(6)] [TestCase(1)]
        public void AWalkerTooWeakToGiveChangesNothing(int hp)
        {
            var tendril = factory.CreateEntity("ChoirTendril"); var walker = Walker(hp);
            var held = Held(tendril); int rep = PlayerReputation.Get(ChoirOffering.Faction);
            Assert.AreEqual("too_weak", ConversationActions.ExecuteRequired("OfferToChoir", tendril, walker, ""));
            Assert.AreEqual(hp, walker.GetStat("Hitpoints").Value);
            Assert.IsEmpty(Held(walker));
            CollectionAssert.AreEqual(held, Held(tendril));
            Assert.AreEqual(rep, PlayerReputation.Get(ChoirOffering.Faction));
            Assert.IsNull(ConversationActions.ExecuteRequired("OfferToChoir", tendril, Walker(7), ""), "counter-check: one more point is enough");
        }

        [Test] public void TheConversationOffersTheOffering()
        {
            var conversation = ConversationLoader.Get("ChoirTendril_1");
            Assert.NotNull(conversation);
            var node = conversation.Nodes.Single(n => n.ID == "NoTrade");
            Assert.True(node.Choices.Any(c => c.Actions != null && c.Actions.Any(a => a.Key == "OfferToChoir")));
            Assert.True(conversation.Nodes.Single(n => n.ID == "Start").Choices.Any(c => c.Target == "NoTrade"));
        }
    }
}
