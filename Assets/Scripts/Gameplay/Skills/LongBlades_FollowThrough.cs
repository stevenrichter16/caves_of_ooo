using CavesOfOoo.Core;
namespace CavesOfOoo.Skills
{
    /// <summary>A normal selected sword strike. Only the victim's actual death opens an ordinary forward step.</summary>
    public sealed class LongBlades_FollowThrough : BaseSkillPart
    {
        public override string Name=>nameof(LongBlades_FollowThrough);
        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)=>new ActivatedAbilitySpec
        {DisplayName="Follow Through",Command="CommandFollowThrough",Class="Skills",TargetingMode=AbilityTargetingMode.AdjacentCell,Range=1,Cooldown=12};
        public override bool OnCommand(SkillEventContext ctx)
        {
            var actor=ctx?.Attacker;var weapon=SkillCombatHelpers.FindEquippedWeaponOfClass(actor,"LongBlades");
            if(weapon==null||ctx.Zone==null||ctx.Rng==null)return false;
            var target=SkillCombatHelpers.FindAdjacentSkillTarget(actor,ctx.Zone,ctx.TargetCell,out var contact);
            if(target==null||contact==null)return false;
            int x=contact.X,y=contact.Y;
            CombatSystem.PerformSingleAttack(actor,target,weapon,true,ctx.Zone,ctx.Rng,"(Follow Through)");
            if(CombatSystem.IsDeathHandled(target)&&actor.SpatialZone==ctx.Zone&&actor.GetStatValue("Hitpoints")>0&&!CombatSystem.IsDeathHandled(actor)
                && ctx.Zone.GetEntityCell(actor) is Cell current && System.Math.Max(System.Math.Abs(current.X-x),System.Math.Abs(current.Y-y))==1)
                MovementSystem.TryMoveTo(actor,ctx.Zone,x,y);
            return true;
        }
    }
}
