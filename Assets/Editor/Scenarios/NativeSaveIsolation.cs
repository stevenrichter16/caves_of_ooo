using System;
using System.IO;
using CavesOfOoo.Core;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Owns every save made by one native audit, including fresh-game
    /// GUID directories. SessionState survives domain reload; cleanup waits until
    /// Play shutdown completes. Never deletes an inherited override or user saves.</summary>
    [InitializeOnLoad]
    public static class NativeSaveIsolation
    {
        private const string Prefix = "CavesOfOoo.NativeSaveIsolation.";
        private static Action<int> _manualStop;
        static NativeSaveIsolation()
        {
            if (!Active) return;
            SaveGameService.SaveRootOverride = OwnedRoot(Token);
            Subscribe();
            EditorApplication.delayCall += ResumePendingFinish;
        }
        private static bool Active => SessionState.GetBool(Prefix + "active", false);
        private static string Token => SessionState.GetString(Prefix + "token", "");
        private static string OwnedRoot(string token)
        {
            if (!Guid.TryParseExact(token,"N",out _)) throw new InvalidOperationException("Invalid native save ownership token.");
            return Path.Combine(Path.GetTempPath(),"coo-native-save-audits",token);
        }
        /// <summary>Starts one isolated audit, creating only its disposable boot
        /// marker. Rejects concurrent owners before changing root or preferences.</summary>
        public static string Begin(string owner,string markerID,bool exitEditor)
        {
            if (Active) throw new InvalidOperationException("Another native audit already owns save isolation.");
            if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("An audit owner is required.",nameof(owner));
            if (string.IsNullOrWhiteSpace(markerID) || markerID=="." || markerID==".."
                || markerID.IndexOfAny(new[]{'/','\\'})>=0) throw new ArgumentException("Marker must be one directory name.",nameof(markerID));
            string token=Guid.NewGuid().ToString("N");
            SessionState.SetString(Prefix+"owner",owner);SessionState.SetString(Prefix+"token",token);
            SessionState.SetBool(Prefix+"rootWasNull",SaveGameService.SaveRootOverride==null);
            SessionState.SetString(Prefix+"oldRoot",SaveGameService.SaveRootOverride??"");
            SessionState.SetBool(Prefix+"hadPref",PlayerPrefs.HasKey(SaveGameService.LastGameIDPrefsKey));
            SessionState.SetString(Prefix+"oldPref",PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey));
            SessionState.SetBool(Prefix+"exitEditor",exitEditor);SessionState.SetBool(Prefix+"finishing",false);
            SessionState.SetInt(Prefix+"code",0);SessionState.SetBool(Prefix+"active",true);
            try
            {
                SaveGameService.SaveRootOverride=OwnedRoot(token);
                string marker=Path.Combine(SaveGameService.SaveRootOverride,markerID);Directory.CreateDirectory(marker);
                File.WriteAllBytes(Path.Combine(marker,"Quick.sav.gz"),Array.Empty<byte>());
                PlayerPrefs.SetString(SaveGameService.LastGameIDPrefsKey,markerID);PlayerPrefs.Save();
                Subscribe();return token;
            }
            catch { SessionState.SetInt(Prefix+"code",4);Complete(false);throw; }
        }
        private static void Validate(string owner,string token)
        {
            if (!Active || !string.Equals(owner,SessionState.GetString(Prefix+"owner",""),StringComparison.Ordinal)
                || !string.Equals(token,Token,StringComparison.Ordinal))
                throw new InvalidOperationException("Native save isolation owner/token does not match.");
        }
        /// <summary>Reapplies the same root after static reset/reload without
        /// replacing its original preference snapshot. Rebinds manual-stop cleanup.</summary>
        public static void Restore(string owner,string token,Action<int> manualStop)
        {
            Validate(owner,token);SaveGameService.SaveRootOverride=OwnedRoot(token);_manualStop=manualStop;Subscribe();
        }
        /// <summary>Requests shutdown once. Root/prefs stay isolated through Play
        /// teardown; already completed requests are harmless. Wrong live owners fail.</summary>
        public static bool Finish(string owner,string token,int code)
        {
            if (!Active) return false;
            Validate(owner,token);
            if (!SessionState.GetBool(Prefix+"finishing",false))
            {SessionState.SetBool(Prefix+"finishing",true);SessionState.SetInt(Prefix+"code",code);}
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying=false;
            else Complete(true);
            return true;
        }
        private static void Subscribe()
        {
            EditorApplication.playModeStateChanged-=OnPlayState;EditorApplication.playModeStateChanged+=OnPlayState;
            EditorApplication.quitting-=OnQuitting;EditorApplication.quitting+=OnQuitting;
        }
        private static void ResumePendingFinish()
        {
            if (!Active || !SessionState.GetBool(Prefix+"finishing",false)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying=false;
            else Complete(true);
        }
        private static void OnPlayState(PlayModeStateChange state)
        {
            if (state!=PlayModeStateChange.EnteredEditMode || !Active) return;
            if (!SessionState.GetBool(Prefix+"finishing",false))
            {
                try { _manualStop?.Invoke(3); }
                catch (Exception ex)
                {
                    SaveGameService.RegisterRuntime(null, null);
                    SessionState.SetBool(Prefix+"finishing",true);SessionState.SetInt(Prefix+"code",4);
                    Debug.LogError("[NativeSaveIsolation] Manual stop failed: "+ex);
                }
                if (!Active) return;
                if (!SessionState.GetBool(Prefix+"finishing",false)) SessionState.SetInt(Prefix+"code",3);
            }
            Complete(true);
        }
        private static void OnQuitting()
        {
            if (!Active) return;
            if (!SessionState.GetBool(Prefix+"finishing",false)) SessionState.SetInt(Prefix+"code",3);
            // An external process quit can precede remaining Play teardown.
            // Disable saving and retain the disposable destination until exit.
            SaveGameService.RegisterRuntime(null, null);
            Complete(false, restoreRoot: false);
        }
        private static void Complete(bool allowExit, bool restoreRoot = true)
        {
            if (!Active) return;
            int code=SessionState.GetInt(Prefix+"code",0);bool exit=SessionState.GetBool(Prefix+"exitEditor",false);
            string owner=SessionState.GetString(Prefix+"owner","");
            try
            {
                if(SessionState.GetBool(Prefix+"hadPref",false))PlayerPrefs.SetString(SaveGameService.LastGameIDPrefsKey,SessionState.GetString(Prefix+"oldPref",""));
                else PlayerPrefs.DeleteKey(SaveGameService.LastGameIDPrefsKey);
                PlayerPrefs.Save();
            }
            catch(Exception ex){code=4;Debug.LogError("[NativeSaveIsolation] Preference restoration failed: "+ex);}
            finally
            {
                if (restoreRoot) SaveGameService.SaveRootOverride=SessionState.GetBool(Prefix+"rootWasNull",false)?null:SessionState.GetString(Prefix+"oldRoot","");
                try{string root=OwnedRoot(Token);if(Directory.Exists(root))Directory.Delete(root,true);}
                catch(Exception ex){code=4;Debug.LogError("[NativeSaveIsolation] Owned-root cleanup failed: "+ex);}
                SessionState.SetBool(Prefix+"active",false);SessionState.SetBool(owner+"active",false);SessionState.SetInt(Prefix+"lastCode",code);
                _manualStop=null;EditorApplication.playModeStateChanged-=OnPlayState;EditorApplication.quitting-=OnQuitting;EditorApplication.delayCall-=ResumePendingFinish;
            }
            Debug.Log("[NativeSaveIsolation] Cleanup complete, exit="+code);
            if(allowExit && exit)EditorApplication.Exit(code);
        }
    }
}
