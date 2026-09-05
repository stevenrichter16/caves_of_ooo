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
    /// <summary>Native key-driven audit. Reflection reads private UI state only;
    /// gameplay actions are driven exclusively by the ordinary keyboard route.</summary>
    public sealed class GameAuditConsumablesBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        private ScenarioContext _ctx;
        private GameAuditConsumablesBench _bench;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground;
        private double _started;

        public void Initialize(ScenarioContext ctx, GameAuditConsumablesBench bench)
        {
            _ctx = ctx; _bench = bench; _started = Time.realtimeSinceStartupAsDouble;
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
            while (true)
            {
                bool moved = false; object current = null; Exception failure = null;
                try { moved = steps.MoveNext(); if (moved) current = steps.Current; }
                catch (Exception e) { failure = e; }
                if (failure != null) { Failures++; Debug.LogError(failure); break; }
                if (!moved) break;
                yield return current;
            }
            Finish();
        }
        private IEnumerator AuditInputs()
        {
            yield return new WaitForSecondsRealtime(0.6f);
            var input = FindFirstObjectByType<InputHandler>();
            if (input == null) throw new InvalidOperationException("Native input handler missing.");
            var bootMenu = Read(input, "_bootMenuController") as BootMenuController;
            if (bootMenu == null) throw new InvalidOperationException("Native boot controller missing.");
            // The alternate fixture enters ordinary gameplay before the audit.
            // This setup key is itself native input, not a controller mutation.
            if (_bench.PreDismissBootMenu && bootMenu.IsActive) yield return Tap(Key.N);
            if (bootMenu.IsActive) yield return Tap(Key.N);
            yield return new WaitForSecondsRealtime(0.3f);
            _bench.Check("native_boot_keeps_arena", !bootMenu.IsActive
                && _ctx.Zone.GetEntityPosition(_ctx.PlayerEntity) == (20, 12));
            yield return Tap(Key.C);
            _bench.Check("native_interaction_direction", State(input) == "AwaitingTalkDirection");
            yield return Tap(Key.RightArrow);
            var menu = input.WorldActionMenuUI;
            var actions = Read(menu, "_actions") as List<InventoryAction>;
            _bench.Check("ground_menu_no_consume", State(input) == "WorldActionMenuOpen" && menu.IsOpen
                && menu.SelectedTarget == _bench.Tonic && actions != null
                && actions.Exists(a => a.Command == "Examine") && !actions.Exists(a => a.Command == "ApplyTonic"));
            _bench.Check("ground_unit_unchanged", _ctx.Zone.GetEntityPosition(_bench.Tonic) == (21, 12)
                && _bench.Tonic.GetPart<StackerPart>().StackCount == 1 && _ctx.PlayerEntity.GetStatValue("Hitpoints") == 10);
            yield return Tap(Key.Escape);
            _bench.Check("world_menu_exit", State(input) == "Normal");
            yield return Tap(Key.RightArrow);
            _bench.Check("native_move_to_item", _ctx.Zone.GetEntityPosition(_ctx.PlayerEntity) == (21, 12));
            yield return Tap(Key.G);
            _bench.Check("native_pickup", _ctx.PlayerEntity.GetPart<InventoryPart>().Objects.Contains(_bench.Tonic)
                && _ctx.Zone.GetEntityCell(_bench.Tonic) == null && _ctx.Turns.WaitingForInput);
            yield return Tap(Key.I); yield return Tap(Key.Tab); yield return Tap(Key.DownArrow); yield return Tap(Key.Enter);
            var inventory = input.InventoryUI;
            object popup = Read(inventory, "_itemActionPopup");
            var popupActions = popup == null ? null : Read(popup, "Actions") as IList;
            int index = -1;
            if (popupActions != null)
                for (int i = 0; i < popupActions.Count; i++)
                    if ((string)Read(popupActions[i], "Command") == "ApplyTonic") index = i;
            _bench.Check("carried_menu_consume", inventory.IsOpen && popup != null
                && ReferenceEquals(Read(popup, "Item"), _bench.Tonic) && index >= 0);
            int cursor = (int)Read(popup, "CursorIndex");
            for (int i = cursor; i < index; i++) yield return Tap(Key.DownArrow);
            for (int i = cursor; i > index; i--) yield return Tap(Key.UpArrow);
            int before = _ctx.PlayerEntity.GetStatValue("Hitpoints");
            yield return Tap(Key.Enter);
            int after = _ctx.PlayerEntity.GetStatValue("Hitpoints");
            _bench.Check("native_consumption", after > before && after <= before + 28
                && !_ctx.PlayerEntity.GetPart<InventoryPart>().Objects.Contains(_bench.Tonic)
                && _ctx.Zone.GetEntityCell(_bench.Tonic) == null && Read(inventory, "_itemActionPopup") == null);
            yield return Tap(Key.Escape);
            _bench.Check("inventory_exit", State(input) == "Normal" && !inventory.IsOpen);
        }
        private IEnumerator Tap(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
            yield return new WaitForSecondsRealtime(0.06f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return new WaitForSecondsRealtime(0.12f);
        }
        private static object Read(object owner, string field) => owner?.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(owner);
        private static string State(InputHandler input) => Read(input, "_inputState")?.ToString();
        private void Finish()
        {
            Failures = Math.Max(Failures, _bench.Failures);
            var report = new Report { runId = _bench.RunId, seconds = Time.realtimeSinceStartupAsDouble - _started,
                cases = _bench.Cases, failures = Failures, audit = _bench.Audit.ToArray() };
            string directory = Path.Combine(Application.dataPath, "../Docs/Verification/GameSystemAudit");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "GA02a-native.json"), JsonUtility.ToJson(report, true));
            Debug.Log("[GameAuditConsumablesBench] " + JsonUtility.ToJson(report));
            Finished = true;
        }
        private void OnDestroy()
        {
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
            if (_oldSettings != null) InputSystem.settings = _oldSettings;
            if (_settings != null) Destroy(_settings);
            Application.runInBackground = _oldBackground;
        }
        [Serializable] private sealed class Report
        { public string runId; public double seconds; public int cases, failures; public string[] audit; }
    }
}
