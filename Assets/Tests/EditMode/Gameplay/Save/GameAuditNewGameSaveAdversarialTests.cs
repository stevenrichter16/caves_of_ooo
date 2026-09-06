using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CavesOfOoo.Tests
{
    public class GameAuditNewGameSaveAdversarialTests
    {
        private static void AddGraph(GameSessionState state)
        {
            var zone = new Zone("Overworld.10.10.0"); Assert.IsTrue(zone.AddEntity(state.Player,3,4));
            var manager = new OverworldZoneManager(null,222);
            manager.ReplaceLoadedState(new Dictionary<string,Zone>{{zone.ZoneID,zone}},zone.ZoneID,new Dictionary<string,List<ZoneConnection>>());
            var turns = new TurnManager(); turns.RestoreSavedState(17,true,state.Player,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=state.Player,Energy=200}});
            state.ZoneManager=manager; state.TurnManager=turns; state.ActiveZoneID=zone.ZoneID;
            state.Player.SetIntProperty("SaveIsolationMarker",202);
        }
        private static void Graph(GameSessionState state)
        {
            Assert.AreSame(state.Player,state.ZoneManager.ActiveZone.GetCell(3,4).Objects.Single());
            Assert.AreSame(state.Player,state.TurnManager.CurrentActor); Assert.AreSame(state.TurnManager,TurnManager.Active);
            Assert.AreEqual(17,state.TurnManager.TickCount); Assert.AreEqual(202,state.Player.GetIntProperty("SaveIsolationMarker"));
        }
        [TestCase("F6")] [TestCase("pause")] [TestCase("pause-key")] [TestCase("death")]
        public void NewGame_LoadControlsRestoreTheActualWorldGraph(string route)
        {
            using(var f=new NewGameSaveFixture())
            {
                AddGraph(f.Fresh); f.Choose(KeyCode.N);
                if(route=="pause-key") { var pause=new PauseMenuController();pause.Open();pause.Tick(new NewGameSaveFixture.Keys(KeyCode.DownArrow),f.Service,f.Messages.Add);pause.Tick(new NewGameSaveFixture.Keys(KeyCode.Return),f.Service,f.Messages.Add);Assert.IsFalse(pause.IsOpen); }
                else f.Load(route);
                f.ExpectNewLoaded(); Graph(f.Loaded);
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void FailedContinueStaysModalAndNewGameCanRecover(bool collision)
        {
            using(var f=new NewGameSaveFixture())
            {
                string oldPath=Path.Combine(f.Root,f.OldID,"Quick.sav.gz"); byte[] bytes=File.ReadAllBytes(oldPath);
                try
                {
                    File.WriteAllText(oldPath,"not gzip"); LogAssert.Expect(LogType.Error,new Regex("\\[Save\\] Load.*failed"));
                    var menu=f.Choose(collision?new[]{KeyCode.C,KeyCode.N}:new[]{KeyCode.C}); Assert.IsTrue(menu.IsActive);Assert.IsNull(f.Loaded); Assert.AreEqual(f.OldID,f.ActiveID);
                    Assert.IsFalse(Directory.Exists(Path.Combine(f.Root,f.NewID)));
                    menu.Tick(new NewGameSaveFixture.Keys(KeyCode.N),f.Service,f.Messages.Add);Assert.IsFalse(menu.IsActive);Assert.AreEqual(f.NewID,f.ActiveID);
                }
                finally { File.WriteAllBytes(oldPath,bytes); }
                f.Load("F6"); f.ExpectNewLoaded();
            }
        }
        [Test]
        public void CaptureSeesFreshBindingAndRunsOnlyOnceBeforeCheckpoint()
        {
            using(var f=new NewGameSaveFixture())
            { int count=0; f.Register(()=>{count++;Assert.AreEqual(f.NewID,f.ActiveID);return f.Fresh;},f.NewID);Assert.IsTrue(f.Begin());Assert.AreEqual(1,count);f.OldUnchanged(); }
        }
        [Test]
        public void RecursiveInitialCheckpointIsRefusedWithoutRecapturing()
        {
            using(var f=new NewGameSaveFixture())
            {
                int count=0;bool nested=true;
                f.Register(()=>{ count++; if(count==1) nested=f.Begin();return f.Fresh;},f.NewID);
                Assert.IsTrue(f.Begin());Assert.IsFalse(nested);Assert.AreEqual(1,count);Assert.IsFalse(File.Exists(Path.Combine(f.Root,f.NewID,"Quick.sav.gz.bak")));f.OldUnchanged();
            }
        }
        [TestCase(null)] [TestCase("")]
        public void NewRegistrationWithoutIdentityCannotReuseEarlierCandidate(string missing)
        {
            using(var f=new NewGameSaveFixture())
            { f.Register(null,missing);Assert.IsFalse(f.Begin());string fallback=f.ActiveID;Assert.AreNotEqual(f.OldID,fallback);Assert.AreNotEqual(f.NewID,fallback);Assert.IsTrue(Guid.TryParseExact(fallback,"N",out var parsed));Assert.IsFalse(f.Begin());Assert.AreEqual(fallback,f.ActiveID);f.OldUnchanged(); }
        }
        [TestCase(false)] [TestCase(true)]
        public void MissingIndependentIdentityDoesNotAdoptAnUnrelatedCapturedID(bool capture)
        {
            using(var f=new NewGameSaveFixture())
            { f.Register(capture?(Func<GameSessionState>)(()=>f.Fresh):null,null);Assert.IsFalse(f.Begin());Assert.AreNotEqual(f.NewID,f.ActiveID);Assert.AreNotEqual(f.OldID,f.ActiveID);Assert.IsFalse(SaveGameService.HasQuickSave());f.OldUnchanged(); }
        }
        [TestCase("success")] [TestCase("null")] [TestCase("throw")]
        public void CaptureRebindingCannotChangeChosenDestination(string result)
        {
            using(var f=new NewGameSaveFixture())
            {
                f.Register(()=>{SaveGameService.SetActiveGameID(f.OldID);if(result=="throw")throw new InvalidOperationException("probe");return result=="null"?null:f.Fresh;},f.NewID);
                if(result=="throw")LogAssert.Expect(LogType.Error,new Regex("\\[Save\\].*failed"));
                Assert.AreEqual(result=="success",f.Begin());Assert.AreEqual(f.NewID,f.ActiveID);Assert.AreEqual(result=="success",SaveGameService.HasQuickSave());f.OldUnchanged();
            }
        }
        [TestCase("Quick.sav.gz.tmp",false)] [TestCase("Quick.json.tmp",true)] [TestCase("Quick.sav.gz",false)]
        public void WriteFailureKeepsNewBindingAndCanRetry(string blocker,bool dataRemains)
        {
            using(var f=new NewGameSaveFixture())
            {
                string path=Path.Combine(f.Root,f.NewID,blocker);Directory.CreateDirectory(path);
                LogAssert.Expect(LogType.Error,new Regex("\\[Save\\] Save.*failed"));Assert.IsFalse(f.Begin());Assert.AreEqual(f.NewID,f.ActiveID);
                Assert.AreEqual(dataRemains,SaveGameService.HasQuickSave()); Assert.AreEqual(f.OldID,PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey));f.OldUnchanged();
                Directory.Delete(path,true);Assert.IsTrue(SaveGameService.QuickSave());f.Load("F6");f.ExpectNewLoaded();
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void PreferenceChangesOnlyAfterCompleteInitialCheckpoint(bool success)
        {
            using(var f=new NewGameSaveFixture())
            { if(!success)f.Register(()=>null,f.NewID);Assert.AreEqual(success,f.Begin());Assert.AreEqual(success?f.NewID:f.OldID,PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey));Assert.AreEqual(f.NewID,f.ActiveID);f.OldUnchanged(); }
        }
        [Test]
        public void SaveAfterAppliedLoadCapturesLoadedCharacterAndBacksUpOnlyItsSlot()
        {
            using(var f=new NewGameSaveFixture())
            {
                AddGraph(f.Fresh);GameSessionState current=f.Fresh;
                SaveGameService.RegisterRuntime(()=>current,s=>{current=s;f.Loaded=s;},f.NewID);
                f.Choose(KeyCode.N);f.Load("F6");Graph(current);current.Player.SetIntProperty("SaveIsolationMarker",303);
                Assert.IsTrue(SaveGameService.QuickSave());Assert.IsTrue(File.Exists(Path.Combine(f.Root,f.NewID,"Quick.sav.gz.bak")));
                Assert.IsTrue(SaveGameService.QuickLoad());Assert.AreEqual(303,f.Loaded.Player.GetIntProperty("SaveIsolationMarker"));f.OldUnchanged();
            }
        }
        [Test]
        public void PrimaryAndMetadataUseTheSameIsolatedNewCharacter()
        {
            using(var f=new NewGameSaveFixture())
            { Assert.IsTrue(f.Begin());Assert.IsTrue(SaveGameService.SavePrimary());Assert.IsTrue(SaveGameService.HasPrimarySave());Assert.AreEqual(f.NewID,SaveGameService.GetSaveInfo("Primary").GameID);Assert.IsTrue(SaveGameService.LoadPrimary());f.ExpectNewLoaded();Assert.IsTrue(File.Exists(Path.Combine(f.Root,f.NewID,"Primary.sav.gz"))); }
        }
        [TestCase("null")] [TestCase("throw")] [TestCase("mismatch")] [TestCase("write")]
        public void InitialOperationGuardReleasesAfterEveryFailure(string failure)
        {
            using(var f=new NewGameSaveFixture())
            {
                bool refuse=true;string blocker=Path.Combine(f.Root,f.NewID,"Quick.sav.gz.tmp");
                f.Register(()=>{if(refuse && failure=="throw")throw new InvalidOperationException("retry probe");return refuse && failure=="null"?null:refuse && failure=="mismatch"?f.Old:f.Fresh;},f.NewID);
                if(failure=="write")Directory.CreateDirectory(blocker);
                if(failure=="write"||failure=="throw")LogAssert.Expect(LogType.Error,new Regex("\\[Save\\].*failed"));
                Assert.IsFalse(f.Begin());refuse=false;if(Directory.Exists(blocker))Directory.Delete(blocker,true);
                Assert.IsTrue(f.Begin(),"Retry without re-registering must release the operation guard.");Assert.AreEqual(f.NewID,f.ActiveID);f.Load("F6");f.ExpectNewLoaded();
            }
        }
        [TestCase("valid-pref")] [TestCase("no-pref")] [TestCase("stale-pref")] [TestCase("missing-meta")] [TestCase("malformed-meta")]
        public void IsolatedDiscoveryRetainsItsExistingPreferenceAndMetadataRules(string kind)
        {
            using(var f=new NewGameSaveFixture())
            {
                Assert.IsTrue(f.Begin());string meta=Path.Combine(f.Root,f.NewID,"Quick.json");var info=JsonUtility.FromJson<SaveGameInfo>(File.ReadAllText(meta));info.SaveTimestampUtc=DateTime.UtcNow.AddDays(1).ToString("O");File.WriteAllText(meta,JsonUtility.ToJson(info));
                if(kind=="valid-pref")PlayerPrefs.SetString(SaveGameService.LastGameIDPrefsKey,f.OldID);
                else if(kind=="stale-pref")PlayerPrefs.SetString(SaveGameService.LastGameIDPrefsKey,"absent");else PlayerPrefs.DeleteKey(SaveGameService.LastGameIDPrefsKey);
                if(kind=="missing-meta")File.Delete(meta);if(kind=="malformed-meta")File.WriteAllText(meta,"not json");
                SaveGameService.SetActiveGameID("stale");SaveGameService.ResolveActiveGameIDOnBoot();Assert.AreEqual(kind=="valid-pref"||kind=="missing-meta"||kind=="malformed-meta"?f.OldID:f.NewID,f.ActiveID);f.OldUnchanged();
            }
        }
        [Test]
        public void TrulyEmptyRootDiscoveryThenNewSessionCreatesFreshCheckpoint()
        {
            using(var f=new NewGameSaveFixture())
            {
                string empty=Path.Combine(f.Root,"empty");Directory.CreateDirectory(empty);SaveGameService.SaveRootOverride=empty;
                SaveGameService.SetActiveGameID(f.OldID);SaveGameService.ResolveActiveGameIDOnBoot();Assert.IsFalse(SaveGameService.HasQuickSave());Assert.IsTrue(f.Begin());Assert.AreEqual(f.NewID,f.ActiveID);
                Assert.IsTrue(File.Exists(Path.Combine(empty,f.NewID,"Quick.sav.gz")));f.Load("F6");f.ExpectNewLoaded();
            }
        }
        [TestCase("corrupt")] [TestCase("capture")] [TestCase("apply")]
        public void FailedNewCharacterLoadDoesNotFallBackToPreviousSave(string failure)
        {
            using(var f=new NewGameSaveFixture())
            {
                Assert.IsTrue(f.Begin());int applies=0;
                if(failure=="corrupt")File.WriteAllText(Path.Combine(f.Root,f.NewID,"Quick.sav.gz"),"bad gzip");
                SaveGameService.RegisterRuntime(()=>{if(failure=="capture")throw new InvalidOperationException("factory");return f.Fresh;},s=>{applies++;if(failure=="apply")throw new InvalidOperationException("apply");f.Loaded=s;},f.NewID);
                LogAssert.Expect(LogType.Error,new Regex("\\[Save\\] Load.*failed"));Assert.IsFalse(SaveGameService.QuickLoad());Assert.AreEqual(f.NewID,f.ActiveID);Assert.IsNull(f.Loaded);Assert.AreEqual(failure=="apply"?1:0,applies);f.OldUnchanged();
            }
        }
        [TestCase(false,false)] [TestCase(false,true)] [TestCase(true,false)] [TestCase(true,true)]
        public void FixtureRestoresExactPreferenceRootAndRuntime(bool hadPref,bool customRoot)
        {
            string originalRoot=SaveGameService.SaveRootOverride;bool originalHad=PlayerPrefs.HasKey(SaveGameService.LastGameIDPrefsKey);string originalPref=PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey);
            var fields=typeof(SaveGameService).GetFields(NewGameSaveFixture.Static).Where(x=>!x.IsLiteral&&!x.IsInitOnly).ToDictionary(x=>x,x=>x.GetValue(null));
            string temporary=Path.Combine(Path.GetTempPath(),"coo-outer-"+Guid.NewGuid().ToString("N"));
            try
            {
                SaveGameService.SaveRootOverride=customRoot?temporary:null;if(hadPref)PlayerPrefs.SetString(SaveGameService.LastGameIDPrefsKey,"outer");else PlayerPrefs.DeleteKey(SaveGameService.LastGameIDPrefsKey);
                SaveGameService.SetActiveGameID("outer-active");Func<GameSessionState> capture=()=>null;Action<GameSessionState> apply=s=>{};SaveGameService.RegisterRuntime(capture,apply,"outer-fresh");var turns=TurnManager.Active;
                using(var f=new NewGameSaveFixture()){AddGraph(f.Fresh);Assert.IsTrue(f.Begin());f.Load("F6");}
                Assert.AreEqual(customRoot?temporary:null,SaveGameService.SaveRootOverride);Assert.AreEqual(hadPref,PlayerPrefs.HasKey(SaveGameService.LastGameIDPrefsKey));if(hadPref)Assert.AreEqual("outer",PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey));
                Assert.AreEqual("outer-active",typeof(SaveGameService).GetField("_activeGameID",NewGameSaveFixture.Static).GetValue(null));Assert.AreSame(capture,typeof(SaveGameService).GetField("_captureCurrent",NewGameSaveFixture.Static).GetValue(null));Assert.AreSame(turns,TurnManager.Active);Assert.AreSame(apply,typeof(SaveGameService).GetField("_applyLoaded",NewGameSaveFixture.Static).GetValue(null));Assert.AreEqual("outer-fresh",typeof(SaveGameService).GetField("_newGameID",NewGameSaveFixture.Static).GetValue(null));Assert.AreEqual(false,typeof(SaveGameService).GetField("_startingNewGame",NewGameSaveFixture.Static).GetValue(null));
            }
            finally
            { foreach(var field in fields)field.Key.SetValue(null,field.Value);SaveGameService.SaveRootOverride=originalRoot;if(originalHad)PlayerPrefs.SetString(SaveGameService.LastGameIDPrefsKey,originalPref);else PlayerPrefs.DeleteKey(SaveGameService.LastGameIDPrefsKey);PlayerPrefs.Save();if(Directory.Exists(temporary))Directory.Delete(temporary,true); }
        }
    }
}
