using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>GA02c: callback isolation, quantity boundaries, ownership, save reach and observable outcomes.</summary>
    public class GameAuditTransferAdversarialTests
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
            FactionManager.Initialize(); PlayerReputation.Reset(); Diag.ResetAll();
            Diag.SetChannel("event", true); Diag.SetChannel("trade", true); TraderPart.Factory = null;
            _actor = Item("Player"); Inv.MaxWeight = -1; _trader = Item("Merchant");
            TradeSystem.SetDrams(_actor, 20); TradeSystem.SetDrams(_trader, 1000);
            _zone = new Zone("TransferAdversarial"); _zone.AddEntity(_actor, 10, 10); MessageLog.Clear();
        }
        [TearDown]
        public void TearDown()
        { MessageLog.OnMessage = null; MessageLog.Clear(); Diag.ResetAll(); FactionManager.Reset(); PlayerReputation.Reset(); TraderPart.Factory = null; }
        private Entity Item(string bp, int count = 1)
        {
            var item = _factory.CreateEntity(bp); Assert.NotNull(item, bp);
            if (count != 1) { Assert.NotNull(item.GetPart<StackerPart>(), bp); item.GetPart<StackerPart>().StackCount = count; }
            return item;
        }
        private Entity Carry(string bp, int count = 1) { var item = Item(bp, count); Assert.IsTrue(Inv.AddObject(item)); return item; }
        private InventoryCommandResult Run(IInventoryCommand command, bool rollback = false) =>
            InventorySystem.ExecuteCommand(rollback ? new FailAfter(command) : command, _actor, _zone);
        private Entity Equip(string bp = "Dagger")
        {
            var item = Carry(bp); Assert.IsTrue(InventorySystem.Equip(_actor, item)); return item;
        }

        // Callback isolation: the new rollback receipt must not undo independent committed work.
        [TestCase(false, true)] [TestCase(true, true)] [TestCase(true, false)]
        public void Adversarial_OuterDropRollbackDoesNotResurrectAnotherCompletedDrop(bool callback, bool rollback)
        {
            var first = Carry("Dagger"); var second = Carry("SilverSand"); bool nested = false;
            _actor.AddPart(new CallbackPart { Event = "AfterDrop", Action = e =>
            { if (callback && e.GetParameter<Entity>("Item") == first) nested = InventorySystem.Drop(_actor, second, _zone); } });
            Assert.AreEqual(!rollback, Run(new DropCommand(first), rollback).Success);
            Assert.AreEqual(callback, nested);
            Assert.AreEqual(rollback, Inv.Contains(first)); Assert.AreEqual(!rollback, _zone.GetEntityCell(first) != null);
            Assert.AreEqual(!callback, Inv.Contains(second)); Assert.AreEqual(callback, _zone.GetEntityCell(second) != null);
            Assert.IsFalse(Inv.Contains(second) && _zone.GetEntityCell(second) != null, "One authoritative owner after rollback.");
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_ReentrantSaleDuringUnequipCannotOwnAndPayTwice(bool callback)
        {
            var item = Equip(); bool entered = false, nested = false;
            _actor.AddPart(new CallbackPart { Event = "AfterUnequip", Action = e =>
            { if (callback && !entered) { entered = true; nested = TradeSystem.SellToTrader(_actor, _trader, item); } } });
            int price = TradeSystem.GetSellPrice(item, TradeSystem.GetTradePerformance(_actor), _trader);
            Assert.IsTrue(TradeSystem.SellToTrader(_actor, _trader, item)); Assert.IsFalse(nested);
            Assert.AreEqual(20 + price, TradeSystem.GetDrams(_actor)); Assert.AreEqual(1000 - price, TradeSystem.GetDrams(_trader));
            Assert.IsFalse(Inv.Contains(item)); Assert.IsTrue(_trader.GetPart<InventoryPart>().Objects.Contains(item));
            Assert.IsFalse(InventorySystem.IsEquipped(_actor, item));
            Assert.AreEqual(1, DiagQuery.Count(new DiagQuery.Filter { Kind = "Sold" }).Count);
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_NestedDropDuringSaleCannotLeaveSoldItemOnGround(bool callback)
        {
            var item = Equip(); bool nested = false;
            _actor.AddPart(new CallbackPart { Event = "AfterUnequip", Action = e =>
            { if (callback) nested = InventorySystem.Drop(_actor, item, _zone); } });
            Assert.IsTrue(TradeSystem.SellToTrader(_actor, _trader, item)); Assert.IsFalse(nested);
            Assert.IsNull(_zone.GetEntityCell(item)); Assert.IsFalse(Inv.Contains(item));
            Assert.IsTrue(_trader.GetPart<InventoryPart>().Objects.Contains(item));
        }

        // Required outcome records make both sides of the new placement gate observable.
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_IndependentNestedSaleCannotSpendTheSameTraderFundsTwice(bool enoughForBoth)
        {
            var outer = Equip(); var inner = Carry("SilverSand");
            int outerPrice = TradeSystem.GetSellPrice(outer, TradeSystem.GetTradePerformance(_actor), _trader);
            int innerPrice = TradeSystem.GetSellPrice(inner, TradeSystem.GetTradePerformance(_actor), _trader);
            int purse = enoughForBoth ? outerPrice + innerPrice : Math.Max(outerPrice, innerPrice);
            TradeSystem.SetDrams(_trader, purse); bool nested = false;
            _actor.AddPart(new CallbackPart { Event = "AfterUnequip", Action = e =>
            { if (e.GetParameter<Entity>("Item") == outer) nested = TradeSystem.SellToTrader(_actor, _trader, inner); } });
            Assert.AreEqual(enoughForBoth, TradeSystem.SellToTrader(_actor, _trader, outer)); Assert.IsTrue(nested);
            int paid = innerPrice + (enoughForBoth ? outerPrice : 0);
            Assert.AreEqual(20 + paid, TradeSystem.GetDrams(_actor)); Assert.AreEqual(purse - paid, TradeSystem.GetDrams(_trader));
            Assert.AreEqual(!enoughForBoth, InventorySystem.IsEquipped(_actor, outer));
            Assert.IsTrue(_trader.GetPart<InventoryPart>().Objects.Contains(inner)); Assert.IsFalse(Inv.Contains(inner));
        }
        [Test]
        public void Adversarial_SaleClaimPrecedesCanBeTradedCallback()
        {
            var item = Carry("Dagger"); bool entered = false, nested = false;
            item.AddPart(new CallbackPart { Event = "CanBeTraded", Action = e =>
            { if (!entered) { entered = true; nested = TradeSystem.SellToTrader(_actor, _trader, item); } } });
            Assert.IsTrue(TradeSystem.SellToTrader(_actor, _trader, item)); Assert.IsTrue(entered); Assert.IsFalse(nested);
            Assert.AreEqual(1, DiagQuery.Count(new DiagQuery.Filter { Kind = "Sold" }).Count);
        }

        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void Adversarial_DropOutcomeRecordsActualPlacementAndQuantity(bool partial, bool barren)
        {
            var item = Item("FlowerField"); item.AddPart(new StackerPart { StackCount = 3 }); Inv.AddObject(item);
            if (barren) _zone.AddEntity(Item("FellingBarePosition"), 10, 10);
            Assert.AreEqual(!barren, Run(partial ? new DropPartialCommand(item, 1) : new DropCommand(item)).Success);
            var records = DiagQuery.Apply(new DiagQuery.Filter { Kind = barren ? "ItemDispositionRejected" : "ItemDispositionApplied", Actor = _actor.ID }).Records;
            Assert.AreEqual(1, records.Count); StringAssert.Contains(partial ? "DropPartial" : "Drop", records[0].PayloadJson);
            StringAssert.Contains("\"quantity\":" + (partial ? 1 : 3), records[0].PayloadJson);
            StringAssert.Contains(_zone.ZoneID, records[0].PayloadJson);
            if (barren) StringAssert.Contains("ground_placement_refused", records[0].PayloadJson);
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_ContainerOutcomeRecordsDeliveryOrCapacityRefusal(bool full)
        {
            var item = Carry("Torch", 3); var sack = Item("Sack"); if (full) sack.GetPart<ContainerPart>().MaxItems = 0;
            Assert.AreEqual(!full, Run(new PutInContainerCommand(sack, item)).Success);
            var records = DiagQuery.Apply(new DiagQuery.Filter { Kind = full ? "ItemDispositionRejected" : "ItemDispositionApplied", Actor = _actor.ID }).Records;
            Assert.AreEqual(1, records.Count); StringAssert.Contains("PutInContainer", records[0].PayloadJson);
            StringAssert.Contains(sack.ID, records[0].PayloadJson); StringAssert.Contains("\"quantity\":3", records[0].PayloadJson);
        }

        // Boundary/API controls: malformed quantities and invalid ownership are not real sale units.
        [TestCase("actor")] [TestCase("trader")] [TestCase("item")]
        public void Adversarial_NullSaleInputHasNoPayment(string missing)
        {
            var item = Carry("Dagger"); Assert.IsFalse(TradeSystem.SellToTrader(missing == "actor" ? null : _actor,
                missing == "trader" ? null : _trader, missing == "item" ? null : item));
            Assert.IsTrue(Inv.Contains(item)); Assert.AreEqual(20, TradeSystem.GetDrams(_actor)); Assert.AreEqual(1000, TradeSystem.GetDrams(_trader));
        }
        [TestCase(-1)] [TestCase(0)] [TestCase(1)]
        public void Adversarial_SaleRequiresAPositiveUnit(int quantity)
        {
            var item = Carry("Dagger"); item.GetPart<StackerPart>().StackCount = quantity;
            Assert.AreEqual(quantity > 0, TradeSystem.SellToTrader(_actor, _trader, item));
            Assert.AreEqual(quantity <= 0, Inv.Contains(item));
            if (quantity <= 0) { Assert.AreEqual(20, TradeSystem.GetDrams(_actor)); Assert.AreEqual(1000, TradeSystem.GetDrams(_trader)); }
        }
        [Test]
        public void Adversarial_SelfSaleRefusesWithoutChangingWalletOrOrder()
        {
            var item = Carry("Dagger"); Carry("SilverSand"); var before = Inv.Objects.ToArray();
            Assert.IsFalse(TradeSystem.SellToTrader(_actor, _actor, item));
            CollectionAssert.AreEqual(before, Inv.Objects); Assert.AreEqual(20, TradeSystem.GetDrams(_actor));
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_NoTradeVetoHasPositiveCounter(bool veto)
        {
            var item = Carry("Dagger"); if (veto) item.Tags["NoTrade"] = "";
            Assert.AreEqual(!veto, TradeSystem.SellToTrader(_actor, _trader, item)); Assert.AreEqual(veto, Inv.Contains(item));
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_UnequipVetoKeepsTwoHandedEquipment(bool veto)
        {
            var item = Equip("Warhammer"); var callback = new CallbackPart { Event = "BeforeUnequip", Veto = veto }; _actor.AddPart(callback);
            Assert.AreEqual(!veto, TradeSystem.SellToTrader(_actor, _trader, item));
            Assert.AreEqual(veto, InventorySystem.IsEquipped(_actor, item));
            Assert.AreEqual(veto ? 2 : 0, _actor.GetPart<Body>().GetParts().Count(p => p._Equipped == item));
            if (veto) { callback.Veto = false; Assert.IsTrue(TradeSystem.SellToTrader(_actor, _trader, item), "Veto must release the item claim."); }
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_RefusedTwoHandedSaleRestoresBonusesOnce(bool full)
        {
            var item = Carry("Warhammer"); item.GetPart<EquippablePart>().EquipBonuses = "Strength:2";
            int strength = _actor.GetStatValue("Strength"); Assert.IsTrue(InventorySystem.Equip(_actor, item));
            Assert.AreEqual(strength + 2, _actor.GetStatValue("Strength"));
            _trader.GetPart<InventoryPart>().AddObject(Item("Torch", full ? 45 : 44));
            Assert.AreEqual(!full, TradeSystem.SellToTrader(_actor, _trader, item));
            Assert.AreEqual(strength + (full ? 2 : 0), _actor.GetStatValue("Strength"));
            Assert.AreEqual(full ? 2 : 0, _actor.GetPart<Body>().GetParts().Count(p => p._Equipped == item));
        }

        // Save/load and hard-capacity controls exercise the receipt without AddObject restoration.
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_SavedStackRefusalKeepsIdentityEvenWithNowFullSource(bool saved)
        {
            Carry("SilverSand"); Carry("Dagger", 3);
            if (saved) { _zone.RemoveEntity(_actor); _actor = PartRoundTripHelper.RoundTripEntityViaTokenGraph(_actor); _zone.AddEntity(_actor, 10, 10); }
            var item = Inv.Objects.Single(e => e.BlueprintName == "Dagger"); Inv.MaxWeight = 0;
            var sack = Item("Sack"); sack.GetPart<ContainerPart>().MaxItems = 0;
            Assert.IsFalse(Run(new PutInContainerCommand(sack, item)).Success);
            Assert.AreSame(item, Inv.Objects[1]); Assert.AreEqual(3, item.GetPart<StackerPart>().StackCount);
            Assert.AreSame(_actor, item.GetPart<PhysicsPart>().InInventory);
        }
        [TestCase(96)] [TestCase(98)]
        public void Adversarial_FullContainerOnlyAcceptsACompleteMerge(int existingCount)
        {
            var sack = Item("Sack"); var container = sack.GetPart<ContainerPart>(); container.MaxItems = 1;
            var existing = Item("Starapple", existingCount); container.AddItem(existing); var incoming = Carry("Starapple", 3);
            bool allowed = existingCount == 96;
            Assert.AreEqual(allowed, Run(new PutInContainerCommand(sack, incoming)).Success);
            Assert.AreEqual(allowed ? 99 : 98, existing.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(allowed ? 0 : 3, incoming.GetPart<StackerPart>().StackCount); Assert.AreEqual(!allowed, Inv.Contains(incoming));
        }
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_PostPlacementMessageExceptionRestoresExactItems(bool container)
        {
            var item = Carry("Torch", 3); var sack = Item("Sack"); var existing = Item("Torch", 2); sack.GetPart<ContainerPart>().AddItem(existing);
            MessageLog.OnMessage = message => { if (message.StartsWith("You put ") || message.Contains(" drops ")) throw new InvalidOperationException("Injected message failure"); };
            var result = Run(container ? new PutInContainerCommand(sack, item) : new DropCommand(item));
            Assert.IsFalse(result.Success); Assert.AreSame(item, Inv.Objects[0]); Assert.AreEqual(3, item.GetPart<StackerPart>().StackCount);
            Assert.IsNull(_zone.GetEntityCell(item)); Assert.AreEqual(2, existing.GetPart<StackerPart>().StackCount);
            MessageLog.OnMessage = null;
            Assert.IsTrue(Run(container ? new PutInContainerCommand(sack, item) : new DropCommand(item)).Success,
                "Exception rollback must release claims for a successful retry.");
        }
        // One outer transaction may legitimately perform more than one disposition.
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void Adversarial_SequentialTransfersShareClaimsAndUndoInReverse(bool partial, bool rollback)
        {
            var first = Carry("Torch", 5); var second = Carry("SilverSand", 2); var sack = Item("Sack");
            IInventoryCommand a = partial ? new DropPartialCommand(first, 2) : new PutInContainerCommand(sack, first);
            IInventoryCommand b = partial ? new DropPartialCommand(first, 2) : new PutInContainerCommand(sack, second);
            Assert.AreEqual(!rollback, Run(new Sequence(a, b), rollback).Success);
            Assert.AreEqual(partial || rollback, Inv.Contains(first));
            Assert.AreEqual(partial || rollback, Inv.Contains(second));
            Assert.AreEqual(partial && !rollback ? 1 : 5, first.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(partial && !rollback ? 4 : 0, _zone.GetAllEntities()
                .Where(e => e.BlueprintName == "Torch").Sum(e => e.GetPart<StackerPart>().StackCount));
            Assert.AreEqual(!partial && !rollback ? 2 : 0, sack.GetPart<ContainerPart>().Contents.Count);
            if (rollback) { Assert.AreSame(first, Inv.Objects[0]); Assert.AreSame(second, Inv.Objects[1]); }
        }
        // A nested merge cannot mutate the outer transaction's claimed destination stack.
        // A different stack is independent committed work and survives outer rollback.
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void Adversarial_NestedContainerMergeProtectsOnlyParticipatingStacks(bool sameStack, bool rollback)
        {
            var sack = Item("Sack"); var contents = sack.GetPart<ContainerPart>();
            var existing = Item("Torch", 2); Assert.IsTrue(contents.AddItem(existing));
            var outer = Carry("Torch", 3); var other = Item("Player");
            var inner = Item(sameStack ? "Torch" : "SilverSand", 4);
            Assert.IsTrue(other.GetPart<InventoryPart>().AddObject(inner));
            bool entered = false, nested = false;
            MessageLog.OnMessage = message =>
            {
                if (entered || !message.StartsWith("You put ")) return;
                entered = true;
                nested = InventorySystem.ExecuteCommand(new PutInContainerCommand(sack, inner), other, _zone).Success;
            };
            Assert.AreEqual(!rollback, Run(new PutInContainerCommand(sack, outer), rollback).Success);
            Assert.IsTrue(entered); Assert.AreEqual(!sameStack, nested);
            Assert.AreEqual(rollback ? 2 : 5, existing.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(sameStack, other.GetPart<InventoryPart>().Contains(inner));
            Assert.AreEqual(!sameStack, contents.Contents.Contains(inner));
            Assert.AreEqual(4, inner.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(rollback, Inv.Contains(outer));
            Assert.AreEqual(rollback ? 3 : 0, outer.GetPart<StackerPart>().StackCount);
        }
        [Test]
        public void Adversarial_RefusedSaleCanRetryAfterCapacityClears()
        {
            var stock = Item("Torch", 50); _trader.GetPart<InventoryPart>().AddObject(stock); var item = Equip();
            Assert.IsFalse(TradeSystem.SellToTrader(_actor, _trader, item)); Assert.IsTrue(InventorySystem.IsEquipped(_actor, item));
            _trader.GetPart<InventoryPart>().RemoveObject(stock);
            Assert.IsTrue(TradeSystem.SellToTrader(_actor, _trader, item));
            Assert.IsFalse(Inv.Contains(item)); Assert.IsTrue(_trader.GetPart<InventoryPart>().Objects.Contains(item));
        }
        private sealed class CallbackPart : Part
        {
            public override string Name => "TransferCallback";
            public string Event; public Action<GameEvent> Action; public bool Veto;
            public override bool HandleEvent(GameEvent e) { if (e.ID != Event) return true; Action?.Invoke(e); return !Veto; }
        }
        private sealed class FailAfter : IInventoryCommand
        {
            private readonly IInventoryCommand _inner; public FailAfter(IInventoryCommand inner) { _inner = inner; }
            public string Name => "AdversarialTransferOuterFailure";
            public InventoryValidationResult Validate(InventoryContext context) => _inner.Validate(context);
            public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
            { var result = _inner.Execute(context, transaction); return result.Success ? InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "Injected outer refusal.") : result; }
        }
        private sealed class Sequence : IInventoryCommand
        {
            private readonly IInventoryCommand _first, _second;
            internal Sequence(IInventoryCommand first, IInventoryCommand second) { _first = first; _second = second; }
            public string Name => "AdversarialTransferSequence";
            public InventoryValidationResult Validate(InventoryContext context) => _first.Validate(context);
            public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
            { var result = _first.Execute(context, transaction); return result.Success ? _second.Execute(context, transaction) : result; }
        }
    }
}
