using CavesOfOoo.Skills;
namespace CavesOfOoo.Core
{
    /// <summary>One paid parry opportunity. Environmental and nonadjacent harm never spend it.</summary>
    public sealed class EnGardeEffect : Effect
    {
        public override string DisplayName=>"en garde";
        public bool Spent;
        public EnGardeEffect(int duration=2){Duration=duration;}
        public override bool OnStack(Effect incoming)=>incoming is EnGardeEffect;
        public override void OnBeforeTakeDamage(Entity target,GameEvent e)
        {
            var damage=e?.GetParameter<Damage>("Damage");var source=e?.GetParameter<Entity>("Source");var zone=target?.SpatialZone;
            if(Spent||Duration<=0||damage==null||damage.Amount<=0||!damage.HasAttribute("Melee")
                ||source==null||source==target||zone==null||source.SpatialZone!=zone||source.GetStatValue("Hitpoints")<=0
                ||CombatSystem.IsDeathHandled(source)||SpatialQuery.Distance(zone,target,source)!=1
                ||SkillCombatHelpers.FindEquippedWeaponOfClass(target,"LongBlades")==null)return;
            Spent=true;damage.Amount=damage.Amount/2+damage.Amount%2;
            target.GetPart<StatusEffectsPart>()?.RemoveEffect(this);
            MessageLog.Add(target.GetDisplayName()+" turns the blow aside with a measured guard.");
        }
    }
}
