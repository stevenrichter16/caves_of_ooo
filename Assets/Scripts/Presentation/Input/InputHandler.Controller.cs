using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public partial class InputHandler
    {
        private enum ControllerTravelMode { None, Edge, Explore, Point, Wait }
        private ControllerTravelMode _controllerTravel;
        private Zone _controllerTravelZone;
        private Entity _controllerTravelActor;
        private int _controllerTravelHp, _controllerTravelBudget, _controllerTravelX, _controllerTravelY;
        private bool _controllerRepeatBlocked;
        private GamepadCommand _controllerDirectionCommand;
        private readonly List<Action> _controllerMenuActions = new List<Action>();
        private Entity _controllerMenuActor;
        private Zone _controllerMenuZone;
        private readonly WorldCursorState _controllerDirectionCue = new WorldCursorState();
        private bool _controllerOwnsCue;
        private static readonly GamepadCommand[] ControllerCommands = {
            GamepadCommand.Pause, GamepadCommand.Character, GamepadCommand.Help,
            GamepadCommand.Interact, GamepadCommand.InteractDirection, GamepadCommand.WaitUntilHealed,
            GamepadCommand.WaitMenu, GamepadCommand.ActivateAbility, GamepadCommand.Abilities,
            GamepadCommand.Walk, GamepadCommand.AutoExplore, GamepadCommand.PointOfInterest,
            GamepadCommand.AttackNearest, GamepadCommand.ForceAttack, GamepadCommand.Throw,
            GamepadCommand.Fire, GamepadCommand.Reload, GamepadCommand.ReplaceCell,
            GamepadCommand.Ascend, GamepadCommand.Descend, GamepadCommand.PreviousAbility,
            GamepadCommand.NextAbility, GamepadCommand.PreviousAbilityPage, GamepadCommand.NextAbilityPage,
            GamepadCommand.ZoomIn, GamepadCommand.ZoomOut
        };

        private Entity _controllerSessionActor;
        private Zone _controllerSessionZone;
        private TurnManager _controllerSessionTurns;
        private void RefreshControllerSession()
        {
            if (_controllerSessionActor == PlayerEntity && _controllerSessionZone == CurrentZone
                && _controllerSessionTurns == TurnManager) return;
            NativeGamepadInput.QuarantineHeldControls();
            CancelControllerTravel(null);
            _controllerSessionActor = PlayerEntity; _controllerSessionZone = CurrentZone;
            _controllerSessionTurns = TurnManager; _controllerRepeatBlocked = false;
        }

        private GamepadInputContext ControllerContext()
        {
            if (PlayerEntity == null || CurrentZone == null || TurnManager == null
                || SpellFxSettingsPanel.IsOpen || _buildMenuController.IsOpen || _bootMenuController.IsActive
                || _deathScreenController.IsActive || PlayerEntity.GetStatValue("Hitpoints", 1) <= 0
                || CombatSystem.IsDeathHandled(PlayerEntity) || _pauseMenuUI?.IsOpen == true)
                return GamepadInputContext.Menu;
            if (_inputState == InputState.Normal) return GamepadInputContext.World;
            if (_inputState == InputState.LookMode || _inputState == InputState.ThrowTargeting
                || _inputState == InputState.AwaitingDirection || _inputState == InputState.AwaitingTalkDirection
                || _inputState == InputState.AwaitingRitePreviewDirection || _inputState == InputState.ControllerDirection)
                return GamepadInputContext.Targeting;
            return GamepadInputContext.Menu;
        }

        private bool HandleControllerWorldInput()
        {
            foreach (var command in ControllerCommands)
                if (NativeGamepadInput.IsPressed(command))
                {
                    ExecuteControllerCommand(command);
                    _lastMoveTime = Time.time;
                    return true;
                }
            if (NativeGamepadInput.TryLookDirection(out int lx, out int ly))
            {
                if (EnterLookMode()) MoveLookCursor(lx, ly);
                return true;
            }
            if (!NativeGamepadInput.IsHeld(GamepadCommand.Step))
            { _controllerRepeatBlocked = false; return false; }
            bool first = NativeGamepadInput.IsPressed(GamepadCommand.Step);
            if (_controllerRepeatBlocked || (!first && Time.time - _lastMoveTime < MoveRepeatDelay)) return true;
            // A deliberate tap can fight or enter dangerous terrain. Held repeat
            // stops at danger; it cannot turn a convenience action into auto-combat.
            if (!first && ControllerTravel.HasDanger(CurrentZone, PlayerEntity))
            { _controllerRepeatBlocked = true; MessageLog.Add("Movement stopped by danger. Release RT before taking another step."); return true; }
            int before = PlayerEntity.GetStatValue("Hitpoints");
            if (NativeGamepadInput.TryMoveDirection(out int dx, out int dy)) ExecuteMovementStep(dx, dy);
            else EndTurnAndProcess();
            if (PlayerEntity.GetStatValue("Hitpoints") < before) _controllerRepeatBlocked = true;
            _lastMoveTime = Time.time;
            return true;
        }

        private void ExecuteControllerCommand(GamepadCommand command)
        {
            switch (command)
            {
                case GamepadCommand.Pause:
                    if (_pauseMenuUI != null)
                    {
                        _pauseMenuUI.HandleInput(ControllerPauseProbe.Instance);
                        if (_pauseMenuUI.IsOpen) EnterCenteredPopupOverlayView();
                    }
                    break;
                case GamepadCommand.Character: OpenControllerCharacter(); break;
                case GamepadCommand.Help: OpenControlsReader(); break;
                case GamepadCommand.Interact:
                    if (NativeGamepadInput.TrySelectedDirection(out int ix, out int iy)) InteractInDirection(ix, iy);
                    else ControllerContextualInteract();
                    break;
                case GamepadCommand.InteractDirection:
                    if (NativeGamepadInput.TrySelectedDirection(out int ax, out int ay)) InteractInDirection(ax, ay);
                    else OpenControllerNearby();
                    break;
                case GamepadCommand.WaitUntilHealed: ControllerRest(); break;
                case GamepadCommand.WaitMenu: OpenControllerWaitMenu(); break;
                case GamepadCommand.ActivateAbility: ActivateSelectedHotbarSlot(); break;
                case GamepadCommand.Abilities: OpenAbilityManager(); break;
                case GamepadCommand.Walk:
                case GamepadCommand.ForceAttack:
                    if (NativeGamepadInput.TrySelectedDirection(out int dx, out int dy)) ControllerDirectionalAction(command, dx, dy);
                    else
                    {
                        _controllerDirectionCommand = command; _inputState = InputState.ControllerDirection;
                        MessageLog.Add(command == GamepadCommand.Walk
                            ? "Walk to edge: choose a direction with the left stick, then A or RT. B cancels."
                            : "Force attack: choose a direction with the left stick, then A or RT. B cancels.");
                    }
                    break;
                case GamepadCommand.AutoExplore: BeginControllerTravel(ControllerTravelMode.Explore, 0, 0); break;
                case GamepadCommand.PointOfInterest: OpenControllerPoints(); break;
                case GamepadCommand.AttackNearest: ControllerAttackNearest(); break;
                case GamepadCommand.Throw: OpenControllerThrow(); break;
                case GamepadCommand.Fire:
                    MessageLog.Add("Missile weapons are not implemented yet. X uses your selected ability; LT + RB throws an item."); break;
                case GamepadCommand.Reload:
                case GamepadCommand.ReplaceCell:
                    MessageLog.Add("There is no reloadable missile weapon or energy-cell slot in this build."); break;
                case GamepadCommand.Ascend: TryUseStairs(false); break;
                case GamepadCommand.Descend: TryUseStairs(true); break;
                case GamepadCommand.PreviousAbility: CycleHotbarSelection(-1); break;
                case GamepadCommand.NextAbility: CycleHotbarSelection(1); break;
                case GamepadCommand.PreviousAbilityPage:
                case GamepadCommand.NextAbilityPage:
                    MessageLog.Add("Your hotbar has one page of ten slots. LT + X opens all abilities."); break;
                case GamepadCommand.ZoomIn: ControllerZoom(.85f); break;
                case GamepadCommand.ZoomOut: ControllerZoom(1f / .85f); break;
            }
        }
        private sealed class ControllerPauseProbe : IInputProbe
        {
            public static readonly ControllerPauseProbe Instance = new ControllerPauseProbe();
            public bool GetKeyDown(KeyCode key) => key == KeyCode.Tab;
        }
        private void ControllerZoom(float factor)
        {
            if (CameraFollow == null) return;
            CameraFollow.GameplayZoomMultiplier = Mathf.Clamp(CameraFollow.GameplayZoomMultiplier * factor, .5f, 1.5f);
            CameraFollow.SnapToPlayer();
        }
        private void HandleControllerDirection()
        {
            if (InputHelper.GetKeyDown(KeyCode.Escape)) { _inputState = InputState.Normal; return; }
            if (!GetDirectionKeyDown(out int dx, out int dy)) return;
            _inputState = InputState.Normal;
            ControllerDirectionalAction(_controllerDirectionCommand, dx, dy);
            _lastMoveTime = Time.time;
        }
        private void ControllerDirectionalAction(GamepadCommand command, int dx, int dy)
        {
            if (command == GamepadCommand.Walk) BeginControllerTravel(ControllerTravelMode.Edge, dx, dy);
            else ControllerForceAttack(dx, dy);
        }

        private void OpenControllerMenu(string title, List<(string label, Action action)> options)
        {
            if (WorldActionMenuUI == null) { MessageLog.Add(title); return; }
            _controllerMenuActions.Clear();
            var rows = new List<InventoryAction>();
            foreach (var option in options)
            {
                rows.Add(new InventoryAction("Controller", option.label, _controllerMenuActions.Count.ToString()));
                _controllerMenuActions.Add(option.action);
            }
            _controllerMenuActor = PlayerEntity; _controllerMenuZone = CurrentZone;
            _inputState = InputState.ControllerMenu;
            EnterCenteredPopupOverlayView();
            WorldActionMenuUI.Open(PlayerEntity, PlayerEntity, CurrentZone.GetEntityCell(PlayerEntity), rows, CurrentZone, title: title);
            MessageLog.Add(title + " — A select, B back.");
        }
        private void HandleControllerMenu()
        {
            if (WorldActionMenuUI == null || !WorldActionMenuUI.IsOpen
                || _controllerMenuActor != PlayerEntity || _controllerMenuZone != CurrentZone)
            { CloseControllerMenu(); return; }
            WorldActionMenuUI.HandleInput();
            if (WorldActionMenuUI.SelectionCancelled) { CloseControllerMenu(); return; }
            if (!WorldActionMenuUI.SelectionMade) return;
            var selected = WorldActionMenuUI.SelectedAction;
            Action action = selected != null && int.TryParse(selected.Command, out int index)
                && index >= 0 && index < _controllerMenuActions.Count ? _controllerMenuActions[index] : null;
            CloseControllerMenu();
            action?.Invoke();
        }
        private void CloseControllerMenu()
        {
            WorldActionMenuUI?.HideForReader(); WorldActionMenuUI?.ConsumeSelection();
            _controllerMenuActions.Clear(); _controllerMenuActor = null; _controllerMenuZone = null;
            _inputState = InputState.Normal; ExitCenteredPopupOverlayViewToGameplay();
        }
        private void OpenControllerCharacter()
        {
            OpenControllerMenu("Character", new List<(string, Action)> {
                ("Inventory and equipment", OpenInventory),
                ("Attributes and current effects", () => {
                    var text = new System.Text.StringBuilder("Attributes\n");
                    foreach (string stat in new[] { "Strength", "Agility", "Toughness", "Intelligence", "Willpower", "Ego", "Speed" })
                        text.AppendLine(stat + ": " + PlayerEntity.GetStatValue(stat));
                    text.AppendLine().Append(InventoryDecisionDetails.Status(PlayerEntity));
                    OpenDecisionReader(text.ToString());
                }),
                ("Skills", OpenSkillsScreen), ("Abilities", OpenAbilityManager),
                ("Quest log", OpenQuestLog), ("Factions", OpenFaction), ("Controls", OpenControlsReader)
            });
        }
        private void OpenControllerAbilitySlots()
        {
            var abilities = PlayerEntity.GetPart<ActivatedAbilitiesPart>();
            var id = AbilityManagerUI.SelectedAbilityID;
            if (abilities == null || id == Guid.Empty || !abilities.AbilityByGuid.ContainsKey(id)) return;
            var actor = PlayerEntity;
            var options = new List<(string, Action)>();
            for (int i = 0; i < ActivatedAbilitiesPart.SlotCount; i++)
            {
                int slot = i;
                var assigned = abilities.GetAbilityBySlot(slot);
                options.Add(($"Slot {(slot + 1) % 10}: {assigned?.DisplayName ?? "empty"}", () => {
                    if (PlayerEntity != actor) return;
                    abilities.AssignAbilityToSlot(id, slot); OpenAbilityManager();
                }));
            }
            options.Add(("Clear this ability's slot", () => {
                if (PlayerEntity != actor) return;
                int slot = abilities.GetSlotForAbility(id);
                if (slot >= 0) abilities.AssignAbilityToSlot(Guid.Empty, slot);
                OpenAbilityManager();
            }));
            AbilityManagerUI.Close();
            OpenControllerMenu("Assign ability to hotbar", options);
        }

        private void OpenControllerWaitMenu()
        {
            OpenControllerMenu("Wait", new List<(string, Action)> {
                ("Wait one turn", EndTurnAndProcess),
                ("Wait 10 turns (stops for danger)", () => BeginControllerTravel(ControllerTravelMode.Wait, 10, 0)),
                ("Wait 100 turns (stops for danger)", () => BeginControllerTravel(ControllerTravelMode.Wait, 100, 0)),
                ("Rest / recover at a bed or campfire", ControllerRest)
            });
        }
        private void ControllerRest()
        {
            var hp = PlayerEntity.GetStat("Hitpoints");
            if (hp != null && hp.Value >= hp.Max) { MessageLog.Add("You are already fully healed."); return; }
            // This game heals at authored rest services, not by ordinary waiting.
            // Open their real actions so ownership, payment and safety still apply.
            var at = CurrentZone.GetEntityCell(PlayerEntity);
            if (at == null) return;
            for (int y = at.Y - 1; y <= at.Y + 1; y++) for (int x = at.X - 1; x <= at.X + 1; x++)
            {
                var cell = CurrentZone.GetCell(x, y);
                if (cell?.IsVisible != true) continue;
                foreach (var owner in cell.Occupants)
                    if (owner.GetPart<CampfirePart>()?.AllowRest == true || owner.HasPart<BedPart>())
                    { InteractInDirection(x - at.X, y - at.Y); return; }
            }
            MessageLog.Add("Recovery needs a bed, resting campfire, or healing item here. Menu opens inventory; LT + B offers ordinary waiting.");
        }
        private void ControllerContextualInteract()
        {
            var at = CurrentZone.GetEntityCell(PlayerEntity);
            if (at == null) return;
            var cue = WorldAffordanceQuery.Find(PlayerEntity, CurrentZone, false, 0, 0);
            if (cue.HasValue) { InteractInDirection(cue.Value.Cell.X - at.X, cue.Value.Cell.Y - at.Y); return; }
            var choices = ControllerNearbyChoices();
            if (choices.Count == 1) choices[0].action();
            else if (choices.Count > 1) OpenControllerMenu("Use nearby", choices);
            else InteractInDirection(0, 0);
        }
        private List<(string label, Action action)> ControllerNearbyChoices()
        {
            var choices = new List<(string, Action)>(); var at = CurrentZone.GetEntityCell(PlayerEntity);
            if (at == null) return choices;
            for (int y = at.Y - 1; y <= at.Y + 1; y++) for (int x = at.X - 1; x <= at.X + 1; x++)
            {
                var cell = CurrentZone.GetCell(x, y);
                if (cell?.IsVisible != true || !cell.Explored) continue;
                Entity target = null;
                foreach (var owner in cell.Occupants)
                    if (ControllerInteresting(owner)) { target = owner; break; }
                if (target == null) continue;
                int dx = x - at.X, dy = y - at.Y;
                choices.Add(($"{ControllerDirectionName(dx, dy)}: {target.GetDisplayName()}", () => InteractInDirection(dx, dy)));
            }
            return choices;
        }
        private void OpenControllerNearby()
        {
            var choices = ControllerNearbyChoices();
            if (choices.Count == 0) { MessageLog.Add("Nothing nearby to interact with."); return; }
            OpenControllerMenu("Interact nearby", choices);
        }
        private bool ControllerInteresting(Entity owner) => owner != null && owner != PlayerEntity
            && owner.GetPart<RenderPart>()?.Visible == true && !owner.HasTag(WorldInteractionSystem.WorldMetadataTag)
            && (owner.HasTag("Creature") || owner.HasPart<ContainerPart>() || owner.HasPart<StairsDownPart>()
                || owner.HasPart<StairsUpPart>() || owner.HasPart<HarvestablePart>()
                || owner.HasPart<FieldHarvestPart>() || owner.HasPart<DoorPart>() || owner.HasPart<BedPart>()
                || owner.HasPart<CampfirePart>() || (!WorldInteractionSystem.IsTerrain(owner)
                    && (owner.HasPart<ExaminablePart>() || owner.GetPart<PhysicsPart>()?.Takeable == true)));

        private void OpenControllerThrow()
        {
            var choices = new List<(string, Action)>();
            var inventory = PlayerEntity.GetPart<InventoryPart>();
            if (inventory != null) foreach (var item in inventory.Objects)
            {
                if (!HandlingService.CanThrow(PlayerEntity, item, out _)) continue;
                Entity selected = item;
                choices.Add(("Throw " + item.GetDisplayName(), () => {
                    if (PlayerEntity.GetPart<InventoryPart>()?.Objects.Contains(selected) != true) return;
                    var at = CurrentZone.GetEntityPosition(PlayerEntity);
                    BeginThrowTargeting(selected, ThrowOriginKind.Inventory, at.x, at.y, true);
                }));
            }
            if (choices.Count == 0) { MessageLog.Add("You aren't carrying anything you can throw."); return; }
            OpenControllerMenu("Throw an item", choices);
        }
        private void ControllerAttackNearest()
        {
            Entity target = null;
            foreach (var owner in CurrentZone.GetAllEntities())
                if (owner != PlayerEntity && owner.HasTag("Creature") && owner.GetStatValue("Hitpoints", 0) > 0
                    && owner.GetPart<RenderPart>()?.Visible == true
                    && SpatialQuery.Distance(CurrentZone, PlayerEntity, owner) == 1
                    && CurrentZone.GetEntityCell(owner)?.IsVisible == true
                    && (FactionManager.IsHostile(PlayerEntity, owner)
                        || owner.GetPart<SpreadTerritoryPart>()?.IsEnforcingAgainst(PlayerEntity, CurrentZone) == true))
                { target = owner; break; }
            if (target == null) { MessageLog.Add("No hostile is within melee reach."); return; }
            CombatSystem.PerformMeleeAttack(PlayerEntity, target, CurrentZone, _combatRng);
            EndTurnAndProcess();
        }
        private void ControllerForceAttack(int dx, int dy)
        {
            var at = CurrentZone.GetEntityCell(PlayerEntity); if (at == null) return;
            var cell = CurrentZone.GetCell(at.X + dx, at.Y + dy);
            if (cell?.IsVisible != true) { MessageLog.Add("No visible target in that direction."); return; }
            foreach (var target in cell.Occupants)
                if (target != PlayerEntity && target.HasTag("Creature") && target.GetStatValue("Hitpoints", 0) > 0)
                { ExecuteAttackOnNPC(target); return; }
            foreach (var target in cell.Occupants)
                if (DestructionSystem.IsBreakable(target))
                { DestructionSystem.StrikeStructure(target, PlayerEntity, CurrentZone, _combatRng); EndTurnAndProcess(); return; }
            MessageLog.Add("Nothing there to strike.");
        }

        private void OpenControllerPoints()
        {
            var choices = new List<(string, Action)>(); var seen = new HashSet<Entity>();
            var reachable = ControllerTravel.BuildReachable(CurrentZone, PlayerEntity);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
            {
                var cell = CurrentZone.GetCell(x, y);
                if (!cell.IsVisible || !cell.Explored) continue;
                foreach (var owner in cell.Occupants)
                {
                    if (!ControllerInteresting(owner) || !seen.Add(owner)
                        || (owner.HasTag("Creature") && FactionManager.IsHostile(PlayerEntity, owner))) continue;
                    // A destination is a safe reachable cell next to/on the object.
                    if (!ControllerApproachCell(reachable, x, y, out int tx, out int ty)) continue;
                    choices.Add(($"{owner.GetDisplayName()} [{x},{y}]", () => BeginControllerTravel(ControllerTravelMode.Point, tx, ty)));
                }
            }
            if (choices.Count == 0) { MessageLog.Add("No visible, safely reachable points of interest."); return; }
            OpenControllerMenu("Move to point of interest", choices);
        }
        private bool ControllerApproachCell(ControllerTravel.ReachabilityMap reachable, int x, int y, out int targetX, out int targetY)
        {
            targetX = targetY = 0; var at = CurrentZone.GetEntityPosition(PlayerEntity);
            int best = int.MaxValue;
            for (int cy = y - 1; cy <= y + 1; cy++) for (int cx = x - 1; cx <= x + 1; cx++)
            {
                int distance = Math.Max(Math.Abs(cx - at.x), Math.Abs(cy - at.y));
                if (distance == 0 || distance >= best) continue;
                if (!reachable.CanReach(cx, cy)) continue;
                best = distance; targetX = cx; targetY = cy;
            }
            return best != int.MaxValue;
        }
        private void BeginControllerTravel(ControllerTravelMode mode, int x, int y)
        {
            if (ControllerTravel.HasDanger(CurrentZone, PlayerEntity)) { MessageLog.Add("Not safe to travel or wait automatically here."); return; }
            CancelStairTravel(null);
            _controllerTravel = mode; _controllerTravelZone = CurrentZone; _controllerTravelActor = PlayerEntity;
            _controllerTravelHp = PlayerEntity.GetStatValue("Hitpoints");
            _controllerTravelX = x; _controllerTravelY = y;
            _controllerTravelBudget = mode == ControllerTravelMode.Wait ? x : Zone.Width * Zone.Height;
            MessageLog.Add(mode == ControllerTravelMode.Wait ? "Waiting. Any input stops." : "Travelling. Any input stops.");
        }
        private void CancelControllerTravel(string reason)
        {
            bool active = _controllerTravel != ControllerTravelMode.None;
            _controllerTravel = ControllerTravelMode.None; _controllerTravelActor = null; _controllerTravelZone = null;
            if (active && !string.IsNullOrEmpty(reason)) MessageLog.Add(reason);
        }
        private void StepControllerTravel()
        {
            if (_controllerTravel == ControllerTravelMode.None) return;
            if (_controllerTravelZone != CurrentZone || _controllerTravelActor != PlayerEntity
                || PlayerEntity.GetStatValue("Hitpoints") < _controllerTravelHp
                || ControllerTravel.HasDanger(CurrentZone, PlayerEntity))
            { CancelControllerTravel("You stop — conditions changed."); return; }
            if (_controllerTravelBudget-- <= 0) { CancelControllerTravel("You stop."); return; }
            if (_controllerTravel == ControllerTravelMode.Wait) { EndTurnAndProcess(); return; }
            int dx = 0, dy = 0;
            bool next = _controllerTravel == ControllerTravelMode.Edge
                ? ControllerTravel.TryStepTowardEdge(CurrentZone, PlayerEntity, _controllerTravelX, _controllerTravelY, out dx, out dy)
                : _controllerTravel == ControllerTravelMode.Point
                    ? ControllerTravel.TryStepTowardPoint(CurrentZone, PlayerEntity, _controllerTravelX, _controllerTravelY, out dx, out dy)
                    : ControllerTravel.TryStepTowardFrontier(CurrentZone, PlayerEntity, out dx, out dy);
            if (!next) { CancelControllerTravel("No further safe, known route."); return; }
            var old = CurrentZone.GetEntityPosition(PlayerEntity);
            // Planner forbids doors/obstacles/hostiles; repeat the real move only.
            // Never call the bump-combat or transition fallback from auto-travel.
            if (!MovementSystem.TryMove(PlayerEntity, CurrentZone, dx, dy))
            { CancelControllerTravel("Something is in the way."); return; }
            var now = CurrentZone.GetEntityPosition(PlayerEntity);
            ZoneRenderer?.RefreshMovement(old.x, old.y, now.x, now.y);
            EndTurnAndProcess();
        }

        private void RefreshControllerDirectionCue()
        {
            bool aiming = ControllerContext() != GamepadInputContext.Menu
                && (_inputState == InputState.Normal || _inputState == InputState.ControllerDirection
                    || _inputState == InputState.AwaitingDirection || _inputState == InputState.AwaitingTalkDirection
                    || _inputState == InputState.AwaitingRitePreviewDirection);
            if (aiming && NativeGamepadInput.TrySelectedDirection(out int dx, out int dy))
            {
                var at = CurrentZone.GetEntityCell(PlayerEntity);
                if (at != null && CurrentZone.InBounds(at.X + dx, at.Y + dy))
                {
                    _controllerDirectionCue.Activate(WorldCursorMode.InspectTarget, CurrentZone,
                        at.X + dx, at.Y + dy, at.X, at.Y, followMouse: false);
                    ZoneRenderer?.SetWorldCursorState(_controllerDirectionCue, PlayerEntity);
                    _controllerOwnsCue = true; return;
                }
            }
            if (_controllerOwnsCue)
            {
                _controllerDirectionCue.Deactivate();
                if (_inputState != InputState.LookMode && _inputState != InputState.ThrowTargeting) ZoneRenderer?.ClearWorldCursor();
                _controllerOwnsCue = false;
            }
        }
        private static string ControllerDirectionName(int x, int y)
            => y < 0 ? (x < 0 ? "NW" : x > 0 ? "NE" : "N")
                : y > 0 ? (x < 0 ? "SW" : x > 0 ? "SE" : "S") : x < 0 ? "W" : x > 0 ? "E" : "Underfoot";
    }
}
