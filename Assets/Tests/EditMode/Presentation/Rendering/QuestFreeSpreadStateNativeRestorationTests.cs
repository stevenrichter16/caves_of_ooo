using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;

namespace CavesOfOoo.Tests
{
    public sealed class QuestFreeSpreadStateNativeRestorationTests
    {
        const BindingFlags Static = BindingFlags.NonPublic | BindingFlags.Static;
        static Type Launcher(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("CavesOfOoo.Editor." + name)).First(t => t != null);
        static EditorApplication.CallbackFunction Callback(Type type) =>
            (EditorApplication.CallbackFunction)Delegate.CreateDelegate(typeof(EditorApplication.CallbackFunction), type.GetMethod("RestoreScenes", Static));
        static int Count(EditorApplication.CallbackFunction callback) =>
            EditorApplication.update?.GetInvocationList().Count(d => d.Equals(callback)) ?? 0;

        [Test]
        public void RepeatedSchedulingKeepsOneFallbackUntilTheEditorSettles()
        {
            var type = Launcher("QuestFreeSpreadStateNativeBatch"); var callback = Callback(type);
            var prefix = (string)type.GetField("Prefix", Static).GetRawConstantValue();
            bool pending = SessionState.GetBool(prefix + "restoreScenes", false);
            try
            {
                SessionState.SetBool(prefix + "restoreScenes", true);
                type.GetMethod("AwaitSceneRestore", Static).Invoke(null, null);
                type.GetMethod("AwaitSceneRestore", Static).Invoke(null, null);
                Assert.AreEqual(1, Count(callback), "A delay callback can run while Play teardown is still busy; the update retry must survive it without duplicate registration.");
            }
            finally
            {
                EditorApplication.update -= callback; EditorApplication.delayCall -= callback;
                var stopped = (Action<PlayModeStateChange>)Delegate.CreateDelegate(typeof(Action<PlayModeStateChange>), type.GetMethod("OnStopped", Static));
                EditorApplication.playModeStateChanged -= stopped;
                SessionState.SetBool(prefix + "restoreScenes", pending);
            }
        }

        [Test]
        public void NoPendingRestoreRemovesTheRetryWithoutOpeningAnyScene()
        {
            var type = Launcher("QuestFreeSpreadStateNativeBatch"); var callback = Callback(type);
            var prefix = (string)type.GetField("Prefix", Static).GetRawConstantValue();
            bool pending = SessionState.GetBool(prefix + "restoreScenes", false);
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            try
            {
                SessionState.SetBool(prefix + "restoreScenes", false);
                EditorApplication.update += callback;
                type.GetMethod("RestoreScenes", Static).Invoke(null, null);
                Assert.AreEqual(0, Count(callback));
                Assert.AreEqual(scene, UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
            }
            finally { EditorApplication.update -= callback; SessionState.SetBool(prefix + "restoreScenes", pending); }
        }
    }
}
