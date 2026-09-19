using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The Stillleaf Archive, SA.4 (Docs/MIDGAME-STILLLEAF-ARCHIVE.md): custody
    /// and aftermath. Three testimonies, one enacted choice, each physical,
    /// persistent and one-time; standing follows what was promised; refusal
    /// (resealing) is closure and pays no one; both residents can be told the
    /// truth. The last test walks the whole chain through the real paths.
    /// </summary>
    public sealed class StillleafCustodyTests
    {
        private HotbarSaveFixture scope;
        private EntityFactory factory;
        private OverworldZoneManager manager;
        private Zone quillhold, salt, vault;
        private Entity player, searcher, indexer, cabinet, door, register;

        [SetUp] public void Setup()
        {
            scope = new HotbarSaveFixture(false, false);
            factory = GrovelandsCompositionTests.Factory();
            manager = OverworldZoneManager.CreateDetached(factory, 64);
            quillhold = manager.GetZone(StillleafArchiveContent.QuillholdZoneId);
            salt = manager.GetZone(StillleafSaltVault.ZoneId);
            vault = manager.GetZone(SealedLibraryBuilder.ZoneID);
            searcher = quillhold.GetAllEntities().Single(e => e.ID == StillleafArchiveContent.SearcherId);
            indexer = salt.GetAllEntities().Single(e => e.ID == StillleafSaltVault.IndexerId);
            cabinet = StillleafSaltVault.FindCabinet(salt);
            door = vault.GetAllEntities().Single(e => e.BlueprintName == "SealedLibraryDoor");
            register = vault.GetAllEntities().Single(e => e.ID == StillleafArchive.RegisterId);
            player = factory.CreateEntity("Player");
            StoryletPart.Current = new StoryletPart(); StoryletPart.LocalPlayer = player;
            StillleafArchiveContent.EnsureRegistered();
            PlayerReputation.Set(StillleafCustody.RecensionFaction, 0); PlayerReputation.Set(StillleafCustody.CurationFaction, 0);
            MessageLog.Clear(); Diag.ResetAll();
        }
        [TearDown] public void Teardown() { ConversationManager.EndConversation(); scope.Dispose(); }

        // ── helpers ──────────────────────────────────────────────────────
        private static Cell Beside(Zone z, Entity e)
        {
            var p = z.GetEntityPosition(e);
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
            { if (dx == 0 && dy == 0) continue; var c = z.GetCell(p.x + dx, p.y + dy); if (c != null && !c.BlocksMovement()) return c; }
            Assert.Fail("no free cell beside " + e.BlueprintName); return null;
        }
        private void Leave() { var z = SettlementRuntime.ActiveZone; if (z != null && z.GetEntityCell(player) != null) z.RemoveEntity(player); }
        private void Go(Zone z, Entity beside) { Leave(); var c = Beside(z, beside); Assert.IsTrue(z.AddEntity(player, c.X, c.Y)); SettlementRuntime.ActiveZone = z; }
        private void GoOutsideTheDoor() { Leave(); var d = vault.GetEntityPosition(door); Assert.IsTrue(vault.AddEntity(player, d.x - 1, d.y)); SettlementRuntime.ActiveZone = vault; }
        private void CarryRegister() { Assert.IsTrue(vault.RemoveEntity(register)); Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(register)); }
        private void CarryKey() { Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(factory.CreateEntity(StillleafArchive.KeyBlueprint))); }
        private void AtCustodyStage() => StoryletPart.Current.StartQuest(new QuestState { QuestId = StillleafArchiveContent.QuestId, CurrentStageIndex = 3 });
        private static int Rep(string f) => PlayerReputation.Get(f);
        private bool Reseal(out int cost) => StillleafCustody.TryWorldAction(door, player, vault, StillleafCustody.ResealCommand, out cost);
        private bool DoorOffersReseal()
        {
            var list = new InventoryActionList(); var ev = GameEvent.New("GetInventoryActions"); ev.SetParameter("Actions", (object)list);
            door.FireEventAndRelease(ev); return list.Actions.Any(a => a.Command == StillleafCustody.ResealCommand);
        }
        private int Choice(string actionValue)
        {
            var choices = ConversationManager.VisibleChoices;
            for (int i = 0; i < choices.Count; i++) if (choices[i].Actions.Any(a => a.Key == "StillleafArchive" && a.Value == actionValue)) return i;
            return -1;
        }
        private static int Count(string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = "quest", Kind = kind, Limit = 40 }).Records.Count;
        private static string LastRejectedPayload() => DiagQuery.Apply(new DiagQuery.Filter { Category = "quest", Kind = "StillleafArchiveRejected", Limit = 40 }).Records.Last().PayloadJson;
        private int Stage => StoryletPart.Current.GetActiveQuests().Single(q => q.QuestId == StillleafArchiveContent.QuestId).CurrentStageIndex;

        // ════════════════════════════════════════════════════════════
        //   Deliver to Quillhold
        // ════════════════════════════════════════════════════════════

        [Test]
        public void DeliverToQuillhold_SealsTheRegisterBesideHollin_PaysRecension_AndCompletesTheJournal()
        {
            AtCustodyStage(); Go(quillhold, searcher); CarryRegister();
            Assert.IsTrue(ConversationManager.StartConversation(searcher, player));
            int deliver = Choice("deliver"); Assert.GreaterOrEqual(deliver, 0, "the real choice is offered");
            ConversationManager.SelectChoice(deliver);
            Assert.AreEqual(StillleafCustody.OutcomeDelivered, player.GetIntProperty(StillleafCustody.Outcome));
            Assert.IsFalse(player.GetPart<InventoryPart>().Objects.Contains(register));
            Assert.IsTrue(quillhold.GetAllEntities().Contains(register), "physically in Quillhold");
            Assert.AreEqual(quillhold.GetEntityPosition(searcher), quillhold.GetEntityPosition(register), "at her desk");
            Assert.IsFalse(register.GetPart<PhysicsPart>().Takeable, "sealed: it cannot be carried off again");
            StringAssert.Contains("sealed", register.GetPart<RenderPart>().DisplayName);
            Assert.IsTrue(StoryletPart.Current.IsQuestCompleted(StillleafArchiveContent.QuestId));
            Assert.AreEqual(StillleafCustody.RecensionForTheRecord, Rep(StillleafCustody.RecensionFaction));
            Assert.AreEqual(0, Rep(StillleafCustody.CurationFaction), "nothing was promised to Curation");
            Assert.AreEqual(1, Count("StillleafArchiveApplied"));
        }

        [TestCase(StillleafSaltVault.TermsAgreed, StillleafCustody.CurationBrokenAgreement)]
        [TestCase(StillleafSaltVault.TermsBargained, StillleafCustody.CurationBrokenBargain)]
        [TestCase(StillleafSaltVault.TermsRefused, 0)]
        [TestCase(0, 0)]
        public void DeliveringBreaksExactlyWhatWasPromisedToCuration(int terms, int expected)
        {
            player.SetIntProperty(StillleafSaltVault.Terms, terms); Go(quillhold, searcher); CarryRegister();
            Assert.IsTrue(StillleafArchiveContent.TryConversation(searcher, player, "deliver"));
            Assert.AreEqual(expected, Rep(StillleafCustody.CurationFaction));
            Assert.AreEqual(StillleafCustody.RecensionForTheRecord, Rep(StillleafCustody.RecensionFaction));
            string log = string.Join("\n", MessageLog.GetRecent(12));
            Assert.AreEqual(expected < 0, log.Contains("Curation's file"), "the player is told, in words, when a promise is broken");
        }

        // ════════════════════════════════════════════════════════════
        //   File at the Salt-Vault
        // ════════════════════════════════════════════════════════════

        [Test]
        public void FileAtTheSaltVault_LocksTheRegisterBesideItsKeeper_PaysCuration_AndCompletesTheJournal()
        {
            AtCustodyStage(); Go(salt, indexer); CarryRegister();
            cabinet.GetPart<ContainerPart>().Locked = false; // released earlier; the key is elsewhere
            Assert.IsTrue(ConversationManager.StartConversation(indexer, player));
            int file = Choice("file"); Assert.GreaterOrEqual(file, 0);
            ConversationManager.SelectChoice(file);
            Assert.AreEqual(StillleafCustody.OutcomeFiled, player.GetIntProperty(StillleafCustody.Outcome));
            var container = cabinet.GetPart<ContainerPart>();
            Assert.IsTrue(container.Contents.Contains(register), "filed in the salt file");
            Assert.IsTrue(container.Locked, "and the drawer locks again");
            Assert.IsFalse(player.GetPart<InventoryPart>().Objects.Contains(register));
            Assert.IsTrue(StoryletPart.Current.IsQuestCompleted(StillleafArchiveContent.QuestId));
            Assert.AreEqual(StillleafCustody.CurationForTheRecord, Rep(StillleafCustody.CurationFaction));
            Assert.AreEqual(0, Rep(StillleafCustody.RecensionFaction), "Hollin promised nothing for its absence");
            StringAssert.Contains("Status: continuing", string.Join("\n", MessageLog.GetRecent(8)));
        }

        [Test]
        public void FilingNeedsTheSaltFile()
        {
            Go(salt, indexer); CarryRegister();
            Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Destroy(cabinet, player, salt, "test"));
            Assert.IsFalse(StillleafCustody.CanConversation(indexer, player, "file"));
            Assert.IsFalse(StillleafArchiveContent.TryConversation(indexer, player, "file"));
            Assert.IsTrue(player.GetPart<InventoryPart>().Objects.Contains(register), "nothing was taken");
            Assert.AreEqual(0, player.GetIntProperty(StillleafCustody.Outcome));
        }

        // ════════════════════════════════════════════════════════════
        //   Reseal in place
        // ════════════════════════════════════════════════════════════

        [Test]
        public void ResealInPlace_LocksTheDoorAgain_PaysNoOne_AndBothCanBeToldTruthfully()
        {
            AtCustodyStage(); GoOutsideTheDoor(); CarryKey();
            Assert.IsFalse(DoorOffersReseal(), "a sealed door has nothing to offer");
            Assert.IsFalse(MovementSystem.TryMove(player, vault, 1, 0)); Assert.IsFalse(door.GetPart<LockPart>().IsLocked, "opened by the real bump");
            Assert.IsTrue(DoorOffersReseal(), "an open door offers to be resealed");
            Assert.IsTrue(Reseal(out int cost)); Assert.AreEqual(StillleafCustody.ResealCost, cost, "resealing is a turn's labour");
            Assert.IsTrue(door.GetPart<LockPart>().IsLocked); Assert.IsTrue(door.GetPart<PhysicsPart>().Solid);
            Assert.IsTrue(door.GetPart<SealedLibraryBarrierPart>().IsClosed); Assert.IsFalse(DoorOffersReseal());
            Assert.IsTrue(vault.GetAllEntities().Contains(register), "the register stays where it was kept");
            Assert.AreEqual(StillleafCustody.OutcomeResealed, player.GetIntProperty(StillleafCustody.Outcome));
            Assert.IsTrue(StoryletPart.Current.IsQuestCompleted(StillleafArchiveContent.QuestId));
            Assert.IsFalse(StoryletPart.Current.IsQuestFailed(StillleafArchiveContent.QuestId), "closure, not failure");
            Assert.AreEqual(0, Rep(StillleafCustody.RecensionFaction)); Assert.AreEqual(0, Rep(StillleafCustody.CurationFaction));
            // Both are told, once each, and nothing is paid for the telling.
            Go(quillhold, searcher);
            Assert.IsTrue(ConversationManager.StartConversation(searcher, player)); int report = Choice("report"); Assert.GreaterOrEqual(report, 0);
            ConversationManager.SelectChoice(report); ConversationManager.EndConversation();
            Assert.AreEqual(1, player.GetIntProperty(StillleafCustody.ToldSearcher)); Assert.IsFalse(StillleafCustody.CanConversation(searcher, player, "report"));
            StringAssert.Contains("Sealed again", string.Join("\n", MessageLog.GetRecent(6)));
            Go(salt, indexer);
            Assert.IsTrue(StillleafArchiveContent.TryConversation(indexer, player, "report"));
            Assert.IsFalse(StillleafCustody.CanConversation(indexer, player, "report"));
            StringAssert.Contains("correct, by other means", string.Join("\n", MessageLog.GetRecent(6)));
            Assert.AreEqual(0, Rep(StillleafCustody.RecensionFaction)); Assert.AreEqual(0, Rep(StillleafCustody.CurationFaction));
        }

        [TestCase("already_sealed")] [TestCase("no_key")] [TestCase("carrying_register")] [TestCase("not_beside_the_door")]
        [TestCase("register_not_inside")] [TestCase("custody_decided")]
        public void ResealRefusesForTheRightReason_AndChangesNothing(string reason)
        {
            GoOutsideTheDoor(); Diag.ResetAll(); CarryKey();
            if (reason != "already_sealed")
            { Assert.IsFalse(MovementSystem.TryMove(player, vault, 1, 0)); Assert.IsFalse(door.GetPart<LockPart>().IsLocked); }
            if (reason == "no_key")
            {
                // The door was opened with the key; the key is then put down before resealing.
                var key = player.GetPart<InventoryPart>().Objects.Single(e => e.GetPart<KeyPart>() != null);
                Assert.IsTrue(player.GetPart<InventoryPart>().RemoveObject(key));
            }
            if (reason == "carrying_register") CarryRegister();
            if (reason == "not_beside_the_door") Assert.IsTrue(MovementSystem.TryMove(player, vault, 1, 0), "standing in the doorway");
            if (reason == "register_not_inside")
            {
                var d = vault.GetEntityPosition(door); Assert.IsTrue(vault.RemoveEntity(register));
                var outside = vault.GetCell(d.x - 1, d.y - 1).BlocksMovement() ? vault.GetCell(d.x - 1, d.y + 1) : vault.GetCell(d.x - 1, d.y - 1);
                Assert.IsTrue(vault.AddEntity(register, outside.X, outside.Y), "dropped outside the vault");
            }
            if (reason == "custody_decided") player.SetIntProperty(StillleafCustody.Outcome, StillleafCustody.OutcomeDelivered);
            bool lockedBefore = door.GetPart<LockPart>().IsLocked;
            Assert.IsFalse(Reseal(out int cost)); Assert.AreEqual(0, cost, "no labour charged for a refusal");
            Assert.AreEqual(lockedBefore, door.GetPart<LockPart>().IsLocked, "state untouched");
            if (reason != "custody_decided") Assert.AreEqual(0, player.GetIntProperty(StillleafCustody.Outcome));
            Assert.IsFalse(StoryletPart.Current.IsQuestCompleted(StillleafArchiveContent.QuestId));
            Assert.AreEqual(1, Count("StillleafArchiveRejected")); StringAssert.Contains(reason, LastRejectedPayload());
            Assert.AreEqual(0, Count("StillleafArchiveApplied"));
        }

        // ════════════════════════════════════════════════════════════
        //   One choice, one payment
        // ════════════════════════════════════════════════════════════

        [Test]
        public void CustodyIsOneTime_AndMutuallyExclusive_AfterDelivery()
        {
            Go(quillhold, searcher); CarryRegister();
            Assert.IsTrue(StillleafArchiveContent.TryConversation(searcher, player, "deliver"));
            Assert.IsFalse(StillleafArchiveContent.TryConversation(searcher, player, "deliver"), "nothing left to deliver");
            Assert.AreEqual(StillleafCustody.RecensionForTheRecord, Rep(StillleafCustody.RecensionFaction), "paid once");
            Assert.IsFalse(StillleafCustody.CanConversation(searcher, player, "report"), "she received it; there is nothing to tell her");
            Go(salt, indexer); Assert.IsFalse(StillleafCustody.CanConversation(indexer, player, "file"));
            Assert.IsTrue(StillleafCustody.CanConversation(indexer, player, "report"), "the Indexer is owed the truth");
            GoOutsideTheDoor(); CarryKey(); MovementSystem.TryMove(player, vault, 1, 0);
            Assert.IsFalse(Reseal(out _)); StringAssert.Contains("custody_decided", LastRejectedPayload());
        }

        [Test]
        public void AfterResealing_TakingTheRegisterOutLater_DoesNotReopenTheChoice()
        {
            GoOutsideTheDoor(); CarryKey(); MovementSystem.TryMove(player, vault, 1, 0);
            Assert.IsTrue(Reseal(out _));
            CarryRegister(); Go(quillhold, searcher);
            Assert.IsFalse(StillleafCustody.CanConversation(searcher, player, "deliver"), "the choice was made");
            Go(salt, indexer); Assert.IsFalse(StillleafCustody.CanConversation(indexer, player, "file"));
            Assert.AreEqual(0, Rep(StillleafCustody.RecensionFaction)); Assert.AreEqual(0, Rep(StillleafCustody.CurationFaction));
        }

        [Test]
        public void AfterFiling_OnlyTheSearcherIsOwedTheTruth()
        {
            Go(salt, indexer); CarryRegister(); cabinet.GetPart<ContainerPart>().Locked = false;
            Assert.IsTrue(StillleafArchiveContent.TryConversation(indexer, player, "file"));
            Assert.IsFalse(StillleafCustody.CanConversation(indexer, player, "report"));
            Go(quillhold, searcher); Assert.IsTrue(StillleafCustody.CanConversation(searcher, player, "report"));
            Assert.IsTrue(StillleafArchiveContent.TryConversation(searcher, player, "report"));
            StringAssert.Contains("Filed beside its keeper", string.Join("\n", MessageLog.GetRecent(6)));
            Assert.AreEqual(0, Rep(StillleafCustody.RecensionFaction), "telling the truth is not paid and not punished");
        }

        [Test]
        public void AReleasedErrandStillLeavesCustodyToThePlayer()
        {
            // Refusal is closure with Hollin, not the loss of the record.
            Go(quillhold, searcher); CarryRegister();
            Assert.IsFalse(StoryletPart.Current.IsQuestActive(StillleafArchiveContent.QuestId));
            Assert.IsTrue(StillleafArchiveContent.TryConversation(searcher, player, "deliver"));
            Assert.AreEqual(StillleafCustody.OutcomeDelivered, player.GetIntProperty(StillleafCustody.Outcome));
            Assert.IsFalse(StoryletPart.Current.IsQuestCompleted(StillleafArchiveContent.QuestId), "no journal entry was open to complete");
        }

        [Test]
        public void TheRegisterInTheVaultCarriesTheJournalObjective()
        {
            Assert.AreEqual("register", register.GetPart<CompleteObjectiveOnTaken>()?.Objective);
            Assert.AreEqual(StillleafArchiveContent.QuestId, register.GetPart<CompleteObjectiveOnTaken>()?.Quest);
        }

        // ════════════════════════════════════════════════════════════
        //   The whole chain, through the real paths
        // ════════════════════════════════════════════════════════════

        [Test]
        public void TheWholeChain_AcceptWordsKeyDoorRegisterDelivery_ThroughTheRealPaths()
        {
            Go(quillhold, searcher);
            Assert.IsTrue(StillleafArchiveContent.TryConversation(searcher, player, "accept")); Assert.AreEqual(0, Stage);
            Go(salt, indexer);
            Assert.IsTrue(StillleafArchiveContent.TryConversation(indexer, player, "retrieve")); Assert.AreEqual(1, Stage);
            Assert.IsTrue(StillleafArchiveContent.TryConversation(indexer, player, "agree"));
            var key = cabinet.GetPart<ContainerPart>().Contents.Single();
            Assert.IsTrue(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(cabinet, key), player, salt).Success); Assert.AreEqual(2, Stage);
            GoOutsideTheDoor();
            Assert.IsFalse(MovementSystem.TryMove(player, vault, 1, 0), "the keeper's key opens the vault on the bump");
            Assert.IsTrue(MovementSystem.TryMove(player, vault, 1, 0), "and the next step enters");
            var r = vault.GetEntityPosition(register); Assert.IsTrue(vault.MoveEntity(player, r.x, r.y));
            Assert.IsTrue(InventorySystem.ExecuteCommand(new PickupCommand(register), player, vault).Success, "the real pickup");
            Assert.AreEqual(3, Stage, "custody");
            Go(quillhold, searcher);
            Assert.IsTrue(StillleafArchiveContent.TryConversation(searcher, player, "deliver"));
            Assert.IsTrue(StoryletPart.Current.IsQuestCompleted(StillleafArchiveContent.QuestId));
            Assert.AreEqual(StillleafCustody.RecensionForTheRecord, Rep(StillleafCustody.RecensionFaction));
            Assert.AreEqual(StillleafCustody.CurationBrokenAgreement, Rep(StillleafCustody.CurationFaction), "the agreed terms were broken");
            Go(salt, indexer);
            Assert.IsTrue(StillleafArchiveContent.TryConversation(indexer, player, "report"));
            StringAssert.Contains("against terms agreed", string.Join("\n", MessageLog.GetRecent(6)));
        }
    }
}
