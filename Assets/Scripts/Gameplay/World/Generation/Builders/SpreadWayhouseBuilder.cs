using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>One late, atomic surface expedition. Replaces exact ordinary
    /// generation sources; never runs during attach, reading, or save restoration.</summary>
    public sealed class SpreadWayhouseBuilder : IZoneBuilder
    {
        public const string RoleKey="SpreadWayhouse.Role";
        public string Name=>"SpreadWayhouse";
        public int Priority=>4300;
        readonly OverworldZoneManager manager;
        readonly PopulationBuilder population;
        readonly ContainerBuilder containers;
        readonly SpreadCompositionBuilder terrain;
        readonly SpreadWayhousePlan selection;
        static readonly string[] Required={"StoneWall","VillageDoor","Signpost","Crate","Sack","IronKey","Buckler","MarlbackScrabbler"};

        public SpreadWayhouseBuilder(OverworldZoneManager manager,SpreadCompositionBuilder terrain,
            PopulationBuilder population,ContainerBuilder containers)
        {
            this.manager=manager;this.terrain=terrain;this.population=population;this.containers=containers;
            selection=manager?.Wayhouse;
            if(population!=null)population.CaptureSourceReceipts=true;
            if(containers!=null)containers.CaptureSourceReceipts=true;
        }

        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            if(selection==null||!ReferenceEquals(selection,manager.Wayhouse)||!selection.Selects(manager,zone?.ZoneID))return true;
            var actors=population?.SourceReceipt;var stock=containers?.SourceReceipt;
            if(factory!=manager.Factory||terrain?.SourceZone!=zone||terrain.Plan?.ZoneID!=zone.ZoneID
                ||manager.CachedZones.ContainsKey(zone.ZoneID)||actors?.IsCurrent!=true||stock?.IsCurrent!=true
                ||actors.Zone!=zone||stock.Zone!=zone||actors.Factory!=factory||stock.Factory!=factory
                ||actors.Owners.Count<1||actors.Owners.Count>2||stock.Owners.Count<1
                ||actors.Owners.Any(e=>e.BlueprintName!="MarlbackScrabbler"&&e.BlueprintName!="Viper")
                ||zone.GetReadOnlyEntities().Any(e=>e.Properties.ContainsKey(RoleKey)))return Refuse(zone,"source");
            if(Required.Any(id=>!factory.Blueprints.ContainsKey(id))||LoadoutPart.Factory!=factory)return Refuse(zone,"content");
            var replaced=actors.Owners.Concat(new[]{stock.Owners[0]}).ToArray();
            var ignored=new HashSet<Entity>(replaced);
            var candidates=new List<(int x,int y)>();
            // A 7x7 room and separated exterior key/notice stations fit between
            // working lanes. The footprint's empty gaps do not claim those lanes.
            for(int y=2;y<=Zone.Height-10;y++)for(int x=2;x<=Zone.Width-25;x++)
                if(Geometry(zone,x,y,ignored,out _))candidates.Add((x,y));
            if(candidates.Count==0)return Refuse(zone,"no-footprint");
            // Claim before factory callbacks: recursive or competing builders
            // cannot spend the same already-produced sources twice.
            if(!actors.TryConsume()||!stock.TryConsume())return Refuse(zone,"already-consumed-source");
            var staged=new List<(Entity owner,int dx,int dy)>();
            try
            {
                Entity Make(string bp,string role,int dx,int dy)
                {
                    var e=factory.CreateEntity(bp);
                    if(e==null||e.BlueprintName!=bp||string.IsNullOrEmpty(e.ID)||e.SpatialZone!=null||e.GetPart<PhysicsPart>() is not PhysicsPart p||p.ParentEntity!=e
                        ||p.InInventory!=null||p.Equipped!=null||!NativeAppearance(e)||e.HasPart<SpatialFootprintPart>())throw new InvalidOperationException("invalid staged owner");
                    e.Properties[RoleKey]=role;staged.Add((e,dx,dy));return e;
                }
                for(int y=1;y<=7;y++)for(int x=15;x<=21;x++)
                    if((x==15||x==21||y==1||y==7)&&!(x==15&&y==4)&&!(x==21&&y==6))Make("StoneWall","wall",x,y);
                var door=Make("VillageDoor","door",15,4);var dp=door.GetPart<DoorPart>();
                if(dp==null||dp.ParentEntity!=door||door.GetPart<PhysicsPart>().Solid||!string.IsNullOrEmpty(dp.OwnerId))throw new InvalidOperationException("invalid door");
                dp.IsOpen=false;dp.QuarterTurns=1;door.GetPart<RenderPart>().RenderString="+";
                string keyID="turnbank:"+manager.WorldSeed+":"+zone.ZoneID;
                door.AddPart(new LockPart{KeyId=keyID,IsLocked=true});
                var guard=Make("MarlbackScrabbler","guard",12,4);
                if(guard.GetPart<BrainPart>()?.SightRadius!=10||guard.GetStatValue("Hitpoints")!=15)throw new InvalidOperationException("changed guard");
                var sack=Make("Sack","key-sack",12,2);
                var cache=Make("Crate","cache",19,3);
                var notice=Make("Signpost","notice",0,4);
                if(notice.GetPart<ExaminablePart>()?.ParentEntity!=notice||sack.GetPart<ContainerPart>()?.ParentEntity!=sack||cache.GetPart<ContainerPart>()?.ParentEntity!=cache)throw new InvalidOperationException("missing furniture parts");
                notice.GetPart<ExaminablePart>().Text="Turnbank wayhouse. The old notice says the front-door key was kept in the outside sack. A service opening is around the far end of the building. The marks do not say who is here now, or what remains inside.";
                var key=factory.CreateEntity("IronKey");var reward=factory.CreateEntity("Buckler");
                if(!FreshItem(key,"IronKey")||!FreshItem(reward,"Buckler")||ReferenceEquals(key,reward)||key.GetPart<KeyPart>()?.ParentEntity!=key)throw new InvalidOperationException("invalid goods");
                if(!Current(zone,factory,actors,stock)||!FreshContainer(sack)||!FreshContainer(cache)
                    ||staged.Any(owner=>owner.owner.SpatialZone!=null||owner.owner.GetPart<PhysicsPart>()?.InInventory!=null
                        ||owner.owner.GetPart<PhysicsPart>()?.Equipped!=null))return Refuse(zone,"borrowed-furniture");
                key.GetPart<KeyPart>().KeyId=keyID;key.GetPart<RenderPart>().DisplayName="Turnbank wayhouse key";
                key.Properties[RoleKey]="key";reward.Properties[RoleKey]="reward";
                if(key.GetPart<ExaminablePart>()==null)key.AddPart(new ExaminablePart());
                key.GetPart<ExaminablePart>().Text="An iron key with the square notch cut into Turnbank wayhouse's front door. It can be used again after unlocking.";
                if(!sack.GetPart<ContainerPart>().AddItem(key)||!cache.GetPart<ContainerPart>().AddItem(reward))throw new InvalidOperationException("stock refused");
                var anchor=new Entity{BlueprintName="SpreadWayhouseState",ID=Guid.NewGuid().ToString("N")};anchor.Properties[RoleKey]="anchor";
                anchor.SetIntProperty("SpreadWayhouse.Version",1);staged.Add((anchor,0,0));
                if(staged.Select(t=>t.owner.ID).Distinct().Count()!=staged.Count||staged.Any(t=>t.owner.SpatialZone!=null)
                    ||!Current(zone,factory,actors,stock)||!Packet(zone,staged,key,reward,door,guard,sack,cache,keyID))return Refuse(zone,"staging-changed-source");
                var beforeReach=Reachable(zone,null,null);
                // Bounded deterministic trials reuse staged owners: no preferred loot rerolls.
                int trials=0;
                foreach(var at in candidates)
                {
                    if(++trials>96)break;
                    if(!Current(zone,factory,actors,stock)||!Geometry(zone,at.x,at.y,ignored,out var scrub))return Refuse(zone,"changed-footprint");
                    var removed=replaced.Concat(scrub).Distinct().Select(e=>(owner:e,pos:zone.GetEntityPosition(e))).ToArray();
                    bool accepted=false;
                    try
                    {
                        foreach(var r in removed)zone.RemoveEntity(r.owner);
                        bool placed=true;
                        foreach(var stagedOwner in staged)
                            if(!zone.AddEntity(stagedOwner.owner,at.x+stagedOwner.dx,at.y+stagedOwner.dy)){placed=false;break;}
                        accepted=placed&&selection.Selects(manager,zone.ZoneID)&&manager.Wayhouse==selection
                            &&staged.All(owner=>owner.owner.SpatialZone==zone&&zone.GetEntityPosition(owner.owner)==(at.x+owner.dx,at.y+owner.dy))
                            &&Packet(zone,staged,key,reward,door,guard,sack,cache,keyID)
                            &&Routes(zone,notice,guard,cache,beforeReach);
                    }
                    finally
                    {
                        if(!accepted)
                        {
                            foreach(var owner in staged)if(owner.owner.SpatialZone==zone)zone.RemoveEntity(owner.owner);
                            foreach(var r in removed)if(r.owner.SpatialZone==null)zone.AddEntity(r.owner,r.pos.x,r.pos.y);
                        }
                    }
                    if(!accepted)continue;
                    anchor.SetIntProperty("SpreadWayhouse.X",at.x);anchor.SetIntProperty("SpreadWayhouse.Y",at.y);
                    selection.PendingZone=zone;
                    manager.StagedWayhouse=selection;
                    selection.PendingValidation=()=>manager.Wayhouse==selection&&selection.Selects(manager,zone.ZoneID)
                        &&staged.All(s=>s.owner.SpatialZone==zone&&zone.GetEntityPosition(s.owner)==(at.x+s.dx,at.y+s.dy))
                        &&cache.GetPart<ContainerPart>().Contents.Count==1&&cache.GetPart<ContainerPart>().Contents[0]==reward
                        &&reward.GetPart<PhysicsPart>().InInventory==cache&&sack.GetPart<ContainerPart>().Contents.Count==1
                        &&sack.GetPart<ContainerPart>().Contents[0]==key&&key.GetPart<PhysicsPart>().InInventory==sack
                        &&Packet(zone,staged,key,reward,door,guard,sack,cache,keyID)
                        &&Routes(zone,notice,guard,cache,beforeReach);
                    if(Diag.IsChannelEnabled("worldgen"))Diag.Record("worldgen","SpreadWayhouseStaged",target:anchor,
                        payload:new{zone=zone.ZoneID,x=at.x,y=at.y,replacedActors=actors.Owners.Count,replacedContainers=1,reward=reward.ID});
                    return true;
                }
                return Refuse(zone,"no-safe-two-route-site");
            }
            catch(Exception)
            {return Refuse(zone,"invalid-required-owner");}
        }

        static bool NativeAppearance(Entity e)
        {
            var r=e?.GetPart<RenderPart>();
            return r!=null&&r.ParentEntity==e&&r.Visible&&string.IsNullOrEmpty(r.VisualID)
                &&string.IsNullOrEmpty(r.VisualVariant)&&!e.HasPart<SpatialFootprintPart>();
        }
        static bool Packet(Zone zone,List<(Entity owner,int dx,int dy)> staged,Entity key,Entity reward,
            Entity door,Entity guard,Entity sack,Entity cache,string keyID)
        {
            var owners=new HashSet<Entity>(staged.Select(s=>s.owner));
            var graph=new HashSet<Entity>();var ids=new HashSet<string>();var queue=new Queue<Entity>(owners);
            while(queue.Count>0)
            {
                var e=queue.Dequeue();
                if(e==null||!graph.Add(e)||graph.Count>128||string.IsNullOrEmpty(e.ID)||!ids.Add(e.ID))return false;
                if(e.GetProperty(RoleKey)!="anchor"&&!NativeAppearance(e))return false;
                var physics=e.GetPart<PhysicsPart>();
                if(e.GetProperty(RoleKey)!="anchor"&&(physics==null||physics.ParentEntity!=e))return false;
                if(owners.Contains(e)&&physics!=null&&(physics.InInventory!=null||physics.Equipped!=null))return false;
                if(e.SpatialZone!=null&&(e.SpatialZone!=zone||!owners.Contains(e)))return false;
                if(e.GetProperty(RoleKey)=="wall"&&(e.BlueprintName!="StoneWall"||!physics.Solid))return false;
                if(e.GetPart<ContainerPart>() is ContainerPart container)
                {
                    if(container.ParentEntity!=e)return false;
                    foreach(var child in container.Contents)
                    {if(child?.GetPart<PhysicsPart>()?.InInventory!=e)return false;queue.Enqueue(child);}
                }
                if(e.GetPart<InventoryPart>() is InventoryPart inventory)
                {
                    if(inventory.ParentEntity!=e)return false;
                    foreach(var child in inventory.Objects)
                    {if(child?.GetPart<PhysicsPart>()?.InInventory!=e||child.GetPart<PhysicsPart>().Equipped!=null)return false;queue.Enqueue(child);}
                    foreach(var child in inventory.EquippedItems.Values.Distinct())
                    {if(child?.GetPart<PhysicsPart>()?.Equipped!=e)return false;queue.Enqueue(child);}
                    var body=e.GetPart<Body>();
                    if(body!=null&&(body.ParentEntity!=e||body.GetParts().Any(part=>part.Equipped!=null
                        &&(!inventory.EquippedItems.Values.Contains(part.Equipped)||part.Equipped.GetPart<PhysicsPart>()?.Equipped!=e))))return false;
                }
            }
            foreach(var e in zone.GetReadOnlyEntities())
                if(!graph.Contains(e)&&ids.Contains(e.ID))return false;
            var d=door.GetPart<DoorPart>();var l=door.GetPart<LockPart>();
            return d?.ParentEntity==door&&d.IsClosed&&d.QuarterTurns==1&&string.IsNullOrEmpty(d.OwnerId)
                &&l?.ParentEntity==door&&l.IsLocked&&l.KeyId==keyID&&door.GetPart<RenderPart>().RenderString=="+"
                &&guard.GetPart<BrainPart>()?.ParentEntity==guard&&guard.GetPart<BrainPart>().SightRadius==10
                &&guard.GetStatValue("Hitpoints")==15&&guard.BlueprintName=="MarlbackScrabbler"
                &&key.BlueprintName=="IronKey"&&key.HasTag("NoTrade")&&(key.GetPart<StackerPart>()?.StackCount??1)==1
                &&key.GetPart<KeyPart>()?.ParentEntity==key&&key.GetPart<KeyPart>().KeyId==keyID
                &&reward.BlueprintName=="Buckler"&&(reward.GetPart<StackerPart>()?.StackCount??1)==1
                &&reward.GetPart<CommercePart>()?.ParentEntity==reward&&reward.GetPart<CommercePart>().Value==20
                &&sack.GetPart<ContainerPart>()?.Contents.Count==1
                &&sack.GetPart<ContainerPart>().Contents[0]==key&&cache.GetPart<ContainerPart>()?.Contents.Count==1
                &&cache.GetPart<ContainerPart>().Contents[0]==reward;
        }
        static bool FreshContainer(Entity owner)=>owner!=null&&owner.SpatialZone==null
            &&owner.GetPart<PhysicsPart>() is PhysicsPart physics&&physics.ParentEntity==owner
            &&physics.InInventory==null&&physics.Equipped==null
            &&owner.GetPart<ContainerPart>() is ContainerPart container&&container.ParentEntity==owner&&container.Contents.Count==0;
        bool Current(Zone z,EntityFactory f,SpreadGenerationReceipt a,SpreadGenerationReceipt c)
            => manager.Wayhouse==selection&&selection.Selects(manager,z.ZoneID)&&f==manager.Factory&&terrain.SourceZone==z
                &&!manager.CachedZones.ContainsKey(z.ZoneID)&&a.MatchesOwnedState()&&c.MatchesOwnedState();
        static bool FreshItem(Entity e,string bp)=>e?.BlueprintName==bp&&e.SpatialZone==null&&e.GetPart<PhysicsPart>() is PhysicsPart p
            &&p.ParentEntity==e&&p.Takeable&&p.InInventory==null&&p.Equipped==null&&e.GetPart<RenderPart>()?.ParentEntity==e;
        static IEnumerable<(int x,int y)> Mask(int ax,int ay)
        {
            for(int y=1;y<=7;y++)for(int x=15;x<=21;x++)yield return(ax+x,ay+y);
            yield return(ax,ay+4);yield return(ax+1,ay+4);yield return(ax+12,ay+4);yield return(ax+12,ay+2);
        }
        static bool Geometry(Zone z,int x,int y,HashSet<Entity> ignored,out List<Entity> scrub)
        {
            scrub=new List<Entity>();
            foreach(var p in Mask(x,y))
            {
                var cell=z.GetCell(p.x,p.y);
                if(cell==null||cell.IsInterior||z.GenReservedCells.Contains(p)||!Dry(z,p.x,p.y))return false;
                foreach(var e in cell.Occupants)
                {
                    if(ignored.Contains(e))continue;
                    if(e.BlueprintName=="Bush"&&!e.HasPart<HarvestablePart>()){scrub.Add(e);continue;}
                    if(!DoorPart.IsBareGround(e))return false;
                }
            }
            return true;
        }
        static bool Dry(Zone z,int x,int y)
        {
            var state=z.TileState.Get(x,y);
            return state==null||state.IsEmpty;
        }
        static bool Passable(Zone z,int x,int y)
        {
            var cell=z.GetCell(x,y);if(cell==null||!Dry(z,x,y)||cell.BlocksMovement())return false;
            foreach(var e in cell.Occupants)
                if(e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>()||e.HasPart<TriggerOnStepPart>())return false;
            return true;
        }
        static bool[,] Reachable(Zone z,(int x,int y)? start,Entity avoid)
        {
            var reached=new bool[Zone.Width,Zone.Height];var queue=new Queue<(int x,int y)>();
            var enemy=avoid==null?(-100,-100):z.GetEntityPosition(avoid);int sight=avoid?.GetPart<BrainPart>()?.SightRadius??0;
            void Add(int x,int y)
            {
                if(x<0||y<0||x>=Zone.Width||y>=Zone.Height||reached[x,y]||!Passable(z,x,y))return;
                if(avoid!=null&&Math.Max(Math.Abs(x-enemy.Item1),Math.Abs(y-enemy.Item2))<=sight
                    &&AIHelpers.HasLineOfSight(z,enemy.Item1,enemy.Item2,x,y))return;
                reached[x,y]=true;queue.Enqueue((x,y));
            }
            if(start.HasValue)Add(start.Value.x,start.Value.y);
            else for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)if(x==0||y==0||x==Zone.Width-1||y==Zone.Height-1)Add(x,y);
            while(queue.Count>0)
            {
                var at=queue.Dequeue();Add(at.x-1,at.y);Add(at.x+1,at.y);Add(at.x,at.y-1);Add(at.x,at.y+1);
            }
            return reached;
        }
        static bool Routes(Zone z,Entity notice,Entity guard,Entity cache,bool[,] before)
        {
            var n=z.GetEntityPosition(notice);var c=z.GetEntityPosition(cache);
            var safe=Reachable(z,(n.x+1,n.y),guard);
            bool reachesCache=false;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                if(z.GetCell(c.x+dx,c.y+dy)!=null&&safe[c.x+dx,c.y+dy])reachesCache=true;
            if(!reachesCache)return false;
            var all=Reachable(z,null,null);
            // Both authored entrances start in the same border-connected area.
            // A valid rear route alone must not hide an unreachable key station.
            if(!all[n.x+1,n.y])return false;
            var fromNotice=Reachable(z,(n.x+1,n.y),null);
            var sack=z.GetReadOnlyEntities().SingleOrDefault(e=>e.GetProperty(RoleKey)=="key-sack");
            var door=z.GetReadOnlyEntities().SingleOrDefault(e=>e.GetProperty(RoleKey)=="door");
            if(sack==null||door==null)return false;
            bool keyAccessible=false;var k=z.GetEntityPosition(sack);var d=z.GetEntityPosition(door);
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                if((dx!=0||dy!=0)&&z.GetCell(k.x+dx,k.y+dy)!=null&&fromNotice[k.x+dx,k.y+dy])keyAccessible=true;
            if(!keyAccessible||!fromNotice[d.x-1,d.y])return false;
            foreach(var p in z.GenReservedCells)if(before[p.x,p.y]&&!all[p.x,p.y])return false;
            foreach(var e in z.GetReadOnlyEntities())
                if(e.HasPart<StairsDownPart>()||e.HasPart<StairsUpPart>())
                {var at=z.GetEntityPosition(e);if(before[at.x,at.y]&&!all[at.x,at.y])return false;}
            // All originally connected edge approaches must still reach one another.
            (int x,int y)? first=null;
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                if(before[x,y]&&(x==0||y==0||x==Zone.Width-1||y==Zone.Height-1)){first=(x,y);break;}
            if(!first.HasValue)return false;
            var connected=Reachable(z,first,null);
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                if(before[x,y]&&(x==0||y==0||x==Zone.Width-1||y==Zone.Height-1)&&!connected[x,y])return false;
            return true;
        }
        static bool Refuse(Zone z,string reason)
        {if(Diag.IsChannelEnabled("worldgen"))Diag.Record("worldgen","SpreadWayhouseRefused",payload:new{zone=z?.ZoneID,reason});return true;}
    }
}
