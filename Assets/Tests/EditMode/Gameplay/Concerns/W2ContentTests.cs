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
    /// W2 (Docs/PLACE-WORK-AND-PROGRESSION.md, content waves): the tier-2 concerns of
    /// Wellmeet, the Salt-Vault, the Drowned Ledger and the Quiet's Door, every approach
    /// played end to end through the real conversation, site, document, cargo, foe,
    /// reputation and consequence systems. Each approach leaves its own place behind.
    /// </summary>
    public sealed class W2ContentTests
    {
        static EntityFactory factory;
        StoryletPart storylets;
        Entity player;
        int turn = 10;
        const string WellmeetZone = "Overworld.8.16.0", FirstTentZone = "Overworld.5.17.0", TallyZone = "Overworld.10.14.0",
            GantryZone = "Overworld.7.8.0", VaultZone = "Overworld.15.15.0", MarrowZone = "Overworld.12.12.0",
            LedgerZone = "Overworld.17.5.0", QuillZone = "Overworld.14.9.0", QuietZone = "Overworld.16.1.0";

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
            FactionManager.Initialize(Resources.Load<TextAsset>("Content/Data/Factions").text); // authored standings: raiders hostile
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
        Zone Visit(string zoneId)
        {
            var zone = new Zone(zoneId); current.RemoveEntity(player); zone.AddEntity(player, 2, 12); current = zone;
            ConcernWorld.OnZoneEntered(player, zone, arriving: true);
            return zone;
        }
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
        static bool HasChoice(NodeData n, string text) => n.Choices.Any(c => c.Text.Contains(text));
        void Take(Entity speaker, NodeData n, string text) => Assert.True(ConversationActions.TryExecuteAll(Choice(n, text).Actions, speaker, player), text);
        NodeData Status(Entity owner, string id) => ConcernConversation.BuildNode("status|" + id, owner, player);
        static Entity[] Foes(Zone zone) => zone.GetAllEntities().Where(e => e.GetProperty(ConcernWorld.FoeProperty) != null).ToArray();
        void KillAll(Zone zone) { foreach (var f in Foes(zone)) f.FireEvent(GameEvent.New("Died")); Tick(); }

        // ════════════════ the wave as a whole ════════════════

        [Test] public void EveryWaveConcernIsTierTwoInItsPlaceAndLintsClean()
        {
            var wave = new[] { "wellmeet.guest", "wellmeet.scales", "saltvault.lastwords", "ledger.stakes", "quiet.dim" };
            foreach (var id in wave)
            {
                var c = ConcernRegistry.Get(id);
                Assert.NotNull(c, id);
                Assert.AreEqual(2, c.Tier, id);
                var place = WorldMapAuthoring.Places.First(p => p.Name == c.Place);
                Assert.AreEqual(2, WorldMapAuthoring.TierAt(place.X, place.Y), id + " sits at a tier-2 place");
                CollectionAssert.IsEmpty(ConcernRegistry.Lint(c, factory), id);
                Assert.GreaterOrEqual(c.Reward.XP, 1000, id + " pays its share of the tier-2 budget");
            }
        }

        // ════════════════ Wellmeet: Under the Cloth ════════════════

        [Test] public void StandingAtTheWashMeetsHarlsKinByName()
        {
            var wellmeet = Place(WellmeetZone, "TentRightHost"); var host = Who(wellmeet, "TentRightHost");
            Assert.IsNull(ConcernConversation.Accept(host, player, "wellmeet.guest"));
            Assert.AreEqual("Overworld.8.17.0", ConcernWorld.SiteOf("wellmeet.guest", "stand"), "the dry wash south of town");
            var wash = Visit("Overworld.8.17.0");
            var kin = Foes(wash);
            Assert.AreEqual(3, kin.Length);
            Assert.True(kin.All(k => k.GetDisplayName() == "Harl's kin" && k.BlueprintName == "DesertBandit"));
            Assert.True(kin.All(k => FactionManager.IsHostile(k, player)));
            KillAll(wash);
            Assert.AreEqual(1, Approach("wellmeet.guest"));
            Assert.True(Holds(WellmeetZone, "Wellmeet.KinFaced"));
            Assert.AreEqual(6, PlayerReputation.Get("TentRight"));
            MoveTo(wellmeet);
            int drams = TradeSystem.GetDrams(player);
            Assert.IsNull(ConcernConversation.Report(host, player, "wellmeet.guest"));
            Assert.AreEqual(drams, TradeSystem.GetDrams(player), "the oath is not paid for");
            StringAssert.Contains("dry wash", PlaceConsequences.LineFor(FirstTentZone), "the First Tent has heard");
        }

        [Test] public void AGuestWhoCarriesABladeToTheWashLosesTheirOwnCloth()
        {
            var wellmeet = Place(WellmeetZone, "TentRightHost"); var host = Who(wellmeet, "TentRightHost");
            StringAssert.Contains("cannot carry a blade out to them", ConcernRegistry.Get("wellmeet.guest").Pitch, "the host warns, as law");
            Assert.IsNull(ConcernConversation.Accept(host, player, "wellmeet.guest"));
            player.ApplyEffect(new UnderTheClothEffect(), host, null);
            var wash = Visit("Overworld.8.17.0");
            var kin = Foes(wash);
            Assert.False(FactionManager.IsHostile(kin[0], player), "under the cloth, the kin hold off");
            CombatSystem.PerformMeleeAttack(player, kin[0], wash, new System.Random(3));
            Assert.False(player.HasEffect<UnderTheClothEffect>(), "the cloth comes off the one who strikes");
            Assert.AreEqual(UnderTheClothEffect.OathbreakRepLoss, PlayerReputation.Get("TentRight"));
            Assert.True(FactionManager.IsHostile(kin[1], player), "and now they are free to answer");
        }

        [Test] public void WithoutACloth_StrikingTheKinIsNoOathbreak()
        {
            var wellmeet = Place(WellmeetZone, "TentRightHost"); var host = Who(wellmeet, "TentRightHost");
            Assert.IsNull(ConcernConversation.Accept(host, player, "wellmeet.guest"));
            var wash = Visit("Overworld.8.17.0");
            CombatSystem.PerformMeleeAttack(player, Foes(wash)[0], wash, new System.Random(3));
            Assert.AreEqual(0, PlayerReputation.Get("TentRight"), "counter-check: no cloth, no oath to break");
        }

        [Test] public void TheBloodPriceIsPaidNotTheOath()
        {
            var wellmeet = Place(WellmeetZone, "TentRightHost"); var host = Who(wellmeet, "TentRightHost");
            Assert.IsNull(ConcernConversation.Accept(host, player, "wellmeet.guest"));
            Take(host, Status(host, "wellmeet.guest"), "blood-price");
            Assert.AreEqual(155, TradeSystem.GetDrams(player));
            Tick(); Assert.AreEqual(2, Approach("wellmeet.guest"));
            Assert.True(Holds(WellmeetZone, "Wellmeet.BloodPricePaid")); Assert.False(Holds(WellmeetZone, "Wellmeet.KinFaced"));
            Assert.AreEqual(2, PlayerReputation.Get("TentRight"), "less than standing for her");
            StringAssert.Contains("blood-debt", PlaceConsequences.LineFor(TallyZone), "Tally approves");
        }

        [Test] public void TheBloodPriceCannotBePaidShort()
        {
            var wellmeet = Place(WellmeetZone, "TentRightHost"); var host = Who(wellmeet, "TentRightHost");
            Assert.IsNull(ConcernConversation.Accept(host, player, "wellmeet.guest"));
            TradeSystem.SetDrams(player, 44);
            Assert.False(ConversationActions.TryExecuteAll(Choice(Status(host, "wellmeet.guest"), "blood-price").Actions, host, player));
            Tick();
            Assert.AreEqual(0, Approach("wellmeet.guest")); Assert.AreEqual(44, TradeSystem.GetDrams(player), "nothing taken");
        }

        [Test] public void HerWordReachesTheKeepersAndAKeeperComesToWellmeet()
        {
            var wellmeet = Place(WellmeetZone, "TentRightHost"); var host = Who(wellmeet, "TentRightHost");
            Assert.IsNull(ConcernConversation.Accept(host, player, "wellmeet.guest"));
            Take(host, Status(host, "wellmeet.guest"), "I'll carry");
            var tent = Place(FirstTentZone, "TentRightHost");
            Assert.IsNull(ConcernConversation.DeliverCargo(Who(tent, "TentRightHost"), player, "wellmeet.guest|keepers"));
            Tick(); Assert.AreEqual(3, Approach("wellmeet.guest"));
            Assert.True(Holds(WellmeetZone, "Wellmeet.KeepersSent")); Assert.AreEqual(4, PlayerReputation.Get("TentRight"));
            MoveTo(wellmeet);
            int hosts = wellmeet.GetAllEntities().Count(e => e.BlueprintName == "TentRightHost");
            PlaceConsequences.OnZoneEntered(player, wellmeet, arriving: true);
            Assert.AreEqual(hosts + 1, wellmeet.GetAllEntities().Count(e => e.BlueprintName == "TentRightHost"), "a keeper at the pole");
        }

        // ════════════════ Wellmeet: Short Weight ════════════════

        [Test] public void TheScalesTrueForGoodOnlyWithIron()
        {
            var wellmeet = Place(WellmeetZone, "SaltMaster", "Merchant"); var master = Who(wellmeet, "SaltMaster");
            Assert.IsNull(ConcernConversation.Accept(master, player, "wellmeet.scales"));
            Give("SilverSand", 2);
            Take(master, Status(master, "wellmeet.scales"), "Patch the pan scales");
            Tick(); Assert.AreEqual(0, Approach("wellmeet.scales"), "sand holds for now; it is not true");
            Give("IronSpikeComponent", 2); Give("LeatherBindingComponent", 1);
            Take(master, Status(master, "wellmeet.scales"), "Repair the pan scales");
            Tick(); Assert.AreEqual(1, Approach("wellmeet.scales"));
            Assert.True(Holds(WellmeetZone, "Wellmeet.ScalesTrue"));
            Assert.AreEqual(0.97, PlaceConsequences.PriceModifier(Who(wellmeet, "Merchant")), 1e-9);
            Assert.IsNull(ConcernConversation.Report(master, player, "wellmeet.scales"));
            Assert.AreEqual(2, ConcernItems.CountCarried(player, "PaleSalt"), "paid in Pale-Salt");
        }

        [Test] public void TheTareBookAtTallyValuesTheGuestRight()
        {
            var wellmeet = Place(WellmeetZone, "SaltMaster", "Merchant"); var master = Who(wellmeet, "SaltMaster");
            Assert.IsNull(ConcernConversation.Accept(master, player, "wellmeet.scales"));
            var tally = Place(TallyZone, "Elder");
            ConcernWorld.OnZoneEntered(player, tally, arriving: true);
            ReadPlaced(tally, "wellmeet-tarebook");
            Tick(); Assert.AreEqual(2, Approach("wellmeet.scales"));
            Assert.True(Holds(WellmeetZone, "Wellmeet.TareRead")); Assert.False(Holds(WellmeetZone, "Wellmeet.ScalesTrue"));
            Assert.AreEqual(4, PlayerReputation.Get("TentRight")); Assert.AreEqual(-3, PlayerReputation.Get("SaccharineConcord"));
            Assert.AreEqual(0.95, PlaceConsequences.PriceModifier(Who(wellmeet, "Merchant")), 1e-9);
            StringAssert.Contains("tare-book", PlaceConsequences.LineFor(GantryZone), "Gantry has heard");
            StringAssert.Contains("guest-right", ReadableDocumentCatalog.Get("wellmeet-tarebook").Text);
        }

        // ════════════════ the Salt-Vault: Unrecorded ════════════════

        [Test] public void SheIsFiledUnderWhatSheWroteAtWellmeet()
        {
            var vault = Place(VaultZone, "StillleafIndexer"); var indexer = Who(vault, "StillleafIndexer");
            Assert.IsNull(ConcernConversation.Accept(indexer, player, "saltvault.lastwords"));
            var wellmeet = Place(WellmeetZone, "Innkeeper");
            ConcernWorld.OnZoneEntered(player, wellmeet, arriving: true);
            ReadPlaced(wellmeet, "saltvault-lodgingslate");
            Tick(); Assert.AreEqual(1, Approach("saltvault.lastwords"));
            Assert.True(Holds(VaultZone, "SaltVault.FiledWritten")); Assert.False(Holds(VaultZone, "SaltVault.FiledSpoken"));
            Assert.AreEqual(3, PlayerReputation.Get("PaleCuration"));
        }

        [Test] public void OrUnderWhatSheSaidToTheInnkeeper()
        {
            var vault = Place(VaultZone, "StillleafIndexer"); var indexer = Who(vault, "StillleafIndexer");
            Assert.IsNull(ConcernConversation.Accept(indexer, player, "saltvault.lastwords"));
            Take(indexer, Status(indexer, "saltvault.lastwords"), "I'll carry");
            var wellmeet = Place(WellmeetZone, "Innkeeper");
            Assert.IsNull(ConcernConversation.DeliverCargo(Who(wellmeet, "Innkeeper"), player, "saltvault.lastwords|witness"));
            Tick(); Assert.AreEqual(2, Approach("saltvault.lastwords"));
            Assert.True(Holds(VaultZone, "SaltVault.FiledSpoken")); Assert.AreEqual(5, PlayerReputation.Get("PaleCuration"));
            StringAssert.Contains("last words", PlaceConsequences.LineFor(MarrowZone), "Marrowstye has heard");
        }

        // ════════════════ the Drowned Ledger: Between the Lines ════════════════

        [Test] public void TheSurveyLineIsRedrivenWithOakNotWillow()
        {
            var ledger = Place(LedgerZone, "CurationSorter"); var sorter = Who(ledger, "CurationSorter");
            Assert.IsNull(ConcernConversation.Accept(sorter, player, "ledger.stakes"));
            Give("WillowHaftComponent", 2);
            Take(sorter, Status(sorter, "ledger.stakes"), "Patch the survey line");
            Tick(); Assert.AreEqual(0, Approach("ledger.stakes"), "willow bends back");
            Give("OakHaftComponent", 3); Give("LeatherBindingComponent", 1);
            Take(sorter, Status(sorter, "ledger.stakes"), "Repair the survey line");
            Tick(); Assert.AreEqual(1, Approach("ledger.stakes"));
            Assert.True(Holds(LedgerZone, "Ledger.LineRedriven"));
        }

        [Test] public void TheMawToadsInTheCutAreWhatHeavesThePeat()
        {
            var ledger = Place(LedgerZone, "CurationSorter"); var sorter = Who(ledger, "CurationSorter");
            Assert.IsNull(ConcernConversation.Accept(sorter, player, "ledger.stakes"));
            Assert.AreEqual("Overworld.16.5.0", ConcernWorld.SiteOf("ledger.stakes", "cut"));
            var cut = Visit("Overworld.16.5.0");
            Assert.AreEqual(2, Foes(cut).Count(f => f.BlueprintName == "MawToad"));
            KillAll(cut);
            Assert.AreEqual(2, Approach("ledger.stakes"));
            Assert.True(Holds(LedgerZone, "Ledger.CutQuiet"));
            StringAssert.Contains("the flooded cut west of the Drowned Ledger", Status(sorter, "ledger.stakes").Text);
        }

        [Test] public void TheLineIsSetByTheTenthsSheetAndQuillholdHears()
        {
            var ledger = Place(LedgerZone, "CurationSorter"); var sorter = Who(ledger, "CurationSorter");
            Assert.IsNull(ConcernConversation.Accept(sorter, player, "ledger.stakes"));
            ConcernWorld.OnZoneEntered(player, ledger, arriving: true);
            ReadPlaced(ledger, "ledger-tenthsurvey");
            Tick(); Assert.AreEqual(3, Approach("ledger.stakes"));
            Assert.True(Holds(LedgerZone, "Ledger.SetByTenth")); Assert.AreEqual(4, PlayerReputation.Get("Palimpsest"));
            StringAssert.Contains("tenth recension", PlaceConsequences.LineFor(QuillZone));
        }

        // ════════════════ the Quiet's Door: Before the Dim ════════════════

        [Test] public void TheQuietWithholdTheirTroubleFromStrangers()
        {
            var quiet = Place(QuietZone, "Elder"); var elder = Who(quiet, "Elder");
            var c = ConcernRegistry.Get("quiet.dim");
            Assert.AreEqual(ConcernConversation.State.Withheld, ConcernConversation.StateOf(c, elder, player));
            StringAssert.Contains("Upstairs", ConcernConversation.BuildNode("withheld|quiet.dim", elder, player).Text);
            Assert.AreEqual(QuestCueState.None, ConcernConversation.CueFor(elder, quiet, player));
            Assert.AreEqual("not_offerable", ConcernConversation.Accept(elder, player, "quiet.dim"));

            PlayerReputation.Set("CatacombFolk", PlayerReputation.LIKED_THRESHOLD);
            Assert.AreEqual(QuestCueState.Available, ConcernConversation.CueFor(elder, quiet, player), "counter-check: liked, it is offered");
            Assert.IsNull(ConcernConversation.Accept(elder, player, "quiet.dim"));
        }

        [Test] public void UpstairsOilServesTheDim()
        {
            PlayerReputation.Set("CatacombFolk", PlayerReputation.LIKED_THRESHOLD);
            var quiet = Place(QuietZone, "Elder"); var elder = Who(quiet, "Elder");
            Assert.IsNull(ConcernConversation.Accept(elder, player, "quiet.dim"));
            Give("WardOil", 3); Take(elder, Status(elder, "quiet.dim"), "Hand over 3");
            Tick(); Assert.AreEqual(2, Approach("quiet.dim"));
            Assert.True(Holds(QuietZone, "Quiet.WardFromOil"));
            Assert.AreEqual(PlayerReputation.LIKED_THRESHOLD + 3, PlayerReputation.Get("CatacombFolk"));
        }

        [Test] public void TheSariAtTheLipGiveProperWard()
        {
            PlayerReputation.Set("CatacombFolk", PlayerReputation.LIKED_THRESHOLD);
            var quiet = Place(QuietZone, "Elder"); var elder = Who(quiet, "Elder");
            Assert.IsNull(ConcernConversation.Accept(elder, player, "quiet.dim"));
            Assert.AreEqual("Overworld.17.1.0", ConcernWorld.SiteOf("quiet.dim", "venom"));
            var lip = Visit("Overworld.17.1.0");
            Assert.AreEqual(3, Foes(lip).Count(f => f.BlueprintName == "SariSnake"));
            KillAll(lip);
            Assert.AreEqual(1, Approach("quiet.dim"));
            Assert.True(Holds(QuietZone, "Quiet.WardFromVenom"));
            Assert.AreEqual(PlayerReputation.LIKED_THRESHOLD + 8, PlayerReputation.Get("CatacombFolk"));
        }
    }
}
