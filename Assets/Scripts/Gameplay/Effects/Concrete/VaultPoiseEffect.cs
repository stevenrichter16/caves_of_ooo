namespace CavesOfOoo.Core
{
    /// <summary>One adjacent melee accuracy opportunity, consumed at an actual hit roll.</summary>
    public sealed class VaultPoiseEffect : Effect
    {
        public override string DisplayName=>"vault poise";
        public int Charges=1;
        public VaultPoiseEffect(int duration=2){Duration=duration;}
        public override bool OnStack(Effect incoming)
        {if(!(incoming is VaultPoiseEffect))return false;Duration=2;Charges=1;return true;}
        public static int ConsumeHitBonus(Entity actor,Entity target,Zone zone)
        {
            var effect=actor?.GetEffect<VaultPoiseEffect>();
            if(effect==null||effect.Duration<=0||effect.Charges<=0||zone==null||actor.SpatialZone!=zone
                ||target==null||target==actor||target.SpatialZone!=zone||target.GetStatValue("Hitpoints")<=0
                ||actor.GetStatValue("Hitpoints")<=0||CombatSystem.IsDeathHandled(actor)||CombatSystem.IsDeathHandled(target)
                ||SpatialQuery.Distance(zone,actor,target)!=1)return 0;
            effect.Charges=0;actor.GetPart<StatusEffectsPart>()?.RemoveEffect(effect);return 2;
        }
    }
}
