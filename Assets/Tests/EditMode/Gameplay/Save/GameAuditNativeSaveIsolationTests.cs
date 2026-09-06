using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine.TestTools;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class GameAuditNativeSaveIsolationTests
    {
        private static Type Helper()
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor.NativeSaveIsolation")).FirstOrDefault(t=>t!=null);
            Assert.NotNull(type,"Native launchers need isolation for marker AND fresh checkpoint directories.");return type;
        }
        private static object Call(Type type,string name,params object[] args) => type.GetMethod(name,BindingFlags.Public|BindingFlags.Static).Invoke(null,args);
        private static string Begin(Type type,string owner) => (string)Call(type,"Begin",owner,"marker",false);
        private static void Finish(Type type,string owner,string token) { if(token!=null)Call(type,"Finish",owner,token,0); }
        [TestCase(false,false)] [TestCase(false,true)] [TestCase(true,false)] [TestCase(true,true)]
        public void CompletionRestoresPreferencePresenceAndPreviousRoot(bool hadPref,bool hadRoot)
        {
            using(var f=new NewGameSaveFixture())
            {
                var type=Helper();string owner="isolation-test-"+Guid.NewGuid().ToString("N"),token=null;
                SaveGameService.SaveRootOverride=hadRoot?f.Root:null;if(hadPref)PlayerPrefs.SetString(SaveGameService.LastGameIDPrefsKey,"prior");else PlayerPrefs.DeleteKey(SaveGameService.LastGameIDPrefsKey);
                string disposable=null;
                try { token=Begin(type,owner);disposable=SaveGameService.SaveRootOverride;Assert.AreNotEqual(hadRoot?f.Root:null,disposable);Assert.IsTrue(File.Exists(Path.Combine(disposable,"marker","Quick.sav.gz")));Assert.AreEqual("marker",PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey)); }
                finally {Finish(type,owner,token);}
                Assert.IsFalse(Directory.Exists(disposable));Assert.AreEqual(hadRoot?f.Root:null,SaveGameService.SaveRootOverride);Assert.AreEqual(hadPref,PlayerPrefs.HasKey(SaveGameService.LastGameIDPrefsKey));if(hadPref)Assert.AreEqual("prior",PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey));f.OldUnchanged();
            }
        }
        [Test]
        public void ConcurrentOwnerIsRefusedBeforeChangingFirstOwnersState()
        {
            using(var f=new NewGameSaveFixture())
            {var type=Helper();string owner=Guid.NewGuid().ToString("N"),token=Begin(type,owner);try{string root=SaveGameService.SaveRootOverride;Assert.Throws<TargetInvocationException>(()=>Begin(type,"other-owner"));Assert.AreEqual(root,SaveGameService.SaveRootOverride);Assert.AreEqual("marker",PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey));Assert.IsTrue(Directory.Exists(root));}finally{Finish(type,owner,token);}f.OldUnchanged();}
        }
        [TestCase("Restore")] [TestCase("Finish")]
        public void WrongTokenCannotRestoreStopOrDeleteAnotherRun(string operation)
        {
            using(var f=new NewGameSaveFixture())
            {
                var type=Helper();string owner=Guid.NewGuid().ToString("N"),token=Begin(type,owner);
                try
                {string root=SaveGameService.SaveRootOverride;Assert.Throws<TargetInvocationException>(()=>{if(operation=="Restore")Call(type,operation,owner,"wrong",(Action<int>)(_=>Assert.Fail("No stop")));else Call(type,operation,owner,"wrong",0);});Assert.AreEqual(root,SaveGameService.SaveRootOverride);Assert.IsTrue(Directory.Exists(root));}
                finally{Finish(type,owner,token);}f.OldUnchanged();
            }
        }
        [Test]
        public void RestoreAfterStaticResetReusesRootAndOriginalPreferenceSnapshot()
        {
            using(var f=new NewGameSaveFixture())
            {
                var type=Helper();string owner=Guid.NewGuid().ToString("N"),token=Begin(type,owner),root=SaveGameService.SaveRootOverride;
                try {SaveGameService.SaveRootOverride=null;PlayerPrefs.SetString(SaveGameService.LastGameIDPrefsKey,"audit-changed");Call(type,"Restore",owner,token,(Action<int>)(_=>{}));Assert.AreEqual(root,SaveGameService.SaveRootOverride);Call(type,"Restore",owner,token,(Action<int>)(_=>{}));Assert.AreEqual(root,SaveGameService.SaveRootOverride);}
                finally{Finish(type,owner,token);}
                Assert.AreEqual(f.Root,SaveGameService.SaveRootOverride);Assert.AreEqual(f.OldID,PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey));f.OldUnchanged();
            }
        }
        [Test]
        public void FailedLaunchBeforePlayCanCleanUpImmediately()
        {
            using(var f=new NewGameSaveFixture())
            {
                var type=Helper();string owner=Guid.NewGuid().ToString("N"),token=Begin(type,owner),root=SaveGameService.SaveRootOverride;
                try{throw new InvalidOperationException("scene-open failure probe");}catch(InvalidOperationException){Call(type,"Finish",owner,token,4);}
                Assert.IsFalse(Directory.Exists(root));Assert.AreEqual(f.Root,SaveGameService.SaveRootOverride);Assert.AreEqual(f.OldID,PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey));f.OldUnchanged();
            }
        }
        [Test]
        public void CleanupRemovesEveryOwnedSessionButPreservesSiblingDirectory()
        {
            using(var f=new NewGameSaveFixture())
            {
                var type=Helper();string owner=Guid.NewGuid().ToString("N"),token=Begin(type,owner),root=SaveGameService.SaveRootOverride,sibling=root+"-sibling";
                try{Directory.CreateDirectory(Path.Combine(root,"fresh-guid"));File.WriteAllText(Path.Combine(root,"fresh-guid","Quick.json"),"owned");Directory.CreateDirectory(sibling);File.WriteAllText(Path.Combine(sibling,"keep"),"sibling");Finish(type,owner,token);Assert.IsFalse(Directory.Exists(root));Assert.AreEqual("sibling",File.ReadAllText(Path.Combine(sibling,"keep")));}
                finally{Finish(type,owner,token);if(Directory.Exists(sibling))Directory.Delete(sibling,true);}f.OldUnchanged();
            }
        }
        [Test]
        public void InvalidMarkerCannotEscapeOwnedRootOrChangePreferences()
        {
            using(var f=new NewGameSaveFixture())
            {var type=Helper();Assert.Throws<TargetInvocationException>(()=>Call(type,"Begin",Guid.NewGuid().ToString("N"),"../escape",false));Assert.AreEqual(f.Root,SaveGameService.SaveRootOverride);Assert.AreEqual(f.OldID,PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey));f.OldUnchanged();}
        }
        [Test]
        public void ThrowingManualStopStillRestoresIsolationAndReportsFailure()
        {
            using(var f=new NewGameSaveFixture())
            {
                var type=Helper();string owner=Guid.NewGuid().ToString("N"),token=Begin(type,owner),root=SaveGameService.SaveRootOverride;
                try
                {
                    Call(type,"Restore",owner,token,(Action<int>)(_=>throw new InvalidOperationException("stop probe")));
                    var method=type.GetMethod("OnPlayState",BindingFlags.NonPublic|BindingFlags.Static);
                    object state=Enum.Parse(method.GetParameters()[0].ParameterType,"EnteredEditMode");
                    LogAssert.Expect(LogType.Error,new Regex("NativeSaveIsolation.*Manual.*failed"));
                    Assert.DoesNotThrow(()=>method.Invoke(null,new[]{state}));
                    Assert.AreEqual(f.Root,SaveGameService.SaveRootOverride);Assert.IsFalse(Directory.Exists(root));Assert.AreEqual(f.OldID,PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey));
                }
                finally{Finish(type,owner,token);}f.OldUnchanged();
            }
        }
        [Test]
        public void ExternalQuitCannotExposeRealSaveRootToRemainingShutdownCallbacks()
        {
            using(var f=new NewGameSaveFixture())
            {
                var type=Helper();string owner=Guid.NewGuid().ToString("N"),token=Begin(type,owner),root=SaveGameService.SaveRootOverride;
                try
                {
                    type.GetMethod("OnQuitting",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
                    Assert.AreEqual(root,SaveGameService.SaveRootOverride,"Process shutdown must keep the disposable root selected.");
                    Assert.IsFalse(SaveGameService.QuickSave(),"Unregister runtime before releasing quit ownership.");
                    Assert.AreEqual(f.OldID,PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey));Assert.IsFalse(Directory.Exists(root));
                }
                finally{Finish(type,owner,token);}f.OldUnchanged();
            }
        }
        [Test]
        public void DuplicateFinishDoesNotRestoreAnOlderPreferenceOverNewerWork()
        {
            using(var f=new NewGameSaveFixture())
            {var type=Helper();string owner=Guid.NewGuid().ToString("N"),token=Begin(type,owner);Finish(type,owner,token);PlayerPrefs.SetString(SaveGameService.LastGameIDPrefsKey,"later-work");Finish(type,owner,token);Assert.AreEqual("later-work",PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey));Assert.AreEqual(f.Root,SaveGameService.SaveRootOverride);f.OldUnchanged();}
        }
    }
}
