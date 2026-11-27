using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class NaturalCropRainTests : FiftyWorldFixture
    {
        const int RainMoisture = 10;
        OverworldZoneManager manager;

        [SetUp]
        public void PrepareRegionalCropBoundary()
        {
            Zone.ZoneID = "Overworld.2.6.0";
            manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { [Zone.ZoneID] = Zone }, Zone.ZoneID,
                new Dictionary<string, List<ZoneConnection>>());
            SettlementRuntime.ActiveZone = Zone;
            Zone.ForEachCell((cell, _, _) => { cell.IsVisible = true; cell.Explored = true; });
            var world = new Entity { ID = "natural-rain-test-world", BlueprintName = "World" };
            world.AddPart(new CropSystemPart());
            TurnManager.World = world;
            Clock.AddEntity(Actor);
        }

        int Window(bool raining, int first = 0) => Enumerable.Range(first, RegionalWeather.CycleWindows)
            .First(window =>
            {
                var reading = RegionalWeather.Current(Zone, window * RegionalWeather.WindowTicks, RegionalWeather.PolicyVersion);
                return reading.Valid && reading.Raining == raining;
            });

        void GoToWindow(int window)
            => Clock.AdvanceClock(window * RegionalWeather.WindowTicks + 5 - Clock.TickCount);

        void EndPlayerTurn() => Clock.EndTurn(Actor, Zone);

        static CropPart CropState(Entity owner) => owner.GetPart<CropPart>();

        [Test]
        public void AShowerWetsOnlyEligibleOutdoorYoungBedsWithoutOverwritingMoreWater()
        {
            GoToWindow(Window(true));
            var exposed = Crop(x: 11, y: 10);
            var indoors = Crop(x: 12, y: 10);
            Zone.GetCell(12, 10).IsInterior = true;
            var untended = Crop(x: 13, y: 10, prepared: false);
            var ripe = Crop(stage: 2, x: 14, y: 10);
            var wetter = Crop(moisture: 20, x: 15, y: 10);

            EndPlayerTurn();

            Assert.AreEqual(RainMoisture, CropState(exposed).MoistureTicks);
            Assert.AreEqual(CropPart.WET_SOIL_BG, exposed.GetPart<RenderPart>().BackgroundColor);
            Assert.AreEqual(0, CropState(exposed).TicksInStage, "New rain cannot backdate growth.");
            Assert.AreEqual(0, CropState(indoors).MoistureTicks);
            Assert.AreEqual(0, CropState(untended).MoistureTicks);
            Assert.AreEqual(0, CropState(ripe).MoistureTicks);
            Assert.AreEqual(20, CropState(wetter).MoistureTicks);
        }

        [Test]
        public void AutoDropEmberwheatReceivesRainWhenAFreezeMakesThePlayerSkipATurn()
        {
            GoToWindow(Window(true));
            Bed();
            var seed = Carry("EmberwheatSeed");
            Assert.True(Act(seed, Choice(seed, "PlantSeedAt|", row => row.Command.EndsWith("|11|10"))));
            var crop = CropState(Zone.GetCell(11, 10).Objects.Single(owner => owner.BlueprintName == "EmberwheatCrop"));
            Assert.False(crop.HarvestAtMaturity, "The planted stock uses ordinary auto-drop maturity.");
            Assert.AreEqual(0, crop.MoistureTicks);

            if (!Actor.HasPart<StatusEffectsPart>()) Actor.AddPart(new StatusEffectsPart());
            var frozen = new FrozenEffect(1f);
            Assert.True(Actor.GetPart<StatusEffectsPart>().ForceApplyEffect(frozen));
            int tick = Clock.TickCount;
            Assert.That(Clock.ProcessUntilPlayerTurn(), Is.SameAs(Actor));

            Assert.Greater(Clock.TickCount, tick);
            Assert.That(frozen.Cold, Is.GreaterThan(0f).And.LessThan(1f), "The scheduler spent a blocked turn while the actor stayed frozen.");
            Assert.AreEqual(RainMoisture, crop.MoistureTicks);
            Assert.AreEqual(0, crop.TicksInStage);
            Assert.False(crop.HarvestAtMaturity);
        }

        [Test]
        public void GrowthStartsAfterTheShowerAndTheSameWindowCannotRefillTheBed()
        {
            var crop = CropState(Crop());
            // Keep this test on a young growth stage while its first moisture runs out.
            crop.TicksPerStage = 30;
            int first = Window(true);
            GoToWindow(first);
            EndPlayerTurn();
            Assert.AreEqual(RainMoisture, crop.MoistureTicks);
            Assert.AreEqual(0, crop.TicksInStage);

            EndPlayerTurn();
            Assert.AreEqual(RainMoisture, crop.MoistureTicks);
            Assert.AreEqual(0, crop.TicksInStage);
            Clock.AdvanceClock(CropTime.WorldTicksPerUnit);
            EndPlayerTurn();
            Assert.AreEqual(RainMoisture - 1, crop.MoistureTicks);
            Assert.AreEqual(1, crop.TicksInStage);

            Clock.AdvanceClock((RainMoisture - 1) * CropTime.WorldTicksPerUnit);
            EndPlayerTurn();
            Assert.AreEqual(0, crop.MoistureTicks);
            Assert.AreEqual(RainMoisture, crop.TicksInStage);
            GoToWindow(Window(true, first + 1));
            EndPlayerTurn();
            Assert.AreEqual(RainMoisture, crop.MoistureTicks);
            Assert.AreEqual(RainMoisture, crop.TicksInStage);
        }

        [Test]
        public void OnlyTheLocalPlayerTurnCanClaimRain()
        {
            GoToWindow(Window(true));
            var crop = CropState(Crop());
            var npc = Place("Creature", 9, 10);
            Clock.AddEntity(npc);
            Clock.EndTurn(npc, Zone);
            Assert.AreEqual(0, crop.MoistureTicks);

            var otherZone = new Zone("Overworld.2.7.0");
            manager.SetActiveZone(otherZone);
            // The legacy hook still points at the stale zone until the transition completes.
            Assert.That(SettlementRuntime.ActiveZone, Is.SameAs(Zone));
            EndPlayerTurn();
            Assert.AreEqual(0, crop.MoistureTicks);
            manager.SetActiveZone(Zone);
            Assert.AreEqual(0, crop.MoistureTicks);
            Clock.AdvanceClock(1);
            EndPlayerTurn();
            Assert.AreEqual(RainMoisture, crop.MoistureTicks);
        }

        [Test]
        public void ReadsAndDryReturnDoNotClaimPastRainAndOrdinaryWaterStillWorks()
        {
            var owner = Crop();
            var crop = CropState(owner);
            int rainy = Window(true);
            GoToWindow(rainy);

            Assert.That(manager.GetZone(Zone.ZoneID), Is.SameAs(Zone));
            CropTime.ReconcileZone(Zone, Clock.TickCount);
            var at = Zone.GetEntityCell(owner);
            CellMenu(at.X, at.Y);
            ExamineDocumentBuilder.BuildWorld(owner, Zone, at);
            Assert.AreEqual(0, crop.MoistureTicks);
            Assert.AreEqual(0, crop.TicksInStage);

            GoToWindow(Window(false, rainy + 1));
            EndPlayerTurn();
            Assert.AreEqual(0, crop.MoistureTicks);
            Assert.AreEqual(0, crop.TicksInStage);
            var water = Flask(2);
            Assert.True(Act(water, Pour(water, true)));
            Assert.AreEqual(1, Units(water));
            Assert.AreEqual(40, crop.MoistureTicks);
        }

        [Test]
        public void ReplantingAndReloadingTheExactSpentBedCannotClaimTheSameShower()
        {
            GoToWindow(Window(true));
            var original = Crop();
            var bed = Zone.GetCell(11, 10).Objects.Single(owner => owner.HasPart<CultivatedSoilPart>());
            EndPlayerTurn();
            Assert.AreEqual(RainMoisture, CropState(original).MoistureTicks);
            Assert.True(Act(original, Choice(original, "ClearCrop")));
            var seed = Seed();
            Assert.True(Act(seed, Choice(seed, "PlantSeedAt|", row => row.Command.EndsWith("|11|10"))));
            var replant = Zone.GetCell(11, 10).Objects.Single(owner => owner.HasPart<CropPart>());
            Assert.That(replant, Is.Not.SameAs(original));
            EndPlayerTurn();
            Assert.AreEqual(0, CropState(replant).MoistureTicks);

            using var stream = new MemoryStream();
            GameSessionState.Capture("natural-crop-rain", "one shower", manager, Clock, Actor, world: TurnManager.World)
                .Save(new SaveWriter(stream));
            stream.Position = 0;
            var loaded = GameSessionState.Load(new SaveReader(stream, Factory));
            manager = loaded.ZoneManager;
            Zone = manager.ActiveZone;
            Actor = loaded.Player;
            Clock = loaded.TurnManager;
            TurnManager.World = loaded.World;
            SettlementRuntime.ActiveZone = Zone;

            var cell = Zone.GetCell(11, 10);
            Assert.AreEqual(bed.ID, cell.Objects.Single(owner => owner.HasPart<CultivatedSoilPart>()).ID);
            var restored = cell.Objects.Single(owner => owner.HasPart<CropPart>());
            Assert.AreEqual(replant.ID, restored.ID);
            Clock.AdvanceClock(CropTime.WorldTicksPerUnit);
            EndPlayerTurn();
            Assert.AreEqual(0, CropState(restored).MoistureTicks);
            Assert.AreEqual(0, CropState(restored).TicksInStage);
        }

        [Test]
        public void LeavingAndUnloadingKeepsTheRainMarkedBedWithoutASecondRefill()
        {
            GoToWindow(Window(true));
            var home = Zone;
            var owner = Crop();
            var crop = CropState(owner);
            crop.TicksPerStage = 30;
            var bed = home.GetCell(11, 10).Objects.Single(entry => entry.HasPart<CultivatedSoilPart>());
            EndPlayerTurn();
            Assert.AreEqual(RainMoisture, crop.MoistureTicks);

            var away = new Zone("Overworld.2.7.0");
            Assert.True(home.TryTransferEntityTo(Actor, away, 10, 10));
            manager.SetActiveZone(away);
            Zone = away;
            SettlementRuntime.ActiveZone = away;
            manager.UnloadZone(home.ZoneID);
            Assert.That(manager.ActiveZone, Is.SameAs(away));
            Assert.True(manager.CachedZones.TryGetValue(home.ZoneID, out var held));
            Assert.That(held, Is.SameAs(home));
            Clock.AdvanceClock(RainMoisture * CropTime.WorldTicksPerUnit);
            EndPlayerTurn();
            Assert.AreEqual(RainMoisture, crop.MoistureTicks, "The away turn does not reconcile or wet the inactive bed.");

            Zone = manager.GetZone(home.ZoneID);
            Assert.That(Zone, Is.SameAs(home));
            Assert.True(away.TryTransferEntityTo(Actor, Zone, 10, 10));
            manager.SetActiveZone(Zone);
            SettlementRuntime.ActiveZone = Zone;
            var restoredBed = Zone.GetCell(11, 10).Objects.Single(entry => entry.HasPart<CultivatedSoilPart>());
            Assert.That(restoredBed, Is.SameAs(bed));
            Assert.That(crop.ParentEntity, Is.SameAs(owner));
            EndPlayerTurn();
            Assert.AreEqual(RainMoisture, crop.TicksInStage);
            Assert.AreEqual(0, crop.MoistureTicks, "Returning before this shower ends does not refill the same soil.");
        }
    }
}
