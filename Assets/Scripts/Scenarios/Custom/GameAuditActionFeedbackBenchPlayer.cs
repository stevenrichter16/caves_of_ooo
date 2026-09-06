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
    public sealed class GameAuditActionFeedbackBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        private ScenarioContext _ctx;
        private GameAuditActionFeedbackBench _bench;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground;
        private double _started;
        private readonly Dictionary<string, bool> _channels = new Dictionary<string, bool>();

        private const int SampleCapacity = 500000;
        private readonly string[] _metricNames = { "COO.Input.Update", "COO.UI.Inventory.Render", "COO.ZoneRenderer.LateUpdate", "GC Allocated In Frame" };
        private ProfilerRecorder[] _recorders; private long[][] _samples; private int _frames; private bool _measuring; private double _measuredSeconds, _measurementStarted; private double[] _sampleTimes; private int _navAttempts,_navChanges,_toggleAttempts,_toggleChanges;
        public void Initialize(ScenarioContext ctx, GameAuditActionFeedbackBench bench)
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
            _bench.Check("native_boot_keeps_arena",!boot.IsActive&&_ctx.Zone.GetEntityPosition(_ctx.PlayerEntity)==(20,12));
            var ui=input.InventoryUI;var inv=_ctx.PlayerEntity.GetPart<InventoryPart>();var body=_ctx.PlayerEntity.GetPart<Body>();var box=_bench.Sack.GetPart<ContainerPart>();
            var slots=body.GetParts().Where(p=>p._Equipped==_bench.EquippedDagger).ToArray();
            _bench.Check("native_full_sack_and_distinct_equipment",box.Contents.Count==6&&box.MaxItems==6&&slots.Length>0&&_bench.CarriedDagger!=_bench.EquippedDagger&&_bench.CarriedDagger.GetPart<StackerPart>().StackCount==2);
            yield return OpenPanel(input,1);yield return ItemAction(input,_bench.EquippedDagger,"put_container",false);
            StatusTiles(ui,"Container is full.");
            _bench.Check("native_refusal_preserves_exact_equipment",slots.All(p=>p._Equipped==_bench.EquippedDagger)&&box.Contents.Count==6&&_bench.CarriedDagger.GetPart<StackerPart>().StackCount==2);
            yield return Tap(Key.Escape);_bench.Check("native_popup_cancel_keeps_inventory_and_clears_status",ui.IsOpen&&Read(ui,"_itemActionPopup")==null&&NoStatus(ui));
            yield return Tap(Key.Escape);_bench.Check("native_inventory_exit",State(input)=="Normal");
            yield return Tap(Key.C);yield return Tap(Key.Period);yield return MenuAction(input,"PickCell");yield return MenuAction(input,"PickTarget:"+_bench.Sack.ID);yield return MenuAction(input,"OpenContainer");
            var pickup=input.PickupUI;var items=Read(pickup,"_items") as List<Entity>;int filler=items?.IndexOf(_bench.Filler)??-1;
            _bench.Check("native_actual_sack_loot",pickup.IsOpen&&Read(pickup,"_sourceContainer")==_bench.Sack&&filler>=0);
            yield return MoveCursor(pickup,"_cursorIndex",filler);yield return Tap(Key.Enter);
            _bench.Check("native_loot_repairs_capacity",inv.Contains(_bench.Filler)&&box.Contents.Count==5&&!box.Contents.Contains(_bench.Filler));yield return Tap(Key.Escape);
            yield return OpenPanel(input,1);yield return ItemAction(input,_bench.EquippedDagger,"put_container",true);
            _bench.Check("native_put_retry_transfers_once",box.Contents.Count==6&&box.Contents.Count(e=>e==_bench.EquippedDagger)==1&&slots.All(p=>p._Equipped!=_bench.EquippedDagger)&&!inv.Contains(_bench.EquippedDagger)&&_bench.CarriedDagger.GetPart<StackerPart>().StackCount==2&&NoStatus(ui));
            yield return Tap(Key.Escape);

            yield return OpenPanel(input,2);yield return Tap(Key.B);yield return SelectRecipe(ui,"craft_dagger");var bits=_ctx.PlayerEntity.GetPart<BitLockerPart>();int recipeCursor=(int)Read(ui,"_tinkerCursorIndex");
            _bench.Check("native_tinker_missing_only_B",bits.GetBitCount('B')==0&&bits.GetBitCount('C')==1);
            yield return Tap(Key.Enter);StatusTiles(ui,"Not enough bits.");_bench.Check("native_tinker_refusal_keeps_recipe_and_stock",(int)Read(ui,"_tinkerCursorIndex")==recipeCursor&&_bench.CarriedDagger.GetPart<StackerPart>().StackCount==2&&bits.GetBitCount('B')==0&&bits.GetBitCount('C')==1);
            yield return Tap(Key.LeftArrow);yield return ItemAction(input,_bench.CarriedDagger,"disassemble",true);
            _bench.Check("native_disassembly_supplies_missing_bit",_bench.CarriedDagger.GetPart<StackerPart>().StackCount==1&&bits.GetBitCount('B')==1&&bits.GetBitCount('C')==1);
            yield return Tap(Key.Tab);yield return SelectRecipe(ui,"craft_dagger");yield return Tap(Key.Enter);
            _bench.Check("native_tinker_retry_pays_once_and_merges",_bench.CarriedDagger.GetPart<StackerPart>().StackCount==2&&bits.GetBitCount('B')==0&&bits.GetBitCount('C')==0&&NoStatus(ui));yield return Tap(Key.Escape);

            yield return OpenPanel(input,4);yield return Tap(Key.F);yield return Measure(input);yield return Tap(Key.C);
            yield return Tap(Key.Enter);string missing=(string)Read(ui,"_actionStatus");_bench.Check("native_incomplete_forge_has_reason",!string.IsNullOrEmpty(missing)&&_bench.Blade.GetPart<StackerPart>().StackCount==3&&!inv.Objects.Any(e=>e.HasPart<WeaponAssemblyPart>()));StatusTiles(ui,missing);
            yield return Pick(input,_bench.Blade);yield return Pick(input,_bench.Haft);yield return Pick(input,_bench.Binding);yield return Tap(Key.Enter);
            _bench.Check("native_forge_retry_pays_one_kit",inv.Objects.Where(e=>e.HasPart<WeaponAssemblyPart>()).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1)==1&&_bench.Blade.GetPart<StackerPart>().StackCount==2&&_bench.Haft.GetPart<StackerPart>().StackCount==2&&_bench.Binding.GetPart<StackerPart>().StackCount==2&&NoStatus(ui));
            yield return Tap(Key.B);yield return Tap(Key.C);yield return Pick(input,_bench.Moss);_bench.Check("native_invalid_moss_preview",!((BrewPreview)Read(ui,"_brewPreview")).IsValid);yield return Tap(Key.Enter);
            string brewReason=(string)Read(ui,"_actionStatus");_bench.Check("native_invalid_brew_keeps_reagent",!string.IsNullOrEmpty(brewReason)&&_bench.Moss.GetPart<StackerPart>().StackCount==1&&!inv.Objects.Any(e=>e.HasPart<BrewItemPart>()));StatusTiles(ui,brewReason);
            yield return Tap(Key.Space);yield return Pick(input,_bench.Glimmer);yield return Tap(Key.Enter);
            _bench.Check("native_brew_retry_pays_valid_reagent_only",_bench.Moss.GetPart<StackerPart>().StackCount==1&&_bench.Glimmer.GetPart<StackerPart>().StackCount==2&&inv.Objects.Where(e=>e.HasPart<BrewItemPart>()).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1)==1&&NoStatus(ui));
            yield return Tap(Key.Escape);_bench.Check("native_final_normal",State(input)=="Normal"&&!ui.IsOpen);
        }
        private IEnumerator OpenPanel(InputHandler input,int panel)
        {
            yield return Tap(Key.I);var ui=input.InventoryUI;
            for(int i=0;i<5&&(int)Read(ui,"_panel")!=panel;i++)yield return Tap(Key.Tab);
            _bench.Check("native_open_panel_"+panel,ui.IsOpen&&(int)Read(ui,"_panel")==panel);
        }
        private IEnumerator ItemAction(InputHandler input,Entity item,string command,bool success)
        {
            var ui=input.InventoryUI;var rows=Read(ui,"_rows") as IList;int row=-1;for(int i=0;rows!=null&&i<rows.Count;i++)if(Read(Read(rows[i],"Item"),"Item")==item)row=i;
            _bench.Check("native_item_row_"+command,ui.IsOpen&&(int)Read(ui,"_panel")==1&&row>=0);yield return MoveCursor(ui,"_cursorIndex",row);yield return Tap(Key.Enter);
            var popup=Read(ui,"_itemActionPopup");var actions=Read(popup,"Actions") as IList;int action=-1;
            for(int i=0;actions!=null&&i<actions.Count;i++)if((string)Read(actions[i],"Command")==command&&(command!="put_container"||Read(actions[i],"Container")==_bench.Sack))action=i;
            _bench.Check("native_item_action_"+command,Read(popup,"Item")==item&&action>=0);yield return MoveCursor(popup,"CursorIndex",action);yield return Tap(Key.Enter);
            _bench.Check("native_popup_outcome_"+command+"_"+success,ui.IsOpen&&(success?Read(ui,"_itemActionPopup")==null:ReferenceEquals(popup,Read(ui,"_itemActionPopup"))&&Read(popup,"Item")==item&&(int)Read(popup,"CursorIndex")==action));
        }
        private IEnumerator SelectRecipe(InventoryUI ui,string id)
        {
            var rows=Read(ui,"_tinkerRows") as IList;int row=-1;for(int i=0;rows!=null&&i<rows.Count;i++)if(((TinkerRecipe)Read(rows[i],"Recipe")).ID==id)row=i;
            _bench.Check("native_recipe_row_"+id,(int)Read(ui,"_panel")==2&&row>=0);yield return MoveCursor(ui,"_tinkerCursorIndex",row);
        }
        private IEnumerator MenuAction(InputHandler input,string command)
        {
            var menu=input.WorldActionMenuUI;var actions=Read(menu,"_actions") as List<InventoryAction>;int row=actions?.FindIndex(a=>a.Command==command)??-1;
            _bench.Check("native_menu_"+command.Split(':')[0],menu.IsOpen&&row>=0);yield return MoveCursor(menu,"_cursorIndex",row);yield return Tap(Key.Enter);
        }
        private static bool NoStatus(InventoryUI ui)=>string.IsNullOrEmpty((string)Read(ui,"_actionStatus"));
        private void StatusTiles(InventoryUI ui,string expected)
        {
            bool drawn=ui.Tilemap!=null&&!string.IsNullOrEmpty(expected)&&(string)Read(ui,"_actionStatus")==expected;
            if(drawn)for(int i=0;i<expected.Length&&i+1<80;i++)
            {var pos=new Vector3Int(i+1,0,0);var tile=ui.Tilemap.GetTile(pos);drawn&=expected[i]==' '?tile==null:tile==CP437TilesetGenerator.GetUiTile(Cp437.Map(expected[i]))&&ui.Tilemap.GetColor(pos)==QudColorParser.BrightRed;}
            _bench.Check("native_existing_render_shows_refusal",drawn&&ui.Tilemap.GetTile(new Vector3Int(1,1,0))!=null);
        }
        private IEnumerator Pick(InputHandler input, Entity item)
        {
            var ui=input.InventoryUI; int row=CraftRow(ui,item); _bench.Check("native_actual_craft_row_"+item.BlueprintName,row>=0);
            yield return MoveCursor(ui,"_craftCursorIndex",row); yield return Tap(Key.Space);
            _bench.Check("native_mark_"+item.BlueprintName,CraftingMarkPart.IsMarked(item));
        }
        private static int CraftRow(InventoryUI ui, Entity item)
        { var rows=Read(ui,"_craftRows") as IList; for(int i=0;rows!=null&&i<rows.Count;i++)if(Read(rows[i],"Item")==item)return i;return -1; }
        private IEnumerator Measure(InputHandler input)
        {
            _recorders=new ProfilerRecorder[_metricNames.Length];_samples=new long[_metricNames.Length][];
            for(int i=0;i<_metricNames.Length;i++)
            { _samples[i]=new long[SampleCapacity];_recorders[i]=ProfilerRecorder.StartNew(i==_metricNames.Length-1?ProfilerCategory.Memory:ProfilerCategory.Scripts,_metricNames[i],2,ProfilerRecorderOptions.Default|ProfilerRecorderOptions.SumAllSamplesInFrame); }
            _frames=0;_sampleTimes=new double[SampleCapacity];_measuring=true;double start=Time.realtimeSinceStartupAsDouble,next=start;_measurementStarted=start;int pulse=0;
            while(Time.realtimeSinceStartupAsDouble-start<75)
            {
                double elapsed=Time.realtimeSinceStartupAsDouble-start;
                if(elapsed>=25&&Time.realtimeSinceStartupAsDouble>=next)
                {
                    next=Time.realtimeSinceStartupAsDouble+.36;int cursor=(int)Read(input.InventoryUI,"_craftCursorIndex");
                    var row=((IList)Read(input.InventoryUI,"_craftRows"))[cursor];var item=Read(row,"Item") as Entity;bool marked=CraftingMarkPart.IsMarked(item);
                    bool navigation=elapsed<50;if(navigation)_navAttempts++;else _toggleAttempts++;
                    yield return Tap(navigation?(pulse++%2==0?Key.DownArrow:Key.UpArrow):Key.Space);
                    if(navigation&&cursor!=(int)Read(input.InventoryUI,"_craftCursorIndex"))_navChanges++;
                    if(!navigation&&marked!=CraftingMarkPart.IsMarked(item))_toggleChanges++;
                }
                else yield return null;
            }
            _measuring=false;_measuredSeconds=Time.realtimeSinceStartupAsDouble-start;
            string prefix="FLOW2-after";var metrics=new Metric[_metricNames.Length];var csv=new StringBuilder("frame,seconds,phase,"+string.Join(",",_metricNames)+"\n");
            for(int f=0;f<_frames;f++){csv.Append(f).Append(',').Append(_sampleTimes[f].ToString("F6",System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(_sampleTimes[f]<25?"idle":_sampleTimes[f]<50?"navigate":"toggle");for(int i=0;i<_metricNames.Length;i++)csv.Append(',').Append(_samples[i][f]);csv.Append('\n');}
            for(int i=0;i<_metricNames.Length;i++)
            { var values=_samples[i].Take(_frames).OrderBy(v=>v).ToArray();metrics[i]=new Metric{name=_metricNames[i],unit=i==_metricNames.Length-1?"bytes":"nanoseconds",max=values[^1],p99=values[Math.Min(values.Length-1,(int)(values.Length*.99))],mean=values.Average(v=>(double)v),valid=_recorders[i].Valid};_recorders[i].Dispose(); }
            _recorders=null;string directory=Path.Combine(Application.dataPath,"../Docs/Verification/GameSystemAudit");
            File.WriteAllText(Path.Combine(directory,prefix+"-frames.csv"),csv.ToString());
            File.WriteAllText(Path.Combine(directory,prefix+"-perf.json"),JsonUtility.ToJson(new PerfReport{runId=_bench.RunId,seconds=_measuredSeconds,frames=_frames,metrics=metrics,navAttempts=_navAttempts,navChanges=_navChanges,toggleAttempts=_toggleAttempts,toggleChanges=_toggleChanges,panel=(int)Read(input.InventoryUI,"_panel"),open=input.InventoryUI.IsOpen},true));
            _bench.Check("native_75second_crafting_workload",_measuredSeconds>=75&&_frames>100&&_frames<SampleCapacity&&metrics.All(m=>m.valid)
                && input.InventoryUI.IsOpen&&(int)Read(input.InventoryUI,"_panel")==4&&_navChanges>=50&&_navChanges==_navAttempts&&_toggleChanges>=50&&_toggleChanges==_toggleAttempts);

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
            File.WriteAllText(Path.Combine(directory, "FLOW2-native.json"), JsonUtility.ToJson(report, true));
            Debug.Log("[GameAuditActionFeedbackBench] " + JsonUtility.ToJson(report)); Finished = true;
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
        [Serializable] private sealed class PerfReport { public string runId;public double seconds;public int frames,navAttempts,navChanges,toggleAttempts,toggleChanges,panel;public bool open;public Metric[] metrics; }
        [Serializable] private sealed class Report
        { public string runId; public double seconds; public int cases, failures; public string[] audit; }
    }
}
