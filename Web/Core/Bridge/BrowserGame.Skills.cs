using System;
using System.Linq;
using CavesOfOoo.Rendering;
namespace CavesOfOoo.Web
{
    public partial class BrowserGame
    {
        SkillBrowserModel skillModel;
        void EnsureSkills(){if(skillModel==null||skillModel.Actor!=player){skillModel=new SkillBrowserModel();skillModel.Open(player);}}
        object SkillsView()
        {
            EnsureSkills();
            return new {groups=skillModel.Disciplines.Select(d=>new{name=d.Name,rows=d.Rows.Select(r=>new{id=r.Class,name=r.DisplayName,description=r.Description,rank=r.OwnedRank,maxRank=r.MaxRank,cost=r.Cost,state=skillModel.StateLabel(r),eligible=r.PurchaseEligible}).ToArray()}).ToArray(),
                selected=skillModel.Selected.Class,details=skillModel.Details,requirements=skillModel.Requirements,summary=skillModel.ActivationSummary,
                pending=skillModel.PendingPurchase,rank=skillModel.PendingRank,cost=skillModel.PendingCost,warning=skillModel.PendingWarning,notice=skillModel.Notice,unit=skillModel.Unit,points=skillModel.Points};
        }
        void SkillAction(Intent a)
        {
            EnsureSkills();panel="skills";
            if(a.command=="select"){skillModel.Cancel();if(!skillModel.SelectClass(a.id))error="That discipline is not available.";}
            else if(a.command=="review"){if(skillModel.SelectClass(a.id)&&!skillModel.ReviewPurchase())error=skillModel.Notice;}
            else if(a.command=="buy") {if(skillModel.PendingPurchase!=a.id){error="Review the current purchase first.";return;}if(!skillModel.ConfirmPurchase())error=skillModel.Notice;}
            else if(a.command=="cancel")skillModel.Cancel();
        }
    }
}
