using System;
using System.Collections;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class ReferenceGladeNativePlayer
    {
        private bool _controllerOnly, _controllerComplete;
        private Action _controllerCleanup;
        private const string ControllerCanVerify = "Isolated ordinary seed64 new game. Synthetic Gamepad states pass through the production Input System and InputHandler: eight free facing directions, paid RT movement and neutral wait, keyboard coexistence, A on the authored sign, character/inventory, held-stick menu quarantine, View/graphics, abilities and cancellation, highlight/cue captures, wait menu and finite walk cancellation. Scheduler tick/energy observations distinguish free input from exactly one paid action. Exact graphics preferences and prior current gamepad are restored.";
        private const string ControllerCannotVerify = "No physical Steam Deck, Steam Input remapping, ergonomics, performance, or complete Qud feature parity claim. This is a finite script-selected route with the ordinary scheduler active; no teleports, combat grants, target changes or forced outcomes. The walk approach uses real keyboard movement around the authored east wall to break sight with visible danger. No ability is cast and no creature is attacked. Rest remains CoO's authored-service recovery, not passive wait-healing. Screenshot readability and cue/highlight appearance require visual review.";
        public void ConfigureControllerAudit() => _controllerOnly = true;

        private readonly struct ControllerCheckpoint
        {
            public readonly int X, Y, Tick, Energy, Speed;
            public readonly Zone Zone;
            public readonly Entity Actor;
            public ControllerCheckpoint(InputHandler input)
            {
                Zone = input.CurrentZone; Actor = input.PlayerEntity;
                var cell = Zone.GetEntityCell(Actor); X = cell.X; Y = cell.Y;
                Tick = input.TurnManager.TickCount; Energy = input.TurnManager.GetEnergy(Actor);
                Speed = input.TurnManager.GetSpeed(Actor);
            }
        }
        private ControllerCheckpoint ControllerBefore() => new ControllerCheckpoint(_input);
        private bool ControllerCost(ControllerCheckpoint before, int actions)
        {
            if (_input.CurrentZone != before.Zone || _input.PlayerEntity != before.Actor
                || _input.TurnManager.GetSpeed(before.Actor) != before.Speed) return false;
            long spent = (long)(_input.TurnManager.TickCount - before.Tick) * before.Speed
                + before.Energy - _input.TurnManager.GetEnergy(before.Actor);
            return spent == (long)actions * TurnManager.ActionThreshold;
        }
        private bool ControllerStayed(ControllerCheckpoint before) => _input.CurrentZone == before.Zone
            && Cell().X == before.X && Cell().Y == before.Y;
        private string ControllerTravelState => Field(_input, "_controllerTravel").ToString();

        private IEnumerator RunControllerAudit()
        {
            // The launcher owns save, scene, background input and keyboard isolation.
            // Presets are persistent, so this mode additionally owns exact prefs.
            string[] keys = { Village3DSettings.PreferenceKey, Village3DSettings.LowDetailPreferenceKey,
                Village3DSettings.WorldResolutionPreferenceKey, Village3DSettings.ShadowsPreferenceKey,
                SpellFxSettings.ModePreferenceKey, "CavesOfOoo.Fx.Speed", "CavesOfOoo.Fx.Flash",
                "CavesOfOoo.Fx.SoundVolume", "CavesOfOoo.Fx.Shake" };
            bool[] floats = { false, false, true, false, false, true, true, true, true };
            var had = new bool[keys.Length]; var numbers = new float[keys.Length]; var integers = new int[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                had[i] = PlayerPrefs.HasKey(keys[i]);
                if (floats[i]) numbers[i] = PlayerPrefs.GetFloat(keys[i]); else integers[i] = PlayerPrefs.GetInt(keys[i]);
            }
            var previousPad = Gamepad.current;
            var pad = InputSystem.AddDevice<Gamepad>();
            _controllerCleanup = () =>
            {
                if (pad.added) { InputSystem.QueueStateEvent(pad, new GamepadState()); InputSystem.RemoveDevice(pad); }
                if (previousPad != null && previousPad.added) previousPad.MakeCurrent();
                for (int i = 0; i < keys.Length; i++)
                    if (!had[i]) PlayerPrefs.DeleteKey(keys[i]);
                    else if (floats[i]) PlayerPrefs.SetFloat(keys[i], numbers[i]);
                    else PlayerPrefs.SetInt(keys[i], integers[i]);
                PlayerPrefs.Save(); Village3DSettings.Load(); SpellFxSettings.Load();
                bool restored = true;
                for (int i = 0; i < keys.Length; i++)
                    restored &= PlayerPrefs.HasKey(keys[i]) == had[i] && (!had[i]
                        || (floats[i] ? PlayerPrefs.GetFloat(keys[i]).Equals(numbers[i]) : PlayerPrefs.GetInt(keys[i]) == integers[i]));
                Check("controller_graphics_preferences_restored", restored);
                Check("controller_synthetic_gamepad_removed", !pad.added);
                Check("controller_previous_gamepad_restored", previousPad == null || !previousPad.added
                    || ReferenceEquals(Gamepad.current, previousPad));
            };
            try
            {
                yield return ControllerHold(pad, new GamepadState(), .03f);
                Require(Cell().X == ReferenceGladePlan.StartX && Cell().Y == ReferenceGladePlan.StartY, "ordinary controller-audit arrival");
                var before = ControllerBefore();
                int[] dxs = { 0, 1, 1, 1, 0, -1, -1, -1 };
                int[] dys = { -1, -1, 0, 1, 1, 1, 0, -1 };
                for (int i = 0; i < 8; i++)
                {
                    yield return ControllerHold(pad, new GamepadState { leftStick = new Vector2(dxs[i], -dys[i]) }, .03f);
                    var cue = (WorldCursorState)Field(_input, "_controllerDirectionCue");
                    Check("left_stick_facing_" + i + "_is_free_with_visible_cue", ControllerStayed(before) && ControllerCost(before, 0)
                        && State() == "Normal" && NativeGamepadInput.TrySelectedDirection(out int dx, out int dy)
                        && dx == dxs[i] && dy == dys[i] && cue.Active && cue.Zone == _input.CurrentZone
                        && cue.X == before.X + dx && cue.Y == before.Y + dy && (bool)Field(_input, "_controllerOwnsCue"));
                    if (i == 1) yield return Capture("02-left-stick-direction-cue");
                }
                yield return ControllerHold(pad, new GamepadState(), .03f);
                Check("neutral_stick_clears_direction_cue", !((WorldCursorState)Field(_input, "_controllerDirectionCue")).Active);
                before = ControllerBefore();
                yield return ControllerPulse(pad, new GamepadState { leftStick = Vector2.right, rightTrigger = 1 });
                Check("right_trigger_moves_exactly_one_east_cell_and_pays_once", Cell().X == before.X + 1
                    && Cell().Y == before.Y && ControllerCost(before, 1));
                Require(Cell().X == 41 && Cell().Y == 12, "one native RT step to sign approach");
                before = ControllerBefore();
                yield return ControllerPulse(pad, new GamepadState { rightTrigger = 1 });
                Check("neutral_right_trigger_waits_exactly_one_turn", ControllerStayed(before) && ControllerCost(before, 1));
                before = ControllerBefore(); yield return Tap(Key.D);
                Check("keyboard_still_moves_exactly_one_cell_and_pays_once", Cell().X == before.X + 1
                    && Cell().Y == before.Y && ControllerCost(before, 1));
                Require(Cell().X == 42 && Cell().Y == 12, "native keyboard approach to authored sign");

                Entity sign = null;
                foreach (var owner in _input.CurrentZone.GetCell(43, 11).Occupants)
                    if (owner.BlueprintName == "Signpost") { sign = owner; break; }
                Require(sign != null && sign.SpatialZone == _input.CurrentZone, "original authored sign owner");
                before = ControllerBefore();
                yield return ControllerPulse(pad, new GamepadState { leftStick = new Vector2(1, 1) }.WithButton(GamepadButton.South));
                Check("a_uses_indicated_authored_sign_without_spending_turn", State() == "WorldActionMenuOpen"
                    && _input.WorldActionMenuUI.IsOpen && _input.WorldActionMenuUI.SelectedTarget == sign
                    && ControllerStayed(before) && ControllerCost(before, 0));
                yield return Capture("03-contextual-sign-actions");
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.East));
                Require(State() == "Normal", "B closes authored action menu");

                before = ControllerBefore();
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.Start));
                Check("menu_opens_character_actions", State() == "ControllerMenu" && _input.WorldActionMenuUI.IsOpen);
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.South));
                Check("character_a_opens_inventory", State() == "InventoryOpen" && _input.InventoryUI.IsOpen);
                yield return Capture("04-controller-inventory");
                yield return ControllerHold(pad, new GamepadState { leftStick = Vector2.right }, .03f);
                yield return ControllerPulse(pad, new GamepadState { leftStick = Vector2.right }.WithButton(GamepadButton.East),
                    new GamepadState { leftStick = Vector2.right });
                yield return new WaitForSecondsRealtime(_input.MoveRepeatDelay + .05f);
                Check("inventory_b_returns_with_held_stick_quarantined", State() == "Normal" && ControllerStayed(before)
                    && ControllerCost(before, 0) && !NativeGamepadInput.TrySelectedDirection(out _, out _)
                    && !((WorldCursorState)Field(_input, "_controllerDirectionCue")).Active);
                yield return ControllerHold(pad, new GamepadState(), .03f);
                yield return ControllerHold(pad, new GamepadState { leftStick = Vector2.right }, .03f);
                Check("released_stick_rearms_world_facing_without_a_turn", NativeGamepadInput.TrySelectedDirection(out int rx, out int ry)
                    && rx == 1 && ry == 0 && ControllerStayed(before) && ControllerCost(before, 0));
                yield return ControllerHold(pad, new GamepadState(), .03f);

                var pause = FindFirstObjectByType<PauseMenuUI>(); Require(pause != null, "native pause UI");
                before = ControllerBefore();
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.Select));
                Check("view_opens_system_menu", pause.Controller.IsOpen);
                for (int i = 0; i < PauseMenuController.GraphicsIndex; i++)
                    yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.DpadDown));
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.South));
                Check("system_menu_a_opens_graphics", pause.Controller.IsGraphicsOpen);
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.South));
                Check("controller_handheld_preset_applies", Village3DSettings.LowDetail
                    && Mathf.Approximately(.75f, Village3DSettings.WorldResolutionScale)
                    && !Village3DSettings.ShadowsEnabled && SpellFxSettings.Mode == SpellFxMode.Reduced);
                yield return Capture("05-controller-graphics");
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.East));
                Check("graphics_b_returns_to_system_menu", pause.Controller.IsOpen && !pause.Controller.IsGraphicsOpen);
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.East));
                Check("system_b_returns_to_world_without_turn", !pause.Controller.IsOpen && State() == "Normal"
                    && ControllerStayed(before) && ControllerCost(before, 0));

                before = ControllerBefore();
                yield return ControllerPulse(pad, new GamepadState { leftTrigger = 1 }.WithButton(GamepadButton.West));
                Check("lt_x_opens_abilities_without_casting", State() == "AbilityManagerOpen" && _input.AbilityManagerUI.IsOpen
                    && ControllerCost(before, 0));
                yield return Capture("06-controller-abilities");
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.East));
                Require(State() == "Normal", "B closes abilities");
                var abilities = _input.PlayerEntity.GetPart<ActivatedAbilitiesPart>();
                Require(abilities != null, "ordinary starting abilities");
                for (int i = 0; i < 10; i++)
                {
                    var selected = abilities.GetAbilityBySlot(_input.CaptureHotbarSelection());
                    if (selected != null && selected.IsUsable && selected.TargetingMode != AbilityTargetingMode.SelfCentered) break;
                    yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.DpadRight));
                }
                var ability = abilities.GetAbilityBySlot(_input.CaptureHotbarSelection());
                Require(ability != null && ability.IsUsable && ability.TargetingMode != AbilityTargetingMode.SelfCentered,
                    "existing ready directional hotbar ability selected through native dpad");
                int cooldown = ability.CooldownRemaining;
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.West));
                Check("x_enters_selected_ability_targeting", State() == "AwaitingDirection" && ReferenceEquals(Field(_input, "_pendingAbility"), ability));
                yield return ControllerHold(pad, new GamepadState { leftStick = Vector2.right }, _input.MoveRepeatDelay + .05f);
                Check("targeting_stick_selects_without_cast_or_payment", State() == "AwaitingDirection"
                    && ability.CooldownRemaining == cooldown && ControllerStayed(before) && ControllerCost(before, 0));
                yield return Capture("07-controller-ability-direction");
                yield return ControllerPulse(pad, new GamepadState { leftStick = Vector2.right }.WithButton(GamepadButton.East));
                Check("targeting_b_cancels_without_cast_or_payment", State() == "Normal" && Field(_input, "_pendingAbility") == null
                    && ability.CooldownRemaining == cooldown && ControllerCost(before, 0));

                before = ControllerBefore();
                yield return ControllerHold(pad, new GamepadState { leftTrigger = 1, rightTrigger = 1 }, .1f);
                yield return Capture("08-controller-highlight");
                var labels = (IList)Field(_input, "_controllerLabels");
                var hintCamera = (Camera)Field(_input, "_controllerHintCamera");
                var projected = new System.Collections.Generic.List<string>();
                foreach (var value in labels)
                {
                    var row = ((Entity owner, int x, int y, string label))value;
                    var c = _input.CurrentZone.GetCell(row.x, row.y);
                    projected.Add(row.label + " " + row.x + "," + row.y + " active:" + (row.owner.SpatialZone == _input.CurrentZone)
                        + " visible:" + c.IsVisible + " member:" + c.Occupants.Contains(row.owner)
                        + " screen:" + hintCamera.WorldToScreenPoint(new Vector3(row.x+.5f, Zone.Height-row.y-.5f, -.1f)));
                    if(projected.Count == 3) break;
                }
                _biomeSteps.Add("highlight drawn=" + Field(_input,"_controllerLabelsDrawn") + " candidates=" + labels.Count
                    + " camera=" + hintCamera.name + " position=" + hintCamera.transform.position + " rect=" + hintCamera.pixelRect
                    + " rows=" + string.Join(";", projected));
                Check("lt_rt_renders_visible_labels_without_turn", NativeGamepadInput.IsHighlightHeld
                    && (bool)Field(_input, "_controllerHighlighted") && labels.Count > 0
                    && (int)Field(_input, "_controllerLabelsDrawn") > 0 && ControllerStayed(before) && ControllerCost(before, 0));
                yield return ControllerHold(pad, new GamepadState { rightTrigger = 1 }, _input.MoveRepeatDelay + .05f);
                Check("releasing_lt_never_converts_highlight_to_wait", !NativeGamepadInput.IsHighlightHeld
                    && ControllerStayed(before) && ControllerCost(before, 0));
                yield return ControllerHold(pad, new GamepadState(), .03f);
                yield return ControllerPulse(pad, new GamepadState { leftTrigger = 1 }.WithButton(GamepadButton.East));
                Check("lt_b_opens_wait_menu_without_waiting", State() == "ControllerMenu" && ControllerCost(before, 0));
                yield return Capture("09-controller-wait-menu");
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.South));
                Check("wait_menu_a_waits_exactly_once_and_returns", State() == "Normal" && ControllerStayed(before) && ControllerCost(before, 1));

                // A straight retreat can keep equal-speed pursuers in sight.
                // Round the authored wall's south end through actual movement,
                // then use its east side as a real line-of-sight break. Every
                // checkpoint records only currently visible threats; no owner,
                // hostility, cooldown, position or visibility is rewritten.
                ControllerRecordTravelApproach("before wall approach");
                yield return WalkTo(54, 10);
                ControllerRecordTravelApproach("past wall south end");
                yield return WalkTo(54, 8);
                ControllerRecordTravelApproach("behind east wall");
                Require(!ControllerTravel.HasDanger(_input.CurrentZone, _input.PlayerEntity), "safe ordinary east-wall travel approach");
                Require(ControllerTravel.TryStepTowardEdge(_input.CurrentZone, _input.PlayerEntity, 0, -1, out _, out _), "known safe route behind east wall");
                before = ControllerBefore();
                yield return ControllerHold(pad, new GamepadState { leftStick = Vector2.up }, .03f);
                yield return ControllerPulse(pad, new GamepadState { leftStick = Vector2.up }.WithButton(GamepadButton.North));
                Check("y_starts_walk_to_selected_edge", ControllerTravelState == "Edge");
                double travelBegan = Time.realtimeSinceStartupAsDouble;
                while (ControllerStayed(before) && ControllerTravelState != "None")
                { Require(Time.realtimeSinceStartupAsDouble - travelBegan < 3, "finite first travel step"); yield return null; }
                Check("native_walk_takes_a_real_paid_step", !ControllerStayed(before) && ControllerCost(before, 1));
                // Cancel immediately, without waiting for the movement repeat gate.
                yield return ControllerPulse(pad, new GamepadState().WithButton(GamepadButton.East),
                    new GamepadState().WithButton(GamepadButton.East), false);
                var cancelled = ControllerBefore();
                Check("b_cancels_walk", ControllerTravelState == "None" && State() == "Normal");
                yield return ControllerHold(pad, new GamepadState().WithButton(GamepadButton.East), _input.MoveRepeatDelay * 2 + .1f);
                Check("held_b_after_cancel_never_repeats_travel", ControllerTravelState == "None"
                    && ControllerStayed(cancelled) && ControllerCost(cancelled, 0));
                yield return ControllerHold(pad, new GamepadState(), .03f);
                yield return Capture("10-controller-world-after-cancel");
                Check("controller_audit_keeps_ordinary_actor_alive_in_original_zone", _input.CurrentZone == _stagedZone
                    && _input.PlayerEntity.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(_input.PlayerEntity)
                    && !DevMode.Enabled && !_input.PlayerEntity.HasPart<BitLockerPart>());
                _controllerComplete = true;
            }
            finally { _controllerCleanup?.Invoke(); _controllerCleanup = null; }
        }

        private void ControllerRecordTravelApproach(string stage)
        {
            var text = new System.Text.StringBuilder(stage).Append(" at ").Append(Cell().X).Append(',').Append(Cell().Y)
                .Append("; HP=").Append(_input.PlayerEntity.GetStatValue("Hitpoints"))
                .Append("; danger=").Append(ControllerTravel.HasDanger(_input.CurrentZone, _input.PlayerEntity));
            var seen = new System.Collections.Generic.HashSet<Entity>();
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
            {
                var cell = _input.CurrentZone.GetCell(x, y);
                if (!cell.IsVisible) continue;
                foreach (var owner in cell.Occupants)
                {
                    if (owner == _input.PlayerEntity || !seen.Add(owner) || !owner.HasTag("Creature")
                        || owner.GetPart<RenderPart>()?.Visible != true || owner.GetStatValue("Hitpoints", 0) <= 0) continue;
                    var brain = owner.GetPart<BrainPart>();
                    if (brain?.Passive == true && !brain.IsPersonallyHostileTo(_input.PlayerEntity)) continue;
                    if (FactionManager.IsHostile(owner, _input.PlayerEntity))
                        text.Append("; visible threat ").Append(owner.BlueprintName).Append('@').Append(x).Append(',').Append(y);
                }
            }
            _descriptions.Add(new Description { subject = "Controller travel approach", source = "ordinary keyboard route", text = text.ToString() });
        }

        private IEnumerator ControllerHold(Gamepad pad, GamepadState state, float seconds)
        {
            Require(_clock.Elapsed.TotalSeconds < 240, "finite controller audit deadline");
            pad.MakeCurrent(); InputSystem.QueueStateEvent(pad, state); yield return null;
            if (seconds > 0) yield return new WaitForSecondsRealtime(seconds);
        }
        private IEnumerator ControllerPulse(Gamepad pad, GamepadState state,
            GamepadState released = default, bool waitForGate = true)
        {
            Require(_clock.Elapsed.TotalSeconds < 240, "finite controller audit deadline");
            if (waitForGate)
            {
                double began = Time.realtimeSinceStartupAsDouble;
                while (Time.time - (float)Field(_input, "_lastMoveTime") < _input.MoveRepeatDelay)
                { Require(Time.realtimeSinceStartupAsDouble - began < 3, "controller input rate gate reopens"); yield return null; }
            }
            pad.MakeCurrent(); InputSystem.QueueStateEvent(pad, state); yield return null;
            InputSystem.QueueStateEvent(pad, released); yield return null;
        }
    }
}
