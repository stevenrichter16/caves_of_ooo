using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Core
{
    /// <summary>Explicit physical capability for cutting flexible workshop stock.
    /// This does not infer combat damage, tool wear, or other cutting uses.</summary>
    public sealed class CuttingToolPart : Part
    {
        public override string Name => "CuttingTool";
        public bool FlexibleStock = true;
        internal const string DefinitionSeen="CuttingToolDefinitionSeen";
        public override void Initialize(){if(ParentEntity!=null)ParentEntity.IntProperties[DefinitionSeen]=1;}
        internal static void UpgradeLoadedDefinition(Entity item,CavesOfOoo.Data.EntityFactory factory)
        {
            if(item==null || item.IntProperties.ContainsKey(DefinitionSeen) || item.Parts.Any(p=>p is CuttingToolPart)
                || factory?.Blueprints==null || item.BlueprintName==null || !factory.Blueprints.TryGetValue(item.BlueprintName,out var blueprint)
                || blueprint?.Parts==null || !blueprint.Parts.TryGetValue("CuttingTool",out var values) || values==null
                || !values.TryGetValue("FlexibleStock",out var text) || !bool.TryParse(text,out bool enabled) || !enabled)return;
            // A post-parse, explicit definition upgrade only. Never replay creation,
            // restore removed instance Parts, change damage, or populate inventory.
            item.AddPart(new CuttingToolPart());
        }
        // Generic structural splitting cannot copy private effect state. Refuse
        // these instances rather than laundering a broken tool into a fresh one.
        internal static bool CanSplitInstance(Entity item)
        {
            if(item==null)return false;
            var parts=item.Parts.OfType<CuttingToolPart>().ToArray();
            if(parts.Length==0 && !item.IntProperties.ContainsKey(DefinitionSeen))return true;
            return parts.Length<=1 && parts.All(p=>p.ParentEntity==item)
                && item.Parts.OfType<StatusEffectsPart>().All(p=>p.ParentEntity==item && p.EffectCount==0);
        }
        internal static bool CompatibleStackState(Entity a,Entity b)
        {
            if(a==null || b==null)return false;
            var left=a.Parts.OfType<CuttingToolPart>().ToArray();var right=b.Parts.OfType<CuttingToolPart>().ToArray();
            bool seenA=a.IntProperties.TryGetValue(DefinitionSeen,out int markA),seenB=b.IntProperties.TryGetValue(DefinitionSeen,out int markB);
            if(left.Length==0 && right.Length==0 && !seenA && !seenB)return true;
            return left.Length<=1 && right.Length<=1 && left.Length==right.Length && left.All(p=>p.ParentEntity==a) && right.All(p=>p.ParentEntity==b)
                && (left.Length==0 || left[0].FlexibleStock==right[0].FlexibleStock) && seenA==seenB && markA==markB
                && (a.GetPart<StatusEffectsPart>()?.EffectCount??0)==0 && (b.GetPart<StatusEffectsPart>()?.EffectCount??0)==0;
        }
    }

    /// <summary>One exact accessible tool, not a consumable or a workshop location.
    /// A cancelled process does not undo independent effects on the tool.</summary>
    internal sealed class MaterialCuttingWorkReceipt
    {
        internal readonly Entity Tool;
        readonly Entity actor,source;
        readonly Zone zone;
        readonly InventoryPart pack;
        readonly PhysicsPart physics;
        readonly CuttingToolPart capability;
        readonly Part[] parts;
        readonly BodyPart[] slots;
        readonly string[] keys;
        readonly string id,blueprint;
        readonly bool held;
        readonly int weight;
        MaterialCuttingWorkReceipt(Entity actor,Entity source,Entity tool,Zone zone)
        {
            this.actor=actor;this.source=source;Tool=tool;this.zone=zone;pack=actor.GetPart<InventoryPart>();
            physics=tool.GetPart<PhysicsPart>();capability=tool.GetPart<CuttingToolPart>();parts=tool.Parts.ToArray();
            id=tool.ID;blueprint=tool.BlueprintName;held=physics.Equipped==actor;weight=physics.Weight;
            slots=Slots(actor,tool);keys=Keys(pack,tool);
        }
        static BodyPart[] Slots(Entity actor,Entity item)=>actor.GetPart<Body>()?.GetParts().Where(p=>p._Equipped==item).ToArray()??Array.Empty<BodyPart>();
        static string[] Keys(InventoryPart pack,Entity item)=>pack.EquippedItems.Where(p=>p.Value==item).Select(p=>p.Key).OrderBy(k=>k,StringComparer.Ordinal).ToArray();
        static bool Available(Entity actor,Entity source,Entity item,Zone zone)
        {
            var pack=actor?.GetPart<InventoryPart>();var p=item?.GetPart<PhysicsPart>();
            if(pack==null || item==source || item==actor || string.IsNullOrEmpty(item?.ID) || item.ID==actor.ID || item.ID==source?.ID
                || !PreparationActions.CurrentActor(actor,zone) || item.SpatialZone!=null || p?.ParentEntity!=item || !p.Takeable || p.Solid
                || !item.HasTag("Item") || item.HasTag("Creature") || item.HasTag("Terrain") || item.HasPart<SpatialFootprintPart>()
                || item.Parts.Count(v=>v is PhysicsPart)!=1 || !OwnedOptional<StackerPart>(item) || !OwnedOptional<DestructiblePart>(item) || !OwnedOptional<StatusEffectsPart>(item)
                || item.Parts.Count(v=>v is CuttingToolPart)!=1 || item.GetPart<CuttingToolPart>()?.ParentEntity!=item
                || item.GetPart<CuttingToolPart>()?.FlexibleStock!=true || item.HasEffect<BrokenEffect>()
                || CombatSystem.IsDeathHandled(item) || item.GetStat("Hitpoints")!=null && item.GetStatValue("Hitpoints")<=0
                || (item.GetPart<StackerPart>()?.StackCount??1)!=1
                || (item.GetPart<StackerPart>()?.MaxStack??1)<1
                || item.GetPart<DestructiblePart>() is DestructiblePart d && (d.Gone || d.HP<=0))return false;
            var limbs=Slots(actor,item);var keys=Keys(pack,item);int copies=pack.Objects.Count(e=>e==item);
            if(p.InInventory==actor && p.Equipped==null)
            {if(copies!=1 || keys.Length!=0 || limbs.Length!=0)return false;}
            else if(p.InInventory==null && p.Equipped==actor)
            {
                if(copies!=0 || keys.Length==0)return false;
                if(actor.HasPart<Body>() && (limbs.Length!=keys.Length || limbs.Length==0 || limbs.Count(l=>l.FirstSlotForEquipped)!=1
                    || limbs.Any(l=>!pack.EquippedItems.TryGetValue(l.ID.ToString(),out var e)||e!=item)))return false;
            }
            else return false;
            var owners=pack.Objects.Concat(pack.EquippedItems.Values).Concat(actor.GetPart<Body>()?.GetParts().Select(l=>l._Equipped)??Enumerable.Empty<Entity>()).Where(e=>e!=null).Distinct();
            return owners.Count(e=>e.ID==item.ID)==1 && !zone.GetReadOnlyEntities().Any(e=>e.ID==item.ID);
        }
        static bool OwnedOptional<T>(Entity item) where T:Part
        {var parts=item.Parts.OfType<T>().ToArray();return parts.Length<=1 && parts.All(p=>p.ParentEntity==item);}
        internal static MaterialCuttingWorkReceipt Resolve(Entity actor,Entity source,Zone zone)
        {
            var pack=actor?.GetPart<InventoryPart>();if(pack==null)return null;
            var item=pack.Objects.Concat(pack.EquippedItems.Values).Where(e=>Available(actor,source,e,zone)).Distinct().OrderBy(e=>e.ID,StringComparer.Ordinal).FirstOrDefault();
            return item==null?null:new MaterialCuttingWorkReceipt(actor,source,item,zone);
        }
        internal bool Current()=>actor.GetPart<InventoryPart>()==pack && Tool.ID==id && Tool.BlueprintName==blueprint
            && Tool.Parts.SequenceEqual(parts) && Tool.GetPart<PhysicsPart>()==physics && physics.Weight==weight
            && Tool.GetPart<CuttingToolPart>()==capability && (physics.Equipped==actor)==held
            && Slots(actor,Tool).SequenceEqual(slots) && Keys(pack,Tool).SequenceEqual(keys) && Available(actor,source,Tool,zone);
        internal bool Begin(InventoryTransaction tx,string command)
        {
            if(tx==null || !Current() || !tx.TryClaim(Tool,actor,command))return false;
            tx.BeforeCommit(Current);return true;
        }
    }
}
