using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class FiftyWorldEnvironmentTests : FiftyWorldFixture
    {
        Entity BurningProp(int x = 11, int y = 10)
        {
            var prop = Place("PhysicalObject", x, y); prop.AddPart(new ThermalPart { Temperature = 450, FlameTemperature = 300 });
            prop.AddPart(new FuelPart { FuelMass = 20, MaxFuel = 20 }); prop.AddPart(new MaterialPart { MaterialTagsRaw = "Wood,Flammable" }); Assert.True(prop.ApplyEffect(new BurningEffect(1)), "The fixture must actually be burning."); return prop;
        }
        Entity WetGear()
        { var gear = Factory.CreateEntity("PhysicalObject"); gear.GetPart<PhysicsPart>().Takeable = true; Assert.True(Pack.AddObject(gear)); gear.ApplyEffect(new WetEffect(0.5f)); return gear; }
        Entity ColdCoals()
        { var coals = Place("SpreadCookingCoals"); coals.GetPart<FuelPart>().FuelMass = 0; coals.GetPart<ThermalPart>().Temperature = 25; return coals; }
        Entity HeldTorch(bool equip = true)
        { var torch = Carry("Torch"); if (equip) { Assert.True(InventorySystem.Equip(Actor, torch)); Assert.NotNull(Pack.FindEquippedBodyPart(torch)); } return torch; }

        [Test] public void OneWaterUnitDousesOnlyTheSelectedActualBurningProp()
        {
            var prop = BurningProp(); var other = BurningProp(10, 11); var water = Skin(3);
            Assert.True(Act(water, Choice(water, "DouseWorld|", a => a.Command == "DouseWorld|" + Id(prop))));
            Assert.AreEqual(2, Units(water)); Assert.False(prop.HasEffect<BurningEffect>()); Assert.Less(prop.GetPart<ThermalPart>().Temperature, 300);
            Assert.True(other.HasEffect<BurningEffect>()); Assert.AreEqual(20, prop.GetPart<FuelPart>().FuelMass);
            Assert.False(Act(water, "DouseWorld|" + Id(prop))); Assert.AreEqual(2, Units(water));
        }
        [TestCase("acid")] [TestCase("remote")] [TestCase("creature")]
        public void DousingRefusesNonwaterRemoteAndCreatureTargets(string refusal)
        {
            var prop = BurningProp(); var water = Flask(3, refusal == "acid" ? "acid" : "water");
            if (refusal == "remote") Zone.MoveEntity(prop, 20, 10); if (refusal == "creature") prop.SetTag("Creature");
            Assert.False(Act(water, "DouseWorld|" + Id(prop))); Assert.AreEqual(3, Units(water)); Assert.True(prop.HasEffect<BurningEffect>());
        }
        [Test] public void FailedDousingRestoresTheOriginalFlameAndWater()
        {
            var prop = BurningProp(); var burn = prop.GetEffect<BurningEffect>(); float heat = prop.GetPart<ThermalPart>().Temperature; var water = Skin(3); FailAfter();
            Assert.False(Act(water, "DouseWorld|" + Id(prop))); Assert.AreEqual(3, Units(water)); Assert.AreSame(burn, prop.GetEffect<BurningEffect>()); Assert.AreEqual(heat, prop.GetPart<ThermalPart>().Temperature);
        }
        [Test] public void EachFireDryingActionRemovesOnlyAQuarterOfTheSelectedGearsMoisture()
        {
            var fire = Place("SpreadCookingCoals"); var gear = WetGear(); string command = Choice(fire, "DryGear|", a => a.Command == "DryGear|" + Id(gear));
            Assert.True(Act(fire, command)); Assert.AreEqual(0.25f, gear.GetEffect<WetEffect>().Moisture, 0.00001f);
            Assert.True(Act(fire, command)); Assert.False(gear.HasEffect<WetEffect>()); Assert.False(Act(fire, command));
        }
        [TestCase("cold")] [TestCase("spent")] [TestCase("frozen")] [TestCase("foreign")]
        public void DryingRequiresActualHeatFuelAndOwnedUnfrozenGear(string refusal)
        {
            var fire = Place("SpreadCookingCoals"); var gear = WetGear();
            if (refusal == "cold") fire.GetPart<ThermalPart>().Temperature = 25;
            if (refusal == "spent") fire.GetPart<FuelPart>().FuelMass = 0;
            if (refusal == "frozen") { gear.AddPart(new MaterialPart { MaterialTagsRaw = "Metal" }); Assert.True(gear.ApplyEffect(new FrozenEffect(1))); }
            if (refusal == "foreign") Pack.RemoveObject(gear);
            Assert.False(Act(fire, "DryGear|" + Id(gear))); Assert.AreEqual(0.5f, gear.GetEffect<WetEffect>().Moisture);
        }
        [Test] public void DryingJoinsOuterRollback()
        { var fire = Place("SpreadCookingCoals"); var gear = WetGear(); FailAfter(); Assert.False(Act(fire, "DryGear|" + Id(gear))); Assert.AreEqual(0.5f, gear.GetEffect<WetEffect>().Moisture); }
        [Test] public void TimberFeedsColdCoalsButActualFlameIsStillRequiredToRelight()
        {
            var fire = ColdCoals(); Carry("SalvagedTimber"); Assert.True(Act(fire, Choice(fire, "FeedCookingFire")));
            Assert.AreEqual(0, Count("SalvagedTimber")); Assert.AreEqual(10, fire.GetPart<FuelPart>().FuelMass); Assert.AreEqual(25, fire.GetPart<ThermalPart>().Temperature);
            Assert.False(Act(fire, "RelightCookingFire")); HeldTorch(); Assert.True(Act(fire, Choice(fire, "RelightCookingFire")));
            Assert.AreEqual(450, fire.GetPart<ThermalPart>().Temperature); Assert.AreEqual(10, fire.GetPart<FuelPart>().FuelMass);
            Assert.False(fire.GetPart<CampfirePart>().AllowRest); var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(fire);
            Assert.AreEqual(10, copy.GetPart<FuelPart>().FuelMass); Assert.False(copy.GetPart<CampfirePart>().AllowRest);
        }
        [TestCase("no-timber")] [TestCase("full")] [TestCase("ordinary")]
        public void FeedingIsOnlyForActualFiniteCoalsWithRoomAndCarriedTimber(string refusal)
        {
            var fire = refusal == "ordinary" ? Place("Campfire") : ColdCoals(); if (refusal != "no-timber") Carry("SalvagedTimber");
            if (refusal == "full") fire.GetPart<FuelPart>().FuelMass = fire.GetPart<FuelPart>().MaxFuel;
            float fuel = fire.GetPart<FuelPart>()?.FuelMass ?? 0; int timber = Count("SalvagedTimber");
            Assert.False(Act(fire, "FeedCookingFire")); Assert.AreEqual(fuel, fire.GetPart<FuelPart>()?.FuelMass ?? 0); Assert.AreEqual(timber, Count("SalvagedTimber"));
        }
        [Test] public void FeedingRollbackRestoresTimberAndFuel()
        { var fire = ColdCoals(); Carry("SalvagedTimber"); FailAfter(); Assert.False(Act(fire, "FeedCookingFire")); Assert.AreEqual(1, Count("SalvagedTimber")); Assert.AreEqual(0, fire.GetPart<FuelPart>().FuelMass); }
        [Test] public void ARealEquippedTorchCanPassFireToAnAdjacentLooseTorch()
        {
            var held = HeldTorch(); var spare = Place("Torch"); Assert.True(Act(spare, "ExtinguishTorch"));
            Assert.True(Act(spare, "LightTorch")); Assert.True(spare.GetPart<ThermalPart>().IsAflame); Assert.AreEqual(50, held.GetPart<FuelPart>().FuelMass);
        }
        [TestCase("stowed")] [TestCase("off")] [TestCase("spent")]
        public void StowedExtinguishedOrExhaustedTorchesCannotDonateFire(string state)
        {
            var held = HeldTorch(state != "stowed"); var spare = Place("Torch"); Assert.True(Act(spare, "ExtinguishTorch"));
            if (state == "off") Assert.True(Act(held, "ExtinguishTorch")); if (state == "spent") held.GetPart<FuelPart>().FuelMass = 0;
            Assert.False(Act(spare, "LightTorch")); Assert.False(spare.GetPart<ThermalPart>().IsAflame);
        }
    }
}
