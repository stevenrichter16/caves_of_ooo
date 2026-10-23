using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelCameraFramingTests
    {
        private readonly List<Object> _owned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var owned in _owned)
                if (owned is GameObject host && host != null && host.TryGetComponent<Camera>(out var camera))
                    camera.targetTexture = null;
            for (int i = _owned.Count - 1; i >= 0; i--)
                if (_owned[i] != null) Object.DestroyImmediate(_owned[i]);
            _owned.Clear();
        }

        [Test]
        public void DisplayAspect_UsesOutputPixelsDespiteStaleProjection_AndObservesResize()
        {
            var camera = CreateCamera(1280, 800);
            camera.rect = new Rect(0, .15f, .8f, .85f);
            camera.aspect = 1.97277653f;
            Assert.AreEqual(1.6f, GameplayViewportLayout.GetDisplayAspect(camera), .0001f);
            camera.targetTexture = Own(new RenderTexture(1024, 768, 0));
            Assert.AreEqual(4f / 3f, GameplayViewportLayout.GetDisplayAspect(camera), .0001f);
            Assert.AreEqual(1.97277653f, camera.aspect, .0001f, "Measuring does not mutate a borrowed projection.");
        }

        [Test]
        public void DisplayAspect_UsesViewportPixelsWithoutRenderTexture()
        {
            var camera = CreateCamera(1280, 800);
            camera.targetTexture = null;
            camera.pixelRect = new Rect(20, 30, 640, 400);
            camera.aspect = 9f;
            float expected = (camera.pixelRect.width / camera.rect.width) / (camera.pixelRect.height / camera.rect.height);
            Assert.AreEqual(expected, GameplayViewportLayout.GetDisplayAspect(camera), .0001f);
        }

        [TestCase(1280, 800)]
        [TestCase(1281, 801)]
        [TestCase(1920, 1080)]
        [TestCase(3840, 2160)]
        public void FlatWorld_UsesSquareNativePixelsAtIntegerScale_WithAlignedEdges(int width, int height)
        {
            var follow = CreateWorld(width, height, out var camera);
            SetPixelDensity(follow, 16);
            follow.SnapToPlayer();
            float pixelWidth = width * camera.rect.width;
            float pixelHeight = height * camera.rect.height;
            float pixelsX = pixelWidth / (camera.orthographicSize * 2f * camera.aspect);
            float pixelsY = pixelHeight / (camera.orthographicSize * 2f);
            float scale = pixelsY / 16f;
            Assert.AreEqual(pixelsX, pixelsY, .001f, "Pixel cells must not squeeze horizontally.");
            Assert.GreaterOrEqual(scale, 1f, "No native source pixel may be undersampled.");
            Assert.AreEqual(Mathf.Round(scale), scale, .001f);
            float left = camera.transform.position.x * pixelsX - pixelWidth * .5f - camera.pixelRect.x;
            float bottom = camera.transform.position.y * pixelsY - pixelHeight * .5f - camera.pixelRect.y;
            Assert.AreEqual(Mathf.Round(left), left, .001f, "Odd viewport widths still align source pixel edges.");
            Assert.AreEqual(Mathf.Round(bottom), bottom, .001f);
            if (width == 1280) Assert.AreEqual(21.25f, camera.orthographicSize, .001f);
        }

        [Test]
        public void FullscreenRoundTripsUsePhysicalAspect_AndBypassWorldPixelZoom()
        {
            var follow = CreateWorld(1280, 800, out var camera);
            SetPixelDensity(follow, 16);
            for (int i = 0; i < 2; i++)
            {
                camera.aspect = 4f;
                follow.SetUIView(80, 45);
                Assert.AreEqual(1.6f, camera.aspect, .0001f);
                Assert.AreEqual(25.65f, camera.orthographicSize, .001f);
                follow.RestoreGameView();
                Assert.AreEqual(21.25f, camera.orthographicSize, .001f);
                Assert.AreEqual((1280f * camera.rect.width) / (800f * camera.rect.height), camera.aspect, .0001f);
            }
        }

        [Test]
        public void UnoptedProjectedWorldRetainsRowZoomAndMultiplier()
        {
            var follow = CreateWorld(1280, 800, out var camera);
            follow.SnapToPlayer();
            Assert.AreEqual(30.6f, camera.orthographicSize, .001f);
        }

        [Test]
        public void ProjectionOwnerBorrowsAndRestoresPixelDensity_ForFullscreenAndExit()
        {
            var follow = CreateWorld(1280, 800, out var camera);
            SetPixelDensity(follow, 8);
            using (var projection = new FlatPixelWorldProjection(follow.WorldTilemap, follow, null))
            {
                projection.Apply(true, false);
                Assert.AreEqual(16, GetPixelDensity(follow));
                projection.Apply(true, true);
                Assert.AreEqual(8, GetPixelDensity(follow));
                projection.Apply(true, false);
                Assert.AreEqual(16, GetPixelDensity(follow));
            }
            Assert.AreEqual(8, GetPixelDensity(follow));
        }

        private Camera CreateCamera(int width, int height)
        {
            var target = Own(new RenderTexture(width, height, 0));
            var host = Own(new GameObject("Flat pixel camera"));
            var camera = host.AddComponent<Camera>();
            camera.targetTexture = target;
            camera.orthographic = true;
            camera.aspect = 1.97277653f;
            camera.transform.position = new Vector3(0, 0, -10);
            return camera;
        }

        private CameraFollow CreateWorld(int width, int height, out Camera camera)
        {
            camera = CreateCamera(width, height);
            var world = Own(new GameObject("Flat camera grid", typeof(Grid)));
            world.GetComponent<Grid>().cellSize = new Vector3(1, 1.5f, 1);
            var host = new GameObject("Flat camera map", typeof(Tilemap));
            host.transform.SetParent(world.transform, false);
            var zone = new Zone("FlatCameraFraming");
            var player = new Entity { BlueprintName = "Player" };
            zone.AddEntity(player, 40, 12);
            var follow = camera.gameObject.AddComponent<CameraFollow>();
            follow.Player = player;
            follow.CurrentZone = zone;
            follow.WorldTilemap = host.GetComponent<Tilemap>();
            follow.TargetVisibleTileRows = 34;
            follow.GameplayZoomMultiplier = 1.2f;
            return follow;
        }

        private static void SetPixelDensity(CameraFollow follow, int value) => follow.WorldPixelsPerUnit = value;
        private static int GetPixelDensity(CameraFollow follow) => follow.WorldPixelsPerUnit;
        private T Own<T>(T value) where T : Object { _owned.Add(value); return value; }
    }
}
