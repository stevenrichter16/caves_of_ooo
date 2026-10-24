using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Borrow the existing native save owner; never launch Play or switch scenes.</summary>
    public sealed class WorldPresentationAuditOverrideTests
    {
        const string Owner="PresentationOverrideTest.",Prefix="CavesOfOoo.NativeSaveIsolation.";
        const BindingFlags Static=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        Type isolation;NewGameSaveFixture saves;string token,root;
        bool hadMode,hadSprites,enabled,low,sprites;int prefMode,prefSprites;
        WorldPresentationMode mode;object resolution,shadows;Action<string> dirty;
        [SetUp] public void Setup()
        {
            Assert.False(EditorApplication.isPlayingOrWillChangePlaymode);
            Assert.False(SessionState.GetBool(Prefix+"active",false),"Do not borrow an active native audit.");
            isolation=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor.NativeSaveIsolation")).First(t=>t!=null);
            RuntimeHelpers.RunClassConstructor(isolation.TypeHandle);
            hadMode=PlayerPrefs.HasKey(WorldPresentationSettings.PreferenceKey);prefMode=PlayerPrefs.GetInt(WorldPresentationSettings.PreferenceKey);
            hadSprites=PlayerPrefs.HasKey(WorldPresentationSettings.LegacySpritesPreferenceKey);prefSprites=PlayerPrefs.GetInt(WorldPresentationSettings.LegacySpritesPreferenceKey);
            mode=WorldPresentationSettings.Mode;sprites=WorldPresentationSettings.LegacySpritesEnabled;enabled=Village3DSettings.Enabled;low=Village3DSettings.LowDetail;
            resolution=Quality("resolutionOverride").GetValue(null);shadows=Quality("shadowsOverride").GetValue(null);
            dirty=ZoneRenderHooks.FullDirtyCallback;ZoneRenderHooks.FullDirtyCallback=null;
            saves=new NewGameSaveFixture();token=(string)Call("Begin",Owner,"presentation-marker",false);root=SaveGameService.SaveRootOverride;
            PlayerPrefs.SetInt(WorldPresentationSettings.PreferenceKey,0);PlayerPrefs.SetInt(WorldPresentationSettings.LegacySpritesPreferenceKey,0);
        }
        [TearDown] public void Cleanup()
        {
            if(token!=null)Call("Finish",Owner,token,0);saves?.Dispose();
            WorldPresentationSettings.Mode=mode;WorldPresentationSettings.LegacySpritesEnabled=sprites;Village3DSettings.Enabled=enabled;Village3DSettings.LowDetail=low;
            Quality("resolutionOverride").SetValue(null,resolution);Quality("shadowsOverride").SetValue(null,shadows);ZoneRenderHooks.FullDirtyCallback=dirty;
            if(hadMode)PlayerPrefs.SetInt(WorldPresentationSettings.PreferenceKey,prefMode);else PlayerPrefs.DeleteKey(WorldPresentationSettings.PreferenceKey);
            if(hadSprites)PlayerPrefs.SetInt(WorldPresentationSettings.LegacySpritesPreferenceKey,prefSprites);else PlayerPrefs.DeleteKey(WorldPresentationSettings.LegacySpritesPreferenceKey);
            PlayerPrefs.Save();
        }
        [TestCase(0)] [TestCase(1)]
        public void ExactActiveOwnedAuditOverridesLaunchOnlyWithoutEditingPlayerPreference(int selected)
        {
            PlayerPrefs.SetInt(WorldPresentationSettings.PreferenceKey,1-selected);
            Call("SetPresentation",Owner,token,(WorldPresentationMode)selected);Initialize();
            Assert.AreEqual((WorldPresentationMode)selected,WorldPresentationSettings.Mode);Assert.AreEqual(1-selected,PlayerPrefs.GetInt(WorldPresentationSettings.PreferenceKey));
            Assert.False(WorldPresentationSettings.LegacySpritesEnabled);Assert.AreEqual(root,SaveGameService.SaveRootOverride);
            // Re-establishing this same isolation after reload retains its explicit view.
            Call("Restore",Owner,token,null);Initialize();Assert.AreEqual((WorldPresentationMode)selected,WorldPresentationSettings.Mode);
        }
        [Test] public void RootMismatchIgnoresEvenAnActiveAuditView()
        {
            Call("SetPresentation",Owner,token,WorldPresentationMode.Previous);SaveGameService.SaveRootOverride=saves.Root;
            try{Initialize();Assert.AreEqual(WorldPresentationMode.Flat16x24,WorldPresentationSettings.Mode);Assert.AreEqual(0,PlayerPrefs.GetInt(WorldPresentationSettings.PreferenceKey));}
            finally{SaveGameService.SaveRootOverride=root;}
        }
        [TestCase(true)] [TestCase(false)]
        public void WrongOwnerOrTokenCannotInstallAView(bool wrongOwner)
        {
            var method=isolation.GetMethod("SetPresentation",Static);Assert.NotNull(method,"An explicit owned audit request is required.");
            var error=Assert.Throws<TargetInvocationException>(()=>method.Invoke(null,new object[]{wrongOwner?"Other.":Owner,wrongOwner?token:Guid.NewGuid().ToString("N"),WorldPresentationMode.Previous}));
            Assert.IsInstanceOf<InvalidOperationException>(error.InnerException);Initialize();Assert.AreEqual(WorldPresentationMode.Flat16x24,WorldPresentationSettings.Mode);
        }
        [Test] public void FinishingAndBeginningANewOwnerCannotInheritThePreviousAuditView()
        {
            Call("SetPresentation",Owner,token,WorldPresentationMode.Previous);Initialize();Assert.AreEqual(WorldPresentationMode.Previous,WorldPresentationSettings.Mode);
            Call("Finish",Owner,token,0);token=null;Initialize();Assert.AreEqual(WorldPresentationMode.Flat16x24,WorldPresentationSettings.Mode);
            token=(string)Call("Begin",Owner,"second-presentation-marker",false);root=SaveGameService.SaveRootOverride;
            Initialize();Assert.AreEqual(WorldPresentationMode.Flat16x24,WorldPresentationSettings.Mode);Assert.AreEqual(0,PlayerPrefs.GetInt(WorldPresentationSettings.PreferenceKey));
        }
        object Call(string name,params object[] args){var m=isolation.GetMethod(name,Static);Assert.NotNull(m,"Missing owned-isolation API: "+name);return m.Invoke(null,args);}
        static FieldInfo Quality(string name)=>typeof(Village3DSettings).GetField(name,Static);
        static void Initialize()
        {
            var go=new GameObject("Presentation launch initialization");go.SetActive(false);
            try{var boot=go.AddComponent<GameBootstrap>();var method=typeof(GameBootstrap).GetMethod("InitializeWorldPresentation",BindingFlags.Instance|BindingFlags.NonPublic);Assert.NotNull(method,"Bootstrap must explicitly load presentation once.");method.Invoke(boot,null);}
            finally{Object.DestroyImmediate(go);}
        }
    }
}
