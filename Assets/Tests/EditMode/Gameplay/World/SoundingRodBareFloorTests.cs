using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SoundingRodBareFloorTests : FiftyWorldFixture
    {
        const string NaturalCave = "Overworld.6.7.2";
        const int SandstoneReach = 12;

        OverworldZoneManager EnterCave(int seed = 2, string address = NaturalCave)
        {
            var manager = OverworldZoneManager.CreateDetached(Factory, seed, true);
            Assert.True(Zone.RemoveEntity(Actor));
            Zone = manager.GetZone(address);
            Assert.That(Zone, Is.Not.Null);
            Assert.True(BiomeCropPlan.IsOrdinaryCave(manager, address));
            manager.SetActiveZone(Zone);
            SettlementRuntime.ActiveZone = Zone;
            return manager;
        }

        Cell StandOn(Cell floor)
        {
            Assert.True(Zone.AddEntity(Actor, floor.X, floor.Y));
            floor.IsVisible = floor.Explored = true;
            return floor;
        }

        Cell StandOnNativeBareFloor()
        {
            var floor = Zone.Cells.Cast<Cell>().First(cell => cell.IsInterior && cell.IsPassable()
                && !cell.Occupants.Any() && !Zone.TileState.Has(cell.X, cell.Y)
                && cell.X > 0 && cell.X < Zone.Width - 1 && cell.Y > 0 && cell.Y < Zone.Height - 1);
            StandOn(floor);
            Assert.That(floor.Occupants, Is.EquivalentTo(new[] { Actor }));
            return floor;
        }

        [Test]
        public void RealBareSandstoneHasTheSameReachAsItsExplicitFloor()
        {
            EnterCave();
            StandOnNativeBareFloor();
            var rod = Carry("SoundingRod");

            Assert.That(SoundingRodPart.Refusal(Actor, Zone, out var why, out int reach), Is.Null, why);
            Assert.That(reach, Is.EqualTo(SandstoneReach));
            var lines = new List<string>();
            StateReader.Collect(rod, false, Zone, null, Actor, lines);
            Assert.That(string.Join(" ", lines), Does.Contain("The rod can survey up to 12 paces here."));
            Assert.That(rod.GetPart<SoundingRodPart>().Log, Is.Empty);
        }

        [Test]
        public void OrdinaryCellMenuCreatesOneNotebookEntryThatSurvivesAWholeSave()
        {
            var manager = EnterCave();
            var floor = StandOnNativeBareFloor();
            var rod = Carry("SoundingRod");
            var survey = rod.GetPart<SoundingRodPart>();
            Clock.AdvanceClock(230);
            int recordedAt = Clock.TickCount;

            var option = CellMenu(floor.X, floor.Y).Single(row => row.Command == SoundingRodPart.Command);
            Assert.That(option.Via, Is.SameAs(rod));
            Assert.That(option.IsDisabled, Is.False);
            Assert.True(ChooseRow(option, floor.X, floor.Y));
            Assert.That(survey.LastTick, Is.EqualTo(recordedAt));
            Assert.That(survey.LastReading, Is.Not.Empty);
            Assert.That(survey.Log.Single(), Is.EqualTo($"{recordedAt}|map (6,7) depth 2, at ({floor.X},{floor.Y})|{survey.LastReading}"));
            Assert.That(Noise.Recent(Actor, 1), Is.True);
            Assert.That(floor.Occupants, Is.EquivalentTo(new[] { Actor }));
            var logRow = Actions(rod).Single(row => row.Command == SoundingRodPart.LogVerb);
            Assert.True(Act(rod, logRow.Command));
            Assert.That(survey.Log, Has.Count.EqualTo(1));

            using var bytes = new MemoryStream();
            GameSessionState.Capture("sounding-bare-floor", "native-test", manager, Clock, Actor).Save(new SaveWriter(bytes));
            bytes.Position = 0;
            var loaded = GameSessionState.Load(new SaveReader(bytes, Factory));
            var loadedZone = loaded.ZoneManager.ActiveZone;
            var loadedFloor = loadedZone.GetEntityCell(loaded.Player);
            var loadedRod = loaded.Player.GetPart<InventoryPart>().Objects.Single(item => item.ID == rod.ID);
            var loadedSurvey = loadedRod.GetPart<SoundingRodPart>();
            Assert.That(loadedZone.ZoneID, Is.EqualTo(NaturalCave));
            Assert.That((loadedFloor.X, loadedFloor.Y), Is.EqualTo((floor.X, floor.Y)));
            Assert.That(loadedFloor.Occupants, Is.EquivalentTo(new[] { loaded.Player }));
            Assert.That(loadedSurvey.LastTick, Is.EqualTo(recordedAt));
            Assert.That(loadedSurvey.LastReading, Is.EqualTo(survey.LastReading));
            Assert.That(loadedSurvey.Log, Is.EqualTo(survey.Log));
        }

        [TestCase("inactive")]
        [TestCase("replaced")]
        [TestCase("wet")]
        [TestCase("hidden")]
        [TestCase("fixture")]
        public void AChangedOrObstructedBareCellDoesNotProduceASounding(string fault)
        {
            var manager = EnterCave();
            var floor = StandOnNativeBareFloor();
            if (fault == "inactive") manager.SetActiveZone(new Zone("Overworld.6.7.1"));
            if (fault == "replaced") manager.SetActiveZone(new Zone(NaturalCave));
            if (fault == "wet") Zone.TileState.WriteCoating(floor.X, floor.Y, "water", ZoneTileState.Permanent);
            if (fault == "hidden") floor.IsVisible = false;
            if (fault == "fixture") Place("SalvagedTimber", floor.X, floor.Y);
            var rod = Carry("SoundingRod");

            Assert.That(SoundingRodPart.Refusal(Actor, Zone, out _, out int reach), Is.Not.Null);
            Assert.That(reach, Is.Zero);
            Assert.False(Act(rod, SoundingRodPart.Command));
            Assert.That(rod.GetPart<SoundingRodPart>().Log, Is.Empty);
        }

        [Test]
        public void RemovingAnExplicitFloorInARoomDoesNotCreateNaturalSoundingGround()
        {
            const string roomed = "Overworld.6.7.3";
            var manager = EnterCave(64, roomed);
            var plan = UndergroundLayoutPlan.Select(manager.WorldSeed, roomed, manager.WorldMap.GetBiome(6, 7), null, Factory);
            Assert.True(plan.IsRoomed);
            var cell = Zone.Cells.Cast<Cell>().First(candidate => candidate.IsInterior && candidate.IsPassable()
                && !Zone.TileState.Has(candidate.X, candidate.Y) && candidate.Occupants.Count == 1
                && candidate.Objects[0].BlueprintName == plan.FloorBlueprint);
            var support = cell.Objects[0];
            StandOn(cell);
            Assert.That(SoundingRodPart.Refusal(Actor, Zone, out _, out int supportedReach), Is.Null);
            Assert.That(supportedReach, Is.EqualTo(18));

            Assert.True(Zone.RemoveEntity(support));
            Assert.That(cell.Occupants, Is.EquivalentTo(new[] { Actor }));
            Assert.That(SoundingRodPart.Refusal(Actor, Zone, out _, out int bareReach), Is.Not.Null);
            Assert.That(bareReach, Is.Zero);
        }
    }
}
