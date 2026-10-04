using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Cold enrichment of the exact native receiving hall. Existing cargo is
    /// neither cloned nor moved; a contained side case is separate from public work.</summary>
    public sealed class CurationReceivingBuilder : IZoneBuilder
    {
        public string Name=>"CurationReceiving";
        public int Priority=>3865;
        readonly MarrowstyeCompositionBuilder terrain;
        readonly bool connected;
        public CurationReceivingBuilder(MarrowstyeCompositionBuilder terrain):this(terrain,false){}
        public CurationReceivingBuilder(MarrowstyeCompositionBuilder terrain, bool connected){this.terrain=terrain;this.connected=connected;}
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            var plan=terrain?.Plan;
            bool Authority()=>zone!=null && factory!=null && zone.ZoneID==MarrowstyeCompositionPlan.ZoneID
                && terrain?.Plan==plan && plan!=null && terrain.RealizedZone==zone && terrain.ProfileRealized && AreaCompositionScope.Allows(zone);
            if(!Authority() || rng==null || zone.GetReadOnlyEntities().Any(e=>e.HasPart<CurationIntakePart>()))return Reject(zone,"authority-or-replay");
            var hall=plan.Rooms.Single(r=>r.Role=="IntakeHall"); var supply=plan.Rooms.Single(r=>r.Role=="SupplyWing"); var disused=plan.Rooms.Single(r=>r.Role=="DisusedWing");
            var original=new HashSet<Entity>(zone.GetReadOnlyEntities());
            var bodies=original.Where(e=>e.BlueprintName=="SaltCuredBody").OrderBy(e=>zone.GetEntityPosition(e).x).ToArray();
            var clerks=original.Where(e=>e.BlueprintName=="FilerClerk").ToArray();
            if(bodies.Length!=2 || clerks.Length!=1 || !MarrowstyeCompositionBuilder.Valid(clerks[0],"FilerClerk"))return Reject(zone,"native-owners");
            for(int i=0;i<2;i++)if(!MarrowstyeCompositionBuilder.Valid(bodies[i],"SaltCuredBody") || bodies[i].HasPart<CurationReceivingBodyPart>()
                || !CurationIntakePart.Ground(bodies[i],zone,"SaltCuredBody") || bodies[i].HasTag("Creature") || bodies[i].GetPart<HandlingPart>()?.ParentEntity!=bodies[i]
                || zone.GetEntityPosition(bodies[i])!=(hall.X+7+10*i,hall.Y+5))return Reject(zone,"native-body-source");
            var specs=new List<(string bp,int x,int y)>
            {
                ("CurationIntakeIndex",hall.X+hall.Width-7,hall.Y+2),
                ("CurationReceivingBay",hall.X+17,hall.Y+4),("CurationReceivingBay",hall.X+7,hall.Y+4),
                ("CurationToolCabinet",supply.X+5,supply.Y+2),("CurationSaltBench",supply.X+8,supply.Y+3),
                ("CurationIntakeFiler",hall.X+3,hall.Y+4),("CurationJuniorIndexer",hall.X+hall.Width-7,hall.Y+4)
            };
            specs.AddRange(new[]
            {
                ("CurationQuarantineGate",disused.X+9,disused.Y+2),
                ("CurationTransferGate",disused.X+20,disused.Y+3),
                ("CurationGalleryGate",disused.X+15,disused.Y+6),
                ("CurationServiceGate",disused.X+23,disused.Y+6),
                ("CurationHalfSet",disused.X+14,disused.Y+3),
                ("CurationRecoveryCabinet",disused.X+12,disused.Y+1),
                ("CurationConservationCase",disused.X+18,disused.Y+5),
                ("CurationInspectionSlab",disused.X+16,disused.Y+3),
                ("CurationQuarantineRail",disused.X+18,disused.Y+1),
                ("CurationMaintenanceRack",disused.X+3,disused.Y+5),
                ("CurationAnnexPlacard",disused.X+7,disused.Y+1)
            });
            if(connected)specs.Add(("BotanicalInkDesk",hall.X+hall.Width-8,hall.Y+4));
            var goods=new[]{"CurationCounterfoil","CurationSaltRake","CurationInspectionKey","CurationTransferDocket","CurationDiscrepancyReport"};
            if(connected)goods=goods.Concat(new[]{"ShatteredRimeGrimoire"}).ToArray();
            var annexStock=new[]
            {
                (container:"CurationMaintenanceRack",bp:"SalvagedTimber",count:2),
                (container:"CurationConservationCase",bp:"SootrootPulp",count:2),
                (container:"CurationConservationCase",bp:"PitchpodResin",count:1),
                (container:"CurationRecoveryCabinet",bp:"FireClay",count:2),
                (container:"CurationRecoveryCabinet",bp:"SoddenFieldDressing",count:1),
                (container:"CurationRecoveryCabinet",bp:"LeatherGloves",count:1)
            };
            if(specs.Select(s=>s.bp).Concat(goods).Concat(annexStock.Select(s=>s.bp)).Any(bp=>!factory.Blueprints.ContainsKey(bp)) || specs.Select(s=>(s.x,s.y)).Distinct().Count()!=specs.Count
                || specs.Any(s=>!Bare(zone,s.x,s.y)))return Reject(zone,"required-content-or-slot");
            var originalProof=SpreadGenerationReceipt.CaptureFinalState(zone,original);
            var added=new HashSet<Entity>(); var placed=new Dictionary<Entity,Func<bool>>(); var staged=new List<Entity>();
            var otherOriginalProof=SpreadGenerationReceipt.CaptureFinalState(zone,original.Except(bodies));
            bool Source()=>Authority() && originalProof() && original.Concat(added).ToHashSet().SetEquals(zone.GetReadOnlyEntities()) && placed.Values.All(p=>p());
            var before=Flood(zone,new HashSet<(int,int)>());
            var oldLF=LoadoutPart.Factory;var oldTF=TraderPart.Factory;var oldLR=LoadoutPart.Rng;var oldTR=TraderPart.Rng;
            bool committed=false; var editedBodies=new HashSet<Entity>(); var markers=new List<(Entity owner,Part marker)>();
            var oldNames=bodies.Select(e=>e.GetPart<RenderPart>().DisplayName).ToArray();var oldTexts=bodies.Select(e=>e.GetPart<ExaminablePart>().Text).ToArray();
            try
            {
                LoadoutPart.Factory=TraderPart.Factory=factory;
                LoadoutPart.Rng=TraderPart.Rng=new Random(FormationSelector.StableIndex(zone.ZoneID+":curation-receiving",int.MaxValue));
                foreach(var spec in specs)
                {
                    if(!Source())return Reject(zone,"changed-original-source");
                    var e=factory.CreateEntity(spec.bp);
                    if(!Fresh(e,spec.bp) || !Valid(e,spec.bp) || !Source())return Reject(zone,"malformed-new-owner:"+spec.bp);
                    if(spec.bp=="CurationQuarantineGate"||spec.bp=="CurationTransferGate")e.GetPart<DoorPart>().QuarterTurns=1;
                    staged.Add(e);
                }
                var stock=new List<Entity>();
                foreach(string bp in goods)
                {
                    if(!Source())return Reject(zone,"changed-original-stock-source");
                    var item=factory.CreateEntity(bp);
                    if(!Fresh(item,bp) || item.GetPart<PhysicsPart>().Takeable!=true || item.HasTag("Creature") || !(bp=="ShatteredRimeGrimoire" ? (item.GetPart<StackerPart>()?.StackCount??1)==1 : CurationIntakePart.SinglePhysicalItem(item)) || !Source())return Reject(zone,"malformed-goods:"+bp);
                    stock.Add(item);
                }
                var index=staged[0]; var cabinet=staged[3]; var filer=staged[5];var indexer=staged[6];var enemy=staged.Single(e=>e.BlueprintName=="CurationHalfSet");
                if(stock[0].GetPart<KeyPart>()?.KeyId!="marrowstye-intake-tools" || stock[2].GetPart<KeyPart>()?.KeyId!="marrowstye-quarantine")return Reject(zone,"wrong-keys");
                if(!index.GetPart<InventoryPart>().AddObject(stock[0]))return Reject(zone,"index-stock");
                foreach(var item in stock.Skip(1).Take(4))if(!cabinet.GetPart<ContainerPart>().AddItem(item))return Reject(zone,"cabinet-stock");
                foreach(var provision in annexStock)
                {
                    if(!Source())return Reject(zone,"changed-annex-stock-source");
                    var item=factory.CreateEntity(provision.bp);var stack=item?.GetPart<StackerPart>();
                    if(!Fresh(item,provision.bp)||item.GetPart<PhysicsPart>().Takeable!=true||item.HasTag("Creature")
                        ||(stack==null?provision.count!=1:stack.StackCount!=1||stack.MaxStack<provision.count)||!Source())return Reject(zone,"malformed-annex-stock:"+provision.bp);
                    if(stack!=null)stack.StackCount=provision.count;
                    var holder=staged.Single(e=>e.BlueprintName==provision.container);
                    if(!holder.GetPart<ContainerPart>().AddItem(item)||item.GetPart<PhysicsPart>().InInventory!=holder)return Reject(zone,"annex-stock-refused");
                }
                if(connected)
                {
                    var book=stock[5];
                    if(book.GetPart<GrimoireChargePart>()?.Charges!=10||book.GetPart<GrimoirePart>()?.SkillClassName!="Rites_ShatteredRime")return Reject(zone,"invalid-public-book");
                    indexer.AddPart(new TraderPart{StockTable="",Drams=0});
                    TradeSystem.SetDrams(indexer,0);
                    if(!indexer.GetPart<InventoryPart>().AddObject(book))return Reject(zone,"public-book-stock");
                }
                foreach(var staff in new[]{clerks[0],filer,indexer})enemy.GetPart<BrainPart>().SetPersonallyHostile(staff,false);
                enemy.GetPart<BrainPart>().Target=null; // Hostility is saved; perception still chooses a target in play.
                if(!Source())return Reject(zone,"invalid-new-source");
                if(!UniqueNewGraphs(original,staged))return Reject(zone,"invalid-new-graph");
                var blocked=new HashSet<(int,int)>();
                for(int i=0;i<staged.Count;i++)if(!staged[i].HasTag("Creature") && (staged[i].GetPart<PhysicsPart>().Solid||staged[i].GetPart<DoorPart>()?.IsClosed==true))blocked.Add((specs[i].x,specs[i].y));
                var after=Flood(zone,blocked);
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                    if(before[x,y] && !after[x,y] && !blocked.Contains((x,y)) && !(x>disused.X+9&&x<disused.X+20&&y>disused.Y&&y<disused.Y+6))return Reject(zone,"public-route:"+x+","+y+" disused="+disused.X+","+disused.Y);
                for(int i=0;i<staged.Count;i++)
                {
                    if(!Source() || !Bare(zone,specs[i].x,specs[i].y))return Reject(zone,"changed-before-publication");
                    var e=staged[i];if(!zone.AddEntity(e,specs[i].x,specs[i].y))return Reject(zone,"placement-refused");
                    added.Add(e);placed[e]=SpreadGenerationReceipt.CaptureFinalState(zone,new[]{e});
                    if(!Source())return Reject(zone,"changed-during-publication");
                }
                if(connected)
                {
                    var desk=staged.Single(e=>e.BlueprintName=="BotanicalInkDesk");
                    desk.GetPart<BotanicalInkDeskPart>().Configure(zone,indexer);
                    if(!desk.GetPart<BotanicalInkDeskPart>().Configured)return Reject(zone,"ink-desk-binding");
                    placed[desk]=SpreadGenerationReceipt.CaptureFinalState(zone,new[]{desk});
                }
                var intake=index.GetPart<CurationIntakePart>();
                intake.Configure(zone,bodies[0],bodies[1],staged[1],staged[2],stock[0],filer,indexer);
                for(int i=0;i<2;i++)
                {
                    var marker=new CurationReceivingBodyPart{Index=index,CaseNumber=i+1};bodies[i].AddPart(marker);markers.Add((bodies[i],marker));
                    var bayMarker=new CurationReceivingBayPart{Index=index,CaseNumber=i+1};staged[i+1].AddPart(bayMarker);
                    editedBodies.Add(bodies[i]);
                    bodies[i].GetPart<RenderPart>().DisplayName=i==0?"Meret Pell, salt-cured":"Osren Vale, salt-cured";
                    bodies[i].GetPart<ExaminablePart>().Text=i==0
                        ?"Meret Pell. Last words: 'No, the other one.' Eyes salt-sealed; a crooked thimble is tied to the cuff. Name retained as a cross-reference. Filed under final words. A heavy cured subject; haul it to the matching quoted bay and release your hold before certification."
                        :"Osren Vale. Last words: 'Leave the door open.' Eyes salt-sealed; blue thread is knotted around the wrist. Name retained as a cross-reference. Filed under final words. The quotation is a record, not an instruction to open quarantine. Haul to the matching quoted bay and release your hold.";
                    staged[i+1].GetPart<ExaminablePart>().Text=i==0
                        ?"Receiving position: 'No, the other one.' Cross-reference: Meret Pell; crooked thimble. Match the subject, then release your hold."
                        :"Receiving position: 'Leave the door open.' Cross-reference: Osren Vale; blue wrist-thread. These are recorded last words, not an instruction to open quarantine. Match the subject, then release your hold.";
                    if(!marker.IsCurrent(zone)||!bayMarker.IsCurrent(zone))return Reject(zone,"binding-refused");
                }
                // These are our final synchronous writes; no event callbacks intervene.
                // Retain all unrelated original owners verbatim after subject labels change.
                foreach(var e in new[]{index,staged[1],staged[2]})placed[e]=SpreadGenerationReceipt.CaptureFinalState(zone,new[]{e});
                if(!Authority() || !otherOriginalProof() || !original.Concat(added).ToHashSet().SetEquals(zone.GetReadOnlyEntities())
                    || !placed.Values.All(p=>p()))return Reject(zone,"changed-final-source");
                foreach(var spec in specs)zone.GenReservedCells.Add((spec.x,spec.y));
                committed=true;
                Diag.Record("worldgen","CurationReceivingPlaced",payload:new{zoneId=zone.ZoneID,owners=staged.Count,first=bodies[0].ID,second=bodies[1].ID});return true;
            }
            catch(Exception error){return Reject(zone,"exception:"+error.GetType().Name);}
            finally
            {
                LoadoutPart.Factory=oldLF;TraderPart.Factory=oldTF;LoadoutPart.Rng=oldLR;TraderPart.Rng=oldTR;
                if(!committed)
                {
                    foreach(var pair in markers)if(pair.marker.ParentEntity==pair.owner&&pair.owner.Parts.Contains(pair.marker))pair.owner.RemovePart(pair.marker);
                    for(int i=0;i<bodies.Length;i++)if(editedBodies.Contains(bodies[i]) && zone.GetEntityCell(bodies[i])!=null)
                    {bodies[i].GetPart<RenderPart>().DisplayName=oldNames[i];bodies[i].GetPart<ExaminablePart>().Text=oldTexts[i];}
                    foreach(var e in staged.AsEnumerable().Reverse())
                        if(added.Contains(e) && e.SpatialZone==zone && placed.TryGetValue(e,out var ownState) && ownState())zone.RemoveEntity(e);
                    // Reservations are published only at successful completion, never removed on refusal.
                }
            }
        }
        static bool Bare(Zone zone,int x,int y)
        {
            var cell=zone.GetCell(x,y);var state=zone.TileState.Get(x,y);
            return cell!=null && cell.Objects.All(DoorPart.IsBareGround) && state?.IsEmpty!=false;
        }
        static bool Fresh(Entity e,string bp)=>e!=null && e.BlueprintName==bp && !string.IsNullOrEmpty(e.ID) && e.SpatialZone==null
            && e.GetPart<PhysicsPart>() is PhysicsPart p && p.InInventory==null && p.Equipped==null && e.GetPart<RenderPart>()?.Visible==true
            && e.HasPart<ExaminablePart>() && !e.HasPart<SpatialFootprintPart>() && e.Parts.All(part=>part!=null&&part.ParentEntity==e);
        static bool Valid(Entity e,string bp)
        {
            var p=e.GetPart<PhysicsPart>();if(p.Takeable)return false;
            if(bp=="CurationTransferGate"||bp=="CurationGalleryGate"||bp=="CurationServiceGate")
            {
                var annexDoor=e.GetPart<DoorPart>();
                if(p.Solid||e.HasTag("Solid")||e.HasTag("Creature")||e.HasPart<BrainPart>()||e.HasPart<LockPart>()
                    ||annexDoor==null||!string.IsNullOrEmpty(annexDoor.OwnerId)||e.HasPart<DestructiblePart>())return false;
                if(bp!="CurationServiceGate")return !annexDoor.IsOpen&&!e.HasPart<RepairablePart>();
                var fault=e.GetPart<RepairablePart>();
                return annexDoor.IsOpen&&fault!=null&&!fault.Repaired&&fault.RecipeId=="timber-gate-frame"&&e.GetPart<CompositionPart>()?.Contains("Wood")==true;
            }
            if(bp=="CurationReceivingBay")return !p.Solid && !e.HasTag("Solid") && !e.HasTag("Creature");
            if(bp=="CurationIntakeFiler"||bp=="CurationJuniorIndexer"||bp=="CurationHalfSet")
            {
                var brain=e.GetPart<BrainPart>();
                if(!p.Solid||!e.HasTag("Creature")||brain==null||e.GetStatValue("Hitpoints")<=0||e.HasTag("Player")
                    ||e.HasPart<TraderPart>()||brain.Target!=null||brain.PartyLeader!=null||brain.PartyMembers.Count!=0||e.GetPart<InventoryPart>()?.Objects.Count!=0)return false;
                return bp=="CurationHalfSet" ? !brain.Passive && !e.HasPart<ConversationPart>() && !e.HasTag("CanOpenDoors")
                    : brain.Passive && e.GetPart<ConversationPart>()?.ConversationID==bp+"_1";
            }
            if(!p.Solid||e.HasTag("Creature")||e.HasPart<BrainPart>()||e.HasPart<LiquidPoolPart>()||e.HasPart<TileStateSourcePart>())return false;
            if(bp=="CurationIntakeIndex")return e.GetPart<CurationIntakePart>() is CurationIntakePart intake && !intake.Configured && !intake.Certified && e.GetPart<InventoryPart>()?.Objects.Count==0 && !e.HasPart<ContainerPart>();
            if(bp=="CurationToolCabinet")return e.GetPart<ContainerPart>() is ContainerPart c && !c.Locked && c.Contents.Count==0 && (c.MaxItems<0||c.MaxItems>=4)
                && e.GetPart<LockPart>() is LockPart l && l.IsLocked && l.KeyId=="marrowstye-intake-tools";
            if(bp=="CurationQuarantineGate")return e.GetPart<DoorPart>() is DoorPart door && !door.IsOpen && string.IsNullOrEmpty(door.OwnerId)
                && e.GetPart<LockPart>() is LockPart gateLock && gateLock.IsLocked && gateLock.KeyId=="marrowstye-quarantine" && !e.HasPart<DestructiblePart>();
            if(bp=="CurationQuarantineRail")return !e.HasPart<DoorPart>() && !e.HasPart<DestructiblePart>() && !e.HasPart<HandlingPart>();
            if(bp=="CurationMaintenanceRack"||bp=="CurationRecoveryCabinet"||bp=="CurationConservationCase")
            {
                var container=e.GetPart<ContainerPart>();int capacity=bp=="CurationMaintenanceRack"?1:bp=="CurationConservationCase"?2:3;
                return container!=null&&!container.IsLocked&&container.Contents.Count==0&&(container.MaxItems<0||container.MaxItems>=capacity);
            }
            return bp=="CurationSaltBench"||bp=="CurationInspectionSlab"||bp=="CurationAnnexPlacard" || (bp=="BotanicalInkDesk" && e.HasPart<BotanicalInkDeskPart>());
        }
        static IEnumerable<Entity> Children(Entity e)=>(e.GetPart<InventoryPart>()?.Objects??Enumerable.Empty<Entity>())
            .Concat(e.GetPart<ContainerPart>()?.Contents??Enumerable.Empty<Entity>())
            .Concat(e.GetPart<InventoryPart>()?.EquippedItems.Values??Enumerable.Empty<Entity>())
            .Concat(e.GetPart<Body>()?.GetParts().SelectMany(p=>new[]{p.Equipped,p.DefaultBehavior}).Where(p=>p!=null)??Enumerable.Empty<Entity>()).Distinct();
        static bool UniqueNewGraphs(IEnumerable<Entity> old,IEnumerable<Entity> roots)
        {
            var ids=new HashSet<string>(StringComparer.Ordinal);var visited=new HashSet<Entity>();var queue=new Queue<Entity>(old);
            while(queue.Count>0){var e=queue.Dequeue();if(e==null||!visited.Add(e))continue;ids.Add(e.ID);foreach(var c in Children(e))queue.Enqueue(c);}
            visited.Clear();queue=new Queue<Entity>(roots);
            while(queue.Count>0)
            {
                var e=queue.Dequeue();
                if(e==null||!visited.Add(e)||visited.Count>128||e.SpatialZone!=null||e.Parts.Any(p=>p==null||p.ParentEntity!=e))return false;
                // Anatomy creates transient natural weapons without persistent entity IDs.
                if(string.IsNullOrEmpty(e.ID) ? !e.HasTag("Natural")||e.HasPart<PhysicsPart>()||!e.HasPart<MeleeWeaponPart>() : !ids.Add(e.ID))return false;
                foreach(var c in Children(e))queue.Enqueue(c);
            }
            return true;
        }
        static bool[,] Flood(Zone zone,HashSet<(int,int)> blocked)
        {
            var seen=new bool[Zone.Width,Zone.Height];var queue=new Queue<(int x,int y)>();queue.Enqueue((40,11));seen[40,11]=true;
            while(queue.Count>0)
            {
                var p=queue.Dequeue();foreach(var d in new[]{(-1,0),(1,0),(0,-1),(0,1)})
                {
                    var n=(x:p.x+d.Item1,y:p.y+d.Item2);
                    if(!zone.InBounds(n.x,n.y)||seen[n.x,n.y]||blocked.Contains(n))continue;
                    if(zone.GetCell(n.x,n.y).Objects.Any(e=>!e.HasTag("Creature")&&(e.GetPart<DoorPart>() is DoorPart door?door.IsClosed:e.GetPart<PhysicsPart>()?.Solid==true)))continue;
                    seen[n.x,n.y]=true;queue.Enqueue(n);
                }
            }
            return seen;
        }
        static bool Reject(Zone zone,string reason){Diag.Record("worldgen","CurationReceivingRejected",payload:new{zoneId=zone?.ZoneID,reason});return false;}
    }
}
