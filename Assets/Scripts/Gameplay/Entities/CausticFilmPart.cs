namespace CavesOfOoo.Core
{
    /// <summary>Visible four-turn acid film. Repeated overlap refreshes a floor, never stacks a dose.</summary>
    public sealed class CausticFilmPart : Part
    {
        public override string Name=>"CausticFilm";
        public float Corrosion=.35f;
        public Entity Creator;
        public override bool HandleEvent(GameEvent e)
        {
            if(e?.ID=="EntityEnteredCell")
            {
                var cell=e.GetParameter<Cell>("Cell");var zone=cell?.ParentZone;
                if(zone?.GetEntityCell(ParentEntity)==cell)Coat(e.GetParameter<Entity>("Actor"),zone);
            }
            return true;
        }
        public bool Coat(Entity target,Zone zone)
        {
            var cell=zone?.GetEntityCell(ParentEntity);
            if(cell==null||target==null||target==ParentEntity||!target.HasTag("Creature")
                ||target.SpatialZone!=zone||target.GetStatValue("Hitpoints")<=0||CombatSystem.IsDeathHandled(target))return false;
            bool contact=false;
            foreach(var occupied in zone.GetOccupiedCells(target))if(occupied==cell){contact=true;break;}
            if(!contact)return false;
            float amount=System.Math.Max(0,System.Math.Min(1,Corrosion));if(amount<=0)return false;
            var existing=target.GetEffect<AcidicEffect>();
            if(existing!=null){existing.Corrosion=System.Math.Max(existing.Corrosion,amount);return true;}
            return ObjectStatusMatrix.TryApply(new AcidicEffect(amount),target,Creator,zone);
        }
    }
}
