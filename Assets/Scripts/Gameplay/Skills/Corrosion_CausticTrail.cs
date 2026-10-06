using System;
using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Skills
{
    /// <summary>A short-lived, noncollectible contact hazard; no new liquid resource.</summary>
    public sealed class Corrosion_CausticTrail : SpellSkillPart
    {
        public override string Name => nameof(Corrosion_CausticTrail);
        public override ActivatedAbilitySpec DeclareActivatedAbility(Entity actor) => new ActivatedAbilitySpec
        { DisplayName="Caustic Trail",Command="CommandCausticTrail",Class="Corrosion",TargetingMode=AbilityTargetingMode.DirectionLine,Range=3,Cooldown=16 };
        protected override bool ResolveSpell(SkillEventContext ctx)
        {
            var actor=ctx?.Attacker;var zone=ctx?.Zone;var origin=zone?.GetEntityCell(actor);
            if(origin==null || (ctx.DirectionX==0&&ctx.DirectionY==0) || MaterialReactionResolver.Factory==null)return false;
            var cells=SkillLine.CollectCells(zone,actor,origin.X,origin.Y,Math.Sign(ctx.DirectionX),Math.Sign(ctx.DirectionY),3);
            var coated=new HashSet<Entity>();int placed=0;
            foreach(var p in cells)
            {
                var cell=zone.GetCell(p.X,p.Y);Entity film=null;
                foreach(var occupant in cell.Occupants)
                    if(occupant?.BlueprintName=="CausticFilm"&&occupant.GetPart<CausticFilmPart>()!=null){film=occupant;break;}
                if(film==null)
                {
                    film=MaterialReactionResolver.Factory.CreateEntity("CausticFilm");
                    if(film?.GetPart<CausticFilmPart>()==null||film.GetPart<LifespanPart>()==null||film.SpatialZone!=null)continue;
                    if(!zone.AddEntity(film,p.X,p.Y))continue;
                }
                film.GetPart<LifespanPart>().TurnsRemaining=Math.Max(4,film.GetPart<LifespanPart>().TurnsRemaining);
                var contact=film.GetPart<CausticFilmPart>();contact.Creator=actor;placed++;
                SpellFxCapture.AffectCell(zone,p.X,p.Y);
                foreach(var victim in MultiCellAbilityQueries.SnapshotOccupants(new[]{cell},film))
                    if(coated.Add(victim))contact.Coat(victim,zone);
            }
            if(placed==0)return false;
            MessageLog.Add(actor.GetDisplayName()+" paints a short lane with caustic film.");return true;
        }
    }
}
