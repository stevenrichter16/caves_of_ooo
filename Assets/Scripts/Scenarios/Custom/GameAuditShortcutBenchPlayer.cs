using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.Profiling;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Temporary native keyboard driver. Reflection observes UI state; it never selects actions.</summary>
    public sealed class GameAuditShortcutBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        private ScenarioContext _ctx;
        private GameAuditShortcutBench _bench;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground;
        private double _started;
        private readonly Dictionary<string, bool> _channels = new Dictionary<string, bool>();

        private const int SampleCapacity = 500000;
        private readonly string[] _metricNames = { "COO.Input.Update", "COO.ZoneRenderer.LateUpdate", "GC Allocated In Frame" };
        private ProfilerRecorder[] _recorders; private long[][] _samples; private int _frames; private bool _measuring; private double _measuredSeconds, _measurementStarted; private double[] _sampleTimes; private int _navAttempts,_navChanges,_reopenAttempts,_reopenChanges;
        public void Initialize(ScenarioContext ctx, GameAuditShortcutBench bench)
        {
            _ctx = ctx; _bench = bench; _started = Time.realtimeSinceStartupAsDouble;
            foreach (string channel in new[] { "crop", "event", "mineral-trade", "furniture" })
            { _channels[channel] = Diag.IsChannelEnabled(channel); Diag.SetChannel(channel, true); }
            _oldSettings = InputSystem.settings; _settings = Instantiate(_oldSettings);
            _settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = _settings;
            _oldBackground = Application.runInBackground; Application.runInBackground = true;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            StartCoroutine(RunSafely(AuditInputs()));
        }
        private IEnumerator RunSafely(IEnumerator steps)
        {
            // Flatten nested audit helpers so an assertion in any helper writes a failed report immediately.
            var stack = new Stack<IEnumerator>(); stack.Push(steps);
            while (stack.Count > 0)
            {
                bool moved = false; object current = null; Exception failure = null;
                try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; }
                catch (Exception e) { failure = e; }
                if (failure != null) { Failures++; Debug.LogError(failure); break; }
                if (!moved) { stack.Pop(); continue; }
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current;
            }
            Finish();
        }
        private IEnumerator AuditInputs()
        {
            yield return new WaitForSecondsRealtime(.6f);
            var input=FindFirstObjectByType<InputHandler>();if(input==null)throw new InvalidOperationException("Native input missing.");
            var boot=Read(input,"_bootMenuController") as BootMenuController;if(boot==null)throw new InvalidOperationException("Native boot missing.");
            if(boot.IsActive)yield return Tap(Key.N);yield return new WaitForSecondsRealtime(.3f);
            var inv=_ctx.PlayerEntity.GetPart<InventoryPart>();
            _bench.Check("native_boot_keeps_arena",!boot.IsActive&&_ctx.Zone.GetEntityPosition(_ctx.PlayerEntity)==(20,12));
            yield return Tap(Key.G);_bench.Check("native_seven_ground_rows",State(input)=="PickupOpen"&&((List<Entity>)Read(input.PickupUI,"_items")).Count==7);
            _bench.Check("native_ground_seventh_label_h",RowKey(input.PickupUI,input.PickupUI.Tilemap,6,3)=='h');
            yield return Tap(Key.G);_bench.Check("native_g_closes_without_take",State(input)=="Normal"&&_bench.Ground.All(e=>!inv.Contains(e)));
            yield return Tap(Key.G);yield return Tap(Key.H);_bench.Check("native_h_takes_exact_seventh",inv.Contains(_bench.Ground[6])&&_bench.Ground.Take(6).All(e=>!inv.Contains(e)));
            for(int i=0;i<5;i++)yield return Tap(Key.A);_bench.Check("native_one_pickup_row_remains",input.PickupUI.IsOpen&&((List<Entity>)Read(input.PickupUI,"_items")).Count==1);
            yield return HoldSelection(input,Key.A);_bench.Check("native_last_pickup_letter_takes_remaining",State(input)=="Normal"&&_bench.Ground.All(inv.Contains));

            yield return Interact(Key.LeftArrow);yield return SelectMenu(input,"Break",hold:true);
            _bench.Check("native_actual_break_reaches_chest",_bench.Chest.GetPart<DestructiblePart>().HP<_bench.Chest.GetPart<DestructiblePart>().MaxHP);
            yield return Interact(Key.RightArrow);yield return SelectMenu(input,CraftingMarkPart.ToggleCommandPrefix+_bench.Glimmer.ID);
            _bench.Check("native_fallback_marks_actual_reagent",CraftingMarkPart.IsMarked(_bench.Glimmer));
            _bench.Check("native_mark_reopens_station",input.WorldActionMenuUI.IsOpen&&CraftingMarkPart.IsMarked(_bench.Glimmer));yield return SelectMenu(input,"BrewMix");
            _bench.Check("native_single_brew_pays_one",_bench.Glimmer.GetPart<StackerPart>().StackCount==2&&BrewUnits(inv)==1);
            yield return Interact(Key.RightArrow);yield return SelectMenu(input,"BrewMixBatch");
            _bench.Check("native_batch_brew_pays_remaining_two",!inv.Contains(_bench.Glimmer)&&BrewUnits(inv)==3);

            yield return Interact(Key.UpArrow);yield return SelectMenu(input,"Chat");var dialogue=input.DialogueUI;
            _bench.Check("native_staged_ten_dialogue_rows",dialogue.IsOpen&&ConversationManager.VisibleChoices.Count==10&&(bool)Read(dialogue,"_revealing"));
            yield return Tap(Key.L);_bench.Check("native_mapped_l_reveals_only",dialogue.IsOpen&&!(bool)Read(dialogue,"_revealing")&&ConversationManager.PendingAttackTarget==null);
            yield return Tap(Key.L);_bench.Check("native_l_enters_attack_confirmation",State(input)=="AwaitingAttackConfirm"&&!dialogue.IsOpen);yield return Tap(Key.Escape);
            yield return Interact(Key.UpArrow);yield return SelectMenu(input,"Chat");yield return Tap(Key.Space);yield return HoldSelection(input,Key.A);
            _bench.Check("native_benign_dialogue_closes",State(input)=="Normal"&&!dialogue.IsOpen);

            yield return Tap(Key.DownArrow);for(int i=0;i<4;i++)yield return Tap(Key.RightArrow);yield return Tap(Key.UpArrow);
            _bench.Check("native_reaches_crowded_container_cell",_ctx.Zone.GetEntityPosition(_ctx.PlayerEntity)==(24,12));
            yield return Tap(Key.G);var picker=input.ContainerPickerUI;
            _bench.Check("native_same_cell_container_picker",State(input)=="ContainerPickerOpen"&&picker.IsOpen&&RowKey(picker,picker.Tilemap,6,3)=='h');
            var contents=_bench.Sacks[6].GetPart<ContainerPart>().Contents.Single();int units=inv.Objects.Where(e=>e.BlueprintName==contents.BlueprintName).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
            yield return HoldSelection(input,Key.H);
            _bench.Check("native_container_h_pays_exact_source",State(input)=="Normal"&&_bench.Sacks[6].GetPart<ContainerPart>().Contents.Count==0&&_bench.Sacks.Take(6).All(e=>e.GetPart<ContainerPart>().Contents.Count==1)&&inv.Objects.Where(e=>e.BlueprintName==contents.BlueprintName).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1)==units+1);
            yield return Tap(Key.H);_bench.Check("native_fresh_h_moves_after_release",_ctx.Zone.GetEntityPosition(_ctx.PlayerEntity)==(23,12));yield return Tap(Key.RightArrow);
            yield return Tap(Key.DownArrow);for(int i=0;i<4;i++)yield return Tap(Key.LeftArrow);yield return Tap(Key.UpArrow);
            _bench.Check("native_returns_to_station",_ctx.Zone.GetEntityPosition(_ctx.PlayerEntity)==(20,12));
            yield return Interact(Key.RightArrow);yield return Measure(input);yield return Tap(Key.Escape);
            _bench.Check("native_final_normal",State(input)=="Normal"&&!input.WorldActionMenuUI.IsOpen);
        }
        private IEnumerator Interact(Key direction){yield return Tap(Key.C);yield return Tap(direction);}
        private static int BrewUnits(InventoryPart inv)=>inv.Objects.Where(e=>e.HasPart<BrewItemPart>()).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
        private IEnumerator SelectMenu(InputHandler input,string command,bool hold=false)
        {
            var ui=input.WorldActionMenuUI;var actions=Read(ui,"_actions") as List<InventoryAction>;int row=actions?.FindIndex(a=>a.Command==command)??-1;
            _bench.Check("native_menu_action_"+command.Split(':')[0],ui.IsOpen&&row>=0);yield return MoveCursor(ui,"_cursorIndex",row);
            int content=(int)ui.GetType().GetProperty("ContentY",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ui);
            char key=RowKey(ui,ui.Tilemap,row-(int)Read(ui,"_scrollOffset"),content);var action=actions[row];char raw=action.Key;
            _bench.Check("native_displayed_key_"+command.Split(':')[0],key!='\0'&&key!='j'&&key!='k');
            if(hold)yield return HoldSelection(input,Letter(key));else yield return Tap(Letter(key));
            _bench.Check("native_authored_key_preserved_"+command.Split(':')[0],action.Key==raw);
        }
        private IEnumerator HoldSelection(InputHandler input,Key key)
        {
            var before=_ctx.Zone.GetEntityPosition(_ctx.PlayerEntity);InputSystem.QueueStateEvent(_keyboard,new KeyboardState(key));yield return new WaitForSecondsRealtime(.5f);
            _bench.Check("native_held_selection_stays_put_"+key,State(input)=="Normal"&&_ctx.Zone.GetEntityPosition(_ctx.PlayerEntity)==before);
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState());yield return new WaitForSecondsRealtime(.15f);
            _bench.Check("native_release_clears_gate_"+key,(KeyCode)Read(input,"_worldActionKeyToRelease")==KeyCode.None&&_ctx.Zone.GetEntityPosition(_ctx.PlayerEntity)==before);
        }
        private static Key Letter(char c)=>(Key)((int)Key.A+c-'a');
        private static char RowKey(object ui,UnityEngine.Tilemaps.Tilemap tiles,int row,int content)
        {int x=(int)Read(ui,"_worldOriginX")+2,y=(int)Read(ui,"_worldTopY")-content-row;var tile=tiles.GetTile(new Vector3Int(x,y,0));for(char c='a';c<='z';c++)if(tile==CP437TilesetGenerator.GetUiTile(c))return c;return '\0';}
        private IEnumerator Measure(InputHandler input)
        {
            var menu=input.WorldActionMenuUI;var actions=(List<InventoryAction>)Read(menu,"_actions");int examine=actions.FindIndex(a=>a.Command=="Examine");
            int content=(int)menu.GetType().GetProperty("ContentY",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(menu);
            char shown=RowKey(menu,menu.Tilemap,examine-(int)Read(menu,"_scrollOffset"),content);
            _bench.Check("native_measured_examine_binding",examine>=0&&shown=='x'&&actions[examine].Key=='x');
            Key examineKey=Letter(shown);
            _recorders=new ProfilerRecorder[_metricNames.Length];_samples=new long[_metricNames.Length][];
            for(int i=0;i<_metricNames.Length;i++)
            { _samples[i]=new long[SampleCapacity];_recorders[i]=ProfilerRecorder.StartNew(i==_metricNames.Length-1?ProfilerCategory.Memory:ProfilerCategory.Scripts,_metricNames[i],2,ProfilerRecorderOptions.Default|ProfilerRecorderOptions.SumAllSamplesInFrame); }
            _frames=0;_sampleTimes=new double[SampleCapacity];_measuring=true;double start=Time.realtimeSinceStartupAsDouble,next=start;_measurementStarted=start;int pulse=0;
            while(Time.realtimeSinceStartupAsDouble-start<75)
            {
                double elapsed=Time.realtimeSinceStartupAsDouble-start;
                if(elapsed>=25&&Time.realtimeSinceStartupAsDouble>=next)
                {
                    next=Time.realtimeSinceStartupAsDouble+.36;int cursor=(int)Read(input.WorldActionMenuUI,"_cursorIndex");
                    bool navigation=elapsed<50;if(navigation)_navAttempts++;else _reopenAttempts++;
                    if(navigation)yield return Tap(pulse++%2==0?Key.DownArrow:Key.UpArrow);
                    else {yield return Tap(examineKey);bool closed=!input.WorldActionMenuUI.IsOpen&&State(input)=="Normal";yield return Interact(Key.RightArrow);
                        if(closed&&input.WorldActionMenuUI.IsOpen&&State(input)=="WorldActionMenuOpen"&&input.WorldActionMenuUI.SelectedTarget==_bench.Still)_reopenChanges++;}
                    if(navigation&&cursor!=(int)Read(input.WorldActionMenuUI,"_cursorIndex"))_navChanges++;
                }
                else yield return null;
            }
            _measuring=false;_measuredSeconds=Time.realtimeSinceStartupAsDouble-start;
            string prefix="FLOW3-after";var metrics=new Metric[_metricNames.Length];var csv=new StringBuilder("frame,seconds,phase,"+string.Join(",",_metricNames)+"\n");
            for(int f=0;f<_frames;f++){csv.Append(f).Append(',').Append(_sampleTimes[f].ToString("F6",System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(_sampleTimes[f]<25?"idle":_sampleTimes[f]<50?"navigate":"reopen");for(int i=0;i<_metricNames.Length;i++)csv.Append(',').Append(_samples[i][f]);csv.Append('\n');}
            for(int i=0;i<_metricNames.Length;i++)
            { var values=_samples[i].Take(_frames).OrderBy(v=>v).ToArray();metrics[i]=new Metric{name=_metricNames[i],unit=i==_metricNames.Length-1?"bytes":"nanoseconds",max=values[^1],p99=values[Math.Min(values.Length-1,(int)(values.Length*.99))],mean=values.Average(v=>(double)v),valid=_recorders[i].Valid};_recorders[i].Dispose(); }
            _recorders=null;string directory=Path.Combine(Application.dataPath,"../Docs/Verification/GameSystemAudit");
            File.WriteAllText(Path.Combine(directory,prefix+"-frames.csv"),csv.ToString());
            File.WriteAllText(Path.Combine(directory,prefix+"-perf.json"),JsonUtility.ToJson(new PerfReport{runId=_bench.RunId,seconds=_measuredSeconds,frames=_frames,metrics=metrics,navAttempts=_navAttempts,navChanges=_navChanges,reopenAttempts=_reopenAttempts,reopenChanges=_reopenChanges,state=State(input),open=input.WorldActionMenuUI.IsOpen},true));
            _bench.Check("native_75second_menu_workload",_measuredSeconds>=75&&_frames>100&&_frames<SampleCapacity&&metrics.All(m=>m.valid)
                && input.WorldActionMenuUI.IsOpen&&State(input)=="WorldActionMenuOpen"&&_navChanges>=50&&_navChanges==_navAttempts&&_reopenChanges>=20&&_reopenChanges==_reopenAttempts);

        }
        private void LateUpdate()
        {
            if(!_measuring)return;if(_frames>=SampleCapacity)return;
            _sampleTimes[_frames]=Time.realtimeSinceStartupAsDouble-_measurementStarted;for(int i=0;i<_recorders.Length;i++)_samples[i][_frames]=_recorders[i].Valid?_recorders[i].LastValue:-1;_frames++;
        }
        private IEnumerator MoveCursor(object ui, string field, int row)
        {
            for (int i = 0; i < 100; i++)
            {
                int cursor = (int)Read(ui, field); if (cursor == row) yield break;
                yield return Tap(cursor < row ? Key.DownArrow : Key.UpArrow);
                if ((int)Read(ui, field) == cursor) throw new InvalidOperationException("Native cursor did not move toward intended row.");
            }
            throw new InvalidOperationException("Native cursor did not reach intended row.");
        }
        private IEnumerator Tap(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key)); yield return new WaitForSecondsRealtime(0.06f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return new WaitForSecondsRealtime(0.12f);
        }
        private static object Read(object owner, string field) => owner?.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(owner);
        private static string State(InputHandler input) => Read(input, "_inputState")?.ToString();
        private void Finish()
        {
            Failures = Math.Max(Failures, _bench.Failures);
            var report = new Report { runId = _bench.RunId, seconds = Time.realtimeSinceStartupAsDouble - _started,
                cases = _bench.Cases, failures = Failures, audit = _bench.Audit.ToArray() };
            string directory = Path.Combine(Application.dataPath, "../Docs/Verification/GameSystemAudit"); Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "FLOW3-native.json"), JsonUtility.ToJson(report, true));
            Debug.Log("[GameAuditShortcutBench] " + JsonUtility.ToJson(report)); Finished = true;
        }
        private void OnDestroy()
        {
            if (_recorders != null) foreach (var recorder in _recorders) recorder.Dispose();
            foreach (var channel in _channels) Diag.SetChannel(channel.Key, channel.Value);
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
            if (_oldSettings != null) InputSystem.settings = _oldSettings;
            if (_settings != null) Destroy(_settings);
            Application.runInBackground = _oldBackground;
        }
        [Serializable] private sealed class Metric { public string name; public string unit; public long max,p99;public double mean;public bool valid; }
        [Serializable] private sealed class PerfReport { public string runId;public double seconds;public int frames,navAttempts,navChanges,reopenAttempts,reopenChanges;public string state;public bool open;public Metric[] metrics; }
        [Serializable] private sealed class Report
        { public string runId; public double seconds; public int cases, failures; public string[] audit; }
    }
}
