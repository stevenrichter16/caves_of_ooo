using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The other advertised routes, ER.4 (Docs/ENDING-ROUTES.md): the two Root acts
    /// on the closure-ledger and in the journal, pinned from every side the player
    /// meets them. Written as hypotheses before re-reading the code and run first
    /// against the shipped ER.1–ER.3; each states what it probes.
    /// </summary>
    public sealed class EndingRoutesLedgerTests
    {
        private HotbarSaveFixture scope; private EntityFactory factory; private OverworldZoneManager manager; private Zone chamber, here; private Entity player, face;
        private EntityFactory previousConversationFactory;

        [SetUp] public void SetUp()
        {
            scope = new HotbarSaveFixture(false, false); factory = GrovelandsCompositionTests.Factory(); manager = OverworldZoneManager.CreateDetached(factory, 64);
            chamber = manager.GetZone(RootSiteBuilder.ChamberZoneID); face = chamber.GetAllEntities().Single(e => e.HasPart<RootFacePart>());
            player = factory.CreateEntity("Player"); var f = chamber.GetEntityPosition(face);
            Assert.IsTrue(chamber.AddEntity(player, f.x - 1, f.y)); here = chamber;
            StoryletPart.Current = new StoryletPart(); StoryletPart.LocalPlayer = player; SettlementRuntime.ActiveZone = chamber; NarrativeStatePart.Current = new NarrativeStatePart();
            previousConversationFactory = ConversationActions.Factory; ConversationActions.Factory = factory;
            StoryletRegistry.Reset(); StoryletRegistry.LoadAll();
            StillleafArchiveContent.EnsureRegistered();
            Diag.ResetAll(); MessageLog.Clear();
        }
        [TearDown] public void TearDown() { ConversationManager.EndConversation(); ConversationActions.Factory = previousConversationFactory; StoryletRegistry.Reset(); StoryletPart.Current = null; StoryletPart.LocalPlayer = null; SettlementRuntime.ActiveZone = null; NarrativeStatePart.Current = null; scope.Dispose(); }

        private static int Choice(string target = null, string actionKey = null, string actionValue = null)
        {
            var choices = ConversationManager.VisibleChoices;
            for (int i = 0; i < choices.Count; i++)
            {
                var c = choices[i];
                if (target != null && c.Target != target) continue;
                if (actionKey != null && !c.Actions.Any(a => a.Key == actionKey && (actionValue == null || a.Value == actionValue))) continue;
                return i;
            }
            return -1;
        }
        private static void Select(string target) { int i = Choice(target: target); Assert.GreaterOrEqual(i, 0, target); Assert.IsTrue(ConversationManager.SelectChoice(i)); }
        private void MoveBeside(Zone zone, Entity e)
        {
            Assert.IsTrue(here.RemoveEntity(player)); var p = zone.GetEntityPosition(e);
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue; var c = zone.GetCell(p.x + dx, p.y + dy);
                if (c != null && !c.BlocksMovement()) { Assert.IsTrue(zone.AddEntity(player, c.X, c.Y)); here = zone; SettlementRuntime.ActiveZone = zone; return; }
            }
            Assert.Fail("no free cell beside " + e.GetDisplayName());
        }
        private void MoveTo(Zone zone, int x, int y) { Assert.IsTrue(here.RemoveEntity(player)); Assert.IsTrue(zone.AddEntity(player, x, y)); here = zone; SettlementRuntime.ActiveZone = zone; }
        private void BackToTheFace() { var f = chamber.GetEntityPosition(face); MoveTo(chamber, f.x - 1, f.y); }
        private Entity Hollin() => manager.GetZone(StillleafArchiveContent.QuillholdZoneId).GetAllEntities().Single(e => e.ID == StillleafArchiveContent.SearcherId);
        private Entity Halm() => manager.GetZone(StillleafSaltVault.ZoneId).GetAllEntities().Single(e => e.ID == StillleafSaltVault.IndexerId);
        private Entity TakeMarbleFromHollin()
        {
            var hollin = Hollin(); MoveBeside(manager.GetZone(StillleafArchiveContent.QuillholdZoneId), hollin);
            Assert.IsTrue(ConversationManager.StartConversation(hollin, player)); Select("Stone"); ConversationManager.SelectChoice(Choice(actionKey: "GiveItem", actionValue: "MemoryMarble")); ConversationManager.EndConversation();
            Assert.IsTrue(StoryletPart.Current.IsQuestActive(EndingRoutes.KeepQuestId)); return hollin;
        }
        private Entity TakeMuteStoneFromHalm()
        {
            var halm = Halm(); MoveBeside(manager.GetZone(StillleafSaltVault.ZoneId), halm);
            Assert.IsTrue(ConversationManager.StartConversation(halm, player)); Select("MuteStone"); ConversationManager.SelectChoice(Choice(actionKey: "GiveItem", actionValue: "MuteStone")); ConversationManager.EndConversation();
            return halm;
        }
        private Entity Tendril()
        {
            var t = factory.CreateEntity("ChoirTendril"); var f = chamber.GetEntityPosition(face); Assert.IsTrue(chamber.AddEntity(t, f.x - 2, f.y)); return t;
        }
        private void TakeThreadFrom(Entity tendril)
        {
            Assert.IsTrue(ConversationManager.StartConversation(tendril, player)); Select("WhatAreYou"); Select("FirstRoot"); Select("FindFirstRoot"); Select("Thread");
            ConversationManager.SelectChoice(Choice(actionKey: "GiveItem", actionValue: EndingRoutes.CuttingBlueprint)); ConversationManager.EndConversation();
            Assert.IsTrue(StoryletPart.Current.IsQuestActive(EndingRoutes.GatherQuestId));
        }
        private Entity Give(string bp) { var e = factory.CreateEntity(bp); Assert.IsNotNull(e, bp); Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(e)); return e; }
        private static void Start(string id) => StoryletPart.Current.StartQuest(new QuestState { QuestId = id, CurrentStageIndex = 0 });
        private static QuestLogSnapshot Journal() => QuestLogStateBuilder.Build(StoryletPart.Current, NarrativeStatePart.Current);
        private (Zone site, Entity seventh) AtTheSeventh()
        {
            var site = manager.GetZone(FellingSiteBuilder.ZoneID); var seventh = site.GetAllEntities().Single(e => e.HasPart<SeventhPositionPart>());
            var at = site.GetEntityPosition(seventh); MoveTo(site, at.x, at.y); return (site, seventh);
        }
        private static string LastPayload(string category, string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = category, Kind = kind, Limit = 60 }).Records.Last().PayloadJson.Replace(" ", "");
        private StoryletPart RoundTrip(StoryletPart part) { using var stream = new MemoryStream(); part.Save(new SaveWriter(stream)); stream.Position = 0; var loaded = new StoryletPart(); loaded.Load(new SaveReader(stream, factory)); return loaded; }

        /// <summary>Hypothesis: the stone from Hollin records her and Quillhold, and the reading says exactly where to end the act.</summary>
        [Test]
        public void Hypothesis_AStoneFromHollin_RecordsHerAndQuillhold_AndTheReadingSaysWhereToEndIt()
        {
            var hollin = TakeMarbleFromHollin();
            var e = StoryletPart.Current.GetClosure(EndingRoutes.KeepQuestId);
            Assert.AreEqual(hollin.GetDisplayName(), e.GiverName); Assert.AreEqual(StillleafArchiveContent.QuillholdZoneId, e.Where);
            Assert.AreEqual(EndingRoutes.KeepTitle + " — end it with " + hollin.GetDisplayName() + " at Quillhold.", StoryletPart.Current.ReadLedger().Descriptions.Single());
            Assert.AreEqual(EndingRoutes.KeepTitle, StoryletPart.QuestDisplayName(EndingRoutes.KeepQuestId));
            Assert.IsTrue(Journal().Active.Any(a => a.QuestId == EndingRoutes.KeepQuestId), "the journal lists it under ACTIVE");
        }

        /// <summary>Hypothesis: the tendril's thread records the tendril, and its place reads "below the Root".</summary>
        [Test]
        public void Hypothesis_TheThread_RecordsTheTendril_BelowTheRoot()
        {
            var t = Tendril(); TakeThreadFrom(t);
            var e = StoryletPart.Current.GetClosure(EndingRoutes.GatherQuestId);
            Assert.AreEqual(t.GetDisplayName(), e.GiverName); Assert.AreEqual(RootSiteBuilder.ChamberZoneID, e.Where);
            string d = StoryletPart.Current.Describe(e); StringAssert.Contains(EndingRoutes.GatherTitle, d); StringAssert.Contains("at below " + RootSiteBuilder.SiteName + ".", d);
        }

        /// <summary>Hypothesis: with both acts open the practice path is barred and names both, Gather before Keep, with givers and places.</summary>
        [Test]
        public void Hypothesis_BothActsOpen_BarThePracticePath_AndAreNamed_GatherBeforeKeep_WithGiversAndPlaces()
        {
            var hollin = TakeMarbleFromHollin(); BackToTheFace(); var t = Tendril(); TakeThreadFrom(t);
            var (site, seventh) = AtTheSeventh();
            Assert.IsFalse(EndingSpine.TryWorldAction(seventh, player, site, EndingSpine.NameCommand, out _)); StringAssert.Contains("ledger_open", LastPayload("ending", "Rejected"));
            string tell = MessageLog.GetRecent(3).Last(m => m.Contains("unspoken"));
            StringAssert.Contains(EndingRoutes.GatherTitle, tell); StringAssert.Contains(EndingRoutes.KeepTitle, tell); Assert.Less(tell.IndexOf(EndingRoutes.GatherTitle), tell.IndexOf(EndingRoutes.KeepTitle), "ordinal order of ids");
            StringAssert.Contains(hollin.GetDisplayName(), tell); StringAssert.Contains("Quillhold", tell); StringAssert.Contains("below " + RootSiteBuilder.SiteName, tell);
            Assert.AreEqual(0, EndingSpine.Enacted(player));
        }

        /// <summary>Hypothesis: the journal lists both acts under ACTIVE by name; after one is enacted it moves to COMPLETED and the other stays ACTIVE, the closure line counting one closed and one open.</summary>
        [Test]
        public void Hypothesis_TheJournal_ListsBoth_AndMovesOnlyTheEnactedOne_ToCompleted()
        {
            Start(EndingRoutes.GatherQuestId); Start(EndingRoutes.KeepQuestId);
            var before = Journal(); Assert.IsTrue(before.Active.Any(a => a.QuestId == EndingRoutes.GatherQuestId)); Assert.IsTrue(before.Active.Any(a => a.QuestId == EndingRoutes.KeepQuestId)); Assert.AreEqual(2, before.ClosureOpen);
            Give(EndingRoutes.CuttingBlueprint); Assert.IsTrue(EndingRoutes.TryWorldAction(face, player, chamber, EndingRoutes.GatherCommand, out _)); MessageLog.ConsumeAnnouncement();
            var after = Journal();
            CollectionAssert.Contains(after.Completed, EndingRoutes.GatherTitle); Assert.IsFalse(after.Active.Any(a => a.QuestId == EndingRoutes.GatherQuestId));
            Assert.IsTrue(after.Active.Any(a => a.QuestId == EndingRoutes.KeepQuestId), "the other stays ACTIVE");
            Assert.AreEqual(1, after.ClosureClosed); Assert.AreEqual(1, after.ClosureOpen); Assert.AreEqual(0, after.UnspokenCount, "an active act with a living giver is not unspoken");
        }

        /// <summary>Hypothesis: a tendril gone from its loaded place makes the Gather act lost; the journal names it under UNSPOKEN with [R]; [R] renounces it once; the reading records lost; and the practice path then opens.</summary>
        [Test]
        public void Hypothesis_AGoneTendril_MakesTheActLost_TheJournalNamesIt_RRenouncesItOnce_AndThePracticePathOpens()
        {
            var t = Tendril(); TakeThreadFrom(t); Assert.IsTrue(chamber.RemoveEntity(t));
            var e = StoryletPart.Current.GetClosure(EndingRoutes.GatherQuestId); Assert.IsTrue(StoryletPart.Current.IsLost(e));
            var journal = Journal(); Assert.AreEqual(1, journal.LostCount); StringAssert.Contains("is gone; [R] renounces it", journal.Unspoken.Single()); StringAssert.Contains(EndingRoutes.GatherTitle, journal.Unspoken.Single());
            Diag.ResetAll(); StoryletPart.Current.ReadLedger(player); StringAssert.Contains("\"lost\":1", LastPayload("closure", "Read"));
            Assert.AreEqual(1, StoryletPart.Current.RenounceLost(player)); Assert.AreEqual(ClosureState.Refused, e.State); Assert.IsTrue(e.Spoken);
            Assert.AreEqual(0, StoryletPart.Current.RenounceLost(player), "once"); Assert.IsTrue(StoryletPart.Current.ReadLedger().Clean);
            var (site, seventh) = AtTheSeventh();
            Assert.IsTrue(EndingSpine.TryWorldAction(seventh, player, site, EndingSpine.NameCommand, out _), "a renounced act does not bar the practice path");
            Assert.AreEqual(EndingSpine.PracticePath, EndingSpine.Enacted(player));
        }

        /// <summary>Hypothesis: a tendril dead where it stands is lost too.</summary>
        [Test]
        public void Hypothesis_ADeadTendrilStillStanding_IsLost()
        {
            var t = Tendril(); TakeThreadFrom(t); t.SetStatValue("Hitpoints", 0);
            Assert.IsTrue(StoryletPart.Current.IsLost(StoryletPart.Current.GetClosure(EndingRoutes.GatherQuestId)));
            Assert.AreEqual(1, StoryletPart.Current.RenounceLost(player));
        }

        /// <summary>Hypothesis: a Root ending leaves another open act open and listed; the seventh then refuses as already enacted, not as a ledger gate.</summary>
        [Test]
        public void Hypothesis_ARootEnding_LeavesAnotherActOpenAndListed_AndTheSeventhRefusesAsEnacted_NotAsAGate()
        {
            Start("errand"); foreach (var s in EndingRoutes.Stones) Give(s);
            Assert.IsTrue(EndingRoutes.TryWorldAction(face, player, chamber, EndingRoutes.SealCommand, out _)); MessageLog.ConsumeAnnouncement();
            var journal = Journal(); Assert.IsTrue(journal.Active.Any(a => a.QuestId == "errand")); Assert.AreEqual(1, journal.ClosureOpen); Assert.AreEqual(1, journal.ClosureClosed);
            var (site, seventh) = AtTheSeventh();
            Assert.IsFalse(EndingSpine.TryWorldAction(seventh, player, site, EndingSpine.NameCommand, out _)); StringAssert.Contains("already_enacted", LastPayload("ending", "Rejected"));
            Assert.IsTrue(StoryletPart.Current.IsQuestActive("errand"), "nothing is written off by an ending");
        }

        /// <summary>Hypothesis: releasing at either preserver ends the one shared act, and the other's release line goes with it.</summary>
        [Test]
        public void Hypothesis_ReleasingAtEitherPreserver_EndsTheOneSharedAct_AndTheOthersReleaseLineGoesWithIt()
        {
            var hollin = TakeMarbleFromHollin(); var halm = TakeMuteStoneFromHalm();
            Assert.AreEqual(hollin.GetDisplayName(), StoryletPart.Current.GetClosure(EndingRoutes.KeepQuestId).GiverName, "the act stays the first giver's");
            Assert.IsTrue(ConversationManager.StartConversation(halm, player)); int release = Choice(actionKey: "RefuseQuest", actionValue: EndingRoutes.KeepQuestId); Assert.GreaterOrEqual(release, 0);
            Assert.IsTrue(ConversationManager.SelectChoice(release)); ConversationManager.EndConversation();
            Assert.AreEqual(ClosureState.Refused, StoryletPart.Current.GetClosure(EndingRoutes.KeepQuestId).State); Assert.AreEqual(1, StoryletPart.Current.Ledger.Count);
            MoveBeside(manager.GetZone(StillleafArchiveContent.QuillholdZoneId), hollin);
            Assert.IsTrue(ConversationManager.StartConversation(hollin, player)); Assert.AreEqual(-1, Choice(actionKey: "RefuseQuest", actionValue: EndingRoutes.KeepQuestId), "nothing left to release with Hollin"); Assert.AreEqual(-1, Choice(target: "Stone"), "the stone was given once");
        }

        /// <summary>Hypothesis: both entries survive a save and a load with giver, place and title.</summary>
        [Test]
        public void Hypothesis_SaveLoad_KeepsBothEntries_WithGiverPlaceAndTitle()
        {
            var hollin = TakeMarbleFromHollin(); BackToTheFace(); var t = Tendril(); TakeThreadFrom(t);
            var loaded = RoundTrip(StoryletPart.Current);
            var k = loaded.GetClosure(EndingRoutes.KeepQuestId); Assert.AreEqual(hollin.ID, k.GiverId); Assert.AreEqual(hollin.GetDisplayName(), k.GiverName); Assert.AreEqual(StillleafArchiveContent.QuillholdZoneId, k.Where); Assert.AreEqual(EndingRoutes.KeepTitle, StoryletPart.ClosureTitle(k));
            var g = loaded.GetClosure(EndingRoutes.GatherQuestId); Assert.AreEqual(t.ID, g.GiverId); Assert.AreEqual(RootSiteBuilder.ChamberZoneID, g.Where); Assert.AreEqual(EndingRoutes.GatherTitle, StoryletPart.ClosureTitle(g));
            Assert.IsTrue(loaded.IsQuestActive(EndingRoutes.KeepQuestId)); Assert.IsTrue(loaded.IsQuestActive(EndingRoutes.GatherQuestId)); Assert.AreEqual(2, loaded.ReadLedger().Open);
        }
    }
}
