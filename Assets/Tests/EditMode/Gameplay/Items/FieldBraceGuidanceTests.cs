using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class FieldBraceGuidanceTests : FiftyWorldFixture
    {
        [Test]
        public void WrapInspectionNamesTheWorldBraceRowAndSolidOutcrop()
        {
            var wrap = Wear("GripfrondWrap");
            var outcrop = Place("TepuiWall", 10, 9);
            var hedge = Place("Hedge", 11, 10);
            Reveal(10, 9);
            Reveal(11, 10);
            Assert.True(outcrop.GetPart<PhysicsPart>().Solid);
            Assert.True(hedge.GetPart<PhysicsPart>().Solid);

            var row = CellMenu(10, 9).Single(action => action.Name == "BraceGrip");
            Assert.AreSame(wrap, row.Via);
            StringAssert.StartsWith("brace against wall", row.Display);
            Assert.False(CellMenu(11, 10).Any(action => action.Name == "BraceGrip"));

            string hint = Guidance(wrap);
            StringAssert.Contains("brace against wall", hint);
            StringAssert.Contains("solid", hint);
            StringAssert.Contains("wall", hint);
            StringAssert.Contains("outcrop", hint);
            AssertBraceLimits(hint);
            StringAssert.Contains("teleport", hint);
        }

        [Test]
        public void BootsInspectionNamesThePackActionAndFirmFooting()
        {
            Place("Grass", 10, 10);
            Reveal(10, 10);
            var boots = Wear("IronshodBoots");
            var row = Actions(boots).Single(action => action.Name == "BraceBoots");
            StringAssert.StartsWith("plant feet", row.Display);
            Assert.False(CellMenu(10, 10).Any(action => action.Name == "BraceBoots"));

            var pool = Pool(2, x: 10, y: 10);
            Assert.False(Actions(boots).Any(action => action.Name == "BraceBoots"));
            Assert.True(Zone.RemoveEntity(pool));
            Zone.TileState.WriteCoating(10, 10, "oil", 6);
            Assert.False(Actions(boots).Any(action => action.Name == "BraceBoots"));
            Assert.IsNull(Actor.GetEffect<EquipmentBraceEffect>());

            string hint = Guidance(boots);
            StringAssert.Contains("plant feet", hint);
            StringAssert.Contains("pack", hint);
            StringAssert.Contains("firm", hint);
            StringAssert.Contains("slippery", hint);
            StringAssert.Contains("speed penalty", hint);
            AssertBraceLimits(hint);
        }

        static void AssertBraceLimits(string hint)
        {
            StringAssert.Contains("one physical shove or pull", hint);
            StringAssert.Contains("three of your turns", hint);
            StringAssert.Contains("moving", hint);
            StringAssert.Contains("one action", hint);
        }

        static string Guidance(Entity equipment)
        {
            string hint = ItemTacticalUseDescription.Describe(equipment);
            Assert.IsNotNull(hint);
            Assert.True(ItemExamineService.TryDescribeDetails(equipment, out var details));
            StringAssert.Contains(hint, details);
            StringAssert.Contains(hint, equipment.GetPart<ExaminablePart>().BuildExamineLine());
            return hint.ToLowerInvariant();
        }

        Entity Wear(string blueprint)
        {
            var item = Carry(blueprint);
            Assert.True(InventorySystem.Equip(Actor, item));
            return item;
        }

        void Reveal(int x, int y)
        {
            var cell = Zone.GetCell(x, y);
            cell.IsVisible = cell.Explored = true;
        }
    }
}
