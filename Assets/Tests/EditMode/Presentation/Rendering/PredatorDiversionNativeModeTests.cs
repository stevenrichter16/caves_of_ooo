#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Scenarios.Custom;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CavesOfOoo.Tests
{
    public sealed class PredatorDiversionNativeModeTests
    {
        static Type Launcher => AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("CavesOfOoo.Editor.SpreadDiscoveryNativeBatch")).Single(t => t != null);
        [Test] public void ActualDiversionHasItsOwnIsolatedNoArgumentLauncher()
        {
            var entry = Launcher.GetMethod("LaunchPredatorDiversion", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            Assert.NotNull(entry, "Missing ordinary provision-to-generated-hunt native mode"); Assert.AreEqual(typeof(void), entry.ReturnType);
        }
        [Test] public void DiversionRefusesWithoutSaveIsolationBeforeChangingInputOrWorld()
        {
            string oldRoot = SaveGameService.SaveRootOverride; var settings = InputSystem.settings; var keyboard = Keyboard.current;
            bool background = Application.runInBackground; var go = new GameObject("Owned predator diversion guard");
            try
            {
                SaveGameService.SaveRootOverride = null; var driver = go.AddComponent<SpreadDiscoveryNativePlayer>();
                var entry = typeof(SpreadDiscoveryNativePlayer).GetMethod("InitializePredatorDiversion", BindingFlags.Public | BindingFlags.Instance,
                    null, new[] { typeof(ScenarioContext) }, null);
                Assert.NotNull(entry, "Missing explicit isolated native diversion initializer");
                var error = Assert.Throws<TargetInvocationException>(() => entry.Invoke(driver, new object[] { null }));
                Assert.IsInstanceOf<InvalidOperationException>(error.InnerException); StringAssert.Contains("Isolated launcher", error.InnerException.Message);
                Assert.AreSame(settings, InputSystem.settings); Assert.AreSame(keyboard, Keyboard.current); Assert.AreEqual(background, Application.runInBackground);
                Assert.False(driver.Finished); Assert.IsNull(driver.ReportPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go); SaveGameService.SaveRootOverride = oldRoot;
                Assert.AreSame(settings, InputSystem.settings); Assert.AreSame(keyboard, Keyboard.current); Assert.AreEqual(background, Application.runInBackground);
            }
        }
    }
}
#endif
