using System;

namespace CavesOfOoo.Core
{
    /// <summary>One committed, perceived physical work action. This is presentation evidence, never gameplay permission.</summary>
    public readonly struct WorldAudioWorkReceipt
    {
        public readonly Entity Actor, Source;
        public readonly Zone Zone;
        public readonly Point Contact;
        public readonly WorldAudioCue Cue;
        public readonly bool SourceConsumed;
        internal WorldAudioWorkReceipt(Entity actor, Entity source, Zone zone, Point contact, WorldAudioCue cue, bool consumed)
        { Actor=actor; Source=source; Zone=zone; Contact=contact; Cue=cue; SourceConsumed=consumed; }
    }

    /// <summary>Optional transaction-completion audio. No simulation events, clocks, RNG or serialized state.</summary>
    public static class WorldAudioWork
    {
        public static Action<WorldAudioWorkReceipt> Callback { get; set; }

        internal static Capture Repair(Entity actor, RepairablePart part, Zone zone, RepairRecipe recipe)
        {
            if (recipe == null) return null;
            WorldAudioCue cue;
            switch (recipe.Composition)
            {
                case "Wood": cue=WorldAudioCue.WorkRepairWood; break;
                case "Masonry": cue=WorldAudioCue.WorkRepairStone; break;
                case "Metal": cue=WorldAudioCue.WorkRepairMetal; break;
                case "Fiber": case "Leather": cue=WorldAudioCue.Use; break;
                default: return null;
            }
            return Take(actor, part, zone, cue, part?.PortableEquipment == true, false, recipe);
        }
        internal static Capture Field(Entity actor, FieldHarvestPart part, Zone zone)
            => Take(actor,part,zone,WorldAudioCue.HarvestPlant,false,false,null);
        internal static Capture Crop(Entity actor, CropPart part, Zone zone)
            => Take(actor,part,zone,WorldAudioCue.HarvestPlant,false,true,null);

        internal static Capture Forge(Entity actor, Zone zone)
        {
            var origin=zone?.GetEntityCell(actor);
            if (Callback == null || origin == null) return null;
            // The sound needs an actual visible workbench even when one-unit
            // pack assembly is permitted. Never change the crafting permission.
            for (int y=origin.Y-1;y<=origin.Y+1;y++)
                for (int x=origin.X-1;x<=origin.X+1;x++)
                {
                    var cell=zone.GetCell(x,y); if(cell==null)continue;
                    for(int i=0;i<cell.Objects.Count;i++)
                    {
                        var part=cell.Objects[i]?.GetPart<ForgePart>();
                        if(part==null)continue;
                        var result=Take(actor,part,zone,WorldAudioCue.WorkForge,false,false,null);
                        if(result!=null)return result;
                    }
                }
            return null;
        }

        static Capture Take(Entity actor, Part part, Zone zone, WorldAudioCue cue, bool carried, bool consumed, RepairRecipe recipe)
        {
            if (Callback == null || actor == null || part?.ParentEntity == null || zone == null) return null;
            var result=new Capture(actor,part,zone,cue,carried,consumed,recipe);
            return result.Current(false)?result:null;
        }

        internal sealed class Capture
        {
            readonly Entity actor,source;
            readonly Zone zone;
            readonly Cell origin,contact;
            readonly PhysicsPart actorPhysics,physics;
            readonly RenderPart actorRender,render;
            readonly Part part;
            readonly RepairRecipe recipe;
            readonly WorldAudioCue cue;
            readonly bool carried,consumed;
            readonly InventoryPart inventory;
            bool published;
            internal Capture(Entity actor, Part part, Zone zone, WorldAudioCue cue, bool carried, bool consumed, RepairRecipe recipe)
            {
                this.actor=actor;this.part=part;this.zone=zone;this.cue=cue;this.carried=carried;this.consumed=consumed;this.recipe=recipe;
                source=part.ParentEntity; origin=zone.GetEntityCell(actor); contact=carried?origin:zone.GetEntityCell(source);
                actorPhysics=actor.GetPart<PhysicsPart>();actorRender=actor.GetPart<RenderPart>();
                physics=source.GetPart<PhysicsPart>();render=source.GetPart<RenderPart>();inventory=actor.GetPart<InventoryPart>();
            }
            internal bool Current(bool after)
            {
                if (origin==null || contact==null || origin.ParentZone!=zone || contact.ParentZone!=zone
                    || actor.SpatialZone!=zone || zone.GetEntityCell(actor)!=origin || !origin.Objects.Contains(actor)
                    || CombatSystem.IsDeathHandled(actor) || actor.GetStatValue("Hitpoints",0)<=0
                    || actor.GetPart<PhysicsPart>()!=actorPhysics || actorPhysics?.ParentEntity!=actor
                    || actorPhysics.InInventory!=null || actorPhysics.Equipped!=null
                    || actor.GetPart<RenderPart>()!=actorRender || actorRender?.ParentEntity!=actor || !actorRender.Visible
                    || !origin.Explored || !origin.IsVisible || !contact.Explored || !contact.IsVisible
                    || source.GetPart<PhysicsPart>()!=physics || physics?.ParentEntity!=source
                    || source.GetPart<RenderPart>()!=render || render?.ParentEntity!=source || !render.Visible
                    || part.ParentEntity!=source || !source.Parts.Contains(part)
                    || source.HasTag("Creature") || source.GetPart<DestructiblePart>() is DestructiblePart d && (d.Gone || d.HP<=0))return false;
                if(carried)
                {
                    if(actor.GetPart<InventoryPart>()!=inventory || inventory?.ParentEntity!=actor || !inventory.Objects.Contains(source)
                        || source.SpatialZone!=null || !physics.Takeable || physics.InInventory!=actor || physics.Equipped!=null)return false;
                }
                else
                {
                    if(physics.Takeable || physics.InInventory!=null || physics.Equipped!=null
                        || Math.Abs(origin.X-contact.X)>1 || Math.Abs(origin.Y-contact.Y)>1)return false;
                    if(after&&consumed)
                    { if(source.SpatialZone!=null || zone.GetEntityCell(source)!=null || contact.Objects.Contains(source))return false; }
                    else if(source.SpatialZone!=zone || zone.GetEntityCell(source)!=contact || !contact.Objects.Contains(source))return false;
                }
                if(part is RepairablePart repair)
                    return source.GetPart<RepairablePart>()==repair && repair.PortableEquipment==carried
                        && RepairRecipeRegistry.Get(repair.RecipeId)==recipe
                        && (!after || (repair.RepairedBy==actor && (carried?!source.HasEffect<BrokenEffect>():repair.Repaired)));
                if(part is FieldHarvestPart field)return source.GetPart<FieldHarvestPart>()==field && (!after || field.Harvested);
                if(part is CropPart crop)return source.GetPart<CropPart>()==crop;
                return part is ForgePart forge && source.GetPart<ForgePart>()==forge;
            }
            internal void Publish()
            {
                if(published)return;published=true;
                try
                {
                    if(Current(true))Callback?.Invoke(new WorldAudioWorkReceipt(actor,source,zone,new Point(contact.X,contact.Y),cue,consumed));
                }
                catch(Exception){/* A presentation observer cannot refund or fail completed work. */}
            }
        }
    }
}
