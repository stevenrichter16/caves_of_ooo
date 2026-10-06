using System;
using CavesOfOoo.Core;
namespace CavesOfOoo.Skills
{
    /// <summary>Spend one real acidic primer for immediate damage; dry first bodies stop the attempt.</summary>
    public sealed class Corrosion_CausticDraw : SpellSkillPart
    {
        public override string Name=>nameof(Corrosion_CausticDraw);
        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor)=>new ActivatedAbilitySpec
        {DisplayName="Caustic Draw",Command="CommandCausticDraw",Class="Corrosion",TargetingMode=AbilityTargetingMode.DirectionLine,Range=4,Cooldown=20};
        protected override bool ResolveSpell(SkillEventContext ctx)
        {
            var actor=ctx?.Attacker;var zone=ctx?.Zone;var cell=zone?.GetEntityCell(actor);
            if(cell==null||(ctx.DirectionX==0&&ctx.DirectionY==0))return false;
            var hits=SkillLine.Collect(zone,actor,cell.X,cell.Y,Math.Sign(ctx.DirectionX),Math.Sign(ctx.DirectionY),4);
            var target=hits.Count>0?hits[0]:null;var acid=target?.GetEffect<AcidicEffect>();
            if(acid==null||acid.Corrosion<=0)return false;
            int amount=4+(int)Math.Floor(8*Math.Min(1,acid.Corrosion));
            int landed=SpellDamageHelpers.ApplySpellDamage(target,amount,"Acid",actor,zone);
            target.GetPart<StatusEffectsPart>()?.RemoveEffect(acid);
            MessageLog.Add(actor.GetDisplayName()+" draws the corrosion inward for "+landed+" acid damage.");return true;
        }
    }
}
