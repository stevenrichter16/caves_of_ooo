using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CavesOfOoo.Tests
{
    public sealed class FellingCameraClearTests
    {
        private GameObject _root, _art;
        private Camera _camera;
        private CameraFollow _follow;
        private FellingScenePresenter _presenter;
        private Zone _zone;
        private Entity _player;

        [SetUp] public void SetUp()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            _zone = new Zone(FellingSiteBuilder.ZoneID); Assert.IsTrue(FellingSceneRuntime.Install(_zone, factory));
            _art = new GameObject("Letterbox source fixture"); _presenter = _art.AddComponent<FellingScenePresenter>(); _presenter.Bind(_zone);
            _root = new GameObject("Letterbox camera fixture"); _camera = _root.AddComponent<Camera>(); _camera.aspect = 16f / 9;
            _follow = _root.AddComponent<CameraFollow>();
            typeof(CameraFollow).GetField("_scenePresenter", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(_follow, _presenter);
            _player = new Entity { BlueprintName = "Player" }; _zone.AddEntity(_player, 55, 22);
            _follow.Player = _player; _follow.CurrentZone = _zone;
        }
        [TearDown] public void TearDown() { Object.DestroyImmediate(_root); Object.DestroyImmediate(_art); }
        private Camera Background() => _root.GetComponentsInChildren<Camera>(true).SingleOrDefault(c => c != _camera);

        [TestCase(640, 360)]
        [TestCase(640, 640)]
        public void RestoringTheSourceViewportClearsOldGpuPixelsOnlyInsideTheMap(int width, int height)
        {
            _camera.aspect = width / (float)height;
            var layout = GameplayViewportLayout.Measure(_camera, _follow.SidebarReferenceZoom, _follow.ReservedSidebarWidthChars, _follow.ReservedHotbarHeightRows);
            // Exercise the real full-viewport modal -> narrow source transition.
            _follow.SetUIView(80, 25); _follow.RestoreGameView();
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var readback = new Texture2D(width, height, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            try
            {
                target.Create(); RenderTexture.active = target; GL.Clear(true, true, Color.magenta);
                _camera.cullingMask = 0; _camera.backgroundColor = Color.cyan; _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
                // Use the actual configured camera order and shared destination.
                // With no margin clear, the excluded pixels retain the magenta sentinel.
                foreach (var camera in _root.GetComponentsInChildren<Camera>().Where(c => c.enabled).OrderBy(c => c.depth))
                {
                    var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                    Assert.IsTrue(RenderPipeline.SupportsRenderRequest(camera, request));
                    RenderPipeline.SubmitRenderRequest(camera, request);
                }
                RenderTexture.active = target; readback.ReadPixels(new Rect(0, 0, width, height), 0, 0); readback.Apply();
                int marginPixels = 0, changedMarginPixels = 0;
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                {
                    var point = new Vector2((x + 0.5f) / width, (y + 0.5f) / height);
                    Rect innerMap = new Rect(layout.MapRect.x + 2f / width, layout.MapRect.y + 2f / height,
                        layout.MapRect.width - 4f / width, layout.MapRect.height - 4f / height);
                    Rect paddedSource = new Rect(_camera.rect.x - 2f / width, _camera.rect.y - 2f / height,
                        _camera.rect.width + 4f / width, _camera.rect.height + 4f / height);
                    if (!innerMap.Contains(point) || paddedSource.Contains(point)) continue;
                    marginPixels++;
                    if (readback.GetPixel(x, y).maxColorComponent > 0.01f) changedMarginPixels++;
                }
                Assert.Greater(marginPixels, 500);
                Assert.AreEqual(0, changedMarginPixels, "Excluded map pixels must be black after restoring a narrower camera viewport.");
                Assert.Greater(readback.GetPixel(width - 2, height / 2).r, 0.9f, "The margin clear must not erase the sidebar region.");
                Assert.Greater(readback.GetPixel(width / 4, 2).r, 0.9f, "The margin clear must not erase the hotbar region.");
            }
            finally { RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(readback); }
        }

        [Test] public void MarginClearIsReusedDisabledOutsideSourceAndDestroyedWithItsOwner()
        {
            _follow.SnapToPlayer(); var background = Background(); Assert.IsNotNull(background);
            Assert.IsTrue(background.enabled); Assert.AreEqual(0, background.cullingMask); Assert.Less(background.depth, _camera.depth);
            Assert.AreEqual(Color.black, background.backgroundColor); Assert.AreEqual(CameraClearFlags.SolidColor, background.clearFlags);
            Assert.IsNull(background.targetTexture, "Normal play must not allocate a separate scene render texture.");
            _follow.SnapToPlayer(); Assert.AreSame(background, Background());
            _follow.SetOverrideTargetCell(0, 22); _follow.SnapToPlayer(); Assert.IsFalse(background.enabled);
            _follow.ClearOverrideTarget(); _follow.SnapToPlayer(); Assert.IsTrue(background.enabled);
            _presenter.SetPresentationVisible(false); _follow.SnapToPlayer(); Assert.IsFalse(background.enabled);
            _presenter.SetPresentationVisible(true); _follow.SnapToPlayer(); Assert.IsTrue(background.enabled);
            _follow.CurrentZone = new Zone("ordinary"); _follow.SnapToPlayer(); Assert.IsFalse(background.enabled);
            _follow.CurrentZone = _zone; _follow.SnapToPlayer(); Assert.IsTrue(background.enabled);
            // CameraFollow is not ExecuteAlways: edit-mode fixture creation does not
            // invoke its native Awake/OnDestroy lifecycle. Exercise its cleanup body.
            typeof(CameraFollow).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_follow, null);
            Assert.IsTrue(background == null); Object.DestroyImmediate(_follow); Assert.IsTrue(_camera != null);
        }

        [Test] public void PausedFullscreenAndDisabledCameraDoNotLeaveAnActiveClearPass()
        {
            _follow.SnapToPlayer(); var background = Background(); Assert.IsNotNull(background);
            _follow.SetUIView(80, 25); Assert.IsFalse(background.enabled);
            _follow.RestoreGameView(); Assert.IsTrue(background.enabled);
            _follow.SetFullscreenOverlayView(); Assert.IsFalse(background.enabled);
            _follow.RestoreGameView(); _follow.SetCenteredPopupOverlayView();
            Assert.IsTrue(background.enabled, "A centered map popup keeps the source and its black margins underneath.");
            _follow.RestoreGameView(); _camera.enabled = false; _follow.SnapToPlayer(); Assert.IsFalse(background.enabled);
            _camera.enabled = true; _follow.SnapToPlayer(); Assert.IsTrue(background.enabled);
            typeof(CameraFollow).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(_follow, null);
            Assert.IsFalse(background.enabled);
        }
    }
}
