using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    /// <summary>Player-flow hypothesis: the same gifted parcel must survive
    /// transfer between both native compositions; proximity alone is no delivery.</summary>
    public sealed class WitnessIntakeCourierTests
    {
        [SetUp] public void NativeStock()=>CinderholdCompositionTests.LoadLoot();
        [TearDown] public void ResetStock()=>CavesOfOoo.Data.LootTableRegistry.ResetForTests();
        [TestCase(false)] [TestCase(true)]
        public void ActualSorterParcelReachesActualClerkAndOnlyCarriedCargoCanComplete(bool dropAtWindow)
        {
            DrownedLedgerCompositionTests.WithCourier((factory,ledger,player)=>
            {
                var witnesses=ledger.GetAllEntities().Where(e=>e.BlueprintName=="PreFellingBody").ToArray();
                DrownedLedgerCompositionTests.Accept(ledger.GetAllEntities().Single(e=>e.BlueprintName=="CurationSorter"),player);
                var inventory=player.GetPart<InventoryPart>();var parcel=inventory.Objects.Single(e=>e.BlueprintName=="SealedBogTakenBody");
                Assert.AreEqual(3,witnesses.Length);Assert.IsTrue(StoryletPart.Current.IsQuestActive("BogBodyCourier"));
                var intake=new OverworldZoneManager(factory,64).GetZone(MarrowstyeCompositionTests.Id);
                var clerk=intake.GetAllEntities().Single(e=>e.BlueprintName=="FilerClerk");var at=intake.GetEntityPosition(clerk);
                var approach=CinderholdCompositionTests.Neighbors(at.x,at.y).First(c=>intake.InBounds(c.x,c.y)&&!intake.GetCell(c.x,c.y).BlocksMovement());
                ledger.RemoveEntity(player);Assert.IsTrue(intake.AddEntity(player,approach.x,approach.y));SettlementRuntime.ActiveZone=intake;
                Assert.IsTrue(inventory.Objects.Contains(parcel));Assert.IsFalse(StoryletPart.Current.IsQuestCompleted("BogBodyCourier"));
                var catalog=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
                Assert.IsNull(SpawnRing3DRecipes.Resolve(ledger,parcel,catalog).ModelId);
                Assert.IsNull(SpawnRing3DRecipes.Resolve(intake,parcel,catalog).ModelId,"Carried items are not ground owners.");
                if(dropAtWindow)
                {
                    inventory.RemoveObject(parcel);Assert.IsTrue(intake.AddEntity(parcel,approach.x,approach.y));
                    StringAssert.StartsWith("drownedledger-parcel-",SpawnRing3DRecipes.Resolve(intake,parcel,catalog).ModelId);
                    ConversationManager.StartConversation(clerk,player);Assert.IsFalse(CinderholdCompositionTests.ChoiceVisible("[Deliver]"));ConversationManager.EndConversation();
                    Assert.AreEqual(0,TradeSystem.GetDrams(player));Assert.IsFalse(StoryletPart.Current.IsQuestCompleted("BogBodyCourier"));
                    intake.RemoveEntity(parcel);Assert.IsTrue(inventory.AddObject(parcel));
                }
                ConversationManager.StartConversation(clerk,player);Assert.IsTrue(CinderholdCompositionTests.Choose("[Deliver]"));ConversationManager.EndConversation();
                Assert.IsFalse(inventory.Objects.Contains(parcel));Assert.IsNull(intake.GetEntityCell(parcel));
                Assert.AreEqual(25,TradeSystem.GetDrams(player));Assert.AreEqual(10,PlayerReputation.Get("PaleCuration"));Assert.IsTrue(StoryletPart.Current.IsQuestCompleted("BogBodyCourier"));
                CollectionAssert.AreEquivalent(witnesses,ledger.GetAllEntities().Where(e=>e.BlueprintName=="PreFellingBody"));
                Assert.AreEqual(2,intake.GetAllEntities().Count(e=>e.BlueprintName=="SaltCuredBody"));Assert.AreEqual(2,intake.GetAllEntities().Count(e=>e.BlueprintName=="StoneCoffer"));
            });
        }
    }
}
