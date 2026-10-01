using System;
using System.Collections.Generic;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>One authored structural fault. Repair spends carried materials and
    /// saves a restored-function flag in the same command receipt. It does not heal
    /// combat HP, resurrect destroyed structures, or change settlement quest stages.</summary>
    public sealed class RepairablePart : Part
    {
        public override string Name => "Repairable";
        public const string RepairCommand="RepairObject";
        public string RecipeId="";
        public bool Repaired;
        /// <summary>Shared functional gate. Unknown or malformed faults remain
        /// broken; owners without this opt-in Part retain their ordinary behavior.</summary>
        public static bool BlocksFunction(Entity owner)
        {
            if(owner==null)return false;
            int count=0;bool blocked=false;
            foreach(var part in owner.Parts)
                if(part is RepairablePart fault) { count++;blocked|=fault.ParentEntity!=owner || !fault.Repaired; }
            return count>1 || blocked;
        }
        public string Describe()
        {
            var recipe=RepairRecipeRegistry.Get(RecipeId);
            if(recipe==null)return "The damage cannot be repaired with a known method.";
            return Repaired ? recipe.RepairedText : recipe.Diagnosis+"\nRepair: "+recipe.Quantity+" "+recipe.MaterialName+". Composition: "+recipe.Composition+".";
        }
        /// <summary>Use the same native command receipt as the player menu. This
        /// method charges no turn; successful gameplay callers charge one action.</summary>
        public bool TryRepair(Entity actor, Zone zone)
        {
            // A retained reference must never dispatch a replacement owner's fault.
            string reason=Context(actor,zone,RepairRecipeRegistry.Get(RecipeId));
            if(reason!=null)return Reject(actor,reason);
            return InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(ParentEntity,RepairCommand),actor,zone).Success;
        }
        public override bool HandleEvent(GameEvent e)
        {
            if(e.ID=="GetInventoryActions")
            {
                if(!Repaired)
                {
                    var recipe=RepairRecipeRegistry.Get(RecipeId);
                    if(recipe!=null)e.GetParameter<InventoryActionList>("Actions")?.AddAction("Repair",recipe.ActionText+" ("+recipe.Quantity+" "+recipe.MaterialName+")",RepairCommand,'r',20);
                }
                return true;
            }
            if(e.ID!="InventoryAction" || e.GetStringParameter("Command")!=RepairCommand)return true;
            var actor=e.GetParameter<Entity>("Actor"); var zone=e.GetParameter<Zone>("Zone");
            var tx=e.GetParameter<InventoryTransaction>("InventoryTransaction");
            if(tx==null) { Reject(actor,"missing-transaction"); return true; }
            if(!Apply(actor,zone,tx))return true;
            e.Handled=true;return false;
        }
        private string Context(Entity actor,Zone zone,RepairRecipe recipe)
        {
            var owner=ParentEntity;
            if(actor==null || zone==null || owner==null || owner.GetPart<RepairablePart>()!=this)return "missing-context";
            int faultCount=0,compositionCount=0;
            foreach(var part in owner.Parts) { if(part is RepairablePart)faultCount++;if(part is CompositionPart)compositionCount++; }
            if(faultCount!=1 || compositionCount!=1)return "ambiguous-structure";
            if(Repaired)return "already-repaired";
            if(recipe==null || RepairRecipeRegistry.Get(RecipeId)!=recipe)return "unknown-recipe";
            if(!actor.HasTag("Player") || actor.GetStatValue("Hitpoints",0)<=0 || CombatSystem.IsDeathHandled(actor)
                || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked()==true)return "actor-unavailable";
            var inventory=actor.GetPart<InventoryPart>();
            if(inventory?.ParentEntity!=actor)return "missing-inventory";
            var a=zone.GetEntityCell(actor); var t=zone.GetEntityCell(owner); var physics=owner.GetPart<PhysicsPart>();
            if(actor.SpatialZone!=zone || owner.SpatialZone!=zone || a?.ParentZone!=zone || t?.ParentZone!=zone
                || !a.Objects.Contains(actor) || !t.Objects.Contains(owner))return "not-local";
            var manager=WorldLocationContext.For(zone);
            if(manager!=null && (!manager.CachedZones.TryGetValue(zone.ZoneID,out var current) || current!=zone))return "stale-zone";
            if(SpatialQuery.Distance(zone,actor,owner)>1)return "out-of-reach";
            if(owner.HasTag("Creature") || physics?.ParentEntity!=owner || physics.Takeable || physics.InInventory!=null || physics.Equipped!=null
                || owner.GetPart<DestructiblePart>() is DestructiblePart d && (d.Gone || d.HP<=0))return "invalid-target";
            var composition=owner.GetPart<CompositionPart>();
            if(composition?.ParentEntity!=owner || !composition.Contains(recipe.Composition))return "wrong-composition";
            return null;
        }
        private static bool Carried(Entity item,Entity actor,InventoryPart inventory,string blueprint)
        {
            var physical=item?.GetPart<PhysicsPart>(); var stack=item?.GetPart<StackerPart>();
            if(item==null || item.BlueprintName!=blueprint || item.HasTag("Creature") || item.SpatialZone!=null
                || physical?.ParentEntity!=item || !physical.Takeable || physical.InInventory!=actor || physical.Equipped!=null
                || (stack!=null && (stack.ParentEntity!=item || stack.StackCount<1)) || !inventory.Objects.Contains(item))return false;
            // Adversarial hypothesis: corrupted legacy/body equipment aliases must
            // not become payment merely because their Physics backlink says carried.
            foreach(var equipped in inventory.EquippedItems.Values)if(equipped==item)return false;
            if(inventory.FindEquippedBodyPart(item)!=null)return false;
            int references=0; foreach(var candidate in inventory.Objects)if(candidate==item)references++;
            return references==1;
        }
        private bool Apply(Entity actor,Zone zone,InventoryTransaction tx)
        {
            var recipe=RepairRecipeRegistry.Get(RecipeId); string reason=Context(actor,zone,recipe);
            if(reason!=null)return Reject(actor,reason);
            if(!tx.TryClaim(ParentEntity,actor,RepairCommand) || !tx.TryClaim(actor,actor,RepairCommand))return Reject(actor,"owner-busy");
            var inventory=actor.GetPart<InventoryPart>(); var units=new List<Entity>(); int remaining=recipe.Quantity;
            foreach(var item in inventory.Objects)
            {
                if(!Carried(item,actor,inventory,recipe.MaterialBlueprint))continue;
                int take=Math.Min(remaining,item.GetPart<StackerPart>()?.StackCount??1);
                for(int i=0;i<take;i++)units.Add(item);
                remaining-=take;if(remaining==0)break;
            }
            if(remaining!=0)return Reject(actor,"missing-materials");
            foreach(var item in units)if(!tx.TryClaim(item,actor,RepairCommand))return Reject(actor,"material-busy");
            var receipt=InventoryTransferSnapshot.Capture(inventory); tx.Do(null,receipt.Restore);
            bool applied=receipt.Apply(()=>
            {
                foreach(var item in units)
                {
                    if(!Carried(item,actor,inventory,recipe.MaterialBlueprint) || !inventory.TryConsumeOne(item))return false;
                }
                return true;
            });
            // Claims protect native command re-entry; recheck the saved owner and
            // selected recipe after payment before granting the structural benefit.
            if(!applied || !receipt.ClaimChanges(tx,actor,RepairCommand) || Context(actor,zone,recipe)!=null
                || actor.GetPart<InventoryPart>()!=inventory)return Reject(actor,"state-changed");
            tx.Do(()=>Repaired=true,()=>Repaired=false);
            tx.AfterCommit(()=>
            {
                MessageLog.Add(recipe.RepairedText);
                Diag.Record("furniture","ObjectRepaired",actor,ParentEntity,new{recipeId=recipe.Id,material=recipe.MaterialBlueprint,quantity=recipe.Quantity,zoneId=zone.ZoneID});
                var cell=zone.GetEntityCell(ParentEntity); if(cell!=null)ZoneRenderHooks.MarkCellDirty(cell.X,cell.Y,"Repair.Completed");
            });
            return true;
        }
        private bool Reject(Entity actor,string reason)
        {
            if(actor?.HasTag("Player")==true)MessageLog.Add(reason=="already-repaired"?"That repair is already complete.":Describe());
            Diag.Record("furniture","RepairRejected",actor,ParentEntity,new{recipeId=RecipeId,reason});return false;
        }
    }
}
