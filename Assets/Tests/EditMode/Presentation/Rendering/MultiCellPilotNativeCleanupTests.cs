using System;
using System.Reflection;
using CavesOfOoo.Rendering;
using CavesOfOoo.Scenarios.Custom;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using Object=UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class MultiCellPilotNativeCleanupTests
    {
        const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        static void Set(object target,string field,object value)=>target.GetType().GetField(field,Private).SetValue(target,value);

        [Test] public void OutputFailureStillReleasesKeyboardAndRestoresInputDisplayAndBackground()
        {
            // Invalid output path fails before touching any audit/user file. The
            // regression is cleanup order under real File.WriteAllText failure.
            var oldSettings=InputSystem.settings;var temporarySettings=Object.Instantiate(oldSettings);
            var oldKeyboard=Keyboard.current;Keyboard keyboard=null;var oldBackground=Application.runInBackground;
            bool oldEnabled=Village3DSettings.Enabled,oldLow=Village3DSettings.LowDetail;
            string[] keys={Village3DSettings.PreferenceKey,Village3DSettings.LowDetailPreferenceKey};
            bool[] had={PlayerPrefs.HasKey(keys[0]),PlayerPrefs.HasKey(keys[1])};
            int[] values={PlayerPrefs.GetInt(keys[0]),PlayerPrefs.GetInt(keys[1])};
            var go=new GameObject("Native pilot cleanup failure fixture");var audit=go.AddComponent<MultiCellPilotNativeAudit>();
            try
            {
                Set(audit,"originalSettings",oldSettings);Set(audit,"originalBackground",oldBackground);
                Set(audit,"originalEnabled",oldEnabled);Set(audit,"originalLow",oldLow);Set(audit,"oldKeyboard",oldKeyboard);
                Array.Copy(had,(bool[])typeof(MultiCellPilotNativeAudit).GetField("prefPresent",Private).GetValue(audit),2);
                Array.Copy(values,(int[])typeof(MultiCellPilotNativeAudit).GetField("prefValue",Private).GetValue(audit),2);
                Set(audit,"<RunId>k__BackingField","invalid\0output");
                keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();Set(audit,"keyboard",keyboard);
                InputSystem.settings=temporarySettings;Application.runInBackground=!oldBackground;
                Village3DSettings.Enabled=!oldEnabled;Village3DSettings.LowDetail=!oldLow;Set(audit,"initialized",true);
                Assert.AreSame(temporarySettings,InputSystem.settings);Assert.IsTrue(keyboard.added);
                Assert.AreEqual(!oldBackground,Application.runInBackground,"live settings really differ before failure");
                Assert.Throws<TargetInvocationException>(()=>typeof(MultiCellPilotNativeAudit).GetMethod("OnDestroy",Private).Invoke(audit,null));
                Assert.AreSame(oldSettings,InputSystem.settings,"fallible report writing cannot bypass settings restoration");
                Assert.IsFalse(keyboard.added,"dedicated audit keyboard must always be removed");
                Assert.AreEqual(oldBackground,Application.runInBackground);
                Assert.AreEqual(oldEnabled,Village3DSettings.Enabled);Assert.AreEqual(oldLow,Village3DSettings.LowDetail);
                for(int i=0;i<keys.Length;i++)
                {Assert.AreEqual(had[i],PlayerPrefs.HasKey(keys[i]));if(had[i])Assert.AreEqual(values[i],PlayerPrefs.GetInt(keys[i]));}
            }
            finally
            {
                Set(audit,"initialized",false);if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);
                if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();InputSystem.settings=oldSettings;
                Application.runInBackground=oldBackground;Village3DSettings.Enabled=oldEnabled;Village3DSettings.LowDetail=oldLow;
                for(int i=0;i<keys.Length;i++)if(had[i])PlayerPrefs.SetInt(keys[i],values[i]);else PlayerPrefs.DeleteKey(keys[i]);
                PlayerPrefs.Save();Object.DestroyImmediate(go);Object.DestroyImmediate(temporarySettings);
            }
        }
    }
}
