using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Queued keyboard drives real bootstrap selection/save/load. Sparse
    /// ability setup and removals are explicit fixture changes, never UI evidence.</summary>
    public sealed class GameAuditHotbarSaveBenchPlayer : MonoBehaviour
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
        public void Initialize(Entity player)
        {
            _markerID=PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey);_root=SaveGameService.SaveRootOverride;_runID=Guid.NewGuid().ToString("N");_elapsed=System.Diagnostics.Stopwatch.StartNew();
            Seed(player);
            _oldSettings=InputSystem.settings;_settings=Instantiate(_oldSettings);_settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings=_settings;_oldBackground=Application.runInBackground;Application.runInBackground=true;
            _keyboard=InputSystem.AddDevice<Keyboard>();StartCoroutine(RunSafely(Audit()));
        }
        private static void Clear(Entity player)
        {
            var part=player.GetPart<ActivatedAbilitiesPart>();if(part==null)return;
            for(int i=part.AbilityList.Count-1;i>=0;i--)part.RemoveAbility(part.AbilityList[i].ID);
        }
        private static void Seed(Entity player)
        {
            Clear(player);var part=player.GetPart<ActivatedAbilitiesPart>();if(part==null){part=new ActivatedAbilitiesPart();player.AddPart(part);}
            foreach(int slot in new[]{5,9}){var id=part.AddAbility("Save Audit "+slot,"AuditHotbar"+slot,"Audit");part.AssignAbilityToSlot(id,slot);}
            part.GetAbilityBySlot(9).CooldownRemaining=7;
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
            var input=FindFirstObjectByType<InputHandler>();if(input==null)throw new InvalidOperationException("Missing input.");
            var boot=(BootMenuController)Read(input,"_bootMenuController");Check("boot_menu_before_new_game",boot.IsActive);yield return Tap(Key.N);
            Check("new_game_has_initial_checkpoint",!boot.IsActive&&SaveGameService.HasQuickSave());_freshID=SaveGameService.GetSaveInfo("Quick").GameID;
            Check("sparse_hotbar_starts_at_first_occupied",Selected(input)==5&&Rendered(input)==5);
            int tick=input.TurnManager.TickCount,energy=input.TurnManager.GetEnergy(input.PlayerEntity);var oldPlayer=input.PlayerEntity;
            yield return Tap(Key.RightBracket);Check("right_bracket_selects_cooldown_slot9",Selected(input)==9&&Rendered(input)==9);CheckUnspent("right_bracket",input,tick,energy);
            yield return Tap(Key.F5);Check("f5_creates_checkpoint",SaveGameService.HasQuickSave());
            yield return Tap(Key.LeftBracket);Check("left_bracket_changes_to_slot5",Selected(input)==5&&Rendered(input)==5);CheckUnspent("left_bracket",input,tick,energy);
            yield return Tap(Key.F6);
            Check("f6_replaces_actor",!ReferenceEquals(oldPlayer,input.PlayerEntity));
            Check("f6_restores_selected_slot9_in_input_and_renderer",Selected(input)==9&&Rendered(input)==9);
            Check("f6_preserves_cooldown7",input.PlayerEntity.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(9).CooldownRemaining==7);
            Check("selection_save_load_spends_no_turn_or_energy",input.TurnManager.TickCount==tick&&input.TurnManager.GetEnergy(input.PlayerEntity)==energy);
            Check("selection_does_not_enter_targeting",Read(input,"_pendingAbility")==null&&Read(input,"_inputState").ToString()=="Normal");
            Check("loaded_bindings_and_aliases_match",input.PlayerEntity.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(5)?.Command=="AuditHotbar5"&&input.PlayerEntity.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(9)?.Command=="AuditHotbar9"&&ReferenceEquals(input.PlayerEntity,input.TurnManager.CurrentActor));
            // Explicit fixture mutation creates an empty character checkpoint.
            Clear(input.PlayerEntity);yield return new WaitForSecondsRealtime(.1f);Check("empty_hotbar_clears_selection",Selected(input)==-1&&Rendered(input)==-1);
            yield return Tap(Key.F5);Seed(input.PlayerEntity);yield return new WaitForSecondsRealtime(.1f);
            Check("fixture_repopulation_selects_first_slot",Selected(input)==5&&Rendered(input)==5);yield return Tap(Key.RightBracket);Check("repopulated_bracket_reaches9",Selected(input)==9);CheckUnspent("repopulated_bracket",input,tick,energy);
            yield return Tap(Key.F6);Check("empty_checkpoint_clears_prior_occupied_selection",Selected(input)==-1&&Rendered(input)==-1&&input.PlayerEntity.GetPart<ActivatedAbilitiesPart>().AbilityList.Count==0);
            Check("empty_restore_spends_no_turn_or_energy",input.TurnManager.TickCount==tick&&input.TurnManager.GetEnergy(input.PlayerEntity)==energy);
            Check("native_saves_remain_in_owned_root",SaveGameService.SaveRootOverride==_root&&_freshID!=_markerID
                &&File.Exists(Path.Combine(_root,_freshID,"Quick.sav.gz"))&&File.Exists(Path.Combine(_root,_markerID,"Quick.sav.gz"))
                &&Directory.GetFiles(_root,"Quick.sav.gz",SearchOption.AllDirectories).Length==2
                &&!Directory.Exists(Path.Combine(Application.persistentDataPath,"Saves",_freshID)));
        }
        private void CheckUnspent(string label,InputHandler input,int tick,int energy)
        {Check(label+"_does_not_spend_or_target_before_load",input.TurnManager.TickCount==tick&&input.TurnManager.GetEnergy(input.PlayerEntity)==energy
            &&input.PlayerEntity.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(9).CooldownRemaining==7
            &&Read(input,"_pendingAbility")==null&&Read(input,"_inputState").ToString()=="Normal");}
        private static object Read(object owner,string field)=>owner.GetType().GetField(field,Instance).GetValue(owner);
        private static int Selected(InputHandler input)=>(int)Read(input,"_selectedHotbarSlot");
        private static int Rendered(InputHandler input)=>(int)Read(input.ZoneRenderer,"_selectedHotbarSlot");
        private IEnumerator Tap(Key key)
        {InputSystem.QueueStateEvent(_keyboard,new KeyboardState(key));yield return new WaitForSecondsRealtime(.08f);InputSystem.QueueStateEvent(_keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.2f);}
        private void Check(string name,bool pass){_audit.Add((pass?"PASS ":"FAIL ")+name);if(!pass)Failures++;}
        private void Finish()
        {
            _report=new Report{runId=_runID,root=_root,freshID=_freshID,markerID=_markerID,seconds=_elapsed.Elapsed.TotalSeconds};WriteReport();Debug.Log("[GameAuditHotbarSaveBench] "+JsonUtility.ToJson(_report));Finished=true;
        }
        private void WriteReport()
        {_report.cases=_audit.Count;_report.failures=Failures;_report.audit=_audit.ToArray();File.WriteAllText(Path.Combine(Application.dataPath,"../Docs/Verification/GameSystemAudit/GA03c-native.json"),JsonUtility.ToJson(_report,true));}
        private void OnDestroy()
        {
            if(_report!=null){Check("shutdown_keeps_disposable_root",SaveGameService.SaveRootOverride==_root);_report.shutdownObserved=true;_report.shutdownSeconds=_elapsed.Elapsed.TotalSeconds;WriteReport();}
            if(_keyboard!=null)InputSystem.RemoveDevice(_keyboard);if(_oldSettings!=null)InputSystem.settings=_oldSettings;if(_settings!=null)Destroy(_settings);Application.runInBackground=_oldBackground;
        }
        [Serializable]private sealed class Report
        {public string runId,root,freshID,markerID;public int cases,failures;public double seconds,shutdownSeconds;public bool shutdownObserved;public string[] audit;}
    }
}
