using CavesOfOoo.Core;
namespace CavesOfOoo.Skills
{
    public sealed class LongBlades_EnGarde : BaseSkillPart
    {
        public override string Name=>nameof(LongBlades_EnGarde);
        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)=>new ActivatedAbilitySpec
        {DisplayName="En Garde",Command="CommandEnGarde",Class="Skills",TargetingMode=AbilityTargetingMode.SelfCentered,Cooldown=12};
        public override bool OnCommand(SkillEventContext ctx)
        {
            var actor=ctx?.Attacker;
            if(actor==null||ctx.Zone?.GetEntityCell(actor)==null||actor.HasEffect<EnGardeEffect>()
                ||SkillCombatHelpers.FindEquippedWeaponOfClass(actor,"LongBlades")==null)return false;
            return actor.ApplyEffect(new EnGardeEffect(),actor,ctx.Zone);
        }
    }
}
