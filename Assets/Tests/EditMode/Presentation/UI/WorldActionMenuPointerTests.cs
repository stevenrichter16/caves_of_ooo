using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Tests
{
    // Queued native mouse states reach the saved HandleInput and real painted
    // grid/camera. This is not an OS click or a packaged-player acceptance claim.
    public sealed class WorldActionMenuPointerTests : ShortcutFixture
    {
        const string Meal = "prepare field meal (2 grain, 1 claspbean, 2 drams)";
        Mouse mouse;
        WorldActionMenuUI menu;
        RenderTexture display;
        List<InventoryAction> actions;

        [SetUp]
        public void AddNativeMouse()
        {
            mouse = InputSystem.AddDevice<Mouse>();
            mouse.MakeCurrent();
            Queue(new Vector2(-100, -100));
        }

        [TearDown]
        public void ReleaseOwnedTarget()
        {
            if (menu != null && menu.PopupCamera != null) menu.PopupCamera.targetTexture = null;
            if (display != null) { display.Release(); Object.DestroyImmediate(display); }
            // ShortcutFixture owns all created GameObjects and restores devices.
        }

        void Open(int width = 1280, int height = 800, bool wide = true)
        {
            menu = Component<WorldActionMenuUI>();
            menu.Tilemap = Tiles(); menu.BgTilemap = Tiles();
            menu.PopupCamera = OutsideCamera(); menu.PopupCamera.enabled = false;
            display = new RenderTexture(width, height, 0);
            menu.PopupCamera.targetTexture = display;
            menu.PopupCamera.transform.position = new Vector3(0, 0, -10);
            var viewport = new Rect(0, .15f, 940f / 1280f, .85f);
            float aspect = width * viewport.width / (height * viewport.height);
            CenteredPopupLayout.ConfigureOverlayCamera(menu.PopupCamera, viewport, aspect);
            var cellSize = new Vector3(CenteredPopupLayout.ComputeCellWidth(aspect), 1, 0);
            menu.Tilemap.layoutGrid.cellSize = cellSize;
            menu.BgTilemap.layoutGrid.cellSize = cellSize;
            actions = new List<InventoryAction> {
                new InventoryAction("Meal", wide ? Meal : "prepare meal", "PrepareMeal"),
                new InventoryAction("Examine", "examine", "Examine"),
                new InventoryAction("Back", "back", "Back")
            };
            Reopen();
        }

        void Reopen() => menu.Open(Player, Player, Zone.GetCell(10, 10), actions, Zone);

        Vector2 Point(int x, int y, bool requirePaint = false)
        {
            var cell = new Vector3Int((int)Get(menu, "_worldOriginX") + x,
                (int)Get(menu, "_worldTopY") - y, 0);
            if (requirePaint) Assert.NotNull(menu.Tilemap.GetTile(cell), "Point must hit an actually painted glyph.");
            Vector2 screen = menu.PopupCamera.WorldToScreenPoint(menu.Tilemap.GetCellCenterWorld(cell));
            Assert.True(CenteredPopupLayout.ScreenToGrid(menu.PopupCamera, menu.Tilemap, screen, out int gx, out int gy));
            Assert.AreEqual(cell.x, gx); Assert.AreEqual(cell.y, gy);
            return screen;
        }

        void Queue(Vector2 point, bool left = false, bool right = false)
        {
            var state = new MouseState { position = point };
            if (left) state = state.WithButton(MouseButton.Left);
            if (right) state = state.WithButton(MouseButton.Right);
            InputSystem.QueueStateEvent(mouse, state); InputSystem.Update();
        }

        void Frame(Vector2 point, bool left = false, bool right = false)
        { Queue(point, left, right); menu.HandleInput(); }

        void AssertSelected(int index)
        {
            Assert.True(menu.SelectionMade); Assert.False(menu.SelectionCancelled); Assert.False(menu.IsOpen);
            Assert.AreSame(actions[index], menu.SelectedAction);
            Assert.AreSame(Player, menu.SelectedTarget); Assert.AreSame(Zone.GetCell(10, 10), menu.SelectedCell);
            Assert.AreEqual(KeyCode.None, menu.SelectedActivationKey, "Mouse selection must not invent a keyboard activation key.");
        }

        [TestCase(1280, 800, false)] [TestCase(1280, 800, true)]
        [TestCase(1920, 1080, true)]
        public void NativePressSelectsExactPaintedActionIncludingExpandedRightEdge(int width, int height, bool wide)
        {
            Open(width, height, wide);
            // The long label's final ')' lies beyond the old fixed-width menu.
            Vector2 point = Point(wide ? 54 : 5, 3, true);
            Frame(point); Frame(point, left: true); AssertSelected(0);
        }

        [TestCase(false)] [TestCase(true)]
        public void NativeHoverAndRightButtonMoveHighlightWithoutSelecting(bool right)
        {
            Open(); Vector2 point = Point(5, 4, true);
            Frame(point, right: right);
            Assert.True(menu.IsOpen); Assert.False(menu.SelectionMade); Assert.False(menu.SelectionCancelled);
            Assert.AreSame(actions[1], menu.HighlightedAction);
            Frame(point); Frame(point, left: true); AssertSelected(1);
        }

        [TestCase("outside")] [TestCase("border")] [TestCase("footer")]
        public void NativePressOutsideActionRowsCancelsWithoutBorrowingAnAction(string location)
        {
            Open();
            Vector2 point = location == "outside"
                ? new Vector2(menu.PopupCamera.pixelRect.xMax + 8, menu.PopupCamera.pixelRect.center.y)
                : Point(location == "border" ? 56 : 5,
                    location == "border" ? 3 : (int)Get(menu, "_popupH") - 1);
            Frame(point); Frame(point, left: true);
            Assert.True(menu.SelectionCancelled); Assert.False(menu.SelectionMade); Assert.IsNull(menu.SelectedAction);
            Assert.False(menu.IsOpen);
            Reopen(); point = Point(5, 4, true); Frame(point); Frame(point, left: true); AssertSelected(1);
        }

        [Test]
        public void HeldPressCannotSelectReopenedMenuButReleaseAndRepressCan()
        {
            Open(); Vector2 point = Point(5, 4, true);
            Frame(point); Frame(point, left: true); AssertSelected(1);
            Queue(point, left: true); Assert.False(mouse.leftButton.wasPressedThisFrame);
            Reopen(); menu.HandleInput();
            Assert.True(menu.IsOpen); Assert.False(menu.SelectionMade); Assert.False(menu.SelectionCancelled);
            Frame(point, left: true); Assert.True(menu.IsOpen);
            Frame(point); Frame(point, left: true); AssertSelected(1);
        }

        [Test]
        public void PointerParkedWhenOpenedCannotUndoRestoredOrKeyboardHighlight()
        {
            Open(); Vector2 point = Point(5, 3, true);
            Queue(point); Reopen(); menu.RestoreHighlight(actions[2]);
            Frame(point); Assert.AreSame(actions[2], menu.HighlightedAction);
            Press(Key.UpArrow, menu.HandleInput); Assert.AreSame(actions[1], menu.HighlightedAction);
            Release(); Frame(point); Assert.AreSame(actions[1], menu.HighlightedAction);
            Frame(Point(5, 3, true) + Vector2.right); Assert.AreSame(actions[0], menu.HighlightedAction);
            Assert.True(menu.IsOpen); Assert.False(menu.SelectionMade);
        }

        [Test]
        public void EnabledNativePointerSuppliesOneCoherentPositionAndLeftEdgePair()
        {
            Open(); Vector2 point = Point(5, 4, true);
            Queue(point); AssertPointer(point, false);
            Queue(point, right: true); AssertPointer(point, false);
            Queue(point); Queue(point, left: true); AssertPointer(point, true);
            Queue(point, left: true); AssertPointer(point, false);
            Queue(point); Queue(point, left: true); AssertPointer(point, true);
        }

        [TestCase(false)] [TestCase(true)]
        public void AbsentOrDisabledNativeMouseUsesActualLegacyPair(bool disabled)
        {
            Open(); Vector2 point = Point(5, 4, true);
            Queue(point, left: true); Assert.True(mouse.leftButton.wasPressedThisFrame);
            if (disabled) { InputSystem.DisableDevice(mouse); Assert.False(mouse.enabled); }
            else { InputSystem.RemoveDevice(mouse); Assert.IsNull(Mouse.current); }
            // Compare actual legacy values; this does not simulate a physical legacy click.
            AssertPointer(Input.mousePosition, Input.GetMouseButtonDown(0));
            if (disabled) InputSystem.EnableDevice(mouse); else mouse = InputSystem.AddDevice<Mouse>();
            mouse.MakeCurrent(); Queue(point); Queue(point, left: true); AssertPointer(point, true);
        }

        static void AssertPointer(Vector3 position, bool pressed)
        {
            var method = typeof(WorldActionMenuUI).GetMethod("ReadPointer", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method, "The position and left-button edge must come from the same enabled pointer backend.");
            var args = new object[] { Vector3.zero, false }; method.Invoke(null, args);
            Assert.AreEqual(position, (Vector3)args[0]); Assert.AreEqual(pressed, (bool)args[1]);
        }
    }
}
