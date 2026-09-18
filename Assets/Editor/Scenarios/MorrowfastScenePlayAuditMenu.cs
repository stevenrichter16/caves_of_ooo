using System;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Scenarios.Custom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Owns a disposable save root before ordinary bootstrap, including
    /// domain reloads and shutdown saves. Successful runs stay open for review.</summary>
    [InitializeOnLoad]
    public static class MorrowfastScenePlayAuditMenu
    {
        private const string Prefix = "MorrowfastScene.NativePlayAudit.";
        static MorrowfastScenePlayAuditMenu() { if (SessionState.GetBool(Prefix + "active", false)) Subscribe(); }

        [MenuItem("Caves Of Ooo/Scenarios/World/Morrowfast Scene Native Play Audit")]
        public static void Launch() => LaunchMode(false);

        [MenuItem("Caves Of Ooo/Scenarios/World/Morrowfast Scene Play Preview")]
        public static void LaunchPreview() => LaunchMode(true);

        private static void LaunchMode(bool previewOnly)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play before launching the ordinary-bootstrap audit.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save the current scene before launching; this audit will not discard scene edits.");
            string token = NativeSaveIsolation.Begin(Prefix, "Morrowfast-native-marker-" + Guid.NewGuid().ToString("N"), false);
            SessionState.SetString(Prefix + "token", token);
            SessionState.SetBool(Prefix + "active", true);
            SessionState.SetBool(Prefix + "reported", false);
            SessionState.SetBool(Prefix + "previewOnly", previewOnly);
            SessionState.SetFloat(Prefix + "deadline", (float)EditorApplication.timeSinceStartup + 1260);
            try
            {
                Subscribe();
                EditorSceneManager.OpenScene("Assets/Scenes/Main/SampleScene.unity");
                EditorApplication.isPlaying = true;
            }
            catch (Exception error) { Debug.LogError("[MorrowfastScenePlayAudit] Launch failed: " + error); Finish(4); }
        }
        [MenuItem("Caves Of Ooo/Scenarios/World/Morrowfast Scene Native Play Audit", true)]
        [MenuItem("Caves Of Ooo/Scenarios/World/Morrowfast Scene Play Preview", true)]
        private static bool CanLaunch() => !EditorApplication.isPlayingOrWillChangePlaymode;

        private static void Subscribe()
        {
            NativeSaveIsolation.Restore(Prefix, SessionState.GetString(Prefix + "token", ""), Finish);
            GameBootstrap.OnAfterBootstrap -= Apply; GameBootstrap.OnAfterBootstrap += Apply;
            EditorApplication.update -= Poll; EditorApplication.update += Poll;
        }
        private static void Apply(Zone zone, EntityFactory factory, Entity player, TurnManager turns)
        {
            GameBootstrap.OnAfterBootstrap -= Apply;
            new GameObject("Morrowfast Scene Native Play Audit").AddComponent<MorrowfastScenePlayAudit>().Initialize(SessionState.GetBool(Prefix + "previewOnly", false));
        }
        private static void Poll()
        {
            if (!SessionState.GetBool(Prefix + "active", false) || SessionState.GetBool(Prefix + "reported", false)) return;
            var driver = UnityEngine.Object.FindFirstObjectByType<MorrowfastScenePlayAudit>();
            if (driver != null && driver.Finished)
            {
                SessionState.SetBool(Prefix + "reported", true);
                Debug.Log("[MorrowfastScenePlayAudit] Finished with " + driver.Failures + " failures. Play remains open for review; stopping Play restores the prior save root.");
                return;
            }
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Prefix + "deadline", 0))
            {
                if (driver != null) driver.Abort("Editor watchdog exceeded twenty-one minutes.");
                else { Debug.LogError("[MorrowfastScenePlayAudit] Ordinary bootstrap did not create audit driver."); Finish(2); }
            }
        }
        private static void Finish(int code)
        {
            SessionState.SetBool(Prefix + "active", false);
            GameBootstrap.OnAfterBootstrap -= Apply; EditorApplication.update -= Poll;
            SaveGameService.RegisterRuntime(null, null);
            NativeSaveIsolation.Finish(Prefix, SessionState.GetString(Prefix + "token", ""), code);
        }
    }
}
