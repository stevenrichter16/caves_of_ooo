using System.Collections.Generic;
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
    /// <summary>A01/A04: refused disposition preserves ownership, quantity and equipment.</summary>
    public class GameAuditTransferTests
    {
        private EntityFactory _factory;
        private Entity _actor;
        private Zone _zone;
        private readonly List<string> _messages = new List<string>();
        private InventoryPart Inv => _actor.GetPart<InventoryPart>();
        [OneTimeSetUp]
        public void Load()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp]
        public void SetUp()
        {
            FactionManager.Initialize(); PlayerReputation.Reset(); Diag.ResetAll(); Diag.SetChannel("trade", true);
            TraderPart.Factory = null; TraderPart.Rng = null;
            _actor = _factory.CreateEntity("Player"); Inv.MaxWeight = -1;
            _zone = new Zone("AuditTransfer"); _zone.AddEntity(_actor, 10, 10);
            MessageLog.Clear(); _messages.Clear(); MessageLog.OnMessage = m => _messages.Add(m);
        }
        [TearDown]
        public void TearDown()
        {
            MessageLog.OnMessage = null; MessageLog.Clear(); FactionManager.Reset(); PlayerReputation.Reset();
            Diag.ResetAll(); TraderPart.Factory = null; TraderPart.Rng = null;
        }
        private Entity Item(string bp, int count = 1)
        {
            var item = _factory.CreateEntity(bp); Assert.NotNull(item, bp);
            if (count != 1) { Assert.NotNull(item.GetPart<StackerPart>(), bp); item.GetPart<StackerPart>().StackCount = count; }
            return item;
        }
        private Entity Carry(string bp, int count = 1)
        {
            var item = Item(bp, count); Assert.IsTrue(Inv.AddObject(item)); return item;
        }
        private Entity FullSack(bool full)
        {
            var sack = Item("Sack"); Assert.IsFalse(sack.GetPart<PhysicsPart>().Solid);
            Assert.AreEqual(6, sack.GetPart<ContainerPart>().MaxItems);
            foreach (string bp in new[] { "Torch", "SilverSand", "FireClay", "WardOil", "HealingTonic", "OvenBuildersGuide" }.Take(full ? 6 : 5))
                Assert.IsTrue(sack.GetPart<ContainerPart>().AddItem(Item(bp)));
            Assert.IsTrue(_zone.AddEntity(sack, 10, 10)); return sack;
        }
        private InventoryCommandResult Run(IInventoryCommand command, bool rollback = false) =>
            InventorySystem.ExecuteCommand(rollback ? new FailAfter(command) : command, _actor, _zone);
        private void AssertCarried(Entity item, int index, int count)
        {
            Assert.AreSame(item, Inv.Objects[index]);
            Assert.AreEqual(count, item.GetPart<StackerPart>()?.StackCount ?? 1);
            Assert.AreSame(_actor, item.GetPart<PhysicsPart>().InInventory);
            Assert.IsNull(item.GetPart<PhysicsPart>().Equipped); Assert.IsNull(_zone.GetEntityCell(item));
        }

        [TestCase(false, false)] [TestCase(true, false)] [TestCase(false, true)] [TestCase(true, true)]
        public void SyntheticCarriedVegetation_DropRespectsActualPlacementRefusal(bool barren, bool partial)
        {
            if (barren) Assert.IsTrue(_zone.AddEntity(Item("FellingBarePosition"), 10, 10));
            Carry("Dagger"); var flower = Item("FlowerField");
            Assert.IsFalse(flower.GetPart<PhysicsPart>().Takeable, "Explicit API control: this is not a legitimate flower pickup.");
            if (partial) flower.AddPart(new StackerPart { StackCount = 3 });
            Assert.IsTrue(Inv.AddObject(flower)); Carry("SilverSand");
            var probe = new DropProbe(); _actor.AddPart(probe);
            var result = Run(partial ? new DropPartialCommand(flower, 1) : new DropCommand(flower));
            Assert.AreEqual(!barren, result.Success);
            var ground = _zone.GetAllEntities().Where(e => e.BlueprintName == "FlowerField").ToList();
            Assert.AreEqual(barren ? 0 : 1, ground.Count);
            if (barren) AssertCarried(flower, 1, partial ? 3 : 1);
            else if (partial) AssertCarried(flower, 1, 2);
            else Assert.IsFalse(Inv.Contains(flower));
            Assert.AreEqual(!barren && !partial ? 1 : 0, probe.After);
            Assert.AreEqual(!barren, _messages.Any(m => m.Contains(" drops ")));
        }

        [TestCase(false)] [TestCase(true)]
        public void RealDagger_CanDropOnOrdinaryOrBarrenGround(bool barren)
        {
            if (barren) _zone.AddEntity(Item("FellingBarePosition"), 10, 10);
            var dagger = Carry("Dagger");
            Assert.IsTrue(Run(new DropPartialCommand(dagger, 1)).Success);
            Assert.IsFalse(Inv.Contains(dagger)); Assert.AreSame(_zone.GetEntityCell(_actor), _zone.GetEntityCell(dagger));
        }

        [TestCase(false)] [TestCase(true)]
        public void EquippedRealDagger_FullSackRefusalKeepsDistinctCarriedStack(bool full)
        {
            var original = Carry("Dagger", 3); Assert.IsTrue(InventorySystem.Equip(_actor, original));
            var equipped = Inv.EquippedItems.Values.Distinct().Single();
            Assert.AreNotSame(original, equipped); Assert.AreEqual(2, original.GetPart<StackerPart>().StackCount);
            var sack = FullSack(full);
            Assert.AreEqual(!full, Run(new PutInContainerCommand(sack, equipped)).Success);
            AssertCarried(original, 0, 2);
            Assert.AreEqual(1, equipped.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(full, InventorySystem.IsEquipped(_actor, equipped));
            Assert.AreEqual(!full, sack.GetPart<ContainerPart>().Contents.Contains(equipped));
            Assert.AreSame(full ? _actor : null, equipped.GetPart<PhysicsPart>().Equipped);
            Assert.AreSame(full ? null : sack, equipped.GetPart<PhysicsPart>().InInventory);
        }

        [TestCase(2, false)] [TestCase(2, true)] [TestCase(98, false)] [TestCase(98, true)]
        public void ContainerMerge_OuterRollbackRestoresBothStackIdentities(int existingCount, bool rollback)
        {
            var sack = Item("Sack"); var container = sack.GetPart<ContainerPart>();
            var existing = Item("Torch", existingCount); Assert.IsTrue(container.AddItem(existing));
            Carry("Dagger"); var incoming = Carry("Torch", 3); Carry("SilverSand");
            Assert.AreEqual(!rollback, Run(new PutInContainerCommand(sack, incoming), rollback).Success);
            Assert.AreSame(existing, container.Contents[0]);
            Assert.AreEqual(rollback ? existingCount : System.Math.Min(99, existingCount + 3), existing.GetPart<StackerPart>().StackCount);
            if (rollback)
            {
                AssertCarried(incoming, 1, 3); Assert.AreEqual(1, container.Contents.Count);
                Assert.AreSame(sack, existing.GetPart<PhysicsPart>().InInventory);
            }
            else
            {
                Assert.IsFalse(Inv.Contains(incoming));
                Assert.AreEqual(existingCount == 98 ? 2 : 1, container.Contents.Count);
                Assert.AreEqual(existingCount == 98 ? 2 : 0, incoming.GetPart<StackerPart>().StackCount);
            }
        }

        [TestCase(48, false)] [TestCase(50, false)] [TestCase(48, true)] [TestCase(50, true)]
        public void ActualTraderCapacity_PaysOnlyAfterDeliveryAndRestoresEquipmentOnRefusal(int stockCount, bool equipped)
        {
            var trader = Item("Merchant"); var shelf = trader.GetPart<InventoryPart>();
            Assert.AreEqual(150, shelf.MaxWeight); Assert.IsEmpty(shelf.Objects);
            Assert.IsTrue(shelf.AddObject(Item("Torch", stockCount)));
            var original = Carry("Dagger", equipped ? 3 : 1); Entity sold = original;
            if (equipped)
            {
                Assert.IsTrue(InventorySystem.Equip(_actor, original)); sold = Inv.EquippedItems.Values.Distinct().Single();
            }
            TradeSystem.SetDrams(_actor, 17); TradeSystem.SetDrams(trader, 1000);
            int price = TradeSystem.GetSellPrice(sold, TradeSystem.GetTradePerformance(_actor), trader);
            bool allowed = stockCount == 48;
            Assert.AreEqual(allowed, TradeSystem.SellToTrader(_actor, trader, sold));
            Assert.AreEqual(17 + (allowed ? price : 0), TradeSystem.GetDrams(_actor));
            Assert.AreEqual(1000 - (allowed ? price : 0), TradeSystem.GetDrams(trader));
            Assert.AreEqual(allowed, shelf.Objects.Contains(sold));
            Assert.AreEqual(!allowed, Inv.Contains(sold));
            Assert.AreEqual(equipped && !allowed, InventorySystem.IsEquipped(_actor, sold));
            Assert.AreEqual(1, sold.GetPart<StackerPart>().StackCount);
            if (equipped) AssertCarried(original, 0, 2);
            else if (!allowed) AssertCarried(sold, 0, 1);
            Assert.AreEqual(allowed ? 1 : 0, DiagQuery.Count(new DiagQuery.Filter { Kind = "Sold" }).Count);
        }

        [TestCase(false)] [TestCase(true)]
        public void PartialDrop_RefreshesSyntheticHandlingAndRollbackPreservesUnrelatedPenalty(bool rollback)
        {
            var item = Carry("Dagger", 3); var handling = item.GetPart<HandlingPart>();
            Assert.NotNull(handling); handling.CarryMovePenalty = 2;
            var speed = _actor.GetStat("Speed"); speed.Penalty = 7; Inv.RefreshHandlingCarryPenalty();
            Assert.AreEqual(13, speed.Penalty);
            Assert.AreEqual(!rollback, Run(new DropPartialCommand(item, 1), rollback).Success);
            Assert.AreEqual(rollback ? 3 : 2, item.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(rollback ? 13 : 11, speed.Penalty);
            Inv.RefreshHandlingCarryPenalty(); Assert.AreEqual(rollback ? 13 : 11, speed.Penalty);
        }
        [TestCase(false)] [TestCase(true)]
        public void TradeScreen_ExplainsCapacityRefusalAndClearsStatusOnSuccess(bool full)
        {
            var trader = Item("Merchant"); trader.GetPart<InventoryPart>().AddObject(Item("Torch", full ? 50 : 48));
            TradeSystem.SetDrams(trader, 1000); Carry("Dagger");
            var go = new UnityEngine.GameObject("Transfer trade UI test");
            try
            {
                var ui = go.AddComponent<TradeUI>(); ui.PlayerEntity = _actor; ui.Open(trader);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(TradeUI).GetField("_panel", flags).SetValue(ui, 1);
                typeof(TradeUI).GetMethod("ExecuteTrade", flags).Invoke(ui, null);
                string status = (string)typeof(TradeUI).GetField("_statusMessage", flags).GetValue(ui);
                if (full)
                {
                    Assert.AreEqual("The trader cannot carry that much weight.", status);
                    var shelf = trader.GetPart<InventoryPart>(); shelf.RemoveObject(shelf.Objects.Single());
                    typeof(TradeUI).GetMethod("ExecuteTrade", flags).Invoke(ui, null);
                    Assert.IsNull(typeof(TradeUI).GetField("_statusMessage", flags).GetValue(ui),
                        "The same refused screen must clear its status after a successful retry.");
                    Assert.IsTrue(shelf.Objects.Any(e => e.BlueprintName == "Dagger"));
                }
                else Assert.IsNull(status);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        [TestCase(false)] [TestCase(true)]
        public void EquipmentSplit_RollbackRebasesHandlingAfterRestoringQuantity(bool rollback)
        {
            var item = Carry("Dagger", 3); item.GetPart<HandlingPart>().CarryMovePenalty = 2;
            var speed = _actor.GetStat("Speed"); speed.Penalty = 7; Inv.RefreshHandlingCarryPenalty();
            Assert.AreEqual(13, speed.Penalty);
            Assert.AreEqual(!rollback, Run(new EquipCommand(item), rollback).Success);
            Assert.AreEqual(rollback ? 3 : 2, item.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(rollback ? 13 : 11, speed.Penalty);
            Inv.RefreshHandlingCarryPenalty(); Assert.AreEqual(rollback ? 13 : 11, speed.Penalty);
        }
        private sealed class FailAfter : IInventoryCommand
        {
            private readonly IInventoryCommand _inner;
            public FailAfter(IInventoryCommand inner) { _inner = inner; }
            public string Name => "TransferOuterFailure";
            public InventoryValidationResult Validate(InventoryContext context) => _inner.Validate(context);
            public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
            {
                var result = _inner.Execute(context, transaction);
                return result.Success ? InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "Injected outer refusal.") : result;
            }
        }
        private sealed class DropProbe : Part
        {
            public override string Name => "TransferDropProbe";
            public int After;
            public override bool HandleEvent(GameEvent e) { if (e.ID == "AfterDrop") After++; return true; }
        }
    }
}
