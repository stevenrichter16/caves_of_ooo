using System.IO;
using System.Linq;
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

        [Test] public void TheWeirRebuiltByHandFeedsTine()
        {
            var tine = Place(TineZone, "Elder", "Merchant"); var elder = Who(tine, "Elder");
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

        [Test] public void SlipsEastWallIsReadAloud()
        {
            var slip = Place(SlipZone, "Warden"); var warden = Who(slip, "Warden");
            Assert.IsNull(ConcernConversation.Accept(warden, player, "slip.east"));
            ReadPlaced(slip, "slip-eastwall");
            Tick(); Assert.AreEqual(2, Approach("slip.east"));
            Assert.True(Holds(SlipZone, "Slip.NamesSaid"));
            StringAssert.Contains("read out every name", PlaceConsequences.LineFor(SumpZone));
        }
    }
}
