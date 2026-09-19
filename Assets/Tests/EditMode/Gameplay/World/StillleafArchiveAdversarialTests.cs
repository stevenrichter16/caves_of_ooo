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
    /// The Stillleaf Archive, SA.5 (Docs/MIDGAME-STILLLEAF-ARCHIVE.md): the
    /// dedicated adversarial sweep for the whole chain (ADVERSARIAL_TESTING.md
    /// taxonomy) plus hypothesis-driven probes of what a player actually does
    /// that the per-milestone pins never simulated. Each hypothesis is stated
    /// in its docstring; the commit classifies which ran RED first.
    /// </summary>
    public sealed class StillleafArchiveAdversarialTests
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
        private void GoTo(Zone z, int x, int y) { Leave(); Assert.IsTrue(z.AddEntity(player, x, y)); SettlementRuntime.ActiveZone = z; }
        private void CarryRegister() { Assert.IsTrue(vault.RemoveEntity(register)); Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(register)); }
        private void CarryKey() { Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(factory.CreateEntity(StillleafArchive.KeyBlueprint))); }
        private void KnowTheWords(int stage = 0)
        { player.SetIntProperty(StillleafArchiveContent.WordsKnown, 1); StoryletPart.Current.StartQuest(new QuestState { QuestId = StillleafArchiveContent.QuestId, CurrentStageIndex = stage }); }
        private int Stage => StoryletPart.Current.GetActiveQuests().Single(q => q.QuestId == StillleafArchiveContent.QuestId).CurrentStageIndex;
        private static int Rep(string f) => PlayerReputation.Get(f);
        private static int Count(string category, string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = category, Kind = kind, Limit = 60 }).Records.Count;
        private static string LastPayload(string category, string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = category, Kind = kind, Limit = 60 }).Records.Last().PayloadJson;
        private Entity Key => cabinet.GetPart<ContainerPart>().Contents.SingleOrDefault(e => e.ID == StillleafSaltVault.KeyId);

        // ════════════════════════════════════════════════════════════
        //   Boundary inputs: nulls and nonsense never throw, never act
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_NullsAndNonsense_AreRefusedWithoutThrowing()
        {
            Assert.IsFalse(StillleafArchive.TryInstallVault(null, factory)); Assert.IsFalse(StillleafArchive.TryInstallVault(vault, null));
            Assert.IsFalse(StillleafArchiveContent.TryInstallSearcher(null, factory)); Assert.IsFalse(StillleafArchiveContent.TryInstallSearcher(quillhold, null));
            Assert.IsFalse(StillleafSaltVault.TryInstall(null, factory)); Assert.IsFalse(StillleafSaltVault.TryInstall(salt, null));
            Go(quillhold, searcher);
            Assert.IsFalse(StillleafArchiveContent.CanConversation(null, player, "accept"));
            Assert.IsFalse(StillleafArchiveContent.CanConversation(searcher, null, "accept"));
            Assert.IsFalse(StillleafArchiveContent.CanConversation(searcher, player, null));
            Assert.IsFalse(StillleafArchiveContent.CanConversation(searcher, player, ""));
            Assert.IsFalse(StillleafArchiveContent.TryConversation(searcher, player, "gibberish"));
            Assert.IsFalse(StillleafCustody.TryWorldAction(null, player, vault, StillleafCustody.ResealCommand, out int c1)); Assert.AreEqual(0, c1);
            Assert.IsFalse(StillleafCustody.TryWorldAction(door, null, vault, StillleafCustody.ResealCommand, out _));
            Assert.IsFalse(StillleafCustody.TryWorldAction(door, player, null, StillleafCustody.ResealCommand, out _));
            Assert.IsFalse(StillleafCustody.TryWorldAction(door, player, vault, "NotACommand", out _));
            Assert.AreEqual(0, StoryletPart.Current.GetActiveQuests().Count);
        }

        [Test]
        public void Adversarial_ForeignZonesAreRefusedByName()
        {
            var tine = manager.GetZone("Overworld.13.7.0"); Diag.ResetAll();
            Assert.IsFalse(StillleafArchive.TryInstallVault(tine, factory)); StringAssert.Contains("not_stillleaf_floor", LastPayload("worldgen", "StillleafRegisterRefused"));
            Assert.IsFalse(StillleafArchiveContent.TryInstallSearcher(tine, factory)); StringAssert.Contains("not_quillhold", LastPayload("worldgen", "StillleafSearcherRefused"));
            Assert.IsFalse(StillleafSaltVault.TryInstall(tine, factory)); StringAssert.Contains("not_salt_vault", LastPayload("worldgen", "StillleafIndexerRefused"));
            Assert.IsFalse(tine.GetAllEntities().Any(e => e.BlueprintName.StartsWith("Stillleaf")));
        }

        // ════════════════════════════════════════════════════════════
        //   Cross-actor: only the player's hands count
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_AnNpcTakingTheRegister_LearnsNothingForThePlayer_AndAdvancesNothing()
        {
            KnowTheWords(stage: 2); player.SetIntProperty(StillleafArchiveContent.WordsKnown, 0);
            var scribe = quillhold.GetAllEntities().Single(e => e.BlueprintName == "Scribe");
            Assert.IsTrue(vault.RemoveEntity(register)); Assert.IsTrue(scribe.GetPart<InventoryPart>().AddObject(register));
            var taken = GameEvent.New("Taken"); taken.SetParameter("Actor", (object)scribe); taken.SetParameter("Item", (object)register); register.FireEventAndRelease(taken);
            Assert.AreEqual(0, player.GetIntProperty(StillleafArchiveContent.WordsKnown), "the slate is in the scribe's hands, not the player's");
            Assert.AreEqual(2, Stage, "CompleteObjectiveOnTaken's player gate holds");
        }

        [Test]
        public void Adversarial_AnNpcTakingTheKey_AdvancesNothing()
        {
            KnowTheWords(stage: 1); var key = Key; var scribe = salt.GetAllEntities().First(e => e.BlueprintName == "Scribe");
            Assert.IsTrue(cabinet.GetPart<ContainerPart>().RemoveItem(key)); Assert.IsTrue(scribe.GetPart<InventoryPart>().AddObject(key));
            var taken = GameEvent.New("Taken"); taken.SetParameter("Actor", (object)scribe); taken.SetParameter("Item", (object)key); key.FireEventAndRelease(taken);
            Assert.AreEqual(1, Stage);
        }

        [Test]
        public void Adversarial_AHostileSearcherOffersNothing()
        {
            Go(quillhold, searcher);
            PlayerReputation.Set(StillleafCustody.RecensionFaction, -100);
            Assert.IsTrue(FactionManager.IsHostile(searcher, player), "standing this low is hostility on the faction bridge");
            Assert.IsFalse(StillleafArchiveContent.CanConversation(searcher, player, "accept"));
        }

        // ════════════════════════════════════════════════════════════
        //   Out of order: the chain from the wrong end
        // ════════════════════════════════════════════════════════════

        /// <summary>Hypothesis (corrected by the RED run): the vault door is not
        /// breakable, so the only way in is the keeper's key — by the words or by
        /// theft. A thief who never met the Searcher still holds the keeper's slate
        /// once the register is picked up, and the slate carries the last words;
        /// the index should answer them.</summary>
        [Test]
        public void Hypothesis_AThiefWhoNeverHeardTheWords_ReadsThemOffTheSlate_AndTheIndexAnswers()
        {
            Assert.IsFalse(DestructionSystem.IsBreakable(door), "the vault door cannot be broken; the key is the only way in");
            var fp = salt.GetEntityPosition(cabinet); var key = Key; GoTo(salt, Beside(salt, cabinet).X, Beside(salt, cabinet).Y);
            Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Destroy(cabinet, player, salt, "test"));
            Assert.IsTrue(salt.MoveEntity(player, fp.x, fp.y)); Assert.IsTrue(InventorySystem.ExecuteCommand(new PickupCommand(key), player, salt).Success);
            GoOutsideTheDoor();
            Assert.IsFalse(MovementSystem.TryMove(player, vault, 1, 0)); Assert.IsTrue(MovementSystem.TryMove(player, vault, 1, 0));
            var r = vault.GetEntityPosition(register); Assert.IsTrue(vault.MoveEntity(player, r.x, r.y));
            Assert.AreEqual(0, player.GetIntProperty(StillleafArchiveContent.WordsKnown), "never met the Searcher");
            Assert.IsTrue(InventorySystem.ExecuteCommand(new PickupCommand(register), player, vault).Success);
            Assert.AreEqual(1, player.GetIntProperty(StillleafArchiveContent.WordsKnown), "the slate is read on pickup");
            StringAssert.Contains(StillleafArchiveContent.LastWords, string.Join("\n", MessageLog.GetRecent(6)));
            Go(salt, indexer);
            Assert.IsTrue(StillleafSaltVault.CanConversation(indexer, player, "retrieve"));
            Assert.IsTrue(StillleafArchiveContent.TryConversation(indexer, player, "retrieve"));
            Assert.IsFalse(StillleafCustody.CanConversation(indexer, player, "file"), "the broken file cannot receive the record");
            Go(quillhold, searcher); Assert.IsTrue(StillleafCustody.CanConversation(searcher, player, "deliver"), "but Quillhold can");
        }

        /// <summary>Hypothesis: breaking the salt file is theft, once, by name;
        /// the spilled key still works, and the journal still advances when the
        /// player takes it, however it was obtained.</summary>
        [Test]
        public void Hypothesis_BreakingTheSaltFile_CostsCurationStandingOnce_AndTheKeyStillWorks()
        {
            KnowTheWords(stage: 1); var key = Key; var fp = salt.GetEntityPosition(cabinet); GoTo(salt, Beside(salt, cabinet).X, Beside(salt, cabinet).Y);
            Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Destroy(cabinet, player, salt, "test"));
            Assert.AreEqual(StillleafFilePart.CurationForABrokenFile, Rep(StillleafCustody.CurationFaction));
            Assert.AreEqual(1, player.GetIntProperty(StillleafFilePart.Broken));
            Assert.IsTrue(salt.GetCell(fp.x, fp.y).Objects.Contains(key));
            Assert.IsTrue(salt.MoveEntity(player, fp.x, fp.y));
            Assert.IsTrue(InventorySystem.ExecuteCommand(new PickupCommand(key), player, salt).Success);
            Assert.AreEqual(2, Stage, "the key objective does not ask how the key was obtained");
            GoOutsideTheDoor();
            Assert.IsFalse(MovementSystem.TryMove(player, vault, 1, 0)); Assert.IsFalse(door.GetPart<LockPart>().IsLocked, "a stolen key is still the key");
            Assert.AreEqual(StillleafFilePart.CurationForABrokenFile, Rep(StillleafCustody.CurationFaction), "charged once");
        }

        [Test]
        public void Adversarial_SomeoneElseBreakingTheFile_CostsThePlayerNothing()
        {
            var scribe = salt.GetAllEntities().First(e => e.BlueprintName == "Scribe");
            Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Destroy(cabinet, scribe, salt, "test"));
            Assert.AreEqual(0, Rep(StillleafCustody.CurationFaction)); Assert.AreEqual(0, player.GetIntProperty(StillleafFilePart.Broken));
        }

        /// <summary>Hypothesis: taking the errand AFTER reading the file and taking
        /// the key must not leave the journal stuck at a stage whose objectives can
        /// never fire again.</summary>
        [Test]
        public void Hypothesis_AcceptingAfterTheWorkIsDone_FastForwardsTheJournal()
        {
            player.SetIntProperty(StillleafArchiveContent.WordsKnown, 1); Go(salt, indexer);
            Assert.IsTrue(StillleafArchiveContent.TryConversation(indexer, player, "retrieve"));
            Assert.IsTrue(StillleafArchiveContent.TryConversation(indexer, player, "agree"));
            Assert.IsTrue(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(cabinet, Key), player, salt).Success);
            Assert.AreEqual(0, StoryletPart.Current.GetActiveQuests().Count, "no journal yet");
            Go(quillhold, searcher);
            Assert.IsTrue(StillleafArchiveContent.TryConversation(searcher, player, "accept"));
            Assert.AreEqual(2, Stage, "file read and key in hand are already done: the journal opens at 'descend'");
        }

        [Test]
        public void Hypothesis_AcceptingWhileCarryingTheRegister_OpensAtCustody()
        {
            player.SetIntProperty(StillleafSaltVault.FileFound, 1); CarryKey(); CarryRegister(); Go(quillhold, searcher);
            Assert.IsTrue(StillleafArchiveContent.TryConversation(searcher, player, "accept"));
            Assert.AreEqual(3, Stage);
            Assert.IsTrue(StillleafCustody.CanConversation(searcher, player, "deliver"));
        }

        // ════════════════════════════════════════════════════════════
        //   Lost participants
        // ════════════════════════════════════════════════════════════

        /// <summary>Hypothesis: a destroyed register must close the chain, not
        /// leave it dangling: both residents can be told, nothing is paid, and
        /// the journal is removed rather than failed.</summary>
        [Test]
        public void Hypothesis_ADestroyedRegister_ClosesTheChainOnReport()
        {
            KnowTheWords(stage: 2);
            Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Destroy(register, player, vault, "test"));
            Assert.AreEqual(1, player.GetIntProperty(StillleafRegisterPart.Destroyed));
            Go(quillhold, searcher);
            Assert.IsTrue(StillleafCustody.CanConversation(searcher, player, "report"));
            Assert.IsTrue(StillleafArchiveContent.TryConversation(searcher, player, "report"));
            Assert.AreEqual(StillleafCustody.OutcomeLost, player.GetIntProperty(StillleafCustody.Outcome));
            Assert.IsFalse(StoryletPart.Current.IsQuestActive(StillleafArchiveContent.QuestId)); Assert.IsFalse(StoryletPart.Current.IsQuestFailed(StillleafArchiveContent.QuestId));
            Assert.AreEqual(ClosureState.Refused, StoryletPart.Current.GetClosure(StillleafArchiveContent.QuestId)?.State, "ES.1: a reported loss is a spoken end");
            StringAssert.Contains("Destroyed", string.Join("\n", MessageLog.GetRecent(6)));
            Assert.IsFalse(StillleafCustody.CanConversation(searcher, player, "report"), "told once");
            Go(salt, indexer);
            Assert.IsTrue(StillleafArchiveContent.TryConversation(indexer, player, "report"));
            StringAssert.Contains("destroyed", string.Join("\n", MessageLog.GetRecent(6)));
            Assert.AreEqual(0, Rep(StillleafCustody.RecensionFaction)); Assert.AreEqual(0, Rep(StillleafCustody.CurationFaction));
            GoOutsideTheDoor(); CarryKey(); MovementSystem.TryMove(player, vault, 1, 0);
            Assert.IsFalse(StillleafCustody.TryWorldAction(door, player, vault, StillleafCustody.ResealCommand, out _), "nothing left to seal in");
        }

        [Test]
        public void Adversarial_ADeadSearcherAfterAcceptance_CustodyStillCompletes_ButSheCannotBeTold()
        {
            Go(quillhold, searcher); Assert.IsTrue(StillleafArchiveContent.TryConversation(searcher, player, "accept"));
            searcher.SetStatValue("Hitpoints", 0);
            Go(salt, indexer); CarryRegister(); cabinet.GetPart<ContainerPart>().Locked = false;
            Assert.IsTrue(StillleafArchiveContent.TryConversation(indexer, player, "file"));
            Assert.IsTrue(StoryletPart.Current.IsQuestCompleted(StillleafArchiveContent.QuestId));
            Go(quillhold, searcher); Assert.IsFalse(StillleafCustody.CanConversation(searcher, player, "report"));
        }

        [Test]
        public void Adversarial_ADeadIndexerAfterRelease_TheKeyIsStillTakeable()
        {
            KnowTheWords(); Go(salt, indexer);
            Assert.IsTrue(StillleafArchiveContent.TryConversation(indexer, player, "retrieve")); Assert.IsTrue(StillleafArchiveContent.TryConversation(indexer, player, "agree"));
            indexer.SetStatValue("Hitpoints", 0);
            Assert.IsTrue(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(cabinet, Key), player, salt).Success);
            Assert.AreEqual(2, Stage);
        }

        [Test]
        public void Adversarial_ADeadPlayerChangesNothing()
        {
            Go(quillhold, searcher); player.SetStatValue("Hitpoints", 0);
            Assert.IsFalse(StillleafArchiveContent.CanConversation(searcher, player, "accept"));
            GoOutsideTheDoor(); CarryKey(); door.GetPart<LockPart>().IsLocked = false; door.GetPart<PhysicsPart>().Solid = false;
            Assert.IsFalse(StillleafCustody.TryWorldAction(door, player, vault, StillleafCustody.ResealCommand, out _));
            StringAssert.Contains("no_actor", LastPayload("quest", "StillleafArchiveRejected"));
        }

        // ════════════════════════════════════════════════════════════
        //   Anti-exploit
        // ════════════════════════════════════════════════════════════

        /// <summary>Hypothesis: putting the register into the unlocked salt file by
        /// hand is not filing — no outcome, no standing — and it can be taken back.</summary>
        [Test]
        public void Hypothesis_ManualPlacementInTheSaltFileIsNotFiling()
        {
            KnowTheWords(); Go(salt, indexer);
            Assert.IsTrue(StillleafArchiveContent.TryConversation(indexer, player, "retrieve")); Assert.IsTrue(StillleafArchiveContent.TryConversation(indexer, player, "agree"));
            Assert.IsTrue(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(cabinet, Key), player, salt).Success);
            CarryRegister();
            Assert.IsTrue(InventorySystem.ExecuteCommand(new PutInContainerCommand(cabinet, register), player, salt).Success);
            Assert.AreEqual(0, player.GetIntProperty(StillleafCustody.Outcome)); Assert.AreEqual(0, Rep(StillleafCustody.CurationFaction));
            Assert.IsFalse(StillleafCustody.CanConversation(indexer, player, "file"), "not in hand");
            Assert.IsTrue(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(cabinet, register), player, salt).Success);
            Assert.IsTrue(StillleafCustody.CanConversation(indexer, player, "file"));
        }

        [Test]
        public void Adversarial_AnImpostorRegisterIsNotTheRecord()
        {
            var fake = factory.CreateEntity(StillleafArchive.RegisterBlueprint); Assert.IsNotNull(fake);
            Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(fake));
            Go(quillhold, searcher); Assert.IsFalse(StillleafCustody.CanConversation(searcher, player, "deliver"));
            Go(salt, indexer); Assert.IsFalse(StillleafCustody.CanConversation(indexer, player, "file"));
            Assert.AreEqual(0, Rep(StillleafCustody.RecensionFaction));
        }

        [Test]
        public void Adversarial_FilingWithTheKeyStillInside_MakesRoomForTheRecord()
        {
            // The door was broken instead of unlocked, so the key was never taken.
            Go(salt, indexer); CarryRegister(); cabinet.GetPart<ContainerPart>().Locked = false;
            Assert.AreEqual(1, cabinet.GetPart<ContainerPart>().Contents.Count);
            Assert.IsTrue(StillleafArchiveContent.TryConversation(indexer, player, "file"));
            Assert.AreEqual(2, cabinet.GetPart<ContainerPart>().Contents.Count); Assert.IsTrue(cabinet.GetPart<ContainerPart>().Locked);
        }

        // ════════════════════════════════════════════════════════════
        //   Save/load reach
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_SaveLoad_PlayerChainStateRoundTrips()
        {
            player.SetIntProperty(StillleafArchiveContent.WordsKnown, 1); player.SetIntProperty(StillleafSaltVault.FileFound, 1);
            player.SetIntProperty(StillleafSaltVault.Terms, StillleafSaltVault.TermsBargained); player.SetIntProperty(StillleafCustody.Outcome, StillleafCustody.OutcomeResealed);
            player.SetIntProperty(StillleafCustody.ToldIndexer, 1);
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(player);
            Assert.AreEqual(1, loaded.GetIntProperty(StillleafArchiveContent.WordsKnown)); Assert.AreEqual(1, loaded.GetIntProperty(StillleafSaltVault.FileFound));
            Assert.AreEqual(StillleafSaltVault.TermsBargained, loaded.GetIntProperty(StillleafSaltVault.Terms));
            Assert.AreEqual(StillleafCustody.OutcomeResealed, loaded.GetIntProperty(StillleafCustody.Outcome)); Assert.AreEqual(1, loaded.GetIntProperty(StillleafCustody.ToldIndexer));
        }

        [Test]
        public void Adversarial_SaveLoad_ResealedDoorStaysSealed_AndLatchesSurvive()
        {
            GoOutsideTheDoor(); CarryKey(); MovementSystem.TryMove(player, vault, 1, 0);
            Assert.IsTrue(StillleafCustody.TryWorldAction(door, player, vault, StillleafCustody.ResealCommand, out _));
            var loadedDoor = PartRoundTripHelper.RoundTripEntityViaTokenGraph(door);
            Assert.IsTrue(loadedDoor.GetPart<LockPart>().IsLocked); Assert.AreEqual(SealedLibraryBuilder.KeyID, loadedDoor.GetPart<LockPart>().KeyId);
            Assert.IsTrue(loadedDoor.GetPart<SealedLibraryBarrierPart>().IsClosed);
            Assert.AreEqual(1, loadedDoor.GetIntProperty("StillleafRegisterInstalled"), "the register latch outlives the session");
        }

        [Test]
        public void Adversarial_SaveLoad_TheSaltFileKeepsItsLockAndItsKey()
        {
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(cabinet);
            var container = loaded.GetPart<ContainerPart>();
            Assert.IsTrue(container.Locked);
            Assert.AreEqual(1, container.Contents.Count, "the filed key travels with the file");
            Assert.AreEqual(SealedLibraryBuilder.KeyID, container.Contents[0].GetPart<KeyPart>()?.KeyId);
            Assert.AreEqual("key", container.Contents[0].GetPart<CompleteObjectiveOnTaken>()?.Objective);
            Assert.IsNotNull(loaded.GetPart<StillleafFilePart>());
        }

        [Test]
        public void Adversarial_SaveLoad_ResidentsKeepIdentityAndVoice()
        {
            var s = PartRoundTripHelper.RoundTripEntityViaTokenGraph(searcher); var i = PartRoundTripHelper.RoundTripEntityViaTokenGraph(indexer);
            Assert.AreEqual(StillleafArchiveContent.SearcherId, s.ID); Assert.AreEqual("searcher", s.GetPart<StillleafResidentPart>()?.ResidentId);
            Assert.AreEqual(StillleafArchiveContent.ConversationId, s.GetPart<ConversationPart>()?.ConversationID);
            Assert.AreEqual(StillleafSaltVault.IndexerId, i.ID); Assert.AreEqual("indexer", i.GetPart<StillleafResidentPart>()?.ResidentId);
            Assert.AreEqual(StillleafSaltVault.ConversationId, i.GetPart<ConversationPart>()?.ConversationID);
            Assert.IsTrue(ConversationActions.IsRegistered("StillleafArchive"), "OnAfterLoad re-registered the verbs");
        }

        [Test]
        public void Adversarial_SaveLoad_ADeliveredRegisterStaysSealed()
        {
            Go(quillhold, searcher); CarryRegister();
            Assert.IsTrue(StillleafArchiveContent.TryConversation(searcher, player, "deliver"));
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(register);
            Assert.IsFalse(loaded.GetPart<PhysicsPart>().Takeable); StringAssert.Contains("sealed", loaded.GetPart<RenderPart>().DisplayName);
            Assert.IsNotNull(loaded.GetPart<StillleafRegisterPart>()); Assert.AreEqual("register", loaded.GetPart<CompleteObjectiveOnTaken>()?.Objective);
        }

        // ════════════════════════════════════════════════════════════
        //   Diag contracts
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Adversarial_EveryRejectionNamesAReason_AndNeverClaimsSuccess()
        {
            Go(quillhold, searcher); Diag.ResetAll();
            Assert.IsFalse(StillleafArchiveContent.TryConversation(searcher, player, "deliver"));
            Assert.IsFalse(StillleafArchiveContent.TryConversation(searcher, player, "release"));
            Go(salt, indexer);
            Assert.IsFalse(StillleafArchiveContent.TryConversation(indexer, player, "retrieve"));
            Assert.IsFalse(StillleafArchiveContent.TryConversation(indexer, player, "agree"));
            var rejected = DiagQuery.Apply(new DiagQuery.Filter { Category = "quest", Kind = "StillleafArchiveRejected", Limit = 60 }).Records;
            Assert.AreEqual(4, rejected.Count);
            foreach (var r in rejected) StringAssert.Contains("\"reason\"", r.PayloadJson);
            Assert.AreEqual(0, Count("quest", "StillleafArchiveApplied"));
        }
    }
}
