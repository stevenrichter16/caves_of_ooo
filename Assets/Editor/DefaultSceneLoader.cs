using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CavesOfOoo.Editor
{
    /// <summary>
    /// Keeps the real game scene loaded in the editor. Unity sometimes
    /// drops back to an untitled/empty scene after a domain reload (or
    /// opens the stub <c>Assets/game.unity</c> by default), which shows
    /// as a blue viewport with no content. This loader runs once on
    /// editor load; if the active scene is empty/unsaved, it opens
    /// <see cref="SampleScenePath"/> in its place.
    ///
    /// It also wires <see cref="EditorSceneManager.playModeStartScene"/>
    /// so pressing Play always enters SampleScene first, regardless of
    /// which scene happens to be in the hierarchy at that moment.
    /// </summary>
    [InitializeOnLoad]
    internal static class DefaultSceneLoader
    {
        private const string SampleScenePath = "Assets/Scenes/Main/SampleScene.unity";

        static DefaultSceneLoader()
        {
            EditorApplication.delayCall += EnsureSampleSceneLoaded;
            EditorApplication.delayCall += EnsurePlayModeStartScene;
        }

        private static void EnsureSampleSceneLoaded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling) return;
            if (!File.Exists(SampleScenePath)) return;

            Scene active = EditorSceneManager.GetActiveScene();

            // Leave the active scene alone if it has actual content
            // attached to a real asset path — the user might be editing
            // it deliberately (e.g. a test scene, a scenario scene).
            bool activeIsUsable =
                !string.IsNullOrEmpty(active.path)
                && active.path != "Assets/game.unity"
                && active.rootCount > 0;

            if (activeIsUsable) return;

            // Blue-screen state: untitled scene, or the stub game.unity,
            // or an empty scene with no roots. Pull in the real one.
            EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        }

        private static void EnsurePlayModeStartScene()
        {
            if (!File.Exists(SampleScenePath)) return;

            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(SampleScenePath);
            if (asset == null) return;

            // Only (re)assign if it's missing or pointing elsewhere, to
            // avoid dirtying editor prefs on every domain reload.
            if (EditorSceneManager.playModeStartScene != asset)
                EditorSceneManager.playModeStartScene = asset;
        }
    }
}
