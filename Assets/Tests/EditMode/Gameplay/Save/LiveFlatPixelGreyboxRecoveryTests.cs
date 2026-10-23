using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEditor;

namespace CavesOfOoo.Tests
{
    public sealed class LiveFlatPixelGreyboxRecoveryTests
    {
        private const string Prefix = "LiveFlatPixelGreybox.";
        private const string IsolationPrefix = "CavesOfOoo.NativeSaveIsolation.";
        private const BindingFlags Static = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        private Type _launcher, _isolation;
        private NewGameSaveFixture _saves;
        private string _token, _oldToken, _ownedRoot;

        [SetUp]
        public void SetUp()
        {
            Assert.IsFalse(EditorApplication.isPlayingOrWillChangePlaymode);
            Assert.IsFalse(SessionState.GetBool(Prefix + "pending", false), "Do not interrupt a real art session.");
            Assert.IsFalse(SessionState.GetBool(IsolationPrefix + "active", false), "Do not take another audit's root.");
            _launcher = FindEditorType("LiveFlatPixelGreyboxTools");
            _isolation = FindEditorType("NativeSaveIsolation");
            RuntimeHelpers.RunClassConstructor(_launcher.TypeHandle);
            _oldToken = SessionState.GetString(Prefix + "token", "");
            _saves = new NewGameSaveFixture();
            _token = (string)Call(_isolation, "Begin", Prefix, "recovery-test", false);
            _ownedRoot = SaveGameService.SaveRootOverride;
            SessionState.SetString(Prefix + "token", _token);
            SessionState.SetBool(Prefix + "pending", true);
        }

        [TearDown]
        public void TearDown()
        {
            if (_token != null) Call(_isolation, "Finish", Prefix, _token, 0);
            // These tests verify scheduling, not scene switching. Remove only
            // our launcher callbacks before Unity's next editor tick.
            if (_launcher != null && EditorApplication.delayCall != null)
                foreach (var callback in EditorApplication.delayCall.GetInvocationList())
                    if (callback.Method.DeclaringType == _launcher)
                        EditorApplication.delayCall -= (EditorApplication.CallbackFunction)callback;
            if (_token != null)
            {
                SessionState.SetBool(Prefix + "pending", false);
                SessionState.SetString(Prefix + "token", _oldToken);
            }
            _saves?.Dispose();
        }

        [Test]
        public void FinishInEditMode_RestoresSaveOwnershipAndQueuesPresentationRestore()
        {
            Call(_launcher, "Finish");
            AssertCompletedAndQueued();
            Call(_launcher, "Finish");
            Assert.AreEqual(1, RestoreCount(), "Repeated Finish must not queue duplicate scene restoration.");
        }

        [Test]
        public void ReloadRecoveryInEditMode_CompletesPendingIsolationAndQueuesRestore()
        {
            Call(_launcher, "RecoverPendingSession", false);
            AssertCompletedAndQueued();
        }

        [Test]
        public void ReloadRecoveryDuringPlayOrTransition_PreservesAuditAndDoesNotQueueRestore()
        {
            Call(_launcher, "RecoverPendingSession", true);
            Assert.IsTrue(SessionState.GetBool(Prefix + "pending", false));
            Assert.IsTrue(SessionState.GetBool(IsolationPrefix + "active", false));
            Assert.AreEqual(_ownedRoot, SaveGameService.SaveRootOverride);
            Assert.IsTrue(Directory.Exists(_ownedRoot));
            Assert.AreEqual(0, RestoreCount(), "A domain reload entering or running Play must not stop it.");
            _saves.OldUnchanged();
        }

        [Test]
        public void ReloadAfterIsolationFinished_RequeuesLostPresentationRestore()
        {
            Call(_isolation, "Finish", Prefix, _token, 0);
            Assert.AreEqual(0, RestoreCount(), "Simulate reload losing the EnteredEditMode delay callback.");
            Call(_launcher, "RecoverPendingSession", false);
            AssertCompletedAndQueued();
        }

        private void AssertCompletedAndQueued()
        {
            Assert.IsFalse(SessionState.GetBool(IsolationPrefix + "active", false));
            Assert.IsFalse(Directory.Exists(_ownedRoot));
            Assert.AreEqual(_saves.Root, SaveGameService.SaveRootOverride);
            Assert.AreEqual(_saves.OldID, UnityEngine.PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey));
            Assert.AreEqual(1, RestoreCount(), "Scene/view/seed restoration still needs an editor callback after isolated shutdown.");
            _saves.OldUnchanged();
        }

        private int RestoreCount() => EditorApplication.delayCall?.GetInvocationList()
            .Count(callback => callback.Method.DeclaringType == _launcher && callback.Method.Name == "Restore") ?? 0;

        private static Type FindEditorType(string name)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("CavesOfOoo.Editor." + name)).FirstOrDefault(value => value != null);
            Assert.NotNull(type);
            return type;
        }

        private static object Call(Type type, string name, params object[] arguments)
        {
            var method = type.GetMethod(name, Static);
            Assert.NotNull(method, "Missing recovery entry point: " + name);
            return method.Invoke(null, arguments);
        }
    }
}
