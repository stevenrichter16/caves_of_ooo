#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CavesOfOoo.Tests
{
    public sealed class ReferenceGladeSceneTests
    {
        public const string Path="Assets/Scenes/Main/ReferenceGlade.unity";
        [Test]public void DedicatedPlayableSceneIsSavedAlongsideTheNormalScene()
        {Assert.NotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(Path));Assert.NotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Main/SampleScene.unity"));}
        [TestCase(Path,Path)]
        [TestCase("Assets/Scenes/Main/SampleScene.unity","Assets/Scenes/Main/SampleScene.unity")]
        public void PlayStartsTheSelectedSupportedScene(string activePath,string expected)
        {
            var old=EditorSceneManager.playModeStartScene;var prior=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.OpenScene(activePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor.DefaultSceneLoader")).First(t=>t!=null);
                type.GetMethod("EnsurePlayModeStartScene",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
                Assert.AreEqual(expected,AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            }
            finally{EditorSceneManager.CloseScene(scene,true);if(prior.IsValid())SceneManager.SetActiveScene(prior);EditorSceneManager.playModeStartScene=old;}
        }
        [Test]public void SceneUsesNativeBootstrapWithGladeStartAndAllRendererReferences()
        {
            Assert.NotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(Path));
            var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);
            try
            {
                var boot=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GameBootstrap>(true)).Single();
                Assert.AreEqual(ReferenceGladePlan.ZoneID,boot.FreshGameZoneID);Assert.NotNull(boot.ZoneRenderer);
                Assert.IsFalse(boot.ZoneRenderer.RevealEntire3DZone,"Ordinary glade play must obey native visibility after load and transition.");
                Assert.That(boot.GameplayZoomMultiplier,Is.InRange(.75f,1f));
                Assert.IsTrue(boot.enabled,"Native bootstrap creates and wires InputHandler at Start; actual input is verified in Play.");
                Assert.AreEqual(1,scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).Count(c=>c.CompareTag("MainCamera")));
                Assert.IsFalse(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)).Any(c=>c!=null&&c.GetType().Name.Contains("NativePlayer")),"Audit drivers must not ship in the playable scene.");
            }
            finally{EditorSceneManager.CloseScene(scene,true);if(prior.IsValid())SceneManager.SetActiveScene(prior);}
        }
    }
}
#endif
