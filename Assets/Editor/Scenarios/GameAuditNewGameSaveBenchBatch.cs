using System;
using System.IO;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Scenarios.Custom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    [InitializeOnLoad]
    public static class GameAuditNewGameSaveBenchBatch
    {
        private const string Prefix="GA03b.NativeBench.";
        static GameAuditNewGameSaveBenchBatch(){if(SessionState.GetBool(Prefix+"active",false))Subscribe();}
        public static void Run()=>RunMode("new");
        public static void RunContinue()=>RunMode("continue");
        public static void RunEmpty()=>RunMode("empty");
        public static void RunFailure()=>RunMode("failure");
        [MenuItem("Caves Of Ooo/Scenarios/Save/New Game Checkpoint Audit")]
        private static void Launch(){if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())RunMode("new",false);}
        [MenuItem("Caves Of Ooo/Scenarios/Save/New Game Checkpoint Audit",true)]
        private static bool CanLaunch()=>!EditorApplication.isPlayingOrWillChangePlaymode;
        private static void RunMode(string mode,bool exitEditor=true)
        {
            string marker="GA03b-marker-"+Guid.NewGuid().ToString("N");
            string token=NativeSaveIsolation.Begin(Prefix,marker,exitEditor);
            SessionState.SetString(Prefix+"saveToken",token);SessionState.SetString(Prefix+"mode",mode);
            SessionState.SetString(Prefix+"oldID","GA03b-old-native-"+Guid.NewGuid().ToString("N"));
            SessionState.SetBool(Prefix+"active",true);SessionState.SetInt(Prefix+"errors",0);SessionState.SetFloat(Prefix+"deadline",(float)EditorApplication.timeSinceStartup+180);
            try
            {
                if(mode=="empty"){Directory.Delete(Path.Combine(SaveGameService.SaveRootOverride,marker),true);PlayerPrefs.DeleteKey(SaveGameService.LastGameIDPrefsKey);PlayerPrefs.Save();}
                Subscribe();EditorSceneManager.OpenScene("Assets/Scenes/Main/SampleScene.unity");EditorApplication.isPlaying=true;
            }
            catch(Exception ex){Debug.LogError("[GameAuditNewGameSaveBench] Launch failed: "+ex);Finish(4);}
        }
        private static void Subscribe()
        {
            NativeSaveIsolation.Restore(Prefix,SessionState.GetString(Prefix+"saveToken",""),Finish);
            GameBootstrap.OnAfterBootstrap-=Apply;GameBootstrap.OnAfterBootstrap+=Apply;EditorApplication.update-=Poll;EditorApplication.update+=Poll;
            Application.logMessageReceived-=OnLog;Application.logMessageReceived+=OnLog;
        }
        private static void Apply(Zone zone,EntityFactory factory,Entity player,TurnManager turns)
        {
            GameBootstrap.OnAfterBootstrap-=Apply;
            var bootstrap=UnityEngine.Object.FindFirstObjectByType<GameBootstrap>();const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            // Read-only capture of the already built runtime; fixture seeding happens
            // before bootstrap registers its real capture/apply callbacks.
            var fresh=GameSessionState.Capture((string)typeof(GameBootstrap).GetField("_gameID",flags).GetValue(bootstrap),Application.version,
                (OverworldZoneManager)typeof(GameBootstrap).GetField("_zoneManager",flags).GetValue(bootstrap),turns,player,
                world:(Entity)typeof(GameBootstrap).GetField("_world",flags).GetValue(bootstrap));
            var driver=new GameObject("New-game save audit").AddComponent<GameAuditNewGameSaveBenchPlayer>();
            driver.Initialize(SessionState.GetString(Prefix+"mode","new"),fresh,SessionState.GetString(Prefix+"oldID",""));
        }
        private static void Poll()
        {
            if(!SessionState.GetBool(Prefix+"active",false))return;
            var driver=UnityEngine.Object.FindFirstObjectByType<GameAuditNewGameSaveBenchPlayer>();
            if(driver!=null&&driver.Finished){Finish(driver.Failures+SessionState.GetInt(Prefix+"errors",0)==0?0:1);return;}
            if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Prefix+"deadline",0)){Debug.LogError("[GameAuditNewGameSaveBench] Timed out.");Finish(2);}
        }
        private static void OnLog(string message,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)SessionState.SetInt(Prefix+"errors",SessionState.GetInt(Prefix+"errors",0)+1);}
        private static void Finish(int code)
        {
            SessionState.SetBool(Prefix+"active",false);GameBootstrap.OnAfterBootstrap-=Apply;EditorApplication.update-=Poll;Application.logMessageReceived-=OnLog;
            SaveGameService.RegisterRuntime(null,null);Debug.Log("[GameAuditNewGameSaveBench] Native capture exit="+code);
            NativeSaveIsolation.Finish(Prefix,SessionState.GetString(Prefix+"saveToken",""),code);
        }
    }
}
