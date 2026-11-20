using System;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Web
{
    public partial class BrowserGame
    {
        sealed class AbilityPlan
        {
            public string Token,Command;public Entity Actor,VaultTarget;public Zone Zone;public ActivatedAbility Ability;
            public Cell Source,Selected,Landing;public AbilityTargetingMode Mode;public int Dx,Dy,Range;
            public bool Neutral;
        }
        AbilityPlan pendingAbility;
        AbilityPlan PlanAbility(Intent a,bool legacy=false)
        {
            var ability=OwnedAbility(a.id);var source=zone.GetEntityCell(player);
            if(ability==null||!ability.IsUsable||source==null){error="That ability is not ready.";return null;}
            if(IsThrowingAbility(ability)){error="Choose an item for this throwing skill first.";return null;}
            var plan=new AbilityPlan{Actor=player,Zone=zone,Ability=ability,Command=ability.Command,Mode=ability.TargetingMode,Range=ability.Range,Source=source};
            long dx=(long)a.x-source.X,dy=(long)a.y-source.Y,distance=Math.Max(Math.Abs(dx),Math.Abs(dy));
            switch(ability.TargetingMode)
            {
                case AbilityTargetingMode.SelfCentered:break;
                case AbilityTargetingMode.DirectionLine:
                    int ux=a.dx,uy=a.dy;
                    if(legacy)
                    {
                        if(distance<1||distance>ability.Range||(dx!=0&&dy!=0&&Math.Abs(dx)!=Math.Abs(dy)))
                        {error="Choose one of eight directions, not an off-axis impact tile.";return null;}
                        ux=Math.Sign(dx);uy=Math.Sign(dy);
                    }
                    if(ux<-1||ux>1||uy<-1||uy>1||(ux==0&&uy==0)){error="Choose a single direction.";return null;}
                    if(!zone.InBounds(source.X+ux,source.Y+uy)){error="That direction leads beyond the edge of the area.";return null;}
                    plan.Dx=ux;plan.Dy=uy;break;
                case AbilityTargetingMode.AdjacentCell:
                case AbilityTargetingMode.CursorCell:
                    var selected=zone.GetCell(a.x,a.y);
                    int range=ability.TargetingMode==AbilityTargetingMode.AdjacentCell?1:ability.Range;
                    if(distance<1||distance>range||selected?.IsVisible!=true){error="Choose a visible target within range.";return null;}
                    plan.Selected=selected;plan.Dx=Math.Sign(dx);plan.Dy=Math.Sign(dy);
                    if(ability.Command=="CommandVault")
                    {
                        if(!VaultTargeting.TryPlan(player,zone,a.x,a.y,out var landing,out var target,out _))
                        {error="Vault has no clear landing there.";return null;}
                        if(target!=null&&!ThrowTrajectory.IsKnownOwner(selected,target)){error="Choose a visible Vault target.";return null;}
                        plan.Landing=landing;plan.VaultTarget=target;plan.Neutral=target!=null&&!FactionManager.IsHostile(player,target);
                    }
                    break;
                default:error="That targeting mode is not available.";return null;
            }
            return plan;
        }
        object AbilityPreviewView()
        {
            var p=pendingAbility;if(p==null)return null;
            return new{token=p.Token,mode=p.Mode.ToString(),name=p.Ability.DisplayName,range=p.Range,direction=new{x=p.Dx,y=p.Dy},
                selected=p.Selected==null?null:new{x=p.Selected.X,y=p.Selected.Y},landing=p.Landing==null?null:new{x=p.Landing.X,y=p.Landing.Y},
                targetName=p.VaultTarget?.GetDisplayName(),requiresNeutralConfirmation=p.Neutral,details=AbilityText(p.Ability)};
        }
        void PreviewAbility(Intent a)
        {
            pendingAbility=null;var plan=PlanAbility(a);if(plan==null)return;
            plan.Token=Guid.NewGuid().ToString("N");pendingAbility=plan;panel="abilityPreview";
        }
        void ConfirmAbility(Intent a)
        {
            var old=pendingAbility;pendingAbility=null;
            if(old==null||string.IsNullOrEmpty(a.token)||a.token!=old.Token||old.Actor!=player||old.Zone!=zone||old.Source!=zone.GetEntityCell(player)||OwnedAbility(old.Ability.ID.ToString())!=old.Ability)
            {error="Review the ability again; its source or choice has changed.";panel="";return;}
            var current=PlanAbility(new Intent{id=old.Ability.ID.ToString(),x=old.Selected?.X??0,y=old.Selected?.Y??0,dx=old.Dx,dy=old.Dy});
            if(current==null||current.Command!=old.Command||current.Mode!=old.Mode||current.Range!=old.Range||current.VaultTarget!=old.VaultTarget||current.Landing!=old.Landing||current.Neutral!=old.Neutral)
            {error="Review the ability again; its target or landing has changed.";panel="";return;}
            if(current.Neutral&&!a.acceptNeutralAttack){error="Review and explicitly confirm the attack on this non-hostile target.";panel="";return;}
            ExecuteAbility(current);
        }
        void ExecuteAbility(AbilityPlan plan)
        {
            var ev=GameEvent.New(plan.Command);bool handled;
            try
            {
                ev.SetParameter("Zone",(object)zone);ev.SetParameter("RNG",(object)rng);ev.SetParameter("SourceCell",(object)plan.Source);
                ev.SetParameter("DirectionX",plan.Dx);ev.SetParameter("DirectionY",plan.Dy);ev.SetParameter("Range",plan.Range);
                if(plan.Selected!=null)ev.SetParameter("TargetCell",(object)plan.Selected);
                player.FireEvent(ev);handled=ev.Handled;
            }
            finally{ev.Release();}
            panel="";if(handled)EndTurn();else error="The ability could not resolve.";
        }
    }
}
