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

        [TestCase(false)]
        [TestCase(true)]
        public void RestoreViewRemovesOnlyItsOwnedAuditSizeAndRestoresSelectionOnce(bool fullOwnershipToken)
        {
            using (var view = new ViewSizeScope(_launcher))
            {
                Assert.AreEqual(32, _token.Length, "Keep the actual native isolation token unchanged.");
                object target = view.Arm(fullOwnershipToken ? _token : "view-test", false, false);
                object[] before = view.Sizes();
                Call(_launcher, "RestoreView");
                view.AssertSizes(before.Where(size => !ReferenceEquals(size, target)).ToArray());
                Assert.AreEqual(0, view.SelectedIndex);
                Assert.IsFalse(SessionState.GetBool(Prefix + "viewSet", true));
                Call(_launcher, "RestoreView");
                view.AssertSizes(before.Where(size => !ReferenceEquals(size, target)).ToArray());
            }
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void RestoreViewPreservesAPresetWhoseTokenOrDimensionsDoNotMatch(bool foreignToken, bool wrongWidth)
        {
            using (var view = new ViewSizeScope(_launcher))
            {
                view.Arm(_token, foreignToken, wrongWidth);
                object[] before = view.Sizes();
                Call(_launcher, "RestoreView");
                view.AssertSizes(before);
                Assert.AreEqual(0, view.SelectedIndex);
                Assert.IsFalse(SessionState.GetBool(Prefix + "viewSet", true));
            }
        }

        // Exercise Unity's real combined-index API without risking any user preset
        // even while RestoreView is still wrong. All possible wrong removals below
        // land in test-owned padding; cleanup uses current indices and references.
        private sealed class ViewSizeScope : IDisposable
        {
            private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            private readonly object _group;
            private readonly Type _groupType, _viewType;
            private readonly PropertyInfo _selected;
            private readonly int _built, _oldView, _oldAdded;
            private readonly bool _oldViewSet;
            private readonly string _oldLauncherToken;
            private readonly EditorWindow[] _borrowedViews;
            private readonly int[] _borrowedSelections;
            private readonly EditorWindow _oldFocus;
            private readonly System.Collections.Generic.List<object> _owned = new System.Collections.Generic.List<object>();
            private EditorWindow _view;

            public ViewSizeScope(Type launcher)
            {
                _group = Call(launcher, "Group");
                _groupType = _group.GetType();
                _built = Count("GetBuiltinCount");
                Assert.Greater(_built, 0, "This regression needs Unity's real nonempty built-in preset prefix.");
                _viewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView", true);
                _selected = _viewType.GetProperty("selectedSizeIndex", All);
                Assert.NotNull(_selected);
                _borrowedViews = UnityEngine.Resources.FindObjectsOfTypeAll(_viewType).Cast<EditorWindow>().ToArray();
                _borrowedSelections = _borrowedViews.Select(window => (int)_selected.GetValue(window)).ToArray();
                _oldFocus = EditorWindow.focusedWindow;
                _oldViewSet = SessionState.GetBool(Prefix + "viewSet", false);
                _oldView = SessionState.GetInt(Prefix + "view", 0);
                _oldAdded = SessionState.GetInt(Prefix + "added", -1);
                _oldLauncherToken = SessionState.GetString(Prefix + "token", "");
            }

            public object Arm(string token, bool foreignToken, bool wrongWidth)
            {
                _view = EditorWindow.GetWindow(_viewType);
                // If there were C user customs and B built-ins, the buggy call
                // removes custom (C+B)-B=C: the first of our B padding entries.
                for (int i = 0; i < _built; i++) Add("owned regression padding " + token + " " + i, 901 + i);
                int targetCustomIndex = Count("GetCustomCount");
                // This is only the launcher's view receipt. NativeSaveIsolation's
                // separate ownership token and the outer fixture token are untouched.
                SessionState.SetString(Prefix + "token", token);
                string labelToken = foreignToken ? (token[0] == '0' ? "1" : "0") + token.Substring(1) : token;
                string requestedLabel = "CoO " + labelToken;
                int requestedWidth = wrongWidth ? 1279 : 1280;
                object target = Add(requestedLabel, requestedWidth);
                Assert.LessOrEqual(requestedLabel.Length, 40, "Preserve the full token within Unity's preset label limit.");
                Assert.AreEqual(requestedLabel, target.GetType().GetProperty("baseText", All).GetValue(target),
                    "Unity must store the complete ownership label without truncation.");
                Assert.AreEqual(requestedWidth, target.GetType().GetProperty("width", All).GetValue(target));
                Assert.AreEqual(800, target.GetType().GetProperty("height", All).GetValue(target));
                Assert.AreSame(target, At(targetCustomIndex), "The custom-relative receipt must identify this exact preset.");
                Assert.AreEqual(token, SessionState.GetString(Prefix + "token", ""), "Keep the launcher view receipt exact.");
                Add("owned regression sentinel " + token, 973);
                SessionState.SetInt(Prefix + "view", 0);
                SessionState.SetInt(Prefix + "added", targetCustomIndex);
                SessionState.SetBool(Prefix + "viewSet", true);
                _selected.SetValue(_view, _built + targetCustomIndex);
                return target;
            }

            public int SelectedIndex => (int)_selected.GetValue(_view);
            private int Count(string method) => (int)_groupType.GetMethod(method, All).Invoke(_group, null);
            private object At(int customIndex) => _groupType.GetMethod("GetGameViewSize", All).Invoke(_group, new object[] { _built + customIndex });

            public object[] Sizes()
            {
                var sizes = new object[Count("GetCustomCount")];
                for (int i = 0; i < sizes.Length; i++) sizes[i] = At(i);
                return sizes;
            }

            public void AssertSizes(object[] expected)
            {
                object[] actual = Sizes();
                Assert.AreEqual(expected.Length, actual.Length, "Only the owned audit preset may be removed.");
                for (int i = 0; i < expected.Length; i++)
                    Assert.AreSame(expected[i], actual[i], "Preset identity/order changed at custom index " + i);
            }

            private object Add(string label, int width)
            {
                Type sizeType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSize", true);
                Type kindType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizeType", true);
                object size = Activator.CreateInstance(sizeType, All, null,
                    new object[] { Enum.Parse(kindType, "FixedResolution"), width, 800, label }, null);
                _owned.Add(size);
                _groupType.GetMethod("AddCustomSize", All).Invoke(_group, new[] { size });
                return size;
            }

            public void Dispose()
            {
                try
                {
                    // Never use the production method under test to clean up.
                    for (int i = Count("GetCustomCount") - 1; i >= 0; i--)
                        if (_owned.Any(size => ReferenceEquals(size, At(i))))
                            _groupType.GetMethod("RemoveCustomSize", All).Invoke(_group, new object[] { _built + i });
                }
                finally
                {
                    SessionState.SetBool(Prefix + "viewSet", _oldViewSet);
                    SessionState.SetInt(Prefix + "view", _oldView);
                    SessionState.SetInt(Prefix + "added", _oldAdded);
                    SessionState.SetString(Prefix + "token", _oldLauncherToken);
                    for (int i = 0; i < _borrowedViews.Length; i++)
                        if (_borrowedViews[i]) _selected.SetValue(_borrowedViews[i], _borrowedSelections[i]);
                    if (_view && !_borrowedViews.Contains(_view)) _view.Close();
                    if (_oldFocus) _oldFocus.Focus();
                }
            }
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
