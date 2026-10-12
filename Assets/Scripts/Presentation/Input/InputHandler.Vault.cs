using System;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using UnityEngine;
namespace CavesOfOoo.Rendering
{
    public partial class InputHandler
    {
        Entity _vaultCursorActor; Zone _vaultCursorZone; Cell _vaultConfirmedCell;
        bool _vaultAttackConfirmation;
        bool IsVaultTargeting=>_pendingAbility?.Command=="CommandVault"&&_worldCursorState.Active&&_worldCursorState.Mode==WorldCursorMode.AbilityTarget;
        void BeginVaultTargeting(ActivatedAbility ability,Cell source)
        {
            _pendingAbility=ability;_vaultCursorActor=PlayerEntity;_vaultCursorZone=CurrentZone;
            _worldCursorState.Activate(WorldCursorMode.AbilityTarget,CurrentZone,source.X,source.Y,source.X,source.Y,2,true);
            _lastLookMousePosition=Input.mousePosition;_inputState=InputState.AwaitingDirection;
            ZoneRenderer?.SetWorldCursorState(_worldCursorState,PlayerEntity);SyncHotbarState();
            MessageLog.Add("Vault: choose a visible tile within 2 cells. Red = melee target; yellow = landing. Confirm to vault, cancel to leave.");
        }
        void ExitVaultTargeting()
        {
            _worldCursorState.Deactivate();ZoneRenderer?.ClearWorldCursor();_pendingAbility=null;
            _vaultCursorActor=null;_vaultCursorZone=null;_vaultConfirmedCell=null;_vaultAttackConfirmation=false;
            _inputState=InputState.Normal;SyncHotbarState();
        }
        void HandleVaultTargeting()
        {
            if(PlayerEntity!=_vaultCursorActor||CurrentZone!=_vaultCursorZone||!IsVaultTargeting)
            {ExitVaultTargeting();return;}
            if(InputHelper.GetKeyDown(KeyCode.Escape)){ExitVaultTargeting();return;}
            if(GetDirectionKeyDown(out int dx,out int dy))
            {MoveThrowCursor(dx,dy);_lastLookMousePosition=Input.mousePosition;return;}
            if(InputHelper.GetKeyDown(KeyCode.Return)||InputHelper.GetKeyDown(KeyCode.KeypadEnter)){ConfirmVaultTarget();return;}
            if(Input.GetMouseButtonDown(0)){if(TryUpdateThrowCursorFromMouse(false))ConfirmVaultTarget();return;}
            TryUpdateThrowCursorFromMouse(true);
        }
        void ConfirmVaultTarget()
        {
            if(PlayerEntity!=_vaultCursorActor||CurrentZone!=_vaultCursorZone||_pendingAbility?.IsUsable!=true)
            {ExitVaultTargeting();return;}
            var selected=CurrentZone.GetCell(_worldCursorState.X,_worldCursorState.Y);
            if(selected?.IsVisible!=true||!VaultTargeting.TryPlan(PlayerEntity,CurrentZone,selected.X,selected.Y,out _,out var target,out _))
            {MessageLog.Add("No clear Vault landing at that visible target (range 2).");return;}
            _vaultConfirmedCell=selected;
            if(target!=null&&!FactionManager.IsHostile(PlayerEntity,target))
            {_vaultAttackConfirmation=true;OpenAttackConfirmation(target);return;}
            CompleteVaultTarget(target);
        }
        void ResolveVaultAttackConfirmation(bool confirmed,Entity expected)
        {
            _vaultAttackConfirmation=false;ExitCenteredPopupOverlayViewToGameplay();
            if(!confirmed){MessageLog.Add("You decide against it.");ExitVaultTargeting();return;}
            CompleteVaultTarget(expected);
        }
        void CompleteVaultTarget(Entity expected)
        {
            var selected=_vaultConfirmedCell;var ability=_pendingAbility;
            if(PlayerEntity!=_vaultCursorActor||CurrentZone!=_vaultCursorZone||ability?.IsUsable!=true||selected?.IsVisible!=true
                ||!VaultTargeting.TryPlan(PlayerEntity,CurrentZone,selected.X,selected.Y,out _,out var target,out _)||target!=expected)
            {MessageLog.Add("The Vault target or landing changed.");ExitVaultTargeting();return;}
            var source=CurrentZone.GetEntityCell(PlayerEntity);
            ExitVaultTargeting();
            ResolveAbilityCommand(ability,source,Math.Sign(selected.X-source.X),Math.Sign(selected.Y-source.Y),selected);
        }
    }
}
