using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>A supervised, finite preparation at a physical bench. Quantities
    /// are authored item measures, not liquid drams or a thermal simulation.</summary>
    public sealed class OilStrainingPart : Part
    {
        public override string Name => "OilStraining";
        public const string Blueprint="PeatOilStrainer", CommandPrefix="StrainFrogOil|";
        public static bool IsCommand(string command)=>command?.StartsWith(CommandPrefix,StringComparison.Ordinal)==true;
        public string Describe()=>"Cold-strain 2 carried frog-oil gourds into 1 lamp-oil measure in one action. Carried weight: 2 to 1. Both quoted gourds are consumed; straining residue is discarded here and cannot be recovered. Lamp oil can later restore up to 25 fuel to an unlit packed torch, limited by its capacity. Keep raw oil for greasing ground or trade. No heat or waiting is required.";
        static bool Parts(Entity e,params string[] names)=>e!=null&&e.Parts.Count==names.Length
            &&names.All(n=>e.Parts.Count(p=>p!=null&&p.Name==n&&p.ParentEntity==e)==1);
        internal static bool Payload(Entity e)=>e?.BlueprintName==Blueprint&&!string.IsNullOrEmpty(e.ID)
            &&Parts(e,"Render","Physics","Examinable","Composition","OilStraining")
            &&e.HasTag("Furniture")&&!e.HasTag("Creature")&&!e.HasTag("Terrain")&&!e.HasTag("Item")
            &&e.GetPart<PhysicsPart>() is PhysicsPart p&&!p.Takeable&&p.Solid&&p.Weight==80&&p.InInventory==null&&p.Equipped==null
            &&e.GetPart<CompositionPart>().MaterialsRaw=="Wood"&&e.GetPart<RenderPart>().Visible;
        internal static bool ContentAvailable(EntityFactory factory)
        {
            if(factory==null||!factory.Blueprints.TryGetValue(Blueprint,out var bp)||bp==null
                ||!factory.Blueprints.ContainsKey("LampOil")||!bp.Tags.ContainsKey("Furniture")
                ||bp.Tags.ContainsKey("Item")||bp.Tags.ContainsKey("Creature")||bp.Tags.ContainsKey("Terrain")
                ||bp.Parts.Count!=5||new[]{"Render","Physics","Examinable","Composition","OilStraining"}.Any(n=>!bp.Parts.ContainsKey(n)))return false;
            bool Field(string part,string key,string value)=>bp.Parts[part].TryGetValue(key,out var actual)&&string.Equals(actual,value,StringComparison.OrdinalIgnoreCase);
            return Field("Physics","Weight","80")&&Field("Physics","Solid","true")&&Field("Physics","Takeable","false")
                &&Field("Composition","MaterialsRaw","Wood")&&Field("Render","RenderString","=")&&Field("Render","ColorString","&y")&&Field("Render","RenderLayer","4");
        }
        static bool OutputTemplate(EntityFactory factory)
        {
            if(factory==null||!factory.Blueprints.TryGetValue("LampOil",out var bp)||bp==null
                ||bp.Parts.Count!=7||new[]{"Render","Physics","Examinable","Stacker","Commerce","Reagent","TorchRefuel"}.Any(n=>!bp.Parts.ContainsKey(n)))return false;
            bool Field(string part,string key,string value)=>bp.Parts[part].TryGetValue(key,out var actual)&&actual==value;
            bool Default(string part,string key,string value)=>!bp.Parts[part].ContainsKey(key)||Field(part,key,value);
            return Field("Physics","Weight","1")&&Field("Physics","Takeable","true")&&Default("Physics","Solid","false")
                &&Field("Commerce","Value","6")&&Field("Reagent","PropertiesRaw","combustible:3, viscous:1")
                &&Default("Stacker","StackCount","1")&&Default("Stacker","MaxStack","99")&&Default("Render","Visible","true");
        }
        static bool Grid(Entity e,Zone z)=>WorldResourceActions.Ground(e,z)&&z.GetEntityCell(e) is Cell c
            &&z.GetCell(c.X,c.Y)==c&&z.GetReadOnlyEntities().Count(o=>o==e||o.ID==e.ID)==1;
        bool Context(Entity actor,Zone zone)=>Payload(ParentEntity)&&ParentEntity.GetPart<OilStrainingPart>()==this
            &&PreparationActions.CurrentActor(actor,zone)&&actor.HasTag("Player")&&actor.ID!=ParentEntity.ID
            &&actor.GetPart<StatusEffectsPart>()?.IsActionBlocked()!=true&&Grid(actor,zone)&&Grid(ParentEntity,zone)
            &&zone.GetEntityCell(ParentEntity).IsVisible&&zone.GetEntityCell(ParentEntity).Explored
            &&SpatialQuery.Distance(zone,actor,ParentEntity)<=1;
        static bool Raw(Entity e)=>e?.BlueprintName=="FrogOil"&&Parts(e,"Render","Physics","Examinable","Commerce")
            &&!e.HasTag("Creature")&&!e.HasTag("Terrain")&&e.GetPart<RenderPart>().Visible&&e.GetPart<PhysicsPart>().Weight==1
            &&!e.GetPart<PhysicsPart>().Solid&&e.GetPart<CommercePart>().Value==8;
        static bool Oil(Entity e)=>e?.BlueprintName=="LampOil"&&Parts(e,"Render","Physics","Examinable","Stacker","Commerce","Reagent","TorchRefuel")
            &&e.HasTag("Item")&&!e.HasTag("Creature")&&!e.HasTag("Terrain")&&e.GetPart<RenderPart>().Visible&&e.GetPart<PhysicsPart>().Weight==1
            &&!e.GetPart<PhysicsPart>().Solid&&e.GetPart<CommercePart>().Value==6
            &&e.GetPart<StackerPart>().MaxStack==99&&e.GetPart<ReagentPart>().PropertiesRaw=="combustible:3, viscous:1";
        static bool RawCarried(Entity actor,Entity e)=>Raw(e)&&WorldResourceActions.Carried(actor,e)&&actor.GetPart<InventoryPart>().CanConsumeOne(e);
        static bool OilCarried(Entity actor,Entity e)=>Oil(e)&&PreparationActions.Carried(actor,e);
        public override bool HandleEvent(GameEvent e)
        {
            var actor=e.GetParameter<Entity>("Actor");var zone=PreparationActions.ZoneFor(e,actor);
            if(e.ID=="GetInventoryActions")
            {
                if(Context(actor,zone))
                {
                    var inputs=actor.GetPart<InventoryPart>().Objects.Where(o=>RawCarried(actor,o)).Take(2).ToArray();
                    if(inputs.Length==2)e.GetParameter<InventoryActionList>("Actions")?.AddAction("OilStraining",
                        "strain 2 frog oil -> 1 lamp oil (1 action)",CommandPrefix+Uri.EscapeDataString(inputs[0].ID)+"|"+Uri.EscapeDataString(inputs[1].ID),'s',20);
                }
                return true;
            }
            string command=e.GetStringParameter("Command");
            if(e.ID!="InventoryAction"||!IsCommand(command))return true;
            if(e.GetParameter<bool>("OilStrainingAttempted"))return true;e.SetParameter("OilStrainingAttempted",true);
            if(!Strain(actor,zone,command,e.GetParameter<InventoryTransaction>("InventoryTransaction")))return true;
            e.Handled=true;return false;
        }
        // Only used for exact participating owners during rollback. Creation
        // callbacks run before validation, so even rejected staged output needs
        // its transient custody removed.
        static void DetachTransient(Entity owned)
        {
            if(owned==null)return;
            owned.SpatialZone?.RemoveEntity(owned);
            var carrier=owned.GetPart<PhysicsPart>()?.InInventory;
            if(carrier!=null)
            {carrier.GetPart<InventoryPart>()?.RemoveObject(owned);carrier.GetPart<ContainerPart>()?.RemoveItem(owned);}
            var wearer=owned.GetPart<PhysicsPart>()?.Equipped;
            if(wearer!=null)
            {
                // Neither supported material is equipment. Clear only
                // this forcibly attached owner, without dispatching a
                // second action while the failed receipt is unwinding.
                var inventory=wearer.GetPart<InventoryPart>();
                if(inventory!=null)foreach(var slot in inventory.EquippedItems.Where(p=>p.Value==owned).Select(p=>p.Key).ToArray())inventory.EquippedItems.Remove(slot);
                var body=wearer.GetPart<Body>();
                if(body!=null)foreach(var limb in body.GetParts())if(limb._Equipped==owned)limb.ClearEquipped();
                inventory?.Objects.Remove(owned);
            }
        }
        bool Strain(Entity actor,Zone zone,string command,InventoryTransaction tx)
        {
            bool Refuse()=>PreparationActions.Reject(actor,ParentEntity,command,"strainer_or_supplies_unavailable");
            if(tx==null||!Context(actor,zone))return Refuse();
            var fields=command.Split('|');if(fields.Length!=3)return Refuse();
            var first=WorldResourceActions.ExactCarried(actor,fields[1]);var second=WorldResourceActions.ExactCarried(actor,fields[2]);
            if(first==second||!RawCarried(actor,first)||!RawCarried(actor,second))return Refuse();
            var factory=SeedPart.Factory;var pack=actor.GetPart<InventoryPart>();
            // Reject unexpected creation behaviour before invoking any output Part.
            // This deliberately supports the canonical authored LampOil only.
            if(!OutputTemplate(factory))return Refuse();
            var inputs=new[]{first,second};var station=ParentEntity;var actorCell=zone.GetEntityCell(actor);var stationCell=zone.GetEntityCell(station);
            var stationParts=station.Parts.ToArray();string stationId=station.ID,actorId=actor.ID;
            var inputParts=inputs.Select(o=>o.Parts.ToArray()).ToArray();var inputIds=inputs.Select(o=>o.ID).ToArray();
            foreach(var owner in inputs.Concat(new[]{actor,station}))if(!tx.TryClaim(owner,actor,command))return Refuse();
            bool Current()=>ReferenceEquals(ParentEntity,station)&&Payload(station)&&station.GetPart<OilStrainingPart>()==this
                &&Context(actor,zone)&&SeedPart.Factory==factory&&actor.GetPart<InventoryPart>()==pack
                &&actor.ID==actorId&&station.ID==stationId&&station.Parts.SequenceEqual(stationParts)
                &&zone.GetEntityCell(actor)==actorCell&&zone.GetEntityCell(station)==stationCell;
            bool Inputs(bool carried)=>inputs.Select((o,i)=>o.ID==inputIds[i]&&o.Parts.SequenceEqual(inputParts[i])&&Raw(o)
                &&(carried?RawCarried(actor,o):o.SpatialZone==null&&o.GetPart<PhysicsPart>().InInventory==null
                    &&o.GetPart<PhysicsPart>().Equipped==null&&!pack.Objects.Any(x=>x==o||x.ID==o.ID))).All(v=>v);
            var product=factory.CreateEntity("LampOil");
            tx.Do(null,()=>DetachTransient(product));
            if(!Current()||!Inputs(true)||!Oil(product)||!PreparationActions.Fresh(product,"LampOil")
                ||product.GetPart<StackerPart>().StackCount!=1||product.ID==actorId||product.ID==stationId
                ||pack.Objects.Concat(pack.EquippedItems.Values).Concat(zone.GetReadOnlyEntities()).Any(o=>o==product||o?.ID==product.ID)
                ||!tx.TryClaim(product,actor,command))return Refuse();
            string productId=product.ID;var productParts=product.Parts.ToArray();
            var residents=pack.Objects.Where(o=>o?.BlueprintName=="LampOil").ToArray();
            if(residents.Any(o=>!OilCarried(actor,o)))return Refuse();
            foreach(var resident in residents)if(!tx.TryClaim(resident,actor,command))return Refuse();
            int before=pack.GetCarriedWeight(),priorOil=residents.Sum(o=>o.GetPart<StackerPart>().StackCount);
            int ceiling=pack.MaxWeight<0?int.MaxValue:Math.Max(pack.MaxWeight,before);
            var receipt=InventoryTransferSnapshot.Capture(pack,product);bool restored=false;Entity received=null;
            tx.Do(null,()=>
            {
                if(restored)return;restored=true;
                // These exact owners were claimed throughout this operation. A
                // forced callback can still move them; remove that transient
                // custody before restoring the receipt, so no refund duplicates.
                foreach(var owned in inputs.Concat(new[]{product,received}).Where(o=>o!=null).Distinct())DetachTransient(owned);
                receipt.Restore();
            });
            if(!receipt.Apply(()=>pack.TryConsumeOne(first)&&pack.TryConsumeOne(second)&&pack.AddCraftedUnit(product,out received))
                ||!receipt.ClaimChanges(tx,actor,command))return Refuse();
            string receivedId=received?.ID;var receivedParts=received?.Parts.ToArray();
            bool Final()=>Current()&&Inputs(false)&&product.ID==productId&&product.Parts.SequenceEqual(productParts)&&Oil(product)
                &&(product==received||product.GetPart<StackerPart>().StackCount==0&&product.SpatialZone==null
                    &&product.GetPart<PhysicsPart>().InInventory==null&&product.GetPart<PhysicsPart>().Equipped==null)
                &&OilCarried(actor,received)&&received.ID==receivedId&&received.Parts.SequenceEqual(receivedParts)
                &&pack.Objects.Where(o=>o?.BlueprintName=="LampOil").All(o=>OilCarried(actor,o))
                &&pack.Objects.Where(o=>o?.BlueprintName=="LampOil").Sum(o=>o.GetPart<StackerPart>().StackCount)==priorOil+1
                &&pack.GetCarriedWeight()<=ceiling&&pack.GetCarriedWeight()<=before-1;
            if(!Final())return Refuse();tx.BeforeCommit(Final);
            PreparationActions.Complete(tx,actor,station,command,"You strain two frog-oil gourds into one lamp-oil measure. The straining residue is discarded. Use the oil on an unlit packed torch, or keep it for greasing or alchemy.");
            return true;
        }
    }
}
