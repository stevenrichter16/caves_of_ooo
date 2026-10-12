using System;
using CavesOfOoo.Core;
namespace CavesOfOoo.Skills
{
    /// <summary>Shared read-only cursor and execution geometry. No turn, RNG, or mutation.</summary>
    public static class VaultTargeting
    {
        public static bool TryPlan(Entity actor,Zone zone,int x,int y,out Cell landing,out Entity target,out string reason)
        {
            landing=null;target=null;reason="invalid_context";
            var source=zone?.GetEntityCell(actor);
            if(source==null||actor.GetStatValue("Hitpoints")<=0||CombatSystem.IsDeathHandled(actor)||!zone.InBounds(x,y))return false;
            int dx=x-source.X,dy=y-source.Y,distance=Math.Max(Math.Abs(dx),Math.Abs(dy));
            if(distance<1||distance>Acrobatics_Vault.VAULT_DISTANCE){reason="out_of_range";return false;}
            var selected=zone.GetCell(x,y);
            foreach(var e in selected.Occupants)
                if(e!=actor&&e.HasTag("Creature")&&e.GetStatValue("Hitpoints")>0&&!CombatSystem.IsDeathHandled(e)){target=e;break;}
            int lx=target==null?x:x-Math.Sign(dx),ly=target==null?y:y-Math.Sign(dy);
            int moveX=lx-source.X,moveY=ly-source.Y;
            if(Math.Max(Math.Abs(moveX),Math.Abs(moveY))==2)
                foreach(var cell in zone.GetOccupiedCells(actor,source.X+Math.Sign(moveX),source.Y+Math.Sign(moveY)))
                    if(cell?.HasClosedArchiveBarrier()==true){reason="sealed_barrier";return false;}
            landing=zone.GetCell(lx,ly);
            if(landing==null||landing.HasClosedArchiveBarrier()||!zone.CanPlaceFootprint(actor,lx,ly)
                ||MultiCellAbilityQueries.CreatureAtPlacement(zone,actor,lx,ly)!=null)
            {landing=null;reason="landing_blocked";return false;}
            reason=null;return true;
        }
    }
}
