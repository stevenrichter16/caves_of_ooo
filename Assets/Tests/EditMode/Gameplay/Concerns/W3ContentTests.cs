using System.IO;
using System.Linq;
using CavesOfOoo.Concerns;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W3 (Docs/PLACE-WORK-AND-PROGRESSION.md, content waves): the tier-3 concerns of the
    /// First Tent, the Last Counter and Cinderhold, every approach played end to end
    /// through the real conversation, cloth, site, document, foe, supply and consequence
    /// systems. The First Tent's Host approach is settled by the sworn player's own cloth.
    /// </summary>
    public sealed class W3ContentTests
    {
        static EntityFactory factory;
        StoryletPart storylets;
        Entity player;
        int turn = 10;
        internal const string FirstTentZone = "Overworld.5.17.0", WellmeetZone = "Overworld.8.16.0", TallyZone = "Overworld.10.14.0",
            CounterZone = "Overworld.18.18.0", OldCounterZone = "Overworld.19.18.0", CinderZone = "Overworld.6.6.0",
            NorthFieldZone = "Overworld.6.5.0", MorrowfastZone = "Overworld.3.6.0";

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup()
        {
            PlaceSiteCatalog.Reset(); PlaceConditionCatalog.Reset(); ReadableDocumentCatalog.ResetForTests();
            StoryletRegistry.Reset(); ConcernRegistry.LoadAll();
            MessageLog.Clear(); Diag.ResetAll(); ConversationActions.Factory = factory; PlayerReputation.Reset();
            FactionManager.Initialize(Resources.Load<TextAsset>("Content/Data/Factions").text);
            SkillRegistry.ResetForTests(); SkillRegistry.EnsureInitialized();
            storylets = new StoryletPart(); StoryletPart.Current = storylets;
            NarrativeStatePart.Current = new NarrativeStatePart();
            new SettlementManager(() => turn, id => PlaceSiteCatalog.PlaceAt(id) is string name ? new PointOfInterest(POIType.Village, name) : null);
            player = factory.CreateEntity("Player"); StoryletPart.LocalPlayer = player;
            TradeSystem.SetDrams(player, 300);
        }
        [TearDown] public void Cleanup()
        {
            ConversationManager.EndConversation(); PlaceSiteCatalog.Reset(); PlaceConditionCatalog.Reset();
            StoryletRegistry.Reset(); ConcernRegistry.Reset(); StoryletPart.Current = null; StoryletPart.LocalPlayer = null;
            NarrativeStatePart.Current = null; SettlementManager.ResetCurrent(); ConversationActions.Factory = null;
            SkillRegistry.ResetForTests();
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
        void Take(Entity speaker, NodeData n, string text) => Assert.True(ConversationActions.TryExecuteAll(Choice(n, text).Actions, speaker, player), text);
        NodeData Status(Entity owner, string id) => ConcernConversation.BuildNode("status|" + id, owner, player);
        static Entity[] Foes(Zone zone) => zone.GetAllEntities().Where(e => e.GetProperty(ConcernWorld.FoeProperty) != null).ToArray();
        void KillAll(Zone zone) { foreach (var f in Foes(zone)) f.FireEvent(GameEvent.New("Died")); Tick(); }
        static Entity Guest(Zone zone) => zone.GetAllEntities().FirstOrDefault(e => e.GetProperty(ConcernWorld.GuestProperty) != null);
        void Swear(Entity host) { H1OathTests.Like("TentRight"); Assert.IsNull(TentRightOath.Swear(player, host)); }
        bool Extend(Zone zone, Entity target)
        {
            var at = zone.GetEntityPosition(target);
            zone.MoveEntity(player, at.x - 1 >= 2 ? at.x - 1 : at.x + 1, at.y);
            var skill = player.GetPart<SkillsPart>().GetSkill(TentRightOath.PowerClass)
                ?? (BaseSkillPart)new Hospitality_ExtendCloth();
            if (!player.GetPart<SkillsPart>().HasSkill(TentRightOath.PowerClass)) player.GetPart<SkillsPart>().AddSkill(skill);
            return skill.OnCommand(new SkillEventContext { Attacker = player, Zone = zone, TargetCell = zone.GetEntityCell(target) });
        }

        // ════════════════ the wave as a whole ════════════════

        [Test] public void EveryWaveConcernIsTierThreeInItsPlaceAndLintsClean()
        {
            foreach (var id in new[] { "firsttent.clan", "lastcounter.retreat", "cinderhold.pump" })
            {
                var c = ConcernRegistry.Get(id);
                Assert.NotNull(c, id);
                Assert.AreEqual(3, c.Tier, id);
                var place = WorldMapAuthoring.Places.First(p => p.Name == c.Place);
                Assert.AreEqual(3, WorldMapAuthoring.TierAt(place.X, place.Y), id + " sits at a tier-3 place");
                CollectionAssert.IsEmpty(ConcernRegistry.Lint(c, factory), id);
                Assert.GreaterOrEqual(c.Reward.XP, 2000, id + " pays its share of the tier-3 budget");
            }
        }

        // ════════════════ the First Tent: the clan at the poles ════════════════

        [Test] public void ASwornHostTakesTheCarrierUnderTheirOwnCloth()
        {
            var tent = Place(FirstTentZone, "TentRightHost"); var host = Who(tent, "TentRightHost");
            Swear(host);
            int standing = PlayerReputation.Get("TentRight");
            Assert.IsNull(ConcernConversation.Accept(host, player, "firsttent.clan"));
            Assert.GreaterOrEqual(ConcernWorld.OnZoneEntered(player, tent, arriving: true), 1, "the carrier is there while the work is open");
            var carrier = Guest(tent);
            Assert.NotNull(carrier);
            Assert.AreEqual("the salt-carrier", carrier.GetDisplayName());
            Assert.True(Extend(tent, carrier));
            Assert.AreEqual(player.ID, carrier.GetEffect<UnderTheClothEffect>().HostId);
            Tick(); Assert.AreEqual(1, Approach("firsttent.clan"));
            Assert.True(Holds(FirstTentZone, "FirstTent.CarriedOut"));
            Assert.AreEqual(standing + 10, PlayerReputation.Get("TentRight"));
            Assert.IsNull(ConcernConversation.Report(host, player, "firsttent.clan"));
            Assert.AreEqual(standing + 20, PlayerReputation.Get("TentRight"), "and the reward's standing on top");
            StringAssert.Contains("second cloth", PlaceConsequences.LineFor(WellmeetZone), "Wellmeet has heard");
        }

        [Test] public void AnUnswornPlayerCannotTakeHerAndNoOtherGuestCounts()
        {
            var tent = Place(FirstTentZone, "TentRightHost", "Villager"); var host = Who(tent, "TentRightHost");
            Assert.IsNull(ConcernConversation.Accept(host, player, "firsttent.clan"));
            ConcernWorld.OnZoneEntered(player, tent, arriving: true);
            var carrier = Guest(tent);
            Assert.False(Extend(tent, carrier), "counter-check: the power without the oath is empty");
            Tick(); Assert.AreEqual(0, Approach("firsttent.clan"));
            Swear(host);
            var stranger = tent.GetAllEntities().First(e => e.BlueprintName == "Villager" && e != carrier);
            Assert.True(Extend(tent, stranger));
            Tick(); Assert.AreEqual(0, Approach("firsttent.clan"), "counter-check: hosting someone else settles nothing");
        }

        [Test] public void TheCarrierIsPlacedOnceAndNotReplacedIfSheDies()
        {
            var tent = Place(FirstTentZone, "TentRightHost"); var host = Who(tent, "TentRightHost");
            Assert.IsNull(ConcernConversation.Accept(host, player, "firsttent.clan"));
            ConcernWorld.OnZoneEntered(player, tent, arriving: true);
            ConcernWorld.OnZoneEntered(player, tent, arriving: true);
            Assert.AreEqual(1, tent.GetAllEntities().Count(e => e.GetProperty(ConcernWorld.GuestProperty) != null), "one carrier, however often you arrive");
            tent.RemoveEntity(Guest(tent));
            ConcernWorld.OnZoneEntered(player, tent, arriving: true);
            Assert.IsNull(Guest(tent), "a carrier who is gone stays gone; the other ways remain");
        }

        [Test] public void TheClanIsPaidForTheSalt()
        {
            var tent = Place(FirstTentZone, "TentRightHost"); var host = Who(tent, "TentRightHost");
            Assert.IsNull(ConcernConversation.Accept(host, player, "firsttent.clan"));
            Take(host, Status(host, "firsttent.clan"), "120 drams");
            Assert.AreEqual(180, TradeSystem.GetDrams(player));
            Tick(); Assert.AreEqual(2, Approach("firsttent.clan"));
            Assert.True(Holds(FirstTentZone, "FirstTent.ContractPaid")); Assert.False(Holds(FirstTentZone, "FirstTent.CarriedOut"));
            StringAssert.Contains("noted the rate", PlaceConsequences.LineFor(TallyZone), "Tally approves, in its way");
        }

        [Test] public void TheClanCannotBePaidShort()
        {
            var tent = Place(FirstTentZone, "TentRightHost"); var host = Who(tent, "TentRightHost");
            Assert.IsNull(ConcernConversation.Accept(host, player, "firsttent.clan"));
            TradeSystem.SetDrams(player, 119);
            Assert.False(ConversationActions.TryExecuteAll(Choice(Status(host, "firsttent.clan"), "120 drams").Actions, host, player));
            Tick(); Assert.AreEqual(0, Approach("firsttent.clan")); Assert.AreEqual(119, TradeSystem.GetDrams(player));
        }

        [Test] public void TheFirstCordIsReadToTheClan()
        {
            var tent = Place(FirstTentZone, "TentRightHost"); var host = Who(tent, "TentRightHost");
            Assert.IsNull(ConcernConversation.Accept(host, player, "firsttent.clan"));
            ConcernWorld.OnZoneEntered(player, tent, arriving: true);
            ReadPlaced(tent, "firsttent-cord");
            Tick(); Assert.AreEqual(3, Approach("firsttent.clan"));
            Assert.True(Holds(FirstTentZone, "FirstTent.PrecedentRead"));
            StringAssert.Contains("brother", ReadableDocumentCatalog.Get("firsttent-cord").Text, "the precedent is the oath over blood");
        }

        // ════════════════ the Last Counter: the third retreat ════════════════

        [Test] public void TheFirstCountersLedgerLiesInTheEmptyPostNotAtTheCounter()
        {
            var counter = Place(CounterZone, "SaccharineEnvoy"); var envoy = Who(counter, "SaccharineEnvoy");
            Assert.IsNull(ConcernConversation.Accept(envoy, player, "lastcounter.retreat"));
            ConcernWorld.OnZoneEntered(player, counter, arriving: true);
            Assert.False(counter.GetAllEntities().Any(e => e.GetPart<ReadableDocumentPart>()?.DocumentId == "lastcounter-ledger"),
                "counter-check: the ledger is not at the Counter itself");
            var old = Visit(OldCounterZone);
            ReadPlaced(old, "lastcounter-ledger");
            Tick(); Assert.AreEqual(1, Approach("lastcounter.retreat"));
            Assert.True(Holds(CounterZone, "LastCounter.LedgerRead"));
            StringAssert.Contains("rate is under review", PlaceConsequences.LineFor(TallyZone));
        }

        [Test] public void WhatNestsInTheFirstCounterIsCleared()
        {
            var counter = Place(CounterZone, "SaccharineEnvoy"); var envoy = Who(counter, "SaccharineEnvoy");
            Assert.IsNull(ConcernConversation.Accept(envoy, player, "lastcounter.retreat"));
            Assert.AreEqual(OldCounterZone, ConcernWorld.SiteOf("lastcounter.retreat", "road"), "it nests in the first abandoned counter");
            var old = Visit(OldCounterZone);
            Assert.AreEqual(2, Foes(old).Count(f => f.BlueprintName == "DuneLurker"));
            Assert.AreEqual(1, Foes(old).Count(f => f.BlueprintName == "BrittleHound"));
            KillAll(old);
            Assert.AreEqual(2, Approach("lastcounter.retreat"));
            Assert.True(Holds(CounterZone, "LastCounter.Held"));
            MoveTo(counter);
            PlaceConsequences.OnZoneEntered(player, counter, arriving: true);
            Assert.GreaterOrEqual(ConcernItems.CountCarried(envoy, "Panacea"), 1, "expedition goods on the shelf");
            Assert.AreEqual(0.95, PlaceConsequences.PriceModifier(envoy), 1e-9);
        }

        [Test] public void TheCounterCanBePaidToStayAndPassesTheSurchargeOn()
        {
            var counter = Place(CounterZone, "SaccharineEnvoy"); var envoy = Who(counter, "SaccharineEnvoy");
            Assert.IsNull(ConcernConversation.Accept(envoy, player, "lastcounter.retreat"));
            Take(envoy, Status(envoy, "lastcounter.retreat"), "continuity surcharge");
            Assert.AreEqual(150, TradeSystem.GetDrams(player));
            Tick(); Assert.AreEqual(3, Approach("lastcounter.retreat"));
            Assert.True(Holds(CounterZone, "LastCounter.Underwritten"));
            Assert.AreEqual(1.1, PlaceConsequences.PriceModifier(envoy), 1e-9, "you paid for it, so you pay for it");
        }

        // ════════════════ Cinderhold: the drowned level ════════════════

        [Test] public void ThePumpTurnsForGoodOnlyWithIronAndOak()
        {
            var town = Place(CinderZone, "Elder", "Quartermaster"); var elder = Who(town, "Elder");
            Assert.IsNull(ConcernConversation.Accept(elder, player, "cinderhold.pump"));
            Give("FireClay", 2);
            Take(elder, Status(elder, "cinderhold.pump"), "Patch the mine pump");
            Tick(); Assert.AreEqual(0, Approach("cinderhold.pump"), "clay turns it for now; it is not mended");
            Give("IronSpikeComponent", 2); Give("OakHaftComponent", 1); Give("LeatherBindingComponent", 1);
            Take(elder, Status(elder, "cinderhold.pump"), "Repair the mine pump");
            Tick(); Assert.AreEqual(1, Approach("cinderhold.pump"));
            Assert.True(Holds(CinderZone, "Cinderhold.PumpRunning"));
            Assert.AreEqual(0.95, PlaceConsequences.PriceModifier(Who(town, "Quartermaster")), 1e-9);
        }

        [Test] public void WhatCameUpAfterTheLadsIsInTheNorthField()
        {
            var town = Place(CinderZone, "Elder"); var elder = Who(town, "Elder");
            Assert.IsNull(ConcernConversation.Accept(elder, player, "cinderhold.pump"));
            Assert.AreEqual(NorthFieldZone, ConcernWorld.SiteOf("cinderhold.pump", "field"));
            var field = Visit(NorthFieldZone);
            Assert.AreEqual(3, Foes(field).Length);
            KillAll(field);
            Assert.AreEqual(2, Approach("cinderhold.pump"));
            Assert.True(Holds(CinderZone, "Cinderhold.LevelCleared"));
        }

        [Test] public void ClayingTheSeepDriesTheGroveSpringAndTheChoirNotices()
        {
            var town = Place(CinderZone, "Elder"); var elder = Who(town, "Elder");
            Assert.IsNull(ConcernConversation.Accept(elder, player, "cinderhold.pump"));
            Give("FireClay", 4); Take(elder, Status(elder, "cinderhold.pump"), "Hand over 4");
            Tick(); Assert.AreEqual(3, Approach("cinderhold.pump"));
            Assert.True(Holds(CinderZone, "Cinderhold.Sealed"));
            Assert.AreEqual(-3, PlayerReputation.Get("RotChoir"));
            StringAssert.Contains("trickle", PlaceConsequences.LineFor(MorrowfastZone), "Morrowfast has heard");
        }
    }
}
