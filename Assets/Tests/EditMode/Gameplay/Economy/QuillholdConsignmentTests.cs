using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    [TestFixture] public sealed class QuillholdConsignmentTests : FiftyWorldFixture
    {
        const string City = "Overworld.14.9.0", Lit = "Quillhold.DesksLit", Collated = "Quillhold.ReportCollated";
        EntityFactory oldTrader, oldRestock, oldConversation;
        [SetUp] public void Prepare()
        {
            oldTrader = TraderPart.Factory; oldRestock = TraderRestockSystem.Factory; oldConversation = ConversationActions.Factory;
            TraderPart.Factory = null; TraderRestockSystem.Factory = ConversationActions.Factory = Factory;
            Zone.ZoneID = City; Pack.MaxWeight = 1000; TradeSystem.SetDrams(Actor, 10000); PlaceConditionCatalog.Reset();
        }
        [TearDown] public void Restore()
        {
            TraderPart.Factory = oldTrader; TraderRestockSystem.Factory = oldRestock; ConversationActions.Factory = oldConversation;
            QuillholdFactoryProbe.Callback = null; QuillholdFactoryProbe.CreatedCallback = null; PlaceConditionCatalog.Reset();
        }
        Entity Scribe()
        { var e = Place("Scribe"); e.ID = "quillhold-scribe"; e.GetPart<InventoryPart>().MaxWeight = 1000; TradeSystem.SetDrams(e, 10000); return e; }
        MerchantStockPart Start(Entity e, int tick = 10)
        { Assert.True(MerchantStockService.Reconcile(e, Zone, tick)); var s = e.GetPart<MerchantStockPart>(); Assert.NotNull(s); return s; }
        static int Units(Entity e, string bp) => e.GetPart<InventoryPart>().Objects.Where(i => i.BlueprintName == bp)
            .Sum(i => Math.Max(0, BarterDeal.Units(i)));
        SettlementManager State(string condition = null, string zoneId = City)
        {
            var m = SettlementManager.Current ?? new SettlementManager();
            var s = m.GetOrCreateSettlement(zoneId, new PointOfInterest(POIType.Village, "Quillhold", "Palimpsest", 2));
            if (condition != null) s.SetCondition(condition, true); return m;
        }
        Entity Saved(Entity e)
        {
            using (var stream = new MemoryStream())
            {
                var w = new SaveWriter(stream); w.WriteEntityReference(e); w.WriteQueuedEntityBodies(); stream.Position = 0;
                var r = new SaveReader(stream, Factory); var result = r.ReadEntityReference(); r.ReadEntityBodies(); return result;
            }
        }
        [Test] public void QuillholdHasInkCopiesCanonicalReadingAndTwoSpecials()
        {
            var e = Scribe(); var s = Start(e); Assert.AreEqual(1, Units(e, "InkVial")); Assert.AreEqual(1, Units(e, "GrimoireCopy"));
            Assert.AreEqual(1, Units(e, "Codex04")); Assert.AreEqual(2, s.Specials.Count); Assert.False(s.BonusApplied);
            Assert.AreEqual("codex-04", e.GetPart<InventoryPart>().Objects.Single(i => i.BlueprintName == "Codex04").GetPart<ReadableDocumentPart>().DocumentId);
            Assert.AreEqual(25, e.GetPart<InventoryPart>().Objects.Single(i => i.BlueprintName == "InkVial").GetPart<InkVialPart>().InkAmount);
            Assert.IsEmpty(e.GetPart<InventoryPart>().Objects.Single(i => i.BlueprintName == "GrimoireCopy").GetPart<GrimoirePart>().SkillClassName, "blank copies remain service materials");
        }
        [TestCase("Overworld.10.10.0")] [TestCase("Overworld.3.6.0")] [TestCase("Overworld.14.9.1")]
        public void OtherScribesKeepTheirExistingStockPolicy(string zone)
        {
            Zone.ZoneID = zone; var e = Scribe(); var before = e.GetPart<InventoryPart>().Objects.ToArray();
            Assert.False(MerchantStockService.Reconcile(e, Zone, 10)); Assert.IsNull(e.GetPart<MerchantStockPart>());
            CollectionAssert.AreEqual(before, e.GetPart<InventoryPart>().Objects);
        }
        [TestCase("declared")] [TestCase("property")]
        public void BothStockDeclarationsMustMatch(string change)
        {
            var e = Scribe(); if (change == "declared") e.GetPart<TraderPart>().StockTable = "MerchantStock";
            else e.Properties[TraderRestockSystem.ShopStockTableProp] = "MerchantStock";
            Assert.False(MerchantStockService.Reconcile(e, Zone, 10)); Assert.IsNull(e.GetPart<MerchantStockPart>());
        }
        [Test] public void ConditionReadsNeitherCreateNorBackfillSettlements()
        {
            var m = new SettlementManager(); var f = typeof(SettlementManager).GetField("_settlements", BindingFlags.Instance | BindingFlags.NonPublic);
            var states = (Dictionary<string, SettlementState>)f.GetValue(m); Assert.AreEqual(0, states.Count);
            Assert.False(m.HasExistingCondition(City, Lit)); var e = Scribe(); Start(e); MerchantStockService.ProjectBenefit(e);
            Assert.AreEqual(0, states.Count); State(Lit); Assert.True(m.HasExistingCondition(City, Lit)); Assert.False(m.HasExistingCondition("elsewhere", Lit));
        }
        [Test] public void OnlyOilSuppliedLampsExpandTheNextLot()
        {
            var e = Scribe(); var s = Start(e); var ids = s.Specials.ToArray(); State(Lit);
            StringAssert.Contains("next consignment", MerchantStockService.ProjectBenefit(e));
            MerchantStockService.Reconcile(e, Zone, 11); CollectionAssert.AreEqual(ids, s.Specials); Assert.False(s.BonusApplied);
            Assert.True(MerchantStockService.Reconcile(e, Zone, 1210)); Assert.True(s.BonusApplied); Assert.AreEqual(3, s.Specials.Count);
            Assert.AreEqual(1, s.Specials.Count(i => i.BlueprintName == "DryingBreezeGrimoire"));
            StringAssert.Contains("included", MerchantStockService.ProjectBenefit(e));
        }
        [TestCase(Collated)] [TestCase("ImprovedOven")] [TestCase("Quillhold.LampsLit")]
        public void OtherConditionsDoNotImplySuppliedLamps(string condition)
        {
            State(condition); var e = Scribe(); var s = Start(e); Assert.False(s.BonusApplied); Assert.AreEqual(2, s.Specials.Count);
        }
        [Test] public void RemoteSuppliedLampsCannotExpandLocalOffers()
        {
            State(Lit, "Overworld.10.10.0"); var e = Scribe(); var s = Start(e); Assert.False(s.BonusApplied); Assert.AreEqual(2, s.Specials.Count);
        }
        [Test] public void ExistingLitInkTopupRemainsThreeNotThreeMore()
        {
            var e = Scribe(); Start(e); State(Lit); PlaceConsequences.OnZoneEntered(Actor, Zone, true);
            Assert.AreEqual(3, Units(e, "InkVial")); MerchantStockService.Reconcile(e, Zone, 311);
            Assert.AreEqual(3, Units(e, "InkVial")); PlaceConsequences.OnZoneEntered(Actor, Zone, true); Assert.AreEqual(3, Units(e, "InkVial"));
        }
        [Test] public void EveryUtilityChoiceWorksAcrossTheCompleteCycle()
        {
            var e = Scribe(); var s = Start(e); var seen = new HashSet<string>(); string previous = null;
            for (int n = 0; n < 4; n++)
            {
                if (n > 0) Assert.True(MerchantStockService.Reconcile(e, Zone, 10 + 1200 * n));
                Assert.AreNotEqual(previous, s.SelectionRaw); previous = s.SelectionRaw;
                foreach (var item in s.Specials) { seen.Add(item.BlueprintName); Assert.True(MerchantStockService.IsSpecial(e, item)); }
            }
            CollectionAssert.AreEquivalent(new[] { "WardGleamGrimoire", "WateringGrimoire", "PurifyWaterGrimoire", "MendingRiteGrimoire" }, seen);
        }
        [TestCase("ink")] [TestCase("document")] [TestCase("unknown-ink")]
        public void ModifiedGoodsStayRealButDoNotHideBaseSupplyDeficits(string kind)
        {
            var e = Scribe(); Start(e); var bp = kind == "document" ? "Codex04" : "InkVial";
            var item = e.GetPart<InventoryPart>().Objects.Single(i => i.BlueprintName == bp);
            if (kind == "ink") item.GetPart<InkVialPart>().InkAmount = 3;
            if (kind == "document") item.GetPart<ReadableDocumentPart>().DocumentId = "different-document";
            if (kind == "unknown-ink") { item.RemovePart(item.GetPart<InkVialPart>()); item.AddPart(new UnknownMerchantInk()); }
            Assert.True(MerchantStockService.Reconcile(e, Zone, 311)); Assert.Contains(item, e.GetPart<InventoryPart>().Objects); Assert.AreEqual(2, Units(e, bp));
        }
        [Test] public void OldScribeSaveLoadsWithoutAnyRestock()
        {
            var e = Scribe(); var item = Factory.CreateEntity("Codex04"); Assert.True(e.GetPart<InventoryPart>().AddObject(item));
            var copy = Saved(e); Assert.IsNull(copy.GetPart<MerchantStockPart>()); Assert.AreEqual(1, Units(copy, "Codex04"));
            Assert.True(Zone.RemoveEntity(e)); Assert.True(Zone.AddEntity(copy, 11, 10)); Start(copy); Assert.AreEqual(1, Units(copy, "Codex04"));
        }
        [Test] public void CurrentBonusStockRoundTripsByExactReferenceWithoutReroll()
        {
            State(Lit); var e = Scribe(); var s = Start(e); var copy = Saved(e); var t = copy.GetPart<MerchantStockPart>();
            Assert.True(t.BonusApplied); Assert.AreEqual(s.SelectionRaw, t.SelectionRaw); CollectionAssert.AreEqual(s.SpecialKeys, t.SpecialKeys);
            foreach (var item in t.Specials) { Assert.Contains(item, copy.GetPart<InventoryPart>().Objects); Assert.True(MerchantStockService.IsSpecial(copy, item)); }
        }
        [Test] public void OrdinaryGroupingStillRefusesInkAndDocumentExtraPayloads()
        {
            var e = Scribe(); Start(e);
            foreach (var bp in new[] { "InkVial", "Codex04" })
            {
                var item = e.GetPart<InventoryPart>().Objects.First(i => i.BlueprintName == bp);
                var method = typeof(InventoryItemGroups).GetMethod("CaptureIdentity", BindingFlags.Static | BindingFlags.NonPublic,
                    null, new[] { typeof(Entity), typeof(InventoryScreenData.ItemDisplay) }, null);
                Assert.NotNull(method);
                Assert.IsNull(method.Invoke(null, new object[] { e, new InventoryScreenData.ItemDisplay
                    { Item = item, Name = item.GetDisplayName(), Category = "", State = "", StackCount = 1 } }));
            }
        }
        [TestCase("place")] [TestCase("condition")]
        public void FactoryCallbacksCannotPublishStaleLocalEligibility(string change)
        {
            State(Lit); var e = Scribe(); var pack = e.GetPart<InventoryPart>(); var before = pack.Objects.ToArray();
            Factory.RegisterPartType<QuillholdFactoryProbe>();
            Factory.Blueprints["DryingBreezeGrimoire"].Parts[nameof(QuillholdFactoryProbe)] = new Dictionary<string, string>();
            QuillholdFactoryProbe.Callback = () => { if (change == "place") Zone.ZoneID = "Overworld.10.10.0"; else State().GetOrCreateSettlement(City, null).SetCondition(Lit, false); };
            Assert.False(MerchantStockService.Reconcile(e, Zone, 10)); Assert.IsNull(e.GetPart<MerchantStockPart>()); CollectionAssert.AreEqual(before, pack.Objects);
        }
        [Test] public void FailedBonusPreparationKeepsOldLotAndDoesNotConsumePlayerOil()
        {
            var e = Scribe(); var s = Start(e); var ids = s.Specials.ToArray(); State(Lit); var oil = Carry("LampOil");
            e.GetPart<InventoryPart>().MaxWeight = 0;
            Assert.False(MerchantStockService.Reconcile(e, Zone, 1210)); CollectionAssert.AreEqual(ids, s.Specials); Assert.Contains(oil, Pack.Objects);
            Assert.AreEqual(10, s.LastSpecialTick);
        }
        [Test] public void BothProjectMessagesFitTheReservedStatusLine()
        {
            var e = Scribe(); Start(e); Assert.LessOrEqual(MerchantStockService.ProjectBenefit(e).Length, 78);
            State(Lit); Assert.LessOrEqual(MerchantStockService.ProjectBenefit(e).Length, 78);
            MerchantStockService.Reconcile(e, Zone, 1210); Assert.LessOrEqual(MerchantStockService.ProjectBenefit(e).Length, 78);
        }
        [Test] public void MovingAnExistingScribePreservesTheCurrentLotAndCannotRefreshIt()
        {
            State(Lit); var e = Scribe(); var s = Start(e); var stock = e.GetPart<InventoryPart>().Objects.ToArray();
            var ids = s.Specials.ToArray(); Zone.ZoneID = "Overworld.10.10.0";
            Assert.False(MerchantStockService.Reconcile(e, Zone, 1210)); Assert.AreEqual(0, s.Generation);
            CollectionAssert.AreEqual(stock, e.GetPart<InventoryPart>().Objects); CollectionAssert.AreEqual(ids, s.Specials);
            Assert.IsEmpty(MerchantStockService.ProjectBenefit(e));
            Zone.ZoneID = City; Assert.True(MerchantStockService.Reconcile(e, Zone, 1210)); Assert.True(s.BonusApplied);
        }
        [TestCase("empty-ink")] [TestCase("negative-ink")] [TestCase("missing-document")] [TestCase("wrong-document")]
        [TestCase("unknown-ink")]
        public void MalformedFreshSupplyCannotBePublished(string change)
        {
            var e = Scribe(); var pack = e.GetPart<InventoryPart>(); var before = pack.Objects.ToArray();
            string bp = change.Contains("document") ? "Codex04" : "InkVial";
            Factory.RegisterPartType<QuillholdFactoryProbe>();
            Factory.Blueprints[bp].Parts[nameof(QuillholdFactoryProbe)] = new Dictionary<string, string>();
            QuillholdFactoryProbe.CreatedCallback = item =>
            {
                if (change == "empty-ink") item.GetPart<InkVialPart>().InkAmount = 0;
                if (change == "negative-ink") item.GetPart<InkVialPart>().InkAmount = -1;
                if (change == "missing-document") item.GetPart<ReadableDocumentPart>().DocumentId = "missing-document";
                if (change == "wrong-document") item.GetPart<ReadableDocumentPart>().DocumentId = "codex-13";
                if (change == "unknown-ink") { item.RemovePart(item.GetPart<InkVialPart>()); item.AddPart(new UnknownMerchantInk()); }
            };
            Assert.False(MerchantStockService.Reconcile(e, Zone, 10)); Assert.IsNull(e.GetPart<MerchantStockPart>());
            CollectionAssert.AreEqual(before, pack.Objects);
        }
    }
    public sealed class UnknownMerchantInk : InkVialPart { }
    public sealed class QuillholdFactoryProbe : Part
    {
        public static Action Callback;
        public static Action<Entity> CreatedCallback;
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "ObjectCreated") { Callback?.Invoke(); CreatedCallback?.Invoke(ParentEntity); ParentEntity.RemovePart(this); }
            return true;
        }
    }
}
