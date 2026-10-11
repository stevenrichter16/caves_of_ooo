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
        internal static bool IsUtility(WorldAudioCue cue)=>IsWater(cue)||cue==WorldAudioCue.CookPrepare||cue==WorldAudioCue.BrewPrepare||cue==WorldAudioCue.HandleSoft||cue==WorldAudioCue.HandleWood||cue==WorldAudioCue.HandleMetal||cue==WorldAudioCue.Page;
        internal static Capture TakeActor(Entity actor,Zone zone,WorldAudioCue cue,Func<bool> committed)
        {
            if(Callback==null||actor==null||zone==null||!IsUtility(cue)||committed==null)return null;
            var capture=new Capture(actor,null,zone,cue,null,false,committed);return capture.Current()?capture:null;
        }
        internal static Capture TakeStation(Entity actor,Entity source,Zone zone,WorldAudioCue cue,Func<bool> committed)
            =>source==null?null:Take(actor,null,zone,cue,source,false,committed);
        internal static Capture Take(Entity actor,Entity vessel,Zone zone,WorldAudioCue cue,
            Entity source,bool sourceCarried,Func<bool> committed)
        {
            if(Callback==null||actor==null||(vessel==null&&source==null)||zone==null||!IsUtility(cue)||committed==null)return null;
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
                vesselPhysics=vessel?.GetPart<PhysicsPart>();vesselRender=vessel?.GetPart<RenderPart>();
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
                    ||(vessel!=null&&!Carried(vessel,vesselPhysics,vesselRender)))return false;
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
            internal void PublishHandling(Entity item,WorldAudioCue fallback)
            {
                if(published)return;published=true;
                try
                {
                    if(!Current()||!committed()||item==null)return;
                    // A coin becomes currency, not a retained handled object.
                    if(item.BlueprintName=="GoldCoin")
                    {WorldAudioHooks.Emit(fallback,actor,zone,origin.X,origin.Y);return;}
                    var physical=item.GetPart<PhysicsPart>();var render=item.GetPart<RenderPart>();
                    if(physical?.ParentEntity!=item||!physical.Takeable||(render!=null&&(render.ParentEntity!=item||!render.Visible))
                        ||item.HasTag("Creature")||(item.GetPart<StackerPart>()?.StackCount??1)<=0)return;
                    bool carried=item.SpatialZone==null&&inventory.Objects.Contains(item)&&physical.InInventory==actor
                        &&physical.Equipped==null&&!InventorySystem.IsEquipped(actor,item);
                    bool equipped=item.SpatialZone==null&&!inventory.Objects.Contains(item)&&physical.InInventory==null
                        &&physical.Equipped==actor&&InventorySystem.IsEquipped(actor,item);
                    var ground=zone.GetEntityCell(item);
                    bool dropped=item.SpatialZone==zone&&ground==origin&&origin.Objects.Contains(item)
                        &&physical.InInventory==null&&physical.Equipped==null;
                    if(!carried&&!equipped&&!dropped)return;
                    // Existing nonvisual inventory items still provide generic action feedback.
                    // Absence is distinct from an explicitly hidden or foreign render part.
                    if(render==null){WorldAudioHooks.Emit(fallback,actor,zone,origin.X,origin.Y);return;}
                    var material=item.GetPart<MaterialPart>();WorldAudioCue chosen;
                    switch(material?.ParentEntity==item?material.MaterialID:null)
                    {
                        case "Leather":case "Cloth":chosen=WorldAudioCue.HandleSoft;break;
                        case "Wood":chosen=WorldAudioCue.HandleWood;break;
                        case "Steel":case "Iron":case "Copper":case "Brass":case "RustedIron":chosen=WorldAudioCue.HandleMetal;break;
                        default:WorldAudioHooks.Emit(fallback,actor,zone,origin.X,origin.Y);return;
                    }
                    Callback?.Invoke(new WorldAudioUtilityReceipt(actor,item,null,zone,new Point(origin.X,origin.Y),chosen));
                }
                catch(Exception){/* A committed item operation remains complete if optional sound refuses. */}
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
