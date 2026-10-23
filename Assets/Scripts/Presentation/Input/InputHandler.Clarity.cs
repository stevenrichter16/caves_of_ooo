using CavesOfOoo.Core;
namespace CavesOfOoo.Rendering
{
    public partial class InputHandler
    {
        private Entity _decisionReaderActor;
        private Zone _decisionReaderZone;
        private bool _decisionReaderReturnToPause;
        private bool _pauseControlsPending;
        private void OpenControlsReader()
        {
            if (_inputState == InputState.Normal) OpenDecisionReader(ControlsReference.BuildReaderText());
        }
        private void OpenPlayerStatusReader()
        {
            if (_inputState != InputState.InventoryOpen || InventoryUI?.PlayerEntity != PlayerEntity || InventoryUI.IsSearching) return;
            string text = InventoryDecisionDetails.Status(PlayerEntity);
            string haul = InventoryDecisionDetails.Haul(PlayerEntity, CurrentZone);
            OpenDecisionReader(text + (string.IsNullOrEmpty(haul) ? "" : "\n\n" + haul));
        }
        private void OpenInventoryDetailsReader()
        {
            var inventory = InventoryUI;
            var viewer = PlayerEntity;
            var zone = CurrentZone;
            if (_inputState != InputState.InventoryOpen || inventory == null
                || inventory.PlayerEntity != viewer || inventory.CurrentZone != zone) return;
            var document = inventory.SelectedDecisionPresentation(out string text);
            // Description providers may invalidate the selected owner or replace the reader's
            // live context. Do not publish the old text into that new context.
            if (_inputState != InputState.InventoryOpen || InventoryUI != inventory
                || PlayerEntity != viewer || CurrentZone != zone
                || inventory.PlayerEntity != viewer || inventory.CurrentZone != zone
                || (document != null && !ExamineDocumentBuilder.IsCurrentSubject(document))) return;
            OpenDecisionReaderWithPresentation(text, document);
        }
        private void OpenPickupDetailsReader()
        {
            if (_inputState == InputState.PickupOpen && PickupUI?.PlayerEntity == PlayerEntity && PickupUI.CurrentZone == CurrentZone)
                OpenDecisionReader(PickupUI.SelectedDecisionDetails());
        }
        private void OpenTradeDetailsReader()
        {
            if (_inputState == InputState.TradeOpen && TradeUI?.PlayerEntity == PlayerEntity)
                OpenDecisionReader(TradeUI.SelectedDecisionDetails());
        }
        private void OpenDecisionReader(string text) => OpenDecisionReaderWithPresentation(text, null);

        private void OpenDecisionReaderWithPresentation(string text, AnnouncementDocument document)
        {
            if (PlayerEntity == null || AnnouncementUI == null || string.IsNullOrWhiteSpace(text)) return;
            _decisionReaderActor = PlayerEntity; _decisionReaderZone = CurrentZone;
            _stateBeforeAnnouncement = _inputState;
            if (_inputState == InputState.InventoryOpen) InventoryUI.HideForDecisionReader();
            else if (_inputState == InputState.PickupOpen) PickupUI.HideForDecisionReader();
            else if (_inputState == InputState.TradeOpen) TradeUI.HideForDecisionReader();
            if ((_inputState == InputState.InventoryOpen || _inputState == InputState.TradeOpen) && CameraFollow != null)
                CameraFollow.SetCenteredPopupOverlayOverUIView();
            else EnterCenteredPopupOverlayView();
            AnnouncementUI.Open(text, null, document);
            _inputState = InputState.AnnouncementOpen;
        }
        private void RestoreDecisionReader(InputState prior)
        {
            bool same = PlayerEntity == _decisionReaderActor && CurrentZone == _decisionReaderZone;
            _decisionReaderActor = null; _decisionReaderZone = null;
            bool pause = _decisionReaderReturnToPause; _decisionReaderReturnToPause = false;
            if (!same)
            {
                InventoryUI?.Close(); PickupUI?.Close(); TradeUI?.Close();
                _inputState = InputState.Normal; ExitCenteredPopupOverlayViewToGameplay();
                if (ZoneRenderer != null) { ZoneRenderer.Paused = false; ZoneRenderer.MarkDirty("UI.Reader.ContextChanged"); }
                return;
            }
            if (prior == InputState.InventoryOpen || prior == InputState.TradeOpen)
            {
                if (CameraFollow != null)
                {
                    if (CameraFollow.PopupOverlayCamera != null) CameraFollow.PopupOverlayCamera.enabled = false;
                    CameraFollow.SetUIView(FullscreenUiGridWidth, FullscreenUiGridHeight);
                }
                if (prior == InputState.InventoryOpen) InventoryUI?.RestoreAfterDecisionReader();
                else TradeUI?.RestoreAfterDecisionReader();
            }
            else if (prior == InputState.PickupOpen)
            { EnterCenteredPopupOverlayView(); PickupUI?.RestoreAfterDecisionReader(); }
            else if (pause)
            {
                _pauseMenuController.Open(); _pauseMenuController.HoverSelect(PauseMenuController.ControlsIndex);
                EnterCenteredPopupOverlayView(); // PauseMenuUI redraws its externally reopened controller next frame.
            }
            else ExitCenteredPopupOverlayViewToGameplay();
        }
    }
}
