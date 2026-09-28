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
 public sealed class QuestFreeCollectorNativeModeTests
 {
  [TestCase("Launch")][TestCase("LaunchCollector")][TestCase("LaunchAffordances")]
  public void ExplicitModeHasAnOwnedNoArgumentLauncherWithoutReplacingDefault(string method)
  {
   var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor.QuestFreeSpreadStateNativeBatch")).Single(t=>t!=null);
   var entry=type.GetMethod(method,BindingFlags.Public|BindingFlags.Static,null,Type.EmptyTypes,null);Assert.NotNull(entry,"Missing explicit bounded collector mode");Assert.AreEqual(typeof(void),entry.ReturnType);
  }
  [TestCase("Initialize")][TestCase("InitializeCollector")][TestCase("InitializeAffordances")]
  public void BothModesRefuseBeforeInputOrWorldSetupWithoutActualSaveIsolation(string method)
  {
   string saveRoot=SaveGameService.SaveRootOverride;var settings=InputSystem.settings;var keyboard=Keyboard.current;bool background=Application.runInBackground;
   var go=new GameObject("OwnedCollectorModeGuard");
   try
   {
    SaveGameService.SaveRootOverride=null;var driver=go.AddComponent<QuestFreeSpreadStateNativePlayer>();
    var entry=typeof(QuestFreeSpreadStateNativePlayer).GetMethod(method,BindingFlags.Public|BindingFlags.Instance,null,new[]{typeof(ScenarioContext)},null);Assert.NotNull(entry,"Missing explicit collector initializer");
    var error=Assert.Throws<TargetInvocationException>(()=>entry.Invoke(driver,new object[]{null}));Assert.IsInstanceOf<InvalidOperationException>(error.InnerException);StringAssert.Contains("Isolated launcher",error.InnerException.Message);
    Assert.AreSame(settings,InputSystem.settings);Assert.AreSame(keyboard,Keyboard.current);Assert.AreEqual(background,Application.runInBackground);Assert.False(driver.Finished);Assert.IsNull(driver.ReportPath);
   }
   finally{UnityEngine.Object.DestroyImmediate(go);SaveGameService.SaveRootOverride=saveRoot;Assert.AreSame(settings,InputSystem.settings);Assert.AreSame(keyboard,Keyboard.current);Assert.AreEqual(background,Application.runInBackground);}
  }
 }
}
#endif
