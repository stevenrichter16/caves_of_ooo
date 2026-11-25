using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SoddenRainGuideTests : FiftyWorldFixture
    {
        private const string Guide = "SoddenRainGuide";
        private const string Prefix = "GuideRain|";
        private const int ShowerTick = 610;
        private const int FirstFairTick = 1200;

        [Test]
        public void ShowerGuideWatersTheExactDrySumpsieveAndLeavesTheFlaskAvailable()
        {
            var cropOwner = EnterShelter(ShowerTick);
            var crop = cropOwner.GetPart<CropPart>();
            var guide = GuideOwner();
            var ripe = Zone.GetCell(47, 9).Objects.Single(e => e.BlueprintName == "SumpsieveCrop").GetPart<CropPart>();
            var flask = Flask(2);
            Assert.True(Weather.Raining);
            Assert.That(crop.MoistureTicks, Is.Zero);
            Assert.That(ripe.GrowthStage, Is.EqualTo(2));
            Assert.That(ripe.MoistureTicks, Is.Zero);

            var row = GuideRows().Single();
            Assert.That(row.Time, Is.EqualTo(ActionTime.OneAction));
            Assert.That(ManualRow(flask), Is.Not.Null);
            var document = ExamineDocumentBuilder.BuildWorld(guide, Zone, Zone.GetCell(51, 9), Actor);
            Assert.That(document, Is.Not.Null);
            Assert.That(document.ToPlainText(), Does.Contain("sumpsieve").IgnoreCase);
            Assert.That(document.ToPlainText(), Does.Contain("shower").IgnoreCase);
            Assert.That(Clock.TickCount, Is.EqualTo(ShowerTick));
            Assert.That(crop.MoistureTicks, Is.Zero);

            Assert.True(ChooseRow(row, 51, 9));
            Assert.That(Units(flask), Is.EqualTo(2));
            Assert.That(crop.MoistureTicks, Is.EqualTo(CropWateringService.MoisturePerUnit));
            Assert.That(cropOwner.GetPart<RenderPart>().BackgroundColor, Is.EqualTo(CropPart.WET_SOIL_BG));
            Assert.That(ripe.MoistureTicks, Is.Zero);
            Assert.False(Zone.GetCell(50, 9).Objects.Any(e => e.HasPart<LiquidPoolPart>()));
            Assert.False(Zone.TileState.HasCoating(50, 9, "water"));

            int shower = Weather.Window;
            Clock.AdvanceClock(CropWateringService.MoisturePerUnit * CropTime.WorldTicksPerUnit);
            Assert.True(CropTime.Reconcile(crop, Zone, Clock.TickCount));
            Assert.That(Weather.Window, Is.EqualTo(shower));
            Assert.That(crop.MoistureTicks, Is.Zero);
            Assert.That(crop.GrowthStage, Is.EqualTo(1));
            Assert.That(GuideRows(), Is.Empty, "Drying out does not supply a second watering in the same shower.");
            Assert.False(Act(guide, row.Command));
            Assert.True(ChooseRow(ManualRow(flask), 50, 9));
            Assert.That(Units(flask), Is.EqualTo(1));
            Assert.That(crop.MoistureTicks, Is.EqualTo(CropWateringService.MoisturePerUnit));
        }

        [TestCase(25, ShowerTick, true, false)]
        [TestCase(26, FirstFairTick, false, true)]
        public void FairWeatherAndTheOldColdSiteKeepOrdinaryFlaskWatering(int version, int tick, bool raining, bool hasGuide)
        {
            var crop = EnterShelter(tick, version).GetPart<CropPart>();
            Assert.That(Weather.Raining, Is.EqualTo(raining));
            Assert.That(Zone.GetReadOnlyEntities().Count(e => e.BlueprintName == Guide), Is.EqualTo(hasGuide ? 1 : 0));
            if (hasGuide) GuideOwner();
            Assert.That(GuideRows(), Is.Empty);
            Assert.That(crop.MoistureTicks, Is.Zero);

            var flask = Flask(2);
            Assert.True(ChooseRow(ManualRow(flask), 50, 9));
            Assert.That(Units(flask), Is.EqualTo(1));
            Assert.That(crop.MoistureTicks, Is.EqualTo(CropWateringService.MoisturePerUnit));
        }

        [Test]
        public void AShowerQuoteExpiresWhenTheWeatherTurnsFair()
        {
            var crop = EnterShelter(FirstFairTick - 1).GetPart<CropPart>();
            var flask = Flask(2);
            var old = GuideRows().Single();
            Assert.True(Weather.Raining);
            Clock.AdvanceClock(1);
            Assert.False(Weather.Raining);

            Assert.False(ChooseRow(old, 51, 9));
            Assert.That(crop.MoistureTicks, Is.Zero);
            Assert.That(Units(flask), Is.EqualTo(2));
            Assert.That(GuideRows(), Is.Empty);
            Assert.That(Clock.TickCount, Is.EqualTo(FirstFairTick));

            int rainyWindow = RegionalWeather.NextRainWindow(64, BiomeType.Sodden, Weather.Window + 1);
            Assert.That(rainyWindow, Is.GreaterThan(Weather.Window));
            Clock.AdvanceClock(rainyWindow * RegionalWeather.WindowTicks + 10 - Clock.TickCount);
            var fresh = GuideRows().Single();
            Assert.That(fresh.Command, Is.Not.EqualTo(old.Command));
            Assert.True(ChooseRow(fresh, 51, 9));
            Assert.That(crop.MoistureTicks, Is.EqualTo(CropWateringService.MoisturePerUnit));
            Assert.That(Units(flask), Is.EqualTo(2));
        }

        [Test]
        public void LateGuideFailureRestoresTheDryCropAndAllowsTheSameShowerQuote()
        {
            var cropOwner = EnterShelter(ShowerTick);
            var crop = cropOwner.GetPart<CropPart>();
            var guide = GuideOwner();
            var flask = Flask(2);
            Assert.True(CropTime.Reconcile(crop, Zone, Clock.TickCount));
            var row = GuideRows().Single();
            var render = cropOwner.GetPart<RenderPart>();
            string background = render.BackgroundColor;
            int stagedMoisture = -1;
            var failure = new ThrowAfterGuide(() => stagedMoisture = crop.MoistureTicks);
            Actor.AddPart(failure);
            try
            {
                Assert.False(ChooseRow(row, 51, 9));
                Assert.That(stagedMoisture, Is.EqualTo(CropWateringService.MoisturePerUnit));
                Assert.That(crop.MoistureTicks, Is.Zero);
                Assert.That(render.BackgroundColor, Is.EqualTo(background));
                Assert.That(Units(flask), Is.EqualTo(2));
                Assert.That(Clock.TickCount, Is.EqualTo(ShowerTick));
                Assert.That(Zone.GetEntityCell(guide), Is.SameAs(Zone.GetCell(51, 9)));
            }
            finally { Actor.RemovePart(failure); }

            Assert.True(ChooseRow(row, 51, 9));
            Assert.That(crop.MoistureTicks, Is.EqualTo(CropWateringService.MoisturePerUnit));
            Assert.That(Units(flask), Is.EqualTo(2));
        }

        [Test]
        public void LateGrowthChangeKeepsItsStageAndReturnsTheUncommittedRain()
        {
            var cropOwner = EnterShelter(ShowerTick);
            var crop = cropOwner.GetPart<CropPart>();
            var catcher = GuideOwner().GetPart<RainCatchmentPart>();
            int priorWindow = catcher.LastWindow;
            int shower = Weather.Window;
            var row = GuideRows().Single();
            int stagedMoisture = -1;
            int stagedSpent = -1;
            var failure = new ThrowAfterGuide(() =>
            {
                stagedMoisture = crop.MoistureTicks;
                stagedSpent = catcher.Spent;
                crop.GrowthStage = 1;
                crop.TicksInStage = 3;
            });
            Actor.AddPart(failure);
            try { Assert.False(ChooseRow(row, 51, 9)); }
            finally { Actor.RemovePart(failure); }

            Assert.That(stagedMoisture, Is.EqualTo(CropWateringService.MoisturePerUnit));
            Assert.That(stagedSpent, Is.EqualTo(1));
            Assert.That(cropOwner.GetPart<CropPart>(), Is.SameAs(crop));
            Assert.That(crop.GrowthStage, Is.EqualTo(1));
            Assert.That(crop.TicksInStage, Is.EqualTo(3));
            Assert.That(crop.MoistureTicks, Is.Zero);
            Assert.That(catcher.Spent, Is.Zero);
            Assert.That(catcher.LastWindow, Is.EqualTo(priorWindow));
            Assert.That(Weather.Window, Is.EqualTo(shower));
            Assert.True(ChooseRow(GuideRows().Single(), 51, 9));
            Assert.That(crop.MoistureTicks, Is.EqualTo(CropWateringService.MoisturePerUnit));
            Assert.That(catcher.Spent, Is.EqualTo(1));
        }

        [Test]
        public void LateCropPartReplacementKeepsItsOwnWaterAndReturnsTheUncommittedRain()
        {
            var cropOwner = EnterShelter(ShowerTick);
            var crop = cropOwner.GetPart<CropPart>();
            var guide = GuideOwner();
            var catcher = guide.GetPart<RainCatchmentPart>();
            int priorWindow = catcher.LastWindow;
            var row = GuideRows().Single();
            var donor = Factory.CreateEntity("SumpsieveCrop");
            var replacement = donor.GetPart<CropPart>();
            donor.RemovePart(replacement);
            int stagedMoisture = -1;
            var failure = new ThrowAfterGuide(() =>
            {
                stagedMoisture = crop.MoistureTicks;
                cropOwner.RemovePart(crop);
                cropOwner.AddPart(replacement);
                replacement.Water(7);
            });
            Actor.AddPart(failure);
            try { Assert.False(ChooseRow(row, 51, 9)); }
            finally { Actor.RemovePart(failure); }

            Assert.That(stagedMoisture, Is.EqualTo(CropWateringService.MoisturePerUnit));
            Assert.That(cropOwner.GetPart<CropPart>(), Is.SameAs(replacement));
            Assert.That(replacement.ParentEntity, Is.SameAs(cropOwner));
            Assert.That(replacement.MoistureTicks, Is.EqualTo(7));
            Assert.That(cropOwner.GetPart<RenderPart>().BackgroundColor, Is.EqualTo(CropPart.WET_SOIL_BG));
            Assert.That(guide.GetPart<RainCatchmentPart>(), Is.SameAs(catcher));
            Assert.That(catcher.Spent, Is.Zero);
            Assert.That(catcher.LastWindow, Is.EqualTo(priorWindow));
        }

        [Test]
        public void ReloadKeepsTheGuideAndItsExactBedBoundToTheSpentShower()
        {
            var crop = EnterShelter(ShowerTick);
            var originalZone = Zone;
            var manager = WorldLocationContext.For(Zone);
            var guide = GuideOwner();
            var soil = Zone.GetCell(50, 9).Objects.Single(e => e.HasPart<CultivatedSoilPart>());
            var ground = Zone.GetCell(51, 9).Objects.Single(e => e.HasTag("Terrain"));
            var row = GuideRows().Single();
            int shower = Weather.Window;
            Assert.True(ChooseRow(row, 51, 9));

            using var isolation = new HotbarSaveFixture(false, false);
            using var bytes = new MemoryStream();
            GameSessionState.Capture("sodden-rain-guide", "used shower", manager, Clock, Actor).Save(new SaveWriter(bytes));
            bytes.Position = 0;
            var loaded = GameSessionState.Load(new SaveReader(bytes, Factory));
            Zone = loaded.ZoneManager.ActiveZone;
            Actor = loaded.Player;
            Clock = loaded.TurnManager;
            SettlementRuntime.ActiveZone = Zone;
            Assert.That(Zone, Is.Not.SameAs(originalZone));
            Assert.That(Clock.TickCount, Is.EqualTo(ShowerTick));

            var savedGuide = GuideOwner();
            var savedPart = savedGuide.GetPart<SoddenRainGuidePart>();
            var savedCatcher = savedGuide.GetPart<RainCatchmentPart>();
            var savedCrop = Zone.GetCell(50, 9).Objects.Single(e => e.ID == crop.ID);
            var savedSoil = Zone.GetCell(50, 9).Objects.Single(e => e.ID == soil.ID);
            var savedGround = Zone.GetCell(51, 9).Objects.Single(e => e.ID == ground.ID);
            Assert.That(savedGuide.ID, Is.EqualTo(guide.ID));
            Assert.That(savedPart.Crop, Is.SameAs(savedCrop));
            Assert.That(savedPart.Soil, Is.SameAs(savedSoil));
            Assert.That(savedCatcher.ParentEntity, Is.SameAs(savedGuide));
            Assert.That(savedCatcher.Ground, Is.SameAs(savedGround));
            Assert.That(savedCatcher.LastWindow, Is.EqualTo(shower));
            Assert.That(savedCatcher.Spent, Is.EqualTo(1));
            var savedGrowth = savedCrop.GetPart<CropPart>();
            Assert.That(savedGrowth.MoistureTicks, Is.EqualTo(CropWateringService.MoisturePerUnit));

            Clock.AdvanceClock(CropWateringService.MoisturePerUnit * CropTime.WorldTicksPerUnit);
            Assert.True(CropTime.Reconcile(savedGrowth, Zone, Clock.TickCount));
            Assert.That(Weather.Window, Is.EqualTo(shower));
            Assert.That(savedGrowth.GrowthStage, Is.EqualTo(1));
            Assert.That(savedGrowth.MoistureTicks, Is.Zero);
            Assert.That(GuideRows(), Is.Empty);
            Assert.False(Act(savedGuide, row.Command));
            Assert.That(savedCatcher.LastWindow, Is.EqualTo(shower));
            Assert.That(savedCatcher.Spent, Is.EqualTo(1));
            Assert.That(savedGrowth.MoistureTicks, Is.Zero);
        }

        [Test]
        public void ColdShelterOmitsAnInvisibleGuideWithoutLosingItsCrops()
        {
            Factory.Blueprints[Guide].Parts["Render"]["Visible"] = "false";
            var crop = EnterShelter(ShowerTick);
            Assert.That(Zone.GetReadOnlyEntities().Count(e => e.BlueprintName == Guide || e.HasPart<RainCatchmentPart>()), Is.Zero);
            Assert.That(Zone.GetCell(51, 9).Objects.Single().BlueprintName, Is.EqualTo("Grass"));
            Assert.That(Zone.GetReadOnlyEntities().Count(e => e.BlueprintName == "SoddenDressingBench"), Is.EqualTo(1));
            Assert.That(Zone.GetReadOnlyEntities().Count(e => e.BlueprintName == "PeatCutter"), Is.EqualTo(1));
            var ripe = Zone.GetCell(47, 9).Objects.Single(e => e.BlueprintName == "SumpsieveCrop").GetPart<CropPart>();
            Assert.That(ripe.GrowthStage, Is.EqualTo(2));
            Assert.That(GuideRows(), Is.Empty);

            var flask = Flask(2);
            Assert.True(ChooseRow(ManualRow(flask), 50, 9));
            Assert.That(Units(flask), Is.EqualTo(1));
            Assert.That(crop.GetPart<CropPart>().MoistureTicks, Is.EqualTo(CropWateringService.MoisturePerUnit));
            Assert.That(ripe.GrowthStage, Is.EqualTo(2));
        }

        private sealed class ThrowAfterGuide : Part
        {
            private readonly Action observe;

            public ThrowAfterGuide(Action observe) => this.observe = observe;

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "AfterInventoryAction" && e.GetStringParameter("Command")?.StartsWith(Prefix, StringComparison.Ordinal) == true)
                {
                    observe();
                    throw new InvalidOperationException("rain guide rollback probe");
                }
                return true;
            }
        }

        private Entity EnterShelter(int tick, int version = 26)
        {
            Assert.True(Zone.RemoveEntity(Actor));
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            if (version == 25) SoddenDistrictIntegrationTests.Version(manager, 25);
            Assert.That(manager.Exploration.Version,
                Is.EqualTo(version == 25 ? 25 : SpreadExplorationPlan.CurrentVersion));
            Assert.False(manager.CachedZones.ContainsKey(SoddenDistrictPlan.StopZoneID));
            Clock.AdvanceClock(tick);
            Zone = manager.GetZone(SoddenDistrictPlan.StopZoneID);
            Assert.That(Zone, Is.Not.Null);
            manager.SetActiveZone(Zone);
            SettlementRuntime.ActiveZone = Zone;
            Assert.True(Zone.AddEntity(Actor, 50, 10));
            for (int x = 47; x <= 51; x++)
            for (int y = 9; y <= 10; y++)
            {
                Zone.GetCell(x, y).IsVisible = true;
                Zone.GetCell(x, y).Explored = true;
            }
            var cell = Zone.GetCell(50, 9);
            Assert.False(cell.IsInterior);
            Assert.True(CultivatedSoilPart.IsCultivated(Zone, cell));
            var owner = cell.Objects.Single(e => e.BlueprintName == "SumpsieveCrop");
            Assert.That(owner.GetProperty(SoddenDistrictBuilder.RoleKey), Is.EqualTo("dry-sumpsieve"));
            Assert.That(owner.GetPart<CropPart>().GrowthStage, Is.Zero);
            Assert.That(owner.GetPart<CropPart>().MoistureTicks, Is.Zero);
            Assert.True(Weather.Valid);
            return owner;
        }

        private RegionalWeather.Observation Weather => RegionalWeather.Current(Zone, WorldClock.CurrentTick, RegionalWeather.PolicyVersion);

        private Entity GuideOwner()
        {
            var guide = Zone.GetCell(51, 9).Objects.SingleOrDefault(e => e.BlueprintName == Guide);
            Assert.That(guide, Is.Not.Null, "Fresh Sodden generation should place the guide beside this crop.");
            Assert.That(WorldLocationContext.For(Zone).Exploration.Version, Is.EqualTo(26));
            return guide;
        }

        private InventoryAction[] GuideRows() => CellMenu(51, 9).Where(a => !a.IsDisabled
            && a.Command?.StartsWith(Prefix, StringComparison.Ordinal) == true).ToArray();

        private InventoryAction ManualRow(Entity flask) => CellMenu(50, 9)
            .Single(a => a.Command == "WaterCrop|" + Id(flask));
    }
}
