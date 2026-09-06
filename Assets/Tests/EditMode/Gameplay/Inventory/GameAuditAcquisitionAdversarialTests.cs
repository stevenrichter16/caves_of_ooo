using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>A43: callback reentry, mutable sources, currency rollback and observable gates.</summary>
    public class GameAuditAcquisitionAdversarialTests
    {
        private EntityFactory _factory;
        private Entity _actor, _trader;
        private Zone _zone;
        private InventoryPart Inv => _actor.GetPart<InventoryPart>();
        [OneTimeSetUp]
        public void Load()
        {
            _factory = new EntityFactory(); _factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp]
        public void SetUp()
        {
            FactionManager.Initialize(); PlayerReputation.Reset(); Diag.ResetAll(); Diag.SetChannel("event", true); Diag.SetChannel("trade", true);
            TraderPart.Factory = null; TraderPart.Rng = null;
            _actor = Item("Player"); Inv.MaxWeight = 150; _trader = Item("Merchant");
            TradeSystem.SetDrams(_actor, 10000); TradeSystem.SetDrams(_trader, 1000);
            _zone = new Zone("AcquisitionAdversarial"); _zone.AddEntity(_actor, 10, 10); MessageLog.Clear();
        }
        [TearDown]
        public void TearDown()
        { MessageLog.OnMessage = null; MessageLog.Clear(); Diag.ResetAll(); FactionManager.Reset(); PlayerReputation.Reset(); TraderPart.Factory = null; TraderPart.Rng = null; }
        private Entity Item(string bp, int count = 1)
        {
            var item = _factory.CreateEntity(bp); Assert.NotNull(item, bp);
            if (count != 1) { Assert.NotNull(item.GetPart<StackerPart>(), bp); item.GetPart<StackerPart>().StackCount = count; }
            return item;
        }
        private Entity Ground(string bp = "GoldCoin", int count = 3) { var item = Item(bp, count); _zone.AddEntity(item, 11, 10); return item; }
        private InventoryCommandResult Run(IInventoryCommand command, bool rollback = false) =>
            InventorySystem.ExecuteCommand(rollback ? new FailAfter(command) : command, _actor, _zone);
        private int Records(string kind, Entity target) => DiagQuery.Count(new DiagQuery.Filter { Kind = kind, Actor = _actor.ID, Target = target.ID }).Count;

        // Gold must obey the same veto and acquisition event protocol as ordinary pickup.
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void Adversarial_GoldVetoesAndSuccessHooksHaveRealCounters(bool itemSide, bool veto)
        {
            var item = Ground(); var before = new Callback { Event = itemSide ? "BeforeBeingPickedUp" : "BeforePickup", Veto = veto };
            (itemSide ? item : _actor).AddPart(before);
            var taken = new Callback { Event = "Taken" }; item.AddPart(taken);
            var after = new Callback { Event = "AfterPickup" }; _actor.AddPart(after);
            Assert.AreEqual(!veto, InventorySystem.Pickup(_actor, item, _zone));
            Assert.AreEqual(1, before.Count); Assert.AreEqual(veto ? 0 : 1, taken.Count); Assert.AreEqual(veto ? 0 : 1, after.Count);
            Assert.AreEqual(veto ? 10000 : 10015, TradeSystem.GetDrams(_actor));
            Assert.AreEqual(veto, _zone.GetEntityCell(item) != null);
            Assert.AreEqual(1, Records(veto ? "ItemAcquisitionRejected" : "ItemAcquisitionApplied", item));
        }
        [TestCase(false, "remove")] [TestCase(false, "foreign")] [TestCase(false, "same")]
        [TestCase(true, "remove")] [TestCase(true, "foreign")] [TestCase(true, "same")]
        public void Adversarial_SourceRevalidatesAfterItemHook(bool gold, string move)
        {
            var item = Ground(gold ? "GoldCoin" : "Starapple"); var foreign = new Zone("OtherGround");
            var hook = new Callback { Event = "BeforeBeingPickedUp", Action = e =>
            { _zone.RemoveEntity(item); if (move == "foreign") foreign.AddEntity(item, 5, 5); if (move == "same") _zone.AddEntity(item, 12, 10); } };
            item.AddPart(hook);
            Assert.AreEqual(move == "same", InventorySystem.Pickup(_actor, item, _zone)); Assert.AreEqual(1, hook.Count);
            Assert.AreEqual(10000 + (gold && move == "same" ? 15 : 0), TradeSystem.GetDrams(_actor));
            Assert.AreEqual(move == "foreign", foreign.GetEntityCell(item) != null);
            if (move != "same") Assert.AreEqual(3, item.GetPart<StackerPart>().StackCount);
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_ClaimPrecedesPickupCallback(bool gold)
        {
            var item = Ground(gold ? "GoldCoin" : "Starapple"); bool entered = false, nested = false;
            _actor.AddPart(new Callback { Event = "BeforePickup", Action = e =>
            { if (!entered) { entered = true; nested = InventorySystem.Pickup(_actor, item, _zone); } } });
            Assert.IsTrue(InventorySystem.Pickup(_actor, item, _zone)); Assert.IsTrue(entered); Assert.IsFalse(nested);
            Assert.AreEqual(gold ? 10015 : 10000, TradeSystem.GetDrams(_actor));
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void Adversarial_GoldRollbackDoesNotEraseIndependentCoinPayment(bool callback, bool rollback)
        {
            var outer = Ground(); var inner = Ground("GoldCoin", 1); bool nested = false;
            _actor.AddPart(new Callback { Event = "AfterPickup", Action = e =>
            { if (callback && e.GetParameter<Entity>("Item") == outer) nested = InventorySystem.Pickup(_actor, inner, _zone); } });
            Assert.AreEqual(!rollback, Run(new PickupCommand(outer), rollback).Success); Assert.AreEqual(callback, nested);
            Assert.AreEqual(10000 + (rollback ? 0 : 15) + (callback ? 5 : 0), TradeSystem.GetDrams(_actor));
            Assert.AreEqual(rollback, _zone.GetEntityCell(outer) != null); Assert.AreEqual(!callback, _zone.GetEntityCell(inner) != null);
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_PickupCallbackCannotDropTheParticipatingItem(bool callback)
        {
            var item = Ground("Starapple"); bool nested = false;
            _actor.AddPart(new Callback { Event = "AfterPickup", Action = e =>
            { if (callback) nested = InventorySystem.Drop(_actor, item, _zone); } });
            Assert.IsTrue(InventorySystem.Pickup(_actor, item, _zone)); Assert.IsFalse(nested);
            Assert.IsTrue(Inv.Objects.Contains(item)); Assert.IsNull(_zone.GetEntityCell(item));
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_ExceptionAfterPickupRestoresSourceAndReleasesClaim(bool gold)
        {
            var item = Ground(gold ? "GoldCoin" : "Starapple");
            var hook = new Callback { Event = "AfterPickup", Action = e => { throw new InvalidOperationException("Injected acquisition callback failure"); } };
            _actor.AddPart(hook);
            Assert.IsFalse(InventorySystem.Pickup(_actor, item, _zone));
            Assert.AreEqual((11, 10), _zone.GetEntityPosition(item)); Assert.AreEqual(3, item.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(10000, TradeSystem.GetDrams(_actor)); Assert.IsFalse(Inv.Contains(item));
            hook.Action = null; Assert.IsTrue(InventorySystem.Pickup(_actor, item, _zone));
        }

        // Buyer-side guards mirror seller ownership boundaries; callbacks can spend funds.
        [TestCase("actor")] [TestCase("trader")] [TestCase("item")]
        public void Adversarial_NullBuyInputsRefuseWithoutPayment(string missing)
        {
            var item = Item("Dagger"); _trader.GetPart<InventoryPart>().AddObject(item);
            Assert.IsFalse(TradeSystem.BuyFromTrader(missing == "actor" ? null : _actor, missing == "trader" ? null : _trader, missing == "item" ? null : item));
            Assert.AreEqual(10000, TradeSystem.GetDrams(_actor)); Assert.IsTrue(_trader.GetPart<InventoryPart>().Objects.Contains(item));
        }
        [TestCase(-1)] [TestCase(0)] [TestCase(1)]
        public void Adversarial_BuyRequiresPositiveStock(int count)
        {
            var item = Item("Dagger"); _trader.GetPart<InventoryPart>().AddObject(item); item.GetPart<StackerPart>().StackCount = count;
            Assert.AreEqual(count > 0, TradeSystem.BuyFromTrader(_actor, _trader, item));
            Assert.AreEqual(count <= 0, _trader.GetPart<InventoryPart>().Objects.Contains(item));
            if (count <= 0) Assert.AreEqual(10000, TradeSystem.GetDrams(_actor));
        }
        [Test]
        public void Adversarial_SelfBuyAndAbsentStockDoNotChangeOwnership()
        {
            var item = Item("Dagger"); Inv.AddObject(item);
            Assert.IsFalse(TradeSystem.BuyFromTrader(_actor, _actor, item));
            Assert.IsFalse(TradeSystem.BuyFromTrader(_actor, _trader, item));
            Assert.IsTrue(Inv.Objects.Contains(item)); Assert.AreEqual(10000, TradeSystem.GetDrams(_actor));
        }
        [TestCase("CanBeTraded")] [TestCase("BeforeTrade")]
        public void Adversarial_BuyClaimPrecedesBothCallbacks(string eventName)
        {
            var item = Item("Dagger"); _trader.GetPart<InventoryPart>().AddObject(item); bool entered = false, nested = false;
            (eventName == "CanBeTraded" ? item : _actor).AddPart(new Callback { Event = eventName, Action = e =>
            { if (!entered) { entered = true; nested = TradeSystem.BuyFromTrader(_actor, _trader, item); } } });
            Assert.IsTrue(TradeSystem.BuyFromTrader(_actor, _trader, item)); Assert.IsTrue(entered); Assert.IsFalse(nested);
            Assert.AreEqual(1, Records("Bought", _trader));
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_IndependentBuyMaySpendThePurseBeforeOuterPayment(bool enough)
        {
            var outer = Item("Dagger"); var inner = Item("SilverSand"); var stock = _trader.GetPart<InventoryPart>(); stock.AddObject(outer); stock.AddObject(inner);
            int a = TradeSystem.GetBuyPrice(outer, TradeSystem.GetTradePerformance(_actor), _trader);
            int b = TradeSystem.GetBuyPrice(inner, TradeSystem.GetTradePerformance(_actor), _trader);
            int purse = enough ? a + b : Math.Max(a, b); TradeSystem.SetDrams(_actor, purse); bool nested = false;
            _actor.AddPart(new Callback { Event = "BeforeTrade", Action = e =>
            { if (e.GetParameter<Entity>("Item") == outer) nested = TradeSystem.BuyFromTrader(_actor, _trader, inner); } });
            Assert.AreEqual(enough, TradeSystem.BuyFromTrader(_actor, _trader, outer)); Assert.IsTrue(nested);
            Assert.AreEqual(purse - b - (enough ? a : 0), TradeSystem.GetDrams(_actor));
            Assert.IsTrue(Inv.Objects.Contains(inner)); Assert.AreEqual(!enough, stock.Objects.Contains(outer));
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_BuyCannotOverflowTraderWallet(bool overflow)
        {
            var item = Item("Dagger"); _trader.GetPart<InventoryPart>().AddObject(item);
            int price = TradeSystem.GetBuyPrice(item, TradeSystem.GetTradePerformance(_actor), _trader);
            int before = int.MaxValue - price + (overflow ? 1 : 0); TradeSystem.SetDrams(_trader, before);
            Assert.AreEqual(!overflow, TradeSystem.BuyFromTrader(_actor, _trader, item));
            Assert.AreEqual(overflow ? before : int.MaxValue, TradeSystem.GetDrams(_trader));
            Assert.AreEqual(overflow ? 10000 : 10000 - price, TradeSystem.GetDrams(_actor));
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_AcquisitionOutcomesDistinguishWeightRefusal(bool full)
        {
            Inv.AddObject(Item("Torch", full ? 50 : 48)); var item = Ground("SilverSand", 1);
            Assert.AreEqual(!full, InventorySystem.Pickup(_actor, item, _zone));
            Assert.AreEqual(1, Records(full ? "ItemAcquisitionRejected" : "ItemAcquisitionApplied", item));
        }
        [Test]
        public void Adversarial_ContainerGoldRemainsAnInventoryTradeGood()
        {
            var sack = Item("Sack"); var item = Item("GoldCoin", 3); sack.GetPart<ContainerPart>().AddItem(item);
            Assert.IsTrue(Run(new TakeFromContainerCommand(sack, item)).Success);
            Assert.AreEqual(10000, TradeSystem.GetDrams(_actor)); Assert.AreEqual(3, item.GetPart<StackerPart>().StackCount);
            Assert.IsTrue(Inv.Objects.Contains(item));
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void Adversarial_IndependentPurchaseCannotSpendProvisionalGold(bool preexistingFunds, bool rollback)
        {
            var outer = Ground(); var goods = Item("GoldCoin"); _trader.GetPart<InventoryPart>().AddObject(goods);
            int price = TradeSystem.GetBuyPrice(goods, TradeSystem.GetTradePerformance(_actor), _trader);
            Assert.That(price, Is.InRange(1, 15)); int before = preexistingFunds ? price : 0;
            TradeSystem.SetDrams(_actor, before); bool nested = false;
            _actor.AddPart(new Callback { Event = "AfterPickup", Action = e =>
            { if (e.GetParameter<Entity>("Item") == outer) nested = TradeSystem.BuyFromTrader(_actor, _trader, goods); } });
            Assert.AreEqual(!rollback, Run(new PickupCommand(outer), rollback).Success);
            Assert.AreEqual(preexistingFunds, nested, "Only committed money can fund independent work.");
            Assert.AreEqual(rollback ? 0 : 15, TradeSystem.GetDrams(_actor));
            Assert.AreEqual(preexistingFunds, Inv.Objects.Contains(goods));
            Assert.AreEqual(1000 + (preexistingFunds ? price : 0), TradeSystem.GetDrams(_trader));
        }
        [TestCase(1)] [TestCase(2)]
        public void Adversarial_DeferredGoldRechecksCapacityBeforeCommit(int innerQuantity)
        {
            TradeSystem.SetDrams(_actor, int.MaxValue - 20); var outer = Ground(); var inner = Ground("GoldCoin", innerQuantity);
            bool nested = false;
            _actor.AddPart(new Callback { Event = "AfterPickup", Action = e =>
            { if (e.GetParameter<Entity>("Item") == outer) nested = InventorySystem.Pickup(_actor, inner, _zone); } });
            Assert.AreEqual(innerQuantity == 1, Run(new PickupCommand(outer)).Success); Assert.IsTrue(nested);
            Assert.AreEqual(innerQuantity == 1 ? int.MaxValue : int.MaxValue - 10, TradeSystem.GetDrams(_actor));
            Assert.AreEqual(innerQuantity == 2, _zone.GetEntityCell(outer) != null); Assert.IsNull(_zone.GetEntityCell(inner));
            Assert.AreEqual(innerQuantity == 1 ? 2 : 1,
                Records(innerQuantity == 1 ? "CurrencyCreditApplied" : "CurrencyCreditRejected", _actor));
        }
        [Test]
        public void Adversarial_EquippedDaggerCannotBePickedUpAsGroundLoot()
        {
            var item = Item("Dagger"); Inv.AddObject(item); Assert.IsTrue(InventorySystem.Equip(_actor, item));
            Assert.IsFalse(InventorySystem.Pickup(_actor, item, _zone)); Assert.IsTrue(InventorySystem.IsEquipped(_actor, item));
            Assert.AreEqual(1, _actor.GetPart<Body>().GetParts().Count(p => p._Equipped == item));
        }
        // Property notifications are post-commit observers, not another payment step.
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void Adversarial_TradePublishesBothWalletsBeforeNotifying(bool buy, bool throws)
        {
            var item = Item("Dagger"); (buy ? _trader.GetPart<InventoryPart>() : Inv).AddObject(item);
            int price = buy ? TradeSystem.GetBuyPrice(item, TradeSystem.GetTradePerformance(_actor), _trader)
                : TradeSystem.GetSellPrice(item, TradeSystem.GetTradePerformance(_actor), _trader);
            var payer = buy ? _actor : _trader; var receiver = buy ? _trader : _actor;
            int payerBefore = TradeSystem.GetDrams(payer), receiverBefore = TradeSystem.GetDrams(receiver);
            int observations = 0, oldValue = -1, newValue = -1; bool sawBoth = false;
            payer.AddPart(new Callback { Event = "IntPropertyChanged", Action = e =>
            {
                if (e.GetStringParameter("Name") != TradeSystem.CURRENCY_PROP) return;
                observations++; oldValue = e.GetIntParameter("OldValue"); newValue = e.GetIntParameter("NewValue");
                sawBoth = TradeSystem.GetDrams(payer) == payerBefore - price && TradeSystem.GetDrams(receiver) == receiverBefore + price;
                if (throws) throw new InvalidOperationException("Injected wallet observer failure");
            } });
            Assert.IsTrue(buy ? TradeSystem.BuyFromTrader(_actor, _trader, item) : TradeSystem.SellToTrader(_actor, _trader, item));
            Assert.IsTrue(sawBoth); Assert.AreEqual(1, observations); Assert.AreEqual(payerBefore, oldValue); Assert.AreEqual(payerBefore - price, newValue);
            Assert.AreEqual(payerBefore - price, TradeSystem.GetDrams(payer)); Assert.AreEqual(receiverBefore + price, TradeSystem.GetDrams(receiver));
            Assert.AreEqual(throws ? 1 : 0, DiagQuery.Count(new DiagQuery.Filter { Kind = "CurrencyObserverFailed", Actor = payer.ID }).Count);
            Assert.IsTrue(payer.GetPart<InventoryPart>().Objects.Contains(item));
            Assert.IsFalse(receiver.GetPart<InventoryPart>().Objects.Contains(item));
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_TwoGoldRecipientsArePaidBeforeFirstObserver(bool throws)
        {
            var other = Item("Player"); TradeSystem.SetDrams(other, 20);
            var first = Ground(); var second = Ground("GoldCoin", 1); bool sawBoth = false;
            int secondNotifications = 0;
            other.AddPart(new Callback { Event = "IntPropertyChanged", Action = e =>
            { if (e.GetStringParameter("Name") == TradeSystem.CURRENCY_PROP) secondNotifications++; } });
            _actor.AddPart(new Callback { Event = "IntPropertyChanged", Action = e =>
            {
                if (e.GetStringParameter("Name") != TradeSystem.CURRENCY_PROP) return;
                sawBoth = TradeSystem.GetDrams(_actor) == 10015 && TradeSystem.GetDrams(other) == 25;
                if (throws) throw new InvalidOperationException("Injected first recipient observer failure");
            } });
            var sequence = new Sequence(new PickupCommand(first), new PickupCommand(second), new InventoryContext(other, _zone));
            Assert.IsTrue(Run(sequence).Success); Assert.IsTrue(sawBoth);
            Assert.AreEqual(10015, TradeSystem.GetDrams(_actor)); Assert.AreEqual(25, TradeSystem.GetDrams(other));
            Assert.IsNull(_zone.GetEntityCell(first)); Assert.IsNull(_zone.GetEntityCell(second));
            sequence.Transaction.Commit(); sequence.Transaction.Rollback();
            Assert.AreEqual(1, secondNotifications);
            Assert.AreEqual(10015, TradeSystem.GetDrams(_actor)); Assert.AreEqual(25, TradeSystem.GetDrams(other));
        }
        [Test]
        public void Adversarial_OneRecipientOverflowRefusesEveryQueuedPayment()
        {
            var other = Item("Player"); TradeSystem.SetDrams(other, int.MaxValue - 5);
            var first = Ground(); var second = Ground("GoldCoin", 1);
            other.AddPart(new Callback { Event = "AfterPickup", Action = e => TradeSystem.SetDrams(other, int.MaxValue) });
            var sequence = new Sequence(new PickupCommand(first), new PickupCommand(second), new InventoryContext(other, _zone));
            Assert.IsFalse(Run(sequence).Success);
            Assert.AreEqual(10000, TradeSystem.GetDrams(_actor)); Assert.AreEqual(int.MaxValue, TradeSystem.GetDrams(other));
            Assert.AreEqual(3, first.GetPart<StackerPart>().StackCount); Assert.AreEqual(1, second.GetPart<StackerPart>().StackCount);
            Assert.NotNull(_zone.GetEntityCell(first)); Assert.NotNull(_zone.GetEntityCell(second));
            Assert.AreEqual(0, Records("CurrencyCreditApplied", _actor));
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_CombinedGoldCreditChecksItsTotal(bool overflow)
        {
            int before = int.MaxValue - (overflow ? 19 : 20); TradeSystem.SetDrams(_actor, before);
            var first = Ground(); var second = Ground("GoldCoin", 1);
            var sequence = new Sequence(new PickupCommand(first), new PickupCommand(second));
            Assert.AreEqual(!overflow, Run(sequence).Success);
            Assert.AreEqual(overflow ? before : int.MaxValue, TradeSystem.GetDrams(_actor));
            Assert.AreEqual(overflow, _zone.GetEntityCell(first) != null); Assert.AreEqual(overflow, _zone.GetEntityCell(second) != null);
            if (!overflow) { sequence.Transaction.Commit(); Assert.AreEqual(int.MaxValue, TradeSystem.GetDrams(_actor)); }
        }
        [TestCase("pickup", false)] [TestCase("pickup", true)] [TestCase("take", false)] [TestCase("take", true)]
        [TestCase("buy", false)] [TestCase("buy", true)]
        public void Adversarial_AcquisitionMergeClaimsProtectDestinationOnly(string action, bool sameStack)
        {
            var existing = Item("Starapple", 2); Inv.AddObject(existing); var outer = Ground("Starapple");
            var inner = Item(sameStack ? "Starapple" : "SilverSand", 4); var sack = Item("Sack");
            if (action == "pickup") _zone.AddEntity(inner, 12, 10);
            if (action == "take") sack.GetPart<ContainerPart>().AddItem(inner);
            if (action == "buy") _trader.GetPart<InventoryPart>().AddObject(inner);
            bool nested = false, entered = false;
            outer.AddPart(new Callback { Event = "Taken", Action = e =>
            {
                entered = true;
                nested = action == "pickup" ? InventorySystem.Pickup(_actor, inner, _zone)
                    : action == "buy" ? TradeSystem.BuyFromTrader(_actor, _trader, inner)
                    : Run(new TakeFromContainerCommand(sack, inner)).Success;
            } });
            Assert.IsFalse(Run(new PickupCommand(outer), rollback: true).Success); Assert.IsTrue(entered);
            Assert.AreEqual(!sameStack, nested); Assert.AreEqual(2, existing.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(3, outer.GetPart<StackerPart>().StackCount); Assert.NotNull(_zone.GetEntityCell(outer));
            Assert.AreEqual(4, inner.GetPart<StackerPart>().StackCount); Assert.AreEqual(!sameStack, Inv.Objects.Contains(inner));
            if (action == "pickup") Assert.AreEqual(sameStack, _zone.GetEntityCell(inner) != null);
            if (action == "take") Assert.AreEqual(sameStack, sack.GetPart<ContainerPart>().Contents.Contains(inner));
            if (action == "buy") Assert.AreEqual(sameStack, _trader.GetPart<InventoryPart>().Objects.Contains(inner));
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_SequentialPartialMergesRestoreEverySource(bool rollback)
        {
            var existing = Item("Starapple", 98); Inv.AddObject(existing); var first = Ground("Starapple", 3); var second = Ground("Starapple", 4);
            Assert.AreEqual(!rollback, Run(new Sequence(new PickupCommand(first), new PickupCommand(second)), rollback).Success);
            Assert.AreEqual(rollback ? 98 : 99, existing.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(rollback ? 3 : 6, first.GetPart<StackerPart>().StackCount); Assert.AreEqual(rollback ? 4 : 0, second.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(rollback, _zone.GetEntityCell(first) != null); Assert.AreEqual(rollback, _zone.GetEntityCell(second) != null);
        }
        private sealed class Sequence : IInventoryCommand
        {
            private readonly IInventoryCommand _first, _second; private readonly InventoryContext _secondContext;
            internal InventoryTransaction Transaction;
            internal Sequence(IInventoryCommand first, IInventoryCommand second, InventoryContext secondContext = null)
            { _first = first; _second = second; _secondContext = secondContext; }
            public string Name => "AcquisitionSequence";
            public InventoryValidationResult Validate(InventoryContext context) => _first.Validate(context);
            public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
            { Transaction = transaction; var result = _first.Execute(context, transaction); return result.Success ? _second.Execute(_secondContext ?? context, transaction) : result; }
        }
        private sealed class Callback : Part
        {
            public override string Name => "AcquisitionCallback";
            public string Event; public bool Veto; public Action<GameEvent> Action; public int Count;
            public override bool HandleEvent(GameEvent e) { if (e.ID != Event) return true; Count++; Action?.Invoke(e); return !Veto; }
        }
        private sealed class FailAfter : IInventoryCommand
        {
            private readonly IInventoryCommand _inner; internal FailAfter(IInventoryCommand inner) { _inner = inner; }
            public string Name => "AdversarialAcquisitionFailure";
            public InventoryValidationResult Validate(InventoryContext context) => _inner.Validate(context);
            public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
            { var result = _inner.Execute(context, transaction); return result.Success ? InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "Injected outer refusal.") : result; }
        }
    }
}
