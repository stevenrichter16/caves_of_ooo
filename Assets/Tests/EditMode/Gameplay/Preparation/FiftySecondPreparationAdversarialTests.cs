using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class FiftySecondPreparationAdversarialTests : FiftySecondPreparationFixture
    {
        [TestCase("no-forge")] [TestCase("equipped")] [TestCase("wrong-head")] [TestCase("unknown-haft")]
        public void SalvageRefusalPreservesExactWeaponAndCannotMintComponents(string refusal)
        {
            if (refusal != "no-forge") Forge(); var weapon = Weapon(); var p = weapon.GetPart<WeaponAssemblyPart>();
            if (refusal == "equipped") Assert.True(InventorySystem.Equip(Actor, weapon));
            if (refusal == "wrong-head") p.BladeBlueprint = "LeatherBindingComponent";
            if (refusal == "unknown-haft") p.HaftBlueprint = "not-an-authored-component";
            Assert.False(Act(weapon, "SalvageForgedWeapon")); Assert.AreEqual(0, PackCount("SteelBladeComponent")); Assert.AreEqual(0, PackCount("OakHaftComponent"));
            Assert.True(Pack.Contains(weapon) || InventorySystem.IsEquipped(Actor, weapon));
        }
        [Test] public void SalvageAfterEventExceptionRestoresPaymentAndRemovesBothOutputs()
        {
            Forge(); var weapon = Weapon(); FailAfter(); Assert.False(Act(weapon, "SalvageForgedWeapon"));
            Assert.True(Pack.Contains(weapon)); Assert.AreSame(Actor, weapon.GetPart<PhysicsPart>().InInventory); Assert.AreEqual(0, PackCount("SteelBladeComponent")); Assert.AreEqual(0, PackCount("OakHaftComponent"));
        }
        [Test] public void SalvagingOneStackUnitLeavesSecondAssemblyPayloadIntact()
        {
            Forge(); var weapon = Weapon(); weapon.GetPart<StackerPart>().StackCount = 2;
            var assembly = weapon.GetPart<WeaponAssemblyPart>(); var blade = assembly.BladeBlueprint; Assert.True(Act(weapon, "SalvageForgedWeapon"));
            Assert.AreEqual(1, PackCount("ForgedWeapon")); Assert.AreEqual(1, PackCount("SteelBladeComponent"));
            Assert.AreEqual(blade, Packed("ForgedWeapon").GetPart<WeaponAssemblyPart>().BladeBlueprint);
        }
        [TestCase("foreign")] [TestCase("wrong-material")] [TestCase("zero")]
        public void PreparationRefusesInvalidInputWithoutOutputOrConsumption(string refusal)
        {
            var input = Stock(refusal == "wrong-material" ? "LampOil" : "SalvagedTimber", 2);
            if (refusal == "foreign") input.GetPart<PhysicsPart>().InInventory = Factory.CreateEntity("Player");
            if (refusal == "zero") input.GetPart<StackerPart>().StackCount = 0;
            int units = input.GetPart<StackerPart>().StackCount; Assert.False(Prepare(input, "shape_haft"));
            Assert.AreEqual(units, input.GetPart<StackerPart>().StackCount); Assert.AreEqual(0, PackCount("FieldHaftComponent"));
        }
        [TestCase("shape_haft", "SalvagedTimber", 1, "FieldHaftComponent")]
        [TestCase("concentrate_mendleaf", "MendleafSprig", 2, "ConcentratedMendleaf")]
        [TestCase("detox_grove_red", "GroveRed", 1, "CleansedGrovePulp")]
        public void OuterFailureRollsPreparationBack(string recipe, string input, int units, string output)
        {
            Still(); var raw = Stock(input, units); FailAfter(); Assert.False(Prepare(raw, recipe));
            Assert.AreEqual(units, PackCount(input)); Assert.AreSame(Actor, raw.GetPart<PhysicsPart>().InInventory); Assert.AreEqual(0, PackCount(output));
        }
        [TestCase("one-sprig")] [TestCase("distant-still")] [TestCase("no-still")]
        public void ConcentrationRequiresTwoUnitsAndARealNearbyStill(string refusal)
        {
            if (refusal == "one-sprig") Still(); if (refusal == "distant-still") Place("AlchemyStill", 20, 10);
            var raw = Stock("MendleafSprig", refusal == "one-sprig" ? 1 : 2); int count = PackCount("MendleafSprig");
            Assert.False(Prepare(raw, "concentrate_mendleaf")); Assert.AreEqual(count, PackCount("MendleafSprig")); Assert.AreEqual(0, PackCount("ConcentratedMendleaf"));
        }
        [Test] public void ConcentrateCannotBeConcentratedAgain()
        { Still(); var input = Stock("ConcentratedMendleaf", 2); Assert.False(Prepare(input, "concentrate_mendleaf")); Assert.AreEqual(2, PackCount("ConcentratedMendleaf")); }
        [TestCase("lit")] [TestCase("full")] [TestCase("foreign")]
        public void RefusedTorchRefillCannotSpendOilOrChangeFuel(string refusal)
        {
            var torch = Stock("Torch"); var fuel = torch.GetPart<FuelPart>(); fuel.FuelMass = refusal == "full" ? fuel.MaxFuel : 2;
            torch.GetPart<LightSourcePart>().Enabled = refusal == "lit"; if (refusal == "foreign") torch.GetPart<PhysicsPart>().InInventory = Factory.CreateEntity("Player");
            var oil = Stock("LampOil", 2); float original = fuel.FuelMass; Assert.False(Refuel(oil, torch)); Assert.AreEqual(original, fuel.FuelMass); Assert.AreEqual(2, PackCount("LampOil"));
        }
        [Test] public void TorchRefillOuterFailureRestoresFuelAndOil()
        {
            var torch = Stock("Torch"); torch.GetPart<LightSourcePart>().Enabled = false; torch.GetPart<FuelPart>().FuelMass = 2; var oil = Stock("LampOil", 2); FailAfter();
            Assert.False(Refuel(oil, torch)); Assert.AreEqual(2, torch.GetPart<FuelPart>().FuelMass); Assert.AreEqual(2, PackCount("LampOil"));
        }
        [Test] public void HoodNeedsCanonicalBodyOwnershipAndDoesNotEraseInnateMask()
        {
            var hood = Stock("FilterHood"); hood.GetPart<PhysicsPart>().Equipped = Actor; Assert.AreEqual(100, Intake());
            hood.GetPart<PhysicsPart>().Equipped = null; Assert.True(InventorySystem.Equip(Actor, hood));
            Actor.AddPart(new GasMaskPart { Power = 2 }); Assert.AreEqual(40, Intake());
            Assert.True(InventorySystem.UnequipItem(Actor, hood)); Assert.AreEqual(90, Intake()); Assert.AreEqual(2, Actor.GetPart<GasMaskPart>().Power);
        }
        [Test] public void SavedHoodKeepsExactlyOneEquipmentContribution()
        {
            var hood = Stock("FilterHood"); Assert.True(InventorySystem.Equip(Actor, hood)); var loaded = RoundTrip();
            Actor = loaded.Player; Zone = loaded.ZoneManager.ActiveZone; Assert.AreEqual(50, Intake()); Assert.Null(Actor.GetPart<GasMaskPart>());
            var saved = Pack.GetAllEquipped().Single(e => e.ID == hood.ID); Assert.True(InventorySystem.UnequipItem(Actor, saved)); Assert.AreEqual(100, Intake());
        }
        [TestCase("ripe")] [TestCase("progressed")] [TestCase("one-tick")] [TestCase("two-ticks")] [TestCase("three-ticks")]
        public void CompostCannotRushExistingGrowthOrSpendSludgeForNoBenefit(string refusal)
        {
            var crop = Plant(refusal == "ripe" ? 2 : 0); var p = crop.GetPart<CropPart>(); if (refusal == "progressed") p.TicksInStage = 1; if (refusal == "one-tick") p.TicksPerStage = 1; if (refusal == "two-ticks") p.TicksPerStage = 2; if (refusal == "three-ticks") p.TicksPerStage = 3;
            int original = p.TicksPerStage; var sludge = Stock("InertSludge"); Assert.False(Compost(crop, sludge)); Assert.AreEqual(original, p.TicksPerStage); Assert.AreEqual(1, PackCount("InertSludge"));
        }
        [Test] public void CompostOuterFailureRestoresItsSavedFlagAndOriginalStageLength()
        {
            var crop = Plant(); int original = crop.GetPart<CropPart>().TicksPerStage; var sludge = Stock("InertSludge"); FailAfter();
            Assert.False(Compost(crop, sludge)); Assert.AreEqual(original, crop.GetPart<CropPart>().TicksPerStage); Assert.False(Composted(crop)); Assert.AreEqual(1, PackCount("InertSludge"));
        }
        [TestCase("young")] [TestCase("bad-seed")]
        public void RefusedSeedHarvestPreservesTheOwnerAndProducesNothing(string refusal)
        {
            var crop = Plant(refusal == "young" ? 1 : 2); if (refusal == "bad-seed") crop.GetPart<CropPart>().SeedYieldBlueprint = "Dagger";
            Assert.False(Act(crop, "HarvestCropSeeds")); Assert.AreSame(Zone.GetCell(11, 10), Zone.GetEntityCell(crop)); Assert.AreEqual(0, Count("KnotflaxSeed")); Assert.AreEqual(0, Count("Dagger"));
        }
        [Test] public void SeedHarvestOuterFailureRestoresOwnerAndAllSeeds()
        {
            var crop = Plant(2); FailAfter(); Assert.False(Act(crop, "HarvestCropSeeds")); Assert.AreSame(Zone.GetCell(11, 10), Zone.GetEntityCell(crop)); Assert.AreEqual(0, Count("KnotflaxSeed"));
        }
        [Test] public void NormalHarvestRetainsItsOriginalProduceAndSeedChoice()
        { var crop = Plant(2); Assert.True(Act(crop, "HarvestCultivatedCrop")); Assert.AreEqual(2, Count("KnotflaxCord")); Assert.AreEqual(1, Count("KnotflaxSeed")); }
        [TestCase("ripe")] [TestCase("unprepared")] [TestCase("occupied")] [TestCase("hidden")] [TestCase("distant")]
        public void TransplantRefusesUnsuitableBedsWithoutMovingOrResettingGrowth(string refusal)
        {
            var crop = Plant(refusal == "ripe" ? 2 : 1); crop.GetPart<CropPart>().TicksInStage = 3;
            int x = refusal == "distant" ? 15 : 10, y = 11; Bed(x, y, refusal != "unprepared");
            if (refusal == "occupied") Place("KnotflaxCrop", x, y); if (refusal == "hidden") Zone.GetCell(x, y).IsVisible = false;
            Assert.False(Transplant(crop, x, y)); Assert.AreSame(Zone.GetCell(11, 10), Zone.GetEntityCell(crop)); Assert.AreEqual(3, crop.GetPart<CropPart>().TicksInStage);
        }
        [Test] public void TransplantAfterFailureReturnsExactOwnerAndMoistureToOriginalBed()
        {
            var crop = Plant(1); crop.GetPart<CropPart>().MoistureTicks = 9; Bed(10, 11); FailAfter(); Assert.False(Transplant(crop, 10, 11));
            Assert.AreSame(Zone.GetCell(11, 10), Zone.GetEntityCell(crop)); Assert.False(Zone.GetCell(10, 11).HasObjectWithPart<CropPart>()); Assert.AreEqual(9, crop.GetPart<CropPart>().MoistureTicks);
        }
        [TestCase("no-forge")] [TestCase("equipped")] [TestCase("stacked")] [TestCase("foreign")]
        public void PhysicalInfusionRefusalCannotSpendMineralOrGrantEnhancement(string refusal)
        {
            if (refusal != "no-forge") Forge(); var target = Stock("Dagger", refusal == "stacked" ? 2 : 1); var mineral = Stock("PaleSalt");
            if (refusal == "equipped") Assert.True(InventorySystem.Equip(Actor, target)); if (refusal == "foreign") target.GetPart<PhysicsPart>().InInventory = Factory.CreateEntity("Player");
            Assert.False(Infuse(mineral, target)); Assert.AreEqual(1, PackCount("PaleSalt")); Assert.AreEqual(0, ItemEnhancing.CountEnhancements(target)); Assert.False(Actor.HasPart<BitLockerPart>());
        }
        [Test] public void PhysicalInfusionRespectsTwoSlotLimitAndPreservesThirdMineral()
        {
            Forge(); var target = Stock("Dagger"); Assert.True(Infuse(Stock("PaleSalt"), target)); Assert.True(Infuse(Stock("ChoirIron"), target)); var third = Stock("GlowQuartz");
            Assert.False(Infuse(third, target)); Assert.AreEqual(2, ItemEnhancing.CountEnhancements(target)); Assert.AreEqual(1, PackCount("GlowQuartz"));
        }
        [Test] public void InfusionRollbackPreservesAnIndependentCallbackEnhancement()
        {
            Forge(); var target = Stock("Dagger"); var salt = Stock("PaleSalt");
            var observer = new AddEnhancementThenThrow { Target = target }; Actor.AddPart(observer);
            Assert.False(Infuse(salt, target)); Assert.AreEqual(1, PackCount("PaleSalt"));
            Assert.IsNull(target.GetPart<EnhancementPaleSalt>(), "Only the failed infusion is undone.");
            Assert.NotNull(observer.Added, "The independent callback ran before failing.");
            Assert.AreSame(observer.Added, target.GetPart<EnhancementChoirIron>(), "Rollback must not erase the callback's separate addition.");
        }
        public sealed class AddEnhancementThenThrow : Part
        {
            public Entity Target; public EnhancementChoirIron Added;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID != "AfterInventoryAction") return true;
                Assert.True(ItemEnhancing.Apply(Target, "EnhancementChoirIron", 3));
                Added = Target.GetPart<EnhancementChoirIron>();
                throw new InvalidOperationException("independent callback after infusion");
            }
        }
        [Test] public void PhysicalInfusionOuterFailureRestoresNamesPartsCountsAndMaterial()
        {
            Forge(); var target = Stock("Dagger"); string name = target.GetDisplayName(); int modifications = target.GetIntProperty("ModificationCount"); var mineral = Stock("PaleSalt"); FailAfter();
            Assert.False(Infuse(mineral, target)); Assert.AreEqual(0, ItemEnhancing.CountEnhancements(target)); Assert.AreEqual(name, target.GetDisplayName()); Assert.AreEqual(modifications, target.GetIntProperty("ModificationCount")); Assert.AreEqual(1, PackCount("PaleSalt"));
        }
    }
}
