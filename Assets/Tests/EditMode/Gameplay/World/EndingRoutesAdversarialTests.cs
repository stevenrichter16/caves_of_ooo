using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The other advertised routes, ER.5 (Docs/ENDING-ROUTES.md): the dedicated
    /// adversarial sweep over the Root routes (ADVERSARIAL_TESTING.md taxonomy) plus
    /// hypothesis-driven probes, written before re-reading the code and run first
    /// against the shipped ER.1–ER.4 so each fails or passes on its own assertion.
    /// Surfaces: boundary inputs, cross-actor, requirement reach (carried vs on the
    /// floor; stacks), impostor targets, adjacency geometry, state-side exclusivity
    /// with the seventh, save/load of the face and of the ledger, diag contracts.
    /// </summary>
    public sealed class EndingRoutesAdversarialTests
    {
        private HotbarSaveFixture scope; private EntityFactory factory; private OverworldZoneManager manager; private Zone chamber; private Entity player, face;
        private EntityFactory previousConversationFactory;

        [SetUp] public void SetUp()
        {
            scope = new HotbarSaveFixture(false, false); factory = GrovelandsCompositionTests.Factory(); manager = OverworldZoneManager.CreateDetached(factory, 64);
            chamber = manager.GetZone(RootSiteBuilder.ChamberZoneID); face = chamber.GetAllEntities().Single(e => e.HasPart<RootFacePart>());
            player = factory.CreateEntity("Player"); var f = chamber.GetEntityPosition(face); Assert.IsTrue(chamber.AddEntity(player, f.x - 1, f.y));
            StoryletPart.Current = new StoryletPart(); StoryletPart.LocalPlayer = player; SettlementRuntime.ActiveZone = chamber; NarrativeStatePart.Current = new NarrativeStatePart();
            previousConversationFactory = ConversationActions.Factory; ConversationActions.Factory = factory; StoryletRegistry.Reset(); StoryletRegistry.LoadAll();
            Diag.ResetAll(); MessageLog.Clear();
        }
        [TearDown] public void TearDown() { ConversationManager.EndConversation(); ConversationActions.Factory = previousConversationFactory; StoryletRegistry.Reset(); StoryletPart.Current = null; StoryletPart.LocalPlayer = null; SettlementRuntime.ActiveZone = null; NarrativeStatePart.Current = null; scope.Dispose(); }

        private bool Seal() => EndingRoutes.TryWorldAction(face, player, chamber, EndingRoutes.SealCommand, out _);
        private bool Gather() => EndingRoutes.TryWorldAction(face, player, chamber, EndingRoutes.GatherCommand, out _);
        private Entity Give(string bp, int count = 1) { var e = factory.CreateEntity(bp); Assert.IsNotNull(e, bp); if (count > 1) e.GetPart<StackerPart>().StackCount = count; Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(e)); return e; }
        private void GiveStones() { foreach (var s in EndingRoutes.Stones) Give(s); }
        private int Carried(string bp) => player.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == bp).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
        private static int Count(string category, string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = category, Kind = kind, Limit = 60 }).Records.Count;
        private static string LastPayload(string category, string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = category, Kind = kind, Limit = 60 }).Records.Last().PayloadJson.Replace(" ", "");
        private StoryletPart RoundTrip(StoryletPart part) { using var stream = new MemoryStream(); part.Save(new SaveWriter(stream)); stream.Position = 0; var loaded = new StoryletPart(); loaded.Load(new SaveReader(stream, factory)); return loaded; }
        private static InventoryActionList Offered(Entity target) { var list = new InventoryActionList(); var ev = GameEvent.New("GetInventoryActions"); ev.SetParameter("Actions", (object)list); target.FireEventAndRelease(ev); return list; }

        // ════════════════ boundary inputs ════════════════

        [Test]
        public void Adversarial_NullsAndNonsense_NeverThrow_NeverAct()
        {
            GiveStones();
            Assert.IsFalse(EndingRoutes.TryWorldAction(null, player, chamber, EndingRoutes.SealCommand, out int c1)); Assert.AreEqual(0, c1);
            Assert.IsFalse(EndingRoutes.TryWorldAction(face, null, chamber, EndingRoutes.SealCommand, out _));
            Assert.IsFalse(EndingRoutes.TryWorldAction(face, player, null, EndingRoutes.SealCommand, out _));
            Assert.IsFalse(EndingRoutes.TryWorldAction(face, player, chamber, null, out _)); Assert.IsFalse(EndingRoutes.TryWorldAction(face, player, chamber, "", out _));
            Assert.IsFalse(EndingRoutes.IsWorldCommand(null)); Assert.DoesNotThrow(() => EndingRoutes.AddActions(null, player));
            Assert.AreEqual(0, EndingSpine.Enacted(player)); Assert.AreEqual(0, Count("ending", "Enacted")); foreach (var s in EndingRoutes.Stones) Assert.AreEqual(1, Carried(s));
        }

        // ════════════════ cross-actor ════════════════

        [Test]
        public void Adversarial_AnotherCreatureBesideTheFace_CannotEnact()
        {
            var villager = factory.CreateEntity("Villager"); var f = chamber.GetEntityPosition(face); Assert.IsTrue(chamber.AddEntity(villager, f.x - 1, f.y - 1));
            foreach (var s in EndingRoutes.Stones) { var e = factory.CreateEntity(s); Assert.IsTrue(villager.GetPart<InventoryPart>().AddObject(e)); }
            Assert.IsFalse(EndingRoutes.TryWorldAction(face, villager, chamber, EndingRoutes.SealCommand, out _)); StringAssert.Contains("no_actor", LastPayload("ending", "Rejected"));
            Assert.AreEqual(0, villager.GetIntProperty(EndingSpine.EndingProperty)); Assert.AreEqual(0, Count("ending", "Enacted"));
        }

        // ════════════════ requirement reach ════════════════

        [Test]
        public void Adversarial_StonesOnTheFloorBesideThePlayer_DoNotCount()
        {
            var p = chamber.GetEntityPosition(player);
            foreach (var s in EndingRoutes.Stones) { var e = factory.CreateEntity(s); Assert.IsTrue(chamber.AddEntity(e, p.x, p.y), "lying underfoot"); }
            Assert.IsFalse(Seal()); StringAssert.Contains("missing_stones", LastPayload("ending", "Rejected"));
            Assert.AreEqual(3, chamber.GetCell(p.x, p.y).Objects.Count(o => EndingRoutes.Stones.Contains(o.BlueprintName)), "nothing on the floor is taken");
        }

        [Test]
        public void Adversarial_AStackOfStones_LosesExactlyOneUnit()
        {
            Give("Tepuibone", 3); Give("MemoryMarble", 2); Give("MuteStone");
            Assert.IsTrue(Seal()); MessageLog.ConsumeAnnouncement();
            Assert.AreEqual(2, Carried("Tepuibone")); Assert.AreEqual(1, Carried("MemoryMarble")); Assert.AreEqual(0, Carried("MuteStone"));
        }

        [Test]
        public void Adversarial_TwoCuttings_LoseExactlyOne_AndTheStonesAreUntouched()
        {
            Give(EndingRoutes.CuttingBlueprint, 2); GiveStones();
            Assert.IsTrue(Gather()); MessageLog.ConsumeAnnouncement();
            Assert.AreEqual(1, Carried(EndingRoutes.CuttingBlueprint)); foreach (var s in EndingRoutes.Stones) Assert.AreEqual(1, Carried(s));
        }

        // ════════════════ impostor targets and geometry ════════════════

        /// <summary>Hypothesis: a second face standing in the chamber is not the Root; only the authored face (its fixed id) enacts.</summary>
        [Test]
        public void Hypothesis_ASecondFaceInTheChamber_IsNotTheRoot()
        {
            var impostor = factory.CreateEntity(RootSiteBuilder.FaceBlueprint); var p = chamber.GetEntityPosition(player);
            var seat = chamber.GetCell(p.x - 1, p.y); Assert.IsTrue(seat != null && !seat.BlocksMovement()); Assert.IsTrue(chamber.AddEntity(impostor, p.x - 1, p.y));
            GiveStones();
            Assert.IsFalse(EndingRoutes.TryWorldAction(impostor, player, chamber, EndingRoutes.SealCommand, out _)); StringAssert.Contains("not_the_root", LastPayload("ending", "Rejected"));
            foreach (var s in EndingRoutes.Stones) Assert.AreEqual(1, Carried(s)); Assert.AreEqual(0, EndingSpine.Enacted(player));
        }

        [Test]
        public void Adversarial_DiagonalAdjacency_Counts_TwoCellsDoesNot()
        {
            var f = chamber.GetEntityPosition(face); GiveStones();
            Assert.IsTrue(chamber.RemoveEntity(player)); Assert.IsTrue(chamber.AddEntity(player, f.x - 1, f.y - 1), "diagonal");
            Assert.IsTrue(Seal(), "a diagonal neighbour is beside the face");
        }

        // ════════════════ state-side exclusivity with the seventh ════════════════

        [TestCase(EndingSpine.GatheredPath)] [TestCase(EndingSpine.KeptPath)]
        public void Adversarial_ARootEndingOnThePlayer_ClosesTheSeventhsExposureAndOffers(int path)
        {
            player.SetIntProperty(EndingSpine.EndingProperty, path);
            var site = manager.GetZone(FellingSiteBuilder.ZoneID); var seventh = site.GetAllEntities().Single(e => e.HasPart<SeventhPositionPart>());
            Assert.IsTrue(chamber.RemoveEntity(player)); var at = site.GetEntityPosition(seventh); Assert.IsTrue(site.AddEntity(player, at.x, at.y)); SettlementRuntime.ActiveZone = site;
            Assert.IsFalse(seventh.GetPart<SeventhPositionPart>().TryAffectStandingPlayer(player, site), "the exposure has ended");
            var list = new InventoryActionList(); EndingSpine.AddActions(list, player); Assert.IsEmpty(list.Actions);
            Assert.IsFalse(EndingSpine.TryWorldAction(seventh, player, site, EndingSpine.StrikeCommand, out _)); StringAssert.Contains("already_enacted", LastPayload("ending", "Rejected"));
        }

        // ════════════════ save/load ════════════════

        /// <summary>Hypothesis: the face's changed examine text after an ending survives a round trip of the face itself.</summary>
        [Test]
        public void Hypothesis_TheFacesExamineText_SurvivesARoundTrip_AfterAnEnding()
        {
            GiveStones(); Assert.IsTrue(Seal()); MessageLog.ConsumeAnnouncement();
            var loaded = PartRoundTripHelper.RoundTripEntity(face);
            StringAssert.Contains("kept", loaded.GetPart<ExaminablePart>()?.Text ?? "", "the examine text is part of the face's saved state");
            Assert.IsNotNull(loaded.GetPart<RootFacePart>());
        }

        [Test]
        public void Adversarial_TheLedgerEntryOfARootEnding_SurvivesARoundTrip()
        {
            Give(EndingRoutes.CuttingBlueprint); Assert.IsTrue(Gather()); MessageLog.ConsumeAnnouncement();
            var loaded = RoundTrip(StoryletPart.Current); var e = loaded.GetClosure(EndingRoutes.GatherQuestId);
            Assert.AreEqual(ClosureState.Closed, e.State); Assert.AreEqual(face.GetDisplayName(), e.GiverName); Assert.AreEqual(RootSiteBuilder.ChamberZoneID, e.Where); Assert.IsTrue(loaded.IsQuestCompleted(EndingRoutes.GatherQuestId));
        }

        [Test]
        public void Adversarial_CarriedStonesAndTheOpenAct_SurviveThePlayersRoundTrip_BeforeTheEnactment()
        {
            GiveStones(); StoryletPart.Current.StartQuest(new QuestState { QuestId = EndingRoutes.KeepQuestId, CurrentStageIndex = 0 });
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(player);
            foreach (var s in EndingRoutes.Stones) Assert.AreEqual(1, loaded.GetPart<InventoryPart>().Objects.Count(e => e.BlueprintName == s), s);
            Assert.AreEqual(0, EndingSpine.Enacted(loaded)); Assert.AreEqual(ClosureState.Open, RoundTrip(StoryletPart.Current).GetClosure(EndingRoutes.KeepQuestId).State);
        }

        // ════════════════ anti-exploit and diag contracts ════════════════

        [Test]
        public void Adversarial_AClosedRootAct_CannotBeRefused_AndRenounceTouchesNothing()
        {
            GiveStones(); Assert.IsTrue(Seal()); MessageLog.ConsumeAnnouncement();
            Assert.IsFalse(StoryletPart.Current.RefuseQuest(EndingRoutes.KeepQuestId, player)); Assert.AreEqual(ClosureState.Closed, StoryletPart.Current.GetClosure(EndingRoutes.KeepQuestId).State);
            Assert.AreEqual(0, StoryletPart.Current.RenounceLost(player));
        }

        [Test]
        public void Adversarial_EveryRejectionEmitsExactlyOneRecord_AndNeverAnEnactment()
        {
            int before = Count("ending", "Rejected");
            Assert.IsFalse(Seal()); Assert.IsFalse(Gather()); Assert.AreEqual(before + 2, Count("ending", "Rejected")); Assert.AreEqual(0, Count("ending", "Enacted"));
            GiveStones(); Assert.IsTrue(Seal()); MessageLog.ConsumeAnnouncement(); Assert.AreEqual(1, Count("ending", "Enacted"));
            Assert.IsFalse(Gather()); Assert.AreEqual(before + 3, Count("ending", "Rejected")); Assert.AreEqual(1, Count("ending", "Enacted"));
            StringAssert.Contains("\"consumed\":3", LastPayload("ending", "Enacted"));
        }

        [Test]
        public void Adversarial_TheFaceOffersNothing_ToAPlayerWhoseWorldEndedElsewhere_EvenWithEverythingCarried()
        {
            GiveStones(); Give(EndingRoutes.CuttingBlueprint); player.SetIntProperty(EndingSpine.EndingProperty, EndingSpine.VesselPath);
            Assert.IsFalse(Offered(face).Actions.Any(a => EndingRoutes.IsWorldCommand(a.Command)));
            Assert.IsFalse(Seal()); Assert.IsFalse(Gather()); Assert.AreEqual(2, Count("ending", "Rejected")); Assert.AreEqual(1, Carried(EndingRoutes.CuttingBlueprint));
        }
    }
}
