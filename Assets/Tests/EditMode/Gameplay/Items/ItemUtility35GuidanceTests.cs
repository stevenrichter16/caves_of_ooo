using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class ItemUtility35GuidanceTests : FiftyWorldFixture
    {
        [TestCase("HealingTonic")]
        [TestCase("Antidote")]
        [TestCase("BurnSalve")]
        [TestCase("Panacea")]
        [TestCase("KnotflaxBandage")]
        [TestCase("SoddenFieldDressing")]
        [TestCase("SumpsievePad")]
        [TestCase("ClaspbeanPulp")]
        [TestCase("MargincressRibbon")]
        [TestCase("AbsentmintLeaf")]
        [TestCase("KnitmossPad")]
        [TestCase("SootrootPulp")]
        [TestCase("CookedMeat")]
        [TestCase("ToastedEmberwheat")]
        [TestCase("RoastedMushroom")]
        [TestCase("RoastedHearthbulb")]
        [TestCase("RoastedStarapple")]
        [TestCase("FieldMeal")]
        [TestCase("FireMoss")]
        [TestCase("FrostLichen")]
        [TestCase("GlacierSalt")]
        [TestCase("EmberFruit")]
        [TestCase("GlimmerBrine")]
        [TestCase("SparkRoot")]
        [TestCase("PrismreedPith")]
        [TestCase("LampOil")]
        [TestCase("SlipsedgeGel")]
        [TestCase("PitchpodResin")]
        [TestCase("Honeycomb")]
        [TestCase("Torch")]
        [TestCase("LampveinFan")]
        [TestCase("GroundwireScreen")]
        [TestCase("GripfrondWrap")]
        [TestCase("IronshodBoots")]
        [TestCase("GlowQuartz")]
        public void OrdinaryItemInspectionExplainsItsNewTacticalUse(string blueprint)
        {
            var item = Factory.CreateEntity(blueprint);
            Assert.NotNull(item);
            Assert.True(ItemExamineService.TryDescribeDetails(item, out var details));
            StringAssert.Contains("Tactical use:", details);
            StringAssert.Contains("action", details.ToLowerInvariant());
            StringAssert.Contains("Tactical use:", item.GetPart<ExaminablePart>().BuildExamineLine());
        }
        [Test] public void UnrelatedItemDoesNotAdvertiseAnInventedUtility()
        {
            var item = Factory.CreateEntity("Dagger");
            Assert.True(ItemExamineService.TryDescribeDetails(item, out var details));
            StringAssert.DoesNotContain("Tactical use:", details);
        }
        [Test] public void TacticalGuidancePreservesActualWeaponArmorAndTonicDetails()
        {
            var boots = Factory.CreateEntity("IronshodBoots");
            Assert.True(ItemExamineService.TryDescribeDetails(boots, out var armor));
            StringAssert.Contains("Speed: -5", armor); StringAssert.Contains("Tactical use:", armor);
            var healing = Factory.CreateEntity("HealingTonic");
            Assert.True(ItemExamineService.TryDescribeDetails(healing, out var medicine));
            StringAssert.Contains("4d6+4", medicine); StringAssert.Contains("Tactical use:", medicine);
        }
    }
}
