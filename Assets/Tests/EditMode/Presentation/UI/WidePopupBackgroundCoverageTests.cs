using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Measures real painted popup quads, not merely occupied tile cells.
    /// CPU geometry/opacity coverage is not a GPU or ordinary-Play screenshot claim.</summary>
    public sealed class WidePopupBackgroundCoverageTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        const float Epsilon = .0002f;

        [TestCase(.9f)]
        [TestCase(1f)]
        [TestCase(1.1860311f)]
        public void PaintedAnnouncementHasContinuousOpaqueBacking(float cellWidth)
        {
            using (var rig = new Rig())
            {
                rig.SetCellWidth(cellWidth);
                rig.Reader.Open("A real painted paragraph above the world.\nA second line remains readable.");
                AssertOpaqueCoverage(rig.Reader);
                Assert.That(rig.Reader.Tilemap.orientationMatrix, Is.EqualTo(Matrix4x4.identity),
                    "Backing coverage must not stretch the foreground font.");
            }
        }

        [Test]
        public void ResizeWhileOpenUpdatesBackingWithoutRepaintingOrResizingGlyphs()
        {
            using (var rig = new Rig())
            {
                rig.SetCellWidth(.9f);
                rig.Reader.Open("HHHH\nA second line keeps the same letters during a resize.");
                var glyphs = Capture(rig.Reader.Tilemap);
                var lines = new List<string>(rig.Reader.VisibleLines);
                foreach (float width in new[] { 1.1860311f, 1f, .9f })
                {
                    // This public binding invokes the shipping layout updater, also
                    // called by LateUpdate. Do not reopen/repaint the AnnouncementUI.
                    rig.SetCellWidth(width);
                    Assert.True(rig.Reader.IsOpen);
                    CollectionAssert.AreEqual(lines, rig.Reader.VisibleLines);
                    AssertTilesUnchanged(rig.Reader.Tilemap, glyphs);
                    Assert.That(rig.Reader.Tilemap.orientationMatrix, Is.EqualTo(Matrix4x4.identity));
                    AssertOpaqueCoverage(rig.Reader);
                }
                rig.Reader.Close();
                Assert.Zero(rig.Reader.BgTilemap.GetUsedTilesCount());
                Assert.Zero(rig.Reader.Tilemap.GetUsedTilesCount());
            }
        }

        [Test]
        public void InventoryExamineQueueUsesFullscreenCameraAndOpaqueBackingWithoutErasingInventory()
        {
            using (var rig = new Rig())
            {
                rig.Display = new RenderTexture(2321, 1139, 0);
                rig.MainCamera.targetTexture = rig.Display;
                rig.MainCamera.aspect = 2321f / 1139f;
                var player = new Entity { BlueprintName = "Player" };
                player.AddPart(new RenderPart { DisplayName = "you", RenderString = "@" });
                player.AddPart(new PhysicsPart());
                player.AddPart(new InventoryPart());
                var item = new Entity { BlueprintName = "PopupCoverageTonic" };
                item.AddPart(new RenderPart { DisplayName = "test tonic", RenderString = "!" });
                item.AddPart(new PhysicsPart { Takeable = true });
                Assert.True(player.GetPart<InventoryPart>().AddObject(item));
                var zone = new Zone("WidePopupBackgroundCoverage");
                Assert.True(zone.AddEntity(player, 10, 10));
                rig.Input.PlayerEntity = player;
                rig.Input.CurrentZone = zone;
                Invoke(rig.Input, "OpenInventory");
                Assert.True(rig.Inventory.IsOpen);
                var inventoryTiles = Capture(rig.Main);
                Assert.Greater(inventoryTiles.Count, 0, "A real painted inventory must remain underneath.");
                Vector3 position = rig.MainCamera.transform.position;
                float size = rig.MainCamera.orthographicSize;
                var document = ExamineDocumentBuilder.BuildInventory(item, player);
                Assert.NotNull(document, "Use an owned item Examine document, not synthetic blank text.");
                MessageLog.AddPresentedAnnouncement(document.ToPlainText(), document);
                Assert.True((bool)Invoke(rig.Input, "TryOpenAnnouncement"));
                rig.Renderer.SetPopupOverlayCamera(rig.PopupCamera);
                Assert.AreSame(document, rig.Reader.Document);
                Assert.True(rig.Reader.IsOpen);
                Assert.True(rig.Inventory.IsOpen);
                Assert.True(rig.PopupCamera.enabled);
                Assert.AreEqual(new Rect(0, 0, 1, 1), rig.PopupCamera.rect);
                Assert.AreEqual(CameraClearFlags.Depth, rig.PopupCamera.clearFlags);
                Assert.AreEqual(new Rect(0, 0, 1, 1), rig.MainCamera.rect);
                Assert.AreEqual(position, rig.MainCamera.transform.position);
                Assert.AreEqual(size, rig.MainCamera.orthographicSize);
                Assert.Greater(rig.Reader.BgTilemap.layoutGrid.cellSize.x, 1f);
                AssertTilesUnchanged(rig.Main, inventoryTiles);
                AssertOpaqueCoverage(rig.Reader);
                rig.Reader.Close();
                Invoke(rig.Input, "CloseAnnouncement");
                Assert.False(rig.PopupCamera.enabled);
                Assert.True(rig.Inventory.IsOpen);
                Assert.Zero(rig.Reader.BgTilemap.GetUsedTilesCount());
                AssertTilesUnchanged(rig.Main, inventoryTiles);
                Assert.True(player.GetPart<InventoryPart>().Contains(item));
            }
        }

        static void AssertOpaqueCoverage(AnnouncementUI ui)
        {
            var map = ui.BgTilemap;
            int left = (int)typeof(AnnouncementUI).GetField("_worldOriginX", Private).GetValue(ui);
            int top = (int)typeof(AnnouncementUI).GetField("_worldTopY", Private).GetValue(ui);
            int height = (int)typeof(AnnouncementUI).GetField("_popupH", Private).GetValue(ui);
            var solid = CP437TilesetGenerator.GetUiTile(CP437TilesetGenerator.SolidBlock);
            var rect = solid.sprite.rect;
            foreach (var pixel in solid.sprite.texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height))
                Assert.That(pixel.a, Is.EqualTo(1f).Within(Epsilon), "The actual fill sprite must be opaque throughout.");
            Assert.That(map.color.a * solid.color.a, Is.EqualTo(1f).Within(Epsilon));
            Assert.Greater(height, 2);
            for (int y = top - height + 1; y <= top; y++)
                for (int x = left; x < left + 56; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    Assert.AreSame(solid, map.GetTile(cell), "No missing or substituted transparent backing tile.");
                    Assert.That(map.GetColor(cell).a, Is.EqualTo(1f).Within(Epsilon));
                    Rect painted = PaintedBounds(map, cell);
                    Vector3 low = map.CellToWorld(cell);
                    Vector3 high = map.CellToWorld(cell + new Vector3Int(1, 1, 0));
                    Assert.LessOrEqual(painted.xMin, low.x + Epsilon, "Backing leaves a vertical gap at " + cell);
                    Assert.GreaterOrEqual(painted.xMax, high.x - Epsilon, "Backing leaves a vertical gap at " + cell);
                    Assert.LessOrEqual(painted.yMin, low.y + Epsilon, "Backing leaves a horizontal gap at " + cell);
                    Assert.GreaterOrEqual(painted.yMax, high.y - Epsilon, "Backing leaves a horizontal gap at " + cell);
                    if (x + 1 < left + 56)
                        Assert.GreaterOrEqual(painted.xMax + Epsilon,
                            PaintedBounds(map, cell + Vector3Int.right).xMin, "Adjacent backing quads must meet.");
                }
        }

        static Rect PaintedBounds(Tilemap map, Vector3Int cell)
        {
            var sprite = map.GetSprite(cell);
            Assert.NotNull(sprite);
            Bounds bounds = sprite.bounds;
            Matrix4x4 matrix = map.orientationMatrix * map.GetTransformMatrix(cell);
            Vector3 center = map.GetCellCenterLocal(cell);
            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
            foreach (var corner in new[] {
                new Vector3(bounds.min.x, bounds.min.y, 0), new Vector3(bounds.max.x, bounds.min.y, 0),
                new Vector3(bounds.min.x, bounds.max.y, 0), new Vector3(bounds.max.x, bounds.max.y, 0) })
            {
                Vector3 point = map.transform.TransformPoint(center + matrix.MultiplyPoint3x4(corner));
                minX = Mathf.Min(minX, point.x); minY = Mathf.Min(minY, point.y);
                maxX = Mathf.Max(maxX, point.x); maxY = Mathf.Max(maxY, point.y);
            }
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        readonly struct PaintedTile
        {
            public readonly Vector3Int Cell;
            public readonly TileBase Tile;
            public readonly Matrix4x4 Matrix;
            public readonly Color Color;
            public PaintedTile(Tilemap map, Vector3Int cell)
            { Cell = cell; Tile = map.GetTile(cell); Matrix = map.GetTransformMatrix(cell); Color = map.GetColor(cell); }
        }
        static List<PaintedTile> Capture(Tilemap map)
        {
            var result = new List<PaintedTile>();
            foreach (var cell in map.cellBounds.allPositionsWithin) if (map.HasTile(cell)) result.Add(new PaintedTile(map, cell));
            return result;
        }
        static void AssertTilesUnchanged(Tilemap map, List<PaintedTile> before)
        {
            Assert.AreEqual(before.Count, Capture(map).Count);
            foreach (var tile in before)
            {
                Assert.AreSame(tile.Tile, map.GetTile(tile.Cell));
                Assert.AreEqual(tile.Matrix, map.GetTransformMatrix(tile.Cell));
                Assert.AreEqual(tile.Color, map.GetColor(tile.Cell));
            }
        }
        static object Invoke(object instance, string name) => instance.GetType().GetMethod(name, Private).Invoke(instance, null);

        sealed class Rig : IDisposable
        {
            readonly GameObject root = new GameObject("Wide popup background test");
            readonly List<Camera> borrowedCameras = new List<Camera>();
            readonly List<MessageLog.Entry> oldLog = MessageLog.GetAllEntries();
            readonly List<string> oldAnnouncements = MessageLog.GetPendingAnnouncementsSnapshot();
            readonly int oldFlash = MessageLog.FlashStamp, oldSerial = MessageLog.NextSerialValue;
            readonly long oldRevision = MessageLog.Revision, oldGeneration = MessageLog.HistoryGeneration;
            readonly Action<string> oldMessage = MessageLog.OnMessage;
            readonly Func<int> oldTick = MessageLog.TickProvider;
            readonly object origins = typeof(MessageLog).GetField("AnnouncementOrigins", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            readonly Array oldOrigins;
            readonly List<GameObject> detached = new List<GameObject>();
            readonly Action<int, int, string> priorCellDirty = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> priorFullDirty = ZoneRenderHooks.FullDirtyCallback;
            public readonly Camera MainCamera, PopupCamera;
            public readonly Tilemap Main;
            public readonly ZoneRenderer Renderer;
            public readonly AnnouncementUI Reader;
            public readonly InventoryUI Inventory;
            public readonly InputHandler Input;
            public RenderTexture Display;

            public Rig()
            {
                oldOrigins = (Array)origins.GetType().GetMethod("ToArray").Invoke(origins, null);
                MessageLog.OnMessage = null; MessageLog.TickProvider = null;
                MessageLog.Clear();
                foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (camera.CompareTag("MainCamera")) { borrowedCameras.Add(camera); camera.tag = "Untagged"; }
                var cameraHost = Child("Main camera", root.transform);
                cameraHost.tag = "MainCamera";
                MainCamera = cameraHost.AddComponent<Camera>();
                MainCamera.orthographic = true;
                MainCamera.aspect = 16f / 9f;
                MainCamera.transform.position = new Vector3(40, 22.5f, -10);
                PopupCamera = Child("Popup camera", root.transform).AddComponent<Camera>();
                PopupCamera.orthographic = true;
                PopupCamera.enabled = false;
                var grid = Child("World grid", root.transform);
                grid.AddComponent<Grid>();
                var world = Child("World tilemap", grid.transform);
                Main = world.AddComponent<Tilemap>(); world.AddComponent<TilemapRenderer>();
                Renderer = world.AddComponent<ZoneRenderer>();
                if (Renderer.CenteredPopupBgTilemap == null) Invoke(Renderer, "Awake");
                foreach (string field in new[] { "_fineWaterGridTransform", "_sidebarGridTransform", "_hotbarGridTransform", "_popupOverlayGridTransform" })
                {
                    var transform = typeof(ZoneRenderer).GetField(field, Private).GetValue(Renderer) as Transform;
                    if (transform != null) detached.Add(transform.gameObject);
                }
                Renderer.SetPopupOverlayCamera(PopupCamera);
                var follow = cameraHost.AddComponent<CameraFollow>(); follow.PopupOverlayCamera = PopupCamera;
                var uiHost = Child("UI controls", root.transform);
                Reader = uiHost.AddComponent<AnnouncementUI>();
                Reader.Tilemap = Renderer.CenteredPopupFgTilemap;
                Reader.BgTilemap = Renderer.CenteredPopupBgTilemap;
                Reader.PopupCamera = PopupCamera;
                Inventory = uiHost.AddComponent<InventoryUI>(); Inventory.Tilemap = Main;
                Input = uiHost.AddComponent<InputHandler>();
                Input.ZoneRenderer = Renderer; Input.CameraFollow = follow;
                Input.InventoryUI = Inventory; Input.AnnouncementUI = Reader;
            }
            public void SetCellWidth(float width)
            {
                PopupCamera.aspect = width * 80f / 45f;
                Renderer.SetPopupOverlayCamera(PopupCamera);
                Assert.That(Reader.BgTilemap.layoutGrid.cellSize.x, Is.EqualTo(width).Within(Epsilon));
            }
            public void Dispose()
            {
                Reader.Close(); Inventory.Close(); MainCamera.targetTexture = null;
                Object.DestroyImmediate(root);
                foreach (var host in detached) if (host != null) Object.DestroyImmediate(host);
                if (Display != null) Object.DestroyImmediate(Display);
                ZoneRenderHooks.CellDirtyCallback = priorCellDirty;
                ZoneRenderHooks.FullDirtyCallback = priorFullDirty;
                MessageLog.Restore(oldLog, oldAnnouncements, oldFlash, oldSerial);
                // Public Restore intentionally drops transient origin authority;
                // preserve the borrowed test/editor queue exactly as well.
                origins.GetType().GetMethod("Clear").Invoke(origins, null);
                foreach (object origin in oldOrigins)
                    origins.GetType().GetMethod("Enqueue").Invoke(origins, new[] { origin });
                typeof(MessageLog).GetProperty("Revision").SetValue(null, oldRevision);
                typeof(MessageLog).GetProperty("HistoryGeneration").SetValue(null, oldGeneration);
                MessageLog.OnMessage = oldMessage; MessageLog.TickProvider = oldTick;
                foreach (var camera in borrowedCameras) if (camera != null) camera.tag = "MainCamera";
            }
            static GameObject Child(string name, Transform parent)
            { var child = new GameObject(name); child.transform.SetParent(parent, false); return child; }
        }
    }
}
