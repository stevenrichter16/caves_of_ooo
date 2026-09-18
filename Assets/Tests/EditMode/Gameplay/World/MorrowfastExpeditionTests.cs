using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class MorrowfastExpeditionTests
    {
        private HotbarSaveFixture scope;
        private EntityFactory factory;
        private OverworldZoneManager manager;
        private Zone field, town;
        private Entity player, farra;

        [SetUp] public void Setup()
        {
            scope = new HotbarSaveFixture(false, false);
            factory = GrovelandsCompositionTests.Factory();
            manager = OverworldZoneManager.CreateDetached(factory, 64);
            field = manager.GetZone("Overworld.2.6.0");
            town = manager.GetZone(MorrowfastSceneRuntime.ZoneID);
            player = factory.CreateEntity("Player");
            farra = MorrowfastSceneRuntime.FindOwner(town, "farra-sprig");
            town.AddEntity(player, 36, 14);
            StoryletPart.Current = new StoryletPart(); StoryletPart.LocalPlayer = player;
            SettlementRuntime.ActiveZone = town;
            MorrowfastContent.EnsureRegistered();
        }
        [TearDown] public void Teardown() { ConversationManager.EndConversation(); scope.Dispose(); }

        private Entity Cache => field.GetAllEntities().Single(e => e.GetIntProperty("MorrowfastDryGoodsCache") == 1);
        private Entity TakeParcel()
        {
            var container = Cache.GetPart<ContainerPart>();
            var parcel = container.Contents.Single(e => e.GetIntProperty("MorrowfastDryGoodsParcel") == 1);
            Assert.IsTrue(container.RemoveItem(parcel)); Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(parcel));
            return parcel;
        }

        [Test] public void FreshWesternChunkContainsOneRealRecoverableCacheWithoutAcceptingQuest()
        {
            Assert.AreEqual(1, field.GetAllEntities().Count(e => e.GetIntProperty("MorrowfastDryGoodsCache") == 1));
            Assert.AreEqual("WovenBasket", Cache.BlueprintName);
            Assert.AreEqual(1, Cache.GetPart<ContainerPart>().Contents.Count);
            Assert.IsFalse(StoryletPart.Current.IsQuestActive("MorrowfastDryGoods"));
            Assert.IsFalse(manager.GetZone("Overworld.2.5.0").GetAllEntities().Any(e => e.GetIntProperty("MorrowfastDryGoodsCache") == 1));
        }
        [Test] public void RemovingOrLootingCacheIsNotUndoneOnOrdinaryReentry()
        {
            var cache = Cache; TakeParcel();
            Assert.IsEmpty(manager.GetZone(field.ZoneID).GetAllEntities().Single(e=>e==cache).GetPart<ContainerPart>().Contents);
            field.RemoveEntity(cache);
            Assert.IsFalse(manager.GetZone(field.ZoneID).GetAllEntities().Any(e=>e.GetIntProperty("MorrowfastDryGoodsCache")==1));
        }
        [Test] public void JournalDefinitionGivesActualWesternDestinationAndReturnContact()
        {
            var q = StoryletRegistry.FindQuest("MorrowfastDryGoods"); Assert.NotNull(q);
            string text = string.Join(" ", q.Stages.SelectMany(s=>s.Objectives).Select(o=>o.Text));
            StringAssert.Contains("west", text.ToLowerInvariant()); StringAssert.Contains("Farra", text);
        }
        [Test] public void ActualFarraConversationOffersRegisteredExpeditionActions()
        {
            Assert.IsTrue(ConversationManager.StartConversation(farra, player));
            Assert.IsTrue(ConversationManager.VisibleChoices.Any(c=>c.Target=="DryGoods"));
            var node=ConversationManager.CurrentConversation.GetNode("DryGoods"); Assert.NotNull(node);
            Assert.IsTrue(node.Choices.Any(c=>c.Actions.Any(a=>a.Key=="MorrowfastExpedition"&&a.Value=="accept")));
            Assert.IsTrue(ConversationActions.IsRegistered("MorrowfastExpedition"));
        }
        [Test] public void AcceptRetrieveAndReturnCompletesOnceAndLeavesVisibleSupplies()
        {
            Assert.IsTrue(MorrowfastExpedition.TryConversation(farra,player,"accept"));
            Assert.IsFalse(MorrowfastExpedition.TryConversation(farra,player,"deliver"));
            var parcel=TakeParcel(); int before=TradeSystem.GetDrams(player);
            Assert.IsTrue(MorrowfastExpedition.TryConversation(farra,player,"deliver"));
            Assert.IsTrue(StoryletPart.Current.IsQuestCompleted("MorrowfastDryGoods"));
            Assert.AreEqual(before+15,TradeSystem.GetDrams(player));
            Assert.IsFalse(player.GetPart<InventoryPart>().Objects.Contains(parcel));
            Assert.IsTrue(town.GetAllEntities().Contains(parcel),"Returned cloth remains visible in the guesthouse.");
            Assert.AreEqual(1,parcel.GetIntProperty("MorrowfastDryGoodsReturned"));
            Assert.IsTrue(player.GetPart<InventoryPart>().Objects.Any(e=>e.BlueprintName=="FireClay"));
            Assert.IsFalse(MorrowfastExpedition.TryConversation(farra,player,"deliver"));
            Assert.AreEqual(before+15,TradeSystem.GetDrams(player));
        }
        [Test] public void OrdinarySackDoesNotSubstituteForActualRecoveredParcel()
        {
            Assert.IsTrue(MorrowfastExpedition.TryConversation(farra,player,"accept"));
            player.GetPart<InventoryPart>().AddObject(factory.CreateEntity("Sack"));
            Assert.IsFalse(MorrowfastExpedition.TryConversation(farra,player,"deliver"));
            Assert.IsFalse(StoryletPart.Current.IsQuestCompleted("MorrowfastDryGoods"));
        }
        [Test] public void RecoveryBeforeAcceptanceRemainsValid()
        { TakeParcel(); Assert.IsTrue(MorrowfastExpedition.TryConversation(farra,player,"accept")); Assert.IsTrue(MorrowfastExpedition.TryConversation(farra,player,"deliver")); }
        [Test] public void WrongResidentAndRemoteConversationCannotChangeProgress()
        {
            var other=MorrowfastSceneRuntime.FindOwner(town,"north-guard-west");
            Assert.IsFalse(MorrowfastExpedition.TryConversation(other,player,"accept"));
            town.MoveEntity(player,2,2);
            Assert.IsFalse(MorrowfastExpedition.TryConversation(farra,player,"accept"));
            Assert.AreEqual(0,StoryletPart.Current.GetActiveQuests().Count);
        }
        [Test] public void DecliningAcceptedWorkDoesNotCompleteRewardOrRemoveGuestBeds()
        {
            Assert.IsTrue(MorrowfastExpedition.TryConversation(farra,player,"accept"));int before=TradeSystem.GetDrams(player);
            Assert.IsTrue(MorrowfastExpedition.TryConversation(farra,player,"release"));
            Assert.IsFalse(StoryletPart.Current.IsQuestActive("MorrowfastDryGoods"));
            Assert.IsFalse(StoryletPart.Current.IsQuestCompleted("MorrowfastDryGoods"));
            Assert.AreEqual(before,TradeSystem.GetDrams(player));
            Assert.NotNull(MorrowfastSceneRuntime.FindOwner(town,"guest-bed-west"));
        }
    }
}
