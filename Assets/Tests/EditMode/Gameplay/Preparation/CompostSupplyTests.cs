using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    public abstract class CompostSupplyFixture : FiftyWorldFixture
    {
        EntityFactory priorTrader; System.Random priorRng;
        [SetUp] public void SetUpSupply()
        {
            priorTrader = TraderPart.Factory; priorRng = TraderPart.Rng;
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")));
            TraderPart.Factory = Factory; TraderPart.Rng = new System.Random(64); TradeSystem.SetDrams(Actor, 1000);
            foreach (var cell in Zone.Cells) { cell.Explored = true; cell.IsVisible = true; }
            Diag.ResetAll();
        }
        [TearDown] public void TearDownSupply()
        { TraderPart.Factory = priorTrader; TraderPart.Rng = priorRng; LootTableRegistry.ResetForTests(); }
        protected Entity Seller(bool provisioner = true) => provisioner ? MorrowfastContent.CreateResident("southern-food-vendor", Factory) : Factory.CreateEntity("SpreadSeedKeeper");
        protected int SludgeUnits(Entity owner) => owner.GetPart<InventoryPart>().Objects.Where(i => i.BlueprintName == "InertSludge").Sum(i => Math.Max(0, i.GetPart<StackerPart>()?.StackCount ?? 1));
        protected Entity BuySludge(Entity seller = null)
        {
            seller = seller ?? Seller(); var goods = seller.GetPart<InventoryPart>().Objects.Where(i => i.BlueprintName == "InertSludge").ToArray();
            Assert.AreEqual(2, SludgeUnits(seller), "actual ordinary stock must supply two units");
            int buyerBefore = TradeSystem.GetDrams(Actor), sellerBefore = TradeSystem.GetDrams(seller);
            int price = goods.Sum(i => TradeSystem.GetBuyPrice(i, TradeSystem.GetTradePerformance(Actor), seller));
            foreach (var item in goods) Assert.True(TradeSystem.BuyFromTrader(Actor, seller, item));
            Assert.AreEqual(buyerBefore - price, TradeSystem.GetDrams(Actor)); Assert.AreEqual(sellerBefore + price, TradeSystem.GetDrams(seller));
            Assert.AreEqual(0, SludgeUnits(seller)); Assert.AreEqual(2, SludgeUnits(Actor));
            return Pack.Objects.First(i => i.BlueprintName == "InertSludge");
        }
        protected Entity FreshPlant()
        {
            Bed(); var seed = Carry("KnotflaxSeed");
            Assert.True(Act(seed, Choice(seed, "PlantSeedAt|", a => a.Command.EndsWith("|11|10"))));
            return Zone.GetCell(11, 10).Objects.Single(e => e.HasPart<CropPart>());
        }
        protected static string CompostCommand(Entity sludge) => "CompostCrop|" + Uri.EscapeDataString(sludge.ID);
        protected sealed class ActionProbe : Part
        {
            public int Before, After; public bool Veto, ThrowAfter;
            public override string Name => "CompostSupplyActionProbe";
            public override bool HandleEvent(GameEvent e)
            {
                if (e.GetStringParameter("Command")?.StartsWith("CompostCrop|", StringComparison.Ordinal) != true) return true;
                if (e.ID == "BeforeInventoryAction") { Before++; if (Veto) return false; }
                if (e.ID == "AfterInventoryAction") { After++; if (ThrowAfter) throw new InvalidOperationException("compost rollback probe"); }
                return true;
            }
        }
    }
    [TestFixture] public class CompostSupplyTests : CompostSupplyFixture
    {
        [TestCase(false)] [TestCase(true)]
        public void OrdinarySellerProvidesFinitePurchasableSupply(bool provisioner)
        {
            var seller = Seller(provisioner); var sludge = BuySludge(seller); Assert.NotNull(sludge.GetPart<ExaminablePart>());
            string clue = sludge.GetPart<ExaminablePart>().Text.ToLowerInvariant();
            StringAssert.Contains("compost", clue); StringAssert.Contains("once", clue); StringAssert.Contains("water", clue);
            Assert.False(TradeSystem.BuyFromTrader(Actor, seller, sludge)); Assert.AreEqual(2, SludgeUnits(Actor));
        }
        [Test] public void BoughtSupplyEntersRealPlantAndCompostTransactionsExactlyOnce()
        {
            var sludge = BuySludge(); var crop = FreshPlant(); var part = crop.GetPart<CropPart>(); int before = part.TicksPerStage;
            var probe = new ActionProbe(); Actor.AddPart(probe);
            Assert.True(Actions(crop).Any(a => a.Command == CompostCommand(sludge)));
            Assert.True(Act(crop, CompostCommand(sludge))); Assert.AreEqual(1, SludgeUnits(Actor));
            Assert.True(part.Composted); Assert.AreEqual((before * 3 + 3) / 4, part.TicksPerStage);
            Assert.AreEqual(0, part.MoistureTicks); Assert.AreEqual(0, part.TicksInStage); Assert.AreEqual(0, part.GrowthWetTickRemainder);
            Assert.AreEqual(1, probe.Before); Assert.AreEqual(1, probe.After);
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "event", Kind = "PreparationCompleted", Limit = 20 }).Records.Count);
            var second = Pack.Objects.First(i => i.BlueprintName == "InertSludge"); Assert.False(Act(crop, CompostCommand(second)));
            Assert.AreEqual(1, SludgeUnits(Actor)); Assert.AreEqual(1, probe.After);
            CropSystem.OnTickEnd(Zone); Assert.AreEqual(0, part.TicksInStage); Assert.AreEqual(0, part.GrowthStage);
        }
        [Test] public void GeneratedAllotmentNoticeGivesAUsableSupplyAndTimingClue()
        {
            var zone = new Zone(RepairCultivationSite.ZoneID);
            foreach (var cell in zone.Cells) Assert.True(zone.AddEntity(Factory.CreateEntity("Grass"), cell.X, cell.Y));
            Assert.True(RepairCultivationSite.TryInstall(zone, Factory));
            var owners = zone.GetReadOnlyEntities().Where(e => e.GetProperty(RepairCultivationSite.RoleKey) != null).ToArray();
            Assert.AreEqual(13, owners.Length, "supply uses existing shops, not an extra scenery owner");
            string clue = owners.Single(e => e.GetProperty(RepairCultivationSite.RoleKey) == "notice").GetPart<ExaminablePart>().Text.ToLowerInvariant();
            StringAssert.Contains("compost", clue); StringAssert.Contains("sludge", clue); StringAssert.Contains("before", clue);
            StringAssert.Contains("provisioner", clue); StringAssert.Contains("water", clue); StringAssert.DoesNotContain("only while you act here", clue);
        }
        [TestCase(false)] [TestCase(true)]
        public void ProvisionerFallbackAndConnectedFactoryHaveTheSameSupply(bool connected)
        { TraderPart.Factory = connected ? Factory : null; Assert.AreEqual(2, SludgeUnits(Seller())); }
    }
}
