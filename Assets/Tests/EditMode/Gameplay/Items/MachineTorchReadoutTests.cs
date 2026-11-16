using System;
using System.Globalization;
using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class MachineTorchReadoutTests
    {
        EntityFactory factory; Entity actor, torch; InventoryPart pack; Zone zone;
        [SetUp] public void Setup()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            actor = new Entity { ID = "torch-reader-player" }; actor.SetTag("Player");
            pack = new InventoryPart(); actor.AddPart(pack); actor.AddPart(new PhysicsPart()); actor.AddPart(new StatusEffectsPart());
            actor.Statistics["Hitpoints"] = new Stat { Owner = actor, Name = "Hitpoints", BaseValue = 20, Max = 20 };
            zone = new Zone("TorchReadout"); zone.AddEntity(actor, 10, 10);
            torch = factory.CreateEntity("Torch"); Assert.True(pack.AddObject(torch));
        }
        string Read() => torch.GetPart<ExaminablePart>().BuildExamineLine();
        void Hold() => Assert.True(pack.Equip(torch, "Hand"));
        void Tick() { var e = GameEvent.New("EndTurn"); e.SetParameter("Zone", (object)zone); actor.FireEventAndRelease(e); }

        [Test] public void ActualHeldTick_IsVisibleWithoutEstimatedLifetime()
        {
            Hold(); Tick();
            StringAssert.Contains("Fuel: 49.7/50 per torch", Read());
            StringAssert.Contains("Controlled flame: Lit", Read());
            StringAssert.DoesNotContain("turns remaining", Read());
            StringAssert.Contains("extinguish", Read()); StringAssert.Contains("unequip", Read());
            StringAssert.Contains("25 fuel", Read());
        }
        [Test] public void OrdinaryExtinguish_UpdatesReaderWithoutConsumingFuel()
        {
            Hold(); Tick(); Assert.True(InventorySystem.PerformAction(actor, torch, "ExtinguishTorch", zone));
            StringAssert.Contains("Controlled flame: Out", Read());
            StringAssert.Contains("Fuel: 49.7/50 per torch", Read());
        }
        [Test] public void StowedStack_ReportsPerTorchAndExplainsSuspendedConsumption()
        {
            torch.GetPart<StackerPart>().StackCount = 4; Tick();
            StringAssert.Contains("Fuel: 50/50 per torch", Read());
            StringAssert.Contains("stowed torches retain their state", Read());
            StringAssert.DoesNotContain("200", Read());
        }
        [Test] public void LooseWorldReader_UsesSameLiveFuel()
        {
            pack.RemoveObject(torch); zone.AddEntity(torch, 11, 10); MaterialSimSystem.TickMaterialEntities(zone);
            var text = torch.GetPart<ExaminablePart>().BuildWorldExamineLine(zone, zone.GetCell(11, 10), actor);
            StringAssert.Contains("Fuel: 49.7/50 per torch", text);
        }
        [TestCase(0.35f, true)] [TestCase(0.36f, false)]
        public void WetOwner_UsesActualSuppressionThreshold(float moisture, bool lit)
        {
            Hold(); torch.ApplyEffect(new WetEffect(moisture));
            StringAssert.Contains(lit ? "Controlled flame: Lit" : "Controlled flame: Out (wet)", Read());
        }
        [Test] public void WetBearer_DoesNotInventWetTorch()
        { Hold(); actor.ApplyEffect(new WetEffect(1)); StringAssert.Contains("Controlled flame: Lit", Read()); }
        [Test] public void FrozenOwner_IsDisclosed()
        { Hold(); torch.ApplyEffect(new FrozenEffect()); StringAssert.Contains("Controlled flame: Out (frozen)", Read()); }
        [Test] public void EmptyFuel_IsDisclosedWithoutMutatingEnabledLight()
        {
            Hold(); torch.GetPart<FuelPart>().FuelMass = 0;
            StringAssert.Contains("Fuel: 0/50 per torch", Read()); StringAssert.Contains("Controlled flame: Out (no fuel)", Read());
            Assert.True(torch.GetPart<LightSourcePart>().Enabled);
        }
        [Test] public void TinyPositiveFuel_IsNotRoundedToZero()
        { torch.GetPart<FuelPart>().FuelMass = 0.0001f; StringAssert.Contains("Fuel: 0.0001/50 per torch", Read()); }
        [Test] public void AccumulatedTickNoise_DoesNotLeakIntoDisplayedFuel()
        { torch.GetPart<FuelPart>().FuelMass = 47.0000076f; StringAssert.Contains("Fuel: 47/50 per torch", Read()); }
        [Test] public void LiveCapacityAndInvariantDecimals_AreUsed()
        {
            var before = CultureInfo.CurrentCulture;
            try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR"); torch.GetPart<FuelPart>().MaxFuel = 80; torch.GetPart<FuelPart>().FuelMass = 12.5f;
                StringAssert.Contains("Fuel: 12.5/80 per torch", Read()); }
            finally { CultureInfo.CurrentCulture = before; }
        }
        [TestCase("Fuel")] [TestCase("Thermal")] [TestCase("LightSource")]
        public void MissingRequiredPart_DoesNotInventReading(string name)
        { torch.RemovePart(torch.GetPart(name)); StringAssert.Contains("Torch reading unavailable", Read()); }
        [TestCase("Fuel")] [TestCase("Thermal")] [TestCase("LightSource")] [TestCase("TorchLight")]
        public void ForeignPart_DoesNotBorrowState(string name)
        { torch.GetPart(name).ParentEntity = new Entity(); StringAssert.Contains("Torch reading unavailable", Read()); }
        [TestCase("negative")] [TestCase("overfull")] [TestCase("nan")] [TestCase("infinite")]
        [TestCase("capacity")] [TestCase("rate")] [TestCase("temperature")] [TestCase("flame")]
        public void MalformedState_ReportsUnavailableWithoutRepair(string fault)
        {
            var fuel = torch.GetPart<FuelPart>(); var heat = torch.GetPart<ThermalPart>();
            switch (fault) {
                case "negative": fuel.FuelMass = -1; break; case "overfull": fuel.FuelMass = 51; break;
                case "nan": fuel.FuelMass = float.NaN; break; case "infinite": fuel.MaxFuel = float.PositiveInfinity; break;
                case "capacity": fuel.MaxFuel = 0; break; case "rate": fuel.BurnRate = float.NaN; break;
                case "temperature": heat.Temperature = float.NaN; break; case "flame": heat.FlameTemperature = float.PositiveInfinity; break;
            }
            StringAssert.Contains("Torch reading unavailable", Read());
        }
        [Test] public void OtherFuelOwners_DoNotGainTorchInstructions()
        {
            var item = factory.CreateEntity("Dagger"); item.AddPart(new FuelPart());
            StringAssert.DoesNotContain("Fuel:", item.GetPart<ExaminablePart>().BuildExamineLine());
            StringAssert.DoesNotContain("lamp oil", item.GetPart<ExaminablePart>().BuildExamineLine());
        }
        [Test] public void ControlledOut_PreservesSeparateDestructiveFireAffliction()
        {
            Hold(); Assert.True(InventorySystem.PerformAction(actor, torch, "ExtinguishTorch", zone));
            torch.ApplyEffect(new BurningEffect(1, null, new System.Random(1)));
            StringAssert.Contains("Controlled flame: Out", Read());
            StringAssert.Contains("Set ablaze - burning", Read());
            Assert.True(torch.HasEffect<BurningEffect>());
        }
        [Test] public void RepeatedRead_PreservesFuelHeatLightEffectsCustodyAndTime()
        {
            Hold(); Tick(); var wet = new WetEffect(0.5f); torch.ApplyEffect(wet);
            var fuel = torch.GetPart<FuelPart>(); var heat = torch.GetPart<ThermalPart>(); var light = torch.GetPart<LightSourcePart>();
            float f = fuel.FuelMass, t = heat.Temperature; bool enabled = light.Enabled; long? tick = TurnManager.Active?.TickCount;
            for (int i = 0; i < 20; i++) Read();
            Assert.AreEqual(f, fuel.FuelMass); Assert.AreEqual(t, heat.Temperature); Assert.AreEqual(enabled, light.Enabled);
            Assert.AreSame(wet, torch.GetEffect<WetEffect>()); Assert.AreSame(torch, pack.EquippedItems["Hand"]);
            Assert.False(pack.Objects.Contains(torch)); Assert.AreEqual(tick, TurnManager.Active?.TickCount);
        }
        [Test] public void SavedFuelAndOutState_ProduceSameReaderAfterRoundtrip()
        {
            Hold(); Tick(); Assert.True(InventorySystem.PerformAction(actor, torch, "ExtinguishTorch", zone));
            var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var saved = copy.GetPart<InventoryPart>().EquippedItems["Hand"];
            Assert.AreEqual(Read(), saved.GetPart<ExaminablePart>().BuildExamineLine());
            StringAssert.Contains("Fuel: 49.7/50 per torch", saved.GetPart<ExaminablePart>().BuildExamineLine());
        }
    }
}
