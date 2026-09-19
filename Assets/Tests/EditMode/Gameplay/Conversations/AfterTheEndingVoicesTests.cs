using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Breadth pass B.3 (Docs/BREADTH-PASS.md): after any ending, no giver offers the
    /// makings of another (the memory-marble, the mute-stone, the Choir's thread), and
    /// the four voices tied to the endings — the Recension Searcher, the Curation
    /// Indexer, the Choir's tendrils, the Palimpsest's echo — say, when asked, what
    /// changed under the ending actually enacted, and never which ending was right.
    /// </summary>
    public sealed class AfterTheEndingVoicesTests
    {
        private HotbarSaveFixture scope; private EntityFactory factory; private OverworldZoneManager manager; private Entity player; private Zone here;
        private EntityFactory previousConversationFactory;

        [SetUp] public void SetUp()
        {
            scope = new HotbarSaveFixture(false, false); factory = GrovelandsCompositionTests.Factory(); manager = OverworldZoneManager.CreateDetached(factory, 64);
            player = factory.CreateEntity("Player");
            StoryletPart.Current = new StoryletPart(); StoryletPart.LocalPlayer = player; NarrativeStatePart.Current = new NarrativeStatePart();
            previousConversationFactory = ConversationActions.Factory; ConversationActions.Factory = factory; StoryletRegistry.Reset(); StoryletRegistry.LoadAll();
            StillleafArchiveContent.EnsureRegistered();
        }
        [TearDown] public void TearDown() { ConversationManager.EndConversation(); ConversationActions.Factory = previousConversationFactory; StoryletRegistry.Reset(); StoryletPart.Current = null; StoryletPart.LocalPlayer = null; NarrativeStatePart.Current = null; SettlementRuntime.ActiveZone = null; scope.Dispose(); }

        private void Beside(Zone zone, Entity e)
        {
            here?.RemoveEntity(player); var p = zone.GetEntityPosition(e);
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue; var c = zone.GetCell(p.x + dx, p.y + dy);
                if (c != null && !c.BlocksMovement()) { Assert.IsTrue(zone.AddEntity(player, c.X, c.Y)); here = zone; SettlementRuntime.ActiveZone = zone; return; }
            }
            Assert.Fail("no free cell beside " + e.GetDisplayName());
        }
        private Entity Speaker(string who)
        {
            if (who == "Hollin") { var z = manager.GetZone(StillleafArchiveContent.QuillholdZoneId); var e = z.GetAllEntities().Single(x => x.ID == StillleafArchiveContent.SearcherId); Beside(z, e); return e; }
            if (who == "Halm") { var z = manager.GetZone(StillleafSaltVault.ZoneId); var e = z.GetAllEntities().Single(x => x.ID == StillleafSaltVault.IndexerId); Beside(z, e); return e; }
            var bp = who == "Tendril" ? "ChoirTendril" : "PalimpsestEcho"; var s = factory.CreateEntity(bp); Assert.IsNotNull(s, bp);
            var zone = new Zone("AfterTheEndingTestZone"); Assert.IsTrue(zone.AddEntity(s, 10, 10)); Assert.IsTrue(zone.AddEntity(player, 11, 10)); here = zone; return s;
        }
        private static int Choice(string target) { var cs = ConversationManager.VisibleChoices; for (int i = 0; i < cs.Count; i++) if (cs[i].Target == target) return i; return -1; }
        private static int CountAfter() => ConversationManager.VisibleChoices.Count(c => c.Target.StartsWith("After"));
        private void Ending(int path) => NarrativeStatePart.Current.SetFact(EndingSpine.EndingFact, path);
        private void Open(Entity speaker) { ConversationManager.EndConversation(); Assert.IsTrue(ConversationManager.StartConversation(speaker, player)); }

        // ════════════════ the offers close ════════════════

        [TestCase("Hollin", "Stone")] [TestCase("Halm", "MuteStone")]
        public void APreserversStone_IsOfferedBeforeAnyEnding_AndNeverAfter(string who, string node)
        {
            var s = Speaker(who); Open(s);
            Assert.GreaterOrEqual(Choice(node), 0, "offered while no ending is enacted");
            for (int path = 1; path <= 4; path++) { Ending(path); Open(s); Assert.AreEqual(-1, Choice(node), "closed after ending " + path); }
        }

        [Test]
        public void TheChoirsThread_IsOfferedBeforeAnyEnding_AndNeverAfter()
        {
            var t = Speaker("Tendril");
            void ToFirstRoot() { Open(t); foreach (var n in new[] { "WhatAreYou", "FirstRoot", "FindFirstRoot" }) Assert.IsTrue(ConversationManager.SelectChoice(Choice(n)), n); }
            ToFirstRoot(); Assert.GreaterOrEqual(Choice("Thread"), 0);
            for (int path = 1; path <= 4; path++) { Ending(path); ToFirstRoot(); Assert.AreEqual(-1, Choice("Thread"), "closed after ending " + path); }
        }

        // ════════════════ the voices after ════════════════

        [TestCase("Hollin")] [TestCase("Halm")] [TestCase("Tendril")] [TestCase("Echo")]
        public void EachVoice_SaysNothingOfAnEnding_BeforeOne(string who)
        {
            Open(Speaker(who)); Assert.AreEqual(0, CountAfter());
        }

        [TestCase("Hollin")] [TestCase("Halm")] [TestCase("Tendril")] [TestCase("Echo")]
        public void EachVoice_SpeaksToTheEndingEnacted_ExactlyOneLinePerEnding(string who)
        {
            var s = Speaker(who); var seen = new System.Collections.Generic.HashSet<string>();
            for (int path = 1; path <= 4; path++)
            {
                Ending(path); Open(s);
                Assert.AreEqual(1, CountAfter(), "one 'what has changed' per ending");
                int i = Choice("After" + path); Assert.GreaterOrEqual(i, 0);
                Assert.IsTrue(ConversationManager.SelectChoice(i)); Assert.AreEqual("After" + path, ConversationManager.CurrentNode.ID);
                string text = ConversationManager.CurrentNode.Text; Assert.IsFalse(string.IsNullOrWhiteSpace(text));
                Assert.IsTrue(seen.Add(text), "each ending has its own line");
                var lower = text.ToLowerInvariant(); StringAssert.DoesNotContain(" best", lower); StringAssert.DoesNotContain("naro", lower, "no voice explains the seventh's refusal");
                if (who == "Tendril") { StringAssert.DoesNotContain("dead", lower); StringAssert.DoesNotContain(" died", lower); }
                Assert.GreaterOrEqual(Choice("Start"), 0, "the talk can go back"); Assert.GreaterOrEqual(Choice("End"), 0, "and end");
            }
        }

        [Test]
        public void AnUnknownEndingValue_OpensNoAfterLine()
        {
            // Counter-check on the gate: only the four enacted values open a line.
            var s = Speaker("Echo"); Ending(7); Open(s); Assert.AreEqual(0, CountAfter());
        }
    }
}
