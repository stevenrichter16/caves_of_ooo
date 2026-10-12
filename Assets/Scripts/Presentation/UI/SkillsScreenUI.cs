using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>Full-screen Field Manual with optional prerequisite board. Retained
    /// rendering, free browsing, reviewed purchases and explicit hotbar assignment.</summary>
    public partial class SkillsScreenUI : MonoBehaviour
    {
        // Kept for existing bootstrap/scenario wiring; the full-screen Canvas owns rendering.
        public Tilemap Tilemap, BgTilemap;
        public Camera PopupCamera;
        public Entity PlayerEntity { get; set; }
        public SkillBrowserModel Model { get; } = new SkillBrowserModel();
        public bool IsOpen { get; private set; }
        public int RowCount { get { int count=0;foreach(var d in Model.Disciplines)count+=d.Rows.Count;return count; } }
        public int CursorIndex => Model.SkillIndex;
        public string SelectedClass => IsOpen ? Model.Selected.Class : null;
        public bool IsReviewingPurchase => Model.PendingPurchase!=null;
        public bool IsAssigning => assigning;
        public int DetailPage => detailPage;
        public int DetailPageCount => Mathf.Max(1,(detailLines.Count+DetailLines-1)/DetailLines);
        public int RenderCount { get; private set; }
        const int DetailLines=17, CardsPerPage=8;
        bool railFocus,assigning,learnSelected,hidden;
        int detailPage,slotIndex;
        string detailClass,detailSource;
        readonly List<string> detailLines=new List<string>();
        bool lastController;

        public void Open(Entity actor)
        {
            if(actor==null)return;
            PlayerEntity=actor;Model.Open(actor);IsOpen=true;hidden=false;assigning=false;learnSelected=false;
            EnsureView();canvasRoot.SetActive(true);Render();
        }
        public void Close()
        {
            Model.Cancel();assigning=false;IsOpen=false;hidden=false;PlayerEntity=null;
            if(canvasRoot!=null)canvasRoot.SetActive(false);
        }
        public void RefreshSnapshot() { Model.Refresh();if(IsOpen)Render(); }
        public void HideForReader() { hidden=true;if(canvasRoot!=null)canvasRoot.SetActive(false); }
        public void RestoreAfterReader() { hidden=false;Model.Refresh();if(IsOpen){canvasRoot.SetActive(true);Render();} }
        public void PageDetails(int delta)
        { detailPage=Mathf.Clamp(detailPage+delta,0,DetailPageCount-1);if(IsOpen)Render(); }
        public void HandleInput()
        {
            if(!IsOpen || hidden)return;
            bool changed=false;
            if(InputHelper.GetKeyDown(KeyCode.Escape) || InputHelper.GetKeyboardKeyDown(KeyCode.X))
            {
                if(IsReviewingPurchase || assigning) { Model.Cancel();assigning=false;Render(); }
                else Close();
                return;
            }
            if(IsReviewingPurchase)
            {
                if(InputHelper.GetKeyDown(KeyCode.LeftArrow)) { learnSelected=false;changed=true; }
                if(InputHelper.GetKeyDown(KeyCode.RightArrow)) { learnSelected=true;changed=true; }
                if(ConfirmKey()) { if(learnSelected)Model.ConfirmPurchase();else Model.Cancel();changed=true; }
            }
            else if(assigning)
            {
                if(InputHelper.GetKeyDown(KeyCode.UpArrow)||InputHelper.GetKeyDown(KeyCode.LeftArrow)) {slotIndex=Mathf.Max(0,slotIndex-1);changed=true;}
                if(InputHelper.GetKeyDown(KeyCode.DownArrow)||InputHelper.GetKeyDown(KeyCode.RightArrow)) {slotIndex=Mathf.Min(9,slotIndex+1);changed=true;}
                if(ConfirmKey()) { Model.Assign(slotIndex);assigning=false;changed=true; }
            }
            else
            {
                if(InputHelper.GetKeyDown(KeyCode.Q)) {Model.SelectDiscipline(Model.DisciplineIndex-1);changed=true;}
                else if(InputHelper.GetKeyDown(KeyCode.E)) {Model.SelectDiscipline(Model.DisciplineIndex+1);changed=true;}
                else if(InputHelper.GetKeyDown(KeyCode.Tab)) {Model.Overview=!Model.Overview;changed=true;}
                else if(InputHelper.GetKeyDown(KeyCode.I)) {BeginAssign();changed=true;}
                else if(InputHelper.GetKeyDown(KeyCode.UpArrow)||InputHelper.GetKeyDown(KeyCode.K)) {Navigate(0,-1);changed=true;}
                else if(InputHelper.GetKeyDown(KeyCode.DownArrow)||InputHelper.GetKeyDown(KeyCode.J)) {Navigate(0,1);changed=true;}
                else if(InputHelper.GetKeyDown(KeyCode.LeftArrow)) {Navigate(-1,0);changed=true;}
                else if(InputHelper.GetKeyDown(KeyCode.RightArrow)) {Navigate(1,0);changed=true;}
                else if(ConfirmKey()) { if(railFocus)railFocus=false;else Review();changed=true; }
                if(InputHelper.GetKeyDown(KeyCode.PageUp)) {PageDetails(-1);return;}
                if(InputHelper.GetKeyDown(KeyCode.PageDown)||InputHelper.GetKeyDown(KeyCode.D)||NativeGamepadInput.GetKeyDown(KeyCode.F1)) {PageDetails(1);return;}
            }
            // One input source per frame; never hit-test a stale layout after a key changes it.
            if(!changed && HandlePointer())changed=true;
            if(lastController!=NativeGamepadInput.IsConnected)changed=true;
            if(changed)Render();
        }
        bool ConfirmKey()=>InputHelper.GetKeyDown(KeyCode.Return)||InputHelper.GetKeyDown(KeyCode.KeypadEnter);
        void Review() {Model.ReviewPurchase();learnSelected=false;}
        void BeginAssign()
        { if(Model.CanAssign){assigning=true;slotIndex=Mathf.Max(0,Model.AssignedSlot);} else Model.ExplainAssignmentUnavailable(); }
        void Navigate(int dx,int dy)
        {
            if(railFocus)
            { if(dx>0)railFocus=false;else if(dy!=0)Model.SelectDiscipline(Model.DisciplineIndex+dy);return; }
            int i=Model.SkillIndex;
            if(!Model.Overview)
            {if(dx<0)railFocus=true;else if(dy!=0)Model.SelectSkill(i+dy);return;}
            if(dx<0 && (i==0 || (i-1)%2==0)){railFocus=true;return;}
            if(dx!=0) { if(i>0 && !(dx>0 && (i-1)%2==1))Model.SelectSkill(i+dx);return; }
            Model.SelectSkill(i==0 ? (dy>0?1:0) : (dy<0 && i<=2 ? 0 : i+dy*2));
        }
        bool HandlePointer()
        {
            if(!Input.GetMouseButtonDown(0) && Input.mouseScrollDelta.y==0)return false;
            Vector2 point;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(contentRoot,Input.mousePosition,null,out point))return false;
            point=new Vector2(point.x+640,400-point.y);
            if(Input.mouseScrollDelta.y!=0)
            {
                if(IsReviewingPurchase||assigning)return false;
                int direction=Input.mouseScrollDelta.y>0?-1:1;
                if(point.x>=766)detailPage=Mathf.Clamp(detailPage+direction,0,DetailPageCount-1);
                else if(point.x<252)Model.SelectDiscipline(Model.DisciplineIndex+direction);
                else Model.SelectSkill(Model.SkillIndex+direction);
                return true;
            }
            foreach(var hit in hits)
            {
                if(!hit.Bounds.Contains(point))continue;
                switch(hit.Kind)
                {
                    case HitKind.Tree: Model.SelectDiscipline(hit.Index);railFocus=false;break;
                    case HitKind.Skill: Model.SelectSkill(hit.Index);railFocus=false;break;
                    case HitKind.Toggle:Model.Overview=!Model.Overview;break;
                    case HitKind.Review:Review();break;
                    case HitKind.Assign:BeginAssign();break;
                    case HitKind.Cancel:Model.Cancel();assigning=false;break;
                    case HitKind.Buy:Model.ConfirmPurchase();break;
                    case HitKind.Slot:slotIndex=hit.Index;break;
                    case HitKind.Bind:Model.Assign(slotIndex);assigning=false;break;
                    case HitKind.Page:detailPage=Mathf.Clamp(detailPage+hit.Index,0,DetailPageCount-1);break;
                    case HitKind.Close:Close();break;
                }
                return true;
            }
            return false;
        }
        // Retain the shared legacy failure-message contract for callers and tests.
        static string FormatFailure(string display,BuySkillAction.Result r)=>SkillBrowserModel.Failure(display,r);
    }
}
