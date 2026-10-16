using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public partial class InputHandler
    {
        Entity _throwDebtActor,_throwSelectionActor;
        Zone _throwSelectionZone,_throwDebtZone;
        TurnManager _throwDebtTurns;
        int _throwPreviewVersion=-1,_throwPreviewDoor=-1,_throwPreviewTick=-1,_throwPreviewVisibility;
        Rect _throwControlsRect;
        GUIStyle _throwControlStyle, _throwLegendStyle;

        void HoldThrowAction()
        { _throwDebtActor=PlayerEntity;_throwDebtZone=CurrentZone;_throwDebtTurns=TurnManager;ActionResolutionGuard.HoldPendingAction(_throwDebtActor); }

        bool TryOpenThrowSkill(ActivatedAbility ability)
        {
            if(ability==null || (ability.Command!="CommandMeasuredThrow" && ability.Command!="CommandDrivingThrow"
                && ability.Command!="CommandRecoveringStep"))return false;
            if(!ThrowingRules.TryBeginSelection(PlayerEntity,ability.Command,out var request))
            {MessageLog.Add("That throwing skill is unavailable or cooling down.");return true;}
            var cell=CurrentZone.GetEntityCell(PlayerEntity);
            var popup=new ThrowPopupState{Kind=ThrowPopupKind.SkillPicker,SkillSelection=request,
                Title=request.Kind==ThrowSelectionKind.Measured?"Prepare which item?":request.Kind==ThrowSelectionKind.Driving?"Drive with which item?":"Step & recover (no equip)",
                SourceTileX=cell.X,SourceTileY=cell.Y};
            var seen=new HashSet<Entity>();
            void Add(Entity item)
            {
                if(item==null || !seen.Add(item))return;
                var world=CurrentZone.GetEntityCell(item);
                if(world!=null && !ThrowTrajectory.IsKnownOwner(world,item))return;
                if(request.Kind==ThrowSelectionKind.Recovery)
                {if(!ThrowingRules.CanRecover(PlayerEntity,item,CurrentZone,out _))return;}
                else if(!ThrowingRules.IsPhysical(item) || !HandlingService.CanThrow(PlayerEntity,item,out _)
                    || (item.GetPart<StackerPart>()?.StackCount??1)<=0
                    || request.Kind==ThrowSelectionKind.Driving && HandlingService.GetWeight(item)<8)return;
                popup.Options.Add(new ThrowPopupOption{Item=item,Label=item.GetDisplayName()
                    +(world==null?(item.GetPart<PhysicsPart>()?.Equipped==PlayerEntity?" (equipped)":""):$" [{world.X},{world.Y}]")});
            }
            if(request.Kind!=ThrowSelectionKind.Recovery)
            {
                var pack=PlayerEntity.GetPart<InventoryPart>();
                if(pack!=null){foreach(var item in pack.Objects)Add(item);foreach(var item in pack.EquippedItems.Values)Add(item);}
            }
            for(int y=cell.Y-1;y<=cell.Y+1;y++)for(int x=cell.X-1;x<=cell.X+1;x++)
            {
                if(request.Kind!=ThrowSelectionKind.Recovery && Math.Abs(x-cell.X)+Math.Abs(y-cell.Y)>1)continue;
                var source=CurrentZone.GetCell(x,y);if(source==null || !source.IsVisible || !source.Explored)continue;
                foreach(var item in source.Objects)Add(item);
            }
            if(popup.Options.Count==0)
            {MessageLog.Add(request.Kind==ThrowSelectionKind.Recovery?"No adjacent unit from your own throw can be recovered. Ordinary pickup and ground throws remain available.":"No accessible physical item meets this throw's requirements.");return true;}
            _throwPopup=popup;OpenThrowPopup();return true;
        }

        void ExecuteThrowSkillSelection()
        {
            var popup=_throwPopup;var item=popup.Options[popup.CursorIndex].Item;var request=popup.SkillSelection;
            ClearThrowPopupTiles();ExitCenteredPopupOverlayViewToGameplay();_throwPopup=null;_inputState=InputState.Normal;
            if(request.Kind==ThrowSelectionKind.Recovery)
            {
                var result=ThrowingRules.Recover(PlayerEntity,item,CurrentZone);
                if(!string.IsNullOrEmpty(result.Reason))MessageLog.Add(result.Reason);
                if(result.Committed){HoldThrowAction();EndTurnAndProcess();SyncHotbarState();}
                return;
            }
            if(BeginThrowTargeting(item,ThrowOriginKind.Skill,popup.SourceTileX,popup.SourceTileY,true))
            {
                _pendingThrowTarget.Mode=request.Kind==ThrowSelectionKind.Measured?ThrowMode.Measured:ThrowMode.Driving;
                RefreshThrowPreview(true);
            }
        }

        void ToggleThrowAim()
        {
            if(_pendingThrowTarget==null)return;
            _pendingThrowTarget.GroundAim=!_pendingThrowTarget.GroundAim;RefreshThrowPreview(true);
        }
        void ToggleThrowFocus()
        {
            if(_pendingThrowTarget==null)return;
            if(!ThrowingRules.CanFocus(PlayerEntity,_pendingThrowTarget.Item))
            {MessageLog.Add("Focused Splash needs its skill and a tonic payload.");return;}
            _pendingThrowTarget.Focused=!_pendingThrowTarget.Focused;RefreshThrowPreview(true);
        }
        int ThrowVisibilitySignature()
        {
            unchecked
            {
                int signature=17;
                void CellHash(Cell cell)
                {
                    if(cell==null)return;
                    signature=signature*31+(cell.IsVisible?1:0)+(cell.Explored?2:0);
                    if(!cell.IsVisible || !cell.Explored)return;
                    for(int i=0;i<cell.Occupants.Count;i++)signature=signature*31+(cell.Occupants[i].GetPart<RenderPart>()?.Visible==true?1:0);
                }
                var preview=_worldCursorState.ThrowPreview;
                if(preview!=null)foreach(var p in preview.Trace.VisibilityCells)
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)CellHash(CurrentZone.GetCell(p.X+dx,p.Y+dy));
                CellHash(CurrentZone.GetCell(_worldCursorState.X,_worldCursorState.Y));
                return signature;
            }
        }
        void RefreshThrowPreview(bool force=false)
        {
            if(_pendingThrowTarget==null || CurrentZone==null || !_worldCursorState.Active || _worldCursorState.Mode!=WorldCursorMode.ThrowTarget)return;
            int visibility=ThrowVisibilitySignature(),tick=TurnManager?.TickCount??0;
            if(!force && _throwPreviewVersion==CurrentZone.EntityVersion && _throwPreviewDoor==CurrentZone.DoorOcclusionVersion
                && _throwPreviewTick==tick && _throwPreviewVisibility==visibility)return;
            var state=_pendingThrowTarget;
            state.Intent=state.GroundAim?ThrowIntent.Ground(_worldCursorState.X,_worldCursorState.Y)
                :ThrowTrajectory.ResolveIntent(CurrentZone,PlayerEntity,_worldCursorState.X,_worldCursorState.Y,true);
            var preview=ThrowPreview.Build(CurrentZone,PlayerEntity,state.Item,state.Intent,_worldCursorState.MaxRange??0,state.Focused,state.Mode==ThrowMode.Driving,state.Mode==ThrowMode.Measured);
            _worldCursorState.SetThrowPreview(state.GroundAim,state.Focused,preview);
            _throwPreviewVersion=CurrentZone.EntityVersion;_throwPreviewDoor=CurrentZone.DoorOcclusionVersion;_throwPreviewTick=tick;
            _throwPreviewVisibility=ThrowVisibilitySignature();
            string heading=state.Mode==ThrowMode.Measured?"Prepare: "+state.Item.GetDisplayName():state.Mode==ThrowMode.Driving?"Driving: "+state.Item.GetDisplayName():"Throw: "+state.Item.GetDisplayName();
            string description=preview.Description+(state.Mode==ThrowMode.Measured?$" Prepared throw: +{ThrowingRules.MeasuredPenetrationBonus} penetration and cap.":"");
            var details=new List<string>{description,"Cyan: known route / initial splash.","Yellow: item rest / spill. Red: first contact.","Magenta: conditional push.","Tab / Y: body or ground. F / LB: splash.","Enter / A: confirm. Esc / B: cancel."};
            ZoneRenderer?.SetLookSnapshot(new LookSnapshot(state.Intent.X,state.Intent.Y,heading,
                state.Mode==ThrowMode.Measured?"Prepare now; throw next turn.":state.Intent.Kind==ThrowAimKind.Entity?"Body aim":"Ground aim",details,
                state.Intent.Target,CurrentZone.GetCell(state.Intent.X,state.Intent.Y)));
            ZoneRenderer?.SetWorldCursorState(_worldCursorState,PlayerEntity);
        }

        bool ThrowControlsContainMouse()
            => _throwControlsRect.Contains(new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y));

        bool RenderThrowControls()
        {
            if(_inputState!=InputState.ThrowTargeting || _pendingThrowTarget==null || !isActiveAndEnabled)return false;
            var camera=Camera.main;var viewport=camera!=null?camera.pixelRect:new Rect(0,0,Screen.width*.7f,Screen.height*.8f);
            if(_throwControlStyle==null)
            {
                _throwControlStyle=new GUIStyle(GUIStyle.none){fontSize=14,alignment=TextAnchor.MiddleCenter,padding=new RectOffset(5,5,4,4)};
                _throwControlStyle.normal.background=null;_throwControlStyle.hover.background=null;
                _throwControlStyle.active.background=null;_throwControlStyle.focused.background=null;
                _throwControlStyle.normal.textColor=_throwControlStyle.hover.textColor=new Color(.8f,.94f,.79f);
                _throwControlStyle.active.textColor=Color.white;
                _throwLegendStyle=new GUIStyle(GUI.skin.label){fontSize=12,alignment=TextAnchor.MiddleLeft};
                _throwLegendStyle.normal.textColor=new Color(.78f,.85f,.78f);
            }
            float width=Mathf.Min(viewport.width-12,640);
            _throwControlsRect=new Rect(viewport.x+6,Screen.height-viewport.y-82,width,76);
            ThrowPanel(_throwControlsRect,new Color(.025f,.055f,.05f,.97f),new Color(.22f,.38f,.3f));
            bool pad=NativeGamepadInput.IsConnected;
            string aim=(_pendingThrowTarget.GroundAim?"Ground":"Body")+" ["+(pad?"Y":"Tab")+"]";
            float buttonWidth=Mathf.Min(180,(width-16)/3);
            bool ModeButton(Rect box,string label)
            {
                ThrowPanel(box,new Color(.05f,.12f,.10f),new Color(.38f,.68f,.48f));
                return GUI.Button(box,label,_throwControlStyle);
            }
            if(ModeButton(new Rect(_throwControlsRect.x+4,_throwControlsRect.y+3,buttonWidth,25),aim))ToggleThrowAim();
            bool focusable=ThrowingRules.CanFocus(PlayerEntity,_pendingThrowTarget.Item);
            if(focusable && ModeButton(new Rect(_throwControlsRect.x+8+buttonWidth,_throwControlsRect.y+3,buttonWidth,25),
                (_pendingThrowTarget.Focused?"Focused":"Broad")+" ["+(pad?"LB":"F")+"]"))ToggleThrowFocus();
            GUI.Label(new Rect(_throwControlsRect.x+8,_throwControlsRect.y+30,width-16,21),
                pad?"D-pad / sticks: aim | A / RT: confirm | B: cancel":"Arrows / mouse: aim | Enter / click: confirm | Esc: cancel",_throwLegendStyle);
            GUI.Label(new Rect(_throwControlsRect.x+8,_throwControlsRect.y+51,width-16,21),
                "Cyan: route / splash | Yellow: rest | Red: contact | Magenta: push",_throwLegendStyle);
            return true;
        }
        static void ThrowPanel(Rect box,Color fill,Color border)
        {
            var old=GUI.color;GUI.color=border;GUI.DrawTexture(box,Texture2D.whiteTexture);
            GUI.color=fill;GUI.DrawTexture(new Rect(box.x+1,box.y+1,box.width-2,box.height-2),Texture2D.whiteTexture);GUI.color=old;
        }

    }
}
