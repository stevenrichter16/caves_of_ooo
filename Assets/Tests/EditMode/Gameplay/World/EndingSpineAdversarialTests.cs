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
    /// Ending spine ES.5 (Docs/ENDING-SPINE.md): the dedicated adversarial sweep for
    /// the closure-ledger and the circle (ADVERSARIAL_TESTING.md taxonomy) plus
    /// hypothesis-driven probes of what a player actually does. Each hypothesis is
    /// stated in its docstring; the commit classifies which ran RED first.
    /// </summary>
    public sealed class EndingSpineAdversarialTests
    {
        private HotbarSaveFixture scope; private EntityFactory factory; private OverworldZoneManager manager; private Zone site; private Entity player, seventh;

        [SetUp] public void SetUp()
        {
            scope = new HotbarSaveFixture(false, false); factory = GrovelandsCompositionTests.Factory(); manager = OverworldZoneManager.CreateDetached(factory, 64);
            site = manager.GetZone(FellingSiteBuilder.ZoneID); seventh = site.GetAllEntities().Single(e => e.HasPart<SeventhPositionPart>());
            player = factory.CreateEntity("Player"); var at = site.GetEntityPosition(seventh); Assert.IsTrue(site.AddEntity(player, at.x, at.y));
            StoryletPart.Current = new StoryletPart(); StoryletPart.LocalPlayer = player; SettlementRuntime.ActiveZone = site; NarrativeStatePart.Current = new NarrativeStatePart();
            ConversationActions.Reset(); ConversationPredicates.Reset(); StoryletRegistry.Reset(); Diag.ResetAll(); MessageLog.Clear();
        }
        [TearDown] public void TearDown() { ConversationActions.Reset(); ConversationPredicates.Reset(); StoryletRegistry.Reset(); StoryletPart.Current = null; StoryletPart.LocalPlayer = null; SettlementRuntime.ActiveZone = null; NarrativeStatePart.Current = null; scope.Dispose(); }

        private static void Start(string id) => StoryletPart.Current.StartQuest(new QuestState { QuestId = id, CurrentStageIndex = 0 });
        private bool Enact(string command) => EndingSpine.TryWorldAction(seventh, player, site, command, out _);
        private static int Count(string category, string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = category, Kind = kind, Limit = 80 }).Records.Count;
        private static StoryletPart RoundTrip(StoryletPart part) { using var stream = new MemoryStream(); part.Save(new SaveWriter(stream)); stream.Position = 0; var loaded = new StoryletPart(); loaded.Load(new SaveReader(stream, null)); return loaded; }

        // ════════════════ boundary inputs ════════════════

        [Test]
        public void Adversarial_NullsAndNonsense_NeverThrow_NeverAct()
        {
            var sp = StoryletPart.Current;
            sp.SetGiver(null, player, site); sp.SetGiver("", player, site); sp.SetGiver("never", player, site); sp.SetGiver("x", null, null);
            Assert.IsFalse(sp.IsLost(null)); Assert.AreEqual("", sp.Describe(null)); Assert.AreEqual(0, sp.RenounceLost(null));
            sp.UndertakeAct(null, "t"); sp.UndertakeAct("", "t"); Assert.AreEqual(0, sp.Ledger.Count);
            Assert.IsFalse(EndingSpine.TryWorldAction(null, player, site, EndingSpine.StrikeCommand, out int c)); Assert.AreEqual(0, c);
            Assert.IsFalse(EndingSpine.TryWorldAction(seventh, null, site, EndingSpine.StrikeCommand, out _));
            Assert.IsFalse(EndingSpine.TryWorldAction(seventh, player, null, EndingSpine.StrikeCommand, out _));
            Assert.IsFalse(EndingSpine.TryWorldAction(seventh, player, site, null, out _)); Assert.IsFalse(EndingSpine.TryWorldAction(seventh, player, site, "", out _));
            EndingSpine.AddActions(null, player); Assert.AreEqual(0, EndingSpine.Enacted(null));
            Assert.AreEqual(0, EndingSpine.Enacted(player)); Assert.AreEqual(0, Count("ending", "Enacted"));
        }

        [Test]
        public void Adversarial_ANonPlayerActor_CannotEnact()
        {
            var villager = factory.CreateEntity("Villager"); var at = site.GetEntityPosition(seventh); Assert.IsTrue(site.MoveEntity(player, at.x - 1, at.y)); Assert.IsTrue(site.AddEntity(villager, at.x, at.y));
            Assert.IsFalse(EndingSpine.TryWorldAction(seventh, villager, site, EndingSpine.StrikeCommand, out _));
            Assert.AreEqual(0, EndingSpine.Enacted(villager)); Assert.AreEqual(0, Count("ending", "Enacted"));
        }

        // ════════════════ save/load reach ════════════════

        [Test]
        public void Adversarial_SaveLoad_AV1LedgerSection_LoadsWithoutGiverFields()
        {
            // The ES.2 layout: count first (no version marker), six fields per entry.
            using var stream = new MemoryStream(); var w = new SaveWriter(stream);
            w.Write(0); w.Write(0); w.Write(0); w.Write(0); w.Write(0);              // fired, quests, completed, objectives, failed
            w.Write(1); w.WriteString("old"); w.Write((int)ClosureState.Refused); w.Write(3); w.Write(9); w.Write(1); w.WriteString("Old Title");
            stream.Position = 0; var loaded = new StoryletPart(); loaded.Load(new SaveReader(stream, null));
            var e = loaded.GetClosure("old"); Assert.IsNotNull(e); Assert.AreEqual(ClosureState.Refused, e.State); Assert.AreEqual("Old Title", e.Title); Assert.IsTrue(e.Spoken); Assert.AreEqual(3, e.UndertakenTurn); Assert.AreEqual(9, e.EndedTurn);
            Assert.IsNull(e.GiverId); Assert.IsNull(e.Where); Assert.IsFalse(loaded.IsLost(e));
            Assert.AreEqual("Old Title", RoundTrip(loaded).GetClosure("old").Title, "and it re-saves in the current layout");
        }

        [Test]
        public void Adversarial_SaveLoad_TheEnactmentAndTheFactSurvive()
        {
            Assert.IsTrue(Enact(EndingSpine.StrikeCommand));
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(player); Assert.AreEqual(EndingSpine.VesselPath, EndingSpine.Enacted(loaded));
            Assert.AreEqual(EndingSpine.VesselPath, NarrativeStatePart.Current.GetFact(EndingSpine.EndingFact));
        }

        // ════════════════ stacking, re-taking, ghosting ════════════════

        [Test]
        public void Adversarial_UndertakingTheSameActTwice_KeepsOneEntry_Superseded()
        {
            var sp = StoryletPart.Current; sp.UndertakeAct("a", "First"); sp.RefuseAct("a"); sp.UndertakeAct("a", "Second");
            Assert.AreEqual(1, sp.Ledger.Count); Assert.AreEqual(ClosureState.Open, sp.GetClosure("a").State); Assert.AreEqual("Second", sp.GetClosure("a").Title);
            Assert.AreEqual(1, sp.ReadLedger().Open); Assert.AreEqual(0, sp.ReadLedger().Refused);
        }

        /// <summary>Hypothesis: a spoken no followed by re-taking and then ghosting is a
        /// ghosting — the earlier refusal does not launder the later silence.</summary>
        [Test]
        public void Hypothesis_RefuseThenRetakeThenDrop_IsOpenAgain()
        {
            var sp = StoryletPart.Current; Start("q"); Assert.IsTrue(sp.RefuseQuest("q")); Start("q"); sp.RemoveActiveQuest("q");
            Assert.AreEqual(ClosureState.Open, sp.GetClosure("q").State); Assert.IsFalse(sp.ReadLedger().Clean);
            Assert.IsFalse(Enact(EndingSpine.NameCommand)); Assert.AreEqual(0, EndingSpine.Enacted(player));
        }

        /// <summary>Hypothesis: two acts from one giver are both lost when the giver
        /// dies, and one [R] renounces both — the ledger does not make the player
        /// renounce the same loss twice.</summary>
        [Test]
        public void Hypothesis_TwoActsOneGiver_AreBothLost_AndRenouncedTogether()
        {
            var sp = StoryletPart.Current; var giver = factory.CreateEntity("Villager"); giver.ID = "test:giver"; var at = site.GetEntityPosition(seventh); Assert.IsTrue(site.AddEntity(giver, at.x + 3, at.y));
            Start("one"); sp.SetGiver("one", giver, site); Start("two"); sp.SetGiver("two", giver, site);
            Assert.AreEqual(0, sp.ReadLedger().Lost); Assert.IsTrue(site.RemoveEntity(giver)); Assert.AreEqual(2, sp.ReadLedger().Lost);
            Assert.AreEqual(2, sp.RenounceLost(player)); Assert.IsTrue(sp.ReadLedger().Clean); Assert.AreEqual(2, Count("closure", "Renounced"));
            Assert.IsTrue(Enact(EndingSpine.NameCommand));
        }

        /// <summary>Hypothesis: a giver who dies between the reading and the enactment
        /// changes nothing — the enactment reads the ledger afresh, and a dead giver
        /// makes the act lost, not closed; the practice is refused until renounced.</summary>
        [Test]
        public void Hypothesis_TheGiverDiesBetweenReadingAndEnacting()
        {
            var sp = StoryletPart.Current; var giver = factory.CreateEntity("Villager"); giver.ID = "test:giver"; var at = site.GetEntityPosition(seventh); Assert.IsTrue(site.AddEntity(giver, at.x + 3, at.y));
            Start("late"); sp.SetGiver("late", giver, site); sp.CompleteQuest("late");
            Assert.IsTrue(sp.ReadLedger().Clean); giver.SetStatValue("Hitpoints", 0);
            Assert.IsTrue(Enact(EndingSpine.NameCommand), "a closed act stays closed when its giver dies afterwards");
        }

        /// <summary>Hypothesis: completing an act whose giver is already gone still closes
        /// it (the world can be carried through without the giver).</summary>
        [Test]
        public void Hypothesis_CompletingALostActClosesIt()
        {
            var sp = StoryletPart.Current; var giver = factory.CreateEntity("Villager"); giver.ID = "test:giver"; var at = site.GetEntityPosition(seventh); Assert.IsTrue(site.AddEntity(giver, at.x + 3, at.y));
            Start("gone"); sp.SetGiver("gone", giver, site); Assert.IsTrue(site.RemoveEntity(giver)); Assert.AreEqual(1, sp.ReadLedger().Lost);
            Assert.IsTrue(sp.CompleteQuest("gone")); Assert.AreEqual(ClosureState.Closed, sp.GetClosure("gone").State); Assert.AreEqual(0, sp.ReadLedger().Lost); Assert.AreEqual(0, sp.RenounceLost(player));
        }

        [Test]
        public void Adversarial_PlaceNamesForOddZoneIds()
        {
            Assert.AreEqual("where you took it on", StoryletPart.PlaceName(WorldMap.WorldMapZoneID, manager));
            Assert.AreEqual("the Felling-Site", StoryletPart.PlaceName(FellingSiteBuilder.ZoneID, manager));
            Assert.AreEqual("where you took it on", StoryletPart.PlaceName("Overworld.-1.-1.0", manager));
            Assert.AreEqual("where you took it on", StoryletPart.PlaceName("Overworld.99.99.0", null));
        }

        // ════════════════ regional acts: the deliver hook ════════════════

        [Test]
        public void Adversarial_RegionalDeliveryClosesTheAct_ThroughTheSameApi()
        {
            var sp = StoryletPart.Current; sp.UndertakeAct("regional:i", "Oil for Sumphold", seventh, site);
            Assert.AreEqual(seventh.ID, sp.GetClosure("regional:i").GiverId);
            Assert.IsTrue(sp.CloseAct("regional:i")); Assert.AreEqual(ClosureState.Closed, sp.GetClosure("regional:i").State); Assert.IsTrue(sp.GetClosure("regional:i").Spoken);
            Assert.IsFalse(sp.RefuseAct("regional:i"), "closed is not open to refuse");
        }

        // ════════════════ diag contracts ════════════════

        [Test]
        public void Adversarial_RejectionsNeverClaimEnactment_AndEveryReadingIsRecorded()
        {
            Start("open"); Diag.ResetAll();
            Assert.IsFalse(Enact(EndingSpine.NameCommand)); Assert.AreEqual(1, Count("ending", "Rejected")); Assert.AreEqual(0, Count("ending", "Enacted")); Assert.AreEqual(1, Count("closure", "Read"), "the gate read the ledger once");
            Assert.IsTrue(Enact(EndingSpine.StrikeCommand)); Assert.AreEqual(1, Count("ending", "Enacted")); Assert.AreEqual(2, Count("closure", "Read"));
            Assert.IsFalse(Enact(EndingSpine.NameCommand)); Assert.AreEqual(2, Count("ending", "Rejected")); Assert.AreEqual(2, Count("closure", "Read"), "an already-enacted circle does not re-read");
        }

        /// <summary>Hypothesis: the Strike with an open ledger records what was left
        /// open, so the aftermath can be honest about it later.</summary>
        [Test]
        public void Hypothesis_TheStrikeRecordsTheOpenLedgerItWasEnactedOver()
        {
            Start("open"); Start("also"); Assert.IsTrue(Enact(EndingSpine.StrikeCommand));
            string payload = DiagQuery.Apply(new DiagQuery.Filter { Category = "ending", Kind = "Enacted", Limit = 5 }).Records.Last().PayloadJson.Replace(" ", "");
            StringAssert.Contains("\"open\":2", payload); StringAssert.Contains("\"path\":\"vessel\"", payload);
        }
    }
}
