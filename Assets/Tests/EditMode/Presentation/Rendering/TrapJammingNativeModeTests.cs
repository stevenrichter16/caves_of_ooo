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
    public sealed class TrapJammingNativeModeTests
    {
        [Test] public void OrdinaryTrapJourneyHasItsOwnNoArgumentIsolatedLauncher()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CavesOfOoo.Editor.SpreadDiscoveryNativeBatch")).Single(t => t != null);
            var method = type.GetMethod("LaunchTrapJamming", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            Assert.NotNull(method); Assert.AreEqual(typeof(void), method.ReturnType);
        }
        [Test] public void TrapJourneyRequiresIsolationBeforeChangingInputOrWorld()
        {
            string root = SaveGameService.SaveRootOverride; var settings = InputSystem.settings; var keyboard = Keyboard.current;
            bool background = Application.runInBackground; var go = new GameObject("Owned trap jamming guard");
            try
            {
                SaveGameService.SaveRootOverride = null; var driver = go.AddComponent<SpreadDiscoveryNativePlayer>();
                var method = typeof(SpreadDiscoveryNativePlayer).GetMethod("InitializeTrapJamming", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(ScenarioContext) }, null);
                Assert.NotNull(method);
                var error = Assert.Throws<TargetInvocationException>(() => method.Invoke(driver, new object[] { null }));
                Assert.IsInstanceOf<InvalidOperationException>(error.InnerException); StringAssert.Contains("Isolated launcher", error.InnerException.Message);
                Assert.AreSame(settings, InputSystem.settings); Assert.AreSame(keyboard, Keyboard.current); Assert.AreEqual(background, Application.runInBackground);
                Assert.False(driver.Finished); Assert.IsNull(driver.ReportPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go); SaveGameService.SaveRootOverride = root;
                Assert.AreSame(settings, InputSystem.settings); Assert.AreSame(keyboard, Keyboard.current); Assert.AreEqual(background, Application.runInBackground);
            }
        }
    }
}
#endif
