using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    [TestFixture]
    public class MerchantSpareKeyAdversarialTests : MerchantSpareKeyFixture
    {
        [TestCase("IronKey", "buy")] [TestCase("StillleafKey", "buy")]
        [TestCase("IronKey", "sell")] [TestCase("StillleafKey", "sell")]
        public void ProtectedKeysRemainUntradeable(string blueprint, string direction)
        {
            var trader = Trader(out var key, blueprint); var actor = Actor();
            Assert.True(key.HasTag("NoTrade"));
            if (direction == "sell") { trader.GetPart<InventoryPart>().RemoveObject(key); actor.GetPart<InventoryPart>().AddObject(key); }
            bool success = direction == "buy" ? TradeSystem.BuyFromTrader(actor, trader, key) : TradeSystem.SellToTrader(actor, trader, key);
            Assert.False(success); Assert.AreEqual(1000, TradeSystem.GetDrams(actor)); Assert.AreEqual(1000, TradeSystem.GetDrams(trader));
            Assert.Contains(key, (direction == "buy" ? trader : actor).GetPart<InventoryPart>().Objects);
        }
        [TestCase("poor")] [TestCase("full")] [TestCase("stale")] [TestCase("self")]
        [TestCase("missingBuyer")] [TestCase("missingTrader")] [TestCase("missingKey")] [TestCase("cancel")]
        public void FailedPurchaseDoesNotMoveMoneyOrKey(string fault)
        {
            var trader = Trader(out var key); var buyer = Actor(fault == "poor" ? 0 : 1000);
            var inventory = buyer.GetPart<InventoryPart>();
            if (fault == "full") inventory.MaxWeight = 0;
            if (fault == "stale") trader.GetPart<InventoryPart>().RemoveObject(key);
            if (fault == "cancel") buyer.AddPart(new CancelTrade());
            int a = TradeSystem.GetDrams(buyer), b = TradeSystem.GetDrams(trader);
            Assert.False(TradeSystem.BuyFromTrader(fault == "missingBuyer" ? null : fault == "self" ? trader : buyer,
                fault == "missingTrader" ? null : trader, fault == "missingKey" ? null : key));
            Assert.AreEqual(a, TradeSystem.GetDrams(buyer)); Assert.AreEqual(b, TradeSystem.GetDrams(trader));
            Assert.False(inventory.Objects.Contains(key));
            Assert.AreEqual(fault != "stale", trader.GetPart<InventoryPart>().Objects.Contains(key));
        }
        [TestCase("iron", true)] [TestCase("Iron", false)] [TestCase("IRON", false)] [TestCase("iron ", false)]
        [TestCase("coo.sealed-library.stillleaf", false)] [TestCase("iron:site", false)] [TestCase("brass", false)] [TestCase("other", false)]
        public void SpareUsesExactCaseSensitiveIdentity(string lockId, bool opens)
        {
            var actor = Actor(); var key = Factory.CreateEntity("SpareIronKey"); Assert.NotNull(key);
            Assert.True(actor.GetPart<InventoryPart>().AddObject(key));
            var door = new Entity(); var part = new LockPart { KeyId = lockId }; door.AddPart(part);
            var e = GameEvent.New("AttemptUnlock"); e.SetParameter("Actor", (object)actor); door.FireEventAndRelease(e);
            Assert.AreEqual(!opens, part.IsLocked); Assert.AreEqual("iron", key.GetPart<KeyPart>().KeyId);
        }
        private sealed class CancelTrade : Part
        { public override string Name => "CancelTrade"; public override bool HandleEvent(GameEvent e) => e.ID != "BeforeTrade"; }
    }
}
