using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The other advertised routes, ER.2 (Docs/ENDING-ROUTES.md): the Kept ending.
    /// Beside the Root's face, with tepuibone, memory-marble and mute-stone carried,
    /// the player seals the Root: one of each stone is spent, the world is kept,
    /// the cost is stated before the choice and witnessed after, and it is once and
    /// for all with the seventh position. The stones come from the two preservers
    /// through their real conversations; taking one takes on the act, the seal
    /// closes it, and either giver releases the player from it aloud.
    /// </summary>
    public sealed class EndingRoutesKeptTests
    {
        private HotbarSaveFixture scope; private EntityFactory factory; private OverworldZoneManager manager; private Zone chamber; private Entity player, face;
        private EntityFactory previousConversationFactory;

        [SetUp] public void SetUp()
        {
            scope = new HotbarSaveFixture(false, false); factory = GrovelandsCompositionTests.Factory(); manager = OverworldZoneManager.CreateDetached(factory, 64);
            chamber = manager.GetZone(RootSiteBuilder.ChamberZoneID); face = chamber.GetAllEntities().Single(e => e.HasPart<RootFacePart>());
            player = factory.CreateEntity("Player"); var f = chamber.GetEntityPosition(face);
            Assert.IsTrue(chamber.AddEntity(player, f.x - 1, f.y), "standing beside the face, in the hollow");
            StoryletPart.Current = new StoryletPart(); StoryletPart.LocalPlayer = player; SettlementRuntime.ActiveZone = chamber; NarrativeStatePart.Current = new NarrativeStatePart();
            // The conversation action layer creates given items through this factory (GameBootstrap sets it at runtime).
            previousConversationFactory = ConversationActions.Factory; ConversationActions.Factory = factory;
            // The registry is static and another class in the same run may have loaded a test-only file into it; start from the shipped resources.
            StoryletRegistry.Reset(); StoryletRegistry.LoadAll();
            StillleafArchiveContent.EnsureRegistered();
            Diag.ResetAll(); MessageLog.Clear();
        }
        [TearDown] public void TearDown() { ConversationManager.EndConversation(); ConversationActions.Factory = previousConversationFactory; StoryletRegistry.Reset(); StoryletPart.Current = null; StoryletPart.LocalPlayer = null; SettlementRuntime.ActiveZone = null; NarrativeStatePart.Current = null; scope.Dispose(); }

        private static InventoryActionList Offered(Entity target) { var list = new InventoryActionList(); var ev = GameEvent.New("GetInventoryActions"); ev.SetParameter("Actions", (object)list); target.FireEventAndRelease(ev); return list; }
        private bool Seal(out int cost) => EndingRoutes.TryWorldAction(face, player, chamber, EndingRoutes.SealCommand, out cost);
        private Entity Give(string bp) { var e = factory.CreateEntity(bp); Assert.IsNotNull(e, bp); Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(e)); return e; }
        private void GiveAll() { foreach (var s in EndingRoutes.Stones) Give(s); }
        private int Carried(string bp) => player.GetPart<InventoryPart>().Objects.Count(e => e.BlueprintName == bp);
        private static int Count(string category, string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = category, Kind = kind, Limit = 40 }).Records.Count;
        private static string LastPayload(string category, string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = category, Kind = kind, Limit = 40 }).Records.Last().PayloadJson.Replace(" ", "");
        private static void Start(string id) => StoryletPart.Current.StartQuest(new QuestState { QuestId = id, CurrentStageIndex = 0 });
        private static int Choice(string target = null, string actionKey = null, string actionValue = null)
        {
            var choices = ConversationManager.VisibleChoices;
            for (int i = 0; i < choices.Count; i++)
            {
                var c = choices[i];
                if (target != null && c.Target != target) continue;
                if (actionKey != null && !c.Actions.Any(a => a.Key == actionKey && (actionValue == null || a.Value == actionValue))) continue;
                return i;
            }
            return -1;
        }

        // ════════════════════════════════════════════════════════════
        //   The offer and its gates
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheFace_OffersTheSeal_WithItsCostOnTheLabel_AndNothingOnceAnyEndingIsEnacted()
        {
            var seal = Offered(face).Actions.Single(a => a.Command == EndingRoutes.SealCommand);
            StringAssert.Contains("kept", seal.Display); StringAssert.Contains("nothing heals", seal.Display);
            Assert.LessOrEqual(seal.Display.Length, 40, "fits the native picker's width");
            player.SetIntProperty(EndingSpine.EndingProperty, EndingSpine.PracticePath);
            Assert.IsFalse(Offered(face).Actions.Any(a => EndingRoutes.IsWorldCommand(a.Command)), "after any ending, nothing");
        }

        [Test]
        public void WithoutTheStones_TheSealIsRefused_NamingWhatIsMissing_AndConsumesNothing()
        {
            Assert.IsFalse(Seal(out int cost)); Assert.AreEqual(0, cost);
            string payload = LastPayload("ending", "Rejected");
            StringAssert.Contains("missing_stones", payload); StringAssert.Contains("tepuibone,memory-marble,mute-stone", payload);
            StringAssert.Contains("you lack", string.Join("\n", MessageLog.GetRecent(4)));
            Assert.AreEqual(0, EndingSpine.Enacted(player)); Assert.IsFalse(MessageLog.HasPendingAnnouncement); Assert.AreEqual(0, Count("ending", "Enacted"));
            // Two of three: only the third is named, and nothing carried is spent.
            Give("Tepuibone"); Give("MemoryMarble");
            Assert.IsFalse(Seal(out _)); payload = LastPayload("ending", "Rejected");
            StringAssert.Contains("\"missing\":\"mute-stone\"", payload);
            Assert.AreEqual(1, Carried("Tepuibone")); Assert.AreEqual(1, Carried("MemoryMarble"));
        }

        // ════════════════════════════════════════════════════════════
        //   The enactment
        // ════════════════════════════════════════════════════════════

        [Test]
        public void WithAllThree_SealingKeepsTheWorld_OnceAndForAll_AndTheSeventhOffersNothing()
        {
            GiveAll(); Give("Tepuibone");
            Assert.IsTrue(Seal(out int cost)); Assert.AreEqual(EndingSpine.EnactCost, cost);
            Assert.AreEqual(EndingSpine.KeptPath, EndingSpine.Enacted(player)); Assert.AreEqual(EndingSpine.KeptPath, NarrativeStatePart.Current.GetFact(EndingSpine.EndingFact));
            Assert.AreEqual(1, Carried("Tepuibone"), "exactly one of each is spent"); Assert.AreEqual(0, Carried("MemoryMarble")); Assert.AreEqual(0, Carried("MuteStone"));
            Assert.IsTrue(MessageLog.HasPendingAnnouncement); string epilogue = MessageLog.ConsumeAnnouncement();
            StringAssert.Contains("nothing heals", epilogue); StringAssert.Contains("the Six survive as themselves", epilogue); StringAssert.Contains("kept", epilogue);
            StringAssert.DoesNotContain("best", epilogue);
            StringAssert.Contains("kept", face.GetPart<ExaminablePart>().Text); StringAssert.DoesNotContain("dreams", face.GetPart<ExaminablePart>().Text);
            Assert.IsFalse(Offered(face).Actions.Any(a => EndingRoutes.IsWorldCommand(a.Command)), "nothing more is offered");
            Assert.IsFalse(Seal(out cost)); Assert.AreEqual(0, cost); StringAssert.Contains("already_enacted", LastPayload("ending", "Rejected"));
            Assert.AreEqual(1, Count("ending", "Enacted")); StringAssert.Contains("\"path\":\"kept\"", LastPayload("ending", "Enacted"));
            // The seventh position is closed by the same state.
            var site = manager.GetZone(FellingSiteBuilder.ZoneID); var seventh = site.GetAllEntities().Single(e => e.HasPart<SeventhPositionPart>());
            var list = new InventoryActionList(); EndingSpine.AddActions(list, player); Assert.IsEmpty(list.Actions, "the seventh offers nothing after a Root ending");
            Assert.IsFalse(EndingSpine.TryWorldAction(seventh, player, site, EndingSpine.StrikeCommand, out _)); StringAssert.Contains("already_enacted", LastPayload("ending", "Rejected"));
        }

        [Test]
        public void AfterARenewal_TheSealIsRefused_AndTheStonesStay()
        {
            GiveAll(); player.SetIntProperty(EndingSpine.EndingProperty, EndingSpine.VesselPath);
            Assert.IsFalse(Seal(out _)); StringAssert.Contains("already_enacted", LastPayload("ending", "Rejected"));
            foreach (var s in EndingRoutes.Stones) Assert.AreEqual(1, Carried(s), s);
            Assert.AreEqual(0, Count("ending", "Enacted"));
        }

        [Test]
        public void NotBesideTheFace_NotTheRoot_NoLedger_OrDead_IsRefused_AndSpendsNothing()
        {
            GiveAll(); var f = chamber.GetEntityPosition(face);
            Assert.IsTrue(chamber.RemoveEntity(player)); Assert.IsTrue(chamber.AddEntity(player, f.x - 3, f.y), "three cells west, still in the hollow");
            Assert.IsFalse(Seal(out _)); StringAssert.Contains("not_adjacent", LastPayload("ending", "Rejected"));
            Assert.IsTrue(chamber.RemoveEntity(player)); Assert.IsTrue(chamber.AddEntity(player, f.x - 1, f.y));
            Assert.IsFalse(EndingRoutes.TryWorldAction(face, player, manager.GetZone(RootSiteBuilder.MouthZoneID), EndingRoutes.SealCommand, out _)); StringAssert.Contains("not_the_root", LastPayload("ending", "Rejected"));
            Assert.IsFalse(EndingRoutes.TryWorldAction(face, player, chamber, "NotACommand", out _));
            var current = StoryletPart.Current; StoryletPart.Current = null;
            Assert.IsFalse(Seal(out _)); StringAssert.Contains("no_ledger", LastPayload("ending", "Rejected")); StoryletPart.Current = current;
            player.SetStatValue("Hitpoints", 0);
            Assert.IsFalse(Seal(out _)); StringAssert.Contains("no_actor", LastPayload("ending", "Rejected"));
            foreach (var s in EndingRoutes.Stones) Assert.AreEqual(1, Carried(s), s);
            Assert.AreEqual(0, Count("ending", "Enacted"));
        }

        // ════════════════════════════════════════════════════════════
        //   The ledger
        // ════════════════════════════════════════════════════════════

        [Test]
        public void SealingClosesTheKeepAct_AndTheJournalShowsItCompleted()
        {
            Start(EndingRoutes.KeepQuestId); GiveAll();
            Assert.IsTrue(Seal(out _));
            Assert.IsTrue(StoryletPart.Current.IsQuestCompleted(EndingRoutes.KeepQuestId)); Assert.IsFalse(StoryletPart.Current.IsQuestActive(EndingRoutes.KeepQuestId));
            Assert.AreEqual(ClosureState.Closed, StoryletPart.Current.GetClosure(EndingRoutes.KeepQuestId).State);
            var reading = StoryletPart.Current.ReadLedger(); Assert.AreEqual(1, reading.Closed); Assert.AreEqual(0, reading.Open);
            Assert.AreEqual(1, Count("closure", "Closed"));
        }

        [TestCase(true)] [TestCase(false)]
        public void SealingWithTheActRefusedOrNeverTakenOn_TakesItOnAtTheRoot_AndClosesIt(bool refusedFirst)
        {
            if (refusedFirst) { Start(EndingRoutes.KeepQuestId); Assert.IsTrue(StoryletPart.Current.RefuseQuest(EndingRoutes.KeepQuestId)); Assert.AreEqual(ClosureState.Refused, StoryletPart.Current.GetClosure(EndingRoutes.KeepQuestId).State); }
            GiveAll(); Assert.IsTrue(Seal(out _));
            var entry = StoryletPart.Current.GetClosure(EndingRoutes.KeepQuestId);
            Assert.AreEqual(ClosureState.Closed, entry.State, "a carried-through act is closed, whatever was said before");
            Assert.AreEqual(RootSiteBuilder.ChamberZoneID, entry.Where); Assert.AreEqual(face.GetDisplayName(), entry.GiverName, "taken on at the Root, from the face");
            Assert.AreEqual(1, StoryletPart.Current.Ledger.Count); Assert.IsTrue(StoryletPart.Current.IsQuestCompleted(EndingRoutes.KeepQuestId));
            Assert.AreEqual(0, StoryletPart.Current.ReadLedger().Refused, "the earlier no is superseded, not laundered into a second entry");
        }

        [Test]
        public void TheLedgerIsNotAGate_OtherOpenActsStayOpen_AndVisible()
        {
            Start("errand"); GiveAll();
            Assert.IsTrue(Seal(out _));
            var reading = StoryletPart.Current.ReadLedger(); Assert.AreEqual(1, reading.Open); Assert.AreEqual(1, reading.Closed);
            Assert.IsTrue(StoryletPart.Current.IsQuestActive("errand"), "nothing is written off");
            StringAssert.Contains("\"open\":1", LastPayload("ending", "Enacted"));
        }

        [Test]
        public void Persistence_TheKeptEndingSurvivesARoundTrip()
        {
            GiveAll(); Assert.IsTrue(Seal(out _));
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(player);
            Assert.AreEqual(EndingSpine.KeptPath, EndingSpine.Enacted(loaded)); Assert.AreEqual(EndingSpine.KeptPath, NarrativeStatePart.Current.GetFact(EndingSpine.EndingFact));
        }

        [Test]
        public void Diag_EveryGateReadingIsRecorded_AndRejectionsNeverClaimEnactment()
        {
            Assert.IsFalse(Seal(out _)); Assert.AreEqual(1, Count("ending", "Rejected")); Assert.AreEqual(0, Count("ending", "Enacted"));
            GiveAll(); Assert.IsTrue(Seal(out _)); Assert.AreEqual(1, Count("ending", "Enacted"));
            Assert.IsFalse(Seal(out _)); Assert.AreEqual(2, Count("ending", "Rejected")); Assert.AreEqual(1, Count("ending", "Enacted"));
        }

        // ════════════════════════════════════════════════════════════
        //   The stones and their givers
        // ════════════════════════════════════════════════════════════

        [TestCase("MemoryMarble")] [TestCase("MuteStone")] [TestCase("Tepuibone")]
        public void TheStones_AreRealCarryableMinerals_ThatSayWhatTheyHold(string bp)
        {
            var e = factory.CreateEntity(bp); Assert.IsNotNull(e);
            Assert.IsTrue(e.GetPart<PhysicsPart>().Takeable); Assert.IsTrue(e.HasTag("Mineral")); Assert.AreEqual("3", e.GetTag("Tier"));
            StringAssert.Contains("name", (e.GetPart<ExaminablePart>()?.Text ?? "").ToLowerInvariant(), "each stone says it holds its name (tepuibone: 'Name-holders work it')");
        }

        [Test]
        public void IfNotHaveProperty_IsTheMirrorOfIfHaveProperty()
        {
            Assert.IsTrue(ConversationPredicates.Evaluate("IfNotHaveProperty", null, player, "KeepMemoryMarbleGiven"));
            Assert.IsFalse(ConversationPredicates.Evaluate("IfHaveProperty", null, player, "KeepMemoryMarbleGiven"));
            player.Properties["KeepMemoryMarbleGiven"] = "true";
            Assert.IsFalse(ConversationPredicates.Evaluate("IfNotHaveProperty", null, player, "KeepMemoryMarbleGiven"));
            Assert.IsTrue(ConversationPredicates.Evaluate("IfHaveProperty", null, player, "KeepMemoryMarbleGiven"));
            Assert.IsFalse(ConversationPredicates.Evaluate("IfNotHaveProperty", null, null, "KeepMemoryMarbleGiven"), "no listener, no offer");
        }

        private Entity MoveTo(Zone zone, Entity beside)
        {
            Assert.IsTrue(chamber.RemoveEntity(player));
            var p = zone.GetEntityPosition(beside);
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                var c = zone.GetCell(p.x + dx, p.y + dy);
                if (c != null && !c.BlocksMovement()) { Assert.IsTrue(zone.AddEntity(player, c.X, c.Y)); SettlementRuntime.ActiveZone = zone; return beside; }
            }
            Assert.Fail("no free cell beside the giver"); return null;
        }

        [TestCase("Searcher")] [TestCase("Indexer")]
        public void TheGiver_HandsOverTheirStone_ForTheSealing_StatingTheCost_Once_AndReleasesAloud(string who)
        {
            Zone zone; Entity giver; string stone, node, flag;
            if (who == "Searcher") { zone = manager.GetZone(StillleafArchiveContent.QuillholdZoneId); giver = zone.GetAllEntities().Single(e => e.ID == StillleafArchiveContent.SearcherId); stone = "MemoryMarble"; node = "Stone"; flag = "KeepMemoryMarbleGiven"; }
            else { zone = manager.GetZone(StillleafSaltVault.ZoneId); giver = zone.GetAllEntities().Single(e => e.ID == StillleafSaltVault.IndexerId); stone = "MuteStone"; node = "MuteStone"; flag = "KeepMuteStoneGiven"; }
            MoveTo(zone, giver);
            Assert.IsTrue(ConversationManager.StartConversation(giver, player));
            int ask = Choice(target: node); Assert.GreaterOrEqual(ask, 0, "the stone is asked for, once"); Assert.IsTrue(ConversationManager.SelectChoice(ask));
            string said = ConversationManager.CurrentNode.Text; StringAssert.Contains("heal", said, "the cost is said before the stone changes hands"); StringAssert.DoesNotContain("best", said);
            int decline = Choice(target: "Start"); Assert.GreaterOrEqual(decline, 0, "'not from me' is a real choice");
            int carry = Choice(actionKey: "GiveItem", actionValue: stone); Assert.GreaterOrEqual(carry, 0);
            ConversationManager.SelectChoice(carry); Assert.IsFalse(ConversationManager.IsActive, "the handover ends the talk (a choice to End returns false by design)");
            Assert.AreEqual(1, Carried(stone)); Assert.IsTrue(player.Properties.ContainsKey(flag));
            Assert.IsTrue(StoryletPart.Current.IsQuestActive(EndingRoutes.KeepQuestId), "taking the stone takes on the act");
            var entry = StoryletPart.Current.GetClosure(EndingRoutes.KeepQuestId); Assert.AreEqual(giver.GetDisplayName(), entry.GiverName); Assert.AreEqual(zone.ZoneID, entry.Where);
            Assert.NotNull(StoryletRegistry.FindQuest(EndingRoutes.KeepQuestId), "the journal entry exists"); Assert.AreEqual(EndingRoutes.KeepTitle, StoryletRegistry.FindQuest(EndingRoutes.KeepQuestId).Name);
            ConversationManager.EndConversation();
            Assert.IsTrue(ConversationManager.StartConversation(giver, player));
            Assert.AreEqual(-1, Choice(target: node), "a second stone is not offered");
            int release = Choice(actionKey: "RefuseQuest", actionValue: EndingRoutes.KeepQuestId); Assert.GreaterOrEqual(release, 0, "the act can be ended aloud here");
            Assert.IsTrue(ConversationManager.SelectChoice(release)); Assert.AreEqual("KeepReleased", ConversationManager.CurrentNode.ID);
            Assert.IsFalse(StoryletPart.Current.IsQuestActive(EndingRoutes.KeepQuestId)); Assert.AreEqual(ClosureState.Refused, StoryletPart.Current.GetClosure(EndingRoutes.KeepQuestId).State);
            Assert.AreEqual(1, Carried(stone), "the stone stays; the act is what ended");
            ConversationManager.EndConversation();
            Assert.IsTrue(ConversationManager.StartConversation(giver, player));
            Assert.AreEqual(-1, Choice(actionKey: "RefuseQuest", actionValue: EndingRoutes.KeepQuestId), "nothing left to release");
        }

        [Test]
        public void TheSecondGiver_DoesNotRestartAnActiveAct_ButTakesItOnAgainAfterARefusal()
        {
            var quillhold = manager.GetZone(StillleafArchiveContent.QuillholdZoneId); var searcher = quillhold.GetAllEntities().Single(e => e.ID == StillleafArchiveContent.SearcherId);
            MoveTo(quillhold, searcher);
            Assert.IsTrue(ConversationManager.StartConversation(searcher, player)); Assert.IsTrue(ConversationManager.SelectChoice(Choice(target: "Stone"))); ConversationManager.SelectChoice(Choice(actionKey: "GiveItem", actionValue: "MemoryMarble")); Assert.IsFalse(ConversationManager.IsActive);
            ConversationManager.EndConversation();
            var salt = manager.GetZone(StillleafSaltVault.ZoneId); var indexer = salt.GetAllEntities().Single(e => e.ID == StillleafSaltVault.IndexerId);
            Assert.IsTrue(quillhold.RemoveEntity(player)); chamber.AddEntity(player, chamber.GetEntityPosition(face).x - 1, chamber.GetEntityPosition(face).y); MoveTo(salt, indexer);
            Assert.IsTrue(ConversationManager.StartConversation(indexer, player)); Assert.IsTrue(ConversationManager.SelectChoice(Choice(target: "MuteStone"))); ConversationManager.SelectChoice(Choice(actionKey: "GiveItem", actionValue: "MuteStone")); Assert.IsFalse(ConversationManager.IsActive);
            var entry = StoryletPart.Current.GetClosure(EndingRoutes.KeepQuestId);
            Assert.AreEqual(searcher.GetDisplayName(), entry.GiverName, "the act stays the first giver's; the second stone does not restart it");
            Assert.AreEqual(1, StoryletPart.Current.Ledger.Count);
            ConversationManager.EndConversation();
            Assert.IsTrue(ConversationManager.StartConversation(indexer, player)); Assert.IsTrue(ConversationManager.SelectChoice(Choice(actionKey: "RefuseQuest", actionValue: EndingRoutes.KeepQuestId))); ConversationManager.EndConversation();
            Assert.AreEqual(ClosureState.Refused, StoryletPart.Current.GetClosure(EndingRoutes.KeepQuestId).State);
            // Carrying both stones on: the seal still closes the act, taken on again at the Root.
            Assert.IsTrue(salt.RemoveEntity(player)); Assert.IsTrue(chamber.AddEntity(player, chamber.GetEntityPosition(face).x - 1, chamber.GetEntityPosition(face).y)); SettlementRuntime.ActiveZone = chamber;
            Give("Tepuibone"); Assert.IsTrue(Seal(out _)); Assert.AreEqual(ClosureState.Closed, StoryletPart.Current.GetClosure(EndingRoutes.KeepQuestId).State);
        }
    }
}
