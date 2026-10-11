using System;
using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class ReferenceGladeNativePlayer
    {
        bool _hudPolishOnly;
        Action _hudPolishCleanup;
        public void ConfigureHudPolish() => _hudPolishOnly = true;
        const string HudPolishCanVerify = "Ordinary isolated seed64 boot, original six powers, enlarged icons and all ten framed HUD cells. Synthetic keyboard/gamepad input cycles, targets/cancels, makes one real Ground Surge across visible empty ground, opens ability details and full history, pages older than the recent30 window, returns without paid actions, and uses real F5/F6. Observes actual tile identities, source cooldown/name/range, grouped stored ticks, bounded idle work and camera render targets. Native screenshots are saved at the configured Game view size.";
        const string HudPolishCannotVerify = "No physical Steam Deck, subjective readability or input comfort claim. Root must inspect original pixels at1280x800,1280x720 and1920x1080 in separate fresh runs. This finite script-selected route performs34 real empty-slot key attempts plus one after the paid cast to create inspectable history; no MessageLog injection, grants, teleport, enemy suppression, cooldown mutation or direct ability execution. One legal empty-ground electrical cast changes actual ground and spends one turn with ordinary NPCs active. No exhaustive combat balance, long-session performance or GPU gain claim. This mode does not change persisted graphics/audio preferences.";
        static readonly string[] HudPolishChecks = {
            "hud_original_six_icons_and_ten_cards", "hud_controller_cycle_free",
            "hud_pending_and_cancel_free", "hud_empty_attempts_extend_real_history_free",
            "hud_real_cast_spends_once_and_starts_cooldown", "hud_real_log_has_tick_groups",
            "hud_full_ability_details_remain_accessible", "hud_character_history_opens_free",
            "hud_history_reaches_before_recent_window", "hud_history_idle_has_no_reformat_or_redraw",
            "hud_history_back_returns_free", "hud_keyboard_history_ends_and_return",
            "hud_focus_look_returns_free", "hud_checkpoint_preserves_raw_prefix_and_selection",
            "hud_idle_frames_have_zero_hotbar_repaint", "hud_overlay_cameras_remain_full_resolution",
            "hud_synthetic_gamepad_removed", "hud_previous_gamepad_restored"
        };
        bool HudPolishComplete => _audit.Count == HudPolishChecks.Length + 1
            && HudPolishChecks.All(name => _audit.Count(row => row == "PASS " + name) == 1)
            && _audit.Count(row => row == "PASS ordinary_player_and_native_zone") == 1 && _screenshots.Count >= 7;

        IEnumerator RunHudPolishAudit()
        {
            var previousPad = Gamepad.current; var pad = InputSystem.AddDevice<Gamepad>();
            _hudPolishCleanup = () => {
                if (pad.added) { InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.RemoveDevice(pad); }
                if (previousPad != null && previousPad.added) previousPad.MakeCurrent();
                Check("hud_synthetic_gamepad_removed", !pad.added);
                Check("hud_previous_gamepad_restored", previousPad == null || !previousPad.added || ReferenceEquals(previousPad, Gamepad.current));
            };
            try
            {
                yield return ControllerHold(pad, new GamepadState(), .05f); yield return new WaitForEndOfFrame();
                Require(State() == "Normal" && Cell().X == 40 && Cell().Y == 12, "ordinary HUD start without travel/setup");
                var actor = _input.PlayerEntity; var abilities = actor.GetPart<ActivatedAbilitiesPart>();
                Require(abilities != null, "original activated abilities");
                var expected = new[] { "Pyromancy_EmberSpit", "Pyromancy_FlamingHands", "Hydromancy_JetBlast", "Galvanism_GroundSurge", "Cryomancy_RimeGrip", "Spellcraft_Calm" };
                var occupied = Enumerable.Range(0, 10).Where(i => abilities.GetAbilityBySlot(i) != null).ToArray();
                Require(occupied.Length == 6 && expected.All(id => occupied.Any(i => abilities.GetAbilityBySlot(i).SourcePowerClass == id)), "exact six original starter powers");
                var hotbar = _input.ZoneRenderer.HotbarTilemap;
                var snapshot = HotbarStateBuilder.Build(actor, _input.CaptureHotbarSelection(), null);
                bool actual = snapshot.Slots.Count == 10;
                for (int i = 0; i < 10; i++)
                {
                    actual &= _input.ZoneRenderer.HotbarBgTilemap.GetTile(new Vector3Int(i * 8, 0, 0)) == HudCardFrame.GetTile();
                    actual &= hotbar.GetTile(new Vector3Int(i * 8 + 1, 4, 0)) == HudPixelFont.GetTile(HotbarStateBuilder.SlotToHotkey(i));
                    if (snapshot.Slots[i].Occupied) actual &= hotbar.GetTile(new Vector3Int(i * 8 + 3, 2, 0)) == HudSkillIconAtlas.GetTile(snapshot.Slots[i].Icon);
                }
                Check("hud_original_six_icons_and_ten_cards", actual);
                yield return Capture("hud-01-original-ten-cards");
                var before = ControllerBefore(); int selected = _input.CaptureHotbarSelection(); var seen = new System.Collections.Generic.HashSet<int>();
                for (int i = 0; i < occupied.Length; i++)
                { seen.Add(_input.CaptureHotbarSelection()); yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.DpadRight)); }
                Check("hud_controller_cycle_free", seen.SetEquals(occupied) && _input.CaptureHotbarSelection() == selected && ControllerCost(before, 0) && ControllerStayed(before));
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.West));
                Require(State() == "AwaitingDirection", "real X enters targeting");
                var pending = (ActivatedAbility)Field(_input, "_pendingAbility");
                var pendingSnap = HotbarStateBuilder.Build(actor, selected, pending);
                yield return Capture("hud-02-actual-pending");
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.East));
                Check("hud_pending_and_cancel_free", pendingSnap.PendingSlot == selected && pendingSnap.Slots[selected].Pending
                    && State() == "Normal" && ControllerCost(before, 0) && pending.CooldownRemaining == 0);
                int oldCount = MessageLog.Count;
                Require(abilities.GetAbilityBySlot(9) == null, "original tenth slot remains empty");
                for (int i = 0; i < 34; i++) yield return Tap(Key.Digit0);
                Check("hud_empty_attempts_extend_real_history_free", MessageLog.Count == oldCount + 34 && ControllerCost(before, 0)
                    && MessageLog.GetRecentEntries(34).All(e => e.Text.Contains("No rite bound")));

                int surgeSlot = occupied.Single(i => abilities.GetAbilityBySlot(i).SourcePowerClass == "Galvanism_GroundSurge");
                for (int i = 0; i < 10 && _input.CaptureHotbarSelection() != surgeSlot; i++)
                    yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.DpadRight));
                Require(_input.CaptureHotbarSelection() == surgeSlot, "select original Ground Surge by D-pad");
                Point direction = default; bool found = false;
                int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 }, dy = { -1, -1, 0, 1, 1, 1, 0, -1 };
                for (int i = 0; i < 8 && !found; i++)
                {
                    var cells = SkillLine.CollectCells(_input.CurrentZone, actor, Cell().X, Cell().Y, dx[i], dy[i], Galvanism_GroundSurge.SURGE_RANGE);
                    var targets = SkillLine.Collect(_input.CurrentZone, actor, Cell().X, Cell().Y, dx[i], dy[i], Galvanism_GroundSurge.SURGE_RANGE, out bool blocked);
                    if (cells.Count > 0 && cells.All(p => _input.CurrentZone.GetCell(p.X, p.Y).IsVisible) && targets.Count == 0)
                    { direction = new Point(dx[i], dy[i]); found = true; }
                }
                Require(found, "real visible empty ground ray for one paid Ground Surge; do not invent a target or bypass refusal");
                before = ControllerBefore(); var surge = abilities.GetAbilityBySlot(surgeSlot);
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.West));
                yield return ControllerHold(pad, new GamepadState { leftStick = new Vector2(direction.X, -direction.Y) }, .03f);
                Require(ControllerCost(before, 0) && surge.CooldownRemaining == 0, "left-stick selection does not cast");
                yield return ControllerPulse(pad, new GamepadState { leftStick = new Vector2(direction.X, -direction.Y), rightTrigger = 1 });
                yield return CombatWaitForFx(); yield return new WaitForEndOfFrame();
                Check("hud_real_cast_spends_once_and_starts_cooldown", State() == "Normal" && ControllerStayed(before)
                    && ControllerCost(before, 1) && surge.CooldownRemaining > 0
                    && HotbarStateBuilder.Build(actor, surgeSlot, null).Slots[surgeSlot].CooldownRemaining == surge.CooldownRemaining);
                yield return Tap(Key.Digit0); // A real free message on the new scheduler tick.
                yield return Capture("hud-03-real-cooldown-and-grouped-log");
                var recent = SidebarStateBuilder.Build(actor, _input.CurrentZone, null);
                Check("hud_real_log_has_tick_groups", SidebarTextFormatter.FormatLog(recent.LogEntriesNewestFirst, 31).Any(l => l.IsGroupSeparator));
                before = ControllerBefore();
                yield return ControllerPulse(pad, new GamepadState { leftTrigger = 1 }.WithButton(GamepadButton.West));
                Require(State() == "AbilityManagerOpen", "LT+X original ability manager"); yield return Tap(Key.D);
                Check("hud_full_ability_details_remain_accessible", State() == "AnnouncementOpen"
                    && ((string)Field(_input.AnnouncementUI, "_message")).Contains("Range:") && ControllerCost(before, 0));
                yield return Capture("hud-04-original-full-details-reader"); yield return Tap(Key.Escape); yield return Tap(Key.Escape);
                Require(State() == "Normal", "details and ability manager return normally");

                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.Start));
                Require(State() == "ControllerMenu", "native Character menu");
                var historyRows = new System.Text.StringBuilder();
                for (int i = 0; i < 12 && _input.WorldActionMenuUI.HighlightedAction?.Display != "World log"; i++)
                {
                    historyRows.Append(_input.WorldActionMenuUI.HighlightedAction?.Display).Append(" -> ");
                    yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.DpadDown));
                }
                historyRows.Append(_input.WorldActionMenuUI.HighlightedAction?.Display);
                var hoveredRow = typeof(WorldActionMenuUI).GetMethod("GetRowAtMouse", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(_input.WorldActionMenuUI, null);
                _descriptions.Add(new Description { subject = "Native Character menu navigation", source = "real pointer and D-pad observations",
                    text = "pointer=" + Input.mousePosition + "; hoveredRow=" + hoveredRow + "; highlights=" + historyRows });
                Require(_input.WorldActionMenuUI.HighlightedAction?.Display == "World log", "appended history row reached with native D-pad");
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.South));
                Check("hud_character_history_opens_free", State() == "LogHistoryOpen" && _input.LogHistoryUI.IsOpen && ControllerCost(before, 0));
                for (int i = 0; i < 12 && _input.LogHistoryUI.Model.HasOlder; i++)
                    yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.LeftShoulder));
                Check("hud_history_reaches_before_recent_window", _input.LogHistoryUI.Model.FirstEntryIndex < MessageLog.Count - 30
                    && _input.LogHistoryUI.Model.VisibleLines.Count > 0 && _input.LogHistoryUI.Model.LastReadCount <= WorldLogHistoryModel.WindowEntries);
                yield return Capture("hud-05-native-older-history");
                int builds = _input.LogHistoryUI.Model.BuildCount, paints = _input.LogHistoryUI.RenderCount;
                yield return new WaitForSecondsRealtime(.3f);
                Check("hud_history_idle_has_no_reformat_or_redraw", builds == _input.LogHistoryUI.Model.BuildCount && paints == _input.LogHistoryUI.RenderCount);
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.East));
                Check("hud_history_back_returns_free", State() == "Normal" && !_input.LogHistoryUI.IsOpen && ControllerCost(before, 0));
                yield return Tap(Key.F2); Require(State() == "LogHistoryOpen", "keyboard full-history entry");
                yield return Tap(Key.Home); bool oldest = _input.LogHistoryUI.Model.FirstEntryIndex == 0;
                yield return Tap(Key.End); bool newest = _input.LogHistoryUI.Model.EndEntryIndex == MessageLog.Count;
                yield return Tap(Key.Escape);
                Check("hud_keyboard_history_ends_and_return", oldest && newest && State() == "Normal" && ControllerCost(before, 0));
                yield return Tap(Key.L); yield return Tap(Key.RightArrow); yield return Capture("hud-06-actual-look-focus"); yield return Tap(Key.Escape);
                Check("hud_focus_look_returns_free", State() == "Normal" && ControllerCost(before, 0));

                var raw = MessageLog.GetAllEntries(); int savedSelection = _input.CaptureHotbarSelection(); var oldActor = actor;
                yield return Tap(Key.F5); yield return Tap(Key.F6);
                double deadline = Time.realtimeSinceStartupAsDouble + 10;
                while (ReferenceEquals(oldActor, _input.PlayerEntity) && Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                yield return new WaitForEndOfFrame();
                var restored = MessageLog.GetAllEntries();
                Check("hud_checkpoint_preserves_raw_prefix_and_selection", !ReferenceEquals(oldActor, _input.PlayerEntity)
                    && _input.CaptureHotbarSelection() == savedSelection && restored.Count >= raw.Count
                    && raw.Select((e, i) => e.Text == restored[i].Text && e.Tick == restored[i].Tick && e.Serial == restored[i].Serial).All(v => v));
                yield return Capture("hud-07-load-restores-hud");
                yield return new WaitForSecondsRealtime(.5f); yield return new WaitForEndOfFrame();
                int frame = -1, samples = 0; bool quiet = true;
                for (int i = 0; i < 12; i++)
                {
                    yield return null; yield return new WaitForEndOfFrame(); var measured = PerformanceDiagnostics.LastCompletedFrameSnapshot;
                    quiet &= measured.UnityFrame != frame && measured.HotbarRenderCount == 0;
                    frame = measured.UnityFrame; samples++;
                }
                Check("hud_idle_frames_have_zero_hotbar_repaint", quiet && samples == 12);
                var follow = _input.CameraFollow;
                Check("hud_overlay_cameras_remain_full_resolution", follow?.HotbarCamera != null && follow.SidebarCamera != null
                    && follow.HotbarCamera.targetTexture == null && follow.SidebarCamera.targetTexture == null
                    && follow.ReservedHotbarHeightRows == 6 && follow.HotbarCamera.enabled && follow.SidebarCamera.enabled);
                _descriptions.Add(new Description { subject = "HUD capture configuration", source = "actual Screen and native cameras",
                    text = "Screen=" + Screen.width + "x" + Screen.height + "; hotbar pixelRect=" + follow.HotbarCamera.pixelRect
                        + "; sidebar pixelRect=" + follow.SidebarCamera.pixelRect + "; no renderer/viewport settings changed. Inspect original pixels; no readability inference from counters." });
            }
            finally { _hudPolishCleanup?.Invoke(); _hudPolishCleanup = null; }
        }
    }
}
