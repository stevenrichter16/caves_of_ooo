using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Queued keyboard drives real save rejection and recovery. Marker
    /// mutations and owned-file corruption are fixture setup, never player actions.</summary>
    public sealed class GameAuditDecodeSaveBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        private const BindingFlags Instance=BindingFlags.Instance|BindingFlags.NonPublic;
        private string _root,_runID,_markerID,_freshID;
        private readonly List<string> _audit=new List<string>();
        private Keyboard _keyboard;
        private InputSettings _oldSettings,_settings;
        private bool _oldBackground;
        private System.Diagnostics.Stopwatch _elapsed;
        private Report _report;
        private bool _expectRejection;
        private int _expectedRejections;
        public void Initialize(Entity player)
        {
            _markerID=PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey);_root=SaveGameService.SaveRootOverride;_runID=Guid.NewGuid().ToString("N");_elapsed=System.Diagnostics.Stopwatch.StartNew();
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
            yield return new WaitForSecondsRealtime(.7f);var input=FindFirstObjectByType<InputHandler>();if(input==null)throw new InvalidOperationException("Missing input.");
            var boot=(BootMenuController)Read(input,"_bootMenuController");Check("real_boot_menu",boot.IsActive);yield return Tap(Key.N);
            Check("new_checkpoint_created",!boot.IsActive&&SaveGameService.HasQuickSave());_freshID=SaveGameService.GetSaveInfo("Quick").GameID;
            input.PlayerEntity.SetIntProperty("DecodeNativeMarker",111);PlayerReputation.Set("DecodeNativeFaction",11);yield return Tap(Key.F5);
            string path=Path.Combine(_root,_freshID,"Quick.sav.gz");byte[] valid;
            using(var file=File.OpenRead(path))using(var gzip=new GZipStream(file,CompressionMode.Decompress))using(var bytes=new MemoryStream()){gzip.CopyTo(bytes);valid=bytes.ToArray();}
            Check("actual_checkpoint_has_payload",valid.Length>100);
            var corrupt=(byte[])valid.Clone();corrupt[corrupt.Length-1]^=0xff;WriteOwned(path,corrupt);
            input.PlayerEntity.SetIntProperty("DecodeNativeMarker",222);PlayerReputation.Set("DecodeNativeFaction",33);MessageLog.Add("Live decode audit sentinel.");
            var actor=input.PlayerEntity;var turns=input.TurnManager;var settlements=SettlementManager.Current;var entries=MessageLog.GetAllEntries();int tick=turns.TickCount,energy=turns.GetEnergy(actor);
            Check("live_manager_aliases_before_failure",ReferenceEquals(turns,TurnManager.Active)&&ReferenceEquals(settlements,((OverworldZoneManager)input.ZoneManager).SettlementManager));
            _expectRejection=true;yield return Tap(Key.F6);_expectRejection=false;
            Check("one_expected_footer_rejection",_expectedRejections==1);
            Check("failure_keeps_live_actor_and_marker",ReferenceEquals(actor,input.PlayerEntity)&&actor.GetIntProperty("DecodeNativeMarker")==222);
            Check("failure_keeps_active_turn_manager",ReferenceEquals(turns,TurnManager.Active)&&ReferenceEquals(turns,input.TurnManager));
            Check("failure_keeps_current_settlement_manager",ReferenceEquals(settlements,SettlementManager.Current)&&ReferenceEquals(settlements,((OverworldZoneManager)input.ZoneManager).SettlementManager));
            Check("failure_keeps_live_reputation",PlayerReputation.Get("DecodeNativeFaction")==33);
            Check("failure_spends_no_action",turns.TickCount==tick&&turns.GetEnergy(actor)==energy);
            var after=MessageLog.GetAllEntries();Check("failure_preserves_history_and_adds_truthful_message",after.Count==entries.Count+1&&entries.Select((entry,i)=>entry.Text==after[i].Text&&entry.Tick==after[i].Tick&&entry.Serial==after[i].Serial).All(v=>v)&&after.Last().Text=="Load failed — save may be corrupted.");
            Check("failure_retains_normal_input",Read(input,"_inputState").ToString()=="Normal");
            // Restore the exact saved payload in owned storage, then retry via F6.
            WriteOwned(path,valid);yield return Tap(Key.F6);
            Check("valid_retry_replaces_actor_and_restores_marker",!ReferenceEquals(actor,input.PlayerEntity)&&input.PlayerEntity.GetIntProperty("DecodeNativeMarker")==111);
            Check("valid_retry_restores_reputation",PlayerReputation.Get("DecodeNativeFaction")==11);
            Check("valid_retry_publishes_correct_manager_aliases",ReferenceEquals(input.TurnManager,TurnManager.Active)&&ReferenceEquals(((OverworldZoneManager)input.ZoneManager).SettlementManager,SettlementManager.Current)&&!ReferenceEquals(settlements,SettlementManager.Current));
            Check("valid_retry_restores_turn_and_energy",input.TurnManager.TickCount==tick&&input.TurnManager.GetEnergy(input.PlayerEntity)==energy);
            Check("valid_retry_restores_character_zone_alias",ReferenceEquals(input.PlayerEntity,input.TurnManager.CurrentActor)&&input.CurrentZone.GetEntityCell(input.PlayerEntity)!=null);
            Check("recovery_reports_success",MessageLog.GetLast()=="Game loaded.");
            input.PlayerEntity.SetIntProperty("DecodeNativeMarker",333);int beforeSave=MessageLog.GetAllEntries().Count;
            yield return Tap(Key.F5);Check("recovered_session_can_save_again",SaveGameService.HasQuickSave()&&SaveGameService.GetSaveInfo("Quick").GameID==_freshID&&MessageLog.GetAllEntries().Count==beforeSave+1&&MessageLog.GetLast()=="Game saved.");
            input.PlayerEntity.SetIntProperty("DecodeNativeMarker",444);yield return Tap(Key.F6);Check("new_checkpoint_has_updated_payload",input.PlayerEntity.GetIntProperty("DecodeNativeMarker")==333);
            Check("all_save_paths_are_owned",SaveGameService.SaveRootOverride==_root&&File.Exists(path)&&File.Exists(Path.Combine(_root,_markerID,"Quick.sav.gz"))&&Directory.GetFiles(_root,"Quick.sav.gz",SearchOption.AllDirectories).Length==2&&!Directory.Exists(Path.Combine(Application.persistentDataPath,"Saves",_freshID)));
        }
        private static void WriteOwned(string path,byte[] bytes)
        {using(var file=File.Create(path))using(var gzip=new GZipStream(file,CompressionMode.Compress))gzip.Write(bytes,0,bytes.Length);}
        public bool ObserveExpectedRejection(string message,LogType type)
        {if(!_expectRejection||type!=LogType.Error||message!="[Save] Load of slot 'Quick' failed: Save section check failed: GameSession.End.")return false;_expectedRejections++;return true;}
        private static object Read(object owner,string field)=>owner.GetType().GetField(field,Instance).GetValue(owner);
        private IEnumerator Tap(Key key)
        {InputSystem.QueueStateEvent(_keyboard,new KeyboardState(key));yield return new WaitForSecondsRealtime(.08f);InputSystem.QueueStateEvent(_keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.2f);}
        private void Check(string name,bool pass){_audit.Add((pass?"PASS ":"FAIL ")+name);if(!pass)Failures++;}
        private void Finish()
        {
            _report=new Report{runId=_runID,root=_root,expectedRejections=_expectedRejections,freshID=_freshID,markerID=_markerID,seconds=_elapsed.Elapsed.TotalSeconds};WriteReport();Debug.Log("[GameAuditDecodeSaveBench] "+JsonUtility.ToJson(_report));Finished=true;
        }
        private void WriteReport()
        {_report.cases=_audit.Count;_report.failures=Failures;_report.audit=_audit.ToArray();File.WriteAllText(Path.Combine(Application.dataPath,"../Docs/Verification/GameSystemAudit/GA03d-native.json"),JsonUtility.ToJson(_report,true));}
        private void OnDestroy()
        {
            if(_report!=null){Check("shutdown_keeps_disposable_root",SaveGameService.SaveRootOverride==_root);_report.shutdownObserved=true;_report.shutdownSeconds=_elapsed.Elapsed.TotalSeconds;WriteReport();}
            if(_keyboard!=null)InputSystem.RemoveDevice(_keyboard);if(_oldSettings!=null)InputSystem.settings=_oldSettings;if(_settings!=null)Destroy(_settings);Application.runInBackground=_oldBackground;
        }
        [Serializable]private sealed class Report
        {public string runId,root,freshID,markerID;public int cases,failures,expectedRejections;public double seconds,shutdownSeconds;public bool shutdownObserved;public string[] audit;}
    }
}
