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
    public sealed class CinderholdProjectConsignmentTests : FiftyWorldFixture
    {
        const string City = "Overworld.6.6.0", Sealed = "Cinderhold.Sealed";
        EntityFactory oldTrader, oldRestock;
        [SetUp] public void SetupSupply()
        {
            oldTrader = TraderPart.Factory; oldRestock = TraderRestockSystem.Factory;
            TraderPart.Factory = null; TraderRestockSystem.Factory = Factory; Zone.ZoneID = City;
            Pack.MaxWeight = 1000; TradeSystem.SetDrams(Actor, 1000);
        }
        [TearDown] public void RestoreSupply()
        { TraderPart.Factory = oldTrader; TraderRestockSystem.Factory = oldRestock; QuillholdFactoryProbe.Callback = null; QuillholdFactoryProbe.CreatedCallback = null; }
        SettlementState Local(string zone = City)
        {
            var m = SettlementManager.Current ?? new SettlementManager();
            return m.GetOrCreateSettlement(zone, new PointOfInterest(POIType.Village, "Cinderhold", "Villagers", 2));
        }
        void Stable() { var site = Local().GetSite("Pump"); Assert.NotNull(site); site.Stage = RepairStage.StableRepair; }
        Entity Smith()
        { var s = Place("Weaponsmith"); s.ID = "project-cinder-smith"; s.GetPart<InventoryPart>().MaxWeight = 1000; TradeSystem.SetDrams(s, 1000); return s; }
        MerchantStockPart Start(Entity s, int tick = 10)
        { Assert.True(MerchantStockService.Reconcile(s, Zone, tick)); return s.GetPart<MerchantStockPart>(); }
        Entity Rod(Entity s) => s.GetPart<MerchantStockPart>().Specials.Single(i => i.BlueprintName == "SoundingRod");
        [TestCase(RepairStage.StableRepair)] [TestCase(RepairStage.ImprovedWithCaretaker)]
        public void LastingLocalPumpExpandsOnlyTheNextDueLot(RepairStage stage)
        {
            var s = Smith(); var stock = Start(s); var old = stock.Specials.ToArray();
            Local().GetSite("Pump").Stage = stage; MerchantStockService.Reconcile(s, Zone, 11);
            Assert.False(stock.BonusApplied); CollectionAssert.AreEqual(old, stock.Specials); StringAssert.Contains("next", MerchantStockService.ProjectBenefit(s));
            Assert.True(MerchantStockService.Reconcile(s, Zone, 1210)); Assert.True(stock.BonusApplied); Assert.AreEqual(3, stock.Specials.Count); Assert.NotNull(Rod(s));
        }
        [Test] public void SealingTheSeepIsAnEqualLastingAlternative()
        { Local().SetCondition(Sealed, true); var s = Smith(); var stock = Start(s); Assert.True(stock.BonusApplied); Assert.AreEqual(3, stock.Specials.Count); Assert.NotNull(Rod(s)); }
        [Test] public void BothQualifyingPathsStillGiveOnlyOneExtraRod()
        { Stable(); Local().SetCondition(Sealed, true); var s = Smith(); Start(s); Assert.AreEqual(1, s.GetPart<MerchantStockPart>().Specials.Count(i => i.BlueprintName == "SoundingRod")); }
        [TestCase("broken")] [TestCase("patch")] [TestCase("running")] [TestCase("field")]
        public void TemporaryOrUnrelatedProgressDoesNotQualify(string state)
        {
            var local = Local(); if (state == "patch") local.GetSite("Pump").Stage = RepairStage.TemporarilyPurified;
            if (state == "running") local.SetCondition("Cinderhold.PumpRunning", true);
            if (state == "field") local.SetCondition("Cinderhold.LevelCleared", true);
            var s = Smith(); var stock = Start(s); Assert.False(stock.BonusApplied); Assert.AreEqual(2, stock.Specials.Count); StringAssert.Contains("pump", MerchantStockService.ProjectBenefit(s));
        }
        [Test] public void RemoteProgressCannotQualifyLocalSmithOrCreateLocalState()
        {
            Local("Overworld.10.10.0").SetCondition(Sealed, true); var s = Smith(); Start(s); Assert.False(s.GetPart<MerchantStockPart>().BonusApplied);
            Assert.False(SettlementManager.Current.TryGetExistingSite(City, "Pump", out _));
        }
        [Test] public void ReadingAndStockWithNoManagerDoNotCreateAProject()
        { var s = Smith(); Start(s); Assert.IsNull(SettlementManager.Current); StringAssert.Contains("pump", MerchantStockService.ProjectBenefit(s)); Assert.IsNull(SettlementManager.Current); }
        [TestCase("Overworld.6.6.1")] [TestCase("Overworld.10.10.0")]
        public void OtherSmithsKeepTheirOrdinaryCatalogue(string zone)
        { Stable(); Zone.ZoneID = zone; var s = Smith(); Start(s); Assert.AreEqual(2, s.GetPart<MerchantStockPart>().Specials.Count); Assert.IsEmpty(MerchantStockService.ProjectBenefit(s)); }
        [Test] public void DueBoundaryAndReopeningCannotRerollOrDuplicateTheRod()
        {
            Stable(); var s = Smith(); var stock = Start(s); var first = Rod(s); MerchantStockService.Reconcile(s, Zone, 1209); Assert.AreSame(first, Rod(s));
            Assert.True(MerchantStockService.Reconcile(s, Zone, 1210)); Assert.AreNotSame(first, Rod(s));
            var current = Rod(s); for (int i = 0; i < 3; i++) { TradeSystem.GetTraderStock(s); MerchantStockService.Reconcile(s, Zone, 1210); }
            Assert.AreSame(current, Rod(s)); Assert.AreEqual(1, stock.Generation);
        }
        [Test] public void OrdinaryPaidPurchaseRemovesOnlyThatSpecialMembership()
        {
            Stable(); var s = Smith(); Start(s); var rod = Rod(s); int before = TradeSystem.GetDrams(Actor);
            Assert.True(TradeSystem.BuyFromTrader(Actor, s, rod)); Assert.Less(TradeSystem.GetDrams(Actor), before); Assert.Contains(rod, Pack.Objects); Assert.False(MerchantStockService.IsSpecial(s, rod));
            Assert.AreEqual(2, s.GetPart<MerchantStockPart>().Specials.Count);
        }
        [Test] public void UsedOrResoldRodSurvivesTheNextConsignment()
        {
            Stable(); var s = Smith(); Start(s); var rod = Rod(s); Assert.True(TradeSystem.BuyFromTrader(Actor, s, rod));
            rod.GetPart<SoundingRodPart>().LastReading = "Already observed stone."; rod.GetPart<SoundingRodPart>().LastTick = 12; rod.GetPart<SoundingRodPart>().Log.Add("12|known floor|Already observed stone.");
            Assert.True(TradeSystem.SellToTrader(Actor, s, rod)); Assert.True(MerchantStockService.Reconcile(s, Zone, 1210)); Assert.Contains(rod, s.GetPart<InventoryPart>().Objects);
            Assert.AreEqual(1, rod.GetPart<SoundingRodPart>().Log.Count); Assert.AreNotSame(rod, Rod(s));
        }
        [Test] public void ModifiedUnboughtRodCannotBeWithdrawnAsPristine()
        {
            Stable(); var s = Smith(); Start(s); var rod = Rod(s); rod.GetPart<SoundingRodPart>().Log.Add("10|known floor|Known answer.");
            Assert.True(MerchantStockService.Reconcile(s, Zone, 1210)); Assert.Contains(rod, s.GetPart<InventoryPart>().Objects); Assert.False(MerchantStockService.IsSpecial(s, rod));
        }
        [Test] public void BonusRodAndExactMembershipSaveWithoutRerollOrFreeRefresh()
        {
            Stable(); var s = Smith(); var stock = Start(s);
            using (var stream = new MemoryStream()) {
                var w = new SaveWriter(stream); w.WriteEntityReference(s); w.WriteQueuedEntityBodies(); stream.Position = 0;
                var r = new SaveReader(stream, Factory); var copy = r.ReadEntityReference(); r.ReadEntityBodies();
                Assert.True(copy.GetPart<MerchantStockPart>().BonusApplied); CollectionAssert.AreEqual(stock.Specials.Select(i => i.ID), copy.GetPart<MerchantStockPart>().Specials.Select(i => i.ID));
                Assert.AreEqual("", Rod(copy).GetPart<SoundingRodPart>().LastReading); Assert.AreEqual(-1, Rod(copy).GetPart<SoundingRodPart>().LastTick); Assert.IsEmpty(Rod(copy).GetPart<SoundingRodPart>().Log);
            }
        }
        [Test] public void RodIdentityDoesNotGrantOrdinaryGroupingOrPhysicalStackPermission()
        {
            Stable(); var s = Smith(); Start(s); var rod = Rod(s);
            var method = typeof(InventoryItemGroups).GetMethod("CaptureIdentity", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(Entity), typeof(InventoryScreenData.ItemDisplay) }, null);
            Assert.IsNull(method.Invoke(null, new object[] { s, new InventoryScreenData.ItemDisplay { Item = rod, Name = rod.GetDisplayName(), Category = "", State = "", StackCount = 1 } }));
            Assert.False(rod.GetPart<StackerPart>()?.CanStackWith(Factory.CreateEntity("SoundingRod")) == true);
        }
        void Probe(Action<Entity> callback)
        { Factory.RegisterPartType<QuillholdFactoryProbe>(); Factory.Blueprints["SoundingRod"].Parts[nameof(QuillholdFactoryProbe)] = new Dictionary<string, string>(); QuillholdFactoryProbe.CreatedCallback = callback; }
        [TestCase("reading")] [TestCase("tick")] [TestCase("log")] [TestCase("null")] [TestCase("missing")]
        public void FreshToolsMustHaveTheirExactPristineSurveyPayload(string change)
        {
            Stable(); var s = Smith(); var before = s.GetPart<InventoryPart>().Objects.ToArray(); int calls = 0;
            Probe(item => { calls++; var p = item.GetPart<SoundingRodPart>();
                if (change == "reading") p.LastReading = "An unearned answer.";
                if (change == "tick") p.LastTick = 0;
                if (change == "log") p.Log.Add("unearned");
                if (change == "null") p.Log = null;
                if (change == "missing") item.RemovePart(p);
            });
            Assert.False(MerchantStockService.Reconcile(s, Zone, 10)); Assert.Greater(calls, 0); Assert.IsNull(s.GetPart<MerchantStockPart>()); CollectionAssert.AreEqual(before, s.GetPart<InventoryPart>().Objects);
        }
        [TestCase(false)] [TestCase(true)] public void FactoryCallbackLosingTheLastQualifyingPathDefersWithoutErasingIt(bool sealedPath)
        {
            if (sealedPath) Local().SetCondition(Sealed, true); else Stable();
            var s = Smith(); Probe(item => { var state = Local(); state.SetCondition(Sealed, false); state.GetSite("Pump").Stage = RepairStage.Fouled; });
            Assert.False(MerchantStockService.Reconcile(s, Zone, 10)); Assert.IsNull(s.GetPart<MerchantStockPart>()); Assert.AreEqual(RepairStage.Fouled, Local().GetSite("Pump").Stage);
        }
        [TestCase(false)] [TestCase(true)] public void EitherRemainingLastingPathStillQualifiesAfterFactoryCallback(bool keepSealed)
        {
            Stable(); Local().SetCondition(Sealed, true); var s = Smith();
            Probe(item => { if (keepSealed) Local().GetSite("Pump").Stage = RepairStage.Fouled; else Local().SetCondition(Sealed, false); });
            Start(s); Assert.NotNull(Rod(s)); Assert.True(s.GetPart<MerchantStockPart>().BonusApplied);
        }
        string MerchantKey(Entity s, Entity item)
        {
            var method = typeof(InventoryItemGroups).GetMethod("CaptureMerchantIdentity", BindingFlags.Static | BindingFlags.NonPublic);
            return (string)method.Invoke(null, new object[] { s, new InventoryScreenData.ItemDisplay { Item = item, Name = item.GetDisplayName(), Category = "", State = "", StackCount = 1 } });
        }
        [TestCase("null-reading")] [TestCase("null-entry")] [TestCase("too-many")] [TestCase("oversize")] [TestCase("invalid-tick")]
        public void UnsupportedResidentNotebooksHaveNoWithdrawalKey(string change)
        {
            Stable(); var s = Smith(); Start(s); var item = Rod(s); var rod = item.GetPart<SoundingRodPart>();
            Assert.NotNull(MerchantKey(s, item));
            if (change == "null-reading") rod.LastReading = null;
            if (change == "null-entry") rod.Log.Add(null);
            if (change == "too-many") for (int i = 0; i <= SoundingRodPart.LogSize; i++) rod.Log.Add("entry");
            if (change == "oversize") rod.LastReading = new string('x', 16384);
            if (change == "invalid-tick") rod.LastTick = -2;
            Assert.IsNull(MerchantKey(s, item)); Assert.True(MerchantStockService.Reconcile(s, Zone, 1210));
            Assert.Contains(item, s.GetPart<InventoryPart>().Objects);
        }
        [Test] public void NotebookKeyFramesActualOrderedEntriesAndNeverRewritesThem()
        {
            Stable(); var s = Smith(); Start(s); var item = Rod(s); var rod = item.GetPart<SoundingRodPart>();
            rod.LastReading = "known"; rod.LastTick = 11; rod.Log.AddRange(new[] { "a", "b|c" });
            var first = MerchantKey(s, item); Assert.NotNull(first);
            rod.Log.Clear(); rod.Log.AddRange(new[] { "a|b", "c" }); Assert.AreNotEqual(first, MerchantKey(s, item));
            var second = MerchantKey(s, item); rod.Log.Reverse(); Assert.AreNotEqual(second, MerchantKey(s, item));
            Assert.AreEqual("known", rod.LastReading); Assert.AreEqual(11, rod.LastTick); CollectionAssert.AreEqual(new[] { "c", "a|b" }, rod.Log);
        }
        [Test] public void ProjectMessagesFitTheExistingStatusLine()
        {
            var s = Smith(); Start(s); Assert.That(MerchantStockService.ProjectBenefit(s).Length, Is.InRange(1,78)); Stable(); Assert.That(MerchantStockService.ProjectBenefit(s).Length, Is.InRange(1,78));
            MerchantStockService.Reconcile(s, Zone, 1210); Assert.That(MerchantStockService.ProjectBenefit(s).Length, Is.InRange(1,78));
        }
    }
}
