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
    /// The Stillleaf Archive, SA.2 (Docs/MIDGAME-STILLLEAF-ARCHIVE.md): a
    /// Recension Searcher at Quillhold's stacks hires the player as an
    /// outsider and gives the one thing that can find the keeper's file at
    /// the Salt-Vault: the keeper's last words. Every pin here goes through
    /// the real resident, the real conversation and the real quest journal.
    /// </summary>
    public sealed class StillleafSearcherTests
    {
        private HotbarSaveFixture scope;
        private EntityFactory factory;
        private OverworldZoneManager manager;
        private Zone quillhold;
        private Entity player, searcher;

        [SetUp] public void Setup()
        {
            scope = new HotbarSaveFixture(false, false);
            factory = GrovelandsCompositionTests.Factory();
            manager = OverworldZoneManager.CreateDetached(factory, 64);
            quillhold = manager.GetZone(StillleafArchiveContent.QuillholdZoneId);
            searcher = Searcher(quillhold);
            Assert.IsNotNull(searcher, "the Searcher is installed on fresh generation");
            player = factory.CreateEntity("Player");
            var seat = Beside(quillhold, searcher);
            Assert.IsTrue(quillhold.AddEntity(player, seat.X, seat.Y), "player stands beside the Searcher");
            StoryletPart.Current = new StoryletPart(); StoryletPart.LocalPlayer = player;
            SettlementRuntime.ActiveZone = quillhold;
            StillleafArchiveContent.EnsureRegistered();
            MessageLog.Clear();
        }
        [TearDown] public void Teardown() { ConversationManager.EndConversation(); scope.Dispose(); }

        private static Entity Searcher(Zone z) => z.GetAllEntities().SingleOrDefault(e => e.ID == StillleafArchiveContent.SearcherId);

        private static Cell Beside(Zone z, Entity e)
        {
            var p = z.GetEntityPosition(e);
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                var c = z.GetCell(p.x + dx, p.y + dy);
                if (c != null && !c.BlocksMovement()) return c;
            }
            Assert.Fail("no free cell beside the Searcher"); return null;
        }

        private int Choice(string target = null, string actionValue = null)
        {
            var choices = ConversationManager.VisibleChoices;
            for (int i = 0; i < choices.Count; i++)
            {
                var c = choices[i];
                if (target != null && c.Target != target) continue;
                if (actionValue != null && !c.Actions.Any(a => a.Key == "StillleafArchive" && a.Value == actionValue)) continue;
                return i;
            }
            return -1;
        }

        private static int Count(string category, string kind) =>
            DiagQuery.Apply(new DiagQuery.Filter { Category = category, Kind = kind, Limit = 20 }).Records.Count;

        // ════════════════════════════════════════════════════════════
        //   The resident
        // ════════════════════════════════════════════════════════════

        [TestCase(64)] [TestCase(1729)]
        public void QuillholdHoldsExactlyOneSearcher_InsideTheArchive_AndOtherVillagesNone(int seed)
        {
            var m = OverworldZoneManager.CreateDetached(factory, seed);
            var z = m.GetZone(StillleafArchiveContent.QuillholdZoneId);
            var all = z.GetAllEntities().Where(e => e.BlueprintName == StillleafArchiveContent.SearcherBlueprint).ToList();
            Assert.AreEqual(1, all.Count, "one Searcher, by blueprint");
            var s = all[0]; var p = z.GetEntityPosition(s);
            Assert.AreEqual(StillleafArchiveContent.SearcherId, s.ID);
            Assert.IsTrue(z.GetCell(p.x, p.y).IsInterior, "she works inside the archive, not in the yard");
            Assert.Greater(s.GetStatValue("Hitpoints"), 0);
            Assert.AreEqual("Palimpsest", s.GetTag("Faction"), "code faction id for the Recension");
            Assert.AreEqual(StillleafArchiveContent.ConversationId, s.GetPart<ConversationPart>()?.ConversationID);
            Assert.AreEqual(StillleafArchiveContent.QuestId, s.GetPart<QuestBeaconPart>()?.Quest);
            // Counter-checks: the generic scribe is untouched; other villages get no Searcher.
            Assert.AreEqual(1, z.GetAllEntities().Count(e => e.BlueprintName == "Scribe"));
            Assert.AreEqual("Scribe_1", z.GetAllEntities().Single(e => e.BlueprintName == "Scribe").GetPart<ConversationPart>()?.ConversationID);
            Assert.IsFalse(m.GetZone("Overworld.13.7.0").GetAllEntities().Any(e => e.BlueprintName == StillleafArchiveContent.SearcherBlueprint), "Tine");
            Assert.IsFalse(m.GetZone("Overworld.10.10.0").GetAllEntities().Any(e => e.BlueprintName == StillleafArchiveContent.SearcherBlueprint), "Sill");
        }

        [Test]
        public void TheSearcherIsRealNamedArt_InBothPresentations()
        {
            // 2D: a named 16x16 sprite row with the '@' canonical glyph.
            var row = EnvironmentSpriteRenderer.CreatureSprites.SingleOrDefault(r => r.Blueprint == StillleafArchiveContent.SearcherBlueprint);
            Assert.AreEqual('@', row.Glyph); Assert.AreEqual("stillleaf_searcher", row.File);
            var sprite = Resources.Load<Sprite>("Sprites/Environment/stillleaf_searcher");
            Assert.IsNotNull(sprite, "the sprite asset ships");
            Assert.AreEqual(new Vector2(16, 16), new Vector2(sprite.texture.width, sprite.texture.height), "a real 16x16 asset");
            Assert.AreEqual(new Rect(3, 1, 10, 13), sprite.rect, "sliced to the named-resident silhouette, like her Curation and Concord counterparts");
            Assert.AreEqual(FilterMode.Point, sprite.texture.filterMode); Assert.AreEqual("stillleaf_searcher_0", sprite.name, "her own name on the slice, not a copied one");
            Assert.AreEqual("@", searcher.GetPart<RenderPart>().RenderString, "the shipped glyph is the canonical one, so the reskin guard passes");
            // 3D: she keeps the Quillhold scribe body and moves as an actor.
            var catalog = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
            var recipe = SpawnRing3DRecipes.Resolve(quillhold, searcher, catalog);
            Assert.NotNull(recipe.ModelId, recipe.Failure); StringAssert.StartsWith("quillhold-scribe-", recipe.ModelId);
            Assert.IsTrue(recipe.Transient); Assert.IsFalse(recipe.Batched);
        }

        [Test]
        public void InstallIsIdempotent_AndALostSearcherIsNotReplaced()
        {
            Assert.IsFalse(StillleafArchiveContent.TryInstallSearcher(quillhold, factory), "a second install refuses");
            Assert.AreEqual(1, quillhold.GetAllEntities().Count(e => e.BlueprintName == StillleafArchiveContent.SearcherBlueprint));
            Assert.IsTrue(quillhold.RemoveEntity(searcher));
            Assert.IsFalse(StillleafArchiveContent.TryInstallSearcher(quillhold, factory), "the latch outlives the person");
            Assert.IsNull(Searcher(quillhold));
        }

        // ════════════════════════════════════════════════════════════
        //   The conversation (the real path)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheActualConversationOffersTheContract_AndDeclineLeavesItOnTheTable()
        {
            Assert.IsTrue(ConversationManager.StartConversation(searcher, player));
            int contract = Choice(target: "Contract"); Assert.GreaterOrEqual(contract, 0, "the contract is offered");
            Assert.IsTrue(ConversationManager.SelectChoice(contract));
            int decline = Choice(target: "Start"); Assert.GreaterOrEqual(decline, 0, "'not now' is a real choice");
            Assert.IsTrue(ConversationManager.SelectChoice(decline));
            Assert.IsFalse(StoryletPart.Current.IsQuestActive(StillleafArchiveContent.QuestId), "declining starts nothing");
            Assert.AreEqual(0, player.GetIntProperty(StillleafArchiveContent.WordsKnown), "and gives nothing away");
            Assert.GreaterOrEqual(Choice(target: "Contract"), 0, "the offer repeats");
            ConversationManager.EndConversation();
            Assert.IsTrue(ConversationManager.StartConversation(searcher, player));
            Assert.GreaterOrEqual(Choice(target: "Contract"), 0, "and repeats on a later visit");
        }

        [Test]
        public void AcceptingThroughTheRealChoice_StartsTheQuest_GivesTheWords_AndNavigableDirections()
        {
            Diag.ResetAll();
            Assert.IsTrue(ConversationManager.StartConversation(searcher, player));
            Assert.IsTrue(ConversationManager.SelectChoice(Choice(target: "Contract")));
            int accept = Choice(actionValue: "accept"); Assert.GreaterOrEqual(accept, 0);
            ConversationManager.SelectChoice(accept);
            Assert.IsTrue(StoryletPart.Current.IsQuestActive(StillleafArchiveContent.QuestId));
            Assert.AreEqual(1, player.GetIntProperty(StillleafArchiveContent.WordsKnown), "the player now holds the index key");
            string log = string.Join("\n", MessageLog.GetRecent(12));
            StringAssert.Contains(StillleafArchiveContent.LastWords, log, "the words themselves are said aloud");
            StringAssert.Contains("Salt-Vault", log);
            var q = StoryletRegistry.FindQuest(StillleafArchiveContent.QuestId); Assert.NotNull(q, "the journal entry exists");
            string note = q.Stages[0].Objectives[0].Text;
            StringAssert.Contains("Salt-Vault", note); StringAssert.Contains("south", note); StringAssert.Contains("east", note);
            StringAssert.Contains(StillleafArchiveContent.LastWords, note, "the Field Note keeps the words for the player");
            Assert.IsFalse(StillleafArchiveContent.CanConversation(searcher, player, "accept"), "not offered twice while active");
            Assert.AreEqual(1, Count("quest", "StillleafArchiveApplied"));
            Assert.AreEqual(0, Count("quest", "StillleafArchiveRejected"));
        }

        [Test]
        public void ReleaseIsClosureNotFailure_KeepsTheWords_AndTheDoorStaysOpen()
        {
            Assert.IsTrue(StillleafArchiveContent.TryConversation(searcher, player, "accept"));
            Assert.IsTrue(StillleafArchiveContent.CanConversation(searcher, player, "release"));
            Assert.IsTrue(StillleafArchiveContent.TryConversation(searcher, player, "release"));
            Assert.IsFalse(StoryletPart.Current.IsQuestActive(StillleafArchiveContent.QuestId));
            Assert.IsFalse(StoryletPart.Current.IsQuestCompleted(StillleafArchiveContent.QuestId), "released is not completed");
            Assert.IsFalse(StoryletPart.Current.IsQuestFailed(StillleafArchiveContent.QuestId), "and not failed: refusal is closure");
            Assert.AreEqual(ClosureState.Refused, StoryletPart.Current.GetClosure(StillleafArchiveContent.QuestId)?.State, "ES.1: a release is a spoken no on the ledger");
            Assert.IsTrue(StoryletPart.Current.ReadLedger().Clean);
            Assert.AreEqual(1, player.GetIntProperty(StillleafArchiveContent.WordsKnown), "what was heard is not unheard");
            Assert.IsFalse(StillleafArchiveContent.CanConversation(searcher, player, "release"), "nothing left to release");
            Assert.IsTrue(StillleafArchiveContent.TryConversation(searcher, player, "accept"), "and the work can be taken up again");
        }

        [Test]
        public void WrongSpeaker_RemotePlayer_OrDeadSearcher_CannotChangeProgress()
        {
            Diag.ResetAll();
            var scribe = quillhold.GetAllEntities().Single(e => e.BlueprintName == "Scribe");
            Assert.IsFalse(StillleafArchiveContent.TryConversation(scribe, player, "accept"), "the generic scribe does not speak for the Searchers");
            var sp = quillhold.GetEntityPosition(searcher); Cell far = null;
            for (int y = 0; y < Zone.Height && far == null; y++) for (int x = 0; x < Zone.Width; x++)
            { var c = quillhold.GetCell(x, y); if (!c.BlocksMovement() && System.Math.Max(System.Math.Abs(x - sp.x), System.Math.Abs(y - sp.y)) >= 4) { far = c; break; } }
            Assert.IsTrue(quillhold.MoveEntity(player, far.X, far.Y));
            Assert.IsFalse(StillleafArchiveContent.TryConversation(searcher, player, "accept"), "not across the room");
            Assert.AreEqual(0, StoryletPart.Current.GetActiveQuests().Count);
            Assert.AreEqual(0, player.GetIntProperty(StillleafArchiveContent.WordsKnown));
            Assert.AreEqual(2, Count("quest", "StillleafArchiveRejected"));
            Assert.AreEqual(0, Count("quest", "StillleafArchiveApplied"));
        }

        [Test]
        public void ADeadSearcherOffersNothing_ButAnAcceptedErrandSurvivesHer()
        {
            Assert.IsTrue(StillleafArchiveContent.TryConversation(searcher, player, "accept"));
            searcher.SetStatValue("Hitpoints", 0);
            Assert.IsFalse(StillleafArchiveContent.CanConversation(searcher, player, "release"));
            Assert.IsTrue(StoryletPart.Current.IsQuestActive(StillleafArchiveContent.QuestId), "the journal and the words remain");
            Assert.AreEqual(1, player.GetIntProperty(StillleafArchiveContent.WordsKnown));
        }

        // ════════════════════════════════════════════════════════════
        //   Canon and observability
        // ════════════════════════════════════════════════════════════

        [Test]
        public void SheSpeaksAsASearcher_NeverForTheMainline_AndNeverNamesTheLedgerMysteries()
        {
            var conv = ConversationLoader.Get(StillleafArchiveContent.ConversationId); Assert.NotNull(conv);
            string all = string.Join("\n", conv.Nodes.Select(n => n.Text));
            StringAssert.Contains("Searcher", all); StringAssert.Contains("Recension", all);
            StringAssert.Contains("Salt-Vault", all); StringAssert.Contains("keeper", all);
            Assert.IsFalse(all.Contains("Naro"), "the register resolves no Mystery Ledger entry");
            Assert.IsFalse(all.Contains("Urqu"), "Urqu is pressure, not a character here");
            Assert.IsTrue(conv.Nodes.All(n => n.Choices.Any(c => c.Target == "End" || c.Target == "Start")), "every node can be left");
        }

        [Test]
        public void FreshGenerationEmitsOnePlacedRecord()
        {
            Diag.ResetAll();
            OverworldZoneManager.CreateDetached(factory, 913).GetZone(StillleafArchiveContent.QuillholdZoneId);
            Assert.AreEqual(1, Count("worldgen", "StillleafSearcherPlaced"));
            Assert.AreEqual(0, Count("worldgen", "StillleafSearcherRefused"));
        }
    }
}
