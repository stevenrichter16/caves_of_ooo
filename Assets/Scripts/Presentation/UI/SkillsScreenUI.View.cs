using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CavesOfOoo.Rendering
{
    public partial class SkillsScreenUI
    {
        enum HitKind { Tree,Skill,Toggle,Review,Assign,Cancel,Buy,Slot,Bind,Page,Close }
        struct Hit { public Rect Bounds; public HitKind Kind; public int Index; }
        readonly List<Hit> hits=new List<Hit>(40);
        readonly List<Image> images=new List<Image>(100);
        readonly List<SkillScreenText> labels=new List<SkillScreenText>(90);
        GameObject canvasRoot;
        RectTransform contentRoot;
        int imageUsed,labelUsed;
        static readonly Color Background=new Color(.018f,.044f,.040f),Panel=new Color(.028f,.073f,.065f),
            SelectedFill=new Color(.085f,.14f,.105f),Border=new Color(.21f,.38f,.31f),
            Ink=new Color(.86f,.88f,.76f),Muted=new Color(.57f,.69f,.60f),Gold=new Color(.97f,.77f,.33f),
            Green=new Color(.48f,.86f,.57f),Red=new Color(.96f,.55f,.44f);

        void EnsureView()
        {
            if(canvasRoot!=null)return;
            canvasRoot=new GameObject("FullScreenSkills",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
            canvasRoot.transform.SetParent(transform,false);
            var canvas=canvasRoot.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;
            var scaler=canvasRoot.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1280,800);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var backdrop=new GameObject("OpaqueBackground",typeof(RectTransform),typeof(Image));backdrop.transform.SetParent(canvasRoot.transform,false);
            var bg=backdrop.GetComponent<Image>();bg.color=Background;bg.raycastTarget=false;
            var r=bg.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            contentRoot=new GameObject("FieldManual",typeof(RectTransform)).GetComponent<RectTransform>();contentRoot.SetParent(canvasRoot.transform,false);
            contentRoot.anchorMin=contentRoot.anchorMax=new Vector2(.5f,.5f);contentRoot.sizeDelta=new Vector2(1280,800);
        }
        void Place(RectTransform r,Rect box)
        {
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);
            r.anchoredPosition=new Vector2(box.x,-box.y);r.sizeDelta=box.size;r.localRotation=Quaternion.identity;
        }
        Image Box(Rect rect,Color tint)
        {
            if(imageUsed==images.Count)
            {
                var obj=new GameObject("Panel",typeof(RectTransform),typeof(Image));obj.transform.SetParent(contentRoot,false);
                var image=obj.GetComponent<Image>();image.raycastTarget=false;images.Add(image);
            }
            var im=images[imageUsed++];im.gameObject.SetActive(true);im.transform.SetAsLastSibling();Place(im.rectTransform,rect);im.color=tint;return im;
        }
        void Frame(Rect rect,Color edge,Color fill)
        {Box(rect,edge);Box(new Rect(rect.x+1,rect.y+1,rect.width-2,rect.height-2),fill);}
        void Label(Rect rect,string text,Color tint,float height=20)
        {
            if(labelUsed==labels.Count)
            {
                var obj=new GameObject("GlyphText",typeof(RectTransform),typeof(SkillScreenText));obj.transform.SetParent(contentRoot,false);
                var label=obj.GetComponent<SkillScreenText>();label.raycastTarget=false;labels.Add(label);
            }
            var t=labels[labelUsed++];t.gameObject.SetActive(true);t.transform.SetAsLastSibling();Place(t.rectTransform,rect);
            if(t.GlyphHeight!=height){t.GlyphHeight=height;t.SetVerticesDirty();}
            t.color=tint;t.Content=text;
        }
        void Button(Rect rect,string title,HitKind kind,int index=0,bool selected=false,bool enabled=true)
        {
            Frame(rect,selected?Gold:Border,selected?SelectedFill:Panel);
            Label(new Rect(rect.x+10,rect.y+Mathf.Max(2,(rect.height-18)/2),rect.width-20,18),SkillScreenText.Fit(title,Mathf.FloorToInt((rect.width-20)/9),1),enabled?(selected?Gold:Ink):Muted,18);
            if(enabled)hits.Add(new Hit{Bounds=rect,Kind=kind,Index=index});
        }
        void EndPools()
        {
            for(int i=imageUsed;i<images.Count;i++)images[i].gameObject.SetActive(false);
            for(int i=labelUsed;i<labels.Count;i++)labels[i].gameObject.SetActive(false);
        }
        void Render()
        {
            if(!IsOpen || hidden)return;
            EnsureView();RenderCount++;lastController=NativeGamepadInput.IsConnected;
            imageUsed=labelUsed=0;hits.Clear();
            Label(new Rect(24,22,470,32),"SKILLS / FIELD MANUAL",Gold,28);
            Label(new Rect(24,61,750,20),"Know your tools. Choose what you become.",Muted,18);
            Button(new Rect(828,24,286,40),lastController?"[Y] "+(Model.Overview?"Field manual":"Overview"):"[Tab] "+(Model.Overview?"Field manual":"Overview"),HitKind.Toggle);
            Label(new Rect(1140,28,115,28),Model.Points+" SP",Gold,26);
            Frame(new Rect(24,98,224,596),railFocus?Gold:Border,Panel);
            Frame(new Rect(264,98,482,596),railFocus?Border:Gold,Panel);
            Frame(new Rect(762,98,494,596),Border,Panel);
            Label(new Rect(38,113,202,20),"DISCIPLINES",Muted,18);
            int treeStart=Model.DisciplineIndex/13*13;
            for(int i=treeStart;i<Mathf.Min(Model.Disciplines.Count,treeStart+13);i++)
            {
                Rect r=new Rect(32,150+(i-treeStart)*39,208,36);bool selected=i==Model.DisciplineIndex;
                if(selected)Box(r,SelectedFill);
                string prefix=selected?"> ":"  ";
                Label(new Rect(r.x+5,r.y+8,r.width-8,24),prefix+Model.Disciplines[i].Name,selected?Gold:Ink,18);
                hits.Add(new Hit{Bounds=r,Kind=HitKind.Tree,Index=i});
            }
            Label(new Rect(38,667,202,20),lastController?"LB / RB  discipline":"Q / E  discipline",Muted,18);
            if(Model.Current!=null)
            {
                Label(new Rect(280,112,450,27),Model.Current.Name.ToUpperInvariant(),Ink,24);
                RenderSkills();RenderDetails();
            }
            else Label(new Rect(284,170,430,120),"No skills available.",Muted);
            Label(new Rect(24,708,1225,22),SkillScreenText.Fit(Model.Notice,122,1),Green,20);
            Button(new Rect(24,744,226,36),Model.Selected.State==SkillsScreenRowState.Owned?"Already learned":lastController?"[A] Review purchase":"[Enter] Review",HitKind.Review,enabled:Model.Selected.State!=SkillsScreenRowState.Owned);
            Button(new Rect(266,744,266,36),Model.CanAssign?(lastController?"[X] Assign to hotbar":"[I] Assign to hotbar"):"No active to assign",HitKind.Assign,enabled:Model.CanAssign);
            Button(new Rect(1132,744,124,36),lastController?"[B] Back":"[Esc] Back",HitKind.Close);
            Label(new Rect(552,744,564,45),lastController?"D-pad / LS: browse\nRS: details pages":"Arrows: browse   PgUp/PgDn: details\nClick: select   Wheel: browse",Muted,18);
            if(IsReviewingPurchase)RenderReview();else if(assigning)RenderAssignment();
            EndPools();
        }
        void RenderSkills()
        {
            var rows=Model.Current.Rows;int index=Model.SkillIndex;
            SkillCard(new Rect(280,150,450,61),0,false);
            int page=index<=0?0:(index-1)/CardsPerPage;
            int first=1+page*CardsPerPage;
            if(Model.Overview)
            {
                // Trace only the selected card's known prerequisites. Missing/off-page
                // prerequisites stay in the text panel instead of implying new edges.
                if(index>0 && !Model.Selected.IsObfuscated)
                {
                    Rect target=CardRect(index-first);var end=new Vector2(target.center.x,target.y);
                    foreach(string prerequisite in Model.Prerequisites)
                    {
                        if(prerequisite==rows[0].Class)
                        {
                            // The gutter route cannot look like a chain through an unrelated card.
                            float joinY=target.center.y;
                            Link(new Vector2(505,211),new Vector2(505,joinY));
                            Link(new Vector2(505,joinY),new Vector2(target.x<505?target.xMax:target.x,joinY));
                        }
                        else for(int i=first;i<Mathf.Min(rows.Count,first+CardsPerPage);i++)
                            if(rows[i].Class==prerequisite && !rows[i].IsObfuscated)
                            {var from=CardRect(i-first);Link(new Vector2(from.center.x,from.yMax),end);}
                    }
                }
                for(int i=first;i<Mathf.Min(rows.Count,first+CardsPerPage);i++)SkillCard(CardRect(i-first),i,true);
            }
            else for(int i=first;i<Mathf.Min(rows.Count,first+CardsPerPage);i++)SkillCard(new Rect(280,224+(i-first)*53,450,49),i,false);
            int pages=Mathf.Max(1,(rows.Count-1+CardsPerPage-1)/CardsPerPage);
            Label(new Rect(282,668,448,20),(Model.Overview?"OVERVIEW":"POWERS")+"  "+(page+1)+" / "+pages+(Model.Overview?"   selected links":""),Muted,18);
        }
        Rect CardRect(int local)=>new Rect(280+(local%2)*234,230+(local/2)*108,216,96);
        void Link(Vector2 a,Vector2 b)
        {
            var im=Box(new Rect(a.x,a.y,(b-a).magnitude,2),Gold);
            im.rectTransform.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);
        }
        void SkillCard(Rect r,int i,bool board)
        {
            var row=Model.Current.Rows[i];bool selected=i==Model.SkillIndex;
            Frame(r,selected?Gold:Border,selected?SelectedFill:Background);
            string name=(selected?"> ":"")+row.DisplayName;
            Label(new Rect(r.x+10,r.y+7,r.width-18,board?49:24),SkillScreenText.Fit(name,board?19:43,board?2:1),selected?Gold:Ink,20);
            string state=Model.StateLabel(row)+(row.State==SkillsScreenRowState.Owned || row.Cost<0?"":"  "+row.Cost+" SP");
            Label(new Rect(r.x+10,r.yMax-23,r.width-18,20),state,row.State==SkillsScreenRowState.Owned?Green:row.State==SkillsScreenRowState.Buyable?Muted:Red,18);
            hits.Add(new Hit{Bounds=r,Kind=HitKind.Skill,Index=i});
        }
        void RenderDetails()
        {
            var row=Model.Selected;
            Label(new Rect(782,114,453,65),SkillScreenText.Page(SkillScreenText.Wrap(row.DisplayName.ToUpperInvariant(),32),0,2),Gold,28);
            string state=Model.StateLabel(row);
            if(row.State!=SkillsScreenRowState.Owned && row.Cost>=0)state+="  /  "+row.Cost+" SP";
            else if(Model.CanAssign)state+="  /  SLOT "+(Model.AssignedSlot<0?"UNBOUND":AbilityManagerStateBuilder.SlotToHotkey(Model.AssignedSlot).ToString());
            Label(new Rect(782,187,454,24),state,row.State==SkillsScreenRowState.Owned?Green:row.State==SkillsScreenRowState.Buyable?Muted:Red,20);
            Label(new Rect(782,210,454,20),Model.ActivationSummary,Muted,18);
            string source=Model.Requirements.Length>0?"REQUIREMENTS TO PURCHASE\n"+Model.Requirements+"\n\n":"";
            string details=Model.Details;
            int newline=details.IndexOf('\n');
            if(newline>0 && row.DisplayName.Length<=64) details=details.Substring(newline).TrimStart();
            source+=details;
            if(detailClass!=row.Class || detailSource!=source)
            {
                detailClass=row.Class;detailSource=source;detailPage=0;detailLines.Clear();detailLines.AddRange(SkillScreenText.Wrap(source,45));
            }
            Label(new Rect(782,242,454,408),SkillScreenText.Page(detailLines,detailPage,DetailLines),Ink,20);
            Button(new Rect(782,658,61,27),"<",HitKind.Page,-1);
            Label(new Rect(858,663,277,22),"DETAILS "+(detailPage+1)+" / "+DetailPageCount,Muted,18);
            Button(new Rect(1175,658,61,27),">",HitKind.Page,1);
        }
        void RenderReview()
        {
            hits.Clear();Box(new Rect(0,0,1280,800),new Color(0,0,0,.85f));
            Frame(new Rect(286,204,708,374),Gold,Panel);
            Label(new Rect(310,228,660,30),"LEARN THIS SKILL?",Gold,28);
            Label(new Rect(310,286,660,65),SkillScreenText.Page(SkillScreenText.Wrap(Model.Selected.DisplayName,47),0,2),Ink,28);
            Label(new Rect(310,371,660,70),"Cost: "+Model.Selected.Cost+" SP\nAvailable: "+Model.Points+" -> "+(Model.Points-Model.Selected.Cost)+" SP",Ink,24);
            Label(new Rect(310,459,660,24),lastController?"Left / Right: choose   A: confirm   B: cancel":"Left / Right: choose   Enter: confirm   Esc: cancel",Muted,20);
            Button(new Rect(310,511,290,42),lastController?"[A] Cancel":"Cancel",HitKind.Cancel,selected:!learnSelected);
            Button(new Rect(624,511,346,42),lastController?"[A] Learn":"Learn",HitKind.Buy,selected:learnSelected);
        }
        void RenderAssignment()
        {
            hits.Clear();Box(new Rect(0,0,1280,800),new Color(0,0,0,.85f));
            Frame(new Rect(256,82,768,636),Gold,Panel);
            Label(new Rect(280,104,720,28),"ASSIGN TO HOTBAR",Gold,28);
            Label(new Rect(280,147,720,50),SkillScreenText.Page(SkillScreenText.Wrap(Model.Selected.DisplayName,60),0,2),Ink,24);
            for(int i=0;i<10;i++)Button(new Rect(280,204+i*39,720,35),AbilityManagerStateBuilder.SlotToHotkey(i)+"  "+Model.SlotName(i),HitKind.Slot,i,i==slotIndex);
            Label(new Rect(280,601,720,51),"Moves this ability here; replaces the current slot.\nDoes not activate or spend a turn.",Muted,20);
            Button(new Rect(280,658,254,40),lastController?"[B] Cancel":"Cancel",HitKind.Cancel);
            Button(new Rect(556,658,444,40),lastController?"[A] Assign to selected slot":"Assign to selected slot",HitKind.Bind,0,true);
        }
    }
}
