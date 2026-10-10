using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class QudControllerChoiceMenuTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        readonly InputTestFixture input = new InputTestFixture();
        readonly List<GameObject> objects = new List<GameObject>();
        readonly Dictionary<TileBase, char> glyphs = new Dictionary<TileBase, char>();
        WorldActionMenuUI menu;
        Zone zone;
        Entity actor;
        Cell cell;
        List<InventoryAction> actions;

        [SetUp] public void SetUp()
        {
            input.Setup();
            var grid = Create("Choice popup grid"); grid.AddComponent<Grid>();
            menu = Create("Choice popup").AddComponent<WorldActionMenuUI>();
            var text = Create("Choice text"); text.transform.SetParent(grid.transform); menu.Tilemap = text.AddComponent<Tilemap>();
            var bg = Create("Choice background"); bg.transform.SetParent(grid.transform); menu.BgTilemap = bg.AddComponent<Tilemap>();
            for (char c = ' '; c <= '~'; c++)
            {
                var tile = CP437TilesetGenerator.GetUiTile(c);
                if (tile != null) glyphs[tile] = c;
            }
            zone = new Zone("ChoiceMenu");
            actor = new Entity { BlueprintName = "MenuActor" };
            actor.Tags["Creature"] = "";
            actor.AddPart(new RenderPart { DisplayName = "test actor" });
            actor.Statistics["Hitpoints"] = new Stat { Owner = actor, Name = "Hitpoints", BaseValue = 6, Min = 0, Max = 10 };
            zone.AddEntity(actor, 10, 10); cell = zone.GetCell(10, 10);
            cell.Explored = cell.IsVisible = true;
            zone.TileState.WriteCoating(10, 10, "water", 5);
            actions = new List<InventoryAction> { new InventoryAction("Choice", "Wait one turn", "0") };
            StringAssert.Contains("HP 6/10", WorldActionMenuUI.BuildTitleFor(cell, actor));
            Assert.IsNotEmpty(WorldActionMenuUI.BuildStatusLinesFor(actor, cell, zone), "World status precondition must be real.");
        }

        [TearDown] public void TearDown()
        {
            for (int i = objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(objects[i]);
            objects.Clear(); glyphs.Clear(); input.TearDown();
        }

        [TestCase("Wait", false)]
        [TestCase("Wait", true)]
        [TestCase("Character", false)]
        [TestCase("Character", true)]
        public void ChoiceMenuRendersItsTitleWithoutWorldStatusOrFalseDetails(string title, bool connected)
        {
            Device(connected); Open(title, true);
            StringAssert.StartsWith(title, Row(1));
            StringAssert.DoesNotContain("You see", Row(1));
            StringAssert.DoesNotContain("HP", Row(1));
            Assert.AreEqual(3, ContentY);
            Assert.IsFalse(menu.SelectedCellIsPile);
            StringAssert.Contains("Wait one turn", Row(ContentY));
            Assert.AreEqual(connected ? "[A]select [B]back" : "[Enter]ok [Esc]back", Footer);
            StringAssert.DoesNotContain("details", Footer);
            Assert.AreSame(actions[0], menu.HighlightedAction, "Presentation must preserve the actual choice.");
            Assert.IsFalse(menu.SelectionMade);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DefaultWorldMenuRetainsHealthStatusAndAppropriateDetailsHint(bool connected)
        {
            Device(connected); menu.Open(actor, actor, cell, actions, zone);
            StringAssert.StartsWith(WorldActionMenuUI.BuildTitleFor(cell, actor), Row(1));
            Assert.Greater(ContentY, 3);
            StringAssert.Contains("On the ground:", Row(3));
            Assert.AreEqual(connected ? "[A]select [B]back [LT+RT]details" : "[F1]details [Enter]ok [Esc]back", Footer);
            Assert.AreSame(actor, menu.SelectedTarget); Assert.AreSame(cell, menu.SelectedCell);
        }

        [Test]
        public void ReopeningWorldMenuAfterChoicesRestoresDefaultTitleStatusAndPileFlag()
        {
            Device(true); Open("Wait", false);
            StringAssert.StartsWith("Wait", Row(1)); Assert.AreEqual(3, ContentY);
            menu.HideForReader();
            menu.Open(actor, actor, cell, actions, zone, true);
            StringAssert.StartsWith(WorldActionMenuUI.BuildTitleFor(cell, actor), Row(1));
            Assert.Greater(ContentY, 3); Assert.IsTrue(menu.SelectedCellIsPile);
            StringAssert.Contains("[LT+RT]details", Footer);
        }

        // Fall back to the existing signature before implementation so RED
        // observes the actual wrong rendered title, not a missing-API failure.
        void Open(string title, bool pile)
        {
            var method = typeof(WorldActionMenuUI).GetMethod("Open", new[] { typeof(Entity), typeof(Entity), typeof(Cell),
                typeof(List<InventoryAction>), typeof(Zone), typeof(bool), typeof(string) });
            if (method == null) menu.Open(actor, actor, cell, actions, zone, pile);
            else method.Invoke(menu, new object[] { actor, actor, cell, actions, zone, pile, title });
        }
        void Device(bool connected)
        {
            if (connected) InputSystem.AddDevice<Gamepad>().MakeCurrent();
            InputSystem.Update();
        }
        int ContentY => (int)typeof(WorldActionMenuUI).GetProperty("ContentY", Private).GetValue(menu);
        int Field(string name) => (int)typeof(WorldActionMenuUI).GetField(name, Private).GetValue(menu);
        string Footer => Row(Field("_popupH") - 1);
        string Row(int y)
        {
            var result = new char[44];
            for (int x = 0; x < result.Length; x++)
            {
                var tile = menu.Tilemap.GetTile(new Vector3Int(Field("_worldOriginX") + 2 + x, Field("_worldTopY") - y, 0));
                result[x] = tile != null && glyphs.TryGetValue(tile, out char c) ? c : ' ';
            }
            return new string(result).TrimEnd();
        }
        GameObject Create(string name)
        {
            var result = new GameObject(name); objects.Add(result); return result;
        }
    }
}
