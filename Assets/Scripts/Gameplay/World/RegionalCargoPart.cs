namespace CavesOfOoo.Core
{
    /// <summary>Maintains the derived identity lookup for an authored sealed
    /// consignment acquired through native pickup or container retrieval. This
    /// observer never moves cargo, grants work, or generates a zone.</summary>
    public sealed class RegionalCargoPart : Part
    {
        public override string Name => "RegionalCargo";
        public override bool HandleEvent(GameEvent e)
        {
            if(e.ID=="Taken"&&ReferenceEquals(e.GetParameter<Entity>("Item"),ParentEntity))
                RegionalSituations.RegisterTakenCargo(ParentEntity,e.GetParameter<Entity>("Actor"));
            return true;
        }
    }
}
