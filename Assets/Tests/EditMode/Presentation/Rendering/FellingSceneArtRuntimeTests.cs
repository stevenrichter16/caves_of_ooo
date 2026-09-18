using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FellingSceneArtRuntimeTests
    {
        private GameObject _go;
        private Zone _zone;
        private FellingScenePresenter _presenter;

        [SetUp] public void SetUp()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            _zone = new Zone(FellingSiteBuilder.ZoneID);
            Assert.IsTrue(FellingSceneRuntime.Install(_zone, factory));
            SetVisibility(true, true);
            _go = new GameObject("Felling art runtime test");
            _presenter = _go.AddComponent<FellingScenePresenter>();
            _presenter.Bind(_zone);
            Assert.IsTrue(_presenter.IsReady, _presenter.Failure);
        }
        [TearDown] public void TearDown() { if (_go != null) Object.DestroyImmediate(_go); }
        private void SetVisibility(bool explored, bool visible)
        {
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++)
            { var cell = _zone.GetCell(x, y); cell.Explored = explored; cell.IsVisible = visible; }
        }
        private SpriteRenderer View(string name) => _go.GetComponentsInChildren<SpriteRenderer>(true).Single(r => r.name == name);

        [Test]
        public void EveryNativeLayerAndContactHasItsOwnImportedRenderer()
        {
            Assert.AreEqual(55, _presenter.ComponentCount);
            Assert.AreEqual(95, _go.GetComponentsInChildren<SpriteRenderer>(true).Length);
            Assert.IsTrue(_presenter.ClaimsCell(16, 0));
            Assert.IsTrue(_presenter.ClaimsCell(63, 24));
            Assert.IsFalse(_presenter.ClaimsCell(15, 0));
            Assert.IsFalse(_presenter.ClaimsCell(64, 24));
            foreach (var layer in FellingSceneDefinition.Load().layers)
            {
                var sprite = View(layer.id).sprite;
                Assert.AreEqual(new Vector2(layer.bounds[2], layer.bounds[3]), sprite.rect.size, layer.id);
                Assert.AreEqual(32, sprite.pixelsPerUnit, layer.id);
                Assert.AreEqual(Vector2.zero, sprite.pivot, layer.id);
                Assert.AreEqual(FilterMode.Point, sprite.texture.filterMode, layer.id);
                Assert.IsTrue(sprite.texture.isReadable, layer.id);
            }
        }

        [Test]
        public void RemovingAnOwnerHidesOnlyItsSpriteAndContactAndSurvivesPresentationToggle()
        {
            var layer = FellingSceneDefinition.Load().layers.First(l => l.mutable);
            Entity owner = FellingSceneRuntime.FindOwner(_zone, layer.id);
            Assert.IsTrue(_zone.RemoveEntity(owner));
            _presenter.Refresh();
            Assert.IsFalse(View(layer.id).enabled);
            Assert.IsFalse(View(layer.id + " contact").enabled);
            Assert.IsTrue(View("stump-main").enabled);
            _presenter.SetPresentationVisible(false);
            Assert.IsFalse(_presenter.ClaimsCell(40, 20));
            _presenter.SetPresentationVisible(true);
            Assert.IsFalse(View(layer.id).enabled);
            Assert.IsTrue(_presenter.ClaimsCell(40, 20));
            _presenter.Bind(new Zone("other"));
            Assert.IsFalse(_presenter.IsReady);
            Assert.AreEqual(0, _go.GetComponentsInChildren<SpriteRenderer>(true).Length);
        }

        [Test]
        public void PickingUsesVisibleAlphaAndNeverSelectsRemovedOrFoggedOwner()
        {
            var layer = FellingSceneDefinition.Load().layers.First(l => l.mutable);
            Color32[] pixels = View(layer.id).sprite.texture.GetPixels32();
            int opaque = System.Array.FindIndex(pixels, p => p.a != 0);
            int empty = System.Array.FindIndex(pixels, p => p.a == 0);
            Assert.GreaterOrEqual(opaque, 0); Assert.GreaterOrEqual(empty, 0);
            Vector2 Pixel(int index) => new Vector2(layer.bounds[0] + index % layer.bounds[2] + 0.5f,
                layer.bounds[1] + layer.bounds[3] - 1 - index / layer.bounds[2] + 0.5f);
            Entity target = FellingSceneRuntime.FindOwner(_zone, layer.id);
            Assert.IsTrue(_presenter.TryPickImage(Pixel(opaque), out _, out _, out var selected));
            Assert.AreSame(target, selected);
            _presenter.TryPickImage(Pixel(empty), out _, out _, out selected);
            Assert.AreNotSame(target, selected, "Transparent sprite bounds must fall through.");
            SetVisibility(false, false); _presenter.Refresh();
            Assert.IsFalse(_presenter.TryPickImage(Pixel(opaque), out _, out _, out _));
            SetVisibility(true, true);
            _zone.RemoveEntity(target); _presenter.Refresh();
            _presenter.TryPickImage(Pixel(opaque), out _, out _, out selected);
            Assert.AreNotSame(target, selected);
        }

        [Test]
        public void VisibleCliffColumnCanBeInspectedWithoutRevealingAnotherColumnOrItsHiddenAnchor()
        {
            var definition = FellingSceneDefinition.Load();
            var trunk = definition.layers.Single(l => l.id == "stump-main");
            var pixels = View(trunk.id).sprite.texture.GetPixels32();
            int[] rows = new int[48];
            foreach (var cell in definition.cells)
                if (cell.opaque && cell.x >= 16 && cell.x < 64) rows[cell.x - 16] = Mathf.Max(rows[cell.x - 16], cell.y);
            int index = -1;
            for (int i = 0; i < pixels.Length; i++)
            {
                int sx = trunk.bounds[0] + i % trunk.bounds[2];
                int sy = trunk.bounds[1] + trunk.bounds[3] - 1 - i / trunk.bounds[2];
                if (pixels[i].a > 0 && sy < 224 && sx / 32 + 16 != trunk.anchorX) { index = i; break; }
            }
            Assert.GreaterOrEqual(index, 0);
            var sample = new Vector2(trunk.bounds[0] + index % trunk.bounds[2] + 0.5f,
                trunk.bounds[1] + trunk.bounds[3] - 1 - index / trunk.bounds[2] + 0.5f);
            int column = Mathf.FloorToInt(sample.x / 32);
            var sampleCell = _zone.GetCell(16 + column, rows[column]);
            SetVisibility(false, false);
            sampleCell.IsVisible = sampleCell.Explored = true;
            _presenter.Refresh();
            Assert.IsTrue(_presenter.TryPickImage(sample, out _, out _, out var selected));
            Assert.AreSame(FellingSceneRuntime.FindOwner(_zone, trunk.id), selected);
            var fog = (Texture2D)View("Backing").sharedMaterial.GetTexture("_FellingFog");
            Assert.Greater(fog.GetPixel(column, 31).r, 0.99f);
            Assert.AreEqual(0, fog.GetPixel((column + 1) % 48, 31).r);
            SetVisibility(false, false);
            var anchor = _zone.GetCell(trunk.anchorX, trunk.anchorY);
            anchor.IsVisible = anchor.Explored = true;
            _presenter.Refresh();
            Assert.IsFalse(_presenter.TryPickImage(sample, out _, out _, out _), "The hidden face must not become selectable just because its entity anchor is visible.");
        }

        [Test]
        public void CameraUsesArtBoundsAndRestoresNormalFollowAfterToggleAndDeparture()
        {
            var cameraObject = new GameObject("Felling camera test");
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                var follow = cameraObject.AddComponent<CameraFollow>();
                typeof(CameraFollow).GetField("_scenePresenter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(follow, _presenter);
                var player = new Entity { BlueprintName = "Player" };
                _zone.AddEntity(player, 40, 20);
                follow.CurrentZone = _zone; follow.Player = player;
                follow.SnapToPlayer();
                Assert.AreEqual(40, cameraObject.transform.position.x);
                Assert.AreEqual(16, cameraObject.transform.position.y);
                Assert.AreEqual(FellingScenePresenter.CameraHalfHeight(camera.aspect), camera.orthographicSize);
                _presenter.SetPresentationVisible(false); follow.SnapToPlayer();
                Assert.AreEqual(17, camera.orthographicSize);
                Assert.AreEqual(12.5f, cameraObject.transform.position.y);
                _presenter.SetPresentationVisible(true); follow.SnapToPlayer();
                Assert.AreEqual(16, cameraObject.transform.position.y);
                _zone.RemoveEntity(player);
                var ordinary = new Zone("ordinary"); ordinary.AddEntity(player, 30, 10);
                follow.CurrentZone = ordinary; follow.SnapToPlayer();
                Assert.AreEqual(17, camera.orthographicSize);
                Assert.AreEqual(12.5f, cameraObject.transform.position.y);
            }
            finally { Object.DestroyImmediate(cameraObject); }
        }

        [Test]
        public void ImportedRendererOrderReconstructsTheActorFreeSourcePixelForPixel()
        {
            // This checks imported pixel data and the ACTUAL configured renderer order.
            // GPU lighting/postprocess output remains a separate live-capture check.
            var expectedTexture = new Texture2D(2, 2);
            try
            {
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../ArtSource/FellingSite/Components/build/baseline.png"));
                Assert.IsTrue(expectedTexture.LoadImage(File.ReadAllBytes(path)));
                Color32[] expected = expectedTexture.GetPixels32();
                var composed = new Color32[1536 * 1024];
                int nonbinary = 0;
                foreach (var renderer in _go.GetComponentsInChildren<SpriteRenderer>().OrderBy(r => r.sortingOrder).ThenByDescending(r => r.transform.position.z))
                {
                    var pixels = renderer.sprite.texture.GetPixels32();
                    int width = renderer.sprite.texture.width;
                    int height = renderer.sprite.texture.height;
                    int left = Mathf.RoundToInt((renderer.transform.position.x - 16) * 32);
                    int bottom = Mathf.RoundToInt(renderer.transform.position.y * 32);
                    for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                    {
                        Color32 source = pixels[y * width + x];
                        if (source.a != 0 && source.a != 255) nonbinary++;
                        if (source.a != 0) composed[(bottom + y) * 1536 + left + x] = source;
                    }
                }
                int differences = 0;
                for (int i = 0; i < expected.Length; i++) if (!expected[i].Equals(composed[i])) differences++;
                Assert.AreEqual(0, nonbinary, "Native source contributions require binary ownership alpha.");
                Assert.AreEqual(0, differences, "Changing actor depth must not reorder plants/contact contributions beneath their structural backing.");
            }
            finally { Object.DestroyImmediate(expectedTexture); }
        }

        [TestCase(false, 0)]
        [TestCase(false, 79)]
        [TestCase(true, 0)]
        [TestCase(true, 79)]
        public void PlayerAndLookTargetsOutsideTheArtworkStayVisibleOnApproachRoutes(bool useLook, int outsideX)
        {
            var cameraObject = new GameObject("Felling edge camera test");
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true; camera.aspect = 1;
                var follow = cameraObject.AddComponent<CameraFollow>();
                typeof(CameraFollow).GetField("_scenePresenter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(follow, _presenter);
                var player = new Entity { BlueprintName = "Player" };
                _zone.AddEntity(player, 40, 20);
                follow.CurrentZone = _zone; follow.Player = player;
                follow.SnapToPlayer();
                Assert.AreEqual(40, cameraObject.transform.position.x, "Inside the art, retain the full source composition.");
                if (useLook) follow.SetOverrideTargetCell(outsideX, 20);
                else { _zone.RemoveEntity(player); _zone.AddEntity(player, outsideX, 20); }
                follow.SnapToPlayer();
                float targetX = outsideX + 0.5f;
                float halfWidth = camera.orthographicSize * camera.aspect;
                Assert.GreaterOrEqual(targetX, cameraObject.transform.position.x - halfWidth, "The tracked target must not disappear beyond the left edge.");
                Assert.LessOrEqual(targetX, cameraObject.transform.position.x + halfWidth, "The tracked target must not disappear beyond the right edge.");
                Assert.AreEqual(16, cameraObject.transform.position.y, "Horizontal approach panning keeps the upper trunk's vertical framing.");
                follow.ClearOverrideTarget();
                _zone.RemoveEntity(player); _zone.AddEntity(player, 40, 20);
                follow.SnapToPlayer();
                Assert.AreEqual(40, cameraObject.transform.position.x, "Returning to the art restores the authored composition.");
            }
            finally { Object.DestroyImmediate(cameraObject); }
        }

        [TestCase(40, 12)]
        [TestCase(40, 20)]
        public void ActualArrivalFovShowsTheCliffFacadeFromItsLocalFrontGround(int x, int y)
        {
            SetVisibility(false, false);
            FieldOfView.Compute(_zone, x, y, 999);
            _presenter.Refresh();
            var fog = (Texture2D)View("Backing").sharedMaterial.GetTexture("_FellingFog");
            for (int column = 0; column < 48; column++)
                Assert.Greater(fog.GetPixel(column, 31).r, 0.99f, "The local facade in column " + column + " is observed from the arrival clearing.");
        }

        [TestCase(1.7777778f)]
        [TestCase(1f)]
        public void SourceCameraLetterboxesOnlyTheMapAndRestoresApproachAndUiRects(float displayAspect)
        {
            var cameraObject = new GameObject("Felling letterbox camera test");
            try
            {
                Camera ChildCamera(string name)
                {
                    var child = new GameObject(name); child.transform.SetParent(cameraObject.transform);
                    return child.AddComponent<Camera>();
                }
                void AssertRect(Rect expected, Rect actual, string reason = null)
                {
                    Assert.AreEqual(expected.x, actual.x, 0.0001f, reason);
                    Assert.AreEqual(expected.y, actual.y, 0.0001f, reason);
                    Assert.AreEqual(expected.width, actual.width, 0.0001f, reason);
                    Assert.AreEqual(expected.height, actual.height, 0.0001f, reason);
                }
                var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.aspect = displayAspect;
                var follow = cameraObject.AddComponent<CameraFollow>();
                follow.SidebarCamera = ChildCamera("sidebar"); follow.HotbarCamera = ChildCamera("hotbar");
                follow.PopupOverlayCamera = ChildCamera("popup");
                typeof(CameraFollow).GetField("_scenePresenter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(follow, _presenter);
                var player = new Entity { BlueprintName = "Player" }; _zone.AddEntity(player, 40, 20);
                follow.CurrentZone = _zone; follow.Player = player;
                var layout = GameplayViewportLayout.Measure(camera, follow.SidebarReferenceZoom, follow.ReservedSidebarWidthChars, follow.ReservedHotbarHeightRows);
                follow.SnapToPlayer();
                Assert.AreEqual(1.5f, camera.aspect, 0.0001f, "Fit the 3:2 source instead of exposing unrelated exterior floor strips.");
                Assert.AreEqual(16, camera.orthographicSize, 0.0001f);
                Assert.AreEqual(layout.MapRect.center.x, camera.rect.center.x, 0.0001f);
                Assert.AreEqual(layout.MapRect.center.y, camera.rect.center.y, 0.0001f);
                Assert.LessOrEqual(camera.rect.width, layout.MapRect.width + 0.0001f);
                Assert.LessOrEqual(camera.rect.height, layout.MapRect.height + 0.0001f);
                Assert.AreNotEqual(layout.MapRect, camera.rect);
                AssertRect(layout.SidebarRect, follow.SidebarCamera.rect);
                AssertRect(layout.HotbarRect, follow.HotbarCamera.rect);
                follow.SetOverrideTargetCell(0, 20); follow.SnapToPlayer();
                AssertRect(layout.MapRect, camera.rect, "Look on the exterior approach restores the full map viewport.");
                follow.ClearOverrideTarget(); _zone.MoveEntity(player, 79, 20); follow.SnapToPlayer();
                AssertRect(layout.MapRect, camera.rect, "Walking out restores the full map viewport.");
                _zone.MoveEntity(player, 40, 20); follow.SnapToPlayer();
                Assert.AreEqual(1.5f, camera.aspect, 0.0001f);
                follow.SetUIView(80, 25); Assert.AreEqual(new Rect(0, 0, 1, 1), camera.rect);
                follow.RestoreGameView(); Assert.AreEqual(1.5f, camera.aspect, 0.0001f);
                _presenter.SetPresentationVisible(false); follow.SnapToPlayer();
                AssertRect(layout.MapRect, camera.rect, "The glyph mode retains ordinary viewport dimensions.");
                _presenter.SetPresentationVisible(true); follow.CurrentZone = new Zone("ordinary");
                _zone.RemoveEntity(player); follow.CurrentZone.AddEntity(player, 40, 20); follow.SnapToPlayer();
                AssertRect(layout.MapRect, camera.rect, "The scene profile must not leak into another zone.");
            }
            finally { Object.DestroyImmediate(cameraObject); }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void FacadeObservationBandDoesNotRevealUnseenGroundOrOtherColumns(int southOffset)
        {
            int column = 24;
            int boundary = FellingSceneDefinition.Load().cells.Where(c => c.x == column + 16 && c.opaque).Max(c => c.y);
            SetVisibility(false, false);
            var observation = _zone.GetCell(column + 16, boundary + southOffset);
            observation.IsVisible = observation.Explored = true;
            _presenter.Refresh();
            var fog = (Texture2D)View("Backing").sharedMaterial.GetTexture("_FellingFog");
            Assert.Greater(fog.GetPixel(column, 31).r, 0.99f);
            Assert.AreEqual(0, fog.GetPixel(column + 1, 31).r);
            int hiddenGroundY = boundary + (southOffset == 1 ? 2 : 1);
            Assert.AreEqual(0, fog.GetPixel(column, 31 - (7 + hiddenGroundY)).r, "Wall projection must not reveal the ground between observation samples.");
            SetVisibility(false, false); _presenter.Refresh();
            Assert.AreEqual(0, fog.GetPixel(column, 31).r, "With every local observation hidden, the facade remains hidden.");
        }

        [TestCase(true, true, true)]
        [TestCase(false, true, true)]
        [TestCase(true, false, true)]
        [TestCase(true, true, false)]
        public void ActorTintPreservesSourceColorOnlyInVisibleFellingArt(bool inside, bool visible, bool artEnabled)
        {
            var rendererObject = new GameObject("Felling actor color test"); rendererObject.SetActive(false);
            try
            {
                var renderer = rendererObject.AddComponent<ZoneRenderer>();
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(ZoneRenderer).GetField("_fellingScenePresenter", flags).SetValue(renderer, _presenter);
                renderer.SetZone(_zone); _presenter.SetPresentationVisible(artEnabled);
                _zone.AmbientLevel = 0.2f;
                var lighting = new LightMap(); lighting.Compute(_zone);
                typeof(ZoneRenderer).GetField("_lightMap", flags).SetValue(renderer, lighting);
                int x = inside ? 40 : 8, y = 20;
                var actor = new Entity { BlueprintName = "GlasspaneFrog" }; _zone.AddEntity(actor, x, y);
                _zone.GetCell(x, y).IsVisible = visible;
                Color ordinary = lighting.ApplyToColor(Color.white, x, y);
                Assert.Less(ordinary.maxColorComponent, 0.99f, "The ordinary lightmap must attenuate this fixture.");
                Color actual = (Color)typeof(ZoneRenderer).GetMethod("ResolveEntityVisualTint", flags)
                    .Invoke(renderer, new object[] { actor, x, y });
                Assert.AreEqual(inside && visible && artEnabled ? Color.white : ordinary, actual);
            }
            finally { Object.DestroyImmediate(rendererObject); }
        }

        [TestCase(true, false)]
        [TestCase(false, false)]
        [TestCase(true, true)]
        public void NativeRiverDoesNotPaintDuplicateWaterGlyphsButSpillsAndOilRemainVisible(bool river, bool oil)
        {
            var rendererObject = new GameObject("Felling coating paint test");
            rendererObject.SetActive(false);
            try
            {
                var renderer = rendererObject.AddComponent<ZoneRenderer>();
                var marks = rendererObject.GetComponent<UnityEngine.Tilemaps.Tilemap>();
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(ZoneRenderer).GetField("_fellingScenePresenter", flags).SetValue(renderer, _presenter);
                typeof(ZoneRenderer).GetField("_tileStateTilemap", flags).SetValue(renderer, marks);
                renderer.SetZone(_zone);
                var water = FellingSceneDefinition.Load().cells.First(c => c.water && c.x >= 16 && c.x < 64);
                int x = river ? water.x : 40, y = river ? water.y : 20;
                _zone.TileState.WriteCoating(x, y, "water", ZoneTileState.Permanent);
                if (oil) _zone.TileState.WriteCoating(x, y, "oil", 5);
                var pos = new Vector3Int(x, 24 - y, 0);
                typeof(ZoneRenderer).GetMethod("PaintTileStateMark", flags).Invoke(renderer, new object[] { x, y, pos, _zone.GetCell(x, y) });
                if (river && !oil) Assert.IsNull(marks.GetTile(pos), "The river already represents its own permanent water coating.");
                else Assert.IsNotNull(marks.GetTile(pos), "New spilled water or an oil coating must still be legible.");
            }
            finally { Object.DestroyImmediate(rendererObject); }
        }
    }
}
