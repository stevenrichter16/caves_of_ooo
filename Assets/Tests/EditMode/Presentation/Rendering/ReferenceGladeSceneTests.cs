#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

namespace CavesOfOoo.Tests
{
    public sealed class ReferenceGladeSceneTests
    {
        public const string Path = "Assets/Scenes/Main/ReferenceGlade.unity";

        [Test]
        public void DedicatedPlayableSceneIsSavedAlongsideTheNormalScene()
        {
            Assert.NotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(Path));
            Assert.NotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Main/SampleScene.unity"));
        }

        [TestCase(Path, Path)]
        [TestCase("Assets/Scenes/Main/SampleScene.unity", "Assets/Scenes/Main/SampleScene.unity")]
        public void PlayStartsTheSelectedSupportedScene(string activePath, string expected)
        {
            WithIsolatedScene(activePath, scene =>
            {
                SceneManager.SetActiveScene(scene);
                var type = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("CavesOfOoo.Editor.DefaultSceneLoader")).First(t => t != null);
                type.GetMethod("EnsurePlayModeStartScene", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
                Assert.AreEqual(expected, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            });
        }

        [Test]
        public void SceneUsesNativeBootstrapWithGladeStartAndAllRendererReferences()
        {
            Assert.NotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(Path));
            WithIsolatedScene(Path, scene =>
            {
                var boot = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameBootstrap>(true)).Single();
                Assert.AreEqual(ReferenceGladePlan.ZoneID, boot.FreshGameZoneID);
                Assert.NotNull(boot.ZoneRenderer);
                Assert.IsFalse(boot.ZoneRenderer.RevealEntire3DZone, "Ordinary glade play must obey native visibility after load and transition.");
                Assert.That(boot.GameplayZoomMultiplier, Is.InRange(.75f, 1f));
                Assert.IsTrue(boot.enabled, "Native bootstrap creates and wires InputHandler at Start; actual input is verified in Play.");
                Assert.AreEqual(1, scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Count(c => c.CompareTag("MainCamera")));
                Assert.IsFalse(scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true))
                    .Any(c => c != null && c.GetType().Name.Contains("NativePlayer")), "Audit drivers must not ship in the playable scene.");
            });
        }

        static void WithIsolatedScene(string path, Action<Scene> verify)
        {
            // Keep the runner's untitled scene and unsaved user work intact.
            // Only the inspected scene's own global lights participate while
            // this synchronous fixture is open; restore exact prior flags.
            var prior = SceneManager.GetActiveScene();
            var oldStart = EditorSceneManager.playModeStartScene;
            var scene = SceneManager.GetSceneByPath(path);
            bool borrowed = scene.IsValid() && scene.isLoaded;
            var priorLights = Resources.FindObjectsOfTypeAll<Light2D>()
                .Where(light => light != null && light.gameObject.scene.IsValid()
                    && light.gameObject.scene.isLoaded && light.isActiveAndEnabled
                    && light.lightType == Light2D.LightType.Global
                    && (!borrowed || light.gameObject.scene != scene))
                .Distinct().ToArray();
            try
            {
                foreach (var light in priorLights) light.enabled = false;
                if (!borrowed) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                verify(scene);
            }
            finally
            {
                try
                {
                    // OpenScene may reuse an already loaded asset. Never close
                    // a scene that belonged to the editor before this fixture.
                    if (!borrowed && scene.IsValid() && scene.isLoaded)
                        EditorSceneManager.CloseScene(scene, true);
                    if (prior.IsValid() && prior.isLoaded) SceneManager.SetActiveScene(prior);
                }
                finally
                {
                    try
                    {
                        foreach (var light in priorLights)
                            if (light != null) light.enabled = true;
                    }
                    finally { EditorSceneManager.playModeStartScene = oldStart; }
                }
            }
        }

    }
}
#endif
