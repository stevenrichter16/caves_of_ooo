using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>A43: checked acquisition sources, single gold payment and exact merge rollback.</summary>
    public class GameAuditAcquisitionTests
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
            _zone = new Zone("AcquisitionAudit"); _zone.AddEntity(_actor, 10, 10); MessageLog.Clear();
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
        private Entity Carry(string bp, int count = 1) { var item = Item(bp, count); Assert.IsTrue(Inv.AddObject(item)); return item; }
        private InventoryCommandResult Run(IInventoryCommand command, bool rollback = false) =>
            InventorySystem.ExecuteCommand(rollback ? new FailAfter(command) : command, _actor, _zone);

        [TestCase("GoldCoin", "adjacent")] [TestCase("GoldCoin", "detached")] [TestCase("GoldCoin", "foreign")]
        [TestCase("GoldCoin", "self")] [TestCase("GoldCoin", "other")]
        [TestCase("Starapple", "adjacent")] [TestCase("Starapple", "detached")] [TestCase("Starapple", "foreign")]
        [TestCase("Starapple", "self")] [TestCase("Starapple", "other")]
        public void PickupRequiresActualUnownedZoneSource(string bp, string source)
        {
            var item = Item(bp, 3); var foreign = new Zone("ForeignSource");
            if (source == "adjacent") _zone.AddEntity(item, 11, 10);
            if (source == "foreign") foreign.AddEntity(item, 11, 10);
            if (source == "self") Inv.AddObject(item);
            if (source == "other") _trader.GetPart<InventoryPart>().AddObject(item);
            bool allowed = source == "adjacent";
            Assert.AreEqual(allowed, InventorySystem.Pickup(_actor, item, _zone));
            Assert.AreEqual(10000 + (allowed && bp == "GoldCoin" ? 15 : 0), TradeSystem.GetDrams(_actor));
            if (!allowed)
            {
                Assert.AreEqual(3, item.GetPart<StackerPart>().StackCount);
                Assert.AreEqual(source == "self", Inv.Objects.Contains(item));
                Assert.AreEqual(source == "other", _trader.GetPart<InventoryPart>().Objects.Contains(item));
                Assert.AreEqual(source == "foreign", foreign.GetEntityCell(item) != null);
            }
            else { Assert.IsNull(_zone.GetEntityCell(item)); Assert.AreEqual(bp != "GoldCoin", Inv.Contains(item)); }
        }
        [TestCase(-1)] [TestCase(0)] [TestCase(1)] [TestCase(3)]
        public void GoldRequiresPositiveQuantity(int count)
        {
            var item = Item("GoldCoin", count); _zone.AddEntity(item, 10, 10);
            Assert.AreEqual(count > 0, InventorySystem.Pickup(_actor, item, _zone));
            Assert.AreEqual(10000 + (count > 0 ? count * 5 : 0), TradeSystem.GetDrams(_actor));
            Assert.AreEqual(count <= 0, _zone.GetEntityCell(item) != null);
        }
        [TestCase(false)] [TestCase(true)]
        public void GoldStaleReferenceCannotPayAgainButDistinctCoinCan(bool distinct)
        {
            var first = Item("GoldCoin", 3); _zone.AddEntity(first, 10, 10);
            Assert.IsTrue(InventorySystem.Pickup(_actor, first, _zone));
            var second = distinct ? Item("GoldCoin", 3) : first;
            if (distinct) _zone.AddEntity(second, 10, 10);
            Assert.AreEqual(distinct, InventorySystem.Pickup(_actor, second, _zone));
            Assert.AreEqual(distinct ? 10030 : 10015, TradeSystem.GetDrams(_actor));
        }
        [TestCase(false)] [TestCase(true)]
        public void GoldOuterRollbackRestoresWalletAndExactGroundStack(bool rollback)
        {
            var item = Item("GoldCoin", 3); _zone.AddEntity(item, 11, 10);
            Assert.AreEqual(!rollback, Run(new PickupCommand(item), rollback).Success);
            Assert.AreEqual(rollback ? 10000 : 10015, TradeSystem.GetDrams(_actor));
            Assert.AreEqual(rollback ? (11, 10) : (-1, -1), _zone.GetEntityPosition(item));
            Assert.AreEqual(rollback ? 3 : 0, item.GetPart<StackerPart>().StackCount);
            Assert.IsFalse(Inv.Contains(item));
        }
        [TestCase(false)] [TestCase(true)]
        public void GoldPurseOverflowRefusesWhileExactLimitSucceeds(bool overflow)
        {
            int before = int.MaxValue - (overflow ? 14 : 15); TradeSystem.SetDrams(_actor, before);
            var item = Item("GoldCoin", 3); _zone.AddEntity(item, 10, 10);
            Assert.AreEqual(!overflow, InventorySystem.Pickup(_actor, item, _zone));
            Assert.AreEqual(overflow ? before : int.MaxValue, TradeSystem.GetDrams(_actor));
            Assert.AreEqual(overflow, _zone.GetEntityCell(item) != null);
        }
        [TestCase(false, 2, false)] [TestCase(false, 2, true)] [TestCase(false, 98, false)] [TestCase(false, 98, true)]
        [TestCase(true, 2, false)] [TestCase(true, 2, true)] [TestCase(true, 98, false)] [TestCase(true, 98, true)]
        public void AcquisitionMergeRollbackRestoresBothExactStacks(bool container, int existingCount, bool rollback)
        {
            var existing = Carry("Starapple", existingCount); var incoming = Item("Starapple", 3); var sack = Item("Sack");
            if (container) sack.GetPart<ContainerPart>().AddItem(incoming); else _zone.AddEntity(incoming, 11, 10);
            Assert.AreEqual(!rollback, Run(container ? new TakeFromContainerCommand(sack, incoming) : new PickupCommand(incoming), rollback).Success);
            Assert.AreSame(existing, Inv.Objects[0]);
            Assert.AreEqual(rollback ? existingCount : Math.Min(99, existingCount + 3), existing.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(rollback ? 3 : Math.Max(0, existingCount + 3 - 99), incoming.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(!rollback && existingCount == 98, Inv.Objects.Contains(incoming));
            Assert.AreEqual(container && rollback, sack.GetPart<ContainerPart>().Contents.Contains(incoming));
            Assert.AreEqual(!container && rollback, _zone.GetEntityCell(incoming) != null);
            if (rollback) Assert.AreSame(container ? sack : null, incoming.GetPart<PhysicsPart>().InInventory);
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void RefusedAcquisitionDoesNotRemergeSourceSiblings(bool buy, bool full)
        {
            Carry("Starapple", full ? 52 : 51); var first = Item("Starapple", 99); var second = Item("Starapple"); var sack = Item("Sack");
            var stock = _trader.GetPart<InventoryPart>(); var contents = sack.GetPart<ContainerPart>();
            if (buy) { stock.AddObject(first); stock.AddObject(second); } else { contents.AddItem(first); contents.AddItem(second); }
            int price = TradeSystem.GetBuyPrice(first, TradeSystem.GetTradePerformance(_actor), _trader);
            Assert.AreEqual(!full, buy ? TradeSystem.BuyFromTrader(_actor, _trader, first) : Run(new TakeFromContainerCommand(sack, first)).Success);
            var source = buy ? stock.Objects : contents.Contents;
            if (full)
            {
                Assert.AreSame(first, source[0]); Assert.AreSame(second, source[1]);
                Assert.AreEqual(99, first.GetPart<StackerPart>().StackCount); Assert.AreEqual(1, second.GetPart<StackerPart>().StackCount);
                Assert.AreSame(buy ? _trader : sack, first.GetPart<PhysicsPart>().InInventory);
            }
            else { Assert.AreEqual(1, source.Count); Assert.AreSame(second, source[0]); Assert.AreEqual(150, Inv.GetCarriedWeight()); }
            Assert.AreEqual(buy && !full ? 10000 - price : 10000, TradeSystem.GetDrams(_actor));
        }
        [TestCase(false)] [TestCase(true)]
        public void BuyScreenReportsCapacityAndRetriesOnSameScreen(bool full)
        {
            var ballast = Carry("Torch", full ? 50 : 48); var item = Item("Dagger"); _trader.GetPart<InventoryPart>().AddObject(item);
            var go = new UnityEngine.GameObject("AcquisitionTradeUI");
            try
            {
            var ui = go.AddComponent<TradeUI>(); ui.PlayerEntity = _actor; ui.Open(_trader);
            var execute = typeof(TradeUI).GetMethod("ExecuteTrade", BindingFlags.Instance | BindingFlags.NonPublic);
            var status = typeof(TradeUI).GetField("_statusMessage", BindingFlags.Instance | BindingFlags.NonPublic);
            execute.Invoke(ui, null);
            Assert.AreEqual(full ? "You cannot carry that much weight." : null, status.GetValue(ui));
            Assert.AreEqual(!full, Inv.Objects.Contains(item));
            if (full) { Inv.RemoveObject(ballast); execute.Invoke(ui, null); Assert.IsNull(status.GetValue(ui)); Assert.IsTrue(Inv.Objects.Contains(item)); }
            ui.Close();
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        private sealed class FailAfter : IInventoryCommand
        {
            private readonly IInventoryCommand _inner; internal FailAfter(IInventoryCommand inner) { _inner = inner; }
            public string Name => "AcquisitionOuterFailure";
            public InventoryValidationResult Validate(InventoryContext context) => _inner.Validate(context);
            public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
            { var result = _inner.Execute(context, transaction); return result.Success ? InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "Injected outer failure.") : result; }
        }
    }
}
