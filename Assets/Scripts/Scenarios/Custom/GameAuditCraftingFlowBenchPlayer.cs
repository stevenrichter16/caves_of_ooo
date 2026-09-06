using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.Profiling;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Temporary native keyboard driver. Reflection observes UI state; it never selects actions.</summary>
    public sealed class GameAuditCraftingFlowBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        private ScenarioContext _ctx;
        private GameAuditCraftingFlowBench _bench;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground;
        private double _started;
        private readonly Dictionary<string, bool> _channels = new Dictionary<string, bool>();

        private const int SampleCapacity = 500000;
        private readonly string[] _metricNames = { "COO.Input.Update", "COO.UI.Inventory.Render", "COO.ZoneRenderer.LateUpdate", "GC Allocated In Frame" };
        private ProfilerRecorder[] _recorders; private long[][] _samples; private int _frames; private bool _measuring; private double _measuredSeconds, _measurementStarted; private double[] _sampleTimes; private int _navAttempts,_navChanges,_toggleAttempts,_toggleChanges;
        public void Initialize(ScenarioContext ctx, GameAuditCraftingFlowBench bench)
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
            if (!bench.ObservePointer) _keyboard = InputSystem.AddDevice<Keyboard>();
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
            yield return new WaitForSecondsRealtime(0.6f);
            var input = FindFirstObjectByType<InputHandler>();
            if (input == null) throw new InvalidOperationException("Native input handler missing.");
            var boot = Read(input, "_bootMenuController") as BootMenuController;
            if (boot == null) throw new InvalidOperationException("Native boot controller missing.");
            if (_bench.ObservePointer) { yield return ObserveDesktop(input, boot); yield break; }
            if (boot.IsActive) yield return Tap(Key.N);
            yield return new WaitForSecondsRealtime(0.3f);
            _bench.Check("native_boot_keeps_arena", !boot.IsActive && _ctx.Zone.GetEntityPosition(_ctx.PlayerEntity) == (20, 12));
            var actor = _ctx.PlayerEntity; var inv = actor.GetPart<InventoryPart>();
            yield return Tap(Key.I); for (int i = 0; i < 4; i++) { yield return Tap(Key.Tab); _bench.Check("native_tab_"+(i+1),input.InventoryUI.IsOpen&&(int)Read(input.InventoryUI,"_panel")==i+1); }
            _bench.Check("native_crafting_tab", input.InventoryUI.IsOpen && (int)Read(input.InventoryUI, "_panel") == 4);
            yield return Measure(input);
            yield return Tap(Key.C);
            if (!_bench.VerifyFixes) yield break;
            yield return Pick(input, _bench.Blade); yield return Pick(input, _bench.Replacement);
            _bench.Check("native_second_blade_replaces_first", !CraftingMarkPart.IsMarked(_bench.Blade) && CraftingMarkPart.IsMarked(_bench.Replacement)
                && Read(input.InventoryUI,"_pickedBlade") == _bench.Replacement);
            if (!CraftingMarkPart.IsMarked(_bench.Haft)) yield return Pick(input, _bench.Haft);
            yield return Pick(input, _bench.Binding);
            var preview = (ForgePreview)Read(input.InventoryUI,"_forgePreview");
            _bench.Check("native_complete_preview", preview.IsComplete);
            yield return Tap(Key.Enter);
            var weapon = inv.Objects.SingleOrDefault(e => e.GetPart<WeaponAssemblyPart>() != null);
            _bench.Check("native_selected_iron_matches_output_and_payment", weapon?.GetPart<WeaponAssemblyPart>().BladeBlueprint == "IronSpikeComponent"
                && weapon.GetPart<MeleeWeaponPart>().BaseDamage == preview.BaseDamage && _bench.Blade.GetPart<StackerPart>().StackCount == 3
                && !inv.Contains(_bench.Replacement) && _bench.Haft.GetPart<StackerPart>().StackCount == 2 && _bench.Binding.GetPart<StackerPart>().StackCount == 2);
            yield return Tap(Key.B); yield return Tap(Key.C); yield return Pick(input,_bench.Glimmer); yield return Pick(input,_bench.Spark);
            _bench.Check("native_reagents_stay_multi_selected", CraftingMarkPart.IsMarked(_bench.Glimmer) && CraftingMarkPart.IsMarked(_bench.Spark));
            yield return Tap(Key.Escape); _bench.Check("native_final_normal", State(input)=="Normal");
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
            string prefix=_bench.VerifyFixes?"FLOW1-after":"FLOW1-before";var metrics=new Metric[_metricNames.Length];var csv=new StringBuilder("frame,seconds,phase,"+string.Join(",",_metricNames)+"\n");
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
        private IEnumerator ObserveDesktop(InputHandler input, BootMenuController boot)
        {
            string path=Path.Combine(Path.GetTempPath(),"coo-flow1-pointer.json");double deadline=Time.realtimeSinceStartupAsDouble+240,next=0;bool sawSteel=false,ready=false;
            // Desktop mode uses actual external keyboard/mouse input. No queued
            // device events or reflection-driven selections participate.
            while(Time.realtimeSinceStartupAsDouble<deadline&&!ready)
            {
                yield return null;
                bool crafting=input.InventoryUI.IsOpen&&(int)Read(input.InventoryUI,"_panel")==4;
                if(crafting&&CraftingMarkPart.IsMarked(_bench.Blade)&&!CraftingMarkPart.IsMarked(_bench.Replacement))sawSteel=true;
                ready=crafting&&sawSteel&&CraftingMarkPart.IsMarked(_bench.Replacement)&&!CraftingMarkPart.IsMarked(_bench.Blade);
                if(Time.realtimeSinceStartupAsDouble>=next)
                {next=Time.realtimeSinceStartupAsDouble+.2;File.WriteAllText(path,JsonUtility.ToJson(new PointerReport{runId=_bench.RunId,phase=boot.IsActive?"dismiss_boot":!crafting?"open_crafting":sawSteel?"select_iron":"select_steel",panel=(int)Read(input.InventoryUI,"_panel"),cursor=(int)Read(input.InventoryUI,"_craftCursorIndex")},true));}
            }
            _bench.Check("desktop_real_keyboard_radio_selection",ready&&Read(input.InventoryUI,"_pickedBlade")==_bench.Replacement);
            yield return ObservePointer(input);
            var inv=_ctx.PlayerEntity.GetPart<InventoryPart>();Entity weapon=null;deadline=Time.realtimeSinceStartupAsDouble+120;next=0;
            while(Time.realtimeSinceStartupAsDouble<deadline&&weapon==null)
            {
                yield return null;weapon=inv.Objects.FirstOrDefault(e=>e.GetPart<WeaponAssemblyPart>()!=null);
                if(Time.realtimeSinceStartupAsDouble>=next){next=Time.realtimeSinceStartupAsDouble+.2;File.WriteAllText(path,JsonUtility.ToJson(new PointerReport{runId=_bench.RunId,phase="mark_binding_and_forge",panel=(int)Read(input.InventoryUI,"_panel"),cursor=(int)Read(input.InventoryUI,"_craftCursorIndex")},true));}
            }
            _bench.Check("desktop_pointer_selection_forges_expected_paid_weapon",weapon?.GetPart<WeaponAssemblyPart>().BladeBlueprint=="IronSpikeComponent"
                &&!inv.Contains(_bench.Replacement)&&_bench.Blade.GetPart<StackerPart>().StackCount==3&&_bench.Haft.GetPart<StackerPart>().StackCount==2&&_bench.Binding.GetPart<StackerPart>().StackCount==2);
            File.WriteAllText(path,JsonUtility.ToJson(new PointerReport{runId=_bench.RunId,phase="complete"}));
        }
        private IEnumerator ObservePointer(InputHandler input)
        {
            string path=Path.Combine(Path.GetTempPath(),"coo-flow1-pointer.json");double start=Time.realtimeSinceStartupAsDouble,next=0;bool hovered=false,clicked=false;
            int oak=CraftRow(input.InventoryUI,_bench.Haft);
            while(Time.realtimeSinceStartupAsDouble-start<180&&!clicked)
            {
                yield return null;
                var grid=(Vector2Int)typeof(InventoryUI).GetMethod("MouseToGrid",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(input.InventoryUI,null);
                bool atOak=grid==new Vector2Int(13,9);bool mouseDown=Input.GetMouseButtonDown(0);
                if(atOak&&!Input.GetMouseButton(0)&&!hovered)
                {
                    _bench.Check("native_legacy_mouse_hover_reaches_oak",(int)Read(input.InventoryUI,"_panel")==4&&(int)Read(input.InventoryUI,"_craftCursorIndex")==oak&&!CraftingMarkPart.IsMarked(_bench.Haft));
                    hovered=true;
                }
                if(atOak&&mouseDown&&hovered)
                {
                    _bench.Check("native_legacy_mouse_click_marks_oak_once",(int)Read(input.InventoryUI,"_panel")==4&&Read(input.InventoryUI,"_equipPopup")==null&&CraftingMarkPart.IsMarked(_bench.Haft));
                    clicked=true;
                }
                if(Time.realtimeSinceStartupAsDouble>=next)
                { next=Time.realtimeSinceStartupAsDouble+.2;File.WriteAllText(path,JsonUtility.ToJson(new PointerReport{runId=_bench.RunId,phase=hovered?"click_oak":"hover_oak",gridX=grid.x,gridY=grid.y,mouseX=Input.mousePosition.x,mouseY=Input.mousePosition.y,panel=(int)Read(input.InventoryUI,"_panel"),cursor=(int)Read(input.InventoryUI,"_craftCursorIndex"),oakRow=oak},true)); }
            }
            _bench.Check("native_pointer_sequence_completed",hovered&&clicked);
            yield return new WaitForSecondsRealtime(.3f);
            _bench.Check("native_pointer_mark_stays_stable_after_click",CraftingMarkPart.IsMarked(_bench.Haft));
            File.WriteAllText(path,JsonUtility.ToJson(new PointerReport{runId=_bench.RunId,phase="oak_click_complete"}));
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
            File.WriteAllText(Path.Combine(directory, _bench.ObservePointer ? "FLOW1-pointer-native.json" : _bench.VerifyFixes ? "FLOW1-native.json" : "FLOW1-baseline-native.json"), JsonUtility.ToJson(report, true));
            Debug.Log("[GameAuditCraftingFlowBench] " + JsonUtility.ToJson(report)); Finished = true;
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
        [Serializable] private sealed class PointerReport { public string runId,phase;public int gridX,gridY,panel,cursor,oakRow;public float mouseX,mouseY; }
        [Serializable] private sealed class Report
        { public string runId; public double seconds; public int cases, failures; public string[] audit; }
    }
}
