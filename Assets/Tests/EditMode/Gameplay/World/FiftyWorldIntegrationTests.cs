using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class FiftyWorldIntegrationTests : FiftyWorldFixture
    {
        [TearDown] public void ClearProbe() => FiftyWorldCreationProbe.Created = null;
        [Test] public void FieldCreationCallbackCannotHarvestTheSameRowTwice()
        {
            var row = Place("RipeCropRow"); bool? nested = null;
            Factory.RegisterPartType<FiftyWorldCreationProbe>();
            Factory.Blueprints["Emberwheat"].Parts["FiftyWorldCreationProbe"] = new Dictionary<string, string>();
            FiftyWorldCreationProbe.Created = _ => { if (nested == null) { nested = false; nested = Act(row, "Harvest"); } };
            Assert.True(Act(row, "Harvest")); Assert.AreEqual(false, nested); Assert.AreEqual(1, Count("Emberwheat"));
        }
        [Test] public void MalformedCreatedGrainLeavesTheRowUncutAndNoPhysicalOutput()
        {
            var row = Place("RipeCropRow"); Factory.RegisterPartType<FiftyWorldCreationProbe>();
            Factory.Blueprints["Emberwheat"].Parts["FiftyWorldCreationProbe"] = new Dictionary<string, string>();
            FiftyWorldCreationProbe.Created = item => item.GetPart<PhysicsPart>().Takeable = false;
            Assert.False(Act(row, "Harvest")); Assert.False(row.GetPart<FieldHarvestPart>().Harvested); Assert.AreEqual(0, Count("Emberwheat"));
        }
        [Test] public void DuplicateDestinationIdentityCannotRedirectWater()
        {
            var from = Flask(); var to = Skin(); var alias = Flask(0); alias.ID = to.ID;
            Assert.False(Act(from, "TransferWater|" + Id(to))); Assert.AreEqual(3, Units(from)); Assert.AreEqual(0, Units(to)); Assert.AreEqual(0, Units(alias));
        }
        [Test] public void BrowsingAllNewResourceChoicesDoesNotReconcileOrConsumeState()
        {
            var crop = Crop(moisture: 2); var water = Flask(); var coals = Place("SpreadCookingCoals", 10, 11);
            Clock.AdvanceClock(9); var part = crop.GetPart<CropPart>(); int tick = part.LastGrowthWorldTick, remainder = part.GrowthWetTickRemainder;
            for (int i = 0; i < 3; i++) { Actions(crop); Actions(water); Actions(coals); }
            Assert.AreEqual(tick, part.LastGrowthWorldTick); Assert.AreEqual(remainder, part.GrowthWetTickRemainder); Assert.AreEqual(2, part.MoistureTicks);
            Assert.AreEqual(3, Units(water)); Assert.AreEqual(25, coals.GetPart<FuelPart>().FuelMass); Assert.AreEqual(0, PoolUnits());
        }
        [Test] public void ActualHeatThawsTheSameFiniteSourceBeforeDrawing()
        {
            var source = Place("WaterPuddle"); source.GetPart<ThermalPart>().Temperature = -10; source.ApplyEffect(new FrozenEffect(0.1f));
            var vessel = Skin(); Assert.False(Act(vessel, "FillWaterskin")); int before = source.GetPart<LiquidPoolPart>().Volume;
            var heat = GameEvent.New("ApplyHeat"); heat.SetParameter("Joules", (object)60f); heat.SetParameter("Zone", (object)Zone); source.FireEventAndRelease(heat);
            Assert.False(source.GetEffect<FrozenEffect>()?.Cold > 0); Assert.True(Act(vessel, "FillWaterskin"));
            Assert.AreEqual(before, Units(vessel) + source.GetPart<LiquidPoolPart>().Volume);
        }
        [Test] public void ReplacementSessionPreservesPreparedStubbleIrrigatedCropAndSpentSupplies()
        {
            Zone.RemoveEntity(Actor); Zone = new Zone("Overworld.10.10.0"); SettlementRuntime.ActiveZone = Zone; Assert.True(Zone.AddEntity(Actor, 10, 10));
            var row = Place("RipeCropRow"); Assert.True(Act(row, "Harvest")); Assert.True(Act(row, "PrepareFieldBed"));
            var seed = Seed(); Assert.True(Act(seed, Choice(seed, "PlantSeedAt|", a => a.Command.EndsWith("|11|10"))));
            var crop = Zone.GetCell(11, 10).Objects.Single(e => e.HasPart<CropPart>()); var water = Flask(3); var skin = Skin(0, 2);
            Assert.True(Act(water, "TransferWater|" + Id(skin))); Assert.True(Act(water, Pour(water, true)));
            var fire = Place("SpreadCookingCoals", 10, 11); fire.GetPart<FuelPart>().FuelMass = 0; fire.GetPart<ThermalPart>().Temperature = 25;
            Carry("SalvagedTimber"); Assert.True(Act(fire, "FeedCookingFire"));
            var manager = OverworldZoneManager.CreateDetached(Factory, 64);
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { Zone.ZoneID, Zone } }, Zone.ZoneID, new Dictionary<string, List<ZoneConnection>>());
            Clock.RestoreSavedState(0, true, Actor, new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = Actor, Energy = 1000 } });
            var loaded = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("fifty-world", "controlled-resource-save", manager, Clock, Actor));
            var zone = loaded.ZoneManager.ActiveZone; var loadedRow = zone.GetReadOnlyEntities().Single(e => e.ID == row.ID);
            Assert.AreNotSame(row, loadedRow); Assert.True(loadedRow.GetPart<FieldHarvestPart>().Harvested); Assert.True(loadedRow.HasPart<CultivatedSoilPart>());
            Assert.AreEqual(40, zone.GetReadOnlyEntities().Single(e => e.ID == crop.ID).GetPart<CropPart>().MoistureTicks);
            Assert.False(zone.GetCell(11, 10).Objects.Any(e => e.HasPart<LiquidPoolPart>()));
            var pack = loaded.Player.GetPart<InventoryPart>(); Assert.AreEqual(2, pack.Objects.Where(e => e.HasPart<WaterskinPart>() || e.HasPart<LiquidVesselPart>()).Sum(Units));
            Assert.False(pack.Objects.Any(e => e.BlueprintName == "SalvagedTimber"));
            Assert.AreEqual(10, zone.GetReadOnlyEntities().Single(e => e.ID == fire.ID).GetPart<FuelPart>().FuelMass);
            Assert.AreEqual(25, zone.GetReadOnlyEntities().Single(e => e.ID == fire.ID).GetPart<ThermalPart>().Temperature);
            Assert.False(InventorySystem.PerformAction(loaded.Player, loadedRow, "Harvest", zone));
        }
    }
    public sealed class FiftyWorldCreationProbe : Part
    {
        public static Action<Entity> Created;
        public override void Initialize() => Created?.Invoke(ParentEntity);
    }
}
