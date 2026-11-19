using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The set-down fixture's reader line and the derived list (Docs/HOME-BASE-IMPLEMENTATION.md, PB3). Read-only and derived from live entities: nothing saved, no ledger.
    /// </summary>
    public sealed class HeldFixtureReaderTests : FiftyWorldFixture
    {
        OverworldZoneManager manager; string wildId; Zone wild; const int AX = 20, AY = 12;

        [SetUp] public void SetupReader()
        {
            PackableFixtureRegistry.ResetForTests(); Diag.ResetAll(); MessageLog.Clear();
            manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            wildId = Enumerable.Range(0, WorldMap.Width * WorldMap.Height).Select(i => WorldMap.ToZoneID(i % WorldMap.Width, i / WorldMap.Width, 0))
                .First(id => HeldPlaceZones.Classify(manager, id) == HeldPlaceZoneClass.WildSurface);
            wild = new Zone(wildId);
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++) Assert.True(wild.AddEntity(Factory.CreateEntity("Grass"), x, y));
            Zone.RemoveEntity(Actor); Zone = wild; Assert.True(Zone.AddEntity(Actor, AX, AY));
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { wildId, wild } }, wildId, new Dictionary<string, List<ZoneConnection>>());
            SettlementRuntime.ActiveZone = wild;
        }
        [TearDown] public void TearDownReader() { PackableFixtureRegistry.ResetForTests(); }

        string Row(Entity kit, string direction = "east") => Actions(kit).Single(a => a.Name == "SetDownFixture" && a.Display == "set down " + direction).Command;
        string Tail(Entity f) => Uri.EscapeDataString(wildId) + "|" + f.ID;
        List<string> Lines(Entity e, bool full)
        { var into = new List<string>(); StateReader.Collect(e, full, wild, wild.GetEntityCell(e), Actor, into); return into; }
        Entity SetDownAt(int ax, int ay, string direction = "east")
        {
            Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, ax, ay)); var box = Carry("CampBox"); Assert.True(Act(box, Row(box, direction))); return box;
        }

        // ── reader line ──

        [Test] public void ACarriedKitSaysItCanBeSetDownAndWhatThatMeansInBothReaders()
        {
            var kit = Carry("CampBox");
            foreach (bool full in new[] { false, true })
            {
                var line = Lines(kit, full).Single(l => l.Contains("set down")); StringAssert.Contains("stays where you put it", line); StringAssert.Contains("take it up again", line);
            }
        }

        [Test] public void AStandingHeldFixtureSaysYouSetItDownAndOffersBothWays()
        {
            var box = SetDownAt(AX, AY); var line = Lines(box, false).Single(l => l.StartsWith("You set this down")); StringAssert.Contains("take it up", line); StringAssert.Contains("hand it over", line);
            CollectionAssert.AreEqual(Lines(box, false), Lines(box, true), "the quick and full readers agree for this Part");
        }

        [Test] public void AHandedOverFixtureIsAnOrdinaryObjectAndSaysNothingExtra_Counter()
        {
            var box = SetDownAt(AX, AY); Assert.True(Act(box, "HandOver|" + Tail(box))); Assert.IsEmpty(Lines(box, false)); Assert.IsEmpty(Lines(box, true));
        }

        [Test] public void ATakenUpKitGoesBackToTheCarriedLine()
        {
            var box = SetDownAt(AX, AY); Assert.True(Act(box, "TakeUp|" + Tail(box))); Assert.True(Lines(box, false).Any(l => l.StartsWith("Can be set down")));
        }

        [Test] public void AnUnknownKindSaysItCannotTellHowItWasMeantToBeMoved()
        {
            var box = SetDownAt(AX, AY); PackableFixtureRegistry.InitializeFromJson("{\"Version\":1,\"Fixtures\":[]}");
            StringAssert.Contains("cannot tell how this was meant to be moved", Lines(box, false).Single());
        }

        [Test] public void TheLineNeverPromisesSafetyOrProtection()
        {
            var kit = Carry("CampBox"); var box = SetDownAt(AX, AY); var all = Lines(kit, true).Concat(Lines(box, true)).Concat(new[] { HeldFixtureReader.Describe(box, box.GetPart<PackableFixturePart>()) });
            foreach (string line in all) foreach (string word in new[] { "safe", "protect", "guard", "ward", "secure", "shelter" }) StringAssert.DoesNotContain(word, line.ToLowerInvariant());
        }

        [Test] public void AnOrdinaryChestHasNoFixtureLine_Counter()
        {
            var chest = Place("Chest", AX + 1, AY); Assert.IsEmpty(Lines(chest, false)); Assert.IsEmpty(Lines(chest, true));
        }

        [Test] public void TheRealExamineBuildersShowTheLine()
        {
            var box = SetDownAt(AX, AY); var examinable = box.GetPart<ExaminablePart>();
            StringAssert.Contains("You set this down", examinable.BuildWorldExamineLine(wild, wild.GetEntityCell(box), Actor));
        }

        [Test] public void TheHandOverRowStatesItsConsequenceInItsOwnLabel()
        {
            var box = SetDownAt(AX, AY); var row = Actions(box).Single(a => a.Name == "HandOverFixture");
            StringAssert.Contains("no longer take it up", row.Display); Assert.AreEqual(ActionTime.OneAction, row.Time);
        }

        // ── derived list ──

        [Test] public void NothingStandingMeansAnEmptyListAndNoSummary()
        {
            Assert.IsEmpty(HeldFixtureReader.Enumerate(manager)); Assert.AreEqual("", HeldFixtureReader.Summarize(HeldFixtureReader.Enumerate(manager))); Assert.IsEmpty(HeldFixtureReader.Enumerate(null));
        }

        [Test] public void FixturesWithinEightCellsAreOnePlaceAndFartherOnesAreAnother()
        {
            SetDownAt(10, 6); SetDownAt(15, 6); SetDownAt(40, 18);
            var places = HeldFixtureReader.Enumerate(manager); Assert.AreEqual(2, places.Count); Assert.AreEqual(2, places[0].Count); Assert.AreEqual(1, places[1].Count);
            Assert.AreEqual(wildId, places[0].ZoneId); Assert.Less(places[0].X, places[1].X, "ordered by position");
            Assert.AreEqual("You keep 3 things standing in 2 places.", HeldFixtureReader.Summarize(places));
        }

        [Test] public void ChainedFixturesJoinIntoOnePlaceBySingleLink()
        {
            SetDownAt(6, 6); SetDownAt(13, 6); SetDownAt(20, 6); // 7 then 7 apart: each within 8 of its neighbour
            var places = HeldFixtureReader.Enumerate(manager); Assert.AreEqual(1, places.Count); Assert.AreEqual(3, places[0].Count);
        }

        [Test] public void TheListIsDerivedFromLiveEntitiesSoTakeUpHandOverAndRemovalEmptyIt()
        {
            var a = SetDownAt(10, 6); var b = SetDownAt(40, 6); var c = SetDownAt(60, 6);
            Assert.AreEqual(3, HeldFixtureReader.Enumerate(manager).Count);
            Zone.RemoveEntity(Actor); Zone.AddEntity(Actor, 10, 6); Assert.True(Act(a, "TakeUp|" + Tail(a))); Assert.AreEqual(2, HeldFixtureReader.Enumerate(manager).Count);
            Zone.RemoveEntity(Actor); Zone.AddEntity(Actor, 40, 6); Assert.True(Act(b, "HandOver|" + Tail(b))); Assert.AreEqual(1, HeldFixtureReader.Enumerate(manager).Count);
            wild.RemoveEntity(c); Assert.AreEqual(0, HeldFixtureReader.Enumerate(manager).Count, "deleting the only fixture empties the list; there is no ledger to correct");
        }

        [Test] public void AuthoredInstallationsAndOtherOriginsNeverAppear_Counter()
        {
            var forge = Place("TinkersForge", 30, 8); forge.AddPart(new InstallationMarkerPart { Origin = "Generated", RetainZone = true }); Assert.IsEmpty(HeldFixtureReader.Enumerate(manager));
            var relocated = Place("Chest", 50, 8); relocated.AddPart(new InstallationMarkerPart { Origin = "Relocated" }); Assert.AreEqual(1, HeldFixtureReader.Enumerate(manager).Count, "a relocated rig is the player's to keep");
        }

        [Test] public void TheOldestSetDownTickIsKeptPerPlace()
        {
            var a = SetDownAt(10, 6); a.GetPart<PackableFixturePart>().SetTick = 40; var b = SetDownAt(14, 6); b.GetPart<PackableFixturePart>().SetTick = 25;
            Assert.AreEqual(25, HeldFixtureReader.Enumerate(manager).Single().FirstTick);
        }

        [Test] public void EnumeratingNeverGeneratesOrLoadsAZoneAndNeverMutates()
        {
            SetDownAt(10, 6); int zones = manager.CachedZones.Count; int entities = wild.EntityCount;
            for (int i = 0; i < 3; i++) HeldFixtureReader.Enumerate(manager);
            Assert.AreEqual(zones, manager.CachedZones.Count); Assert.AreEqual(entities, wild.EntityCount);
        }

        [Test] public void OnlyTheReaderAndTheListConsumeTheEnumerationInProductionCode()
        {
            string root = Path.Combine(Application.dataPath, "Scripts");
            var users = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories).Where(f => File.ReadAllText(f).Contains("HeldFixtureReader.Enumerate(")).Select(Path.GetFileName).ToArray();
            CollectionAssert.IsSubsetOf(users, new[] { "HeldFixtureReader.cs" }, "a new consumer must be added here on purpose: the list is derived, never authoritative");
        }

        [Test] public void AThreeHundredZoneCacheListsInBoundedTime()
        {
            var zones = new Dictionary<string, Zone>();
            for (int i = 0; i < 300; i++)
            {
                var z = new Zone("synthetic-" + i); zones[z.ZoneID] = z;
                if (i % 10 == 0) { var f = Factory.CreateEntity("CampBox"); f.AddPart(new InstallationMarkerPart { Origin = "Player" }); f.AddPart(new PackableFixturePart { Kind = "camp-box", SetTick = i }); z.AddEntity(f, 10, 10); }
            }
            manager.ReplaceLoadedState(zones, "synthetic-0", new Dictionary<string, List<ZoneConnection>>());
            var watch = System.Diagnostics.Stopwatch.StartNew(); var places = HeldFixtureReader.Enumerate(manager); watch.Stop();
            Assert.AreEqual(30, places.Count); Assert.Less(watch.ElapsedMilliseconds, 250, "an open-the-list cost, not a per-frame cost");
        }

        // ── map notes: where did I leave my camp ──

        Zone KeptZone(int x, int y, int z, string blueprint = "CampBox", int count = 1)
        {
            string id = WorldMap.ToZoneID(x, y, z); var zone = new Zone(id);
            for (int i = 0; i < count; i++)
            {
                var f = Factory.CreateEntity(blueprint); f.AddPart(new InstallationMarkerPart { Origin = "Player" }); f.AddPart(new PackableFixturePart { Kind = "camp-box", SetTick = 1 });
                Assert.True(zone.AddEntity(f, 10 + i * 12, 10)); // 12 apart: two clusters in one zone
            }
            var all = new Dictionary<string, Zone>(manager.CachedZones) { [id] = zone };
            manager.ReplaceLoadedState(all, wildId, new Dictionary<string, List<ZoneConnection>>());
            return zone;
        }

        [Test] public void NothingKeptMeansNoMapNotes_AndNullsAndAZeroCapAreSafe()
        {
            Assert.IsEmpty(HeldFixtureReader.MapNotes(manager, 10, 10)); Assert.IsEmpty(HeldFixtureReader.MapNotes(null, 10, 10));
            KeptZone(10, 10, 0); Assert.IsEmpty(HeldFixtureReader.MapNotes(manager, 10, 10, 0)); Assert.DoesNotThrow(() => HeldFixtureReader.OnMapOpened(null, 1, 1));
            MessageLog.Clear(); var empty = OverworldZoneManager.CreateDetached(Factory, 64, true); HeldFixtureReader.OnMapOpened(empty, 5, 5); Assert.IsEmpty(MessageLog.GetRecent(5));
        }

        [Test] public void ThingsKeptInTheZoneYouOpenTheMapFromAreThisPlace()
        {
            KeptZone(10, 10, 0); CollectionAssert.AreEqual(new[] { "You keep camp box: this place." }, HeldFixtureReader.MapNotes(manager, 10, 10));
        }

        [Test] public void OffsetsAreStepsFromTheCellYouStandOnNearestFirstWithEastWestSouthNorth()
        {
            KeptZone(8, 14, 0); KeptZone(13, 9, 0); KeptZone(10, 12, 0); KeptZone(10, 10, 0);
            CollectionAssert.AreEqual(new[]
            {
                "You keep camp box: this place.",
                "You keep camp box: 2 south of here.",
                "You keep camp box: 3 east, 1 north of here.",
                "You keep camp box: 2 west, 4 south of here.",
            }, HeldFixtureReader.MapNotes(manager, 10, 10));
        }

        [Test] public void ASurfaceAndABelowGroundZoneAtTheSameCellAreDistinguishedAndOrderedBySurfaceFirst()
        {
            KeptZone(10, 10, 1); KeptZone(10, 10, 0);
            CollectionAssert.AreEqual(new[] { "You keep camp box: this place.", "You keep camp box: this place, below ground." }, HeldFixtureReader.MapNotes(manager, 10, 10));
        }

        [Test] public void MoreThanTheCapIsCountedNotListed()
        {
            for (int i = 1; i <= 6; i++) KeptZone(10 + i, 10, 0);
            var notes = HeldFixtureReader.MapNotes(manager, 10, 10); Assert.AreEqual(5, notes.Count); StringAssert.Contains("1 east", notes[0]); StringAssert.Contains("4 east", notes[3]);
            Assert.AreEqual("You keep things in 2 more places.", notes[4]);
            Assert.AreEqual("You keep things in 1 more place.", HeldFixtureReader.MapNotes(manager, 10, 10, 5)[5]);
            Assert.AreEqual(6, HeldFixtureReader.MapNotes(manager, 10, 10, 6).Count, "a cap at least the size of the list shows everything and no rest line");
        }

        [Test] public void TwoClustersInOneZoneAreOneLineWithCounts()
        {
            KeptZone(12, 10, 0, "CampBox", 2); var notes = HeldFixtureReader.MapNotes(manager, 10, 10);
            CollectionAssert.AreEqual(new[] { "You keep camp box (x2): 2 east of here." }, notes);
        }

        [Test] public void DifferentKindsInOneZoneAreListedTogetherByName()
        {
            var zone = KeptZone(11, 10, 0); var bench = Factory.CreateEntity("CampBench"); bench.AddPart(new InstallationMarkerPart { Origin = "Player" }); Assert.True(zone.AddEntity(bench, 12, 10));
            CollectionAssert.AreEqual(new[] { "You keep camp bench, camp box: 1 east of here." }, HeldFixtureReader.MapNotes(manager, 10, 10));
        }

        [Test] public void CarriedHandedOverAndAuthoredThingsAreNeverListed_Counter()
        {
            Carry("CampBox"); var box = SetDownAt(AX, AY); Assert.True(Act(box, "HandOver|" + Tail(box)));
            var forge = Place("TinkersForge", 30, 8); forge.AddPart(new InstallationMarkerPart { Origin = "Generated", RetainZone = true });
            var (wx, wy, _) = WorldMap.FromZoneID(wildId); Assert.IsEmpty(HeldFixtureReader.MapNotes(manager, wx, wy));
        }

        [Test] public void OpeningTheMapWritesTheLinesFromTheCellYouAscendedFrom()
        {
            SetDownAt(AX, AY); var (wx, wy, _) = WorldMap.FromZoneID(wildId); MessageLog.Clear();
            var result = WorldMapTraversal.Ascend(Actor, wild, manager); Assert.True(result.Success, "the map opens in this fixture");
            Assert.AreEqual("You keep camp box: this place.", MessageLog.GetLast());
        }

        [Test] public void OpeningTheMapWithNothingKeptSaysNothingExtra_Counter()
        {
            MessageLog.Clear(); var result = WorldMapTraversal.Ascend(Actor, wild, manager); Assert.True(result.Success);
            Assert.False(MessageLog.GetRecent(10).Any(l => l.StartsWith("You keep")));
        }

        // ── arrival note ──

        [Test] public void NothingKeptMeansNoArrivalNote_AndANullZoneIsSafe()
        {
            Assert.IsNull(HeldFixtureReader.ArrivalNote(wild)); Assert.IsNull(HeldFixtureReader.ArrivalNote(null)); Assert.DoesNotThrow(() => HeldFixtureReader.OnZoneEntered(null)); Assert.IsEmpty(MessageLog.GetRecent(5));
        }

        [Test] public void TheArrivalNoteNamesWhatYouKeepAndCountsRepeats()
        {
            SetDownAt(10, 6); SetDownAt(40, 6); Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, 60, 14)); var hearth = Carry("CampBox"); Assert.True(Act(hearth, Row(hearth)));
            Assert.AreEqual("You keep here: camp box (x3).", HeldFixtureReader.ArrivalNote(wild)); HeldFixtureReader.OnZoneEntered(wild); StringAssert.Contains("camp box (x3)", MessageLog.GetLast());
        }

        [Test] public void AuthoredAndHandedOverThingsAreNotInTheArrivalNote_Counter()
        {
            var forge = Place("TinkersForge", 30, 8); forge.AddPart(new InstallationMarkerPart { Origin = "Generated", RetainZone = true });
            var box = SetDownAt(AX, AY); Assert.True(Act(box, "HandOver|" + Tail(box))); Assert.IsNull(HeldFixtureReader.ArrivalNote(wild));
        }
    }
}
