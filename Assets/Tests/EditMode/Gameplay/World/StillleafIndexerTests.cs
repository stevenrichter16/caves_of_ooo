using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The Stillleaf Archive, SA.3 (Docs/MIDGAME-STILLLEAF-ARCHIVE.md): at the
    /// Salt-Vault a Pale Curation Indexer can find the keeper's file only by
    /// the keeper's last words; the key is filed beside the keeper in a
    /// locked salt file. Curation's price is a request, not a toll: agree or
    /// bargain and the file is released for the player to take the key by
    /// the ordinary container path; refuse and leave without it, unpunished.
    /// </summary>
    public sealed class StillleafIndexerTests
    {
        private HotbarSaveFixture scope;
        private EntityFactory factory;
        private OverworldZoneManager manager;
        private Zone salt;
        private Entity player, indexer, cabinet;

        [SetUp] public void Setup()
        {
            scope = new HotbarSaveFixture(false, false);
            factory = GrovelandsCompositionTests.Factory();
            manager = OverworldZoneManager.CreateDetached(factory, 64);
            salt = manager.GetZone(StillleafSaltVault.ZoneId);
            indexer = salt.GetAllEntities().SingleOrDefault(e => e.ID == StillleafSaltVault.IndexerId);
            cabinet = StillleafSaltVault.FindCabinet(salt);
            Assert.IsNotNull(indexer, "the Indexer is installed on fresh generation");
            Assert.IsNotNull(cabinet, "the salt file is installed on fresh generation");
            player = factory.CreateEntity("Player");
            var seat = Beside(salt, indexer);
            Assert.IsTrue(salt.AddEntity(player, seat.X, seat.Y));
            StoryletPart.Current = new StoryletPart(); StoryletPart.LocalPlayer = player;
            SettlementRuntime.ActiveZone = salt;
            StillleafArchiveContent.EnsureRegistered();
            MessageLog.Clear();
        }
        [TearDown] public void Teardown() { ConversationManager.EndConversation(); scope.Dispose(); }

        private static Cell Beside(Zone z, Entity e)
        {
            var p = z.GetEntityPosition(e);
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                var c = z.GetCell(p.x + dx, p.y + dy);
                if (c != null && !c.BlocksMovement()) return c;
            }
            Assert.Fail("no free cell beside the Indexer"); return null;
        }
        private Entity Key => cabinet.GetPart<ContainerPart>().Contents.SingleOrDefault(e => e.ID == StillleafSaltVault.KeyId);
        private bool Locked => cabinet.GetPart<ContainerPart>().Locked;
        private void KnowTheWords()
        {
            player.SetIntProperty(StillleafArchiveContent.WordsKnown, 1);
            StoryletPart.Current.StartQuest(new QuestState { QuestId = StillleafArchiveContent.QuestId, CurrentStageIndex = 0 });
        }
        private int Stage => StoryletPart.Current.GetActiveQuests().Single(q => q.QuestId == StillleafArchiveContent.QuestId).CurrentStageIndex;
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
        private bool TakeKey()
        {
            var key = Key; if (key == null) return false;
            return InventorySystem.ExecuteCommand(new TakeFromContainerCommand(cabinet, key), player, salt).Success;
        }
        private static int Count(string category, string kind) =>
            DiagQuery.Apply(new DiagQuery.Filter { Category = category, Kind = kind, Limit = 20 }).Records.Count;

        // ════════════════════════════════════════════════════════════
        //   The residents
        // ════════════════════════════════════════════════════════════

        [TestCase(64)] [TestCase(1729)]
        public void TheSaltVaultHoldsOneIndexer_AndOneLockedFileBesideThem_HoldingTheKeepersKey(int seed)
        {
            var m = OverworldZoneManager.CreateDetached(factory, seed);
            var z = m.GetZone(StillleafSaltVault.ZoneId);
            Assert.AreEqual("PaleCuration", m.WorldMap.GetPOI(15, 15)?.Faction, "the Salt-Vault is a Curation place");
            var idx = z.GetAllEntities().Single(e => e.BlueprintName == StillleafSaltVault.IndexerBlueprint);
            var file = z.GetAllEntities().Single(e => e.BlueprintName == StillleafSaltVault.CabinetBlueprint);
            Assert.AreEqual(StillleafSaltVault.IndexerId, idx.ID); Assert.AreEqual(StillleafSaltVault.CabinetId, file.ID);
            Assert.AreEqual("PaleCuration", idx.GetTag("Faction"));
            Assert.AreEqual(StillleafSaltVault.ConversationId, idx.GetPart<ConversationPart>()?.ConversationID);
            var ip = z.GetEntityPosition(idx); var fp = z.GetEntityPosition(file);
            Assert.IsTrue(z.GetCell(ip.x, ip.y).Objects.Any(o => o.BlueprintName == "StoneFloor"), "the Indexer works indoors");
            Assert.IsTrue(z.GetCell(fp.x, fp.y).Objects.Any(o => o.BlueprintName == "StoneFloor"), "the file stands indoors");
            Assert.AreEqual(1, System.Math.Abs(ip.x - fp.x) + System.Math.Abs(ip.y - fp.y), "the file is beside the desk");
            var container = file.GetPart<ContainerPart>();
            Assert.IsTrue(container.Locked); Assert.IsTrue(file.GetPart<PhysicsPart>().Solid); Assert.IsFalse(file.GetPart<PhysicsPart>().Takeable);
            Assert.AreEqual(1, container.Contents.Count, "one key, filed");
            var key = container.Contents[0];
            Assert.AreEqual(StillleafArchive.KeyBlueprint, key.BlueprintName);
            Assert.AreEqual(SealedLibraryBuilder.KeyID, key.GetPart<KeyPart>().KeyId, "it is the vault's own key");
            Assert.AreEqual("key", key.GetPart<CompleteObjectiveOnTaken>()?.Objective, "taking it advances the journal");
            // Counter-checks: exactly one of each; other villages get none.
            Assert.AreEqual(0, z.GetAllEntities().Count(e => e.GetPart<KeyPart>()?.KeyId == SealedLibraryBuilder.KeyID), "no keeper's key lies loose; the one that exists is filed");
            foreach (var other in new[] { "Overworld.10.10.0", "Overworld.8.16.0", "Overworld.12.12.0" })
                Assert.IsFalse(m.GetZone(other).GetAllEntities().Any(e => e.BlueprintName == StillleafSaltVault.IndexerBlueprint || e.BlueprintName == StillleafSaltVault.CabinetBlueprint), other);
        }

        [Test]
        public void TheFileDoesNotWallAnyoneIn()
        {
            // Every passable cell around the file is still reachable from the
            // zone edge with the file standing (creatures ignored).
            var fp = salt.GetEntityPosition(cabinet);
            var seen = new bool[Zone.Width, Zone.Height]; var q = new System.Collections.Generic.Queue<(int, int)>();
            bool Open(int x, int y) { var c = salt.GetCell(x, y); return c != null && !(x == fp.x && y == fp.y) && c.Objects.All(o => o.HasTag("Creature") || o.GetPart<PhysicsPart>()?.Solid != true); }
            for (int x = 0; x < Zone.Width; x++) foreach (int y in new[] { 0, Zone.Height - 1 }) if (Open(x, y) && !seen[x, y]) { seen[x, y] = true; q.Enqueue((x, y)); }
            for (int y = 0; y < Zone.Height; y++) foreach (int x in new[] { 0, Zone.Width - 1 }) if (Open(x, y) && !seen[x, y]) { seen[x, y] = true; q.Enqueue((x, y)); }
            while (q.Count > 0)
            {
                var (x, y) = q.Dequeue();
                foreach (var (dx, dy) in new[] { (0, -1), (-1, 0), (1, 0), (0, 1) })
                { int nx = x + dx, ny = y + dy; if (nx < 0 || ny < 0 || nx >= Zone.Width || ny >= Zone.Height || seen[nx, ny] || !Open(nx, ny)) continue; seen[nx, ny] = true; q.Enqueue((nx, ny)); }
            }
            foreach (var (dx, dy) in new[] { (0, -1), (-1, 0), (1, 0), (0, 1) })
                if (Open(fp.x + dx, fp.y + dy)) Assert.IsTrue(seen[fp.x + dx, fp.y + dy], $"cell beside the file at {fp.x + dx},{fp.y + dy} is reachable");
            var ip = salt.GetEntityPosition(indexer); Assert.IsTrue(seen[ip.x, ip.y], "the Indexer's desk is reachable");
        }

        [Test]
        public void TheIndexerIsRealNamedArt_AndThisPlaceNeedsNoVoxelBody()
        {
            var row = EnvironmentSpriteRenderer.CreatureSprites.SingleOrDefault(r => r.Blueprint == StillleafSaltVault.IndexerBlueprint);
            Assert.AreEqual('@', row.Glyph); Assert.AreEqual("stillleaf_indexer", row.File);
            var sprite = Resources.Load<Sprite>("Sprites/Environment/stillleaf_indexer");
            Assert.IsNotNull(sprite); Assert.AreEqual(new Rect(3, 1, 10, 13), sprite.rect); Assert.AreEqual("stillleaf_indexer_0", sprite.name);
            Assert.AreEqual("@", indexer.GetPart<RenderPart>().RenderString);
            Assert.IsFalse(VoxelWorldPresentation.IsSupported(StillleafSaltVault.ZoneId), "generic villages keep the native presentation");
        }

        [Test]
        public void InstallIsIdempotent_AndLostResidentsAreNotReplaced()
        {
            Assert.IsFalse(StillleafSaltVault.TryInstall(salt, factory));
            Assert.IsTrue(salt.RemoveEntity(indexer)); Assert.IsTrue(salt.RemoveEntity(cabinet));
            Assert.IsFalse(StillleafSaltVault.TryInstall(salt, factory), "the latch outlives them");
            Assert.IsNull(StillleafSaltVault.FindCabinet(salt));
        }

        // ════════════════════════════════════════════════════════════
        //   The index answers only to the words
        // ════════════════════════════════════════════════════════════

        [Test]
        public void WithoutTheWords_TheIndexCannotBeAsked_AndTheFileStaysShut()
        {
            Diag.ResetAll();
            Assert.IsTrue(ConversationManager.StartConversation(indexer, player));
            Assert.GreaterOrEqual(Choice(target: "Index"), 0, "the index is explained");
            Assert.AreEqual(-1, Choice(actionValue: "retrieve"), "the words are not offered by the player who lacks them");
            Assert.IsFalse(StillleafSaltVault.CanConversation(indexer, player, "retrieve"));
            Assert.IsFalse(StillleafArchiveContent.TryConversation(indexer, player, "retrieve"));
            Assert.AreEqual(1, Count("quest", "StillleafArchiveRejected"));
            Assert.IsTrue(Locked); Assert.IsFalse(TakeKey(), "a locked file gives nothing");
            Assert.IsNotNull(Key);
        }

        [Test]
        public void WithTheWords_TheRealChoiceOpensTheFile_AndTheJournalAdvancesToTheKey()
        {
            KnowTheWords(); Diag.ResetAll();
            Assert.IsTrue(ConversationManager.StartConversation(indexer, player));
            int retrieve = Choice(actionValue: "retrieve"); Assert.GreaterOrEqual(retrieve, 0);
            Assert.IsTrue(ConversationManager.SelectChoice(retrieve));
            Assert.AreEqual("File", ConversationManager.CurrentNode?.ID, "the file is read");
            Assert.AreEqual(1, player.GetIntProperty(StillleafSaltVault.FileFound));
            string log = string.Join("\n", MessageLog.GetRecent(12));
            StringAssert.Contains(StillleafArchiveContent.LastWords, log); StringAssert.Contains("Status: continuing", log);
            Assert.AreEqual(1, Stage, "stage 'key'");
            Assert.IsTrue(Locked, "reading the file releases nothing by itself");
            Assert.AreEqual(1, Count("quest", "StillleafArchiveApplied"));
            ConversationManager.EndConversation();
            Assert.IsFalse(StillleafSaltVault.CanConversation(indexer, player, "retrieve"), "read once");
        }

        // ════════════════════════════════════════════════════════════
        //   Curation's request
        // ════════════════════════════════════════════════════════════

        [TestCase("agree", StillleafSaltVault.TermsAgreed)]
        [TestCase("bargain", StillleafSaltVault.TermsBargained)]
        public void AgreeOrBargain_ReleasesTheFile_AndTakingTheKeyAdvancesToTheDescent(string command, int terms)
        {
            KnowTheWords();
            Assert.IsTrue(StillleafArchiveContent.TryConversation(indexer, player, "retrieve"));
            Assert.IsTrue(ConversationManager.StartConversation(indexer, player));
            Assert.IsTrue(ConversationManager.SelectChoice(Choice(target: "Request")));
            int pick = Choice(actionValue: command); Assert.GreaterOrEqual(pick, 0);
            ConversationManager.SelectChoice(pick);
            Assert.AreEqual(terms, player.GetIntProperty(StillleafSaltVault.Terms));
            Assert.IsFalse(Locked, "released");
            var key = Key; Assert.IsNotNull(key);
            Assert.IsTrue(TakeKey(), "the ordinary container path hands over the key");
            Assert.IsTrue(player.GetPart<InventoryPart>().Objects.Contains(key));
            Assert.IsNull(Key, "not duplicated");
            Assert.AreEqual(2, Stage, "stage 'descend'");
            // Terms are one-time once the file is released.
            foreach (var c in new[] { "agree", "bargain", "refuse" })
                Assert.IsFalse(StillleafSaltVault.CanConversation(indexer, player, c), c);
        }

        [Test]
        public void Refuse_LeavesWithoutTheKey_UnpunishedAndUnfailed_AndTheOfferStandsOnReturn()
        {
            KnowTheWords();
            Assert.IsTrue(StillleafArchiveContent.TryConversation(indexer, player, "retrieve"));
            Assert.IsTrue(ConversationManager.StartConversation(indexer, player));
            Assert.IsTrue(ConversationManager.SelectChoice(Choice(target: "Request")));
            ConversationManager.SelectChoice(Choice(actionValue: "refuse"));
            Assert.AreEqual(StillleafSaltVault.TermsRefused, player.GetIntProperty(StillleafSaltVault.Terms));
            Assert.IsTrue(Locked); Assert.IsFalse(TakeKey());
            Assert.IsTrue(StoryletPart.Current.IsQuestActive(StillleafArchiveContent.QuestId));
            Assert.IsFalse(StoryletPart.Current.IsQuestFailed(StillleafArchiveContent.QuestId), "refusal is closure, not failure");
            Assert.AreEqual(1, Stage, "still at 'key'");
            // On return the request is still open; a refusal is not repeated as a verb.
            Assert.IsTrue(StillleafSaltVault.CanConversation(indexer, player, "agree"));
            Assert.IsTrue(StillleafSaltVault.CanConversation(indexer, player, "bargain"));
            Assert.IsFalse(StillleafSaltVault.CanConversation(indexer, player, "refuse"));
            Assert.IsTrue(StillleafArchiveContent.TryConversation(indexer, player, "agree"));
            Assert.IsFalse(Locked);
        }

        [Test]
        public void TheRequestNeedsTheFileReadFirst()
        {
            KnowTheWords();
            foreach (var c in new[] { "agree", "bargain", "refuse" })
                Assert.IsFalse(StillleafSaltVault.CanConversation(indexer, player, c), c + " before the file is read");
        }

        [Test]
        public void TheFileCanBeBrokenOpen_ButBreakingIsNotARelease()
        {
            // Out-of-order path kept honest: the key spills where the file
            // stood, no terms are recorded, and there is nothing left for the
            // Indexer to release. Standing consequences belong to SA.4/SA.5.
            KnowTheWords();
            Assert.IsTrue(StillleafArchiveContent.TryConversation(indexer, player, "retrieve"));
            var fp = salt.GetEntityPosition(cabinet); var key = Key;
            Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Destroy(cabinet, player, salt, "test"));
            Assert.IsFalse(salt.GetAllEntities().Contains(cabinet));
            Assert.IsTrue(salt.GetCell(fp.x, fp.y).Objects.Contains(key), "the key spills onto the floor");
            Assert.AreEqual(0, player.GetIntProperty(StillleafSaltVault.Terms));
            foreach (var c in new[] { "agree", "bargain", "refuse" })
                Assert.IsFalse(StillleafSaltVault.CanConversation(indexer, player, c), c);
        }

        // ════════════════════════════════════════════════════════════
        //   Gates, canon, observability
        // ════════════════════════════════════════════════════════════

        [Test]
        public void WrongSpeaker_RemotePlayer_OrDeadIndexer_CannotChangeProgress()
        {
            KnowTheWords(); Diag.ResetAll();
            var scribe = salt.GetAllEntities().First(e => e.BlueprintName == "Scribe");
            Assert.IsFalse(StillleafArchiveContent.TryConversation(scribe, player, "retrieve"));
            var sp = salt.GetEntityPosition(indexer); Cell far = null;
            for (int y = 0; y < Zone.Height && far == null; y++) for (int x = 0; x < Zone.Width; x++)
            { var c = salt.GetCell(x, y); if (!c.BlocksMovement() && System.Math.Max(System.Math.Abs(x - sp.x), System.Math.Abs(y - sp.y)) >= 4) { far = c; break; } }
            Assert.IsTrue(salt.MoveEntity(player, far.X, far.Y));
            Assert.IsFalse(StillleafArchiveContent.TryConversation(indexer, player, "retrieve"));
            Assert.AreEqual(0, player.GetIntProperty(StillleafSaltVault.FileFound));
            Assert.AreEqual(2, Count("quest", "StillleafArchiveRejected")); Assert.AreEqual(0, Count("quest", "StillleafArchiveApplied"));
            salt.MoveEntity(player, Beside(salt, indexer).X, Beside(salt, indexer).Y);
            indexer.SetStatValue("Hitpoints", 0);
            Assert.IsFalse(StillleafSaltVault.CanConversation(indexer, player, "retrieve"));
        }

        [Test]
        public void TheSearcherDoesNotAnswerForTheIndex_AndTheIndexerNotForTheSearchers()
        {
            // Cross-site counter-check on the shared action name.
            KnowTheWords();
            Assert.IsFalse(StillleafArchiveContent.CanConversation(indexer, player, "accept"));
            Assert.IsFalse(StillleafArchiveContent.CanConversation(indexer, player, "release"));
        }

        [Test]
        public void TheIndexerSpeaksInCurationsRegister_AndNamesNoMystery()
        {
            var conv = ConversationLoader.Get(StillleafSaltVault.ConversationId); Assert.NotNull(conv);
            string all = string.Join("\n", conv.Nodes.Select(n => n.Text));
            StringAssert.Contains("last words", all); StringAssert.Contains("Status: continuing", all);
            StringAssert.Contains("filed, not read", all); StringAssert.Contains("Not a toll", all);
            Assert.IsFalse(all.Contains("Naro")); Assert.IsFalse(all.Contains("Urqu")); Assert.IsFalse(all.Contains("Salted"), "the Salted is late-game canon");
            Assert.IsTrue(conv.Nodes.All(n => n.Choices.Any(c => c.Target == "End" || c.Target == "Start")));
        }

        [Test]
        public void FreshGenerationEmitsOnePlacedRecord()
        {
            Diag.ResetAll();
            OverworldZoneManager.CreateDetached(factory, 913).GetZone(StillleafSaltVault.ZoneId);
            Assert.AreEqual(1, Count("worldgen", "StillleafIndexerPlaced"));
            Assert.AreEqual(0, Count("worldgen", "StillleafIndexerRefused"));
        }
    }
}
