using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public sealed class WorldActionCostReadabilityTests : ShortcutFixture
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        const string Meal = "prepare field meal (2 grain, 1 claspbean, 2 drams)";
        static int Width(WorldActionMenuUI ui) => (int)(typeof(WorldActionMenuUI)
            .GetField("_popupW", Private)?.GetValue(ui) ?? 48); // RED observes old pixels, not missing API.
        static Vector3Int Cell(WorldActionMenuUI ui, int x, int y) => new Vector3Int(
            (int)Get(ui, "_worldOriginX") + x, (int)Get(ui, "_worldTopY") - y, 0);
        static HashSet<Vector3Int> Painted(Tilemap map)
        {
            var cells = new HashSet<Vector3Int>();
            foreach (var p in map.cellBounds.allPositionsWithin) if (map.HasTile(p)) cells.Add(p);
            return cells;
        }
        static string Row(WorldActionMenuUI ui, int y, int start = 5)
        {
            var chars = new char[Width(ui) - start - 2];
            for (int x = 0; x < chars.Length; x++)
            {
                var tile = ui.Tilemap.GetTile(Cell(ui, start + x, y)); chars[x] = ' ';
                for (char c = '!'; c <= '~'; c++)
                    if (tile != null && tile == CP437TilesetGenerator.GetUiTile(c)) { chars[x] = c; break; }
            }
            return new string(chars).TrimEnd();
        }
        WorldActionMenuUI Open(params string[] labels)
        {
            var ui = Component<WorldActionMenuUI>(); ui.Tilemap = Tiles(); ui.BgTilemap = Tiles();
            ui.PopupCamera = OutsideCamera(); Reopen(ui, labels); return ui;
        }
        void Reopen(WorldActionMenuUI ui, params string[] labels) => ui.Open(Player, Player,
            Zone.GetCell(10,10), labels.Select((text,i) => new InventoryAction("Action" + i, text, "Command" + i)).ToList(), Zone);

        [TestCase(Meal)]
        [TestCase("prepare dressing (2 claspbean, 1 salt, 1 oil, and 3 clean jars)")]
        [TestCase("prepare ink (2 soot, 1 water, 1 gum, and 2 clean bottles)")]
        public void CompleteCostLabelIsActuallyPainted(string label)
        {
            var ui = Open(label, "examine");
            Assert.AreEqual(label, Row(ui, 3));
            Assert.AreEqual(label.Length + 7, Width(ui));
            Assert.AreEqual(7, Get(ui, "_popupH"), "Two choices keep their old row geometry.");
            Assert.AreEqual(Width(ui) * 7, Painted(ui.BgTilemap).Count);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(12)] [TestCase(20)]
        public void OrdinaryShortMenusRetainTheirFootprint(int count)
        {
            var ui = Open(Enumerable.Repeat("examine", count).ToArray());
            Assert.AreEqual(48, Width(ui)); Assert.AreEqual(Math.Max(1, Math.Min(12, count)) + 5, Get(ui,"_popupH"));
        }

        [TestCase(40,48)] [TestCase(41,48)] [TestCase(42,49)]
        [TestCase(71,78)] [TestCase(72,78)]
        public void WidthBoundaryPreservesEveryFittingCharacterAndOnlyMarksRealOverflow(int length,int width)
        {
            var ui=Open(new string('m',length)); Assert.AreEqual(width,Width(ui));
            Assert.AreEqual(length<=71?new string('m',length):new string('m',70)+"~",Row(ui,3));
            Assert.AreEqual(6,Get(ui,"_popupH"));
        }

        [Test]
        public void UnboundedNameStaysInsideOverlayAndSignalsOverflow()
        {
            var ui = Open(new string('w', 200));
            Assert.AreEqual(78, Width(ui)); Assert.AreEqual(new string('w',70) + "~", Row(ui,3));
            Assert.GreaterOrEqual((int)Get(ui,"_worldOriginX"),1);
            Assert.LessOrEqual((int)Get(ui,"_worldOriginX") + Width(ui),79);
        }

        [TestCase(false)] [TestCase(true)]
        public void ShrinkingClearsPriorForegroundIncludingWithoutOptionalBackground(bool noBackground)
        {
            var ui = Open(new string('w',70), Meal, "examine");
            if(noBackground) { ui.HideForReader(); ui.BgTilemap = null; Reopen(ui,new string('w',70),Meal,"examine"); }
            var old = Painted(ui.Tilemap); int oldWidth = Width(ui);
            Reopen(ui,"examine"); Assert.Greater(oldWidth,Width(ui));
            int left=(int)Get(ui,"_worldOriginX"),top=(int)Get(ui,"_worldTopY"),height=(int)Get(ui,"_popupH");
            var outside=old.Where(p=>p.x<left || p.x>=left+Width(ui) || p.y>top || p.y<=top-height).ToArray();
            Assert.IsNotEmpty(outside,"Actual old cells must extend beyond the smaller rectangle.");
            foreach(var p in outside) Assert.False(ui.Tilemap.HasTile(p),"stale glyph "+p);
        }

        [Test]
        public void CloseClearsWideFooterAndPreservesForeignTiles()
        {
            var ui=Open(Meal); var foreign=new Vector3Int(-100,-100,0); var tile=CP437TilesetGenerator.GetUiTile('!');
            ui.Tilemap.SetTile(foreign,tile); ui.BgTilemap.SetTile(foreign,tile);
            Press(Key.Escape,ui.HandleInput);
            CollectionAssert.AreEquivalent(new[]{foreign},Painted(ui.Tilemap));
            CollectionAssert.AreEquivalent(new[]{foreign},Painted(ui.BgTilemap));
        }

        [TestCase(false)] [TestCase(true)]
        public void FullWidthIsStableWhenScrollingAndSelectionKeepsAbsoluteIdentity(bool shortcut)
        {
            var labels=Enumerable.Range(0,20).Select(i=>i==19?Meal:"Choice "+i).ToArray();var ui=Open(labels);
            int width=Width(ui);Assert.AreEqual(Meal.Length+7,width,"Offscreen long rows set stable width at open.");
            for(int i=0;i<19;i++) Press(Key.DownArrow,ui.HandleInput);
            Assert.AreEqual(8,Get(ui,"_scrollOffset"));Assert.AreEqual(width,Width(ui));Assert.AreEqual(Meal,Row(ui,14));
            var action=ui.HighlightedAction;
            if(shortcut) Press(Letter(((char[])Get(ui,"_shortcuts"))[19]),ui.HandleInput);
            else Press(Key.Enter,ui.HandleInput);
            Assert.True(ui.SelectionMade);Assert.AreSame(action,ui.SelectedAction);Assert.AreEqual("Command19",ui.SelectedAction.Command);
        }

        [Test]
        public void MouseAcceptsExpandedInteriorAndRejectsItsBorderAndFooter()
        {
            var ui=Open(new string('w',70));var camera=ui.PopupCamera;camera.enabled=false;
            var pointer=Input.mousePosition;camera.pixelRect=new Rect(pointer.x-320,pointer.y-240,640,480);
            camera.orthographicSize=22.5f;camera.aspect=4f/3f;
            Action<int,int> aim=(x,y)=>
            {
                var desired=new Vector3((int)Get(ui,"_worldOriginX")+x+.5f,
                    (int)Get(ui,"_worldTopY")-y+.5f,0);
                // Unity clips pixelRect at the Game-view boundary. Align using
                // its actual projection, not an assumed centered 640px rect.
                var projected=camera.ScreenToWorldPoint(new Vector3(pointer.x,pointer.y,10));
                camera.transform.position+=new Vector3(desired.x-projected.x,desired.y-projected.y,0);
            };
            aim(70,3);
            bool mapped=CenteredPopupLayout.ScreenToGrid(camera,ui.Tilemap,pointer,out int gx,out int gy);
            Assert.True(mapped,"pointer="+pointer+" rect="+camera.pixelRect+" cell="+gx+","+gy+" camera="+camera.transform.position+" grid="+ui.Tilemap.layoutGrid.cellSize);
            Assert.AreEqual((int)Get(ui,"_worldOriginX")+70,gx,"Fixture must aim inside the expanded row.");
            Assert.AreEqual((int)Get(ui,"_worldTopY")-3,gy);
            Assert.AreEqual(0,Call(ui,"GetRowAtMouse"));
            aim(Width(ui)-1,3);Assert.AreEqual(-1,Call(ui,"GetRowAtMouse"));
            aim(70,(int)Get(ui,"_popupH")-1);Assert.AreEqual(-1,Call(ui,"GetRowAtMouse"));
        }

        [TestCase(false)] [TestCase(true)]
        public void ReaderReopenRetainsHighlightAndDeviceFooter(bool controller)
        {
            if(controller) {InputSystem.AddDevice<Gamepad>().MakeCurrent();InputSystem.Update();}
            var ui=Open(Meal,"examine");Press(Key.DownArrow,ui.HandleInput);var selected=ui.HighlightedAction;
            ui.HideForReader();Assert.IsEmpty(Painted(ui.Tilemap));Reopen(ui,Meal,"examine");ui.RestoreHighlight(selected);
            Assert.AreEqual("Command1",ui.HighlightedAction.Command);Assert.AreEqual(Meal,Row(ui,3));
            string footer=Row(ui,(int)Get(ui,"_popupH")-1,2);
            StringAssert.Contains(controller?"[LT+RT]details":"[F1]details",footer);
        }
    }
}
