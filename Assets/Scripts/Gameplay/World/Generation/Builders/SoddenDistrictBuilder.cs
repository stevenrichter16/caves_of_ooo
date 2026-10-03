using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>One complete fresh Sodden destination. Map/version authority is
    /// owned by the manager; this builder only accepts empty exact-address graphs.
    /// Stages native owners before publication and seals them for final generation.</summary>
    public sealed class SoddenDistrictBuilder:IZoneBuilder
    {
        public const string RoleKey="SoddenDistrict.Role";
        public string Name=>"SoddenDistrict";
        public int Priority=>2000;
        public SoddenDistrictPlan Plan {get;private set;}
        public Zone SourceZone {get;private set;}
        /// <summary>Cold construction hook for binding the actual local worker.
        /// Called once before the packet's final receipt is captured.</summary>
        public Action<Zone,Entity> ConfigureStop;
        readonly int seed;
        Func<bool> finalProof;
        HashSet<Entity> finalOwners;
        HashSet<(int x,int y)> finalReservations;
        static readonly string[] required={"Grass","StoneFloor","StoneWall","Duckboard","PeatBank","MirePool","Reeds","DeadTree","Bed","Chair",
            "SoddenRouteNotice","SoddenDressingBench","SoddenWorksSalvage","SoddenWorksLocker","PeatCutter","MawToad","Bandfrog",
            "SumpsieveCrop","SumpsievePad","SumpsieveSeed","SoddenFieldDressing","SalvagedTimber","LeatherBoots","Buckler","KnotflaxCord"};
        public SoddenDistrictBuilder(int seed){this.seed=seed;}

        /// <summary>Require the complete finite circuit content before selecting
        /// a bespoke pipeline. Missing packs must retain the ordinary wilderness.</summary>
        public static bool SupportsContent(EntityFactory factory)
            =>factory?.Blueprints!=null&&required.All(id=>factory.Blueprints.TryGetValue(id,out var bp)&&bp!=null
                &&bp.Parts.ContainsKey("Physics")&&bp.Parts.ContainsKey("Render"))
                &&factory.Blueprints["SoddenDressingBench"].Parts.ContainsKey("SoddenPreparation")
                &&factory.Blueprints["Bed"].Parts.ContainsKey("Bed")&&factory.Blueprints["Chair"].Parts.ContainsKey("Chair");

        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(zone==null||rng==null||!SoddenDistrictPlan.IsSupportedZone(zone.ZoneID)||zone.EntityCount!=0
                ||SourceZone!=null||!SupportsContent(factory))return Reject(zone,"invalid-empty-source-or-content");
            var plan=SoddenDistrictPlan.Create(zone.ZoneID,seed);
            var priorReservations=new HashSet<(int x,int y)>(zone.GenReservedCells);
            var staged=new List<(Entity owner,int x,int y)>();var added=new List<Entity>();
            var created=new Dictionary<Entity,(string id,string blueprint)>();var ids=new HashSet<string>(StringComparer.Ordinal);
            var detachedProofs=new List<Func<bool>>();bool success=false;
            var oldFactory=LoadoutPart.Factory;var oldRng=LoadoutPart.Rng;
            try
            {
                LoadoutPart.Factory=factory;LoadoutPart.Rng=new Random(unchecked(seed^0x50dd3e));
                bool EmptySource()=>zone.EntityCount==0&&priorReservations.SetEquals(zone.GenReservedCells);
                Entity Make(string bp)
                {
                    if(!EmptySource())throw new InvalidOperationException("changed-empty-source");
                    var e=factory.CreateEntity(bp);
                    if(!Fresh(e,bp)||!ids.Add(e.ID)||!EmptySource())throw new InvalidOperationException("invalid-created-owner:"+bp);
                    created.Add(e,(e.ID,bp));return e;
                }
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                {
                    var floor=Make(plan.GroundAt(x,y));
                    if(plan.ObjectAt(x,y)=="SumpsieveCrop"){floor.SetTag("Plantable");floor.AddPart(new CultivatedSoilPart());}
                    if(!Valid(floor,plan.GroundAt(x,y)))throw new InvalidOperationException("invalid-ground");
                    staged.Add((floor,x,y));detachedProofs.Add(SpreadGenerationReceipt.CaptureDetachedState(floor));
                    string bp=plan.ObjectAt(x,y);if(bp==null)continue;
                    var owner=Make(bp);string role=plan.RoleAt(x,y);
                    if(role!=null)owner.Properties[RoleKey]=role;
                    if(role=="notice")DescribeNotice(owner,zone.ZoneID);
                    if(role=="keeper")owner.GetPart<RenderPart>().DisplayName="shelter peat-cutter";
                    if(role=="keeper"||role=="frog")
                    {
                        // This frog rests on its dry island until a real threat
                        // engages it; ordinary wandering would kill it in mire
                        // before a traveller could make the crossing choice.
                        var brain=owner.GetPart<BrainPart>();if(brain==null)throw new InvalidOperationException("missing-stationed-brain");
                        brain.Wanders=false;brain.WandersRandomly=false;brain.Stay(x,y);
                    }
                    if(bp=="SumpsieveCrop")
                    {
                        var crop=owner.GetPart<CropPart>();if(crop==null)throw new InvalidOperationException("missing-crop");
                        crop.GrowthStage=role=="ripe-sumpsieve"?2:0;crop.TicksInStage=0;crop.MoistureTicks=0;
                        owner.GetPart<RenderPart>().RenderString=crop.GlyphForStage(crop.GrowthStage).ToString();
                        owner.GetPart<RenderPart>().ColorString=crop.ColorForStage(crop.GrowthStage);
                    }
                    if(!Valid(owner,bp))throw new InvalidOperationException("invalid-object:"+bp);
                    staged.Add((owner,x,y));
                    if(!owner.HasPart<BrainPart>()&&!owner.HasPart<ContainerPart>())detachedProofs.Add(SpreadGenerationReceipt.CaptureDetachedState(owner));
                }
                var locker=staged.Select(p=>p.owner).FirstOrDefault(e=>e.BlueprintName=="SoddenWorksLocker");
                if(locker!=null)
                {
                    var container=locker.GetPart<ContainerPart>();
                    foreach(string bp in new[]{"LeatherBoots","Buckler","KnotflaxCord","KnotflaxCord"})
                    {
                        var item=Make(bp);
                        if(!Portable(item,bp)||!container.AddItem(item))throw new InvalidOperationException("invalid-stock:"+bp);
                    }
                }
                if(!EmptySource()||!detachedProofs.All(p=>p())||created.Any(p=>p.Key.ID!=p.Value.id||p.Key.BlueprintName!=p.Value.blueprint)
                    ||staged.Any(p=>!Valid(p.owner,p.owner.BlueprintName))||!StockValid(locker)||!GraphValid(staged.Select(p=>p.owner)))
                    throw new InvalidOperationException("changed-staged-packet");
                foreach(var p in staged)
                {
                    if(!zone.AddEntity(p.owner,p.x,p.y))throw new InvalidOperationException("placement-refused");
                    added.Add(p.owner);
                }
                var worker=staged.Select(p=>p.owner).FirstOrDefault(e=>e.GetProperty(RoleKey)=="keeper");
                if(worker!=null)ConfigureStop?.Invoke(zone,worker);
                if(created.Any(p=>p.Key.ID!=p.Value.id||p.Key.BlueprintName!=p.Value.blueprint)
                    ||!staged.All(p=>p.owner.SpatialZone==zone&&zone.GetEntityPosition(p.owner)==(p.x,p.y))
                    ||!new HashSet<Entity>(staged.Select(p=>p.owner)).SetEquals(zone.GetReadOnlyEntities())
                    ||staged.Any(p=>!Valid(p.owner,p.owner.BlueprintName))||!StockValid(locker)||!GraphValid(staged.Select(p=>p.owner)))
                    throw new InvalidOperationException("changed-published-packet");
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                {
                    zone.GetCell(x,y).IsInterior=plan.IsInterior(x,y);
                    if(plan.IsReserved(x,y))zone.GenReservedCells.Add((x,y));
                }
                foreach(var p in staged)p.owner.GetPart<TileStateSourcePart>()?.Seed(zone,p.x,p.y);
                Plan=plan;SourceZone=zone;finalOwners=new HashSet<Entity>(staged.Select(p=>p.owner));
                finalReservations=new HashSet<(int x,int y)>(zone.GenReservedCells);
                finalProof=SpreadGenerationReceipt.CaptureFinalState(zone,finalOwners);
                success=true;
                Diag.Record("worldgen","SoddenDistrictStaged",payload:new{zoneId=zone.ZoneID,seed,owners=staged.Count,title=plan.Title});
                return true;
            }
            catch(Exception error){return Reject(zone,error.Message);}
            finally
            {
                LoadoutPart.Factory=oldFactory;LoadoutPart.Rng=oldRng;
                if(!success)
                {
                    foreach(var e in added)if(e.SpatialZone==zone)zone.RemoveEntity(e);
                    zone.GenReservedCells.Clear();foreach(var p in priorReservations)zone.GenReservedCells.Add(p);
                }
            }
        }
        /// <summary>Read-only cold-generation receipt. Call only at final commit;
        /// ordinary play and save hydration preserve later owner changes literally.</summary>
        public bool ValidateFinal(Zone zone)=>ReferenceEquals(SourceZone,zone)&&zone!=null&&finalOwners!=null
            &&finalOwners.SetEquals(zone.GetReadOnlyEntities())&&finalReservations.SetEquals(zone.GenReservedCells)&&finalProof?.Invoke()==true;

        static bool Fresh(Entity e,string bp)=>e!=null&&e.BlueprintName==bp&&!string.IsNullOrEmpty(e.ID)&&e.SpatialZone==null
            &&e.GetPart<PhysicsPart>() is PhysicsPart p&&p.InInventory==null&&p.Equipped==null
            &&e.GetPart<RenderPart>()?.Visible==true&&!e.HasPart<SpatialFootprintPart>()&&e.Parts.All(part=>part!=null&&part.ParentEntity==e);
        static bool Valid(Entity e,string bp)
        {
            if(e==null||e.BlueprintName!=bp||e.GetPart<PhysicsPart>() is not PhysicsPart p||p.Takeable
                ||e.GetPart<RenderPart>()?.Visible!=true||e.HasPart<SpatialFootprintPart>()||e.Parts.Any(part=>part==null||part.ParentEntity!=e))return false;
            bool actor=bp=="PeatCutter"||bp=="MawToad"||bp=="Bandfrog";
            bool solid=actor||bp=="StoneWall"||bp=="PeatBank"||bp=="DeadTree"||bp=="SoddenRouteNotice"
                ||bp=="SoddenDressingBench"||bp=="SoddenWorksLocker";
            if(p.Solid!=solid||!solid&&e.HasTag("Solid"))return false;
            if(actor)
            {
                var brain=e.GetPart<BrainPart>();
                if(!e.HasTag("Creature")||brain==null||e.GetStatValue("Hitpoints")<=0)return false;
                if(e.GetProperty(RoleKey)=="keeper")return !brain.Wanders&&!brain.WandersRandomly&&brain.Staying
                    &&brain.StartingCellX==35&&brain.StartingCellY==6&&e.HasPart<AISelfPreservationPart>();
                if(e.GetProperty(RoleKey)=="frog")return !brain.Wanders&&!brain.WandersRandomly&&brain.Staying&&!brain.Passive
                    &&brain.StartingCellX==40&&brain.StartingCellY==13&&e.HasPart<CausticSkinPart>();
                return true;
            }
            if(e.HasPart<BrainPart>())return false;
            if(bp=="Grass"||bp=="StoneFloor")return e.HasTag("Terrain");
            if(bp=="Bed")return e.HasTag("Furniture")&&e.GetPart<BedPart>() is BedPart bed
                &&e.Parts.Count(part=>part is BedPart)==1&&!bed.Occupied&&string.IsNullOrEmpty(bed.Owner);
            if(bp=="Chair")return e.HasTag("Furniture")&&e.GetPart<ChairPart>() is ChairPart chair
                &&e.Parts.Count(part=>part is ChairPart)==1&&!chair.Occupied&&chair.Occupant==null&&string.IsNullOrEmpty(chair.Owner);
            if(bp=="MirePool")return e.GetPart<LiquidPoolPart>()?.LiquidId=="bog-mire"&&e.GetPart<LiquidPoolPart>().Volume==60
                &&e.HasPart<TileStateSourcePart>()&&e.HasPart<BurnOffGasPart>();
            if(bp=="SumpsieveCrop")return e.GetPart<CropPart>() is CropPart c&&c.HarvestAtMaturity&&c.YieldBlueprint=="SumpsievePad"
                &&c.YieldCount==2&&c.SeedYieldBlueprint=="SumpsieveSeed"&&c.SeedYieldCount==1;
            if(bp=="SoddenWorksLocker")return e.GetPart<ContainerPart>() is ContainerPart box&&!box.IsLocked;
            if(bp=="SoddenWorksSalvage")return e.GetPart<HarvestablePart>() is HarvestablePart h&&h.YieldBlueprint=="SalvagedTimber"
                &&h.YieldMin==4&&h.YieldMax==4&&h.YieldChance==100&&!h.Harvested;
            if(bp=="SoddenDressingBench")return e.GetPart<SoddenPreparationPart>()!=null&&e.Parts.Count(part=>part is SoddenPreparationPart)==1
                &&e.GetPart<RepairablePart>()?.RecipeId=="timber-dressing-bench"
                &&e.GetPart<RepairablePart>().Repaired==false&&e.GetPart<CompositionPart>()?.Contains("Wood")==true;
            return true;
        }
        static bool Portable(Entity e,string bp)=>e?.BlueprintName==bp&&e.GetPart<PhysicsPart>()?.Takeable==true
            &&e.GetPart<RenderPart>()?.Visible==true&&!e.HasTag("Creature")&&(e.GetPart<StackerPart>()?.StackCount??1)==1
            &&(bp=="KnotflaxCord"||e.GetPart<ArmorPart>()?.AV==1&&e.GetPart<EquippablePart>()!=null);
        static bool StockValid(Entity locker)
        {
            if(locker==null)return true;var box=locker.GetPart<ContainerPart>();
            if(box==null||box.IsLocked)return false;
            var names=box.Contents.SelectMany(e=>Enumerable.Repeat(e.BlueprintName,e.GetPart<StackerPart>()?.StackCount??1)).OrderBy(n=>n).ToArray();
            return names.SequenceEqual(new[]{"Buckler","KnotflaxCord","KnotflaxCord","LeatherBoots"})
                &&box.Contents.All(e=>e.GetPart<PhysicsPart>()?.InInventory==locker&&e.SpatialZone==null&&e.GetPart<PhysicsPart>().Equipped==null
                    &&e.GetPart<PhysicsPart>().Takeable&&(e.BlueprintName=="KnotflaxCord"||e.HasPart<ArmorPart>()&&e.HasPart<EquippablePart>()));
        }
        static bool GraphValid(IEnumerable<Entity> roots)
        {
            var seen=new HashSet<Entity>();var ids=new HashSet<string>(StringComparer.Ordinal);
            var queue=new Queue<(Entity item,Entity parent,bool equipped)>(roots.Select(e=>(e,(Entity)null,false)));
            while(queue.Count>0)
            {
                var (e,parent,equipped)=queue.Dequeue();var p=e?.GetPart<PhysicsPart>();
                if(e==null||!seen.Add(e)||string.IsNullOrEmpty(e.ID)||!ids.Add(e.ID)||p==null
                    ||e.Parts.Any(part=>part==null||part.ParentEntity!=e))return false;
                if(parent==null){if(p.InInventory!=null||p.Equipped!=null)return false;}
                else if(e.SpatialZone!=null)return false;
                else if(equipped){if(p.Equipped!=parent||(p.InInventory!=null&&p.InInventory!=parent))return false;}
                else if(p.InInventory!=parent||p.Equipped!=null)return false;
                var inventory=e.GetPart<InventoryPart>();
                var worn=(inventory?.EquippedItems.Values??Enumerable.Empty<Entity>())
                    .Concat(e.GetPart<Body>()?.GetParts().Where(b=>b.Equipped!=null).Select(b=>b.Equipped)??Enumerable.Empty<Entity>()).Distinct();
                foreach(var child in inventory?.Objects??Enumerable.Empty<Entity>())queue.Enqueue((child,e,false));
                foreach(var child in e.GetPart<ContainerPart>()?.Contents??Enumerable.Empty<Entity>())queue.Enqueue((child,e,false));
                foreach(var child in worn)queue.Enqueue((child,e,true));
            }
            return true;
        }
        static void DescribeNotice(Entity notice,string id)
        {
            var text=notice.GetPart<ExaminablePart>();if(text==null){text=new ExaminablePart();notice.AddPart(text);}
            notice.GetPart<RenderPart>().DisplayName=id==SoddenDistrictPlan.StopZoneID?"dressing shelter route notice":id==SoddenDistrictPlan.CrossingZoneID?"cutbank crossing route notice":"abandoned works tally";
            text.Text=id==SoddenDistrictPlan.StopZoneID
                ?"Sumphold lies one stretch north. The cutbank crossing is east, with the abandoned peat works one stretch farther east. Two sound lengths of timber from the works will brace this shelter's dressing bench. The keeper can then bind one sumpsieve pad and one knotflax cord for two drams. The ripe bed has pads; the dry bed can be watered. Dressings treat bleeding and ordinary poison, not marsh gas."
                :id==SoddenDistrictPlan.CrossingZoneID
                ?"The dressing shelter is west; the abandoned works are east. A scarlet-banded frog rests beside the direct mire crossing; striking its caustic skin can poison you. Raised boards turn north around the cut; the southern bank also stays dry. Both detours are longer. Heating mire can release poisonous marsh gas. Leather boots are armor, not protection from the mire."
                :"The works are abandoned. The broken frame still holds four sound lengths of timber; take it apart once. Two lengths will brace the dressing bench two stretches west, beyond the cutbank crossing. Boots, a buckler and two knotflax cords remain in the equipment locker. The dry work lane stays clear of the far copse, where a maw-toad waits.";
        }
        static bool Reject(Zone zone,string reason)
        {Diag.Record("worldgen","SoddenDistrictRejected",payload:new{zoneId=zone?.ZoneID,reason});return false;}
    }
}
