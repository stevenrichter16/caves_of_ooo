using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
namespace CavesOfOoo.Skills
{
    public sealed class LongBlades_Riposte : BaseSkillPart
    {
        public override string Name => nameof(LongBlades_Riposte);
        public static int Chance(int rank) => rank==1?45:rank==2?65:rank>=3?80:0;
        public override string DescribeRank(int rank) => Chance(rank)
            + "% counter chance after a full parry; the attack can miss.";
        public override bool AddSkill(Entity actor)
        {
            if(actor.GetPart<RiposteAllowancePart>()==null)actor.AddPart(new RiposteAllowancePart());
            return true;
        }
        public static void TryCounter(Entity actor,Entity opponent,Zone zone,System.Random rng,MeleeStrikeProvenance parent)
        {
            var skill=actor?.GetPart<LongBlades_Riposte>();if(skill==null)return;
            var budget=actor.GetPart<RiposteAllowancePart>();
            string reason=parent.CounterDepth>0 || parent.SuppressFollowups?"reactive_ancestor"
                :budget==null?"missing_allowance":budget.Spent&&!budget.ComparisonUncapped?"spent":budget.Reserved?"reserved"
                :!BladeDefense.CanReact(actor,opponent,zone)?"actor_or_contact_ineligible":null;
            var weapon=reason==null?BladeDefense.FindLongBlade(actor):null;
            if(reason==null && weapon==null)reason="no_held_blade";
            if(reason!=null) {Record("RiposteRejected",actor,opponent,parent,reason);return;}
            budget.Reserved=true;
            try
            {
                int chance=Chance(skill.Rank), roll=rng.Next(100);
                if(Diag.IsChannelEnabled("damage"))Diag.Record(category:"damage",kind:"RiposteRoll",actor:actor,target:opponent,
                    payload:new{parent.ActionId,parent.StrikeId,rank=skill.Rank,chance,roll});
                if(roll>=chance)return;
                // Keep the originally selected blade: a callback must not substitute another one.
                if(!BladeDefense.CanReact(actor,opponent,zone) || !BladeDefense.IsHeldWeapon(actor,weapon)
                    || !BladeDefense.HasToken(weapon.Attributes,"LongBlades") || actor.GetPart<LongBlades_Riposte>()!=skill)
                {Record("RiposteCancelled",actor,opponent,parent,"changed_before_issue");return;}
                budget.Spent=true;
                var counter=parent.Child(MeleeStrikeOrigin.ReactiveCounter,true);
                var result=CombatSystem.PerformSingleAttack(actor,opponent,weapon,true,zone,rng,"(Riposte)",counter);
                if(Diag.IsChannelEnabled("damage"))Diag.Record(category:"damage",kind:"RiposteIssued",actor:actor,target:opponent,
                    payload:new{counter.ActionId,counter.RootStrikeId,counter.ParentStrikeId,counter.StrikeId,counter.Depth,
                        weapon=weapon.ParentEntity.ID,outcome=result.Outcome.ToString(),result.ActualDamage});
            }
            finally {budget.Reserved=false;}
        }
        static void Record(string kind,Entity actor,Entity target,MeleeStrikeProvenance parent,string reason)
        {
            if(Diag.IsChannelEnabled("damage"))Diag.Record(category:"damage",kind:kind,actor:actor,target:target,
                payload:new{parent.ActionId,parent.StrikeId,reason});
        }
    }
}
