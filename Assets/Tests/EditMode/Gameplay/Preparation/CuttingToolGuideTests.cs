using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class CuttingToolGuideTests : FiftySecondPreparationFixture
    {
        [SetUp] public void Catalogue() { MaterialProcessRegistry.ResetForTests(); MaterialProcessRegistry.LoadDefaults(); }
        [TearDown] public void ClearCatalogue() => MaterialProcessRegistry.ResetForTests();
        string Guide(Entity tool)
        {
            string text = UseGuides.SectionText(tool, Zone, Zone.GetEntityCell(tool), Actor);
            Assert.NotNull(text, "The tool should explain its existing material preparations.");
            return text;
        }
        [Test] public void KnifeExplainsThreeMaterialChoicesWithoutHidingCombatDetails()
        {
            var knife = Carry("Dagger"); string text = Guide(knife);
            StringAssert.Contains("material", text.ToLowerInvariant());
            StringAssert.Contains("leather", text.ToLowerInvariant());
            StringAssert.Contains("guard lashing", text.ToLowerInvariant());
            StringAssert.Contains("2 spurgrass", text.ToLowerInvariant());
            Assert.AreEqual(3, text.Split('\n').Count(line => line.Contains(" -> ")), "Examples need separate lines with arrows supported by the pixel reader.");
            StringAssert.DoesNotContain("→", text);
            Assert.True(ItemExamineService.TryDescribeDetails(knife, out var combat));
            StringAssert.Contains("Hit bonus", combat); StringAssert.Contains("Penetration", combat);
        }
        [TestCase("disabled")] [TestCase("broken")] [TestCase("stacked")] [TestCase("duplicate")]
        public void UnavailableExactToolDoesNotBorrowAnotherKnifesAvailability(string state)
        {
            var knife = Carry("Dagger");
            var other = Factory.CreateEntity("Dagger");
            other.GetPart<CuttingToolPart>().FlexibleStock = false;
            Assert.True(Pack.AddObject(other));
            other.GetPart<CuttingToolPart>().FlexibleStock = true;
            Assert.AreEqual(1, knife.GetPart<StackerPart>().StackCount);
            Assert.True(Pack.Objects.Contains(other), "A separate usable knife must really remain available.");
            if (state == "disabled") knife.GetPart<CuttingToolPart>().FlexibleStock = false;
            if (state == "broken") Assert.True(knife.ApplyEffect(new BrokenEffect()));
            if (state == "stacked") knife.GetPart<StackerPart>().StackCount = 2;
            if (state == "duplicate") knife.AddPart(new CuttingToolPart());
            string text = Guide(knife);
            StringAssert.DoesNotContain(UseGuides.NowCan, text);
            StringAssert.Contains("unavailable", text.ToLowerInvariant());
        }
        [Test] public void GroundToolExplainsThatItMustBePickedUpFirst()
        { StringAssert.Contains(UseGuides.PickUpFirst, Guide(Place("Dagger", 11, 10))); }
        [Test] public void ReadingTheKnifePreservesARealMaterialQuoteAndAllQuantities()
        {
            var knife = Carry("Dagger"); var offcut = Carry("CuredLeatherOffcut");
            string quote = Choice(offcut, MaterialProcessInputPart.CommandPrefix, a => a.Name == "cut_binding");
            int tick = Clock.TickCount, hp = Actor.GetStatValue("Hitpoints"), weight = Pack.GetCarriedWeight();
            for (int i = 0; i < 3; i++) Guide(knife);
            Assert.AreEqual(tick, Clock.TickCount); Assert.AreEqual(hp, Actor.GetStatValue("Hitpoints")); Assert.AreEqual(weight, Pack.GetCarriedWeight());
            Assert.True(Pack.Objects.Contains(offcut)); Assert.AreEqual(0, Count("LeatherBindingComponent"));
            Assert.True(Act(offcut, quote), "A guide must not regather the material menu and replace its retained quote token.");
            Assert.AreEqual(1, Count("LeatherBindingComponent")); Assert.True(Pack.Objects.Contains(knife));
        }
        [Test] public void LongAuthorCatalogueIsBoundedAndUsesItsLiveCounts()
        {
            string rows = string.Join(",", Enumerable.Range(1, 4).Select(i =>
                "{\"Id\":\"example" + i + "\",\"SourceBlueprint\":\"CuredLeatherOffcut\",\"Mode\":\"Process\",\"WorkKind\":\"HandCutting\",\"InputCount\":" + i +
                ",\"ActionText\":\"cut example\",\"OutputBlueprints\":[\"LeatherBindingComponent\"],\"OutputCounts\":[1]}"));
            Assert.IsEmpty(MaterialProcessRegistry.InitializeFromJson("{\"Processes\":[" + rows + "]}"));
            Assert.IsEmpty(MaterialProcessRegistry.Validate(Factory));
            string text = Guide(Carry("Dagger"));
            Assert.AreEqual(3, text.Split('\n').Count(line => line.Contains(" -> ")));
            StringAssert.Contains("3 cured", text.ToLowerInvariant());
        }
        [Test] public void MissingFactoryDoesNotAdvertiseRecipesItCannotValidate()
        {
            var knife = Carry("Dagger"); ForgePart.Factory = null;
            Assert.That(Guide(knife), Does.Not.Contain(" -> "));
        }
        [Test] public void ACombatHatchetDoesNotAcquireFlexibleStockInstructions()
        { Assert.Null(UseGuides.SectionText(Carry("Hatchet"), Zone, null, Actor)); }
        [Test] public void AuthoredShortSwordCutsOneBindingWithNoOtherFlexibleStockTool()
        {
            var hatchet = Carry("Hatchet");
            var offcut = Carry("CuredLeatherOffcut");
            Assert.Null(hatchet.GetPart<CuttingToolPart>());
            Assert.False(Actions(offcut).Any(action => action.Name == "cut_binding" && !action.IsDisabled));
            Assert.AreEqual(0, PackCount("LeatherBindingComponent"));

            var sword = Carry("ShortSword");
            var quote = Choice(offcut, MaterialProcessInputPart.CommandPrefix, action => action.Name == "cut_binding");
            Assert.True(sword.GetPart<CuttingToolPart>()?.FlexibleStock == true);
            Assert.True(Act(offcut, quote));

            Assert.False(Pack.Objects.Contains(offcut));
            Assert.Null(offcut.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(1, PackCount("LeatherBindingComponent"));
            Assert.AreSame(sword, Packed("ShortSword"));
            Assert.AreEqual(1, PackCount("ShortSword"));
            Assert.AreSame(Actor, sword.GetPart<PhysicsPart>().InInventory);
            Assert.AreSame(hatchet, Packed("Hatchet"));
        }
        [Test] public void KnifeDoesNotGainFictionalDirectMaterialActions()
        {
            var knife = Carry("Dagger"); GuideOrAbsent(knife);
            Assert.False(Actions(knife).Any(a => MaterialProcessInputPart.IsCommand(a.Command)));
        }
        void GuideOrAbsent(Entity knife) => UseGuides.SectionText(knife, Zone, null, Actor);
        [Test] public void SavedKnifeRetainsTheGuideWithoutNewInstanceState()
        {
            var knife = Carry("Dagger"); string id = knife.ID, before = Guide(knife);
            var loaded = RoundTrip(); Actor = loaded.Player; Zone = loaded.ZoneManager.ActiveZone; SettlementRuntime.ActiveZone = Zone;
            knife = Pack.Objects.Single(e => e.ID == id);
            Assert.AreEqual(before, Guide(knife)); Assert.AreEqual(1, knife.Parts.Count(p => p is CuttingToolPart));
        }
        [TestCase(false)]
        [TestCase(true)]
        public void LoadedShortSwordHonorsWhetherItHasAlreadySeenTheCuttingDefinition(bool known)
        {
            var sword = Carry("ShortSword");
            string id = sword.ID;
            Assert.True(sword.RemovePart(sword.GetPart<CuttingToolPart>()));
            if (!known) sword.IntProperties.Remove("CuttingToolDefinitionSeen");
            using (var stream = new MemoryStream())
            {
                var writer = new SaveWriter(stream);
                writer.WriteEntityReference(sword);
                writer.WriteQueuedEntityBodies();
                stream.Position = 0;
                var reader = new SaveReader(stream, Factory);
                var loaded = reader.ReadEntityReference();
                reader.ReadEntityBodies();
                Assert.AreEqual(id, loaded.ID);
                Assert.That(loaded.Parts.Count(p => p is CuttingToolPart), Is.EqualTo(known ? 0 : 1));
            }
        }
    }
}
