using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class DensityAuthoredExamineCoverageTests
    {
        EntityFactory factory;
        [SetUp] public void Setup()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
        }

        [TestCase("Dagger")]
        [TestCase("ShortSword")]
        [TestCase("LongSword")]
        [TestCase("Claymore")]
        [TestCase("Greatsword")]
        [TestCase("Hatchet")]
        [TestCase("Battleaxe")]
        [TestCase("Cudgel")]
        [TestCase("Mace")]
        [TestCase("Warhammer")]
        [TestCase("Spear")]
        [TestCase("LeatherArmor")]
        [TestCase("ChainMail")]
        [TestCase("PlateArmor")]
        [TestCase("Buckler")]
        [TestCase("IronBuckler")]
        [TestCase("IronHelmet")]
        [TestCase("LeatherBoots")]
        [TestCase("IronshodBoots")]
        [TestCase("LeatherGloves")]
        [TestCase("LeatherCap")]
        [TestCase("Cloak")]
        [TestCase("WardedCloak")]
        [TestCase("Tree")]
        [TestCase("Bush")]
        [TestCase("VineWall")]
        [TestCase("Reeds")]
        [TestCase("Crate")]
        [TestCase("Chest")]
        [TestCase("Sack")]
        [TestCase("WovenBasket")]
        [TestCase("HollowLog")]
        [TestCase("Chair")]
        [TestCase("Bed")]
        [TestCase("Campfire")]
        [TestCase("Well")]
        [TestCase("MarketStall")]
        [TestCase("BerryBush")]
        [TestCase("Beehive")]
        public void FrequentObjectHasReadableAuthoredFlavorExactlyOnce(string blueprint)
        {
            var item = factory.CreateEntity(blueprint);
            var examine = item.GetPart<ExaminablePart>();
            Assert.NotNull(examine);
            Assert.IsFalse(string.IsNullOrWhiteSpace(examine.Text), blueprint + " has no authored description.");
            Assert.IsTrue(examine.Text.All(c => c >= 32 && c <= 126), "Authored copy must fit the ASCII renderer and stay on one line.");
            Assert.LessOrEqual(examine.Text.Length, 160, "Keep ordinary flavor short beside the existing live details.");
            string text = examine.BuildExamineLine();
            Assert.AreEqual(1, Count(text, examine.Text));
            Assert.AreEqual(examine.Text, examine.Description);
            Assert.AreEqual(text, examine.BuildExamineLine(), "Inspection must be stable.");
        }

        [TestCase("PhysicalObject")]
        [TestCase("Terrain")]
        public void BaseTemplatesDoNotLeakSpecificFlavorToUnrelatedDescendants(string blueprint)
        {
            var part = factory.CreateEntity(blueprint).GetPart<ExaminablePart>();
            Assert.IsTrue(part == null || string.IsNullOrWhiteSpace(part.Text));
        }

        [Test]
        public void ChangedWeaponKeepsLiveMechanicsAndAuthoredFlavorOnce()
        {
            var item = factory.CreateEntity("Dagger");
            var weapon = item.GetPart<MeleeWeaponPart>();
            weapon.BaseDamage = "2d3+7"; weapon.PenBonus = -4;
            string text = item.GetPart<ExaminablePart>().BuildExamineLine();
            Assert.AreEqual(1, Count(text, "Damage: 2d3+7 per penetration"));
            Assert.AreEqual(1, Count(text, "Penetration bonus: -4"));
            StringAssert.DoesNotContain("Damage: 1d4", text);
            Assert.AreEqual("2d3+7", weapon.BaseDamage); Assert.AreEqual(-4, weapon.PenBonus);
        }

        [Test]
        public void ChangedArmorKeepsLiveContributionsWithoutWeaponText()
        {
            var item = factory.CreateEntity("LeatherArmor");
            var armor = item.GetPart<ArmorPart>(); armor.AV = 7; armor.DV = -3;
            string text = item.GetPart<ExaminablePart>().BuildExamineLine();
            Assert.AreEqual(1, Count(text, "AV: +7"));
            Assert.AreEqual(1, Count(text, "DV: -3"));
            StringAssert.DoesNotContain("Damage:", text);
            Assert.AreEqual(7, armor.AV); Assert.AreEqual(-3, armor.DV);
        }

        [TestCase("Signpost")]
        [TestCase("BreacherCleaver")]
        public void PreviouslyAuthoredLeafRetainsItsOwnText(string blueprint)
        {
            var authored = factory.Blueprints[blueprint].Parts["Examinable"];
            string expected = authored.ContainsKey("Text") ? authored["Text"] : authored["Description"];
            var examine = factory.CreateEntity(blueprint).GetPart<ExaminablePart>();
            Assert.IsNotEmpty(expected); Assert.AreEqual(expected, examine.Text);
            Assert.AreEqual(1, Count(examine.BuildExamineLine(), expected));
        }

        [TestCase(false)] [TestCase(true)]
        public void InspectingHarvestPlantDoesNotSpendOrReplenishIt(bool spent)
        {
            var plant = factory.CreateEntity("BerryBush");
            var harvest = plant.GetPart<HarvestablePart>(); harvest.Harvested = spent;
            string yield = harvest.YieldBlueprint; int min = harvest.YieldMin, max = harvest.YieldMax;
            var text = plant.GetPart<ExaminablePart>().BuildExamineLine();
            Assert.IsNotEmpty(text); Assert.AreEqual(spent, harvest.Harvested);
            Assert.AreEqual(yield, harvest.YieldBlueprint); Assert.AreEqual(min, harvest.YieldMin); Assert.AreEqual(max, harvest.YieldMax);
        }

        static int Count(string text, string fragment)
        {
            if (string.IsNullOrEmpty(fragment)) return 0;
            int result = 0, start = 0;
            while ((start = text.IndexOf(fragment, start, StringComparison.Ordinal)) >= 0)
            { result++; start += fragment.Length; }
            return result;
        }
    }
}
