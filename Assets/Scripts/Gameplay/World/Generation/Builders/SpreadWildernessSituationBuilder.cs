using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>One late, optional rearrangement of exact ordinary rolled owners.
    /// No factory calls, new stock, clearing, AI changes or random draws.</summary>
    public sealed class SpreadWildernessSituationBuilder : IZoneBuilder
    {
        public string Name=>"SpreadWildernessSituation";
        public int Priority=>4300;
        public string LastResult {get;private set;}="";
        public int LayoutTrials {get;private set;}
        const int MaxLayoutTrials=256;
        readonly OverworldZoneManager manager;
        readonly SpreadCompositionBuilder terrain;
        readonly PopulationBuilder population;
        readonly ContainerBuilder containers;
        readonly string excluded;
        SpreadGenerationReceipt attempted;
        public SpreadWildernessSituationBuilder(OverworldZoneManager manager,SpreadCompositionBuilder terrain,
            PopulationBuilder population,ContainerBuilder containers,string excludedWayhouseZoneID)
        {
            this.manager=manager;this.terrain=terrain;this.population=population;this.containers=containers;excluded=excludedWayhouseZoneID;
            if(population!=null)population.CaptureSourceReceipts=true;
            if(containers!=null)containers.CaptureSourceReceipts=true;
        }
        public bool BuildZone(Zone zone,EntityFactory factory,Random rng)
        {
            LastResult="";LayoutTrials=0;
            string kind=SpreadWildernessSituationPlan.Select(manager,zone?.ZoneID,excluded);
            if(kind!="cargo"&&kind!="shelter")return true;
            var stock=containers?.SourceReceipt;var actors=population?.SourceReceipt;
            if(!Source(zone,factory,kind,stock,actors)||ReferenceEquals(attempted,stock))return Refuse(zone,kind,"source");
            attempted=stock;
            var cache=stock.Owners.FirstOrDefault(e=>Cache(zone,e));
            if(cache==null)return Refuse(zone,kind,"no-rolled-open-cache");
            Entity[] moving=kind=="cargo"?new[]{cache}:new[]{cache}.Concat(actors.Owners).ToArray();
            var ignored=new HashSet<Entity>(moving);
            var original=moving.Select(e=>zone.GetEntityPosition(e)).ToArray();
            var physical=new Geometry(zone,ignored);
            (int x,int y)[] destinations=null;
            for(int y=2;y<Zone.Height-2&&destinations==null;y++)for(int x=2;x<Zone.Width-2;x++)
            {
                if(!physical.Place(x,y)||original[0]==(x,y))continue;
                if(kind=="cargo")
                {
                    if(LayoutTrials>=MaxLayoutTrials)return Refuse(zone,kind,"layout-budget");LayoutTrials++;
                    var candidate=new[]{(x,y)};
                    if(physical.PreservesRoutes(candidate)&&Cargo(zone,physical,candidate[0])){destinations=candidate;break;}
                }
                else
                {
                    if(!Cover(zone,x,y))continue;
                    var nearby=new List<(int x,int y)>();
                    for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)
                        if((dx!=0||dy!=0)&&physical.Place(x+dx,y+dy))nearby.Add((x+dx,y+dy));
                    // Try every ordered neighboring body pair, bounded by 24^2.
                    for(int i=0;i<nearby.Count&&destinations==null;i++)
                    {
                        if(moving.Length==2)
                        {
                            if(LayoutTrials>=MaxLayoutTrials)return Refuse(zone,kind,"layout-budget");LayoutTrials++;
                            var candidate=new[]{(x,y),nearby[i]};
                            if(physical.PreservesRoutes(candidate)&&Shelter(zone,physical,moving,candidate))destinations=candidate;
                        }
                        else for(int j=i+1;j<nearby.Count;j++)
                        {
                            if(LayoutTrials>=MaxLayoutTrials)return Refuse(zone,kind,"layout-budget");LayoutTrials++;
                            var candidate=new[]{(x,y),nearby[i],nearby[j]};
                            if(physical.PreservesRoutes(candidate)&&Shelter(zone,physical,moving,candidate)){destinations=candidate;break;}
                        }
                    }
                    if(destinations!=null)break;
                }
            }
            if(destinations==null)return Refuse(zone,kind,"no-existing-geometry");
            // Nothing above dispatches gameplay or modifies the graph. Claim only
            // after a complete physical plan; claim failures never move owners.
            if(!Source(zone,factory,kind,stock,actors)||!stock.TryConsume()||(kind=="shelter"&&!actors.TryConsume()))return Refuse(zone,kind,"changed-source");
            bool committed=false;
            try
            {
                for(int i=0;i<moving.Length;i++)if(!zone.MoveEntity(moving[i],destinations[i].x,destinations[i].y))return Refuse(zone,kind,"move-refused");
                committed=stock.MatchesOwnedState()&&(kind!="shelter"||actors.MatchesOwnedState())
                    &&moving.Select((e,i)=>e.SpatialZone==zone&&zone.GetEntityPosition(e)==destinations[i]).All(v=>v);
                if(!committed)return Refuse(zone,kind,"changed-commit");
                LastResult=kind;
                if(Diag.IsChannelEnabled("worldgen"))Diag.Record("worldgen","SpreadWildernessSituationCommitted",target:cache,
                    payload:new{zone=zone.ZoneID,kind,owners=moving.Select(e=>e.ID).ToArray(),before=original,after=destinations});
                return true;
            }
            finally
            {
                if(!committed)for(int i=0;i<moving.Length;i++)
                    if(moving[i].SpatialZone==zone&&zone.GetEntityPosition(moving[i])==destinations[i])zone.MoveEntity(moving[i],original[i].x,original[i].y);
            }
        }
        bool Source(Zone z,EntityFactory f,string kind,SpreadGenerationReceipt stock,SpreadGenerationReceipt actors)
            =>z!=null&&f!=null&&ReferenceEquals(f,manager?.Factory)&&terrain?.SourceZone==z&&terrain.Plan?.ZoneID==z.ZoneID
                &&terrain.Plan.Seed==manager.WorldSeed&&terrain.Plan.Formation==FormationSelector.For(BiomeType.Spread,z.ZoneID)
                &&!manager.CachedZones.ContainsKey(z.ZoneID)&&stock?.IsCurrent==true&&stock.Zone==z&&stock.Factory==f
                &&SpreadWildernessSituationPlan.Select(manager,z.ZoneID,excluded)==kind
                &&(kind!="shelter"||(actors?.IsCurrent==true&&actors.Zone==z&&actors.Factory==f&&actors.Owners.Count>=1&&actors.Owners.Count<=2
                    &&actors.Owners.All(e=>Actor(z,e))));
        static bool Body(Zone z,Entity e)=>e!=null&&e.SpatialZone==z&&e.GetPart<PhysicsPart>() is PhysicsPart p&&p.ParentEntity==e
            &&p.InInventory==null&&p.Equipped==null&&!e.HasPart<SpatialFootprintPart>()&&z.GetOccupiedCells(e).Count==1;
        static bool Cache(Zone z,Entity e)=>Body(z,e)&&(e.BlueprintName=="Crate"||e.BlueprintName=="Sack")
            &&!e.GetPart<PhysicsPart>().Takeable&&e.GetPart<ContainerPart>() is ContainerPart c&&c.ParentEntity==e&&!c.IsLocked
            &&e.GetPart<RenderPart>()?.ParentEntity==e&&!e.HasPart<DoorPart>();
        static bool Actor(Zone z,Entity e)=>Body(z,e)&&e.BlueprintName=="MarlbackScrabbler"&&e.HasTag("Creature")
            &&e.GetStatValue("Hitpoints")>0&&e.GetPart<BrainPart>() is BrainPart b&&b.ParentEntity==e&&b.SightRadius>0;
        static bool Cover(Zone z,int x,int y)
        {
            for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)
                if(z.GetCell(x+dx,y+dy)?.Occupants.Any(e=>(e.BlueprintName=="Hedge"||e.BlueprintName=="Tree")&&e.GetPart<PhysicsPart>()?.Solid==true)==true)return true;
            return false;
        }
        static bool Cargo(Zone z,Geometry g,(int x,int y) cache)
        {
            var blocked=new HashSet<(int x,int y)>{cache};
            // Short physical spur to an actual connected road tile. Never call a
            // ground glyph or a disconnected decorative tile a road approach.
            var reach=g.Flood(blocked,cache,4,null);
            for(int dy=-4;dy<=4;dy++)for(int dx=-4;dx<=4;dx++)
            {
                int x=cache.x+dx,y=cache.y+dy;
                if(g.In(x,y)&&reach[x,y]&&g.BorderReach[x,y]&&z.GetCell(x,y).Occupants.Any(e=>e.BlueprintName=="RoadStone")
                    &&AIHelpers.HasLineOfSight(z,x,y,cache.x,cache.y))return true;
            }
            return false;
        }
        static bool Shelter(Zone z,Geometry g,Entity[] owners,(int x,int y)[] dest)
        {
            var blocked=new HashSet<(int x,int y)>(dest);
            bool Avoid(int x,int y)
            {
                for(int i=1;i<owners.Length;i++)if(Math.Max(Math.Abs(x-dest[i].x),Math.Abs(y-dest[i].y))<=owners[i].GetPart<BrainPart>().SightRadius)return true;
                return false;
            }
            var safe=g.Flood(blocked,null,int.MaxValue,Avoid);
            // At least one real opposite-border bypass must remain connected,
            // not two disconnected border components counted as one flood.
            bool bypass=false;
            for(int y=0;y<Zone.Height&&!bypass;y++)if(safe[0,y])
            {var path=g.Flood(blocked,(0,y),int.MaxValue,Avoid);for(int yy=0;yy<Zone.Height;yy++)if(path[Zone.Width-1,yy]){bypass=true;break;}}
            if(!bypass)for(int x=0;x<Zone.Width&&!bypass;x++)if(safe[x,0])
            {var path=g.Flood(blocked,(x,0),int.MaxValue,Avoid);for(int xx=0;xx<Zone.Width;xx++)if(path[xx,Zone.Height-1]){bypass=true;break;}}
            if(!bypass)return false;
            bool view=false,approach=false;
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                int distance=Math.Max(Math.Abs(x-dest[0].x),Math.Abs(y-dest[0].y));
                if(distance==1&&g.BorderReach[x,y]&&g.Walk(x,y,blocked))approach=true;
                if(safe[x,y]&&distance<=14&&AIHelpers.HasLineOfSight(z,x,y,dest[0].x,dest[0].y))view=true;
            }
            return view&&approach;
        }
        bool Refuse(Zone z,string kind,string reason)
        {LastResult="refused:"+reason;if(Diag.IsChannelEnabled("worldgen"))Diag.Record("worldgen","SpreadWildernessSituationRefused",payload:new{zone=z?.ZoneID,kind,reason});return true;}

        // A bounded virtual occupancy map permits full preflight without moving
        // anything. Single-cell admission above makes these anchors exact bodies.
        sealed class Geometry
        {
            readonly Zone zone;readonly bool[,] passable=new bool[Zone.Width,Zone.Height];readonly bool[,] places=new bool[Zone.Width,Zone.Height];
            readonly int[,] components=new int[Zone.Width,Zone.Height];readonly List<(int x,int y)> critical=new List<(int x,int y)>();
            internal readonly bool[,] BorderReach;
            internal Geometry(Zone z,HashSet<Entity> ignored)
            {
                zone=z;
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                {
                    var c=z.GetCell(x,y);bool dry=z.TileState.Get(x,y)?.IsEmpty!=false;
                    bool pass=dry,bare=dry&&!c.IsInterior&&!z.GenReservedCells.Contains((x,y));
                    foreach(var e in c.Occupants)
                    {
                        if(ignored.Contains(e))continue;
                        if(e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>()||e.HasPart<TriggerOnStepPart>())pass=bare=false;
                        if(e.HasTag("Solid")||e.GetPart<PhysicsPart>()?.Solid==true||e.GetPart<DoorPart>()?.IsClosed==true
                            ||e.GetPart<SealedLibraryBarrierPart>()?.IsClosed==true||e.HasTag("Creature"))pass=false;
                        if(!DoorPart.IsBareGround(e)||e.HasPart<StairsUpPart>()||e.HasPart<StairsDownPart>())bare=false;
                    }
                    passable[x,y]=pass;places[x,y]=bare&&pass;
                    if(pass&&(x==0||y==0||x==Zone.Width-1||y==Zone.Height-1||z.GenReservedCells.Contains((x,y))))critical.Add((x,y));
                }
                // Every actual stair's previously open approach is protected.
                foreach(var e in z.GetReadOnlyEntities().Where(e=>e.HasPart<StairsUpPart>()||e.HasPart<StairsDownPart>()))
                {var at=z.GetEntityPosition(e);for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(In(at.x+dx,at.y+dy)&&passable[at.x+dx,at.y+dy])critical.Add((at.x+dx,at.y+dy));}
                Label(null,components);BorderReach=Flood(null,null,int.MaxValue,null);
            }
            internal bool In(int x,int y)=>x>=0&&x<Zone.Width&&y>=0&&y<Zone.Height;
            internal bool Place(int x,int y)=>In(x,y)&&x>=2&&y>=2&&x<Zone.Width-2&&y<Zone.Height-2&&places[x,y];
            internal bool Walk(int x,int y,HashSet<(int x,int y)> blocked)=>In(x,y)&&passable[x,y]&&(blocked==null||!blocked.Contains((x,y)));
            internal bool PreservesRoutes((int x,int y)[] dest)
            {
                var blocked=new HashSet<(int x,int y)>(dest);if(blocked.Count!=dest.Length)return false;
                var next=new int[Zone.Width,Zone.Height];Label(blocked,next);var mapping=new Dictionary<int,int>();
                foreach(var p in critical)
                {int a=components[p.x,p.y],b=next[p.x,p.y];if(b==0)return false;if(mapping.TryGetValue(a,out int prior)&&prior!=b)return false;mapping[a]=b;}
                return true;
            }
            void Label(HashSet<(int x,int y)> blocked,int[,] labels)
            {
                int id=0;var q=new Queue<(int x,int y)>();
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                {
                    if(labels[x,y]!=0||!Walk(x,y,blocked))continue;labels[x,y]=++id;q.Enqueue((x,y));
                    while(q.Count>0){var p=q.Dequeue();for(int d=0;d<4;d++){int xx=p.x+(d==0?1:d==1?-1:0),yy=p.y+(d==2?1:d==3?-1:0);if(!Walk(xx,yy,blocked)||labels[xx,yy]!=0)continue;labels[xx,yy]=id;q.Enqueue((xx,yy));}}
                }
            }
            internal bool[,] Flood(HashSet<(int x,int y)> blocked,(int x,int y)? start,int limit,Func<int,int,bool> avoid)
            {
                var reached=new bool[Zone.Width,Zone.Height];var q=new Queue<(int x,int y,int distance)>();
                void Add(int x,int y,int distance){if(!Walk(x,y,blocked)||reached[x,y]||distance>limit||avoid?.Invoke(x,y)==true)return;reached[x,y]=true;q.Enqueue((x,y,distance));}
                if(start.HasValue)
                {
                    // A blocked container source seeds its four approach cells.
                    var p=start.Value;if(blocked?.Contains(p)==true){Add(p.x-1,p.y,1);Add(p.x+1,p.y,1);Add(p.x,p.y-1,1);Add(p.x,p.y+1,1);}else Add(p.x,p.y,0);
                }
                else for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)if(x==0||y==0||x==Zone.Width-1||y==Zone.Height-1)Add(x,y,0);
                while(q.Count>0){var p=q.Dequeue();Add(p.x-1,p.y,p.distance+1);Add(p.x+1,p.y,p.distance+1);Add(p.x,p.y-1,p.distance+1);Add(p.x,p.y+1,p.distance+1);}
                return reached;
            }
        }
    }
}
