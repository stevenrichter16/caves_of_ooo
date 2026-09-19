using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Ending spine ES.1 (Docs/ENDING-SPINE.md): the closure-ledger. Every
    /// undertaken act is open until it ends in a sentence — closed by carrying
    /// it through, or refused by a spoken no. A silent drop and a failure leave
    /// it open; the ledger never writes an act off; a reading names what is
    /// still open. The scale is a list, not a ratio.
    /// </summary>
    public sealed class ClosureLedgerTests
    {
        [SetUp] public void SetUp() { ConversationPredicates.Reset(); StoryletRegistry.Reset(); Diag.ResetAll(); StoryletPart.Current = null; StoryletPart.LocalPlayer = null; MessageLog.Clear(); }
        [TearDown] public void TearDown() { ConversationPredicates.Reset(); StoryletRegistry.Reset(); StoryletPart.Current = null; StoryletPart.LocalPlayer = null; }

        private static StoryletPart RoundTrip(StoryletPart part)
        {
            using var stream = new MemoryStream();
            part.Save(new SaveWriter(stream)); stream.Position = 0;
            var loaded = new StoryletPart(); loaded.Load(new SaveReader(stream, null)); return loaded;
        }
        private static void Start(StoryletPart sp, string id) => sp.StartQuest(new QuestState { QuestId = id, CurrentStageIndex = 0 });
        private static int Count(string category, string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = category, Kind = kind, Limit = 50 }).Records.Count;
        private static string Last(string category, string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = category, Kind = kind, Limit = 50 }).Records.Last().PayloadJson;

        // ════════════════ the three states ════════════════

        [Test]
        public void Undertaking_OpensTheAct_AndTheReadingNamesIt()
        {
            var sp = new StoryletPart(); Start(sp, "A");
            var e = sp.GetClosure("A"); Assert.IsNotNull(e); Assert.AreEqual(ClosureState.Open, e.State); Assert.IsTrue(e.IsOpen); Assert.IsFalse(e.Spoken); Assert.AreEqual(-1, e.EndedTurn);
            var r = sp.ReadLedger(); Assert.AreEqual(1, r.Open); Assert.AreEqual(0, r.Closed); Assert.AreEqual(0, r.Refused); CollectionAssert.AreEqual(new[] { "A" }, r.OpenIds); Assert.IsFalse(r.Clean);
            Assert.AreEqual(1, Count("closure", "Undertaken")); StringAssert.Contains("\"spoken\":false", Last("closure", "Undertaken").Replace(" ", ""));
            Assert.IsTrue(Diag.IsChannelEnabled("closure"), "closure is a default diag category");
        }

        [Test]
        public void CarryingThrough_ClosesTheAct_Spoken()
        {
            var sp = new StoryletPart(); Start(sp, "A"); Assert.IsTrue(sp.CompleteQuest("A"));
            var e = sp.GetClosure("A"); Assert.AreEqual(ClosureState.Closed, e.State); Assert.IsTrue(e.Spoken); Assert.GreaterOrEqual(e.EndedTurn, 0);
            var r = sp.ReadLedger(); Assert.IsTrue(r.Clean); Assert.AreEqual(1, r.Closed);
            Assert.AreEqual(1, Count("closure", "Closed")); StringAssert.Contains("\"spoken\":true", Last("closure", "Closed").Replace(" ", ""));
        }

        [Test]
        public void RefusingAloud_EndsTheAct_IsNotAFailure_AndCleansTheLedger()
        {
            var sp = new StoryletPart(); Start(sp, "A");
            Assert.IsTrue(sp.RefuseQuest("A"));
            Assert.IsFalse(sp.IsQuestActive("A")); Assert.IsFalse(sp.IsQuestFailed("A")); Assert.IsFalse(sp.IsQuestCompleted("A"));
            var e = sp.GetClosure("A"); Assert.AreEqual(ClosureState.Refused, e.State); Assert.IsTrue(e.Spoken);
            var r = sp.ReadLedger(); Assert.IsTrue(r.Clean); Assert.AreEqual(1, r.Refused);
            Assert.AreEqual(1, Count("closure", "Refused")); Assert.AreEqual(1, Count("quest", "Refused"));
            StringAssert.Contains("Ended aloud", string.Join("\n", MessageLog.GetRecent(4)));
        }

        [Test]
        public void RefusingWhatWasNeverTaken_IsRejected_AndWritesNothing()
        {
            var sp = new StoryletPart(); Diag.ResetAll();
            Assert.IsFalse(sp.RefuseQuest("Never")); Assert.IsFalse(sp.RefuseQuest(null)); Assert.IsFalse(sp.RefuseQuest(""));
            Assert.IsNull(sp.GetClosure("Never")); Assert.AreEqual(0, sp.Ledger.Count);
            Assert.AreEqual(0, Count("closure", "Refused")); StringAssert.Contains("quest_not_active", Last("quest", "Rejected"));
        }

        // ════════════════ what does NOT close an act ════════════════

        [Test]
        public void ASilentDrop_LeavesTheActOpen_AndIsOnlyLogged()
        {
            // Counter-check for RefuseQuest: RemoveActiveQuest is not a spoken no.
            var sp = new StoryletPart(); Start(sp, "A"); Diag.ResetAll();
            sp.RemoveActiveQuest("A");
            Assert.IsFalse(sp.IsQuestActive("A"));
            Assert.AreEqual(ClosureState.Open, sp.GetClosure("A").State); Assert.IsFalse(sp.GetClosure("A").Spoken);
            Assert.AreEqual(1, sp.ReadLedger().Open);
            Assert.AreEqual(1, Count("closure", "Dropped")); Assert.AreEqual(0, Count("closure", "Refused")); Assert.AreEqual(0, Count("closure", "Closed"));
            sp.RemoveActiveQuest("A"); Assert.AreEqual(1, Count("closure", "Dropped"), "dropping what is not active logs nothing");
        }

        [Test]
        public void AFailure_LeavesTheActOpen()
        {
            var sp = new StoryletPart(); Start(sp, "A"); Assert.IsTrue(sp.FailQuest("A"));
            Assert.IsTrue(sp.IsQuestFailed("A")); Assert.AreEqual(ClosureState.Open, sp.GetClosure("A").State);
            Assert.AreEqual(1, sp.ReadLedger().Open);
            // The failed act can still be ended aloud once re-taken, or completed.
            Start(sp, "A"); Assert.IsTrue(sp.RefuseQuest("A")); Assert.AreEqual(ClosureState.Refused, sp.GetClosure("A").State); Assert.IsFalse(sp.IsQuestFailed("A"));
        }

        [Test]
        public void CheapClosuresNeverOffsetAnOpenAct()
        {
            var sp = new StoryletPart();
            for (int i = 0; i < 20; i++) { Start(sp, "cheap" + i); sp.CompleteQuest("cheap" + i); }
            Start(sp, "big"); sp.RemoveActiveQuest("big");
            var r = sp.ReadLedger(); Assert.AreEqual(20, r.Closed); Assert.AreEqual(1, r.Open); Assert.IsFalse(r.Clean, "a list, never a ratio"); CollectionAssert.AreEqual(new[] { "big" }, r.OpenIds);
        }

        // ════════════════ re-taking and reading ════════════════

        [Test]
        public void RetakingAfterARefusal_IsANewUndertaking_ThatSupersedesTheOldEnd()
        {
            var sp = new StoryletPart(); Start(sp, "A"); Assert.IsTrue(sp.RefuseQuest("A"));
            Start(sp, "A");
            var e = sp.GetClosure("A"); Assert.AreEqual(ClosureState.Open, e.State); Assert.IsFalse(e.Spoken); Assert.AreEqual(-1, e.EndedTurn);
            Assert.AreEqual(1, sp.ReadLedger().Open); Assert.AreEqual(0, sp.ReadLedger().Refused);
            Assert.IsTrue(sp.CompleteQuest("A")); Assert.AreEqual(ClosureState.Closed, sp.GetClosure("A").State); Assert.IsTrue(sp.ReadLedger().Clean);
        }

        [Test]
        public void ReStartingAnActiveAct_DoesNotUndertakeItTwice()
        {
            var sp = new StoryletPart(); Start(sp, "A"); Diag.ResetAll();
            sp.StartQuest(new QuestState { QuestId = "A", CurrentStageIndex = 1 });
            Assert.AreEqual(0, Count("closure", "Undertaken")); Assert.AreEqual(1, sp.Ledger.Count);
        }

        [Test]
        public void TheReading_NamesOpenActsInStableOrder_AndHasNoSideEffects()
        {
            var sp = new StoryletPart();
            Start(sp, "zeta"); Start(sp, "alpha"); Start(sp, "mid");
            sp.CompleteQuest("mid"); sp.RefuseQuest("zeta"); Start(sp, "beta");
            var r1 = sp.ReadLedger(); var r2 = sp.ReadLedger();
            Assert.AreEqual(1, r1.Closed); Assert.AreEqual(1, r1.Refused); Assert.AreEqual(2, r1.Open);
            CollectionAssert.AreEqual(new[] { "alpha", "beta" }, r1.OpenIds); CollectionAssert.AreEqual(r1.OpenIds, r2.OpenIds);
            Assert.AreEqual(2, Count("closure", "Read")); StringAssert.Contains("alpha,beta", Last("closure", "Read"));
        }

        // ════════════════ save/load ════════════════

        [Test]
        public void TheLedgerRoundTrips_WithStatesTurnsAndSpokenness()
        {
            var sp = new StoryletPart(); Start(sp, "open"); Start(sp, "done"); Start(sp, "no"); Start(sp, "dropped");
            sp.CompleteQuest("done"); sp.RefuseQuest("no"); sp.RemoveActiveQuest("dropped");
            var loaded = RoundTrip(sp);
            Assert.AreEqual(4, loaded.Ledger.Count);
            Assert.AreEqual(ClosureState.Open, loaded.GetClosure("open").State); Assert.IsTrue(loaded.IsQuestActive("open"));
            Assert.AreEqual(ClosureState.Closed, loaded.GetClosure("done").State); Assert.IsTrue(loaded.GetClosure("done").Spoken); Assert.AreEqual(sp.GetClosure("done").EndedTurn, loaded.GetClosure("done").EndedTurn);
            Assert.AreEqual(ClosureState.Refused, loaded.GetClosure("no").State); Assert.IsTrue(loaded.GetClosure("no").Spoken); Assert.IsFalse(loaded.IsQuestFailed("no"));
            Assert.AreEqual(ClosureState.Open, loaded.GetClosure("dropped").State); Assert.IsFalse(loaded.IsQuestActive("dropped"));
            Assert.AreEqual(sp.GetClosure("open").UndertakenTurn, loaded.GetClosure("open").UndertakenTurn);
            var r = loaded.ReadLedger(); Assert.AreEqual(2, r.Open); CollectionAssert.AreEqual(new[] { "dropped", "open" }, r.OpenIds);
        }

        [Test]
        public void AnOlderSaveWithoutALedger_ProjectsItFromTheQuestSets()
        {
            // The pre-ES.1 layout, written by hand: fired storylets, active quests,
            // completed set, finished-objective sets, failed set — and nothing after.
            using var stream = new MemoryStream(); var w = new SaveWriter(stream);
            w.Write(0);
            w.Write(1); w.WriteString("active"); w.WriteString("active"); w.Write(0); w.Write(0);
            w.Write(1); w.WriteString("done");
            w.Write(0);
            w.Write(1); w.WriteString("failed");
            stream.Position = 0;
            var loaded = new StoryletPart(); loaded.Load(new SaveReader(stream, null));
            Assert.IsTrue(loaded.IsQuestActive("active")); Assert.IsTrue(loaded.IsQuestCompleted("done")); Assert.IsTrue(loaded.IsQuestFailed("failed"));
            Assert.AreEqual(ClosureState.Open, loaded.GetClosure("active").State);
            Assert.AreEqual(ClosureState.Closed, loaded.GetClosure("done").State); Assert.IsTrue(loaded.GetClosure("done").Spoken); Assert.AreEqual(-1, loaded.GetClosure("done").UndertakenTurn, "projected, not stamped");
            Assert.AreEqual(ClosureState.Open, loaded.GetClosure("failed").State);
            var r = loaded.ReadLedger(); Assert.AreEqual(1, r.Closed); Assert.AreEqual(2, r.Open);
            Assert.AreEqual(3, RoundTrip(loaded).Ledger.Count, "and the projection then round-trips as a real ledger");
        }
    }
}
