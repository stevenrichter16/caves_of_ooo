using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Ending spine ES.2 (Docs/ENDING-SPINE.md): the ledger is visible in the
    /// journal, every act can be ended aloud where it was taken on, and acts
    /// outside the quest layer are ledger acts too. Conversations are driven
    /// through the real manager and the shipped JSON.
    /// </summary>
    public sealed class ClosureVisibilityTests
    {
        private static void LoadHermitQuest() { StoryletRegistry.GetAll(); StoryletRegistry.LoadFromJson(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Storylets/MessageForHermit.json")), "MessageForHermit.json"); }

        [SetUp] public void SetUp()
        {
            ConversationActions.Reset(); ConversationPredicates.Reset(); ConversationLoader.Reset(); StoryletRegistry.Reset(); Diag.ResetAll(); MessageLog.Clear();
            StoryletPart.Current = new StoryletPart(); StoryletPart.LocalPlayer = null;
        }
        [TearDown] public void TearDown() { ConversationManager.EndConversation(); ConversationActions.Reset(); ConversationPredicates.Reset(); ConversationLoader.Reset(); StoryletRegistry.Reset(); StoryletPart.Current = null; StoryletPart.LocalPlayer = null; }

        private static void Start(StoryletPart sp, string id) => sp.StartQuest(new QuestState { QuestId = id, CurrentStageIndex = 0 });
        private static StoryletPart RoundTrip(StoryletPart part)
        {
            using var stream = new MemoryStream(); part.Save(new SaveWriter(stream)); stream.Position = 0;
            var loaded = new StoryletPart(); loaded.Load(new SaveReader(stream, null)); return loaded;
        }
        private static string ConversationPath(string file) => Path.Combine(Application.dataPath, "Resources/Content/Conversations", file);
        private static Entity Speaker(string conversationId) { var e = new Entity { BlueprintName = "Villager", ID = "giver:" + conversationId }; e.AddPart(new ConversationPart { ConversationID = conversationId }); return e; }
        private static Entity Player() { var p = new Entity { BlueprintName = "Player", ID = "player" }; p.SetTag("Player", ""); p.AddPart(new InventoryPart()); StoryletPart.LocalPlayer = p; return p; }
        private static List<string> Choices() => ConversationManager.VisibleChoices.Select(c => c.Text).ToList();
        private static bool Select(string prefix)
        {
            for (int i = 0; i < ConversationManager.VisibleChoices.Count; i++)
                if (ConversationManager.VisibleChoices[i].Text.StartsWith(prefix)) { ConversationManager.SelectChoice(i); return true; }
            return false;
        }

        // ════════════════ the journal ════════════════

        [Test]
        public void TheJournalShowsClosureCounts_RefusedTitles_AndCompletedDisplayNames()
        {
            // A valid minimal definition: a quest needs at least one stage to be a quest, and the
            // registry must be warmed first or its first lookup reloads the shipped files over us.
            StoryletRegistry.GetAll();
            StoryletRegistry.LoadFromJson("{\"Storylets\":[{\"ID\":\"AlphaErrand\",\"Triggers\":[],\"Effects\":[],\"Quest\":{\"Name\":\"Alpha Errand\",\"Stages\":[{\"ID\":\"only\",\"Triggers\":[],\"OnEnter\":[],\"Objectives\":[]}]}}]}", "test");
            Assert.AreEqual("Alpha Errand", StoryletPart.QuestDisplayName("AlphaErrand"), "the definition registered with its name");
            var sp = StoryletPart.Current; Start(sp, "AlphaErrand"); Start(sp, "B"); Start(sp, "C"); Start(sp, "D");
            sp.CompleteQuest("AlphaErrand"); sp.RefuseQuest("B"); sp.RemoveActiveQuest("D");
            var snap = QuestLogStateBuilder.Build(sp);
            Assert.AreEqual(1, snap.ClosureClosed); Assert.AreEqual(1, snap.ClosureRefused); Assert.AreEqual(2, snap.ClosureOpen, "the active act and the silently dropped one are both open");
            CollectionAssert.AreEqual(new[] { "Alpha Errand" }, snap.Completed, "display names, not ids");
            CollectionAssert.AreEqual(new[] { "B" }, snap.Refused, "an unregistered id is shown as itself"); Assert.AreEqual(1, snap.RefusedCount);
            Assert.AreEqual(1, snap.ActiveCount);
        }

        [Test]
        public void ARefusedActAlone_IsStillAJournalWorthShowing()
        {
            var sp = StoryletPart.Current; Start(sp, "only"); sp.RefuseQuest("only");
            var snap = QuestLogStateBuilder.Build(sp);
            Assert.AreEqual(0, snap.ActiveCount); Assert.AreEqual(0, snap.CompletedCount); Assert.AreEqual(1, snap.RefusedCount);
            Assert.IsFalse(snap.ActiveCount == 0 && snap.CompletedCount == 0 && snap.RefusedCount == 0, "the empty-state condition includes refused");
        }

        // ════════════════ acts outside the quest layer ════════════════

        [Test]
        public void GenericActs_AreUndertakenClosedOrRefused_WithTitles_AndRoundTrip()
        {
            var sp = StoryletPart.Current;
            sp.UndertakeAct("regional:x", "Oil for Sumphold");
            var e = sp.GetClosure("regional:x"); Assert.AreEqual(ClosureState.Open, e.State); Assert.AreEqual("Oil for Sumphold", e.Title); Assert.AreEqual("Oil for Sumphold", StoryletPart.ClosureTitle(e));
            Assert.IsTrue(sp.RefuseAct("regional:x")); Assert.AreEqual(ClosureState.Refused, sp.GetClosure("regional:x").State);
            Assert.IsFalse(sp.CloseAct("regional:x"), "a refused act is not open to close");
            sp.UndertakeAct("regional:x", "Oil for Sumphold"); Assert.IsTrue(sp.CloseAct("regional:x")); Assert.AreEqual(ClosureState.Closed, sp.GetClosure("regional:x").State);
            sp.UndertakeAct("regional:y", "Salt for Marrowstye"); sp.RefuseAct("regional:y");
            var loaded = RoundTrip(sp);
            Assert.AreEqual("Salt for Marrowstye", loaded.GetClosure("regional:y").Title); Assert.AreEqual(ClosureState.Refused, loaded.GetClosure("regional:y").State);
            CollectionAssert.AreEqual(new[] { "Salt for Marrowstye" }, QuestLogStateBuilder.Build(loaded).Refused);
            Assert.IsFalse(sp.CloseAct("never")); Assert.IsFalse(sp.RefuseAct(null)); Assert.IsFalse(sp.RefuseAct(""));
        }

        // ════════════════ the verb and the predicate ════════════════

        [Test]
        public void RefuseQuestAction_AndIfQuestRefusedPredicate_AreRegistered_AndAgree()
        {
            ConversationActions.EnsureInitialized(); ConversationPredicates.EnsureInitialized();
            Assert.IsTrue(ConversationActions.IsRegistered("RefuseQuest")); Assert.IsTrue(ConversationPredicates.IsRegistered("IfQuestRefused"));
            var sp = StoryletPart.Current; Start(sp, "Q");
            Assert.IsFalse(ConversationPredicates.Evaluate("IfQuestRefused", null, null, "Q"));
            ConversationActions.Execute("RefuseQuest", null, null, "Q");
            Assert.IsTrue(ConversationPredicates.Evaluate("IfQuestRefused", null, null, "Q"));
            Assert.IsFalse(sp.IsQuestActive("Q")); Assert.IsFalse(ConversationPredicates.Evaluate("IfQuestFailed", null, null, "Q"));
            Assert.IsTrue(ConversationPredicates.Evaluate("IfQuestNotStarted", null, null, "Q"), "a refused act can be taken up again");
            Start(sp, "Q"); Assert.IsFalse(ConversationPredicates.Evaluate("IfQuestRefused", null, null, "Q"), "taken up again: no longer refused");
            ConversationActions.Execute("RefuseQuest", null, null, ""); ConversationActions.Execute("RefuseQuest", null, null, "Never");
            Assert.IsTrue(sp.IsQuestActive("Q"));
        }

        // ════════════════ every giver offers a spoken no ════════════════

        [Test]
        public void EveryQuestGiver_OffersToBeToldNoToTheirFace()
        {
            var files = Directory.GetFiles(Path.Combine(Application.dataPath, "Resources/Content/Conversations"), "*_Quest.json");
            int audited = 0;
            foreach (var file in files)
            {
                ConversationLoader.Reset(); ConversationLoader.LoadFromJson(File.ReadAllText(file), Path.GetFileName(file));
                foreach (var conv in ConversationsIn(file))
                {
                    var starts = conv.Nodes.SelectMany(n => n.Choices).SelectMany(c => c.Actions ?? new List<ConversationParam>()).Where(a => a.Key == "StartQuest").Select(a => a.Value).Distinct().ToList();
                    foreach (var q in starts)
                    {
                        audited++;
                        var refuse = conv.Nodes.SelectMany(n => n.Choices).Where(c => (c.Actions ?? new List<ConversationParam>()).Any(a => a.Key == "RefuseQuest" && a.Value == q)).ToList();
                        Assert.IsNotEmpty(refuse, Path.GetFileName(file) + ": " + q + " has no spoken no");
                        foreach (var c in refuse)
                        {
                            Assert.IsTrue((c.Predicates ?? new List<ConversationParam>()).Any(p => p.Key == "IfQuestActive" && p.Value == q), Path.GetFileName(file) + ": refusal is offered only while the act is undertaken");
                            var target = conv.GetNode(c.Target); Assert.IsNotNull(target, Path.GetFileName(file) + ": refusal target exists");
                            Assert.IsTrue(target.Choices.Any(x => x.Target == "End" || x.Target == "Start" || x.Target == "__end__"), Path.GetFileName(file) + ": the giver's reply can be left");
                            Assert.IsFalse((c.Actions ?? new List<ConversationParam>()).Any(a => a.Key == "FailQuest"), "a spoken no is never recorded as a failure");
                        }
                    }
                }
            }
            Assert.GreaterOrEqual(audited, 10, "the quest-giver roster was audited");
        }
        private static IEnumerable<ConversationData> ConversationsIn(string file)
        {
            foreach (var id in IdsIn(file)) { var c = ConversationLoader.Get(id); if (c != null) yield return c; }
        }
        private static IEnumerable<string> IdsIn(string file)
        {
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(file), "\"ID\"\\s*:\\s*\"([A-Za-z0-9_]+_Quest)\""))
                yield return m.Groups[1].Value;
        }

        [Test]
        public void Baker_ReleasingToHisFace_IsARefusal_AndTheErrandCanBeTakenAgain()
        {
            ConversationLoader.LoadFromJson(File.ReadAllText(ConversationPath("Baker_Quest.json")), "Baker_Quest.json");
            LoadHermitQuest();
            var baker = Speaker("Baker_Quest"); var player = Player(); var sp = StoryletPart.Current;
            Assert.IsTrue(ConversationManager.StartConversation(baker, player));
            Assert.IsFalse(Choices().Any(c => c.StartsWith("[Release]")), "nothing to release before accepting");
            Assert.IsTrue(Select("[Accept]")); ConversationManager.EndConversation();
            Assert.IsTrue(sp.IsQuestActive("MessageForHermit"));
            Assert.IsTrue(ConversationManager.StartConversation(baker, player));
            Assert.IsTrue(Select("[Release]")); Assert.AreEqual("Released", ConversationManager.CurrentNode?.ID, "the baker answers");
            StringAssert.Contains("kinder to hear it", ConversationManager.CurrentNode.Text);
            ConversationManager.EndConversation();
            Assert.IsFalse(sp.IsQuestActive("MessageForHermit")); Assert.IsFalse(sp.IsQuestFailed("MessageForHermit"));
            Assert.AreEqual(ClosureState.Refused, sp.GetClosure("MessageForHermit").State); Assert.IsTrue(sp.ReadLedger().Clean);
            Assert.IsTrue(ConversationManager.StartConversation(baker, player));
            Assert.IsTrue(Choices().Any(c => c.StartsWith("[Accept]")), "and it can be taken up again"); Assert.IsFalse(Choices().Any(c => c.StartsWith("[Release]")));
        }

        [Test]
        public void CandyTax_TheSpokenDecline_IsARefusal_AndTheAftermathReadsIt()
        {
            ConversationLoader.LoadFromJson(File.ReadAllText(ConversationPath("CandyTax_Quest.json")), "CandyTax_Quest.json");
            var clerk = Speaker("CandyTax_Quest"); var player = Player(); var sp = StoryletPart.Current; Start(sp, "TheCandyTax");
            Assert.IsTrue(ConversationManager.StartConversation(clerk, player));
            Assert.IsTrue(Select("Your tax squabble")); ConversationManager.EndConversation();
            Assert.AreEqual(ClosureState.Refused, sp.GetClosure("TheCandyTax").State); Assert.IsFalse(sp.IsQuestFailed("TheCandyTax"), "no longer recorded as a failure");
            Assert.IsTrue(ConversationManager.StartConversation(clerk, player));
            Assert.IsTrue(Choices().Any(c => c.StartsWith("About the tax business")), "the aftermath is gated on the refusal");
        }
    }
}
