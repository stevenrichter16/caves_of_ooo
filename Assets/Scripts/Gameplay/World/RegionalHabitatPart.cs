namespace CavesOfOoo.Core
{
    /// <summary>Local conservation receipt, not a global ecology simulation.
    /// A saved expected-owner list lives on the cargo; missing owners therefore
    /// never count as an empty, successfully preserved habitat.</summary>
    public sealed class RegionalHabitatPart : Part
    {
        public override string Name => "RegionalHabitat";
        public string InstanceId;
        public int OriginalX,OriginalY,InitialHP;
        public bool Disturbed;
        public override bool HandleEvent(GameEvent e)
        {
            if(e.ID=="Destroyed")Disturbed=true;
            if(e.ID=="TakeDamage")
            {
                var damage=e.GetParameter<Damage>("Damage");
                if(damage!=null&&damage.Amount>0)Disturbed=true;
            }
            return true;
        }
    }
}
