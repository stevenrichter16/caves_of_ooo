using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Ending spine ES.4 (Docs/ENDING-SPINE.md): at the empty seventh position of the
    /// Felling-Site circle, two enactments are offered underfoot — be struck (always;
    /// its crack stated) or refuse aloud and name the world (a clean ledger; the
    /// costs stated). Each is once and for all; the open ledger is named, never
    /// written off; no ending is called best.
    /// </summary>
    public sealed class EndingSpineTests
    {
        private HotbarSaveFixture scope; private EntityFactory factory; private OverworldZoneManager manager; private Zone site; private Entity player, seventh;

        [SetUp] public void SetUp()
        {
            scope = new HotbarSaveFixture(false, false); factory = GrovelandsCompositionTests.Factory(); manager = OverworldZoneManager.CreateDetached(factory, 64);
            site = manager.GetZone(FellingSiteBuilder.ZoneID); Assert.IsTrue(FellingSceneRuntime.IsActive(site), "the circle is authored on generation");
            seventh = site.GetAllEntities().Single(e => e.HasPart<SeventhPositionPart>());
            player = factory.CreateEntity("Player"); var at = site.GetEntityPosition(seventh); Assert.IsTrue(site.AddEntity(player, at.x, at.y), "standing in the seventh position");
            StoryletPart.Current = new StoryletPart(); StoryletPart.LocalPlayer = player; SettlementRuntime.ActiveZone = site; NarrativeStatePart.Current = new NarrativeStatePart();
            Diag.ResetAll(); MessageLog.Clear();
        }
        [TearDown] public void TearDown() { StoryletPart.Current = null; StoryletPart.LocalPlayer = null; SettlementRuntime.ActiveZone = null; NarrativeStatePart.Current = null; scope.Dispose(); }

        private static InventoryActionList Offered(Entity target) { var list = new InventoryActionList(); var ev = GameEvent.New("GetInventoryActions"); ev.SetParameter("Actions", (object)list); target.FireEventAndRelease(ev); return list; }
        private bool Enact(string command, out int cost) => EndingSpine.TryWorldAction(seventh, player, site, command, out cost);
        private static void Start(string id) => StoryletPart.Current.StartQuest(new QuestState { QuestId = id, CurrentStageIndex = 0 });
        private static int Count(string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = "ending", Kind = kind, Limit = 40 }).Records.Count;
        private static string LastPayload(string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = "ending", Kind = kind, Limit = 40 }).Records.Last().PayloadJson;

        [Test]
        public void TheEmptySeventh_IsUnderfootInteractable_AndOffersBothEnactments()
        {
            Assert.IsTrue(seventh.HasTag("UnderfootInteractable")); Assert.IsFalse(string.IsNullOrEmpty(seventh.ID), "the underfoot pick needs an id");
            var offered = Offered(seventh).Actions.Select(a => a.Command).ToList();
            CollectionAssert.Contains(offered, EndingSpine.StrikeCommand); CollectionAssert.Contains(offered, EndingSpine.NameCommand);
            var bare = site.GetAllEntities().First(e => e.BlueprintName == "FellingBarePosition");
            Assert.IsFalse(Offered(bare).Actions.Any(a => EndingSpine.IsWorldCommand(a.Command)), "the six bare positions offer nothing");
            Assert.IsTrue(Diag.IsChannelEnabled("ending"));
        }

        [Test]
        public void BeingStruck_EnactsTheVesselPath_OnceAndForAll()
        {
            Assert.IsTrue(Enact(EndingSpine.StrikeCommand, out int cost)); Assert.AreEqual(EndingSpine.EnactCost, cost);
            Assert.AreEqual(EndingSpine.VesselPath, EndingSpine.Enacted(player)); Assert.AreEqual(EndingSpine.VesselPath, NarrativeStatePart.Current.GetFact(EndingSpine.EndingFact));
            Assert.IsTrue(MessageLog.HasPendingAnnouncement); string epilogue = MessageLog.ConsumeAnnouncement();
            StringAssert.Contains("installs a chooser", epilogue); StringAssert.Contains("That is the crack", epilogue);
            StringAssert.Contains("has a bearer", seventh.GetPart<ExaminablePart>().Text);
            Assert.IsFalse(seventh.GetPart<SeventhPositionPart>().TryAffectStandingPlayer(player, site), "the exposure ends");
            Assert.IsFalse(Offered(seventh).Actions.Any(a => EndingSpine.IsWorldCommand(a.Command)), "nothing more is offered");
            Assert.IsFalse(Enact(EndingSpine.StrikeCommand, out cost)); Assert.AreEqual(0, cost); StringAssert.Contains("already_enacted", LastPayload("Rejected"));
            Assert.IsFalse(Enact(EndingSpine.NameCommand, out _), "one enactment, ever");
            Assert.AreEqual(1, Count("Enacted")); StringAssert.Contains("\"path\":\"vessel\"", LastPayload("Enacted").Replace(" ", ""));
        }

        [Test]
        public void NamingTheWorld_NeedsACleanLedger_AndNamesWhatIsOpenInstead()
        {
            Start("errand");
            Assert.IsFalse(Enact(EndingSpine.NameCommand, out int cost)); Assert.AreEqual(0, cost);
            Assert.AreEqual(0, EndingSpine.Enacted(player)); Assert.IsFalse(MessageLog.HasPendingAnnouncement);
            StringAssert.Contains("ledger_open", LastPayload("Rejected"));
            string told = string.Join("\n", MessageLog.GetRecent(4)); StringAssert.Contains("leaving your own unspoken", told); StringAssert.Contains("errand", told);
            Assert.IsTrue(StoryletPart.Current.RefuseQuest("errand"), "a principled no is closure");
            Assert.IsTrue(Enact(EndingSpine.NameCommand, out cost)); Assert.AreEqual(EndingSpine.EnactCost, cost);
            Assert.AreEqual(EndingSpine.PracticePath, EndingSpine.Enacted(player)); Assert.AreEqual(EndingSpine.PracticePath, NarrativeStatePart.Current.GetFact(EndingSpine.EndingFact));
            string epilogue = MessageLog.ConsumeAnnouncement();
            StringAssert.Contains("wakes without fear", epilogue); StringAssert.Contains("never finishes", epilogue); StringAssert.Contains("begin to age", epilogue);
            StringAssert.Contains("answered, not filled", seventh.GetPart<ExaminablePart>().Text);
            StringAssert.Contains("\"path\":\"practice\"", LastPayload("Enacted").Replace(" ", ""));
        }

        [Test]
        public void ACompletionist_AndAPrincipledRefuser_BothEarnThePractice()
        {
            for (int i = 0; i < 3; i++) { Start("done" + i); StoryletPart.Current.CompleteQuest("done" + i); }
            for (int i = 0; i < 3; i++) { Start("no" + i); StoryletPart.Current.RefuseQuest("no" + i); }
            var r = StoryletPart.Current.ReadLedger(); Assert.IsTrue(r.Clean); Assert.AreEqual(3, r.Closed); Assert.AreEqual(3, r.Refused);
            Assert.IsTrue(Enact(EndingSpine.NameCommand, out _)); Assert.AreEqual(EndingSpine.PracticePath, EndingSpine.Enacted(player));
        }

        [Test]
        public void ASilentlyDroppedAct_BarsThePractice_ButNotTheStrike()
        {
            Start("dropped"); StoryletPart.Current.RemoveActiveQuest("dropped");
            Assert.IsFalse(Enact(EndingSpine.NameCommand, out _), "a ghost cannot");
            Assert.IsTrue(Enact(EndingSpine.StrikeCommand, out _), "the Strike asks nothing of the ledger");
        }

        [Test]
        public void ALostActRenounced_CleansTheLedgerForThePractice()
        {
            Start("lost"); var giver = factory.CreateEntity("Villager"); giver.ID = "test:giver"; var at = site.GetEntityPosition(seventh);
            Assert.IsTrue(site.AddEntity(giver, at.x + 2, at.y)); StoryletPart.Current.SetGiver("lost", giver, site);
            Assert.IsFalse(Enact(EndingSpine.NameCommand, out _));
            Assert.IsTrue(site.RemoveEntity(giver)); Assert.AreEqual(1, StoryletPart.Current.RenounceLost(player));
            Assert.IsTrue(Enact(EndingSpine.NameCommand, out _));
        }

        [Test]
        public void Guards_ThePlace_TheActor_AndTheCircle()
        {
            Diag.ResetAll();
            var at = site.GetEntityPosition(seventh); Assert.IsTrue(site.MoveEntity(player, at.x - 1, at.y));
            Assert.IsFalse(Enact(EndingSpine.StrikeCommand, out _)); StringAssert.Contains("not_in_the_position", LastPayload("Rejected")); StringAssert.Contains("Stand in the seventh position", string.Join("\n", MessageLog.GetRecent(3)));
            Assert.IsTrue(site.MoveEntity(player, at.x, at.y));
            var bare = site.GetAllEntities().First(e => e.BlueprintName == "FellingBarePosition");
            Assert.IsFalse(EndingSpine.TryWorldAction(bare, player, site, EndingSpine.StrikeCommand, out _)); StringAssert.Contains("not_the_circle", LastPayload("Rejected"));
            Assert.IsFalse(EndingSpine.TryWorldAction(seventh, player, site, "NotACommand", out _));
            var current = StoryletPart.Current; StoryletPart.Current = null;
            Assert.IsFalse(Enact(EndingSpine.NameCommand, out _)); StringAssert.Contains("no_ledger", LastPayload("Rejected")); StoryletPart.Current = current;
            player.SetStatValue("Hitpoints", 0);
            Assert.IsFalse(Enact(EndingSpine.StrikeCommand, out _)); StringAssert.Contains("no_actor", LastPayload("Rejected"));
            Assert.AreEqual(0, EndingSpine.Enacted(player)); Assert.AreEqual(0, Count("Enacted"));
        }

        [Test]
        public void TheEnactmentPersistsOnThePlayer()
        {
            Assert.IsTrue(Enact(EndingSpine.StrikeCommand, out _));
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(player);
            Assert.AreEqual(EndingSpine.VesselPath, EndingSpine.Enacted(loaded));
        }

        [Test]
        public void TheEpiloguesKeepCanon_StateTheirCosts_AndPreferNothing()
        {
            foreach (var text in new[] { EndingSpine.VesselEpilogue, EndingSpine.PracticeEpilogue })
            {
                Assert.IsFalse(text.Contains("Naro")); Assert.IsFalse(text.Contains("Urqu")); Assert.IsFalse(text.ToLowerInvariant().Contains("best")); Assert.IsFalse(text.ToLowerInvariant().Contains("true ending"));
            }
            StringAssert.Contains("crack", EndingSpine.VesselEpilogue); StringAssert.Contains("no one can choose it for you", EndingSpine.VesselEpilogue);
            StringAssert.Contains("begin to age", EndingSpine.PracticeEpilogue); StringAssert.Contains("never finishes", EndingSpine.PracticeEpilogue); StringAssert.Contains("wakes without fear", EndingSpine.PracticeEpilogue);
            var offered = Offered(seventh).Actions.Where(a => EndingSpine.IsWorldCommand(a.Command)).Select(a => a.Display).ToList();
            Assert.IsTrue(offered.Any(d => d.Contains("cracks")), "the strike's cost is stated on the menu"); Assert.IsTrue(offered.Any(d => d.Contains("clean ledger")), "the practice's gate is stated on the menu");
        }
    }
}
