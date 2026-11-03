using System;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public partial class InputHandler
    {
        private bool _phoneTravel, _phoneHidden, _phoneOverlay;
        internal bool PhoneUsesCursor => _inputState == InputState.LookMode || _inputState == InputState.ThrowTargeting || IsVaultTargeting;
        internal LookSnapshot PhoneLookSnapshot { get; private set; }
        internal string PhoneAbilityName => PlayerEntity?.GetPart<ActivatedAbilitiesPart>()?.GetAbilityBySlot(_selectedHotbarSlot)?.DisplayName ?? "Use ability";
        private Entity _phoneActor;
        private Zone _phoneZone;
        private float _phoneLastCheckpoint;
        private long _phoneLastCheckpointTick;
        private int _phoneViewRevision = -1, _phoneLogSerial = -1, _phoneUiRevision = -1;
        private long _phoneViewTick = -1;
        private int PhoneContext => (int)_inputState + (_buildMenuController.IsOpen ? 1024 : 0)
            + (_bootMenuController.IsActive ? 2048 : 0) + (_deathScreenController.IsActive ? 4096 : 0)
            + (_pauseMenuUI?.IsOpen == true ? 8192 : 0);

        private void BeginPhoneFrame()
        {
            if (!PhoneInput.Enabled) return;
            if (_phoneActor != PlayerEntity || _phoneZone != CurrentZone)
            {
                PhoneInput.Gate.Quarantine(); _phoneActor = PlayerEntity; _phoneZone = CurrentZone;
                _phoneLastCheckpointTick = TurnManager?.TickCount ?? 0;
            }
            if (NativeGamepadInput.AnyPressed) _phoneTravel = false;
            PhoneInput.BeginFrame(PhoneContext, ControllerContext());
            if (!PhoneUsesCursor) PhoneLookSnapshot = null;
        }

        private void EndPhoneFrame()
        {
            if (!PhoneInput.Enabled) return;
            bool acted = PhoneInput.AnyPressed;
            PhoneInput.EndFrame();
            PhoneInput.Gate.SetContext(PhoneContext);
            if (_phoneUiRevision != PhoneMenuMirror.Revision)
            { _phoneUiRevision = PhoneMenuMirror.Revision; PhoneInput.Gate.Quarantine(); }
            if (acted || _phoneViewRevision != PhoneInput.Gate.Revision || _phoneLogSerial != MessageLog.NextSerialValue
                || _phoneViewTick != (TurnManager?.TickCount ?? 0))
            {
                _phoneViewRevision = PhoneInput.Gate.Revision; _phoneLogSerial = MessageLog.NextSerialValue;
                _phoneViewTick = TurnManager?.TickCount ?? 0;
                PhoneMenuMirror.Publish(this, ControllerContext().ToString(), _inputState.ToString(),
                    _bootMenuController.IsActive, _deathScreenController.IsActive,
                    InventoryUI != null ? InventoryUI.PhoneLoadoutName : null);
            }
            if (!PhoneInput.Gate.Suspended && !_bootMenuController.IsActive && !_buildMenuController.IsOpen
                && _inputState == InputState.Normal && !_deathScreenController.IsActive && TurnManager != null && PlayerEntity != null
                && PlayerEntity.GetStatValue("Hitpoints", 1) > 0 && !CombatSystem.IsDeathHandled(PlayerEntity)
                && TurnManager.WaitingForInput && !SaveGameService.IsStorageBusy
                && TurnManager.TickCount != _phoneLastCheckpointTick && Time.unscaledTime - _phoneLastCheckpoint > 30f)
            {
                if (SaveGameService.RequestQuickSave())
                { _phoneLastCheckpoint = Time.unscaledTime; _phoneLastCheckpointTick = TurnManager.TickCount; }
            }
        }

        private bool HandlePhoneWorldInput()
        {
            if (!PhoneInput.Enabled || PhoneInput.FrameIntent == null) return false;
            var intent = PhoneInput.FrameIntent;
            switch (intent.kind)
            {
                case "step":
                    if (intent.x != 0 || intent.y != 0) ExecuteMovementStep(intent.x, intent.y);
                    return true;
                case "wait": EndTurnAndProcess(); return true;
                case "inspect":
                    if (ZoneRenderer != null && ZoneRenderer.ScreenToLookCell(
                        new Vector2(intent.screenX * Screen.width, (1f - intent.screenY) * Screen.height), Camera.main, out int x, out int y)
                        && EnterLookMode())
                    { _worldCursorState.SetPosition(x, y); RefreshLookSnapshot(); }
                    return true;
                case "command":
                    if (!Enum.TryParse(intent.value, out GamepadCommand command)) return true;
                    if (command == GamepadCommand.Look) { EnterLookMode(); return true; }
                    ExecuteControllerCommand(command);
                    _phoneTravel = _controllerTravel != ControllerTravelMode.None;
                    return true;
            }
            return false;
        }

        public void SetPhoneOverlay(string overlay)
        {
            if (!PhoneInput.Enabled) return;
            CancelControllerTravel(null); _phoneTravel = false; _stairInterruptRequested = true;
            _phoneOverlay = overlay == "journal";
            PhoneInput.Direction.Clear();
            if (_phoneHidden || _phoneOverlay) PhoneInput.Gate.Suspend(); else PhoneInput.Gate.Resume();
            _phoneViewRevision = -1;
        }

        public void SetPhoneVisibility(string visibility)
        {
            if (!PhoneInput.Enabled) return;
            CancelControllerTravel(null); _stairInterruptRequested = true; _phoneTravel = false;
            NativeGamepadInput.QuarantineHeldControls();
            _phoneHidden = visibility == "hidden";
            PhoneInput.Direction.Clear();
            if (_phoneHidden)
            {
                PhoneInput.Gate.Suspend();
                // Settle an already committed action, never invent a new turn for suspension.
                if (_throwDebtActor != null || _inputState == InputState.WaitingForFxResolution)
                {
                    ZoneRenderer?.CancelWorldFx(); _inputState = InputState.Normal; _pendingAbility = null;
                    EndTurnAndProcess();
                }
                if (!_bootMenuController.IsActive && !_buildMenuController.IsOpen && !_deathScreenController.IsActive
                    && PlayerEntity != null && PlayerEntity.GetStatValue("Hitpoints", 1) > 0 && !CombatSystem.IsDeathHandled(PlayerEntity))
                { SaveGameService.RequestQuickSave(); SaveGameService.PumpPendingSaves(); }
            }
            else { if (_phoneOverlay) PhoneInput.Gate.Suspend(); else PhoneInput.Gate.Resume(); _phoneViewRevision = -1; }
        }
    }
}
