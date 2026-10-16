using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>Known-state presentation, never an execution plan or paid RNG result.</summary>
    public sealed class ThrowPreview
    {
        public ThrowTraceResult Trace { get; private set; }
        public List<Cell> Splash { get; } = new List<Cell>(9);
        public string Description { get; private set; }
        public Cell ConditionalPush { get; private set; }
        public static ThrowPreview Build(Zone zone,Entity actor,Entity item,ThrowIntent intent,int range,bool focused,bool driving,bool measured=false)
        {
            if(intent.Kind==ThrowAimKind.Entity && !ThrowTrajectory.IsKnownOwner(zone?.GetCell(intent.X,intent.Y),intent.Target))
                intent=ThrowIntent.Ground(intent.X,intent.Y);
            var p=new ThrowPreview{Trace=ThrowTrajectory.Trace(zone,actor,item,intent,range,true)};
            bool tonic=item?.GetPart<TonicPart>()?.HasThrowablePayload()==true;
            if(!p.Trace.Uncertain && tonic && p.Trace.BurstCell!=null)
                ThrowPayloadResolver.CollectFootprint(zone,p.Trace.BurstCell,focused,p.Splash,true);
            string aim=intent.Kind==ThrowAimKind.Entity?"Body: "+intent.Target.GetDisplayName():"Ground";
            string cost=measured?"1 action now / item kept; throw next action":"1 item / 1 action";
            if(p.Trace.Uncertain)p.Description=aim+" / "+cost+". Route continues into unseen space; impact and spread uncertain.";
            else
            {
                string contact=p.Trace.AmbiguousContact?"indistinguishable obstruction":p.Trace.HitEntity!=null?p.Trace.HitEntity.GetDisplayName():p.Trace.BlockedBySolid?"cover":"ground";
                string disposition=tonic?"shatters":item?.GetPart<GasGrenadePart>()!=null?"detonates":"rests";
                var rest=p.Trace.BurstCell;
                p.Description=aim+" / "+cost+". First contact: "+contact+"; "+disposition
                    +(rest==null?" (no safe floor)":" at ["+rest.X+","+rest.Y+"]")+".";
                if(tonic)p.Description+=" "+(focused?"Focused":"Broad")+" initial splash; later reactions can spread. "+ThrowPayloadResolver.DescribeGroundPolicy(item);
                foreach(var splash in p.Splash)
                    if(!splash.IsVisible || !splash.Explored){p.Description+=" Unseen splash cells are uncertain.";break;}
                if(p.Trace.HitEntity?.HasTag("Creature")==true && !FactionManager.IsHostile(actor,p.Trace.HitEntity))
                    p.Description+=" Friendly/neutral contact: effects still apply.";
                if(driving && p.Trace.HitEntity?.HasTag("Creature")==true)
                {
                    var at=zone.GetEntityCell(actor);var hit=zone.GetEntityCell(p.Trace.HitEntity);
                    if(at!=null && hit!=null)
                    {
                        var next=zone.GetCell(hit.X+Math.Sign(hit.X-at.X),hit.Y+Math.Sign(hit.Y-at.Y));
                        if(next?.IsVisible==true && next.Explored)p.ConditionalPush=next;
                    }
                    p.Description+=" Push only after damage to a surviving, unbraced target; destination may be blocked.";
                }
            }
            return p;
        }
    }
}
