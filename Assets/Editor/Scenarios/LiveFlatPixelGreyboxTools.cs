using System;
using System.IO;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CavesOfOoo.Editor
{
    /// <summary>Manual, isolated ordinary Play session for the art pipeline.
    /// Does not spawn, move, reveal or otherwise change simulation entities.</summary>
    [InitializeOnLoad]
    public static class LiveFlatPixelGreyboxTools
    {
        private const string Prefix = "LiveFlatPixelGreybox.";
        private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        static LiveFlatPixelGreyboxTools()
        {
            if (!SessionState.GetBool(Prefix + "pending", false)) return;
            NativeAuditBootstrapSettings.RequestedSeed = 64;
            EditorApplication.playModeStateChanged += OnPlayState;
            // A reload can lose the delayed restore after isolation has already
            // finished. Defer recovery until Unity's Play transition flags settle.
            EditorApplication.delayCall += RecoverAfterReload;
        }

        [MenuItem("Caves Of Ooo/Art/Play Live 16x24 Greybox")]
        public static void Launch()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(Prefix + "pending", false))
                throw new InvalidOperationException("Finish the current Play session first.");
            if (SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().isDirty || string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
                throw new InvalidOperationException("This art launcher requires one saved, clean scene.");
            SessionState.SetString(Prefix + "scene", SceneManager.GetActiveScene().path);
            SessionState.SetInt(Prefix + "seed", NativeAuditBootstrapSettings.RequestedSeed);
            SessionState.SetString(Prefix + "startScene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            string token = NativeSaveIsolation.Begin(Prefix, "FlatPixelGreyboxMarker", false);
            SessionState.SetString(Prefix + "token", token);
            SessionState.SetBool(Prefix + "pending", true);
            try
            {
                ConfigureView();
                NativeAuditBootstrapSettings.RequestedSeed = 64;
                EditorSceneManager.OpenScene("Assets/Scenes/Main/SampleScene.unity");
                var renderer = UnityEngine.Object.FindFirstObjectByType<ZoneRenderer>();
                if (renderer == null) throw new InvalidOperationException("The ordinary scene has no world renderer.");
                renderer.FlatPixelModeEnabled = true;
                EditorSceneManager.playModeStartScene = null; // play this unsaved presentation configuration
                EditorApplication.playModeStateChanged -= OnPlayState;
                EditorApplication.playModeStateChanged += OnPlayState;
                EditorApplication.isPlaying = true;
            }
            catch { NativeSaveIsolation.Finish(Prefix, token, 4); Restore(); throw; }
        }

        [MenuItem("Caves Of Ooo/Art/Finish Live 16x24 Greybox")]
        public static void Finish()
        {
            if (!SessionState.GetBool(Prefix + "pending", false)) return;
            NativeSaveIsolation.Finish(Prefix, SessionState.GetString(Prefix + "token", ""), 0);
            QueueRestore();
        }

        private static void RecoverAfterReload() => RecoverPendingSession(EditorApplication.isPlayingOrWillChangePlaymode);

        private static void RecoverPendingSession(bool playingOrWillChangePlaymode)
        {
            if (playingOrWillChangePlaymode) return;
            Finish();
        }

        private static void QueueRestore()
        {
            EditorApplication.delayCall -= Restore;
            EditorApplication.delayCall += Restore;
        }

        [MenuItem("Caves Of Ooo/Art/Capture Live 16x24 Greybox")]
        public static void Capture()
        {
            if (!EditorApplication.isPlaying || !SessionState.GetBool(Prefix + "pending", false))
                throw new InvalidOperationException("Launch the isolated live greybox first.");
            string directory = Path.GetFullPath("Docs/Verification/Live16x24");
            Directory.CreateDirectory(directory);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "greybox-game-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".png"));
        }

        private static void OnPlayState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode) QueueRestore();
        }
        private static void Restore()
        {
            if (!SessionState.GetBool(Prefix + "pending", false)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool("CavesOfOoo.NativeSaveIsolation.active", false))
            { QueueRestore(); return; }
            NativeAuditBootstrapSettings.RequestedSeed = SessionState.GetInt(Prefix + "seed", 0);
            try { RestoreView(); }
            catch (Exception error) { Debug.LogException(error); }
            finally
            {
                EditorSceneManager.OpenScene(SessionState.GetString(Prefix + "scene", "Assets/Scenes/Main/SampleScene.unity"));
                string startScene = SessionState.GetString(Prefix + "startScene", "");
                EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(startScene) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(startScene);
                SessionState.SetBool(Prefix + "pending", false);
                EditorApplication.playModeStateChanged -= OnPlayState;
            }
            Debug.Log("[LiveFlatPixelGreybox] Original scene, view, seed and isolated saves restored.");
        }
        private static Type EditorType(string name) => typeof(UnityEditor.Editor).Assembly.GetType(name, true);
        private static object Group()
        {
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(EditorType("UnityEditor.GameViewSizes"));
            var sizes = singleton.GetProperty("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetValue(null);
            return sizes.GetType().GetMethod("GetGroup", All).Invoke(sizes,
                new[] { Enum.Parse(EditorType("UnityEditor.GameViewSizeGroupType"), "Standalone") });
        }
        private static void ConfigureView()
        {
            var view = EditorWindow.GetWindow(EditorType("UnityEditor.GameView"));
            var selected = view.GetType().GetProperty("selectedSizeIndex", All);
            SessionState.SetInt(Prefix + "view", (int)selected.GetValue(view));
            SessionState.SetInt(Prefix + "added", -1);
            SessionState.SetBool(Prefix + "viewSet", true);
            var group = Group(); var type = group.GetType();
            int built = (int)type.GetMethod("GetBuiltinCount", All).Invoke(group, null);
            int custom = (int)type.GetMethod("GetCustomCount", All).Invoke(group, null);
            int chosen = -1;
            for (int i = 0; i < built + custom; i++)
            {
                var size = type.GetMethod("GetGameViewSize", All).Invoke(group, new object[] { i });
                if ((int)size.GetType().GetProperty("width", All).GetValue(size) == 1280
                    && (int)size.GetType().GetProperty("height", All).GetValue(size) == 800) { chosen = i; break; }
            }
            if (chosen < 0)
            {
                var size = Activator.CreateInstance(EditorType("UnityEditor.GameViewSize"), All, null,
                    new object[] { Enum.Parse(EditorType("UnityEditor.GameViewSizeType"), "FixedResolution"), 1280, 800, "Live flat pixel audit " + SessionState.GetString(Prefix + "token", "") }, null);
                type.GetMethod("AddCustomSize", All).Invoke(group, new[] { size });
                SessionState.SetInt(Prefix + "added", custom); chosen = built + custom;
            }
            selected.SetValue(view, chosen); view.Show(); view.Focus(); view.Repaint();
        }
        private static void RestoreView()
        {
            if (!SessionState.GetBool(Prefix + "viewSet", false)) return;
            var view = EditorWindow.GetWindow(EditorType("UnityEditor.GameView"));
            int added = SessionState.GetInt(Prefix + "added", -1);
            if (added >= 0)
            {
                var group = Group(); var type = group.GetType();
                int count = (int)type.GetMethod("GetCustomCount", All).Invoke(group, null);
                int built = (int)type.GetMethod("GetBuiltinCount", All).Invoke(group, null);
                if (added < count)
                {
                    var size = type.GetMethod("GetGameViewSize", All).Invoke(group, new object[] { built + added });
                    string label = (string)size.GetType().GetProperty("baseText", All).GetValue(size);
                    if (label == "Live flat pixel audit " + SessionState.GetString(Prefix + "token", "")
                        && (int)size.GetType().GetProperty("width", All).GetValue(size) == 1280
                        && (int)size.GetType().GetProperty("height", All).GetValue(size) == 800)
                        type.GetMethod("RemoveCustomSize", All).Invoke(group, new object[] { added });
                }
            }
            view.GetType().GetProperty("selectedSizeIndex", All).SetValue(view, SessionState.GetInt(Prefix + "view", 0));
            SessionState.SetBool(Prefix + "viewSet", false);
        }
    }
}
