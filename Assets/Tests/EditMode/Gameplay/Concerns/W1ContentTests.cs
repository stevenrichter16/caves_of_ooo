using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Concerns;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W1 (Docs/PLACE-WORK-AND-PROGRESSION.md, content waves): the shipped tier-1 concerns
    /// of Sill, Gantry and Tine played end to end through the real conversation, site,
    /// document, reputation and consequence systems — each approach leaves its own place
    /// behind.
    /// </summary>
    public sealed class W1ContentTests
    {
        static EntityFactory factory;
        StoryletPart storylets;
        Entity player;
        int turn = 10;
        const string SillZone = "Overworld.10.10.0", GantryZone = "Overworld.7.8.0", TineZone = "Overworld.13.7.0", QuillZone = "Overworld.14.9.0";

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            FactionManager.Initialize(Resources.Load<TextAsset>("Content/Data/Factions").text);
        }
        [SetUp] public void Setup()
        {
            PlaceSiteCatalog.Reset(); PlaceConditionCatalog.Reset(); ReadableDocumentCatalog.ResetForTests();
            StoryletRegistry.Reset(); ConcernRegistry.LoadAll();
            MessageLog.Clear(); Diag.ResetAll(); ConversationActions.Factory = factory; PlayerReputation.Reset();
            storylets = new StoryletPart(); StoryletPart.Current = storylets;
            NarrativeStatePart.Current = new NarrativeStatePart();
            new SettlementManager(() => turn, id => PlaceSiteCatalog.PlaceAt(id) is string name ? new PointOfInterest(POIType.Village, name) : null);
            player = factory.CreateEntity("Player"); StoryletPart.LocalPlayer = player;
            TradeSystem.SetDrams(player, 200);
        }
        [TearDown] public void Cleanup()
        {
            ConversationManager.EndConversation(); PlaceSiteCatalog.Reset(); PlaceConditionCatalog.Reset();
            StoryletRegistry.Reset(); ConcernRegistry.Reset(); StoryletPart.Current = null; StoryletPart.LocalPlayer = null;
            NarrativeStatePart.Current = null; SettlementManager.ResetCurrent(); ConversationActions.Factory = null;
        }

        Zone current;
        Zone Place(string zoneId, params string[] residents)
        {
            var zone = new Zone(zoneId);
            current?.RemoveEntity(player); zone.AddEntity(player, 10, 10); current = zone;
            for (int i = 0; i < residents.Length; i++) zone.AddEntity(factory.CreateEntity(residents[i]), 12 + i, 10);
            return zone;
        }
        void MoveTo(Zone zone) { current.RemoveEntity(player); zone.AddEntity(player, 10, 10); current = zone; }
        static Entity Who(Zone z, string bp) => z.GetAllEntities().First(e => e.BlueprintName == bp);
        void Give(string bp, int n) { for (int i = 0; i < n; i++) player.GetPart<InventoryPart>().AddObject(factory.CreateEntity(bp)); }
        void Tick() => storylets.OnTickEnd(NarrativeStatePart.Current);
        int Approach(string id) => NarrativeStatePart.Current.GetFact(ConcernRegistry.ApproachFact(id));
        bool Holds(string zone, string condition) => SettlementManager.Current.HasCondition(zone, condition);
        void ReadPlaced(Zone zone, string documentId)
        {
            var doc = zone.GetAllEntities().First(e => e.GetPart<ReadableDocumentPart>()?.DocumentId == documentId);
            var at = zone.GetEntityPosition(doc); zone.MoveEntity(player, at.x, at.y);
            Assert.True(doc.GetPart<ReadableDocumentPart>().TryRead(player, zone));
        }
        static ChoiceData Choice(NodeData n, string text) => n.Choices.First(c => c.Text.Contains(text));
        void Take(Entity speaker, NodeData n, string text) => Assert.True(ConversationActions.TryExecuteAll(Choice(n, text).Actions, speaker, player), text);
        NodeData Status(Entity owner, string id) => ConcernConversation.BuildNode("status|" + id, owner, player);

        // ════════════════ Tine ════════════════

        private const string TinePerchStamp = "PlaceStock:RebuiltByHand.Weir:LakePerch";

        private (Zone zone, OverworldZoneManager manager, Entity elder, Entity merchant) ActiveTineForWeirStock()
        {
            turn = 10;
            var tine = Place(TineZone, "Elder", "Merchant");
            var manager = new OverworldZoneManager(factory, 64);
            manager.ReplaceLoadedState(new System.Collections.Generic.Dictionary<string, Zone> { { TineZone, tine } }, TineZone, null);
            manager.SetTurnProvider(() => turn);
            return (tine, manager, Who(tine, "Elder"), Who(tine, "Merchant"));
        }

        private static int TinePerchCount(Entity merchant) => ConcernItems.CountCarried(merchant, "LakePerch");

        private Entity TineOrdinaryTorch(Entity merchant)
        {
            var inventory = merchant.GetPart<InventoryPart>();
            var existing = inventory.Objects.FirstOrDefault(e => e.BlueprintName == "Torch");
            if (existing != null) return existing;
            var torch = factory.CreateEntity("Torch");
            Assert.That(inventory.AddObject(torch), Is.True);
            return torch;
        }

        private void FinishNativeTineWeir(Entity elder, bool contract)
        {
            Assert.That(ConcernConversation.Accept(elder, player, "tine.weir"), Is.Null);
            Take(elder, Status(elder, "tine.weir"), contract ? "Pay the Concord to rebuild the weir" : "Repair the fishing weir");
            Tick();
            Assert.That(Approach("tine.weir"), Is.EqualTo(contract ? 2 : 1));
            var weir = PlaceSites.Site("Tine", "Weir");
            Assert.That(weir.Stage, Is.EqualTo(RepairStage.StableRepair));
            Assert.That(weir.ResolvedByMethod, Is.EqualTo(contract ? RepairMethodId.Contracted : RepairMethodId.ManualRepair));
            Assert.That(weir.RelapseAtTurn, Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BothDurableTineWeirMethodsStockPerchAtTheNormalBoundaryWithoutSharingPrices(bool contract)
        {
            var (tine, _, elder, merchant) = ActiveTineForWeirStock();
            var ordinary = TineOrdinaryTorch(merchant);
            var merchantPack = merchant.GetPart<InventoryPart>();
            Give("SalvagedTimber", 2);
            Give("OakHaftComponent", 2);
            Give("Hatchet", 1);
            int before = TradeSystem.GetDrams(player);

            FinishNativeTineWeir(elder, contract);
            Assert.That(TradeSystem.GetDrams(player), Is.EqualTo(before - (contract ? 45 : 0)));
            Assert.That(ConcernItems.CountCarried(player, "SalvagedTimber"), Is.EqualTo(contract ? 2 : 0));
            Assert.That(ConcernItems.CountCarried(player, "OakHaftComponent"), Is.EqualTo(contract ? 2 : 0));
            Assert.That(ConcernItems.CountCarried(player, "Hatchet"), Is.EqualTo(1));
            Assert.That(PlaceConsequences.PriceModifier(merchant), Is.EqualTo(contract ? 1.04 : .97).Within(1e-9));
            StringAssert.Contains(contract ? "catch-tax" : "weir's holding", PlaceConsequences.LineFor(TineZone));
            Assert.That(TinePerchCount(merchant), Is.Zero, "Completing the job does not give an immediate stock grant.");
            Assert.That(ConcernItems.CountCarried(player, "LakePerch"), Is.Zero);
            Assert.That(ConversationActions.ExecuteRequired("ContractPlaceRepair", elder, player, "Tine:Weir:45"), Is.EqualTo("already_repaired"));
            Assert.That(ConversationActions.ExecuteRequired("RepairPlaceSite", elder, player, "Tine:Weir:Lasting"), Is.EqualTo("already_repaired"));
            Assert.That(TradeSystem.GetDrams(player), Is.EqualTo(before - (contract ? 45 : 0)));
            Assert.That(ConcernItems.CountCarried(player, "SalvagedTimber"), Is.EqualTo(contract ? 2 : 0));

            PlaceConsequences.OnZoneEntered(player, tine, arriving: true);
            Assert.That(TinePerchCount(merchant), Is.EqualTo(4));
            Assert.That(merchantPack.Objects.Contains(ordinary), Is.True);
            Assert.That(ConcernItems.CountCarried(player, "LakePerch"), Is.Zero, "Perch are paid shop stock.");
        }

        [TestCase("baseline")]
        [TestCase("patch")]
        [TestCase("unfunded")]
        [TestCase("contract-flag-only")]
        [TestCase("hand-flag-only")]
        public void TineKeepsOrdinaryTradeWithoutACompletedDurableWeir(string caseName)
        {
            var (tine, _, elder, merchant) = ActiveTineForWeirStock();
            var ordinary = TineOrdinaryTorch(merchant);
            var sellerPack = merchant.GetPart<InventoryPart>();
            if (caseName == "patch")
            {
                Give("SalvagedTimber", 1);
                Assert.That(ConversationActions.ExecuteRequired("RepairPlaceSite", elder, player, "Tine:Weir:Patch"), Is.Null);
                Assert.That(PlaceSites.Site("Tine", "Weir").Stage, Is.EqualTo(RepairStage.TemporarilyPurified));
            }
            if (caseName == "unfunded")
            {
                Assert.That(ConcernConversation.Accept(elder, player, "tine.weir"), Is.Null);
                TradeSystem.SetDrams(player, 44);
                var offer = Choice(Status(elder, "tine.weir"), "Pay the Concord to rebuild the weir");
                Assert.That(ConversationActions.TryExecuteAll(offer.Actions, elder, player), Is.False);
                Assert.That(TradeSystem.GetDrams(player), Is.EqualTo(44));
                Assert.That(PlaceSites.Site("Tine", "Weir").Stage, Is.EqualTo(RepairStage.Fouled));
            }
            if (caseName == "contract-flag-only")
                SettlementManager.Current.SetCondition(TineZone, "ConcordContract.Weir", true);
            if (caseName == "hand-flag-only")
                SettlementManager.Current.SetCondition(TineZone, "RebuiltByHand.Weir", true);

            PlaceConsequences.OnZoneEntered(player, tine, arriving: true);
            Assert.That(TinePerchCount(merchant), Is.Zero);
            var sellerCell = tine.GetEntityCell(merchant);
            Assert.That(tine.MoveEntity(player, sellerCell.X, sellerCell.Y - 1), Is.True);
            int beforeTorches = ConcernItems.CountCarried(player, "Torch");
            int boughtTorches = ordinary.GetPart<StackerPart>()?.StackCount ?? 1;
            Assert.That(TradeSystem.BuyFromTrader(player, merchant, ordinary), Is.True);
            Assert.That(ConcernItems.CountCarried(player, "Torch"), Is.EqualTo(beforeTorches + boughtTorches));
            Assert.That(sellerPack.Objects.Contains(ordinary), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TineWeirStockAndTheSharedRestockWindowSurviveTheSavedWorld(bool contract)
        {
            var (tine, manager, elder, merchant) = ActiveTineForWeirStock();
            if (!contract)
            {
                Give("SalvagedTimber", 2);
                Give("OakHaftComponent", 2);
                Give("Hatchet", 1);
            }
            FinishNativeTineWeir(elder, contract);
            PlaceConsequences.OnZoneEntered(player, tine, arriving: true);
            Assert.That(TinePerchCount(merchant), Is.EqualTo(4));
            int firstStock = turn;
            Assert.That(merchant.GetIntProperty(TinePerchStamp, -1), Is.EqualTo(firstStock));
            var stockIds = merchant.GetPart<InventoryPart>().Objects.Select(e => e.ID).ToArray();

            using var stream = new MemoryStream();
            GameSessionState.Capture("tine-weir-stock", "native-receipt", manager, null, player).Save(new SaveWriter(stream));
            stream.Position = 0;
            var loaded = GameSessionState.Load(new SaveReader(stream, factory));
            loaded.ZoneManager.SetTurnProvider(() => turn);
            var savedTine = loaded.ZoneManager.ActiveZone;
            var savedMerchant = Who(savedTine, "Merchant");
            var shelf = savedMerchant.GetPart<InventoryPart>();
            Assert.That(savedMerchant.ID, Is.EqualTo(merchant.ID));
            Assert.That(shelf.Objects.Select(e => e.ID), Is.EquivalentTo(stockIds));
            Assert.That(TinePerchCount(savedMerchant), Is.EqualTo(4));
            Assert.That(savedMerchant.GetIntProperty(TinePerchStamp, -1), Is.EqualTo(firstStock));
            Assert.That(PlaceSites.Site("Tine", "Weir").ResolvedByMethod, Is.EqualTo(contract ? RepairMethodId.Contracted : RepairMethodId.ManualRepair));

            var sellerCell = savedTine.GetEntityCell(savedMerchant);
            Assert.That(savedTine.MoveEntity(loaded.Player, sellerCell.X, sellerCell.Y - 1), Is.True);
            var paidFish = shelf.Objects.First(e => e.BlueprintName == "LakePerch");
            int paidUnits = paidFish.GetPart<StackerPart>()?.StackCount ?? 1;
            int drams = TradeSystem.GetDrams(loaded.Player);
            int price = TradeSystem.GetBuyPrice(paidFish, TradeSystem.GetTradePerformance(loaded.Player), savedMerchant);
            Assert.That(TradeSystem.BuyFromTrader(loaded.Player, savedMerchant, paidFish), Is.True);
            Assert.That(TradeSystem.GetDrams(loaded.Player), Is.EqualTo(drams - price));
            Assert.That(ConcernItems.CountCarried(loaded.Player, "LakePerch"), Is.EqualTo(paidUnits));
            turn = firstStock + TraderRestockSystem.RestockIntervalTurns;
            PlaceConsequences.OnZoneEntered(loaded.Player, savedTine, arriving: true);
            PlaceConsequences.OnZoneEntered(loaded.Player, savedTine, arriving: false);
            Assert.That(TinePerchCount(savedMerchant), Is.EqualTo(4 - paidUnits));
            Assert.That(savedMerchant.GetIntProperty(TinePerchStamp, -1), Is.EqualTo(firstStock));

            turn++;
            PlaceConsequences.OnZoneEntered(loaded.Player, savedTine, arriving: true);
            Assert.That(TinePerchCount(savedMerchant), Is.EqualTo(4));
            Assert.That(savedMerchant.GetIntProperty(TinePerchStamp, -1), Is.EqualTo(turn));
            Assert.That(ConcernItems.CountCarried(loaded.Player, "LakePerch"), Is.EqualTo(paidUnits));
        }

        [Test]
        public void ADetachedSameAddressCannotDrawOnTheLiveWorldsWorkingWeir()
        {
            var (tine, _, elder, merchant) = ActiveTineForWeirStock();
            FinishNativeTineWeir(elder, contract: true);

            var detached = new Zone(TineZone);
            var foreignPlayer = factory.CreateEntity("Player");
            var foreignMerchant = factory.CreateEntity("Merchant");
            detached.AddEntity(foreignPlayer, 10, 10);
            detached.AddEntity(foreignMerchant, 12, 10);
            PlaceConsequences.OnZoneEntered(foreignPlayer, detached, arriving: true);
            Assert.That(TinePerchCount(foreignMerchant), Is.Zero);
            Assert.That(foreignMerchant.GetIntProperty(TinePerchStamp, -1), Is.EqualTo(-1));

            PlaceConsequences.OnZoneEntered(foreignPlayer, tine, arriving: true);
            Assert.That(TinePerchCount(merchant), Is.Zero);
            PlaceConsequences.OnZoneEntered(player, tine, arriving: true);
            Assert.That(TinePerchCount(merchant), Is.EqualTo(4));
        }

        [Test] public void TheWeirRebuiltByHandFeedsTine()
        {
            var (tine, _, elder, _) = ActiveTineForWeirStock();
            Assert.IsNull(ConcernConversation.Accept(elder, player, "tine.weir"));
            Give("SalvagedTimber", 2); Give("OakHaftComponent", 2); Give("Hatchet", 1);
            Take(elder, Status(elder, "tine.weir"), "Repair the fishing weir");
            Tick(); Assert.AreEqual(1, Approach("tine.weir"));
            Assert.True(Holds(TineZone, "RebuiltByHand.Weir"));
            ConcernWorld.OnZoneEntered(player, tine, arriving: true);
            Assert.AreEqual(4, ConcernItems.CountCarried(Who(tine, "Merchant"), "LakePerch"), "the merchant sells perch again");
            StringAssert.Contains("Tine's weir is mended", PlaceConsequences.LineFor(SillZone), "Sill has heard");
        }

        [Test] public void TheScribeIsHelpedByInkOrByReadingAtQuillhold()
        {
            var tine = Place(TineZone, "Scribe"); var scribe = Who(tine, "Scribe");
            Assert.IsNull(ConcernConversation.Accept(scribe, player, "tine.scribe"));
            var quill = Place(QuillZone, "Scribe");
            ConcernWorld.OnZoneEntered(player, quill, arriving: true);
            ReadPlaced(quill, "codex-01");
            Tick(); Assert.AreEqual(2, Approach("tine.scribe"), "read at Quillhold");
            MoveTo(tine);
            Assert.IsNull(ConcernConversation.Report(scribe, player, "tine.scribe"));
            Assert.AreEqual(1, ConcernItems.CountCarried(player, "Codex05"), "she gives you her old copy");
            Assert.True(Holds(TineZone, "Tine.ScribeCopyCollated"));
            StringAssert.Contains("third hand", PlaceConsequences.LineFor(QuillZone), "Quillhold has heard");
        }

        // ════════════════ Gantry ════════════════

        [Test] public void BackingTheRegistrarAndSigningTheExchangeLeaveDifferentGantrys()
        {
            var gantry = Place(GantryZone, "GantryRegistrar", "Merchant"); var registrar = Who(gantry, "GantryRegistrar");
            Assert.IsNull(ConcernConversation.Accept(registrar, player, "gantry.manifest"));
            ReadPlaced(gantry, "gantry-manifest");
            Tick(); Assert.AreEqual(1, Approach("gantry.manifest"));
            Assert.True(Holds(GantryZone, "Gantry.ManifestEntered")); Assert.False(Holds(GantryZone, "Gantry.ShortCountSigned"));
            Assert.Greater(PlayerReputation.Get("TentRight"), 0); Assert.Less(PlayerReputation.Get("SaccharineConcord"), 0);
        }

        [Test] public void CountersigningTheShortCountWinsTheExchangeAndLowersPrices()
        {
            var gantry = Place(GantryZone, "GantryRegistrar", "Merchant"); var registrar = Who(gantry, "GantryRegistrar");
            var torch = factory.CreateEntity("LongSword"); int before = TradeSystem.GetBuyPrice(torch, 1.0, Who(gantry, "Merchant"));
            Assert.IsNull(ConcernConversation.Accept(registrar, player, "gantry.manifest"));
            Take(registrar, Status(registrar, "gantry.manifest"), "Countersign");
            Tick(); Assert.AreEqual(2, Approach("gantry.manifest"));
            Assert.True(Holds(GantryZone, "Gantry.ShortCountSigned"));
            Assert.Less(TradeSystem.GetBuyPrice(torch, 1.0, Who(gantry, "Merchant")), before, "the exchange is grateful");
            Assert.Less(PlayerReputation.Get("TentRight"), 0, "the cloth remembers");
            StringAssert.Contains("short count", PlaceConsequences.LineFor("Overworld.8.16.0"), "Wellmeet has heard");
        }

        [Test] public void TheGuestClothIsRehungOrTheBowlFilled()
        {
            var gantry = Place(GantryZone, "TentRightHost"); var host = Who(gantry, "TentRightHost");
            Assert.IsNull(ConcernConversation.Accept(host, player, "gantry.cloth"));
            Give("Starapple", 4);
            Take(host, Status(host, "gantry.cloth"), "Hand over 4");
            Tick(); Assert.AreEqual(2, Approach("gantry.cloth"));
            Assert.True(Holds(GantryZone, "Gantry.GuestBowlFilled"));
            Assert.AreEqual(RepairStage.Fouled, PlaceSites.Site("Gantry", "GuestPole").Stage, "the cloth is still down: a different place");
        }

        // ════════════════ Sill's arc ════════════════

        [Test] public void TheBridgeMoneyFollowsTheWellAndTheLetterStrikesIt()
        {
            var sill = Place(SillZone, "Elder", "WellKeeper"); var elder = Who(sill, "Elder");
            var tithe = ConcernRegistry.Get("sill.tithe");
            Assert.AreEqual(ConcernConversation.State.Hidden, ConcernConversation.StateOf(tithe, elder, player), "after the well");
            var keeper = Who(sill, "WellKeeper");
            Assert.IsNull(ConcernConversation.Accept(keeper, player, "sill.well"));
            var well = PlaceSites.Site("Sill", "MainWell"); well.Stage = RepairStage.StableRepair; well.ResolvedByMethod = RepairMethodId.ManualRepair;
            Tick(); Assert.IsNull(ConcernConversation.Report(keeper, player, "sill.well"));
            Assert.AreEqual(ConcernConversation.State.Offerable, ConcernConversation.StateOf(tithe, elder, player));

            Assert.IsNull(ConcernConversation.Accept(elder, player, "sill.tithe"));
            Take(elder, Status(elder, "sill.tithe"), "I'll carry");
            var gantry = Place(GantryZone, "GantryRegistrar"); var registrar = Who(gantry, "GantryRegistrar");
            Assert.IsNull(ConcernConversation.DeliverCargo(registrar, player, "sill.tithe|letter"));
            Tick(); Assert.AreEqual(1, Approach("sill.tithe"));
            Assert.True(Holds(SillZone, "Sill.TitheStruck"));
            Assert.True(Enumerable.Range(0, 4).Any(i => PlaceConsequences.LineFor(GantryZone, i).Contains("new hand on the roll")),
                "Gantry has heard, beside its other news");
            Assert.True(Enumerable.Range(0, 4).Any(i => PlaceConsequences.LineFor(GantryZone, i).Contains("Sill's well runs clear")), "both pieces of news are told");
        }

        [Test] public void PayingTheBridgeMoneyCostsItAndChangesNothingElse()
        {
            var sill = Place(SillZone, "Elder"); var elder = Who(sill, "Elder");
            storylets.StartQuest(new QuestState { QuestId = ConcernRegistry.QuestIdFor("sill.well") });
            storylets.CompleteQuest(ConcernRegistry.QuestIdFor("sill.well"));
            Assert.IsNull(ConcernConversation.Accept(elder, player, "sill.tithe"));
            TradeSystem.SetDrams(player, 29);
            Assert.False(ConversationActions.TryExecuteAll(Choice(Status(elder, "sill.tithe"), "Pay the bridge money").Actions, elder, player));
            Assert.AreEqual(29, TradeSystem.GetDrams(player)); Tick(); Assert.AreEqual(0, Approach("sill.tithe"), "short: nothing paid, nothing done");
            TradeSystem.SetDrams(player, 30);
            Take(elder, Status(elder, "sill.tithe"), "Pay the bridge money");
            Tick(); Assert.AreEqual(3, Approach("sill.tithe")); Assert.AreEqual(0, TradeSystem.GetDrams(player));
            Assert.True(Holds(SillZone, "Sill.TithePaid")); Assert.False(Holds(SillZone, "Sill.TitheStruck"));
        }

        [Test] public void ShippedTierOneContentLintsAndPinsItsShape()
        {
            Assert.IsEmpty(PlaceSiteCatalog.Lint(factory));
            Assert.IsEmpty(PlaceConditionCatalog.Lint(factory));
            var ids = ConcernRegistry.All.Select(c => c.Id).ToArray();
            CollectionAssert.IsSupersetOf(ids, new[] { "sill.well", "sill.oven", "sill.lantern", "sill.tithe", "gantry.manifest", "gantry.cloth", "tine.weir", "tine.scribe" });
            foreach (var c in ConcernRegistry.All.Where(c => c.Tier == 1))
                Assert.True(c.Approaches.Select(a => a.Kind).Distinct().Count() >= 2 || c.Id.StartsWith("sill.") && c.Id != "sill.tithe",
                    c.Id + ": a new concern offers two different kinds of approach");
        }
        // ════════════════ W1b: Posy, Quillhold, Tally, Marrowstye, Sumphold, Slip ════════════════

        const string PosyZone = "Overworld.5.9.0", TallyZone = "Overworld.10.14.0", MarrowZone = "Overworld.12.12.0",
            VaultZone = "Overworld.15.15.0", SumpZone = "Overworld.15.6.0", SlipZone = "Overworld.16.11.0";

        [Test] public void PosyIsFinishedWithResinOrByStandingInIt()
        {
            var posy = Place(PosyZone, "Elder"); var elder = Who(posy, "Elder");
            Assert.IsNull(ConcernConversation.Accept(elder, player, "posy.composition"));
            Take(elder, Status(elder, "posy.composition"), "Stand where she places you");
            Tick(); Assert.AreEqual(2, Approach("posy.composition"));
            Assert.True(Holds(PosyZone, "Posy.YouWereHeld")); Assert.Greater(PlayerReputation.Get("BowerFolk"), 0);
        }

        [Test] public void QuillholdPaysInInkForLampOil()
        {
            var quill = Place(QuillZone, "Scribe"); var scribe = Who(quill, "Scribe");
            Assert.IsNull(ConcernConversation.Accept(scribe, player, "quillhold.lamps"));
            Give("LampOil", 3); Take(scribe, Status(scribe, "quillhold.lamps"), "Hand over 3");
            Tick(); Assert.AreEqual(1, Approach("quillhold.lamps"));
            int ink = RentalSystem.GetInk(player);
            Assert.IsNull(ConcernConversation.Report(scribe, player, "quillhold.lamps"));
            Assert.AreEqual(ink + 3, RentalSystem.GetInk(player), "paid in ink");
            ConcernWorld.OnZoneEntered(player, quill, arriving: true);
            Assert.AreEqual(3, ConcernItems.CountCarried(scribe, "InkVial"), "the lit desks sell ink");
        }

        const string CopyDesks = "quillhold.lamps";
        const string HandLiquidSupply = "HandConcernLiquidSupply";

        private (Zone zone, Entity scribe) StartCopyDeskSupply()
        {
            var zone = Place(QuillZone, "Scribe");
            var scribe = Who(zone, "Scribe");
            Assert.True(zone.MoveEntity(player, 11, 10));
            zone.GetEntityCell(player).IsVisible = zone.GetEntityCell(player).Explored = true;
            zone.GetEntityCell(scribe).IsVisible = zone.GetEntityCell(scribe).Explored = true;
            Assert.IsNull(ConcernConversation.Accept(scribe, player, CopyDesks));
            Assert.True(ConversationManager.StartConversation(scribe, player));
            return (zone, scribe);
        }

        private Entity SupplyFlask(string liquid, int volume)
        {
            var flask = factory.CreateEntity("LiquidFlask");
            var contents = flask.GetPart<LiquidVesselPart>();
            contents.LiquidId = liquid;
            contents.Volume = volume;
            Assert.True(player.GetPart<InventoryPart>().AddObject(flask));
            return flask;
        }

        private void OpenCopyDeskStatus(Entity scribe)
        {
            Assert.IsNull(ConcernConversation.Open(scribe, player, "status|" + CopyDesks));
            ConversationManager.RefreshVisibleChoices();
        }

        private ChoiceData[] CopyDeskLiquidChoices() => ConversationManager.VisibleChoices
            .Where(choice => choice.GetAction(HandLiquidSupply) != null).ToArray();

        private int CopyDeskDone(string method) => NarrativeStatePart.Current.GetFact(ConcernRegistry.DoneFact(CopyDesks, method));

        [Test]
        public void QuillholdUsesThreeLiquidOilOnceAndReportsTheExistingSupplyApproach()
        {
            using var liquids = new CopyDeskLiquidScope();
            var (zone, scribe) = StartCopyDeskSupply();
            var flask = SupplyFlask("oil", 5);
            var water = SupplyFlask("water", 6);
            Give("LampOil", 3);
            OpenCopyDeskStatus(scribe);
            var choice = CopyDeskLiquidChoices().Single();
            Assert.True(ConversationManager.VisibleChoices.Any(row => row.GetAction(ConcernConversation.SupplyAction) != null));
            int ink = RentalSystem.GetInk(player);

            Assert.True(ConversationManager.SelectChoice(ConversationManager.VisibleChoices.ToList().IndexOf(choice)));
            Assert.That(flask.GetPart<LiquidVesselPart>().Volume, Is.EqualTo(2));
            Assert.That(flask.GetPart<LiquidVesselPart>().LiquidId, Is.EqualTo("oil"));
            Assert.That(flask.GetPart<PhysicsPart>().InInventory, Is.SameAs(player));
            Assert.True(player.GetPart<InventoryPart>().Contains(flask));
            Assert.That(water.GetPart<LiquidVesselPart>().Volume, Is.EqualTo(6));
            Assert.That(ConcernItems.CountCarried(player, "LampOil"), Is.EqualTo(3));
            Assert.That(CopyDeskDone("oil"), Is.EqualTo(1));
            Assert.Zero(CopyDeskDone("collate"));
            Assert.False(Holds(QuillZone, "Quillhold.DesksLit"));
            Assert.False(ConversationActions.TryExecuteAll(choice.Actions, scribe, player));
            Assert.That(RentalSystem.GetInk(player), Is.EqualTo(ink));

            Tick();
            Assert.That(Approach(CopyDesks), Is.EqualTo(1));
            Assert.True(Holds(QuillZone, "Quillhold.DesksLit"));
            Assert.False(Holds(QuillZone, "Quillhold.ReportCollated"));
            Assert.IsNull(ConcernConversation.Open(scribe, player, "report|" + CopyDesks));
            ConversationManager.RefreshVisibleChoices();
            var report = ConversationManager.VisibleChoices.Single(row => row.GetAction(ConcernConversation.ReportAction) != null);
            Assert.True(ConversationManager.SelectChoice(ConversationManager.VisibleChoices.ToList().IndexOf(report)));
            Assert.That(RentalSystem.GetInk(player), Is.EqualTo(ink + 3));
            Assert.False(ConversationActions.TryExecuteAll(report.Actions, scribe, player));
            Assert.That(RentalSystem.GetInk(player), Is.EqualTo(ink + 3));
            ConcernWorld.OnZoneEntered(player, zone, arriving: true);
            Assert.That(ConcernItems.CountCarried(scribe, "InkVial"), Is.EqualTo(3));
        }

        [TestCase("changed-volume")]
        [TestCase("replacement-flask")]
        [TestCase("wrong-recipient")]
        public void QuillholdRejectsStaleLiquidOilQuotesWithoutSpendingOrFinishing(string change)
        {
            using var liquids = new CopyDeskLiquidScope();
            var (zone, scribe) = StartCopyDeskSupply();
            var flask = SupplyFlask("oil", 5);
            OpenCopyDeskStatus(scribe);
            var quoted = CopyDeskLiquidChoices().Single();
            var recipient = scribe;
            if (change == "changed-volume") flask.GetPart<LiquidVesselPart>().Volume = 4;
            else if (change == "replacement-flask")
            {
                Assert.True(player.GetPart<InventoryPart>().RemoveObject(flask));
                var replacement = SupplyFlask("oil", 5);
                replacement.ID = flask.ID;
                flask = replacement;
            }
            else
            {
                recipient = factory.CreateEntity("Scribe");
                Assert.True(zone.AddEntity(recipient, 11, 11));
                zone.GetEntityCell(recipient).IsVisible = zone.GetEntityCell(recipient).Explored = true;
            }

            Assert.False(ConversationActions.TryExecuteAll(quoted.Actions, recipient, player));
            if (change == "wrong-recipient")
            {
                var oldNode = ConversationManager.CurrentNode;
                OpenCopyDeskStatus(scribe);
                Assert.AreNotSame(oldNode, ConversationManager.CurrentNode);
                Assert.False(ConversationActions.TryExecuteAll(quoted.Actions, scribe, player));
                var fresh = CopyDeskLiquidChoices().Single();
                Assert.True(zone.MoveEntity(scribe, 15, 10));
                Assert.False(ConversationActions.TryExecuteAll(fresh.Actions, scribe, player));
            }
            Assert.That(flask.GetPart<LiquidVesselPart>().Volume, Is.EqualTo(change == "changed-volume" ? 4 : 5));
            Assert.That(flask.GetPart<LiquidVesselPart>().LiquidId, Is.EqualTo("oil"));
            Assert.That(flask.GetPart<PhysicsPart>().InInventory, Is.SameAs(player));
            Assert.Zero(CopyDeskDone("oil"));
            Tick();
            Assert.Zero(Approach(CopyDesks));
            Assert.False(Holds(QuillZone, "Quillhold.DesksLit"));
        }

        [TestCase("oil", 2)]
        [TestCase("water", 5)]
        public void QuillholdDoesNotOfferTheLiquidSupplyForAnUnderfilledOrDifferentFlask(string liquid, int volume)
        {
            using var liquids = new CopyDeskLiquidScope();
            var (_, scribe) = StartCopyDeskSupply();
            var flask = SupplyFlask(liquid, volume);
            OpenCopyDeskStatus(scribe);
            Assert.That(CopyDeskLiquidChoices(), Is.Empty);
            Assert.That(flask.GetPart<LiquidVesselPart>().Volume, Is.EqualTo(volume));
            Assert.Zero(CopyDeskDone("oil"));
        }

        private sealed class CopyDeskLiquidScope : IDisposable
        {
            private const BindingFlags RegistryFields = BindingFlags.Static | BindingFlags.NonPublic;
            private readonly Dictionary<string, LiquidDefinition> registry = (Dictionary<string, LiquidDefinition>)
                typeof(LiquidRegistry).GetField("_byId", RegistryFields).GetValue(null);
            private readonly Dictionary<string, LiquidDefinition> saved;
            private readonly bool initialized = LiquidRegistry.IsInitialized;

            public CopyDeskLiquidScope()
            {
                saved = new Dictionary<string, LiquidDefinition>(registry);
                LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(asset => asset.text));
            }

            public void Dispose()
            {
                registry.Clear();
                foreach (var pair in saved) registry.Add(pair.Key, pair.Value);
                typeof(LiquidRegistry).GetField("_initialized", RegistryFields).SetValue(null, initialized);
            }
        }

        [Test] public void TallysAccountIsCarriedPaidOrFormallyDeclined()
        {
            var tally = Place(TallyZone, "Elder"); var factor = Who(tally, "Elder");
            int rep = PlayerReputation.Get("SaccharineConcord");
            Assert.IsNull(ConcernConversation.Refuse(factor, player, "tally.account"));
            Assert.AreEqual(rep - 3, PlayerReputation.Get("SaccharineConcord"), "a formal default costs standing");
            Assert.AreEqual(ClosureState.Refused, storylets.GetClosure(ConcernRegistry.QuestIdFor("tally.account")).State, "and is a clean closure");
        }

        [Test] public void TallysNoticeReachesTheFarmerAtSill()
        {
            var tally = Place(TallyZone, "Elder"); var factor = Who(tally, "Elder");
            Assert.IsNull(ConcernConversation.Accept(factor, player, "tally.account"));
            Take(factor, Status(factor, "tally.account"), "I'll carry");
            var sill = Place(SillZone, "Farmer");
            Assert.IsNull(ConcernConversation.DeliverCargo(Who(sill, "Farmer"), player, "tally.account|notice"));
            Tick(); Assert.AreEqual(1, Approach("tally.account"));
            Assert.True(Holds(SillZone, "Sill.FarmerDunned"));
        }

        [Test] public void MarrowstyesCardsAreRuledVoidAtTheSaltVault()
        {
            var marrow = Place(MarrowZone, "CurationIntakeFiler"); var filer = Who(marrow, "CurationIntakeFiler");
            Assert.IsNull(ConcernConversation.Accept(filer, player, "marrowstye.wing"));
            Take(filer, Status(filer, "marrowstye.wing"), "I'll carry");
            var vault = Place(VaultZone, "StillleafIndexer");
            Assert.IsNull(ConcernConversation.DeliverCargo(Who(vault, "StillleafIndexer"), player, "marrowstye.wing|cards"));
            Tick(); Assert.AreEqual(2, Approach("marrowstye.wing"));
            Assert.True(Holds(MarrowZone, "Marrowstye.CardsVoided"));
        }

        [Test] public void SumpholdsEastCutIsClearedOfItsBandfrogs()
        {
            var sump = Place(SumpZone, "PeatCutter"); var cutter = Who(sump, "PeatCutter");
            Assert.IsNull(ConcernConversation.Accept(cutter, player, "sumphold.cut"));
            Assert.AreEqual("Overworld.16.6.0", ConcernWorld.SiteOf("sumphold.cut", "clear"));
            var cut = new Zone("Overworld.16.6.0"); current.RemoveEntity(player); cut.AddEntity(player, 2, 12); current = cut;
            ConcernWorld.OnZoneEntered(player, cut, arriving: true);
            var frogs = cut.GetAllEntities().Where(e => e.BlueprintName == "Bandfrog").ToList();
            Assert.AreEqual(3, frogs.Count);
            foreach (var f in frogs) f.FireEvent(GameEvent.New("Died"));
            Tick(); Assert.AreEqual(1, Approach("sumphold.cut"));
            Assert.True(Holds(SumpZone, "Sumphold.EastCutCleared"));
        }

        const string SlipEast = "slip.east", SlipLamps = "Slip.EastLampsLit", SlipNames = "Slip.NamesSaid";

        private (Zone zone, Entity warden) SlipLampTown()
        {
            var zone = Place(SlipZone, "Warden");
            var warden = Who(zone, "Warden");
            Assert.True(zone.MoveEntity(player, 11, 10));
            foreach (var cell in new[] { zone.GetEntityCell(player), zone.GetEntityCell(warden) })
                cell.IsVisible = cell.Explored = true;
            return (zone, warden);
        }

        private void OpenSlipLampStatus(Entity warden)
        {
            Assert.IsNull(ConcernConversation.Accept(warden, player, SlipEast));
            Assert.True(ConversationManager.StartConversation(warden, player));
            Assert.IsNull(ConcernConversation.Open(warden, player, "status|" + SlipEast));
            ConversationManager.RefreshVisibleChoices();
        }

        private Entity SettleSlipLampOil(Zone zone)
        {
            var still = factory.CreateEntity("AlchemyStill");
            Assert.True(zone.AddEntity(still, 11, 11));
            Assert.True(zone.AddEntity(factory.CreateEntity("SteamVent"), 9, 11));
            var flask = SupplyFlask("bog-mire", 12);
            var settle = InventorySystem.GetActions(player, still).Single(row => !row.IsDisabled
                && row.Command.StartsWith("SettleLiquid|oil|", StringComparison.Ordinal));
            Assert.True(InventorySystem.PerformAction(player, still, settle.Command, zone));
            Assert.AreEqual("oil", flask.GetPart<LiquidVesselPart>().LiquidId);
            Assert.AreEqual(3, flask.GetPart<LiquidVesselPart>().Volume);
            return flask;
        }

        [TestCase(true, 3)] [TestCase(true, 5)] [TestCase(false, 5)]
        public void SlipsEastLampsUseMeasuredOilOrThreeLampOilItemsAndPayOnlyOnReport(bool measured, int volume)
        {
            using var liquids = new CopyDeskLiquidScope();
            var (zone, warden) = SlipLampTown();
            var flask = measured && volume == 3 ? SettleSlipLampOil(zone) : SupplyFlask("oil", volume);
            var contents = flask.GetPart<LiquidVesselPart>();
            Give("LampOil", 3);
            OpenSlipLampStatus(warden);
            string action = measured ? HandLiquidSupply : ConcernConversation.SupplyAction;
            var choice = ConversationManager.VisibleChoices.Single(row => row.GetAction(action) != null);
            if (measured) Assert.That(choice.Text, Is.EqualTo("[Pour 3 oil (flask 1, " + volume + "/12). Keep flask.]"));
            Assert.True(ConversationManager.VisibleChoices.Any(row => row.GetAction(ConcernConversation.SupplyAction) != null));
            int drams = TradeSystem.GetDrams(player);
            int xp = player.GetStat("Experience").BaseValue;

            Assert.True(ConversationManager.SelectChoice(ConversationManager.VisibleChoices.ToList().IndexOf(choice)));
            Assert.AreSame(contents, flask.GetPart<LiquidVesselPart>());
            Assert.AreSame(player, flask.GetPart<PhysicsPart>().InInventory);
            Assert.True(player.GetPart<InventoryPart>().Contains(flask));
            int remaining = measured ? volume - 3 : volume;
            Assert.AreEqual(remaining, contents.Volume);
            Assert.AreEqual(remaining == 0 ? "" : "oil", contents.LiquidId);
            Assert.AreEqual(measured ? 3 : 0, ConcernItems.CountCarried(player, "LampOil"));
            Assert.AreEqual(1, NarrativeStatePart.Current.GetFact(ConcernRegistry.DoneFact(SlipEast, "lamps")));
            Assert.False(Holds(SlipZone, SlipLamps));
            Assert.False(ConversationActions.TryExecuteAll(choice.Actions, warden, player));

            Tick();
            Assert.AreEqual(1, Approach(SlipEast));
            Assert.True(Holds(SlipZone, SlipLamps));
            Assert.False(Holds(SlipZone, SlipNames));
            Assert.AreEqual(drams, TradeSystem.GetDrams(player));
            Assert.AreEqual(xp, player.GetStat("Experience").BaseValue);
            Assert.IsNull(ConcernConversation.Open(warden, player, "report|" + SlipEast));
            ConversationManager.RefreshVisibleChoices();
            var report = ConversationManager.VisibleChoices.Single(row => row.GetAction(ConcernConversation.ReportAction) != null);
            Assert.True(ConversationManager.SelectChoice(ConversationManager.VisibleChoices.ToList().IndexOf(report)));
            Assert.False(ConversationActions.TryExecuteAll(report.Actions, warden, player));
            Assert.AreEqual(drams + 10, TradeSystem.GetDrams(player));
            Assert.AreEqual(xp + 60, player.GetStat("Experience").BaseValue);
        }

        [TestCase(true)] [TestCase(false)]
        public void SlipsEastLampsRefuseSplitMeasuresAndTheWrongLiquid(bool split)
        {
            using var liquids = new CopyDeskLiquidScope();
            var (_, warden) = SlipLampTown();
            var flasks = split ? new[] { SupplyFlask("oil", 2), SupplyFlask("oil", 2) }
                : new[] { SupplyFlask("water", 5) };
            OpenSlipLampStatus(warden);

            Assert.False(ConversationManager.VisibleChoices.Any(row => row.GetAction(HandLiquidSupply) != null));
            Assert.That(flasks.All(flask => flask.GetPart<LiquidVesselPart>().Volume == (split ? 2 : 5)), Is.True);
            Assert.AreEqual(0, NarrativeStatePart.Current.GetFact(ConcernRegistry.DoneFact(SlipEast, "lamps")));
        }

        [Test]
        public void SlipsEastLampsRejectAnOilFlaskChangedAfterTheWardenQuotesIt()
        {
            using var liquids = new CopyDeskLiquidScope();
            var (_, warden) = SlipLampTown();
            var flask = SupplyFlask("oil", 5);
            OpenSlipLampStatus(warden);
            var quote = ConversationManager.VisibleChoices.Single(row => row.GetAction(HandLiquidSupply) != null);
            int index = ConversationManager.VisibleChoices.ToList().IndexOf(quote);
            int drams = TradeSystem.GetDrams(player);
            int xp = player.GetStat("Experience").BaseValue;
            var status = ConversationManager.CurrentNode;
            flask.GetPart<LiquidVesselPart>().Volume = 4;

            Assert.True(ConversationManager.SelectChoice(index), "a refused handover keeps the conversation open");
            Assert.AreNotSame(status, ConversationManager.CurrentNode, "a stale flask refreshes the available quotes");
            Assert.AreEqual(status.ID, ConversationManager.CurrentNode.ID);
            var refreshed = ConversationManager.VisibleChoices.Single(row => row.GetAction(HandLiquidSupply) != null);
            Assert.That(refreshed.Text, Does.Contain("4/12"));
            Tick();
            Assert.AreEqual(4, flask.GetPart<LiquidVesselPart>().Volume);
            Assert.AreEqual("oil", flask.GetPart<LiquidVesselPart>().LiquidId);
            Assert.AreSame(player, flask.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(0, NarrativeStatePart.Current.GetFact(ConcernRegistry.DoneFact(SlipEast, "lamps")));
            Assert.AreEqual(0, Approach(SlipEast));
            Assert.False(Holds(SlipZone, SlipLamps));
            Assert.False(Holds(SlipZone, SlipNames));
            Assert.AreEqual(drams, TradeSystem.GetDrams(player));
            Assert.AreEqual(xp, player.GetStat("Experience").BaseValue);
        }

        const string SlipEastLamp = "SlipEastLamp";

        private Zone GenerateSlip(OverworldZoneManager manager)
        {
            bool loadLoot = !LootTableRegistry.IsInitialized;
            if (loadLoot) CinderholdCompositionTests.LoadLoot();
            try { return manager.GetZone(SlipZone); }
            finally { if (loadLoot) LootTableRegistry.ResetForTests(); }
        }

        private static IEnumerable<Cell> SlipCardinalNeighbors(Zone zone, Cell cell)
        {
            yield return zone.GetCell(cell.X + 1, cell.Y);
            yield return zone.GetCell(cell.X, cell.Y + 1);
            yield return zone.GetCell(cell.X - 1, cell.Y);
            yield return zone.GetCell(cell.X, cell.Y - 1);
        }

        private static bool DrySlipCell(Zone zone, Cell cell) => cell != null && !cell.IsInterior
            && zone.TileState.Get(cell.X, cell.Y)?.Coatings.Any(layer => layer.Turns > 0) != true
            && !cell.Occupants.Any(entity => entity.GetPart<LiquidPoolPart>() is { Volume: > 0 });

        [Test]
        public void SlipsEastLampsAreReachableFixedFixturesInTheGeneratedVillage()
        {
            var manager = new OverworldZoneManager(factory, 65);
            var zone = GenerateSlip(manager);
            var well = zone.GetEntityCell(Who(zone, "Well"));
            var lamps = zone.GetAllEntities().Where(entity => entity.BlueprintName == SlipEastLamp).ToArray();
            Assert.That(lamps, Has.Length.EqualTo(2));
            var square = SlipCardinalNeighbors(zone, well).First(cell => DrySlipCell(zone, cell) && !cell.BlocksMovement());
            var cells = lamps.Select(zone.GetEntityCell).ToArray();
            Assert.AreNotSame(cells[0], cells[1]);
            foreach (var lamp in lamps)
            {
                var cell = zone.GetEntityCell(lamp);
                Assert.Greater(cell.X, well.X);
                Assert.True(DrySlipCell(zone, cell));
                Assert.False(InventorySystem.GetTakeableItemsInCell(cell).Contains(lamp));
                Assert.True(SlipCardinalNeighbors(zone, cell).Any(approach => DrySlipCell(zone, approach)
                    && !approach.BlocksMovement()
                    && FindPath.Search(zone, square.X, square.Y, approach.X, approach.Y, ignoreCreatures: true).Usable));
            }

            Assert.AreSame(zone, manager.GetZone(SlipZone));
            CollectionAssert.AreEquivalent(lamps, zone.GetAllEntities().Where(entity => entity.BlueprintName == SlipEastLamp));
        }

        [Test]
        public void SlipsSuppliedEastLampsBrightenTheRoadOnTheSameMapWhileReadingNamesDoesNot()
        {
            var manager = new OverworldZoneManager(factory, 65);
            manager.SetTurnProvider(() => turn);
            var zone = GenerateSlip(manager);
            var well = zone.GetEntityCell(Who(zone, "Well"));
            var ordinary = zone.GetEntityCell(Who(zone, "WatchLantern"));
            zone.AmbientLevel = 0.1f;
            var light = new LightMap();
            light.Compute(zone);
            float west = light.GetBrightness(well.X - 1, well.Y + 3);
            float ordinaryLight = light.GetBrightness(ordinary.X, ordinary.Y);
            var east = Enumerable.Range(well.X + 1, 11)
                .SelectMany(x => Enumerable.Range(well.Y - 3, 7).Select(y => zone.GetCell(x, y)))
                .Where(cell => DrySlipCell(zone, cell) && !cell.BlocksMovement()).ToArray();
            var before = east.ToDictionary(cell => cell, cell => light.GetBrightness(cell.X, cell.Y));
            Assert.That(east, Is.Not.Empty);

            manager.SettlementManager.SetCondition(SlipZone, SlipNames, true);
            light.Compute(zone);
            Assert.True(east.All(cell => Math.Abs(light.GetBrightness(cell.X, cell.Y) - before[cell]) < 0.0001f));

            manager.SettlementManager.SetCondition(SlipZone, SlipLamps, true);
            light.Compute(zone);
            Assert.True(east.Any(cell => light.GetBrightness(cell.X, cell.Y) > before[cell] + 0.0001f));
            foreach (var lamp in zone.GetAllEntities().Where(entity => entity.BlueprintName == SlipEastLamp))
                Assert.True(SlipCardinalNeighbors(zone, zone.GetEntityCell(lamp)).Any(cell => cell != null
                    && before.TryGetValue(cell, out float dark) && light.GetBrightness(cell.X, cell.Y) > dark + 0.0001f));
            Assert.AreEqual(west, light.GetBrightness(well.X - 1, well.Y + 3), 0.0001f);
            Assert.Greater(ordinaryLight, zone.AmbientLevel);
            Assert.AreEqual(ordinaryLight, light.GetBrightness(ordinary.X, ordinary.Y), 0.0001f);
        }

        private static Zone AttachOlderCachedSlip(OverworldZoneManager manager)
        {
            var zone = new Zone(SlipZone) { AmbientLevel = 0.1f };
            for (int x = 36; x <= 54; x++)
                for (int y = 8; y <= 16; y++)
                    Assert.True(zone.AddEntity(factory.CreateEntity("Grass"), x, y));
            var well = factory.CreateEntity("Well");
            well.Properties["SettlementId"] = SlipZone;
            well.Properties["SettlementSiteId"] = SettlementSiteDefinitions.MainWellSiteId;
            well.AddPart(new WellSitePart { SettlementId = SlipZone, SiteId = SettlementSiteDefinitions.MainWellSiteId });
            Assert.True(zone.AddEntity(well, 40, 12));
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { SlipZone, zone } }, SlipZone, null);
            return zone;
        }

        [Test]
        public void OlderCachedSlipGetsOneLampPairAndCannotBorrowAnotherWorldsOil()
        {
            var supplied = new OverworldZoneManager(factory, 65);
            supplied.SettlementManager.SetCondition(SlipZone, SlipLamps, true);
            var suppliedZone = AttachOlderCachedSlip(supplied);
            Assert.AreSame(suppliedZone, supplied.GetZone(SlipZone));
            var suppliedLamps = suppliedZone.GetAllEntities().Where(entity => entity.BlueprintName == SlipEastLamp).ToArray();
            Assert.That(suppliedLamps, Has.Length.EqualTo(2));
            Assert.AreSame(suppliedZone, supplied.GetZone(SlipZone));
            CollectionAssert.AreEquivalent(suppliedLamps, suppliedZone.GetAllEntities().Where(entity => entity.BlueprintName == SlipEastLamp));

            var other = OverworldZoneManager.CreateDetached(factory, 65, enableExploration: true);
            var otherZone = AttachOlderCachedSlip(other);
            Assert.AreSame(otherZone, other.GetZone(SlipZone));
            Assert.That(otherZone.GetAllEntities().Count(entity => entity.BlueprintName == SlipEastLamp), Is.EqualTo(2));
            Assert.AreSame(supplied.SettlementManager, SettlementManager.Current);
            Assert.False(other.SettlementManager.HasExistingCondition(SlipZone, SlipLamps));

            var sample = suppliedZone.GetEntityCell(suppliedLamps[0]);
            var light = new LightMap();
            light.Compute(suppliedZone);
            Assert.Greater(light.GetBrightness(sample.X, sample.Y), suppliedZone.AmbientLevel);

            var state = supplied.SettlementManager.GetAllSettlementsSnapshot()[SlipZone];
            state.SetCondition(SlipLamps, false);
            light.Compute(suppliedZone);
            Assert.AreEqual(suppliedZone.AmbientLevel, light.GetBrightness(sample.X, sample.Y), 0.0001f);
            state.SetCondition(SlipLamps, true);
            light.Compute(suppliedZone);
            Assert.Greater(light.GetBrightness(sample.X, sample.Y), suppliedZone.AmbientLevel);

            light.Compute(otherZone);
            Assert.AreEqual(otherZone.AmbientLevel, light.GetBrightness(sample.X, sample.Y), 0.0001f);
        }

        [Test] public void SlipsEastWallIsReadAloud()
        {
            var slip = Place(SlipZone, "Warden"); var warden = Who(slip, "Warden");
            Assert.IsNull(ConcernConversation.Accept(warden, player, "slip.east"));
            ReadPlaced(slip, "slip-eastwall");
            Tick(); Assert.AreEqual(2, Approach("slip.east"));
            Assert.True(Holds(SlipZone, "Slip.NamesSaid"));
            Assert.False(Holds(SlipZone, SlipLamps));
            StringAssert.Contains("read out every name", PlaceConsequences.LineFor(SumpZone));
        }
    }
}
