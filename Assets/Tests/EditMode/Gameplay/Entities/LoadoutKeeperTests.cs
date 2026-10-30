using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class LoadoutKeeperTests : FiftyWorldFixture
    {
        Entity Keeper(string blueprint = "Armorer")
        {
            var keeper = blueprint == "MorrowfastOrrit"
                ? MorrowfastContent.CreateResident("southwest-craftsperson", Factory)
                : Factory.CreateEntity(blueprint);
            Assert.NotNull(keeper);
            // Isolate permission tests from the session's faction reputation.
            keeper.GetPart<BrainPart>().SetPartyLeader(Actor);
            Assert.True(Zone.AddEntity(keeper, 11, 10));
            return keeper;
        }

        [TestCase("Armorer")]
        [TestCase("Weaponsmith")]
        [TestCase("MorrowfastOrrit")]
        public void AuthoredOutfittersOfferPreparationInTheWorldMenu(string blueprint)
        {
            var keeper = Keeper(blueprint);
            Assert.True(LoadoutKeeperPart.CanServe(Actor, keeper, out string reason), reason);
            Assert.AreEqual(1, WorldInteractionSystem.GatherActions(keeper, Actor)
                .Count(a => a.Command == LoadoutKeeperPart.Command));
        }

        [Test]
        public void PermissionExpiresWhenKeeperMovesDiesOrBecomesHostile()
        {
            var keeper = Keeper();
            Assert.True(LoadoutKeeperPart.CanServe(Actor, keeper, out _));
            Zone.RemoveEntity(keeper); Assert.True(Zone.AddEntity(keeper, 15, 10));
            Assert.False(LoadoutKeeperPart.CanServe(Actor, keeper, out _));
            Zone.RemoveEntity(keeper); Assert.True(Zone.AddEntity(keeper, 11, 10));
            keeper.GetPart<BrainPart>().SetPersonallyHostile(Actor, false);
            Assert.False(LoadoutKeeperPart.CanServe(Actor, keeper, out _));
            Assert.False(WorldInteractionSystem.GatherActions(keeper, Actor)
                .Any(a => a.Command == LoadoutKeeperPart.Command));
            keeper.GetPart<BrainPart>().PersonalEnemies.Clear();
            keeper.GetStat("Hitpoints").BaseValue = 0;
            Assert.False(LoadoutKeeperPart.CanServe(Actor, keeper, out _));
        }

        [Test]
        public void OrdinaryTradersAndAbsentProvidersCannotAuthorizePreparation()
        {
            var ordinary = Keeper("Villager");
            Assert.False(LoadoutKeeperPart.CanServe(Actor, ordinary, out _));
            Assert.False(LoadoutKeeperPart.CanServe(Actor, Actor, out _));
            Assert.False(LoadoutKeeperPart.CanServe(Actor, null, out _));
            var keeper = Keeper();
            Zone.RemoveEntity(keeper);
            Assert.False(LoadoutKeeperPart.CanServe(Actor, keeper, out _));
            Assert.False(LoadoutKeeperPart.CanServe(null, keeper, out _));
        }

        [TestCase("Armorer")]
        [TestCase("Weaponsmith")]
        [TestCase("MorrowfastOrrit")]
        public void OldSavedOutfittersGainServiceWithoutRestockingOrChangingMoney(string blueprint)
        {
            var keeper = Keeper(blueprint);
            keeper.RemovePart(keeper.GetPart<LoadoutKeeperPart>());
            var inv = keeper.GetPart<InventoryPart>();
            var item = Factory.CreateEntity("QuarryToothTrinket");
            Assert.True(inv.AddObject(item));
            TradeSystem.SetDrams(keeper, 37);
            var itemIds = inv.Objects.Select(i => i.ID).ToArray();
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(keeper);
            loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(loaded);
            Assert.AreEqual(1, loaded.Parts.OfType<LoadoutKeeperPart>().Count());
            Assert.AreEqual(37, TradeSystem.GetDrams(loaded));
            CollectionAssert.AreEquivalent(itemIds,
                loaded.GetPart<InventoryPart>().Objects.Select(i => i.ID));
        }
    }
}
