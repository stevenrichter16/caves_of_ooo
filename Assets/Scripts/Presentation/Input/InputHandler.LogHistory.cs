using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public partial class InputHandler
    {
        public WorldLogHistoryUI LogHistoryUI {get;private set;}
        InputState _logHistoryReturnState;
        Entity _logHistoryActor;Zone _logHistoryZone;TurnManager _logHistoryTurns;

        /// <summary>Opens a read-only history from normal play or Look. It does
        /// not publish/consume announcements or invoke a gameplay command.</summary>
        public void OpenLogHistory()
        {
            if((_inputState!=InputState.Normal&&_inputState!=InputState.LookMode)
                ||PlayerEntity==null||CurrentZone==null||TurnManager==null
                ||!TurnManager.WaitingForInput||TurnManager.CurrentActor!=PlayerEntity
                ||_pauseMenuUI?.IsOpen==true||AnnouncementUI?.IsOpen==true)return;
            var foreground=AnnouncementUI?.Tilemap??ZoneRenderer?.CenteredPopupFgTilemap;
            if(foreground==null)return; // No hidden input-blocking popup without a canvas.
            if(LogHistoryUI==null)LogHistoryUI=gameObject.GetComponent<WorldLogHistoryUI>()??gameObject.AddComponent<WorldLogHistoryUI>();
            LogHistoryUI.Tilemap=foreground;
            LogHistoryUI.BgTilemap=AnnouncementUI?.BgTilemap??ZoneRenderer?.CenteredPopupBgTilemap;
            LogHistoryUI.PopupCamera=AnnouncementUI?.PopupCamera??CameraFollow?.PopupOverlayCamera;
            _logHistoryReturnState=_inputState;_logHistoryActor=PlayerEntity;_logHistoryZone=CurrentZone;_logHistoryTurns=TurnManager;
            _inputState=InputState.LogHistoryOpen;EnterCenteredPopupOverlayView();LogHistoryUI.Open();
        }
        void HandleLogHistoryInput()
        {
            bool current=_logHistoryActor==PlayerEntity&&_logHistoryZone==CurrentZone&&_logHistoryTurns==TurnManager;
            if(!current||LogHistoryUI==null||!LogHistoryUI.IsOpen){CloseLogHistory(current);return;}
            LogHistoryUI.HandleInput();if(!LogHistoryUI.IsOpen)CloseLogHistory(true);
        }
        void CloseLogHistory(bool restore)
        {
            LogHistoryUI?.Close();_inputState=restore?_logHistoryReturnState:InputState.Normal;
            _logHistoryActor=null;_logHistoryZone=null;_logHistoryTurns=null;ExitCenteredPopupOverlayViewToGameplay();_lastMoveTime=Time.time;
        }
    }
}
