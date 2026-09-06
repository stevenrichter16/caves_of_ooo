using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Real bootstrap and queued keyboard audit. Reflection observes
    /// bindings; deliberate marker/capture faults are fixture setup, not UI actions.</summary>
    public sealed class GameAuditNewGameSaveBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        private const string Marker="NewGameSaveAuditMarker";
        private static readonly BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private static readonly BindingFlags Instance=BindingFlags.Instance|BindingFlags.NonPublic;
        private string _mode,_oldID,_freshID,_root,_runID;
        private readonly Dictionary<string,byte[]> _oldFiles=new Dictionary<string,byte[]>();
        private readonly List<string> _audit=new List<string>();
        private Keyboard _keyboard;
        private InputSettings _oldSettings,_settings;
        private bool _oldBackground;
        private System.Diagnostics.Stopwatch _elapsed;
        private Report _report;
        public void Initialize(string mode,GameSessionState fresh,string oldID)
        {
            _mode=mode;_oldID=oldID;_freshID=fresh.GameID;_root=SaveGameService.SaveRootOverride;_runID=Guid.NewGuid().ToString("N");_elapsed=System.Diagnostics.Stopwatch.StartNew();
            if(mode!="empty")
            {
                fresh.Player.SetIntProperty(Marker,111);
                var old=GameSessionState.Capture(oldID,"native-audit",fresh.ZoneManager,fresh.TurnManager,fresh.Player,world:fresh.World);
                SaveGameService.RegisterRuntime(()=>old,null,oldID);
                Check("old_real_quick_and_primary_seeded",SaveGameService.QuickSave()&&SaveGameService.SavePrimary()&&SaveGameService.QuickSave()&&SaveGameService.SavePrimary());
                foreach(string path in Directory.GetFiles(Path.Combine(_root,oldID)))_oldFiles[path]=File.ReadAllBytes(path);
                Check("old_backups_are_real",_oldFiles.Count==8);
            }
            fresh.Player.SetIntProperty(Marker,222);
            _oldSettings=InputSystem.settings;_settings=Instantiate(_oldSettings);_settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings=_settings;_oldBackground=Application.runInBackground;Application.runInBackground=true;
            _keyboard=InputSystem.AddDevice<Keyboard>();StartCoroutine(RunSafely(Audit()));
        }
        private IEnumerator RunSafely(IEnumerator steps)
        {
            var stack=new Stack<IEnumerator>();stack.Push(steps);
            while(stack.Count>0)
            {
                bool moved=false;object current=null;Exception failure=null;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception ex){failure=ex;}
                if(failure!=null){Failures++;Debug.LogError(failure);break;}
                if(!moved){stack.Pop();continue;}if(current is IEnumerator nested){stack.Push(nested);continue;}yield return current;
            }
            Finish();
        }
        private IEnumerator Audit()
        {
            yield return new WaitForSecondsRealtime(.7f);
            var input=FindFirstObjectByType<InputHandler>();if(input==null)throw new InvalidOperationException("Missing live input.");
            var boot=(BootMenuController)typeof(InputHandler).GetField("_bootMenuController",Instance).GetValue(input);
            Check("real_boot_modal_precondition",boot.IsActive==(_mode!="empty"));
            Check("same_isolated_root_after_boot",SaveGameService.SaveRootOverride==_root);
            if(_mode=="failure")
            {
                var capture=(Func<GameSessionState>)typeof(SaveGameService).GetField("_captureCurrent",Static).GetValue(null);
                var apply=(Action<GameSessionState>)typeof(SaveGameService).GetField("_applyLoaded",Static).GetValue(null);
                SaveGameService.RegisterRuntime(()=>null,apply,_freshID);yield return Tap(Key.N);
                Check("failed_initial_save_enters_fresh_world",!boot.IsActive&&ActiveID()==_freshID&&input.PlayerEntity.GetIntProperty(Marker)==222);
                Check("failure_has_no_old_load_target",!SaveGameService.HasQuickSave());
                Check("failure_explains_f5_retry",MessageLog.GetMessages().Any(m=>m.Contains("initial save failed")&&m.Contains("F5")));
                yield return Tap(Key.F6);Check("immediate_f6_cannot_load_old_character",input.PlayerEntity.GetIntProperty(Marker)==222&&ActiveID()==_freshID);
                SaveGameService.RegisterRuntime(capture,apply,_freshID);yield return Tap(Key.F5);
                Check("f5_retry_creates_fresh_checkpoint",SaveGameService.HasQuickSave()&&SaveGameService.GetSaveInfo("Quick").GameID==_freshID);
            }
            else if(_mode=="continue") yield return Tap(Key.C);
            else if(_mode!="empty") yield return Tap(Key.N);
            int expectedMarker=_mode=="continue"?111:222;string expectedID=_mode=="continue"?_oldID:_freshID;
            Check("chosen_character_and_binding",!boot.IsActive&&ActiveID()==expectedID&&input.PlayerEntity.GetIntProperty(Marker)==expectedMarker);
            Check(_mode=="failure"?"recovery_checkpoint_exists_after_f5":"selected_checkpoint_exists_without_manual_save",SaveGameService.HasQuickSave()&&SaveGameService.GetSaveInfo("Quick").GameID==expectedID);
            if(_mode=="continue")Check("continue_does_not_create_fresh_slot",!Directory.Exists(Path.Combine(_root,_freshID)));
            input.PlayerEntity.SetIntProperty(Marker,999);yield return Tap(Key.F6);
            Check("f6_restores_selected_character",ActiveID()==expectedID&&input.PlayerEntity.GetIntProperty(Marker)==expectedMarker);
            Check("loaded_world_aliases_match_live_input",ReferenceEquals(input.PlayerEntity,input.TurnManager.CurrentActor)
                &&ReferenceEquals(input.TurnManager,TurnManager.Active)&&ReferenceEquals(input.CurrentZone,input.ZoneManager.ActiveZone)
                &&input.CurrentZone.GetEntityCell(input.PlayerEntity)?.Objects.Contains(input.PlayerEntity)==true);
            if(_mode!="continue")
            {
                input.PlayerEntity.SetIntProperty(Marker,998);yield return Tap(Key.Escape);yield return Tap(Key.DownArrow);yield return Tap(Key.Enter);
                Check("pause_keyboard_loads_fresh_checkpoint",ActiveID()==_freshID&&input.PlayerEntity.GetIntProperty(Marker)==222);
                input.PlayerEntity.SetIntProperty(Marker,333);yield return Tap(Key.F5);input.PlayerEntity.SetIntProperty(Marker,997);yield return Tap(Key.F6);
                Check("later_f5_and_f6_keep_new_character",ActiveID()==_freshID&&input.PlayerEntity.GetIntProperty(Marker)==333);
                Check("new_backup_stays_in_owned_root",File.Exists(Path.Combine(_root,_freshID,"Quick.sav.gz.bak")));
            }
            Check("previous_save_and_backups_unchanged",_oldFiles.All(e=>File.Exists(e.Key)&&e.Value.SequenceEqual(File.ReadAllBytes(e.Key)))
                &&(_oldFiles.Count==0||Directory.GetFiles(Path.Combine(_root,_oldID)).Length==_oldFiles.Count));
            string normalRoot=Path.Combine(Application.persistentDataPath,"Saves");
            Check("expected_save_paths_are_owned_and_unique_ids_absent_from_normal_root",SaveGameService.SaveRootOverride==_root
                &&File.Exists(Path.Combine(_root,expectedID,"Quick.sav.gz"))
                &&!Directory.Exists(Path.Combine(normalRoot,_oldID))&&!Directory.Exists(Path.Combine(normalRoot,_freshID)));
        }
        private static string ActiveID()=>(string)typeof(SaveGameService).GetField("_activeGameID",Static).GetValue(null);
        private IEnumerator Tap(Key key)
        {InputSystem.QueueStateEvent(_keyboard,new KeyboardState(key));yield return new WaitForSecondsRealtime(.08f);InputSystem.QueueStateEvent(_keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.2f);}
        private void Check(string name,bool pass)
        {_audit.Add((pass?"PASS ":"FAIL ")+name);if(!pass)Failures++;}
        private void Finish()
        {
            _report=new Report{runId=_runID,mode=_mode,oldID=_oldID,freshID=_freshID,root=_root,cases=_audit.Count,failures=Failures,seconds=_elapsed.Elapsed.TotalSeconds,audit=_audit.ToArray()};
            WriteReport();Debug.Log("[GameAuditNewGameSaveBench] "+JsonUtility.ToJson(_report));Finished=true;
        }
        private void WriteReport()
        {
            _report.cases=_audit.Count;_report.failures=Failures;_report.audit=_audit.ToArray();
            string path=Path.Combine(Application.dataPath,"../Docs/Verification/GameSystemAudit/GA03b-native-"+_mode+".json");File.WriteAllText(path,JsonUtility.ToJson(_report,true));
        }
        private void OnDestroy()
        {
            if(_report!=null)
            {
                Check("native_shutdown_keeps_disposable_root",SaveGameService.SaveRootOverride==_root);
                Check("native_shutdown_unregisters_saving",typeof(SaveGameService).GetField("_captureCurrent",Static).GetValue(null)==null);
                _report.shutdownObserved=true;_report.shutdownSeconds=_elapsed.Elapsed.TotalSeconds;WriteReport();
            }
            if(_keyboard!=null)InputSystem.RemoveDevice(_keyboard);
            if(_oldSettings!=null)InputSystem.settings=_oldSettings;if(_settings!=null)Destroy(_settings);Application.runInBackground=_oldBackground;
        }
        [Serializable]private sealed class Report
        {public string runId,mode,oldID,freshID,root;public int cases,failures;public double seconds,shutdownSeconds;public bool shutdownObserved;public string[] audit;}
    }
}
