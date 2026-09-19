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
    /// Ending spine ES.3 (Docs/ENDING-SPINE.md): who an act was taken on from and
    /// where are on the ledger; a reading says where to end each open act; an act
    /// whose giver is gone — known, loaded, and not there alive — is lost and can be
    /// renounced aloud from the journal. Anything unknown stays open, never "lost".
    /// </summary>
    public sealed class ClosureLostTests
    {
        private HotbarSaveFixture scope; private EntityFactory factory; private OverworldZoneManager manager; private Zone sill; private Entity player, baker;
        private const string Quest = "MessageForHermit";

        [SetUp] public void SetUp()
        {
            scope = new HotbarSaveFixture(false, false); ConversationActions.Reset(); ConversationPredicates.Reset(); ConversationLoader.Reset(); StoryletRegistry.Reset(); Diag.ResetAll(); MessageLog.Clear();
            factory = GrovelandsCompositionTests.Factory(); manager = OverworldZoneManager.CreateDetached(factory, 64); sill = manager.GetZone("Overworld.10.10.0");
            StoryletRegistry.LoadFromJson(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Storylets/MessageForHermit.json")), "MessageForHermit.json");
            ConversationLoader.LoadFromJson(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Conversations/Baker_Quest.json")), "Baker_Quest.json");
            baker = factory.CreateEntity("Villager"); baker.ID = "test:baker"; baker.GetPart<RenderPart>().DisplayName = "Orrin the baker";
            var talk = baker.GetPart<ConversationPart>(); if (talk == null) { talk = new ConversationPart(); baker.AddPart(talk); } talk.ConversationID = "Baker_Quest"; // the Villager blueprint already carries one
            var seat = Free(sill); Assert.IsTrue(sill.AddEntity(baker, seat.X, seat.Y));
            player = factory.CreateEntity("Player"); var beside = Beside(sill, baker); Assert.IsTrue(sill.AddEntity(player, beside.X, beside.Y));
            StoryletPart.Current = new StoryletPart(); StoryletPart.LocalPlayer = player; SettlementRuntime.ActiveZone = sill;
            ConversationActions.EnsureInitialized(); ConversationPredicates.EnsureInitialized();
        }
        [TearDown] public void TearDown() { ConversationManager.EndConversation(); ConversationActions.Reset(); ConversationPredicates.Reset(); ConversationLoader.Reset(); StoryletRegistry.Reset(); StoryletPart.Current = null; StoryletPart.LocalPlayer = null; SettlementRuntime.ActiveZone = null; scope.Dispose(); }

        private static Cell Free(Zone z) { for (int y = 2; y < Zone.Height - 2; y++) for (int x = 2; x < Zone.Width - 2; x++) { var c = z.GetCell(x, y); if (!c.BlocksMovement() && z.GetCell(x + 1, y) != null && !z.GetCell(x + 1, y).BlocksMovement()) return c; } Assert.Fail("no free pair"); return null; }
        private static Cell Beside(Zone z, Entity e) { var p = z.GetEntityPosition(e); for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) { if (dx == 0 && dy == 0) continue; var c = z.GetCell(p.x + dx, p.y + dy); if (c != null && !c.BlocksMovement()) return c; } Assert.Fail("no cell beside"); return null; }
        private static bool Select(string prefix) { for (int i = 0; i < ConversationManager.VisibleChoices.Count; i++) if (ConversationManager.VisibleChoices[i].Text.StartsWith(prefix)) { ConversationManager.SelectChoice(i); return true; } return false; }
        private void AcceptFromTheBaker() { Assert.IsTrue(ConversationManager.StartConversation(baker, player)); Assert.IsTrue(Select("[Accept]")); ConversationManager.EndConversation(); Assert.IsTrue(StoryletPart.Current.IsQuestActive(Quest)); }
        private static StoryletPart RoundTrip(StoryletPart part) { using var stream = new MemoryStream(); part.Save(new SaveWriter(stream)); stream.Position = 0; var loaded = new StoryletPart(); loaded.Load(new SaveReader(stream, null)); return loaded; }
        private static int Count(string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = "closure", Kind = kind, Limit = 50 }).Records.Count;

        [Test]
        public void AcceptingFromTheBaker_RecordsWhoAndWhere_AndTheReadingSaysWhereToEndIt()
        {
            AcceptFromTheBaker(); var e = StoryletPart.Current.GetClosure(Quest);
            Assert.AreEqual(baker.ID, e.GiverId); Assert.AreEqual("Orrin the baker", e.GiverName); Assert.AreEqual(sill.ZoneID, e.Where);
            var r = StoryletPart.Current.ReadLedger(); Assert.AreEqual(1, r.Open); Assert.AreEqual(0, r.Lost);
            StringAssert.Contains("end it with Orrin the baker at Sill", r.Descriptions.Single());
            Assert.IsFalse(StoryletPart.Current.IsLost(e)); Assert.AreEqual(0, StoryletPart.Current.RenounceLost(player), "a healthy giver's act is not renounceable");
        }

        [Test]
        public void AGiverGoneFromTheLoadedPlace_IsLost_AndCanBeRenouncedAloud_Once()
        {
            AcceptFromTheBaker(); Diag.ResetAll(); Assert.IsTrue(sill.RemoveEntity(baker));
            var e = StoryletPart.Current.GetClosure(Quest); Assert.IsTrue(StoryletPart.Current.IsLost(e));
            var r = StoryletPart.Current.ReadLedger(); Assert.AreEqual(1, r.Lost); StringAssert.Contains("Orrin the baker is gone; [R] renounces it", r.Descriptions.Single());
            Assert.AreEqual(1, StoryletPart.Current.RenounceLost(player));
            Assert.AreEqual(ClosureState.Refused, e.State); Assert.IsTrue(e.Spoken); Assert.IsFalse(StoryletPart.Current.IsQuestActive(Quest)); Assert.IsFalse(StoryletPart.Current.IsQuestFailed(Quest));
            Assert.IsTrue(StoryletPart.Current.ReadLedger().Clean); Assert.AreEqual(1, Count("Renounced")); Assert.AreEqual(1, Count("Refused"));
            StringAssert.Contains("Renounced:", string.Join("\n", MessageLog.GetRecent(4)));
            Assert.AreEqual(0, StoryletPart.Current.RenounceLost(player), "once");
        }

        [Test]
        public void ADeadGiverStillStanding_IsLost()
        {
            AcceptFromTheBaker(); baker.SetStatValue("Hitpoints", 0);
            Assert.IsTrue(StoryletPart.Current.IsLost(StoryletPart.Current.GetClosure(Quest)));
            Assert.AreEqual(1, StoryletPart.Current.RenounceLost(player));
        }

        [Test]
        public void AnUnknownGiver_IsNeverLost_AndRenounceTouchesNothing()
        {
            // Counter-check: an act taken on by code, with no giver recorded.
            StoryletPart.Current.StartQuest(new QuestState { QuestId = "code-started", CurrentStageIndex = 0 });
            var e = StoryletPart.Current.GetClosure("code-started"); Assert.IsNull(e.GiverId);
            Assert.IsFalse(StoryletPart.Current.IsLost(e)); StringAssert.Contains("end it where you took it on", StoryletPart.Current.Describe(e));
            Assert.AreEqual(0, StoryletPart.Current.RenounceLost(player)); Assert.AreEqual(ClosureState.Open, e.State);
        }

        [Test]
        public void AnUnloadedPlace_IsUnknown_NotLost()
        {
            StoryletPart.Current.StartQuest(new QuestState { QuestId = "far", CurrentStageIndex = 0 });
            StoryletPart.Current.SetGiver("far", baker, new Zone("Overworld.19.19.0"));
            var e = StoryletPart.Current.GetClosure("far"); Assert.AreEqual("Overworld.19.19.0", e.Where);
            Assert.IsFalse(StoryletPart.Current.IsLost(e), "a place that is not loaded cannot testify");
            Assert.AreEqual(0, StoryletPart.Current.RenounceLost(player));
            SettlementRuntime.ActiveZone = null; Assert.IsFalse(StoryletPart.Current.IsLost(StoryletPart.Current.GetClosure(Quest)), "no active zone: nothing is lost");
        }

        [Test]
        public void ADroppedActWithALivingGiver_IsUnspoken_NotLost_AndStillNamesTheGiver()
        {
            AcceptFromTheBaker(); StoryletPart.Current.RemoveActiveQuest(Quest);
            var snap = QuestLogStateBuilder.Build(StoryletPart.Current);
            Assert.AreEqual(0, snap.ActiveCount); Assert.AreEqual(1, snap.UnspokenCount); Assert.AreEqual(0, snap.LostCount);
            StringAssert.Contains("end it with Orrin the baker at Sill", snap.Unspoken.Single());
            Assert.AreEqual(0, StoryletPart.Current.RenounceLost(player), "a living giver can still be told no");
        }

        [Test]
        public void TheJournal_NamesLostActsUnderUnspoken_AndActiveHealthyOnesOnlyUnderActive()
        {
            AcceptFromTheBaker(); StoryletPart.Current.StartQuest(new QuestState { QuestId = "other", CurrentStageIndex = 0 });
            var before = QuestLogStateBuilder.Build(StoryletPart.Current); Assert.AreEqual(2, before.ActiveCount); Assert.AreEqual(0, before.UnspokenCount);
            Assert.IsTrue(sill.RemoveEntity(baker));
            var after = QuestLogStateBuilder.Build(StoryletPart.Current); Assert.AreEqual(2, after.ActiveCount); Assert.AreEqual(1, after.UnspokenCount); Assert.AreEqual(1, after.LostCount);
            StringAssert.Contains("is gone; [R] renounces it", after.Unspoken.Single());
        }

        [Test]
        public void SaveLoad_KeepsGiverPlaceAndTitle()
        {
            AcceptFromTheBaker(); StoryletPart.Current.UndertakeAct("regional:x", "Oil for Sumphold", baker, sill);
            var loaded = RoundTrip(StoryletPart.Current);
            var q = loaded.GetClosure(Quest); Assert.AreEqual(baker.ID, q.GiverId); Assert.AreEqual("Orrin the baker", q.GiverName); Assert.AreEqual(sill.ZoneID, q.Where);
            var a = loaded.GetClosure("regional:x"); Assert.AreEqual("Oil for Sumphold", a.Title); Assert.AreEqual(baker.ID, a.GiverId); Assert.AreEqual(sill.ZoneID, a.Where);
        }

        [Test]
        public void PlaceNames_ComeFromTheMap_WithADepthPrefix_AndAWildsFallback()
        {
            Assert.AreEqual("Sill", StoryletPart.PlaceName("Overworld.10.10.0", manager));
            Assert.AreEqual("below Sill", StoryletPart.PlaceName("Overworld.10.10.2", manager));
            StringAssert.StartsWith("the wilds at (", StoryletPart.PlaceName("Overworld.0.0.0", manager));
            Assert.AreEqual("where you took it on", StoryletPart.PlaceName(null, manager)); Assert.AreEqual("where you took it on", StoryletPart.PlaceName("garbage", manager));
        }
    }
}
