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
    public static class GameAuditHotbarSaveBenchBatch
    {
        private const string Prefix="GA03c.NativeBench.";
        static GameAuditHotbarSaveBenchBatch(){if(SessionState.GetBool(Prefix+"active",false))Subscribe();}
        public static void Run()=>RunMode("new");
        [MenuItem("Caves Of Ooo/Scenarios/Save/Hotbar Save Selection Audit")]
        private static void Launch(){if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())RunMode("new",false);}
        [MenuItem("Caves Of Ooo/Scenarios/Save/Hotbar Save Selection Audit",true)]
        private static bool CanLaunch()=>!EditorApplication.isPlayingOrWillChangePlaymode;
        private static void RunMode(string mode,bool exitEditor=true)
        {
            string marker="GA03c-marker-"+Guid.NewGuid().ToString("N");
            string token=NativeSaveIsolation.Begin(Prefix,marker,exitEditor);
            SessionState.SetString(Prefix+"saveToken",token);SessionState.SetString(Prefix+"mode",mode);
            SessionState.SetBool(Prefix+"active",true);SessionState.SetInt(Prefix+"errors",0);SessionState.SetFloat(Prefix+"deadline",(float)EditorApplication.timeSinceStartup+180);
            try
            {
                Subscribe();EditorSceneManager.OpenScene("Assets/Scenes/Main/SampleScene.unity");EditorApplication.isPlaying=true;
            }
            catch(Exception ex){Debug.LogError("[GameAuditHotbarSaveBench] Launch failed: "+ex);Finish(4);}
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
            var driver=new GameObject("Hotbar save selection audit").AddComponent<GameAuditHotbarSaveBenchPlayer>();
            driver.Initialize(player);
        }
        private static void Poll()
        {
            if(!SessionState.GetBool(Prefix+"active",false))return;
            var driver=UnityEngine.Object.FindFirstObjectByType<GameAuditHotbarSaveBenchPlayer>();
            if(driver!=null&&driver.Finished){Finish(driver.Failures+SessionState.GetInt(Prefix+"errors",0)==0?0:1);return;}
            if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Prefix+"deadline",0)){Debug.LogError("[GameAuditHotbarSaveBench] Timed out.");Finish(2);}
        }
        private static void OnLog(string message,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)SessionState.SetInt(Prefix+"errors",SessionState.GetInt(Prefix+"errors",0)+1);}
        private static void Finish(int code)
        {
            SessionState.SetBool(Prefix+"active",false);GameBootstrap.OnAfterBootstrap-=Apply;EditorApplication.update-=Poll;Application.logMessageReceived-=OnLog;
            SaveGameService.RegisterRuntime(null,null);Debug.Log("[GameAuditHotbarSaveBench] Native capture exit="+code);
            NativeSaveIsolation.Finish(Prefix,SessionState.GetString(Prefix+"saveToken",""),code);
        }
    }
}
