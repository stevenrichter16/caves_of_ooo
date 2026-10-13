using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class ComposableBuildComparisonBench
    {
        static string Act(Trial t)
        {
            var p=t.Player;var z=t.Zone;var cell=z.GetEntityCell(p);
            var tonic=p.GetPart<InventoryPart>().Objects.FirstOrDefault(e=>e.BlueprintName=="HealingTonic");
            if(p.GetStatValue("Hitpoints")<=14&&tonic!=null&&tonic.GetPart<TonicPart>().ApplyTo(p,p,z,t.Rng,consumeItem:true))
            {t.Result.Tonics++;return "tonic";}
            if(p.GetStatValue("Hitpoints")<=8&&tonic==null)t.Retreating=true;
            if(t.Retreating)
            {
                if(t.Result.Vault&&TrySafeVault(t,cell.X-2,cell.Y))return "vault-retreat";
                if(Walk(t,cell.X-1,cell.Y))return "walk-retreat";
            }
            bool threatened=Threatened(t,cell.X,cell.Y);
            // Shared walking control takes precedence over spending a skill.
            if(threatened&&t.Result.Scene!=Encounter.HeldEntrance)
            {
                foreach(var step in new[]{(0,-1),(0,1),(-1,0),(-1,-1),(-1,1),(1,-1),(1,1)})
                    if(SafeCell(t,cell.X+step.Item1,cell.Y+step.Item2)&&Walk(t,cell.X+step.Item1,cell.Y+step.Item2))return "walk-off-ray";
            }
            Entity nearest=null;int distance=int.MaxValue;
            foreach(var enemy in t.Enemies)
                if(Alive(enemy,z)&&AIHelpers.TryGetVisibleTargetCell(p,enemy,z,10,out _))
                {int d=SpatialQuery.Distance(z,p,enemy);if(d<distance){distance=d;nearest=enemy;}}
            if(nearest==null)
            {
                // Fixed known fixture entrance, never a hidden enemy coordinate.
                if(t.Result.Scene!=Encounter.HeldEntrance&&Walk(t,cell.X+Math.Sign(14-cell.X),cell.Y+Math.Sign(10-cell.Y)))return "approach-entrance";
                return "wait";
            }
            var target=z.GetEntityCell(nearest);int dx=Math.Sign(target.X-cell.X),dy=Math.Sign(target.Y-cell.Y);
            if(t.Result.Scene==Encounter.MixedLane&&distance==1&&nearest.GetPart<CombatTacticsPart>()==null)
            {
                foreach(var enemy in t.Enemies)
                {
                    if(!Alive(enemy,z)||enemy.GetPart<CombatTacticsPart>()==null||!AIHelpers.TryGetVisibleTargetCell(p,enemy,z,10,out var casterCell))continue;
                    int after=Math.Max(Math.Abs(target.X-casterCell.X),Math.Abs(target.Y-casterCell.Y));
                    if(after>=2&&after<SpatialQuery.Distance(z,p,enemy)&&Command(t,"CommandTumble",target,dx,dy))return "tumble";
                }
            }
            if(distance<=2&&(threatened||distance==1)&&Command(t,"CommandJetBlast",target,dx,dy))return "jet-blast";
            if(distance==1&&Command(t,"CommandEnGarde",cell,0,0))return "guard";
            if(threatened&&distance==1&&!Threatened(t,target.X,target.Y)&&Command(t,"CommandTumble",target,dx,dy))return "tumble";
            // Pull only a visible caster at two cells when it is isolated. A failed
            // nonadjacent pull is never selected merely to fill a cooldown slot.
            if(distance==2&&nearest.GetPart<CombatTacticsPart>()!=null&&t.Enemies.Count(e=>Alive(e,z))==1
                &&Command(t,"CommandUndertow",target,dx,dy))return "undertow";
            if(distance==2&&(dx==0||dy==0||Math.Abs(target.X-cell.X)==Math.Abs(target.Y-cell.Y))&&Command(t,"CommandLunge",target,dx,dy))return "lunge";
            if(distance==2&&t.Result.Vault&&target.IsVisible&&VaultTargeting.TryPlan(p,z,target.X,target.Y,out var landing,out _,out _)
                &&!Threatened(t,landing.X,landing.Y)&&Command(t,"CommandVault",target,dx,dy))return "vault-attack";
            if(distance==1){Require(CombatSystem.PerformMeleeAttack(p,nearest,z,t.Rng),"ordinary melee commits");return "melee";}
            if(t.Result.Scene==Encounter.HeldEntrance)return "hold-entrance";
            if(Walk(t,cell.X+dx,cell.Y+dy))return "walk-approach";
            // One deterministic local detour around actual opaque cover.
            foreach(var step in new[]{(0,-1),(0,1),(1,0),(-1,0)})
                if(SafeCell(t,cell.X+step.Item1,cell.Y+step.Item2)&&Walk(t,cell.X+step.Item1,cell.Y+step.Item2))return "walk-around-cover";
            return "wait";
        }
        static bool Command(Trial t,string command,Cell target,int dx,int dy)
        {
            // No trial-and-error casting of unknown skills or cooling abilities.
            var ability=t.Player.GetPart<ActivatedAbilitiesPart>().AbilityList.FirstOrDefault(a=>a.Command==command);
            if(ability==null||!ability.IsUsable)return false;
            bool done=t.Player.GetPart<SkillsPart>().TryRouteSkillCommand(command,t.Zone,t.Rng,dx,dy,t.Zone.GetEntityCell(t.Player),target,ability.Range,out bool blocks);
            if(blocks)
            {
                // SpellSkillPart resolves gameplay synchronously, then requests
                // a presentation-only wait. The detached lab has no renderer.
                Require(done&&(command=="CommandJetBlast"||command=="CommandUndertow"),"unsupported pending action");
                Require(SpellFxBus.Drain().Count>0,"successful spell captured its visual sequence");
            }
            return done;
        }
        static bool Threatened(Trial t,int x,int y)
        {
            foreach(var enemy in t.Enemies)
                if(Alive(enemy,t.Zone)&&AIHelpers.TryGetVisibleTargetCell(t.Player,enemy,t.Zone,10,out _)
                    &&enemy.GetPart<CommittedMeleePart>()?.ThreatensCell(t.Zone,x,y)==true)return true;
            return false;
        }
        static bool SafeCell(Trial t,int x,int y)=>t.Zone.GetCell(x,y)?.IsVisible==true&&!Threatened(t,x,y)
            &&t.Zone.CanPlaceFootprint(t.Player,x,y)&&MultiCellAbilityQueries.CreatureAtPlacement(t.Zone,t.Player,x,y)==null;
        static bool Walk(Trial t,int x,int y)=>MovementSystem.TryMoveTo(t.Player,t.Zone,x,y);
        static bool TrySafeVault(Trial t,int x,int y)=>SafeCell(t,x,y)
            &&VaultTargeting.TryPlan(t.Player,t.Zone,x,y,out _,out var target,out _)&&target==null
            &&Command(t,"CommandVault",t.Zone.GetCell(x,y),-1,0);
    }
}
