using System;

namespace CavesOfOoo.Core
{
    /// <summary>One committed utility action with current actor, vessel and contact evidence.
    /// Presentation only: never permission to mutate, reveal or replay an action.</summary>
    public readonly struct WorldAudioUtilityReceipt
    {
        public readonly Entity Actor,Vessel,Source;
        public readonly Zone Zone;
        public readonly Point Contact;
        public readonly WorldAudioCue Cue;
        internal WorldAudioUtilityReceipt(Entity actor,Entity vessel,Entity source,Zone zone,Point contact,WorldAudioCue cue)
        {Actor=actor;Vessel=vessel;Source=source;Zone=zone;Contact=contact;Cue=cue;}
    }
    public static class WorldAudioUtility
    {
        public static Action<WorldAudioUtilityReceipt> Callback {get;set;}
        internal static bool IsWater(WorldAudioCue cue)=>cue==WorldAudioCue.WaterFill
            ||cue==WorldAudioCue.WaterDrink||cue==WorldAudioCue.WaterPour;
        internal static Capture Take(Entity actor,Entity vessel,Zone zone,WorldAudioCue cue,
            Entity source,bool sourceCarried,Func<bool> committed)
        {
            if(Callback==null||actor==null||vessel==null||zone==null||!IsWater(cue)||committed==null)return null;
            var capture=new Capture(actor,vessel,zone,cue,source,sourceCarried,committed);
            return capture.Current()?capture:null;
        }
        internal sealed class Capture
        {
            readonly Entity actor,vessel,source;
            readonly Zone zone;readonly Cell origin,contact,sourceAnchor;
            readonly PhysicsPart actorPhysics,vesselPhysics,sourcePhysics;
            readonly RenderPart actorRender,vesselRender,sourceRender;
            readonly InventoryPart inventory;
            readonly WorldAudioCue cue;readonly bool sourceCarried;
            readonly Func<bool> committed;bool published;
            internal Capture(Entity actor,Entity vessel,Zone zone,WorldAudioCue cue,Entity source,bool sourceCarried,Func<bool> committed)
            {
                this.actor=actor;this.vessel=vessel;this.zone=zone;this.cue=cue;this.source=source;
                this.sourceCarried=sourceCarried;this.committed=committed;
                origin=zone.GetEntityCell(actor);sourceAnchor=source==null?null:zone.GetEntityCell(source);
                contact=source==null||sourceCarried?origin:(origin==null?null:SpatialQuery.ClosestCell(zone,source,origin.X,origin.Y));
                inventory=actor.GetPart<InventoryPart>();actorPhysics=actor.GetPart<PhysicsPart>();actorRender=actor.GetPart<RenderPart>();
                vesselPhysics=vessel.GetPart<PhysicsPart>();vesselRender=vessel.GetPart<RenderPart>();
                sourcePhysics=source?.GetPart<PhysicsPart>();sourceRender=source?.GetPart<RenderPart>();
            }
            bool Carried(Entity item,PhysicsPart physical,RenderPart render)
                =>item!=null&&item.SpatialZone==null&&physical!=null&&physical.ParentEntity==item&&item.GetPart<PhysicsPart>()==physical
                    &&physical.Takeable&&physical.InInventory==actor&&physical.Equipped==null
                    &&render!=null&&render.ParentEntity==item&&item.GetPart<RenderPart>()==render&&render.Visible
                    &&inventory.Objects.Contains(item)&&!inventory.EquippedItems.ContainsValue(item)
                    &&inventory.FindEquippedBodyPart(item)==null&&(item.GetPart<StackerPart>()?.StackCount??1)==1;
            internal bool Current()
            {
                if(origin==null||contact==null||origin.ParentZone!=zone||contact.ParentZone!=zone
                    ||actor.SpatialZone!=zone||zone.GetEntityCell(actor)!=origin||!origin.Objects.Contains(actor)
                    ||CombatSystem.IsDeathHandled(actor)||actor.GetStatValue("Hitpoints",0)<=0
                    ||actor.GetPart<PhysicsPart>()!=actorPhysics||actorPhysics?.ParentEntity!=actor
                    ||actorPhysics.InInventory!=null||actorPhysics.Equipped!=null
                    ||actor.GetPart<RenderPart>()!=actorRender||actorRender?.ParentEntity!=actor||!actorRender.Visible
                    ||!origin.Explored||!origin.IsVisible||!contact.Explored||!contact.IsVisible
                    ||actor.GetPart<InventoryPart>()!=inventory||inventory?.ParentEntity!=actor
                    ||!Carried(vessel,vesselPhysics,vesselRender))return false;
                if(source==null)return true;
                if(sourceCarried)return source!=vessel&&Carried(source,sourcePhysics,sourceRender);
                return source.SpatialZone==zone&&sourceAnchor!=null&&zone.GetEntityCell(source)==sourceAnchor&&sourceAnchor.Objects.Contains(source)
                    &&contact.Occupants.Contains(source)&&SpatialQuery.Distance(zone,actor,source)<=1
                    &&(!source.HasPart<SpatialFootprintPart>()||zone.IsFootprintCurrent(source))
                    &&source.GetPart<PhysicsPart>()==sourcePhysics
                    &&(sourcePhysics==null||(sourcePhysics.ParentEntity==source&&!sourcePhysics.Takeable
                        &&sourcePhysics.InInventory==null&&sourcePhysics.Equipped==null))
                    &&source.GetPart<RenderPart>()==sourceRender&&sourceRender?.ParentEntity==source&&sourceRender.Visible
                    &&!source.HasTag("Creature");
            }
            internal void Publish()
            {
                if(published)return;published=true;
                try{if(Current()&&committed())Callback?.Invoke(new WorldAudioUtilityReceipt(actor,vessel,source,zone,
                    new Point(contact.X,contact.Y),cue));}
                catch(Exception){/* Optional presentation never changes a committed action. */}
            }
        }
    }
}
