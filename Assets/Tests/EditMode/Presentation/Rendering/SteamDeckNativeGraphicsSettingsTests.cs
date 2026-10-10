using System;
using System.Reflection;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class SteamDeckNativeGraphicsSettingsTests
    {
        static void Configured(NativeZone3DRenderSurface surface, Camera camera, float scale, bool shadows)
        {
            var method = typeof(NativeZone3DRenderSurface).GetMethod("SyncConfigured", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method, "The explicit-resolution/shadow surface contract must exist independently of legacy low detail.");
            try { method.Invoke(surface, new object[] { camera, true, scale, shadows }); }
            catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        }
        [TestCase(.75f, false)][TestCase(.75f, true)][TestCase(1f, false)][TestCase(1f, true)]
        public void NativeResolutionAndShadowsAreIndependentAndLeaveBorrowedHudTargetFullSize(float scale, bool shadows)
        {
            var library = Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
            var root = new GameObject("Independent graphics fixture"); var target = new RenderTexture(640, 400, 16);
            try
            {
                target.Create(); var camera = root.AddComponent<Camera>(); camera.enabled = false; camera.orthographic = true;
                camera.orthographicSize = 12.75f; camera.targetTexture = target; camera.aspect = 1.6f;
                using (var surface = new NativeZone3DRenderSurface(root.transform, library.Renderer, library.RendererIndex,
                    library.CompositeMaterial, new[] { library.WorldMaterial, library.WaterMaterial }, 2.2f))
                {
                    Configured(surface, camera, scale, shadows);
                    Assert.True(surface.IsVisible); Assert.AreEqual(Mathf.RoundToInt(640 * scale), surface.WorldCamera.targetTexture.width);
                    Assert.AreEqual(Mathf.RoundToInt(400 * scale), surface.WorldCamera.targetTexture.height);
                    Assert.AreEqual(shadows ? LightShadows.Soft : LightShadows.None, surface.Sun.shadows);
                    Assert.AreSame(target, camera.targetTexture); Assert.AreEqual(640, target.width); Assert.AreEqual(400, target.height);
                    var cached = surface.WorldCamera.targetTexture; Configured(surface, camera, scale, !shadows);
                    Assert.AreSame(cached, surface.WorldCamera.targetTexture); Assert.AreEqual(shadows ? LightShadows.None : LightShadows.Soft, surface.Sun.shadows);
                    surface.Sync(camera, true, false); Assert.AreEqual(640, surface.WorldCamera.targetTexture.width); Assert.AreEqual(LightShadows.Soft, surface.Sun.shadows);
                    surface.Sync(camera, true, true); Assert.AreEqual(480, surface.WorldCamera.targetTexture.width); Assert.AreEqual(LightShadows.None, surface.Sun.shadows);
                }
            }
            finally { Object.DestroyImmediate(root); target.Release(); Object.DestroyImmediate(target); }
        }
    }
}
