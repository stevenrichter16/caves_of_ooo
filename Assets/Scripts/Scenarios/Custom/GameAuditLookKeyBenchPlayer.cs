using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Temporary native keyboard driver. Reflection observes UI state; it never selects actions.</summary>
    public sealed class GameAuditLookKeyBenchPlayer : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures { get; private set; }
        private ScenarioContext _ctx;
        private GameAuditLookKeyBench _bench;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground;
        private double _started;

        public void Initialize(ScenarioContext ctx, GameAuditLookKeyBench bench)
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
            var input = FindFirstObjectByType<InputHandler>(); if (input == null) throw new InvalidOperationException("Native input handler missing.");
            var boot = Read(input, "_bootMenuController") as BootMenuController; if (boot == null) throw new InvalidOperationException("Native boot controller missing.");
            if (boot.IsActive) yield return Tap(Key.N); yield return new WaitForSecondsRealtime(.3f);
            var actor = _ctx.PlayerEntity; var cursor = (WorldCursorState)Read(input, "_worldCursorState");
            var position = _ctx.Zone.GetEntityPosition(actor); int ticks = _ctx.Turns.TickCount, energy = _ctx.Turns.GetEnergy(actor);
            int chestHP = _bench.Chest.GetPart<DestructiblePart>().HP;
            _bench.Check("native_actual_preconditions", !boot.IsActive && State(input) == "Normal" && position == (20, 12)
                && _ctx.Zone.GetEntityPosition(_bench.Chest) == (21, 12) && input.MoveRepeatDelay > 0 && input.MoveRepeatDelay < .3f);
            Queue(Key.L); yield return new WaitForSecondsRealtime(.2f);
            _bench.Check("native_fresh_L_opens_look_without_time", State(input) == "LookMode" && cursor.Active && cursor.X == 20 && cursor.Y == 12
                && _ctx.Turns.TickCount == ticks && _ctx.Turns.GetEnergy(actor) == energy && _keyboard.lKey.isPressed && !_keyboard.lKey.wasPressedThisFrame);
            // Keep L down across Escape. Tap would release it and make this proof vacuous.
            Queue(Key.L, Key.Escape); yield return new WaitForSecondsRealtime(.06f);
            _bench.Check("native_escape_closes_look_while_L_stays_held", State(input) == "Normal" && !cursor.Active && _keyboard.lKey.isPressed);
            Queue(Key.L); yield return new WaitForSecondsRealtime(.5f);
            _bench.Check("native_held_L_after_exit_does_not_walk_attack_or_spend_time", State(input) == "Normal" && _keyboard.lKey.isPressed
                && !_keyboard.lKey.wasPressedThisFrame && _ctx.Zone.GetEntityPosition(actor) == position && _ctx.Turns.TickCount == ticks
                && _ctx.Turns.GetEnergy(actor) == energy && _bench.Chest.GetPart<DestructiblePart>().HP == chestHP);
            Queue(); yield return new WaitForSecondsRealtime(.15f);
            yield return Tap(Key.L); _bench.Check("native_released_repressed_L_reopens_look", State(input) == "LookMode" && cursor.Active && cursor.X == 20);
            Queue(Key.L); yield return new WaitForSecondsRealtime(.08f);
            _bench.Check("native_modal_fresh_L_moves_cursor_east", State(input) == "LookMode" && cursor.X == 21 && cursor.Y == 12);
            yield return new WaitForSecondsRealtime(.35f);
            _bench.Check("native_modal_held_L_is_edge_only_without_actor_time", _keyboard.lKey.isPressed && !_keyboard.lKey.wasPressedThisFrame
                && cursor.X == 21 && cursor.Y == 12 && _ctx.Zone.GetEntityPosition(actor) == position
                && _ctx.Turns.TickCount == ticks && _ctx.Turns.GetEnergy(actor) == energy);
            Queue(); yield return new WaitForSecondsRealtime(.15f); yield return Tap(Key.Escape);
            yield return Tap(Key.C); _bench.Check("native_C_awaits_direction", State(input) == "AwaitingTalkDirection");
            yield return Tap(Key.L); _bench.Check("native_modal_L_selects_actual_east_chest", State(input) == "WorldActionMenuOpen"
                && ReferenceEquals(input.WorldActionMenuUI.SelectedTarget, _bench.Chest) && _ctx.Zone.GetEntityPosition(actor) == position);
            yield return Tap(Key.Escape); _bench.Check("native_interaction_exit_preserves_time", State(input) == "Normal" && _ctx.Turns.TickCount == ticks);
            // The adjacent Chest is solid. Move to a clear row before proving east repeats.
            yield return Tap(Key.S); var lane = _ctx.Zone.GetEntityPosition(actor);
            _bench.Check("native_clear_movement_lane", lane.Item1 == 20 && lane.Item2 == 13 && State(input) == "Normal");
            foreach (var key in new[] { Key.D, Key.RightArrow, Key.Numpad6 })
            {
                var before = _ctx.Zone.GetEntityPosition(actor); int beforeTicks = _ctx.Turns.TickCount;
                Queue(key); double deadline = Time.realtimeSinceStartupAsDouble + 1;
                while (_ctx.Zone.GetEntityPosition(actor) == before && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                var first = _ctx.Zone.GetEntityPosition(actor);
                _bench.Check("native_" + key + "_first_east_step", first.Item1 > before.Item1 && first.Item2 == before.Item2 && State(input) == "Normal");
                yield return new WaitForSecondsRealtime(.4f); var repeated = _ctx.Zone.GetEntityPosition(actor);
                _bench.Check("native_" + key + "_held_repeats", _keyboard[key].isPressed && !_keyboard[key].wasPressedThisFrame
                    && repeated.Item1 > first.Item1 && repeated.Item2 == first.Item2 && _ctx.Turns.TickCount > beforeTicks);
                Queue(); yield return new WaitForSecondsRealtime(.1f); var released = _ctx.Zone.GetEntityPosition(actor); int releaseTicks = _ctx.Turns.TickCount;
                yield return new WaitForSecondsRealtime(.3f);
                _bench.Check("native_" + key + "_release_stops", _ctx.Zone.GetEntityPosition(actor) == released && _ctx.Turns.TickCount == releaseTicks);
            }
            _bench.Check("native_final_normal_and_chest_untouched", State(input) == "Normal" && !cursor.Active && _bench.Chest.GetPart<DestructiblePart>().HP == chestHP);
        }
        private void Queue(params Key[] keys) => InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
        private IEnumerator Tap(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key)); yield return new WaitForSecondsRealtime(.06f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return new WaitForSecondsRealtime(.12f);
        }
        private static object Read(object owner, string field) => owner?.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(owner);
        private static string State(InputHandler input) => Read(input, "_inputState")?.ToString();
        private void Finish()
        {
            Failures = Math.Max(Failures, _bench.Failures);
            var report = new Report { runId = _bench.RunId, seconds = Time.realtimeSinceStartupAsDouble - _started,
                cases = _bench.Cases, failures = Failures, audit = _bench.Audit.ToArray() };
            string directory = Path.Combine(Application.dataPath, "../Docs/Verification/GameSystemAudit"); Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "FLOW6-native.json"), JsonUtility.ToJson(report, true));
            Debug.Log("[GameAuditLookKeyBench] " + JsonUtility.ToJson(report)); Finished = true;
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
