using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The sounding rod (Docs/MACHINE-INVENTED-USES.md 3g): in an ordinary deep cave it answers about unseen open ground, ways through and dense stone as a bearing and a distance, and the strike is
    /// loud. Everything it refuses and everything it must not say has its own case.
    /// </summary>
    public sealed class SoundingRodTests : FiftyWorldFixture
    {
        const string Cave = "Overworld.5.5.3";   // depth 3: limestone, reach 18
        Zone cave;
        Entity rod;

        [SetUp] public void SetupRod()
        {
            Diag.ResetAll(); MessageLog.Clear(); WorldClock.ResetForTests();
            // A solid cave of limestone with the player standing in a small, already seen room.
            cave = new Zone(Cave);
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++) { Assert.True(cave.AddEntity(Factory.CreateEntity("LimestoneWall"), x, y)); }
            Zone.RemoveEntity(Actor); Zone = cave;
            foreach (var (x, y) in Rect(38, 10, 42, 14)) Open(x, y);
            Assert.True(cave.AddEntity(Actor, 40, 12)); SeeRoom();
            rod = Carry("SoundingRod");
        }

        void Open(int x, int y) { var cell = cave.GetCell(x, y); foreach (var o in cell.Objects.ToArray()) cave.RemoveEntity(o); Assert.True(cave.AddEntity(Factory.CreateEntity("LimestoneFloor"), x, y)); }
        static IEnumerable<(int, int)> Rect(int x0, int y0, int x1, int y1) { for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) yield return (x, y); }
        void SeeRoom() { for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) cave.GetCell(x, y).Explored = x >= 37 && x <= 43 && y >= 9 && y <= 15; }
        void Hollow(int x0, int y0, int x1, int y1) { foreach (var (x, y) in Rect(x0, y0, x1, y1)) Open(x, y); }
        string SoundRow() => Actions(rod).Single(a => a.Name == "SoundGround").Command;
        string Said() => string.Join(" | ", MessageLog.GetRecent(6));

        [System.Serializable] class StockEntry { public string Blueprint; public int Chance; }
        [System.Serializable] class StockTable { public string Name; public List<StockEntry> Entries; }
        [System.Serializable] class StockFile { public List<StockTable> Tables; }

        // ── the item ──

        [Test] public void TheRodIsAnIronToolTheTinkerStocks()
        {
            var made = Factory.CreateEntity("SoundingRod"); Assert.NotNull(made.GetPart<SoundingRodPart>()); Assert.True(made.GetPart<PhysicsPart>().Takeable); Assert.Greater(made.GetPart<CommercePart>().Value, 0);
            var json = JsonUtility.FromJson<StockFile>(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Loot/LootTables.json"))); var tinker = json.Tables.Single(t => t.Name == "TinkerStock");
            Assert.True(tinker.Entries.Any(e => e.Blueprint == "SoundingRod" && e.Chance > 0 && e.Chance < 100), "findable at a Tinker, not guaranteed"); Assert.False(json.Tables.Where(t => t.Name != "TinkerStock").Any(t => t.Entries.Any(e => e.Blueprint == "SoundingRod")), "and nowhere else");
        }

        [Test] public void EveryCaveFloorTheGameCanMakeHasAReach_SoADeeperBandCannotSilentlyRefuseTheRod()
        {
            for (int depth = 1; depth <= 14; depth++) Assert.Greater(SoundingRodPart.Reach(SolidEarthBuilder.GetMaterialsForDepth(depth).floorBP), 0, "depth " + depth);
            Assert.AreEqual(0, SoundingRodPart.Reach("Grass")); Assert.AreEqual(0, SoundingRodPart.Reach(null)); Assert.Greater(SoundingRodPart.Reach("ObsidianFloor"), SoundingRodPart.Reach("SandstoneFloor"), "dense stone carries farther than soft");
        }

        // ── bearings and bands ──

        [TestCase(5, 0, "east")] [TestCase(5, -5, "northeast")] [TestCase(0, -5, "north")] [TestCase(-5, -5, "northwest")] [TestCase(-5, 0, "west")] [TestCase(-5, 5, "southwest")] [TestCase(0, 5, "south")] [TestCase(5, 5, "southeast")]
        public void BearingsAreEightPointsWithNorthUpTheMap(int dx, int dy, string expected) => Assert.AreEqual(expected, SoundingRodPart.Bearing(dx, dy));

        [Test] public void BandsAreCloseMiddlingFar() { Assert.AreEqual("close", SoundingRodPart.Band(8)); Assert.AreEqual("middling", SoundingRodPart.Band(9)); Assert.AreEqual("middling", SoundingRodPart.Band(16)); Assert.AreEqual("far", SoundingRodPart.Band(17)); }

        // ── what it hears ──

        [Test] public void AnUnseenHollowToTheEastIsHeardAsOpenGroundWithItsBearingAndBand()
        {
            Hollow(50, 10, 56, 14);   // five paces beyond the seen room's edge; nearest cell is 10 paces from the player
            Assert.True(Act(rod, SoundRow()), Said()); string said = Said(); StringAssert.Contains("Open ground lies east, middling.", said); StringAssert.Contains("The stone rings", said);
            Assert.AreEqual("Open ground lies east, middling.", rod.GetPart<SoundingRodPart>().LastReading);
        }

        [Test] public void ABoreAndASeamAreHeardToo_AndNeverNamed()
        {
            var stairs = Factory.CreateEntity("StairsDown"); Open(40, 4); cave.RemoveEntity(cave.GetCell(40, 4).Objects.First(o => o.BlueprintName == "LimestoneFloor")); Assert.True(cave.AddEntity(stairs, 40, 4));
            var vein = Factory.CreateEntity("GlowQuartzVein"); foreach (var o in cave.GetCell(33, 12).Objects.ToArray()) cave.RemoveEntity(o); Assert.True(cave.AddEntity(vein, 33, 12));
            Assert.True(Act(rod, SoundRow()), Said()); string reading = rod.GetPart<SoundingRodPart>().LastReading;
            StringAssert.Contains("Something bored through the stone lies north, close.", reading); StringAssert.Contains("Dense stone lies west, close.", reading);
            StringAssert.DoesNotContain("stair", reading.ToLowerInvariant()); StringAssert.DoesNotContain("quartz", reading.ToLowerInvariant()); Assert.False(Regex.IsMatch(reading, @"\b(up|down)\b"), "the rod cannot tell which way a bore goes");
        }

        [Test] public void OnlyUnseenGroundCounts_ASeenHollowAndNothingAnswersTheSame_Counters()
        {
            Hollow(50, 10, 56, 14); foreach (var (x, y) in Rect(50, 10, 56, 14)) cave.GetCell(x, y).Explored = true;
            Assert.True(Act(rod, SoundRow()), Said()); StringAssert.Contains("nothing in reach that you have not already seen", rod.GetPart<SoundingRodPart>().LastReading + " " + Said());
            StringAssert.DoesNotContain("Open ground", rod.GetPart<SoundingRodPart>().LastReading);
        }

        [Test] public void AGapTooSmallToBeAHollowAndGroundBeyondReachAreNotHeard()
        {
            Hollow(50, 10, 51, 11);   // four cells: below the six-cell floor
            Hollow(70, 10, 76, 14);   // 30 paces: past limestone's 18
            Assert.True(Act(rod, SoundRow())); StringAssert.DoesNotContain("Open ground", rod.GetPart<SoundingRodPart>().LastReading);
        }

        [Test] public void GroundCloserThanFourPacesIsNotALead()
        {
            Hollow(43, 12, 47, 14); foreach (var (x, y) in Rect(43, 12, 44, 14)) cave.GetCell(x, y).Explored = false; // unseen cells 3 paces away
            var leads = SoundingRodPart.Sense(cave, cave.GetCell(40, 12), 18); Assert.True(leads.All(l => l.Paces >= SoundingRodPart.MinPaces), string.Join(",", leads.Select(l => l.Paces)));
        }

        [Test] public void OnlyTheNearestOfEachKindIsReported_AtMostThreeLeads()
        {
            Hollow(48, 10, 54, 14); Hollow(60, 10, 66, 14); Hollow(20, 10, 26, 14);
            var leads = SoundingRodPart.Sense(cave, cave.GetCell(40, 12), 18); Assert.LessOrEqual(leads.Count, 3); Assert.AreEqual(1, leads.Count(l => l.Kind == "hollow")); Assert.AreEqual(8, leads.Single(l => l.Kind == "hollow").Paces, "the nearer east hollow, not the far ones");
        }

        // ── the cost: noise, and nothing consumed ──

        [Test] public void TheStrikeIsLoud_AndConsumesAndChangesNothingElse()
        {
            Hollow(50, 10, 56, 14); int items = Pack.Objects.Count; var before = cave.GetReadOnlyEntities().Count; var turns = new TurnManager(); turns.AdvanceClock(777);
            Assert.False(Noise.Recent(Actor, 5)); Assert.True(Act(rod, SoundRow())); Assert.True(Noise.Recent(Actor, 5), "eyeless hunters hear it"); Assert.AreEqual(items, Pack.Objects.Count); Assert.AreEqual(before, cave.GetReadOnlyEntities().Count);
            Assert.AreEqual(WorldClock.CurrentTick, rod.GetPart<SoundingRodPart>().LastTick);
        }

        // ── the survey log (a notebook of what the stone told you) ──

        string LogRow() => Actions(rod).Single(a => a.Name == "SurveyLog").Command;

        [Test] public void TheLogRowIsDisabledUntilTheFirstSoundingAndThenReadsFreeAnywhere()
        {
            var row = Actions(rod).Single(a => a.Name == "SurveyLog"); Assert.True(row.IsDisabled); Assert.AreEqual("empty", row.DisabledCode); StringAssert.Contains("not sounded", row.DisabledReason);
            Hollow(50, 10, 56, 14); Assert.True(Act(rod, SoundRow()));
            row = Actions(rod).Single(a => a.Name == "SurveyLog"); Assert.False(row.IsDisabled); Assert.AreEqual(ActionTime.Free, row.Time); StringAssert.Contains("1 sounding)", row.Display); StringAssert.Contains("takes no time", row.Hint);
        }

        [Test] public void EachSoundingIsLoggedWithWhereYouStoodAndTheLogKeepsTheLastEight()
        {
            Hollow(50, 10, 56, 14); var part = rod.GetPart<SoundingRodPart>();
            for (int i = 0; i < SoundingRodPart.LogSize + 3; i++) { new TurnManager().AdvanceClock(10); Assert.True(Act(rod, SoundRow())); }
            Assert.AreEqual(SoundingRodPart.LogSize, part.Log.Count, "the oldest three fell off");
            var last = part.Log[part.Log.Count - 1].Split('|'); Assert.AreEqual(3, last.Length); Assert.AreEqual(part.LastTick, int.Parse(last[0])); StringAssert.Contains("depth 3", last[1]); StringAssert.Contains("at (40,12)", last[1]); StringAssert.Contains(part.LastReading, last[2]);
            var ticks = part.Log.Select(l => int.Parse(l.Split('|')[0])).ToList(); CollectionAssert.AreEqual(ticks.OrderBy(t => t).ToList(), ticks, "oldest first");
        }

        [Test] public void ReadingTheLogPrintsItOldestFirstAndChangesNothing()
        {
            Hollow(50, 10, 56, 14); Assert.True(Act(rod, SoundRow())); new TurnManager().AdvanceClock(WorldClock.DayLengthTicks * 2); Assert.True(Act(rod, SoundRow()));
            var part = rod.GetPart<SoundingRodPart>(); var snapshot = new List<string>(part.Log); int tick = part.LastTick; int items = Pack.Objects.Count; MessageLog.Clear();
            Assert.True(Act(rod, LogRow()));
            var said = MessageLog.GetRecent(8); Assert.True(said.Any(m => m.Contains("survey log")), string.Join(" | ", said));
            int first = said.ToList().FindIndex(m => m.Contains("days ago")), second = said.ToList().FindIndex(m => m.Contains("just now")); Assert.Greater(first, -1); Assert.Greater(second, first, "older first, newest last");
            Assert.True(said.Any(m => m.Contains("map (5,5) depth 3") && m.Contains("Open ground lies")), "where you stood and what it said");
            CollectionAssert.AreEqual(snapshot, part.Log, "reading adds nothing"); Assert.AreEqual(tick, part.LastTick); Assert.AreEqual(items, Pack.Objects.Count);
        }

        [Test] public void ARolledBackSoundingLeavesTheLogAsItWas_AndAnOlderRodWithoutALogLoadsEmpty()
        {
            Hollow(50, 10, 56, 14); Assert.True(Act(rod, SoundRow())); var part = rod.GetPart<SoundingRodPart>(); var before = new List<string>(part.Log); string command = SoundRow(); FailAfter();
            Assert.False(Act(rod, command)); CollectionAssert.AreEqual(before, part.Log, "the failed strike was never logged");
            Assert.IsEmpty(new SoundingRodPart().Log, "a rod saved before the log existed starts with none");
        }

        [Test] public void TheLogRoundTripsThroughASave()
        {
            Hollow(50, 10, 56, 14); Assert.True(Act(rod, SoundRow())); var part = rod.GetPart<SoundingRodPart>();
            var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(rod).GetPart<SoundingRodPart>();
            CollectionAssert.AreEqual(part.Log, copy.Log); Assert.AreEqual(part.LastReading, copy.LastReading);
        }

        [Test] public void AnOuterFailureUndoesTheReadingAndTheNoise()
        {
            Hollow(50, 10, 56, 14); string command = SoundRow(); FailAfter(); Assert.False(Act(rod, command));
            Assert.AreEqual("", rod.GetPart<SoundingRodPart>().LastReading); Assert.AreEqual(-1, rod.GetPart<SoundingRodPart>().LastTick); Assert.False(Actor.IntProperties.ContainsKey(Noise.Property), "the strike never rang");
        }

        // ── where it will not sound ──

        [Test] public void OnTheSurfaceTheRowIsDisabledAndSaysWhy_AndAForgedCommandIsRefused()
        {
            var surface = new Zone("Overworld.5.5.0"); Zone.RemoveEntity(Actor); Zone = surface; Assert.True(surface.AddEntity(Actor, 40, 12));
            var row = Actions(rod).Single(a => a.Name == "SoundGround"); Assert.True(row.IsDisabled); StringAssert.Contains("too loose", row.DisabledReason); Assert.False(Act(rod, "SoundGround")); Assert.AreEqual("", rod.GetPart<SoundingRodPart>().LastReading);
        }

        [Test] public void TheGleanersCellarIsNotSounded_BecauseItIsNotAnOrdinaryColumn()
        {
            var cellar = new Zone(SoundingRodPart.GleanersCellarZone); Zone.RemoveEntity(Actor); Zone = cellar; Assert.True(cellar.AddEntity(Actor, 40, 12));
            var row = Actions(rod).Single(a => a.Name == "SoundGround"); Assert.True(row.IsDisabled); StringAssert.Contains("no solid floor", row.DisabledReason, "the same words as for ground the rod cannot bite into");
        }

        [Test] public void LooseGroundThatIsNotAFloorTheRodBitesIntoIsRefused()
        {
            foreach (var o in cave.GetCell(40, 12).Objects.Where(o => o.HasTag("Terrain")).ToArray()) cave.RemoveEntity(o); Assert.True(cave.AddEntity(Factory.CreateEntity("Grass"), 40, 12));
            var row = Actions(rod).Single(a => a.Name == "SoundGround"); Assert.True(row.IsDisabled); StringAssert.Contains("no solid floor", row.DisabledReason);
        }

        [Test] public void ADroppedRodOffersNoSounding_Counter()
        {
            Pack.RemoveObject(rod); Assert.True(cave.AddEntity(rod, 41, 12)); Assert.IsEmpty(Actions(rod).Where(a => a.Name == "SoundGround"));
        }

        // ── the reader ──

        [Test] public void TheReaderSaysHowFarTheStoneCarriesHereAndWhatItLastHeard()
        {
            Hollow(50, 10, 56, 14); var lines = new List<string>(); StateReader.Collect(rod, false, cave, null, Actor, lines); StringAssert.Contains("about 18 paces", string.Join(" ", lines)); StringAssert.DoesNotContain("Last sounding", string.Join(" ", lines));
            Assert.True(Act(rod, SoundRow())); lines.Clear(); StateReader.Collect(rod, false, cave, null, Actor, lines); StringAssert.Contains("Last sounding (just now): Open ground lies east", string.Join(" ", lines));
        }

        [Test] public void TheRodSavesItsLastReading()
        {
            Hollow(50, 10, 56, 14); Assert.True(Act(rod, SoundRow())); var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(rod); Assert.AreEqual(rod.GetPart<SoundingRodPart>().LastReading, copy.GetPart<SoundingRodPart>().LastReading);
        }

        [Test] public void TheSensingIsDeterministicAndReadsOnlyTheGrid()
        {
            Hollow(50, 10, 56, 14); var a = SoundingRodPart.Sense(cave, cave.GetCell(40, 12), 18).Select(l => l.Sentence).ToArray(); var b = SoundingRodPart.Sense(cave, cave.GetCell(40, 12), 18).Select(l => l.Sentence).ToArray(); CollectionAssert.AreEqual(a, b);
        }

        // ── with a real world attached: the layout plan's own column rule ──

        [Test] public void InAWorldTheRodRefusesColumnsTheGameKeepsUnmappedWithTheSameWordsAsAnyOtherRefusal_AndSoundsAnOrdinaryOne()
        {
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            // A sinkhole's own column (it has a point of interest) and an ordinary cave column, both depth 3, both generated by the real pipeline.
            var (name, sx, sy) = SinkholeSites.All.First(); string special = WorldMap.ToZoneID(sx, sy, 3);
            string ordinary = Enumerable.Range(0, WorldMap.Width * WorldMap.Height).Select(i => (x: i % WorldMap.Width, y: i / WorldMap.Width)).Where(c => manager.WorldMap.GetPOI(c.x, c.y) == null && WorldMapAuthoring.InBounds(c.x, c.y))
                .Select(c => WorldMap.ToZoneID(c.x, c.y, 3)).First(id => !OverworldZoneManager.AuthoredWildernessZoneIDs.Contains(WorldMap.ToZoneID(WorldMap.FromZoneID(id).x, WorldMap.FromZoneID(id).y, 0)));
            string Refuse(string id, bool needCoupledFloor, out string message)
            {
                var zone = manager.GetZone(id); Assert.NotNull(zone, id); var all = Enumerable.Range(0, Zone.Width * Zone.Height).Select(i => zone.GetCell(i % Zone.Width, i / Zone.Width)).Where(c => !c.BlocksMovement()).ToList();
                var cell = needCoupledFloor ? all.FirstOrDefault(c => c.Objects.Any(o => SoundingRodPart.Reach(o.BlueprintName) > 0)) : all.FirstOrDefault(); Assert.NotNull(cell, id + " has no standing cell (" + all.Count + " open, floors: " + string.Join(",", all.Take(40).SelectMany(c => c.Objects).Select(o => o.BlueprintName).Distinct()) + ")");
                var walker = Factory.CreateEntity("Villager"); Assert.True(zone.AddEntity(walker, cell.X, cell.Y)); try { return SoundingRodPart.Refusal(walker, zone, out message, out _); } finally { zone.RemoveEntity(walker); }
            }
            Assert.IsNull(Refuse(ordinary, true, out _), "an ordinary cave floor is sounded");
            string code = Refuse(special, false, out string said); Assert.AreEqual("not-ordinary", code, name + " sits on a point of interest"); StringAssert.Contains("no solid floor", said);
            Refuse("Overworld.5.5.0", false, out string surface); StringAssert.Contains("too loose", surface);
        }

        // ── the interact (C) menu of the cell you stand in ──

        [Test] public void TheCellYouStandInOffersToSoundTheGround_RunByTheRod_AndNoOtherCellDoes()
        {
            var own = CellMenu(40, 12).Where(a => a.Name == "SoundGround").ToArray(); Assert.AreEqual(1, own.Length); Assert.AreSame(rod, own[0].Via); Assert.AreEqual(SoundRow(), own[0].Command, "the very command the pack row runs");
            Assert.IsEmpty(CellMenu(41, 12).Where(a => a.Name == "SoundGround"), "it is about the floor under you");
            Actor.IntProperties.Remove(Noise.Property); Assert.True(ChooseRow(own[0], 40, 12), Said()); Assert.True(Noise.Recent(Actor, 1), "the strike is loud, as it is from the pack");
        }
    }
}
