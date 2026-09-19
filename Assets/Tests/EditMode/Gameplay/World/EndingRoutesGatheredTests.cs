using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The other advertised routes, ER.3 (Docs/ENDING-ROUTES.md): the Gathered
    /// ending. Any choir tendril, asked for a thread to carry down to the First
    /// Root, says the cost first and gives a cutting of the Choir; taking it takes
    /// on the act, any tendril releases the player from it aloud, and beside the
    /// Root's face the cutting is spent and the world is gathered — once and for
    /// all with the seal and the seventh position, the cost witnessed after.
    /// </summary>
    public sealed class EndingRoutesGatheredTests
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
            Diag.ResetAll(); MessageLog.Clear();
        }
        [TearDown] public void TearDown() { ConversationManager.EndConversation(); ConversationActions.Factory = previousConversationFactory; StoryletRegistry.Reset(); StoryletPart.Current = null; StoryletPart.LocalPlayer = null; SettlementRuntime.ActiveZone = null; NarrativeStatePart.Current = null; scope.Dispose(); }

        private static InventoryActionList Offered(Entity target) { var list = new InventoryActionList(); var ev = GameEvent.New("GetInventoryActions"); ev.SetParameter("Actions", (object)list); target.FireEventAndRelease(ev); return list; }
        private bool Gather(out int cost) => EndingRoutes.TryWorldAction(face, player, chamber, EndingRoutes.GatherCommand, out cost);
        private bool Seal(out int cost) => EndingRoutes.TryWorldAction(face, player, chamber, EndingRoutes.SealCommand, out cost);
        private Entity Give(string bp) { var e = factory.CreateEntity(bp); Assert.IsNotNull(e, bp); Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(e)); return e; }
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
        private Entity Tendril(int dx)
        {
            var t = factory.CreateEntity("ChoirTendril"); Assert.IsNotNull(t, "the Choir's shipped voice");
            var f = chamber.GetEntityPosition(face); Assert.IsTrue(chamber.AddEntity(t, f.x - 2 - dx, f.y), "seated in the hollow");
            Assert.IsFalse(FactionManager.IsHostile(t, player), "a tendril speaks to a walker at neutral standing");
            return t;
        }
        private static void Select(string target) { int i = Choice(target: target); Assert.GreaterOrEqual(i, 0, target); Assert.IsTrue(ConversationManager.SelectChoice(i)); }

        // ════════════════════════════════════════════════════════════
        //   The offer and its gates
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheFace_OffersBothRoutes_WithTheirCostsOnTheLabels()
        {
            var offered = Offered(face).Actions.Where(a => EndingRoutes.IsWorldCommand(a.Command)).ToList();
            Assert.AreEqual(2, offered.Count);
            var gather = offered.Single(a => a.Command == EndingRoutes.GatherCommand); var seal = offered.Single(a => a.Command == EndingRoutes.SealCommand);
            StringAssert.Contains("gathered", gather.Display); StringAssert.Contains("no one left", gather.Display); Assert.LessOrEqual(gather.Display.Length, 40);
            StringAssert.Contains("kept", seal.Display); StringAssert.Contains("nothing heals", seal.Display);
            foreach (var a in offered) StringAssert.DoesNotContain("best", a.Display);
        }

        [Test]
        public void WithoutTheCutting_GatheringIsRefused_AndSpendsNothing()
        {
            foreach (var s in EndingRoutes.Stones) Give(s);
            Assert.IsFalse(Gather(out int cost)); Assert.AreEqual(0, cost);
            StringAssert.Contains("no_cutting", LastPayload("ending", "Rejected"));
            StringAssert.Contains("no cutting", string.Join("\n", MessageLog.GetRecent(4)));
            foreach (var s in EndingRoutes.Stones) Assert.AreEqual(1, Carried(s), s);
            Assert.AreEqual(0, EndingSpine.Enacted(player)); Assert.AreEqual(0, Count("ending", "Enacted"));
        }

        // ════════════════════════════════════════════════════════════
        //   The enactment
        // ════════════════════════════════════════════════════════════

        [Test]
        public void WithTheCutting_TheChoirIsLetIn_OnceAndForAll_AndTheSealAndTheSeventhAreClosed()
        {
            Give(EndingRoutes.CuttingBlueprint); Give(EndingRoutes.CuttingBlueprint); foreach (var s in EndingRoutes.Stones) Give(s);
            Assert.IsTrue(Gather(out int cost)); Assert.AreEqual(EndingSpine.EnactCost, cost);
            Assert.AreEqual(EndingSpine.GatheredPath, EndingSpine.Enacted(player)); Assert.AreEqual(EndingSpine.GatheredPath, NarrativeStatePart.Current.GetFact(EndingSpine.EndingFact));
            Assert.AreEqual(1, Carried(EndingRoutes.CuttingBlueprint), "exactly one cutting is spent"); foreach (var s in EndingRoutes.Stones) Assert.AreEqual(1, Carried(s), s + " stays");
            Assert.IsTrue(MessageLog.HasPendingAnnouncement); string epilogue = MessageLog.ConsumeAnnouncement();
            StringAssert.Contains("no one remains", epilogue); StringAssert.Contains("nothing is ever lost", epilogue); StringAssert.DoesNotContain("best", epilogue); StringAssert.DoesNotContain("Naro", epilogue);
            StringAssert.Contains("sings", face.GetPart<ExaminablePart>().Text); StringAssert.DoesNotContain("dreams.", face.GetPart<ExaminablePart>().Text);
            Assert.IsFalse(Offered(face).Actions.Any(a => EndingRoutes.IsWorldCommand(a.Command)), "nothing more is offered");
            Assert.IsFalse(Gather(out cost)); Assert.AreEqual(0, cost); StringAssert.Contains("already_enacted", LastPayload("ending", "Rejected"));
            Assert.IsFalse(Seal(out _), "the seal is closed by the same state"); StringAssert.Contains("already_enacted", LastPayload("ending", "Rejected")); foreach (var s in EndingRoutes.Stones) Assert.AreEqual(1, Carried(s));
            Assert.AreEqual(1, Count("ending", "Enacted")); StringAssert.Contains("\"path\":\"gathered\"", LastPayload("ending", "Enacted")); StringAssert.Contains("\"consumed\":1", LastPayload("ending", "Enacted"), "one cutting, recorded");
            var site = manager.GetZone(FellingSiteBuilder.ZoneID); var seventh = site.GetAllEntities().Single(e => e.HasPart<SeventhPositionPart>());
            var list = new InventoryActionList(); EndingSpine.AddActions(list, player); Assert.IsEmpty(list.Actions, "the seventh offers nothing after a Root ending");
            Assert.IsFalse(EndingSpine.TryWorldAction(seventh, player, site, EndingSpine.NameCommand, out _)); StringAssert.Contains("already_enacted", LastPayload("ending", "Rejected"));
        }

        [Test]
        public void AfterKept_GatheringIsRefused_AndTheCuttingStays()
        {
            foreach (var s in EndingRoutes.Stones) Give(s); Give(EndingRoutes.CuttingBlueprint);
            Assert.IsTrue(Seal(out _)); MessageLog.ConsumeAnnouncement();
            Assert.IsFalse(Gather(out _)); StringAssert.Contains("already_enacted", LastPayload("ending", "Rejected"));
            Assert.AreEqual(1, Carried(EndingRoutes.CuttingBlueprint)); Assert.AreEqual(1, Count("ending", "Enacted"));
        }

        // ════════════════════════════════════════════════════════════
        //   The ledger
        // ════════════════════════════════════════════════════════════

        [Test]
        public void GatheringClosesTheGatherAct_LeavesTheKeepActOpenAndVisible_AndItCanStillBeReleased()
        {
            Start(EndingRoutes.GatherQuestId); Start(EndingRoutes.KeepQuestId); Give(EndingRoutes.CuttingBlueprint);
            Assert.IsTrue(Gather(out _));
            Assert.AreEqual(ClosureState.Closed, StoryletPart.Current.GetClosure(EndingRoutes.GatherQuestId).State); Assert.IsTrue(StoryletPart.Current.IsQuestCompleted(EndingRoutes.GatherQuestId));
            Assert.AreEqual(ClosureState.Open, StoryletPart.Current.GetClosure(EndingRoutes.KeepQuestId).State, "nothing is written off"); Assert.IsTrue(StoryletPart.Current.IsQuestActive(EndingRoutes.KeepQuestId));
            StringAssert.Contains("\"open\":1", LastPayload("ending", "Enacted"));
            Assert.IsTrue(StoryletPart.Current.RefuseQuest(EndingRoutes.KeepQuestId, player), "a spoken no still ends it after the world is gathered");
            Assert.AreEqual(ClosureState.Refused, StoryletPart.Current.GetClosure(EndingRoutes.KeepQuestId).State);
            Assert.AreEqual(0, StoryletPart.Current.ReadLedger().Open);
        }

        [TestCase(true)] [TestCase(false)]
        public void GatheringWithTheActRefusedOrNeverTakenOn_TakesItOnAtTheRoot_AndClosesIt(bool refusedFirst)
        {
            if (refusedFirst) { Start(EndingRoutes.GatherQuestId); Assert.IsTrue(StoryletPart.Current.RefuseQuest(EndingRoutes.GatherQuestId)); }
            Give(EndingRoutes.CuttingBlueprint); Assert.IsTrue(Gather(out _));
            var entry = StoryletPart.Current.GetClosure(EndingRoutes.GatherQuestId);
            Assert.AreEqual(ClosureState.Closed, entry.State); Assert.AreEqual(RootSiteBuilder.ChamberZoneID, entry.Where); Assert.AreEqual(face.GetDisplayName(), entry.GiverName);
            Assert.AreEqual(1, StoryletPart.Current.Ledger.Count); Assert.AreEqual(0, StoryletPart.Current.ReadLedger().Refused);
        }

        [Test]
        public void Persistence_TheGatheredEndingSurvivesARoundTrip()
        {
            Give(EndingRoutes.CuttingBlueprint); Assert.IsTrue(Gather(out _));
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(player);
            Assert.AreEqual(EndingSpine.GatheredPath, EndingSpine.Enacted(loaded)); Assert.AreEqual(EndingSpine.GatheredPath, NarrativeStatePart.Current.GetFact(EndingSpine.EndingFact));
        }

        // ════════════════════════════════════════════════════════════
        //   The cutting and its givers
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheCutting_IsARealCarryableItem_ThatSaysWhatItCosts()
        {
            var e = factory.CreateEntity(EndingRoutes.CuttingBlueprint); Assert.IsNotNull(e);
            Assert.IsTrue(e.GetPart<PhysicsPart>().Takeable); Assert.IsFalse(e.HasTag("Mineral"), "a living thread, not a stone");
            string text = e.GetPart<ExaminablePart>()?.Text ?? ""; StringAssert.Contains("no one left", text); StringAssert.Contains("nothing lost", text);
            Assert.NotNull(StoryletRegistry.FindQuest(EndingRoutes.GatherQuestId), "the journal entry exists"); Assert.AreEqual(EndingRoutes.GatherTitle, StoryletRegistry.FindQuest(EndingRoutes.GatherQuestId).Name);
            StringAssert.Contains("no one remains", StoryletRegistry.FindQuest(EndingRoutes.GatherQuestId).Stages[0].Objectives[0].Text, "the Field Note states the cost");
        }

        [Test]
        public void AnyTendril_GivesTheThread_AfterTheFirstRootIsAskedAbout_StatingTheCost_Once_AndAnyTendrilReleasesAloud()
        {
            var first = Tendril(0);
            Assert.IsTrue(ConversationManager.StartConversation(first, player));
            Assert.AreEqual(-1, Choice(target: "Thread"), "the thread is not on the doorstep; it follows asking after the First Root");
            Select("WhatAreYou"); Select("FirstRoot"); Select("FindFirstRoot");
            int ask = Choice(target: "Thread"); Assert.GreaterOrEqual(ask, 0); Assert.IsTrue(ConversationManager.SelectChoice(ask));
            string said = ConversationManager.CurrentNode.Text.ToLowerInvariant();
            StringAssert.Contains("no one remains", said, "the cost is said before the thread changes hands"); StringAssert.Contains("take no side", said, "the Choir sings all six accounts"); StringAssert.DoesNotContain("dead", said); StringAssert.DoesNotContain("best", said);
            Assert.GreaterOrEqual(Choice(target: "Start"), 0, "'not from me' is a real choice");
            int carry = Choice(actionKey: "GiveItem", actionValue: EndingRoutes.CuttingBlueprint); Assert.GreaterOrEqual(carry, 0); ConversationManager.SelectChoice(carry); Assert.IsFalse(ConversationManager.IsActive, "the handover ends the talk");
            Assert.AreEqual(1, Carried(EndingRoutes.CuttingBlueprint)); Assert.IsTrue(player.Properties.ContainsKey("GatherCuttingGiven"));
            Assert.IsTrue(StoryletPart.Current.IsQuestActive(EndingRoutes.GatherQuestId), "taking the thread takes on the act");
            var entry = StoryletPart.Current.GetClosure(EndingRoutes.GatherQuestId); Assert.AreEqual(first.GetDisplayName(), entry.GiverName); Assert.AreEqual(chamber.ZoneID, entry.Where);
            ConversationManager.EndConversation();
            // A second tendril, another instance of the same voice: no second thread, but the release.
            var second = Tendril(1);
            Assert.IsTrue(ConversationManager.StartConversation(second, player));
            Select("WhatAreYou"); Select("FirstRoot"); Select("FindFirstRoot"); Assert.AreEqual(-1, Choice(target: "Thread"), "a second thread is not offered"); Select("Start");
            int release = Choice(actionKey: "RefuseQuest", actionValue: EndingRoutes.GatherQuestId); Assert.GreaterOrEqual(release, 0, "any tendril takes the no");
            Assert.IsTrue(ConversationManager.SelectChoice(release)); Assert.AreEqual("GatherReleased", ConversationManager.CurrentNode.ID);
            StringAssert.DoesNotContain("dead", ConversationManager.CurrentNode.Text.ToLowerInvariant());
            Assert.IsFalse(StoryletPart.Current.IsQuestActive(EndingRoutes.GatherQuestId)); Assert.AreEqual(ClosureState.Refused, StoryletPart.Current.GetClosure(EndingRoutes.GatherQuestId).State);
            Assert.AreEqual(1, Carried(EndingRoutes.CuttingBlueprint), "the cutting stays; the act is what ended");
            ConversationManager.EndConversation();
            Assert.IsTrue(ConversationManager.StartConversation(first, player)); Assert.AreEqual(-1, Choice(actionKey: "RefuseQuest", actionValue: EndingRoutes.GatherQuestId), "nothing left to release");
            ConversationManager.EndConversation();
            // Carrying on regardless: the enactment closes the act, taken on again at the Root.
            Assert.IsTrue(Gather(out _)); Assert.AreEqual(ClosureState.Closed, StoryletPart.Current.GetClosure(EndingRoutes.GatherQuestId).State);
        }
    }
}
