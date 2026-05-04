using System.Reflection;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests.EditMode.Presentation.SceneViews
{
    /// <summary>
    /// Bug A TDD tests: when a Scene View activates, <see cref="SceneViewUI"/>
    /// must hand control of the gameplay camera to its full-screen UI-view
    /// mode (<c>CameraFollow.SetUIView</c>) so the sidebar and hotbar
    /// cameras' black-clear backgrounds don't paint as black bars on the
    /// right and bottom of the screen around the scene composition.
    ///
    /// <para>Observable side effect of <c>SetUIView</c>:
    /// <c>SidebarCamera.enabled</c> flips from true to false (per
    /// <c>CameraFollow.ApplyUIViewLayout</c> which disables sidebar /
    /// hotbar / popup-overlay cameras). Asserting on that flag is the
    /// most direct unit-level proof that SetUIView ran without coupling
    /// the test to the rest of the layout math.</para>
    ///
    /// <para>Counter-check pairs the positive assertion with an unknown
    /// SceneID — <c>SceneViewUI.HandleActivated</c> bails when the ID
    /// isn't recognized, so the camera is left untouched.</para>
    ///
    /// <para>Plan: Docs/Plans/SCENE_VIEW_HANDOFF_2.md → "Bug A".</para>
    /// </summary>
    [TestFixture]
    public class SceneViewUICameraFramingTests
    {
        [SetUp]
        public void SetUp()
        {
            // Static SceneViewManager state — must be clean per test, both
            // ActiveSceneID and the OnActivated/OnDeactivated subscriber lists
            // (otherwise stale SceneViewUI subs from a prior test fire).
            SceneViewManager.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            SceneViewManager.Reset();

            foreach (var ui in Object.FindObjectsOfType<SceneViewUI>())
                Object.DestroyImmediate(ui.gameObject);
            foreach (var follow in Object.FindObjectsOfType<CameraFollow>())
                Object.DestroyImmediate(follow.gameObject);
            foreach (var camera in Object.FindObjectsOfType<Camera>())
                Object.DestroyImmediate(camera.gameObject);
            foreach (var tilemap in Object.FindObjectsOfType<Tilemap>())
                Object.DestroyImmediate(tilemap.gameObject);
            foreach (var grid in Object.FindObjectsOfType<Grid>())
                Object.DestroyImmediate(grid.gameObject);
        }

        [Test]
        public void HandleActivated_Campfire_DisablesSidebarCameraViaSetUIView()
        {
            var (ui, sidebar, hotbar) = BuildSceneAndWire();
            // Pre-condition: sidebar/hotbar are on (the normal split-layout
            // state). Without this baseline the post-assertion is vacuous.
            Assert.IsTrue(sidebar.enabled,
                "Setup precondition: sidebar camera must start enabled.");
            Assert.IsTrue(hotbar.enabled,
                "Setup precondition: hotbar camera must start enabled.");

            // Public-API trigger — same code path the real game uses.
            SceneViewManager.Activate("Campfire");

            Assert.IsFalse(sidebar.enabled,
                "SceneViewUI.HandleActivated must call CameraFollow.SetUIView, " +
                "which disables the SidebarCamera so its Color.black clear no " +
                "longer paints a black bar on the right of the scene.");
            Assert.IsFalse(hotbar.enabled,
                "Same for the HotbarCamera (bottom black bar).");
        }

        // Counter-check (CLAUDE.md §3.4): an unknown SceneID must NOT trigger
        // the camera switch — HandleActivated returns early on `sceneID !=
        // "Campfire"`. Without this counter-check, a buggy implementation
        // that ALWAYS calls SetUIView regardless of sceneID would still
        // pass the positive test above.
        [Test]
        public void HandleActivated_UnknownScene_LeavesSidebarCameraEnabled()
        {
            var (ui, sidebar, hotbar) = BuildSceneAndWire();
            Assert.IsTrue(sidebar.enabled, "Setup precondition.");

            SceneViewManager.Activate("UnknownScene");

            Assert.IsTrue(sidebar.enabled,
                "Activate with an unrecognized SceneID must NOT switch the " +
                "camera into UI view — HandleActivated bails before the " +
                "SetUIView call.");
            Assert.IsTrue(hotbar.enabled,
                "Same for HotbarCamera — must remain enabled.");
        }

        // ============================================================
        // Helpers
        // ============================================================

        private static (SceneViewUI ui, Camera sidebar, Camera hotbar) BuildSceneAndWire()
        {
            // Main camera + CameraFollow.
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var mainCam = camGo.AddComponent<Camera>();
            mainCam.orthographic = true;
            mainCam.aspect = 16f / 9f;
            var follow = camGo.AddComponent<CameraFollow>();
            // EditMode tests don't auto-fire MonoBehaviour Awake; mirror the
            // InputHandlerLookModeTests pattern of explicit reflection-invoke.
            InvokeNonPublic(follow, "Awake");

            // Sidebar + Hotbar cameras — their .enabled flag is the
            // observable assertion target (toggled by ApplyUIViewLayout
            // / ApplyGameplayLayout on the CameraFollow).
            var sidebarGo = new GameObject("Sidebar Camera");
            var sidebar = sidebarGo.AddComponent<Camera>();
            sidebar.enabled = true;

            var hotbarGo = new GameObject("Hotbar Camera");
            var hotbar = hotbarGo.AddComponent<Camera>();
            hotbar.enabled = true;

            follow.SidebarCamera = sidebar;
            follow.HotbarCamera = hotbar;

            // Tilemap + Grid for SceneViewUI.
            var gridGo = new GameObject("Grid");
            gridGo.AddComponent<Grid>();
            var tilemapGo = new GameObject("SceneTilemap");
            tilemapGo.transform.SetParent(gridGo.transform, false);
            var tilemap = tilemapGo.AddComponent<Tilemap>();
            tilemapGo.AddComponent<TilemapRenderer>();

            // SceneViewUI itself.
            var uiGo = new GameObject("SceneViewUI");
            var ui = uiGo.AddComponent<SceneViewUI>();
            ui.Tilemap = tilemap;
            ui.CameraFollow = follow;
            // Same EditMode quirk as above — manually run Awake then
            // OnEnable so the SceneViewManager.OnActivated subscription
            // is registered before we fire Activate.
            InvokeNonPublic(ui, "Awake");
            InvokeNonPublic(ui, "OnEnable");

            return (ui, sidebar, hotbar);
        }

        private static object InvokeNonPublic(object instance, string methodName, params object[] args)
        {
            MethodInfo method = instance.GetType().GetMethod(
                methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method,
                $"Reflection lookup failed for non-public method '{methodName}' " +
                $"on {instance.GetType().Name}. The method may have been renamed " +
                $"or its access modifier changed; update the test or the " +
                $"production code together.");
            return method.Invoke(instance, args);
        }
    }
}
