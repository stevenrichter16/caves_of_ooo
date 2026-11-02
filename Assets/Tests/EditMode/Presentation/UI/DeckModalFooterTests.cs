using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public sealed class DeckModalFooterTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        readonly List<GameObject> owned = new List<GameObject>();
        readonly InputTestFixture input = new InputTestFixture();
        [SetUp] public void Setup() => input.Setup();
        [TearDown] public void Cleanup()
        {
            for (int i = owned.Count - 1; i >= 0; i--) Object.DestroyImmediate(owned[i]);
            owned.Clear(); input.TearDown();
        }
        T Create<T>() where T : Component
        { var go = new GameObject("Owned Deck modal"); owned.Add(go); return go.AddComponent<T>(); }
        static int Field(object obj, string name) => (int)obj.GetType().GetField(name, Private).GetValue(obj);
        static void Call(object obj, string name) => obj.GetType().GetMethod(name, Private).Invoke(obj, null);
        static int Count(Tilemap map)
        { int n = 0; foreach (var cell in map.cellBounds.allPositionsWithin) if (map.HasTile(cell)) n++; return n; }

        [Test] public void ShorterAbilityMenuClearsOldTextAndBackingOutsideItsNewFootprint()
        {
            var ui = Create<AbilityManagerUI>(); ui.Tilemap = Create<Tilemap>(); ui.BgTilemap = Create<Tilemap>();
            var tall = new Entity(); var abilities = new ActivatedAbilitiesPart(); tall.AddPart(abilities);
            for (int i = 0; i < 10; i++) abilities.AddAbility("Ability " + i, "Test" + i, "Skills");
            ui.Open(tall); int oldHeight = Field(ui, "_popupH");
            var shortActor = new Entity(); shortActor.AddPart(new ActivatedAbilitiesPart()); ui.Open(shortActor);
            int height = Field(ui, "_popupH"), x = Field(ui, "_worldOriginX"), top = Field(ui, "_worldTopY");
            Assert.Less(height, oldHeight);
            foreach (var map in new[] { ui.Tilemap, ui.BgTilemap })
                foreach (var cell in map.cellBounds.allPositionsWithin)
                    if (map.HasTile(cell)) Assert.That(cell.y, Is.InRange(top - height + 1, top), "Stale menu pixel " + cell);
            ui.Close(); Assert.Zero(Count(ui.Tilemap)); Assert.Zero(Count(ui.BgTilemap));
        }

        [TestCase(false, false)] [TestCase(false, true)]
        [TestCase(true, false)] [TestCase(true, true)]
        public void ActionFooterHasReadableBackingAndClearsWithItsMenu(bool abilities, bool gamepad)
        {
            if (gamepad) InputSystem.AddDevice<Gamepad>();
            Assert.NotNull(CP437TilesetGenerator.GetUiTile('['), "Actual font required");
            var text = Create<Tilemap>(); var bg = Create<Tilemap>();
            object ui; int height;
            if (abilities)
            {
                var a = Create<AbilityManagerUI>(); a.Tilemap = text; a.BgTilemap = bg;
                var actor = new Entity { BlueprintName = "DeckMenuActor" };
                actor.AddPart(new ActivatedAbilitiesPart()); a.Open(actor); ui = a;
                height = Field(a, "_popupH");
            }
            else
            {
                var p = Create<PauseMenuUI>(); p.Tilemap = text; p.BgTilemap = bg;
                p.Controller = new PauseMenuController(); p.Controller.Open();
                p.Controller.ClickSelect(PauseMenuController.GraphicsIndex, null, null);
                Call(p, "Render"); ui = p; height = Field(p, "_renderedHeight");
            }
            var footer = new Vector3Int(Field(ui, "_worldOriginX"), Field(ui, "_worldTopY") - height + 1, 0);
            Assert.True(bg.HasTile(footer), "Action hint must not sit directly on world scenery.");
            Assert.AreEqual(CP437TilesetGenerator.GetUiTile('['), text.GetTile(footer));
            Assert.AreEqual(QudColorParser.Gray, text.GetColor(footer), "Action hint uses readable existing light-gray palette.");
            Assert.False(bg.HasTile(footer + Vector3Int.down), "Backing stays within the owned menu footprint.");
            if (abilities) ((AbilityManagerUI)ui).Close();
            else
            {
                var p = (PauseMenuUI)ui; p.Controller.Back(); Call(p, "Render");
                int nextHeight = Field(p, "_renderedHeight");
                Assert.AreEqual(28 * nextHeight, Count(bg), "Shorter page cannot leave stale background rows.");
                p.Controller.Close(); Call(p, "ClearAll");
            }
            Assert.Zero(Count(text)); Assert.Zero(Count(bg));
        }
    }
}
